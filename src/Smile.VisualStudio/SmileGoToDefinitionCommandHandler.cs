using System;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Smile.Language;

namespace Smile.VisualStudio;

[Export(typeof(ICommandHandler))]
[Name(nameof(SmileGoToDefinitionCommandHandler))]
[Order(Before = "default")]
[ContentType(SmileContentType.Name)]
[TextViewRole(PredefinedTextViewRoles.Editable)]
internal sealed class SmileGoToDefinitionCommandHandler : IChainedCommandHandler<GoToDefinitionCommandArgs>
{
    [Import]
    internal ITextDocumentFactoryService TextDocumentFactory { get; set; } = null!;

    public string DisplayName => "SMILE Go To Definition";

    public CommandState GetCommandState(GoToDefinitionCommandArgs args, Func<CommandState> nextCommandHandler) =>
        CommandState.Available;

    public void ExecuteCommand(GoToDefinitionCommandArgs args, Action nextCommandHandler,
        CommandExecutionContext executionContext)
    {
        try
        {
            var point = args.TextView.Caret.Position.Point.GetPoint(args.SubjectBuffer,
                PositionAffinity.Predecessor);
            if (!point.HasValue || !TextDocumentFactory.TryGetTextDocument(args.SubjectBuffer, out var document))
            {
                nextCommandHandler();
                return;
            }

            var snapshot = args.SubjectBuffer.CurrentSnapshot;
            var cache = args.SubjectBuffer.Properties.GetOrCreateSingletonProperty(() =>
                new SmileAnalysisCache(args.SubjectBuffer, document.FilePath, TextDocumentFactory));
            if (!cache.TryGet(snapshot, out var analysis))
                analysis = SmileProjectWorkspace.Analyze(document.FilePath, snapshot.GetText(), cache.ProjectPath);
            var syntaxTree = analysis.TryGetSyntaxTree(document.FilePath, out var currentTree)
                ? currentTree : analysis.SyntaxTree;
            if (!SmileSymbolService.TryResolve(analysis, syntaxTree, point.Value.Position, out var symbol))
            {
                nextCommandHandler();
                return;
            }

            var location = symbol.DeclarationLocation;
            if (location == null || string.IsNullOrWhiteSpace(location.FilePath) || !File.Exists(location.FilePath))
            {
                SmileNavigationService.SetStatus(
                    "SMILE definition found, but source is not available for this library package.");
                return;
            }

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await SmileNavigationService.NavigateAsync(location);
                }
                catch (Exception exception)
                {
                    ActivityLog.LogError(nameof(SmileGoToDefinitionCommandHandler), exception.ToString());
                    SmileNavigationService.SetStatus("SMILE could not open the definition source.");
                }
            }).FileAndForget("Smile/GoToDefinition");
        }
        catch (Exception exception)
        {
            ActivityLog.LogError(nameof(SmileGoToDefinitionCommandHandler), exception.ToString());
            nextCommandHandler();
        }
    }
}

internal static class SmileNavigationService
{
    public static async System.Threading.Tasks.Task NavigateAsync(SourceLocation location)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, location.FilePath,
            VSConstants.LOGVIEWID_TextView, out _, out _, out var windowFrame);
        ErrorHandler.ThrowOnFailure(windowFrame.Show());
        var view = VsShellUtilities.GetTextView(windowFrame);
        if (view == null)
            throw new InvalidOperationException("Visual Studio did not provide a text view for the definition.");

        location.Source.GetLineColumn(location.Span.End, out var endLine, out var endColumn);
        var startLine = Math.Max(0, location.Line - 1);
        var startColumn = Math.Max(0, location.Column - 1);
        var finishLine = Math.Max(startLine, endLine - 1);
        var finishColumn = finishLine == startLine ? Math.Max(startColumn, endColumn - 1) : Math.Max(0, endColumn - 1);
        ErrorHandler.ThrowOnFailure(view.SetSelection(startLine, startColumn, finishLine, finishColumn));
        ErrorHandler.ThrowOnFailure(view.EnsureSpanVisible(new Microsoft.VisualStudio.TextManager.Interop.TextSpan
        {
            iStartLine = startLine,
            iStartIndex = startColumn,
            iEndLine = finishLine,
            iEndIndex = finishColumn
        }));
        ErrorHandler.ThrowOnFailure(view.CenterLines(startLine, 1));
        view.SendExplicitFocus();
    }

    public static void SetStatus(string message)
    {
        ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var statusBar = Package.GetGlobalService(typeof(SVsStatusbar)) as IVsStatusbar;
            statusBar?.SetText(message);
        }).FileAndForget("Smile/Status");
    }
}
