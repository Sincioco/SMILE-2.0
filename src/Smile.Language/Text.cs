using System;
using System.Collections.Generic;

namespace Smile.Language;

public readonly struct TextSpan
{
    public TextSpan(int start, int length)
    {
        Start = start;
        Length = length;
    }

    public int Start { get; }
    public int Length { get; }
    public int End => Start + Length;

    public static TextSpan FromBounds(int start, int end) => new(start, Math.Max(0, end - start));
}

public sealed class SourceLocation
{
    public SourceLocation(SourceText source, TextSpan span)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Span = span;
        source.GetLineColumn(span.Start, out var line, out var column);
        Line = line;
        Column = column;
    }

    public SourceText Source { get; }
    public TextSpan Span { get; }
    public string FilePath => Source.FilePath;
    public int Line { get; }
    public int Column { get; }
}

public sealed class SourceText
{
    private readonly int[] _lineStarts;

    public SourceText(string text, string? filePath = null)
    {
        Text = text ?? string.Empty;
        FilePath = filePath ?? string.Empty;

        var starts = new List<int> { 0 };
        for (var i = 0; i < Text.Length; i++)
        {
            if (Text[i] == '\r' && i + 1 < Text.Length && Text[i + 1] == '\n')
            {
                starts.Add(i + 2);
                i++;
            }
            else if (Text[i] == '\r' || Text[i] == '\n')
            {
                starts.Add(i + 1);
            }
        }

        _lineStarts = starts.ToArray();
    }

    public string Text { get; }
    public string FilePath { get; }
    public int Length => Text.Length;
    public char this[int index] => index >= 0 && index < Text.Length ? Text[index] : '\0';

    public string Substring(int start, int length) => Text.Substring(start, length);

    public void GetLineColumn(int position, out int line, out int column)
    {
        position = Math.Max(0, Math.Min(position, Text.Length));
        var index = Array.BinarySearch(_lineStarts, position);
        if (index < 0)
            index = ~index - 1;

        line = index + 1;
        column = position - _lineStarts[index] + 1;
    }
}
