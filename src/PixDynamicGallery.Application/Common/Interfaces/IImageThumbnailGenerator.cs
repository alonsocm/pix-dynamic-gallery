namespace PixDynamicGallery.Application.Common.Interfaces;

/// <summary>
/// Produces a small static JPEG preview of an uploaded photo/GIF, used by the guest live wall so
/// tiles don't each pull down the full-resolution original (or, for GIFs, the whole animation).
/// Kept out of Infrastructure's storage abstraction — thumbnailing is an image-processing concern,
/// storage is where bytes end up — so a provider swap and a resizing-strategy swap never force
/// each other to change.
/// </summary>
public interface IImageThumbnailGenerator
{
    /// <summary>True for content types this generator knows how to decode (jpeg/png/gif/webp/bmp).</summary>
    bool CanGenerate(string contentType);

    /// <summary>
    /// Decodes <paramref name="content"/> and returns a resized JPEG stream (a single static frame,
    /// even for animated GIFs — the wall grid doesn't need motion, only the full-size original
    /// does). Returns null if the image couldn't be decoded; callers should treat that as
    /// best-effort and keep going without a thumbnail rather than fail the whole upload.
    /// </summary>
    Task<Stream?> GenerateAsync(Stream content, CancellationToken cancellationToken = default);
}
