#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <d3d11.h>
#include <dxgi1_3.h>
#include "graphics_common.h"
#include "graphics_directx.h"

struct SmileDirectXState
{
    HWND window;
    ID3D11Device* device;
    ID3D11DeviceContext* context;
    IDXGIFactory2* factory;
    IDXGISwapChain1* swap_chain;
    IDXGISwapChain2* swap_chain2;
    ID3D11RenderTargetView* render_target;
    HANDLE frame_latency_waitable;
    UINT swap_chain_flags;
    long long logical_width;
    long long logical_height;
    int physical_width;
    int physical_height;
    int vsync_enabled;
    int minimized;
    SmileGraphicsViewport viewport;
    char device_removal_reason[160];
};

static SmileDirectXState smile_directx;

template<typename T>
static void smile_directx_release(T*& object)
{
    if (object != 0)
        object->Release();
    object = 0;
}

static void smile_directx_zero_memory(void* memory, SIZE_T length)
{
    volatile unsigned char* current = (volatile unsigned char*)memory;
    while (length-- != 0)
        *current++ = 0;
}

static void smile_directx_append(char* destination, int capacity, const char* source)
{
    int length = lstrlenA(destination);
    while (source != 0 && *source != 0 && length + 1 < capacity)
        destination[length++] = *source++;
    destination[length] = 0;
}

static void smile_directx_append_hex(char* destination, int capacity, HRESULT result)
{
    static const char digits[] = "0123456789ABCDEF";
    unsigned long value = (unsigned long)result;
    char text[11];
    int index;
    text[0] = '0';
    text[1] = 'x';
    for (index = 0; index < 8; index++)
        text[index + 2] = digits[(value >> ((7 - index) * 4)) & 15UL];
    text[10] = 0;
    smile_directx_append(destination, capacity, text);
}

static void smile_directx_set_error(SmileDirectXState* state, char* error, int error_capacity,
    const char* stage, HRESULT result)
{
    state->device_removal_reason[0] = 0;
    smile_directx_append(state->device_removal_reason,
        (int)sizeof(state->device_removal_reason), stage);
    smile_directx_append(state->device_removal_reason,
        (int)sizeof(state->device_removal_reason), " failed with ");
    smile_directx_append_hex(state->device_removal_reason,
        (int)sizeof(state->device_removal_reason), result);
    if (error != 0 && error_capacity > 0)
        lstrcpynA(error, state->device_removal_reason, error_capacity);
}

static void smile_directx_current_client(const SmileDirectXState* state, int* width, int* height)
{
    RECT client;
    smile_directx_zero_memory(&client, sizeof(client));
    if (state->window != 0)
        GetClientRect(state->window, &client);
    *width = client.right - client.left;
    *height = client.bottom - client.top;
}

static void smile_directx_release_render_target(SmileDirectXState* state)
{
    if (state->context != 0)
        state->context->OMSetRenderTargets(0, 0, 0);
    smile_directx_release(state->render_target);
}

static HRESULT smile_directx_create_render_target(SmileDirectXState* state)
{
    ID3D11Texture2D* back_buffer = 0;
    HRESULT result;
    result = state->swap_chain->GetBuffer(0, __uuidof(ID3D11Texture2D),
        reinterpret_cast<void**>(&back_buffer));
    if (FAILED(result))
        return result;
    result = state->device->CreateRenderTargetView(back_buffer, 0, &state->render_target);
    back_buffer->Release();
    if (FAILED(result))
        return result;
    state->context->OMSetRenderTargets(1, &state->render_target, 0);
    return S_OK;
}

static void smile_directx_set_output_size(SmileDirectXState* state, int width, int height)
{
    D3D11_VIEWPORT output_viewport;
    state->physical_width = width;
    state->physical_height = height;
    smile_graphics_calculate_viewport(state->logical_width, state->logical_height,
        width, height, &state->viewport);
    smile_directx_zero_memory(&output_viewport, sizeof(output_viewport));
    output_viewport.Width = (FLOAT)width;
    output_viewport.Height = (FLOAT)height;
    output_viewport.MaxDepth = 1.0f;
    state->context->RSSetViewports(1, &output_viewport);
}

static HRESULT smile_directx_create_device(SmileDirectXState* state)
{
    D3D_FEATURE_LEVEL feature_levels[] =
    {
        D3D_FEATURE_LEVEL_11_1,
        D3D_FEATURE_LEVEL_11_0,
        D3D_FEATURE_LEVEL_10_1,
        D3D_FEATURE_LEVEL_10_0
    };
    D3D_FEATURE_LEVEL selected_level;
    UINT flags = D3D11_CREATE_DEVICE_BGRA_SUPPORT;
    HRESULT result;
#ifndef NDEBUG
    flags |= D3D11_CREATE_DEVICE_DEBUG;
#endif
    result = D3D11CreateDevice(0, D3D_DRIVER_TYPE_HARDWARE, 0, flags,
        feature_levels, (UINT)(sizeof(feature_levels) / sizeof(feature_levels[0])),
        D3D11_SDK_VERSION, &state->device, &selected_level, &state->context);
#ifndef NDEBUG
    if (result == DXGI_ERROR_SDK_COMPONENT_MISSING)
    {
        flags &= ~D3D11_CREATE_DEVICE_DEBUG;
        result = D3D11CreateDevice(0, D3D_DRIVER_TYPE_HARDWARE, 0, flags,
            feature_levels, (UINT)(sizeof(feature_levels) / sizeof(feature_levels[0])),
            D3D11_SDK_VERSION, &state->device, &selected_level, &state->context);
    }
#endif
    if (result == E_INVALIDARG)
    {
        result = D3D11CreateDevice(0, D3D_DRIVER_TYPE_HARDWARE, 0, flags,
            feature_levels + 1, (UINT)(sizeof(feature_levels) / sizeof(feature_levels[0])) - 1,
            D3D11_SDK_VERSION, &state->device, &selected_level, &state->context);
    }
    return result;
}

static HRESULT smile_directx_find_factory(SmileDirectXState* state)
{
    IDXGIDevice* dxgi_device = 0;
    IDXGIAdapter* adapter = 0;
    HRESULT result = state->device->QueryInterface(__uuidof(IDXGIDevice),
        reinterpret_cast<void**>(&dxgi_device));
    if (SUCCEEDED(result))
        result = dxgi_device->GetAdapter(&adapter);
    if (SUCCEEDED(result))
        result = adapter->GetParent(__uuidof(IDXGIFactory2),
            reinterpret_cast<void**>(&state->factory));
    smile_directx_release(adapter);
    smile_directx_release(dxgi_device);
    return result;
}

static HRESULT smile_directx_create_swap_chain(SmileDirectXState* state, int width, int height)
{
    DXGI_SWAP_CHAIN_DESC1 description;
    HRESULT result;
    smile_directx_zero_memory(&description, sizeof(description));
    description.Width = (UINT)width;
    description.Height = (UINT)height;
    description.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
    description.SampleDesc.Count = 1;
    description.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    description.BufferCount = 2;
    description.Scaling = DXGI_SCALING_STRETCH;
    description.SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD;
    description.AlphaMode = DXGI_ALPHA_MODE_IGNORE;
    description.Flags = DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT;
    result = state->factory->CreateSwapChainForHwnd(state->device, state->window,
        &description, 0, 0, &state->swap_chain);
    if (FAILED(result))
    {
        description.Flags = 0;
        result = state->factory->CreateSwapChainForHwnd(state->device, state->window,
            &description, 0, 0, &state->swap_chain);
    }
    if (FAILED(result))
        return result;
    state->swap_chain_flags = description.Flags;
    state->factory->MakeWindowAssociation(state->window, DXGI_MWA_NO_ALT_ENTER);
    if ((description.Flags & DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT) != 0 &&
        SUCCEEDED(state->swap_chain->QueryInterface(__uuidof(IDXGISwapChain2),
            reinterpret_cast<void**>(&state->swap_chain2))))
    {
        if (SUCCEEDED(state->swap_chain2->SetMaximumFrameLatency(1)))
            state->frame_latency_waitable = state->swap_chain2->GetFrameLatencyWaitableObject();
    }
    return S_OK;
}

static void smile_directx_shutdown_resources(SmileDirectXState* state)
{
    smile_directx_release_render_target(state);
    if (state->frame_latency_waitable != 0)
        CloseHandle(state->frame_latency_waitable);
    state->frame_latency_waitable = 0;
    smile_directx_release(state->swap_chain2);
    smile_directx_release(state->swap_chain);
    if (state->context != 0)
    {
        state->context->ClearState();
        state->context->Flush();
    }
    smile_directx_release(state->factory);
    smile_directx_release(state->context);
    smile_directx_release(state->device);
}

static int smile_directx_initialize(SmileGraphicsBackend* backend, void* native_window,
    long long logical_width, long long logical_height, int vsync_enabled,
    char* error, int error_capacity)
{
    SmileDirectXState* state = static_cast<SmileDirectXState*>(backend->state);
    HRESULT result;
    int width;
    int height;
    smile_directx_zero_memory(state, sizeof(*state));
    state->window = static_cast<HWND>(native_window);
    state->logical_width = logical_width;
    state->logical_height = logical_height;
    state->vsync_enabled = vsync_enabled;
    smile_directx_current_client(state, &width, &height);
    if (width <= 0) width = (int)logical_width;
    if (height <= 0) height = (int)logical_height;

    result = smile_directx_create_device(state);
    if (FAILED(result))
    {
        smile_directx_set_error(state, error, error_capacity,
            "Direct3D 11 device creation", result);
        smile_directx_shutdown_resources(state);
        return 0;
    }
    result = smile_directx_find_factory(state);
    if (FAILED(result))
    {
        smile_directx_set_error(state, error, error_capacity,
            "DXGI factory discovery", result);
        smile_directx_shutdown_resources(state);
        return 0;
    }
    result = smile_directx_create_swap_chain(state, width, height);
    if (FAILED(result))
    {
        smile_directx_set_error(state, error, error_capacity,
            "DXGI flip-model swap-chain creation", result);
        smile_directx_shutdown_resources(state);
        return 0;
    }
    result = smile_directx_create_render_target(state);
    if (FAILED(result))
    {
        smile_directx_set_error(state, error, error_capacity,
            "Direct3D render-target creation", result);
        smile_directx_shutdown_resources(state);
        return 0;
    }
    smile_directx_set_output_size(state, width, height);
    state->device_removal_reason[0] = 0;
    return 1;
}

static void smile_directx_resize(SmileGraphicsBackend* backend, int physical_width, int physical_height)
{
    SmileDirectXState* state = static_cast<SmileDirectXState*>(backend->state);
    HRESULT result;
    if (physical_width <= 0 || physical_height <= 0)
    {
        state->minimized = 1;
        return;
    }
    state->minimized = 0;
    if (state->swap_chain == 0 ||
        (physical_width == state->physical_width && physical_height == state->physical_height))
        return;
    smile_directx_release_render_target(state);
    if (state->context != 0)
        state->context->ClearState();
    result = state->swap_chain->ResizeBuffers(0, (UINT)physical_width, (UINT)physical_height,
        DXGI_FORMAT_UNKNOWN, state->swap_chain_flags);
    if (FAILED(result))
    {
        smile_directx_set_error(state, 0, 0, "DXGI ResizeBuffers", result);
        return;
    }
    result = smile_directx_create_render_target(state);
    if (FAILED(result))
    {
        smile_directx_set_error(state, 0, 0, "Direct3D render-target recreation", result);
        return;
    }
    smile_directx_set_output_size(state, physical_width, physical_height);
}

static void smile_directx_begin_frame(SmileGraphicsBackend* backend)
{
    SmileDirectXState* state = static_cast<SmileDirectXState*>(backend->state);
    static const FLOAT black[] = { 0.0f, 0.0f, 0.0f, 1.0f };
    if (state->frame_latency_waitable != 0)
        WaitForSingleObjectEx(state->frame_latency_waitable, 100, FALSE);
    if (!state->minimized && state->context != 0 && state->render_target != 0)
    {
        state->context->OMSetRenderTargets(1, &state->render_target, 0);
        state->context->ClearRenderTargetView(state->render_target, black);
    }
}

static void smile_directx_clear(SmileGraphicsBackend* backend, long long color)
{
    SmileDirectXState* state = static_cast<SmileDirectXState*>(backend->state);
    FLOAT converted[4];
    converted[0] = (FLOAT)(color & 255LL) / 255.0f;
    converted[1] = (FLOAT)((color >> 8) & 255LL) / 255.0f;
    converted[2] = (FLOAT)((color >> 16) & 255LL) / 255.0f;
    converted[3] = 1.0f;
    if (!state->minimized && state->context != 0 && state->render_target != 0)
        state->context->ClearRenderTargetView(state->render_target, converted);
}

static void smile_directx_fill_rectangle(SmileGraphicsBackend* backend, long long x,
    long long y, long long width, long long height, long long color)
{ (void)backend; (void)x; (void)y; (void)width; (void)height; (void)color; }

static void smile_directx_draw_rectangle(SmileGraphicsBackend* backend, long long x,
    long long y, long long width, long long height, long long color)
{ (void)backend; (void)x; (void)y; (void)width; (void)height; (void)color; }

static void smile_directx_fill_rounded_rectangle(SmileGraphicsBackend* backend, long long x,
    long long y, long long width, long long height, long long radius, long long color)
{ (void)backend; (void)x; (void)y; (void)width; (void)height; (void)radius; (void)color; }

static void smile_directx_draw_rounded_rectangle(SmileGraphicsBackend* backend, long long x,
    long long y, long long width, long long height, long long radius, long long color)
{ (void)backend; (void)x; (void)y; (void)width; (void)height; (void)radius; (void)color; }

static void smile_directx_fill_circle(SmileGraphicsBackend* backend, long long x,
    long long y, long long radius, long long color)
{ (void)backend; (void)x; (void)y; (void)radius; (void)color; }

static void smile_directx_draw_circle(SmileGraphicsBackend* backend, long long x,
    long long y, long long radius, long long color)
{ (void)backend; (void)x; (void)y; (void)radius; (void)color; }

static void smile_directx_draw_line(SmileGraphicsBackend* backend, long long x1,
    long long y1, long long x2, long long y2, long long color)
{ (void)backend; (void)x1; (void)y1; (void)x2; (void)y2; (void)color; }

static void smile_directx_draw_text(SmileGraphicsBackend* backend, const char* text,
    long long length, long long x, long long y, long long size, long long color,
    long long centered)
{ (void)backend; (void)text; (void)length; (void)x; (void)y; (void)size; (void)color; (void)centered; }

static void smile_directx_draw_number(SmileGraphicsBackend* backend, long long value,
    long long x, long long y, long long size, long long color)
{ (void)backend; (void)value; (void)x; (void)y; (void)size; (void)color; }

static int smile_directx_present(SmileGraphicsBackend* backend)
{
    SmileDirectXState* state = static_cast<SmileDirectXState*>(backend->state);
    HRESULT result;
    if (state->minimized || state->swap_chain == 0)
        return 1;
    result = state->swap_chain->Present(state->vsync_enabled ? 1 : 0, 0);
    if (result == DXGI_ERROR_DEVICE_REMOVED || result == DXGI_ERROR_DEVICE_RESET)
    {
        HRESULT reason = state->device != 0 ? state->device->GetDeviceRemovedReason() : result;
        smile_directx_set_error(state, 0, 0, "DirectX device removal", reason);
    }
    return SUCCEEDED(result);
}

static void smile_directx_repaint(SmileGraphicsBackend* backend, void* native_paint_context)
{ (void)backend; (void)native_paint_context; }

static void smile_directx_on_fullscreen_changed(SmileGraphicsBackend* backend, int fullscreen)
{
    SmileDirectXState* state = static_cast<SmileDirectXState*>(backend->state);
    int width;
    int height;
    (void)fullscreen;
    smile_directx_current_client(state, &width, &height);
    smile_directx_resize(backend, width, height);
}

static void smile_directx_on_dpi_changed(SmileGraphicsBackend* backend, unsigned int dpi)
{
    SmileDirectXState* state = static_cast<SmileDirectXState*>(backend->state);
    int width;
    int height;
    (void)dpi;
    smile_directx_current_client(state, &width, &height);
    smile_directx_resize(backend, width, height);
}

static void smile_directx_shutdown(SmileGraphicsBackend* backend)
{
    SmileDirectXState* state = static_cast<SmileDirectXState*>(backend->state);
    smile_directx_shutdown_resources(state);
    state->window = 0;
}

static const char* smile_directx_get_backend_name(const SmileGraphicsBackend* backend)
{
    (void)backend;
    return "DirectX";
}

static void smile_directx_get_diagnostics(const SmileGraphicsBackend* backend,
    SmileGraphicsBackendDiagnostics* diagnostics)
{
    const SmileDirectXState* state = static_cast<const SmileDirectXState*>(backend->state);
    diagnostics->physical_width = state->physical_width;
    diagnostics->physical_height = state->physical_height;
    diagnostics->viewport_x = state->viewport.x;
    diagnostics->viewport_y = state->viewport.y;
    diagnostics->viewport_width = state->viewport.width;
    diagnostics->viewport_height = state->viewport.height;
    diagnostics->scale = state->viewport.scale;
    diagnostics->pacing_mode = state->frame_latency_waitable != 0
        ? "DXGI frame-latency waitable object" : "DXGI synchronized presentation";
    diagnostics->device_removal_reason = state->device_removal_reason[0] != 0
        ? state->device_removal_reason : "None";
}

static const SmileGraphicsBackendVTable smile_directx_operations =
{
    smile_directx_initialize,
    smile_directx_resize,
    smile_directx_begin_frame,
    smile_directx_clear,
    smile_directx_fill_rectangle,
    smile_directx_draw_rectangle,
    smile_directx_fill_rounded_rectangle,
    smile_directx_draw_rounded_rectangle,
    smile_directx_fill_circle,
    smile_directx_draw_circle,
    smile_directx_draw_line,
    smile_directx_draw_text,
    smile_directx_draw_number,
    smile_directx_present,
    smile_directx_repaint,
    smile_directx_on_fullscreen_changed,
    smile_directx_on_dpi_changed,
    smile_directx_shutdown,
    smile_directx_get_backend_name,
    smile_directx_get_diagnostics
};

extern "C" void smile_graphics_directx_create(SmileGraphicsBackend* backend)
{
    backend->operations = &smile_directx_operations;
    backend->state = &smile_directx;
}
