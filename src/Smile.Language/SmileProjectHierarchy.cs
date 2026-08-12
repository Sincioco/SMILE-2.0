using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Smile.Language;

public enum SmileProjectHierarchyItemKind
{
    Source,
    Folder,
    Asset
}

public sealed class SmileProjectHierarchyItem
{
    internal SmileProjectHierarchyItem(string caption, string fullPath, string? parentPath,
        SmileProjectHierarchyItemKind kind)
    {
        Caption = caption;
        FullPath = Path.GetFullPath(fullPath);
        ParentPath = parentPath == null ? null : Path.GetFullPath(parentPath);
        Kind = kind;
        Key = kind + "|" + FullPath;
        Exists = kind == SmileProjectHierarchyItemKind.Folder
            ? Directory.Exists(FullPath)
            : File.Exists(FullPath);
    }

    public string Caption { get; }
    public string FullPath { get; }
    public string? ParentPath { get; }
    public SmileProjectHierarchyItemKind Kind { get; }
    public string Key { get; }
    public bool Exists { get; }
}

public static class SmileProjectHierarchyProjection
{
    public static IReadOnlyList<SmileProjectHierarchyItem> Create(
        SmileProjectSourceSet sourceSet, string projectKind, IReadOnlyList<string> assetIncludes)
    {
        if (sourceSet == null)
            throw new ArgumentNullException(nameof(sourceSet));

        var result = new List<SmileProjectHierarchyItem>();
        var sourcePaths = new HashSet<string>(
            sourceSet.Items.Select(source => source.FullPath), StringComparer.OrdinalIgnoreCase);

        foreach (var source in sourceSet.Items.Select((item, index) => new { Item = item, Index = index })
                     .OrderBy(source => source.Item.IsStartup ? 0 : source.Item.StartupOnly ? 1 : 2)
                     .ThenBy(source => source.Index)
                     .Select(source => source.Item))
        {
            result.Add(new SmileProjectHierarchyItem(Path.GetFileName(source.Include), source.FullPath, null,
                SmileProjectHierarchyItemKind.Source));
        }

        if (projectKind.Equals("Game", StringComparison.OrdinalIgnoreCase) || assetIncludes.Count != 0)
        {
            var assetsPath = Path.Combine(sourceSet.ProjectDirectory, "Assets");
            result.Add(new SmileProjectHierarchyItem("Assets", assetsPath, null,
                SmileProjectHierarchyItemKind.Folder));
            AddAssetChildren(result, assetsPath, sourcePaths);
        }

        return result;
    }

    private static void AddAssetChildren(List<SmileProjectHierarchyItem> result, string directory,
        HashSet<string> sourcePaths)
    {
        if (!Directory.Exists(directory))
            return;

        foreach (var childDirectory in Directory.EnumerateDirectories(directory)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            result.Add(new SmileProjectHierarchyItem(Path.GetFileName(childDirectory), childDirectory, directory,
                SmileProjectHierarchyItemKind.Folder));
            AddAssetChildren(result, childDirectory, sourcePaths);
        }

        foreach (var file in Directory.EnumerateFiles(directory)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            if (!sourcePaths.Contains(Path.GetFullPath(file)))
                result.Add(new SmileProjectHierarchyItem(Path.GetFileName(file), file, directory,
                    SmileProjectHierarchyItemKind.Asset));
        }
    }
}

public sealed class SmileProjectHierarchyIdentityMap
{
    private readonly Dictionary<string, uint> _ids = new(StringComparer.OrdinalIgnoreCase);
    private uint _nextId = 1;

    public IReadOnlyDictionary<string, uint> Apply(IReadOnlyList<SmileProjectHierarchyItem> projection)
    {
        var next = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in projection)
        {
            if (!_ids.TryGetValue(item.Key, out var id))
            {
                id = NextId();
                _ids.Add(item.Key, id);
            }
            if (next.ContainsKey(item.Key))
                throw new InvalidDataException($"Duplicate SMILE hierarchy item '{item.FullPath}'.");
            next.Add(item.Key, id);
        }
        return next;
    }

    private uint NextId()
    {
        // The top three uint values are reserved by the Visual Studio hierarchy contract.
        if (_nextId >= 0xfffffffd)
            throw new InvalidOperationException("The SMILE project hierarchy exhausted its available item IDs.");
        return _nextId++;
    }
}
