using Smile.Compiler;
using Smile.Language;

internal static class ContextualIdentifierTests
{
    public static void Register(TestContext tests)
    {
        tests.Run("Direction identifiers preserve variable reads, arrays and constants", () =>
        {
            foreach (var declarations in new[] {
                "Dim Left As Number\nLeft = 94\n",
                "Left = 94\n",
                "Dim Left As Double\nLeft = 94.5\n",
                "Dim Left As Text\nLeft = \"position\"\n",
                "Const Left = 94\n" })
            {
                var analysis = Valid(declarations + "Print lEfT\n");
                var read = analysis.BoundSyntaxTree.Root.Statements.OfType<PrintStatementSyntax>()
                    .Single().Items.Single();
                Require(read is NameExpressionSyntax name && name.Identifier.Kind == SyntaxKind.IdentifierToken,
                    "Declared read must bind as an identifier, not the number 12.");
                Require(SmileSymbolService.TryResolve(analysis, analysis.SyntaxTree,
                    declarations.Length + "Print ".Length, out var resolved) && resolved.Name == "Left" &&
                    resolved.DeclarationLocation != null, "Hover and definition resolve the declaration");
                Emit(analysis);
            }
            Emit(Valid("Dim Right[2] As Number\nRight[1] = 94\nPrint Right[1]\n"));
        });
        tests.Run("Unshadowed directions retain constant values and constant-expression use", () =>
        {
            var analysis = Valid("Option Explicit\nConst Total = Left + Right\n" +
                "Dim Values[Left] As Number\nPrint Left\nPrint Right\n");
            var values = analysis.BoundSyntaxTree.Root.Statements.OfType<PrintStatementSyntax>()
                .Select(print => ((LiteralExpressionSyntax)print.Items.Single()).Value).ToArray();
            Require(values.SequenceEqual(new object[] { 12L, 13L }), "Legacy direction constants");
            Require(Equals(analysis.SemanticModel.Symbols["Total"].ConstantValue, 25L), "Constant folding");
            Emit(analysis);
        });
        tests.Run("Direction parameters and local declarations shadow only their own scopes", () =>
        {
            var analysis = Valid("Print Left\nCall Place(94)\n" +
                "Sub Place(Left As Number)\nDim Right As Number\nRight = Left\nPrint Right\nEnd Sub\n");
            var routine = analysis.BoundSyntaxTree.Root.Statements.OfType<RoutineDeclarationSyntax>().Single();
            Require(routine.Statements.OfType<AssignmentStatementSyntax>().Single().Expression
                is NameExpressionSyntax, "Parameter read");
            Require(routine.Statements.OfType<PrintStatementSyntax>().Single().Items.Single()
                is NameExpressionSyntax, "Local read");
            Require(analysis.BoundSyntaxTree.Root.Statements.OfType<PrintStatementSyntax>().Single().Items.Single()
                is LiteralExpressionSyntax, "Local must not shadow another scope");
            Emit(analysis);
            var invalid = SmileLanguage.Analyze("Sub Place()\nPrint Left\nDim Left As Number\nEnd Sub\n");
            Require(invalid.Diagnostics.Any(d => d.Code == "SML3307"), "Read before local Dim retains its diagnostic");
        });
        tests.Run("Direction module members resolve without inheriting consumer variables", () =>
        {
            var analysis = SmileLanguage.Analyze(new[] {
                new SmileSourceDocument("Import Test.Directions As Directions\nDim Left As Number\nLeft = 94\n" +
                    "Print Directions.Member()\nPrint Directions.BuiltIn()\n", "Main.smile", true),
                new SmileSourceDocument("Module Test.Directions\nPrivate Dim Right As Number\n" +
                    "Public Function Member() As Number\nRight = 95\nReturn Right\nEnd Function\n" +
                    "Public Function BuiltIn() As Number\nReturn Left\nEnd Function\nEnd Module\n", "Directions.smile") });
            Require(!analysis.HasErrors, string.Join("; ", analysis.Diagnostics.Select(d => d.Message)));
            var functions = analysis.BoundSyntaxTrees[1].Root.Statements.OfType<RoutineDeclarationSyntax>().ToArray();
            Require(functions[0].Statements.OfType<ReturnStatementSyntax>().Single().Expression
                is NameExpressionSyntax, "Module field read");
            Require(functions[1].Statements.OfType<ReturnStatementSyntax>().Single().Expression
                is LiteralExpressionSyntax constant && Equals(constant.Value, 12L), "Module isolation");
            Emit(analysis);
        });
    }

    private static SmileAnalysisResult Valid(string source)
    {
        var analysis = SmileLanguage.Analyze(source);
        Require(!analysis.HasErrors, string.Join("; ", analysis.Diagnostics.Select(d => d.Message)));
        return analysis;
    }

    private static void Emit(SmileAnalysisResult analysis)
    {
        _ = new MasmEmitter(analysis, SmileGraphicsBackend.DirectX, true, false).Emit();
        _ = new WebEmitter(analysis).Emit();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
