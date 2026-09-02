#define WIN32_LEAN_AND_MEAN
#define _WIN32_WINNT 0x0A00
#include <windows.h>
#include <windowsx.h>
#include <mmsystem.h>
#include <initguid.h>
#include <knownfolders.h>
#include <shlobj.h>
#include <limits.h>
#include <stdint.h>
#include "graphics/graphics_common.h"
#include "graphics/graphics3d.h"
#include "graphics/image_resource.h"
#include "graphics/graphics_diagnostics.h"
#include "input/pointer_state.h"
#include "timing/frame_clock_win32.h"
#include "audio/asset_path.h"
#include "audio/audio_focus.h"
#include "audio/audio_focus_state.h"
#include "audio/sfx_channels.h"

#define SMILE_KEY_NONE 0
#define SMILE_KEY_W 1
#define SMILE_KEY_A 2
#define SMILE_KEY_S 3
#define SMILE_KEY_D 4
#define SMILE_KEY_O 27
#define SMILE_KEY_F 28
#define SMILE_KEY_G 29
#define SMILE_KEY_R 30
#define SMILE_KEY_UP 10
#define SMILE_KEY_DOWN 11
#define SMILE_KEY_LEFT 12
#define SMILE_KEY_RIGHT 13
#define SMILE_KEY_ENTER 14
#define SMILE_KEY_ESCAPE 15
#define SMILE_KEY_SPACE 16
#define SMILE_KEY_1 17
#define SMILE_KEY_2 18
#define SMILE_KEY_OTHER 19
#define SMILE_KEY_3 20
#define SMILE_KEY_TAB 21
#define SMILE_KEY_4 22

static HWND smile_window;
static long long smile_logical_width = 960;
static long long smile_logical_height = 540;
static long long smile_closed;
static unsigned char smile_held[256];
static long long smile_key_queue[64];
static int smile_key_head;
static int smile_key_tail;
static SmilePointerState smile_pointer;
static int smile_fullscreen;
static DWORD smile_windowed_style;
static DWORD smile_windowed_ex_style;
static WINDOWPLACEMENT smile_windowed_placement = { sizeof(WINDOWPLACEMENT) };
static const WCHAR smile_window_class[] = L"SMILE20GameWindow";
static SmileFrameClock smile_frame_clock;
static SmileGraphicsBackendKind smile_requested_graphics_backend = SMILE_GRAPHICS_BACKEND_AUTO;
static int smile_vsync_enabled = 1;
static int smile_remember_window_placement;
static int smile_responsive_window;
static int smile_dpi_change_in_progress;
static SmileAudioFocusState smile_audio_focus = { 1, 1, 0, 1 };
static SmileMusicActivationCallback smile_music_activation_callback;
static char* smile_app_identity;
static long long smile_app_identity_length;
static char* smile_asset_manifest;
static long long smile_asset_manifest_length;

static void smile_pump_messages(void);
static void smile_toggle_fullscreen(void);
static void smile_update_game_audio_active(void);
static int smile_storage_data_path(const char* key, long long key_length, WCHAR* path, int capacity);
static uint32_t smile_data_u32(const unsigned char* value);
static void smile_data_put_u32(unsigned char* value, uint32_t number);
void smile_print_text(const char* text, long long length);
void smile_print_number(long long value);
void smile_print_newline(void);
int smile_resolve_asset_path_utf8(const char* path, long long length, WCHAR* resolved_path, int capacity);
void smile_play_sound_channel(const char* path, long long length, long long channel);

static uint32_t smile_window_placement_checksum(const unsigned char* record, SIZE_T length)
{
    uint32_t checksum = 2166136261u;
    SIZE_T index;
    for (index = 12; index < length; ++index)
    {
        checksum ^= record[index];
        checksum *= 16777619u;
    }
    return checksum;
}

static UINT smile_monitor_dpi(HMONITOR monitor)
{
    typedef HRESULT (WINAPI *SmileGetDpiForMonitor)(HMONITOR, int, UINT*, UINT*);
    HMODULE library;
    SmileGetDpiForMonitor function;
    UINT x = 0;
    UINT y = 0;
    library = LoadLibraryW(L"Shcore.dll");
    if (library != 0)
    {
        function = (SmileGetDpiForMonitor)GetProcAddress(library, "GetDpiForMonitor");
        if (function != 0 && SUCCEEDED(function(monitor, 0, &x, &y)) && x != 0)
        {
            FreeLibrary(library);
            return x;
        }
        FreeLibrary(library);
    }
    x = GetDpiForSystem();
    return x == 0 ? 96 : x;
}

static int smile_window_load_placement_v1(RECT* rectangle)
{
    static const char placement_key[] = "__smile_internal_window_placement_v1";
    WCHAR path[2048];
    unsigned char record[24];
    HANDLE file;
    LARGE_INTEGER size;
    DWORD read;
    int32_t x;
    int32_t y;
    int32_t width;
    int32_t height;
    int64_t right;
    int64_t bottom;
    if (!smile_remember_window_placement || rectangle == 0 ||
        !smile_storage_data_path(placement_key, sizeof(placement_key) - 1,
            path, (int)(sizeof(path) / sizeof(path[0]))))
        return 0;
    file = CreateFileW(path, GENERIC_READ, FILE_SHARE_READ, 0, OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL, 0);
    if (file == INVALID_HANDLE_VALUE)
        return 0;
    if (!GetFileSizeEx(file, &size) || size.QuadPart != (LONGLONG)sizeof(record) ||
        !ReadFile(file, record, (DWORD)sizeof(record), &read, 0) || read != sizeof(record))
    {
        CloseHandle(file);
        return 0;
    }
    CloseHandle(file);
    if (record[0] != 'S' || record[1] != 'M' || record[2] != 'W' || record[3] != 'P' ||
        smile_data_u32(record + 4) != 1)
        return 0;
    x = (int32_t)smile_data_u32(record + 8);
    y = (int32_t)smile_data_u32(record + 12);
    width = (int32_t)smile_data_u32(record + 16);
    height = (int32_t)smile_data_u32(record + 20);
    right = (int64_t)x + width;
    bottom = (int64_t)y + height;
    if (width < 160 || height < 120 || width > 32768 || height > 32768 ||
        right < LONG_MIN || right > LONG_MAX || bottom < LONG_MIN || bottom > LONG_MAX)
        return 0;
    rectangle->left = x;
    rectangle->top = y;
    rectangle->right = (LONG)right;
    rectangle->bottom = (LONG)bottom;
    return MonitorFromRect(rectangle, MONITOR_DEFAULTTONULL) != 0;
}

static int smile_window_load_placement(
    RECT* rectangle,
    long long* logical_width,
    long long* logical_height,
    int* show_command)
{
    static const char placement_key[] = "__smile_internal_window_placement_v2";
    WCHAR path[2048];
    unsigned char record[64];
    HANDLE file;
    LARGE_INTEGER size;
    DWORD read;
    RECT saved_work;
    RECT client;
    RECT work;
    MONITORINFO monitor_info = { sizeof(MONITORINFO) };
    HMONITOR monitor;
    UINT dpi;
    LONG outer_width;
    LONG outer_height;
    LONG x;
    LONG y;
    int32_t offset_x;
    int32_t offset_y;
    uint32_t width;
    uint32_t height;
    DWORD style = WS_OVERLAPPEDWINDOW;
    if (!smile_remember_window_placement || rectangle == 0 || logical_width == 0 ||
        logical_height == 0 || show_command == 0 ||
        !smile_storage_data_path(placement_key, sizeof(placement_key) - 1,
            path, (int)(sizeof(path) / sizeof(path[0]))))
        return 0;
    file = CreateFileW(path, GENERIC_READ, FILE_SHARE_READ, 0, OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL, 0);
    if (file == INVALID_HANDLE_VALUE)
        return smile_window_load_placement_v1(rectangle);
    if (!GetFileSizeEx(file, &size) || size.QuadPart != (LONGLONG)sizeof(record) ||
        !ReadFile(file, record, (DWORD)sizeof(record), &read, 0) || read != sizeof(record))
    {
        CloseHandle(file);
        return smile_window_load_placement_v1(rectangle);
    }
    CloseHandle(file);
    if (record[0] != 'S' || record[1] != 'M' || record[2] != 'W' || record[3] != 'P' ||
        smile_data_u32(record + 4) != 2 ||
        smile_data_u32(record + 8) != smile_window_placement_checksum(record, sizeof(record)))
        return smile_window_load_placement_v1(rectangle);
    width = smile_data_u32(record + 16);
    height = smile_data_u32(record + 20);
    if (width < 160 || height < 120 || width > 32768 || height > 32768)
        return smile_window_load_placement_v1(rectangle);
    saved_work.left = (LONG)(int32_t)smile_data_u32(record + 32);
    saved_work.top = (LONG)(int32_t)smile_data_u32(record + 36);
    saved_work.right = (LONG)(int32_t)smile_data_u32(record + 40);
    saved_work.bottom = (LONG)(int32_t)smile_data_u32(record + 44);
    monitor = MonitorFromRect(&saved_work, MONITOR_DEFAULTTONULL);
    if (monitor == 0)
        monitor = MonitorFromWindow(GetDesktopWindow(), MONITOR_DEFAULTTOPRIMARY);
    if (monitor == 0 || !GetMonitorInfoW(monitor, &monitor_info))
        return smile_window_load_placement_v1(rectangle);
    work = monitor_info.rcWork;
    dpi = smile_monitor_dpi(monitor);
    client.left = 0;
    client.top = 0;
    client.right = MulDiv((int)width, (int)dpi, 96);
    client.bottom = MulDiv((int)height, (int)dpi, 96);
    if (!AdjustWindowRectExForDpi(&client, style, FALSE, 0, dpi))
        return smile_window_load_placement_v1(rectangle);
    outer_width = client.right - client.left;
    outer_height = client.bottom - client.top;
    if (outer_width > work.right - work.left)
        outer_width = work.right - work.left;
    if (outer_height > work.bottom - work.top)
        outer_height = work.bottom - work.top;
    offset_x = (int32_t)smile_data_u32(record + 24);
    offset_y = (int32_t)smile_data_u32(record + 28);
    x = work.left + MulDiv(offset_x, (int)dpi, 96);
    y = work.top + MulDiv(offset_y, (int)dpi, 96);
    if (x < work.left) x = work.left;
    if (y < work.top) y = work.top;
    if (x + outer_width > work.right) x = work.right - outer_width;
    if (y + outer_height > work.bottom) y = work.bottom - outer_height;
    rectangle->left = x;
    rectangle->top = y;
    rectangle->right = x + outer_width;
    rectangle->bottom = y + outer_height;
    *logical_width = width;
    *logical_height = height;
    *show_command = smile_data_u32(record + 48) == SW_SHOWMAXIMIZED
        ? SW_SHOWMAXIMIZED : SW_SHOWNORMAL;
    return 1;
}

static void smile_window_save_placement(void)
{
    static const char placement_key[] = "__smile_internal_window_placement_v2";
    WCHAR path[2048];
    WCHAR temporary[2048];
    WINDOWPLACEMENT placement = { sizeof(WINDOWPLACEMENT) };
    RECT rectangle;
    unsigned char record[64];
    HANDLE file = INVALID_HANDLE_VALUE;
    DWORD written;
    int path_length;
    RECT client;
    RECT nonclient = { 0, 0, 0, 0 };
    MONITORINFO monitor_info = { sizeof(MONITORINFO) };
    HMONITOR monitor;
    UINT dpi;
    DWORD style;
    LONG client_width;
    LONG client_height;
    if (!smile_remember_window_placement || smile_window == 0 ||
        !smile_storage_data_path(placement_key, sizeof(placement_key) - 1,
            path, (int)(sizeof(path) / sizeof(path[0]))))
        return;
    if (smile_fullscreen && smile_windowed_placement.length == sizeof(WINDOWPLACEMENT))
        rectangle = smile_windowed_placement.rcNormalPosition;
    else if (GetWindowPlacement(smile_window, &placement) &&
        placement.showCmd != SW_SHOWNORMAL)
        rectangle = placement.rcNormalPosition;
    else if (!GetWindowRect(smile_window, &rectangle))
        return;
    monitor = MonitorFromRect(&rectangle, MONITOR_DEFAULTTONEAREST);
    if (monitor == 0 || !GetMonitorInfoW(monitor, &monitor_info))
        return;
    dpi = smile_window != 0 ? GetDpiForWindow(smile_window) : smile_monitor_dpi(monitor);
    if (dpi == 0) dpi = 96;
    style = smile_fullscreen ? smile_windowed_style : (DWORD)GetWindowLongPtrW(smile_window, GWL_STYLE);
    if (!AdjustWindowRectExForDpi(&nonclient, style, FALSE, 0, dpi))
        return;
    client_width = (rectangle.right - rectangle.left) - (nonclient.right - nonclient.left);
    client_height = (rectangle.bottom - rectangle.top) - (nonclient.bottom - nonclient.top);
    if (client_width < 1 || client_height < 1)
    {
        if (!GetClientRect(smile_window, &client))
            return;
        client_width = client.right - client.left;
        client_height = client.bottom - client.top;
    }
    if (client_width < 160 || client_height < 120)
        return;
    path_length = lstrlenW(path);
    if (path_length + 4 >= (int)(sizeof(temporary) / sizeof(temporary[0])))
        return;
    lstrcpyW(temporary, path);
    lstrcatW(temporary, L".tmp");
    ZeroMemory(record, sizeof(record));
    record[0] = 'S'; record[1] = 'M'; record[2] = 'W'; record[3] = 'P';
    smile_data_put_u32(record + 4, 2);
    smile_data_put_u32(record + 12, dpi);
    smile_data_put_u32(record + 16, (uint32_t)MulDiv(client_width, 96, (int)dpi));
    smile_data_put_u32(record + 20, (uint32_t)MulDiv(client_height, 96, (int)dpi));
    smile_data_put_u32(record + 24, (uint32_t)(int32_t)MulDiv(
        rectangle.left - monitor_info.rcWork.left, 96, (int)dpi));
    smile_data_put_u32(record + 28, (uint32_t)(int32_t)MulDiv(
        rectangle.top - monitor_info.rcWork.top, 96, (int)dpi));
    smile_data_put_u32(record + 32, (uint32_t)(int32_t)monitor_info.rcWork.left);
    smile_data_put_u32(record + 36, (uint32_t)(int32_t)monitor_info.rcWork.top);
    smile_data_put_u32(record + 40, (uint32_t)(int32_t)monitor_info.rcWork.right);
    smile_data_put_u32(record + 44, (uint32_t)(int32_t)monitor_info.rcWork.bottom);
    smile_data_put_u32(record + 48, placement.showCmd == SW_SHOWMAXIMIZED
        ? SW_SHOWMAXIMIZED : SW_SHOWNORMAL);
    smile_data_put_u32(record + 8, smile_window_placement_checksum(record, sizeof(record)));
    file = CreateFileW(temporary, GENERIC_WRITE, 0, 0, CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL, 0);
    if (file == INVALID_HANDLE_VALUE)
        return;
    if (!WriteFile(file, record, (DWORD)sizeof(record), &written, 0) ||
        written != sizeof(record) || !FlushFileBuffers(file))
    {
        CloseHandle(file);
        DeleteFileW(temporary);
        return;
    }
    CloseHandle(file);
    if (!MoveFileExW(temporary, path, MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH))
        DeleteFileW(temporary);
}

static void smile_pointer_position(LPARAM lparam)
{
    RECT client;
    SmileGraphicsViewport viewport;
    long long logical_x;
    long long logical_y;
    int inside;
    int physical_x = GET_X_LPARAM(lparam);
    int physical_y = GET_Y_LPARAM(lparam);
    if (smile_window == 0 || !GetClientRect(smile_window, &client))
        return;
    smile_graphics_calculate_viewport(smile_logical_width, smile_logical_height,
        client.right - client.left, client.bottom - client.top, &viewport);
    if (viewport.scale <= 0.0)
        return;
    logical_x = smile_graphics_round_pixel(((double)physical_x - viewport.x) / viewport.scale);
    logical_y = smile_graphics_round_pixel(((double)physical_y - viewport.y) / viewport.scale);
    inside = physical_x >= viewport.x && physical_y >= viewport.y &&
        physical_x < viewport.x + viewport.width && physical_y < viewport.y + viewport.height;
    smile_pointer_state_position(&smile_pointer, logical_x, logical_y, inside);
}

static void smile_pointer_press(long long button)
{
    if (smile_pointer_state_press(&smile_pointer, button) && smile_window != 0)
        SetCapture(smile_window);
}

static void smile_pointer_release(long long button)
{
    if (!smile_pointer_state_release(&smile_pointer, button))
        return;
    if (smile_pointer.held_buttons == 0 && GetCapture() == smile_window)
        ReleaseCapture();
}

static void smile_pointer_cancel(void)
{
    smile_pointer_state_cancel(&smile_pointer);
}

static void smile_pointer_reset(void)
{
    smile_pointer_state_reset(&smile_pointer);
}

static void smile_zero_memory(void* memory, SIZE_T length)
{
    volatile unsigned char* current = (volatile unsigned char*)memory;
    while (length-- != 0)
        *current++ = 0;
}

static void smile_copy_bytes(char* destination, const char* source, SIZE_T length)
{
    while (length-- != 0)
        *destination++ = *source++;
}

static int smile_bytes_equal(const char* left, const char* right, SIZE_T length)
{
    while (length-- != 0)
        if (*left++ != *right++)
            return 0;
    return 1;
}

typedef struct SmileSha256
{
    uint32_t state[8];
    uint64_t bit_length;
    unsigned char block[64];
    unsigned int block_length;
} SmileSha256;

static uint32_t smile_sha_rotate(uint32_t value, unsigned int count)
{
    return (value >> count) | (value << (32 - count));
}

static void smile_sha_transform(SmileSha256* sha, const unsigned char* block)
{
    static const uint32_t constants[64] = {
        0x428a2f98,0x71374491,0xb5c0fbcf,0xe9b5dba5,0x3956c25b,0x59f111f1,0x923f82a4,0xab1c5ed5,
        0xd807aa98,0x12835b01,0x243185be,0x550c7dc3,0x72be5d74,0x80deb1fe,0x9bdc06a7,0xc19bf174,
        0xe49b69c1,0xefbe4786,0x0fc19dc6,0x240ca1cc,0x2de92c6f,0x4a7484aa,0x5cb0a9dc,0x76f988da,
        0x983e5152,0xa831c66d,0xb00327c8,0xbf597fc7,0xc6e00bf3,0xd5a79147,0x06ca6351,0x14292967,
        0x27b70a85,0x2e1b2138,0x4d2c6dfc,0x53380d13,0x650a7354,0x766a0abb,0x81c2c92e,0x92722c85,
        0xa2bfe8a1,0xa81a664b,0xc24b8b70,0xc76c51a3,0xd192e819,0xd6990624,0xf40e3585,0x106aa070,
        0x19a4c116,0x1e376c08,0x2748774c,0x34b0bcb5,0x391c0cb3,0x4ed8aa4a,0x5b9cca4f,0x682e6ff3,
        0x748f82ee,0x78a5636f,0x84c87814,0x8cc70208,0x90befffa,0xa4506ceb,0xbef9a3f7,0xc67178f2
    };
    uint32_t words[64];
    uint32_t a,b,c,d,e,f,g,h,first,second,s0,s1,choice,majority;
    unsigned int index;
    for (index = 0; index < 16; ++index)
        words[index] = ((uint32_t)block[index * 4] << 24) | ((uint32_t)block[index * 4 + 1] << 16) |
            ((uint32_t)block[index * 4 + 2] << 8) | block[index * 4 + 3];
    for (index = 16; index < 64; ++index)
    {
        s0 = smile_sha_rotate(words[index - 15], 7) ^ smile_sha_rotate(words[index - 15], 18) ^ (words[index - 15] >> 3);
        s1 = smile_sha_rotate(words[index - 2], 17) ^ smile_sha_rotate(words[index - 2], 19) ^ (words[index - 2] >> 10);
        words[index] = words[index - 16] + s0 + words[index - 7] + s1;
    }
    a=sha->state[0]; b=sha->state[1]; c=sha->state[2]; d=sha->state[3];
    e=sha->state[4]; f=sha->state[5]; g=sha->state[6]; h=sha->state[7];
    for (index = 0; index < 64; ++index)
    {
        s1 = smile_sha_rotate(e, 6) ^ smile_sha_rotate(e, 11) ^ smile_sha_rotate(e, 25);
        choice = (e & f) ^ (~e & g);
        first = h + s1 + choice + constants[index] + words[index];
        s0 = smile_sha_rotate(a, 2) ^ smile_sha_rotate(a, 13) ^ smile_sha_rotate(a, 22);
        majority = (a & b) ^ (a & c) ^ (b & c);
        second = s0 + majority;
        h=g; g=f; f=e; e=d+first; d=c; c=b; b=a; a=first+second;
    }
    sha->state[0]+=a; sha->state[1]+=b; sha->state[2]+=c; sha->state[3]+=d;
    sha->state[4]+=e; sha->state[5]+=f; sha->state[6]+=g; sha->state[7]+=h;
}

static void smile_sha_initialize(SmileSha256* sha)
{
    static const uint32_t initial[8] = { 0x6a09e667,0xbb67ae85,0x3c6ef372,0xa54ff53a,
        0x510e527f,0x9b05688c,0x1f83d9ab,0x5be0cd19 };
    smile_zero_memory(sha, sizeof(*sha));
    CopyMemory(sha->state, initial, sizeof(initial));
}

static void smile_sha_update(SmileSha256* sha, const unsigned char* data, SIZE_T length)
{
    SIZE_T index;
    for (index = 0; index < length; ++index)
    {
        sha->block[sha->block_length++] = data[index];
        if (sha->block_length == 64)
        {
            smile_sha_transform(sha, sha->block);
            sha->bit_length += 512;
            sha->block_length = 0;
        }
    }
}

static void smile_sha_finish(SmileSha256* sha, unsigned char digest[32])
{
    unsigned int index = sha->block_length;
    uint64_t length;
    sha->block[index++] = 0x80;
    if (index > 56)
    {
        while (index < 64) sha->block[index++] = 0;
        smile_sha_transform(sha, sha->block);
        index = 0;
    }
    while (index < 56) sha->block[index++] = 0;
    length = sha->bit_length + (uint64_t)sha->block_length * 8;
    for (index = 0; index < 8; ++index) sha->block[63 - index] = (unsigned char)(length >> (index * 8));
    smile_sha_transform(sha, sha->block);
    for (index = 0; index < 8; ++index)
    {
        digest[index * 4] = (unsigned char)(sha->state[index] >> 24);
        digest[index * 4 + 1] = (unsigned char)(sha->state[index] >> 16);
        digest[index * 4 + 2] = (unsigned char)(sha->state[index] >> 8);
        digest[index * 4 + 3] = (unsigned char)sha->state[index];
    }
}

static void smile_sha_bytes(const unsigned char* data, SIZE_T length, unsigned char digest[32])
{
    SmileSha256 sha;
    smile_sha_initialize(&sha);
    smile_sha_update(&sha, data, length);
    smile_sha_finish(&sha, digest);
}

void smile_media_configure(const char* app_identity, long long app_length,
    const char* asset_manifest, long long manifest_length)
{
    if (smile_app_identity != 0) HeapFree(GetProcessHeap(), 0, smile_app_identity);
    if (smile_asset_manifest != 0) HeapFree(GetProcessHeap(), 0, smile_asset_manifest);
    smile_app_identity = 0;
    smile_asset_manifest = 0;
    smile_app_identity_length = 0;
    smile_asset_manifest_length = 0;
    if (app_identity != 0 && app_length > 0 && app_length <= 4096)
    {
        smile_app_identity = (char*)HeapAlloc(GetProcessHeap(), 0, (SIZE_T)app_length);
        if (smile_app_identity != 0)
        {
            smile_copy_bytes(smile_app_identity, app_identity, (SIZE_T)app_length);
            smile_app_identity_length = app_length;
        }
    }
    if (asset_manifest != 0 && manifest_length > 0 && manifest_length <= 16 * 1024 * 1024)
    {
        smile_asset_manifest = (char*)HeapAlloc(GetProcessHeap(), 0, (SIZE_T)manifest_length);
        if (smile_asset_manifest != 0)
        {
            smile_copy_bytes(smile_asset_manifest, asset_manifest, (SIZE_T)manifest_length);
            smile_asset_manifest_length = manifest_length;
        }
    }
}

static HANDLE smile_output(void)
{
    return GetStdHandle(STD_OUTPUT_HANDLE);
}

typedef struct SmileText
{
    volatile LONG64 references;
    long long length;
    char bytes[1];
} SmileText;

static volatile LONG64 smile_text_allocations;
static volatile LONG64 smile_text_frees;
static volatile LONG64 smile_text_live_objects;

typedef void (*SmileClassFinalizer)(void* value);

typedef struct SmileClassHeader
{
    volatile LONG64 references;
    SmileClassFinalizer finalizer;
} SmileClassHeader;

static volatile LONG64 smile_class_allocations;
static volatile LONG64 smile_class_frees;
static volatile LONG64 smile_class_live_objects;
static volatile LONG smile_class_allocation_fault_initialized;
static long long smile_class_allocation_fail_after = -1;

static void smile_class_initialize_allocation_fault(void)
{
    WCHAR text[64];
    DWORD length;
    long long value = 0;
    DWORD index;
    if (InterlockedCompareExchange(&smile_class_allocation_fault_initialized, 1, 0) != 0)
        return;
    length = GetEnvironmentVariableW(L"SMILE_CLASS_ALLOCATION_FAIL_AFTER", text,
        (DWORD)(sizeof(text) / sizeof(text[0])));
    if (length == 0 || length >= (DWORD)(sizeof(text) / sizeof(text[0])))
        return;
    for (index = 0; index < length; index++)
    {
        long long digit;
        if (text[index] < L'0' || text[index] > L'9')
            return;
        digit = (long long)(text[index] - L'0');
        if (value > (LLONG_MAX - digit) / 10)
            return;
        value = value * 10 + digit;
    }
    smile_class_allocation_fail_after = value;
}

void* smile_class_allocate(long long payload_size, SmileClassFinalizer finalizer)
{
    SIZE_T bytes;
    SmileClassHeader* header;
    smile_class_initialize_allocation_fault();
    if (smile_class_allocation_fail_after >= 0 &&
        InterlockedCompareExchange64(&smile_class_allocations, 0, 0) >= smile_class_allocation_fail_after)
        return 0;
    if (payload_size < 0 || (unsigned long long)payload_size >
        (unsigned long long)(SIZE_MAX - sizeof(SmileClassHeader)))
        return 0;
    bytes = sizeof(SmileClassHeader) + (SIZE_T)(payload_size == 0 ? 1 : payload_size);
    header = (SmileClassHeader*)HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, bytes);
    if (header == 0)
        return 0;
    header->references = 1;
    header->finalizer = finalizer;
    InterlockedIncrement64(&smile_class_allocations);
    InterlockedIncrement64(&smile_class_live_objects);
    return (void*)(header + 1);
}

void* smile_class_retain(void* value)
{
    SmileClassHeader* header;
    if (value == 0)
        return 0;
    header = ((SmileClassHeader*)value) - 1;
    InterlockedIncrement64(&header->references);
    return value;
}

void smile_class_release(void* value)
{
    SmileClassHeader* header;
    if (value == 0)
        return;
    header = ((SmileClassHeader*)value) - 1;
    if (InterlockedDecrement64(&header->references) != 0)
        return;
    if (header->finalizer != 0)
        header->finalizer(value);
    InterlockedIncrement64(&smile_class_frees);
    InterlockedDecrement64(&smile_class_live_objects);
    HeapFree(GetProcessHeap(), 0, header);
}

void smile_class_move_assign(void** target, void* owned_value)
{
    void* previous;
    if (target == 0)
    {
        smile_class_release(owned_value);
        return;
    }
    previous = *target;
    *target = owned_value;
    smile_class_release(previous);
}

void smile_class_clear(void** target)
{
    void* previous;
    if (target == 0)
        return;
    previous = *target;
    *target = 0;
    smile_class_release(previous);
}

long long smile_class_allocation_count(void)
{
    return InterlockedCompareExchange64(&smile_class_allocations, 0, 0);
}

long long smile_class_free_count(void)
{
    return InterlockedCompareExchange64(&smile_class_frees, 0, 0);
}

long long smile_class_live_count(void)
{
    return InterlockedCompareExchange64(&smile_class_live_objects, 0, 0);
}

void smile_class_lifetime_report(void)
{
    WCHAR enabled[2];
    static const char prefix[] = "SMILE_CLASS_LIVE=";
    if (GetEnvironmentVariableW(L"SMILE_CLASS_LIFETIME_DIAGNOSTICS", enabled, 2) == 0)
        return;
    smile_print_text(prefix, (long long)(sizeof(prefix) - 1));
    smile_print_number(smile_class_live_count());
    smile_print_newline();
}

void smile_class_nothing_report(void)
{
    static const char message[] = "SMILE runtime error: Object reference is Nothing.";
    smile_print_text(message, (long long)(sizeof(message) - 1));
    smile_print_newline();
}

void smile_class_allocation_failure_report(void)
{
    static const char message[] = "SMILE runtime error: Class allocation failed.";
    smile_print_text(message, (long long)(sizeof(message) - 1));
    smile_print_newline();
}

void smile_image_lifetime_report(void)
{
    WCHAR enabled[2];
    static const char prefix[] = "SMILE_IMAGE_LIVE=";
    if (GetEnvironmentVariableW(L"SMILE_IMAGE_LIFETIME_DIAGNOSTICS", enabled, 2) == 0)
        return;
    smile_print_text(prefix, (long long)(sizeof(prefix) - 1));
    smile_print_number(smile_image_resource_live_count());
    smile_print_newline();
}

static const char* smile_text_bytes(const SmileText* text)
{
    static const char empty[] = "";
    return text == 0 ? empty : text->bytes;
}

static long long smile_text_length(const SmileText* text)
{
    return text == 0 ? 0 : text->length;
}

void* smile_text_retain(void* value)
{
    SmileText* text = (SmileText*)value;
    if (text != 0 && text->references >= 0)
        InterlockedIncrement64(&text->references);
    return text;
}

void smile_text_release(void* value)
{
    SmileText* text = (SmileText*)value;
    if (text != 0 && text->references >= 0 && InterlockedDecrement64(&text->references) == 0)
    {
        InterlockedIncrement64(&smile_text_frees);
        InterlockedDecrement64(&smile_text_live_objects);
        HeapFree(GetProcessHeap(), 0, text);
    }
}

static SmileText* smile_text_allocate(long long length)
{
    SIZE_T bytes;
    SmileText* text;
    if (length <= 0)
        return 0;
    if ((unsigned long long)length > (unsigned long long)(SIZE_MAX - sizeof(SmileText)))
        ExitProcess(2);
    bytes = sizeof(SmileText) + (SIZE_T)length;
    text = (SmileText*)HeapAlloc(GetProcessHeap(), 0, bytes);
    if (text == 0)
        ExitProcess(2);
    text->references = 1;
    text->length = length;
    text->bytes[length] = 0;
    InterlockedIncrement64(&smile_text_allocations);
    InterlockedIncrement64(&smile_text_live_objects);
    return text;
}

static int smile_utf8_scalar(const char* bytes, long long length, long long* offset, unsigned int* scalar)
{
    const unsigned char* input = (const unsigned char*)bytes;
    long long index;
    unsigned int first;
    unsigned int value;
    int count;
    if (bytes == 0 || offset == 0 || scalar == 0 || *offset < 0 || *offset >= length)
        return 0;
    index = *offset;
    first = input[index];
    if (first < 0x80)
    {
        *scalar = first;
        *offset = index + 1;
        return 1;
    }
    if (first >= 0xc2 && first <= 0xdf) { value = first & 0x1f; count = 1; }
    else if (first >= 0xe0 && first <= 0xef) { value = first & 0x0f; count = 2; }
    else if (first >= 0xf0 && first <= 0xf4) { value = first & 0x07; count = 3; }
    else return 0;
    if (index > length - count - 1)
        return 0;
    if (count >= 2 && ((first == 0xe0 && input[index + 1] < 0xa0) ||
        (first == 0xed && input[index + 1] >= 0xa0)))
        return 0;
    if (count == 3 && ((first == 0xf0 && input[index + 1] < 0x90) ||
        (first == 0xf4 && input[index + 1] >= 0x90)))
        return 0;
    while (count-- > 0)
    {
        unsigned int next = input[++index];
        if ((next & 0xc0) != 0x80)
            return 0;
        value = (value << 6) | (next & 0x3f);
    }
    if (value > 0x10ffff || (value >= 0xd800 && value <= 0xdfff))
        return 0;
    *scalar = value;
    *offset = index + 1;
    return 1;
}

long long smile_text_scalar_length(void* owned_value)
{
    SmileText* text = (SmileText*)owned_value;
    const char* bytes = smile_text_bytes(text);
    long long length = smile_text_length(text);
    long long offset = 0;
    long long count = 0;
    unsigned int scalar;
    while (offset < length)
    {
        if (!smile_utf8_scalar(bytes, length, &offset, &scalar))
        {
            count = -1;
            break;
        }
        count++;
    }
    smile_text_release(text);
    return count;
}

long long smile_text_code_at(void* owned_value, long long requested_index)
{
    SmileText* text = (SmileText*)owned_value;
    const char* bytes = smile_text_bytes(text);
    long long length = smile_text_length(text);
    long long offset = 0;
    long long index = 0;
    long long result = -1;
    unsigned int scalar;
    if (requested_index >= 0)
    {
        while (offset < length)
        {
            if (!smile_utf8_scalar(bytes, length, &offset, &scalar))
                break;
            if (index++ == requested_index)
            {
                result = (long long)scalar;
                break;
            }
        }
    }
    smile_text_release(text);
    return result;
}

void* smile_text_slice(void* owned_value, long long start, long long count)
{
    SmileText* text = (SmileText*)owned_value;
    const char* bytes = smile_text_bytes(text);
    long long length = smile_text_length(text);
    long long offset = 0;
    long long index = 0;
    long long copied = 0;
    long long byte_start = -1;
    long long byte_end = -1;
    unsigned int scalar;
    SmileText* result = 0;
    if (start >= 0 && count > 0)
    {
        while (offset < length)
        {
            long long scalar_start = offset;
            if (!smile_utf8_scalar(bytes, length, &offset, &scalar))
            {
                byte_start = -1;
                break;
            }
            if (index >= start)
            {
                if (byte_start < 0) byte_start = scalar_start;
                byte_end = offset;
                copied++;
                if (copied >= count) break;
            }
            index++;
        }
    }
    if (byte_start >= 0 && byte_end > byte_start)
    {
        result = smile_text_allocate(byte_end - byte_start);
        smile_copy_bytes(result->bytes, bytes + byte_start, (SIZE_T)(byte_end - byte_start));
    }
    smile_text_release(text);
    return result;
}

void smile_text_move_assign(void** target, void* owned_value)
{
    void* previous;
    if (target == 0)
    {
        smile_text_release(owned_value);
        return;
    }
    previous = *target;
    *target = owned_value;
    smile_text_release(previous);
}

void smile_text_clear(void** target)
{
    smile_text_move_assign(target, 0);
}

void* smile_text_concat(void* owned_left, void* owned_right)
{
    SmileText* left = (SmileText*)owned_left;
    SmileText* right = (SmileText*)owned_right;
    long long left_length = smile_text_length(left);
    long long right_length = smile_text_length(right);
    long long total;
    SmileText* result;
    if (right_length > LLONG_MAX - left_length)
        ExitProcess(2);
    total = left_length + right_length;
    result = smile_text_allocate(total);
    if (left_length != 0)
        smile_copy_bytes(result->bytes, smile_text_bytes(left), (SIZE_T)left_length);
    if (right_length != 0)
        smile_copy_bytes(result->bytes + left_length, smile_text_bytes(right), (SIZE_T)right_length);
    smile_text_release(left);
    smile_text_release(right);
    return result;
}

static long long smile_text_content_equal(const SmileText* left, const SmileText* right)
{
    long long length = smile_text_length(left);
    if (length != smile_text_length(right))
        return 0;
    if (length == 0)
        return 1;
    return smile_bytes_equal(smile_text_bytes(left), smile_text_bytes(right), (SIZE_T)length);
}

long long smile_text_equal(void* owned_left, void* owned_right)
{
    long long equal = smile_text_content_equal((SmileText*)owned_left, (SmileText*)owned_right);
    smile_text_release(owned_left);
    smile_text_release(owned_right);
    return equal;
}

long long smile_text_equal_case(void* borrowed_left, void* owned_right)
{
    long long equal = smile_text_content_equal((SmileText*)borrowed_left, (SmileText*)owned_right);
    smile_text_release(owned_right);
    return equal;
}

void smile_print_text_value(void* owned_value)
{
    SmileText* text = (SmileText*)owned_value;
    smile_print_text(smile_text_bytes(text), smile_text_length(text));
    smile_text_release(text);
}

static SIZE_T smile_utf8_output_chunk(const char* text, SIZE_T remaining)
{
    const SIZE_T maximum = 16384;
    SIZE_T length = remaining < maximum ? remaining : maximum;
    SIZE_T lead;
    unsigned char value;
    int expected;
    if (length == remaining)
        return length;
    lead = length;
    while (lead > 0 && (((unsigned char)text[lead]) & 0xc0) == 0x80)
        lead--;
    value = (unsigned char)text[lead];
    expected = value < 0x80 ? 1 : value < 0xe0 ? 2 : value < 0xf0 ? 3 : 4;
    if (lead + (SIZE_T)expected > length)
        length = lead;
    return length == 0 ? (remaining < maximum ? remaining : maximum) : length;
}

static int smile_write_console_utf8(HANDLE output, const char* text, SIZE_T length)
{
    while (length != 0)
    {
        SIZE_T input_length = smile_utf8_output_chunk(text, length);
        int wide_length = MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, text, (int)input_length, 0, 0);
        WCHAR* wide;
        int offset = 0;
        if (wide_length <= 0)
            return 0;
        wide = (WCHAR*)HeapAlloc(GetProcessHeap(), 0, (SIZE_T)wide_length * sizeof(WCHAR));
        if (wide == 0)
            return 0;
        if (MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, text, (int)input_length,
            wide, wide_length) != wide_length)
        {
            HeapFree(GetProcessHeap(), 0, wide);
            return 0;
        }
        while (offset < wide_length)
        {
            DWORD requested = (DWORD)(wide_length - offset);
            DWORD written = 0;
            if (requested > 8192)
                requested = 8192;
            if (offset + (int)requested < wide_length && requested > 0 &&
                wide[offset + requested - 1] >= 0xd800 && wide[offset + requested - 1] <= 0xdbff)
                requested--;
            if (requested == 0 || !WriteConsoleW(output, wide + offset, requested, &written, 0) || written == 0)
            {
                HeapFree(GetProcessHeap(), 0, wide);
                return 0;
            }
            offset += (int)written;
        }
        HeapFree(GetProcessHeap(), 0, wide);
        text += input_length;
        length -= input_length;
    }
    return 1;
}

static int smile_write_utf8(HANDLE output, const char* text, unsigned long long length)
{
    while (length != 0)
    {
        DWORD requested = length > MAXDWORD ? MAXDWORD : (DWORD)length;
        DWORD written = 0;
        if (!WriteFile(output, text, requested, &written, 0) || written == 0)
            return 0;
        text += written;
        length -= written;
    }
    return 1;
}

void smile_print_text(const char* text, long long length)
{
    HANDLE output;
    DWORD mode;
    if (text == 0 || length <= 0)
        return;
    output = smile_output();
    if (output == 0 || output == INVALID_HANDLE_VALUE)
        return;
    if (GetConsoleMode(output, &mode))
        (void)smile_write_console_utf8(output, text, (SIZE_T)length);
    else
        (void)smile_write_utf8(output, text, (unsigned long long)length);
}

static int smile_format_number(long long value, char* buffer, int capacity)
{
    char temporary[32];
    int count = 0;
    int output = 0;
    unsigned long long magnitude;
    if (capacity <= 0)
        return 0;
    magnitude = value < 0 ? (unsigned long long)(-(value + 1)) + 1 : (unsigned long long)value;
    do
    {
        temporary[count++] = (char)('0' + (magnitude % 10));
        magnitude /= 10;
    }
    while (magnitude != 0 && count < (int)sizeof(temporary));
    if (value < 0 && output < capacity)
        buffer[output++] = '-';
    while (count > 0 && output < capacity)
        buffer[output++] = temporary[--count];
    return output;
}

void smile_print_number(long long value)
{
    char buffer[32];
    int length = smile_format_number(value, buffer, (int)sizeof(buffer));
    smile_print_text(buffer, length);
}

void smile_print_boolean(long long value)
{
    static const char true_text[] = "True";
    static const char false_text[] = "False";
    if (value != 0)
        smile_print_text(true_text, 4);
    else
        smile_print_text(false_text, 5);
}

void smile_print_newline(void)
{
    static const char newline[] = "\r\n";
    smile_print_text(newline, 2);
}

long long smile_text_allocation_count(void) { return InterlockedCompareExchange64(&smile_text_allocations, 0, 0); }
long long smile_text_free_count(void) { return InterlockedCompareExchange64(&smile_text_frees, 0, 0); }
long long smile_text_live_count(void) { return InterlockedCompareExchange64(&smile_text_live_objects, 0, 0); }

void smile_text_lifetime_report(void)
{
    WCHAR enabled[2];
    static const char prefix[] = "SMILE_TEXT_LIVE=";
    if (GetEnvironmentVariableW(L"SMILE_TEXT_LIFETIME_DIAGNOSTICS", enabled, 2) == 0)
        return;
    smile_print_text(prefix, (long long)(sizeof(prefix) - 1));
    smile_print_number(smile_text_live_count());
    smile_print_newline();
}

static long long smile_map_key(WCHAR character, WORD virtual_key)
{
    if (character == L'w' || character == L'W' || virtual_key == 'W') return SMILE_KEY_W;
    if (character == L'a' || character == L'A' || virtual_key == 'A') return SMILE_KEY_A;
    if (character == L's' || character == L'S' || virtual_key == 'S') return SMILE_KEY_S;
    if (character == L'd' || character == L'D' || virtual_key == 'D') return SMILE_KEY_D;
    if (character == L'o' || character == L'O' || virtual_key == 'O') return SMILE_KEY_O;
    if (character == L'f' || character == L'F' || virtual_key == 'F') return SMILE_KEY_F;
    if (character == L'g' || character == L'G' || virtual_key == 'G') return SMILE_KEY_G;
    if (character == L'r' || character == L'R' || virtual_key == 'R') return SMILE_KEY_R;
    if (virtual_key == VK_UP) return SMILE_KEY_UP;
    if (virtual_key == VK_DOWN) return SMILE_KEY_DOWN;
    if (virtual_key == VK_LEFT) return SMILE_KEY_LEFT;
    if (virtual_key == VK_RIGHT) return SMILE_KEY_RIGHT;
    if (virtual_key == VK_RETURN) return SMILE_KEY_ENTER;
    if (virtual_key == VK_ESCAPE) return SMILE_KEY_ESCAPE;
    if (virtual_key == VK_SPACE) return SMILE_KEY_SPACE;
    if (virtual_key == '1') return SMILE_KEY_1;
    if (virtual_key == '2') return SMILE_KEY_2;
    if (virtual_key == '3') return SMILE_KEY_3;
    if (virtual_key == '4') return SMILE_KEY_4;
    if (virtual_key == VK_TAB) return SMILE_KEY_TAB;
    return character != 0 || virtual_key != 0 ? SMILE_KEY_OTHER : SMILE_KEY_NONE;
}

static int smile_key_virtual(long long key)
{
    switch (key)
    {
        case SMILE_KEY_W: return 'W';
        case SMILE_KEY_A: return 'A';
        case SMILE_KEY_S: return 'S';
        case SMILE_KEY_D: return 'D';
        case SMILE_KEY_O: return 'O';
        case SMILE_KEY_F: return 'F';
        case SMILE_KEY_G: return 'G';
        case SMILE_KEY_R: return 'R';
        case SMILE_KEY_UP: return VK_UP;
        case SMILE_KEY_DOWN: return VK_DOWN;
        case SMILE_KEY_LEFT: return VK_LEFT;
        case SMILE_KEY_RIGHT: return VK_RIGHT;
        case SMILE_KEY_ENTER: return VK_RETURN;
        case SMILE_KEY_ESCAPE: return VK_ESCAPE;
        case SMILE_KEY_SPACE: return VK_SPACE;
        case SMILE_KEY_1: return '1';
        case SMILE_KEY_2: return '2';
        case SMILE_KEY_3: return '3';
        case SMILE_KEY_4: return '4';
        case SMILE_KEY_TAB: return VK_TAB;
        default: return 0;
    }
}

static void smile_queue_key(long long key)
{
    int next;
    if (key == SMILE_KEY_NONE)
        return;
    next = (smile_key_tail + 1) % (int)(sizeof(smile_key_queue) / sizeof(smile_key_queue[0]));
    if (next == smile_key_head)
        return;
    smile_key_queue[smile_key_tail] = key;
    smile_key_tail = next;
}

long long smile_get_key(void)
{
    if (smile_window != 0 || smile_closed != 0)
    {
        long long key;
        smile_pump_messages();
        if (smile_key_head == smile_key_tail)
            return SMILE_KEY_NONE;
        key = smile_key_queue[smile_key_head];
        smile_key_head = (smile_key_head + 1) % (int)(sizeof(smile_key_queue) / sizeof(smile_key_queue[0]));
        return key;
    }
    else
    {
        HANDLE input = GetStdHandle(STD_INPUT_HANDLE);
        DWORD file_type = GetFileType(input);
        if (file_type == FILE_TYPE_CHAR)
        {
            INPUT_RECORD record;
            DWORD available;
            DWORD read;
            while (PeekConsoleInputW(input, &record, 1, &available) && available != 0)
            {
                if (!ReadConsoleInputW(input, &record, 1, &read) || read == 0)
                    break;
                if (record.EventType == KEY_EVENT && record.Event.KeyEvent.bKeyDown)
                {
                    long long key = smile_map_key(record.Event.KeyEvent.uChar.UnicodeChar, record.Event.KeyEvent.wVirtualKeyCode);
                    if (key != SMILE_KEY_NONE)
                        return key;
                }
            }
        }
        else if (file_type == FILE_TYPE_PIPE)
        {
            DWORD available;
            if (PeekNamedPipe(input, 0, 0, 0, &available, 0) && available != 0)
            {
                char character;
                DWORD read;
                if (ReadFile(input, &character, 1, &read, 0) && read == 1)
                    return smile_map_key((WCHAR)(unsigned char)character, 0);
            }
        }
        if ((GetAsyncKeyState('W') & 0x8000) != 0) return SMILE_KEY_W;
        if ((GetAsyncKeyState('A') & 0x8000) != 0) return SMILE_KEY_A;
        if ((GetAsyncKeyState('S') & 0x8000) != 0) return SMILE_KEY_S;
        if ((GetAsyncKeyState('D') & 0x8000) != 0) return SMILE_KEY_D;
        if ((GetAsyncKeyState('O') & 0x8000) != 0) return SMILE_KEY_O;
        if ((GetAsyncKeyState('F') & 0x8000) != 0) return SMILE_KEY_F;
        if ((GetAsyncKeyState('G') & 0x8000) != 0) return SMILE_KEY_G;
        if ((GetAsyncKeyState('R') & 0x8000) != 0) return SMILE_KEY_R;
        return SMILE_KEY_NONE;
    }
}

long long smile_key_held(long long key)
{
    int virtual_key = smile_key_virtual(key);
    return virtual_key > 0 && virtual_key < 256 && smile_held[virtual_key] != 0;
}

long long smile_pointer_x(void) { return smile_pointer.x; }
long long smile_pointer_y(void) { return smile_pointer.y; }
long long smile_pointer_delta_x(void) { return smile_pointer.delta_x; }
long long smile_pointer_delta_y(void) { return smile_pointer.delta_y; }
long long smile_pointer_wheel_delta(void) { return smile_pointer.wheel_delta; }
long long smile_pointer_wheel_remainder(void) { return smile_pointer.wheel_remainder; }
long long smile_pointer_inside(void) { return smile_pointer.inside != 0; }
long long smile_pointer_held(long long button)
{
    unsigned int mask = button >= 1 && button <= 3 ? 1U << (unsigned int)(button - 1) : 0;
    return mask != 0 && (smile_pointer.held_buttons & mask) != 0;
}
long long smile_pointer_pressed(long long button)
{
    unsigned int mask = button >= 1 && button <= 3 ? 1U << (unsigned int)(button - 1) : 0;
    return mask != 0 && (smile_pointer.pressed_buttons & mask) != 0;
}
long long smile_pointer_released(long long button)
{
    unsigned int mask = button >= 1 && button <= 3 ? 1U << (unsigned int)(button - 1) : 0;
    return mask != 0 && (smile_pointer.released_buttons & mask) != 0;
}

void smile_clear_screen(void)
{
    COORD origin;
    origin.X = 0;
    origin.Y = 0;
    SetConsoleCursorPosition(smile_output(), origin);
}

void smile_wait(long long milliseconds)
{
    ULONGLONG end;
    if (milliseconds < 0)
        milliseconds = 0;
    if (milliseconds > 0xFFFFFFFFLL)
        milliseconds = 0xFFFFFFFFLL;
    if (smile_window == 0)
    {
        Sleep((DWORD)milliseconds);
        return;
    }
    end = GetTickCount64() + (ULONGLONG)milliseconds;
    do
    {
        ULONGLONG now;
        DWORD remaining;
        smile_pump_messages();
        now = GetTickCount64();
        if (now >= end)
            break;
        remaining = (DWORD)(end - now);
        MsgWaitForMultipleObjects(0, 0, FALSE, remaining > 10 ? 10 : remaining, QS_ALLINPUT);
    }
    while (smile_closed == 0);
}

long long smile_random(long long minimum, long long maximum)
{
    static unsigned long long state;
    unsigned long long range;
    if (minimum > maximum)
        return minimum;
    if (state == 0)
        state = GetTickCount64() ^ (unsigned long long)(ULONG_PTR)&state;
    state = state * 6364136223846793005ULL + 1442695040888963407ULL;
    range = (unsigned long long)(maximum - minimum) + 1ULL;
    return minimum + (long long)(state % range);
}

long long smile_timer(void)
{
    return smile_monotonic_milliseconds();
}

static WCHAR* smile_utf8_to_wide(const char* text, long long length)
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

void smile_audio_register_music_activation_callback(SmileMusicActivationCallback callback)
{
    smile_music_activation_callback = callback;
}

long long smile_audio_is_active(void)
{
    return smile_audio_focus_accepts_sound(&smile_audio_focus);
}

static void smile_update_game_audio_active(void)
{
    int transition = smile_audio_focus_update(&smile_audio_focus);
    if (transition == 0)
        return;
    if (transition < 0)
        smile_sfx_stop_all();
    if (smile_music_activation_callback != 0)
        smile_music_activation_callback(smile_audio_focus.effective_active ? 1 : 0);
}

static int smile_integer(long long value)
{
    if (value < INT_MIN) return INT_MIN;
    if (value > INT_MAX) return INT_MAX;
    return (int)value;
}

static LRESULT CALLBACK smile_window_proc(HWND window, UINT message, WPARAM wparam, LPARAM lparam)
{
    switch (message)
    {
        case WM_ERASEBKGND:
            return 1;
        case WM_PAINT:
        {
            PAINTSTRUCT paint;
            HDC dc = BeginPaint(window, &paint);
            smile_graphics_repaint(dc);
            EndPaint(window, &paint);
            return 0;
        }
        case WM_SIZE:
            smile_audio_focus.minimized = wparam == SIZE_MINIMIZED;
            smile_update_game_audio_active();
            if (smile_dpi_change_in_progress)
                return 0;
            if (smile_responsive_window && wparam != SIZE_MINIMIZED &&
                LOWORD(lparam) > 0 && HIWORD(lparam) > 0)
            {
                UINT dpi = GetDpiForWindow(window);
                if (dpi == 0) dpi = 96;
                smile_logical_width = MulDiv(LOWORD(lparam), 96, (int)dpi);
                smile_logical_height = MulDiv(HIWORD(lparam), 96, (int)dpi);
                smile_graphics_set_logical_size(smile_logical_width, smile_logical_height);
            }
            smile_graphics_resize(LOWORD(lparam), HIWORD(lparam));
            InvalidateRect(window, 0, FALSE);
            return 0;
        case WM_ACTIVATEAPP:
            smile_audio_focus.app_active = wparam != FALSE;
            smile_update_game_audio_active();
            return 0;
        case WM_ACTIVATE:
            smile_audio_focus.window_active = LOWORD(wparam) != WA_INACTIVE;
            smile_update_game_audio_active();
            return 0;
        case WM_DPICHANGED:
        {
            RECT* suggested = (RECT*)lparam;
            RECT client;
            UINT dpi = HIWORD(wparam);
            smile_dpi_change_in_progress = 1;
            SetWindowPos(window, 0, suggested->left, suggested->top,
                suggested->right - suggested->left, suggested->bottom - suggested->top,
                SWP_NOACTIVATE | SWP_NOZORDER);
            smile_graphics_on_dpi_changed(dpi);
            if (GetClientRect(window, &client))
            {
                int width = client.right - client.left;
                int height = client.bottom - client.top;
                if (smile_responsive_window && width > 0 && height > 0)
                {
                    smile_logical_width = MulDiv(width, 96, (int)dpi);
                    smile_logical_height = MulDiv(height, 96, (int)dpi);
                    smile_graphics_set_logical_size(smile_logical_width, smile_logical_height);
                }
                smile_graphics_resize(width, height);
            }
            smile_dpi_change_in_progress = 0;
            InvalidateRect(window, 0, FALSE);
            return 0;
        }
        case WM_SYSKEYDOWN:
            if (wparam == VK_RETURN && (lparam & (1LL << 29)) != 0)
            {
                smile_toggle_fullscreen();
                return 0;
            }
            return DefWindowProcW(window, message, wparam, lparam);
        case WM_KEYDOWN:
        {
            int virtual_key = (int)wparam;
            if (virtual_key >= 0 && virtual_key < 256)
                smile_held[virtual_key] = 1;
            if ((lparam & (1LL << 30)) == 0)
                smile_queue_key(smile_map_key(0, (WORD)virtual_key));
            return 0;
        }
        case WM_SYSKEYUP:
            return DefWindowProcW(window, message, wparam, lparam);
        case WM_KEYUP:
        {
            int virtual_key = (int)wparam;
            if (virtual_key >= 0 && virtual_key < 256)
                smile_held[virtual_key] = 0;
            return 0;
        }
        case WM_MOUSEMOVE:
        {
            TRACKMOUSEEVENT tracking;
            smile_pointer_position(lparam);
            smile_zero_memory(&tracking, sizeof(tracking));
            tracking.cbSize = sizeof(tracking);
            tracking.dwFlags = TME_LEAVE;
            tracking.hwndTrack = window;
            TrackMouseEvent(&tracking);
            return 0;
        }
        case WM_MOUSELEAVE:
            smile_pointer.inside = 0;
            return 0;
        case WM_LBUTTONDOWN:
            smile_pointer_position(lparam);
            smile_pointer_press(1);
            return 0;
        case WM_RBUTTONDOWN:
            smile_pointer_position(lparam);
            smile_pointer_press(2);
            return 0;
        case WM_MBUTTONDOWN:
            smile_pointer_position(lparam);
            smile_pointer_press(3);
            return 0;
        case WM_LBUTTONUP:
            smile_pointer_position(lparam);
            smile_pointer_release(1);
            return 0;
        case WM_RBUTTONUP:
            smile_pointer_position(lparam);
            smile_pointer_release(2);
            return 0;
        case WM_MBUTTONUP:
            smile_pointer_position(lparam);
            smile_pointer_release(3);
            return 0;
        case WM_MOUSEWHEEL:
        {
            POINT point;
            point.x = GET_X_LPARAM(lparam);
            point.y = GET_Y_LPARAM(lparam);
            ScreenToClient(window, &point);
            smile_pointer_position(MAKELPARAM(point.x, point.y));
            smile_pointer_state_wheel(&smile_pointer, GET_WHEEL_DELTA_WPARAM(wparam), WHEEL_DELTA);
            return 0;
        }
        case WM_CANCELMODE:
            smile_pointer_cancel();
            return 0;
        case WM_CAPTURECHANGED:
            if (smile_pointer.held_buttons != 0)
                smile_pointer_cancel();
            return 0;
        case WM_KILLFOCUS:
            smile_zero_memory(smile_held, sizeof(smile_held));
            smile_pointer_cancel();
            return 0;
        case WM_CLOSE:
            smile_window_save_placement();
            DestroyWindow(window);
            return 0;
        case WM_DESTROY:
            smile_audio_focus.window_active = 0;
            smile_update_game_audio_active();
            smile_window = 0;
            smile_closed = 1;
            smile_zero_memory(smile_held, sizeof(smile_held));
            smile_pointer_reset();
            smile_graphics_shutdown();
            smile_graphics_diagnostics_shutdown();
            PostQuitMessage(0);
            return 0;
    }
    return DefWindowProcW(window, message, wparam, lparam);
}

void smile_graphics_configure(long long backend, long long vsync)
{
    if (smile_window != 0)
        return;
    if (backend >= SMILE_GRAPHICS_BACKEND_AUTO && backend <= SMILE_GRAPHICS_BACKEND_DIRECTX)
        smile_requested_graphics_backend = (SmileGraphicsBackendKind)backend;
    else
        smile_requested_graphics_backend = SMILE_GRAPHICS_BACKEND_AUTO;
    smile_vsync_enabled = vsync != 0;
}

void smile_window_persistence_configure(long long remember_placement)
{
    if (smile_window == 0)
        smile_remember_window_placement = remember_placement != 0;
}

void smile_window_responsive_configure(long long responsive)
{
    if (smile_window == 0)
        smile_responsive_window = responsive != 0;
}

void smile_game_open(const char* title, long long title_length, long long width, long long height)
{
    WNDCLASSEXW window_class;
    RECT rectangle;
    SmileGraphicsBackendKind requested_backend = smile_requested_graphics_backend;
    DWORD style = WS_OVERLAPPEDWINDOW;
    HINSTANCE instance = GetModuleHandleW(0);
    WCHAR* wide_title;
    char graphics_error[768];
    UINT dpi;
    int window_x = CW_USEDEFAULT;
    int window_y = CW_USEDEFAULT;
    int window_width;
    int window_height;
    int show_command = SW_SHOW;
    if (smile_window != 0)
        return;
    if (width <= 0) width = 960;
    if (height <= 0) height = 540;
    smile_logical_width = width;
    smile_logical_height = height;
    smile_closed = 0;
    smile_key_head = 0;
    smile_key_tail = 0;
    smile_zero_memory(smile_held, sizeof(smile_held));
    smile_pointer_reset();
    smile_audio_focus_initialize(&smile_audio_focus);
    smile_frame_clock_initialize(&smile_frame_clock);
    smile_graphics_diagnostics_initialize();
    smile_zero_memory(graphics_error, sizeof(graphics_error));
    SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

    smile_zero_memory(&window_class, sizeof(window_class));
    window_class.cbSize = sizeof(window_class);
    window_class.style = CS_HREDRAW | CS_VREDRAW | CS_OWNDC;
    window_class.lpfnWndProc = smile_window_proc;
    window_class.hInstance = instance;
    window_class.hCursor = LoadCursorW(0, IDC_ARROW);
    window_class.hbrBackground = (HBRUSH)GetStockObject(BLACK_BRUSH);
    window_class.lpszClassName = smile_window_class;
    RegisterClassExW(&window_class);

    wide_title = smile_utf8_to_wide(title, title_length);
    dpi = GetDpiForSystem();
    rectangle.left = 0;
    rectangle.top = 0;
    rectangle.right = MulDiv(smile_integer(width), (int)dpi, 96);
    rectangle.bottom = MulDiv(smile_integer(height), (int)dpi, 96);
    AdjustWindowRectExForDpi(&rectangle, style, FALSE, 0, dpi);
    window_width = rectangle.right - rectangle.left;
    window_height = rectangle.bottom - rectangle.top;
    if (smile_window_load_placement(&rectangle, &smile_logical_width,
        &smile_logical_height, &show_command))
    {
        window_x = rectangle.left;
        window_y = rectangle.top;
        window_width = rectangle.right - rectangle.left;
        window_height = rectangle.bottom - rectangle.top;
    }
    smile_window = CreateWindowExW(0, smile_window_class, wide_title != 0 ? wide_title : L"SMILE 2.0",
        style, window_x, window_y, window_width, window_height,
        0, 0, instance, 0);
    if (wide_title != 0)
        HeapFree(GetProcessHeap(), 0, wide_title);
    if (smile_window == 0)
    {
        smile_closed = 1;
        return;
    }
    if (smile_responsive_window && GetClientRect(smile_window, &rectangle) &&
        rectangle.right > rectangle.left && rectangle.bottom > rectangle.top)
    {
        dpi = GetDpiForWindow(smile_window);
        if (dpi == 0) dpi = 96;
        smile_logical_width = MulDiv(rectangle.right - rectangle.left, 96, (int)dpi);
        smile_logical_height = MulDiv(rectangle.bottom - rectangle.top, 96, (int)dpi);
    }
    if (!smile_graphics_initialize(smile_window, smile_logical_width, smile_logical_height,
        requested_backend, smile_vsync_enabled,
        graphics_error, (int)sizeof(graphics_error)))
    {
        if (graphics_error[0] != 0)
            MessageBoxA(smile_window, graphics_error, "SMILE 2.0 graphics initialization", MB_OK | MB_ICONERROR);
        DestroyWindow(smile_window);
        smile_closed = 1;
        return;
    }
    ShowWindow(smile_window, show_command);
    UpdateWindow(smile_window);
}

static void smile_pump_messages(void)
{
    MSG message;
    while (PeekMessageW(&message, 0, 0, 0, PM_REMOVE))
    {
        if (message.message == WM_QUIT)
        {
            smile_closed = 1;
            continue;
        }
        TranslateMessage(&message);
        DispatchMessageW(&message);
    }
    smile_sfx_reap();
}

void smile_show_screen(void)
{
    int diagnostics_ready;
    smile_frame_clock_begin_present(&smile_frame_clock);

    /* Finish the input frame before collecting messages for the next one.
       Clearing after the pump discarded mouse events received during present. */
    smile_pointer_state_begin_frame(&smile_pointer);
    smile_pump_messages();
    if (smile_window == 0)
        return;
    smile_graphics_present();
    diagnostics_ready = smile_frame_clock_end_present(&smile_frame_clock);
    if (diagnostics_ready && smile_graphics_diagnostics_enabled())
    {
        DEVMODEW mode;
        MONITORINFOEXW monitor;
        SmileGraphicsBackendDiagnostics backend_diagnostics;
        SmileGraphicsDiagnosticsSnapshot snapshot;
        smile_zero_memory(&snapshot, sizeof(snapshot));
        smile_zero_memory(&backend_diagnostics, sizeof(backend_diagnostics));
        smile_graphics_get_diagnostics(&backend_diagnostics);
        smile_zero_memory(&monitor, sizeof(monitor));
        monitor.cbSize = sizeof(monitor);
        smile_zero_memory(&mode, sizeof(mode));
        mode.dmSize = sizeof(mode);
        if (GetMonitorInfoW(MonitorFromWindow(smile_window, MONITOR_DEFAULTTONEAREST), (MONITORINFO*)&monitor))
            EnumDisplaySettingsW(monitor.szDevice, ENUM_CURRENT_SETTINGS, &mode);
        snapshot.requested_backend = backend_diagnostics.requested_backend;
        snapshot.selected_backend = backend_diagnostics.selected_backend;
        snapshot.fallback_reason = backend_diagnostics.fallback_reason;
        snapshot.logical_width = smile_logical_width;
        snapshot.logical_height = smile_logical_height;
        snapshot.physical_width = backend_diagnostics.physical_width;
        snapshot.physical_height = backend_diagnostics.physical_height;
        snapshot.viewport_x = backend_diagnostics.viewport_x;
        snapshot.viewport_y = backend_diagnostics.viewport_y;
        snapshot.viewport_width = backend_diagnostics.viewport_width;
        snapshot.viewport_height = backend_diagnostics.viewport_height;
        snapshot.scale = backend_diagnostics.scale;
        snapshot.refresh_rate = mode.dmDisplayFrequency > 1 ? (int)mode.dmDisplayFrequency : 0;
        snapshot.vsync_enabled = smile_vsync_enabled;
        snapshot.pacing_mode = backend_diagnostics.pacing_mode;
        snapshot.device_removal_reason = backend_diagnostics.device_removal_reason;
        snapshot.frame_metrics = *smile_frame_clock_metrics(&smile_frame_clock);
        smile_graphics_diagnostics_log(&snapshot);
    }
}

long long smile_game_closed(void)
{
    smile_pump_messages();
    return smile_closed;
}

long long smile_window_width(void)
{
    return smile_logical_width;
}

long long smile_window_height(void)
{
    return smile_logical_height;
}

static void smile_toggle_fullscreen(void)
{
    MONITORINFO monitor;
    DWORD fullscreen_style;
    DWORD fullscreen_ex_style;
    if (smile_window == 0)
        return;
    if (!smile_fullscreen)
    {
        smile_windowed_style = (DWORD)GetWindowLongPtrW(smile_window, GWL_STYLE);
        smile_windowed_ex_style = (DWORD)GetWindowLongPtrW(smile_window, GWL_EXSTYLE);
        smile_windowed_placement.length = sizeof(smile_windowed_placement);
        GetWindowPlacement(smile_window, &smile_windowed_placement);
        smile_zero_memory(&monitor, sizeof(monitor));
        monitor.cbSize = sizeof(monitor);
        GetMonitorInfoW(MonitorFromWindow(smile_window, MONITOR_DEFAULTTONEAREST), &monitor);
        fullscreen_style = (smile_windowed_style &
            ~(WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX)) | WS_POPUP;
        fullscreen_ex_style = smile_windowed_ex_style &
            ~(WS_EX_DLGMODALFRAME | WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE | WS_EX_STATICEDGE);
        SetWindowLongPtrW(smile_window, GWL_STYLE, (LONG_PTR)fullscreen_style);
        SetWindowLongPtrW(smile_window, GWL_EXSTYLE, (LONG_PTR)fullscreen_ex_style);
        SetWindowPos(smile_window, HWND_TOPMOST, monitor.rcMonitor.left, monitor.rcMonitor.top,
            monitor.rcMonitor.right - monitor.rcMonitor.left, monitor.rcMonitor.bottom - monitor.rcMonitor.top,
            SWP_FRAMECHANGED | SWP_NOOWNERZORDER | SWP_SHOWWINDOW);
        smile_fullscreen = 1;
    }
    else
    {
        SetWindowLongPtrW(smile_window, GWL_STYLE, (LONG_PTR)smile_windowed_style);
        SetWindowLongPtrW(smile_window, GWL_EXSTYLE, (LONG_PTR)smile_windowed_ex_style);
        SetWindowPlacement(smile_window, &smile_windowed_placement);
        SetWindowPos(smile_window, HWND_NOTOPMOST, 0, 0, 0, 0,
            SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOOWNERZORDER | SWP_SHOWWINDOW);
        smile_fullscreen = 0;
    }
    smile_graphics_on_fullscreen_changed(smile_fullscreen);
}

void smile_game_clear(long long color)
{
    smile_graphics_clear(color);
}

void smile_fill_rectangle(long long x, long long y, long long width, long long height, long long color)
{ smile_graphics_fill_rectangle(x, y, width, height, color); }

void smile_fill_rectangle_opacity(long long x, long long y, long long width, long long height, long long color, long long opacity)
{ smile_graphics_fill_rectangle_opacity(x, y, width, height, color, opacity); }

void smile_draw_rectangle(long long x, long long y, long long width, long long height, long long color)
{ smile_graphics_draw_rectangle(x, y, width, height, color); }

void smile_fill_rounded_rectangle(long long x, long long y, long long width, long long height, long long radius, long long color)
{ smile_graphics_fill_rounded_rectangle(x, y, width, height, radius, color); }

void smile_draw_rounded_rectangle(long long x, long long y, long long width, long long height, long long radius, long long color)
{ smile_graphics_draw_rounded_rectangle(x, y, width, height, radius, color); }

void smile_fill_circle(long long x, long long y, long long radius, long long color)
{ smile_graphics_fill_circle(x, y, radius, color); }

void smile_draw_circle(long long x, long long y, long long radius, long long color)
{ smile_graphics_draw_circle(x, y, radius, color); }

void smile_draw_arc(long long center_x, long long center_y, long long radius,
    long long start_angle, long long sweep_angle, long long color)
{ smile_graphics_draw_arc(center_x, center_y, radius, start_angle, sweep_angle, color); }

void smile_fill_quadrilateral(long long x1, long long y1, long long x2, long long y2,
    long long x3, long long y3, long long x4, long long y4, long long color)
{
    smile_graphics_fill_quadrilateral(x1, y1, x2, y2, x3, y3, x4, y4, color);
}

void smile_draw_quadrilateral(long long x1, long long y1, long long x2, long long y2,
    long long x3, long long y3, long long x4, long long y4, long long color)
{
    smile_graphics_draw_quadrilateral(x1, y1, x2, y2, x3, y3, x4, y4, color);
}

void smile_draw_line(long long x1, long long y1, long long x2, long long y2, long long color)
{
    smile_graphics_draw_line(x1, y1, x2, y2, color);
}

void smile_draw_text(const char* text, long long length, long long x, long long y, long long size, long long color, long long centered)
{
    smile_graphics_draw_text(text, length, x, y, size, color, centered);
}

void smile_draw_text_value(void* owned_value, long long x, long long y, long long size, long long color, long long centered)
{
    SmileText* text = (SmileText*)owned_value;
    smile_graphics_draw_text(smile_text_bytes(text), smile_text_length(text), x, y, size, color, centered);
    smile_text_release(text);
}

void smile_draw_number(long long value, long long x, long long y, long long size, long long color)
{
    smile_graphics_draw_number(value, x, y, size, color);
}

void* smile_image_retain(void* value)
{
    return smile_image_resource_retain((SmileImageResource*)value);
}

void smile_image_release(void* value)
{
    smile_image_resource_release((SmileImageResource*)value);
}

void smile_image_move_assign(void** target, void* owned_value)
{
    void* previous;
    if (target == 0)
    {
        smile_image_release(owned_value);
        return;
    }
    previous = *target;
    *target = owned_value;
    smile_image_release(previous);
}

void smile_image_clear(void** target)
{
    smile_image_move_assign(target, 0);
}

void smile_load_image_value(void** target, void* owned_path)
{
    SmileText* path = (SmileText*)owned_path;
    WCHAR full_path[2048];
    SmileImageResource* image = 0;
    if (smile_resolve_asset_path_utf8(smile_text_bytes(path), smile_text_length(path), full_path,
        (int)(sizeof(full_path) / sizeof(full_path[0]))))
        image = smile_image_resource_load(full_path);
    smile_text_release(path);
    if (image == 0)
    {
        MessageBoxA(smile_window, "Load Image could not decode the requested PNG asset.",
            "SMILE 2.0 image runtime", MB_OK | MB_ICONERROR);
        ExitProcess(2);
    }
    smile_image_move_assign(target, image);
}

long long smile_renderer3d_text_command(long long command, void* owned_text,
    long long a, long long b, long long c, long long d,
    long long e, long long f, long long g, long long h)
{
    SmileText* text = (SmileText*)owned_text;
    WCHAR full_path[2048];
    long long result = 0;
    (void)a; (void)b; (void)c; (void)d; (void)e; (void)f; (void)g; (void)h;
    if ((command == SMILE_3D_TEXT_LOAD_MODEL ||
        command == SMILE_3D_TEXT_LOAD_MODEL_GEOMETRY) && text != 0 &&
        smile_resolve_asset_path_utf8(smile_text_bytes(text), smile_text_length(text), full_path,
            (int)(sizeof(full_path) / sizeof(full_path[0]))))
        result = command == SMILE_3D_TEXT_LOAD_MODEL
            ? smile_renderer3d_load_model_path(full_path)
            : smile_renderer3d_load_model_geometry_path(full_path);
    else if (command == SMILE_3D_TEXT_PREPARE_MODEL_PBR)
        result = smile_renderer3d_prepare_model_pbr(a, b, c, d);
    else if (command >= SMILE_3D_TEXT_MODEL_CLIP_INDEX &&
        command <= SMILE_3D_TEXT_TAKE_MODEL_ANIMATOR_EVENT && text != 0)
        result = smile_renderer3d_model_text_operation(command, smile_text_bytes(text),
            smile_text_length(text), a, b, c);
    smile_text_release(text);
    return result;
}

long long smile_image_width_value(void* owned_image)
{
    long long value = smile_image_resource_width((SmileImageResource*)owned_image);
    smile_image_release(owned_image);
    return value;
}

long long smile_image_height_value(void* owned_image)
{
    long long value = smile_image_resource_height((SmileImageResource*)owned_image);
    smile_image_release(owned_image);
    return value;
}

long long smile_image_loaded_value(void* owned_image)
{
    long long value = owned_image != 0;
    smile_image_release(owned_image);
    return value;
}

void smile_draw_image_value(void* owned_image, long long source_x, long long source_y,
    long long source_width, long long source_height, long long destination_x, long long destination_y,
    long long destination_width, long long destination_height, long long opacity, long long filter,
    long long flip, long long anchor_x, long long anchor_y)
{
    SmileImageResource* image = (SmileImageResource*)owned_image;
    long long image_width = smile_image_resource_width(image);
    long long image_height = smile_image_resource_height(image);
    if (source_width < 0) source_width = image_width;
    if (source_height < 0) source_height = image_height;
    if (destination_width < 0) destination_width = source_width;
    if (destination_height < 0) destination_height = source_height;
    if (image == 0 || source_x < 0 || source_y < 0 || source_width <= 0 || source_height <= 0 ||
        source_x > image_width - source_width || source_y > image_height - source_height ||
        destination_width <= 0 || destination_height <= 0 || opacity < 0 || opacity > 100 ||
        filter < 0 || filter > 1 || flip < 0 || flip > 3)
    {
        smile_image_release(image);
        MessageBoxA(smile_window, "Draw Image received an invalid handle, rectangle, opacity, filter, or flip.",
            "SMILE 2.0 image runtime", MB_OK | MB_ICONERROR);
        ExitProcess(2);
    }
    smile_graphics_draw_image(image, source_x, source_y, source_width, source_height,
        destination_x - anchor_x, destination_y - anchor_y, destination_width, destination_height,
        opacity, filter, flip);
    smile_image_release(image);
}

void smile_clip_push(long long x, long long y, long long width, long long height)
{
    if (width <= 0 || height <= 0)
    {
        MessageBoxA(smile_window, "Clip Rectangle width and height must be positive.",
            "SMILE 2.0 graphics runtime", MB_OK | MB_ICONERROR);
        ExitProcess(2);
    }
    smile_graphics_push_clip(x, y, width, height);
}

void smile_clip_pop(void)
{
    smile_graphics_pop_clip();
}

long long smile_text_width_value(void* owned_text, long long size)
{
    SmileText* text = (SmileText*)owned_text;
    long long result = size <= 0 ? 0 : smile_graphics_text_width(smile_text_bytes(text), smile_text_length(text), size);
    smile_text_release(text);
    return result;
}

long long smile_text_height_value(void* owned_text, long long size)
{
    SmileText* text = (SmileText*)owned_text;
    long long result = size <= 0 ? 0 : smile_graphics_text_height(smile_text_bytes(text), smile_text_length(text), size);
    smile_text_release(text);
    return result;
}

static int smile_asset_declared(const char* path, int length)
{
    long long start = 0;
    if (smile_asset_manifest_length == 0) return 1;
    while (start < smile_asset_manifest_length)
    {
        long long end = start;
        while (end < smile_asset_manifest_length && smile_asset_manifest[end] != '\n') end++;
        if (end - start == length && smile_bytes_equal(smile_asset_manifest + start, path, (SIZE_T)length)) return 1;
        start = end + 1;
    }
    return 0;
}

static int smile_canonical_asset_path(const char* path, long long length, char* output, int capacity)
{
    int segment_starts[512];
    int segment_count = 0;
    int written = 0;
    long long index = 0;
    if (path == 0 || length <= 0 || output == 0 || capacity <= 1) return 0;
    if (path[0] == '/' || path[0] == '\\') return 0;
    while (index < length)
    {
        long long start;
        long long segment_length;
        while (index < length && (path[index] == '/' || path[index] == '\\')) index++;
        if (index >= length) break;
        start = index;
        while (index < length && path[index] != '/' && path[index] != '\\')
        {
            unsigned char character = (unsigned char)path[index];
            if (character == 0 || character == ':') return 0;
            index++;
        }
        segment_length = index - start;
        if (segment_length == 1 && path[start] == '.') continue;
        if (segment_length == 2 && path[start] == '.' && path[start + 1] == '.')
        {
            if (segment_count == 0) return 0;
            written = segment_starts[--segment_count];
            if (written > 0 && output[written - 1] == '/') written--;
            continue;
        }
        if (segment_count >= (int)(sizeof(segment_starts) / sizeof(segment_starts[0])) ||
            written + (written == 0 ? 0 : 1) + segment_length >= capacity) return 0;
        if (written != 0) output[written++] = '/';
        segment_starts[segment_count++] = written;
        smile_copy_bytes(output + written, path + start, (SIZE_T)segment_length);
        written += (int)segment_length;
    }
    if (written == 0) return 0;
    output[written] = 0;
    return smile_asset_declared(output, written);
}

static void smile_append(WCHAR* destination, int capacity, const WCHAR* source)
{
    int index = lstrlenW(destination);
    while (source != 0 && *source != 0 && index + 1 < capacity)
        destination[index++] = *source++;
    destination[index] = 0;
}

int smile_resolve_asset_path_utf8(const char* path, long long length, WCHAR* resolved_path, int capacity)
{
    char canonical[4096];
    WCHAR* wide;
    WCHAR* slash;
    int base_length;
    int path_length;
    if (!smile_canonical_asset_path(path, length, canonical, (int)sizeof(canonical))) return 0;
    wide = smile_utf8_to_wide(canonical, (long long)lstrlenA(canonical));
    if (resolved_path == 0 || capacity <= 0 || wide == 0)
        return 0;
    for (slash = wide; *slash != 0; ++slash)
        if (*slash == L'/') *slash = L'\\';
    resolved_path[0] = 0;
    {
        DWORD copied = GetModuleFileNameW(0, resolved_path, (DWORD)capacity);
        if (copied == 0 || copied >= (DWORD)capacity)
        {
            HeapFree(GetProcessHeap(), 0, wide);
            resolved_path[0] = 0;
            return 0;
        }
        slash = resolved_path + lstrlenW(resolved_path);
        while (slash > resolved_path && slash[-1] != L'\\' && slash[-1] != L'/')
            --slash;
        *slash = 0;
        base_length = lstrlenW(resolved_path);
        path_length = lstrlenW(wide);
        if (base_length + path_length >= capacity)
        {
            HeapFree(GetProcessHeap(), 0, wide);
            resolved_path[0] = 0;
            return 0;
        }
        smile_append(resolved_path, capacity, wide);
    }
    HeapFree(GetProcessHeap(), 0, wide);
    return 1;
}

long long smile_load_text_file(void* owned_path, long long* destination, long long capacity)
{
    SmileText* path = (SmileText*)owned_path;
    WCHAR full_path[2048];
    HANDLE file;
    unsigned char buffer[4096];
    DWORD read = 0;
    long long copied = 0;
    int failed = 0;

    if (destination == 0 || capacity <= 0)
    {
        smile_text_release(path);
        return 0;
    }
    smile_zero_memory(destination, (SIZE_T)capacity * sizeof(long long));
    if (path == 0 || smile_text_length(path) <= 0 ||
        !smile_resolve_asset_path_utf8(smile_text_bytes(path), smile_text_length(path), full_path,
            (int)(sizeof(full_path) / sizeof(full_path[0]))))
    {
        smile_text_release(path);
        return 0;
    }
    smile_text_release(path);

    file = CreateFileW(full_path, GENERIC_READ, FILE_SHARE_READ, 0, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
    if (file == INVALID_HANDLE_VALUE)
        return 0;

    if (!ReadFile(file, buffer, 3, &read, 0))
        failed = 1;
    else
    {
        DWORD start = read == 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF ? 3 : 0;
        DWORD byte_index;
        for (byte_index = start; byte_index < read && copied < capacity; ++byte_index)
            destination[copied++] = (long long)buffer[byte_index];
    }

    while (!failed && copied < capacity)
    {
        if (!ReadFile(file, buffer, (DWORD)sizeof(buffer), &read, 0))
        {
            failed = 1;
            break;
        }
        if (read == 0)
            break;
        {
            DWORD byte_index;
            for (byte_index = 0; byte_index < read && copied < capacity; ++byte_index)
                destination[copied++] = (long long)buffer[byte_index];
        }
    }

    CloseHandle(file);
    if (!failed)
        return copied;
    smile_zero_memory(destination, (SIZE_T)capacity * sizeof(long long));
    return 0;
}

void smile_play_sound(const char* path, long long length)
{
    smile_play_sound_channel(path, length, 0);
}

void smile_play_sound_channel(const char* path, long long length, long long channel)
{
    WCHAR full_path[2048];
    if (channel < 0 || channel >= 16)
    {
        MessageBoxA(smile_window, "Sound channel must be from 0 through 15.",
            "SMILE 2.0 audio runtime", MB_OK | MB_ICONERROR);
        ExitProcess(2);
    }
    if (!smile_audio_focus_accepts_sound(&smile_audio_focus))
        return;
    if (!smile_resolve_asset_path_utf8(path, length, full_path,
        (int)(sizeof(full_path) / sizeof(full_path[0]))))
        return;
    smile_sfx_play(full_path, (int)channel);
}

void smile_stop_sound(void)
{
    smile_sfx_stop_all();
}

void smile_stop_sound_channel(long long channel)
{
    if (channel < 0 || channel >= 16)
    {
        MessageBoxA(smile_window, "Sound channel must be from 0 through 15.",
            "SMILE 2.0 audio runtime", MB_OK | MB_ICONERROR);
        ExitProcess(2);
    }
    smile_sfx_stop((int)channel);
}

static void smile_sanitize(WCHAR* destination, int capacity, const WCHAR* source)
{
    int index = 0;
    while (source != 0 && *source != 0 && index + 1 < capacity)
    {
        WCHAR character = *source++;
        if ((character >= L'a' && character <= L'z') || (character >= L'A' && character <= L'Z') ||
            (character >= L'0' && character <= L'9') || character == L'-' || character == L'_')
            destination[index++] = character;
        else
            destination[index++] = L'_';
    }
    if (index == 0 && capacity > 1)
        destination[index++] = L'_';
    destination[index] = 0;
}

static int smile_storage_path(const char* key, long long key_length, WCHAR* path, int capacity)
{
    PWSTR local_app_data = 0;
    WCHAR executable[1024];
    WCHAR game_name[256];
    WCHAR key_name[256];
    WCHAR* file_name;
    WCHAR* extension;
    WCHAR* wide_key;
    HRESULT result = SHGetKnownFolderPath(&FOLDERID_LocalAppData, KF_FLAG_CREATE, 0, &local_app_data);
    if (FAILED(result) || local_app_data == 0)
        return 0;
    path[0] = 0;
    smile_append(path, capacity, local_app_data);
    CoTaskMemFree(local_app_data);
    smile_append(path, capacity, L"\\SMILE 2.0\\Games\\");
    GetModuleFileNameW(0, executable, (DWORD)(sizeof(executable) / sizeof(executable[0])));
    file_name = executable + lstrlenW(executable);
    while (file_name > executable && file_name[-1] != L'\\' && file_name[-1] != L'/')
        --file_name;
    extension = file_name + lstrlenW(file_name);
    while (extension > file_name && extension[-1] != L'.')
        --extension;
    if (extension > file_name)
        extension[-1] = 0;
    smile_sanitize(game_name, (int)(sizeof(game_name) / sizeof(game_name[0])), file_name);
    smile_append(path, capacity, game_name);
    SHCreateDirectoryExW(0, path, 0);
    smile_append(path, capacity, L"\\");
    wide_key = smile_utf8_to_wide(key, key_length);
    if (wide_key == 0)
        return 0;
    smile_sanitize(key_name, (int)(sizeof(key_name) / sizeof(key_name[0])), wide_key);
    HeapFree(GetProcessHeap(), 0, wide_key);
    smile_append(path, capacity, key_name);
    smile_append(path, capacity, L".txt");
    return 1;
}

long long smile_load_value(const char* key, long long key_length, long long default_value)
{
    WCHAR path[2048];
    HANDLE file;
    char buffer[64];
    DWORD read = 0;
    int index = 0;
    int negative = 0;
    unsigned long long magnitude = 0;
    if (!smile_storage_path(key, key_length, path, (int)(sizeof(path) / sizeof(path[0]))))
        return default_value;
    file = CreateFileW(path, GENERIC_READ, FILE_SHARE_READ, 0, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
    if (file == INVALID_HANDLE_VALUE)
        return default_value;
    if (!ReadFile(file, buffer, sizeof(buffer) - 1, &read, 0))
    {
        CloseHandle(file);
        return default_value;
    }
    CloseHandle(file);
    buffer[read] = 0;
    while (index < (int)read && (buffer[index] == ' ' || buffer[index] == '\t' || buffer[index] == '\r' || buffer[index] == '\n')) ++index;
    if (index < (int)read && (buffer[index] == '-' || buffer[index] == '+')) negative = buffer[index++] == '-';
    if (index >= (int)read || buffer[index] < '0' || buffer[index] > '9') return default_value;
    while (index < (int)read && buffer[index] >= '0' && buffer[index] <= '9')
    {
        unsigned int digit = (unsigned int)(buffer[index++] - '0');
        if (magnitude > ((unsigned long long)LLONG_MAX + (negative ? 1ULL : 0ULL) - digit) / 10ULL)
            return default_value;
        magnitude = magnitude * 10ULL + digit;
    }
    while (index < (int)read && (buffer[index] == ' ' || buffer[index] == '\t' || buffer[index] == '\r' || buffer[index] == '\n')) ++index;
    if (index != (int)read) return default_value;
    if (negative)
        return magnitude == (unsigned long long)LLONG_MAX + 1ULL ? LLONG_MIN : -(long long)magnitude;
    return (long long)magnitude;
}

void smile_save_value(const char* key, long long key_length, long long value)
{
    WCHAR path[2048];
    HANDLE file;
    char buffer[32];
    int length;
    DWORD written;
    if (!smile_storage_path(key, key_length, path, (int)(sizeof(path) / sizeof(path[0]))))
        return;
    file = CreateFileW(path, GENERIC_WRITE, 0, 0, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, 0);
    if (file == INVALID_HANDLE_VALUE)
        return;
    length = smile_format_number(value, buffer, (int)sizeof(buffer));
    WriteFile(file, buffer, (DWORD)length, &written, 0);
    CloseHandle(file);
}

static int smile_storage_data_path(const char* key, long long key_length, WCHAR* path, int capacity)
{
    static const char fallback_identity[] = "Program";
    static const WCHAR hex[] = L"0123456789abcdef";
    PWSTR local_app_data = 0;
    unsigned char app_digest[32];
    unsigned char key_digest[32];
    WCHAR hash_text[65];
    int index;
    const unsigned char* identity = (const unsigned char*)(smile_app_identity != 0 ? smile_app_identity : fallback_identity);
    SIZE_T identity_length = smile_app_identity != 0 ? (SIZE_T)smile_app_identity_length : sizeof(fallback_identity) - 1;
    if (key == 0 || key_length < 0 || key_length > 1024 * 1024 ||
        FAILED(SHGetKnownFolderPath(&FOLDERID_LocalAppData, KF_FLAG_CREATE, 0, &local_app_data)) || local_app_data == 0)
        return 0;
    smile_sha_bytes(identity, identity_length, app_digest);
    smile_sha_bytes((const unsigned char*)key, (SIZE_T)key_length, key_digest);
    path[0] = 0;
    smile_append(path, capacity, local_app_data);
    CoTaskMemFree(local_app_data);
    smile_append(path, capacity, L"\\SMILE 2.0\\Games");
    SHCreateDirectoryExW(0, path, 0);
    smile_append(path, capacity, L"\\");
    for (index = 0; index < 32; ++index)
    {
        hash_text[index * 2] = hex[app_digest[index] >> 4];
        hash_text[index * 2 + 1] = hex[app_digest[index] & 15];
    }
    hash_text[64] = 0;
    smile_append(path, capacity, hash_text);
    SHCreateDirectoryExW(0, path, 0);
    smile_append(path, capacity, L"\\Data");
    SHCreateDirectoryExW(0, path, 0);
    smile_append(path, capacity, L"\\");
    for (index = 0; index < 32; ++index)
    {
        hash_text[index * 2] = hex[key_digest[index] >> 4];
        hash_text[index * 2 + 1] = hex[key_digest[index] & 15];
    }
    hash_text[64] = 0;
    smile_append(path, capacity, hash_text);
    smile_append(path, capacity, L".bin");
    return 1;
}

static uint32_t smile_data_u32(const unsigned char* value)
{
    return (uint32_t)value[0] | ((uint32_t)value[1] << 8) |
        ((uint32_t)value[2] << 16) | ((uint32_t)value[3] << 24);
}

static void smile_data_put_u32(unsigned char* value, uint32_t number)
{
    value[0] = (unsigned char)number;
    value[1] = (unsigned char)(number >> 8);
    value[2] = (unsigned char)(number >> 16);
    value[3] = (unsigned char)(number >> 24);
}

static void smile_data_error(const char* message)
{
    if (smile_window != 0)
        MessageBoxA(smile_window, message, "SMILE 2.0 persistent data", MB_OK | MB_ICONERROR);
    else
    {
        DWORD written;
        HANDLE error = GetStdHandle(STD_ERROR_HANDLE);
        WriteFile(error, message, (DWORD)lstrlenA(message), &written, 0);
        WriteFile(error, "\r\n", 2, &written, 0);
    }
}

long long smile_load_data_value(void* owned_key, long long* destination, long long capacity)
{
    SmileText* key = (SmileText*)owned_key;
    WCHAR path[2048];
    HANDLE file = INVALID_HANDLE_VALUE;
    LARGE_INTEGER size;
    unsigned char* envelope = 0;
    unsigned char digest[32];
    DWORD read = 0;
    uint32_t payload_length;
    uint32_t index;
    if (destination == 0 || capacity < 0 || capacity > 1024 * 1024)
        goto fail;
    smile_zero_memory(destination, (SIZE_T)capacity * sizeof(long long));
    if (!smile_storage_data_path(smile_text_bytes(key), smile_text_length(key), path,
        (int)(sizeof(path) / sizeof(path[0])))) goto fail;
    file = CreateFileW(path, GENERIC_READ, FILE_SHARE_READ, 0, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
    if (file == INVALID_HANDLE_VALUE)
    {
        smile_text_release(key);
        return 0;
    }
    if (!GetFileSizeEx(file, &size) || size.QuadPart < 44 || size.QuadPart > 1024 * 1024 + 44)
        goto fail;
    envelope = (unsigned char*)HeapAlloc(GetProcessHeap(), 0, (SIZE_T)size.QuadPart);
    if (envelope == 0 || !ReadFile(file, envelope, (DWORD)size.QuadPart, &read, 0) || read != (DWORD)size.QuadPart)
        goto fail;
    if (envelope[0] != 'S' || envelope[1] != 'M' || envelope[2] != 'D' || envelope[3] != '4' ||
        smile_data_u32(envelope + 4) != 1) goto fail;
    payload_length = smile_data_u32(envelope + 8);
    if (payload_length > 1024 * 1024 || payload_length > capacity || size.QuadPart != (long long)payload_length + 44)
        goto fail;
    smile_sha_bytes(envelope + 44, payload_length, digest);
    if (!smile_bytes_equal((const char*)digest, (const char*)envelope + 12, 32)) goto fail;
    for (index = 0; index < payload_length; ++index) destination[index] = envelope[44 + index];
    CloseHandle(file);
    HeapFree(GetProcessHeap(), 0, envelope);
    smile_text_release(key);
    return payload_length;

fail:
    if (file != INVALID_HANDLE_VALUE) CloseHandle(file);
    if (envelope != 0) HeapFree(GetProcessHeap(), 0, envelope);
    if (destination != 0 && capacity > 0 && capacity <= 1024 * 1024)
        smile_zero_memory(destination, (SIZE_T)capacity * sizeof(long long));
    smile_text_release(key);
    smile_data_error("Load Data encountered an invalid destination, corrupt block, or oversized block.");
    ExitProcess(2);
}

void smile_save_data_value(const long long* source, long long capacity, long long count, void* owned_key)
{
    SmileText* key = (SmileText*)owned_key;
    WCHAR path[2048];
    WCHAR temporary[2100] = { 0 };
    HANDLE file = INVALID_HANDLE_VALUE;
    unsigned char header[44];
    unsigned char* payload = 0;
    long long copied = 0;
    DWORD written;
    int valid = source != 0 && capacity >= 0 && capacity <= 1024 * 1024 && count >= 0 &&
        count <= capacity && count <= 1024 * 1024;
    payload = (unsigned char*)HeapAlloc(GetProcessHeap(), 0, (SIZE_T)(count > 0 ? count : 1));
    if (payload == 0) valid = 0;
    while (valid && copied < count)
    {
        long long value = source[copied++];
        if (value < 0 || value > 255) valid = 0;
        else payload[copied - 1] = (unsigned char)value;
    }
    if (!valid || !smile_storage_data_path(smile_text_bytes(key), smile_text_length(key), path,
        (int)(sizeof(path) / sizeof(path[0])))) goto fail;
    wsprintfW(temporary, L"%s.tmp.%lu.%I64u", path, GetCurrentProcessId(), GetTickCount64());
    file = CreateFileW(temporary, GENERIC_WRITE, 0, 0, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, 0);
    if (file == INVALID_HANDLE_VALUE) goto fail;
    header[0] = 'S'; header[1] = 'M'; header[2] = 'D'; header[3] = '4';
    smile_data_put_u32(header + 4, 1);
    smile_data_put_u32(header + 8, (uint32_t)count);
    smile_sha_bytes(payload, (SIZE_T)count, header + 12);
    if (!WriteFile(file, header, (DWORD)sizeof(header), &written, 0) || written != (DWORD)sizeof(header) ||
        (count > 0 && (!WriteFile(file, payload, (DWORD)count, &written, 0) || written != (DWORD)count)) ||
        !FlushFileBuffers(file)) goto fail;
    CloseHandle(file);
    file = INVALID_HANDLE_VALUE;
    if (!MoveFileExW(temporary, path, MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH)) goto fail;
    HeapFree(GetProcessHeap(), 0, payload);
    smile_text_release(key);
    return;

fail:
    if (file != INVALID_HANDLE_VALUE) CloseHandle(file);
    if (temporary[0] != 0) DeleteFileW(temporary);
    if (payload != 0) HeapFree(GetProcessHeap(), 0, payload);
    smile_text_release(key);
    smile_data_error("Save Data received invalid bytes/count or could not atomically store the block.");
    ExitProcess(2);
}

void smile_media_shutdown(void)
{
    smile_window_save_placement();
    smile_sfx_shutdown();
    smile_image_resource_shutdown();
    if (smile_app_identity != 0) HeapFree(GetProcessHeap(), 0, smile_app_identity);
    if (smile_asset_manifest != 0) HeapFree(GetProcessHeap(), 0, smile_asset_manifest);
    smile_app_identity = 0;
    smile_asset_manifest = 0;
    smile_app_identity_length = 0;
    smile_asset_manifest_length = 0;
}
