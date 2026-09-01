using Microsoft.Extensions.Logging;
using PixDynamicGallery.Application.Common.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace PixDynamicGallery.Infrastructure.Storage;

/// <summary>
/// <see cref="IImageThumbnailGenerator"/> backed by ImageSharp — pure managed code, so it needs no
/// native libvips/libjpeg-turbo dependency in the container image.
/// </summary>
public class ImageSharpThumbnailGenerator(ILogger<ImageSharpThumbnailGenerator> logger) : IImageThumbnailGenerator
{
    // Wide enough for a masonry grid tile even on a retina phone; a small fraction of the weight of
    // a multi-MB photobooth original.
    private const int MaxDimensionPx = 480;
    private const int JpegQuality = 75;

    private static readonly HashSet<string> SupportedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp",
    };

    public bool CanGenerate(string contentType) => SupportedContentTypes.Contains(contentType);

    public async Task<Stream?> GenerateAsync(Stream content, CancellationToken cancellationToken = default)
    {
        try
        {
            using var image = await Image.LoadAsync(content, cancellationToken);

            // Animated GIFs decode as multiple frames. The wall grid only ever needs a static
            // preview — the original stays available at full quality/animation via Photo.Url — and
            // JPEG can't hold animation anyway, so drop every frame but the first before resizing
            // rather than paying to resize frames that would just be discarded at encode time.
            while (image.Frames.Count > 1)
            {
                image.Frames.RemoveFrame(image.Frames.Count - 1);
            }

            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max, // preserves aspect ratio, never upscales a smaller original
                Size = new Size(MaxDimensionPx, MaxDimensionPx),
            }));

            var output = new MemoryStream();
            await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = JpegQuality }, cancellationToken);
            output.Position = 0;
            return output;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Corrupt/unsupported file, decode bomb guard trip, etc. — caller treats null as
            // "no thumbnail available", not a fatal error.
            logger.LogWarning(ex, "Failed to decode image for thumbnail generation.");
            return null;
        }
    }
}
