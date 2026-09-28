/*global
    piranha
*/

piranha.utils = {
    getOrigin() {
        return window.location.origin;
    },
    formatUrl: function (str) {
        return str.replace("~/", piranha.baseUrl);
    },
    isEmptyHtml: function (str) {
        return str == null || str.replace(/(<([^>]+)>)/ig,"").replace(/\s/g, "") == "" && str.indexOf("<img") === -1;
    },
    isEmptyText: function (str) {
        return str == null || str.replace(/\s/g, "") == "" || str.replace(/\s/g, "") == "<br>";
    },
    strLength: function (str) {
        return str != null ? str.length : 0;
    },
    languageRegion: function (culture) {
        var neutralRegions = {
            af: "ZA", ar: "SA", bg: "BG", bs: "BA", ca: "ES", cs: "CZ",
            da: "DK", de: "DE", el: "GR", en: "GB", es: "ES", fa: "IR",
            fi: "FI", fr: "FR", he: "IL", hi: "IN", hr: "HR", hu: "HU",
            id: "ID", it: "IT", ja: "JP", ka: "GE", ko: "KR", ky: "KG",
            nb: "NO", nl: "NL", nn: "NO", no: "NO", pl: "PL", pt: "PT",
            ro: "RO", ru: "RU", si: "LK", sr: "RS", sv: "SE", th: "TH",
            tr: "TR", uk: "UA", vi: "VN", zh: "CN"
        };
        var parts = String(culture || "").replace("_", "-").split("-");
        var language = (parts[0] || "").toLowerCase();
        var region = parts.find(function (part, index) {
            return index > 0 && /^[a-z]{2}$/i.test(part);
        });

        region = (region || neutralRegions[language] || "").toUpperCase();
        return /^[A-Z]{2}$/.test(region) ? region : null;
    },
    languageFlag: function (culture) {
        var region = piranha.utils.languageRegion(culture);
        if (!region) {
            return "\uD83C\uDF10";
        }

        return String.fromCodePoint(region.charCodeAt(0) + 127397, region.charCodeAt(1) + 127397);
    },
    languageFlagUrl: function (culture, size) {
        var region = piranha.utils.languageRegion(culture);
        if (!region) {
            return null;
        }

        return "https://flagsapi.com/" + region + "/flat/" + (size || 16) + ".png";
    },
    antiForgery: function () {
        const cookies = document.cookie.split(";");
        for (let i = 0; i < cookies.length; i++) {
            let c = cookies[i].trim().split("=");
            if (c[0] === piranha.antiForgery.cookieName) {
                return c[1];
            }
        }
        return "";
    },
    antiForgeryHeaders: function (isJson) {
        var headers = {};

        if (isJson === undefined || isJson === true)
        {
            headers["Content-Type"] = "application/json";
        }
        headers[piranha.antiForgery.headerName] = piranha.utils.antiForgery();

        return headers;
    }    
};

Date.prototype.addDays = function(days) {
    var date = new Date(this.valueOf());
    date.setDate(date.getDate() + days);
    return date;
}
Date.prototype.toDateString = function(days) {
    var date = new Date(this.valueOf());

    return date.getFullYear() + "-" +
        String(date.getMonth() + 1).padStart(2, '0') + "-" +
        String(date.getDate()).padStart(2, '0');
}

$(document).ready(function () {
    $('.block-header .danger').hover(
        function() {
            $(this).closest('.block').addClass('danger');
        },
        function() {
            $(this).closest('.block').removeClass('danger');
        });

    $('form.validate').submit(
        function (e) {
            if ($(this).get(0).checkValidity() === false) {
                e.preventDefault();
                e.stopPropagation();

                $(this).addClass('was-validated');
            }
        }
    );
});

$(document).on('mouseenter', '.block-header .danger', function() {
    $(this).closest('.block').addClass('danger');
});

$(document).on('mouseleave', '.block-header .danger', function() {
    $(this).closest('.block').removeClass('danger');
});

$(document).on('shown.bs.collapse', '.collapse', function () {
	$(this).parent().addClass('active');
});

$(document).on('hide.bs.collapse', '.collapse', function () {
	$(this).parent().removeClass('active');
});

// Fix scroll prevention for multiple modals in bootstrap
$(document).on('hidden.bs.modal', '.modal', function () {
    $('.modal:visible').length && $(document.body).addClass('modal-open');
});

$(window).scroll(function () {
    var scroll = $(this).scrollTop();

    $(".app .component-toolbar").each(function () {
        var parent = $(this).parent();
        var parentTop = parent.offset().top;

        if (scroll > parentTop) {
            var parentHeight = parent.outerHeight();
            var bottom = parentTop + parentHeight;

            if (scroll > bottom) {
                $(this).css({ "top": parentHeight + "px" })
            } else {
                $(this).css({ "top": scroll - parentTop + "px" })
            }
        } else {
            $(this).removeAttr("style");
        }
    });
});
