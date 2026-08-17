using System;
using System.Collections.Generic;

namespace Smile.Language;

internal sealed class Parser
{
    private readonly IReadOnlyList<SyntaxToken> _allTokens;
    private readonly List<SyntaxToken> _tokens = new();
    private readonly DiagnosticBag _diagnostics;
    private int _position;
    private int _declarationContinuationDepth;
    private int _expressionContinuationDepth;
    private int _typeDeclarationDepth;

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
            case SyntaxKind.OptionKeyword: return ParseOptionExplicitStatement();
            case SyntaxKind.ModuleKeyword: return ParseModuleDeclaration();
            case SyntaxKind.ImportKeyword: return ParseImportStatement();
            case SyntaxKind.PublicKeyword:
            case SyntaxKind.PrivateKeyword: return ParseVisibilityDeclaration();
            case SyntaxKind.ConstKeyword: return ParseConstStatement();
            case SyntaxKind.TypeKeyword: return ParseTypeDeclaration();
            case SyntaxKind.EnumKeyword: return ParseEnumDeclaration();
            case SyntaxKind.DimKeyword: return ParseDimStatement();
            case SyntaxKind.IfKeyword: return ParseIfStatement();
            case SyntaxKind.WithKeyword: return ParseWithStatement();
            case SyntaxKind.ForKeyword: return ParseForStatement();
            case SyntaxKind.DoKeyword: return ParseDoStatement();
            case SyntaxKind.PrintKeyword: return ParsePrintStatement();
            case SyntaxKind.GetKeyword: return ParseGetKeyStatement();
            case SyntaxKind.ClearKeyword: return ParseClearStatement();
            case SyntaxKind.WaitKeyword: return ParseWaitStatement();
            case SyntaxKind.RandomKeyword: return ParseRandomStatement();
            case SyntaxKind.SubKeyword:
            case SyntaxKind.FunctionKeyword: return ParseRoutineDeclaration();
            case SyntaxKind.CallKeyword: return ParseCallStatement();
            case SyntaxKind.ReturnKeyword: return ParseReturnStatement();
            case SyntaxKind.SelectKeyword: return ParseSelectStatement();
            case SyntaxKind.ExitKeyword: return ParseExitStatement();
            case SyntaxKind.EndKeyword when Peek(1).Kind == SyntaxKind.ProgramKeyword: return ParseEndProgramStatement();
            case SyntaxKind.GameKeyword: return ParseGameWindowStatement();
            case SyntaxKind.ClipKeyword when Peek(1).Kind == SyntaxKind.RectangleKeyword: return ParseClipRectangleStatement();
            case SyntaxKind.FillKeyword: return ParseGraphicsStatement(isFill: true);
            case SyntaxKind.DrawKeyword when Peek(1).Kind == SyntaxKind.ImageKeyword: return ParseDrawImageStatement();
            case SyntaxKind.DrawKeyword: return ParseGraphicsStatement(isFill: false);
            case SyntaxKind.ShowKeyword: return ParseShowScreenStatement();
            case SyntaxKind.PlayKeyword when Peek(1).Kind == SyntaxKind.MusicKeyword: return ParseMusicStatement(MusicOperation.Play);
            case SyntaxKind.PlayKeyword: return ParseSoundStatement(isStop: false);
            case SyntaxKind.PauseKeyword: return ParseMusicStatement(MusicOperation.Pause);
            case SyntaxKind.ResumeKeyword: return ParseMusicStatement(MusicOperation.Resume);
            case SyntaxKind.StopKeyword when Peek(1).Kind == SyntaxKind.MusicKeyword: return ParseMusicStatement(MusicOperation.Stop);
            case SyntaxKind.StopKeyword: return ParseSoundStatement(isStop: true);
            case SyntaxKind.MusicKeyword: return ParseMusicStatement(MusicOperation.SetVolume);
            case SyntaxKind.LoadKeyword when Peek(1).Kind == SyntaxKind.ImageKeyword: return ParseImageLoadStatement(isUnload: false);
            case SyntaxKind.UnloadKeyword when Peek(1).Kind == SyntaxKind.ImageKeyword: return ParseImageLoadStatement(isUnload: true);
            case SyntaxKind.LoadKeyword when Peek(1).Kind == SyntaxKind.DataKeyword: return ParseDataLoadStatement();
            case SyntaxKind.LoadKeyword when Peek(1).Kind == SyntaxKind.TextKeyword: return ParseTextFileLoadStatement();
            case SyntaxKind.LoadKeyword: return ParseLoadStatement();
            case SyntaxKind.SaveKeyword when Peek(1).Kind == SyntaxKind.DataKeyword: return ParseDataSaveStatement();
            case SyntaxKind.SaveKeyword: return ParseSaveStatement();
            case SyntaxKind.IdentifierToken:
            case SyntaxKind.KeyKeyword: return ParseAssignmentStatement();
            case SyntaxKind.MeKeyword: return ParseAssignmentStatement();
            case SyntaxKind.DotToken: return ParseAssignmentStatement();
            case var kind when IsContextualIdentifier(kind): return ParseAssignmentStatement();
            default:
                _diagnostics.Report("SML2002", Current.Span, $"Unexpected token '{Display(Current)}' at the start of a statement.");
                SynchronizeLine();
                return null;
        }
    }

    private OptionExplicitStatementSyntax ParseOptionExplicitStatement()
    {
        var option = MatchToken(SyntaxKind.OptionKeyword);
        var explicitKeyword = MatchToken(SyntaxKind.ExplicitKeyword);
        ConsumeLineEnd();
        return new OptionExplicitStatementSyntax(option, explicitKeyword);
    }

    private ModuleDeclarationSyntax ParseModuleDeclaration()
    {
        var module = MatchToken(SyntaxKind.ModuleKeyword);
        var name = ParseDottedName();
        ConsumeLineEnd();
        var statements = ParseStatementsUntil(() => IsEndPair(SyntaxKind.ModuleKeyword));
        var end = MatchToken(SyntaxKind.EndKeyword);
        var finalModule = MatchToken(SyntaxKind.ModuleKeyword);
        ConsumeLineEnd();
        return new ModuleDeclarationSyntax(module, name, statements, end, finalModule);
    }

    private ImportStatementSyntax ParseImportStatement()
    {
        var import = MatchToken(SyntaxKind.ImportKeyword);
        var moduleName = ParseDottedName();
        var asKeyword = MatchToken(SyntaxKind.AsKeyword);
        var alias = MatchIdentifier();
        ConsumeLineEnd();
        return new ImportStatementSyntax(import, moduleName, asKeyword, alias);
    }

    private DottedNameSyntax ParseDottedName()
    {
        var identifiers = new List<SyntaxToken> { MatchDottedIdentifier() };
        var dots = new List<SyntaxToken>();
        while (Current.Kind == SyntaxKind.DotToken)
        {
            dots.Add(NextToken());
            identifiers.Add(MatchDottedIdentifier());
        }
        return new DottedNameSyntax(identifiers, dots);
    }

    private VisibilityDeclarationSyntax ParseVisibilityDeclaration()
    {
        var visibility = NextToken();
        if (Current.Kind is not (SyntaxKind.ConstKeyword or SyntaxKind.DimKeyword or SyntaxKind.TypeKeyword or SyntaxKind.EnumKeyword or
            SyntaxKind.SubKeyword or SyntaxKind.FunctionKeyword))
        {
            _diagnostics.Report("SML2003", Current.Span,
                "Public or Private must modify a module Const, Dim, Type, Enum, Sub, or Function declaration.");
        }
        var declaration = ParseStatement();
        if (declaration == null)
        {
            var missing = new SyntaxToken(SyntaxKind.NumberToken, Current.Position, string.Empty, 0L);
            declaration = new ConstStatementSyntax(
                new SyntaxToken(SyntaxKind.ConstKeyword, Current.Position, string.Empty),
                new SyntaxToken(SyntaxKind.IdentifierToken, Current.Position, string.Empty),
                new SyntaxToken(SyntaxKind.EqualsToken, Current.Position, string.Empty),
                new LiteralExpressionSyntax(missing, 0L));
        }
        return new VisibilityDeclarationSyntax(visibility, declaration);
    }

    private ConstStatementSyntax ParseConstStatement()
    {
        var keyword = MatchToken(SyntaxKind.ConstKeyword);
        var identifier = MatchIdentifier();
        if (Current.Kind == SyntaxKind.AsKeyword)
        {
            _diagnostics.Report("SML3403", Current.Span,
                "Const declarations cannot have record types; declare a Dim record variable instead.");
            SynchronizeLine();
            var missingEquals = new SyntaxToken(SyntaxKind.EqualsToken, identifier.Span.End, string.Empty);
            var missingValue = new SyntaxToken(SyntaxKind.NumberToken, identifier.Span.End, string.Empty, 0L);
            return new ConstStatementSyntax(keyword, identifier, missingEquals,
                new LiteralExpressionSyntax(missingValue, 0L));
        }
        var equals = MatchToken(SyntaxKind.EqualsToken);
        var expression = ParseExpression();
        ConsumeLineEnd();
        return new ConstStatementSyntax(keyword, identifier, equals, expression);
    }

    private TypeDeclarationSyntax ParseTypeDeclaration()
    {
        var typeKeyword = MatchToken(SyntaxKind.TypeKeyword);
        var identifier = MatchIdentifier();
        ConsumeLineEnd();
        var members = new List<TypeMemberDeclarationSyntax>();
        _typeDeclarationDepth++;
        try
        {
            while (Current.Kind != SyntaxKind.EndOfFileToken && !IsEndPair(SyntaxKind.TypeKeyword))
            {
                if (Current.Kind == SyntaxKind.NewLineToken)
                {
                    NextToken();
                    continue;
                }
                SyntaxToken? visibility = null;
                if (Current.Kind is SyntaxKind.PublicKeyword or SyntaxKind.PrivateKeyword)
                    visibility = NextToken();

                if (visibility != null && Current.Kind == SyntaxKind.NewLineToken &&
                    Peek(1).Kind == SyntaxKind.EndKeyword && Peek(2).Kind == SyntaxKind.TypeKeyword)
                {
                    _diagnostics.Report("SML3440", visibility.Span,
                        "A visibility modifier inside Type must precede a field, method, or Property.");
                    NextToken();
                    continue;
                }
                if (IsEndPair(SyntaxKind.TypeKeyword) || Current.Kind == SyntaxKind.EndOfFileToken)
                {
                    _diagnostics.Report("SML3440", visibility?.Span ?? Current.Span,
                        "A visibility modifier inside Type must precede a field, method, or Property.");
                    continue;
                }

                if (Current.Kind is SyntaxKind.SubKeyword or SyntaxKind.FunctionKeyword)
                {
                    members.Add(new TypeRoutineDeclarationSyntax(visibility, ParseRoutineDeclaration()));
                    continue;
                }

                if (Current.Kind == SyntaxKind.PropertyKeyword)
                {
                    members.Add(ParsePropertyDeclaration(visibility));
                    continue;
                }

                if (visibility?.Kind == SyntaxKind.PrivateKeyword)
                {
                    _diagnostics.Report("SML3440", visibility.Span,
                        "Type fields are always Public and cannot be declared Private.");
                }

                if (!IsIdentifierLike(Current.Kind))
                {
                    _diagnostics.Report("SML3403", Current.Span,
                        "Type members must be fields, Sub or Function methods, or Property declarations.");
                    SynchronizeLine();
                    continue;
                }
                var field = MatchIdentifier();
                if (Current.Kind != SyntaxKind.AsKeyword)
                {
                    var unsupported = Current.Kind is SyntaxKind.OpenBracketToken or SyntaxKind.EqualsToken;
                    _diagnostics.Report(unsupported ? "SML3403" : "SML3402", Current.Span, unsupported
                        ? "Record fields cannot be arrays and cannot have initializers."
                        : $"Field '{field.Text}' requires As and a field type.");
                    SynchronizeLine();
                    continue;
                }
                var asKeyword = MatchToken(SyntaxKind.AsKeyword);
                var fieldType = MatchTypeToken();
                members.Add(new RecordFieldDeclarationSyntax(visibility, field, asKeyword, fieldType));
                if (!IsLineEnd(Current.Kind))
                {
                    _diagnostics.Report("SML3403", Current.Span,
                        "Record fields cannot be arrays and cannot have initializers.");
                    SynchronizeLine();
                    continue;
                }
                ConsumeLineEnd();
            }
        }
        finally
        {
            _typeDeclarationDepth--;
        }
        var end = MatchToken(SyntaxKind.EndKeyword);
        var finalType = MatchToken(SyntaxKind.TypeKeyword);
        ConsumeLineEnd();
        return new TypeDeclarationSyntax(typeKeyword, identifier, members, end, finalType);
    }

    private PropertyDeclarationSyntax ParsePropertyDeclaration(SyntaxToken? visibility)
    {
        var propertyKeyword = NextToken();
        var identifier = MatchIdentifier();
        var asKeyword = MatchToken(SyntaxKind.AsKeyword);
        var typeToken = MatchTypeToken();
        ConsumeLineEnd();

        PropertyAccessorDeclarationSyntax? getter = null;
        PropertyAccessorDeclarationSyntax? setter = null;
        while (Current.Kind != SyntaxKind.EndOfFileToken && !IsEndContextualPair("Property") &&
               !IsEndPair(SyntaxKind.TypeKeyword) && !IsTypeMemberStart() && !IsTypeFieldStart())
        {
            if (Current.Kind == SyntaxKind.NewLineToken)
            {
                NextToken();
                continue;
            }

            if (Current.Kind == SyntaxKind.GetKeyword)
            {
                var accessor = ParsePropertyAccessor(PropertyAccessorKind.Get, SyntaxKind.GetKeyword, "Get");
                if (getter != null)
                    _diagnostics.Report("SML3441", accessor.Keyword.Span,
                        $"Property '{identifier.Text}' declares more than one Get accessor.");
                else
                    getter = accessor;
                continue;
            }
            if (Current.Kind == SyntaxKind.SetKeyword)
            {
                var accessor = ParsePropertyAccessor(PropertyAccessorKind.Set, null, "Set");
                if (setter != null)
                    _diagnostics.Report("SML3441", accessor.Keyword.Span,
                        $"Property '{identifier.Text}' declares more than one Set accessor.");
                else
                    setter = accessor;
                continue;
            }

            _diagnostics.Report("SML3441", Current.Span,
                "A Property body may contain only Get and Set accessor blocks.");
            SynchronizeLine();
        }

        var (endKeyword, finalProperty) = MatchContextualEndPair("Property");
        return new PropertyDeclarationSyntax(visibility, propertyKeyword, identifier, asKeyword, typeToken,
            getter, setter, endKeyword, finalProperty);
    }

    private PropertyAccessorDeclarationSyntax ParsePropertyAccessor(PropertyAccessorKind kind,
        SyntaxKind? keywordKind, string keywordText)
    {
        var keyword = keywordKind.HasValue ? MatchToken(keywordKind.Value) : MatchContextualIdentifier(keywordText);
        ConsumeLineEnd();
        var statements = ParseStatementsUntil(() =>
            (keywordKind.HasValue ? IsEndPair(keywordKind.Value) : IsEndContextualPair(keywordText)) ||
            IsPropertyAccessorBoundary());
        SyntaxToken endKeyword;
        SyntaxToken finalKeyword;
        if (keywordKind.HasValue && IsEndPair(keywordKind.Value))
        {
            endKeyword = NextToken();
            finalKeyword = NextToken();
            ConsumeLineEnd();
        }
        else if (!keywordKind.HasValue && IsEndContextualPair(keywordText))
        {
            (endKeyword, finalKeyword) = MatchContextualEndPair(keywordText);
        }
        else
        {
            _diagnostics.Report("SML2001", Current.Span,
                $"Expected End {keywordText} before '{Display(Current)}'.");
            endKeyword = new SyntaxToken(SyntaxKind.EndKeyword, Current.Position, string.Empty);
            finalKeyword = new SyntaxToken(keywordKind ?? SyntaxKind.IdentifierToken, Current.Position,
                string.Empty);
        }
        return new PropertyAccessorDeclarationSyntax(kind, keyword, statements, endKeyword, finalKeyword);
    }

    private EnumDeclarationSyntax ParseEnumDeclaration()
    {
        var enumKeyword = MatchToken(SyntaxKind.EnumKeyword);
        var identifier = MatchIdentifier();
        ConsumeLineEnd();
        var members = new List<EnumMemberDeclarationSyntax>();
        while (Current.Kind != SyntaxKind.EndOfFileToken && !IsEndPair(SyntaxKind.EnumKeyword))
        {
            if (Current.Kind == SyntaxKind.NewLineToken)
            {
                NextToken();
                continue;
            }
            if (!IsMemberIdentifier(Current.Kind))
            {
                _diagnostics.Report("SML3421", Current.Span,
                    "Enum members must use 'Name' or 'Name = constant integer expression'.");
                SynchronizeLine();
                continue;
            }
            var member = MatchMemberIdentifier();
            SyntaxToken? equals = null;
            ExpressionSyntax? value = null;
            if (Current.Kind == SyntaxKind.EqualsToken)
            {
                equals = NextToken();
                value = ParseExpression();
            }
            if (!IsLineEnd(Current.Kind))
            {
                _diagnostics.Report("SML3421", Current.Span,
                    "Enum members must use 'Name' or 'Name = constant integer expression'.");
                SynchronizeLine();
            }
            else
            {
                ConsumeLineEnd();
            }
            members.Add(new EnumMemberDeclarationSyntax(member, equals, value));
        }
        var end = MatchToken(SyntaxKind.EndKeyword);
        var finalEnum = MatchToken(SyntaxKind.EnumKeyword);
        ConsumeLineEnd();
        return new EnumDeclarationSyntax(enumKeyword, identifier, members, end, finalEnum);
    }

    private DimStatementSyntax ParseDimStatement()
    {
        var keyword = MatchToken(SyntaxKind.DimKeyword);
        var identifier = MatchIdentifier();
        SyntaxToken? open = null;
        SyntaxToken? close = null;
        IReadOnlyList<ExpressionSyntax> sizes = Array.Empty<ExpressionSyntax>();
        if (Current.Kind == SyntaxKind.OpenBracketToken)
        {
            open = NextToken();
            sizes = ParseExpressionList(SyntaxKind.CloseBracketToken);
            close = MatchToken(SyntaxKind.CloseBracketToken);
        }
        SyntaxToken? asKeyword = null;
        SyntaxToken? typeToken = null;
        if (Current.Kind == SyntaxKind.AsKeyword)
        {
            asKeyword = NextToken();
            typeToken = MatchTypeToken();
        }
        ConsumeLineEnd();
        return new DimStatementSyntax(keyword, identifier, open, sizes, close, asKeyword, typeToken);
    }

    private AssignmentStatementSyntax ParseAssignmentStatement()
    {
        var target = ParseAssignmentTarget();
        var equals = MatchToken(SyntaxKind.EqualsToken);
        var expression = ParseExpression();
        ConsumeLineEnd();
        return new AssignmentStatementSyntax(target, equals, expression);
    }

    private AssignmentTargetSyntax ParseAssignmentTarget()
    {
        if (IsContextualIdentifier(Current.Kind))
            return new AssignmentTargetSyntax(ParseAssignmentIdentifierLocation());
        return new AssignmentTargetSyntax(ParsePrimaryExpression());
    }

    private ExpressionSyntax ParseAssignmentIdentifierLocation()
    {
        var identifier = NextToken();
        if (Current.Kind == SyntaxKind.DotToken)
        {
            var dot = NextToken();
            var member = MatchMemberIdentifier();
            if (Current.Kind == SyntaxKind.OpenBracketToken)
            {
                NextToken();
                var indices = ParseExpressionList(SyntaxKind.CloseBracketToken);
                return ParseFieldSuffix(new QualifiedArrayAccessExpressionSyntax(identifier, dot, member, indices,
                    MatchToken(SyntaxKind.CloseBracketToken)));
            }
            return ParseFieldSuffix(new FieldAccessExpressionSyntax(
                new NameExpressionSyntax(identifier), dot, member));
        }
        if (Current.Kind == SyntaxKind.OpenBracketToken)
        {
            NextToken();
            var indices = ParseExpressionList(SyntaxKind.CloseBracketToken);
            return ParseFieldSuffix(new ArrayAccessExpressionSyntax(identifier, indices,
                MatchToken(SyntaxKind.CloseBracketToken)));
        }
        return ParseFieldSuffix(new NameExpressionSyntax(identifier));
    }

    private WithStatementSyntax ParseWithStatement()
    {
        var withKeyword = MatchToken(SyntaxKind.WithKeyword);
        var target = ParseExpression();
        ConsumeLineEnd();
        var statements = ParseStatementsUntil(() => IsEndPair(SyntaxKind.WithKeyword));
        var endKeyword = MatchToken(SyntaxKind.EndKeyword);
        var finalWithKeyword = MatchToken(SyntaxKind.WithKeyword);
        ConsumeLineEnd();
        return new WithStatementSyntax(withKeyword, target, statements, endKeyword, finalWithKeyword);
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

    private StatementSyntax ParseClearStatement()
    {
        var clear = MatchToken(SyntaxKind.ClearKeyword);
        if (Current.Kind != SyntaxKind.ScreenKeyword)
        {
            var color = ParseExpression();
            ConsumeLineEnd();
            return new ClearColorStatementSyntax(clear, color);
        }
        var screen = NextToken();
        ConsumeLineEnd();
        return new ClearScreenStatementSyntax(clear, screen);
    }

    private GameWindowStatementSyntax ParseGameWindowStatement()
    {
        var game = MatchToken(SyntaxKind.GameKeyword);
        MatchToken(SyntaxKind.WindowKeyword);
        var title = MatchToken(SyntaxKind.StringToken);
        ExpressionSyntax? width = null;
        ExpressionSyntax? height = null;
        var end = title.Span.End;
        if (Current.Kind == SyntaxKind.SizeKeyword)
        {
            NextToken();
            width = ParseExpression();
            MatchToken(SyntaxKind.ByKeyword);
            height = ParseExpression();
            end = height.Span.End;
        }
        ConsumeLineEnd();
        return new GameWindowStatementSyntax(game, title, width, height, end);
    }

    private GraphicsStatementSyntax ParseGraphicsStatement(bool isFill)
    {
        var keyword = NextToken();
        var rounded = false;
        if (Current.Kind == SyntaxKind.RoundedKeyword)
        {
            rounded = true;
            NextToken();
        }

        GraphicsOperation operation;
        IReadOnlyList<ExpressionSyntax> arguments;
        ExpressionSyntax? textExpression = null;
        var centered = false;
        var end = keyword.Span.End;

        if (Current.Kind == SyntaxKind.RectangleKeyword)
        {
            NextToken();
            operation = rounded
                ? (isFill ? GraphicsOperation.FillRoundedRectangle : GraphicsOperation.DrawRoundedRectangle)
                : (isFill ? GraphicsOperation.FillRectangle : GraphicsOperation.DrawRectangle);
            arguments = ParseFixedArguments(rounded ? 6 : 5);
            end = arguments.Count == 0 ? keyword.Span.End : arguments[arguments.Count - 1].Span.End;
        }
        else if (Current.Kind == SyntaxKind.CircleKeyword)
        {
            NextToken();
            operation = isFill ? GraphicsOperation.FillCircle : GraphicsOperation.DrawCircle;
            arguments = ParseFixedArguments(4);
            end = arguments.Count == 0 ? keyword.Span.End : arguments[arguments.Count - 1].Span.End;
        }
        else if (!isFill && Current.Kind == SyntaxKind.ArcKeyword)
        {
            NextToken();
            operation = GraphicsOperation.DrawArc;
            arguments = ParseFixedArguments(6);
            end = arguments.Count == 0 ? keyword.Span.End : arguments[arguments.Count - 1].Span.End;
        }
        else if (Current.Kind == SyntaxKind.QuadrilateralKeyword)
        {
            NextToken();
            operation = isFill ? GraphicsOperation.FillQuadrilateral : GraphicsOperation.DrawQuadrilateral;
            arguments = ParseFixedArguments(9);
            end = arguments.Count == 0 ? keyword.Span.End : arguments[arguments.Count - 1].Span.End;
        }
        else if (!isFill && Current.Kind == SyntaxKind.LineKeyword)
        {
            NextToken();
            operation = GraphicsOperation.DrawLine;
            arguments = ParseFixedArguments(5);
            end = arguments.Count == 0 ? keyword.Span.End : arguments[arguments.Count - 1].Span.End;
        }
        else if (!isFill && Current.Kind == SyntaxKind.TextKeyword)
        {
            NextToken();
            operation = GraphicsOperation.DrawText;
            textExpression = ParseExpression();
            MatchToken(SyntaxKind.AtKeyword);
            var values = new List<ExpressionSyntax>();
            values.Add(ParseExpression());
            MatchToken(SyntaxKind.CommaToken);
            values.Add(ParseExpression());
            MatchToken(SyntaxKind.SizeKeyword);
            values.Add(ParseExpression());
            MatchToken(SyntaxKind.ColorKeyword);
            values.Add(ParseExpression());
            if (Current.Kind == SyntaxKind.CenteredKeyword)
            {
                centered = true;
                end = NextToken().Span.End;
            }
            else
            {
                end = values[values.Count - 1].Span.End;
            }
            arguments = values;
        }
        else if (!isFill && Current.Kind == SyntaxKind.NumberKeyword)
        {
            NextToken();
            operation = GraphicsOperation.DrawNumber;
            var values = new List<ExpressionSyntax> { ParseExpression() };
            MatchToken(SyntaxKind.AtKeyword);
            values.Add(ParseExpression());
            MatchToken(SyntaxKind.CommaToken);
            values.Add(ParseExpression());
            MatchToken(SyntaxKind.SizeKeyword);
            values.Add(ParseExpression());
            MatchToken(SyntaxKind.ColorKeyword);
            values.Add(ParseExpression());
            arguments = values;
            end = values[values.Count - 1].Span.End;
        }
        else
        {
            var expected = isFill ? "Rectangle, Rounded Rectangle, Circle, or Quadrilateral" : "a drawing primitive";
            _diagnostics.Report("SML2001", Current.Span, $"Expected {expected}, found '{Display(Current)}'.");
            arguments = Array.Empty<ExpressionSyntax>();
            operation = isFill ? GraphicsOperation.FillRectangle : GraphicsOperation.DrawRectangle;
            SynchronizeLine();
            return new GraphicsStatementSyntax(keyword, operation, arguments, null, false, end);
        }

        ConsumeLineEnd();
        return new GraphicsStatementSyntax(keyword, operation, arguments, textExpression, centered, end);
    }

    private DrawImageStatementSyntax ParseDrawImageStatement()
    {
        var draw = MatchToken(SyntaxKind.DrawKeyword);
        var imageKeyword = MatchToken(SyntaxKind.ImageKeyword);
        var image = ParseExpression();
        ExpressionSyntax? sourceX = null;
        ExpressionSyntax? sourceY = null;
        ExpressionSyntax? sourceWidth = null;
        ExpressionSyntax? sourceHeight = null;
        if (Current.Kind == SyntaxKind.FromKeyword)
        {
            NextToken();
            sourceX = ParseExpression();
            MatchToken(SyntaxKind.CommaToken);
            sourceY = ParseExpression();
            MatchToken(SyntaxKind.SizeKeyword);
            sourceWidth = ParseExpression();
            MatchToken(SyntaxKind.ByKeyword);
            sourceHeight = ParseExpression();
        }
        MatchToken(SyntaxKind.AtKeyword);
        var destinationX = ParseExpression();
        MatchToken(SyntaxKind.CommaToken);
        var destinationY = ParseExpression();
        ExpressionSyntax? destinationWidth = null;
        ExpressionSyntax? destinationHeight = null;
        if (Current.Kind == SyntaxKind.SizeKeyword)
        {
            NextToken();
            destinationWidth = ParseExpression();
            MatchToken(SyntaxKind.ByKeyword);
            destinationHeight = ParseExpression();
        }

        ExpressionSyntax? opacity = null;
        ExpressionSyntax? anchorX = null;
        ExpressionSyntax? anchorY = null;
        var filter = ImageFilter.Smooth;
        var flip = ImageFlip.None;
        var end = destinationHeight?.Span.End ?? destinationY.Span.End;
        while (!IsLineEnd(Current.Kind))
        {
            if (Current.Kind == SyntaxKind.OpacityKeyword)
            {
                NextToken();
                opacity = ParseExpression();
                end = opacity.Span.End;
            }
            else if (Current.Kind == SyntaxKind.AnchorKeyword)
            {
                NextToken();
                anchorX = ParseExpression();
                MatchToken(SyntaxKind.CommaToken);
                anchorY = ParseExpression();
                end = anchorY.Span.End;
            }
            else if (Current.Kind == SyntaxKind.FilterKeyword)
            {
                NextToken();
                SyntaxToken filterToken;
                if (Current.Kind == SyntaxKind.PixelKeyword)
                {
                    filter = ImageFilter.Pixel;
                    filterToken = NextToken();
                }
                else
                    filterToken = MatchToken(SyntaxKind.SmoothKeyword);
                end = filterToken.Span.End;
            }
            else if (Current.Kind == SyntaxKind.FlipKeyword)
            {
                NextToken();
                flip = Current.Kind switch
                {
                    SyntaxKind.HorizontalKeyword => ImageFlip.Horizontal,
                    SyntaxKind.VerticalKeyword => ImageFlip.Vertical,
                    SyntaxKind.BothKeyword => ImageFlip.Horizontal | ImageFlip.Vertical,
                    _ => ImageFlip.None
                };
                if (flip == ImageFlip.None)
                    end = MatchToken(SyntaxKind.HorizontalKeyword).Span.End;
                else
                    end = NextToken().Span.End;
            }
            else
            {
                _diagnostics.Report("SML3503", Current.Span,
                    $"Unexpected Draw Image modifier '{Display(Current)}'.");
                SynchronizeLine();
                break;
            }
        }
        ConsumeLineEnd();
        return new DrawImageStatementSyntax(draw, imageKeyword, image, sourceX, sourceY, sourceWidth,
            sourceHeight, destinationX, destinationY, destinationWidth, destinationHeight, opacity,
            filter, flip, anchorX, anchorY, end);
    }

    private ClipRectangleStatementSyntax ParseClipRectangleStatement()
    {
        var clip = MatchToken(SyntaxKind.ClipKeyword);
        MatchToken(SyntaxKind.RectangleKeyword);
        var arguments = ParseFixedArguments(4);
        ConsumeLineEnd();
        var statements = ParseStatementsUntil(() => IsEndPair(SyntaxKind.ClipKeyword));
        var end = MatchToken(SyntaxKind.EndKeyword);
        var finalClip = MatchToken(SyntaxKind.ClipKeyword);
        ConsumeLineEnd();
        return new ClipRectangleStatementSyntax(clip, arguments, statements, end, finalClip);
    }

    private IReadOnlyList<ExpressionSyntax> ParseFixedArguments(int count)
    {
        var arguments = new List<ExpressionSyntax>();
        for (var index = 0; index < count; index++)
        {
            if (index != 0)
                MatchToken(SyntaxKind.CommaToken);
            arguments.Add(ParseExpression());
        }
        return arguments;
    }

    private ShowScreenStatementSyntax ParseShowScreenStatement()
    {
        var show = MatchToken(SyntaxKind.ShowKeyword);
        var screen = MatchToken(SyntaxKind.ScreenKeyword);
        ConsumeLineEnd();
        return new ShowScreenStatementSyntax(show, screen);
    }

    private SoundStatementSyntax ParseSoundStatement(bool isStop)
    {
        var keyword = MatchToken(isStop ? SyntaxKind.StopKeyword : SyntaxKind.PlayKeyword);
        var sound = MatchToken(SyntaxKind.SoundKeyword);
        var path = isStop ? null : MatchToken(SyntaxKind.StringToken);
        ExpressionSyntax? channel = null;
        if (Current.Kind == SyntaxKind.OnKeyword)
        {
            NextToken();
            MatchToken(SyntaxKind.ChannelKeyword);
            channel = ParseExpression();
        }
        ConsumeLineEnd();
        return new SoundStatementSyntax(keyword, sound, path, channel);
    }

    private MusicStatementSyntax ParseMusicStatement(MusicOperation operation)
    {
        SyntaxToken keyword;
        SyntaxToken music;
        SyntaxToken? path = null;
        SyntaxToken? loop = null;
        ExpressionSyntax? volume = null;

        if (operation == MusicOperation.SetVolume)
        {
            keyword = MatchToken(SyntaxKind.MusicKeyword);
            music = keyword;
            MatchToken(SyntaxKind.VolumeKeyword);
            volume = ParseExpression();
        }
        else
        {
            var keywordKind = operation switch
            {
                MusicOperation.Play => SyntaxKind.PlayKeyword,
                MusicOperation.Pause => SyntaxKind.PauseKeyword,
                MusicOperation.Resume => SyntaxKind.ResumeKeyword,
                MusicOperation.Stop => SyntaxKind.StopKeyword,
                _ => throw new InvalidOperationException("Unknown music operation.")
            };
            keyword = MatchToken(keywordKind);
            music = MatchToken(SyntaxKind.MusicKeyword);
            if (operation == MusicOperation.Play)
            {
                path = MatchToken(SyntaxKind.StringToken);
                if (Current.Kind == SyntaxKind.LoopKeyword)
                    loop = NextToken();
            }
        }

        ConsumeLineEnd();
        return new MusicStatementSyntax(keyword, music, operation, path, loop, volume);
    }

    private LoadStatementSyntax ParseLoadStatement()
    {
        var load = MatchToken(SyntaxKind.LoadKeyword);
        var identifier = MatchIdentifier();
        MatchToken(SyntaxKind.FromKeyword);
        var key = MatchToken(SyntaxKind.StringToken);
        MatchToken(SyntaxKind.DefaultKeyword);
        var defaultValue = ParseExpression();
        ConsumeLineEnd();
        return new LoadStatementSyntax(load, identifier, key, defaultValue);
    }

    private ImageLoadStatementSyntax ParseImageLoadStatement(bool isUnload)
    {
        var keyword = MatchToken(isUnload ? SyntaxKind.UnloadKeyword : SyntaxKind.LoadKeyword);
        var image = MatchToken(SyntaxKind.ImageKeyword);
        var target = ParseAssignmentTarget();
        ExpressionSyntax? path = null;
        if (!isUnload)
        {
            MatchToken(SyntaxKind.FromKeyword);
            path = ParseExpression();
        }
        ConsumeLineEnd();
        return new ImageLoadStatementSyntax(keyword, image, target, path);
    }

    private DataLoadStatementSyntax ParseDataLoadStatement()
    {
        var load = MatchToken(SyntaxKind.LoadKeyword);
        var data = MatchToken(SyntaxKind.DataKeyword);
        var key = ParseExpression();
        MatchToken(SyntaxKind.IntoKeyword);
        var destination = MatchIdentifier();
        MatchToken(SyntaxKind.CountKeyword);
        var countTarget = ParseAssignmentTarget();
        ConsumeLineEnd();
        return new DataLoadStatementSyntax(load, data, key, destination, countTarget);
    }

    private DataSaveStatementSyntax ParseDataSaveStatement()
    {
        var save = MatchToken(SyntaxKind.SaveKeyword);
        var data = MatchToken(SyntaxKind.DataKeyword);
        var source = MatchIdentifier();
        MatchToken(SyntaxKind.CountKeyword);
        var count = ParseExpression();
        MatchToken(SyntaxKind.ToKeyword);
        var key = ParseExpression();
        ConsumeLineEnd();
        return new DataSaveStatementSyntax(save, data, source, count, key);
    }

    private TextFileLoadStatementSyntax ParseTextFileLoadStatement()
    {
        var load = MatchToken(SyntaxKind.LoadKeyword);
        var text = MatchToken(SyntaxKind.TextKeyword);
        var file = MatchToken(SyntaxKind.FileKeyword);
        var path = ParseExpression();
        var into = MatchToken(SyntaxKind.IntoKeyword);
        var destination = MatchIdentifier();
        var count = MatchToken(SyntaxKind.CountKeyword);
        var countIdentifier = MatchIdentifier();
        ConsumeLineEnd();
        return new TextFileLoadStatementSyntax(load, text, file, path, into, destination, count, countIdentifier);
    }

    private SaveStatementSyntax ParseSaveStatement()
    {
        var save = MatchToken(SyntaxKind.SaveKeyword);
        var identifier = MatchIdentifier();
        MatchToken(SyntaxKind.ToKeyword);
        var key = MatchToken(SyntaxKind.StringToken);
        ConsumeLineEnd();
        return new SaveStatementSyntax(save, identifier, key);
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
        SyntaxToken? openParenthesis = null;
        SyntaxToken? closeParenthesis = null;
        var parameters = new List<ParameterSyntax>();
        var continuedDeclaration = false;
        if (Current.Kind == SyntaxKind.OpenParenthesisToken)
        {
            openParenthesis = NextToken();
            _declarationContinuationDepth++;

            try
            {
                continuedDeclaration |= SkipDeclarationNewLines();
                if (Current.Kind != SyntaxKind.CloseParenthesisToken)
                {
                    while (true)
                    {
                        SyntaxToken? optional = null;
                        if (Current.Kind == SyntaxKind.OptionalKeyword)
                        {
                            optional = NextToken();
                            continuedDeclaration |= SkipDeclarationNewLines();
                        }
                        SyntaxToken? mode = null;
                        if (Current.Kind is SyntaxKind.ByRefKeyword or SyntaxKind.ByValKeyword)
                        {
                            mode = NextToken();
                            continuedDeclaration |= SkipDeclarationNewLines();
                        }
                        var parameter = MatchIdentifier();
                        continuedDeclaration |= SkipDeclarationNewLines();
                        SyntaxToken? parameterAs = null;
                        SyntaxToken? parameterType = null;
                        if (Current.Kind == SyntaxKind.AsKeyword)
                        {
                            parameterAs = NextToken();
                            parameterType = MatchTypeToken();
                            continuedDeclaration |= SkipDeclarationNewLines();
                        }
                        SyntaxToken? equals = null;
                        ExpressionSyntax? defaultValue = null;
                        if (Current.Kind == SyntaxKind.EqualsToken)
                        {
                            equals = NextToken();
                            continuedDeclaration |= SkipDeclarationNewLines();
                            defaultValue = ParseExpression();
                            continuedDeclaration |= SkipDeclarationNewLines();
                        }
                        parameters.Add(new ParameterSyntax(optional, mode, parameter, parameterAs, parameterType,
                            equals, defaultValue));
                        if (Current.Kind == SyntaxKind.CloseParenthesisToken)
                            break;
                        if (Current.Kind != SyntaxKind.CommaToken)
                        {
                            if (IsParameterStart(Current.Kind))
                            {
                                _diagnostics.Report("SML2001", Current.Span,
                                    $"Expected comma between routine parameters, found '{Display(Current)}'.");
                                continue;
                            }
                            break;
                        }
                        NextToken();
                        continuedDeclaration |= SkipDeclarationNewLines();
                    }
                }
                closeParenthesis = MatchToken(SyntaxKind.CloseParenthesisToken);
            }
            finally
            {
                _declarationContinuationDepth--;
            }
        }
        SyntaxToken? asKeyword = null;
        SyntaxToken? returnType = null;
        if (Current.Kind == SyntaxKind.AsKeyword)
        {
            asKeyword = NextToken();
            returnType = MatchTypeToken();
        }
        var recoveredAtBody = closeParenthesis is { Span.Length: 0 } && continuedDeclaration &&
            !IsLineEnd(Current.Kind) && Current.Kind != SyntaxKind.AsKeyword;
        if (!recoveredAtBody)
            ConsumeLineEnd();
        var statements = ParseStatementsUntil(() => IsEndPair(keyword.Kind) ||
            (_typeDeclarationDepth > 0 && IsTypeRoutineRecoveryBoundary()));
        SyntaxToken end;
        SyntaxToken final;
        if (IsEndPair(keyword.Kind))
        {
            end = NextToken();
            final = NextToken();
            ConsumeLineEnd();
        }
        else
        {
            _diagnostics.Report("SML2001", Current.Span,
                $"Expected End {keyword.Text} before '{Display(Current)}'.");
            end = new SyntaxToken(SyntaxKind.EndKeyword, Current.Position, string.Empty);
            final = new SyntaxToken(keyword.Kind, Current.Position, string.Empty);
        }
        return new RoutineDeclarationSyntax(keyword, identifier, openParenthesis, parameters, closeParenthesis,
            asKeyword, returnType, statements, end, final);
    }

    private StatementSyntax ParseCallStatement()
    {
        var call = MatchToken(SyntaxKind.CallKeyword);
        var invocation = ParsePrimaryExpression();
        StatementSyntax result;
        switch (invocation)
        {
            case CallExpressionSyntax direct:
                result = new CallStatementSyntax(call, direct.Identifier, direct.Arguments, direct.CloseParenthesis);
                break;
            case QualifiedCallExpressionSyntax qualified:
                result = new QualifiedCallStatementSyntax(call, qualified.Alias, qualified.DotToken,
                    qualified.Member, qualified.Arguments, qualified.CloseParenthesis);
                break;
            case MemberInvocationExpressionSyntax member:
                result = new MemberCallStatementSyntax(call, member);
                break;
            case LeadingMemberInvocationExpressionSyntax leading:
                result = new LeadingMemberCallStatementSyntax(call, leading.DotToken, leading.Member,
                    leading.Arguments, leading.CloseParenthesis);
                break;
            default:
                _diagnostics.Report("SML2001", invocation.Span,
                    "Call requires a routine invocation ending in parentheses.");
                var close = new SyntaxToken(SyntaxKind.CloseParenthesisToken, invocation.Span.End, string.Empty);
                var identifier = invocation is NameExpressionSyntax name
                    ? name.Identifier
                    : new SyntaxToken(SyntaxKind.IdentifierToken, invocation.Span.Start, string.Empty);
                result = new CallStatementSyntax(call, identifier, Array.Empty<ArgumentSyntax>(), close);
                break;
        }
        ConsumeLineEnd();
        return result;
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
        SkipExpressionNewLines();
        if (Current.Kind == closingKind)
            return expressions;
        while (true)
        {
            expressions.Add(ParseExpression());
            SkipExpressionNewLines();
            if (Current.Kind != SyntaxKind.CommaToken)
                break;
            NextToken();
            SkipExpressionNewLines();
        }
        return expressions;
    }

    private (IReadOnlyList<ArgumentSyntax> Arguments, SyntaxToken CloseParenthesis) ParseParenthesizedArgumentList()
    {
        var hasOpenParenthesis = Current.Kind == SyntaxKind.OpenParenthesisToken;
        MatchToken(SyntaxKind.OpenParenthesisToken);
        if (hasOpenParenthesis)
            _expressionContinuationDepth++;

        try
        {
            var arguments = ParseArgumentList(SyntaxKind.CloseParenthesisToken);
            SkipExpressionNewLines();
            var closeParenthesis = MatchToken(SyntaxKind.CloseParenthesisToken);
            return (arguments, closeParenthesis);
        }
        finally
        {
            if (hasOpenParenthesis)
                _expressionContinuationDepth--;
        }
    }

    private IReadOnlyList<ArgumentSyntax> ParseArgumentList(SyntaxKind closingKind)
    {
        var arguments = new List<ArgumentSyntax>();
        SkipExpressionNewLines();
        if (Current.Kind == closingKind)
            return arguments;
        while (true)
        {
            SyntaxToken? name = null;
            SyntaxToken? colonEquals = null;
            if (IsIdentifierLike(Current.Kind) && Peek(1).Kind == SyntaxKind.ColonEqualsToken)
            {
                name = NextToken();
                colonEquals = NextToken();
                SkipExpressionNewLines();
            }
            arguments.Add(new ArgumentSyntax(name, colonEquals, ParseExpression()));
            SkipExpressionNewLines();
            if (Current.Kind != SyntaxKind.CommaToken)
                break;
            NextToken();
            SkipExpressionNewLines();
        }
        return arguments;
    }

    private ExpressionSyntax ParseExpression(int parentPrecedence = 0)
    {
        SkipExpressionNewLines();
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
            SkipExpressionNewLines();
            var precedence = SyntaxFacts.GetBinaryPrecedence(Current.Kind);
            if (precedence == 0 || precedence <= parentPrecedence)
                break;
            var operatorToken = NextToken();
            SkipExpressionNewLines();
            left = new BinaryExpressionSyntax(left, operatorToken, ParseExpression(precedence));
        }
        return left;
    }

    private ExpressionSyntax ParsePrimaryExpression()
    {
        if (Current.Kind == SyntaxKind.DotToken)
        {
            var dot = NextToken();
            var member = MatchMemberIdentifier();
            ExpressionSyntax leading;
            if (Current.Kind == SyntaxKind.OpenParenthesisToken)
            {
                var (arguments, closeParenthesis) = ParseParenthesizedArgumentList();
                leading = new LeadingMemberInvocationExpressionSyntax(dot, member, arguments, closeParenthesis);
            }
            else
            {
                leading = new LeadingMemberAccessExpressionSyntax(dot, member);
            }
            return ParsePostfixSuffix(leading);
        }
        if (Current.Kind == SyntaxKind.OpenParenthesisToken)
        {
            var open = NextToken();
            _expressionContinuationDepth++;

            try
            {
                var expression = ParseExpression();
                SkipExpressionNewLines();
                return ParsePostfixSuffix(new ParenthesizedExpressionSyntax(open, expression,
                    MatchToken(SyntaxKind.CloseParenthesisToken)));
            }
            finally
            {
                _expressionContinuationDepth--;
            }
        }
        if (Current.Kind == SyntaxKind.MeKeyword)
            return ParsePostfixSuffix(new MeExpressionSyntax(NextToken()));
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
        if (IsIdentifierLike(Current.Kind) || SyntaxFacts.IsBuiltInFunction(Current.Kind))
        {
            var identifier = NextToken();
            if (Current.Kind == SyntaxKind.DotToken)
            {
                var dot = NextToken();
                var member = MatchMemberIdentifier();
                if (Current.Kind == SyntaxKind.OpenParenthesisToken)
                {
                    var (qualifiedArguments, qualifiedClose) = ParseParenthesizedArgumentList();
                    return ParsePostfixSuffix(new MemberInvocationExpressionSyntax(
                        new NameExpressionSyntax(identifier), dot, member, qualifiedArguments, qualifiedClose));
                }
                if (Current.Kind == SyntaxKind.OpenBracketToken)
                {
                    NextToken();
                    var qualifiedIndices = ParseExpressionList(SyntaxKind.CloseBracketToken);
                    return ParsePostfixSuffix(new QualifiedArrayAccessExpressionSyntax(identifier, dot, member, qualifiedIndices,
                        MatchToken(SyntaxKind.CloseBracketToken)));
                }
                ExpressionSyntax memberAccess = new FieldAccessExpressionSyntax(
                    new NameExpressionSyntax(identifier), dot, member);
                return ParsePostfixSuffix(memberAccess);
            }
            if (Current.Kind == SyntaxKind.OpenParenthesisToken)
            {
                var (arguments, closeParenthesis) = ParseParenthesizedArgumentList();
                return ParsePostfixSuffix(new CallExpressionSyntax(identifier, arguments, closeParenthesis));
            }
            if (Current.Kind == SyntaxKind.OpenBracketToken)
            {
                NextToken();
                var indices = ParseExpressionList(SyntaxKind.CloseBracketToken);
                ExpressionSyntax array = new ArrayAccessExpressionSyntax(identifier, indices, MatchToken(SyntaxKind.CloseBracketToken));
                return ParsePostfixSuffix(array);
            }
            ExpressionSyntax name = new NameExpressionSyntax(identifier);
            return ParsePostfixSuffix(name);
        }
        var missing = MatchToken(SyntaxKind.NumberToken);
        return new LiteralExpressionSyntax(missing, 0L);
    }

    private void SkipExpressionNewLines()
    {
        if (_expressionContinuationDepth == 0)
            return;

        while (Current.Kind == SyntaxKind.NewLineToken)
            NextToken();
    }

    private bool SkipDeclarationNewLines()
    {
        if (_declarationContinuationDepth == 0)
            return false;

        var skipped = false;
        while (Current.Kind == SyntaxKind.NewLineToken)
        {
            NextToken();
            skipped = true;
        }
        return skipped;
    }

    private ExpressionSyntax ParseFieldSuffix(ExpressionSyntax expression)
        => ParsePostfixSuffix(expression);

    private ExpressionSyntax ParsePostfixSuffix(ExpressionSyntax expression)
    {
        while (Current.Kind == SyntaxKind.DotToken)
        {
            var dot = NextToken();
            var member = MatchMemberIdentifier();
            if (Current.Kind == SyntaxKind.OpenParenthesisToken)
            {
                var (arguments, closeParenthesis) = ParseParenthesizedArgumentList();
                expression = new MemberInvocationExpressionSyntax(expression, dot, member, arguments,
                    closeParenthesis);
            }
            else
            {
                expression = new FieldAccessExpressionSyntax(expression, dot, member);
            }
        }
        return expression;
    }

    private bool IsEndPair(SyntaxKind finalKind) => Current.Kind == SyntaxKind.EndKeyword && Peek(1).Kind == finalKind;

    private bool IsEndContextualPair(string text) => Current.Kind == SyntaxKind.EndKeyword &&
        IsContextualText(Peek(1), text);

    private bool IsTypeMemberStart()
    {
        var offset = Current.Kind is SyntaxKind.PublicKeyword or SyntaxKind.PrivateKeyword ? 1 : 0;
        var kind = Peek(offset).Kind;
        return kind is SyntaxKind.SubKeyword or SyntaxKind.FunctionKeyword or SyntaxKind.PropertyKeyword;
    }

    private bool IsPropertyAccessorBoundary() => Current.Kind is SyntaxKind.GetKeyword or SyntaxKind.SetKeyword ||
        IsEndContextualPair("Property") || IsEndPair(SyntaxKind.TypeKeyword) || IsTypeFieldStart() ||
        (Current.Kind == SyntaxKind.EndKeyword &&
         Peek(1).Kind is SyntaxKind.GetKeyword or SyntaxKind.SetKeyword);

    private bool IsTypeRoutineRecoveryBoundary() => IsTypeMemberStart() || IsTypeFieldStart() ||
        IsEndPair(SyntaxKind.TypeKeyword) ||
        IsEndContextualPair("Property") ||
        (Current.Kind == SyntaxKind.EndKeyword &&
         Peek(1).Kind is SyntaxKind.SubKeyword or SyntaxKind.FunctionKeyword or SyntaxKind.GetKeyword or
             SyntaxKind.SetKeyword);

    private bool IsTypeFieldStart()
    {
        var offset = Current.Kind is SyntaxKind.PublicKeyword or SyntaxKind.PrivateKeyword ? 1 : 0;
        return IsIdentifierLike(Peek(offset).Kind) && Peek(offset + 1).Kind == SyntaxKind.AsKeyword;
    }

    private (SyntaxToken EndKeyword, SyntaxToken FinalKeyword) MatchContextualEndPair(string text)
    {
        if (IsEndContextualPair(text))
        {
            var end = NextToken();
            var final = NextToken();
            ConsumeLineEnd();
            return (end, final);
        }
        _diagnostics.Report("SML2001", Current.Span, $"Expected End {text} before '{Display(Current)}'.");
        return (new SyntaxToken(SyntaxKind.EndKeyword, Current.Position, string.Empty),
            new SyntaxToken(SyntaxKind.IdentifierToken, Current.Position, string.Empty));
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
        if (IsIdentifierLike(Current.Kind))
            return NextToken();
        return MatchToken(SyntaxKind.IdentifierToken);
    }

    private SyntaxToken MatchContextualIdentifier(string text)
    {
        if (IsContextualText(Current, text))
            return NextToken();
        _diagnostics.Report("SML2001", Current.Span, $"Expected {text}, found '{Display(Current)}'.");
        return new SyntaxToken(SyntaxKind.IdentifierToken, Current.Position, string.Empty);
    }

    private static bool IsContextualText(SyntaxToken token, string text) =>
        string.Equals(token.Text, text, StringComparison.OrdinalIgnoreCase);

    private SyntaxToken MatchMemberIdentifier()
    {
        if (IsMemberIdentifier(Current.Kind))
            return NextToken();
        return MatchToken(SyntaxKind.IdentifierToken);
    }

    private static bool IsMemberIdentifier(SyntaxKind kind) =>
        IsIdentifierLike(kind) || kind is SyntaxKind.NoneKeyword or SyntaxKind.UpKeyword or SyntaxKind.DownKeyword;

    private static bool IsContextualIdentifier(SyntaxKind kind) =>
        kind is SyntaxKind.WindowKeyword or SyntaxKind.SizeKeyword or SyntaxKind.DrawKeyword or SyntaxKind.LineKeyword or
            SyntaxKind.TextKeyword or SyntaxKind.LeftKeyword or SyntaxKind.RightKeyword or SyntaxKind.SetKeyword or
            SyntaxKind.PropertyKeyword ||
        kind >= SyntaxKind.UnloadKeyword && kind <= SyntaxKind.ChannelKeyword;

    private static bool IsIdentifierLike(SyntaxKind kind) =>
        kind is SyntaxKind.IdentifierToken or SyntaxKind.KeyKeyword || IsContextualIdentifier(kind);

    private static bool IsParameterStart(SyntaxKind kind) =>
        kind is SyntaxKind.OptionalKeyword or SyntaxKind.ByRefKeyword or SyntaxKind.ByValKeyword ||
        IsIdentifierLike(kind);

    private SyntaxToken MatchTypeToken()
    {
        SkipDeclarationNewLines();
        if (Current.Kind is SyntaxKind.NumberKeyword or SyntaxKind.BooleanKeyword or SyntaxKind.TextKeyword or SyntaxKind.ImageKeyword or
            SyntaxKind.IdentifierToken)
        {
            var first = NextToken();
            SkipDeclarationNewLines();
            if (first.Kind == SyntaxKind.IdentifierToken && Current.Kind == SyntaxKind.DotToken)
            {
                NextToken();
                SkipDeclarationNewLines();
                var second = MatchIdentifier();
                return new SyntaxToken(SyntaxKind.IdentifierToken, first.Position, $"{first.Text}.{second.Text}",
                    spanLength: second.Span.End - first.Position);
            }
            return first;
        }
        return MatchToken(SyntaxKind.IdentifierToken);
    }

    private SyntaxToken MatchDottedIdentifier()
    {
        if (IsIdentifierLike(Current.Kind) || Current.Kind is SyntaxKind.GameKeyword or SyntaxKind.TextKeyword or
            SyntaxKind.NumberKeyword or SyntaxKind.BooleanKeyword or SyntaxKind.ImageKeyword)
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
