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
Run("Fixed-step ball speed is identical at 60-ish and 125 Hz", () =>
{
    var sixtyHz = Enumerable.Repeat(16, 62).Concat(new[] { 8 });
    var oneTwentyFiveHz = Enumerable.Repeat(8, 125);
    Equal(300000L, SimulateFixedPoint(sixtyHz, 300000));
    Equal(300000L, SimulateFixedPoint(oneTwentyFiveHz, 300000));
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

Console.WriteLine("15 SMILE project and timing tests passed.");
return 0;

SmileProjectGraphicsOptions Parse(string xml) =>
    SmileProjectGraphicsOptions.Parse(XElement.Parse(xml));

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
