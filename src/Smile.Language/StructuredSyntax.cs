using System.Collections.Generic;

namespace Smile.Language;

public sealed class ConstStatementSyntax : StatementSyntax
{
    public ConstStatementSyntax(SyntaxToken constKeyword, SyntaxToken identifier, SyntaxToken equalsToken, ExpressionSyntax expression)
    {
        ConstKeyword = constKeyword;
        Identifier = identifier;
        EqualsToken = equalsToken;
        Expression = expression;
    }

    public SyntaxToken ConstKeyword { get; }
    public SyntaxToken Identifier { get; }
    public SyntaxToken EqualsToken { get; }
    public ExpressionSyntax Expression { get; }
    public override TextSpan Span => TextSpan.FromBounds(ConstKeyword.Span.Start, Expression.Span.End);
}

public sealed class RoutineDeclarationSyntax : StatementSyntax
{
    public RoutineDeclarationSyntax(SyntaxToken keyword, SyntaxToken identifier, IReadOnlyList<SyntaxToken> parameters,
        IReadOnlyList<StatementSyntax> statements, SyntaxToken endKeyword, SyntaxToken finalKeyword)
    {
        Keyword = keyword;
        Identifier = identifier;
        Parameters = parameters;
        Statements = statements;
        EndKeyword = endKeyword;
        FinalKeyword = finalKeyword;
    }

    public SyntaxToken Keyword { get; }
    public SyntaxToken Identifier { get; }
    public IReadOnlyList<SyntaxToken> Parameters { get; }
    public IReadOnlyList<StatementSyntax> Statements { get; }
    public SyntaxToken EndKeyword { get; }
    public SyntaxToken FinalKeyword { get; }
    public bool IsFunction => Keyword.Kind == SyntaxKind.FunctionKeyword;
    public override TextSpan Span => TextSpan.FromBounds(Keyword.Span.Start, FinalKeyword.Span.End);
}

public sealed class CallStatementSyntax : StatementSyntax
{
    public CallStatementSyntax(SyntaxToken callKeyword, SyntaxToken identifier, IReadOnlyList<ExpressionSyntax> arguments, SyntaxToken closeParenthesis)
    {
        CallKeyword = callKeyword;
        Identifier = identifier;
        Arguments = arguments;
        CloseParenthesis = closeParenthesis;
    }

    public SyntaxToken CallKeyword { get; }
    public SyntaxToken Identifier { get; }
    public IReadOnlyList<ExpressionSyntax> Arguments { get; }
    public SyntaxToken CloseParenthesis { get; }
    public override TextSpan Span => TextSpan.FromBounds(CallKeyword.Span.Start, CloseParenthesis.Span.End);
}

public sealed class ReturnStatementSyntax : StatementSyntax
{
    public ReturnStatementSyntax(SyntaxToken returnKeyword, ExpressionSyntax? expression)
    {
        ReturnKeyword = returnKeyword;
        Expression = expression;
    }

    public SyntaxToken ReturnKeyword { get; }
    public ExpressionSyntax? Expression { get; }
    public override TextSpan Span => Expression == null
        ? ReturnKeyword.Span
        : TextSpan.FromBounds(ReturnKeyword.Span.Start, Expression.Span.End);
}

public sealed class SelectCaseClauseSyntax : SyntaxNode
{
    public SelectCaseClauseSyntax(SyntaxToken caseKeyword, ExpressionSyntax? value, bool isElse, IReadOnlyList<StatementSyntax> statements)
    {
        CaseKeyword = caseKeyword;
        Value = value;
        IsElse = isElse;
        Statements = statements;
    }

    public SyntaxToken CaseKeyword { get; }
    public ExpressionSyntax? Value { get; }
    public bool IsElse { get; }
    public IReadOnlyList<StatementSyntax> Statements { get; }
    public override TextSpan Span => Statements.Count == 0
        ? (Value?.Span ?? CaseKeyword.Span)
        : TextSpan.FromBounds(CaseKeyword.Span.Start, Statements[Statements.Count - 1].Span.End);
}

public sealed class SelectStatementSyntax : StatementSyntax
{
    public SelectStatementSyntax(SyntaxToken selectKeyword, ExpressionSyntax expression, IReadOnlyList<SelectCaseClauseSyntax> cases,
        SyntaxToken endKeyword, SyntaxToken finalSelectKeyword)
    {
        SelectKeyword = selectKeyword;
        Expression = expression;
        Cases = cases;
        EndKeyword = endKeyword;
        FinalSelectKeyword = finalSelectKeyword;
    }

    public SyntaxToken SelectKeyword { get; }
    public ExpressionSyntax Expression { get; }
    public IReadOnlyList<SelectCaseClauseSyntax> Cases { get; }
    public SyntaxToken EndKeyword { get; }
    public SyntaxToken FinalSelectKeyword { get; }
    public override TextSpan Span => TextSpan.FromBounds(SelectKeyword.Span.Start, FinalSelectKeyword.Span.End);
}

public sealed class DoStatementSyntax : StatementSyntax
{
    public DoStatementSyntax(SyntaxToken doKeyword, IReadOnlyList<StatementSyntax> statements, SyntaxToken loopKeyword, ExpressionSyntax? untilCondition)
    {
        DoKeyword = doKeyword;
        Statements = statements;
        LoopKeyword = loopKeyword;
        UntilCondition = untilCondition;
    }

    public SyntaxToken DoKeyword { get; }
    public IReadOnlyList<StatementSyntax> Statements { get; }
    public SyntaxToken LoopKeyword { get; }
    public ExpressionSyntax? UntilCondition { get; }
    public override TextSpan Span => TextSpan.FromBounds(DoKeyword.Span.Start, UntilCondition?.Span.End ?? LoopKeyword.Span.End);
}

public sealed class ExitStatementSyntax : StatementSyntax
{
    public ExitStatementSyntax(SyntaxToken exitKeyword, SyntaxToken targetKeyword)
    {
        ExitKeyword = exitKeyword;
        TargetKeyword = targetKeyword;
    }

    public SyntaxToken ExitKeyword { get; }
    public SyntaxToken TargetKeyword { get; }
    public override TextSpan Span => TextSpan.FromBounds(ExitKeyword.Span.Start, TargetKeyword.Span.End);
}

public sealed class EndProgramStatementSyntax : StatementSyntax
{
    public EndProgramStatementSyntax(SyntaxToken endKeyword, SyntaxToken programKeyword)
    {
        EndKeyword = endKeyword;
        ProgramKeyword = programKeyword;
    }

    public SyntaxToken EndKeyword { get; }
    public SyntaxToken ProgramKeyword { get; }
    public override TextSpan Span => TextSpan.FromBounds(EndKeyword.Span.Start, ProgramKeyword.Span.End);
}

public sealed class CallExpressionSyntax : ExpressionSyntax
{
    public CallExpressionSyntax(SyntaxToken identifier, IReadOnlyList<ExpressionSyntax> arguments, SyntaxToken closeParenthesis)
    {
        Identifier = identifier;
        Arguments = arguments;
        CloseParenthesis = closeParenthesis;
    }

    public SyntaxToken Identifier { get; }
    public IReadOnlyList<ExpressionSyntax> Arguments { get; }
    public SyntaxToken CloseParenthesis { get; }
    public override TextSpan Span => TextSpan.FromBounds(Identifier.Span.Start, CloseParenthesis.Span.End);
}
