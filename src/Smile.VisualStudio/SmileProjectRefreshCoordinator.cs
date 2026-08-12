using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Smile.Language;

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
    private HashSet<string> _referencePaths = new(StringComparer.OrdinalIgnoreCase);
    private List<FileSystemWatcher> _additionalWatchers = new();
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
            IncludeSubdirectories = false,
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
            if (_disposed || (!_includedSources.Contains(path) && !_referencePaths.Contains(path)))
                return;
            if (_referencePaths.Contains(path))
            {
                Queue(SmileProjectRefreshReason.ReferenceChanged);
                return;
            }
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
            if (_disposed)
                return;
            var referenceChanged = (oldPath != null && _referencePaths.Contains(oldPath)) ||
                                   (newPath != null && _referencePaths.Contains(newPath));
            if (referenceChanged)
            {
                Queue(SmileProjectRefreshReason.ReferenceChanged);
                return;
            }
            if ((oldPath == null || !_includedSources.Contains(oldPath)) &&
                (newPath == null || !_includedSources.Contains(newPath)))
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
        var references = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var graph = SmileProjectBuildGraph.Load(_project.ProjectPath);
            references.UnionWith(graph.ParticipatingPaths.Where(path =>
                !string.Equals(path, _project.ProjectPath, StringComparison.OrdinalIgnoreCase) &&
                !sources.Contains(path)));
        }
        catch (Exception exception) when (SmileProjectDiagnostic.TryCreate(exception, _project.ProjectPath, out _))
        {
            references.UnionWith(_project.SourceSet.References.Select(reference => reference.FullPath));
        }

        var watchedPaths = sources.Concat(references).ToArray();
        var projectDirectory = Path.GetFullPath(_project.ProjectDirectory);
        var additionalDirectories = watchedPaths.Select(Path.GetDirectoryName)
            .Where(directory => !string.IsNullOrWhiteSpace(directory))
            .Select(directory => Path.GetFullPath(directory!))
            .Where(directory => !string.Equals(directory, projectDirectory, StringComparison.OrdinalIgnoreCase) &&
                                Directory.Exists(directory))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(directory => directory, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var watchers = additionalDirectories.Select(CreateAdditionalWatcher).ToList();
        List<FileSystemWatcher> oldWatchers;
        lock (_gate)
        {
            if (_disposed)
            {
                oldWatchers = watchers;
            }
            else
            {
                _includedSources = sources;
                _referencePaths = references;
                oldWatchers = _additionalWatchers;
                _additionalWatchers = watchers;
            }
        }
        foreach (var watcher in oldWatchers)
            DisposeWatcher(watcher);
    }

    private FileSystemWatcher CreateAdditionalWatcher(string directory)
    {
        var watcher = new FileSystemWatcher(directory)
        {
            IncludeSubdirectories = false,
            Filter = "*",
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite |
                           NotifyFilters.CreationTime | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        watcher.Changed += OnChanged;
        watcher.Created += OnChanged;
        watcher.Deleted += OnChanged;
        watcher.Renamed += OnRenamed;
        watcher.Error += OnWatcherError;
        return watcher;
    }

    private void DisposeWatcher(FileSystemWatcher watcher)
    {
        watcher.EnableRaisingEvents = false;
        watcher.Changed -= OnChanged;
        watcher.Created -= OnChanged;
        watcher.Deleted -= OnChanged;
        watcher.Renamed -= OnRenamed;
        watcher.Error -= OnWatcherError;
        watcher.Dispose();
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
        List<FileSystemWatcher> additionalWatchers;
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            _includedSources.Clear();
            _referencePaths.Clear();
            additionalWatchers = _additionalWatchers;
            _additionalWatchers = new List<FileSystemWatcher>();
        }

        _cancellation.Cancel();
        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnChanged;
        _watcher.Created -= OnChanged;
        _watcher.Deleted -= OnChanged;
        _watcher.Renamed -= OnRenamed;
        _watcher.Error -= OnWatcherError;
        _watcher.Dispose();
        foreach (var watcher in additionalWatchers)
            DisposeWatcher(watcher);
        _timer.Dispose();
        _cancellation.Dispose();
    }
}
