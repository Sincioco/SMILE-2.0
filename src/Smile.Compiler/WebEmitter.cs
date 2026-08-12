using System.Globalization;
using System.Text;
using System.Text.Json;
using Smile.Language;

namespace Smile.Compiler;

internal sealed class WebEmitter
{
    private const long MaxSafeInteger = 9_007_199_254_740_991;

    private readonly SmileAnalysisResult _analysis;
    private readonly StringBuilder _builder = new();
    private readonly Dictionary<VariableSymbol, string> _variableNames = new();
    private readonly Dictionary<RoutineSymbol, string> _routineNames = new();
    private readonly Stack<string> _forExitLabels = new();
    private readonly Stack<string> _doExitLabels = new();
    private SourceText _currentSource;
    private RoutineSymbol? _currentRoutine;
    private int _indent;
    private int _temporaryId;

    public WebEmitter(SmileAnalysisResult analysis)
    {
        _analysis = analysis;
        _currentSource = analysis.BoundSyntaxTree.Source;
        AssignNames();
        var gameWindow = analysis.BoundSyntaxTree.Root.Statements.OfType<GameWindowStatementSyntax>().FirstOrDefault();
        Title = gameWindow?.Title.Value as string ?? "SMILE 2.0 Web Program";
    }

    public string Title { get; }

    public string Emit()
    {
        Line("\"use strict\";");
        Line();
        EmitGlobalDeclarations();

        foreach (var routine in OrderedRoutines())
        {
            EmitRoutine(routine);
            Line();
        }

        Line("async function smileMain() {");
        _indent++;
        _currentSource = _analysis.BoundSyntaxTree.Source;
        EmitStatements(_analysis.BoundSyntaxTree.Root.Statements, topLevel: true);
        _indent--;
        Line("}");
        Line();
        Line("smile.run(smileMain);");
        return _builder.ToString();
    }

    private void AssignNames()
    {
        var id = 0;
        foreach (var symbol in OrderedSymbols())
            _variableNames[symbol] = $"g_{id++}_{Sanitize(symbol.Name)}";

        id = 0;
        foreach (var routine in OrderedRoutines())
        {
            _routineNames[routine] = $"r_{id++}_{Sanitize(routine.Name)}";
            var localId = 0;
            foreach (var local in routine.LocalSymbols.Values.OrderBy(item => item.DeclarationSpan.Start))
            {
                if (!_variableNames.ContainsKey(local))
                    _variableNames[local] = $"l_{id - 1}_{localId++}_{Sanitize(local.Name)}";
            }
        }
    }

    private void EmitGlobalDeclarations()
    {
        foreach (var symbol in OrderedSymbols())
        {
            var keyword = symbol.IsConstant ? "const" : "let";
            Line($"{keyword} {_variableNames[symbol]} = {InitialValue(symbol)};");
        }
        if (_analysis.SemanticModel.Symbols.Count != 0)
            Line();
    }

    private void EmitRoutine(RoutineSymbol routine)
    {
        _currentSource = routine.Source;
        _currentRoutine = routine;
        var parameters = string.Join(", ", routine.Parameters.Select(parameter => _variableNames[parameter]));
        Line($"async function {_routineNames[routine]}({parameters}) {{");
        _indent++;
        foreach (var local in routine.LocalSymbols.Values
                     .Where(local => !routine.Parameters.Contains(local))
                     .OrderBy(local => local.DeclarationSpan.Start))
        {
            Line($"let {_variableNames[local]} = {InitialValue(local)};");
        }
        EmitStatements(routine.Declaration.Statements, topLevel: false);
        _indent--;
        Line("}");
        _currentRoutine = null;
    }

    private string InitialValue(VariableSymbol symbol)
    {
        if (symbol.IsArray)
            return $"smile.array([{string.Join(", ", symbol.ArrayDimensions)}])";
        return symbol.IsConstant ? symbol.ConstantValue.ToString(CultureInfo.InvariantCulture) : "0";
    }

    private void EmitStatements(IReadOnlyList<StatementSyntax> statements, bool topLevel)
    {
        foreach (var statement in statements)
        {
            if (statement is RoutineDeclarationSyntax)
            {
                if (!topLevel)
                    Unsupported(statement, "nested routine declarations");
                continue;
            }
            EmitStatement(statement, topLevel);
        }
    }

    private void EmitStatement(StatementSyntax statement, bool topLevel)
    {
        switch (statement)
        {
            case ConstStatementSyntax or DimStatementSyntax:
                return;
            case AssignmentStatementSyntax assignment:
                EmitAssignment(assignment);
                return;
            case GetKeyStatementSyntax getKey:
                Line($"{Variable(getKey.Identifier)} = smile.getKey();");
                return;
            case RandomStatementSyntax random:
                Line($"{Variable(random.Identifier)} = smile.random({Expression(random.Minimum)}, {Expression(random.Maximum)});");
                return;
            case IfStatementSyntax ifStatement:
                EmitIf(ifStatement, topLevel);
                return;
            case ForStatementSyntax forStatement:
                EmitFor(forStatement, topLevel);
                return;
            case DoStatementSyntax doStatement:
                EmitDo(doStatement, topLevel);
                return;
            case CallStatementSyntax call:
                Line($"await {Routine(call.Identifier)}({Arguments(call.Arguments)});");
                return;
            case ReturnStatementSyntax returnStatement:
                Line(returnStatement.Expression == null ? "return;" : $"return {Expression(returnStatement.Expression)};");
                return;
            case SelectStatementSyntax select:
                EmitSelect(select, topLevel);
                return;
            case ExitStatementSyntax exit:
                EmitExit(exit);
                return;
            case EndProgramStatementSyntax:
                Line("smile.endProgram();");
                return;
            case GameWindowStatementSyntax gameWindow:
                EmitGameWindow(gameWindow);
                return;
            case ClearColorStatementSyntax clear:
                Line($"smile.clear({Expression(clear.Color)});");
                return;
            case GraphicsStatementSyntax graphics:
                EmitGraphics(graphics);
                return;
            case ShowScreenStatementSyntax show:
                Line("await smile.showScreen();");
                return;
            case SoundStatementSyntax sound:
                EmitSound(sound);
                return;
            case MusicStatementSyntax music:
                EmitMusic(music);
                return;
            case LoadStatementSyntax load:
                Line($"{Variable(load.Identifier)} = smile.loadInt({Json(load.Key.Value as string ?? string.Empty)}, {Expression(load.DefaultValue)});");
                return;
            case TextFileLoadStatementSyntax textFileLoad:
                EmitTextFileLoad(textFileLoad);
                return;
            case SaveStatementSyntax save:
                Line($"smile.saveInt({Json(save.Key.Value as string ?? string.Empty)}, {Variable(save.Identifier)});");
                return;
            case PrintStatementSyntax print:
                var items = string.Join(", ", print.Items.Select(PrintItem));
                Line($"smile.print([{items}], {(print.SuppressNewLine ? "true" : "false")});");
                return;
            case ClearScreenStatementSyntax:
                Line("smile.clearScreen();");
                return;
            case WaitStatementSyntax wait:
                Line($"await smile.wait({Expression(wait.Duration)});");
                return;
            default:
                Unsupported(statement, statement.GetType().Name);
                return;
        }
    }

    private void EmitAssignment(AssignmentStatementSyntax assignment)
    {
        var value = Expression(assignment.Expression);
        if (!assignment.Target.IsArrayElement)
        {
            Line($"{Variable(assignment.Target.Identifier)} = smile.safe({value});");
            return;
        }

        var symbol = ResolveVariable(assignment.Target.Identifier);
        Line($"smile.set({_variableNames[symbol]}, [{Arguments(assignment.Target.Indices)}], {value});");
    }

    private void EmitIf(IfStatementSyntax statement, bool topLevel)
    {
        for (var index = 0; index < statement.Clauses.Count; index++)
        {
            var clause = statement.Clauses[index];
            Line($"{(index == 0 ? "if" : "else if")} (smile.isTrue({Expression(clause.Condition)})) {{");
            _indent++;
            EmitStatements(clause.Statements, topLevel);
            _indent--;
            Line("}");
        }
        if (statement.ElseStatements.Count != 0)
        {
            Line("else {");
            _indent++;
            EmitStatements(statement.ElseStatements, topLevel);
            _indent--;
            Line("}");
        }
    }

    private void EmitFor(ForStatementSyntax statement, bool topLevel)
    {
        var counter = Variable(statement.Identifier);
        var limit = Temporary("limit");
        var label = Temporary("for");
        Line($"{counter} = smile.safe({Expression(statement.LowerBound)});");
        Line($"const {limit} = smile.safe({Expression(statement.UpperBound)});");
        var comparison = statement.IsDescending ? ">=" : "<=";
        var step = statement.IsDescending ? "-1" : "1";
        Line($"{label}: for (; {counter} {comparison} {limit}; {counter} = smile.add({counter}, {step})) {{");
        _indent++;
        _forExitLabels.Push(label);
        EmitStatements(statement.Statements, topLevel);
        _forExitLabels.Pop();
        _indent--;
        Line("}");
    }

    private void EmitDo(DoStatementSyntax statement, bool topLevel)
    {
        var label = Temporary("do");
        Line(statement.UntilCondition == null ? $"{label}: while (true) {{" : $"{label}: do {{");
        _indent++;
        _doExitLabels.Push(label);
        EmitStatements(statement.Statements, topLevel);
        _doExitLabels.Pop();
        _indent--;
        Line(statement.UntilCondition == null
            ? "}"
            : $"}} while (!smile.isTrue({Expression(statement.UntilCondition)}));");
    }

    private void EmitSelect(SelectStatementSyntax statement, bool topLevel)
    {
        var selected = Temporary("select");
        Line($"const {selected} = smile.safe({Expression(statement.Expression)});");
        var emittedCondition = false;
        var elseClause = statement.Cases.FirstOrDefault(clause => clause.IsElse);
        foreach (var clause in statement.Cases.Where(clause => !clause.IsElse))
        {
            Line($"{(emittedCondition ? "else if" : "if")} ({selected} === {Expression(clause.Value!)}) {{");
            _indent++;
            EmitStatements(clause.Statements, topLevel);
            _indent--;
            Line("}");
            emittedCondition = true;
        }
        if (elseClause != null)
        {
            Line(emittedCondition ? "else {" : "{");
            _indent++;
            EmitStatements(elseClause.Statements, topLevel);
            _indent--;
            Line("}");
        }
    }

    private void EmitExit(ExitStatementSyntax statement)
    {
        var labels = statement.TargetKeyword.Kind == SyntaxKind.ForKeyword ? _forExitLabels : _doExitLabels;
        if (labels.Count == 0)
            Unsupported(statement, $"EXIT {statement.TargetKeyword.Text}");
        Line($"break {labels.Peek()};");
    }

    private void EmitGameWindow(GameWindowStatementSyntax statement)
    {
        var title = Json(statement.Title.Value as string ?? "SMILE 2.0 Web Program");
        var width = statement.Width == null ? "960" : Expression(statement.Width);
        var height = statement.Height == null ? "540" : Expression(statement.Height);
        Line($"smile.gameWindow({title}, {width}, {height});");
    }

    private void EmitGraphics(GraphicsStatementSyntax statement)
    {
        var arguments = Arguments(statement.Arguments);
        switch (statement.Operation)
        {
            case GraphicsOperation.FillRectangle:
                Line($"smile.fillRectangle({arguments});");
                return;
            case GraphicsOperation.DrawRectangle:
                Line($"smile.drawRectangle({arguments});");
                return;
            case GraphicsOperation.FillRoundedRectangle:
                Line($"smile.fillRoundedRectangle({arguments});");
                return;
            case GraphicsOperation.DrawRoundedRectangle:
                Line($"smile.drawRoundedRectangle({arguments});");
                return;
            case GraphicsOperation.FillCircle:
                Line($"smile.fillCircle({arguments});");
                return;
            case GraphicsOperation.DrawCircle:
                Line($"smile.drawCircle({arguments});");
                return;
            case GraphicsOperation.DrawArc:
                Line($"smile.drawArc({arguments});");
                return;
            case GraphicsOperation.FillQuadrilateral:
                Line($"smile.fillQuadrilateral({arguments});");
                return;
            case GraphicsOperation.DrawQuadrilateral:
                Line($"smile.drawQuadrilateral({arguments});");
                return;
            case GraphicsOperation.DrawLine:
                Line($"smile.drawLine({arguments});");
                return;
            case GraphicsOperation.DrawText:
                Line($"smile.drawText({Json(statement.Text?.Value as string ?? string.Empty)}, {arguments}, {(statement.Centered ? "true" : "false")});");
                return;
            case GraphicsOperation.DrawNumber:
                Line($"smile.drawNumber({arguments}, {(statement.Centered ? "true" : "false")});");
                return;
            default:
                Unsupported(statement, statement.Operation.ToString());
                return;
        }
    }

    private void EmitSound(SoundStatementSyntax statement)
    {
        if (statement.IsStop)
        {
            Line("smile.stopSound();");
            return;
        }
        var path = (statement.Path?.Value as string ?? string.Empty).Replace('\\', '/');
        Line($"smile.playSound({Json(path)});");
    }

    private void EmitMusic(MusicStatementSyntax statement)
    {
        switch (statement.Operation)
        {
            case MusicOperation.Play:
                var path = (statement.Path?.Value as string ?? string.Empty).Replace('\\', '/');
                Line($"smile.playMusic({Json(path)}, {(statement.Loop ? "true" : "false")});");
                return;
            case MusicOperation.Pause:
                Line("smile.pauseMusic();");
                return;
            case MusicOperation.Resume:
                Line("smile.resumeMusic();");
                return;
            case MusicOperation.Stop:
                Line("smile.stopMusic();");
                return;
            case MusicOperation.SetVolume:
                Line($"smile.setMusicVolume({Expression(statement.Volume!)});");
                return;
            default:
                Unsupported(statement, statement.Operation.ToString());
                return;
        }
    }

    private void EmitTextFileLoad(TextFileLoadStatementSyntax statement)
    {
        var path = (statement.Path.Value as string ?? string.Empty).Replace('\\', '/');
        var destination = _variableNames[ResolveVariable(statement.Destination)];
        Line($"{Variable(statement.CountIdentifier)} = await smile.loadTextFile({Json(path)}, {destination});");
    }

    private string Expression(ExpressionSyntax expression)
    {
        switch (expression)
        {
            case LiteralExpressionSyntax literal when literal.Value is long number:
                if (number is > MaxSafeInteger or < -MaxSafeInteger)
                    throw new WebTargetException(_currentSource, "SML5102", literal.Span, "Web target NUMBER literals must be within JavaScript's safe integer range.");
                return number.ToString(CultureInfo.InvariantCulture);
            case LiteralExpressionSyntax literal when literal.Value is bool boolean:
                return boolean ? "1" : "0";
            case LiteralExpressionSyntax literal when literal.Value is string text:
                return Json(text);
            case NameExpressionSyntax name:
                if (SyntaxFacts.IsBuiltInConstant(name.Identifier.Kind))
                    return SyntaxFacts.GetBuiltInConstantValue(name.Identifier.Kind).ToString(CultureInfo.InvariantCulture);
                return Variable(name.Identifier);
            case ArrayAccessExpressionSyntax array:
                return $"smile.get({_variableNames[ResolveVariable(array.Identifier)]}, [{Arguments(array.Indices)}])";
            case ParenthesizedExpressionSyntax parenthesized:
                return $"({Expression(parenthesized.Expression)})";
            case UnaryExpressionSyntax unary:
                return unary.OperatorToken.Kind switch
                {
                    SyntaxKind.MinusToken => $"smile.neg({Expression(unary.Operand)})",
                    SyntaxKind.NotKeyword => $"(smile.isTrue({Expression(unary.Operand)}) ? 0 : 1)",
                    _ => throw UnsupportedExpression(unary, unary.OperatorToken.Text)
                };
            case BinaryExpressionSyntax binary:
                return Binary(binary);
            case CallExpressionSyntax call:
                return Call(call);
            default:
                throw UnsupportedExpression(expression, expression.GetType().Name);
        }
    }

    private string Binary(BinaryExpressionSyntax binary)
    {
        var left = Expression(binary.Left);
        var right = Expression(binary.Right);
        return binary.OperatorToken.Kind switch
        {
            SyntaxKind.PlusToken => $"smile.add({left}, {right})",
            SyntaxKind.MinusToken => $"smile.sub({left}, {right})",
            SyntaxKind.StarToken => $"smile.mul({left}, {right})",
            SyntaxKind.SlashToken => $"smile.div({left}, {right})",
            SyntaxKind.ModKeyword => $"smile.mod({left}, {right})",
            SyntaxKind.EqualsToken => $"(({left}) === ({right}) ? 1 : 0)",
            SyntaxKind.NotEqualsToken => $"(({left}) !== ({right}) ? 1 : 0)",
            SyntaxKind.LessToken => $"(({left}) < ({right}) ? 1 : 0)",
            SyntaxKind.GreaterToken => $"(({left}) > ({right}) ? 1 : 0)",
            SyntaxKind.LessOrEqualsToken => $"(({left}) <= ({right}) ? 1 : 0)",
            SyntaxKind.GreaterOrEqualsToken => $"(({left}) >= ({right}) ? 1 : 0)",
            SyntaxKind.AndKeyword => $"((smile.isTrue({left}) && smile.isTrue({right})) ? 1 : 0)",
            SyntaxKind.OrKeyword => $"((smile.isTrue({left}) || smile.isTrue({right})) ? 1 : 0)",
            _ => throw UnsupportedExpression(binary, binary.OperatorToken.Text)
        };
    }

    private string Call(CallExpressionSyntax call)
    {
        var arguments = Arguments(call.Arguments);
        return call.Identifier.Kind switch
        {
            SyntaxKind.TimerKeyword => "smile.timer()",
            SyntaxKind.AbsKeyword => $"smile.abs({arguments})",
            SyntaxKind.MinKeyword => $"smile.min({arguments})",
            SyntaxKind.MaxKeyword => $"smile.max({arguments})",
            SyntaxKind.RgbKeyword => $"smile.rgb({arguments})",
            SyntaxKind.GameClosedKeyword => "smile.gameClosed()",
            SyntaxKind.KeyHeldKeyword => $"smile.keyHeld({arguments})",
            _ => $"await {Routine(call.Identifier)}({arguments})"
        };
    }

    private string Arguments(IEnumerable<ExpressionSyntax> arguments) => string.Join(", ", arguments.Select(Expression));

    private string PrintItem(ExpressionSyntax expression) =>
        _analysis.SemanticModel.GetType(expression) == SmileType.Boolean
            ? $"smile.booleanText({Expression(expression)})"
            : Expression(expression);

    private string Variable(SyntaxToken identifier) => _variableNames[ResolveVariable(identifier)];

    private VariableSymbol ResolveVariable(SyntaxToken identifier)
    {
        if (_analysis.SemanticModel.TryResolveVariable(identifier.Text, _currentRoutine?.Name, out var symbol))
            return symbol;
        throw new WebTargetException(_currentSource, "SML5101", identifier.Span, $"Web target could not resolve variable '{identifier.Text}'.");
    }

    private string Routine(SyntaxToken identifier)
    {
        if (_analysis.SemanticModel.TryGetRoutine(identifier.Text, out var routine))
            return _routineNames[routine];
        throw new WebTargetException(_currentSource, "SML5101", identifier.Span, $"Web target could not resolve routine '{identifier.Text}'.");
    }

    private void Unsupported(SyntaxNode node, string feature) =>
        throw new WebTargetException(_currentSource, "SML5101", node.Span, $"Web target does not yet support {feature}.");

    private WebTargetException UnsupportedExpression(SyntaxNode node, string feature) =>
        new(_currentSource, "SML5101", node.Span, $"Web target does not yet support expression '{feature}'.");

    private IOrderedEnumerable<VariableSymbol> OrderedSymbols() =>
        _analysis.SemanticModel.Symbols.Values.OrderBy(item => item.SourceOrdinal).ThenBy(item => item.DeclarationSpan.Start);

    private IOrderedEnumerable<RoutineSymbol> OrderedRoutines() =>
        _analysis.SemanticModel.Routines.Values.OrderBy(item => item.SourceOrdinal).ThenBy(item => item.Declaration.Span.Start);

    private string Temporary(string purpose) => $"t_{_temporaryId++}_{purpose}";

    private void Line(string text = "") => _builder.Append(' ', _indent * 4).AppendLine(text);

    private static string Json(string value) => JsonSerializer.Serialize(value);

    private static string Sanitize(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
            builder.Append(char.IsLetterOrDigit(character) || character == '_' ? char.ToLowerInvariant(character) : '_');
        return builder.Length == 0 ? "symbol" : builder.ToString();
    }
}

internal sealed class WebTargetException : Exception
{
    public WebTargetException(SourceText source, string code, TextSpan span, string message) : base(message)
    {
        SourceText = source;
        Code = code;
        Span = span;
    }

    public SourceText SourceText { get; }
    public string Code { get; }
    public TextSpan Span { get; }
}
