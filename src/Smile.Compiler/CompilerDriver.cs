using Smile.Language;
using System.Text;

namespace Smile.Compiler;

internal sealed class CompilerDriver
{
    public int Run(string[] args)
    {
        if (!TryParseArguments(args, out var sourcePath, out var outputPath, out var keepTemp, out var emitDebugInformation,
             out var graphicsBackend, out var vSync, out var argumentError))
        {
            if (!string.IsNullOrWhiteSpace(argumentError))
                Console.Error.WriteLine($"error SML5007: {argumentError}");
            Console.Error.WriteLine("Usage: smilec <source.smile> [-o <output.exe>] [--keep-temp] [--debug] [--graphics auto|gdi|directx] [--vsync true|false]");
            return 2;
        }

        try
        {
            sourcePath = Path.GetFullPath(sourcePath!);
            outputPath = outputPath == null
                ? Path.ChangeExtension(sourcePath, ".exe")
                : Path.GetFullPath(outputPath);

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

            var emitter = new MasmEmitter(analysis, graphicsBackend, vSync, emitDebugInformation);
            File.WriteAllText(assemblyPath, emitter.Emit());
            if (emitDebugInformation)
                File.WriteAllText(debugSourcePath, BuildDebugSource(sourcePath, emitter.DebugLines));
            var isGame = analysis.SyntaxTree.Root.Statements.Any(statement => statement is GameWindowStatementSyntax);
            var result = new NativeToolchain().AssembleAndLink(assemblyPath, objectPath, outputPath, runtimePath,
                isGame, emitter.UsesMusic, emitDebugInformation ? debugSourcePath : null,
                emitDebugInformation ? debugObjectPath : null);
            if (!result.Success)
            {
                Console.Error.WriteLine($"{sourcePath}(1,1): error SML5003: Native toolchain failed.");
                if (!string.IsNullOrWhiteSpace(result.Output))
                    Console.Error.WriteLine(result.Output.TrimEnd());
                return 2;
            }

            if (!keepTemp)
            {
                TryDelete(assemblyPath);
                TryDelete(objectPath);
                TryDelete(debugSourcePath);
                TryDelete(debugObjectPath);
            }

            Console.WriteLine($"Compiled {sourcePath}");
            Console.WriteLine($"Output: {outputPath}");
            if (keepTemp)
            {
                Console.WriteLine($"Assembly: {assemblyPath}");
                Console.WriteLine($"Object: {objectPath}");
                if (emitDebugInformation)
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

    private static bool TryParseArguments(string[] args, out string? sourcePath, out string? outputPath,
        out bool keepTemp, out bool emitDebugInformation, out SmileGraphicsBackend graphicsBackend, out bool vSync,
        out string? error)
    {
        sourcePath = null;
        outputPath = null;
        keepTemp = false;
        emitDebugInformation = false;
        graphicsBackend = SmileGraphicsBackend.Auto;
        vSync = true;
        error = null;

        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--keep-temp", StringComparison.OrdinalIgnoreCase))
            {
                keepTemp = true;
            }
            else if (string.Equals(args[i], "--debug", StringComparison.OrdinalIgnoreCase))
            {
                emitDebugInformation = true;
            }
            else if (string.Equals(args[i], "-o", StringComparison.OrdinalIgnoreCase))
            {
                if (++i >= args.Length || outputPath != null)
                    return false;
                outputPath = args[i];
            }
            else if (string.Equals(args[i], "--graphics", StringComparison.OrdinalIgnoreCase))
            {
                if (++i >= args.Length || !Enum.TryParse(args[i], true, out graphicsBackend) ||
                    !Enum.IsDefined(graphicsBackend))
                {
                    error = "--graphics must be Auto, GDI, or DirectX.";
                    return false;
                }
            }
            else if (string.Equals(args[i], "--vsync", StringComparison.OrdinalIgnoreCase))
            {
                if (++i >= args.Length || !bool.TryParse(args[i], out vSync))
                {
                    error = "--vsync must be true or false.";
                    return false;
                }
            }
            else if (sourcePath == null)
            {
                sourcePath = args[i];
            }
            else
            {
                error = $"Unexpected argument '{args[i]}'.";
                return false;
            }
        }

        return sourcePath != null;
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

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
