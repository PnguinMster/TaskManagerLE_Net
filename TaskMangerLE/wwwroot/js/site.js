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
                ?.addEventListener("submit", async function (event) {
                    event.preventDefault();
        
                    const form = event.currentTarget;
                    const response = await fetch(form.action, {
                        method: "POST",
                        body: new FormData(form),
                        headers: { "X-Requested-With": "XMLHttpRequest" }
                    });
        
                    if (response.ok) {
                        window.location.reload();
                        return;
                    }
        
                    if (response.status === 400) {
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
                    }
                });
                
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

    // TODO: submit a POST to the purge-all action once the backend exists.
    okButton.addEventListener("click", () => dialog.close());
}

setUpEmptyTrashDialog();
setUpTrashDialog("confirm-trash", "data-confirm-trash");
setUpTrashDialog("confirm-task-trash", "data-confirm-task-trash");
setUpTrashDialog("confirm-purge", "data-confirm-purge");
