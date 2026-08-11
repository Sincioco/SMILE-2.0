using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Smile.Language;

public sealed class SmileProjectOwnershipIndex
{
    private readonly Dictionary<string, SmileProjectSourceSet> _projects =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, SmileProjectSourceSet>> _sources =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> Register(SmileProjectSourceSet sourceSet)
    {
        var affected = Unregister(sourceSet.ProjectPath).ToList();
        _projects[sourceSet.ProjectPath] = sourceSet;
        foreach (var source in sourceSet.Items)
        {
            if (!_sources.TryGetValue(source.FullPath, out var owners))
                _sources[source.FullPath] = owners = new Dictionary<string, SmileProjectSourceSet>(StringComparer.OrdinalIgnoreCase);
            owners[sourceSet.ProjectPath] = sourceSet;
            affected.Add(source.FullPath);
        }
        return affected.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public IReadOnlyList<string> Unregister(string projectPath)
    {
        var normalizedProjectPath = Path.GetFullPath(projectPath);
        if (!_projects.TryGetValue(normalizedProjectPath, out var existing))
            return Array.Empty<string>();
        _projects.Remove(normalizedProjectPath);
        foreach (var source in existing.Items)
        {
            if (!_sources.TryGetValue(source.FullPath, out var owners))
                continue;
            owners.Remove(normalizedProjectPath);
            if (owners.Count == 0)
                _sources.Remove(source.FullPath);
        }
        return existing.Items.Select(source => source.FullPath).ToArray();
    }

    public bool Contains(string projectPath, string sourcePath)
    {
        var normalizedProject = Path.GetFullPath(projectPath);
        var normalizedSource = Path.GetFullPath(sourcePath);
        return _sources.TryGetValue(normalizedSource, out var owners) && owners.ContainsKey(normalizedProject);
    }

    public IReadOnlyList<SmileProjectSourceSet> GetOwners(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            return Array.Empty<SmileProjectSourceSet>();
        var normalizedSource = Path.GetFullPath(sourcePath);
        return _sources.TryGetValue(normalizedSource, out var owners)
            ? owners.Values.OrderBy(owner => owner.ProjectPath, StringComparer.OrdinalIgnoreCase).ToArray()
            : Array.Empty<SmileProjectSourceSet>();
    }
}
