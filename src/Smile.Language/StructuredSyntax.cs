using System.Collections.Generic;
using System.Linq;

namespace Smile.Language;

public enum ModuleVisibility
{
    Private,
    Public
}

public sealed class OptionExplicitStatementSyntax : StatementSyntax
{
    public OptionExplicitStatementSyntax(SyntaxToken optionKeyword, SyntaxToken explicitKeyword)
    {
        OptionKeyword = optionKeyword;
        ExplicitKeyword = explicitKeyword;
    }

    public SyntaxToken OptionKeyword { get; }
    public SyntaxToken ExplicitKeyword { get; }
    public override TextSpan Span => TextSpan.FromBounds(OptionKeyword.Span.Start, ExplicitKeyword.Span.End);
}

public sealed class RecordFieldDeclarationSyntax : SyntaxNode
{
    public RecordFieldDeclarationSyntax(SyntaxToken identifier, SyntaxToken asKeyword, SyntaxToken typeToken)
    {
        Identifier = identifier;
        AsKeyword = asKeyword;
        TypeToken = typeToken;
    }

    public SyntaxToken Identifier { get; }
    public SyntaxToken AsKeyword { get; }
    public SyntaxToken TypeToken { get; }
    public override TextSpan Span => TextSpan.FromBounds(Identifier.Span.Start, TypeToken.Span.End);
}

public sealed class TypeDeclarationSyntax : StatementSyntax
{
    public TypeDeclarationSyntax(SyntaxToken typeKeyword, SyntaxToken identifier,
        IReadOnlyList<RecordFieldDeclarationSyntax> fields, SyntaxToken endKeyword, SyntaxToken finalTypeKeyword)
    {
        TypeKeyword = typeKeyword;
        Identifier = identifier;
        Fields = fields;
        EndKeyword = endKeyword;
        FinalTypeKeyword = finalTypeKeyword;
    }

    public SyntaxToken TypeKeyword { get; }
    public SyntaxToken Identifier { get; }
    public IReadOnlyList<RecordFieldDeclarationSyntax> Fields { get; }
    public SyntaxToken EndKeyword { get; }
    public SyntaxToken FinalTypeKeyword { get; }
    public override TextSpan Span => TextSpan.FromBounds(TypeKeyword.Span.Start, FinalTypeKeyword.Span.End);
}

public sealed class ParameterSyntax : SyntaxNode
{
    public ParameterSyntax(SyntaxToken? modeKeyword, SyntaxToken identifier, SyntaxToken? asKeyword,
        SyntaxToken? typeToken)
    {
        ModeKeyword = modeKeyword;
        Identifier = identifier;
        AsKeyword = asKeyword;
        TypeToken = typeToken;
    }

    public SyntaxToken? ModeKeyword { get; }
    public SyntaxToken Identifier { get; }
    public SyntaxToken? AsKeyword { get; }
    public SyntaxToken? TypeToken { get; }
    public override TextSpan Span => TextSpan.FromBounds(ModeKeyword?.Span.Start ?? Identifier.Span.Start,
        TypeToken?.Span.End ?? Identifier.Span.End);
}

public sealed class DottedNameSyntax : SyntaxNode
{
    public DottedNameSyntax(IReadOnlyList<SyntaxToken> identifiers, IReadOnlyList<SyntaxToken> dots)
    {
        Identifiers = identifiers;
        Dots = dots;
    }

    public IReadOnlyList<SyntaxToken> Identifiers { get; }
    public IReadOnlyList<SyntaxToken> Dots { get; }
    public string Name => string.Join(".", Identifiers.Select(identifier => identifier.Text));
    public override TextSpan Span => Identifiers.Count == 0
        ? new TextSpan(0, 0)
        : TextSpan.FromBounds(Identifiers[0].Span.Start, Identifiers[Identifiers.Count - 1].Span.End);
}

public sealed class ImportStatementSyntax : StatementSyntax
{
    public ImportStatementSyntax(SyntaxToken importKeyword, DottedNameSyntax moduleName, SyntaxToken asKeyword, SyntaxToken alias)
    {
        ImportKeyword = importKeyword;
        ModuleName = moduleName;
        AsKeyword = asKeyword;
        Alias = alias;
    }

    public SyntaxToken ImportKeyword { get; }
    public DottedNameSyntax ModuleName { get; }
    public SyntaxToken AsKeyword { get; }
    public SyntaxToken Alias { get; }
    public override TextSpan Span => TextSpan.FromBounds(ImportKeyword.Span.Start, Alias.Span.End);
}

public sealed class VisibilityDeclarationSyntax : StatementSyntax
{
    public VisibilityDeclarationSyntax(SyntaxToken visibilityKeyword, StatementSyntax declaration)
    {
        VisibilityKeyword = visibilityKeyword;
        Declaration = declaration;
    }

    public SyntaxToken VisibilityKeyword { get; }
    public StatementSyntax Declaration { get; }
    public ModuleVisibility Visibility => VisibilityKeyword.Kind == SyntaxKind.PublicKeyword
        ? ModuleVisibility.Public : ModuleVisibility.Private;
    public override TextSpan Span => TextSpan.FromBounds(VisibilityKeyword.Span.Start, Declaration.Span.End);
}

public sealed class ModuleDeclarationSyntax : StatementSyntax
{
    public ModuleDeclarationSyntax(SyntaxToken moduleKeyword, DottedNameSyntax name, IReadOnlyList<StatementSyntax> statements,
        SyntaxToken endKeyword, SyntaxToken finalModuleKeyword)
    {
        ModuleKeyword = moduleKeyword;
        Name = name;
        Statements = statements;
        EndKeyword = endKeyword;
        FinalModuleKeyword = finalModuleKeyword;
    }

    public SyntaxToken ModuleKeyword { get; }
    public DottedNameSyntax Name { get; }
    public IReadOnlyList<StatementSyntax> Statements { get; }
    public SyntaxToken EndKeyword { get; }
    public SyntaxToken FinalModuleKeyword { get; }
    public override TextSpan Span => TextSpan.FromBounds(ModuleKeyword.Span.Start, FinalModuleKeyword.Span.End);
}

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
    public RoutineDeclarationSyntax(SyntaxToken keyword, SyntaxToken identifier, SyntaxToken? openParenthesis,
        IReadOnlyList<ParameterSyntax> parameters, SyntaxToken? closeParenthesis, SyntaxToken? asKeyword,
        SyntaxToken? returnTypeToken, IReadOnlyList<StatementSyntax> statements, SyntaxToken endKeyword,
        SyntaxToken finalKeyword)
    {
        Keyword = keyword;
        Identifier = identifier;
        OpenParenthesis = openParenthesis;
        Parameters = parameters;
        CloseParenthesis = closeParenthesis;
        AsKeyword = asKeyword;
        ReturnTypeToken = returnTypeToken;
        Statements = statements;
        EndKeyword = endKeyword;
        FinalKeyword = finalKeyword;
    }

    public SyntaxToken Keyword { get; }
    public SyntaxToken Identifier { get; }
    public SyntaxToken? OpenParenthesis { get; }
    public IReadOnlyList<ParameterSyntax> Parameters { get; }
    public SyntaxToken? CloseParenthesis { get; }
    public SyntaxToken? AsKeyword { get; }
    public SyntaxToken? ReturnTypeToken { get; }
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

public sealed class QualifiedCallStatementSyntax : StatementSyntax
{
    public QualifiedCallStatementSyntax(SyntaxToken callKeyword, SyntaxToken alias, SyntaxToken dotToken,
        SyntaxToken member, IReadOnlyList<ExpressionSyntax> arguments, SyntaxToken closeParenthesis)
    {
        CallKeyword = callKeyword;
        Alias = alias;
        DotToken = dotToken;
        Member = member;
        Arguments = arguments;
        CloseParenthesis = closeParenthesis;
    }

    public SyntaxToken CallKeyword { get; }
    public SyntaxToken Alias { get; }
    public SyntaxToken DotToken { get; }
    public SyntaxToken Member { get; }
    public IReadOnlyList<ExpressionSyntax> Arguments { get; }
    public SyntaxToken CloseParenthesis { get; }
    public override TextSpan Span => TextSpan.FromBounds(CallKeyword.Span.Start, CloseParenthesis.Span.End);
}

public sealed class LeadingMemberCallStatementSyntax : StatementSyntax
{
    public LeadingMemberCallStatementSyntax(SyntaxToken callKeyword, SyntaxToken dotToken,
        SyntaxToken member, IReadOnlyList<ExpressionSyntax> arguments, SyntaxToken closeParenthesis)
    {
        CallKeyword = callKeyword;
        DotToken = dotToken;
        Member = member;
        Arguments = arguments;
        CloseParenthesis = closeParenthesis;
    }

    public SyntaxToken CallKeyword { get; }
    public SyntaxToken DotToken { get; }
    public SyntaxToken Member { get; }
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

public sealed class QualifiedNameExpressionSyntax : ExpressionSyntax
{
    public QualifiedNameExpressionSyntax(SyntaxToken alias, SyntaxToken dotToken, SyntaxToken member)
    {
        Alias = alias;
        DotToken = dotToken;
        Member = member;
    }

    public SyntaxToken Alias { get; }
    public SyntaxToken DotToken { get; }
    public SyntaxToken Member { get; }
    public override TextSpan Span => TextSpan.FromBounds(Alias.Span.Start, Member.Span.End);
}

public sealed class QualifiedArrayAccessExpressionSyntax : ExpressionSyntax
{
    public QualifiedArrayAccessExpressionSyntax(SyntaxToken alias, SyntaxToken dotToken, SyntaxToken member,
        IReadOnlyList<ExpressionSyntax> indices, SyntaxToken closeBracket)
    {
        Alias = alias;
        DotToken = dotToken;
        Member = member;
        Indices = indices;
        CloseBracket = closeBracket;
    }

    public SyntaxToken Alias { get; }
    public SyntaxToken DotToken { get; }
    public SyntaxToken Member { get; }
    public IReadOnlyList<ExpressionSyntax> Indices { get; }
    public SyntaxToken CloseBracket { get; }
    public override TextSpan Span => TextSpan.FromBounds(Alias.Span.Start, CloseBracket.Span.End);
}

public sealed class QualifiedCallExpressionSyntax : ExpressionSyntax
{
    public QualifiedCallExpressionSyntax(SyntaxToken alias, SyntaxToken dotToken, SyntaxToken member,
        IReadOnlyList<ExpressionSyntax> arguments, SyntaxToken closeParenthesis)
    {
        Alias = alias;
        DotToken = dotToken;
        Member = member;
        Arguments = arguments;
        CloseParenthesis = closeParenthesis;
    }

    public SyntaxToken Alias { get; }
    public SyntaxToken DotToken { get; }
    public SyntaxToken Member { get; }
    public IReadOnlyList<ExpressionSyntax> Arguments { get; }
    public SyntaxToken CloseParenthesis { get; }
    public override TextSpan Span => TextSpan.FromBounds(Alias.Span.Start, CloseParenthesis.Span.End);
}
