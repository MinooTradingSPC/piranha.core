/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

namespace Piranha.Manager.LocalAuth;

/// <summary>
/// Optional feature-detection service for the shared login page. It lets
/// the page know whether the host has passkey/WebAuthn sign-in wired up
/// (only <c>Piranha.AspNetCore.Identity</c> provides an implementation)
/// without <c>Piranha.Manager.LocalAuth</c> having to depend on it.
/// </summary>
public interface IPasskeyLoginSupport
{
    /// <summary>
    /// Gets if passkey sign-in is available.
    /// </summary>
    bool IsEnabled { get; }
}
