namespace PixDynamicGallery.Application.Common.Interfaces;

/// <summary>
/// Reads a capture file from the local Sparkbooth watch folder. Exists as its own abstraction
/// (rather than calling <c>File.OpenRead</c> straight from the handler) because the concrete
/// implementation must cope with a well-known <c>FileSystemWatcher</c> gotcha: the OS can raise
/// the "created" event before Sparkbooth's writer has closed its file handle, so a naive open can
/// throw <see cref="IOException"/> for a still-in-use file. Infrastructure's implementation retries
/// with backoff; Application/handlers stay oblivious to that detail.
/// </summary>
public interface ILocalCaptureFileReader
{
    Task<Stream> OpenReadAsync(string filePath, CancellationToken cancellationToken = default);
}
