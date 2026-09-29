/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Piranha.Models;

namespace Piranha.Manager.LocalAuth.Areas.Manager.Pages
{
    /// <summary>
    /// View model for the login page.
    /// </summary>
    public class LoginModel : PageModel
    {
        private readonly ISecurity _service;
        private readonly ManagerLocalizer _localizer;
        private readonly IPasskeyLoginSupport _passkeySupport;
        private readonly IApi _api;
        private readonly ILogger<LoginModel> _logger;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="service">The current security service</param>
        /// <param name="localizer">The manager localizer</param>
        /// <param name="passkeySupport">The optional passkey login support service,
        /// only provided when the host has Piranha.AspNetCore.Identity configured</param>
        /// <param name="api">The current api, used to brand the page with the current site</param>
        /// <param name="logger">The optional logger</param>
        public LoginModel(ISecurity service, ManagerLocalizer localizer, IPasskeyLoginSupport passkeySupport = null,
            IApi api = null, ILogger<LoginModel> logger = null)
        {
            _service = service;
            _localizer = localizer;
            _passkeySupport = passkeySupport;
            _api = api;
            _logger = logger;
        }

        /// <summary>
        /// Gets the site the page is shown for, matched on the request's
        /// hostname and falling back to the default site.
        /// </summary>
        public Site Site { get; private set; }

        /// <summary>
        /// Gets the name to brand the page with.
        /// </summary>
        public string SiteName { get; private set; } = "Piranha CMS";

        /// <summary>
        /// Gets the public url of the site's logo, if it has one.
        /// </summary>
        public string LogoUrl { get; private set; }

        /// <summary>
        /// Gets the lead text describing the manager.
        /// </summary>
        public string Lead => !string.IsNullOrWhiteSpace(Site?.Description)
            ? Site.Description
            : _localizer.General["Manage the pages, posts and media of your site."].Value;

        /// <summary>
        /// Gets the page title.
        /// </summary>
        public string Title => _localizer.General["Login"].Value;

        /// <summary>
        /// Gets the page subtitle.
        /// </summary>
        public string Subtitle => _localizer.General["Sign in with your username or email to continue."].Value;

        /// <summary>
        /// Gets the design picked on the manager config page, which sets the
        /// style and script files the page loads.
        /// </summary>
        public LoginDesign Design { get; private set; } = DefaultDesign;

        /// <summary>
        /// Gets the error messages to show above the form.
        /// </summary>
        public IEnumerable<string> Errors => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage);

        /// <summary>
        /// Gets if passkey sign-in should be offered on this page.
        /// </summary>
        public bool PasskeysEnabled => _passkeySupport?.IsEnabled ?? false;

        /// <summary>
        /// Gets/sets the model for binding form data.
        /// </summary>
        /// <value></value>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Gets/sets the optional return url after successful
        /// authorization.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        /// Gets/sets the possible error message to be returned
        /// after failed authorization.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Model for form data.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// Gets/sets the user name.
            /// </summary>
            [Required]
            public string Username { get; set; }

            /// <summary>
            /// Gets/sets the password.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        /// <summary>
        /// Gets the login page.
        /// </summary>
        /// <param name="returnUrl">The optional return url</param>
        public void OnGet(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        }

        /// <summary>
        /// Handles authorization after a post.
        /// </summary>
        /// <param name="returnUrl">The optional return url</param>
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            await _service.SignOut(HttpContext);

            if (!ModelState.IsValid || (await _service.SignIn(HttpContext, Input.Username, Input.Password)) != LoginResult.Succeeded)
            {
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, _localizer.General["Username and/or password are incorrect."].Value);
                return Page();
            }

            if (Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect($"~/manager/login/auth?returnUrl={ Uri.EscapeDataString(returnUrl) }");
            }
            return LocalRedirect("~/manager/login/auth");
        }

        /// <summary>
        /// Loads the site and the login design before the handler runs, so both
        /// are already set inside OnGet and OnPostAsync.
        /// </summary>
        public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            await LoadSiteAsync();
            await next();
        }

        /// <summary>
        /// Gets the registered design with the given key, falling back to the
        /// default design when no design has that key.
        /// </summary>
        /// <param name="id">The design key stored in the config</param>
        /// <param name="designs">The registered designs</param>
        public static LoginDesign GetDesign(string id, IEnumerable<LoginDesign> designs)
        {
            return designs?.FirstOrDefault(d => d.Id == id)
                ?? designs?.FirstOrDefault(d => d.Id == LoginDesign.DefaultId)
                ?? DefaultDesign;
        }

        private async Task LoadSiteAsync()
        {
            if (_api == null)
            {
                return;
            }

            Site = await _api.Sites.GetByHostnameAsync(HttpContext.Request.Host.Host)
                ?? await _api.Sites.GetDefaultAsync();

            // Keep the built-in name when the site doesn't set one, including
            // the "Default Site" title seeded on a new install.
            if (!string.IsNullOrWhiteSpace(Site?.Title) && Site.Title != "Default Site")
            {
                SiteName = Site.Title;
            }
            LogoUrl = Site?.Logo?.Media?.PublicUrl;

            string id;
            using (var config = new Config(_api))
            {
                id = config.ManagerLoginDesign;
            }

            var designs = App.Modules.Get<Piranha.Manager.Module>()?.LoginDesigns;
            Design = GetDesign(id, designs);

            if (Design.Id != id)
            {
                _logger?.LogWarning("The configured login page design {Id} isn't registered. Using {Design}.", id, Design.Id);
            }
        }

        // Used when no designs are registered at all.
        private static readonly LoginDesign DefaultDesign = new LoginDesign
        {
            Id = LoginDesign.DefaultId,
            Title = "Default",
            Styles = { "~/manager/assets/css/login-default.min.css" }
        };
    }
}
