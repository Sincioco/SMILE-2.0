using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using EnvDTE;
using Smile.Language;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualBasic;
using System.Windows.Forms;

namespace Smile.VisualStudio;

[Guid(SmileProjectTypeGuidString)]
internal sealed class SmileProjectFactory : IVsProjectFactory, IDisposable
{
    public const string SmileProjectTypeGuidString = "4fbc4e72-6b0f-40d8-b758-bf8932926d5d";

    private readonly SmilePackage _package;
    private readonly List<SmileProject> _projects = new();
    private Microsoft.VisualStudio.OLE.Interop.IServiceProvider? _site;

    public SmileProjectFactory(SmilePackage package) => _package = package;

    public int SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider pSP)
    {
        _site = pSP;
        return VSConstants.S_OK;
    }

    public int CanCreateProject(string pszFilename, uint grfCreateFlags, out int pfCanCreate)
    {
        pfCanCreate = string.Equals(Path.GetExtension(pszFilename), ".smileproj", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        return VSConstants.S_OK;
    }

    public int CreateProject(string pszFilename, string pszLocation, string pszName, uint grfCreateFlags,
        ref Guid iidProject, out IntPtr ppvProject, out int pfCanceled)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        ppvProject = IntPtr.Zero;
        pfCanceled = 0;
        try
        {
            var flags = (__VSCREATEPROJFLAGS)grfCreateFlags;
            var projectPath = flags.HasFlag(__VSCREATEPROJFLAGS.CPF_CLONEFILE)
                ? CloneTemplate(pszFilename, pszLocation, pszName, flags.HasFlag(__VSCREATEPROJFLAGS.CPF_OVERWRITE))
                : Path.GetFullPath(pszFilename);

            var project = new SmileProject(_package, projectPath);
            if (_site != null)
                project.SetSite(_site);
            project.Load(projectPath, 0, 0);
            _projects.Add(project);

            var unknown = Marshal.GetIUnknownForObject(project);
            try
            {
                var result = Marshal.QueryInterface(unknown, ref iidProject, out ppvProject);
                if (result != VSConstants.S_OK)
                    Marshal.ThrowExceptionForHR(result);
            }
            finally
            {
                Marshal.Release(unknown);
            }
            return VSConstants.S_OK;
        }
        catch (Exception exception)
        {
            ActivityLog.LogError(nameof(SmileProjectFactory), exception.ToString());
            return Marshal.GetHRForException(exception);
        }
    }

    public int Close()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        Dispose();
        return VSConstants.S_OK;
    }

    public void Dispose()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        foreach (var project in _projects.ToArray())
            project.Close();
        _projects.Clear();
    }

    private static string CloneTemplate(string templateProject, string location, string name, bool overwrite)
    {
        var templateDirectory = Path.GetDirectoryName(Path.GetFullPath(templateProject))!;
        var projectName = Path.GetFileNameWithoutExtension(name);
        var destinationDirectory = Path.GetFullPath(location);
        Directory.CreateDirectory(destinationDirectory);

        foreach (var source in Directory.EnumerateFiles(templateDirectory, "*", SearchOption.AllDirectories))
        {
            if (source.EndsWith(".vstemplate", StringComparison.OrdinalIgnoreCase))
                continue;
            var relative = source.Substring(templateDirectory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var destinationName = string.Equals(source, Path.GetFullPath(templateProject), StringComparison.OrdinalIgnoreCase)
                ? projectName + ".smileproj"
                : relative;
            var destination = Path.Combine(destinationDirectory, destinationName);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            if (File.Exists(destination) && !overwrite)
                throw new IOException($"Project template destination already exists: {destination}");

            if (source.EndsWith(".smile", StringComparison.OrdinalIgnoreCase) || source.EndsWith(".smileproj", StringComparison.OrdinalIgnoreCase))
            {
                var text = File.ReadAllText(source)
                    .Replace("$safeprojectname$", projectName)
                    .Replace("$projectname$", projectName);
                File.WriteAllText(destination, text, new UTF8Encoding(false));
            }
            else
            {
                File.Copy(source, destination, true);
            }
        }

        return Path.Combine(destinationDirectory, projectName + ".smileproj");
    }
}

internal sealed class SmileProject : IVsUIHierarchy, IVsProject2, IVsGetCfgProvider, IPersistFileFormat, IVsPersistHierarchyItem2, IOleCommandTarget
{
    private const int CommandNotSupported = unchecked((int)0x80040100);

    private readonly SmilePackage _package;
    private readonly Dictionary<uint, ProjectItem> _items = new();
    private readonly Dictionary<uint, IVsHierarchyEvents> _events = new();
    private readonly Guid _projectGuid;
    private uint _nextEventCookie = 1;
    private uint _nextItemId = 1;
    private uint _contextCommandItemId = VSConstants.VSITEMID_ROOT;
    private Microsoft.VisualStudio.OLE.Interop.IServiceProvider? _site;
    private SmileConfigurationProvider? _configurationProvider;

    public SmileProject(SmilePackage package, string projectPath)
    {
        _package = package;
        ProjectPath = Path.GetFullPath(projectPath);
        _projectGuid = StableGuid(ProjectPath);
    }

    public string ProjectPath { get; private set; }
    public string ProjectDirectory => Path.GetDirectoryName(ProjectPath)!;
    public string ProjectName => Path.GetFileNameWithoutExtension(ProjectPath);
    public string ProjectKind { get; private set; } = "Console";
    public string StartupFile { get; private set; } = "Program.smile";
    public SmileProjectSourceSet SourceSet { get; private set; } = null!;
    public string OutputName { get; private set; } = "Program";
    public SmileGraphicsBackend GraphicsBackend { get; private set; } = SmileGraphicsBackend.Auto;
    public bool VSync { get; private set; } = true;
    public IReadOnlyList<string> AssetIncludes { get; private set; } = Array.Empty<string>();

    public string GetOutputPath(string configuration) =>
        Path.Combine(ProjectDirectory, "bin", NormalizeConfiguration(configuration), SafeFileName(OutputName) + ".exe");

    public string GetWebOutputDirectory(string configuration) =>
        Path.Combine(ProjectDirectory, "bin", NormalizeConfiguration(configuration), "Web");

    public bool Build(string configuration, string platform, IVsOutputWindowPane? pane)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        pane ??= SmileBuildService.GetOutputPane();
        pane.Clear();
        pane.Activate();

        try
        {
            SourceSet.ValidateFiles();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            pane.OutputStringThreadSafe(exception.Message + "\r\n");
            return false;
        }

        var sourcePath = SourceSet.StartupSource.FullPath;
        var supportSourcePaths = SourceSet.SupportSources.Select(source => source.FullPath).ToArray();
        var participatingSourcePaths = SourceSet.CompilationSources.Select(source => source.FullPath).ToArray();
        if (!SaveOpenSourceDocuments(participatingSourcePaths, pane))
            return false;

        var compilerPath = SmileBuildService.FindCompiler(ProjectDirectory);
        if (!File.Exists(sourcePath))
        {
            pane.OutputStringThreadSafe($"Startup file was not found: {sourcePath}\r\n");
            return false;
        }
        if (compilerPath == null)
        {
            pane.OutputStringThreadSafe("smilec.exe was not found in the extension or repository artifacts.\r\n");
            return false;
        }

        SmileBuildService.CompilerResult result;
        string outputPath;
        if (IsWeb(platform))
        {
            var outputDirectory = GetWebOutputDirectory(configuration);
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, true);
            Directory.CreateDirectory(outputDirectory);
            pane.OutputStringThreadSafe($"> \"{compilerPath}\" \"{sourcePath}\"{SmileBuildService.FormatSupportArguments(supportSourcePaths)} --target web --output-dir \"{outputDirectory}\"\r\n");
            result = ThreadHelper.JoinableTaskFactory.Run(() =>
                SmileBuildService.RunWebAsync(compilerPath, sourcePath, outputDirectory, supportSourcePaths));
            outputPath = Path.Combine(outputDirectory, "index.html");
        }
        else
        {
            var emitDebugInformation = NormalizeConfiguration(configuration) == "Debug";
            outputPath = GetOutputPath(configuration);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            pane.OutputStringThreadSafe($"> \"{compilerPath}\" \"{sourcePath}\"{SmileBuildService.FormatSupportArguments(supportSourcePaths)} -o \"{outputPath}\" --graphics {GraphicsBackend} --vsync {VSync.ToString().ToLowerInvariant()}{(emitDebugInformation ? " --debug" : string.Empty)}\r\n");
            result = ThreadHelper.JoinableTaskFactory.Run(() => SmileBuildService.RunAsync(
                compilerPath, sourcePath, outputPath, GraphicsBackend, VSync, emitDebugInformation, supportSourcePaths));
        }
        if (!string.IsNullOrEmpty(result.Output))
            pane.OutputStringThreadSafe(SmileBuildService.NormalizeOutput(result.Output));
        SmileBuildService.ReportDiagnostics(result.Output);

        if (result.ExitCode != 0)
        {
            pane.OutputStringThreadSafe($"SMILE build failed with exit code {result.ExitCode}.\r\n");
            return false;
        }

        var assetOutput = IsWeb(platform) ? GetWebOutputDirectory(configuration) : Path.GetDirectoryName(outputPath)!;
        CopyAssets(assetOutput);
        pane.OutputStringThreadSafe(IsWeb(platform)
            ? $"SMILE web publish succeeded: {outputPath}\r\n"
            : $"SMILE build succeeded: {outputPath}\r\n");
        return true;
    }

    private static bool SaveOpenSourceDocuments(IReadOnlyCollection<string> sourcePaths, IVsOutputWindowPane pane)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
        if (dte == null)
            return true;

        try
        {
            foreach (Document document in dte.Documents)
            {
                if (!sourcePaths.Contains(document.FullName, StringComparer.OrdinalIgnoreCase))
                    continue;

                document.Save();
            }
        }
        catch (Exception exception)
        {
            pane.OutputStringThreadSafe($"Could not save all participating SMILE sources before building: {exception.Message}\r\n");
            return false;
        }

        return true;
    }

    public bool Clean(string configuration, string platform, IVsOutputWindowPane? pane)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        pane ??= SmileBuildService.GetOutputPane();
        var target = IsWeb(platform) ? GetWebOutputDirectory(configuration) : GetOutputPath(configuration);
        try
        {
            if (IsWeb(platform))
            {
                if (Directory.Exists(target))
                    Directory.Delete(target, true);
            }
            else
            {
                if (File.Exists(target))
                    File.Delete(target);
                var pdb = Path.ChangeExtension(target, ".pdb");
                if (File.Exists(pdb))
                    File.Delete(pdb);
            }
            pane.OutputStringThreadSafe($"Cleaned {target}\r\n");
            return true;
        }
        catch (Exception exception)
        {
            pane.OutputStringThreadSafe($"Could not clean {target}: {exception.Message}\r\n");
            return false;
        }
    }

    public bool Launch(string configuration, string platform, uint launchFlags)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (IsWeb(platform))
        {
            // Always republish on Web launch so F5/Ctrl+F5 reflects the latest saved source and assets.
            if (!Build(configuration, platform, null))
                return false;
            var url = SmileWebServer.Start(GetWebOutputDirectory(configuration));
            System.Diagnostics.Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            return true;
        }

        var outputPath = GetOutputPath(configuration);
        // Native F5 must never launch an executable built from a previous source set.
        if (!Build(configuration, platform, null))
            return false;

        var debugger = Package.GetGlobalService(typeof(SVsShellDebugger)) as IVsDebugger4;
        if (debugger == null)
            return false;

        var targets = new[]
        {
            new VsDebugTargetInfo4
            {
                dlo = (uint)DEBUG_LAUNCH_OPERATION.DLO_CreateProcess,
                LaunchFlags = launchFlags,
                bstrExe = outputPath,
                bstrCurDir = Path.GetDirectoryName(outputPath),
                guidLaunchDebugEngine = VSConstants.DebugEnginesGuids.NativeOnly_guid,
                project = this
            }
        };
        debugger.LaunchDebugTargets4(1, targets, new VsDebugTargetProcessInfo[1]);
        return true;
    }

    private void CopyAssets(string outputDirectory)
    {
        foreach (var include in AssetIncludes)
        {
            var wildcard = include.IndexOfAny(new[] { '*', '?' });
            var relativeRoot = wildcard < 0 ? include : include.Substring(0, wildcard);
            relativeRoot = relativeRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.IsNullOrWhiteSpace(relativeRoot))
                continue;

            var source = Path.GetFullPath(Path.Combine(ProjectDirectory, relativeRoot));
            if (File.Exists(source))
            {
                var destination = Path.Combine(outputDirectory, relativeRoot);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(source, destination, true);
                continue;
            }
            if (!Directory.Exists(source))
                continue;

            foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                var relative = file.Substring(ProjectDirectory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var destination = Path.Combine(outputDirectory, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(file, destination, true);
            }
        }
    }

    private IReadOnlyDictionary<uint, ProjectItem>? ReadProject(bool captureHierarchy = false)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var previousItems = captureHierarchy ? new Dictionary<uint, ProjectItem>(_items) : null;
        var document = XDocument.Load(ProjectPath, LoadOptions.SetLineInfo);
        var root = document.Root;
        if (root == null || root.Name.LocalName != "SmileProject")
            throw new InvalidDataException("A .smileproj file must have a SmileProject root element.");

        var properties = root.Elements().FirstOrDefault(element => element.Name.LocalName == "PropertyGroup");
        ProjectKind = Value(properties, "ProjectKind", "Console");
        SourceSet = SmileProjectSourceSet.Load(ProjectPath);
        StartupFile = SourceSet.StartupFile;
        OutputName = Value(properties, "OutputName", ProjectName);
        var graphicsOptions = SmileProjectGraphicsOptions.Parse(properties);
        GraphicsBackend = graphicsOptions.GraphicsBackend;
        VSync = graphicsOptions.VSync;
        AssetIncludes = root.Elements().Where(element => element.Name.LocalName == "ItemGroup")
            .SelectMany(element => element.Elements().Where(item => item.Name.LocalName == "Asset"))
            .Select(item => (string?)item.Attribute("Include") ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();

        BuildHierarchy(root);
        SmileProjectWorkspace.Register(SourceSet);
        _configurationProvider ??= new SmileConfigurationProvider(this);
        return previousItems;
    }

    private void BuildHierarchy(XElement root)
    {
        var reusableIds = _items.Values
            .Where(item => item.Kind != ItemKind.Project)
            .ToDictionary(HierarchyKey, item => item.Id, StringComparer.OrdinalIgnoreCase);
        _items.Clear();
        var rootNode = new ProjectItem(VSConstants.VSITEMID_ROOT, ProjectName, ProjectPath, ItemKind.Project, 0);
        _items[rootNode.Id] = rootNode;

        foreach (var source in SourceSet.Items.Where(source =>
                     source.StartupOnly ||
                     string.Equals(source.FullPath, SourceSet.StartupSource.FullPath, StringComparison.OrdinalIgnoreCase)))
            AddNode(rootNode, reusableIds, Path.GetFileName(source.Include), source.FullPath, ItemKind.File);

        if (ProjectKind.Equals("Game", StringComparison.OrdinalIgnoreCase) || AssetIncludes.Count != 0)
        {
            var assetsPath = Path.Combine(ProjectDirectory, "Assets");
            Directory.CreateDirectory(assetsPath);
            var assets = AddNode(rootNode, reusableIds, "Assets", assetsPath, ItemKind.Folder);
            AddDirectoryChildren(assets, assetsPath, reusableIds);
        }

        foreach (var source in SourceSet.Items.Where(source =>
                     !source.StartupOnly &&
                     !string.Equals(source.FullPath, SourceSet.StartupSource.FullPath, StringComparison.OrdinalIgnoreCase)))
            AddNode(rootNode, reusableIds, Path.GetFileName(source.Include), source.FullPath, ItemKind.File);
    }

    private ProjectItem AddNode(ProjectItem parent, IDictionary<string, uint> reusableIds, string caption, string path, ItemKind kind)
    {
        var key = HierarchyKey(kind, path);
        var id = reusableIds.TryGetValue(key, out var existingId) ? existingId : _nextItemId++;
        reusableIds.Remove(key);
        var node = new ProjectItem(id, caption, path, kind, parent.Id);
        parent.Children.Add(node.Id);
        _items[node.Id] = node;
        return node;
    }

    private void AddDirectoryChildren(ProjectItem parent, string directory, IDictionary<string, uint> reusableIds)
    {
        foreach (var childDirectory in Directory.EnumerateDirectories(directory).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var child = AddNode(parent, reusableIds, Path.GetFileName(childDirectory), childDirectory, ItemKind.Folder);
            AddDirectoryChildren(child, childDirectory, reusableIds);
        }
        foreach (var file in Directory.EnumerateFiles(directory).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            AddNode(parent, reusableIds, Path.GetFileName(file), file, ItemKind.File);
    }

    private static string HierarchyKey(ProjectItem item) => HierarchyKey(item.Kind, item.Path);

    private static string HierarchyKey(ItemKind kind, string path) => kind + "|" + Path.GetFullPath(path);

    private static string Value(XElement? group, string name, string fallback) =>
        group?.Elements().FirstOrDefault(element => element.Name.LocalName == name)?.Value.Trim() is { Length: > 0 } value ? value : fallback;

    private static string NormalizeConfiguration(string value) =>
        value.StartsWith("Release", StringComparison.OrdinalIgnoreCase) ? "Release" : "Debug";

    private static bool IsWeb(string platform) => platform.Equals("Web", StringComparison.OrdinalIgnoreCase);

    private static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var result = new string(value.Where(character => Array.IndexOf(invalid, character) < 0).ToArray());
        return string.IsNullOrWhiteSpace(result) ? "SmileProgram" : result;
    }

    private static Guid StableGuid(string value)
    {
        using var hash = MD5.Create();
        return new Guid(hash.ComputeHash(Encoding.UTF8.GetBytes(value.ToUpperInvariant())));
    }

    private ProjectItem? Item(uint itemid) => _items.TryGetValue(itemid, out var item) ? item : null;

    public int GetProperty(uint itemid, int propid, out object pvar)
    {
        pvar = null!;
        var item = Item(itemid);
        if (item == null)
            return VSConstants.E_INVALIDARG;

        if (propid == (int)__VSHPROPID.VSHPROPID_FirstChild || propid == (int)__VSHPROPID.VSHPROPID_FirstVisibleChild)
            pvar = item.Children.Count == 0 ? VSConstants.VSITEMID_NIL : item.Children[0];
        else if (propid == (int)__VSHPROPID.VSHPROPID_NextSibling || propid == (int)__VSHPROPID.VSHPROPID_NextVisibleSibling)
            pvar = NextSibling(item);
        else if (propid == (int)__VSHPROPID.VSHPROPID_Parent)
            pvar = item.Kind == ItemKind.Project ? VSConstants.VSITEMID_NIL : item.ParentId;
        else if (propid == (int)__VSHPROPID.VSHPROPID_Caption)
            pvar = IsSelectedStartup(item) ? item.Caption + " (Startup)" : item.Caption;
        else if (propid == (int)__VSHPROPID.VSHPROPID_Name)
            pvar = item.Caption;
        else if (propid == (int)__VSHPROPID.VSHPROPID_SaveName)
            pvar = item.Path;
        else if (propid == (int)__VSHPROPID.VSHPROPID_Expandable)
            pvar = item.Kind != ItemKind.File;
        else if (propid == (int)__VSHPROPID.VSHPROPID_ExpandByDefault)
            pvar = item.Kind == ItemKind.Project;
        else if (propid == (int)__VSHPROPID.VSHPROPID_ProjectDir)
            pvar = ProjectDirectory + Path.DirectorySeparatorChar;
        else if (propid == (int)__VSHPROPID.VSHPROPID_ProjectName)
            pvar = ProjectName;
        else if (propid == (int)__VSHPROPID.VSHPROPID_ConfigurationProvider)
            pvar = _configurationProvider!;
        else if (propid == (int)__VSHPROPID.VSHPROPID_DefaultEnableBuildProjectCfg)
            pvar = true;
        else if (propid == (int)__VSHPROPID.VSHPROPID_ProjectType || propid == (int)__VSHPROPID.VSHPROPID_TypeName)
            pvar = "SMILE 2.0";
        else if (propid == (int)__VSHPROPID.VSHPROPID_ReloadableProjectFile)
            pvar = true;
        else if (propid == (int)__VSHPROPID.VSHPROPID_IsHiddenItem || propid == (int)__VSHPROPID.VSHPROPID_IsNonMemberItem)
            pvar = false;
        else if (propid == (int)__VSHPROPID2.VSHPROPID_ChildrenEnumerated)
            pvar = true;
        else if (propid == (int)__VSHPROPID4.VSHPROPID_AlwaysBuildOnDebugLaunch)
            pvar = true;
        else if (propid == (int)__VSHPROPID8.VSHPROPID_SupportsIconMonikers)
            pvar = true;
        else if (propid == (int)__VSHPROPID8.VSHPROPID_IconMonikerId ||
                 propid == (int)__VSHPROPID8.VSHPROPID_OpenFolderIconMonikerId)
        {
            var openFolder = propid == (int)__VSHPROPID8.VSHPROPID_OpenFolderIconMonikerId;
            if (!TryGetIcon(item, openFolder, out var icon))
                return VSConstants.E_NOTIMPL;
            pvar = icon.Id;
        }
        else
            return VSConstants.E_NOTIMPL;
        return VSConstants.S_OK;
    }

    private uint NextSibling(ProjectItem item)
    {
        if (item.Kind == ItemKind.Project || !_items.TryGetValue(item.ParentId, out var parent))
            return VSConstants.VSITEMID_NIL;
        var index = parent.Children.IndexOf(item.Id);
        return index < 0 || index + 1 >= parent.Children.Count ? VSConstants.VSITEMID_NIL : parent.Children[index + 1];
    }

    public int GetGuidProperty(uint itemid, int propid, out Guid pguid)
    {
        pguid = Guid.Empty;
        var item = Item(itemid);
        if (item == null)
            return VSConstants.E_INVALIDARG;
        if (propid == (int)__VSHPROPID.VSHPROPID_ProjectIDGuid)
            pguid = _projectGuid;
        else if (propid == (int)__VSHPROPID.VSHPROPID_TypeGuid)
            pguid = item.Kind switch
            {
                ItemKind.Project => new Guid(SmileProjectFactory.SmileProjectTypeGuidString),
                ItemKind.Folder => VSConstants.GUID_ItemType_PhysicalFolder,
                _ => VSConstants.GUID_ItemType_PhysicalFile
            };
        else if (propid == (int)__VSHPROPID.VSHPROPID_CmdUIGuid)
            pguid = new Guid(SmileProjectFactory.SmileProjectTypeGuidString);
        else if (propid == (int)__VSHPROPID8.VSHPROPID_IconMonikerGuid ||
                 propid == (int)__VSHPROPID8.VSHPROPID_OpenFolderIconMonikerGuid)
        {
            var openFolder = propid == (int)__VSHPROPID8.VSHPROPID_OpenFolderIconMonikerGuid;
            if (!TryGetIcon(item, openFolder, out var icon))
                return VSConstants.E_NOTIMPL;
            pguid = icon.Guid;
        }
        else
            return VSConstants.E_NOTIMPL;
        return VSConstants.S_OK;
    }

    private static bool TryGetIcon(ProjectItem item, bool openFolder, out ImageMoniker icon)
    {
        if (item.Kind == ItemKind.Project)
            icon = KnownMonikers.Application;
        else if (item.Kind == ItemKind.Folder)
            icon = openFolder ? KnownMonikers.FolderOpened : KnownMonikers.FolderClosed;
        else if (item.Path.EndsWith(".smile", StringComparison.OrdinalIgnoreCase))
            icon = KnownMonikers.Code;
        else
        {
            icon = default;
            return false;
        }
        return true;
    }

    public int SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp)
    {
        _site = psp;
        return VSConstants.S_OK;
    }

    public int GetSite(out Microsoft.VisualStudio.OLE.Interop.IServiceProvider ppSP)
    {
        ppSP = _site!;
        return _site == null ? VSConstants.E_FAIL : VSConstants.S_OK;
    }

    public int QueryClose(out int pfCanClose) { pfCanClose = 1; return VSConstants.S_OK; }
    public int Close()
    {
        SmileProjectWorkspace.Unregister(ProjectPath);
        _events.Clear();
        return VSConstants.S_OK;
    }
    public int SetGuidProperty(uint itemid, int propid, ref Guid rguid) => VSConstants.E_NOTIMPL;
    public int SetProperty(uint itemid, int propid, object var) => VSConstants.E_NOTIMPL;
    public int GetNestedHierarchy(uint itemid, ref Guid iidHierarchyNested, out IntPtr ppHierarchyNested, out uint pitemidNested)
    { ppHierarchyNested = IntPtr.Zero; pitemidNested = VSConstants.VSITEMID_NIL; return VSConstants.E_NOTIMPL; }
    public int Unused0() => VSConstants.E_NOTIMPL;
    public int Unused1() => VSConstants.E_NOTIMPL;
    public int Unused2() => VSConstants.E_NOTIMPL;
    public int Unused3() => VSConstants.E_NOTIMPL;
    public int Unused4() => VSConstants.E_NOTIMPL;

    public int GetCanonicalName(uint itemid, out string pbstrName)
    {
        pbstrName = Item(itemid)?.Path ?? string.Empty;
        return string.IsNullOrEmpty(pbstrName) ? VSConstants.E_INVALIDARG : VSConstants.S_OK;
    }

    public int ParseCanonicalName(string pszName, out uint pitemid)
    {
        var fullPath = Path.GetFullPath(pszName);
        var match = _items.Values.FirstOrDefault(item => string.Equals(Path.GetFullPath(item.Path), fullPath, StringComparison.OrdinalIgnoreCase));
        pitemid = match?.Id ?? VSConstants.VSITEMID_NIL;
        return match == null ? VSConstants.E_FAIL : VSConstants.S_OK;
    }

    public int AdviseHierarchyEvents(IVsHierarchyEvents pEventSink, out uint pdwCookie)
    {
        pdwCookie = _nextEventCookie++;
        _events[pdwCookie] = pEventSink;
        return VSConstants.S_OK;
    }

    public int UnadviseHierarchyEvents(uint dwCookie)
    {
        _events.Remove(dwCookie);
        return VSConstants.S_OK;
    }

    public int QueryStatusCommand(uint itemid, ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
    {
        var item = Item(itemid);
        if (pguidCmdGroup == VSConstants.GUID_VsUIHierarchyWindowCmds && item != null)
        {
            var rightClickSupported = false;
            for (var index = 0; index < Math.Min((int)cCmds, prgCmds.Length); index++)
            {
                if (prgCmds[index].cmdID != (uint)VSConstants.VsUIHierarchyWindowCmdIds.UIHWCMDID_RightClick)
                    continue;
                prgCmds[index].cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
                rightClickSupported = true;
            }
            if (rightClickSupported)
                return VSConstants.S_OK;
        }
        if (pguidCmdGroup == SmileProjectCommands.CommandSet && item != null)
        {
            for (var index = 0; index < Math.Min((int)cCmds, prgCmds.Length); index++)
                prgCmds[index].cmdf = CommandStatus(item, prgCmds[index].cmdID);
            return VSConstants.S_OK;
        }
        if (Item(itemid)?.Kind != ItemKind.File)
            return CommandNotSupported;
        var supported = false;
        for (var index = 0; index < Math.Min((int)cCmds, prgCmds.Length); index++)
        {
            if (IsOpenCommand(pguidCmdGroup, prgCmds[index].cmdID))
            {
                prgCmds[index].cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
                supported = true;
            }
        }
        return supported ? VSConstants.S_OK : CommandNotSupported;
    }

    public int ExecCommand(uint itemid, ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var item = Item(itemid);
        if (pguidCmdGroup == VSConstants.GUID_VsUIHierarchyWindowCmds &&
            nCmdID == (uint)VSConstants.VsUIHierarchyWindowCmdIds.UIHWCMDID_RightClick && item != null)
            return ShowContextMenu(item, pvaIn);
        if (pguidCmdGroup == SmileProjectCommands.CommandSet && item != null)
        {
            try
            {
                return ExecuteProjectCommand(item, nCmdID);
            }
            catch (Exception exception)
            {
                ShowMessage(exception.Message, OLEMSGICON.OLEMSGICON_CRITICAL);
                ActivityLog.LogError(nameof(SmileProject), exception.ToString());
                return VSConstants.S_OK;
            }
        }
        if (Item(itemid)?.Kind != ItemKind.File || !IsOpenCommand(pguidCmdGroup, nCmdID))
            return CommandNotSupported;
        var logicalView = VSConstants.LOGVIEWID_Primary;
        return OpenItem(itemid, ref logicalView, IntPtr.Zero, out _);
    }

    private static bool IsOpenCommand(Guid commandGroup, uint commandId)
    {
        if (commandGroup == VSConstants.GUID_VsUIHierarchyWindowCmds)
            return commandId == (uint)VSConstants.VsUIHierarchyWindowCmdIds.UIHWCMDID_DoubleClick ||
                   commandId == (uint)VSConstants.VsUIHierarchyWindowCmdIds.UIHWCMDID_EnterKey;
        if (commandGroup != VSConstants.GUID_VSStandardCommandSet97)
            return false;
        return commandId == (uint)VSConstants.VSStd97CmdID.Open ||
               commandId == (uint)VSConstants.VSStd97CmdID.OpenProjectItem ||
               commandId == (uint)VSConstants.VSStd97CmdID.ViewCode;
    }

    public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        return QueryStatusCommand(_contextCommandItemId, ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
    }

    public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        return ExecCommand(_contextCommandItemId, ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
    }

    private int ShowContextMenu(ProjectItem item, IntPtr pointerToVariant)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
        if (shell == null)
            return VSConstants.E_FAIL;
        var menuId = item.Kind switch
        {
            ItemKind.Project => VsMenus.IDM_VS_CTXT_PROJNODE,
            ItemKind.Folder => VsMenus.IDM_VS_CTXT_FOLDERNODE,
            _ => VsMenus.IDM_VS_CTXT_ITEMNODE
        };
        var x = Cursor.Position.X;
        var y = Cursor.Position.Y;
        if (pointerToVariant != IntPtr.Zero)
        {
            try
            {
                var packed = Convert.ToUInt32(Marshal.GetObjectForNativeVariant(pointerToVariant));
                x = unchecked((short)(packed & 0xffff));
                y = unchecked((short)(packed >> 16));
            }
            catch (Exception exception) when (exception is InvalidCastException or FormatException or OverflowException)
            {
                ActivityLog.LogWarning(nameof(SmileProject), $"Visual Studio supplied invalid context-menu coordinates: {exception.Message}");
            }
        }

        var points = new[] { new POINTS { x = checked((short)x), y = checked((short)y) } };
        var menuGroup = VsMenus.guidSHLMainMenu;
        var previousItemId = _contextCommandItemId;
        _contextCommandItemId = item.Id;
        try
        {
            return shell.ShowContextMenu(0, ref menuGroup, menuId, points, this);
        }
        finally
        {
            _contextCommandItemId = previousItemId;
        }
    }

    private uint CommandStatus(ProjectItem item, uint commandId)
    {
        var supported = (uint)OLECMDF.OLECMDF_SUPPORTED;
        var enabled = supported | (uint)OLECMDF.OLECMDF_ENABLED;
        var invisible = supported | (uint)OLECMDF.OLECMDF_INVISIBLE;
        if (item.Kind == ItemKind.Project)
            return commandId is SmileProjectCommands.Build or SmileProjectCommands.Rebuild or SmileProjectCommands.Clean or
                SmileProjectCommands.AddNewSource or SmileProjectCommands.AddExistingSource or SmileProjectCommands.OpenProjectFolder
                ? enabled : invisible;
        if (item.Kind == ItemKind.Folder)
            return commandId == SmileProjectCommands.OpenFolder ? enabled : invisible;
        if (commandId == SmileProjectCommands.OpenContainingFolder)
            return enabled;
        if (!TryGetSource(item, out var source))
            return invisible;
        if (commandId == SmileProjectCommands.SetStartupSource)
            return source.IsStartup ? supported : enabled;
        if (commandId == SmileProjectCommands.IncludeSupportSource)
            return source.IsStartup || source.IsSupport ? supported : enabled;
        if (commandId == SmileProjectCommands.RemoveSource)
            return source.IsStartup ? supported : enabled;
        return invisible;
    }

    private int ExecuteProjectCommand(ProjectItem item, uint commandId)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (commandId == SmileProjectCommands.OpenProjectFolder)
            return OpenFolder(ProjectDirectory);
        if (commandId == SmileProjectCommands.OpenContainingFolder)
            return OpenFolder(Path.GetDirectoryName(item.Path)!);
        if (commandId == SmileProjectCommands.OpenFolder)
            return OpenFolder(item.Path);
        if (commandId == SmileProjectCommands.AddNewSource)
            return AddNewSource();
        if (commandId == SmileProjectCommands.AddExistingSource)
            return AddExistingSource();
        if (commandId is SmileProjectCommands.Build or SmileProjectCommands.Rebuild or SmileProjectCommands.Clean)
            return ExecuteBuildCommand(commandId);
        if (!TryGetSource(item, out _))
            return CommandNotSupported;
        if (commandId == SmileProjectCommands.SetStartupSource)
            SmileProjectFileEditor.SetStartup(ProjectPath, item.Path);
        else if (commandId == SmileProjectCommands.IncludeSupportSource)
            SmileProjectFileEditor.IncludeAsSupport(ProjectPath, item.Path);
        else if (commandId == SmileProjectCommands.RemoveSource)
            SmileProjectFileEditor.RemoveSource(ProjectPath, item.Path);
        else
            return CommandNotSupported;
        NotifyHierarchyChanged(ReadProject(captureHierarchy: true)!);
        return VSConstants.S_OK;
    }

    private int AddNewSource()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var name = Interaction.InputBox("Enter a name for the new support source file.",
            "Add New SMILE Source", "NewSource.smile").Trim();
        if (string.IsNullOrEmpty(name))
            return VSConstants.S_OK;
        if (string.IsNullOrEmpty(Path.GetExtension(name)))
            name += ".smile";
        if (!string.Equals(name, Path.GetFileName(name), StringComparison.Ordinal) ||
            !string.Equals(Path.GetExtension(name), ".smile", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Enter a file name ending in .smile without a directory path.");
        var sourcePath = Path.Combine(ProjectDirectory, name);
        if (File.Exists(sourcePath))
            throw new IOException($"A file named '{name}' already exists.");

        File.WriteAllText(sourcePath, "' Support declarations and routines for this project." + Environment.NewLine,
            new UTF8Encoding(false));
        try
        {
            SmileProjectFileEditor.AddSource(ProjectPath, sourcePath);
        }
        catch
        {
            File.Delete(sourcePath);
            throw;
        }
        var previousItems = ReadProject(captureHierarchy: true)!;
        var result = OpenPath(sourcePath);
        NotifyHierarchyChanged(previousItems);
        return result;
    }

    private int AddExistingSource()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        using var dialog = new OpenFileDialog
        {
            Title = "Add Existing SMILE Source",
            Filter = "SMILE source files (*.smile)|*.smile",
            InitialDirectory = ProjectDirectory,
            Multiselect = false,
            CheckFileExists = true
        };
        if (dialog.ShowDialog() != DialogResult.OK)
            return VSConstants.S_OK;

        var selectedPath = Path.GetFullPath(dialog.FileName);
        var projectPrefix = ProjectDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var sourcePath = selectedPath;
        var copied = false;
        if (!selectedPath.StartsWith(projectPrefix, StringComparison.OrdinalIgnoreCase))
        {
            sourcePath = Path.Combine(ProjectDirectory, Path.GetFileName(selectedPath));
            if (File.Exists(sourcePath))
                throw new IOException($"A project file named '{Path.GetFileName(sourcePath)}' already exists.");
            File.Copy(selectedPath, sourcePath);
            copied = true;
        }
        try
        {
            SmileProjectFileEditor.AddSource(ProjectPath, sourcePath);
        }
        catch
        {
            if (copied && File.Exists(sourcePath))
                File.Delete(sourcePath);
            throw;
        }
        var previousItems = ReadProject(captureHierarchy: true)!;
        var result = OpenPath(sourcePath);
        NotifyHierarchyChanged(previousItems);
        return result;
    }

    private int ExecuteBuildCommand(uint commandId)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
        var active = dte?.Solution?.SolutionBuild?.ActiveConfiguration;
        var configuration = active?.Name ?? "Debug";
        var platform = active?.SolutionContexts?.Count > 0
            ? active.SolutionContexts.Item(1).PlatformName
            : "Windows 64-bit .exe";
        var pane = SmileBuildService.GetOutputPane();
        var success = commandId switch
        {
            SmileProjectCommands.Clean => Clean(configuration, platform, pane),
            SmileProjectCommands.Rebuild => Clean(configuration, platform, pane) && Build(configuration, platform, pane),
            _ => Build(configuration, platform, pane)
        };
        return success ? VSConstants.S_OK : VSConstants.E_FAIL;
    }

    private int OpenPath(string path)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (ParseCanonicalName(path, out var itemId) != VSConstants.S_OK)
            return VSConstants.E_FAIL;
        var logicalView = VSConstants.LOGVIEWID_Primary;
        return OpenItem(itemId, ref logicalView, IntPtr.Zero, out _);
    }

    private static int OpenFolder(string path)
    {
        System.Diagnostics.Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        return VSConstants.S_OK;
    }

    private bool TryGetSource(ProjectItem item, out SmileProjectSourceItem source)
    {
        source = SourceSet.Items.FirstOrDefault(candidate =>
            string.Equals(candidate.FullPath, item.Path, StringComparison.OrdinalIgnoreCase))!;
        return source != null;
    }

    private bool IsSelectedStartup(ProjectItem item) =>
        item.Kind == ItemKind.File && string.Equals(item.Path, SourceSet.StartupSource.FullPath, StringComparison.OrdinalIgnoreCase);

    private void NotifyHierarchyChanged(IReadOnlyDictionary<uint, ProjectItem> previousItems)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        foreach (var sink in _events.Values.ToArray())
        {
            foreach (var removed in previousItems.Values
                         .Where(item => item.Kind != ItemKind.Project && !_items.ContainsKey(item.Id))
                         .OrderByDescending(item => item.Id))
                sink.OnItemDeleted(removed.Id);

            foreach (var added in _items.Values.Where(item => item.Kind != ItemKind.Project && !previousItems.ContainsKey(item.Id)))
            {
                var parent = _items[added.ParentId];
                var index = parent.Children.IndexOf(added.Id);
                var previousSibling = index <= 0 ? VSConstants.VSITEMID_NIL : parent.Children[index - 1];
                sink.OnPropertyChanged(parent.Id, (int)__VSHPROPID.VSHPROPID_FirstChild, 0);
                sink.OnPropertyChanged(parent.Id, (int)__VSHPROPID.VSHPROPID_FirstVisibleChild, 0);
                if (previousSibling != VSConstants.VSITEMID_NIL)
                {
                    sink.OnPropertyChanged(previousSibling, (int)__VSHPROPID.VSHPROPID_NextSibling, 0);
                    sink.OnPropertyChanged(previousSibling, (int)__VSHPROPID.VSHPROPID_NextVisibleSibling, 0);
                }
                sink.OnItemAdded(parent.Id, previousSibling, added.Id);
                sink.OnItemsAppended(parent.Id);
                sink.OnInvalidateItems(parent.Id);
            }

            foreach (var existing in _items.Values.Where(item => previousItems.ContainsKey(item.Id)))
                sink.OnPropertyChanged(existing.Id, (int)__VSHPROPID.VSHPROPID_Caption, 0);
            sink.OnInvalidateItems(VSConstants.VSITEMID_ROOT);
        }
    }

    private void ShowMessage(string message, OLEMSGICON icon) =>
        VsShellUtilities.ShowMessageBox(_package, message, "SMILE 2.0", icon,
            OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

    public int IsDocumentInProject(string pszMkDocument, out int pfFound, VSDOCUMENTPRIORITY[] pdwPriority, out uint pitemid)
    {
        var fullPath = Path.GetFullPath(pszMkDocument);
        var match = _items.Values.FirstOrDefault(item => item.Kind == ItemKind.File && string.Equals(Path.GetFullPath(item.Path), fullPath, StringComparison.OrdinalIgnoreCase));
        pfFound = match == null ? 0 : 1;
        pitemid = match?.Id ?? VSConstants.VSITEMID_NIL;
        if (pdwPriority != null && pdwPriority.Length != 0)
            pdwPriority[0] = match == null ? VSDOCUMENTPRIORITY.DP_Unsupported : VSDOCUMENTPRIORITY.DP_Standard;
        return VSConstants.S_OK;
    }

    public int GetMkDocument(uint itemid, out string pbstrMkDocument)
    {
        pbstrMkDocument = Item(itemid)?.Path ?? string.Empty;
        return string.IsNullOrEmpty(pbstrMkDocument) ? VSConstants.E_INVALIDARG : VSConstants.S_OK;
    }

    public int OpenItem(uint itemid, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        ppWindowFrame = null!;
        var item = Item(itemid);
        if (item == null || item.Kind != ItemKind.File)
            return VSConstants.E_INVALIDARG;

        try
        {
            var openDocument = Package.GetGlobalService(typeof(SVsUIShellOpenDocument)) as IVsUIShellOpenDocument;
            if (openDocument == null)
                return VSConstants.E_FAIL;

            var logicalView = VSConstants.LOGVIEWID_TextView;
            var openItemIds = new uint[1];
            var result = openDocument.IsDocumentOpen(
                this,
                itemid,
                item.Path,
                ref logicalView,
                (uint)__VSIDOFLAGS.IDO_ActivateIfOpen,
                out _,
                openItemIds,
                out ppWindowFrame,
                out var isOpen);
            if (ErrorHandler.Failed(result))
                return result;

            if (isOpen != 0)
            {
                ppWindowFrame.Show();
                return VSConstants.S_OK;
            }

            var editorType = VSConstants.GUID_TextEditorFactory;
            var site = _site ?? (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)_package;
            result = openDocument.OpenSpecificEditor(
                (uint)_VSRDTFLAGS.RDT_EditLock,
                item.Path,
                ref editorType,
                null!,
                ref logicalView,
                item.Caption,
                this,
                itemid,
                punkDocDataExisting,
                site,
                out ppWindowFrame);
            if (ErrorHandler.Failed(result))
                return result;

            ppWindowFrame.Show();
            return result;
        }
        catch (Exception exception)
        {
            ActivityLog.LogError(nameof(SmileProject), exception.ToString());
            return Marshal.GetHRForException(exception);
        }
    }

    public int GetItemContext(uint itemid, out Microsoft.VisualStudio.OLE.Interop.IServiceProvider ppSP)
    { ppSP = _site!; return _site == null ? VSConstants.E_FAIL : VSConstants.S_OK; }
    public int GetCfgProvider(out IVsCfgProvider ppCfgProvider)
    { ppCfgProvider = _configurationProvider!; return _configurationProvider == null ? VSConstants.E_FAIL : VSConstants.S_OK; }
    public int GenerateUniqueItemName(uint itemidLoc, string pszExt, string pszSuggestedRoot, out string pbstrItemName)
    { pbstrItemName = pszSuggestedRoot + pszExt; return VSConstants.S_OK; }
    public int AddItem(uint itemidLoc, VSADDITEMOPERATION dwAddItemOperation, string pszItemName, uint cFilesToOpen,
        string[] rgpszFilesToOpen, IntPtr hwndDlgOwner, VSADDRESULT[] pResult)
    { if (pResult.Length != 0) pResult[0] = VSADDRESULT.ADDRESULT_Failure; return VSConstants.E_NOTIMPL; }
    public int RemoveItem(uint dwReserved, uint itemid, out int pfResult)
    { pfResult = 0; return VSConstants.E_NOTIMPL; }
    public int ReopenItem(uint itemid, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidLogicalView,
        IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame)
    { ThreadHelper.ThrowIfNotOnUIThread(); return OpenItem(itemid, ref rguidLogicalView, punkDocDataExisting, out ppWindowFrame); }

    public int GetClassID(out Guid pClassID) { pClassID = new Guid(SmileProjectFactory.SmileProjectTypeGuidString); return VSConstants.S_OK; }
    public int IsDirty(out int pfIsDirty) { pfIsDirty = 0; return VSConstants.S_OK; }
    public int InitNew(uint nFormatIndex) => VSConstants.S_OK;
    public int Load(string pszFilename, uint grfMode, int fReadOnly)
    { ThreadHelper.ThrowIfNotOnUIThread(); ProjectPath = Path.GetFullPath(pszFilename); ReadProject(); return VSConstants.S_OK; }
    public int Save(string pszFilename, int fRemember, uint nFormatIndex) => VSConstants.S_OK;
    public int SaveCompleted(string pszFilename) => VSConstants.S_OK;
    public int GetCurFile(out string ppszFilename, out uint pnFormatIndex)
    { ppszFilename = ProjectPath; pnFormatIndex = 0; return VSConstants.S_OK; }
    public int GetFormatList(out string ppszFormatList)
    { ppszFormatList = "SMILE Project Files (*.smileproj)\n*.smileproj\n"; return VSConstants.S_OK; }

    public int IsItemDirty(uint itemid, IntPtr punkDocData, out int pfDirty)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        pfDirty = 0;
        if (Item(itemid)?.Kind != ItemKind.File || punkDocData == IntPtr.Zero)
            return VSConstants.E_INVALIDARG;

        try
        {
            return (Marshal.GetObjectForIUnknown(punkDocData) as IVsPersistDocData)?.IsDocDataDirty(out pfDirty)
                ?? VSConstants.E_NOINTERFACE;
        }
        catch (Exception exception)
        {
            return Marshal.GetHRForException(exception);
        }
    }

    public int SaveItem(VSSAVEFLAGS dwSave, string pszSilentSaveAsName, uint itemid, IntPtr punkDocData, out int pfCanceled)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        pfCanceled = 0;
        if (Item(itemid)?.Kind != ItemKind.File || punkDocData == IntPtr.Zero)
            return VSConstants.E_INVALIDARG;

        try
        {
            var persistDocData = Marshal.GetObjectForIUnknown(punkDocData) as IVsPersistDocData;
            return persistDocData?.SaveDocData(dwSave, out _, out pfCanceled) ?? VSConstants.E_NOINTERFACE;
        }
        catch (Exception exception)
        {
            return Marshal.GetHRForException(exception);
        }
    }

    public int IsItemReloadable(uint itemid, out int pfReloadable)
    {
        pfReloadable = 0;
        return VSConstants.S_OK;
    }

    public int ReloadItem(uint itemid, uint dwReserved) => VSConstants.E_NOTIMPL;
    public int IgnoreItemFileChanges(uint itemid, int fIgnore) => VSConstants.S_OK;

    private sealed class ProjectItem
    {
        public ProjectItem(uint id, string caption, string path, ItemKind kind, uint parentId)
        { Id = id; Caption = caption; Path = path; Kind = kind; ParentId = parentId; }
        public uint Id { get; }
        public string Caption { get; }
        public string Path { get; }
        public ItemKind Kind { get; }
        public uint ParentId { get; }
        public List<uint> Children { get; } = new();
    }

    private enum ItemKind { Project, Folder, File }
}

internal sealed class SmileConfigurationProvider : IVsCfgProvider2, IVsProjectCfgProvider
{
    private const string NativePlatformName = "Windows 64-bit .exe";
    private static readonly string[] ConfigurationNames = { "Debug", "Release" };
    private static readonly string[] PlatformNames = { NativePlatformName, "Web" };
    private readonly SmileProject _project;
    private readonly Dictionary<string, SmileProjectConfiguration> _configurations;

    public SmileConfigurationProvider(SmileProject project)
    {
        _project = project;
        _configurations = new Dictionary<string, SmileProjectConfiguration>(StringComparer.OrdinalIgnoreCase);
        foreach (var platform in PlatformNames)
        foreach (var configuration in ConfigurationNames)
            _configurations[$"{configuration}|{platform}"] = new SmileProjectConfiguration(this, project, configuration, platform);
    }

    public int GetCfgs(uint celt, IVsCfg[] rgpcfg, uint[] pcActual, uint[] prgfFlags)
    {
        var values = _configurations.Values.Cast<IVsCfg>().ToArray();
        if (pcActual != null && pcActual.Length != 0) pcActual[0] = (uint)values.Length;
        for (var index = 0; index < Math.Min((int)celt, values.Length); index++) rgpcfg[index] = values[index];
        return VSConstants.S_OK;
    }

    public int GetCfgNames(uint celt, string[] rgbstr, uint[] pcActual)
    {
        var names = ConfigurationNames;
        if (pcActual != null && pcActual.Length != 0) pcActual[0] = (uint)names.Length;
        for (var index = 0; index < Math.Min((int)celt, names.Length); index++) rgbstr[index] = names[index];
        return VSConstants.S_OK;
    }

    public int GetPlatformNames(uint celt, string[] rgbstr, uint[] pcActual) => Names(PlatformNames, celt, rgbstr, pcActual);
    public int GetSupportedPlatformNames(uint celt, string[] rgbstr, uint[] pcActual) => Names(PlatformNames, celt, rgbstr, pcActual);

    public int GetCfgOfName(string pszCfgName, string pszPlatformName, out IVsCfg ppCfg)
    {
        var platform = NormalizePlatform(pszPlatformName);
        _configurations.TryGetValue($"{pszCfgName}|{platform}", out var configuration);
        ppCfg = configuration!;
        return configuration == null ? VSConstants.E_INVALIDARG : VSConstants.S_OK;
    }

    public int OpenProjectCfg(string pszProjectCfgCanonicalName, out IVsProjectCfg ppIVsProjectCfg)
    {
        var parts = pszProjectCfgCanonicalName.Split('|');
        var platform = NormalizePlatform(parts.Length > 1 ? parts[1] : null);
        _configurations.TryGetValue($"{parts[0]}|{platform}", out var configuration);
        ppIVsProjectCfg = configuration!;
        return configuration == null ? VSConstants.E_INVALIDARG : VSConstants.S_OK;
    }

    public int get_UsesIndependentConfigurations(out int pfUsesIndependentConfigurations)
    { pfUsesIndependentConfigurations = 1; return VSConstants.S_OK; }
    public int GetCfgProviderProperty(int propid, out object pvar) { pvar = false; return VSConstants.S_OK; }
    public int AdviseCfgProviderEvents(IVsCfgProviderEvents pCPE, out uint pdwCookie) { pdwCookie = 0; return VSConstants.S_OK; }
    public int UnadviseCfgProviderEvents(uint dwCookie) => VSConstants.S_OK;
    public int AddCfgsOfCfgName(string pszCfgName, string pszCloneCfgName, int fPrivate) => VSConstants.E_NOTIMPL;
    public int DeleteCfgsOfCfgName(string pszCfgName) => VSConstants.E_NOTIMPL;
    public int RenameCfgsOfCfgName(string pszOldName, string pszNewName) => VSConstants.E_NOTIMPL;
    public int AddCfgsOfPlatformName(string pszPlatformName, string pszClonePlatformName) => VSConstants.E_NOTIMPL;
    public int DeleteCfgsOfPlatformName(string pszPlatformName) => VSConstants.E_NOTIMPL;

    private static string NormalizePlatform(string? platform) =>
        string.IsNullOrWhiteSpace(platform) ||
        string.Equals(platform, "Default", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(platform, "x64", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(platform, "windows-x64", StringComparison.OrdinalIgnoreCase)
            ? NativePlatformName
            : platform!;

    private static int Names(string[] names, uint celt, string[] values, uint[] actual)
    {
        if (actual != null && actual.Length != 0) actual[0] = (uint)names.Length;
        for (var index = 0; index < Math.Min((int)celt, names.Length); index++) values[index] = names[index];
        return VSConstants.S_OK;
    }
}

internal sealed class SmileProjectConfiguration : IVsProjectCfg2, IVsBuildableProjectCfg, IVsDebuggableProjectCfg
{
    private readonly SmileConfigurationProvider _provider;
    private readonly SmileProject _project;
    private readonly string _configuration;
    private readonly string _platform;
    private readonly Dictionary<uint, IVsBuildStatusCallback> _callbacks = new();
    private uint _nextCookie = 1;
    private bool _building;

    public SmileProjectConfiguration(SmileConfigurationProvider provider, SmileProject project, string configuration, string platform)
    { _provider = provider; _project = project; _configuration = configuration; _platform = platform; }

    public int get_DisplayName(out string pbstrDisplayName) { pbstrDisplayName = _configuration; return VSConstants.S_OK; }
    public int get_IsDebugOnly(out int pfIsDebugOnly) { pfIsDebugOnly = 0; return VSConstants.S_OK; }
    public int get_IsReleaseOnly(out int pfIsReleaseOnly) { pfIsReleaseOnly = 0; return VSConstants.S_OK; }
    public int get_CanonicalName(out string pbstrCanonicalName) { pbstrCanonicalName = _configuration + "|" + _platform; return VSConstants.S_OK; }
    public int get_Platform(out Guid pguidPlatform) { pguidPlatform = Guid.Empty; return VSConstants.S_OK; }
    public int get_IsPackaged(out int pfIsPackaged) { pfIsPackaged = 0; return VSConstants.S_OK; }
    public int get_IsSpecifyingOutputSupported(out int pfIsSpecifyingOutputSupported) { pfIsSpecifyingOutputSupported = 0; return VSConstants.S_OK; }
    public int get_TargetCodePage(out uint puiTargetCodePage) { puiTargetCodePage = 65001; return VSConstants.S_OK; }
    public int get_UpdateSequenceNumber(ULARGE_INTEGER[] puliUSN) { if (puliUSN.Length != 0) puliUSN[0].QuadPart = 0; return VSConstants.S_OK; }
    public int get_RootURL(out string pbstrRootURL) { pbstrRootURL = new Uri(_project.ProjectDirectory + Path.DirectorySeparatorChar).AbsoluteUri; return VSConstants.S_OK; }
    public int get_BuildableProjectCfg(out IVsBuildableProjectCfg ppIVsBuildableProjectCfg) { ppIVsBuildableProjectCfg = this; return VSConstants.S_OK; }
    public int get_ProjectCfgProvider(out IVsProjectCfgProvider ppIVsProjectCfgProvider) { ppIVsProjectCfgProvider = _provider; return VSConstants.S_OK; }
    public int EnumOutputs(out IVsEnumOutputs ppIVsEnumOutputs) { ppIVsEnumOutputs = null!; return VSConstants.E_NOTIMPL; }
    public int OpenOutput(string szOutputCanonicalName, out IVsOutput ppIVsOutput) { ppIVsOutput = null!; return VSConstants.E_NOTIMPL; }
    public int get_IsPrivate(out int pfIsPrivate) { pfIsPrivate = 0; return VSConstants.S_OK; }
    public int get_VirtualRoot(out string pbstrVRoot)
    { pbstrVRoot = new Uri(_project.ProjectDirectory + Path.DirectorySeparatorChar).AbsoluteUri; return VSConstants.S_OK; }
    public int OutputsRequireAppRoot(out int pfRequiresAppRoot) { pfRequiresAppRoot = 0; return VSConstants.S_OK; }
    public int get_OutputGroups(uint celt, IVsOutputGroup[] rgpcfg, uint[] pcActual) { if (pcActual.Length != 0) pcActual[0] = 0; return VSConstants.S_OK; }
    public int OpenOutputGroup(string szCanonicalName, out IVsOutputGroup ppIVsOutputGroup) { ppIVsOutputGroup = null!; return VSConstants.E_NOTIMPL; }

    public int get_CfgType(ref Guid iidCfg, out IntPtr ppCfg)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        ppCfg = IntPtr.Zero;
        if (iidCfg != typeof(IVsBuildableProjectCfg).GUID && iidCfg != typeof(IVsDebuggableProjectCfg).GUID && iidCfg != typeof(IVsProjectCfg).GUID)
            return VSConstants.E_NOINTERFACE;
        ppCfg = Marshal.GetComInterfaceForObject(this, iidCfg == typeof(IVsBuildableProjectCfg).GUID
            ? typeof(IVsBuildableProjectCfg)
            : iidCfg == typeof(IVsDebuggableProjectCfg).GUID ? typeof(IVsDebuggableProjectCfg) : typeof(IVsProjectCfg));
        return VSConstants.S_OK;
    }

    public int QueryStartBuild(uint dwOptions, int[] pfSupported, int[] pfReady) => QueryStart(pfSupported, pfReady);
    public int QueryStartClean(uint dwOptions, int[] pfSupported, int[] pfReady) => QueryStart(pfSupported, pfReady);
    public int QueryStartUpToDateCheck(uint dwOptions, int[] pfSupported, int[] pfReady) => QueryStart(pfSupported, pfReady);
    public int StartBuild(IVsOutputWindowPane pIVsOutputWindowPane, uint dwOptions)
    { ThreadHelper.ThrowIfNotOnUIThread(); return RunBuild(pIVsOutputWindowPane, clean: false); }
    public int StartClean(IVsOutputWindowPane pIVsOutputWindowPane, uint dwOptions)
    { ThreadHelper.ThrowIfNotOnUIThread(); return RunBuild(pIVsOutputWindowPane, clean: true); }
    public int StartUpToDateCheck(IVsOutputWindowPane pIVsOutputWindowPane, uint dwOptions)
    { ThreadHelper.ThrowIfNotOnUIThread(); return RunBuild(pIVsOutputWindowPane, clean: false); }
    public int QueryStatus(out int pfBuildDone) { pfBuildDone = _building ? 0 : 1; return VSConstants.S_OK; }
    public int Stop(int fSync) { _building = false; return VSConstants.S_OK; }
    public int Wait(uint dwMilliseconds, int fTickWhenMessageQNotEmpty) => VSConstants.S_OK;
    public int get_ProjectCfg(out IVsProjectCfg ppIVsProjectCfg) { ppIVsProjectCfg = this; return VSConstants.S_OK; }

    public int AdviseBuildStatusCallback(IVsBuildStatusCallback pIVsBuildStatusCallback, out uint pdwCookie)
    { pdwCookie = _nextCookie++; _callbacks[pdwCookie] = pIVsBuildStatusCallback; return VSConstants.S_OK; }
    public int UnadviseBuildStatusCallback(uint dwCookie) { _callbacks.Remove(dwCookie); return VSConstants.S_OK; }

    public int QueryDebugLaunch(uint grfLaunch, out int pfCanLaunch)
    {
        var output = _platform.Equals("Web", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(_project.GetWebOutputDirectory(_configuration), "index.html")
            : _project.GetOutputPath(_configuration);
        pfCanLaunch = File.Exists(output) || File.Exists(Path.Combine(_project.ProjectDirectory, _project.StartupFile)) ? 1 : 0;
        return VSConstants.S_OK;
    }
    public int DebugLaunch(uint grfLaunch)
    { ThreadHelper.ThrowIfNotOnUIThread(); return _project.Launch(_configuration, _platform, grfLaunch) ? VSConstants.S_OK : VSConstants.E_FAIL; }

    private int RunBuild(IVsOutputWindowPane pane, bool clean)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        _building = true;
        var continueBuild = 1;
        foreach (var callback in _callbacks.Values) callback.BuildBegin(ref continueBuild);
        var success = continueBuild != 0 && (clean
            ? _project.Clean(_configuration, _platform, pane)
            : _project.Build(_configuration, _platform, pane));
        _building = false;
        foreach (var callback in _callbacks.Values) callback.BuildEnd(success ? 1 : 0);
        return success ? VSConstants.S_OK : VSConstants.E_FAIL;
    }

    private int QueryStart(int[] supported, int[] ready)
    {
        if (supported != null && supported.Length != 0) supported[0] = 1;
        if (ready != null && ready.Length != 0) ready[0] = _building ? 0 : 1;
        return VSConstants.S_OK;
    }
}
