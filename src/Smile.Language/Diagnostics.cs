using System;
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
        Location = new SourceLocation(source, span);
    }

    public string Code { get; }
    public DiagnosticSeverity Severity { get; }
    public string Message { get; }
    public SourceLocation Location { get; }
    public SourceText Source => Location.Source;
    public string FilePath => Location.FilePath;
    public TextSpan Span => Location.Span;
    public int Line => Location.Line;
    public int Column => Location.Column;
}

internal sealed class DiagnosticBag
{
    private readonly SourceText? _source;
    private readonly List<Diagnostic> _diagnostics = new();

    public DiagnosticBag()
    {
    }

    public DiagnosticBag(SourceText source) => _source = source;

    public void Report(string code, TextSpan span, string message) =>
        _diagnostics.Add(new Diagnostic(code, DiagnosticSeverity.Error, message,
            _source ?? throw new InvalidOperationException("A source is required for this diagnostic."), span));

    public void Report(SourceText source, string code, TextSpan span, string message) =>
        _diagnostics.Add(new Diagnostic(code, DiagnosticSeverity.Error, message, source, span));

    public void AddRange(IEnumerable<Diagnostic> diagnostics) => _diagnostics.AddRange(diagnostics);

    public IReadOnlyList<Diagnostic> ToArray() => _diagnostics.ToArray();
}
