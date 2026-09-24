/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

namespace Piranha.AspNetCore.Identity.Data;

/// <summary>
/// A registered WebAuthn/passkey credential for a Manager user.
/// </summary>
public sealed class Passkey
{
    /// <summary>
    /// Gets/sets the unique id.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets/sets the id of the user this credential belongs to.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets/sets the WebAuthn credential id, as returned by the authenticator.
    /// </summary>
    public byte[] CredentialId { get; set; }

    /// <summary>
    /// Gets/sets the credential's public key.
    /// </summary>
    public byte[] PublicKey { get; set; }

    /// <summary>
    /// Gets/sets the last known signature counter, used to detect
    /// cloned authenticators.
    /// </summary>
    public uint SignatureCounter { get; set; }

    /// <summary>
    /// Gets/sets the user-supplied display name for the device
    /// this credential was registered from.
    /// </summary>
    public string DeviceName { get; set; }

    /// <summary>
    /// Gets/sets when the credential was registered.
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// Gets/sets when the credential was last used to sign in.
    /// </summary>
    public DateTime? LastUsed { get; set; }

    /// <summary>
    /// Gets/sets the user this credential belongs to.
    /// </summary>
    public User User { get; set; }
}
