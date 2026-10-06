/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

namespace Piranha.Manager;

/// <summary>
/// A theme for the manager interface: the style and script files every
/// manager page loads on top of the built-in ones, picked by its key on the
/// config page.
/// </summary>
public class ManagerTheme
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
    /// "~/css/manager-theme.css", loaded after the built-in styles.
    /// </summary>
    public List<string> Styles { get; set; } = new List<string>();

    /// <summary>
    /// Gets/sets the urls of the style sheets loaded in place of
    /// <see cref="Styles"/> when the current culture is right-to-left. If
    /// empty, the page loads <see cref="Styles"/> for every culture.
    /// </summary>
    public List<string> RtlStyles { get; set; } = new List<string>();

    /// <summary>
    /// Gets/sets the urls of the scripts, loaded after the built-in
    /// manager scripts.
    /// </summary>
    public List<string> Scripts { get; set; } = new List<string>();
}
