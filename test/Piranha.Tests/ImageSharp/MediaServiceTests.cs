/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using Xunit;

namespace Piranha.Tests.ImageSharp;

[Collection("Integration tests")]
public class MediaServiceTests : BaseTestsAsync
{
    public class MediaOnBeforeSaveException : Exception { }

    private Guid imageId;

    public override async Task InitializeAsync()
    {
        using (var api = CreateApi())
        {
            // Add media
            using (var stream = File.OpenRead("../../../Assets/HLD_Screenshot_01_mech_1080.png"))
            {
                var image1 = new Models.StreamMediaContent()
                {
                    Filename = "HLD_Screenshot_01_mech_1080.png",
                    Data = stream
                };
                await api.Media.SaveAsync(image1);

                imageId = image1.Id.Value;
            }
        }
    }
    public override async Task DisposeAsync()
    {
        using (var api = CreateApi())
        {
            await api.Media.DeleteAsync(imageId);
        }
    }

    [Fact]
    public async Task GetOriginal()
    {
        using (var api = CreateApi())
        {
            var media = await api.Media.GetByIdAsync(imageId);

            Assert.NotNull(media);
            Assert.Equal($"~/uploads/{imageId}-{media.Filename}", media.PublicUrl);
        }
    }

    [Fact]
    public async Task GetScaled()
    {
        using (var api = CreateApi())
        {
            var url = await api.Media.EnsureVersionAsync(imageId, 640);

            Assert.NotNull(url);
            Assert.Equal($"~/uploads/{imageId}-HLD_Screenshot_01_mech_1080_640.png", url);
        }
    }

    [Fact]
    public async Task GetScaledConcurrently()
    {
        var urls = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(async _ =>
            {
                using (var api = CreateApi())
                {
                    return await api.Media.EnsureVersionAsync(imageId, 640);
                }
            }));

        Assert.All(urls, url => Assert.EndsWith($"/uploads/{imageId}-HLD_Screenshot_01_mech_1080_640.png", url));
    }

    [Fact]
    public async Task FailedReplacementKeepsOriginalAndVersions()
    {
        using (var api = CreateApi())
        {
            var media = await api.Media.GetByIdAsync(imageId);
            var versionUrl = await api.Media.EnsureVersionAsync(imageId, 640);

            Assert.NotNull(media);
            Assert.NotNull(versionUrl);

            Piranha.App.Hooks.Media.RegisterOnBeforeSave(m => throw new MediaOnBeforeSaveException());
            try
            {
                using (var stream = File.OpenRead("../../../Assets/HLD_Screenshot_01_rise_1080.png"))
                {
                    var replacement = new Models.StreamMediaContent
                    {
                        Id = imageId,
                        Filename = "HLD_Screenshot_01_rise_1080.png",
                        Data = stream
                    };

                    await Assert.ThrowsAsync<MediaOnBeforeSaveException>(async () =>
                    {
                        await api.Media.SaveAsync(replacement);
                    });
                }
            }
            finally
            {
                Piranha.App.Hooks.Media.Clear();
            }

            using (var session = await _storage.OpenAsync())
            {
                using (var original = new MemoryStream())
                {
                    Assert.True(await session.GetAsync(media, media.Filename, original));
                    Assert.True(original.Length > 0);
                }

                using (var version = new MemoryStream())
                {
                    Assert.True(await session.GetAsync(media, "HLD_Screenshot_01_mech_1080_640.png", version));
                    Assert.True(version.Length > 0);
                }
            }
        }
    }

    [Fact]
    public async Task GetCropped()
    {
        using (var api = CreateApi())
        {
            var url = await api.Media.EnsureVersionAsync(imageId, 640, 300);

            Assert.NotNull(url);
            Assert.Equal($"~/uploads/{imageId}-HLD_Screenshot_01_mech_1080_640x300.png", url);
        }
    }

    [Fact]
    public async Task GetScaledOrgSize()
    {
        using (var api = CreateApi())
        {
            var url = await api.Media.EnsureVersionAsync(imageId, 1920);

            Assert.NotNull(url);
            Assert.Equal($"~/uploads/{imageId}-HLD_Screenshot_01_mech_1080.png", url);
        }
    }

    [Fact]
    public async Task GetCroppedOrgSize()
    {
        using (var api = CreateApi())
        {
            var url = await api.Media.EnsureVersionAsync(imageId, 1920, 1080);

            Assert.NotNull(url);
            Assert.Equal($"~/uploads/{imageId}-HLD_Screenshot_01_mech_1080.png", url);
        }
    }
}
