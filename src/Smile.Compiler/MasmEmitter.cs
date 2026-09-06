using System.Globalization;
using System.Text;
using Smile.Language;

namespace Smile.Compiler;

internal sealed class MasmDebugSite
{
    public MasmDebugSite(int id, SourceText source, StatementSyntax statement,
        IReadOnlyList<VariableSymbol> variables)
    {
        Id = id;
        Source = source;
        Statement = statement;
        Variables = variables;
        source.GetLineColumn(statement.Span.Start, out var line, out _);
        Line = line;
    }

    public int Id { get; }
    public SourceText Source { get; }
    public StatementSyntax Statement { get; }
    public IReadOnlyList<VariableSymbol> Variables { get; }
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
    public MasmTemporaryStorage(string name, SmileType type, RoutineSymbol? owner,
        int sizeOverride = 0)
    {
        Name = name;
        Type = type;
        Owner = owner;
        Kind = owner == null ? MasmStorageKind.Static : MasmStorageKind.Frame;
        SizeOverride = sizeOverride;
    }

    public string Name { get; }
    public SmileType Type { get; }
    public RoutineSymbol? Owner { get; }
    public MasmStorageKind Kind { get; }
    public int FrameOffset { get; set; }
    public int SizeOverride { get; }
    public int Size => SizeOverride == 0 ? Math.Max(8, Type.Size) : SizeOverride;
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

internal sealed record MasmCleanupAction(MasmTemporaryStorage Storage,
    MasmTemporaryStorage? Registration = null);
internal sealed record MasmLoopContext(string ExitLabel, int CleanupDepth, int ClipDepth);

internal sealed class MasmEmitter
{
    private static readonly HashSet<string> CDebugKeywords = new(StringComparer.Ordinal)
    {
        "auto", "break", "case", "char", "const", "continue", "default", "do", "double", "else",
        "enum", "extern", "float", "for", "goto", "if", "inline", "int", "long", "register",
        "restrict", "return", "short", "signed", "sizeof", "static", "struct", "switch", "typedef",
        "union", "unsigned", "void", "volatile", "while", "_Alignas", "_Alignof", "_Atomic", "_Bool",
        "_Complex", "_Generic", "_Imaginary", "_Noreturn", "_Static_assert", "_Thread_local"
    };

    private readonly SmileAnalysisResult _analysis;
    private readonly SmileGraphicsBackend _graphicsBackend;
    private readonly bool _vSync;
    private readonly bool _emitDebugInformation;
    private readonly byte[] _appIdentityBytes;
    private readonly byte[] _assetManifestBytes;
    private readonly bool _rememberWindowPlacement;
    private readonly bool _responsiveWindow;
    private readonly StringBuilder _builder = new();
    private readonly Dictionary<VariableSymbol, string> _symbolLabels = new();
    private readonly Dictionary<RoutineSymbol, string> _routineLabels = new();
    private readonly Dictionary<RecordTypeSymbol, string> _recordHelperLabels = new();
    private readonly Dictionary<ClassTypeSymbol, string> _classFinalizerLabels = new();
    private readonly Dictionary<ExpressionSyntax, MasmTemporaryStorage> _recordCallResults = new();
    private readonly Dictionary<BoundCallArgument, MasmTemporaryStorage> _callArgumentTemporaries = new();
    private readonly Dictionary<BoundCallArgument, MasmTemporaryStorage> _callArgumentRegistrations = new();
    private readonly Dictionary<SyntaxNode, MasmTemporaryStorage> _callReceiverTemporaries = new();
    private readonly Dictionary<SyntaxNode, MasmTemporaryStorage> _callReceiverRegistrations = new();
    private readonly Dictionary<SyntaxNode, MasmTemporaryStorage> _implicitValueTemporaries = new();
    private readonly Dictionary<SyntaxNode, MasmTemporaryStorage> _implicitValueRegistrations = new();
    private readonly Dictionary<LiteralExpressionSyntax, TextLiteral> _textLiterals = new();
    private readonly Dictionary<VariableSymbol, TextLiteral> _constantTextLiterals = new();
    private readonly Dictionary<ParameterSymbol, TextLiteral> _optionalDefaultTextLiterals = new();
    private readonly Dictionary<SyntaxToken, TextLiteral> _gameTextLiterals = new();
    private readonly Dictionary<ForStatementSyntax, MasmTemporaryStorage> _forLimits = new();
    private readonly Dictionary<DataLoadStatementSyntax, MasmTemporaryStorage> _dataLoadCounts = new();
    private readonly Dictionary<SelectStatementSyntax, MasmTemporaryStorage> _selectValues = new();
    private readonly Dictionary<WithStatementSyntax, MasmTemporaryStorage> _withLocations = new();
    private readonly Dictionary<WithStatementSyntax, MasmTemporaryStorage> _withRegistrations = new();
    private readonly Dictionary<ExpressionSyntax, MasmTemporaryStorage> _classLocationOwners = new();
    private readonly Dictionary<ExpressionSyntax, MasmTemporaryStorage> _classLocationOwnerRegistrations = new();
    private readonly Dictionary<AssignmentStatementSyntax, MasmTemporaryStorage> _classAssignmentValues = new();
    private readonly Dictionary<AssignmentStatementSyntax, MasmTemporaryStorage> _classAssignmentRegistrations = new();
    private readonly Dictionary<IdentityExpressionSyntax, (MasmTemporaryStorage Left, MasmTemporaryStorage Right)>
        _identityValues = new();
    private readonly Dictionary<IdentityExpressionSyntax, (MasmTemporaryStorage? Left, MasmTemporaryStorage? Right)>
        _identityRegistrations = new();
    private readonly Dictionary<NewExpressionSyntax, MasmTemporaryStorage> _constructorResults = new();
    private readonly Dictionary<NewExpressionSyntax, MasmTemporaryStorage> _constructorRegistrations = new();
    private readonly Dictionary<RoutineSymbol, MasmTemporaryStorage> _activeFrameRegistrations = new();
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
    private bool _usesKeyEventHeld;
    private bool _usesMusic;
    private int _dynamicStackSlots;
    private int _clipDepth;
    private RoutineSymbol? _collectRoutine;

    public MasmEmitter(SmileAnalysisResult analysis, SmileGraphicsBackend graphicsBackend,
        bool vSync, bool emitDebugInformation, string? appIdentity = null,
        IReadOnlyList<string>? assetPaths = null, bool rememberWindowPlacement = false,
        bool responsiveWindow = false)
    {
        _analysis = analysis;
        _graphicsBackend = graphicsBackend;
        _vSync = vSync;
        _emitDebugInformation = emitDebugInformation;
        _appIdentityBytes = Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(appIdentity) ? "Program" : appIdentity);
        _assetManifestBytes = Encoding.UTF8.GetBytes(string.Join("\n", assetPaths ?? Array.Empty<string>()));
        _rememberWindowPlacement = rememberWindowPlacement;
        _responsiveWindow = responsiveWindow;
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
        foreach (var routine in OrderedRoutines())
        {
            _currentSource = routine.Source;
            _collectRoutine = routine;
            foreach (var parameter in routine.Parameters)
                CollectExpression(parameter.Declaration.DefaultValue);
            Collect(routine.BodyStatements);
            _activeFrameRegistrations[routine] = CreateTemporary("active_frame_cleanup", SmileType.Number, 32);
        }
        _collectRoutine = null;
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
        Line("EXTERN smile_text_scalar_length:PROC");
        Line("EXTERN smile_text_code_at:PROC");
        Line("EXTERN smile_text_slice:PROC");
        Line("EXTERN smile_text_clear:PROC");
        Line("EXTERN smile_text_lifetime_report:PROC");
        Line("EXTERN smile_class_allocate:PROC");
        Line("EXTERN smile_class_retain:PROC");
        Line("EXTERN smile_class_release:PROC");
        Line("EXTERN smile_class_move_assign:PROC");
        Line("EXTERN smile_class_clear:PROC");
        Line("EXTERN smile_class_lifetime_report:PROC");
        Line("EXTERN smile_class_nothing_report:PROC");
        Line("EXTERN smile_class_allocation_failure_report:PROC");
        Line("EXTERN smile_image_retain:PROC");
        Line("EXTERN smile_image_release:PROC");
        Line("EXTERN smile_image_move_assign:PROC");
        Line("EXTERN smile_image_clear:PROC");
        Line("EXTERN smile_load_image_value:PROC");
        Line("EXTERN smile_draw_image_value:PROC");
        Line("EXTERN smile_image_width_value:PROC");
        Line("EXTERN smile_image_height_value:PROC");
        Line("EXTERN smile_image_loaded_value:PROC");
        Line("EXTERN smile_image_lifetime_report:PROC");
        Line("EXTERN smile_print_text_value:PROC");
        Line("EXTERN smile_draw_text_value:PROC");
        Line("EXTERN smile_get_key:PROC");
        Line("EXTERN smile_clear_screen:PROC");
        Line("EXTERN smile_wait:PROC");
        Line("EXTERN smile_random:PROC");
        if (_usesTimer) Line("EXTERN smile_timer:PROC");
        if (_usesGameClosed) Line("EXTERN smile_game_closed:PROC");
        if (_usesKeyHeld) Line("EXTERN smile_key_held:PROC");
        if (_usesKeyEventHeld) Line("EXTERN smile_key_event_held:PROC");
        Line("EXTERN smile_pointer_x:PROC");
        Line("EXTERN smile_pointer_y:PROC");
        Line("EXTERN smile_pointer_delta_x:PROC");
        Line("EXTERN smile_pointer_delta_y:PROC");
        Line("EXTERN smile_pointer_wheel_delta:PROC");
        Line("EXTERN smile_pointer_wheel_remainder:PROC");
        Line("EXTERN smile_pointer_inside:PROC");
        Line("EXTERN smile_pointer_held:PROC");
        Line("EXTERN smile_pointer_pressed:PROC");
        Line("EXTERN smile_pointer_released:PROC");
        Line("EXTERN smile_game_open:PROC");
        Line("EXTERN smile_graphics_configure:PROC");
        Line("EXTERN smile_window_width:PROC");
        Line("EXTERN smile_window_height:PROC");
        Line("EXTERN smile_window_title:PROC");
        Line("EXTERN smile_window_activate:PROC");
        Line("EXTERN smile_file_reveal:PROC");
        Line("EXTERN smile_file_export:PROC");
        Line("EXTERN smile_file_import:PROC");
        if (_rememberWindowPlacement) Line("EXTERN smile_window_persistence_configure:PROC");
        if (_responsiveWindow) Line("EXTERN smile_window_responsive_configure:PROC");
        Line("EXTERN smile_game_clear:PROC");
        Line("EXTERN smile_fill_rectangle:PROC");
        Line("EXTERN smile_fill_rectangle_opacity:PROC");
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
        Line("EXTERN smile_renderer3d_command:PROC");
        Line("EXTERN smile_renderer3d_image_command:PROC");
        Line("EXTERN smile_renderer3d_text_command:PROC");
        Line("EXTERN smile_renderer3d_text_value:PROC");
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
        Line("EXTERN smile_load_data_checked:PROC");
        Line("EXTERN smile_save_data_checked:PROC");
        Line("EXTERN smile_media_shutdown:PROC");
        Line("EXTERN smile_media_configure:PROC");
        foreach (var site in _debugSites)
            Line($"EXTERN {site.HelperName}:PROC");
        Line();
        Line(".data");
        Line("smile_staged_cleanup_head QWORD 0");
        Line("smile_active_frame_cleanup_head QWORD 0");
        EmitStorage(_analysis.SemanticModel.Symbols.Values);
        foreach (var temporary in _temporaries.Where(temporary => temporary.Kind == MasmStorageKind.Static))
            Line(temporary.Size == 8 ? $"{temporary.Name} QWORD 0" :
                $"{temporary.Name} QWORD {(temporary.Size / 8).ToString(CultureInfo.InvariantCulture)} DUP(0)");
        foreach (var literal in _textLiterals.Values.Concat(_constantTextLiterals.Values)
                     .Concat(_optionalDefaultTextLiterals.Values))
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
        if (_rememberWindowPlacement)
        {
            Line("    mov rcx, 1");
            CallAligned("smile_window_persistence_configure");
        }
        if (_responsiveWindow)
        {
            Line("    mov rcx, 1");
            CallAligned("smile_window_responsive_configure");
        }
        _currentSource = _analysis.BoundSyntaxTree.Source;
        EmitStatements(_analysis.BoundSyntaxTree.Root.Statements);
        CallAligned("smile_cleanup_staged_arguments");
        EmitProgramCleanup();
        CallAligned("smile_class_lifetime_report");
        CallAligned("smile_image_lifetime_report");
        CallAligned("smile_media_shutdown");
        CallAligned("smile_text_lifetime_report");
        if (_usesMusic) CallAligned("smile_music_shutdown");
        Line("    xor ecx, ecx");
        CallAligned("ExitProcess");
        Line("main ENDP");

        foreach (var routine in OrderedRoutines())
            EmitRoutine(routine);

        EmitStagedCleanupHelper();
        EmitActiveFrameCleanupHelper();

        foreach (var record in OrderedRecordTypes())
            EmitRecordHelpers(record);

        foreach (var classType in OrderedClassTypes())
            EmitClassFinalizer(classType);

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
                var site = new MasmDebugSite(_debugSites.Count, _currentSource, statement,
                    GetDebugVariables());
                _debugSites.Add(site);
                _debugSitesByStatement[statement] = site;
            }

            switch (statement)
            {
                case ConstStatementSyntax constant:
                    CollectExpression(constant.Expression);
                    break;
                case AssignmentStatementSyntax assignment:
                    CollectExpression(assignment.Target.Location);
                    CollectExpression(assignment.Expression);
                    CollectBoundCall(assignment);
                    if (_analysis.SemanticModel.GetType(assignment.Target.Location) is ClassTypeSymbol classType)
                    {
                        _classAssignmentValues[assignment] = CreateTemporary("class_assignment", classType);
                        _classAssignmentRegistrations[assignment] = CreateTemporary(
                            "class_assignment_cleanup", SmileType.Number, 24);
                    }
                    break;
                case DimStatementSyntax dim:
                    foreach (var size in dim.Sizes)
                        CollectExpression(size);
                    CollectExpression(dim.NewInitializer);
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
                case WithStatementSyntax withStatement:
                    CollectExpression(withStatement.Target);
                    if (_analysis.SemanticModel.TryGetWithTarget(withStatement, out var withBinding) &&
                        withBinding.TargetType is ClassTypeSymbol withClass)
                    {
                        _withLocations[withStatement] = CreateTemporary("with_reference", withClass);
                        _withRegistrations[withStatement] = CreateTemporary(
                            "with_reference_cleanup", SmileType.Number, 24);
                    }
                    else
                        _withLocations[withStatement] = CreateTemporary("with_location", SmileType.Number);
                    Collect(withStatement.Statements);
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
                case RoutineDeclarationSyntax:
                    break;
                case CallStatementSyntax call:
                    foreach (var argument in call.Arguments)
                        CollectExpression(argument.Expression);
                    CollectBoundCall(call);
                    break;
                case MemberCallStatementSyntax call:
                    CollectExpression(call.Receiver);
                    foreach (var argument in call.Arguments)
                        CollectExpression(argument.Expression);
                    CollectBoundCall(call);
                    break;
                case LeadingMemberCallStatementSyntax call:
                    foreach (var argument in call.Arguments)
                        CollectExpression(argument.Expression);
                    CollectBoundCall(call);
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
                    CollectExpression(image.Target.Location);
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
                    CollectExpression(textFileLoad.Path);
                    break;
                case DataLoadStatementSyntax data:
                    if (data.StatusTarget != null) _dataLoadCounts[data] = CreateTemporary("data_count", SmileType.Number);
                    CollectExpression(data.Key);
                    CollectExpression(data.CountTarget.Location);
                    CollectExpression(data.StatusTarget?.Location);
                    break;
                case DataSaveStatementSyntax data:
                    CollectExpression(data.Count);
                    CollectExpression(data.Key);
                    CollectExpression(data.StatusTarget?.Location);
                    break;
                case SaveStatementSyntax save:
                    CollectTextToken(save.Key);
                    break;
            }
        }
    }

    private MasmTemporaryStorage CreateTemporary(string prefix, SmileType type, int sizeOverride = 0)
    {
        var storage = new MasmTemporaryStorage($"{prefix}_{_temporaries.Count}", type, _collectRoutine,
            sizeOverride);
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
            case IndexedExpressionSyntax indexed:
                CollectExpression(indexed.Receiver);
                foreach (var index in indexed.Indices)
                    CollectExpression(index);
                CollectClassLocationOwner(indexed);
                break;
            case FieldAccessExpressionSyntax field:
                CollectExpression(field.Receiver);
                CollectBoundCall(field);
                CollectRecordCallResult(field);
                CollectClassLocationOwner(field);
                break;
            case LeadingMemberAccessExpressionSyntax leading:
                CollectBoundCall(leading);
                CollectRecordCallResult(leading);
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
            case IdentityExpressionSyntax identity:
            {
                CollectExpression(identity.Left);
                CollectExpression(identity.Right);
                var leftType = _analysis.SemanticModel.GetType(identity.Left);
                var rightType = _analysis.SemanticModel.GetType(identity.Right);
                var identityType = leftType as ClassTypeSymbol ?? (ClassTypeSymbol)rightType;
                var left = CreateTemporary("identity_left", identityType);
                var right = CreateTemporary("identity_right", identityType);
                _identityValues[identity] = (left, right);
                _identityRegistrations[identity] = (
                    leftType.IsClass ? CreateTemporary("identity_left_cleanup", SmileType.Number, 24) : null,
                    rightType.IsClass ? CreateTemporary("identity_right_cleanup", SmileType.Number, 24) : null);
                break;
            }
            case NewExpressionSyntax creation:
                foreach (var argument in creation.Arguments)
                    CollectExpression(argument.Expression);
                CollectBoundCall(creation);
                if (_analysis.SemanticModel.GetType(creation) is ClassTypeSymbol classType)
                {
                    _constructorResults[creation] = CreateTemporary("constructor_result", classType);
                    _constructorRegistrations[creation] = CreateTemporary(
                        "constructor_cleanup", SmileType.Number, 24);
                }
                break;
            case CallExpressionSyntax call:
                _usesTimer |= call.Identifier.Kind == SyntaxKind.TimerKeyword;
                _usesGameClosed |= call.Identifier.Kind == SyntaxKind.GameClosedKeyword;
                _usesKeyHeld |= call.Identifier.Kind == SyntaxKind.KeyHeldKeyword;
                _usesKeyEventHeld |= call.Identifier.Kind == SyntaxKind.KeyEventHeldKeyword;
                foreach (var argument in call.Arguments)
                    CollectExpression(argument.Expression);
                CollectBoundCall(call);
                CollectRecordCallResult(call);
                break;
            case MemberInvocationExpressionSyntax call:
                CollectExpression(call.Receiver);
                foreach (var argument in call.Arguments)
                    CollectExpression(argument.Expression);
                CollectBoundCall(call);
                CollectRecordCallResult(call);
                break;
            case LeadingMemberInvocationExpressionSyntax call:
                foreach (var argument in call.Arguments)
                    CollectExpression(argument.Expression);
                CollectBoundCall(call);
                CollectRecordCallResult(call);
                break;
        }
    }

    private void CollectClassLocationOwner(ExpressionSyntax expression)
    {
        if (_classLocationOwners.ContainsKey(expression) ||
            !_analysis.SemanticModel.TryGetClassLocationOwner(expression, out var owner))
            return;
        _classLocationOwners[expression] = CreateTemporary("class_location_owner", owner.RootType);
        _classLocationOwnerRegistrations[expression] = CreateTemporary(
            "class_location_cleanup", SmileType.Number, 24);
    }

    private void CollectRecordCallResult(ExpressionSyntax expression)
    {
        if (!_recordCallResults.ContainsKey(expression) &&
            _analysis.SemanticModel.TryGetBoundCall(expression, out var call) &&
            call.Routine.ReturnType is RecordTypeSymbol record)
            _recordCallResults[expression] = CreateTemporary("record_result", record);
    }

    private void CollectBoundCall(SyntaxNode callSyntax)
    {
        if (!_analysis.SemanticModel.TryGetBoundCall(callSyntax, out var call))
            return;
        if (call.InstanceReceiver != null && !_callReceiverTemporaries.ContainsKey(callSyntax))
        {
            var receiverType = call.InstanceReceiver.ContainingType;
            _callReceiverTemporaries[callSyntax] = CreateTemporary("call_receiver",
                receiverType is ClassTypeSymbol ? receiverType : SmileType.Number);
            if (receiverType is ClassTypeSymbol)
                _callReceiverRegistrations[callSyntax] = CreateTemporary(
                    "call_receiver_cleanup", SmileType.Number, 24);
        }
        if (call.ImplicitValue != null && !_implicitValueTemporaries.ContainsKey(callSyntax))
        {
            var type = call.Routine.SetterValue?.Type ?? _analysis.SemanticModel.GetType(call.ImplicitValue);
            _implicitValueTemporaries[callSyntax] = CreateTemporary("call_value", type);
            if (type.RequiresCleanup)
                _implicitValueRegistrations[callSyntax] = CreateTemporary("call_value_cleanup", SmileType.Number, 24);
        }
        foreach (var argument in call.SourceArguments)
        {
            if (_callArgumentTemporaries.ContainsKey(argument))
                continue;
            var type = argument.Parameter.ParameterMode == ParameterPassingMode.ByVal &&
                       (argument.Parameter.Type is RecordTypeSymbol || argument.Parameter.Type.IsClass ||
                        argument.Parameter.Type == SmileType.Text || argument.Parameter.Type == SmileType.Image)
                ? argument.Parameter.Type : SmileType.Number;
            _callArgumentTemporaries[argument] = CreateTemporary("call_argument", type);
            if (RequiresStagedCleanup(argument))
                _callArgumentRegistrations[argument] = CreateTemporary("call_cleanup", SmileType.Number, 24);
        }
    }

    private static bool RequiresStagedCleanup(BoundCallArgument argument) =>
        argument.Parameter.ParameterMode == ParameterPassingMode.ByVal && argument.Parameter.Type.RequiresCleanup;

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
        foreach (var routine in OrderedRoutines())
        {
            _routineLabels[routine] = "routine_" + _routineLabels.Count;
            foreach (var parameter in routine.Parameters.Where(parameter => parameter.IsOptional &&
                         parameter.HasDefaultValue && parameter.Type == SmileType.Text))
                _optionalDefaultTextLiterals[parameter] = new TextLiteral(
                    $"optional_text_{_optionalDefaultTextLiterals.Count}",
                    Encoding.UTF8.GetBytes((string)parameter.DefaultValue));
        }
        var recordId = 0;
        foreach (var record in OrderedRecordTypes())
            _recordHelperLabels[record] = $"record_{recordId++}_{SafeName(record.RuntimeIdentity)}";
        var classId = 0;
        foreach (var classType in OrderedClassTypes())
            _classFinalizerLabels[classType] = $"class_{classId++}_{SafeName(classType.RuntimeIdentity)}_finalize";
    }

    private void BuildFrameLayouts()
    {
        foreach (var routine in OrderedRoutines())
        {
            var localOffsets = new Dictionary<VariableSymbol, int>();
            var offset = 0;
            foreach (var symbol in routine.LocalSymbols.Values.Where(symbol => !symbol.IsConstant)
                         .OrderBy(symbol => symbol.DeclarationSpan.Start))
            {
                offset += Math.Max(1, symbol.ArraySize) * Math.Max(8, symbol.Type.Size);
                localOffsets[symbol] = offset;
            }

            var temporaries = _temporaries.Where(temporary => ReferenceEquals(temporary.Owner, routine))
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
        var argumentIndex = 0;
        if (routine.ReturnType is RecordTypeSymbol)
        {
            Line("    mov QWORD PTR [rbp-" + _currentFrame.ReturnOffset + "], rcx");
            argumentIndex++;
        }
        if (routine.Receiver != null)
        {
            EmitIncomingArgument(routine.Receiver, argumentIndex);
            argumentIndex++;
        }
        if (routine.SetterValue != null)
        {
            EmitIncomingArgument(routine.SetterValue, argumentIndex);
            argumentIndex++;
        }
        foreach (var parameter in routine.Parameters)
            EmitIncomingArgument(parameter, argumentIndex++);
        foreach (var parameter in routine.Parameters.Cast<VariableSymbol>()
                     .Concat(routine.SetterValue == null
                         ? Array.Empty<VariableSymbol>() : new VariableSymbol[] { routine.SetterValue })
                     .Where(parameter => parameter.Type is RecordTypeSymbol &&
                                         parameter.ParameterMode != ParameterPassingMode.ByRef))
        {
            var record = (RecordTypeSymbol)parameter.Type;
            var offset = _currentFrame.LocalOffsets[parameter];
            Line($"    mov rdx, QWORD PTR [rbp-{offset}]");
            Line($"    mov QWORD PTR [rbp-{offset}], 0");
            Line($"    lea rcx, [rbp-{offset}]");
            CallAligned(RecordCopy(record));
        }
        RegisterActiveFrame(routine);
        if (routine.ReturnType is not RecordTypeSymbol)
            Line($"    mov QWORD PTR [rbp-{_currentFrame.ReturnOffset}], 0");
        EmitStatements(routine.BodyStatements);
        if (!routine.IsFunction)
            Line("    xor eax, eax");
        if (routine.ReturnType is not RecordTypeSymbol)
            Line($"    mov QWORD PTR [rbp-{_currentFrame.ReturnOffset}], rax");
        Line($"{_returnLabel}:");
        foreach (var temporary in _currentFrame.Temporaries.Where(temporary => temporary.RequiresCleanup))
            EmitCleanup(new MasmCleanupAction(temporary));
        foreach (var symbol in routine.LocalSymbols.Values.Where(symbol => symbol.Type.RequiresCleanup &&
                     !symbol.Type.IsClass && symbol.ParameterMode != ParameterPassingMode.ByRef))
            EmitReleaseSymbol(symbol);
        foreach (var symbol in routine.LocalSymbols.Values.Where(IsOwnedClassLocal))
            EmitReleaseSymbol(symbol);
        UnregisterActiveFrame(routine);
        Line($"    mov rax, QWORD PTR [rbp-{_currentFrame.ReturnOffset}]");
        Line("    mov rsp, rbp");
        Line("    pop rbp");
        Line("    ret");
        Line($"{_routineLabels[routine]} ENDP");
        EmitActiveFrameCleanup(routine);
        _returnLabel = null;
        _currentRoutine = null;
        _currentFrame = null;
        _activeCleanups.Clear();
    }

    private void EmitIncomingArgument(VariableSymbol symbol, int argumentIndex)
    {
        var source = argumentIndex switch
        {
            0 => "rcx", 1 => "rdx", 2 => "r8", 3 => "r9",
            _ => $"QWORD PTR [rbp+{48 + (argumentIndex - 4) * 8}]"
        };
        if (argumentIndex >= 4)
            Line($"    mov rax, {source}");
        var sourceRegister = argumentIndex >= 4 ? "rax" : source;
        Line($"    mov QWORD PTR [rbp-{_currentFrame!.LocalOffsets[symbol]}], {sourceRegister}");
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
            EmitDebugSiteCall(_debugSitesByStatement[statement]);
        }

        switch (statement)
        {
            case ConstStatementSyntax:
            case TypeDeclarationSyntax:
            case ClassDeclarationSyntax:
            case EnumDeclarationSyntax:
            case RoutineDeclarationSyntax:
                break;
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
            case WithStatementSyntax withStatement:
                EmitWith(withStatement);
                break;
            case ForStatementSyntax forStatement:
                EmitFor(forStatement);
                break;
            case DoStatementSyntax doStatement:
                EmitDo(doStatement);
                break;
            case CallStatementSyntax call:
                EmitRoutineCall(call);
                break;
            case MemberCallStatementSyntax call:
                EmitRoutineCall(call);
                break;
            case LeadingMemberCallStatementSyntax call:
                EmitRoutineCall(call);
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
                    ReleaseClassLocationOwner(returnStatement.Expression!);
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
                EmitTermination(0);
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
                EmitExpression(textFileLoad.Path);
                PushRax();
                var destination = Resolve(textFileLoad.Destination.Text);
                EmitAddress(destination);
                PushRax();
                Line($"    mov rax, {destination.ArraySize.ToString(CultureInfo.InvariantCulture)}");
                PushRax();
                EmitNativeCall("smile_load_text_file", 3);
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
                if (dataLoad.StatusTarget != null)
                {
                    EmitTemporaryAddress(_dataLoadCounts[dataLoad], "rax");
                    PushRax();
                    EmitNativeCall("smile_load_data_checked", 4);
                    PushRax();
                    EmitTargetAddress(dataLoad.CountTarget);
                    Line("    mov rcx, rax");
                    Line($"    mov rax, {TemporaryMemory(_dataLoadCounts[dataLoad])}");
                    Line("    mov QWORD PTR [rcx], rax");
                    PopRax();
                }
                else EmitNativeCall("smile_load_data_value", 3);
                PushRax();
                EmitTargetAddress(dataLoad.StatusTarget ?? dataLoad.CountTarget);
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
                EmitNativeCall(dataSave.StatusTarget == null ? "smile_save_data_value" : "smile_save_data_checked", 4);
                if (dataSave.StatusTarget != null)
                {
                    PushRax();
                    EmitTargetAddress(dataSave.StatusTarget);
                    Line("    mov rcx, rax");
                    PopRax();
                    Line("    mov QWORD PTR [rcx], rax");
                }
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
            GraphicsOperation.FillRectangleOpacity => "smile_fill_rectangle_opacity",
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
        UpdateActiveFrameClipCount(1);
        EmitStatements(clip.Statements);
        CallAligned("smile_clip_pop");
        UpdateActiveFrameClipCount(-1);
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
        if (_analysis.SemanticModel.TryGetBoundCall(assignment, out var propertyCall) &&
            propertyCall.Routine.SymbolKind == RoutineSymbolKind.PropertySet)
        {
            EmitRoutineCall(assignment);
            return;
        }
        var targetType = _analysis.SemanticModel.GetType(assignment.Target.Location);
        if (targetType is ClassTypeSymbol)
        {
            var value = _classAssignmentValues[assignment];
            var registration = _classAssignmentRegistrations[assignment];
            EmitExpression(assignment.Expression);
            Line($"    mov {TemporaryMemory(value)}, rax");
            RegisterStagedCleanup(value, registration, targetType);
            EmitTargetAddress(assignment.Target);
            Line("    mov r10, rax");
            Line($"    mov r11, {TemporaryMemory(value)}");
            Line($"    mov {TemporaryMemory(value)}, 0");
            UnregisterStagedCleanup(registration);
            Line("    mov rcx, r10");
            Line("    mov rdx, r11");
            CallAligned("smile_class_move_assign");
            ReleaseClassLocationOwner(assignment.Target.Location);
            return;
        }
        EmitExpression(assignment.Expression);
        PushRax();
        EmitTargetAddress(assignment.Target);
        Line("    mov rcx, rax");
        PopRax();
        if (targetType is RecordTypeSymbol targetRecord)
        {
            Line("    mov rdx, rax");
            CallAligned(RecordCopy(targetRecord));
            ReleaseClassLocationOwner(assignment.Target.Location);
            ReleaseClassLocationOwner(assignment.Expression);
            return;
        }
        if (targetType == SmileType.Text || targetType == SmileType.Image)
        {
            Line("    mov rdx, rax");
            CallAligned(targetType == SmileType.Text ? "smile_text_move_assign" : "smile_image_move_assign");
        }
        else
            Line("    mov QWORD PTR [rcx], rax");
        ReleaseClassLocationOwner(assignment.Target.Location);
    }

    private void EmitClassInitializer(ClassInitializerBinding initializer)
    {
        EmitExpression(initializer.Initializer);
        Line("    mov rdx, rax");
        EmitAddress(initializer.Target);
        Line("    mov rcx, rax");
        CallAligned("smile_class_move_assign");
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

    private void EmitTargetAddress(AssignmentTargetSyntax target)
    {
        EmitWritableAddress(target.Location);
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

    private void EmitWith(WithStatementSyntax statement)
    {
        var storage = _withLocations[statement];
        if (_analysis.SemanticModel.TryGetWithTarget(statement, out var binding) &&
            binding.TargetType is ClassTypeSymbol)
        {
            EmitExpression(statement.Target);
            EmitRequireClassReference();
            Line($"    mov {TemporaryMemory(storage)}, rax");
            var registration = _withRegistrations[statement];
            RegisterStagedCleanup(storage, registration, binding.TargetType);
            var cleanup = new MasmCleanupAction(storage, registration);
            _activeCleanups.Add(cleanup);
            EmitStatements(statement.Statements);
            EmitCleanup(cleanup);
            _activeCleanups.RemoveAt(_activeCleanups.Count - 1);
            return;
        }

        EmitWritableAddress(statement.Target);
        Line($"    mov {TemporaryMemory(storage)}, rax");
        MasmCleanupAction? ownerCleanup = null;
        if (_classLocationOwners.TryGetValue(statement.Target, out var owner))
        {
            ownerCleanup = new MasmCleanupAction(owner,
                _classLocationOwnerRegistrations[statement.Target]);
            _activeCleanups.Add(ownerCleanup);
        }
        EmitStatements(statement.Statements);
        if (ownerCleanup != null)
        {
            EmitCleanup(ownerCleanup);
            _activeCleanups.RemoveAt(_activeCleanups.Count - 1);
        }
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
            case NothingExpressionSyntax:
                Line("    xor eax, eax");
                break;
            case NewExpressionSyntax creation:
                EmitRoutineCall(creation);
                break;
            case NameExpressionSyntax name:
                var symbol = Resolve(name.Identifier.Text);
                EmitLoad(symbol);
                break;
            case MeExpressionSyntax:
                if (_currentRoutine?.Receiver == null)
                    throw new InvalidOperationException("Me does not have a bound instance receiver.");
                EmitLoad(_currentRoutine.Receiver);
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
            case IndexedExpressionSyntax indexed:
                EmitWritableAddress(indexed);
                if (_analysis.SemanticModel.GetType(indexed) is not RecordTypeSymbol)
                {
                    Line("    mov rax, QWORD PTR [rax]");
                    var indexedType = _analysis.SemanticModel.GetType(indexed);
                    if (indexedType == SmileType.Text || indexedType == SmileType.Image || indexedType.IsClass)
                    {
                        Line("    mov rcx, rax");
                        CallAligned(indexedType == SmileType.Text ? "smile_text_retain" :
                            indexedType == SmileType.Image ? "smile_image_retain" : "smile_class_retain");
                    }
                    ReleaseClassLocationOwner(indexed);
                }
                break;
            case FieldAccessExpressionSyntax field:
                if (_analysis.SemanticModel.TryGetEnumMember(field, out var enumMember))
                {
                    Line($"    mov rax, {QwordImmediate(enumMember.Value)}");
                    break;
                }
                if (_analysis.SemanticModel.TryGetBoundCall(field, out _))
                {
                    EmitRoutineCall(field,
                        _recordCallResults.TryGetValue(field, out var fieldResult) ? fieldResult : null);
                    break;
                }
                EmitWritableAddress(field);
                if (_analysis.SemanticModel.GetType(field) is not RecordTypeSymbol)
                {
                    Line("    mov rax, QWORD PTR [rax]");
                    if (_analysis.SemanticModel.GetType(field) == SmileType.Text ||
                        _analysis.SemanticModel.GetType(field) == SmileType.Image ||
                        _analysis.SemanticModel.GetType(field).IsClass)
                    {
                        Line("    mov rcx, rax");
                        CallAligned(_analysis.SemanticModel.GetType(field) == SmileType.Text
                            ? "smile_text_retain" : _analysis.SemanticModel.GetType(field) == SmileType.Image
                                ? "smile_image_retain" : "smile_class_retain");
                    }
                    ReleaseClassLocationOwner(field);
                }
                break;
            case LeadingMemberAccessExpressionSyntax leading:
                if (_analysis.SemanticModel.TryGetBoundCall(leading, out _))
                {
                    EmitRoutineCall(leading,
                        _recordCallResults.TryGetValue(leading, out var leadingResult) ? leadingResult : null);
                    break;
                }
                EmitWritableAddress(leading);
                if (_analysis.SemanticModel.GetType(leading) is not RecordTypeSymbol)
                {
                    Line("    mov rax, QWORD PTR [rax]");
                    if (_analysis.SemanticModel.GetType(leading) == SmileType.Text ||
                        _analysis.SemanticModel.GetType(leading) == SmileType.Image ||
                        _analysis.SemanticModel.GetType(leading).IsClass)
                    {
                        Line("    mov rcx, rax");
                        CallAligned(_analysis.SemanticModel.GetType(leading) == SmileType.Text
                            ? "smile_text_retain" : _analysis.SemanticModel.GetType(leading) == SmileType.Image
                                ? "smile_image_retain" : "smile_class_retain");
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
            case IdentityExpressionSyntax identity:
                EmitIdentity(identity);
                break;
            case CallExpressionSyntax call:
                EmitCallExpression(call);
                break;
            case MemberInvocationExpressionSyntax call:
                EmitRoutineCall(call,
                    _recordCallResults.TryGetValue(call, out var memberResult) ? memberResult : null);
                break;
            case LeadingMemberInvocationExpressionSyntax call:
                EmitRoutineCall(call,
                    _recordCallResults.TryGetValue(call, out var leadingMemberResult) ? leadingMemberResult : null);
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
                EmitExpression(call.Arguments[0].Expression);
                var positive = NewLabel("abs_positive");
                Line("    cmp rax, 0");
                Line($"    jge {positive}");
                Line("    neg rax");
                Line($"{positive}:");
                break;
            case SyntaxKind.MinKeyword:
            case SyntaxKind.MaxKeyword:
                EmitExpression(call.Arguments[0].Expression);
                PushRax();
                EmitExpression(call.Arguments[1].Expression);
                Line("    mov rcx, rax");
                PopRax();
                Line("    cmp rax, rcx");
                Line(call.Identifier.Kind == SyntaxKind.MinKeyword ? "    cmovg rax, rcx" : "    cmovl rax, rcx");
                break;
            case SyntaxKind.RgbKeyword:
                EmitExpression(call.Arguments[0].Expression);
                PushRax();
                EmitExpression(call.Arguments[1].Expression);
                PushRax();
                EmitExpression(call.Arguments[2].Expression);
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
            case SyntaxKind.WindowWidthKeyword:
                CallAligned("smile_window_width");
                break;
            case SyntaxKind.WindowHeightKeyword:
                CallAligned("smile_window_height");
                break;
            case SyntaxKind.WindowTitleKeyword:
                EmitExpression(call.Arguments[0].Expression);
                Line("    mov rcx, rax");
                CallAligned("smile_window_title");
                break;
            case SyntaxKind.WindowActivateKeyword:
                CallAligned("smile_window_activate");
                break;
            case SyntaxKind.FileRevealKeyword:
                EmitExpression(call.Arguments[0].Expression);
                Line("    mov rcx, rax");
                CallAligned("smile_file_reveal");
                break;
            case SyntaxKind.FileExportKeyword:
                foreach (var argument in call.Arguments)
                {
                    EmitExpression(argument.Expression);
                    PushRax();
                }
                EmitNativeCall("smile_file_export", 2);
                break;
            case SyntaxKind.FileImportKeyword:
                CallAligned("smile_file_import");
                break;
            case SyntaxKind.KeyHeldKeyword:
            case SyntaxKind.KeyEventHeldKeyword:
                EmitExpression(call.Arguments[0].Expression);
                Line("    mov rcx, rax");
                CallAligned(call.Identifier.Kind == SyntaxKind.KeyHeldKeyword
                    ? "smile_key_held" : "smile_key_event_held");
                break;
            case SyntaxKind.PointerXKeyword:
            case SyntaxKind.PointerYKeyword:
            case SyntaxKind.PointerDeltaXKeyword:
            case SyntaxKind.PointerDeltaYKeyword:
            case SyntaxKind.PointerWheelDeltaKeyword:
            case SyntaxKind.PointerWheelRemainderKeyword:
            case SyntaxKind.PointerInsideKeyword:
                CallAligned(call.Identifier.Kind switch
                {
                    SyntaxKind.PointerXKeyword => "smile_pointer_x",
                    SyntaxKind.PointerYKeyword => "smile_pointer_y",
                    SyntaxKind.PointerDeltaXKeyword => "smile_pointer_delta_x",
                    SyntaxKind.PointerDeltaYKeyword => "smile_pointer_delta_y",
                    SyntaxKind.PointerWheelDeltaKeyword => "smile_pointer_wheel_delta",
                    SyntaxKind.PointerWheelRemainderKeyword => "smile_pointer_wheel_remainder",
                    _ => "smile_pointer_inside"
                });
                break;
            case SyntaxKind.PointerHeldKeyword:
            case SyntaxKind.PointerPressedKeyword:
            case SyntaxKind.PointerReleasedKeyword:
                EmitExpression(call.Arguments[0].Expression);
                Line("    mov rcx, rax");
                CallAligned(call.Identifier.Kind switch
                {
                    SyntaxKind.PointerHeldKeyword => "smile_pointer_held",
                    SyntaxKind.PointerPressedKeyword => "smile_pointer_pressed",
                    _ => "smile_pointer_released"
                });
                break;
            case SyntaxKind.ImageWidthKeyword:
            case SyntaxKind.ImageHeightKeyword:
            case SyntaxKind.ImageLoadedKeyword:
                EmitExpression(call.Arguments[0].Expression);
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
                EmitExpression(call.Arguments[0].Expression);
                PushRax();
                EmitExpression(call.Arguments[1].Expression);
                PushRax();
                EmitNativeCall(call.Identifier.Kind == SyntaxKind.TextWidthKeyword
                    ? "smile_text_width_value" : "smile_text_height_value", 2);
                break;
            case SyntaxKind.TextLengthKeyword:
                EmitExpression(call.Arguments[0].Expression);
                Line("    mov rcx, rax");
                CallAligned("smile_text_scalar_length");
                break;
            case SyntaxKind.TextCodeAtKeyword:
                EmitExpression(call.Arguments[0].Expression);
                PushRax();
                EmitExpression(call.Arguments[1].Expression);
                PushRax();
                EmitNativeCall("smile_text_code_at", 2);
                break;
            case SyntaxKind.TextSliceKeyword:
                EmitExpression(call.Arguments[0].Expression);
                PushRax();
                EmitExpression(call.Arguments[1].Expression);
                PushRax();
                EmitExpression(call.Arguments[2].Expression);
                PushRax();
                EmitNativeCall("smile_text_slice", 3);
                break;
            case SyntaxKind.Renderer3DKeyword:
            case SyntaxKind.Renderer3DImageKeyword:
            case SyntaxKind.Renderer3DTextKeyword:
            case SyntaxKind.Renderer3DTextValueKeyword:
                foreach (var argument in call.Arguments)
                {
                    EmitExpression(argument.Expression);
                    PushRax();
                }
                EmitNativeCall(call.Identifier.Kind switch
                {
                    SyntaxKind.Renderer3DKeyword => "smile_renderer3d_command",
                    SyntaxKind.Renderer3DImageKeyword => "smile_renderer3d_image_command",
                    SyntaxKind.Renderer3DTextKeyword => "smile_renderer3d_text_command",
                    _ => "smile_renderer3d_text_value"
                }, call.Arguments.Count);
                break;
            default:
                EmitRoutineCall(call, _recordCallResults.TryGetValue(call, out var result) ? result : null);
                break;
        }
    }

    private void EmitRoutineCall(SyntaxNode callSyntax, MasmTemporaryStorage? recordResult = null)
    {
        if (!_analysis.SemanticModel.TryGetBoundCall(callSyntax, out var call))
            throw new InvalidOperationException("Unbound routine call.");

        if (call.EvaluateReceiverAfterImplicitValue)
        {
            CaptureImplicitValue(callSyntax, call);
            CaptureInstanceReceiver(callSyntax, call);
        }
        else
        {
            CaptureInstanceReceiver(callSyntax, call);
            CaptureImplicitValue(callSyntax, call);
        }
        foreach (var argument in call.SourceArguments)
        {
            var expression = argument.Expression!;
            var temporary = _callArgumentTemporaries[argument];
            if (argument.Parameter.ParameterMode == ParameterPassingMode.ByRef)
            {
                EmitWritableAddress(expression);
                Line($"    mov {TemporaryMemory(temporary)}, rax");
            }
            else if (argument.Parameter.Type is RecordTypeSymbol record)
            {
                EmitExpression(expression);
                Line("    mov rdx, rax");
                EmitTemporaryAddress(temporary, "rcx");
                CallAligned(RecordCopy(record));
                ClearConsumedRecordResults(expression);
                ReleaseClassLocationOwner(expression);
            }
            else
            {
                EmitExpression(expression);
                Line($"    mov {TemporaryMemory(temporary)}, rax");
            }
            if (RequiresStagedCleanup(argument))
                RegisterStagedCleanup(argument);
        }

        MasmTemporaryStorage? constructorResult = null;
        MasmTemporaryStorage? constructorRegistration = null;
        if (call.Routine.IsConstructor)
        {
            if (callSyntax is not NewExpressionSyntax creation ||
                call.Routine.ContainingType is not ClassTypeSymbol classType)
                throw new InvalidOperationException("A constructor call requires a bound New expression.");
            constructorResult = _constructorResults[creation];
            constructorRegistration = _constructorRegistrations[creation];
            Line($"    mov rcx, {classType.InstanceSize.ToString(CultureInfo.InvariantCulture)}");
            Line($"    lea rdx, {_classFinalizerLabels[classType]}");
            CallAligned("smile_class_allocate");
            EmitRequireClassAllocation();
            Line($"    mov {TemporaryMemory(constructorResult)}, rax");
            RegisterStagedCleanup(constructorResult, constructorRegistration, classType);
        }

        if (recordResult != null)
        {
            EmitTemporaryAddress(recordResult, "rax");
            PushRax();
        }
        if (constructorResult != null)
        {
            Line($"    mov rax, {TemporaryMemory(constructorResult)}");
            PushRax();
        }
        else if (call.InstanceReceiver != null)
        {
            Line($"    mov rax, {TemporaryMemory(_callReceiverTemporaries[callSyntax])}");
            PushRax();
        }
        if (call.ImplicitValue != null)
        {
            var value = _implicitValueTemporaries[callSyntax];
            if (value.Type is RecordTypeSymbol)
                EmitTemporaryAddress(value, "rax");
            else
                Line($"    mov rax, {TemporaryMemory(value)}");
            PushRax();
        }
        foreach (var argument in call.ParameterArguments)
        {
            if (argument.IsDefault)
                EmitOptionalDefault(argument.Parameter);
            else if (argument.Parameter.ParameterMode == ParameterPassingMode.ByVal &&
                     argument.Parameter.Type is RecordTypeSymbol)
                EmitTemporaryAddress(_callArgumentTemporaries[argument], "rax");
            else
                Line($"    mov rax, {TemporaryMemory(_callArgumentTemporaries[argument])}");
            PushRax();
        }
        foreach (var argument in call.SourceArguments.Reverse().Where(argument =>
                     RequiresStagedCleanup(argument) && argument.Parameter.Type is not RecordTypeSymbol))
        {
            Line($"    mov {TemporaryMemory(_callArgumentTemporaries[argument])}, 0");
        }
        if (call.ImplicitValue != null && _implicitValueRegistrations.ContainsKey(callSyntax) &&
            _implicitValueTemporaries[callSyntax].Type is not RecordTypeSymbol)
            Line($"    mov {TemporaryMemory(_implicitValueTemporaries[callSyntax])}, 0");
        var abiArgumentCount = call.ParameterArguments.Count +
                               (recordResult == null ? 0 : 1) +
                               (call.InstanceReceiver == null && constructorResult == null ? 0 : 1) +
                               (call.ImplicitValue == null ? 0 : 1);
        EmitNativeCall(_routineLabels[call.Routine], abiArgumentCount);
        var cleanupCaptures = call.SourceArguments.Where(argument =>
            RequiresStagedCleanup(argument) || argument.Parameter.ParameterMode == ParameterPassingMode.ByVal &&
            argument.Parameter.Type is RecordTypeSymbol).ToArray();
        var cleanupImplicitValue = call.ImplicitValue != null &&
                                   (_implicitValueRegistrations.ContainsKey(callSyntax) ||
                                    _implicitValueTemporaries[callSyntax].Type is RecordTypeSymbol);
        var cleanupReceiver = call.InstanceReceiver?.ContainingType is ClassTypeSymbol;
        var releaseLocationOwner = call.InstanceReceiver?.ContainingType is RecordTypeSymbol &&
                                   call.InstanceReceiver.Expression != null &&
                                   _classLocationOwners.ContainsKey(call.InstanceReceiver.Expression);
        var byRefLocationOwners = call.SourceArguments.Where(argument =>
            argument.Parameter.ParameterMode == ParameterPassingMode.ByRef && argument.Expression != null &&
            _classLocationOwners.ContainsKey(argument.Expression)).ToArray();
        if (cleanupCaptures.Length != 0 || cleanupImplicitValue || cleanupReceiver ||
            releaseLocationOwner || byRefLocationOwners.Length != 0 || constructorResult != null)
        {
            PushRax();
            if (constructorResult != null)
                UnregisterStagedCleanup(constructorRegistration!);
            foreach (var argument in call.SourceArguments.Reverse())
            {
                if (RequiresStagedCleanup(argument))
                    UnregisterStagedCleanup(argument);
                if (argument.Parameter.ParameterMode == ParameterPassingMode.ByVal &&
                    argument.Parameter.Type is RecordTypeSymbol record)
                {
                    EmitTemporaryAddress(_callArgumentTemporaries[argument], "rcx");
                    CallAligned(RecordClear(record));
                }
                if (argument.Parameter.ParameterMode == ParameterPassingMode.ByRef &&
                    argument.Expression != null && _classLocationOwners.ContainsKey(argument.Expression))
                    ReleaseClassLocationOwner(argument.Expression);
            }

            void CleanupImplicitValue()
            {
                if (!cleanupImplicitValue)
                    return;
                var value = _implicitValueTemporaries[callSyntax];
                if (_implicitValueRegistrations.TryGetValue(callSyntax, out var registration))
                    UnregisterStagedCleanup(registration);
                if (value.Type is RecordTypeSymbol record)
                {
                    EmitTemporaryAddress(value, "rcx");
                    CallAligned(RecordClear(record));
                }
            }

            void CleanupReceiver()
            {
                if (cleanupReceiver)
                {
                    var receiver = _callReceiverTemporaries[callSyntax];
                    var registration = _callReceiverRegistrations[callSyntax];
                    EmitCleanup(new MasmCleanupAction(receiver, registration));
                }
                if (releaseLocationOwner)
                    ReleaseClassLocationOwner(call.InstanceReceiver!.Expression!);
            }

            if (call.EvaluateReceiverAfterImplicitValue)
            {
                CleanupReceiver();
                CleanupImplicitValue();
            }
            else
            {
                CleanupImplicitValue();
                CleanupReceiver();
            }
            PopRax();
        }
        if (constructorResult != null)
        {
            Line($"    mov rax, {TemporaryMemory(constructorResult)}");
            Line($"    mov {TemporaryMemory(constructorResult)}, 0");
        }
        if (recordResult != null)
            EmitTemporaryAddress(recordResult, "rax");
    }

    private void CaptureInstanceReceiver(SyntaxNode callSyntax, BoundCall call)
    {
        if (call.InstanceReceiver == null)
            return;
        var receiver = call.InstanceReceiver;
        if (receiver.Kind == BoundInstanceReceiverKind.WithTarget)
        {
            if (receiver.WithTarget == null || !_withLocations.TryGetValue(receiver.WithTarget, out var withLocation))
                throw new InvalidOperationException("Bound With receiver does not have a captured native location.");
            Line($"    mov rax, {TemporaryMemory(withLocation)}");
            if (receiver.ContainingType is ClassTypeSymbol)
            {
                Line("    mov rcx, rax");
                CallAligned("smile_class_retain");
            }
        }
        else if (receiver.Expression != null)
        {
            if (receiver.ContainingType is ClassTypeSymbol)
                EmitExpression(receiver.Expression);
            else
                EmitWritableAddress(receiver.Expression);
        }
        else
            throw new InvalidOperationException("Bound instance receiver does not have a native location.");
        var temporary = _callReceiverTemporaries[callSyntax];
        Line($"    mov {TemporaryMemory(temporary)}, rax");
        if (receiver.ContainingType is ClassTypeSymbol)
        {
            EmitRequireClassReference();
            RegisterStagedCleanup(temporary, _callReceiverRegistrations[callSyntax], receiver.ContainingType);
        }
    }

    private void CaptureImplicitValue(SyntaxNode callSyntax, BoundCall call)
    {
        if (call.ImplicitValue == null)
            return;
        var temporary = _implicitValueTemporaries[callSyntax];
        if (temporary.Type is RecordTypeSymbol record)
        {
            EmitExpression(call.ImplicitValue);
            Line("    mov rdx, rax");
            EmitTemporaryAddress(temporary, "rcx");
            CallAligned(RecordCopy(record));
            ClearConsumedRecordResults(call.ImplicitValue);
            ReleaseClassLocationOwner(call.ImplicitValue);
        }
        else
        {
            EmitExpression(call.ImplicitValue);
            Line($"    mov {TemporaryMemory(temporary)}, rax");
        }
        if (_implicitValueRegistrations.TryGetValue(callSyntax, out var registration))
            RegisterStagedCleanup(temporary, registration, temporary.Type);
    }

    private void ClearConsumedRecordResults(ExpressionSyntax expression)
    {
        if (_recordCallResults.TryGetValue(expression, out var result) &&
            result.Type is RecordTypeSymbol resultRecord)
        {
            EmitTemporaryAddress(result, "rcx");
            CallAligned(RecordClear(resultRecord));
            return;
        }
        switch (expression)
        {
            case ParenthesizedExpressionSyntax parenthesized:
                ClearConsumedRecordResults(parenthesized.Expression);
                break;
            case FieldAccessExpressionSyntax field:
                ClearConsumedRecordResults(field.Receiver);
                break;
            case MemberInvocationExpressionSyntax member:
                ClearConsumedRecordResults(member.Receiver);
                break;
        }
    }

    private void EmitOptionalDefault(ParameterSymbol parameter)
    {
        if (parameter.Type == SmileType.Text)
        {
            Line($"    lea rcx, {_optionalDefaultTextLiterals[parameter].Label}");
            CallAligned("smile_text_retain");
            return;
        }
        var value = parameter.DefaultValue switch
        {
            long number => number,
            bool boolean => boolean ? 1L : 0L,
            _ => 0L
        };
        Line($"    mov rax, {QwordImmediate(value)}");
    }

    private void RegisterStagedCleanup(BoundCallArgument argument)
    {
        RegisterStagedCleanup(_callArgumentTemporaries[argument],
            _callArgumentRegistrations[argument], argument.Parameter.Type);
    }

    private void RegisterStagedCleanup(MasmTemporaryStorage temporary,
        MasmTemporaryStorage registration, SmileType type)
    {
        EmitTemporaryAddress(registration, "rax");
        Line("    mov rdx, QWORD PTR [smile_staged_cleanup_head]");
        Line("    mov QWORD PTR [rax], rdx");
        EmitTemporaryAddress(temporary, "rdx");
        Line("    mov QWORD PTR [rax+8], rdx");
        var cleanup = CleanupRoutine(type);
        Line($"    mov rdx, OFFSET {cleanup}");
        Line("    mov QWORD PTR [rax+16], rdx");
        Line("    mov QWORD PTR [smile_staged_cleanup_head], rax");
    }

    private void UnregisterStagedCleanup(BoundCallArgument argument)
    {
        UnregisterStagedCleanup(_callArgumentRegistrations[argument]);
    }

    private void UnregisterStagedCleanup(MasmTemporaryStorage registration)
    {
        EmitTemporaryAddress(registration, "rax");
        Line("    mov rdx, QWORD PTR [rax]");
        Line("    mov QWORD PTR [smile_staged_cleanup_head], rdx");
        Line("    mov QWORD PTR [rax], 0");
        Line("    mov QWORD PTR [rax+8], 0");
        Line("    mov QWORD PTR [rax+16], 0");
    }

    private void RegisterActiveFrame(RoutineSymbol routine)
    {
        var registration = _activeFrameRegistrations[routine];
        EmitTemporaryAddress(registration, "rax");
        Line("    mov rdx, QWORD PTR [smile_active_frame_cleanup_head]");
        Line("    mov QWORD PTR [rax], rdx");
        Line("    mov QWORD PTR [rax+8], rbp");
        Line($"    mov rdx, OFFSET {ActiveFrameCleanupLabel(routine)}");
        Line("    mov QWORD PTR [rax+16], rdx");
        Line("    mov QWORD PTR [rax+24], 0");
        Line("    mov QWORD PTR [smile_active_frame_cleanup_head], rax");
    }

    private void UnregisterActiveFrame(RoutineSymbol routine)
    {
        var registration = _activeFrameRegistrations[routine];
        EmitTemporaryAddress(registration, "rax");
        Line("    mov rdx, QWORD PTR [rax]");
        Line("    mov QWORD PTR [smile_active_frame_cleanup_head], rdx");
        for (var offset = 0; offset < registration.Size; offset += 8)
            Line($"    mov QWORD PTR [rax{Offset(offset)}], 0");
    }

    private void UpdateActiveFrameClipCount(int delta)
    {
        if (_currentRoutine == null)
            return;
        var registration = _activeFrameRegistrations[_currentRoutine];
        var operation = delta > 0 ? "add" : "sub";
        Line($"    {operation} QWORD PTR [rbp-{registration.FrameOffset - 24}], 1");
    }

    private void EmitWritableAddress(ExpressionSyntax expression)
    {
        if (_classLocationOwners.TryGetValue(expression, out var ownerStorage) &&
            _analysis.SemanticModel.TryGetClassLocationOwner(expression, out var ownerBinding))
        {
            EmitExpression(ownerBinding.RootExpression);
            EmitRequireClassReference();
            Line($"    mov {TemporaryMemory(ownerStorage)}, rax");
            RegisterStagedCleanup(ownerStorage, _classLocationOwnerRegistrations[expression],
                ownerBinding.RootType);
            EmitClassOwnedLocationPath(expression, ownerBinding.RootExpression, ownerStorage);
            return;
        }
        if (_analysis.SemanticModel.TryGetBoundCall(expression, out var recordCall) &&
            recordCall.Routine.ReturnType is RecordTypeSymbol)
        {
            EmitExpression(expression);
            return;
        }
        if (expression is MeExpressionSyntax)
        {
            if (_currentRoutine?.Receiver == null)
                throw new InvalidOperationException("Me does not have a bound instance receiver.");
            if (_currentRoutine.Receiver.Type.IsClass)
            {
                EmitAddress(_currentRoutine.Receiver);
                Line("    mov rax, QWORD PTR [rax]");
                EmitRequireClassReference();
            }
            else
                EmitAddress(_currentRoutine.Receiver);
            return;
        }
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
            if (!_analysis.SemanticModel.TryGetInstanceField(field, out var fieldSymbol))
                throw new InvalidOperationException($"Unbound instance field '{field.Field.Text}'.");
            if (fieldSymbol.Offset != 0)
                Line($"    add rax, {fieldSymbol.Offset.ToString(CultureInfo.InvariantCulture)}");
            return;
        }
        if (expression is IndexedExpressionSyntax indexed)
        {
            EmitWritableAddress(indexed.Receiver);
            PushRax();
            EmitInstanceFieldIndex(indexed.Indices,
                _analysis.SemanticModel.TryGetInstanceField(indexed, out var indexedField)
                    ? indexedField : throw new InvalidOperationException("Unbound fixed-array field."));
            Line("    mov rcx, rax");
            PopRax();
            Line($"    imul rcx, {Math.Max(8, indexedField.Type.Size).ToString(CultureInfo.InvariantCulture)}");
            Line("    add rax, rcx");
            return;
        }
        if (expression is LeadingMemberAccessExpressionSyntax leading)
        {
            if (!_analysis.SemanticModel.TryGetWithMember(leading, out var binding))
                throw new InvalidOperationException($"Unbound With member '{leading.Member.Text}'.");
            if (binding.InstanceField == null)
                throw new InvalidOperationException($"With property '{leading.Member.Text}' is not a writable field.");
            var storage = _withLocations[binding.ReceiverStatement];
            Line($"    mov rax, {TemporaryMemory(storage)}");
            if (binding.InstanceField.Offset != 0)
                Line($"    add rax, {binding.InstanceField.Offset.ToString(CultureInfo.InvariantCulture)}");
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

    private void EmitClassOwnedLocationPath(ExpressionSyntax expression, ExpressionSyntax rootExpression,
        MasmTemporaryStorage ownerStorage)
    {
        if (ReferenceEquals(expression, rootExpression))
        {
            Line($"    mov rax, {TemporaryMemory(ownerStorage)}");
            return;
        }
        switch (expression)
        {
            case ParenthesizedExpressionSyntax parenthesized:
                EmitClassOwnedLocationPath(parenthesized.Expression, rootExpression, ownerStorage);
                return;
            case FieldAccessExpressionSyntax field:
                EmitClassOwnedLocationPath(field.Receiver, rootExpression, ownerStorage);
                if (!_analysis.SemanticModel.TryGetInstanceField(field, out var fieldSymbol))
                    throw new InvalidOperationException($"Unbound instance field '{field.Field.Text}'.");
                if (fieldSymbol.Offset != 0)
                    Line($"    add rax, {fieldSymbol.Offset.ToString(CultureInfo.InvariantCulture)}");
                return;
            case IndexedExpressionSyntax indexed:
                EmitClassOwnedLocationPath(indexed.Receiver, rootExpression, ownerStorage);
                PushRax();
                if (!_analysis.SemanticModel.TryGetInstanceField(indexed, out var indexedField))
                    throw new InvalidOperationException("Unbound fixed-array field.");
                EmitInstanceFieldIndex(indexed.Indices, indexedField);
                Line("    mov rcx, rax");
                PopRax();
                Line($"    imul rcx, {Math.Max(8, indexedField.Type.Size).ToString(CultureInfo.InvariantCulture)}");
                Line("    add rax, rcx");
                return;
            default:
                throw new InvalidOperationException("Unsupported Class-rooted writable location.");
        }
    }

    private void EmitInstanceFieldIndex(IReadOnlyList<ExpressionSyntax> indices, IInstanceFieldSymbol field)
    {
        EmitExpression(indices[0]);
        if (indices.Count == 1)
            return;
        PushRax();
        EmitExpression(indices[1]);
        Line("    mov rcx, rax");
        PopRax();
        Line($"    imul rax, {field.Dimensions[1].ToString(CultureInfo.InvariantCulture)}");
        Line("    add rax, rcx");
    }

    private void ReleaseClassLocationOwner(ExpressionSyntax expression)
    {
        if (!_classLocationOwners.TryGetValue(expression, out var storage))
            return;
        PushRax();
        EmitCleanup(new MasmCleanupAction(storage, _classLocationOwnerRegistrations[expression]));
        PopRax();
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

    private void EmitIdentity(IdentityExpressionSyntax identity)
    {
        var values = _identityValues[identity];
        var registrations = _identityRegistrations[identity];
        EmitExpression(identity.Left);
        Line($"    mov {TemporaryMemory(values.Left)}, rax");
        if (registrations.Left != null)
            RegisterStagedCleanup(values.Left, registrations.Left, values.Left.Type);
        EmitExpression(identity.Right);
        Line($"    mov {TemporaryMemory(values.Right)}, rax");
        if (registrations.Right != null)
            RegisterStagedCleanup(values.Right, registrations.Right, values.Right.Type);
        Line($"    mov rax, {TemporaryMemory(values.Left)}");
        Line($"    cmp rax, {TemporaryMemory(values.Right)}");
        Line($"    {(identity.IsNegated ? "setne" : "sete")} al");
        Line("    movzx rax, al");
        PushRax();
        if (registrations.Right != null)
            EmitCleanup(new MasmCleanupAction(values.Right, registrations.Right));
        if (registrations.Left != null)
            EmitCleanup(new MasmCleanupAction(values.Left, registrations.Left));
        PopRax();
    }

    private void EmitComparison(string instruction)
    {
        Line("    cmp rax, rcx");
        Line($"    {instruction} al");
        Line("    movzx rax, al");
    }

    private VariableSymbol Resolve(string name)
    {
        if (_analysis.SemanticModel.TryResolveVariable(name, _currentRoutine, out var symbol))
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
            Line($"    mov rax, {QwordImmediate(value)}");
            return;
        }
        EmitAddress(symbol);
        if (symbol.Type is RecordTypeSymbol)
            return;
        Line("    mov rax, QWORD PTR [rax]");
        if (symbol.Type == SmileType.Text || symbol.Type == SmileType.Image || symbol.Type.IsClass)
        {
            Line("    mov rcx, rax");
            CallAligned(symbol.Type == SmileType.Text ? "smile_text_retain" :
                symbol.Type == SmileType.Image ? "smile_image_retain" : "smile_class_retain");
        }
    }

    private void EmitStore(VariableSymbol symbol)
    {
        Line("    mov r10, rax");
        EmitAddress(symbol);
        Line("    mov QWORD PTR [rax], r10");
        Line("    mov rax, r10");
    }

    private IReadOnlyList<VariableSymbol> GetDebugVariables()
    {
        var variables = new List<VariableSymbol>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var routine = _collectRoutine;
        var moduleName = routine?.ModuleName ?? _analysis.SemanticModel.Modules.Values
            .FirstOrDefault(module => module.SyntaxTrees.Any(tree => ReferenceEquals(tree.Source, _currentSource)))
            ?.Name;

        if (routine != null)
        {
            foreach (var local in routine.LocalSymbols.Values.OrderBy(symbol => symbol.DeclarationSpan.Start))
                Add(local);
        }

        foreach (var symbol in _analysis.SemanticModel.Symbols.Values
                     .Where(symbol => string.IsNullOrWhiteSpace(symbol.ModuleName) ||
                                      string.Equals(symbol.ModuleName, moduleName,
                                          StringComparison.OrdinalIgnoreCase))
                     .OrderBy(symbol => string.Equals(symbol.ModuleName, moduleName,
                         StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                     .ThenBy(symbol => symbol.SourceOrdinal)
                     .ThenBy(symbol => symbol.DeclarationSpan.Start))
        {
            Add(symbol);
        }

        return variables;

        void Add(VariableSymbol symbol)
        {
            if (IsCDebugIdentifier(symbol.Name) && names.Add(symbol.Name))
                variables.Add(symbol);
        }
    }

    private static bool IsCDebugIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || CDebugKeywords.Contains(name) ||
            !IsAsciiIdentifierStart(name[0]))
        {
            return false;
        }

        return name.Skip(1).All(character => IsAsciiIdentifierStart(character) ||
                                               character is >= '0' and <= '9');

        static bool IsAsciiIdentifierStart(char character) =>
            character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '_';
    }

    private void EmitDebugSiteCall(MasmDebugSite site)
    {
        if (site.Variables.Count == 0)
        {
            CallAligned(site.HelperName);
            return;
        }

        var stackArguments = Math.Max(0, site.Variables.Count - 4);
        var alignmentBytes = ((_dynamicStackSlots + stackArguments) & 1) != 0 ? 8 : 0;
        var callAreaBytes = 32 + stackArguments * 8 + alignmentBytes;
        Line($"    sub rsp, {callAreaBytes}");

        for (var index = 0; index < site.Variables.Count; index++)
        {
            EmitDebugValue(site.Variables[index]);
            switch (index)
            {
                case 0: Line("    mov rcx, rax"); break;
                case 1: Line("    mov rdx, rax"); break;
                case 2: Line("    mov r8, rax"); break;
                case 3: Line("    mov r9, rax"); break;
                default: Line($"    mov QWORD PTR [rsp+{32 + (index - 4) * 8}], rax"); break;
            }
        }

        Line($"    call {site.HelperName}");
        Line($"    add rsp, {callAreaBytes}");
    }

    private void EmitDebugValue(VariableSymbol symbol)
    {
        if (symbol.IsConstant)
        {
            if (symbol.Type == SmileType.Text)
            {
                Line($"    lea rax, {_constantTextLiterals[symbol].Label}");
            }
            else
            {
                var value = symbol.ConstantValue switch
                {
                    long number => number,
                    bool boolean => boolean ? 1L : 0L,
                    _ => 0L
                };
                Line($"    mov rax, {QwordImmediate(value)}");
            }
        }
        else if (symbol.IsArray || symbol.Type.IsRecord)
        {
            EmitAddress(symbol);
        }
        else
        {
            EmitAddress(symbol);
            Line("    mov rax, QWORD PTR [rax]");
        }

        if (symbol.Type == SmileType.Text && !symbol.IsArray)
        {
            var empty = NewLabel("debug_text_empty");
            Line("    test rax, rax");
            Line($"    jz {empty}");
            Line("    add rax, 16");
            Line($"{empty}:");
        }
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
                if (symbol.Type.IsClass)
                {
                    Line("    mov rcx, rax");
                    CallAligned("smile_class_clear");
                }
                else
                {
                    Line("    mov rcx, QWORD PTR [rax]");
                    CallAligned(symbol.Type == SmileType.Text ? "smile_text_release" : "smile_image_release");
                }
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
        if (cleanup.Registration != null)
            UnregisterStagedCleanup(cleanup.Registration);
        EmitTemporaryAddress(cleanup.Storage, "rcx");
        if (cleanup.Storage.Type is RecordTypeSymbol record)
            CallAligned(RecordClear(record));
        else if (cleanup.Storage.Type.IsClass)
            CallAligned("smile_class_clear");
        else
            CallAligned(cleanup.Storage.Type == SmileType.Text ? "smile_text_clear" : "smile_image_clear");
    }

    private string CleanupRoutine(SmileType type) => type is RecordTypeSymbol record
        ? RecordClear(record)
        : type == SmileType.Text ? "smile_text_clear"
        : type == SmileType.Image ? "smile_image_clear"
        : type.IsClass ? "smile_class_clear"
        : throw new InvalidOperationException($"Type '{type.Name}' does not have a cleanup routine.");

    private static bool IsOwnedClassLocal(VariableSymbol symbol) =>
        symbol.Type.IsClass && symbol.ParameterMode != ParameterPassingMode.ByRef &&
        symbol is not InstanceReceiverSymbol;

    private void EmitRequireClassReference()
    {
        var valid = NewLabel("class_reference_valid");
        Line("    test rax, rax");
        Line($"    jnz {valid}");
        CallAligned("smile_class_nothing_report");
        EmitPopClipsTo(0);
        EmitTermination(2);
        Line($"{valid}:");
    }

    private void EmitRequireClassAllocation()
    {
        var valid = NewLabel("class_allocation_valid");
        Line("    test rax, rax");
        Line($"    jnz {valid}");
        CallAligned("smile_class_allocation_failure_report");
        EmitPopClipsTo(0);
        EmitTermination(3);
        Line($"{valid}:");
    }

    private void EmitTermination(int exitCode)
    {
        CallAligned("smile_cleanup_staged_arguments");
        CallAligned("smile_cleanup_active_frames");
        EmitProgramCleanup();
        CallAligned("smile_class_lifetime_report");
        CallAligned("smile_image_lifetime_report");
        CallAligned("smile_media_shutdown");
        CallAligned("smile_text_lifetime_report");
        if (_usesMusic) CallAligned("smile_music_shutdown");
        Line($"    mov ecx, {exitCode.ToString(CultureInfo.InvariantCulture)}");
        CallAligned("ExitProcess");
    }

    private void EmitCleanupToDepth(int cleanupDepth)
    {
        for (var index = _activeCleanups.Count - 1; index >= cleanupDepth; index--)
            EmitCleanup(_activeCleanups[index]);
    }

    private void EmitPopClipsTo(int clipDepth)
    {
        for (var index = _clipDepth; index > clipDepth; index--)
        {
            CallAligned("smile_clip_pop");
            UpdateActiveFrameClipCount(-1);
        }
    }

    private void EmitStagedCleanupHelper()
    {
        var loop = NewLabel("staged_cleanup_loop");
        var done = NewLabel("staged_cleanup_done");
        Line();
        Line("smile_cleanup_staged_arguments PROC");
        Line("    push rbp");
        Line("    mov rbp, rsp");
        Line("    sub rsp, 48");
        Line($"{loop}:");
        Line("    mov rax, QWORD PTR [smile_staged_cleanup_head]");
        Line("    test rax, rax");
        Line($"    jz {done}");
        Line("    mov QWORD PTR [rbp-8], rax");
        Line("    mov rdx, QWORD PTR [rax]");
        Line("    mov QWORD PTR [smile_staged_cleanup_head], rdx");
        Line("    mov rcx, QWORD PTR [rax+8]");
        Line("    mov rax, QWORD PTR [rax+16]");
        Line("    call rax");
        Line("    mov rax, QWORD PTR [rbp-8]");
        Line("    mov QWORD PTR [rax], 0");
        Line("    mov QWORD PTR [rax+8], 0");
        Line("    mov QWORD PTR [rax+16], 0");
        Line($"    jmp {loop}");
        Line($"{done}:");
        Line("    mov rsp, rbp");
        Line("    pop rbp");
        Line("    ret");
        Line("smile_cleanup_staged_arguments ENDP");
    }

    private void EmitActiveFrameCleanupHelper()
    {
        var loop = NewLabel("active_frame_cleanup_loop");
        var done = NewLabel("active_frame_cleanup_done");
        Line();
        Line("smile_cleanup_active_frames PROC");
        Line("    push rbp");
        Line("    mov rbp, rsp");
        Line("    sub rsp, 48");
        Line($"{loop}:");
        Line("    mov rax, QWORD PTR [smile_active_frame_cleanup_head]");
        Line("    test rax, rax");
        Line($"    jz {done}");
        Line("    mov QWORD PTR [rbp-8], rax");
        Line("    mov rdx, QWORD PTR [rax]");
        Line("    mov QWORD PTR [smile_active_frame_cleanup_head], rdx");
        Line("    mov rcx, rax");
        Line("    mov rax, QWORD PTR [rax+16]");
        Line("    call rax");
        Line("    mov rax, QWORD PTR [rbp-8]");
        for (var offset = 0; offset < 32; offset += 8)
            Line($"    mov QWORD PTR [rax{Offset(offset)}], 0");
        Line($"    jmp {loop}");
        Line($"{done}:");
        Line("    mov rsp, rbp");
        Line("    pop rbp");
        Line("    ret");
        Line("smile_cleanup_active_frames ENDP");
    }

    private void EmitActiveFrameCleanup(RoutineSymbol routine)
    {
        var popClip = NewLabel("active_frame_pop_clip");
        var clearValues = NewLabel("active_frame_clear_values");
        Line();
        Line($"{ActiveFrameCleanupLabel(routine)} PROC");
        Line("    push rbp");
        Line("    mov rbp, rsp");
        Line("    sub rsp, 48");
        Line("    mov QWORD PTR [rbp-8], rcx");
        Line("    mov rax, QWORD PTR [rcx+8]");
        Line("    mov QWORD PTR [rbp-16], rax");
        Line($"{popClip}:");
        Line("    mov rax, QWORD PTR [rbp-8]");
        Line("    cmp QWORD PTR [rax+24], 0");
        Line($"    je {clearValues}");
        CallAligned("smile_clip_pop");
        Line("    mov rax, QWORD PTR [rbp-8]");
        Line("    sub QWORD PTR [rax+24], 1");
        Line($"    jmp {popClip}");
        Line($"{clearValues}:");

        foreach (var temporary in _frameLayouts[routine].Temporaries
                     .Where(temporary => temporary.RequiresCleanup).Reverse())
            EmitActiveFrameValueClear(temporary.Type, temporary.FrameOffset);
        foreach (var symbol in routine.LocalSymbols.Values.Where(IsOwnedFrameValue)
                     .OrderByDescending(symbol => symbol.DeclarationSpan.Start))
        {
            var elementSize = Math.Max(8, symbol.Type.Size);
            for (var index = Math.Max(1, symbol.ArraySize) - 1; index >= 0; index--)
                EmitActiveFrameValueClear(symbol.Type,
                    _frameLayouts[routine].LocalOffsets[symbol] - index * elementSize);
        }

        Line("    mov rsp, rbp");
        Line("    pop rbp");
        Line("    ret");
        Line($"{ActiveFrameCleanupLabel(routine)} ENDP");
    }

    private void EmitActiveFrameValueClear(SmileType type, int frameOffset)
    {
        Line("    mov rax, QWORD PTR [rbp-16]");
        Line($"    lea rcx, [rax-{frameOffset.ToString(CultureInfo.InvariantCulture)}]");
        CallAligned(CleanupRoutine(type));
    }

    private static bool IsOwnedFrameValue(VariableSymbol symbol) =>
        !symbol.IsConstant && symbol.Type.RequiresCleanup &&
        symbol.ParameterMode != ParameterPassingMode.ByRef && symbol is not InstanceReceiverSymbol;

    private string ActiveFrameCleanupLabel(RoutineSymbol routine) =>
        _routineLabels[routine] + "_active_frame_cleanup";

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

    private void EmitClassFinalizer(ClassTypeSymbol classType)
    {
        var label = _classFinalizerLabels[classType];
        Line();
        Line($"{label} PROC");
        Line("    push rbp");
        Line("    mov rbp, rsp");
        Line("    sub rsp, 48");
        Line("    mov QWORD PTR [rbp-8], rcx");

        foreach (var field in classType.Fields.Where(field => field.Type.RequiresValueCleanup)
                     .OrderByDescending(field => field.Ordinal))
        {
            if (field.IsArray)
            {
                var elementSize = Math.Max(8, field.Type.Size);
                for (var index = field.ElementCount - 1; index >= 0; index--)
                    EmitClassFieldClear(field, field.Offset + index * elementSize);
            }
            else
                EmitClassFieldClear(field, field.Offset);
        }

        Line("    mov rax, QWORD PTR [rbp-8]");
        Line("    mov rsp, rbp");
        Line("    pop rbp");
        Line("    ret");
        Line($"{label} ENDP");
    }

    private void EmitClassFieldClear(ClassFieldSymbol field, int offset)
    {
        Line("    mov rax, QWORD PTR [rbp-8]");
        Line($"    lea rcx, [rax{Offset(offset)}]");
        if (field.Type is RecordTypeSymbol record)
            CallAligned(RecordClear(record));
        else if (field.Type == SmileType.Text)
            CallAligned("smile_text_clear");
        else
            throw new InvalidOperationException($"Unsupported finalized Class field type '{field.Type.Name}'.");
    }

    private string RecordCopy(RecordTypeSymbol record) => _recordHelperLabels[record] + "_copy";
    private string RecordClear(RecordTypeSymbol record) => _recordHelperLabels[record] + "_clear";
    private static string Offset(int offset) => offset == 0 ? string.Empty : $"+{offset}";

    private IOrderedEnumerable<RoutineSymbol> OrderedRoutines() =>
        _analysis.SemanticModel.AllRoutines.OrderBy(routine => routine.SourceOrdinal)
            .ThenBy(routine => routine.DeclarationSyntax.Span.Start).ThenBy(routine => routine.SymbolKind);

    private IOrderedEnumerable<RecordTypeSymbol> OrderedRecordTypes() =>
        _analysis.SemanticModel.Types.Values.OrderBy(type => type.SourceOrdinal).ThenBy(type => type.DeclarationSpan.Start);

    private IOrderedEnumerable<ClassTypeSymbol> OrderedClassTypes() =>
        _analysis.SemanticModel.Classes.Values.OrderBy(type => type.SourceOrdinal)
            .ThenBy(type => type.DeclarationSpan.Start);

    private static string SafeName(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
            builder.Append(char.IsLetterOrDigit(character) || character == '_' ? char.ToLowerInvariant(character) : '_');
        return builder.Length == 0 ? "record" : builder.ToString();
    }

    private static string QwordImmediate(long value) =>
        "0" + unchecked((ulong)value).ToString("X16", CultureInfo.InvariantCulture) + "h";

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
        statement is not ConstStatementSyntax and not DimStatementSyntax and not TypeDeclarationSyntax and
            not EnumDeclarationSyntax and not RoutineDeclarationSyntax;

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
