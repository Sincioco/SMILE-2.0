#ifndef SMILE_GRAPHICS_COMMON_H
#define SMILE_GRAPHICS_COMMON_H

#include "graphics_backend.h"

typedef struct SmileGraphicsViewport
{
    double x;
    double y;
    double width;
    double height;
    double scale;
} SmileGraphicsViewport;

#ifdef __cplusplus
extern "C" {
#endif

void smile_graphics_calculate_viewport(long long logical_width, long long logical_height,
    int physical_width, int physical_height, SmileGraphicsViewport* viewport);
double smile_graphics_map_x(const SmileGraphicsViewport* viewport, double logical_x);
double smile_graphics_map_y(const SmileGraphicsViewport* viewport, double logical_y);
double smile_graphics_map_size(const SmileGraphicsViewport* viewport, double logical_size);
int smile_graphics_round_pixel(double value);

int smile_graphics_initialize(void* native_window, long long logical_width,
    long long logical_height, SmileGraphicsBackendKind requested_backend,
    int vsync_enabled, char* error, int error_capacity);
void smile_graphics_resize(int physical_width, int physical_height);
void smile_graphics_begin_frame(void);
void smile_graphics_clear(long long color);
void smile_graphics_fill_rectangle(long long x, long long y, long long width, long long height, long long color);
void smile_graphics_draw_rectangle(long long x, long long y, long long width, long long height, long long color);
void smile_graphics_fill_rounded_rectangle(long long x, long long y, long long width, long long height, long long radius, long long color);
void smile_graphics_draw_rounded_rectangle(long long x, long long y, long long width, long long height, long long radius, long long color);
void smile_graphics_fill_circle(long long x, long long y, long long radius, long long color);
void smile_graphics_draw_circle(long long x, long long y, long long radius, long long color);
void smile_graphics_draw_line(long long x1, long long y1, long long x2, long long y2, long long color);
void smile_graphics_draw_text(const char* text, long long length, long long x, long long y,
    long long size, long long color, long long centered);
void smile_graphics_draw_number(long long value, long long x, long long y, long long size, long long color);
int smile_graphics_present(void);
void smile_graphics_repaint(void* native_paint_context);
void smile_graphics_on_fullscreen_changed(int fullscreen);
void smile_graphics_on_dpi_changed(unsigned int dpi);
void smile_graphics_shutdown(void);
const char* smile_graphics_backend_name(void);
void smile_graphics_get_diagnostics(SmileGraphicsBackendDiagnostics* diagnostics);

#ifdef __cplusplus
}
#endif

#endif
