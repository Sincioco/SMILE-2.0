using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Smile.VisualStudio;

// Expose real hierarchy paths to Visual Studio's selected source-control provider.
// The provider owns Git status, history, comparisons and ignore/track semantics.
internal sealed partial class SmileProject : IVsSccProject2
{
    private readonly Dictionary<uint, VsStateIcon> _sourceControlIcons = new();

    public int GetSccFiles(uint itemid, CALPOLESTR[] files, CADWORD[] flags)
    {
        var item = Item(itemid);
        if (item == null) return VSConstants.E_INVALIDARG;
        var paths = item.Kind is ItemKind.Project or ItemKind.Folder
            ? _items.Values.Where(candidate => candidate.Kind == ItemKind.File || candidate.Id == itemid)
                .Where(candidate => item.Kind == ItemKind.Project || candidate.Path.StartsWith(item.Path + "\\", StringComparison.OrdinalIgnoreCase))
                .Select(candidate => candidate.Path).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            : new[] { item.Path };
        files[0].cElems = (uint)paths.Length;
        files[0].pElems = Marshal.AllocCoTaskMem(IntPtr.Size * paths.Length);
        flags[0].cElems = (uint)paths.Length;
        flags[0].pElems = Marshal.AllocCoTaskMem(sizeof(int) * paths.Length);
        for (var index = 0; index < paths.Length; index++)
        {
            Marshal.WriteIntPtr(files[0].pElems, index * IntPtr.Size, Marshal.StringToCoTaskMemUni(paths[index]));
            Marshal.WriteInt32(flags[0].pElems, index * sizeof(int), 0);
        }
        return VSConstants.S_OK;
    }

    public int GetSccSpecialFiles(uint itemid, string file, CALPOLESTR[] files, CADWORD[] flags)
    {
        files[0] = new CALPOLESTR();
        flags[0] = new CADWORD();
        return VSConstants.S_OK;
    }

    public int SetSccLocation(string project, string auxiliary, string local, string provider) => VSConstants.S_OK;

    public int SccGlyphChanged(int count, uint[] items, VsStateIcon[] icons, uint[] status)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (count == 0) _sourceControlIcons.Clear();
        for (var index = 0; index < count; index++)
        {
            _sourceControlIcons[items[index]] = icons[index];
            foreach (var listener in _events.Values)
                listener.OnPropertyChanged(items[index], (int)__VSHPROPID.VSHPROPID_StateIconIndex, 0);
        }
        return VSConstants.S_OK;
    }
}
