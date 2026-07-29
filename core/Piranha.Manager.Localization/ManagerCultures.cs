/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Piranha.Manager.Localization
{
    /// <summary>
    /// Provides the cultures the Manager UI has translated resources for.
    /// </summary>
    public static class ManagerCultures
    {
        private static readonly string[] Codes =
        {
            "en", "af", "ar", "bg", "bs", "ca", "cs", "da", "de", "el",
            "es", "fa", "fi", "fr", "he", "hr", "hu", "id", "it", "ja",
            "ka", "ko", "ky", "nl", "no", "pl", "pt", "ro", "ru", "si",
            "sr", "sv", "tr", "uk", "vi", "zh"
        };

        /// <summary>
        /// Gets the cultures the Manager UI has translated resources for,
        /// ordered by their native display name.
        /// </summary>
        public static IReadOnlyList<CultureInfo> SupportedCultures { get; } = Codes
            .Select(code =>
            {
                try
                {
                    return CultureInfo.GetCultureInfo(code);
                }
                catch (CultureNotFoundException)
                {
                    return null;
                }
            })
            .Where(culture => culture != null)
            .OrderBy(culture => culture.NativeName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
