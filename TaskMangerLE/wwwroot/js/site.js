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
    
        const userMenu = document.querySelector(".user-menu");
    
        if (userMenu) {
            document.addEventListener("click", (event) => {
                if (!userMenu.contains(event.target)) {
                    userMenu.open = false;
                }
            });
    
            document.addEventListener("keydown", (event) => {
                if (event.key === "Escape" && userMenu.open) {
                    userMenu.open = false;
                    userMenu.querySelector("summary")?.focus();
                }
            });
        }
        
        document.addEventListener("DOMContentLoaded", () => {
            const gridButton = document.getElementById("grid-view");
            const listButton = document.getElementById("list-view");
            const grid = document.getElementById("project-grid");
            const list = document.getElementById("project-list");
        
            if (!gridButton || !listButton || !grid || !list) return;
        
            function setView(view) {
                const showGrid = view === "grid";
        
                grid.hidden = !showGrid;
                list.hidden = showGrid;
        
                gridButton.classList.toggle("is-active", showGrid);
                listButton.classList.toggle("is-active", !showGrid);
                gridButton.setAttribute("aria-pressed", String(showGrid));
                listButton.setAttribute("aria-pressed", String(!showGrid));
            }
        
            gridButton.addEventListener("click", () => setView("grid"));
            listButton.addEventListener("click", () => setView("list"));
        });
        
            document.getElementById("create-project-form")
                ?.addEventListener("submit", event => {
                    event.preventDefault();
                    submitCreateProject(event.currentTarget);
                });

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

// A toast the server set before redirecting (see ToastExtensions and _Layout).
function showServerToast() {
    const data = document.getElementById("server-toast");
    if (!data) return;

    const { actionUrl, ...toast } = JSON.parse(data.textContent);
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

showServerToast();
setUpEmptyTrashDialog();
setUpTrashDialog("confirm-trash", "data-confirm-trash");
setUpTrashDialog("confirm-task-trash", "data-confirm-task-trash");
setUpTrashDialog("confirm-purge", "data-confirm-purge");
