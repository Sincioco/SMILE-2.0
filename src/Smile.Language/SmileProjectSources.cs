using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Smile.Language;

public enum SmileProjectKind
{
    Console,
    Game,
    Library
}

public enum SmileProjectReferenceKind
{
    Project,
    Package
}

public static class SmileApplicationIdentity
{
    private static readonly Regex SegmentPattern = new("^[a-z][a-z0-9-]*$", RegexOptions.CultureInvariant);

    public static bool IsValid(string? value)
    {
        if (value == null || value.Length < 3 || value.Length > 128 || value.IndexOf('.') < 0)
            return false;
        var segments = value.Split('.');
        return segments.Length >= 2 && segments.All(segment =>
            segment.Length != 0 && !segment.EndsWith("-", StringComparison.Ordinal) && SegmentPattern.IsMatch(segment));
    }

    public static string ValidateExplicit(string? value, string filePath, int line = 1, int column = 1)
    {
        if (!IsValid(value))
            throw new SmileProjectDiagnosticException("SML3800",
                "ApplicationId must be 3 through 128 ASCII lowercase characters in at least two dot-separated segments; each segment begins with a letter and contains only lowercase letters, digits, or non-trailing hyphens.",
                filePath, line, column);
        return value!;
    }
}

public sealed class SmileProjectReferenceItem
{
    internal SmileProjectReferenceItem(string include, string fullPath, SmileProjectReferenceKind kind)
    {
        Include = include;
        FullPath = fullPath;
        Kind = kind;
    }

    public string Include { get; }
    public string FullPath { get; }
    public SmileProjectReferenceKind Kind { get; }
    public bool Exists => File.Exists(FullPath);
    public string DisplayName => Path.GetFileNameWithoutExtension(FullPath);
}

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
    private SmileProjectSourceSet(string projectPath, SmileProjectKind projectKind, string startupFile,
        string libraryName, string version, string outputName, string? applicationId,
        bool rememberWindowPlacement, bool responsiveWindow, string? webLoadingAuthor, string? webLoadingLogoPath,
        IReadOnlyList<SmileProjectSourceItem> items, IReadOnlyList<SmileProjectSourceItem> compilationSources,
        IReadOnlyList<SmileProjectReferenceItem> references, SmileProjectAssetManifest assetManifest,
        SmileProjectModel3DAssetSet model3DAssets)
    {
        ProjectPath = projectPath;
        ProjectDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory;
        StartupFile = startupFile;
        ProjectKind = projectKind;
        LibraryName = libraryName;
        Version = version;
        OutputName = outputName;
        ApplicationId = applicationId;
        RememberWindowPlacement = rememberWindowPlacement;
        ResponsiveWindow = responsiveWindow;
        WebLoadingAuthor = webLoadingAuthor;
        WebLoadingLogoPath = webLoadingLogoPath;
        Items = items;
        CompilationSources = compilationSources;
        StartupSource = projectKind == SmileProjectKind.Library ? null : compilationSources[0];
        SupportSources = projectKind == SmileProjectKind.Library ? compilationSources : compilationSources.Skip(1).ToArray();
        References = references;
        AssetManifest = assetManifest;
        Model3DAssets = model3DAssets;
    }

    public string ProjectPath { get; }
    public string ProjectDirectory { get; }
    public string StartupFile { get; }
    public SmileProjectKind ProjectKind { get; }
    public string LibraryName { get; }
    public string Version { get; }
    public string OutputName { get; }
    public string? ApplicationId { get; }
    public string EffectiveApplicationId => ApplicationId ?? OutputName;
    public bool RememberWindowPlacement { get; }
    public bool ResponsiveWindow { get; }
    public string? WebLoadingAuthor { get; }
    public string? WebLoadingLogoPath { get; }
    public bool IsLibrary => ProjectKind == SmileProjectKind.Library;
    public SmileProjectSourceItem? StartupSource { get; }
    public IReadOnlyList<SmileProjectSourceItem> Items { get; }
    public IReadOnlyList<SmileProjectSourceItem> CompilationSources { get; }
    public IReadOnlyList<SmileProjectSourceItem> SupportSources { get; }
    public IReadOnlyList<SmileProjectReferenceItem> References { get; }
    public SmileProjectAssetManifest AssetManifest { get; }
    public IReadOnlyList<string> AssetPaths => AssetManifest.AssetPaths;
    public SmileProjectModel3DAssetSet Model3DAssets { get; }

    public IReadOnlyList<SmileProjectSourceItem> GetCompilationSourcesFor(string filePath)
    {
        if (IsLibrary)
            return CompilationSources;
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
            throw new InvalidDataException("A SMILE project file must have a SmileProject root element.");

        var projectDirectory = Path.GetDirectoryName(fullProjectPath) ?? Environment.CurrentDirectory;
        var propertyGroups = root.Elements().Where(element => element.Name.LocalName == "PropertyGroup").ToArray();
        var properties = propertyGroups.FirstOrDefault();
        var kindText = properties?.Elements().FirstOrDefault(element => element.Name.LocalName == "ProjectKind")?.Value.Trim();
        if (string.IsNullOrWhiteSpace(kindText))
            kindText = string.Equals(Path.GetExtension(fullProjectPath), ".smilelibproj", StringComparison.OrdinalIgnoreCase)
                ? "Library" : "Console";
        if (!Enum.TryParse(kindText, true, out SmileProjectKind projectKind) ||
            !Enum.IsDefined(typeof(SmileProjectKind), projectKind))
            throw new InvalidDataException($"Unknown ProjectKind '{kindText}'. Expected Console, Game, or Library.");
        if (string.Equals(Path.GetExtension(fullProjectPath), ".smilelibproj", StringComparison.OrdinalIgnoreCase) &&
            projectKind != SmileProjectKind.Library)
            throw new InvalidDataException("A .smilelibproj must declare ProjectKind Library.");

        var libraryName = properties?.Elements().FirstOrDefault(element => element.Name.LocalName == "LibraryName")?.Value.Trim() ?? string.Empty;
        var version = properties?.Elements().FirstOrDefault(element => element.Name.LocalName == "Version")?.Value.Trim() ?? string.Empty;
        var outputName = properties?.Elements().FirstOrDefault(element => element.Name.LocalName == "OutputName")?.Value.Trim();
        if (projectKind == SmileProjectKind.Library)
        {
            if (string.IsNullOrWhiteSpace(libraryName))
                throw new InvalidDataException("LibraryName is required for a SMILE library project.");
            if (!System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d+\.\d+\.\d+$"))
                throw new InvalidDataException("Library Version is required and must use major.minor.patch.");
            if (string.IsNullOrWhiteSpace(outputName))
                outputName = libraryName;
        }
        else if (string.IsNullOrWhiteSpace(outputName))
        {
            outputName = Path.GetFileNameWithoutExtension(fullProjectPath);
        }

        var applicationElements = propertyGroups.SelectMany(group =>
            group.Elements().Where(element => element.Name.LocalName == "ApplicationId")).ToArray();
        if (applicationElements.Length > 1)
        {
            var duplicate = applicationElements[1];
            var duplicateLocation = (IXmlLineInfo)duplicate;
            throw new SmileProjectDiagnosticException("SML3801",
                "ApplicationId may be declared only once in a SMILE project.", fullProjectPath,
                duplicateLocation.HasLineInfo() ? duplicateLocation.LineNumber : 1,
                duplicateLocation.HasLineInfo() ? duplicateLocation.LinePosition : 1);
        }

        string? applicationId = null;
        if (applicationElements.Length == 1)
        {
            var applicationElement = applicationElements[0];
            var location = (IXmlLineInfo)applicationElement;
            var line = location.HasLineInfo() ? location.LineNumber : 1;
            var column = location.HasLineInfo() ? location.LinePosition : 1;
            if (projectKind == SmileProjectKind.Library)
                throw new SmileProjectDiagnosticException("SML3802",
                    "Library projects do not own an ApplicationId; declare it only in Console or Game application projects.",
                    fullProjectPath, line, column);
            applicationId = SmileApplicationIdentity.ValidateExplicit(applicationElement.Value.Trim(),
                fullProjectPath, line, column);
        }

        var placementElements = propertyGroups.SelectMany(group =>
            group.Elements().Where(element => element.Name.LocalName == "RememberWindowPlacement")).ToArray();
        if (placementElements.Length > 1)
        {
            var duplicate = placementElements[1];
            var duplicateLocation = (IXmlLineInfo)duplicate;
            throw new SmileProjectDiagnosticException("SML3804",
                "RememberWindowPlacement may be declared only once in a SMILE project.", fullProjectPath,
                duplicateLocation.HasLineInfo() ? duplicateLocation.LineNumber : 1,
                duplicateLocation.HasLineInfo() ? duplicateLocation.LinePosition : 1);
        }

        var rememberWindowPlacement = false;
        if (placementElements.Length == 1)
        {
            var placementElement = placementElements[0];
            var location = (IXmlLineInfo)placementElement;
            var line = location.HasLineInfo() ? location.LineNumber : 1;
            var column = location.HasLineInfo() ? location.LinePosition : 1;
            if (!bool.TryParse(placementElement.Value.Trim(), out rememberWindowPlacement))
                throw new SmileProjectDiagnosticException("SML3805",
                    "RememberWindowPlacement must be true or false.", fullProjectPath, line, column);
            if (rememberWindowPlacement && projectKind != SmileProjectKind.Game)
                throw new SmileProjectDiagnosticException("SML3806",
                    "RememberWindowPlacement is available only to Game projects.", fullProjectPath, line, column);
            if (rememberWindowPlacement && applicationId == null)
                throw new SmileProjectDiagnosticException("SML3807",
                    "RememberWindowPlacement requires an explicit stable ApplicationId.",
                    fullProjectPath, line, column);
        }

        var responsiveElements = propertyGroups.SelectMany(group =>
            group.Elements().Where(element => element.Name.LocalName == "ResponsiveWindow")).ToArray();
        if (responsiveElements.Length > 1)
        {
            var duplicate = responsiveElements[1];
            var duplicateLocation = (IXmlLineInfo)duplicate;
            throw new SmileProjectDiagnosticException("SML3808",
                "ResponsiveWindow may be declared only once in a SMILE project.", fullProjectPath,
                duplicateLocation.HasLineInfo() ? duplicateLocation.LineNumber : 1,
                duplicateLocation.HasLineInfo() ? duplicateLocation.LinePosition : 1);
        }

        var responsiveWindow = false;
        if (responsiveElements.Length == 1)
        {
            var responsiveElement = responsiveElements[0];
            var location = (IXmlLineInfo)responsiveElement;
            var line = location.HasLineInfo() ? location.LineNumber : 1;
            var column = location.HasLineInfo() ? location.LinePosition : 1;
            if (!bool.TryParse(responsiveElement.Value.Trim(), out responsiveWindow))
                throw new SmileProjectDiagnosticException("SML3809",
                    "ResponsiveWindow must be true or false.", fullProjectPath, line, column);
            if (responsiveWindow && projectKind != SmileProjectKind.Game)
                throw new SmileProjectDiagnosticException("SML3810",
                    "ResponsiveWindow is available only to Game projects.", fullProjectPath, line, column);
        }

        var authorElements = propertyGroups.SelectMany(group =>
            group.Elements().Where(element => element.Name.LocalName == "WebLoadingAuthor")).ToArray();
        string? webLoadingAuthor = null;
        if (authorElements.Length != 0)
        {
            var element = authorElements.Length > 1 ? authorElements[1] : authorElements[0];
            var location = (IXmlLineInfo)element;
            webLoadingAuthor = element.Value.Trim();
            if (authorElements.Length > 1 || projectKind == SmileProjectKind.Library ||
                element.HasElements || webLoadingAuthor.Length is < 1 or > 128 ||
                webLoadingAuthor.Any(char.IsControl))
                throw new SmileProjectDiagnosticException("SML3811",
                    "WebLoadingAuthor must be declared at most once in an application project, as 1 through 128 characters on one line.",
                    fullProjectPath, location.HasLineInfo() ? location.LineNumber : 1,
                    location.HasLineInfo() ? location.LinePosition : 1);
        }

        var logoElements = propertyGroups.SelectMany(group =>
            group.Elements().Where(element => element.Name.LocalName == "WebLoadingLogo")).ToArray();
        string? webLoadingLogoPath = null;
        if (logoElements.Length != 0)
        {
            var element = logoElements.Length > 1 ? logoElements[1] : logoElements[0];
            var location = (IXmlLineInfo)element;
            var value = element.Value.Trim();
            if (logoElements.Length > 1 || projectKind == SmileProjectKind.Library || element.HasElements ||
                value.Length is < 1 or > 512 || value.Any(char.IsControl) ||
                value.IndexOfAny(new[] { ':', '*', '?', '<', '>', '"', '|' }) >= 0 ||
                value.StartsWith("/") || value.StartsWith("\\") ||
                !string.Equals(Path.GetExtension(value), ".png", StringComparison.OrdinalIgnoreCase))
                throw new SmileProjectDiagnosticException("SML3812",
                    "WebLoadingLogo must be declared at most once in an application project, as a project-relative PNG file path.",
                    fullProjectPath, location.HasLineInfo() ? location.LineNumber : 1,
                    location.HasLineInfo() ? location.LinePosition : 1);
            webLoadingLogoPath = Path.GetFullPath(Path.Combine(projectDirectory, value));
        }

        var startupFile = properties?.Elements().FirstOrDefault(element => element.Name.LocalName == "StartupFile")?.Value.Trim();
        if (projectKind != SmileProjectKind.Library && string.IsNullOrWhiteSpace(startupFile))
            startupFile = "Program.smile";
        if (projectKind == SmileProjectKind.Library && !string.IsNullOrWhiteSpace(startupFile))
            throw new InvalidDataException("SMILE library projects do not have a StartupFile.");
        var startupPath = projectKind == SmileProjectKind.Library ? string.Empty :
            Path.GetFullPath(Path.Combine(projectDirectory, startupFile!));

        var sourceElements = root.Elements().Where(element => element.Name.LocalName == "ItemGroup")
            .SelectMany(element => element.Elements().Where(item => item.Name.LocalName == "SmileSource"))
            .ToArray();
        if (sourceElements.Length == 0 && projectKind != SmileProjectKind.Library)
            sourceElements = new[] { new XElement("SmileSource", new XAttribute("Include", startupFile!)) };
        if (sourceElements.Length == 0)
            throw new InvalidDataException("A SMILE library project requires at least one SmileSource.");

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

        if (projectKind != SmileProjectKind.Library && !paths.Contains(startupPath))
            throw new InvalidDataException($"StartupFile '{startupFile}' is not listed as a SmileSource.");

        var items = entries.Select(entry => new SmileProjectSourceItem(
            entry.Include, entry.FullPath, entry.StartupOnly,
            projectKind != SmileProjectKind.Library && string.Equals(entry.FullPath, startupPath, StringComparison.OrdinalIgnoreCase))).ToArray();
        var compilationSources = projectKind == SmileProjectKind.Library
            ? items
            : new[] { items.Single(item => item.IsStartup) }.Concat(items.Where(item => item.IsSupport)).ToArray();

        var references = new List<SmileProjectReferenceItem>();
        var referencePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in root.Elements().Where(element => element.Name.LocalName == "ItemGroup").SelectMany(element => element.Elements()))
        {
            SmileProjectReferenceKind referenceKind;
            string expectedExtension;
            if (element.Name.LocalName == "SmileProjectReference")
            {
                referenceKind = SmileProjectReferenceKind.Project;
                expectedExtension = ".smilelibproj";
            }
            else if (element.Name.LocalName == "SmileLibraryReference")
            {
                referenceKind = SmileProjectReferenceKind.Package;
                expectedExtension = ".smilelib";
            }
            else
            {
                continue;
            }
            var include = ((string?)element.Attribute("Include") ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(include) || !string.Equals(Path.GetExtension(include), expectedExtension, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"{element.Name.LocalName} Include must name a {expectedExtension} file.");
            var referencePath = Path.GetFullPath(Path.Combine(projectDirectory, include));
            if (!referencePaths.Add(referencePath))
                throw new InvalidDataException($"Duplicate SMILE library reference '{include}'.");
            references.Add(new SmileProjectReferenceItem(include, referencePath, referenceKind));
        }

        var assetManifest = SmileProjectAssetResolver.Resolve(fullProjectPath, projectKind, root);
        var model3DAssets = SmileProjectModel3DAssetResolver.Resolve(fullProjectPath, projectKind, root);
        return new SmileProjectSourceSet(fullProjectPath, projectKind, startupFile ?? string.Empty,
            libraryName, version, outputName!, applicationId, rememberWindowPlacement, responsiveWindow, webLoadingAuthor, webLoadingLogoPath,
            items, compilationSources, references, assetManifest,
            model3DAssets);
    }

    public void ValidateFiles()
    {
        foreach (var source in Items)
        {
            if (!File.Exists(source.FullPath))
            {
                var role = IsLibrary ? "Library" : source.IsStartup ? "Startup" : "Support";
                throw new FileNotFoundException($"{role} source file was not found: {source.FullPath}", source.FullPath);
            }
        }
    }

    public void ValidateAssetsForBuild()
    {
        AssetManifest.ValidateForBuild();
        Model3DAssets.ValidateForBuild();
    }

    public void ValidateReferences()
    {
        foreach (var reference in References)
        {
            if (!reference.Exists)
                throw new FileNotFoundException($"Referenced SMILE {reference.Kind.ToString().ToLowerInvariant()} was not found: {reference.FullPath}", reference.FullPath);
        }
    }

    public string GetLibraryOutputPath(string configuration) => Path.Combine(ProjectDirectory, "bin",
        configuration.StartsWith("Release", StringComparison.OrdinalIgnoreCase) ? "Release" : "Debug",
        OutputName + ".smilelib");

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

    public static SmileProjectSourceSet AddReference(string projectPath, string referencePath)
    {
        var context = Load(projectPath);
        var fullReferencePath = Path.GetFullPath(referencePath);
        var extension = Path.GetExtension(fullReferencePath);
        var elementName = string.Equals(extension, ".smilelibproj", StringComparison.OrdinalIgnoreCase)
            ? "SmileProjectReference"
            : string.Equals(extension, ".smilelib", StringComparison.OrdinalIgnoreCase)
                ? "SmileLibraryReference"
                : throw new InvalidDataException("SMILE library references must be .smilelibproj or .smilelib files.");
        var existing = ReferenceElements(context.Root).ToArray();
        if (existing.Any(element => SameInclude(context.ProjectDirectory, element, fullReferencePath)))
            throw new InvalidDataException($"SMILE library reference '{Path.GetFileName(fullReferencePath)}' is already included in the project.");
        var itemGroup = context.Root.Elements().FirstOrDefault(element => element.Name.LocalName == "ItemGroup" &&
            element.Elements().Any(item => item.Name.LocalName is "SmileProjectReference" or "SmileLibraryReference"));
        if (itemGroup == null)
        {
            itemGroup = new XElement(context.Root.Name.Namespace + "ItemGroup");
            context.Root.Add(itemGroup);
        }
        var include = RelativePath(context.ProjectDirectory, fullReferencePath)
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        AppendItem(itemGroup, new XElement(context.Root.Name.Namespace + elementName, new XAttribute("Include", include)));
        return Save(context);
    }

    public static SmileProjectSourceSet RemoveReference(string projectPath, string referencePath)
    {
        var context = Load(projectPath);
        var reference = ReferenceElements(context.Root).FirstOrDefault(element =>
            SameInclude(context.ProjectDirectory, element, referencePath))
            ?? throw new InvalidDataException($"SMILE library reference '{referencePath}' is not included in the project.");
        if (reference.PreviousNode is XText indentation && string.IsNullOrWhiteSpace(indentation.Value))
            indentation.Remove();
        reference.Remove();
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

    private static IEnumerable<XElement> ReferenceElements(XElement root) =>
        root.Elements().Where(element => element.Name.LocalName == "ItemGroup")
            .SelectMany(element => element.Elements().Where(item =>
                item.Name.LocalName is "SmileProjectReference" or "SmileLibraryReference"));

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

    private static string RelativePath(string directoryPath, string filePath)
    {
        var directoryUri = new Uri(Path.GetFullPath(directoryPath).TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar);
        var fileUri = new Uri(Path.GetFullPath(filePath));
        if (!string.Equals(directoryUri.Scheme, fileUri.Scheme, StringComparison.OrdinalIgnoreCase))
            return filePath;
        return Uri.UnescapeDataString(directoryUri.MakeRelativeUri(fileUri).ToString())
            .Replace('/', Path.DirectorySeparatorChar);
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
