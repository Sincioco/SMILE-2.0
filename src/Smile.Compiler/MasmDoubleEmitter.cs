using Smile.Language;

namespace Smile.Compiler;

/// <summary>Binary64 operations and external ABI placement. Internal SMILE scalar
/// transport remains an eight-byte RAX payload; arithmetic uses floating registers.</summary>
internal static class MasmDoubleEmitter
{
    public static void Binary(BinaryExpressionSyntax binary, Action<string> line,
        Action<string> call, Action<ExpressionSyntax> check)
    {
        line("    movq xmm1, rax");
        line("    movq xmm2, rcx");
        if (DoubleSemantics.IsComparison(binary.OperatorToken.Kind))
        {
            line("    ucomisd xmm1, xmm2");
            var instruction = binary.OperatorToken.Kind switch
            {
                SyntaxKind.EqualsToken => "sete", SyntaxKind.NotEqualsToken => "setne",
                SyntaxKind.LessToken => "setb", SyntaxKind.LessOrEqualsToken => "setbe",
                SyntaxKind.GreaterToken => "seta", _ => "setae"
            };
            line($"    {instruction} al");
            line("    movzx rax, al");
            return;
        }
        line($"    mov ecx, {DoubleSemantics.OperationCode(binary.OperatorToken.Kind)}");
        call("smile_double_math");
        line("    movq rax, xmm0");
        check(binary);
    }

    public static void Intrinsic(CallExpressionSyntax expression, Action<string> line,
        Action<ExpressionSyntax> emit, Action push, Action pop, Action<string> call,
        Action<ExpressionSyntax> check)
    {
        var kind = expression.Identifier.Kind;
        foreach (var argument in expression.Arguments) { emit(argument.Expression); push(); }
        if (DoubleSemantics.OperationCode(kind) != 0)
        {
            for (var index = expression.Arguments.Count; index > 0; index--)
            { pop(); line($"    movq xmm{index}, rax"); }
            line($"    mov ecx, {DoubleSemantics.OperationCode(kind)}");
            call("smile_double_math");
        }
        else
        {
            pop();
            line(kind is SyntaxKind.ToDoubleKeyword or SyntaxKind.TextToDoubleKeyword
                ? "    mov rcx, rax" : "    movq xmm0, rax");
            call(kind switch
            {
                SyntaxKind.ToDoubleKeyword => "smile_to_double",
                SyntaxKind.ToNumberKeyword => "smile_to_number",
                SyntaxKind.TextFromDoubleKeyword => "smile_text_from_double",
                SyntaxKind.TextToDoubleKeyword => "smile_text_to_double",
                _ => throw new InvalidOperationException("Unbound Double intrinsic.")
            });
        }
        if (DoubleSemantics.ResultType(kind) == SmileType.Double) line("    movq rax, xmm0");
        check(expression);
    }
}
