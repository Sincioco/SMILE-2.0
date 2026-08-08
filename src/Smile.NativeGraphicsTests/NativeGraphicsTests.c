#include <stdio.h>
#include <string.h>
#include "graphics_common.h"
#include "graphics_directx.h"
#include "graphics_gdi.h"

typedef struct MockState
{
    const char* name;
    int initialize_count;
    int shutdown_count;
    int should_fail;
    const char* failure;
} MockState;

static MockState directx_state = { "DirectX", 0, 0, 0, "DXGI swap-chain creation failed with 0x887A0004 (DXGI_ERROR_UNSUPPORTED)" };
static MockState gdi_state = { "GDI", 0, 0, 0, "GDI physical back-buffer creation failed." };
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
static void mock_begin(SmileGraphicsBackend* backend) { (void)backend; }
static void mock_clear(SmileGraphicsBackend* backend, long long color) { (void)backend; (void)color; }
static void mock_rectangle(SmileGraphicsBackend* backend, long long x, long long y,
    long long width, long long height, long long color)
{ (void)backend; (void)x; (void)y; (void)width; (void)height; (void)color; }
static void mock_rounded(SmileGraphicsBackend* backend, long long x, long long y,
    long long width, long long height, long long radius, long long color)
{ (void)backend; (void)x; (void)y; (void)width; (void)height; (void)radius; (void)color; }
static void mock_circle(SmileGraphicsBackend* backend, long long x, long long y,
    long long radius, long long color)
{ (void)backend; (void)x; (void)y; (void)radius; (void)color; }
static void mock_line(SmileGraphicsBackend* backend, long long x1, long long y1,
    long long x2, long long y2, long long color)
{ (void)backend; (void)x1; (void)y1; (void)x2; (void)y2; (void)color; }
static void mock_text(SmileGraphicsBackend* backend, const char* text, long long length,
    long long x, long long y, long long size, long long color, long long centered)
{ (void)backend; (void)text; (void)length; (void)x; (void)y; (void)size; (void)color; (void)centered; }
static void mock_number(SmileGraphicsBackend* backend, long long value, long long x,
    long long y, long long size, long long color)
{ (void)backend; (void)value; (void)x; (void)y; (void)size; (void)color; }
static int mock_present(SmileGraphicsBackend* backend) { (void)backend; return 1; }
static void mock_context(SmileGraphicsBackend* backend, void* context)
{ (void)backend; (void)context; }
static void mock_flag(SmileGraphicsBackend* backend, int value)
{ (void)backend; (void)value; }
static void mock_dpi(SmileGraphicsBackend* backend, unsigned int value)
{ (void)backend; (void)value; }

static const SmileGraphicsBackendVTable mock_operations =
{
    mock_initialize, mock_resize, mock_begin, mock_clear,
    mock_rectangle, mock_rectangle, mock_rounded, mock_rounded,
    mock_circle, mock_circle, mock_line, mock_text, mock_number,
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
    if (failures != 0)
    {
        fprintf(stderr, "%d native graphics selection test(s) failed.\n", failures);
        return 1;
    }
    printf("15 native graphics selection checks passed.\n");
    return 0;
}
