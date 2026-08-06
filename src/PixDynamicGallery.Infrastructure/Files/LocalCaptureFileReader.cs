using Microsoft.Extensions.Logging;
using PixDynamicGallery.Application.Common.Interfaces;

namespace PixDynamicGallery.Infrastructure.Files;

/// <summary>
/// Opens a capture file for reading, retrying with exponential backoff if it is still locked by
/// the process that wrote it. <c>FileSystemWatcher</c> can raise <c>Created</c> the instant the
/// file handle is created, which — especially for larger GIFs on a slow SD card/USB drive — is
/// often before Sparkbooth has finished writing and released the handle.
/// </summary>
public class LocalCaptureFileReader(ILogger<LocalCaptureFileReader> logger) : ILocalCaptureFileReader
{
    private const int MaxAttempts = 8;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMilliseconds(250);

    public async Task<Stream> OpenReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var delay = InitialDelay;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                // FileShare.ReadWrite: we only need read access and must not fight the writer for
                // an exclusive lock — just wait it out if the file genuinely isn't ready yet.
                return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (IOException) when (attempt < MaxAttempts)
            {
                logger.LogDebug(
                    "'{Path}' is still locked, retrying in {DelayMs}ms (attempt {Attempt}/{Max})",
                    filePath, delay.TotalMilliseconds, attempt, MaxAttempts);

                await Task.Delay(delay, cancellationToken);
                delay *= 2;
            }
        }

        // Final attempt: let any exception propagate to the caller instead of swallowing it.
        return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }
}
