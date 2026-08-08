using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Smile.Language;

namespace Smile.VisualStudio;

[Export(typeof(ITaggerProvider))]
[ContentType(SmileContentType.Name)]
[TagType(typeof(ErrorTag))]
internal sealed class SmileDiagnosticTaggerProvider : ITaggerProvider
{
    [Import]
    internal ITextDocumentFactoryService TextDocumentFactory { get; set; } = null!;

    public ITagger<T>? CreateTagger<T>(ITextBuffer buffer) where T : ITag =>
        buffer.Properties.GetOrCreateSingletonProperty(() =>
            new SmileDiagnosticTagger(buffer, GetFilePath(buffer))) as ITagger<T>;

    private string GetFilePath(ITextBuffer buffer) =>
        TextDocumentFactory.TryGetTextDocument(buffer, out var document) ? document.FilePath : string.Empty;
}

internal sealed class SmileDiagnosticTagger : ITagger<ErrorTag>
{
    private readonly ITextBuffer _buffer;
    private readonly SmileAnalysisCache _cache;

    public SmileDiagnosticTagger(ITextBuffer buffer, string filePath)
    {
        _buffer = buffer;
        _cache = buffer.Properties.GetOrCreateSingletonProperty(() => new SmileAnalysisCache(buffer, filePath));
        _cache.AnalysisChanged += AnalysisChanged;
    }

    public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

    public IEnumerable<ITagSpan<ErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
    {
        if (spans.Count == 0 || !_cache.TryGet(spans[0].Snapshot, out var analysis))
            yield break;

        var snapshot = spans[0].Snapshot;
        foreach (var diagnostic in analysis.Diagnostics)
        {
            if (diagnostic.Span.Start > snapshot.Length)
                continue;

            var start = Math.Min(diagnostic.Span.Start, Math.Max(0, snapshot.Length - 1));
            var available = snapshot.Length - start;
            var length = Math.Min(Math.Max(1, diagnostic.Span.Length), available);
            if (length <= 0)
                continue;

            var diagnosticSpan = new SnapshotSpan(snapshot, start, length);
            if (!spans.IntersectsWith(new NormalizedSnapshotSpanCollection(diagnosticSpan)))
                continue;

            var errorType = diagnostic.Code.StartsWith("SML2", StringComparison.Ordinal)
                ? PredefinedErrorTypeNames.SyntaxError
                : PredefinedErrorTypeNames.CompilerError;
            yield return new TagSpan<ErrorTag>(diagnosticSpan,
                new ErrorTag(errorType, $"{diagnostic.Code}: {diagnostic.Message}"));
        }
    }

    private void AnalysisChanged(object sender, EventArgs e)
    {
        var snapshot = _buffer.CurrentSnapshot;
        TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
    }
}
