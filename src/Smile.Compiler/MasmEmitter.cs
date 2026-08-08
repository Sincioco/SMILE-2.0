using System.Globalization;
using System.Text;
using Smile.Language;

namespace Smile.Compiler;

internal sealed class MasmEmitter
{
    private readonly SmileAnalysisResult _analysis;
    private readonly StringBuilder _builder = new();
    private readonly Dictionary<string, string> _symbolLabels = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<LiteralExpressionSyntax, TextLiteral> _textLiterals = new();
    private readonly Dictionary<ForStatementSyntax, string> _forLimits = new();
    private int _labelId;

    public MasmEmitter(SmileAnalysisResult analysis) => _analysis = analysis;

    public string Emit()
    {
        Collect(_analysis.SyntaxTree.Root.Statements);
        AssignSymbolLabels();

        Line("option casemap:none");
        Line("EXTERN smile_print_text:PROC");
        Line("EXTERN smile_print_number:PROC");
        Line("EXTERN smile_print_boolean:PROC");
        Line("EXTERN smile_print_newline:PROC");
        Line("EXTERN smile_get_key:PROC");
        Line("EXTERN smile_clear_screen:PROC");
        Line("EXTERN smile_wait:PROC");
        Line("EXTERN smile_random:PROC");
        Line();
        Line(".data");
        foreach (var pair in _analysis.SemanticModel.Symbols)
        {
            var symbol = pair.Value;
            var label = _symbolLabels[pair.Key];
            Line(symbol.IsArray
                ? $"{label} QWORD {symbol.ArraySize.ToString(CultureInfo.InvariantCulture)} DUP(0)"
                : $"{label} QWORD 0");
        }
        foreach (var limit in _forLimits.Values)
            Line($"{limit} QWORD 0");
        foreach (var literal in _textLiterals.Values)
            Line($"{literal.Label} BYTE {FormatBytes(literal.Bytes)}");

        Line();
        Line(".code");
        Line("main PROC");
        Line("    sub rsp, 40");
        EmitStatements(_analysis.SyntaxTree.Root.Statements);
        Line("    xor eax, eax");
        Line("    add rsp, 40");
        Line("    ret");
        Line("main ENDP");
        Line("END");
        return _builder.ToString();
    }

    private void Collect(IReadOnlyList<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            switch (statement)
            {
                case AssignmentStatementSyntax assignment:
                    CollectExpression(assignment.Target.Index);
                    CollectExpression(assignment.Expression);
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
                case DoUntilStatementSyntax doUntil:
                    Collect(doUntil.Statements);
                    CollectExpression(doUntil.Condition);
                    break;
            }
        }
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
                CollectExpression(array.Index);
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
        }
    }

    private void AssignSymbolLabels()
    {
        var id = 0;
        foreach (var pair in _analysis.SemanticModel.Symbols)
            _symbolLabels[pair.Key] = (pair.Value.IsArray ? "array_" : "variable_") + id++;
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
            case AssignmentStatementSyntax assignment:
                EmitAssignment(assignment);
                break;
            case DimStatementSyntax:
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
            case DoUntilStatementSyntax doUntil:
                EmitDoUntil(doUntil);
                break;
        }
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
        EmitExpression(assignment.Target.Index!);
        Line("    mov rcx, rax");
        Line("    pop rax");
        Line($"    lea rdx, {Label(assignment.Target.Identifier.Text)}");
        Line("    mov QWORD PTR [rdx+rcx*8], rax");
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
        EmitStatements(statement.Statements);
        Line(statement.IsDescending
            ? $"    dec QWORD PTR [{counterLabel}]"
            : $"    inc QWORD PTR [{counterLabel}]");
        Line($"    jmp {startLabel}");
        Line($"{endLabel}:");
    }

    private void EmitDoUntil(DoUntilStatementSyntax statement)
    {
        var startLabel = NewLabel("do_start");
        Line($"{startLabel}:");
        EmitStatements(statement.Statements);
        EmitExpression(statement.Condition);
        Line("    cmp rax, 0");
        Line($"    je {startLabel}");
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
                Line($"    mov rax, QWORD PTR [{Label(name.Identifier.Text)}]");
                break;
            case ArrayAccessExpressionSyntax array:
                EmitExpression(array.Index);
                Line($"    lea rdx, {Label(array.Identifier.Text)}");
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
            default:
                Line("    xor rax, rax");
                break;
        }
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
            case SyntaxKind.PlusToken:
                Line("    add rax, rcx");
                break;
            case SyntaxKind.MinusToken:
                Line("    sub rax, rcx");
                break;
            case SyntaxKind.AndKeyword:
                Line("    and rax, rcx");
                break;
            case SyntaxKind.OrKeyword:
                Line("    or rax, rcx");
                break;
            case SyntaxKind.EqualsToken:
                EmitComparison("sete");
                break;
            case SyntaxKind.NotEqualsToken:
                EmitComparison("setne");
                break;
            case SyntaxKind.LessToken:
                EmitComparison("setl");
                break;
            case SyntaxKind.GreaterToken:
                EmitComparison("setg");
                break;
            case SyntaxKind.LessOrEqualsToken:
                EmitComparison("setle");
                break;
            case SyntaxKind.GreaterOrEqualsToken:
                EmitComparison("setge");
                break;
        }
    }

    private void EmitComparison(string instruction)
    {
        Line("    cmp rax, rcx");
        Line($"    {instruction} al");
        Line("    movzx rax, al");
    }

    private string Label(string name) => _symbolLabels[name];
    private string NewLabel(string prefix) => prefix + "_" + _labelId++;
    private void Line(string text = "") => _builder.AppendLine(text);

    private static string FormatBytes(byte[] bytes)
    {
        if (bytes.Length == 0)
            return "0";
        return string.Join(",", bytes.Select(value => $"0{value:X2}h"));
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
