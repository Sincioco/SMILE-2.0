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
    AssetChanged,
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
    private List<AssetWatchRoot> _assetWatchRoots = new();
    private HashSet<string> _lastKnownParticipatingPaths = new(StringComparer.OrdinalIgnoreCase);
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
            if (_disposed)
                return;
            if (IsReferencePathOrAncestor(path))
            {
                Queue(SmileProjectRefreshReason.ReferenceChanged);
                return;
            }
            if (IsAssetPathOrAncestor(path))
            {
                Queue(SmileProjectRefreshReason.AssetChanged);
                return;
            }
            if (!_includedSources.Contains(path))
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
            if (_disposed)
                return;
            var referenceChanged = (oldPath != null && IsReferencePathOrAncestor(oldPath)) ||
                                   (newPath != null && IsReferencePathOrAncestor(newPath));
            if (referenceChanged)
            {
                Queue(SmileProjectRefreshReason.ReferenceChanged);
                return;
            }
            if ((oldPath != null && IsAssetPathOrAncestor(oldPath)) ||
                (newPath != null && IsAssetPathOrAncestor(newPath)))
            {
                Queue(SmileProjectRefreshReason.AssetChanged);
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
        var discovery = SmileProjectParticipationDiscovery.Discover(_project.ProjectPath);
        var references = new HashSet<string>(discovery.Paths.Where(path =>
            !string.Equals(path, _project.ProjectPath, StringComparison.OrdinalIgnoreCase) &&
            !sources.Contains(path)), StringComparer.OrdinalIgnoreCase);
        if (discovery.Diagnostic == null)
            _lastKnownParticipatingPaths = new HashSet<string>(references, StringComparer.OrdinalIgnoreCase);
        else
            references.UnionWith(_lastKnownParticipatingPaths);

        var watchedPaths = sources.Concat(references).ToArray();
        var projectDirectory = Path.GetFullPath(_project.ProjectDirectory);
        var sourceAndReferenceRequests = watchedPaths.Select(FindExistingDirectory)
            .Where(directory => !string.IsNullOrWhiteSpace(directory))
            .Select(directory => Path.GetFullPath(directory!))
            .Where(directory => !string.Equals(directory, projectDirectory, StringComparison.OrdinalIgnoreCase) &&
                                Directory.Exists(directory))
            .Select(directory => new WatcherRequest(directory, includeSubdirectories: false));
        var assetRoots = _project.SourceSet.AssetManifest.Includes.Where(include => include.IsValid)
            .Select(include => new AssetWatchRoot(Path.GetFullPath(include.SearchRootFullPath),
                include.WatchSubdirectories)).ToList();
        var assetRequests = assetRoots.Select(root =>
        {
            var directory = Directory.Exists(root.Path) ? root.Path : FindExistingDirectoryForDirectory(root.Path);
            return string.IsNullOrWhiteSpace(directory) ? null : new WatcherRequest(Path.GetFullPath(directory!),
                root.IncludeSubdirectories || !string.Equals(directory, root.Path, StringComparison.OrdinalIgnoreCase));
        }).Where(request => request != null).Select(request => request!);
        var requests = sourceAndReferenceRequests.Concat(assetRequests)
            .Where(request => !string.Equals(request.Path, projectDirectory, StringComparison.OrdinalIgnoreCase) ||
                              request.IncludeSubdirectories)
            .GroupBy(request => request.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => new WatcherRequest(group.Key, group.Any(request => request.IncludeSubdirectories)))
            .OrderBy(request => request.Path, StringComparer.OrdinalIgnoreCase).ToArray();
        var watchers = requests.Select(request => CreateAdditionalWatcher(request.Path,
            request.IncludeSubdirectories)).ToList();
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
                _assetWatchRoots = assetRoots;
                oldWatchers = _additionalWatchers;
                _additionalWatchers = watchers;
            }
        }
        foreach (var watcher in oldWatchers)
            DisposeWatcher(watcher);
    }

    private FileSystemWatcher CreateAdditionalWatcher(string directory, bool includeSubdirectories)
    {
        var watcher = new FileSystemWatcher(directory)
        {
            IncludeSubdirectories = includeSubdirectories,
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

    private bool IsReferencePathOrAncestor(string path)
    {
        if (_referencePaths.Contains(path))
            return true;
        var prefix = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                     Path.DirectorySeparatorChar;
        return _referencePaths.Any(reference => reference.StartsWith(prefix,
            StringComparison.OrdinalIgnoreCase));
    }

    private bool IsAssetPathOrAncestor(string path)
    {
        var normalized = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedPrefix = normalized + Path.DirectorySeparatorChar;
        foreach (var root in _assetWatchRoots)
        {
            var rootPath = root.Path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.Equals(normalized, rootPath, StringComparison.OrdinalIgnoreCase) ||
                rootPath.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
                return true;
            var rootPrefix = rootPath + Path.DirectorySeparatorChar;
            if (normalized.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) &&
                (root.IncludeSubdirectories || string.Equals(Path.GetDirectoryName(normalized), rootPath,
                    StringComparison.OrdinalIgnoreCase)))
                return true;
        }
        return false;
    }

    private static string? FindExistingDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        while (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            directory = Path.GetDirectoryName(directory);
        return directory;
    }

    private static string? FindExistingDirectoryForDirectory(string path)
    {
        var directory = path;
        while (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            directory = Path.GetDirectoryName(directory);
        return directory;
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
            _assetWatchRoots.Clear();
            _lastKnownParticipatingPaths.Clear();
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

    private sealed class AssetWatchRoot
    {
        public AssetWatchRoot(string path, bool includeSubdirectories)
        { Path = path; IncludeSubdirectories = includeSubdirectories; }
        public string Path { get; }
        public bool IncludeSubdirectories { get; }
    }

    private sealed class WatcherRequest
    {
        public WatcherRequest(string path, bool includeSubdirectories)
        { Path = path; IncludeSubdirectories = includeSubdirectories; }
        public string Path { get; }
        public bool IncludeSubdirectories { get; }
    }
}
