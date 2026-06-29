/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Piranha.AspNetCore.Helpers;
using Piranha.AspNetCore.Http;
using Piranha.AspNetCore.Security;
using Piranha.AspNetCore.Services;
using Piranha.Manager;
using Piranha.Manager.LocalAuth;
using Piranha.Manager.LocalAuth.Areas.Manager.Pages;
using Piranha.Models;
using Xunit;

namespace Piranha.Tests.Security;

public class ReturnUrlSecurityTests
{
    [Fact]
    public async Task SecurityMiddlewareUrlEncodesReturnUrl()
    {
        var context = new DefaultHttpContext();
        var service = new TestApplicationService
        {
            Request =
            {
                Url = "/protected?returnUrl=//evil.example/<script>alert(1)</script>"
            }
        };
        var middleware = new SecurityMiddleware(
            ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            },
            Options.Create(new SecurityOptions { LoginUrl = "/login" }));

        await middleware.InvokeAsync(context, service);

        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal(
            "/login?returnUrl=%2Fprotected%3FreturnUrl%3D%2F%2Fevil.example%2F%3Cscript%3Ealert%281%29%3C%2Fscript%3E",
            context.Response.Headers.Location.ToString());
    }

    [Fact]
    public void LoginGetDropsExternalReturnUrl()
    {
        var model = CreateLoginModel();

        model.OnGet("//evil.example/login");

        Assert.Null(model.ReturnUrl);
    }

    [Fact]
    public async Task LoginPostEncodesLocalReturnUrlBeforeAuthRedirect()
    {
        var model = CreateLoginModel();
        model.Input = new LoginModel.InputModel
        {
            Username = "admin",
            Password = "password"
        };

        var result = await model.OnPostAsync("/protected?name=<script>");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("~/manager/login/auth?returnUrl=%2Fprotected%3Fname%3D%3Cscript%3E", redirect.Url);
    }

    [Fact]
    public async Task LoginPostDropsExternalReturnUrlBeforeAuthRedirect()
    {
        var model = CreateLoginModel();
        model.Input = new LoginModel.InputModel
        {
            Username = "admin",
            Password = "password"
        };

        var result = await model.OnPostAsync("https://evil.example/login");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("~/manager/login/auth", redirect.Url);
    }

    private static LoginModel CreateLoginModel()
    {
        var httpContext = new DefaultHttpContext();
        var routeData = new RouteData();
        var actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor());

        return new LoginModel(new TestSecurity(), CreateLocalizer())
        {
            PageContext = new PageContext(actionContext),
            Url = new UrlHelper(actionContext)
        };
    }

    private static ManagerLocalizer CreateLocalizer()
    {
        return new ManagerLocalizer(
            new TestStringLocalizer<Piranha.Manager.Localization.Alias>(),
            new TestStringLocalizer<Piranha.Manager.Localization.Comment>(),
            new TestStringLocalizer<Piranha.Manager.Localization.Content>(),
            new TestStringLocalizer<Piranha.Manager.Localization.Config>(),
            new TestStringLocalizer<Piranha.Manager.Localization.General>(),
            new TestStringLocalizer<Piranha.Manager.Localization.Security>(),
            new TestStringLocalizer<Piranha.Manager.Localization.Language>(),
            new TestStringLocalizer<Piranha.Manager.Localization.Media>(),
            new TestStringLocalizer<Piranha.Manager.Localization.Menu>(),
            new TestStringLocalizer<Piranha.Manager.Localization.Module>(),
            new TestStringLocalizer<Piranha.Manager.Localization.Page>(),
            new TestStringLocalizer<Piranha.Manager.Localization.Post>(),
            new TestStringLocalizer<Piranha.Manager.Localization.Site>());
    }

    private sealed class TestApplicationService : IApplicationService
    {
        public IApi Api => null;

        public ISiteHelper Site => null;

        public IMediaHelper Media => null;

        public IRequestHelper Request { get; } = new RequestHelper();

        public Guid PageId { get; set; }

        public Piranha.Models.PageBase CurrentPage { get; set; }

        public PostBase CurrentPost { get; set; }

        public Task InitAsync(HttpContext context)
        {
            return Task.CompletedTask;
        }

        public string GetGravatarUrl(string email, int size = 0)
        {
            return string.Empty;
        }
    }

    private sealed class TestSecurity : ISecurity
    {
        public Task<LoginResult> SignIn(object context, string username, string password)
        {
            return Task.FromResult(LoginResult.Succeeded);
        }

        public Task SignOut(object context)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestStringLocalizer<T> : IStringLocalizer<T>
    {
        public LocalizedString this[string name] => new(name, name);

        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return Enumerable.Empty<LocalizedString>();
        }
    }
}