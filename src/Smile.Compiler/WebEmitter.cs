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
    private readonly Dictionary<ClassTypeSymbol, string> _classNames = new();
    private readonly Dictionary<ClassFieldSymbol, string> _classFieldNames = new();
    private readonly string _appIdentity;
    private readonly IReadOnlyList<string> _assetPaths;
    private readonly Stack<string> _forExitLabels = new();
    private readonly Stack<string> _doExitLabels = new();
    private readonly Dictionary<WithStatementSyntax, string> _withReferences = new();
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
        EmitClassHelpers();
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

        id = 0;
        foreach (var type in OrderedClassTypes())
        {
            var typeId = id++;
            _classNames[type] = $"class_{typeId}_{Sanitize(type.RuntimeIdentity)}";
            foreach (var field in type.Fields.OrderBy(field => field.Ordinal))
                _classFieldNames[field] = $"__smile_c{typeId}_f{field.Ordinal}";
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

    private void EmitClassHelpers()
    {
        foreach (var type in OrderedClassTypes())
        {
            var name = _classNames[type];
            var defaults = string.Join(", ", type.Fields.Select(field =>
                $"{Json(FieldKey(field))}: {ClassFieldDefault(field)}"));
            Line($"function {name}_create() {{ return smile.classCreate({{ {defaults} }}, {name}_finalize); }}");
            Line($"function {name}_finalize(value) {{");
            _indent++;
            foreach (var field in type.Fields.Where(field => !field.IsArray && field.Type == SmileType.Text)
                         .OrderByDescending(field => field.Ordinal))
                Line($"value[{Json(FieldKey(field))}] = \"\";");
            foreach (var field in type.Fields.Where(field => !field.IsArray &&
                             field.Type is RecordTypeSymbol { RequiresValueCleanup: true })
                         .OrderByDescending(field => field.Ordinal))
            {
                var record = (RecordTypeSymbol)field.Type;
                Line($"{_recordNames[record]}_clear(value[{Json(FieldKey(field))}]);");
                Line($"value[{Json(FieldKey(field))}] = {_recordNames[record]}_default();");
            }
            foreach (var field in type.Fields.Where(field => field.IsArray && field.Type.RequiresValueCleanup)
                         .OrderByDescending(field => field.Ordinal))
            {
                var array = $"value[{Json(FieldKey(field))}].data";
                Line($"for (let index = {field.ElementCount - 1}; index >= 0; index -= 1) {{");
                _indent++;
                if (field.Type is RecordTypeSymbol record)
                {
                    Line($"{_recordNames[record]}_clear({array}[index]);");
                    Line($"{array}[index] = {_recordNames[record]}_default();");
                }
                else
                    Line($"{array}[index] = \"\";");
                _indent--;
                Line("}");
            }
            _indent--;
            Line("}");
        }
        if (_classNames.Count != 0)
            Line();
    }

    private string ClassFieldDefault(ClassFieldSymbol field)
    {
        if (!field.IsArray)
            return DefaultValue(field.Type);
        var initial = field.Type is RecordTypeSymbol
            ? $"() => {DefaultValue(field.Type)}" : DefaultValue(field.Type);
        return $"smile.array([{string.Join(", ", field.Dimensions)}], {initial})";
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
        var abiParameters = RoutineAbiParameters(routine).ToArray();
        var parameters = string.Join(", ", abiParameters.Select(parameter => _variableNames[parameter]));
        Line($"async function {_routineNames[routine]}({parameters}) {{");
        _indent++;
        foreach (var local in routine.LocalSymbols.Values
                     .Where(local => !abiParameters.Contains(local))
                     .OrderBy(local => local.DeclarationSpan.Start))
        {
            Line($"let {_variableNames[local]} = {InitialValue(local)};");
        }
        Line("try {");
        _indent++;
        EmitStatements(routine.BodyStatements, topLevel: false);
        _indent--;
        Line("} finally {");
        _indent++;
        foreach (var local in routine.LocalSymbols.Values.Where(local => local.Type.RequiresCleanup &&
                     local.ParameterMode != ParameterPassingMode.ByRef && local is not InstanceReceiverSymbol))
            EmitRelease(local);
        _indent--;
        Line("}");
        _indent--;
        Line("}");
        _currentRoutine = null;
    }

    private static IEnumerable<VariableSymbol> RoutineAbiParameters(RoutineSymbol routine)
    {
        if (routine.Receiver != null)
            yield return routine.Receiver;
        if (routine.SetterValue != null)
            yield return routine.SetterValue;
        foreach (var parameter in routine.Parameters)
            yield return parameter;
    }

    private string InitialValue(VariableSymbol symbol)
    {
        if (symbol.IsArray)
            return symbol.Type is RecordTypeSymbol
                ? $"smile.array([{string.Join(", ", symbol.ArrayDimensions)}], () => {DefaultValue(symbol.Type)})"
                : $"smile.array([{string.Join(", ", symbol.ArrayDimensions)}], {DefaultValue(symbol.Type)})";
        return symbol.IsConstant ? ConstantValue(symbol.ConstantValue, symbol.Type) : DefaultValue(symbol.Type);
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
            case ConstStatementSyntax or TypeDeclarationSyntax or ClassDeclarationSyntax or EnumDeclarationSyntax:
                return;
            case DimStatementSyntax dim:
                if (dim.NewInitializer != null)
                {
                    var initializer = _analysis.SemanticModel.ClassInitializers.SingleOrDefault(binding =>
                        ReferenceEquals(binding.Declaration, dim));
                    if (initializer != null)
                    {
                        var source = _currentSource;
                        _currentSource = initializer.Source;
                        EmitClassInitializer(initializer);
                        _currentSource = source;
                    }
                }
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
            case WithStatementSyntax withStatement:
                EmitWith(withStatement, topLevel);
                return;
            case ForStatementSyntax forStatement:
                EmitFor(forStatement, topLevel);
                return;
            case DoStatementSyntax doStatement:
                EmitDo(doStatement, topLevel);
                return;
            case CallStatementSyntax call:
                Line($"{RoutineCall(call, call.Identifier)};");
                return;
            case MemberCallStatementSyntax call:
                Line($"{RoutineCall(call, call.Member)};");
                return;
            case LeadingMemberCallStatementSyntax call:
                Line($"{RoutineCall(call, call.Member)};");
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
        if (_analysis.SemanticModel.TryGetBoundCall(assignment, out var boundCall) &&
            boundCall.Routine.SymbolKind == RoutineSymbolKind.PropertySet)
        {
            Line($"{RoutineCall(assignment)};");
            return;
        }
        var targetType = TargetType(assignment.Target);
        var value = Temporary("value");
        if (targetType == SmileType.Image || targetType.IsClass)
        {
            Line($"const {value} = {Expression(assignment.Expression)};");
        }
        else if (targetType is RecordTypeSymbol record && record.RequiresCleanup)
        {
            Line($"const {value} = {RecordValue(assignment.Expression, targetType, Expression(assignment.Expression))};");
        }
        else
        {
            Line($"const {value} = {StoreValue(targetType, Expression(assignment.Expression))};");
        }

        if (targetType == SmileType.Image || targetType.IsClass)
        {
            var reference = Temporary("target");
            var transferred = Temporary("transferred");
            Line($"let {reference};");
            Line($"let {transferred} = false;");
            Line("try {");
            _indent++;
            Line($"{reference} = {Reference(assignment.Target.Location)};");
            Line($"{reference}.set({(targetType.IsClass ? "smile.classMoveAssign" : "smile.imageMoveAssign")}({reference}.get(), {value}));");
            Line($"{transferred} = true;");
            _indent--;
            var release = targetType.IsClass ? "smile.classRelease" : "smile.imageRelease";
            Line($"}} finally {{ if ({reference}) {reference}.release(); if (!{transferred}) {release}({value}); }}");
        }
        else if (targetType is RecordTypeSymbol cleanupRecord && cleanupRecord.RequiresCleanup)
        {
            var reference = Temporary("target");
            var transferred = Temporary("transferred");
            Line($"let {reference};");
            Line($"let {transferred} = false;");
            Line("try {");
            _indent++;
            Line($"{reference} = {Reference(assignment.Target.Location)};");
            Line($"{_recordNames[cleanupRecord]}_clear({reference}.get());");
            Line($"{reference}.set({value});");
            Line($"{transferred} = true;");
            _indent--;
            Line($"}} finally {{ if ({reference}) {reference}.release(); " +
                 $"if (!{transferred}) {_recordNames[cleanupRecord]}_clear({value}); }}");
        }
        else if (_analysis.SemanticModel.TryGetClassLocationOwner(assignment.Target.Location, out _))
        {
            var reference = Temporary("target");
            Line($"const {reference} = {Reference(assignment.Target.Location)};");
            Line($"try {{ {reference}.set({value}); }} finally {{ {reference}.release(); }}");
        }
        else
            Line(WriteTarget(assignment.Target, value) + ";");
    }

    private void EmitClassInitializer(ClassInitializerBinding initializer)
    {
        var target = _variableNames[initializer.Target];
        var value = Expression(initializer.Initializer);
        if (initializer.Target.ParameterMode == ParameterPassingMode.ByRef)
            Line($"{target}.set(smile.classMoveAssign({target}.get(), {value}));");
        else
            Line($"{target} = smile.classMoveAssign({target}, {value});");
    }

    private void EmitWith(WithStatementSyntax statement, bool topLevel)
    {
        var reference = Temporary("with");
        Line("{");
        _indent++;
        if (_analysis.SemanticModel.TryGetWithTarget(statement, out var binding) &&
            binding.TargetType is ClassTypeSymbol)
        {
            var value = Temporary("with_value");
            Line($"const {value} = smile.classRequire({Expression(statement.Target)});");
            Line($"const {reference} = smile.ref(() => {value}, () => {{ throw new Error(\"With Class roots cannot be rebound.\"); }});");
            Line("try {");
            _indent++;
            _withReferences[statement] = reference;
            EmitStatements(statement.Statements, topLevel);
            _withReferences.Remove(statement);
            _indent--;
            Line($"}} finally {{ smile.classRelease({value}); }}");
            _indent--;
            Line("}");
            return;
        }

        Line($"const {reference} = {Reference(statement.Target)};");
        Line("try {");
        _indent++;
        _withReferences[statement] = reference;
        EmitStatements(statement.Statements, topLevel);
        _withReferences.Remove(statement);
        _indent--;
        Line($"}} finally {{ {reference}.release(); }}");
        _indent--;
        Line("}");
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
        var destination = _variableNames[ResolveVariable(statement.Destination)];
        Line(WriteVariable(statement.CountIdentifier,
            $"await smile.loadTextFile({Expression(statement.Path)}, {destination})") + ";");
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
            case NothingExpressionSyntax:
                return "null";
            case NewExpressionSyntax creation:
                return RoutineCall(creation, creation.TypeToken);
            case NameExpressionSyntax name:
                if (SyntaxFacts.IsBuiltInConstant(name.Identifier.Kind))
                    return SyntaxFacts.GetBuiltInConstantValue(name.Identifier.Kind).ToString(CultureInfo.InvariantCulture);
                var namedValue = ReadVariable(name.Identifier);
                var namedType = _analysis.SemanticModel.GetType(name);
                return namedType == SmileType.Image
                    ? $"smile.imageRetain({namedValue})"
                    : namedType.IsClass ? $"smile.classRetain({namedValue})" : namedValue;
            case MeExpressionSyntax:
                if (_currentRoutine?.Receiver == null)
                    throw UnsupportedExpression(expression, "Me outside an instance member");
                var meValue = ReadVariable(_currentRoutine.Receiver);
                return _currentRoutine.Receiver.Type.IsClass ? $"smile.classRetain({meValue})" : meValue;
            case ArrayAccessExpressionSyntax array:
                var arrayValue = $"smile.get({_variableNames[ResolveVariable(array.Identifier)]}, [{Arguments(array.Indices)}])";
                return _analysis.SemanticModel.GetType(array) == SmileType.Image
                    ? $"smile.imageRetain({arrayValue})" : arrayValue;
            case IndexedExpressionSyntax indexed:
                if (!_analysis.SemanticModel.TryGetInstanceField(indexed, out var indexedField))
                    throw UnsupportedExpression(indexed, "unbound fixed-array field");
                return InstanceFieldValue(indexed, indexedField);
            case FieldAccessExpressionSyntax field:
                if (_analysis.SemanticModel.TryGetEnumMember(field, out var enumMember))
                    return EnumValue(enumMember.Value);
                if (_analysis.SemanticModel.TryGetBoundCall(field, out _))
                    return RoutineCall(field, field.Field);
                if (!_analysis.SemanticModel.TryGetInstanceField(field, out var fieldSymbol))
                    throw UnsupportedExpression(field, "unbound instance field");
                return InstanceFieldValue(field, fieldSymbol);
            case LeadingMemberAccessExpressionSyntax leading:
                if (_analysis.SemanticModel.TryGetBoundCall(leading, out _))
                    return RoutineCall(leading, leading.Member);
                var leadingValue = LeadingMemberLocation(leading);
                var leadingType = _analysis.SemanticModel.GetType(leading);
                return leadingType == SmileType.Image
                    ? $"smile.imageRetain({leadingValue})"
                    : leadingType.IsClass ? $"smile.classRetain({leadingValue})" : leadingValue;
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
            case IdentityExpressionSyntax identity:
                return Identity(identity);
            case CallExpressionSyntax call:
                return Call(call);
            case MemberInvocationExpressionSyntax call:
                return RoutineCall(call, call.Member);
            case LeadingMemberInvocationExpressionSyntax call:
                return RoutineCall(call, call.Member);
            default:
                throw UnsupportedExpression(expression, expression.GetType().Name);
        }
    }

    private string InstanceFieldValue(ExpressionSyntax expression, IInstanceFieldSymbol fieldSymbol)
    {
        var fieldType = _analysis.SemanticModel.GetType(expression);
        if (_analysis.SemanticModel.TryGetClassLocationOwner(expression, out _))
        {
            var reference = Temporary("class_field");
            var value = $"{reference}.get()";
            var ownedValue = fieldType is RecordTypeSymbol ? CloneValue(fieldType, value)
                : fieldType == SmileType.Image ? $"smile.imageRetain({value})"
                : fieldType.IsClass ? $"smile.classRetain({value})" : value;
            return $"await (async () => {{ const {reference} = {Reference(expression)}; " +
                   $"try {{ return {ownedValue}; }} finally {{ {reference}.release(); }} }})()";
        }

        if (expression is IndexedExpressionSyntax)
        {
            var indexedValue = Location(expression);
            return fieldType == SmileType.Image ? $"smile.imageRetain({indexedValue})"
                : fieldType.IsClass ? $"smile.classRetain({indexedValue})" : indexedValue;
        }

        var field = (FieldAccessExpressionSyntax)expression;
        var receiverValue = Expression(field.Receiver);
        var key = Json(FieldKey(fieldSymbol));
        if (!IsOwnedRecordExpression(field.Receiver))
        {
            var fieldValue = $"({receiverValue})[{key}]";
            return fieldType == SmileType.Image ? $"smile.imageRetain({fieldValue})"
                : fieldType.IsClass ? $"smile.classRetain({fieldValue})" : fieldValue;
        }

        var receiverType = (RecordTypeSymbol)_analysis.SemanticModel.GetType(field.Receiver);
        var capturedReceiver = Temporary("record");
        var capturedField = $"{capturedReceiver}[{key}]";
        var result = fieldType is RecordTypeSymbol
            ? CloneValue(fieldType, capturedField)
            : fieldType == SmileType.Image ? $"smile.imageRetain({capturedField})"
            : fieldType.IsClass ? $"smile.classRetain({capturedField})" : capturedField;
        return $"await (async () => {{ const {capturedReceiver} = {receiverValue}; " +
               $"try {{ return {result}; }} finally {{ {_recordNames[receiverType]}_clear({capturedReceiver}); }} }})()";
    }

    private string Identity(IdentityExpressionSyntax identity)
    {
        var left = Temporary("identity_left");
        var right = Temporary("identity_right");
        var leftType = _analysis.SemanticModel.GetType(identity.Left);
        var rightType = _analysis.SemanticModel.GetType(identity.Right);
        return $"await (async () => {{ const {left} = {Expression(identity.Left)}; " +
               $"try {{ const {right} = {Expression(identity.Right)}; try {{ return {left} " +
               $"{(identity.IsNegated ? "!==" : "===")} {right}; }} finally {{ " +
               $"{(rightType.IsClass ? $"smile.classRelease({right});" : string.Empty)} }} }} finally {{ " +
               $"{(leftType.IsClass ? $"smile.classRelease({left});" : string.Empty)} }} }})()";
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
        var arguments = Arguments(call.Arguments.Select(argument => argument.Expression));
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
            _ => RoutineCall(call, call.Identifier)
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

    private string RoutineCall(SyntaxNode callSyntax, SyntaxToken? identifier = null)
    {
        if (!_analysis.SemanticModel.TryGetBoundCall(callSyntax, out var call))
            throw new WebTargetException(_currentSource, "SML5101", identifier?.Span ?? callSyntax.Span,
                $"Web target could not bind routine call '{identifier?.Text ?? "member"}'.");

        var captures = call.SourceArguments.ToDictionary(argument => argument,
            _ => Temporary("argument"));
        var capturedFlags = call.SourceArguments
            .Where(RequiresCapturedCleanup)
            .ToDictionary(argument => argument, _ => Temporary("captured"));
        var byRefFlags = call.SourceArguments
            .Where(argument => argument.Parameter.ParameterMode == ParameterPassingMode.ByRef)
            .ToDictionary(argument => argument, _ => Temporary("reference_captured"));
        var receiver = call.InstanceReceiver == null ? null : Temporary("receiver");
        var receiverNeedsCleanup = call.InstanceReceiver != null &&
                                   (call.InstanceReceiver.ContainingType.IsClass ||
                                    call.InstanceReceiver.Kind == BoundInstanceReceiverKind.Expression);
        var receiverCaptured = receiverNeedsCleanup ? Temporary("receiver_captured") : null;
        var implicitValue = call.ImplicitValue == null ? null : Temporary("value");
        var implicitType = call.Routine.SetterValue?.Type;
        var implicitCaptured = implicitType != null && RequiresCapturedCleanup(implicitType)
            ? Temporary("captured") : null;
        var constructorType = call.Routine.IsConstructor
            ? call.Routine.ContainingType as ClassTypeSymbol ??
              throw new InvalidOperationException("A constructor must belong to a Class.")
            : null;
        var constructorResult = constructorType == null ? null : Temporary("instance");
        var constructorCaptured = constructorType == null ? null : Temporary("instance_captured");
        var transferred = Temporary("transferred");
        var cleanupBeforeTransfer = new List<string>();
        var cleanupAlways = new List<string>();
        var builder = new StringBuilder();
        builder.Append("await (async () => { ");
        if (receiver != null)
            builder.Append("let ").Append(receiver).Append("; ");
        if (receiverCaptured != null)
            builder.Append("let ").Append(receiverCaptured).Append(" = false; ");
        if (implicitValue != null)
            builder.Append("let ").Append(implicitValue).Append("; ");
        foreach (var argument in call.SourceArguments)
            builder.Append("let ").Append(captures[argument]).Append("; ");
        if (constructorResult != null)
            builder.Append("let ").Append(constructorResult).Append("; ");
        if (constructorCaptured != null)
            builder.Append("let ").Append(constructorCaptured).Append(" = false; ");
        if (implicitCaptured != null)
            builder.Append("let ").Append(implicitCaptured).Append(" = false; ");
        foreach (var flag in capturedFlags.Values)
            builder.Append("let ").Append(flag).Append(" = false; ");
        foreach (var flag in byRefFlags.Values)
            builder.Append("let ").Append(flag).Append(" = false; ");
        builder.Append("let ").Append(transferred).Append(" = false; try { ");

        if (call.EvaluateReceiverAfterImplicitValue)
        {
            CaptureImplicitValue();
            CaptureReceiver();
        }
        else
        {
            CaptureReceiver();
            CaptureImplicitValue();
        }
        foreach (var argument in call.SourceArguments)
        {
            builder.Append(captures[argument]).Append(" = ").Append(CapturedArgumentValue(argument)).Append("; ");
            if (capturedFlags.TryGetValue(argument, out var flag))
            {
                builder.Append(flag).Append(" = true; ");
                cleanupBeforeTransfer.Add(
                    $"if ({flag}) {{ {CapturedCleanup(argument.Parameter.Type, captures[argument])} }} ");
            }
            if (byRefFlags.TryGetValue(argument, out var referenceFlag))
            {
                builder.Append(referenceFlag).Append(" = true; ");
                cleanupAlways.Add($"if ({referenceFlag}) {{ {captures[argument]}.release(); }} ");
            }
        }
        if (constructorType != null && constructorResult != null && constructorCaptured != null)
        {
            builder.Append(constructorResult).Append(" = ")
                .Append(_classNames[constructorType]).Append("_create(); ")
                .Append(constructorCaptured).Append(" = true; ");
        }
        var abiArguments = new List<string>();
        if (constructorResult != null)
            abiArguments.Add(constructorResult);
        else if (receiver != null)
            abiArguments.Add(receiver);
        if (implicitValue != null)
            abiArguments.Add(implicitValue);
        abiArguments.AddRange(call.ParameterArguments.Select(argument =>
            argument.IsDefault ? OptionalDefaultValue(argument.Parameter) : captures[argument]));
        builder.Append(transferred).Append(" = true; ");
        if (constructorResult != null && constructorCaptured != null)
        {
            builder.Append("await ").Append(_routineNames[call.Routine])
                .Append('(').Append(string.Join(", ", abiArguments)).Append("); ")
                .Append(constructorCaptured).Append(" = false; return ").Append(constructorResult).Append("; ");
        }
        else
        {
            builder.Append("return await ").Append(_routineNames[call.Routine])
                .Append('(').Append(string.Join(", ", abiArguments)).Append("); ");
        }
        builder.Append("} catch (error) { if (!").Append(transferred).Append(") { ");
        foreach (var action in cleanupBeforeTransfer.AsEnumerable().Reverse())
            builder.Append(action);
        builder.Append("} ");
        if (constructorResult != null && constructorCaptured != null)
            builder.Append("if (").Append(constructorCaptured).Append(") smile.classRelease(")
                .Append(constructorResult).Append("); ");
        builder.Append("throw error; } finally { ");
        foreach (var action in cleanupAlways.AsEnumerable().Reverse())
            builder.Append(action);
        if (receiverCaptured != null && receiver != null)
        {
            builder.Append("if (").Append(receiverCaptured).Append(") { ")
                .Append(call.InstanceReceiver!.ContainingType.IsClass
                    ? $"smile.classRelease({receiver});"
                    : $"{receiver}.release();")
                .Append(" } ");
        }
        builder.Append("} })()");
        return builder.ToString();

        void CaptureReceiver()
        {
            if (receiver != null)
            {
                builder.Append(receiver).Append(" = ").Append(ReceiverCapture(call)).Append("; ");
                if (receiverCaptured != null)
                    builder.Append(receiverCaptured).Append(" = true; ");
            }
        }

        void CaptureImplicitValue()
        {
            if (implicitValue == null || call.ImplicitValue == null || implicitType == null)
                return;
            builder.Append(implicitValue).Append(" = ")
                .Append(CapturedImplicitValue(call.ImplicitValue, implicitType)).Append("; ");
            if (implicitCaptured != null)
            {
                builder.Append(implicitCaptured).Append(" = true; ");
                cleanupBeforeTransfer.Add(
                    $"if ({implicitCaptured}) {{ {CapturedCleanup(implicitType, implicitValue)} }} ");
            }
        }
    }

    private string CapturedArgumentValue(BoundCallArgument argument)
    {
        var expression = argument.Expression!;
        if (argument.Parameter.ParameterMode == ParameterPassingMode.ByRef)
            return Reference(expression);
        var value = Expression(expression);
        if (argument.Parameter.Type is RecordTypeSymbol)
            return RecordValue(expression, argument.Parameter.Type, value);
        return !argument.Parameter.HasDeclaredType && argument.Parameter.Type == SmileType.Number &&
               _analysis.SemanticModel.GetType(expression) == SmileType.Boolean
            ? $"(smile.isTrue({value}) ? 1 : 0)"
            : value;
    }

    private static bool RequiresCapturedCleanup(BoundCallArgument argument) =>
        argument.Parameter.ParameterMode == ParameterPassingMode.ByVal &&
        RequiresCapturedCleanup(argument.Parameter.Type);

    private static bool RequiresCapturedCleanup(SmileType type) =>
        type == SmileType.Image || type.IsClass || type is RecordTypeSymbol { RequiresCleanup: true };

    private string CapturedCleanup(SmileType type, string value) =>
        type == SmileType.Image
            ? $"smile.imageRelease({value}); {value} = null;"
            : type.IsClass
                ? $"smile.classRelease({value}); {value} = null;"
                : $"{_recordNames[(RecordTypeSymbol)type]}_clear({value});";

    private string CapturedImplicitValue(ExpressionSyntax expression, SmileType type)
    {
        var value = Expression(expression);
        if (type is RecordTypeSymbol)
            return RecordValue(expression, type, value);
        return type == SmileType.Number ? $"smile.safe({value})" : value;
    }

    private string ReceiverCapture(BoundCall call)
    {
        var receiver = call.InstanceReceiver!;
        if (receiver.Kind == BoundInstanceReceiverKind.WithTarget)
        {
            if (receiver.WithTarget == null ||
                !_withReferences.TryGetValue(receiver.WithTarget, out var withReference))
                throw new InvalidOperationException("Bound With receiver does not have a captured Web reference.");
            return receiver.ContainingType.IsClass
                ? $"smile.classRequire(smile.classRetain({withReference}.get()))"
                : withReference;
        }
        if (receiver.Expression == null)
            return "smile.invalidRef()";
        return receiver.ContainingType.IsClass
            ? $"smile.classRequire({Expression(receiver.Expression)})"
            : Reference(receiver.Expression);
    }

    private string OptionalDefaultValue(ParameterSymbol parameter)
    {
        if (parameter.Type == SmileType.Number && parameter.DefaultValue is long number &&
            number is > MaxSafeInteger or < -MaxSafeInteger)
        {
            throw new WebTargetException(parameter.Source, "SML5102",
                parameter.Declaration.DefaultValue?.Span ?? parameter.DeclarationSpan,
                "Web target Optional Number defaults must be within JavaScript's safe integer range.");
        }
        return ConstantValue(parameter.DefaultValue, parameter.Type);
    }

    private string Reference(ExpressionSyntax expression)
    {
        if (_analysis.SemanticModel.TryGetClassLocationOwner(expression, out var classOwner))
            return ClassOwnedReference(expression, classOwner);
        if (expression is ParenthesizedExpressionSyntax parenthesized)
            return Reference(parenthesized.Expression);
        if (expression is MeExpressionSyntax)
        {
            if (_currentRoutine?.Receiver == null)
                return "smile.invalidRef()";
            return BorrowedReference(_variableNames[_currentRoutine.Receiver]);
        }
        if (expression is NameExpressionSyntax name)
        {
            var symbol = ResolveVariable(name.Identifier);
            if (symbol.ParameterMode == ParameterPassingMode.ByRef)
                return BorrowedReference(_variableNames[symbol]);
            return $"smile.ref(() => {_variableNames[symbol]}, value => {{ {_variableNames[symbol]} = value; }})";
        }
        if (expression is ArrayAccessExpressionSyntax array)
        {
            var symbol = ResolveVariable(array.Identifier);
            return $"smile.refArray({_variableNames[symbol]}, [{Arguments(array.Indices)}])";
        }
        if (expression is FieldAccessExpressionSyntax field)
        {
            if (!_analysis.SemanticModel.TryGetInstanceField(field, out var fieldSymbol))
                return "smile.invalidRef()";
            var key = Json(FieldKey(fieldSymbol));
            return MemberReference(Reference(field.Receiver), key);
        }
        if (expression is IndexedExpressionSyntax indexed)
            return IndexedReference(Reference(indexed.Receiver), indexed.Indices);
        if (expression is LeadingMemberAccessExpressionSyntax leading)
        {
            if (!_analysis.SemanticModel.TryGetWithMember(leading, out var binding) ||
                binding.InstanceField == null ||
                !_withReferences.TryGetValue(binding.ReceiverStatement, out var receiver))
                return "smile.invalidRef()";
            return MemberReference(BorrowedReference(receiver), Json(FieldKey(binding.InstanceField)));
        }
        return "smile.invalidRef()";
    }

    private string ClassOwnedReference(ExpressionSyntax expression, BoundClassLocationOwner owner)
    {
        var root = Temporary("class_owner");
        var indexExpressions = new List<ExpressionSyntax>();
        CollectIndices(expression, owner.RootExpression, indexExpressions);
        var indexNames = indexExpressions.ToDictionary(index => index, _ => Temporary("index"));
        var builder = new StringBuilder();
        builder.Append("await (async () => { const ").Append(root).Append(" = smile.classRequire(")
            .Append(Expression(owner.RootExpression)).Append("); try { ");
        foreach (var index in indexExpressions)
            builder.Append("const ").Append(indexNames[index]).Append(" = smile.safe(")
                .Append(Expression(index)).Append("); ");
        var access = ClassLocationAccess(expression, owner.RootExpression, root, indexNames);
        var setter = ClassLocationSetter(expression, owner.RootExpression, root, indexNames);
        builder.Append("return smile.classOwnedRef(").Append(root).Append(", root => ")
            .Append(access).Append(", (root, value) => { ").Append(setter).Append(" }); ")
            .Append("} catch (error) { smile.classRelease(").Append(root).Append("); throw error; } })()");
        return builder.ToString();

        void CollectIndices(ExpressionSyntax current, ExpressionSyntax rootExpression,
            ICollection<ExpressionSyntax> indices)
        {
            if (ReferenceEquals(current, rootExpression))
                return;
            switch (current)
            {
                case ParenthesizedExpressionSyntax parenthesized:
                    CollectIndices(parenthesized.Expression, rootExpression, indices);
                    break;
                case FieldAccessExpressionSyntax field:
                    CollectIndices(field.Receiver, rootExpression, indices);
                    break;
                case IndexedExpressionSyntax indexed:
                    CollectIndices(indexed.Receiver, rootExpression, indices);
                    foreach (var index in indexed.Indices)
                        indices.Add(index);
                    break;
            }
        }
    }

    private string ClassLocationAccess(ExpressionSyntax expression, ExpressionSyntax rootExpression,
        string rootName, IReadOnlyDictionary<ExpressionSyntax, string> indexNames)
    {
        if (ReferenceEquals(expression, rootExpression))
            return rootName;
        return expression switch
        {
            ParenthesizedExpressionSyntax parenthesized =>
                ClassLocationAccess(parenthesized.Expression, rootExpression, rootName, indexNames),
            FieldAccessExpressionSyntax field =>
                $"({ClassLocationAccess(field.Receiver, rootExpression, rootName, indexNames)})" +
                $"[{Json(FieldKey(RequireInstanceField(field)))}]",
            IndexedExpressionSyntax indexed =>
                $"smile.get({ClassLocationAccess(indexed.Receiver, rootExpression, rootName, indexNames)}, " +
                $"[{string.Join(", ", indexed.Indices.Select(index => indexNames[index]))}])",
            _ => throw new InvalidOperationException("Unsupported Class-rooted Web location.")
        };
    }

    private string ClassLocationSetter(ExpressionSyntax expression, ExpressionSyntax rootExpression,
        string rootName, IReadOnlyDictionary<ExpressionSyntax, string> indexNames)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
            expression = parenthesized.Expression;
        if (expression is IndexedExpressionSyntax indexed)
        {
            var receiver = ClassLocationAccess(indexed.Receiver, rootExpression, rootName, indexNames);
            var indices = string.Join(", ", indexed.Indices.Select(index => indexNames[index]));
            return $"smile.set({receiver}, [{indices}], value);";
        }
        return ClassLocationAccess(expression, rootExpression, rootName, indexNames) + " = value;";
    }

    private IInstanceFieldSymbol RequireInstanceField(ExpressionSyntax expression) =>
        _analysis.SemanticModel.TryGetInstanceField(expression, out var field)
            ? field
            : throw new InvalidOperationException("Web location does not have a bound instance field.");

    private string IndexedReference(string receiverReference, IReadOnlyList<ExpressionSyntax> indices)
    {
        var target = Temporary("indexed_target");
        var indexNames = indices.Select(_ => Temporary("index")).ToArray();
        var builder = new StringBuilder();
        builder.Append("await (async () => { const ").Append(target).Append(" = ")
            .Append(receiverReference).Append("; try { ");
        for (var index = 0; index < indices.Count; index++)
            builder.Append("const ").Append(indexNames[index]).Append(" = smile.safe(")
                .Append(Expression(indices[index])).Append("); ");
        var values = string.Join(", ", indexNames);
        builder.Append("return { get: () => smile.get(").Append(target).Append(".get(), [")
            .Append(values).Append("]), set: value => smile.set(").Append(target)
            .Append(".get(), [").Append(values).Append("], value), release: () => ")
            .Append(target).Append(".release() }; } catch (error) { ").Append(target)
            .Append(".release(); throw error; } })()");
        return builder.ToString();
    }

    private static string BorrowedReference(string reference) =>
        $"smile.ref(() => {reference}.get(), value => {reference}.set(value))";

    private static string MemberReference(string receiverReference, string key) =>
        $"(() => {{ const target = {receiverReference}; return {{ get: () => target.get()[{key}], " +
        $"set: value => {{ target.get()[{key}] = value; }}, release: () => target.release() }}; }})()";

    private string DefaultValue(SmileType type) => type is RecordTypeSymbol record
        ? $"{_recordNames[record]}_default()"
        : type.IsEnum
        ? "0n"
        : type == SmileType.Image || type.IsClass
        ? "null"
        : type == SmileType.Text
        ? "\"\""
        : type == SmileType.Boolean ? "false" : "0";

    private string CloneValue(SmileType type, string value) => type is RecordTypeSymbol record
        ? $"{_recordNames[record]}_clone({value})"
        : type == SmileType.Image
        ? $"smile.imageRetain({value})"
        : type.IsClass
        ? $"smile.classRetain({value})"
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
        _ when _analysis.SemanticModel.GetType(expression) is RecordTypeSymbol &&
               _analysis.SemanticModel.TryGetClassLocationOwner(expression, out _) => true,
        _ when _analysis.SemanticModel.TryGetBoundCall(expression, out var call) &&
               call.Routine.ReturnType is RecordTypeSymbol => true,
        FieldAccessExpressionSyntax field when _analysis.SemanticModel.GetType(field) is RecordTypeSymbol =>
            IsOwnedRecordExpression(field.Receiver),
        _ => false
    };

    private string StoreValue(SmileType type, string value) => type == SmileType.Number
        ? $"smile.safe({value})"
        : CloneValue(type, value);

    private string ReadTarget(AssignmentTargetSyntax target) => TargetLocation(target);

    private string WriteTarget(AssignmentTargetSyntax target, string value)
    {
        if (target.Location is NameExpressionSyntax name)
            return WriteVariable(ResolveVariable(name.Identifier), value);
        if (target.Location is ArrayAccessExpressionSyntax array)
            return $"smile.set({_variableNames[ResolveVariable(array.Identifier)]}, [{Arguments(array.Indices)}], {value})";
        return $"{Location(target.Location)} = {value}";
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
        else if (symbol.Type.IsClass)
        {
            Line($"smile.classRelease({value});");
            if (symbol.ParameterMode != ParameterPassingMode.ByRef)
                Line($"{name} = null;");
        }
        else if (symbol.Type is RecordTypeSymbol record)
            Line($"{_recordNames[record]}_clear({value});");
    }

    private SmileType TargetType(AssignmentTargetSyntax target)
    {
        return _analysis.SemanticModel.GetType(target.Location);
    }

    private string TargetLocation(AssignmentTargetSyntax target) => Location(target.Location);

    private string Location(ExpressionSyntax expression)
    {
        if (expression is NameExpressionSyntax name)
            return ReadVariable(ResolveVariable(name.Identifier));
        if (expression is ArrayAccessExpressionSyntax array)
            return $"smile.get({_variableNames[ResolveVariable(array.Identifier)]}, [{Arguments(array.Indices)}])";
        if (expression is FieldAccessExpressionSyntax field &&
            _analysis.SemanticModel.TryGetInstanceField(field, out var fieldSymbol))
            return $"({Expression(field.Receiver)})[{Json(FieldKey(fieldSymbol))}]";
        if (expression is IndexedExpressionSyntax indexed)
            return $"smile.get({Location(indexed.Receiver)}, [{Arguments(indexed.Indices)}])";
        if (expression is ParenthesizedExpressionSyntax parenthesized)
            return Location(parenthesized.Expression);
        if (expression is LeadingMemberAccessExpressionSyntax leading)
            return LeadingMemberLocation(leading);
        throw UnsupportedExpression(expression, "non-location assignment target");
    }

    private string LeadingMemberLocation(LeadingMemberAccessExpressionSyntax expression)
    {
        if (!_analysis.SemanticModel.TryGetWithMember(expression, out var binding) ||
            binding.InstanceField == null ||
            !_withReferences.TryGetValue(binding.ReceiverStatement, out var receiver))
            throw UnsupportedExpression(expression, "unbound With member");
        return $"{receiver}.get()[{Json(FieldKey(binding.InstanceField))}]";
    }

    private static string ConstantValue(object value, SmileType type) => value switch
    {
        string text => Json(text),
        bool boolean => boolean ? "true" : "false",
        long number when type.IsEnum => EnumValue(number),
        long number => number.ToString(CultureInfo.InvariantCulture),
        _ => "0"
    };

    private static string EnumValue(long value) => value.ToString(CultureInfo.InvariantCulture) + "n";

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
        _analysis.SemanticModel.AllRoutines.OrderBy(item => item.SourceOrdinal)
            .ThenBy(item => item.DeclarationSyntax.Span.Start).ThenBy(item => item.SymbolKind);

    private IOrderedEnumerable<RecordTypeSymbol> OrderedRecordTypes() =>
        _analysis.SemanticModel.Types.Values.OrderBy(item => item.SourceOrdinal).ThenBy(item => item.DeclarationSpan.Start);

    private IOrderedEnumerable<ClassTypeSymbol> OrderedClassTypes() =>
        _analysis.SemanticModel.Classes.Values.OrderBy(item => item.SourceOrdinal)
            .ThenBy(item => item.DeclarationSpan.Start);

    private string FieldKey(IInstanceFieldSymbol field) => field switch
    {
        RecordFieldSymbol record when _fieldNames.TryGetValue(record, out var key) => key,
        ClassFieldSymbol classField when _classFieldNames.TryGetValue(classField, out var key) => key,
        _ => throw new InvalidOperationException(
            $"Web instance field '{field.Name}' does not have a bound runtime key.")
    };

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
