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
using Piranha.Runtime;
using Xunit;

namespace Piranha.Tests.Security;

public class SiteThemeTests
{
    [Fact]
    public void ThemesAreFoundByKey()
    {
        var themes = new AppSiteThemeList
        {
            new SiteTheme
            {
                Id = "Acme",
                Title = "Acme",
                Styles = { "~/css/acme.css" },
                Scripts = { "~/js/acme.js" }
            }
        };

        var theme = themes.Get("Acme");

        Assert.Equal("Acme", theme.Id);
        Assert.Equal(new[] { "~/css/acme.css" }, theme.Styles);
        Assert.Equal(new[] { "~/js/acme.js" }, theme.Scripts);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Missing")]
    [InlineData("acme")]
    public void UnregisteredKeysUseTheSitesOwnLook(string id)
    {
        var themes = new AppSiteThemeList { new SiteTheme { Id = "Acme", Title = "Acme" } };

        Assert.Null(themes.Get(id));
    }

    [Fact]
    public void EmptyListSkipsTheConfigLookup()
    {
        // No api is needed when nothing is registered.
        Assert.Null(new AppSiteThemeList().GetCurrent(null));
    }
}
