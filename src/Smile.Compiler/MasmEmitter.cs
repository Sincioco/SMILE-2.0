using System.Globalization;
using System.Text;
using Smile.Language;

namespace Smile.Compiler;

internal sealed class MasmEmitter
{
    private readonly SmileAnalysisResult _analysis;
    private readonly SmileGraphicsBackend _graphicsBackend;
    private readonly bool _vSync;
    private readonly StringBuilder _builder = new();
    private readonly Dictionary<VariableSymbol, string> _symbolLabels = new();
    private readonly Dictionary<string, string> _routineLabels = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<LiteralExpressionSyntax, TextLiteral> _textLiterals = new();
    private readonly Dictionary<SyntaxToken, TextLiteral> _gameTextLiterals = new();
    private readonly Dictionary<ForStatementSyntax, string> _forLimits = new();
    private readonly Dictionary<SelectStatementSyntax, string> _selectValues = new();
    private readonly Stack<string> _forExitLabels = new();
    private readonly Stack<string> _doExitLabels = new();
    private RoutineSymbol? _currentRoutine;
    private string? _returnLabel;
    private int _labelId;
    private bool _usesTimer;
    private bool _usesGameClosed;
    private bool _usesKeyHeld;
    private bool _usesMusic;

    public MasmEmitter(SmileAnalysisResult analysis, SmileGraphicsBackend graphicsBackend,
        bool vSync)
    {
        _analysis = analysis;
        _graphicsBackend = graphicsBackend;
        _vSync = vSync;
    }

    public bool UsesMusic => _usesMusic;

    public string Emit()
    {
        Collect(_analysis.SyntaxTree.Root.Statements);
        AssignLabels();

        Line("option casemap:none");
        Line("EXTERN ExitProcess:PROC");
        Line("EXTERN smile_print_text:PROC");
        Line("EXTERN smile_print_number:PROC");
        Line("EXTERN smile_print_boolean:PROC");
        Line("EXTERN smile_print_newline:PROC");
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
        Line("EXTERN smile_show_screen:PROC");
        Line("EXTERN smile_play_sound:PROC");
        Line("EXTERN smile_stop_sound:PROC");
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
        Line();
        Line(".data");
        EmitStorage(_analysis.SemanticModel.Symbols.Values);
        foreach (var routine in _analysis.SemanticModel.Routines.Values)
            EmitStorage(routine.LocalSymbols.Values);
        foreach (var limit in _forLimits.Values)
            Line($"{limit} QWORD 0");
        foreach (var value in _selectValues.Values)
            Line($"{value} QWORD 0");
        foreach (var literal in _textLiterals.Values)
            Line($"{literal.Label} BYTE {FormatBytes(literal.Bytes)}");
        foreach (var literal in _gameTextLiterals.Values)
            Line($"{literal.Label} BYTE {FormatBytes(literal.Bytes)}");

        Line();
        Line(".code");
        Line("main PROC");
        Line("    sub rsp, 104");
        Line($"    mov rcx, {(int)_graphicsBackend}");
        Line($"    mov rdx, {(_vSync ? 1 : 0)}");
        Line("    call smile_graphics_configure");
        EmitStatements(_analysis.SyntaxTree.Root.Statements);
        if (_usesMusic) Line("    call smile_music_shutdown");
        Line("    xor eax, eax");
        Line("    add rsp, 104");
        Line("    ret");
        Line("main ENDP");

        foreach (var routine in _analysis.SemanticModel.Routines.Values)
            EmitRoutine(routine);

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
            Line(symbol.IsArray
                ? $"{label} QWORD {symbol.ArraySize.ToString(CultureInfo.InvariantCulture)} DUP(0)"
                : $"{label} QWORD 0");
        }
    }

    private void Collect(IReadOnlyList<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
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
                    _forLimits[forStatement] = $"for_limit_{_forLimits.Count}";
                    Collect(forStatement.Statements);
                    break;
                case DoStatementSyntax doStatement:
                    Collect(doStatement.Statements);
                    CollectExpression(doStatement.UntilCondition);
                    break;
                case RoutineDeclarationSyntax routine:
                    Collect(routine.Statements);
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
                    _selectValues[select] = $"select_value_{_selectValues.Count}";
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
                    if (graphics.Text != null)
                        CollectTextToken(graphics.Text);
                    foreach (var argument in graphics.Arguments)
                        CollectExpression(argument);
                    break;
                case SoundStatementSyntax sound when sound.Path != null:
                    CollectTextToken(sound.Path);
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
                case SaveStatementSyntax save:
                    CollectTextToken(save.Key);
                    break;
            }
        }
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
        }
        foreach (var routine in _analysis.SemanticModel.Routines.Values)
        {
            _routineLabels[routine.Name] = "routine_" + _routineLabels.Count;
            foreach (var symbol in routine.LocalSymbols.Values)
            {
                if (!symbol.IsConstant)
                    _symbolLabels[symbol] = (symbol.IsArray ? "local_array_" : "local_") + id++;
            }
        }
    }

    private void EmitRoutine(RoutineSymbol routine)
    {
        _currentRoutine = routine;
        _returnLabel = NewLabel("routine_return");
        Line();
        Line($"{_routineLabels[routine.Name]} PROC");
        Line("    sub rsp, 104");
        EmitStatements(routine.Declaration.Statements);
        if (!routine.IsFunction)
            Line("    xor eax, eax");
        Line($"{_returnLabel}:");
        Line("    add rsp, 104");
        Line("    ret");
        Line($"{_routineLabels[routine.Name]} ENDP");
        _returnLabel = null;
        _currentRoutine = null;
    }

    private void EmitStatements(IReadOnlyList<StatementSyntax> statements)
    {
        foreach (var statement in statements)
            EmitStatement(statement);
    }

    private void EmitStatement(StatementSyntax statement)
    {
        switch (statement)
        {
            case ConstStatementSyntax:
            case DimStatementSyntax:
            case RoutineDeclarationSyntax:
                break;
            case AssignmentStatementSyntax assignment:
                EmitAssignment(assignment);
                break;
            case PrintStatementSyntax print:
                EmitPrint(print);
                break;
            case GetKeyStatementSyntax getKey:
                Line("    call smile_get_key");
                Line($"    mov QWORD PTR [{Label(getKey.Identifier.Text)}], rax");
                break;
            case ClearScreenStatementSyntax:
                Line("    call smile_clear_screen");
                break;
            case WaitStatementSyntax wait:
                EmitExpression(wait.Duration);
                Line("    mov rcx, rax");
                Line("    call smile_wait");
                break;
            case RandomStatementSyntax random:
                EmitExpression(random.Minimum);
                Line("    push rax");
                EmitExpression(random.Maximum);
                Line("    mov rdx, rax");
                Line("    pop rcx");
                Line("    call smile_random");
                Line($"    mov QWORD PTR [{Label(random.Identifier.Text)}], rax");
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
                Line($"    jmp {_returnLabel}");
                break;
            case SelectStatementSyntax select:
                EmitSelect(select);
                break;
            case ExitStatementSyntax exit:
                Line($"    jmp {(exit.TargetKeyword.Kind == SyntaxKind.ForKeyword ? _forExitLabels.Peek() : _doExitLabels.Peek())}");
                break;
            case EndProgramStatementSyntax:
                if (_usesMusic) Line("    call smile_music_shutdown");
                Line("    xor ecx, ecx");
                Line("    call ExitProcess");
                break;
            case GameWindowStatementSyntax gameWindow:
                EmitTextArgument(gameWindow.Title);
                if (gameWindow.Width != null) EmitExpression(gameWindow.Width); else Line("    mov rax, 960");
                Line("    push rax");
                if (gameWindow.Height != null) EmitExpression(gameWindow.Height); else Line("    mov rax, 540");
                Line("    push rax");
                EmitNativeCall("smile_game_open", 4);
                break;
            case ClearColorStatementSyntax clearColor:
                EmitExpression(clearColor.Color);
                Line("    push rax");
                EmitNativeCall("smile_game_clear", 1);
                break;
            case GraphicsStatementSyntax graphics:
                EmitGraphics(graphics);
                break;
            case ShowScreenStatementSyntax:
                Line("    call smile_show_screen");
                break;
            case SoundStatementSyntax sound:
                if (sound.IsStop)
                    Line("    call smile_stop_sound");
                else
                {
                    EmitTextArgument(sound.Path!);
                    EmitNativeCall("smile_play_sound", 2);
                }
                break;
            case MusicStatementSyntax music:
                EmitMusic(music);
                break;
            case LoadStatementSyntax load:
                EmitTextArgument(load.Key);
                EmitExpression(load.DefaultValue);
                Line("    push rax");
                EmitNativeCall("smile_load_value", 3);
                Line($"    mov QWORD PTR [{Label(load.Identifier.Text)}], rax");
                break;
            case TextFileLoadStatementSyntax textFileLoad:
                EmitTextArgument(textFileLoad.Path);
                var destination = Resolve(textFileLoad.Destination.Text);
                Line($"    lea rax, {_symbolLabels[destination]}");
                Line("    push rax");
                Line($"    mov rax, {destination.ArraySize.ToString(CultureInfo.InvariantCulture)}");
                Line("    push rax");
                EmitNativeCall("smile_load_text_file", 4);
                Line($"    mov QWORD PTR [{Label(textFileLoad.CountIdentifier.Text)}], rax");
                break;
            case SaveStatementSyntax save:
                EmitTextArgument(save.Key);
                var saved = Resolve(save.Identifier.Text);
                Line(saved.IsConstant
                    ? $"    mov rax, {saved.ConstantValue.ToString(CultureInfo.InvariantCulture)}"
                    : $"    mov rax, QWORD PTR [{_symbolLabels[saved]}]");
                Line("    push rax");
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
                Line("    push rax");
                EmitNativeCall("smile_music_play", 3);
                break;
            case MusicOperation.Pause:
                Line("    call smile_music_pause");
                break;
            case MusicOperation.Resume:
                Line("    call smile_music_resume");
                break;
            case MusicOperation.Stop:
                Line("    call smile_music_stop");
                break;
            case MusicOperation.SetVolume:
                EmitExpression(statement.Volume!);
                Line("    mov rcx, rax");
                Line("    call smile_music_set_volume");
                break;
        }
    }

    private void EmitGraphics(GraphicsStatementSyntax statement)
    {
        if (statement.Operation == GraphicsOperation.DrawText)
            EmitTextArgument(statement.Text!);
        foreach (var argument in statement.Arguments)
        {
            EmitExpression(argument);
            Line("    push rax");
        }
        if (statement.Operation == GraphicsOperation.DrawText)
        {
            Line($"    mov rax, {(statement.Centered ? 1 : 0)}");
            Line("    push rax");
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
            GraphicsOperation.DrawText => "smile_draw_text",
            GraphicsOperation.DrawNumber => "smile_draw_number",
            _ => throw new InvalidOperationException("Unknown graphics operation.")
        };
        EmitNativeCall(name, statement.Arguments.Count + (statement.Operation == GraphicsOperation.DrawText ? 3 : 0));
    }

    private void EmitTextArgument(SyntaxToken token)
    {
        var text = _gameTextLiterals[token];
        Line($"    lea rax, {text.Label}");
        Line("    push rax");
        Line($"    mov rax, {text.Bytes.Length.ToString(CultureInfo.InvariantCulture)}");
        Line("    push rax");
    }

    private void EmitNativeCall(string name, int argumentCount)
    {
        for (var index = argumentCount - 1; index >= 0; index--)
        {
            Line("    pop rax");
            switch (index)
            {
                case 0: Line("    mov rcx, rax"); break;
                case 1: Line("    mov rdx, rax"); break;
                case 2: Line("    mov r8, rax"); break;
                case 3: Line("    mov r9, rax"); break;
                // The remaining arguments are still below RSP while values are popped.
                // Account for that temporary expression stack so the value lands in the
                // final Windows x64 outgoing-argument slot after all pops complete.
                default: Line($"    mov QWORD PTR [rsp+{index * 16}], rax"); break;
            }
        }
        Line($"    call {name}");
    }

    private void EmitAssignment(AssignmentStatementSyntax assignment)
    {
        EmitExpression(assignment.Expression);
        if (!assignment.Target.IsArrayElement)
        {
            Line($"    mov QWORD PTR [{Label(assignment.Target.Identifier.Text)}], rax");
            return;
        }
        Line("    push rax");
        var symbol = Resolve(assignment.Target.Identifier.Text);
        EmitArrayIndex(assignment.Target.Indices, symbol);
        Line("    mov rcx, rax");
        Line("    pop rax");
        Line($"    lea rdx, {_symbolLabels[symbol]}");
        Line("    mov QWORD PTR [rdx+rcx*8], rax");
    }

    private void EmitArrayIndex(IReadOnlyList<ExpressionSyntax> indices, VariableSymbol symbol)
    {
        EmitExpression(indices[0]);
        if (indices.Count == 1)
            return;
        Line("    push rax");
        EmitExpression(indices[1]);
        Line("    mov rcx, rax");
        Line("    pop rax");
        Line($"    imul rax, {symbol.ArrayDimensions[1].ToString(CultureInfo.InvariantCulture)}");
        Line("    add rax, rcx");
    }

    private void EmitPrint(PrintStatementSyntax print)
    {
        foreach (var item in print.Items)
        {
            if (item is LiteralExpressionSyntax literal && literal.Value is string)
            {
                var text = _textLiterals[literal];
                Line($"    lea rcx, {text.Label}");
                Line($"    mov rdx, {text.Bytes.Length.ToString(CultureInfo.InvariantCulture)}");
                Line("    call smile_print_text");
                continue;
            }
            EmitExpression(item);
            Line("    mov rcx, rax");
            Line(_analysis.SemanticModel.GetType(item) == SmileType.Boolean
                ? "    call smile_print_boolean"
                : "    call smile_print_number");
        }
        if (!print.SuppressNewLine)
            Line("    call smile_print_newline");
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
        var counterLabel = Label(statement.Identifier.Text);
        var limitLabel = _forLimits[statement];
        EmitExpression(statement.LowerBound);
        Line($"    mov QWORD PTR [{counterLabel}], rax");
        EmitExpression(statement.UpperBound);
        Line($"    mov QWORD PTR [{limitLabel}], rax");
        Line($"{startLabel}:");
        Line($"    mov rax, QWORD PTR [{counterLabel}]");
        Line($"    cmp rax, QWORD PTR [{limitLabel}]");
        Line(statement.IsDescending ? $"    jl {endLabel}" : $"    jg {endLabel}");
        _forExitLabels.Push(endLabel);
        EmitStatements(statement.Statements);
        _forExitLabels.Pop();
        Line(statement.IsDescending ? $"    dec QWORD PTR [{counterLabel}]" : $"    inc QWORD PTR [{counterLabel}]");
        Line($"    jmp {startLabel}");
        Line($"{endLabel}:");
    }

    private void EmitDo(DoStatementSyntax statement)
    {
        var startLabel = NewLabel("do_start");
        var endLabel = NewLabel("do_end");
        Line($"{startLabel}:");
        _doExitLabels.Push(endLabel);
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
        var valueLabel = _selectValues[statement];
        EmitExpression(statement.Expression);
        Line($"    mov QWORD PTR [{valueLabel}], rax");
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
            Line($"    cmp QWORD PTR [{valueLabel}], rax");
            Line($"    jne {nextLabel}");
            EmitStatements(clause.Statements);
            Line($"    jmp {endLabel}");
            Line($"{nextLabel}:");
        }
        if (elseClause != null)
            EmitStatements(elseClause.Statements);
        Line($"{endLabel}:");
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
            case NameExpressionSyntax name:
                var symbol = Resolve(name.Identifier.Text);
                Line(symbol.IsConstant
                    ? $"    mov rax, {symbol.ConstantValue.ToString(CultureInfo.InvariantCulture)}"
                    : $"    mov rax, QWORD PTR [{_symbolLabels[symbol]}]");
                break;
            case ArrayAccessExpressionSyntax array:
                var arraySymbol = Resolve(array.Identifier.Text);
                EmitArrayIndex(array.Indices, arraySymbol);
                Line($"    lea rdx, {_symbolLabels[arraySymbol]}");
                Line("    mov rax, QWORD PTR [rdx+rax*8]");
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
                Line("    push rax");
                EmitExpression(call.Arguments[1]);
                Line("    mov rcx, rax");
                Line("    pop rax");
                Line("    cmp rax, rcx");
                Line(call.Identifier.Kind == SyntaxKind.MinKeyword ? "    cmovg rax, rcx" : "    cmovl rax, rcx");
                break;
            case SyntaxKind.RgbKeyword:
                EmitExpression(call.Arguments[0]);
                Line("    push rax");
                EmitExpression(call.Arguments[1]);
                Line("    push rax");
                EmitExpression(call.Arguments[2]);
                Line("    mov rdx, rax");
                Line("    pop rcx");
                Line("    pop rax");
                Line("    and rax, 255");
                Line("    and rcx, 255");
                Line("    shl rcx, 8");
                Line("    and rdx, 255");
                Line("    shl rdx, 16");
                Line("    or rax, rcx");
                Line("    or rax, rdx");
                break;
            case SyntaxKind.TimerKeyword:
                Line("    call smile_timer");
                break;
            case SyntaxKind.GameClosedKeyword:
                Line("    call smile_game_closed");
                break;
            case SyntaxKind.KeyHeldKeyword:
                EmitExpression(call.Arguments[0]);
                Line("    mov rcx, rax");
                Line("    call smile_key_held");
                break;
            default:
                EmitRoutineCall(call.Identifier.Text, call.Arguments);
                break;
        }
    }

    private void EmitRoutineCall(string name, IReadOnlyList<ExpressionSyntax> arguments)
    {
        var routine = _analysis.SemanticModel.Routines[name];
        foreach (var argument in arguments)
        {
            EmitExpression(argument);
            Line("    push rax");
        }
        for (var index = arguments.Count - 1; index >= 0; index--)
        {
            Line("    pop rax");
            Line($"    mov QWORD PTR [{_symbolLabels[routine.Parameters[index]]}], rax");
        }
        Line($"    call {_routineLabels[name]}");
    }

    private void EmitBinary(BinaryExpressionSyntax binary)
    {
        EmitExpression(binary.Left);
        Line("    push rax");
        EmitExpression(binary.Right);
        Line("    mov rcx, rax");
        Line("    pop rax");
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

    private string Label(string name) => _symbolLabels[Resolve(name)];
    private string NewLabel(string prefix) => prefix + "_" + _labelId++;
    private void Line(string text = "") => _builder.AppendLine(text);

    private static string FormatBytes(byte[] bytes) => bytes.Length == 0
        ? "0"
        : string.Join(",", bytes.Select(value => $"0{value:X2}h"));

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
