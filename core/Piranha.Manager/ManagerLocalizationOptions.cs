/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using System.Collections.Generic;
using System.Globalization;

namespace Piranha.Manager
{
    /// <summary>
    /// The cultures the Manager UI can be displayed in, registered
    /// when the Piranha Manager module is added to the service collection.
    /// </summary>
    public sealed class ManagerLocalizationOptions
    {
        /// <summary>
        /// Gets/sets the cultures available in the Manager UI language chooser.
        /// </summary>
        public IReadOnlyList<CultureInfo> SupportedCultures { get; init; } = System.Array.Empty<CultureInfo>();
    }
}
