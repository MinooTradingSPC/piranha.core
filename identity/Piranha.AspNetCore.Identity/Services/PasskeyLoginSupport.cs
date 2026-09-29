/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using Piranha.Manager.LocalAuth;

namespace Piranha.AspNetCore.Identity.Services;

/// <inheritdoc />
public sealed class PasskeyLoginSupport : IPasskeyLoginSupport
{
    /// <inheritdoc />
    public bool IsEnabled => true;
}
