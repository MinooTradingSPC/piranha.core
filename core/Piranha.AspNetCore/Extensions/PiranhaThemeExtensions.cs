/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Piranha;
using Piranha.Models;

/// <summary>
/// Extension class with html helper methods for the site theme picked on
/// the manager config page.
/// </summary>
public static class PiranhaThemeExtensions
{
    private static readonly object ThemeKey = new object();

    /// <summary>
    /// Gets the site theme selected in the config.
    /// </summary>
    /// <param name="html">The html helper</param>
    /// <returns>The theme, or null when the site uses its own look</returns>
    public static SiteTheme SiteTheme(this IHtmlHelper html)
    {
        var context = html.ViewContext.HttpContext;

        // The layout usually asks several times per request.
        if (!context.Items.TryGetValue(ThemeKey, out var theme))
        {
            theme = App.SiteThemes.GetCurrent(context.RequestServices.GetRequiredService<IApi>());
            context.Items[ThemeKey] = theme;
        }
        return (SiteTheme)theme;
    }

    /// <summary>
    /// Generates the link tags for the style sheets of the selected site
    /// theme. Place it in the head after the layout's own style sheets.
    /// </summary>
    /// <param name="html">The html helper</param>
    /// <returns>The link tags</returns>
    public static IHtmlContent SiteThemeStyles(this IHtmlHelper html)
    {
        var theme = html.SiteTheme();
        if (theme == null)
        {
            return HtmlString.Empty;
        }

        var styles = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft && theme.RtlStyles.Count > 0
            ? theme.RtlStyles
            : theme.Styles;

        return Render(html, styles, "<link rel=\"stylesheet\" href=\"{0}\">");
    }

    /// <summary>
    /// Generates the script tags for the scripts of the selected site theme.
    /// Place it at the end of the body.
    /// </summary>
    /// <param name="html">The html helper</param>
    /// <returns>The script tags</returns>
    public static IHtmlContent SiteThemeScripts(this IHtmlHelper html)
    {
        var theme = html.SiteTheme();

        return theme == null
            ? HtmlString.Empty
            : Render(html, theme.Scripts, "<script src=\"{0}\"></script>");
    }

    /// <summary>
    /// Gets the css class for the selected site theme, in the form
    /// "site-theme-{key}" in lower case, for styles that need to tell
    /// themes apart.
    /// </summary>
    /// <param name="html">The html helper</param>
    /// <returns>The class name, or an empty string without a theme</returns>
    public static string SiteThemeClass(this IHtmlHelper html)
    {
        var theme = html.SiteTheme();

        return theme == null ? "" : "site-theme-" + theme.Id.ToLowerInvariant();
    }

    private static HtmlString Render(IHtmlHelper html, IEnumerable<string> urls, string format)
    {
        var url = html.ViewContext.HttpContext.RequestServices
            .GetRequiredService<IUrlHelperFactory>()
            .GetUrlHelper(html.ViewContext);
        var sb = new StringBuilder();

        foreach (var src in urls)
        {
            sb.AppendLine(string.Format(format, HtmlEncoder.Default.Encode(url.Content(src))));
        }
        return new HtmlString(sb.ToString());
    }
}
