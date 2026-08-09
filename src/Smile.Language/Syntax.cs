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
    StarToken,
    SlashToken,
    CommaToken,
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
    ConstKeyword,
    ModKeyword,
    SubKeyword,
    CallKeyword,
    FunctionKeyword,
    ReturnKeyword,
    SelectKeyword,
    CaseKeyword,
    ExitKeyword,
    ProgramKeyword,
    TimerKeyword,
    RgbKeyword,
    AbsKeyword,
    MinKeyword,
    MaxKeyword,
    GameClosedKeyword,
    KeyHeldKeyword,
    GameKeyword,
    WindowKeyword,
    SizeKeyword,
    ByKeyword,
    FillKeyword,
    DrawKeyword,
    RectangleKeyword,
    RoundedKeyword,
    CircleKeyword,
    QuadrilateralKeyword,
    LineKeyword,
    TextKeyword,
    NumberKeyword,
    AtKeyword,
    ColorKeyword,
    CenteredKeyword,
    ShowKeyword,
    PlayKeyword,
    SoundKeyword,
    MusicKeyword,
    PauseKeyword,
    ResumeKeyword,
    VolumeKeyword,
    StopKeyword,
    LoadKeyword,
    FileKeyword,
    IntoKeyword,
    CountKeyword,
    SaveKeyword,
    DefaultKeyword,
    NoneKeyword,
    WKeyword,
    AKeyword,
    SKeyword,
    DKeyword,
    UpKeyword,
    LeftKeyword,
    RightKeyword,
    KeyNoneKeyword,
    KeyWKeyword,
    KeyAKeyword,
    KeySKeyword,
    KeyDKeyword,
    KeyUpKeyword,
    KeyDownKeyword,
    KeyLeftKeyword,
    KeyRightKeyword,
    KeyEnterKeyword,
    KeyEscapeKeyword,
    KeySpaceKeyword,
    Key1Keyword,
    Key2Keyword,
    KeyOtherKeyword,
    BlackKeyword,
    WhiteKeyword,
    RedKeyword,
    GreenKeyword,
    BlueKeyword,
    CyanKeyword,
    MagentaKeyword,
    YellowKeyword,
    OrangeKeyword,
    GrayKeyword,
    DarkRedKeyword,
    DarkGreenKeyword,
    DarkBlueKeyword,
    DarkGrayKeyword,
    LightRedKeyword,
    LightGreenKeyword,
    LightBlueKeyword,
    LightGrayKeyword,
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
        ["CONST"] = SyntaxKind.ConstKeyword,
        ["MOD"] = SyntaxKind.ModKeyword,
        ["SUB"] = SyntaxKind.SubKeyword,
        ["CALL"] = SyntaxKind.CallKeyword,
        ["FUNCTION"] = SyntaxKind.FunctionKeyword,
        ["RETURN"] = SyntaxKind.ReturnKeyword,
        ["SELECT"] = SyntaxKind.SelectKeyword,
        ["CASE"] = SyntaxKind.CaseKeyword,
        ["EXIT"] = SyntaxKind.ExitKeyword,
        ["PROGRAM"] = SyntaxKind.ProgramKeyword,
        ["TIMER"] = SyntaxKind.TimerKeyword,
        ["RGB"] = SyntaxKind.RgbKeyword,
        ["ABS"] = SyntaxKind.AbsKeyword,
        ["MIN"] = SyntaxKind.MinKeyword,
        ["MAX"] = SyntaxKind.MaxKeyword,
        ["GAME_CLOSED"] = SyntaxKind.GameClosedKeyword,
        ["KEY_HELD"] = SyntaxKind.KeyHeldKeyword,
        ["GAME"] = SyntaxKind.GameKeyword,
        ["WINDOW"] = SyntaxKind.WindowKeyword,
        ["SIZE"] = SyntaxKind.SizeKeyword,
        ["BY"] = SyntaxKind.ByKeyword,
        ["FILL"] = SyntaxKind.FillKeyword,
        ["DRAW"] = SyntaxKind.DrawKeyword,
        ["RECTANGLE"] = SyntaxKind.RectangleKeyword,
        ["ROUNDED"] = SyntaxKind.RoundedKeyword,
        ["CIRCLE"] = SyntaxKind.CircleKeyword,
        ["QUADRILATERAL"] = SyntaxKind.QuadrilateralKeyword,
        ["LINE"] = SyntaxKind.LineKeyword,
        ["TEXT"] = SyntaxKind.TextKeyword,
        ["NUMBER"] = SyntaxKind.NumberKeyword,
        ["AT"] = SyntaxKind.AtKeyword,
        ["COLOR"] = SyntaxKind.ColorKeyword,
        ["CENTERED"] = SyntaxKind.CenteredKeyword,
        ["SHOW"] = SyntaxKind.ShowKeyword,
        ["PLAY"] = SyntaxKind.PlayKeyword,
        ["SOUND"] = SyntaxKind.SoundKeyword,
        ["MUSIC"] = SyntaxKind.MusicKeyword,
        ["PAUSE"] = SyntaxKind.PauseKeyword,
        ["RESUME"] = SyntaxKind.ResumeKeyword,
        ["VOLUME"] = SyntaxKind.VolumeKeyword,
        ["STOP"] = SyntaxKind.StopKeyword,
        ["LOAD"] = SyntaxKind.LoadKeyword,
        ["FILE"] = SyntaxKind.FileKeyword,
        ["INTO"] = SyntaxKind.IntoKeyword,
        ["COUNT"] = SyntaxKind.CountKeyword,
        ["SAVE"] = SyntaxKind.SaveKeyword,
        ["DEFAULT"] = SyntaxKind.DefaultKeyword,
        ["NONE"] = SyntaxKind.NoneKeyword,
        ["W"] = SyntaxKind.WKeyword,
        ["A"] = SyntaxKind.AKeyword,
        ["S"] = SyntaxKind.SKeyword,
        ["D"] = SyntaxKind.DKeyword,
        ["UP"] = SyntaxKind.UpKeyword,
        ["LEFT"] = SyntaxKind.LeftKeyword,
        ["RIGHT"] = SyntaxKind.RightKeyword,
        ["KEY_NONE"] = SyntaxKind.KeyNoneKeyword,
        ["KEY_W"] = SyntaxKind.KeyWKeyword,
        ["KEY_A"] = SyntaxKind.KeyAKeyword,
        ["KEY_S"] = SyntaxKind.KeySKeyword,
        ["KEY_D"] = SyntaxKind.KeyDKeyword,
        ["KEY_UP"] = SyntaxKind.KeyUpKeyword,
        ["KEY_DOWN"] = SyntaxKind.KeyDownKeyword,
        ["KEY_LEFT"] = SyntaxKind.KeyLeftKeyword,
        ["KEY_RIGHT"] = SyntaxKind.KeyRightKeyword,
        ["KEY_ENTER"] = SyntaxKind.KeyEnterKeyword,
        ["KEY_ESCAPE"] = SyntaxKind.KeyEscapeKeyword,
        ["KEY_SPACE"] = SyntaxKind.KeySpaceKeyword,
        ["KEY_1"] = SyntaxKind.Key1Keyword,
        ["KEY_2"] = SyntaxKind.Key2Keyword,
        ["KEY_OTHER"] = SyntaxKind.KeyOtherKeyword,
        ["BLACK"] = SyntaxKind.BlackKeyword,
        ["WHITE"] = SyntaxKind.WhiteKeyword,
        ["RED"] = SyntaxKind.RedKeyword,
        ["GREEN"] = SyntaxKind.GreenKeyword,
        ["BLUE"] = SyntaxKind.BlueKeyword,
        ["CYAN"] = SyntaxKind.CyanKeyword,
        ["MAGENTA"] = SyntaxKind.MagentaKeyword,
        ["YELLOW"] = SyntaxKind.YellowKeyword,
        ["ORANGE"] = SyntaxKind.OrangeKeyword,
        ["GRAY"] = SyntaxKind.GrayKeyword,
        ["DARK_RED"] = SyntaxKind.DarkRedKeyword,
        ["DARK_GREEN"] = SyntaxKind.DarkGreenKeyword,
        ["DARK_BLUE"] = SyntaxKind.DarkBlueKeyword,
        ["DARK_GRAY"] = SyntaxKind.DarkGrayKeyword,
        ["LIGHT_RED"] = SyntaxKind.LightRedKeyword,
        ["LIGHT_GREEN"] = SyntaxKind.LightGreenKeyword,
        ["LIGHT_BLUE"] = SyntaxKind.LightBlueKeyword,
        ["LIGHT_GRAY"] = SyntaxKind.LightGrayKeyword,
    };

    public static SyntaxKind GetKeywordKind(string text) =>
        Keywords.TryGetValue(text, out var kind) ? kind : SyntaxKind.IdentifierToken;

    public static bool IsKeyword(SyntaxKind kind) =>
        kind >= SyntaxKind.DimKeyword && kind <= SyntaxKind.DefaultKeyword;

    public static bool IsBuiltInConstant(SyntaxKind kind) =>
        kind >= SyntaxKind.NoneKeyword && kind <= SyntaxKind.LightGrayKeyword || kind == SyntaxKind.DownKeyword;

    public static bool IsBuiltInFunction(SyntaxKind kind) =>
        kind >= SyntaxKind.TimerKeyword && kind <= SyntaxKind.KeyHeldKeyword;

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
            SyntaxKind.StarToken => "*",
            SyntaxKind.SlashToken => "/",
            SyntaxKind.CommaToken => ",",
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
        kind == SyntaxKind.MinusToken || kind == SyntaxKind.NotKeyword ? 8 : 0;

    public static int GetBinaryPrecedence(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.StarToken or SyntaxKind.SlashToken or SyntaxKind.ModKeyword => 7,
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
            SyntaxKind.KeyNoneKeyword => 0,
            SyntaxKind.KeyWKeyword => 1,
            SyntaxKind.KeyAKeyword => 2,
            SyntaxKind.KeySKeyword => 3,
            SyntaxKind.KeyDKeyword => 4,
            SyntaxKind.KeyUpKeyword => 10,
            SyntaxKind.KeyDownKeyword => 11,
            SyntaxKind.KeyLeftKeyword => 12,
            SyntaxKind.KeyRightKeyword => 13,
            SyntaxKind.KeyEnterKeyword => 14,
            SyntaxKind.KeyEscapeKeyword => 15,
            SyntaxKind.KeySpaceKeyword => 16,
            SyntaxKind.Key1Keyword => 17,
            SyntaxKind.Key2Keyword => 18,
            SyntaxKind.KeyOtherKeyword => 19,
            SyntaxKind.BlackKeyword => 0x000000,
            SyntaxKind.WhiteKeyword => 0xFFFFFF,
            SyntaxKind.RedKeyword => 0x0000FF,
            SyntaxKind.GreenKeyword => 0x00FF00,
            SyntaxKind.BlueKeyword => 0xFF0000,
            SyntaxKind.CyanKeyword => 0xFFFF00,
            SyntaxKind.MagentaKeyword => 0xFF00FF,
            SyntaxKind.YellowKeyword => 0x00FFFF,
            SyntaxKind.OrangeKeyword => 0x0080FF,
            SyntaxKind.GrayKeyword => 0x808080,
            SyntaxKind.DarkRedKeyword => 0x000080,
            SyntaxKind.DarkGreenKeyword => 0x008000,
            SyntaxKind.DarkBlueKeyword => 0x800000,
            SyntaxKind.DarkGrayKeyword => 0x404040,
            SyntaxKind.LightRedKeyword => 0x8080FF,
            SyntaxKind.LightGreenKeyword => 0x80FF80,
            SyntaxKind.LightBlueKeyword => 0xFF8080,
            SyntaxKind.LightGrayKeyword => 0xC0C0C0,
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
    public AssignmentTargetSyntax(SyntaxToken identifier, SyntaxToken? openBracket, IReadOnlyList<ExpressionSyntax> indices, SyntaxToken? closeBracket)
    {
        Identifier = identifier;
        OpenBracket = openBracket;
        Indices = indices;
        CloseBracket = closeBracket;
    }

    public SyntaxToken Identifier { get; }
    public SyntaxToken? OpenBracket { get; }
    public IReadOnlyList<ExpressionSyntax> Indices { get; }
    public SyntaxToken? CloseBracket { get; }
    public bool IsArrayElement => Indices.Count != 0;
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
    public DimStatementSyntax(SyntaxToken dimKeyword, SyntaxToken identifier, SyntaxToken openBracket, IReadOnlyList<ExpressionSyntax> sizes, SyntaxToken closeBracket)
    {
        DimKeyword = dimKeyword;
        Identifier = identifier;
        OpenBracket = openBracket;
        Sizes = sizes;
        CloseBracket = closeBracket;
    }

    public SyntaxToken DimKeyword { get; }
    public SyntaxToken Identifier { get; }
    public SyntaxToken OpenBracket { get; }
    public IReadOnlyList<ExpressionSyntax> Sizes { get; }
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
    public ArrayAccessExpressionSyntax(SyntaxToken identifier, IReadOnlyList<ExpressionSyntax> indices, SyntaxToken closeBracket)
    {
        Identifier = identifier;
        Indices = indices;
        CloseBracket = closeBracket;
    }

    public SyntaxToken Identifier { get; }
    public IReadOnlyList<ExpressionSyntax> Indices { get; }
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
