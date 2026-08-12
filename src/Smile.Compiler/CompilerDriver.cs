using Smile.Language;
using System.Text;

namespace Smile.Compiler;

internal sealed class CompilerDriver
{
    public int Run(string[] args)
    {
        if (!CompilerOptions.TryParse(args, out var options, out var argumentError))
        {
            if (!string.IsNullOrWhiteSpace(argumentError))
                Console.Error.WriteLine($"error SML5007: {argumentError}");
            Console.Error.WriteLine("Usage: smilec <startup.smile> [--source <support.smile>]... [--library <package.smilelib>]... [--target windows-x64 -o <output.exe>] [--target web --output-dir <directory>] | smilec --project <project> [--target windows-x64|web|library] [-o <output>] [--configuration <name>]");
            return 2;
        }

        try
        {
            var input = options.ProjectPath != null ? LoadProject(options) : LoadLoose(options);
            var sourcePath = input.DisplayPath;
            var analysis = SmileLanguage.Analyze(input.Sources, input.CompilationKind, input.DependencyContext);
            foreach (var diagnostic in analysis.Diagnostics)
                PrintDiagnostic(diagnostic);

            if (analysis.HasErrors)
                return 1;

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

            if (options.Target == SmileCompilationTarget.Web)
            {
                var outputDirectory = Path.GetFullPath(options.OutputDirectory!);
                try
                {
                    WebOutputWriter.Write(outputDirectory, new WebEmitter(analysis));
                }
                catch (WebTargetException exception)
                {
                    PrintWebDiagnostic(exception.SourceText, exception);
                    return 1;
                }

                Console.WriteLine($"Compiled {sourcePath} for Web");
                Console.WriteLine($"Output: {outputDirectory}");
                return 0;
            }

            var outputPath = options.OutputPath == null
                ? input.DefaultNativeOutputPath
                : Path.GetFullPath(options.OutputPath);

            var repositoryRoot = FindRepositoryRoot();
            var tempDirectory = Path.Combine(repositoryRoot, "artifacts", "temp");
            Directory.CreateDirectory(tempDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            var baseName = Path.GetFileNameWithoutExtension(outputPath);
            var assemblyPath = Path.Combine(tempDirectory, baseName + ".asm");
            var objectPath = Path.Combine(tempDirectory, baseName + ".obj");
            var debugSourcePath = Path.Combine(tempDirectory, baseName + ".debug.c");
            var debugObjectPath = Path.Combine(tempDirectory, baseName + ".debug.obj");
            var runtimePath = FindRuntimeLibrary(repositoryRoot);

            if (runtimePath == null)
            {
                Console.Error.WriteLine($"{sourcePath}(1,1): error SML5002: Smile.NativeRuntime.lib was not found. Run scripts\\build.cmd first.");
                return 2;
            }

            var emitter = new MasmEmitter(analysis, options.GraphicsBackend, options.VSync, options.EmitDebugInformation);
            File.WriteAllText(assemblyPath, emitter.Emit());
            if (options.EmitDebugInformation)
                File.WriteAllText(debugSourcePath, BuildDebugSource(emitter.DebugSites));
            var isGame = analysis.BoundSyntaxTrees.Any(tree => tree.Root.Statements.Any(statement => statement is GameWindowStatementSyntax));
            var result = new NativeToolchain().AssembleAndLink(assemblyPath, objectPath, outputPath, runtimePath,
                isGame, emitter.UsesMusic, options.EmitDebugInformation ? debugSourcePath : null,
                options.EmitDebugInformation ? debugObjectPath : null);
            if (!result.Success)
            {
                Console.Error.WriteLine($"{sourcePath}(1,1): error SML5003: Native toolchain failed.");
                if (!string.IsNullOrWhiteSpace(result.Output))
                    Console.Error.WriteLine(result.Output.TrimEnd());
                return 2;
            }

            if (!options.KeepTemp)
            {
                TryDelete(assemblyPath);
                TryDelete(objectPath);
                TryDelete(debugSourcePath);
                TryDelete(debugObjectPath);
            }

            Console.WriteLine($"Compiled {sourcePath}");
            Console.WriteLine($"Output: {outputPath}");
            if (options.KeepTemp)
            {
                Console.WriteLine($"Assembly: {assemblyPath}");
                Console.WriteLine($"Object: {objectPath}");
                if (options.EmitDebugInformation)
                {
                    Console.WriteLine($"Debug source map: {debugSourcePath}");
                    Console.WriteLine($"Debug object: {debugObjectPath}");
                }
            }
            return 0;
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
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException ||
                                          exception is ArgumentException)
        {
            Console.Error.WriteLine($"error SML5004: {exception.Message}");
            return 2;
        }
    }

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

    private static void BuildProjectDependencies(string projectPath, string configuration)
    {
        var graph = SmileProjectBuildGraph.Load(projectPath);
        foreach (var dependency in graph.BuildOrder.Where(project => project.IsLibrary &&
                     !string.Equals(project.ProjectPath, graph.Root.ProjectPath, StringComparison.OrdinalIgnoreCase)))
        {
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
        var builder = new StringBuilder("static volatile unsigned char smile_debug_counter;\n");
        foreach (var site in sites)
        {
            var escapedPath = site.Source.FilePath.Replace("\\", "\\\\").Replace("\"", "\\\"");
            builder.Append("#line ").Append(site.Line).Append(" \"").Append(escapedPath).Append("\"\n")
                .Append("__declspec(noinline) void ").Append(site.HelperName)
                .Append("(void) { smile_debug_counter++; }\n");
        }
        return builder.ToString();
    }

    private static string FindRepositoryRoot()
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

        return Environment.CurrentDirectory;
    }

    private static string? FindRuntimeLibrary(string repositoryRoot)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Smile.NativeRuntime.lib"),
            Path.Combine(repositoryRoot, "artifacts", "runtime", "Smile.NativeRuntime.lib")
        };

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

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
