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
/// A design for the manager login page: the style and script files the page
/// loads, picked by its key on the config page.
/// </summary>
public class LoginDesign
{
    /// <summary>
    /// The key of the built-in design.
    /// </summary>
    public const string DefaultId = "Default";

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
    /// "~/css/login.css", loaded in the page head.
    /// </summary>
    public List<string> Styles { get; set; } = new List<string>();

    /// <summary>
    /// Gets/sets the urls of the scripts, loaded after the page's own
    /// sign-in scripts.
    /// </summary>
    public List<string> Scripts { get; set; } = new List<string>();
}
