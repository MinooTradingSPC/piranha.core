/*global
    bootstrap, jQuery
*/

// Bootstrap 5 no longer exposes jQuery plugins. Keep the Manager's existing
// modal calls working while templates and extensions move to the native API.
(function ($) {
    if (!$ || !window.bootstrap || !bootstrap.Modal) {
        return;
    }

    $.fn.modal = function (action) {
        return this.each(function () {
            var instance = bootstrap.Modal.getOrCreateInstance(this);

            if (action === "hide") {
                instance.hide();
            } else if (action === "dispose") {
                instance.dispose();
            } else if (action === "toggle") {
                instance.toggle();
            } else {
                instance.show();
            }
        });
    };
})(window.jQuery);
