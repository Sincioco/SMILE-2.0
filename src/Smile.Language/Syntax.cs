using System;
using System.Collections.Generic;

namespace Smile.Language;

public enum SyntaxKind
{
    BadToken,
    EndOfFileToken,
    NewLineToken,
    CommentToken,
    IdentifierToken,
    NumberToken,
    StringToken,

    PlusToken,
    MinusToken,
    EqualsToken,
    NotEqualsToken,
    LessToken,
    GreaterToken,
    LessOrEqualsToken,
    GreaterOrEqualsToken,
    OpenParenthesisToken,
    CloseParenthesisToken,
    OpenBracketToken,
    CloseBracketToken,
    SemicolonToken,

    DimKeyword,
    IfKeyword,
    ThenKeyword,
    ElseKeyword,
    EndKeyword,
    ForKeyword,
    ToKeyword,
    DownKeyword,
    DoKeyword,
    LoopKeyword,
    UntilKeyword,
    PrintKeyword,
    GetKeyword,
    KeyKeyword,
    ClearKeyword,
    ScreenKeyword,
    WaitKeyword,
    MillisecondsKeyword,
    RandomKeyword,
    FromKeyword,
    TrueKeyword,
    FalseKeyword,
    AndKeyword,
    OrKeyword,
    NotKeyword,
    NoneKeyword,
    WKeyword,
    AKeyword,
    SKeyword,
    DKeyword,
    UpKeyword,
    LeftKeyword,
    RightKeyword,
}

public static class SyntaxFacts
{
    private static readonly Dictionary<string, SyntaxKind> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["DIM"] = SyntaxKind.DimKeyword,
        ["IF"] = SyntaxKind.IfKeyword,
        ["THEN"] = SyntaxKind.ThenKeyword,
        ["ELSE"] = SyntaxKind.ElseKeyword,
        ["END"] = SyntaxKind.EndKeyword,
        ["FOR"] = SyntaxKind.ForKeyword,
        ["TO"] = SyntaxKind.ToKeyword,
        ["DOWN"] = SyntaxKind.DownKeyword,
        ["DO"] = SyntaxKind.DoKeyword,
        ["LOOP"] = SyntaxKind.LoopKeyword,
        ["UNTIL"] = SyntaxKind.UntilKeyword,
        ["PRINT"] = SyntaxKind.PrintKeyword,
        ["GET"] = SyntaxKind.GetKeyword,
        ["KEY"] = SyntaxKind.KeyKeyword,
        ["CLEAR"] = SyntaxKind.ClearKeyword,
        ["SCREEN"] = SyntaxKind.ScreenKeyword,
        ["WAIT"] = SyntaxKind.WaitKeyword,
        ["MILLISECONDS"] = SyntaxKind.MillisecondsKeyword,
        ["RANDOM"] = SyntaxKind.RandomKeyword,
        ["FROM"] = SyntaxKind.FromKeyword,
        ["TRUE"] = SyntaxKind.TrueKeyword,
        ["FALSE"] = SyntaxKind.FalseKeyword,
        ["AND"] = SyntaxKind.AndKeyword,
        ["OR"] = SyntaxKind.OrKeyword,
        ["NOT"] = SyntaxKind.NotKeyword,
        ["NONE"] = SyntaxKind.NoneKeyword,
        ["W"] = SyntaxKind.WKeyword,
        ["A"] = SyntaxKind.AKeyword,
        ["S"] = SyntaxKind.SKeyword,
        ["D"] = SyntaxKind.DKeyword,
        ["UP"] = SyntaxKind.UpKeyword,
        ["LEFT"] = SyntaxKind.LeftKeyword,
        ["RIGHT"] = SyntaxKind.RightKeyword,
    };

    public static SyntaxKind GetKeywordKind(string text) =>
        Keywords.TryGetValue(text, out var kind) ? kind : SyntaxKind.IdentifierToken;

    public static bool IsKeyword(SyntaxKind kind) =>
        kind >= SyntaxKind.DimKeyword && kind <= SyntaxKind.NotKeyword;

    public static bool IsBuiltInConstant(SyntaxKind kind) =>
        kind >= SyntaxKind.NoneKeyword && kind <= SyntaxKind.RightKeyword || kind == SyntaxKind.DownKeyword;

    public static string GetText(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.EndOfFileToken => "end of file",
            SyntaxKind.NewLineToken => "newline",
            SyntaxKind.IdentifierToken => "identifier",
            SyntaxKind.NumberToken => "number",
            SyntaxKind.StringToken => "text literal",
            SyntaxKind.PlusToken => "+",
            SyntaxKind.MinusToken => "-",
            SyntaxKind.EqualsToken => "=",
            SyntaxKind.NotEqualsToken => "<>",
            SyntaxKind.LessToken => "<",
            SyntaxKind.GreaterToken => ">",
            SyntaxKind.LessOrEqualsToken => "<=",
            SyntaxKind.GreaterOrEqualsToken => ">=",
            SyntaxKind.OpenParenthesisToken => "(",
            SyntaxKind.CloseParenthesisToken => ")",
            SyntaxKind.OpenBracketToken => "[",
            SyntaxKind.CloseBracketToken => "]",
            SyntaxKind.SemicolonToken => ";",
            _ when IsKeyword(kind) || IsBuiltInConstant(kind) => kind.ToString().Replace("Keyword", string.Empty).ToUpperInvariant(),
            _ => kind.ToString()
        };
    }

    public static int GetUnaryPrecedence(SyntaxKind kind) =>
        kind == SyntaxKind.MinusToken || kind == SyntaxKind.NotKeyword ? 7 : 0;

    public static int GetBinaryPrecedence(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.PlusToken or SyntaxKind.MinusToken => 6,
            SyntaxKind.LessToken or SyntaxKind.GreaterToken or SyntaxKind.LessOrEqualsToken or SyntaxKind.GreaterOrEqualsToken => 5,
            SyntaxKind.EqualsToken or SyntaxKind.NotEqualsToken => 4,
            SyntaxKind.AndKeyword => 3,
            SyntaxKind.OrKeyword => 2,
            _ => 0
        };
    }

    public static long GetBuiltInConstantValue(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.NoneKeyword => 0,
            SyntaxKind.WKeyword => 1,
            SyntaxKind.AKeyword => 2,
            SyntaxKind.SKeyword => 3,
            SyntaxKind.DKeyword => 4,
            SyntaxKind.UpKeyword => 10,
            SyntaxKind.DownKeyword => 11,
            SyntaxKind.LeftKeyword => 12,
            SyntaxKind.RightKeyword => 13,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }
}

public sealed class SyntaxToken
{
    public SyntaxToken(SyntaxKind kind, int position, string text, object? value = null)
    {
        Kind = kind;
        Position = position;
        Text = text;
        Value = value;
    }

    public SyntaxKind Kind { get; }
    public int Position { get; }
    public string Text { get; }
    public object? Value { get; }
    public TextSpan Span => new(Position, Text.Length);
}

public abstract class SyntaxNode
{
    public abstract TextSpan Span { get; }
}

public sealed class CompilationUnitSyntax : SyntaxNode
{
    public CompilationUnitSyntax(IReadOnlyList<StatementSyntax> statements, SyntaxToken endOfFileToken)
    {
        Statements = statements;
        EndOfFileToken = endOfFileToken;
    }

    public IReadOnlyList<StatementSyntax> Statements { get; }
    public SyntaxToken EndOfFileToken { get; }
    public override TextSpan Span => Statements.Count == 0
        ? EndOfFileToken.Span
        : TextSpan.FromBounds(Statements[0].Span.Start, EndOfFileToken.Span.End);
}

public abstract class StatementSyntax : SyntaxNode { }
public abstract class ExpressionSyntax : SyntaxNode { }

public sealed class AssignmentTargetSyntax : SyntaxNode
{
    public AssignmentTargetSyntax(SyntaxToken identifier, SyntaxToken? openBracket, ExpressionSyntax? index, SyntaxToken? closeBracket)
    {
        Identifier = identifier;
        OpenBracket = openBracket;
        Index = index;
        CloseBracket = closeBracket;
    }

    public SyntaxToken Identifier { get; }
    public SyntaxToken? OpenBracket { get; }
    public ExpressionSyntax? Index { get; }
    public SyntaxToken? CloseBracket { get; }
    public bool IsArrayElement => Index != null;
    public override TextSpan Span => TextSpan.FromBounds(Identifier.Span.Start, CloseBracket?.Span.End ?? Identifier.Span.End);
}

public sealed class AssignmentStatementSyntax : StatementSyntax
{
    public AssignmentStatementSyntax(AssignmentTargetSyntax target, SyntaxToken equalsToken, ExpressionSyntax expression)
    {
        Target = target;
        EqualsToken = equalsToken;
        Expression = expression;
    }

    public AssignmentTargetSyntax Target { get; }
    public SyntaxToken EqualsToken { get; }
    public ExpressionSyntax Expression { get; }
    public override TextSpan Span => TextSpan.FromBounds(Target.Span.Start, Expression.Span.End);
}

public sealed class DimStatementSyntax : StatementSyntax
{
    public DimStatementSyntax(SyntaxToken dimKeyword, SyntaxToken identifier, SyntaxToken openBracket, SyntaxToken size, SyntaxToken closeBracket)
    {
        DimKeyword = dimKeyword;
        Identifier = identifier;
        OpenBracket = openBracket;
        Size = size;
        CloseBracket = closeBracket;
    }

    public SyntaxToken DimKeyword { get; }
    public SyntaxToken Identifier { get; }
    public SyntaxToken OpenBracket { get; }
    public SyntaxToken Size { get; }
    public SyntaxToken CloseBracket { get; }
    public override TextSpan Span => TextSpan.FromBounds(DimKeyword.Span.Start, CloseBracket.Span.End);
}

public sealed class PrintStatementSyntax : StatementSyntax
{
    public PrintStatementSyntax(SyntaxToken printKeyword, IReadOnlyList<ExpressionSyntax> items, bool suppressNewLine, int end)
    {
        PrintKeyword = printKeyword;
        Items = items;
        SuppressNewLine = suppressNewLine;
        _end = end;
    }

    private readonly int _end;
    public SyntaxToken PrintKeyword { get; }
    public IReadOnlyList<ExpressionSyntax> Items { get; }
    public bool SuppressNewLine { get; }
    public override TextSpan Span => TextSpan.FromBounds(PrintKeyword.Span.Start, _end);
}

public sealed class GetKeyStatementSyntax : StatementSyntax
{
    public GetKeyStatementSyntax(SyntaxToken getKeyword, SyntaxToken keyKeyword, SyntaxToken identifier)
    {
        GetKeyword = getKeyword;
        KeyKeyword = keyKeyword;
        Identifier = identifier;
    }

    public SyntaxToken GetKeyword { get; }
    public SyntaxToken KeyKeyword { get; }
    public SyntaxToken Identifier { get; }
    public override TextSpan Span => TextSpan.FromBounds(GetKeyword.Span.Start, Identifier.Span.End);
}

public sealed class ClearScreenStatementSyntax : StatementSyntax
{
    public ClearScreenStatementSyntax(SyntaxToken clearKeyword, SyntaxToken screenKeyword)
    {
        ClearKeyword = clearKeyword;
        ScreenKeyword = screenKeyword;
    }

    public SyntaxToken ClearKeyword { get; }
    public SyntaxToken ScreenKeyword { get; }
    public override TextSpan Span => TextSpan.FromBounds(ClearKeyword.Span.Start, ScreenKeyword.Span.End);
}

public sealed class WaitStatementSyntax : StatementSyntax
{
    public WaitStatementSyntax(SyntaxToken waitKeyword, ExpressionSyntax duration, SyntaxToken millisecondsKeyword)
    {
        WaitKeyword = waitKeyword;
        Duration = duration;
        MillisecondsKeyword = millisecondsKeyword;
    }

    public SyntaxToken WaitKeyword { get; }
    public ExpressionSyntax Duration { get; }
    public SyntaxToken MillisecondsKeyword { get; }
    public override TextSpan Span => TextSpan.FromBounds(WaitKeyword.Span.Start, MillisecondsKeyword.Span.End);
}

public sealed class RandomStatementSyntax : StatementSyntax
{
    public RandomStatementSyntax(SyntaxToken randomKeyword, SyntaxToken identifier, SyntaxToken fromKeyword, ExpressionSyntax minimum, SyntaxToken toKeyword, ExpressionSyntax maximum)
    {
        RandomKeyword = randomKeyword;
        Identifier = identifier;
        FromKeyword = fromKeyword;
        Minimum = minimum;
        ToKeyword = toKeyword;
        Maximum = maximum;
    }

    public SyntaxToken RandomKeyword { get; }
    public SyntaxToken Identifier { get; }
    public SyntaxToken FromKeyword { get; }
    public ExpressionSyntax Minimum { get; }
    public SyntaxToken ToKeyword { get; }
    public ExpressionSyntax Maximum { get; }
    public override TextSpan Span => TextSpan.FromBounds(RandomKeyword.Span.Start, Maximum.Span.End);
}

public sealed class IfClauseSyntax : SyntaxNode
{
    public IfClauseSyntax(ExpressionSyntax condition, IReadOnlyList<StatementSyntax> statements)
    {
        Condition = condition;
        Statements = statements;
    }

    public ExpressionSyntax Condition { get; }
    public IReadOnlyList<StatementSyntax> Statements { get; }
    public override TextSpan Span => Statements.Count == 0 ? Condition.Span : TextSpan.FromBounds(Condition.Span.Start, Statements[Statements.Count - 1].Span.End);
}

public sealed class IfStatementSyntax : StatementSyntax
{
    public IfStatementSyntax(SyntaxToken ifKeyword, IReadOnlyList<IfClauseSyntax> clauses, IReadOnlyList<StatementSyntax> elseStatements, SyntaxToken endKeyword, SyntaxToken finalIfKeyword)
    {
        IfKeyword = ifKeyword;
        Clauses = clauses;
        ElseStatements = elseStatements;
        EndKeyword = endKeyword;
        FinalIfKeyword = finalIfKeyword;
    }

    public SyntaxToken IfKeyword { get; }
    public IReadOnlyList<IfClauseSyntax> Clauses { get; }
    public IReadOnlyList<StatementSyntax> ElseStatements { get; }
    public SyntaxToken EndKeyword { get; }
    public SyntaxToken FinalIfKeyword { get; }
    public override TextSpan Span => TextSpan.FromBounds(IfKeyword.Span.Start, FinalIfKeyword.Span.End);
}

public sealed class ForStatementSyntax : StatementSyntax
{
    public ForStatementSyntax(SyntaxToken forKeyword, SyntaxToken identifier, ExpressionSyntax lowerBound, bool isDescending, ExpressionSyntax upperBound, IReadOnlyList<StatementSyntax> statements, SyntaxToken finalForKeyword)
    {
        ForKeyword = forKeyword;
        Identifier = identifier;
        LowerBound = lowerBound;
        IsDescending = isDescending;
        UpperBound = upperBound;
        Statements = statements;
        FinalForKeyword = finalForKeyword;
    }

    public SyntaxToken ForKeyword { get; }
    public SyntaxToken Identifier { get; }
    public ExpressionSyntax LowerBound { get; }
    public bool IsDescending { get; }
    public ExpressionSyntax UpperBound { get; }
    public IReadOnlyList<StatementSyntax> Statements { get; }
    public SyntaxToken FinalForKeyword { get; }
    public override TextSpan Span => TextSpan.FromBounds(ForKeyword.Span.Start, FinalForKeyword.Span.End);
}

public sealed class DoUntilStatementSyntax : StatementSyntax
{
    public DoUntilStatementSyntax(SyntaxToken doKeyword, IReadOnlyList<StatementSyntax> statements, ExpressionSyntax condition)
    {
        DoKeyword = doKeyword;
        Statements = statements;
        Condition = condition;
    }

    public SyntaxToken DoKeyword { get; }
    public IReadOnlyList<StatementSyntax> Statements { get; }
    public ExpressionSyntax Condition { get; }
    public override TextSpan Span => TextSpan.FromBounds(DoKeyword.Span.Start, Condition.Span.End);
}

public sealed class LiteralExpressionSyntax : ExpressionSyntax
{
    public LiteralExpressionSyntax(SyntaxToken literalToken, object value)
    {
        LiteralToken = literalToken;
        Value = value;
    }

    public SyntaxToken LiteralToken { get; }
    public object Value { get; }
    public override TextSpan Span => LiteralToken.Span;
}

public sealed class NameExpressionSyntax : ExpressionSyntax
{
    public NameExpressionSyntax(SyntaxToken identifier) => Identifier = identifier;
    public SyntaxToken Identifier { get; }
    public override TextSpan Span => Identifier.Span;
}

public sealed class ArrayAccessExpressionSyntax : ExpressionSyntax
{
    public ArrayAccessExpressionSyntax(SyntaxToken identifier, ExpressionSyntax index, SyntaxToken closeBracket)
    {
        Identifier = identifier;
        Index = index;
        CloseBracket = closeBracket;
    }

    public SyntaxToken Identifier { get; }
    public ExpressionSyntax Index { get; }
    public SyntaxToken CloseBracket { get; }
    public override TextSpan Span => TextSpan.FromBounds(Identifier.Span.Start, CloseBracket.Span.End);
}

public sealed class UnaryExpressionSyntax : ExpressionSyntax
{
    public UnaryExpressionSyntax(SyntaxToken operatorToken, ExpressionSyntax operand)
    {
        OperatorToken = operatorToken;
        Operand = operand;
    }

    public SyntaxToken OperatorToken { get; }
    public ExpressionSyntax Operand { get; }
    public override TextSpan Span => TextSpan.FromBounds(OperatorToken.Span.Start, Operand.Span.End);
}

public sealed class BinaryExpressionSyntax : ExpressionSyntax
{
    public BinaryExpressionSyntax(ExpressionSyntax left, SyntaxToken operatorToken, ExpressionSyntax right)
    {
        Left = left;
        OperatorToken = operatorToken;
        Right = right;
    }

    public ExpressionSyntax Left { get; }
    public SyntaxToken OperatorToken { get; }
    public ExpressionSyntax Right { get; }
    public override TextSpan Span => TextSpan.FromBounds(Left.Span.Start, Right.Span.End);
}

public sealed class ParenthesizedExpressionSyntax : ExpressionSyntax
{
    public ParenthesizedExpressionSyntax(SyntaxToken openParenthesis, ExpressionSyntax expression, SyntaxToken closeParenthesis)
    {
        OpenParenthesis = openParenthesis;
        Expression = expression;
        CloseParenthesis = closeParenthesis;
    }

    public SyntaxToken OpenParenthesis { get; }
    public ExpressionSyntax Expression { get; }
    public SyntaxToken CloseParenthesis { get; }
    public override TextSpan Span => TextSpan.FromBounds(OpenParenthesis.Span.Start, CloseParenthesis.Span.End);
}
