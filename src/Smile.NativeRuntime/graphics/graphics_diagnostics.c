#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include "graphics_diagnostics.h"

static int smile_diagnostics_enabled;
static HANDLE smile_diagnostics_file = INVALID_HANDLE_VALUE;

static void smile_append_text(char* buffer, int capacity, int* length, const char* text)
{
    while (text != 0 && *text != 0 && *length + 1 < capacity)
        buffer[(*length)++] = *text++;
    buffer[*length] = 0;
}

static void smile_append_unsigned(char* buffer, int capacity, int* length, unsigned long long value)
{
    char digits[32];
    int count = 0;
    do
    {
        digits[count++] = (char)('0' + value % 10ULL);
        value /= 10ULL;
    }
    while (value != 0 && count < (int)sizeof(digits));
    while (count > 0 && *length + 1 < capacity)
        buffer[(*length)++] = digits[--count];
    buffer[*length] = 0;
}

static void smile_append_integer(char* buffer, int capacity, int* length, long long value)
{
    unsigned long long magnitude;
    if (value < 0)
    {
        smile_append_text(buffer, capacity, length, "-");
        magnitude = (unsigned long long)(-(value + 1)) + 1ULL;
    }
    else
    {
        magnitude = (unsigned long long)value;
    }
    smile_append_unsigned(buffer, capacity, length, magnitude);
}

static void smile_append_fixed(char* buffer, int capacity, int* length, double value, int decimals)
{
    unsigned long long multiplier = 1;
    unsigned long long scaled;
    unsigned long long fraction;
    int index;
    if (value < 0.0)
    {
        smile_append_text(buffer, capacity, length, "-");
        value = -value;
    }
    for (index = 0; index < decimals; index++)
        multiplier *= 10ULL;
    scaled = (unsigned long long)(value * (double)multiplier + 0.5);
    smile_append_unsigned(buffer, capacity, length, scaled / multiplier);
    if (decimals == 0)
        return;
    smile_append_text(buffer, capacity, length, ".");
    fraction = scaled % multiplier;
    for (index = decimals - 1; index > 0; index--)
    {
        unsigned long long threshold = 1;
        int power;
        for (power = 0; power < index; power++)
            threshold *= 10ULL;
        if (fraction < threshold)
            smile_append_text(buffer, capacity, length, "0");
    }
    smile_append_unsigned(buffer, capacity, length, fraction);
}

static void smile_diagnostics_write(const char* text)
{
    DWORD written;
    DWORD length;
    if (text == 0)
        return;
    OutputDebugStringA(text);
    if (smile_diagnostics_file == INVALID_HANDLE_VALUE)
        return;
    length = (DWORD)lstrlenA(text);
    WriteFile(smile_diagnostics_file, text, length, &written, 0);
    FlushFileBuffers(smile_diagnostics_file);
}

void smile_graphics_diagnostics_initialize(void)
{
    WCHAR enabled[8];
    WCHAR temporary_path[MAX_PATH];
    WCHAR log_path[MAX_PATH];
    DWORD value_length = GetEnvironmentVariableW(L"SMILE_GRAPHICS_DIAGNOSTICS", enabled,
        (DWORD)(sizeof(enabled) / sizeof(enabled[0])));
    smile_diagnostics_enabled = value_length != 0 && value_length < (DWORD)(sizeof(enabled) / sizeof(enabled[0])) &&
        !(value_length == 1 && enabled[0] == L'0');
    if (!smile_diagnostics_enabled)
        return;
    if (GetTempPathW((DWORD)(sizeof(temporary_path) / sizeof(temporary_path[0])), temporary_path) == 0)
        return;
    if (lstrlenW(temporary_path) + 48 >= (int)(sizeof(log_path) / sizeof(log_path[0])))
        return;
    wsprintfW(log_path, L"%sSMILE-graphics-diagnostics-%lu.log", temporary_path, GetCurrentProcessId());
    smile_diagnostics_file = CreateFileW(log_path, FILE_APPEND_DATA, FILE_SHARE_READ | FILE_SHARE_WRITE,
        0, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, 0);
    smile_diagnostics_write("SMILE 2.0 graphics diagnostics enabled.\r\n");
}

int smile_graphics_diagnostics_enabled(void)
{
    return smile_diagnostics_enabled;
}

void smile_graphics_diagnostics_log(const SmileGraphicsDiagnosticsSnapshot* snapshot)
{
    char buffer[2048];
    int length = 0;
    if (!smile_diagnostics_enabled || snapshot == 0)
        return;
    buffer[0] = 0;
#define APPEND_TEXT(value) smile_append_text(buffer, (int)sizeof(buffer), &length, (value))
#define APPEND_INTEGER(value) smile_append_integer(buffer, (int)sizeof(buffer), &length, (long long)(value))
#define APPEND_FIXED(value, decimals) smile_append_fixed(buffer, (int)sizeof(buffer), &length, (value), (decimals))
    APPEND_TEXT("Requested backend: "); APPEND_TEXT(snapshot->requested_backend != 0 ? snapshot->requested_backend : "Auto"); APPEND_TEXT("\r\n");
    APPEND_TEXT("Selected backend: "); APPEND_TEXT(snapshot->selected_backend != 0 ? snapshot->selected_backend : "Unknown"); APPEND_TEXT("\r\n");
    APPEND_TEXT("Fallback reason: "); APPEND_TEXT(snapshot->fallback_reason != 0 && snapshot->fallback_reason[0] != 0 ? snapshot->fallback_reason : "None"); APPEND_TEXT("\r\n");
    APPEND_TEXT("Logical canvas: "); APPEND_INTEGER(snapshot->logical_width); APPEND_TEXT(" x "); APPEND_INTEGER(snapshot->logical_height); APPEND_TEXT("\r\n");
    APPEND_TEXT("Physical output: "); APPEND_INTEGER(snapshot->physical_width); APPEND_TEXT(" x "); APPEND_INTEGER(snapshot->physical_height); APPEND_TEXT("\r\n");
    APPEND_TEXT("Viewport: "); APPEND_FIXED(snapshot->viewport_x, 1); APPEND_TEXT(","); APPEND_FIXED(snapshot->viewport_y, 1); APPEND_TEXT(" "); APPEND_FIXED(snapshot->viewport_width, 1); APPEND_TEXT(" x "); APPEND_FIXED(snapshot->viewport_height, 1); APPEND_TEXT("\r\n");
    APPEND_TEXT("Uniform scale: "); APPEND_FIXED(snapshot->scale, 3); APPEND_TEXT("\r\n");
    APPEND_TEXT("Display refresh: "); APPEND_INTEGER(snapshot->refresh_rate); APPEND_TEXT(" Hz\r\n");
    APPEND_TEXT("VSync: "); APPEND_TEXT(snapshot->vsync_enabled ? "On" : "Off"); APPEND_TEXT("\r\n");
    APPEND_TEXT("Pacing mode: "); APPEND_TEXT(snapshot->pacing_mode != 0 ? snapshot->pacing_mode : "Unknown"); APPEND_TEXT("\r\n");
    APPEND_TEXT("Average FPS: "); APPEND_FIXED(snapshot->frame_metrics.average_fps, 2); APPEND_TEXT("\r\n");
    APPEND_TEXT("Average frame: "); APPEND_FIXED(snapshot->frame_metrics.average_frame_ms, 3); APPEND_TEXT(" ms\r\n");
    APPEND_TEXT("Minimum frame: "); APPEND_FIXED(snapshot->frame_metrics.minimum_frame_ms, 3); APPEND_TEXT(" ms\r\n");
    APPEND_TEXT("Longest recent frame: "); APPEND_FIXED(snapshot->frame_metrics.maximum_frame_ms, 3); APPEND_TEXT(" ms\r\n");
    APPEND_TEXT("Average draw: "); APPEND_FIXED(snapshot->frame_metrics.average_draw_ms, 3); APPEND_TEXT(" ms\r\n");
    APPEND_TEXT("Average present: "); APPEND_FIXED(snapshot->frame_metrics.average_present_ms, 3); APPEND_TEXT(" ms\r\n");
    APPEND_TEXT("DirectX device-removal reason: "); APPEND_TEXT(snapshot->device_removal_reason != 0 && snapshot->device_removal_reason[0] != 0 ? snapshot->device_removal_reason : "None"); APPEND_TEXT("\r\n\r\n");
#undef APPEND_FIXED
#undef APPEND_INTEGER
#undef APPEND_TEXT
    smile_diagnostics_write(buffer);
}

void smile_graphics_diagnostics_shutdown(void)
{
    if (smile_diagnostics_file != INVALID_HANDLE_VALUE)
        CloseHandle(smile_diagnostics_file);
    smile_diagnostics_file = INVALID_HANDLE_VALUE;
    smile_diagnostics_enabled = 0;
}
