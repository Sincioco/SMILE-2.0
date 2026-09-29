using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Smile.Language;

// Native helper startup is project metadata, not a second source-language process API.
internal static class NativeWorkerProject
{
    public static string? Read(IEnumerable<XElement> groups, SmileProjectKind kind,
        string directory, string project)
    {
        var entries = groups.SelectMany(group => group.Elements()
            .Where(element => element.Name.LocalName == "NativeWorkerScript")).ToArray();
        if (entries.Length == 0) return null;
        var entry = entries[entries.Length - 1];
        var location = (IXmlLineInfo)entry;
        var value = entry.Value.Trim();
        if (entries.Length != 1 || kind != SmileProjectKind.Game || entry.HasElements ||
            value.Length is < 1 or > 512 || value.Any(char.IsControl) || Path.IsPathRooted(value) ||
            value.IndexOfAny(new[] { ':', '*', '?', '<', '>', '"', '|' }) >= 0 ||
            !string.Equals(Path.GetExtension(value), ".ps1", StringComparison.OrdinalIgnoreCase))
            throw new SmileProjectDiagnosticException("SML3813",
                "NativeWorkerScript must name one project-relative PowerShell script in a Game project.",
                project, location.HasLineInfo() ? location.LineNumber : 1,
                location.HasLineInfo() ? location.LinePosition : 1);
        var path = Path.GetFullPath(Path.Combine(directory, value));
        if (!File.Exists(path))
            throw new SmileProjectDiagnosticException("SML3813", "NativeWorkerScript was not found: " + value,
                project, location.HasLineInfo() ? location.LineNumber : 1,
                location.HasLineInfo() ? location.LinePosition : 1);
        return path;
    }
}
