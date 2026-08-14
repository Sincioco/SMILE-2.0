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
    Equal(20L, SyntaxFacts.GetBuiltInConstantValue(SyntaxKind.Key3Keyword));
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
Run("Phase 4 media keywords and constants are shared", () =>
{
    Equal(SyntaxKind.ImageKeyword, SyntaxFacts.GetKeywordKind("image"));
    Equal(SyntaxKind.ClipKeyword, SyntaxFacts.GetKeywordKind("ClIp"));
    Equal(SyntaxKind.ChannelKeyword, SyntaxFacts.GetKeywordKind("channel"));
    Equal(16L, SyntaxFacts.GetBuiltInConstantValue(SyntaxKind.SoundChannelCountKeyword));
    Equal(1048576L, SyntaxFacts.GetBuiltInConstantValue(SyntaxKind.DataBlockMaxBytesKeyword));
    Equal(true, SyntaxFacts.IsKeyword(SyntaxKind.PixelKeyword));
});
Run("IMAGE works in variables arrays records parameters BYREF and returns", () =>
{
    const string source = "OPTION EXPLICIT\nTYPE Art\nPicture AS IMAGE\nEND TYPE\nDIM SourceImage AS IMAGE\nDIM Copies[2] AS IMAGE\nDIM Card AS Art\nLOAD IMAGE SourceImage FROM \"Assets\\A.png\"\nCopies[0] = SourceImage\nCard.Picture = Copies[0]\nCALL Keep(Card.Picture)\nSourceImage = CopyImage(Card.Picture)\nUNLOAD IMAGE Copies[0]\nSUB Keep(BYREF Value AS IMAGE)\nValue = Value\nEND SUB\nFUNCTION CopyImage(Value AS IMAGE) AS IMAGE\nRETURN Value\nEND FUNCTION\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    Equal(SmileType.Image, analysis.SemanticModel.Symbols["SourceImage"].Type);
    Equal(true, analysis.SemanticModel.Types["Art"].ContainsOwnedImage);
});
Run("DRAW IMAGE supports full and explicit rectangles with all Phase 4 modifiers", () =>
{
    const string source = "GAME WINDOW \"Images\" SIZE 960 BY 540\nDIM Art AS IMAGE\nDRAW IMAGE Art AT 0, 0\nDRAW IMAGE Art FROM 10, 20 SIZE 300 BY 200 AT 480, 270 SIZE 600 BY 400 OPACITY 65 ANCHOR 300, 400 FILTER PIXEL FLIP BOTH\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var draws = analysis.SyntaxTree.Root.Statements.OfType<DrawImageStatementSyntax>().ToArray();
    Equal(2, draws.Length);
    Equal(ImageFilter.Smooth, draws[0].Filter);
    Equal(ImageFilter.Pixel, draws[1].Filter);
    Equal(ImageFlip.Horizontal | ImageFlip.Vertical, draws[1].Flip);
});
Run("CLIP RECTANGLE nests and includes structured statements", () =>
{
    var analysis = Analyze("GAME WINDOW \"Clip\"\nCLIP RECTANGLE 0, 0, 100, 100\nCLIP RECTANGLE 10, 10, 40, 40\nFILL RECTANGLE 0, 0, 100, 100, WHITE\nEND CLIP\nEND CLIP\n");
    Equal(false, analysis.HasErrors);
    var outer = analysis.SyntaxTree.Root.Statements.OfType<ClipRectangleStatementSyntax>().Single();
    Equal(1, outer.Statements.OfType<ClipRectangleStatementSyntax>().Count());
});
Run("IMAGE measurement and TEXT measurement built-ins type check", () => Equal(false,
    Analyze("GAME WINDOW \"Measure\"\nDIM Art AS IMAGE\nDIM Caption AS TEXT\nPRINT IMAGE_WIDTH(Art)\nPRINT IMAGE_HEIGHT(Art)\nPRINT IMAGE_LOADED(Art)\nPRINT TEXT_WIDTH(Caption, 28)\nPRINT TEXT_HEIGHT(Caption, 28)\n").HasErrors));
Run("Persistent DATA statements accept byte arrays and writable count targets", () => Equal(false,
    Analyze("OPTION EXPLICIT\nDIM Bytes[8]\nDIM ByteCount AS NUMBER\nSAVE DATA Bytes COUNT 8 TO \"slot\"\nLOAD DATA \"slot\" INTO Bytes COUNT ByteCount\n").HasErrors));
Run("Explicit WAV channels support play per-channel stop and global stop", () =>
{
    var analysis = Analyze("GAME WINDOW \"Audio\"\nPLAY SOUND \"Assets\\One.wav\" ON CHANNEL 1\nPLAY SOUND \"Assets\\Two.wav\" ON CHANNEL 2\nSTOP SOUND ON CHANNEL 1\nSTOP SOUND\n");
    Equal(false, analysis.HasErrors);
    var sounds = analysis.SyntaxTree.Root.Statements.OfType<SoundStatementSyntax>().ToArray();
    Equal(4, sounds.Length);
    Equal(true, sounds[0].Channel != null && sounds[2].Channel != null && sounds[3].Channel == null);
});
Run("Out-of-range constant sound channels report SML3507", () => Equal(true,
    HasDiagnostic(Analyze("GAME WINDOW \"Audio\"\nPLAY SOUND \"a.wav\" ON CHANNEL 16\n"), "SML3507")));
Run("IMAGE operators report SML3509", () => Equal(true,
    HasDiagnostic(Analyze("DIM A AS IMAGE\nDIM B AS IMAGE\nPRINT A = B\n"), "SML3509")));
Run("Phase 5 TEXT inspection built-ins use Unicode scalar signatures", () =>
{
    var analysis = Analyze("DIM Value AS TEXT\nValue = \"A😀B\"\nPRINT TEXT_LENGTH(Value)\nPRINT TEXT_CODE_AT(Value, 1)\nPRINT TEXT_SLICE(Value, 1, 1)\n");
    Equal(false, analysis.HasErrors);
    var calls = analysis.BoundSyntaxTree.Root.Statements.OfType<PrintStatementSyntax>()
        .SelectMany(statement => statement.Items).OfType<CallExpressionSyntax>().ToArray();
    Equal(SmileType.Number, analysis.SemanticModel.GetType(calls.Single(call => call.Identifier.Kind == SyntaxKind.TextLengthKeyword)));
    Equal(SmileType.Number, analysis.SemanticModel.GetType(calls.Single(call => call.Identifier.Kind == SyntaxKind.TextCodeAtKeyword)));
    Equal(SmileType.Text, analysis.SemanticModel.GetType(calls.Single(call => call.Identifier.Kind == SyntaxKind.TextSliceKeyword)));
    Equal(true, HasDiagnostic(Analyze("PRINT TEXT_LENGTH(1)\n"), "SML3700"));
    Equal(true, HasDiagnostic(Analyze("PRINT TEXT_CODE_AT(\"A\", TRUE)\n"), "SML3700"));
    Equal(true, HasDiagnostic(Analyze("PRINT TEXT_SLICE(\"A\", 0, FALSE)\n"), "SML3700"));
});
Run("Phase 5.1 text literals preserve embedded and trailing newlines", () =>
{
    var analysis = Analyze("DIM Value AS TEXT\nValue = \"\nONE\nTWO\n\"\nPRINT TEXT_LENGTH(Value)\n");
    Equal(false, analysis.HasErrors);
    var windowsAnalysis = Analyze("DIM Value AS TEXT\r\nValue = \"\r\nONE\r\nTWO\r\n\"\r\nPRINT TEXT_LENGTH(Value)\r\n");
    Equal(false, windowsAnalysis.HasErrors);
    var literal = (LiteralExpressionSyntax)windowsAnalysis.SyntaxTree.Root.Statements
        .OfType<AssignmentStatementSyntax>().Single().Expression;
    Equal("\nONE\nTWO\n", (string)literal.Value);
});
Run("Phase 5 routine GAME WINDOW capabilities are direct transitive and call-site located", () =>
{
    const string module = "MODULE Test.UI\nPUBLIC SUB Draw()\nFILL RECTANGLE 0, 0, 10, 10, WHITE\nEND SUB\nPUBLIC SUB Wrapper()\nCALL Draw()\nEND SUB\nPUBLIC SUB RecursiveA()\nCALL RecursiveB()\nEND SUB\nPUBLIC SUB RecursiveB()\nCALL RecursiveA()\nCALL Draw()\nEND SUB\nPUBLIC SUB Pure()\nEND SUB\nEND MODULE\n";
    var library = SmileLanguage.Analyze(new[] { new SmileSourceDocument(module, "UI.smile") }, SmileCompilationKind.Library);
    if (library.HasErrors)
        throw new InvalidOperationException(string.Join(" | ", library.Diagnostics.Select(diagnostic => diagnostic.Code + ": " + diagnostic.Message)));
    var routines = library.SemanticModel.Routines.Values.ToArray();
    Equal(true, routines.Single(routine => routine.DisplayName == "Test.UI.Draw").RequiresGameWindow);
    Equal(true, routines.Single(routine => routine.DisplayName == "Test.UI.Wrapper").RequiresGameWindow);
    Equal(true, routines.Single(routine => routine.DisplayName == "Test.UI.RecursiveA").RequiresGameWindow);
    Equal(false, routines.Single(routine => routine.DisplayName == "Test.UI.Pure").RequiresGameWindow);

    var console = Multi(("Program.smile", true, "IMPORT Test.UI AS UI\nCALL UI.Wrapper()\nEND PROGRAM\n"),
        ("UI.smile", false, module));
    var capabilityDiagnostic = console.Diagnostics.Single(diagnostic => diagnostic.Code == "SML3704");
    Equal("Program.smile", Path.GetFileName(capabilityDiagnostic.FilePath));
    Equal(true, capabilityDiagnostic.Message.Contains("Test.UI.Wrapper", StringComparison.Ordinal));
    Equal(0, console.Diagnostics.Count(diagnostic => diagnostic.Code == "SML3023"));

    var pureConsole = Multi(("Program.smile", true, "IMPORT Test.UI AS UI\nCALL UI.Pure()\nEND PROGRAM\n"),
        ("UI.smile", false, module));
    Equal(false, pureConsole.HasErrors);
    var game = Multi(("Program.smile", true, "IMPORT Test.UI AS UI\nGAME WINDOW \"Capabilities\"\nCALL UI.Wrapper()\nEND PROGRAM\n"),
        ("UI.smile", false, module));
    Equal(false, game.HasErrors);
});
Run("Phase 5 API keyword names remain identifiers in declaration and member contexts", () =>
{
    const string core = "MODULE Context.Core\nPUBLIC TYPE Insets\nLeft AS NUMBER\nRight AS NUMBER\nEND TYPE\nPUBLIC TYPE Style\nWindow AS Insets\nText AS NUMBER\nLine AS NUMBER\nEND TYPE\nEND MODULE\n";
    const string window = "MODULE Context.Window\nIMPORT Context.Core AS UI\nPUBLIC SUB Draw(BYREF Size AS UI.Style)\nSize.Window.Left = Size.Window.Right\nSize.Text = Size.Line\nEND SUB\nEND MODULE\n";
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
    const string first = "MODULE First.Library\nPUBLIC SUB Set(BYREF Value AS NUMBER)\nValue = 1\nEND SUB\nEND MODULE\n";
    const string second = "MODULE Second.Library\nPUBLIC SUB Set(BYREF Value AS NUMBER)\nValue = 2\nEND SUB\nEND MODULE\n";
    var analysis = Multi(
        ("Program.smile", true, "IMPORT First.Library AS First\nIMPORT Second.Library AS Second\nDIM Value AS NUMBER\nCALL First.Set(Value)\nCALL Second.Set(Value)\n"),
        ("First.smile", false, first),
        ("Second.smile", false, second));
    if (analysis.HasErrors)
        throw new InvalidOperationException(string.Join(" | ", analysis.Diagnostics.Select(diagnostic => diagnostic.Code + ": " + diagnostic.Message)));
    Equal(false, analysis.HasErrors);
    Equal(true, new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, false).Emit().Contains("routine_", StringComparison.Ordinal));
    Equal(true, new WebEmitter(analysis).Emit().Contains("async function r_", StringComparison.Ordinal));
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
    Equal(true, completions.Any(completion => completion.DisplayText == "IMAGE"));
    Equal(true, completions.Any(completion => completion.DisplayText == "CLIP"));
    Equal(true, completions.Any(completion => completion.DisplayText == "IMAGE_WIDTH"));
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
    Equal("SUB Move(PlayerX AS NUMBER)", completions.Single(completion => completion.DisplayText == "Move").Description);
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
    Equal(true, javascript.Contains("smile.print([smile.booleanText(true), 42]"));
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
        File.WriteAllText(Path.Combine(directory, "Program.smile"), "END PROGRAM\n");
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
        File.WriteAllText(Path.Combine(directory, "Program.smile"), "END PROGRAM\n");
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
        File.WriteAllText(Path.Combine(directory, "Program.smile"), "END PROGRAM\n");
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
        File.WriteAllText(Path.Combine(directory, "Program.smile"), "END PROGRAM\n");
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
Run("Asset publication I/O failures report SML3604 and do not claim success", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileAssetFailureTests-" + Guid.NewGuid().ToString("N"));
    var output = Path.Combine(directory, "output");
    Directory.CreateDirectory(Path.Combine(directory, "Assets"));
    try
    {
        File.WriteAllText(Path.Combine(directory, "Program.smile"), "END PROGRAM\n");
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
        File.WriteAllText(programPath, "END PROGRAM\n");
        File.WriteAllText(supportPath, "CONST Existing = 1\n");
        File.WriteAllText(dynamicPath, "CONST Dynamic = 2\n");
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
        File.WriteAllText(programPath, "END PROGRAM\n");
        File.WriteAllText(untrackedPath, "CONST Untracked = 1\n");
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

        File.WriteAllText(missingPath, "CONST Restored = 1\n");
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

Run("Phase 3A keywords are shared and case-insensitive", () =>
{
    Equal(SyntaxKind.OptionKeyword, SyntaxFacts.GetKeywordKind("option"));
    Equal(SyntaxKind.ExplicitKeyword, SyntaxFacts.GetKeywordKind("EXPLICIT"));
    Equal(SyntaxKind.BooleanKeyword, SyntaxFacts.GetKeywordKind("Boolean"));
    Equal(SyntaxKind.ByRefKeyword, SyntaxFacts.GetKeywordKind("byref"));
    Equal(SyntaxKind.ByValKeyword, SyntaxFacts.GetKeywordKind("BYVAL"));
});
Run("OPTION EXPLICIT is physical-source scoped and enforces declarations", () =>
{
    Equal(false, Analyze("OPTION EXPLICIT\nDIM Value AS NUMBER\nValue = 1\n").HasErrors);
    Equal(true, HasDiagnostic(Analyze("OPTION EXPLICIT\nValue = 1\n"), "SML3303"));
    Equal(true, HasDiagnostic(Analyze("Value = 1\nOPTION EXPLICIT\n"), "SML3300"));
    Equal(true, HasDiagnostic(Analyze("OPTION EXPLICIT\nOPTION EXPLICIT\n"), "SML3300"));
    var scoped = Multi(
        ("Program.smile", true, "OPTION EXPLICIT\nDIM Value AS NUMBER\nValue = 1\n"),
        ("Support.smile", false, "SUB Legacy()\nImplicit = 2\nEND SUB\n"));
    Equal(false, scoped.HasErrors);
});
Run("Typed scalars arrays and legacy numeric arrays bind shared types", () =>
{
    var analysis = Analyze("OPTION EXPLICIT\nDIM Score AS NUMBER\nDIM Alive AS BOOLEAN\nDIM Name AS TEXT\nDIM Flags[2] AS BOOLEAN\nDIM Names[3] AS TEXT\nDIM Legacy[4]\n");
    Equal(false, analysis.HasErrors);
    Equal(SmileType.Number, analysis.SemanticModel.Symbols["Score"].Type);
    Equal(SmileType.Boolean, analysis.SemanticModel.Symbols["Alive"].Type);
    Equal(SmileType.Text, analysis.SemanticModel.Symbols["Name"].Type);
    Equal(SmileType.Boolean, analysis.SemanticModel.Symbols["Flags"].Type);
    Equal(SmileType.Text, analysis.SemanticModel.Symbols["Names"].Type);
    Equal(SmileType.Number, analysis.SemanticModel.Symbols["Legacy"].Type);
    Equal(true, HasDiagnostic(Analyze("DIM MissingType\n"), "SML3302"));
    Equal(true, HasDiagnostic(Analyze("DIM Value AS STRING\n"), "SML3401"));
});
Run("TEXT constants values operators arrays SELECT and DRAW bind", () =>
{
    const string source = "OPTION EXPLICIT\nCONST Greeting = \"Hello, \" + \"SMILE\"\nDIM Name AS TEXT\nDIM Copy AS TEXT\nDIM Names[2] AS TEXT\nDIM Same AS BOOLEAN\nName = Greeting\nCopy = Name\nNames[0] = Copy\nSame = Name = Copy\nSELECT CASE Name\nCASE \"Hello, SMILE\"\nPRINT Names[0]\nCASE ELSE\nPRINT \"NO\"\nEND SELECT\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    Equal("Hello, SMILE", analysis.SemanticModel.Symbols["Greeting"].ConstantValue);
    Equal(SmileType.Text, analysis.SemanticModel.Symbols["Names"].Type);
    Equal(true, HasDiagnostic(Analyze("DIM TextValue AS TEXT\nTextValue = \"x\" + 1\n"), "SML3308"));
    Equal(true, HasDiagnostic(Analyze("DIM A AS TEXT\nDIM B AS TEXT\nPRINT A < B\n"), "SML3308"));
    Equal(false, Analyze("GAME WINDOW \"Text\"\nDIM Caption AS TEXT\nCaption = \"Ready\"\nDRAW TEXT Caption AT 10, 20 SIZE 16 COLOR WHITE\n").HasErrors);
});
Run("Typed routines default BYVAL and validate BYREF writable locations", () =>
{
    const string source = "OPTION EXPLICIT\nDIM Name AS TEXT\nName = \"Before\"\nCALL Rename(Name, \"After\")\nSUB Rename(BYREF Value AS TEXT, BYVAL Replacement AS TEXT)\nValue = Replacement\nEND SUB\nFUNCTION IsEmpty(Value AS TEXT) AS BOOLEAN\nRETURN Value = \"\"\nEND FUNCTION\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var rename = analysis.SemanticModel.Routines.Values.Single(routine => routine.Name == "Rename");
    Equal(ParameterPassingMode.ByRef, rename.Parameters[0].ParameterMode);
    Equal(ParameterPassingMode.ByVal, rename.Parameters[1].ParameterMode);
    Equal(SmileType.Text, rename.Parameters[0].Type);
    Equal(SmileType.Boolean, analysis.SemanticModel.Routines.Values.Single(routine => routine.Name == "IsEmpty").ReturnType);
    Equal(true, HasDiagnostic(Analyze("SUB Set(BYREF Value AS NUMBER)\nValue = 1\nEND SUB\nCALL Set(5)\n"), "SML3305"));
    Equal(true, HasDiagnostic(Analyze("CONST Fixed = 1\nSUB Set(BYREF Value AS NUMBER)\nValue = 1\nEND SUB\nCALL Set(Fixed)\n"), "SML3305"));
});
Run("Legacy numeric parameters accept Boolean values compatibly", () =>
{
    const string source = "PRINT Legacy(TRUE)\nFUNCTION Legacy(Value)\nRETURN Value = 1\nEND FUNCTION\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    Equal(false, analysis.SemanticModel.Routines.Values.Single().Parameters[0].HasDeclaredType);
    Equal(true, new WebEmitter(analysis).Emit().Contains("? 1 : 0", StringComparison.Ordinal));
    Equal(true, HasDiagnostic(Analyze("PRINT Typed(TRUE)\nFUNCTION Typed(Value AS NUMBER) AS BOOLEAN\nRETURN Value = 1\nEND FUNCTION\n"), "SML3304"));
});
Run("Routine calls support sixteen typed parameters", () =>
{
    const string parameters = "Value1 AS NUMBER, Value2 AS NUMBER, Value3 AS NUMBER, Value4 AS NUMBER, Value5 AS NUMBER, Value6 AS NUMBER, Value7 AS NUMBER, Value8 AS NUMBER, Value9 AS NUMBER, Value10 AS NUMBER, Value11 AS NUMBER, Value12 AS NUMBER, Value13 AS NUMBER, Value14 AS NUMBER, Value15 AS NUMBER, Value16 AS NUMBER";
    var analysis = Analyze($"PRINT Sum16(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16)\nFUNCTION Sum16({parameters}) AS NUMBER\nRETURN Value1 + Value2 + Value3 + Value4 + Value5 + Value6 + Value7 + Value8 + Value9 + Value10 + Value11 + Value12 + Value13 + Value14 + Value15 + Value16\nEND FUNCTION\n");
    Equal(false, analysis.HasErrors);
    Equal(16, analysis.SemanticModel.Routines.Values.Single().Parameters.Count);
    Equal(true, new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, false).Emit().Contains("[rbp+136]", StringComparison.Ordinal));
});
Run("Routine-local DIM shadows globals and diagnoses duplicate and early use", () =>
{
    var shadow = Analyze("DIM Value AS NUMBER\nSUB Work()\nDIM Value AS TEXT\nValue = \"local\"\nPRINT Value\nEND SUB\nValue = 1\nCALL Work()\n");
    Equal(false, shadow.HasErrors);
    Equal(SmileType.Text, shadow.SemanticModel.Routines.Values.Single().LocalSymbols["Value"].Type);
    Equal(true, HasDiagnostic(Analyze("SUB Work()\nDIM Value AS NUMBER\nDIM Value AS TEXT\nEND SUB\n"), "SML3306"));
    Equal(true, HasDiagnostic(Analyze("SUB Work()\nPRINT Value\nDIM Value AS NUMBER\nEND SUB\n"), "SML3307"));
});
Run("Legacy function inference checks all return types", () =>
{
    Equal(true, HasDiagnostic(Analyze("PRINT Mixed(TRUE)\nFUNCTION Mixed(Flag AS BOOLEAN)\nIF Flag THEN\nRETURN \"text\"\nELSE\nRETURN 1\nEND IF\nEND FUNCTION\n"), "SML3309"));
});
Run("Web emitter uses JavaScript TEXT values and BYREF references", () =>
{
    var analysis = Analyze("DIM Name AS TEXT\nName = \"A\"\nCALL Replace(Name, \"B\")\nPRINT Name\nSUB Replace(BYREF Value AS TEXT, NewValue AS TEXT)\nValue = NewValue\nEND SUB\n");
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
            Equal(true, api.Contains("\"type\": \"TEXT\"", StringComparison.Ordinal));
            Equal(true, api.Contains("\"mode\": \"ByRef\"", StringComparison.Ordinal));
            Equal(true, api.Contains("\"mode\": \"ByVal\"", StringComparison.Ordinal));
            Equal(true, api.Contains("\"returnType\": \"TEXT\"", StringComparison.Ordinal));
            Equal(false, api.Contains("Hidden", StringComparison.Ordinal));
        }
        RewriteManifest(first, manifest => manifest.Replace("\"formatVersion\": 5", "\"formatVersion\": 4",
            StringComparison.Ordinal));
        ThrowsContains(() => SmileLibraryPackage.ReadIdentity(first), "rebuild the library");
    }
    finally { Directory.Delete(directory, true); }
});
Run("FormatVersion 5 packages preserve direct and transitive GAME WINDOW capabilities", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmilePhase5CapabilityPackageTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var projectPath = Path.Combine(directory, "Capability.smilelibproj");
        File.WriteAllText(projectPath,
            "<SmileProject Version=\"1.0\"><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Capability.Proof</LibraryName><Version>1.0.0</Version><OutputName>Capability</OutputName></PropertyGroup><ItemGroup><SmileSource Include=\"Capability.smile\" /></ItemGroup></SmileProject>");
        File.WriteAllText(Path.Combine(directory, "Capability.smile"),
            "MODULE Capability.Proof\nPUBLIC SUB Draw()\nFILL RECTANGLE 0, 0, 1, 1, WHITE\nEND SUB\nPUBLIC SUB Wrapper()\nCALL Draw()\nEND SUB\nPUBLIC SUB Pure()\nEND SUB\nEND MODULE\n");
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
    const string source = "SUB Rename(BYREF Name AS TEXT, NewName AS TEXT)\nName = NewName\nEND SUB\nFUNCTION Join(First AS TEXT, Second AS TEXT) AS TEXT\nRETURN First + Second\nEND FUNCTION\nPRINT Ren";
    var completions = SmileCompletionService.GetCompletions(Analyze(source), source.Length);
    Equal("SUB Rename(BYREF Name AS TEXT, NewName AS TEXT)",
        completions.Single(item => item.DisplayText == "Rename").Description);
    Equal("FUNCTION Join(First AS TEXT, Second AS TEXT) AS TEXT",
        completions.Single(item => item.DisplayText == "Join").Description);
    const string typedDeclaration = "DIM Name AS ";
    Equal("BOOLEAN|IMAGE|NUMBER|TEXT", string.Join("|", SmileCompletionService
        .GetCompletions(Analyze(typedDeclaration), typedDeclaration.Length).Select(item => item.DisplayText)));
});

Run("FormatVersion 5 public API metadata preserves IMAGE signatures", () =>
{
    var root = Path.Combine(Path.GetTempPath(), "SmilePhase4ImagePackageTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    try
    {
        var projectPath = Path.Combine(root, "Media.smilelibproj");
        File.WriteAllText(projectPath,
            "<SmileProject Version=\"1.0\"><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Smile.Media.Proof</LibraryName><Version>1.0.0</Version><OutputName>Media</OutputName></PropertyGroup><ItemGroup><SmileSource Include=\"Media.smile\" /></ItemGroup></SmileProject>");
        File.WriteAllText(Path.Combine(root, "Media.smile"),
            "MODULE Smile.Media.Proof\nPUBLIC FUNCTION Ready(Value AS IMAGE) AS BOOLEAN\nRETURN IMAGE_LOADED(Value)\nEND FUNCTION\nEND MODULE\n");
        var compilation = SmileProjectCompilation.Load(projectPath, Path.Combine(root, "cache"));
        var analysis = SmileLanguage.Analyze(compilation.Sources, SmileCompilationKind.Library,
            compilation.DependencyContext);
        Equal(false, analysis.HasErrors);
        var package = Path.Combine(root, "Media.smilelib");
        SmileLibraryPackage.Write(package, compilation.Graph.Root, analysis);
        using var archive = System.IO.Compression.ZipFile.OpenRead(package);
        using var reader = new StreamReader(archive.GetEntry("api/public-symbols.json")!.Open());
        var api = reader.ReadToEnd();
        Equal(true, api.Contains("\"type\": \"IMAGE\"", StringComparison.Ordinal));
        Equal(true, api.Contains("\"returnType\": \"BOOLEAN\"", StringComparison.Ordinal));
    }
    finally { Directory.Delete(root, true); }
});

Run("IMAGE ownership emits retain release move and record cleanup on both targets", () =>
{
    const string source = "TYPE Media\nArt AS IMAGE\nEND TYPE\nDIM SourceImage AS IMAGE\nDIM Copy AS IMAGE\nDIM Items[2] AS IMAGE\nDIM Card AS Media\nCopy = SourceImage\nItems[0] = Copy\nCard.Art = Items[0]\nSourceImage = Card.Art\n";
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

Run("Web IMAGE reads are owned and calls transfer without an extra retain", () =>
{
    const string source = "GAME WINDOW \"Display title\" SIZE 320 BY 180\nDIM Shared AS IMAGE\nDIM Copy AS IMAGE\nCopy = GetImage()\nPRINT IMAGE_WIDTH(GetImage())\nDRAW IMAGE GetImage() AT 0, 0\nFUNCTION GetImage() AS IMAGE\nRETURN Shared\nEND FUNCTION\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var web = new WebEmitter(analysis, "Stable.Output", new[] { "Assets/Hero.png" }).Emit();
    Equal(true, web.Contains("smile.configure(\"Stable.Output\", [\"Assets/Hero.png\"])", StringComparison.Ordinal));
    Equal(true, web.Contains("return smile.imageRetain(g_0_shared);", StringComparison.Ordinal));
    Equal(false, web.Contains("smile.imageAssign", StringComparison.Ordinal));
    Equal(true, web.Contains("smile.imageWidth(await", StringComparison.Ordinal));
    Equal(true, web.Contains("smile.drawImage(await", StringComparison.Ordinal));
});

Run("Structured clips emit balanced cleanup for RETURN loop exits and END PROGRAM", () =>
{
    const string source = "GAME WINDOW \"Clip cleanup\"\nCALL Leave()\nFOR Index = 0 TO 1\nCLIP RECTANGLE 0, 0, 20, 20\nEXIT FOR\nEND CLIP\nEND FOR\nDO\nCLIP RECTANGLE 0, 0, 20, 20\nEXIT DO\nEND CLIP\nLOOP\nCLIP RECTANGLE 0, 0, 20, 20\nEND PROGRAM\nEND CLIP\nSUB Leave()\nCLIP RECTANGLE 0, 0, 20, 20\nRETURN\nEND CLIP\nEND SUB\n";
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
    const string source = "TYPE Point\nX AS NUMBER\nY AS NUMBER\nEND TYPE\nTYPE Actor\nName AS TEXT\nPosition AS Point\nActive AS BOOLEAN\nEND TYPE\nDIM Hero AS Actor\nDIM Party[2, 2] AS Actor\nHero.Position.X = 7\nParty[1, 1] = Hero\n";
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
    const string source = "TYPE Item\nName AS TEXT\nValue AS NUMBER\nEND TYPE\nDIM First AS Item\nDIM Copy AS Item\nDIM Items[2] AS Item\nFirst.Name = \"A\"\nCopy = First\nFirst = First\nItems[0] = Copy\nPRINT Items[0].Name\n";
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
    const string parameters = "Value1 AS NUMBER, Value2 AS NUMBER, Value3 AS NUMBER, Value4 AS NUMBER, Value5 AS NUMBER, Value6 AS NUMBER, Value7 AS NUMBER, Value8 AS NUMBER, Value9 AS NUMBER, Value10 AS NUMBER, Value11 AS NUMBER, Value12 AS NUMBER, Value13 AS NUMBER, Value14 AS NUMBER, Value15 AS NUMBER, Value16 AS NUMBER";
    var analysis = Analyze($"TYPE Result\nValue AS NUMBER\nEND TYPE\nDIM Answer AS Result\nAnswer = Make(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16)\nFUNCTION Make({parameters}) AS Result\nDIM Value AS Result\nValue.Value = Value16\nRETURN Value\nEND FUNCTION\n");
    Equal(false, analysis.HasErrors);
    var assembly = new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, false).Emit();
    Equal(true, assembly.Contains("[rbp+144]", StringComparison.Ordinal));
    Equal(true, assembly.Contains("record_result", StringComparison.Ordinal));
});

Run("Record type and field completion shares public visibility and nested type information", () =>
{
    const string fieldSource = "TYPE Point\nX AS NUMBER\nY AS NUMBER\nEND TYPE\nDIM Hero AS Point\nPRINT Hero.";
    var fieldCompletions = SmileCompletionService.GetCompletions(Analyze(fieldSource), fieldSource.Length);
    Equal("X|Y", string.Join("|", fieldCompletions.Select(item => item.DisplayText)));
    Equal(true, fieldCompletions.All(item => item.Kind == SmileCompletionKind.Field));

    const string typeSource = "TYPE Point\nX AS NUMBER\nEND TYPE\nDIM Hero AS ";
    var typeCompletions = SmileCompletionService.GetCompletions(Analyze(typeSource), typeSource.Length);
    Equal(true, typeCompletions.Any(item => item.DisplayText == "Point" && item.Kind == SmileCompletionKind.Type));

    const string program = "IMPORT Example.Models AS Models\nDIM Value AS Models.";
    var imported = Multi(("Program.smile", true, program),
        ("Models.smile", false, "MODULE Example.Models\nPUBLIC TYPE Visible\nValue AS NUMBER\nEND TYPE\nPRIVATE TYPE Hidden\nValue AS NUMBER\nEND TYPE\nEND MODULE\n"));
    var importedCompletions = SmileCompletionService.GetCompletions(imported, program.Length);
    Equal(true, importedCompletions.Any(item => item.DisplayText == "Visible"));
    Equal(false, importedCompletions.Any(item => item.DisplayText == "Hidden"));
    Equal(false, importedCompletions.Any(item => item.Kind is SmileCompletionKind.Variable or SmileCompletionKind.Function));
});

Run("Record diagnostics cover declarations fields cycles visibility operations and BYREF", () =>
{
    Equal(true, HasDiagnostic(Analyze("TYPE A\nX AS NUMBER\nEND TYPE\nTYPE A\nY AS NUMBER\nEND TYPE\n"), "SML3400"));
    Equal(true, HasDiagnostic(Analyze("TYPE A\nX AS Missing\nEND TYPE\n"), "SML3401"));
    Equal(true, HasDiagnostic(Analyze("TYPE A\nX AS NUMBER\nX AS NUMBER\nEND TYPE\n"), "SML3402"));
    Equal(true, HasDiagnostic(Analyze("SUB Work()\nTYPE A\nX AS NUMBER\nEND TYPE\nEND SUB\n"), "SML3403"));
    Equal(true, HasDiagnostic(Analyze("TYPE A\nNext AS A\nEND TYPE\n"), "SML3404"));
    Equal(true, HasDiagnostic(Analyze("TYPE A\nX AS NUMBER\nEND TYPE\nDIM Value AS A\nPRINT Value.Y\n"), "SML3405"));
    Equal(true, HasDiagnostic(Analyze("DIM Value AS NUMBER\nPRINT Value.X\n"), "SML3406"));
    Equal(true, HasDiagnostic(Analyze("TYPE A\nX AS NUMBER\nEND TYPE\nDIM Value AS A\nPRINT Value\n"), "SML3407"));
    Equal(true, HasDiagnostic(Analyze("TYPE A\nX AS NUMBER\nEND TYPE\nCALL Change(Create())\nFUNCTION Create() AS A\nDIM Result AS A\nRETURN Result\nEND FUNCTION\nSUB Change(BYREF Value AS A)\nEND SUB\n"), "SML3305"));
});

Run("Routine compiler temporaries have distinct invocation-local frame storage", () =>
{
    const string source = "OPTION EXPLICIT\nCALL Work(2)\nSUB Work(Level AS NUMBER)\nDIM Index AS NUMBER\nDIM Values[2] AS TEXT\nFOR Index = 1 TO Level\nSELECT CASE Level\nCASE 1\nPRINT Index\nEND SELECT\nSELECT CASE TRUE\nCASE TRUE\nPRINT Index\nEND SELECT\nSELECT CASE \"X\" + \"\"\nCASE \"X\"\nPRINT Values[0]\nEND SELECT\nEND FOR\nEND SUB\n";
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

Run("Owned TEXT selector cleanup precedes RETURN and loop exits", () =>
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
    const string module = "MODULE Example.Isolated\nPUBLIC TYPE Wrapper\nValue AS ConsumerData\nEND TYPE\nPUBLIC DIM Shared AS ConsumerData\nPUBLIC FUNCTION Copy(Value AS ConsumerData) AS ConsumerData\nRETURN Value\nEND FUNCTION\nEND MODULE\n";
    var withGlobal = Multi(
        ("Program.smile", true, "TYPE ConsumerData\nValue AS NUMBER\nEND TYPE\nPRINT 1\n"),
        ("Library.smile", false, module));
    var withoutGlobal = Multi(
        ("Program.smile", true, "PRINT 1\n"),
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
        new SmileSourceDocument("MODULE Example.Shared\nPUBLIC TYPE Component\nValue AS NUMBER\nEND TYPE\nPUBLIC TYPE Container\nItem AS Component\nEND TYPE\nEND MODULE\n", "Types.smile"),
        new SmileSourceDocument("MODULE Example.Shared\nPUBLIC DIM Values[2] AS Container\nPUBLIC FUNCTION Copy(Value AS Container) AS Container\nDIM Local AS Container\nLocal = Value\nRETURN Local\nEND FUNCTION\nEND MODULE\n", "Factories.smile")
    }, SmileCompilationKind.Library);
    Equal(false, analysis.HasErrors);
    Equal("Container", analysis.SemanticModel.Modules["Example.Shared"].Members["Values"].Variable!.Type.Name);
});

Run("Record completion separates type value alias and indexed-field contexts", () =>
{
    const string moduleTypes = "MODULE Example.Models\nPUBLIC TYPE Position\nX AS NUMBER\nY AS NUMBER\nEND TYPE\nPUBLIC TYPE Actor\nName AS TEXT\nPosition AS Position\nEND TYPE\nPUBLIC DIM DefaultActor AS Actor\nPUBLIC FUNCTION Create() AS Actor\nDIM Value AS Actor\nRETURN Value\nEND FUNCTION\nEND MODULE\n";

    const string crossFile = "MODULE Example.Models\nPUBLIC DIM Value AS \nEND MODULE\n";
    var crossAnalysis = Multi(
        ("Program.smile", true, "TYPE ConsumerOnly\nValue AS NUMBER\nEND TYPE\nPRINT 1\n"),
        ("Types.smile", false, moduleTypes), ("Use.smile", false, crossFile));
    var crossTypes = SmileCompletionService.GetCompletions(crossAnalysis, "Use.smile",
        crossFile.IndexOf("\nEND MODULE", StringComparison.Ordinal));
    Equal(true, crossTypes.Any(item => item.DisplayText == "Actor" && item.Kind == SmileCompletionKind.Type));
    Equal(false, crossTypes.Any(item => item.DisplayText == "ConsumerOnly"));

    const string valueProgram = "IMPORT Example.Models AS Models\nPRINT Models.";
    var valueAnalysis = Multi(("Program.smile", true, valueProgram), ("Models.smile", false, moduleTypes));
    var aliasValues = SmileCompletionService.GetCompletions(valueAnalysis, valueProgram.Length);
    Equal(true, aliasValues.Any(item => item.DisplayText == "DefaultActor"));
    Equal(true, aliasValues.Any(item => item.DisplayText == "Create"));
    Equal(false, aliasValues.Any(item => item.Kind == SmileCompletionKind.Type));

    const string typeProgram = "IMPORT Example.Models AS Models\nDIM Value AS Models.";
    var typeAnalysis = Multi(("Program.smile", true, typeProgram), ("Models.smile", false, moduleTypes));
    var aliasTypes = SmileCompletionService.GetCompletions(typeAnalysis, typeProgram.Length);
    Equal("Actor|Position", string.Join("|", aliasTypes.Select(item => item.DisplayText)));
    Equal(true, aliasTypes.All(item => item.Kind == SmileCompletionKind.Type));
    Equal(false, SmileCompletionService.GetCompletions(Analyze("TYPE Local\nValue AS NUMBER\nEND TYPE\nPRINT "), 49)
        .Any(item => item.Kind == SmileCompletionKind.Type));

    const string records = "TYPE Position\nX AS NUMBER\nY AS NUMBER\nEND TYPE\nTYPE Actor\nName AS TEXT\nPosition AS Position\nEND TYPE\nDIM Party[4] AS Actor\nDIM Grid[2, 2] AS Actor\n";
    foreach (var expression in new[] { "Party[Index + 1].", "Grid[X, Y]." })
    {
        var source = records + "PRINT " + expression;
        var fields = SmileCompletionService.GetCompletions(Analyze(source), source.Length);
        Equal("Name|Position", string.Join("|", fields.Select(item => item.DisplayText)));
    }
    var nestedSource = records + "PRINT Party[Index + 1].Position.";
    Equal("X|Y", string.Join("|", SmileCompletionService.GetCompletions(Analyze(nestedSource), nestedSource.Length)
        .Select(item => item.DisplayText)));
    var importedFieldSource = "IMPORT Example.Models AS Models\nPRINT Models.DefaultActor.";
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
        const string sourceText = "MODULE Example.Models\nPUBLIC TYPE Actor\nName AS TEXT\nEND TYPE\nEND MODULE\n";
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
            "MODULE Example.Base\nPUBLIC TYPE Point\nX AS NUMBER\nEND TYPE\nEND MODULE\n");
        File.WriteAllText(Path.Combine(consumerRoot, "Consumer.smilelibproj"),
            "<SmileProject Version=\"1.0\"><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Example.ConsumerProvider</LibraryName><Version>2.0.0</Version><OutputName>Consumer</OutputName></PropertyGroup><ItemGroup><SmileSource Include=\"Types.smile\" /><SmileProjectReference Include=\"..\\Base\\Base.smilelibproj\" /></ItemGroup></SmileProject>");
        File.WriteAllText(Path.Combine(consumerRoot, "Types.smile"),
            "MODULE Example.Consumer\nIMPORT Example.Base AS Base\nPUBLIC TYPE Wrapper\nValue AS Base.Point\nEND TYPE\nPUBLIC FUNCTION Copy(Value AS Base.Point) AS Base.Point\nRETURN Value\nEND FUNCTION\nEND MODULE\n");

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
