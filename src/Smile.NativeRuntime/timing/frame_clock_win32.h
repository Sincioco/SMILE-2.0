#ifndef SMILE_FRAME_CLOCK_WIN32_H
#define SMILE_FRAME_CLOCK_WIN32_H

#define WIN32_LEAN_AND_MEAN
#include <windows.h>

typedef struct SmileFrameMetrics
{
    unsigned long long total_frame_count;
    double average_fps;
    double average_frame_ms;
    double minimum_frame_ms;
    double maximum_frame_ms;
    double average_draw_ms;
    double average_present_ms;
} SmileFrameMetrics;

typedef struct SmileFrameClock
{
    LARGE_INTEGER frequency;
    LARGE_INTEGER frame_started;
    LARGE_INTEGER present_started;
    LARGE_INTEGER sample_started;
    unsigned long long total_frame_count;
    unsigned long long sample_frame_count;
    double sample_frame_total_ms;
    double sample_frame_minimum_ms;
    double sample_frame_maximum_ms;
    double sample_draw_total_ms;
    double sample_present_total_ms;
    double current_draw_ms;
    SmileFrameMetrics metrics;
} SmileFrameClock;

int smile_frame_clock_initialize(SmileFrameClock* clock);
void smile_frame_clock_begin_present(SmileFrameClock* clock);
int smile_frame_clock_end_present(SmileFrameClock* clock);
const SmileFrameMetrics* smile_frame_clock_metrics(const SmileFrameClock* clock);
long long smile_monotonic_milliseconds(void);

#endif
