#include <stdio.h>
#include <string.h>
#include "graphics_common.h"
#include "graphics_directx.h"
#include "graphics_gdi.h"
#include "audio_focus_state.h"
#include "pointer_state.h"

typedef struct MockState
{
    const char* name;
    int initialize_count;
    int shutdown_count;
    int begin_count;
    int fill_quad_count;
    int draw_quad_count;
    int should_fail;
    const char* failure;
    int draw_arc_count;
    long long arc_values[6];
    int draw_image_count;
    long long image_values[11];
    int push_clip_count;
    int pop_clip_count;
    int text_measure_count;
} MockState;

static MockState directx_state = { "DirectX", 0, 0, 0, 0, 0, 0, "DXGI swap-chain creation failed with 0x887A0004 (DXGI_ERROR_UNSUPPORTED)" };
static MockState gdi_state = { "GDI", 0, 0, 0, 0, 0, 0, "GDI physical back-buffer creation failed." };
static int failures;

static void copy_text(char* destination, int capacity, const char* source)
{
    int index = 0;
    if (destination == 0 || capacity <= 0)
        return;
    while (source[index] != 0 && index + 1 < capacity)
    {
        destination[index] = source[index];
        index++;
    }
    destination[index] = 0;
}

static int mock_initialize(SmileGraphicsBackend* backend, void* window,
    long long logical_width, long long logical_height, int vsync_enabled,
    char* error, int error_capacity)
{
    MockState* state = (MockState*)backend->state;
    (void)window; (void)logical_width; (void)logical_height; (void)vsync_enabled;
    state->initialize_count++;
    if (!state->should_fail)
        return 1;
    copy_text(error, error_capacity, state->failure);
    return 0;
}

static void mock_shutdown(SmileGraphicsBackend* backend)
{ ((MockState*)backend->state)->shutdown_count++; }
static const char* mock_name(const SmileGraphicsBackend* backend)
{ return ((const MockState*)backend->state)->name; }
static void mock_diagnostics(const SmileGraphicsBackend* backend,
    SmileGraphicsBackendDiagnostics* diagnostics)
{ (void)backend; diagnostics->pacing_mode = "Mock"; diagnostics->device_removal_reason = "None"; }
static void mock_resize(SmileGraphicsBackend* backend, int width, int height)
{ (void)backend; (void)width; (void)height; }
static void mock_begin(SmileGraphicsBackend* backend)
{ ((MockState*)backend->state)->begin_count++; }
static void mock_clear(SmileGraphicsBackend* backend, long long color) { (void)backend; (void)color; }
static void mock_rectangle(SmileGraphicsBackend* backend, long long x, long long y,
    long long width, long long height, long long color)
{ (void)backend; (void)x; (void)y; (void)width; (void)height; (void)color; }
static void mock_rectangle_opacity(SmileGraphicsBackend* backend, long long x, long long y,
    long long width, long long height, long long color, long long opacity)
{ (void)backend; (void)x; (void)y; (void)width; (void)height; (void)color; (void)opacity; }
static void mock_rounded(SmileGraphicsBackend* backend, long long x, long long y,
    long long width, long long height, long long radius, long long color)
{ (void)backend; (void)x; (void)y; (void)width; (void)height; (void)radius; (void)color; }
static void mock_circle(SmileGraphicsBackend* backend, long long x, long long y,
    long long radius, long long color)
{ (void)backend; (void)x; (void)y; (void)radius; (void)color; }
static void mock_arc(SmileGraphicsBackend* backend, long long center_x, long long center_y,
    long long radius, long long start_angle, long long sweep_angle, long long color)
{
    MockState* state = (MockState*)backend->state;
    state->draw_arc_count++;
    state->arc_values[0] = center_x;
    state->arc_values[1] = center_y;
    state->arc_values[2] = radius;
    state->arc_values[3] = start_angle;
    state->arc_values[4] = sweep_angle;
    state->arc_values[5] = color;
}
static void mock_fill_quadrilateral(SmileGraphicsBackend* backend,
    long long x1, long long y1, long long x2, long long y2,
    long long x3, long long y3, long long x4, long long y4, long long color)
{
    ((MockState*)backend->state)->fill_quad_count++;
    (void)x1; (void)y1; (void)x2; (void)y2; (void)x3; (void)y3;
    (void)x4; (void)y4; (void)color;
}
static void mock_draw_quadrilateral(SmileGraphicsBackend* backend,
    long long x1, long long y1, long long x2, long long y2,
    long long x3, long long y3, long long x4, long long y4, long long color)
{
    ((MockState*)backend->state)->draw_quad_count++;
    (void)x1; (void)y1; (void)x2; (void)y2; (void)x3; (void)y3;
    (void)x4; (void)y4; (void)color;
}
static void mock_line(SmileGraphicsBackend* backend, long long x1, long long y1,
    long long x2, long long y2, long long color)
{ (void)backend; (void)x1; (void)y1; (void)x2; (void)y2; (void)color; }
static void mock_text(SmileGraphicsBackend* backend, const char* text, long long length,
    long long x, long long y, long long size, long long color, long long centered)
{ (void)backend; (void)text; (void)length; (void)x; (void)y; (void)size; (void)color; (void)centered; }
static void mock_number(SmileGraphicsBackend* backend, long long value, long long x,
    long long y, long long size, long long color)
{ (void)backend; (void)value; (void)x; (void)y; (void)size; (void)color; }
static void mock_image(SmileGraphicsBackend* backend, void* image,
    long long source_x, long long source_y, long long source_width, long long source_height,
    long long destination_x, long long destination_y, long long destination_width, long long destination_height,
    long long opacity, long long filter, long long flip)
{
    MockState* state = (MockState*)backend->state;
    state->draw_image_count++;
    state->image_values[0] = source_x; state->image_values[1] = source_y;
    state->image_values[2] = source_width; state->image_values[3] = source_height;
    state->image_values[4] = destination_x; state->image_values[5] = destination_y;
    state->image_values[6] = destination_width; state->image_values[7] = destination_height;
    state->image_values[8] = opacity; state->image_values[9] = filter; state->image_values[10] = flip;
    (void)image;
}
static void mock_push_clip(SmileGraphicsBackend* backend, long long x, long long y,
    long long width, long long height)
{
    ((MockState*)backend->state)->push_clip_count++;
    (void)x; (void)y; (void)width; (void)height;
}
static void mock_pop_clip(SmileGraphicsBackend* backend)
{ ((MockState*)backend->state)->pop_clip_count++; }
static long long mock_text_width(SmileGraphicsBackend* backend, const char* text, long long length, long long size)
{
    ((MockState*)backend->state)->text_measure_count++;
    (void)text;
    return length * size;
}
static long long mock_text_height(SmileGraphicsBackend* backend, const char* text, long long length, long long size)
{
    ((MockState*)backend->state)->text_measure_count++;
    (void)text; (void)length;
    return size + 2;
}
static int mock_present(SmileGraphicsBackend* backend) { (void)backend; return 1; }
static void mock_context(SmileGraphicsBackend* backend, void* context)
{ (void)backend; (void)context; }
static void mock_flag(SmileGraphicsBackend* backend, int value)
{ (void)backend; (void)value; }
static void mock_logical_size(SmileGraphicsBackend* backend, long long width, long long height)
{ (void)backend; (void)width; (void)height; }
static void mock_dpi(SmileGraphicsBackend* backend, unsigned int value)
{ (void)backend; (void)value; }

static const SmileGraphicsBackendVTable mock_operations =
{
    mock_initialize, mock_resize, mock_logical_size, mock_begin, mock_clear,
    mock_rectangle, mock_rectangle_opacity, mock_rectangle, mock_rounded, mock_rounded,
    mock_circle, mock_circle, mock_arc, mock_fill_quadrilateral, mock_draw_quadrilateral,
    mock_line, mock_text, mock_number, mock_image, mock_push_clip, mock_pop_clip,
    mock_text_width, mock_text_height,
    mock_present, mock_context, mock_flag, mock_dpi, mock_shutdown,
    mock_name, mock_diagnostics
};

void smile_graphics_directx_create(SmileGraphicsBackend* backend)
{ backend->operations = &mock_operations; backend->state = &directx_state; }
void smile_graphics_gdi_create(SmileGraphicsBackend* backend)
{ backend->operations = &mock_operations; backend->state = &gdi_state; }

static void reset_mocks(void)
{
    smile_graphics_shutdown();
    directx_state.initialize_count = directx_state.shutdown_count = directx_state.should_fail = 0;
    gdi_state.initialize_count = gdi_state.shutdown_count = gdi_state.should_fail = 0;
    directx_state.begin_count = gdi_state.begin_count = 0;
    directx_state.fill_quad_count = directx_state.draw_quad_count = 0;
    gdi_state.fill_quad_count = gdi_state.draw_quad_count = 0;
    directx_state.draw_arc_count = gdi_state.draw_arc_count = 0;
    directx_state.draw_image_count = gdi_state.draw_image_count = 0;
    directx_state.push_clip_count = gdi_state.push_clip_count = 0;
    directx_state.pop_clip_count = gdi_state.pop_clip_count = 0;
    directx_state.text_measure_count = gdi_state.text_measure_count = 0;
    memset(directx_state.arc_values, 0, sizeof(directx_state.arc_values));
    memset(gdi_state.arc_values, 0, sizeof(gdi_state.arc_values));
    memset(directx_state.image_values, 0, sizeof(directx_state.image_values));
    memset(gdi_state.image_values, 0, sizeof(gdi_state.image_values));
}

static void check(int condition, const char* message)
{
    if (!condition)
    {
        failures++;
        fprintf(stderr, "FAIL: %s\n", message);
    }
}

int main(void)
{
    char error[768];
    SmileGraphicsBackendDiagnostics diagnostics;
    SmileAudioFocusState audio_focus;
    SmilePointerState pointer_state;

    reset_mocks();
    error[0] = 0;
    check(smile_graphics_initialize(0, 960, 540, SMILE_GRAPHICS_BACKEND_AUTO, 1,
        error, (int)sizeof(error)), "Auto succeeds when DirectX succeeds");
    smile_graphics_get_diagnostics(&diagnostics);
    check(directx_state.initialize_count == 1 && gdi_state.initialize_count == 0,
        "Auto tries DirectX first");
    check(strcmp(diagnostics.requested_backend, "Auto") == 0 &&
        strcmp(diagnostics.selected_backend, "DirectX") == 0,
        "Auto diagnostics report DirectX selection");
    check(strcmp(diagnostics.fallback_reason, "None") == 0,
        "Successful Auto selection has no fallback reason");

    reset_mocks();
    directx_state.should_fail = 1;
    error[0] = 0;
    check(smile_graphics_initialize(0, 960, 540, SMILE_GRAPHICS_BACKEND_AUTO, 1,
        error, (int)sizeof(error)), "Auto falls back when DirectX fails");
    smile_graphics_get_diagnostics(&diagnostics);
    check(directx_state.initialize_count == 1 && directx_state.shutdown_count == 1 &&
        gdi_state.initialize_count == 1, "Auto releases DirectX and initializes GDI");
    check(strcmp(diagnostics.selected_backend, "GDI") == 0,
        "Auto diagnostics report GDI fallback selection");
    check(strstr(diagnostics.fallback_reason, "DXGI swap-chain creation") != 0,
        "Auto diagnostics retain the DirectX failure stage");

    reset_mocks();
    error[0] = 0;
    check(smile_graphics_initialize(0, 960, 540, SMILE_GRAPHICS_BACKEND_GDI, 1,
        error, (int)sizeof(error)), "Explicit GDI initializes");
    check(directx_state.initialize_count == 0 && gdi_state.initialize_count == 1,
        "Explicit GDI never initializes DirectX");

    reset_mocks();
    directx_state.should_fail = 1;
    error[0] = 0;
    check(!smile_graphics_initialize(0, 960, 540, SMILE_GRAPHICS_BACKEND_DIRECTX, 1,
        error, (int)sizeof(error)), "Explicit DirectX failure does not fall back");
    check(gdi_state.initialize_count == 0, "Explicit DirectX failure never initializes GDI");
    check(strstr(error, "SMILE could not start the DirectX graphics backend") != 0 &&
        strstr(error, "<GraphicsBackend>GDI</GraphicsBackend>") != 0,
        "Explicit DirectX failure is actionable");

    reset_mocks();
    directx_state.should_fail = 1;
    gdi_state.should_fail = 1;
    error[0] = 0;
    check(!smile_graphics_initialize(0, 960, 540, SMILE_GRAPHICS_BACKEND_AUTO, 1,
        error, (int)sizeof(error)), "Auto fails when both backends fail");
    check(strstr(error, "DirectX initialization failed") != 0 &&
        strstr(error, "GDI fallback failed") != 0,
        "Dual-backend failure reports both causes");

    reset_mocks();
    error[0] = 0;
    check(smile_graphics_initialize(0, 960, 540, SMILE_GRAPHICS_BACKEND_DIRECTX, 1,
        error, (int)sizeof(error)), "DirectX initializes for quadrilateral routing tests");
    smile_graphics_fill_quadrilateral(0, 0, 20, 0, 20, 20, 0, 20, 1);
    smile_graphics_draw_quadrilateral(0, 0, 20, 0, 20, 20, 0, 20, 2);
    check(directx_state.fill_quad_count == 1, "Filled quadrilateral reaches the active backend");
    check(directx_state.draw_quad_count == 1, "Outlined quadrilateral reaches the active backend");
    check(directx_state.begin_count == 1, "Quadrilateral drawing begins one shared frame");

    reset_mocks();
    error[0] = 0;
    check(smile_graphics_initialize(0, 960, 540, SMILE_GRAPHICS_BACKEND_DIRECTX, 1,
        error, (int)sizeof(error)), "DirectX initializes for arc routing tests");
    smile_graphics_draw_arc(101, 202, 33, -90, 270, 0x123456);
    check(directx_state.draw_arc_count == 1, "Arc reaches the active backend");
    check(directx_state.arc_values[0] == 101 && directx_state.arc_values[1] == 202 &&
        directx_state.arc_values[2] == 33 && directx_state.arc_values[3] == -90 &&
        directx_state.arc_values[4] == 270 && directx_state.arc_values[5] == 0x123456,
        "Arc forwards all six values in order");
    check(directx_state.begin_count == 1, "Arc drawing begins one shared frame");

    smile_graphics_draw_image((void*)1, 11, 12, 513, 257, 101, 102, 777, 333, 64, 1, 3);
    check(directx_state.draw_image_count == 1, "Image drawing reaches the active backend");
    check(directx_state.image_values[0] == 11 && directx_state.image_values[2] == 513 &&
        directx_state.image_values[4] == 101 && directx_state.image_values[6] == 777 &&
        directx_state.image_values[8] == 64 && directx_state.image_values[9] == 1 &&
        directx_state.image_values[10] == 3,
        "Image drawing preserves source/destination rectangles opacity filter and flip");
    smile_graphics_push_clip(5, 6, 700, 400);
    smile_graphics_push_clip(20, 30, 100, 80);
    smile_graphics_pop_clip();
    smile_graphics_pop_clip();
    check(directx_state.push_clip_count == 2 && directx_state.pop_clip_count == 2,
        "nested clip operations remain balanced at the backend boundary");
    check(smile_graphics_text_width("SMILE", 5, 20) == 100 &&
        smile_graphics_text_height("SMILE", 5, 20) == 22 && directx_state.text_measure_count == 2,
        "Text_Width and Text_Height route through the selected backend");
    check(smile_graphics_text_width("", 0, 20) == 0 && smile_graphics_text_height("", 0, 20) == 20 &&
        directx_state.text_measure_count == 2,
        "empty Text has zero width and a positive backend-independent line height");

    smile_graphics_push_clip(9, 10, 200, 120);
    smile_graphics_present();
    check(directx_state.push_clip_count == 3 && directx_state.pop_clip_count == 3,
        "frame presentation unwinds user clips before the backend ends its frame");
    smile_graphics_fill_rectangle(0, 0, 20, 20, 1);
    check(directx_state.begin_count == 2 && directx_state.push_clip_count == 4,
        "the next frame reapplies the shared logical clip stack");
    smile_graphics_resize(1280, 720);
    smile_graphics_begin_frame();
    check(directx_state.pop_clip_count == 4 && directx_state.push_clip_count == 5,
        "resize preserves and reapplies active logical clips");
    smile_graphics_pop_clip();

    reset_mocks();
    error[0] = 0;
    check(smile_graphics_initialize(0, 960, 540, SMILE_GRAPHICS_BACKEND_DIRECTX, 1,
        error, (int)sizeof(error)), "DirectX initializes for frame invalidation tests");
    smile_graphics_begin_frame();
    smile_graphics_resize(1280, 720);
    smile_graphics_begin_frame();
    check(directx_state.begin_count == 2, "Resize starts a fresh backend frame");
    smile_graphics_on_fullscreen_changed(1);
    smile_graphics_begin_frame();
    check(directx_state.begin_count == 3, "Fullscreen change starts a fresh backend frame");
    smile_graphics_on_dpi_changed(144);
    smile_graphics_begin_frame();
    check(directx_state.begin_count == 4, "DPI change starts a fresh backend frame");

    smile_audio_focus_initialize(&audio_focus);
    check(smile_audio_focus_accepts_sound(&audio_focus) != 0,
        "Active application window accepts WAV requests");
    audio_focus.app_active = 0;
    check(smile_audio_focus_update(&audio_focus) == -1 &&
        !smile_audio_focus_accepts_sound(&audio_focus),
        "Application deactivation signals immediate audio muting");
    check(smile_audio_focus_update(&audio_focus) == 0,
        "Repeated inactive state is idempotent");
    audio_focus.app_active = 1;
    check(smile_audio_focus_update(&audio_focus) == 1 &&
        smile_audio_focus_accepts_sound(&audio_focus),
        "Application reactivation accepts new WAV requests");
    audio_focus.window_active = 0;
    check(smile_audio_focus_update(&audio_focus) == -1,
        "Inactive top-level window mutes audio");
    audio_focus.app_active = 0;
    smile_audio_focus_update(&audio_focus);
    audio_focus.app_active = 1;
    check(smile_audio_focus_update(&audio_focus) == 0 &&
        !smile_audio_focus_accepts_sound(&audio_focus),
        "Application activation cannot override an inactive window");
    audio_focus.window_active = 1;
    check(smile_audio_focus_update(&audio_focus) == 1,
        "Active top-level window restores audio when other conditions allow it");
    audio_focus.minimized = 1;
    check(smile_audio_focus_update(&audio_focus) == -1 &&
        !smile_audio_focus_accepts_sound(&audio_focus),
        "Minimized windows reject WAV requests");
    audio_focus.minimized = 0;
    check(smile_audio_focus_update(&audio_focus) == 1 &&
        smile_audio_focus_accepts_sound(&audio_focus),
        "Restored windows accept WAV requests");
    check(smile_audio_effective_volume(1, 50) == 0.5,
        "Active music uses the exact requested volume");
    check(smile_audio_effective_volume(0, 50) == 0.0 &&
        smile_audio_effective_volume(1, 50) == 0.5,
        "Focus muting restores volume without cumulative drift");
    check(smile_audio_effective_volume(1, -5) == 0.0 &&
        smile_audio_effective_volume(1, 150) == 1.0,
        "Music volume clamps safely to zero through one hundred percent");

    smile_pointer_state_reset(&pointer_state);
    check(pointer_state.x == 0 && pointer_state.wheel_remainder == 0 &&
        pointer_state.held_buttons == 0, "Pointer reset clears persistent and transient state");
    smile_pointer_state_position(&pointer_state, 10, 20, 1);
    check(pointer_state.delta_x == 0 && pointer_state.delta_y == 0 && pointer_state.inside,
        "First pointer position establishes an origin without synthetic movement");
    smile_pointer_state_position(&pointer_state, 14, 25, 1);
    smile_pointer_state_position(&pointer_state, 20, 22, 1);
    check(pointer_state.delta_x == 10 && pointer_state.delta_y == 2,
        "Multiple pointer messages accumulate exact motion");
    check(smile_pointer_state_press(&pointer_state, 1) &&
        pointer_state.held_buttons == 1 && pointer_state.pressed_buttons == 1,
        "Pointer press latches held and pressed state");
    check(!smile_pointer_state_press(&pointer_state, 1) && pointer_state.pressed_buttons == 1,
        "Repeated pointer press is idempotent");
    check(smile_pointer_state_release(&pointer_state, 1) &&
        pointer_state.held_buttons == 0 && pointer_state.pressed_buttons == 1 &&
        pointer_state.released_buttons == 1,
        "Press and release in one pump preserve both transitions");
    smile_pointer_state_begin_frame(&pointer_state);
    check(pointer_state.delta_x == 0 && pointer_state.delta_y == 0 &&
        pointer_state.pressed_buttons == 0 && pointer_state.released_buttons == 0 &&
        pointer_state.x == 20 && pointer_state.y == 22,
        "Frame rollover clears only transient pointer state");
    smile_pointer_state_wheel(&pointer_state, 30, 120);
    check(pointer_state.wheel_delta == 0 && pointer_state.wheel_remainder == 30,
        "Partial positive wheel input remains pending");
    smile_pointer_state_begin_frame(&pointer_state);
    check(pointer_state.wheel_delta == 0 && pointer_state.wheel_remainder == 30,
        "Frame rollover preserves partial wheel input");
    smile_pointer_state_wheel(&pointer_state, 90, 120);
    check(pointer_state.wheel_delta == 1 && pointer_state.wheel_remainder == 0,
        "Positive partial wheel messages combine into one step");
    smile_pointer_state_begin_frame(&pointer_state);
    smile_pointer_state_wheel(&pointer_state, -45, 120);
    check(pointer_state.wheel_delta == 0 && pointer_state.wheel_remainder == -45,
        "Partial negative wheel input remains pending");
    smile_pointer_state_wheel(&pointer_state, -75, 120);
    check(pointer_state.wheel_delta == -1 && pointer_state.wheel_remainder == 0,
        "Negative partial wheel messages combine with signed truncation");
    smile_pointer_state_begin_frame(&pointer_state);
    smile_pointer_state_wheel(&pointer_state, 240, 120);
    check(pointer_state.wheel_delta == 2 && pointer_state.wheel_remainder == 0,
        "Multiple wheel steps in one message remain visible");
    check(smile_pointer_state_press(&pointer_state, 2) &&
        smile_pointer_state_press(&pointer_state, 3),
        "Secondary and middle buttons may be held together");
    smile_pointer_state_cancel(&pointer_state);
    check(pointer_state.held_buttons == 0 && pointer_state.released_buttons == 6 &&
        !pointer_state.inside && !pointer_state.position_valid,
        "Capture or focus loss releases every held pointer and invalidates position");

    reset_mocks();
    if (failures != 0)
    {
        fprintf(stderr, "%d native graphics selection test(s) failed.\n", failures);
        return 1;
    }
    printf("54 native graphics, pointer-input, and audio-focus checks passed.\n");
    return 0;
}
