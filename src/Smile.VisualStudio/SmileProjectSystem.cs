using System;
using System.Collections.Generic;
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

internal sealed class SmileProject : IVsUIHierarchy, IVsProject2, IVsGetCfgProvider, IPersistFileFormat
{
    private const int CommandNotSupported = unchecked((int)0x80040100);

    private readonly SmilePackage _package;
    private readonly Dictionary<uint, ProjectItem> _items = new();
    private readonly Dictionary<uint, IVsHierarchyEvents> _events = new();
    private readonly Dictionary<string, string> _editorPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly Guid _projectGuid;
    private uint _nextEventCookie = 1;
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
    public string OutputName { get; private set; } = "Program";
    public SmileGraphicsBackend GraphicsBackend { get; private set; } = SmileGraphicsBackend.Auto;
    public bool VSync { get; private set; } = true;
    public IReadOnlyList<string> AssetIncludes { get; private set; } = Array.Empty<string>();

    public string GetOutputPath(string configuration) =>
        Path.Combine(ProjectDirectory, "bin", NormalizeConfiguration(configuration), SafeFileName(OutputName) + ".exe");

    public bool Build(string configuration, IVsOutputWindowPane? pane)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        pane ??= SmileBuildService.GetOutputPane();
        pane.Clear();
        pane.Activate();

        var sourcePath = Path.GetFullPath(Path.Combine(ProjectDirectory, StartupFile));
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

        var emitDebugInformation = NormalizeConfiguration(configuration) == "Debug";
        var compilerSourcePath = GetCompilerSourcePath(sourcePath, emitDebugInformation);
        var outputPath = GetOutputPath(configuration);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        pane.OutputStringThreadSafe($"> \"{compilerPath}\" \"{compilerSourcePath}\" -o \"{outputPath}\" --graphics {GraphicsBackend} --vsync {VSync.ToString().ToLowerInvariant()}{(emitDebugInformation ? " --debug" : string.Empty)}\r\n");
        var result = ThreadHelper.JoinableTaskFactory.Run(() => SmileBuildService.RunAsync(
            compilerPath, compilerSourcePath, outputPath, GraphicsBackend, VSync, emitDebugInformation));
        if (!string.IsNullOrEmpty(result.Output))
            pane.OutputStringThreadSafe(SmileBuildService.NormalizeOutput(result.Output));
        SmileBuildService.ReportDiagnostics(result.Output);

        if (result.ExitCode != 0)
        {
            pane.OutputStringThreadSafe($"SMILE build failed with exit code {result.ExitCode}.\r\n");
            return false;
        }

        CopyAssets(Path.GetDirectoryName(outputPath)!);
        pane.OutputStringThreadSafe($"SMILE build succeeded: {outputPath}\r\n");
        return true;
    }

    public bool Clean(string configuration, IVsOutputWindowPane? pane)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        pane ??= SmileBuildService.GetOutputPane();
        var directory = Path.Combine(ProjectDirectory, "bin", NormalizeConfiguration(configuration));
        try
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
            pane.OutputStringThreadSafe($"Cleaned {directory}\r\n");
            return true;
        }
        catch (Exception exception)
        {
            pane.OutputStringThreadSafe($"Could not clean {directory}: {exception.Message}\r\n");
            return false;
        }
    }

    private string GetCompilerSourcePath(string projectSourcePath, bool emitDebugInformation)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (!emitDebugInformation)
            return projectSourcePath;

        var fullProjectPath = Path.GetFullPath(projectSourcePath);
        if (_editorPaths.TryGetValue(fullProjectPath, out var editorPath) && File.Exists(editorPath))
            return editorPath;

        try
        {
            var dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            var document = dte?.ActiveDocument;
            var activePath = document?.FullName;
            if (activePath != null &&
                activePath.EndsWith(".smile", StringComparison.OrdinalIgnoreCase) &&
                File.Exists(activePath))
            {
                document!.Save();
                activePath = Path.GetFullPath(activePath);
                _editorPaths[fullProjectPath] = activePath;
                return activePath;
            }
        }
        catch (Exception exception)
        {
            ActivityLog.LogWarning(nameof(SmileProject), $"Could not resolve the active SMILE editor path: {exception.Message}");
        }

        return projectSourcePath;
    }

    public bool Launch(string configuration, uint launchFlags)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var outputPath = GetOutputPath(configuration);
        if (!File.Exists(outputPath) && !Build(configuration, null))
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

    private void ReadProject()
    {
        var document = XDocument.Load(ProjectPath, LoadOptions.SetLineInfo);
        var root = document.Root;
        if (root == null || root.Name.LocalName != "SmileProject")
            throw new InvalidDataException("A .smileproj file must have a SmileProject root element.");

        var properties = root.Elements().FirstOrDefault(element => element.Name.LocalName == "PropertyGroup");
        ProjectKind = Value(properties, "ProjectKind", "Console");
        StartupFile = Value(properties, "StartupFile", "Program.smile");
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
        _configurationProvider = new SmileConfigurationProvider(this);
    }

    private void BuildHierarchy(XElement root)
    {
        _items.Clear();
        var rootNode = new ProjectItem(VSConstants.VSITEMID_ROOT, ProjectName, ProjectPath, ItemKind.Project, 0);
        _items[rootNode.Id] = rootNode;
        uint nextId = 1;

        var sources = root.Elements().Where(element => element.Name.LocalName == "ItemGroup")
            .SelectMany(element => element.Elements().Where(item => item.Name.LocalName == "SmileSource"))
            .Select(item => (string?)item.Attribute("Include") ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();
        if (sources.Length == 0)
            sources = new[] { StartupFile };
        foreach (var source in sources)
            AddNode(rootNode, ref nextId, Path.GetFileName(source), Path.GetFullPath(Path.Combine(ProjectDirectory, source)), ItemKind.File);

        if (ProjectKind.Equals("Game", StringComparison.OrdinalIgnoreCase) || AssetIncludes.Count != 0)
        {
            var assetsPath = Path.Combine(ProjectDirectory, "Assets");
            Directory.CreateDirectory(assetsPath);
            var assets = AddNode(rootNode, ref nextId, "Assets", assetsPath, ItemKind.Folder);
            AddDirectoryChildren(assets, assetsPath, ref nextId);
        }
    }

    private ProjectItem AddNode(ProjectItem parent, ref uint nextId, string caption, string path, ItemKind kind)
    {
        var node = new ProjectItem(nextId++, caption, path, kind, parent.Id);
        parent.Children.Add(node.Id);
        _items[node.Id] = node;
        return node;
    }

    private void AddDirectoryChildren(ProjectItem parent, string directory, ref uint nextId)
    {
        foreach (var childDirectory in Directory.EnumerateDirectories(directory).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var child = AddNode(parent, ref nextId, Path.GetFileName(childDirectory), childDirectory, ItemKind.Folder);
            AddDirectoryChildren(child, childDirectory, ref nextId);
        }
        foreach (var file in Directory.EnumerateFiles(directory).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            AddNode(parent, ref nextId, Path.GetFileName(file), file, ItemKind.File);
    }

    private static string Value(XElement? group, string name, string fallback) =>
        group?.Elements().FirstOrDefault(element => element.Name.LocalName == name)?.Value.Trim() is { Length: > 0 } value ? value : fallback;

    private static string NormalizeConfiguration(string value) =>
        value.StartsWith("Release", StringComparison.OrdinalIgnoreCase) ? "Release" : "Debug";

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
        else if (propid == (int)__VSHPROPID.VSHPROPID_Caption || propid == (int)__VSHPROPID.VSHPROPID_Name)
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
    public int Close() { _events.Clear(); return VSConstants.S_OK; }
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

        // The built-in editor creates a temporary moniker for this lightweight
        // hierarchy. Remember that exact path so Debug symbols match the document
        // where Visual Studio places the breakpoint.
        try
        {
            VsShellUtilities.OpenAsMiscellaneousFile(_package, item.Path, item.Caption,
                VSConstants.GUID_TextEditorFactory, null!, VSConstants.LOGVIEWID_TextView);

            var dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            var editorPath = dte?.ActiveDocument?.FullName;
            if (!string.IsNullOrWhiteSpace(editorPath) && File.Exists(editorPath))
                _editorPaths[Path.GetFullPath(item.Path)] = Path.GetFullPath(editorPath);

            if (VsShellUtilities.IsDocumentOpen(_package, item.Path, Guid.Empty,
                    out _, out _, out ppWindowFrame))
            {
                ppWindowFrame.Show();
            }

            return VSConstants.S_OK;
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
    { ProjectPath = Path.GetFullPath(pszFilename); ReadProject(); return VSConstants.S_OK; }
    public int Save(string pszFilename, int fRemember, uint nFormatIndex) => VSConstants.S_OK;
    public int SaveCompleted(string pszFilename) => VSConstants.S_OK;
    public int GetCurFile(out string ppszFilename, out uint pnFormatIndex)
    { ppszFilename = ProjectPath; pnFormatIndex = 0; return VSConstants.S_OK; }
    public int GetFormatList(out string ppszFormatList)
    { ppszFormatList = "SMILE Project Files (*.smileproj)\n*.smileproj\n"; return VSConstants.S_OK; }

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
    private readonly SmileProject _project;
    private readonly Dictionary<string, SmileProjectConfiguration> _configurations;

    public SmileConfigurationProvider(SmileProject project)
    {
        _project = project;
        _configurations = new Dictionary<string, SmileProjectConfiguration>(StringComparer.OrdinalIgnoreCase)
        {
            ["Debug"] = new SmileProjectConfiguration(this, project, "Debug"),
            ["Release"] = new SmileProjectConfiguration(this, project, "Release")
        };
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
        var names = _configurations.Keys.ToArray();
        if (pcActual != null && pcActual.Length != 0) pcActual[0] = (uint)names.Length;
        for (var index = 0; index < Math.Min((int)celt, names.Length); index++) rgbstr[index] = names[index];
        return VSConstants.S_OK;
    }

    public int GetPlatformNames(uint celt, string[] rgbstr, uint[] pcActual) => OneName("x64", celt, rgbstr, pcActual);
    public int GetSupportedPlatformNames(uint celt, string[] rgbstr, uint[] pcActual) => OneName("x64", celt, rgbstr, pcActual);

    public int GetCfgOfName(string pszCfgName, string pszPlatformName, out IVsCfg ppCfg)
    {
        _configurations.TryGetValue(pszCfgName, out var configuration);
        ppCfg = configuration!;
        return configuration == null ? VSConstants.E_INVALIDARG : VSConstants.S_OK;
    }

    public int OpenProjectCfg(string pszProjectCfgCanonicalName, out IVsProjectCfg ppIVsProjectCfg)
    {
        var name = pszProjectCfgCanonicalName.Split('|')[0];
        _configurations.TryGetValue(name, out var configuration);
        ppIVsProjectCfg = configuration!;
        return configuration == null ? VSConstants.E_INVALIDARG : VSConstants.S_OK;
    }

    public int get_UsesIndependentConfigurations(out int pfUsesIndependentConfigurations)
    { pfUsesIndependentConfigurations = 0; return VSConstants.S_OK; }
    public int GetCfgProviderProperty(int propid, out object pvar) { pvar = false; return VSConstants.S_OK; }
    public int AdviseCfgProviderEvents(IVsCfgProviderEvents pCPE, out uint pdwCookie) { pdwCookie = 0; return VSConstants.S_OK; }
    public int UnadviseCfgProviderEvents(uint dwCookie) => VSConstants.S_OK;
    public int AddCfgsOfCfgName(string pszCfgName, string pszCloneCfgName, int fPrivate) => VSConstants.E_NOTIMPL;
    public int DeleteCfgsOfCfgName(string pszCfgName) => VSConstants.E_NOTIMPL;
    public int RenameCfgsOfCfgName(string pszOldName, string pszNewName) => VSConstants.E_NOTIMPL;
    public int AddCfgsOfPlatformName(string pszPlatformName, string pszClonePlatformName) => VSConstants.E_NOTIMPL;
    public int DeleteCfgsOfPlatformName(string pszPlatformName) => VSConstants.E_NOTIMPL;

    private static int OneName(string value, uint celt, string[] values, uint[] actual)
    {
        if (actual != null && actual.Length != 0) actual[0] = 1;
        if (celt != 0 && values != null && values.Length != 0) values[0] = value;
        return VSConstants.S_OK;
    }
}

internal sealed class SmileProjectConfiguration : IVsProjectCfg2, IVsBuildableProjectCfg, IVsDebuggableProjectCfg
{
    private readonly SmileConfigurationProvider _provider;
    private readonly SmileProject _project;
    private readonly string _name;
    private readonly Dictionary<uint, IVsBuildStatusCallback> _callbacks = new();
    private uint _nextCookie = 1;
    private bool _building;

    public SmileProjectConfiguration(SmileConfigurationProvider provider, SmileProject project, string name)
    { _provider = provider; _project = project; _name = name; }

    public int get_DisplayName(out string pbstrDisplayName) { pbstrDisplayName = _name; return VSConstants.S_OK; }
    public int get_IsDebugOnly(out int pfIsDebugOnly) { pfIsDebugOnly = 0; return VSConstants.S_OK; }
    public int get_IsReleaseOnly(out int pfIsReleaseOnly) { pfIsReleaseOnly = 0; return VSConstants.S_OK; }
    public int get_CanonicalName(out string pbstrCanonicalName) { pbstrCanonicalName = _name + "|x64"; return VSConstants.S_OK; }
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
    { pfCanLaunch = File.Exists(_project.GetOutputPath(_name)) || File.Exists(Path.Combine(_project.ProjectDirectory, _project.StartupFile)) ? 1 : 0; return VSConstants.S_OK; }
    public int DebugLaunch(uint grfLaunch)
    { ThreadHelper.ThrowIfNotOnUIThread(); return _project.Launch(_name, grfLaunch) ? VSConstants.S_OK : VSConstants.E_FAIL; }

    private int RunBuild(IVsOutputWindowPane pane, bool clean)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        _building = true;
        var continueBuild = 1;
        foreach (var callback in _callbacks.Values) callback.BuildBegin(ref continueBuild);
        var success = continueBuild != 0 && (clean ? _project.Clean(_name, pane) : _project.Build(_name, pane));
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
