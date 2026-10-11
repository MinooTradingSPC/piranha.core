/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

namespace Piranha.Models;

/// <summary>
/// A theme for the public site: the style and script files the site layout
/// loads on top of its own, picked by its key on the manager config page.
/// </summary>
public class SiteTheme
{
    /// <summary>
    /// Gets/sets the unique key stored in the config.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets/sets the display title.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets/sets the urls of the style sheets, for example
    /// "~/css/theme.css", loaded after the layout's own styles.
    /// </summary>
    public List<string> Styles { get; set; } = new List<string>();

    /// <summary>
    /// Gets/sets the urls of the style sheets loaded in place of
    /// <see cref="Styles"/> when the current culture is right-to-left. If
    /// empty, the layout loads <see cref="Styles"/> for every culture.
    /// </summary>
    public List<string> RtlStyles { get; set; } = new List<string>();

    /// <summary>
    /// Gets/sets the urls of the scripts, loaded at the end of the page.
    /// </summary>
    public List<string> Scripts { get; set; } = new List<string>();
}
