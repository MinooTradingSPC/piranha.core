/*global
    piranha
*/

piranha.managerLanguage = new function () {
    var storageKey = "piranha-admin-language";
    var cookieName = "piranha.manager.culture";

    function normalize(culture) {
        return String(culture || "").replace("_", "-").toLowerCase();
    }

    function findAvailable(culture) {
        var requested = normalize(culture);
        var requestedLanguage = requested.split("-")[0];
        var buttons = Array.from(document.querySelectorAll("[data-manager-language-value]"));

        return buttons.find(function (button) {
            return normalize(button.getAttribute("data-manager-language-value")) === requested;
        }) || buttons.find(function (button) {
            return normalize(button.getAttribute("data-manager-language-value")).split("-")[0] === requestedLanguage;
        });
    }

    var maxScale = 1.3;
    var minScale = 0.85;
    var minOpacity = 0.55;
    var carouselFrame = null;

    function carouselContainer() {
        return document.querySelector(".manager-language-chooser");
    }

    function updateCarouselScale() {
        var container = carouselContainer();
        if (!container) {
            return;
        }

        var rect = container.getBoundingClientRect();
        if (rect.width === 0) {
            return;
        }

        var centerX = rect.left + rect.width / 2;
        var halfWidth = rect.width / 2;

        Array.from(container.querySelectorAll("[data-manager-language-value]")).forEach(function (button) {
            if (getComputedStyle(button).display === "none") {
                return;
            }

            var buttonRect = button.getBoundingClientRect();
            var buttonCenter = buttonRect.left + buttonRect.width / 2;
            var ratio = Math.min(Math.abs(buttonCenter - centerX) / halfWidth, 1);
            var scale = maxScale - ratio * (maxScale - minScale);
            var opacity = 1 - ratio * (1 - minOpacity);

            button.style.transform = "scale(" + scale.toFixed(3) + ")";
            button.style.opacity = opacity.toFixed(3);
        });
    }

    function requestCarouselUpdate() {
        if (carouselFrame) {
            return;
        }

        carouselFrame = window.requestAnimationFrame(function () {
            carouselFrame = null;
            updateCarouselScale();
        });
    }

    function centerCarouselButton(button, smooth) {
        if (!button) {
            return;
        }

        button.scrollIntoView({
            behavior: smooth ? "smooth" : "auto",
            block: "nearest",
            inline: "center"
        });
    }

    function initCarousel(selected) {
        var container = carouselContainer();
        if (!container) {
            return;
        }

        container.addEventListener("scroll", requestCarouselUpdate, { passive: true });
        window.addEventListener("resize", requestCarouselUpdate);

        container.addEventListener("wheel", function (event) {
            var delta = Math.abs(event.deltaY) > Math.abs(event.deltaX) ? event.deltaY : event.deltaX;
            if (delta === 0) {
                return;
            }

            event.preventDefault();
            container.scrollLeft += delta;
        }, { passive: false });

        var navbar = document.querySelector(".navbar-left");
        if (navbar) {
            navbar.addEventListener("transitionend", function (event) {
                if (event.propertyName === "width") {
                    centerCarouselButton(container.querySelector(".active"), false);
                    requestCarouselUpdate();
                }
            });
        }

        centerCarouselButton(selected, false);
        requestCarouselUpdate();
    }

    function store(culture) {
        try {
            localStorage.setItem(storageKey, culture);
        } catch (error) {
            // Keep the selected language for this page when storage is unavailable.
        }

        var path = (piranha.baseUrl || "/") + "manager";
        var secure = window.location.protocol === "https:" ? "; Secure" : "";
        document.cookie = cookieName + "=" + encodeURIComponent(culture) +
            "; Path=" + path + "; Max-Age=31536000; SameSite=Lax" + secure;
    }

    this.set = function (culture, persist, reload) {
        var selected = findAvailable(culture);
        if (!selected) {
            return;
        }

        var selectedCulture = selected.getAttribute("data-manager-language-value");
        document.documentElement.setAttribute("lang", selectedCulture);

        document.querySelectorAll("[data-manager-language-value]").forEach(function (button) {
            var isActive = button === selected;
            button.classList.toggle("active", isActive);
            button.setAttribute("aria-pressed", isActive ? "true" : "false");
        });

        centerCarouselButton(selected, persist !== false);
        requestCarouselUpdate();

        if (persist !== false) {
            store(selectedCulture);
        }

        if (reload) {
            window.location.reload();
        }
    };

    this.init = function () {
        var self = this;
        var storedCulture = null;
        var documentCulture = document.documentElement.getAttribute("lang") || "";

        document.querySelectorAll("[data-manager-language-flag]").forEach(function (flag) {
            var url = piranha.utils.languageFlagUrl(flag.getAttribute("data-manager-language-flag"), 24);
            if (url) {
                flag.src = url;
            }
        });

        try {
            storedCulture = localStorage.getItem(storageKey);
        } catch (error) {
            // Use the current document culture when storage is unavailable.
        }

        var selected = findAvailable(storedCulture || documentCulture);
        if (!selected) {
            selected = document.querySelector("[data-manager-language-value]");
        }
        if (!selected) {
            return;
        }

        var selectedCulture = selected.getAttribute("data-manager-language-value");
        if (storedCulture && normalize(selectedCulture) !== normalize(documentCulture)) {
            store(selectedCulture);
            window.location.reload();
            return;
        }

        self.set(selectedCulture, false, false);
        initCarousel(selected);

        document.querySelectorAll("[data-manager-language-value]").forEach(function (button) {
            button.addEventListener("click", function () {
                self.set(button.getAttribute("data-manager-language-value"), true, true);
            });
        });
    };
};

piranha.managerLanguage.init();
