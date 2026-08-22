using System.Security.Cryptography;
using System.Text;

namespace Smile.Compiler;

internal sealed class OutputPublicationLock : IDisposable
{
    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private readonly Mutex _mutex;
    private bool _ownsMutex;

    private OutputPublicationLock(string targetPath, TimeSpan timeout)
    {
        _mutex = new Mutex(false, CreateMutexName(targetPath));
        try
        {
            _ownsMutex = _mutex.WaitOne(timeout);
        }
        catch (AbandonedMutexException)
        {
            _ownsMutex = true;
        }
        if (!_ownsMutex)
        {
            _mutex.Dispose();
            throw new OutputLockTimeoutException(targetPath, timeout);
        }
    }

    public static OutputPublicationLock Acquire(string targetPath, TimeSpan? timeout = null) =>
        new(targetPath, timeout ?? DefaultTimeout);

    internal static string CreateMutexName(string targetPath)
    {
        var normalized = Path.GetFullPath(targetPath).TrimEnd(Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar).ToUpperInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return "Smile.Compiler.Output." + Convert.ToHexString(hash);
    }

    public void Dispose()
    {
        if (_ownsMutex)
        {
            _mutex.ReleaseMutex();
            _ownsMutex = false;
        }
        _mutex.Dispose();
    }
}

internal sealed class OutputLockTimeoutException : IOException
{
    public OutputLockTimeoutException(string targetPath, TimeSpan timeout)
        : base($"Another build still owns output '{targetPath}' after {timeout.TotalSeconds:0.###} seconds.")
    {
        TargetPath = targetPath;
    }

    public string TargetPath { get; }
}

internal sealed class CompilerIntermediateDirectory : IDisposable
{
    private readonly bool _keep;

    public CompilerIntermediateDirectory(string sourceOrProjectPath, string outputBaseName, bool keep)
    {
        _keep = keep;
        var ownerDirectory = Path.GetDirectoryName(Path.GetFullPath(sourceOrProjectPath))!;
        var buildId = outputBaseName + "." + Environment.ProcessId + "." + Guid.NewGuid().ToString("N");
        DirectoryPath = Path.Combine(ownerDirectory, "obj", "Smile", "Compiler", buildId);
        Directory.CreateDirectory(DirectoryPath);
        AssemblyPath = Path.Combine(DirectoryPath, outputBaseName + ".asm");
        ObjectPath = Path.Combine(DirectoryPath, outputBaseName + ".obj");
        DebugSourcePath = Path.Combine(DirectoryPath, outputBaseName + ".debug.c");
        DebugObjectPath = Path.Combine(DirectoryPath, outputBaseName + ".debug.obj");
    }

    public string DirectoryPath { get; }
    public string AssemblyPath { get; }
    public string ObjectPath { get; }
    public string DebugSourcePath { get; }
    public string DebugObjectPath { get; }

    public void Dispose()
    {
        if (_keep)
            return;
        TryDelete(AssemblyPath);
        TryDelete(ObjectPath);
        TryDelete(DebugSourcePath);
        TryDelete(DebugObjectPath);
        try
        {
            if (Directory.Exists(DirectoryPath) && !Directory.EnumerateFileSystemEntries(DirectoryPath).Any())
                Directory.Delete(DirectoryPath);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

internal enum TransactionalPublicationStage
{
    BeforeCommit,
    AfterFileCommit,
    BeforeStaleCleanup
}

internal static class TransactionalOutputPublisher
{
    public static void PublishDirectory(string stagingRoot, string outputRoot,
        IReadOnlyList<string> currentRelativePaths, IReadOnlyCollection<string> previousRelativePaths,
        Action<TransactionalPublicationStage, string?>? testHook = null)
    {
        var stage = Path.GetFullPath(stagingRoot);
        var output = Path.GetFullPath(outputRoot);
        Directory.CreateDirectory(output);
        var current = currentRelativePaths.Select(NormalizeRelativePath).Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var previous = previousRelativePaths.Select(NormalizeRelativePath).Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        foreach (var relative in current)
        {
            var source = ContainedPath(stage, relative);
            if (!File.Exists(source))
                throw new IOException($"Staged publication file is missing: {source}");
        }

        var backupRoot = Path.Combine(Path.GetDirectoryName(output)!,
            "." + Path.GetFileName(output) + ".smile-backup-" + Guid.NewGuid().ToString("N"));
        var changed = current.Where(relative =>
        {
            var destination = ContainedPath(output, relative);
            return !File.Exists(destination) ||
                   !FilesHaveSameContent(ContainedPath(stage, relative), destination);
        }).ToArray();
        var backedUp = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var committed = new List<string>();
        try
        {
            foreach (var relative in changed)
            {
                var destination = ContainedPath(output, relative);
                if (!File.Exists(destination))
                    continue;
                var backup = ContainedPath(backupRoot, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                File.Copy(destination, backup, overwrite: false);
                backedUp.Add(relative);
            }

            testHook?.Invoke(TransactionalPublicationStage.BeforeCommit, null);
            foreach (var relative in changed)
            {
                ReplaceFromCopy(ContainedPath(stage, relative), ContainedPath(output, relative));
                committed.Add(relative);
                testHook?.Invoke(TransactionalPublicationStage.AfterFileCommit, relative);
            }

            testHook?.Invoke(TransactionalPublicationStage.BeforeStaleCleanup, null);
            foreach (var relative in previous.Where(item => !current.Contains(item,
                         StringComparer.OrdinalIgnoreCase)))
            {
                var stale = ContainedPath(output, relative);
                try
                {
                    if (File.Exists(stale))
                        File.Delete(stale);
                    RemoveEmptyParents(Path.GetDirectoryName(stale), output);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
        catch
        {
            foreach (var relative in committed.AsEnumerable().Reverse())
            {
                var destination = ContainedPath(output, relative);
                try
                {
                    if (backedUp.Contains(relative))
                        ReplaceFromCopy(ContainedPath(backupRoot, relative), destination);
                    else if (File.Exists(destination))
                        File.Delete(destination);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            throw;
        }
        finally
        {
            TryDeleteDirectory(backupRoot);
        }
    }

    private static bool FilesHaveSameContent(string leftPath, string rightPath)
    {
        var leftLength = new FileInfo(leftPath).Length;
        var rightLength = new FileInfo(rightPath).Length;
        if (leftLength != rightLength)
            return false;
        if (leftLength == 0)
            return true;

        using var left = new FileStream(leftPath, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete, 128 * 1024, FileOptions.SequentialScan);
        using var right = new FileStream(rightPath, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete, 128 * 1024, FileOptions.SequentialScan);
        var leftHash = SHA256.HashData(left);
        var rightHash = SHA256.HashData(right);
        return leftHash.AsSpan().SequenceEqual(rightHash);
    }

    internal static string CreateStagingDirectory(string targetPath)
    {
        var fullTarget = Path.GetFullPath(targetPath);
        var parent = Directory.Exists(fullTarget) || !Path.HasExtension(fullTarget)
            ? Path.GetDirectoryName(fullTarget)!
            : Path.GetDirectoryName(fullTarget)!;
        var name = Path.GetFileName(fullTarget.TrimEnd(Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar));
        var staging = Path.Combine(parent, "." + name + ".smile-staging-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        return staging;
    }

    internal static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void ReplaceFromCopy(string source, string destination)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var temporary = Path.Combine(Path.GetDirectoryName(destination)!,
            "." + Path.GetFileName(destination) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.Copy(source, temporary, overwrite: false);
            using (var stream = new FileStream(temporary, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                stream.Flush(flushToDisk: true);
            if (File.Exists(destination))
                File.Replace(temporary, destination, null);
            else
                File.Move(temporary, destination);
        }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static string NormalizeRelativePath(string path)
    {
        var normalized = path.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        if (string.IsNullOrWhiteSpace(normalized) || Path.IsPathRooted(normalized) ||
            normalized.Split(Path.DirectorySeparatorChar).Any(part => part is "" or "." or ".."))
            throw new ArgumentException($"Unsafe managed publication path: '{path}'.");
        return normalized;
    }

    private static string ContainedPath(string root, string relative)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);
        var path = Path.GetFullPath(Path.Combine(fullRoot, relative));
        if (!path.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Managed publication path escaped its root: '{relative}'.");
        return path;
    }

    private static void RemoveEmptyParents(string? directory, string outputRoot)
    {
        var root = Path.GetFullPath(outputRoot).TrimEnd(Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);
        while (!string.IsNullOrWhiteSpace(directory) &&
               !string.Equals(Path.GetFullPath(directory), root, StringComparison.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(directory) || Directory.EnumerateFileSystemEntries(directory).Any())
                return;
            Directory.Delete(directory);
            directory = Path.GetDirectoryName(directory);
        }
    }
}
