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
using Piranha.AspNetCore.Identity.Data;

namespace Piranha.AspNetCore.Identity.Services;

/// <summary>
/// Service for registering and verifying WebAuthn/passkey credentials.
/// </summary>
public interface IPasskeyService
{
    /// <summary>
    /// Begins a new passkey registration ceremony for the given, already
    /// authenticated, user.
    /// </summary>
    /// <param name="user">The user registering a new passkey</param>
    /// <returns>The registration challenge</returns>
    Task<PasskeyChallenge> BeginRegistrationAsync(User user);

    /// <summary>
    /// Completes a passkey registration ceremony and stores the new
    /// credential for the user.
    /// </summary>
    /// <param name="user">The user registering a new passkey</param>
    /// <param name="challengeToken">The token returned by <see cref="BeginRegistrationAsync"/></param>
    /// <param name="attestationResponse">The attestation response from the browser</param>
    /// <param name="deviceName">The display name given to the credential</param>
    /// <returns>The newly stored passkey</returns>
    Task<Passkey> CompleteRegistrationAsync(User user, string challengeToken,
        AuthenticatorAttestationRawResponse attestationResponse, string deviceName);

    /// <summary>
    /// Begins a new passkey assertion (sign-in) ceremony. <paramref name="user"/>
    /// may be <c>null</c> for an email that doesn't match an account, in which
    /// case an empty credential list is returned so the response shape can't be
    /// used to tell whether the account exists.
    /// </summary>
    /// <param name="user">The user signing in, or null if unknown</param>
    /// <returns>The assertion challenge</returns>
    Task<PasskeyChallenge> BeginAssertionAsync(User user);

    /// <summary>
    /// Completes a passkey assertion ceremony.
    /// </summary>
    /// <param name="challengeToken">The token returned by <see cref="BeginAssertionAsync"/></param>
    /// <param name="assertionResponse">The assertion response from the browser</param>
    /// <returns>The result of the assertion</returns>
    Task<PasskeyAssertionResult> CompleteAssertionAsync(string challengeToken,
        AuthenticatorAssertionRawResponse assertionResponse);

    /// <summary>
    /// Gets all passkeys registered for the given user.
    /// </summary>
    /// <param name="userId">The user id</param>
    Task<IReadOnlyList<Passkey>> GetPasskeysAsync(Guid userId);

    /// <summary>
    /// Removes a passkey belonging to the given user.
    /// </summary>
    /// <param name="userId">The user id</param>
    /// <param name="passkeyId">The passkey id</param>
    /// <returns>If a passkey was removed</returns>
    Task<bool> RemovePasskeyAsync(Guid userId, Guid passkeyId);

    /// <summary>
    /// Renames a passkey belonging to the given user.
    /// </summary>
    /// <param name="userId">The user id</param>
    /// <param name="passkeyId">The passkey id</param>
    /// <param name="deviceName">The new display name</param>
    /// <returns>If a passkey was renamed</returns>
    Task<bool> RenamePasskeyAsync(Guid userId, Guid passkeyId, string deviceName);
}

/// <summary>
/// A registration or assertion challenge handed to the browser, together with
/// the opaque, signed token used to recover the original challenge on completion.
/// </summary>
public sealed class PasskeyChallenge
{
    /// <summary>
    /// Gets/sets the opaque token identifying this challenge.
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// Gets/sets the WebAuthn options, serialized as JSON for the browser.
    /// </summary>
    public string OptionsJson { get; set; }
}

/// <summary>
/// The result of completing a passkey assertion ceremony.
/// </summary>
public sealed class PasskeyAssertionResult
{
    /// <summary>
    /// Gets/sets if the assertion succeeded.
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// Gets/sets the id of the user that was authenticated, if the
    /// assertion succeeded.
    /// </summary>
    public Guid? UserId { get; set; }
}
