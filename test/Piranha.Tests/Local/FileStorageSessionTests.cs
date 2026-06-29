/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using System.Text;
using Piranha.Models;
using Xunit;

namespace Piranha.Tests.FileStorage;

public class FileStorageSessionTests : IDisposable
{
    private readonly string _basePath;
    private readonly Piranha.Local.FileStorage _storage;
    private readonly Media _media;

    public FileStorageSessionTests()
    {
        _basePath = Path.Combine(Path.GetTempPath(), "piranha-filestorage-tests", Guid.NewGuid().ToString("N")) + Path.DirectorySeparatorChar;
        _storage = new Piranha.Local.FileStorage(_basePath, "~/uploads/");
        _media = new Media
        {
            Id = Guid.NewGuid(),
            Filename = "file.txt"
        };
    }

    [Fact]
    public async Task PutStreamTruncatesExistingFile()
    {
        using (var session = await _storage.OpenAsync())
        {
            using (var stream = CreateStream("longer content"))
            {
                await session.PutAsync(_media, _media.Filename, "text/plain", stream);
            }

            using (var stream = CreateStream("short"))
            {
                await session.PutAsync(_media, _media.Filename, "text/plain", stream);
            }

            using (var output = new MemoryStream())
            {
                Assert.True(await session.GetAsync(_media, _media.Filename, output));
                Assert.Equal("short", Encoding.UTF8.GetString(output.ToArray()));
            }
        }
    }

    [Fact]
    public async Task PutBytesTruncatesExistingFile()
    {
        using (var session = await _storage.OpenAsync())
        {
            await session.PutAsync(_media, _media.Filename, "text/plain", Encoding.UTF8.GetBytes("longer content"));
            await session.PutAsync(_media, _media.Filename, "text/plain", Encoding.UTF8.GetBytes("short"));

            using (var output = new MemoryStream())
            {
                Assert.True(await session.GetAsync(_media, _media.Filename, output));
                Assert.Equal("short", Encoding.UTF8.GetString(output.ToArray()));
            }
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_basePath))
        {
            Directory.Delete(_basePath, true);
        }
    }

    private static MemoryStream CreateStream(string value)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(value));
    }
}
