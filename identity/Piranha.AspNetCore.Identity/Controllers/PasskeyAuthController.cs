/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Piranha.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Piranha.AspNetCore.Identity.Data;
using Piranha.AspNetCore.Identity.Models;
using Piranha.AspNetCore.Identity.Services;

namespace Piranha.AspNetCore.Identity.Controllers;

/// <summary>
/// Anonymous endpoints for the Manager's email-first sign-in flow: given an
/// email, discover which methods are available, then verify one of them.
/// The whole point of this controller is that its responses never reveal
/// whether <c>email</c> belongs to a real account - see the comments below
/// wherever that matters.
/// </summary>
[Area("Manager")]
[Route("manager/auth")]
[AllowAnonymous]
[ApiController]
public sealed class PasskeyAuthController : Controller
{
    private const string FlowTokenPurpose = "Piranha.AspNetCore.Identity.Auth.Flow.v1";
    private static readonly TimeSpan FlowTokenLifetime = TimeSpan.FromMinutes(5);

    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IPasskeyService _passkeys;
    private readonly ITotpService _totp;
    private readonly IRecoveryService _recovery;
    private readonly AuthRateLimiters _rateLimiters;
    private readonly ISecurityAuditLogger _auditLogger;
    private readonly IDataProtector _protector;
    private readonly ILogger<PasskeyAuthController> _logger;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public PasskeyAuthController(UserManager<User> userManager, SignInManager<User> signInManager,
        IPasskeyService passkeys, ITotpService totp, IRecoveryService recovery, AuthRateLimiters rateLimiters,
        ISecurityAuditLogger auditLogger, IDataProtectionProvider dataProtectionProvider,
        ILogger<PasskeyAuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _passkeys = passkeys;
        _totp = totp;
        _recovery = recovery;
        _rateLimiters = rateLimiters;
        _auditLogger = auditLogger;
        _protector = dataProtectionProvider.CreateProtector(FlowTokenPurpose);
        _logger = logger;
    }

    /// <summary>
    /// Discovers which sign-in methods are available for the given email.
    /// Always returns 200 with the same response shape, whether or not the
    /// email matches an account.
    /// </summary>
    [HttpPost("options")]
    [AuthRateLimit(IdentityModuleExtensions.AuthRateLimitPolicies.AuthOptions)]
    public async Task<AuthOptionsResponse> Options([FromBody] AuthOptionsRequest request)
    {
        var email = request?.Email?.Trim();
        var user = string.IsNullOrEmpty(email) ? null : await FindUserAsync(email);

        // Ordered strongest-first (design doc §6.5: passkey > totp >
        // password > email-otp). "password" is always offered: every
        // account that exists today has one (there is no other way to have
        // created it yet). "email-otp" is always offered too, last - it's
        // the recovery/bootstrap path (#175), never the default when a
        // stronger method exists. Both being unconditional keeps this
        // response identical whether or not the email matches an account.
        var methods = new List<string>();
        if (user != null && await UserHasPasskeyAsync(user))
        {
            methods.Add("passkey");
        }
        if (user != null && (await _totp.GetStatusAsync(user.Id)).Enrolled)
        {
            methods.Add("totp");
        }
        methods.Add("password");
        methods.Add("email-otp");

        return new AuthOptionsResponse
        {
            Token = ProtectFlow(email),
            Methods = methods.ToArray()
        };
    }

    /// <summary>
    /// Sends a one-time recovery code to the account's email, if it exists.
    /// Always returns the same generic response, whether or not the
    /// account exists, the send succeeded, or a rate limit was hit -
    /// nothing here is allowed to be a distinguishable signal.
    /// </summary>
    [HttpPost("email-otp/request")]
    [AuthRateLimit(IdentityModuleExtensions.AuthRateLimitPolicies.EmailOtpRequest)]
    public async Task<IActionResult> RequestEmailOtp([FromBody] EmailOtpRequestRequest request)
    {
        var email = UnprotectFlow(request?.Token);

        // Keyed by the raw submitted address, not by whether it resolves to
        // a real account, so exhausting this budget never itself reveals
        // account existence.
        if (!string.IsNullOrEmpty(email))
        {
            using var lease = _rateLimiters.EmailOtpByEmail.AttemptAcquire(email.Trim().ToLowerInvariant());
            if (lease.IsAcquired)
            {
                var user = await FindUserAsync(email);
                if (user != null)
                {
                    await _recovery.RequestCodeAsync(user);
                }
            }
            else
            {
                _auditLogger.LogEvent(SecurityAuditEvent.SuspiciousRepeatedAttempts, "email-otp",
                    SecurityAuditResult.RateLimited, email: email);
            }
        }

        return Ok();
    }

    /// <summary>
    /// Begins a passkey assertion ceremony for the account identified by
    /// the flow token. Returns an empty credential list, rather than an
    /// error, when the account doesn't exist or has no passkeys.
    /// </summary>
    [HttpPost("passkey/assertion-options")]
    [AuthRateLimit(IdentityModuleExtensions.AuthRateLimitPolicies.AuthOptions)]
    public async Task<IActionResult> PasskeyAssertionOptions([FromBody] AuthAssertionOptionsRequest request)
    {
        var email = UnprotectFlow(request?.Token);
        if (email == null)
        {
            return BadRequest();
        }

        var user = await FindUserAsync(email);
        var challenge = await _passkeys.BeginAssertionAsync(user);

        return Ok(challenge);
    }

    /// <summary>
    /// Verifies the chosen sign-in method and, on success, signs the user in.
    /// Every failure, for whatever reason, returns the same generic message.
    /// </summary>
    [HttpPost("verify")]
    [AuthRateLimit(IdentityModuleExtensions.AuthRateLimitPolicies.AuthVerify)]
    public async Task<AuthVerifyResponse> Verify([FromBody] AuthVerifyRequest request, [FromQuery] string returnUrl = null)
    {
        var email = UnprotectFlow(request?.Token);

        // Per-account rate limit, keyed the same way as email-otp/request:
        // by the raw submitted address, not by whether it resolves to a
        // real account. Never returns a distinguishable response - just the
        // same generic failure - since this is tied to account identity.
        if (!string.IsNullOrEmpty(email))
        {
            using var lease = _rateLimiters.AuthVerifyByEmail.AttemptAcquire(email.Trim().ToLowerInvariant());
            if (!lease.IsAcquired)
            {
                _auditLogger.LogEvent(SecurityAuditEvent.SuspiciousRepeatedAttempts, request?.Method,
                    SecurityAuditResult.RateLimited, email: email);
                return GenericFailure();
            }
        }

        var user = email == null ? null : await FindUserAsync(email);

        var succeeded = user != null && await VerifyMethodAsync(user, request);

        if (!succeeded)
        {
            return GenericFailure();
        }

        await _signInManager.SignInAsync(user, isPersistent: false);

        var target = Url.IsLocalUrl(returnUrl)
            ? $"/manager/login/auth?returnUrl={Uri.EscapeDataString(returnUrl)}"
            : "/manager/login/auth";

        return new AuthVerifyResponse
        {
            Succeeded = true,
            ReturnUrl = target,
            PromptStrongMethodSetup = request.Method == "email-otp"
        };
    }

    private async Task<bool> VerifyMethodAsync(User user, AuthVerifyRequest request)
    {
        if (await _userManager.IsLockedOutAsync(user))
        {
            _auditLogger.LogEvent(SecurityAuditEvent.LoginFailed, request.Method, SecurityAuditResult.Locked, user.Id);
            return false;
        }

        var succeeded = request.Method switch
        {
            "password" => await VerifyPasswordAsync(user, request.Password),
            "passkey" => await VerifyPasskeyAsync(user, request),
            "totp" => await _totp.VerifyCodeAsync(user.Id, request.Code),
            "email-otp" => await _recovery.VerifyCodeAsync(user.Id, request.Code),
            _ => false
        };

        if (succeeded)
        {
            await _userManager.ResetAccessFailedCountAsync(user);
            _auditLogger.LogEvent(SecurityAuditEvent.LoginSucceeded, request.Method, SecurityAuditResult.Success, user.Id);
        }
        else
        {
            await _userManager.AccessFailedAsync(user);
            _auditLogger.LogEvent(SecurityAuditEvent.LoginFailed, request.Method, SecurityAuditResult.Failure, user.Id);

            // We already know we weren't locked out before this attempt (the
            // check above returns early otherwise), so a locked-out state
            // now means this exact attempt is what tripped it.
            if (await _userManager.IsLockedOutAsync(user))
            {
                _auditLogger.LogEvent(SecurityAuditEvent.LockoutTriggered, request.Method, SecurityAuditResult.Locked, user.Id);
            }
        }

        return succeeded;
    }

    private async Task<bool> VerifyPasswordAsync(User user, string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);

        return result.Succeeded;
    }

    private async Task<bool> VerifyPasskeyAsync(User user, AuthVerifyRequest request)
    {
        if (string.IsNullOrEmpty(request.AssertionToken) || request.AssertionResponse == null)
        {
            return false;
        }

        var result = await _passkeys.CompleteAssertionAsync(request.AssertionToken, request.AssertionResponse);

        return result.Succeeded && result.UserId == user.Id;
    }

    /// <summary>
    /// Resolves the identifier typed into the login page's username field.
    /// It accepts either the account's email or its username, since the
    /// password form next to it signs in by username.
    /// </summary>
    private async Task<User> FindUserAsync(string identifier)
    {
        return await _userManager.FindByEmailAsync(identifier)
            ?? await _userManager.FindByNameAsync(identifier);
    }

    private async Task<bool> UserHasPasskeyAsync(User user)
    {
        var passkeys = await _passkeys.GetPasskeysAsync(user.Id);

        return passkeys.Count > 0;
    }

    private static AuthVerifyResponse GenericFailure() => new()
    {
        Succeeded = false,
        Message = "The code you entered is incorrect or has expired."
    };

    private string ProtectFlow(string email)
    {
        var payload = new FlowPayload { Email = email, ExpiresUtc = DateTime.UtcNow.Add(FlowTokenLifetime) };

        return _protector.Protect(JsonSerializer.Serialize(payload));
    }

    private string UnprotectFlow(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<FlowPayload>(_protector.Unprotect(token));

            return payload != null && payload.ExpiresUtc >= DateTime.UtcNow ? payload.Email : null;
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed class FlowPayload
    {
        public string Email { get; set; }
        public DateTime ExpiresUtc { get; set; }
    }
}
