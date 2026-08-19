using System.Diagnostics;
using System.Xml.Linq;
using Smile.Compiler;
using Smile.Language;

Environment.CurrentDirectory = RepositoryTestContext.FindRepositoryRoot();
var tests = new TestContext();

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
Run("Comment Selection comments every selected nonblank SMILE line", () =>
{
    const string source = "If True Then\r\n    Print \"A\"\r\n\r\nEnd If\r\nPrint \"B\"\r\n";
    var selectionLength = source.IndexOf("Print \"B\"", StringComparison.Ordinal);
    var result = ApplyCommentEdits(source,
        SmileCommentService.GetEdits(source, 0, selectionLength, SmileCommentMode.Comment));
    Equal("'If True Then\r\n    'Print \"A\"\r\n\r\n'End If\r\nPrint \"B\"\r\n", result);
});
Run("Uncomment Selection exactly restores editor-generated comments", () =>
{
    const string commented = "'If True Then\n    'Print \"A\"\n'End If\n";
    var result = ApplyCommentEdits(commented,
        SmileCommentService.GetEdits(commented, 0, commented.Length, SmileCommentMode.Uncomment));
    Equal("If True Then\n    Print \"A\"\nEnd If\n", result);
});
Run("Toggle Line Comment comments mixed selections and then uncomments them", () =>
{
    const string mixed = "'Dim A As Number\nDim B As Number\n";
    var commented = ApplyCommentEdits(mixed,
        SmileCommentService.GetEdits(mixed, 0, mixed.Length, SmileCommentMode.Toggle));
    Equal("''Dim A As Number\n'Dim B As Number\n", commented);
    var restored = ApplyCommentEdits(commented,
        SmileCommentService.GetEdits(commented, 0, commented.Length, SmileCommentMode.Toggle));
    Equal(mixed, restored);
});
Run("Comment Selection with an empty selection comments the caret line", () =>
{
    const string source = "Dim A As Number\n    Dim B As Number\n";
    var caret = source.IndexOf("B As", StringComparison.Ordinal);
    var result = ApplyCommentEdits(source,
        SmileCommentService.GetEdits(source, caret, 0, SmileCommentMode.Comment));
    Equal("Dim A As Number\n    'Dim B As Number\n", result);
});
Run("Comment commands round-trip complete and partial lightweight OOP block selections", () =>
{
    const string source = "Class Paladin\n" +
                          "    Public Sub New(Optional Rank As Number = 1)\n" +
                          "        Me.Level = Rank\n" +
                          "    End Sub\n" +
                          "    Public Property Power As Number\n" +
                          "        Get\n" +
                          "            Return Me.Level\n" +
                          "        End Get\n" +
                          "    End Property\n" +
                          "End Class\n";
    var selectionStart = source.IndexOf("Public Sub", StringComparison.Ordinal) + 3;
    var selectionEnd = source.IndexOf("End Property", StringComparison.Ordinal) + "End Prop".Length;
    var commented = ApplyCommentEdits(source, SmileCommentService.GetEdits(source, selectionStart,
        selectionEnd - selectionStart, SmileCommentMode.Comment));
    Equal(true, commented.StartsWith("Class Paladin\n", StringComparison.Ordinal));
    Equal(true, commented.Contains("    'Public Sub New", StringComparison.Ordinal));
    Equal(true, commented.Contains("    'End Property", StringComparison.Ordinal));
    Equal(true, commented.EndsWith("End Class\n", StringComparison.Ordinal));

    var commentedStart = commented.IndexOf("Public Sub", StringComparison.Ordinal) + 2;
    var commentedEnd = commented.IndexOf("End Property", StringComparison.Ordinal) + "End Proper".Length;
    var restored = ApplyCommentEdits(commented, SmileCommentService.GetEdits(commented, commentedStart,
        commentedEnd - commentedStart, SmileCommentMode.Uncomment));
    Equal(source, restored);

    var toggled = ApplyCommentEdits(source, SmileCommentService.GetEdits(source, selectionStart,
        selectionEnd - selectionStart, SmileCommentMode.Toggle));
    var toggleStart = toggled.IndexOf("Public Sub", StringComparison.Ordinal) + 1;
    var toggleEnd = toggled.IndexOf("End Property", StringComparison.Ordinal) + "End Property".Length;
    var toggledBack = ApplyCommentEdits(toggled, SmileCommentService.GetEdits(toggled, toggleStart,
        toggleEnd - toggleStart, SmileCommentMode.Toggle));
    Equal(source, toggledBack);
});
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
    Equal(21L, SyntaxFacts.GetBuiltInConstantValue(SyntaxKind.KeyTabKeyword));
    Equal(22L, SyntaxFacts.GetBuiltInConstantValue(SyntaxKind.Key4Keyword));
});
Run("Left and Right prefer identifiers only in assignment-target context", () =>
{
    const string source = "Option Explicit\nDim Left As Number\nDim Right As Number\nLeft = 10\nRight = 20\nPrint LEFT\nPrint RIGHT\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var assignments = analysis.SyntaxTree.Root.Statements.OfType<AssignmentStatementSyntax>().ToArray();
    Equal(2, assignments.Length);
    Equal(true, assignments.All(statement => statement.Target.Location is NameExpressionSyntax));
    Equal(SyntaxKind.LeftKeyword,
        ((NameExpressionSyntax)assignments[0].Target.Location).Identifier.Kind);
    Equal(SyntaxKind.RightKeyword,
        ((NameExpressionSyntax)assignments[1].Target.Location).Identifier.Kind);
    var constants = analysis.SyntaxTree.Root.Statements.OfType<PrintStatementSyntax>()
        .Select(statement => statement.Items.Single()).ToArray();
    Equal(SyntaxKind.LeftKeyword, ((LiteralExpressionSyntax)constants[0]).LiteralToken.Kind);
    Equal(SyntaxKind.RightKeyword, ((LiteralExpressionSyntax)constants[1]).LiteralToken.Kind);
});
Run("KEY_4 is a shared named input constant", () =>
{
    Equal(SyntaxKind.Key4Keyword, SyntaxFacts.GetKeywordKind("key_4"));
    Equal(22L, SyntaxFacts.GetBuiltInConstantValue(SyntaxKind.Key4Keyword));
});
Run("KEY_TAB is a shared named input constant", () =>
{
    Equal(SyntaxKind.KeyTabKeyword, SyntaxFacts.GetKeywordKind("key_tab"));
    Equal(21L, SyntaxFacts.GetBuiltInConstantValue(SyntaxKind.KeyTabKeyword));
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
        var expectedNames = new[] { "index.html", "smile-runtime.js", "game.js", "smile.css" };
        var analysis = Analyze("Game Window \"Test\"\nShow Screen\nEnd Program\n");
        WebOutputWriter.Write(directory, new WebEmitter(analysis));
        Equal(true, WebOutputWriter.ManagedFileNames.SequenceEqual(expectedNames));
        Equal(expectedNames.Length, Directory.EnumerateFiles(directory).Count());
        foreach (var name in expectedNames)
            Equal(true, File.Exists(Path.Combine(directory, name)));

        var html = File.ReadAllText(Path.Combine(directory, "index.html"));
        var css = File.ReadAllText(Path.Combine(directory, "smile.css"));
        var runtime = File.ReadAllText(Path.Combine(directory, "smile-runtime.js"));
        Equal(true, html.Contains("width=device-width, initial-scale=1, viewport-fit=cover", StringComparison.Ordinal));
        Equal(1, html.Split(new[] { "id=\"smile-controls\"" }, StringSplitOptions.None).Length - 1);
        Equal(true, html.Contains("id=\"smile-controls\" hidden aria-hidden=\"true\"", StringComparison.Ordinal));
        foreach (var control in new[] { "up", "down", "left", "right", "a", "b", "x", "y" })
            Equal(1, html.Split(new[] { $"data-smile-control=\"{control}\"" }, StringSplitOptions.None).Length - 1);
        foreach (var removedControl in new[] { "one", "two", "three", "four" })
            Equal(false, html.Contains($"data-smile-control=\"{removedControl}\"", StringComparison.Ordinal));
        Equal(8, html.Split(new[] { "type=\"button\"" }, StringSplitOptions.None).Length - 1);
        Equal(4, html.Split(new[] { "<span aria-hidden=\"true\">▲</span>" }, StringSplitOptions.None).Length - 1);
        Equal(false, html.Contains("◀", StringComparison.Ordinal));
        Equal(false, html.Contains("▶", StringComparison.Ordinal));
        Equal(true, css.Contains("#smile-controls[hidden] { display: none; }", StringComparison.Ordinal));
        Equal(true, css.Contains("#smile-controls button { pointer-events: auto; touch-action: none;", StringComparison.Ordinal));
        Equal(true, css.Contains("border: 2px solid rgba(220, 247, 255, .56)", StringComparison.Ordinal));
        Equal(true, css.Contains("height: 100dvh", StringComparison.Ordinal));
        Equal(true, css.Contains("#smile-shell.smile-controls-visible { display: flex; flex-direction: column; justify-content: center;", StringComparison.Ordinal));
        Equal(true, css.Contains(".smile-control-left span { transform: rotate(-90deg); }", StringComparison.Ordinal));
        Equal(false, css.Contains("#smile-canvas { touch-action: none", StringComparison.Ordinal));
        Equal(false, html.Contains("user-scalable=no", StringComparison.Ordinal));
        Equal(true, runtime.Contains("new URLSearchParams(search).getAll(\"smile-controls\")", StringComparison.Ordinal));
        Equal(true, runtime.Contains("shell.classList.toggle(\"smile-controls-visible\", next)", StringComparison.Ordinal));
        Equal(true, runtime.Contains("a: 14, b: 16, x: 16, y: 21", StringComparison.Ordinal));
        Equal(false, runtime.Contains("userAgent", StringComparison.Ordinal));
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
Run("Asset publication stages replacements and rolls back a failed commit", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileAssetRollbackTests-" + Guid.NewGuid().ToString("N"));
    var output = Path.Combine(directory, "output");
    Directory.CreateDirectory(Path.Combine(directory, "Assets"));
    Directory.CreateDirectory(output);
    try
    {
        File.WriteAllText(Path.Combine(directory, "Program.smile"), "End Program\n");
        var assetPath = Path.Combine(directory, "Assets", "Current.txt");
        File.WriteAllText(assetPath, "last-known-good");
        var projectPath = Path.Combine(directory, "Rollback.smileproj");
        File.WriteAllText(projectPath, "<SmileProject><PropertyGroup><ProjectKind>Game</ProjectKind><StartupFile>Program.smile</StartupFile></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" StartupOnly=\"true\" /><Asset Include=\"Assets\\Current.txt\" /></ItemGroup></SmileProject>");
        var manifest = SmileProjectSourceSet.Load(projectPath).AssetManifest;
        SmileProjectAssetPublisher.Publish(manifest, output, "Rollback", "web");
        var priorManifest = File.ReadAllBytes(Path.Combine(output, "smile-assets.json"));
        File.WriteAllText(Path.Combine(output, "sentinel.txt"), "unrelated");
        File.WriteAllText(assetPath, "replacement");
        manifest = SmileProjectSourceSet.Load(projectPath).AssetManifest;

        ThrowsProjectDiagnostic(() => SmileProjectAssetPublisher.Publish(manifest, output, "Rollback", "web",
            null, false, (stage, relative) =>
            {
                if (stage == SmileAssetPublicationStage.AfterFileCommit &&
                    string.Equals(relative, "Assets/Current.txt", StringComparison.Ordinal))
                    throw new IOException("Synthetic asset commit failure.");
            }), "SML3604");
        Equal("last-known-good", File.ReadAllText(Path.Combine(output, "Assets", "Current.txt")));
        Equal(true, priorManifest.SequenceEqual(File.ReadAllBytes(Path.Combine(output, "smile-assets.json"))));
        Equal("unrelated", File.ReadAllText(Path.Combine(output, "sentinel.txt")));
        Equal(0, Directory.EnumerateDirectories(output, ".smile-assets-*").Count());
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
Run("Every public Smile.UI stateful facade member has educational documentation", () =>
{
    var compilation = SmileProjectCompilation.Load("libraries/Smile.UI/Smile.UI.smilelibproj");
    var analysis = SmileLanguage.Analyze(compilation.Sources, SmileCompilationKind.Library,
        compilation.DependencyContext);
    Equal(false, analysis.HasErrors);
    var facades = analysis.SemanticModel.NominalTypes.Values.OfType<ClassTypeSymbol>()
        .Where(type => type.Name is "Menu" or "MenuNavigator" or "Dialogue")
        .DistinctBy(type => type.RuntimeIdentity).OrderBy(type => type.RuntimeIdentity).ToArray();
    Equal(3, facades.Length);
    foreach (var facade in facades)
    {
        var constructorDocumentation = SmileDocumentationService.GetDocumentation(facade.Constructor.Source,
            facade.Constructor.Declaration.Keyword.Span.Start);
        Equal(false, string.IsNullOrWhiteSpace(constructorDocumentation.Summary));
        foreach (var parameter in facade.Constructor.Parameters)
            Equal(true, constructorDocumentation.Parameters.ContainsKey(parameter.Name));
        foreach (var member in facade.Members.Where(member => member.Visibility == ModuleVisibility.Public))
        {
            var position = member switch
            {
                TypeRoutineSymbol routine => routine.Routine.Declaration.Keyword.Span.Start,
                PropertySymbol property => property.Declaration.PropertyKeyword.Span.Start,
                _ => member.DeclarationSpan.Start
            };
            var documentation = SmileDocumentationService.GetDocumentation(member.Source, position);
            Equal(false, string.IsNullOrWhiteSpace(documentation.Summary));
            if (member is TypeRoutineSymbol routineMember)
            {
                foreach (var parameter in routineMember.Routine.Parameters)
                    Equal(true, documentation.Parameters.ContainsKey(parameter.Name));
                if (routineMember.Routine.IsFunction)
                    Equal(false, string.IsNullOrWhiteSpace(documentation.Returns));
            }
        }
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
Run("Library package schema contract is formatVersion 6", () =>
    Equal(6, SmileLibraryPackage.CurrentFormatVersion));
Run("Library packages are deterministic and reload through authoritative analysis", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmilePackageTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var projectPath = Path.Combine(directory, "Tools.smilelibproj");
        Directory.CreateDirectory(Path.Combine(directory, "Code"));
        Directory.CreateDirectory(Path.Combine(directory, "Other"));
        var sourcePath = Path.Combine(directory, "Code", "Tools.smile");
        var secondSourcePath = Path.Combine(directory, "Other", "Tools.smile");
        File.WriteAllText(sourcePath, "Module Example.Tools\nPublic Function Double(Value)\nReturn Value * 2\nEnd Function\nPrivate Const Hidden = 1\nEnd Module\n");
        File.WriteAllText(secondSourcePath,
            "Module Example.Tools\nPublic Const Marker = 7\nEnd Module\n");
        File.WriteAllText(projectPath, "<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind><LibraryName>Example.Tools</LibraryName><Version>1.0.0</Version></PropertyGroup><ItemGroup><SmileSource Include=\"Code\\Tools.smile\" /><SmileSource Include=\"Other\\Tools.smile\" /></ItemGroup></SmileProject>");
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
        Equal("Example.Tools@1.0.0", loaded.Identity.Provider);
        Equal(2, loaded.Sources.Count);
        Equal("src/Code/Tools.smile|src/Other/Tools.smile", string.Join("|",
            loaded.SourceIds.Values.OrderBy(item => item, StringComparer.Ordinal)));
        using (var archive = System.IO.Compression.ZipFile.OpenRead(first))
        {
            Equal(true, archive.GetEntry("manifest.json") != null);
            using (var manifestReader = new StreamReader(archive.GetEntry("manifest.json")!.Open()))
            using (var manifest = System.Text.Json.JsonDocument.Parse(manifestReader.ReadToEnd()))
            {
                Equal(6, manifest.RootElement.GetProperty("formatVersion").GetInt32());
                Equal("Example.Tools@1.0.0", manifest.RootElement.GetProperty("provider").GetString());
                Equal("src/Code/Tools.smile", manifest.RootElement.GetProperty("sources")[0].GetString());
                Equal("src/Other/Tools.smile", manifest.RootElement.GetProperty("sources")[1].GetString());
            }
            var apiEntry = archive.GetEntry("api/public-symbols.json")!;
            using var reader = new StreamReader(apiEntry.Open());
            var api = reader.ReadToEnd();
            Equal(true, api.Contains("Double", StringComparison.Ordinal));
            Equal(false, api.Contains("Hidden", StringComparison.Ordinal));
            using var document = System.Text.Json.JsonDocument.Parse(api);
            Equal(6, document.RootElement.GetProperty("formatVersion").GetInt32());
            Equal("Example.Tools@1.0.0", document.RootElement.GetProperty("library")
                .GetProperty("provider").GetString());
            var module = document.RootElement.GetProperty("modules")[0];
            Equal("src/Code/Tools.smile", module.GetProperty("sources")[0].GetString());
            Equal("src/Other/Tools.smile", module.GetProperty("sources")[1].GetString());
            var member = module.GetProperty("members").EnumerateArray()
                .Single(item => item.GetProperty("name").GetString() == "Double");
            Equal("src/Code/Tools.smile", member.GetProperty("location").GetProperty("source").GetString());
            Equal(6, member.GetProperty("location").GetProperty("length").GetInt32());
            Equal(false, api.Contains(directory, StringComparison.OrdinalIgnoreCase));
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
Run("FormatVersion 6 preserves enum metadata and project package consumer parity", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileEnumPackageTests-" + Guid.NewGuid().ToString("N"));
    var libraryDirectory = Path.Combine(directory, "Library");
    var projectConsumerDirectory = Path.Combine(directory, "ProjectConsumer");
    var packageConsumerDirectory = Path.Combine(directory, "PackageConsumer");
    Directory.CreateDirectory(libraryDirectory);
    Directory.CreateDirectory(projectConsumerDirectory);
    Directory.CreateDirectory(packageConsumerDirectory);
    try
    {
        var libraryProjectPath = Path.Combine(libraryDirectory, "Enums.smilelibproj");
        var librarySourcePath = Path.Combine(libraryDirectory, "Enums.smile");
        File.WriteAllText(librarySourcePath,
            "Module Example.Enums\n" +
            "Public Enum Direction\n" +
            "Up = 1\n" +
            "Right = 2\n" +
            "Down = 3\n" +
            "Left = 4\n" +
            "End Enum\n" +
            "Private Enum HiddenDirection\n" +
            "Hidden = 9\n" +
            "End Enum\n" +
            "Public Function Echo(Value As Direction) As Direction\n" +
            "Return Value\n" +
            "End Function\n" +
            "End Module\n");
        File.WriteAllText(libraryProjectPath,
            "<SmileProject><PropertyGroup><ProjectKind>Library</ProjectKind>" +
            "<LibraryName>Example.EnumLibrary</LibraryName><Version>1.0.0</Version></PropertyGroup>" +
            "<ItemGroup><SmileSource Include=\"Enums.smile\" /></ItemGroup></SmileProject>");
        var libraryCompilation = SmileProjectCompilation.Load(libraryProjectPath,
            Path.Combine(directory, "library-cache"));
        var libraryAnalysis = SmileLanguage.Analyze(libraryCompilation.Sources, SmileCompilationKind.Library,
            libraryCompilation.DependencyContext);
        Equal(false, libraryAnalysis.HasErrors);
        var packagePath = Path.Combine(directory, "Enums.smilelib");
        SmileLibraryPackage.Write(packagePath, libraryCompilation.Graph.Root, libraryAnalysis);

        string apiText;
        using (var archive = System.IO.Compression.ZipFile.OpenRead(packagePath))
        using (var reader = new StreamReader(archive.GetEntry("api/public-symbols.json")!.Open()))
            apiText = reader.ReadToEnd();
        using (var document = System.Text.Json.JsonDocument.Parse(apiText))
        {
            var root = document.RootElement;
            Equal(6, root.GetProperty("formatVersion").GetInt32());
            Equal("Example.EnumLibrary@1.0.0", root.GetProperty("library").GetProperty("provider").GetString());
            var module = root.GetProperty("modules")[0];
            Equal("src/Enums.smile", module.GetProperty("sources")[0].GetString());
            var direction = module.GetProperty("members").EnumerateArray()
                .Single(member => member.GetProperty("name").GetString() == "Direction");
            Equal("Enum", direction.GetProperty("kind").GetString());
            Equal("Example.Enums::Direction", direction.GetProperty("identity").GetString());
            Equal("Example.EnumLibrary@1.0.0", direction.GetProperty("provider").GetString());
            Equal("Up|Right|Down|Left", string.Join("|", direction.GetProperty("members")
                .EnumerateArray().Select(member => member.GetProperty("name").GetString())));
            Equal("1|2|3|4", string.Join("|", direction.GetProperty("members")
                .EnumerateArray().Select(member => member.GetProperty("value").GetInt64())));
            Equal("0|1|2|3", string.Join("|", direction.GetProperty("members")
                .EnumerateArray().Select(member => member.GetProperty("ordinal").GetInt32())));
            var echo = module.GetProperty("members").EnumerateArray()
                .Single(member => member.GetProperty("name").GetString() == "Echo");
            Equal("enum", echo.GetProperty("returnType").GetProperty("kind").GetString());
            Equal("Example.EnumLibrary@1.0.0",
                echo.GetProperty("returnType").GetProperty("provider").GetString());
            var parameter = echo.GetProperty("parameters")[0];
            Equal("Value", parameter.GetProperty("name").GetString());
            Equal("ByVal", parameter.GetProperty("mode").GetString());
            Equal(0, parameter.GetProperty("ordinal").GetInt32());
            Equal(false, parameter.GetProperty("optional").GetBoolean());
            Equal(System.Text.Json.JsonValueKind.Null, parameter.GetProperty("default").ValueKind);
            Equal("enum", parameter.GetProperty("type").GetProperty("kind").GetString());
            Equal(false, apiText.Contains("HiddenDirection", StringComparison.Ordinal));
            Equal(false, apiText.Contains(directory, StringComparison.OrdinalIgnoreCase));
        }

        const string program = "Import Example.Enums As Enums\n" +
                               "Dim Facing As Enums.Direction\n" +
                               "Facing = Enums.Direction.Left\n" +
                               "If Enums.Echo(Facing) = Enums.Direction.Left Then\n" +
                               "Print \"PASS\"\n" +
                               "End If\n";
        SmileAnalysisResult AnalyzeConsumer(string consumerDirectory, string reference)
        {
            var programPath = Path.Combine(consumerDirectory, "Program.smile");
            var projectPath = Path.Combine(consumerDirectory, "Consumer.smileproj");
            File.WriteAllText(programPath, program);
            File.WriteAllText(projectPath,
                "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup>" +
                "<ItemGroup><SmileSource Include=\"Program.smile\" />" + reference +
                "</ItemGroup></SmileProject>");
            var compilation = SmileProjectCompilation.Load(projectPath,
                Path.Combine(consumerDirectory, "cache"));
            var analysis = SmileLanguage.Analyze(compilation.Sources, SmileCompilationKind.Program,
                compilation.DependencyContext);
            Equal(false, analysis.HasErrors);
            var facingType = analysis.SemanticModel.Symbols["Facing"].Type;
            Equal(SmileTypeKind.Enum, facingType.Kind);
            Equal(true, compilation.DependencyContext.TryGetProviderDescriptor(
                facingType.ProviderIdentity, out var descriptor));
            Equal("Example.EnumLibrary@1.0.0", descriptor.LogicalIdentity);
            var tree = analysis.GetSyntaxTree(programPath);
            var leftPosition = program.IndexOf("Left", StringComparison.Ordinal);
            Equal(true, SmileSymbolService.TryResolve(analysis, tree, leftPosition, out var resolved));
            Equal("Left", resolved.Name);
            Equal("Enums.smile", Path.GetFileName(resolved.DeclarationLocation!.FilePath));
            return analysis;
        }

        AnalyzeConsumer(projectConsumerDirectory,
            "<SmileProjectReference Include=\"..\\Library\\Enums.smilelibproj\" />");
        var packageConsumerPackage = Path.Combine(packageConsumerDirectory, "Enums.smilelib");
        File.Copy(packagePath, packageConsumerPackage);
        AnalyzeConsumer(packageConsumerDirectory,
            "<SmileLibraryReference Include=\"Enums.smilelib\" />");
    }
    finally { Directory.Delete(directory, true); }
});
Run("FormatVersion 6 preserves Optional defaults and named-call project package parity", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileOptionalPackageTests-" + Guid.NewGuid().ToString("N"));
    var fixtureDirectory = Path.GetFullPath("examples/LightweightOopCalls");
    var libraryProjectPath = Path.Combine(fixtureDirectory, "LightweightOopLibrary.smilelibproj");
    Directory.CreateDirectory(directory);
    try
    {
        var compilation = SmileProjectCompilation.Load(libraryProjectPath,
            Path.Combine(directory, "library-cache"));
        var analysis = SmileLanguage.Analyze(compilation.Sources, SmileCompilationKind.Library,
            compilation.DependencyContext);
        Equal(false, analysis.HasErrors);
        var apiPath = Path.Combine(fixtureDirectory, "Library", "Api.smile");
        var apiSourceText = File.ReadAllText(apiPath);
        var apiTree = analysis.GetSyntaxTree(apiPath);
        var configureMePosition = apiSourceText.IndexOf("Me.Label", StringComparison.Ordinal);
        if (!SmileSymbolService.TryResolve(analysis, apiTree, configureMePosition + 1, out var resolvedMe))
            throw new InvalidOperationException("Me use did not resolve inside a Type method.");
        Equal(SmileResolvedSymbolKind.Local, resolvedMe.Kind);
        Equal("Me", resolvedMe.Name);
        var methodGeneralCompletions = SmileCompletionService.GetCompletions(analysis, apiTree,
            configureMePosition + 1);
        if (!methodGeneralCompletions.Any(completion => completion.DisplayText == "Me"))
            throw new InvalidOperationException("Me was absent from Type-method completion.");
        var insideTypeMembers = SmileCompletionService.GetCompletions(analysis, apiTree,
            configureMePosition + "Me.".Length);
        if (!insideTypeMembers.Any(completion => completion.DisplayText == "Hide"))
            throw new InvalidOperationException("Private Type method was absent from inside-Type completion: " +
                string.Join("|", insideTypeMembers.Select(completion => completion.DisplayText)));
        if (!insideTypeMembers.Any(completion => completion.DisplayText == "Secret"))
            throw new InvalidOperationException("Private Property was absent from inside-Type completion.");
        var setterValuePosition = apiSourceText.IndexOf("Me.StoredValue = Value", StringComparison.Ordinal) +
                                  "Me.StoredValue = ".Length;
        if (!SmileSymbolService.TryResolve(analysis, apiTree, setterValuePosition + 1,
                out var resolvedSetterValue))
            throw new InvalidOperationException("Setter Value use did not resolve.");
        Equal(SmileResolvedSymbolKind.Local, resolvedSetterValue.Kind);
        Equal("Value", resolvedSetterValue.Name);
        var setterCompletions = SmileCompletionService.GetCompletions(analysis, apiTree,
            setterValuePosition + 1);
        if (!setterCompletions.Any(completion => completion.DisplayText == "Me"))
            throw new InvalidOperationException("Me was absent from Property-setter completion.");
        if (!setterCompletions.Any(completion => completion.DisplayText == "Value"))
            throw new InvalidOperationException("Value was absent from Property-setter completion.");
        var classStartPosition = apiSourceText.IndexOf("Public Class ReferenceCounter", StringComparison.Ordinal);
        var classMePosition = apiSourceText.IndexOf("Me.Label = Label", classStartPosition,
            StringComparison.Ordinal);
        Equal(true, SmileSymbolService.TryResolve(analysis, apiTree, classMePosition + 1,
            out var resolvedClassMe));
        Equal(SmileResolvedSymbolKind.Local, resolvedClassMe.Kind);
        Equal("Me", resolvedClassMe.Name);
        var insideClassMembers = SmileCompletionService.GetCompletions(analysis, apiTree,
            classMePosition + "Me.".Length);
        foreach (var expected in new[] { "Advance", "Caption", "Code", "CurrentMode", "Hide", "Label",
                     "Notes", "Samples", "Secret", "State", "Total" })
            Equal(true, insideClassMembers.Any(completion => completion.DisplayText == expected));
        var classConstructorDeclarationPosition = apiSourceText.IndexOf("Sub New(", classStartPosition,
                                                      StringComparison.Ordinal) + "Sub ".Length;
        Equal(true, SmileSymbolService.TryResolve(analysis, apiTree,
            classConstructorDeclarationPosition + 1, out var resolvedClassConstructorDeclaration));
        Equal(SmileResolvedSymbolKind.Constructor, resolvedClassConstructorDeclaration.Kind);
        Equal("New", resolvedClassConstructorDeclaration.Name);
        var firstPackage = Path.Combine(directory, "first.smilelib");
        var secondPackage = Path.Combine(directory, "second.smilelib");
        SmileLibraryPackage.Write(firstPackage, compilation.Graph.Root, analysis);
        SmileLibraryPackage.Write(secondPackage, compilation.Graph.Root, analysis);
        Equal(true, File.ReadAllBytes(firstPackage).SequenceEqual(File.ReadAllBytes(secondPackage)));

        var expectedFingerprint = SmileLibraryPackage.CreateBuildFingerprint(compilation.Graph.Root, analysis);
        Equal(true, SmileLibraryPackage.ReadBuildFingerprint(firstPackage).Matches(expectedFingerprint));
        var loaded = SmileLibraryPackage.Read(firstPackage, Path.Combine(directory, "read-cache"));
        Equal("src/Library/Api.smile", loaded.SourceIds.Values.Single());
        Equal(false, loaded.SourceIds.Keys.Any(path => path.Contains("src/Library/Api.smile",
            StringComparison.Ordinal)));

        string apiText;
        var gameProbeGetterLine = 0;
        var gameProbeGetterColumn = 0;
        var gameProbeGetterLength = 0;
        var referenceConstructorLine = 0;
        var referenceConstructorColumn = 0;
        var referenceConstructorLength = 0;
        using (var archive = System.IO.Compression.ZipFile.OpenRead(firstPackage))
        using (var reader = new StreamReader(archive.GetEntry("api/public-symbols.json")!.Open()))
            apiText = reader.ReadToEnd();
        using (var document = System.Text.Json.JsonDocument.Parse(apiText))
        {
            var root = document.RootElement;
            Equal("Smile.Lightweight.Oop.Proof@1.2.0",
                root.GetProperty("library").GetProperty("provider").GetString());
            var module = root.GetProperty("modules")[0];
            Equal("src/Library/Api.smile", module.GetProperty("sources")[0].GetString());
            var moduleMembers = module.GetProperty("members").EnumerateArray().ToArray();
            Equal("Counter|CounterBox|DisplayMode|EmptyReference|GameConstructorProbe|GameReferenceProbe|ReferenceCounter|Report",
                string.Join("|", moduleMembers
                .Select(member => member.GetProperty("name").GetString())));
            var report = moduleMembers
                .Single(member => member.GetProperty("name").GetString() == "Report");
            var parameters = report.GetProperty("parameters").EnumerateArray().ToArray();
            Equal("Label|Copies|Enabled|Suffix|Mode",
                string.Join("|", parameters.Select(parameter => parameter.GetProperty("name").GetString())));
            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];
                Equal("name|type|mode|optional|default|ordinal|location",
                    string.Join("|", parameter.EnumerateObject().Select(property => property.Name)));
                Equal(index, parameter.GetProperty("ordinal").GetInt32());
                Equal(parameter.GetProperty("name").GetString()!.Length,
                    parameter.GetProperty("location").GetProperty("length").GetInt32());
                Equal("src/Library/Api.smile",
                    parameter.GetProperty("location").GetProperty("source").GetString());
            }
            Equal(false, parameters[0].GetProperty("optional").GetBoolean());
            Equal(System.Text.Json.JsonValueKind.Null, parameters[0].GetProperty("default").ValueKind);

            var numberDefault = parameters[1].GetProperty("default");
            Equal("kind|value", string.Join("|", numberDefault.EnumerateObject().Select(property => property.Name)));
            Equal("number", numberDefault.GetProperty("kind").GetString());
            Equal(3L, numberDefault.GetProperty("value").GetInt64());
            var booleanDefault = parameters[2].GetProperty("default");
            Equal("boolean", booleanDefault.GetProperty("kind").GetString());
            Equal(true, booleanDefault.GetProperty("value").GetBoolean());
            var textDefault = parameters[3].GetProperty("default");
            Equal("text", textDefault.GetProperty("kind").GetString());
            Equal("!", textDefault.GetProperty("value").GetString());
            var enumDefault = parameters[4].GetProperty("default");
            Equal("kind|member|value",
                string.Join("|", enumDefault.EnumerateObject().Select(property => property.Name)));
            Equal("enum", enumDefault.GetProperty("kind").GetString());
            Equal("CompactAlias", enumDefault.GetProperty("member").GetString());
            Equal(2L, enumDefault.GetProperty("value").GetInt64());
            var enumType = parameters[4].GetProperty("type");
            Equal("enum", enumType.GetProperty("kind").GetString());
            Equal("Smile.Lightweight.Oop.Proof::DisplayMode", enumType.GetProperty("identity").GetString());
            Equal("Smile.Lightweight.Oop.Proof@1.2.0", enumType.GetProperty("provider").GetString());
            Equal(false, enumDefault.TryGetProperty("type", out _));
            Equal(false, enumDefault.TryGetProperty("provider", out _));

            void AssertLocation(System.Text.Json.JsonElement element, string token)
            {
                var location = element.GetProperty("location");
                Equal("src/Library/Api.smile", location.GetProperty("source").GetString());
                Equal(token.Length, location.GetProperty("length").GetInt32());
            }

            var counter = moduleMembers.Single(member =>
                member.GetProperty("name").GetString() == "Counter");
            Equal("Smile.Lightweight.Oop.Proof::Counter", counter.GetProperty("identity").GetString());
            Equal("Smile.Lightweight.Oop.Proof@1.2.0", counter.GetProperty("provider").GetString());
            Equal("Label|StoredValue|Enabled|Mode", string.Join("|", counter.GetProperty("fields")
                .EnumerateArray().Select(field => field.GetProperty("name").GetString())));
            AssertLocation(counter, "Counter");
            foreach (var field in counter.GetProperty("fields").EnumerateArray())
                AssertLocation(field, field.GetProperty("name").GetString()!);

            var nestedMembers = counter.GetProperty("members").EnumerateArray().ToArray();
            Equal("Advance|Caption|Configure|Difference|DrawProbe|GameProbe|Shifted|Total",
                string.Join("|", nestedMembers.Select(member => member.GetProperty("name").GetString())));
            Equal(false, apiText.Contains("\"name\": \"Hide\"", StringComparison.Ordinal));
            Equal(false, apiText.Contains("\"name\": \"Secret\"", StringComparison.Ordinal));
            Equal(false, apiText.Contains("::receiver", StringComparison.Ordinal));
            Equal(false, apiText.Contains("::value", StringComparison.Ordinal));
            Equal(false, apiText.Contains("\"name\": \"Me\"", StringComparison.Ordinal));
            Equal(false, apiText.Contains("\"name\": \"Value\"", StringComparison.Ordinal));

            var routines = nestedMembers.Where(member =>
                member.GetProperty("kind").GetString() is "Subroutine" or "Function").ToArray();
            foreach (var routine in routines)
            {
                Equal("name|kind|visibility|identity|returnType|parameters|requiresGameWindow|location",
                    string.Join("|", routine.EnumerateObject().Select(property => property.Name)));
                var name = routine.GetProperty("name").GetString()!;
                Equal("Smile.Lightweight.Oop.Proof::Counter::member::" + name,
                    routine.GetProperty("identity").GetString());
                AssertLocation(routine, name);
                foreach (var parameter in routine.GetProperty("parameters").EnumerateArray())
                {
                    AssertLocation(parameter, parameter.GetProperty("name").GetString()!);
                    Equal(false, parameter.GetProperty("name").GetString() == "Me");
                }
            }
            Equal(true, routines.Single(member => member.GetProperty("name").GetString() == "DrawProbe")
                .GetProperty("requiresGameWindow").GetBoolean());
            Equal(false, routines.Where(member => member.GetProperty("name").GetString() != "DrawProbe")
                .Any(member => member.GetProperty("requiresGameWindow").GetBoolean()));

            var configure = routines.Single(member => member.GetProperty("name").GetString() == "Configure");
            var configureParameters = configure.GetProperty("parameters").EnumerateArray().ToArray();
            Equal("Label|Start|Enabled|Mode", string.Join("|", configureParameters
                .Select(parameter => parameter.GetProperty("name").GetString())));
            for (var index = 0; index < configureParameters.Length; index++)
                Equal(index, configureParameters[index].GetProperty("ordinal").GetInt32());
            var configureMode = configureParameters[3];
            Equal("Smile.Lightweight.Oop.Proof@1.2.0",
                configureMode.GetProperty("type").GetProperty("provider").GetString());
            Equal("Standard", configureMode.GetProperty("default").GetProperty("member").GetString());
            Equal(1L, configureMode.GetProperty("default").GetProperty("value").GetInt64());
            var shiftedReturn = routines.Single(member => member.GetProperty("name").GetString() == "Shifted")
                .GetProperty("returnType");
            Equal("Smile.Lightweight.Oop.Proof::Counter", shiftedReturn.GetProperty("identity").GetString());
            Equal("Smile.Lightweight.Oop.Proof", shiftedReturn.GetProperty("module").GetString());
            Equal("Smile.Lightweight.Oop.Proof@1.2.0", shiftedReturn.GetProperty("provider").GetString());
            var differenceOther = routines.Single(member => member.GetProperty("name").GetString() == "Difference")
                .GetProperty("parameters")[0].GetProperty("type");
            Equal("Smile.Lightweight.Oop.Proof::Counter", differenceOther.GetProperty("identity").GetString());
            Equal("Smile.Lightweight.Oop.Proof@1.2.0", differenceOther.GetProperty("provider").GetString());

            var properties = nestedMembers.Where(member => member.GetProperty("kind").GetString() == "Property")
                .ToArray();
            foreach (var property in properties)
            {
                Equal("name|kind|visibility|identity|type|get|set|location",
                    string.Join("|", property.EnumerateObject().Select(item => item.Name)));
                var name = property.GetProperty("name").GetString()!;
                Equal("Smile.Lightweight.Oop.Proof::Counter::property::" + name,
                    property.GetProperty("identity").GetString());
                AssertLocation(property, name);
                Equal(false, property.TryGetProperty("parameters", out _));
                foreach (var accessorName in new[] { "get", "set" })
                {
                    var accessor = property.GetProperty(accessorName);
                    if (accessor.ValueKind == System.Text.Json.JsonValueKind.Null)
                        continue;
                    Equal("identity|requiresGameWindow|location",
                        string.Join("|", accessor.EnumerateObject().Select(item => item.Name)));
                    Equal(property.GetProperty("identity").GetString() + "::" + accessorName,
                        accessor.GetProperty("identity").GetString());
                    Equal(false, accessor.TryGetProperty("parameters", out _));
                    AssertLocation(accessor, accessorName == "get" ? "Get" : "Set");
                }
            }
            var caption = properties.Single(property => property.GetProperty("name").GetString() == "Caption");
            Equal(System.Text.Json.JsonValueKind.Null, caption.GetProperty("set").ValueKind);
            var total = properties.Single(property => property.GetProperty("name").GetString() == "Total");
            Equal(false, total.GetProperty("get").GetProperty("requiresGameWindow").GetBoolean());
            Equal(false, total.GetProperty("set").GetProperty("requiresGameWindow").GetBoolean());
            Equal(false, total.GetProperty("get").GetProperty("identity").GetString() ==
                total.GetProperty("set").GetProperty("identity").GetString());
            var gameProbe = properties.Single(property => property.GetProperty("name").GetString() == "GameProbe");
            Equal(true, gameProbe.GetProperty("get").GetProperty("requiresGameWindow").GetBoolean());
            Equal(false, gameProbe.GetProperty("set").GetProperty("requiresGameWindow").GetBoolean());
            var gameProbeGetterLocation = gameProbe.GetProperty("get").GetProperty("location");
            gameProbeGetterLine = gameProbeGetterLocation.GetProperty("line").GetInt32();
            gameProbeGetterColumn = gameProbeGetterLocation.GetProperty("column").GetInt32();
            gameProbeGetterLength = gameProbeGetterLocation.GetProperty("length").GetInt32();

            var referenceCounter = moduleMembers.Single(member =>
                member.GetProperty("name").GetString() == "ReferenceCounter");
            Equal("name|kind|visibility|identity|module|provider|size|alignment|fields|constructor|members|location",
                string.Join("|", referenceCounter.EnumerateObject().Select(property => property.Name)));
            Equal("Class", referenceCounter.GetProperty("kind").GetString());
            Equal("Smile.Lightweight.Oop.Proof::ReferenceCounter",
                referenceCounter.GetProperty("identity").GetString());
            Equal("Smile.Lightweight.Oop.Proof@1.2.0",
                referenceCounter.GetProperty("provider").GetString());
            Equal(8, referenceCounter.GetProperty("size").GetInt32());
            Equal(8, referenceCounter.GetProperty("alignment").GetInt32());
            Equal(false, referenceCounter.TryGetProperty("instanceSize", out _));
            var classFields = referenceCounter.GetProperty("fields").EnumerateArray().ToArray();
            Equal("Code|Samples", string.Join("|", classFields
                .Select(field => field.GetProperty("name").GetString())));
            Equal("name|visibility|type|ordinal|location",
                string.Join("|", classFields[0].EnumerateObject().Select(property => property.Name)));
            Equal("name|visibility|elementType|rank|dimensions|ordinal|location",
                string.Join("|", classFields[1].EnumerateObject().Select(property => property.Name)));
            Equal(1, classFields[1].GetProperty("rank").GetInt32());
            Equal(2, classFields[1].GetProperty("dimensions")[0].GetInt32());
            Equal(false, classFields.Any(field => field.TryGetProperty("offset", out _)));

            var constructor = referenceCounter.GetProperty("constructor");
            Equal("identity|visibility|declared|parameters|requiresGameWindow|location",
                string.Join("|", constructor.EnumerateObject().Select(property => property.Name)));
            Equal("Smile.Lightweight.Oop.Proof::ReferenceCounter::constructor::New",
                constructor.GetProperty("identity").GetString());
            Equal(true, constructor.GetProperty("declared").GetBoolean());
            Equal(false, constructor.GetProperty("requiresGameWindow").GetBoolean());
            var constructorParameters = constructor.GetProperty("parameters").EnumerateArray().ToArray();
            Equal("Label|Start|Mode", string.Join("|", constructorParameters
                .Select(parameter => parameter.GetProperty("name").GetString())));
            Equal(false, constructorParameters.Any(parameter => parameter.GetProperty("name").GetString() == "Me"));
            Equal("class", referenceCounter.GetProperty("members").EnumerateArray()
                .Single(member => member.GetProperty("name").GetString() == "Alias")
                .GetProperty("returnType").GetProperty("kind").GetString());
            Equal("Smile.Lightweight.Oop.Proof@1.2.0", referenceCounter.GetProperty("members")
                .EnumerateArray().Single(member => member.GetProperty("name").GetString() == "Same")
                .GetProperty("parameters")[0].GetProperty("type").GetProperty("provider").GetString());
            Equal("Advance|Alias|Caption|Same|Snapshot|Total", string.Join("|", referenceCounter
                .GetProperty("members").EnumerateArray().Select(member => member.GetProperty("name").GetString())));
            var referenceConstructorLocation = constructor.GetProperty("location");
            referenceConstructorLine = referenceConstructorLocation.GetProperty("line").GetInt32();
            referenceConstructorColumn = referenceConstructorLocation.GetProperty("column").GetInt32();
            referenceConstructorLength = referenceConstructorLocation.GetProperty("length").GetInt32();
            Equal(3, referenceConstructorLength);

            var emptyReference = moduleMembers.Single(member =>
                member.GetProperty("name").GetString() == "EmptyReference");
            Equal(false, emptyReference.GetProperty("constructor").GetProperty("declared").GetBoolean());
            Equal(0, emptyReference.GetProperty("constructor").GetProperty("parameters").GetArrayLength());
            AssertLocation(emptyReference.GetProperty("constructor"), "EmptyReference");

            var gameReference = moduleMembers.Single(member =>
                member.GetProperty("name").GetString() == "GameReferenceProbe");
            Equal(false, gameReference.GetProperty("constructor").GetProperty("requiresGameWindow").GetBoolean());
            Equal(true, gameReference.GetProperty("members").EnumerateArray()
                .Single(member => member.GetProperty("name").GetString() == "DrawProbe")
                .GetProperty("requiresGameWindow").GetBoolean());
            var classGameProperty = gameReference.GetProperty("members").EnumerateArray()
                .Single(member => member.GetProperty("name").GetString() == "GameProbe");
            Equal(true, classGameProperty.GetProperty("get").GetProperty("requiresGameWindow").GetBoolean());
            Equal(false, classGameProperty.GetProperty("set").GetProperty("requiresGameWindow").GetBoolean());
            var gameConstructor = moduleMembers.Single(member =>
                member.GetProperty("name").GetString() == "GameConstructorProbe");
            Equal(true, gameConstructor.GetProperty("constructor")
                .GetProperty("requiresGameWindow").GetBoolean());
            Equal(false, apiText.Contains("ReferenceCounter::member::Hide", StringComparison.Ordinal));
            Equal(false, apiText.Contains("ReferenceCounter::property::Secret", StringComparison.Ordinal));
            Equal(false, apiText.Contains("\"instanceSize\"", StringComparison.Ordinal));
            Equal(false, apiText.Contains(directory, StringComparison.OrdinalIgnoreCase));
        }

        void AssertApiTamper(string fileName, Func<string, string> rewrite)
        {
            var tampered = Path.Combine(directory, fileName);
            File.Copy(firstPackage, tampered);
            RewritePackageTextEntry(tampered, "api/public-symbols.json", rewrite);
            Equal(false, SmileLibraryPackage.ReadBuildFingerprint(tampered).Matches(expectedFingerprint));
            Equal(true, CompilerDriver.NeedsLibraryBuild(compilation.Graph.Root, tampered, analysis));
            ThrowsProjectDiagnostic(() => SmileLibraryPackage.Read(tampered,
                Path.Combine(directory, fileName + "-cache")), "SML3207");
        }

        AssertApiTamper("member-tampered.smilelib", text => text.Replace(
            "{\"kind\": \"enum\", \"member\": \"CompactAlias\", \"value\": 2}",
            "{\"kind\": \"enum\", \"member\": \"Compact\", \"value\": 2}",
            StringComparison.Ordinal));
        AssertApiTamper("value-tampered.smilelib", text => text.Replace(
            "{\"kind\": \"enum\", \"member\": \"CompactAlias\", \"value\": 2}",
            "{\"kind\": \"enum\", \"member\": \"CompactAlias\", \"value\": 99}",
            StringComparison.Ordinal));
        AssertApiTamper("type-method-identity-tampered.smilelib", text => text.Replace(
            "Smile.Lightweight.Oop.Proof::Counter::member::Configure",
            "Smile.Lightweight.Oop.Proof::Counter::member::ConfigureTampered",
            StringComparison.Ordinal));
        AssertApiTamper("property-capability-tampered.smilelib", text => text.Replace(
            "Smile.Lightweight.Oop.Proof::Counter::property::GameProbe::get\", \"requiresGameWindow\": true",
            "Smile.Lightweight.Oop.Proof::Counter::property::GameProbe::get\", \"requiresGameWindow\": false",
            StringComparison.Ordinal));
        AssertApiTamper("accessor-location-tampered.smilelib", text => text.Replace(
            $"\"line\": {gameProbeGetterLine}, \"column\": {gameProbeGetterColumn}, \"length\": {gameProbeGetterLength}",
            $"\"line\": {gameProbeGetterLine + 1}, \"column\": {gameProbeGetterColumn}, \"length\": {gameProbeGetterLength}",
            StringComparison.Ordinal));
        AssertApiTamper("class-constructor-identity-tampered.smilelib", text => text.Replace(
            "Smile.Lightweight.Oop.Proof::ReferenceCounter::constructor::New",
            "Smile.Lightweight.Oop.Proof::ReferenceCounter::constructor::Tampered",
            StringComparison.Ordinal));
        AssertApiTamper("class-constructor-location-tampered.smilelib", text => text.Replace(
            $"\"line\": {referenceConstructorLine}, \"column\": {referenceConstructorColumn}, \"length\": {referenceConstructorLength}",
            $"\"line\": {referenceConstructorLine + 1}, \"column\": {referenceConstructorColumn}, \"length\": {referenceConstructorLength}",
            StringComparison.Ordinal));
        AssertApiTamper("class-field-dimension-tampered.smilelib", text => text.Replace(
            "\"dimensions\": [2], \"ordinal\": 1",
            "\"dimensions\": [3], \"ordinal\": 1",
            StringComparison.Ordinal));

        string AnalyzeConsumer(string name, string reference, bool packageReference)
        {
            var consumerDirectory = Path.Combine(directory, name);
            Directory.CreateDirectory(consumerDirectory);
            var programPath = Path.Combine(consumerDirectory, "Program.smile");
            File.Copy(Path.Combine(fixtureDirectory, "Program.smile"), programPath);
            var projectPath = Path.Combine(consumerDirectory, "Consumer.smileproj");
            File.WriteAllText(projectPath,
                "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup>" +
                "<ItemGroup><SmileSource Include=\"Program.smile\" />" + reference +
                "</ItemGroup></SmileProject>");
            var consumerCompilation = SmileProjectCompilation.Load(projectPath,
                Path.Combine(consumerDirectory, "cache"));
            var consumerAnalysis = SmileLanguage.Analyze(consumerCompilation.Sources,
                SmileCompilationKind.Program, consumerCompilation.DependencyContext);
            Equal(false, consumerAnalysis.HasErrors);
            var proofModule = consumerAnalysis.SemanticModel.Modules["Smile.Lightweight.Oop.Proof"];
            var report = proofModule.PublicMembers
                .Single(member => member.Name == "Report").Routine!;
            Equal("false|true|true|true|true",
                string.Join("|", report.Parameters.Select(parameter =>
                    parameter.IsOptional.ToString().ToLowerInvariant())));
            Equal("3|True|!|CompactAlias",
                string.Join("|", report.Parameters.Skip(1).Select(parameter =>
                    parameter.DefaultEnumMember?.Name ?? parameter.DefaultValue.ToString())));
            Equal(true, consumerCompilation.DependencyContext.TryGetProviderDescriptor(
                report.ProviderIdentity, out var descriptor));
            Equal("Smile.Lightweight.Oop.Proof@1.2.0", descriptor.LogicalIdentity);

            var counter = (RecordTypeSymbol)proofModule.Types["Counter"].Type!;
            Equal("Smile.Lightweight.Oop.Proof::Counter", counter.RuntimeIdentity);
            Equal(true, consumerCompilation.DependencyContext.TryGetProviderDescriptor(
                counter.ProviderIdentity, out var counterProvider));
            Equal("Smile.Lightweight.Oop.Proof@1.2.0", counterProvider.LogicalIdentity);
            var publicTypeMembers = counter.Members.Where(member => member.Visibility == ModuleVisibility.Public &&
                    member.MemberKind != SmileTypeMemberKind.Field)
                .OrderBy(member => member.Name, StringComparer.Ordinal).ToArray();
            Equal("Advance|Caption|Configure|Difference|DrawProbe|GameProbe|Shifted|Total",
                string.Join("|", publicTypeMembers.Select(member => member.Name)));
            Equal(false, publicTypeMembers.Any(member => member.Name is "Hide" or "Secret"));
            var configureMethod = counter.Methods.Single(method => method.Name == "Configure");
            Equal("Smile.Lightweight.Oop.Proof::Counter::member::Configure", configureMethod.RuntimeIdentity);
            Equal(true, consumerCompilation.DependencyContext.TryGetProviderDescriptor(
                configureMethod.ProviderIdentity, out var configureProvider));
            Equal("Smile.Lightweight.Oop.Proof@1.2.0", configureProvider.LogicalIdentity);
            Equal("Label|Start|Enabled|Mode", string.Join("|", configureMethod.Parameters
                .Select(parameter => parameter.Name)));
            Equal(false, configureMethod.Parameters.Any(parameter => parameter.Name == "Me"));
            Equal(true, configureMethod.Receiver != null);
            Equal(true, configureMethod.LocalSymbols.Values.Contains(configureMethod.Receiver!));
            Equal("Standard", configureMethod.Parameters[3].DefaultEnumMember?.Name);
            Equal(1L, configureMethod.Parameters[3].DefaultValue);
            Equal(false, configureMethod.RequiresGameWindow);
            Equal(true, counter.Methods.Single(method => method.Name == "DrawProbe").RequiresGameWindow);
            var shifted = counter.Methods.Single(method => method.Name == "Shifted");
            Equal(counter.RuntimeIdentity, ((RecordTypeSymbol)shifted.ReturnType).RuntimeIdentity);
            var difference = counter.Methods.Single(method => method.Name == "Difference");
            Equal(counter.RuntimeIdentity, ((RecordTypeSymbol)difference.Parameters[0].Type).RuntimeIdentity);
            var captionProperty = counter.Properties.Single(property => property.Name == "Caption");
            Equal(true, captionProperty.Getter != null);
            Equal(true, captionProperty.Setter == null);
            var totalProperty = counter.Properties.Single(property => property.Name == "Total");
            Equal(false, totalProperty.Getter!.RequiresGameWindow);
            Equal(false, totalProperty.Setter!.RequiresGameWindow);
            Equal(false, totalProperty.Getter.RuntimeIdentity == totalProperty.Setter.RuntimeIdentity);
            var gameProbeProperty = counter.Properties.Single(property => property.Name == "GameProbe");
            Equal(true, gameProbeProperty.Getter!.RequiresGameWindow);
            Equal(false, gameProbeProperty.Setter!.RequiresGameWindow);

            var referenceCounter = (ClassTypeSymbol)proofModule.Types["ReferenceCounter"].Type!;
            Equal("Smile.Lightweight.Oop.Proof::ReferenceCounter", referenceCounter.RuntimeIdentity);
            Equal(true, consumerCompilation.DependencyContext.TryGetProviderDescriptor(
                referenceCounter.ProviderIdentity, out var referenceCounterProvider));
            Equal("Smile.Lightweight.Oop.Proof@1.2.0", referenceCounterProvider.LogicalIdentity);
            Equal("Code|Samples|Label|State|Notes|CurrentMode",
                string.Join("|", referenceCounter.Fields.Select(field => field.Name)));
            Equal("Code|Samples", string.Join("|", referenceCounter.Fields
                .Where(field => field.Visibility == ModuleVisibility.Public).Select(field => field.Name)));
            Equal(true, referenceCounter.Constructor.IsDeclared);
            Equal("Smile.Lightweight.Oop.Proof::ReferenceCounter::constructor::New",
                referenceCounter.Constructor.RuntimeIdentity);
            Equal("Label|Start|Mode", string.Join("|", referenceCounter.Constructor.Parameters
                .Select(parameter => parameter.Name)));
            Equal(false, referenceCounter.Constructor.Parameters.Any(parameter => parameter.Name == "Me"));
            Equal(true, referenceCounter.Constructor.Receiver != null);
            Equal(false, referenceCounter.Constructor.RequiresGameWindow);
            var publicClassMembers = referenceCounter.Members.Where(member =>
                    member.Visibility == ModuleVisibility.Public && member.MemberKind != SmileTypeMemberKind.Field)
                .OrderBy(member => member.Name, StringComparer.Ordinal).ToArray();
            Equal("Advance|Alias|Caption|Same|Snapshot|Total",
                string.Join("|", publicClassMembers.Select(member => member.Name)));
            Equal(false, publicClassMembers.Any(member => member.Name is "Hide" or "Secret"));
            Equal(referenceCounter.RuntimeIdentity, ((ClassTypeSymbol)referenceCounter.Methods
                .Single(method => method.Name == "Alias").ReturnType).RuntimeIdentity);
            Equal(referenceCounter.RuntimeIdentity, ((ClassTypeSymbol)referenceCounter.Methods
                .Single(method => method.Name == "Same").Parameters[0].Type).RuntimeIdentity);
            var emptyReference = (ClassTypeSymbol)proofModule.Types["EmptyReference"].Type!;
            Equal(false, emptyReference.Constructor.IsDeclared);
            Equal(0, emptyReference.Constructor.Parameters.Count);
            var gameReference = (ClassTypeSymbol)proofModule.Types["GameReferenceProbe"].Type!;
            Equal(false, gameReference.Constructor.RequiresGameWindow);
            Equal(true, gameReference.Methods.Single(method => method.Name == "DrawProbe").RequiresGameWindow);
            Equal(true, gameReference.Properties.Single(property => property.Name == "GameProbe")
                .Getter!.RequiresGameWindow);
            Equal(false, gameReference.Properties.Single(property => property.Name == "GameProbe")
                .Setter!.RequiresGameWindow);
            var gameConstructor = (ClassTypeSymbol)proofModule.Types["GameConstructorProbe"].Type!;
            Equal(true, gameConstructor.Constructor.RequiresGameWindow);

            var sourceTree = consumerAnalysis.GetSyntaxTree(programPath);
            var programText = File.ReadAllText(programPath);
            var qualifiedNewPosition = programText.IndexOf("New Proof.ReferenceCounter", StringComparison.Ordinal) +
                                       "New Proof.".Length;
            Equal(true, SmileSymbolService.TryResolve(consumerAnalysis, sourceTree,
                qualifiedNewPosition + 2, out var resolvedConstructor));
            Equal(SmileResolvedSymbolKind.Constructor, resolvedConstructor.Kind);
            Equal("New", resolvedConstructor.Name);
            Equal(referenceCounter.Constructor.DeclarationLocation.Line,
                resolvedConstructor.DeclarationLocation!.Line);
            var constructorPresentation = SmileSymbolDisplayService.Present(resolvedConstructor,
                consumerCompilation.DependencyContext);
            Equal("Sub Smile.Lightweight.Oop.Proof.ReferenceCounter.New(Label As Text, Optional Start As Number = 0, Optional Mode As DisplayMode = DisplayMode.Standard)",
                constructorPresentation.Signature);
            Equal("Smile.Lightweight.Oop.Proof@1.2.0", constructorPresentation.Provider);
            var constructorNamedCompletions = SmileCompletionService.GetCompletions(consumerAnalysis, sourceTree,
                    programText.IndexOf("New Proof.ReferenceCounter(", StringComparison.Ordinal) +
                    "New Proof.ReferenceCounter(".Length)
                .Where(completion => completion.Kind == SmileCompletionKind.Parameter &&
                    completion.InsertionText.EndsWith(":=", StringComparison.Ordinal)).ToArray();
            Equal("Label:=|Mode:=|Start:=", string.Join("|", constructorNamedCompletions
                .Select(completion => completion.InsertionText)));
            var qualifiedClassCompletions = SmileCompletionService.GetCompletions(consumerAnalysis, sourceTree,
                programText.IndexOf("New Proof.ReferenceCounter", StringComparison.Ordinal) + "New Proof.".Length);
            Equal(true, qualifiedClassCompletions.Any(completion => completion.DisplayText == "ReferenceCounter" &&
                completion.Kind == SmileCompletionKind.Class));
            Equal(true, qualifiedClassCompletions.Any(completion => completion.DisplayText == "EmptyReference" &&
                completion.Kind == SmileCompletionKind.Class));
            Equal(false, qualifiedClassCompletions.Any(completion => completion.DisplayText == "Counter"));

            var implicitNewPosition = programText.IndexOf("New Proof.EmptyReference", StringComparison.Ordinal) +
                                      "New Proof.".Length;
            Equal(true, SmileSymbolService.TryResolve(consumerAnalysis, sourceTree,
                implicitNewPosition + 2, out var resolvedImplicitConstructor));
            Equal(SmileResolvedSymbolKind.Constructor, resolvedImplicitConstructor.Kind);
            Equal(emptyReference.Constructor.DeclarationLocation.Line,
                resolvedImplicitConstructor.DeclarationLocation!.Line);

            var objectMemberPosition = programText.IndexOf("Object.Caption", StringComparison.Ordinal) +
                                       "Object.".Length;
            var objectMembers = SmileCompletionService.GetCompletions(consumerAnalysis, sourceTree,
                objectMemberPosition);
            foreach (var expected in new[] { "Advance", "Alias", "Caption", "Code", "Same", "Samples", "Snapshot", "Total" })
                Equal(true, objectMembers.Any(completion => completion.DisplayText == expected));
            Equal(false, objectMembers.Any(completion => completion.DisplayText is "Hide" or "Secret" or
                "Label" or "State" or "Notes" or "CurrentMode"));
            Equal(true, SmileSymbolService.TryResolve(consumerAnalysis, sourceTree,
                objectMemberPosition + 2, out var resolvedClassProperty));
            Equal(SmileResolvedSymbolKind.Property, resolvedClassProperty.Kind);
            Equal("Property Smile.Lightweight.Oop.Proof.ReferenceCounter.Caption As Text { Get }",
                SmileSymbolDisplayService.Present(resolvedClassProperty,
                    consumerCompilation.DependencyContext).Signature);
            var classFieldPosition = programText.IndexOf("Object.Samples", StringComparison.Ordinal) +
                                     "Object.".Length;
            Equal(true, SmileSymbolService.TryResolve(consumerAnalysis, sourceTree,
                classFieldPosition + 2, out var resolvedClassField));
            Equal(SmileResolvedSymbolKind.Field, resolvedClassField.Kind);
            Equal("Field Smile.Lightweight.Oop.Proof.ReferenceCounter.Samples[2] As Number",
                SmileSymbolDisplayService.Present(resolvedClassField,
                    consumerCompilation.DependencyContext).Signature);

            var labelPosition = programText.IndexOf("Enabled:=", StringComparison.Ordinal);
            Equal(true, SmileSymbolService.TryResolve(consumerAnalysis, sourceTree,
                labelPosition + 2, out var resolvedLabel));
            Equal(SmileResolvedSymbolKind.NamedArgument, resolvedLabel.Kind);
            Equal("Enabled", resolvedLabel.Name);
            Equal("Smile.Lightweight.Oop.Proof", resolvedLabel.ModuleName);
            Equal(true, consumerCompilation.DependencyContext.TryGetProviderDescriptor(
                resolvedLabel.ProviderIdentity, out var labelProvider));
            Equal("Smile.Lightweight.Oop.Proof@1.2.0", labelProvider.LogicalIdentity);
            var labelPresentation = SmileSymbolDisplayService.Present(resolvedLabel,
                consumerCompilation.DependencyContext);
            Equal("Optional Enabled As Boolean = True", labelPresentation.Signature);
            Equal("Smile.Lightweight.Oop.Proof@1.2.0", labelPresentation.Provider);
            Equal(resolvedLabel.DeclarationLocation!.FilePath, labelPresentation.SourcePath);
            Equal("Api.smile", Path.GetFileName(labelPresentation.SourcePath));
            if (packageReference)
            {
                var cachePrefix = Path.GetFullPath(Path.Combine(consumerDirectory, "cache"))
                    .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                Equal(true, Path.GetFullPath(labelPresentation.SourcePath).StartsWith(cachePrefix,
                    StringComparison.OrdinalIgnoreCase));
                Equal(false, string.Equals(Path.GetFullPath(labelPresentation.SourcePath),
                    Path.GetFullPath(Path.Combine(fixtureDirectory, "Library", "Api.smile")),
                    StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                Equal(Path.GetFullPath(Path.Combine(fixtureDirectory, "Library", "Api.smile")),
                    Path.GetFullPath(labelPresentation.SourcePath));
            }

            var primaryDotPosition = programText.IndexOf("Primary.Configure", StringComparison.Ordinal) +
                                     "Primary.".Length;
            var publicMemberCompletions = SmileCompletionService.GetCompletions(consumerAnalysis, sourceTree,
                primaryDotPosition);
            Equal("Advance|Caption|Configure|Difference|DrawProbe|Enabled|GameProbe|Label|Mode|Shifted|StoredValue|Total",
                string.Join("|", publicMemberCompletions.Select(completion => completion.DisplayText)));
            Equal(false, publicMemberCompletions.Any(completion => completion.DisplayText is "Hide" or "Secret"));
            var configureOpenPosition = programText.IndexOf("Primary.Configure(", StringComparison.Ordinal) +
                                        "Primary.Configure(".Length;
            var configureNamedCompletions = SmileCompletionService.GetCompletions(consumerAnalysis, sourceTree,
                configureOpenPosition).Where(completion => completion.Kind == SmileCompletionKind.Parameter &&
                    completion.InsertionText.EndsWith(":=", StringComparison.Ordinal)).ToArray();
            Equal("Enabled:=|Label:=|Mode:=|Start:=", string.Join("|", configureNamedCompletions
                .Select(completion => completion.InsertionText)));
            Equal(false, configureNamedCompletions.Any(completion =>
                completion.DisplayText.Contains("Me", StringComparison.OrdinalIgnoreCase) ||
                completion.DisplayText.Contains("Value", StringComparison.OrdinalIgnoreCase)));

            var startLabelPosition = programText.IndexOf("Start:=", StringComparison.Ordinal);
            Equal(true, SmileSymbolService.TryResolve(consumerAnalysis, sourceTree,
                startLabelPosition + 2, out var resolvedStartLabel));
            Equal(SmileResolvedSymbolKind.NamedArgument, resolvedStartLabel.Kind);
            Equal("Start", resolvedStartLabel.Name);
            Equal(configureMethod.Parameters[1].DeclarationLocation.Line,
                resolvedStartLabel.DeclarationLocation!.Line);
            Equal(configureMethod.Parameters[1].DeclarationLocation.Column,
                resolvedStartLabel.DeclarationLocation.Column);
            Equal("Smile.Lightweight.Oop.Proof@1.2.0",
                SmileSymbolDisplayService.Present(resolvedStartLabel,
                    consumerCompilation.DependencyContext).Provider);

            var configureUsePosition = programText.IndexOf("Primary.Configure", StringComparison.Ordinal) +
                                       "Primary.".Length;
            Equal(true, SmileSymbolService.TryResolve(consumerAnalysis, sourceTree,
                configureUsePosition + 2, out var resolvedConfigure));
            Equal(SmileResolvedSymbolKind.Subroutine, resolvedConfigure.Kind);
            var configurePresentation = SmileSymbolDisplayService.Present(resolvedConfigure,
                consumerCompilation.DependencyContext);
            Equal("Sub Smile.Lightweight.Oop.Proof.Counter.Configure(Label As Text, Optional Start As Number = 0, Optional Enabled As Boolean = True, Optional Mode As DisplayMode = DisplayMode.Standard)",
                configurePresentation.Signature);
            Equal("Smile.Lightweight.Oop.Proof@1.2.0", configurePresentation.Provider);
            Equal(configureMethod.DeclarationLocation.Line, resolvedConfigure.DeclarationLocation!.Line);
            Equal(configureMethod.DeclarationLocation.Column, resolvedConfigure.DeclarationLocation.Column);

            var totalUsePosition = programText.IndexOf("Primary.Total", StringComparison.Ordinal) +
                                   "Primary.".Length;
            Equal(true, SmileSymbolService.TryResolve(consumerAnalysis, sourceTree,
                totalUsePosition + 2, out var resolvedTotal));
            Equal(SmileResolvedSymbolKind.Property, resolvedTotal.Kind);
            var totalPresentation = SmileSymbolDisplayService.Present(resolvedTotal,
                consumerCompilation.DependencyContext);
            Equal("Property Smile.Lightweight.Oop.Proof.Counter.Total As Number { Get; Set }",
                totalPresentation.Signature);
            Equal("Smile.Lightweight.Oop.Proof@1.2.0", totalPresentation.Provider);
            Equal(totalProperty.DeclarationLocation.Line, resolvedTotal.DeclarationLocation!.Line);
            Equal(totalProperty.DeclarationLocation.Column, resolvedTotal.DeclarationLocation.Column);

            var gameProbeUsePosition = programText.IndexOf("Primary.GameProbe", StringComparison.Ordinal) +
                                       "Primary.".Length;
            Equal(true, SmileSymbolService.TryResolve(consumerAnalysis, sourceTree,
                gameProbeUsePosition + 2, out var resolvedGameProbe));
            Equal(SmileResolvedSymbolKind.Property, resolvedGameProbe.Kind);
            var gameProbePresentation = SmileSymbolDisplayService.Present(resolvedGameProbe,
                consumerCompilation.DependencyContext);
            Equal("Property get requires Game Window; set does not.", gameProbePresentation.Capability);
            Equal("Smile.Lightweight.Oop.Proof@1.2.0", gameProbePresentation.Provider);

            foreach (var navigation in new[] { resolvedStartLabel, resolvedConfigure, resolvedTotal,
                         resolvedGameProbe, resolvedConstructor, resolvedImplicitConstructor,
                         resolvedClassProperty, resolvedClassField })
            {
                Equal("Api.smile", Path.GetFileName(navigation.DeclarationLocation!.FilePath));
                var navigationPath = Path.GetFullPath(navigation.DeclarationLocation.FilePath);
                if (packageReference)
                {
                    var cachePrefix = Path.GetFullPath(Path.Combine(consumerDirectory, "cache"))
                        .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    Equal(true, navigationPath.StartsWith(cachePrefix, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    Equal(Path.GetFullPath(apiPath), navigationPath);
                }
            }

            var tree = consumerAnalysis.BoundSyntaxTrees.Single(candidate => string.Equals(
                candidate.Source.FilePath, programPath, StringComparison.OrdinalIgnoreCase));
            var calls = tree.Root.Statements.OfType<CallStatementSyntax>()
                .Where(call => consumerAnalysis.SemanticModel.TryGetBoundCall(call, out var bound) &&
                               bound.Routine.Name == "Report").ToArray();
            Equal(3, calls.Length);
            Equal(true, consumerAnalysis.SemanticModel.TryGetBoundCall(calls[0], out var omitted));
            Equal("false|true|true|true|true", string.Join("|", omitted.ParameterArguments
                .Select(argument => argument.IsDefault.ToString().ToLowerInvariant())));
            Equal(true, consumerAnalysis.SemanticModel.TryGetBoundCall(calls[1], out var reordered));
            Equal("Enabled|Label|Mode|Suffix|Copies", string.Join("|", reordered.SourceArguments
                .Select(argument => argument.Syntax!.Name!.Text)));
            Equal("Label|Copies|Enabled|Suffix|Mode", string.Join("|", reordered.ParameterArguments
                .Select(argument => argument.Parameter.Name)));
            Equal(true, consumerAnalysis.SemanticModel.TryGetBoundCall(calls[2], out var mixed));
            Equal("false|false|true|false|true", string.Join("|", mixed.ParameterArguments
                .Select(argument => argument.IsDefault.ToString().ToLowerInvariant())));
            var memberCalls = tree.Root.Statements.OfType<MemberCallStatementSyntax>()
                .Where(call => consumerAnalysis.SemanticModel.TryGetBoundCall(call, out _)).ToArray();
            var configureCall = memberCalls.Single(call =>
                consumerAnalysis.SemanticModel.TryGetBoundCall(call, out var bound) &&
                bound.Routine.Name == "Configure");
            Equal(true, consumerAnalysis.SemanticModel.TryGetBoundCall(configureCall, out var boundConfigure));
            Equal("Mode|Label|Start", string.Join("|", boundConfigure.SourceArguments
                .Select(argument => argument.Syntax!.Name!.Text)));
            Equal("Label|Start|Enabled|Mode", string.Join("|", boundConfigure.ParameterArguments
                .Select(argument => argument.Parameter.Name)));
            Equal("false|false|true|false", string.Join("|", boundConfigure.ParameterArguments
                .Select(argument => argument.IsDefault.ToString().ToLowerInvariant())));
            Equal(true, boundConfigure.InstanceReceiver != null);
            Equal(false, boundConfigure.ParameterArguments.Any(argument => argument.Parameter.Name == "Me"));

            var classInitializer = tree.Root.Statements.OfType<DimStatementSyntax>()
                .Single(dim => dim.Identifier.Text == "Object").NewInitializer!;
            Equal(true, consumerAnalysis.SemanticModel.TryGetBoundCall(classInitializer,
                out var boundConstructor));
            Equal(RoutineSymbolKind.Constructor, boundConstructor.Routine.SymbolKind);
            Equal("Mode|Label|Start", string.Join("|", boundConstructor.SourceArguments
                .Select(argument => argument.Syntax!.Name!.Text)));
            Equal("Label|Start|Mode", string.Join("|", boundConstructor.ParameterArguments
                .Select(argument => argument.Parameter.Name)));
            Equal(false, boundConstructor.HasInstanceReceiver);

            static string SymbolLocation(SourceLocation location) =>
                location.Line + ":" + location.Column + ":" + location.Span.Length;
            var nestedSignature = string.Join("|", publicTypeMembers.Select(member =>
            {
                if (member is TypeRoutineSymbol method)
                    return method.RuntimeIdentity + ":" + method.Routine.RequiresGameWindow + ":" +
                           SymbolLocation(method.DeclarationLocation) + ":" + string.Join(",",
                               method.Routine.Parameters.Select(parameter => parameter.RuntimeIdentity + "@" +
                                   SymbolLocation(parameter.DeclarationLocation)));
                var property = (PropertySymbol)member;
                return property.RuntimeIdentity + ":" + SymbolLocation(property.DeclarationLocation) + ":" +
                       (property.Getter == null ? "-" : property.Getter.RuntimeIdentity + "/" +
                           property.Getter.RequiresGameWindow + "/" + SymbolLocation(property.Getter.DeclarationLocation)) +
                       ":" + (property.Setter == null ? "-" : property.Setter.RuntimeIdentity + "/" +
                           property.Setter.RequiresGameWindow + "/" + SymbolLocation(property.Setter.DeclarationLocation));
            }));
            return string.Join("|", report.Parameters.Select(parameter => parameter.Name + ":" +
                       parameter.IsOptional + ":" + (parameter.DefaultEnumMember?.Name ?? parameter.DefaultValue))) +
                   "||" + counter.RuntimeIdentity + "||" + nestedSignature + "||" +
                   referenceCounter.RuntimeIdentity + "||" + referenceCounter.Constructor.RuntimeIdentity + "||" +
                   string.Join("|", publicClassMembers.Select(member => member.RuntimeIdentity));
        }

        var projectReference = Path.GetRelativePath(Path.Combine(directory, "project-consumer"),
            libraryProjectPath).Replace('/', '\\');
        var packageReference = Path.GetRelativePath(Path.Combine(directory, "package-consumer"),
            firstPackage).Replace('/', '\\');
        var projectSignature = AnalyzeConsumer("project-consumer",
            $"<SmileProjectReference Include=\"{projectReference}\" />", packageReference: false);
        var packageSignature = AnalyzeConsumer("package-consumer",
            $"<SmileLibraryReference Include=\"{packageReference}\" />", packageReference: true);
        Equal(projectSignature, packageSignature);
    }
    finally { Directory.Delete(directory, true); }
});
Run("FormatVersion 6 rejects the dedicated malformed and tampered package matrix", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileFormat6TamperTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var projectPath = Path.GetFullPath("examples/LightweightOopCalls/LightweightOopLibrary.smilelibproj");
        var compilation = SmileProjectCompilation.Load(projectPath, Path.Combine(directory, "build-cache"));
        var analysis = SmileLanguage.Analyze(compilation.Sources, SmileCompilationKind.Library,
            compilation.DependencyContext);
        Equal(false, analysis.HasErrors);
        var baseline = Path.Combine(directory, "baseline.smilelib");
        SmileLibraryPackage.Write(baseline, compilation.Graph.Root, analysis);

        void RebuildArchive(string outputPath, Func<string, string?> mapName,
            Action<System.IO.Compression.ZipArchive>? append = null)
        {
            using var source = System.IO.Compression.ZipFile.OpenRead(baseline);
            using var output = System.IO.Compression.ZipFile.Open(outputPath,
                System.IO.Compression.ZipArchiveMode.Create);
            foreach (var sourceEntry in source.Entries)
            {
                var name = mapName(sourceEntry.FullName);
                if (name == null)
                    continue;
                var outputEntry = output.CreateEntry(name, System.IO.Compression.CompressionLevel.NoCompression);
                using var inputStream = sourceEntry.Open();
                using var outputStream = outputEntry.Open();
                inputStream.CopyTo(outputStream);
            }
            append?.Invoke(output);
        }

        void AssertEnvelope(string name, Func<string, string?> mapName,
            Action<System.IO.Compression.ZipArchive>? append, string expected)
        {
            var package = Path.Combine(directory, name + ".smilelib");
            RebuildArchive(package, mapName, append);
            ThrowsContains(() => SmileLibraryPackage.Read(package, Path.Combine(directory, name + "-cache")), expected);
        }

        AssertEnvelope("missing-manifest", entry => entry == "manifest.json" ? null : entry, null,
            "missing manifest.json");
        AssertEnvelope("missing-api", entry => entry == "api/public-symbols.json" ? null : entry, null,
            "missing api/public-symbols.json");
        AssertEnvelope("duplicate-entry", entry => entry, archive =>
        {
            using var writer = new StreamWriter(archive.CreateEntry("manifest.json").Open());
            writer.Write("{}");
        }, "Duplicate SMILE library archive entry");
        AssertEnvelope("unexpected-payload", entry => entry, archive =>
        {
            using var writer = new StreamWriter(archive.CreateEntry("bin/native.exe").Open());
            writer.Write("payload");
        }, "Unexpected executable or package payload entry");
        AssertEnvelope("unsafe-traversal", entry => entry, archive =>
        {
            using var writer = new StreamWriter(archive.CreateEntry("src/../escape.smile").Open());
            writer.Write("Module Escape\nEnd Module\n");
        }, "Unsafe SMILE library archive path");
        AssertEnvelope("absolute-source", entry => entry == "src/Library/Api.smile"
                ? "C:/cache/Api.smile"
                : entry,
            null, "Unsafe SMILE library archive path");
        AssertEnvelope("nonnormal-source", entry => entry == "src/Library/Api.smile"
                ? "src//Api.smile"
                : entry,
            null, "Unsafe SMILE library archive path");

        var unsupported = Path.Combine(directory, "unsupported-format.smilelib");
        File.Copy(baseline, unsupported);
        RewriteManifest(unsupported, text => ReplaceOnce(text, "\"formatVersion\": 6", "\"formatVersion\": 5"));
        ThrowsContains(() => SmileLibraryPackage.Read(unsupported, Path.Combine(directory, "unsupported-cache")),
            "no longer supported");

        var wrongProvider = Path.Combine(directory, "wrong-provider.smilelib");
        File.Copy(baseline, wrongProvider);
        RewriteManifest(wrongProvider, text => ReplaceOnce(text,
            "Smile.Lightweight.Oop.Proof@1.2.0", "Smile.Lightweight.Oop.Proof@9.9.9"));
        ThrowsContains(() => SmileLibraryPackage.Read(wrongProvider, Path.Combine(directory, "provider-cache")),
            "canonical identity");

        var badHash = Path.Combine(directory, "bad-source-hash.smilelib");
        File.Copy(baseline, badHash);
        RewritePackageTextEntry(badHash, "src/Library/Api.smile", text => text + "\n' tampered\n");
        ThrowsContains(() => SmileLibraryPackage.Read(badHash, Path.Combine(directory, "hash-cache")),
            "source hash is invalid");

        var mutations = new (string Name, string OldText, string NewText)[]
        {
            ("api-library-identity", "\"name\": \"Smile.Lightweight.Oop.Proof\", \"version\": \"1.2.0\"",
                "\"name\": \"Foreign.Proof\", \"version\": \"1.2.0\""),
            ("undeclared-provider", "\"provider\": \"Smile.Lightweight.Oop.Proof@1.2.0\"",
                "\"provider\": \"Undeclared.Provider@1.0.0\""),
            ("transitive-provider-as-direct", "Smile.Lightweight.Oop.Proof::Counter\", \"module\"",
                "Transitive.Base::Counter\", \"module\""),
            ("absolute-api-source", "\"source\": \"src/Library/Api.smile\"",
                "\"source\": \"C:/cache/Api.smile\""),
            ("extraction-cache-source", "\"source\": \"src/Library/Api.smile\"",
                "\"source\": \"src/obj/packages/cache/Api.smile\""),
            ("nonnormal-api-source", "\"source\": \"src/Library/Api.smile\"",
                "\"source\": \"src/Library/../Api.smile\""),
            ("outside-package-source", "\"source\": \"src/Library/Api.smile\"",
                "\"source\": \"src/Missing.smile\""),
            ("line-out-of-range", "\"line\": 29, \"column\": 5", "\"line\": 99999, \"column\": 5"),
            ("column-out-of-range", "\"line\": 29, \"column\": 5", "\"line\": 29, \"column\": 99999"),

            ("enum-duplicate-member", "\"name\": \"CompactAlias\", \"value\": 2",
                "\"name\": \"Compact\", \"value\": 2"),
            ("enum-malformed-signed-value", "\"name\": \"Standard\", \"value\": 1",
                "\"name\": \"Standard\", \"value\": \"-1x\""),
            ("enum-containing-identity", "Smile.Lightweight.Oop.Proof::DisplayMode\", \"module\"",
                "Smile.Lightweight.Oop.Proof::OtherMode\", \"module\""),
            ("enum-noncanonical-order", "\"name\": \"Standard\", \"value\": 1, \"ordinal\": 0",
                "\"name\": \"Standard\", \"value\": 1, \"ordinal\": 2"),
            ("enum-default-missing-member", "\"kind\": \"enum\", \"member\": \"Standard\", \"value\": 1",
                "\"kind\": \"enum\", \"member\": \"Missing\", \"value\": 1"),
            ("enum-default-value-mismatch", "\"kind\": \"enum\", \"member\": \"CompactAlias\", \"value\": 2",
                "\"kind\": \"enum\", \"member\": \"CompactAlias\", \"value\": 99"),

            ("type-duplicate-field", "\"name\": \"StoredValue\", \"visibility\": \"Public\"",
                "\"name\": \"Label\", \"visibility\": \"Public\""),
            ("type-field-ordinal", "\"ordinal\": 1, \"offset\": 8", "\"ordinal\": 7, \"offset\": 8"),
            ("type-field-offset", "\"ordinal\": 1, \"offset\": 8", "\"ordinal\": 1, \"offset\": 9"),
            ("type-size", "\"provider\": \"Smile.Lightweight.Oop.Proof@1.2.0\", \"size\": 32",
                "\"provider\": \"Smile.Lightweight.Oop.Proof@1.2.0\", \"size\": 40"),
            ("type-method-containing-type", "Counter::member::Configure", "CounterBox::member::Configure"),
            ("type-private-member-leak", "\"name\": \"Advance\", \"kind\": \"Subroutine\"",
                "\"name\": \"Hide\", \"kind\": \"Subroutine\""),
            ("type-hidden-me-parameter", "\"name\": \"Delta\", \"type\": {\"kind\": \"primitive\", \"name\": \"Number\"}",
                "\"name\": \"Me\", \"type\": {\"kind\": \"primitive\", \"name\": \"Number\"}"),
            ("property-missing-getter", "\"get\": {\"identity\": \"Smile.Lightweight.Oop.Proof::Counter::property::Caption::get\"",
                "\"missingGet\": {\"identity\": \"Smile.Lightweight.Oop.Proof::Counter::property::Caption::get\""),
            ("property-missing-setter", "\"set\": {\"identity\": \"Smile.Lightweight.Oop.Proof::Counter::property::Total::set\"",
                "\"missingSet\": {\"identity\": \"Smile.Lightweight.Oop.Proof::Counter::property::Total::set\""),
            ("property-capability", "Counter::property::GameProbe::get\", \"requiresGameWindow\": true",
                "Counter::property::GameProbe::get\", \"requiresGameWindow\": false"),

            ("class-missing-constructor", "\"constructor\": {\"identity\": \"Smile.Lightweight.Oop.Proof::ReferenceCounter::constructor::New\"",
                "\"missingConstructor\": {\"identity\": \"Smile.Lightweight.Oop.Proof::ReferenceCounter::constructor::New\""),
            ("class-duplicate-constructor", "\"constructor\": {\"identity\": \"Smile.Lightweight.Oop.Proof::ReferenceCounter::constructor::New\"",
                "\"constructor\": null, \"constructor\": {\"identity\": \"Smile.Lightweight.Oop.Proof::ReferenceCounter::constructor::New\""),
            ("class-implicit-constructor", "EmptyReference::constructor::New\", \"visibility\": \"Public\", \"declared\": false",
                "EmptyReference::constructor::New\", \"visibility\": \"Public\", \"declared\": true"),
            ("class-field-layout", "\"dimensions\": [2], \"ordinal\": 1", "\"dimensions\": [3], \"ordinal\": 1"),
            ("class-private-member-leak", "\"name\": \"Snapshot\", \"kind\": \"Function\"",
                "\"name\": \"Hide\", \"kind\": \"Function\""),
            ("class-hidden-me-parameter", "\"name\": \"Label\", \"type\": {\"kind\": \"primitive\", \"name\": \"Text\"}, \"mode\": \"ByVal\"",
                "\"name\": \"Me\", \"type\": {\"kind\": \"primitive\", \"name\": \"Text\"}, \"mode\": \"ByVal\""),
            ("class-setter-value-parameter", "ReferenceCounter::property::Total::set\", \"requiresGameWindow\": false",
                "ReferenceCounter::property::Total::set\", \"parameters\": [{\"name\": \"Value\"}], \"requiresGameWindow\": false"),
            ("class-runtime-identity-collision", "ReferenceCounter::member::Snapshot", "ReferenceCounter::member::Alias"),
            ("class-wrong-declaration-kind", "\"name\": \"ReferenceCounter\", \"kind\": \"Class\"",
                "\"name\": \"ReferenceCounter\", \"kind\": \"Type\""),
            ("class-reference-field", "\"name\": \"Code\", \"visibility\": \"Public\", \"type\": {\"kind\": \"primitive\", \"name\": \"Number\"}",
                "\"name\": \"Code\", \"visibility\": \"Public\", \"type\": {\"kind\": \"class\", \"name\": \"ReferenceCounter\"}"),

            ("optional-required-after-optional", "\"name\": \"Enabled\", \"type\": {\"kind\": \"primitive\", \"name\": \"Boolean\"}, \"mode\": \"ByVal\", \"optional\": true",
                "\"name\": \"Enabled\", \"type\": {\"kind\": \"primitive\", \"name\": \"Boolean\"}, \"mode\": \"ByVal\", \"optional\": false"),
            ("optional-byref", "\"name\": \"Start\", \"type\": {\"kind\": \"primitive\", \"name\": \"Number\"}, \"mode\": \"ByVal\", \"optional\": true",
                "\"name\": \"Start\", \"type\": {\"kind\": \"primitive\", \"name\": \"Number\"}, \"mode\": \"ByRef\", \"optional\": true"),
            ("optional-default-type", "\"default\": {\"kind\": \"number\", \"value\": 0}, \"ordinal\": 1",
                "\"default\": {\"kind\": \"text\", \"value\": \"zero\"}, \"ordinal\": 1"),
            ("optional-default-missing", "\"optional\": true, \"default\": {\"kind\": \"number\", \"value\": 1}",
                "\"optional\": true, \"default\": null"),
            ("optional-text-encoding", "\"kind\": \"text\", \"value\": \"!\"",
                "\"kind\": \"text\", \"value\": \"\\uD800\""),
            ("optional-unsafe-web-number", "\"kind\": \"number\", \"value\": 3",
                "\"kind\": \"number\", \"value\": 9007199254740992"),
            ("parameter-name-mismatch", "\"name\": \"Copies\", \"type\": {\"kind\": \"primitive\", \"name\": \"Number\"}",
                "\"name\": \"CopyCount\", \"type\": {\"kind\": \"primitive\", \"name\": \"Number\"}"),
            ("parameter-order-mismatch", "\"name\": \"Copies\", \"type\": {\"kind\": \"primitive\", \"name\": \"Number\"}, \"mode\": \"ByVal\", \"optional\": true, \"default\": {\"kind\": \"number\", \"value\": 3}, \"ordinal\": 1",
                "\"name\": \"Copies\", \"type\": {\"kind\": \"primitive\", \"name\": \"Number\"}, \"mode\": \"ByVal\", \"optional\": true, \"default\": {\"kind\": \"number\", \"value\": 3}, \"ordinal\": 9")
        };

        foreach (var mutation in mutations)
        {
            var package = Path.Combine(directory, mutation.Name + ".smilelib");
            File.Copy(baseline, package);
            RewritePackageTextEntry(package, "api/public-symbols.json", text =>
                ReplaceOnce(text, mutation.OldText, mutation.NewText));
            ThrowsProjectDiagnostic(() => SmileLibraryPackage.Read(package,
                Path.Combine(directory, mutation.Name + "-cache")), "SML3207");
        }
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
Run("Compiler diagnostic parsing preserves warnings errors and Windows paths", () =>
{
    var output = "tool preamble\r\n" +
                 "C:\\work area\\copy (1)\\Program.smile(12,34): warning SML1234: Check this value.\r\n" +
                 "C:\\work area\\Program.smile(5,6): error SML5678: Build failed.\r\n" +
                 "C:\\ignored.smile(x,1): warning SML9999: malformed\r\n";
    var diagnostics = SmileCompilerDiagnosticParser.Parse(output);
    Equal(2, diagnostics.Count);
    Equal(Path.GetFullPath("C:\\work area\\copy (1)\\Program.smile"), diagnostics[0].FilePath);
    Equal(12, diagnostics[0].Line);
    Equal(34, diagnostics[0].Column);
    Equal("SML1234", diagnostics[0].Code);
    Equal("Check this value.", diagnostics[0].Message);
    Equal(DiagnosticSeverity.Warning, diagnostics[0].Severity);
    Equal(DiagnosticSeverity.Error, diagnostics[1].Severity);
});
Run("Native build flags keep runtime protection and constrain the custom-entry exception", () =>
{
    var project = File.ReadAllText("src/Smile.NativeRuntime/Smile.NativeRuntime.vcxproj");
    Equal(2, project.Split("<BufferSecurityCheck>true</BufferSecurityCheck>",
        StringSplitOptions.None).Length - 1);
    Equal(true, project.Contains("<SDLCheck>true</SDLCheck>", StringComparison.Ordinal));
    Equal(true, project.Contains("<RuntimeLibrary>MultiThreadedDLL</RuntimeLibrary>",
        StringComparison.Ordinal));
    Equal(true, project.Contains("custom-entry link uses the DLL CRT import libraries",
        StringComparison.Ordinal));

    var toolchain = File.ReadAllText("src/Smile.Compiler/NativeToolchain.cs");
    Equal(true, toolchain.Contains("/GS- /Fo", StringComparison.Ordinal));
    Equal(true, toolchain.Contains("constrained to this generated, buffer-free helper",
        StringComparison.Ordinal));
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
Run("SMILE library package resource limits reject every bounded package dimension", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileLibraryResourceTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var projectPath = Path.GetFullPath("libraries/Smile.Text.Extras/Smile.Text.Extras.smilelibproj");
        var compilation = SmileProjectCompilation.Load(projectPath, Path.Combine(directory, "build-cache"));
        var analysis = SmileLanguage.Analyze(compilation.Sources, SmileCompilationKind.Library,
            compilation.DependencyContext);
        Equal(false, analysis.HasErrors);
        var package = Path.Combine(directory, "Normal.smilelib");
        SmileLibraryPackage.Write(package, compilation.Graph.Root, analysis);
        Equal(compilation.Graph.Root.LibraryName,
            SmileLibraryPackage.Read(package, Path.Combine(directory, "normal-cache")).Identity.Name);

        long physicalBytes;
        int entryCount;
        int longestName;
        int manifestBytes;
        int publicApiBytes;
        int maximumSourceBytes;
        int sourceCount;
        long expandedBytes;
        using (var archive = System.IO.Compression.ZipFile.OpenRead(package))
        {
            physicalBytes = new FileInfo(package).Length;
            entryCount = archive.Entries.Count;
            longestName = archive.Entries.Max(entry => entry.FullName.Length);
            manifestBytes = checked((int)archive.GetEntry("manifest.json")!.Length);
            publicApiBytes = checked((int)archive.GetEntry("api/public-symbols.json")!.Length);
            var sources = archive.Entries.Where(entry => entry.FullName.StartsWith("src/", StringComparison.Ordinal))
                .ToArray();
            maximumSourceBytes = checked((int)sources.Max(entry => entry.Length));
            sourceCount = sources.Length;
            expandedBytes = archive.Entries.Sum(entry => entry.Length);
        }

        SmileLibraryResourcePolicy Policy(long? physical = null, int? entries = null, int? name = null,
            int? manifest = null, int? api = null, int? source = null, int? sources = null,
            long? expanded = null) => new(
            physical ?? physicalBytes, entries ?? entryCount, name ?? longestName,
            manifest ?? manifestBytes, api ?? publicApiBytes, source ?? maximumSourceBytes,
            sources ?? sourceCount, expanded ?? expandedBytes);

        ThrowsProjectDiagnostic(() => SmileLibraryPackage.ReadIdentity(package,
            Policy(physical: physicalBytes - 1)), SmileLibraryPackage.ResourceLimitDiagnosticCode);
        ThrowsProjectDiagnostic(() => SmileLibraryPackage.ReadIdentity(package,
            Policy(entries: entryCount - 1)), SmileLibraryPackage.ResourceLimitDiagnosticCode);
        ThrowsProjectDiagnostic(() => SmileLibraryPackage.ReadIdentity(package,
            Policy(name: longestName - 1)), SmileLibraryPackage.ResourceLimitDiagnosticCode);
        ThrowsProjectDiagnostic(() => SmileLibraryPackage.ReadIdentity(package,
            Policy(manifest: manifestBytes - 1)), SmileLibraryPackage.ResourceLimitDiagnosticCode);
        ThrowsProjectDiagnostic(() => SmileLibraryPackage.ReadEnvelope(package,
            Path.Combine(directory, "api-limit-cache"), Policy(api: publicApiBytes - 1)),
            SmileLibraryPackage.ResourceLimitDiagnosticCode);
        ThrowsProjectDiagnostic(() => SmileLibraryPackage.ReadEnvelope(package,
            Path.Combine(directory, "source-limit-cache"), Policy(source: maximumSourceBytes - 1)),
            SmileLibraryPackage.ResourceLimitDiagnosticCode);
        ThrowsProjectDiagnostic(() => SmileLibraryPackage.ReadEnvelope(package,
            Path.Combine(directory, "source-count-cache"), Policy(sources: sourceCount - 1)),
            SmileLibraryPackage.ResourceLimitDiagnosticCode);
        var rejectedCache = Path.Combine(directory, "expanded-limit-cache");
        ThrowsProjectDiagnostic(() => SmileLibraryPackage.ReadEnvelope(package, rejectedCache,
            Policy(expanded: expandedBytes - 1)), SmileLibraryPackage.ResourceLimitDiagnosticCode);
        Equal(false, Directory.Exists(rejectedCache));

        var boundary = SmileLibraryPackage.ReadEnvelope(package, Path.Combine(directory, "boundary-cache"),
            Policy());
        Equal(compilation.Graph.Root.LibraryName, boundary.Identity.Name);

        var compressed = Path.Combine(directory, "CompressedOversize.smilelib");
        using (var archive = System.IO.Compression.ZipFile.Open(compressed,
                   System.IO.Compression.ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("manifest.json", System.IO.Compression.CompressionLevel.Optimal);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(new string('A', 4096));
        }
        var compressedPolicy = new SmileLibraryResourcePolicy(1024 * 1024, 3, 512, 128, 128, 128, 1, 384);
        ThrowsProjectDiagnostic(() => SmileLibraryPackage.ReadIdentity(compressed, compressedPolicy),
            SmileLibraryPackage.ResourceLimitDiagnosticCode);
    }
    finally { Directory.Delete(directory, true); }
});
Run("SMILE library publication preserves prior output and serializes same-target writers", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileLibraryPublishTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var projectPath = Path.GetFullPath("libraries/Smile.Text.Extras/Smile.Text.Extras.smilelibproj");
        var compilation = SmileProjectCompilation.Load(projectPath, Path.Combine(directory, "build-cache"));
        var analysis = SmileLanguage.Analyze(compilation.Sources, SmileCompilationKind.Library,
            compilation.DependencyContext);
        var output = Path.Combine(directory, "Library.smilelib");
        SmileLibraryPackage.Write(output, compilation.Graph.Root, analysis);
        var priorBytes = File.ReadAllBytes(output);
        ThrowsContains(() => SmileLibraryPackage.Write(output, compilation.Graph.Root, analysis,
            SmileLibraryResourcePolicy.Production, TimeSpan.FromSeconds(5),
            _ => throw new IOException("Synthetic package publication failure.")), "Synthetic package");
        Equal(true, priorBytes.SequenceEqual(File.ReadAllBytes(output)));
        Equal(0, Directory.EnumerateFiles(directory, "*.tmp").Count());

        using var firstReady = new ManualResetEventSlim(false);
        using var releaseFirst = new ManualResetEventSlim(false);
        var firstWriter = Task.Run(() => SmileLibraryPackage.Write(output, compilation.Graph.Root, analysis,
            SmileLibraryResourcePolicy.Production, TimeSpan.FromSeconds(5), _ =>
            {
                firstReady.Set();
                releaseFirst.Wait(TimeSpan.FromSeconds(5));
            }));
        Equal(true, firstReady.Wait(TimeSpan.FromSeconds(5)));
        try
        {
            ThrowsProjectDiagnostic(() => SmileLibraryPackage.Write(output, compilation.Graph.Root, analysis,
                SmileLibraryResourcePolicy.Production, TimeSpan.FromMilliseconds(100)),
                SmileLibraryPackage.OutputLockDiagnosticCode);
            var independent = Path.Combine(directory, "Independent.smilelib");
            SmileLibraryPackage.Write(independent, compilation.Graph.Root, analysis,
                SmileLibraryResourcePolicy.Production, TimeSpan.FromSeconds(2));
            Equal(true, File.Exists(independent));
        }
        finally
        {
            releaseFirst.Set();
            firstWriter.GetAwaiter().GetResult();
        }
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
    context.AddProvider("official", SmileProviderKind.Package, "Smile.UI", "2.0.0", "Smile.UI.smilelib");
    context.AddProvider("student", SmileProviderKind.Project, "Student.Tools", "1.0.0",
        "Student.Tools.smilelibproj");
    Equal(true, context.TryGetProviderDescriptor("official", out var official));
    Equal(true, official.IsBuiltIn);
    Equal(true, official.Describe().Contains("SMILE 2.0 built-in library", StringComparison.Ordinal));
    Equal("SMILE 2.0 built-in library Smile.UI@2.0.0",
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
Run("Multiline routine declarations preserve parameters and physical source lines", () =>
{
    const string source = "Option Explicit\n" +
        "Sub Present(\n" +
        "    ByRef\n" +
        "    Caption\n" +
        "    As\n" +
        "    Text, ' Keep the declaration comment.\n" +
        "\n" +
        "    Amount As Number\n" +
        ")\n" +
        "    Print Caption\n" +
        "    Print Amount\n" +
        "End Sub\n" +
        "Function Add(\n" +
        "    LeftValue As Number\n" +
        "    ,\n" +
        "    RightValue As Number\n" +
        ") As Number\n" +
        "    Return LeftValue + RightValue\n" +
        "End Function\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    Equal(false, Analyze(source.Replace("\n", "\r\n", StringComparison.Ordinal)).HasErrors);

    var routines = analysis.SyntaxTree.Root.Statements.OfType<RoutineDeclarationSyntax>().ToArray();
    Equal(2, routines.Length);
    var present = routines[0];
    Equal(SyntaxKind.OpenParenthesisToken, present.OpenParenthesis!.Kind);
    Equal(SyntaxKind.CloseParenthesisToken, present.CloseParenthesis!.Kind);
    Equal(2, present.Parameters.Count);
    Equal(SyntaxKind.ByRefKeyword, present.Parameters[0].ModeKeyword!.Kind);
    Equal("Caption", present.Parameters[0].Identifier.Text);
    Equal("Text", present.Parameters[0].TypeToken!.Text);
    Equal("Amount", present.Parameters[1].Identifier.Text);
    Equal(4, new SourceLocation(analysis.SyntaxTree.Source, present.Parameters[0].Identifier.Span).Line);
    Equal(8, new SourceLocation(analysis.SyntaxTree.Source, present.Parameters[1].Identifier.Span).Line);
    Equal(9, new SourceLocation(analysis.SyntaxTree.Source, present.CloseParenthesis.Span).Line);
    var boundPresent = analysis.BoundSyntaxTree.Root.Statements.OfType<RoutineDeclarationSyntax>()
        .Single(routine => routine.Identifier.Text == "Present");
    Equal(present.OpenParenthesis.Span.Start, boundPresent.OpenParenthesis!.Span.Start);
    Equal(present.CloseParenthesis.Span.Start, boundPresent.CloseParenthesis!.Span.Start);
    var caption = ResolveSymbol(analysis, analysis.SyntaxTree, present.Parameters[0].Identifier.Span.Start);
    Equal(SmileResolvedSymbolKind.Parameter, caption.Kind);
    Equal(4, caption.DeclarationLocation!.Line);

    var add = routines[1];
    Equal(2, add.Parameters.Count);
    Equal("Number", add.ReturnTypeToken!.Text);
    Equal(17, new SourceLocation(analysis.SyntaxTree.Source, add.CloseParenthesis!.Span).Line);
    Equal(17, new SourceLocation(analysis.SyntaxTree.Source, add.ReturnTypeToken.Span).Line);
    Equal(2, analysis.SemanticModel.Routines.Values.Single(routine => routine.Name == "Add").Parameters.Count);
    Equal(true, new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, false).Emit()
        .Contains("smile_print_number", StringComparison.Ordinal));
    Equal(true, new WebEmitter(analysis).Emit().Contains("smile.print", StringComparison.Ordinal));
});
Run("Malformed multiline routine declarations recover at physical source lines", () =>
{
    const string missingComma = "Sub Work(\n    First As Number\n    Second As Number\n)\nEnd Sub\n";
    var commaDiagnostics = Analyze(missingComma).Diagnostics
        .Where(diagnostic => diagnostic.Code == "SML2001").ToArray();
    Equal(1, commaDiagnostics.Length);
    Equal(3, commaDiagnostics[0].Line);
    Equal(5, commaDiagnostics[0].Column);
    Equal("Expected comma between routine parameters, found 'Second'.", commaDiagnostics[0].Message);

    const string missingClose = "Sub Work(\n    Value As Number\nPrint Value\nEnd Sub\n";
    var closeDiagnostics = Analyze(missingClose).Diagnostics
        .Where(diagnostic => diagnostic.Code == "SML2001").ToArray();
    Equal(1, closeDiagnostics.Length);
    Equal(3, closeDiagnostics[0].Line);
    Equal(1, closeDiagnostics[0].Column);
    Equal("Expected ), found 'Print'.", closeDiagnostics[0].Message);
});
Run("Routine declaration continuation stays inside balanced parentheses", () =>
{
    Equal(true, Analyze("Sub Work\n    Value As Number\nEnd Sub\n").HasErrors);
    Equal(true, Analyze("Function Work(\n)\nAs Number\nReturn 1\nEnd Function\n").HasErrors);
    Equal(true, Analyze("Sub Work(\n    Values[2] As Number\n)\nEnd Sub\n").HasErrors);
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
        .Count(statement => statement is CallStatementSyntax or QualifiedCallStatementSyntax or
            MemberCallStatementSyntax));
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
        "Dim Values[\n2]\n"
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
Run("FormatVersion 6 packages contain deterministic typed public API metadata", () =>
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
            Equal(true, manifestReader.ReadToEnd().Contains("\"formatVersion\": 6", StringComparison.Ordinal));
            using var apiReader = new StreamReader(archive.GetEntry("api/public-symbols.json")!.Open());
            var api = apiReader.ReadToEnd();
            Equal(true, api.Contains("\"type\": {\"kind\": \"primitive\", \"name\": \"Text\"}",
                StringComparison.Ordinal));
            Equal(true, api.Contains("\"mode\": \"ByRef\"", StringComparison.Ordinal));
            Equal(true, api.Contains("\"mode\": \"ByVal\"", StringComparison.Ordinal));
            Equal(true, api.Contains("\"returnType\": {\"kind\": \"primitive\", \"name\": \"Text\"}",
                StringComparison.Ordinal));
            Equal(true, api.Contains("\"optional\": false, \"default\": null", StringComparison.Ordinal));
            Equal(false, api.Contains("Hidden", StringComparison.Ordinal));
        }
        for (var formatVersion = 1; formatVersion <= 5; formatVersion++)
        {
            var legacy = Path.Combine(directory, $"legacy-{formatVersion}.smilelib");
            File.Copy(first, legacy);
            RewriteManifest(legacy, manifest => manifest.Replace("\"formatVersion\": 6",
                $"\"formatVersion\": {formatVersion}", StringComparison.Ordinal));
            ThrowsContains(() => SmileLibraryPackage.ReadIdentity(legacy), "no longer supported");
            var diagnostic = ThrowsProjectDiagnostic(() => SmileLibraryProviderResolver.LoadPackages(
                new[] { legacy }, Path.Combine(directory, $"legacy-cache-{formatVersion}")), "SML3206");
            Equal(true, diagnostic.Message.Contains("rebuild", StringComparison.OrdinalIgnoreCase));
            Equal(true, diagnostic.Message.Contains("expected formatVersion 6", StringComparison.Ordinal));
        }
        var unknown = Path.Combine(directory, "unknown.smilelib");
        File.Copy(first, unknown);
        RewriteManifest(unknown, manifest => manifest.Replace("\"formatVersion\": 6",
            "\"formatVersion\": 7", StringComparison.Ordinal));
        ThrowsContains(() => SmileLibraryPackage.ReadIdentity(unknown), "expected 6");
    }
    finally { Directory.Delete(directory, true); }
});
Run("FormatVersion 6 packages preserve direct and transitive Game Window capabilities", () =>
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

Run("FormatVersion 6 public API metadata preserves Image signatures", () =>
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
        Equal(true, api.Contains("\"type\": {\"kind\": \"primitive\", \"name\": \"Image\"}",
            StringComparison.Ordinal));
        Equal(true, api.Contains("\"returnType\": {\"kind\": \"primitive\", \"name\": \"Boolean\"}",
            StringComparison.Ordinal));
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

Run("Enum declarations bind checked Const values aliases contextual names and parser recovery", () =>
{
    const string source = "Option Explicit\nEnum Direction\nNone = ENUM_BASE\nUp\nDown = Abs(-10)\nLeft = -1\nRight = -1\nEnd Enum\nConst ENUM_BASE = 5\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var declaration = analysis.SyntaxTree.Root.Statements.OfType<EnumDeclarationSyntax>().Single();
    Equal("None|Up|Down|Left|Right", string.Join("|", declaration.Members.Select(member => member.Identifier.Text)));
    var type = analysis.SemanticModel.EnumTypes["Direction"];
    Equal(SmileTypeKind.Enum, type.Kind);
    Equal(8, type.Size);
    Equal("5|6|10|-1|-1", string.Join("|", type.Members.Select(member => member.Value)));

    var recovered = Analyze("Enum Direction\nEnd Type\nUp\nEnd Enum\nDim Value As Direction\n");
    Equal(true, HasDiagnostic(recovered, "SML3421"));
    Equal(SmileTypeKind.Enum, recovered.SemanticModel.Symbols["Value"].Type.Kind);
    Equal("Up", recovered.SemanticModel.EnumTypes["Direction"].Members.Single().Name);
    Equal(true, HasDiagnostic(Analyze("Enum Empty\nEnd Enum\n"), "SML3421"));
    Equal(true, HasDiagnostic(Analyze(
        "Enum Limit\nMaximum = 9223372036854775807\nOverflow\nEnd Enum\n"), "SML3422"));
    Equal(true, HasDiagnostic(Analyze(
        "Enum Limit\nOverflow = TOO_LARGE\nEnd Enum\nConst TOO_LARGE = 9223372036854775807 + 1\n"),
        "SML3422"));
    Equal(true, HasDiagnostic(Analyze(
        "Enum Direction\nUp\nDown = Direction.Up + 1\nEnd Enum\n"), "SML3422"));
});

Run("Enum exact typing spans records arrays ByRef returns Select constants and both emitters", () =>
{
    const string source = "Option Explicit\nEnum State\nNone\nReady = 7\nMaximum = 9223372036854775807\nMinimum = -9223372036854775807 - 1\nAlias = 7\nEnd Enum\nConst DEFAULT_STATE = State.None\nConst LOWEST_STATE = State.Minimum\nType Holder\nValue As State\nEnd Type\nDim Current As Holder\nDim Values[2] As State\nDim Selected As State\nCurrent.Value = DEFAULT_STATE\nValues[0] = Current.Value\nValues[1] = LOWEST_STATE\nCall SetState(Values[0], State.Ready)\nSelected = ReadState(Current)\nIf State.Minimum = State.Minimum Then\nSelected = State.Maximum\nEnd If\nSelect Case Values[0]\nCase State.Ready\nSelected = State.Ready\nCase Else\nSelected = State.None\nEnd Select\nSub SetState(ByRef Value As State, NewValue As State)\nValue = NewValue\nEnd Sub\nFunction ReadState(Value As Holder) As State\nReturn Value.Value\nEnd Function\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var state = analysis.SemanticModel.EnumTypes["State"];
    Equal(state, analysis.SemanticModel.Symbols["Values"].Type);
    Equal(state, analysis.SemanticModel.Types["Holder"].Fields.Single().Type);
    Equal(state, analysis.SemanticModel.Routines.Values.Single(routine => routine.Name == "ReadState").ReturnType);
    Equal(ParameterPassingMode.ByRef, analysis.SemanticModel.Routines.Values
        .Single(routine => routine.Name == "SetState").Parameters[0].ParameterMode);
    Equal(state, analysis.SemanticModel.Symbols["DEFAULT_STATE"].Type);

    var nativeEmitter = new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, true);
    var native = nativeEmitter.Emit();
    Equal(true, native.Contains("08000000000000000h", StringComparison.Ordinal));
    Equal(true, native.Split(new[] { "08000000000000000h" }, StringSplitOptions.None).Length >= 4);
    Equal(true, native.Contains("07FFFFFFFFFFFFFFFh", StringComparison.Ordinal));
    Equal(true, CompilerDriver.BuildDebugSource(nativeEmitter.DebugSites)
        .Contains("long long Selected", StringComparison.Ordinal));
    var web = new WebEmitter(analysis).Emit();
    Equal(true, web.Contains("-9223372036854775808n", StringComparison.Ordinal));
    Equal(true, web.Contains("9223372036854775807n", StringComparison.Ordinal));
    Equal(true, web.Contains("smile.array([2], 0n)", StringComparison.Ordinal));
    Equal(true, web.Contains("const g_0_default_state = 0n", StringComparison.Ordinal));
});

Run("Enum identity rejects conversion arithmetic cross-type equality and duplicate Select aliases", () =>
{
    Equal(true, HasDiagnostic(Analyze(
        "Enum Direction\nUp\nEnd Enum\nDim Value As Direction\nValue = 0\n"), "SML3304"));
    Equal(true, HasDiagnostic(Analyze(
        "Enum Direction\nUp\nEnd Enum\nDim Value As Direction\nValue = Direction.Up + Direction.Up\n"),
        "SML3424"));
    Equal(true, HasDiagnostic(Analyze(
        "Enum First\nReady\nEnd Enum\nEnum Second\nReady\nEnd Enum\nDim A As First\nDim B As Second\nIf A = B Then\nPrint 1\nEnd If\n"),
        "SML3424"));
    Equal(true, HasDiagnostic(Analyze(
        "Enum Direction\nLeft = -1\nRight = -1\nEnd Enum\nDim Value As Direction\nSelect Case Value\nCase Direction.Left\nPrint 1\nCase Direction.Right\nPrint 2\nEnd Select\n"),
        "SML3019"));
    Equal(true, HasDiagnostic(Analyze(
        "Enum Direction\nUp\nEnd Enum\nDim Value As Direction\nValue = Direction.Missing\n"),
        "SML3423"));
});

Run("Enum completion Quick Info definition and implicit inference retain nominal identity", () =>
{
    const string source = "Enum Direction\nNone\nUp\nDown\nLeft = 3\nRight = 4\nEnd Enum\nConst DEFAULT_DIRECTION = Direction.Left\nType Holder\nValue As Direction\nEnd Type\nDim Items[1] As Direction\nDim Current As Holder\nFromArray = Items[0]\nFromField = Current.Value\nFunction ReadArray()\nReturn Items[0]\nEnd Function\nFunction ReadField()\nReturn Current.Value\nEnd Function\nDim Selected As Direction\nSelected = Direction.Left\nSelected = Direction.\n";
    var analysis = Analyze(source);
    Equal(SmileTypeKind.Enum, analysis.SemanticModel.Symbols["FromArray"].Type.Kind);
    Equal(SmileTypeKind.Enum, analysis.SemanticModel.Symbols["FromField"].Type.Kind);
    Equal(SmileTypeKind.Enum, analysis.SemanticModel.Routines.Values.Single(routine => routine.Name == "ReadArray").ReturnType.Kind);
    Equal(SmileTypeKind.Enum, analysis.SemanticModel.Routines.Values.Single(routine => routine.Name == "ReadField").ReturnType.Kind);
    var ordinaryPosition = source.IndexOf("Dim Selected", StringComparison.Ordinal);
    Equal(true, SmileCompletionService.GetCompletions(analysis, ordinaryPosition)
        .Any(completion => completion.DisplayText == "Direction" && completion.Kind == SmileCompletionKind.Type));
    var completionPosition = source.LastIndexOf("Direction.", StringComparison.Ordinal) + "Direction.".Length;
    Equal("None|Up|Down|Left|Right", string.Join("|",
        SmileCompletionService.GetCompletions(analysis, completionPosition)
            .Select(completion => completion.DisplayText)));
    var memberUse = source.IndexOf("Direction.Left", StringComparison.Ordinal) + "Direction.".Length;
    var member = ResolveSymbol(analysis, analysis.SyntaxTree, memberUse);
    Equal(SmileResolvedSymbolKind.EnumMember, member.Kind);
    Equal("Enum member Direction.Left = 3", member.Signature);
    Equal(source.IndexOf("Left = 3", StringComparison.Ordinal), member.DeclarationLocation!.Span.Start);
    var typeUse = source.IndexOf("As Direction", StringComparison.Ordinal) + "As ".Length;
    Equal(SmileResolvedSymbolKind.Enum, ResolveSymbol(analysis, analysis.SyntaxTree, typeUse).Kind);

    const string module = "Module Example.Enums\nPublic Enum Direction\nNone\nUp\nDown\nLeft\nRight\nEnd Enum\nEnd Module\n";
    const string program = "Import Example.Enums As Enums\nDim Value As Enums.Direction\nValue = Enums.Direction.\n";
    var imported = Multi(("Program.smile", true, program), ("Enums.smile", false, module));
    Equal("None|Up|Down|Left|Right", string.Join("|", SmileCompletionService.GetCompletions(imported,
        "Program.smile", program.LastIndexOf("Enums.Direction.", StringComparison.Ordinal) +
        "Enums.Direction.".Length).Select(completion => completion.DisplayText)));
});

Run("Optional parameters bind typed defaults and named calls into source and declaration order", () =>
{
    const string source = "Option Explicit\nEnum Direction\nLeft = 3\nRight = 4\nEnd Enum\nConst DEFAULT_DIRECTION = Direction.Left\nSub Present(\nValue As Number,\nOptional Caption As Text = \"ready\",\nOptional DirectionValue As Direction = DEFAULT_DIRECTION\n)\nPrint Value\nPrint Caption\nEnd Sub\nCall Present(\nDirectionValue:=Direction.Right,\nValue:=1\n)\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var routine = analysis.SemanticModel.Routines["Present"];
    Equal(3, routine.Parameters.Count);
    Equal(false, routine.Parameters[0].IsOptional);
    Equal(true, routine.Parameters[1].IsOptional);
    Equal("ready", routine.Parameters[1].DefaultValue);
    Equal(true, routine.Parameters[2].HasDefaultValue);
    Equal("Left", routine.Parameters[2].DefaultEnumMember!.Name);
    Equal(3L, routine.Parameters[2].DefaultValue);
    Equal(ParameterPassingMode.ByVal, routine.Parameters[2].ParameterMode);

    var syntax = analysis.BoundSyntaxTree.Root.Statements.OfType<CallStatementSyntax>().Single();
    Equal(true, analysis.SemanticModel.TryGetBoundCall(syntax, out var call));
    Equal("DirectionValue|Value", string.Join("|", call.SourceArguments.Select(argument => argument.Parameter.Name)));
    Equal("Value|Caption|DirectionValue", string.Join("|",
        call.ParameterArguments.Select(argument => argument.Parameter.Name)));
    Equal(true, call.ParameterArguments[1].IsDefault);
    Equal("ready", call.ParameterArguments[1].DefaultValue);
});

Run("Optional and named diagnostics are stable and suppress derivative binding cascades", () =>
{
    Equal(true, HasDiagnostic(Analyze(
        "Sub Bad(Optional ByRef Value As Number = 1)\nEnd Sub\n"), "SML3430"));
    Equal(true, HasDiagnostic(Analyze(
        "Sub Bad(Optional First As Number = 1, Second As Number)\nEnd Sub\n"), "SML3430"));
    Equal(true, HasDiagnostic(Analyze(
        "Sub Bad(Optional Value As Boolean = 1)\nEnd Sub\n"), "SML3431"));
    Equal(true, HasDiagnostic(Analyze(
        "Sub Pair(First As Number, Second As Number)\nEnd Sub\nCall Pair(First:=1, 2)\n"), "SML3432"));
    Equal(false, HasDiagnostic(Analyze(
        "Sub Pair(First As Number, Second As Number)\nEnd Sub\nCall Pair(First:=1, 2)\n"), "SML3434"));
    Equal(true, HasDiagnostic(Analyze(
        "Sub Pair(First As Number, Second As Number)\nEnd Sub\nCall Pair(Missing:=1)\n"), "SML3433"));
    Equal(true, HasDiagnostic(Analyze(
        "Sub Pair(First As Number, Optional Second As Number = 2)\nEnd Sub\nCall Pair(1, First:=2)\n"), "SML3434"));
    Equal(true, HasDiagnostic(Analyze(
        "Sub Pair(First As Number, Second As Number)\nEnd Sub\nCall Pair(First:=1)\n"), "SML3435"));
    Equal(true, HasDiagnostic(Analyze("Print Abs(Value:=1)\n"), "SML3433"));
});

Run("Named parameter completion insertion Quick Info and definition use label identity", () =>
{
    const string source = "Option Explicit\nEnum Direction\nLeft\nRight\nEnd Enum\nSub Present(Value As Number, Optional Caption As Text = \"ready\", Optional DirectionValue As Direction = Direction.Left)\nPrint Value\nEnd Sub\nCall Present(Value:=1, Caption:=\"set\")\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var labelStart = source.LastIndexOf("Caption:=", StringComparison.Ordinal);
    var completions = SmileCompletionService.GetCompletions(analysis, labelStart);
    Equal(false, completions.Any(completion => completion.DisplayText == "Value:="));
    var caption = completions.Single(completion => completion.DisplayText == "Caption:=");
    Equal(SmileCompletionKind.Parameter, caption.Kind);
    Equal("Caption:=", caption.InsertionText);
    Equal(true, completions.Any(completion => completion.DisplayText == "DirectionValue:=" &&
        completion.InsertionText == "DirectionValue:="));
    Equal(true, completions.Any(completion => completion.DisplayText == "Abs" &&
        completion.Kind == SmileCompletionKind.BuiltInFunction));

    var resolved = ResolveSymbol(analysis, analysis.SyntaxTree, labelStart + 2);
    Equal(SmileResolvedSymbolKind.NamedArgument, resolved.Kind);
    Equal("Optional Caption As Text = \"ready\"", resolved.Signature);
    Equal(source.IndexOf("Caption As Text", StringComparison.Ordinal), resolved.DeclarationLocation!.Span.Start);
    var signature = SmileSymbolDisplayService.FormatRoutineSignature(analysis.SemanticModel.Routines["Present"]);
    Equal(true, signature.Contains("Optional DirectionValue As Direction = Direction.Left", StringComparison.Ordinal));

    const string nested = "Sub Outer(First As Number, Optional Second As Number = 2)\nEnd Sub\nFunction Inner(Optional Value As Number = 1) As Number\nReturn Value\nEnd Function\nCall Outer(Inner(Value:=1), Second:=2)\n";
    var nestedAnalysis = Analyze(nested);
    var insideIndex = nested.IndexOf("Value:=", StringComparison.Ordinal);
    Equal(false, SmileCompletionService.GetCompletions(nestedAnalysis, insideIndex + "Value:=".Length)
        .Any(completion => completion.Kind == SmileCompletionKind.Parameter));

    const string valueCompletionSource = "Dim Shared As Number\nDim Value As Number\nSub Present(Value As Number, Optional Caption As Text = \"ready\")\nEnd Sub\nCall Present(";
    var valueCompletionAnalysis = Analyze(valueCompletionSource);
    var valueCompletions = SmileCompletionService.GetCompletions(valueCompletionAnalysis,
        valueCompletionSource.Length);
    Equal(true, valueCompletions.Any(completion => completion.DisplayText == "Shared"));
    Equal(true, valueCompletions.Any(completion => completion.DisplayText == "Value:=" &&
        completion.Kind == SmileCompletionKind.Parameter));
    Equal(true, valueCompletions.Any(completion => completion.DisplayText == "Value" &&
        completion.InsertionText == "Value"));
    Equal(true, valueCompletions.Any(completion => completion.DisplayText == "Shared" &&
        completion.InsertionText == "Shared"));
    const string builtInCompletionSource = "Dim Shared As Number\nPrint Abs(";
    var builtInCompletions = SmileCompletionService.GetCompletions(Analyze(builtInCompletionSource),
        builtInCompletionSource.Length);
    Equal(true, builtInCompletions.Any(completion => completion.DisplayText == "Shared"));
});

Run("Bound calls stage ByRef locations and owned ByVal records for both emitters", () =>
{
    const string source = "Option Explicit\nType Payload\nName As Text\nArt As Image\nEnd Type\nDim Shared As Payload\nDim Slot As Number\nDim Values[2] As Number\nShared.Name = \"before\"\nSlot = 0\nCall Observe(Shared, Mutate(Shared))\nCall SetValue(Target:=Values[Slot], Value:=MoveSlot())\nSub Observe(Value As Payload, Ignored As Number)\nPrint Value.Name\nEnd Sub\nFunction Mutate(ByRef Value As Payload) As Number\nValue.Name = \"after\"\nReturn 0\nEnd Function\nFunction MoveSlot() As Number\nSlot = 1\nReturn 9\nEnd Function\nSub SetValue(ByRef Target As Number, Value As Number)\nTarget = Value\nEnd Sub\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var native = new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, false).Emit();
    Equal(true, native.Contains("call_argument_", StringComparison.Ordinal));
    var nativeCall = native.IndexOf("call routine_", StringComparison.Ordinal);
    var captureClear = native.IndexOf("call record_0_payload_clear", nativeCall, StringComparison.Ordinal);
    Equal(true, nativeCall >= 0 && captureClear > nativeCall);
    Equal(true, native.Contains("call smile_image_clear", StringComparison.Ordinal));

    const string byRefRecordSource = "Option Explicit\nType Payload\nName As Text\nEnd Type\nDim Shared As Payload\nShared.Name = \"before\"\nCall Rename(Shared)\nPrint Shared.Name\nSub Rename(ByRef Value As Payload)\nValue.Name = \"after\"\nEnd Sub\n";
    var byRefRecordAnalysis = Analyze(byRefRecordSource);
    Equal(false, byRefRecordAnalysis.HasErrors);
    var byRefRecordNative = new MasmEmitter(byRefRecordAnalysis,
        SmileGraphicsBackend.Auto, true, false).Emit();
    Equal(1, byRefRecordNative.Split("call record_0_payload_clear",
        StringSplitOptions.None).Length - 1);

    var web = new WebEmitter(analysis).Emit();
    Equal(true, web.Contains("transferred = false", StringComparison.Ordinal));
    Equal(true, web.Contains("record_0_payload_clone", StringComparison.Ordinal));
    Equal(true, web.Contains("record_0_payload_clear", StringComparison.Ordinal));
    Equal(true, web.Contains("smile.refArray", StringComparison.Ordinal));

    var unsafeDefault = Analyze(
        "Sub Use(Optional Value As Number = 9007199254740992)\nEnd Sub\nCall Use()\n");
    Equal(false, unsafeDefault.HasErrors);
    try
    {
        _ = new WebEmitter(unsafeDefault).Emit();
        throw new InvalidOperationException("Expected unsafe Optional Number default to fail Web emission.");
    }
    catch (WebTargetException exception)
    {
        Equal("SML5102", exception.Code);
    }
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

Run("With binds explicit nested syntax over writable record locations", () =>
{
    const string source = "Option Explicit\nType Position\nX As Number\nY As Number\nEnd Type\nType Actor\nName As Text\nPosition As Position\nEnd Type\nDim Hero As Actor\nDim Party[2] As Actor\nWith Hero\n.Name = \"A\"\nWith .Position\n.X = .X + 1\nCall SetNumber(.Y, 2)\nEnd With\nEnd With\nWith Party[Pick()]\n.Name = Hero.Name\nEnd With\nFunction Pick() As Number\nReturn 1\nEnd Function\nSub SetNumber(ByRef Value As Number, NewValue As Number)\nValue = NewValue\nEnd Sub\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var outer = analysis.BoundSyntaxTree.Root.Statements.OfType<WithStatementSyntax>().First();
    var nested = outer.Statements.OfType<WithStatementSyntax>().Single();
    var arrayTarget = analysis.BoundSyntaxTree.Root.Statements.OfType<WithStatementSyntax>().Last();
    Equal(true, outer.Target is NameExpressionSyntax);
    Equal(true, nested.Target is LeadingMemberAccessExpressionSyntax);
    Equal(true, arrayTarget.Target is ArrayAccessExpressionSyntax);
    Equal(true, analysis.SemanticModel.TryGetWithTarget(outer, out var outerBinding));
    Equal("Actor", outerBinding.TargetType.Name);
    Equal(0, outerBinding.Depth);
    Equal(true, analysis.SemanticModel.TryGetWithTarget(nested, out var nestedBinding));
    Equal("Position", nestedBinding.TargetType.Name);
    Equal(1, nestedBinding.Depth);
    var leadingAssignment = nested.Statements.OfType<AssignmentStatementSyntax>().First();
    Equal(true, leadingAssignment.Target.Location is LeadingMemberAccessExpressionSyntax);
    Equal(SmileType.Number, analysis.SemanticModel.GetType(leadingAssignment.Target.Location));
});

Run("With diagnostics distinguish scope type location field and unavailable record methods", () =>
{
    Equal(true, HasDiagnostic(Analyze(".Missing = 1\n"), "SML3413"));
    Equal(true, HasDiagnostic(Analyze("Call .Reset()\n"), "SML3413"));
    Equal(true, HasDiagnostic(Analyze("Dim Value As Number\nWith Value\nPrint Value\nEnd With\n"), "SML3415"));
    Equal(true, HasDiagnostic(Analyze("With 1\nPrint 1\nEnd With\n"), "SML3415"));
    Equal(true, HasDiagnostic(Analyze("Type Item\nValue As Number\nEnd Type\nWith Create()\n.Value = 1\nEnd With\nFunction Create() As Item\nDim Result As Item\nReturn Result\nEnd Function\n"), "SML3412"));
    Equal(true, HasDiagnostic(Analyze("Type Item\nValue As Number\nEnd Type\nDim Current As Item\nWith Current\n.Missing = 1\nEnd With\n"), "SML3405"));
    Equal(true, HasDiagnostic(Analyze("Type Item\nValue As Number\nEnd Type\nDim Current As Item\nWith Current\nCall .Reset()\nEnd With\n"), "SML3414"));

    const string shadowSource = "Type Item\nName As Text\nEnd Type\nDim Current As Item\nWith Current\nWith 1\n.Name = \"Wrong\"\nEnd With\nEnd With\n";
    var shadowAnalysis = Analyze(shadowSource);
    Equal(true, HasDiagnostic(shadowAnalysis, "SML3415"));
    var outer = shadowAnalysis.BoundSyntaxTree.Root.Statements.OfType<WithStatementSyntax>().Single();
    var invalidInner = outer.Statements.OfType<WithStatementSyntax>().Single();
    var leading = (LeadingMemberAccessExpressionSyntax)invalidInner.Statements
        .OfType<AssignmentStatementSyntax>().Single().Target.Location;
    Equal(false, shadowAnalysis.SemanticModel.TryGetWithMember(leading, out _));
    Equal(false, shadowAnalysis.SemanticModel.TryGetInnermostWithScope(shadowAnalysis.SyntaxTree.Source,
        leading.Span.Start, out _));

    const string completionSource = "Type Item\nName As Text\nEnd Type\nDim Current As Item\nWith Current\nWith 1\nPrint .\nEnd With\nEnd With\n";
    var completionAnalysis = Analyze(completionSource);
    var completionPosition = completionSource.IndexOf("Print .", StringComparison.Ordinal) + "Print .".Length;
    Equal(0, SmileCompletionService.GetCompletions(completionAnalysis, completionPosition).Count);
});

Run("With context drives legacy return inference completion and field navigation", () =>
{
    const string functionSource = "Type Actor\nName As Text\nEnd Type\nFunction NameOf(Value As Actor)\nWith Value\nReturn .Name\nEnd With\nEnd Function\nPrint NameOf(Make())\nFunction Make() As Actor\nDim Result As Actor\nReturn Result\nEnd Function\n";
    var inferred = Analyze(functionSource);
    Equal(false, inferred.HasErrors);
    Equal(SmileType.Text, inferred.SemanticModel.Routines["NameOf"].ReturnType);

    const string completionSource = "Type Position\nX As Number\nY As Number\nEnd Type\nType Actor\nName As Text\nPosition As Position\nEnd Type\nDim Hero As Actor\nWith Hero\nPrint .Position.\nPrint . Position .\nEnd With\n";
    var completionAnalysis = Analyze(completionSource);
    var completionPosition = completionSource.IndexOf(".Position.", StringComparison.Ordinal) + ".Position.".Length;
    Equal("X|Y", string.Join("|", SmileCompletionService.GetCompletions(completionAnalysis,
        completionPosition).Select(item => item.DisplayText)));
    var spacedCompletionPosition = completionSource.IndexOf(". Position .", StringComparison.Ordinal) +
                                   ". Position .".Length;
    Equal("X|Y", string.Join("|", SmileCompletionService.GetCompletions(completionAnalysis,
        spacedCompletionPosition).Select(item => item.DisplayText)));

    const string contextualCompletionSource = "Type Insets\nLeft As Number\nRight As Number\nEnd Type\nType Style\nWindow As Insets\nEnd Type\nDim Current As Style\nWith Current\nPrint .Window.\nEnd With\n";
    var contextualCompletionAnalysis = Analyze(contextualCompletionSource);
    var contextualCompletionPosition = contextualCompletionSource.IndexOf(".Window.", StringComparison.Ordinal) +
                                       ".Window.".Length;
    Equal("Left|Right", string.Join("|", SmileCompletionService.GetCompletions(contextualCompletionAnalysis,
        contextualCompletionPosition).Select(item => item.DisplayText)));

    const string symbolSource = "Type Actor\nName As Text\nEnd Type\nDim Hero As Actor\nWith Hero\nPrint .Name\nEnd With\n";
    var symbolAnalysis = Analyze(symbolSource);
    var resolved = ResolveSymbol(symbolAnalysis, symbolAnalysis.SyntaxTree,
        symbolSource.IndexOf(".Name", StringComparison.Ordinal) + 3);
    Equal(SmileResolvedSymbolKind.Field, resolved.Kind);
    Equal("Name", resolved.Name);
    Equal("Field Actor.Name As Text", resolved.Signature);
});

Run("With emitters cache native addresses and Web writable root references", () =>
{
    const string source = "Type Actor\nName As Text\nEnd Type\nDim Party[2] As Actor\nDim Calls As Number\nWith Party[Choose()]\nCall Replace(Party[1])\n.Name = ReplaceDuringAssignment(Party[1])\nPrint .Name\nEnd With\nFunction Choose() As Number\nCalls = Calls + 1\nReturn 1\nEnd Function\nSub Replace(ByRef Value As Actor)\nDim NextValue As Actor\nNextValue.Name = \"Replacement\"\nValue = NextValue\nEnd Sub\nFunction ReplaceDuringAssignment(ByRef Value As Actor) As Text\nDim NextValue As Actor\nNextValue.Name = \"RHS Replacement\"\nValue = NextValue\nReturn \"Assigned\"\nEnd Function\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var native = new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, false).Emit();
    Equal(true, native.Contains("with_location_", StringComparison.Ordinal));
    Equal(true, native.Contains("mov QWORD PTR [with_location_", StringComparison.Ordinal));
    var web = new WebEmitter(analysis).Emit();
    Equal(true, web.Contains("const t_", StringComparison.Ordinal));
    Equal(true, web.Contains(" = smile.refArray(", StringComparison.Ordinal));
    Equal(true, web.Contains(".get()[\"__smile_r0_f0\"]", StringComparison.Ordinal));
    var valueDeclaration = web.IndexOf("_value = ", StringComparison.Ordinal);
    var targetDeclaration = web.IndexOf("_target = ", StringComparison.Ordinal);
    Equal(true, valueDeclaration >= 0 && targetDeclaration > valueDeclaration);
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

Run("FormatVersion 6 public API uses logical provider identities deterministically", () =>
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
        Equal(true, api.Contains("\"provider\": \"Example.BaseProvider@1.0.0\"", StringComparison.Ordinal));
        Equal(true, api.Contains("\"provider\": \"Example.ConsumerProvider@2.0.0\"", StringComparison.Ordinal));
        Equal(false, api.Contains(root, StringComparison.OrdinalIgnoreCase));
        SmileLibraryProviderResolver.LoadPackages(new[] { basePackage, consumerPackage },
            Path.Combine(root, "package-cache"));
    }
    finally { Directory.Delete(root, true); }
});

Run("Syntax-aware formatter owns complete multiline routine header boundaries", () =>
{
    const string source = "Option Explicit\n\n" +
        "Function Add(\n" +
        "    LeftValue As Number,\n" +
        "    RightValue As Number\n" +
        ")\n" +
        "    Return LeftValue + RightValue\n" +
        "End Function\n\n" +
        "Sub Present(\n" +
        "    Value As Number\n" +
        ")\n" +
        "    Print Value\n" +
        "End Sub\n";
    var layouts = SmileSourceFormatter.GetRoutineDeclarationLayouts(source, "FormatterTest.smile");
    Equal(2, layouts.Count);
    Equal(3, layouts[0].HeaderStartLine);
    Equal(6, layouts[0].HeaderEndLine);
    Equal(true, layouts[0].IsMultiline);
    Equal(10, layouts[1].HeaderStartLine);
    Equal(12, layouts[1].HeaderEndLine);

    var formatted = FormatSource(source);
    Equal(false, formatted.Contains("Function Add(\n\n", StringComparison.Ordinal));
    Equal(true, formatted.Contains(
        ")\n\n    Dim ReturnValue As Number\n\n    ReturnValue = LeftValue + RightValue",
        StringComparison.Ordinal));
    Equal(formatted, FormatSource(formatted));
    Equal(false, SmileLanguage.Analyze(formatted).HasErrors);
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

Run("Smile.Game 2.0 value Types and Smile.RPG remain independent built-in source packages", () =>
{
    var gameProject = SmileProjectSourceSet.Load("libraries/Smile.Game/Smile.Game.smilelibproj");
    var gameCompilation = SmileProjectCompilation.Load(gameProject.ProjectPath);
    var gameAnalysis = SmileLanguage.Analyze(gameCompilation.Sources, SmileCompilationKind.Library,
        gameCompilation.DependencyContext);
    Equal(false, gameAnalysis.HasErrors);
    Equal("2.0.0", gameProject.Version);
    Equal(5, gameProject.CompilationSources.Count);
    Equal(true, SmileBuiltInLibraryCatalog.IsBuiltIn("Smile.Game"));
    foreach (var module in new[] { "Smile.Game.Core", "Smile.Game.Animation", "Smile.Game.TileMap",
        "Smile.Game.Camera2D", "Smile.Game.Collision2D" })
        Equal(true, gameAnalysis.SemanticModel.Modules.ContainsKey(module));
    Equal(false, gameProject.References.Any());
    Equal(false, gameProject.CompilationSources.Any(source => File.ReadAllText(source.FullPath)
        .Contains("Game Window", StringComparison.OrdinalIgnoreCase)));
    var gameCore = gameAnalysis.SemanticModel.Modules["Smile.Game.Core"];
    var direction = (EnumTypeSymbol)gameCore.Types["CardinalDirection"].Type!;
    Equal("None|Up|Right|Down|Left", string.Join("|", direction.Members.Select(member => member.Name)));
    Equal("0|1|2|3|4", string.Join("|", direction.Members.Select(member => member.Value)));
    Equal(true, gameCompilation.DependencyContext.TryGetProviderDescriptor(direction.ProviderIdentity,
        out var directionProvider));
    Equal("Smile.Game@2.0.0", directionProvider.LogicalIdentity);
    var mover = (RecordTypeSymbol)gameCore.Types["CardinalMover"].Type!;
    Equal("BeginMove|CancelMove|Place|UpdateMove|VisualX|VisualY",
        string.Join("|", mover.Methods.Select(method => method.Name).OrderBy(name => name,
            StringComparer.Ordinal)));
    Equal("Smile.Game.Core::CardinalDirection", mover.Fields.Single(field => field.Name == "Facing")
        .Type.RuntimeIdentity);
    Equal(false, mover.Methods.Any(method => method.Parameters.Any(parameter => parameter.Name == "Me")));
    var cameraModule = gameAnalysis.SemanticModel.Modules["Smile.Game.Camera2D"];
    var camera = (RecordTypeSymbol)cameraModule.Types["CameraState"].Type!;
    Equal("Configure|FirstVisibleCellX|FirstVisibleCellY|Follow|LastVisibleCellX|LastVisibleCellY|SmoothFollow",
        string.Join("|", camera.Methods.Select(method => method.Name).OrderBy(name => name,
            StringComparer.Ordinal)));

    var rpgProject = SmileProjectSourceSet.Load("libraries/Smile.RPG/Smile.RPG.smilelibproj");
    var rpgCompilation = SmileProjectCompilation.Load(rpgProject.ProjectPath);
    var rpgAnalysis = SmileLanguage.Analyze(rpgCompilation.Sources, SmileCompilationKind.Library,
        rpgCompilation.DependencyContext);
    Equal(false, rpgAnalysis.HasErrors);
    Equal("1.2.1", rpgProject.Version);
    Equal(15, rpgProject.CompilationSources.Count);
    Equal(true, SmileBuiltInLibraryCatalog.IsBuiltIn("Smile.RPG"));
    foreach (var module in new[] { "Smile.RPG.Core", "Smile.RPG.Characters", "Smile.RPG.Party",
        "Smile.RPG.Inventory", "Smile.RPG.Equipment", "Smile.RPG.Abilities", "Smile.RPG.BattleEffects",
        "Smile.RPG.BattleCore", "Smile.RPG.BattleStrategy", "Smile.RPG.BattleView", "Smile.RPG.Shops",
        "Smile.RPG.World", "Smile.RPG.Story", "Smile.RPG.Encounters", "Smile.RPG.SaveGames" })
        Equal(true, rpgAnalysis.SemanticModel.Modules.ContainsKey(module));
    foreach (var module in rpgAnalysis.SemanticModel.Modules.Values)
    {
        Equal(true, rpgCompilation.DependencyContext.TryGetProviderDescriptor(module.ProviderIdentity,
            out var rpgProvider));
        Equal("Smile.RPG@1.2.1", rpgProvider.LogicalIdentity);
    }
    Equal(491, rpgAnalysis.SemanticModel.Modules.Values.Sum(module => module.PublicMembers.Count()));
    Equal(false, rpgAnalysis.SemanticModel.NominalTypes.Values.Any(type =>
        type is EnumTypeSymbol or ClassTypeSymbol));
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

Run("Smile.Game 2.0 project and package consumers share Type-member and Enum editor identity", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "smile-game-oop-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var gameProject = SmileProjectSourceSet.Load("libraries/Smile.Game/Smile.Game.smilelibproj");
        var gameCompilation = SmileProjectCompilation.Load(gameProject.ProjectPath);
        var gameAnalysis = SmileLanguage.Analyze(gameCompilation.Sources, SmileCompilationKind.Library,
            gameCompilation.DependencyContext);
        Equal(false, gameAnalysis.HasErrors);
        var packagePath = Path.Combine(directory, "Smile.Game.smilelib");
        SmileLibraryPackage.Write(packagePath, gameCompilation.Graph.Root, gameAnalysis);

        const string source = "Option Explicit\n" +
            "Import Smile.Game.Core As GameCore\n" +
            "Dim Mover As GameCore.CardinalMover\n" +
            "Dim Started As Boolean\n" +
            "Call Mover.Place(1, 2, GameCore.CardinalDirection.Right)\n" +
            "Started = Mover.BeginMove(Duration:=4, Direction:=GameCore.CardinalDirection.Down)\n" +
            "Print Started\n";

        void AssertConsumer(string name, string referencePath, bool packageReference)
        {
            var consumerDirectory = Path.Combine(directory, name);
            Directory.CreateDirectory(consumerDirectory);
            var programPath = Path.Combine(consumerDirectory, "Program.smile");
            File.WriteAllText(programPath, source);
            var projectPath = Path.Combine(consumerDirectory, "Consumer.smileproj");
            var relativeReference = Path.GetRelativePath(consumerDirectory, referencePath);
            var referenceElement = packageReference
                ? $"<SmileLibraryReference Include=\"{relativeReference}\" />"
                : $"<SmileProjectReference Include=\"{relativeReference}\" />";
            File.WriteAllText(projectPath,
                "<SmileProject><PropertyGroup><StartupFile>Program.smile</StartupFile></PropertyGroup>" +
                "<ItemGroup><SmileSource Include=\"Program.smile\" />" + referenceElement +
                "</ItemGroup></SmileProject>");
            var compilation = SmileProjectCompilation.Load(projectPath,
                Path.Combine(consumerDirectory, "cache"));
            var analysis = SmileLanguage.Analyze(compilation.Sources, SmileCompilationKind.Program,
                compilation.DependencyContext);
            Equal(false, analysis.HasErrors);
            var core = analysis.SemanticModel.Modules["Smile.Game.Core"];
            var mover = (RecordTypeSymbol)core.Types["CardinalMover"].Type!;
            Equal(true, compilation.DependencyContext.TryGetProviderDescriptor(mover.ProviderIdentity,
                out var moverProvider));
            Equal("Smile.Game@2.0.0", moverProvider.LogicalIdentity);
            Equal("Smile.Game.Core::CardinalMover::member::BeginMove",
                mover.Methods.Single(method => method.Name == "BeginMove").RuntimeIdentity);

            var tree = analysis.GetSyntaxTree(programPath);
            var memberPosition = source.IndexOf("Mover.BeginMove", StringComparison.Ordinal) +
                                 "Mover.".Length;
            var completions = SmileCompletionService.GetCompletions(analysis, tree, memberPosition);
            foreach (var expected in new[] { "BeginMove", "CancelMove", "Place", "UpdateMove", "VisualX", "VisualY" })
                Equal(true, completions.Any(completion => completion.DisplayText == expected));
            Equal(true, SmileSymbolService.TryResolve(analysis, tree, memberPosition + 2,
                out var resolvedMethod));
            Equal(SmileResolvedSymbolKind.Function, resolvedMethod.Kind);
            var methodPresentation = SmileSymbolDisplayService.Present(resolvedMethod,
                compilation.DependencyContext);
            Equal(true, methodPresentation.Provider.EndsWith("Smile.Game@2.0.0",
                StringComparison.Ordinal));
            Equal(mover.Methods.Single(method => method.Name == "BeginMove").DeclarationLocation.Line,
                resolvedMethod.DeclarationLocation!.Line);

            var labelPosition = source.IndexOf("Duration:=", StringComparison.Ordinal);
            Equal(true, SmileSymbolService.TryResolve(analysis, tree, labelPosition + 2,
                out var resolvedLabel));
            Equal(SmileResolvedSymbolKind.NamedArgument, resolvedLabel.Kind);
            Equal(true, SmileSymbolDisplayService.Present(resolvedLabel,
                compilation.DependencyContext).Provider.EndsWith("Smile.Game@2.0.0",
                StringComparison.Ordinal));

            var enumPosition = source.IndexOf("CardinalDirection.Right", StringComparison.Ordinal) +
                               "CardinalDirection.".Length;
            Equal(true, SmileSymbolService.TryResolve(analysis, tree, enumPosition + 2,
                out var resolvedEnumMember));
            Equal(SmileResolvedSymbolKind.EnumMember, resolvedEnumMember.Kind);
            Equal(true, SmileSymbolDisplayService.Present(resolvedEnumMember,
                compilation.DependencyContext).Provider.EndsWith("Smile.Game@2.0.0",
                StringComparison.Ordinal));

            var resolvedPath = Path.GetFullPath(resolvedMethod.DeclarationLocation.FilePath);
            if (packageReference)
            {
                var cachePrefix = Path.GetFullPath(Path.Combine(consumerDirectory, "cache"))
                    .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                Equal(true, resolvedPath.StartsWith(cachePrefix, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                Equal(Path.GetFullPath("libraries/Smile.Game/Core.smile"), resolvedPath);
            }
        }

        AssertConsumer("Project", gameProject.ProjectPath, false);
        AssertConsumer("Package", packagePath, true);
    }
    finally
    {
        Directory.Delete(directory, true);
    }
});

Run("Snake shares one private Class model across both startup stories and editor services", () =>
{
    var project = SmileProjectSourceSet.Load("games/Snake/Snake.smileproj");
    Equal("Program.smile", project.StartupFile);
    Equal("Program.smile|Program-NoDemo.smile", string.Join("|", project.Items
        .Where(source => source.StartupOnly).Select(source => source.Include)));
    Equal("SnakeModel.smile", project.Items.Single(source => !source.StartupOnly).Include);

    var compilation = SmileProjectCompilation.Load(project.ProjectPath);
    var analysis = SmileLanguage.Analyze(compilation.Sources, SmileCompilationKind.Program,
        compilation.DependencyContext);
    Equal(false, analysis.HasErrors);
    var snake = analysis.SemanticModel.NominalTypes.Values.OfType<ClassTypeSymbol>()
        .Single(type => type.Name == "Snake");
    Equal(true, snake.Fields.All(field => field.Visibility == ModuleVisibility.Private));
    Equal("CanTurn|CellBlocked|Contains|Direction|Grow|HeadX|HeadY|HitSelf|HitWall|Length|Move|Reset|SegmentX|SegmentY|TryTurn",
        string.Join("|", snake.Members.Where(member => member.Visibility == ModuleVisibility.Public)
            .Select(member => member.Name).OrderBy(name => name, StringComparer.Ordinal)));
    Equal(false, snake.Members.Where(member => member.Visibility == ModuleVisibility.Public)
        .Any(member => member.Name is "SegmentXs" or "SegmentYs" or "CurrentLength" or
            "CurrentDirection" or "RequestedDirection" or "TailX" or "TailY"));
    Equal(true, snake.Constructor.IsDeclared);
    Equal(0, snake.Constructor.Parameters.Count);
    var modelPath = Path.GetFullPath("games/Snake/SnakeModel.smile");
    Equal(modelPath, Path.GetFullPath(snake.DeclarationLocation!.FilePath));

    var gameState = analysis.SemanticModel.NominalTypes.Values.OfType<EnumTypeSymbol>()
        .Single(type => type.Name == "GameState");
    Equal("Title|Playing|GameOver", string.Join("|", gameState.Members.Select(member => member.Name)));
    var moveDirection = analysis.SemanticModel.NominalTypes.Values.OfType<EnumTypeSymbol>()
        .Single(type => type.Name == "MoveDirection");
    Equal("Up|Down|Left|Right", string.Join("|", moveDirection.Members.Select(member => member.Name)));
    Equal(true, analysis.SemanticModel.NominalTypes.Values.OfType<RecordTypeSymbol>()
        .Any(type => type.Name == "GridPoint"));

    var programPath = Path.GetFullPath("games/Snake/Program.smile");
    var programText = File.ReadAllText(programPath);
    var tree = analysis.GetSyntaxTree(programPath);
    var memberPosition = programText.IndexOf("Player.TryTurn", StringComparison.Ordinal) +
                         "Player.".Length;
    var completions = SmileCompletionService.GetCompletions(analysis, tree, memberPosition);
    foreach (var expected in new[] { "TryTurn", "Move", "Grow", "Contains", "HitSelf", "HitWall",
        "SegmentX", "SegmentY", "Length", "HeadX", "HeadY", "Direction" })
        Equal(true, completions.Any(completion => completion.DisplayText == expected));
    foreach (var hidden in new[] { "SegmentXs", "SegmentYs", "CurrentLength", "CurrentDirection",
        "RequestedDirection", "TailX", "TailY", "IsReverse" })
        Equal(false, completions.Any(completion => completion.DisplayText == hidden));
    Equal(true, SmileSymbolService.TryResolve(analysis, tree, memberPosition + 2,
        out var resolvedTryTurn));
    Equal(SmileResolvedSymbolKind.Subroutine, resolvedTryTurn.Kind);
    Equal(modelPath, Path.GetFullPath(resolvedTryTurn.DeclarationLocation!.FilePath));
    Equal(snake.Methods.Single(method => method.Name == "TryTurn").DeclarationLocation.Line,
        resolvedTryTurn.DeclarationLocation.Line);

    var enumPosition = programText.IndexOf("MoveDirection.Up", StringComparison.Ordinal) +
                       "MoveDirection.".Length;
    Equal(true, SmileSymbolService.TryResolve(analysis, tree, enumPosition + 1,
        out var resolvedDirection));
    Equal(SmileResolvedSymbolKind.EnumMember, resolvedDirection.Kind);
    Equal(modelPath, Path.GetFullPath(resolvedDirection.DeclarationLocation!.FilePath));

    var playerPosition = programText.IndexOf("Player.TryTurn", StringComparison.Ordinal);
    Equal(true, SmileSymbolService.TryResolve(analysis, tree, playerPosition + 1,
        out var resolvedPlayer));
    Equal(SmileResolvedSymbolKind.Variable, resolvedPlayer.Kind);
    Equal(true, SmileSymbolDisplayService.Present(resolvedPlayer, compilation.DependencyContext)
        .Signature.Contains("Player As Snake", StringComparison.Ordinal));

    var modelText = File.ReadAllText(modelPath);
    Equal(false, modelText.Contains("KEY_UP", StringComparison.Ordinal));
    Equal(false, programText.Contains("SnakeX[", StringComparison.Ordinal));
    Equal(false, File.ReadAllText("games/Snake/Program-NoDemo.smile")
        .Contains("SnakeX[", StringComparison.Ordinal));
});

Run("Sin Star I keeps its TitleScreen Module and exposes one typed action enum", () =>
{
    var project = SmileProjectSourceSet.Load("games/SinStarI/SinStarI.smileproj");
    Equal(SmileProjectKind.Game, project.ProjectKind);
    Equal("smile.game.sin-star-i", project.ApplicationId);
    Equal("Program.smile", project.StartupFile);
    Equal(true, project.AssetPaths.Contains("Assets/Sin Star - Title Screen - Background.png"));
    Equal(true, project.AssetPaths.Contains("Assets/TitleMusic.mp3"));
    Equal(true, project.AssetPaths.Contains("Maps/Towns/Town2_NE.smilemap"));

    var compilation = SmileProjectCompilation.Load(project.ProjectPath);
    var analysis = SmileLanguage.Analyze(compilation.Sources, SmileCompilationKind.Program,
        compilation.DependencyContext);
    Equal(false, analysis.HasErrors);
    var titleModule = analysis.SemanticModel.Modules["SinStarI.TitleScreen"];
    var titleAction = (EnumTypeSymbol)titleModule.Types["TitleAction"].Type!;
    Equal(ModuleVisibility.Public, titleModule.Types["TitleAction"].Visibility);
    Equal("None|Character|Town|Town2|Shop|Dungeon|Battle",
        string.Join("|", titleAction.Members.Select(member => member.Name)));
    Equal("0|1|2|3|4|5|6",
        string.Join("|", titleAction.Members.Select(member => member.Value)));
    Equal(false, titleModule.Members.Keys.Any(name =>
        name.StartsWith("TITLE_ACTION_", StringComparison.OrdinalIgnoreCase)));
    Equal(true, titleModule.Members["HandleInput"].Routine!.ReturnType.Equals(titleAction));

    var titlePath = Path.GetFullPath("games/SinStarI/TitleScreen.smile");
    var programPath = Path.GetFullPath("games/SinStarI/Program.smile");
    var programText = File.ReadAllText(programPath);
    var tree = analysis.GetSyntaxTree(programPath);

    var typeCompletionPosition = programText.IndexOf("TitleScreen.TitleAction",
        StringComparison.Ordinal) + "TitleScreen.".Length;
    var typeCompletions = SmileCompletionService.GetCompletions(analysis, tree,
        typeCompletionPosition);
    Equal(true, typeCompletions.Any(completion => completion.DisplayText == "TitleAction"));

    var routineCompletionPosition = programText.IndexOf("TitleScreen.Initialize",
        StringComparison.Ordinal) + "TitleScreen.".Length;
    var routineCompletions = SmileCompletionService.GetCompletions(analysis, tree,
        routineCompletionPosition);
    foreach (var expected in new[] { "Initialize", "Enter", "Leave", "HandleInput", "Draw", "Shutdown" })
        Equal(true, routineCompletions.Any(completion => completion.DisplayText == expected));
    foreach (var hidden in new[] { "TitleBackground", "TitleLogo", "MenuBackground", "TitleSelection",
        "DrawChoice" })
        Equal(false, routineCompletions.Any(completion => completion.DisplayText == hidden));

    var enumCompletionPosition = programText.IndexOf("TitleScreen.TitleAction.None",
        StringComparison.Ordinal) + "TitleScreen.TitleAction.".Length;
    Equal("None|Character|Town|Town2|Shop|Dungeon|Battle", string.Join("|",
        SmileCompletionService.GetCompletions(analysis, tree, enumCompletionPosition)
            .Select(completion => completion.DisplayText)));

    var typePosition = programText.IndexOf("TitleScreen.TitleAction", StringComparison.Ordinal) +
                       "TitleScreen.".Length;
    Equal(true, SmileSymbolService.TryResolve(analysis, tree, typePosition + 2,
        out var resolvedType));
    Equal(SmileResolvedSymbolKind.Enum, resolvedType.Kind);
    Equal(titlePath, Path.GetFullPath(resolvedType.DeclarationLocation!.FilePath));

    var memberPosition = programText.IndexOf("TitleScreen.TitleAction.None", StringComparison.Ordinal) +
                         "TitleScreen.TitleAction.".Length;
    Equal(true, SmileSymbolService.TryResolve(analysis, tree, memberPosition + 1,
        out var resolvedMember));
    Equal(SmileResolvedSymbolKind.EnumMember, resolvedMember.Kind);
    Equal("Enum member SinStarI.TitleScreen.TitleAction.None = 0", resolvedMember.Signature);
    Equal(titlePath, Path.GetFullPath(resolvedMember.DeclarationLocation!.FilePath));
    Equal(true, SmileSymbolDisplayService.Present(resolvedMember, compilation.DependencyContext)
        .SourcePath.EndsWith("TitleScreen.smile", StringComparison.OrdinalIgnoreCase));
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
        .Replace("$smileversion$", "2.0.48", StringComparison.Ordinal);
    var header = rendered.Split('\n').Take(9).Select(line => line.TrimEnd('\r')).ToArray();
    Equal("' Programmed By: " + "Sin".PadRight(69) + "Version: 0.0.1", header[3]);
    Equal("' Programmed Date: " + "August 15, 2026".PadRight(69) + "SMILE: 2.0.48", header[4]);
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
        Equal(true, manifest.Contains("Version=2.0.48.0", StringComparison.Ordinal));
    }
    foreach (var applicationProject in new[] { gameProject, consoleProject })
        Equal(true, applicationProject.Contains("<ApplicationId>$smileapplicationid$</ApplicationId>", StringComparison.Ordinal));
    Equal(false, libraryProject.Contains("ApplicationId", StringComparison.Ordinal));
    Equal(true, wizard.Contains("\"smile.app.a\" + Guid.NewGuid().ToString(\"N\")", StringComparison.Ordinal));
    Equal(true, wizard.Contains("ToString(\"D\", CultureInfo.CurrentCulture)", StringComparison.Ordinal));
    Equal(true, project.Contains("<Version>2.0.48</Version>", StringComparison.Ordinal));
    Equal(true, vsixManifest.Contains("Type=\"Microsoft.VisualStudio.Assembly\"", StringComparison.Ordinal));
});

Run("VSIX registers SMILE line comments with the Visual Studio editor", () =>
{
    var configuration = File.ReadAllText("src/Smile.VisualStudio/smile-language-configuration.json");
    var registration = File.ReadAllText("src/Smile.VisualStudio/Smile.LanguageConfiguration.pkgdef");
    var project = File.ReadAllText("src/Smile.VisualStudio/Smile.VisualStudio.csproj");
    var manifest = File.ReadAllText("src/Smile.VisualStudio/source.extension.vsixmanifest");
    var handler = File.ReadAllText("src/Smile.VisualStudio/SmileCommentCommandHandler.cs");
    Equal(true, configuration.Contains("\"lineComment\": \"'\"", StringComparison.Ordinal));
    Equal(true, registration.Contains("TextMate\\LanguageConfiguration\\ContentTypeMapping",
        StringComparison.Ordinal));
    Equal(true, registration.Contains("\"SMILE 2.0\"=\"$PackageFolder$\\smile-language-configuration.json\"",
        StringComparison.Ordinal));
    foreach (var payload in new[] { "smile-language-configuration.json", "Smile.LanguageConfiguration.pkgdef" })
    {
        Equal(true, project.Contains($"<Content Include=\"{payload}\">", StringComparison.Ordinal));
        Equal(true, project.Contains("<IncludeInVSIX>true</IncludeInVSIX>", StringComparison.Ordinal));
    }
    Equal(true, manifest.Contains("Type=\"Microsoft.VisualStudio.VsPackage\" Path=\"Smile.LanguageConfiguration.pkgdef\"",
        StringComparison.Ordinal));
    foreach (var command in new[] { "CommentSelectionCommandArgs", "UncommentSelectionCommandArgs",
        "ToggleLineCommentCommandArgs" })
        Equal(true, handler.Contains($"IChainedCommandHandler<{command}>", StringComparison.Ordinal));
    Equal(true, handler.Contains("CommandState.Available", StringComparison.Ordinal));
});

Run("VSIX completion Quick Info and definition share one unchanged-snapshot analysis cache", () =>
{
    foreach (var file in new[]
             {
                 "src/Smile.VisualStudio/SmileCompletionSource.cs",
                 "src/Smile.VisualStudio/SmileQuickInfoSource.cs",
                 "src/Smile.VisualStudio/SmileGoToDefinitionCommandHandler.cs"
             })
    {
        var source = File.ReadAllText(file);
        Equal(true, source.Contains("GetOrCreateSingletonProperty", StringComparison.Ordinal));
        Equal(true, source.Contains("TryGet(", StringComparison.Ordinal));
        Equal(true, source.IndexOf("TryGet(", StringComparison.Ordinal) <
                    source.IndexOf("SmileProjectWorkspace.Analyze", StringComparison.Ordinal));
    }
    var cache = File.ReadAllText("src/Smile.VisualStudio/SmileAnalysisCache.cs");
    Equal(true, cache.Contains("ReferenceEquals(snapshot, _snapshot)", StringComparison.Ordinal));
    Equal(true, cache.Contains("ReferenceEquals(snapshot, _buffer.CurrentSnapshot)", StringComparison.Ordinal));
});

Run("Property Quick Info is static and never executes the getter", () =>
{
    const string source = "Type Meter\nStored As Number\nProperty Reading As Number\nGet\nPrint \"GETTER EXECUTED\"\nReturn Me.Stored\nEnd Get\nEnd Property\nEnd Type\nDim Current As Meter\nPrint Current.Reading\n";
    var output = new StringWriter();
    var original = Console.Out;
    SmileResolvedSymbol resolved;
    SmileSymbolPresentation presentation;
    try
    {
        Console.SetOut(output);
        var analysis = Analyze(source);
        var position = source.LastIndexOf("Reading", StringComparison.Ordinal);
        Equal(true, SmileSymbolService.TryResolve(analysis, analysis.SyntaxTree, position + 1, out resolved));
        presentation = SmileSymbolDisplayService.Present(resolved,
            SmileCompilationDependencyContext.Create());
    }
    finally
    {
        Console.SetOut(original);
    }
    Equal(string.Empty, output.ToString());
    Equal("Property Meter.Reading As Number { Get }", presentation.Signature);
});

Run("VSIX project builds release the Visual Studio UI thread while the compiler runs", () =>
{
    var projectSystem = File.ReadAllText("src/Smile.VisualStudio/SmileProjectSystem.cs");
    Equal(true, projectSystem.Contains("public async Task<bool> BuildAsync", StringComparison.Ordinal));
    Equal(false, projectSystem.Contains(
        "ThreadHelper.JoinableTaskFactory.Run(() => SmileBuildService.RunProjectAsync",
        StringComparison.Ordinal));
    Equal(true, projectSystem.Contains(
        "_ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>", StringComparison.Ordinal));
    Equal(true, projectSystem.Contains("callback.BuildBegin(ref continueBuild)", StringComparison.Ordinal));
    Equal(true, projectSystem.Contains("callback.BuildEnd(success ? 1 : 0)", StringComparison.Ordinal));
});

Run("VSIX compiler execution has bounded timeout and real Stop cancellation", () =>
{
    var buildService = File.ReadAllText("src/Smile.VisualStudio/SmileBuildService.cs");
    var projectSystem = File.ReadAllText("src/Smile.VisualStudio/SmileProjectSystem.cs");
    Equal(true, buildService.Contains("CompilerTimeout = TimeSpan.FromMinutes(10)", StringComparison.Ordinal));
    Equal(true, buildService.Contains("CancellationToken cancellationToken", StringComparison.Ordinal));
    Equal(true, buildService.Contains("/T /F", StringComparison.Ordinal));
    Equal(true, buildService.Contains("error {code}", StringComparison.Ordinal));
    Equal(true, buildService.Contains("\"SML5006\" : \"SML5005\"", StringComparison.Ordinal));
    Equal(true, projectSystem.Contains("_buildCancellation?.Cancel()", StringComparison.Ordinal));
    Equal(true, projectSystem.Contains("buildCancellation.Token", StringComparison.Ordinal));
});

Run("VSIX launch performs one shell-coordinated build before starting the program", () =>
{
    var projectSystem = File.ReadAllText("src/Smile.VisualStudio/SmileProjectSystem.cs");
    var launchStart = projectSystem.IndexOf("public bool Launch(", StringComparison.Ordinal);
    var launchEnd = projectSystem.IndexOf("private void ReadProject()", launchStart, StringComparison.Ordinal);
    var launch = projectSystem.Substring(launchStart, launchEnd - launchStart);
    Equal(false, launch.Contains("BuildAsync(", StringComparison.Ordinal));
    Equal(true, launch.Contains("Visual Studio completes the configured build before calling DebugLaunch",
        StringComparison.Ordinal));
    var upToDateStart = projectSystem.IndexOf("public int StartUpToDateCheck(", StringComparison.Ordinal);
    var statusStart = projectSystem.IndexOf("public int QueryStatus(", upToDateStart, StringComparison.Ordinal);
    var upToDateCheck = projectSystem.Substring(upToDateStart, statusStart - upToDateStart);
    Equal(false, upToDateCheck.Contains("StartOperation", StringComparison.Ordinal));
    Equal(true, upToDateCheck.Contains("VSConstants.E_NOTIMPL", StringComparison.Ordinal));
});

Run("Bounded process execution captures output, exit status, timeout, cancellation, and start failure", () =>
{
    var completed = BoundedProcessRunner.Run(new ProcessStartInfo("cmd.exe")
    {
        Arguments = "/d /s /c \"echo standard-output & echo standard-error 1>&2 & exit /b 7\"",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    }, TimeSpan.FromSeconds(5));
    Equal(ProcessExecutionStatus.Completed, completed.Status);
    Equal(7, completed.ExitCode!.Value);
    Equal(true, completed.StandardOutput.Contains("standard-output", StringComparison.Ordinal));
    Equal(true, completed.StandardError.Contains("standard-error", StringComparison.Ordinal));

    var timedOut = BoundedProcessRunner.Run(new ProcessStartInfo("powershell.exe")
    {
        Arguments = "-NoProfile -Command Start-Sleep -Seconds 30",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    }, TimeSpan.FromMilliseconds(150));
    Equal(ProcessExecutionStatus.TimedOut, timedOut.Status);

    var childPidPath = Path.Combine(Path.GetTempPath(), "smile-child-pid-" + Guid.NewGuid().ToString("N") + ".txt");
    try
    {
        var treeStart = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        treeStart.ArgumentList.Add("-NoProfile");
        treeStart.ArgumentList.Add("-Command");
        treeStart.ArgumentList.Add("$child = Start-Process powershell.exe -ArgumentList '-NoProfile'," +
                                   "'-Command','Start-Sleep -Seconds 30' -PassThru; " +
                                   "$child.Id | Set-Content -LiteralPath '" +
                                   childPidPath.Replace("'", "''") + "'; Wait-Process -Id $child.Id");
        var treeTimeout = BoundedProcessRunner.Run(treeStart, TimeSpan.FromSeconds(2));
        Equal(ProcessExecutionStatus.TimedOut, treeTimeout.Status);
        Equal(true, File.Exists(childPidPath));
        var childPid = int.Parse(File.ReadAllText(childPidPath).Trim(), System.Globalization.CultureInfo.InvariantCulture);
        var childExited = false;
        try
        {
            using var child = Process.GetProcessById(childPid);
            childExited = child.WaitForExit(2000);
        }
        catch (ArgumentException)
        {
            childExited = true;
        }
        Equal(true, childExited);
    }
    finally
    {
        if (File.Exists(childPidPath))
            File.Delete(childPidPath);
    }

    using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
    var cancelled = BoundedProcessRunner.Run(new ProcessStartInfo("powershell.exe")
    {
        Arguments = "-NoProfile -Command Start-Sleep -Seconds 30",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    }, TimeSpan.FromSeconds(5), cancellation.Token);
    Equal(ProcessExecutionStatus.Cancelled, cancelled.Status);

    var startFailed = BoundedProcessRunner.Run(new ProcessStartInfo(
        "smile-process-that-does-not-exist-" + Guid.NewGuid().ToString("N") + ".exe")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    }, TimeSpan.FromSeconds(1));
    Equal(ProcessExecutionStatus.StartFailed, startFailed.Status);
});

Run("Output locks are bounded, case-insensitive, recover abandoned ownership, and keep targets independent", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileOutputLockTests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var firstPath = Path.Combine(directory, "Game.exe");
        Equal(OutputPublicationLock.CreateMutexName(firstPath),
            OutputPublicationLock.CreateMutexName(firstPath.ToUpperInvariant()));
        using var acquired = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        var holder = Task.Run(() =>
        {
            using var outputLock = OutputPublicationLock.Acquire(firstPath, TimeSpan.FromSeconds(2));
            acquired.Set();
            release.Wait(TimeSpan.FromSeconds(5));
        });
        Equal(true, acquired.Wait(TimeSpan.FromSeconds(2)));
        try
        {
            ThrowsContains(() => OutputPublicationLock.Acquire(firstPath, TimeSpan.FromMilliseconds(100)),
                "Another build still owns output");
            using var independent = OutputPublicationLock.Acquire(Path.Combine(directory, "Other.exe"),
                TimeSpan.FromMilliseconds(100));
        }
        finally
        {
            release.Set();
            holder.GetAwaiter().GetResult();
        }

        var abandonedPath = Path.Combine(directory, "Abandoned.exe");
        var abandonedName = OutputPublicationLock.CreateMutexName(abandonedPath);
        var thread = new Thread(() =>
        {
            var mutex = new Mutex(false, abandonedName);
            mutex.WaitOne();
        });
        thread.Start();
        thread.Join();
        using var recovered = OutputPublicationLock.Acquire(abandonedPath, TimeSpan.FromSeconds(1));
    }
    finally { Directory.Delete(directory, true); }
});

Run("Native intermediates are project-owned, Unicode-safe, and cleaned on every failure path", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "Smile Native 測試 " + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var sourcePath = Path.Combine(directory, "來源 Program.smile");
        var outputPath = Path.Combine(directory, "bin", "Program.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(sourcePath, "Print \"Hello\"\n");
        File.WriteAllText(outputPath, "last-known-good");

        foreach (var scenario in new[] { "assembler", "linker", "debug-helper" })
        {
            CompilerIntermediateDirectory? captured = null;
            var hooks = new CompilerDriverTestHooks
            {
                AfterAssemblyEmission = owner => captured = owner,
                RunNativeToolchain = invocation =>
                {
                    File.WriteAllText(invocation.ObjectPath, scenario + " object");
                    if (invocation.DebugObjectPath != null)
                        File.WriteAllText(invocation.DebugObjectPath, scenario + " debug object");
                    return new ToolchainResult(false, "Synthetic " + scenario + " failure.");
                }
            };
            var arguments = new List<string> { sourcePath, "-o", outputPath };
            if (scenario == "debug-helper")
                arguments.Add("--debug");
            Equal(2, new CompilerDriver(hooks).Run(arguments.ToArray()));
            Equal("last-known-good", File.ReadAllText(outputPath));
            Equal(true, captured != null);
            Equal(false, Directory.Exists(captured!.DirectoryPath));
            Equal(true, captured.DirectoryPath.StartsWith(
                Path.Combine(directory, "obj", "Smile", "Compiler") + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase));
        }

        CompilerIntermediateDirectory? emissionOwner = null;
        var emissionFailure = new CompilerDriverTestHooks
        {
            AfterAssemblyEmission = owner =>
            {
                emissionOwner = owner;
                throw new IOException("Synthetic emission failure.");
            }
        };
        Equal(2, new CompilerDriver(emissionFailure).Run(new[] { sourcePath, "-o", outputPath }));
        Equal(false, Directory.Exists(emissionOwner!.DirectoryPath));

        CompilerIntermediateDirectory? kept = null;
        var keepFailure = new CompilerDriverTestHooks
        {
            AfterAssemblyEmission = owner => kept = owner,
            RunNativeToolchain = invocation =>
            {
                File.WriteAllText(invocation.ObjectPath, "kept object");
                File.WriteAllText(invocation.DebugObjectPath!, "kept debug object");
                return new ToolchainResult(false, "Synthetic kept failure.");
            }
        };
        Equal(2, new CompilerDriver(keepFailure).Run(new[]
            { sourcePath, "-o", outputPath, "--debug", "--keep-temp" }));
        Equal(true, File.Exists(kept!.AssemblyPath));
        Equal(true, File.Exists(kept.ObjectPath));
        Equal(true, File.Exists(kept.DebugSourcePath));
        Equal(true, File.Exists(kept.DebugObjectPath));
        Directory.Delete(kept.DirectoryPath, true);
    }
    finally { Directory.Delete(directory, true); }
});

Run("Transactional managed publication rolls back failures and removes stale owned files only after success", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileOutputTransactionTests-" + Guid.NewGuid().ToString("N"));
    var staging = Path.Combine(directory, "staging");
    var output = Path.Combine(directory, "output");
    Directory.CreateDirectory(staging);
    Directory.CreateDirectory(output);
    try
    {
        foreach (var relative in new[] { "Program.exe", "Program.pdb", "Assets/New.txt", "Program.smile-assets.json" })
        {
            var staged = Path.Combine(staging, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(staged)!);
            File.WriteAllText(staged, "new-" + relative);
            var prior = Path.Combine(output, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(prior)!);
            File.WriteAllText(prior, "old-" + relative);
        }
        File.WriteAllText(Path.Combine(output, "Assets", "Stale.txt"), "stale-owned");
        File.WriteAllText(Path.Combine(output, "sentinel.txt"), "unrelated");
        var current = new[] { "Program.exe", "Program.pdb", "Assets/New.txt", "Program.smile-assets.json" };
        var previous = current.Concat(new[] { "Assets/Stale.txt" }).ToArray();
        ThrowsContains(() => TransactionalOutputPublisher.PublishDirectory(staging, output, current, previous,
            (stage, relative) =>
            {
                if (stage == TransactionalPublicationStage.AfterFileCommit && relative == "Program.pdb")
                    throw new IOException("Synthetic transactional commit failure.");
            }), "Synthetic transactional");
        foreach (var relative in current)
            Equal("old-" + relative, File.ReadAllText(Path.Combine(output, relative)));
        Equal("stale-owned", File.ReadAllText(Path.Combine(output, "Assets", "Stale.txt")));
        Equal("unrelated", File.ReadAllText(Path.Combine(output, "sentinel.txt")));

        TransactionalOutputPublisher.PublishDirectory(staging, output, current, previous);
        foreach (var relative in current)
            Equal("new-" + relative, File.ReadAllText(Path.Combine(output, relative)));
        Equal(false, File.Exists(Path.Combine(output, "Assets", "Stale.txt")));
        Equal("unrelated", File.ReadAllText(Path.Combine(output, "sentinel.txt")));
    }
    finally { Directory.Delete(directory, true); }
});

Run("Failed staged Web generation preserves the complete prior publication", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileWebTransactionTests-" + Guid.NewGuid().ToString("N"));
    var sourcePath = Path.Combine(directory, "Program.smile");
    var output = Path.Combine(directory, "Web");
    Directory.CreateDirectory(output);
    try
    {
        File.WriteAllText(sourcePath, "Print \"replacement\"\n");
        foreach (var file in WebOutputWriter.ManagedFileNames)
            File.WriteAllText(Path.Combine(output, file), "last-known-good-" + file);
        File.WriteAllText(Path.Combine(output, "sentinel.txt"), "unrelated");
        var hooks = new CompilerDriverTestHooks
        {
            AfterWebStagedFile = file =>
            {
                if (file == "smile-runtime.js")
                    throw new IOException("Synthetic Web generation failure.");
            }
        };
        Equal(2, new CompilerDriver(hooks).Run(new[]
            { sourcePath, "--target", "web", "--output-dir", output }));
        foreach (var file in WebOutputWriter.ManagedFileNames)
            Equal("last-known-good-" + file, File.ReadAllText(Path.Combine(output, file)));
        Equal("unrelated", File.ReadAllText(Path.Combine(output, "sentinel.txt")));
        Equal(0, Directory.EnumerateDirectories(directory, ".Web.smile-staging-*").Count());
    }
    finally { Directory.Delete(directory, true); }
});

Run("Failed project asset staging preserves prior native and Web publications", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "SmileProjectPublishRollbackTests-" +
                                                  Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(Path.Combine(directory, "Assets"));
    try
    {
        var sourcePath = Path.Combine(directory, "Program.smile");
        var assetPath = Path.Combine(directory, "Assets", "Data.txt");
        var projectPath = Path.Combine(directory, "Rollback.smileproj");
        File.WriteAllText(sourcePath, "Print \"first\"\n");
        File.WriteAllText(assetPath, "first-asset");
        File.WriteAllText(projectPath,
            "<SmileProject><PropertyGroup><ProjectKind>Console</ProjectKind><StartupFile>Program.smile</StartupFile><OutputName>Rollback</OutputName></PropertyGroup><ItemGroup><SmileSource Include=\"Program.smile\" StartupOnly=\"true\" /><Asset Include=\"Assets\\Data.txt\" /></ItemGroup></SmileProject>");

        var webOutput = Path.Combine(directory, "Web");
        Equal(0, new CompilerDriver().Run(new[]
            { "--project", projectPath, "--target", "web", "--output-dir", webOutput }));
        var priorWeb = WebOutputWriter.ManagedFileNames.Concat(new[] { "Assets/Data.txt", "smile-assets.json" })
            .ToDictionary(relative => relative,
                relative => File.ReadAllBytes(Path.Combine(webOutput, relative.Replace('/', Path.DirectorySeparatorChar))),
                StringComparer.Ordinal);
        File.WriteAllText(Path.Combine(webOutput, "sentinel.txt"), "unrelated");

        var nativeOutput = Path.Combine(directory, "bin", "Rollback.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(nativeOutput)!);
        File.WriteAllText(nativeOutput, "prior-executable");
        var project = SmileProjectSourceSet.Load(projectPath);
        SmileProjectAssetPublisher.Publish(project.AssetManifest, Path.GetDirectoryName(nativeOutput)!,
            project.EffectiveApplicationId, "windows-x64", Path.GetFileNameWithoutExtension(nativeOutput));
        var priorNativeAsset = File.ReadAllBytes(Path.Combine(directory, "bin", "Assets", "Data.txt"));
        var priorNativeManifest = File.ReadAllBytes(Path.Combine(directory, "bin", "Rollback.smile-assets.json"));

        File.WriteAllText(sourcePath, "Print \"replacement\"\n");
        File.WriteAllText(assetPath, "replacement-asset");
        var failedAssetHooks = new CompilerDriverTestHooks
        {
            AssetPublicationHook = (stage, _) =>
            {
                if (stage == SmileAssetPublicationStage.BeforeAssetStage)
                    throw new IOException("Synthetic staged asset copy failure.");
            }
        };
        Equal(1, new CompilerDriver(failedAssetHooks).Run(new[]
            { "--project", projectPath, "--target", "web", "--output-dir", webOutput }));
        foreach (var prior in priorWeb)
            Equal(true, prior.Value.SequenceEqual(File.ReadAllBytes(Path.Combine(webOutput,
                prior.Key.Replace('/', Path.DirectorySeparatorChar)))));
        Equal("unrelated", File.ReadAllText(Path.Combine(webOutput, "sentinel.txt")));

        failedAssetHooks = new CompilerDriverTestHooks
        {
            RunNativeToolchain = invocation =>
            {
                File.WriteAllText(invocation.OutputPath, "replacement-executable");
                return new ToolchainResult(true, string.Empty);
            },
            AssetPublicationHook = (stage, _) =>
            {
                if (stage == SmileAssetPublicationStage.BeforeAssetStage)
                    throw new IOException("Synthetic staged asset copy failure.");
            }
        };
        Equal(1, new CompilerDriver(failedAssetHooks).Run(new[]
            { "--project", projectPath, "--target", "windows-x64", "-o", nativeOutput }));
        Equal("prior-executable", File.ReadAllText(nativeOutput));
        Equal(true, priorNativeAsset.SequenceEqual(
            File.ReadAllBytes(Path.Combine(directory, "bin", "Assets", "Data.txt"))));
        Equal(true, priorNativeManifest.SequenceEqual(
            File.ReadAllBytes(Path.Combine(directory, "bin", "Rollback.smile-assets.json"))));
        Equal(0, Directory.EnumerateDirectories(directory, ".*.smile-staging-*").Count());
        Equal(0, Directory.EnumerateDirectories(Path.Combine(directory, "bin"), ".*.smile-staging-*").Count());
    }
    finally { Directory.Delete(directory, true); }
});

Run("Native Debug C uses deterministic ASCII identifiers while retaining safe aliases", () =>
{
    const string longName = "ThisIdentifierIsDeliberatelyLongEnoughToProveThereIsNoTruncationCollision";
    var path = Path.Combine(Path.GetTempPath(), "SMILE 除錯 path", "來源.smile");
    var source = "Option Explicit\nDim auto As Number\nDim MixedCase As Number\nDim Café As Number\n" +
                 "Dim 變數 As Number\nDim " + longName + " As Number\nPrint auto + MixedCase\n";
    var analysis = SmileLanguage.Analyze(new[] { new SmileSourceDocument(source, path, isStartup: true) });
    Equal(false, analysis.HasErrors);
    var emitter = new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, emitDebugInformation: true);
    _ = emitter.Emit();
    var first = CompilerDriver.BuildDebugSource(emitter.DebugSites);
    var second = CompilerDriver.BuildDebugSource(emitter.DebugSites);
    Equal(first, second);
    Equal(true, first.Contains("smile_debug_v0", StringComparison.Ordinal));
    Equal(true, first.Contains("MixedCase = smile_debug_v", StringComparison.Ordinal));
    Equal(false, first.Contains("long long auto", StringComparison.Ordinal));
    Equal(false, first.Contains(" Café", StringComparison.Ordinal));
    Equal(false, first.Contains(" 變數", StringComparison.Ordinal));
    Equal(true, first.Contains(path.Replace("\\", "\\\\"), StringComparison.Ordinal));
});

Run("Native compiler isolates intermediates and serializes identical output targets", () =>
{
    var first = CompilerDriver.CreateIntermediateBaseName("Game", keepTemp: false);
    var second = CompilerDriver.CreateIntermediateBaseName("Game", keepTemp: false);
    Equal(false, string.Equals(first, second, StringComparison.Ordinal));
    Equal(true, first.StartsWith("Game.", StringComparison.Ordinal));
    Equal("Game", CompilerDriver.CreateIntermediateBaseName("Game", keepTemp: true));
    Equal(CompilerDriver.CreateNativeBuildMutexName("bin/Debug/Game.exe"),
        CompilerDriver.CreateNativeBuildMutexName("bin/Debug/Game.exe"));
    Equal(false, string.Equals(
        CompilerDriver.CreateNativeBuildMutexName("bin/Debug/Game.exe"),
        CompilerDriver.CreateNativeBuildMutexName("bin/Release/Game.exe"),
        StringComparison.Ordinal));
});

Run("Smile.UI 2.0 publishes Class facades while keeping the hardened handle engines private", () =>
{
    var project = File.ReadAllText("libraries/Smile.UI/Smile.UI.smilelibproj");
    var core = File.ReadAllText("libraries/Smile.UI/Core.smile");
    var menu = File.ReadAllText("libraries/Smile.UI/Menu.smile");
    var navigator = File.ReadAllText("libraries/Smile.UI/MenuNavigator.smile");
    var dialogue = File.ReadAllText("libraries/Smile.UI/Dialogue.smile");
    Equal(true, project.Contains("<Version>2.0.0</Version>", StringComparison.Ordinal));
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
    Equal(true, menu.Contains("Public Class Menu", StringComparison.Ordinal));
    Equal(true, navigator.Contains("Module Smile.UI.Menu", StringComparison.Ordinal));
    Equal(true, navigator.Contains("Public Class MenuNavigator", StringComparison.Ordinal));
    Equal(true, dialogue.Contains("Public Class Dialogue", StringComparison.Ordinal));
    foreach (var member in new[] { "SetItemHasSubmenu", "ItemHasSubmenu", "SetPosition", "ResetSelection",
        "Update", "DrawFocused" })
        Equal(true, menu.Contains("Public Function " + member + "(", StringComparison.Ordinal) ||
            menu.Contains("Public Sub " + member + "(", StringComparison.Ordinal));
    foreach (var property in new[] { "Valid", "ItemRevision", "ItemCount", "SelectedIndex", "SelectedValue",
        "TopIndex", "VisibleRows", "Bounds", "SelectedRowRect" })
        Equal(true, menu.Contains("Public Property " + property, StringComparison.Ordinal));
    foreach (var member in new[] { "BindSubmenu", "UnbindSubmenu", "ClearBindings", "OpenSelected", "Back",
        "Update", "Relayout", "DrawActive", "Draw" })
        Equal(true, navigator.Contains("Public Function " + member + "(", StringComparison.Ordinal) ||
            navigator.Contains("Public Sub " + member + "(", StringComparison.Ordinal));
    foreach (var property in new[] { "Valid", "Depth", "CanGoBack", "LastAcceptedIndex", "LastAcceptedValue" })
        Equal(true, navigator.Contains("Public Property " + property, StringComparison.Ordinal));
    foreach (var oldPublicRoutine in new[] { "Create", "HandleKey", "DrawStack", "RootMenu",
        "CurrentMenu", "MenuAtDepth", "ParentMenu", "LastAcceptedMenu" })
        Equal(false, navigator.Contains("Public Function " + oldPublicRoutine + "(", StringComparison.Ordinal) ||
            navigator.Contains("Public Sub " + oldPublicRoutine + "(", StringComparison.Ordinal));
    Equal(true, menu.Contains("Private Function MenuHandleCreate(", StringComparison.Ordinal));
    Equal(true, navigator.Contains("Private Function NavigatorHandleCreate(", StringComparison.Ordinal));
    Equal(true, dialogue.Contains("Private Function DialogueHandleCreate(", StringComparison.Ordinal));
    Equal(true, core.Contains("ShowSubmenuIndicator As Boolean", StringComparison.Ordinal));
    Equal(true, core.Contains("SubmenuIndicatorPosition As Number", StringComparison.Ordinal));
    Equal(true, navigator.Contains("MenuHandleSelectedIndex(ParentHandle) <> StackParentItems[Slot, Level]", StringComparison.Ordinal));
    Equal(true, navigator.Contains("BindingIndex = FindBinding(Slot, CurrentHandle, SelectedItem)", StringComparison.Ordinal));
    Equal(true, menu.Contains("TextBlockHeight = PreparedLineCount * LineHeight", StringComparison.Ordinal));
    Equal(true, menu.Contains("CursorY = RowY + Max(0, (RowDrawHeight - MenuStyles[Slot].CursorHeight) / 2)", StringComparison.Ordinal));
    Equal(true, menu.Contains("MarkerY = DrawLabel", StringComparison.Ordinal));
    Equal(true, navigator.Contains("Call MenuHandleDrawFocused(StackMenus[Slot, Level], True)", StringComparison.Ordinal));
});

Run("MenuGallery uses the Smile.UI 2.0 Class API, With, and named/default arguments", () =>
{
    var gallery = File.ReadAllText("examples/MenuGallery/Program.smile");
    Equal(true, gallery.Contains("Import Smile.UI.Menu As Menus", StringComparison.Ordinal));
    Equal(false, gallery.Contains("Import Smile.UI.MenuNavigator", StringComparison.Ordinal));
    Equal(true, gallery.Contains("New Menus.Menu(", StringComparison.Ordinal));
    Equal(true, gallery.Contains("New Menus.MenuNavigator(", StringComparison.Ordinal));
    Equal(true, gallery.Contains("Navigator.Update(", StringComparison.Ordinal));
    Equal(true, gallery.Contains("Call Navigator.Draw()", StringComparison.Ordinal));
    Equal(true, gallery.Contains("Navigator.LastAcceptedValue", StringComparison.Ordinal));
    Equal(true, gallery.Contains("With FontDefinition", StringComparison.Ordinal));
    Equal(true, gallery.Contains("With SkinWindow", StringComparison.Ordinal));
    Equal(true, gallery.Contains("Enabled:=False", StringComparison.Ordinal));
    Equal(false, gallery.Contains("MenuDepth", StringComparison.Ordinal));
    Equal(false, gallery.Split('\n').Any(line => line.Contains(".AddItem", StringComparison.Ordinal) &&
        line.Contains(" >", StringComparison.Ordinal)));
});

Run("Smile.UI 2.0 Class completion Quick Info and definitions use the official project provider", () =>
{
    var compilation = SmileProjectCompilation.Load("examples/MenuGallery/MenuGallery.smileproj");
    var analysis = SmileLanguage.Analyze(compilation.Sources, compilation.CompilationKind,
        compilation.DependencyContext);
    Equal(false, analysis.HasErrors);
    var programPath = Path.GetFullPath("examples/MenuGallery/Program.smile");
    var program = File.ReadAllText(programPath);
    var tree = analysis.GetSyntaxTree(programPath);

    var memberPosition = program.IndexOf("RootMenu.AddItem", StringComparison.Ordinal) +
                         "RootMenu.".Length;
    var completions = SmileCompletionService.GetCompletions(analysis, tree, memberPosition);
    foreach (var expected in new[] { "AddItem", "Destroy", "Draw", "SelectedIndex", "Valid" })
        Equal(true, completions.Any(completion => completion.DisplayText == expected));
    foreach (var obsolete in new[] { "Create", "HandleKey", "SetSelectedIndex" })
        Equal(false, completions.Any(completion => completion.DisplayText == obsolete));

    Equal(true, SmileSymbolService.TryResolve(analysis, tree, memberPosition + 2,
        out var addItem));
    Equal(SmileResolvedSymbolKind.Function, addItem.Kind);
    var addItemPresentation = SmileSymbolDisplayService.Present(addItem, compilation.DependencyContext);
    Equal(true, addItemPresentation.Signature.Contains(
        "Menu.AddItem(Label As Text, UserValue As Number, Optional Enabled As Boolean = True) As Number",
        StringComparison.Ordinal));
    Equal("SMILE 2.0 built-in library Smile.UI@2.0.0", addItemPresentation.Provider);
    Equal("Menu.smile", Path.GetFileName(addItem.DeclarationLocation!.FilePath));
    Equal(false, string.IsNullOrWhiteSpace(addItem.Documentation.Summary));

    var enabledPosition = program.IndexOf("Enabled:=False", StringComparison.Ordinal);
    Equal(true, SmileSymbolService.TryResolve(analysis, tree, enabledPosition + 2,
        out var enabled));
    Equal(SmileResolvedSymbolKind.NamedArgument, enabled.Kind);
    Equal("Enabled", enabled.Name);
    Equal("SMILE 2.0 built-in library Smile.UI@2.0.0",
        SmileSymbolDisplayService.Present(enabled, compilation.DependencyContext).Provider);

    var selectedPosition = program.IndexOf("RootMenu.SelectedIndex", StringComparison.Ordinal) +
                           "RootMenu.".Length;
    Equal(true, SmileSymbolService.TryResolve(analysis, tree, selectedPosition + 2,
        out var selected));
    Equal(SmileResolvedSymbolKind.Property, selected.Kind);
    Equal(true, SmileSymbolDisplayService.Present(selected, compilation.DependencyContext).Signature
        .Contains("Menu.SelectedIndex As Number { Get; Set }", StringComparison.Ordinal));
    Equal("Menu.smile", Path.GetFileName(selected.DeclarationLocation!.FilePath));
});

Run("Type members parse as structured declarations and true receiver invocations", () =>
{
    const string source = "Option Explicit\nType Position\nPublic X As Number\nY As Number\nPublic Sub MoveBy(DX As Number)\nMe.X = Me.X + DX\nEnd Sub\nFunction Sum() As Number\nReturn Me.X + Me.Y\nEnd Function\nProperty Current As Number\nGet\nReturn Me.X\nEnd Get\nSet\nMe.X = Value\nEnd Set\nEnd Property\nEnd Type\nType Actor\nPosition As Position\nEnd Type\nDim Current As Position\nDim Party[2] As Actor\nDim Total As Number\nCall Current.MoveBy(1)\nCall Party[0].Position.MoveBy(2)\nCall (Current).MoveBy(3)\nTotal = Current.Sum()\n";
    var analysis = Analyze(source);
    Equal(false, analysis.HasErrors);
    var type = analysis.SyntaxTree.Root.Statements.OfType<TypeDeclarationSyntax>().First();
    Equal(5, type.Members.Count);
    Equal(2, type.Fields.Count);
    Equal(SyntaxKind.PublicKeyword, type.Fields[0].VisibilityKeyword!.Kind);
    Equal(true, type.Members[2] is TypeRoutineDeclarationSyntax);
    var property = (PropertyDeclarationSyntax)type.Members[4];
    Equal(true, property.Getter != null);
    Equal(true, property.Setter != null);

    var calls = analysis.SyntaxTree.Root.Statements.OfType<MemberCallStatementSyntax>().ToArray();
    Equal(3, calls.Length);
    Equal(true, calls[0].Receiver is NameExpressionSyntax);
    Equal(true, calls[1].Receiver is FieldAccessExpressionSyntax
        { Receiver: ArrayAccessExpressionSyntax });
    Equal(true, calls[2].Receiver is ParenthesizedExpressionSyntax);
    var assignment = analysis.SyntaxTree.Root.Statements.OfType<AssignmentStatementSyntax>().Last();
    Equal(true, assignment.Expression is MemberInvocationExpressionSyntax
        { Receiver: NameExpressionSyntax });
});

Run("Type member semantic APIs keep hidden instance ABI state outside user parameters", () =>
{
    const string source = "Option Explicit\nType Point\nPublic X As Number\nY As Number\nPublic Sub MoveBy(DX As Number, DY As Number)\nMe.X = Me.X + DX\nMe.Y = Me.Y + DY\nEnd Sub\nPrivate Function Secret() As Number\nReturn Me.X\nEnd Function\nPublic Property Current As Number\nGet\nReturn Me.X\nEnd Get\nSet\nMe.X = Value\nEnd Set\nEnd Property\nEnd Type\nDim P As Point\nCall P.MoveBy(DY := 2, DX := 1)\nP.Current = NextValue()\nPrint P.Current\nWith P\nCall .MoveBy(1, 1)\nPrint .Current\nEnd With\nFunction NextValue() As Number\nReturn 3\nEnd Function\n";
    var analysis = Analyze(source);
    if (analysis.HasErrors)
        throw new InvalidOperationException(string.Join(" | ", analysis.Diagnostics.Select(diagnostic =>
            diagnostic.Code + ": " + diagnostic.Message)));

    var point = analysis.SemanticModel.Types["Point"];
    Equal(5, point.Members.Count);
    Equal(2, point.Methods.Count);
    Equal(1, point.Properties.Count);
    Equal(1, analysis.SemanticModel.Routines.Count);
    Equal(5, analysis.SemanticModel.AllRoutines.Count);
    var move = point.Methods.Single(method => method.Name == "MoveBy");
    Equal(RoutineSymbolKind.TypeMethod, move.SymbolKind);
    Equal(2, move.Parameters.Count);
    Equal(true, move.Receiver != null);
    Equal(ParameterPassingMode.ByRef, move.Receiver!.ParameterMode);
    Equal(true, move.LocalSymbols.ContainsKey("Me"));
    Equal(false, move.Parameters.Any(parameter => parameter.Name == "Me"));

    var property = point.Properties.Single();
    Equal(true, property.Getter != null);
    Equal(true, property.Setter != null);
    Equal(false, property.Getter!.RuntimeIdentity == property.Setter!.RuntimeIdentity);
    Equal(true, property.Getter.RuntimeIdentity.StartsWith(point.RuntimeIdentity, StringComparison.Ordinal));
    Equal(0, property.Setter.Parameters.Count);
    Equal(true, property.Setter.LocalSymbols.ContainsKey("Me"));
    Equal(true, property.Setter.LocalSymbols.ContainsKey("Value"));
    Equal(ParameterPassingMode.ByVal, property.Setter.SetterValue!.ParameterMode);

    var directCall = analysis.BoundSyntaxTree.Root.Statements.OfType<MemberCallStatementSyntax>().Single();
    Equal(true, analysis.SemanticModel.TryGetBoundCall(directCall, out var boundMethod));
    Equal(BoundInstanceReceiverKind.Expression, boundMethod.InstanceReceiver!.Kind);
    Equal(false, boundMethod.EvaluateReceiverAfterImplicitValue);
    Equal("DY|DX", string.Join("|", boundMethod.SourceArguments.Select(argument => argument.Parameter.Name)));
    Equal("DX|DY", string.Join("|", boundMethod.ParameterArguments.Select(argument => argument.Parameter.Name)));

    var setterAssignment = analysis.BoundSyntaxTree.Root.Statements.OfType<AssignmentStatementSyntax>()
        .Single(statement => statement.Target.Location is FieldAccessExpressionSyntax field &&
            field.Field.Text == "Current");
    Equal(true, analysis.SemanticModel.TryGetBoundCall(setterAssignment, out var boundSetter));
    Equal(RoutineSymbolKind.PropertySet, boundSetter.Routine.SymbolKind);
    Equal(true, boundSetter.EvaluateReceiverAfterImplicitValue);
    Equal(true, ReferenceEquals(setterAssignment.Expression, boundSetter.ImplicitValue));
    Equal(0, boundSetter.SourceArguments.Count);
    Equal(0, boundSetter.ParameterArguments.Count);

    var getter = analysis.BoundSyntaxTree.Root.Statements.OfType<PrintStatementSyntax>().Single().Items.Single();
    Equal(true, analysis.SemanticModel.TryGetBoundCall(getter, out var boundGetter));
    Equal(RoutineSymbolKind.PropertyGet, boundGetter.Routine.SymbolKind);
    var withCall = analysis.BoundSyntaxTree.Root.Statements.OfType<WithStatementSyntax>().Single()
        .Statements.OfType<LeadingMemberCallStatementSyntax>().Single();
    Equal(true, analysis.SemanticModel.TryGetBoundCall(withCall, out var boundWithCall));
    Equal(BoundInstanceReceiverKind.WithTarget, boundWithCall.InstanceReceiver!.Kind);

    Equal(true, analysis.SemanticModel.TryResolveVariable("Me", move.RuntimeIdentity, out var resolvedMe));
    Equal(true, ReferenceEquals(move.Receiver, resolvedMe));
    Equal(true, analysis.SemanticModel.TryResolveVariable("Value", property.Setter.RuntimeIdentity,
        out var resolvedValue));
    Equal(true, ReferenceEquals(property.Setter.SetterValue, resolvedValue));
});

Run("Type member identities cascade from linked module providers", () =>
{
    const string program = "Option Explicit\nImport Example.Models As Models\nDim P As Models.Point\nCall P.Move(OptionalStep := 2)\n";
    const string module = "Module Example.Models\nPublic Type Point\nX As Number\nPublic Sub Move(Optional OptionalStep As Number = 1)\nMe.X = Me.X + OptionalStep\nEnd Sub\nPublic Property Current As Number\nGet\nReturn Me.X\nEnd Get\nEnd Property\nEnd Type\nEnd Module\n";
    var analysis = Multi(("Program.smile", true, program), ("Models.smile", false, module));
    Equal(false, analysis.HasErrors);
    var point = analysis.SemanticModel.Types.Values.Single();
    var move = point.Methods.Single();
    var property = point.Properties.Single();
    Equal("Example.Models", point.ModuleName);
    Equal("<local>", point.ProviderIdentity);
    Equal(true, move.RuntimeIdentity.StartsWith(point.RuntimeIdentity + "::member::", StringComparison.Ordinal));
    Equal(point.ProviderIdentity, move.ProviderIdentity);
    Equal(point.ProviderIdentity, move.Parameters[0].ProviderIdentity);
    Equal(point.ProviderIdentity, move.Receiver!.ProviderIdentity);
    Equal(true, property.RuntimeIdentity.StartsWith(point.RuntimeIdentity + "::property::", StringComparison.Ordinal));
    Equal(true, property.Getter!.RuntimeIdentity.EndsWith("::get", StringComparison.Ordinal));
});

Run("Type member parser recovers missing nested terminators without swallowing later members", () =>
{
    const string source = "Type Broken\nPublic X As Number\nProperty Score As Number\nGet\nReturn Me.X\nSet\nMe.X = Value\nEnd Set\nY As Number\nSub Work()\nMe.Y = 1\nZ As Number\nEnd Type\nDim Current As Broken\n";
    var analysis = Analyze(source);
    Equal(3, analysis.Diagnostics.Count(diagnostic => diagnostic.Code == "SML2001"));
    var type = analysis.SemanticModel.Types["Broken"];
    Equal("X|Y|Z", string.Join("|", type.Fields.Select(field => field.Name)));
    Equal(1, type.Properties.Count);
    Equal(true, type.Properties[0].Getter != null);
    Equal(true, type.Properties[0].Setter != null);
    Equal(1, type.Methods.Count);
    Equal(true, analysis.SemanticModel.Symbols.ContainsKey("Current"));
    var syntaxType = analysis.SyntaxTree.Root.Statements.OfType<TypeDeclarationSyntax>().Single();
    Equal(SyntaxKind.PublicKeyword, syntaxType.Fields[0].VisibilityKeyword!.Kind);

    var strayVisibility = Analyze("Type Item\nValue As Number\nPublic\nEnd Type\nDim Current As Item\n");
    Equal(true, HasDiagnostic(strayVisibility, "SML3440"));
    Equal(true, strayVisibility.SemanticModel.Symbols.ContainsKey("Current"));
});

Run("Type member diagnostics cover namespace context receivers accessors and privacy", () =>
{
    Equal(true, HasDiagnostic(Analyze("Type Item\nValue As Number\nSub Value()\nEnd Sub\nEnd Type\n"), "SML3440"));
    Equal(true, HasDiagnostic(Analyze("Type Item\nProperty Value As Number\nEnd Property\nEnd Type\n"), "SML3441"));
    Equal(true, HasDiagnostic(Analyze("Print Me.X\n"), "SML3442"));
    Equal(true, HasDiagnostic(Analyze("Type Item\nValue As Number\nEnd Type\nDim Current As Item\nCall Current.Missing()\n"), "SML3443"));
    Equal(true, HasDiagnostic(Analyze("Type Item\nValue As Number\nSub Reset()\nEnd Sub\nEnd Type\nCall Make().Reset()\nFunction Make() As Item\nDim Result As Item\nReturn Result\nEnd Function\n"), "SML3444"));
    Equal(true, HasDiagnostic(Analyze("Type Item\nValue As Number\nProperty ReadOnly As Number\nGet\nReturn Me.Value\nEnd Get\nEnd Property\nProperty WriteOnly As Number\nSet\nMe.Value = Value\nEnd Set\nEnd Property\nEnd Type\nDim Current As Item\nCurrent.ReadOnly = 1\nPrint Current.WriteOnly\n"), "SML3445"));
    Equal(true, HasDiagnostic(Analyze("Type Item\nValue As Number\nPrivate Sub Hide()\nEnd Sub\nEnd Type\nDim Current As Item\nCall Current.Hide()\n"), "SML3446"));
    Equal(true, HasDiagnostic(Analyze("Type Item\nPrivate Value As Number\nEnd Type\n"), "SML3440"));
    Equal(true, HasDiagnostic(Analyze("Type Item\nValue As Number\nSub Reject()\nCall Take(Me)\nEnd Sub\nEnd Type\nSub Take(ByRef Value As Item)\nEnd Sub\n"), "SML3442"));
});

Run("Type members preserve local enum shadows and diagnose project-wide import alias collisions", () =>
{
    const string program = "Option Explicit\nImport Example.Model As Model\nDim Item As Model.Holder\nCall Item.Read(Item)\n";
    const string module = "Module Example.Model\nPublic Enum State\nReady\nEnd Enum\nPublic Type Holder\nValue As Number\nPublic Sub Read(State As Holder)\nPrint State.Value\nEnd Sub\nEnd Type\nEnd Module\n";
    Equal(false, Multi(("Program.smile", true, program), ("Model.smile", false, module)).HasErrors);

    const string aliasProgram = "Import Example.Tools As Shared\nPrint 1\n";
    const string globals = "Dim Shared As Number\n";
    const string tools = "Module Example.Tools\nPublic Sub Work()\nEnd Sub\nEnd Module\n";
    Equal(true, HasDiagnostic(Multi(("Program.smile", true, aliasProgram),
        ("Globals.smile", false, globals), ("Tools.smile", false, tools)), "SML3106"));
    const string implicitAliasProgram = "Import Example.Tools As Shared\nShared = 1\n";
    Equal(true, HasDiagnostic(Multi(("Program.smile", true, implicitAliasProgram),
        ("Tools.smile", false, tools)), "SML3106"));
});

Run("Type member APIs validate visibility capabilities and same-named routine identities", () =>
{
    const string publicApi = "Module Example.Api\nPrivate Type Hidden\nValue As Number\nEnd Type\nPublic Type Visible\nValue As Number\nPublic Function Leak(Input As Hidden) As Hidden\nDim Result As Hidden\nReturn Result\nEnd Function\nPublic Property Item As Hidden\nGet\nDim Result As Hidden\nReturn Result\nEnd Get\nEnd Property\nPrivate Function PrivateLeak(Input As Hidden) As Hidden\nDim Result As Hidden\nReturn Result\nEnd Function\nEnd Type\nEnd Module\n";
    var apiAnalysis = Multi(("Program.smile", true, "Print 1\n"),
        ("Api.smile", false, publicApi));
    Equal(3, apiAnalysis.Diagnostics.Count(diagnostic => diagnostic.Code == "SML3409"));

    const string capabilities = "Type Meter\nValue As Number\nSub NeedsGame()\nClear RED\nEnd Sub\nProperty Reading As Number\nGet\nReturn Me.Value\nEnd Get\nSet\nCall Me.NeedsGame()\nMe.Value = Value\nEnd Set\nEnd Property\nEnd Type\nType First\nSub Reset()\nDim State As Number\nEnd Sub\nEnd Type\nType Second\nSub Reset()\nDim State As Text\nEnd Sub\nEnd Type\n";
    var capabilityAnalysis = Analyze(capabilities);
    Equal(false, capabilityAnalysis.HasErrors);
    var meter = capabilityAnalysis.SemanticModel.Types["Meter"];
    var needsGame = meter.Methods.Single();
    var reading = meter.Properties.Single();
    Equal(true, needsGame.RequiresGameWindow);
    Equal(false, reading.Getter!.RequiresGameWindow);
    Equal(true, reading.Setter!.RequiresGameWindow);

    var resetMethods = capabilityAnalysis.SemanticModel.AllRoutines
        .Where(routine => routine.Name == "Reset").ToArray();
    Equal(2, resetMethods.Length);
    Equal(false, resetMethods[0].RuntimeIdentity == resetMethods[1].RuntimeIdentity);
    Equal(true, capabilityAnalysis.SemanticModel.TryResolveVariable("State",
        resetMethods[0].RuntimeIdentity, out var firstState));
    Equal(true, capabilityAnalysis.SemanticModel.TryResolveVariable("State",
        resetMethods[1].RuntimeIdentity, out var secondState));
    Equal(false, firstState.Type == secondState.Type);
});

Run("Properties are values rather than writable locations and setter Value remains contextual", () =>
{
    const string source = "Type Inner\nValue As Number\nSub Touch(Optional Delta As Number = 1)\nMe.Value = Me.Value + Delta\nEnd Sub\nProperty Amount As Number\nGet\nReturn Me.Value\nEnd Get\nEnd Property\nEnd Type\nType Outer\nBacking As Inner\nProperty Child As Inner\nGet\nReturn Me.Backing\nEnd Get\nEnd Property\nProperty Count As Number\nSet\nDim Value As Number\nEnd Set\nEnd Property\nEnd Type\nDim Current As Outer\nCall Take(Current.Child)\nWith Current.Child\nPrint .Value\nEnd With\nCall Current.Child.Touch()\nSub Take(ByRef Value As Inner)\nEnd Sub\n";
    var analysis = Analyze(source);
    Equal(true, HasDiagnostic(analysis, "SML3305"));
    Equal(true, HasDiagnostic(analysis, "SML3412"));
    Equal(true, HasDiagnostic(analysis, "SML3444"));
    Equal(true, HasDiagnostic(analysis, "SML3306"));
    var propertyResultDot = source.IndexOf("Current.Child.Touch", StringComparison.Ordinal) +
                            "Current.Child.".Length;
    var propertyResultCompletions = SmileCompletionService.GetCompletions(analysis, propertyResultDot);
    Equal(true, propertyResultCompletions.Any(completion => completion.DisplayText == "Value"));
    Equal(false, propertyResultCompletions.Any(completion => completion.DisplayText == "Touch"));
    Equal(false, propertyResultCompletions.Any(completion => completion.DisplayText == "Amount"));
    var propertyResultCall = source.IndexOf("Current.Child.Touch(", StringComparison.Ordinal) +
                             "Current.Child.Touch(".Length;
    Equal(false, SmileCompletionService.GetCompletions(analysis, propertyResultCall)
        .Any(completion => completion.Kind == SmileCompletionKind.Parameter &&
                           completion.InsertionText.EndsWith(":=", StringComparison.Ordinal)));

    const string ordinaryValue = "Option Explicit\nDim Value As Number\nCall Use(Value)\nSub Use(Value As Number)\nPrint Value\nEnd Sub\n";
    Equal(false, Analyze(ordinaryValue).HasErrors);
});

Run("Classes bind constructors reference identity fields methods properties and fixed arrays", () =>
{
    const string source = "Option Explicit\nType Point\nX As Number\nEnd Type\nClass Counter\nPublic Current As Number\nPrivate Label As Text\nPublic Samples[2, 3] As Number\nPrivate Position As Point\nPublic Sub New(Optional Start As Number = 1, Optional Name As Text = \"Counter\")\nMe.Current = Start\nMe.Label = Name\nEnd Sub\nPublic Sub Add(Optional Delta As Number = 1)\nMe.Current = Me.Current + Delta\nMe.Samples[1, 2] = Me.Current\nEnd Sub\nPublic Function Alias() As Counter\nReturn Me\nEnd Function\nPublic Property Value As Number\nGet\nReturn Me.Current\nEnd Get\nSet\nMe.Current = Value\nEnd Set\nEnd Property\nEnd Class\nClass Empty\nEnd Class\nDim First As Counter\nDim Second As New Counter(Start := 4)\nFirst = New Counter(Name := \"First\", Start := 2)\nCall First.Add(Delta := 3)\nSecond = First\nPrint First Is Second\nSecond = Nothing\nPrint Second Is Nothing\nWith First\n.Value = 9\nCall .Add()\nPrint .Value\nEnd With\n";
    var analysis = Analyze(source);
    if (analysis.HasErrors)
        throw new InvalidOperationException(string.Join(" | ", analysis.Diagnostics.Select(diagnostic =>
            diagnostic.Code + ": " + diagnostic.Message)));

    var counter = analysis.SemanticModel.Classes["Counter"];
    Equal(8, counter.Size);
    Equal(4, counter.Fields.Count);
    Equal("Current|Label|Samples|Position", string.Join("|", counter.Fields.Select(field => field.Name)));
    Equal(2, counter.Fields[2].ArrayRank);
    Equal("2|3", string.Join("|", counter.Fields[2].Dimensions));
    Equal(6, counter.Fields[2].ElementCount);
    Equal(true, counter.InstanceContainsOwnedText);
    Equal(true, counter.RequiresReferenceCleanup);
    Equal(true, counter.Constructor.IsDeclared);
    Equal(RoutineSymbolKind.Constructor, counter.Constructor.SymbolKind);
    Equal(2, counter.Constructor.Parameters.Count);
    Equal(2, counter.Methods.Count);
    Equal(1, counter.Properties.Count);
    Equal(false, counter.Constructor.Parameters.Any(parameter => parameter.Name == "Me"));
    Equal(true, counter.Constructor.LocalSymbols.ContainsKey("Me"));
    Equal(ParameterPassingMode.ByVal, counter.Constructor.Receiver!.ParameterMode);

    var empty = analysis.SemanticModel.Classes["Empty"];
    Equal(false, empty.Constructor.IsDeclared);
    Equal(0, empty.Constructor.Parameters.Count);
    Equal(empty.Declaration.Identifier.Span, empty.Constructor.DeclarationSpan);

    var creations = analysis.BoundSyntaxTree.Root.Statements
        .SelectMany(statement => statement switch
        {
            DimStatementSyntax { NewInitializer: not null } dim => new[] { dim.NewInitializer! },
            AssignmentStatementSyntax { Expression: NewExpressionSyntax creation } => new[] { creation },
            _ => Array.Empty<NewExpressionSyntax>()
        }).ToArray();
    Equal(2, creations.Length);
    foreach (var creation in creations)
    {
        Equal(true, analysis.SemanticModel.TryGetBoundCall(creation, out var constructorCall));
        Equal(RoutineSymbolKind.Constructor, constructorCall.Routine.SymbolKind);
        Equal(false, constructorCall.HasInstanceReceiver);
    }

    var identities = analysis.BoundSyntaxTree.Root.Statements.OfType<PrintStatementSyntax>()
        .Select(statement => statement.Items.Single()).OfType<IdentityExpressionSyntax>().ToArray();
    Equal(2, identities.Length);
    Equal(false, identities[0].IsNegated);
    Equal(false, identities[1].IsNegated);
    Equal(SmileType.Boolean, analysis.SemanticModel.GetType(identities[0]));

    var indexed = counter.Methods.Single(method => method.Name == "Add").BodyStatements
        .OfType<AssignmentStatementSyntax>().Single(statement => statement.Target.Location is IndexedExpressionSyntax)
        .Target.Location;
    Equal(true, analysis.SemanticModel.TryGetInstanceField(indexed, out var indexedField));
    Equal("Samples", indexedField.Name);
    Equal(true, analysis.SemanticModel.TryGetClassLocationOwner(indexed, out var owner));
    Equal(counter, owner.RootType);
});

Run("Classes link through modules with stable constructor identities and public API validation", () =>
{
    const string program = "Option Explicit\nImport Example.Objects As Objects\nDim Item As New Objects.Widget(Start := 3)\nDim Other As Objects.Widget\nOther = New Objects.Widget()\nPrint Item Is Not Other\n";
    const string module = "Module Example.Objects\nPrivate Type Hidden\nValue As Number\nEnd Type\nPublic Class Widget\nPublic Value As Number\nPrivate Secret As Hidden\nPublic Sub New(Optional Start As Number = 1)\nMe.Value = Start\nEnd Sub\nPublic Function Leak(Input As Hidden) As Hidden\nReturn Me.Secret\nEnd Function\nPublic Property HiddenValue As Hidden\nGet\nReturn Me.Secret\nEnd Get\nEnd Property\nEnd Class\nEnd Module\n";
    var analysis = Multi(("Program.smile", true, program), ("Objects.smile", false, module));
    Equal(3, analysis.Diagnostics.Count(diagnostic => diagnostic.Code == "SML3409"));
    Equal(false, analysis.Diagnostics.Any(diagnostic => diagnostic.Code is "SML3101" or "SML3401"));
    var widget = analysis.SemanticModel.Classes.Values.Single();
    Equal("Example.Objects", widget.ModuleName);
    Equal("<local>", widget.ProviderIdentity);
    Equal("Example.Objects::Widget::constructor::New", widget.Constructor.RuntimeIdentity);
    Equal(widget.ProviderIdentity, widget.Constructor.ProviderIdentity);
    Equal(widget.ProviderIdentity, widget.Constructor.Parameters[0].ProviderIdentity);
    Equal(widget.ProviderIdentity, widget.Constructor.Receiver!.ProviderIdentity);
});

Run("Class finalizers preserve reverse declaration and array element order", () =>
{
    const string source = "Option Explicit\nType Payload\nName As Text\nEnd Type\nClass Owner\nPrivate FirstText As Text\nPrivate FirstPayload As Payload\nPrivate Notes[2] As Text\nPrivate LastText As Text\nPrivate Payloads[2] As Payload\nEnd Class\nDim Value As New Owner()\n";
    var analysis = Analyze(source);
    if (analysis.HasErrors)
        throw new InvalidOperationException(string.Join(" | ", analysis.Diagnostics.Select(diagnostic =>
            diagnostic.Code + ": " + diagnostic.Message)));

    var owner = analysis.SemanticModel.Classes["Owner"];
    var expectedOffsets = owner.Fields.Where(field => field.Type.RequiresValueCleanup)
        .OrderByDescending(field => field.Ordinal)
        .SelectMany(field => field.IsArray
            ? Enumerable.Range(0, field.ElementCount).Reverse()
                .Select(index => field.Offset + index * Math.Max(8, field.Type.Size))
            : new[] { field.Offset })
        .ToArray();
    var native = new MasmEmitter(analysis, SmileGraphicsBackend.Auto, true, false).Emit();
    var nativeStart = native.IndexOf("_finalize PROC", StringComparison.Ordinal);
    var nativeEnd = native.IndexOf("_finalize ENDP", nativeStart, StringComparison.Ordinal);
    var nativeFinalizer = native[nativeStart..nativeEnd];
    var previousOffsetPosition = -1;
    foreach (var offset in expectedOffsets)
    {
        var address = offset == 0 ? "lea rcx, [rax]" : $"lea rcx, [rax+{offset}]";
        var position = nativeFinalizer.IndexOf(address, previousOffsetPosition + 1, StringComparison.Ordinal);
        if (position < 0)
            throw new InvalidOperationException($"Native finalizer did not clear offset {offset} in order.");
        previousOffsetPosition = position;
    }

    var web = new WebEmitter(analysis).Emit();
    var webStart = web.IndexOf("_finalize(value) {", StringComparison.Ordinal);
    var webEnd = web.IndexOf("\n}", webStart, StringComparison.Ordinal);
    var webFinalizer = web[webStart..webEnd];
    var previousFieldPosition = -1;
    foreach (var field in owner.Fields.Where(field => field.Type.RequiresValueCleanup)
                 .OrderByDescending(field => field.Ordinal))
    {
        var key = $"__smile_c0_f{field.Ordinal}";
        var position = webFinalizer.IndexOf(key, previousFieldPosition + 1, StringComparison.Ordinal);
        if (position < 0)
            throw new InvalidOperationException($"Web finalizer did not clear field {field.Name} in order.");
        previousFieldPosition = position;
    }
});

Run("Class diagnostics enforce scalar reference storage constructor and identity rules", () =>
{
    var cases = new[]
    {
        ("SML3450", "Class Item\nDim Value As Number\nEnd Class\n"),
        ("SML3451", "Class Item\nPrivate Sub New()\nEnd Sub\nEnd Class\n"),
        ("SML3452", "Class Item\nOther As Item\nEnd Class\n"),
        ("SML3453", "Dim Value As Number\nValue = New Number()\n"),
        ("SML3454", "Dim Value As Number\nValue = Nothing\n"),
        ("SML3455", "Class Item\nEnd Class\nDim A As Item\nDim B As Item\nPrint A = B\n"),
        ("SML3452", "Class Item\nEnd Class\nDim Values[2] As Item\n"),
        ("SML3457", "Print Nothing.Value\n")
    };
    foreach (var (code, source) in cases)
    {
        var analysis = Analyze(source);
        if (!HasDiagnostic(analysis, code))
            throw new InvalidOperationException($"Expected {code}; found " + string.Join(",",
                analysis.Diagnostics.Select(diagnostic => diagnostic.Code)));
    }
});

Run("Lightweight OOP parser recovery remains bounded and preserves later declarations", () =>
{
    var terminatedInvalidClass = Analyze("Class Item\nDim Value As Number\nEnd Class\n");
    var terminatedInvalidClassCodes = terminatedInvalidClass.Diagnostics
        .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
        .Select(diagnostic => diagnostic.Code)
        .ToArray();
    if (!terminatedInvalidClassCodes.SequenceEqual(new[] { "SML3450" }))
        throw new InvalidOperationException("A terminated invalid Class must retain exact SML3450 diagnostics; found " +
            string.Join(",", terminatedInvalidClassCodes));

    var cases = new (string Name, string Source)[]
    {
        ("missing End With",
            "Type Item\nValue As Number\nEnd Type\nDim Current As Item\nSub Broken()\nWith Current\nPrint .Value\nEnd Sub\nDim Later As Number\n"),
        ("extra End With", "End With\nDim Later As Number\n"),
        ("missing End Enum", "Enum Broken\nOne\nDim Later As Number\n"),
        ("malformed enum initializer", "Enum Broken\nOne = )\nEnd Enum\nDim Later As Number\n"),
        ("missing End Type", "Type Broken\nValue As Number\nDim Later As Number\n"),
        ("missing End Class", "Class Broken\nValue As Number\nDim Later As Number\n"),
        ("malformed Sub New",
            "Class Broken\nSub New(\nOptional Start As Number = 1\nValue As Number\nEnd Class\nDim Later As Number\n"),
        ("duplicate constructor",
            "Class Broken\nSub New()\nEnd Sub\nSub New()\nEnd Sub\nEnd Class\nDim Later As Number\n"),
        ("missing End Property",
            "Type Broken\nValue As Number\nProperty Score As Number\nGet\nReturn Me.Value\nEnd Get\nNextValue As Number\nEnd Type\nDim Later As Number\n"),
        ("missing End Get",
            "Type Broken\nValue As Number\nProperty Score As Number\nGet\nReturn Me.Value\nSet\nMe.Value = Value\nEnd Set\nEnd Property\nEnd Type\nDim Later As Number\n"),
        ("missing End Set",
            "Type Broken\nValue As Number\nProperty Score As Number\nSet\nMe.Value = Value\nEnd Property\nEnd Type\nDim Later As Number\n"),
        ("Value outside setter",
            "Type Broken\nValue As Number\nSub Work()\nPrint Value\nEnd Sub\nEnd Type\nDim Later As Number\n"),
        ("malformed Is Not",
            "Class Item\nEnd Class\nDim First As Item\nDim Result As Boolean\nResult = First Is Not\nDim Later As Number\n"),
        ("malformed New", "Class Item\nEnd Class\nDim First As Item\nFirst = New ()\nDim Later As Number\n"),
        ("missing multiline Optional comma",
            "Sub Broken(\nOptional First As Number = 1\nOptional Second As Number = 2\n)\nEnd Sub\nDim Later As Number\n"),
        ("missing multiline declaration close",
            "Sub Broken(\nOptional First As Number = 1\nPrint First\nEnd Sub\nDim Later As Number\n"),
        ("malformed named argument",
            "Sub Work(Value As Number)\nEnd Sub\nCall Work(Value:=)\nDim Later As Number\n"),
        ("positional after named argument",
            "Sub Work(First As Number, Second As Number)\nEnd Sub\nCall Work(First:=1, 2)\nDim Later As Number\n")
    };

    foreach (var (name, source) in cases)
    {
        var analysis = Analyze(source);
        if (!analysis.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            throw new InvalidOperationException($"Parser recovery fixture '{name}' produced no error.");
        if (!analysis.SyntaxTree.Root.Statements.OfType<DimStatementSyntax>()
                .Any(dim => dim.Identifier.Text == "Later"))
            throw new InvalidOperationException($"Parser recovery fixture '{name}' swallowed the later declaration.");
        foreach (var diagnostic in analysis.Diagnostics)
        {
            if (diagnostic.Span.Start < 0 || diagnostic.Span.End > source.Length)
                throw new InvalidOperationException($"Parser recovery fixture '{name}' produced an invalid span.");
        }
    }
});

if (tests.Failures.Count != 0)
{
    Console.Error.WriteLine($"{tests.Failures.Count} SMILE test(s) failed:");
    foreach (var failure in tests.Failures)
        Console.Error.WriteLine("- " + failure);
    return 1;
}

Console.WriteLine($"{tests.Passed} SMILE language, compiler, project, completion, and timing tests passed.");
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

void Run(string name, Action test) => tests.Run(name, test);

void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Expected {expected}, found {actual}.");
}

string ApplyCommentEdits(string source, IReadOnlyList<SmileCommentEdit> edits)
{
    var result = source;
    foreach (var edit in edits.OrderByDescending(edit => edit.Position))
        result = result.Remove(edit.Position, edit.DeleteLength).Insert(edit.Position, edit.InsertText);
    return result;
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

string ReplaceOnce(string text, string oldText, string newText)
{
    var index = text.IndexOf(oldText, StringComparison.Ordinal);
    if (index < 0)
        throw new InvalidOperationException($"Package tamper fixture text was not found: {oldText}");
    return text.Substring(0, index) + newText + text.Substring(index + oldText.Length);
}

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
