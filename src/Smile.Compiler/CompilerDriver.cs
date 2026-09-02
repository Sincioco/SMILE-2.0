using Smile.Language;
using System.Security.Cryptography;
using System.Text;

namespace Smile.Compiler;

internal sealed class CompilerDriver
{
    private readonly CompilerDriverTestHooks? _testHooks;

    public CompilerDriver() { }

    internal CompilerDriver(CompilerDriverTestHooks testHooks) => _testHooks = testHooks;

    public int Run(string[] args)
    {
        if (!CompilerOptions.TryParse(args, out var options, out var argumentError))
        {
            if (!string.IsNullOrWhiteSpace(argumentError))
                Console.Error.WriteLine($"error SML5007: {argumentError}");
            Console.Error.WriteLine("SMILE 2.0: Simple Modern and Intuitive Language for Everyone");
            Console.Error.WriteLine("Usage: smilec <startup.smile> [--source <support.smile>]... [--library <package.smilelib>]... [--application-id <id>] [--target windows-x64 -o <output.exe>] [--target web --output-dir <directory>] | smilec --project <project> [--target windows-x64|web|library] [-o <output>] [--configuration <name>] [--application-id <id>]");
            return 2;
        }

        try
        {
            var input = options.ProjectPath != null ? LoadProject(options) : LoadLoose(options);
            var appIdentity = ResolveApplicationIdentity(input, options);
            var sourcePath = input.DisplayPath;
            var analysis = SmileLanguage.Analyze(input.Sources, input.CompilationKind, input.DependencyContext);
            foreach (var diagnostic in analysis.Diagnostics)
                PrintDiagnostic(diagnostic);

            if (analysis.HasErrors)
                return 1;

            input.Project?.ValidateAssetsForBuild();
            if (input.Project != null)
                BuildProjectDependencies(input.Project.ProjectPath, options.Configuration);

            if (options.Target == SmileCompilationTarget.Library)
            {
                if (input.Project == null || !input.Project.IsLibrary)
                    throw new InvalidDataException("The library target requires a .smilelibproj library project.");
                var packagePath = options.OutputPath == null
                    ? input.Project.GetLibraryOutputPath(options.Configuration)
                    : Path.GetFullPath(options.OutputPath);
                SmileLibraryPackage.Write(packagePath, input.Project, analysis);
                Console.WriteLine($"Compiled {input.Project.ProjectPath} as a SMILE library");
                Console.WriteLine($"Output: {packagePath}");
                return 0;
            }

            var buildAssets = input.Project == null ? null : Model3DAssetBuildPipeline.Prepare(input.Project);

            if (options.Target == SmileCompilationTarget.Web)
            {
                var outputDirectory = Path.GetFullPath(options.OutputDirectory!);
                using var outputLock = OutputPublicationLock.Acquire(outputDirectory,
                    _testHooks?.OutputLockTimeout);
                var webStagingDirectory = TransactionalOutputPublisher.CreateStagingDirectory(outputDirectory);
                SmileProjectAssetPublishResult? publication = null;
                try
                {
                    var previousPaths = new List<string>(WebOutputWriter.ManagedFileNames);
                    SmilePublishedAssetSnapshot? previousAssets = null;
                    if (input.Project != null)
                    {
                        previousAssets = SmileProjectAssetPublisher.ReadPublishedAssets(outputDirectory,
                            appIdentity, "web", input.Project.ProjectPath);
                        previousPaths.AddRange(previousAssets.AssetPaths);
                        previousPaths.Add(previousAssets.ManifestName);
                        previousPaths.AddRange(previousAssets.LegacyManifestNames);
                    }

                    WebOutputWriter.Write(webStagingDirectory, new WebEmitter(analysis, appIdentity,
                        buildAssets?.AssetPaths,
                        responsiveWindow: input.Project?.ResponsiveWindow == true),
                        _testHooks?.AfterWebStagedFile);
                    var currentPaths = new List<string>(WebOutputWriter.ManagedFileNames);
                    if (input.Project != null)
                    {
                        publication = SmileProjectAssetPublisher.Publish(buildAssets!.Manifest,
                            webStagingDirectory, appIdentity, "web", null, false,
                            _testHooks?.AssetPublicationHook);
                        currentPaths.AddRange(buildAssets.AssetPaths);
                        currentPaths.Add(Path.GetFileName(publication.ManifestPath));
                    }
                    TransactionalOutputPublisher.PublishDirectory(webStagingDirectory, outputDirectory,
                        currentPaths, previousPaths, _testHooks?.OutputPublicationHook);
                    if (publication != null && previousAssets != null && previousAssets.Warnings.Count != 0)
                        publication = new SmileProjectAssetPublishResult(publication.PublishedCount,
                            Path.Combine(outputDirectory, Path.GetFileName(publication.ManifestPath)),
                            previousAssets.Warnings.Concat(publication.Warnings).ToArray());
                }
                catch (WebTargetException exception)
                {
                    PrintWebDiagnostic(exception.SourceText, exception);
                    return 1;
                }
                finally
                {
                    TransactionalOutputPublisher.TryDeleteDirectory(webStagingDirectory);
                }

                if (publication != null)
                    foreach (var warning in publication.Warnings)
                        Console.Error.WriteLine(warning.FormatCompiler());
                Console.WriteLine($"Compiled {sourcePath} for Web");
                Console.WriteLine($"Output: {outputDirectory}");
                if (publication != null)
                    Console.WriteLine($"Published {publication.PublishedCount} project assets.");
                return 0;
            }

            var outputPath = options.OutputPath == null
                ? input.DefaultNativeOutputPath
                : Path.GetFullPath(options.OutputPath);

            using var nativeOutputLock = OutputPublicationLock.Acquire(outputPath,
                _testHooks?.OutputLockTimeout);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            var baseName = Path.GetFileNameWithoutExtension(outputPath);
            using var intermediates = new CompilerIntermediateDirectory(input.DisplayPath, baseName,
                options.KeepTemp);
            var assemblyPath = intermediates.AssemblyPath;
            var objectPath = intermediates.ObjectPath;
            var debugSourcePath = intermediates.DebugSourcePath;
            var debugObjectPath = intermediates.DebugObjectPath;
            var repositoryRoot = FindRepositoryRoot();
            var runtimePath = FindRuntimeLibrary(repositoryRoot);

            if (runtimePath == null)
            {
                Console.Error.WriteLine($"{sourcePath}(1,1): error SML5002: Smile.NativeRuntime.lib was not found. Run scripts\\build.cmd first.");
                return 2;
            }

            var stagingDirectory = TransactionalOutputPublisher.CreateStagingDirectory(outputPath);
            try
            {
                var stagedOutputPath = Path.Combine(stagingDirectory, Path.GetFileName(outputPath));
                var emitter = new MasmEmitter(analysis, options.GraphicsBackend, options.VSync,
                    options.EmitDebugInformation, appIdentity,
                    buildAssets?.AssetPaths,
                    rememberWindowPlacement: input.Project?.RememberWindowPlacement == true,
                    responsiveWindow: input.Project?.ResponsiveWindow == true);
                File.WriteAllText(assemblyPath, emitter.Emit());
                _testHooks?.AfterAssemblyEmission?.Invoke(intermediates);
                if (options.EmitDebugInformation)
                    File.WriteAllText(debugSourcePath, BuildDebugSource(emitter.DebugSites),
                        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                var isGame = analysis.BoundSyntaxTrees.Any(tree => tree.Root.Statements.Any(statement => statement is GameWindowStatementSyntax));
                var invocation = new NativeToolchainInvocation(assemblyPath, objectPath, stagedOutputPath,
                    runtimePath, isGame, emitter.UsesMusic,
                    options.EmitDebugInformation ? debugSourcePath : null,
                    options.EmitDebugInformation ? debugObjectPath : null);
                var result = _testHooks?.RunNativeToolchain?.Invoke(invocation) ??
                    new NativeToolchain().AssembleAndLink(invocation.AssemblyPath, invocation.ObjectPath,
                        invocation.OutputPath, invocation.RuntimePath, invocation.IsGame, invocation.UsesMusic,
                        invocation.DebugSourcePath, invocation.DebugObjectPath);
                if (!result.Success)
                {
                    var code = result.Status == ProcessExecutionStatus.TimedOut ? "SML5005" :
                        result.Status == ProcessExecutionStatus.Cancelled ? "SML5006" : "SML5003";
                    var message = result.Status == ProcessExecutionStatus.TimedOut ? "Native toolchain timed out." :
                        result.Status == ProcessExecutionStatus.Cancelled ? "Native toolchain was canceled." :
                        "Native toolchain failed.";
                    Console.Error.WriteLine($"{sourcePath}(1,1): error {code}: {message}");
                    if (!string.IsNullOrWhiteSpace(result.Output))
                        Console.Error.WriteLine(result.Output.TrimEnd());
                    if (options.KeepTemp)
                        PrintIntermediatePaths(intermediates, options.EmitDebugInformation);
                    return 2;
                }

                if (!File.Exists(stagedOutputPath))
                    throw new IOException("Native toolchain reported success without producing an executable.");
                var stagedPdbPath = Path.ChangeExtension(stagedOutputPath, ".pdb");
                if (options.EmitDebugInformation && !File.Exists(stagedPdbPath))
                    throw new IOException("Native Debug toolchain reported success without producing a PDB.");

                var outputRoot = Path.GetDirectoryName(outputPath)!;
                var currentPaths = new List<string> { Path.GetFileName(outputPath) };
                var previousPaths = new List<string> { Path.GetFileName(outputPath) };
                var finalPdbPath = Path.ChangeExtension(outputPath, ".pdb");
                if (options.EmitDebugInformation)
                    currentPaths.Add(Path.GetFileName(finalPdbPath));
                if (File.Exists(finalPdbPath))
                    previousPaths.Add(Path.GetFileName(finalPdbPath));

                SmileProjectAssetPublishResult? nativePublication = null;
                SmilePublishedAssetSnapshot? previousAssets = null;
                if (input.Project != null)
                {
                    var explicitIdentity = options.ApplicationId != null || input.Project.ApplicationId != null;
                    previousAssets = SmileProjectAssetPublisher.ReadPublishedAssets(outputRoot, appIdentity,
                        "windows-x64", input.Project.ProjectPath, Path.GetFileNameWithoutExtension(outputPath),
                        explicitIdentity);
                    previousPaths.AddRange(previousAssets.AssetPaths);
                    previousPaths.Add(previousAssets.ManifestName);
                    previousPaths.AddRange(previousAssets.LegacyManifestNames);
                    nativePublication = SmileProjectAssetPublisher.Publish(buildAssets!.Manifest,
                        stagingDirectory, appIdentity, "windows-x64", Path.GetFileNameWithoutExtension(outputPath),
                        explicitIdentity, _testHooks?.AssetPublicationHook);
                    currentPaths.AddRange(buildAssets.AssetPaths);
                    currentPaths.Add(Path.GetFileName(nativePublication.ManifestPath));
                }

                TransactionalOutputPublisher.PublishDirectory(stagingDirectory, outputRoot, currentPaths,
                    previousPaths, _testHooks?.OutputPublicationHook);
                if (nativePublication != null)
                {
                    var warnings = (previousAssets?.Warnings ?? Array.Empty<SmileProjectDiagnostic>())
                        .Concat(nativePublication.Warnings);
                    foreach (var warning in warnings)
                        Console.Error.WriteLine(warning.FormatCompiler());
                }

                Console.WriteLine($"Compiled {sourcePath}");
                Console.WriteLine($"Output: {outputPath}");
                if (nativePublication != null)
                    Console.WriteLine($"Published {nativePublication.PublishedCount} project assets.");
                if (options.EmitDebugInformation)
                    EqualFilePresence(stagedPdbPath, finalPdbPath);
                if (options.KeepTemp)
                    PrintIntermediatePaths(intermediates, options.EmitDebugInformation);
                return 0;
            }
            finally
            {
                TransactionalOutputPublisher.TryDeleteDirectory(stagingDirectory);
            }
        }
        catch (SmileProjectDiagnosticException exception)
        {
            Console.Error.WriteLine(exception.Diagnostic.FormatCompiler());
            return 1;
        }
        catch (InvalidDataException exception)
        {
            var path = options.ProjectPath ?? options.SourcePath ?? "<project>";
            Console.Error.WriteLine(new SmileProjectDiagnostic("SML3206", exception.Message,
                Path.GetFullPath(path)).FormatCompiler());
            return 1;
        }
        catch (OutputLockTimeoutException exception)
        {
            var path = options.ProjectPath ?? options.SourcePath ?? exception.TargetPath;
            Console.Error.WriteLine($"{Path.GetFullPath(path)}(1,1): error SML5008: {exception.Message}");
            return 2;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException ||
                                          exception is ArgumentException)
        {
            Console.Error.WriteLine($"error SML5004: {exception.Message}");
            return 2;
        }
    }

    internal static string CreateIntermediateBaseName(string baseName, bool keepTemp) =>
        keepTemp ? baseName : $"{baseName}.{Environment.ProcessId}.{Guid.NewGuid():N}";

    internal static string CreateNativeBuildMutexName(string outputPath) =>
        OutputPublicationLock.CreateMutexName(outputPath);

    private static CompilationInput LoadProject(CompilerOptions options)
    {
        var projectPath = Path.GetFullPath(options.ProjectPath!);
        var compilation = SmileProjectCompilation.Load(projectPath);
        var project = compilation.Graph.Root;
        if (project.IsLibrary && options.Target != SmileCompilationTarget.Library)
            throw new InvalidDataException("A SMILE library project must be built with --target library.");
        if (!project.IsLibrary && options.Target == SmileCompilationTarget.Library)
            throw new InvalidDataException("The library target requires a .smilelibproj library project.");
        var configuration = options.Configuration.StartsWith("Release", StringComparison.OrdinalIgnoreCase) ? "Release" : "Debug";
        var defaultOutput = Path.Combine(project.ProjectDirectory, "bin", configuration, project.OutputName + ".exe");
        return new CompilationInput(project.ProjectPath, compilation.Sources, compilation.CompilationKind,
            defaultOutput, project, compilation.DependencyContext);
    }

    private static string ResolveApplicationIdentity(CompilationInput input, CompilerOptions options)
    {
        if (input.Project?.IsLibrary == true)
        {
            if (options.ApplicationId != null)
                throw new SmileProjectDiagnosticException("SML3802",
                    "Library projects do not own an ApplicationId and cannot use --application-id.",
                    input.Project.ProjectPath);
            return input.Project.OutputName;
        }

        var explicitOverride = options.ApplicationId == null
            ? null
            : SmileApplicationIdentity.ValidateExplicit(options.ApplicationId, input.DisplayPath);
        if (input.Project != null)
        {
            if (explicitOverride != null && input.Project.ApplicationId != null &&
                !string.Equals(explicitOverride, input.Project.ApplicationId, StringComparison.Ordinal))
                throw new SmileProjectDiagnosticException("SML3803",
                    $"--application-id '{explicitOverride}' conflicts with project ApplicationId '{input.Project.ApplicationId}'.",
                    input.Project.ProjectPath);
            return explicitOverride ?? input.Project.EffectiveApplicationId;
        }

        return explicitOverride ?? Path.GetFileNameWithoutExtension(input.DisplayPath);
    }

    private static void BuildProjectDependencies(string projectPath, string configuration)
    {
        var graph = SmileProjectBuildGraph.Load(projectPath);
        foreach (var dependency in graph.BuildOrder.Where(project => project.IsLibrary &&
                     !string.Equals(project.ProjectPath, graph.Root.ProjectPath, StringComparison.OrdinalIgnoreCase)))
        {
            dependency.ValidateAssetsForBuild();
            var dependencyOutput = dependency.GetLibraryOutputPath(configuration);
            var dependencyCompilation = SmileProjectCompilation.Load(dependency.ProjectPath);
            var dependencyAnalysis = SmileLanguage.Analyze(dependencyCompilation.Sources,
                SmileCompilationKind.Library, dependencyCompilation.DependencyContext);
            foreach (var diagnostic in dependencyAnalysis.Diagnostics) PrintDiagnostic(diagnostic);
            if (dependencyAnalysis.HasErrors)
                throw new SmileProjectDiagnosticException("SML3207",
                    $"Referenced library project failed authoritative analysis: {dependency.ProjectPath}",
                    dependency.ProjectPath);
            if (!NeedsLibraryBuild(dependency, dependencyOutput, dependencyAnalysis))
            {
                Console.WriteLine($"Dependency is up to date: {dependencyOutput}");
                continue;
            }
            SmileLibraryPackage.Write(dependencyOutput, dependency, dependencyAnalysis);
            Console.WriteLine($"Built dependency: {dependencyOutput}");
        }
    }

    internal static bool NeedsLibraryBuild(SmileProjectSourceSet project, string outputPath,
        SmileAnalysisResult analysis) => !SmileLibraryPackage.IsCurrentProjectBuild(outputPath, project, analysis);

    private static CompilationInput LoadLoose(CompilerOptions options)
    {
        var sourcePath = Path.GetFullPath(options.SourcePath!);
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Source file was not found.", sourcePath);
        var sourcePaths = new List<string> { sourcePath };
        var normalizedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { sourcePath };
        foreach (var supportOption in options.SourcePaths)
        {
            var supportPath = Path.GetFullPath(supportOption);
            if (!normalizedPaths.Add(supportPath))
                throw new InvalidDataException($"Duplicate SMILE source path: {supportPath}");
            if (!File.Exists(supportPath))
                throw new FileNotFoundException("Support source file was not found.", supportPath);
            sourcePaths.Add(supportPath);
        }
        var documents = sourcePaths.Select((path, index) =>
            new SmileSourceDocument(File.ReadAllText(path), path, isStartup: index == 0)).ToList();
        var libraryResolution = LoadLooseLibraryResolution(sourcePath, options.LibraryPaths);
        documents.AddRange(libraryResolution.Sources);
        return new CompilationInput(sourcePath, documents, SmileCompilationKind.Program,
            Path.ChangeExtension(sourcePath, ".exe"), project: null, libraryResolution.CreateLooseRootContext());
    }

    internal static IReadOnlyList<SmileSourceDocument> LoadLooseLibraries(string sourcePath,
        IEnumerable<string> libraryPaths) => LoadLooseLibraryResolution(sourcePath, libraryPaths).Sources;

    internal static SmileLibraryResolution LoadLooseLibraryResolution(string sourcePath,
        IEnumerable<string> libraryPaths)
    {
        var fullSourcePath = Path.GetFullPath(sourcePath);
        var cacheRoot = Path.Combine(Path.GetDirectoryName(fullSourcePath)!, "obj", "Smile", "Libraries");
        return SmileLibraryProviderResolver.LoadPackages(libraryPaths.Select(Path.GetFullPath), cacheRoot);
    }

    private sealed class CompilationInput
    {
        public CompilationInput(string displayPath, IReadOnlyList<SmileSourceDocument> sources,
            SmileCompilationKind compilationKind, string defaultNativeOutputPath, SmileProjectSourceSet? project,
            SmileCompilationDependencyContext dependencyContext)
        {
            DisplayPath = displayPath;
            Sources = sources;
            CompilationKind = compilationKind;
            DefaultNativeOutputPath = defaultNativeOutputPath;
            Project = project;
            DependencyContext = dependencyContext;
        }
        public string DisplayPath { get; }
        public IReadOnlyList<SmileSourceDocument> Sources { get; }
        public SmileCompilationKind CompilationKind { get; }
        public string DefaultNativeOutputPath { get; }
        public SmileProjectSourceSet? Project { get; }
        public SmileCompilationDependencyContext DependencyContext { get; }
    }

    internal static string BuildDebugSource(IEnumerable<MasmDebugSite> sites)
    {
        var builder = new StringBuilder(
            "typedef enum SmileDebugBoolean { False = 0, True = 1 } SmileDebugBoolean;\n" +
            "typedef struct SmileDebugText { long long references; long long length; char bytes[1]; } SmileDebugText;\n" +
            "static volatile unsigned char smile_debug_counter;\n");
        foreach (var site in sites)
        {
            var escapedPath = site.Source.FilePath.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var parameters = site.Variables.Count == 0
                ? "void"
                : string.Join(", ", site.Variables.Select((symbol, ordinal) =>
                    DebugParameterDeclaration(symbol, ordinal)));
            var aliases = string.Concat(site.Variables.Select((symbol, ordinal) =>
                DebugAliasDeclaration(symbol, ordinal)));
            builder.Append("#line ").Append(site.Line).Append(" \"").Append(escapedPath).Append("\"\n")
                .Append("__declspec(noinline) void ").Append(site.HelperName)
                .Append('(').Append(parameters).Append(") {").Append(aliases)
                .Append(" smile_debug_counter++; }\n");
        }
        return builder.ToString();
    }

    private static string DebugParameterDeclaration(VariableSymbol symbol, int ordinal) =>
        DebugParameterType(symbol) + " smile_debug_v" + ordinal;

    private static string DebugParameterType(VariableSymbol symbol)
    {
        var type = symbol.IsArray
            ? symbol.Type.Kind switch
            {
                SmileTypeKind.Number => "const long long*",
                SmileTypeKind.Enum => "const long long*",
                SmileTypeKind.Boolean => "const SmileDebugBoolean*",
                SmileTypeKind.Text => "const SmileDebugText* const*",
                _ => "const void*"
            }
            : symbol.Type.Kind switch
            {
                SmileTypeKind.Number => "long long",
                SmileTypeKind.Enum => "long long",
                SmileTypeKind.Boolean => "SmileDebugBoolean",
                SmileTypeKind.Text => "const char*",
                _ => "const void*"
            };
        return type;
    }

    private static string DebugAliasDeclaration(VariableSymbol symbol, int ordinal)
    {
        if (!IsSafeDebugAlias(symbol.Name))
            return string.Empty;
        return " " + DebugParameterType(symbol) + " " + symbol.Name + " = smile_debug_v" + ordinal +
               "; (void)" + symbol.Name + ";";
    }

    private static bool IsSafeDebugAlias(string name)
    {
        if (string.IsNullOrEmpty(name) || !(name[0] is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '_') ||
            name.Skip(1).Any(character => !(character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_')))
            return false;
        return !name.StartsWith("smile_debug_", StringComparison.OrdinalIgnoreCase) && !CKeywords.Contains(name);
    }

    private static readonly HashSet<string> CKeywords = new(StringComparer.Ordinal)
    {
        "auto", "break", "case", "char", "const", "continue", "default", "do", "double", "else",
        "enum", "extern", "float", "for", "goto", "if", "inline", "int", "long", "register",
        "restrict", "return", "short", "signed", "sizeof", "static", "struct", "switch", "typedef",
        "union", "unsigned", "void", "volatile", "while", "_Alignas", "_Alignof", "_Atomic", "_Bool",
        "_Complex", "_Generic", "_Imaginary", "_Noreturn", "_Static_assert", "_Thread_local"
    };

    private static string? FindRepositoryRoot()
    {
        foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "SMILE 2.0.sln")))
                    return directory.FullName;
                directory = directory.Parent;
            }
        }

        return null;
    }

    private static string? FindRuntimeLibrary(string? repositoryRoot)
    {
        var candidates = new List<string> { Path.Combine(AppContext.BaseDirectory, "Smile.NativeRuntime.lib") };
        if (repositoryRoot != null)
            candidates.Add(Path.Combine(repositoryRoot, "artifacts", "runtime", "Smile.NativeRuntime.lib"));

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }
        return null;
    }

    private static void PrintDiagnostic(Diagnostic diagnostic)
    {
        var path = string.IsNullOrEmpty(diagnostic.FilePath) ? "<source>" : diagnostic.FilePath;
        var severity = diagnostic.Severity == DiagnosticSeverity.Error ? "error" : "warning";
        Console.Error.WriteLine($"{path}({diagnostic.Line},{diagnostic.Column}): {severity} {diagnostic.Code}: {diagnostic.Message}");
    }

    private static void PrintWebDiagnostic(SourceText source, WebTargetException exception)
    {
        source.GetLineColumn(exception.Span.Start, out var line, out var column);
        var path = string.IsNullOrEmpty(source.FilePath) ? "<source>" : source.FilePath;
        Console.Error.WriteLine($"{path}({line},{column}): error {exception.Code}: {exception.Message}");
    }

    private static void PrintIntermediatePaths(CompilerIntermediateDirectory intermediates, bool includeDebug)
    {
        Console.WriteLine($"Intermediate directory: {intermediates.DirectoryPath}");
        Console.WriteLine($"Assembly: {intermediates.AssemblyPath}");
        Console.WriteLine($"Object: {intermediates.ObjectPath}");
        if (includeDebug)
        {
            Console.WriteLine($"Debug source map: {intermediates.DebugSourcePath}");
            Console.WriteLine($"Debug object: {intermediates.DebugObjectPath}");
        }
    }

    private static void EqualFilePresence(string stagedPath, string finalPath)
    {
        if (!File.Exists(finalPath))
            throw new IOException($"Native Debug publication did not produce '{finalPath}' from '{stagedPath}'.");
    }
}

internal sealed class NativeToolchainInvocation
{
    public NativeToolchainInvocation(string assemblyPath, string objectPath, string outputPath, string runtimePath,
        bool isGame, bool usesMusic, string? debugSourcePath, string? debugObjectPath)
    {
        AssemblyPath = assemblyPath;
        ObjectPath = objectPath;
        OutputPath = outputPath;
        RuntimePath = runtimePath;
        IsGame = isGame;
        UsesMusic = usesMusic;
        DebugSourcePath = debugSourcePath;
        DebugObjectPath = debugObjectPath;
    }

    public string AssemblyPath { get; }
    public string ObjectPath { get; }
    public string OutputPath { get; }
    public string RuntimePath { get; }
    public bool IsGame { get; }
    public bool UsesMusic { get; }
    public string? DebugSourcePath { get; }
    public string? DebugObjectPath { get; }
}

internal sealed class CompilerDriverTestHooks
{
    public TimeSpan? OutputLockTimeout { get; init; }
    public Action<CompilerIntermediateDirectory>? AfterAssemblyEmission { get; init; }
    public Func<NativeToolchainInvocation, ToolchainResult>? RunNativeToolchain { get; init; }
    public Action<string>? AfterWebStagedFile { get; init; }
    public Action<SmileAssetPublicationStage, string?>? AssetPublicationHook { get; init; }
    public Action<TransactionalPublicationStage, string?>? OutputPublicationHook { get; init; }
}
