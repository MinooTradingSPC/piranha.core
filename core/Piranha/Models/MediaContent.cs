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
/// Abstract class for media content.
/// </summary>
public abstract class MediaContent
{
    private Lazy<Task<Stream>> _lazyStream;

    /// <summary>
    /// Gets/sets the optional id. If this is empty a new media
    /// entry is created, otherwise the specified media
    /// is updated.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets/sets the optional folder id this media should be placed in.
    /// </summary>
    public Guid? FolderId { get; set; }

    /// <summary>
    /// Gets/sets the filename.
    /// </summary>
    public string Filename { get; set; }

    /// <summary>
    /// Gets the content as a seekable stream. Non-seekable streams are
    /// buffered into memory once, and every call, including concurrent ones,
    /// returns the same instance. Creating the stream is thread-safe, but
    /// using it is not: callers must not read or reposition it in parallel.
    /// </summary>
    /// <returns>The stream, or null if the content type has no data</returns>
    internal Task<Stream> GetStream()
    {
        return LazyInitializer.EnsureInitialized(ref _lazyStream,
            () => new Lazy<Task<Stream>>(CreateStream)).Value;
    }

    private async Task<Stream> CreateStream()
    {
        Stream stream = this switch
        {
            BinaryMediaContent binaryContent => new MemoryStream(binaryContent.Data),
            StreamMediaContent streamContent => streamContent.Data,
            _ => null
        };

        if (stream is not null && !stream.CanSeek)
        {
            var buffered = new MemoryStream();
            await stream.CopyToAsync(buffered).ConfigureAwait(false);
            return buffered;
        }

        return stream;
    }
}

/// <summary>
/// Binary media content.
/// </summary>
public class BinaryMediaContent : MediaContent
{
    /// <summary>
    /// Gets/sets the byte data of the media.
    /// </summary>
    public byte[] Data { get; set; }
}

/// <summary>
/// Stream media content.
/// </summary>
public class StreamMediaContent : MediaContent
{
    /// <summary>
    /// Gets/sets the stream the contains the media content.
    /// </summary>
    public Stream Data { get; set; }
}