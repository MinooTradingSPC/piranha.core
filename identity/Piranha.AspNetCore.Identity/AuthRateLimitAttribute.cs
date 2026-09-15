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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Piranha.AspNetCore.Identity.Services;

namespace Piranha.AspNetCore.Identity;

/// <summary>
/// Applies a per-client-IP rate limit, backed by <see cref="PasskeyRateLimiters"/>,
/// to the action it decorates. See <see cref="IdentityModuleExtensions.PasskeyRateLimitPolicies"/>
/// for the available policy names.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class PasskeyRateLimitAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _policy;

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="policy">One of <see cref="IdentityModuleExtensions.PasskeyRateLimitPolicies"/></param>
    public PasskeyRateLimitAttribute(string policy)
    {
        _policy = policy;
    }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var limiters = context.HttpContext.RequestServices.GetRequiredService<PasskeyRateLimiters>();
        var limiter = _policy switch
        {
            IdentityModuleExtensions.PasskeyRateLimitPolicies.AuthOptions => limiters.AuthOptions,
            IdentityModuleExtensions.PasskeyRateLimitPolicies.AuthVerify => limiters.AuthVerify,
            IdentityModuleExtensions.PasskeyRateLimitPolicies.PasskeyRegister => limiters.PasskeyRegister,
            _ => null
        };

        if (limiter != null)
        {
            var key = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            using var lease = limiter.AttemptAcquire(key);
            if (!lease.IsAcquired)
            {
                context.Result = new StatusCodeResult(StatusCodes.Status429TooManyRequests);
                return;
            }
        }

        await next();
    }
}
