#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include "frame_clock_win32.h"

static double smile_elapsed_milliseconds(const SmileFrameClock* clock, LARGE_INTEGER start, LARGE_INTEGER end)
{
    if (clock == 0 || clock->frequency.QuadPart <= 0)
        return 0.0;
    return (double)(end.QuadPart - start.QuadPart) * 1000.0 / (double)clock->frequency.QuadPart;
}

int smile_frame_clock_initialize(SmileFrameClock* clock)
{
    LARGE_INTEGER now;
    if (clock == 0 || !QueryPerformanceFrequency(&clock->frequency) || clock->frequency.QuadPart <= 0)
        return 0;
    QueryPerformanceCounter(&now);
    clock->frame_started = now;
    clock->present_started = now;
    clock->sample_started = now;
    clock->total_frame_count = 0;
    clock->sample_frame_count = 0;
    clock->sample_frame_total_ms = 0.0;
    clock->sample_frame_minimum_ms = 0.0;
    clock->sample_frame_maximum_ms = 0.0;
    clock->sample_draw_total_ms = 0.0;
    clock->sample_present_total_ms = 0.0;
    clock->current_draw_ms = 0.0;
    clock->metrics.total_frame_count = 0;
    clock->metrics.average_fps = 0.0;
    clock->metrics.average_frame_ms = 0.0;
    clock->metrics.minimum_frame_ms = 0.0;
    clock->metrics.maximum_frame_ms = 0.0;
    clock->metrics.average_draw_ms = 0.0;
    clock->metrics.average_present_ms = 0.0;
    return 1;
}

void smile_frame_clock_begin_present(SmileFrameClock* clock)
{
    LARGE_INTEGER now;
    if (clock == 0 || clock->frequency.QuadPart <= 0)
        return;
    QueryPerformanceCounter(&now);
    clock->current_draw_ms = smile_elapsed_milliseconds(clock, clock->frame_started, now);
    clock->present_started = now;
}

int smile_frame_clock_end_present(SmileFrameClock* clock)
{
    LARGE_INTEGER now;
    double present_ms;
    double frame_ms;
    double sample_ms;
    if (clock == 0 || clock->frequency.QuadPart <= 0)
        return 0;

    QueryPerformanceCounter(&now);
    present_ms = smile_elapsed_milliseconds(clock, clock->present_started, now);
    frame_ms = smile_elapsed_milliseconds(clock, clock->frame_started, now);
    clock->total_frame_count++;
    clock->sample_frame_count++;
    clock->sample_frame_total_ms += frame_ms;
    clock->sample_draw_total_ms += clock->current_draw_ms;
    clock->sample_present_total_ms += present_ms;
    if (clock->sample_frame_count == 1 || frame_ms < clock->sample_frame_minimum_ms)
        clock->sample_frame_minimum_ms = frame_ms;
    if (frame_ms > clock->sample_frame_maximum_ms)
        clock->sample_frame_maximum_ms = frame_ms;
    clock->frame_started = now;

    sample_ms = smile_elapsed_milliseconds(clock, clock->sample_started, now);
    if (sample_ms < 1000.0)
        return 0;

    clock->metrics.total_frame_count = clock->total_frame_count;
    clock->metrics.average_fps = sample_ms > 0.0
        ? (double)clock->sample_frame_count * 1000.0 / sample_ms
        : 0.0;
    clock->metrics.average_frame_ms = clock->sample_frame_count != 0
        ? clock->sample_frame_total_ms / (double)clock->sample_frame_count
        : 0.0;
    clock->metrics.minimum_frame_ms = clock->sample_frame_minimum_ms;
    clock->metrics.maximum_frame_ms = clock->sample_frame_maximum_ms;
    clock->metrics.average_draw_ms = clock->sample_frame_count != 0
        ? clock->sample_draw_total_ms / (double)clock->sample_frame_count
        : 0.0;
    clock->metrics.average_present_ms = clock->sample_frame_count != 0
        ? clock->sample_present_total_ms / (double)clock->sample_frame_count
        : 0.0;

    clock->sample_started = now;
    clock->sample_frame_count = 0;
    clock->sample_frame_total_ms = 0.0;
    clock->sample_frame_minimum_ms = 0.0;
    clock->sample_frame_maximum_ms = 0.0;
    clock->sample_draw_total_ms = 0.0;
    clock->sample_present_total_ms = 0.0;
    return 1;
}

const SmileFrameMetrics* smile_frame_clock_metrics(const SmileFrameClock* clock)
{
    return clock == 0 ? 0 : &clock->metrics;
}

long long smile_monotonic_milliseconds(void)
{
    static LARGE_INTEGER frequency;
    LARGE_INTEGER now;
    if (frequency.QuadPart == 0 && !QueryPerformanceFrequency(&frequency))
        return (long long)GetTickCount64();
    QueryPerformanceCounter(&now);
    return (long long)(now.QuadPart * 1000LL / frequency.QuadPart);
}
