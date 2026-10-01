# Keep support tools on the same Windows Known Folder as storage_location.c.
$ErrorActionPreference = 'Stop'
if (-not ('Smile.SaveLocation' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
namespace Smile {
    public static class SaveLocation {
        [DllImport("shell32.dll")]
        private static extern int SHGetKnownFolderPath(ref Guid id, uint flags, IntPtr token, out IntPtr path);
        public static string SavedGames() {
            var id = new Guid("4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4");
            IntPtr path;
            Marshal.ThrowExceptionForHR(SHGetKnownFolderPath(ref id, 0x8000, IntPtr.Zero, out path));
            try { return Marshal.PtrToStringUni(path); }
            finally { Marshal.FreeCoTaskMem(path); }
        }
    }
}
'@
}
Join-Path ([Smile.SaveLocation]::SavedGames()) 'SMILE 2.0\Games'
