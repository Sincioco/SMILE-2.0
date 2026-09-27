#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <commdlg.h>

extern "C" {
void* smile_text_from_utf8(const char*, long long);
const char* smile_text_utf8(void*);
long long smile_text_byte_length(void*);
void smile_text_release(void*);
}

static bool wide(void* text, WCHAR* out, int capacity)
{
    long long length = smile_text_byte_length(text);
    out[0] = 0;
    if (!length) return true;
    if (length < 0 || length > 16384) return false;
    int count = MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS,
        smile_text_utf8(text), static_cast<int>(length), out, capacity - 1);
    if (!count) return false;
    out[count] = 0;
    return true;
}

// Select a path only. File contents and background jobs belong to the caller.
extern "C" void* smile_file_pick_window(HWND owner, long long saving,
    void* title, void* extension, void* suggested)
{
    WCHAR caption[257], ext[17], path[4096], filter[64] = {};
    bool valid = wide(title, caption, 257) && wide(extension, ext, 17) &&
        wide(suggested, path, 4096);
    smile_text_release(title);
    smile_text_release(extension);
    smile_text_release(suggested);
    if (!valid || !ext[0]) return nullptr;
    for (int i = 0; ext[i]; ++i)
        if (!(ext[i] >= L'a' && ext[i] <= L'z') && !(ext[i] >= L'A' && ext[i] <= L'Z') &&
            !(ext[i] >= L'0' && ext[i] <= L'9')) return nullptr;
    // Extension-specific default filter; a user can still select All Files.
    int n = 0;
    filter[n++] = L'*'; filter[n++] = L'.';
    for (int i = 0; ext[i]; ++i) filter[n++] = ext[i];
    ++n;
    filter[n++] = L'*'; filter[n++] = L'.';
    for (int i = 0; ext[i]; ++i) filter[n++] = ext[i];
    ++n;
    const WCHAR all[] = L"All Files\0*.*\0";
    for (int i = 0; i < static_cast<int>(sizeof(all) / sizeof(WCHAR)); ++i) filter[n++] = all[i];
    HMODULE library = LoadLibraryExW(L"comdlg32.dll", nullptr, LOAD_LIBRARY_SEARCH_SYSTEM32);
    if (!library) return nullptr;
    typedef BOOL (WINAPI *ShowDialog)(LPOPENFILENAMEW);
    auto show = reinterpret_cast<ShowDialog>(GetProcAddress(library,
        saving ? "GetSaveFileNameW" : "GetOpenFileNameW"));
    OPENFILENAMEW dialog = {};
    dialog.lStructSize = sizeof(dialog);
    dialog.hwndOwner = owner;
    dialog.lpstrFile = path;
    dialog.nMaxFile = 4096;
    dialog.lpstrFilter = filter;
    dialog.lpstrDefExt = ext;
    dialog.lpstrTitle = caption;
    dialog.Flags = OFN_EXPLORER | OFN_NOCHANGEDIR | OFN_PATHMUSTEXIST |
        (saving ? OFN_OVERWRITEPROMPT : OFN_FILEMUSTEXIST);
    bool accepted = show && show(&dialog);
    FreeLibrary(library);
    if (!accepted) return nullptr;
    char utf8[16384];
    int count = WideCharToMultiByte(CP_UTF8, WC_ERR_INVALID_CHARS, path, -1,
        utf8, sizeof(utf8), nullptr, nullptr);
    return count > 0 ? smile_text_from_utf8(utf8, count - 1) : nullptr;
}
