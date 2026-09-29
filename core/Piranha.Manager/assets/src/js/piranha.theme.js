/*global
    piranha
*/

piranha.theme = new function () {
    var storageKey = "piranha-admin-theme";
    var legacyStorageKey = "piranha.manager.theme";
    var themes = ["light", "dark"];

    this.storageKey = storageKey;

    this.current = function () {
        var theme = document.documentElement.getAttribute("data-manager-theme");
        return themes.indexOf(theme) !== -1 ? theme : "light";
    };

    this.set = function (theme, persist) {
        if (themes.indexOf(theme) === -1) {
            return;
        }

        document.documentElement.setAttribute("data-bs-theme", theme);
        document.documentElement.setAttribute("data-manager-theme", theme);

        if (persist !== false) {
            try {
                localStorage.setItem(storageKey, theme);
                localStorage.setItem(legacyStorageKey, theme);
            } catch (error) {
                // Keep the active theme for this page when storage is unavailable.
            }
        }

        // Vuetify applications mounted by independent Manager modules often create
        // their own framework instance. Switching the generated theme class keeps
        // those applications in sync even when they do not subscribe to our event.
        document.querySelectorAll(".v-theme--light, .v-theme--dark").forEach(function (element) {
            element.classList.remove("v-theme--light", "v-theme--dark");
            element.classList.add("v-theme--" + theme);
        });

        document.querySelectorAll("[data-manager-theme-value]").forEach(function (button) {
            var isActive = button.getAttribute("data-manager-theme-value") === theme;
            button.classList.toggle("active", isActive);
            button.setAttribute("aria-pressed", isActive ? "true" : "false");
        });

        window.dispatchEvent(new CustomEvent("piranha:theme-changed", {
            detail: {
                theme: theme,
                storageKey: storageKey
            }
        }));
    };

    this.init = function () {
        var self = this;

        document.querySelectorAll("[data-manager-theme-value]").forEach(function (button) {
            button.addEventListener("click", function () {
                self.set(button.getAttribute("data-manager-theme-value"));
            });
        });

        self.set(self.current(), false);
    };
};

piranha.theme.init();
