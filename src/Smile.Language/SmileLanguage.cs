using System.Collections.Generic;

namespace Smile.Language;

public sealed class SyntaxTree
{
    internal SyntaxTree(SourceText source, CompilationUnitSyntax root, IReadOnlyList<SyntaxToken> tokens)
    {
        Source = source;
        Root = root;
        Tokens = tokens;
    }

    public SourceText Source { get; }
    public CompilationUnitSyntax Root { get; }
    public IReadOnlyList<SyntaxToken> Tokens { get; }
}

public sealed class SmileAnalysisResult
{
    internal SmileAnalysisResult(SyntaxTree syntaxTree, SemanticModel semanticModel, IReadOnlyList<Diagnostic> diagnostics)
    {
        SyntaxTree = syntaxTree;
        SemanticModel = semanticModel;
        Diagnostics = diagnostics;
    }

    public SyntaxTree SyntaxTree { get; }
    public SemanticModel SemanticModel { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public IReadOnlyList<SyntaxToken> Tokens => SyntaxTree.Tokens;
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
    {
        var source = new SourceText(sourceText, filePath);
        var lexer = new Lexer(source);
        var tokens = lexer.Lex();
        var parser = new Parser(source, tokens, lexer.Diagnostics);
        var root = parser.ParseCompilationUnit();

        var semanticAnalyzer = new SemanticAnalyzer(source);
        var semanticModel = semanticAnalyzer.Analyze(root);

        var diagnostics = new List<Diagnostic>();
        diagnostics.AddRange(parser.Diagnostics);
        diagnostics.AddRange(semanticAnalyzer.Diagnostics);

        return new SmileAnalysisResult(new SyntaxTree(source, root, tokens), semanticModel, diagnostics.ToArray());
    }
}
