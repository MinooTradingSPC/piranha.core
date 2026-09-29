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
using Piranha.Manager.LocalAuth.Areas.Manager.Pages;
using Xunit;

namespace Piranha.Tests.Security;

public class LoginDesignTests
{
    [Fact]
    public void BuiltInDesignsAreRegistered()
    {
        var designs = new Piranha.Manager.Module().LoginDesigns;

        Assert.Equal(new[] { LoginDesign.DefaultId, "Verdant", "Aura", "NeuroSync", "Auralis", "AuralisIndigo", "Ecosystem", "Lumina", "Nexus" }, designs.Select(d => d.Id));
        Assert.Equal(designs.Count, designs.Select(d => d.Id).Distinct().Count());

        var design = LoginModel.GetDesign(LoginDesign.DefaultId, designs);
        Assert.Equal(new[] { "~/manager/assets/css/login-default.min.css" }, design.Styles);
        Assert.Empty(design.Scripts);

        var neuroSync = LoginModel.GetDesign("NeuroSync", designs);
        Assert.Equal(new[] { "~/manager/assets/js/login-neurosync.min.js" }, neuroSync.Scripts);
    }

    [Fact]
    public void BuiltInDesignFilesAreEmbeddedInTheManager()
    {
        // The Manager serves ~/manager/assets/ from these embedded files.
        var files = new EmbeddedFileProvider(typeof(ManagerModuleExtensions).Assembly, "Piranha.Manager.assets.dist");

        foreach (var design in new Piranha.Manager.Module().LoginDesigns)
        {
            foreach (var url in design.Styles.Concat(design.RtlStyles).Concat(design.Scripts))
            {
                Assert.StartsWith("~/manager/assets/", url);
                Assert.True(files.GetFileInfo(url["~/manager/assets/".Length..]).Exists, $"{design.Id} is missing {url}");
            }
        }
    }

    [Fact]
    public void ClientDesignsAreFoundByKey()
    {
        var designs = new Piranha.Manager.Module().LoginDesigns;
        designs.Add(new LoginDesign
        {
            Id = "Acme",
            Title = "Acme",
            Styles = { "~/css/acme-login.css" },
            Scripts = { "~/js/acme-login.js" }
        });

        var design = LoginModel.GetDesign("Acme", designs);

        Assert.Equal("Acme", design.Id);
        Assert.Equal(new[] { "~/css/acme-login.css" }, design.Styles);
        Assert.Equal(new[] { "~/js/acme-login.js" }, design.Scripts);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Missing")]
    [InlineData("default")]
    [InlineData("Centralized")]
    public void UnregisteredKeysFallBackToTheDefaultDesign(string id)
    {
        Assert.Equal(LoginDesign.DefaultId, LoginModel.GetDesign(id, new Piranha.Manager.Module().LoginDesigns).Id);
    }

    [Fact]
    public void RemovedDefaultDesignFallsBackToTheBuiltInFiles()
    {
        var designs = new Piranha.Manager.Module().LoginDesigns;
        designs.Clear();

        var design = LoginModel.GetDesign(LoginDesign.DefaultId, designs);

        Assert.Equal(LoginDesign.DefaultId, design.Id);
        Assert.Equal(new[] { "~/manager/assets/css/login-default.min.css" }, design.Styles);
    }
}
