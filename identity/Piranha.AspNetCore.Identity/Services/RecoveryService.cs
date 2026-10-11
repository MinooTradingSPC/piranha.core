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
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Piranha.AspNetCore.Identity.Data;

namespace Piranha.AspNetCore.Identity.Services;

/// <summary>
/// Default implementation of <see cref="IRecoveryService"/>.
/// </summary>
public sealed class RecoveryService : IRecoveryService
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);

    private readonly IDb _db;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<RecoveryService> _logger;

    /// <summary>
    /// Default constructor. <paramref name="emailSender"/> is optional -
    /// see <see cref="IEmailSender"/>.
    /// </summary>
    public RecoveryService(IDb db, ILogger<RecoveryService> logger, IEmailSender emailSender = null)
    {
        _db = db;
        _logger = logger;
        _emailSender = emailSender;
    }

    /// <inheritdoc />
    public async Task RequestCodeAsync(User user)
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        var token = new RecoveryToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Created = DateTime.UtcNow,
            ExpiresUtc = DateTime.UtcNow.Add(CodeLifetime)
        };
        token.CodeHash = Hash(token.Id, code);

        _db.RecoveryTokens.Add(token);
        await _db.SaveChangesAsync();

        var html = "<p>Your Manager sign-in code is:</p>" +
                   $"<h2 style=\"letter-spacing:0.25em\">{code}</h2>" +
                   $"<p>This code expires in {CodeLifetime.TotalMinutes:0} minutes and can only be used once. " +
                   "If you didn't request this, you can ignore this email.</p>";

        if (_emailSender == null)
        {
            _logger.LogWarning(
                "No IEmailSender is configured - recovery code for user {UserId} was generated but not delivered",
                user.Id);
            return;
        }

        try
        {
            await _emailSender.SendAsync(user.Email, "Your Manager sign-in code", html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send recovery email to user {UserId}", user.Id);
        }
    }

    /// <inheritdoc />
    public async Task<bool> VerifyCodeAsync(Guid userId, string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return false;
        }

        var candidates = await _db.RecoveryTokens
            .Where(r => r.UserId == userId && r.UsedAt == null && r.ExpiresUtc >= DateTime.UtcNow)
            .ToListAsync();

        foreach (var candidate in candidates)
        {
            var candidateHash = Hash(candidate.Id, code);
            if (CryptographicOperations.FixedTimeEquals(
                    Convert.FromBase64String(candidateHash), Convert.FromBase64String(candidate.CodeHash)))
            {
                candidate.UsedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                _logger.LogInformation("Recovery code used to sign in user {UserId}", userId);

                return true;
            }
        }

        _logger.LogWarning("Recovery code verification failed for user {UserId}", userId);

        return false;
    }

    private static string Hash(Guid id, string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(id.ToString("N") + ":" + code));

        return Convert.ToBase64String(bytes);
    }
}
