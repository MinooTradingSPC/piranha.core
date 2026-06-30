/*global
    piranha, Vue, Vuetify
*/

piranha.vuetify = new function () {
    var colors = {
        background: "#f2f2f2",
        surface: "#ffffff",
        primary: "#007eaa",
        secondary: "#6c757d",
        success: "#439700",
        info: "#17a2b8",
        warning: "#f0ad4e",
        error: "#a94441"
    };

    this.theme = {
        themes: {
            light: colors
        }
    };

    this.install = function () {
        if (window.Vue && window.Vuetify && Vue.use) {
            Vue.use(Vuetify);
        }
    };

    this.create = function (options) {
        if (!window.Vuetify) {
            return null;
        }

        return new Vuetify(Object.assign({
            theme: this.theme
        }, options || {}));
    };

    this.mount = function (selector) {
        var target = document.querySelector(selector);

        if (!target || !window.Vue || !window.Vuetify) {
            return null;
        }

        this.install();

        var vuetify = this.create();
        var app = new Vue({
            vuetify: vuetify,
            render: function (createElement) {
                return createElement("v-app", {
                    staticClass: "piranha-vuetify-runtime"
                });
            }
        });

        this.app = app;
        this.instance = app.$mount(target);

        return this.instance;
    };

    this.applyTheme = function () {
        var root = document.documentElement;

        root.setAttribute("data-piranha-theme", "vuetify");
        root.style.setProperty("--piranha-theme-background", colors.background);
        root.style.setProperty("--piranha-theme-surface", colors.surface);
        root.style.setProperty("--piranha-theme-primary", colors.primary);
        root.style.setProperty("--piranha-theme-secondary", colors.secondary);
        root.style.setProperty("--piranha-theme-success", colors.success);
        root.style.setProperty("--piranha-theme-info", colors.info);
        root.style.setProperty("--piranha-theme-warning", colors.warning);
        root.style.setProperty("--piranha-theme-error", colors.error);
    };
};

piranha.vuetify.install();
piranha.vuetify.applyTheme();
piranha.vuetify.mount("#piranha-vuetify-app");
