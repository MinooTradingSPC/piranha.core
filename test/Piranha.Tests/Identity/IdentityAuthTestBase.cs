/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using Fido2NetLib;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Piranha.AspNetCore.Identity.Controllers;
using Piranha.AspNetCore.Identity.Data;
using Piranha.AspNetCore.Identity.Models;
using Piranha.AspNetCore.Identity.Services;
using Piranha.AspNetCore.Identity.SQLite;
using Xunit;

namespace Piranha.Tests.Identity;

/// <summary>
/// Shared harness for exercising the real Manager passwordless-auth stack
/// (real ASP.NET Core Identity, real EF Core against a temp-file SQLite
/// database, real Otp.NET/data-protection) rather than mocking it away.
/// Only WebAuthn is faked (see <see cref="FakeFido2"/>) since a genuine
/// ceremony needs an actual authenticator.
/// </summary>
public abstract class IdentityAuthTestBase : IDisposable
{
    /// <summary>
    /// Kept low so tests that need to trigger lockout don't need many
    /// iterations.
    /// </summary>
    protected const int MaxFailedAccessAttempts = 3;

    private readonly string _dbFile;
    protected readonly ServiceProvider Provider;
    protected readonly FakeFido2 Fido2;

    protected IdentityAuthTestBase(Action<IServiceCollection> configureServices = null)
    {
        _dbFile = Path.Combine(Path.GetTempPath(),
            $"piranha.tests.identity.{GetType().Name}.{Guid.NewGuid():N}.db");

        Fido2 = new FakeFido2();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddHttpContextAccessor();
        services.AddMvcCore();

        services.AddDbContext<IdentitySQLiteDb>(o => o.UseSqlite($"Filename={_dbFile}"));
        services.AddScoped<Piranha.AspNetCore.Identity.IDb, IdentitySQLiteDb>();

        services.AddIdentity<User, Role>(o =>
            {
                o.Lockout.MaxFailedAccessAttempts = MaxFailedAccessAttempts;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                o.Lockout.AllowedForNewUsers = true;
                o.Password.RequireDigit = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequiredUniqueChars = 1;
                o.Password.RequiredLength = 6;
                o.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<IdentitySQLiteDb>()
            .AddDefaultTokenProviders();

        services.AddSingleton<IFido2>(Fido2);
        services.AddScoped<IPasskeyService, PasskeyService>();
        services.AddScoped<ITotpService, TotpService>();
        services.AddScoped<IRecoveryService, RecoveryService>();
        services.AddSingleton<AuthRateLimiters>();
        services.AddScoped<ISecurityAuditLogger, SecurityAuditLogger>();

        configureServices?.Invoke(services);

        Provider = services.BuildServiceProvider();

        // Db<T>'s own auto-migrate is guarded by a static flag on the
        // closed generic type shared across the whole test process, so
        // once any other Identity test class has already constructed one,
        // ours would silently skip migrating even though it's a different,
        // fresh file. Migrate explicitly - idempotent, since EF's migration
        // history table already tracks what's applied - to guarantee this
        // file's schema exists regardless of test class ordering.
        using var scope = Provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IdentitySQLiteDb>().Database.Migrate();
    }

    public virtual void Dispose()
    {
        Provider.Dispose();

        try
        {
            if (File.Exists(_dbFile))
            {
                File.Delete(_dbFile);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup; a locked temp file isn't worth failing
            // the test run over.
        }
    }

    /// <summary>
    /// Creates a confirmed user with the given password in a throwaway
    /// scope, matching how a real request would.
    /// </summary>
    protected async Task<User> CreateUserAsync(string email, string password)
    {
        using var scope = Provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var user = new User { UserName = email, Email = email, EmailConfirmed = true };
        var created = await userManager.CreateAsync(user, password);

        Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(e => e.Description)));

        return user;
    }

    /// <summary>
    /// Runs the email-first discovery step and returns the resulting flow
    /// token, exactly as a real client would before calling Verify.
    /// </summary>
    protected async Task<AuthOptionsResponse> GetOptionsAsync(string email)
    {
        using var scope = Provider.CreateScope();
        var controller = CreateAuthController(scope.ServiceProvider);

        return await controller.Options(new AuthOptionsRequest { Email = email });
    }

    /// <summary>
    /// Creates a <see cref="PasskeyAuthController"/> wired to a real
    /// HttpContext/IUrlHelper/IHttpContextAccessor so SignInManager and
    /// Url.IsLocalUrl both work, matching what ASP.NET Core would normally
    /// provide per-request.
    /// </summary>
    protected static PasskeyAuthController CreateAuthController(IServiceProvider provider)
    {
        var httpContext = NewHttpContext(provider);

        var controller = ActivatorUtilities.CreateInstance<PasskeyAuthController>(provider);
        controller.ControllerContext = NewControllerContext(httpContext);

        return controller;
    }

    /// <summary>
    /// Creates a <see cref="PasskeyController"/> for the self-service
    /// passkey management endpoints, with the given user set as the
    /// "signed in" caller.
    /// </summary>
    protected static async Task<PasskeyController> CreatePasskeyControllerAsync(IServiceProvider provider, User user)
    {
        var httpContext = await NewAuthenticatedHttpContextAsync(provider, user);

        var controller = ActivatorUtilities.CreateInstance<PasskeyController>(provider);
        controller.ControllerContext = NewControllerContext(httpContext);

        return controller;
    }

    /// <summary>
    /// Creates a <see cref="TotpController"/> for the self-service TOTP
    /// enrollment endpoints, with the given user set as the "signed in"
    /// caller.
    /// </summary>
    protected static async Task<TotpController> CreateTotpControllerAsync(IServiceProvider provider, User user)
    {
        var httpContext = await NewAuthenticatedHttpContextAsync(provider, user);

        var controller = ActivatorUtilities.CreateInstance<TotpController>(provider);
        controller.ControllerContext = NewControllerContext(httpContext);

        return controller;
    }

    private static HttpContext NewHttpContext(IServiceProvider provider)
    {
        var httpContext = new DefaultHttpContext { RequestServices = provider };

        // SignInManager resolves the request through its own injected
        // IHttpContextAccessor, separate from a controller's own
        // ControllerContext.HttpContext - both need to point at the same
        // context or SignInAsync throws "HttpContext must not be null".
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;

        return httpContext;
    }

    private static async Task<HttpContext> NewAuthenticatedHttpContextAsync(IServiceProvider provider, User user)
    {
        var httpContext = NewHttpContext(provider);
        var signInManager = provider.GetRequiredService<SignInManager<User>>();

        httpContext.User = await signInManager.CreateUserPrincipalAsync(user);

        return httpContext;
    }

    private static ControllerContext NewControllerContext(HttpContext httpContext) => new()
    {
        HttpContext = httpContext,
        RouteData = new RouteData(),
        ActionDescriptor = new ControllerActionDescriptor()
    };
}
