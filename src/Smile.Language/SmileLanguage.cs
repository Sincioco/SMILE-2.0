using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Smile.Language;

public sealed class SyntaxTree
{
    internal SyntaxTree(SourceText source, CompilationUnitSyntax root, IReadOnlyList<SyntaxToken> tokens, bool isStartup)
    {
        Source = source;
        Root = root;
        Tokens = tokens;
        IsStartup = isStartup;
    }

    public SourceText Source { get; }
    public CompilationUnitSyntax Root { get; }
    public IReadOnlyList<SyntaxToken> Tokens { get; }
    public bool IsStartup { get; }
}

public sealed class SmileSourceDocument
{
    public SmileSourceDocument(string text, string? filePath = null, bool isStartup = false, bool isMissing = false)
    {
        Text = text ?? string.Empty;
        FilePath = NormalizePath(filePath);
        IsStartup = isStartup;
        IsMissing = isMissing;
    }

    public string Text { get; }
    public string FilePath { get; }
    public bool IsStartup { get; }
    public bool IsMissing { get; }

    internal static string NormalizePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return string.Empty;
        try
        {
            return Path.GetFullPath(filePath);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return filePath!;
        }
    }
}

public sealed class SmileAnalysisResult
{
    private readonly Dictionary<string, SyntaxTree> _syntaxTreesByPath;

    internal SmileAnalysisResult(IReadOnlyList<SyntaxTree> syntaxTrees, SyntaxTree startupSyntaxTree,
        SemanticModel semanticModel, IReadOnlyList<Diagnostic> diagnostics)
    {
        SyntaxTrees = syntaxTrees;
        SyntaxTree = startupSyntaxTree;
        SemanticModel = semanticModel;
        Diagnostics = diagnostics;
        _syntaxTreesByPath = new Dictionary<string, SyntaxTree>(StringComparer.OrdinalIgnoreCase);
        foreach (var tree in syntaxTrees)
        {
            if (!string.IsNullOrEmpty(tree.Source.FilePath))
                _syntaxTreesByPath[tree.Source.FilePath] = tree;
        }
    }

    public SyntaxTree SyntaxTree { get; }
    public IReadOnlyList<SyntaxTree> SyntaxTrees { get; }
    public SemanticModel SemanticModel { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public IReadOnlyList<SyntaxToken> Tokens => SyntaxTree.Tokens;
    public bool TryGetSyntaxTree(string? filePath, out SyntaxTree syntaxTree)
    {
        var normalizedPath = SmileSourceDocument.NormalizePath(filePath);
        if (!string.IsNullOrEmpty(normalizedPath) && _syntaxTreesByPath.TryGetValue(normalizedPath, out syntaxTree!))
            return true;
        syntaxTree = null!;
        return false;
    }

    public SyntaxTree GetSyntaxTree(string filePath) =>
        TryGetSyntaxTree(filePath, out var syntaxTree)
            ? syntaxTree
            : throw new ArgumentException($"Source file '{filePath}' is not part of this compilation.", nameof(filePath));

    public bool HasErrors
    {
        get
        {
            foreach (var diagnostic in Diagnostics)
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                    return true;
            }
            return false;
        }
    }
}

public static class SmileLanguage
{
    public static SmileAnalysisResult Analyze(string sourceText, string? filePath = null)
        => Analyze(new[] { new SmileSourceDocument(sourceText, filePath, isStartup: true) });

    public static SmileAnalysisResult Analyze(IReadOnlyList<SmileSourceDocument> sources)
    {
        if (sources == null)
            throw new ArgumentNullException(nameof(sources));
        if (sources.Count == 0)
            throw new ArgumentException("A SMILE compilation requires at least one source document.", nameof(sources));

        var startupCount = sources.Count(source => source != null && source.IsStartup);
        if (startupCount != 1)
            throw new ArgumentException($"A SMILE compilation requires exactly one startup source; found {startupCount}.", nameof(sources));

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var syntaxTrees = new List<SyntaxTree>(sources.Count);
        var parserDiagnostics = new List<Diagnostic>();
        foreach (var document in sources)
        {
            if (document == null)
                throw new ArgumentException("A SMILE compilation cannot contain a null source document.", nameof(sources));
            var identity = document.FilePath;
            if (!paths.Add(identity))
                throw new ArgumentException($"Duplicate SMILE source path '{(string.IsNullOrEmpty(identity) ? "<source>" : identity)}'.", nameof(sources));

            var source = new SourceText(document.Text, document.FilePath);
            var lexer = new Lexer(source);
            var tokens = lexer.Lex();
            var parser = new Parser(source, tokens, lexer.Diagnostics);
            var root = parser.ParseCompilationUnit();
            var tree = new SyntaxTree(source, root, tokens, document.IsStartup);
            syntaxTrees.Add(tree);
            parserDiagnostics.AddRange(parser.Diagnostics);
            if (document.IsMissing)
                parserDiagnostics.Add(new Diagnostic("SML0001", DiagnosticSeverity.Error,
                    $"Project source file was not found: {document.FilePath}", source, new TextSpan(0, 0)));
        }

        var startupTree = syntaxTrees.Single(tree => tree.IsStartup);
        var semanticAnalyzer = new SemanticAnalyzer(syntaxTrees, startupTree);
        var semanticModel = semanticAnalyzer.Analyze();

        var diagnostics = new List<Diagnostic>();
        diagnostics.AddRange(parserDiagnostics);
        diagnostics.AddRange(semanticAnalyzer.Diagnostics);

        var sourceOrder = syntaxTrees.Select((tree, index) => new { tree.Source, index })
            .ToDictionary(item => item.Source, item => item.index);
        var orderedDiagnostics = diagnostics
            .OrderBy(diagnostic => sourceOrder[diagnostic.Source])
            .ThenBy(diagnostic => diagnostic.Span.Start)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ToArray();

        return new SmileAnalysisResult(syntaxTrees.ToArray(), startupTree, semanticModel, orderedDiagnostics);
    }
}
