#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <dwmapi.h>
#include <limits.h>
#include "graphics_common.h"
#include "graphics_gdi.h"

#define SMILE_GDI_BRUSH_CACHE_SIZE 32
#define SMILE_GDI_PEN_CACHE_SIZE 64
#define SMILE_GDI_FONT_CACHE_SIZE 64

typedef struct SmileGdiBrushCacheEntry
{
    COLORREF color;
    HBRUSH handle;
} SmileGdiBrushCacheEntry;

typedef struct SmileGdiPenCacheEntry
{
    COLORREF color;
    int width;
    HPEN handle;
} SmileGdiPenCacheEntry;

typedef struct SmileGdiFontCacheEntry
{
    int height;
    int quality;
    HFONT handle;
} SmileGdiFontCacheEntry;

typedef struct SmileGdiState
{
    HWND window;
    HDC back_dc;
    HBITMAP back_bitmap;
    HGDIOBJ old_bitmap;
    long long logical_width;
    long long logical_height;
    int physical_width;
    int physical_height;
    int vsync_enabled;
    int dwm_flush_enabled;
    SmileGraphicsViewport viewport;
    SmileGdiBrushCacheEntry brushes[SMILE_GDI_BRUSH_CACHE_SIZE];
    SmileGdiPenCacheEntry pens[SMILE_GDI_PEN_CACHE_SIZE];
    SmileGdiFontCacheEntry fonts[SMILE_GDI_FONT_CACHE_SIZE];
    unsigned int next_brush;
    unsigned int next_pen;
    unsigned int next_font;
} SmileGdiState;

static SmileGdiState smile_gdi;

static void smile_gdi_zero_memory(void* memory, SIZE_T length)
{
    volatile unsigned char* current = (volatile unsigned char*)memory;
    while (length-- != 0)
        *current++ = 0;
}

static int smile_gdi_integer(long long value)
{
    if (value < INT_MIN) return INT_MIN;
    if (value > INT_MAX) return INT_MAX;
    return (int)value;
}

static int smile_gdi_positive_pixel(double value)
{
    int result = smile_graphics_round_pixel(value);
    return result < 1 ? 1 : result;
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

static void smile_gdi_release_buffer(SmileGdiState* state)
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
    state->physical_width = 0;
    state->physical_height = 0;
}

static void smile_gdi_release_size_dependent_cache(SmileGdiState* state)
{
    int index;
    for (index = 0; index < SMILE_GDI_PEN_CACHE_SIZE; index++)
    {
        if (state->pens[index].handle != 0)
            DeleteObject(state->pens[index].handle);
        smile_gdi_zero_memory(&state->pens[index], sizeof(state->pens[index]));
    }
    for (index = 0; index < SMILE_GDI_FONT_CACHE_SIZE; index++)
    {
        if (state->fonts[index].handle != 0)
            DeleteObject(state->fonts[index].handle);
        smile_gdi_zero_memory(&state->fonts[index], sizeof(state->fonts[index]));
    }
    state->next_pen = 0;
    state->next_font = 0;
}

static void smile_gdi_release_cache(SmileGdiState* state)
{
    int index;
    smile_gdi_release_size_dependent_cache(state);
    for (index = 0; index < SMILE_GDI_BRUSH_CACHE_SIZE; index++)
    {
        if (state->brushes[index].handle != 0)
            DeleteObject(state->brushes[index].handle);
        smile_gdi_zero_memory(&state->brushes[index], sizeof(state->brushes[index]));
    }
    state->next_brush = 0;
}

static HBRUSH smile_gdi_brush(SmileGdiState* state, COLORREF color)
{
    unsigned int index;
    for (index = 0; index < SMILE_GDI_BRUSH_CACHE_SIZE; index++)
    {
        if (state->brushes[index].handle != 0 && state->brushes[index].color == color)
            return state->brushes[index].handle;
    }
    index = state->next_brush++ % SMILE_GDI_BRUSH_CACHE_SIZE;
    if (state->brushes[index].handle != 0)
        DeleteObject(state->brushes[index].handle);
    state->brushes[index].color = color;
    state->brushes[index].handle = CreateSolidBrush(color);
    return state->brushes[index].handle;
}

static HPEN smile_gdi_pen(SmileGdiState* state, COLORREF color, int width)
{
    unsigned int index;
    for (index = 0; index < SMILE_GDI_PEN_CACHE_SIZE; index++)
    {
        if (state->pens[index].handle != 0 && state->pens[index].color == color &&
            state->pens[index].width == width)
            return state->pens[index].handle;
    }
    index = state->next_pen++ % SMILE_GDI_PEN_CACHE_SIZE;
    if (state->pens[index].handle != 0)
        DeleteObject(state->pens[index].handle);
    state->pens[index].color = color;
    state->pens[index].width = width;
    state->pens[index].handle = CreatePen(PS_SOLID, width, color);
    return state->pens[index].handle;
}

static HFONT smile_gdi_create_font(int height, int* quality)
{
    HFONT font;
    *quality = CLEARTYPE_NATURAL_QUALITY;
    font = CreateFontW(-height, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, DEFAULT_CHARSET,
        OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, *quality, FIXED_PITCH | FF_MODERN, L"Consolas");
    if (font == 0)
    {
        *quality = CLEARTYPE_QUALITY;
        font = CreateFontW(-height, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, DEFAULT_CHARSET,
            OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, *quality, FIXED_PITCH | FF_MODERN, L"Consolas");
    }
    if (font == 0)
    {
        *quality = ANTIALIASED_QUALITY;
        font = CreateFontW(-height, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, DEFAULT_CHARSET,
            OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, *quality, FIXED_PITCH | FF_MODERN, L"Consolas");
    }
    return font;
}

static HFONT smile_gdi_font(SmileGdiState* state, int height)
{
    unsigned int index;
    int quality;
    HFONT font;
    for (index = 0; index < SMILE_GDI_FONT_CACHE_SIZE; index++)
    {
        if (state->fonts[index].handle != 0 && state->fonts[index].height == height)
            return state->fonts[index].handle;
    }
    index = state->next_font++ % SMILE_GDI_FONT_CACHE_SIZE;
    if (state->fonts[index].handle != 0)
        DeleteObject(state->fonts[index].handle);
    font = smile_gdi_create_font(height, &quality);
    state->fonts[index].height = height;
    state->fonts[index].quality = quality;
    state->fonts[index].handle = font;
    return font;
}

static int smile_gdi_create_back_buffer(SmileGdiState* state, int width, int height)
{
    BITMAPINFO bitmap_info;
    HDC screen;
    HDC new_dc;
    HBITMAP new_bitmap;
    HGDIOBJ new_old_bitmap;
    void* bits;
    if (width <= 0 || height <= 0)
        return 0;
    smile_gdi_zero_memory(&bitmap_info, sizeof(bitmap_info));
    bitmap_info.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
    bitmap_info.bmiHeader.biWidth = width;
    bitmap_info.bmiHeader.biHeight = -height;
    bitmap_info.bmiHeader.biPlanes = 1;
    bitmap_info.bmiHeader.biBitCount = 32;
    bitmap_info.bmiHeader.biCompression = BI_RGB;
    screen = GetDC(0);
    new_dc = CreateCompatibleDC(screen);
    new_bitmap = CreateDIBSection(screen, &bitmap_info, DIB_RGB_COLORS, &bits, 0, 0);
    ReleaseDC(0, screen);
    if (new_dc == 0 || new_bitmap == 0)
    {
        if (new_bitmap != 0) DeleteObject(new_bitmap);
        if (new_dc != 0) DeleteDC(new_dc);
        return 0;
    }
    new_old_bitmap = SelectObject(new_dc, new_bitmap);
    smile_gdi_release_buffer(state);
    state->back_dc = new_dc;
    state->back_bitmap = new_bitmap;
    state->old_bitmap = new_old_bitmap;
    state->physical_width = width;
    state->physical_height = height;
    smile_graphics_calculate_viewport(state->logical_width, state->logical_height,
        width, height, &state->viewport);
    smile_gdi_release_size_dependent_cache(state);
    PatBlt(state->back_dc, 0, 0, width, height, BLACKNESS);
    return 1;
}

static void smile_gdi_current_client(const SmileGdiState* state, int* width, int* height)
{
    RECT client;
    smile_gdi_zero_memory(&client, sizeof(client));
    if (state->window != 0)
        GetClientRect(state->window, &client);
    *width = client.right - client.left;
    *height = client.bottom - client.top;
}

static void smile_gdi_ensure_client_buffer(SmileGdiState* state)
{
    int width;
    int height;
    smile_gdi_current_client(state, &width, &height);
    if (width > 0 && height > 0 &&
        (width != state->physical_width || height != state->physical_height))
        smile_gdi_create_back_buffer(state, width, height);
}

static int smile_gdi_initialize(SmileGraphicsBackend* backend, void* native_window,
    long long logical_width, long long logical_height, int vsync_enabled,
    char* error, int error_capacity)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    BOOL composition_enabled = FALSE;
    WCHAR option[8];
    DWORD option_length;
    int width;
    int height;
    smile_gdi_zero_memory(state, sizeof(*state));
    state->window = (HWND)native_window;
    state->logical_width = logical_width;
    state->logical_height = logical_height;
    state->vsync_enabled = vsync_enabled;
    state->dwm_flush_enabled = vsync_enabled && SUCCEEDED(DwmIsCompositionEnabled(&composition_enabled)) && composition_enabled;
    option_length = GetEnvironmentVariableW(L"SMILE_GDI_DWM_FLUSH", option,
        (DWORD)(sizeof(option) / sizeof(option[0])));
    if (option_length == 1 && option[0] == L'0')
        state->dwm_flush_enabled = 0;
    smile_gdi_current_client(state, &width, &height);
    if (width <= 0) width = smile_gdi_integer(logical_width);
    if (height <= 0) height = smile_gdi_integer(logical_height);
    if (!smile_gdi_create_back_buffer(state, width, height))
    {
        if (error != 0 && error_capacity > 0)
            lstrcpynA(error, "GDI physical back-buffer creation failed.", error_capacity);
        return 0;
    }
    return 1;
}

static void smile_gdi_resize(SmileGraphicsBackend* backend, int physical_width, int physical_height)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    if (physical_width <= 0 || physical_height <= 0)
        return;
    if (physical_width == state->physical_width && physical_height == state->physical_height)
        return;
    smile_gdi_create_back_buffer(state, physical_width, physical_height);
}

static void smile_gdi_begin_frame(SmileGraphicsBackend* backend)
{
    smile_gdi_ensure_client_buffer((SmileGdiState*)backend->state);
}

static void smile_gdi_clear(SmileGraphicsBackend* backend, long long color)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    RECT viewport;
    if (state->back_dc == 0)
        return;
    PatBlt(state->back_dc, 0, 0, state->physical_width, state->physical_height, BLACKNESS);
    viewport.left = smile_graphics_round_pixel(state->viewport.x);
    viewport.top = smile_graphics_round_pixel(state->viewport.y);
    viewport.right = smile_graphics_round_pixel(state->viewport.x + state->viewport.width);
    viewport.bottom = smile_graphics_round_pixel(state->viewport.y + state->viewport.height);
    FillRect(state->back_dc, &viewport, smile_gdi_brush(state, (COLORREF)color));
}

static void smile_gdi_rectangle(SmileGraphicsBackend* backend, long long x, long long y,
    long long width, long long height, long long radius, long long color, int fill, int rounded)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    HGDIOBJ old_pen;
    HGDIOBJ old_brush;
    HPEN pen;
    HBRUSH brush;
    int left;
    int top;
    int right;
    int bottom;
    int corner;
    if (state->back_dc == 0)
        return;
    pen = smile_gdi_pen(state, (COLORREF)color,
        smile_gdi_positive_pixel(smile_graphics_map_size(&state->viewport, 1.0)));
    brush = smile_gdi_brush(state, (COLORREF)color);
    left = smile_graphics_round_pixel(smile_graphics_map_x(&state->viewport, (double)x));
    top = smile_graphics_round_pixel(smile_graphics_map_y(&state->viewport, (double)y));
    right = smile_graphics_round_pixel(smile_graphics_map_x(&state->viewport, (double)(x + width)));
    bottom = smile_graphics_round_pixel(smile_graphics_map_y(&state->viewport, (double)(y + height)));
    corner = smile_gdi_positive_pixel(smile_graphics_map_size(&state->viewport, (double)(radius * 2)));
    old_pen = SelectObject(state->back_dc, fill ? GetStockObject(NULL_PEN) : pen);
    old_brush = SelectObject(state->back_dc, fill ? brush : GetStockObject(NULL_BRUSH));
    if (rounded)
        RoundRect(state->back_dc, left, top, right, bottom, corner, corner);
    else
        Rectangle(state->back_dc, left, top, right, bottom);
    SelectObject(state->back_dc, old_brush);
    SelectObject(state->back_dc, old_pen);
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
    HPEN pen;
    HBRUSH brush;
    double center_x;
    double center_y;
    double physical_radius;
    if (state->back_dc == 0)
        return;
    pen = smile_gdi_pen(state, (COLORREF)color,
        smile_gdi_positive_pixel(smile_graphics_map_size(&state->viewport, 1.0)));
    brush = smile_gdi_brush(state, (COLORREF)color);
    center_x = smile_graphics_map_x(&state->viewport, (double)x);
    center_y = smile_graphics_map_y(&state->viewport, (double)y);
    physical_radius = smile_graphics_map_size(&state->viewport, (double)radius);
    old_pen = SelectObject(state->back_dc, fill ? GetStockObject(NULL_PEN) : pen);
    old_brush = SelectObject(state->back_dc, fill ? brush : GetStockObject(NULL_BRUSH));
    Ellipse(state->back_dc,
        smile_graphics_round_pixel(center_x - physical_radius),
        smile_graphics_round_pixel(center_y - physical_radius),
        smile_graphics_round_pixel(center_x + physical_radius),
        smile_graphics_round_pixel(center_y + physical_radius));
    SelectObject(state->back_dc, old_brush);
    SelectObject(state->back_dc, old_pen);
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
    pen = smile_gdi_pen(state, (COLORREF)color,
        smile_gdi_positive_pixel(smile_graphics_map_size(&state->viewport, 1.0)));
    old_pen = SelectObject(state->back_dc, pen);
    MoveToEx(state->back_dc,
        smile_graphics_round_pixel(smile_graphics_map_x(&state->viewport, (double)x1)),
        smile_graphics_round_pixel(smile_graphics_map_y(&state->viewport, (double)y1)), 0);
    LineTo(state->back_dc,
        smile_graphics_round_pixel(smile_graphics_map_x(&state->viewport, (double)x2)),
        smile_graphics_round_pixel(smile_graphics_map_y(&state->viewport, (double)y2)));
    SelectObject(state->back_dc, old_pen);
}

static void smile_gdi_draw_wide(SmileGdiState* state, const WCHAR* text, int length,
    long long x, long long y, long long size, long long color, long long centered)
{
    HFONT font;
    HGDIOBJ old_font;
    int physical_size;
    int physical_x;
    int physical_y;
    if (state->back_dc == 0 || text == 0 || length <= 0)
        return;
    physical_size = smile_gdi_positive_pixel(smile_graphics_map_size(&state->viewport, (double)size));
    physical_x = smile_graphics_round_pixel(smile_graphics_map_x(&state->viewport, (double)x));
    physical_y = smile_graphics_round_pixel(smile_graphics_map_y(&state->viewport, (double)y));
    font = smile_gdi_font(state, physical_size);
    if (font == 0)
        return;
    old_font = SelectObject(state->back_dc, font);
    SetBkMode(state->back_dc, TRANSPARENT);
    SetTextColor(state->back_dc, (COLORREF)color);
    SetTextAlign(state->back_dc, (UINT)((centered != 0 ? TA_CENTER : TA_LEFT) | TA_TOP));
    TextOutW(state->back_dc, physical_x, physical_y, text, length);
    SelectObject(state->back_dc, old_font);
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
    BOOL copied;
    smile_gdi_ensure_client_buffer(state);
    if (state->window == 0 || state->back_dc == 0 || state->physical_width <= 0 || state->physical_height <= 0)
        return 0;
    destination = GetDC(state->window);
    if (destination == 0)
        return 0;
    copied = BitBlt(destination, 0, 0, state->physical_width, state->physical_height,
        state->back_dc, 0, 0, SRCCOPY);
    ReleaseDC(state->window, destination);
    if (copied && state->dwm_flush_enabled)
        DwmFlush();
    return copied != FALSE;
}

static void smile_gdi_repaint(SmileGraphicsBackend* backend, void* native_paint_context)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    if (native_paint_context != 0 && state->back_dc != 0)
        BitBlt((HDC)native_paint_context, 0, 0, state->physical_width, state->physical_height,
            state->back_dc, 0, 0, SRCCOPY);
}

static void smile_gdi_on_fullscreen_changed(SmileGraphicsBackend* backend, int fullscreen)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    (void)fullscreen;
    smile_gdi_ensure_client_buffer(state);
    if (state->window != 0)
        InvalidateRect(state->window, 0, FALSE);
}

static void smile_gdi_on_dpi_changed(SmileGraphicsBackend* backend, unsigned int dpi)
{
    (void)dpi;
    smile_gdi_ensure_client_buffer((SmileGdiState*)backend->state);
}

static void smile_gdi_shutdown(SmileGraphicsBackend* backend)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    smile_gdi_release_buffer(state);
    smile_gdi_release_cache(state);
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
    diagnostics->physical_width = state->physical_width;
    diagnostics->physical_height = state->physical_height;
    diagnostics->viewport_x = state->viewport.x;
    diagnostics->viewport_y = state->viewport.y;
    diagnostics->viewport_width = state->viewport.width;
    diagnostics->viewport_height = state->viewport.height;
    diagnostics->scale = state->viewport.scale;
    diagnostics->pacing_mode = state->dwm_flush_enabled
        ? "GDI DwmFlush best effort" : "GDI best effort (DwmFlush disabled)";
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
