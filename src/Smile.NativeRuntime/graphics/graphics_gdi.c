#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <dwmapi.h>
#include <limits.h>
#include <math.h>
#include "graphics_common.h"
#include "graphics_gdi.h"
#include "graphics_gdi_image.h"
#include "image_resource.h"

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
    unsigned char* back_bits;
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
    state->back_bits = 0;
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
    state->back_bits = (unsigned char*)bits;
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

static void smile_gdi_set_logical_size(SmileGraphicsBackend* backend,
    long long logical_width, long long logical_height)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    state->logical_width = logical_width;
    state->logical_height = logical_height;
    smile_graphics_calculate_viewport(logical_width, logical_height,
        state->physical_width, state->physical_height, &state->viewport);
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

static void smile_gdi_fill_rectangle_opacity(SmileGraphicsBackend* backend, long long x,
    long long y, long long width, long long height, long long color, long long opacity)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    RECT clip;
    int left;
    int top;
    int right;
    int bottom;
    int row;
    int column;
    int alpha;
    int red;
    int green;
    int blue;
    unsigned char* pixel;
    if (state->back_bits == 0 || opacity <= 0)
        return;
    if (opacity >= 100)
    {
        smile_gdi_fill_rectangle(backend, x, y, width, height, color);
        return;
    }
    left = smile_graphics_round_pixel(smile_graphics_map_x(&state->viewport, (double)x));
    top = smile_graphics_round_pixel(smile_graphics_map_y(&state->viewport, (double)y));
    right = smile_graphics_round_pixel(smile_graphics_map_x(&state->viewport, (double)(x + width)));
    bottom = smile_graphics_round_pixel(smile_graphics_map_y(&state->viewport, (double)(y + height)));
    GetClipBox(state->back_dc, &clip);
    if (left < clip.left) left = clip.left;
    if (top < clip.top) top = clip.top;
    if (right > clip.right) right = clip.right;
    if (bottom > clip.bottom) bottom = clip.bottom;
    if (left < 0) left = 0;
    if (top < 0) top = 0;
    if (right > state->physical_width) right = state->physical_width;
    if (bottom > state->physical_height) bottom = state->physical_height;
    alpha = (int)opacity;
    red = (int)(color & 255LL);
    green = (int)((color >> 8) & 255LL);
    blue = (int)((color >> 16) & 255LL);
    for (row = top; row < bottom; row++)
    {
        pixel = state->back_bits + ((row * state->physical_width + left) * 4);
        for (column = left; column < right; column++)
        {
            pixel[0] = (unsigned char)((blue * alpha + pixel[0] * (100 - alpha)) / 100);
            pixel[1] = (unsigned char)((green * alpha + pixel[1] * (100 - alpha)) / 100);
            pixel[2] = (unsigned char)((red * alpha + pixel[2] * (100 - alpha)) / 100);
            pixel += 4;
        }
    }
}

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

static void smile_gdi_draw_arc(SmileGraphicsBackend* backend, long long center_x,
    long long center_y, long long radius, long long start_angle,
    long long sweep_angle, long long color)
{
    const double degrees_to_radians = 3.14159265358979323846 / 180.0;
    SmileGdiState* state = (SmileGdiState*)backend->state;
    HGDIOBJ old_pen;
    HGDIOBJ old_brush;
    HPEN pen;
    long long normalized_start;
    long long clamped_sweep;
    double physical_center_x;
    double physical_center_y;
    double physical_radius;
    double start_radians;
    double end_radians;
    int previous_direction;

    if (state->back_dc == 0 || radius <= 0 || sweep_angle == 0)
        return;
    if (sweep_angle >= 360 || sweep_angle <= -360)
    {
        smile_gdi_draw_circle(backend, center_x, center_y, radius, color);
        return;
    }

    normalized_start = start_angle % 360;
    if (normalized_start < 0)
        normalized_start += 360;
    clamped_sweep = sweep_angle;
    physical_center_x = smile_graphics_map_x(&state->viewport, (double)center_x);
    physical_center_y = smile_graphics_map_y(&state->viewport, (double)center_y);
    physical_radius = smile_graphics_map_size(&state->viewport, (double)radius);
    if (physical_radius <= 0.0)
        return;
    start_radians = (double)normalized_start * degrees_to_radians;
    end_radians = (double)(normalized_start + clamped_sweep) * degrees_to_radians;

    pen = smile_gdi_pen(state, (COLORREF)color,
        smile_gdi_positive_pixel(smile_graphics_map_size(&state->viewport, 1.0)));
    old_pen = SelectObject(state->back_dc, pen);
    old_brush = SelectObject(state->back_dc, GetStockObject(NULL_BRUSH));
    previous_direction = SetArcDirection(state->back_dc,
        clamped_sweep > 0 ? AD_CLOCKWISE : AD_COUNTERCLOCKWISE);
    Arc(state->back_dc,
        smile_graphics_round_pixel(physical_center_x - physical_radius),
        smile_graphics_round_pixel(physical_center_y - physical_radius),
        smile_graphics_round_pixel(physical_center_x + physical_radius),
        smile_graphics_round_pixel(physical_center_y + physical_radius),
        smile_graphics_round_pixel(physical_center_x + cos(start_radians) * physical_radius),
        smile_graphics_round_pixel(physical_center_y + sin(start_radians) * physical_radius),
        smile_graphics_round_pixel(physical_center_x + cos(end_radians) * physical_radius),
        smile_graphics_round_pixel(physical_center_y + sin(end_radians) * physical_radius));
    if (previous_direction != 0)
        SetArcDirection(state->back_dc, previous_direction);
    SelectObject(state->back_dc, old_brush);
    SelectObject(state->back_dc, old_pen);
}

static void smile_gdi_quadrilateral(SmileGraphicsBackend* backend,
    long long x1, long long y1, long long x2, long long y2,
    long long x3, long long y3, long long x4, long long y4,
    long long color, int fill)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    POINT points[4];
    HGDIOBJ old_pen;
    HGDIOBJ old_brush;
    HPEN pen;
    HBRUSH brush;
    if (state->back_dc == 0)
        return;
    points[0].x = smile_graphics_round_pixel(smile_graphics_map_x(&state->viewport, (double)x1));
    points[0].y = smile_graphics_round_pixel(smile_graphics_map_y(&state->viewport, (double)y1));
    points[1].x = smile_graphics_round_pixel(smile_graphics_map_x(&state->viewport, (double)x2));
    points[1].y = smile_graphics_round_pixel(smile_graphics_map_y(&state->viewport, (double)y2));
    points[2].x = smile_graphics_round_pixel(smile_graphics_map_x(&state->viewport, (double)x3));
    points[2].y = smile_graphics_round_pixel(smile_graphics_map_y(&state->viewport, (double)y3));
    points[3].x = smile_graphics_round_pixel(smile_graphics_map_x(&state->viewport, (double)x4));
    points[3].y = smile_graphics_round_pixel(smile_graphics_map_y(&state->viewport, (double)y4));
    pen = smile_gdi_pen(state, (COLORREF)color,
        smile_gdi_positive_pixel(smile_graphics_map_size(&state->viewport, 1.0)));
    brush = smile_gdi_brush(state, (COLORREF)color);
    old_pen = SelectObject(state->back_dc, fill ? GetStockObject(NULL_PEN) : pen);
    old_brush = SelectObject(state->back_dc, fill ? brush : GetStockObject(NULL_BRUSH));
    Polygon(state->back_dc, points, 4);
    SelectObject(state->back_dc, old_brush);
    SelectObject(state->back_dc, old_pen);
}

static void smile_gdi_fill_quadrilateral(SmileGraphicsBackend* backend,
    long long x1, long long y1, long long x2, long long y2,
    long long x3, long long y3, long long x4, long long y4, long long color)
{
    smile_gdi_quadrilateral(backend, x1, y1, x2, y2, x3, y3, x4, y4, color, 1);
}

static void smile_gdi_draw_quadrilateral(SmileGraphicsBackend* backend,
    long long x1, long long y1, long long x2, long long y2,
    long long x3, long long y3, long long x4, long long y4, long long color)
{
    smile_gdi_quadrilateral(backend, x1, y1, x2, y2, x3, y3, x4, y4, color, 0);
}

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

static void smile_gdi_draw_image(SmileGraphicsBackend* backend, void* image_value,
    long long source_x, long long source_y, long long source_width, long long source_height,
    long long destination_x, long long destination_y, long long destination_width, long long destination_height,
    long long opacity, long long filter, long long flip)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    int left = smile_graphics_round_pixel(smile_graphics_map_x(&state->viewport, (double)destination_x));
    int top = smile_graphics_round_pixel(smile_graphics_map_y(&state->viewport, (double)destination_y));
    int width = smile_gdi_positive_pixel(smile_graphics_map_size(&state->viewport, (double)destination_width));
    int height = smile_gdi_positive_pixel(smile_graphics_map_size(&state->viewport, (double)destination_height));
    if (state->back_dc == 0 || image_value == 0) return;
    smile_gdi_draw_image_resource(state->back_dc, (SmileImageResource*)image_value,
        smile_gdi_integer(source_x), smile_gdi_integer(source_y), smile_gdi_integer(source_width),
        smile_gdi_integer(source_height), left, top, width, height,
        smile_gdi_integer(opacity), smile_gdi_integer(filter), smile_gdi_integer(flip));
}

static void smile_gdi_push_clip(SmileGraphicsBackend* backend, long long x, long long y,
    long long width, long long height)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    int left = smile_graphics_round_pixel(smile_graphics_map_x(&state->viewport, (double)x));
    int top = smile_graphics_round_pixel(smile_graphics_map_y(&state->viewport, (double)y));
    int right = smile_graphics_round_pixel(smile_graphics_map_x(&state->viewport, (double)(x + width)));
    int bottom = smile_graphics_round_pixel(smile_graphics_map_y(&state->viewport, (double)(y + height)));
    if (state->back_dc == 0) return;
    SaveDC(state->back_dc);
    IntersectClipRect(state->back_dc, left, top, right, bottom);
}

static void smile_gdi_pop_clip(SmileGraphicsBackend* backend)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    if (state->back_dc != 0) RestoreDC(state->back_dc, -1);
}

static void smile_gdi_measure_text(SmileGdiState* state, const char* text, long long length,
    long long size, SIZE* measured)
{
    WCHAR* wide = smile_gdi_utf8_to_wide(text, length);
    HFONT font;
    HGDIOBJ previous;
    int physical_size = smile_gdi_positive_pixel(smile_graphics_map_size(&state->viewport, (double)size));
    measured->cx = 0;
    measured->cy = physical_size;
    if (state->back_dc == 0 || wide == 0) return;
    font = smile_gdi_font(state, physical_size);
    previous = SelectObject(state->back_dc, font);
    if (lstrlenW(wide) != 0) GetTextExtentPoint32W(state->back_dc, wide, lstrlenW(wide), measured);
    else
    {
        TEXTMETRICW metrics;
        if (GetTextMetricsW(state->back_dc, &metrics)) measured->cy = metrics.tmHeight;
    }
    SelectObject(state->back_dc, previous);
    HeapFree(GetProcessHeap(), 0, wide);
}

static long long smile_gdi_text_width(SmileGraphicsBackend* backend, const char* text, long long length, long long size)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    SIZE measured;
    smile_gdi_measure_text(state, text, length, size, &measured);
    return state->viewport.scale > 0.0 ? (long long)ceil((double)measured.cx / state->viewport.scale) : 0;
}

static long long smile_gdi_text_height(SmileGraphicsBackend* backend, const char* text, long long length, long long size)
{
    SmileGdiState* state = (SmileGdiState*)backend->state;
    SIZE measured;
    smile_gdi_measure_text(state, text, length, size, &measured);
    return state->viewport.scale > 0.0 ? (long long)ceil((double)measured.cy / state->viewport.scale) : 0;
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
    smile_gdi_image_shutdown();
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
    smile_gdi_set_logical_size,
    smile_gdi_begin_frame,
    smile_gdi_clear,
    smile_gdi_fill_rectangle,
    smile_gdi_fill_rectangle_opacity,
    smile_gdi_draw_rectangle,
    smile_gdi_fill_rounded_rectangle,
    smile_gdi_draw_rounded_rectangle,
    smile_gdi_fill_circle,
    smile_gdi_draw_circle,
    smile_gdi_draw_arc,
    smile_gdi_fill_quadrilateral,
    smile_gdi_draw_quadrilateral,
    smile_gdi_draw_line,
    smile_gdi_draw_text,
    smile_gdi_draw_number,
    smile_gdi_draw_image,
    smile_gdi_push_clip,
    smile_gdi_pop_clip,
    smile_gdi_text_width,
    smile_gdi_text_height,
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
