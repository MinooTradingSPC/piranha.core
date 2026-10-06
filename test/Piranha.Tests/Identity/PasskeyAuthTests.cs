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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Piranha.AspNetCore.Identity.Data;
using Piranha.AspNetCore.Identity.Models;
using Piranha.AspNetCore.Identity.Services;
using Xunit;

namespace Piranha.Tests.Identity;

/// <summary>
/// Covers #177's "passkey registration and login happy path", "passkey
/// login failure cases", and "authentication method revocation" coverage.
/// The actual WebAuthn cryptography is faked (see FakeFido2) since a real
/// ceremony needs an actual authenticator; everything else - challenge
/// tokens, credential storage, the sign-in flow, revocation - is real.
/// </summary>
public class PasskeyAuthTests : IdentityAuthTestBase
{
    private static readonly byte[] CredentialId = { 9, 9, 9 };

    [Fact]
    public async Task RegisterThenSignIn_Succeeds()
    {
        const string email = "passkey-happy-path@example.com";
        var user = await CreateUserAsync(email, "Correct-Horse-1!");

        await RegisterPasskeyAsync(user);

        var flowToken = (await GetOptionsAsync(email)).Token;

        Fido2.OnMakeAssertion = (_, _) => Task.FromResult(new VerifyAssertionResult { SignCount = 1 });

        using var scope = Provider.CreateScope();
        var authController = CreateAuthController(scope.ServiceProvider);

        var assertionOptionsResult = (OkObjectResult)await authController.PasskeyAssertionOptions(
            new AuthAssertionOptionsRequest { Token = flowToken });
        var challenge = Assert.IsType<PasskeyChallenge>(assertionOptionsResult.Value);

        var result = await authController.Verify(new AuthVerifyRequest
        {
            Token = flowToken,
            Method = "passkey",
            AssertionToken = challenge.Token,
            AssertionResponse = new AuthenticatorAssertionRawResponse { Id = "cred", RawId = CredentialId }
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task SignInWithUnregisteredCredential_Fails()
    {
        const string email = "passkey-unknown-credential@example.com";
        var user = await CreateUserAsync(email, "Correct-Horse-1!");
        await RegisterPasskeyAsync(user);

        var flowToken = (await GetOptionsAsync(email)).Token;

        using var scope = Provider.CreateScope();
        var authController = CreateAuthController(scope.ServiceProvider);

        var assertionOptionsResult = (OkObjectResult)await authController.PasskeyAssertionOptions(
            new AuthAssertionOptionsRequest { Token = flowToken });
        var challenge = Assert.IsType<PasskeyChallenge>(assertionOptionsResult.Value);

        var result = await authController.Verify(new AuthVerifyRequest
        {
            Token = flowToken,
            Method = "passkey",
            AssertionToken = challenge.Token,
            // A credential id that was never registered.
            AssertionResponse = new AuthenticatorAssertionRawResponse { Id = "other", RawId = new byte[] { 1, 1, 1 } }
        });

        Assert.False(result.Succeeded);
        Assert.Equal("The code you entered is incorrect or has expired.", result.Message);
    }

    [Fact]
    public async Task SignInWhenFido2RejectsAssertion_Fails()
    {
        const string email = "passkey-verification-failure@example.com";
        var user = await CreateUserAsync(email, "Correct-Horse-1!");
        await RegisterPasskeyAsync(user);

        var flowToken = (await GetOptionsAsync(email)).Token;

        Fido2.OnMakeAssertion = (_, _) => throw new Fido2VerificationException("signature invalid");

        using var scope = Provider.CreateScope();
        var authController = CreateAuthController(scope.ServiceProvider);

        var assertionOptionsResult = (OkObjectResult)await authController.PasskeyAssertionOptions(
            new AuthAssertionOptionsRequest { Token = flowToken });
        var challenge = Assert.IsType<PasskeyChallenge>(assertionOptionsResult.Value);

        var result = await authController.Verify(new AuthVerifyRequest
        {
            Token = flowToken,
            Method = "passkey",
            AssertionToken = challenge.Token,
            AssertionResponse = new AuthenticatorAssertionRawResponse { Id = "cred", RawId = CredentialId }
        });

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task RevokedPasskey_CanNoLongerSignIn()
    {
        const string email = "passkey-revoked@example.com";
        const string password = "Correct-Horse-1!";
        var user = await CreateUserAsync(email, password);

        var passkeyId = await RegisterPasskeyAsync(user);

        using (var scope = Provider.CreateScope())
        {
            var passkeyController = await CreatePasskeyControllerAsync(scope.ServiceProvider, user);

            // Step-up verification: wrong password is rejected.
            var wrongPasswordResult = await passkeyController.Remove(passkeyId, new RemovePasskeyRequest { Password = "nope" });
            Assert.IsType<BadRequestObjectResult>(wrongPasswordResult);

            var removeResult = await passkeyController.Remove(passkeyId, new RemovePasskeyRequest { Password = password });
            Assert.IsType<OkResult>(removeResult);
        }

        var flowToken = (await GetOptionsAsync(email)).Token;

        // With the passkey gone, discovery no longer offers it.
        Assert.DoesNotContain("passkey", (await GetOptionsAsync(email)).Methods);

        Fido2.OnMakeAssertion = (_, _) => Task.FromResult(new VerifyAssertionResult { SignCount = 1 });

        using var authScope = Provider.CreateScope();
        var authController = CreateAuthController(authScope.ServiceProvider);

        var assertionOptionsResult = (OkObjectResult)await authController.PasskeyAssertionOptions(
            new AuthAssertionOptionsRequest { Token = flowToken });
        var challenge = Assert.IsType<PasskeyChallenge>(assertionOptionsResult.Value);

        var result = await authController.Verify(new AuthVerifyRequest
        {
            Token = flowToken,
            Method = "passkey",
            AssertionToken = challenge.Token,
            AssertionResponse = new AuthenticatorAssertionRawResponse { Id = "cred", RawId = CredentialId }
        });

        Assert.False(result.Succeeded);
    }

    /// <summary>
    /// Runs a full registration ceremony (through the real self-service
    /// controller) and returns the new passkey's id.
    /// </summary>
    private async Task<Guid> RegisterPasskeyAsync(User user)
    {
        Fido2.OnMakeNewCredential = (_, _) => Task.FromResult(new RegisteredPublicKeyCredential
        {
            Id = CredentialId,
            PublicKey = new byte[] { 1, 2, 3 },
            SignCount = 0
        });

        using var scope = Provider.CreateScope();
        var controller = await CreatePasskeyControllerAsync(scope.ServiceProvider, user);

        var beginResult = (OkObjectResult)await controller.BeginRegistration();
        var challenge = Assert.IsType<PasskeyChallenge>(beginResult.Value);

        var completeResult = (OkObjectResult)await controller.CompleteRegistration(new CompletePasskeyRegistrationRequest
        {
            Token = challenge.Token,
            DeviceName = "Test device",
            AttestationResponse = new AuthenticatorAttestationRawResponse { Id = "cred", RawId = CredentialId }
        });
        var passkeys = Assert.IsAssignableFrom<IEnumerable<PasskeyListItem>>(completeResult.Value);

        return passkeys.Single().Id;
    }
}
