using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Smile.Language;

namespace Smile.VisualStudio;

[Export(typeof(IAsyncQuickInfoSourceProvider))]
[Name(nameof(SmileQuickInfoSourceProvider))]
[ContentType(SmileContentType.Name)]
internal sealed class SmileQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
{
    [Import]
    internal ITextDocumentFactoryService TextDocumentFactory { get; set; } = null!;

    public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer) =>
        textBuffer.Properties.GetOrCreateSingletonProperty(() => new SmileQuickInfoSource(textBuffer,
            TextDocumentFactory.TryGetTextDocument(textBuffer, out var document) ? document.FilePath : string.Empty,
            TextDocumentFactory));
}

internal sealed class SmileQuickInfoSource : IAsyncQuickInfoSource
{
    private readonly ITextBuffer _buffer;
    private readonly string _filePath;
    private readonly SmileAnalysisCache _cache;

    public SmileQuickInfoSource(ITextBuffer buffer, string filePath,
        ITextDocumentFactoryService textDocumentFactory)
    {
        _buffer = buffer;
        _filePath = filePath;
        _cache = buffer.Properties.GetOrCreateSingletonProperty(() =>
            new SmileAnalysisCache(buffer, filePath, textDocumentFactory));
    }

    public async Task<QuickInfoItem?> GetQuickInfoItemAsync(IAsyncQuickInfoSession session,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var snapshot = _buffer.CurrentSnapshot;
            var triggerPoint = session.GetTriggerPoint(snapshot);
            if (!triggerPoint.HasValue)
                return null;

            if (!_cache.TryGet(snapshot, out var analysis))
                analysis = SmileProjectWorkspace.Analyze(_filePath, snapshot.GetText(), _cache.ProjectPath);
            cancellationToken.ThrowIfCancellationRequested();

            var syntaxTree = analysis.TryGetSyntaxTree(_filePath, out var currentTree)
                ? currentTree : analysis.SyntaxTree;
            if (!SmileSymbolService.TryResolve(analysis, syntaxTree, triggerPoint.Value.Position, out var symbol))
                return null;

            var span = symbol.ReferenceSpan;
            if (span.Start < 0 || span.End > snapshot.Length || span.Length == 0)
                return null;
            var trackingSpan = snapshot.CreateTrackingSpan(span.Start, span.Length, SpanTrackingMode.EdgeInclusive);
            var runtimeValue = await TryGetRuntimeValueAsync(symbol, cancellationToken);
            var content = BuildContent(SmileSymbolDisplayService.Present(symbol, analysis.DependencyContext),
                runtimeValue);
            return new QuickInfoItem(trackingSpan, content);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            Microsoft.VisualStudio.Shell.ActivityLog.LogError(nameof(SmileQuickInfoSource), exception.ToString());
            return null;
        }
    }

    private static async Task<RuntimeValue?> TryGetRuntimeValueAsync(SmileResolvedSymbol symbol,
        CancellationToken cancellationToken)
    {
        if (symbol.Kind is not SmileResolvedSymbolKind.Variable and not SmileResolvedSymbolKind.Constant and
            not SmileResolvedSymbolKind.Array and not SmileResolvedSymbolKind.Parameter and
            not SmileResolvedSymbolKind.Local)
        {
            return null;
        }

        try
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var dte = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.GetService(typeof(SDTE)) as DTE;
            var debugger = dte?.Debugger;
            if (debugger == null || debugger.CurrentMode != dbgDebugMode.dbgBreakMode)
                return null;

            var expression = debugger.GetExpression(symbol.Name, UseAutoExpandRules: true, Timeout: 100);
            if (expression == null || !expression.IsValidValue || string.IsNullOrWhiteSpace(expression.Value))
                return null;

            return new RuntimeValue(expression.Value, expression.Type);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static string BuildContent(SmileSymbolPresentation presentation, RuntimeValue? runtimeValue)
    {
        var builder = new StringBuilder(presentation.Signature);
        if (runtimeValue != null)
        {
            builder.AppendLine().AppendLine().Append("Current Value").AppendLine()
                .Append(runtimeValue.Value);
            if (!string.IsNullOrWhiteSpace(runtimeValue.Type))
                builder.Append(" (").Append(runtimeValue.Type).Append(')');
        }
        if (!string.IsNullOrWhiteSpace(presentation.Alias))
            builder.AppendLine().Append("Imported as ").Append(presentation.Alias);
        if (!string.IsNullOrWhiteSpace(presentation.Summary))
            builder.AppendLine().AppendLine().Append(presentation.Summary);

        var documentedParameters = presentation.Parameters
            .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Description)).ToArray();
        if (documentedParameters.Length != 0)
        {
            builder.AppendLine().AppendLine().Append("Parameters");
            foreach (var parameter in documentedParameters)
                builder.AppendLine().Append(parameter.Signature).Append(" — ").Append(parameter.Description);
        }
        if (!string.IsNullOrWhiteSpace(presentation.Returns))
            builder.AppendLine().AppendLine().Append("Returns").AppendLine().Append(presentation.Returns);
        if (!string.IsNullOrWhiteSpace(presentation.Remarks))
            builder.AppendLine().AppendLine().Append("Remarks").AppendLine().Append(presentation.Remarks);
        if (!string.IsNullOrWhiteSpace(presentation.Capability))
            builder.AppendLine().AppendLine().Append(presentation.Capability);
        if (!string.IsNullOrWhiteSpace(presentation.Provider) || !string.IsNullOrWhiteSpace(presentation.SourcePath))
        {
            builder.AppendLine().AppendLine().Append("Defined in");
            if (!string.IsNullOrWhiteSpace(presentation.Provider))
                builder.AppendLine().Append(presentation.Provider);
            if (!string.IsNullOrWhiteSpace(presentation.SourcePath))
                builder.AppendLine().Append(presentation.SourcePath);
        }
        return builder.ToString();
    }

    private sealed class RuntimeValue
    {
        public RuntimeValue(string value, string type)
        {
            Value = value;
            Type = type;
        }

        public string Value { get; }
        public string Type { get; }
    }

    public void Dispose()
    {
    }
}
