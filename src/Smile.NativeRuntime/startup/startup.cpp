#define WIN32_LEAN_AND_MEAN
#define _WIN32_WINNT 0x0A00
#include <windows.h>
#include <dwmapi.h>
#include <objidl.h>
#include <gdiplus.h>
#include <shellapi.h>
#include "startup.h"
#include "startup_logo.generated.h"

// Independent presentation keeps loading responsive without moving program/renderer ownership.
// No static C++ constructors: SMILE executables use a custom entry point.
static HANDLE startup_thread, startup_painted;
static HWND startup_window;
static SRWLOCK startup_lock = SRWLOCK_INIT;
static WCHAR startup_title[512], startup_author[256], startup_build[256], startup_detail[1024];
static Gdiplus::Bitmap* startup_logo;
static volatile LONG startup_ready_requested;
static ULONGLONG startup_last_tick, startup_visible_ms;
static RECT startup_links[7];
static const WCHAR* startup_urls[] = {
    L"mailto:louiery@gmail.com", L"https://github.com/Sincioco",
    L"https://facebook.com/louiery.sincioco", L"https://linkedin.com/in/louierysincioco",
    L"https://youtube.com/@TheSincioco", L"https://tiktok.com/@sincioco",
    L"https://github.com/sincioco/smile-2.0"
};

static bool startup_visible(HWND window)
{
    HWND owner = GetWindow(window, GW_OWNER);
    return IsWindowVisible(window) && !IsIconic(window) && (!owner || !IsIconic(owner));
}

static void startup_utf8(const char* text, int length, WCHAR* output, int capacity)
{
    int count = text ? MultiByteToWideChar(CP_UTF8, 0, text, length, output, capacity - 1) : 0;
    output[count > 0 ? count : 0] = 0;
}

static void startup_text(HDC dc, const WCHAR* text, int x, int y, int w, int h, int size, COLORREF color)
{
    HFONT font = CreateFontW(-size, 0, 0, 0, FW_NORMAL, FALSE, FALSE, FALSE,
        DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, CLEARTYPE_QUALITY,
        DEFAULT_PITCH, L"Segoe UI");
    HGDIOBJ previous = SelectObject(dc, font);
    RECT area = { x, y, x + w, y + h };
    SetTextColor(dc, color);
    DrawTextW(dc, text, -1, &area, DT_CENTER | DT_WORDBREAK | DT_NOPREFIX);
    SelectObject(dc, previous);
    DeleteObject(font);
}

static void startup_paint(HWND window)
{
    PAINTSTRUCT paint;
    HDC target = BeginPaint(window, &paint);
    RECT client;
    GetClientRect(window, &client);
    const int width = client.right, height = client.bottom;
    HDC dc = CreateCompatibleDC(target);
    HBITMAP bitmap = CreateCompatibleBitmap(target, width, height);
    HGDIOBJ old = SelectObject(dc, bitmap);
    HBRUSH background = CreateSolidBrush(RGB(8, 16, 30));
    FillRect(dc, &client, background);
    DeleteObject(background);
    SetBkMode(dc, TRANSPARENT);
    // Scale one layout to the current monitor/work area; keep footer visible on small screens.
    const float scale = (float)height / 650.0f;
    const int margin = (int)(22 * scale), text_width = width - 2 * margin;
    startup_text(dc, startup_title, margin, (int)(20 * scale), text_width, (int)(60 * scale),
        (int)(23 * scale), RGB(244, 247, 252));
    if (startup_logo) {
        Gdiplus::Graphics graphics(dc);
        graphics.SetInterpolationMode(Gdiplus::InterpolationModeHighQualityBicubic);
        const float max_width = width * .72f, max_height = 265 * scale;
        const float ratio_x = max_width / startup_logo->GetWidth();
        const float ratio_y = max_height / startup_logo->GetHeight();
        const float ratio = ratio_x < ratio_y ? ratio_x : ratio_y;
        const float w = startup_logo->GetWidth() * ratio, h = startup_logo->GetHeight() * ratio;
        graphics.DrawImage(startup_logo, (width - w) / 2, 82 * scale + (max_height - h) / 2, w, h);
    }
    WCHAR detail[1024];
    AcquireSRWLockShared(&startup_lock);
    lstrcpynW(detail, startup_detail, 1024);
    ReleaseSRWLockShared(&startup_lock);
    startup_text(dc, startup_ready_requested ? L"Ready - Opening the program..." : L"Preparing the program...",
        margin, (int)(360 * scale), text_width, (int)(26 * scale), (int)(17 * scale), RGB(238, 199, 70));
    startup_text(dc, detail, margin, (int)(390 * scale), text_width, (int)(46 * scale),
        (int)(12 * scale), RGB(171, 188, 211));
    startup_text(dc, L"Created in SMILE 2.0", margin, (int)(440 * scale), text_width, (int)(25 * scale),
        (int)(18 * scale), RGB(244, 247, 252));
    startup_text(dc, startup_author, margin, (int)(469 * scale), text_width, (int)(24 * scale),
        (int)(14 * scale), RGB(244, 247, 252));
    startup_text(dc, startup_build, margin, (int)(497 * scale), text_width, (int)(22 * scale),
        (int)(12 * scale), RGB(171, 188, 211));
    startup_text(dc, L"SMILE 2.0 - Simple Modern and Intuitive Language for Everyone.\nCopyright(c) 2026. All rights reserved.\nProgrammed by: Louiery R. Sincioco (Sin)",
        margin, (int)(532 * scale), text_width, (int)(48 * scale), (int)(11 * scale), RGB(156, 169, 190));
    const WCHAR* labels[] = { L"louiery@gmail.com", L"GitHub", L"Facebook", L"LinkedIn", L"YouTube", L"TikTok", L"github.com/sincioco/smile-2.0" };
    for (int i = 0; i < 7; ++i) {
        const int row = i == 6 ? 2 : i / 3;
        const int cell = text_width / 3;
        const int x = i == 6 ? margin : margin + (i % 3) * cell;
        const int y = (int)((586 + row * 18) * scale);
        const int w = i == 6 ? text_width : cell;
        startup_links[i] = { x, y, x + w, y + (int)(18 * scale) };
        startup_text(dc, labels[i], x, y, w, (int)(18 * scale), (int)(11 * scale), RGB(114, 205, 228));
    }
    BitBlt(target, 0, 0, width, height, dc, 0, 0, SRCCOPY);
    SelectObject(dc, old);
    DeleteObject(bitmap);
    DeleteDC(dc);
    EndPaint(window, &paint);
    if (!startup_last_tick && startup_logo && startup_visible(window)) {
        GdiFlush();
        DwmFlush();
        startup_last_tick = GetTickCount64();
        SetEvent(startup_painted);
    }
}

static LRESULT CALLBACK startup_proc(HWND window, UINT message, WPARAM wparam, LPARAM lparam)
{
    switch (message) {
    case WM_PAINT: startup_paint(window); return 0;
    case WM_ERASEBKGND: return 1;
    case WM_TIMER: {
        const ULONGLONG now = GetTickCount64();
        if (startup_last_tick && startup_visible(window))
            startup_visible_ms += now - startup_last_tick;
        startup_last_tick = startup_visible(window) ? now : 0;
        if (startup_ready_requested && startup_visible_ms >= 1000) DestroyWindow(window);
        else InvalidateRect(window, 0, FALSE);
        return 0;
    }
    case WM_LBUTTONUP: {
        POINT point = { (short)LOWORD(lparam), (short)HIWORD(lparam) };
        for (int i = 0; i < 7; ++i)
            if (PtInRect(&startup_links[i], point)) ShellExecuteW(window, L"open", startup_urls[i], 0, 0, SW_SHOWNORMAL);
        return 0;
    }
    case WM_CLOSE: ExitProcess(0); // Closing this program's startup window cancels its launch.
    case WM_DESTROY: PostQuitMessage(0); return 0;
    }
    return DefWindowProcW(window, message, wparam, lparam);
}

static DWORD WINAPI startup_run(void*)
{
    SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
    CoInitializeEx(0, COINIT_APARTMENTTHREADED);
    Gdiplus::GdiplusStartupInput input;
    ULONG_PTR token = 0;
    IStream* stream = 0;
    HGLOBAL memory = GlobalAlloc(GMEM_MOVEABLE, sizeof(smile_startup_logo));
    if (memory) {
        void* bytes = GlobalLock(memory);
        CopyMemory(bytes, smile_startup_logo, sizeof(smile_startup_logo));
        GlobalUnlock(memory);
        if (SUCCEEDED(CreateStreamOnHGlobal(memory, TRUE, &stream))) {
            memory = 0;
            if (Gdiplus::GdiplusStartup(&token, &input, 0) == Gdiplus::Ok)
                startup_logo = Gdiplus::Bitmap::FromStream(stream);
        }
    }
    if (memory) GlobalFree(memory);
    if (!startup_logo || startup_logo->GetLastStatus() != Gdiplus::Ok) {
        MessageBoxW(0, L"The embedded SMILE startup logo could not be decoded.", L"SMILE 2.0 startup", MB_OK | MB_ICONERROR);
        ExitProcess(2);
    }
    WNDCLASSW type = {};
    type.lpfnWndProc = startup_proc;
    type.hInstance = GetModuleHandleW(0);
    type.hCursor = LoadCursorW(0, IDC_ARROW);
    type.lpszClassName = L"SMILE20StartupWindow";
    RegisterClassW(&type);
    MONITORINFO monitor = { sizeof(monitor) };
    GetMonitorInfoW(MonitorFromWindow(GetForegroundWindow(), MONITOR_DEFAULTTONEAREST), &monitor);
    const RECT area = monitor.rcWork;
    int height = MulDiv(650, (int)GetDpiForSystem(), 96);
    if (height > area.bottom - area.top - 40) height = area.bottom - area.top - 40;
    if (height > area.right - area.left - 40) height = area.right - area.left - 40;
    const int width = height;
    startup_window = CreateWindowExW(WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE, type.lpszClassName, startup_title,
        WS_POPUP | WS_BORDER, area.left + (area.right - area.left - width) / 2,
        area.top + (area.bottom - area.top - height) / 2, width, height, 0, 0, type.hInstance, 0);
    if (!startup_window) ExitProcess(2);
    ShowWindow(startup_window, SW_SHOWNOACTIVATE);
    UpdateWindow(startup_window);
    SetTimer(startup_window, 1, 50, 0);
    MSG message;
    while (GetMessageW(&message, 0, 0, 0) > 0) { TranslateMessage(&message); DispatchMessageW(&message); }
    startup_window = 0;
    delete startup_logo;
    startup_logo = 0;
    if (stream) stream->Release();
    if (token) Gdiplus::GdiplusShutdown(token);
    CoUninitialize();
    return 0;
}

extern "C" void smile_startup_begin(const char* title, const char* author, const char* build)
{
    startup_utf8(title, -1, startup_title, 512);
    startup_utf8(build, -1, startup_build, 256);
    if (author && *author) {
        lstrcpyW(startup_author, L"Created by ");
        startup_utf8(author, -1, startup_author + 11, 245);
    }
    lstrcpyW(startup_detail, L"Initializing the runtime. Startup duration is not yet known.");
    startup_painted = CreateEventW(0, TRUE, FALSE, 0);
    startup_thread = CreateThread(0, 0, startup_run, 0, 0, 0);
    if (!startup_painted || !startup_thread) ExitProcess(2);
    WaitForSingleObject(startup_painted, INFINITE);
}

extern "C" void smile_startup_attach(HWND owner)
{
    if (startup_window) {
        AcquireSRWLockExclusive(&startup_lock);
        lstrcpyW(startup_detail, L"Preparing the first frame. Startup duration is not yet known.");
        ReleaseSRWLockExclusive(&startup_lock);
        SetWindowLongPtrW(startup_window, GWLP_HWNDPARENT, (LONG_PTR)owner);
        SetWindowPos(startup_window, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }
}

extern "C" void smile_startup_status(const char* path, long long length)
{
    if (!startup_thread || startup_ready_requested) return;
    AcquireSRWLockExclusive(&startup_lock);
    lstrcpyW(startup_detail, L"Loading asset: ");
    startup_utf8(path, (int)(length < 900 ? length : 900), startup_detail + 15, 1009);
    ReleaseSRWLockExclusive(&startup_lock);
}

extern "C" void smile_startup_ready(void)
{
    if (!startup_thread) return;
    InterlockedExchange(&startup_ready_requested, 1);
    BOOL quit = FALSE;
    WPARAM quit_code = 0;
    // Keep the owning program's queue responsive while only the remaining visible interval elapses.
    while (MsgWaitForMultipleObjects(1, &startup_thread, FALSE, INFINITE, QS_ALLINPUT) == WAIT_OBJECT_0 + 1) {
        MSG message;
        while (PeekMessageW(&message, 0, 0, 0, PM_REMOVE)) {
            if (message.message == WM_QUIT) { quit = TRUE; quit_code = message.wParam; continue; }
            TranslateMessage(&message); DispatchMessageW(&message);
        }
    }
    CloseHandle(startup_thread);
    CloseHandle(startup_painted);
    startup_thread = startup_painted = 0;
    if (quit) PostQuitMessage((int)quit_code);
}
