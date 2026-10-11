/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Piranha.AspNetCore.Identity;
using Piranha.AspNetCore.Identity.Models;
using Piranha.AspNetCore.Identity.Services;
using Xunit;

namespace Piranha.Tests.Identity;

/// <summary>
/// Covers #177's "email OTP/magic-link token expiry and single-use
/// behavior" coverage, plus the recovery slice of "user enumeration
/// resistance". A FakeEmailSender captures what would have been sent so
/// tests read the code the way a real recipient would - out of the email -
/// rather than reaching into RecoveryService internals.
/// </summary>
public class RecoveryAuthTests : IdentityAuthTestBase
{
    public RecoveryAuthTests() : base(services => services.AddSingleton<IEmailSender, FakeEmailSender>())
    {
    }

    private FakeEmailSender EmailSender => (FakeEmailSender)Provider.GetRequiredService<IEmailSender>();

    [Fact]
    public async Task RequestThenVerify_SignsIn()
    {
        const string email = "recovery-happy-path@example.com";
        await CreateUserAsync(email, "Correct-Horse-1!");

        var flowToken = (await GetOptionsAsync(email)).Token;

        using (var scope = Provider.CreateScope())
        {
            var controller = CreateAuthController(scope.ServiceProvider);
            await controller.RequestEmailOtp(new EmailOtpRequestRequest { Token = flowToken });
        }

        var code = EmailSender.GetLastCode();
        Assert.NotNull(code);

        using var verifyScope = Provider.CreateScope();
        var authController = CreateAuthController(verifyScope.ServiceProvider);

        var result = await authController.Verify(new AuthVerifyRequest
        {
            Token = flowToken,
            Method = "email-otp",
            Code = code
        });

        Assert.True(result.Succeeded);
        Assert.True(result.PromptStrongMethodSetup);
    }

    [Fact]
    public async Task RequestForUnknownEmail_SendsNothingButStillReturnsGenericResponse()
    {
        var response = await GetOptionsAsync("recovery-nobody@example.com");

        using var scope = Provider.CreateScope();
        var controller = CreateAuthController(scope.ServiceProvider);

        var result = await controller.RequestEmailOtp(new EmailOtpRequestRequest { Token = response.Token });

        Assert.IsType<Microsoft.AspNetCore.Mvc.OkResult>(result);
        Assert.Empty(EmailSender.SentMessages);
    }

    [Fact]
    public async Task Code_IsSingleUse()
    {
        const string email = "recovery-single-use@example.com";
        await CreateUserAsync(email, "Correct-Horse-1!");

        var flowToken = (await GetOptionsAsync(email)).Token;

        using (var scope = Provider.CreateScope())
        {
            var controller = CreateAuthController(scope.ServiceProvider);
            await controller.RequestEmailOtp(new EmailOtpRequestRequest { Token = flowToken });
        }

        var code = EmailSender.GetLastCode();

        using (var scope = Provider.CreateScope())
        {
            var authController = CreateAuthController(scope.ServiceProvider);
            var first = await authController.Verify(new AuthVerifyRequest { Token = flowToken, Method = "email-otp", Code = code });
            Assert.True(first.Succeeded);
        }

        using (var scope = Provider.CreateScope())
        {
            var authController = CreateAuthController(scope.ServiceProvider);
            var second = await authController.Verify(new AuthVerifyRequest { Token = flowToken, Method = "email-otp", Code = code });
            Assert.False(second.Succeeded);
        }
    }

    [Fact]
    public async Task ExpiredCode_Fails()
    {
        const string email = "recovery-expired@example.com";
        await CreateUserAsync(email, "Correct-Horse-1!");

        var flowToken = (await GetOptionsAsync(email)).Token;

        using (var scope = Provider.CreateScope())
        {
            var controller = CreateAuthController(scope.ServiceProvider);
            await controller.RequestEmailOtp(new EmailOtpRequestRequest { Token = flowToken });
        }

        var code = EmailSender.GetLastCode();

        // Simulate the code's expiry window having already passed.
        using (var scope = Provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Piranha.AspNetCore.Identity.IDb>();
            var token = await db.RecoveryTokens.SingleAsync();
            token.ExpiresUtc = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        using var verifyScope = Provider.CreateScope();
        var authController = CreateAuthController(verifyScope.ServiceProvider);

        var result = await authController.Verify(new AuthVerifyRequest
        {
            Token = flowToken,
            Method = "email-otp",
            Code = code
        });

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task WrongCode_Fails()
    {
        const string email = "recovery-wrong-code@example.com";
        await CreateUserAsync(email, "Correct-Horse-1!");

        var flowToken = (await GetOptionsAsync(email)).Token;

        using (var scope = Provider.CreateScope())
        {
            var controller = CreateAuthController(scope.ServiceProvider);
            await controller.RequestEmailOtp(new EmailOtpRequestRequest { Token = flowToken });
        }

        using var verifyScope = Provider.CreateScope();
        var authController = CreateAuthController(verifyScope.ServiceProvider);

        var result = await authController.Verify(new AuthVerifyRequest
        {
            Token = flowToken,
            Method = "email-otp",
            Code = "000000"
        });

        Assert.False(result.Succeeded);
        Assert.Equal("The code you entered is incorrect or has expired.", result.Message);
    }
}
