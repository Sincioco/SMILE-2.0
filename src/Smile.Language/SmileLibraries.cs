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

public sealed class SmileLibraryLoadResult
{
    internal SmileLibraryLoadResult(SmileLibraryIdentity identity, IReadOnlyList<SmileSourceDocument> sources,
        string packageHash, string extractionDirectory)
    {
        Identity = identity;
        Sources = sources;
        PackageHash = packageHash;
        ExtractionDirectory = extractionDirectory;
    }

    public SmileLibraryIdentity Identity { get; }
    public IReadOnlyList<SmileSourceDocument> Sources { get; }
    public string PackageHash { get; }
    public string ExtractionDirectory { get; }
}

public static class SmileLibraryPackage
{
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
        var api = BuildPublicApi(modules);

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

    public static SmileLibraryLoadResult Read(string packagePath, string cacheRoot)
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

            var analysis = SmileLanguage.Analyze(sources, SmileCompilationKind.Library);
            if (analysis.HasErrors)
                throw new InvalidDataException("Packaged SMILE source does not pass authoritative library analysis: " +
                    string.Join("; ", analysis.Diagnostics.Select(item => item.Code + " " + item.Message)));
            var analyzedModules = analysis.SemanticModel.Modules.Values
                .OrderBy(module => module.Name, StringComparer.OrdinalIgnoreCase).ToArray();
            if (!identity.Modules.SequenceEqual(analyzedModules.Select(module => module.Name), StringComparer.OrdinalIgnoreCase))
                throw new InvalidDataException("SMILE library manifest module list does not match its source.");
            var apiEntry = archive.GetEntry("api/public-symbols.json")
                ?? throw new InvalidDataException("SMILE library package is missing api/public-symbols.json.");
            var actualApi = Encoding.UTF8.GetString(ReadAllBytes(apiEntry));
            var expectedApi = BuildPublicApi(analyzedModules);
            if (!string.Equals(actualApi, expectedApi, StringComparison.Ordinal))
                throw new InvalidDataException("SMILE library public API metadata does not match authoritative analysis.");

            return new SmileLibraryLoadResult(identity, sources, packageHash, extractionDirectory);
        }
    }

    public static string BuildPublicApi(IEnumerable<ModuleSymbol> modules)
    {
        var builder = new StringBuilder("{\n  \"formatVersion\": 1,\n  \"modules\": [");
        var orderedModules = modules.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        for (var moduleIndex = 0; moduleIndex < orderedModules.Length; moduleIndex++)
        {
            var module = orderedModules[moduleIndex];
            builder.Append(moduleIndex == 0 ? "\n" : ",\n")
                .Append("    {\"name\": \"").Append(JsonEscape(module.Name)).Append("\", \"members\": [");
            var members = module.PublicMembers.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray();
            for (var memberIndex = 0; memberIndex < members.Length; memberIndex++)
            {
                var member = members[memberIndex];
                builder.Append(memberIndex == 0 ? "\n" : ",\n")
                    .Append("      {\"name\": \"").Append(JsonEscape(member.Name))
                    .Append("\", \"kind\": \"").Append(member.Kind).Append('"');
                if (member.Variable != null)
                {
                    builder.Append(", \"type\": \"").Append(member.Variable.Type).Append('"');
                    if (member.Variable.IsConstant)
                        builder.Append(", \"value\": ").Append(member.Variable.ConstantValue.ToString(CultureInfo.InvariantCulture));
                    if (member.Variable.IsArray)
                    {
                        builder.Append(", \"dimensions\": [")
                            .Append(string.Join(", ", member.Variable.ArrayDimensions.Select(size =>
                                size.ToString(CultureInfo.InvariantCulture)))).Append(']');
                    }
                }
                if (member.Routine != null)
                    builder.Append(", \"returnType\": \"").Append(member.Routine.ReturnType)
                        .Append("\", \"parameters\": [")
                        .Append(string.Join(", ", member.Routine.Parameters.Select(parameter =>
                            "\"" + JsonEscape(parameter.Name) + "\""))).Append(']');
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
        return "{\n  \"formatVersion\": 1,\n  \"name\": \"" + JsonEscape(project.LibraryName) +
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
        if (manifest.FormatVersion != 1)
            throw new InvalidDataException("Unsupported SMILE library formatVersion; expected 1.");
        var name = RequiredValue(manifest.Name, "name");
        var version = RequiredValue(manifest.Version, "version");
        var modules = RequiredValues(manifest.Modules, "modules");
        var dependencies = (manifest.Dependencies ?? Array.Empty<PackageDependency>())
            .Select(dependency => new SmileLibraryDependency(RequiredValue(dependency.Name, "dependency name"),
                RequiredValue(dependency.Version, "dependency version"))).ToArray();
        return new SmileLibraryIdentity(name, version, modules, dependencies);
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

public sealed class SmileProjectBuildGraph
{
    private SmileProjectBuildGraph(SmileProjectSourceSet root, IReadOnlyList<SmileProjectSourceSet> buildOrder)
    {
        Root = root;
        BuildOrder = buildOrder;
    }

    public SmileProjectSourceSet Root { get; }
    public IReadOnlyList<SmileProjectSourceSet> BuildOrder { get; }

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
                        .Select(Path.GetFileName).ToArray();
                    throw new InvalidDataException("SMILE project-reference cycle detected: " + string.Join(" -> ", cycle));
                }
                return projects[path];
            }
            if (!File.Exists(path))
                throw new FileNotFoundException("Referenced SMILE project was not found.", path);
            var project = SmileProjectSourceSet.Load(path);
            projects[path] = project;
            state[path] = 1;
            stack.Add(path);
            foreach (var reference in project.References.Where(item => item.Kind == SmileProjectReferenceKind.Project)
                         .OrderBy(item => item.FullPath, StringComparer.OrdinalIgnoreCase))
            {
                var dependency = Visit(reference.FullPath, isRoot: false);
                if (!dependency.IsLibrary)
                    throw new InvalidDataException($"SMILE project reference must target a library project: {reference.FullPath}");
            }
            stack.RemoveAt(stack.Count - 1);
            state[path] = 2;
            order.Add(project);
            return project;
        }
    }
}

public sealed class SmileProjectCompilation
{
    private SmileProjectCompilation(SmileProjectBuildGraph graph, IReadOnlyList<SmileSourceDocument> sources,
        IReadOnlyList<SmileLibraryLoadResult> packages)
    {
        Graph = graph;
        Sources = sources;
        Packages = packages;
    }

    public SmileProjectBuildGraph Graph { get; }
    public IReadOnlyList<SmileSourceDocument> Sources { get; }
    public IReadOnlyList<SmileLibraryLoadResult> Packages { get; }
    public SmileCompilationKind CompilationKind => Graph.Root.IsLibrary ? SmileCompilationKind.Library : SmileCompilationKind.Program;

    public static SmileProjectCompilation Load(string projectPath, string? cacheRoot = null,
        Func<string, string?>? openText = null)
    {
        var graph = SmileProjectBuildGraph.Load(projectPath);
        var sources = new List<SmileSourceDocument>();
        foreach (var project in new[] { graph.Root }.Concat(graph.BuildOrder.Where(item =>
                     !string.Equals(item.ProjectPath, graph.Root.ProjectPath, StringComparison.OrdinalIgnoreCase))))
        {
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
                sources.Add(new SmileSourceDocument(text, source.FullPath,
                    isStartup: string.Equals(project.ProjectPath, graph.Root.ProjectPath, StringComparison.OrdinalIgnoreCase) && source.IsStartup,
                    isMissing: missing, providerIdentity: project.ProjectPath));
            }
        }

        cacheRoot ??= Path.Combine(graph.Root.ProjectDirectory, "obj", "Smile", "Libraries");
        var packages = new List<SmileLibraryLoadResult>();
        var packagePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in graph.BuildOrder)
        {
            foreach (var reference in project.References.Where(item => item.Kind == SmileProjectReferenceKind.Package)
                         .OrderBy(item => item.FullPath, StringComparer.OrdinalIgnoreCase))
            {
                if (!packagePaths.Add(reference.FullPath))
                    continue;
                var package = SmileLibraryPackage.Read(reference.FullPath, cacheRoot);
                packages.Add(package);
                sources.AddRange(package.Sources);
            }
        }

        var identities = packages.ToDictionary(item => item.Identity.Name, item => item.Identity.Version,
            StringComparer.OrdinalIgnoreCase);
        foreach (var dependency in packages.SelectMany(item => item.Identity.Dependencies))
        {
            if (!identities.TryGetValue(dependency.Name, out var version) ||
                !string.Equals(version, dependency.Version, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Package dependency {dependency.Name} {dependency.Version} must be supplied explicitly.");
        }
        return new SmileProjectCompilation(graph, sources, packages);
    }
}
