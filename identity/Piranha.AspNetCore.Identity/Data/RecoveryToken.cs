/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

namespace Piranha.AspNetCore.Identity.Data;

/// <summary>
/// A single-use email recovery code. The code itself is never stored -
/// only a hash keyed by this row's own id, so a database read alone can't
/// recover it.
/// </summary>
public sealed class RecoveryToken
{
    /// <summary>
    /// Gets/sets the unique id. Doubles as the salt for <see cref="CodeHash"/>.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets/sets the id of the user this code was sent to.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets/sets the hash of the code.
    /// </summary>
    public string CodeHash { get; set; }

    /// <summary>
    /// Gets/sets when the code was created.
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// Gets/sets when the code expires.
    /// </summary>
    public DateTime ExpiresUtc { get; set; }

    /// <summary>
    /// Gets/sets when the code was used, if it has been.
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// Gets/sets the user this code was sent to.
    /// </summary>
    public User User { get; set; }
}
