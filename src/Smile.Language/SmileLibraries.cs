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
    public SmileProjectDiagnostic(string code, string message, string filePath)
    {
        Code = code;
        Message = message;
        FilePath = SmileSourceDocument.NormalizePath(filePath);
    }

    public string Code { get; }
    public string Message { get; }
    public string FilePath { get; }
    public int Line => 1;
    public int Column => 1;
    public string FormatCompiler() =>
        $"{(string.IsNullOrWhiteSpace(FilePath) ? "<project>" : FilePath)}({Line},{Column}): error {Code}: {Message}";

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
    public SmileProjectDiagnosticException(string code, string message, string filePath)
        : base(message)
    {
        Diagnostic = new SmileProjectDiagnostic(code, message, filePath);
    }

    public SmileProjectDiagnostic Diagnostic { get; }
    public string Code => Diagnostic.Code;
    public string FilePath => Diagnostic.FilePath;
}

public sealed class SmileLibraryLoadResult
{
    internal SmileLibraryLoadResult(SmileLibraryIdentity identity, IReadOnlyList<SmileSourceDocument> sources,
        string packageHash, string extractionDirectory, string providerPath, string publicApiMetadata)
    {
        Identity = identity;
        Sources = sources;
        PackageHash = packageHash;
        ExtractionDirectory = extractionDirectory;
        ProviderPath = providerPath;
        PublicApiMetadata = publicApiMetadata;
    }

    public SmileLibraryIdentity Identity { get; }
    public IReadOnlyList<SmileSourceDocument> Sources { get; }
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
            !Modules.OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .SequenceEqual(other.Modules.OrderBy(item => item, StringComparer.OrdinalIgnoreCase),
                    StringComparer.OrdinalIgnoreCase) ||
            SourceHashes.Count != other.SourceHashes.Count)
            return false;
        foreach (var source in SourceHashes)
            if (!other.SourceHashes.TryGetValue(source.Key, out var hash) ||
                !string.Equals(source.Value, hash, StringComparison.OrdinalIgnoreCase))
                return false;
        var dependencies = Dependencies.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.Name + "\0" + item.Version);
        var otherDependencies = other.Dependencies.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.Name + "\0" + item.Version);
        return dependencies.SequenceEqual(otherDependencies, StringComparer.OrdinalIgnoreCase);
    }
}

public static class SmileLibraryPackage
{
    public const int CurrentFormatVersion = 4;
    private static readonly DateTimeOffset DeterministicTimestamp =
        new(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static void Write(string outputPath, SmileProjectSourceSet project, SmileAnalysisResult analysis)
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
            .OrderBy(module => module.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        if (modules.Length == 0)
            throw new InvalidDataException("The library project does not declare a module.");

        var sourceEntries = project.Items.OrderBy(item => Normalize(item.Include), StringComparer.Ordinal).Select(item =>
        {
            var bytes = Encoding.UTF8.GetBytes(NormalizeText(File.ReadAllText(item.FullPath)));
            return new PackageEntry("src/" + Normalize(item.Include), bytes);
        }).ToArray();
        var hashes = sourceEntries.ToDictionary(entry => entry.Name,
            entry => Hash(entry.Bytes), StringComparer.Ordinal);
        var dependencies = GetDependencies(project);
        var manifest = BuildManifest(project, modules.Select(module => module.Name), sourceEntries.Select(entry => entry.Name), hashes, dependencies);
        var api = BuildPublicApi(modules, analysis.DependencyContext);

        var entries = new List<PackageEntry>
        {
            new("manifest.json", Encoding.UTF8.GetBytes(manifest)),
            new("api/public-symbols.json", Encoding.UTF8.GetBytes(api))
        };
        entries.AddRange(sourceEntries);

        var fullOutputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
        using var bytesOutput = new MemoryStream();
        using (var archive = new ZipArchive(bytesOutput, ZipArchiveMode.Create, leaveOpen: true, Encoding.UTF8))
        {
            foreach (var item in entries.OrderBy(entry => entry.Name, StringComparer.Ordinal))
            {
                var entry = archive.CreateEntry(item.Name, CompressionLevel.NoCompression);
                entry.LastWriteTime = DeterministicTimestamp;
                using var stream = entry.Open();
                stream.Write(item.Bytes, 0, item.Bytes.Length);
            }
        }
        File.WriteAllBytes(fullOutputPath, bytesOutput.ToArray());
    }

    public static SmileLibraryIdentity ReadIdentity(string packagePath)
    {
        using var archive = ZipFile.OpenRead(Path.GetFullPath(packagePath));
        ValidateEntries(archive);
        var manifest = archive.GetEntry("manifest.json")
            ?? throw new InvalidDataException("SMILE library package is missing manifest.json.");
        using var stream = manifest.Open();
        return ParseIdentity(ReadManifest(stream));
    }

    public static SmileLibraryBuildFingerprint ReadBuildFingerprint(string packagePath)
    {
        using var archive = ZipFile.OpenRead(Path.GetFullPath(packagePath));
        ValidateEntries(archive);
        var manifestEntry = archive.GetEntry("manifest.json")
            ?? throw new InvalidDataException("SMILE library package is missing manifest.json.");
        PackageManifest manifest;
        using (var stream = manifestEntry.Open())
            manifest = ReadManifest(stream);
        var identity = ParseIdentity(manifest);
        var sources = RequiredValues(manifest.Sources, "sources");
        var declaredHashes = manifest.SourceHashes
            ?? throw new InvalidDataException("SMILE library manifest is missing sourceHashes.");
        var verifiedHashes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var source in sources)
        {
            ValidateEntryName(source);
            if (!source.StartsWith("src/", StringComparison.Ordinal))
                throw new InvalidDataException($"SMILE library source entry is outside src/: {source}");
            var entry = archive.GetEntry(source)
                ?? throw new InvalidDataException($"SMILE library declared source is missing: {source}");
            var actualHash = Hash(ReadAllBytes(entry));
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
        var publicApiHash = Hash(ReadAllBytes(apiEntry));
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
            .OrderBy(module => module.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        var hashes = project.Items.ToDictionary(item => "src/" + Normalize(item.Include),
            item => Hash(Encoding.UTF8.GetBytes(NormalizeText(File.ReadAllText(item.FullPath)))),
            StringComparer.Ordinal);
        return new SmileLibraryBuildFingerprint(CurrentFormatVersion, project.LibraryName, project.Version,
            modules.Select(module => module.Name).ToArray(), hashes, GetDependencies(project),
            Hash(Encoding.UTF8.GetBytes(BuildPublicApi(modules, analysis.DependencyContext))));
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
    {
        var fullPackagePath = Path.GetFullPath(packagePath);
        if (!File.Exists(fullPackagePath))
            throw new FileNotFoundException("Referenced SMILE library package was not found.", fullPackagePath);
        var packageBytes = File.ReadAllBytes(fullPackagePath);
        var packageHash = Hash(packageBytes);
        using var archive = new ZipArchive(new MemoryStream(packageBytes, writable: false), ZipArchiveMode.Read,
            leaveOpen: false, Encoding.UTF8);
        ValidateEntries(archive);

        var manifestEntry = archive.GetEntry("manifest.json")
            ?? throw new InvalidDataException("SMILE library package is missing manifest.json.");
        PackageManifest manifest;
        using (var stream = manifestEntry.Open())
            manifest = ReadManifest(stream);
        {
            var identity = ParseIdentity(manifest);
            var extractionDirectory = Path.Combine(Path.GetFullPath(cacheRoot), Safe(identity.Name),
                Safe(identity.Version), packageHash);
            Directory.CreateDirectory(extractionDirectory);

            var sources = new List<SmileSourceDocument>();
            var declaredSources = RequiredValues(manifest.Sources, "sources");
            var sourceHashes = manifest.SourceHashes;
            if (sourceHashes == null)
                throw new InvalidDataException("SMILE library manifest is missing sourceHashes.");
            foreach (var sourceName in declaredSources.OrderBy(item => item, StringComparer.Ordinal))
            {
                ValidateEntryName(sourceName);
                if (!sourceName.StartsWith("src/", StringComparison.Ordinal))
                    throw new InvalidDataException($"SMILE library source entry is outside src/: {sourceName}");
                var entry = archive.GetEntry(sourceName)
                    ?? throw new InvalidDataException($"SMILE library declared source is missing: {sourceName}");
                var bytes = ReadAllBytes(entry);
                if (!sourceHashes.TryGetValue(sourceName, out var declaredHash) ||
                    !string.Equals(declaredHash, Hash(bytes), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"SMILE library source hash is invalid: {sourceName}");
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
            var actualApi = Encoding.UTF8.GetString(ReadAllBytes(apiEntry));
            return new SmileLibraryLoadResult(identity, sources, packageHash, extractionDirectory,
                fullPackagePath, actualApi);
        }
    }

    public static string BuildPublicApi(IEnumerable<ModuleSymbol> modules,
        SmileCompilationDependencyContext dependencyContext)
    {
        if (modules == null) throw new ArgumentNullException(nameof(modules));
        if (dependencyContext == null) throw new ArgumentNullException(nameof(dependencyContext));
        var builder = new StringBuilder("{\n  \"formatVersion\": 4,\n  \"modules\": [");
        var orderedModules = modules.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        for (var moduleIndex = 0; moduleIndex < orderedModules.Length; moduleIndex++)
        {
            var module = orderedModules[moduleIndex];
            builder.Append(moduleIndex == 0 ? "\n" : ",\n")
                .Append("    {\"name\": \"").Append(JsonEscape(module.Name)).Append("\", \"members\": [");
            var members = module.PublicMembers.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Kind).ToArray();
            for (var memberIndex = 0; memberIndex < members.Length; memberIndex++)
            {
                var member = members[memberIndex];
                builder.Append(memberIndex == 0 ? "\n" : ",\n")
                    .Append("      {\"name\": \"").Append(JsonEscape(member.Name))
                    .Append("\", \"kind\": \"").Append(member.Kind).Append('"');
                if (member.Variable != null)
                {
                    builder.Append(", \"type\": \"").Append(JsonEscape(TypeIdentity(member.Variable.Type))).Append('"');
                    AppendTypeProvider(builder, "typeProvider", member.Variable.Type, dependencyContext);
                    if (member.Variable.IsConstant)
                        builder.Append(", \"value\": ").Append(JsonValue(member.Variable.ConstantValue));
                    if (member.Variable.IsArray)
                    {
                        builder.Append(", \"elementType\": \"").Append(JsonEscape(TypeIdentity(member.Variable.Type)))
                            .Append('"');
                        AppendTypeProvider(builder, "elementProvider", member.Variable.Type, dependencyContext);
                        builder.Append(", \"rank\": ").Append(member.Variable.ArrayRank.ToString(CultureInfo.InvariantCulture))
                            .Append(", \"dimensions\": [")
                            .Append(string.Join(", ", member.Variable.ArrayDimensions.Select(size =>
                                size.ToString(CultureInfo.InvariantCulture)))).Append(']');
                    }
                }
                if (member.Routine != null)
                {
                    builder.Append(", \"returnType\": \"")
                        .Append(member.Routine.IsFunction ? JsonEscape(TypeIdentity(member.Routine.ReturnType)) : "VOID")
                        .Append('"');
                    if (member.Routine.IsFunction)
                        AppendTypeProvider(builder, "returnProvider", member.Routine.ReturnType, dependencyContext);
                    builder.Append(", \"parameters\": [")
                        .Append(string.Join(", ", member.Routine.Parameters.Select(parameter => ParameterJson(parameter,
                            dependencyContext)))).Append(']');
                }
                if (member.Type != null)
                {
                    builder.Append(", \"identity\": \"").Append(JsonEscape(TypeIdentity(member.Type)))
                        .Append("\", \"module\": \"").Append(JsonEscape(member.Type.ModuleName ?? string.Empty))
                        .Append("\", \"provider\": \"").Append(JsonEscape(LogicalProviderIdentity(member.Type,
                            dependencyContext)))
                        .Append("\", \"size\": ").Append(member.Type.Size.ToString(CultureInfo.InvariantCulture))
                        .Append(", \"fields\": [")
                        .Append(string.Join(", ", member.Type.Fields.OrderBy(field => field.Ordinal)
                            .Select(field => FieldJson(field, dependencyContext))))
                        .Append(']');
                }
                member.Source.GetLineColumn(member.DeclarationSpan.Start, out var line, out var column);
                builder.Append(", \"source\": \"").Append(JsonEscape(Normalize(Path.GetFileName(member.Source.FilePath))))
                    .Append("\", \"line\": ").Append(line.ToString(CultureInfo.InvariantCulture))
                    .Append(", \"column\": ").Append(column.ToString(CultureInfo.InvariantCulture)).Append('}');
            }
            if (members.Length != 0) builder.Append('\n').Append("    ");
            builder.Append("]}");
        }
        if (orderedModules.Length != 0) builder.Append('\n').Append("  ");
        return builder.Append("]\n}\n").ToString();
    }

    private static string BuildManifest(SmileProjectSourceSet project, IEnumerable<string> modules,
        IEnumerable<string> sources, IReadOnlyDictionary<string, string> hashes,
        IReadOnlyList<SmileLibraryDependency> dependencies)
    {
        var moduleJson = string.Join(", ", modules.OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .Select(item => "\"" + JsonEscape(item) + "\""));
        var sourceJson = string.Join(", ", sources.OrderBy(item => item, StringComparer.Ordinal)
            .Select(item => "\"" + JsonEscape(item) + "\""));
        var hashJson = string.Join(",\n", hashes.OrderBy(item => item.Key, StringComparer.Ordinal)
            .Select(item => "    \"" + JsonEscape(item.Key) + "\": \"" + JsonEscape(item.Value) + "\""));
        var dependencyJson = string.Join(",\n", dependencies.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => "    {\"name\": \"" + JsonEscape(item.Name) + "\", \"version\": \"" +
                            JsonEscape(item.Version) + "\"}"));
        return "{\n  \"formatVersion\": 4,\n  \"name\": \"" + JsonEscape(project.LibraryName) +
               "\",\n  \"version\": \"" + JsonEscape(project.Version) + "\",\n  \"modules\": [" + moduleJson +
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
            throw new InvalidDataException(manifest.FormatVersion is 1 or 2 or 3
                ? $"SMILE library formatVersion {manifest.FormatVersion} is obsolete; rebuild the library with the current SMILE compiler (formatVersion 4)."
                : $"Unsupported SMILE library formatVersion {manifest.FormatVersion}; expected {CurrentFormatVersion}. Rebuild the library with the current SMILE compiler.");
        var name = RequiredValue(manifest.Name, "name");
        var version = RequiredValue(manifest.Version, "version");
        ValidateExactVersion(version, $"library '{name}'");
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

    private static void ValidateEntries(ZipArchive archive)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in archive.Entries)
        {
            ValidateEntryName(entry.FullName);
            if (!names.Add(entry.FullName))
                throw new InvalidDataException($"Duplicate SMILE library archive entry: {entry.FullName}");
            if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
                throw new InvalidDataException($"Directory entries are not allowed in SMILE libraries: {entry.FullName}");
        }
    }

    private static void ValidateEntryName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Contains('\\') || name.StartsWith("/", StringComparison.Ordinal) ||
            name.Contains(":") || name.Split('/').Any(part => part is ".." or "." or ""))
            throw new InvalidDataException($"Unsafe SMILE library archive path: {name}");
    }

    private static byte[] ReadAllBytes(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var output = new MemoryStream();
        stream.CopyTo(output);
        return output.ToArray();
    }

    private static string Normalize(string path) => path.Replace('\\', '/');
    private static string NormalizeText(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n');
    private static string Safe(string value) => string.Concat(value.Select(character =>
        char.IsLetterOrDigit(character) || character is '.' or '-' or '_' ? character : '_'));
    private static string Hash(byte[] bytes)
    {
        using var hash = SHA256.Create();
        return string.Concat(hash.ComputeHash(bytes).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
    }

    private static PackageManifest ReadManifest(Stream stream)
    {
        try
        {
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

    private static string TypeIdentity(SmileType type) => type is RecordTypeSymbol record
        ? record.RuntimeIdentity
        : type.Name;

    private static string LogicalProviderIdentity(SmileType type,
        SmileCompilationDependencyContext dependencyContext)
    {
        if (type is not RecordTypeSymbol record)
            return string.Empty;
        if (!dependencyContext.TryGetProviderDescriptor(record.ProviderIdentity, out var descriptor) ||
            string.IsNullOrWhiteSpace(descriptor.LogicalIdentity))
            throw new InvalidDataException(
                $"Public record type '{record.RuntimeIdentity}' has no exact logical library provider descriptor.");
        return descriptor.LogicalIdentity;
    }

    private static void AppendTypeProvider(StringBuilder builder, string propertyName, SmileType type,
        SmileCompilationDependencyContext dependencyContext)
    {
        if (type is RecordTypeSymbol)
            builder.Append(", \"").Append(propertyName).Append("\": \"")
                .Append(JsonEscape(LogicalProviderIdentity(type, dependencyContext))).Append('"');
    }

    private static string ParameterJson(VariableSymbol parameter,
        SmileCompilationDependencyContext dependencyContext)
    {
        var builder = new StringBuilder("{\"name\": \"").Append(JsonEscape(parameter.Name))
            .Append("\", \"type\": \"").Append(JsonEscape(TypeIdentity(parameter.Type))).Append('"');
        AppendTypeProvider(builder, "typeProvider", parameter.Type, dependencyContext);
        return builder.Append(", \"mode\": \"").Append(parameter.ParameterMode).Append("\"}").ToString();
    }

    private static string FieldJson(RecordFieldSymbol field,
        SmileCompilationDependencyContext dependencyContext)
    {
        field.Source.GetLineColumn(field.DeclarationSpan.Start, out var line, out var column);
        var builder = new StringBuilder("{\"name\": \"").Append(JsonEscape(field.Name))
            .Append("\", \"type\": \"").Append(JsonEscape(TypeIdentity(field.Type))).Append('"');
        AppendTypeProvider(builder, "typeProvider", field.Type, dependencyContext);
        return builder.Append(", \"ordinal\": ").Append(field.Ordinal.ToString(CultureInfo.InvariantCulture))
            .Append(", \"offset\": ").Append(field.Offset.ToString(CultureInfo.InvariantCulture))
            .Append(", \"source\": \"").Append(JsonEscape(Normalize(Path.GetFileName(field.Source.FilePath))))
            .Append("\", \"line\": ").Append(line.ToString(CultureInfo.InvariantCulture))
            .Append(", \"column\": ").Append(column.ToString(CultureInfo.InvariantCulture)).Append('}').ToString();
    }

    [DataContract]
    private sealed class PackageManifest
    {
        [DataMember(Name = "formatVersion", Order = 0)] public int FormatVersion { get; set; }
        [DataMember(Name = "name", Order = 1)] public string? Name { get; set; }
        [DataMember(Name = "version", Order = 2)] public string? Version { get; set; }
        [DataMember(Name = "modules", Order = 3)] public string[]? Modules { get; set; }
        [DataMember(Name = "sources", Order = 4)] public string[]? Sources { get; set; }
        [DataMember(Name = "sourceHashes", Order = 5)] public Dictionary<string, string>? SourceHashes { get; set; }
        [DataMember(Name = "dependencies", Order = 6)] public PackageDependency[]? Dependencies { get; set; }
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
                .OrderBy(module => module.Name, StringComparer.OrdinalIgnoreCase).ToArray();
            if (!provider.Identity.Modules.SequenceEqual(modules.Select(module => module.Name),
                    StringComparer.OrdinalIgnoreCase))
                throw new SmileProjectDiagnosticException("SML3207",
                    $"Packaged library '{provider.Identity.Name}' {provider.Identity.Version} at '{provider.ProviderPath}' " +
                    "has a manifest module list that does not match its owned source modules.", provider.ProviderPath);
            var expectedApi = SmileLibraryPackage.BuildPublicApi(modules, analysis.DependencyContext);
            if (!string.Equals(provider.Package!.PublicApiMetadata, expectedApi, StringComparison.Ordinal))
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
