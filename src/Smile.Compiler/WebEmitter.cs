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
    private readonly Dictionary<RecordTypeSymbol, string> _recordNames = new();
    private readonly Dictionary<RecordFieldSymbol, string> _fieldNames = new();
    private readonly string _appIdentity;
    private readonly IReadOnlyList<string> _assetPaths;
    private readonly Stack<string> _forExitLabels = new();
    private readonly Stack<string> _doExitLabels = new();
    private SourceText _currentSource;
    private RoutineSymbol? _currentRoutine;
    private int _indent;
    private int _temporaryId;

    public WebEmitter(SmileAnalysisResult analysis, string? appIdentity = null,
        IReadOnlyList<string>? assetPaths = null)
    {
        _analysis = analysis;
        _appIdentity = string.IsNullOrWhiteSpace(appIdentity) ? "Program" : appIdentity;
        _assetPaths = assetPaths ?? Array.Empty<string>();
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
        EmitRecordHelpers();
        EmitGlobalDeclarations();

        foreach (var routine in OrderedRoutines())
        {
            EmitRoutine(routine);
            Line();
        }

        Line("async function smileMain() {");
        _indent++;
        _currentSource = _analysis.BoundSyntaxTree.Source;
        Line($"smile.configure({Json(_appIdentity)}, [{string.Join(", ", _assetPaths.Select(Json))}]);");
        Line("try {");
        _indent++;
        EmitStatements(_analysis.BoundSyntaxTree.Root.Statements, topLevel: true);
        _indent--;
        Line("} finally {");
        _indent++;
        foreach (var symbol in OrderedSymbols().Where(symbol => !symbol.IsConstant && symbol.Type.RequiresCleanup))
            EmitRelease(symbol);
        _indent--;
        Line("}");
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

        id = 0;
        foreach (var type in OrderedRecordTypes())
        {
            var typeId = id++;
            _recordNames[type] = $"record_{typeId}_{Sanitize(type.RuntimeIdentity)}";
            foreach (var field in type.Fields.OrderBy(field => field.Ordinal))
                _fieldNames[field] = $"__smile_r{typeId}_f{field.Ordinal}";
        }
    }

    private void EmitRecordHelpers()
    {
        foreach (var type in OrderedRecordTypes())
        {
            var name = _recordNames[type];
            var defaults = string.Join(", ", type.Fields.Select(field =>
                $"{Json(FieldKey(field))}: {DefaultValue(field.Type)}"));
            var copies = string.Join(", ", type.Fields.Select(field =>
                $"{Json(FieldKey(field))}: {CloneValue(field.Type, $"value[{Json(FieldKey(field))}]")}"));
            Line($"function {name}_default() {{ return {{ {defaults} }}; }}");
            Line($"function {name}_clone(value) {{ return {{ {copies} }}; }}");
            var clears = string.Join(" ", type.Fields.Where(field => field.Type.RequiresCleanup).Select(field =>
                field.Type is RecordTypeSymbol nested
                    ? $"{_recordNames[nested]}_clear(value[{Json(FieldKey(field))}]);"
                    : field.Type == SmileType.Image
                        ? $"smile.imageRelease(value[{Json(FieldKey(field))}]); value[{Json(FieldKey(field))}] = null;"
                        : string.Empty));
            Line($"function {name}_clear(value) {{ if (!value) return; {clears} }}");
        }
        if (_recordNames.Count != 0)
            Line();
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
        Line("try {");
        _indent++;
        EmitStatements(routine.Declaration.Statements, topLevel: false);
        _indent--;
        Line("} finally {");
        _indent++;
        foreach (var local in routine.LocalSymbols.Values.Where(local => local.Type.RequiresCleanup &&
                     local.ParameterMode != ParameterPassingMode.ByRef))
            EmitRelease(local);
        _indent--;
        Line("}");
        _indent--;
        Line("}");
        _currentRoutine = null;
    }

    private string InitialValue(VariableSymbol symbol)
    {
        if (symbol.IsArray)
            return symbol.Type is RecordTypeSymbol
                ? $"smile.array([{string.Join(", ", symbol.ArrayDimensions)}], () => {DefaultValue(symbol.Type)})"
                : $"smile.array([{string.Join(", ", symbol.ArrayDimensions)}], {DefaultValue(symbol.Type)})";
        return symbol.IsConstant ? ConstantValue(symbol.ConstantValue) : DefaultValue(symbol.Type);
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
            case ConstStatementSyntax or DimStatementSyntax or TypeDeclarationSyntax:
                return;
            case AssignmentStatementSyntax assignment:
                EmitAssignment(assignment);
                return;
            case GetKeyStatementSyntax getKey:
                Line(WriteVariable(getKey.Identifier, "smile.getKey()") + ";");
                return;
            case RandomStatementSyntax random:
                Line(WriteVariable(random.Identifier,
                    $"smile.random({Expression(random.Minimum)}, {Expression(random.Maximum)})") + ";");
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
                Line($"await {Routine(call.Identifier)}({RoutineArguments(call.Identifier, call.Arguments)});");
                return;
            case ReturnStatementSyntax returnStatement:
                Line(returnStatement.Expression == null ? "return;" :
                    $"return {ReturnValue(_currentRoutine!.ReturnType, returnStatement.Expression)};");
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
            case DrawImageStatementSyntax image:
                EmitDrawImage(image);
                return;
            case ImageLoadStatementSyntax image:
                EmitImageLoad(image);
                return;
            case ClipRectangleStatementSyntax clip:
                EmitClip(clip, topLevel);
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
                Line(WriteVariable(load.Identifier,
                    $"smile.loadInt({Json(load.Key.Value as string ?? string.Empty)}, {Expression(load.DefaultValue)})") + ";");
                return;
            case TextFileLoadStatementSyntax textFileLoad:
                EmitTextFileLoad(textFileLoad);
                return;
            case DataLoadStatementSyntax dataLoad:
                Line(WriteTarget(dataLoad.CountTarget,
                    $"smile.loadData({Expression(dataLoad.Key)}, {_variableNames[ResolveVariable(dataLoad.Destination)]})") + ";");
                return;
            case DataSaveStatementSyntax dataSave:
                Line($"smile.saveData({_variableNames[ResolveVariable(dataSave.Source)]}, {Expression(dataSave.Count)}, {Expression(dataSave.Key)});");
                return;
            case SaveStatementSyntax save:
                Line($"smile.saveInt({Json(save.Key.Value as string ?? string.Empty)}, {ReadVariable(save.Identifier)});");
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
        var targetType = TargetType(assignment.Target);
        if (targetType == SmileType.Image)
        {
            Line(WriteTarget(assignment.Target,
                $"smile.imageMoveAssign({ReadTarget(assignment.Target)}, {value})") + ";");
            return;
        }
        if (targetType is RecordTypeSymbol record && record.RequiresCleanup)
        {
            var temporary = Temporary("record");
            Line($"const {temporary} = {RecordValue(assignment.Expression, targetType, value)};");
            Line($"{_recordNames[record]}_clear({ReadTarget(assignment.Target)});");
            Line(WriteTarget(assignment.Target, temporary) + ";");
            return;
        }
        if (assignment.Target.Fields.Count != 0)
        {
            Line($"{TargetLocation(assignment.Target)} = {StoreValue(targetType, value)};");
            return;
        }
        if (!assignment.Target.IsArrayElement)
        {
            var targetSymbol = ResolveVariable(assignment.Target.Identifier);
            var stored = StoreValue(targetSymbol.Type, value);
            Line(WriteVariable(targetSymbol, stored) + ";");
            return;
        }

        var symbol = ResolveVariable(assignment.Target.Identifier);
        var arrayValue = StoreValue(symbol.Type, value);
        Line($"smile.set({_variableNames[symbol]}, [{Arguments(assignment.Target.Indices)}], {arrayValue});");
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
        var counter = ResolveVariable(statement.Identifier);
        var limit = Temporary("limit");
        var label = Temporary("for");
        Line(WriteVariable(counter, $"smile.safe({Expression(statement.LowerBound)})") + ";");
        Line($"const {limit} = smile.safe({Expression(statement.UpperBound)});");
        var comparison = statement.IsDescending ? ">=" : "<=";
        var step = statement.IsDescending ? "-1" : "1";
        Line($"{label}: for (; {ReadVariable(counter)} {comparison} {limit}; {WriteVariable(counter, $"smile.add({ReadVariable(counter)}, {step})")}) {{");
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
        var selectedValue = Expression(statement.Expression);
        if (_analysis.SemanticModel.GetType(statement.Expression) == SmileType.Number)
            selectedValue = $"smile.safe({selectedValue})";
        Line($"const {selected} = {selectedValue};");
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
            Unsupported(statement, $"Exit {statement.TargetKeyword.Text}");
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
                Line($"smile.drawText({Expression(statement.TextExpression!)}, {arguments}, {(statement.Centered ? "true" : "false")});");
                return;
            case GraphicsOperation.DrawNumber:
                Line($"smile.drawNumber({arguments}, {(statement.Centered ? "true" : "false")});");
                return;
            default:
                Unsupported(statement, statement.Operation.ToString());
                return;
        }
    }

    private void EmitDrawImage(DrawImageStatementSyntax image)
    {
        var values = new[]
        {
            Expression(image.Image), ImageValue(image.SourceX, "0"), ImageValue(image.SourceY, "0"),
            ImageValue(image.SourceWidth, "-1"), ImageValue(image.SourceHeight, "-1"),
            Expression(image.DestinationX), Expression(image.DestinationY),
            ImageValue(image.DestinationWidth, "-1"), ImageValue(image.DestinationHeight, "-1"),
            ImageValue(image.Opacity, "100"), ((int)image.Filter).ToString(CultureInfo.InvariantCulture),
            ((int)image.Flip).ToString(CultureInfo.InvariantCulture), ImageValue(image.AnchorX, "0"),
            ImageValue(image.AnchorY, "0")
        };
        Line($"smile.drawImage({string.Join(", ", values)});");
    }

    private string ImageValue(ExpressionSyntax? expression, string fallback) =>
        expression == null ? fallback : Expression(expression);

    private void EmitImageLoad(ImageLoadStatementSyntax image)
    {
        if (image.IsUnload)
        {
            Line(WriteTarget(image.Target, $"smile.imageMoveAssign({ReadTarget(image.Target)}, null)") + ";");
            return;
        }
        var loaded = Temporary("image");
        Line($"const {loaded} = await smile.loadImage({Expression(image.Path!)});");
        Line(WriteTarget(image.Target, $"smile.imageMoveAssign({ReadTarget(image.Target)}, {loaded})") + ";");
    }

    private void EmitClip(ClipRectangleStatementSyntax clip, bool topLevel)
    {
        Line($"smile.pushClip({Arguments(clip.Arguments)});");
        Line("try {");
        _indent++;
        EmitStatements(clip.Statements, topLevel);
        _indent--;
        Line("} finally {");
        _indent++;
        Line("smile.popClip();");
        _indent--;
        Line("}");
    }

    private void EmitSound(SoundStatementSyntax statement)
    {
        if (statement.IsStop)
        {
            Line(statement.Channel == null ? "smile.stopSound();" : $"smile.stopSound({Expression(statement.Channel)});");
            return;
        }
        var path = (statement.Path?.Value as string ?? string.Empty).Replace('\\', '/');
        Line($"await smile.playSound({Json(path)}, {(statement.Channel == null ? "0" : Expression(statement.Channel))});");
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
        Line(WriteVariable(statement.CountIdentifier, $"await smile.loadTextFile({Json(path)}, {destination})") + ";");
    }

    private string Expression(ExpressionSyntax expression)
    {
        switch (expression)
        {
            case LiteralExpressionSyntax literal when literal.Value is long number:
                if (number is > MaxSafeInteger or < -MaxSafeInteger)
                    throw new WebTargetException(_currentSource, "SML5102", literal.Span, "Web target Number literals must be within JavaScript's safe integer range.");
                return number.ToString(CultureInfo.InvariantCulture);
            case LiteralExpressionSyntax literal when literal.Value is bool boolean:
                return boolean ? "true" : "false";
            case LiteralExpressionSyntax literal when literal.Value is string text:
                return Json(text);
            case NameExpressionSyntax name:
                if (SyntaxFacts.IsBuiltInConstant(name.Identifier.Kind))
                    return SyntaxFacts.GetBuiltInConstantValue(name.Identifier.Kind).ToString(CultureInfo.InvariantCulture);
                var namedValue = ReadVariable(name.Identifier);
                return _analysis.SemanticModel.GetType(name) == SmileType.Image
                    ? $"smile.imageRetain({namedValue})" : namedValue;
            case ArrayAccessExpressionSyntax array:
                var arrayValue = $"smile.get({_variableNames[ResolveVariable(array.Identifier)]}, [{Arguments(array.Indices)}])";
                return _analysis.SemanticModel.GetType(array) == SmileType.Image
                    ? $"smile.imageRetain({arrayValue})" : arrayValue;
            case FieldAccessExpressionSyntax field:
                if (!_analysis.SemanticModel.TryGetField(field, out var fieldSymbol))
                    throw UnsupportedExpression(field, "unbound record field");
                var fieldValue = $"({Expression(field.Receiver)})[{Json(FieldKey(fieldSymbol))}]";
                return _analysis.SemanticModel.GetType(field) == SmileType.Image
                    ? $"smile.imageRetain({fieldValue})" : fieldValue;
            case ParenthesizedExpressionSyntax parenthesized:
                return $"({Expression(parenthesized.Expression)})";
            case UnaryExpressionSyntax unary:
                return unary.OperatorToken.Kind switch
                {
                    SyntaxKind.MinusToken => $"smile.neg({Expression(unary.Operand)})",
                    SyntaxKind.NotKeyword => $"(!smile.isTrue({Expression(unary.Operand)}))",
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
            SyntaxKind.PlusToken when _analysis.SemanticModel.GetType(binary) == SmileType.Text => $"(({left}) + ({right}))",
            SyntaxKind.PlusToken => $"smile.add({left}, {right})",
            SyntaxKind.MinusToken => $"smile.sub({left}, {right})",
            SyntaxKind.StarToken => $"smile.mul({left}, {right})",
            SyntaxKind.SlashToken => $"smile.div({left}, {right})",
            SyntaxKind.ModKeyword => $"smile.mod({left}, {right})",
            SyntaxKind.EqualsToken => $"(({left}) === ({right}))",
            SyntaxKind.NotEqualsToken => $"(({left}) !== ({right}))",
            SyntaxKind.LessToken => $"(({left}) < ({right}))",
            SyntaxKind.GreaterToken => $"(({left}) > ({right}))",
            SyntaxKind.LessOrEqualsToken => $"(({left}) <= ({right}))",
            SyntaxKind.GreaterOrEqualsToken => $"(({left}) >= ({right}))",
            SyntaxKind.AndKeyword => $"(smile.isTrue({left}) && smile.isTrue({right}))",
            SyntaxKind.OrKeyword => $"(smile.isTrue({left}) || smile.isTrue({right}))",
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
            SyntaxKind.GameClosedKeyword => "smile.isTrue(smile.gameClosed())",
            SyntaxKind.KeyHeldKeyword => $"smile.isTrue(smile.keyHeld({arguments}))",
            SyntaxKind.ImageWidthKeyword => $"smile.imageWidth({arguments})",
            SyntaxKind.ImageHeightKeyword => $"smile.imageHeight({arguments})",
            SyntaxKind.ImageLoadedKeyword => $"smile.imageLoaded({arguments})",
            SyntaxKind.TextWidthKeyword => $"smile.textWidth({arguments})",
            SyntaxKind.TextHeightKeyword => $"smile.textHeight({arguments})",
            SyntaxKind.TextLengthKeyword => $"smile.textLength({arguments})",
            SyntaxKind.TextCodeAtKeyword => $"smile.textCodeAt({arguments})",
            SyntaxKind.TextSliceKeyword => $"smile.textSlice({arguments})",
            _ => $"await {Routine(call.Identifier)}({RoutineArguments(call.Identifier, call.Arguments)})"
        };
    }

    private string Arguments(IEnumerable<ExpressionSyntax> arguments) => string.Join(", ", arguments.Select(Expression));

    private string PrintItem(ExpressionSyntax expression) =>
        _analysis.SemanticModel.GetType(expression) == SmileType.Boolean
            ? $"smile.booleanText({Expression(expression)})"
            : Expression(expression);

    private string ReadVariable(SyntaxToken identifier) => ReadVariable(ResolveVariable(identifier));

    private string ReadVariable(VariableSymbol symbol) => symbol.ParameterMode == ParameterPassingMode.ByRef
        ? _variableNames[symbol] + ".get()"
        : _variableNames[symbol];

    private string WriteVariable(SyntaxToken identifier, string value) =>
        WriteVariable(ResolveVariable(identifier), value);

    private string WriteVariable(VariableSymbol symbol, string value) => symbol.ParameterMode == ParameterPassingMode.ByRef
        ? _variableNames[symbol] + $".set({value})"
        : _variableNames[symbol] + $" = {value}";

    private string RoutineArguments(SyntaxToken identifier, IReadOnlyList<ExpressionSyntax> arguments)
    {
        if (!_analysis.SemanticModel.TryGetRoutine(identifier.Text, out var routine))
            return Arguments(arguments);
        return string.Join(", ", arguments.Select((argument, index) =>
        {
            if (index >= routine.Parameters.Count)
                return Expression(argument);
            var parameter = routine.Parameters[index];
            if (parameter.ParameterMode == ParameterPassingMode.ByRef)
                return Reference(argument);
            var value = Expression(argument);
            if (parameter.Type is RecordTypeSymbol)
                return RecordValue(argument, parameter.Type, value);
            return !parameter.HasDeclaredType && parameter.Type == SmileType.Number &&
                   _analysis.SemanticModel.GetType(argument) == SmileType.Boolean
                ? $"(smile.isTrue({value}) ? 1 : 0)"
                : value;
        }));
    }

    private string Reference(ExpressionSyntax expression)
    {
        if (expression is NameExpressionSyntax name)
        {
            var symbol = ResolveVariable(name.Identifier);
            if (symbol.ParameterMode == ParameterPassingMode.ByRef)
                return _variableNames[symbol];
            return $"smile.ref(() => {_variableNames[symbol]}, value => {{ {_variableNames[symbol]} = value; }})";
        }
        if (expression is ArrayAccessExpressionSyntax array)
        {
            var symbol = ResolveVariable(array.Identifier);
            return $"smile.refArray({_variableNames[symbol]}, [{Arguments(array.Indices)}])";
        }
        if (expression is FieldAccessExpressionSyntax field)
        {
            if (!_analysis.SemanticModel.TryGetField(field, out var fieldSymbol))
                return "smile.invalidRef()";
            var receiver = Expression(field.Receiver);
            var key = Json(FieldKey(fieldSymbol));
            return $"(() => {{ const target = {receiver}; return smile.ref(() => target[{key}], value => {{ target[{key}] = value; }}); }})()";
        }
        return "smile.invalidRef()";
    }

    private string DefaultValue(SmileType type) => type is RecordTypeSymbol record
        ? $"{_recordNames[record]}_default()"
        : type == SmileType.Image
        ? "null"
        : type == SmileType.Text
        ? "\"\""
        : type == SmileType.Boolean ? "false" : "0";

    private string CloneValue(SmileType type, string value) => type is RecordTypeSymbol record
        ? $"{_recordNames[record]}_clone({value})"
        : type == SmileType.Image
        ? $"smile.imageRetain({value})"
        : value;

    private string ReturnValue(SmileType type, ExpressionSyntax expression)
    {
        var value = Expression(expression);
        if (type is RecordTypeSymbol)
            return RecordValue(expression, type, value);
        return value;
    }

    private string RecordValue(ExpressionSyntax expression, SmileType type, string value) =>
        IsOwnedRecordExpression(expression) ? value : CloneValue(type, value);

    private bool IsOwnedRecordExpression(ExpressionSyntax expression) => expression switch
    {
        ParenthesizedExpressionSyntax parenthesized => IsOwnedRecordExpression(parenthesized.Expression),
        CallExpressionSyntax call => _analysis.SemanticModel.GetType(call) is RecordTypeSymbol,
        _ => false
    };

    private string StoreValue(SmileType type, string value) => type == SmileType.Number
        ? $"smile.safe({value})"
        : CloneValue(type, value);

    private string ReadTarget(AssignmentTargetSyntax target) => TargetLocation(target);

    private string WriteTarget(AssignmentTargetSyntax target, string value)
    {
        if (target.Fields.Count != 0)
            return $"{TargetLocation(target)} = {value}";
        var symbol = ResolveVariable(target.Identifier);
        if (!target.IsArrayElement)
            return WriteVariable(symbol, value);
        return $"smile.set({_variableNames[symbol]}, [{Arguments(target.Indices)}], {value})";
    }

    private void EmitRelease(VariableSymbol symbol)
    {
        if (symbol.Type == SmileType.Text)
            return;
        var name = _variableNames[symbol];
        if (symbol.IsArray)
        {
            if (symbol.Type == SmileType.Image)
            {
                Line($"for (const value of {name}.data) smile.imageRelease(value);");
                Line($"{name}.data.fill(null);");
            }
            else if (symbol.Type is RecordTypeSymbol record)
                Line($"for (const value of {name}.data) {_recordNames[record]}_clear(value);");
            return;
        }
        var value = ReadVariable(symbol);
        if (symbol.Type == SmileType.Image)
        {
            Line($"smile.imageRelease({value});");
            if (symbol.ParameterMode != ParameterPassingMode.ByRef)
                Line($"{name} = null;");
        }
        else if (symbol.Type is RecordTypeSymbol record)
            Line($"{_recordNames[record]}_clear({value});");
    }

    private SmileType TargetType(AssignmentTargetSyntax target)
    {
        SmileType type = ResolveVariable(target.Identifier).Type;
        foreach (var token in target.Fields)
        {
            if (type is not RecordTypeSymbol record || !record.TryGetField(token.Text, out var field))
                throw new WebTargetException(_currentSource, "SML5101", token.Span,
                    $"Web target could not resolve record field '{token.Text}'.");
            type = field.Type;
        }
        return type;
    }

    private string TargetLocation(AssignmentTargetSyntax target)
    {
        var symbol = ResolveVariable(target.Identifier);
        var location = target.IsArrayElement
            ? $"smile.get({_variableNames[symbol]}, [{Arguments(target.Indices)}])"
            : ReadVariable(symbol);
        SmileType type = symbol.Type;
        foreach (var token in target.Fields)
        {
            var record = (RecordTypeSymbol)type;
            record.TryGetField(token.Text, out var field);
            location = $"({location})[{Json(FieldKey(field))}]";
            type = field.Type;
        }
        return location;
    }

    private static string ConstantValue(object value) => value switch
    {
        string text => Json(text),
        bool boolean => boolean ? "true" : "false",
        long number => number.ToString(CultureInfo.InvariantCulture),
        _ => "0"
    };

    private VariableSymbol ResolveVariable(SyntaxToken identifier)
    {
        if (_analysis.SemanticModel.TryResolveVariable(identifier.Text, _currentRoutine, out var symbol))
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

    private IOrderedEnumerable<RecordTypeSymbol> OrderedRecordTypes() =>
        _analysis.SemanticModel.Types.Values.OrderBy(item => item.SourceOrdinal).ThenBy(item => item.DeclarationSpan.Start);

    private string FieldKey(RecordFieldSymbol field) => _fieldNames.TryGetValue(field, out var key)
        ? key
        : throw new InvalidOperationException($"Web record field '{field.Name}' does not have a bound runtime key.");

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
