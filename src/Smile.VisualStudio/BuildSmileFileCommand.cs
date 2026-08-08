using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Smile.VisualStudio;

internal sealed class BuildSmileFileCommand
{
    private const int CommandId = 0x0100;
    private static readonly Guid CommandSet = new("c2d95dd7-3995-4f84-a78b-e67e88c5a31f");
    private static readonly Guid OutputPaneGuid = new("9315bdd2-9105-4c2b-82c1-5d28bdf89588");

    private readonly AsyncPackage _package;

    private BuildSmileFileCommand(AsyncPackage package, OleMenuCommandService commandService)
    {
        _package = package;
        var command = new MenuCommand(Execute,
            new CommandID(CommandSet, CommandId));
        commandService.AddCommand(command);
    }

    private void Execute(object sender, EventArgs e) =>
        _package.JoinableTaskFactory.RunAsync(ExecuteAsync).FileAndForget("Smile/BuildFile");

    public static async Task InitializeAsync(AsyncPackage package)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
        var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
        if (commandService != null)
            _ = new BuildSmileFileCommand(package, commandService);
    }

    private async Task ExecuteAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var pane = await GetOutputPaneAsync();
        pane.Clear();
        pane.Activate();

        var dte = await _package.GetServiceAsync(typeof(SDTE)) as DTE2;
        Document? document = dte?.ActiveDocument;
        if (document == null || string.IsNullOrWhiteSpace(document.FullName) || !document.FullName.EndsWith(".smile", StringComparison.OrdinalIgnoreCase))
        {
            pane.OutputStringThreadSafe("Open an active .smile file before running Build SMILE File.\r\n");
            return;
        }

        var sourcePath = document.FullName;
        document.Save();
        var compilerPath = FindCompiler(sourcePath);
        if (compilerPath == null)
        {
            pane.OutputStringThreadSafe("smilec.exe was not found in the extension or repository artifacts.\r\n");
            return;
        }

        pane.OutputStringThreadSafe($"> \"{compilerPath}\" \"{sourcePath}\"\r\n");
        var result = await RunCompilerAsync(compilerPath, sourcePath);
        if (!string.IsNullOrEmpty(result.Output))
            pane.OutputStringThreadSafe(result.Output.Replace("\n", "\r\n"));
        pane.OutputStringThreadSafe($"smilec exit code: {result.ExitCode}\r\n");
    }

    private async Task<IVsOutputWindowPane> GetOutputPaneAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var service = await _package.GetServiceAsync(typeof(SVsOutputWindow));
        if (service is not IVsOutputWindow outputWindow)
            throw new InvalidOperationException("Visual Studio Output window service is unavailable.");
        var paneGuid = OutputPaneGuid;
        outputWindow.CreatePane(ref paneGuid, "SMILE 2.0", 1, 1);
        outputWindow.GetPane(ref paneGuid, out var pane);
        return pane ?? throw new InvalidOperationException("Could not create the SMILE 2.0 Output pane.");
    }

    private static string? FindCompiler(string sourcePath)
    {
        var extensionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var bundled = Path.Combine(extensionDirectory, "Compiler", "smilec.exe");
        if (File.Exists(bundled))
            return bundled;

        var directory = new DirectoryInfo(Path.GetDirectoryName(sourcePath)!);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "artifacts", "compiler", "smilec.exe");
            if (File.Exists(candidate))
                return candidate;
            directory = directory.Parent;
        }

        return null;
    }

    private static async Task<CompilerResult> RunCompilerAsync(string compilerPath, string sourcePath)
    {
        var startInfo = new ProcessStartInfo(compilerPath)
        {
            Arguments = Quote(sourcePath),
            WorkingDirectory = Path.GetDirectoryName(sourcePath),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        if (process == null)
            return new CompilerResult(2, "Could not start smilec.exe.\n");

        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await Task.Run(() => process.WaitForExit()).ConfigureAwait(false);
        var output = new StringBuilder();
        output.Append(await standardOutput.ConfigureAwait(false));
        output.Append(await standardError.ConfigureAwait(false));
        return new CompilerResult(process.ExitCode, output.ToString());
    }

    private static string Quote(string value) => "\"" + value + "\"";

    private sealed class CompilerResult
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
