/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using System.Threading.RateLimiting;

namespace Piranha.AspNetCore.Identity.Services;

/// <summary>
/// Per-IP rate limiters for the anonymous auth-discovery/verification
/// endpoints and the authenticated passkey-registration endpoints. Applied
/// as an MVC action filter (<see cref="Piranha.AspNetCore.Identity.AuthRateLimitAttribute"/>)
/// rather than the ASP.NET Core rate-limiting middleware, so it doesn't
/// depend on where <c>UseIdentity()</c> ends up in Piranha's own pipeline
/// setup relative to <c>UseRouting</c>/<c>UseEndpoints</c>.
/// </summary>
public sealed class AuthRateLimiters
{
    /// <summary>
    /// Limiter for <c>POST /manager/auth/options</c> and
    /// <c>POST /manager/auth/passkey/assertion-options</c>.
    /// </summary>
    public PartitionedRateLimiter<string> AuthOptions { get; } = CreateLimiter(TimeSpan.FromMinutes(5), 20);

    /// <summary>
    /// Per-IP limiter for <c>POST /manager/auth/verify</c>.
    /// </summary>
    public PartitionedRateLimiter<string> AuthVerify { get; } = CreateLimiter(TimeSpan.FromMinutes(5), 10);

    /// <summary>
    /// Per-account limiter for <c>POST /manager/auth/verify</c>, keyed by
    /// the raw submitted email regardless of whether it resolves to a real
    /// account. Closes the gap the IP limiter alone leaves open: without
    /// this, an attacker spreading guesses across many IPs could hit
    /// Identity's own account lockout as their only ceiling, rather than
    /// being slowed down well before that.
    /// </summary>
    public PartitionedRateLimiter<string> AuthVerifyByEmail { get; } = CreateLimiter(TimeSpan.FromMinutes(5), 10);

    /// <summary>
    /// Limiter for the passkey registration ceremony endpoints.
    /// </summary>
    public PartitionedRateLimiter<string> PasskeyRegister { get; } = CreateLimiter(TimeSpan.FromMinutes(10), 10);

    /// <summary>
    /// Limiter for the TOTP enrollment/confirmation endpoints.
    /// </summary>
    public PartitionedRateLimiter<string> TotpEnroll { get; } = CreateLimiter(TimeSpan.FromMinutes(10), 10);

    /// <summary>
    /// Per-IP limiter for <c>POST /manager/auth/email-otp/request</c>.
    /// </summary>
    public PartitionedRateLimiter<string> EmailOtpRequest { get; } = CreateLimiter(TimeSpan.FromMinutes(15), 5);

    /// <summary>
    /// Per-email limiter for <c>POST /manager/auth/email-otp/request</c>,
    /// keyed by the raw submitted address regardless of whether it maps to
    /// a real account, so hitting it never reveals account existence.
    /// Stricter than the IP limiter since it directly protects one inbox
    /// from being spammed from many different IPs.
    /// </summary>
    public PartitionedRateLimiter<string> EmailOtpByEmail { get; } = CreateLimiter(TimeSpan.FromHours(1), 3);

    private static PartitionedRateLimiter<string> CreateLimiter(TimeSpan window, int permitLimit)
    {
        return PartitionedRateLimiter.Create<string, string>(key =>
            RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                Window = window,
                PermitLimit = permitLimit,
                QueueLimit = 0
            }));
    }
}
