using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Smile.Language;

public sealed class SyntaxTree
{
    internal SyntaxTree(SourceText source, CompilationUnitSyntax root, IReadOnlyList<SyntaxToken> tokens, bool isStartup,
        string? providerIdentity = null)
    {
        Source = source;
        Root = root;
        Tokens = tokens;
        IsStartup = isStartup;
        ProviderIdentity = providerIdentity ?? string.Empty;
    }

    public SourceText Source { get; }
    public CompilationUnitSyntax Root { get; }
    public IReadOnlyList<SyntaxToken> Tokens { get; }
    public bool IsStartup { get; }
    public string ProviderIdentity { get; }
}

public sealed class SmileSourceDocument
{
    public SmileSourceDocument(string text, string? filePath = null, bool isStartup = false, bool isMissing = false,
        string? providerIdentity = null)
    {
        Text = text ?? string.Empty;
        FilePath = NormalizePath(filePath);
        IsStartup = isStartup;
        IsMissing = isMissing;
        ProviderIdentity = providerIdentity ?? string.Empty;
    }

    public string Text { get; }
    public string FilePath { get; }
    public bool IsStartup { get; }
    public bool IsMissing { get; }
    public string ProviderIdentity { get; }

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

    internal SmileAnalysisResult(IReadOnlyList<SyntaxTree> syntaxTrees, SyntaxTree primarySyntaxTree,
        IReadOnlyList<SyntaxTree> boundSyntaxTrees, SyntaxTree boundPrimarySyntaxTree,
        SmileCompilationKind compilationKind, SemanticModel semanticModel, IReadOnlyList<Diagnostic> diagnostics,
        SmileCompilationDependencyContext dependencyContext)
    {
        SyntaxTrees = syntaxTrees;
        SyntaxTree = primarySyntaxTree;
        BoundSyntaxTrees = boundSyntaxTrees;
        BoundSyntaxTree = boundPrimarySyntaxTree;
        CompilationKind = compilationKind;
        SemanticModel = semanticModel;
        Diagnostics = diagnostics;
        DependencyContext = dependencyContext;
        _syntaxTreesByPath = new Dictionary<string, SyntaxTree>(StringComparer.OrdinalIgnoreCase);
        foreach (var tree in syntaxTrees)
        {
            if (!string.IsNullOrEmpty(tree.Source.FilePath))
                _syntaxTreesByPath[tree.Source.FilePath] = tree;
        }
    }

    public SyntaxTree SyntaxTree { get; }
    public IReadOnlyList<SyntaxTree> SyntaxTrees { get; }
    public IReadOnlyList<SyntaxTree> BoundSyntaxTrees { get; }
    public SyntaxTree BoundSyntaxTree { get; }
    public SmileCompilationKind CompilationKind { get; }
    public SemanticModel SemanticModel { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public SmileCompilationDependencyContext DependencyContext { get; }
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
        => Analyze(sources, SmileCompilationKind.Program);

    public static SmileAnalysisResult Analyze(IReadOnlyList<SmileSourceDocument> sources, SmileCompilationKind compilationKind)
        => Analyze(sources, compilationKind, SmileCompilationDependencyContext.Unrestricted);

    public static SmileAnalysisResult Analyze(IReadOnlyList<SmileSourceDocument> sources,
        SmileCompilationKind compilationKind, SmileCompilationDependencyContext dependencyContext)
    {
        if (sources == null)
            throw new ArgumentNullException(nameof(sources));
        if (sources.Count == 0)
            throw new ArgumentException("A SMILE compilation requires at least one source document.", nameof(sources));
        if (dependencyContext == null)
            throw new ArgumentNullException(nameof(dependencyContext));

        var startupCount = sources.Count(source => source != null && source.IsStartup);
        var requiredStartupCount = compilationKind == SmileCompilationKind.Program ? 1 : 0;
        if (startupCount != requiredStartupCount)
            throw new ArgumentException(compilationKind == SmileCompilationKind.Program
                ? $"A SMILE compilation requires exactly one startup source; found {startupCount}."
                : $"A library SMILE compilation requires no startup sources; found {startupCount}.", nameof(sources));

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
            var tree = new SyntaxTree(source, root, tokens, document.IsStartup, document.ProviderIdentity);
            syntaxTrees.Add(tree);
            parserDiagnostics.AddRange(parser.Diagnostics);
            if (document.IsMissing)
                parserDiagnostics.Add(new Diagnostic("SML0001", DiagnosticSeverity.Error,
                    $"Project source file was not found: {document.FilePath}", source, new TextSpan(0, 0)));
        }

        var moduleProcessing = new ModuleProcessor(syntaxTrees, compilationKind, dependencyContext).Process();
        var boundStartupTree = moduleProcessing.BoundTrees.Single(tree => tree.IsStartup);
        var semanticAnalyzer = new SemanticAnalyzer(moduleProcessing.BoundTrees, boundStartupTree);
        var semanticModel = semanticAnalyzer.Analyze();
        moduleProcessing.Link(semanticModel);

        var diagnostics = new List<Diagnostic>();
        diagnostics.AddRange(parserDiagnostics);
        diagnostics.AddRange(moduleProcessing.Diagnostics);
        diagnostics.AddRange(semanticAnalyzer.Diagnostics);

        var sourceOrder = syntaxTrees.Select((tree, index) => new { tree.Source, index })
            .ToDictionary(item => item.Source, item => item.index);
        var orderedDiagnostics = diagnostics
            .OrderBy(diagnostic => sourceOrder.TryGetValue(diagnostic.Source, out var ordinal) ? ordinal : -1)
            .ThenBy(diagnostic => diagnostic.Span.Start)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ToArray();

        var primaryTree = compilationKind == SmileCompilationKind.Program
            ? syntaxTrees.Single(tree => tree.IsStartup)
            : syntaxTrees[0];
        return new SmileAnalysisResult(syntaxTrees.ToArray(), primaryTree,
            moduleProcessing.BoundTrees, boundStartupTree, compilationKind, semanticModel, orderedDiagnostics,
            dependencyContext);
    }

    public static SmileAnalysisResult AnalyzeWithProjectDiagnostic(IReadOnlyList<SmileSourceDocument> sources,
        SmileCompilationKind compilationKind, SmileProjectDiagnostic projectDiagnostic)
    {
        if (projectDiagnostic == null) throw new ArgumentNullException(nameof(projectDiagnostic));
        var analysis = Analyze(sources, compilationKind);
        var source = analysis.SyntaxTrees.FirstOrDefault(tree => string.Equals(tree.Source.FilePath,
                         projectDiagnostic.FilePath, StringComparison.OrdinalIgnoreCase))?.Source
                     ?? new SourceText(string.Empty, projectDiagnostic.FilePath);
        var diagnostics = analysis.Diagnostics.Concat(new[]
        {
            new Diagnostic(projectDiagnostic.Code, DiagnosticSeverity.Error, projectDiagnostic.Message,
                source, new TextSpan(0, 0))
        }).OrderBy(diagnostic => diagnostic.FilePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(diagnostic => diagnostic.Span.Start)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ToArray();
        return new SmileAnalysisResult(analysis.SyntaxTrees, analysis.SyntaxTree,
            analysis.BoundSyntaxTrees, analysis.BoundSyntaxTree, analysis.CompilationKind,
            analysis.SemanticModel, diagnostics, analysis.DependencyContext);
    }
}
