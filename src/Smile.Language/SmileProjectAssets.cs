using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Smile.Language;

public sealed class SmileProjectAssetInclude
{
    internal SmileProjectAssetInclude(string originalText, string normalizedPattern, string projectFilePath,
        int line, int column, bool hasWildcards, string searchRootLogicalPath, string searchRootFullPath,
        bool watchSubdirectories, IReadOnlyList<string> segments, bool isValid)
    {
        OriginalText = originalText;
        NormalizedPattern = normalizedPattern;
        ProjectFilePath = projectFilePath;
        Line = line;
        Column = column;
        HasWildcards = hasWildcards;
        SearchRootLogicalPath = searchRootLogicalPath;
        SearchRootFullPath = searchRootFullPath;
        WatchSubdirectories = watchSubdirectories;
        Segments = segments;
        IsValid = isValid;
    }

    public string OriginalText { get; }
    public string NormalizedPattern { get; }
    public string ProjectFilePath { get; }
    public int Line { get; }
    public int Column { get; }
    public bool HasWildcards { get; }
    public string SearchRootLogicalPath { get; }
    public string SearchRootFullPath { get; }
    public bool WatchSubdirectories { get; }
    internal IReadOnlyList<string> Segments { get; }
    public bool IsValid { get; }
}

public sealed class SmileProjectAssetItem
{
    internal SmileProjectAssetItem(string logicalPath, string fullPath, IReadOnlyList<SmileProjectAssetInclude> includes)
    {
        LogicalPath = logicalPath;
        FullPath = Path.GetFullPath(fullPath);
        MatchedIncludes = includes;
        var file = new FileInfo(FullPath);
        FileSize = file.Length;
        LastWriteTimeUtc = file.LastWriteTimeUtc;
    }

    public string LogicalPath { get; }
    public string FullPath { get; }
    public IReadOnlyList<SmileProjectAssetInclude> MatchedIncludes { get; }
    public long FileSize { get; }
    public DateTime LastWriteTimeUtc { get; }
}

public sealed class SmileProjectAssetManifest
{
    internal SmileProjectAssetManifest(string projectPath, IReadOnlyList<SmileProjectAssetInclude> includes,
        IReadOnlyList<SmileProjectAssetItem> items, IReadOnlyList<SmileProjectDiagnostic> diagnostics)
    {
        ProjectPath = Path.GetFullPath(projectPath);
        ProjectDirectory = Path.GetDirectoryName(ProjectPath) ?? Environment.CurrentDirectory;
        Includes = includes;
        Items = items;
        Diagnostics = diagnostics;
        AssetPaths = items.Select(item => item.LogicalPath).ToArray();
    }

    public string ProjectPath { get; }
    public string ProjectDirectory { get; }
    public IReadOnlyList<SmileProjectAssetInclude> Includes { get; }
    public IReadOnlyList<SmileProjectAssetItem> Items { get; }
    public IReadOnlyList<string> AssetPaths { get; }
    public IReadOnlyList<SmileProjectDiagnostic> Diagnostics { get; }

    public void ValidateForBuild()
    {
        var error = Diagnostics.FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        if (error != null)
            throw new SmileProjectDiagnosticException(error.Code, error.Message, error.FilePath,
                error.Line, error.Column);
    }
}

public static class SmileProjectAssetResolver
{
    private static readonly char[] UnsupportedPatternCharacters = { '[', ']', '{', '}', '!', ';' };
    private static readonly Regex UriScheme = new(@"^[A-Za-z][A-Za-z0-9+.-]*:", RegexOptions.CultureInvariant);

    internal static SmileProjectAssetManifest Resolve(string projectPath, SmileProjectKind projectKind, XElement root)
    {
        var fullProjectPath = Path.GetFullPath(projectPath);
        var projectDirectory = Path.GetDirectoryName(fullProjectPath) ?? Environment.CurrentDirectory;
        var diagnostics = new List<SmileProjectDiagnostic>();
        var includes = new List<SmileProjectAssetInclude>();
        var elements = root.Elements().Where(element => element.Name.LocalName == "ItemGroup")
            .SelectMany(element => element.Elements().Where(item => item.Name.LocalName == "Asset"));
        foreach (var element in elements)
            includes.Add(ParseInclude(fullProjectPath, projectDirectory, element, diagnostics));

        if (projectKind == SmileProjectKind.Library && includes.Count != 0)
        {
            var include = includes[0];
            diagnostics.Add(new SmileProjectDiagnostic("SML3606",
                "Library project assets are not supported yet; declare runtime assets in the consuming application project.",
                fullProjectPath, include.Line, include.Column));
            return new SmileProjectAssetManifest(fullProjectPath, includes, Array.Empty<SmileProjectAssetItem>(), diagnostics);
        }

        var builders = new Dictionary<string, AssetBuilder>(StringComparer.Ordinal);
        var portable = new Dictionary<string, AssetBuilder>(StringComparer.OrdinalIgnoreCase);
        foreach (var include in includes.Where(item => item.IsValid))
        {
            if (!include.HasWildcards)
            {
                if (!TryResolveActualPath(projectDirectory, include.Segments, requireFile: true,
                        out var actualPath, out var actualLogical, out var caseMismatch))
                {
                    diagnostics.Add(new SmileProjectDiagnostic("SML3601",
                        $"Explicit asset '{include.OriginalText}' was not found at '{Path.Combine(projectDirectory, include.NormalizedPattern.Replace('/', Path.DirectorySeparatorChar))}'.",
                        fullProjectPath, include.Line, include.Column));
                    continue;
                }
                if (caseMismatch)
                    AddCaseDiagnostic(diagnostics, include, actualLogical);
                AddCandidate(projectDirectory, actualPath, actualLogical, include, builders, portable, diagnostics);
                continue;
            }

            var rootSegments = SplitLogical(include.SearchRootLogicalPath);
            if (!TryResolveActualPath(projectDirectory, rootSegments, requireFile: false,
                    out var searchRoot, out var actualRootLogical, out var rootCaseMismatch))
                continue;
            if (rootCaseMismatch)
                AddCaseDiagnostic(diagnostics, include, actualRootLogical);

            var searchOption = include.WatchSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var candidate in Directory.EnumerateFiles(searchRoot, "*", searchOption)
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                if (!TryProjectRelativePath(projectDirectory, candidate, out var logicalPath) ||
                    !GlobMatches(include.Segments, SplitLogical(logicalPath)))
                    continue;
                AddCandidate(projectDirectory, candidate, logicalPath, include, builders, portable, diagnostics);
            }
        }

        var items = builders.Values.OrderBy(builder => builder.LogicalPath, StringComparer.Ordinal)
            .Select(builder => new SmileProjectAssetItem(builder.LogicalPath, builder.FullPath,
                builder.Includes.ToArray())).ToArray();
        return new SmileProjectAssetManifest(fullProjectPath, includes, items, diagnostics);
    }

    private static SmileProjectAssetInclude ParseInclude(string projectPath, string projectDirectory,
        XElement element, List<SmileProjectDiagnostic> diagnostics)
    {
        var attribute = element.Attribute("Include");
        var original = (attribute?.Value ?? string.Empty).Trim();
        IXmlLineInfo lineInfo = attribute != null ? attribute : element;
        var line = lineInfo?.HasLineInfo() == true ? lineInfo.LineNumber : 1;
        var column = lineInfo?.HasLineInfo() == true ? lineInfo.LinePosition : 1;
        if (!TryNormalizePattern(original, allowWildcards: true, out var normalized, out var segments,
                out var hasWildcards, out var error))
        {
            diagnostics.Add(new SmileProjectDiagnostic("SML3600",
                $"Invalid Asset Include '{original}': {error}", projectPath, line, column));
            return new SmileProjectAssetInclude(original, string.Empty, projectPath, line, column, false,
                string.Empty, projectDirectory, false, Array.Empty<string>(), isValid: false);
        }

        var wildcardIndex = Array.FindIndex(segments, segment => segment.IndexOfAny(new[] { '*', '?' }) >= 0);
        var rootCount = wildcardIndex < 0 ? Math.Max(0, segments.Length - 1) : wildcardIndex;
        var rootSegments = segments.Take(rootCount).ToArray();
        var rootLogical = string.Join("/", rootSegments);
        var rootFull = Path.GetFullPath(Path.Combine(projectDirectory,
            rootLogical.Replace('/', Path.DirectorySeparatorChar)));
        var remainingSegments = segments.Length - rootCount;
        var recursiveWatch = hasWildcards && (remainingSegments > 1 || segments.Contains("**", StringComparer.Ordinal));
        return new SmileProjectAssetInclude(original, normalized, projectPath, line, column, hasWildcards,
            rootLogical, rootFull, recursiveWatch, segments, isValid: true);
    }

    internal static bool TryNormalizePattern(string text, bool allowWildcards, out string normalized,
        out string[] segments, out bool hasWildcards, out string error)
    {
        normalized = string.Empty;
        segments = Array.Empty<string>();
        hasWildcards = false;
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            error = "the include is empty.";
            return false;
        }
        if (text.IndexOf('\0') >= 0)
        {
            error = "NUL characters are not allowed.";
            return false;
        }

        var value = text.Replace('\\', '/');
        if (value.StartsWith("/", StringComparison.Ordinal) || value.StartsWith("//", StringComparison.Ordinal) ||
            Path.IsPathRooted(value) || UriScheme.IsMatch(value) || value.IndexOf(':') >= 0)
        {
            error = "assets must use a project-relative path, not a rooted, drive, UNC, or URI path.";
            return false;
        }
        if (value.EndsWith("/", StringComparison.Ordinal) || value.Contains("//"))
        {
            error = "empty path segments are not supported.";
            return false;
        }
        if (value.IndexOfAny(UnsupportedPatternCharacters) >= 0)
        {
            error = "character classes, braces, negation, and semicolon patterns are not supported.";
            return false;
        }

        var result = new List<string>();
        foreach (var segment in value.Split('/'))
        {
            if (segment == ".")
                continue;
            if (segment == "..")
            {
                if (result.Count == 0)
                {
                    error = "the path escapes the project directory.";
                    return false;
                }
                result.RemoveAt(result.Count - 1);
                continue;
            }
            if (segment.Length == 0)
            {
                error = "empty path segments are not supported.";
                return false;
            }
            if (segment.Contains("**") && segment != "**")
            {
                error = "** is special only as a complete path segment.";
                return false;
            }
            if (!allowWildcards && segment.IndexOfAny(new[] { '*', '?' }) >= 0)
            {
                error = "wildcards are not allowed in a concrete asset path.";
                return false;
            }
            result.Add(segment);
        }

        if (result.Count == 0)
        {
            error = "the normalized path is empty.";
            return false;
        }
        segments = result.ToArray();
        normalized = string.Join("/", segments);
        hasWildcards = segments.Any(segment => segment.IndexOfAny(new[] { '*', '?' }) >= 0);
        return true;
    }

    private static void AddCaseDiagnostic(List<SmileProjectDiagnostic> diagnostics,
        SmileProjectAssetInclude include, string actualLogical)
    {
        if (diagnostics.Any(item => item.Code == "SML3602" && item.Line == include.Line && item.Column == include.Column))
            return;
        diagnostics.Add(new SmileProjectDiagnostic("SML3602",
            $"Asset Include '{include.OriginalText}' does not match the filesystem case; actual path is '{actualLogical}'.",
            include.ProjectFilePath, include.Line, include.Column));
    }

    private static void AddCandidate(string projectDirectory, string fullPath, string logicalPath,
        SmileProjectAssetInclude include, Dictionary<string, AssetBuilder> builders,
        Dictionary<string, AssetBuilder> portable, List<SmileProjectDiagnostic> diagnostics)
    {
        var normalizedFullPath = Path.GetFullPath(fullPath);
        if (!TryProjectRelativePath(projectDirectory, normalizedFullPath, out var containedLogical) ||
            !string.Equals(logicalPath, containedLogical, StringComparison.Ordinal))
            return;
        if (builders.TryGetValue(logicalPath, out var exact))
        {
            if (!string.Equals(exact.FullPath, normalizedFullPath, StringComparison.Ordinal))
                AddCollision(diagnostics, include, exact, normalizedFullPath, logicalPath);
            else if (!exact.Includes.Contains(include))
                exact.Includes.Add(include);
            return;
        }
        if (portable.TryGetValue(logicalPath, out var collision) &&
            (!string.Equals(collision.LogicalPath, logicalPath, StringComparison.Ordinal) ||
             !string.Equals(collision.FullPath, normalizedFullPath, StringComparison.Ordinal)))
        {
            AddCollision(diagnostics, include, collision, normalizedFullPath, logicalPath);
            return;
        }

        var builder = new AssetBuilder(logicalPath, normalizedFullPath, include);
        builders.Add(logicalPath, builder);
        portable[logicalPath] = builder;
    }

    private static void AddCollision(List<SmileProjectDiagnostic> diagnostics, SmileProjectAssetInclude include,
        AssetBuilder existing, string secondPath, string logicalPath) => diagnostics.Add(
        new SmileProjectDiagnostic("SML3603",
            $"Assets '{existing.FullPath}' and '{secondPath}' collide at portable destination '{logicalPath}'.",
            include.ProjectFilePath, include.Line, include.Column));

    internal static SmileProjectDiagnostic? FindDestinationCollision(string projectPath,
        IEnumerable<KeyValuePair<string, string>> candidates)
    {
        var identities = new Dictionary<string, KeyValuePair<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            if (identities.TryGetValue(candidate.Key, out var existing) &&
                (!string.Equals(existing.Key, candidate.Key, StringComparison.Ordinal) ||
                 !string.Equals(existing.Value, candidate.Value, StringComparison.Ordinal)))
                return new SmileProjectDiagnostic("SML3603",
                    $"Assets '{existing.Value}' and '{candidate.Value}' collide at portable destination '{candidate.Key}'.",
                    projectPath);
            identities[candidate.Key] = candidate;
        }
        return null;
    }

    private static bool TryResolveActualPath(string projectDirectory, IReadOnlyList<string> relativeSegments,
        bool requireFile, out string fullPath, out string actualLogical, out bool caseMismatch)
    {
        var current = Path.GetFullPath(projectDirectory);
        var actualSegments = new List<string>();
        caseMismatch = false;
        if (relativeSegments.Count == 0)
        {
            fullPath = current;
            actualLogical = string.Empty;
            return Directory.Exists(current) && !requireFile;
        }

        for (var index = 0; index < relativeSegments.Count; index++)
        {
            if (!Directory.Exists(current))
            {
                fullPath = Path.Combine(current, relativeSegments[index]);
                actualLogical = string.Join("/", actualSegments.Concat(relativeSegments.Skip(index)));
                return false;
            }
            var expected = relativeSegments[index];
            var entries = Directory.EnumerateFileSystemEntries(current)
                .OrderBy(entry => entry, StringComparer.Ordinal).ToArray();
            var match = entries.FirstOrDefault(entry =>
                string.Equals(Path.GetFileName(entry), expected, StringComparison.Ordinal));
            if (match == null)
            {
                match = entries.FirstOrDefault(entry =>
                    string.Equals(Path.GetFileName(entry), expected, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    caseMismatch = true;
            }
            if (match == null)
            {
                fullPath = Path.Combine(current, expected);
                actualLogical = string.Join("/", actualSegments.Concat(relativeSegments.Skip(index)));
                return false;
            }
            actualSegments.Add(Path.GetFileName(match));
            current = Path.GetFullPath(match);
        }

        fullPath = current;
        actualLogical = string.Join("/", actualSegments);
        return requireFile ? File.Exists(current) : Directory.Exists(current);
    }

    private static bool TryProjectRelativePath(string projectDirectory, string fullPath, out string logicalPath)
    {
        var root = Path.GetFullPath(projectDirectory).TrimEnd(Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var normalized = Path.GetFullPath(fullPath);
        if (!normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            logicalPath = string.Empty;
            return false;
        }
        logicalPath = normalized.Substring(root.Length).Replace('\\', '/');
        return logicalPath.Length != 0;
    }

    private static string[] SplitLogical(string path) => string.IsNullOrEmpty(path)
        ? Array.Empty<string>() : path.Split('/');

    private static bool GlobMatches(IReadOnlyList<string> pattern, IReadOnlyList<string> path)
    {
        var memo = new Dictionary<long, bool>();
        return Match(0, 0);

        bool Match(int patternIndex, int pathIndex)
        {
            var key = ((long)patternIndex << 32) | (uint)pathIndex;
            if (memo.TryGetValue(key, out var known))
                return known;
            bool result;
            if (patternIndex == pattern.Count)
                result = pathIndex == path.Count;
            else if (pattern[patternIndex] == "**")
                result = Match(patternIndex + 1, pathIndex) ||
                         (pathIndex < path.Count && Match(patternIndex, pathIndex + 1));
            else
                result = pathIndex < path.Count && SegmentMatches(pattern[patternIndex], path[pathIndex]) &&
                         Match(patternIndex + 1, pathIndex + 1);
            memo[key] = result;
            return result;
        }
    }

    private static bool SegmentMatches(string pattern, string value)
    {
        var previous = new bool[value.Length + 1];
        previous[0] = true;
        foreach (var token in pattern)
        {
            var next = new bool[value.Length + 1];
            if (token == '*')
            {
                next[0] = previous[0];
                for (var index = 1; index <= value.Length; index++)
                    next[index] = previous[index] || next[index - 1];
            }
            else
            {
                for (var index = 1; index <= value.Length; index++)
                    next[index] = previous[index - 1] && (token == '?' || token == value[index - 1]);
            }
            previous = next;
        }
        return previous[value.Length];
    }

    private sealed class AssetBuilder
    {
        public AssetBuilder(string logicalPath, string fullPath, SmileProjectAssetInclude include)
        {
            LogicalPath = logicalPath;
            FullPath = fullPath;
            Includes = new List<SmileProjectAssetInclude> { include };
        }
        public string LogicalPath { get; }
        public string FullPath { get; }
        public List<SmileProjectAssetInclude> Includes { get; }
    }
}

public sealed class SmileProjectAssetPublishResult
{
    internal SmileProjectAssetPublishResult(int publishedCount, string manifestPath,
        IReadOnlyList<SmileProjectDiagnostic> warnings)
    {
        PublishedCount = publishedCount;
        ManifestPath = manifestPath;
        Warnings = warnings;
    }
    public int PublishedCount { get; }
    public string ManifestPath { get; }
    public IReadOnlyList<SmileProjectDiagnostic> Warnings { get; }
}

public static class SmileProjectAssetPublisher
{
    public static SmileProjectAssetPublishResult Publish(SmileProjectAssetManifest manifest, string outputRoot,
        string applicationIdentity, string target, string? nativeOutputBaseName = null,
        bool hasExplicitApplicationIdentity = false)
        => Publish(manifest, outputRoot, applicationIdentity, target, nativeOutputBaseName,
            hasExplicitApplicationIdentity, null);

    internal static SmileProjectAssetPublishResult Publish(SmileProjectAssetManifest manifest, string outputRoot,
        string applicationIdentity, string target, string? nativeOutputBaseName,
        bool hasExplicitApplicationIdentity, Action<SmileAssetPublicationStage, string?>? testHook)
    {
        manifest.ValidateForBuild();
        var root = Path.GetFullPath(outputRoot);
        Directory.CreateDirectory(root);
        var isWeb = string.Equals(target, "web", StringComparison.OrdinalIgnoreCase);
        var manifestName = GetManifestName(applicationIdentity, isWeb, nativeOutputBaseName,
            hasExplicitApplicationIdentity);
        var manifestPath = ContainedDestination(root, manifestName);
        var warnings = new List<SmileProjectDiagnostic>();
        var previous = ReadPreviousManifest(manifestPath, applicationIdentity, target, root, manifest.ProjectPath, warnings);
        var legacyManifests = new List<(string Path, PublicationManifest Manifest)>();
        if (!isWeb && hasExplicitApplicationIdentity)
        {
            foreach (var candidatePath in Directory.EnumerateFiles(root, "*.smile-assets.json")
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                if (string.Equals(candidatePath, manifestPath, StringComparison.OrdinalIgnoreCase))
                    continue;
                var candidate = ReadPreviousManifest(candidatePath, applicationIdentity, target, root,
                    manifest.ProjectPath, warnings, warnOnIdentityMismatch: false);
                if (candidate != null)
                    legacyManifests.Add((candidatePath, candidate));
            }
        }
        var currentPaths = new HashSet<string>(manifest.AssetPaths, StringComparer.Ordinal);

        var priorAssets = new HashSet<string>(StringComparer.Ordinal);
        if (previous != null)
            priorAssets.UnionWith(previous.Assets);
        foreach (var legacy in legacyManifests)
            priorAssets.UnionWith(legacy.Manifest.Assets);
        var stagingRoot = Path.Combine(root, ".smile-assets-staging-" + Guid.NewGuid().ToString("N"));
        var backupRoot = Path.Combine(root, ".smile-assets-backup-" + Guid.NewGuid().ToString("N"));
        var committed = new List<string>();
        var backedUp = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            Directory.CreateDirectory(stagingRoot);
            foreach (var item in manifest.Items)
            {
                testHook?.Invoke(SmileAssetPublicationStage.BeforeAssetStage, item.LogicalPath);
                var staged = ContainedDestination(stagingRoot, item.LogicalPath);
                Directory.CreateDirectory(Path.GetDirectoryName(staged)!);
                File.Copy(item.FullPath, staged, overwrite: false);
                File.SetLastWriteTimeUtc(staged, item.LastWriteTimeUtc);
            }
            WriteManifest(ContainedDestination(stagingRoot, manifestName), new PublicationManifest
            {
                FormatVersion = 1,
                ApplicationIdentity = applicationIdentity,
                Target = target,
                Assets = manifest.AssetPaths.ToList()
            });

            var publicationPaths = manifest.AssetPaths.Concat(new[] { manifestName }).ToArray();
            foreach (var relative in publicationPaths)
            {
                var destination = ContainedDestination(root, relative);
                if (!File.Exists(destination))
                    continue;
                var backup = ContainedDestination(backupRoot, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                File.Copy(destination, backup, overwrite: false);
                backedUp.Add(relative);
            }

            testHook?.Invoke(SmileAssetPublicationStage.BeforeCommit, null);
            foreach (var relative in publicationPaths)
            {
                ReplaceFromCopy(ContainedDestination(stagingRoot, relative), ContainedDestination(root, relative));
                committed.Add(relative);
                testHook?.Invoke(SmileAssetPublicationStage.AfterFileCommit, relative);
            }

            testHook?.Invoke(SmileAssetPublicationStage.BeforeStaleCleanup, null);
            foreach (var stale in priorAssets.Where(asset => !currentPaths.Contains(asset)))
            {
                var stalePath = ContainedDestination(root, stale);
                try
                {
                    if (File.Exists(stalePath))
                        File.Delete(stalePath);
                    RemoveEmptyParents(Path.GetDirectoryName(stalePath), root);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    warnings.Add(new SmileProjectDiagnostic("SML3605",
                        $"The stale managed asset '{stale}' could not be removed after successful publication: {exception.Message}",
                        manifest.ProjectPath, severity: DiagnosticSeverity.Warning));
                }
            }
            foreach (var legacy in legacyManifests)
            {
                try { File.Delete(legacy.Path); }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    warnings.Add(new SmileProjectDiagnostic("SML3605",
                        $"The legacy managed asset manifest '{legacy.Path}' could not be removed after successful publication: {exception.Message}",
                        manifest.ProjectPath, severity: DiagnosticSeverity.Warning));
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            foreach (var relative in committed.AsEnumerable().Reverse())
            {
                var destination = ContainedDestination(root, relative);
                try
                {
                    if (backedUp.Contains(relative))
                        ReplaceFromCopy(ContainedDestination(backupRoot, relative), destination);
                    else if (File.Exists(destination))
                        File.Delete(destination);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            throw new SmileProjectDiagnosticException("SML3604",
                $"Project asset publication failed beneath '{root}': {exception.Message}", manifest.ProjectPath);
        }
        finally
        {
            TryDeleteDirectory(stagingRoot);
            TryDeleteDirectory(backupRoot);
        }

        return new SmileProjectAssetPublishResult(manifest.Items.Count, manifestPath, warnings);
    }

    internal static SmilePublishedAssetSnapshot ReadPublishedAssets(string outputRoot, string applicationIdentity,
        string target, string projectPath, string? nativeOutputBaseName = null,
        bool hasExplicitApplicationIdentity = false)
    {
        var root = Path.GetFullPath(outputRoot);
        var isWeb = string.Equals(target, "web", StringComparison.OrdinalIgnoreCase);
        var manifestName = GetManifestName(applicationIdentity, isWeb, nativeOutputBaseName,
            hasExplicitApplicationIdentity);
        var warnings = new List<SmileProjectDiagnostic>();
        var previous = ReadPreviousManifest(ContainedDestination(root, manifestName), applicationIdentity, target,
            root, projectPath, warnings);
        var assets = new HashSet<string>(previous?.Assets ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
        var legacyManifestNames = new List<string>();
        if (!isWeb && hasExplicitApplicationIdentity && Directory.Exists(root))
        {
            foreach (var candidatePath in Directory.EnumerateFiles(root, "*.smile-assets.json")
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                if (string.Equals(candidatePath, ContainedDestination(root, manifestName),
                        StringComparison.OrdinalIgnoreCase))
                    continue;
                var candidate = ReadPreviousManifest(candidatePath, applicationIdentity, target, root,
                    projectPath, warnings, warnOnIdentityMismatch: false);
                if (candidate == null)
                    continue;
                assets.UnionWith(candidate.Assets);
                legacyManifestNames.Add(Path.GetFileName(candidatePath));
            }
        }
        return new SmilePublishedAssetSnapshot(manifestName,
            assets.OrderBy(path => path, StringComparer.Ordinal).ToArray(), legacyManifestNames, warnings);
    }

    private static PublicationManifest? ReadPreviousManifest(string manifestPath, string applicationIdentity,
        string target, string outputRoot, string projectPath, List<SmileProjectDiagnostic> warnings,
        bool warnOnIdentityMismatch = true)
    {
        if (!File.Exists(manifestPath))
            return null;
        try
        {
            PublicationManifest data;
            using (var stream = File.OpenRead(manifestPath))
                data = (PublicationManifest?)new DataContractJsonSerializer(typeof(PublicationManifest)).ReadObject(stream)
                       ?? throw new SerializationException("The manifest was empty.");
            if (!string.Equals(data.ApplicationIdentity, applicationIdentity, StringComparison.Ordinal) &&
                !warnOnIdentityMismatch)
                return null;
            if (data.FormatVersion != 1 || data.Assets == null ||
                !string.Equals(data.ApplicationIdentity, applicationIdentity, StringComparison.Ordinal) ||
                !string.Equals(data.Target, target, StringComparison.OrdinalIgnoreCase))
                throw new SerializationException("The manifest identity, target, or format version was invalid.");
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var asset in data.Assets)
            {
                if (!SmileProjectAssetResolver.TryNormalizePattern(asset, allowWildcards: false,
                        out var normalized, out _, out _, out _) ||
                    !string.Equals(asset, normalized, StringComparison.Ordinal) || !identities.Add(asset))
                    throw new SerializationException($"Unsafe or noncanonical asset path '{asset}'.");
                ContainedDestination(outputRoot, asset);
            }
            return data;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                          SerializationException or ArgumentException)
        {
            warnings.Add(new SmileProjectDiagnostic("SML3605",
                $"The previous asset publication manifest was unsafe or malformed and was ignored: {exception.Message}",
                projectPath, severity: DiagnosticSeverity.Warning));
            return null;
        }
    }

    private static void CopyAsset(SmileProjectAssetItem item, string destination)
    {
        var destinationDirectory = Path.GetDirectoryName(destination)!;
        Directory.CreateDirectory(destinationDirectory);
        if (File.Exists(destination))
        {
            var existing = new FileInfo(destination);
            if (existing.Length == item.FileSize && existing.LastWriteTimeUtc == item.LastWriteTimeUtc)
                return;
        }

        var temporary = Path.Combine(destinationDirectory,
            "." + Path.GetFileName(destination) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.Copy(item.FullPath, temporary, overwrite: false);
            File.SetLastWriteTimeUtc(temporary, item.LastWriteTimeUtc);
            ReplaceFile(temporary, destination);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    private static void ReplaceFromCopy(string source, string destination)
    {
        var destinationDirectory = Path.GetDirectoryName(destination)!;
        Directory.CreateDirectory(destinationDirectory);
        var temporary = Path.Combine(destinationDirectory,
            "." + Path.GetFileName(destination) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.Copy(source, temporary, overwrite: false);
            File.SetLastWriteTimeUtc(temporary, File.GetLastWriteTimeUtc(source));
            ReplaceFile(temporary, destination);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    private static void WriteManifest(string manifestPath, PublicationManifest manifest)
    {
        var temporary = manifestPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                new DataContractJsonSerializer(typeof(PublicationManifest)).WriteObject(stream, manifest);
                stream.Flush();
            }
            ReplaceFile(temporary, manifestPath);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    private static void ReplaceFile(string temporary, string destination)
    {
        if (File.Exists(destination))
            File.Replace(temporary, destination, null);
        else
            File.Move(temporary, destination);
    }

    private static string ContainedDestination(string outputRoot, string logicalPath)
    {
        var root = Path.GetFullPath(outputRoot);
        var destination = Path.GetFullPath(Path.Combine(root, logicalPath.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                     Path.DirectorySeparatorChar;
        if (!destination.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Asset destination escaped the publication root: '{logicalPath}'.");
        return destination;
    }

    private static void RemoveEmptyParents(string? directory, string outputRoot)
    {
        var root = DirectoryIdentity(outputRoot);
        while (!string.IsNullOrWhiteSpace(directory) &&
               !string.Equals(DirectoryIdentity(directory!), root, StringComparison.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(directory) || Directory.EnumerateFileSystemEntries(directory).Any())
                return;
            Directory.Delete(directory);
            directory = Path.GetDirectoryName(directory);
        }
    }

    private static string DirectoryIdentity(string path) => Path.GetFullPath(path)
        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
            builder.Append(invalid.Contains(character) ? '_' : character);
        return builder.Length == 0 ? "smile-output" : builder.ToString();
    }

    private static string GetManifestName(string applicationIdentity, bool isWeb, string? nativeOutputBaseName,
        bool hasExplicitApplicationIdentity) => isWeb
        ? "smile-assets.json"
        : SafeFileName(hasExplicitApplicationIdentity ? applicationIdentity :
            nativeOutputBaseName ?? applicationIdentity) + ".smile-assets.json";

    private static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    [DataContract]
    private sealed class PublicationManifest
    {
        [DataMember(Name = "formatVersion", Order = 0)]
        public int FormatVersion { get; set; }
        [DataMember(Name = "applicationIdentity", Order = 1)]
        public string ApplicationIdentity { get; set; } = string.Empty;
        [DataMember(Name = "target", Order = 2)]
        public string Target { get; set; } = string.Empty;
        [DataMember(Name = "assets", Order = 3)]
        public List<string> Assets { get; set; } = new();
    }
}

internal enum SmileAssetPublicationStage
{
    BeforeAssetStage,
    BeforeCommit,
    AfterFileCommit,
    BeforeStaleCleanup
}

internal sealed class SmilePublishedAssetSnapshot
{
    public SmilePublishedAssetSnapshot(string manifestName, IReadOnlyList<string> assetPaths,
        IReadOnlyList<string> legacyManifestNames, IReadOnlyList<SmileProjectDiagnostic> warnings)
    {
        ManifestName = manifestName;
        AssetPaths = assetPaths;
        LegacyManifestNames = legacyManifestNames;
        Warnings = warnings;
    }

    public string ManifestName { get; }
    public IReadOnlyList<string> AssetPaths { get; }
    public IReadOnlyList<string> LegacyManifestNames { get; }
    public IReadOnlyList<SmileProjectDiagnostic> Warnings { get; }
}
