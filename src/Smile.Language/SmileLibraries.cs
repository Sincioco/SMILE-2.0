using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Xml;

namespace Smile.Language;

public sealed class SmileLibraryIdentity
{
    internal SmileLibraryIdentity(string name, string version, IReadOnlyList<string> modules,
        IReadOnlyList<SmileLibraryDependency> dependencies)
    {
        Name = name;
        Version = version;
        Modules = modules;
        Dependencies = dependencies;
    }

    public string Name { get; }
    public string Version { get; }
    public string Provider => Name + "@" + Version;
    public IReadOnlyList<string> Modules { get; }
    public IReadOnlyList<SmileLibraryDependency> Dependencies { get; }
}

public sealed class SmileLibraryDependency
{
    internal SmileLibraryDependency(string name, string version)
    {
        Name = name;
        Version = version;
    }

    public string Name { get; }
    public string Version { get; }
}

public enum SmileLibraryProviderKind
{
    Project,
    Package
}

public sealed class SmileProjectDiagnostic
{
    public SmileProjectDiagnostic(string code, string message, string filePath, int line = 1, int column = 1,
        DiagnosticSeverity severity = DiagnosticSeverity.Error)
    {
        Code = code;
        Message = message;
        FilePath = SmileSourceDocument.NormalizePath(filePath);
        Line = Math.Max(1, line);
        Column = Math.Max(1, column);
        Severity = severity;
    }

    public string Code { get; }
    public string Message { get; }
    public string FilePath { get; }
    public int Line { get; }
    public int Column { get; }
    public DiagnosticSeverity Severity { get; }
    public string FormatCompiler() =>
        $"{(string.IsNullOrWhiteSpace(FilePath) ? "<project>" : FilePath)}({Line},{Column}): " +
        $"{(Severity == DiagnosticSeverity.Error ? "error" : "warning")} {Code}: {Message}";

    public static bool TryCreate(Exception exception, string fallbackPath, out SmileProjectDiagnostic diagnostic)
    {
        if (exception is SmileProjectDiagnosticException projectException)
        {
            diagnostic = projectException.Diagnostic;
            return true;
        }

        var path = exception is FileNotFoundException missing && !string.IsNullOrWhiteSpace(missing.FileName)
            ? missing.FileName!
            : fallbackPath;
        if (exception is FileNotFoundException)
        {
            diagnostic = new SmileProjectDiagnostic("SML3200", exception.Message, path);
            return true;
        }
        if (exception is InvalidDataException or XmlException or ArgumentException)
        {
            diagnostic = new SmileProjectDiagnostic("SML3206", exception.Message, path);
            return true;
        }
        if (exception is IOException or UnauthorizedAccessException)
        {
            diagnostic = new SmileProjectDiagnostic("SML3209",
                $"SMILE project or library data could not be read: {exception.Message}", path);
            return true;
        }

        diagnostic = null!;
        return false;
    }
}

public sealed class SmileProjectDiagnosticException : Exception
{
    public SmileProjectDiagnosticException(string code, string message, string filePath, int line = 1, int column = 1)
        : base(message)
    {
        Diagnostic = new SmileProjectDiagnostic(code, message, filePath, line, column);
    }

    public SmileProjectDiagnostic Diagnostic { get; }
    public string Code => Diagnostic.Code;
    public string FilePath => Diagnostic.FilePath;
}

public sealed class SmileLibraryLoadResult
{
    internal SmileLibraryLoadResult(SmileLibraryIdentity identity, IReadOnlyList<SmileSourceDocument> sources,
        IReadOnlyDictionary<string, string> sourceIds, string packageHash, string extractionDirectory,
        string providerPath, string publicApiMetadata)
    {
        Identity = identity;
        Sources = sources;
        SourceIds = sourceIds;
        PackageHash = packageHash;
        ExtractionDirectory = extractionDirectory;
        ProviderPath = providerPath;
        PublicApiMetadata = publicApiMetadata;
    }

    public SmileLibraryIdentity Identity { get; }
    public IReadOnlyList<SmileSourceDocument> Sources { get; }
    public IReadOnlyDictionary<string, string> SourceIds { get; }
    public string PackageHash { get; }
    public string ExtractionDirectory { get; }
    public string ProviderPath { get; }
    internal string PublicApiMetadata { get; }
}

public sealed class SmileLibraryBuildFingerprint
{
    internal SmileLibraryBuildFingerprint(int formatVersion, string name, string version,
        IReadOnlyList<string> modules, IReadOnlyDictionary<string, string> sourceHashes,
        IReadOnlyList<SmileLibraryDependency> dependencies, string publicApiHash)
    {
        FormatVersion = formatVersion;
        Name = name;
        Version = version;
        Modules = modules;
        SourceHashes = sourceHashes;
        Dependencies = dependencies;
        PublicApiHash = publicApiHash;
    }

    public int FormatVersion { get; }
    public string Name { get; }
    public string Version { get; }
    public IReadOnlyList<string> Modules { get; }
    public IReadOnlyDictionary<string, string> SourceHashes { get; }
    public IReadOnlyList<SmileLibraryDependency> Dependencies { get; }
    public string PublicApiHash { get; }

    public bool Matches(SmileLibraryBuildFingerprint other)
    {
        if (other == null || FormatVersion != other.FormatVersion ||
            !string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Version, other.Version, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(PublicApiHash, other.PublicApiHash, StringComparison.OrdinalIgnoreCase) ||
            !Modules.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ThenBy(item => item, StringComparer.Ordinal)
                .SequenceEqual(other.Modules.OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(item => item, StringComparer.Ordinal),
                    StringComparer.OrdinalIgnoreCase) ||
            SourceHashes.Count != other.SourceHashes.Count)
            return false;
        foreach (var source in SourceHashes)
            if (!other.SourceHashes.TryGetValue(source.Key, out var hash) ||
                !string.Equals(source.Value, hash, StringComparison.OrdinalIgnoreCase))
                return false;
        var dependencies = Dependencies.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Name, StringComparer.Ordinal).ThenBy(item => item.Version, StringComparer.Ordinal)
            .Select(item => item.Name + "\0" + item.Version);
        var otherDependencies = other.Dependencies.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Name, StringComparer.Ordinal).ThenBy(item => item.Version, StringComparer.Ordinal)
            .Select(item => item.Name + "\0" + item.Version);
        return dependencies.SequenceEqual(otherDependencies, StringComparer.OrdinalIgnoreCase);
    }
}

internal sealed class SmileLibraryResourcePolicy
{
    public static readonly SmileLibraryResourcePolicy Production = new(
        maximumPackageBytes: 64L * 1024 * 1024,
        maximumEntries: 1026,
        maximumEntryNameCharacters: 512,
        maximumManifestBytes: 4 * 1024 * 1024,
        maximumPublicApiBytes: 16 * 1024 * 1024,
        maximumSourceBytes: 4 * 1024 * 1024,
        maximumSourceCount: 1024,
        maximumExpandedBytes: 64L * 1024 * 1024);

    public SmileLibraryResourcePolicy(long maximumPackageBytes, int maximumEntries,
        int maximumEntryNameCharacters, int maximumManifestBytes, int maximumPublicApiBytes,
        int maximumSourceBytes, int maximumSourceCount, long maximumExpandedBytes)
    {
        MaximumPackageBytes = maximumPackageBytes;
        MaximumEntries = maximumEntries;
        MaximumEntryNameCharacters = maximumEntryNameCharacters;
        MaximumManifestBytes = maximumManifestBytes;
        MaximumPublicApiBytes = maximumPublicApiBytes;
        MaximumSourceBytes = maximumSourceBytes;
        MaximumSourceCount = maximumSourceCount;
        MaximumExpandedBytes = maximumExpandedBytes;
    }

    public long MaximumPackageBytes { get; }
    public int MaximumEntries { get; }
    public int MaximumEntryNameCharacters { get; }
    public int MaximumManifestBytes { get; }
    public int MaximumPublicApiBytes { get; }
    public int MaximumSourceBytes { get; }
    public int MaximumSourceCount { get; }
    public long MaximumExpandedBytes { get; }
}

public static class SmileLibraryPackage
{
    public const int CurrentFormatVersion = 6;
    internal const string ResourceLimitDiagnosticCode = "SML3210";
    internal const string OutputLockDiagnosticCode = "SML3211";
    private static readonly DateTimeOffset DeterministicTimestamp =
        new(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan OutputLockTimeout = TimeSpan.FromSeconds(30);

    public static void Write(string outputPath, SmileProjectSourceSet project, SmileAnalysisResult analysis)
        => Write(outputPath, project, analysis, SmileLibraryResourcePolicy.Production, OutputLockTimeout);

    internal static void Write(string outputPath, SmileProjectSourceSet project, SmileAnalysisResult analysis,
        SmileLibraryResourcePolicy policy, TimeSpan outputLockTimeout, Action<string>? beforePublish = null)
    {
        if (project == null) throw new ArgumentNullException(nameof(project));
        if (analysis == null) throw new ArgumentNullException(nameof(analysis));
        if (!project.IsLibrary)
            throw new InvalidDataException("Only a SMILE library project can produce a .smilelib package.");
        if (analysis.CompilationKind != SmileCompilationKind.Library || analysis.HasErrors)
            throw new InvalidDataException("A .smilelib package requires a successful library analysis.");

        var provider = project.ProjectPath;
        var modules = analysis.SemanticModel.Modules.Values
            .Where(module => string.Equals(module.ProviderIdentity, provider, StringComparison.OrdinalIgnoreCase))
            .OrderBy(module => module.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(module => module.Name, StringComparer.Ordinal).ToArray();
        if (modules.Length == 0)
            throw new InvalidDataException("The library project does not declare a module.");

        var sourceItems = project.Items.OrderBy(item => Normalize(item.Include), StringComparer.Ordinal).ToArray();
        var sourceEntries = sourceItems.Select(item =>
        {
            var bytes = Encoding.UTF8.GetBytes(NormalizeText(File.ReadAllText(item.FullPath)));
            return new PackageEntry("src/" + Normalize(item.Include), bytes);
        }).ToArray();
        var sourceIds = sourceItems.ToDictionary(item => SmileSourceDocument.NormalizePath(item.FullPath),
            item => "src/" + Normalize(item.Include), StringComparer.OrdinalIgnoreCase);
        var hashes = sourceEntries.ToDictionary(entry => entry.Name,
            entry => Hash(entry.Bytes), StringComparer.Ordinal);
        var dependencies = GetDependencies(project);
        var manifest = BuildManifest(project, modules.Select(module => module.Name), sourceEntries.Select(entry => entry.Name), hashes, dependencies);
        var api = BuildPublicApi(modules, analysis.DependencyContext, project.LibraryName, project.Version, sourceIds);

        var entries = new List<PackageEntry>
        {
            new("manifest.json", Encoding.UTF8.GetBytes(manifest)),
            new("api/public-symbols.json", Encoding.UTF8.GetBytes(api))
        };
        entries.AddRange(sourceEntries);

        var fullOutputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
        ValidateGeneratedEntries(entries, fullOutputPath, policy);
        using var outputLock = PackageOutputLock.Acquire(fullOutputPath, outputLockTimeout);
        var temporaryPath = Path.Combine(Path.GetDirectoryName(fullOutputPath)!,
            "." + Path.GetFileName(fullOutputPath) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var file = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using (var archive = new ZipArchive(file, ZipArchiveMode.Create, leaveOpen: true, Encoding.UTF8))
                {
                    foreach (var item in entries.OrderBy(entry => entry.Name, StringComparer.Ordinal))
                    {
                        var entry = archive.CreateEntry(item.Name, CompressionLevel.NoCompression);
                        entry.LastWriteTime = DeterministicTimestamp;
                        using var stream = entry.Open();
                        stream.Write(item.Bytes, 0, item.Bytes.Length);
                    }
                }
                file.Flush(flushToDisk: true);
            }

            _ = ReadBuildFingerprint(temporaryPath, policy);
            beforePublish?.Invoke(temporaryPath);
            ReplaceFile(temporaryPath, fullOutputPath);
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    public static SmileLibraryIdentity ReadIdentity(string packagePath)
        => ReadIdentity(packagePath, SmileLibraryResourcePolicy.Production);

    internal static SmileLibraryIdentity ReadIdentity(string packagePath, SmileLibraryResourcePolicy policy)
    {
        var fullPackagePath = Path.GetFullPath(packagePath);
        ValidatePhysicalPackage(fullPackagePath, policy);
        using var file = File.OpenRead(fullPackagePath);
        using var archive = new ZipArchive(file, ZipArchiveMode.Read, leaveOpen: false, Encoding.UTF8);
        ValidateEntries(archive, fullPackagePath, policy);
        var budget = new ExpandedReadBudget(fullPackagePath, policy.MaximumExpandedBytes);
        var manifest = archive.GetEntry("manifest.json")
            ?? throw new InvalidDataException("SMILE library package is missing manifest.json.");
        return ParseIdentity(ReadManifest(ReadEntryBytes(manifest, policy.MaximumManifestBytes,
            "manifest.json", fullPackagePath, budget)));
    }

    public static SmileLibraryBuildFingerprint ReadBuildFingerprint(string packagePath)
        => ReadBuildFingerprint(packagePath, SmileLibraryResourcePolicy.Production);

    internal static SmileLibraryBuildFingerprint ReadBuildFingerprint(string packagePath,
        SmileLibraryResourcePolicy policy)
    {
        var fullPackagePath = Path.GetFullPath(packagePath);
        ValidatePhysicalPackage(fullPackagePath, policy);
        using var file = File.OpenRead(fullPackagePath);
        using var archive = new ZipArchive(file, ZipArchiveMode.Read, leaveOpen: false, Encoding.UTF8);
        ValidateEntries(archive, fullPackagePath, policy);
        var budget = new ExpandedReadBudget(fullPackagePath, policy.MaximumExpandedBytes);
        var manifestEntry = archive.GetEntry("manifest.json")
            ?? throw new InvalidDataException("SMILE library package is missing manifest.json.");
        var manifest = ReadManifest(ReadEntryBytes(manifestEntry, policy.MaximumManifestBytes,
            "manifest.json", fullPackagePath, budget));
        var identity = ParseIdentity(manifest);
        var sources = RequiredValues(manifest.Sources, "sources");
        ValidateSourceCount(sources.Count, fullPackagePath, policy);
        var declaredHashes = manifest.SourceHashes
            ?? throw new InvalidDataException("SMILE library manifest is missing sourceHashes.");
        ValidateSourceHashes(sources, declaredHashes);
        var verifiedHashes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var source in sources)
        {
            ValidateEntryName(source);
            if (!source.StartsWith("src/", StringComparison.Ordinal))
                throw new InvalidDataException($"SMILE library source entry is outside src/: {source}");
            var entry = archive.GetEntry(source)
                ?? throw new InvalidDataException($"SMILE library declared source is missing: {source}");
            var actualHash = Hash(ReadEntryBytes(entry, policy.MaximumSourceBytes, "source entry",
                fullPackagePath, budget));
            if (!declaredHashes.TryGetValue(source, out var declaredHash) ||
                !string.Equals(actualHash, declaredHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"SMILE library source hash is invalid: {source}");
            verifiedHashes.Add(source, actualHash);
        }
        var allowed = new HashSet<string>(sources, StringComparer.Ordinal)
        {
            "manifest.json", "api/public-symbols.json"
        };
        foreach (var entry in archive.Entries)
            if (!allowed.Contains(entry.FullName))
                throw new InvalidDataException($"Unexpected executable or package payload entry: {entry.FullName}");
        var apiEntry = archive.GetEntry("api/public-symbols.json")
            ?? throw new InvalidDataException("SMILE library package is missing api/public-symbols.json.");
        var publicApiHash = Hash(ReadEntryBytes(apiEntry, policy.MaximumPublicApiBytes,
            "api/public-symbols.json", fullPackagePath, budget));
        return new SmileLibraryBuildFingerprint(manifest.FormatVersion, identity.Name, identity.Version,
            identity.Modules, verifiedHashes, identity.Dependencies, publicApiHash);
    }

    public static SmileLibraryBuildFingerprint CreateBuildFingerprint(SmileProjectSourceSet project,
        SmileAnalysisResult analysis)
    {
        if (project == null) throw new ArgumentNullException(nameof(project));
        if (analysis == null) throw new ArgumentNullException(nameof(analysis));
        var modules = analysis.SemanticModel.Modules.Values
            .Where(module => string.Equals(module.ProviderIdentity, project.ProjectPath,
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(module => module.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(module => module.Name, StringComparer.Ordinal).ToArray();
        var hashes = project.Items.ToDictionary(item => "src/" + Normalize(item.Include),
            item => Hash(Encoding.UTF8.GetBytes(NormalizeText(File.ReadAllText(item.FullPath)))),
            StringComparer.Ordinal);
        var sourceIds = project.Items.ToDictionary(item => SmileSourceDocument.NormalizePath(item.FullPath),
            item => "src/" + Normalize(item.Include), StringComparer.OrdinalIgnoreCase);
        return new SmileLibraryBuildFingerprint(CurrentFormatVersion, project.LibraryName, project.Version,
            modules.Select(module => module.Name).ToArray(), hashes, GetDependencies(project),
            Hash(Encoding.UTF8.GetBytes(BuildPublicApi(modules, analysis.DependencyContext,
                project.LibraryName, project.Version, sourceIds))));
    }

    public static bool IsCurrentProjectBuild(string packagePath, SmileProjectSourceSet project,
        SmileAnalysisResult analysis)
    {
        if (!File.Exists(packagePath))
            return false;
        try
        {
            return ReadBuildFingerprint(packagePath).Matches(CreateBuildFingerprint(project, analysis));
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or
                                           UnauthorizedAccessException or SmileProjectDiagnosticException)
        {
            return false;
        }
    }

    public static SmileLibraryLoadResult Read(string packagePath, string cacheRoot)
    {
        var package = ReadEnvelope(packagePath, cacheRoot);
        SmileLibraryProviderResolver.Resolve(new[] { SmileLibraryProvider.FromPackage(package) });
        return package;
    }

    internal static SmileLibraryLoadResult ReadEnvelope(string packagePath, string cacheRoot)
        => ReadEnvelope(packagePath, cacheRoot, SmileLibraryResourcePolicy.Production);

    internal static SmileLibraryLoadResult ReadEnvelope(string packagePath, string cacheRoot,
        SmileLibraryResourcePolicy policy)
    {
        var fullPackagePath = Path.GetFullPath(packagePath);
        if (!File.Exists(fullPackagePath))
            throw new FileNotFoundException("Referenced SMILE library package was not found.", fullPackagePath);
        ValidatePhysicalPackage(fullPackagePath, policy);
        using var file = File.OpenRead(fullPackagePath);
        var packageHash = Hash(file);
        file.Position = 0;
        using var archive = new ZipArchive(file, ZipArchiveMode.Read, leaveOpen: false, Encoding.UTF8);
        ValidateEntries(archive, fullPackagePath, policy);
        var budget = new ExpandedReadBudget(fullPackagePath, policy.MaximumExpandedBytes);

        var manifestEntry = archive.GetEntry("manifest.json")
            ?? throw new InvalidDataException("SMILE library package is missing manifest.json.");
        var manifest = ReadManifest(ReadEntryBytes(manifestEntry, policy.MaximumManifestBytes,
            "manifest.json", fullPackagePath, budget));
        {
            var identity = ParseIdentity(manifest);
            var declaredSources = RequiredValues(manifest.Sources, "sources");
            ValidateSourceCount(declaredSources.Count, fullPackagePath, policy);
            var sourceHashes = manifest.SourceHashes;
            if (sourceHashes == null)
                throw new InvalidDataException("SMILE library manifest is missing sourceHashes.");
            ValidateSourceHashes(declaredSources, sourceHashes);
            var sourcePayloads = new List<(string Name, byte[] Bytes)>();
            foreach (var sourceName in declaredSources.OrderBy(item => item, StringComparer.Ordinal))
            {
                ValidateEntryName(sourceName);
                if (!sourceName.StartsWith("src/", StringComparison.Ordinal))
                    throw new InvalidDataException($"SMILE library source entry is outside src/: {sourceName}");
                var entry = archive.GetEntry(sourceName)
                    ?? throw new InvalidDataException($"SMILE library declared source is missing: {sourceName}");
                var bytes = ReadEntryBytes(entry, policy.MaximumSourceBytes, "source entry",
                    fullPackagePath, budget);
                if (!sourceHashes.TryGetValue(sourceName, out var declaredHash) ||
                    !string.Equals(declaredHash, Hash(bytes), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"SMILE library source hash is invalid: {sourceName}");
                sourcePayloads.Add((sourceName, bytes));
            }

            var allowed = new HashSet<string>(declaredSources, StringComparer.Ordinal)
            {
                "manifest.json", "api/public-symbols.json"
            };
            foreach (var entry in archive.Entries)
            {
                if (!allowed.Contains(entry.FullName))
                    throw new InvalidDataException($"Unexpected executable or package payload entry: {entry.FullName}");
            }

            var apiEntry = archive.GetEntry("api/public-symbols.json")
                ?? throw new InvalidDataException("SMILE library package is missing api/public-symbols.json.");
            var actualApi = Encoding.UTF8.GetString(ReadEntryBytes(apiEntry, policy.MaximumPublicApiBytes,
                "api/public-symbols.json", fullPackagePath, budget));

            var extractionDirectory = Path.Combine(Path.GetFullPath(cacheRoot), Safe(identity.Name),
                Safe(identity.Version), packageHash);
            Directory.CreateDirectory(extractionDirectory);
            var sources = new List<SmileSourceDocument>();
            var sourceIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var payload in sourcePayloads)
            {
                var sourceName = payload.Name;
                var bytes = payload.Bytes;
                var relative = sourceName.Substring("src/".Length).Replace('/', Path.DirectorySeparatorChar);
                var extractedPath = Path.GetFullPath(Path.Combine(extractionDirectory, relative));
                var extractionPrefix = extractionDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!extractedPath.StartsWith(extractionPrefix, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Unsafe SMILE library extraction path: {sourceName}");
                Directory.CreateDirectory(Path.GetDirectoryName(extractedPath)!);
                if (!File.Exists(extractedPath) || !File.ReadAllBytes(extractedPath).SequenceEqual(bytes))
                    File.WriteAllBytes(extractedPath, bytes);
                sources.Add(new SmileSourceDocument(Encoding.UTF8.GetString(bytes), extractedPath,
                    providerIdentity: fullPackagePath));
                sourceIds.Add(SmileSourceDocument.NormalizePath(extractedPath), sourceName);
            }
            return new SmileLibraryLoadResult(identity, sources, sourceIds, packageHash, extractionDirectory,
                fullPackagePath, actualApi);
        }
    }

    public static string BuildPublicApi(IEnumerable<ModuleSymbol> modules,
        SmileCompilationDependencyContext dependencyContext, string libraryName, string libraryVersion,
        IReadOnlyDictionary<string, string> sourceIds)
    {
        if (modules == null) throw new ArgumentNullException(nameof(modules));
        if (dependencyContext == null) throw new ArgumentNullException(nameof(dependencyContext));
        if (string.IsNullOrWhiteSpace(libraryName)) throw new ArgumentException("A library name is required.", nameof(libraryName));
        if (string.IsNullOrWhiteSpace(libraryVersion)) throw new ArgumentException("A library version is required.", nameof(libraryVersion));
        if (sourceIds == null) throw new ArgumentNullException(nameof(sourceIds));
        var provider = libraryName + "@" + libraryVersion;
        var orderedModules = modules.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Name, StringComparer.Ordinal).ToArray();
        var builder = new StringBuilder("{\n  \"formatVersion\": 6,\n  \"library\": {\"name\": \"")
            .Append(JsonEscape(libraryName)).Append("\", \"version\": \"")
            .Append(JsonEscape(libraryVersion)).Append("\", \"provider\": \"")
            .Append(JsonEscape(provider)).Append("\"},\n  \"modules\": [");
        for (var moduleIndex = 0; moduleIndex < orderedModules.Length; moduleIndex++)
        {
            var module = orderedModules[moduleIndex];
            var moduleSources = module.SyntaxTrees.Select(tree => SourceId(tree.Source, sourceIds))
                .Distinct(StringComparer.Ordinal).OrderBy(item => item, StringComparer.Ordinal).ToArray();
            builder.Append(moduleIndex == 0 ? "\n" : ",\n")
                .Append("    {\"name\": \"").Append(JsonEscape(module.Name))
                .Append("\", \"provider\": \"").Append(JsonEscape(provider))
                .Append("\", \"sources\": [")
                .Append(string.Join(", ", moduleSources.Select(source => "\"" + JsonEscape(source) + "\"")))
                .Append("], \"members\": [");
            var members = module.PublicMembers.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Name, StringComparer.Ordinal).ThenBy(item => item.Kind).ToArray();
            for (var memberIndex = 0; memberIndex < members.Length; memberIndex++)
            {
                var member = members[memberIndex];
                builder.Append(memberIndex == 0 ? "\n" : ",\n").Append("      ")
                    .Append(MemberJson(member, dependencyContext, sourceIds));
            }
            if (members.Length != 0) builder.Append('\n').Append("    ");
            builder.Append("]}");
        }
        if (orderedModules.Length != 0) builder.Append('\n').Append("  ");
        return builder.Append("]\n}\n").ToString();
    }

    private static string MemberJson(SmileModuleMember member,
        SmileCompilationDependencyContext dependencyContext, IReadOnlyDictionary<string, string> sourceIds)
    {
        var builder = new StringBuilder("{\"name\": \"").Append(JsonEscape(member.Name))
            .Append("\", \"kind\": \"").Append(member.Kind)
            .Append("\", \"visibility\": \"").Append(member.Visibility).Append('"');
        if (member.Variable != null)
        {
            builder.Append(member.Variable.IsArray ? ", \"elementType\": " : ", \"type\": ")
                .Append(TypeReferenceJson(member.Variable.Type, dependencyContext));
            if (member.Variable.IsConstant)
                builder.Append(", \"value\": ").Append(JsonValue(member.Variable.ConstantValue));
            if (member.Variable.IsArray)
                builder.Append(", \"rank\": ")
                    .Append(member.Variable.ArrayRank.ToString(CultureInfo.InvariantCulture))
                    .Append(", \"dimensions\": [")
                    .Append(string.Join(", ", member.Variable.ArrayDimensions.Select(size =>
                        size.ToString(CultureInfo.InvariantCulture)))).Append(']');
        }
        if (member.Routine != null)
        {
            builder.Append(", \"returnType\": ")
                .Append(member.Routine.IsFunction
                    ? TypeReferenceJson(member.Routine.ReturnType, dependencyContext)
                    : "null")
                .Append(", \"parameters\": [")
                .Append(string.Join(", ", member.Routine.Parameters.Select((parameter, ordinal) =>
                    ParameterJson(parameter, ordinal, dependencyContext, sourceIds))))
                .Append("], \"requiresGameWindow\": ")
                .Append(member.Routine.RequiresGameWindow ? "true" : "false");
        }
        if (member.Type != null)
        {
            builder.Append(", \"identity\": \"").Append(JsonEscape(TypeIdentity(member.Type)))
                .Append("\", \"module\": \"").Append(JsonEscape(member.Type.ModuleName ?? string.Empty))
                .Append("\", \"provider\": \"").Append(JsonEscape(LogicalProviderIdentity(member.Type,
                    dependencyContext)))
                .Append("\", \"size\": ").Append(member.Type.Size.ToString(CultureInfo.InvariantCulture))
                .Append(", \"alignment\": ").Append(member.Type.Alignment.ToString(CultureInfo.InvariantCulture));
            if (member.Type is RecordTypeSymbol record)
                builder.Append(", \"fields\": [")
                    .Append(string.Join(", ", record.Fields.OrderBy(field => field.Ordinal)
                        .Select(field => FieldJson(field, dependencyContext, sourceIds))))
                    .Append("], \"members\": [")
                    .Append(string.Join(", ", record.Members
                        .Where(typeMember => typeMember.MemberKind != SmileTypeMemberKind.Field &&
                                             typeMember.Visibility == ModuleVisibility.Public)
                        .OrderBy(typeMember => typeMember.Name, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(typeMember => typeMember.Name, StringComparer.Ordinal)
                        .ThenBy(typeMember => typeMember.MemberKind)
                        .Select(typeMember => TypeMemberJson(typeMember, dependencyContext, sourceIds))))
                    .Append(']');
            else if (member.Type is ClassTypeSymbol classType)
                builder.Append(", \"fields\": [")
                    .Append(string.Join(", ", classType.Fields
                        .Where(field => field.Visibility == ModuleVisibility.Public)
                        .OrderBy(field => field.Ordinal)
                        .Select(field => ClassFieldJson(field, dependencyContext, sourceIds))))
                    .Append("], \"constructor\": ")
                    .Append(ConstructorJson(classType.Constructor, dependencyContext, sourceIds))
                    .Append(", \"members\": [")
                    .Append(string.Join(", ", classType.Members
                        .Where(typeMember => typeMember.MemberKind != SmileTypeMemberKind.Field &&
                                             typeMember.Visibility == ModuleVisibility.Public)
                        .OrderBy(typeMember => typeMember.Name, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(typeMember => typeMember.Name, StringComparer.Ordinal)
                        .ThenBy(typeMember => typeMember.MemberKind)
                        .Select(typeMember => TypeMemberJson(typeMember, dependencyContext, sourceIds))))
                    .Append(']');
            else if (member.Type is EnumTypeSymbol enumType)
                builder.Append(", \"members\": [")
                    .Append(string.Join(", ", enumType.Members.OrderBy(enumMember => enumMember.Ordinal)
                        .Select(enumMember => EnumMemberJson(enumMember, sourceIds))))
                    .Append(']');
            else
                throw new InvalidDataException(
                    $"Unsupported public nominal type metadata kind '{member.Type.Kind}'.");
        }
        return builder.Append(", \"location\": ").Append(LocationJson(member.Source,
            member.DeclarationSpan, sourceIds)).Append('}').ToString();
    }

    private static string BuildManifest(SmileProjectSourceSet project, IEnumerable<string> modules,
        IEnumerable<string> sources, IReadOnlyDictionary<string, string> hashes,
        IReadOnlyList<SmileLibraryDependency> dependencies)
    {
        var moduleJson = string.Join(", ", modules.OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item, StringComparer.Ordinal)
            .Select(item => "\"" + JsonEscape(item) + "\""));
        var sourceJson = string.Join(", ", sources.OrderBy(item => item, StringComparer.Ordinal)
            .Select(item => "\"" + JsonEscape(item) + "\""));
        var hashJson = string.Join(",\n", hashes.OrderBy(item => item.Key, StringComparer.Ordinal)
            .Select(item => "    \"" + JsonEscape(item.Key) + "\": \"" + JsonEscape(item.Value) + "\""));
        var dependencyJson = string.Join(",\n", dependencies.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Name, StringComparer.Ordinal).ThenBy(item => item.Version, StringComparer.Ordinal)
            .Select(item => "    {\"name\": \"" + JsonEscape(item.Name) + "\", \"version\": \"" +
                            JsonEscape(item.Version) + "\"}"));
        return "{\n  \"formatVersion\": 6,\n  \"name\": \"" + JsonEscape(project.LibraryName) +
               "\",\n  \"version\": \"" + JsonEscape(project.Version) +
               "\",\n  \"provider\": \"" + JsonEscape(project.LibraryName + "@" + project.Version) +
               "\",\n  \"modules\": [" + moduleJson +
               "],\n  \"sources\": [" + sourceJson + "],\n  \"sourceHashes\": {\n" + hashJson +
               "\n  },\n  \"dependencies\": [\n" + dependencyJson + "\n  ]\n}\n";
    }

    private static IReadOnlyList<SmileLibraryDependency> GetDependencies(SmileProjectSourceSet project)
    {
        var result = new List<SmileLibraryDependency>();
        foreach (var reference in project.References)
        {
            if (!reference.Exists)
                throw new FileNotFoundException("Referenced SMILE library was not found.", reference.FullPath);
            if (reference.Kind == SmileProjectReferenceKind.Project)
            {
                var dependency = SmileProjectSourceSet.Load(reference.FullPath);
                if (!dependency.IsLibrary)
                    throw new InvalidDataException($"Referenced project is not a SMILE library: {reference.FullPath}");
                result.Add(new SmileLibraryDependency(dependency.LibraryName, dependency.Version));
            }
            else
            {
                var dependency = ReadIdentity(reference.FullPath);
                result.Add(new SmileLibraryDependency(dependency.Name, dependency.Version));
            }
        }
        return result;
    }

    private static SmileLibraryIdentity ParseIdentity(PackageManifest manifest)
    {
        if (manifest.FormatVersion != CurrentFormatVersion)
            throw new InvalidDataException(manifest.FormatVersion is >= 1 and <= 5
                ? $"SMILE library formatVersion {manifest.FormatVersion} is no longer supported; rebuild the library with the current SMILE compiler (expected formatVersion 6)."
                : $"Unsupported SMILE library formatVersion {manifest.FormatVersion}; expected 6. Rebuild the library with the current SMILE compiler.");
        var name = RequiredValue(manifest.Name, "name");
        var version = RequiredValue(manifest.Version, "version");
        ValidateExactVersion(version, $"library '{name}'");
        var provider = RequiredValue(manifest.Provider, "provider");
        if (!string.Equals(provider, name + "@" + version, StringComparison.Ordinal))
            throw new InvalidDataException(
                $"SMILE library manifest provider must be the canonical identity '{name}@{version}'.");
        var modules = RequiredValues(manifest.Modules, "modules");
        var dependencies = (manifest.Dependencies ?? Array.Empty<PackageDependency>())
            .Select(dependency => new SmileLibraryDependency(RequiredValue(dependency.Name, "dependency name"),
                RequiredValue(dependency.Version, "dependency version"))).ToArray();
        foreach (var dependency in dependencies)
            ValidateExactVersion(dependency.Version, $"dependency '{dependency.Name}'");
        var duplicate = dependencies.GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate != null)
            throw new SmileProjectDiagnosticException("SML3203",
                $"Library '{name}' {version} declares dependency '{duplicate.Key}' more than once.", name);
        var self = dependencies.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        if (self != null)
            throw new SmileProjectDiagnosticException("SML3204",
                $"Library '{name}' {version} cannot depend on itself ({self.Name} {self.Version}).", name);
        return new SmileLibraryIdentity(name, version, modules, dependencies);
    }

    private static void ValidateExactVersion(string version, string description)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d+\.\d+\.\d+$"))
            throw new InvalidDataException($"SMILE library {description} version must use exact major.minor.patch: '{version}'.");
    }

    private static string RequiredValue(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidDataException($"SMILE library manifest requires non-empty '{name}'.");
        return value!;
    }

    private static IReadOnlyList<string> RequiredValues(string[]? values, string name)
    {
        if (values == null)
            throw new InvalidDataException($"SMILE library manifest requires array '{name}'.");
        var result = values.ToArray();
        if (result.Any(string.IsNullOrWhiteSpace) || result.Distinct(StringComparer.OrdinalIgnoreCase).Count() != result.Length)
            throw new InvalidDataException($"SMILE library manifest '{name}' contains empty or duplicate values.");
        return result;
    }

    private static void ValidateSourceHashes(IReadOnlyList<string> sources,
        IReadOnlyDictionary<string, string> sourceHashes)
    {
        var declaredSources = new HashSet<string>(sources, StringComparer.Ordinal);
        if (sourceHashes.Count != declaredSources.Count || sourceHashes.Keys.Any(key => !declaredSources.Contains(key)))
            throw new InvalidDataException(
                "SMILE library manifest sourceHashes must match the declared sources exactly.");
    }

    private static void ValidateGeneratedEntries(IReadOnlyList<PackageEntry> entries, string packagePath,
        SmileLibraryResourcePolicy policy)
    {
        if (entries.Count > policy.MaximumEntries)
            ThrowResourceLimit(packagePath,
                $"SMILE library package contains {entries.Count} entries; the maximum is {policy.MaximumEntries}.");
        var sourceCount = entries.Count(entry => entry.Name.StartsWith("src/", StringComparison.Ordinal));
        ValidateSourceCount(sourceCount, packagePath, policy);
        long expanded = 0;
        foreach (var entry in entries)
        {
            if (entry.Name.Length > policy.MaximumEntryNameCharacters)
                ThrowResourceLimit(packagePath,
                    $"SMILE library entry name exceeds the maximum {policy.MaximumEntryNameCharacters} characters: {entry.Name}");
            var maximum = EntryMaximum(entry.Name, policy);
            if (entry.Bytes.LongLength > maximum)
                ThrowResourceLimit(packagePath,
                    $"SMILE library {EntryDescription(entry.Name)} exceeds the maximum supported size of {maximum} bytes.");
            try { expanded = checked(expanded + entry.Bytes.LongLength); }
            catch (OverflowException) { ThrowResourceLimit(packagePath, "SMILE library expanded size overflowed."); }
        }
        if (expanded > policy.MaximumExpandedBytes)
            ThrowResourceLimit(packagePath,
                $"SMILE library expanded content exceeds the maximum supported size of {policy.MaximumExpandedBytes} bytes.");
    }

    private static void ValidatePhysicalPackage(string packagePath, SmileLibraryResourcePolicy policy)
    {
        var length = new FileInfo(packagePath).Length;
        if (length > policy.MaximumPackageBytes)
            ThrowResourceLimit(packagePath,
                $"SMILE library package exceeds the maximum supported physical size of {policy.MaximumPackageBytes} bytes.");
    }

    private static void ValidateEntries(ZipArchive archive, string packagePath, SmileLibraryResourcePolicy policy)
    {
        if (archive.Entries.Count > policy.MaximumEntries)
            ThrowResourceLimit(packagePath,
                $"SMILE library package contains {archive.Entries.Count} entries; the maximum is {policy.MaximumEntries}.");
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sourceCount = 0;
        long declaredExpanded = 0;
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.Length > policy.MaximumEntryNameCharacters)
                ThrowResourceLimit(packagePath,
                    $"SMILE library entry name exceeds the maximum {policy.MaximumEntryNameCharacters} characters: {entry.FullName}");
            ValidateEntryName(entry.FullName);
            if (!names.Add(entry.FullName))
                throw new InvalidDataException($"Duplicate SMILE library archive entry: {entry.FullName}");
            if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
                throw new InvalidDataException($"Directory entries are not allowed in SMILE libraries: {entry.FullName}");
            if (entry.FullName.StartsWith("src/", StringComparison.Ordinal))
                sourceCount++;
            var maximum = EntryMaximum(entry.FullName, policy);
            if (entry.Length > maximum)
                ThrowResourceLimit(packagePath,
                    $"SMILE library {EntryDescription(entry.FullName)} exceeds the maximum supported size of {maximum} bytes.");
            try { declaredExpanded = checked(declaredExpanded + entry.Length); }
            catch (OverflowException) { ThrowResourceLimit(packagePath, "SMILE library expanded size overflowed."); }
        }
        ValidateSourceCount(sourceCount, packagePath, policy);
        if (declaredExpanded > policy.MaximumExpandedBytes)
            ThrowResourceLimit(packagePath,
                $"SMILE library expanded content exceeds the maximum supported size of {policy.MaximumExpandedBytes} bytes.");
    }

    private static void ValidateEntryName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Contains('\\') || name.StartsWith("/", StringComparison.Ordinal) ||
            name.Contains(":") || name.Split('/').Any(part => part is ".." or "." or ""))
            throw new InvalidDataException($"Unsafe SMILE library archive path: {name}");
    }

    private static byte[] ReadEntryBytes(ZipArchiveEntry entry, int maximumBytes, string description,
        string packagePath, ExpandedReadBudget budget)
    {
        if (entry.Length > maximumBytes)
            ThrowResourceLimit(packagePath,
                $"SMILE library {description} exceeds the maximum supported size of {maximumBytes} bytes.");
        using var stream = entry.Open();
        using var output = new MemoryStream((int)Math.Min(entry.Length, maximumBytes));
        var buffer = new byte[81920];
        while (true)
        {
            var count = stream.Read(buffer, 0, buffer.Length);
            if (count == 0)
                break;
            if (output.Length > maximumBytes - count)
                ThrowResourceLimit(packagePath,
                    $"SMILE library {description} exceeds the maximum supported size of {maximumBytes} bytes.");
            budget.Add(count);
            output.Write(buffer, 0, count);
        }
        return output.ToArray();
    }

    private static int EntryMaximum(string entryName, SmileLibraryResourcePolicy policy) => entryName switch
    {
        "manifest.json" => policy.MaximumManifestBytes,
        "api/public-symbols.json" => policy.MaximumPublicApiBytes,
        _ when entryName.StartsWith("src/", StringComparison.Ordinal) => policy.MaximumSourceBytes,
        _ => (int)Math.Min(int.MaxValue, policy.MaximumExpandedBytes)
    };

    private static string EntryDescription(string entryName) => entryName switch
    {
        "manifest.json" => "manifest.json",
        "api/public-symbols.json" => "api/public-symbols.json",
        _ when entryName.StartsWith("src/", StringComparison.Ordinal) => $"source entry '{entryName}'",
        _ => $"entry '{entryName}'"
    };

    private static void ValidateSourceCount(int sourceCount, string packagePath,
        SmileLibraryResourcePolicy policy)
    {
        if (sourceCount > policy.MaximumSourceCount)
            ThrowResourceLimit(packagePath,
                $"SMILE library package contains {sourceCount} source entries; the maximum is {policy.MaximumSourceCount}.");
    }

    private static void ThrowResourceLimit(string packagePath, string message) =>
        throw new SmileProjectDiagnosticException(ResourceLimitDiagnosticCode, message, packagePath);

    private static string Normalize(string path) => path.Replace('\\', '/');
    private static string NormalizeText(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n');
    private static string Safe(string value) => string.Concat(value.Select(character =>
        char.IsLetterOrDigit(character) || character is '.' or '-' or '_' ? character : '_'));
    private static string Hash(byte[] bytes)
    {
        using var hash = SHA256.Create();
        return string.Concat(hash.ComputeHash(bytes).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
    }

    private static string Hash(Stream stream)
    {
        using var hash = SHA256.Create();
        return string.Concat(hash.ComputeHash(stream).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
    }

    private static PackageManifest ReadManifest(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            var serializer = new DataContractJsonSerializer(typeof(PackageManifest),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            return serializer.ReadObject(stream) as PackageManifest
                   ?? throw new InvalidDataException("SMILE library manifest is empty.");
        }
        catch (Exception exception) when (exception is SerializationException or XmlException)
        {
            throw new InvalidDataException("SMILE library manifest is malformed JSON.", exception);
        }
    }

    private static void ReplaceFile(string temporaryPath, string outputPath)
    {
        if (File.Exists(outputPath))
            File.Replace(temporaryPath, outputPath, null);
        else
            File.Move(temporaryPath, outputPath);
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private sealed class ExpandedReadBudget
    {
        private readonly string _packagePath;
        private readonly long _maximum;
        private long _consumed;

        public ExpandedReadBudget(string packagePath, long maximum)
        {
            _packagePath = packagePath;
            _maximum = maximum;
        }

        public void Add(int count)
        {
            try { _consumed = checked(_consumed + count); }
            catch (OverflowException) { ThrowResourceLimit(_packagePath, "SMILE library expanded size overflowed."); }
            if (_consumed > _maximum)
                ThrowResourceLimit(_packagePath,
                    $"SMILE library expanded content exceeds the maximum supported size of {_maximum} bytes.");
        }
    }

    private sealed class PackageOutputLock : IDisposable
    {
        private readonly Mutex _mutex;
        private bool _ownsMutex;

        private PackageOutputLock(string outputPath, TimeSpan timeout)
        {
            var normalizedPath = Path.GetFullPath(outputPath).ToUpperInvariant();
            var hash = Hash(Encoding.UTF8.GetBytes(normalizedPath)).ToUpperInvariant();
            _mutex = new Mutex(false, "Smile.Library.Output." + hash);
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
                throw new SmileProjectDiagnosticException(OutputLockDiagnosticCode,
                    $"Another build still owns the SMILE library output '{outputPath}'.", outputPath);
            }
        }

        public static PackageOutputLock Acquire(string outputPath, TimeSpan timeout) => new(outputPath, timeout);

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

    private static string JsonEscape(string value)
    {
        var builder = new StringBuilder(value.Length + 8);
        foreach (var character in value)
        {
            switch (character)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\b': builder.Append("\\b"); break;
                case '\f': builder.Append("\\f"); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;
                default:
                    if (character < ' ')
                        builder.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    else
                        builder.Append(character);
                    break;
            }
        }
        return builder.ToString();
    }

    private static string JsonValue(object value) => value switch
    {
        string text => "\"" + JsonEscape(text) + "\"",
        bool boolean => boolean ? "true" : "false",
        long number => number.ToString(CultureInfo.InvariantCulture),
        _ => throw new InvalidDataException($"Unsupported SMILE constant value type '{value.GetType().Name}'.")
    };

    private static string TypeIdentity(SmileType type) => type.Source == null ? type.Name : type.RuntimeIdentity;

    private static string TypeReferenceJson(SmileType type,
        SmileCompilationDependencyContext dependencyContext)
    {
        if (type.Source == null)
            return "{\"kind\": \"primitive\", \"name\": \"" + JsonEscape(type.Name) + "\"}";
        var kind = type.Kind switch
        {
            SmileTypeKind.Record => "type",
            SmileTypeKind.Enum => "enum",
            _ => type.Kind.ToString().ToLowerInvariant()
        };
        return "{\"kind\": \"" + JsonEscape(kind) + "\", \"name\": \"" + JsonEscape(type.Name) +
               "\", \"identity\": \"" + JsonEscape(TypeIdentity(type)) + "\", \"module\": \"" +
               JsonEscape(type.ModuleName ?? string.Empty) + "\", \"provider\": \"" +
               JsonEscape(LogicalProviderIdentity(type, dependencyContext)) + "\"}";
    }

    private static string LogicalProviderIdentity(SmileType type,
        SmileCompilationDependencyContext dependencyContext)
    {
        if (type.Source == null)
            return string.Empty;
        if (!dependencyContext.TryGetProviderDescriptor(type.ProviderIdentity, out var descriptor) ||
            string.IsNullOrWhiteSpace(descriptor.LogicalIdentity))
            throw new InvalidDataException(
                $"Public nominal type '{type.RuntimeIdentity}' has no exact logical library provider descriptor.");
        return descriptor.LogicalIdentity;
    }

    private static string SourceId(SourceText source, IReadOnlyDictionary<string, string> sourceIds)
    {
        var path = SmileSourceDocument.NormalizePath(source.FilePath);
        if (!sourceIds.TryGetValue(path, out var sourceId))
            throw new InvalidDataException($"Public API source '{source.FilePath}' has no declared package source ID.");
        ValidateEntryName(sourceId);
        if (!sourceId.StartsWith("src/", StringComparison.Ordinal))
            throw new InvalidDataException($"Public API source ID is outside src/: {sourceId}");
        return sourceId;
    }

    private static string LocationJson(SourceText source, TextSpan span,
        IReadOnlyDictionary<string, string> sourceIds)
    {
        source.GetLineColumn(span.Start, out var line, out var column);
        return "{\"source\": \"" + JsonEscape(SourceId(source, sourceIds)) + "\", \"line\": " +
               line.ToString(CultureInfo.InvariantCulture) + ", \"column\": " +
               column.ToString(CultureInfo.InvariantCulture) + ", \"length\": " +
               span.Length.ToString(CultureInfo.InvariantCulture) + "}";
    }

    private static string ParameterJson(ParameterSymbol parameter, int ordinal,
        SmileCompilationDependencyContext dependencyContext, IReadOnlyDictionary<string, string> sourceIds)
    {
        var builder = new StringBuilder("{\"name\": \"").Append(JsonEscape(parameter.Name))
            .Append("\", \"type\": ").Append(TypeReferenceJson(parameter.Type, dependencyContext))
            .Append(", \"mode\": \"").Append(parameter.ParameterMode)
            .Append("\", \"optional\": ").Append(parameter.IsOptional ? "true" : "false")
            .Append(", \"default\": ").Append(ParameterDefaultJson(parameter))
            .Append(", \"ordinal\": ")
            .Append(ordinal.ToString(CultureInfo.InvariantCulture)).Append(", \"location\": ")
            .Append(LocationJson(parameter.Source, parameter.DeclarationSpan, sourceIds));
        return builder.Append('}').ToString();
    }

    private static string ParameterDefaultJson(ParameterSymbol parameter)
    {
        if (!parameter.IsOptional)
            return "null";
        if (parameter.ParameterMode != ParameterPassingMode.ByVal || !parameter.HasDefaultValue)
            throw new InvalidDataException(
                $"Optional public parameter '{parameter.Name}' has no valid bound ByVal default.");

        if (parameter.DefaultEnumMember != null)
        {
            if (!parameter.Type.IsEnum || parameter.DefaultValue is not long enumValue ||
                enumValue != parameter.DefaultEnumMember.Value ||
                !ReferenceEquals(parameter.DefaultEnumMember.ContainingType, parameter.Type))
                throw new InvalidDataException(
                    $"Optional public parameter '{parameter.Name}' has inconsistent Enum default metadata.");
            return "{\"kind\": \"enum\", \"member\": \"" +
                   JsonEscape(parameter.DefaultEnumMember.Name) + "\", \"value\": " +
                   enumValue.ToString(CultureInfo.InvariantCulture) + "}";
        }

        if (parameter.Type == SmileType.Number && parameter.DefaultValue is long number)
            return "{\"kind\": \"number\", \"value\": " +
                   number.ToString(CultureInfo.InvariantCulture) + "}";
        if (parameter.Type == SmileType.Boolean && parameter.DefaultValue is bool boolean)
            return "{\"kind\": \"boolean\", \"value\": " + (boolean ? "true" : "false") + "}";
        if (parameter.Type == SmileType.Text && parameter.DefaultValue is string text)
            return "{\"kind\": \"text\", \"value\": \"" + JsonEscape(text) + "\"}";

        throw new InvalidDataException(
            $"Optional public parameter '{parameter.Name}' has unsupported default type '{parameter.Type.Name}'.");
    }

    private static string TypeMemberJson(ITypeMemberSymbol member,
        SmileCompilationDependencyContext dependencyContext, IReadOnlyDictionary<string, string> sourceIds)
    {
        if (member.Visibility != ModuleVisibility.Public)
            throw new InvalidDataException($"Private instance member '{member.Name}' cannot enter public API metadata.");
        if (string.IsNullOrWhiteSpace(member.RuntimeIdentity))
            throw new InvalidDataException($"Public instance member '{member.Name}' has no stable runtime identity.");

        var builder = new StringBuilder("{\"name\": \"").Append(JsonEscape(member.Name))
            .Append("\", \"kind\": \"").Append(member.MemberKind)
            .Append("\", \"visibility\": \"").Append(member.Visibility)
            .Append("\", \"identity\": \"").Append(JsonEscape(member.RuntimeIdentity)).Append('"');
        switch (member)
        {
            case TypeRoutineSymbol method:
                var routine = method.Routine;
                builder.Append(", \"returnType\": ")
                    .Append(routine.IsFunction
                        ? TypeReferenceJson(routine.ReturnType, dependencyContext)
                        : "null")
                    .Append(", \"parameters\": [")
                    .Append(string.Join(", ", routine.Parameters.Select((parameter, ordinal) =>
                        ParameterJson(parameter, ordinal, dependencyContext, sourceIds))))
                    .Append("], \"requiresGameWindow\": ")
                    .Append(routine.RequiresGameWindow ? "true" : "false");
                break;
            case PropertySymbol property:
                builder.Append(", \"type\": ").Append(TypeReferenceJson(property.Type, dependencyContext))
                    .Append(", \"get\": ").Append(PropertyAccessorJson(property.Getter, sourceIds))
                    .Append(", \"set\": ").Append(PropertyAccessorJson(property.Setter, sourceIds));
                break;
            default:
                throw new InvalidDataException(
                    $"Unsupported public instance member metadata kind '{member.MemberKind}'.");
        }
        return builder.Append(", \"location\": ")
            .Append(LocationJson(member.Source, member.DeclarationSpan, sourceIds)).Append('}').ToString();
    }

    private static string PropertyAccessorJson(RoutineSymbol? accessor,
        IReadOnlyDictionary<string, string> sourceIds)
    {
        if (accessor == null)
            return "null";
        if (!accessor.IsPropertyAccessor || string.IsNullOrWhiteSpace(accessor.RuntimeIdentity))
            throw new InvalidDataException("Public property accessor has no stable accessor identity.");
        return "{\"identity\": \"" + JsonEscape(accessor.RuntimeIdentity) +
               "\", \"requiresGameWindow\": " + (accessor.RequiresGameWindow ? "true" : "false") +
               ", \"location\": " + LocationJson(accessor.Source, accessor.DeclarationSpan, sourceIds) + "}";
    }

    private static string FieldJson(RecordFieldSymbol field,
        SmileCompilationDependencyContext dependencyContext, IReadOnlyDictionary<string, string> sourceIds)
    {
        var builder = new StringBuilder("{\"name\": \"").Append(JsonEscape(field.Name))
            .Append("\", \"visibility\": \"Public\", ")
            .Append(field.IsArray ? "\"elementType\": " : "\"type\": ")
            .Append(TypeReferenceJson(field.Type, dependencyContext));
        if (field.IsArray)
            builder.Append(", \"rank\": ").Append(field.ArrayRank.ToString(CultureInfo.InvariantCulture))
                .Append(", \"dimensions\": [")
                .Append(string.Join(", ", field.Dimensions.Select(dimension =>
                    dimension.ToString(CultureInfo.InvariantCulture)))).Append(']');
        return builder.Append(", \"ordinal\": ").Append(field.Ordinal.ToString(CultureInfo.InvariantCulture))
            .Append(", \"offset\": ").Append(field.Offset.ToString(CultureInfo.InvariantCulture))
            .Append(", \"location\": ").Append(LocationJson(field.Source, field.DeclarationSpan, sourceIds))
            .Append('}').ToString();
    }

    private static string ClassFieldJson(ClassFieldSymbol field,
        SmileCompilationDependencyContext dependencyContext, IReadOnlyDictionary<string, string> sourceIds)
    {
        if (field.Visibility != ModuleVisibility.Public)
            throw new InvalidDataException($"Private Class field '{field.Name}' cannot enter public API metadata.");
        var builder = new StringBuilder("{\"name\": \"").Append(JsonEscape(field.Name))
            .Append("\", \"visibility\": \"Public\", ")
            .Append(field.IsArray ? "\"elementType\": " : "\"type\": ")
            .Append(TypeReferenceJson(field.Type, dependencyContext));
        if (field.IsArray)
            builder.Append(", \"rank\": ").Append(field.ArrayRank.ToString(CultureInfo.InvariantCulture))
                .Append(", \"dimensions\": [")
                .Append(string.Join(", ", field.Dimensions.Select(dimension =>
                    dimension.ToString(CultureInfo.InvariantCulture)))).Append(']');
        return builder.Append(", \"ordinal\": ").Append(field.Ordinal.ToString(CultureInfo.InvariantCulture))
            .Append(", \"location\": ").Append(LocationJson(field.Source, field.DeclarationSpan, sourceIds))
            .Append('}').ToString();
    }

    private static string ConstructorJson(RoutineSymbol constructor,
        SmileCompilationDependencyContext dependencyContext, IReadOnlyDictionary<string, string> sourceIds)
    {
        if (!constructor.IsConstructor || constructor.ContainingType is not ClassTypeSymbol ||
            constructor.Visibility != ModuleVisibility.Public || string.IsNullOrWhiteSpace(constructor.RuntimeIdentity))
            throw new InvalidDataException("Public Class constructor metadata is incomplete.");
        return new StringBuilder("{\"identity\": \"").Append(JsonEscape(constructor.RuntimeIdentity))
            .Append("\", \"visibility\": \"Public\", \"declared\": ")
            .Append(constructor.IsDeclared ? "true" : "false")
            .Append(", \"parameters\": [")
            .Append(string.Join(", ", constructor.Parameters.Select((parameter, ordinal) =>
                ParameterJson(parameter, ordinal, dependencyContext, sourceIds))))
            .Append("], \"requiresGameWindow\": ")
            .Append(constructor.RequiresGameWindow ? "true" : "false")
            .Append(", \"location\": ")
            .Append(LocationJson(constructor.Source, constructor.DeclarationSpan, sourceIds))
            .Append('}').ToString();
    }

    private static string EnumMemberJson(EnumMemberSymbol member,
        IReadOnlyDictionary<string, string> sourceIds) =>
        new StringBuilder("{\"name\": \"").Append(JsonEscape(member.Name))
            .Append("\", \"value\": ").Append(member.Value.ToString(CultureInfo.InvariantCulture))
            .Append(", \"ordinal\": ").Append(member.Ordinal.ToString(CultureInfo.InvariantCulture))
            .Append(", \"location\": ").Append(LocationJson(member.Source, member.DeclarationSpan, sourceIds))
            .Append('}').ToString();

    [DataContract]
    private sealed class PackageManifest
    {
        [DataMember(Name = "formatVersion", Order = 0)] public int FormatVersion { get; set; }
        [DataMember(Name = "name", Order = 1)] public string? Name { get; set; }
        [DataMember(Name = "version", Order = 2)] public string? Version { get; set; }
        [DataMember(Name = "provider", Order = 3)] public string? Provider { get; set; }
        [DataMember(Name = "modules", Order = 4)] public string[]? Modules { get; set; }
        [DataMember(Name = "sources", Order = 5)] public string[]? Sources { get; set; }
        [DataMember(Name = "sourceHashes", Order = 6)] public Dictionary<string, string>? SourceHashes { get; set; }
        [DataMember(Name = "dependencies", Order = 7)] public PackageDependency[]? Dependencies { get; set; }
    }

    [DataContract]
    private sealed class PackageDependency
    {
        [DataMember(Name = "name", Order = 0)] public string? Name { get; set; }
        [DataMember(Name = "version", Order = 1)] public string? Version { get; set; }
    }

    private sealed class PackageEntry
    {
        public PackageEntry(string name, byte[] bytes) { Name = name; Bytes = bytes; }
        public string Name { get; }
        public byte[] Bytes { get; }
    }
}

public sealed class SmileLibraryProvider
{
    private SmileLibraryProvider(SmileLibraryIdentity identity, SmileLibraryProviderKind kind,
        string providerPath, IReadOnlyList<SmileSourceDocument> sources, SmileLibraryLoadResult? package)
    {
        Identity = identity;
        Kind = kind;
        ProviderPath = Path.GetFullPath(providerPath);
        Sources = sources;
        Package = package;
    }

    public SmileLibraryIdentity Identity { get; }
    public SmileLibraryProviderKind Kind { get; }
    public string ProviderPath { get; }
    public IReadOnlyList<SmileLibraryDependency> DeclaredDependencies => Identity.Dependencies;
    public IReadOnlyList<SmileSourceDocument> Sources { get; }
    internal SmileLibraryLoadResult? Package { get; }

    internal static SmileLibraryProvider FromProject(SmileProjectSourceSet project,
        IReadOnlyList<SmileSourceDocument> sources, IReadOnlyList<SmileLibraryDependency> dependencies) =>
        new(new SmileLibraryIdentity(project.LibraryName, project.Version, Array.Empty<string>(), dependencies),
            SmileLibraryProviderKind.Project, project.ProjectPath, sources, null);

    internal static SmileLibraryProvider FromPackage(SmileLibraryLoadResult package) =>
        new(package.Identity, SmileLibraryProviderKind.Package, package.ProviderPath, package.Sources, package);
}

public sealed class SmileLibraryResolution
{
    internal SmileLibraryResolution(IReadOnlyList<SmileLibraryProvider> providers,
        IReadOnlyList<SmileSourceDocument> sources, SmileCompilationDependencyContext dependencyContext)
    {
        Providers = providers;
        Sources = sources;
        DependencyContext = dependencyContext;
    }

    public IReadOnlyList<SmileLibraryProvider> Providers { get; }
    public IReadOnlyList<SmileSourceDocument> Sources { get; }
    public SmileCompilationDependencyContext DependencyContext { get; }

    public SmileCompilationDependencyContext CreateLooseRootContext()
    {
        var context = DependencyContext.Copy();
        context.AddProvider("<local>", SmileProviderKind.Loose, string.Empty, string.Empty, "<loose>");
        foreach (var provider in Providers)
            context.AddDirectAccess("<local>", provider.ProviderPath);
        return context;
    }
}

public static class SmileLibraryProviderResolver
{
    public static SmileLibraryResolution LoadPackages(IEnumerable<string> packagePaths, string cacheRoot)
    {
        if (packagePaths == null) throw new ArgumentNullException(nameof(packagePaths));
        var paths = new List<string>();
        var normalizedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var packagePath in packagePaths)
        {
            var fullPath = Path.GetFullPath(packagePath);
            if (!normalizedPaths.Add(fullPath))
                throw new SmileProjectDiagnosticException("SML3201",
                    $"SMILE library package provider path was supplied more than once: {fullPath}", fullPath);
            paths.Add(fullPath);
        }

        var providers = paths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => SmileLibraryProvider.FromPackage(ReadPackage(path, cacheRoot))).ToArray();
        return Resolve(providers);
    }

    internal static SmileLibraryResolution Resolve(IReadOnlyList<SmileLibraryProvider> providers)
    {
        var byName = new Dictionary<string, SmileLibraryProvider>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in providers.OrderBy(item => item.Identity.Name, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(item => item.ProviderPath, StringComparer.OrdinalIgnoreCase))
        {
            if (byName.TryGetValue(provider.Identity.Name, out var existing) &&
                !string.Equals(existing.ProviderPath, provider.ProviderPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new SmileProjectDiagnosticException("SML3201",
                    $"Conflicting providers for library '{provider.Identity.Name}': " +
                    $"{existing.Kind} {existing.Identity.Version} at '{existing.ProviderPath}' and " +
                    $"{provider.Kind} {provider.Identity.Version} at '{provider.ProviderPath}'.",
                    provider.ProviderPath);
            }
            byName[provider.Identity.Name] = provider;
        }

        foreach (var provider in byName.Values.OrderBy(item => item.Identity.Name, StringComparer.OrdinalIgnoreCase))
        {
            var duplicate = provider.DeclaredDependencies.GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
                throw new SmileProjectDiagnosticException("SML3203",
                    $"Library '{provider.Identity.Name}' {provider.Identity.Version} at '{provider.ProviderPath}' " +
                    $"declares dependency '{duplicate.Key}' more than once.", provider.ProviderPath);

            foreach (var dependency in provider.DeclaredDependencies)
            {
                if (string.Equals(dependency.Name, provider.Identity.Name, StringComparison.OrdinalIgnoreCase))
                    throw new SmileProjectDiagnosticException("SML3204",
                        $"Library '{provider.Identity.Name}' {provider.Identity.Version} at '{provider.ProviderPath}' " +
                        $"cannot depend on itself ({dependency.Name} {dependency.Version}).", provider.ProviderPath);
                if (!byName.TryGetValue(dependency.Name, out var actual))
                    throw new SmileProjectDiagnosticException("SML3200",
                        $"Library '{provider.Identity.Name}' {provider.Identity.Version} at '{provider.ProviderPath}' " +
                        $"requires '{dependency.Name}' {dependency.Version}, but no explicit project or package provider was supplied.",
                        provider.ProviderPath);
                if (!string.Equals(actual.Identity.Version, dependency.Version, StringComparison.OrdinalIgnoreCase))
                    throw new SmileProjectDiagnosticException("SML3202",
                        $"Library '{provider.Identity.Name}' {provider.Identity.Version} at '{provider.ProviderPath}' " +
                        $"requires '{dependency.Name}' {dependency.Version}, but provider '{actual.ProviderPath}' is " +
                        $"{actual.Identity.Name} {actual.Identity.Version}.", provider.ProviderPath);
            }
        }

        var state = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var stack = new List<SmileLibraryProvider>();
        var order = new List<SmileLibraryProvider>();
        foreach (var provider in byName.Values.OrderBy(item => item.Identity.Name, StringComparer.OrdinalIgnoreCase))
            Visit(provider);

        var sources = order.SelectMany(provider => provider.Sources).ToArray();
        var sourceOwners = new Dictionary<string, SmileLibraryProvider>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in order)
        {
            foreach (var source in provider.Sources)
            {
                if (sourceOwners.TryGetValue(source.FilePath, out var existing) &&
                    !string.Equals(existing.ProviderPath, provider.ProviderPath, StringComparison.OrdinalIgnoreCase))
                    throw new SmileProjectDiagnosticException("SML3201",
                        $"Conflicting library source providers for '{source.FilePath}': " +
                        $"'{existing.ProviderPath}' and '{provider.ProviderPath}'.", provider.ProviderPath);
                sourceOwners[source.FilePath] = provider;
            }
        }
        if (sources.Length == 0 && order.Count != 0)
            throw new SmileProjectDiagnosticException("SML3207",
                $"Library provider '{order[0].ProviderPath}' contains no source documents.", order[0].ProviderPath);
        var dependencyContext = CreateDependencyContext(order, byName);
        if (sources.Length != 0)
        {
            var analysis = SmileLanguage.Analyze(sources, SmileCompilationKind.Library, dependencyContext);
            ValidatePackages(order, analysis);
        }
        return new SmileLibraryResolution(order, sources, dependencyContext);

        void Visit(SmileLibraryProvider provider)
        {
            if (state.TryGetValue(provider.Identity.Name, out var current))
            {
                if (current == 1)
                {
                    var start = stack.FindIndex(item => string.Equals(item.Identity.Name,
                        provider.Identity.Name, StringComparison.OrdinalIgnoreCase));
                    var cycle = stack.Skip(Math.Max(start, 0)).Concat(new[] { provider })
                        .Select(item => $"{item.Identity.Name} {item.Identity.Version} ('{item.ProviderPath}')");
                    throw new SmileProjectDiagnosticException("SML3205",
                        "SMILE library dependency cycle detected: " + string.Join(" -> ", cycle),
                        provider.ProviderPath);
                }
                return;
            }

            state[provider.Identity.Name] = 1;
            stack.Add(provider);
            foreach (var dependency in provider.DeclaredDependencies.OrderBy(item => item.Name,
                         StringComparer.OrdinalIgnoreCase))
                Visit(byName[dependency.Name]);
            stack.RemoveAt(stack.Count - 1);
            state[provider.Identity.Name] = 2;
            order.Add(provider);
        }
    }

    private static SmileCompilationDependencyContext CreateDependencyContext(
        IReadOnlyList<SmileLibraryProvider> providers,
        IReadOnlyDictionary<string, SmileLibraryProvider> providersByName)
    {
        var context = SmileCompilationDependencyContext.Create();
        foreach (var provider in providers)
            context.AddProvider(provider.ProviderPath,
                provider.Kind == SmileLibraryProviderKind.Project ? SmileProviderKind.Project : SmileProviderKind.Package,
                provider.Identity.Name, provider.Identity.Version, provider.ProviderPath);
        foreach (var provider in providers)
            foreach (var dependency in provider.DeclaredDependencies)
                context.AddDirectAccess(provider.ProviderPath, providersByName[dependency.Name].ProviderPath);
        return context;
    }

    internal static SmileLibraryLoadResult ReadPackage(string packagePath, string cacheRoot)
    {
        try
        {
            return SmileLibraryPackage.ReadEnvelope(packagePath, cacheRoot);
        }
        catch (Exception exception) when (SmileProjectDiagnostic.TryCreate(exception, packagePath, out _))
        {
            SmileProjectDiagnostic.TryCreate(exception, packagePath, out var diagnostic);
            throw new SmileProjectDiagnosticException(diagnostic.Code,
                $"SMILE library package '{packagePath}' could not be loaded: {diagnostic.Message}", packagePath);
        }
    }

    private static void ValidatePackages(IReadOnlyList<SmileLibraryProvider> providers, SmileAnalysisResult analysis)
    {
        foreach (var provider in providers.Where(item => item.Kind == SmileLibraryProviderKind.Package))
        {
            var package = provider.Package!;
            var ownedPaths = new HashSet<string>(provider.Sources.Select(source => source.FilePath),
                StringComparer.OrdinalIgnoreCase);
            var errors = analysis.Diagnostics.Where(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error && ownedPaths.Contains(diagnostic.FilePath)).ToArray();
            if (errors.Length != 0)
                throw new SmileProjectDiagnosticException("SML3207",
                    $"Packaged library '{provider.Identity.Name}' {provider.Identity.Version} at '{provider.ProviderPath}' " +
                    "does not pass authoritative dependency-aware analysis: " +
                    string.Join("; ", errors.Select(item => item.Code + " " + item.Message)), provider.ProviderPath);

            var modules = analysis.SemanticModel.Modules.Values.Where(module =>
                    string.Equals(module.ProviderIdentity, provider.ProviderPath, StringComparison.OrdinalIgnoreCase))
                .OrderBy(module => module.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(module => module.Name, StringComparer.Ordinal).ToArray();
            if (!provider.Identity.Modules.SequenceEqual(modules.Select(module => module.Name),
                    StringComparer.OrdinalIgnoreCase))
                throw new SmileProjectDiagnosticException("SML3207",
                    $"Packaged library '{provider.Identity.Name}' {provider.Identity.Version} at '{provider.ProviderPath}' " +
                    "has a manifest module list that does not match its owned source modules.", provider.ProviderPath);
            var expectedApi = SmileLibraryPackage.BuildPublicApi(modules, analysis.DependencyContext,
                provider.Identity.Name, provider.Identity.Version, package.SourceIds);
            if (!string.Equals(package.PublicApiMetadata, expectedApi, StringComparison.Ordinal))
                throw new SmileProjectDiagnosticException("SML3207",
                    $"Packaged library '{provider.Identity.Name}' {provider.Identity.Version} at '{provider.ProviderPath}' " +
                    "has public API metadata that does not match authoritative analysis.", provider.ProviderPath);
        }
    }
}

public sealed class SmileProjectBuildGraph
{
    private SmileProjectBuildGraph(SmileProjectSourceSet root, IReadOnlyList<SmileProjectSourceSet> buildOrder)
    {
        Root = root;
        BuildOrder = buildOrder;
    }

    public SmileProjectSourceSet Root { get; }
    public IReadOnlyList<SmileProjectSourceSet> BuildOrder { get; }
    public IReadOnlyList<string> PhysicalCompilationSourcePaths => BuildOrder
        .SelectMany(project => project.CompilationSources)
        .Select(source => source.FullPath)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToArray();
    public IReadOnlyList<string> ParticipatingPaths => BuildOrder
        .SelectMany(project => new[] { project.ProjectPath }
            .Concat(project.CompilationSources.Select(source => source.FullPath))
            .Concat(project.References.Where(reference => reference.Kind == SmileProjectReferenceKind.Package)
                .Select(reference => reference.FullPath)))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public static SmileProjectBuildGraph Load(string projectPath)
    {
        var projects = new Dictionary<string, SmileProjectSourceSet>(StringComparer.OrdinalIgnoreCase);
        var state = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var stack = new List<string>();
        var order = new List<SmileProjectSourceSet>();
        var root = Visit(Path.GetFullPath(projectPath), isRoot: true);
        return new SmileProjectBuildGraph(root, order);

        SmileProjectSourceSet Visit(string path, bool isRoot)
        {
            if (state.TryGetValue(path, out var currentState))
            {
                if (currentState == 1)
                {
                    var start = stack.FindIndex(item => string.Equals(item, path, StringComparison.OrdinalIgnoreCase));
                    var cycle = stack.Skip(Math.Max(0, start)).Concat(new[] { path })
                        .Select(item => $"{Path.GetFileName(item)} ('{item}')").ToArray();
                    throw new SmileProjectDiagnosticException("SML3205",
                        "SMILE project-reference cycle detected: " + string.Join(" -> ", cycle), path);
                }
                return projects[path];
            }
            if (!File.Exists(path))
                throw new SmileProjectDiagnosticException("SML3200",
                    $"Referenced SMILE project was not found: {path}", path);
            SmileProjectSourceSet project;
            try
            {
                project = SmileProjectSourceSet.Load(path);
            }
            catch (Exception exception) when (SmileProjectDiagnostic.TryCreate(exception, path, out _))
            {
                SmileProjectDiagnostic.TryCreate(exception, path, out var diagnostic);
                throw new SmileProjectDiagnosticException(diagnostic.Code,
                    $"SMILE project '{path}' could not be loaded: {diagnostic.Message}", path);
            }
            projects[path] = project;
            state[path] = 1;
            stack.Add(path);
            foreach (var reference in project.References.Where(item => item.Kind == SmileProjectReferenceKind.Project)
                         .OrderBy(item => item.FullPath, StringComparer.OrdinalIgnoreCase))
            {
                if (!File.Exists(reference.FullPath))
                    throw new SmileProjectDiagnosticException("SML3200",
                        $"SMILE project '{project.ProjectPath}' references missing library project " +
                        $"'{reference.FullPath}'.", reference.FullPath);
                var dependency = Visit(reference.FullPath, isRoot: false);
                if (!dependency.IsLibrary)
                    throw new SmileProjectDiagnosticException("SML3206",
                        $"SMILE project reference must target a library project: {reference.FullPath}", reference.FullPath);
            }
            stack.RemoveAt(stack.Count - 1);
            state[path] = 2;
            order.Add(project);
            return project;
        }
    }
}

public sealed class SmileProjectParticipationDiscoveryResult
{
    internal SmileProjectParticipationDiscoveryResult(IReadOnlyList<string> paths,
        SmileProjectDiagnostic? diagnostic)
    {
        Paths = paths;
        Diagnostic = diagnostic;
    }

    public IReadOnlyList<string> Paths { get; }
    public SmileProjectDiagnostic? Diagnostic { get; }
}

public static class SmileProjectParticipationDiscovery
{
    public static SmileProjectParticipationDiscoveryResult Discover(string projectPath)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visitedProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        SmileProjectDiagnostic? firstDiagnostic = null;
        VisitProject(Path.GetFullPath(projectPath), referencingProject: null);
        return new SmileProjectParticipationDiscoveryResult(
            paths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray(), firstDiagnostic);

        void Record(SmileProjectDiagnostic diagnostic)
        {
            firstDiagnostic ??= diagnostic;
        }

        void VisitProject(string path, string? referencingProject)
        {
            paths.Add(path);
            if (!visitedProjects.Add(path))
                return;
            if (!File.Exists(path))
            {
                Record(new SmileProjectDiagnostic("SML3200", referencingProject == null
                    ? $"SMILE project was not found: {path}"
                    : $"SMILE project '{referencingProject}' references missing library project '{path}'.", path));
                return;
            }

            SmileProjectSourceSet project;
            try
            {
                project = SmileProjectSourceSet.Load(path);
            }
            catch (Exception exception) when (SmileProjectDiagnostic.TryCreate(exception, path, out _))
            {
                SmileProjectDiagnostic.TryCreate(exception, path, out var diagnostic);
                Record(diagnostic);
                return;
            }

            foreach (var item in project.Items)
                paths.Add(item.FullPath);
            foreach (var reference in project.References)
            {
                paths.Add(reference.FullPath);
                if (reference.Kind == SmileProjectReferenceKind.Project)
                {
                    VisitProject(reference.FullPath, project.ProjectPath);
                    continue;
                }
                if (!File.Exists(reference.FullPath))
                    Record(new SmileProjectDiagnostic("SML3200",
                        $"SMILE project '{project.ProjectPath}' references missing library package " +
                        $"'{reference.FullPath}'.", reference.FullPath));
            }
        }
    }
}

public sealed class SmileProjectCompilation
{
    private SmileProjectCompilation(SmileProjectBuildGraph graph, IReadOnlyList<SmileSourceDocument> sources,
        IReadOnlyList<SmileLibraryLoadResult> packages, IReadOnlyList<SmileLibraryProvider> providers,
        SmileCompilationDependencyContext dependencyContext)
    {
        Graph = graph;
        Sources = sources;
        Packages = packages;
        Providers = providers;
        DependencyContext = dependencyContext;
    }

    public SmileProjectBuildGraph Graph { get; }
    public IReadOnlyList<SmileSourceDocument> Sources { get; }
    public IReadOnlyList<SmileLibraryLoadResult> Packages { get; }
    public IReadOnlyList<SmileLibraryProvider> Providers { get; }
    public SmileCompilationDependencyContext DependencyContext { get; }
    public SmileCompilationKind CompilationKind => Graph.Root.IsLibrary ? SmileCompilationKind.Library : SmileCompilationKind.Program;

    public static SmileProjectCompilation Load(string projectPath, string? cacheRoot = null,
        Func<string, string?>? openText = null)
    {
        var graph = SmileProjectBuildGraph.Load(projectPath);
        var projectSources = new Dictionary<string, IReadOnlyList<SmileSourceDocument>>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in graph.BuildOrder)
        {
            var documents = new List<SmileSourceDocument>();
            foreach (var source in project.CompilationSources)
            {
                var text = openText?.Invoke(source.FullPath);
                var missing = false;
                if (text == null)
                {
                    try { text = File.ReadAllText(source.FullPath); }
                    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                    { text = string.Empty; missing = true; }
                }
                documents.Add(new SmileSourceDocument(text, source.FullPath,
                    isStartup: string.Equals(project.ProjectPath, graph.Root.ProjectPath, StringComparison.OrdinalIgnoreCase) && source.IsStartup,
                    isMissing: missing, providerIdentity: project.ProjectPath));
            }
            projectSources[project.ProjectPath] = documents;
        }

        cacheRoot ??= Path.Combine(graph.Root.ProjectDirectory, "obj", "Smile", "Libraries");
        var packagesByPath = new Dictionary<string, SmileLibraryLoadResult>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in graph.BuildOrder)
        {
            foreach (var reference in project.References.Where(item => item.Kind == SmileProjectReferenceKind.Package)
                         .OrderBy(item => item.FullPath, StringComparer.OrdinalIgnoreCase))
            {
                if (packagesByPath.ContainsKey(reference.FullPath))
                    continue;
                packagesByPath[reference.FullPath] = SmileLibraryProviderResolver.ReadPackage(reference.FullPath, cacheRoot);
            }
        }

        var projectsByPath = graph.BuildOrder.ToDictionary(item => item.ProjectPath,
            StringComparer.OrdinalIgnoreCase);
        var providers = new List<SmileLibraryProvider>();
        foreach (var project in graph.BuildOrder.Where(item => item.IsLibrary))
        {
            var dependencies = project.References.Select(reference =>
            {
                if (reference.Kind == SmileProjectReferenceKind.Project)
                {
                    var dependency = projectsByPath[reference.FullPath];
                    return new SmileLibraryDependency(dependency.LibraryName, dependency.Version);
                }
                var package = packagesByPath[reference.FullPath];
                return new SmileLibraryDependency(package.Identity.Name, package.Identity.Version);
            }).ToArray();
            providers.Add(SmileLibraryProvider.FromProject(project, projectSources[project.ProjectPath], dependencies));
        }
        providers.AddRange(packagesByPath.Values.Select(SmileLibraryProvider.FromPackage));
        var resolution = SmileLibraryProviderResolver.Resolve(providers);

        var dependencyContext = resolution.DependencyContext;
        if (!graph.Root.IsLibrary)
        {
            dependencyContext = dependencyContext.Copy();
            dependencyContext.AddProvider(graph.Root.ProjectPath, SmileProviderKind.Project,
                string.Empty, string.Empty, graph.Root.ProjectPath);
            foreach (var reference in graph.Root.References)
            {
                var providerPath = reference.Kind == SmileProjectReferenceKind.Project
                    ? reference.FullPath
                    : packagesByPath[reference.FullPath].ProviderPath;
                dependencyContext.AddDirectAccess(graph.Root.ProjectPath, providerPath);
            }
        }

        IReadOnlyList<SmileSourceDocument> sources = graph.Root.IsLibrary
            ? resolution.Sources
            : projectSources[graph.Root.ProjectPath].Concat(resolution.Sources).ToArray();
        var packages = resolution.Providers.Where(provider => provider.Kind == SmileLibraryProviderKind.Package)
            .Select(provider => provider.Package!).ToArray();
        return new SmileProjectCompilation(graph, sources, packages, resolution.Providers, dependencyContext);
    }

    public static SmileProjectCompilationLoadResult TryLoad(string projectPath, string? cacheRoot = null,
        Func<string, string?>? openText = null)
    {
        try
        {
            return SmileProjectCompilationLoadResult.Success(Load(projectPath, cacheRoot, openText));
        }
        catch (Exception exception) when (SmileProjectDiagnostic.TryCreate(exception, projectPath, out _))
        {
            SmileProjectDiagnostic.TryCreate(exception, projectPath, out var diagnostic);
            return SmileProjectCompilationLoadResult.Failure(diagnostic);
        }
    }
}

public sealed class SmileProjectCompilationLoadResult
{
    private SmileProjectCompilationLoadResult(SmileProjectCompilation? compilation, SmileProjectDiagnostic? diagnostic)
    {
        Compilation = compilation;
        Diagnostic = diagnostic;
    }

    public SmileProjectCompilation? Compilation { get; }
    public SmileProjectDiagnostic? Diagnostic { get; }
    public bool Succeeded => Compilation != null;

    internal static SmileProjectCompilationLoadResult Success(SmileProjectCompilation compilation) => new(compilation, null);
    internal static SmileProjectCompilationLoadResult Failure(SmileProjectDiagnostic diagnostic) => new(null, diagnostic);
}
