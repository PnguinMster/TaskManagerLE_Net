// site.js loads at the end of <body> (see _Layout), so the page's elements already exist when it
// runs. Each feature lives in its own setUpX() function, called at the bottom of this file. Every one
// returns early when its elements aren't on the page, so one page's missing markup never stops the rest.

// Reads and writes localStorage. It can throw (blocked storage, private mode), so never let it break a
// toggle: loading falls back to the default, and not saving just means the default next time.
function loadSetting(key) {
    try {
        return localStorage.getItem(key);
    } catch {
        return null;
    }
}

function saveSetting(key, value) {
    try {
        localStorage.setItem(key, value);
    } catch {
        // Not saving is fine: the user just gets the default next time.
    }
}

// Compares two names for "Sort by: Name" style ordering. sensitivity "base" ignores case and accents,
// so "apple" and "Apple" sort together instead of every capital coming first.
function compareText(a, b) {
    return a.localeCompare(b, undefined, { sensitivity: "base" });
}

// The light/dark button in the header (_Layout).
function setUpThemeToggle() {
    const root = document.documentElement;
    const themeToggle = document.getElementById("theme-toggle");
    if (!themeToggle) return;

    const THEME_KEY = "theme";

    function setTheme(theme) {
        root.setAttribute("data-bs-theme", theme);
        themeToggle.setAttribute(
            "aria-label",
            `Switch to ${theme === "dark" ? "light" : "dark"} mode`
        );
    }

    // Only trust known values.
    setTheme(loadSetting(THEME_KEY) === "light" ? "light" : "dark");

    themeToggle.addEventListener("click", () => {
        const nextTheme =
            root.getAttribute("data-bs-theme") === "dark" ? "light" : "dark";

        setTheme(nextTheme);
        saveSetting(THEME_KEY, nextTheme);
    });
}

// Projects page: the grid/list buttons. The inline script at the bottom of Project/Index applies the
// saved choice before the first paint; this keeps the buttons working and saves new choices.
function setUpProjectViewToggle() {
    const gridButton = document.getElementById("grid-view");
    const listButton = document.getElementById("list-view");
    const grid = document.getElementById("project-grid");
    const list = document.getElementById("project-list");
    if (!gridButton || !listButton || !grid || !list) return;

    const VIEW_KEY = "projectView"; // keep in sync with the inline script in Project/Index

    function setView(view) {
        const showGrid = view === "grid";

        grid.hidden = !showGrid;
        list.hidden = showGrid;

        gridButton.classList.toggle("is-active", showGrid);
        listButton.classList.toggle("is-active", !showGrid);
        gridButton.setAttribute("aria-pressed", String(showGrid));
        listButton.setAttribute("aria-pressed", String(!showGrid));
    }

    gridButton.addEventListener("click", () => {
        setView("grid");
        saveSetting(VIEW_KEY, "grid");
    });
    listButton.addEventListener("click", () => {
        setView("list");
        saveSetting(VIEW_KEY, "list");
    });

    // Only trust known values.
    setView(loadSetting(VIEW_KEY) === "list" ? "list" : "grid");
}

// The create and edit modals for projects and tasks (<form data-fetch-form>).
function setUpModalForms() {
    document.querySelectorAll("form[data-fetch-form]").forEach(form => {
        form.addEventListener("submit", event => {
            event.preventDefault();
            submitModalForm(event.currentTarget);
        });
    });

    // The server pre-fills its own "today" as a fallback (the pages that render the create
    // modals set DueDate). Replace it with the browser's date, since "today" depends on the
    // user's timezone. Built from local parts: toISOString() would give the UTC date instead.
    const now = new Date();
    const month = String(now.getMonth() + 1).padStart(2, "0");
    const day = String(now.getDate()).padStart(2, "0");
    const today = `${now.getFullYear()}-${month}-${day}`;
    document.querySelectorAll("form[data-create-form] [name='DueDate']").forEach(input => {
        // defaultValue (not just value) so form.reset() on modal close keeps the browser's date.
        input.defaultValue = today;
        input.value = today;
    });
}

// Sends a modal's form without leaving the page (Create and Update on ProjectController and
// TaskController). Success reloads the page the modal is on, so its filters and
// ?showArchived stay as they were. A 400 puts the server's errors under each field.
// A named function so the error toast's Retry button can call it again.
// FormData is read on each call, so a retry sends whatever is in the form now.
async function submitModalForm(form) {
    let response = null;
    try {
        response = await fetch(form.action, {
            method: "POST",
            body: new FormData(form),
            headers: { "X-Requested-With": "XMLHttpRequest" }
        });
    } catch {
        // fetch only throws when no response arrives at all (offline, server down).
        // Left as null, so it falls through to the error toast below.
    }

    if (response?.ok) {
        window.location.reload();
        return;
    }

    if (response?.status === 400) {
        const result = await response.json();
        // Errors for a field with no message spot (e.g. the hidden Id) go in a toast instead,
        // so the user never gets a modal that silently won't save.
        const unshown = [];

        for (const [field, messages] of Object.entries(result.errors)) {
            const input = form.querySelector(`[name="${field}"]`);
            input?.classList.add("input-validation-error");

            const messageElement =
                form.querySelector(`[data-valmsg-for="${field}"]`);

            if (messageElement) {
                messageElement.textContent = messages.join(" ");
            } else {
                unshown.push(...messages);
            }
        }

        if (unshown.length > 0) {
            showToast({ kind: "error", title: "Changes not saved", message: unshown.join(" ") });
        }
        return;
    }

    // Anything else (404, 500, network failure): nothing was saved that we know of.
    showToast({
        kind: "error",
        title: "Could not save changes",
        message: `${form.dataset.notSaved} Check your connection and try again.`,
        actionLabel: "Retry",
        onAction: () => submitModalForm(form)
    });
}

// Trash page: the type pills (All / Projects / Tasks), the search box and "Sort by".
// Each row carries data-trash-item="project|task", data-trash-name and data-trash-date (ISO date).
function setUpTrashFilters() {
    const filters = document.querySelectorAll("[data-trash-filter]");
    const search = document.querySelector("[data-trash-search]");
    const sortSelect = document.querySelector("[data-trash-sort]");
    const items = document.querySelectorAll("[data-trash-item]");
    const summary = document.querySelector("[data-trash-summary]");
    const table = document.getElementById("trash-table");
    const noResults = document.getElementById("trash-no-results");
    const clearButtons = document.querySelectorAll("[data-trash-clear-filters]");
    // Not on the Trash page, or the trash is empty (the server shows its own empty state then).
    if (!search || !sortSelect || !summary || !table || !noResults) return;

    let activeFilter = "all";

    const matchesType = (item, type) => type === "all" || item.dataset.trashItem === type;
    const matchesSearch = (item, searchText) => item.textContent.toLowerCase().includes(searchText);

    function filterItems() {
        const searchText = search.value.trim().toLowerCase();

        items.forEach(item => {
            item.hidden = !(matchesType(item, activeFilter) && matchesSearch(item, searchText));
        });

        // Each pill counts the rows it would show with the current search.
        filters.forEach(button => {
            const count = [...items].filter(item =>
                matchesType(item, button.dataset.trashFilter) && matchesSearch(item, searchText)).length;
            button.querySelector(".pill-count").textContent = String(count);
        });

        // Same "Showing X of Y" line and empty card as the Tasks page.
        const shown = [...items].filter(item => !item.hidden).length;
        table.hidden = shown === 0;
        noResults.hidden = shown > 0;
        summary.textContent = `Showing ${shown} of ${items.length} items`;
    }

    function setFilter(type) {
        activeFilter = type;
        filters.forEach(button => {
            const isActive = button.dataset.trashFilter === type;
            button.classList.toggle("is-active", isActive);
            button.setAttribute("aria-pressed", String(isActive));
        });
        filterItems();
    }

    const byName = (a, b) => compareText(a.dataset.trashName, b.dataset.trashName);
    // data-trash-date is an ISO date string, so plain text order is date order.
    const sorters = {
        newest: (a, b) => b.dataset.trashDate.localeCompare(a.dataset.trashDate) || byName(a, b),
        oldest: (a, b) => a.dataset.trashDate.localeCompare(b.dataset.trashDate) || byName(a, b),
        name: byName
    };

    function sortItems() {
        const sorter = sorters[sortSelect.value] ?? sorters.newest;
        // append() moves the existing rows, so the header row stays first.
        items[0]?.parentElement.append(...[...items].sort(sorter));
    }

    filters.forEach(button => {
        button.addEventListener("click", () => setFilter(button.dataset.trashFilter));
    });

    // Back to the starting view: all items, no search.
    clearButtons.forEach(button => {
        button.addEventListener("click", () => {
            search.value = "";
            setFilter("all");
            search.focus();
        });
    });

    search.addEventListener("input", filterItems);
    sortSelect.addEventListener("change", sortItems);

    filterItems(); // fill in the summary line on load
}

// Styled confirmations (_ConfirmDialog), instead of submitting a form right away:
//   <form data-confirm-trash="Project name">       the "Move project to Trash?" dialog from _Layout
//   <form data-confirm-task-trash="Task name">     the "Move task to Trash?" dialog from _Layout
//   <form data-confirm-purge="Item name">          the "Delete forever?" dialog on the Trash page
// The attribute's value fills every [data-confirm-name] in the dialog. Any of these forms can add
// data-trash-detail="..." for the grey text in the summary row.
//
// idPrefix:      the dialog's element ids are "<idPrefix>-dialog", "-detail", "-ok", "-cancel".
// formAttribute: the data attribute (as written in HTML) that marks forms for this dialog.
function setUpConfirmDialog(idPrefix, formAttribute) {
    const dialog = document.getElementById(`${idPrefix}-dialog`);
    if (!dialog) return;

    // "data-confirm-task-trash" -> "confirmTaskTrash", the name dataset uses.
    const datasetKey = formAttribute.slice("data-".length)
        .replace(/-./g, match => match[1].toUpperCase());
    const nameElements = dialog.querySelectorAll("[data-confirm-name]");
    const detailElement = document.getElementById(`${idPrefix}-detail`);
    let pendingForm = null;

    // One delegated listener covers every matching form on the page.
    document.addEventListener("submit", event => {
        const form = event.target.closest?.(`form[${formAttribute}]`);
        if (!form) return;

        event.preventDefault();
        pendingForm = form;
        // textContent (not innerHTML) so an item name can never inject markup.
        nameElements.forEach(element => element.textContent = form.dataset[datasetKey]);
        detailElement.textContent = form.dataset.trashDetail ?? "";
        dialog.showModal();
    });

    document.getElementById(`${idPrefix}-ok`).addEventListener("click", () => {
        const form = pendingForm;
        pendingForm = null;
        dialog.close();
        // form.submit() skips the submit event, so this doesn't re-trigger the dialog.
        form?.submit();
    });

    document.getElementById(`${idPrefix}-cancel`).addEventListener("click", () => dialog.close());
    // Escape and clicking Cancel both end up here; forget the form either way.
    dialog.addEventListener("close", () => { pendingForm = null; });
}

// "Empty Trash" confirmation (Trash page only). The confirm button is disabled until the
// checkbox is ticked, so a stray click can't hard delete everything.
function setUpEmptyTrashDialog() {
    const openButton = document.getElementById("empty-trash-open");
    const dialog = document.getElementById("empty-trash-dialog");
    if (!openButton || !dialog) return;

    const check = document.getElementById("empty-trash-check");
    const okButton = document.getElementById("empty-trash-ok");
    const detail = document.getElementById("empty-trash-detail");
    const plural = (count, word) => `${count} ${word}${count === 1 ? "" : "s"}`;

    openButton.addEventListener("click", () => {
        detail.textContent =
            `${plural(openButton.dataset.projects, "project")}, ${plural(openButton.dataset.tasks, "task")}`;
        dialog.showModal();
    });

    check.addEventListener("change", () => { okButton.disabled = !check.checked; });

    // Reset on every close (Cancel, Escape, or confirm) so it always reopens locked.
    dialog.addEventListener("close", () => {
        check.checked = false;
        okButton.disabled = true;
    });

    document.getElementById("empty-trash-cancel").addEventListener("click", () => dialog.close());
    // No click handler for the confirm button: it is a submit button inside the dialog's form,
    // so the browser posts to TrashController.EmptyTrash once it is enabled.
}

// Click outside a popup to close it.
// - <details> menus (the account menu and each task's "..." menu): a click anywhere outside an open
//   menu closes it, and so does Esc. Opening a second menu counts as outside the first, so only one
//   stays open. querySelectorAll runs on each click, so it also covers menus added to the page later.
// - Modals (<dialog class="modal">): a click on the dark backdrop closes it, the same as Cancel or Esc.
//   Every modal already cleans up on "close" (form reset, pending form cleared), so nothing else changes.
function setUpClickOutsideToClose() {
    const openMenus = () => document.querySelectorAll(
        "details.user-menu[open], details.task-card-menu[open], details.filter-panel[open]");

    document.addEventListener("click", event => {
        openMenus().forEach(menu => {
            if (!menu.contains(event.target)) menu.open = false;
        });
    });

    document.addEventListener("keydown", event => {
        if (event.key !== "Escape") return;
        openMenus().forEach(menu => {
            menu.open = false;
            menu.querySelector("summary")?.focus();
        });
    });

    // A click on a modal's ::backdrop is reported with the <dialog> itself as the target. The x/y check
    // makes sure it really was outside the box, not on a gap between the dialog's children.
    function isOnBackdrop(event) {
        const dialog = event.target;
        if (!(dialog instanceof HTMLDialogElement) || !dialog.matches("dialog.modal[open]")) return false;

        const box = dialog.getBoundingClientRect();
        return event.clientX < box.left || event.clientX > box.right ||
               event.clientY < box.top || event.clientY > box.bottom;
    }

    // Only close when the press also started on the backdrop. Otherwise selecting text in an input
    // and letting go of the mouse outside the box would close the modal and lose what was typed.
    let pressStartedOnBackdrop = false;
    document.addEventListener("pointerdown", event => { pressStartedOnBackdrop = isOnBackdrop(event); });
    document.addEventListener("click", event => {
        if (pressStartedOnBackdrop && isOnBackdrop(event)) event.target.close();
        pressStartedOnBackdrop = false;
    });
}

// Tasks page: each row's "..." menu. The table scrolls sideways on small screens (.data-scroll has
// overflow-x: auto), and a scroll container clips in both directions, so the menu of the last rows was
// cut off at the bottom of the table. While open, the panel is position: fixed, placed from the
// button's position on screen, which takes it out of the table's clipping. A fixed panel wouldn't move
// with the page, so scrolling or resizing closes the menu instead.
function setUpRowMenus() {
    const menus = document.querySelectorAll("details.task-row-menu");
    if (menus.length === 0) return;

    const GAP = 4; // px between the button and the panel, same as the CSS (top: calc(100% + 4px))

    function place(menu, panel) {
        const button = menu.querySelector("summary").getBoundingClientRect();
        panel.style.position = "fixed";
        // clientWidth (not innerWidth) leaves out the scrollbar, which is what `right` is measured from.
        panel.style.right = `${document.documentElement.clientWidth - button.right}px`;

        // Below the button, or above it when there isn't room below (e.g. the last row on screen).
        const fitsBelow = button.bottom + GAP + panel.offsetHeight <= window.innerHeight;
        panel.style.top = fitsBelow
            ? `${button.bottom + GAP}px`
            : `${button.top - GAP - panel.offsetHeight}px`;
    }

    menus.forEach(menu => {
        const panel = menu.querySelector(".task-card-menu-panel");
        menu.addEventListener("toggle", () => {
            if (menu.open) place(menu, panel);
            else panel.removeAttribute("style"); // back to the CSS position for next time
        });
    });

    // Capture (the `true`) so scrolling inside the table, which doesn't bubble, also counts.
    const closeAll = () => menus.forEach(menu => { menu.open = false; });
    window.addEventListener("scroll", closeAll, true);
    window.addEventListener("resize", closeAll);
}

// ---------- Toasts ----------
// showToast({ kind, title, message, actionLabel, onAction })
//   kind:        "success" (green), "info" (blue) or "error" (rose)
//   actionLabel: optional button text, e.g. "Undo" or "Retry"; onAction runs when it is clicked.
// Toasts close after 5 seconds (paused while hovered or focused). At most 3 are shown;
// a 4th pushes out the oldest.
const MAX_TOASTS = 3;
const TOAST_DURATION_MS = 5000;

const TOAST_ICONS = {
    success: '<path d="M5 12l5 5 9-10"></path>',
    info: '<path d="M3 12a9 9 0 1 0 3-6.7L3 8M3 3v5h5"></path>',
    error: '<path d="M12 8v5M12 17h.01"></path><circle cx="12" cy="12" r="9"></circle>'
};

function showToast({ kind = "success", title, message, actionLabel, onAction }) {
    const container = document.getElementById("toasts");
    if (!container) return;

    // A modal <dialog> (opened with showModal) sits above everything and makes the rest of the
    // page unclickable, so toasts outside it would be hidden behind its backdrop. Put the stack
    // inside the open modal instead; position: fixed keeps it in the screen corner.
    (document.querySelector("dialog:modal") ?? document.body).append(container);

    while (container.children.length >= MAX_TOASTS) {
        container.firstElementChild.remove();
    }

    const toast = document.createElement("div");
    toast.className = `toast toast-${kind}`;
    // Errors interrupt the screen reader; the others wait politely (aria-live on the container).
    if (kind === "error") toast.setAttribute("role", "alert");

    // Only fixed markup goes through innerHTML; title/message are set with textContent below
    // so an item name can never inject markup.
    toast.innerHTML = `
        <span class="toast-icon" aria-hidden="true">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"
                 stroke-linecap="round" stroke-linejoin="round">${TOAST_ICONS[kind] ?? TOAST_ICONS.success}</svg>
        </span>
        <div class="toast-body">
            <p class="toast-title"></p>
            <p class="muted small toast-message"></p>
        </div>
        <button type="button" class="btn btn-text toast-action"></button>
        <button type="button" class="icon-btn borderless toast-close" aria-label="Dismiss notification">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
                 stroke-linecap="round" aria-hidden="true"><path d="M6 6l12 12M18 6L6 18"></path></svg>
        </button>`;

    toast.querySelector(".toast-title").textContent = title;

    const messageElement = toast.querySelector(".toast-message");
    if (message) messageElement.textContent = message;
    else messageElement.remove();

    const actionButton = toast.querySelector(".toast-action");
    if (actionLabel && onAction) {
        actionButton.textContent = actionLabel;
        actionButton.addEventListener("click", () => {
            dismiss();
            onAction();
        });
    } else {
        actionButton.remove();
    }

    toast.querySelector(".toast-close").addEventListener("click", dismiss);

    // Pause while the user is reading or about to click, so the toast can't vanish under the cursor.
    let timer;
    const startTimer = () => { timer = setTimeout(dismiss, TOAST_DURATION_MS); };
    const stopTimer = () => clearTimeout(timer);
    toast.addEventListener("mouseenter", stopTimer);
    toast.addEventListener("mouseleave", startTimer);
    toast.addEventListener("focusin", stopTimer);
    toast.addEventListener("focusout", startTimer);

    function dismiss() {
        stopTimer();
        toast.remove();
    }

    container.append(toast);
    startTimer();
}

// When a dialog closes, move the toast stack back to the page so open toasts don't disappear
// with the dialog. "close" doesn't bubble, so listen in the capture phase (the `true`).
document.addEventListener("close", () => {
    const container = document.getElementById("toasts");
    if (container && container.parentElement !== document.body) document.body.append(container);
}, true);

// The toasts (a list) the server set before redirecting (see ToastExtensions and _Layout).
function showServerToasts() {
    const data = document.getElementById("server-toasts");
    if (!data) return;

    for (const { actionUrl, ...toast } of JSON.parse(data.textContent)) {
        if (actionUrl) {
            // A POST, not a link, because the action changes data (e.g. Undo restores an item).
            const form = document.getElementById("server-toast-form");
            toast.onAction = () => {
                form.action = actionUrl;
                form.submit();
            };
        }

        showToast(toast);
    }
}

// Instant "this field is required" messages for a <form data-check-required>, without a page reload.
// asp-for already writes the [Required] message into each input's data-val-required attribute,
// so the text stays in one place (the view model). The server still validates: this is only a shortcut.
// Every form with fields the user types in pairs data-check-required with novalidate, which turns off the
// browser's own popups (e.g. for type="email") so these messages are the only ones shown.
function setUpRequiredFieldChecks() {
    for (const form of document.querySelectorAll("form[data-check-required]")) {
        // Hidden inputs are skipped: the user can't fill them in, so a message for one would never show
        // (e.g. Code and Email on ResetPassword). The server reports those.
        const inputs = form.querySelectorAll("[data-val-required]:not([type='hidden'])");

        function setFieldError(input, message) {
            input.classList.toggle("input-validation-error", Boolean(message));

            const messageElement = form.querySelector(`[data-valmsg-for="${input.name}"]`);
            if (!messageElement) return;
            messageElement.textContent = message;
            messageElement.classList.toggle("field-validation-error", Boolean(message));
        }

        // Capture phase (the `true`) so this runs before any other submit listener on the form,
        // such as the fetch-based one on the create forms. stopImmediatePropagation then
        // keeps those from sending a form we already know is incomplete.
        form.addEventListener("submit", event => {
            const emptyInputs = [...inputs].filter(input => input.value.trim() === "");
            if (emptyInputs.length === 0) return;

            event.preventDefault();
            event.stopImmediatePropagation();
            emptyInputs.forEach(input => setFieldError(input, input.dataset.valRequired));
            emptyInputs[0].focus();
        }, true);

        // Clear a field's message as soon as the user starts fixing it.
        inputs.forEach(input => input.addEventListener("input", () => setFieldError(input, "")));

        // A modal keeps its errors and typed text in the page while it is closed, so clean up on close
        // (Cancel, the X button and Esc all fire "close"). Otherwise they show up again next time it opens.
        // reset() puts each field back to the value the server rendered: empty/defaults for a create
        // form, the saved values for an edit form.
        // The message clearing covers every message in the form, not just the required ones, so errors the
        // server sent back (see submitModalForm) go away too.
        form.closest("dialog")?.addEventListener("close", () => {
            form.reset();
            form.querySelectorAll(".input-validation-error")
                .forEach(input => input.classList.remove("input-validation-error"));
            form.querySelectorAll("[data-valmsg-for]").forEach(messageElement => {
                messageElement.textContent = "";
                messageElement.classList.remove("field-validation-error");
            });
        });
    }
}

// Opening and closing modals, with data attributes instead of inline onclick:
//   <button data-open-modal="create-task-modal">  opens the <dialog> with that id
//   <button data-close-modal>                      closes the <dialog> it sits in (Cancel and the X, see _ModalCloseButton)
// One delegated listener (same idea as setUpConfirmDialog) covers every button on the page.
function setUpModalButtons() {
    document.addEventListener("click", event => {
        const openButton = event.target.closest?.("[data-open-modal]");
        if (openButton) {
            document.getElementById(openButton.dataset.openModal)?.showModal();
            return;
        }

        event.target.closest?.("[data-close-modal]")?.closest("dialog")?.close();
    });
}

// <input data-submit-on-change> sends its form as soon as it changes (a task's done checkbox,
// "Include archived projects"), instead of an inline onchange. One delegated listener covers them all.
// requestSubmit() fires the submit event like a real button press, so the other submit listeners
// (the confirm dialogs, data-check-required) still get their say; they only act on forms that carry
// their own attribute. submit() is the fallback for old browsers without requestSubmit.
function setUpSubmitOnChange() {
    document.addEventListener("change", event => {
        // closest("form") also works if the attribute ends up on the <form> itself.
        const form = event.target.closest?.("[data-submit-on-change]")?.closest("form");
        if (!form) return;

        if (form.requestSubmit) form.requestSubmit();
        else form.submit();
    });
}

// <button data-history-back> goes back one page, like the browser's Back button (the error page's
// "Go back"), instead of an inline onclick.
function setUpHistoryBack() {
    document.addEventListener("click", event => {
        if (event.target.closest?.("[data-history-back]")) history.back();
    });
}

// A board lane's "+ Add task" button (<button data-add-task-status="1">) opens the create-task modal
// with Status already set to that lane's status. The number is the TaskStatus enum value, which is
// also what the Status <select> options use. The header's "New task" button has no attribute, so it
// opens the modal with the default status (closing the modal resets the form, see setUpRequiredFieldChecks).
function setUpAddTaskButtons() {
    const modal = document.getElementById("create-task-modal");
    const statusSelect = modal?.querySelector("[name='Status']");
    if (!modal || !statusSelect) return;

    document.querySelectorAll("[data-add-task-status]").forEach(button => {
        button.addEventListener("click", () => {
            statusSelect.value = button.dataset.addTaskStatus;
            modal.showModal();
        });
    });
}

// ---------- Remembered filters ----------
// The Projects and Tasks filters run in the browser, so a reload (e.g. after ticking a task's checkbox)
// would reset them. Each page saves its filter values here and puts them back on load.
// sessionStorage (not localStorage): kept through reloads and Back in this tab, gone when the tab closes,
// so a fresh visit starts unfiltered. Storage can throw (blocked, private mode), so it never breaks the filters.
function loadSavedFilters(key) {
    try {
        return JSON.parse(sessionStorage.getItem(key)) ?? {};
    } catch {
        return {};
    }
}

function saveFilters(key, values) {
    try {
        sessionStorage.setItem(key, JSON.stringify(values));
    } catch {
        // Not saving is fine: the filters just reset on the next reload.
    }
}

// Only restore values the control still offers (e.g. a project that was since deleted is skipped),
// otherwise a <select> would end up blank.
function restoreControl(control, value) {
    if (typeof value !== "string") return;
    if (control instanceof HTMLSelectElement && ![...control.options].some(option => option.value === value)) return;
    control.value = value;
}

// Small helpers for the Filters panels (Tasks and Projects pages). The panel's checkboxes and
// radio buttons are the filter state, so these just read and set them.
const checkedValues = boxes => boxes.filter(box => box.checked).map(box => box.value);
const checkedLabels = boxes => boxes.filter(box => box.checked).map(box => box.closest("label").textContent.trim());
const setChecked = (boxes, values) => boxes.forEach(box => { box.checked = values.includes(box.value); });
const sameValues = (a, b) => a.length === b.length && a.every(value => b.includes(value));

// One removable chip ("Status: Done ×") for the filter row. filter = { name, value }; onRemove runs on ×.
// createElement + textContent (not innerHTML), so a project name can never be read as HTML.
function makeFilterChip(filter, onRemove) {
    const chip = document.createElement("span");
    chip.className = "filter-chip";

    const name = document.createElement("span");
    name.className = "muted";
    name.textContent = `${filter.name}:`;

    const remove = document.createElement("button");
    remove.type = "button";
    remove.className = "filter-chip-remove";
    remove.setAttribute("aria-label", `Remove ${filter.name} filter`);
    remove.textContent = "×";
    remove.addEventListener("click", onRemove);

    chip.append(name, ` ${filter.value}`, remove);
    return chip;
}

// The Due filter on the Projects and Tasks pages. "group" is the server's lowercase DueGroup
// (overdue / today / thisweek / later / finished), so both pages use the server's one rule.
// "due" is the chosen radio value. "finished" only matches "any": a done item is never overdue or due soon.
function matchesDueGroup(group, due) {
    if (due === "any") return true;
    if (due === "week") return group === "today" || group === "thisweek";
    return group === due;
}

// The filters shared by the Projects and Tasks pages: quick views, search box, the Filters panel
// (Status / Due, plus any optional checkbox groups), the removable chips, "Clear all", the
// "Showing X of Y" line, and the Sort by / Group by select. setUpProjectFilters and setUpTaskFilters
// below only add what differs between the two pages.
//
// The markup uses the page's prefix ("project" or "task") for everything that belongs to that page:
//   data-<prefix>-view                    a quick view button (data-view-statuses / data-view-due = its preset)
//   data-<prefix>-status-option           a Status checkbox
//   data-<prefix>-due-option              a Due radio button
//   data-<prefix>-search                  the search box
//   data-<prefix>-clear-filters           Reset, Clear all and the no-results Clear filters button
//   id="<prefix>-no-results"              the _NoResults box
// and on each item:
//   data-<prefix>-item="<status>"         lowercase status: what the Status checkboxes match
//   data-<prefix>-due="<group>"           lowercase DueGroup: what the Due options match (matchesDueGroup)
//   data-<prefix>-text="<name> <description>"   what the search box looks at
// The panel's checkboxes and radio buttons ARE the filter state. Quick views just tick a preset set of
// them, so a quick view and the panel can never disagree.
//
// options:
//   prefix          "project" or "task" (see above)
//   noun            "project" or "task", for "Showing 3 of 5 projects" and "Show 3 projects"
//   items           every element to filter
//   countedItems    the ones to count (defaults to items)
//   optionalGroups  extra checkbox groups where none ticked means "any":
//                   [{ name: "Priority", boxes, itemKey: "taskPriority", savedKey: "priorities" }]
//                   itemKey is the dataset key on each item the box values are compared with.
//   orderSelect     the Sort by / Group by <select>
//   applyOrder      reorders the items for orderSelect.value; runs after every filter change
//   resultsElement  optional: hidden when nothing matches (the Tasks table)
//   serverFilters   optional: how many filters the server drew itself (the archived chip on Tasks)
function setUpFilters({
    prefix, noun, items, countedItems = items, optionalGroups = [],
    orderSelect, applyOrder, resultsElement = null, serverFilters = 0
}) {
    const panel = document.querySelector("details.filter-panel");
    const searchBox = document.querySelector(`[data-${prefix}-search]`);
    const noResults = document.getElementById(`${prefix}-no-results`);
    const chipList = document.querySelector("[data-filter-chips]");
    if (!panel || !searchBox || !noResults || !chipList || !orderSelect) return;

    const viewButtons = [...document.querySelectorAll(`[data-${prefix}-view]`)];
    const statusBoxes = [...document.querySelectorAll(`[data-${prefix}-status-option]`)];
    const dueRadios = [...document.querySelectorAll(`[data-${prefix}-due-option]`)];
    const clearButtons = document.querySelectorAll(`[data-${prefix}-clear-filters]`);
    const clearAllButton = document.querySelector("[data-clear-all]");
    const closeButton = document.querySelector("[data-close-filters]");
    const filterCount = document.querySelector("[data-filter-count]");
    const shownSummary = document.querySelector("[data-shown-summary]");

    // "project" -> dataset.projectItem, dataset.projectDue, dataset.projectText.
    const statusKey = `${prefix}Item`;
    const dueKey = `${prefix}Due`;
    const textKey = `${prefix}Text`;
    const FILTERS_KEY = `${prefix}Filters`;

    // The statuses the server ticked (the open ones): the default view, and what Clear all goes back to.
    // Read from the markup so the list lives in one place (the page's Index.cshtml).
    const defaultStatuses = statusBoxes.filter(box => box.defaultChecked).map(box => box.value);
    const dueValue = () => dueRadios.find(radio => radio.checked)?.value ?? "any";
    const setDue = value => dueRadios.forEach(radio => { radio.checked = radio.value === value; });
    const clearOptionalGroups = () => optionalGroups.forEach(group => setChecked(group.boxes, []));

    // A chip for every filter that differs from the default, each knowing how to undo itself.
    function activeFilters() {
        const filters = [];
        if (!sameValues(checkedValues(statusBoxes), defaultStatuses)) {
            filters.push({
                name: "Status",
                value: checkedLabels(statusBoxes).join(", ") || "None",
                reset: () => setChecked(statusBoxes, defaultStatuses)
            });
        }
        if (dueValue() !== "any") {
            filters.push({ name: "Due", value: checkedLabels(dueRadios).join(""), reset: () => setDue("any") });
        }
        for (const group of optionalGroups) {
            if (checkedValues(group.boxes).length > 0) {
                filters.push({
                    name: group.name,
                    value: checkedLabels(group.boxes).join(", "),
                    reset: () => setChecked(group.boxes, [])
                });
            }
        }
        return filters;
    }

    function renderChips(filters) {
        chipList.replaceChildren(...filters.map(filter => makeFilterChip(filter, () => {
            filter.reset();
            applyFilters();
            // The chip (and the focused button) is gone now, so put focus somewhere sensible.
            panel.querySelector("summary").focus();
        })));

        clearAllButton.hidden = filters.length === 0;
        const total = filters.length + serverFilters;
        filterCount.textContent = String(total);
        filterCount.hidden = total === 0;
    }

    // A quick view is highlighted only when the panel holds exactly its preset.
    function highlightQuickView() {
        const statuses = checkedValues(statusBoxes);
        const noOptionalFilters = optionalGroups.every(group => checkedValues(group.boxes).length === 0);
        viewButtons.forEach(button => {
            const isActive = noOptionalFilters &&
                dueValue() === button.dataset.viewDue &&
                sameValues(statuses, button.dataset.viewStatuses.split(","));
            button.classList.toggle("is-active", isActive);
            button.setAttribute("aria-pressed", String(isActive));
        });
    }

    function applyFilters() {
        const statuses = checkedValues(statusBoxes);
        const due = dueValue();
        const searchText = searchBox.value.trim().toLowerCase();
        const optionalValues = optionalGroups.map(group => checkedValues(group.boxes));

        items.forEach(item => {
            item.hidden = !(
                statuses.includes(item.dataset[statusKey]) &&
                matchesDueGroup(item.dataset[dueKey], due) &&
                item.dataset[textKey].toLowerCase().includes(searchText) &&
                // None ticked = any.
                optionalGroups.every((group, i) =>
                    optionalValues[i].length === 0 || optionalValues[i].includes(item.dataset[group.itemKey])));
        });

        applyOrder(orderSelect.value);
        highlightQuickView();
        renderChips(activeFilters());

        const shown = countedItems.filter(item => !item.hidden).length;
        if (resultsElement) resultsElement.hidden = shown === 0;
        noResults.hidden = shown > 0;
        shownSummary.textContent = `Showing ${shown} of ${countedItems.length} ${noun}s`;
        closeButton.textContent = shown === 1 ? `Show 1 ${noun}` : `Show ${shown} ${noun}s`;

        const saved = { statuses, due, order: orderSelect.value, search: searchBox.value };
        optionalGroups.forEach((group, i) => { saved[group.savedKey] = optionalValues[i]; });
        saveFilters(FILTERS_KEY, saved);
    }

    viewButtons.forEach(button => {
        button.addEventListener("click", () => {
            setChecked(statusBoxes, button.dataset.viewStatuses.split(","));
            clearOptionalGroups();
            setDue(button.dataset.viewDue);
            applyFilters();
        });
    });

    // One listener for every checkbox and radio button in the panel ("change" bubbles up).
    // On Tasks the archived checkbox also lands here, but it reloads the page right after, so that's harmless.
    panel.addEventListener("change", applyFilters);
    orderSelect.addEventListener("change", applyFilters);
    searchBox.addEventListener("input", applyFilters);

    closeButton.addEventListener("click", () => {
        panel.open = false;
        panel.querySelector("summary").focus();
    });

    // Back to the default view. Sort by / Group by is a view choice, not a filter, so it stays.
    // (On Tasks, archived projects aren't touched either: that one reloads the page and has its own chip.)
    clearButtons.forEach(button => {
        button.addEventListener("click", () => {
            setChecked(statusBoxes, defaultStatuses);
            clearOptionalGroups();
            setDue("any");
            searchBox.value = "";
            applyFilters();
        });
    });

    // Put back what was chosen before a reload (see loadSavedFilters). The Array checks skip values
    // saved by the old filter bar, which stored single strings. Values that no longer exist (e.g. the
    // id of a since-deleted project) simply match no checkbox.
    const saved = loadSavedFilters(FILTERS_KEY);
    if (Array.isArray(saved.statuses)) setChecked(statusBoxes, saved.statuses);
    for (const group of optionalGroups) {
        if (Array.isArray(saved[group.savedKey])) setChecked(group.boxes, saved[group.savedKey]);
    }
    if (dueRadios.some(radio => radio.value === saved.due)) setDue(saved.due);
    restoreControl(orderSelect, saved.order);
    restoreControl(searchBox, saved.search);

    applyFilters();
}

// Projects page: the shared filters (setUpFilters) plus "Sort by". Every project is drawn twice, as a
// grid card and a list row, and both carry the data-project-* attributes, so one pass hides, shows or
// reorders both views and they always agree. "Sort by" orders on
//   data-project-name / -date (yyyy-MM-dd) / -progress (0-100)
function setUpProjectFilters() {
    const items = [...document.querySelectorAll("[data-project-item]")];

    const byName = (a, b) => compareText(a.dataset.projectName, b.dataset.projectName);

    // Each sorter is a compare function. A yyyy-MM-dd string sorts correctly as plain text.
    const sorters = {
        // Finished projects last (same rule as the server's default order), then soonest due first.
        due: (a, b) =>
            (a.dataset.projectDue === "finished") - (b.dataset.projectDue === "finished") ||
            a.dataset.projectDate.localeCompare(b.dataset.projectDate) ||
            byName(a, b),
        name: byName,
        // Most complete first.
        progress: (a, b) =>
            Number(b.dataset.projectProgress) - Number(a.dataset.projectProgress) || byName(a, b)
    };

    // The grid cards and the list rows have different parents, so sort each parent's items on
    // its own. append() moves existing elements, so the list's header row stays first.
    function applySort(sortBy) {
        const sorter = sorters[sortBy] ?? sorters.due;
        for (const parent of new Set(items.map(item => item.parentElement))) {
            const sorted = [...parent.querySelectorAll(":scope > [data-project-item]")].sort(sorter);
            parent.append(...sorted);
        }
    }

    setUpFilters({
        prefix: "project",
        noun: "project",
        items,
        // Every project is drawn twice (grid card + list row), so count from the grid only.
        countedItems: [...document.querySelectorAll("#project-grid > [data-project-item]")],
        orderSelect: document.querySelector("[data-project-sort]"),
        applyOrder: applySort
    });
}

// Tasks page: the shared filters (setUpFilters) plus the Priority and Project checkbox groups and
// "Group by". Besides the shared attributes, every task row carries
//   data-task-priority="<TaskPriority>"  what the Priority checkboxes match
//   data-task-project-id="<id>"          what the Project checkboxes match
//   data-task-due-label / -due-color / -due-order, -project-name, -priority-rank, -date
//                                        what "Group by" sorts and labels on
// ("Include archived projects" is not handled here: it reloads the page, because the server leaves those tasks out.)
function setUpTaskFilters() {
    const rows = [...document.querySelectorAll("[data-task-item]")];
    const table = document.getElementById("task-table");
    const tableBody = table?.querySelector(".data-table");
    // Not on the Tasks page, or it has no tasks at all (the server shows its own empty state then).
    if (!tableBody) return;

    // For each "Group by" option: how to order rows, and which header a row goes under
    // (label: null means no headers). Array.sort is stable, and we always start from the
    // server's order (due date, then name), so rows within a group stay in that order.
    const groupings = {
        due: {
            compare: (a, b) => Number(a.dataset.taskDueOrder) - Number(b.dataset.taskDueOrder),
            label: row => row.dataset.taskDueLabel,
            color: row => row.dataset.taskDueColor
        },
        project: {
            compare: (a, b) => compareText(a.dataset.taskProjectName, b.dataset.taskProjectName),
            label: row => row.dataset.taskProjectName
        },
        priority: {
            // Critical first.
            compare: (a, b) => Number(b.dataset.taskPriorityRank) - Number(a.dataset.taskPriorityRank),
            label: row => row.dataset.taskPriority
        },
        none: {
            // A yyyy-MM-dd string sorts correctly as plain text.
            compare: (a, b) => a.dataset.taskDate.localeCompare(b.dataset.taskDate),
            label: null
        }
    };

    function makeGroupHeader(label, color) {
        // textContent (not innerHTML), so a project name can never be read as HTML.
        const header = document.createElement("div");
        header.className = "group-header";
        const title = document.createElement("span");
        title.textContent = label;
        if (color) title.style.color = color;
        const count = document.createElement("span");
        count.className = "badge badge-muted";
        header.append(title, count);
        return { header, count };
    }

    // Re-sorts the rows and rebuilds the group headers, with counts of the rows still shown.
    function renderGroups(groupBy) {
        tableBody.querySelectorAll(".group-header").forEach(header => header.remove());

        const grouping = groupings[groupBy] ?? groupings.due;
        const sorted = [...rows].sort(grouping.compare);

        let currentLabel = null;
        let currentCount = null;
        let shownInGroup = 0;

        for (const row of sorted) {
            // append() moves the existing row, so the table's header row stays first.
            tableBody.append(row);
            if (!grouping.label || row.hidden) continue;

            const label = grouping.label(row);
            if (label !== currentLabel) {
                const { header, count } = makeGroupHeader(label, grouping.color?.(row));
                row.before(header);
                currentLabel = label;
                currentCount = count;
                shownInGroup = 0;
            }
            currentCount.textContent = String(++shownInGroup);
        }
    }

    setUpFilters({
        prefix: "task",
        noun: "task",
        items: rows,
        optionalGroups: [
            {
                name: "Priority",
                boxes: [...document.querySelectorAll("[data-task-priority-option]")],
                itemKey: "taskPriority",
                savedKey: "priorities"
            },
            {
                name: "Project",
                boxes: [...document.querySelectorAll("[data-task-project-option]")],
                itemKey: "taskProjectId",
                savedKey: "projectIds"
            }
        ],
        orderSelect: document.querySelector("[data-task-group-by]"),
        applyOrder: renderGroups,
        resultsElement: table,
        // The archived chip is drawn by the server, but it is still a filter that is on.
        serverFilters: document.querySelector("[data-archived-chip]") ? 1 : 0
    });
}

setUpThemeToggle();
setUpProjectViewToggle();
setUpModalForms();
showServerToasts();
setUpClickOutsideToClose();
setUpRowMenus();
setUpRequiredFieldChecks();
setUpModalButtons();
setUpSubmitOnChange();
setUpHistoryBack();
setUpAddTaskButtons();
setUpProjectFilters();
setUpTaskFilters();
setUpTrashFilters();
setUpEmptyTrashDialog();
setUpConfirmDialog("confirm-trash", "data-confirm-trash");
setUpConfirmDialog("confirm-task-trash", "data-confirm-task-trash");
setUpConfirmDialog("confirm-purge", "data-confirm-purge");
