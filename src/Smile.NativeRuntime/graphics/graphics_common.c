#include "graphics_common.h"
#include "graphics_gdi.h"

static SmileGraphicsBackend smile_active_backend;
static int smile_frame_started;

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
    long long logical_height, int vsync_enabled, char* error, int error_capacity)
{
    smile_graphics_gdi_create(&smile_active_backend);
    if (!smile_active_backend.operations->initialize(&smile_active_backend, native_window,
        logical_width, logical_height, vsync_enabled, error, error_capacity))
    {
        smile_active_backend.operations = 0;
        smile_active_backend.state = 0;
        return 0;
    }
    smile_frame_started = 0;
    return 1;
}

void smile_graphics_resize(int physical_width, int physical_height)
{
    if (smile_graphics_available())
        smile_active_backend.operations->resize(&smile_active_backend, physical_width, physical_height);
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
}

void smile_graphics_on_dpi_changed(unsigned int dpi)
{
    if (smile_graphics_available())
        smile_active_backend.operations->on_dpi_changed(&smile_active_backend, dpi);
}

void smile_graphics_shutdown(void)
{
    if (smile_graphics_available())
        smile_active_backend.operations->shutdown(&smile_active_backend);
    smile_active_backend.operations = 0;
    smile_active_backend.state = 0;
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
}
