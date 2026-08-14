using System.Globalization;
using System.Text;
using Smile.Language;

namespace Smile.Compiler;

internal sealed class MasmDebugSite
{
    public MasmDebugSite(int id, SourceText source, StatementSyntax statement)
    {
        Id = id;
        Source = source;
        Statement = statement;
        source.GetLineColumn(statement.Span.Start, out var line, out _);
        Line = line;
    }

    public int Id { get; }
    public SourceText Source { get; }
    public StatementSyntax Statement { get; }
    public int Line { get; }
    public string HelperName => $"smile_debug_site_{Id}";
}

internal enum MasmStorageKind
{
    Static,
    Frame
}

internal sealed class MasmTemporaryStorage
{
    public MasmTemporaryStorage(string name, SmileType type, RoutineDeclarationSyntax? owner)
    {
        Name = name;
        Type = type;
        Owner = owner;
        Kind = owner == null ? MasmStorageKind.Static : MasmStorageKind.Frame;
    }

    public string Name { get; }
    public SmileType Type { get; }
    public RoutineDeclarationSyntax? Owner { get; }
    public MasmStorageKind Kind { get; }
    public int FrameOffset { get; set; }
    public int Size => Math.Max(8, Type.Size);
    public bool RequiresCleanup => Type.RequiresCleanup;
}

internal sealed class MasmFrameLayout
{
    public MasmFrameLayout(IReadOnlyDictionary<VariableSymbol, int> localOffsets,
        IReadOnlyList<MasmTemporaryStorage> temporaries, int returnOffset, int frameSize)
    {
        LocalOffsets = localOffsets;
        Temporaries = temporaries;
        ReturnOffset = returnOffset;
        FrameSize = frameSize;
    }

    public IReadOnlyDictionary<VariableSymbol, int> LocalOffsets { get; }
    public IReadOnlyList<MasmTemporaryStorage> Temporaries { get; }
    public int ReturnOffset { get; }
    public int FrameSize { get; }
}

internal sealed record MasmCleanupAction(MasmTemporaryStorage Storage);
internal sealed record MasmLoopContext(string ExitLabel, int CleanupDepth, int ClipDepth);

internal sealed class MasmEmitter
{
    private readonly SmileAnalysisResult _analysis;
    private readonly SmileGraphicsBackend _graphicsBackend;
    private readonly bool _vSync;
    private readonly bool _emitDebugInformation;
    private readonly byte[] _appIdentityBytes;
    private readonly byte[] _assetManifestBytes;
    private readonly StringBuilder _builder = new();
    private readonly Dictionary<VariableSymbol, string> _symbolLabels = new();
    private readonly Dictionary<RoutineSymbol, string> _routineLabels = new();
    private readonly Dictionary<RecordTypeSymbol, string> _recordHelperLabels = new();
    private readonly Dictionary<CallExpressionSyntax, MasmTemporaryStorage> _recordCallResults = new();
    private readonly Dictionary<LiteralExpressionSyntax, TextLiteral> _textLiterals = new();
    private readonly Dictionary<VariableSymbol, TextLiteral> _constantTextLiterals = new();
    private readonly Dictionary<SyntaxToken, TextLiteral> _gameTextLiterals = new();
    private readonly Dictionary<ForStatementSyntax, MasmTemporaryStorage> _forLimits = new();
    private readonly Dictionary<SelectStatementSyntax, MasmTemporaryStorage> _selectValues = new();
    private readonly List<MasmTemporaryStorage> _temporaries = new();
    private readonly Dictionary<RoutineSymbol, MasmFrameLayout> _frameLayouts = new();
    private readonly Stack<MasmLoopContext> _forExitLabels = new();
    private readonly Stack<MasmLoopContext> _doExitLabels = new();
    private readonly List<MasmCleanupAction> _activeCleanups = new();
    private readonly List<MasmDebugSite> _debugSites = new();
    private readonly Dictionary<StatementSyntax, MasmDebugSite> _debugSitesByStatement = new();
    private SourceText _currentSource = null!;
    private RoutineSymbol? _currentRoutine;
    private MasmFrameLayout? _currentFrame;
    private string? _returnLabel;
    private int _labelId;
    private bool _usesTimer;
    private bool _usesGameClosed;
    private bool _usesKeyHeld;
    private bool _usesMusic;
    private int _dynamicStackSlots;
    private int _clipDepth;
    private RoutineDeclarationSyntax? _collectRoutine;

    public MasmEmitter(SmileAnalysisResult analysis, SmileGraphicsBackend graphicsBackend,
        bool vSync, bool emitDebugInformation, string? appIdentity = null,
        IReadOnlyList<string>? assetPaths = null)
    {
        _analysis = analysis;
        _graphicsBackend = graphicsBackend;
        _vSync = vSync;
        _emitDebugInformation = emitDebugInformation;
        _appIdentityBytes = Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(appIdentity) ? "Program" : appIdentity);
        _assetManifestBytes = Encoding.UTF8.GetBytes(string.Join("\n", assetPaths ?? Array.Empty<string>()));
    }

    public bool UsesMusic => _usesMusic;
    public IReadOnlyList<MasmDebugSite> DebugSites => _debugSites;
    public IReadOnlyDictionary<RoutineSymbol, MasmFrameLayout> FrameLayouts => _frameLayouts;

    public string Emit()
    {
        foreach (var tree in _analysis.BoundSyntaxTrees)
        {
            _currentSource = tree.Source;
            Collect(tree.Root.Statements);
        }
        AssignLabels();
        BuildFrameLayouts();

        Line("option casemap:none");
        Line("EXTERN ExitProcess:PROC");
        Line("EXTERN smile_print_text:PROC");
        Line("EXTERN smile_print_number:PROC");
        Line("EXTERN smile_print_boolean:PROC");
        Line("EXTERN smile_print_newline:PROC");
        Line("EXTERN smile_text_retain:PROC");
        Line("EXTERN smile_text_release:PROC");
        Line("EXTERN smile_text_move_assign:PROC");
        Line("EXTERN smile_text_concat:PROC");
        Line("EXTERN smile_text_equal:PROC");
        Line("EXTERN smile_text_equal_case:PROC");
        Line("EXTERN smile_text_clear:PROC");
        Line("EXTERN smile_text_lifetime_report:PROC");
        Line("EXTERN smile_image_retain:PROC");
        Line("EXTERN smile_image_release:PROC");
        Line("EXTERN smile_image_move_assign:PROC");
        Line("EXTERN smile_image_clear:PROC");
        Line("EXTERN smile_load_image_value:PROC");
        Line("EXTERN smile_draw_image_value:PROC");
        Line("EXTERN smile_image_width_value:PROC");
        Line("EXTERN smile_image_height_value:PROC");
        Line("EXTERN smile_image_loaded_value:PROC");
        Line("EXTERN smile_print_text_value:PROC");
        Line("EXTERN smile_draw_text_value:PROC");
        Line("EXTERN smile_get_key:PROC");
        Line("EXTERN smile_clear_screen:PROC");
        Line("EXTERN smile_wait:PROC");
        Line("EXTERN smile_random:PROC");
        if (_usesTimer) Line("EXTERN smile_timer:PROC");
        if (_usesGameClosed) Line("EXTERN smile_game_closed:PROC");
        if (_usesKeyHeld) Line("EXTERN smile_key_held:PROC");
        Line("EXTERN smile_game_open:PROC");
        Line("EXTERN smile_graphics_configure:PROC");
        Line("EXTERN smile_game_clear:PROC");
        Line("EXTERN smile_fill_rectangle:PROC");
        Line("EXTERN smile_draw_rectangle:PROC");
        Line("EXTERN smile_fill_rounded_rectangle:PROC");
        Line("EXTERN smile_draw_rounded_rectangle:PROC");
        Line("EXTERN smile_fill_circle:PROC");
        Line("EXTERN smile_draw_circle:PROC");
        Line("EXTERN smile_draw_arc:PROC");
        Line("EXTERN smile_fill_quadrilateral:PROC");
        Line("EXTERN smile_draw_quadrilateral:PROC");
        Line("EXTERN smile_draw_line:PROC");
        Line("EXTERN smile_draw_text:PROC");
        Line("EXTERN smile_draw_number:PROC");
        Line("EXTERN smile_clip_push:PROC");
        Line("EXTERN smile_clip_pop:PROC");
        Line("EXTERN smile_text_width_value:PROC");
        Line("EXTERN smile_text_height_value:PROC");
        Line("EXTERN smile_show_screen:PROC");
        Line("EXTERN smile_play_sound:PROC");
        Line("EXTERN smile_stop_sound:PROC");
        Line("EXTERN smile_play_sound_channel:PROC");
        Line("EXTERN smile_stop_sound_channel:PROC");
        if (_usesMusic)
        {
            Line("EXTERN smile_music_play:PROC");
            Line("EXTERN smile_music_pause:PROC");
            Line("EXTERN smile_music_resume:PROC");
            Line("EXTERN smile_music_stop:PROC");
            Line("EXTERN smile_music_set_volume:PROC");
            Line("EXTERN smile_music_shutdown:PROC");
        }
        Line("EXTERN smile_load_value:PROC");
        Line("EXTERN smile_load_text_file:PROC");
        Line("EXTERN smile_save_value:PROC");
        Line("EXTERN smile_load_data_value:PROC");
        Line("EXTERN smile_save_data_value:PROC");
        Line("EXTERN smile_media_shutdown:PROC");
        Line("EXTERN smile_media_configure:PROC");
        foreach (var site in _debugSites)
            Line($"EXTERN {site.HelperName}:PROC");
        Line();
        Line(".data");
        EmitStorage(_analysis.SemanticModel.Symbols.Values);
        foreach (var temporary in _temporaries.Where(temporary => temporary.Kind == MasmStorageKind.Static))
            Line(temporary.Size == 8 ? $"{temporary.Name} QWORD 0" :
                $"{temporary.Name} QWORD {(temporary.Size / 8).ToString(CultureInfo.InvariantCulture)} DUP(0)");
        foreach (var literal in _textLiterals.Values.Concat(_constantTextLiterals.Values))
        {
            Line("ALIGN 8");
            Line($"{literal.Label} QWORD -1, {literal.Bytes.Length.ToString(CultureInfo.InvariantCulture)}");
            EmitBytes(literal.Bytes, terminate: true);
        }
        foreach (var literal in _gameTextLiterals.Values)
        {
            Line($"{literal.Label} LABEL BYTE");
            EmitBytes(literal.Bytes, terminate: false);
        }
        Line("smile_app_identity LABEL BYTE");
        EmitBytes(_appIdentityBytes, terminate: false);
        Line("smile_asset_manifest LABEL BYTE");
        EmitBytes(_assetManifestBytes, terminate: false);

        Line();
        Line(".code");
        Line("main PROC");
        Line("    push rbp");
        Line("    mov rbp, rsp");
        Line("    sub rsp, 256");
        Line("    lea rcx, smile_app_identity");
        Line($"    mov rdx, {_appIdentityBytes.Length.ToString(CultureInfo.InvariantCulture)}");
        Line("    lea r8, smile_asset_manifest");
        Line($"    mov r9, {_assetManifestBytes.Length.ToString(CultureInfo.InvariantCulture)}");
        CallAligned("smile_media_configure");
        Line($"    mov rcx, {(int)_graphicsBackend}");
        Line($"    mov rdx, {(_vSync ? 1 : 0)}");
        CallAligned("smile_graphics_configure");
        _currentSource = _analysis.BoundSyntaxTree.Source;
        EmitStatements(_analysis.BoundSyntaxTree.Root.Statements);
        EmitProgramCleanup();
        CallAligned("smile_media_shutdown");
        CallAligned("smile_text_lifetime_report");
        if (_usesMusic) CallAligned("smile_music_shutdown");
        Line("    xor ecx, ecx");
        CallAligned("ExitProcess");
        Line("main ENDP");

        foreach (var routine in _analysis.SemanticModel.Routines.Values)
            EmitRoutine(routine);

        foreach (var record in OrderedRecordTypes())
            EmitRecordHelpers(record);

        Line("END");
        return _builder.ToString();
    }

    private void EmitStorage(IEnumerable<VariableSymbol> symbols)
    {
        foreach (var symbol in symbols)
        {
            if (symbol.IsConstant)
                continue;
            var label = _symbolLabels[symbol];
            var qwords = Math.Max(1, symbol.ArraySize) * Math.Max(1, symbol.Type.Size / 8);
            Line(qwords == 1 ? $"{label} QWORD 0" :
                $"{label} QWORD {qwords.ToString(CultureInfo.InvariantCulture)} DUP(0)");
        }
    }

    private void Collect(IReadOnlyList<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            if (_emitDebugInformation && IsExecutable(statement))
            {
                var site = new MasmDebugSite(_debugSites.Count, _currentSource, statement);
                _debugSites.Add(site);
                _debugSitesByStatement[statement] = site;
            }

            switch (statement)
            {
                case ConstStatementSyntax constant:
                    CollectExpression(constant.Expression);
                    break;
                case AssignmentStatementSyntax assignment:
                    foreach (var index in assignment.Target.Indices)
                        CollectExpression(index);
                    CollectExpression(assignment.Expression);
                    break;
                case DimStatementSyntax dim:
                    foreach (var size in dim.Sizes)
                        CollectExpression(size);
                    break;
                case PrintStatementSyntax print:
                    foreach (var item in print.Items)
                        CollectExpression(item);
                    break;
                case WaitStatementSyntax wait:
                    CollectExpression(wait.Duration);
                    break;
                case RandomStatementSyntax random:
                    CollectExpression(random.Minimum);
                    CollectExpression(random.Maximum);
                    break;
                case IfStatementSyntax ifStatement:
                    foreach (var clause in ifStatement.Clauses)
                    {
                        CollectExpression(clause.Condition);
                        Collect(clause.Statements);
                    }
                    Collect(ifStatement.ElseStatements);
                    break;
                case ForStatementSyntax forStatement:
                    CollectExpression(forStatement.LowerBound);
                    CollectExpression(forStatement.UpperBound);
                    _forLimits[forStatement] = CreateTemporary("for_limit", SmileType.Number);
                    Collect(forStatement.Statements);
                    break;
                case DoStatementSyntax doStatement:
                    Collect(doStatement.Statements);
                    CollectExpression(doStatement.UntilCondition);
                    break;
                case RoutineDeclarationSyntax routine:
                    var previousRoutine = _collectRoutine;
                    _collectRoutine = routine;
                    Collect(routine.Statements);
                    _collectRoutine = previousRoutine;
                    break;
                case CallStatementSyntax call:
                    foreach (var argument in call.Arguments)
                        CollectExpression(argument);
                    break;
                case ReturnStatementSyntax returnStatement:
                    CollectExpression(returnStatement.Expression);
                    break;
                case SelectStatementSyntax select:
                    CollectExpression(select.Expression);
                    _selectValues[select] = CreateTemporary("select_value",
                        _analysis.SemanticModel.GetType(select.Expression));
                    foreach (var clause in select.Cases)
                    {
                        CollectExpression(clause.Value);
                        Collect(clause.Statements);
                    }
                    break;
                case GameWindowStatementSyntax gameWindow:
                    CollectTextToken(gameWindow.Title);
                    CollectExpression(gameWindow.Width);
                    CollectExpression(gameWindow.Height);
                    break;
                case ClearColorStatementSyntax clearColor:
                    CollectExpression(clearColor.Color);
                    break;
                case GraphicsStatementSyntax graphics:
                    CollectExpression(graphics.TextExpression);
                    foreach (var argument in graphics.Arguments)
                        CollectExpression(argument);
                    break;
                case DrawImageStatementSyntax image:
                    CollectExpression(image.Image);
                    foreach (var argument in ImageExpressions(image)) CollectExpression(argument);
                    break;
                case ImageLoadStatementSyntax image:
                    foreach (var index in image.Target.Indices) CollectExpression(index);
                    CollectExpression(image.Path);
                    break;
                case ClipRectangleStatementSyntax clip:
                    foreach (var argument in clip.Arguments) CollectExpression(argument);
                    Collect(clip.Statements);
                    break;
                case SoundStatementSyntax sound:
                    if (sound.Path != null) CollectTextToken(sound.Path);
                    CollectExpression(sound.Channel);
                    break;
                case MusicStatementSyntax music:
                    _usesMusic = true;
                    if (music.Path != null)
                        CollectTextToken(music.Path);
                    CollectExpression(music.Volume);
                    break;
                case LoadStatementSyntax load:
                    CollectTextToken(load.Key);
                    CollectExpression(load.DefaultValue);
                    break;
                case TextFileLoadStatementSyntax textFileLoad:
                    CollectTextToken(textFileLoad.Path);
                    break;
                case DataLoadStatementSyntax data:
                    CollectExpression(data.Key);
                    foreach (var index in data.CountTarget.Indices) CollectExpression(index);
                    break;
                case DataSaveStatementSyntax data:
                    CollectExpression(data.Count);
                    CollectExpression(data.Key);
                    break;
                case SaveStatementSyntax save:
                    CollectTextToken(save.Key);
                    break;
            }
        }
    }

    private MasmTemporaryStorage CreateTemporary(string prefix, SmileType type)
    {
        var storage = new MasmTemporaryStorage($"{prefix}_{_temporaries.Count}", type, _collectRoutine);
        _temporaries.Add(storage);
        return storage;
    }

    private void CollectTextToken(SyntaxToken token)
    {
        if (!_gameTextLiterals.ContainsKey(token))
            _gameTextLiterals[token] = new TextLiteral($"game_text_{_gameTextLiterals.Count}", Encoding.UTF8.GetBytes(token.Value as string ?? string.Empty));
    }

    private void CollectExpression(ExpressionSyntax? expression)
    {
        switch (expression)
        {
            case LiteralExpressionSyntax literal when literal.Value is string text:
                if (!_textLiterals.ContainsKey(literal))
                    _textLiterals[literal] = new TextLiteral($"text_{_textLiterals.Count}", Encoding.UTF8.GetBytes(text));
                break;
            case ArrayAccessExpressionSyntax array:
                foreach (var index in array.Indices)
                    CollectExpression(index);
                break;
            case FieldAccessExpressionSyntax field:
                CollectExpression(field.Receiver);
                break;
            case ParenthesizedExpressionSyntax parenthesized:
                CollectExpression(parenthesized.Expression);
                break;
            case UnaryExpressionSyntax unary:
                CollectExpression(unary.Operand);
                break;
            case BinaryExpressionSyntax binary:
                CollectExpression(binary.Left);
                CollectExpression(binary.Right);
                break;
            case CallExpressionSyntax call:
                _usesTimer |= call.Identifier.Kind == SyntaxKind.TimerKeyword;
                _usesGameClosed |= call.Identifier.Kind == SyntaxKind.GameClosedKeyword;
                _usesKeyHeld |= call.Identifier.Kind == SyntaxKind.KeyHeldKeyword;
                foreach (var argument in call.Arguments)
                    CollectExpression(argument);
                if (_analysis.SemanticModel.TryGetRoutine(call.Identifier.Text, out var routine) &&
                    routine.ReturnType is RecordTypeSymbol)
                    _recordCallResults[call] = CreateTemporary("record_result", routine.ReturnType);
                break;
        }
    }

    private void AssignLabels()
    {
        var id = 0;
        foreach (var symbol in _analysis.SemanticModel.Symbols.Values)
        {
            if (!symbol.IsConstant)
                _symbolLabels[symbol] = (symbol.IsArray ? "array_" : "variable_") + id++;
            else if (symbol.Type == SmileType.Text && symbol.ConstantValue is string text)
                _constantTextLiterals[symbol] = new TextLiteral($"constant_text_{_constantTextLiterals.Count}",
                    Encoding.UTF8.GetBytes(text));
        }
        foreach (var routine in _analysis.SemanticModel.Routines.Values)
        {
            _routineLabels[routine] = "routine_" + _routineLabels.Count;
        }
        var recordId = 0;
        foreach (var record in OrderedRecordTypes())
            _recordHelperLabels[record] = $"record_{recordId++}_{SafeName(record.RuntimeIdentity)}";
    }

    private void BuildFrameLayouts()
    {
        foreach (var routine in _analysis.SemanticModel.Routines.Values)
        {
            var localOffsets = new Dictionary<VariableSymbol, int>();
            var offset = 0;
            foreach (var symbol in routine.LocalSymbols.Values.Where(symbol => !symbol.IsConstant)
                         .OrderBy(symbol => symbol.DeclarationSpan.Start))
            {
                offset += Math.Max(1, symbol.ArraySize) * Math.Max(8, symbol.Type.Size);
                localOffsets[symbol] = offset;
            }

            var temporaries = _temporaries.Where(temporary => ReferenceEquals(temporary.Owner, routine.Declaration))
                .ToArray();
            foreach (var temporary in temporaries)
            {
                offset += temporary.Size;
                temporary.FrameOffset = offset;
            }

            var returnOffset = offset + 8;
            _frameLayouts[routine] = new MasmFrameLayout(localOffsets, temporaries, returnOffset,
                Align16(returnOffset + 160));
        }
    }

    private void EmitRoutine(RoutineSymbol routine)
    {
        _currentSource = routine.Source;
        _currentRoutine = routine;
        _currentFrame = _frameLayouts[routine];
        _returnLabel = NewLabel("routine_return");
        _activeCleanups.Clear();
        _clipDepth = 0;
        Line();
        Line($"{_routineLabels[routine]} PROC");
        Line("    push rbp");
        Line("    mov rbp, rsp");
        Line($"    sub rsp, {_currentFrame.FrameSize}");
        foreach (var symbol in routine.LocalSymbols.Values.Where(symbol => !symbol.IsConstant))
            for (var index = 0; index < Math.Max(1, symbol.ArraySize) * Math.Max(1, symbol.Type.Size / 8); index++)
                Line($"    mov QWORD PTR [rbp-{_currentFrame.LocalOffsets[symbol] - index * 8}], 0");
        foreach (var temporary in _currentFrame.Temporaries.Where(temporary => temporary.RequiresCleanup))
            for (var index = 0; index < temporary.Size / 8; index++)
                Line($"    mov QWORD PTR [rbp-{temporary.FrameOffset - index * 8}], 0");
        var argumentShift = routine.ReturnType is RecordTypeSymbol ? 1 : 0;
        if (argumentShift != 0)
        {
            Line("    mov QWORD PTR [rbp-" + _currentFrame.ReturnOffset + "], rcx");
        }
        for (var index = 0; index < routine.Parameters.Count; index++)
        {
            var parameter = routine.Parameters[index];
            var argumentIndex = index + argumentShift;
            var source = argumentIndex switch
            {
                0 => "rcx", 1 => "rdx", 2 => "r8", 3 => "r9",
                _ => $"QWORD PTR [rbp+{48 + (argumentIndex - 4) * 8}]"
            };
            if (argumentIndex >= 4)
                Line($"    mov rax, {source}");
            var sourceRegister = argumentIndex >= 4 ? "rax" : source;
            Line($"    mov QWORD PTR [rbp-{_currentFrame.LocalOffsets[parameter]}], {sourceRegister}");
        }
        foreach (var parameter in routine.Parameters.Where(parameter =>
                     parameter.Type is RecordTypeSymbol && parameter.ParameterMode != ParameterPassingMode.ByRef))
        {
            var record = (RecordTypeSymbol)parameter.Type;
            var offset = _currentFrame.LocalOffsets[parameter];
            Line($"    mov rdx, QWORD PTR [rbp-{offset}]");
            Line($"    mov QWORD PTR [rbp-{offset}], 0");
            Line($"    lea rcx, [rbp-{offset}]");
            CallAligned(RecordCopy(record));
        }
        if (argumentShift == 0)
            Line($"    mov QWORD PTR [rbp-{_currentFrame.ReturnOffset}], 0");
        EmitStatements(routine.Declaration.Statements);
        if (!routine.IsFunction)
            Line("    xor eax, eax");
        if (routine.ReturnType is not RecordTypeSymbol)
            Line($"    mov QWORD PTR [rbp-{_currentFrame.ReturnOffset}], rax");
        Line($"{_returnLabel}:");
        foreach (var temporary in _currentFrame.Temporaries.Where(temporary => temporary.RequiresCleanup))
            EmitCleanup(new MasmCleanupAction(temporary));
        foreach (var symbol in routine.LocalSymbols.Values.Where(symbol => symbol.Type.RequiresCleanup &&
                     symbol.ParameterMode != ParameterPassingMode.ByRef))
            EmitReleaseSymbol(symbol);
        Line($"    mov rax, QWORD PTR [rbp-{_currentFrame.ReturnOffset}]");
        Line("    mov rsp, rbp");
        Line("    pop rbp");
        Line("    ret");
        Line($"{_routineLabels[routine]} ENDP");
        _returnLabel = null;
        _currentRoutine = null;
        _currentFrame = null;
        _activeCleanups.Clear();
    }

    private void EmitStatements(IReadOnlyList<StatementSyntax> statements)
    {
        foreach (var statement in statements)
            EmitStatement(statement);
    }

    private void EmitStatement(StatementSyntax statement)
    {
        if (_emitDebugInformation && IsExecutable(statement))
        {
            CallAligned(_debugSitesByStatement[statement].HelperName);
        }

        switch (statement)
        {
            case ConstStatementSyntax:
            case DimStatementSyntax:
            case TypeDeclarationSyntax:
            case RoutineDeclarationSyntax:
                break;
            case AssignmentStatementSyntax assignment:
                EmitAssignment(assignment);
                break;
            case PrintStatementSyntax print:
                EmitPrint(print);
                break;
            case GetKeyStatementSyntax getKey:
                CallAligned("smile_get_key");
                EmitStore(Resolve(getKey.Identifier.Text));
                break;
            case ClearScreenStatementSyntax:
                CallAligned("smile_clear_screen");
                break;
            case WaitStatementSyntax wait:
                EmitExpression(wait.Duration);
                Line("    mov rcx, rax");
                CallAligned("smile_wait");
                break;
            case RandomStatementSyntax random:
                EmitExpression(random.Minimum);
                PushRax();
                EmitExpression(random.Maximum);
                Line("    mov rdx, rax");
                PopRcx();
                CallAligned("smile_random");
                EmitStore(Resolve(random.Identifier.Text));
                break;
            case IfStatementSyntax ifStatement:
                EmitIf(ifStatement);
                break;
            case ForStatementSyntax forStatement:
                EmitFor(forStatement);
                break;
            case DoStatementSyntax doStatement:
                EmitDo(doStatement);
                break;
            case CallStatementSyntax call:
                EmitRoutineCall(call.Identifier.Text, call.Arguments);
                break;
            case ReturnStatementSyntax returnStatement:
                if (returnStatement.Expression != null)
                    EmitExpression(returnStatement.Expression);
                else
                    Line("    xor eax, eax");
                if (_currentRoutine!.ReturnType is RecordTypeSymbol returnRecord)
                {
                    Line("    mov rdx, rax");
                    Line($"    mov rcx, QWORD PTR [rbp-{_currentFrame!.ReturnOffset}]");
                    CallAligned(RecordCopy(returnRecord));
                }
                else
                    Line($"    mov QWORD PTR [rbp-{_currentFrame!.ReturnOffset}], rax");
                EmitPopClipsTo(0);
                EmitCleanupToDepth(0);
                Line($"    jmp {_returnLabel}");
                break;
            case SelectStatementSyntax select:
                EmitSelect(select);
                break;
            case ExitStatementSyntax exit:
                var loop = exit.TargetKeyword.Kind == SyntaxKind.ForKeyword
                    ? _forExitLabels.Peek()
                    : _doExitLabels.Peek();
                EmitPopClipsTo(loop.ClipDepth);
                EmitCleanupToDepth(loop.CleanupDepth);
                Line($"    jmp {loop.ExitLabel}");
                break;
            case EndProgramStatementSyntax:
                EmitPopClipsTo(0);
                EmitCleanupToDepth(0);
                if (_currentRoutine != null)
                {
                    foreach (var temporary in _currentFrame!.Temporaries.Where(temporary => temporary.RequiresCleanup))
                        EmitCleanup(new MasmCleanupAction(temporary));
                    foreach (var symbol in _currentRoutine.LocalSymbols.Values.Where(symbol => symbol.Type.RequiresCleanup &&
                                 symbol.ParameterMode != ParameterPassingMode.ByRef))
                        EmitReleaseSymbol(symbol);
                }
                EmitProgramCleanup();
                CallAligned("smile_media_shutdown");
                CallAligned("smile_text_lifetime_report");
                if (_usesMusic) CallAligned("smile_music_shutdown");
                Line("    xor ecx, ecx");
                CallAligned("ExitProcess");
                break;
            case GameWindowStatementSyntax gameWindow:
                EmitTextArgument(gameWindow.Title);
                if (gameWindow.Width != null) EmitExpression(gameWindow.Width); else Line("    mov rax, 960");
                PushRax();
                if (gameWindow.Height != null) EmitExpression(gameWindow.Height); else Line("    mov rax, 540");
                PushRax();
                EmitNativeCall("smile_game_open", 4);
                break;
            case ClearColorStatementSyntax clearColor:
                EmitExpression(clearColor.Color);
                PushRax();
                EmitNativeCall("smile_game_clear", 1);
                break;
            case GraphicsStatementSyntax graphics:
                EmitGraphics(graphics);
                break;
            case DrawImageStatementSyntax image:
                EmitDrawImage(image);
                break;
            case ImageLoadStatementSyntax image:
                EmitImageLoad(image);
                break;
            case ClipRectangleStatementSyntax clip:
                EmitClip(clip);
                break;
            case ShowScreenStatementSyntax:
                CallAligned("smile_show_screen");
                break;
            case SoundStatementSyntax sound:
                if (sound.IsStop)
                {
                    if (sound.Channel == null)
                        CallAligned("smile_stop_sound");
                    else
                    {
                        EmitExpression(sound.Channel);
                        Line("    mov rcx, rax");
                        CallAligned("smile_stop_sound_channel");
                    }
                }
                else
                {
                    EmitTextArgument(sound.Path!);
                    if (sound.Channel == null)
                        EmitNativeCall("smile_play_sound", 2);
                    else
                    {
                        EmitExpression(sound.Channel);
                        PushRax();
                        EmitNativeCall("smile_play_sound_channel", 3);
                    }
                }
                break;
            case MusicStatementSyntax music:
                EmitMusic(music);
                break;
            case LoadStatementSyntax load:
                EmitTextArgument(load.Key);
                EmitExpression(load.DefaultValue);
                PushRax();
                EmitNativeCall("smile_load_value", 3);
                EmitStore(Resolve(load.Identifier.Text));
                break;
            case TextFileLoadStatementSyntax textFileLoad:
                EmitTextArgument(textFileLoad.Path);
                var destination = Resolve(textFileLoad.Destination.Text);
                EmitAddress(destination);
                PushRax();
                Line($"    mov rax, {destination.ArraySize.ToString(CultureInfo.InvariantCulture)}");
                PushRax();
                EmitNativeCall("smile_load_text_file", 4);
                EmitStore(Resolve(textFileLoad.CountIdentifier.Text));
                break;
            case DataLoadStatementSyntax dataLoad:
                EmitExpression(dataLoad.Key);
                PushRax();
                var dataDestination = Resolve(dataLoad.Destination.Text);
                EmitAddress(dataDestination);
                PushRax();
                Line($"    mov rax, {dataDestination.ArraySize.ToString(CultureInfo.InvariantCulture)}");
                PushRax();
                EmitNativeCall("smile_load_data_value", 3);
                PushRax();
                EmitTargetAddress(dataLoad.CountTarget);
                Line("    mov rcx, rax");
                PopRax();
                Line("    mov QWORD PTR [rcx], rax");
                break;
            case DataSaveStatementSyntax dataSave:
                var dataSource = Resolve(dataSave.Source.Text);
                EmitAddress(dataSource);
                PushRax();
                Line($"    mov rax, {dataSource.ArraySize.ToString(CultureInfo.InvariantCulture)}");
                PushRax();
                EmitExpression(dataSave.Count);
                PushRax();
                EmitExpression(dataSave.Key);
                PushRax();
                EmitNativeCall("smile_save_data_value", 4);
                break;
            case SaveStatementSyntax save:
                EmitTextArgument(save.Key);
                var saved = Resolve(save.Identifier.Text);
                EmitLoad(saved);
                PushRax();
                EmitNativeCall("smile_save_value", 3);
                break;
        }
    }

    private void EmitMusic(MusicStatementSyntax statement)
    {
        switch (statement.Operation)
        {
            case MusicOperation.Play:
                EmitTextArgument(statement.Path!);
                Line($"    mov rax, {(statement.Loop ? 1 : 0)}");
                PushRax();
                EmitNativeCall("smile_music_play", 3);
                break;
            case MusicOperation.Pause:
                CallAligned("smile_music_pause");
                break;
            case MusicOperation.Resume:
                CallAligned("smile_music_resume");
                break;
            case MusicOperation.Stop:
                CallAligned("smile_music_stop");
                break;
            case MusicOperation.SetVolume:
                EmitExpression(statement.Volume!);
                Line("    mov rcx, rax");
                CallAligned("smile_music_set_volume");
                break;
        }
    }

    private void EmitGraphics(GraphicsStatementSyntax statement)
    {
        if (statement.Operation == GraphicsOperation.DrawText)
        {
            EmitExpression(statement.TextExpression!);
            PushRax();
        }
        foreach (var argument in statement.Arguments)
        {
            EmitExpression(argument);
            PushRax();
        }
        if (statement.Operation == GraphicsOperation.DrawText)
        {
            Line($"    mov rax, {(statement.Centered ? 1 : 0)}");
            PushRax();
        }
        var name = statement.Operation switch
        {
            GraphicsOperation.FillRectangle => "smile_fill_rectangle",
            GraphicsOperation.DrawRectangle => "smile_draw_rectangle",
            GraphicsOperation.FillRoundedRectangle => "smile_fill_rounded_rectangle",
            GraphicsOperation.DrawRoundedRectangle => "smile_draw_rounded_rectangle",
            GraphicsOperation.FillCircle => "smile_fill_circle",
            GraphicsOperation.DrawCircle => "smile_draw_circle",
            GraphicsOperation.DrawArc => "smile_draw_arc",
            GraphicsOperation.FillQuadrilateral => "smile_fill_quadrilateral",
            GraphicsOperation.DrawQuadrilateral => "smile_draw_quadrilateral",
            GraphicsOperation.DrawLine => "smile_draw_line",
            GraphicsOperation.DrawText => "smile_draw_text_value",
            GraphicsOperation.DrawNumber => "smile_draw_number",
            _ => throw new InvalidOperationException("Unknown graphics operation.")
        };
        EmitNativeCall(name, statement.Arguments.Count + (statement.Operation == GraphicsOperation.DrawText ? 2 : 0));
    }

    private static IEnumerable<ExpressionSyntax> ImageExpressions(DrawImageStatementSyntax image)
    {
        foreach (var argument in new ExpressionSyntax?[] { image.SourceX, image.SourceY, image.SourceWidth,
                     image.SourceHeight, image.DestinationX, image.DestinationY, image.DestinationWidth,
                     image.DestinationHeight, image.Opacity, image.AnchorX, image.AnchorY })
            if (argument != null)
                yield return argument;
    }

    private void EmitDrawImage(DrawImageStatementSyntax image)
    {
        EmitExpression(image.Image);
        PushRax();
        EmitImageArgument(image.SourceX, 0);
        EmitImageArgument(image.SourceY, 0);
        EmitImageArgument(image.SourceWidth, -1);
        EmitImageArgument(image.SourceHeight, -1);
        EmitImageArgument(image.DestinationX, 0);
        EmitImageArgument(image.DestinationY, 0);
        EmitImageArgument(image.DestinationWidth, -1);
        EmitImageArgument(image.DestinationHeight, -1);
        EmitImageArgument(image.Opacity, 100);
        Line($"    mov rax, {(int)image.Filter}");
        PushRax();
        Line($"    mov rax, {(int)image.Flip}");
        PushRax();
        EmitImageArgument(image.AnchorX, 0);
        EmitImageArgument(image.AnchorY, 0);
        EmitNativeCall("smile_draw_image_value", 14);
    }

    private void EmitImageArgument(ExpressionSyntax? expression, long defaultValue)
    {
        if (expression == null)
            Line($"    mov rax, {defaultValue.ToString(CultureInfo.InvariantCulture)}");
        else
            EmitExpression(expression);
        PushRax();
    }

    private void EmitImageLoad(ImageLoadStatementSyntax image)
    {
        EmitTargetAddress(image.Target);
        if (image.IsUnload)
        {
            Line("    mov rcx, rax");
            CallAligned("smile_image_clear");
            return;
        }
        PushRax();
        EmitExpression(image.Path!);
        PushRax();
        EmitNativeCall("smile_load_image_value", 2);
    }

    private void EmitClip(ClipRectangleStatementSyntax clip)
    {
        foreach (var argument in clip.Arguments)
        {
            EmitExpression(argument);
            PushRax();
        }
        EmitNativeCall("smile_clip_push", 4);
        _clipDepth++;
        EmitStatements(clip.Statements);
        CallAligned("smile_clip_pop");
        _clipDepth--;
    }

    private void EmitTextArgument(SyntaxToken token)
    {
        var text = _gameTextLiterals[token];
        Line($"    lea rax, {text.Label}");
        PushRax();
        Line($"    mov rax, {text.Bytes.Length.ToString(CultureInfo.InvariantCulture)}");
        PushRax();
    }

    private void EmitNativeCall(string name, int argumentCount)
    {
        var outerSlots = _dynamicStackSlots - argumentCount;
        var stackArguments = Math.Max(0, argumentCount - 4);
        var padSlots = (outerSlots + argumentCount + stackArguments) & 1;
        var callAreaBytes = (4 + stackArguments + padSlots) * 8;
        Line($"    sub rsp, {callAreaBytes}");
        for (var index = 4; index < argumentCount; index++)
        {
            var sourceOffset = callAreaBytes + (argumentCount - 1 - index) * 8;
            var destinationOffset = 32 + (index - 4) * 8;
            Line($"    mov rax, QWORD PTR [rsp+{sourceOffset}]");
            Line($"    mov QWORD PTR [rsp+{destinationOffset}], rax");
        }
        var registerCount = Math.Min(4, argumentCount);
        for (var index = 0; index < registerCount; index++)
        {
            var sourceOffset = callAreaBytes + (argumentCount - 1 - index) * 8;
            var register = index switch { 0 => "rcx", 1 => "rdx", 2 => "r8", _ => "r9" };
            Line($"    mov {register}, QWORD PTR [rsp+{sourceOffset}]");
        }
        Line($"    call {name}");
        Line($"    add rsp, {callAreaBytes + argumentCount * 8}");
        _dynamicStackSlots = outerSlots;
    }

    private void EmitAssignment(AssignmentStatementSyntax assignment)
    {
        EmitExpression(assignment.Expression);
        var targetType = GetTargetType(assignment.Target);
        if (targetType is RecordTypeSymbol targetRecord)
        {
            PushRax();
            EmitTargetAddress(assignment.Target);
            Line("    mov rcx, rax");
            PopRax();
            Line("    mov rdx, rax");
            CallAligned(RecordCopy(targetRecord));
            return;
        }
        if (assignment.Target.Fields.Count != 0)
        {
            PushRax();
            EmitTargetAddress(assignment.Target);
            Line("    mov rcx, rax");
            PopRax();
            if (targetType == SmileType.Text || targetType == SmileType.Image)
            {
                Line("    mov rdx, rax");
                CallAligned(targetType == SmileType.Text ? "smile_text_move_assign" : "smile_image_move_assign");
            }
            else
                Line("    mov QWORD PTR [rcx], rax");
            return;
        }
        if (!assignment.Target.IsArrayElement)
        {
            var target = Resolve(assignment.Target.Identifier.Text);
            if (target.Type == SmileType.Text || target.Type == SmileType.Image)
            {
                PushRax();
                EmitAddress(target);
                Line("    mov rcx, rax");
                PopRax();
                Line("    mov rdx, rax");
                CallAligned(target.Type == SmileType.Text ? "smile_text_move_assign" : "smile_image_move_assign");
            }
            else
                EmitStore(target);
            return;
        }
        PushRax();
        var symbol = Resolve(assignment.Target.Identifier.Text);
        EmitArrayIndex(assignment.Target.Indices, symbol);
        Line("    mov rcx, rax");
        EmitAddress(symbol);
        Line($"    imul rcx, {Math.Max(8, symbol.Type.Size).ToString(CultureInfo.InvariantCulture)}");
        Line("    lea rcx, [rax+rcx]");
        PopRax();
        if (symbol.Type == SmileType.Text || symbol.Type == SmileType.Image)
        {
            Line("    mov rdx, rax");
            CallAligned(symbol.Type == SmileType.Text ? "smile_text_move_assign" : "smile_image_move_assign");
        }
        else
            Line("    mov QWORD PTR [rcx], rax");
    }

    private void EmitArrayIndex(IReadOnlyList<ExpressionSyntax> indices, VariableSymbol symbol)
    {
        EmitExpression(indices[0]);
        if (indices.Count == 1)
            return;
        PushRax();
        EmitExpression(indices[1]);
        Line("    mov rcx, rax");
        PopRax();
        Line($"    imul rax, {symbol.ArrayDimensions[1].ToString(CultureInfo.InvariantCulture)}");
        Line("    add rax, rcx");
    }

    private SmileType GetTargetType(AssignmentTargetSyntax target)
    {
        SmileType type = Resolve(target.Identifier.Text).Type;
        foreach (var token in target.Fields)
        {
            if (type is not RecordTypeSymbol record || !record.TryGetField(token.Text, out var field))
                throw new InvalidOperationException($"Unresolved record field '{token.Text}'.");
            type = field.Type;
        }
        return type;
    }

    private void EmitTargetAddress(AssignmentTargetSyntax target)
    {
        var symbol = Resolve(target.Identifier.Text);
        if (target.IsArrayElement)
        {
            EmitArrayIndex(target.Indices, symbol);
            Line("    mov rcx, rax");
            EmitAddress(symbol);
            Line($"    imul rcx, {Math.Max(8, symbol.Type.Size).ToString(CultureInfo.InvariantCulture)}");
            Line("    add rax, rcx");
        }
        else
            EmitAddress(symbol);

        SmileType type = symbol.Type;
        foreach (var token in target.Fields)
        {
            var record = (RecordTypeSymbol)type;
            record.TryGetField(token.Text, out var field);
            if (field.Offset != 0)
                Line($"    add rax, {field.Offset.ToString(CultureInfo.InvariantCulture)}");
            type = field.Type;
        }
    }

    private void EmitPrint(PrintStatementSyntax print)
    {
        foreach (var item in print.Items)
        {
            if (_analysis.SemanticModel.GetType(item) == SmileType.Text)
            {
                EmitExpression(item);
                Line("    mov rcx, rax");
                CallAligned("smile_print_text_value");
                continue;
            }
            EmitExpression(item);
            Line("    mov rcx, rax");
            CallAligned(_analysis.SemanticModel.GetType(item) == SmileType.Boolean
                ? "smile_print_boolean"
                : "smile_print_number");
        }
        if (!print.SuppressNewLine)
            CallAligned("smile_print_newline");
    }

    private void EmitIf(IfStatementSyntax statement)
    {
        var endLabel = NewLabel("if_end");
        foreach (var clause in statement.Clauses)
        {
            var nextLabel = NewLabel("if_next");
            EmitExpression(clause.Condition);
            Line("    cmp rax, 0");
            Line($"    je {nextLabel}");
            EmitStatements(clause.Statements);
            Line($"    jmp {endLabel}");
            Line($"{nextLabel}:");
        }
        EmitStatements(statement.ElseStatements);
        Line($"{endLabel}:");
    }

    private void EmitFor(ForStatementSyntax statement)
    {
        var startLabel = NewLabel("for_start");
        var endLabel = NewLabel("for_end");
        var counter = Resolve(statement.Identifier.Text);
        var limit = _forLimits[statement];
        EmitExpression(statement.LowerBound);
        EmitStore(counter);
        EmitExpression(statement.UpperBound);
        Line($"    mov {TemporaryMemory(limit)}, rax");
        Line($"{startLabel}:");
        EmitLoad(counter);
        Line($"    cmp rax, {TemporaryMemory(limit)}");
        Line(statement.IsDescending ? $"    jl {endLabel}" : $"    jg {endLabel}");
        _forExitLabels.Push(new MasmLoopContext(endLabel, _activeCleanups.Count, _clipDepth));
        EmitStatements(statement.Statements);
        _forExitLabels.Pop();
        EmitAddress(counter);
        Line(statement.IsDescending ? "    dec QWORD PTR [rax]" : "    inc QWORD PTR [rax]");
        Line($"    jmp {startLabel}");
        Line($"{endLabel}:");
    }

    private void EmitDo(DoStatementSyntax statement)
    {
        var startLabel = NewLabel("do_start");
        var endLabel = NewLabel("do_end");
        Line($"{startLabel}:");
        _doExitLabels.Push(new MasmLoopContext(endLabel, _activeCleanups.Count, _clipDepth));
        EmitStatements(statement.Statements);
        _doExitLabels.Pop();
        if (statement.UntilCondition == null)
        {
            Line($"    jmp {startLabel}");
        }
        else
        {
            EmitExpression(statement.UntilCondition);
            Line("    cmp rax, 0");
            Line($"    je {startLabel}");
        }
        Line($"{endLabel}:");
    }

    private void EmitSelect(SelectStatementSyntax statement)
    {
        var endLabel = NewLabel("select_end");
        var value = _selectValues[statement];
        var isText = value.Type == SmileType.Text;
        EmitExpression(statement.Expression);
        MasmCleanupAction? cleanup = null;
        if (isText)
        {
            PushRax();
            EmitTemporaryAddress(value, "rcx");
            PopRax();
            Line("    mov rdx, rax");
            CallAligned("smile_text_move_assign");
            cleanup = new MasmCleanupAction(value);
            _activeCleanups.Add(cleanup);
        }
        else
            Line($"    mov {TemporaryMemory(value)}, rax");
        SelectCaseClauseSyntax? elseClause = null;
        foreach (var clause in statement.Cases)
        {
            if (clause.IsElse)
            {
                elseClause = clause;
                continue;
            }
            var nextLabel = NewLabel("case_next");
            EmitExpression(clause.Value!);
            if (isText)
            {
                Line("    mov rdx, rax");
                Line($"    mov rcx, {TemporaryMemory(value)}");
                CallAligned("smile_text_equal_case");
                Line("    cmp rax, 0");
                Line($"    je {nextLabel}");
            }
            else
            {
                Line($"    cmp {TemporaryMemory(value)}, rax");
                Line($"    jne {nextLabel}");
            }
            EmitStatements(clause.Statements);
            Line($"    jmp {endLabel}");
            Line($"{nextLabel}:");
        }
        if (elseClause != null)
            EmitStatements(elseClause.Statements);
        Line($"{endLabel}:");
        if (cleanup != null)
        {
            EmitCleanup(cleanup);
            _activeCleanups.RemoveAt(_activeCleanups.Count - 1);
        }
    }

    private void EmitExpression(ExpressionSyntax expression)
    {
        switch (expression)
        {
            case LiteralExpressionSyntax literal when literal.Value is bool boolean:
                Line($"    mov rax, {(boolean ? 1 : 0)}");
                break;
            case LiteralExpressionSyntax literal when literal.Value is long number:
                Line($"    mov rax, {number.ToString(CultureInfo.InvariantCulture)}");
                break;
            case LiteralExpressionSyntax literal when literal.Value is string:
                Line($"    lea rcx, {_textLiterals[literal].Label}");
                CallAligned("smile_text_retain");
                break;
            case NameExpressionSyntax name:
                var symbol = Resolve(name.Identifier.Text);
                EmitLoad(symbol);
                break;
            case ArrayAccessExpressionSyntax array:
                var arraySymbol = Resolve(array.Identifier.Text);
                EmitArrayIndex(array.Indices, arraySymbol);
                Line("    mov rcx, rax");
                EmitAddress(arraySymbol);
                Line($"    imul rcx, {Math.Max(8, arraySymbol.Type.Size).ToString(CultureInfo.InvariantCulture)}");
                if (arraySymbol.Type is RecordTypeSymbol)
                    Line("    lea rax, [rax+rcx]");
                else
                {
                    Line("    mov rax, QWORD PTR [rax+rcx]");
                    if (arraySymbol.Type == SmileType.Text || arraySymbol.Type == SmileType.Image)
                    {
                        Line("    mov rcx, rax");
                        CallAligned(arraySymbol.Type == SmileType.Text ? "smile_text_retain" : "smile_image_retain");
                    }
                }
                break;
            case FieldAccessExpressionSyntax field:
                EmitWritableAddress(field);
                if (_analysis.SemanticModel.GetType(field) is not RecordTypeSymbol)
                {
                    Line("    mov rax, QWORD PTR [rax]");
                    if (_analysis.SemanticModel.GetType(field) == SmileType.Text ||
                        _analysis.SemanticModel.GetType(field) == SmileType.Image)
                    {
                        Line("    mov rcx, rax");
                        CallAligned(_analysis.SemanticModel.GetType(field) == SmileType.Text
                            ? "smile_text_retain" : "smile_image_retain");
                    }
                }
                break;
            case ParenthesizedExpressionSyntax parenthesized:
                EmitExpression(parenthesized.Expression);
                break;
            case UnaryExpressionSyntax unary:
                EmitExpression(unary.Operand);
                Line(unary.OperatorToken.Kind == SyntaxKind.MinusToken ? "    neg rax" : "    xor rax, 1");
                break;
            case BinaryExpressionSyntax binary:
                EmitBinary(binary);
                break;
            case CallExpressionSyntax call:
                EmitCallExpression(call);
                break;
            default:
                Line("    xor rax, rax");
                break;
        }
    }

    private void EmitCallExpression(CallExpressionSyntax call)
    {
        switch (call.Identifier.Kind)
        {
            case SyntaxKind.AbsKeyword:
                EmitExpression(call.Arguments[0]);
                var positive = NewLabel("abs_positive");
                Line("    cmp rax, 0");
                Line($"    jge {positive}");
                Line("    neg rax");
                Line($"{positive}:");
                break;
            case SyntaxKind.MinKeyword:
            case SyntaxKind.MaxKeyword:
                EmitExpression(call.Arguments[0]);
                PushRax();
                EmitExpression(call.Arguments[1]);
                Line("    mov rcx, rax");
                PopRax();
                Line("    cmp rax, rcx");
                Line(call.Identifier.Kind == SyntaxKind.MinKeyword ? "    cmovg rax, rcx" : "    cmovl rax, rcx");
                break;
            case SyntaxKind.RgbKeyword:
                EmitExpression(call.Arguments[0]);
                PushRax();
                EmitExpression(call.Arguments[1]);
                PushRax();
                EmitExpression(call.Arguments[2]);
                Line("    mov rdx, rax");
                PopRcx();
                PopRax();
                Line("    and rax, 255");
                Line("    and rcx, 255");
                Line("    shl rcx, 8");
                Line("    and rdx, 255");
                Line("    shl rdx, 16");
                Line("    or rax, rcx");
                Line("    or rax, rdx");
                break;
            case SyntaxKind.TimerKeyword:
                CallAligned("smile_timer");
                break;
            case SyntaxKind.GameClosedKeyword:
                CallAligned("smile_game_closed");
                break;
            case SyntaxKind.KeyHeldKeyword:
                EmitExpression(call.Arguments[0]);
                Line("    mov rcx, rax");
                CallAligned("smile_key_held");
                break;
            case SyntaxKind.ImageWidthKeyword:
            case SyntaxKind.ImageHeightKeyword:
            case SyntaxKind.ImageLoadedKeyword:
                EmitExpression(call.Arguments[0]);
                Line("    mov rcx, rax");
                CallAligned(call.Identifier.Kind switch
                {
                    SyntaxKind.ImageWidthKeyword => "smile_image_width_value",
                    SyntaxKind.ImageHeightKeyword => "smile_image_height_value",
                    _ => "smile_image_loaded_value"
                });
                break;
            case SyntaxKind.TextWidthKeyword:
            case SyntaxKind.TextHeightKeyword:
                EmitExpression(call.Arguments[0]);
                PushRax();
                EmitExpression(call.Arguments[1]);
                PushRax();
                EmitNativeCall(call.Identifier.Kind == SyntaxKind.TextWidthKeyword
                    ? "smile_text_width_value" : "smile_text_height_value", 2);
                break;
            default:
                if (_analysis.SemanticModel.TryGetRoutine(call.Identifier.Text, out var routine) &&
                    routine.ReturnType is RecordTypeSymbol)
                    EmitRoutineCall(call.Identifier.Text, call.Arguments, _recordCallResults[call]);
                else
                    EmitRoutineCall(call.Identifier.Text, call.Arguments);
                break;
        }
    }

    private void EmitRoutineCall(string name, IReadOnlyList<ExpressionSyntax> arguments,
        MasmTemporaryStorage? recordResult = null)
    {
        if (!_analysis.SemanticModel.TryGetRoutine(name, out var routine))
            throw new InvalidOperationException($"Unresolved routine '{name}'.");
        if (recordResult != null)
        {
            EmitTemporaryAddress(recordResult, "rax");
            PushRax();
        }
        for (var index = 0; index < arguments.Count; index++)
        {
            if (index < routine.Parameters.Count &&
                routine.Parameters[index].ParameterMode == ParameterPassingMode.ByRef)
                EmitWritableAddress(arguments[index]);
            else
                EmitExpression(arguments[index]);
            PushRax();
        }
        EmitNativeCall(_routineLabels[routine], arguments.Count + (recordResult == null ? 0 : 1));
        if (recordResult != null)
            EmitTemporaryAddress(recordResult, "rax");
    }

    private void EmitWritableAddress(ExpressionSyntax expression)
    {
        if (expression is NameExpressionSyntax name)
        {
            EmitAddress(Resolve(name.Identifier.Text));
            return;
        }
        if (expression is ArrayAccessExpressionSyntax array)
        {
            var symbol = Resolve(array.Identifier.Text);
            EmitArrayIndex(array.Indices, symbol);
            Line("    mov rcx, rax");
            EmitAddress(symbol);
            Line($"    imul rcx, {Math.Max(8, symbol.Type.Size).ToString(CultureInfo.InvariantCulture)}");
            Line("    lea rax, [rax+rcx]");
            return;
        }
        if (expression is FieldAccessExpressionSyntax field)
        {
            EmitWritableAddress(field.Receiver);
            if (!_analysis.SemanticModel.TryGetField(field, out var fieldSymbol))
                throw new InvalidOperationException($"Unbound record field '{field.Field.Text}'.");
            if (fieldSymbol.Offset != 0)
                Line($"    add rax, {fieldSymbol.Offset.ToString(CultureInfo.InvariantCulture)}");
            return;
        }
        if (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            EmitWritableAddress(parenthesized.Expression);
            return;
        }
        if (expression is CallExpressionSyntax call && _analysis.SemanticModel.GetType(call) is RecordTypeSymbol)
        {
            EmitCallExpression(call);
            return;
        }
        Line("    xor eax, eax");
    }

    private void EmitBinary(BinaryExpressionSyntax binary)
    {
        EmitExpression(binary.Left);
        PushRax();
        EmitExpression(binary.Right);
        Line("    mov rcx, rax");
        PopRax();
        if (_analysis.SemanticModel.GetType(binary.Left) == SmileType.Text)
        {
            Line("    mov rdx, rcx");
            Line("    mov rcx, rax");
            if (binary.OperatorToken.Kind == SyntaxKind.PlusToken)
                CallAligned("smile_text_concat");
            else
            {
                CallAligned("smile_text_equal");
                if (binary.OperatorToken.Kind == SyntaxKind.NotEqualsToken)
                    Line("    xor rax, 1");
            }
            return;
        }
        switch (binary.OperatorToken.Kind)
        {
            case SyntaxKind.PlusToken: Line("    add rax, rcx"); break;
            case SyntaxKind.MinusToken: Line("    sub rax, rcx"); break;
            case SyntaxKind.StarToken: Line("    imul rax, rcx"); break;
            case SyntaxKind.SlashToken:
                Line("    cqo");
                Line("    idiv rcx");
                break;
            case SyntaxKind.ModKeyword:
                Line("    cqo");
                Line("    idiv rcx");
                Line("    mov rax, rdx");
                break;
            case SyntaxKind.AndKeyword: Line("    and rax, rcx"); break;
            case SyntaxKind.OrKeyword: Line("    or rax, rcx"); break;
            case SyntaxKind.EqualsToken: EmitComparison("sete"); break;
            case SyntaxKind.NotEqualsToken: EmitComparison("setne"); break;
            case SyntaxKind.LessToken: EmitComparison("setl"); break;
            case SyntaxKind.GreaterToken: EmitComparison("setg"); break;
            case SyntaxKind.LessOrEqualsToken: EmitComparison("setle"); break;
            case SyntaxKind.GreaterOrEqualsToken: EmitComparison("setge"); break;
        }
    }

    private void EmitComparison(string instruction)
    {
        Line("    cmp rax, rcx");
        Line($"    {instruction} al");
        Line("    movzx rax, al");
    }

    private VariableSymbol Resolve(string name)
    {
        if (_analysis.SemanticModel.TryResolveVariable(name, _currentRoutine?.Name, out var symbol))
            return symbol;
        throw new InvalidOperationException($"Unresolved symbol '{name}'.");
    }

    private void EmitAddress(VariableSymbol symbol)
    {
        if (_currentFrame != null && _currentFrame.LocalOffsets.TryGetValue(symbol, out var offset))
        {
            Line(symbol.ParameterMode == ParameterPassingMode.ByRef
                ? $"    mov rax, QWORD PTR [rbp-{offset}]"
                : $"    lea rax, [rbp-{offset}]");
            return;
        }
        Line($"    lea rax, {_symbolLabels[symbol]}");
    }

    private void EmitLoad(VariableSymbol symbol)
    {
        if (symbol.IsConstant)
        {
            if (symbol.Type == SmileType.Text)
            {
                Line($"    lea rcx, {_constantTextLiterals[symbol].Label}");
                CallAligned("smile_text_retain");
                return;
            }
            var value = symbol.ConstantValue switch
            {
                long number => number,
                bool boolean => boolean ? 1L : 0L,
                _ => 0L
            };
            Line($"    mov rax, {value.ToString(CultureInfo.InvariantCulture)}");
            return;
        }
        EmitAddress(symbol);
        if (symbol.Type is RecordTypeSymbol)
            return;
        Line("    mov rax, QWORD PTR [rax]");
        if (symbol.Type == SmileType.Text || symbol.Type == SmileType.Image)
        {
            Line("    mov rcx, rax");
            CallAligned(symbol.Type == SmileType.Text ? "smile_text_retain" : "smile_image_retain");
        }
    }

    private void EmitStore(VariableSymbol symbol)
    {
        Line("    mov r10, rax");
        EmitAddress(symbol);
        Line("    mov QWORD PTR [rax], r10");
        Line("    mov rax, r10");
    }

    private void EmitReleaseSymbol(VariableSymbol symbol)
    {
        var count = Math.Max(1, symbol.ArraySize);
        for (var index = 0; index < count; index++)
        {
            EmitAddress(symbol);
            if (index != 0)
                Line($"    add rax, {index * Math.Max(8, symbol.Type.Size)}");
            if (symbol.Type is RecordTypeSymbol record)
            {
                Line("    mov rcx, rax");
                CallAligned(RecordClear(record));
            }
            else
            {
                Line("    mov rcx, QWORD PTR [rax]");
                CallAligned(symbol.Type == SmileType.Text ? "smile_text_release" : "smile_image_release");
            }
        }
    }

    private void EmitProgramCleanup()
    {
        foreach (var symbol in _analysis.SemanticModel.Symbols.Values.Where(symbol =>
                     !symbol.IsConstant && symbol.Type.RequiresCleanup))
            EmitReleaseSymbol(symbol);
        foreach (var temporary in _temporaries.Where(temporary => temporary.Kind == MasmStorageKind.Static &&
                     temporary.RequiresCleanup))
            EmitCleanup(new MasmCleanupAction(temporary));
    }

    private string TemporaryMemory(MasmTemporaryStorage storage)
    {
        return storage.Kind == MasmStorageKind.Static
            ? $"QWORD PTR [{storage.Name}]"
            : $"QWORD PTR [rbp-{storage.FrameOffset}]";
    }

    private void EmitTemporaryAddress(MasmTemporaryStorage storage, string register)
    {
        Line(storage.Kind == MasmStorageKind.Static
            ? $"    lea {register}, {storage.Name}"
            : $"    lea {register}, [rbp-{storage.FrameOffset}]");
    }

    private void EmitCleanup(MasmCleanupAction cleanup)
    {
        EmitTemporaryAddress(cleanup.Storage, "rcx");
        if (cleanup.Storage.Type is RecordTypeSymbol record)
            CallAligned(RecordClear(record));
        else
            CallAligned(cleanup.Storage.Type == SmileType.Text ? "smile_text_clear" : "smile_image_clear");
    }

    private void EmitCleanupToDepth(int cleanupDepth)
    {
        for (var index = _activeCleanups.Count - 1; index >= cleanupDepth; index--)
            EmitCleanup(_activeCleanups[index]);
    }

    private void EmitPopClipsTo(int clipDepth)
    {
        for (var index = _clipDepth; index > clipDepth; index--)
            CallAligned("smile_clip_pop");
    }

    private void EmitRecordHelpers(RecordTypeSymbol record)
    {
        var prefix = _recordHelperLabels[record];
        Line();
        Line($"{prefix}_init PROC");
        for (var offset = 0; offset < record.Size; offset += 8)
            Line($"    mov QWORD PTR [rcx{Offset(offset)}], 0");
        Line("    mov rax, rcx");
        Line("    ret");
        Line($"{prefix}_init ENDP");

        Line($"{prefix}_clear PROC");
        Line("    push rbp");
        Line("    mov rbp, rsp");
        Line("    sub rsp, 48");
        Line("    mov QWORD PTR [rbp-8], rcx");
        foreach (var field in record.Fields)
        {
            if (!field.Type.RequiresCleanup)
                continue;
            Line("    mov rax, QWORD PTR [rbp-8]");
            Line($"    lea rcx, [rax{Offset(field.Offset)}]");
            Line(field.Type is RecordTypeSymbol nested
                ? $"    call {RecordClear(nested)}"
                : field.Type == SmileType.Text ? "    call smile_text_clear" : "    call smile_image_clear");
        }
        Line("    mov rax, QWORD PTR [rbp-8]");
        Line("    mov rsp, rbp");
        Line("    pop rbp");
        Line("    ret");
        Line($"{prefix}_clear ENDP");

        var done = NewLabel("record_copy_done");
        Line($"{prefix}_copy PROC");
        Line("    cmp rcx, rdx");
        Line($"    je {done}");
        Line("    push rbp");
        Line("    mov rbp, rsp");
        Line("    sub rsp, 48");
        Line("    mov QWORD PTR [rbp-8], rcx");
        Line("    mov QWORD PTR [rbp-16], rdx");
        foreach (var field in record.Fields)
        {
            if (field.Type is RecordTypeSymbol nested)
            {
                Line("    mov rax, QWORD PTR [rbp-8]");
                Line($"    lea rcx, [rax{Offset(field.Offset)}]");
                Line("    mov rax, QWORD PTR [rbp-16]");
                Line($"    lea rdx, [rax{Offset(field.Offset)}]");
                Line($"    call {RecordCopy(nested)}");
            }
            else if (field.Type == SmileType.Text)
            {
                Line("    mov rax, QWORD PTR [rbp-16]");
                Line($"    mov rcx, QWORD PTR [rax{Offset(field.Offset)}]");
                Line("    call smile_text_retain");
                Line("    mov rdx, rax");
                Line("    mov rax, QWORD PTR [rbp-8]");
                Line($"    lea rcx, [rax{Offset(field.Offset)}]");
                Line("    call smile_text_move_assign");
            }
            else if (field.Type == SmileType.Image)
            {
                Line("    mov rax, QWORD PTR [rbp-16]");
                Line($"    mov rcx, QWORD PTR [rax{Offset(field.Offset)}]");
                Line("    call smile_image_retain");
                Line("    mov rdx, rax");
                Line("    mov rax, QWORD PTR [rbp-8]");
                Line($"    lea rcx, [rax{Offset(field.Offset)}]");
                Line("    call smile_image_move_assign");
            }
            else
            {
                Line("    mov rax, QWORD PTR [rbp-16]");
                Line($"    mov rdx, QWORD PTR [rax{Offset(field.Offset)}]");
                Line("    mov rax, QWORD PTR [rbp-8]");
                Line($"    mov QWORD PTR [rax{Offset(field.Offset)}], rdx");
            }
        }
        Line("    mov rax, QWORD PTR [rbp-8]");
        Line("    mov rsp, rbp");
        Line("    pop rbp");
        Line("    ret");
        Line($"{done}:");
        Line("    mov rax, rcx");
        Line("    ret");
        Line($"{prefix}_copy ENDP");
    }

    private string RecordCopy(RecordTypeSymbol record) => _recordHelperLabels[record] + "_copy";
    private string RecordClear(RecordTypeSymbol record) => _recordHelperLabels[record] + "_clear";
    private static string Offset(int offset) => offset == 0 ? string.Empty : $"+{offset}";

    private IOrderedEnumerable<RecordTypeSymbol> OrderedRecordTypes() =>
        _analysis.SemanticModel.Types.Values.OrderBy(type => type.SourceOrdinal).ThenBy(type => type.DeclarationSpan.Start);

    private static string SafeName(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
            builder.Append(char.IsLetterOrDigit(character) || character == '_' ? char.ToLowerInvariant(character) : '_');
        return builder.Length == 0 ? "record" : builder.ToString();
    }

    private void PushRax()
    {
        Line("    push rax");
        _dynamicStackSlots++;
    }

    private void PopRax()
    {
        Line("    pop rax");
        _dynamicStackSlots--;
    }

    private void PopRcx()
    {
        Line("    pop rcx");
        _dynamicStackSlots--;
    }

    private void CallAligned(string name)
    {
        var callAreaBytes = 32 + ((_dynamicStackSlots & 1) != 0 ? 8 : 0);
        Line($"    sub rsp, {callAreaBytes}");
        Line($"    call {name}");
        Line($"    add rsp, {callAreaBytes}");
    }

    private static int Align16(int value) => (value + 15) & ~15;
    private string NewLabel(string prefix) => prefix + "_" + _labelId++;
    private void Line(string text = "") => _builder.AppendLine(text);

    private static bool IsExecutable(StatementSyntax statement) =>
        statement is not ConstStatementSyntax and not DimStatementSyntax and not TypeDeclarationSyntax and not RoutineDeclarationSyntax;

    private void EmitBytes(byte[] bytes, bool terminate)
    {
        var values = terminate ? bytes.Concat(new byte[] { 0 }).ToArray() : bytes;
        if (values.Length == 0)
            values = new byte[] { 0 };
        for (var offset = 0; offset < values.Length; offset += 16)
            Line("BYTE " + string.Join(",", values.Skip(offset).Take(16).Select(value => $"0{value:X2}h")));
    }

    private sealed class TextLiteral
    {
        public TextLiteral(string label, byte[] bytes)
        {
            Label = label;
            Bytes = bytes;
        }

        public string Label { get; }
        public byte[] Bytes { get; }
    }
}
