/*global
    piranha
*/

piranha.menu = new function () {
    var storageKey = "piranha-admin-menu-collapsed";
    var minWidth = 160;
    var maxWidth = 480;

    function navbar() {
        return document.querySelector(".navbar-left");
    }

    function groups() {
        return document.querySelectorAll(".navbar-left [data-manager-menu-group]");
    }

    function load() {
        try {
            var value = JSON.parse(localStorage.getItem(storageKey));
            return Array.isArray(value) ? value : [];
        } catch (error) {
            // Every group starts expanded when storage is unavailable.
            return [];
        }
    }

    function store() {
        var collapsed = [];

        groups().forEach(function (group) {
            if (group.classList.contains("collapsed")) {
                collapsed.push(group.getAttribute("data-manager-menu-group"));
            }
        });

        try {
            localStorage.setItem(storageKey, JSON.stringify(collapsed));
        } catch (error) {
            // The collapsed state still applies to the current page.
        }
    }

    function setCollapsed(group, collapsed) {
        group.classList.toggle("collapsed", collapsed);

        var toggle = group.querySelector(".nav-header");
        if (toggle) {
            toggle.setAttribute("aria-expanded", collapsed ? "false" : "true");
        }
    }

    this.setWidth = function (width) {
        var nav = navbar();
        width = parseInt(width, 10);

        if (!nav || isNaN(width)) {
            return;
        }
        nav.style.setProperty("--manager-menu-width", Math.min(Math.max(width, minWidth), maxWidth) + "px");
    };

    this.init = function () {
        var collapsed = load();

        groups().forEach(function (group) {
            // The group holding the current page always stays open so the
            // active item remains visible after navigation.
            var isCollapsed = !group.classList.contains("current") &&
                collapsed.indexOf(group.getAttribute("data-manager-menu-group")) !== -1;

            setCollapsed(group, isCollapsed);

            var toggle = group.querySelector(".nav-header");
            if (toggle) {
                toggle.addEventListener("click", function () {
                    setCollapsed(group, !group.classList.contains("collapsed"));
                    store();
                });
            }
        });
    };
};

piranha.menu.init();
