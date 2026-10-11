/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

namespace Piranha.AspNetCore.Identity.Services;

/// <summary>
/// Central sink for security-sensitive Manager authentication events -
/// logins, enrollments, revocations, lockouts, and rate-limit hits. Every
/// call site reports through the same shape (timestamp, user id, IP,
/// user-agent, method, result) so an operator can find every event of a
/// given kind the same way, regardless of which controller raised it.
///
/// Never pass a raw secret, TOTP code, WebAuthn challenge/assertion
/// payload, or recovery code through <see cref="LogEvent"/> - only ids,
/// method names, and outcomes.
/// </summary>
public interface ISecurityAuditLogger
{
    /// <summary>
    /// Records a security event. IP address and user-agent are read from
    /// the current request automatically.
    /// </summary>
    /// <param name="eventType">One of <see cref="SecurityAuditEvent"/></param>
    /// <param name="method">The auth method involved: "password", "passkey",
    /// "totp", "email-otp", or null when not applicable</param>
    /// <param name="result">One of <see cref="SecurityAuditResult"/></param>
    /// <param name="userId">The user id, when known</param>
    /// <param name="email">The submitted email, only when <paramref name="userId"/>
    /// is null (an unresolved account) - useful for investigating targeted
    /// probing without ever being a secret itself</param>
    void LogEvent(string eventType, string method, string result, Guid? userId = null, string email = null);
}

/// <summary>
/// The security event types <see cref="ISecurityAuditLogger"/> reports.
/// </summary>
public static class SecurityAuditEvent
{
    public const string LoginSucceeded = "LoginSucceeded";
    public const string LoginFailed = "LoginFailed";
    public const string MethodEnrolled = "MethodEnrolled";
    public const string MethodRevoked = "MethodRevoked";
    public const string LockoutTriggered = "LockoutTriggered";
    public const string SuspiciousRepeatedAttempts = "SuspiciousRepeatedAttempts";
}

/// <summary>
/// The result values <see cref="ISecurityAuditLogger"/> accepts.
/// </summary>
public static class SecurityAuditResult
{
    public const string Success = "Success";
    public const string Failure = "Failure";
    public const string Locked = "Locked";
    public const string RateLimited = "RateLimited";
}
