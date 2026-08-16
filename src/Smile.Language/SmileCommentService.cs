using System;
using System.Collections.Generic;

namespace Smile.Language;

internal enum SmileCommentMode
{
    Comment,
    Uncomment,
    Toggle
}

internal readonly struct SmileCommentEdit
{
    public SmileCommentEdit(int position, int deleteLength, string insertText)
    {
        Position = position;
        DeleteLength = deleteLength;
        InsertText = insertText;
    }

    public int Position { get; }
    public int DeleteLength { get; }
    public string InsertText { get; }
}

internal static class SmileCommentService
{
    public static IReadOnlyList<SmileCommentEdit> GetEdits(string text, int selectionStart,
        int selectionLength, SmileCommentMode mode)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));
        if (selectionStart < 0 || selectionStart > text.Length)
            throw new ArgumentOutOfRangeException(nameof(selectionStart));
        if (selectionLength < 0 || selectionStart + selectionLength > text.Length)
            throw new ArgumentOutOfRangeException(nameof(selectionLength));

        var firstLineStart = FindLineStart(text, selectionStart);
        var selectionEnd = selectionStart + selectionLength;
        var lastPosition = selectionLength > 0 && selectionEnd > 0 && IsLineStart(text, selectionEnd)
            ? selectionEnd - 1
            : selectionEnd;
        var lastLineStart = FindLineStart(text, lastPosition);
        var lineStarts = GetLineStarts(text, firstLineStart, lastLineStart);
        var uncomment = mode == SmileCommentMode.Uncomment ||
            mode == SmileCommentMode.Toggle && AllNonBlankLinesAreComments(text, lineStarts);
        var edits = new List<SmileCommentEdit>();

        foreach (var lineStart in lineStarts)
        {
            var contentStart = FindContentStart(text, lineStart);
            if (contentStart >= text.Length || text[contentStart] == '\r' || text[contentStart] == '\n')
                continue;

            if (uncomment)
            {
                if (text[contentStart] == '\'')
                    edits.Add(new SmileCommentEdit(contentStart, 1, string.Empty));
            }
            else
            {
                edits.Add(new SmileCommentEdit(contentStart, 0, "'"));
            }
        }

        return edits;
    }

    private static List<int> GetLineStarts(string text, int firstLineStart, int lastLineStart)
    {
        var starts = new List<int>();
        var lineStart = firstLineStart;
        while (lineStart <= lastLineStart)
        {
            starts.Add(lineStart);
            var lineEnd = FindLineEnd(text, lineStart);
            if (lineEnd >= text.Length)
                break;
            lineStart = lineEnd + 1;
            if (text[lineEnd] == '\r' && lineStart < text.Length && text[lineStart] == '\n')
                lineStart++;
        }

        return starts;
    }

    private static bool AllNonBlankLinesAreComments(string text, IReadOnlyList<int> lineStarts)
    {
        var foundContent = false;
        foreach (var lineStart in lineStarts)
        {
            var contentStart = FindContentStart(text, lineStart);
            if (contentStart >= text.Length || text[contentStart] == '\r' || text[contentStart] == '\n')
                continue;

            foundContent = true;
            if (text[contentStart] != '\'')
                return false;
        }

        return foundContent;
    }

    private static int FindLineStart(string text, int position)
    {
        position = Math.Min(position, text.Length);
        while (position > 0 && text[position - 1] != '\r' && text[position - 1] != '\n')
            position--;
        return position;
    }

    private static int FindLineEnd(string text, int lineStart)
    {
        var position = lineStart;
        while (position < text.Length && text[position] != '\r' && text[position] != '\n')
            position++;
        return position;
    }

    private static int FindContentStart(string text, int lineStart)
    {
        var position = lineStart;
        while (position < text.Length && (text[position] == ' ' || text[position] == '\t'))
            position++;
        return position;
    }

    private static bool IsLineStart(string text, int position) =>
        position == 0 || position <= text.Length && (text[position - 1] == '\r' || text[position - 1] == '\n');
}
