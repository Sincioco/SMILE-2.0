using Smile.Language;
using Smile.Compiler;

internal static class DoubleTests
{
    public static void Register(TestContext tests)
    {
        tests.Run("Double literals preserve type, binary64 size and signed zero", () =>
        {
            var analysis = Valid("Dim Position As Double\nPosition = 0.125 + 1e-3\nConst ZERO = -0.0\n");
            Require(analysis.SemanticModel.Symbols["Position"].Type == SmileType.Double, "Distinct type");
            Require(SmileType.Double.Size == 8, "Eight-byte storage");
            Require(BitConverter.DoubleToInt64Bits((double)analysis.SemanticModel.Symbols["ZERO"].ConstantValue) < 0,
                "Signed constant zero");
        });
        tests.Run("Double rejects mixed types and integral-only destinations", () =>
        {
            foreach (var source in new[] {
                "Dim Value As Double\nValue = 1\n", "Dim Value As Number\nValue = 1.0\n",
                "Print 1.0 + 1\n", "Print 1.0 = 1\n", "Print 1.0 Mod 1.0\n",
                "Dim Values[2] As Double\nPrint Values[0.0]\n", "Dim Values[2.0] As Double\n",
                "Dim Counter As Double\nFor Counter = 0.0 To 2.0\nEnd For\n",
                "Dim Value As Number\nCall Adjust(Value)\nSub Adjust(ByRef Item As Double)\nEnd Sub\n",
                "Print ToDouble(1.0)\n", "Print ToNumber(1)\n", "Print Min(1.0, 1)\n" })
                Invalid(source);
        });
        tests.Run("Double malformed literals retain exact error spans", () =>
        {
            foreach (var literal in new[] { "1e", "1e+", "1e999" })
            {
                var analysis = Invalid("Print " + literal + "\nPrint 2\n");
                Require(analysis.Diagnostics.Any(d => d.Code == "SML3800" && d.Location.Span.Start == 6 &&
                    d.Location.Span.Length == literal.Length), "Literal span " + literal);
            }
            Invalid("Print 1.2.3\n");
            Invalid("Print .5\n");
        });
        tests.Run("Double constants share checked math and conversion rules", () =>
        {
            var analysis = Valid("Const FRACTION = 1.0 / 8.0\nConst INTEGER = ToNumber(-1.75)\n" +
                "Const ROUND_VALUE = Round(2.5)\nConst TEXT = Text_From_Double(-0.0)\n");
            Require(Equals(analysis.SemanticModel.Symbols["FRACTION"].ConstantValue, 0.125), "Fold division");
            Require(Equals(analysis.SemanticModel.Symbols["INTEGER"].ConstantValue, -1L), "Fold truncation");
            Require(Equals(analysis.SemanticModel.Symbols["ROUND_VALUE"].ConstantValue, 2.0), "Fold ties to even");
            Require(Equals(analysis.SemanticModel.Symbols["TEXT"].ConstantValue, "-0.0"), "Fold zero formatting");
            foreach (var value in new[] { "1.0 / 0.0", "1e308 * 2.0", "Sqrt(-1.0)",
                "ToNumber(9223372036854775808.0)", "Clamp(0.0, 2.0, 1.0)", "Text_To_Double(\"1tail\")" })
                Invalid("Const BAD = " + value + "\n");
        });
        tests.Run("Double preserves contextual user routines and scalar names", () =>
        {
            Valid("Dim Floor As Number\nFloor = 3\nPrint Double(Floor)\n" +
                "Function Double(Value As Number) As Number\nReturn Value\nEnd Function\n");
            Valid("Result = Floor(1)\nPrint Result\n" +
                "Function Floor(Value As Number) As Text\nReturn \"Floor\"\nEnd Function\n");
            var analysis = SmileLanguage.Analyze(new[] {
                new SmileSourceDocument("Import Local.Math As Math\nPrint Math.Double(3)\nPrint Math.Clamp(4)\n", "Main.smile", true),
                new SmileSourceDocument("Module Local.Math\nPublic Function Double(Value As Number) As Number\n" +
                    "Return Value\nEnd Function\nPublic Function Clamp(Value As Number) As Number\n" +
                    "Return Value\nEnd Function\nEnd Module\n", "Math.smile") });
            Require(!analysis.HasErrors, string.Join("; ", analysis.Diagnostics.Select(d => d.Message)));
        });
        tests.Run("Web Number constant bounds preserve compile-time Enum definitions", () =>
        {
            const string declarations = "Const MINIMUM = -9223372036854775807 - 1\n" +
                "Enum State\nMinimum = MINIMUM\nEnd Enum\n";
            var source = new WebEmitter(Valid(declarations + "Print State.Minimum = State.Minimum\n")).Emit();
            Require(source.Contains("-9223372036854775808n"), "Exact Enum BigInt value");
            foreach (var program in new[] { declarations + "Print MINIMUM\n",
                "Const OUTSIDE = ToNumber(9007199254740992.0)\nPrint OUTSIDE\n" })
            {
                var rejected = false;
                try { new WebEmitter(Valid(program)).Emit(); }
                catch (WebTargetException) { rejected = true; }
                Require(rejected, "Runtime Number constant must remain in Web safe range");
            }
        });
        tests.Run("Double formatter and completion consume shared type facts", () =>
        {
            const string source = "Function Fraction(Value As Double) As Double\nReturn Value / 2.0\nEnd Function\n";
            var formatted = SmileSourceFormatter.Format(source, true, 100, true, true, "Double.smile");
            Require(formatted.Contains(" As Double"), "Double temporary type");
            Require(!formatted.Contains(" As Number"), "No integer temporary");
            Valid(formatted);
            const string incomplete = "Dim Value As ";
            var analysis = SmileLanguage.Analyze(incomplete);
            Require(SmileCompletionService.GetCompletions(analysis, incomplete.Length).Any(c => c.DisplayText == "Double"),
                "Double completion");
            Require(DoubleSemantics.Signature(SyntaxKind.Atan2Keyword) ==
                "Atan2(Y As Double, X As Double) As Double", "Typed shared signature");
            const string optional = "Print DefaultValue()\nFunction DefaultValue(Optional Value As Double = -0.0) As Double\nReturn Value\nEnd Function\n";
            var optionalAnalysis = Valid(optional);
            Require(SmileSymbolService.TryResolve(optionalAnalysis, optionalAnalysis.SyntaxTree, 8, out var symbol),
                "Double routine navigation");
            Require(symbol.Signature.Contains("Optional Value As Double = -0.0"), "Optional Quick Info signature");
        });
        tests.Run("Double debug views retain scalar, array and nested field types", () =>
        {
            var analysis = Valid("Type Point\nValues[2] As Double\nEnd Type\n" +
                "Dim Value As Double\nDim Values[2] As Double\nDim Points[2] As Point\n" +
                "Value = 0.125\n");
            var emitter = new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, true);
            emitter.Emit();
            var source = CompilerDriver.BuildDebugSource(emitter.DebugSites);
            Require(source.Contains("(double Value, const double* Values,"), "Named scalar/array debug parameters");
            Require(source.Contains("double Field_Values[2]"), "Nested field debug type");
            Require(!source.Contains("const void* Points"), "Record array debug type");
            Require(!source.Contains("Value = smile_debug_v"), "No uninitialized alias at the source breakpoint");
        });
        tests.Run("Double Select Case stays same-type", () =>
        {
            Valid("Select Case -0.0\nCase 0.0\nPrint 1\nEnd Select\n");
            Invalid("Select Case 1.0\nCase 1\nPrint 1\nEnd Select\n");
            Invalid("Select Case 0.0\nCase -0.0\nPrint 1\nCase 0.0\nPrint 2\nEnd Select\n");
            Valid("Select Case 1.0\nCase 1.0000000000000002\nPrint 1\nCase 1.0\nPrint 2\nEnd Select\n");
        });
    }

    private static SmileAnalysisResult Valid(string source)
    {
        var analysis = SmileLanguage.Analyze(source);
        Require(!analysis.HasErrors, string.Join("; ", analysis.Diagnostics.Select(d => d.Message)));
        return analysis;
    }

    private static SmileAnalysisResult Invalid(string source)
    {
        var analysis = SmileLanguage.Analyze(source);
        Require(analysis.HasErrors, "Expected source error: " + source);
        return analysis;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
