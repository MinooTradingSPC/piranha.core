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
/// Service for the email-otp recovery/bootstrap sign-in method: a
/// single-use, short-lived numeric code emailed to the user.
/// </summary>
public interface IRecoveryService
{
    /// <summary>
    /// Generates a new code, stores it hashed, and emails it to the user.
    /// Never call this for an email that doesn't match an account - the
    /// caller is responsible for that check, so a missing account never
    /// triggers a real send.
    /// </summary>
    /// <param name="user">The user requesting a recovery code</param>
    Task RequestCodeAsync(User user);

    /// <summary>
    /// Verifies a code, consuming it on success so it can't be reused.
    /// </summary>
    /// <param name="userId">The user id</param>
    /// <param name="code">The 6-digit code</param>
    /// <returns>If the code was valid and unused</returns>
    Task<bool> VerifyCodeAsync(Guid userId, string code);
}
