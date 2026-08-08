#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <limits.h>
#include "graphics_gdi.h"

typedef struct SmileGdiState
{
    HWND window;
    HDC back_dc;
    HBITMAP back_bitmap;
    HGDIOBJ old_bitmap;
    long long logical_width;
    long long logical_height;
    int vsync_enabled;
} SmileGdiState;

static SmileGdiState smile_gdi;

static int smile_gdi_integer(long long value)
{
    if (value < INT_MIN) return INT_MIN;
    if (value > INT_MAX) return INT_MAX;
    return (int)value;
}

static WCHAR* smile_gdi_utf8_to_wide(const char* text, long long length)
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

static int smile_gdi_format_number(long long value, WCHAR* buffer, int capacity)
{
    WCHAR temporary[32];
    int count = 0;
    int output = 0;
    unsigned long long magnitude;
    if (capacity <= 0)
        return 0;
    magnitude = value < 0 ? (unsigned long long)(-(value + 1)) + 1ULL : (unsigned long long)value;
    do
    {
        temporary[count++] = (WCHAR)(L'0' + (magnitude % 10));
        magnitude /= 10;
    }
    while (magnitude != 0 && count < (int)(sizeof(temporary) / sizeof(temporary[0])));
    if (value < 0 && output < capacity)
        buffer[output++] = L'-';
    while (count > 0 && output < capacity)
        buffer[output++] = temporary[--count];
    return output;
}

static void smile_gdi_cleanup(SmileGdiState* state)
{
    if (state->back_dc != 0 && state->old_bitmap != 0)
        SelectObject(state->back_dc, state->old_bitmap);
    if (state->back_bitmap != 0)
        DeleteObject(state->back_bitmap);
    if (state->back_dc != 0)
        DeleteDC(state->back_dc);
    state->back_dc = 0;
    state->back_bitmap = 0;
    state->old_bitmap = 0;
}

static int smile_gdi_create_back_buffer(SmileGdiState* state, long long width, long long height)
{
    BITMAPINFO bitmap_info;
    HDC screen;
    void* bits;
    ZeroMemory(&bitmap_info, sizeof(bitmap_info));
    bitmap_info.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
    bitmap_info.bmiHeader.biWidth = smile_gdi_integer(width);
    bitmap_info.bmiHeader.biHeight = -smile_gdi_integer(height);
    bitmap_info.bmiHeader.biPlanes = 1;
    bitmap_info.bmiHeader.biBitCount = 32;
    bitmap_info.bmiHeader.biCompression = BI_RGB;
    screen = GetDC(0);
    state->back_dc = CreateCompatibleDC(screen);
    state->back_bitmap = CreateDIBSection(screen, &bitmap_info, DIB_RGB_COLORS, &bits, 0, 0);
    ReleaseDC(0, screen);
    if (state->back_dc == 0 || state->back_bitmap == 0)
    {
        smile_gdi_cleanup(state);
        return 0;
    }
    state->old_bitmap = SelectObject(state->back_dc, state->back_bitmap);
    return 1;
}

static void smile_gdi_present_to_dc(const SmileGdiState* state, HDC destination)
{
    RECT client;
    int client_width;
    int client_height;
    int destination_width;
    int destination_height;
    int destination_x;
    int destination_y;
    if (destination == 0 || state->window == 0 || state->back_dc == 0)
        return;
    GetClientRect(state->window, &client);
    client_width = client.right - client.left;
    client_height = client.bottom - client.top;
    if (client_width <= 0 || client_height <= 0)
        return;
    if ((long long)client_width * state->logical_height <= (long long)client_height * state->logical_width)
    {
        destination_width = client_width;
        destination_height = (int)((long long)client_width * state->logical_height / state->logical_width);
    }
    else
    {
        destination_height = client_height;
        destination_width = (int)((long long)client_height * state->logical_width / state->logical_height);
    }
    destination_x = (client_width - destination_width) / 2;
    destination_y = (client_height - destination_height) / 2;
    if (destination_x > 0)
    {
        PatBlt(destination, 0, 0, destination_x, client_height, BLACKNESS);
        PatBlt(destination, destination_x + destination_width, 0,
            client_width - destination_x - destination_width, client_height, BLACKNESS);
    }
    if (destination_y > 0)
    {
        PatBlt(destination, 0, 0, client_width, destination_y, BLACKNESS);
        PatBlt(destination, 0, destination_y + destination_height,
            client_width, client_height - destination_y - destination_height, BLACKNESS);
    }
    SetStretchBltMode(destination, COLORONCOLOR);
    StretchBlt(destination, destination_x, destination_y, destination_width, destination_height,
        state->back_dc, 0, 0, smile_gdi_integer(state->logical_width),
        smile_gdi_integer(state->logical_height), SRCCOPY);
}

static int smile_gdi_initialize(SmileGraphicsBackend* backend, void* native_window,
    long long logical_width, long long logical_height, int vsync_enabled,
    char* error, int error_capacity)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    (void)error;
    (void)error_capacity;
    ZeroMemory(state, sizeof(*state));
    state->window = (HWND)native_window;
    state->logical_width = logical_width;
    state->logical_height = logical_height;
    state->vsync_enabled = vsync_enabled;
    return smile_gdi_create_back_buffer(state, logical_width, logical_height);
}

static void smile_gdi_resize(SmileGraphicsBackend* backend, int physical_width, int physical_height)
{
    (void)backend;
    (void)physical_width;
    (void)physical_height;
}

static void smile_gdi_begin_frame(SmileGraphicsBackend* backend)
{
    (void)backend;
}

static void smile_gdi_clear(SmileGraphicsBackend* backend, long long color)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    RECT rectangle;
    HBRUSH brush;
    if (state->back_dc == 0)
        return;
    rectangle.left = 0;
    rectangle.top = 0;
    rectangle.right = smile_gdi_integer(state->logical_width);
    rectangle.bottom = smile_gdi_integer(state->logical_height);
    brush = CreateSolidBrush((COLORREF)color);
    FillRect(state->back_dc, &rectangle, brush);
    DeleteObject(brush);
}

static void smile_gdi_rectangle(SmileGraphicsBackend* backend, long long x, long long y,
    long long width, long long height, long long radius, long long color, int fill, int rounded)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    HGDIOBJ old_pen;
    HGDIOBJ old_brush;
    HPEN pen = CreatePen(PS_SOLID, 1, (COLORREF)color);
    HBRUSH brush = CreateSolidBrush((COLORREF)color);
    if (state->back_dc == 0)
    {
        DeleteObject(pen);
        DeleteObject(brush);
        return;
    }
    old_pen = SelectObject(state->back_dc, fill ? GetStockObject(NULL_PEN) : pen);
    old_brush = SelectObject(state->back_dc, fill ? brush : GetStockObject(NULL_BRUSH));
    if (rounded)
        RoundRect(state->back_dc, smile_gdi_integer(x), smile_gdi_integer(y),
            smile_gdi_integer(x + width), smile_gdi_integer(y + height),
            smile_gdi_integer(radius * 2), smile_gdi_integer(radius * 2));
    else
        Rectangle(state->back_dc, smile_gdi_integer(x), smile_gdi_integer(y),
            smile_gdi_integer(x + width), smile_gdi_integer(y + height));
    SelectObject(state->back_dc, old_brush);
    SelectObject(state->back_dc, old_pen);
    DeleteObject(brush);
    DeleteObject(pen);
}

static void smile_gdi_fill_rectangle(SmileGraphicsBackend* backend, long long x, long long y,
    long long width, long long height, long long color)
{ smile_gdi_rectangle(backend, x, y, width, height, 0, color, 1, 0); }

static void smile_gdi_draw_rectangle(SmileGraphicsBackend* backend, long long x, long long y,
    long long width, long long height, long long color)
{ smile_gdi_rectangle(backend, x, y, width, height, 0, color, 0, 0); }

static void smile_gdi_fill_rounded_rectangle(SmileGraphicsBackend* backend, long long x,
    long long y, long long width, long long height, long long radius, long long color)
{ smile_gdi_rectangle(backend, x, y, width, height, radius, color, 1, 1); }

static void smile_gdi_draw_rounded_rectangle(SmileGraphicsBackend* backend, long long x,
    long long y, long long width, long long height, long long radius, long long color)
{ smile_gdi_rectangle(backend, x, y, width, height, radius, color, 0, 1); }

static void smile_gdi_circle(SmileGraphicsBackend* backend, long long x, long long y,
    long long radius, long long color, int fill)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    HGDIOBJ old_pen;
    HGDIOBJ old_brush;
    HPEN pen = CreatePen(PS_SOLID, 1, (COLORREF)color);
    HBRUSH brush = CreateSolidBrush((COLORREF)color);
    if (state->back_dc == 0)
    {
        DeleteObject(pen);
        DeleteObject(brush);
        return;
    }
    old_pen = SelectObject(state->back_dc, fill ? GetStockObject(NULL_PEN) : pen);
    old_brush = SelectObject(state->back_dc, fill ? brush : GetStockObject(NULL_BRUSH));
    Ellipse(state->back_dc, smile_gdi_integer(x - radius), smile_gdi_integer(y - radius),
        smile_gdi_integer(x + radius), smile_gdi_integer(y + radius));
    SelectObject(state->back_dc, old_brush);
    SelectObject(state->back_dc, old_pen);
    DeleteObject(brush);
    DeleteObject(pen);
}

static void smile_gdi_fill_circle(SmileGraphicsBackend* backend, long long x, long long y,
    long long radius, long long color)
{ smile_gdi_circle(backend, x, y, radius, color, 1); }

static void smile_gdi_draw_circle(SmileGraphicsBackend* backend, long long x, long long y,
    long long radius, long long color)
{ smile_gdi_circle(backend, x, y, radius, color, 0); }

static void smile_gdi_draw_line(SmileGraphicsBackend* backend, long long x1, long long y1,
    long long x2, long long y2, long long color)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    HGDIOBJ old_pen;
    HPEN pen;
    if (state->back_dc == 0)
        return;
    pen = CreatePen(PS_SOLID, 1, (COLORREF)color);
    old_pen = SelectObject(state->back_dc, pen);
    MoveToEx(state->back_dc, smile_gdi_integer(x1), smile_gdi_integer(y1), 0);
    LineTo(state->back_dc, smile_gdi_integer(x2), smile_gdi_integer(y2));
    SelectObject(state->back_dc, old_pen);
    DeleteObject(pen);
}

static void smile_gdi_draw_wide(SmileGdiState* state, const WCHAR* text, int length,
    long long x, long long y, long long size, long long color, long long centered)
{
    HFONT font;
    HGDIOBJ old_font;
    if (state->back_dc == 0 || text == 0 || length <= 0)
        return;
    font = CreateFontW(-smile_gdi_integer(size), 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE,
        DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, CLEARTYPE_QUALITY,
        FIXED_PITCH | FF_MODERN, L"Consolas");
    old_font = SelectObject(state->back_dc, font);
    SetBkMode(state->back_dc, TRANSPARENT);
    SetTextColor(state->back_dc, (COLORREF)color);
    SetTextAlign(state->back_dc, (UINT)((centered != 0 ? TA_CENTER : TA_LEFT) | TA_TOP));
    TextOutW(state->back_dc, smile_gdi_integer(x), smile_gdi_integer(y), text, length);
    SelectObject(state->back_dc, old_font);
    DeleteObject(font);
}

static void smile_gdi_draw_text(SmileGraphicsBackend* backend, const char* text,
    long long length, long long x, long long y, long long size, long long color,
    long long centered)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    WCHAR* wide = smile_gdi_utf8_to_wide(text, length);
    if (wide != 0)
    {
        smile_gdi_draw_wide(state, wide, lstrlenW(wide), x, y, size, color, centered);
        HeapFree(GetProcessHeap(), 0, wide);
    }
}

static void smile_gdi_draw_number(SmileGraphicsBackend* backend, long long value,
    long long x, long long y, long long size, long long color)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    WCHAR buffer[32];
    int length = smile_gdi_format_number(value, buffer, (int)(sizeof(buffer) / sizeof(buffer[0])));
    smile_gdi_draw_wide(state, buffer, length, x, y, size, color, 0);
}

static int smile_gdi_present(SmileGraphicsBackend* backend)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    HDC destination;
    if (state->window == 0)
        return 0;
    destination = GetDC(state->window);
    smile_gdi_present_to_dc(state, destination);
    ReleaseDC(state->window, destination);
    return 1;
}

static void smile_gdi_repaint(SmileGraphicsBackend* backend, void* native_paint_context)
{
    smile_gdi_present_to_dc((const SmileGdiState*)backend->state, (HDC)native_paint_context);
}

static void smile_gdi_on_fullscreen_changed(SmileGraphicsBackend* backend, int fullscreen)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    (void)fullscreen;
    if (state->window != 0)
        InvalidateRect(state->window, 0, FALSE);
}

static void smile_gdi_on_dpi_changed(SmileGraphicsBackend* backend, unsigned int dpi)
{
    (void)backend;
    (void)dpi;
}

static void smile_gdi_shutdown(SmileGraphicsBackend* backend)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    smile_gdi_cleanup(state);
    state->window = 0;
}

static const char* smile_gdi_get_backend_name(const SmileGraphicsBackend* backend)
{
    (void)backend;
    return "GDI";
}

static void smile_gdi_get_diagnostics(const SmileGraphicsBackend* backend,
    SmileGraphicsBackendDiagnostics* diagnostics)
{
    const SmileGdiState* state = (const SmileGdiState*)backend->state;
    RECT client;
    int viewport_width;
    int viewport_height;
    ZeroMemory(&client, sizeof(client));
    if (state->window != 0)
        GetClientRect(state->window, &client);
    diagnostics->physical_width = client.right - client.left;
    diagnostics->physical_height = client.bottom - client.top;
    if (diagnostics->physical_width > 0 && diagnostics->physical_height > 0 &&
        state->logical_width > 0 && state->logical_height > 0)
    {
        if ((long long)diagnostics->physical_width * state->logical_height <=
            (long long)diagnostics->physical_height * state->logical_width)
        {
            viewport_width = diagnostics->physical_width;
            viewport_height = (int)((long long)diagnostics->physical_width *
                state->logical_height / state->logical_width);
        }
        else
        {
            viewport_height = diagnostics->physical_height;
            viewport_width = (int)((long long)diagnostics->physical_height *
                state->logical_width / state->logical_height);
        }
    }
    else
    {
        viewport_width = 0;
        viewport_height = 0;
    }
    diagnostics->viewport_x = (double)(diagnostics->physical_width - viewport_width) / 2.0;
    diagnostics->viewport_y = (double)(diagnostics->physical_height - viewport_height) / 2.0;
    diagnostics->viewport_width = (double)viewport_width;
    diagnostics->viewport_height = (double)viewport_height;
    diagnostics->scale = state->logical_width > 0
        ? (double)viewport_width / (double)state->logical_width : 0.0;
    diagnostics->pacing_mode = "Legacy GDI presentation (unsynchronized)";
    diagnostics->device_removal_reason = "None";
}

static const SmileGraphicsBackendVTable smile_gdi_operations =
{
    smile_gdi_initialize,
    smile_gdi_resize,
    smile_gdi_begin_frame,
    smile_gdi_clear,
    smile_gdi_fill_rectangle,
    smile_gdi_draw_rectangle,
    smile_gdi_fill_rounded_rectangle,
    smile_gdi_draw_rounded_rectangle,
    smile_gdi_fill_circle,
    smile_gdi_draw_circle,
    smile_gdi_draw_line,
    smile_gdi_draw_text,
    smile_gdi_draw_number,
    smile_gdi_present,
    smile_gdi_repaint,
    smile_gdi_on_fullscreen_changed,
    smile_gdi_on_dpi_changed,
    smile_gdi_shutdown,
    smile_gdi_get_backend_name,
    smile_gdi_get_diagnostics
};

void smile_graphics_gdi_create(SmileGraphicsBackend* backend)
{
    backend->operations = &smile_gdi_operations;
    backend->state = &smile_gdi;
}
