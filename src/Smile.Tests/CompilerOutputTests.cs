using System.IO.Pipes;
using Smile.Compiler;
using Smile.VisualStudio;

internal static class CompilerOutputTests
{
    public static void Register(TestContext tests)
    {
        tests.Run("VSIX compiler output appears before the compiler pipe closes", () =>
            VerifyStreamingAsync().GetAwaiter().GetResult());
        tests.Run("Compiler reports the active phase while a build is still running", () =>
            VerifyProgressAsync().GetAwaiter().GetResult());
    }

    private static async Task VerifyProgressAsync()
    {
        var heartbeat = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var output = new ProgressWriter(heartbeat);
        using var progress = new CompilerProgress(output, TimeSpan.FromMilliseconds(20));
        progress.Report("Analyzing sources");
        var observed = await heartbeat.Task.WaitAsync(TimeSpan.FromSeconds(3));
        if (!observed.Contains("Still working: Analyzing sources"))
            throw new Exception("The heartbeat did not identify the current build phase.");
        progress.Dispose();
        var finishedLength = output.ToString().Length;
        progress.Report("Must not appear after disposal");
        if (output.ToString().Length != finishedLength)
            throw new Exception("Progress continued after the build ended.");
    }

    private sealed class ProgressWriter(TaskCompletionSource<string> heartbeat) : StringWriter
    {
        public override void WriteLine(string? value)
        {
            base.WriteLine(value);
            if (value?.Contains("Still working: Analyzing sources") == true)
                heartbeat.TrySetResult(value);
        }
    }

    private static async Task VerifyStreamingAsync()
    {
        using var output = new AnonymousPipeServerStream(PipeDirection.Out);
        using var input = new AnonymousPipeClientStream(PipeDirection.In, output.ClientSafePipeHandle);
        using var reader = new StreamReader(input);
        using var writer = new StreamWriter(output) { AutoFlush = true };
        var firstLine = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var captured = CompilerOutput.ReadAsync(reader, line => firstLine.TrySetResult(line));
        await writer.WriteLineAsync("Compiling sources");
        var observed = await firstLine.Task.WaitAsync(TimeSpan.FromSeconds(3));
        if (observed != "Compiling sources\r\n" || captured.IsCompleted)
            throw new Exception("Output was not delivered while the compiler pipe was still open.");
        await writer.WriteAsync("warning: final line without newline");
        writer.Dispose();
        var result = await captured.WaitAsync(TimeSpan.FromSeconds(3));
        if (!result.Contains("Compiling sources") || !result.Contains("warning: final line without newline"))
            throw new Exception("Streaming lost diagnostic output.");
    }
}
