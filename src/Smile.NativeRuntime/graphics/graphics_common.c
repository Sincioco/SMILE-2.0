#include "graphics_common.h"
#include "graphics_directx.h"
#include "graphics_gdi.h"

static SmileGraphicsBackend smile_active_backend;
static int smile_frame_started;
static const char* smile_requested_backend_name = "Auto";
static const char* smile_fallback_reason = "None";
static char smile_fallback_reason_buffer[512];

static void smile_graphics_copy_text(char* destination, int capacity, const char* source)
{
    int index = 0;
    if (destination == 0 || capacity <= 0)
        return;
    while (source != 0 && source[index] != 0 && index + 1 < capacity)
    {
        destination[index] = source[index];
        index++;
    }
    destination[index] = 0;
}

static void smile_graphics_append_text(char* destination, int capacity, const char* source)
{
    int index = 0;
    while (index < capacity && destination[index] != 0)
        index++;
    if (index >= capacity)
        return;
    while (source != 0 && *source != 0 && index + 1 < capacity)
        destination[index++] = *source++;
    destination[index] = 0;
}

static void smile_graphics_clear_backend(void)
{
    smile_active_backend.operations = 0;
    smile_active_backend.state = 0;
}

static int smile_graphics_try_backend(SmileGraphicsBackendKind backend_kind,
    void* native_window, long long logical_width, long long logical_height,
    int vsync_enabled, char* error, int error_capacity)
{
    if (backend_kind == SMILE_GRAPHICS_BACKEND_DIRECTX)
        smile_graphics_directx_create(&smile_active_backend);
    else
        smile_graphics_gdi_create(&smile_active_backend);
    if (smile_active_backend.operations->initialize(&smile_active_backend, native_window,
        logical_width, logical_height, vsync_enabled, error, error_capacity))
        return 1;
    smile_active_backend.operations->shutdown(&smile_active_backend);
    smile_graphics_clear_backend();
    return 0;
}

void smile_graphics_calculate_viewport(long long logical_width, long long logical_height,
    int physical_width, int physical_height, SmileGraphicsViewport* viewport)
{
    double scale_x;
    double scale_y;
    if (viewport == 0)
        return;
    viewport->x = 0.0;
    viewport->y = 0.0;
    viewport->width = 0.0;
    viewport->height = 0.0;
    viewport->scale = 0.0;
    if (logical_width <= 0 || logical_height <= 0 || physical_width <= 0 || physical_height <= 0)
        return;
    scale_x = (double)physical_width / (double)logical_width;
    scale_y = (double)physical_height / (double)logical_height;
    viewport->scale = scale_x < scale_y ? scale_x : scale_y;
    viewport->width = (double)logical_width * viewport->scale;
    viewport->height = (double)logical_height * viewport->scale;
    viewport->x = ((double)physical_width - viewport->width) / 2.0;
    viewport->y = ((double)physical_height - viewport->height) / 2.0;
}

double smile_graphics_map_x(const SmileGraphicsViewport* viewport, double logical_x)
{
    return viewport == 0 ? 0.0 : viewport->x + logical_x * viewport->scale;
}

double smile_graphics_map_y(const SmileGraphicsViewport* viewport, double logical_y)
{
    return viewport == 0 ? 0.0 : viewport->y + logical_y * viewport->scale;
}

double smile_graphics_map_size(const SmileGraphicsViewport* viewport, double logical_size)
{
    return viewport == 0 ? 0.0 : logical_size * viewport->scale;
}

int smile_graphics_round_pixel(double value)
{
    return value >= 0.0 ? (int)(value + 0.5) : (int)(value - 0.5);
}

static int smile_graphics_available(void)
{
    return smile_active_backend.operations != 0;
}

static void smile_graphics_ensure_frame(void)
{
    if (!smile_graphics_available() || smile_frame_started)
        return;
    smile_active_backend.operations->begin_frame(&smile_active_backend);
    smile_frame_started = 1;
}

int smile_graphics_initialize(void* native_window, long long logical_width,
    long long logical_height, SmileGraphicsBackendKind requested_backend,
    int vsync_enabled, char* error, int error_capacity)
{
    char directx_error[512];
    smile_fallback_reason_buffer[0] = 0;
    smile_fallback_reason = "None";
    if (requested_backend == SMILE_GRAPHICS_BACKEND_AUTO)
    {
        smile_requested_backend_name = "Auto";
        directx_error[0] = 0;
        if (smile_graphics_try_backend(SMILE_GRAPHICS_BACKEND_DIRECTX, native_window,
            logical_width, logical_height, vsync_enabled, directx_error,
            (int)sizeof(directx_error)))
        {
            smile_frame_started = 0;
            return 1;
        }
        smile_graphics_copy_text(smile_fallback_reason_buffer,
            (int)sizeof(smile_fallback_reason_buffer),
            directx_error[0] != 0 ? directx_error : "DirectX initialization failed without details.");
        smile_fallback_reason = smile_fallback_reason_buffer;
        if (smile_graphics_try_backend(SMILE_GRAPHICS_BACKEND_GDI, native_window,
            logical_width, logical_height, vsync_enabled, error, error_capacity))
        {
            smile_frame_started = 0;
            return 1;
        }
        if (error != 0 && error_capacity > 0)
        {
            char gdi_error[256];
            smile_graphics_copy_text(gdi_error, (int)sizeof(gdi_error), error);
            smile_graphics_copy_text(error, error_capacity, "DirectX initialization failed: ");
            smile_graphics_append_text(error, error_capacity, smile_fallback_reason_buffer);
            smile_graphics_append_text(error, error_capacity, "\r\nGDI fallback failed: ");
            smile_graphics_append_text(error, error_capacity,
                gdi_error[0] != 0 ? gdi_error : "No details were provided.");
        }
        return 0;
    }
    if (requested_backend == SMILE_GRAPHICS_BACKEND_DIRECTX)
    {
        smile_requested_backend_name = "DirectX";
        if (!smile_graphics_try_backend(SMILE_GRAPHICS_BACKEND_DIRECTX, native_window,
            logical_width, logical_height, vsync_enabled, error, error_capacity))
        {
            if (error != 0 && error_capacity > 0)
            {
                char directx_detail[512];
                smile_graphics_copy_text(directx_detail, (int)sizeof(directx_detail), error);
                smile_graphics_copy_text(error, error_capacity,
                    "SMILE could not start the DirectX graphics backend.\r\n");
                smile_graphics_append_text(error, error_capacity, directx_detail);
                smile_graphics_append_text(error, error_capacity,
                    "\r\nTry <GraphicsBackend>GDI</GraphicsBackend> or update the graphics driver.");
            }
            return 0;
        }
    }
    else
    {
        smile_requested_backend_name = "GDI";
        if (!smile_graphics_try_backend(SMILE_GRAPHICS_BACKEND_GDI, native_window,
            logical_width, logical_height, vsync_enabled, error, error_capacity))
            return 0;
    }
    smile_frame_started = 0;
    return 1;
}

void smile_graphics_resize(int physical_width, int physical_height)
{
    if (smile_graphics_available())
        smile_active_backend.operations->resize(&smile_active_backend, physical_width, physical_height);
    smile_frame_started = 0;
}

void smile_graphics_begin_frame(void)
{
    smile_graphics_ensure_frame();
}

void smile_graphics_clear(long long color)
{
    smile_graphics_ensure_frame();
    if (smile_graphics_available()) smile_active_backend.operations->clear(&smile_active_backend, color);
}

void smile_graphics_fill_rectangle(long long x, long long y, long long width, long long height, long long color)
{
    smile_graphics_ensure_frame();
    if (smile_graphics_available()) smile_active_backend.operations->fill_rectangle(&smile_active_backend, x, y, width, height, color);
}

void smile_graphics_draw_rectangle(long long x, long long y, long long width, long long height, long long color)
{
    smile_graphics_ensure_frame();
    if (smile_graphics_available()) smile_active_backend.operations->draw_rectangle(&smile_active_backend, x, y, width, height, color);
}

void smile_graphics_fill_rounded_rectangle(long long x, long long y, long long width, long long height, long long radius, long long color)
{
    smile_graphics_ensure_frame();
    if (smile_graphics_available()) smile_active_backend.operations->fill_rounded_rectangle(&smile_active_backend, x, y, width, height, radius, color);
}

void smile_graphics_draw_rounded_rectangle(long long x, long long y, long long width, long long height, long long radius, long long color)
{
    smile_graphics_ensure_frame();
    if (smile_graphics_available()) smile_active_backend.operations->draw_rounded_rectangle(&smile_active_backend, x, y, width, height, radius, color);
}

void smile_graphics_fill_circle(long long x, long long y, long long radius, long long color)
{
    smile_graphics_ensure_frame();
    if (smile_graphics_available()) smile_active_backend.operations->fill_circle(&smile_active_backend, x, y, radius, color);
}

void smile_graphics_draw_circle(long long x, long long y, long long radius, long long color)
{
    smile_graphics_ensure_frame();
    if (smile_graphics_available()) smile_active_backend.operations->draw_circle(&smile_active_backend, x, y, radius, color);
}

void smile_graphics_draw_line(long long x1, long long y1, long long x2, long long y2, long long color)
{
    smile_graphics_ensure_frame();
    if (smile_graphics_available()) smile_active_backend.operations->draw_line(&smile_active_backend, x1, y1, x2, y2, color);
}

void smile_graphics_draw_text(const char* text, long long length, long long x, long long y,
    long long size, long long color, long long centered)
{
    smile_graphics_ensure_frame();
    if (smile_graphics_available()) smile_active_backend.operations->draw_text(&smile_active_backend,
        text, length, x, y, size, color, centered);
}

void smile_graphics_draw_number(long long value, long long x, long long y, long long size, long long color)
{
    smile_graphics_ensure_frame();
    if (smile_graphics_available()) smile_active_backend.operations->draw_number(&smile_active_backend, value, x, y, size, color);
}

int smile_graphics_present(void)
{
    int result;
    if (!smile_graphics_available())
        return 0;
    smile_graphics_ensure_frame();
    result = smile_active_backend.operations->present(&smile_active_backend);
    smile_frame_started = 0;
    return result;
}

void smile_graphics_repaint(void* native_paint_context)
{
    if (smile_graphics_available())
        smile_active_backend.operations->repaint(&smile_active_backend, native_paint_context);
}

void smile_graphics_on_fullscreen_changed(int fullscreen)
{
    if (smile_graphics_available())
        smile_active_backend.operations->on_fullscreen_changed(&smile_active_backend, fullscreen);
    smile_frame_started = 0;
}

void smile_graphics_on_dpi_changed(unsigned int dpi)
{
    if (smile_graphics_available())
        smile_active_backend.operations->on_dpi_changed(&smile_active_backend, dpi);
    smile_frame_started = 0;
}

void smile_graphics_shutdown(void)
{
    if (smile_graphics_available())
        smile_active_backend.operations->shutdown(&smile_active_backend);
    smile_graphics_clear_backend();
    smile_frame_started = 0;
}

const char* smile_graphics_backend_name(void)
{
    return smile_graphics_available()
        ? smile_active_backend.operations->get_backend_name(&smile_active_backend)
        : "None";
}

void smile_graphics_get_diagnostics(SmileGraphicsBackendDiagnostics* diagnostics)
{
    if (diagnostics == 0)
        return;
    diagnostics->physical_width = 0;
    diagnostics->physical_height = 0;
    diagnostics->viewport_x = 0.0;
    diagnostics->viewport_y = 0.0;
    diagnostics->viewport_width = 0.0;
    diagnostics->viewport_height = 0.0;
    diagnostics->scale = 0.0;
    diagnostics->pacing_mode = "Unavailable";
    diagnostics->device_removal_reason = "None";
    if (smile_graphics_available())
        smile_active_backend.operations->get_diagnostics(&smile_active_backend, diagnostics);
    diagnostics->requested_backend = smile_requested_backend_name;
    diagnostics->selected_backend = smile_graphics_backend_name();
    diagnostics->fallback_reason = smile_fallback_reason;
}
