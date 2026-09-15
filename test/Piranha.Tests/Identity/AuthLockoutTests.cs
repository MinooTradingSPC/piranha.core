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
using Piranha.AspNetCore.Identity;
using Piranha.AspNetCore.Identity.Controllers;
using Piranha.AspNetCore.Identity.Data;
using Piranha.AspNetCore.Identity.Models;
using Piranha.AspNetCore.Identity.Services;
using Piranha.AspNetCore.Identity.SQLite;
using Xunit;

namespace Piranha.Tests.Identity;

/// <summary>
/// Covers #176's acceptance criterion: repeated failed login attempts
/// trigger lockout, and the account works again once the lockout window
/// has passed. Exercises the real ASP.NET Core Identity lockout machinery
/// through PasskeyAuthController.Verify, backed by a real (temp-file)
/// SQLite database, rather than mocking it out.
/// </summary>
public class AuthLockoutTests : IDisposable
{
    private const int MaxFailedAccessAttempts = 3;

    // Db<T>'s auto-migrate only ever runs once per process (guarded by a
    // static flag on the closed generic type), so every test in this class
    // has to share one database file rather than each getting a fresh one -
    // a second file would never get migrated and every table lookup would
    // fail. Distinct test data (emails) keeps the tests independent even
    // though they share the underlying file. Keyed by process id so the
    // net8/net9/net10 test runs (separate processes, possibly concurrent)
    // never collide on the same file.
    private static readonly string DbFile = Path.Combine(Path.GetTempPath(),
        $"piranha.tests.identity.authlockout.{Environment.ProcessId}.db");

    private readonly ServiceProvider _provider;

    static AuthLockoutTests()
    {
        if (File.Exists(DbFile))
        {
            File.Delete(DbFile);
        }
    }

    public AuthLockoutTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddHttpContextAccessor();
        services.AddMvcCore();

        services.AddDbContext<IdentitySQLiteDb>(o => o.UseSqlite($"Filename={DbFile}"));
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

        services.AddSingleton(new Fido2Configuration
        {
            ServerDomain = "localhost",
            ServerName = "Test",
            Origins = new HashSet<string> { "https://localhost" }
        });
        services.AddSingleton<IFido2>(sp => new Fido2NetLib.Fido2(sp.GetRequiredService<Fido2Configuration>(), null));
        services.AddScoped<IPasskeyService, PasskeyService>();
        services.AddScoped<ITotpService, TotpService>();
        services.AddScoped<IRecoveryService, RecoveryService>();
        services.AddSingleton<AuthRateLimiters>();
        services.AddScoped<ISecurityAuditLogger, SecurityAuditLogger>();

        _provider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _provider.Dispose();
    }

    [Fact]
    public async Task RepeatedFailedPasswordAttempts_LocksOutAccount()
    {
        const string email = "lockout-trigger@example.com";
        const string password = "Correct-Horse-1!";

        await CreateUserAsync(email, password);
        var flowToken = await GetFlowTokenAsync(email);

        for (var i = 0; i < MaxFailedAccessAttempts; i++)
        {
            using var scope = _provider.CreateScope();
            var controller = CreateController(scope.ServiceProvider);

            var result = await controller.Verify(new AuthVerifyRequest
            {
                Token = flowToken,
                Method = "password",
                Password = "definitely-wrong"
            });

            Assert.False(result.Succeeded);
        }

        using (var scope = _provider.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByEmailAsync(email);

            Assert.True(await userManager.IsLockedOutAsync(user));
        }

        // Even the correct password fails while locked out.
        using (var scope = _provider.CreateScope())
        {
            var controller = CreateController(scope.ServiceProvider);

            var result = await controller.Verify(new AuthVerifyRequest
            {
                Token = flowToken,
                Method = "password",
                Password = password
            });

            Assert.False(result.Succeeded);
        }
    }

    [Fact]
    public async Task AccountUnlocksAfterLockoutDurationPasses()
    {
        const string email = "lockout-reset@example.com";
        const string password = "Correct-Horse-2!";

        await CreateUserAsync(email, password);
        var flowToken = await GetFlowTokenAsync(email);

        for (var i = 0; i < MaxFailedAccessAttempts; i++)
        {
            using var scope = _provider.CreateScope();
            var controller = CreateController(scope.ServiceProvider);

            await controller.Verify(new AuthVerifyRequest
            {
                Token = flowToken,
                Method = "password",
                Password = "definitely-wrong"
            });
        }

        using (var scope = _provider.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByEmailAsync(email);
            Assert.True(await userManager.IsLockedOutAsync(user));

            // Simulate the lockout window having already passed.
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddSeconds(-1));
            Assert.False(await userManager.IsLockedOutAsync(user));
        }

        using (var scope = _provider.CreateScope())
        {
            var controller = CreateController(scope.ServiceProvider);

            var result = await controller.Verify(new AuthVerifyRequest
            {
                Token = flowToken,
                Method = "password",
                Password = password
            });

            Assert.True(result.Succeeded);
        }
    }

    private async Task CreateUserAsync(string email, string password)
    {
        using var scope = _provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var user = new User { UserName = email, Email = email, EmailConfirmed = true };
        var created = await userManager.CreateAsync(user, password);

        Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(e => e.Description)));
    }

    private async Task<string> GetFlowTokenAsync(string email)
    {
        using var scope = _provider.CreateScope();
        var controller = CreateController(scope.ServiceProvider);

        var options = await controller.Options(new AuthOptionsRequest { Email = email });

        return options.Token;
    }

    private static PasskeyAuthController CreateController(IServiceProvider provider)
    {
        var httpContext = new DefaultHttpContext { RequestServices = provider };

        // SignInManager resolves the request through its own injected
        // IHttpContextAccessor, separate from the controller's
        // ControllerContext.HttpContext below - both need to point at the
        // same context or SignInAsync throws "HttpContext must not be null".
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;

        var controller = ActivatorUtilities.CreateInstance<PasskeyAuthController>(provider);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
            RouteData = new RouteData(),
            ActionDescriptor = new ControllerActionDescriptor()
        };

        return controller;
    }
}
