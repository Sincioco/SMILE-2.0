using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Smile.Language;

public sealed class SmileIfBlockLayout
{
    internal SmileIfBlockLayout(bool isExpanded, int rootLine, int endLine,
        IReadOnlyList<int> headerEndLines, IReadOnlyList<int> boundaryLines)
    {
        IsExpanded = isExpanded;
        RootLine = rootLine;
        EndLine = endLine;
        HeaderEndLines = headerEndLines;
        BoundaryLines = boundaryLines;
    }

    public bool IsExpanded { get; }
    public int RootLine { get; }
    public int EndLine { get; }
    public IReadOnlyList<int> HeaderEndLines { get; }
    public IReadOnlyList<int> BoundaryLines { get; }
}

public sealed class SmileRoutineDeclarationLayout
{
    internal SmileRoutineDeclarationLayout(int headerStartLine, int headerEndLine)
    {
        HeaderStartLine = headerStartLine;
        HeaderEndLine = headerEndLine;
    }

    public int HeaderStartLine { get; }
    public int HeaderEndLine { get; }
    public bool IsMultiline => HeaderStartLine != HeaderEndLine;
}

/// <summary>
/// Performs syntax-aware SMILE source rewrites that must not be inferred from physical lines.
/// Presentation-only blank-line formatting remains in the command-line formatter wrapper.
/// </summary>
public static class SmileSourceFormatter
{
    private static readonly IReadOnlyDictionary<string, string> ContextualIdentifierCasing =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Left"] = "Left",
            ["Right"] = "Right",
            ["Text"] = "Text",
            ["Line"] = "Line",
            ["Window"] = "Window",
            ["Size"] = "Size",
            ["Key"] = "Key"
        };

    public static string Format(string sourceText, bool formatLongIf, int maximumLineLength,
        bool rewriteComputedReturns, bool formatContextualIdentifiers, string? filePath)
        => FormatCore(sourceText, formatLongIf, maximumLineLength, rewriteComputedReturns,
            formatContextualIdentifiers, filePath, null, null);

    public static string Format(string sourceText, bool formatLongIf, int maximumLineLength,
        bool rewriteComputedReturns, bool formatContextualIdentifiers, string? filePath,
        SmileAnalysisResult symbolAnalysis, SyntaxTree symbolSyntaxTree)
    {
        if (symbolAnalysis == null)
            throw new ArgumentNullException(nameof(symbolAnalysis));
        if (symbolSyntaxTree == null)
            throw new ArgumentNullException(nameof(symbolSyntaxTree));
        if (!symbolAnalysis.SyntaxTrees.Contains(symbolSyntaxTree))
            throw new ArgumentException("The symbol syntax tree is not part of the supplied analysis.",
                nameof(symbolSyntaxTree));
        if (!string.Equals(NormalizeLineEndings(sourceText),
                NormalizeLineEndings(symbolSyntaxTree.Source.Text), StringComparison.Ordinal))
        {
            throw new ArgumentException("The symbol syntax tree does not match the source being formatted.",
                nameof(symbolSyntaxTree));
        }

        return FormatCore(sourceText, formatLongIf, maximumLineLength, rewriteComputedReturns,
            formatContextualIdentifiers, filePath, symbolAnalysis, symbolSyntaxTree);
    }

    public static IReadOnlyList<SmileIfBlockLayout> GetIfBlockLayouts(string sourceText, string? filePath)
    {
        if (sourceText == null)
            throw new ArgumentNullException(nameof(sourceText));

        var analysis = SmileLanguage.Analyze(NormalizeLineEndings(sourceText), filePath);
        var tree = analysis.SyntaxTree;
        var layouts = new List<SmileIfBlockLayout>();

        foreach (var statement in EnumerateIfStatements(tree.Root.Statements))
        {
            var elseToken = FindElseToken(tree, statement);
            var bodies = statement.Clauses.Select(clause => clause.Statements).ToList();
            if (elseToken != null)
                bodies.Add(statement.ElseStatements);
            var isExpanded = bodies.Any(body => body.Count == 0 || body.Count > 2 ||
                body.Any(IsNestedControlStatement));
            var headerEndLines = statement.Clauses.Select(clause =>
                GetLine(tree.Source, Math.Max(clause.Condition.Span.Start, clause.Condition.Span.End - 1)))
                .ToArray();
            var boundaryLines = statement.Clauses.Skip(1)
                .Select(clause => GetLine(tree.Source, clause.Condition.Span.Start)).ToList();
            if (elseToken != null)
                boundaryLines.Add(GetLine(tree.Source, elseToken.Span.Start));
            boundaryLines.Add(GetLine(tree.Source, statement.EndKeyword.Span.Start));
            layouts.Add(new SmileIfBlockLayout(isExpanded,
                GetLine(tree.Source, statement.IfKeyword.Span.Start),
                GetLine(tree.Source, statement.EndKeyword.Span.Start), headerEndLines, boundaryLines));
        }

        return layouts;
    }

    public static IReadOnlyList<SmileRoutineDeclarationLayout> GetRoutineDeclarationLayouts(
        string sourceText, string? filePath)
    {
        if (sourceText == null)
            throw new ArgumentNullException(nameof(sourceText));

        var analysis = SmileLanguage.Analyze(NormalizeLineEndings(sourceText), filePath);
        return EnumerateRoutines(analysis.SyntaxTree.Root.Statements)
            .Select(routine => new SmileRoutineDeclarationLayout(
                GetLine(analysis.SyntaxTree.Source, routine.Keyword.Span.Start),
                GetLine(analysis.SyntaxTree.Source, RoutineHeaderEndToken(routine).Span.Start)))
            .ToArray();
    }

    private static string FormatCore(string sourceText, bool formatLongIf, int maximumLineLength,
        bool rewriteComputedReturns, bool formatContextualIdentifiers, string? filePath,
        SmileAnalysisResult? symbolAnalysis, SyntaxTree? symbolSyntaxTree)
    {
        if (sourceText == null)
            throw new ArgumentNullException(nameof(sourceText));
        if (maximumLineLength < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumLineLength));

        var result = NormalizeLineEndings(sourceText);
        if (formatContextualIdentifiers)
            result = RewriteContextualIdentifiers(result, filePath);
        if (rewriteComputedReturns)
            result = RewriteComputedReturns(result, filePath, symbolAnalysis, symbolSyntaxTree);
        if (formatLongIf)
            result = RewriteLongIfStatements(result, maximumLineLength, filePath);
        return result;
    }

    private static string RewriteContextualIdentifiers(string text, string? filePath)
    {
        var analysis = SmileLanguage.Analyze(text, filePath);
        var tree = analysis.SyntaxTree;
        var edits = new List<TextEdit>();

        for (var index = 0; index < tree.Tokens.Count; index++)
        {
            var token = tree.Tokens[index];
            if (!ContextualIdentifierCasing.TryGetValue(token.Text, out var preferred) ||
                string.Equals(token.Text, preferred, StringComparison.Ordinal))
            {
                continue;
            }
            if (!SmileSymbolService.TryResolveToken(analysis, tree, token, index, out var symbol) ||
                !IsOrdinaryIdentifier(symbol.Kind))
            {
                continue;
            }
            edits.Add(new TextEdit(token.Span.Start, token.Span.Length, preferred));
        }

        return ApplyEdits(text, edits);
    }

    private static bool IsOrdinaryIdentifier(SmileResolvedSymbolKind kind) =>
        kind is SmileResolvedSymbolKind.Module or SmileResolvedSymbolKind.Function or
            SmileResolvedSymbolKind.Subroutine or SmileResolvedSymbolKind.Variable or
            SmileResolvedSymbolKind.Array or SmileResolvedSymbolKind.Type or
            SmileResolvedSymbolKind.Field or SmileResolvedSymbolKind.Parameter or
            SmileResolvedSymbolKind.Local;

    private static string RewriteComputedReturns(string text, string? filePath,
        SmileAnalysisResult? symbolAnalysis, SyntaxTree? symbolSyntaxTree)
    {
        var analysis = SmileLanguage.Analyze(text, filePath);
        var resolutionAnalysis = symbolAnalysis ?? analysis;
        var resolutionTree = symbolSyntaxTree ?? analysis.SyntaxTree;
        var edits = new List<TextEdit>();

        foreach (var routine in EnumerateRoutines(analysis.SyntaxTree.Root.Statements).Where(item => item.IsFunction))
        {
            var computedReturns = EnumerateReturns(routine.Statements)
                .Where(statement => statement.Expression != null &&
                    !IsDirectReturn(statement.Expression, resolutionAnalysis, resolutionTree))
                .ToArray();
            if (computedReturns.Length == 0)
                continue;

            var returnVariable = ChooseReturnVariable(analysis.SyntaxTree, routine);
            var returnType = GetReturnType(text, analysis, routine);
            var declarationIndent = GetLineIndent(text, routine.Keyword.Span.Start);
            var bodyIndent = declarationIndent + "    ";
            var insertionPosition = FindLineEnd(text, RoutineHeaderEndToken(routine).Span.End);
            if (insertionPosition < text.Length && text[insertionPosition] == '\n')
                insertionPosition++;
            var needsBlankAfter = insertionPosition >= text.Length || text[insertionPosition] != '\n';
            var declarationText = "\n" + bodyIndent + "Dim " + returnVariable + " As " + returnType + "\n" +
                (needsBlankAfter ? "\n" : string.Empty);
            edits.Add(new TextEdit(insertionPosition, 0, declarationText));

            foreach (var statement in computedReturns)
            {
                var expression = statement.Expression!;
                var indent = GetLineIndent(text, statement.ReturnKeyword.Span.Start);
                var expressionText = text.Substring(expression.Span.Start, expression.Span.Length);
                var replacement = returnVariable + " = " + expressionText + "\n\n" + indent +
                    "Return " + returnVariable;
                edits.Add(new TextEdit(statement.ReturnKeyword.Span.Start, statement.Span.Length, replacement));
            }
        }

        return ApplyEdits(text, edits);
    }

    private static bool IsDirectReturn(ExpressionSyntax expression, SmileAnalysisResult analysis,
        SyntaxTree tree)
    {
        if (expression is LiteralExpressionSyntax or NameExpressionSyntax)
            return true;
        if (expression is not QualifiedNameExpressionSyntax qualified)
            return false;

        var tokenIndex = -1;
        for (var index = 0; index < tree.Tokens.Count; index++)
        {
            var token = tree.Tokens[index];
            if (token.Span.Start == qualified.Member.Span.Start && token.Span.Length == qualified.Member.Span.Length)
            {
                tokenIndex = index;
                break;
            }
        }
        return tokenIndex >= 0 &&
               SmileSymbolService.TryResolveToken(analysis, tree, tree.Tokens[tokenIndex], tokenIndex, out var symbol) &&
               symbol.Kind is SmileResolvedSymbolKind.Constant or SmileResolvedSymbolKind.Variable;
    }

    private static SyntaxToken? FindElseToken(SyntaxTree tree, IfStatementSyntax statement)
    {
        var lastClause = statement.Clauses[statement.Clauses.Count - 1];
        var searchStart = lastClause.Statements.Count == 0
            ? lastClause.Condition.Span.End
            : lastClause.Statements[lastClause.Statements.Count - 1].Span.End;
        return tree.Tokens.FirstOrDefault(token => token.Kind == SyntaxKind.ElseKeyword &&
            searchStart <= token.Span.Start && token.Span.Start < statement.EndKeyword.Span.Start);
    }

    private static bool IsNestedControlStatement(StatementSyntax statement) =>
        statement is IfStatementSyntax or ForStatementSyntax or DoStatementSyntax or
            SelectStatementSyntax or ClipRectangleStatementSyntax or WithStatementSyntax;

    private static SyntaxToken RoutineHeaderEndToken(RoutineDeclarationSyntax routine) =>
        routine.ReturnTypeToken ?? routine.CloseParenthesis ?? routine.Identifier;

    private static int GetLine(SourceText source, int position)
    {
        source.GetLineColumn(position, out var line, out _);
        return line;
    }

    private static string ChooseReturnVariable(SyntaxTree tree, RoutineDeclarationSyntax routine)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in tree.Tokens)
        {
            if (routine.Span.Start <= token.Span.Start && token.Span.End <= routine.Span.End &&
                IsIdentifierText(token.Text))
            {
                names.Add(token.Text);
            }
        }

        var candidate = "ReturnValue";
        var suffix = 2;
        while (names.Contains(candidate))
            candidate = "ReturnValue" + suffix++;
        return candidate;
    }

    private static bool IsIdentifierText(string text) =>
        !string.IsNullOrWhiteSpace(text) && (char.IsLetter(text[0]) || text[0] == '_') &&
        text.All(character => char.IsLetterOrDigit(character) || character == '_');

    private static string GetReturnType(string text, SmileAnalysisResult analysis,
        RoutineDeclarationSyntax routine)
    {
        if (routine.ReturnTypeToken != null && routine.ReturnTypeToken.Span.Length > 0)
            return text.Substring(routine.ReturnTypeToken.Span.Start, routine.ReturnTypeToken.Span.Length).Trim();

        var hasRoutineError = analysis.Diagnostics.Any(diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error &&
            routine.Span.Start <= diagnostic.Span.Start && diagnostic.Span.Start <= routine.Span.End);
        var symbol = analysis.SemanticModel.Routines.Values.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, routine.Identifier.Text, StringComparison.OrdinalIgnoreCase) &&
            candidate.Declaration.Identifier.Span.Start == routine.Identifier.Span.Start &&
            candidate.Declaration.Identifier.Span.Length == routine.Identifier.Span.Length);
        if (hasRoutineError || symbol == null || symbol.ReturnType == SmileType.Error)
            throw new InvalidOperationException("Unable to infer the return type for Function '" +
                                                routine.Identifier.Text + "'.");
        return string.IsNullOrWhiteSpace(symbol.ReturnType.ModuleName)
            ? symbol.ReturnType.Name
            : symbol.ReturnType.ModuleName + "." + symbol.ReturnType.Name;
    }

    private static string RewriteLongIfStatements(string text, int maximumLineLength, string? filePath)
    {
        var analysis = SmileLanguage.Analyze(text, filePath);
        var tree = analysis.SyntaxTree;
        var edits = new List<TextEdit>();

        foreach (var statement in EnumerateIfStatements(tree.Root.Statements))
        {
            foreach (var clause in statement.Clauses)
            {
                var condition = clause.Condition is ParenthesizedExpressionSyntax parenthesized
                    ? parenthesized.Expression
                    : clause.Condition;
                var expressions = new List<ExpressionSyntax>();
                var operators = new List<SyntaxToken>();
                FlattenLogicalCondition(condition, expressions, operators);
                if (expressions.Count < 2)
                    continue;

                var conditionText = text.Substring(clause.Condition.Span.Start, clause.Condition.Span.Length);
                var collapsedLength = Regex.Replace(conditionText, @"\s+", " ").Trim().Length;
                var prefixLength = clause.Condition.Span.Start - FindLineStart(text, clause.Condition.Span.Start);
                if (expressions.Count <= 2 && prefixLength + collapsedLength + " Then".Length <= maximumLineLength)
                    continue;

                var indent = GetLineIndent(text, clause.Condition.Span.Start);
                var continuationIndent = indent + "    ";
                var builder = new StringBuilder();
                builder.Append('(');
                for (var index = 0; index < expressions.Count; index++)
                {
                    if (index > 0)
                        builder.Append('\n').Append(continuationIndent);
                    var expression = expressions[index];
                    builder.Append(text.Substring(expression.Span.Start, expression.Span.Length).Trim());
                    if (index < operators.Count)
                    {
                        builder.Append(' ').Append(CanonicalLogicalOperator(operators[index]));
                        foreach (var comment in CommentsBetween(tree, expressions[index].Span.End,
                                     expressions[index + 1].Span.Start))
                        {
                            builder.Append(' ').Append(comment.Text.TrimEnd());
                        }
                    }
                }
                builder.Append(')');
                edits.Add(new TextEdit(clause.Condition.Span.Start, clause.Condition.Span.Length,
                    builder.ToString()));
            }
        }

        return ApplyEdits(text, edits);
    }

    private static void FlattenLogicalCondition(ExpressionSyntax expression,
        ICollection<ExpressionSyntax> expressions, ICollection<SyntaxToken> operators)
    {
        if (expression is BinaryExpressionSyntax binary &&
            binary.OperatorToken.Kind is SyntaxKind.AndKeyword or SyntaxKind.OrKeyword)
        {
            FlattenLogicalCondition(binary.Left, expressions, operators);
            operators.Add(binary.OperatorToken);
            FlattenLogicalCondition(binary.Right, expressions, operators);
            return;
        }
        expressions.Add(expression);
    }

    private static string CanonicalLogicalOperator(SyntaxToken token) =>
        token.Kind == SyntaxKind.AndKeyword ? "And" : "Or";

    private static IEnumerable<SyntaxToken> CommentsBetween(SyntaxTree tree, int start, int end) =>
        tree.Tokens.Where(token => token.Kind == SyntaxKind.CommentToken &&
                                   start <= token.Span.Start && token.Span.End <= end);

    private static IEnumerable<RoutineDeclarationSyntax> EnumerateRoutines(
        IEnumerable<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            if (statement is RoutineDeclarationSyntax routine)
                yield return routine;
            if (statement is VisibilityDeclarationSyntax visibility)
            {
                foreach (var nested in EnumerateRoutines(new[] { visibility.Declaration }))
                    yield return nested;
            }
            else if (statement is ModuleDeclarationSyntax module)
            {
                foreach (var nested in EnumerateRoutines(module.Statements))
                    yield return nested;
            }
        }
    }

    private static IEnumerable<ReturnStatementSyntax> EnumerateReturns(
        IEnumerable<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            if (statement is ReturnStatementSyntax returnStatement)
                yield return returnStatement;
            foreach (var nested in ChildStatements(statement))
            {
                foreach (var nestedReturn in EnumerateReturns(nested))
                    yield return nestedReturn;
            }
        }
    }

    private static IEnumerable<IfStatementSyntax> EnumerateIfStatements(
        IEnumerable<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            if (statement is IfStatementSyntax ifStatement)
                yield return ifStatement;
            if (statement is RoutineDeclarationSyntax routine)
            {
                foreach (var nested in EnumerateIfStatements(routine.Statements))
                    yield return nested;
            }
            else if (statement is VisibilityDeclarationSyntax visibility)
            {
                foreach (var nested in EnumerateIfStatements(new[] { visibility.Declaration }))
                    yield return nested;
            }
            else if (statement is ModuleDeclarationSyntax module)
            {
                foreach (var nested in EnumerateIfStatements(module.Statements))
                    yield return nested;
            }
            else
            {
                foreach (var nested in ChildStatements(statement))
                {
                    foreach (var nestedIf in EnumerateIfStatements(nested))
                        yield return nestedIf;
                }
            }
        }
    }

    private static IEnumerable<IReadOnlyList<StatementSyntax>> ChildStatements(StatementSyntax statement)
    {
        switch (statement)
        {
            case IfStatementSyntax ifStatement:
                foreach (var clause in ifStatement.Clauses)
                    yield return clause.Statements;
                yield return ifStatement.ElseStatements;
                break;
            case ForStatementSyntax forStatement:
                yield return forStatement.Statements;
                break;
            case DoStatementSyntax doStatement:
                yield return doStatement.Statements;
                break;
            case SelectStatementSyntax selectStatement:
                foreach (var clause in selectStatement.Cases)
                    yield return clause.Statements;
                break;
            case ClipRectangleStatementSyntax clipStatement:
                yield return clipStatement.Statements;
                break;
            case WithStatementSyntax withStatement:
                yield return withStatement.Statements;
                break;
        }
    }

    private static int FindLineStart(string text, int position)
    {
        var index = Math.Min(position, text.Length);
        while (index > 0 && text[index - 1] != '\n')
            index--;
        return index;
    }

    private static int FindLineEnd(string text, int position)
    {
        var index = Math.Min(position, text.Length);
        while (index < text.Length && text[index] != '\n')
            index++;
        return index;
    }

    private static string GetLineIndent(string text, int position)
    {
        var start = FindLineStart(text, position);
        var index = start;
        while (index < text.Length && (text[index] == ' ' || text[index] == '\t'))
            index++;
        return text.Substring(start, index - start);
    }

    private static string ApplyEdits(string text, IEnumerable<TextEdit> requestedEdits)
    {
        var edits = requestedEdits.OrderByDescending(edit => edit.Start)
            .ThenByDescending(edit => edit.Length).ToArray();
        var previousStart = text.Length + 1;
        var builder = new StringBuilder(text);
        foreach (var edit in edits)
        {
            if (edit.Start < 0 || edit.Length < 0 || edit.Start + edit.Length > text.Length)
                throw new InvalidOperationException("A SMILE formatter edit was outside the source span.");
            if (edit.Start + edit.Length > previousStart)
                throw new InvalidOperationException("SMILE formatter edits overlapped.");
            builder.Remove(edit.Start, edit.Length);
            builder.Insert(edit.Start, edit.Replacement);
            previousStart = edit.Start;
        }
        return builder.ToString();
    }

    private static string NormalizeLineEndings(string text) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n');

    private sealed class TextEdit
    {
        public TextEdit(int start, int length, string replacement)
        {
            Start = start;
            Length = length;
            Replacement = replacement;
        }

        public int Start { get; }
        public int Length { get; }
        public string Replacement { get; }
    }
}
