using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Smile.Language;

namespace Smile.VisualStudio;

internal static class SmileBuildService
{
    private static readonly Guid OutputPaneGuid = new("9315bdd2-9105-4c2b-82c1-5d28bdf89588");
    private static readonly Regex DiagnosticPattern = new(
        @"^(?<file>.+)\((?<line>\d+),(?<column>\d+)\): error (?<code>SML\d+): (?<message>.+)$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static AsyncPackage? _package;
    private static ErrorListProvider? _errors;

    public static void Initialize(AsyncPackage package)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        _package = package;
        _errors = new ErrorListProvider(package)
        {
            ProviderName = "SMILE 2.0",
            ProviderGuid = new Guid("ccfbf4e8-5af8-465b-b374-6575580ed4da")
        };
    }

    public static IVsOutputWindowPane GetOutputPane()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow
            ?? throw new InvalidOperationException("Visual Studio Output window service is unavailable.");
        var paneGuid = OutputPaneGuid;
        outputWindow.CreatePane(ref paneGuid, "SMILE 2.0", 1, 1);
        outputWindow.GetPane(ref paneGuid, out var pane);
        return pane ?? throw new InvalidOperationException("Could not create the SMILE 2.0 Output pane.");
    }

    public static string? FindCompiler(string path)
    {
        var extensionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var bundled = Path.Combine(extensionDirectory, "Compiler", "smilec.exe");
        if (File.Exists(bundled))
            return bundled;

        var directoryPath = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
        var directory = directoryPath == null ? null : new DirectoryInfo(directoryPath);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "artifacts", "compiler", "smilec.exe");
            if (File.Exists(candidate))
                return candidate;
            directory = directory.Parent;
        }

        return null;
    }

    public static async Task<CompilerResult> RunAsync(string compilerPath, string sourcePath,
        string? outputPath, SmileGraphicsBackend graphicsBackend = SmileGraphicsBackend.Auto,
        bool vSync = true, bool emitDebugInformation = false)
    {
        var arguments = new StringBuilder().Append(Quote(sourcePath));
        if (!string.IsNullOrWhiteSpace(outputPath))
            arguments.Append(" -o ").Append(Quote(outputPath!));
        arguments.Append(" --graphics ").Append(graphicsBackend.ToString());
        arguments.Append(" --vsync ").Append(vSync ? "true" : "false");
        if (emitDebugInformation)
            arguments.Append(" --debug");

        return await RunCompilerAsync(compilerPath, sourcePath, arguments.ToString()).ConfigureAwait(false);
    }

    public static Task<CompilerResult> RunWebAsync(string compilerPath, string sourcePath, string outputDirectory)
    {
        var arguments = new StringBuilder().Append(Quote(sourcePath))
            .Append(" --target web --output-dir ").Append(Quote(outputDirectory));
        return RunCompilerAsync(compilerPath, sourcePath, arguments.ToString());
    }

    private static async Task<CompilerResult> RunCompilerAsync(string compilerPath, string sourcePath, string arguments)
    {
        var startInfo = new ProcessStartInfo(compilerPath)
        {
            Arguments = arguments,
            WorkingDirectory = Path.GetDirectoryName(sourcePath),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
            return new CompilerResult(2, "Could not start smilec.exe.\n");

        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await Task.Run(() => process.WaitForExit()).ConfigureAwait(false);
        return new CompilerResult(process.ExitCode,
            await standardOutput.ConfigureAwait(false) + await standardError.ConfigureAwait(false));
    }

    public static void ReportDiagnostics(string output)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (_errors == null || _package == null)
            return;

        _errors.Tasks.Clear();
        foreach (Match match in DiagnosticPattern.Matches(output.Replace("\r\n", "\n")))
        {
            var task = new ErrorTask
            {
                Category = TaskCategory.BuildCompile,
                ErrorCategory = TaskErrorCategory.Error,
                Document = match.Groups["file"].Value,
                Line = Math.Max(0, int.Parse(match.Groups["line"].Value) - 1),
                Column = Math.Max(0, int.Parse(match.Groups["column"].Value) - 1),
                Text = $"{match.Groups["code"].Value}: {match.Groups["message"].Value}"
            };
            task.Navigate += (_, _) => _errors.Navigate(task, Guid.Empty);
            _errors.Tasks.Add(task);
        }

        if (_errors.Tasks.Count != 0)
            _errors.Show();
    }

    public static string NormalizeOutput(string output) =>
        output.Replace("\r\n", "\n").Replace("\n", "\r\n");

    private static string Quote(string value) => "\"" + value + "\"";

    internal sealed class CompilerResult
    {
        public CompilerResult(int exitCode, string output)
        {
            ExitCode = exitCode;
            Output = output;
        }

        public int ExitCode { get; }
        public string Output { get; }
    }
}
