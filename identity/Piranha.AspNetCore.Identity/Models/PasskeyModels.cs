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

namespace Piranha.AspNetCore.Identity.Models;

/// <summary>
/// A registered passkey, as shown in the Manager's account security settings.
/// </summary>
public sealed class PasskeyListItem
{
    public Guid Id { get; set; }
    public string DeviceName { get; set; }
    public DateTime Created { get; set; }
    public DateTime? LastUsed { get; set; }
}

/// <summary>
/// Request body for completing a passkey registration.
/// </summary>
public sealed class CompletePasskeyRegistrationRequest
{
    public string Token { get; set; }
    public string DeviceName { get; set; }
    public AuthenticatorAttestationRawResponse AttestationResponse { get; set; }
}

/// <summary>
/// Request body for renaming a passkey.
/// </summary>
public sealed class RenamePasskeyRequest
{
    public string DeviceName { get; set; }
}

/// <summary>
/// Request body for removing a passkey. Requires the account's password
/// as step-up verification.
/// </summary>
public sealed class RemovePasskeyRequest
{
    public string Password { get; set; }
}

/// <summary>
/// Request body for <c>POST /manager/auth/options</c>.
/// </summary>
public sealed class AuthOptionsRequest
{
    public string Email { get; set; }
}

/// <summary>
/// Response body for <c>POST /manager/auth/options</c>. Always has the same
/// shape regardless of whether <see cref="AuthOptionsRequest.Email"/> matches
/// an account, so it can't be used to enumerate accounts.
/// </summary>
public sealed class AuthOptionsResponse
{
    public string Token { get; set; }
    public string[] Methods { get; set; }
}

/// <summary>
/// Request body for <c>POST /manager/auth/passkey/assertion-options</c>.
/// </summary>
public sealed class AuthAssertionOptionsRequest
{
    public string Token { get; set; }
}

/// <summary>
/// Request body for <c>POST /manager/auth/email-otp/request</c>.
/// </summary>
public sealed class EmailOtpRequestRequest
{
    /// <summary>
    /// The flow token from <c>POST /manager/auth/options</c>.
    /// </summary>
    public string Token { get; set; }
}

/// <summary>
/// Request body for <c>POST /manager/auth/verify</c>.
/// </summary>
public sealed class AuthVerifyRequest
{
    /// <summary>
    /// The flow token from <c>POST /manager/auth/options</c>.
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// "password", "passkey", "totp", or "email-otp".
    /// </summary>
    public string Method { get; set; }

    /// <summary>
    /// Required when <see cref="Method"/> is "password".
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// Required when <see cref="Method"/> is "totp" or "email-otp": the
    /// 6-digit code.
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Required when <see cref="Method"/> is "passkey": the token from
    /// <c>POST /manager/auth/passkey/assertion-options</c>.
    /// </summary>
    public string AssertionToken { get; set; }

    /// <summary>
    /// Required when <see cref="Method"/> is "passkey".
    /// </summary>
    public AuthenticatorAssertionRawResponse AssertionResponse { get; set; }
}

/// <summary>
/// Response body for <c>POST /manager/auth/verify</c>. Every failure, for
/// whatever reason, returns the same generic message.
/// </summary>
public sealed class AuthVerifyResponse
{
    public bool Succeeded { get; set; }
    public string Message { get; set; }
    public string ReturnUrl { get; set; }

    /// <summary>
    /// True when the user signed in with the email-otp recovery method and
    /// should be prompted to register a stronger one.
    /// </summary>
    public bool PromptStrongMethodSetup { get; set; }
}
