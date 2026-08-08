#define WIN32_LEAN_AND_MEAN
#define _WIN32_WINNT 0x0A00
#include <windows.h>
#include <mmsystem.h>
#include <initguid.h>
#include <knownfolders.h>
#include <shlobj.h>
#include <limits.h>

#define SMILE_KEY_NONE 0
#define SMILE_KEY_W 1
#define SMILE_KEY_A 2
#define SMILE_KEY_S 3
#define SMILE_KEY_D 4
#define SMILE_KEY_UP 10
#define SMILE_KEY_DOWN 11
#define SMILE_KEY_LEFT 12
#define SMILE_KEY_RIGHT 13
#define SMILE_KEY_ENTER 14
#define SMILE_KEY_ESCAPE 15
#define SMILE_KEY_SPACE 16
#define SMILE_KEY_1 17
#define SMILE_KEY_2 18

static HWND smile_window;
static HDC smile_back_dc;
static HBITMAP smile_back_bitmap;
static HGDIOBJ smile_old_bitmap;
static long long smile_logical_width = 960;
static long long smile_logical_height = 540;
static long long smile_closed;
static unsigned char smile_held[256];
static long long smile_key_queue[64];
static int smile_key_head;
static int smile_key_tail;
static int smile_fullscreen;
static DWORD smile_windowed_style;
static DWORD smile_windowed_ex_style;
static WINDOWPLACEMENT smile_windowed_placement = { sizeof(WINDOWPLACEMENT) };
static const WCHAR smile_window_class[] = L"SMILE20GameWindow";

static void smile_pump_messages(void);
static void smile_present_to_dc(HDC destination);
static void smile_toggle_fullscreen(void);

static void smile_zero_memory(void* memory, SIZE_T length)
{
    volatile unsigned char* current = (volatile unsigned char*)memory;
    while (length-- != 0)
        *current++ = 0;
}

static HANDLE smile_output(void)
{
    return GetStdHandle(STD_OUTPUT_HANDLE);
}

void smile_print_text(const char* text, long long length)
{
    DWORD written;
    if (text == 0 || length <= 0)
        return;
    WriteFile(smile_output(), text, (DWORD)length, &written, 0);
}

static int smile_format_number(long long value, char* buffer, int capacity)
{
    char temporary[32];
    int count = 0;
    int output = 0;
    unsigned long long magnitude;
    if (capacity <= 0)
        return 0;
    magnitude = value < 0 ? (unsigned long long)(-(value + 1)) + 1 : (unsigned long long)value;
    do
    {
        temporary[count++] = (char)('0' + (magnitude % 10));
        magnitude /= 10;
    }
    while (magnitude != 0 && count < (int)sizeof(temporary));
    if (value < 0 && output < capacity)
        buffer[output++] = '-';
    while (count > 0 && output < capacity)
        buffer[output++] = temporary[--count];
    return output;
}

void smile_print_number(long long value)
{
    char buffer[32];
    int length = smile_format_number(value, buffer, (int)sizeof(buffer));
    smile_print_text(buffer, length);
}

void smile_print_boolean(long long value)
{
    static const char true_text[] = "TRUE";
    static const char false_text[] = "FALSE";
    if (value != 0)
        smile_print_text(true_text, 4);
    else
        smile_print_text(false_text, 5);
}

void smile_print_newline(void)
{
    static const char newline[] = "\r\n";
    smile_print_text(newline, 2);
}

static long long smile_map_key(WCHAR character, WORD virtual_key)
{
    if (character == L'w' || character == L'W' || virtual_key == 'W') return SMILE_KEY_W;
    if (character == L'a' || character == L'A' || virtual_key == 'A') return SMILE_KEY_A;
    if (character == L's' || character == L'S' || virtual_key == 'S') return SMILE_KEY_S;
    if (character == L'd' || character == L'D' || virtual_key == 'D') return SMILE_KEY_D;
    if (virtual_key == VK_UP) return SMILE_KEY_UP;
    if (virtual_key == VK_DOWN) return SMILE_KEY_DOWN;
    if (virtual_key == VK_LEFT) return SMILE_KEY_LEFT;
    if (virtual_key == VK_RIGHT) return SMILE_KEY_RIGHT;
    if (virtual_key == VK_RETURN) return SMILE_KEY_ENTER;
    if (virtual_key == VK_ESCAPE) return SMILE_KEY_ESCAPE;
    if (virtual_key == VK_SPACE) return SMILE_KEY_SPACE;
    if (virtual_key == '1') return SMILE_KEY_1;
    if (virtual_key == '2') return SMILE_KEY_2;
    return SMILE_KEY_NONE;
}

static int smile_key_virtual(long long key)
{
    switch (key)
    {
        case SMILE_KEY_W: return 'W';
        case SMILE_KEY_A: return 'A';
        case SMILE_KEY_S: return 'S';
        case SMILE_KEY_D: return 'D';
        case SMILE_KEY_UP: return VK_UP;
        case SMILE_KEY_DOWN: return VK_DOWN;
        case SMILE_KEY_LEFT: return VK_LEFT;
        case SMILE_KEY_RIGHT: return VK_RIGHT;
        case SMILE_KEY_ENTER: return VK_RETURN;
        case SMILE_KEY_ESCAPE: return VK_ESCAPE;
        case SMILE_KEY_SPACE: return VK_SPACE;
        case SMILE_KEY_1: return '1';
        case SMILE_KEY_2: return '2';
        default: return 0;
    }
}

static void smile_queue_key(long long key)
{
    int next;
    if (key == SMILE_KEY_NONE)
        return;
    next = (smile_key_tail + 1) % (int)(sizeof(smile_key_queue) / sizeof(smile_key_queue[0]));
    if (next == smile_key_head)
        return;
    smile_key_queue[smile_key_tail] = key;
    smile_key_tail = next;
}

long long smile_get_key(void)
{
    if (smile_window != 0 || smile_closed != 0)
    {
        long long key;
        smile_pump_messages();
        if (smile_key_head == smile_key_tail)
            return SMILE_KEY_NONE;
        key = smile_key_queue[smile_key_head];
        smile_key_head = (smile_key_head + 1) % (int)(sizeof(smile_key_queue) / sizeof(smile_key_queue[0]));
        return key;
    }
    else
    {
        HANDLE input = GetStdHandle(STD_INPUT_HANDLE);
        DWORD file_type = GetFileType(input);
        if (file_type == FILE_TYPE_CHAR)
        {
            INPUT_RECORD record;
            DWORD available;
            DWORD read;
            while (PeekConsoleInputW(input, &record, 1, &available) && available != 0)
            {
                if (!ReadConsoleInputW(input, &record, 1, &read) || read == 0)
                    break;
                if (record.EventType == KEY_EVENT && record.Event.KeyEvent.bKeyDown)
                {
                    long long key = smile_map_key(record.Event.KeyEvent.uChar.UnicodeChar, record.Event.KeyEvent.wVirtualKeyCode);
                    if (key != SMILE_KEY_NONE)
                        return key;
                }
            }
        }
        else if (file_type == FILE_TYPE_PIPE)
        {
            DWORD available;
            if (PeekNamedPipe(input, 0, 0, 0, &available, 0) && available != 0)
            {
                char character;
                DWORD read;
                if (ReadFile(input, &character, 1, &read, 0) && read == 1)
                    return smile_map_key((WCHAR)(unsigned char)character, 0);
            }
        }
        if ((GetAsyncKeyState('W') & 0x8000) != 0) return SMILE_KEY_W;
        if ((GetAsyncKeyState('A') & 0x8000) != 0) return SMILE_KEY_A;
        if ((GetAsyncKeyState('S') & 0x8000) != 0) return SMILE_KEY_S;
        if ((GetAsyncKeyState('D') & 0x8000) != 0) return SMILE_KEY_D;
        return SMILE_KEY_NONE;
    }
}

long long smile_key_held(long long key)
{
    int virtual_key = smile_key_virtual(key);
    smile_pump_messages();
    return virtual_key > 0 && virtual_key < 256 && smile_held[virtual_key] != 0;
}

void smile_clear_screen(void)
{
    COORD origin;
    origin.X = 0;
    origin.Y = 0;
    SetConsoleCursorPosition(smile_output(), origin);
}

void smile_wait(long long milliseconds)
{
    ULONGLONG end;
    if (milliseconds < 0)
        milliseconds = 0;
    if (milliseconds > 0xFFFFFFFFLL)
        milliseconds = 0xFFFFFFFFLL;
    if (smile_window == 0)
    {
        Sleep((DWORD)milliseconds);
        return;
    }
    end = GetTickCount64() + (ULONGLONG)milliseconds;
    do
    {
        ULONGLONG now;
        DWORD remaining;
        smile_pump_messages();
        now = GetTickCount64();
        if (now >= end)
            break;
        remaining = (DWORD)(end - now);
        MsgWaitForMultipleObjects(0, 0, FALSE, remaining > 10 ? 10 : remaining, QS_ALLINPUT);
    }
    while (smile_closed == 0);
}

long long smile_random(long long minimum, long long maximum)
{
    static unsigned long long state;
    unsigned long long range;
    if (minimum > maximum)
        return minimum;
    if (state == 0)
        state = GetTickCount64() ^ (unsigned long long)(ULONG_PTR)&state;
    state = state * 6364136223846793005ULL + 1442695040888963407ULL;
    range = (unsigned long long)(maximum - minimum) + 1ULL;
    return minimum + (long long)(state % range);
}

long long smile_timer(void)
{
    return (long long)GetTickCount64();
}

static WCHAR* smile_utf8_to_wide(const char* text, long long length)
{
    int count;
    WCHAR* result;
    if (text == 0 || length < 0 || length > INT_MAX)
        return 0;
    count = MultiByteToWideChar(CP_UTF8, 0, text, (int)length, 0, 0);
    if (count < 0)
        return 0;
    result = (WCHAR*)HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, (SIZE_T)(count + 1) * sizeof(WCHAR));
    if (result == 0)
        return 0;
    if (count != 0)
        MultiByteToWideChar(CP_UTF8, 0, text, (int)length, result, count);
    result[count] = 0;
    return result;
}

static int smile_integer(long long value)
{
    if (value < INT_MIN) return INT_MIN;
    if (value > INT_MAX) return INT_MAX;
    return (int)value;
}

static void smile_cleanup_graphics(void)
{
    if (smile_back_dc != 0 && smile_old_bitmap != 0)
        SelectObject(smile_back_dc, smile_old_bitmap);
    if (smile_back_bitmap != 0)
        DeleteObject(smile_back_bitmap);
    if (smile_back_dc != 0)
        DeleteDC(smile_back_dc);
    smile_back_dc = 0;
    smile_back_bitmap = 0;
    smile_old_bitmap = 0;
}

static LRESULT CALLBACK smile_window_proc(HWND window, UINT message, WPARAM wparam, LPARAM lparam)
{
    switch (message)
    {
        case WM_ERASEBKGND:
            return 1;
        case WM_PAINT:
        {
            PAINTSTRUCT paint;
            HDC dc = BeginPaint(window, &paint);
            smile_present_to_dc(dc);
            EndPaint(window, &paint);
            return 0;
        }
        case WM_SIZE:
            InvalidateRect(window, 0, FALSE);
            return 0;
        case WM_DPICHANGED:
        {
            RECT* suggested = (RECT*)lparam;
            SetWindowPos(window, 0, suggested->left, suggested->top,
                suggested->right - suggested->left, suggested->bottom - suggested->top,
                SWP_NOACTIVATE | SWP_NOZORDER);
            return 0;
        }
        case WM_SYSKEYDOWN:
            if (wparam == VK_RETURN && (lparam & (1LL << 29)) != 0)
            {
                smile_toggle_fullscreen();
                return 0;
            }
            return DefWindowProcW(window, message, wparam, lparam);
        case WM_KEYDOWN:
        {
            int virtual_key = (int)wparam;
            if (virtual_key >= 0 && virtual_key < 256)
                smile_held[virtual_key] = 1;
            if ((lparam & (1LL << 30)) == 0)
                smile_queue_key(smile_map_key(0, (WORD)virtual_key));
            return 0;
        }
        case WM_SYSKEYUP:
            return DefWindowProcW(window, message, wparam, lparam);
        case WM_KEYUP:
        {
            int virtual_key = (int)wparam;
            if (virtual_key >= 0 && virtual_key < 256)
                smile_held[virtual_key] = 0;
            return 0;
        }
        case WM_KILLFOCUS:
            smile_zero_memory(smile_held, sizeof(smile_held));
            return 0;
        case WM_CLOSE:
            DestroyWindow(window);
            return 0;
        case WM_DESTROY:
            smile_window = 0;
            smile_closed = 1;
            smile_zero_memory(smile_held, sizeof(smile_held));
            smile_cleanup_graphics();
            PostQuitMessage(0);
            return 0;
    }
    return DefWindowProcW(window, message, wparam, lparam);
}

static int smile_create_back_buffer(long long width, long long height)
{
    BITMAPINFO bitmap_info;
    HDC screen;
    void* bits;
    smile_zero_memory(&bitmap_info, sizeof(bitmap_info));
    bitmap_info.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
    bitmap_info.bmiHeader.biWidth = smile_integer(width);
    bitmap_info.bmiHeader.biHeight = -smile_integer(height);
    bitmap_info.bmiHeader.biPlanes = 1;
    bitmap_info.bmiHeader.biBitCount = 32;
    bitmap_info.bmiHeader.biCompression = BI_RGB;
    screen = GetDC(0);
    smile_back_dc = CreateCompatibleDC(screen);
    smile_back_bitmap = CreateDIBSection(screen, &bitmap_info, DIB_RGB_COLORS, &bits, 0, 0);
    ReleaseDC(0, screen);
    if (smile_back_dc == 0 || smile_back_bitmap == 0)
    {
        smile_cleanup_graphics();
        return 0;
    }
    smile_old_bitmap = SelectObject(smile_back_dc, smile_back_bitmap);
    return 1;
}

void smile_game_open(const char* title, long long title_length, long long width, long long height)
{
    WNDCLASSEXW window_class;
    RECT rectangle;
    DWORD style = WS_OVERLAPPEDWINDOW;
    HINSTANCE instance = GetModuleHandleW(0);
    WCHAR* wide_title;
    UINT dpi;
    if (smile_window != 0)
        return;
    if (width <= 0) width = 960;
    if (height <= 0) height = 540;
    smile_logical_width = width;
    smile_logical_height = height;
    smile_closed = 0;
    smile_key_head = 0;
    smile_key_tail = 0;
    smile_zero_memory(smile_held, sizeof(smile_held));
    SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

    smile_zero_memory(&window_class, sizeof(window_class));
    window_class.cbSize = sizeof(window_class);
    window_class.style = CS_HREDRAW | CS_VREDRAW | CS_OWNDC;
    window_class.lpfnWndProc = smile_window_proc;
    window_class.hInstance = instance;
    window_class.hCursor = LoadCursorW(0, IDC_ARROW);
    window_class.hbrBackground = (HBRUSH)GetStockObject(BLACK_BRUSH);
    window_class.lpszClassName = smile_window_class;
    RegisterClassExW(&window_class);

    if (!smile_create_back_buffer(width, height))
    {
        smile_closed = 1;
        return;
    }
    wide_title = smile_utf8_to_wide(title, title_length);
    dpi = GetDpiForSystem();
    rectangle.left = 0;
    rectangle.top = 0;
    rectangle.right = smile_integer(width);
    rectangle.bottom = smile_integer(height);
    AdjustWindowRectExForDpi(&rectangle, style, FALSE, 0, dpi);
    smile_window = CreateWindowExW(0, smile_window_class, wide_title != 0 ? wide_title : L"SMILE 2.0",
        style, CW_USEDEFAULT, CW_USEDEFAULT, rectangle.right - rectangle.left, rectangle.bottom - rectangle.top,
        0, 0, instance, 0);
    if (wide_title != 0)
        HeapFree(GetProcessHeap(), 0, wide_title);
    if (smile_window == 0)
    {
        smile_closed = 1;
        smile_cleanup_graphics();
        return;
    }
    ShowWindow(smile_window, SW_SHOW);
    UpdateWindow(smile_window);
}

static void smile_pump_messages(void)
{
    MSG message;
    while (PeekMessageW(&message, 0, 0, 0, PM_REMOVE))
    {
        if (message.message == WM_QUIT)
        {
            smile_closed = 1;
            continue;
        }
        TranslateMessage(&message);
        DispatchMessageW(&message);
    }
}

static void smile_present_to_dc(HDC destination)
{
    RECT client;
    int client_width;
    int client_height;
    int destination_width;
    int destination_height;
    int destination_x;
    int destination_y;
    if (destination == 0 || smile_window == 0 || smile_back_dc == 0)
        return;
    GetClientRect(smile_window, &client);
    client_width = client.right - client.left;
    client_height = client.bottom - client.top;
    if (client_width <= 0 || client_height <= 0)
        return;
    if ((long long)client_width * smile_logical_height <= (long long)client_height * smile_logical_width)
    {
        destination_width = client_width;
        destination_height = (int)((long long)client_width * smile_logical_height / smile_logical_width);
    }
    else
    {
        destination_height = client_height;
        destination_width = (int)((long long)client_height * smile_logical_width / smile_logical_height);
    }
    destination_x = (client_width - destination_width) / 2;
    destination_y = (client_height - destination_height) / 2;
    PatBlt(destination, 0, 0, client_width, client_height, BLACKNESS);
    SetStretchBltMode(destination, COLORONCOLOR);
    StretchBlt(destination, destination_x, destination_y, destination_width, destination_height,
        smile_back_dc, 0, 0, smile_integer(smile_logical_width), smile_integer(smile_logical_height), SRCCOPY);
}

void smile_show_screen(void)
{
    HDC destination;
    smile_pump_messages();
    if (smile_window == 0)
        return;
    destination = GetDC(smile_window);
    smile_present_to_dc(destination);
    ReleaseDC(smile_window, destination);
}

long long smile_game_closed(void)
{
    smile_pump_messages();
    return smile_closed;
}

static void smile_toggle_fullscreen(void)
{
    MONITORINFO monitor;
    DWORD fullscreen_style;
    DWORD fullscreen_ex_style;
    if (smile_window == 0)
        return;
    if (!smile_fullscreen)
    {
        smile_windowed_style = (DWORD)GetWindowLongPtrW(smile_window, GWL_STYLE);
        smile_windowed_ex_style = (DWORD)GetWindowLongPtrW(smile_window, GWL_EXSTYLE);
        smile_windowed_placement.length = sizeof(smile_windowed_placement);
        GetWindowPlacement(smile_window, &smile_windowed_placement);
        smile_zero_memory(&monitor, sizeof(monitor));
        monitor.cbSize = sizeof(monitor);
        GetMonitorInfoW(MonitorFromWindow(smile_window, MONITOR_DEFAULTTONEAREST), &monitor);
        fullscreen_style = (smile_windowed_style &
            ~(WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX)) | WS_POPUP;
        fullscreen_ex_style = smile_windowed_ex_style &
            ~(WS_EX_DLGMODALFRAME | WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE | WS_EX_STATICEDGE);
        SetWindowLongPtrW(smile_window, GWL_STYLE, (LONG_PTR)fullscreen_style);
        SetWindowLongPtrW(smile_window, GWL_EXSTYLE, (LONG_PTR)fullscreen_ex_style);
        SetWindowPos(smile_window, HWND_TOPMOST, monitor.rcMonitor.left, monitor.rcMonitor.top,
            monitor.rcMonitor.right - monitor.rcMonitor.left, monitor.rcMonitor.bottom - monitor.rcMonitor.top,
            SWP_FRAMECHANGED | SWP_NOOWNERZORDER | SWP_SHOWWINDOW);
        smile_fullscreen = 1;
    }
    else
    {
        SetWindowLongPtrW(smile_window, GWL_STYLE, (LONG_PTR)smile_windowed_style);
        SetWindowLongPtrW(smile_window, GWL_EXSTYLE, (LONG_PTR)smile_windowed_ex_style);
        SetWindowPlacement(smile_window, &smile_windowed_placement);
        SetWindowPos(smile_window, HWND_NOTOPMOST, 0, 0, 0, 0,
            SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOOWNERZORDER | SWP_SHOWWINDOW);
        smile_fullscreen = 0;
    }
    InvalidateRect(smile_window, 0, FALSE);
}

void smile_game_clear(long long color)
{
    RECT rectangle;
    HBRUSH brush;
    if (smile_back_dc == 0)
        return;
    rectangle.left = 0;
    rectangle.top = 0;
    rectangle.right = smile_integer(smile_logical_width);
    rectangle.bottom = smile_integer(smile_logical_height);
    brush = CreateSolidBrush((COLORREF)color);
    FillRect(smile_back_dc, &rectangle, brush);
    DeleteObject(brush);
}

static void smile_rectangle(long long x, long long y, long long width, long long height, long long radius, long long color, int fill, int rounded)
{
    HGDIOBJ old_pen;
    HGDIOBJ old_brush;
    HPEN pen = CreatePen(PS_SOLID, 1, (COLORREF)color);
    HBRUSH brush = CreateSolidBrush((COLORREF)color);
    if (smile_back_dc == 0)
    {
        DeleteObject(pen);
        DeleteObject(brush);
        return;
    }
    old_pen = SelectObject(smile_back_dc, fill ? GetStockObject(NULL_PEN) : pen);
    old_brush = SelectObject(smile_back_dc, fill ? brush : GetStockObject(NULL_BRUSH));
    if (rounded)
        RoundRect(smile_back_dc, smile_integer(x), smile_integer(y), smile_integer(x + width), smile_integer(y + height), smile_integer(radius * 2), smile_integer(radius * 2));
    else
        Rectangle(smile_back_dc, smile_integer(x), smile_integer(y), smile_integer(x + width), smile_integer(y + height));
    SelectObject(smile_back_dc, old_brush);
    SelectObject(smile_back_dc, old_pen);
    DeleteObject(brush);
    DeleteObject(pen);
}

void smile_fill_rectangle(long long x, long long y, long long width, long long height, long long color)
{ smile_rectangle(x, y, width, height, 0, color, 1, 0); }

void smile_draw_rectangle(long long x, long long y, long long width, long long height, long long color)
{ smile_rectangle(x, y, width, height, 0, color, 0, 0); }

void smile_fill_rounded_rectangle(long long x, long long y, long long width, long long height, long long radius, long long color)
{ smile_rectangle(x, y, width, height, radius, color, 1, 1); }

void smile_draw_rounded_rectangle(long long x, long long y, long long width, long long height, long long radius, long long color)
{ smile_rectangle(x, y, width, height, radius, color, 0, 1); }

static void smile_circle(long long x, long long y, long long radius, long long color, int fill)
{
    HGDIOBJ old_pen;
    HGDIOBJ old_brush;
    HPEN pen = CreatePen(PS_SOLID, 1, (COLORREF)color);
    HBRUSH brush = CreateSolidBrush((COLORREF)color);
    if (smile_back_dc == 0)
    {
        DeleteObject(pen);
        DeleteObject(brush);
        return;
    }
    old_pen = SelectObject(smile_back_dc, fill ? GetStockObject(NULL_PEN) : pen);
    old_brush = SelectObject(smile_back_dc, fill ? brush : GetStockObject(NULL_BRUSH));
    Ellipse(smile_back_dc, smile_integer(x - radius), smile_integer(y - radius), smile_integer(x + radius), smile_integer(y + radius));
    SelectObject(smile_back_dc, old_brush);
    SelectObject(smile_back_dc, old_pen);
    DeleteObject(brush);
    DeleteObject(pen);
}

void smile_fill_circle(long long x, long long y, long long radius, long long color)
{ smile_circle(x, y, radius, color, 1); }

void smile_draw_circle(long long x, long long y, long long radius, long long color)
{ smile_circle(x, y, radius, color, 0); }

void smile_draw_line(long long x1, long long y1, long long x2, long long y2, long long color)
{
    HGDIOBJ old_pen;
    HPEN pen;
    if (smile_back_dc == 0)
        return;
    pen = CreatePen(PS_SOLID, 1, (COLORREF)color);
    old_pen = SelectObject(smile_back_dc, pen);
    MoveToEx(smile_back_dc, smile_integer(x1), smile_integer(y1), 0);
    LineTo(smile_back_dc, smile_integer(x2), smile_integer(y2));
    SelectObject(smile_back_dc, old_pen);
    DeleteObject(pen);
}

static void smile_draw_wide(const WCHAR* text, int length, long long x, long long y, long long size, long long color, long long centered)
{
    HFONT font;
    HGDIOBJ old_font;
    if (smile_back_dc == 0 || text == 0 || length <= 0)
        return;
    font = CreateFontW(-smile_integer(size), 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, DEFAULT_CHARSET,
        OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, CLEARTYPE_QUALITY, FIXED_PITCH | FF_MODERN, L"Consolas");
    old_font = SelectObject(smile_back_dc, font);
    SetBkMode(smile_back_dc, TRANSPARENT);
    SetTextColor(smile_back_dc, (COLORREF)color);
    SetTextAlign(smile_back_dc, (UINT)((centered != 0 ? TA_CENTER : TA_LEFT) | TA_TOP));
    TextOutW(smile_back_dc, smile_integer(x), smile_integer(y), text, length);
    SelectObject(smile_back_dc, old_font);
    DeleteObject(font);
}

void smile_draw_text(const char* text, long long length, long long x, long long y, long long size, long long color, long long centered)
{
    WCHAR* wide = smile_utf8_to_wide(text, length);
    if (wide != 0)
    {
        smile_draw_wide(wide, lstrlenW(wide), x, y, size, color, centered);
        HeapFree(GetProcessHeap(), 0, wide);
    }
}

void smile_draw_number(long long value, long long x, long long y, long long size, long long color)
{
    WCHAR buffer[32];
    char narrow[32];
    int index;
    int length = smile_format_number(value, narrow, (int)sizeof(narrow));
    for (index = 0; index < length; index++)
        buffer[index] = (WCHAR)(unsigned char)narrow[index];
    buffer[length] = 0;
    smile_draw_wide(buffer, length, x, y, size, color, 0);
}

static int smile_is_absolute_path(const WCHAR* path)
{
    return path != 0 && ((path[0] != 0 && path[1] == L':') || (path[0] == L'\\' && path[1] == L'\\'));
}

static void smile_append(WCHAR* destination, int capacity, const WCHAR* source)
{
    int index = lstrlenW(destination);
    while (source != 0 && *source != 0 && index + 1 < capacity)
        destination[index++] = *source++;
    destination[index] = 0;
}

void smile_play_sound(const char* path, long long length)
{
    WCHAR full_path[2048];
    WCHAR* wide = smile_utf8_to_wide(path, length);
    WCHAR* slash;
    if (wide == 0)
        return;
    full_path[0] = 0;
    if (smile_is_absolute_path(wide))
    {
        smile_append(full_path, (int)(sizeof(full_path) / sizeof(full_path[0])), wide);
    }
    else
    {
        GetModuleFileNameW(0, full_path, (DWORD)(sizeof(full_path) / sizeof(full_path[0])));
        slash = full_path + lstrlenW(full_path);
        while (slash > full_path && slash[-1] != L'\\' && slash[-1] != L'/')
            --slash;
        *slash = 0;
        smile_append(full_path, (int)(sizeof(full_path) / sizeof(full_path[0])), wide);
    }
    PlaySoundW(full_path, 0, SND_FILENAME | SND_ASYNC | SND_NODEFAULT);
    HeapFree(GetProcessHeap(), 0, wide);
}

void smile_stop_sound(void)
{
    PlaySoundW(0, 0, 0);
}

static void smile_sanitize(WCHAR* destination, int capacity, const WCHAR* source)
{
    int index = 0;
    while (source != 0 && *source != 0 && index + 1 < capacity)
    {
        WCHAR character = *source++;
        if ((character >= L'a' && character <= L'z') || (character >= L'A' && character <= L'Z') ||
            (character >= L'0' && character <= L'9') || character == L'-' || character == L'_')
            destination[index++] = character;
        else
            destination[index++] = L'_';
    }
    if (index == 0 && capacity > 1)
        destination[index++] = L'_';
    destination[index] = 0;
}

static int smile_storage_path(const char* key, long long key_length, WCHAR* path, int capacity)
{
    PWSTR local_app_data = 0;
    WCHAR executable[1024];
    WCHAR game_name[256];
    WCHAR key_name[256];
    WCHAR* file_name;
    WCHAR* extension;
    WCHAR* wide_key;
    HRESULT result = SHGetKnownFolderPath(&FOLDERID_LocalAppData, KF_FLAG_CREATE, 0, &local_app_data);
    if (FAILED(result) || local_app_data == 0)
        return 0;
    path[0] = 0;
    smile_append(path, capacity, local_app_data);
    CoTaskMemFree(local_app_data);
    smile_append(path, capacity, L"\\SMILE 2.0\\Games\\");
    GetModuleFileNameW(0, executable, (DWORD)(sizeof(executable) / sizeof(executable[0])));
    file_name = executable + lstrlenW(executable);
    while (file_name > executable && file_name[-1] != L'\\' && file_name[-1] != L'/')
        --file_name;
    extension = file_name + lstrlenW(file_name);
    while (extension > file_name && extension[-1] != L'.')
        --extension;
    if (extension > file_name)
        extension[-1] = 0;
    smile_sanitize(game_name, (int)(sizeof(game_name) / sizeof(game_name[0])), file_name);
    smile_append(path, capacity, game_name);
    SHCreateDirectoryExW(0, path, 0);
    smile_append(path, capacity, L"\\");
    wide_key = smile_utf8_to_wide(key, key_length);
    if (wide_key == 0)
        return 0;
    smile_sanitize(key_name, (int)(sizeof(key_name) / sizeof(key_name[0])), wide_key);
    HeapFree(GetProcessHeap(), 0, wide_key);
    smile_append(path, capacity, key_name);
    smile_append(path, capacity, L".txt");
    return 1;
}

long long smile_load_value(const char* key, long long key_length, long long default_value)
{
    WCHAR path[2048];
    HANDLE file;
    char buffer[64];
    DWORD read = 0;
    int index = 0;
    int negative = 0;
    unsigned long long magnitude = 0;
    if (!smile_storage_path(key, key_length, path, (int)(sizeof(path) / sizeof(path[0]))))
        return default_value;
    file = CreateFileW(path, GENERIC_READ, FILE_SHARE_READ, 0, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
    if (file == INVALID_HANDLE_VALUE)
        return default_value;
    if (!ReadFile(file, buffer, sizeof(buffer) - 1, &read, 0))
    {
        CloseHandle(file);
        return default_value;
    }
    CloseHandle(file);
    buffer[read] = 0;
    while (index < (int)read && (buffer[index] == ' ' || buffer[index] == '\t' || buffer[index] == '\r' || buffer[index] == '\n')) ++index;
    if (index < (int)read && (buffer[index] == '-' || buffer[index] == '+')) negative = buffer[index++] == '-';
    if (index >= (int)read || buffer[index] < '0' || buffer[index] > '9') return default_value;
    while (index < (int)read && buffer[index] >= '0' && buffer[index] <= '9')
    {
        unsigned int digit = (unsigned int)(buffer[index++] - '0');
        if (magnitude > ((unsigned long long)LLONG_MAX + (negative ? 1ULL : 0ULL) - digit) / 10ULL)
            return default_value;
        magnitude = magnitude * 10ULL + digit;
    }
    while (index < (int)read && (buffer[index] == ' ' || buffer[index] == '\t' || buffer[index] == '\r' || buffer[index] == '\n')) ++index;
    if (index != (int)read) return default_value;
    if (negative)
        return magnitude == (unsigned long long)LLONG_MAX + 1ULL ? LLONG_MIN : -(long long)magnitude;
    return (long long)magnitude;
}

void smile_save_value(const char* key, long long key_length, long long value)
{
    WCHAR path[2048];
    HANDLE file;
    char buffer[32];
    int length;
    DWORD written;
    if (!smile_storage_path(key, key_length, path, (int)(sizeof(path) / sizeof(path[0]))))
        return;
    file = CreateFileW(path, GENERIC_WRITE, 0, 0, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, 0);
    if (file == INVALID_HANDLE_VALUE)
        return;
    length = smile_format_number(value, buffer, (int)sizeof(buffer));
    WriteFile(file, buffer, (DWORD)length, &written, 0);
    CloseHandle(file);
}
