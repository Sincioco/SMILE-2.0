using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Smile.Language;

public sealed class SmileProjectSourceItem
{
    internal SmileProjectSourceItem(string include, string fullPath, bool startupOnly, bool isStartup)
    {
        Include = include;
        FullPath = fullPath;
        StartupOnly = startupOnly;
        IsStartup = isStartup;
    }

    public string Include { get; }
    public string FullPath { get; }
    public bool StartupOnly { get; }
    public bool IsStartup { get; }
    public bool IsSupport => !IsStartup && !StartupOnly;
    public bool Exists => File.Exists(FullPath);
}

public sealed class SmileProjectSourceSet
{
    private SmileProjectSourceSet(string projectPath, string startupFile,
        IReadOnlyList<SmileProjectSourceItem> items, IReadOnlyList<SmileProjectSourceItem> compilationSources)
    {
        ProjectPath = projectPath;
        ProjectDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory;
        StartupFile = startupFile;
        Items = items;
        CompilationSources = compilationSources;
        StartupSource = compilationSources[0];
        SupportSources = compilationSources.Skip(1).ToArray();
    }

    public string ProjectPath { get; }
    public string ProjectDirectory { get; }
    public string StartupFile { get; }
    public SmileProjectSourceItem StartupSource { get; }
    public IReadOnlyList<SmileProjectSourceItem> Items { get; }
    public IReadOnlyList<SmileProjectSourceItem> CompilationSources { get; }
    public IReadOnlyList<SmileProjectSourceItem> SupportSources { get; }

    public IReadOnlyList<SmileProjectSourceItem> GetCompilationSourcesFor(string filePath)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        var active = Items.FirstOrDefault(item =>
            string.Equals(item.FullPath, normalizedPath, StringComparison.OrdinalIgnoreCase));
        if (active == null || !active.StartupOnly || active.IsStartup)
            return CompilationSources;

        return new[] { active }.Concat(Items.Where(item => item.IsSupport)).ToArray();
    }

    public static SmileProjectSourceSet Load(string projectPath)
    {
        var fullPath = Path.GetFullPath(projectPath);
        return Parse(fullPath, File.ReadAllText(fullPath));
    }

    public static SmileProjectSourceSet Parse(string projectPath, string xml)
    {
        var fullProjectPath = Path.GetFullPath(projectPath);
        var root = XDocument.Parse(xml, LoadOptions.SetLineInfo).Root;
        if (root == null || root.Name.LocalName != "SmileProject")
            throw new InvalidDataException("A .smileproj file must have a SmileProject root element.");

        var projectDirectory = Path.GetDirectoryName(fullProjectPath) ?? Environment.CurrentDirectory;
        var properties = root.Elements().FirstOrDefault(element => element.Name.LocalName == "PropertyGroup");
        var startupFile = properties?.Elements().FirstOrDefault(element => element.Name.LocalName == "StartupFile")?.Value.Trim();
        if (string.IsNullOrWhiteSpace(startupFile))
            startupFile = "Program.smile";
        var startupPath = Path.GetFullPath(Path.Combine(projectDirectory, startupFile!));

        var sourceElements = root.Elements().Where(element => element.Name.LocalName == "ItemGroup")
            .SelectMany(element => element.Elements().Where(item => item.Name.LocalName == "SmileSource"))
            .ToArray();
        if (sourceElements.Length == 0)
            sourceElements = new[] { new XElement("SmileSource", new XAttribute("Include", startupFile!)) };

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entries = new List<(string Include, string FullPath, bool StartupOnly)>();
        foreach (var element in sourceElements)
        {
            var include = ((string?)element.Attribute("Include") ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(include))
                throw new InvalidDataException("SmileSource Include must name a .smile source file.");
            if (!string.Equals(Path.GetExtension(include), ".smile", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"SmileSource Include must name a .smile source file: '{include}'.");

            var startupOnly = false;
            var startupOnlyText = (string?)element.Attribute("StartupOnly");
            if (startupOnlyText != null && !bool.TryParse(startupOnlyText.Trim(), out startupOnly))
                throw new InvalidDataException($"Unknown StartupOnly value '{startupOnlyText}'. Expected true or false.");

            var sourcePath = Path.GetFullPath(Path.Combine(projectDirectory, include));
            if (!paths.Add(sourcePath))
                throw new InvalidDataException($"Duplicate SmileSource path '{include}'.");
            entries.Add((include, sourcePath, startupOnly));
        }

        if (!paths.Contains(startupPath))
            throw new InvalidDataException($"StartupFile '{startupFile}' is not listed as a SmileSource.");

        var items = entries.Select(entry => new SmileProjectSourceItem(
            entry.Include, entry.FullPath, entry.StartupOnly,
            string.Equals(entry.FullPath, startupPath, StringComparison.OrdinalIgnoreCase))).ToArray();
        var startup = items.Single(item => item.IsStartup);
        var compilationSources = new[] { startup }.Concat(items.Where(item => item.IsSupport)).ToArray();
        return new SmileProjectSourceSet(fullProjectPath, startupFile!, items, compilationSources);
    }

    public void ValidateFiles()
    {
        foreach (var source in Items)
        {
            if (!File.Exists(source.FullPath))
            {
                var role = source.IsStartup ? "Startup" : "Support";
                throw new FileNotFoundException($"{role} source file was not found: {source.FullPath}", source.FullPath);
            }
        }
    }

    public bool TryGetCompilationSource(string filePath, out SmileProjectSourceItem source)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        source = CompilationSources.FirstOrDefault(item =>
            string.Equals(item.FullPath, normalizedPath, StringComparison.OrdinalIgnoreCase))!;
        return source != null;
    }
}

public static class SmileProjectFileEditor
{
    public static SmileProjectSourceSet AddSource(string projectPath, string sourcePath)
    {
        var context = Load(projectPath);
        var include = RelativeSourceInclude(context.ProjectDirectory, sourcePath);
        var existingSources = SourceElements(context.Root).ToArray();
        if (existingSources.Any(element => SameInclude(context.ProjectDirectory, element, sourcePath)))
            throw new InvalidDataException($"SMILE source '{include}' is already included in the project.");

        var itemGroup = context.Root.Elements().FirstOrDefault(element =>
            element.Name.LocalName == "ItemGroup" && element.Elements().Any(item => item.Name.LocalName == "SmileSource"));
        if (itemGroup == null)
        {
            itemGroup = new XElement(context.Root.Name.Namespace + "ItemGroup");
            context.Root.Add(itemGroup);
        }
        if (existingSources.Length == 0)
            AppendItem(itemGroup, new XElement(context.Root.Name.Namespace + "SmileSource",
                new XAttribute("Include", StartupValue(context.Root))));
        AppendItem(itemGroup, new XElement(context.Root.Name.Namespace + "SmileSource", new XAttribute("Include", include)));
        return Save(context);
    }

    public static SmileProjectSourceSet SetStartup(string projectPath, string sourcePath)
    {
        var context = Load(projectPath);
        var selected = FindSource(context, sourcePath);
        var oldStartup = Path.GetFullPath(Path.Combine(context.ProjectDirectory, StartupValue(context.Root)));
        selected.SetAttributeValue("StartupOnly", "true");
        var oldStartupElement = SourceElements(context.Root).FirstOrDefault(element => SameInclude(context.ProjectDirectory, element, oldStartup));
        oldStartupElement?.SetAttributeValue("StartupOnly", "true");

        var propertyGroup = context.Root.Elements().FirstOrDefault(element => element.Name.LocalName == "PropertyGroup");
        if (propertyGroup == null)
        {
            propertyGroup = new XElement(context.Root.Name.Namespace + "PropertyGroup");
            context.Root.AddFirst(propertyGroup);
        }
        var startup = propertyGroup.Elements().FirstOrDefault(element => element.Name.LocalName == "StartupFile");
        if (startup == null)
        {
            startup = new XElement(context.Root.Name.Namespace + "StartupFile");
            propertyGroup.Add(startup);
        }
        startup.Value = RelativeSourceInclude(context.ProjectDirectory, sourcePath);
        return Save(context);
    }

    public static SmileProjectSourceSet IncludeAsSupport(string projectPath, string sourcePath)
    {
        var context = Load(projectPath);
        var fullSourcePath = Path.GetFullPath(sourcePath);
        var startupPath = Path.GetFullPath(Path.Combine(context.ProjectDirectory, StartupValue(context.Root)));
        if (string.Equals(fullSourcePath, startupPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The selected startup source cannot be included as a support source.");
        FindSource(context, fullSourcePath).SetAttributeValue("StartupOnly", null);
        return Save(context);
    }

    public static SmileProjectSourceSet RemoveSource(string projectPath, string sourcePath)
    {
        var context = Load(projectPath);
        var fullSourcePath = Path.GetFullPath(sourcePath);
        var startupPath = Path.GetFullPath(Path.Combine(context.ProjectDirectory, StartupValue(context.Root)));
        if (string.Equals(fullSourcePath, startupPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The selected startup source cannot be removed from the project.");
        var source = FindSource(context, fullSourcePath);
        if (source.PreviousNode is XText indentation && string.IsNullOrWhiteSpace(indentation.Value))
            indentation.Remove();
        source.Remove();
        return Save(context);
    }

    private static ProjectContext Load(string projectPath)
    {
        var fullProjectPath = Path.GetFullPath(projectPath);
        var document = XDocument.Load(fullProjectPath, LoadOptions.PreserveWhitespace);
        if (document.Root == null || document.Root.Name.LocalName != "SmileProject")
            throw new InvalidDataException("A .smileproj file must have a SmileProject root element.");
        return new ProjectContext(fullProjectPath, Path.GetDirectoryName(fullProjectPath)!, document, document.Root);
    }

    private static SmileProjectSourceSet Save(ProjectContext context)
    {
        while (context.Document.LastNode is XText trailingWhitespace &&
               string.IsNullOrWhiteSpace(trailingWhitespace.Value))
            trailingWhitespace.Remove();
        File.WriteAllText(context.ProjectPath, context.Document.ToString() + Environment.NewLine,
            new System.Text.UTF8Encoding(false));
        return SmileProjectSourceSet.Load(context.ProjectPath);
    }

    private static IEnumerable<XElement> SourceElements(XElement root) =>
        root.Elements().Where(element => element.Name.LocalName == "ItemGroup")
            .SelectMany(element => element.Elements().Where(item => item.Name.LocalName == "SmileSource"));

    private static void AppendItem(XElement itemGroup, XElement item)
    {
        var trailingWhitespace = itemGroup.Nodes().LastOrDefault() as XText;
        if (trailingWhitespace != null && string.IsNullOrWhiteSpace(trailingWhitespace.Value))
            trailingWhitespace.AddBeforeSelf(new XText(Environment.NewLine + "    "), item);
        else
            itemGroup.Add(new XText(Environment.NewLine + "    "), item, new XText(Environment.NewLine + "  "));
    }

    private static XElement FindSource(ProjectContext context, string sourcePath) =>
        SourceElements(context.Root).FirstOrDefault(element => SameInclude(context.ProjectDirectory, element, sourcePath))
        ?? throw new InvalidDataException($"SMILE source '{sourcePath}' is not included in the project.");

    private static bool SameInclude(string projectDirectory, XElement element, string sourcePath)
    {
        var include = ((string?)element.Attribute("Include") ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(include))
            return false;
        return string.Equals(Path.GetFullPath(Path.Combine(projectDirectory, include)), Path.GetFullPath(sourcePath),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string StartupValue(XElement root)
    {
        var value = root.Elements().FirstOrDefault(element => element.Name.LocalName == "PropertyGroup")?
            .Elements().FirstOrDefault(element => element.Name.LocalName == "StartupFile")?.Value.Trim();
        return string.IsNullOrWhiteSpace(value) ? "Program.smile" : value!;
    }

    private static string RelativeSourceInclude(string projectDirectory, string sourcePath)
    {
        var fullDirectory = Path.GetFullPath(projectDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var fullSourcePath = Path.GetFullPath(sourcePath);
        if (!string.Equals(Path.GetExtension(fullSourcePath), ".smile", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Only .smile source files can be included in a SMILE project.");
        if (!fullSourcePath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("SMILE project source files must be inside the project directory.");
        return fullSourcePath.Substring(fullDirectory.Length).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }

    private sealed class ProjectContext
    {
        public ProjectContext(string projectPath, string projectDirectory, XDocument document, XElement root)
        { ProjectPath = projectPath; ProjectDirectory = projectDirectory; Document = document; Root = root; }
        public string ProjectPath { get; }
        public string ProjectDirectory { get; }
        public XDocument Document { get; }
        public XElement Root { get; }
    }
}
