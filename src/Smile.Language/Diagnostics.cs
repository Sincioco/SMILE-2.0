using System.Collections.Generic;

namespace Smile.Language;

public enum DiagnosticSeverity
{
    Warning,
    Error
}

public sealed class Diagnostic
{
    internal Diagnostic(string code, DiagnosticSeverity severity, string message, SourceText source, TextSpan span)
    {
        Code = code;
        Severity = severity;
        Message = message;
        FilePath = source.FilePath;
        Span = span;
        source.GetLineColumn(span.Start, out var line, out var column);
        Line = line;
        Column = column;
    }

    public string Code { get; }
    public DiagnosticSeverity Severity { get; }
    public string Message { get; }
    public string FilePath { get; }
    public TextSpan Span { get; }
    public int Line { get; }
    public int Column { get; }
}

internal sealed class DiagnosticBag
{
    private readonly SourceText _source;
    private readonly List<Diagnostic> _diagnostics = new();

    public DiagnosticBag(SourceText source) => _source = source;

    public void Report(string code, TextSpan span, string message) =>
        _diagnostics.Add(new Diagnostic(code, DiagnosticSeverity.Error, message, _source, span));

    public void AddRange(IEnumerable<Diagnostic> diagnostics) => _diagnostics.AddRange(diagnostics);

    public IReadOnlyList<Diagnostic> ToArray() => _diagnostics.ToArray();
}
