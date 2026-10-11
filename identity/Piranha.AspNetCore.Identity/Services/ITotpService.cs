/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using Piranha.AspNetCore.Identity.Data;

namespace Piranha.AspNetCore.Identity.Services;

/// <summary>
/// Service for enrolling and verifying TOTP authenticator-app credentials.
/// </summary>
public interface ITotpService
{
    /// <summary>
    /// Begins a new enrollment for the given user. Nothing is persisted
    /// until <see cref="ConfirmEnrollmentAsync"/> succeeds, so an
    /// abandoned enrollment attempt never disturbs an existing, working
    /// credential.
    /// </summary>
    /// <param name="user">The user enrolling an authenticator app</param>
    /// <returns>The enrollment challenge</returns>
    TotpEnrollment BeginEnrollment(User user);

    /// <summary>
    /// Confirms an enrollment with a code from the authenticator app,
    /// replacing any previous credential for the user only on success.
    /// </summary>
    /// <param name="user">The user enrolling an authenticator app</param>
    /// <param name="enrollmentToken">The token from <see cref="BeginEnrollment"/></param>
    /// <param name="code">The 6-digit code to confirm with</param>
    /// <returns>If the enrollment was confirmed</returns>
    Task<bool> ConfirmEnrollmentAsync(User user, string enrollmentToken, string code);

    /// <summary>
    /// Gets whether the given user has a confirmed authenticator credential,
    /// and when it was confirmed.
    /// </summary>
    /// <param name="userId">The user id</param>
    Task<TotpStatus> GetStatusAsync(Guid userId);

    /// <summary>
    /// Verifies a code against the user's confirmed credential, rejecting a
    /// code from a time step that was already accepted (replay protection).
    /// </summary>
    /// <param name="userId">The user id</param>
    /// <param name="code">The 6-digit code</param>
    /// <returns>If the code was valid</returns>
    Task<bool> VerifyCodeAsync(Guid userId, string code);

    /// <summary>
    /// Revokes the user's confirmed authenticator credential, if any.
    /// </summary>
    /// <param name="userId">The user id</param>
    /// <returns>If a credential was revoked</returns>
    Task<bool> RevokeAsync(Guid userId);
}

/// <summary>
/// An enrollment challenge handed to the browser: the manual-entry secret,
/// the standard <c>otpauth://</c> provisioning URI, a ready-to-display QR
/// code, and the opaque token used to confirm it.
/// </summary>
public sealed class TotpEnrollment
{
    public string Token { get; set; }
    public string Secret { get; set; }
    public string ProvisioningUri { get; set; }
    public string QrCodePngBase64 { get; set; }
}

/// <summary>
/// The current TOTP enrollment status for a user.
/// </summary>
public sealed class TotpStatus
{
    public bool Enrolled { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}
