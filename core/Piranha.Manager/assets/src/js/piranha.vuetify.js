/*global
    piranha, Vue, Vuetify
*/

piranha.vuetify = new function () {
    var colors = {
        light: {
            background: "#f2f2f2",
            surface: "#ffffff",
            primary: "#007eaa",
            secondary: "#6c757d",
            success: "#439700",
            info: "#17a2b8",
            warning: "#f0ad4e",
            error: "#a94441"
        },
        dark: {
            background: "#121212",
            surface: "#1e1e1e",
            primary: "#42a5f5",
            secondary: "#78909c",
            success: "#66bb6a",
            info: "#29b6f6",
            warning: "#ffa726",
            error: "#ef5350"
        }
    };

    this.theme = {
        defaultTheme: piranha.theme ? piranha.theme.current() : "light",
        themes: {
            light: {
                dark: false,
                colors: colors.light
            },
            dark: {
                dark: true,
                colors: colors.dark
            }
        }
    };

    this.create = function (options) {
        if (!window.Vuetify || !Vuetify.createVuetify) {
            return null;
        }

        return Vuetify.createVuetify(Object.assign({
            components: Vuetify.components,
            directives: Vuetify.directives,
            theme: this.theme
        }, options || {}));
    };

    this.mount = function (selector) {
        var target = document.querySelector(selector);

        if (!target || !window.Vue || !Vue.createApp || !window.Vuetify) {
            return null;
        }

        var vuetify = this.create();
        if (!vuetify) {
            return null;
        }

        var app = Vue.createApp({
            render: function () {
                return Vue.h(Vuetify.VApp, {
                    class: "piranha-vuetify-runtime"
                });
            }
        });

        app.use(vuetify);
        this.framework = vuetify;
        this.app = app;
        this.instance = app.mount(target);

        return this.instance;
    };

    this.applyTheme = function (theme) {
        var root = document.documentElement;
        var selectedTheme = theme === "dark" ? "dark" : "light";
        var selectedColors = colors[selectedTheme];

        root.setAttribute("data-piranha-theme", "vuetify");
        root.style.setProperty("--piranha-theme-background", selectedColors.background);
        root.style.setProperty("--piranha-theme-surface", selectedColors.surface);
        root.style.setProperty("--piranha-theme-primary", selectedColors.primary);
        root.style.setProperty("--piranha-theme-secondary", selectedColors.secondary);
        root.style.setProperty("--piranha-theme-success", selectedColors.success);
        root.style.setProperty("--piranha-theme-info", selectedColors.info);
        root.style.setProperty("--piranha-theme-warning", selectedColors.warning);
        root.style.setProperty("--piranha-theme-error", selectedColors.error);
    };

    this.setTheme = function (theme) {
        var selectedTheme = theme === "dark" ? "dark" : "light";
        this.theme.defaultTheme = selectedTheme;

        if (this.framework &&
            this.framework.theme &&
            this.framework.theme.global &&
            this.framework.theme.global.name) {
            this.framework.theme.global.name.value = selectedTheme;
        }

        this.applyTheme(selectedTheme);
    };
};

piranha.vuetify.applyTheme(piranha.theme ? piranha.theme.current() : "light");
piranha.vuetify.mount("#piranha-vuetify-app");

window.addEventListener("piranha:theme-changed", function (event) {
    piranha.vuetify.setTheme(event.detail.theme);
});
