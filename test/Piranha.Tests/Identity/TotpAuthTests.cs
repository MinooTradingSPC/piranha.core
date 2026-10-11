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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;
using Piranha.AspNetCore.Identity.Data;
using Piranha.AspNetCore.Identity.Models;
using Piranha.AspNetCore.Identity.Services;
using Xunit;

namespace Piranha.Tests.Identity;

/// <summary>
/// Covers #177's "TOTP enrollment and verification", "TOTP failed attempt
/// throttling", and "authentication method revocation" coverage. Uses the
/// real Otp.NET algorithm throughout - there's nothing to fake here, only
/// WebAuthn needs that.
/// </summary>
public class TotpAuthTests : IdentityAuthTestBase
{
    [Fact]
    public async Task EnrollConfirmThenSignIn_Succeeds()
    {
        const string email = "totp-happy-path@example.com";
        var user = await CreateUserAsync(email, "Correct-Horse-1!");

        var secret = await EnrollAndConfirmAsync(user);
        var flowToken = (await GetOptionsAsync(email)).Token;

        // Confirmation already consumed the *current* time step; use the
        // next one (still valid under VerificationWindow(1,1)) so this
        // doesn't race the 30-second window confirmation just used.
        var code = new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp(DateTime.UtcNow.AddSeconds(30));

        using var scope = Provider.CreateScope();
        var authController = CreateAuthController(scope.ServiceProvider);

        var result = await authController.Verify(new AuthVerifyRequest
        {
            Token = flowToken,
            Method = "totp",
            Code = code
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ConfirmEnrollment_WithWrongCode_Fails()
    {
        const string email = "totp-wrong-confirm-code@example.com";
        var user = await CreateUserAsync(email, "Correct-Horse-1!");

        using var scope = Provider.CreateScope();
        var controller = await CreateTotpControllerAsync(scope.ServiceProvider, user);

        var beginResult = (OkObjectResult)await controller.BeginEnrollment(new BeginTotpEnrollmentRequest());
        var enrollment = Assert.IsType<TotpEnrollment>(beginResult.Value);

        var confirmResult = await controller.ConfirmEnrollment(new ConfirmTotpEnrollmentRequest
        {
            Token = enrollment.Token,
            Code = "000000"
        });

        Assert.IsType<BadRequestObjectResult>(confirmResult);

        var status = Assert.IsType<TotpStatus>(((OkObjectResult)await controller.Status()).Value);
        Assert.False(status.Enrolled);
    }

    [Fact]
    public async Task VerifyWithSameCodeTwice_SecondAttemptFails()
    {
        const string email = "totp-replay@example.com";
        var user = await CreateUserAsync(email, "Correct-Horse-1!");

        var secret = await EnrollAndConfirmAsync(user);

        // Confirmation itself already consumed the *current* time step, so
        // a code for "now" would spuriously fail here too (not because of
        // replay protection, but because it's the same step confirmation
        // just used). Use the next step instead - VerificationWindow(1,1)
        // still accepts it as a candidate right now, so this deterministically
        // tests "a fresh code works once, then replaying it fails" without
        // racing the 30-second window confirmation already used.
        var code = new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp(DateTime.UtcNow.AddSeconds(30));

        using var scope = Provider.CreateScope();
        var totpService = scope.ServiceProvider.GetRequiredService<ITotpService>();

        Assert.True(await totpService.VerifyCodeAsync(user.Id, code));
        // Replaying the exact same code must not work a second time.
        Assert.False(await totpService.VerifyCodeAsync(user.Id, code));
    }

    [Fact]
    public async Task ConfirmationCode_CannotBeReplayedForSignIn()
    {
        const string email = "totp-confirmation-replay@example.com";
        var user = await CreateUserAsync(email, "Correct-Horse-1!");

        string codeUsedToConfirm;
        using (var scope = Provider.CreateScope())
        {
            var controller = await CreateTotpControllerAsync(scope.ServiceProvider, user);

            var beginResult = (OkObjectResult)await controller.BeginEnrollment(new BeginTotpEnrollmentRequest());
            var enrollment = Assert.IsType<TotpEnrollment>(beginResult.Value);

            codeUsedToConfirm = new Totp(Base32Encoding.ToBytes(enrollment.Secret)).ComputeTotp();

            var confirmResult = await controller.ConfirmEnrollment(new ConfirmTotpEnrollmentRequest
            {
                Token = enrollment.Token,
                Code = codeUsedToConfirm
            });
            Assert.IsType<OkObjectResult>(confirmResult);
        }

        using var verifyScope = Provider.CreateScope();
        var totpService = verifyScope.ServiceProvider.GetRequiredService<ITotpService>();

        // The exact code that confirmed enrollment must not also work to
        // sign in - confirmation itself counts as the code's one use.
        Assert.False(await totpService.VerifyCodeAsync(user.Id, codeUsedToConfirm));
    }

    [Fact]
    public async Task RepeatedFailedTotpAttempts_LocksOutAccount()
    {
        const string email = "totp-throttling@example.com";
        var user = await CreateUserAsync(email, "Correct-Horse-1!");

        await EnrollAndConfirmAsync(user);
        var flowToken = (await GetOptionsAsync(email)).Token;

        for (var i = 0; i < MaxFailedAccessAttempts; i++)
        {
            using var scope = Provider.CreateScope();
            var authController = CreateAuthController(scope.ServiceProvider);

            await authController.Verify(new AuthVerifyRequest
            {
                Token = flowToken,
                Method = "totp",
                Code = "000000"
            });
        }

        using var lockoutScope = Provider.CreateScope();
        var userManager = lockoutScope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var reloaded = await userManager.FindByEmailAsync(email);

        Assert.True(await userManager.IsLockedOutAsync(reloaded));
    }

    [Fact]
    public async Task RevokedTotp_CanNoLongerSignIn()
    {
        const string email = "totp-revoked@example.com";
        const string password = "Correct-Horse-1!";
        var user = await CreateUserAsync(email, password);

        var secret = await EnrollAndConfirmAsync(user);

        using (var scope = Provider.CreateScope())
        {
            var controller = await CreateTotpControllerAsync(scope.ServiceProvider, user);

            var wrongPasswordResult = await controller.Revoke(new RevokeTotpRequest { Password = "nope" });
            Assert.IsType<BadRequestObjectResult>(wrongPasswordResult);

            var revokeResult = await controller.Revoke(new RevokeTotpRequest { Password = password });
            Assert.IsType<OkResult>(revokeResult);
        }

        var flowToken = (await GetOptionsAsync(email)).Token;
        Assert.DoesNotContain("totp", (await GetOptionsAsync(email)).Methods);

        using var authScope = Provider.CreateScope();
        var authController = CreateAuthController(authScope.ServiceProvider);

        // A step-ahead code, same reasoning as the happy-path test: makes
        // sure this fails because the credential is gone, not because it
        // collided with the time step confirmation already consumed.
        var code = new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp(DateTime.UtcNow.AddSeconds(30));

        var result = await authController.Verify(new AuthVerifyRequest
        {
            Token = flowToken,
            Method = "totp",
            Code = code
        });

        Assert.False(result.Succeeded);
    }

    /// <summary>
    /// Runs a full enrollment ceremony through the real self-service
    /// controller and returns the confirmed secret (for computing codes).
    /// </summary>
    private async Task<string> EnrollAndConfirmAsync(User user)
    {
        using var scope = Provider.CreateScope();
        var controller = await CreateTotpControllerAsync(scope.ServiceProvider, user);

        var beginResult = (OkObjectResult)await controller.BeginEnrollment(new BeginTotpEnrollmentRequest());
        var enrollment = Assert.IsType<TotpEnrollment>(beginResult.Value);

        var code = new Totp(Base32Encoding.ToBytes(enrollment.Secret)).ComputeTotp();
        var confirmResult = await controller.ConfirmEnrollment(new ConfirmTotpEnrollmentRequest
        {
            Token = enrollment.Token,
            Code = code
        });
        Assert.IsType<OkObjectResult>(confirmResult);

        return enrollment.Secret;
    }
}
