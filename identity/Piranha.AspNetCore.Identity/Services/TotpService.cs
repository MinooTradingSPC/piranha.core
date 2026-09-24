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
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OtpNet;
using Piranha.AspNetCore.Identity.Data;
using QRCoder;

namespace Piranha.AspNetCore.Identity.Services;

/// <summary>
/// Default implementation of <see cref="ITotpService"/>, using Otp.NET for
/// the RFC 6238 TOTP algorithm and <see cref="IDb"/> for credential storage.
/// The secret is encrypted at rest with <see cref="IDataProtector"/> and
/// only ever decrypted in memory to generate/verify a code.
/// </summary>
public sealed class TotpService : ITotpService
{
    private const string EnrollmentTokenPurpose = "Piranha.AspNetCore.Identity.Totp.Enrollment.v1";
    private const string SecretProtectorPurpose = "Piranha.AspNetCore.Identity.Totp.Secret.v1";
    private const string Issuer = "Piranha Manager";
    private const int SecretLength = 20;
    private static readonly TimeSpan EnrollmentTokenLifetime = TimeSpan.FromMinutes(10);
    private static readonly VerificationWindow Window = new(previous: 1, future: 1);

    private readonly IDb _db;
    private readonly IDataProtector _enrollmentProtector;
    private readonly IDataProtector _secretProtector;
    private readonly ILogger<TotpService> _logger;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public TotpService(IDb db, IDataProtectionProvider dataProtectionProvider, ILogger<TotpService> logger)
    {
        _db = db;
        _enrollmentProtector = dataProtectionProvider.CreateProtector(EnrollmentTokenPurpose);
        _secretProtector = dataProtectionProvider.CreateProtector(SecretProtectorPurpose);
        _logger = logger;
    }

    /// <inheritdoc />
    public TotpEnrollment BeginEnrollment(User user)
    {
        var key = KeyGeneration.GenerateRandomKey(SecretLength);
        var secret = Base32Encoding.ToString(key);

        var provisioningUri = new OtpUri(OtpType.Totp, key, user.Email ?? user.UserName, Issuer,
            OtpHashMode.Sha1, digits: 6, period: 30, counter: 0).ToString();

        var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(provisioningUri, QRCodeGenerator.ECCLevel.Q);
        var qrPng = new PngByteQRCode(qrData).GetGraphic(10, drawQuietZones: true);

        var payload = new EnrollmentPayload
        {
            UserId = user.Id,
            Secret = secret,
            ExpiresUtc = DateTime.UtcNow.Add(EnrollmentTokenLifetime)
        };

        return new TotpEnrollment
        {
            Token = _enrollmentProtector.Protect(JsonSerializer.Serialize(payload)),
            Secret = secret,
            ProvisioningUri = provisioningUri,
            QrCodePngBase64 = Convert.ToBase64String(qrPng)
        };
    }

    /// <inheritdoc />
    public async Task<bool> ConfirmEnrollmentAsync(User user, string enrollmentToken, string code)
    {
        var payload = UnprotectEnrollment(enrollmentToken);
        if (payload == null || payload.UserId != user.Id || string.IsNullOrEmpty(code))
        {
            return false;
        }

        var key = Base32Encoding.ToBytes(payload.Secret);
        var totp = new Totp(key);

        if (!totp.VerifyTotp(code.Trim(), out var timeStepMatched, Window))
        {
            _logger.LogWarning("TOTP enrollment confirmation failed for user {UserId}", user.Id);
            return false;
        }

        var existing = await _db.TotpCredentials.FirstOrDefaultAsync(t => t.UserId == user.Id);
        if (existing != null)
        {
            _db.TotpCredentials.Remove(existing);
        }

        _db.TotpCredentials.Add(new TotpCredential
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            EncryptedSecret = _secretProtector.Protect(payload.Secret),
            ConfirmedAt = DateTime.UtcNow,
            LastAcceptedTimeStep = timeStepMatched
        });
        await _db.SaveChangesAsync();

        _logger.LogInformation("TOTP authenticator confirmed for user {UserId}", user.Id);

        return true;
    }

    /// <inheritdoc />
    public async Task<TotpStatus> GetStatusAsync(Guid userId)
    {
        var credential = await _db.TotpCredentials.FirstOrDefaultAsync(t => t.UserId == userId);

        return new TotpStatus
        {
            Enrolled = credential != null,
            ConfirmedAt = credential?.ConfirmedAt
        };
    }

    /// <inheritdoc />
    public async Task<bool> VerifyCodeAsync(Guid userId, string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return false;
        }

        var credential = await _db.TotpCredentials.FirstOrDefaultAsync(t => t.UserId == userId);
        if (credential == null)
        {
            return false;
        }

        var key = Base32Encoding.ToBytes(_secretProtector.Unprotect(credential.EncryptedSecret));
        var totp = new Totp(key);

        if (!totp.VerifyTotp(code.Trim(), out var timeStepMatched, Window) ||
            timeStepMatched <= credential.LastAcceptedTimeStep)
        {
            _logger.LogWarning("TOTP verification failed for user {UserId}", userId);
            return false;
        }

        credential.LastAcceptedTimeStep = timeStepMatched;
        credential.LastUsed = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("TOTP authenticator used to sign in user {UserId}", userId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RevokeAsync(Guid userId)
    {
        var credential = await _db.TotpCredentials.FirstOrDefaultAsync(t => t.UserId == userId);
        if (credential == null)
        {
            return false;
        }

        _db.TotpCredentials.Remove(credential);
        await _db.SaveChangesAsync();

        _logger.LogInformation("TOTP authenticator revoked for user {UserId}", userId);

        return true;
    }

    private EnrollmentPayload UnprotectEnrollment(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<EnrollmentPayload>(_enrollmentProtector.Unprotect(token));

            return payload != null && payload.ExpiresUtc >= DateTime.UtcNow ? payload : null;
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

    private sealed class EnrollmentPayload
    {
        public Guid UserId { get; set; }
        public string Secret { get; set; }
        public DateTime ExpiresUtc { get; set; }
    }
}
