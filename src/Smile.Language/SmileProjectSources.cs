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
        foreach (var source in CompilationSources)
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
