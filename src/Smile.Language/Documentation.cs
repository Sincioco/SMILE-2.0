using System;
using System.Collections.Generic;
using System.Linq;

namespace Smile.Language;

public sealed class SmileDocumentation
{
    private static readonly IReadOnlyDictionary<string, string> NoParameters =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    internal SmileDocumentation(string summary, IReadOnlyDictionary<string, string>? parameters,
        string returns, string remarks)
    {
        Summary = summary ?? string.Empty;
        Parameters = parameters ?? NoParameters;
        Returns = returns ?? string.Empty;
        Remarks = remarks ?? string.Empty;
    }

    public static SmileDocumentation Empty { get; } =
        new(string.Empty, NoParameters, string.Empty, string.Empty);

    public string Summary { get; }
    public IReadOnlyDictionary<string, string> Parameters { get; }
    public string Returns { get; }
    public string Remarks { get; }
}

public static class SmileDocumentationService
{
    public static SmileDocumentation GetDocumentation(SourceText source, int declarationPosition)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var text = source.Text;
        var position = Math.Max(0, Math.Min(declarationPosition, text.Length));
        var declarationLineStart = position;
        while (declarationLineStart > 0 && text[declarationLineStart - 1] is not ('\r' or '\n'))
            declarationLineStart--;

        var lines = new List<string>();
        var cursor = declarationLineStart;
        while (cursor > 0)
        {
            var lineEnd = cursor;
            if (lineEnd > 0 && text[lineEnd - 1] == '\n') lineEnd--;
            if (lineEnd > 0 && text[lineEnd - 1] == '\r') lineEnd--;
            var lineStart = lineEnd;
            while (lineStart > 0 && text[lineStart - 1] is not ('\r' or '\n'))
                lineStart--;

            var line = text.Substring(lineStart, lineEnd - lineStart).TrimStart(' ', '\t');
            if (!line.StartsWith("'''", StringComparison.Ordinal))
                break;
            line = line.Substring(3);
            if (line.StartsWith(" ", StringComparison.Ordinal))
                line = line.Substring(1);
            lines.Add(line);
            cursor = lineStart;
        }

        if (lines.Count == 0)
            return SmileDocumentation.Empty;

        lines.Reverse();
        var summary = new List<string>();
        var returns = new List<string>();
        var remarks = new List<string>();
        var parameters = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        List<string>? current = summary;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (TryReadParameter(line, out var parameterName, out var parameterText))
            {
                if (!parameters.ContainsKey(parameterName))
                {
                    current = new List<string>();
                    parameters.Add(parameterName, current);
                    Add(current, parameterText);
                }
                else
                {
                    // The first entry wins deterministically. Ignore a repeated entry and its continuations.
                    current = null;
                }
                continue;
            }
            if (TryReadTag(line, "@returns:", out var returnsText))
            {
                current = returns;
                Add(current, returnsText);
                continue;
            }
            if (TryReadTag(line, "@remarks:", out var remarksText))
            {
                current = remarks;
                Add(current, remarksText);
                continue;
            }
            if (line.StartsWith("@", StringComparison.Ordinal))
            {
                current = null;
                continue;
            }
            if (current != null)
                Add(current, line);
        }

        var parameterTextByName = parameters.ToDictionary(item => item.Key, item => Join(item.Value),
            StringComparer.OrdinalIgnoreCase);
        return new SmileDocumentation(Join(summary), parameterTextByName, Join(returns), Join(remarks));
    }

    private static bool TryReadParameter(string line, out string name, out string text)
    {
        name = string.Empty;
        text = string.Empty;
        const string tag = "@param";
        if (!line.StartsWith(tag, StringComparison.OrdinalIgnoreCase) || line.Length == tag.Length ||
            !char.IsWhiteSpace(line[tag.Length]))
            return false;
        var colon = line.IndexOf(':', tag.Length + 1);
        if (colon < 0)
            return false;
        name = line.Substring(tag.Length, colon - tag.Length).Trim();
        if (name.Length == 0)
            return false;
        text = line.Substring(colon + 1).Trim();
        return true;
    }

    private static bool TryReadTag(string line, string tag, out string text)
    {
        text = string.Empty;
        if (!line.StartsWith(tag, StringComparison.OrdinalIgnoreCase))
            return false;
        text = line.Substring(tag.Length).Trim();
        return true;
    }

    private static void Add(ICollection<string> parts, string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
            parts.Add(text.Trim());
    }

    private static string Join(IEnumerable<string> parts) => string.Join(" ", parts);
}
