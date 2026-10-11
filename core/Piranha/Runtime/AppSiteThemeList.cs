/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using Piranha.Models;

namespace Piranha.Runtime;

/// <summary>
/// The site themes that can be picked on the manager config page. The list
/// is empty by default, and the config page only shows the theme dropdown
/// once the application adds its own themes at startup.
/// </summary>
public sealed class AppSiteThemeList : List<SiteTheme>
{
    /// <summary>
    /// Gets the registered theme with the given key.
    /// </summary>
    /// <param name="id">The theme key stored in the config</param>
    /// <returns>The theme, or null to use the site's own look</returns>
    public SiteTheme Get(string id)
    {
        return string.IsNullOrEmpty(id) ? null : this.FirstOrDefault(t => t.Id == id);
    }

    /// <summary>
    /// Gets the theme selected in the config.
    /// </summary>
    /// <param name="api">The current api</param>
    /// <returns>The theme, or null to use the site's own look</returns>
    public SiteTheme GetCurrent(IApi api)
    {
        // Skip the config lookup when there's nothing to pick from.
        if (Count == 0)
        {
            return null;
        }

        using (var config = new Config(api))
        {
            return Get(config.SiteTheme);
        }
    }
}
