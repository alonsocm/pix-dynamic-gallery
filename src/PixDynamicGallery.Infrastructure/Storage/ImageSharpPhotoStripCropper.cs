using Microsoft.Extensions.Logging;
using PixDynamicGallery.Application.Common.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace PixDynamicGallery.Infrastructure.Storage;

/// <summary>
/// <see cref="IPhotoStripCropper"/> backed by ImageSharp — the same library already used for
/// thumbnails, so this needs no extra native dependency.
/// </summary>
public class ImageSharpPhotoStripCropper(ILogger<ImageSharpPhotoStripCropper> logger) : IPhotoStripCropper
{
    private static readonly HashSet<string> SupportedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/bmp",
    };

    public bool CanCrop(string contentType) => SupportedContentTypes.Contains(contentType);

    public async Task<Stream?> CropToSingleStripAsync(
        Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            using var image = await Image.LoadAsync(content, cancellationToken);
            var format = image.Metadata.DecodedImageFormat;

            if (image.Frames.Count > 1 || format is null)
            {
                // Animated (a boomerang/GIF capture, not a print composite — nothing to crop) or a
                // format ImageSharp couldn't identify on decode.
                return null;
            }

            var stripWidth = image.Width / 2;
            if (stripWidth < 1)
            {
                return null;
            }

            image.Mutate(ctx => ctx.Crop(new Rectangle(0, 0, stripWidth, image.Height)));

            var output = new MemoryStream();
            // Re-encode with the source's own format/encoder (e.g. JpegEncoder with no explicit
            // Quality preserves the original's embedded quality) rather than forcing a fixed quality
            // — this replaces the photo's own uploaded copy, not a throwaway preview, so it shouldn't
            // lose more fidelity than the crop itself requires.
            await image.SaveAsync(output, format, cancellationToken);
            output.Position = 0;
            return output;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Corrupt/unsupported file, decode bomb guard trip, etc. — caller treats null as
            // "upload the original untouched", not a fatal error.
            logger.LogWarning(ex, "Failed to crop captured photo to a single strip; uploading the original as-is.");
            return null;
        }
    }
}
