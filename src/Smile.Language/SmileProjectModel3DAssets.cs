using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Smile.Language;

public enum SmileModel3DAssetProfile
{
    Static,
    Character
}

public enum SmileModel3DProductionState
{
    Prototype,
    ProductionCandidate,
    ProductionApproved
}

public sealed class SmileProjectModel3DAssetItem
{
    internal SmileProjectModel3DAssetItem(string include, string fullPath, string logicalPath,
        SmileModel3DAssetProfile profile, string? descriptorPath, string? identity,
        string textureOutputDirectory, int? sampleRate, SmileModel3DProductionState productionState,
        int line, int column)
    {
        Include = include;
        FullPath = Path.GetFullPath(fullPath);
        LogicalPath = logicalPath;
        Profile = profile;
        DescriptorPath = descriptorPath == null ? null : Path.GetFullPath(descriptorPath);
        Identity = identity;
        TextureOutputDirectory = textureOutputDirectory;
        SampleRate = sampleRate;
        ProductionState = productionState;
        Line = line;
        Column = column;
    }

    public string Include { get; }
    public string FullPath { get; }
    public string LogicalPath { get; }
    public SmileModel3DAssetProfile Profile { get; }
    public string? DescriptorPath { get; }
    public string? Identity { get; }
    public string TextureOutputDirectory { get; }
    public int? SampleRate { get; }
    public SmileModel3DProductionState ProductionState { get; }
    public int Line { get; }
    public int Column { get; }
}

public sealed class SmileProjectModel3DAssetSet
{
    internal SmileProjectModel3DAssetSet(string projectPath, IReadOnlyList<SmileProjectModel3DAssetItem> items,
        IReadOnlyList<SmileProjectDiagnostic> diagnostics)
    {
        ProjectPath = Path.GetFullPath(projectPath);
        Items = items;
        Diagnostics = diagnostics;
    }

    public string ProjectPath { get; }
    public IReadOnlyList<SmileProjectModel3DAssetItem> Items { get; }
    public IReadOnlyList<SmileProjectDiagnostic> Diagnostics { get; }

    public void ValidateForBuild()
    {
        var error = Diagnostics.FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        if (error != null)
            throw new SmileProjectDiagnosticException(error.Code, error.Message, error.FilePath,
                error.Line, error.Column);
    }
}

public static class SmileProjectModel3DAssetResolver
{
    private const int MaximumItems = 64;
    private static readonly HashSet<string> AllowedAttributes = new(StringComparer.Ordinal)
    {
        "Include", "LogicalPath", "Profile", "Descriptor", "Identity", "TextureOutputDirectory",
        "SampleRate", "ProductionState"
    };
    private static readonly Regex IdentityPattern = new(
        "^[a-z][a-z0-9-]*(\\.[a-z][a-z0-9-]*)+$", RegexOptions.CultureInvariant);

    internal static SmileProjectModel3DAssetSet Resolve(string projectPath, SmileProjectKind projectKind,
        XElement root)
    {
        var fullProjectPath = Path.GetFullPath(projectPath);
        var projectDirectory = Path.GetDirectoryName(fullProjectPath) ?? Environment.CurrentDirectory;
        var diagnostics = new List<SmileProjectDiagnostic>();
        var items = new List<SmileProjectModel3DAssetItem>();
        var elements = root.Elements().Where(element => element.Name.LocalName == "ItemGroup")
            .SelectMany(element => element.Elements().Where(item => item.Name.LocalName == "Model3DAsset"))
            .ToArray();

        if (elements.Length > MaximumItems)
            diagnostics.Add(new SmileProjectDiagnostic("SML3701",
                $"A project may declare at most {MaximumItems} Model3DAsset items.", fullProjectPath));
        if (projectKind == SmileProjectKind.Library && elements.Length != 0)
            diagnostics.Add(new SmileProjectDiagnostic("SML3702",
                "Library project Model3DAsset items are not supported; declare cooked models in the consuming application project.",
                fullProjectPath, Line(elements[0]), Column(elements[0])));

        var logicalPaths = new Dictionary<string, SmileProjectModel3DAssetItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in elements.Take(MaximumItems))
        {
            var includeAttribute = element.Attribute("Include");
            var include = (includeAttribute?.Value ?? string.Empty).Trim();
            var location = (XObject?)includeAttribute ?? element;
            var line = Line(location);
            var column = Column(location);
            var unsupportedAttribute = element.Attributes().FirstOrDefault(attribute =>
                !AllowedAttributes.Contains(attribute.Name.LocalName));
            if (unsupportedAttribute != null || element.HasElements)
            {
                var unsupported = unsupportedAttribute?.Name.LocalName ?? "child metadata";
                diagnostics.Add(new SmileProjectDiagnostic("SML3700",
                    $"Model3DAsset '{include}' contains unsupported metadata '{unsupported}'.",
                    fullProjectPath, Line((XObject?)unsupportedAttribute ?? element),
                    Column((XObject?)unsupportedAttribute ?? element)));
                continue;
            }
            if (!TryConcretePath(include, out var normalizedInclude, out var includeError))
            {
                diagnostics.Add(new SmileProjectDiagnostic("SML3700",
                    $"Invalid Model3DAsset Include '{include}': {includeError}", fullProjectPath, line, column));
                continue;
            }
            var extension = Path.GetExtension(normalizedInclude);
            if (!extension.Equals(".glb", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".gltf", StringComparison.OrdinalIgnoreCase))
            {
                diagnostics.Add(new SmileProjectDiagnostic("SML3700",
                    $"Model3DAsset Include '{include}' must name a .glb or .gltf file.",
                    fullProjectPath, line, column));
                continue;
            }
            if (!TryResolveContainedFile(projectDirectory, normalizedInclude, out var sourcePath,
                    out var sourceError))
            {
                diagnostics.Add(new SmileProjectDiagnostic("SML3703",
                    $"Model3DAsset source '{include}' {sourceError}", fullProjectPath, line, column));
                continue;
            }

            var logicalText = ((string?)element.Attribute("LogicalPath") ?? string.Empty).Trim();
            if (!TryConcretePath(logicalText, out var logicalPath, out var logicalError) ||
                !Path.GetExtension(logicalPath).Equals(".sm3d", StringComparison.OrdinalIgnoreCase))
            {
                diagnostics.Add(new SmileProjectDiagnostic("SML3704",
                    $"Model3DAsset LogicalPath '{logicalText}' must be a confined project-relative .sm3d path: {logicalError}",
                    fullProjectPath, line, column));
                continue;
            }

            var profileText = ((string?)element.Attribute("Profile") ?? string.Empty).Trim();
            if (!Enum.TryParse(profileText, true, out SmileModel3DAssetProfile profile) ||
                !Enum.IsDefined(typeof(SmileModel3DAssetProfile), profile))
            {
                diagnostics.Add(new SmileProjectDiagnostic("SML3705",
                    $"Model3DAsset Profile '{profileText}' must be Static or Character.",
                    fullProjectPath, line, column));
                continue;
            }

            string? descriptorPath = null;
            var descriptorText = ((string?)element.Attribute("Descriptor") ?? string.Empty).Trim();
            if (descriptorText.Length != 0)
            {
                var descriptorError = string.Empty;
                var descriptorFileError = string.Empty;
                if (!TryConcretePath(descriptorText, out var descriptorLogical, out descriptorError) ||
                    !Path.GetExtension(descriptorLogical).Equals(".json", StringComparison.OrdinalIgnoreCase) ||
                    !TryResolveContainedFile(projectDirectory, descriptorLogical, out descriptorPath,
                        out descriptorFileError))
                {
                    diagnostics.Add(new SmileProjectDiagnostic("SML3706",
                        $"Model3DAsset Descriptor '{descriptorText}' must name a confined existing .json file: " +
                        (descriptorError.Length != 0 ? descriptorError : descriptorFileError),
                        fullProjectPath, line, column));
                    continue;
                }
            }

            var identity = ((string?)element.Attribute("Identity") ?? string.Empty).Trim();
            if (identity.Length != 0 && (identity.Length > 128 || !IdentityPattern.IsMatch(identity)))
            {
                diagnostics.Add(new SmileProjectDiagnostic("SML3707",
                    $"Model3DAsset Identity '{identity}' must use lowercase dot-separated identifier segments.",
                    fullProjectPath, line, column));
                continue;
            }

            var textureText = ((string?)element.Attribute("TextureOutputDirectory") ?? string.Empty).Trim();
            if (textureText.Length == 0)
            {
                var modelDirectory = logicalPath.Contains('/')
                    ? logicalPath.Substring(0, logicalPath.LastIndexOf('/'))
                    : string.Empty;
                textureText = modelDirectory.Length == 0 ? "Textures" : modelDirectory + "/Textures";
            }
            if (!TryConcretePath(textureText, out var textureDirectory, out var textureError) ||
                Path.HasExtension(textureDirectory))
            {
                diagnostics.Add(new SmileProjectDiagnostic("SML3708",
                    $"Model3DAsset TextureOutputDirectory '{textureText}' must be a confined project-relative directory: {textureError}",
                    fullProjectPath, line, column));
                continue;
            }

            int? sampleRate = null;
            var sampleText = ((string?)element.Attribute("SampleRate") ?? string.Empty).Trim();
            if (sampleText.Length != 0)
            {
                if (!int.TryParse(sampleText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ||
                    parsed is < 15 or > 60)
                {
                    diagnostics.Add(new SmileProjectDiagnostic("SML3709",
                        $"Model3DAsset SampleRate '{sampleText}' must be a whole number from 15 through 60.",
                        fullProjectPath, line, column));
                    continue;
                }
                sampleRate = parsed;
            }

            var productionText = ((string?)element.Attribute("ProductionState") ?? "Prototype").Trim();
            if (!Enum.TryParse(productionText, true, out SmileModel3DProductionState productionState) ||
                !Enum.IsDefined(typeof(SmileModel3DProductionState), productionState))
            {
                diagnostics.Add(new SmileProjectDiagnostic("SML3710",
                    $"Model3DAsset ProductionState '{productionText}' must be Prototype, ProductionCandidate, or ProductionApproved.",
                    fullProjectPath, line, column));
                continue;
            }

            var item = new SmileProjectModel3DAssetItem(normalizedInclude, sourcePath, logicalPath, profile,
                descriptorPath, identity.Length == 0 ? null : identity, textureDirectory, sampleRate,
                productionState, line, column);
            if (logicalPaths.TryGetValue(logicalPath, out var existing))
            {
                diagnostics.Add(new SmileProjectDiagnostic("SML3711",
                    $"Model3DAsset outputs '{existing.LogicalPath}' and '{logicalPath}' collide on portable filesystems.",
                    fullProjectPath, line, column));
                continue;
            }
            logicalPaths.Add(logicalPath, item);
            items.Add(item);
        }

        return new SmileProjectModel3DAssetSet(fullProjectPath,
            items.OrderBy(item => item.LogicalPath, StringComparer.Ordinal).ToArray(), diagnostics);
    }

    private static bool TryConcretePath(string text, out string normalized, out string error)
    {
        if (!SmileProjectAssetResolver.TryNormalizePattern(text, allowWildcards: false, out normalized,
                out _, out _, out error))
            return false;
        return true;
    }

    private static bool TryResolveContainedFile(string projectDirectory, string logicalPath,
        out string fullPath, out string error)
    {
        var current = Path.GetFullPath(projectDirectory);
        var actual = new List<string>();
        var caseMismatch = false;
        foreach (var segment in logicalPath.Split('/'))
        {
            if (!Directory.Exists(current))
            {
                fullPath = Path.Combine(current, segment);
                error = $"was not found at '{fullPath}'.";
                return false;
            }
            var entries = Directory.EnumerateFileSystemEntries(current)
                .OrderBy(path => path, StringComparer.Ordinal).ToArray();
            var match = entries.FirstOrDefault(path =>
                string.Equals(Path.GetFileName(path), segment, StringComparison.Ordinal));
            if (match == null)
            {
                match = entries.FirstOrDefault(path =>
                    string.Equals(Path.GetFileName(path), segment, StringComparison.OrdinalIgnoreCase));
                caseMismatch |= match != null;
            }
            if (match == null)
            {
                fullPath = Path.Combine(current, segment);
                error = $"was not found at '{fullPath}'.";
                return false;
            }
            actual.Add(Path.GetFileName(match));
            current = Path.GetFullPath(match);
        }
        fullPath = current;
        if (!File.Exists(fullPath))
        {
            error = $"was not found at '{fullPath}'.";
            return false;
        }
        if (caseMismatch)
        {
            error = $"does not match filesystem case; actual path is '{string.Join("/", actual)}'.";
            return false;
        }
        error = string.Empty;
        return true;
    }

    private static int Line(XObject value) => value is IXmlLineInfo info && info.HasLineInfo()
        ? info.LineNumber : 1;

    private static int Column(XObject value) => value is IXmlLineInfo info && info.HasLineInfo()
        ? info.LinePosition : 1;
}
