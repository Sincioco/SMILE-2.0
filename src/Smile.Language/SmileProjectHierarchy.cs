using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Smile.Language;

public enum SmileProjectHierarchyItemKind
{
    Source,
    Folder,
    Asset,
    References,
    Reference
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
        Exists = kind == SmileProjectHierarchyItemKind.References || kind == SmileProjectHierarchyItemKind.Folder
            ? Directory.Exists(FullPath)
            : File.Exists(FullPath);
        if (kind == SmileProjectHierarchyItemKind.References)
            Exists = true;
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
        SmileProjectSourceSet sourceSet, string projectKind)
    {
        if (sourceSet == null)
            throw new ArgumentNullException(nameof(sourceSet));

        var result = new List<SmileProjectHierarchyItem>();
        var sourcePaths = new HashSet<string>(
            sourceSet.Items.Select(source => source.FullPath), StringComparer.OrdinalIgnoreCase);

        var referencesPath = sourceSet.ProjectPath + ".references";
        result.Add(new SmileProjectHierarchyItem("References", referencesPath, null,
            SmileProjectHierarchyItemKind.References));
        foreach (var reference in sourceSet.References)
        {
            var caption = reference.DisplayName;
            if (reference.Exists)
            {
                try
                {
                    if (reference.Kind == SmileProjectReferenceKind.Project)
                    {
                        var referenced = SmileProjectSourceSet.Load(reference.FullPath);
                        caption = referenced.LibraryName + " (" + referenced.Version + ")";
                    }
                    else
                    {
                        var identity = SmileLibraryPackage.ReadIdentity(reference.FullPath);
                        caption = identity.Name + " (" + identity.Version + ")";
                    }
                }
                catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException)
                {
                    caption += " (invalid)";
                }
            }
            else
            {
                caption += " (missing)";
            }
            result.Add(new SmileProjectHierarchyItem(caption, reference.FullPath, referencesPath,
                SmileProjectHierarchyItemKind.Reference));
        }

        foreach (var source in sourceSet.Items.Select((item, index) => new { Item = item, Index = index })
                     .OrderBy(source => source.Item.IsStartup ? 0 : source.Item.StartupOnly ? 1 : 2)
                     .ThenBy(source => source.Index)
                     .Select(source => source.Item))
        {
            result.Add(new SmileProjectHierarchyItem(Path.GetFileName(source.Include), source.FullPath, null,
                SmileProjectHierarchyItemKind.Source));
        }

        var folders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (projectKind.Equals("Game", StringComparison.OrdinalIgnoreCase))
            AddFolderAndParents(folders, "Assets");
        foreach (var asset in sourceSet.AssetManifest.Items)
        {
            var separator = asset.LogicalPath.LastIndexOf('/');
            if (separator >= 0)
                AddFolderAndParents(folders, asset.LogicalPath.Substring(0, separator));
        }
        AddAssetLevel(result, sourceSet, sourcePaths, folders, parentLogicalPath: null, parentFullPath: null);

        return result;
    }

    private static void AddFolderAndParents(Dictionary<string, string> folders, string logicalPath)
    {
        if (string.IsNullOrWhiteSpace(logicalPath))
            return;
        var segments = logicalPath.Split('/');
        var current = string.Empty;
        foreach (var segment in segments)
        {
            current = current.Length == 0 ? segment : current + "/" + segment;
            if (!folders.ContainsKey(current))
                folders.Add(current, current);
        }
    }

    private static void AddAssetLevel(List<SmileProjectHierarchyItem> result, SmileProjectSourceSet sourceSet,
        HashSet<string> sourcePaths, Dictionary<string, string> folders, string? parentLogicalPath,
        string? parentFullPath)
    {
        var parentPrefix = string.IsNullOrEmpty(parentLogicalPath) ? string.Empty : parentLogicalPath + "/";
        var childFolders = folders.Values.Where(folder =>
        {
            if (!folder.StartsWith(parentPrefix, StringComparison.OrdinalIgnoreCase))
                return false;
            return folder.IndexOf('/', parentPrefix.Length) < 0;
        }).OrderBy(folder => folder, StringComparer.Ordinal).ToArray();
        foreach (var child in childFolders)
        {
            var caption = child.Substring(parentPrefix.Length);
            var fullPath = Path.Combine(sourceSet.ProjectDirectory, child.Replace('/', Path.DirectorySeparatorChar));
            result.Add(new SmileProjectHierarchyItem(caption, fullPath, parentFullPath,
                SmileProjectHierarchyItemKind.Folder));
            AddAssetLevel(result, sourceSet, sourcePaths, folders, child, fullPath);
        }

        foreach (var asset in sourceSet.AssetManifest.Items.Where(item =>
                 string.Equals(ParentLogicalPath(item.LogicalPath), parentLogicalPath ?? string.Empty,
                     StringComparison.OrdinalIgnoreCase)).OrderBy(item => item.LogicalPath, StringComparer.Ordinal))
        {
            if (!sourcePaths.Contains(asset.FullPath))
                result.Add(new SmileProjectHierarchyItem(Path.GetFileName(asset.FullPath), asset.FullPath,
                    parentFullPath, SmileProjectHierarchyItemKind.Asset));
        }
    }

    private static string ParentLogicalPath(string logicalPath)
    {
        var separator = logicalPath.LastIndexOf('/');
        return separator < 0 ? string.Empty : logicalPath.Substring(0, separator);
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
