using System.Xml.Linq;
using Smile.Language;

var failures = new List<string>();

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

if (failures.Count != 0)
{
    Console.Error.WriteLine($"{failures.Count} SMILE project-option test(s) failed:");
    foreach (var failure in failures)
        Console.Error.WriteLine("- " + failure);
    return 1;
}

Console.WriteLine("51 SMILE language, project, and timing tests passed.");
return 0;

SmileProjectGraphicsOptions Parse(string xml) =>
    SmileProjectGraphicsOptions.Parse(XElement.Parse(xml));

SmileAnalysisResult Analyze(string source) => SmileLanguage.Analyze(source);

MusicStatementSyntax Music(SmileAnalysisResult analysis) =>
    analysis.SyntaxTree.Root.Statements.OfType<MusicStatementSyntax>().Single();

bool HasDiagnostic(SmileAnalysisResult analysis, string code) =>
    analysis.Diagnostics.Any(diagnostic => diagnostic.Code == code);

void Run(string name, Action test)
{
    try
    {
        test();
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
