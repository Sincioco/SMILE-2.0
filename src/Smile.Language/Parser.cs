using System.Collections.Generic;

namespace Smile.Language;

internal sealed class Parser
{
    private readonly IReadOnlyList<SyntaxToken> _allTokens;
    private readonly List<SyntaxToken> _tokens = new();
    private readonly DiagnosticBag _diagnostics;
    private int _position;

    public Parser(SourceText source, IReadOnlyList<SyntaxToken> tokens, IReadOnlyList<Diagnostic> lexerDiagnostics)
    {
        _allTokens = tokens;
        _diagnostics = new DiagnosticBag(source);
        _diagnostics.AddRange(lexerDiagnostics);

        foreach (var token in tokens)
        {
            if (token.Kind != SyntaxKind.CommentToken && token.Kind != SyntaxKind.BadToken)
                _tokens.Add(token);
        }
    }

    public IReadOnlyList<SyntaxToken> AllTokens => _allTokens;
    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.ToArray();

    public CompilationUnitSyntax ParseCompilationUnit()
    {
        var statements = ParseStatementsUntil();
        var endOfFile = MatchToken(SyntaxKind.EndOfFileToken);
        return new CompilationUnitSyntax(statements, endOfFile);
    }

    private IReadOnlyList<StatementSyntax> ParseStatementsUntil(params SyntaxKind[] boundaries)
    {
        var statements = new List<StatementSyntax>();
        while (Current.Kind != SyntaxKind.EndOfFileToken && !IsBoundary(Current.Kind, boundaries))
        {
            if (Current.Kind == SyntaxKind.NewLineToken)
            {
                NextToken();
                continue;
            }

            var start = _position;
            var statement = ParseStatement();
            if (statement != null)
                statements.Add(statement);

            if (_position == start)
                NextToken();
        }

        return statements;
    }

    private StatementSyntax? ParseStatement()
    {
        switch (Current.Kind)
        {
            case SyntaxKind.DimKeyword:
                return ParseDimStatement();
            case SyntaxKind.IfKeyword:
                return ParseIfStatement();
            case SyntaxKind.ForKeyword:
                return ParseForStatement();
            case SyntaxKind.DoKeyword:
                return ParseDoUntilStatement();
            case SyntaxKind.PrintKeyword:
                return ParsePrintStatement();
            case SyntaxKind.GetKeyword:
                return ParseGetKeyStatement();
            case SyntaxKind.ClearKeyword:
                return ParseClearScreenStatement();
            case SyntaxKind.WaitKeyword:
                return ParseWaitStatement();
            case SyntaxKind.RandomKeyword:
                return ParseRandomStatement();
            case SyntaxKind.IdentifierToken:
            case SyntaxKind.KeyKeyword:
                return ParseAssignmentStatement();
            default:
                _diagnostics.Report("SML2002", Current.Span, $"Unexpected token '{Display(Current)}' at the start of a statement.");
                SynchronizeLine();
                return null;
        }
    }

    private DimStatementSyntax ParseDimStatement()
    {
        var dim = MatchToken(SyntaxKind.DimKeyword);
        var identifier = MatchIdentifier();
        var open = MatchToken(SyntaxKind.OpenBracketToken);
        var size = MatchToken(SyntaxKind.NumberToken);
        var close = MatchToken(SyntaxKind.CloseBracketToken);
        ConsumeLineEnd();
        return new DimStatementSyntax(dim, identifier, open, size, close);
    }

    private AssignmentStatementSyntax ParseAssignmentStatement()
    {
        var identifier = MatchIdentifier();
        SyntaxToken? open = null;
        ExpressionSyntax? index = null;
        SyntaxToken? close = null;
        if (Current.Kind == SyntaxKind.OpenBracketToken)
        {
            open = NextToken();
            index = ParseExpression();
            close = MatchToken(SyntaxKind.CloseBracketToken);
        }

        var target = new AssignmentTargetSyntax(identifier, open, index, close);
        var equals = MatchToken(SyntaxKind.EqualsToken);
        var expression = ParseExpression();
        ConsumeLineEnd();
        return new AssignmentStatementSyntax(target, equals, expression);
    }

    private PrintStatementSyntax ParsePrintStatement()
    {
        var print = MatchToken(SyntaxKind.PrintKeyword);
        var items = new List<ExpressionSyntax>();
        var suppressNewLine = false;
        var end = print.Span.End;

        if (!IsLineEnd(Current.Kind))
        {
            while (true)
            {
                var item = ParseExpression();
                items.Add(item);
                end = item.Span.End;

                if (Current.Kind != SyntaxKind.SemicolonToken)
                    break;

                var semicolon = NextToken();
                end = semicolon.Span.End;
                if (IsLineEnd(Current.Kind))
                {
                    suppressNewLine = true;
                    break;
                }
            }
        }

        ConsumeLineEnd();
        return new PrintStatementSyntax(print, items, suppressNewLine, end);
    }

    private GetKeyStatementSyntax ParseGetKeyStatement()
    {
        var get = MatchToken(SyntaxKind.GetKeyword);
        var key = MatchToken(SyntaxKind.KeyKeyword);
        var identifier = MatchIdentifier();
        ConsumeLineEnd();
        return new GetKeyStatementSyntax(get, key, identifier);
    }

    private ClearScreenStatementSyntax ParseClearScreenStatement()
    {
        var clear = MatchToken(SyntaxKind.ClearKeyword);
        var screen = MatchToken(SyntaxKind.ScreenKeyword);
        ConsumeLineEnd();
        return new ClearScreenStatementSyntax(clear, screen);
    }

    private WaitStatementSyntax ParseWaitStatement()
    {
        var wait = MatchToken(SyntaxKind.WaitKeyword);
        var duration = ParseExpression();
        var milliseconds = MatchToken(SyntaxKind.MillisecondsKeyword);
        ConsumeLineEnd();
        return new WaitStatementSyntax(wait, duration, milliseconds);
    }

    private RandomStatementSyntax ParseRandomStatement()
    {
        var random = MatchToken(SyntaxKind.RandomKeyword);
        var identifier = MatchIdentifier();
        var from = MatchToken(SyntaxKind.FromKeyword);
        var minimum = ParseExpression();
        var to = MatchToken(SyntaxKind.ToKeyword);
        var maximum = ParseExpression();
        ConsumeLineEnd();
        return new RandomStatementSyntax(random, identifier, from, minimum, to, maximum);
    }

    private IfStatementSyntax ParseIfStatement()
    {
        var ifKeyword = MatchToken(SyntaxKind.IfKeyword);
        var clauses = new List<IfClauseSyntax>();

        var condition = ParseExpression();
        MatchToken(SyntaxKind.ThenKeyword);
        ConsumeLineEnd();
        var statements = ParseStatementsUntil(SyntaxKind.ElseKeyword, SyntaxKind.EndKeyword);
        clauses.Add(new IfClauseSyntax(condition, statements));

        var elseStatements = (IReadOnlyList<StatementSyntax>)new List<StatementSyntax>();
        while (Current.Kind == SyntaxKind.ElseKeyword && Peek(1).Kind == SyntaxKind.IfKeyword)
        {
            NextToken();
            NextToken();
            condition = ParseExpression();
            MatchToken(SyntaxKind.ThenKeyword);
            ConsumeLineEnd();
            statements = ParseStatementsUntil(SyntaxKind.ElseKeyword, SyntaxKind.EndKeyword);
            clauses.Add(new IfClauseSyntax(condition, statements));
        }

        if (Current.Kind == SyntaxKind.ElseKeyword)
        {
            NextToken();
            ConsumeLineEnd();
            elseStatements = ParseStatementsUntil(SyntaxKind.EndKeyword);
        }

        var endKeyword = MatchToken(SyntaxKind.EndKeyword);
        var finalIf = MatchToken(SyntaxKind.IfKeyword);
        ConsumeLineEnd();
        return new IfStatementSyntax(ifKeyword, clauses, elseStatements, endKeyword, finalIf);
    }

    private ForStatementSyntax ParseForStatement()
    {
        var forKeyword = MatchToken(SyntaxKind.ForKeyword);
        var identifier = MatchIdentifier();
        MatchToken(SyntaxKind.EqualsToken);
        var lower = ParseExpression();
        var descending = false;
        if (Current.Kind == SyntaxKind.DownKeyword)
        {
            descending = true;
            NextToken();
        }

        MatchToken(SyntaxKind.ToKeyword);
        var upper = ParseExpression();
        ConsumeLineEnd();
        var statements = ParseStatementsUntil(SyntaxKind.EndKeyword);
        MatchToken(SyntaxKind.EndKeyword);
        var finalFor = MatchToken(SyntaxKind.ForKeyword);
        ConsumeLineEnd();
        return new ForStatementSyntax(forKeyword, identifier, lower, descending, upper, statements, finalFor);
    }

    private DoUntilStatementSyntax ParseDoUntilStatement()
    {
        var doKeyword = MatchToken(SyntaxKind.DoKeyword);
        ConsumeLineEnd();
        var statements = ParseStatementsUntil(SyntaxKind.LoopKeyword);
        MatchToken(SyntaxKind.LoopKeyword);
        MatchToken(SyntaxKind.UntilKeyword);
        var condition = ParseExpression();
        ConsumeLineEnd();
        return new DoUntilStatementSyntax(doKeyword, statements, condition);
    }

    private ExpressionSyntax ParseExpression(int parentPrecedence = 0)
    {
        ExpressionSyntax left;
        var unaryPrecedence = SyntaxFacts.GetUnaryPrecedence(Current.Kind);
        if (unaryPrecedence != 0 && unaryPrecedence >= parentPrecedence)
        {
            var operatorToken = NextToken();
            var operand = ParseExpression(unaryPrecedence);
            left = new UnaryExpressionSyntax(operatorToken, operand);
        }
        else
        {
            left = ParsePrimaryExpression();
        }

        while (true)
        {
            var precedence = SyntaxFacts.GetBinaryPrecedence(Current.Kind);
            if (precedence == 0 || precedence <= parentPrecedence)
                break;

            var operatorToken = NextToken();
            var right = ParseExpression(precedence);
            left = new BinaryExpressionSyntax(left, operatorToken, right);
        }

        return left;
    }

    private ExpressionSyntax ParsePrimaryExpression()
    {
        if (Current.Kind == SyntaxKind.OpenParenthesisToken)
        {
            var open = NextToken();
            var expression = ParseExpression();
            var close = MatchToken(SyntaxKind.CloseParenthesisToken);
            return new ParenthesizedExpressionSyntax(open, expression, close);
        }

        if (Current.Kind == SyntaxKind.TrueKeyword || Current.Kind == SyntaxKind.FalseKeyword)
        {
            var token = NextToken();
            return new LiteralExpressionSyntax(token, token.Kind == SyntaxKind.TrueKeyword);
        }

        if (SyntaxFacts.IsBuiltInConstant(Current.Kind))
        {
            var token = NextToken();
            return new LiteralExpressionSyntax(token, SyntaxFacts.GetBuiltInConstantValue(token.Kind));
        }

        if (Current.Kind == SyntaxKind.NumberToken)
        {
            var token = NextToken();
            return new LiteralExpressionSyntax(token, token.Value ?? 0L);
        }

        if (Current.Kind == SyntaxKind.StringToken)
        {
            var token = NextToken();
            return new LiteralExpressionSyntax(token, token.Value ?? string.Empty);
        }

        if (Current.Kind == SyntaxKind.IdentifierToken || Current.Kind == SyntaxKind.KeyKeyword)
        {
            var identifier = NextToken();
            if (Current.Kind == SyntaxKind.OpenBracketToken)
            {
                NextToken();
                var index = ParseExpression();
                var close = MatchToken(SyntaxKind.CloseBracketToken);
                return new ArrayAccessExpressionSyntax(identifier, index, close);
            }

            return new NameExpressionSyntax(identifier);
        }

        var missing = MatchToken(SyntaxKind.NumberToken);
        return new LiteralExpressionSyntax(missing, 0L);
    }

    private void ConsumeLineEnd()
    {
        if (Current.Kind == SyntaxKind.NewLineToken)
        {
            NextToken();
            return;
        }

        if (Current.Kind == SyntaxKind.EndOfFileToken)
            return;

        _diagnostics.Report("SML2001", Current.Span, $"Expected newline, found '{Display(Current)}'.");
        SynchronizeLine();
    }

    private void SynchronizeLine()
    {
        while (Current.Kind != SyntaxKind.NewLineToken && Current.Kind != SyntaxKind.EndOfFileToken)
            NextToken();
        if (Current.Kind == SyntaxKind.NewLineToken)
            NextToken();
    }

    private SyntaxToken MatchIdentifier()
    {
        if (Current.Kind == SyntaxKind.IdentifierToken || Current.Kind == SyntaxKind.KeyKeyword)
            return NextToken();
        return MatchToken(SyntaxKind.IdentifierToken);
    }

    private SyntaxToken MatchToken(SyntaxKind kind)
    {
        if (Current.Kind == kind)
            return NextToken();

        _diagnostics.Report("SML2001", Current.Span, $"Expected {SyntaxFacts.GetText(kind)}, found '{Display(Current)}'.");
        return new SyntaxToken(kind, Current.Position, string.Empty);
    }

    private SyntaxToken NextToken()
    {
        var current = Current;
        _position++;
        return current;
    }

    private SyntaxToken Peek(int offset)
    {
        var index = _position + offset;
        return index >= _tokens.Count ? _tokens[_tokens.Count - 1] : _tokens[index];
    }

    private SyntaxToken Current => Peek(0);

    private static bool IsBoundary(SyntaxKind kind, SyntaxKind[] boundaries)
    {
        foreach (var boundary in boundaries)
        {
            if (kind == boundary)
                return true;
        }
        return false;
    }

    private static bool IsLineEnd(SyntaxKind kind) => kind == SyntaxKind.NewLineToken || kind == SyntaxKind.EndOfFileToken;
    private static string Display(SyntaxToken token) => token.Kind == SyntaxKind.EndOfFileToken ? "end of file" : token.Text;
}
