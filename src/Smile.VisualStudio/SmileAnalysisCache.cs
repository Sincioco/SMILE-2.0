using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Smile.Language;

namespace Smile.VisualStudio;

internal sealed class SmileAnalysisCache : IDisposable
{
    private readonly ITextBuffer _buffer;
    private readonly string _filePath;
    private readonly string? _projectPath;
    private readonly ITextDocumentFactoryService _textDocumentFactory;
    private readonly IDisposable _workspaceRegistration;
    private readonly object _gate = new();
    private CancellationTokenSource? _pendingAnalysis;
    private ITextSnapshot _snapshot;
    private SmileAnalysisResult _analysis;

    private bool _disposed;

    public SmileAnalysisCache(ITextBuffer buffer, string filePath, ITextDocumentFactoryService textDocumentFactory)
    {
        _buffer = buffer;
        _filePath = filePath;
        ThreadHelper.ThrowIfNotOnUIThread();
        _projectPath = FindActiveProjectPath(filePath);
        _textDocumentFactory = textDocumentFactory;
        _snapshot = buffer.CurrentSnapshot;
        _workspaceRegistration = SmileProjectWorkspace.RegisterBuffer(filePath, _snapshot.GetText(), Invalidate);
        _analysis = SmileProjectWorkspace.Analyze(filePath, _snapshot.GetText(), _projectPath);
        _buffer.Changed += BufferChanged;
        _textDocumentFactory.TextDocumentDisposed += TextDocumentDisposed;
    }

    public event EventHandler? AnalysisChanged;
    public string FilePath => _filePath;
    public string? ProjectPath => _projectPath;

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
        SmileProjectWorkspace.UpdateBuffer(_filePath, e.After.GetText());
    }

    private void Invalidate()
    {
        if (_disposed)
            return;
        var snapshot = _buffer.CurrentSnapshot;
        var cancellation = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _pendingAnalysis, cancellation);
        previous?.Cancel();
        previous?.Dispose();
        _ = AnalyzeAfterDelayAsync(snapshot, cancellation.Token);
    }

    private async Task AnalyzeAfterDelayAsync(ITextSnapshot snapshot, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            var text = snapshot.GetText();
            var analysis = await Task.Run(() => SmileProjectWorkspace.Analyze(_filePath, text, _projectPath), cancellationToken).ConfigureAwait(false);
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

    private void TextDocumentDisposed(object sender, TextDocumentEventArgs e)
    {
        if (ReferenceEquals(e.TextDocument.TextBuffer, _buffer))
            Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _buffer.Changed -= BufferChanged;
        _textDocumentFactory.TextDocumentDisposed -= TextDocumentDisposed;
        var pending = Interlocked.Exchange(ref _pendingAnalysis, null);
        pending?.Cancel();
        pending?.Dispose();
        _workspaceRegistration.Dispose();
        AnalysisChanged = null;
    }

    private static string? FindActiveProjectPath(string filePath)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (string.IsNullOrWhiteSpace(filePath))
            return null;
        var runningDocuments = Package.GetGlobalService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
        if (runningDocuments == null)
            return null;

        IntPtr documentData = IntPtr.Zero;
        try
        {
            var result = runningDocuments.FindAndLockDocument((uint)_VSRDTFLAGS.RDT_NoLock, Path.GetFullPath(filePath),
                out var hierarchy, out _, out documentData, out _);
            if (ErrorHandler.Failed(result) || hierarchy == null ||
                ErrorHandler.Failed(hierarchy.GetCanonicalName(VSConstants.VSITEMID_ROOT, out var projectPath)) ||
                !(string.Equals(Path.GetExtension(projectPath), ".smileproj", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(Path.GetExtension(projectPath), ".smilelibproj", StringComparison.OrdinalIgnoreCase)))
                return null;
            return Path.GetFullPath(projectPath);
        }
        finally
        {
            if (documentData != IntPtr.Zero)
                Marshal.Release(documentData);
        }
    }
}
