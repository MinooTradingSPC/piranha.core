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
using Fido2NetLib.Objects;
using Microsoft.Extensions.DependencyInjection;
using Piranha.AspNetCore.Identity.Controllers;
using Piranha.AspNetCore.Identity.Models;
using Piranha.AspNetCore.Identity.Services;
using Xunit;

namespace Piranha.Tests.Identity;

/// <summary>
/// Covers #177's "email-first method discovery" and "user enumeration
/// resistance" coverage: manager/auth/options must list exactly the
/// methods a real account has confirmed, in strongest-first order, and an
/// unknown email must get a response indistinguishable from a real
/// account with no strong methods registered.
/// </summary>
public class AuthOptionsDiscoveryTests : IdentityAuthTestBase
{
    [Fact]
    public async Task UnknownEmail_OffersOnlyPasswordAndEmailOtp()
    {
        var response = await GetOptionsAsync("nobody-at-all@example.com");

        Assert.Equal(new[] { "password", "email-otp" }, response.Methods);
        Assert.False(string.IsNullOrEmpty(response.Token));
    }

    [Fact]
    public async Task KnownAccountWithNoStrongMethods_OffersSameMethodsAsUnknownEmail()
    {
        const string email = "no-strong-methods@example.com";
        await CreateUserAsync(email, "Correct-Horse-1!");

        var knownResponse = await GetOptionsAsync(email);
        var unknownResponse = await GetOptionsAsync("still-nobody@example.com");

        // The whole point: a real account with only a password looks
        // identical to an email that doesn't exist at all.
        Assert.Equal(unknownResponse.Methods, knownResponse.Methods);
        Assert.Equal(new[] { "password", "email-otp" }, knownResponse.Methods);
    }

    [Fact]
    public async Task AccountWithConfirmedTotp_ListsTotpBeforePasswordAndEmailOtp()
    {
        const string email = "has-totp@example.com";
        var user = await CreateUserAsync(email, "Correct-Horse-1!");

        using (var scope = Provider.CreateScope())
        {
            var controller = await CreateTotpControllerAsync(scope.ServiceProvider, user);
            var enrollResult = await controller.BeginEnrollment(new BeginTotpEnrollmentRequest());
            var enrollment = Assert.IsType<TotpEnrollment>(((Microsoft.AspNetCore.Mvc.OkObjectResult)enrollResult).Value);

            var code = new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(enrollment.Secret)).ComputeTotp();
            var confirmResult = await controller.ConfirmEnrollment(new ConfirmTotpEnrollmentRequest
            {
                Token = enrollment.Token,
                Code = code
            });
            Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(confirmResult);
        }

        var response = await GetOptionsAsync(email);

        Assert.Equal(new[] { "totp", "password", "email-otp" }, response.Methods);
    }

    [Fact]
    public async Task AccountWithConfirmedPasskey_ListsPasskeyFirst()
    {
        const string email = "has-passkey@example.com";
        var user = await CreateUserAsync(email, "Correct-Horse-1!");

        Fido2.OnMakeNewCredential = (_, _) => Task.FromResult(new RegisteredPublicKeyCredential
        {
            Id = new byte[] { 9, 9, 9 },
            PublicKey = new byte[] { 1, 2, 3 },
            SignCount = 0
        });

        using (var scope = Provider.CreateScope())
        {
            var controller = await CreatePasskeyControllerAsync(scope.ServiceProvider, user);
            var beginResult = (Microsoft.AspNetCore.Mvc.OkObjectResult)await controller.BeginRegistration();
            var challenge = Assert.IsType<PasskeyChallenge>(beginResult.Value);

            var completeResult = await controller.CompleteRegistration(new CompletePasskeyRegistrationRequest
            {
                Token = challenge.Token,
                DeviceName = "Test device",
                AttestationResponse = new AuthenticatorAttestationRawResponse { Id = "id", RawId = new byte[] { 9, 9, 9 } }
            });
            Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(completeResult);
        }

        var response = await GetOptionsAsync(email);

        Assert.Equal("passkey", response.Methods[0]);
        Assert.Contains("password", response.Methods);
        Assert.Contains("email-otp", response.Methods);
    }
}
