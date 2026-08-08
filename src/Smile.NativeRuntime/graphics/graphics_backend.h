#ifndef SMILE_GRAPHICS_BACKEND_H
#define SMILE_GRAPHICS_BACKEND_H

typedef struct SmileGraphicsBackend SmileGraphicsBackend;

typedef enum SmileGraphicsBackendKind
{
    SMILE_GRAPHICS_BACKEND_AUTO = 0,
    SMILE_GRAPHICS_BACKEND_GDI = 1,
    SMILE_GRAPHICS_BACKEND_DIRECTX = 2
} SmileGraphicsBackendKind;

typedef struct SmileGraphicsBackendDiagnostics
{
    const char* requested_backend;
    const char* selected_backend;
    const char* fallback_reason;
    int physical_width;
    int physical_height;
    double viewport_x;
    double viewport_y;
    double viewport_width;
    double viewport_height;
    double scale;
    const char* pacing_mode;
    const char* device_removal_reason;
} SmileGraphicsBackendDiagnostics;

typedef struct SmileGraphicsBackendVTable
{
    int (*initialize)(SmileGraphicsBackend* backend, void* native_window,
        long long logical_width, long long logical_height, int vsync_enabled,
        char* error, int error_capacity);
    void (*resize)(SmileGraphicsBackend* backend, int physical_width, int physical_height);
    void (*begin_frame)(SmileGraphicsBackend* backend);
    void (*clear)(SmileGraphicsBackend* backend, long long color);
    void (*fill_rectangle)(SmileGraphicsBackend* backend, long long x, long long y,
        long long width, long long height, long long color);
    void (*draw_rectangle)(SmileGraphicsBackend* backend, long long x, long long y,
        long long width, long long height, long long color);
    void (*fill_rounded_rectangle)(SmileGraphicsBackend* backend, long long x, long long y,
        long long width, long long height, long long radius, long long color);
    void (*draw_rounded_rectangle)(SmileGraphicsBackend* backend, long long x, long long y,
        long long width, long long height, long long radius, long long color);
    void (*fill_circle)(SmileGraphicsBackend* backend, long long x, long long y,
        long long radius, long long color);
    void (*draw_circle)(SmileGraphicsBackend* backend, long long x, long long y,
        long long radius, long long color);
    void (*fill_quadrilateral)(SmileGraphicsBackend* backend,
        long long x1, long long y1, long long x2, long long y2,
        long long x3, long long y3, long long x4, long long y4, long long color);
    void (*draw_quadrilateral)(SmileGraphicsBackend* backend,
        long long x1, long long y1, long long x2, long long y2,
        long long x3, long long y3, long long x4, long long y4, long long color);
    void (*draw_line)(SmileGraphicsBackend* backend, long long x1, long long y1,
        long long x2, long long y2, long long color);
    void (*draw_text)(SmileGraphicsBackend* backend, const char* text, long long length,
        long long x, long long y, long long size, long long color, long long centered);
    void (*draw_number)(SmileGraphicsBackend* backend, long long value, long long x,
        long long y, long long size, long long color);
    int (*present)(SmileGraphicsBackend* backend);
    void (*repaint)(SmileGraphicsBackend* backend, void* native_paint_context);
    void (*on_fullscreen_changed)(SmileGraphicsBackend* backend, int fullscreen);
    void (*on_dpi_changed)(SmileGraphicsBackend* backend, unsigned int dpi);
    void (*shutdown)(SmileGraphicsBackend* backend);
    const char* (*get_backend_name)(const SmileGraphicsBackend* backend);
    void (*get_diagnostics)(const SmileGraphicsBackend* backend,
        SmileGraphicsBackendDiagnostics* diagnostics);
} SmileGraphicsBackendVTable;

struct SmileGraphicsBackend
{
    const SmileGraphicsBackendVTable* operations;
    void* state;
};

#endif
