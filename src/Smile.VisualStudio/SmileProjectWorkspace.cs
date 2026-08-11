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
    private static readonly Dictionary<string, string> OpenBuffers =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, List<Action>> BufferInvalidations =
        new(StringComparer.OrdinalIgnoreCase);

    public static void Register(SmileProjectSourceSet sourceSet)
    {
        lock (Gate)
        {
            UnregisterCore(sourceSet.ProjectPath);
            Projects[sourceSet.ProjectPath] = sourceSet;
            foreach (var source in sourceSet.Items)
                Sources[source.FullPath] = sourceSet;
        }
        Invalidate(sourceSet.Items.Select(source => source.FullPath));
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

        IReadOnlyList<SmileProjectSourceItem> compilationSources = sourceSet.GetCompilationSourcesFor(normalizedPath);
        var documents = new List<SmileSourceDocument>(compilationSources.Count);
        foreach (var source in compilationSources)
        {
            string? text;
            lock (Gate)
                OpenBuffers.TryGetValue(source.FullPath, out text);
            if (string.Equals(source.FullPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
                text = currentText;

            var missing = false;
            if (text == null)
            {
                try
                {
                    text = File.ReadAllText(source.FullPath);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    text = string.Empty;
                    missing = true;
                }
            }
            documents.Add(new SmileSourceDocument(text, source.FullPath,
                string.Equals(source.FullPath, compilationSources[0].FullPath, StringComparison.OrdinalIgnoreCase), missing));
        }
        return SmileLanguage.Analyze(documents);
    }

    public static void RegisterBuffer(string filePath, string currentText, Action invalidate)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        lock (Gate)
        {
            OpenBuffers[normalizedPath] = currentText;
            if (!BufferInvalidations.TryGetValue(normalizedPath, out var callbacks))
                BufferInvalidations[normalizedPath] = callbacks = new List<Action>();
            if (!callbacks.Contains(invalidate))
                callbacks.Add(invalidate);
        }
    }

    public static void UpdateBuffer(string filePath, string currentText)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        string[] affected;
        lock (Gate)
        {
            OpenBuffers[normalizedPath] = currentText;
            affected = Sources.TryGetValue(normalizedPath, out var sourceSet)
                ? sourceSet.Items.Select(source => source.FullPath).ToArray()
                : new[] { normalizedPath };
        }
        Invalidate(affected);
    }

    private static void Invalidate(IEnumerable<string> sourcePaths)
    {
        Action[] callbacks;
        lock (Gate)
            callbacks = sourcePaths.Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(path => BufferInvalidations.ContainsKey(path))
                .SelectMany(path => BufferInvalidations[path]).Distinct().ToArray();
        foreach (var callback in callbacks)
            callback();
    }

    private static void UnregisterCore(string projectPath)
    {
        if (!Projects.TryGetValue(projectPath, out var existing))
            return;
        Projects.Remove(projectPath);
        foreach (var source in existing.Items)
        {
            if (Sources.TryGetValue(source.FullPath, out var owner) && ReferenceEquals(owner, existing))
                Sources.Remove(source.FullPath);
        }
    }
}
