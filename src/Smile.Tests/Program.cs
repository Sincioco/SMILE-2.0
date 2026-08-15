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
    Analyze("Game Window \"Quad\"\nFill Quadrilateral 0, 0, 20, 0, 20, 20, 0, 20, WHITE\n").HasErrors));
Run("Outlined quadrilateral analyzes without errors", () => Equal(false,
    Analyze("Game Window \"Quad\"\nDraw Quadrilateral 0, 0, 20, 0, 20, 20, 0, 20, WHITE\n").HasErrors));
Run("Filled quadrilateral records its shared syntax operation", () => Equal(GraphicsOperation.FillQuadrilateral,
    Analyze("Game Window \"Quad\"\nFill Quadrilateral 0, 0, 20, 0, 20, 20, 0, 20, WHITE\n")
        .SyntaxTree.Root.Statements.OfType<GraphicsStatementSyntax>().Single().Operation));
Run("Outlined quadrilateral records its shared syntax operation", () => Equal(GraphicsOperation.DrawQuadrilateral,
    Analyze("Game Window \"Quad\"\nDraw Quadrilateral 0, 0, 20, 0, 20, 20, 0, 20, WHITE\n")
        .SyntaxTree.Root.Statements.OfType<GraphicsStatementSyntax>().Single().Operation));
Run("Too few quadrilateral arguments report a parser error", () => Equal(true,
    HasDiagnostic(Analyze("Game Window \"Quad\"\nFill Quadrilateral 0, 0, 20\n"), "SML2001")));
Run("Too many quadrilateral arguments report a parser error", () => Equal(true,
    HasDiagnostic(Analyze("Game Window \"Quad\"\nDraw Quadrilateral 0, 0, 20, 0, 20, 20, 0, 20, WHITE, 99\n"), "SML2001")));
Run("Quadrilateral arguments must be numbers", () => Equal(true,
    HasDiagnostic(Analyze("Game Window \"Quad\"\nFill Quadrilateral True, 0, 20, 0, 20, 20, 0, 20, WHITE\n"), "SML3023")));
Run("Arc is a shared case-insensitive keyword", () => Equal(SyntaxKind.ArcKeyword,
    SyntaxFacts.GetKeywordKind("arc")));
Run("Draw Arc analyzes without errors", () => Equal(false,
    Analyze("Game Window \"Arc\"\nDraw Arc 200, 200, 50, 0, 90, BLUE\n").HasErrors));
Run("Draw Arc records its shared syntax operation", () => Equal(GraphicsOperation.DrawArc,
    Analyze("Game Window \"Arc\"\nDraw Arc 200, 200, 50, 0, 90, BLUE\n")
        .SyntaxTree.Root.Statements.OfType<GraphicsStatementSyntax>().Single().Operation));
Run("Draw Arc records exactly six arguments", () => Equal(6,
    Analyze("Game Window \"Arc\"\nDraw Arc 200, 200, 50, 0, 90, BLUE\n")
        .SyntaxTree.Root.Statements.OfType<GraphicsStatementSyntax>().Single().Arguments.Count));
Run("Too few arc arguments report a parser error", () => Equal(true,
    HasDiagnostic(Analyze("Game Window \"Arc\"\nDraw Arc 200, 200, 50\n"), "SML2001")));
Run("Too many arc arguments report a parser error", () => Equal(true,
    HasDiagnostic(Analyze("Game Window \"Arc\"\nDraw Arc 200, 200, 50, 0, 90, BLUE, 99\n"), "SML2001")));
Run("Draw Arc arguments must be numbers", () => Equal(true,
    HasDiagnostic(Analyze("Game Window \"Arc\"\nDraw Arc True, 200, 50, 0, 90, BLUE\n"), "SML3023")));
Run("Fill Arc is rejected", () => Equal(true,
    HasDiagnostic(Analyze("Game Window \"Arc\"\nFill Arc 200, 200, 50, 0, 90, BLUE\n"), "SML2001")));
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
    Equal(20L, SyntaxFacts.GetBuiltInConstantValue(SyntaxKind.Key3Keyword));
});
Run("Existing graphics statements remain valid", () => Equal(false,
    Analyze("Game Window \"Existing\"\nFill Rectangle 1, 2, 3, 4, RED\nDraw Circle 10, 10, 4, WHITE\nDraw Line 0, 0, 20, 20, BLUE\n").HasErrors));
Run("Music keywords are shared and case-insensitive", () =>
{
    Equal(SyntaxKind.MusicKeyword, SyntaxFacts.GetKeywordKind("music"));
    Equal(SyntaxKind.PauseKeyword, SyntaxFacts.GetKeywordKind("PaUsE"));
    Equal(SyntaxKind.ResumeKeyword, SyntaxFacts.GetKeywordKind("resume"));
    Equal(SyntaxKind.VolumeKeyword, SyntaxFacts.GetKeywordKind("Volume"));
    Equal(true, SyntaxFacts.IsKeyword(SyntaxKind.MusicKeyword));
});
Run("Play Music analyzes as non-looping playback", () =>
{
    var music = Music(Analyze("Game Window \"Music\"\nPlay Music \"Assets\\Background.mp3\"\n"));
    Equal(MusicOperation.Play, music.Operation);
    Equal(false, music.Loop);
});
Run("Play Music Loop records looping playback", () => Equal(true,
    Music(Analyze("Game Window \"Music\"\nPlay Music \"Assets\\Background.mp3\" Loop\n")).Loop));
Run("Pause Music records the shared operation", () => Equal(MusicOperation.Pause,
    Music(Analyze("Game Window \"Music\"\nPause Music\n")).Operation));
Run("Resume Music records the shared operation", () => Equal(MusicOperation.Resume,
    Music(Analyze("Game Window \"Music\"\nResume Music\n")).Operation));
Run("Stop Music records the shared operation", () => Equal(MusicOperation.Stop,
    Music(Analyze("Game Window \"Music\"\nStop Music\n")).Operation));
Run("Music Volume accepts numeric expressions", () => Equal(false,
    Analyze("Game Window \"Music\"\nMusic Volume 25 + 25\n").HasErrors));
Run("Existing Play Sound and Stop Sound remain shared sound syntax", () =>
{
    var analysis = Analyze("Game Window \"Sound\"\nPlay Sound \"Assets\\Effect.wav\"\nStop Sound\n");
    Equal(false, analysis.HasErrors);
    Equal(2, analysis.SyntaxTree.Root.Statements.OfType<SoundStatementSyntax>().Count());
});
Run("Phase 4 media keywords and constants are shared", () =>
{
    Equal(SyntaxKind.ImageKeyword, SyntaxFacts.GetKeywordKind("image"));
    Equal(SyntaxKind.ClipKeyword, SyntaxFacts.GetKeywordKind("ClIp"));
    Equal(SyntaxKind.ChannelKeyword, SyntaxFacts.GetKeywordKind("channel"));
    Equal(16L, SyntaxFacts.GetBuiltInConstantValue(SyntaxKind.SoundChannelCountKeyword));
    Equal(1048576L, SyntaxFacts.GetBuiltInConstantValue(SyntaxKind.DataBlockMaxBytesKeyword));
    Equal(true, SyntaxFacts.IsKeyword(SyntaxKind.PixelKeyword));
});
Run("Image works in variables arrays records parameters ByRef and returns", () =>
{
    const string source = "Option Explicit\nType Art\nPicture As Image\nEnd Type\nDim SourceImage As Image\nDim Copies[2] As Image\nDim Card As Art\nLoad Image SourceImage From \"Assets\\A.png\"\nCopies[0] = SourceImage\nCard.Picture = Copies[0]\nCall Keep(Card.Picture)\nSourceImage = CopyImage(Card.Picture)\nUnload Image Copies[0]\nSub Keep(ByRef Value As Image)\nValue = Value\nEnd Sub\nFunction CopyImage(Value As Image) As Image\nReturn Value\nEnd Function\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    Equal(SmileType.Image, analysis.SemanticModel.Symbols["SourceImage"].Type);
    Equal(true, analysis.SemanticModel.Types["Art"].ContainsOwnedImage);
});
Run("Draw Image supports full and explicit rectangles with all Phase 4 modifiers", () =>
{
    const string source = "Game Window \"Images\" Size 960 By 540\nDim Art As Image\nDraw Image Art At 0, 0\nDraw Image Art From 10, 20 Size 300 By 200 At 480, 270 Size 600 By 400 Opacity 65 Anchor 300, 400 Filter Pixel Flip Both\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var draws = analysis.SyntaxTree.Root.Statements.OfType<DrawImageStatementSyntax>().ToArray();
    Equal(2, draws.Length);
    Equal(ImageFilter.Smooth, draws[0].Filter);
    Equal(ImageFilter.Pixel, draws[1].Filter);
    Equal(ImageFlip.Horizontal | ImageFlip.Vertical, draws[1].Flip);
});
Run("Clip Rectangle nests and includes structured statements", () =>
{
    var analysis = Analyze("Game Window \"Clip\"\nClip Rectangle 0, 0, 100, 100\nClip Rectangle 10, 10, 40, 40\nFill Rectangle 0, 0, 100, 100, WHITE\nEnd Clip\nEnd Clip\n");
    Equal(false, analysis.HasErrors);
    var outer = analysis.SyntaxTree.Root.Statements.OfType<ClipRectangleStatementSyntax>().Single();
    Equal(1, outer.Statements.OfType<ClipRectangleStatementSyntax>().Count());
});
Run("Image measurement and Text measurement built-ins type check", () => Equal(false,
    Analyze("Game Window \"Measure\"\nDim Art As Image\nDim Caption As Text\nPrint Image_Width(Art)\nPrint Image_Height(Art)\nPrint Image_Loaded(Art)\nPrint Text_Width(Caption, 28)\nPrint Text_Height(Caption, 28)\n").HasErrors));
Run("Persistent Data statements accept byte arrays and writable count targets", () => Equal(false,
    Analyze("Option Explicit\nDim Bytes[8]\nDim ByteCount As Number\nSave Data Bytes Count 8 To \"slot\"\nLoad Data \"slot\" Into Bytes Count ByteCount\n").HasErrors));
Run("Explicit WAV channels support play per-channel stop and global stop", () =>
{
    var analysis = Analyze("Game Window \"Audio\"\nPlay Sound \"Assets\\One.wav\" On Channel 1\nPlay Sound \"Assets\\Two.wav\" On Channel 2\nStop Sound On Channel 1\nStop Sound\n");
    Equal(false, analysis.HasErrors);
    var sounds = analysis.SyntaxTree.Root.Statements.OfType<SoundStatementSyntax>().ToArray();
    Equal(4, sounds.Length);
    Equal(true, sounds[0].Channel != null && sounds[2].Channel != null && sounds[3].Channel == null);
});
Run("Out-of-range constant sound channels report SML3507", () => Equal(true,
    HasDiagnostic(Analyze("Game Window \"Audio\"\nPlay Sound \"a.wav\" On Channel 16\n"), "SML3507")));
Run("Image operators report SML3509", () => Equal(true,
    HasDiagnostic(Analyze("Dim A As Image\nDim B As Image\nPrint A = B\n"), "SML3509")));
Run("Phase 5 Text inspection built-ins use Unicode scalar signatures", () =>
{
    var analysis = Analyze("Dim Value As Text\nValue = \"A😀B\"\nPrint Text_Length(Value)\nPrint Text_Code_At(Value, 1)\nPrint Text_Slice(Value, 1, 1)\n");
    Equal(false, analysis.HasErrors);
    var calls = analysis.BoundSyntaxTree.Root.Statements.OfType<PrintStatementSyntax>()
        .SelectMany(statement => statement.Items).OfType<CallExpressionSyntax>().ToArray();
    Equal(SmileType.Number, analysis.SemanticModel.GetType(calls.Single(call => call.Identifier.Kind == SyntaxKind.TextLengthKeyword)));
    Equal(SmileType.Number, analysis.SemanticModel.GetType(calls.Single(call => call.Identifier.Kind == SyntaxKind.TextCodeAtKeyword)));
    Equal(SmileType.Text, analysis.SemanticModel.GetType(calls.Single(call => call.Identifier.Kind == SyntaxKind.TextSliceKeyword)));
    Equal(true, HasDiagnostic(Analyze("Print Text_Length(1)\n"), "SML3700"));
    Equal(true, HasDiagnostic(Analyze("Print Text_Code_At(\"A\", True)\n"), "SML3700"));
    Equal(true, HasDiagnostic(Analyze("Print Text_Slice(\"A\", 0, False)\n"), "SML3700"));
});
Run("Phase 5.1 text literals preserve embedded and trailing newlines", () =>
{
    var analysis = Analyze("Dim Value As Text\nValue = \"\nONE\nTWO\n\"\nPrint Text_Length(Value)\n");
    Equal(false, analysis.HasErrors);
    var windowsAnalysis = Analyze("Dim Value As Text\r\nValue = \"\r\nONE\r\nTWO\r\n\"\r\nPrint Text_Length(Value)\r\n");
    Equal(false, windowsAnalysis.HasErrors);
    var literal = (LiteralExpressionSyntax)windowsAnalysis.SyntaxTree.Root.Statements
        .OfType<AssignmentStatementSyntax>().Single().Expression;
    Equal("\nONE\nTWO\n", (string)literal.Value);
});
Run("Phase 5 routine Game Window capabilities are direct transitive and call-site located", () =>
{
    const string module = "Module Test.UI\nPublic Sub Draw()\nFill Rectangle 0, 0, 10, 10, WHITE\nEnd Sub\nPublic Sub Wrapper()\nCall Draw()\nEnd Sub\nPublic Sub RecursiveA()\nCall RecursiveB()\nEnd Sub\nPublic Sub RecursiveB()\nCall RecursiveA()\nCall Draw()\nEnd Sub\nPublic Sub Pure()\nEnd Sub\nEnd Module\n";
    var library = SmileLanguage.Analyze(new[] { new SmileSourceDocument(module, "UI.smile") }, SmileCompilationKind.Library);
    if (library.HasErrors)
        throw new InvalidOperationException(string.Join(" | ", library.Diagnostics.Select(diagnostic => diagnostic.Code + ": " + diagnostic.Message)));
    var routines = library.SemanticModel.Routines.Values.ToArray();
    Equal(true, routines.Single(routine => routine.DisplayName == "Test.UI.Draw").RequiresGameWindow);
    Equal(true, routines.Single(routine => routine.DisplayName == "Test.UI.Wrapper").RequiresGameWindow);
    Equal(true, routines.Single(routine => routine.DisplayName == "Test.UI.RecursiveA").RequiresGameWindow);
    Equal(false, routines.Single(routine => routine.DisplayName == "Test.UI.Pure").RequiresGameWindow);

    var console = Multi(("Program.smile", true, "Import Test.UI As UI\nCall UI.Wrapper()\nEnd Program\n"),
        ("UI.smile", false, module));
    var capabilityDiagnostic = console.Diagnostics.Single(diagnostic => diagnostic.Code == "SML3704");
    Equal("Program.smile", Path.GetFileName(capabilityDiagnostic.FilePath));
    Equal(true, capabilityDiagnostic.Message.Contains("Test.UI.Wrapper", StringComparison.Ordinal));
    Equal(0, console.Diagnostics.Count(diagnostic => diagnostic.Code == "SML3023"));

    var pureConsole = Multi(("Program.smile", true, "Import Test.UI As UI\nCall UI.Pure()\nEnd Program\n"),
        ("UI.smile", false, module));
    Equal(false, pureConsole.HasErrors);
    var game = Multi(("Program.smile", true, "Import Test.UI As UI\nGame Window \"Capabilities\"\nCall UI.Wrapper()\nEnd Program\n"),
        ("UI.smile", false, module));
    Equal(false, game.HasErrors);
});
Run("Phase 5 API keyword names remain identifiers in declaration and member contexts", () =>
{
    const string core = "Module Context.Core\nPublic Type Insets\nLeft As Number\nRight As Number\nEnd Type\nPublic Type Style\nWindow As Insets\nText As Number\nLine As Number\nEnd Type\nEnd Module\n";
    const string window = "Module Context.Window\nImport Context.Core As UI\nPublic Sub Draw(ByRef Size As UI.Style)\nSize.Window.Left = Size.Window.Right\nSize.Text = Size.Line\nEnd Sub\nEnd Module\n";
    var analysis = SmileLanguage.Analyze(new[]
    {
        new SmileSourceDocument(core, "Core.smile"),
        new SmileSourceDocument(window, "Window.smile")
    }, SmileCompilationKind.Library);
    if (analysis.HasErrors)
        throw new InvalidOperationException(string.Join(" | ", analysis.Diagnostics.Select(diagnostic => diagnostic.Code + ": " + diagnostic.Message)));
    Equal(false, analysis.HasErrors);
});
Run("Emitters resolve locals by routine identity when modules reuse routine names", () =>
{
    const string first = "Module First.Library\nPublic Sub Set(ByRef Value As Number)\nValue = 1\nEnd Sub\nEnd Module\n";
    const string second = "Module Second.Library\nPublic Sub Set(ByRef Value As Number)\nValue = 2\nEnd Sub\nEnd Module\n";
    var analysis = Multi(
        ("Program.smile", true, "Import First.Library As First\nImport Second.Library As Second\nDim Value As Number\nCall First.Set(Value)\nCall Second.Set(Value)\n"),
        ("First.smile", false, first),
        ("Second.smile", false, second));
    if (analysis.HasErrors)
        throw new InvalidOperationException(string.Join(" | ", analysis.Diagnostics.Select(diagnostic => diagnostic.Code + ": " + diagnostic.Message)));
    Equal(false, analysis.HasErrors);
    Equal(true, new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, false).Emit().Contains("routine_", StringComparison.Ordinal));
    Equal(true, new WebEmitter(analysis).Emit().Contains("async function r_", StringComparison.Ordinal));
});
Run("Every music operation requires Game Window", () =>
{
    var analysis = Analyze("Play Music \"Assets\\Background.mp3\"\nPause Music\nResume Music\nStop Music\nMusic Volume 50\n");
    Equal(5, analysis.Diagnostics.Count(diagnostic => diagnostic.Code == "SML3023"));
});
Run("Play Music rejects an empty path", () => Equal(true,
    HasDiagnostic(Analyze("Game Window \"Music\"\nPlay Music \"\"\n"), "SML3026")));
Run("Music Volume requires a number", () => Equal(true,
    HasDiagnostic(Analyze("Game Window \"Music\"\nMusic Volume \"loud\"\n"), "SML3026")));
Run("Play Music without a path reports a parser diagnostic", () => Equal(true,
    HasDiagnostic(Analyze("Game Window \"Music\"\nPlay Music\n"), "SML2001")));
Run("Play Music rejects a repeated Loop", () => Equal(true,
    HasDiagnostic(Analyze("Game Window \"Music\"\nPlay Music \"Assets\\Background.mp3\" Loop Loop\n"), "SML2001")));
Run("Pause Sound is not accepted as music syntax", () => Equal(true,
    HasDiagnostic(Analyze("Game Window \"Music\"\nPause Sound\n"), "SML2001")));
Run("Music requires the Volume subcommand", () => Equal(true,
    HasDiagnostic(Analyze("Game Window \"Music\"\nMusic 75\n"), "SML2001")));
Run("Music Volume without a value reports a parser diagnostic", () => Equal(true,
    HasDiagnostic(Analyze("Game Window \"Music\"\nMusic Volume\n"), "SML2001")));
Run("Resume Sound is not accepted as music syntax", () => Equal(true,
    HasDiagnostic(Analyze("Game Window \"Music\"\nResume Sound\n"), "SML2001")));
Run("Bare Stop remains malformed", () => Equal(true,
    HasDiagnostic(Analyze("Game Window \"Music\"\nStop\n"), "SML2001")));
Run("Load Text File keywords are shared and case-insensitive", () =>
{
    Equal(SyntaxKind.FileKeyword, SyntaxFacts.GetKeywordKind("file"));
    Equal(SyntaxKind.IntoKeyword, SyntaxFacts.GetKeywordKind("InTo"));
    Equal(SyntaxKind.CountKeyword, SyntaxFacts.GetKeywordKind("Count"));
});
Run("Load Text File analyzes for a one-dimensional array", () => Equal(false,
    Analyze("Dim Bytes[8]\nLoad Text File \"sample.txt\" Into Bytes Count ByteCount\n").HasErrors));
Run("Load Text File records its shared syntax", () =>
{
    var load = Analyze("Dim Bytes[8]\nLoad Text File \"sample.txt\" Into Bytes Count ByteCount\n")
        .SyntaxTree.Root.Statements.OfType<TextFileLoadStatementSyntax>().Single();
    Equal("sample.txt", ((LiteralExpressionSyntax)load.Path).Value as string);
    Equal("Bytes", load.Destination.Text);
    Equal("ByteCount", load.CountIdentifier.Text);
});
Run("Load Text File accepts a caller-provided Text expression", () => Equal(false,
    Analyze("Dim Bytes[8]\nDim MapPath As Text\nMapPath = \"Maps\\Town.smilemap\"\nLoad Text File MapPath Into Bytes Count ByteCount\n").HasErrors));
Run("Load Text File rejects a non-Text path expression", () => Equal(true,
    HasDiagnostic(Analyze("Dim Bytes[8]\nLoad Text File 42 Into Bytes Count ByteCount\n"), "SML3027")));
Run("Load Text File rejects an empty path", () => Equal(true,
    HasDiagnostic(Analyze("Dim Bytes[8]\nLoad Text File \"\" Into Bytes Count ByteCount\n"), "SML3027")));
Run("Load Text File rejects an unknown destination", () => Equal(true,
    HasDiagnostic(Analyze("Load Text File \"sample.txt\" Into Bytes Count ByteCount\n"), "SML3027")));
Run("Load Text File rejects a scalar destination", () => Equal(true,
    HasDiagnostic(Analyze("Bytes = 0\nLoad Text File \"sample.txt\" Into Bytes Count ByteCount\n"), "SML3027")));
Run("Load Text File rejects a two-dimensional destination", () => Equal(true,
    HasDiagnostic(Analyze("Dim Bytes[4, 4]\nLoad Text File \"sample.txt\" Into Bytes Count ByteCount\n"), "SML3027")));
Run("Existing persistence Load syntax remains valid", () => Equal(false,
    Analyze("Load HighScore From \"HighScore\" Default 0\n").HasErrors));
Run("Completion catalog uses shared keywords and built-in signatures", () =>
{
    var completions = SmileCompletionService.GetCompletions(Analyze("PRI"), 3);
    Equal(SmileCompletionKind.Keyword,
        completions.Single(completion => completion.DisplayText == "Print").Kind);
    var rgb = completions.Single(completion => completion.DisplayText == "Rgb");
    Equal(SmileCompletionKind.BuiltInFunction, rgb.Kind);
    Equal("Built-in function Rgb(red, green, blue)", rgb.Description);
    Equal(true, completions.Any(completion => completion.DisplayText == "Game_Closed"));
    Equal(true, completions.Any(completion => completion.DisplayText == "KEY_ENTER"));
    Equal(true, completions.Any(completion => completion.DisplayText == "Image"));
    Equal(true, completions.Any(completion => completion.DisplayText == "Clip"));
    Equal(true, completions.Any(completion => completion.DisplayText == "Image_Width"));
    Equal(false, completions.Any(completion => completion.DisplayText == "PRI"));
});
Run("Completion catalog includes visible variables arrays and routines", () =>
{
    const string source = "Score = 1\nDim Board[4, 5]\nSub Move(PlayerX)\nStep = 2\nPrint PlayerX\nEnd Sub\nSub Other()\nHidden = 3\nEnd Sub\n";
    var completions = SmileCompletionService.GetCompletions(Analyze(source), source.IndexOf("Print", StringComparison.Ordinal));
    Equal(true, completions.Any(completion => completion.DisplayText == "Score"));
    Equal("Number array Board[4, 5]", completions.Single(completion => completion.DisplayText == "Board").Description);
    Equal(true, completions.Any(completion => completion.DisplayText == "PlayerX"));
    Equal(true, completions.Any(completion => completion.DisplayText == "Step"));
    Equal(false, completions.Any(completion => completion.DisplayText == "Hidden"));
    Equal("Sub Move(PlayerX As Number)", completions.Single(completion => completion.DisplayText == "Move").Description);
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
    const string source = "Dim Values[2]\nSub SetValue(Index)\nValues[Index] = 9 / 2\nEnd Sub\nGame Window \"Test\" Size 320 By 180\nCall SetValue(0)\nIf Values[0] = 4 Then\nShow Screen\nEnd If\nEnd Program\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var javascript = new WebEmitter(analysis).Emit();
    Equal(true, javascript.Contains("smile.div(9, 2)"));
    Equal(true, javascript.Contains("smile.set("));
    Equal(true, javascript.Contains("await smile.showScreen()"));
});
Run("Web emitter lowers console output waits and screen clearing", () =>
{
    var analysis = Analyze("Print True; 42\nWait 1 Milliseconds\nClear Screen\n");
    Equal(false, analysis.HasErrors);
    var javascript = new WebEmitter(analysis).Emit();
    Equal(true, javascript.Contains("smile.print([smile.booleanText(true), 42]"));
    Equal(true, javascript.Contains("await smile.wait(1)"));
    Equal(true, javascript.Contains("smile.clearScreen()"));
});
Run("Web emitter lowers the complete shared game surface", () =>
{
    const string source = "Dim Bytes[8]\nGame Window \"Test\"\nSub DrawFrame()\nFill Circle 10, 10, 4, WHITE\nDraw Line 0, 0, 10, 10, WHITE\nShow Screen\nEnd Sub\nCall DrawFrame()\nIf Key_Held(KEY_W) Then\nPlay Sound \"Assets\\Effect.wav\"\nEnd If\nLoad Text File \"Maps\\test.map\" Into Bytes Count ByteCount\nPlay Music \"Assets\\Music.mp3\" Loop\nMusic Volume 50\nPause Music\nResume Music\nStop Music\nFor Index = 0 To 2\nExit For\nEnd For\nDo\nExit Do\nLoop\nSelect Case ByteCount\nCase 0\nByteCount = 1\nCase Else\nByteCount = 2\nEnd Select\nEnd Program\n";
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
Run("Native and Web emitters lower a dynamic Load Text File path", () =>
{
    const string source = "Dim Bytes[8]\nDim MapPath As Text\nMapPath = \"Maps\\Town.smilemap\"\nLoad Text File MapPath Into Bytes Count ByteCount\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    Equal(true, new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, false).Emit()
        .Contains("call smile_load_text_file", StringComparison.Ordinal));
    Equal(true, new WebEmitter(analysis).Emit().Contains("await smile.loadTextFile(", StringComparison.Ordinal));
});
Run("Web output writer creates deterministic static files", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "smile-web-output-test-" + Guid.NewGuid().ToString("N"));
    try
    {
        var analysis = Analyze("Game Window \"Test\"\nShow Screen\nEnd Program\n");
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
    var analysis = SmileLanguage.Analyze("Print 1\n", "Single.smile");
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
        new SmileSourceDocument("Print 1\n", "One.smile", true),
        new SmileSourceDocument("Print 2\n", "Two.smile", true)
    }),
    "requires exactly one startup source; found 2"));
Run("Multi-source API rejects duplicate normalized paths", () => ThrowsContains(
    () => SmileLanguage.Analyze(new[]
    {
        new SmileSourceDocument("Print 1\n", "Duplicate.smile", true),
        new SmileSourceDocument("Sub Work()\nEnd Sub\n", ".\\Duplicate.smile")
    }),
    "Duplicate SMILE source path"));
Run("Multi-source analysis exposes distinct physical syntax trees", () =>
{
    var analysis = Multi(
        ("Program.smile", true, "Call Work()\n"),
        ("Support.smile", false, "Sub Work()\nPrint 1\nEnd Sub\n"));
    Equal(2, analysis.SyntaxTrees.Count);
    Equal("Program.smile", Path.GetFileName(analysis.SyntaxTree.Source.FilePath));
    Equal("Support.smile", Path.GetFileName(analysis.SyntaxTrees[1].Source.FilePath));
});
Run("Cross-file routines declarations arrays and startup globals bind together", () =>
{
    var analysis = Multi(
        ("Program.smile", true, "Score = 7\nCall ResetState()\nPrint StateValue()\n"),
        ("GameState.smile", false, "Const BaseValue = 3\nDim State[2]\nSub ResetState()\nState[0] = BaseValue\nCall AdvanceState()\nEnd Sub\n"),
        ("Drawing.smile", false, "Sub AdvanceState()\nState[0] = State[0] + Score\nEnd Sub\nFunction StateValue()\nReturn State[0]\nEnd Function\n"));
    Equal(false, analysis.HasErrors);
    Equal(true, analysis.SemanticModel.Symbols.ContainsKey("Score"));
    Equal(true, analysis.SemanticModel.Symbols.ContainsKey("State"));
    Equal(true, analysis.SemanticModel.Routines.ContainsKey("AdvanceState"));
});
Run("Cross-file routine visibility does not depend on support source order", () => Equal(false,
    Multi(
        ("Program.smile", true, "Call First()\n"),
        ("Later.smile", false, "Sub First()\nCall Second()\nEnd Sub\n"),
        ("Earlier.smile", false, "Sub Second()\nEnd Sub\n")).HasErrors));
Run("Cross-file constants and array dimensions are source-order independent", () =>
{
    var analysis = Multi(
        ("Program.smile", true, "Dim StartupValues[MaximumValues]\nCall InitializeArrays()\nPrint MaximumValues\n"),
        ("Arrays.smile", false, "Dim SharedValues[MaximumValues]\nSub InitializeArrays()\nSharedValues[0] = MaximumValues\nEnd Sub\n"),
        ("Derived.smile", false, "Const MaximumValues = BaseValues + ExtraValues\n"),
        ("Base.smile", false, "Const BaseValues = 4\nConst ExtraValues = 4\n"));
    Equal(false, analysis.HasErrors);
    Equal(8L, analysis.SemanticModel.Symbols["MaximumValues"].ConstantValue);
    Equal(8, analysis.SemanticModel.Symbols["StartupValues"].ArrayDimensions[0]);
    Equal(8, analysis.SemanticModel.Symbols["SharedValues"].ArrayDimensions[0]);
});
Run("Reversing support declaration order preserves constant and array results", () =>
{
    var analysis = Multi(
        ("Program.smile", true, "Dim StartupValues[MaximumValues]\nPrint MaximumValues\n"),
        ("Base.smile", false, "Const BaseValues = 3\nConst ExtraValues = 1\n"),
        ("Derived.smile", false, "Const MaximumValues = BaseValues + ExtraValues\n"),
        ("Arrays.smile", false, "Dim SharedValues[MaximumValues]\n"));
    Equal(false, analysis.HasErrors);
    Equal(4L, analysis.SemanticModel.Symbols["MaximumValues"].ConstantValue);
    Equal(4, analysis.SemanticModel.Symbols["SharedValues"].ArrayDimensions[0]);
});
Run("Circular constants report one deterministic physical-file diagnostic", () =>
{
    var diagnostic = Multi(
        ("Program.smile", true, "Print FirstValue\n"),
        ("First.smile", false, "Const FirstValue = SecondValue + 1\n"),
        ("Second.smile", false, "Const SecondValue = FirstValue + 1\n"))
        .Diagnostics.Single(item => item.Code == "SML3029");
    Equal("First.smile", Path.GetFileName(diagnostic.FilePath));
    Equal(true, diagnostic.Message.Contains("FirstValue -> SecondValue -> FirstValue", StringComparison.Ordinal));
});
Run("Const and routine names share one case-insensitive project namespace", () =>
{
    var diagnostic = Multi(
        ("Program.smile", true, "Print SharedName\n"),
        ("Value.smile", false, "Const SharedName = 1\n"),
        ("Routine.smile", false, "Sub sharedname()\nEnd Sub\n"))
        .Diagnostics.Single(item => item.Code == "SML3005");
    Equal("Routine.smile", Path.GetFileName(diagnostic.FilePath));
});
Run("Dim and routine names share one case-insensitive project namespace", () =>
{
    var diagnostic = Multi(
        ("Program.smile", true, "Dim Inventory[4]\n"),
        ("Routine.smile", false, "Function inventory()\nReturn 1\nEnd Function\n"))
        .Diagnostics.Single(item => item.Code == "SML3005");
    Equal("Routine.smile", Path.GetFileName(diagnostic.FilePath));
});
Run("Implicit startup globals share the project routine namespace", () =>
{
    var diagnostic = Multi(
        ("Program.smile", true, "Score = 1\nPrint Score\n"),
        ("Routine.smile", false, "Function score()\nReturn 1\nEnd Function\n"))
        .Diagnostics.Single(item => item.Code == "SML3005");
    Equal("Routine.smile", Path.GetFileName(diagnostic.FilePath));
});
Run("Game hierarchy projection includes startup alternate support and assets exactly once", () =>
{
    var projectPath = Path.GetFullPath("examples/SourceVisibilityBasics/SourceVisibilityBasics.smileproj");
    var sourceSet = SmileProjectSourceSet.Load(projectPath);
    var projection = SmileProjectHierarchyProjection.Create(sourceSet, "Game");
    Equal("References|Program.smile|Program-NoDemo.smile|Helpers.smile|Assets|Readme.txt",
        string.Join("|", projection.Select(item => item.Caption)));
    foreach (var source in sourceSet.Items)
        Equal(1, projection.Count(item => item.Kind == SmileProjectHierarchyItemKind.Source &&
            string.Equals(item.FullPath, source.FullPath, StringComparison.OrdinalIgnoreCase)));
    Equal(projection.Count, projection.Select(item => item.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count());
});
Run("Phase 4.2 asset globs resolve exactly deduplicate overlaps and project only resolved files", () =>
{
    var projectPath = Path.GetFullPath("examples/Phase4AssetPublication/Phase4AssetPublication.smileproj");
    var expected = File.ReadAllLines(Path.Combine(Path.GetDirectoryName(projectPath)!, "ExpectedAssetPaths.txt"));
    var sourceSet = SmileProjectSourceSet.Load(projectPath);
    Equal(0, sourceSet.AssetManifest.Diagnostics.Count);
    Equal(string.Join("|", expected), string.Join("|", sourceSet.AssetManifest.AssetPaths));
    Equal(2, sourceSet.AssetManifest.Items.Single(item => item.LogicalPath == "Assets/UI/Window.png")
        .MatchedIncludes.Count);
    Equal(0, sourceSet.AssetManifest.Includes.Single(include =>
        include.NormalizedPattern == "Assets/Empty/**/*").IsValid ? 0 : 1);

    var hierarchy = SmileProjectHierarchyProjection.Create(sourceSet, "Game");
    var hierarchyAssets = hierarchy
        .Where(item => item.Kind == SmileProjectHierarchyItemKind.Asset)
        .Select(item => Path.GetRelativePath(sourceSet.ProjectDirectory, item.FullPath!).Replace('\\', '/'))
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToArray();
    Equal(string.Join("|", expected), string.Join("|", hierarchyAssets));
    Equal(false, hierarchyAssets.Contains("Assets/UI/Sub/Nested.png", StringComparer.Ordinal));
    Equal(false, hierarchyAssets.Contains("Assets/Audio/Notes.txt", StringComparer.Ordinal));
    Equal(false, hierarchyAssets.Contains("Assets/Unlisted/Secret.txt", StringComparer.Ordinal));
    Equal(false, hierarchy.Any(item => item.Kind == SmileProjectHierarchyItemKind.Folder &&
        item.FullPath.EndsWith(Path.Combine("Assets", "Empty"), StringComparison.OrdinalIgnoreCase)));
});
Run("Missing explicit assets report SML3601 at the project Include", () =>
{
    var projectPath = Path.GetFullPath("examples/InvalidPhase4Assets/MissingExplicit/MissingExplicit.smileproj");
    var sourceSet = SmileProjectSourceSet.Load(projectPath);
    var diagnostic = sourceSet.AssetManifest.Diagnostics.Single(item => item.Code == "SML3601");
    Equal(true, diagnostic.Line > 1);
    Equal(true, diagnostic.Column > 1);
    ThrowsProjectDiagnostic(sourceSet.ValidateAssetsForBuild, "SML3601");
});
Run("Library project assets report SML3606 while the project remains loadable", () =>
{
    var projectPath = Path.GetFullPath("examples/InvalidPhase4Assets/LibraryAsset/LibraryAsset.smilelibproj");
    var sourceSet = SmileProjectSourceSet.Load(projectPath);
    Equal("SML3606", sourceSet.AssetManifest.Diagnostics.Single().Code);
    ThrowsProjectDiagnostic(sourceSet.ValidateAssetsForBuild, "SML3606");
});
Run("Asset matching is ordinal case-sensitive and reports SML3602", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileAssetCaseTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(Path.Combine(directory, "Assets", "UI"));
    try
    {
        File.WriteAllText(Path.Combine(directory, "Program.smile"), "End Program\n");
        File.WriteAllText(Path.Combine(directory, "Assets", "UI", "Window.png"), "image");
        var projectPath = Path.Combine(directory, "Case.smileproj");
        File.WriteAllText(projectPath, "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" StartupOnly=\"true\" /><Asset Include=\"Assets\\ui\\window.png\" /></ItemGroup></SmileProject>");
        var sourceSet = SmileProjectSourceSet.Load(projectPath);
        var diagnostic = sourceSet.AssetManifest.Diagnostics.Single(item => item.Code == "SML3602");
        Equal(true, diagnostic.Message.Contains("Assets/UI/Window.png", StringComparison.Ordinal));
        ThrowsProjectDiagnostic(sourceSet.ValidateAssetsForBuild, "SML3602");
    }
    finally
    {
        Directory.Delete(directory, true);
    }
});
Run("Asset stars match zero characters and question marks match exactly one", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileAssetQuestionTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(Path.Combine(directory, "Assets"));
    try
    {
        File.WriteAllText(Path.Combine(directory, "Program.smile"), "End Program\n");
        File.WriteAllText(Path.Combine(directory, "Assets", "a-click.wav"), "one");
        File.WriteAllText(Path.Combine(directory, "Assets", "ab-click.wav"), "two");
        File.WriteAllText(Path.Combine(directory, "Assets", "abc-click.wav"), "three");
        File.WriteAllText(Path.Combine(directory, "Assets", ".png"), "zero");
        var projectPath = Path.Combine(directory, "Question.smileproj");
        File.WriteAllText(projectPath, "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" StartupOnly=\"true\" /><Asset Include=\"Assets\\??-click.wav\" /><Asset Include=\"Assets\\*.png\" /></ItemGroup></SmileProject>");
        var manifest = SmileProjectSourceSet.Load(projectPath).AssetManifest;
        Equal("Assets/.png|Assets/ab-click.wav", string.Join("|", manifest.AssetPaths));
    }
    finally
    {
        Directory.Delete(directory, true);
    }
});
Run("Invalid asset path and glob forms report SML3600", () =>
{
    foreach (var include in new[]
             {
                 "C:\\Assets\\Image.png", "\\\\server\\share\\Image.png", "https://example.test/Image.png",
                 "..\\Image.png", "Assets\\ab**\\Image.png", "Assets\\[ab].png", "Assets\\{a,b}.png",
                 "Assets\\!Image.png", "Assets\\A.png;Assets\\B.png"
             })
    {
        var xml = $"<SmileProject><PropertyGroup><ProjectKind>Game</ProjectKind><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" StartupOnly=\"true\" /><Asset Include=\"{include}\" /></ItemGroup></SmileProject>";
        Equal("SML3600", ProjectSources(xml).AssetManifest.Diagnostics.Single().Code);
    }
});
Run("Portable destination collisions report both source paths as SML3603", () =>
{
    var diagnostic = SmileProjectAssetResolver.FindDestinationCollision("Collision.smileproj", new[]
    {
        new KeyValuePair<string, string>("Assets/UI/Icon.png", @"D:\\Project\\Assets\\UI\\Icon.png"),
        new KeyValuePair<string, string>("Assets/ui/icon.png", @"D:\\Project\\Assets\\ui\\icon.png")
    });
    Equal("SML3603", diagnostic!.Code);
    Equal(true, diagnostic.Message.Contains(@"Assets\\UI\\Icon.png", StringComparison.Ordinal));
    Equal(true, diagnostic.Message.Contains(@"Assets\\ui\\icon.png", StringComparison.Ordinal));
});
Run("Asset publisher safely removes only stale owned files and preserves unrelated output", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileAssetPublishTests-" + Guid.NewGuid().ToString("N"));
    var output = Path.Combine(directory, "output");
    Directory.CreateDirectory(Path.Combine(directory, "Assets"));
    Directory.CreateDirectory(output);
    try
    {
        File.WriteAllText(Path.Combine(directory, "Program.smile"), "End Program\n");
        File.WriteAllText(Path.Combine(directory, "Assets", "Old.txt"), "old");
        File.WriteAllText(Path.Combine(directory, "Assets", "New.txt"), "new");
        File.WriteAllText(Path.Combine(output, "game.js"), "generated");
        var projectPath = Path.Combine(directory, "Publish.smileproj");
        void WriteProject(string asset) => File.WriteAllText(projectPath, $"<SmileProject><PropertyGroup><ProjectKind>Game</ProjectKind><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" StartupOnly=\"true\" /><Asset Include=\"Assets\\{asset}\" /></ItemGroup></SmileProject>");

        WriteProject("Old.txt");
        SmileProjectAssetPublisher.Publish(SmileProjectSourceSet.Load(projectPath).AssetManifest,
            output, "Publish", "web");
        Equal(true, File.Exists(Path.Combine(output, "Assets", "Old.txt")));

        WriteProject("New.txt");
        SmileProjectAssetPublisher.Publish(SmileProjectSourceSet.Load(projectPath).AssetManifest,
            output, "Publish", "web");
        Equal(false, File.Exists(Path.Combine(output, "Assets", "Old.txt")));
        Equal(true, File.Exists(Path.Combine(output, "Assets", "New.txt")));
        Equal("generated", File.ReadAllText(Path.Combine(output, "game.js")));
    }
    finally
    {
        Directory.Delete(directory, true);
    }
});
Run("Malformed prior asset manifests are ignored without unsafe deletion and replaced safely", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileAssetManifestTests-" + Guid.NewGuid().ToString("N"));
    var output = Path.Combine(directory, "output");
    Directory.CreateDirectory(Path.Combine(directory, "Assets"));
    Directory.CreateDirectory(output);
    try
    {
        File.WriteAllText(Path.Combine(directory, "Program.smile"), "End Program\n");
        File.WriteAllText(Path.Combine(directory, "Assets", "Safe.txt"), "safe");
        var outside = Path.Combine(directory, "outside.txt");
        File.WriteAllText(outside, "untouched");
        var projectPath = Path.Combine(directory, "Manifest.smileproj");
        File.WriteAllText(projectPath, "<SmileProject><PropertyGroup><ProjectKind>Game</ProjectKind><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" StartupOnly=\"true\" /><Asset Include=\"Assets\\Safe.txt\" /></ItemGroup></SmileProject>");
        File.WriteAllText(Path.Combine(output, "smile-assets.json"), "{\"formatVersion\":1,\"applicationIdentity\":\"Manifest\",\"target\":\"web\",\"assets\":[\"../outside.txt\"]}");

        var result = SmileProjectAssetPublisher.Publish(SmileProjectSourceSet.Load(projectPath).AssetManifest,
            output, "Manifest", "web");
        Equal("SML3605", result.Warnings.Single().Code);
        Equal("untouched", File.ReadAllText(outside));
        Equal(true, File.ReadAllText(Path.Combine(output, "smile-assets.json"))
            .Contains("Safe.txt", StringComparison.Ordinal));
    }
    finally
    {
        Directory.Delete(directory, true);
    }
});
Run("Explicit ApplicationId keeps a stable native manifest and safely migrates legacy output names", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileAssetIdentityTests-" + Guid.NewGuid().ToString("N"));
    var output = Path.Combine(directory, "output");
    Directory.CreateDirectory(Path.Combine(directory, "Assets"));
    Directory.CreateDirectory(output);
    try
    {
        File.WriteAllText(Path.Combine(directory, "Program.smile"), "End Program\n");
        File.WriteAllText(Path.Combine(directory, "Assets", "A.txt"), "A");
        File.WriteAllText(Path.Combine(directory, "Assets", "B.txt"), "B");
        File.WriteAllText(Path.Combine(output, "sentinel.txt"), "user-owned");
        var projectPath = Path.Combine(directory, "Identity.smileproj");
        void WriteProject(params string[] assets) => File.WriteAllText(projectPath,
            "<SmileProject><PropertyGroup><ProjectKind>Game</ProjectKind><StartupFile>Program.smile</StartupFile>" +
            "<OutputName>Renamed</OutputName><ApplicationId>smile.tests.asset-identity</ApplicationId>" +
            "</PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" StartupOnly=\"true\" />" +
            string.Concat(assets.Select(asset => $"<Asset Include=\"Assets\\{asset}\" />")) +
            "</ItemGroup></SmileProject>");

        WriteProject("A.txt", "B.txt");
        var legacy = SmileProjectSourceSet.Load(projectPath).AssetManifest;
        var legacyResult = SmileProjectAssetPublisher.Publish(legacy, output,
            "smile.tests.asset-identity", "windows-x64", "OldName");
        Equal("OldName.smile-assets.json", Path.GetFileName(legacyResult.ManifestPath));

        var mismatchedPath = Path.Combine(output, "Mismatched.smile-assets.json");
        var malformedPath = Path.Combine(output, "Malformed.smile-assets.json");
        File.WriteAllText(mismatchedPath,
            "{\"formatVersion\":1,\"applicationIdentity\":\"smile.tests.other\",\"target\":\"windows-x64\",\"assets\":[\"sentinel.txt\"]}");
        File.WriteAllText(malformedPath, "{not-json");

        WriteProject("A.txt");
        var current = SmileProjectSourceSet.Load(projectPath).AssetManifest;
        var migrated = SmileProjectAssetPublisher.Publish(current, output,
            "smile.tests.asset-identity", "windows-x64", "NewName",
            hasExplicitApplicationIdentity: true);

        Equal("smile.tests.asset-identity.smile-assets.json", Path.GetFileName(migrated.ManifestPath));
        Equal(true, File.Exists(Path.Combine(output, "Assets", "A.txt")));
        Equal(false, File.Exists(Path.Combine(output, "Assets", "B.txt")));
        Equal(false, File.Exists(Path.Combine(output, "OldName.smile-assets.json")));
        Equal(true, File.Exists(mismatchedPath));
        Equal(true, File.Exists(malformedPath));
        Equal("user-owned", File.ReadAllText(Path.Combine(output, "sentinel.txt")));
        Equal(true, migrated.Warnings.Any(warning => warning.Code == "SML3605"));

        var changedIdentity = SmileProjectAssetPublisher.Publish(current, output,
            "smile.tests.changed-identity", "windows-x64", "AnotherName",
            hasExplicitApplicationIdentity: true);
        Equal("smile.tests.changed-identity.smile-assets.json", Path.GetFileName(changedIdentity.ManifestPath));
        Equal(true, File.Exists(migrated.ManifestPath));
        Equal(true, File.Exists(mismatchedPath));
        Equal(true, File.Exists(malformedPath));
    }
    finally
    {
        Directory.Delete(directory, true);
    }
});
Run("Asset publication I/O failures report SML3604 and do not claim success", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileAssetFailureTests-" + Guid.NewGuid().ToString("N"));
    var output = Path.Combine(directory, "output");
    Directory.CreateDirectory(Path.Combine(directory, "Assets"));
    try
    {
        File.WriteAllText(Path.Combine(directory, "Program.smile"), "End Program\n");
        var assetPath = Path.Combine(directory, "Assets", "Vanishing.txt");
        File.WriteAllText(assetPath, "temporary");
        var projectPath = Path.Combine(directory, "Failure.smileproj");
        File.WriteAllText(projectPath, "<SmileProject><PropertyGroup><ProjectKind>Game</ProjectKind><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" StartupOnly=\"true\" /><Asset Include=\"Assets\\Vanishing.txt\" /></ItemGroup></SmileProject>");
        var manifest = SmileProjectSourceSet.Load(projectPath).AssetManifest;
        File.Delete(assetPath);
        ThrowsProjectDiagnostic(() => SmileProjectAssetPublisher.Publish(manifest, output,
            "Failure", "web"), "SML3604");
        Equal(false, File.Exists(Path.Combine(output, "smile-assets.json")));
    }
    finally
    {
        Directory.Delete(directory, true);
    }
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
    var projection = SmileProjectHierarchyProjection.Create(sourceSet, "Console");
    Equal("References|Program.smile|Second.smile|Third.smile", string.Join("|", projection.Select(item => item.Caption)));
});
Run("Root hierarchy traversal reaches every source once without missing IDs cycles or Assets ordering gaps", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileHierarchyTraversalTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(Path.Combine(directory, "Assets"));
    try
    {
        foreach (var name in new[] { "Program.smile", "BeforeAssets.smile", "AfterAssets.smile" })
            File.WriteAllText(Path.Combine(directory, name), "End Program\n");
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
        var projection = SmileProjectHierarchyProjection.Create(sourceSet, "Game");
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
        File.WriteAllText(programPath, "End Program\n");
        File.WriteAllText(supportPath, "Const Existing = 1\n");
        File.WriteAllText(dynamicPath, "Const Dynamic = 2\n");
        File.WriteAllText(projectPath, "<SmileProject><PropertyGroup><ProjectKind>Console</ProjectKind><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" StartupOnly=\"true\" /><SmileSource Include=\"Support.smile\" /></ItemGroup></SmileProject>");
        var identities = new SmileProjectHierarchyIdentityMap();
        var initial = SmileProjectHierarchyProjection.Create(SmileProjectSourceSet.Load(projectPath), "Console");
        var initialIds = identities.Apply(initial);
        var addedSet = SmileProjectFileEditor.AddSource(projectPath, dynamicPath);
        var blankLinesAfterAdd = File.ReadAllLines(projectPath).Count(string.IsNullOrWhiteSpace);
        var added = SmileProjectHierarchyProjection.Create(addedSet, "Console");
        var addedIds = identities.Apply(added);
        Equal(initial.Count + 1, added.Count);
        foreach (var item in initial)
            Equal(initialIds[item.Key], addedIds[item.Key]);
        var dynamicItem = added.Single(item => string.Equals(item.FullPath, dynamicPath, StringComparison.OrdinalIgnoreCase));
        Equal(true, addedIds[dynamicItem.Key] is > 0 and < 0xfffffffd);
        ThrowsContains(() => SmileProjectFileEditor.AddSource(projectPath, dynamicPath), "already included in the project");
        var removedSet = SmileProjectFileEditor.RemoveSource(projectPath, dynamicPath);
        Equal(false, SmileProjectHierarchyProjection.Create(removedSet, "Console")
            .Any(item => string.Equals(item.FullPath, dynamicPath, StringComparison.OrdinalIgnoreCase)));
        Equal(true, File.Exists(dynamicPath));
        var readdedSet = SmileProjectFileEditor.AddSource(projectPath, dynamicPath);
        var readded = SmileProjectHierarchyProjection.Create(readdedSet, "Console");
        Equal(1, readded.Count(item => string.Equals(item.FullPath, dynamicPath, StringComparison.OrdinalIgnoreCase)));
        Equal(addedIds[dynamicItem.Key], identities.Apply(readded)[dynamicItem.Key]);
        var reloaded = SmileProjectHierarchyProjection.Create(SmileProjectSourceSet.Load(projectPath), "Console");
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
        File.WriteAllText(programPath, "End Program\n");
        File.WriteAllText(untrackedPath, "Const Untracked = 1\n");
        var projectPath = Path.Combine(directory, "Missing.smileproj");
        File.WriteAllText(projectPath, "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" StartupOnly=\"true\" /><SmileSource Include=\"Missing.smile\" /></ItemGroup></SmileProject>");

        var sourceSet = SmileProjectSourceSet.Load(projectPath);
        var missingProjection = SmileProjectHierarchyProjection.Create(sourceSet, "Console");
        Equal(3, missingProjection.Count);
        Equal(false, missingProjection.Single(item => string.Equals(item.FullPath, missingPath,
            StringComparison.OrdinalIgnoreCase)).Exists);
        Equal(false, missingProjection.Any(item => string.Equals(item.FullPath, untrackedPath,
            StringComparison.OrdinalIgnoreCase)));
        ThrowsContains(sourceSet.ValidateFiles, "Support source file was not found");

        File.WriteAllText(missingPath, "Const Restored = 1\n");
        var restoredProjection = SmileProjectHierarchyProjection.Create(
            SmileProjectSourceSet.Load(projectPath), "Console");
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
        File.WriteAllText(sharedPath, "Const Shared = 1\n");
        File.WriteAllText(Path.Combine(directory, "One.smile"), "Print Shared\n");
        File.WriteAllText(Path.Combine(directory, "Two.smile"), "Print Shared\n");
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
    var registration = registry.Register(filePath, "Print 1\n", () => invalidations++);
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
        ("Program.smile", true, "Print 1\n"),
        ("Support.smile", false, "\nScore = 1\n"));
    var diagnostic = analysis.Diagnostics.Single(item => item.Code == "SML3028");
    Equal("Support.smile", Path.GetFileName(diagnostic.FilePath));
    Equal(2, diagnostic.Line);
});
Run("Support Game Window is rejected in the support file", () =>
{
    var diagnostic = Multi(
        ("Program.smile", true, "Print 1\n"),
        ("Support.smile", false, "Game Window \"Wrong\"\n"))
        .Diagnostics.Single(item => item.Code == "SML3028");
    Equal(true, diagnostic.Message.Contains("Game Window"));
    Equal("Support.smile", Path.GetFileName(diagnostic.FilePath));
});
Run("Support End Program is rejected in the support file", () =>
{
    var diagnostic = Multi(
        ("Program.smile", true, "Print 1\n"),
        ("Support.smile", false, "End Program\n"))
        .Diagnostics.Single(item => item.Code == "SML3028");
    Equal(true, diagnostic.Message.Contains("End Program"));
});
Run("Duplicate globals across files report the later file", () =>
{
    var diagnostic = Multi(
        ("Program.smile", true, "Print Shared\n"),
        ("First.smile", false, "Const Shared = 1\n"),
        ("Second.smile", false, "Dim shared[2]\n"))
        .Diagnostics.Single(item => item.Code == "SML3005");
    Equal("Second.smile", Path.GetFileName(diagnostic.FilePath));
});
Run("Duplicate routines across files report the later file", () =>
{
    var diagnostic = Multi(
        ("Program.smile", true, "Call Work()\n"),
        ("First.smile", false, "Sub Work()\nEnd Sub\n"),
        ("Second.smile", false, "Sub work()\nEnd Sub\n"))
        .Diagnostics.Single(item => item.Code == "SML3015");
    Equal("Second.smile", Path.GetFileName(diagnostic.FilePath));
});
Run("Parser diagnostics retain support-file line and column", () =>
{
    var diagnostic = Multi(
        ("Program.smile", true, "Print 1\n"),
        ("Broken.smile", false, "Sub Work()\n\nPrint (\nEnd Sub\n"))
        .Diagnostics.First(item => item.Code.StartsWith("SML2", StringComparison.Ordinal));
    Equal("Broken.smile", Path.GetFileName(diagnostic.FilePath));
    Equal(4, diagnostic.Line);
});
Run("Cross-file completion uses the active support file scope", () =>
{
    const string support = "Sub Move(Amount)\nLocalStep = 1\nPrint Amount\nEnd Sub\n";
    var analysis = Multi(
        ("Program.smile", true, "Score = 1\nCall Move(2)\n"),
        ("Support.smile", false, support));
    var completions = SmileCompletionService.GetCompletions(
        analysis, analysis.GetSyntaxTree(Path.GetFullPath("Support.smile")), support.IndexOf("Print", StringComparison.Ordinal));
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
        <SmileSource Include="Program.smile" StartupOnly="True" />
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
        File.WriteAllText(programPath, "End Program\n");
        File.WriteAllText(alternatePath, "End Program\n");
        File.WriteAllText(supportPath, "Const Value = 1\n");
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
        ("Program.smile", true, "Score = 1\nCall Work()\n"),
        ("Support.smile", false, "Sub Work()\nScore = Score + 1\nEnd Sub\n"));
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
Run("Native debug sites expose in-scope SMILE values as named helper parameters", () =>
{
    var analysis = Analyze(
        "Const MAX_SCORE = 99\n" +
        "Dim Score As Number\n" +
        "Dim Ready As Boolean\n" +
        "Dim Message As Text\n" +
        "Dim Lives As Number\n" +
        "Dim Level As Number\n" +
        "Dim Bonus As Number\n" +
        "Score = 1\n" +
        "Call Work(Score)\n" +
        "Sub Work(Value As Number)\n" +
        "Dim LocalValue As Number\n" +
        "LocalValue = Value + 1\n" +
        "Print LocalValue\n" +
        "End Sub\n");
    Equal(false, analysis.HasErrors);
    var emitter = new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, true);
    var assembly = emitter.Emit();
    var topLevelSite = emitter.DebugSites.Single(site => site.Line == 8);
    var topLevelDebugSource = CompilerDriver.BuildDebugSource(new[] { topLevelSite });
    Equal(true, topLevelDebugSource.Contains("long long MAX_SCORE", StringComparison.Ordinal));
    Equal(true, topLevelDebugSource.Contains("long long Score", StringComparison.Ordinal));
    Equal(true, topLevelDebugSource.Contains("SmileDebugBoolean Ready", StringComparison.Ordinal));
    Equal(true, topLevelDebugSource.Contains("const char* Message", StringComparison.Ordinal));
    Equal(true, assembly.Contains($"call {topLevelSite.HelperName}", StringComparison.Ordinal));
    Equal(true, assembly.Contains("mov QWORD PTR [rsp+32], rax", StringComparison.Ordinal));
    Equal(true, assembly.Contains("add rax, 16", StringComparison.Ordinal));

    var routineSite = emitter.DebugSites.Single(site => site.Line == 12);
    var routineDebugSource = CompilerDriver.BuildDebugSource(new[] { routineSite });
    Equal(true, routineDebugSource.Contains("long long Value", StringComparison.Ordinal));
    Equal(true, routineDebugSource.Contains("long long LocalValue", StringComparison.Ordinal));
});
Run("Web target failures retain the support source path", () =>
{
    var analysis = Multi(
        ("Program.smile", true, "Print HugeValue()\n"),
        ("Support.smile", false, "Function HugeValue()\nReturn 9007199254740992\nEnd Function\n"));
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
        ("Program.smile", true, "Score = 1\nCall AddOne()\nPrint Score\n"),
        ("Support.smile", false, "Sub AddOne()\nScore = Score + 1\nEnd Sub\n"));
    Equal(false, analysis.HasErrors);
    var javascript = new WebEmitter(analysis).Emit();
    Equal(true, javascript.Contains("async function r_0_addone"));
    Equal(true, javascript.Contains("await r_0_addone()"));
    Equal(1, javascript.Split(new[] { "smile.print" }, StringSplitOptions.None).Length - 1);
});
Run("Local modules import public members through a qualified alias", () =>
{
    var analysis = Multi(
        ("Program.smile", true, "Import Example.Math As Math\nPrint Math.Double(21)\nEnd Program\n"),
        ("Math.smile", false, "Module Example.Math\nPublic Function Double(Value)\nReturn Value * 2\nEnd Function\nPrivate Const Secret = 9\nEnd Module\n"));
    Equal(false, analysis.HasErrors);
    Equal(true, analysis.SemanticModel.Modules.ContainsKey("Example.Math"));
    Equal(true, new WebEmitter(analysis).Emit().Contains("await r_"));
    Equal(true, new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, false).Emit().Contains("call smile_"));
});
Run("Private module members are rejected across import boundaries", () =>
{
    var analysis = Multi(
        ("Program.smile", true, "Import Example.Math As Math\nPrint Math.Secret\n"),
        ("Math.smile", false, "Module Example.Math\nPrivate Const Secret = 9\nEnd Module\n"));
    Equal(true, HasDiagnostic(analysis, "SML3105"));
});
Run("Missing modules aliases members and import cycles have stable diagnostics", () =>
{
    Equal(true, HasDiagnostic(Multi(("Program.smile", true, "Import Missing.Module As Missing\n")), "SML3102"));
    Equal(true, HasDiagnostic(Multi(
        ("Program.smile", true, "Import Example.Math As Math\nPrint Math.Unknown\n"),
        ("Math.smile", false, "Module Example.Math\nPublic Const Value = 1\nEnd Module\n")), "SML3103"));
    Equal(true, HasDiagnostic(Multi(
        ("Program.smile", true, "Import Example.Alpha As Alpha\n"),
        ("A.smile", false, "Module Example.Alpha\nImport Example.Beta As Beta\nPublic Const AValue = 1\nEnd Module\n"),
        ("B.smile", false, "Module Example.Beta\nImport Example.Alpha As Alpha\nPublic Const BValue = 1\nEnd Module\n")), "SML3108"));
});
Run("Alias dot completion exposes only public module members", () =>
{
    var text = "Import Example.Math As Math\nPrint Math.";
    var analysis = Multi(
        ("Program.smile", true, text),
        ("Math.smile", false, "Module Example.Math\nPublic Function Double(Value)\nReturn Value * 2\nEnd Function\nPrivate Const Secret = 9\nEnd Module\n"));
    var completions = SmileCompletionService.GetCompletions(analysis, text.Length);
    Equal(true, completions.Any(item => item.DisplayText == "Double"));
    Equal(false, completions.Any(item => item.DisplayText == "Secret"));
});
Run("Educational documentation comments parse safely and case-insensitively", () =>
{
    const string source = "''' First summary line.\n''' Second summary line.\n''' @PaRaM Value: Primary explanation.\n''' continuation text.\n''' @PARAM value: Ignored duplicate.\n''' @param Unknown: Tolerated metadata.\n''' @ReTuRnS: The resulting value.\n''' @Remarks: First remark.\n''' Additional remark.\n''' @unknown malformed metadata\nFunction Echo(Value As Number) As Number\nReturn Value\nEnd Function\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var routine = analysis.SemanticModel.Routines["Echo"];
    var documentation = SmileDocumentationService.GetDocumentation(routine.Source,
        routine.Declaration.Keyword.Span.Start);
    Equal("First summary line. Second summary line.", documentation.Summary);
    Equal("Primary explanation. continuation text.", documentation.Parameters["VALUE"]);
    Equal("Tolerated metadata.", documentation.Parameters["unknown"]);
    Equal("The resulting value.", documentation.Returns);
    Equal("First remark. Additional remark.", documentation.Remarks);
});
Run("Ordinary comments blank gaps malformed tags and missing documentation stay inert", () =>
{
    foreach (var source in new[]
             {
                 "' Ordinary comment\nFunction Plain() As Number\nReturn 1\nEnd Function\n",
                 "''' Detached summary\n\nFunction Plain() As Number\nReturn 1\nEnd Function\n",
                 "''' @param MissingColon\n''' @returns MissingColon\nFunction Plain() As Number\nReturn 1\nEnd Function\n",
                 "Function Plain() As Number\nReturn 1\nEnd Function\n"
             })
    {
        var analysis = Analyze(source);
        Equal(false, analysis.HasErrors);
        var routine = analysis.SemanticModel.Routines["Plain"];
        var documentation = SmileDocumentationService.GetDocumentation(routine.Source,
            routine.Declaration.Keyword.Span.Start);
        Equal(string.Empty, documentation.Summary);
        Equal(0, documentation.Parameters.Count);
        Equal(string.Empty, documentation.Returns);
    }
});
Run("Every public Smile.UI.Menu routine has complete educational documentation", () =>
{
    var compilation = SmileProjectCompilation.Load("libraries/Smile.UI/Smile.UI.smilelibproj");
    var analysis = SmileLanguage.Analyze(compilation.Sources, SmileCompilationKind.Library,
        compilation.DependencyContext);
    Equal(false, analysis.HasErrors);
    var routines = analysis.SemanticModel.Modules["Smile.UI.Menu"].PublicMembers
        .Where(member => member.Routine != null).Select(member => member.Routine!).ToArray();
    Equal(26, routines.Length);
    foreach (var routine in routines)
    {
        var documentation = SmileDocumentationService.GetDocumentation(routine.Source,
            routine.Declaration.Keyword.Span.Start);
        Equal(false, string.IsNullOrWhiteSpace(documentation.Summary));
        foreach (var parameter in routine.Parameters)
            Equal(true, documentation.Parameters.ContainsKey(parameter.Name));
        if (routine.IsFunction)
            Equal(false, string.IsNullOrWhiteSpace(documentation.Returns));
    }
});
Run("Imported aliases and qualified members resolve to exact declarations and documentation", () =>
{
    const string program = "Import Example.Menu As Menu\nPrint menu.create(7)\n";
    const string module = "''' Menu module summary.\nModule Example.Menu\n''' Creates a value.\n''' @param Value: Number to return.\n''' @returns: The supplied number.\nPublic Function Create(Value As Number) As Number\nReturn Value\nEnd Function\nPrivate Function Secret() As Number\nReturn 1\nEnd Function\nEnd Module\n";
    var analysis = Multi(("Program.smile", true, program), ("Menu.smile", false, module));
    Equal(false, analysis.HasErrors);
    var tree = analysis.GetSyntaxTree("Program.smile");
    var aliasPosition = program.IndexOf("menu.create", StringComparison.Ordinal);
    Equal(true, SmileSymbolService.TryResolve(analysis, tree, aliasPosition + 4, out var alias));
    Equal(SmileResolvedSymbolKind.Module, alias.Kind);
    Equal("Example.Menu", alias.Name);
    Equal("Menu module summary.", alias.Documentation.Summary);
    Equal("Example.Menu", alias.DeclarationLocation!.Source.Substring(alias.DeclarationLocation.Span.Start,
        alias.DeclarationLocation.Span.Length));

    var memberPosition = program.IndexOf("create", StringComparison.Ordinal);
    Equal(true, SmileSymbolService.TryResolve(analysis, tree, memberPosition, out var member));
    Equal(SmileResolvedSymbolKind.Function, member.Kind);
    Equal("Function Example.Menu.Create(Value As Number) As Number", member.Signature);
    Equal("Create", member.DeclarationLocation!.Source.Substring(member.DeclarationLocation.Span.Start,
        member.DeclarationLocation.Span.Length));
    Equal("Number to return.", member.Documentation.Parameters["value"]);
    Equal("The supplied number.", member.Documentation.Returns);

    const string privateUse = "Import Example.Menu As Menu\nPrint Menu.Secret()\n";
    var privateAnalysis = Multi(("Private.smile", true, privateUse), ("Menu.smile", false, module));
    Equal(false, SmileSymbolService.TryResolve(privateAnalysis, privateAnalysis.GetSyntaxTree("Private.smile"),
        privateUse.IndexOf("Secret", StringComparison.Ordinal), out _));
});
Run("Symbol resolution handles locals parameters types fields boundaries and invalid positions", () =>
{
    const string source = "Option Explicit\nType Player\nName As Text\nEnd Type\nDim Hero As Player\nCall Work(Hero)\nSub Work(Value As Player)\nDim Local As Number\nPrint Value.Name\nPrint Local\nEnd Sub\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var tree = analysis.SyntaxTree;

    var workUse = source.IndexOf("Work(Hero)", StringComparison.Ordinal);
    Equal(SmileResolvedSymbolKind.Subroutine,
        ResolveSymbol(analysis, tree, workUse + "Work".Length).Kind);
    var valueUse = source.IndexOf("Value.Name", StringComparison.Ordinal);
    Equal(SmileResolvedSymbolKind.Parameter, ResolveSymbol(analysis, tree, valueUse).Kind);
    var fieldUse = source.IndexOf("Name", valueUse, StringComparison.Ordinal);
    var field = ResolveSymbol(analysis, tree, fieldUse);
    Equal(SmileResolvedSymbolKind.Field, field.Kind);
    Equal("Name", field.DeclarationLocation!.Source.Substring(field.DeclarationLocation.Span.Start,
        field.DeclarationLocation.Span.Length));
    var localUse = source.LastIndexOf("Local", StringComparison.Ordinal);
    Equal(SmileResolvedSymbolKind.Local, ResolveSymbol(analysis, tree, localUse).Kind);
    var typeUse = source.IndexOf("Player", source.IndexOf("Hero", StringComparison.Ordinal), StringComparison.Ordinal);
    Equal(SmileResolvedSymbolKind.Type, ResolveSymbol(analysis, tree, typeUse).Kind);
    Equal(false, SmileSymbolService.TryResolve(analysis, tree,
        source.IndexOf("Option", StringComparison.Ordinal), out _));
    Equal(false, SmileSymbolService.TryResolve(analysis, tree,
        source.IndexOf("\n", StringComparison.Ordinal), out _));
});
Run("Symbol resolution ignores comments strings and unresolved names without throwing", () =>
{
    const string source = "' MissingName in a comment\nPrint \"MissingName in text\"\nPrint MissingName\n";
    var analysis = Analyze(source);
    var tree = analysis.SyntaxTree;
    Equal(false, SmileSymbolService.TryResolve(analysis, tree,
        source.IndexOf("MissingName", StringComparison.Ordinal), out _));
    Equal(false, SmileSymbolService.TryResolve(analysis, tree,
        source.IndexOf("MissingName in text", StringComparison.Ordinal), out _));
    Equal(false, SmileSymbolService.TryResolve(analysis, tree,
        source.LastIndexOf("MissingName", StringComparison.Ordinal), out _));
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
        File.WriteAllText(Path.Combine(directory, "Program.smile"), "End Program\n");
        File.WriteAllText(package, "fixture");
        File.WriteAllText(project, "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" /></ItemGroup></SmileProject>");
        var added = SmileProjectFileEditor.AddReference(project, package);
        Equal(1, added.References.Count);
        Equal(1, SmileProjectHierarchyProjection.Create(added, "Console")
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
        File.WriteAllText(sourcePath, "Module Example.Tools\nPublic Function Double(Value)\nReturn Value * 2\nEnd Function\nPrivate Const Hidden = 1\nEnd Module\n");
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
        File.WriteAllText(sourcePath, "Module Example.Tools\nPublic Function Triple(Value)\nReturn Value * 3\nEnd Function\nEnd Module\n");
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
            "Module Example.Base\nPublic Function Double(Value)\nReturn Value * 2\nEnd Function\nEnd Module\n");
        File.WriteAllText(baseProjectPath,
            "<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Example.Base</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include=\"Base.smile\" /></ItemGroup></SmileProject>");

        var dependentProjectPath = Path.Combine(dependentDirectory, "Dependent.smilelibproj");
        File.WriteAllText(Path.Combine(dependentDirectory, "Dependent.smile"),
            "Module Example.Dependent\nImport Example.Base As Base\nPublic Function Quadruple(Value)\nReturn Base.Double(Base.Double(Value))\nEnd Function\nPrivate Const Hidden = 9\nEnd Module\n");
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
            "Import Example.Dependent As Dependent\nPrint Dependent.Quadruple(3)\nEnd Program\n");
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
            ? new SmileSourceDocument("Import Example.Dependent As Dependent\nPrint Dependent.",
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
            "Import Example.Base As Base\nPrint Base.Double(2)\nEnd Program\n",
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
            "Module Example.Base\nPublic Function Double(Value)\nReturn Value * 2\nEnd Function\nEnd Module\n");
        File.WriteAllText(baseProject,
            "<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Example.Base</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include=\"Base.smile\" /></ItemGroup></SmileProject>");
        File.WriteAllText(dependentSource,
            "Module Example.Dependent\nImport Example.Base As Base\nPublic Function Quadruple(Value)\nReturn Base.Double(Base.Double(Value))\nEnd Function\nEnd Module\n");
        var dependentWithoutReference =
            "<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Example.Dependent</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include=\"Dependent.smile\" /></ItemGroup></SmileProject>";
        var dependentWithReference = dependentWithoutReference.Replace("</ItemGroup>",
            "<SmileProjectReference Include=\"..\\Base\\Base.smilelibproj\" /></ItemGroup>", StringComparison.Ordinal);
        File.WriteAllText(dependentProject, dependentWithoutReference);
        File.WriteAllText(programSource,
            "Import Example.Dependent As Dependent\nPrint Dependent.Quadruple(3)\nEnd Program\n");
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

        File.WriteAllText(programSource, "Import Example.Base As Base\nEnd Program\n");
        var transitiveCompilation = SmileProjectCompilation.Load(appProject);
        var transitiveAnalysis = SmileLanguage.Analyze(transitiveCompilation.Sources, SmileCompilationKind.Program,
            transitiveCompilation.DependencyContext);
        var transitiveDiagnostic = transitiveAnalysis.Diagnostics.Single(diagnostic => diagnostic.Code == "SML3208");
        Equal(programSource, transitiveDiagnostic.FilePath);
        Equal(1, transitiveDiagnostic.Line);
        Equal(8, transitiveDiagnostic.Column);

        File.WriteAllText(programSource, "Import ");
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
        File.WriteAllText(sourcePath, "Module Example.Tools\nPublic Const Value = 1\nEnd Module\n");
        File.WriteAllText(projectPath, projectXml);
        var compilation = SmileProjectCompilation.Load(projectPath);
        var analysis = SmileLanguage.Analyze(compilation.Sources, SmileCompilationKind.Library,
            compilation.DependencyContext);
        SmileLibraryPackage.Write(outputPath, compilation.Graph.Root, analysis);
        Equal(false, CompilerDriver.NeedsLibraryBuild(compilation.Graph.Root, outputPath, analysis));

        File.WriteAllText(sourcePath, "Module Example.Tools\nPublic Const Value = 2\nEnd Module\n");
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
        File.WriteAllText(foreignSourcePath, "Module Example.Foreign\nPublic Const Value = 9\nEnd Module\n");
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
        File.WriteAllText(Path.Combine(directory, "Program.smile"), "End Program\n");
        File.WriteAllText(Path.Combine(directory, "Middle.smile"), "Module Middle\nEnd Module\n");
        File.WriteAllText(rootProject,
            "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" /><SmileProjectReference Include=\"Middle.smilelibproj\" /></ItemGroup></SmileProject>");
        File.WriteAllText(middleProject,
            "<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Middle</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include=\"Middle.smile\" /><SmileProjectReference Include=\"Leaf.smilelibproj\" /></ItemGroup></SmileProject>");
        var missingLeaf = SmileProjectParticipationDiscovery.Discover(rootProject);
        Equal("SML3200", missingLeaf.Diagnostic!.Code);
        Equal(true, missingLeaf.Paths.Contains(leafProject, StringComparer.OrdinalIgnoreCase));

        File.WriteAllText(Path.Combine(directory, "Leaf.smile"), "Module Leaf\nEnd Module\n");
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
        File.WriteAllText(sourcePath, "End Program\n");
        File.WriteAllText(projectPath,
            "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" /><SmileProjectReference Include=\"Missing.smilelibproj\" /></ItemGroup></SmileProject>");
        var diagnostic = SmileProjectCompilation.TryLoad(projectPath).Diagnostic!;
        Equal($"{missingPath}(1,1): error SML3200: {diagnostic.Message}", diagnostic.FormatCompiler());
        var safe = SmileLanguage.AnalyzeWithProjectDiagnostic(new[]
        {
            new SmileSourceDocument("End Program\n", sourcePath, true)
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
        File.WriteAllText(Path.Combine(directory, "A.smile"), "Module A\nEnd Module\n");
        File.WriteAllText(Path.Combine(directory, "B.smile"), "Module B\nEnd Module\n");
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
        ("Program.smile", true, "Import Example.Values As Values\nPrint Values.Hidden\n"),
        ("Values.smile", false, "Module Example.Values\nConst Hidden = 1\nEnd Module\n"));
    Equal(true, HasDiagnostic(privateAnalysis, "SML3105"));
    var captureAnalysis = Multi(
        ("Program.smile", true, "Import Example.Values As Values\nScore = 10\nPrint Values.ReadScore()\n"),
        ("Values.smile", false, "Module Example.Values\nPublic Function ReadScore()\nReturn Score\nEnd Function\nEnd Module\n"));
    Equal(true, HasDiagnostic(captureAnalysis, "SML3110"));
});
Run("Duplicate module providers are rejected independently of source names", () =>
{
    var analysis = SmileLanguage.Analyze(new[]
    {
        new SmileSourceDocument("Import Shared.Tools As Tools\n", "Program.smile", true),
        new SmileSourceDocument("Module Shared.Tools\nPublic Const First = 1\nEnd Module\n", "First.smile", providerIdentity: "First.smilelib"),
        new SmileSourceDocument("Module Shared.Tools\nPublic Const Second = 2\nEnd Module\n", "Second.smile", providerIdentity: "Second.smilelib")
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
            writer.Write("Module Escape\nEnd Module\n");
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
Run("Official SMILE libraries are labeled separately from student libraries", () =>
{
    var context = SmileCompilationDependencyContext.Create();
    context.AddProvider("official", SmileProviderKind.Package, "Smile.UI", "1.1.3", "Smile.UI.smilelib");
    context.AddProvider("student", SmileProviderKind.Project, "Student.Tools", "1.0.0",
        "Student.Tools.smilelibproj");
    Equal(true, context.TryGetProviderDescriptor("official", out var official));
    Equal(true, official.IsBuiltIn);
    Equal(true, official.Describe().Contains("SMILE 2.0 built-in library", StringComparison.Ordinal));
    Equal("SMILE 2.0 built-in library Smile.UI@1.1.3",
        SmileSymbolDisplayService.DescribeProvider("official", context));
    Equal(true, context.TryGetProviderDescriptor("student", out var student));
    Equal(false, student.IsBuiltIn);
    Equal("Student.Tools@1.0.0", SmileSymbolDisplayService.DescribeProvider("student", context));
});
Run("Identical member names in different modules receive distinct emitter identities", () =>
{
    var analysis = Multi(
        ("Program.smile", true, "Import Example.Alpha As Alpha\nImport Example.Beta As Beta\nPrint Alpha.Value()\nPrint Beta.Value()\n"),
        ("Alpha.smile", false, "Module Example.Alpha\nPublic Function Value()\nReturn 1\nEnd Function\nEnd Module\n"),
        ("Beta.smile", false, "Module Example.Beta\nPublic Function Value()\nReturn 2\nEnd Function\nEnd Module\n"));
    Equal(false, analysis.HasErrors);
    var assembly = new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, false).Emit();
    Equal(2, analysis.SemanticModel.Routines.Values.Count(routine => routine.Name == "Value"));
    Equal(true, assembly.Contains("call smile_", StringComparison.Ordinal));
    Equal(true, new WebEmitter(analysis).Emit().Split(new[] { "async function r_" }, StringSplitOptions.None).Length >= 3);
});

Run("Phase 3A keywords are shared and case-insensitive", () =>
{
    Equal(SyntaxKind.OptionKeyword, SyntaxFacts.GetKeywordKind("option"));
    Equal(SyntaxKind.ExplicitKeyword, SyntaxFacts.GetKeywordKind("Explicit"));
    Equal(SyntaxKind.BooleanKeyword, SyntaxFacts.GetKeywordKind("Boolean"));
    Equal(SyntaxKind.ByRefKeyword, SyntaxFacts.GetKeywordKind("byref"));
    Equal(SyntaxKind.ByValKeyword, SyntaxFacts.GetKeywordKind("ByVal"));
});
Run("Option Explicit is physical-source scoped and enforces declarations", () =>
{
    Equal(false, Analyze("Option Explicit\nDim Value As Number\nValue = 1\n").HasErrors);
    Equal(true, HasDiagnostic(Analyze("Option Explicit\nValue = 1\n"), "SML3303"));
    Equal(true, HasDiagnostic(Analyze("Value = 1\nOption Explicit\n"), "SML3300"));
    Equal(true, HasDiagnostic(Analyze("Option Explicit\nOption Explicit\n"), "SML3300"));
    var scoped = Multi(
        ("Program.smile", true, "Option Explicit\nDim Value As Number\nValue = 1\n"),
        ("Support.smile", false, "Sub Legacy()\nImplicit = 2\nEnd Sub\n"));
    Equal(false, scoped.HasErrors);
});
Run("Multiline parenthesized If expressions accept both operator positions and nested groups", () =>
{
    const string declarations = "Option Explicit\nDim First As Number\nDim Second As Number\nDim Third As Number\nDim Fourth As Number\n";
    var sources = new[]
    {
        declarations + "If First < Second Or Third < Fourth Then\nPrint First\nEnd If\n",
        declarations + "If (First < Second Or\n    Third < Fourth) Then\nPrint First\nEnd If\n",
        declarations + "If (First < Second\n    Or Third < Fourth) Then\nPrint First\nEnd If\n",
        declarations + "If (\n    First < Second Or\n    Third < Fourth\n) Then\nPrint First\nEnd If\n",
        declarations + "If ((First + Second) * Third > Fourth And\n    Not (First = Second Or\n        Third = Fourth)) Then\nPrint First\nEnd If\n",
        declarations + "If (First < Second) Then\nPrint First\nElse If (Third < Fourth Or\n    First = Second) Then\nPrint Third\nEnd If\n"
    };

    foreach (var source in sources)
        Equal(false, Analyze(source).HasErrors);
});
Run("Multiline parenthesized assignment preserves expression shape and precedence", () =>
{
    const string source = "Option Explicit\nDim First As Number\nDim Second As Number\nDim Third As Number\nDim Result As Number\nResult = (First +\n    Second *\n    Third)\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var assignment = analysis.BoundSyntaxTree.Root.Statements.OfType<AssignmentStatementSyntax>().Single();
    var parenthesized = (ParenthesizedExpressionSyntax)assignment.Expression;
    var addition = (BinaryExpressionSyntax)parenthesized.Expression;
    Equal(SyntaxKind.PlusToken, addition.OperatorToken.Kind);
    Equal(SyntaxKind.StarToken, ((BinaryExpressionSyntax)addition.Right).OperatorToken.Kind);
    Equal(SmileType.Number, analysis.SemanticModel.GetType(parenthesized));
});
Run("Multiline normal qualified and Call argument lists analyze", () =>
{
    const string program = "Option Explicit\nImport Example.Helpers As Helpers\nDim Value As Number\nValue = Add(\n    1,\n    2\n)\nValue = Helpers.Add(\n    Value\n    ,\n    3\n)\nCall PresentValue(\n    Value,\n    True\n)\nCall Helpers.PresentValue(\n    Value,\n    False\n)\nSub PresentValue(Value As Number, Flag As Boolean)\nPrint Value\nEnd Sub\nFunction Add(Left As Number, Right As Number) As Number\nDim ReturnValue As Number\nReturnValue = Left + Right\nReturn ReturnValue\nEnd Function\n";
    const string module = "Module Example.Helpers\nPublic Function Add(Left As Number, Right As Number) As Number\nDim ReturnValue As Number\nReturnValue = Left + Right\nReturn ReturnValue\nEnd Function\nPublic Sub PresentValue(Value As Number, Flag As Boolean)\nPrint Value\nEnd Sub\nEnd Module\n";
    var analysis = Multi(("Program.smile", true, program), ("Helpers.smile", false, module));
    if (analysis.HasErrors)
        throw new InvalidOperationException(string.Join(" | ", analysis.Diagnostics.Select(diagnostic =>
            diagnostic.Code + ": " + diagnostic.Message)));
    Equal(false, analysis.HasErrors);
    Equal(2, analysis.GetSyntaxTree("Program.smile").Root.Statements.OfType<AssignmentStatementSyntax>().Count());
    Equal(2, analysis.GetSyntaxTree("Program.smile").Root.Statements
        .Count(statement => statement is CallStatementSyntax or QualifiedCallStatementSyntax));
});
Run("Multiline parenthesized expressions accept comments blank lines LF and CRLF", () =>
{
    const string source = "Option Explicit\nDim First As Number\nDim Second As Number\nIf (First < Second Or ' Continue the condition.\n\n    Second = 0) Then\nPrint First\nEnd If\nIf (First < Second ' Continue before the operator.\n    Or Second = 0) Then\nPrint Second\nEnd If\n";
    Equal(false, Analyze(source).HasErrors);
    Equal(false, Analyze(source.Replace("\n", "\r\n", StringComparison.Ordinal)).HasErrors);
});
Run("Missing multiline closing parenthesis reports one source-located expected-token diagnostic", () =>
{
    const string source = "Dim Value As Number\nIf (Value < 1 Then\nPrint Value\nEnd If\n";
    var diagnostics = Analyze(source).Diagnostics.Where(diagnostic => diagnostic.Code == "SML2001").ToArray();
    Equal(1, diagnostics.Length);
    Equal(2, diagnostics[0].Line);
    Equal(15, diagnostics[0].Column);
    Equal("Expected ), found 'Then'.", diagnostics[0].Message);
});
Run("Multiline parenthesized expressions preserve native and Web emitter parity", () =>
{
    const string source = "Option Explicit\nDim First As Number\nDim Second As Number\nDim Result As Boolean\nResult = (First < Second Or\n    First = Second)\nPrint Result\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    Equal(true, new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, false).Emit()
        .Contains("smile_print_number", StringComparison.Ordinal));
    Equal(true, new WebEmitter(analysis).Emit().Contains("smile.print", StringComparison.Ordinal));
});
Run("Newlines remain significant outside parenthesized expression contexts", () =>
{
    var invalidSources = new[]
    {
        "Dim Value As Number\nIf Value < 1 Or\n    Value > 2 Then\nPrint Value\nEnd If\n",
        "Dim Value As Number\nValue = 1 +\n    2\n",
        "Dim Value As Number\nIf (Value < 1 Then\nPrint Value\nEnd If\n",
        "Dim Value As Number\nIf (Value < 1 Or\n) Then\nPrint Value\nEnd If\n",
        "Dim Value As Number\nIf (Value < 1\n    Value > 2) Then\nPrint Value\nEnd If\n",
        "Dim Value As Number\nIf (Value < 1)\nThen\nPrint Value\nEnd If\n",
        "Dim Value As Number\nValue = (1 +\n",
        "Dim Values[\n2]\n",
        "Sub Work(\nValue As Number)\nEnd Sub\n"
    };

    foreach (var source in invalidSources)
        Equal(true, HasDiagnostic(Analyze(source), "SML2001"));
});
Run("Completion Quick Info and definition work on continuation lines", () =>
{
    const string completionSource = "Option Explicit\nDim LongValue As Number\nIf (LongValue > 0 Or\n    LongVal";
    var completionAnalysis = Analyze(completionSource);
    Equal(true, SmileCompletionService.GetCompletions(completionAnalysis, completionSource.Length)
        .Any(item => item.DisplayText == "LongValue"));

    const string source = "Option Explicit\nDim LongValue As Number\nIf (LongValue > 0 Or\n    LongValue < 10) Then\nPrint LongValue\nEnd If\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var continuationPosition = source.IndexOf("LongValue < 10", StringComparison.Ordinal);
    var symbol = ResolveSymbol(analysis, analysis.SyntaxTree, continuationPosition);
    Equal(SmileResolvedSymbolKind.Variable, symbol.Kind);
    Equal("Dim LongValue As Number", symbol.Signature);
    Equal("LongValue", symbol.DeclarationLocation!.Source.Substring(symbol.DeclarationLocation.Span.Start,
        symbol.DeclarationLocation.Span.Length));
    Equal("Dim LongValue As Number",
        SmileSymbolDisplayService.Present(symbol, analysis.DependencyContext).Signature);
});
Run("Typed scalars arrays and legacy numeric arrays bind shared types", () =>
{
    var analysis = Analyze("Option Explicit\nDim Score As Number\nDim Alive As Boolean\nDim Name As Text\nDim Flags[2] As Boolean\nDim Names[3] As Text\nDim Legacy[4]\n");
    Equal(false, analysis.HasErrors);
    Equal(SmileType.Number, analysis.SemanticModel.Symbols["Score"].Type);
    Equal(SmileType.Boolean, analysis.SemanticModel.Symbols["Alive"].Type);
    Equal(SmileType.Text, analysis.SemanticModel.Symbols["Name"].Type);
    Equal(SmileType.Boolean, analysis.SemanticModel.Symbols["Flags"].Type);
    Equal(SmileType.Text, analysis.SemanticModel.Symbols["Names"].Type);
    Equal(SmileType.Number, analysis.SemanticModel.Symbols["Legacy"].Type);
    Equal(true, HasDiagnostic(Analyze("Dim MissingType\n"), "SML3302"));
    Equal(true, HasDiagnostic(Analyze("Dim Value As STRING\n"), "SML3401"));
});
Run("Text constants values operators arrays Select and Draw bind", () =>
{
    const string source = "Option Explicit\nConst Greeting = \"Hello, \" + \"SMILE\"\nDim Name As Text\nDim Copy As Text\nDim Names[2] As Text\nDim Same As Boolean\nName = Greeting\nCopy = Name\nNames[0] = Copy\nSame = Name = Copy\nSelect Case Name\nCase \"Hello, SMILE\"\nPrint Names[0]\nCase Else\nPrint \"NO\"\nEnd Select\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    Equal("Hello, SMILE", analysis.SemanticModel.Symbols["Greeting"].ConstantValue);
    Equal(SmileType.Text, analysis.SemanticModel.Symbols["Names"].Type);
    Equal(true, HasDiagnostic(Analyze("Dim TextValue As Text\nTextValue = \"x\" + 1\n"), "SML3308"));
    Equal(true, HasDiagnostic(Analyze("Dim A As Text\nDim B As Text\nPrint A < B\n"), "SML3308"));
    Equal(false, Analyze("Game Window \"Text\"\nDim Caption As Text\nCaption = \"Ready\"\nDraw Text Caption At 10, 20 Size 16 Color WHITE\n").HasErrors);
});
Run("Typed routines default ByVal and validate ByRef writable locations", () =>
{
    const string source = "Option Explicit\nDim Name As Text\nName = \"Before\"\nCall Rename(Name, \"After\")\nSub Rename(ByRef Value As Text, ByVal Replacement As Text)\nValue = Replacement\nEnd Sub\nFunction IsEmpty(Value As Text) As Boolean\nReturn Value = \"\"\nEnd Function\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var rename = analysis.SemanticModel.Routines.Values.Single(routine => routine.Name == "Rename");
    Equal(ParameterPassingMode.ByRef, rename.Parameters[0].ParameterMode);
    Equal(ParameterPassingMode.ByVal, rename.Parameters[1].ParameterMode);
    Equal(SmileType.Text, rename.Parameters[0].Type);
    Equal(SmileType.Boolean, analysis.SemanticModel.Routines.Values.Single(routine => routine.Name == "IsEmpty").ReturnType);
    Equal(true, HasDiagnostic(Analyze("Sub Set(ByRef Value As Number)\nValue = 1\nEnd Sub\nCall Set(5)\n"), "SML3305"));
    Equal(true, HasDiagnostic(Analyze("Const Fixed = 1\nSub Set(ByRef Value As Number)\nValue = 1\nEnd Sub\nCall Set(Fixed)\n"), "SML3305"));
});
Run("Legacy numeric parameters accept Boolean values compatibly", () =>
{
    const string source = "Print Legacy(True)\nFunction Legacy(Value)\nReturn Value = 1\nEnd Function\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    Equal(false, analysis.SemanticModel.Routines.Values.Single().Parameters[0].HasDeclaredType);
    Equal(true, new WebEmitter(analysis).Emit().Contains("? 1 : 0", StringComparison.Ordinal));
    Equal(true, HasDiagnostic(Analyze("Print Typed(True)\nFunction Typed(Value As Number) As Boolean\nReturn Value = 1\nEnd Function\n"), "SML3304"));
});
Run("Routine calls support sixteen typed parameters", () =>
{
    const string parameters = "Value1 As Number, Value2 As Number, Value3 As Number, Value4 As Number, Value5 As Number, Value6 As Number, Value7 As Number, Value8 As Number, Value9 As Number, Value10 As Number, Value11 As Number, Value12 As Number, Value13 As Number, Value14 As Number, Value15 As Number, Value16 As Number";
    var analysis = Analyze($"Print Sum16(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16)\nFunction Sum16({parameters}) As Number\nReturn Value1 + Value2 + Value3 + Value4 + Value5 + Value6 + Value7 + Value8 + Value9 + Value10 + Value11 + Value12 + Value13 + Value14 + Value15 + Value16\nEnd Function\n");
    Equal(false, analysis.HasErrors);
    Equal(16, analysis.SemanticModel.Routines.Values.Single().Parameters.Count);
    Equal(true, new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, false).Emit().Contains("[rbp+136]", StringComparison.Ordinal));
});
Run("Routine-local Dim shadows globals and diagnoses duplicate and early use", () =>
{
    var shadow = Analyze("Dim Value As Number\nSub Work()\nDim Value As Text\nValue = \"local\"\nPrint Value\nEnd Sub\nValue = 1\nCall Work()\n");
    Equal(false, shadow.HasErrors);
    Equal(SmileType.Text, shadow.SemanticModel.Routines.Values.Single().LocalSymbols["Value"].Type);
    Equal(true, HasDiagnostic(Analyze("Sub Work()\nDim Value As Number\nDim Value As Text\nEnd Sub\n"), "SML3306"));
    Equal(true, HasDiagnostic(Analyze("Sub Work()\nPrint Value\nDim Value As Number\nEnd Sub\n"), "SML3307"));
});
Run("Legacy function inference checks all return types", () =>
{
    Equal(true, HasDiagnostic(Analyze("Print Mixed(True)\nFunction Mixed(Flag As Boolean)\nIf Flag Then\nReturn \"text\"\nElse\nReturn 1\nEnd If\nEnd Function\n"), "SML3309"));
});
Run("Web emitter uses JavaScript Text values and ByRef references", () =>
{
    var analysis = Analyze("Dim Name As Text\nName = \"A\"\nCall Replace(Name, \"B\")\nPrint Name\nSub Replace(ByRef Value As Text, NewValue As Text)\nValue = NewValue\nEnd Sub\n");
    Equal(false, analysis.HasErrors);
    var javascript = new WebEmitter(analysis).Emit();
    Equal(true, javascript.Contains("smile.ref(() =>", StringComparison.Ordinal));
    Equal(true, javascript.Contains(".set(", StringComparison.Ordinal));
    Equal(true, javascript.Contains("\"A\"", StringComparison.Ordinal));
});
Run("FormatVersion 5 packages contain deterministic typed public API metadata", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmilePhase3APackageTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var projectPath = Path.GetFullPath("libraries/Smile.Text.Extras/Smile.Text.Extras.smilelibproj");
        var compilation = SmileProjectCompilation.Load(projectPath, Path.Combine(directory, "cache"));
        var analysis = SmileLanguage.Analyze(compilation.Sources, SmileCompilationKind.Library,
            compilation.DependencyContext);
        Equal(false, analysis.HasErrors);
        var first = Path.Combine(directory, "first.smilelib");
        var second = Path.Combine(directory, "second.smilelib");
        SmileLibraryPackage.Write(first, compilation.Graph.Root, analysis);
        SmileLibraryPackage.Write(second, compilation.Graph.Root, analysis);
        Equal(true, File.ReadAllBytes(first).SequenceEqual(File.ReadAllBytes(second)));
        using (var archive = System.IO.Compression.ZipFile.OpenRead(first))
        {
            using var manifestReader = new StreamReader(archive.GetEntry("manifest.json")!.Open());
            Equal(true, manifestReader.ReadToEnd().Contains("\"formatVersion\": 5", StringComparison.Ordinal));
            using var apiReader = new StreamReader(archive.GetEntry("api/public-symbols.json")!.Open());
            var api = apiReader.ReadToEnd();
            Equal(true, api.Contains("\"type\": \"Text\"", StringComparison.Ordinal));
            Equal(true, api.Contains("\"mode\": \"ByRef\"", StringComparison.Ordinal));
            Equal(true, api.Contains("\"mode\": \"ByVal\"", StringComparison.Ordinal));
            Equal(true, api.Contains("\"returnType\": \"Text\"", StringComparison.Ordinal));
            Equal(false, api.Contains("Hidden", StringComparison.Ordinal));
        }
        RewriteManifest(first, manifest => manifest.Replace("\"formatVersion\": 5", "\"formatVersion\": 4",
            StringComparison.Ordinal));
        ThrowsContains(() => SmileLibraryPackage.ReadIdentity(first), "rebuild the library");
    }
    finally { Directory.Delete(directory, true); }
});
Run("FormatVersion 5 packages preserve direct and transitive Game Window capabilities", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmilePhase5CapabilityPackageTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var projectPath = Path.Combine(directory, "Capability.smilelibproj");
        File.WriteAllText(projectPath,
            "<SmileProject Version=\"1.0\"><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Capability.Proof</LibraryName><Version>1.0.0</Version><OutputName>Capability</OutputName></PropertyGroup><ItemGroup><SmileSource Include=\"Capability.smile\" /></ItemGroup></SmileProject>");
        File.WriteAllText(Path.Combine(directory, "Capability.smile"),
            "Module Capability.Proof\nPublic Sub Draw()\nFill Rectangle 0, 0, 1, 1, WHITE\nEnd Sub\nPublic Sub Wrapper()\nCall Draw()\nEnd Sub\nPublic Sub Pure()\nEnd Sub\nEnd Module\n");
        var compilation = SmileProjectCompilation.Load(projectPath, Path.Combine(directory, "cache"));
        var analysis = SmileLanguage.Analyze(compilation.Sources, SmileCompilationKind.Library,
            compilation.DependencyContext);
        Equal(false, analysis.HasErrors);
        var package = Path.Combine(directory, "Capability.smilelib");
        SmileLibraryPackage.Write(package, compilation.Graph.Root, analysis);
        using var archive = System.IO.Compression.ZipFile.OpenRead(package);
        using var reader = new StreamReader(archive.GetEntry("api/public-symbols.json")!.Open());
        using var document = System.Text.Json.JsonDocument.Parse(reader.ReadToEnd());
        var members = document.RootElement.GetProperty("modules")[0].GetProperty("members");
        bool Capability(string name) => members.EnumerateArray()
            .Single(member => member.GetProperty("name").GetString() == name)
            .GetProperty("requiresGameWindow").GetBoolean();
        Equal(true, Capability("Draw"));
        Equal(true, Capability("Wrapper"));
        Equal(false, Capability("Pure"));
    }
    finally { Directory.Delete(directory, true); }
});
Run("Typed completion descriptions include parameter modes and returns", () =>
{
    const string source = "Sub Rename(ByRef Name As Text, NewName As Text)\nName = NewName\nEnd Sub\nFunction Join(First As Text, Second As Text) As Text\nReturn First + Second\nEnd Function\nPrint Ren";
    var completions = SmileCompletionService.GetCompletions(Analyze(source), source.Length);
    Equal("Sub Rename(ByRef Name As Text, NewName As Text)",
        completions.Single(item => item.DisplayText == "Rename").Description);
    Equal("Function Join(First As Text, Second As Text) As Text",
        completions.Single(item => item.DisplayText == "Join").Description);
    const string typedDeclaration = "Dim Name As ";
    Equal("Boolean|Image|Number|Text", string.Join("|", SmileCompletionService
        .GetCompletions(Analyze(typedDeclaration), typedDeclaration.Length).Select(item => item.DisplayText)));
});

Run("FormatVersion 5 public API metadata preserves Image signatures", () =>
{
    var root = Path.Combine(Path.GetTempPath(), "SmilePhase4ImagePackageTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    try
    {
        var projectPath = Path.Combine(root, "Media.smilelibproj");
        File.WriteAllText(projectPath,
            "<SmileProject Version=\"1.0\"><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Smile.Media.Proof</LibraryName><Version>1.0.0</Version><OutputName>Media</OutputName></PropertyGroup><ItemGroup><SmileSource Include=\"Media.smile\" /></ItemGroup></SmileProject>");
        File.WriteAllText(Path.Combine(root, "Media.smile"),
            "Module Smile.Media.Proof\nPublic Function Ready(Value As Image) As Boolean\nReturn Image_Loaded(Value)\nEnd Function\nEnd Module\n");
        var compilation = SmileProjectCompilation.Load(projectPath, Path.Combine(root, "cache"));
        var analysis = SmileLanguage.Analyze(compilation.Sources, SmileCompilationKind.Library,
            compilation.DependencyContext);
        Equal(false, analysis.HasErrors);
        var package = Path.Combine(root, "Media.smilelib");
        SmileLibraryPackage.Write(package, compilation.Graph.Root, analysis);
        using var archive = System.IO.Compression.ZipFile.OpenRead(package);
        using var reader = new StreamReader(archive.GetEntry("api/public-symbols.json")!.Open());
        var api = reader.ReadToEnd();
        Equal(true, api.Contains("\"type\": \"Image\"", StringComparison.Ordinal));
        Equal(true, api.Contains("\"returnType\": \"Boolean\"", StringComparison.Ordinal));
    }
    finally { Directory.Delete(root, true); }
});

Run("Image ownership emits retain release move and record cleanup on both targets", () =>
{
    const string source = "Type Media\nArt As Image\nEnd Type\nDim SourceImage As Image\nDim Copy As Image\nDim Items[2] As Image\nDim Card As Media\nCopy = SourceImage\nItems[0] = Copy\nCard.Art = Items[0]\nSourceImage = Card.Art\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var native = new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, false).Emit();
    Equal(true, native.Contains("call smile_image_retain", StringComparison.Ordinal));
    Equal(true, native.Contains("call smile_image_move_assign", StringComparison.Ordinal));
    Equal(true, native.Contains("call smile_image_clear", StringComparison.Ordinal));
    var web = new WebEmitter(analysis).Emit();
    Equal(true, web.Contains("smile.imageMoveAssign", StringComparison.Ordinal));
    Equal(true, web.Contains("smile.imageRetain", StringComparison.Ordinal));
    Equal(true, web.Contains("smile.imageRelease", StringComparison.Ordinal));
    Equal(true, web.Contains("record_0_media_clear", StringComparison.Ordinal));
});

Run("Web Image reads are owned and calls transfer without an extra retain", () =>
{
    const string source = "Game Window \"Display title\" Size 320 By 180\nDim Shared As Image\nDim Copy As Image\nCopy = GetImage()\nPrint Image_Width(GetImage())\nDraw Image GetImage() At 0, 0\nFunction GetImage() As Image\nReturn Shared\nEnd Function\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var web = new WebEmitter(analysis, "Stable.Output", new[] { "Assets/Hero.png" }).Emit();
    Equal(true, web.Contains("smile.configure(\"Stable.Output\", [\"Assets/Hero.png\"])", StringComparison.Ordinal));
    Equal(true, web.Contains("return smile.imageRetain(g_0_shared);", StringComparison.Ordinal));
    Equal(false, web.Contains("smile.imageAssign", StringComparison.Ordinal));
    Equal(true, web.Contains("smile.imageWidth(await", StringComparison.Ordinal));
    Equal(true, web.Contains("smile.drawImage(await", StringComparison.Ordinal));
});

Run("Structured clips emit balanced cleanup for Return loop exits and End Program", () =>
{
    const string source = "Game Window \"Clip cleanup\"\nCall Leave()\nFor Index = 0 To 1\nClip Rectangle 0, 0, 20, 20\nExit For\nEnd Clip\nEnd For\nDo\nClip Rectangle 0, 0, 20, 20\nExit Do\nEnd Clip\nLoop\nClip Rectangle 0, 0, 20, 20\nEnd Program\nEnd Clip\nSub Leave()\nClip Rectangle 0, 0, 20, 20\nReturn\nEnd Clip\nEnd Sub\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var native = new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, false).Emit();
    Equal(true, native.Split(new[] { "call smile_clip_pop" }, StringSplitOptions.None).Length >= 8);
    var web = new WebEmitter(analysis).Emit();
    Equal(true, web.Split(new[] { "finally {" }, StringSplitOptions.None).Length >= 6);
    Equal(true, web.Split(new[] { "smile.popClip();" }, StringSplitOptions.None).Length >= 5);
});

Run("Record types bind nominal identities nested fields arrays and deterministic layouts", () =>
{
    const string source = "Type Point\nX As Number\nY As Number\nEnd Type\nType Actor\nName As Text\nPosition As Point\nActive As Boolean\nEnd Type\nDim Hero As Actor\nDim Party[2, 2] As Actor\nHero.Position.X = 7\nParty[1, 1] = Hero\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var point = analysis.SemanticModel.Types["Point"];
    var actor = analysis.SemanticModel.Types["Actor"];
    Equal(16, point.Size);
    Equal(32, actor.Size);
    Equal(true, actor.ContainsOwnedText);
    Equal(actor, analysis.SemanticModel.Symbols["Hero"].Type);
    Equal(actor, analysis.SemanticModel.Symbols["Party"].Type);
    Equal(2, analysis.SemanticModel.Symbols["Party"].ArrayRank);
    Equal(8, actor.Fields.Single(field => field.Name == "Position").Offset);
});

Run("Record value semantics emit native helpers and Web defaults clones and fresh arrays", () =>
{
    const string source = "Type Item\nName As Text\nValue As Number\nEnd Type\nDim First As Item\nDim Copy As Item\nDim Items[2] As Item\nFirst.Name = \"A\"\nCopy = First\nFirst = First\nItems[0] = Copy\nPrint Items[0].Name\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var native = new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, false).Emit();
    Equal(true, native.Contains("record_0_item_copy PROC", StringComparison.Ordinal));
    Equal(true, native.Contains("call smile_text_retain", StringComparison.Ordinal));
    var web = new WebEmitter(analysis).Emit();
    Equal(true, web.Contains("record_0_item_default", StringComparison.Ordinal));
    Equal(true, web.Contains("record_0_item_clone", StringComparison.Ordinal));
    Equal(true, web.Contains("smile.array([2], () =>", StringComparison.Ordinal));
});

Run("Record returns shift the Windows x64 ABI through sixteen explicit parameters", () =>
{
    const string parameters = "Value1 As Number, Value2 As Number, Value3 As Number, Value4 As Number, Value5 As Number, Value6 As Number, Value7 As Number, Value8 As Number, Value9 As Number, Value10 As Number, Value11 As Number, Value12 As Number, Value13 As Number, Value14 As Number, Value15 As Number, Value16 As Number";
    var analysis = Analyze($"Type Result\nValue As Number\nEnd Type\nDim Answer As Result\nAnswer = Make(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16)\nFunction Make({parameters}) As Result\nDim Value As Result\nValue.Value = Value16\nReturn Value\nEnd Function\n");
    Equal(false, analysis.HasErrors);
    var assembly = new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, false).Emit();
    Equal(true, assembly.Contains("[rbp+144]", StringComparison.Ordinal));
    Equal(true, assembly.Contains("record_result", StringComparison.Ordinal));
});

Run("Record type and field completion shares public visibility and nested type information", () =>
{
    const string fieldSource = "Type Point\nX As Number\nY As Number\nEnd Type\nDim Hero As Point\nPrint Hero.";
    var fieldCompletions = SmileCompletionService.GetCompletions(Analyze(fieldSource), fieldSource.Length);
    Equal("X|Y", string.Join("|", fieldCompletions.Select(item => item.DisplayText)));
    Equal(true, fieldCompletions.All(item => item.Kind == SmileCompletionKind.Field));

    const string typeSource = "Type Point\nX As Number\nEnd Type\nDim Hero As ";
    var typeCompletions = SmileCompletionService.GetCompletions(Analyze(typeSource), typeSource.Length);
    Equal(true, typeCompletions.Any(item => item.DisplayText == "Point" && item.Kind == SmileCompletionKind.Type));

    const string program = "Import Example.Models As Models\nDim Value As Models.";
    var imported = Multi(("Program.smile", true, program),
        ("Models.smile", false, "Module Example.Models\nPublic Type Visible\nValue As Number\nEnd Type\nPrivate Type Hidden\nValue As Number\nEnd Type\nEnd Module\n"));
    var importedCompletions = SmileCompletionService.GetCompletions(imported, program.Length);
    Equal(true, importedCompletions.Any(item => item.DisplayText == "Visible"));
    Equal(false, importedCompletions.Any(item => item.DisplayText == "Hidden"));
    Equal(false, importedCompletions.Any(item => item.Kind is SmileCompletionKind.Variable or SmileCompletionKind.Function));
});

Run("Record diagnostics cover declarations fields cycles visibility operations and ByRef", () =>
{
    Equal(true, HasDiagnostic(Analyze("Type A\nX As Number\nEnd Type\nType A\nY As Number\nEnd Type\n"), "SML3400"));
    Equal(true, HasDiagnostic(Analyze("Type A\nX As Missing\nEnd Type\n"), "SML3401"));
    Equal(true, HasDiagnostic(Analyze("Type A\nX As Number\nX As Number\nEnd Type\n"), "SML3402"));
    Equal(true, HasDiagnostic(Analyze("Sub Work()\nType A\nX As Number\nEnd Type\nEnd Sub\n"), "SML3403"));
    Equal(true, HasDiagnostic(Analyze("Type A\nNext As A\nEnd Type\n"), "SML3404"));
    Equal(true, HasDiagnostic(Analyze("Type A\nX As Number\nEnd Type\nDim Value As A\nPrint Value.Y\n"), "SML3405"));
    Equal(true, HasDiagnostic(Analyze("Dim Value As Number\nPrint Value.X\n"), "SML3406"));
    Equal(true, HasDiagnostic(Analyze("Type A\nX As Number\nEnd Type\nDim Value As A\nPrint Value\n"), "SML3407"));
    Equal(true, HasDiagnostic(Analyze("Type A\nX As Number\nEnd Type\nCall Change(Create())\nFunction Create() As A\nDim Result As A\nReturn Result\nEnd Function\nSub Change(ByRef Value As A)\nEnd Sub\n"), "SML3305"));
});

Run("Routine compiler temporaries have distinct invocation-local frame storage", () =>
{
    const string source = "Option Explicit\nCall Work(2)\nSub Work(Level As Number)\nDim Index As Number\nDim Values[2] As Text\nFor Index = 1 To Level\nSelect Case Level\nCase 1\nPrint Index\nEnd Select\nSelect Case True\nCase True\nPrint Index\nEnd Select\nSelect Case \"X\" + \"\"\nCase \"X\"\nPrint Values[0]\nEnd Select\nEnd For\nEnd Sub\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var emitter = new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, false);
    var assembly = emitter.Emit();
    var frame = emitter.FrameLayouts.Single().Value;
    var occupied = new HashSet<int>();
    var expectedSlots = 1;
    foreach (var item in frame.LocalOffsets)
    {
        var slots = Math.Max(1, item.Key.ArraySize);
        expectedSlots += slots;
        for (var index = 0; index < slots; index++)
            Equal(true, occupied.Add(item.Value - index * 8));
    }
    expectedSlots += frame.Temporaries.Count;
    foreach (var temporary in frame.Temporaries)
        Equal(true, occupied.Add(temporary.FrameOffset));
    Equal(true, occupied.Add(frame.ReturnOffset));
    Equal(expectedSlots, occupied.Count);
    Equal(0, frame.FrameSize % 16);
    Equal(false, assembly.Contains("for_limit_", StringComparison.Ordinal));
    Equal(false, assembly.Contains("select_value_", StringComparison.Ordinal));
    Equal(true, assembly.Contains("call smile_text_move_assign", StringComparison.Ordinal));
});

Run("Owned Text selector cleanup precedes Return and loop exits", () =>
{
    var nested = Analyze(File.ReadAllText("examples/Phase3A1Hardening/NestedCleanup.smile"));
    Equal(false, nested.HasErrors);
    var nestedAssembly = new MasmEmitter(nested, SmileGraphicsBackend.Auto, true, false).Emit();
    var returnJump = nestedAssembly.IndexOf("jmp routine_return", StringComparison.Ordinal);
    var returnMove = nestedAssembly.LastIndexOf("call smile_text_move_assign", returnJump, StringComparison.Ordinal);
    var returnClear = nestedAssembly.LastIndexOf("call smile_text_clear", returnJump, StringComparison.Ordinal);
    Equal(true, returnJump > 0 && returnClear > returnMove);

    var exits = Analyze(File.ReadAllText("examples/Phase3A1Hardening/ExitCleanup.smile"));
    Equal(false, exits.HasErrors);
    var exitAssembly = new MasmEmitter(exits, SmileGraphicsBackend.Auto, true, false).Emit();
    foreach (var prefix in new[] { "for_end", "do_end" })
    {
        var jump = exitAssembly.IndexOf("jmp " + prefix, StringComparison.Ordinal);
        var move = exitAssembly.LastIndexOf("call smile_text_move_assign", jump, StringComparison.Ordinal);
        var clear = exitAssembly.LastIndexOf("call smile_text_clear", jump, StringComparison.Ordinal);
        Equal(true, jump > 0 && clear > move);
    }
});

Run("Web record fields use deterministic bound keys instead of source properties", () =>
{
    var analysis = Analyze(File.ReadAllText("examples/Phase3B1Hardening/WebFieldKeys.smile"));
    Equal(false, analysis.HasErrors);
    var web = new WebEmitter(analysis).Emit();
    foreach (var sourceName in new[] { "__proto__", "constructor", "prototype", "toString", "valueOf" })
        Equal(false, web.Contains("[\"" + sourceName + "\"]", StringComparison.Ordinal));
    foreach (var field in analysis.SemanticModel.Types.Values.SelectMany(type => type.Fields))
        Equal(true, web.Contains("__smile_r", StringComparison.Ordinal));
    Equal(true, web.Contains("__smile_r0_f0", StringComparison.Ordinal));
});

Run("Module record types cannot capture project-global ambient types", () =>
{
    const string module = "Module Example.Isolated\nPublic Type Wrapper\nValue As ConsumerData\nEnd Type\nPublic Dim Shared As ConsumerData\nPublic Function Copy(Value As ConsumerData) As ConsumerData\nReturn Value\nEnd Function\nEnd Module\n";
    var withGlobal = Multi(
        ("Program.smile", true, "Type ConsumerData\nValue As Number\nEnd Type\nPrint 1\n"),
        ("Library.smile", false, module));
    var withoutGlobal = Multi(
        ("Program.smile", true, "Print 1\n"),
        ("Library.smile", false, module));
    var withDiagnostics = withGlobal.Diagnostics.Where(item => item.Code == "SML3401")
        .Select(item => item.Message).ToArray();
    var withoutDiagnostics = withoutGlobal.Diagnostics.Where(item => item.Code == "SML3401")
        .Select(item => item.Message).ToArray();
    Equal(4, withDiagnostics.Length);
    Equal(string.Join("\n", withDiagnostics), string.Join("\n", withoutDiagnostics));
    Equal(true, withDiagnostics.All(message => message.Contains("Alias.Type", StringComparison.Ordinal)));
});

Run("Same-module record types bind unqualified across physical files", () =>
{
    var analysis = SmileLanguage.Analyze(new[]
    {
        new SmileSourceDocument("Module Example.Shared\nPublic Type Component\nValue As Number\nEnd Type\nPublic Type Container\nItem As Component\nEnd Type\nEnd Module\n", "Types.smile"),
        new SmileSourceDocument("Module Example.Shared\nPublic Dim Values[2] As Container\nPublic Function Copy(Value As Container) As Container\nDim Local As Container\nLocal = Value\nReturn Local\nEnd Function\nEnd Module\n", "Factories.smile")
    }, SmileCompilationKind.Library);
    Equal(false, analysis.HasErrors);
    Equal("Container", analysis.SemanticModel.Modules["Example.Shared"].Members["Values"].Variable!.Type.Name);
});

Run("Record completion separates type value alias and indexed-field contexts", () =>
{
    const string moduleTypes = "Module Example.Models\nPublic Type Position\nX As Number\nY As Number\nEnd Type\nPublic Type Actor\nName As Text\nPosition As Position\nEnd Type\nPublic Dim DefaultActor As Actor\nPublic Function Create() As Actor\nDim Value As Actor\nReturn Value\nEnd Function\nEnd Module\n";

    const string crossFile = "Module Example.Models\nPublic Dim Value As \nEnd Module\n";
    var crossAnalysis = Multi(
        ("Program.smile", true, "Type ConsumerOnly\nValue As Number\nEnd Type\nPrint 1\n"),
        ("Types.smile", false, moduleTypes), ("Use.smile", false, crossFile));
    var crossTypes = SmileCompletionService.GetCompletions(crossAnalysis, "Use.smile",
        crossFile.IndexOf("\nEnd Module", StringComparison.Ordinal));
    Equal(true, crossTypes.Any(item => item.DisplayText == "Actor" && item.Kind == SmileCompletionKind.Type));
    Equal(false, crossTypes.Any(item => item.DisplayText == "ConsumerOnly"));

    const string valueProgram = "Import Example.Models As Models\nPrint Models.";
    var valueAnalysis = Multi(("Program.smile", true, valueProgram), ("Models.smile", false, moduleTypes));
    var aliasValues = SmileCompletionService.GetCompletions(valueAnalysis, valueProgram.Length);
    Equal(true, aliasValues.Any(item => item.DisplayText == "DefaultActor"));
    Equal(true, aliasValues.Any(item => item.DisplayText == "Create"));
    Equal(false, aliasValues.Any(item => item.Kind == SmileCompletionKind.Type));

    const string typeProgram = "Import Example.Models As Models\nDim Value As Models.";
    var typeAnalysis = Multi(("Program.smile", true, typeProgram), ("Models.smile", false, moduleTypes));
    var aliasTypes = SmileCompletionService.GetCompletions(typeAnalysis, typeProgram.Length);
    Equal("Actor|Position", string.Join("|", aliasTypes.Select(item => item.DisplayText)));
    Equal(true, aliasTypes.All(item => item.Kind == SmileCompletionKind.Type));
    Equal(false, SmileCompletionService.GetCompletions(Analyze("Type Local\nValue As Number\nEnd Type\nPrint "), 49)
        .Any(item => item.Kind == SmileCompletionKind.Type));

    const string records = "Type Position\nX As Number\nY As Number\nEnd Type\nType Actor\nName As Text\nPosition As Position\nEnd Type\nDim Party[4] As Actor\nDim Grid[2, 2] As Actor\n";
    foreach (var expression in new[] { "Party[Index + 1].", "Grid[X, Y]." })
    {
        var source = records + "Print " + expression;
        var fields = SmileCompletionService.GetCompletions(Analyze(source), source.Length);
        Equal("Name|Position", string.Join("|", fields.Select(item => item.DisplayText)));
    }
    var nestedSource = records + "Print Party[Index + 1].Position.";
    Equal("X|Y", string.Join("|", SmileCompletionService.GetCompletions(Analyze(nestedSource), nestedSource.Length)
        .Select(item => item.DisplayText)));
    var importedFieldSource = "Import Example.Models As Models\nPrint Models.DefaultActor.";
    var importedFields = Multi(("Program.smile", true, importedFieldSource), ("Models.smile", false, moduleTypes));
    Equal("Name|Position", string.Join("|", SmileCompletionService
        .GetCompletions(importedFields, importedFieldSource.Length).Select(item => item.DisplayText)));
});

Run("FormatVersion 5 public API uses logical provider identities deterministically", () =>
{
    var root = Path.Combine(Path.GetTempPath(), "SmileP3B1ProviderTests-" + Guid.NewGuid().ToString("N"));
    var firstRoot = Path.Combine(root, "checkout-a");
    var secondRoot = Path.Combine(root, "checkout-b");
    Directory.CreateDirectory(firstRoot);
    Directory.CreateDirectory(secondRoot);
    try
    {
        const string projectText = "<SmileProject Version=\"1.0\"><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Example.ProviderBundle</LibraryName><Version>1.2.3</Version><OutputName>Provider</OutputName></PropertyGroup><ItemGroup><SmileSource Include=\"Types.smile\" /></ItemGroup></SmileProject>";
        const string sourceText = "Module Example.Models\nPublic Type Actor\nName As Text\nEnd Type\nEnd Module\n";
        byte[] Build(string checkout, string packagePath)
        {
            File.WriteAllText(Path.Combine(checkout, "Provider.smilelibproj"), projectText);
            File.WriteAllText(Path.Combine(checkout, "Types.smile"), sourceText);
            var compilation = SmileProjectCompilation.Load(Path.Combine(checkout, "Provider.smilelibproj"),
                Path.Combine(checkout, "cache"));
            var analysis = SmileLanguage.Analyze(compilation.Sources, SmileCompilationKind.Library,
                compilation.DependencyContext);
            Equal(false, analysis.HasErrors);
            SmileLibraryPackage.Write(packagePath, compilation.Graph.Root, analysis);
            return File.ReadAllBytes(packagePath);
        }

        var firstPackage = Path.Combine(firstRoot, "Provider.smilelib");
        var secondPackage = Path.Combine(secondRoot, "Provider.smilelib");
        var firstBytes = Build(firstRoot, firstPackage);
        var secondBytes = Build(secondRoot, secondPackage);
        Equal(true, firstBytes.SequenceEqual(secondBytes));
        string api;
        using (var archive = System.IO.Compression.ZipFile.OpenRead(firstPackage))
        using (var reader = new StreamReader(archive.GetEntry("api/public-symbols.json")!.Open()))
            api = reader.ReadToEnd();
        Equal(true, api.Contains("\"module\": \"Example.Models\"", StringComparison.Ordinal));
        Equal(true, api.Contains("\"provider\": \"Example.ProviderBundle@1.2.3\"", StringComparison.Ordinal));
        Equal(false, api.Contains(firstRoot, StringComparison.OrdinalIgnoreCase));
        Equal(false, api.Contains(secondRoot, StringComparison.OrdinalIgnoreCase));

        RewritePackageTextEntry(firstPackage, "api/public-symbols.json", text =>
            text.Replace("Example.ProviderBundle@1.2.3", "Wrong.Provider@9.9.9", StringComparison.Ordinal));
        ThrowsProjectDiagnostic(() => SmileLibraryPackage.Read(firstPackage, Path.Combine(root, "read-cache")), "SML3207");
    }
    finally { Directory.Delete(root, true); }
});

Run("Public API preserves referenced record provider identities", () =>
{
    var root = Path.Combine(Path.GetTempPath(), "SmileP3B1DependencyProviderTests-" + Guid.NewGuid().ToString("N"));
    var baseRoot = Path.Combine(root, "Base");
    var consumerRoot = Path.Combine(root, "Consumer");
    Directory.CreateDirectory(baseRoot);
    Directory.CreateDirectory(consumerRoot);
    try
    {
        File.WriteAllText(Path.Combine(baseRoot, "Base.smilelibproj"),
            "<SmileProject Version=\"1.0\"><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Example.BaseProvider</LibraryName><Version>1.0.0</Version><OutputName>Base</OutputName></PropertyGroup><ItemGroup><SmileSource Include=\"Types.smile\" /></ItemGroup></SmileProject>");
        File.WriteAllText(Path.Combine(baseRoot, "Types.smile"),
            "Module Example.Base\nPublic Type Point\nX As Number\nEnd Type\nEnd Module\n");
        File.WriteAllText(Path.Combine(consumerRoot, "Consumer.smilelibproj"),
            "<SmileProject Version=\"1.0\"><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Example.ConsumerProvider</LibraryName><Version>2.0.0</Version><OutputName>Consumer</OutputName></PropertyGroup><ItemGroup><SmileSource Include=\"Types.smile\" /><SmileProjectReference Include=\"..\\Base\\Base.smilelibproj\" /></ItemGroup></SmileProject>");
        File.WriteAllText(Path.Combine(consumerRoot, "Types.smile"),
            "Module Example.Consumer\nImport Example.Base As Base\nPublic Type Wrapper\nValue As Base.Point\nEnd Type\nPublic Function Copy(Value As Base.Point) As Base.Point\nReturn Value\nEnd Function\nEnd Module\n");

        string BuildPackage(string projectPath, string packagePath)
        {
            var compilation = SmileProjectCompilation.Load(projectPath, Path.Combine(root, "project-cache"));
            var analysis = SmileLanguage.Analyze(compilation.Sources, SmileCompilationKind.Library,
                compilation.DependencyContext);
            Equal(false, analysis.HasErrors);
            SmileLibraryPackage.Write(packagePath, compilation.Graph.Root, analysis);
            return packagePath;
        }

        var basePackage = BuildPackage(Path.Combine(baseRoot, "Base.smilelibproj"), Path.Combine(root, "Base.smilelib"));
        var consumerPackage = BuildPackage(Path.Combine(consumerRoot, "Consumer.smilelibproj"),
            Path.Combine(root, "Consumer.smilelib"));
        string api;
        using (var archive = System.IO.Compression.ZipFile.OpenRead(consumerPackage))
        using (var reader = new StreamReader(archive.GetEntry("api/public-symbols.json")!.Open()))
            api = reader.ReadToEnd();
        Equal(true, api.Contains("\"typeProvider\": \"Example.BaseProvider@1.0.0\"", StringComparison.Ordinal));
        Equal(true, api.Contains("\"provider\": \"Example.ConsumerProvider@2.0.0\"", StringComparison.Ordinal));
        Equal(false, api.Contains(root, StringComparison.OrdinalIgnoreCase));
        SmileLibraryProviderResolver.LoadPackages(new[] { basePackage, consumerPackage },
            Path.Combine(root, "package-cache"));
    }
    finally { Directory.Delete(root, true); }
});

Run("Syntax-aware formatter preserves all authoritative direct Return forms", () =>
{
    const string source = "Option Explicit\n\nConst MAX_ITEMS = 10\n\nDim ModuleValue As Number\n\n" +
        "Function ReturnLocal() As Number\n\n    Dim LocalValue As Number\n\n    LocalValue = 7\n    Return LocalValue\n\nEnd Function\n\n" +
        "Function ReturnParameter(Value As Number) As Number\n\n    Return Value\n\nEnd Function\n\n" +
        "Function ReturnModuleValue() As Number\n\n    Return ModuleValue\n\nEnd Function\n\n" +
        "Function ReturnConstant() As Number\n\n    Return MAX_ITEMS\n\nEnd Function\n\n" +
        "Function ReturnBooleanLiteral() As Boolean\n\n    Return False\n\nEnd Function\n\n" +
        "Function ReturnNumberLiteral() As Number\n\n    Return 0\n\nEnd Function\n\n" +
        "Function ReturnBuiltInConstant() As Number\n\n    Return KEY_ENTER\n\nEnd Function\n\n" +
        "Function ReturnTextLiteral() As Text\n\n    Return \"Ready\"\n\nEnd Function\n";
    Equal(source, FormatSource(source));
});

Run("Syntax-aware formatter rewrites complete multiline computed Return spans exactly", () =>
{
    const string source = "Option Explicit\n\nImport Example.Math As Math\n\n" +
        "Function AddValues(FirstValue As Number, SecondValue As Number) As Number\n\n" +
        "    Return (\n        FirstValue +\n        SecondValue\n    )\n\nEnd Function\n\n" +
        "Function CalculateValues(FirstValue As Number, SecondValue As Number) As Number\n\n" +
        "    Return Math.Calculate(\n        FirstValue,\n        SecondValue\n    )\n\nEnd Function\n";
    const string expected = "Option Explicit\n\nImport Example.Math As Math\n\n" +
        "Function AddValues(FirstValue As Number, SecondValue As Number) As Number\n\n" +
        "    Dim ReturnValue As Number\n\n" +
        "    ReturnValue = (\n        FirstValue +\n        SecondValue\n    )\n\n" +
        "    Return ReturnValue\n\nEnd Function\n\n" +
        "Function CalculateValues(FirstValue As Number, SecondValue As Number) As Number\n\n" +
        "    Dim ReturnValue As Number\n\n" +
        "    ReturnValue = Math.Calculate(\n        FirstValue,\n        SecondValue\n    )\n\n" +
        "    Return ReturnValue\n\nEnd Function\n";
    var formatted = FormatSource(source);
    Equal(expected, formatted);
    Equal(formatted, FormatSource(formatted));
});

Run("Syntax-aware formatter traverses public and private module declarations", () =>
{
    const string source = "Module Example.Visibility\n\nOption Explicit\n\n" +
        "Public Function PublicValue() As Number\n\n    Return 1 + 2\n\nEnd Function\n\n" +
        "Private Function PrivateValue() As Number\n\n    Return 3 + 4\n\nEnd Function\n\nEnd Module\n";
    var formatted = FormatSource(source);
    Equal(2, formatted.Split("Dim ReturnValue As Number", StringSplitOptions.None).Length - 1);
    Equal(true, formatted.Contains("ReturnValue = 1 + 2", StringComparison.Ordinal));
    Equal(true, formatted.Contains("ReturnValue = 3 + 4", StringComparison.Ordinal));
    Equal(false, SmileLanguage.Analyze(formatted).HasErrors);
});

Run("Syntax-aware formatter traverses nested Clip Return and long If statements", () =>
{
    const string source = "Option Explicit\n\nGame Window \"Clip Formatter\"\n\nDim Result As Number\n\n" +
        "Result = Calculate(50)\n\n" +
        "Function Calculate(Value As Number) As Number\n\n" +
        "    Clip Rectangle 0, 0, 100, 100\n" +
        "        If (Value < 0\n            Or Value > 100\n            Or Value = 50) Then\n" +
        "            Return Value + 1\n        End If\n\n" +
        "        Clip Rectangle 10, 10, 80, 80\n" +
        "            If Value = 1 Then\n                Return 1\n" +
        "            Else If Value < 10 Or Value > 20 Or Value = 15 Then\n" +
        "                Return Value + 2\n            End If\n        End Clip\n    End Clip\n\n" +
        "    Return 0\n\nEnd Function\n";
    var formatted = FormatSource(source);
    Equal(true, formatted.Contains("If (Value < 0 Or\n            Value > 100 Or\n            Value = 50) Then", StringComparison.Ordinal));
    Equal(true, formatted.Contains("Else If (Value < 10 Or\n                Value > 20 Or\n                Value = 15) Then", StringComparison.Ordinal));
    Equal(true, formatted.Contains("ReturnValue = Value + 1", StringComparison.Ordinal));
    Equal(true, formatted.Contains("ReturnValue = Value + 2", StringComparison.Ordinal));
    Equal(formatted, FormatSource(formatted));
    var analysis = SmileLanguage.Analyze(formatted);
    Equal(false, analysis.HasErrors);
    Equal(true, new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, false).Emit()
        .Contains("smile_clip_push", StringComparison.Ordinal));
    Equal(true, new WebEmitter(analysis).Emit().Contains("smile.pushClip", StringComparison.Ordinal));
});

Run("Syntax-owned If layouts classify multiline complete blocks", () =>
{
    const string compact = "Option Explicit\n\nDim First As Boolean\nDim Second As Boolean\nDim Third As Boolean\n\n" +
        "If (First Or\n    Second Or\n    Third) Then\n\n    Print \"Matched\"\n\nEnd If\n";
    var compactLayout = SmileSourceFormatter.GetIfBlockLayouts(compact, "Compact.smile").Single();
    Equal(false, compactLayout.IsExpanded);
    Equal("9", string.Join("|", compactLayout.HeaderEndLines));
    Equal("13", string.Join("|", compactLayout.BoundaryLines));

    const string expanded = "Option Explicit\n\nDim First As Boolean\nDim Second As Boolean\nDim Third As Boolean\n\n" +
        "If (First Or\n    Second Or\n    Third) Then\n    Print \"One\"\n    Print \"Two\"\n    Print \"Three\"\n" +
        "Else If (Second Or\n    Third Or\n    First) Then\n    Print \"Four\"\nEnd If\n";
    var expandedLayout = SmileSourceFormatter.GetIfBlockLayouts(expanded, "Expanded.smile").Single();
    Equal(true, expandedLayout.IsExpanded);
    Equal("9|15", string.Join("|", expandedLayout.HeaderEndLines));
    Equal("13|17", string.Join("|", expandedLayout.BoundaryLines));
});

Run("Symbol-aware formatter preserves only qualified constants and module variables", () =>
{
    const string provider = "Module Example.Values\n\nOption Explicit\n\nPublic Const UI_EVENT_NONE = 0\n" +
        "Public Dim DefaultValue As Number\nPublic Dim Items[2] As Number\n\n" +
        "Public Type Point\n    X As Number\nEnd Type\n\n" +
        "Public Type Holder\n    Value As Number\n    Position As Point\nEnd Type\n\n" +
        "Public Dim Current As Holder\nPrivate Const HIDDEN = 9\n\n" +
        "Public Function CreateValue() As Number\n\n    Return 1\n\nEnd Function\n\n" +
        "Public Function SameConstant() As Number\n\n    Return UI_EVENT_NONE\n\nEnd Function\n\n" +
        "Public Function SameVariable() As Number\n\n    Return DefaultValue\n\nEnd Function\n\nEnd Module\n";
    const string consumer = "Module Example.Consumer\n\nOption Explicit\n\nImport Example.Values As Values\n" +
        "Import Missing.Provider As Missing\n\n" +
        "Public Function ConstantValue() As Number\n\n    Return Values.UI_EVENT_NONE\n\nEnd Function\n\n" +
        "Public Function ModuleValue() As Number\n\n    Return Values.DefaultValue\n\nEnd Function\n\n" +
        "Public Function FieldValue() As Number\n\n    Return Values.Current.Value\n\nEnd Function\n\n" +
        "Public Function NestedFieldValue() As Number\n\n    Return Values.Current.Position.X\n\nEnd Function\n\n" +
        "Public Function ArrayValue() As Number\n\n    Return Values.Items[0]\n\nEnd Function\n\n" +
        "Public Function CallValue() As Number\n\n    Return Values.CreateValue()\n\nEnd Function\n\n" +
        "Public Function PrivateValue() As Number\n\n    Return Values.HIDDEN\n\nEnd Function\n\n" +
        "Public Function MissingValue() As Number\n\n    Return Missing.UNKNOWN_VALUE\n\nEnd Function\n\nEnd Module\n";
    var providerPath = Path.GetFullPath("FormatterProvider.smile");
    var consumerPath = Path.GetFullPath("FormatterConsumer.smile");
    var analysis = SmileLanguage.Analyze(new[]
    {
        new SmileSourceDocument(provider, providerPath),
        new SmileSourceDocument(consumer, consumerPath)
    }, SmileCompilationKind.Library);
    var tree = analysis.GetSyntaxTree(consumerPath);
    var formatted = SmileSourceFormatter.Format(consumer, true, 100, true, true, consumerPath, analysis, tree);
    Equal(true, formatted.Contains("Return Values.UI_EVENT_NONE", StringComparison.Ordinal));
    Equal(true, formatted.Contains("Return Values.DefaultValue", StringComparison.Ordinal));
    foreach (var expression in new[] { "Values.Current.Value", "Values.Current.Position.X", "Values.Items[0]",
                 "Values.CreateValue()", "Values.HIDDEN", "Missing.UNKNOWN_VALUE" })
        Equal(true, formatted.Contains("ReturnValue = " + expression, StringComparison.Ordinal));
    var refreshed = SmileLanguage.Analyze(new[]
    {
        new SmileSourceDocument(provider, providerPath),
        new SmileSourceDocument(formatted, consumerPath)
    }, SmileCompilationKind.Library);
    Equal(formatted, SmileSourceFormatter.Format(formatted, true, 100, true, true, consumerPath,
        refreshed, refreshed.GetSyntaxTree(consumerPath)));
});

Run("Qualified direct Returns honor project and package provider boundaries", () =>
{
    var root = Path.Combine(Path.GetTempPath(), "SmileFormatterProviderTests-" + Guid.NewGuid().ToString("N"));
    var providerRoot = Path.Combine(root, "Provider");
    var consumerRoot = Path.Combine(root, "Consumer");
    Directory.CreateDirectory(providerRoot);
    Directory.CreateDirectory(consumerRoot);
    try
    {
        var providerProject = Path.Combine(providerRoot, "Provider.smilelibproj");
        var providerSource = Path.Combine(providerRoot, "Values.smile");
        var consumerProject = Path.Combine(consumerRoot, "Consumer.smilelibproj");
        var consumerSource = Path.Combine(consumerRoot, "Consumer.smile");
        const string provider = "Module Example.Values\nPublic Const UI_EVENT_NONE = 0\nPublic Dim DefaultValue As Number\nEnd Module\n";
        const string consumer = "Module Example.Consumer\nImport Example.Values As Values\n" +
            "Public Function ConstantValue() As Number\nReturn Values.UI_EVENT_NONE\nEnd Function\n" +
            "Public Function ModuleValue() As Number\nReturn Values.DefaultValue\nEnd Function\nEnd Module\n";
        File.WriteAllText(providerSource, provider);
        File.WriteAllText(providerProject,
            "<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Example.Provider</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include=\"Values.smile\" /></ItemGroup></SmileProject>");
        File.WriteAllText(consumerSource, consumer);
        File.WriteAllText(consumerProject,
            "<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Example.Consumer</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include=\"Consumer.smile\" /><SmileProjectReference Include=\"..\\Provider\\Provider.smilelibproj\" /></ItemGroup></SmileProject>");

        string FormatCompilation(SmileProjectCompilation compilation)
        {
            var analysis = SmileLanguage.Analyze(compilation.Sources, compilation.CompilationKind,
                compilation.DependencyContext);
            var tree = analysis.GetSyntaxTree(consumerSource);
            return SmileSourceFormatter.Format(consumer, true, 100, true, true, consumerSource, analysis, tree);
        }

        var projectFormatted = FormatCompilation(SmileProjectCompilation.Load(consumerProject,
            Path.Combine(root, "project-cache")));
        Equal(true, projectFormatted.Contains("Return Values.UI_EVENT_NONE", StringComparison.Ordinal));
        Equal(true, projectFormatted.Contains("Return Values.DefaultValue", StringComparison.Ordinal));

        var providerCompilation = SmileProjectCompilation.Load(providerProject, Path.Combine(root, "provider-cache"));
        var providerAnalysis = SmileLanguage.Analyze(providerCompilation.Sources, SmileCompilationKind.Library,
            providerCompilation.DependencyContext);
        var package = Path.Combine(providerRoot, "Provider.smilelib");
        SmileLibraryPackage.Write(package, providerCompilation.Graph.Root, providerAnalysis);
        File.WriteAllText(consumerProject,
            "<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Example.Consumer</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include=\"Consumer.smile\" /><SmileLibraryReference Include=\"..\\Provider\\Provider.smilelib\" /></ItemGroup></SmileProject>");
        var packageFormatted = FormatCompilation(SmileProjectCompilation.Load(consumerProject,
            Path.Combine(root, "package-cache")));
        Equal(projectFormatted, packageFormatted);
    }
    finally { Directory.Delete(root, true); }
});

Run("Syntax-aware formatter handles every computed Return category and collision-free names", () =>
{
    const string source = "Option Explicit\n\nType Holder\n    Value As Number\nEnd Type\n\n" +
        "Dim Values[2] As Number\nDim HolderValue As Holder\n\n" +
        "Function Identity(Value As Number) As Number\n\n    Return Value\n\nEnd Function\n\n" +
        "Function FromField() As Number\n\n    Return HolderValue.Value\n\nEnd Function\n\n" +
        "Function FromArray() As Number\n\n    Return Values[0]\n\nEnd Function\n\n" +
        "Function FromCall() As Number\n\n    Return Identity(1)\n\nEnd Function\n\n" +
        "Function FromUnary() As Number\n\n    Return -1\n\nEnd Function\n\n" +
        "Function FromBinary() As Number\n\n    Return 1 + 2\n\nEnd Function\n\n" +
        "Function FromComparison() As Boolean\n\n    Return 1 < 2\n\nEnd Function\n\n" +
        "Function FromParentheses() As Number\n\n    Return (1)\n\nEnd Function\n\n" +
        "Function Collision() As Number\n\n    Dim ReturnValue As Number\n\n    Return 2 * 3\n\nEnd Function\n";
    var formatted = FormatSource(source);
    Equal(true, formatted.Contains("ReturnValue = HolderValue.Value", StringComparison.Ordinal));
    Equal(true, formatted.Contains("ReturnValue = Values[0]", StringComparison.Ordinal));
    Equal(true, formatted.Contains("ReturnValue = Identity(1)", StringComparison.Ordinal));
    Equal(true, formatted.Contains("ReturnValue = -1", StringComparison.Ordinal));
    Equal(true, formatted.Contains("ReturnValue = 1 + 2", StringComparison.Ordinal));
    Equal(true, formatted.Contains("ReturnValue = 1 < 2", StringComparison.Ordinal));
    Equal(true, formatted.Contains("ReturnValue = (1)", StringComparison.Ordinal));
    Equal(true, formatted.Contains("Dim ReturnValue2 As Number", StringComparison.Ordinal));
    Equal(true, formatted.Contains("ReturnValue2 = 2 * 3", StringComparison.Ordinal));
    Equal(false, SmileLanguage.Analyze(formatted).HasErrors);
});

Run("Syntax-aware Return intermediates retain Text Image and record ownership types", () =>
{
    const string source = "Module Example.Ownership\n\nOption Explicit\n\nPublic Type Point\n    X As Number\nEnd Type\n\n" +
        "Private Function CreatePoint() As Point\n\n    Dim Value As Point\n\n    Value.X = 1\n    Return Value\n\nEnd Function\n\n" +
        "Public Function CopyPoint() As Point\n\n    Return CreatePoint()\n\nEnd Function\n\n" +
        "Private Function ForwardText(Value As Text) As Text\n\n    Return Value\n\nEnd Function\n\n" +
        "Public Function CopyText(Value As Text) As Text\n\n    Return ForwardText(Value)\n\nEnd Function\n\n" +
        "Private Function ForwardImage(ByRef Value As Image) As Image\n\n    Return Value\n\nEnd Function\n\n" +
        "Public Function CopyImage(ByRef Value As Image) As Image\n\n    Return ForwardImage(Value)\n\nEnd Function\n\nEnd Module\n";
    var formatted = FormatSource(source);
    Equal(true, formatted.Contains("Dim ReturnValue As Point", StringComparison.Ordinal));
    Equal(true, formatted.Contains("Dim ReturnValue As Text", StringComparison.Ordinal));
    Equal(true, formatted.Contains("Dim ReturnValue As Image", StringComparison.Ordinal));
    Equal(formatted, FormatSource(formatted));
    Equal(false, SmileLanguage.Analyze(formatted).HasErrors);

    const string imported = "Module Example.Consumer\n\nOption Explicit\n\nImport Example.Base As Base\n\n" +
        "Public Function CopyImported() As Base.Point\n\n    Return Base.CreatePoint()\n\nEnd Function\n\nEnd Module\n";
    var importedFormatted = FormatSource(imported);
    Equal(true, importedFormatted.Contains("Dim ReturnValue As Base.Point", StringComparison.Ordinal));
    Equal(true, importedFormatted.Contains("ReturnValue = Base.CreatePoint()", StringComparison.Ordinal));
});

Run("Syntax-aware formatter preserves multiline Return comments blank lines calls and Boolean shape", () =>
{
    const string source = "Option Explicit\r\n\r\n" +
        "Function Combine(First As Number, Second As Number) As Number\r\n\r\n    Return First + Second\r\n\r\nEnd Function\r\n\r\n" +
        "Function Nested(Value As Number) As Number\r\n\r\n    Return Combine(\r\n        Combine(\r\n            Value,\r\n            1\r\n        ),\r\n\r\n        2\r\n    )\r\n\r\nEnd Function\r\n\r\n" +
        "Function Both(First As Boolean, Second As Boolean) As Boolean\r\n\r\n    Return (\r\n        First And ' Keep this Return comment\r\n\r\n        Second\r\n    )\r\n\r\nEnd Function\r\n";
    var formatted = FormatSource(source);
    Equal(true, formatted.Contains("ReturnValue = Combine(\n        Combine(", StringComparison.Ordinal));
    Equal(true, formatted.Contains("        ),\n\n        2", StringComparison.Ordinal));
    Equal(true, formatted.Contains("First And ' Keep this Return comment\n\n        Second", StringComparison.Ordinal));
    Equal(1, formatted.Split("Keep this Return comment", StringSplitOptions.None).Length - 1);
    Equal(formatted, FormatSource(formatted));
    Equal(false, SmileLanguage.Analyze(formatted).HasErrors);
});

Run("Syntax-aware formatter normalizes leading Boolean operators with parser-owned spans", () =>
{
    const string source = "Option Explicit\n\nDim FirstCondition As Boolean\nDim SecondCondition As Boolean\n" +
        "Dim ThirdCondition As Boolean\n\nIf (FirstCondition\n    Or SecondCondition\n    Or ThirdCondition) Then\n" +
        "    Print \"Matched\"\nEnd If\n";
    const string expected = "Option Explicit\n\nDim FirstCondition As Boolean\nDim SecondCondition As Boolean\n" +
        "Dim ThirdCondition As Boolean\n\nIf (FirstCondition Or\n    SecondCondition Or\n    ThirdCondition) Then\n" +
        "    Print \"Matched\"\nEnd If\n";
    var formatted = FormatSource(source);
    Equal(expected, formatted);
    Equal(formatted, FormatSource(formatted));
    Equal(false, SmileLanguage.Analyze(formatted).HasErrors);
});

Run("Syntax-aware long If formatting preserves comments nested calls and text literals", () =>
{
    const string source = "Option Explicit\n\nDim First As Boolean\nDim Second As Boolean\nDim Third As Boolean\n" +
        "Dim Fourth As Boolean\n\nFunction Matches(Value As Text) As Boolean\n\n    Return True\n\nEnd Function\n\n" +
        "If (Matches(\"A And B\") Or ' Preserve this comment\n    (First And Second) Or\n    Third Or\n    Fourth) Then\n" +
        "    Print \"Matched\"\nEnd If\n";
    var formatted = FormatSource(source);
    Equal(true, formatted.Contains("Matches(\"A And B\") Or ' Preserve this comment", StringComparison.Ordinal));
    Equal(true, formatted.Contains("    (First And Second) Or\n    Third Or\n    Fourth) Then", StringComparison.Ordinal));
    Equal(1, formatted.Split("Preserve this comment", StringSplitOptions.None).Length - 1);
    Equal(formatted, FormatSource(formatted.Replace("\n", "\r\n")));
    Equal(false, SmileLanguage.Analyze(formatted).HasErrors);
});

Run("Syntax-aware long If thresholds cover short over-limit and Else If conditions", () =>
{
    const string shortSource = "Option Explicit\n\nDim First As Boolean\nDim Second As Boolean\n\n" +
        "If First Or Second Then\n    Print \"Short\"\nEnd If\n";
    Equal(shortSource, FormatSource(shortSource));

    const string source = "Option Explicit\n\nDim FirstConditionWithALongName As Boolean\n" +
        "Dim SecondConditionWithALongName As Boolean\nDim ThirdCondition As Boolean\nDim FourthCondition As Boolean\n\n" +
        "If FirstConditionWithALongName Or SecondConditionWithALongName Then\n" +
        "    Print \"Long\"\nElse If FirstConditionWithALongName Or ThirdCondition Or FourthCondition Then\n" +
        "    Print \"Else If\"\nEnd If\n";
    var formatted = SmileSourceFormatter.Format(source, formatLongIf: true, maximumLineLength: 60,
        rewriteComputedReturns: true, formatContextualIdentifiers: true, filePath: "FormatterTest.smile");
    Equal(true, formatted.Contains("If (FirstConditionWithALongName Or\n    SecondConditionWithALongName) Then", StringComparison.Ordinal));
    Equal(true, formatted.Contains("Else If (FirstConditionWithALongName Or\n    ThirdCondition Or\n    FourthCondition) Then", StringComparison.Ordinal));
    Equal(formatted, SmileSourceFormatter.Format(formatted, true, 60, true, true, "FormatterTest.smile"));
    Equal(false, SmileLanguage.Analyze(formatted).HasErrors);
});

Run("Formatter presentation preserves intentional invalid Return diagnostic purpose", () =>
{
    const string source = "Option Explicit\n\nFunction InvalidReturn() As Number\n\n    Return (1 + )\n\nEnd Function\n";
    var before = SmileLanguage.Analyze(source).Diagnostics.Where(diagnostic =>
        diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code).ToArray();
    var formatted = SmileSourceFormatter.Format(source, formatLongIf: true, maximumLineLength: 100,
        rewriteComputedReturns: false, formatContextualIdentifiers: true, filePath: "InvalidReturn/Program.smile");
    var after = SmileLanguage.Analyze(formatted).Diagnostics.Where(diagnostic =>
        diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code).ToArray();
    Equal(true, before.Length > 0);
    Equal(string.Join("|", before), string.Join("|", after));
    Equal(true, formatted.Contains("Return (1 + )", StringComparison.Ordinal));
});

Run("Syntax-aware formatter presents contextual identifiers without changing constants", () =>
{
    const string source = "Module Example.Layout\n\nOption Explicit\n\nPublic Const LEFT_LIMIT = 10\n\n" +
        "Public Type Insets\n    LEFT As Number\n    Top As Number\n    RIGHT As Number\n    Bottom As Number\n" +
        "End Type\n\nPublic Type ContextNames\n    TEXT As Number\n    LINE As Number\n    WINDOW As Number\n" +
        "    SIZE As Number\n    KEY As Number\nEnd Type\n\n" +
        "Public Function HorizontalTotal(ByRef Value As Insets) As Number\n\n" +
        "    Dim ReturnValue As Number\n\n    ReturnValue = Value.LEFT + Value.RIGHT\n\n" +
        "    Return ReturnValue\n\nEnd Function\n\n" +
        "Public Function ContextTotal(ByRef Value As ContextNames) As Number\n\n" +
        "    Dim ReturnValue As Number\n\n" +
        "    ReturnValue = Value.TEXT + Value.LINE + Value.WINDOW + Value.SIZE + Value.KEY\n\n" +
        "    Return ReturnValue\n\nEnd Function\n\nEnd Module\n";
    var formatted = FormatSource(source);
    Equal(true, formatted.Contains("    Left As Number", StringComparison.Ordinal));
    Equal(true, formatted.Contains("    Right As Number", StringComparison.Ordinal));
    Equal(true, formatted.Contains("Value.Left + Value.Right", StringComparison.Ordinal));
    Equal(true, formatted.Contains("Value.Text + Value.Line + Value.Window + Value.Size + Value.Key", StringComparison.Ordinal));
    Equal(true, formatted.Contains("LEFT_LIMIT", StringComparison.Ordinal));
    Equal(formatted, FormatSource(formatted));
    Equal(false, SmileLanguage.Analyze(formatted).HasErrors);
});

Run("ApplicationId validates optional project identity and preserves OutputName fallback", () =>
{
    foreach (var value in new[] { "smile.game", "com.example.game-2", "a.b", "smile.app.a0123456789abcdef0123456789abcdef" })
        Equal(true, SmileApplicationIdentity.IsValid(value));
    foreach (var value in new[] { "", "ab", "Smile.game", "smile", ".smile", "smile.", "smile..game",
        "1smile.game", "smile.game-", "smile.game_name", "smile.gáme", new string('a', 127) + ".b" })
        Equal(false, SmileApplicationIdentity.IsValid(value));

    var legacy = ProjectSources("<SmileProject><PropertyGroup><ProjectKind>Console</ProjectKind>" +
        "<StartupFile>Program.smile</StartupFile><OutputName>LegacyName</OutputName></PropertyGroup>" +
        "<ItemGroup><SmileSource Include=\"Program.smile\" StartupOnly=\"true\" /></ItemGroup></SmileProject>");
    Equal(null, legacy.ApplicationId);
    Equal("LegacyName", legacy.EffectiveApplicationId);

    var explicitIdentity = ProjectSources("<SmileProject><PropertyGroup><ProjectKind>Game</ProjectKind>" +
        "<StartupFile>Program.smile</StartupFile><OutputName>Renamed</OutputName>" +
        "<ApplicationId>smile.tests.stable</ApplicationId></PropertyGroup>" +
        "<ItemGroup><SmileSource Include=\"Program.smile\" StartupOnly=\"true\" /></ItemGroup></SmileProject>");
    Equal("smile.tests.stable", explicitIdentity.ApplicationId);
    Equal("smile.tests.stable", explicitIdentity.EffectiveApplicationId);

    ThrowsProjectDiagnostic(() => ProjectSources("<SmileProject><PropertyGroup><ProjectKind>Console</ProjectKind>" +
        "<ApplicationId>Bad.Id</ApplicationId></PropertyGroup><ItemGroup>" +
        "<SmileSource Include=\"Program.smile\" StartupOnly=\"true\" /></ItemGroup></SmileProject>"), "SML3800");
    ThrowsProjectDiagnostic(() => ProjectSources("<SmileProject><PropertyGroup><ProjectKind>Console</ProjectKind>" +
        "<ApplicationId>smile.one</ApplicationId></PropertyGroup><PropertyGroup>" +
        "<ApplicationId>smile.two</ApplicationId></PropertyGroup><ItemGroup>" +
        "<SmileSource Include=\"Program.smile\" StartupOnly=\"true\" /></ItemGroup></SmileProject>"), "SML3801");
    ThrowsProjectDiagnostic(() => ProjectSources("<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind>" +
        "<LibraryName>Example</LibraryName><Version>1.0.0</Version><ApplicationId>smile.library</ApplicationId>" +
        "</PropertyGroup><ItemGroup><SmileSource Include=\"Module.smile\" /></ItemGroup></SmileProject>"), "SML3802");
});

Run("ApplicationId CLI parses and rejects conflicting project overrides", () =>
{
    Equal(true, CompilerOptions.TryParse(new[] { "Program.smile", "--application-id", "smile.cli.test" },
        out var looseOptions, out _));
    Equal("smile.cli.test", looseOptions.ApplicationId);
    Equal(false, CompilerOptions.TryParse(new[] { "Program.smile", "--application-id" }, out _, out _));
    Equal(false, CompilerOptions.TryParse(new[] { "Program.smile", "--application-id", "smile.one",
        "--application-id", "smile.two" }, out _, out _));

    var directory = Path.Combine(Path.GetTempPath(), "smile-application-id-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var programPath = Path.Combine(directory, "Program.smile");
        var projectPath = Path.Combine(directory, "Identity.smileproj");
        File.WriteAllText(programPath, "Print \"identity\"\n");
        File.WriteAllText(projectPath, "<SmileProject><PropertyGroup><ProjectKind>Console</ProjectKind>" +
            "<StartupFile>Program.smile</StartupFile><OutputName>Identity</OutputName>" +
            "<ApplicationId>smile.project.identity</ApplicationId></PropertyGroup><ItemGroup>" +
            "<SmileSource Include=\"Program.smile\" StartupOnly=\"true\" /></ItemGroup></SmileProject>");
        Equal(1, new CompilerDriver().Run(new[] { "--project", projectPath, "--target", "web",
            "--output-dir", Path.Combine(directory, "web"), "--application-id", "smile.other.identity" }));
        Equal(0, new CompilerDriver().Run(new[] { "--project", projectPath, "--target", "web",
            "--output-dir", Path.Combine(directory, "web"), "--application-id", "smile.project.identity" }));
        Equal(true, File.ReadAllText(Path.Combine(directory, "web", "game.js"))
            .Contains("smile.project.identity", StringComparison.Ordinal));
    }
    finally
    {
        Directory.Delete(directory, true);
    }
});

Run("Phase 7 Smile.Game and Smile.RPG are ordinary built-in source packages with bounded public modules", () =>
{
    var gameProject = SmileProjectSourceSet.Load("libraries/Smile.Game/Smile.Game.smilelibproj");
    var gameCompilation = SmileProjectCompilation.Load(gameProject.ProjectPath);
    var gameAnalysis = SmileLanguage.Analyze(gameCompilation.Sources, SmileCompilationKind.Library,
        gameCompilation.DependencyContext);
    Equal(false, gameAnalysis.HasErrors);
    Equal("1.0.0", gameProject.Version);
    Equal(5, gameProject.CompilationSources.Count);
    Equal(true, SmileBuiltInLibraryCatalog.IsBuiltIn("Smile.Game"));
    foreach (var module in new[] { "Smile.Game.Core", "Smile.Game.Animation", "Smile.Game.TileMap",
        "Smile.Game.Camera2D", "Smile.Game.Collision2D" })
        Equal(true, gameAnalysis.SemanticModel.Modules.ContainsKey(module));
    Equal(false, gameProject.References.Any());
    Equal(false, gameProject.CompilationSources.Any(source => File.ReadAllText(source.FullPath)
        .Contains("Game Window", StringComparison.OrdinalIgnoreCase)));

    var rpgProject = SmileProjectSourceSet.Load("libraries/Smile.RPG/Smile.RPG.smilelibproj");
    var rpgCompilation = SmileProjectCompilation.Load(rpgProject.ProjectPath);
    var rpgAnalysis = SmileLanguage.Analyze(rpgCompilation.Sources, SmileCompilationKind.Library,
        rpgCompilation.DependencyContext);
    Equal(false, rpgAnalysis.HasErrors);
    Equal("1.1.0", rpgProject.Version);
    Equal(11, rpgProject.CompilationSources.Count);
    Equal(true, SmileBuiltInLibraryCatalog.IsBuiltIn("Smile.RPG"));
    foreach (var module in new[] { "Smile.RPG.Core", "Smile.RPG.Characters", "Smile.RPG.Party",
        "Smile.RPG.Inventory", "Smile.RPG.Equipment", "Smile.RPG.Abilities", "Smile.RPG.Shops",
        "Smile.RPG.World", "Smile.RPG.Story", "Smile.RPG.Encounters", "Smile.RPG.SaveGames" })
        Equal(true, rpgAnalysis.SemanticModel.Modules.ContainsKey(module));
    Equal(false, rpgProject.References.Any());
    Equal(false, rpgProject.CompilationSources.Any(source => File.ReadAllText(source.FullPath)
        .Contains("Game Window", StringComparison.OrdinalIgnoreCase)));
    Equal(false, rpgProject.CompilationSources.Any(source => File.ReadAllText(source.FullPath)
        .Contains("Smile.UI", StringComparison.OrdinalIgnoreCase)));

    const int phase6MaximumPayload = 12 + 4 + 32 * 13 * 4 + 4 + 8 * 4 + 4 + 4 + 64 * 2 * 4 +
        4 + 32 * 16 * 3 * 4 + 4 + 32 * 32 * 2 * 4 + 4 + 16 * 64 * 3 * 4;
    const int phase7MaximumPayload = phase6MaximumPayload + 6 * 4 + 4 + 64 * 7 * 4 +
        4 + 128 * 2 * 4 + 4 + 64 * 2 * 4 + 2 * 4 + 16 * 3 * 4;
    Equal(true, phase7MaximumPayload < 36864);
    Equal(true, phase7MaximumPayload < 1024 * 1024);
});

Run("VSIX templates render localized identity metadata within the aligned header", () =>
{
    var gameTemplate = File.ReadAllText("src/Smile.VisualStudio/Templates/Game/Program.smile");
    var consoleTemplate = File.ReadAllText("src/Smile.VisualStudio/Templates/Console/Program.smile");
    var gameManifest = File.ReadAllText("src/Smile.VisualStudio/Templates/Game/SmileGame.vstemplate");
    var consoleManifest = File.ReadAllText("src/Smile.VisualStudio/Templates/Console/SmileConsole.vstemplate");
    var gameProject = File.ReadAllText("src/Smile.VisualStudio/Templates/Game/SmileGame.smileproj");
    var consoleProject = File.ReadAllText("src/Smile.VisualStudio/Templates/Console/SmileConsole.smileproj");
    var libraryProject = File.ReadAllText("src/Smile.VisualStudio/Templates/Library/SmileLibrary.smilelibproj");
    var wizard = File.ReadAllText("src/Smile.VisualStudio/SmileProjectTemplateWizard.cs");
    var project = File.ReadAllText("src/Smile.VisualStudio/Smile.VisualStudio.csproj");
    var vsixManifest = File.ReadAllText("src/Smile.VisualStudio/source.extension.vsixmanifest");
    var gameDim = gameTemplate.IndexOf("Dim Caption As Text", StringComparison.Ordinal);
    var gameState = gameTemplate.IndexOf("Caption = \"Hello, SMILE 2.0!\"", StringComparison.Ordinal);
    var gameWindow = gameTemplate.IndexOf("Game Window \"My SMILE 2.0 Game\"", StringComparison.Ordinal);
    var gameLoop = gameTemplate.IndexOf("\nDo\n", StringComparison.Ordinal);
    Equal(true, gameDim >= 0 && gameDim < gameState && gameState < gameWindow && gameWindow < gameLoop);
    Equal(false, consoleTemplate.Contains("Game Window", StringComparison.Ordinal));
    var border = gameTemplate.Split('\n')[0].TrimEnd('\r');
    var rendered = gameTemplate.Replace("$smileuser$", "Sin".PadRight(69), StringComparison.Ordinal)
        .Replace("$smiledate$", "August 15, 2026".PadRight(69), StringComparison.Ordinal)
        .Replace("$smileversion$", "2.0.42", StringComparison.Ordinal);
    var header = rendered.Split('\n').Take(9).Select(line => line.TrimEnd('\r')).ToArray();
    Equal("' Programmed By: " + "Sin".PadRight(69) + "Version: 0.0.1", header[3]);
    Equal("' Programmed Date: " + "August 15, 2026".PadRight(69) + "SMILE: 2.0.42", header[4]);
    Equal(header[3].IndexOf("Version:", StringComparison.Ordinal) + "Version".Length,
        header[4].IndexOf("SMILE:", StringComparison.Ordinal) + "SMILE".Length);
    Equal(true, header.All(line => line.Length <= border.Length));
    foreach (var template in new[] { gameTemplate, consoleTemplate })
    {
        Equal(true, template.Contains("$smileuser$", StringComparison.Ordinal));
        Equal(true, template.Contains("$smiledate$", StringComparison.Ordinal));
        Equal(true, template.Contains("$smileversion$", StringComparison.Ordinal));
    }
    foreach (var manifest in new[] { gameManifest, consoleManifest })
    {
        Equal(true, manifest.Contains("SmileProjectTemplateWizard", StringComparison.Ordinal));
        Equal(true, manifest.Contains("Version=2.0.42.0", StringComparison.Ordinal));
    }
    foreach (var applicationProject in new[] { gameProject, consoleProject })
        Equal(true, applicationProject.Contains("<ApplicationId>$smileapplicationid$</ApplicationId>", StringComparison.Ordinal));
    Equal(false, libraryProject.Contains("ApplicationId", StringComparison.Ordinal));
    Equal(true, wizard.Contains("\"smile.app.a\" + Guid.NewGuid().ToString(\"N\")", StringComparison.Ordinal));
    Equal(true, wizard.Contains("ToString(\"D\", CultureInfo.CurrentCulture)", StringComparison.Ordinal));
    Equal(true, project.Contains("<Version>2.0.42</Version>", StringComparison.Ordinal));
    Equal(true, vsixManifest.Contains("Type=\"Microsoft.VisualStudio.Assembly\"", StringComparison.Ordinal));
});

Run("Smile.UI 1.1.3 publishes canonical Insets fields and the Phase 5.2.2 hardening", () =>
{
    var project = File.ReadAllText("libraries/Smile.UI/Smile.UI.smilelibproj");
    var core = File.ReadAllText("libraries/Smile.UI/Core.smile");
    var menu = File.ReadAllText("libraries/Smile.UI/Menu.smile");
    var navigator = File.ReadAllText("libraries/Smile.UI/MenuNavigator.smile");
    Equal(true, project.Contains("<Version>1.1.3</Version>", StringComparison.Ordinal));
    Equal(true, project.Contains("<SmileSource Include=\"MenuNavigator.smile\" />", StringComparison.Ordinal));
    foreach (var constant in new[] { "UI_EVENT_SUBMENU_OPENED", "UI_EVENT_SUBMENU_CLOSED",
        "UI_MENU_TEXT_ELLIPSIS", "UI_MENU_TEXT_CLIP", "UI_MENU_TEXT_WRAP",
        "UI_SUBMENU_INDICATOR_AFTER_TEXT", "UI_SUBMENU_INDICATOR_RIGHT_ALIGNED",
        "UI_MAX_MENU_NAVIGATORS", "UI_MAX_MENU_DEPTH", "UI_MAX_SUBMENU_BINDINGS" })
        Equal(true, core.Contains("Public Const " + constant, StringComparison.Ordinal));
    Equal(true, core.Contains("    Left As Number", StringComparison.Ordinal));
    Equal(true, core.Contains("    Right As Number", StringComparison.Ordinal));
    Equal(false, core.Contains("    LEFT As Number", StringComparison.Ordinal));
    Equal(false, core.Contains("    RIGHT As Number", StringComparison.Ordinal));
    foreach (var member in new[] { "SetItemHasSubmenu", "ItemHasSubmenu", "ItemRevision", "Bounds",
        "SetPosition", "SelectedRowRect", "ResetSelection", "DrawFocused" })
        Equal(true, menu.Contains("Public ", StringComparison.Ordinal) &&
            menu.Contains(member + "(", StringComparison.Ordinal));
    foreach (var member in new[] { "BindSubmenu", "UnbindSubmenu", "ClearBindings", "OpenSelected", "Back",
        "HandleKey", "LastAcceptedValue", "Relayout", "DrawActive", "DrawStack" })
        Equal(true, navigator.Contains(member + "(", StringComparison.Ordinal));
    Equal(true, core.Contains("ShowSubmenuIndicator As Boolean", StringComparison.Ordinal));
    Equal(true, core.Contains("SubmenuIndicatorPosition As Number", StringComparison.Ordinal));
    Equal(true, navigator.Contains("Menu.SelectedIndex(ParentHandle) <> StackParentItems[Slot, Level]", StringComparison.Ordinal));
    Equal(true, navigator.Contains("BindingIndex = FindBinding(Slot, CurrentHandle, SelectedItem)", StringComparison.Ordinal));
    Equal(true, menu.Contains("TextBlockHeight = PreparedLineCount * LineHeight", StringComparison.Ordinal));
    Equal(true, menu.Contains("CursorY = RowY + Max(0, (RowDrawHeight - MenuStyles[Slot].CursorHeight) / 2)", StringComparison.Ordinal));
    Equal(true, menu.Contains("MarkerY = DrawLabel", StringComparison.Ordinal));
    Equal(true, navigator.Contains("Call Menu.DrawFocused(StackMenus[Slot, Level], True)", StringComparison.Ordinal));
});

Run("MenuGallery uses reusable hierarchical navigation without embedded markers", () =>
{
    var gallery = File.ReadAllText("examples/MenuGallery/Program.smile");
    Equal(true, gallery.Contains("Import Smile.UI.MenuNavigator As MenuNavigator", StringComparison.Ordinal));
    Equal(true, gallery.Contains("MenuNavigator.HandleKey", StringComparison.Ordinal));
    Equal(true, gallery.Contains("MenuNavigator.DrawStack", StringComparison.Ordinal));
    Equal(true, gallery.Contains("MenuNavigator.LastAcceptedValue", StringComparison.Ordinal));
    Equal(false, gallery.Contains("MenuDepth", StringComparison.Ordinal));
    Equal(false, gallery.Split('\n').Any(line => line.Contains("Menu.AddItem", StringComparison.Ordinal) &&
        line.Contains(" >", StringComparison.Ordinal)));
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

string FormatSource(string source) =>
    SmileSourceFormatter.Format(source, formatLongIf: true, maximumLineLength: 100,
        rewriteComputedReturns: true, formatContextualIdentifiers: true, filePath: "FormatterTest.smile");

SmileAnalysisResult Analyze(string source) => SmileLanguage.Analyze(source);

SmileAnalysisResult Multi(params (string Path, bool Startup, string Text)[] sources) =>
    SmileLanguage.Analyze(sources.Select(source =>
        new SmileSourceDocument(source.Text, source.Path, source.Startup)).ToArray());

SmileResolvedSymbol ResolveSymbol(SmileAnalysisResult analysis, SyntaxTree syntaxTree, int position)
{
    if (SmileSymbolService.TryResolve(analysis, syntaxTree, position, out var symbol))
        return symbol;
    throw new InvalidOperationException($"Expected a symbol at source position {position}.");
}

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
