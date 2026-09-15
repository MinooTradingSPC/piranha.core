/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Piranha.AspNetCore.Identity.Data;
using Piranha.AspNetCore.Identity.Models;
using Piranha.AspNetCore.Identity.Services;
using Piranha.Manager;

namespace Piranha.AspNetCore.Identity.Controllers;

/// <summary>
/// Manager controller for the current user to manage their own TOTP
/// authenticator-app enrollment. Like passkeys, this always acts on the
/// caller's own account - there's no admin-driven enrollment.
/// </summary>
[Area("Manager")]
[Route("manager/api/totp")]
[Authorize(Policy = Permission.Admin)]
[ApiController]
[AutoValidateAntiforgeryToken]
public sealed class TotpController : Controller
{
    private readonly ITotpService _totp;
    private readonly UserManager<User> _userManager;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public TotpController(ITotpService totp, UserManager<User> userManager)
    {
        _totp = totp;
        _userManager = userManager;
    }

    /// <summary>
    /// Gets the current user's TOTP enrollment status.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Status()
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(await _totp.GetStatusAsync(user.Id));
    }

    /// <summary>
    /// Begins a new (or replacement) TOTP enrollment for the current user.
    /// Regenerating an existing, confirmed enrollment requires the
    /// account's password.
    /// </summary>
    [HttpPost("enroll")]
    [AuthRateLimit(IdentityModuleExtensions.AuthRateLimitPolicies.TotpEnroll)]
    public async Task<IActionResult> BeginEnrollment([FromBody] BeginTotpEnrollmentRequest request)
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            return Unauthorized();
        }

        var status = await _totp.GetStatusAsync(user.Id);
        if (status.Enrolled)
        {
            if (string.IsNullOrEmpty(request?.Password) || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                return BadRequest("Enter your password to set up a new authenticator.");
            }
        }

        return Ok(_totp.BeginEnrollment(user));
    }

    /// <summary>
    /// Confirms a TOTP enrollment with a code from the authenticator app.
    /// </summary>
    [HttpPost("confirm")]
    [AuthRateLimit(IdentityModuleExtensions.AuthRateLimitPolicies.TotpEnroll)]
    public async Task<IActionResult> ConfirmEnrollment([FromBody] ConfirmTotpEnrollmentRequest request)
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            return Unauthorized();
        }

        var confirmed = await _totp.ConfirmEnrollmentAsync(user, request?.Token, request?.Code);
        if (!confirmed)
        {
            return BadRequest("The code is incorrect or has expired. Please try again.");
        }

        return Ok(await _totp.GetStatusAsync(user.Id));
    }

    /// <summary>
    /// Revokes the current user's TOTP enrollment. Requires the account's
    /// password.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Revoke([FromBody] RevokeTotpRequest request)
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrEmpty(request?.Password) || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return BadRequest("Enter your password to remove your authenticator.");
        }

        await _totp.RevokeAsync(user.Id);

        return Ok();
    }
}
