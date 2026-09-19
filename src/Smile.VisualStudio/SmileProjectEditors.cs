using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Smile.VisualStudio;

internal sealed partial class SmileProject
{
    public int OpenItem(uint itemid, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        ppWindowFrame = null!;
        var item = Item(itemid);
        if (item == null || item.Kind != ItemKind.File)
            return VSConstants.E_INVALIDARG;
        if (item.IsSource && !item.Exists)
        {
            ShowMessage($"The included SMILE source file was not found: {item.Path}", OLEMSGICON.OLEMSGICON_WARNING);
            return VSConstants.S_OK;
        }

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

    public int ReopenItem(uint itemid, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidLogicalView,
        IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        ppWindowFrame = null!;
        var item = Item(itemid);
        if (item == null || item.Kind != ItemKind.File) return VSConstants.E_INVALIDARG;
        var openDocument = Package.GetGlobalService(typeof(SVsUIShellOpenDocument)) as IVsUIShellOpenDocument;
        if (openDocument == null) return VSConstants.E_FAIL;
        // Compare With and Git supply their own editor/view and document data.
        // Returning the already open text editor silently discards that request.
        var site = _site ?? (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)_package;
        return openDocument.OpenSpecificEditor((uint)_VSRDTFLAGS.RDT_EditLock, item.Path,
            ref rguidEditorType, pszPhysicalView, ref rguidLogicalView, item.Caption, this,
            itemid, punkDocDataExisting, site, out ppWindowFrame);
    }

    // IVsProject3 is required for OpenDocumentViaProjectWithSpecific. Without it,
    // the shell falls back to OpenItem and loses the requested comparison editor.
    public int OpenItemWithSpecific(uint itemid, uint grfEditorFlags, ref Guid rguidEditorType,
        string pszPhysicalView, ref Guid rguidLogicalView, IntPtr punkDocDataExisting,
        out IVsWindowFrame ppWindowFrame)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if ((grfEditorFlags & (uint)__VSSPECIFICEDITORFLAGS.VSSPECIFICEDITOR_UseEditor) != 0)
            return ReopenItem(itemid, ref rguidEditorType, pszPhysicalView, ref rguidLogicalView,
                punkDocDataExisting, out ppWindowFrame);
        if ((grfEditorFlags & (uint)__VSSPECIFICEDITORFLAGS.VSSPECIFICEDITOR_UseView) == 0)
            return OpenItem(itemid, ref rguidLogicalView, punkDocDataExisting, out ppWindowFrame);

        ppWindowFrame = null!;
        var item = Item(itemid);
        if (item == null || item.Kind != ItemKind.File) return VSConstants.E_INVALIDARG;
        var openDocument = Package.GetGlobalService(typeof(SVsUIShellOpenDocument)) as IVsUIShellOpenDocument;
        if (openDocument == null) return VSConstants.E_FAIL;
        var site = _site ?? (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)_package;
        return openDocument.OpenStandardEditor((uint)_VSRDTFLAGS.RDT_EditLock, item.Path,
            ref rguidLogicalView, item.Caption, this, itemid, punkDocDataExisting, site, out ppWindowFrame);
    }

    // Source membership continues to use the existing explicit project commands.
    public int AddItemWithSpecific(uint itemidLoc, VSADDITEMOPERATION dwAddItemOperation,
        string pszItemName, uint cFilesToOpen, string[] rgpszFilesToOpen, IntPtr hwndDlgOwner,
        uint grfEditorFlags, ref Guid rguidEditorType, string pszPhysicalView,
        ref Guid rguidLogicalView, VSADDRESULT[] pResult)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        return AddItem(itemidLoc, dwAddItemOperation, pszItemName, cFilesToOpen, rgpszFilesToOpen,
            hwndDlgOwner, pResult);
    }

    public int TransferItem(string pszMkDocumentOld, string pszMkDocumentNew,
        IVsWindowFrame punkWindowFrame) => VSConstants.E_NOTIMPL;
}
