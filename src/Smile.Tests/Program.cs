using System.Xml.Linq;
using Smile.Compiler;
using Smile.Language;

var failures = new List<string>();
var passed = 0;

Run("Missing GraphicsBackend defaults to Auto", () =>
{
    var options = Parse("<PropertyGroup />");
    Equal(SmileGraphicsBackend.Auto, options.GraphicsBackend);
});
Run("Auto backend parses", () => Equal(SmileGraphicsBackend.Auto,
    Parse("<PropertyGroup><GraphicsBackend>Auto</GraphicsBackend></PropertyGroup>").GraphicsBackend));
Run("GDI backend parses", () => Equal(SmileGraphicsBackend.GDI,
    Parse("<PropertyGroup><GraphicsBackend>GDI</GraphicsBackend></PropertyGroup>").GraphicsBackend));
Run("DirectX backend parses", () => Equal(SmileGraphicsBackend.DirectX,
    Parse("<PropertyGroup><GraphicsBackend>DirectX</GraphicsBackend></PropertyGroup>").GraphicsBackend));
Run("Backend values follow existing case-insensitive value handling", () => Equal(
    SmileGraphicsBackend.DirectX,
    Parse("<PropertyGroup><GraphicsBackend>directx</GraphicsBackend></PropertyGroup>").GraphicsBackend));
Run("Unknown backend reports a clear diagnostic", () => Throws(
    () => Parse("<PropertyGroup><GraphicsBackend>Vulkan</GraphicsBackend></PropertyGroup>"),
    "Unknown GraphicsBackend value 'Vulkan'. Expected Auto, GDI, or DirectX."));
Run("Numeric backend is rejected", () => Throws(
    () => Parse("<PropertyGroup><GraphicsBackend>9</GraphicsBackend></PropertyGroup>"),
    "Unknown GraphicsBackend value '9'. Expected Auto, GDI, or DirectX."));
Run("Missing VSync defaults to true", () => Equal(true,
    Parse("<PropertyGroup />").VSync));
Run("VSync true parses", () => Equal(true,
    Parse("<PropertyGroup><VSync>true</VSync></PropertyGroup>").VSync));
Run("VSync false parses", () => Equal(false,
    Parse("<PropertyGroup><VSync>false</VSync></PropertyGroup>").VSync));
Run("Unknown VSync reports a clear diagnostic", () => Throws(
    () => Parse("<PropertyGroup><VSync>sometimes</VSync></PropertyGroup>"),
    "Unknown VSync value 'sometimes'. Expected true or false."));
Run("Filled quadrilateral analyzes without errors", () => Equal(false,
    Analyze("GAME WINDOW \"Quad\"\nFILL QUADRILATERAL 0, 0, 20, 0, 20, 20, 0, 20, WHITE\n").HasErrors));
Run("Outlined quadrilateral analyzes without errors", () => Equal(false,
    Analyze("GAME WINDOW \"Quad\"\nDRAW QUADRILATERAL 0, 0, 20, 0, 20, 20, 0, 20, WHITE\n").HasErrors));
Run("Filled quadrilateral records its shared syntax operation", () => Equal(GraphicsOperation.FillQuadrilateral,
    Analyze("GAME WINDOW \"Quad\"\nFILL QUADRILATERAL 0, 0, 20, 0, 20, 20, 0, 20, WHITE\n")
        .SyntaxTree.Root.Statements.OfType<GraphicsStatementSyntax>().Single().Operation));
Run("Outlined quadrilateral records its shared syntax operation", () => Equal(GraphicsOperation.DrawQuadrilateral,
    Analyze("GAME WINDOW \"Quad\"\nDRAW QUADRILATERAL 0, 0, 20, 0, 20, 20, 0, 20, WHITE\n")
        .SyntaxTree.Root.Statements.OfType<GraphicsStatementSyntax>().Single().Operation));
Run("Too few quadrilateral arguments report a parser error", () => Equal(true,
    HasDiagnostic(Analyze("GAME WINDOW \"Quad\"\nFILL QUADRILATERAL 0, 0, 20\n"), "SML2001")));
Run("Too many quadrilateral arguments report a parser error", () => Equal(true,
    HasDiagnostic(Analyze("GAME WINDOW \"Quad\"\nDRAW QUADRILATERAL 0, 0, 20, 0, 20, 20, 0, 20, WHITE, 99\n"), "SML2001")));
Run("Quadrilateral arguments must be numbers", () => Equal(true,
    HasDiagnostic(Analyze("GAME WINDOW \"Quad\"\nFILL QUADRILATERAL TRUE, 0, 20, 0, 20, 20, 0, 20, WHITE\n"), "SML3023")));
Run("ARC is a shared case-insensitive keyword", () => Equal(SyntaxKind.ArcKeyword,
    SyntaxFacts.GetKeywordKind("arc")));
Run("DRAW ARC analyzes without errors", () => Equal(false,
    Analyze("GAME WINDOW \"Arc\"\nDRAW ARC 200, 200, 50, 0, 90, BLUE\n").HasErrors));
Run("DRAW ARC records its shared syntax operation", () => Equal(GraphicsOperation.DrawArc,
    Analyze("GAME WINDOW \"Arc\"\nDRAW ARC 200, 200, 50, 0, 90, BLUE\n")
        .SyntaxTree.Root.Statements.OfType<GraphicsStatementSyntax>().Single().Operation));
Run("DRAW ARC records exactly six arguments", () => Equal(6,
    Analyze("GAME WINDOW \"Arc\"\nDRAW ARC 200, 200, 50, 0, 90, BLUE\n")
        .SyntaxTree.Root.Statements.OfType<GraphicsStatementSyntax>().Single().Arguments.Count));
Run("Too few arc arguments report a parser error", () => Equal(true,
    HasDiagnostic(Analyze("GAME WINDOW \"Arc\"\nDRAW ARC 200, 200, 50\n"), "SML2001")));
Run("Too many arc arguments report a parser error", () => Equal(true,
    HasDiagnostic(Analyze("GAME WINDOW \"Arc\"\nDRAW ARC 200, 200, 50, 0, 90, BLUE, 99\n"), "SML2001")));
Run("DRAW ARC arguments must be numbers", () => Equal(true,
    HasDiagnostic(Analyze("GAME WINDOW \"Arc\"\nDRAW ARC TRUE, 200, 50, 0, 90, BLUE\n"), "SML3023")));
Run("FILL ARC is rejected", () => Equal(true,
    HasDiagnostic(Analyze("GAME WINDOW \"Arc\"\nFILL ARC 200, 200, 50, 0, 90, BLUE\n"), "SML2001")));
Run("KEY_OTHER is the shared built-in number constant 19", () =>
{
    Equal(SyntaxKind.KeyOtherKeyword, SyntaxFacts.GetKeywordKind("key_other"));
    Equal(19L, SyntaxFacts.GetBuiltInConstantValue(SyntaxKind.KeyOtherKeyword));
});
Run("Existing key constants retain their values", () =>
{
    Equal(1L, SyntaxFacts.GetBuiltInConstantValue(SyntaxKind.KeyWKeyword));
    Equal(14L, SyntaxFacts.GetBuiltInConstantValue(SyntaxKind.KeyEnterKeyword));
    Equal(18L, SyntaxFacts.GetBuiltInConstantValue(SyntaxKind.Key2Keyword));
});
Run("Existing graphics statements remain valid", () => Equal(false,
    Analyze("GAME WINDOW \"Existing\"\nFILL RECTANGLE 1, 2, 3, 4, RED\nDRAW CIRCLE 10, 10, 4, WHITE\nDRAW LINE 0, 0, 20, 20, BLUE\n").HasErrors));
Run("Music keywords are shared and case-insensitive", () =>
{
    Equal(SyntaxKind.MusicKeyword, SyntaxFacts.GetKeywordKind("music"));
    Equal(SyntaxKind.PauseKeyword, SyntaxFacts.GetKeywordKind("PaUsE"));
    Equal(SyntaxKind.ResumeKeyword, SyntaxFacts.GetKeywordKind("resume"));
    Equal(SyntaxKind.VolumeKeyword, SyntaxFacts.GetKeywordKind("VOLUME"));
    Equal(true, SyntaxFacts.IsKeyword(SyntaxKind.MusicKeyword));
});
Run("PLAY MUSIC analyzes as non-looping playback", () =>
{
    var music = Music(Analyze("GAME WINDOW \"Music\"\nPLAY MUSIC \"Assets\\Background.mp3\"\n"));
    Equal(MusicOperation.Play, music.Operation);
    Equal(false, music.Loop);
});
Run("PLAY MUSIC LOOP records looping playback", () => Equal(true,
    Music(Analyze("GAME WINDOW \"Music\"\nPLAY MUSIC \"Assets\\Background.mp3\" LOOP\n")).Loop));
Run("PAUSE MUSIC records the shared operation", () => Equal(MusicOperation.Pause,
    Music(Analyze("GAME WINDOW \"Music\"\nPAUSE MUSIC\n")).Operation));
Run("RESUME MUSIC records the shared operation", () => Equal(MusicOperation.Resume,
    Music(Analyze("GAME WINDOW \"Music\"\nRESUME MUSIC\n")).Operation));
Run("STOP MUSIC records the shared operation", () => Equal(MusicOperation.Stop,
    Music(Analyze("GAME WINDOW \"Music\"\nSTOP MUSIC\n")).Operation));
Run("MUSIC VOLUME accepts numeric expressions", () => Equal(false,
    Analyze("GAME WINDOW \"Music\"\nMUSIC VOLUME 25 + 25\n").HasErrors));
Run("Existing PLAY SOUND and STOP SOUND remain shared sound syntax", () =>
{
    var analysis = Analyze("GAME WINDOW \"Sound\"\nPLAY SOUND \"Assets\\Effect.wav\"\nSTOP SOUND\n");
    Equal(false, analysis.HasErrors);
    Equal(2, analysis.SyntaxTree.Root.Statements.OfType<SoundStatementSyntax>().Count());
});
Run("Every music operation requires GAME WINDOW", () =>
{
    var analysis = Analyze("PLAY MUSIC \"Assets\\Background.mp3\"\nPAUSE MUSIC\nRESUME MUSIC\nSTOP MUSIC\nMUSIC VOLUME 50\n");
    Equal(5, analysis.Diagnostics.Count(diagnostic => diagnostic.Code == "SML3023"));
});
Run("PLAY MUSIC rejects an empty path", () => Equal(true,
    HasDiagnostic(Analyze("GAME WINDOW \"Music\"\nPLAY MUSIC \"\"\n"), "SML3026")));
Run("MUSIC VOLUME requires a number", () => Equal(true,
    HasDiagnostic(Analyze("GAME WINDOW \"Music\"\nMUSIC VOLUME \"loud\"\n"), "SML3026")));
Run("PLAY MUSIC without a path reports a parser diagnostic", () => Equal(true,
    HasDiagnostic(Analyze("GAME WINDOW \"Music\"\nPLAY MUSIC\n"), "SML2001")));
Run("PLAY MUSIC rejects a repeated LOOP", () => Equal(true,
    HasDiagnostic(Analyze("GAME WINDOW \"Music\"\nPLAY MUSIC \"Assets\\Background.mp3\" LOOP LOOP\n"), "SML2001")));
Run("PAUSE SOUND is not accepted as music syntax", () => Equal(true,
    HasDiagnostic(Analyze("GAME WINDOW \"Music\"\nPAUSE SOUND\n"), "SML2001")));
Run("MUSIC requires the VOLUME subcommand", () => Equal(true,
    HasDiagnostic(Analyze("GAME WINDOW \"Music\"\nMUSIC 75\n"), "SML2001")));
Run("MUSIC VOLUME without a value reports a parser diagnostic", () => Equal(true,
    HasDiagnostic(Analyze("GAME WINDOW \"Music\"\nMUSIC VOLUME\n"), "SML2001")));
Run("RESUME SOUND is not accepted as music syntax", () => Equal(true,
    HasDiagnostic(Analyze("GAME WINDOW \"Music\"\nRESUME SOUND\n"), "SML2001")));
Run("Bare STOP remains malformed", () => Equal(true,
    HasDiagnostic(Analyze("GAME WINDOW \"Music\"\nSTOP\n"), "SML2001")));
Run("LOAD TEXT FILE keywords are shared and case-insensitive", () =>
{
    Equal(SyntaxKind.FileKeyword, SyntaxFacts.GetKeywordKind("file"));
    Equal(SyntaxKind.IntoKeyword, SyntaxFacts.GetKeywordKind("InTo"));
    Equal(SyntaxKind.CountKeyword, SyntaxFacts.GetKeywordKind("COUNT"));
});
Run("LOAD TEXT FILE analyzes for a one-dimensional array", () => Equal(false,
    Analyze("DIM Bytes[8]\nLOAD TEXT FILE \"sample.txt\" INTO Bytes COUNT ByteCount\n").HasErrors));
Run("LOAD TEXT FILE records its shared syntax", () =>
{
    var load = Analyze("DIM Bytes[8]\nLOAD TEXT FILE \"sample.txt\" INTO Bytes COUNT ByteCount\n")
        .SyntaxTree.Root.Statements.OfType<TextFileLoadStatementSyntax>().Single();
    Equal("sample.txt", load.Path.Value as string);
    Equal("Bytes", load.Destination.Text);
    Equal("ByteCount", load.CountIdentifier.Text);
});
Run("LOAD TEXT FILE rejects an empty path", () => Equal(true,
    HasDiagnostic(Analyze("DIM Bytes[8]\nLOAD TEXT FILE \"\" INTO Bytes COUNT ByteCount\n"), "SML3027")));
Run("LOAD TEXT FILE rejects an unknown destination", () => Equal(true,
    HasDiagnostic(Analyze("LOAD TEXT FILE \"sample.txt\" INTO Bytes COUNT ByteCount\n"), "SML3027")));
Run("LOAD TEXT FILE rejects a scalar destination", () => Equal(true,
    HasDiagnostic(Analyze("Bytes = 0\nLOAD TEXT FILE \"sample.txt\" INTO Bytes COUNT ByteCount\n"), "SML3027")));
Run("LOAD TEXT FILE rejects a two-dimensional destination", () => Equal(true,
    HasDiagnostic(Analyze("DIM Bytes[4, 4]\nLOAD TEXT FILE \"sample.txt\" INTO Bytes COUNT ByteCount\n"), "SML3027")));
Run("Existing persistence LOAD syntax remains valid", () => Equal(false,
    Analyze("LOAD HighScore FROM \"HighScore\" DEFAULT 0\n").HasErrors));
Run("Completion catalog uses shared keywords and built-in signatures", () =>
{
    var completions = SmileCompletionService.GetCompletions(Analyze("PRI"), 3);
    Equal(SmileCompletionKind.Keyword,
        completions.Single(completion => completion.DisplayText == "PRINT").Kind);
    var rgb = completions.Single(completion => completion.DisplayText == "RGB");
    Equal(SmileCompletionKind.BuiltInFunction, rgb.Kind);
    Equal("Built-in function RGB(red, green, blue)", rgb.Description);
    Equal(true, completions.Any(completion => completion.DisplayText == "GAME_CLOSED"));
    Equal(true, completions.Any(completion => completion.DisplayText == "KEY_ENTER"));
    Equal(false, completions.Any(completion => completion.DisplayText == "PRI"));
});
Run("Completion catalog includes visible variables arrays and routines", () =>
{
    const string source = "Score = 1\nDIM Board[4, 5]\nSUB Move(PlayerX)\nStep = 2\nPRINT PlayerX\nEND SUB\nSUB Other()\nHidden = 3\nEND SUB\n";
    var completions = SmileCompletionService.GetCompletions(Analyze(source), source.IndexOf("PRINT", StringComparison.Ordinal));
    Equal(true, completions.Any(completion => completion.DisplayText == "Score"));
    Equal("NUMBER array Board[4, 5]", completions.Single(completion => completion.DisplayText == "Board").Description);
    Equal(true, completions.Any(completion => completion.DisplayText == "PlayerX"));
    Equal(true, completions.Any(completion => completion.DisplayText == "Step"));
    Equal(false, completions.Any(completion => completion.DisplayText == "Hidden"));
    Equal("SUB Move(PlayerX)", completions.Single(completion => completion.DisplayText == "Move").Description);
});
Run("Fixed-step ball speed is identical at 60, 100, 120, and 144 Hz", () =>
{
    var sixtyHz = Enumerable.Repeat(16, 20).Concat(Enumerable.Repeat(17, 40));
    var oneHundredHz = Enumerable.Repeat(10, 100);
    var oneTwentyHz = Enumerable.Repeat(8, 80).Concat(Enumerable.Repeat(9, 40));
    var oneFortyFourHz = Enumerable.Repeat(6, 8).Concat(Enumerable.Repeat(7, 136));
    Equal(300000L, SimulateFixedPoint(sixtyHz, 300000));
    Equal(300000L, SimulateFixedPoint(oneHundredHz, 300000));
    Equal(300000L, SimulateFixedPoint(oneTwentyHz, 300000));
    Equal(300000L, SimulateFixedPoint(oneFortyFourHz, 300000));
});
Run("PaddleBall player paddle moves 360 pixels per second", () =>
    Equal(360000L, SimulateFixedPoint(Enumerable.Repeat(8, 125), 360000)));
Run("Brick Breaker paddle moves 420 pixels per second", () =>
    Equal(420000L, SimulateFixedPoint(Enumerable.Repeat(8, 125), 420000)));
Run("Elapsed clamping bounds catch-up after a long stall", () =>
    Equal(14400L, SimulateFixedPoint(new[] { 500 }, 300000)));
Run("Compiler target defaults to Windows x64", () =>
{
    Equal(true, CompilerOptions.TryParse(new[] { "Program.smile" }, out var options, out _));
    Equal(SmileCompilationTarget.WindowsX64, options.Target);
});
Run("Web compiler target is case-insensitive", () =>
{
    Equal(true, CompilerOptions.TryParse(new[] { "Program.smile", "--target", "WeB", "--output-dir", "Web" }, out var options, out _));
    Equal(SmileCompilationTarget.Web, options.Target);
    Equal("Web", options.OutputDirectory);
});
Run("Web output rejects native output options", () => Equal(false,
    CompilerOptions.TryParse(new[] { "Program.smile", "--target", "web", "--output-dir", "Web", "-o", "Program.exe" }, out _, out _)));
Run("Web emitter lowers integer division arrays routines booleans and frame yield", () =>
{
    const string source = "DIM Values[2]\nSUB SetValue(Index)\nValues[Index] = 9 / 2\nEND SUB\nGAME WINDOW \"Test\" SIZE 320 BY 180\nCALL SetValue(0)\nIF Values[0] = 4 THEN\nSHOW SCREEN\nEND IF\nEND PROGRAM\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var javascript = new WebEmitter(analysis).Emit();
    Equal(true, javascript.Contains("smile.div(9, 2)"));
    Equal(true, javascript.Contains("smile.set("));
    Equal(true, javascript.Contains("await smile.showScreen()"));
});
Run("Web emitter lowers console output waits and screen clearing", () =>
{
    var analysis = Analyze("PRINT TRUE; 42\nWAIT 1 MILLISECONDS\nCLEAR SCREEN\n");
    Equal(false, analysis.HasErrors);
    var javascript = new WebEmitter(analysis).Emit();
    Equal(true, javascript.Contains("smile.print([smile.booleanText(1), 42]"));
    Equal(true, javascript.Contains("await smile.wait(1)"));
    Equal(true, javascript.Contains("smile.clearScreen()"));
});
Run("Web emitter lowers the complete shared game surface", () =>
{
    const string source = "DIM Bytes[8]\nGAME WINDOW \"Test\"\nSUB DrawFrame()\nFILL CIRCLE 10, 10, 4, WHITE\nDRAW LINE 0, 0, 10, 10, WHITE\nSHOW SCREEN\nEND SUB\nCALL DrawFrame()\nIF KEY_HELD(KEY_W) THEN\nPLAY SOUND \"Assets\\Effect.wav\"\nEND IF\nLOAD TEXT FILE \"Maps\\test.map\" INTO Bytes COUNT ByteCount\nPLAY MUSIC \"Assets\\Music.mp3\" LOOP\nMUSIC VOLUME 50\nPAUSE MUSIC\nRESUME MUSIC\nSTOP MUSIC\nFOR Index = 0 TO 2\nEXIT FOR\nEND FOR\nDO\nEXIT DO\nLOOP\nSELECT CASE ByteCount\nCASE 0\nByteCount = 1\nCASE ELSE\nByteCount = 2\nEND SELECT\nEND PROGRAM\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var javascript = new WebEmitter(analysis).Emit();
    Equal(true, javascript.Contains("async function"));
    Equal(true, javascript.Contains("await smile.loadTextFile"));
    Equal(true, javascript.Contains("smile.keyHeld"));
    Equal(true, javascript.Contains("smile.fillCircle"));
    Equal(true, javascript.Contains("smile.playMusic"));
    Equal(true, javascript.Contains("break t_"));
});
Run("Web output writer creates deterministic static files", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "smile-web-output-test-" + Guid.NewGuid().ToString("N"));
    try
    {
        var analysis = Analyze("GAME WINDOW \"Test\"\nSHOW SCREEN\nEND PROGRAM\n");
        WebOutputWriter.Write(directory, new WebEmitter(analysis));
        foreach (var name in new[] { "index.html", "smile-runtime.js", "game.js", "smile.css" })
            Equal(true, File.Exists(Path.Combine(directory, name)));
    }
    finally
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
});
Run("Single-source API remains a one-document startup compilation", () =>
{
    var analysis = SmileLanguage.Analyze("PRINT 1\n", "Single.smile");
    Equal(1, analysis.SyntaxTrees.Count);
    Equal(true, ReferenceEquals(analysis.SyntaxTree, analysis.SyntaxTrees[0]));
    Equal(true, analysis.SyntaxTree.IsStartup);
});
Run("Multi-source API requires at least one document", () => ThrowsContains(
    () => SmileLanguage.Analyze(Array.Empty<SmileSourceDocument>()),
    "requires at least one source document"));
Run("Multi-source API requires exactly one startup document", () => ThrowsContains(
    () => SmileLanguage.Analyze(new[]
    {
        new SmileSourceDocument("PRINT 1\n", "One.smile", true),
        new SmileSourceDocument("PRINT 2\n", "Two.smile", true)
    }),
    "requires exactly one startup source; found 2"));
Run("Multi-source API rejects duplicate normalized paths", () => ThrowsContains(
    () => SmileLanguage.Analyze(new[]
    {
        new SmileSourceDocument("PRINT 1\n", "Duplicate.smile", true),
        new SmileSourceDocument("SUB Work()\nEND SUB\n", ".\\Duplicate.smile")
    }),
    "Duplicate SMILE source path"));
Run("Multi-source analysis exposes distinct physical syntax trees", () =>
{
    var analysis = Multi(
        ("Program.smile", true, "CALL Work()\n"),
        ("Support.smile", false, "SUB Work()\nPRINT 1\nEND SUB\n"));
    Equal(2, analysis.SyntaxTrees.Count);
    Equal("Program.smile", Path.GetFileName(analysis.SyntaxTree.Source.FilePath));
    Equal("Support.smile", Path.GetFileName(analysis.SyntaxTrees[1].Source.FilePath));
});
Run("Cross-file routines declarations arrays and startup globals bind together", () =>
{
    var analysis = Multi(
        ("Program.smile", true, "Score = 7\nCALL ResetState()\nPRINT StateValue()\n"),
        ("GameState.smile", false, "CONST BaseValue = 3\nDIM State[2]\nSUB ResetState()\nState[0] = BaseValue\nCALL AdvanceState()\nEND SUB\n"),
        ("Drawing.smile", false, "SUB AdvanceState()\nState[0] = State[0] + Score\nEND SUB\nFUNCTION StateValue()\nRETURN State[0]\nEND FUNCTION\n"));
    Equal(false, analysis.HasErrors);
    Equal(true, analysis.SemanticModel.Symbols.ContainsKey("Score"));
    Equal(true, analysis.SemanticModel.Symbols.ContainsKey("State"));
    Equal(true, analysis.SemanticModel.Routines.ContainsKey("AdvanceState"));
});
Run("Cross-file routine visibility does not depend on support source order", () => Equal(false,
    Multi(
        ("Program.smile", true, "CALL First()\n"),
        ("Later.smile", false, "SUB First()\nCALL Second()\nEND SUB\n"),
        ("Earlier.smile", false, "SUB Second()\nEND SUB\n")).HasErrors));
Run("Cross-file constants and array dimensions are source-order independent", () =>
{
    var analysis = Multi(
        ("Program.smile", true, "DIM StartupValues[MaximumValues]\nCALL InitializeArrays()\nPRINT MaximumValues\n"),
        ("Arrays.smile", false, "DIM SharedValues[MaximumValues]\nSUB InitializeArrays()\nSharedValues[0] = MaximumValues\nEND SUB\n"),
        ("Derived.smile", false, "CONST MaximumValues = BaseValues + ExtraValues\n"),
        ("Base.smile", false, "CONST BaseValues = 4\nCONST ExtraValues = 4\n"));
    Equal(false, analysis.HasErrors);
    Equal(8L, analysis.SemanticModel.Symbols["MaximumValues"].ConstantValue);
    Equal(8, analysis.SemanticModel.Symbols["StartupValues"].ArrayDimensions[0]);
    Equal(8, analysis.SemanticModel.Symbols["SharedValues"].ArrayDimensions[0]);
});
Run("Reversing support declaration order preserves constant and array results", () =>
{
    var analysis = Multi(
        ("Program.smile", true, "DIM StartupValues[MaximumValues]\nPRINT MaximumValues\n"),
        ("Base.smile", false, "CONST BaseValues = 3\nCONST ExtraValues = 1\n"),
        ("Derived.smile", false, "CONST MaximumValues = BaseValues + ExtraValues\n"),
        ("Arrays.smile", false, "DIM SharedValues[MaximumValues]\n"));
    Equal(false, analysis.HasErrors);
    Equal(4L, analysis.SemanticModel.Symbols["MaximumValues"].ConstantValue);
    Equal(4, analysis.SemanticModel.Symbols["SharedValues"].ArrayDimensions[0]);
});
Run("Circular constants report one deterministic physical-file diagnostic", () =>
{
    var diagnostic = Multi(
        ("Program.smile", true, "PRINT FirstValue\n"),
        ("First.smile", false, "CONST FirstValue = SecondValue + 1\n"),
        ("Second.smile", false, "CONST SecondValue = FirstValue + 1\n"))
        .Diagnostics.Single(item => item.Code == "SML3029");
    Equal("First.smile", Path.GetFileName(diagnostic.FilePath));
    Equal(true, diagnostic.Message.Contains("FirstValue -> SecondValue -> FirstValue", StringComparison.Ordinal));
});
Run("CONST and routine names share one case-insensitive project namespace", () =>
{
    var diagnostic = Multi(
        ("Program.smile", true, "PRINT SharedName\n"),
        ("Value.smile", false, "CONST SharedName = 1\n"),
        ("Routine.smile", false, "SUB sharedname()\nEND SUB\n"))
        .Diagnostics.Single(item => item.Code == "SML3005");
    Equal("Routine.smile", Path.GetFileName(diagnostic.FilePath));
});
Run("DIM and routine names share one case-insensitive project namespace", () =>
{
    var diagnostic = Multi(
        ("Program.smile", true, "DIM Inventory[4]\n"),
        ("Routine.smile", false, "FUNCTION inventory()\nRETURN 1\nEND FUNCTION\n"))
        .Diagnostics.Single(item => item.Code == "SML3005");
    Equal("Routine.smile", Path.GetFileName(diagnostic.FilePath));
});
Run("Implicit startup globals share the project routine namespace", () =>
{
    var diagnostic = Multi(
        ("Program.smile", true, "Score = 1\nPRINT Score\n"),
        ("Routine.smile", false, "FUNCTION score()\nRETURN 1\nEND FUNCTION\n"))
        .Diagnostics.Single(item => item.Code == "SML3005");
    Equal("Routine.smile", Path.GetFileName(diagnostic.FilePath));
});
Run("Game hierarchy projection includes startup alternate support and assets exactly once", () =>
{
    var projectPath = Path.GetFullPath("examples/SourceVisibilityBasics/SourceVisibilityBasics.smileproj");
    var sourceSet = SmileProjectSourceSet.Load(projectPath);
    var projection = SmileProjectHierarchyProjection.Create(sourceSet, "Game", new[] { "Assets\\**\\*" });
    Equal("References|Program.smile|Program-NoDemo.smile|Helpers.smile|Assets|Readme.txt",
        string.Join("|", projection.Select(item => item.Caption)));
    foreach (var source in sourceSet.Items)
        Equal(1, projection.Count(item => item.Kind == SmileProjectHierarchyItemKind.Source &&
            string.Equals(item.FullPath, source.FullPath, StringComparison.OrdinalIgnoreCase)));
    Equal(projection.Count, projection.Select(item => item.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count());
});
Run("Console hierarchy projection contains every support source without an Assets node", () =>
{
    var sourceSet = ProjectSources("""
        <SmileProject><PropertyGroup><ProjectKind>Console</ProjectKind><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup>
        <SmileSource Include="Program.smile" StartupOnly="true" />
        <SmileSource Include="Second.smile" />
        <SmileSource Include="Third.smile" />
        </ItemGroup></SmileProject>
        """);
    var projection = SmileProjectHierarchyProjection.Create(sourceSet, "Console", Array.Empty<string>());
    Equal("References|Program.smile|Second.smile|Third.smile", string.Join("|", projection.Select(item => item.Caption)));
});
Run("Root hierarchy traversal reaches every source once without missing IDs cycles or Assets ordering gaps", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileHierarchyTraversalTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(Path.Combine(directory, "Assets"));
    try
    {
        foreach (var name in new[] { "Program.smile", "BeforeAssets.smile", "AfterAssets.smile" })
            File.WriteAllText(Path.Combine(directory, name), "END PROGRAM\n");
        File.WriteAllText(Path.Combine(directory, "Assets", "Readme.txt"), "asset\n");
        var projectPath = Path.Combine(directory, "Traversal.smileproj");
        File.WriteAllText(projectPath, """
            <SmileProject><PropertyGroup><ProjectKind>Game</ProjectKind><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup>
            <SmileSource Include="Program.smile" StartupOnly="true" />
            <SmileSource Include="BeforeAssets.smile" />
            <Asset Include="Assets\**\*" />
            <SmileSource Include="AfterAssets.smile" />
            </ItemGroup></SmileProject>
            """);

        var sourceSet = SmileProjectSourceSet.Load(projectPath);
        var projection = SmileProjectHierarchyProjection.Create(sourceSet, "Game", new[] { "Assets\\**\\*" });
        var ids = new SmileProjectHierarchyIdentityMap().Apply(projection);
        var rootItems = projection.Where(item => item.ParentPath == null).ToArray();
        var nextById = rootItems.Select((item, index) => new
            {
                Id = ids[item.Key],
                Next = index + 1 < rootItems.Length ? ids[rootItems[index + 1].Key] : uint.MaxValue
            })
            .ToDictionary(item => item.Id, item => item.Next);
        var reached = new List<uint>();
        for (var itemId = ids[rootItems[0].Key]; itemId != uint.MaxValue; itemId = nextById[itemId])
        {
            Equal(false, reached.Contains(itemId));
            reached.Add(itemId);
        }

        Equal(rootItems.Length, reached.Count);
        Equal(rootItems.Length, reached.Distinct().Count());
        foreach (var source in sourceSet.Items)
            Equal(1, rootItems.Count(item => item.Kind == SmileProjectHierarchyItemKind.Source &&
                string.Equals(item.FullPath, source.FullPath, StringComparison.OrdinalIgnoreCase)));
        Equal("References|Program.smile|BeforeAssets.smile|AfterAssets.smile|Assets",
            string.Join("|", rootItems.Select(item => item.Caption)));
    }
    finally
    {
        Directory.Delete(directory, true);
    }
});
Run("Hierarchy mutation preserves existing IDs and remove re-add keeps the physical source", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileHierarchyTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var projectPath = Path.Combine(directory, "Visibility.smileproj");
        var programPath = Path.Combine(directory, "Program.smile");
        var supportPath = Path.Combine(directory, "Support.smile");
        var dynamicPath = Path.Combine(directory, "Dynamic.smile");
        File.WriteAllText(programPath, "END PROGRAM\n");
        File.WriteAllText(supportPath, "CONST Existing = 1\n");
        File.WriteAllText(dynamicPath, "CONST Dynamic = 2\n");
        File.WriteAllText(projectPath, "<SmileProject><PropertyGroup><ProjectKind>Console</ProjectKind><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" StartupOnly=\"true\" /><SmileSource Include=\"Support.smile\" /></ItemGroup></SmileProject>");
        var identities = new SmileProjectHierarchyIdentityMap();
        var initial = SmileProjectHierarchyProjection.Create(SmileProjectSourceSet.Load(projectPath), "Console", Array.Empty<string>());
        var initialIds = identities.Apply(initial);
        var addedSet = SmileProjectFileEditor.AddSource(projectPath, dynamicPath);
        var blankLinesAfterAdd = File.ReadAllLines(projectPath).Count(string.IsNullOrWhiteSpace);
        var added = SmileProjectHierarchyProjection.Create(addedSet, "Console", Array.Empty<string>());
        var addedIds = identities.Apply(added);
        Equal(initial.Count + 1, added.Count);
        foreach (var item in initial)
            Equal(initialIds[item.Key], addedIds[item.Key]);
        var dynamicItem = added.Single(item => string.Equals(item.FullPath, dynamicPath, StringComparison.OrdinalIgnoreCase));
        Equal(true, addedIds[dynamicItem.Key] is > 0 and < 0xfffffffd);
        ThrowsContains(() => SmileProjectFileEditor.AddSource(projectPath, dynamicPath), "already included in the project");
        var removedSet = SmileProjectFileEditor.RemoveSource(projectPath, dynamicPath);
        Equal(false, SmileProjectHierarchyProjection.Create(removedSet, "Console", Array.Empty<string>())
            .Any(item => string.Equals(item.FullPath, dynamicPath, StringComparison.OrdinalIgnoreCase)));
        Equal(true, File.Exists(dynamicPath));
        var readdedSet = SmileProjectFileEditor.AddSource(projectPath, dynamicPath);
        var readded = SmileProjectHierarchyProjection.Create(readdedSet, "Console", Array.Empty<string>());
        Equal(1, readded.Count(item => string.Equals(item.FullPath, dynamicPath, StringComparison.OrdinalIgnoreCase)));
        Equal(addedIds[dynamicItem.Key], identities.Apply(readded)[dynamicItem.Key]);
        var reloaded = SmileProjectHierarchyProjection.Create(SmileProjectSourceSet.Load(projectPath), "Console", Array.Empty<string>());
        Equal(string.Join("|", readded.Select(item => item.Key)), string.Join("|", reloaded.Select(item => item.Key)));
        var finalBlankLines = File.ReadAllLines(projectPath).Count(string.IsNullOrWhiteSpace);
        Equal(blankLinesAfterAdd, finalBlankLines);
    }
    finally
    {
        Directory.Delete(directory, true);
    }
});
Run("Included missing sources stay projected while untracked files remain excluded", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileMissingHierarchyTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var programPath = Path.Combine(directory, "Program.smile");
        var missingPath = Path.Combine(directory, "Missing.smile");
        var untrackedPath = Path.Combine(directory, "Untracked.smile");
        File.WriteAllText(programPath, "END PROGRAM\n");
        File.WriteAllText(untrackedPath, "CONST Untracked = 1\n");
        var projectPath = Path.Combine(directory, "Missing.smileproj");
        File.WriteAllText(projectPath, "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" StartupOnly=\"true\" /><SmileSource Include=\"Missing.smile\" /></ItemGroup></SmileProject>");

        var sourceSet = SmileProjectSourceSet.Load(projectPath);
        var missingProjection = SmileProjectHierarchyProjection.Create(sourceSet, "Console", Array.Empty<string>());
        Equal(3, missingProjection.Count);
        Equal(false, missingProjection.Single(item => string.Equals(item.FullPath, missingPath,
            StringComparison.OrdinalIgnoreCase)).Exists);
        Equal(false, missingProjection.Any(item => string.Equals(item.FullPath, untrackedPath,
            StringComparison.OrdinalIgnoreCase)));
        ThrowsContains(sourceSet.ValidateFiles, "Support source file was not found");

        File.WriteAllText(missingPath, "CONST Restored = 1\n");
        var restoredProjection = SmileProjectHierarchyProjection.Create(
            SmileProjectSourceSet.Load(projectPath), "Console", Array.Empty<string>());
        Equal(true, restoredProjection.Single(item => string.Equals(item.FullPath, missingPath,
            StringComparison.OrdinalIgnoreCase)).Exists);
        SmileProjectSourceSet.Load(projectPath).ValidateFiles();
    }
    finally
    {
        Directory.Delete(directory, true);
    }
});
Run("One physical source can be owned by multiple SMILE projects", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileOwnershipTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var sharedPath = Path.Combine(directory, "Shared.smile");
        File.WriteAllText(sharedPath, "CONST Shared = 1\n");
        File.WriteAllText(Path.Combine(directory, "One.smile"), "PRINT Shared\n");
        File.WriteAllText(Path.Combine(directory, "Two.smile"), "PRINT Shared\n");
        var onePath = Path.Combine(directory, "One.smileproj");
        var twoPath = Path.Combine(directory, "Two.smileproj");
        File.WriteAllText(onePath, "<SmileProject><PropertyGroup><StartupFile>One.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"One.smile\" StartupOnly=\"true\" /><SmileSource Include=\"Shared.smile\" /></ItemGroup></SmileProject>");
        File.WriteAllText(twoPath, "<SmileProject><PropertyGroup><StartupFile>Two.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Two.smile\" StartupOnly=\"true\" /><SmileSource Include=\"Shared.smile\" /></ItemGroup></SmileProject>");
        var ownership = new SmileProjectOwnershipIndex();
        ownership.Register(SmileProjectSourceSet.Load(onePath));
        ownership.Register(SmileProjectSourceSet.Load(twoPath));
        Equal(2, ownership.GetOwners(sharedPath).Count);
        Equal(true, ownership.Contains(onePath, sharedPath));
        Equal(true, ownership.Contains(twoPath, sharedPath));
        ownership.Unregister(onePath);
        Equal(1, ownership.GetOwners(sharedPath).Count);
        Equal(true, ownership.Contains(twoPath, sharedPath));
    }
    finally
    {
        Directory.Delete(directory, true);
    }
});
Run("Disposing an open-buffer registration releases its text and invalidation callback", () =>
{
    var registry = new SmileOpenBufferRegistry();
    var filePath = Path.Combine(Path.GetTempPath(), "SmileBuffer-" + Guid.NewGuid().ToString("N") + ".smile");
    var invalidations = 0;
    var registration = registry.Register(filePath, "PRINT 1\n", () => invalidations++);
    Equal(1, registry.OpenBufferCount);
    Equal(1, registry.GetInvalidationCount(filePath));
    foreach (var callback in registry.GetInvalidations(new[] { filePath }))
        callback();
    Equal(1, invalidations);
    registration.Dispose();
    Equal(0, registry.OpenBufferCount);
    Equal(0, registry.GetInvalidationCount(filePath));
    Equal(0, registry.GetInvalidations(new[] { filePath }).Count);
});
Run("Support executable top-level statements report their physical file", () =>
{
    var analysis = Multi(
        ("Program.smile", true, "PRINT 1\n"),
        ("Support.smile", false, "\nScore = 1\n"));
    var diagnostic = analysis.Diagnostics.Single(item => item.Code == "SML3028");
    Equal("Support.smile", Path.GetFileName(diagnostic.FilePath));
    Equal(2, diagnostic.Line);
});
Run("Support GAME WINDOW is rejected in the support file", () =>
{
    var diagnostic = Multi(
        ("Program.smile", true, "PRINT 1\n"),
        ("Support.smile", false, "GAME WINDOW \"Wrong\"\n"))
        .Diagnostics.Single(item => item.Code == "SML3028");
    Equal(true, diagnostic.Message.Contains("GAME WINDOW"));
    Equal("Support.smile", Path.GetFileName(diagnostic.FilePath));
});
Run("Support END PROGRAM is rejected in the support file", () =>
{
    var diagnostic = Multi(
        ("Program.smile", true, "PRINT 1\n"),
        ("Support.smile", false, "END PROGRAM\n"))
        .Diagnostics.Single(item => item.Code == "SML3028");
    Equal(true, diagnostic.Message.Contains("END PROGRAM"));
});
Run("Duplicate globals across files report the later file", () =>
{
    var diagnostic = Multi(
        ("Program.smile", true, "PRINT Shared\n"),
        ("First.smile", false, "CONST Shared = 1\n"),
        ("Second.smile", false, "DIM shared[2]\n"))
        .Diagnostics.Single(item => item.Code == "SML3005");
    Equal("Second.smile", Path.GetFileName(diagnostic.FilePath));
});
Run("Duplicate routines across files report the later file", () =>
{
    var diagnostic = Multi(
        ("Program.smile", true, "CALL Work()\n"),
        ("First.smile", false, "SUB Work()\nEND SUB\n"),
        ("Second.smile", false, "SUB work()\nEND SUB\n"))
        .Diagnostics.Single(item => item.Code == "SML3015");
    Equal("Second.smile", Path.GetFileName(diagnostic.FilePath));
});
Run("Parser diagnostics retain support-file line and column", () =>
{
    var diagnostic = Multi(
        ("Program.smile", true, "PRINT 1\n"),
        ("Broken.smile", false, "SUB Work()\n\nPRINT (\nEND SUB\n"))
        .Diagnostics.First(item => item.Code.StartsWith("SML2", StringComparison.Ordinal));
    Equal("Broken.smile", Path.GetFileName(diagnostic.FilePath));
    Equal(3, diagnostic.Line);
});
Run("Cross-file completion uses the active support file scope", () =>
{
    const string support = "SUB Move(Amount)\nLocalStep = 1\nPRINT Amount\nEND SUB\n";
    var analysis = Multi(
        ("Program.smile", true, "Score = 1\nCALL Move(2)\n"),
        ("Support.smile", false, support));
    var completions = SmileCompletionService.GetCompletions(
        analysis, analysis.GetSyntaxTree(Path.GetFullPath("Support.smile")), support.IndexOf("PRINT", StringComparison.Ordinal));
    Equal(true, completions.Any(item => item.DisplayText == "Score"));
    Equal(true, completions.Any(item => item.DisplayText == "Amount"));
    Equal(true, completions.Any(item => item.DisplayText == "LocalStep"));
});
Run("Compiler options accept repeated support sources", () =>
{
    Equal(true, CompilerOptions.TryParse(new[]
    {
        "Program.smile", "--source", "GameState.smile", "--source", "Drawing.smile", "-o", "Game.exe"
    }, out var options, out _));
    Equal(2, options.SourcePaths.Count);
    Equal("Drawing.smile", options.SourcePaths[1]);
});
Run("Project source selection honors StartupOnly and project order", () =>
{
    var sourceSet = ProjectSources("""
        <SmileProject><PropertyGroup><StartupFile>Program-NoDemo.smile</StartupFile></PropertyGroup><ItemGroup>
        <SmileSource Include="Program.smile" StartupOnly="TRUE" />
        <SmileSource Include="GameState.smile" />
        <SmileSource Include="Program-NoDemo.smile" StartupOnly="true" />
        <SmileSource Include="Drawing.smile" />
        </ItemGroup></SmileProject>
        """);
    Equal("Program-NoDemo.smile", sourceSet.StartupSource!.Include);
    Equal(3, sourceSet.CompilationSources.Count);
    Equal("GameState.smile", sourceSet.SupportSources[0].Include);
    Equal("Drawing.smile", sourceSet.SupportSources[1].Include);
    Equal(false, sourceSet.Items.Single(item => item.Include == "GameState.smile").StartupOnly);
});
Run("Alternate startup analysis excludes the selected complete program", () =>
{
    var sourceSet = ProjectSources("""
        <SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup>
        <SmileSource Include="Program.smile" StartupOnly="true" />
        <SmileSource Include="GameState.smile" />
        <SmileSource Include="Program-NoDemo.smile" StartupOnly="true" />
        </ItemGroup></SmileProject>
        """);
    var alternate = sourceSet.GetCompilationSourcesFor(Path.GetFullPath("Program-NoDemo.smile"));
    Equal(2, alternate.Count);
    Equal("Program-NoDemo.smile", alternate[0].Include);
    Equal("GameState.smile", alternate[1].Include);
    Equal(false, alternate.Any(source => source.Include == "Program.smile"));
});
Run("Project sources reject non-SMILE includes", () => Throws(
    () => ProjectSources("<SmileProject><PropertyGroup><StartupFile>Program.txt</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.txt\" /></ItemGroup></SmileProject>"),
    "SmileSource Include must name a .smile source file: 'Program.txt'."));
Run("Missing project documents report a clear physical-file diagnostic", () =>
{
    var missingPath = Path.GetFullPath("Missing.smile");
    var analysis = SmileLanguage.Analyze(new[] { new SmileSourceDocument(string.Empty, missingPath, true, true) });
    Equal(true, HasDiagnostic(analysis, "SML0001"));
    Equal(missingPath, analysis.Diagnostics.Single(diagnostic => diagnostic.Code == "SML0001").FilePath);
});
Run("Project file mutations preserve properties assets and physical files", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileProjectTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var projectPath = Path.Combine(directory, "Test.smileproj");
        var programPath = Path.Combine(directory, "Program.smile");
        var alternatePath = Path.Combine(directory, "Alternate.smile");
        var supportPath = Path.Combine(directory, "Support.smile");
        File.WriteAllText(programPath, "END PROGRAM\n");
        File.WriteAllText(alternatePath, "END PROGRAM\n");
        File.WriteAllText(supportPath, "CONST Value = 1\n");
        File.WriteAllText(projectPath, """
            <SmileProject Version="1.0">
              <PropertyGroup><StartupFile>Program.smile</StartupFile><OutputName>Kept</OutputName></PropertyGroup>
              <ItemGroup><SmileSource Include="Program.smile" StartupOnly="true" /><SmileSource Include="Alternate.smile" StartupOnly="true" /></ItemGroup>
              <ItemGroup><Asset Include="Assets\**\*" /></ItemGroup>
            </SmileProject>
            """);

        var added = SmileProjectFileEditor.AddSource(projectPath, supportPath);
        Equal(true, added.Items.Any(source => source.Include == "Support.smile"));
        var alternate = SmileProjectFileEditor.SetStartup(projectPath, alternatePath);
        Equal("Alternate.smile", alternate.StartupFile);
        var support = SmileProjectFileEditor.IncludeAsSupport(projectPath, programPath);
        Equal(true, support.Items.Single(source => source.Include == "Program.smile").IsSupport);
        var removed = SmileProjectFileEditor.RemoveSource(projectPath, supportPath);
        Equal(false, removed.Items.Any(source => source.Include == "Support.smile"));
        Equal(true, File.Exists(supportPath));
        var saved = File.ReadAllText(projectPath);
        Equal(true, saved.Contains("<OutputName>Kept</OutputName>"));
        Equal(true, saved.Contains("<Asset Include=\"Assets\\**\\*\""));
    }
    finally
    {
        Directory.Delete(directory, true);
    }
});
Run("Invalid StartupOnly values report a clear project error", () => Throws(
    () => ProjectSources("<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" StartupOnly=\"sometimes\" /></ItemGroup></SmileProject>"),
    "Unknown StartupOnly value 'sometimes'. Expected true or false."));
Run("Project source parsing rejects duplicate paths", () => Throws(
    () => ProjectSources("<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" /><SmileSource Include=\".\\Program.smile\" /></ItemGroup></SmileProject>"),
    "Duplicate SmileSource path '.\\Program.smile'."));
Run("Project source parsing requires the selected startup item", () => Throws(
    () => ProjectSources("<SmileProject><PropertyGroup><StartupFile>Other.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" /></ItemGroup></SmileProject>"),
    "StartupFile 'Other.smile' is not listed as a SmileSource."));
Run("Multi-file debug sites are unique and retain real source paths", () =>
{
    var analysis = Multi(
        ("Program.smile", true, "Score = 1\nCALL Work()\n"),
        ("Support.smile", false, "SUB Work()\nScore = Score + 1\nEND SUB\n"));
    var emitter = new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, true);
    var assembly = emitter.Emit();
    var lineTwoSites = emitter.DebugSites.Where(site => site.Line == 2).ToArray();
    Equal(2, lineTwoSites.Length);
    Equal(false, lineTwoSites[0].HelperName == lineTwoSites[1].HelperName);
    Equal(false, lineTwoSites[0].Source.FilePath == lineTwoSites[1].Source.FilePath);
    var debugSource = CompilerDriver.BuildDebugSource(lineTwoSites);
    Equal(true, debugSource.Contains(Path.GetFullPath("Program.smile").Replace("\\", "\\\\")));
    Equal(true, debugSource.Contains(Path.GetFullPath("Support.smile").Replace("\\", "\\\\")));
    Equal(true, assembly.Contains(lineTwoSites[0].HelperName));
});
Run("Web target failures retain the support source path", () =>
{
    var analysis = Multi(
        ("Program.smile", true, "PRINT HugeValue()\n"),
        ("Support.smile", false, "FUNCTION HugeValue()\nRETURN 9007199254740992\nEND FUNCTION\n"));
    try
    {
        _ = new WebEmitter(analysis).Emit();
        throw new InvalidOperationException("Expected a Web target diagnostic.");
    }
    catch (WebTargetException exception)
    {
        Equal("Support.smile", Path.GetFileName(exception.SourceText.FilePath));
        Equal("SML5102", exception.Code);
    }
});
Run("Web emitter emits support routines but only startup top-level execution", () =>
{
    var analysis = Multi(
        ("Program.smile", true, "Score = 1\nCALL AddOne()\nPRINT Score\n"),
        ("Support.smile", false, "SUB AddOne()\nScore = Score + 1\nEND SUB\n"));
    Equal(false, analysis.HasErrors);
    var javascript = new WebEmitter(analysis).Emit();
    Equal(true, javascript.Contains("async function r_0_addone"));
    Equal(true, javascript.Contains("await r_0_addone()"));
    Equal(1, javascript.Split(new[] { "smile.print" }, StringSplitOptions.None).Length - 1);
});
Run("Local modules import public members through a qualified alias", () =>
{
    var analysis = Multi(
        ("Program.smile", true, "IMPORT Example.Math AS Math\nPRINT Math.Double(21)\nEND PROGRAM\n"),
        ("Math.smile", false, "MODULE Example.Math\nPUBLIC FUNCTION Double(Value)\nRETURN Value * 2\nEND FUNCTION\nPRIVATE CONST Secret = 9\nEND MODULE\n"));
    Equal(false, analysis.HasErrors);
    Equal(true, analysis.SemanticModel.Modules.ContainsKey("Example.Math"));
    Equal(true, new WebEmitter(analysis).Emit().Contains("await r_"));
    Equal(true, new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, false).Emit().Contains("call smile_"));
});
Run("Private module members are rejected across import boundaries", () =>
{
    var analysis = Multi(
        ("Program.smile", true, "IMPORT Example.Math AS Math\nPRINT Math.Secret\n"),
        ("Math.smile", false, "MODULE Example.Math\nPRIVATE CONST Secret = 9\nEND MODULE\n"));
    Equal(true, HasDiagnostic(analysis, "SML3105"));
});
Run("Missing modules aliases members and import cycles have stable diagnostics", () =>
{
    Equal(true, HasDiagnostic(Multi(("Program.smile", true, "IMPORT Missing.Module AS Missing\n")), "SML3102"));
    Equal(true, HasDiagnostic(Multi(
        ("Program.smile", true, "IMPORT Example.Math AS Math\nPRINT Math.Unknown\n"),
        ("Math.smile", false, "MODULE Example.Math\nPUBLIC CONST Value = 1\nEND MODULE\n")), "SML3103"));
    Equal(true, HasDiagnostic(Multi(
        ("Program.smile", true, "IMPORT Example.Alpha AS Alpha\n"),
        ("A.smile", false, "MODULE Example.Alpha\nIMPORT Example.Beta AS Beta\nPUBLIC CONST AValue = 1\nEND MODULE\n"),
        ("B.smile", false, "MODULE Example.Beta\nIMPORT Example.Alpha AS Alpha\nPUBLIC CONST BValue = 1\nEND MODULE\n")), "SML3108"));
});
Run("Alias dot completion exposes only public module members", () =>
{
    var text = "IMPORT Example.Math AS Math\nPRINT Math.";
    var analysis = Multi(
        ("Program.smile", true, text),
        ("Math.smile", false, "MODULE Example.Math\nPUBLIC FUNCTION Double(Value)\nRETURN Value * 2\nEND FUNCTION\nPRIVATE CONST Secret = 9\nEND MODULE\n"));
    var completions = SmileCompletionService.GetCompletions(analysis, text.Length);
    Equal(true, completions.Any(item => item.DisplayText == "Double"));
    Equal(false, completions.Any(item => item.DisplayText == "Secret"));
});
Run("Library projects have no startup and support project and package references", () =>
{
    var library = ProjectSources("<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Example.Tools</LibraryName><Version>1.2.3</Version></PropertyGroup><ItemGroup><SmileSource Include=\"Module.smile\" /><SmileLibraryReference Include=\"Tools.smilelib\" /></ItemGroup></SmileProject>");
    Equal(true, library.IsLibrary);
    Equal(true, library.StartupSource == null);
    Equal(1, library.References.Count);
    Equal(SmileProjectReferenceKind.Package, library.References[0].Kind);
});
Run("Reference editing refresh projection immediately and never deletes the target", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileReferenceTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var project = Path.Combine(directory, "App.smileproj");
        var package = Path.Combine(directory, "Tools.smilelib");
        File.WriteAllText(Path.Combine(directory, "Program.smile"), "END PROGRAM\n");
        File.WriteAllText(package, "fixture");
        File.WriteAllText(project, "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" /></ItemGroup></SmileProject>");
        var added = SmileProjectFileEditor.AddReference(project, package);
        Equal(1, added.References.Count);
        Equal(1, SmileProjectHierarchyProjection.Create(added, "Console", Array.Empty<string>())
            .Count(item => item.Kind == SmileProjectHierarchyItemKind.Reference));
        var removed = SmileProjectFileEditor.RemoveReference(project, package);
        Equal(0, removed.References.Count);
        Equal(true, File.Exists(package));
    }
    finally { Directory.Delete(directory, true); }
});
Run("Library packages are deterministic and reload through authoritative analysis", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmilePackageTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var projectPath = Path.Combine(directory, "Tools.smilelibproj");
        var sourcePath = Path.Combine(directory, "Tools.smile");
        File.WriteAllText(sourcePath, "MODULE Example.Tools\nPUBLIC FUNCTION Double(Value)\nRETURN Value * 2\nEND FUNCTION\nPRIVATE CONST Hidden = 1\nEND MODULE\n");
        File.WriteAllText(projectPath, "<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Example.Tools</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include=\"Tools.smile\" /></ItemGroup></SmileProject>");
        var compilation = SmileProjectCompilation.Load(projectPath);
        var analysis = SmileLanguage.Analyze(compilation.Sources, SmileCompilationKind.Library);
        Equal(false, analysis.HasErrors);
        var first = Path.Combine(directory, "first.smilelib");
        var second = Path.Combine(directory, "second.smilelib");
        SmileLibraryPackage.Write(first, compilation.Graph.Root, analysis);
        SmileLibraryPackage.Write(second, compilation.Graph.Root, analysis);
        Equal(true, File.ReadAllBytes(first).SequenceEqual(File.ReadAllBytes(second)));
        var loaded = SmileLibraryPackage.Read(first, Path.Combine(directory, "obj"));
        Equal("Example.Tools", loaded.Identity.Name);
        Equal(1, loaded.Sources.Count);
        using (var archive = System.IO.Compression.ZipFile.OpenRead(first))
        {
            Equal(true, archive.GetEntry("manifest.json") != null);
            var apiEntry = archive.GetEntry("api/public-symbols.json")!;
            using var reader = new StreamReader(apiEntry.Open());
            var api = reader.ReadToEnd();
            Equal(true, api.Contains("Double", StringComparison.Ordinal));
            Equal(false, api.Contains("Hidden", StringComparison.Ordinal));
        }
        File.WriteAllText(sourcePath, "MODULE Example.Tools\nPUBLIC FUNCTION Triple(Value)\nRETURN Value * 3\nEND FUNCTION\nEND MODULE\n");
        var changedCompilation = SmileProjectCompilation.Load(projectPath);
        var changedAnalysis = SmileLanguage.Analyze(changedCompilation.Sources, SmileCompilationKind.Library);
        SmileLibraryPackage.Write(first, changedCompilation.Graph.Root, changedAnalysis);
        var changed = SmileLibraryPackage.Read(first, Path.Combine(directory, "obj"));
        Equal(false, loaded.PackageHash == changed.PackageHash);
        Equal(false, loaded.ExtractionDirectory == changed.ExtractionDirectory);
    }
    finally { Directory.Delete(directory, true); }
});
Run("Dependent packages load with an explicitly supplied base package", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileDependentPackageTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var baseDirectory = Path.Combine(directory, "Base");
        var dependentDirectory = Path.Combine(directory, "Dependent");
        var consumerDirectory = Path.Combine(directory, "Consumer");
        Directory.CreateDirectory(baseDirectory);
        Directory.CreateDirectory(dependentDirectory);
        Directory.CreateDirectory(consumerDirectory);

        var baseProjectPath = Path.Combine(baseDirectory, "Base.smilelibproj");
        File.WriteAllText(Path.Combine(baseDirectory, "Base.smile"),
            "MODULE Example.Base\nPUBLIC FUNCTION Double(Value)\nRETURN Value * 2\nEND FUNCTION\nEND MODULE\n");
        File.WriteAllText(baseProjectPath,
            "<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Example.Base</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include=\"Base.smile\" /></ItemGroup></SmileProject>");

        var dependentProjectPath = Path.Combine(dependentDirectory, "Dependent.smilelibproj");
        File.WriteAllText(Path.Combine(dependentDirectory, "Dependent.smile"),
            "MODULE Example.Dependent\nIMPORT Example.Base AS Base\nPUBLIC FUNCTION Quadruple(Value)\nRETURN Base.Double(Base.Double(Value))\nEND FUNCTION\nPRIVATE CONST Hidden = 9\nEND MODULE\n");
        File.WriteAllText(dependentProjectPath,
            "<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Example.Dependent</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include=\"Dependent.smile\" /><SmileProjectReference Include=\"..\\Base\\Base.smilelibproj\" /></ItemGroup></SmileProject>");

        var baseCompilation = SmileProjectCompilation.Load(baseProjectPath);
        var baseAnalysis = SmileLanguage.Analyze(baseCompilation.Sources, SmileCompilationKind.Library,
            baseCompilation.DependencyContext);
        var basePackagePath = Path.Combine(baseDirectory, "Base.smilelib");
        SmileLibraryPackage.Write(basePackagePath, baseCompilation.Graph.Root, baseAnalysis);
        var dependentCompilation = SmileProjectCompilation.Load(dependentProjectPath);
        var dependentAnalysis = SmileLanguage.Analyze(dependentCompilation.Sources, SmileCompilationKind.Library,
            dependentCompilation.DependencyContext);
        var dependentPackagePath = Path.Combine(dependentDirectory, "Dependent.smilelib");
        SmileLibraryPackage.Write(dependentPackagePath, dependentCompilation.Graph.Root, dependentAnalysis);

        var consumerProjectPath = Path.Combine(consumerDirectory, "Consumer.smileproj");
        File.WriteAllText(Path.Combine(consumerDirectory, "Program.smile"),
            "IMPORT Example.Dependent AS Dependent\nPRINT Dependent.Quadruple(3)\nEND PROGRAM\n");
        File.WriteAllText(consumerProjectPath,
            "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" /><SmileLibraryReference Include=\"..\\Base\\Base.smilelib\" /><SmileLibraryReference Include=\"..\\Dependent\\Dependent.smilelib\" /></ItemGroup></SmileProject>");

        var consumerCompilation = SmileProjectCompilation.Load(consumerProjectPath, Path.Combine(directory, "cache"));
        var consumerAnalysis = SmileLanguage.Analyze(consumerCompilation.Sources, SmileCompilationKind.Program,
            consumerCompilation.DependencyContext);
        Equal(false, consumerAnalysis.HasErrors);
        Equal(true, new WebEmitter(consumerAnalysis).Emit().Contains("async function r_", StringComparison.Ordinal));
        Equal(true, new MasmEmitter(consumerAnalysis, SmileGraphicsBackend.Auto, true, false).Emit()
            .Contains("call smile_", StringComparison.Ordinal));

        var completionSources = consumerCompilation.Sources.Select(source => source.IsStartup
            ? new SmileSourceDocument("IMPORT Example.Dependent AS Dependent\nPRINT Dependent.",
                source.FilePath, true, providerIdentity: source.ProviderIdentity)
            : source).ToArray();
        var completionAnalysis = SmileLanguage.Analyze(completionSources, SmileCompilationKind.Program,
            consumerCompilation.DependencyContext);
        var completions = SmileCompletionService.GetCompletions(completionAnalysis,
            completionSources.Single(source => source.IsStartup).Text.Length);
        Equal(true, completions.Any(item => item.DisplayText == "Quadruple"));
        Equal(false, completions.Any(item => item.DisplayText == "Hidden"));

        var undeclaredPackage = Path.Combine(dependentDirectory, "Undeclared.smilelib");
        File.Copy(dependentPackagePath, undeclaredPackage);
        RewriteManifest(undeclaredPackage, manifest => manifest.Replace(
            "    {\"name\": \"Example.Base\", \"version\": \"1.0.0\"}", string.Empty,
            StringComparison.Ordinal));
        var undeclared = ThrowsProjectDiagnostic(() => SmileLibraryProviderResolver.LoadPackages(
            new[] { basePackagePath, undeclaredPackage }, Path.Combine(directory, "undeclared-cache")), "SML3207");
        Equal(true, undeclared.Message.Contains("SML3208", StringComparison.Ordinal));
        Equal(true, undeclared.Message.Contains("Example.Base", StringComparison.Ordinal));

        var looseResolution = CompilerDriver.LoadLooseLibraryResolution(
            Path.Combine(consumerDirectory, "Program.smile"), new[] { dependentPackagePath, basePackagePath });
        var looseRoot = new SmileSourceDocument(
            "IMPORT Example.Base AS Base\nPRINT Base.Double(2)\nEND PROGRAM\n",
            Path.Combine(consumerDirectory, "Loose.smile"), true);
        Equal(false, SmileLanguage.Analyze(new[] { looseRoot }.Concat(looseResolution.Sources).ToArray(),
            SmileCompilationKind.Program, looseResolution.CreateLooseRootContext()).HasErrors);

        using (var archive = System.IO.Compression.ZipFile.OpenRead(dependentPackagePath))
        {
            using var reader = new StreamReader(archive.GetEntry("api/public-symbols.json")!.Open());
            var api = reader.ReadToEnd();
            Equal(true, api.Contains("Example.Dependent", StringComparison.Ordinal));
            Equal(false, api.Contains("Example.Base", StringComparison.Ordinal));
            Equal(false, api.Contains("Hidden", StringComparison.Ordinal));
        }

        var mixedProjectPath = Path.Combine(consumerDirectory, "ConsumerMixed.smileproj");
        File.WriteAllText(mixedProjectPath,
            "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" /><SmileProjectReference Include=\"..\\Base\\Base.smilelibproj\" /><SmileLibraryReference Include=\"..\\Dependent\\Dependent.smilelib\" /></ItemGroup></SmileProject>");
        var mixedCompilation = SmileProjectCompilation.Load(mixedProjectPath, Path.Combine(directory, "mixed-cache"));
        Equal(false, SmileLanguage.Analyze(mixedCompilation.Sources, SmileCompilationKind.Program).HasErrors);
        Equal(true, mixedCompilation.Graph.PhysicalCompilationSourcePaths.Contains(
            Path.Combine(baseDirectory, "Base.smile"), StringComparer.OrdinalIgnoreCase));
        Equal(false, mixedCompilation.Graph.PhysicalCompilationSourcePaths.Any(path =>
            path.Contains("mixed-cache", StringComparison.OrdinalIgnoreCase)));

        var projectChainPath = Path.Combine(consumerDirectory, "ConsumerProjects.smileproj");
        File.WriteAllText(projectChainPath,
            "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" /><SmileProjectReference Include=\"..\\Dependent\\Dependent.smilelibproj\" /></ItemGroup></SmileProject>");
        var projectChain = SmileProjectBuildGraph.Load(projectChainPath);
        Equal(true, projectChain.PhysicalCompilationSourcePaths.Contains(
            Path.Combine(baseDirectory, "Base.smile"), StringComparer.OrdinalIgnoreCase));
        Equal(true, projectChain.PhysicalCompilationSourcePaths.Contains(
            Path.Combine(dependentDirectory, "Dependent.smile"), StringComparer.OrdinalIgnoreCase));

        var looseSources = new[]
        {
            new SmileSourceDocument(File.ReadAllText(Path.Combine(consumerDirectory, "Program.smile")),
                Path.Combine(consumerDirectory, "Program.smile"), true)
        }.Concat(CompilerDriver.LoadLooseLibraries(Path.Combine(consumerDirectory, "Program.smile"),
            new[] { dependentPackagePath, basePackagePath })).ToArray();
        Equal(false, SmileLanguage.Analyze(looseSources, SmileCompilationKind.Program).HasErrors);
        ThrowsProjectDiagnostic(() => CompilerDriver.LoadLooseLibraries(
            Path.Combine(consumerDirectory, "Program.smile"), new[] { dependentPackagePath }), "SML3200");

        var missingProjectPath = Path.Combine(consumerDirectory, "ConsumerMissing.smileproj");
        File.WriteAllText(missingProjectPath,
            "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" /><SmileLibraryReference Include=\"..\\Dependent\\Dependent.smilelib\" /></ItemGroup></SmileProject>");
        var missing = SmileProjectCompilation.TryLoad(missingProjectPath, Path.Combine(directory, "missing-cache"));
        Equal(false, missing.Succeeded);
        Equal("SML3200", missing.Diagnostic!.Code);
        Equal(true, missing.Diagnostic.Message.Contains("Example.Dependent", StringComparison.Ordinal));
        Equal(true, missing.Diagnostic.Message.Contains("Example.Base", StringComparison.Ordinal));
        var safeAnalysis = SmileLanguage.AnalyzeWithProjectDiagnostic(new[]
        {
            new SmileSourceDocument(File.ReadAllText(Path.Combine(consumerDirectory, "Program.smile")),
                Path.Combine(consumerDirectory, "Program.smile"), true)
        }, SmileCompilationKind.Program, missing.Diagnostic);
        Equal(true, HasDiagnostic(safeAnalysis, "SML3200"));

        var malformedPackagePath = Path.Combine(dependentDirectory, "Malformed.smilelib");
        File.WriteAllText(malformedPackagePath, "not a package");
        var malformedProjectPath = Path.Combine(consumerDirectory, "ConsumerMalformed.smileproj");
        File.WriteAllText(malformedProjectPath,
            "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" /><SmileLibraryReference Include=\"..\\Dependent\\Malformed.smilelib\" /></ItemGroup></SmileProject>");
        var malformed = SmileProjectCompilation.TryLoad(malformedProjectPath,
            Path.Combine(directory, "malformed-cache"));
        Equal(false, malformed.Succeeded);
        Equal("SML3206", malformed.Diagnostic!.Code);
        Equal(malformedPackagePath, malformed.Diagnostic.FilePath);
        var malformedProjectFile = Path.Combine(consumerDirectory, "Malformed.smileproj");
        File.WriteAllText(malformedProjectFile, "<SmileProject>");
        var malformedProject = SmileProjectCompilation.TryLoad(malformedProjectFile);
        Equal(false, malformedProject.Succeeded);
        Equal("SML3206", malformedProject.Diagnostic!.Code);

        var versionTwoProject = File.ReadAllText(baseProjectPath).Replace("<Version>1.0.0</Version>",
            "<Version>2.0.0</Version>", StringComparison.Ordinal);
        File.WriteAllText(baseProjectPath, versionTwoProject);
        var versionTwoCompilation = SmileProjectCompilation.Load(baseProjectPath);
        var versionTwoPackage = Path.Combine(baseDirectory, "BaseV2.smilelib");
        SmileLibraryPackage.Write(versionTwoPackage, versionTwoCompilation.Graph.Root,
            SmileLanguage.Analyze(versionTwoCompilation.Sources, SmileCompilationKind.Library));
        File.WriteAllText(baseProjectPath, versionTwoProject.Replace("<Version>2.0.0</Version>",
            "<Version>1.0.0</Version>", StringComparison.Ordinal));
        var wrongVersionProjectPath = Path.Combine(consumerDirectory, "ConsumerWrongVersion.smileproj");
        File.WriteAllText(wrongVersionProjectPath,
            "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" /><SmileLibraryReference Include=\"..\\Base\\BaseV2.smilelib\" /><SmileLibraryReference Include=\"..\\Dependent\\Dependent.smilelib\" /></ItemGroup></SmileProject>");
        var wrongVersion = SmileProjectCompilation.TryLoad(wrongVersionProjectPath,
            Path.Combine(directory, "wrong-version-cache"));
        Equal("SML3202", wrongVersion.Diagnostic!.Code);
        Equal(true, wrongVersion.Diagnostic.Message.Contains("2.0.0", StringComparison.Ordinal));

        var duplicateProviderProjectPath = Path.Combine(consumerDirectory, "ConsumerDuplicate.smileproj");
        File.WriteAllText(duplicateProviderProjectPath,
            "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" /><SmileProjectReference Include=\"..\\Base\\Base.smilelibproj\" /><SmileLibraryReference Include=\"..\\Base\\Base.smilelib\" /><SmileLibraryReference Include=\"..\\Dependent\\Dependent.smilelib\" /></ItemGroup></SmileProject>");
        var duplicateProvider = SmileProjectCompilation.TryLoad(duplicateProviderProjectPath,
            Path.Combine(directory, "duplicate-cache"));
        Equal("SML3201", duplicateProvider.Diagnostic!.Code);
        Equal(true, duplicateProvider.Diagnostic.Message.Contains(baseProjectPath, StringComparison.OrdinalIgnoreCase));
        Equal(true, duplicateProvider.Diagnostic.Message.Contains(basePackagePath, StringComparison.OrdinalIgnoreCase));

        ThrowsProjectDiagnostic(() => SmileLibraryProviderResolver.LoadPackages(
            new[] { basePackagePath, basePackagePath }, Path.Combine(directory, "duplicate-path-cache")), "SML3201");

        var duplicateDependencyPackage = Path.Combine(dependentDirectory, "DuplicateDependency.smilelib");
        File.Copy(dependentPackagePath, duplicateDependencyPackage);
        RewriteManifest(duplicateDependencyPackage, manifest => manifest.Replace(
            "    {\"name\": \"Example.Base\", \"version\": \"1.0.0\"}",
            "    {\"name\": \"Example.Base\", \"version\": \"1.0.0\"},\n    {\"name\": \"Example.Base\", \"version\": \"1.0.0\"}",
            StringComparison.Ordinal));
        ThrowsProjectDiagnostic(() => SmileLibraryProviderResolver.LoadPackages(
            new[] { basePackagePath, duplicateDependencyPackage },
            Path.Combine(directory, "duplicate-dependency-cache")), "SML3203");

        var selfDependencyPackage = Path.Combine(baseDirectory, "SelfDependency.smilelib");
        File.Copy(basePackagePath, selfDependencyPackage);
        RewriteManifest(selfDependencyPackage, manifest => manifest.Replace("\"dependencies\": [\n\n  ]",
            "\"dependencies\": [\n    {\"name\": \"Example.Base\", \"version\": \"1.0.0\"}\n  ]",
            StringComparison.Ordinal));
        ThrowsProjectDiagnostic(() => SmileLibraryProviderResolver.LoadPackages(
            new[] { selfDependencyPackage }, Path.Combine(directory, "self-cache")), "SML3204");

        var cyclicBasePackage = Path.Combine(baseDirectory, "CyclicBase.smilelib");
        File.Copy(basePackagePath, cyclicBasePackage);
        RewriteManifest(cyclicBasePackage, manifest => manifest.Replace("\"dependencies\": [\n\n  ]",
            "\"dependencies\": [\n    {\"name\": \"Example.Dependent\", \"version\": \"1.0.0\"}\n  ]",
            StringComparison.Ordinal));
        var cycle = ThrowsProjectDiagnostic(() => SmileLibraryProviderResolver.LoadPackages(
            new[] { cyclicBasePackage, dependentPackagePath }, Path.Combine(directory, "cycle-cache")), "SML3205");
        Equal(true, cycle.Message.Contains("Example.Base", StringComparison.Ordinal));
        Equal(true, cycle.Message.Contains("Example.Dependent", StringComparison.Ordinal));
    }
    finally { Directory.Delete(directory, true); }
});
Run("Direct provider boundaries reject ambient and transitive project imports", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileBoundaryTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var baseDirectory = Path.Combine(directory, "Base");
        var dependentDirectory = Path.Combine(directory, "Dependent");
        var appDirectory = Path.Combine(directory, "App");
        Directory.CreateDirectory(baseDirectory);
        Directory.CreateDirectory(dependentDirectory);
        Directory.CreateDirectory(appDirectory);
        var baseProject = Path.Combine(baseDirectory, "Base.smilelibproj");
        var dependentProject = Path.Combine(dependentDirectory, "Dependent.smilelibproj");
        var dependentSource = Path.Combine(dependentDirectory, "Dependent.smile");
        var appProject = Path.Combine(appDirectory, "App.smileproj");
        var programSource = Path.Combine(appDirectory, "Program.smile");
        File.WriteAllText(Path.Combine(baseDirectory, "Base.smile"),
            "MODULE Example.Base\nPUBLIC FUNCTION Double(Value)\nRETURN Value * 2\nEND FUNCTION\nEND MODULE\n");
        File.WriteAllText(baseProject,
            "<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Example.Base</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include=\"Base.smile\" /></ItemGroup></SmileProject>");
        File.WriteAllText(dependentSource,
            "MODULE Example.Dependent\nIMPORT Example.Base AS Base\nPUBLIC FUNCTION Quadruple(Value)\nRETURN Base.Double(Base.Double(Value))\nEND FUNCTION\nEND MODULE\n");
        var dependentWithoutReference =
            "<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Example.Dependent</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include=\"Dependent.smile\" /></ItemGroup></SmileProject>";
        var dependentWithReference = dependentWithoutReference.Replace("</ItemGroup>",
            "<SmileProjectReference Include=\"..\\Base\\Base.smilelibproj\" /></ItemGroup>", StringComparison.Ordinal);
        File.WriteAllText(dependentProject, dependentWithoutReference);
        File.WriteAllText(programSource,
            "IMPORT Example.Dependent AS Dependent\nPRINT Dependent.Quadruple(3)\nEND PROGRAM\n");
        File.WriteAllText(appProject,
            "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" /><SmileProjectReference Include=\"..\\Base\\Base.smilelibproj\" /><SmileProjectReference Include=\"..\\Dependent\\Dependent.smilelibproj\" /></ItemGroup></SmileProject>");

        var ambientCompilation = SmileProjectCompilation.Load(appProject);
        var ambientAnalysis = SmileLanguage.Analyze(ambientCompilation.Sources, SmileCompilationKind.Program,
            ambientCompilation.DependencyContext);
        var ambientDiagnostic = ambientAnalysis.Diagnostics.Single(diagnostic => diagnostic.Code == "SML3208");
        Equal(dependentSource, ambientDiagnostic.FilePath);
        Equal(2, ambientDiagnostic.Line);
        Equal(8, ambientDiagnostic.Column);
        Equal(true, ambientDiagnostic.Message.Contains(dependentProject, StringComparison.OrdinalIgnoreCase));
        Equal(true, ambientDiagnostic.Message.Contains(baseProject, StringComparison.OrdinalIgnoreCase));
        var previousError = Console.Error;
        var boundaryOutput = new StringWriter();
        try
        {
            Console.SetError(boundaryOutput);
            Equal(1, new CompilerDriver().Run(new[] { "--project", appProject, "--target", "web",
                "--output-dir", Path.Combine(directory, "invalid-web") }));
        }
        finally
        {
            Console.SetError(previousError);
        }
        Equal(true, boundaryOutput.ToString().Contains(
            $"{dependentSource}(2,8): error SML3208: {ambientDiagnostic.Message}", StringComparison.Ordinal));

        File.WriteAllText(dependentProject, dependentWithReference);
        File.WriteAllText(appProject,
            "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" /><SmileProjectReference Include=\"..\\Dependent\\Dependent.smilelibproj\" /></ItemGroup></SmileProject>");
        var validCompilation = SmileProjectCompilation.Load(appProject);
        var validAnalysis = SmileLanguage.Analyze(validCompilation.Sources, SmileCompilationKind.Program,
            validCompilation.DependencyContext);
        Equal(false, validAnalysis.HasErrors);
        Equal(true, new WebEmitter(validAnalysis).Emit().Contains("async function r_", StringComparison.Ordinal));
        Equal(true, new MasmEmitter(validAnalysis, SmileGraphicsBackend.Auto, true, false).Emit()
            .Contains("call smile_", StringComparison.Ordinal));

        File.WriteAllText(programSource, "IMPORT Example.Base AS Base\nEND PROGRAM\n");
        var transitiveCompilation = SmileProjectCompilation.Load(appProject);
        var transitiveAnalysis = SmileLanguage.Analyze(transitiveCompilation.Sources, SmileCompilationKind.Program,
            transitiveCompilation.DependencyContext);
        var transitiveDiagnostic = transitiveAnalysis.Diagnostics.Single(diagnostic => diagnostic.Code == "SML3208");
        Equal(programSource, transitiveDiagnostic.FilePath);
        Equal(1, transitiveDiagnostic.Line);
        Equal(8, transitiveDiagnostic.Column);

        File.WriteAllText(programSource, "IMPORT ");
        var completionCompilation = SmileProjectCompilation.Load(appProject);
        var completionAnalysis = SmileLanguage.Analyze(completionCompilation.Sources, SmileCompilationKind.Program,
            completionCompilation.DependencyContext);
        var completions = SmileCompletionService.GetCompletions(completionAnalysis, programSource, 7);
        Equal(true, completions.Any(item => item.DisplayText == "Example.Dependent"));
        Equal(false, completions.Any(item => item.DisplayText == "Example.Base"));

        File.WriteAllText(appProject,
            "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" /><SmileProjectReference Include=\"..\\Dependent\\Dependent.smilelibproj\" /><SmileProjectReference Include=\"..\\Base\\Base.smilelibproj\" /></ItemGroup></SmileProject>");
        var directCompletion = SmileProjectCompilation.Load(appProject);
        var directCompletionAnalysis = SmileLanguage.Analyze(directCompletion.Sources, SmileCompilationKind.Program,
            directCompletion.DependencyContext);
        Equal(true, SmileCompletionService.GetCompletions(directCompletionAnalysis, programSource, 7)
            .Any(item => item.DisplayText == "Example.Base"));
        File.WriteAllText(appProject,
            "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" /><SmileProjectReference Include=\"..\\Dependent\\Dependent.smilelibproj\" /></ItemGroup></SmileProject>");
        var removedCompletion = SmileProjectCompilation.Load(appProject);
        var removedCompletionAnalysis = SmileLanguage.Analyze(removedCompletion.Sources,
            SmileCompilationKind.Program, removedCompletion.DependencyContext);
        Equal(false, SmileCompletionService.GetCompletions(removedCompletionAnalysis, programSource, 7)
            .Any(item => item.DisplayText == "Example.Base"));
    }
    finally { Directory.Delete(directory, true); }
});
Run("Library output fingerprints reject stale and foreign packages without timestamps", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileFingerprintTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var projectPath = Path.Combine(directory, "Tools.smilelibproj");
        var sourcePath = Path.Combine(directory, "Tools.smile");
        var outputPath = Path.Combine(directory, "Tools.smilelib");
        var projectXml = "<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Example.Tools</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include=\"Tools.smile\" /></ItemGroup></SmileProject>";
        File.WriteAllText(sourcePath, "MODULE Example.Tools\nPUBLIC CONST Value = 1\nEND MODULE\n");
        File.WriteAllText(projectPath, projectXml);
        var compilation = SmileProjectCompilation.Load(projectPath);
        var analysis = SmileLanguage.Analyze(compilation.Sources, SmileCompilationKind.Library,
            compilation.DependencyContext);
        SmileLibraryPackage.Write(outputPath, compilation.Graph.Root, analysis);
        Equal(false, CompilerDriver.NeedsLibraryBuild(compilation.Graph.Root, outputPath, analysis));

        File.WriteAllText(sourcePath, "MODULE Example.Tools\nPUBLIC CONST Value = 2\nEND MODULE\n");
        File.SetLastWriteTimeUtc(sourcePath, new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var changedCompilation = SmileProjectCompilation.Load(projectPath);
        var changedAnalysis = SmileLanguage.Analyze(changedCompilation.Sources, SmileCompilationKind.Library,
            changedCompilation.DependencyContext);
        Equal(true, CompilerDriver.NeedsLibraryBuild(changedCompilation.Graph.Root, outputPath, changedAnalysis));
        SmileLibraryPackage.Write(outputPath, changedCompilation.Graph.Root, changedAnalysis);
        var deterministicCopy = Path.Combine(directory, "Tools-copy.smilelib");
        SmileLibraryPackage.Write(deterministicCopy, changedCompilation.Graph.Root, changedAnalysis);
        Equal(true, File.ReadAllBytes(outputPath).SequenceEqual(File.ReadAllBytes(deterministicCopy)));
        var tamperedApi = Path.Combine(directory, "Tools-tampered-api.smilelib");
        File.Copy(outputPath, tamperedApi);
        RewritePackageTextEntry(tamperedApi, "api/public-symbols.json", api =>
            api.Replace("Value", "Changed", StringComparison.Ordinal));
        Equal(true, CompilerDriver.NeedsLibraryBuild(changedCompilation.Graph.Root, tamperedApi,
            changedAnalysis));

        var foreignProjectPath = Path.Combine(directory, "Foreign.smilelibproj");
        var foreignSourcePath = Path.Combine(directory, "Foreign.smile");
        var foreignPackage = Path.Combine(directory, "Foreign.smilelib");
        File.WriteAllText(foreignSourcePath, "MODULE Example.Foreign\nPUBLIC CONST Value = 9\nEND MODULE\n");
        File.WriteAllText(foreignProjectPath,
            "<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Example.Foreign</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include=\"Foreign.smile\" /></ItemGroup></SmileProject>");
        var foreignCompilation = SmileProjectCompilation.Load(foreignProjectPath);
        var foreignAnalysis = SmileLanguage.Analyze(foreignCompilation.Sources, SmileCompilationKind.Library,
            foreignCompilation.DependencyContext);
        SmileLibraryPackage.Write(foreignPackage, foreignCompilation.Graph.Root, foreignAnalysis);
        File.Copy(foreignPackage, outputPath, true);
        Equal(true, CompilerDriver.NeedsLibraryBuild(changedCompilation.Graph.Root, outputPath, changedAnalysis));

        File.WriteAllText(projectPath, projectXml.Replace("<Version>1.0.0</Version>",
            "<Version>2.0.0</Version>", StringComparison.Ordinal));
        var versionCompilation = SmileProjectCompilation.Load(projectPath);
        var versionAnalysis = SmileLanguage.Analyze(versionCompilation.Sources, SmileCompilationKind.Library,
            versionCompilation.DependencyContext);
        Equal(true, CompilerDriver.NeedsLibraryBuild(versionCompilation.Graph.Root, deterministicCopy,
            versionAnalysis));

        File.WriteAllText(projectPath, projectXml.Replace("</ItemGroup>",
            "<SmileProjectReference Include=\"Foreign.smilelibproj\" /></ItemGroup>", StringComparison.Ordinal));
        var referenceCompilation = SmileProjectCompilation.Load(projectPath);
        var referenceAnalysis = SmileLanguage.Analyze(referenceCompilation.Sources, SmileCompilationKind.Library,
            referenceCompilation.DependencyContext);
        SmileLibraryPackage.Write(outputPath, referenceCompilation.Graph.Root, referenceAnalysis);
        Equal(false, CompilerDriver.NeedsLibraryBuild(referenceCompilation.Graph.Root, outputPath,
            referenceAnalysis));
        File.WriteAllText(foreignProjectPath, File.ReadAllText(foreignProjectPath).Replace(
            "<Version>1.0.0</Version>", "<Version>2.0.0</Version>", StringComparison.Ordinal));
        var dependencyVersionCompilation = SmileProjectCompilation.Load(projectPath);
        var dependencyVersionAnalysis = SmileLanguage.Analyze(dependencyVersionCompilation.Sources,
            SmileCompilationKind.Library, dependencyVersionCompilation.DependencyContext);
        Equal(true, CompilerDriver.NeedsLibraryBuild(dependencyVersionCompilation.Graph.Root, outputPath,
            dependencyVersionAnalysis));
    }
    finally { Directory.Delete(directory, true); }
});
Run("Tolerant participation discovery retains missing transitive reference paths", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileRecoveryTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var rootProject = Path.Combine(directory, "App.smileproj");
        var middleProject = Path.Combine(directory, "Middle.smilelibproj");
        var leafProject = Path.Combine(directory, "Leaf.smilelibproj");
        var packagePath = Path.Combine(directory, "Restored.smilelib");
        File.WriteAllText(Path.Combine(directory, "Program.smile"), "END PROGRAM\n");
        File.WriteAllText(Path.Combine(directory, "Middle.smile"), "MODULE Middle\nEND MODULE\n");
        File.WriteAllText(rootProject,
            "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" /><SmileProjectReference Include=\"Middle.smilelibproj\" /></ItemGroup></SmileProject>");
        File.WriteAllText(middleProject,
            "<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Middle</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include=\"Middle.smile\" /><SmileProjectReference Include=\"Leaf.smilelibproj\" /></ItemGroup></SmileProject>");
        var missingLeaf = SmileProjectParticipationDiscovery.Discover(rootProject);
        Equal("SML3200", missingLeaf.Diagnostic!.Code);
        Equal(true, missingLeaf.Paths.Contains(leafProject, StringComparer.OrdinalIgnoreCase));

        File.WriteAllText(Path.Combine(directory, "Leaf.smile"), "MODULE Leaf\nEND MODULE\n");
        File.WriteAllText(leafProject,
            "<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Leaf</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include=\"Leaf.smile\" /><SmileLibraryReference Include=\"Restored.smilelib\" /></ItemGroup></SmileProject>");
        var missingPackage = SmileProjectParticipationDiscovery.Discover(rootProject);
        Equal("SML3200", missingPackage.Diagnostic!.Code);
        Equal(true, missingPackage.Paths.Contains(packagePath, StringComparer.OrdinalIgnoreCase));
        File.WriteAllText(packagePath, "restored watcher fixture");
        Equal(true, SmileProjectParticipationDiscovery.Discover(rootProject).Diagnostic == null);
    }
    finally { Directory.Delete(directory, true); }
});
Run("Project diagnostics retain shared path formatting and compiler exit code one", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileDiagnosticTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var projectPath = Path.Combine(directory, "App.smileproj");
        var sourcePath = Path.Combine(directory, "Program.smile");
        var missingPath = Path.Combine(directory, "Missing.smilelibproj");
        File.WriteAllText(sourcePath, "END PROGRAM\n");
        File.WriteAllText(projectPath,
            "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" /><SmileProjectReference Include=\"Missing.smilelibproj\" /></ItemGroup></SmileProject>");
        var diagnostic = SmileProjectCompilation.TryLoad(projectPath).Diagnostic!;
        Equal($"{missingPath}(1,1): error SML3200: {diagnostic.Message}", diagnostic.FormatCompiler());
        var safe = SmileLanguage.AnalyzeWithProjectDiagnostic(new[]
        {
            new SmileSourceDocument("END PROGRAM\n", sourcePath, true)
        }, SmileCompilationKind.Program, diagnostic);
        var editorDiagnostic = safe.Diagnostics.Single(item => item.Code == "SML3200");
        Equal(missingPath, editorDiagnostic.FilePath);
        Equal(1, editorDiagnostic.Line);
        Equal(1, editorDiagnostic.Column);

        var previousError = Console.Error;
        var captured = new StringWriter();
        try
        {
            Console.SetError(captured);
            Equal(1, new CompilerDriver().Run(new[] { "--project", projectPath, "--target", "web",
                "--output-dir", Path.Combine(directory, "web") }));
        }
        finally
        {
            Console.SetError(previousError);
        }
        Equal(true, captured.ToString().Contains(diagnostic.FormatCompiler(), StringComparison.Ordinal));
        var usageOutput = new StringWriter();
        try
        {
            Console.SetError(usageOutput);
            Equal(2, new CompilerDriver().Run(Array.Empty<string>()));
        }
        finally
        {
            Console.SetError(previousError);
        }
        Equal(true, usageOutput.ToString().Contains("Usage: smilec", StringComparison.Ordinal));
    }
    finally { Directory.Delete(directory, true); }
});
Run("Project reference cycles are diagnosed with the dependency path", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileGraphTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        File.WriteAllText(Path.Combine(directory, "A.smile"), "MODULE A\nEND MODULE\n");
        File.WriteAllText(Path.Combine(directory, "B.smile"), "MODULE B\nEND MODULE\n");
        File.WriteAllText(Path.Combine(directory, "A.smilelibproj"), "<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>A</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include=\"A.smile\" /><SmileProjectReference Include=\"B.smilelibproj\" /></ItemGroup></SmileProject>");
        File.WriteAllText(Path.Combine(directory, "B.smilelibproj"), "<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>B</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include=\"B.smile\" /><SmileProjectReference Include=\"A.smilelibproj\" /></ItemGroup></SmileProject>");
        var cycle = ThrowsProjectDiagnostic(
            () => SmileProjectBuildGraph.Load(Path.Combine(directory, "A.smilelibproj")), "SML3205");
        Equal(true, cycle.Message.Contains(Path.Combine(directory, "A.smilelibproj"),
            StringComparison.OrdinalIgnoreCase));
        Equal(true, cycle.Message.Contains(Path.Combine(directory, "B.smilelibproj"),
            StringComparison.OrdinalIgnoreCase));
    }
    finally { Directory.Delete(directory, true); }
});
Run("Private is the module default and modules cannot capture consumer globals", () =>
{
    var privateAnalysis = Multi(
        ("Program.smile", true, "IMPORT Example.Values AS Values\nPRINT Values.Hidden\n"),
        ("Values.smile", false, "MODULE Example.Values\nCONST Hidden = 1\nEND MODULE\n"));
    Equal(true, HasDiagnostic(privateAnalysis, "SML3105"));
    var captureAnalysis = Multi(
        ("Program.smile", true, "IMPORT Example.Values AS Values\nScore = 10\nPRINT Values.ReadScore()\n"),
        ("Values.smile", false, "MODULE Example.Values\nPUBLIC FUNCTION ReadScore()\nRETURN Score\nEND FUNCTION\nEND MODULE\n"));
    Equal(true, HasDiagnostic(captureAnalysis, "SML3110"));
});
Run("Duplicate module providers are rejected independently of source names", () =>
{
    var analysis = SmileLanguage.Analyze(new[]
    {
        new SmileSourceDocument("IMPORT Shared.Tools AS Tools\n", "Program.smile", true),
        new SmileSourceDocument("MODULE Shared.Tools\nPUBLIC CONST First = 1\nEND MODULE\n", "First.smile", providerIdentity: "First.smilelib"),
        new SmileSourceDocument("MODULE Shared.Tools\nPUBLIC CONST Second = 2\nEND MODULE\n", "Second.smile", providerIdentity: "Second.smilelib")
    });
    Equal(true, HasDiagnostic(analysis, "SML3107"));
});
Run("Malformed and unsafe packages are rejected before extraction", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileUnsafePackageTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var malformed = Path.Combine(directory, "Malformed.smilelib");
        File.WriteAllText(malformed, "not a zip");
        ThrowsContains(() => SmileLibraryPackage.ReadIdentity(malformed), "Central Directory");
        var unsafePackage = Path.Combine(directory, "Unsafe.smilelib");
        using (var archive = System.IO.Compression.ZipFile.Open(unsafePackage, System.IO.Compression.ZipArchiveMode.Create))
        {
            using var writer = new StreamWriter(archive.CreateEntry("src/../escape.smile").Open());
            writer.Write("MODULE Escape\nEND MODULE\n");
        }
        ThrowsContains(() => SmileLibraryPackage.Read(unsafePackage, Path.Combine(directory, "cache")), "Unsafe SMILE library archive path");
    }
    finally { Directory.Delete(directory, true); }
});
Run("Project-reference debug sites retain the real library source path", () =>
{
    var projectPath = Path.GetFullPath("examples/LibraryConsumer/LibraryConsumer.smileproj");
    var compilation = SmileProjectCompilation.Load(projectPath);
    var analysis = SmileLanguage.Analyze(compilation.Sources, compilation.CompilationKind);
    Equal(false, analysis.HasErrors);
    var emitter = new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, true);
    _ = emitter.Emit();
    Equal(true, emitter.DebugSites.Any(site => Path.GetFileName(site.Source.FilePath) == "Clamp.smile"));
});
Run("Identical member names in different modules receive distinct emitter identities", () =>
{
    var analysis = Multi(
        ("Program.smile", true, "IMPORT Example.Alpha AS Alpha\nIMPORT Example.Beta AS Beta\nPRINT Alpha.Value()\nPRINT Beta.Value()\n"),
        ("Alpha.smile", false, "MODULE Example.Alpha\nPUBLIC FUNCTION Value()\nRETURN 1\nEND FUNCTION\nEND MODULE\n"),
        ("Beta.smile", false, "MODULE Example.Beta\nPUBLIC FUNCTION Value()\nRETURN 2\nEND FUNCTION\nEND MODULE\n"));
    Equal(false, analysis.HasErrors);
    var assembly = new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, false).Emit();
    Equal(2, analysis.SemanticModel.Routines.Values.Count(routine => routine.Name == "Value"));
    Equal(true, assembly.Contains("call smile_", StringComparison.Ordinal));
    Equal(true, new WebEmitter(analysis).Emit().Split(new[] { "async function r_" }, StringSplitOptions.None).Length >= 3);
});

if (failures.Count != 0)
{
    Console.Error.WriteLine($"{failures.Count} SMILE project-option test(s) failed:");
    foreach (var failure in failures)
        Console.Error.WriteLine("- " + failure);
    return 1;
}

Console.WriteLine($"{passed} SMILE language, compiler, project, completion, and timing tests passed.");
return 0;

SmileProjectGraphicsOptions Parse(string xml) =>
    SmileProjectGraphicsOptions.Parse(XElement.Parse(xml));

SmileAnalysisResult Analyze(string source) => SmileLanguage.Analyze(source);

SmileAnalysisResult Multi(params (string Path, bool Startup, string Text)[] sources) =>
    SmileLanguage.Analyze(sources.Select(source =>
        new SmileSourceDocument(source.Text, source.Path, source.Startup)).ToArray());

SmileProjectSourceSet ProjectSources(string xml) =>
    SmileProjectSourceSet.Parse(Path.GetFullPath("Test.smileproj"), xml);

MusicStatementSyntax Music(SmileAnalysisResult analysis) =>
    analysis.SyntaxTree.Root.Statements.OfType<MusicStatementSyntax>().Single();

bool HasDiagnostic(SmileAnalysisResult analysis, string code) =>
    analysis.Diagnostics.Any(diagnostic => diagnostic.Code == code);

void Run(string name, Action test)
{
    try
    {
        test();
        passed++;
    }
    catch (Exception exception)
    {
        failures.Add($"{name}: {exception.Message}");
    }
}

void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Expected {expected}, found {actual}.");
}

void Throws(Action action, string expectedMessage)
{
    try
    {
        action();
    }
    catch (Exception exception)
    {
        if (exception.Message == expectedMessage)
            return;
        throw new InvalidOperationException(
            $"Expected diagnostic '{expectedMessage}', found '{exception.Message}'.");
    }
    throw new InvalidOperationException($"Expected diagnostic '{expectedMessage}', but no exception was thrown.");
}

void ThrowsContains(Action action, string expectedText)
{
    try
    {
        action();
    }
    catch (Exception exception)
    {
        if (exception.Message.Contains(expectedText, StringComparison.Ordinal))
            return;
        throw new InvalidOperationException(
            $"Expected diagnostic containing '{expectedText}', found '{exception.Message}'.");
    }
    throw new InvalidOperationException($"Expected diagnostic containing '{expectedText}', but no exception was thrown.");
}

SmileProjectDiagnosticException ThrowsProjectDiagnostic(Action action, string expectedCode)
{
    try
    {
        action();
    }
    catch (SmileProjectDiagnosticException exception)
    {
        Equal(expectedCode, exception.Code);
        return exception;
    }
    throw new InvalidOperationException($"Expected project diagnostic '{expectedCode}', but no exception was thrown.");
}

void RewriteManifest(string packagePath, Func<string, string> rewrite)
    => RewritePackageTextEntry(packagePath, "manifest.json", rewrite);

void RewritePackageTextEntry(string packagePath, string entryName, Func<string, string> rewrite)
{
    using var archive = System.IO.Compression.ZipFile.Open(packagePath,
        System.IO.Compression.ZipArchiveMode.Update);
    var entry = archive.GetEntry(entryName)!;
    string text;
    using (var reader = new StreamReader(entry.Open()))
        text = reader.ReadToEnd();
    entry.Delete();
    using var writer = new StreamWriter(archive.CreateEntry(entryName).Open());
    writer.Write(rewrite(text));
}

long SimulateFixedPoint(IEnumerable<int> frameTimes, int velocitySubpixelsPerSecond)
{
    const int fixedStep = 8;
    const int maximumElapsed = 50;
    const int maximumCatchUpSteps = 6;
    long position = 0;
    var accumulator = 0;
    foreach (var frameTime in frameTimes)
    {
        var elapsed = Math.Max(0, Math.Min(maximumElapsed, frameTime));
        accumulator = Math.Min(fixedStep * maximumCatchUpSteps, accumulator + elapsed);
        var steps = 0;
        while (accumulator >= fixedStep && steps < maximumCatchUpSteps)
        {
            position += (long)velocitySubpixelsPerSecond * fixedStep / 1000;
            accumulator -= fixedStep;
            steps++;
        }
    }
    return position;
}
