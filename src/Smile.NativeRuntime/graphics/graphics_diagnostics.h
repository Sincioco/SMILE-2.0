#ifndef SMILE_GRAPHICS_DIAGNOSTICS_H
#define SMILE_GRAPHICS_DIAGNOSTICS_H

#include "../timing/frame_clock_win32.h"

typedef struct SmileGraphicsDiagnosticsSnapshot
{
    const char* requested_backend;
    const char* selected_backend;
    const char* fallback_reason;
    long long logical_width;
    long long logical_height;
    int physical_width;
    int physical_height;
    double viewport_x;
    double viewport_y;
    double viewport_width;
    double viewport_height;
    double scale;
    int refresh_rate;
    int vsync_enabled;
    const char* pacing_mode;
    const char* device_removal_reason;
    SmileFrameMetrics frame_metrics;
} SmileGraphicsDiagnosticsSnapshot;

void smile_graphics_diagnostics_initialize(void);
int smile_graphics_diagnostics_enabled(void);
void smile_graphics_diagnostics_log(const SmileGraphicsDiagnosticsSnapshot* snapshot);
void smile_graphics_diagnostics_shutdown(void);

#endif
