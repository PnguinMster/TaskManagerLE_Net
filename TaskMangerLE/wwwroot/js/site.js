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
})();