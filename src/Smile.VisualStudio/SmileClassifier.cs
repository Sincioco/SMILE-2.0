using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Smile.Language;

namespace Smile.VisualStudio;

internal static class SmileClassificationNames
{
    public const string BuiltInLibrarySymbol = "SMILE 2.0 Built-in Library Symbol";
}

internal static class SmileClassificationDefinitions
{
#pragma warning disable 649
    [Export(typeof(ClassificationTypeDefinition))]
    [Name(SmileClassificationNames.BuiltInLibrarySymbol)]
    [BaseDefinition(PredefinedClassificationTypeNames.Identifier)]
    internal static ClassificationTypeDefinition? BuiltInLibrarySymbol;
#pragma warning restore 649
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = SmileClassificationNames.BuiltInLibrarySymbol)]
[Name(SmileClassificationNames.BuiltInLibrarySymbol)]
[UserVisible(true)]
internal sealed class SmileBuiltInLibrarySymbolFormat : ClassificationFormatDefinition
{
    public SmileBuiltInLibrarySymbolFormat()
    {
        DisplayName = "SMILE 2.0 Built-in Module or Library";
        ForegroundColor = Color.FromRgb(43, 145, 175);
        ForegroundCustomizable = true;
    }
}

[Export(typeof(IClassifierProvider))]
[ContentType(SmileContentType.Name)]
internal sealed class SmileClassifierProvider : IClassifierProvider
{
    [Import]
    internal IClassificationTypeRegistryService ClassificationRegistry { get; set; } = null!;

    [Import]
    internal ITextDocumentFactoryService TextDocumentFactory { get; set; } = null!;

    public IClassifier GetClassifier(ITextBuffer textBuffer) =>
        textBuffer.Properties.GetOrCreateSingletonProperty(() =>
            new SmileClassifier(textBuffer, ClassificationRegistry, GetFilePath(textBuffer), TextDocumentFactory));

    private string GetFilePath(ITextBuffer buffer) =>
        TextDocumentFactory.TryGetTextDocument(buffer, out var document) ? document.FilePath : string.Empty;
}

internal sealed class SmileClassifier : IClassifier
{
    private readonly ITextBuffer _buffer;
    private readonly SmileAnalysisCache _cache;
    private readonly Dictionary<TokenClassification, IClassificationType> _classifications;

    public SmileClassifier(ITextBuffer buffer, IClassificationTypeRegistryService registry, string filePath,
        ITextDocumentFactoryService textDocumentFactory)
    {
        _buffer = buffer;
        _cache = buffer.Properties.GetOrCreateSingletonProperty(() => new SmileAnalysisCache(buffer, filePath, textDocumentFactory));
        _cache.AnalysisChanged += AnalysisChanged;
        _classifications = new Dictionary<TokenClassification, IClassificationType>
        {
            [TokenClassification.Keyword] = registry.GetClassificationType(PredefinedClassificationTypeNames.Keyword),
            [TokenClassification.String] = registry.GetClassificationType(PredefinedClassificationTypeNames.String),
            [TokenClassification.Comment] = registry.GetClassificationType(PredefinedClassificationTypeNames.Comment),
            [TokenClassification.Number] = registry.GetClassificationType(PredefinedClassificationTypeNames.Number),
            [TokenClassification.Identifier] = registry.GetClassificationType(PredefinedClassificationTypeNames.Identifier),
            [TokenClassification.BuiltInLibrary] = registry.GetClassificationType(
                SmileClassificationNames.BuiltInLibrarySymbol),
            [TokenClassification.Operator] = registry.GetClassificationType(PredefinedClassificationTypeNames.Operator)
        };
    }

    public event EventHandler<ClassificationChangedEventArgs>? ClassificationChanged;

    public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
    {
        var result = new List<ClassificationSpan>();
        if (!_cache.TryGet(span.Snapshot, out var analysis))
            return result;

        var syntaxTree = analysis.TryGetSyntaxTree(_cache.FilePath, out var activeTree)
            ? activeTree
            : analysis.SyntaxTree;
        for (var tokenIndex = 0; tokenIndex < syntaxTree.Tokens.Count; tokenIndex++)
        {
            var token = syntaxTree.Tokens[tokenIndex];
            if (token.Span.Length == 0 || token.Span.Start >= span.Snapshot.Length)
                continue;

            var length = Math.Min(token.Span.Length, span.Snapshot.Length - token.Span.Start);
            var tokenSpan = new SnapshotSpan(span.Snapshot, token.Span.Start, length);
            if (!tokenSpan.IntersectsWith(span))
                continue;

            var classification = Classify(analysis, syntaxTree, token, tokenIndex);
            if (classification != null)
                result.Add(new ClassificationSpan(tokenSpan, _classifications[classification.Value]));
        }

        return result;
    }

    private void AnalysisChanged(object sender, EventArgs e)
    {
        var snapshot = _buffer.CurrentSnapshot;
        ClassificationChanged?.Invoke(this, new ClassificationChangedEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
    }

    private static TokenClassification? Classify(SmileAnalysisResult analysis, SyntaxTree syntaxTree,
        SyntaxToken token, int tokenIndex)
    {
        var kind = token.Kind;
        if (SyntaxFacts.IsKeyword(kind) || SyntaxFacts.IsBuiltInConstant(kind))
            return TokenClassification.Keyword;
        if (kind == SyntaxKind.IdentifierToken &&
            SmileSymbolService.TryResolveToken(analysis, syntaxTree, token, tokenIndex, out var symbol) &&
            analysis.DependencyContext.TryGetProviderDescriptor(symbol.ProviderIdentity, out var provider) &&
            provider.IsBuiltIn)
        {
            return TokenClassification.BuiltInLibrary;
        }
        return kind switch
        {
            SyntaxKind.StringToken => TokenClassification.String,
            SyntaxKind.CommentToken => TokenClassification.Comment,
            SyntaxKind.NumberToken => TokenClassification.Number,
            SyntaxKind.IdentifierToken => TokenClassification.Identifier,
            SyntaxKind.PlusToken or SyntaxKind.MinusToken or SyntaxKind.StarToken or SyntaxKind.SlashToken or SyntaxKind.CommaToken or
                SyntaxKind.EqualsToken or SyntaxKind.NotEqualsToken or
                SyntaxKind.LessToken or SyntaxKind.GreaterToken or SyntaxKind.LessOrEqualsToken or SyntaxKind.GreaterOrEqualsToken or
                SyntaxKind.OpenParenthesisToken or SyntaxKind.CloseParenthesisToken or SyntaxKind.OpenBracketToken or
                SyntaxKind.CloseBracketToken or SyntaxKind.SemicolonToken => TokenClassification.Operator,
            _ => null
        };
    }

    private enum TokenClassification
    {
        Keyword,
        String,
        Comment,
        Number,
        Identifier,
        BuiltInLibrary,
        Operator
    }
}
