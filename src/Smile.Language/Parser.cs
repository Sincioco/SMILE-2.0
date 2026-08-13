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
            case SyntaxKind.OptionKeyword: return ParseOptionExplicitStatement();
            case SyntaxKind.ModuleKeyword: return ParseModuleDeclaration();
            case SyntaxKind.ImportKeyword: return ParseImportStatement();
            case SyntaxKind.PublicKeyword:
            case SyntaxKind.PrivateKeyword: return ParseVisibilityDeclaration();
            case SyntaxKind.ConstKeyword: return ParseConstStatement();
            case SyntaxKind.TypeKeyword: return ParseTypeDeclaration();
            case SyntaxKind.DimKeyword: return ParseDimStatement();
            case SyntaxKind.IfKeyword: return ParseIfStatement();
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
            case SyntaxKind.FillKeyword: return ParseGraphicsStatement(isFill: true);
            case SyntaxKind.DrawKeyword: return ParseGraphicsStatement(isFill: false);
            case SyntaxKind.ShowKeyword: return ParseShowScreenStatement();
            case SyntaxKind.PlayKeyword when Peek(1).Kind == SyntaxKind.MusicKeyword: return ParseMusicStatement(MusicOperation.Play);
            case SyntaxKind.PlayKeyword: return ParseSoundStatement(isStop: false);
            case SyntaxKind.PauseKeyword: return ParseMusicStatement(MusicOperation.Pause);
            case SyntaxKind.ResumeKeyword: return ParseMusicStatement(MusicOperation.Resume);
            case SyntaxKind.StopKeyword when Peek(1).Kind == SyntaxKind.MusicKeyword: return ParseMusicStatement(MusicOperation.Stop);
            case SyntaxKind.StopKeyword: return ParseSoundStatement(isStop: true);
            case SyntaxKind.MusicKeyword: return ParseMusicStatement(MusicOperation.SetVolume);
            case SyntaxKind.LoadKeyword when Peek(1).Kind == SyntaxKind.TextKeyword: return ParseTextFileLoadStatement();
            case SyntaxKind.LoadKeyword: return ParseLoadStatement();
            case SyntaxKind.SaveKeyword: return ParseSaveStatement();
            case SyntaxKind.IdentifierToken:
            case SyntaxKind.KeyKeyword: return ParseAssignmentStatement();
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
        if (Current.Kind is not (SyntaxKind.ConstKeyword or SyntaxKind.DimKeyword or SyntaxKind.TypeKeyword or
            SyntaxKind.SubKeyword or SyntaxKind.FunctionKeyword))
        {
            _diagnostics.Report("SML2003", Current.Span,
                "PUBLIC or PRIVATE must modify a module CONST, DIM, TYPE, SUB, or FUNCTION declaration.");
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
                "CONST declarations cannot have record types; declare a DIM record variable instead.");
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
        var fields = new List<RecordFieldDeclarationSyntax>();
        while (Current.Kind != SyntaxKind.EndOfFileToken && !IsEndPair(SyntaxKind.TypeKeyword))
        {
            if (Current.Kind == SyntaxKind.NewLineToken)
            {
                NextToken();
                continue;
            }
            if (Current.Kind != SyntaxKind.IdentifierToken && Current.Kind != SyntaxKind.KeyKeyword)
            {
                _diagnostics.Report("SML3403", Current.Span,
                    "TYPE fields must use 'Name AS Type' and cannot be arrays, declarations, or initializers.");
                SynchronizeLine();
                continue;
            }
            var field = MatchIdentifier();
            if (Current.Kind != SyntaxKind.AsKeyword)
            {
                var unsupported = Current.Kind is SyntaxKind.OpenBracketToken or SyntaxKind.EqualsToken;
                _diagnostics.Report(unsupported ? "SML3403" : "SML3402", Current.Span, unsupported
                    ? "Record fields cannot be arrays and cannot have initializers."
                    : $"Field '{field.Text}' requires AS and a field type.");
                SynchronizeLine();
                continue;
            }
            var asKeyword = MatchToken(SyntaxKind.AsKeyword);
            var fieldType = MatchTypeToken();
            fields.Add(new RecordFieldDeclarationSyntax(field, asKeyword, fieldType));
            if (!IsLineEnd(Current.Kind))
            {
                _diagnostics.Report("SML3403", Current.Span,
                    "Record fields cannot be arrays and cannot have initializers.");
                SynchronizeLine();
                continue;
            }
            ConsumeLineEnd();
        }
        var end = MatchToken(SyntaxKind.EndKeyword);
        var finalType = MatchToken(SyntaxKind.TypeKeyword);
        ConsumeLineEnd();
        return new TypeDeclarationSyntax(typeKeyword, identifier, fields, end, finalType);
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
        var first = MatchIdentifier();
        SyntaxToken? qualifier = null;
        SyntaxToken? dot = null;
        var identifier = first;
        if (Current.Kind == SyntaxKind.DotToken)
        {
            qualifier = first;
            dot = NextToken();
            identifier = MatchIdentifier();
        }
        SyntaxToken? open = null;
        SyntaxToken? close = null;
        IReadOnlyList<ExpressionSyntax> indices = Array.Empty<ExpressionSyntax>();
        if (Current.Kind == SyntaxKind.OpenBracketToken)
        {
            open = NextToken();
            indices = ParseExpressionList(SyntaxKind.CloseBracketToken);
            close = MatchToken(SyntaxKind.CloseBracketToken);
        }

        var fieldDots = new List<SyntaxToken>();
        var fields = new List<SyntaxToken>();
        while (Current.Kind == SyntaxKind.DotToken)
        {
            fieldDots.Add(NextToken());
            fields.Add(MatchIdentifier());
        }

        var target = new AssignmentTargetSyntax(identifier, open, indices, close, qualifier, dot, fieldDots, fields);
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
            var expected = isFill ? "RECTANGLE, ROUNDED RECTANGLE, CIRCLE, or QUADRILATERAL" : "a drawing primitive";
            _diagnostics.Report("SML2001", Current.Span, $"Expected {expected}, found '{Display(Current)}'.");
            arguments = Array.Empty<ExpressionSyntax>();
            operation = isFill ? GraphicsOperation.FillRectangle : GraphicsOperation.DrawRectangle;
            SynchronizeLine();
            return new GraphicsStatementSyntax(keyword, operation, arguments, null, false, end);
        }

        ConsumeLineEnd();
        return new GraphicsStatementSyntax(keyword, operation, arguments, textExpression, centered, end);
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
        ConsumeLineEnd();
        return new SoundStatementSyntax(keyword, sound, path);
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

    private TextFileLoadStatementSyntax ParseTextFileLoadStatement()
    {
        var load = MatchToken(SyntaxKind.LoadKeyword);
        var text = MatchToken(SyntaxKind.TextKeyword);
        var file = MatchToken(SyntaxKind.FileKeyword);
        var path = MatchToken(SyntaxKind.StringToken);
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
        var parameters = new List<ParameterSyntax>();
        if (Current.Kind == SyntaxKind.OpenParenthesisToken)
        {
            NextToken();
            if (Current.Kind != SyntaxKind.CloseParenthesisToken)
            {
                while (true)
                {
                    SyntaxToken? mode = null;
                    if (Current.Kind is SyntaxKind.ByRefKeyword or SyntaxKind.ByValKeyword)
                        mode = NextToken();
                    var parameter = MatchIdentifier();
                    SyntaxToken? parameterAs = null;
                    SyntaxToken? parameterType = null;
                    if (Current.Kind == SyntaxKind.AsKeyword)
                    {
                        parameterAs = NextToken();
                        parameterType = MatchTypeToken();
                    }
                    parameters.Add(new ParameterSyntax(mode, parameter, parameterAs, parameterType));
                    if (Current.Kind != SyntaxKind.CommaToken)
                        break;
                    NextToken();
                }
            }
            MatchToken(SyntaxKind.CloseParenthesisToken);
        }
        SyntaxToken? asKeyword = null;
        SyntaxToken? returnType = null;
        if (Current.Kind == SyntaxKind.AsKeyword)
        {
            asKeyword = NextToken();
            returnType = MatchTypeToken();
        }
        ConsumeLineEnd();
        var statements = ParseStatementsUntil(() => IsEndPair(keyword.Kind));
        var end = MatchToken(SyntaxKind.EndKeyword);
        var final = MatchToken(keyword.Kind);
        ConsumeLineEnd();
        return new RoutineDeclarationSyntax(keyword, identifier, parameters, asKeyword, returnType, statements, end, final);
    }

    private StatementSyntax ParseCallStatement()
    {
        var call = MatchToken(SyntaxKind.CallKeyword);
        var identifier = MatchIdentifier();
        if (Current.Kind == SyntaxKind.DotToken)
        {
            var dot = NextToken();
            var member = MatchIdentifier();
            MatchToken(SyntaxKind.OpenParenthesisToken);
            var qualifiedArguments = ParseExpressionList(SyntaxKind.CloseParenthesisToken);
            var qualifiedClose = MatchToken(SyntaxKind.CloseParenthesisToken);
            ConsumeLineEnd();
            return new QualifiedCallStatementSyntax(call, identifier, dot, member, qualifiedArguments, qualifiedClose);
        }
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
            if (Current.Kind == SyntaxKind.DotToken)
            {
                var dot = NextToken();
                var member = MatchIdentifier();
                if (Current.Kind == SyntaxKind.OpenParenthesisToken)
                {
                    NextToken();
                    var qualifiedArguments = ParseExpressionList(SyntaxKind.CloseParenthesisToken);
                    return ParseFieldSuffix(new QualifiedCallExpressionSyntax(identifier, dot, member, qualifiedArguments,
                        MatchToken(SyntaxKind.CloseParenthesisToken)));
                }
                if (Current.Kind == SyntaxKind.OpenBracketToken)
                {
                    NextToken();
                    var qualifiedIndices = ParseExpressionList(SyntaxKind.CloseBracketToken);
                    return ParseFieldSuffix(new QualifiedArrayAccessExpressionSyntax(identifier, dot, member, qualifiedIndices,
                        MatchToken(SyntaxKind.CloseBracketToken)));
                }
                ExpressionSyntax qualified = new QualifiedNameExpressionSyntax(identifier, dot, member);
                while (Current.Kind == SyntaxKind.DotToken)
                {
                    var fieldDot = NextToken();
                    qualified = new FieldAccessExpressionSyntax(qualified, fieldDot, MatchIdentifier());
                }
                return qualified;
            }
            if (Current.Kind == SyntaxKind.OpenParenthesisToken)
            {
                NextToken();
                var arguments = ParseExpressionList(SyntaxKind.CloseParenthesisToken);
                return ParseFieldSuffix(new CallExpressionSyntax(identifier, arguments, MatchToken(SyntaxKind.CloseParenthesisToken)));
            }
            if (Current.Kind == SyntaxKind.OpenBracketToken)
            {
                NextToken();
                var indices = ParseExpressionList(SyntaxKind.CloseBracketToken);
                ExpressionSyntax array = new ArrayAccessExpressionSyntax(identifier, indices, MatchToken(SyntaxKind.CloseBracketToken));
                while (Current.Kind == SyntaxKind.DotToken)
                {
                    var fieldDot = NextToken();
                    array = new FieldAccessExpressionSyntax(array, fieldDot, MatchIdentifier());
                }
                return array;
            }
            ExpressionSyntax name = new NameExpressionSyntax(identifier);
            while (Current.Kind == SyntaxKind.DotToken)
            {
                var fieldDot = NextToken();
                name = new FieldAccessExpressionSyntax(name, fieldDot, MatchIdentifier());
            }
            return name;
        }
        var missing = MatchToken(SyntaxKind.NumberToken);
        return new LiteralExpressionSyntax(missing, 0L);
    }

    private ExpressionSyntax ParseFieldSuffix(ExpressionSyntax expression)
    {
        while (Current.Kind == SyntaxKind.DotToken)
        {
            var dot = NextToken();
            expression = new FieldAccessExpressionSyntax(expression, dot, MatchIdentifier());
        }
        return expression;
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

    private SyntaxToken MatchTypeToken()
    {
        if (Current.Kind is SyntaxKind.NumberKeyword or SyntaxKind.BooleanKeyword or SyntaxKind.TextKeyword or
            SyntaxKind.IdentifierToken)
        {
            var first = NextToken();
            if (first.Kind == SyntaxKind.IdentifierToken && Current.Kind == SyntaxKind.DotToken)
            {
                NextToken();
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
        if (Current.Kind is SyntaxKind.IdentifierToken or SyntaxKind.KeyKeyword or SyntaxKind.TextKeyword or
            SyntaxKind.NumberKeyword or SyntaxKind.BooleanKeyword)
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
