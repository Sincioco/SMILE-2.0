using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Smile.Language;

namespace Smile.VisualStudio;

internal sealed class SmileAnalysisCache
{
    private readonly ITextBuffer _buffer;
    private readonly string _filePath;
    private readonly object _gate = new();
    private CancellationTokenSource? _pendingAnalysis;
    private ITextSnapshot _snapshot;
    private SmileAnalysisResult _analysis;

    public SmileAnalysisCache(ITextBuffer buffer, string filePath)
    {
        _buffer = buffer;
        _filePath = filePath;
        _snapshot = buffer.CurrentSnapshot;
        _analysis = SmileLanguage.Analyze(_snapshot.GetText(), filePath);
        _buffer.Changed += BufferChanged;
    }

    public event EventHandler? AnalysisChanged;

    public bool TryGet(ITextSnapshot snapshot, out SmileAnalysisResult analysis)
    {
        lock (_gate)
        {
            analysis = _analysis;
            return ReferenceEquals(snapshot, _snapshot);
        }
    }

    private void BufferChanged(object sender, TextContentChangedEventArgs e)
    {
        var cancellation = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _pendingAnalysis, cancellation);
        previous?.Cancel();
        previous?.Dispose();
        _ = AnalyzeAfterDelayAsync(e.After, cancellation.Token);
    }

    private async Task AnalyzeAfterDelayAsync(ITextSnapshot snapshot, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            var text = snapshot.GetText();
            var analysis = await Task.Run(() => SmileLanguage.Analyze(text, _filePath), cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (!ReferenceEquals(snapshot, _buffer.CurrentSnapshot))
                return;

            lock (_gate)
            {
                _snapshot = snapshot;
                _analysis = analysis;
            }

            RaiseAnalysisChanged();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void RaiseAnalysisChanged()
    {
        ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            AnalysisChanged?.Invoke(this, EventArgs.Empty);
        }).FileAndForget("Smile/AnalysisChanged");
    }
}
