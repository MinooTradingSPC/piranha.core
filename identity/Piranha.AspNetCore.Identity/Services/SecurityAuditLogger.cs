/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Piranha.AspNetCore.Identity.Services;

/// <summary>
/// Default implementation of <see cref="ISecurityAuditLogger"/>, emitting a
/// structured log entry for every event via <see cref="ILogger"/>. This
/// repo doesn't have a persistent audit-log store, so "audited" here means
/// a consistently-shaped, greppable log line - hosts that need a queryable
/// audit trail can capture these through their own logging sink.
/// </summary>
public sealed class SecurityAuditLogger : ISecurityAuditLogger
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SecurityAuditLogger> _logger;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public SecurityAuditLogger(IHttpContextAccessor httpContextAccessor, ILogger<SecurityAuditLogger> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <inheritdoc />
    public void LogEvent(string eventType, string method, string result, Guid? userId = null, string email = null)
    {
        var context = _httpContextAccessor.HttpContext;
        var ipAddress = context?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = context?.Request.Headers.UserAgent.ToString();

        var logLevel = result == SecurityAuditResult.Success ? LogLevel.Information : LogLevel.Warning;

        _logger.Log(logLevel,
            "Security audit: event={EventType} method={Method} result={Result} userId={UserId} email={Email} ip={IpAddress} userAgent={UserAgent}",
            eventType, method, result, userId, email, ipAddress, userAgent);
    }
}
