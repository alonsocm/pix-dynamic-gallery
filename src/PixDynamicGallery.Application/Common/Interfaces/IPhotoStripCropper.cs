namespace PixDynamicGallery.Application.Common.Interfaces;

/// <summary>
/// Crops a Sparkbooth capture down to a single photo strip. Sparkbooth's print composite places
/// the same strip twice, side by side, in one image — so the physical print can be cut in half and
/// two guests each get a copy — but that double-wide sheet is only meaningful to the printer.
/// Guests browsing the wall or downloading their photo should see exactly what they'd hold in
/// their hand: one strip, not the print sheet. Kept out of <see cref="IImageThumbnailGenerator"/>
/// because cropping changes what gets uploaded as the photo's own <c>Url</c>/<c>StorageKey</c>, not
/// just its preview.
/// </summary>
public interface IPhotoStripCropper
{
    /// <summary>True for content types this cropper knows how to decode (jpeg/png/bmp).</summary>
    bool CanCrop(string contentType);

    /// <summary>
    /// Decodes <paramref name="content"/> and returns a new stream containing only the left half of
    /// the image (cropped to width/2, full height, same encoding/quality as the source) — the two
    /// halves are identical duplicates, so which one survives doesn't matter. Returns null if the
    /// image couldn't be decoded, or is animated (a boomerang/GIF capture, not a print composite, so
    /// nothing to crop); callers should treat that as "upload the original untouched" rather than
    /// fail the capture.
    /// </summary>
    Task<Stream?> CropToSingleStripAsync(
        Stream content, string contentType, CancellationToken cancellationToken = default);
}
