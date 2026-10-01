#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <shlobj.h>
#include <knownfolders.h>
#include <strsafe.h>
#include "storage_location.h"

int smile_storage_root(WCHAR* path, int capacity)
{
    PWSTR folder = NULL;
    HRESULT result = SHGetKnownFolderPath(&FOLDERID_SavedGames, KF_FLAG_CREATE, NULL, &folder);
    if (FAILED(result) || folder == NULL) return 0;
    result = StringCchCopyW(path, (size_t)capacity, folder);
    CoTaskMemFree(folder);
    return SUCCEEDED(result);
}

/* Seed an absent save once. Never overwrite canonical data or remove legacy data.
   A backup in Saved Games already establishes authority, even without a primary. */
int smile_storage_import_legacy(const WCHAR* path)
{
    WCHAR root[2048], legacy[2048], backup[2048], legacy_backup[2048];
    PWSTR local = NULL;
    size_t length;
    if (GetFileAttributesW(path) != INVALID_FILE_ATTRIBUTES) return 1;
    if (FAILED(StringCchPrintfW(backup, 2048, L"%ls.bak", path))) return 0;
    if (GetFileAttributesW(backup) != INVALID_FILE_ATTRIBUTES) return 1;
    if (!smile_storage_root(root, 2048)) return 0;
    length = (size_t)lstrlenW(root);
    if (CompareStringOrdinal(root, (int)length, path, (int)length, TRUE) != CSTR_EQUAL ||
        path[length] != L'\\') return 0;
    if (FAILED(SHGetKnownFolderPath(&FOLDERID_LocalAppData, 0, NULL, &local)) || local == NULL)
        return 0;
    if (FAILED(StringCchPrintfW(legacy, 2048, L"%ls%ls", local, path + length)))
    {
        CoTaskMemFree(local);
        return 0;
    }
    CoTaskMemFree(local);
    if (FAILED(StringCchPrintfW(legacy_backup, 2048, L"%ls.bak", legacy))) return 0;
    if (GetFileAttributesW(legacy_backup) != INVALID_FILE_ATTRIBUTES &&
        !CopyFileW(legacy_backup, backup, TRUE) && GetLastError() != ERROR_FILE_EXISTS) return 0;
    if (GetFileAttributesW(legacy) != INVALID_FILE_ATTRIBUTES &&
        !CopyFileW(legacy, path, TRUE) && GetLastError() != ERROR_FILE_EXISTS) return 0;
    return 1;
}
