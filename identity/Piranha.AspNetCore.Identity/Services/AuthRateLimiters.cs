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
/// as an MVC action filter (<see cref="Piranha.AspNetCore.Identity.PasskeyRateLimitAttribute"/>)
/// rather than the ASP.NET Core rate-limiting middleware, so it doesn't
/// depend on where <c>UseIdentity()</c> ends up in Piranha's own pipeline
/// setup relative to <c>UseRouting</c>/<c>UseEndpoints</c>.
/// </summary>
public sealed class PasskeyRateLimiters
{
    /// <summary>
    /// Limiter for <c>POST /manager/auth/options</c> and
    /// <c>POST /manager/auth/passkey/assertion-options</c>.
    /// </summary>
    public PartitionedRateLimiter<string> AuthOptions { get; } = CreateLimiter(TimeSpan.FromMinutes(5), 20);

    /// <summary>
    /// Limiter for <c>POST /manager/auth/verify</c>.
    /// </summary>
    public PartitionedRateLimiter<string> AuthVerify { get; } = CreateLimiter(TimeSpan.FromMinutes(5), 10);

    /// <summary>
    /// Limiter for the passkey registration ceremony endpoints.
    /// </summary>
    public PartitionedRateLimiter<string> PasskeyRegister { get; } = CreateLimiter(TimeSpan.FromMinutes(10), 10);

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
