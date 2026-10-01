using System.Diagnostics;

namespace Smile.Compiler;

// Console progress belongs to the compiler so CLI and IDE builds report the same work.
internal sealed class CompilerProgress : IDisposable
{
    private readonly object _gate = new();
    private readonly TextWriter _output;
    private readonly Stopwatch _elapsed = Stopwatch.StartNew();
    private readonly Timer _timer;
    private string _phase = "Starting build";
    private bool _disposed;

    internal CompilerProgress(TextWriter? output = null, TimeSpan? interval = null)
    {
        _output = output ?? Console.Out;
        var period = interval ?? TimeSpan.FromSeconds(15);
        _timer = new Timer(_ => WriteHeartbeat(), null, period, period);
    }

    internal void Report(string phase)
    {
        lock (_gate)
        {
            if (_disposed) return;
            _phase = phase;
            Write(phase);
        }
    }

    private void WriteHeartbeat()
    {
        lock (_gate)
        {
            if (!_disposed) Write($"Still working: {_phase}");
        }
    }

    private void Write(string message)
    {
        _output.WriteLine($"SMILE [{_elapsed.Elapsed.TotalSeconds:0}s]: {message}");
        _output.Flush();
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            _timer.Dispose();
        }
    }
}
