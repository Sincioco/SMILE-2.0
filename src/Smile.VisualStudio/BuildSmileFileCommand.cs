using System;
using System.ComponentModel.Design;
using System.IO;
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
        var compilerPath = SmileBuildService.FindCompiler(sourcePath);
        if (compilerPath == null)
        {
            pane.OutputStringThreadSafe("smilec.exe was not found in the extension or repository artifacts.\r\n");
            return;
        }

        pane.OutputStringThreadSafe($"> \"{compilerPath}\" \"{sourcePath}\"\r\n");
        var result = await SmileBuildService.RunAsync(compilerPath, sourcePath, null);
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        if (!string.IsNullOrEmpty(result.Output))
            pane.OutputStringThreadSafe(SmileBuildService.NormalizeOutput(result.Output));
        SmileBuildService.ReportDiagnostics(result.Output);
        pane.OutputStringThreadSafe($"smilec exit code: {result.ExitCode}\r\n");
    }

    private async Task<IVsOutputWindowPane> GetOutputPaneAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        return SmileBuildService.GetOutputPane();
    }
}
