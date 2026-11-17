(() => {
    const root = document.documentElement;
    const themeToggle = document.getElementById("theme-toggle");

    if (!themeToggle) return;

    const savedTheme = localStorage.getItem("theme");
    const initialTheme = savedTheme === "light" ? "light" : "dark";

    function setTheme(theme) {
        root.setAttribute("data-bs-theme", theme);
        themeToggle.setAttribute(
            "aria-label",
            `Switch to ${theme === "dark" ? "light" : "dark"} mode`
        );
    }

    setTheme(initialTheme);

    themeToggle.addEventListener("click", () => {
        const nextTheme =
            root.getAttribute("data-bs-theme") === "dark" ? "light" : "dark";

        setTheme(nextTheme);
        localStorage.setItem("theme", nextTheme);
    });

        document.addEventListener("DOMContentLoaded", () => {
            const gridButton = document.getElementById("grid-view");
            const listButton = document.getElementById("list-view");
            const grid = document.getElementById("project-grid");
            const list = document.getElementById("project-list");
        
            if (!gridButton || !listButton || !grid || !list) return;

            const VIEW_KEY = "projectView";

            // localStorage can throw (blocked storage, private mode), so never let it break the toggle.
            function loadView() {
                try {
                    const saved = localStorage.getItem(VIEW_KEY);
                    return saved === "list" ? "list" : "grid"; // only trust known values
                } catch {
                    return "grid";
                }
            }

            function saveView(view) {
                try {
                    localStorage.setItem(VIEW_KEY, view);
                } catch {
                    // Not saving is fine: the user just gets the default next time.
                }
            }

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
                saveView("grid");
            });
            listButton.addEventListener("click", () => {
                setView("list");
                saveView("list");
            });

            setView(loadView());
        });
        
            document.getElementById("create-project-form")
                ?.addEventListener("submit", event => {
                    event.preventDefault();
                    submitCreateProject(event.currentTarget);
                });

            // The server pre-fills its own "today" as a fallback (ProjectItemCreateViewModel). Replace it
            // with the browser's date, since "today" depends on the user's timezone.
            // Built from local parts: toISOString() would give the UTC date instead.
            const createDueDate = document.querySelector("#create-project-form [name='DueDate']");
            if (createDueDate) {
                const now = new Date();
                const month = String(now.getMonth() + 1).padStart(2, "0");
                const day = String(now.getDate()).padStart(2, "0");
                // defaultValue (not just value) so form.reset() on modal close keeps the browser's date.
                createDueDate.defaultValue = `${now.getFullYear()}-${month}-${day}`;
                createDueDate.value = createDueDate.defaultValue;
            }

            // A named function so the error toast's Retry button can call it again.
            // FormData is read on each call, so a retry sends whatever is in the form now.
            async function submitCreateProject(form) {
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

                    for (const [field, messages] of Object.entries(result.errors)) {
                        const input = form.querySelector(`[name="${field}"]`);
                        input?.classList.add("input-validation-error");

                        const messageElement =
                            form.querySelector(`[data-valmsg-for="${field}"]`);

                        if (messageElement) {
                            messageElement.textContent = messages.join(" ");
                        }
                    }
                    return;
                }

                // Anything else (500, network failure): nothing was saved that we know of.
                showToast({
                    kind: "error",
                    title: "Could not save changes",
                    message: "The project wasn't created. Check your connection and try again.",
                    actionLabel: "Retry",
                    onAction: () => submitCreateProject(form)
                });
            }
                
                const trashFilters = document.querySelectorAll("[data-trash-filter]");
                        const trashSearch = document.querySelector("[data-trash-search]");
                        const trashItems = document.querySelectorAll("[data-trash-item]");
                
                        const trashSummary = document.querySelector("[data-trash-summary]");
                        const trashTable = document.getElementById("trash-table");
                        const trashNoResults = document.getElementById("trash-no-results");
                        const trashClearButtons = document.querySelectorAll("[data-trash-clear-filters]");

                        let activeTrashFilter = "all";

                        const matchesTrashType = (item, type) => type === "all" || item.dataset.trashItem === type;
                        const matchesTrashSearch = (item, searchText) =>
                            item.textContent.toLowerCase().includes(searchText);

                        function filterTrashItems() {
                            const searchText = trashSearch.value.trim().toLowerCase();

                            trashItems.forEach(item => {
                                item.hidden = !(matchesTrashType(item, activeTrashFilter) &&
                                    matchesTrashSearch(item, searchText));
                            });

                            // Each pill counts the rows it would show with the current search.
                            trashFilters.forEach(button => {
                                const count = [...trashItems].filter(item =>
                                    matchesTrashType(item, button.dataset.trashFilter) &&
                                    matchesTrashSearch(item, searchText)).length;
                                button.querySelector(".pill-count").textContent = String(count);
                            });

                            // Same "Showing X of Y" line and empty card as the Tasks page.
                            const shown = [...trashItems].filter(item => !item.hidden).length;
                            trashTable.hidden = shown === 0;
                            trashNoResults.hidden = shown > 0;
                            trashSummary.textContent = `Showing ${shown} of ${trashItems.length} items`;
                        }

                        function setTrashFilter(type) {
                            activeTrashFilter = type;
                            trashFilters.forEach(button => {
                                const isActive = button.dataset.trashFilter === type;
                                button.classList.toggle("is-active", isActive);
                                button.setAttribute("aria-pressed", String(isActive));
                            });
                            filterTrashItems();
                        }

                        trashFilters.forEach(button => {
                            button.addEventListener("click", () => setTrashFilter(button.dataset.trashFilter));
                        });

                        // Back to the starting view: all items, no search.
                        trashClearButtons.forEach(button => {
                            button.addEventListener("click", () => {
                                trashSearch.value = "";
                                setTrashFilter("all");
                                trashSearch.focus();
                            });
                        });

                        // Only the Trash page (with items) has these elements; checking them first keeps this
                        // from throwing on every other page and stopping the rest of this file.
                        if (trashSearch && trashSummary && trashTable && trashNoResults) {
                            trashSearch.addEventListener("input", filterTrashItems);
                            filterTrashItems(); // fill in the summary line on load
                        }

                        // "Sort by" on the Trash page (same idea as sorters in setUpProjectFilters).
                        const trashSort = document.querySelector("[data-trash-sort]");

                        const byTrashName = (a, b) =>
                            a.dataset.trashName.localeCompare(b.dataset.trashName, undefined, { sensitivity: "base" });
                        // data-trash-date is an ISO date string, so plain text order is date order.
                        const trashSorters = {
                            newest: (a, b) => b.dataset.trashDate.localeCompare(a.dataset.trashDate) || byTrashName(a, b),
                            oldest: (a, b) => a.dataset.trashDate.localeCompare(b.dataset.trashDate) || byTrashName(a, b),
                            name: byTrashName
                        };

                        function sortTrashItems() {
                            const sorter = trashSorters[trashSort.value] ?? trashSorters.newest;
                            const sorted = [...trashItems].sort(sorter);
                            // append() moves the existing rows, so the header row stays first.
                            trashItems[0]?.parentElement.append(...sorted);
                        }

                        // Optional chaining again: only the Trash page has this select.
                        trashSort?.addEventListener("change", sortTrashItems);
})();

// Styled "move to trash" confirmations.
// A <form data-confirm-trash="Project name"> shows the project dialog from _Layout and a
// <form data-confirm-task-trash="Task name"> shows the task dialog, instead of submitting
// right away. Either form can add data-trash-detail="..." for the grey text in the summary row.
// Kept in its own function so an early `return` above doesn't skip it (an uncaught
// error above would still stop it, though).
//
// idPrefix:      the dialog's element ids are "<idPrefix>-dialog", "-detail", "-ok", "-cancel".
// formAttribute: the data attribute (as written in HTML) that marks forms for this dialog.
function setUpTrashDialog(idPrefix, formAttribute) {
    const dialog = document.getElementById(`${idPrefix}-dialog`);
    if (!dialog) return;

    // "data-confirm-task-trash" -> "confirmTaskTrash", the name dataset uses.
    const datasetKey = formAttribute.slice("data-".length)
        .replace(/-./g, match => match[1].toUpperCase());
    const nameElements = dialog.querySelectorAll("[data-trash-name]");
    const detailElement = document.getElementById(`${idPrefix}-detail`);
    let pendingForm = null;

    // One delegated listener covers every matching form on the page.
    document.addEventListener("submit", (event) => {
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

// "Empty trash" confirmation (Trash page only). The confirm button is disabled until the
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
function setUpRequiredFieldChecks() {
    for (const form of document.querySelectorAll("form[data-check-required]")) {
        const inputs = form.querySelectorAll("[data-val-required]");

        function setFieldError(input, message) {
            input.classList.toggle("input-validation-error", Boolean(message));

            const messageElement = form.querySelector(`[data-valmsg-for="${input.name}"]`);
            if (!messageElement) return;
            messageElement.textContent = message;
            messageElement.classList.toggle("field-validation-error", Boolean(message));
        }

        // Capture phase (the `true`) so this runs before any other submit listener on the form,
        // such as the fetch-based one on the create-project form. stopImmediatePropagation then
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
        // server sent back (see submitCreateProject) go away too.
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

// Projects page: quick views, search box, the Filters panel (Status / Due), the removable chips,
// "Clear all" and "Sort by". Same design as the Tasks page (setUpTaskFilters). Both the grid cards
// and the list rows carry
//   data-project-item="<status>"   lowercase ProjectStatus: what the Status checkboxes match
//   data-project-due="<group>"     lowercase DueGroup: what the Due options match (matchesDueGroup)
//   data-project-text="<name> <description>"   what the search box looks at
//   data-project-name / -date (yyyy-MM-dd) / -progress (0-100)   what "Sort by" orders on
// so one pass hides, shows or reorders both views and they always agree.
function setUpProjectFilters() {
    const panel = document.querySelector("details.filter-panel");
    const viewButtons = [...document.querySelectorAll("[data-project-view]")];
    const statusBoxes = [...document.querySelectorAll("[data-project-status-option]")];
    const dueRadios = [...document.querySelectorAll("[data-project-due-option]")];
    const sortSelect = document.querySelector("[data-project-sort]");
    const clearButtons = document.querySelectorAll("[data-project-clear-filters]");
    const clearAllButton = document.querySelector("[data-clear-all]");
    const closeButton = document.querySelector("[data-close-filters]");
    const chipList = document.querySelector("[data-filter-chips]");
    const filterCount = document.querySelector("[data-filter-count]");
    const shownSummary = document.querySelector("[data-shown-summary]");
    const searchBox = document.getElementById("project-search");
    const items = document.querySelectorAll("[data-project-item]");
    // Every project is drawn twice (grid card + list row), so count from the grid only.
    const gridItems = [...document.querySelectorAll("#project-grid > [data-project-item]")];
    const noResults = document.getElementById("project-no-results");
    if (!panel || !sortSelect || !searchBox || !noResults || !chipList) return;

    const FILTERS_KEY = "projectFilters";

    // The statuses the server ticked (the open ones): the default view, and what Clear all goes back to.
    const defaultStatuses = statusBoxes.filter(box => box.defaultChecked).map(box => box.value);
    const dueValue = () => dueRadios.find(radio => radio.checked)?.value ?? "any";
    const setDue = value => dueRadios.forEach(radio => { radio.checked = radio.value === value; });

    const saveProjectFilters = () => saveFilters(FILTERS_KEY, {
        statuses: checkedValues(statusBoxes),
        due: dueValue(),
        sort: sortSelect.value,
        search: searchBox.value
    });

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
        filterCount.textContent = String(filters.length);
        filterCount.hidden = filters.length === 0;
    }

    // A quick view is highlighted only when the panel holds exactly its preset.
    function highlightQuickView() {
        const statuses = checkedValues(statusBoxes);
        viewButtons.forEach(button => {
            const isActive = dueValue() === button.dataset.viewDue &&
                sameValues(statuses, button.dataset.viewStatuses.split(","));
            button.classList.toggle("is-active", isActive);
            button.setAttribute("aria-pressed", String(isActive));
        });
    }

    const byName = (a, b) =>
        a.dataset.projectName.localeCompare(b.dataset.projectName, undefined, { sensitivity: "base" });

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

    function applySort() {
        const sorter = sorters[sortSelect.value] ?? sorters.due;

        // The grid cards and the list rows have different parents, so sort each parent's items on
        // its own. append() moves existing elements, so the list's header row stays first.
        for (const parent of new Set([...items].map(item => item.parentElement))) {
            const sorted = [...parent.querySelectorAll(":scope > [data-project-item]")].sort(sorter);
            parent.append(...sorted);
        }

        saveProjectFilters();
    }

    function applyFilters() {
        const statuses = checkedValues(statusBoxes);
        const due = dueValue();
        const searchText = searchBox.value.trim().toLowerCase();

        items.forEach(item => {
            item.hidden = !(
                statuses.includes(item.dataset.projectItem) &&
                matchesDueGroup(item.dataset.projectDue, due) &&
                item.dataset.projectText.toLowerCase().includes(searchText));
        });

        highlightQuickView();
        renderChips(activeFilters());

        const shown = gridItems.filter(item => !item.hidden).length;
        noResults.hidden = shown > 0;
        shownSummary.textContent = `Showing ${shown} of ${gridItems.length} projects`;
        closeButton.textContent = shown === 1 ? "Show 1 project" : `Show ${shown} projects`;
        saveProjectFilters();
    }

    viewButtons.forEach(button => {
        button.addEventListener("click", () => {
            setChecked(statusBoxes, button.dataset.viewStatuses.split(","));
            setDue(button.dataset.viewDue);
            applyFilters();
        });
    });

    // One listener for every checkbox and radio button in the panel ("change" bubbles up).
    panel.addEventListener("change", applyFilters);
    searchBox.addEventListener("input", applyFilters);
    sortSelect.addEventListener("change", applySort);

    closeButton.addEventListener("click", () => {
        panel.open = false;
        panel.querySelector("summary").focus();
    });

    // Back to the default view. "Sort by" is a view choice, not a filter, so it stays.
    clearButtons.forEach(button => {
        button.addEventListener("click", () => {
            setChecked(statusBoxes, defaultStatuses);
            setDue("any");
            searchBox.value = "";
            applyFilters();
        });
    });

    // Put back what was chosen before a reload (see loadSavedFilters). The Array check skips a status
    // saved by the old filter bar, which stored a single string.
    const saved = loadSavedFilters(FILTERS_KEY);
    if (Array.isArray(saved.statuses)) setChecked(statusBoxes, saved.statuses);
    if (dueRadios.some(radio => radio.value === saved.due)) setDue(saved.due);
    restoreControl(sortSelect, saved.sort);
    restoreControl(searchBox, saved.search);

    applyFilters();
    // The server already sent the default (due date) order; this only matters when a different
    // Sort by choice was restored above.
    applySort();
}

// Tasks page: quick views, search box, the Filters panel (Status / Priority / Due / Project),
// the removable chips, "Clear all" and "Group by". Every task row carries
//   data-task-item="<status>"            lowercase TaskStatus (todo/inprogress/review/done/cancelled):
//                                        what the Status checkboxes match
//   data-task-priority="<TaskPriority>"  what the Priority checkboxes match
//   data-task-project-id="<id>"          what the Project checkboxes match
//   data-task-due="<group>"              lowercase DueGroup: what the Due options match (matchesDueGroup)
//   data-task-text="<name> <description>"   what the search box looks at
//   data-task-due-label / -due-color / -due-order, -project-name, -priority-rank, -date
//                                        what "Group by" sorts and labels on
// The panel's checkboxes and radio buttons ARE the filter state. Quick views just tick a preset set
// of them, so a quick view and the panel can never disagree.
// One pass hides the rows that don't match, then the rows are re-sorted, the group headers rebuilt
// (with counts of the rows still shown) and the chips redrawn.
// ("Include archived projects" is not handled here: it reloads the page, because the server leaves those tasks out.)
function setUpTaskFilters() {
    const panel = document.querySelector("details.filter-panel");
    const viewButtons = [...document.querySelectorAll("[data-task-view]")];
    const statusBoxes = [...document.querySelectorAll("[data-status-option]")];
    const priorityBoxes = [...document.querySelectorAll("[data-priority-option]")];
    const projectBoxes = [...document.querySelectorAll("[data-project-option]")];
    const dueRadios = [...document.querySelectorAll("[data-due-option]")];
    const groupSelect = document.querySelector("[data-group-by]");
    const searchBox = document.querySelector("[data-task-search]");
    const clearButtons = document.querySelectorAll("[data-clear-filters]");
    const clearAllButton = document.querySelector("[data-clear-all]");
    const closeButton = document.querySelector("[data-close-filters]");
    const chipList = document.querySelector("[data-filter-chips]");
    const filterCount = document.querySelector("[data-filter-count]");
    const archivedChip = document.querySelector("[data-archived-chip]");
    const shownSummary = document.querySelector("[data-shown-summary]");
    const rows = [...document.querySelectorAll("[data-task-item]")];
    const table = document.getElementById("task-table");
    const tableBody = table?.querySelector(".data-table");
    const noResults = document.getElementById("task-no-results");
    // Not on the Tasks page, or it has no tasks at all (the server shows its own empty state then).
    if (!panel || !groupSelect || !searchBox || !tableBody || !noResults || !chipList) return;

    const FILTERS_KEY = "taskFilters";

    // The statuses the server ticked (the open ones): the default view, and what Clear all goes back to.
    // Read from the markup so the list lives in one place (Task/Index.cshtml).
    const defaultStatuses = statusBoxes.filter(box => box.defaultChecked).map(box => box.value);
    const dueValue = () => dueRadios.find(radio => radio.checked)?.value ?? "any";
    const setDue = value => dueRadios.forEach(radio => { radio.checked = radio.value === value; });

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
        if (checkedValues(priorityBoxes).length > 0) {
            filters.push({ name: "Priority", value: checkedLabels(priorityBoxes).join(", "), reset: () => setChecked(priorityBoxes, []) });
        }
        if (dueValue() !== "any") {
            filters.push({ name: "Due", value: checkedLabels(dueRadios).join(""), reset: () => setDue("any") });
        }
        if (checkedValues(projectBoxes).length > 0) {
            filters.push({ name: "Project", value: checkedLabels(projectBoxes).join(", "), reset: () => setChecked(projectBoxes, []) });
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
        // The archived chip is drawn by the server, but it is still a filter that is on.
        const total = filters.length + (archivedChip ? 1 : 0);
        filterCount.textContent = String(total);
        filterCount.hidden = total === 0;
    }

    // A quick view is highlighted only when the panel holds exactly its preset.
    function highlightQuickView() {
        const statuses = checkedValues(statusBoxes);
        const noExtraFilters = checkedValues(priorityBoxes).length === 0 && checkedValues(projectBoxes).length === 0;
        viewButtons.forEach(button => {
            const isActive = noExtraFilters &&
                dueValue() === button.dataset.viewDue &&
                sameValues(statuses, button.dataset.viewStatuses.split(","));
            button.classList.toggle("is-active", isActive);
            button.setAttribute("aria-pressed", String(isActive));
        });
    }

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
            compare: (a, b) => a.dataset.taskProjectName.localeCompare(
                b.dataset.taskProjectName, undefined, { sensitivity: "base" }),
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

    function renderGroups() {
        tableBody.querySelectorAll(".group-header").forEach(header => header.remove());

        const grouping = groupings[groupSelect.value] ?? groupings.due;
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

    function applyFilters() {
        const statuses = checkedValues(statusBoxes);
        const priorities = checkedValues(priorityBoxes);   // none ticked = any
        const projectIds = checkedValues(projectBoxes);    // none ticked = any
        const due = dueValue();
        const searchText = searchBox.value.trim().toLowerCase();

        rows.forEach(row => {
            const matches =
                statuses.includes(row.dataset.taskItem) &&
                (priorities.length === 0 || priorities.includes(row.dataset.taskPriority)) &&
                (projectIds.length === 0 || projectIds.includes(row.dataset.taskProjectId)) &&
                matchesDueGroup(row.dataset.taskDue, due) &&
                row.dataset.taskText.toLowerCase().includes(searchText);
            row.hidden = !matches;
        });

        renderGroups();
        highlightQuickView();
        renderChips(activeFilters());

        const shown = rows.filter(row => !row.hidden).length;
        table.hidden = shown === 0;
        noResults.hidden = shown > 0;
        shownSummary.textContent = `Showing ${shown} of ${rows.length} tasks`;
        closeButton.textContent = shown === 1 ? "Show 1 task" : `Show ${shown} tasks`;

        saveFilters(FILTERS_KEY, {
            statuses,
            priorities,
            projectIds,
            due,
            group: groupSelect.value,
            search: searchBox.value
        });
    }

    viewButtons.forEach(button => {
        button.addEventListener("click", () => {
            setChecked(statusBoxes, button.dataset.viewStatuses.split(","));
            setChecked(priorityBoxes, []);
            setChecked(projectBoxes, []);
            setDue(button.dataset.viewDue);
            applyFilters();
        });
    });

    // One listener for every checkbox and radio button in the panel ("change" bubbles up).
    // The archived checkbox also lands here, but it reloads the page right after, so that's harmless.
    panel.addEventListener("change", applyFilters);
    groupSelect.addEventListener("change", applyFilters);
    searchBox.addEventListener("input", applyFilters);

    closeButton.addEventListener("click", () => {
        panel.open = false;
        panel.querySelector("summary").focus();
    });

    // Back to the default view. "Group by" is a view choice, not a filter, so it stays.
    // (Archived projects aren't touched either: that one reloads the page and has its own chip.)
    clearButtons.forEach(button => {
        button.addEventListener("click", () => {
            setChecked(statusBoxes, defaultStatuses);
            setChecked(priorityBoxes, []);
            setChecked(projectBoxes, []);
            setDue("any");
            searchBox.value = "";
            applyFilters();
        });
    });

    // Put back what was chosen before a reload (see loadSavedFilters). The Array checks skip values
    // saved by the old filter bar, which stored single strings. Ids of since-deleted projects
    // simply match no checkbox.
    const saved = loadSavedFilters(FILTERS_KEY);
    if (Array.isArray(saved.statuses)) setChecked(statusBoxes, saved.statuses);
    if (Array.isArray(saved.priorities)) setChecked(priorityBoxes, saved.priorities);
    if (Array.isArray(saved.projectIds)) setChecked(projectBoxes, saved.projectIds);
    if (dueRadios.some(radio => radio.value === saved.due)) setDue(saved.due);
    restoreControl(groupSelect, saved.group);
    restoreControl(searchBox, saved.search);

    applyFilters();
}

showServerToasts();
setUpClickOutsideToClose();
setUpRowMenus();
setUpRequiredFieldChecks();
setUpAddTaskButtons();
setUpProjectFilters();
setUpTaskFilters();
setUpEmptyTrashDialog();
setUpTrashDialog("confirm-trash", "data-confirm-trash");
setUpTrashDialog("confirm-task-trash", "data-confirm-task-trash");
setUpTrashDialog("confirm-purge", "data-confirm-purge");
