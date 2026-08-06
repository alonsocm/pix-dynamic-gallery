namespace PixDynamicGallery.Infrastructure.Watcher;

/// <summary>Bound from the <c>Watcher</c> section of configuration.</summary>
public class SparkboothWatcherOptions
{
    public const string SectionName = "Watcher";

    /// <summary>Master on/off switch — disable in environments (e.g. a pure API replica) that shouldn't watch the filesystem.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How often to re-scan the database for events that became active/inactive, and — since the
    /// same cycle also drives the polling fallback described on <see cref="SparkboothWatcherService"/>
    /// — the worst-case latency for detecting a file the OS-level watcher missed. Both operations are
    /// cheap (one DB query + a directory listing per active event), so this can safely be turned
    /// down for snappier fallback detection; the minimum enforced at runtime is 5s regardless of
    /// this value.
    /// </summary>
    public int RefreshIntervalSeconds { get; set; } = 15;

    /// <summary>File extensions Sparkbooth is expected to produce; anything else raised by the watcher is ignored.</summary>
    public string[] WatchedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".gif"];
}
