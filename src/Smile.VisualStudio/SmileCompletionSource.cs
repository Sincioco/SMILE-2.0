using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Smile.Language;

namespace Smile.VisualStudio;

[Export(typeof(IAsyncCompletionSourceProvider))]
[Name(nameof(SmileCompletionSourceProvider))]
[ContentType(SmileContentType.Name)]
[TextViewRole(PredefinedTextViewRoles.Editable)]
internal sealed class SmileCompletionSourceProvider : IAsyncCompletionSourceProvider
{
    [Import]
    internal ITextDocumentFactoryService TextDocumentFactory { get; set; } = null!;

    public IAsyncCompletionSource GetOrCreate(ITextView textView) =>
        textView.Properties.GetOrCreateSingletonProperty(() =>
            new SmileCompletionSource(textView.TextBuffer, GetFilePath(textView.TextBuffer), TextDocumentFactory));

    private string GetFilePath(ITextBuffer buffer) =>
        TextDocumentFactory.TryGetTextDocument(buffer, out var document) ? document.FilePath : string.Empty;
}

internal sealed class SmileCompletionSource : IAsyncCompletionSource
{
    private readonly string _filePath;
    private readonly SmileAnalysisCache _cache;
    private readonly ConcurrentDictionary<string, string> _descriptions =
        new(StringComparer.OrdinalIgnoreCase);

    public SmileCompletionSource(ITextBuffer buffer, string filePath, ITextDocumentFactoryService textDocumentFactory)
    {
        _filePath = filePath;
        _cache = buffer.Properties.GetOrCreateSingletonProperty(() => new SmileAnalysisCache(buffer, filePath, textDocumentFactory));
    }

    public CompletionStartData InitializeCompletion(
        CompletionTrigger trigger,
        SnapshotPoint triggerLocation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ShouldStart(trigger) || IsInCommentOrString(triggerLocation))
            return CompletionStartData.DoesNotParticipateInCompletion;

        var snapshot = triggerLocation.Snapshot;
        var line = snapshot.GetLineFromPosition(triggerLocation.Position);
        var start = triggerLocation.Position;
        while (start > line.Start.Position && IsIdentifierPart(snapshot[start - 1]))
            start--;

        return new CompletionStartData(
            CompletionParticipation.ProvidesItems,
            new SnapshotSpan(snapshot, start, triggerLocation.Position - start));
    }

    public Task<CompletionContext> GetCompletionContextAsync(
        IAsyncCompletionSession session,
        CompletionTrigger trigger,
        SnapshotPoint triggerLocation,
        SnapshotSpan applicableToSpan,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = triggerLocation.Snapshot;
        if (!_cache.TryGet(snapshot, out var analysis))
            analysis = SmileProjectWorkspace.Analyze(_filePath, snapshot.GetText(), _cache.ProjectPath);

        var completions = analysis.TryGetSyntaxTree(_filePath, out var syntaxTree)
            ? SmileCompletionService.GetCompletions(analysis, syntaxTree, triggerLocation.Position)
            : SmileCompletionService.GetCompletions(analysis, triggerLocation.Position);
        var items = ImmutableArray.CreateBuilder<CompletionItem>(completions.Count);
        foreach (var completion in completions)
        {
            _descriptions[completion.DisplayText] = completion.Description;
            items.Add(new CompletionItem(completion.DisplayText, this, ImageElement.Empty,
                ImmutableArray<CompletionFilter>.Empty, string.Empty, completion.InsertionText,
                completion.DisplayText, completion.DisplayText, completion.DisplayText,
                ImmutableArray<ImageElement>.Empty, default, applicableToSpan, false, false));
        }
        return Task.FromResult(new CompletionContext(items.ToImmutable()));
    }

    public Task<object> GetDescriptionAsync(
        IAsyncCompletionSession session,
        CompletionItem item,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var description = _descriptions.TryGetValue(item.DisplayText, out var value)
            ? value
            : item.DisplayText;
        return Task.FromResult<object>(description);
    }

    private static bool ShouldStart(CompletionTrigger trigger) =>
        trigger.Reason is CompletionTriggerReason.Invoke or
            CompletionTriggerReason.InvokeAndCommitIfUnique or
            CompletionTriggerReason.InvokeMatchingType ||
        trigger.Reason == CompletionTriggerReason.Insertion &&
        (IsIdentifierStart(trigger.Character) || trigger.Character is '.' or '(' or ',');

    internal static bool IsIdentifierPart(char value) => char.IsLetterOrDigit(value) || value == '_';

    internal static bool IsIdentifierStart(char value) => char.IsLetter(value) || value == '_';

    private static bool IsInCommentOrString(SnapshotPoint point)
    {
        var line = point.Snapshot.GetLineFromPosition(point.Position);
        var text = point.Snapshot.GetText(line.Start.Position, point.Position - line.Start.Position);
        var inString = false;
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '\'' && !inString)
                return true;
            if (text[index] != '"')
                continue;
            if (inString && index + 1 < text.Length && text[index + 1] == '"')
            {
                index++;
                continue;
            }
            inString = !inString;
        }
        return inString;
    }
}
