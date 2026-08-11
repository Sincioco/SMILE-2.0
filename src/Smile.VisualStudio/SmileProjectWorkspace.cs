using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Smile.Language;

namespace Smile.VisualStudio;

internal static class SmileProjectWorkspace
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, SmileProjectSourceSet> Projects =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, SmileProjectSourceSet> Sources =
        new(StringComparer.OrdinalIgnoreCase);

    public static void Register(SmileProjectSourceSet sourceSet)
    {
        lock (Gate)
        {
            UnregisterCore(sourceSet.ProjectPath);
            Projects[sourceSet.ProjectPath] = sourceSet;
            foreach (var source in sourceSet.CompilationSources)
                Sources[source.FullPath] = sourceSet;
        }
    }

    public static void Unregister(string projectPath)
    {
        lock (Gate)
            UnregisterCore(Path.GetFullPath(projectPath));
    }

    public static SmileAnalysisResult Analyze(string filePath, string currentText)
    {
        var normalizedPath = string.IsNullOrWhiteSpace(filePath) ? string.Empty : Path.GetFullPath(filePath);
        SmileProjectSourceSet? sourceSet;
        lock (Gate)
            Sources.TryGetValue(normalizedPath, out sourceSet);

        if (sourceSet == null)
            return SmileLanguage.Analyze(currentText, filePath);

        try
        {
            var documents = sourceSet.CompilationSources.Select(source => new SmileSourceDocument(
                string.Equals(source.FullPath, normalizedPath, StringComparison.OrdinalIgnoreCase)
                    ? currentText
                    : File.ReadAllText(source.FullPath),
                source.FullPath,
                source.IsStartup)).ToArray();
            return SmileLanguage.Analyze(documents);
        }
        catch (IOException)
        {
            return SmileLanguage.Analyze(currentText, filePath);
        }
        catch (UnauthorizedAccessException)
        {
            return SmileLanguage.Analyze(currentText, filePath);
        }
    }

    private static void UnregisterCore(string projectPath)
    {
        if (!Projects.TryGetValue(projectPath, out var existing))
            return;
        Projects.Remove(projectPath);
        foreach (var source in existing.CompilationSources)
        {
            if (Sources.TryGetValue(source.FullPath, out var owner) && ReferenceEquals(owner, existing))
                Sources.Remove(source.FullPath);
        }
    }
}
