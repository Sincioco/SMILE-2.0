using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Smile.Language;

namespace Smile.VisualStudio;

internal static class SmileProjectWorkspace
{
    private static readonly object Gate = new();
    private static readonly SmileProjectOwnershipIndex Ownership = new();
    private static readonly SmileOpenBufferRegistry OpenBuffers = new();

    public static void Register(SmileProjectSourceSet sourceSet)
    {
        string[] affected;
        lock (Gate)
            affected = Ownership.Register(sourceSet).ToArray();
        Invalidate(affected);
    }

    public static void Unregister(string projectPath)
    {
        string[] affected;
        lock (Gate)
            affected = Ownership.Unregister(Path.GetFullPath(projectPath)).ToArray();
        Invalidate(affected);
    }

    public static bool Contains(string projectPath, string sourcePath)
    {
        var normalizedProject = Path.GetFullPath(projectPath);
        var normalizedSource = Path.GetFullPath(sourcePath);
        lock (Gate)
            return Ownership.Contains(normalizedProject, normalizedSource);
    }

    public static IReadOnlyList<string> GetProjectPaths(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            return Array.Empty<string>();
        var normalizedPath = Path.GetFullPath(sourcePath);
        lock (Gate)
            return Ownership.GetOwners(normalizedPath).Select(owner => owner.ProjectPath).ToArray();
    }

    public static SmileAnalysisResult Analyze(string filePath, string currentText, string? projectPath = null)
    {
        var normalizedPath = string.IsNullOrWhiteSpace(filePath) ? string.Empty : Path.GetFullPath(filePath);
        SmileProjectSourceSet? sourceSet = null;
        lock (Gate)
        {
            var owners = Ownership.GetOwners(normalizedPath);
            if (!string.IsNullOrWhiteSpace(projectPath))
                sourceSet = owners.FirstOrDefault(owner => string.Equals(owner.ProjectPath,
                    Path.GetFullPath(projectPath), StringComparison.OrdinalIgnoreCase));
            sourceSet ??= owners.FirstOrDefault();
        }

        if (sourceSet == null)
            return SmileLanguage.Analyze(currentText, filePath);

        IReadOnlyList<SmileProjectSourceItem> compilationSources = sourceSet.GetCompilationSourcesFor(normalizedPath);
        var documents = new List<SmileSourceDocument>(compilationSources.Count);
        foreach (var source in compilationSources)
        {
            string? text;
            text = OpenBuffers.TryGetText(source.FullPath, out var openText) ? openText : null;
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

    public static IDisposable RegisterBuffer(string filePath, string currentText, Action invalidate)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return EmptyRegistration.Instance;
        var normalizedPath = Path.GetFullPath(filePath);
        return OpenBuffers.Register(normalizedPath, currentText, invalidate);
    }

    public static void UpdateBuffer(string filePath, string currentText)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var normalizedPath = Path.GetFullPath(filePath);
        string[] affected;
        lock (Gate)
        {
            OpenBuffers.Update(normalizedPath, currentText);
            var owners = Ownership.GetOwners(normalizedPath);
            affected = owners.Count != 0
                ? owners.SelectMany(owner => owner.Items).Select(source => source.FullPath)
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                : new[] { normalizedPath };
        }
        Invalidate(affected);
    }

    private static void Invalidate(IEnumerable<string> sourcePaths)
    {
        Action[] callbacks;
        callbacks = OpenBuffers.GetInvalidations(sourcePaths).ToArray();
        foreach (var callback in callbacks)
            callback();
    }

    private sealed class EmptyRegistration : IDisposable
    {
        public static readonly EmptyRegistration Instance = new();
        public void Dispose() { }
    }
}
