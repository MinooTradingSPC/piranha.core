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
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Piranha.AspNetCore.Identity.Data;

namespace Piranha.AspNetCore.Identity.Services;

/// <summary>
/// Default implementation of <see cref="IPasskeyService"/>, backed by
/// Fido2NetLib for the WebAuthn ceremonies and <see cref="IDb"/> for
/// credential storage.
/// </summary>
public sealed class PasskeyService : IPasskeyService
{
    /// <summary>
    /// Tokens are only valid for this long, mirroring the recommended
    /// lifetime for the wider login-flow token (2-5 minutes).
    /// </summary>
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);

    private const string ProtectorPurpose = "Piranha.AspNetCore.Identity.Passkey.Challenge.v1";

    private readonly IDb _db;
    private readonly IFido2 _fido2;
    private readonly IDataProtector _protector;
    private readonly ILogger<PasskeyService> _logger;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public PasskeyService(IDb db, IFido2 fido2, IDataProtectionProvider dataProtectionProvider,
        ILogger<PasskeyService> logger)
    {
        _db = db;
        _fido2 = fido2;
        _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PasskeyChallenge> BeginRegistrationAsync(User user)
    {
        var existing = await GetPasskeysAsync(user.Id);

        var options = _fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = new Fido2User
            {
                Id = user.Id.ToByteArray(),
                Name = user.Email,
                DisplayName = user.UserName
            },
            ExcludeCredentials = existing
                .Select(p => new PublicKeyCredentialDescriptor(p.CredentialId))
                .ToList(),
            AuthenticatorSelection = new AuthenticatorSelection
            {
                UserVerification = UserVerificationRequirement.Preferred
            },
            AttestationPreference = AttestationConveyancePreference.None
        });

        return CreateChallenge("registration", user.Id, options.ToJson());
    }

    /// <inheritdoc />
    public async Task<Passkey> CompleteRegistrationAsync(User user, string challengeToken,
        AuthenticatorAttestationRawResponse attestationResponse, string deviceName)
    {
        var payload = Unprotect(challengeToken, "registration");
        if (payload == null || payload.UserId != user.Id)
        {
            throw new InvalidOperationException("The registration challenge is invalid or has expired.");
        }

        var options = CredentialCreateOptions.FromJson(payload.OptionsJson);

        var result = await _fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
        {
            AttestationResponse = attestationResponse,
            OriginalOptions = options,
            IsCredentialIdUniqueToUserCallback = async (p, _) =>
                !await _db.Passkeys.AnyAsync(k => k.CredentialId == p.CredentialId)
        });

        var passkey = new Passkey
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CredentialId = result.Id,
            PublicKey = result.PublicKey,
            SignatureCounter = result.SignCount,
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? "Unnamed passkey" : deviceName.Trim(),
            Created = DateTime.UtcNow
        };

        _db.Passkeys.Add(passkey);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Passkey {PasskeyId} registered for user {UserId}", passkey.Id, user.Id);

        return passkey;
    }

    /// <inheritdoc />
    public Task<PasskeyChallenge> BeginAssertionAsync(User user)
    {
        return BeginAssertionInternalAsync(user);
    }

    private async Task<PasskeyChallenge> BeginAssertionInternalAsync(User user)
    {
        var allowedCredentials = user == null
            ? new List<PublicKeyCredentialDescriptor>()
            : (await GetPasskeysAsync(user.Id))
                .Select(p => new PublicKeyCredentialDescriptor(p.CredentialId))
                .ToList();

        var options = _fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = allowedCredentials,
            UserVerification = UserVerificationRequirement.Preferred
        });

        return CreateChallenge("assertion", user?.Id, options.ToJson());
    }

    /// <inheritdoc />
    public async Task<PasskeyAssertionResult> CompleteAssertionAsync(string challengeToken,
        AuthenticatorAssertionRawResponse assertionResponse)
    {
        var payload = Unprotect(challengeToken, "assertion");
        if (payload == null)
        {
            return new PasskeyAssertionResult { Succeeded = false };
        }

        var passkey = await _db.Passkeys
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.CredentialId == assertionResponse.RawId);

        // The credential must exist and, when the flow already identified an
        // account (the common case), belong to that same account - a
        // credential for a different user must never complete the ceremony
        // for the account the flow started with.
        if (passkey == null || (payload.UserId.HasValue && passkey.UserId != payload.UserId.Value))
        {
            _logger.LogWarning("Passkey assertion failed: unknown or mismatched credential");
            return new PasskeyAssertionResult { Succeeded = false };
        }

        var options = AssertionOptions.FromJson(payload.OptionsJson);

        try
        {
            var result = await _fido2.MakeAssertionAsync(new MakeAssertionParams
            {
                AssertionResponse = assertionResponse,
                OriginalOptions = options,
                StoredPublicKey = passkey.PublicKey,
                StoredSignatureCounter = passkey.SignatureCounter,
                IsUserHandleOwnerOfCredentialIdCallback = (p, _) =>
                    Task.FromResult(p.UserHandle.SequenceEqual(passkey.UserId.ToByteArray()))
            });

            passkey.SignatureCounter = result.SignCount;
            passkey.LastUsed = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Passkey {PasskeyId} used to sign in user {UserId}", passkey.Id, passkey.UserId);

            return new PasskeyAssertionResult { Succeeded = true, UserId = passkey.UserId };
        }
        catch (Fido2VerificationException ex)
        {
            _logger.LogWarning(ex, "Passkey assertion failed verification for credential {PasskeyId}", passkey.Id);
            return new PasskeyAssertionResult { Succeeded = false };
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Passkey>> GetPasskeysAsync(Guid userId)
    {
        return await _db.Passkeys
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.Created)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> RemovePasskeyAsync(Guid userId, Guid passkeyId)
    {
        var passkey = await _db.Passkeys
            .FirstOrDefaultAsync(p => p.Id == passkeyId && p.UserId == userId);

        if (passkey == null)
        {
            return false;
        }

        _db.Passkeys.Remove(passkey);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Passkey {PasskeyId} removed for user {UserId}", passkeyId, userId);

        return true;
    }

    private PasskeyChallenge CreateChallenge(string purpose, Guid? userId, string optionsJson)
    {
        var payload = new ChallengePayload
        {
            Purpose = purpose,
            UserId = userId,
            OptionsJson = optionsJson,
            ExpiresUtc = DateTime.UtcNow.Add(ChallengeLifetime)
        };

        var token = _protector.Protect(JsonSerializer.Serialize(payload));

        return new PasskeyChallenge { Token = token, OptionsJson = optionsJson };
    }

    private ChallengePayload Unprotect(string token, string expectedPurpose)
    {
        try
        {
            var json = _protector.Unprotect(token);
            var payload = JsonSerializer.Deserialize<ChallengePayload>(json);

            if (payload == null || payload.Purpose != expectedPurpose || payload.ExpiresUtc < DateTime.UtcNow)
            {
                return null;
            }

            return payload;
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

    private sealed class ChallengePayload
    {
        public string Purpose { get; set; }
        public Guid? UserId { get; set; }
        public string OptionsJson { get; set; }
        public DateTime ExpiresUtc { get; set; }
    }
}
