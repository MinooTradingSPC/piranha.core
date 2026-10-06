/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

namespace Piranha.AspNetCore.Identity.Models;

/// <summary>
/// Request body for beginning or regenerating a TOTP enrollment.
/// </summary>
public sealed class BeginTotpEnrollmentRequest
{
    /// <summary>
    /// Required when the user already has a confirmed authenticator -
    /// regenerating one needs proof it's really them.
    /// </summary>
    public string Password { get; set; }
}

/// <summary>
/// Request body for confirming a TOTP enrollment.
/// </summary>
public sealed class ConfirmTotpEnrollmentRequest
{
    public string Token { get; set; }
    public string Code { get; set; }
}

/// <summary>
/// Request body for revoking a TOTP enrollment.
/// </summary>
public sealed class RevokeTotpRequest
{
    public string Password { get; set; }
}
