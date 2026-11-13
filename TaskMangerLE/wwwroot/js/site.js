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
                
                        let activeTrashFilter = "all";
                
                        function filterTrashItems() {
                            const searchText = trashSearch.value.trim().toLowerCase();
                
                            trashItems.forEach(item => {
                                const matchesType =
                                    activeTrashFilter === "all" ||
                                    item.dataset.trashItem === activeTrashFilter;
                
                                const matchesSearch =
                                    item.textContent.toLowerCase().includes(searchText);
                
                                item.hidden = !(matchesType && matchesSearch);
                            });
                        }
                
                        trashFilters.forEach(button => {
                            button.addEventListener("click", () => {
                                activeTrashFilter = button.dataset.trashFilter;
                
                                trashFilters.forEach(filterButton => {
                                    const isActive = filterButton === button;
                                    filterButton.classList.toggle("is-active", isActive);
                                    filterButton.setAttribute("aria-pressed", isActive);
                                });
                
                                filterTrashItems();
                            });
                        });
                
                        // Only the Trash page has a search box; without ?. this throws on every
                        // other page and stops the rest of this file (including the dialog below).
                        trashSearch?.addEventListener("input", filterTrashItems);
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
    const openMenus = () => document.querySelectorAll("details.user-menu[open], details.task-card-menu[open]");

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

// Projects page: status pills, Status and Due date dropdowns, search box, "Clear filters" and sorting.
// Both the grid cards and the list rows carry
//   data-project-item="<status>"   lowercase ProjectStatus: the pills and the Status dropdown match it
//   data-project-due="<group>"     lowercase ProjectDueGroup: the Due date dropdown matches it
//   data-project-text="<name> <description>"   what the search box looks at
//   data-project-name / -date (yyyy-MM-dd) / -progress (0-100)   what "Sort by" orders on
// so one pass hides, shows or reorders both views and they always agree.
function setUpProjectFilters() {
    const pills = document.querySelectorAll("[data-project-status-pill]");
    const statusSelect = document.querySelector("[data-project-status]");
    const dueSelect = document.querySelector("[data-project-due-filter]");
    const sortSelect = document.querySelector("[data-project-sort]");
    const clearButtons = document.querySelectorAll("[data-project-clear-filters]");
    const searchBox = document.getElementById("project-search");
    const items = document.querySelectorAll("[data-project-item]");
    const noResults = document.getElementById("project-no-results");
    if (!statusSelect || !dueSelect || !sortSelect || !searchBox || !noResults) return;

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
    }

    // The controls themselves hold the state, so a browser that restores form values after a reload
    // or Back (and the page's own initial call below) stays in sync with what is shown.
    function applyFilters() {
        const status = statusSelect.value;   // "all" or a status
        const due = dueSelect.value;         // "" (any) or a due group
        const searchText = searchBox.value.trim().toLowerCase();

        // The pills are shortcuts for the Status dropdown: highlight the one that matches it
        // (none, if the dropdown is on a status that has no pill, like Planning).
        pills.forEach(pill => {
            const isActive = pill.dataset.projectStatusPill === status;
            pill.classList.toggle("is-active", isActive);
            pill.setAttribute("aria-pressed", String(isActive));
        });

        items.forEach(item => {
            // "All" means everything still in play: archived projects only show when picked directly.
            const matchesStatus = status === "all"
                ? item.dataset.projectItem !== "archived"
                : item.dataset.projectItem === status;
            const matchesDue = due === "" || item.dataset.projectDue === due;
            const matchesSearch = item.dataset.projectText.toLowerCase().includes(searchText);
            item.hidden = !(matchesStatus && matchesDue && matchesSearch);
        });

        noResults.hidden = [...items].some(item => !item.hidden);
    }

    pills.forEach(pill => {
        pill.addEventListener("click", () => {
            statusSelect.value = pill.dataset.projectStatusPill;
            applyFilters();
        });
    });

    statusSelect.addEventListener("change", applyFilters);
    dueSelect.addEventListener("change", applyFilters);
    searchBox.addEventListener("input", applyFilters);
    sortSelect.addEventListener("change", applySort);

    clearButtons.forEach(button => {
        button.addEventListener("click", () => {
            statusSelect.value = "all";
            dueSelect.value = "";
            searchBox.value = "";
            applyFilters();
        });
    });

    applyFilters();
    // The server already sent the default (due date) order; this only matters when the browser
    // restored a different Sort by choice after a reload or Back.
    applySort();
}

// Tasks page: status pills, Project / Priority / Due date dropdowns, search box, "Clear filters"
// and "Group by". Same idea as setUpProjectFilters: every task row carries
//   data-task-item="pending|completed"   what the pills match
//   data-task-group="<key>"              its Due date group (overdue/today/week/later/completed)
//   data-task-project="<project id>"     data-task-priority="<TaskPriority>"
//   data-task-text="<name> <description>"   what the search box looks at
//   data-task-due-label / -due-color / -due-order, -project-name, -priority-rank, -date
//                                        what "Group by" sorts and labels on
// One pass hides the rows that don't match, then the rows are re-sorted and the group headers
// rebuilt (with counts of the rows still shown).
// ("Hide archived" is not handled here: it reloads the page, because the server leaves those tasks out.)
function setUpTaskFilters() {
    const pills = document.querySelectorAll("[data-status-filter]");
    const projectSelect = document.querySelector("[data-project-filter]");
    const prioritySelect = document.querySelector("[data-priority-filter]");
    const dueSelect = document.querySelector("[data-due-filter]");
    const groupSelect = document.querySelector("[data-group-by]");
    const searchBox = document.querySelector("[data-task-search]");
    const clearButtons = document.querySelectorAll("[data-clear-filters]");
    const rows = [...document.querySelectorAll("[data-task-item]")];
    const table = document.getElementById("task-table");
    const tableBody = table?.querySelector(".data-table");
    const noResults = document.getElementById("task-no-results");
    // Not on the Tasks page, or it has no tasks at all (the server shows its own empty state then).
    if (!projectSelect || !prioritySelect || !dueSelect || !groupSelect || !searchBox || !tableBody || !noResults) return;

    // Unlike the dropdowns, pills aren't form fields, so the chosen one is kept here.
    let status = "all";

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
        const searchText = searchBox.value.trim().toLowerCase();

        pills.forEach(pill => {
            const isActive = pill.dataset.statusFilter === status;
            pill.classList.toggle("is-active", isActive);
            pill.setAttribute("aria-pressed", String(isActive));
        });

        rows.forEach(row => {
            const matches =
                (status === "all" || row.dataset.taskItem === status) &&
                (projectSelect.value === "" || row.dataset.taskProject === projectSelect.value) &&
                (prioritySelect.value === "" || row.dataset.taskPriority === prioritySelect.value) &&
                (dueSelect.value === "" || row.dataset.taskGroup === dueSelect.value) &&
                row.dataset.taskText.toLowerCase().includes(searchText);
            row.hidden = !matches;
        });

        renderGroups();

        const anyVisible = rows.some(row => !row.hidden);
        table.hidden = !anyVisible;
        noResults.hidden = anyVisible;
    }

    pills.forEach(pill => {
        pill.addEventListener("click", () => {
            status = pill.dataset.statusFilter;
            applyFilters();
        });
    });

    projectSelect.addEventListener("change", applyFilters);
    prioritySelect.addEventListener("change", applyFilters);
    dueSelect.addEventListener("change", applyFilters);
    groupSelect.addEventListener("change", applyFilters);
    searchBox.addEventListener("input", applyFilters);

    // "Group by" is a view choice, not a filter, so Clear filters leaves it alone.
    clearButtons.forEach(button => {
        button.addEventListener("click", () => {
            status = "all";
            projectSelect.value = "";
            prioritySelect.value = "";
            dueSelect.value = "";
            searchBox.value = "";
            applyFilters();
        });
    });

    // Picks up dropdown/search values a browser restored after a reload or Back.
    applyFilters();
}

showServerToasts();
setUpClickOutsideToClose();
setUpRequiredFieldChecks();
setUpAddTaskButtons();
setUpProjectFilters();
setUpTaskFilters();
setUpEmptyTrashDialog();
setUpTrashDialog("confirm-trash", "data-confirm-trash");
setUpTrashDialog("confirm-task-trash", "data-confirm-task-trash");
setUpTrashDialog("confirm-purge", "data-confirm-purge");
