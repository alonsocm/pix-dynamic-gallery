using System.Collections.Concurrent;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Photos.Commands.UploadCapturedPhoto;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Infrastructure.Watcher;

/// <summary>
/// The bridge between Sparkbooth and the rest of the platform. Runs one <see cref="FileSystemWatcher"/>
/// per active <see cref="Event"/>'s <c>WatchFolderPath</c> and, for every new capture, dispatches
/// <see cref="UploadCapturedPhotoCommand"/> through MediatR — which uploads it to cloud storage and
/// broadcasts it over SignalR. Periodically re-reads the Events table so newly created (or
/// deactivated) events start (or stop) being watched without an app restart.
///
/// <para>
/// Every refresh cycle also re-lists each watched folder as a <b>polling fallback</b> alongside
/// <see cref="FileSystemWatcher"/>'s event-driven detection. This isn't redundant: OS-level file
/// change notifications are not reliable across every filesystem this service might point at in
/// practice — most notably, a Docker Desktop (WSL2) bind mount of a Windows host folder into a
/// Linux container does <i>not</i> reliably forward <c>inotify</c> events for changes made on the
/// Windows side, even though the file is immediately visible via a plain directory listing. Mapped
/// network drives have similar gaps. Both triggers funnel through the same
/// <see cref="OnCaptureFileDetected"/> dedupe check, so whichever one notices a file first "wins"
/// and the other becomes a no-op — the fallback costs nothing extra when the watcher already works
/// natively (e.g. the API running directly on the kiosk's own Windows install, per the README's
/// deployment notes), and quietly saves the day when it doesn't.
/// </para>
/// </summary>
public class SparkboothWatcherService(
    IServiceScopeFactory scopeFactory,
    IOptions<SparkboothWatcherOptions> options,
    ILogger<SparkboothWatcherService> logger)
    : BackgroundService
{
    private readonly SparkboothWatcherOptions _options = options.Value;
    private readonly ConcurrentDictionary<Guid, FileSystemWatcher> _watchers = new();

    /// <summary>
    /// Per-event set of capture file paths currently claimed — dispatched and still in flight or
    /// already successfully processed, or deliberately skipped as pre-existing baseline. A
    /// concurrent set substitute (values are unused) chosen specifically for
    /// <see cref="ConcurrentDictionary{TKey,TValue}.TryAdd"/>'s atomic "add if absent" semantics,
    /// the primitive needed to dedupe between the FileSystemWatcher callback (fires on a ThreadPool
    /// thread) and the polling fallback (fires from the refresh loop) racing on the same file.
    /// <see cref="OnCaptureFileDetected"/> releases the claim (removes the entry) if dispatch fails,
    /// so a file is only ever permanently "known" once it's actually been uploaded — see that
    /// method's doc comment for why.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _knownFiles = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("SparkboothWatcherService is disabled (Watcher:Enabled = false).");
            return;
        }

        var refreshInterval = TimeSpan.FromSeconds(Math.Max(5, _options.RefreshIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshWatchedEventsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to refresh watched events.");
            }

            try
            {
                await Task.Delay(refreshInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
        }
    }

    private async Task RefreshWatchedEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var activeEvents = await context.Events
            .Where(e => e.IsActive)
            .ToListAsync(cancellationToken);

        var activeIds = activeEvents.Select(e => e.Id).ToHashSet();

        foreach (var (eventId, watcher) in _watchers)
        {
            if (activeIds.Contains(eventId))
            {
                continue;
            }

            if (_watchers.TryRemove(eventId, out var removed))
            {
                DisposeWatcher(removed);
                _knownFiles.TryRemove(eventId, out _);
                logger.LogInformation("Stopped watching event {EventId} (no longer active).", eventId);
            }
        }

        foreach (var @event in activeEvents)
        {
            if (!Directory.Exists(@event.WatchFolderPath))
            {
                if (!_watchers.ContainsKey(@event.Id))
                {
                    logger.LogWarning(
                        "Watch folder '{Folder}' for event {EventId} ({Slug}) does not exist yet; will retry on next refresh.",
                        @event.WatchFolderPath, @event.Id, @event.Slug);
                }
                continue;
            }

            if (!_watchers.ContainsKey(@event.Id))
            {
                var watcher = CreateWatcher(@event);
                _watchers[@event.Id] = watcher;

                logger.LogInformation(
                    "Watching '{Folder}' for event {EventId} ({Slug}).",
                    @event.WatchFolderPath, @event.Id, @event.Slug);
            }

            // Polling fallback: re-list the folder every cycle regardless of whether the watcher is
            // brand new or has been running for a while — see the class doc comment for why this
            // isn't redundant with FileSystemWatcher.
            PollForMissedFiles(@event.Id, @event.WatchFolderPath);
        }
    }

    private FileSystemWatcher CreateWatcher(Event @event)
    {
        // Seed the "known files" baseline from whatever's already in the folder *before* watching
        // starts, so — matching FileSystemWatcher's own semantics — only files that appear from now
        // on get uploaded, not a backlog of pre-existing captures.
        var knownFiles = _knownFiles.GetOrAdd(@event.Id, _ => new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase));
        foreach (var file in EnumerateCaptureFiles(@event.WatchFolderPath))
        {
            knownFiles.TryAdd(file, 0);
        }

        var watcher = new FileSystemWatcher(@event.WatchFolderPath)
        {
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
        };

        foreach (var extension in _options.WatchedExtensions)
        {
            watcher.Filters.Add($"*{extension}");
        }

        watcher.Created += (_, e) => OnCaptureFileDetected(@event.Id, e.FullPath);
        watcher.Error += (_, e) =>
            logger.LogError(e.GetException(), "FileSystemWatcher error for event {EventId}.", @event.Id);

        watcher.EnableRaisingEvents = true;
        return watcher;
    }

    /// <summary>Re-lists a watched folder and dispatches anything not already known — the polling half of detection.</summary>
    private void PollForMissedFiles(Guid eventId, string folderPath)
    {
        foreach (var file in EnumerateCaptureFiles(folderPath))
        {
            OnCaptureFileDetected(eventId, file);
        }
    }

    private IEnumerable<string> EnumerateCaptureFiles(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            return [];
        }

        try
        {
            return Directory.EnumerateFiles(folderPath)
                .Where(file => _options.WatchedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase));
        }
        catch (IOException ex)
        {
            // Transient (e.g. the folder briefly disappears on a flaky network share) — skip this
            // cycle, the next refresh will retry.
            logger.LogDebug(ex, "Could not list '{Folder}' this cycle.", folderPath);
            return [];
        }
    }

    /// <summary>
    /// Common entry point for both triggers (FileSystemWatcher's <c>Created</c> event and the
    /// polling fallback). The <see cref="ConcurrentDictionary{TKey,TValue}.TryAdd"/> below is the
    /// dedupe: whichever trigger sees a given file path first "claims" it, and the other becomes a
    /// no-op — without this, a file the watcher *does* catch could also get re-uploaded by the next
    /// poll cycle before the DB write from the first pass lands, producing duplicate Photo rows.
    /// <see cref="FileSystemWatcher"/> raises events synchronously on a ThreadPool thread, so
    /// dispatching (DB writes + a cloud upload, which can take seconds) is handed off to
    /// <see cref="Task.Run(Func{Task?})"/> immediately rather than blocking the caller.
    ///
    /// <para>
    /// If dispatch fails, the claim is released so the <em>next</em> poll cycle retries the same
    /// file instead of it being silently skipped forever — this is what makes the pipeline survive
    /// the cabin PC briefly losing internet (both Neon and R2 need connectivity; a capture that
    /// arrives mid-outage would otherwise fail once and never be looked at again, since
    /// <see cref="FileSystemWatcher"/> only raises <c>Created</c> a single time per file and a
    /// restart re-seeds already-present files as baseline, not a backlog to retry). The retried
    /// dispatch is safe because <c>UploadCapturedPhotoCommandHandler</c> is idempotent per
    /// (eventId, filePath) — it reuses the row from the failed attempt instead of creating a
    /// duplicate.
    /// </para>
    /// </summary>
    private void OnCaptureFileDetected(Guid eventId, string filePath)
    {
        var knownFiles = _knownFiles.GetOrAdd(eventId, _ => new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase));
        if (!knownFiles.TryAdd(filePath, 0))
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();

                await sender.Send(new UploadCapturedPhotoCommand
                {
                    EventId = eventId,
                    LocalFilePath = filePath,
                });
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex, "Failed to process captured file '{FilePath}' for event {EventId}; will retry on the next refresh cycle.",
                    filePath, eventId);

                // Release the claim so the polling fallback re-dispatches this same file on the next
                // refresh cycle instead of it being silently dropped forever — the scenario that
                // matters most is the cabin PC briefly losing internet: both the DB (Neon) and
                // storage (R2) calls inside the command need connectivity, so a capture that arrives
                // during an outage would otherwise never be retried once the OS-level file-created
                // event has already fired once. UploadCapturedPhotoCommandHandler is idempotent per
                // (EventId, LocalFilePath), so the retry reuses the same Photo row instead of
                // creating a duplicate.
                knownFiles.TryRemove(filePath, out _);
            }
        });
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        foreach (var watcher in _watchers.Values)
        {
            DisposeWatcher(watcher);
        }

        _watchers.Clear();
        _knownFiles.Clear();
    }

    private static void DisposeWatcher(FileSystemWatcher watcher)
    {
        watcher.EnableRaisingEvents = false;
        watcher.Dispose();
    }
}
