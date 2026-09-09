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
                Require(analysis.Diagnostics.Any(d => d.Code == "SML3900" && d.Location.Span.Start == 6 &&
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
        tests.Run("Numeric diagnostics preserve messages, source paths and exact spans", () =>
        {
            var path = Path.GetFullPath("NumericDiagnostic.smile");
            foreach (var (source, fragment, code, message) in new[] {
                ("Print 1.0 + 1\n", "1.0 + 1", "SML3901",
                    "Double requires same-type arithmetic/comparison operands. Use explicit ToDouble or ToNumber; Mod remains Number-only."),
                ("Print ToDouble(1.0)\n", "1.0", "SML3901",
                    "Built-in 'ToDouble' requires Number; use an explicit conversion."),
                ("Game Window \"Proof\", 100, 100\nPrint Renderer3DDoubleValue(1, 0.0, 0, 0)\n", "0.0", "SML3901",
                    "Built-in 'Renderer3DDoubleValue' requires exact typed arguments; use explicit conversion."),
                ("Const BAD = Sqrt(-1.0)\n", "Sqrt(-1.0)", "SML3902",
                    "Double constant has an invalid domain, conversion or nonfinite result.") })
            {
                var analysis = SmileLanguage.Analyze(source, path);
                Require(analysis.Diagnostics.Any(d => d.Code == code && d.Message == message &&
                    d.Location.FilePath == path && d.Location.Span.Start == source.IndexOf(fragment, StringComparison.Ordinal) &&
                    d.Location.Span.Length == fragment.Length), "Numeric diagnostic contract: " + source);
            }
        });
        tests.Run("ApplicationId diagnostic identities remain separate from numeric diagnostics", () =>
        {
            var numericCodes = new[] { DoubleSemantics.InvalidLiteralDiagnosticCode,
                DoubleSemantics.TypeMismatchDiagnosticCode, DoubleSemantics.CheckedFailureDiagnosticCode };
            Require(numericCodes.SequenceEqual(new[] { "SML3900", "SML3901", "SML3902" }) &&
                numericCodes.Distinct(StringComparer.Ordinal).Count() == 3 &&
                !numericCodes.Intersect(new[] { "SML3800", "SML3801", "SML3802" }).Any(),
                "Distinct numeric ownership without changing ApplicationId identities");
            var path = Path.GetFullPath("IdentityDiagnostic.smileproj");
            foreach (var (properties, code, line) in new[] {
                ("<ApplicationId>Bad.Id</ApplicationId>", "SML3800", 3),
                ("<ApplicationId>smile.one</ApplicationId>\n<ApplicationId>smile.two</ApplicationId>", "SML3801", 4),
                ("<ApplicationId>smile.library</ApplicationId>\n<ProjectKind>Library</ProjectKind>" +
                    "<LibraryName>Proof</LibraryName><Version>1.0.0</Version>", "SML3802", 3) })
            {
                SmileProjectDiagnosticException? error = null;
                try { SmileProjectSourceSet.Parse(path, "<SmileProject>\n<PropertyGroup>\n" + properties +
                    "\n</PropertyGroup>\n</SmileProject>"); }
                catch (SmileProjectDiagnosticException exception) { error = exception; }
                Require(error != null && error.Code == code && error.FilePath == path &&
                    error.Diagnostic.Line == line && error.Diagnostic.Column == 2,
                    "Established ApplicationId code and XML location: " + code);
            }
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
        tests.Run("Native Debug aggregate fields preserve distinct Unicode identities", () =>
        {
            var fixture = Path.Combine(RepositoryTestContext.FindRepositoryRoot(),
                "examples", "DoubleTests", "UnicodeDebug.smile");
            var analysis = Valid(File.ReadAllText(fixture));
            var emitter = new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, true);
            emitter.Emit();
            var source = CompilerDriver.BuildDebugSource(emitter.DebugSites);
            var repeated = new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, true);
            repeated.Emit();
            Require(source == CompilerDriver.BuildDebugSource(repeated.DebugSites),
                "Repeated actual generation is deterministic");
            var aggregates = System.Text.RegularExpressions.Regex.Matches(source,
                @"struct SmileDebug_[A-F0-9]+ \{(?<fields>[^}]+)\};");
            Require(aggregates.Count == 3, "Record, nested record and class views emitted");
            foreach (System.Text.RegularExpressions.Match aggregate in aggregates)
            {
                var fields = System.Text.RegularExpressions.Regex.Matches(
                    aggregate.Groups["fields"].Value, @"\b(Field_[A-Za-z0-9_]+)(?:\[\d+\])?;")
                    .Select(match => match.Groups[1].Value).ToArray();
                Require(fields.Length > 0 && fields.Distinct(StringComparer.Ordinal).Count() == fields.Length,
                    "Every aggregate C member is unique");
            }
            Require(source.Contains("double Field_Caf_;") && source.Contains("double Field_Caf__0;") &&
                source.Contains("double Field_Caf__0_;") && source.Contains("long long Field_Caf_u00E9;") &&
                source.Contains("double Field_Plain;"), "Original ASCII member names remain readable");
            Require(source.Contains("/* SMILE: Café */") && source.Contains("/* SMILE: Cafè */"),
                "Generated field comments retain the source-name correspondence");
            var record = (RecordTypeSymbol)analysis.SemanticModel.Symbols["Sample"].Type;
            Require(record.Fields.Select(field => field.Offset).SequenceEqual(new[] { 0, 8, 16, 24, 32, 40, 48, 96 }),
                "Record offsets unchanged");
            Require(record.Size == 112 && record.Fields[6].Dimensions.SequenceEqual(new[] { 2, 3 }),
                "Nested record size and fixed-array dimensions unchanged");
            var holder = (ClassTypeSymbol)analysis.SemanticModel.Symbols["Box"].Type;
            Require(holder.Fields.Select(field => field.Offset).SequenceEqual(new[] { 0, 8, 16, 24, 32, 40 }) &&
                holder.InstanceSize == 264 && holder.Fields[5].ElementCount == 2,
                "Class offsets and nested record array layout unchanged");
            Require(source.Contains("double Field_Values[6];") &&
                source.Contains(NativeDebugTypes.Name(record) + " Field_Samples[2];"),
                "Actual C views retain fixed-array element types and extents");
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
