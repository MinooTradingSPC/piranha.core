/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OtpNet;
using Piranha.AspNetCore.Identity.Models;
using Piranha.AspNetCore.Identity.Services;
using Xunit;

namespace Piranha.Tests.Identity;

/// <summary>
/// Covers #177's "multiple authentication methods registered for the same
/// user" and "secrets/tokens are not logged or returned in responses"
/// acceptance criteria.
/// </summary>
public class MultiMethodAndSecretLeakageTests : IdentityAuthTestBase
{
    private readonly CapturingLoggerProvider _loggerProvider = new();

    public MultiMethodAndSecretLeakageTests() : base(services => services.AddSingleton<IEmailSender, FakeEmailSender>())
    {
        // Added after the provider is built - LoggerFactory supports this
        // and retroactively refreshes already-resolved ILogger instances.
        Provider.GetRequiredService<ILoggerFactory>().AddProvider(_loggerProvider);
    }

    private FakeEmailSender EmailSender => (FakeEmailSender)Provider.GetRequiredService<IEmailSender>();

    [Fact]
    public async Task UserWithPasskeyAndTotpAndPassword_CanSignInWithAnyOfThem()
    {
        const string email = "multi-method@example.com";
        const string password = "Correct-Horse-1!";
        var credentialId = new byte[] { 4, 2, 4, 2 };
        var user = await CreateUserAsync(email, password);

        // Register a passkey.
        Fido2.OnMakeNewCredential = (_, _) => Task.FromResult(new Fido2NetLib.Objects.RegisteredPublicKeyCredential
        {
            Id = credentialId,
            PublicKey = new byte[] { 1, 2, 3 },
            SignCount = 0
        });
        using (var scope = Provider.CreateScope())
        {
            var controller = await CreatePasskeyControllerAsync(scope.ServiceProvider, user);
            var begin = (OkObjectResult)await controller.BeginRegistration();
            var challenge = Assert.IsType<PasskeyChallenge>(begin.Value);
            await controller.CompleteRegistration(new CompletePasskeyRegistrationRequest
            {
                Token = challenge.Token,
                DeviceName = "Device",
                AttestationResponse = new Fido2NetLib.AuthenticatorAttestationRawResponse { Id = "cred", RawId = credentialId }
            });
        }

        // Enroll TOTP.
        string totpSecret;
        using (var scope = Provider.CreateScope())
        {
            var controller = await CreateTotpControllerAsync(scope.ServiceProvider, user);
            var begin = (OkObjectResult)await controller.BeginEnrollment(new BeginTotpEnrollmentRequest());
            var enrollment = Assert.IsType<TotpEnrollment>(begin.Value);
            totpSecret = enrollment.Secret;

            await controller.ConfirmEnrollment(new ConfirmTotpEnrollmentRequest
            {
                Token = enrollment.Token,
                Code = new Totp(Base32Encoding.ToBytes(totpSecret)).ComputeTotp()
            });
        }

        // All three methods should now be listed, strongest first.
        var options = await GetOptionsAsync(email);
        Assert.Equal(new[] { "passkey", "totp", "password", "email-otp" }, options.Methods);

        // Password works.
        using (var scope = Provider.CreateScope())
        {
            var controller = CreateAuthController(scope.ServiceProvider);
            var flowToken = (await controller.Options(new AuthOptionsRequest { Email = email })).Token;
            var result = await controller.Verify(new AuthVerifyRequest { Token = flowToken, Method = "password", Password = password });
            Assert.True(result.Succeeded);
        }

        // TOTP works (a step-ahead code, since the earlier confirm already
        // consumed the current step).
        using (var scope = Provider.CreateScope())
        {
            var controller = CreateAuthController(scope.ServiceProvider);
            var flowToken = (await controller.Options(new AuthOptionsRequest { Email = email })).Token;
            var code = new Totp(Base32Encoding.ToBytes(totpSecret)).ComputeTotp(DateTime.UtcNow.AddSeconds(30));
            var result = await controller.Verify(new AuthVerifyRequest { Token = flowToken, Method = "totp", Code = code });
            Assert.True(result.Succeeded);
        }

        // Passkey works.
        Fido2.OnMakeAssertion = (_, _) => Task.FromResult(new Fido2NetLib.Objects.VerifyAssertionResult { SignCount = 1 });
        using (var scope = Provider.CreateScope())
        {
            var controller = CreateAuthController(scope.ServiceProvider);
            var flowToken = (await controller.Options(new AuthOptionsRequest { Email = email })).Token;
            var assertionOptions = (OkObjectResult)await controller.PasskeyAssertionOptions(new AuthAssertionOptionsRequest { Token = flowToken });
            var challenge = Assert.IsType<PasskeyChallenge>(assertionOptions.Value);
            var result = await controller.Verify(new AuthVerifyRequest
            {
                Token = flowToken,
                Method = "passkey",
                AssertionToken = challenge.Token,
                AssertionResponse = new Fido2NetLib.AuthenticatorAssertionRawResponse { Id = "cred", RawId = credentialId }
            });
            Assert.True(result.Succeeded);
        }
    }

    [Fact]
    public async Task SecretsAreNeverLoggedOrReturnedInResponses()
    {
        const string email = "secret-leakage@example.com";
        const string password = "Zz9-Very-Distinctive-Secret-Pwd";
        var user = await CreateUserAsync(email, password);

        // TOTP: enrollment secret and confirmation code.
        string totpSecret;
        string totpCode;
        TotpEnrollment enrollment;
        using (var scope = Provider.CreateScope())
        {
            var controller = await CreateTotpControllerAsync(scope.ServiceProvider, user);
            var begin = (OkObjectResult)await controller.BeginEnrollment(new BeginTotpEnrollmentRequest());
            enrollment = Assert.IsType<TotpEnrollment>(begin.Value);
            totpSecret = enrollment.Secret;
            totpCode = new Totp(Base32Encoding.ToBytes(totpSecret)).ComputeTotp();

            var confirm = await controller.ConfirmEnrollment(new ConfirmTotpEnrollmentRequest { Token = enrollment.Token, Code = totpCode });
            Assert.IsType<OkObjectResult>(confirm);
        }

        // Recovery: emailed code.
        string recoveryCode;
        using (var scope = Provider.CreateScope())
        {
            var controller = CreateAuthController(scope.ServiceProvider);
            var flowToken = (await controller.Options(new AuthOptionsRequest { Email = email })).Token;
            await controller.RequestEmailOtp(new EmailOtpRequestRequest { Token = flowToken });
        }
        recoveryCode = EmailSender.GetLastCode();

        // A failed password attempt too, so LoginFailed also gets exercised.
        using (var scope = Provider.CreateScope())
        {
            var controller = CreateAuthController(scope.ServiceProvider);
            var flowToken = (await controller.Options(new AuthOptionsRequest { Email = email })).Token;
            await controller.Verify(new AuthVerifyRequest { Token = flowToken, Method = "password", Password = "wrong-one" });
        }

        var secrets = new[] { password, totpSecret, totpCode, recoveryCode };

        foreach (var message in _loggerProvider.Messages)
        {
            foreach (var secret in secrets)
            {
                Assert.DoesNotContain(secret, message);
            }
        }
    }
}
