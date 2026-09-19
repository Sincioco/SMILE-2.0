using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Linq;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Smile.Language;

namespace Smile.VisualStudio;

// The shell owns menu labels, icons, Compare With and Git operations. This owner
// supplies the file operations a custom project hierarchy must implement itself.
internal sealed partial class SmileProject : IVsHierarchyDeleteHandler
{
    private const string CutProjectFormat = "SMILE.SourceCutProject";

    public int QueryDeleteItem(uint operation, uint itemId, out int canDelete)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var item = Item(itemId);
        var command = new[] { new OLECMD { cmdID = (uint)VSConstants.VSStd97CmdID.Delete } };
        if (item != null) QueryFileCommands(item, VSConstants.GUID_VSStandardCommandSet97, 1, command);
        canDelete = (command[0].cmdf & (uint)OLECMDF.OLECMDF_ENABLED) != 0 ? 1 : 0;
        return VSConstants.S_OK;
    }

    public int DeleteItem(uint operation, uint itemId)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var item = Item(itemId);
        if (item == null) return VSConstants.E_INVALIDARG;
        try
        {
            // VS DELITEMOP_RemoveFromProject is 2; DeleteFromStorage is 1.
            return operation == 2 ? ExecuteProjectCommand(item, SmileProjectCommands.RemoveSource) :
                ExecuteFileCommand(item, VSConstants.GUID_VSStandardCommandSet97, (uint)VSConstants.VSStd97CmdID.Delete);
        }
        catch (Exception exception)
        {
            ShowMessage(exception.Message, OLEMSGICON.OLEMSGICON_CRITICAL);
            return Marshal.GetHRForException(exception);
        }
    }

    private static bool IsFileCommand(Guid group, uint command) =>
        group == VSConstants.GUID_VSStandardCommandSet97 &&
            command is (uint)VSConstants.VSStd97CmdID.Cut or (uint)VSConstants.VSStd97CmdID.Copy or
                (uint)VSConstants.VSStd97CmdID.Paste or (uint)VSConstants.VSStd97CmdID.Delete or
                (uint)VSConstants.VSStd97CmdID.Rename ||
        group == VSConstants.VSStd2K && command == (uint)VSConstants.VSStd2KCmdID.CopyFullPathName;

    private bool QueryFileCommands(ProjectItem item, Guid group, uint count, OLECMD[] commands)
    {
        var handled = false;
        for (var index = 0; index < Math.Min((int)count, commands.Length); index++)
        {
            var command = commands[index].cmdID;
            if (!IsFileCommand(group, command)) continue;
            var enabled = item.Kind == ItemKind.File && item.Exists;
            if (group == VSConstants.GUID_VSStandardCommandSet97)
            {
                if (command == (uint)VSConstants.VSStd97CmdID.Paste)
                    enabled = item.Kind is ItemKind.Project or ItemKind.Folder && Clipboard.ContainsFileDropList();
                else if (command != (uint)VSConstants.VSStd97CmdID.Copy)
                    enabled = enabled && item.Path.StartsWith(ProjectDirectory + Path.DirectorySeparatorChar,
                        StringComparison.OrdinalIgnoreCase) && TryGetSource(item, out var source) &&
                        (command == (uint)VSConstants.VSStd97CmdID.Rename || !source.IsStartup);
            }
            commands[index].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED |
                (enabled ? (uint)OLECMDF.OLECMDF_ENABLED : 0);
            handled = true;
        }
        return handled;
    }

    private int ExecuteFileCommand(ProjectItem item, Guid group, uint command)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var status = new[] { new OLECMD { cmdID = command } };
        QueryFileCommands(item, group, 1, status);
        if ((status[0].cmdf & (uint)OLECMDF.OLECMDF_ENABLED) == 0) return CommandNotSupported;
        if (group == VSConstants.VSStd2K)
            Clipboard.SetText(item.Path);
        else if (command == (uint)VSConstants.VSStd97CmdID.Copy || command == (uint)VSConstants.VSStd97CmdID.Cut)
        {
            var data = new DataObject();
            data.SetFileDropList(new StringCollection { item.Path });
            if (command == (uint)VSConstants.VSStd97CmdID.Cut)
                data.SetData(CutProjectFormat, ProjectPath);
            Clipboard.SetDataObject(data, true);
        }
        else if (command == (uint)VSConstants.VSStd97CmdID.Rename)
            RefreshSolutionExplorer(item.Id, editLabel: true);
        else if (command == (uint)VSConstants.VSStd97CmdID.Delete)
        {
            if (MessageBox.Show($"Delete '{Path.GetFileName(item.Path)}' and move it to the Recycle Bin?",
                "Delete SMILE Source", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return VSConstants.S_OK;
            EnsureProjectDocumentRemovalAllowed(item.Path);
            CloseSourceDocument(item.Path);
            var original = File.ReadAllBytes(ProjectPath);
            SmileProjectFileEditor.RemoveSource(ProjectPath, item.Path);
            try
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(item.Path,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin,
                    Microsoft.VisualBasic.FileIO.UICancelOption.ThrowException);
            }
            catch { File.WriteAllBytes(ProjectPath, original); throw; }
            _refreshCoordinator!.Refresh(SmileProjectRefreshReason.SourceRemovedByCommand);
            NotifyProjectDocumentRemoved(item.Path);
        }
        else if (command == (uint)VSConstants.VSStd97CmdID.Paste)
            PasteSource(item);
        return VSConstants.S_OK;
    }

    private static bool CloseSourceDocument(string path)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
        if (dte == null) return false;
        foreach (Document document in dte.Documents)
        {
            if (!string.Equals(document.FullName, path, StringComparison.OrdinalIgnoreCase)) continue;
            document.Close(vsSaveChanges.vsSaveChangesPrompt);
            return true;
        }
        return false;
    }

    private void RenameSource(ProjectItem item, string name)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var destination = Path.Combine(Path.GetDirectoryName(item.Path)!, name);
        if (string.Equals(item.Path, destination, StringComparison.Ordinal)) return;
        if (File.Exists(destination)) throw new IOException("A file with that name already exists.");
        var tracker = GetProjectDocumentTracker();
        if (tracker != null)
        {
            ErrorHandler.ThrowOnFailure(tracker.OnQueryRenameFile(this, item.Path, destination, 0, out var allowed));
            if (allowed == 0) return;
        }
        var reopen = CloseSourceDocument(item.Path);
        var document = XDocument.Load(ProjectPath, LoadOptions.PreserveWhitespace);
        // Projects may omit StartupFile and implicitly use Program.smile.
        if (TryGetSource(item, out var source) && source.IsStartup)
        {
            var root = document.Root!;
            var properties = root.Elements().FirstOrDefault(element => element.Name.LocalName == "PropertyGroup");
            if (properties == null)
            {
                properties = new XElement(root.Name.Namespace + "PropertyGroup");
                root.AddFirst(properties);
            }
            var startup = properties.Elements().FirstOrDefault(element => element.Name.LocalName == "StartupFile");
            if (startup == null)
            {
                startup = new XElement(root.Name.Namespace + "StartupFile");
                properties.Add(startup);
            }
            startup.Value = destination.Substring(ProjectDirectory.Length + 1);
        }
        foreach (var element in document.Descendants())
        {
            var oldPath = element.Name.LocalName == "SmileSource" ? element.Attribute("Include")?.Value :
                element.Name.LocalName == "StartupFile" ? element.Value : null;
            if (oldPath == null || !string.Equals(Path.GetFullPath(Path.Combine(ProjectDirectory, oldPath)),
                item.Path, StringComparison.OrdinalIgnoreCase)) continue;
            var relative = Path.Combine(Path.GetDirectoryName(oldPath) ?? "", name);
            if (element.Name.LocalName == "SmileSource") element.SetAttributeValue("Include", relative);
            else element.Value = relative;
        }
        File.Move(item.Path, destination);
        try { document.Save(ProjectPath, SaveOptions.DisableFormatting); }
        catch { File.Move(destination, item.Path); throw; }
        tracker?.OnAfterRenameFile(this, item.Path, destination, 0);
        _refreshCoordinator!.Refresh(SmileProjectRefreshReason.ManualRefresh, revealPath: destination);
        if (reopen && ParseCanonicalName(destination, out var id) == VSConstants.S_OK)
        {
            var view = VSConstants.LOGVIEWID_Primary;
            OpenItem(id, ref view, IntPtr.Zero, out _);
        }
    }

    private int SetSourceLabel(uint itemId, object value)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var item = Item(itemId);
        if (item == null) return VSConstants.E_INVALIDARG;
        var command = new[] { new OLECMD { cmdID = (uint)VSConstants.VSStd97CmdID.Rename } };
        QueryFileCommands(item, VSConstants.GUID_VSStandardCommandSet97, 1, command);
        if ((command[0].cmdf & (uint)OLECMDF.OLECMDF_ENABLED) == 0) return CommandNotSupported;
        try { RenameSource(item, ValidateNewSourceName(Convert.ToString(value) ?? "")); }
        catch (Exception exception)
        {
            ShowMessage(exception.Message, OLEMSGICON.OLEMSGICON_CRITICAL);
            return Marshal.GetHRForException(exception);
        }
        return VSConstants.S_OK;
    }

    private void PasteSource(ProjectItem target)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var files = Clipboard.GetFileDropList();
        if (files.Count != 1 || !string.Equals(Path.GetExtension(files[0]), ".smile", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Paste one .smile source file at a time.");
        var source = files[0]!;
        var cutProject = Clipboard.GetData(CutProjectFormat) as string;
        var directory = target.Kind == ItemKind.Folder ? target.Path : ProjectDirectory;
        var destination = Path.Combine(directory, Path.GetFileName(source));
        if (cutProject != null && string.Equals(source, destination, StringComparison.OrdinalIgnoreCase)) return;
        if (File.Exists(destination))
        {
            var suffix = 1;
            do { destination = Path.Combine(directory, Path.GetFileNameWithoutExtension(source) + "Copy" + suffix++ + ".smile"); }
            while (File.Exists(destination));
        }
        var targetProjectBytes = File.ReadAllBytes(ProjectPath);
        var sourceProjectBytes = cutProject == null ? null : File.ReadAllBytes(cutProject);
        if (cutProject != null) CloseSourceDocument(source);
        File.Copy(source, destination);
        try
        {
            SmileProjectFileEditor.AddSource(ProjectPath, destination);
            if (cutProject != null)
            {
                SmileProjectFileEditor.RemoveSource(cutProject, source);
                File.Delete(source);
                Clipboard.Clear();
            }
        }
        catch
        {
            File.WriteAllBytes(ProjectPath, targetProjectBytes);
            if (cutProject != null) File.WriteAllBytes(cutProject, sourceProjectBytes!);
            File.Delete(destination);
            throw;
        }
        _refreshCoordinator!.Refresh(SmileProjectRefreshReason.ManualRefresh, revealPath: destination);
    }
    private int ShowContextMenu(ProjectItem item, IntPtr pointerToVariant)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
        if (shell == null)
            return VSConstants.E_FAIL;
        var menuId = item.Kind switch
        {
            ItemKind.Project => SmileProjectCommands.ProjectContextMenu,
            ItemKind.Folder => SmileProjectCommands.FolderContextMenu,
            ItemKind.References => SmileProjectCommands.ReferencesContextMenu,
            ItemKind.Reference => SmileProjectCommands.ReferenceContextMenu,
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
        var menuGroup = item.Kind == ItemKind.File ? VsMenus.guidSHLMainMenu : SmileProjectCommands.CommandSet;
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

}
