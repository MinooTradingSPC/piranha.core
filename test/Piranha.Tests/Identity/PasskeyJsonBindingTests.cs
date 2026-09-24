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
using Newtonsoft.Json;
using Piranha.AspNetCore.Identity.Models;
using Xunit;

namespace Piranha.Tests.Identity;

/// <summary>
/// The Manager binds request bodies with Newtonsoft.Json, while the Fido2NetLib
/// WebAuthn types are annotated for System.Text.Json. These tests feed the
/// shape a browser's <c>PublicKeyCredential.toJSON()</c> produces through
/// Newtonsoft, the same way MVC model binding does.
/// </summary>
public class PasskeyJsonBindingTests
{
    [Fact]
    public void RegistrationRequest_BindsBrowserAttestationJson()
    {
        const string body = """
            {
              "token": "token",
              "deviceName": "Surface",
              "attestationResponse": {
                "authenticatorAttachment": "platform",
                "clientExtensionResults": {},
                "id": "qgEeJMauA-2N-giRRBOLbw",
                "rawId": "qgEeJMauA-2N-giRRBOLbw",
                "response": {
                  "attestationObject": "o2NmbXRkbm9uZWdhdHRTdG10oGhhdXRoRGF0YViUSZYN5YgOjGh0NBcPZHZgW4_krrmihjLHmVzzuoMdl2NdAAAAAOqbjWZNAR0hPOS2tIy1ddQAEKoBHiTGrgPtjfoIkUQTi2-lAQIDJiABIVggzHWAyZKfMevVc3HwgabLYsZR9OTWqdOLGzf0uiJJsvwiWCBQAcAYZCTk3dEL73bPQeX3wmbUzNGSavpL800CchMdbA",
                  "authenticatorData": "SZYN5YgOjGh0NBcPZHZgW4_krrmihjLHmVzzuoMdl2NdAAAAAOqbjWZNAR0hPOS2tIy1ddQAEKoBHiTGrgPtjfoIkUQTi2-lAQIDJiABIVggzHWAyZKfMevVc3HwgabLYsZR9OTWqdOLGzf0uiJJsvwiWCBQAcAYZCTk3dEL73bPQeX3wmbUzNGSavpL800CchMdbA",
                  "clientDataJSON": "eyJ0eXBlIjoid2ViYXV0aG4uY3JlYXRlIiwiY2hhbGxlbmdlIjoidngxVWdFMHNhWkQxTVVFZnhLRl80QSIsIm9yaWdpbiI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMCIsImNyb3NzT3JpZ2luIjpmYWxzZX0",
                  "publicKey": "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEzHWAyZKfMevVc3HwgabLYsZR9OTWqdOLGzf0uiJJsvxQAcAYZCTk3dEL73bPQeX3wmbUzNGSavpL800CchMdbA",
                  "publicKeyAlgorithm": -7,
                  "transports": ["hybrid", "internal"]
                },
                "type": "public-key"
              }
            }
            """;

        var request = JsonConvert.DeserializeObject<CompletePasskeyRegistrationRequest>(body);

        Assert.Equal("Surface", request.DeviceName);
        Assert.NotNull(request.AttestationResponse);
        Assert.Equal("qgEeJMauA-2N-giRRBOLbw", request.AttestationResponse.Id);
        Assert.Equal(16, request.AttestationResponse.RawId.Length);
        Assert.NotEmpty(request.AttestationResponse.Response.AttestationObject);
        Assert.NotEmpty(request.AttestationResponse.Response.ClientDataJson);
    }

    [Fact]
    public void VerifyRequest_BindsBrowserAssertionJson()
    {
        const string body = """
            {
              "token": "token",
              "method": "passkey",
              "assertionToken": "assertion",
              "assertionResponse": {
                "authenticatorAttachment": "platform",
                "clientExtensionResults": {},
                "id": "qgEeJMauA-2N-giRRBOLbw",
                "rawId": "qgEeJMauA-2N-giRRBOLbw",
                "response": {
                  "authenticatorData": "SZYN5YgOjGh0NBcPZHZgW4_krrmihjLHmVzzuoMdl2MdAAAAAA",
                  "clientDataJSON": "eyJ0eXBlIjoid2ViYXV0aG4uZ2V0In0",
                  "signature": "MEUCIQDx",
                  "userHandle": "AAECAw"
                },
                "type": "public-key"
              }
            }
            """;

        var request = JsonConvert.DeserializeObject<AuthVerifyRequest>(body);

        Assert.Equal("passkey", request.Method);
        Assert.NotNull(request.AssertionResponse);
        Assert.Equal(16, request.AssertionResponse.RawId.Length);
        Assert.NotEmpty(request.AssertionResponse.Response.Signature);
        Assert.NotEmpty(request.AssertionResponse.Response.ClientDataJson);
    }

    [Fact]
    public void RegistrationRequest_BindsNullAttestation()
    {
        var request = JsonConvert.DeserializeObject<CompletePasskeyRegistrationRequest>(
            """{ "token": "token", "attestationResponse": null }""");

        Assert.Null(request.AttestationResponse);
    }
}
