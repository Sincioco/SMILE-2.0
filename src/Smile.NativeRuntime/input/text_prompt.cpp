#define WIN32_LEAN_AND_MEAN
#include <windows.h>

extern "C" {
void* smile_text_from_utf8(const char*, long long);
const char* smile_text_utf8(void*);
long long smile_text_byte_length(void*);
void smile_text_release(void*);
}

// A short, owner-modal text prompt. Its message loop keeps the existing canvas
// repaintable; the dialog has no renderer, storage or application dependencies.
struct TextPrompt {
    HWND edit;
    bool finished;
    bool accepted;
    WCHAR text[257];
};

static LRESULT CALLBACK text_prompt_proc(HWND window, UINT message, WPARAM wp, LPARAM lp)
{
    TextPrompt* value = reinterpret_cast<TextPrompt*>(GetWindowLongPtrW(window, GWLP_USERDATA));
    if (message == WM_NCCREATE) {
        value = static_cast<TextPrompt*>(reinterpret_cast<CREATESTRUCTW*>(lp)->lpCreateParams);
        SetWindowLongPtrW(window, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(value));
    }
    if (value && message == WM_COMMAND && (LOWORD(wp) == IDOK || LOWORD(wp) == IDCANCEL)) {
        value->accepted = LOWORD(wp) == IDOK;
        if (value->accepted) GetWindowTextW(value->edit, value->text, 257);
        value->finished = true;
        return 0;
    }
    if (value && message == WM_CLOSE) {
        value->finished = true;
        return 0;
    }
    if (message == DM_GETDEFID) return MAKELRESULT(IDOK, DC_HASDEFID);
    return DefWindowProcW(window, message, wp, lp);
}

static bool convert(void* text, WCHAR* destination, int capacity)
{
    const long long length = smile_text_byte_length(text);
    destination[0] = 0;
    if (length == 0) return true;
    if (length < 0 || length > 4096) return false;
    int size = MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS,
        smile_text_utf8(text), static_cast<int>(length), destination, capacity - 1);
    if (size == 0) return false;
    destination[size] = 0;
    return true;
}

extern "C" void* smile_text_prompt_window(HWND owner, void* title, void* message, void* initial)
{
    WCHAR caption[257], label[1025], starting[257];
    const bool valid = convert(title, caption, 257) && convert(message, label, 1025) &&
        convert(initial, starting, 257);
    smile_text_release(title);
    smile_text_release(message);
    smile_text_release(initial);
    if (!valid || !IsWindow(owner)) return nullptr;

    const WCHAR* name = L"SMILE20TextPrompt";
    WNDCLASSW type = {};
    type.lpfnWndProc = text_prompt_proc;
    type.hInstance = GetModuleHandleW(nullptr);
    type.hCursor = LoadCursorW(nullptr, IDC_ARROW);
    type.hbrBackground = reinterpret_cast<HBRUSH>(COLOR_BTNFACE + 1);
    type.lpszClassName = name;
    if (!RegisterClassW(&type) && GetLastError() != ERROR_CLASS_ALREADY_EXISTS) return nullptr;

    UINT dpi = GetDpiForWindow(owner);
    if (!dpi) dpi = 96;
    auto pixels = [dpi](int value) { return MulDiv(value, static_cast<int>(dpi), 96); };
    RECT owner_rect;
    GetWindowRect(owner, &owner_rect);
    RECT rect = {0, 0, pixels(460), pixels(162)};
    DWORD style = WS_POPUP | WS_CAPTION | WS_SYSMENU;
    AdjustWindowRectExForDpi(&rect, style, FALSE, WS_EX_DLGMODALFRAME, dpi);
    const int width = rect.right - rect.left, height = rect.bottom - rect.top;
    TextPrompt value = {};
    HWND window = CreateWindowExW(WS_EX_DLGMODALFRAME | WS_EX_CONTROLPARENT, name, caption, style,
        owner_rect.left + (owner_rect.right - owner_rect.left - width) / 2,
        owner_rect.top + (owner_rect.bottom - owner_rect.top - height) / 2,
        width, height, owner, nullptr, type.hInstance, &value);
    if (!window) return nullptr;
    HFONT font = CreateFontW(-pixels(14), 0, 0, 0, FW_NORMAL, FALSE, FALSE, FALSE,
        DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, CLEARTYPE_QUALITY,
        DEFAULT_PITCH, L"Segoe UI");
    auto child = [&](const WCHAR* cls, const WCHAR* text, DWORD flags,
        int x, int y, int w, int h, int id) {
        HWND control = CreateWindowExW(cls[0] == L'E' ? WS_EX_CLIENTEDGE : 0, cls, text,
            WS_CHILD | WS_VISIBLE | flags, pixels(x), pixels(y), pixels(w), pixels(h),
            window, reinterpret_cast<HMENU>(static_cast<INT_PTR>(id)), type.hInstance, nullptr);
        SendMessageW(control, WM_SETFONT, reinterpret_cast<WPARAM>(font), TRUE);
        return control;
    };
    HWND prompt = child(L"STATIC", label, 0, 18, 15, 424, 40, 100);
    value.edit = child(L"EDIT", starting, WS_TABSTOP | ES_AUTOHSCROLL, 18, 60, 424, 28, 101);
    HWND accept = child(L"BUTTON", L"OK", WS_TABSTOP | BS_DEFPUSHBUTTON, 252, 113, 90, 30, IDOK);
    HWND cancel = child(L"BUTTON", L"Cancel", WS_TABSTOP | BS_PUSHBUTTON, 352, 113, 90, 30, IDCANCEL);
    if (!prompt || !value.edit || !accept || !cancel) {
        DestroyWindow(window);
        DeleteObject(font);
        return nullptr;
    }
    SendMessageW(value.edit, EM_SETLIMITTEXT, 256, 0);
    SendMessageW(value.edit, EM_SETSEL, 0, -1);
    EnableWindow(owner, FALSE);
    ShowWindow(window, SW_SHOW);
    SetFocus(value.edit);
    MSG event;
    while (!value.finished) {
        int status = GetMessageW(&event, nullptr, 0, 0);
        if (status <= 0) {
            if (status == 0) PostQuitMessage(static_cast<int>(event.wParam));
            break;
        }
        if (!IsDialogMessageW(window, &event)) {
            TranslateMessage(&event);
            DispatchMessageW(&event);
        }
    }
    EnableWindow(owner, TRUE);
    DestroyWindow(window);
    SetActiveWindow(owner);
    DeleteObject(font);
    if (!value.accepted) return nullptr;
    char bytes[1025];
    int count = WideCharToMultiByte(CP_UTF8, WC_ERR_INVALID_CHARS,
        value.text, -1, bytes, 1025, nullptr, nullptr);
    return count > 0 ? smile_text_from_utf8(bytes, count - 1) : nullptr;
}
