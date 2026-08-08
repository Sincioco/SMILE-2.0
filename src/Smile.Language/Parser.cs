using System;
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
        return new CompilationUnitSyntax(statements, MatchToken(SyntaxKind.EndOfFileToken));
    }

    private IReadOnlyList<StatementSyntax> ParseStatementsUntil(Func<bool>? isBoundary = null)
    {
        var statements = new List<StatementSyntax>();
        while (Current.Kind != SyntaxKind.EndOfFileToken && !(isBoundary?.Invoke() ?? false))
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
            case SyntaxKind.ConstKeyword: return ParseConstStatement();
            case SyntaxKind.DimKeyword: return ParseDimStatement();
            case SyntaxKind.IfKeyword: return ParseIfStatement();
            case SyntaxKind.ForKeyword: return ParseForStatement();
            case SyntaxKind.DoKeyword: return ParseDoStatement();
            case SyntaxKind.PrintKeyword: return ParsePrintStatement();
            case SyntaxKind.GetKeyword: return ParseGetKeyStatement();
            case SyntaxKind.ClearKeyword: return ParseClearScreenStatement();
            case SyntaxKind.WaitKeyword: return ParseWaitStatement();
            case SyntaxKind.RandomKeyword: return ParseRandomStatement();
            case SyntaxKind.SubKeyword:
            case SyntaxKind.FunctionKeyword: return ParseRoutineDeclaration();
            case SyntaxKind.CallKeyword: return ParseCallStatement();
            case SyntaxKind.ReturnKeyword: return ParseReturnStatement();
            case SyntaxKind.SelectKeyword: return ParseSelectStatement();
            case SyntaxKind.ExitKeyword: return ParseExitStatement();
            case SyntaxKind.EndKeyword when Peek(1).Kind == SyntaxKind.ProgramKeyword: return ParseEndProgramStatement();
            case SyntaxKind.IdentifierToken:
            case SyntaxKind.KeyKeyword: return ParseAssignmentStatement();
            default:
                _diagnostics.Report("SML2002", Current.Span, $"Unexpected token '{Display(Current)}' at the start of a statement.");
                SynchronizeLine();
                return null;
        }
    }

    private ConstStatementSyntax ParseConstStatement()
    {
        var keyword = MatchToken(SyntaxKind.ConstKeyword);
        var identifier = MatchIdentifier();
        var equals = MatchToken(SyntaxKind.EqualsToken);
        var expression = ParseExpression();
        ConsumeLineEnd();
        return new ConstStatementSyntax(keyword, identifier, equals, expression);
    }

    private DimStatementSyntax ParseDimStatement()
    {
        var keyword = MatchToken(SyntaxKind.DimKeyword);
        var identifier = MatchIdentifier();
        var open = MatchToken(SyntaxKind.OpenBracketToken);
        var sizes = ParseExpressionList(SyntaxKind.CloseBracketToken);
        var close = MatchToken(SyntaxKind.CloseBracketToken);
        ConsumeLineEnd();
        return new DimStatementSyntax(keyword, identifier, open, sizes, close);
    }

    private AssignmentStatementSyntax ParseAssignmentStatement()
    {
        var identifier = MatchIdentifier();
        SyntaxToken? open = null;
        SyntaxToken? close = null;
        IReadOnlyList<ExpressionSyntax> indices = Array.Empty<ExpressionSyntax>();
        if (Current.Kind == SyntaxKind.OpenBracketToken)
        {
            open = NextToken();
            indices = ParseExpressionList(SyntaxKind.CloseBracketToken);
            close = MatchToken(SyntaxKind.CloseBracketToken);
        }

        var target = new AssignmentTargetSyntax(identifier, open, indices, close);
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
        var keyword = MatchToken(SyntaxKind.IfKeyword);
        var clauses = new List<IfClauseSyntax>();
        var condition = ParseExpression();
        MatchToken(SyntaxKind.ThenKeyword);
        ConsumeLineEnd();
        var statements = ParseStatementsUntil(() => Current.Kind == SyntaxKind.ElseKeyword || IsEndPair(SyntaxKind.IfKeyword));
        clauses.Add(new IfClauseSyntax(condition, statements));

        IReadOnlyList<StatementSyntax> elseStatements = Array.Empty<StatementSyntax>();
        while (Current.Kind == SyntaxKind.ElseKeyword && Peek(1).Kind == SyntaxKind.IfKeyword)
        {
            NextToken();
            NextToken();
            condition = ParseExpression();
            MatchToken(SyntaxKind.ThenKeyword);
            ConsumeLineEnd();
            statements = ParseStatementsUntil(() => Current.Kind == SyntaxKind.ElseKeyword || IsEndPair(SyntaxKind.IfKeyword));
            clauses.Add(new IfClauseSyntax(condition, statements));
        }

        if (Current.Kind == SyntaxKind.ElseKeyword)
        {
            NextToken();
            ConsumeLineEnd();
            elseStatements = ParseStatementsUntil(() => IsEndPair(SyntaxKind.IfKeyword));
        }

        var end = MatchToken(SyntaxKind.EndKeyword);
        var finalIf = MatchToken(SyntaxKind.IfKeyword);
        ConsumeLineEnd();
        return new IfStatementSyntax(keyword, clauses, elseStatements, end, finalIf);
    }

    private ForStatementSyntax ParseForStatement()
    {
        var keyword = MatchToken(SyntaxKind.ForKeyword);
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
        var statements = ParseStatementsUntil(() => IsEndPair(SyntaxKind.ForKeyword));
        MatchToken(SyntaxKind.EndKeyword);
        var finalFor = MatchToken(SyntaxKind.ForKeyword);
        ConsumeLineEnd();
        return new ForStatementSyntax(keyword, identifier, lower, descending, upper, statements, finalFor);
    }

    private DoStatementSyntax ParseDoStatement()
    {
        var keyword = MatchToken(SyntaxKind.DoKeyword);
        ConsumeLineEnd();
        var statements = ParseStatementsUntil(() => Current.Kind == SyntaxKind.LoopKeyword);
        var loop = MatchToken(SyntaxKind.LoopKeyword);
        ExpressionSyntax? condition = null;
        if (Current.Kind == SyntaxKind.UntilKeyword)
        {
            NextToken();
            condition = ParseExpression();
        }
        ConsumeLineEnd();
        return new DoStatementSyntax(keyword, statements, loop, condition);
    }

    private RoutineDeclarationSyntax ParseRoutineDeclaration()
    {
        var keyword = NextToken();
        var identifier = MatchIdentifier();
        var parameters = new List<SyntaxToken>();
        if (Current.Kind == SyntaxKind.OpenParenthesisToken)
        {
            NextToken();
            if (Current.Kind != SyntaxKind.CloseParenthesisToken)
            {
                while (true)
                {
                    parameters.Add(MatchIdentifier());
                    if (Current.Kind != SyntaxKind.CommaToken)
                        break;
                    NextToken();
                }
            }
            MatchToken(SyntaxKind.CloseParenthesisToken);
        }
        ConsumeLineEnd();
        var statements = ParseStatementsUntil(() => IsEndPair(keyword.Kind));
        var end = MatchToken(SyntaxKind.EndKeyword);
        var final = MatchToken(keyword.Kind);
        ConsumeLineEnd();
        return new RoutineDeclarationSyntax(keyword, identifier, parameters, statements, end, final);
    }

    private CallStatementSyntax ParseCallStatement()
    {
        var call = MatchToken(SyntaxKind.CallKeyword);
        var identifier = MatchIdentifier();
        MatchToken(SyntaxKind.OpenParenthesisToken);
        var arguments = ParseExpressionList(SyntaxKind.CloseParenthesisToken);
        var close = MatchToken(SyntaxKind.CloseParenthesisToken);
        ConsumeLineEnd();
        return new CallStatementSyntax(call, identifier, arguments, close);
    }

    private ReturnStatementSyntax ParseReturnStatement()
    {
        var keyword = MatchToken(SyntaxKind.ReturnKeyword);
        var expression = IsLineEnd(Current.Kind) ? null : ParseExpression();
        ConsumeLineEnd();
        return new ReturnStatementSyntax(keyword, expression);
    }

    private SelectStatementSyntax ParseSelectStatement()
    {
        var select = MatchToken(SyntaxKind.SelectKeyword);
        MatchToken(SyntaxKind.CaseKeyword);
        var expression = ParseExpression();
        ConsumeLineEnd();
        var cases = new List<SelectCaseClauseSyntax>();
        while (Current.Kind == SyntaxKind.CaseKeyword)
        {
            var caseKeyword = NextToken();
            var isElse = Current.Kind == SyntaxKind.ElseKeyword;
            ExpressionSyntax? value = null;
            if (isElse)
                NextToken();
            else
                value = ParseExpression();
            ConsumeLineEnd();
            var statements = ParseStatementsUntil(() => Current.Kind == SyntaxKind.CaseKeyword || IsEndPair(SyntaxKind.SelectKeyword));
            cases.Add(new SelectCaseClauseSyntax(caseKeyword, value, isElse, statements));
        }
        var end = MatchToken(SyntaxKind.EndKeyword);
        var finalSelect = MatchToken(SyntaxKind.SelectKeyword);
        ConsumeLineEnd();
        return new SelectStatementSyntax(select, expression, cases, end, finalSelect);
    }

    private ExitStatementSyntax ParseExitStatement()
    {
        var exit = MatchToken(SyntaxKind.ExitKeyword);
        SyntaxToken target;
        if (Current.Kind == SyntaxKind.ForKeyword || Current.Kind == SyntaxKind.DoKeyword)
            target = NextToken();
        else
            target = MatchToken(SyntaxKind.DoKeyword);
        ConsumeLineEnd();
        return new ExitStatementSyntax(exit, target);
    }

    private EndProgramStatementSyntax ParseEndProgramStatement()
    {
        var end = MatchToken(SyntaxKind.EndKeyword);
        var program = MatchToken(SyntaxKind.ProgramKeyword);
        ConsumeLineEnd();
        return new EndProgramStatementSyntax(end, program);
    }

    private IReadOnlyList<ExpressionSyntax> ParseExpressionList(SyntaxKind closingKind)
    {
        var expressions = new List<ExpressionSyntax>();
        if (Current.Kind == closingKind)
            return expressions;
        while (true)
        {
            expressions.Add(ParseExpression());
            if (Current.Kind != SyntaxKind.CommaToken)
                break;
            NextToken();
        }
        return expressions;
    }

    private ExpressionSyntax ParseExpression(int parentPrecedence = 0)
    {
        ExpressionSyntax left;
        var unaryPrecedence = SyntaxFacts.GetUnaryPrecedence(Current.Kind);
        if (unaryPrecedence != 0 && unaryPrecedence >= parentPrecedence)
        {
            var operatorToken = NextToken();
            left = new UnaryExpressionSyntax(operatorToken, ParseExpression(unaryPrecedence));
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
            left = new BinaryExpressionSyntax(left, operatorToken, ParseExpression(precedence));
        }
        return left;
    }

    private ExpressionSyntax ParsePrimaryExpression()
    {
        if (Current.Kind == SyntaxKind.OpenParenthesisToken)
        {
            var open = NextToken();
            var expression = ParseExpression();
            return new ParenthesizedExpressionSyntax(open, expression, MatchToken(SyntaxKind.CloseParenthesisToken));
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
        if (Current.Kind == SyntaxKind.IdentifierToken || Current.Kind == SyntaxKind.KeyKeyword || SyntaxFacts.IsBuiltInFunction(Current.Kind))
        {
            var identifier = NextToken();
            if (Current.Kind == SyntaxKind.OpenParenthesisToken)
            {
                NextToken();
                var arguments = ParseExpressionList(SyntaxKind.CloseParenthesisToken);
                return new CallExpressionSyntax(identifier, arguments, MatchToken(SyntaxKind.CloseParenthesisToken));
            }
            if (Current.Kind == SyntaxKind.OpenBracketToken)
            {
                NextToken();
                var indices = ParseExpressionList(SyntaxKind.CloseBracketToken);
                return new ArrayAccessExpressionSyntax(identifier, indices, MatchToken(SyntaxKind.CloseBracketToken));
            }
            return new NameExpressionSyntax(identifier);
        }
        var missing = MatchToken(SyntaxKind.NumberToken);
        return new LiteralExpressionSyntax(missing, 0L);
    }

    private bool IsEndPair(SyntaxKind finalKind) => Current.Kind == SyntaxKind.EndKeyword && Peek(1).Kind == finalKind;

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
    private static bool IsLineEnd(SyntaxKind kind) => kind == SyntaxKind.NewLineToken || kind == SyntaxKind.EndOfFileToken;
    private static string Display(SyntaxToken token) => token.Kind switch
    {
        SyntaxKind.EndOfFileToken => "end of file",
        SyntaxKind.NewLineToken => "newline",
        _ => token.Text
    };
}
