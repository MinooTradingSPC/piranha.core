/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using Microsoft.Extensions.FileProviders;
using Piranha.Manager;
using Xunit;

namespace Piranha.Tests.Security;

public class ManagerThemeTests
{
    [Fact]
    public void BuiltInThemesAreRegistered()
    {
        var themes = new Piranha.Manager.Module().Themes;

        Assert.Equal(new[] { "Aura", "Auralis", "AuralisIndigo", "Ecosystem", "Lumina", "NeuroSync", "Nexus", "Verdant" }, themes.Select(t => t.Id));
        Assert.Equal(themes.Count, themes.Select(t => t.Id).Distinct().Count());
        Assert.All(themes, t => Assert.False(string.IsNullOrWhiteSpace(t.Title)));
    }

    [Fact]
    public void BuiltInThemeFilesAreEmbeddedInTheManager()
    {
        // The Manager serves ~/manager/assets/ from these embedded files.
        var files = new EmbeddedFileProvider(typeof(ManagerModuleExtensions).Assembly, "Piranha.Manager.assets.dist");

        foreach (var theme in new Piranha.Manager.Module().Themes)
        {
            Assert.NotEmpty(theme.Styles);
            Assert.NotEmpty(theme.RtlStyles);

            foreach (var url in theme.Styles.Concat(theme.RtlStyles).Concat(theme.Scripts))
            {
                Assert.StartsWith("~/manager/assets/", url);
                Assert.True(files.GetFileInfo(url["~/manager/assets/".Length..]).Exists, $"{theme.Id} is missing {url}");
            }
        }
    }

    [Fact]
    public void ClientThemesAreFoundByKey()
    {
        var module = new Piranha.Manager.Module();
        module.Themes.Add(new ManagerTheme
        {
            Id = "Acme",
            Title = "Acme",
            Styles = { "~/css/acme-manager.css" },
            Scripts = { "~/js/acme-manager.js" }
        });

        var theme = module.GetTheme("Acme");

        Assert.Equal("Acme", theme.Id);
        Assert.Equal(new[] { "~/css/acme-manager.css" }, theme.Styles);
        Assert.Equal(new[] { "~/js/acme-manager.js" }, theme.Scripts);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Missing")]
    [InlineData("acme")]
    public void UnregisteredKeysUseTheBuiltInLook(string id)
    {
        var module = new Piranha.Manager.Module();
        module.Themes.Add(new ManagerTheme { Id = "Acme", Title = "Acme" });

        Assert.Null(module.GetTheme(id));
    }
}
