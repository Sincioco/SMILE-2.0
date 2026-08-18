using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Smile.Language;

internal static class SmileCompilerDiagnosticParser
{
    private static readonly Regex DiagnosticPattern = new(
        @"^(?<file>.+)\((?<line>\d+),(?<column>\d+)\): (?<severity>error|warning) " +
        @"(?<code>SML\d+): (?<message>.*)$",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);

    public static IReadOnlyList<SmileProjectDiagnostic> Parse(string? output)
    {
        var diagnostics = new List<SmileProjectDiagnostic>();
        var normalizedOutput = output ?? string.Empty;
        if (normalizedOutput.Length == 0)
            return diagnostics;

        foreach (Match match in DiagnosticPattern.Matches(normalizedOutput.Replace("\r\n", "\n")))
        {
            if (!int.TryParse(match.Groups["line"].Value, NumberStyles.None, CultureInfo.InvariantCulture,
                    out var line) ||
                !int.TryParse(match.Groups["column"].Value, NumberStyles.None, CultureInfo.InvariantCulture,
                    out var column))
                continue;

            diagnostics.Add(new SmileProjectDiagnostic(
                match.Groups["code"].Value,
                match.Groups["message"].Value,
                match.Groups["file"].Value,
                line,
                column,
                match.Groups["severity"].Value == "warning"
                    ? DiagnosticSeverity.Warning
                    : DiagnosticSeverity.Error));
        }

        return diagnostics;
    }
}
