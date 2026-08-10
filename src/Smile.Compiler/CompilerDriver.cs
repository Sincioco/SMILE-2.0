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
            Console.Error.WriteLine("Usage: smilec <source.smile> [--target windows-x64 -o <output.exe>] [--target web --output-dir <directory>] [--keep-temp] [--debug] [--graphics auto|gdi|directx] [--vsync true|false]");
            return 2;
        }

        try
        {
            var sourcePath = Path.GetFullPath(options.SourcePath!);
            var outputPath = options.OutputPath == null
                ? Path.ChangeExtension(sourcePath, ".exe")
                : Path.GetFullPath(options.OutputPath);

            if (!File.Exists(sourcePath))
            {
                Console.Error.WriteLine($"{sourcePath}(1,1): error SML5001: Source file was not found.");
                return 2;
            }

            var source = File.ReadAllText(sourcePath);
            var analysis = SmileLanguage.Analyze(source, sourcePath);
            foreach (var diagnostic in analysis.Diagnostics)
                PrintDiagnostic(diagnostic);

            if (analysis.HasErrors)
                return 1;

            if (options.Target == SmileCompilationTarget.Web)
            {
                var outputDirectory = Path.GetFullPath(options.OutputDirectory!);
                try
                {
                    WebOutputWriter.Write(outputDirectory, new WebEmitter(analysis));
                }
                catch (WebTargetException exception)
                {
                    PrintWebDiagnostic(analysis.SyntaxTree.Source, exception);
                    return 1;
                }

                Console.WriteLine($"Compiled {sourcePath} for Web");
                Console.WriteLine($"Output: {outputDirectory}");
                return 0;
            }

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
                File.WriteAllText(debugSourcePath, BuildDebugSource(sourcePath, emitter.DebugLines));
            var isGame = analysis.SyntaxTree.Root.Statements.Any(statement => statement is GameWindowStatementSyntax);
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
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
        {
            Console.Error.WriteLine($"error SML5004: {exception.Message}");
            return 2;
        }
    }

    private static string BuildDebugSource(string sourcePath, IEnumerable<int> lines)
    {
        var escapedPath = sourcePath.Replace("\\", "\\\\").Replace("\"", "\\\"");
        var builder = new StringBuilder("static volatile unsigned char smile_debug_counter;\n");
        foreach (var line in lines)
        {
            builder.Append("#line ").Append(line).Append(" \"").Append(escapedPath).Append("\"\n")
                .Append("__declspec(noinline) void smile_debug_line_").Append(line)
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
