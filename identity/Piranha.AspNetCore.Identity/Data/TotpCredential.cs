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
/// A confirmed TOTP authenticator-app enrollment for a Manager user. There
/// is at most one per user - re-enrolling replaces it, but only once the
/// new secret has been confirmed with a valid code (see
/// <see cref="Services.ITotpService"/>), so an abandoned re-enrollment
/// attempt never invalidates a working one.
/// </summary>
public sealed class TotpCredential
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
    /// Gets/sets the TOTP secret, encrypted at rest.
    /// </summary>
    public string EncryptedSecret { get; set; }

    /// <summary>
    /// Gets/sets when the credential was confirmed.
    /// </summary>
    public DateTime ConfirmedAt { get; set; }

    /// <summary>
    /// Gets/sets when the credential was last used to sign in.
    /// </summary>
    public DateTime? LastUsed { get; set; }

    /// <summary>
    /// Gets/sets the most recent TOTP time step that was accepted, so the
    /// same code (or an older one) can't be replayed within its validity
    /// window.
    /// </summary>
    public long LastAcceptedTimeStep { get; set; }

    /// <summary>
    /// Gets/sets the user this credential belongs to.
    /// </summary>
    public User User { get; set; }
}
