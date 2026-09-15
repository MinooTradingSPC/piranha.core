/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using Fido2NetLib;
using Fido2NetLib.Objects;

namespace Piranha.Tests.Identity;

/// <summary>
/// Test double for <see cref="IFido2"/>. A genuine WebAuthn ceremony needs
/// an actual authenticator (hardware key, platform biometric, etc.), so
/// there's no way to exercise real cryptographic verification in a unit
/// test - this fake lets tests control what each ceremony step returns
/// instead, so PasskeyService's own logic (challenge tokens, credential
/// storage, exclude-on-reregister, user-ownership checks) can be tested
/// without it.
/// </summary>
public sealed class FakeFido2 : IFido2
{
    public Func<RequestNewCredentialParams, CredentialCreateOptions> OnRequestNewCredential { get; set; }
        = p => CredentialCreateOptions.Create(
            new Fido2Configuration { ServerDomain = "localhost", ServerName = "Test", Origins = new HashSet<string> { "https://localhost" } },
            new byte[] { 1, 2, 3, 4 }, p.User, p.AuthenticatorSelection, p.AttestationPreference, p.ExcludeCredentials, p.Extensions, null);

    public Func<MakeNewCredentialParams, CancellationToken, Task<RegisteredPublicKeyCredential>> OnMakeNewCredential { get; set; }
        = (p, _) => throw new InvalidOperationException("Configure OnMakeNewCredential before completing a registration.");

    public Func<GetAssertionOptionsParams, AssertionOptions> OnGetAssertionOptions { get; set; }
        = p => AssertionOptions.Create(
            new Fido2Configuration { ServerDomain = "localhost", ServerName = "Test", Origins = new HashSet<string> { "https://localhost" } },
            new byte[] { 5, 6, 7, 8 }, p.AllowedCredentials, p.UserVerification, p.Extensions);

    public Func<MakeAssertionParams, CancellationToken, Task<VerifyAssertionResult>> OnMakeAssertion { get; set; }
        = (p, _) => throw new InvalidOperationException("Configure OnMakeAssertion before completing an assertion.");

    public CredentialCreateOptions RequestNewCredential(RequestNewCredentialParams requestNewCredentialParams) =>
        OnRequestNewCredential(requestNewCredentialParams);

    public Task<RegisteredPublicKeyCredential> MakeNewCredentialAsync(MakeNewCredentialParams makeNewCredentialParams,
        CancellationToken cancellationToken = default) =>
        OnMakeNewCredential(makeNewCredentialParams, cancellationToken);

    public AssertionOptions GetAssertionOptions(GetAssertionOptionsParams getAssertionOptionsParams) =>
        OnGetAssertionOptions(getAssertionOptionsParams);

    public Task<VerifyAssertionResult> MakeAssertionAsync(MakeAssertionParams makeAssertionParams,
        CancellationToken cancellationToken = default) =>
        OnMakeAssertion(makeAssertionParams, cancellationToken);
}
