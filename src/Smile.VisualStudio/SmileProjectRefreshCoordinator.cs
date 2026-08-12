using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Shell;

namespace Smile.VisualStudio;

internal enum SmileProjectRefreshReason
{
    ProjectLoaded,
    SourceAddedByCommand,
    SourceRemovedByCommand,
    StartupChanged,
    SupportStateChanged,
    ReferenceChanged,
    ProjectFileChangedExternally,
    IncludedSourceChanged,
    IncludedSourceCreated,
    IncludedSourceDeleted,
    IncludedSourceRenamed,
    ManualRefresh,
    BuildValidation
}

internal sealed class SmileProjectRefreshCoordinator : IDisposable
{
    private const int DebounceMilliseconds = 200;
    private const int RetryMilliseconds = 250;
    private const int MaximumReadRetries = 4;

    private readonly SmilePackage _package;
    private readonly SmileProject _project;
    private readonly object _gate = new();
    private readonly FileSystemWatcher _watcher;
    private readonly Timer _timer;
    private readonly CancellationTokenSource _cancellation = new();
    private HashSet<string> _includedSources = new(StringComparer.OrdinalIgnoreCase);
    private SmileProjectRefreshReason _pendingReason;
    private int _readRetries;
    private long _projectLength;
    private long _projectWriteUtcTicks;
    private bool _disposed;

    public SmileProjectRefreshCoordinator(SmilePackage package, SmileProject project)
    {
        _package = package;
        _project = project;
        _watcher = new FileSystemWatcher(project.ProjectDirectory)
        {
            IncludeSubdirectories = true,
            Filter = "*",
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite |
                           NotifyFilters.CreationTime | NotifyFilters.Size
        };
        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.Deleted += OnChanged;
        _watcher.Renamed += OnRenamed;
        _watcher.Error += OnWatcherError;
        _timer = new Timer(OnTimer, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        ReconcileSubscriptions();
        CaptureProjectStamp();
        _watcher.EnableRaisingEvents = true;
    }

    public bool Refresh(SmileProjectRefreshReason reason, bool throwOnFailure = true, string? revealPath = null)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (_disposed)
            return false;

        lock (_gate)
            _timer.Change(Timeout.Infinite, Timeout.Infinite);

        if (!_project.TryRefreshFromDisk(reason, out var error))
        {
            ActivityLog.LogWarning(nameof(SmileProjectRefreshCoordinator),
                $"SMILE project refresh '{reason}' retained the last known-good hierarchy: {error}");
            if (throwOnFailure)
                throw new InvalidDataException($"Could not refresh the SMILE project: {error!.Message}", error);
            ScheduleRetry(reason);
            return false;
        }

        _readRetries = 0;
        ReconcileSubscriptions();
        CaptureProjectStamp();
        if (!string.IsNullOrWhiteSpace(revealPath))
            _project.RevealPath(revealPath!);
        return true;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        var path = Normalize(e.FullPath);
        if (path == null)
            return;
        if (string.Equals(path, _project.ProjectPath, StringComparison.OrdinalIgnoreCase))
        {
            if (MatchesProjectStamp())
                return;
            Queue(SmileProjectRefreshReason.ProjectFileChangedExternally);
            return;
        }

        lock (_gate)
        {
            if (_disposed || !_includedSources.Contains(path))
                return;
        }

        Queue(e.ChangeType switch
        {
            WatcherChangeTypes.Created => SmileProjectRefreshReason.IncludedSourceCreated,
            WatcherChangeTypes.Deleted => SmileProjectRefreshReason.IncludedSourceDeleted,
            _ => SmileProjectRefreshReason.IncludedSourceChanged
        });
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        var oldPath = Normalize(e.OldFullPath);
        var newPath = Normalize(e.FullPath);
        if (string.Equals(oldPath, _project.ProjectPath, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(newPath, _project.ProjectPath, StringComparison.OrdinalIgnoreCase))
        {
            Queue(SmileProjectRefreshReason.ProjectFileChangedExternally);
            return;
        }

        lock (_gate)
        {
            if (_disposed ||
                (oldPath != null && !_includedSources.Contains(oldPath) &&
                 (newPath == null || !_includedSources.Contains(newPath))))
                return;
        }
        Queue(SmileProjectRefreshReason.IncludedSourceRenamed);
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        ActivityLog.LogWarning(nameof(SmileProjectRefreshCoordinator),
            $"SMILE project watcher reported an error and scheduled a full refresh: {e.GetException()}");
        Queue(SmileProjectRefreshReason.ProjectFileChangedExternally);
    }

    private void Queue(SmileProjectRefreshReason reason)
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            _pendingReason = reason;
            _readRetries = 0;
            _timer.Change(DebounceMilliseconds, Timeout.Infinite);
        }
    }

    private void ScheduleRetry(SmileProjectRefreshReason reason)
    {
        lock (_gate)
        {
            if (_disposed || ++_readRetries > MaximumReadRetries)
                return;
            _pendingReason = reason;
            _timer.Change(RetryMilliseconds, Timeout.Infinite);
        }
    }

    private void OnTimer(object? state)
    {
        SmileProjectRefreshReason reason;
        lock (_gate)
        {
            if (_disposed)
                return;
            reason = _pendingReason;
        }

        _ = _package.JoinableTaskFactory.RunAsync(async () =>
        {
            try
            {
                await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_cancellation.Token);
                if (!_disposed)
                    Refresh(reason, throwOnFailure: false);
            }
            catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                ActivityLog.LogError(nameof(SmileProjectRefreshCoordinator), exception.ToString());
            }
        });
    }

    private void ReconcileSubscriptions()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var sources = new HashSet<string>(
            _project.SourceSet.Items.Select(source => Path.GetFullPath(source.FullPath)),
            StringComparer.OrdinalIgnoreCase);
        lock (_gate)
        {
            if (!_disposed)
                _includedSources = sources;
        }
    }

    private void CaptureProjectStamp()
    {
        var file = new FileInfo(_project.ProjectPath);
        lock (_gate)
        {
            _projectLength = file.Exists ? file.Length : -1;
            _projectWriteUtcTicks = file.Exists ? file.LastWriteTimeUtc.Ticks : 0;
        }
    }

    private bool MatchesProjectStamp()
    {
        try
        {
            var file = new FileInfo(_project.ProjectPath);
            lock (_gate)
                return file.Exists && file.Length == _projectLength &&
                       file.LastWriteTimeUtc.Ticks == _projectWriteUtcTicks;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string? Normalize(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    public void Dispose()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            _includedSources.Clear();
        }

        _cancellation.Cancel();
        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnChanged;
        _watcher.Created -= OnChanged;
        _watcher.Deleted -= OnChanged;
        _watcher.Renamed -= OnRenamed;
        _watcher.Error -= OnWatcherError;
        _watcher.Dispose();
        _timer.Dispose();
        _cancellation.Dispose();
    }
}
