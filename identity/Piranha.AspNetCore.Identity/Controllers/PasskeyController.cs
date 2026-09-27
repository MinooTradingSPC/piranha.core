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
using Microsoft.Extensions.Logging;
using Piranha.AspNetCore.Identity;
using Piranha.AspNetCore.Identity.Data;
using Piranha.AspNetCore.Identity.Models;
using Piranha.AspNetCore.Identity.Services;
using Piranha.Manager;

namespace Piranha.AspNetCore.Identity.Controllers;

/// <summary>
/// Manager controller for the current user to manage their own passkeys.
/// Registration and removal always act on the caller's own account - there
/// is intentionally no way for one user, admin or not, to register or
/// remove a passkey for someone else, since the registration ceremony has
/// to happen in that person's own browser.
/// </summary>
[Area("Manager")]
[Route("manager/api/passkey")]
[Authorize(Policy = Permission.Admin)]
[ApiController]
[AutoValidateAntiforgeryToken]
public sealed class PasskeyController : Controller
{
    private readonly IPasskeyService _passkeys;
    private readonly UserManager<User> _userManager;
    private readonly ISecurityAuditLogger _auditLogger;
    private readonly ILogger<PasskeyController> _logger;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public PasskeyController(IPasskeyService passkeys, UserManager<User> userManager,
        ISecurityAuditLogger auditLogger, ILogger<PasskeyController> logger)
    {
        _passkeys = passkeys;
        _userManager = userManager;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    /// <summary>
    /// Gets the passkeys registered for the current user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            return Unauthorized();
        }

        var passkeys = await _passkeys.GetPasskeysAsync(user.Id);

        return Ok(passkeys.Select(p => new PasskeyListItem
        {
            Id = p.Id,
            DeviceName = p.DeviceName,
            Created = p.Created,
            LastUsed = p.LastUsed
        }));
    }

    /// <summary>
    /// Begins a new passkey registration ceremony for the current user.
    /// </summary>
    [HttpPost("register/options")]
    [AuthRateLimit(IdentityModuleExtensions.AuthRateLimitPolicies.PasskeyRegister)]
    public async Task<IActionResult> BeginRegistration()
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            return Unauthorized();
        }

        var challenge = await _passkeys.BeginRegistrationAsync(user);

        return Ok(challenge);
    }

    /// <summary>
    /// Completes a passkey registration ceremony for the current user.
    /// </summary>
    [HttpPost("register/complete")]
    [AuthRateLimit(IdentityModuleExtensions.AuthRateLimitPolicies.PasskeyRegister)]
    public async Task<IActionResult> CompleteRegistration([FromBody] CompletePasskeyRegistrationRequest request)
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            return Unauthorized();
        }

        try
        {
            await _passkeys.CompleteRegistrationAsync(user, request.Token, request.AttestationResponse,
                request.DeviceName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Passkey registration failed for user {UserId}", user.Id);

            return BadRequest("The passkey could not be registered. Please try again.");
        }

        _auditLogger.LogEvent(SecurityAuditEvent.MethodEnrolled, "passkey", SecurityAuditResult.Success, user.Id);

        return await List();
    }

    /// <summary>
    /// Renames one of the current user's passkeys.
    /// </summary>
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Rename(Guid id, [FromBody] RenamePasskeyRequest request)
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            return Unauthorized();
        }

        var renamed = await _passkeys.RenamePasskeyAsync(user.Id, id, request?.DeviceName);

        return renamed ? await List() : NotFound();
    }

    /// <summary>
    /// Removes one of the current user's passkeys. Requires the account's
    /// password as step-up verification, since this is a sensitive action.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, [FromBody] RemovePasskeyRequest request)
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrEmpty(request?.Password) || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return BadRequest("Enter your password to remove this passkey.");
        }

        var removed = await _passkeys.RemovePasskeyAsync(user.Id, id);

        if (removed)
        {
            _auditLogger.LogEvent(SecurityAuditEvent.MethodRevoked, "passkey", SecurityAuditResult.Success, user.Id);
        }

        return removed ? Ok() : NotFound();
    }
}
