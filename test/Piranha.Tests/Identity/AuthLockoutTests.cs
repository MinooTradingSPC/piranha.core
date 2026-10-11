/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Piranha.AspNetCore.Identity.Data;
using Piranha.AspNetCore.Identity.Models;
using Xunit;

namespace Piranha.Tests.Identity;

/// <summary>
/// Covers #176/#177's lockout acceptance criteria: repeated failed login
/// attempts trigger lockout, and the account works again once the lockout
/// window has passed. Exercises the real ASP.NET Core Identity lockout
/// machinery through PasskeyAuthController.Verify, backed by a real
/// (temp-file) SQLite database, rather than mocking it out.
/// </summary>
public class AuthLockoutTests : IdentityAuthTestBase
{
    [Fact]
    public async Task RepeatedFailedPasswordAttempts_LocksOutAccount()
    {
        const string email = "lockout-trigger@example.com";
        const string password = "Correct-Horse-1!";

        await CreateUserAsync(email, password);
        var flowToken = (await GetOptionsAsync(email)).Token;

        for (var i = 0; i < MaxFailedAccessAttempts; i++)
        {
            using var scope = Provider.CreateScope();
            var controller = CreateAuthController(scope.ServiceProvider);

            var result = await controller.Verify(new AuthVerifyRequest
            {
                Token = flowToken,
                Method = "password",
                Password = "definitely-wrong"
            });

            Assert.False(result.Succeeded);
        }

        using (var scope = Provider.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByEmailAsync(email);

            Assert.True(await userManager.IsLockedOutAsync(user));
        }

        // Even the correct password fails while locked out.
        using (var scope = Provider.CreateScope())
        {
            var controller = CreateAuthController(scope.ServiceProvider);

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
        var flowToken = (await GetOptionsAsync(email)).Token;

        for (var i = 0; i < MaxFailedAccessAttempts; i++)
        {
            using var scope = Provider.CreateScope();
            var controller = CreateAuthController(scope.ServiceProvider);

            await controller.Verify(new AuthVerifyRequest
            {
                Token = flowToken,
                Method = "password",
                Password = "definitely-wrong"
            });
        }

        using (var scope = Provider.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByEmailAsync(email);
            Assert.True(await userManager.IsLockedOutAsync(user));

            // Simulate the lockout window having already passed.
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddSeconds(-1));
            Assert.False(await userManager.IsLockedOutAsync(user));
        }

        using (var scope = Provider.CreateScope())
        {
            var controller = CreateAuthController(scope.ServiceProvider);

            var result = await controller.Verify(new AuthVerifyRequest
            {
                Token = flowToken,
                Method = "password",
                Password = password
            });

            Assert.True(result.Succeeded);
        }
    }
}
