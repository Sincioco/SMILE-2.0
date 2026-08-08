#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <d3d11.h>
#include <d2d1_1.h>
#include <dwrite.h>
#include <dxgi1_3.h>
#include <dxgi1_5.h>
#include "graphics_common.h"
#include "graphics_directx.h"

struct SmileDirectXBrushCacheEntry
{
    unsigned long color;
    ID2D1SolidColorBrush* handle;
};

struct SmileDirectXTextFormatCacheEntry
{
    int size;
    IDWriteTextFormat* handle;
};

struct SmileDirectXState
{
    HWND window;
    ID3D11Device* device;
    ID3D11DeviceContext* context;
    IDXGIFactory2* factory;
    IDXGISwapChain1* swap_chain;
    IDXGISwapChain2* swap_chain2;
    ID3D11RenderTargetView* render_target;
    ID2D1Factory1* d2d_factory;
    ID2D1Device* d2d_device;
    ID2D1DeviceContext* d2d_context;
    ID2D1Bitmap1* d2d_target;
    IDWriteFactory* dwrite_factory;
    HANDLE frame_latency_waitable;
    UINT swap_chain_flags;
    long long logical_width;
    long long logical_height;
    int physical_width;
    int physical_height;
    int vsync_enabled;
    int tearing_supported;
    int minimized;
    int frame_active;
    int viewport_clip_active;
    SmileGraphicsViewport viewport;
    SmileDirectXBrushCacheEntry brushes[64];
    unsigned int next_brush;
    SmileDirectXTextFormatCacheEntry text_formats[64];
    unsigned int next_text_format;
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

static const char* smile_directx_hresult_name(HRESULT result)
{
    switch (result)
    {
        case DXGI_ERROR_DEVICE_HUNG: return "DXGI_ERROR_DEVICE_HUNG";
        case DXGI_ERROR_DEVICE_REMOVED: return "DXGI_ERROR_DEVICE_REMOVED";
        case DXGI_ERROR_DEVICE_RESET: return "DXGI_ERROR_DEVICE_RESET";
        case DXGI_ERROR_DRIVER_INTERNAL_ERROR: return "DXGI_ERROR_DRIVER_INTERNAL_ERROR";
        case DXGI_ERROR_INVALID_CALL: return "DXGI_ERROR_INVALID_CALL";
        case DXGI_ERROR_NOT_CURRENTLY_AVAILABLE: return "DXGI_ERROR_NOT_CURRENTLY_AVAILABLE";
        case DXGI_ERROR_UNSUPPORTED: return "DXGI_ERROR_UNSUPPORTED";
        case E_INVALIDARG: return "E_INVALIDARG";
        case E_OUTOFMEMORY: return "E_OUTOFMEMORY";
        default: return "Unknown HRESULT";
    }
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
    smile_directx_append(state->device_removal_reason,
        (int)sizeof(state->device_removal_reason), " (");
    smile_directx_append(state->device_removal_reason,
        (int)sizeof(state->device_removal_reason), smile_directx_hresult_name(result));
    smile_directx_append(state->device_removal_reason,
        (int)sizeof(state->device_removal_reason), ")");
    if (error != 0 && error_capacity > 0)
        lstrcpynA(error, state->device_removal_reason, error_capacity);
}

static D2D1_COLOR_F smile_directx_color(long long color)
{
    D2D1_COLOR_F converted;
    converted.r = (FLOAT)(color & 255LL) / 255.0f;
    converted.g = (FLOAT)((color >> 8) & 255LL) / 255.0f;
    converted.b = (FLOAT)((color >> 16) & 255LL) / 255.0f;
    converted.a = 1.0f;
    return converted;
}

static void smile_directx_release_brushes(SmileDirectXState* state)
{
    int index;
    for (index = 0; index < (int)(sizeof(state->brushes) / sizeof(state->brushes[0])); index++)
    {
        smile_directx_release(state->brushes[index].handle);
        state->brushes[index].color = 0;
    }
    state->next_brush = 0;
}

static ID2D1SolidColorBrush* smile_directx_brush(SmileDirectXState* state, long long color)
{
    unsigned long key = (unsigned long)color & 0x00FFFFFFUL;
    unsigned int index;
    HRESULT result;
    for (index = 0; index < (unsigned int)(sizeof(state->brushes) / sizeof(state->brushes[0])); index++)
    {
        if (state->brushes[index].handle != 0 && state->brushes[index].color == key)
            return state->brushes[index].handle;
    }
    index = state->next_brush++ % (unsigned int)(sizeof(state->brushes) / sizeof(state->brushes[0]));
    smile_directx_release(state->brushes[index].handle);
    result = state->d2d_context->CreateSolidColorBrush(smile_directx_color(color),
        &state->brushes[index].handle);
    if (FAILED(result))
    {
        smile_directx_set_error(state, 0, 0, "Direct2D brush creation", result);
        return 0;
    }
    state->brushes[index].color = key;
    return state->brushes[index].handle;
}

static void smile_directx_release_text_formats(SmileDirectXState* state)
{
    int index;
    for (index = 0; index < (int)(sizeof(state->text_formats) / sizeof(state->text_formats[0])); index++)
    {
        smile_directx_release(state->text_formats[index].handle);
        state->text_formats[index].size = 0;
    }
    state->next_text_format = 0;
}

static IDWriteTextFormat* smile_directx_text_format(SmileDirectXState* state, int size)
{
    unsigned int index;
    HRESULT result;
    if (size < 1)
        size = 1;
    for (index = 0; index < (unsigned int)(sizeof(state->text_formats) / sizeof(state->text_formats[0])); index++)
    {
        if (state->text_formats[index].handle != 0 && state->text_formats[index].size == size)
            return state->text_formats[index].handle;
    }
    index = state->next_text_format++ %
        (unsigned int)(sizeof(state->text_formats) / sizeof(state->text_formats[0]));
    smile_directx_release(state->text_formats[index].handle);
    result = state->dwrite_factory->CreateTextFormat(L"Consolas", 0,
        DWRITE_FONT_WEIGHT_BOLD, DWRITE_FONT_STYLE_NORMAL, DWRITE_FONT_STRETCH_NORMAL,
        (FLOAT)size, L"en-us", &state->text_formats[index].handle);
    if (FAILED(result))
    {
        smile_directx_set_error(state, 0, 0, "DirectWrite text-format creation", result);
        return 0;
    }
    state->text_formats[index].handle->SetTextAlignment(DWRITE_TEXT_ALIGNMENT_LEADING);
    state->text_formats[index].handle->SetParagraphAlignment(DWRITE_PARAGRAPH_ALIGNMENT_NEAR);
    state->text_formats[index].handle->SetWordWrapping(DWRITE_WORD_WRAPPING_NO_WRAP);
    state->text_formats[index].size = size;
    return state->text_formats[index].handle;
}

static WCHAR* smile_directx_utf8_to_wide(const char* text, long long length, int* wide_length)
{
    int count;
    WCHAR* result;
    *wide_length = 0;
    if (text == 0 || length < 0 || length > INT_MAX)
        return 0;
    count = MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, text, (int)length, 0, 0);
    if (count <= 0)
        return 0;
    result = static_cast<WCHAR*>(HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY,
        (SIZE_T)(count + 1) * sizeof(WCHAR)));
    if (result == 0)
        return 0;
    if (MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, text, (int)length, result, count) != count)
    {
        HeapFree(GetProcessHeap(), 0, result);
        return 0;
    }
    result[count] = 0;
    *wide_length = count;
    return result;
}

static int smile_directx_format_number(long long value, WCHAR* buffer, int capacity)
{
    WCHAR temporary[32];
    int count = 0;
    int output = 0;
    unsigned long long magnitude;
    if (capacity <= 0)
        return 0;
    magnitude = value < 0 ? (unsigned long long)(-(value + 1)) + 1ULL : (unsigned long long)value;
    do
    {
        temporary[count++] = (WCHAR)(L'0' + (magnitude % 10));
        magnitude /= 10;
    }
    while (magnitude != 0 && count < (int)(sizeof(temporary) / sizeof(temporary[0])));
    if (value < 0 && output < capacity)
        buffer[output++] = L'-';
    while (count > 0 && output < capacity)
        buffer[output++] = temporary[--count];
    return output;
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

static HRESULT smile_directx_end_frame(SmileDirectXState* state)
{
    HRESULT result = S_OK;
    if (state->d2d_context == 0 || !state->frame_active)
        return result;
    if (state->viewport_clip_active)
        state->d2d_context->PopAxisAlignedClip();
    state->viewport_clip_active = 0;
    result = state->d2d_context->EndDraw();
    state->frame_active = 0;
    return result;
}

static void smile_directx_release_render_target(SmileDirectXState* state)
{
    smile_directx_end_frame(state);
    if (state->d2d_context != 0)
        state->d2d_context->SetTarget(0);
    smile_directx_release(state->d2d_target);
    smile_directx_release_brushes(state);
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

static HRESULT smile_directx_create_d2d_device(SmileDirectXState* state)
{
    D2D1_FACTORY_OPTIONS options;
    IDXGIDevice* dxgi_device = 0;
    HRESULT result;
    smile_directx_zero_memory(&options, sizeof(options));
#ifndef NDEBUG
    options.debugLevel = D2D1_DEBUG_LEVEL_INFORMATION;
#endif
    result = D2D1CreateFactory(D2D1_FACTORY_TYPE_SINGLE_THREADED,
        __uuidof(ID2D1Factory1), &options, reinterpret_cast<void**>(&state->d2d_factory));
    if (FAILED(result))
        return result;
    result = state->device->QueryInterface(__uuidof(IDXGIDevice),
        reinterpret_cast<void**>(&dxgi_device));
    if (SUCCEEDED(result))
        result = state->d2d_factory->CreateDevice(dxgi_device, &state->d2d_device);
    if (SUCCEEDED(result))
        result = state->d2d_device->CreateDeviceContext(D2D1_DEVICE_CONTEXT_OPTIONS_NONE,
            &state->d2d_context);
    smile_directx_release(dxgi_device);
    if (SUCCEEDED(result))
    {
        state->d2d_context->SetAntialiasMode(D2D1_ANTIALIAS_MODE_PER_PRIMITIVE);
        state->d2d_context->SetTextAntialiasMode(D2D1_TEXT_ANTIALIAS_MODE_GRAYSCALE);
    }
    return result;
}

static HRESULT smile_directx_create_dwrite_factory(SmileDirectXState* state)
{
    return DWriteCreateFactory(DWRITE_FACTORY_TYPE_SHARED, __uuidof(IDWriteFactory),
        reinterpret_cast<IUnknown**>(&state->dwrite_factory));
}

static HRESULT smile_directx_create_d2d_target(SmileDirectXState* state)
{
    IDXGISurface* surface = 0;
    D2D1_BITMAP_PROPERTIES1 properties;
    HRESULT result = state->swap_chain->GetBuffer(0, __uuidof(IDXGISurface),
        reinterpret_cast<void**>(&surface));
    if (FAILED(result))
        return result;
    smile_directx_zero_memory(&properties, sizeof(properties));
    properties.pixelFormat.format = DXGI_FORMAT_B8G8R8A8_UNORM;
    properties.pixelFormat.alphaMode = D2D1_ALPHA_MODE_IGNORE;
    properties.dpiX = 96.0f;
    properties.dpiY = 96.0f;
    properties.bitmapOptions = D2D1_BITMAP_OPTIONS_TARGET | D2D1_BITMAP_OPTIONS_CANNOT_DRAW;
    result = state->d2d_context->CreateBitmapFromDxgiSurface(surface, &properties,
        &state->d2d_target);
    surface->Release();
    if (FAILED(result))
        return result;
    state->d2d_context->SetTarget(state->d2d_target);
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

static void smile_directx_detect_tearing(SmileDirectXState* state)
{
    IDXGIFactory5* factory5 = 0;
    BOOL supported = FALSE;
    if (SUCCEEDED(state->factory->QueryInterface(__uuidof(IDXGIFactory5),
        reinterpret_cast<void**>(&factory5))))
    {
        if (SUCCEEDED(factory5->CheckFeatureSupport(DXGI_FEATURE_PRESENT_ALLOW_TEARING,
            &supported, sizeof(supported))))
            state->tearing_supported = supported != FALSE;
        factory5->Release();
    }
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
    if (!state->vsync_enabled && state->tearing_supported)
        description.Flags |= DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING;
    result = state->factory->CreateSwapChainForHwnd(state->device, state->window,
        &description, 0, 0, &state->swap_chain);
    if (FAILED(result))
    {
        description.Flags &= ~DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT;
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
    smile_directx_release_text_formats(state);
    smile_directx_release(state->dwrite_factory);
    smile_directx_release(state->d2d_context);
    smile_directx_release(state->d2d_device);
    smile_directx_release(state->d2d_factory);
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
    smile_directx_detect_tearing(state);
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
    result = smile_directx_create_d2d_device(state);
    if (FAILED(result))
    {
        smile_directx_set_error(state, error, error_capacity,
            "Direct2D device creation", result);
        smile_directx_shutdown_resources(state);
        return 0;
    }
    result = smile_directx_create_dwrite_factory(state);
    if (FAILED(result))
    {
        smile_directx_set_error(state, error, error_capacity,
            "DirectWrite factory creation", result);
        smile_directx_shutdown_resources(state);
        return 0;
    }
    result = smile_directx_create_d2d_target(state);
    if (FAILED(result))
    {
        smile_directx_set_error(state, error, error_capacity,
            "Direct2D target creation", result);
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
    result = smile_directx_end_frame(state);
    if (FAILED(result) && result != D2DERR_RECREATE_TARGET)
        smile_directx_set_error(state, 0, 0, "Direct2D resize EndDraw", result);
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
    result = smile_directx_create_d2d_target(state);
    if (FAILED(result))
    {
        smile_directx_set_error(state, 0, 0, "Direct2D target recreation", result);
        return;
    }
    smile_directx_set_output_size(state, physical_width, physical_height);
}

static void smile_directx_begin_frame(SmileGraphicsBackend* backend)
{
    SmileDirectXState* state = static_cast<SmileDirectXState*>(backend->state);
    D2D1_RECT_F viewport;
    if (state->frame_latency_waitable != 0)
        WaitForSingleObjectEx(state->frame_latency_waitable, 100, FALSE);
    if (state->frame_active || state->minimized || state->d2d_context == 0 || state->d2d_target == 0)
        return;
    state->d2d_context->BeginDraw();
    state->frame_active = 1;
    state->d2d_context->Clear(D2D1::ColorF(D2D1::ColorF::Black));
    viewport.left = (FLOAT)state->viewport.x;
    viewport.top = (FLOAT)state->viewport.y;
    viewport.right = (FLOAT)(state->viewport.x + state->viewport.width);
    viewport.bottom = (FLOAT)(state->viewport.y + state->viewport.height);
    state->d2d_context->PushAxisAlignedClip(viewport, D2D1_ANTIALIAS_MODE_ALIASED);
    state->viewport_clip_active = 1;
}

static void smile_directx_clear(SmileGraphicsBackend* backend, long long color)
{
    SmileDirectXState* state = static_cast<SmileDirectXState*>(backend->state);
    ID2D1SolidColorBrush* brush = smile_directx_brush(state, color);
    D2D1_RECT_F viewport;
    if (!state->frame_active || brush == 0)
        return;
    viewport.left = (FLOAT)state->viewport.x;
    viewport.top = (FLOAT)state->viewport.y;
    viewport.right = (FLOAT)(state->viewport.x + state->viewport.width);
    viewport.bottom = (FLOAT)(state->viewport.y + state->viewport.height);
    state->d2d_context->FillRectangle(viewport, brush);
}

static D2D1_RECT_F smile_directx_rectangle(const SmileDirectXState* state,
    long long x, long long y, long long width, long long height)
{
    D2D1_RECT_F rectangle;
    rectangle.left = (FLOAT)smile_graphics_map_x(&state->viewport, (double)x);
    rectangle.top = (FLOAT)smile_graphics_map_y(&state->viewport, (double)y);
    rectangle.right = (FLOAT)smile_graphics_map_x(&state->viewport, (double)(x + width));
    rectangle.bottom = (FLOAT)smile_graphics_map_y(&state->viewport, (double)(y + height));
    return rectangle;
}

static FLOAT smile_directx_stroke_width(const SmileDirectXState* state)
{
    FLOAT width = (FLOAT)smile_graphics_map_size(&state->viewport, 1.0);
    return width < 1.0f ? 1.0f : width;
}

static void smile_directx_fill_rectangle(SmileGraphicsBackend* backend, long long x,
    long long y, long long width, long long height, long long color)
{
    SmileDirectXState* state = static_cast<SmileDirectXState*>(backend->state);
    ID2D1SolidColorBrush* brush = smile_directx_brush(state, color);
    D2D1_RECT_F rectangle = smile_directx_rectangle(state, x, y, width, height);
    if (state->frame_active && brush != 0)
        state->d2d_context->FillRectangle(rectangle, brush);
}

static void smile_directx_draw_rectangle(SmileGraphicsBackend* backend, long long x,
    long long y, long long width, long long height, long long color)
{
    SmileDirectXState* state = static_cast<SmileDirectXState*>(backend->state);
    ID2D1SolidColorBrush* brush = smile_directx_brush(state, color);
    D2D1_RECT_F rectangle = smile_directx_rectangle(state, x, y, width, height);
    if (state->frame_active && brush != 0)
        state->d2d_context->DrawRectangle(rectangle, brush, smile_directx_stroke_width(state));
}

static void smile_directx_fill_rounded_rectangle(SmileGraphicsBackend* backend, long long x,
    long long y, long long width, long long height, long long radius, long long color)
{
    SmileDirectXState* state = static_cast<SmileDirectXState*>(backend->state);
    ID2D1SolidColorBrush* brush = smile_directx_brush(state, color);
    D2D1_ROUNDED_RECT rounded;
    rounded.rect = smile_directx_rectangle(state, x, y, width, height);
    rounded.radiusX = rounded.radiusY = (FLOAT)smile_graphics_map_size(&state->viewport, (double)radius);
    if (state->frame_active && brush != 0)
        state->d2d_context->FillRoundedRectangle(rounded, brush);
}

static void smile_directx_draw_rounded_rectangle(SmileGraphicsBackend* backend, long long x,
    long long y, long long width, long long height, long long radius, long long color)
{
    SmileDirectXState* state = static_cast<SmileDirectXState*>(backend->state);
    ID2D1SolidColorBrush* brush = smile_directx_brush(state, color);
    D2D1_ROUNDED_RECT rounded;
    rounded.rect = smile_directx_rectangle(state, x, y, width, height);
    rounded.radiusX = rounded.radiusY = (FLOAT)smile_graphics_map_size(&state->viewport, (double)radius);
    if (state->frame_active && brush != 0)
        state->d2d_context->DrawRoundedRectangle(rounded, brush, smile_directx_stroke_width(state));
}

static void smile_directx_fill_circle(SmileGraphicsBackend* backend, long long x,
    long long y, long long radius, long long color)
{
    SmileDirectXState* state = static_cast<SmileDirectXState*>(backend->state);
    ID2D1SolidColorBrush* brush = smile_directx_brush(state, color);
    D2D1_ELLIPSE circle = D2D1::Ellipse(
        D2D1::Point2F((FLOAT)smile_graphics_map_x(&state->viewport, (double)x),
            (FLOAT)smile_graphics_map_y(&state->viewport, (double)y)),
        (FLOAT)smile_graphics_map_size(&state->viewport, (double)radius),
        (FLOAT)smile_graphics_map_size(&state->viewport, (double)radius));
    if (state->frame_active && brush != 0)
        state->d2d_context->FillEllipse(circle, brush);
}

static void smile_directx_draw_circle(SmileGraphicsBackend* backend, long long x,
    long long y, long long radius, long long color)
{
    SmileDirectXState* state = static_cast<SmileDirectXState*>(backend->state);
    ID2D1SolidColorBrush* brush = smile_directx_brush(state, color);
    D2D1_ELLIPSE circle = D2D1::Ellipse(
        D2D1::Point2F((FLOAT)smile_graphics_map_x(&state->viewport, (double)x),
            (FLOAT)smile_graphics_map_y(&state->viewport, (double)y)),
        (FLOAT)smile_graphics_map_size(&state->viewport, (double)radius),
        (FLOAT)smile_graphics_map_size(&state->viewport, (double)radius));
    if (state->frame_active && brush != 0)
        state->d2d_context->DrawEllipse(circle, brush, smile_directx_stroke_width(state));
}

static void smile_directx_quadrilateral(SmileGraphicsBackend* backend,
    long long x1, long long y1, long long x2, long long y2,
    long long x3, long long y3, long long x4, long long y4,
    long long color, int fill)
{
    SmileDirectXState* state = static_cast<SmileDirectXState*>(backend->state);
    ID2D1SolidColorBrush* brush;
    ID2D1PathGeometry* geometry = 0;
    ID2D1GeometrySink* sink = 0;
    HRESULT result;
    if (!state->frame_active || state->d2d_factory == 0 || state->d2d_context == 0)
        return;
    brush = smile_directx_brush(state, color);
    if (brush == 0)
        return;
    result = state->d2d_factory->CreatePathGeometry(&geometry);
    if (FAILED(result))
    {
        smile_directx_set_error(state, 0, 0, "Direct2D quadrilateral geometry creation", result);
        return;
    }
    result = geometry->Open(&sink);
    if (FAILED(result))
    {
        smile_directx_set_error(state, 0, 0, "Direct2D quadrilateral geometry open", result);
        smile_directx_release(geometry);
        return;
    }
    sink->BeginFigure(D2D1::Point2F(
        (FLOAT)smile_graphics_map_x(&state->viewport, (double)x1),
        (FLOAT)smile_graphics_map_y(&state->viewport, (double)y1)),
        fill ? D2D1_FIGURE_BEGIN_FILLED : D2D1_FIGURE_BEGIN_HOLLOW);
    sink->AddLine(D2D1::Point2F(
        (FLOAT)smile_graphics_map_x(&state->viewport, (double)x2),
        (FLOAT)smile_graphics_map_y(&state->viewport, (double)y2)));
    sink->AddLine(D2D1::Point2F(
        (FLOAT)smile_graphics_map_x(&state->viewport, (double)x3),
        (FLOAT)smile_graphics_map_y(&state->viewport, (double)y3)));
    sink->AddLine(D2D1::Point2F(
        (FLOAT)smile_graphics_map_x(&state->viewport, (double)x4),
        (FLOAT)smile_graphics_map_y(&state->viewport, (double)y4)));
    sink->EndFigure(D2D1_FIGURE_END_CLOSED);
    result = sink->Close();
    if (FAILED(result))
        smile_directx_set_error(state, 0, 0, "Direct2D quadrilateral geometry close", result);
    else if (fill)
        state->d2d_context->FillGeometry(geometry, brush);
    else
        state->d2d_context->DrawGeometry(geometry, brush, smile_directx_stroke_width(state));
    smile_directx_release(sink);
    smile_directx_release(geometry);
}

static void smile_directx_fill_quadrilateral(SmileGraphicsBackend* backend,
    long long x1, long long y1, long long x2, long long y2,
    long long x3, long long y3, long long x4, long long y4, long long color)
{
    smile_directx_quadrilateral(backend, x1, y1, x2, y2, x3, y3, x4, y4, color, 1);
}

static void smile_directx_draw_quadrilateral(SmileGraphicsBackend* backend,
    long long x1, long long y1, long long x2, long long y2,
    long long x3, long long y3, long long x4, long long y4, long long color)
{
    smile_directx_quadrilateral(backend, x1, y1, x2, y2, x3, y3, x4, y4, color, 0);
}

static void smile_directx_draw_line(SmileGraphicsBackend* backend, long long x1,
    long long y1, long long x2, long long y2, long long color)
{
    SmileDirectXState* state = static_cast<SmileDirectXState*>(backend->state);
    ID2D1SolidColorBrush* brush = smile_directx_brush(state, color);
    D2D1_POINT_2F start = D2D1::Point2F(
        (FLOAT)smile_graphics_map_x(&state->viewport, (double)x1),
        (FLOAT)smile_graphics_map_y(&state->viewport, (double)y1));
    D2D1_POINT_2F end = D2D1::Point2F(
        (FLOAT)smile_graphics_map_x(&state->viewport, (double)x2),
        (FLOAT)smile_graphics_map_y(&state->viewport, (double)y2));
    if (state->frame_active && brush != 0)
        state->d2d_context->DrawLine(start, end, brush, smile_directx_stroke_width(state));
}

static void smile_directx_draw_text(SmileGraphicsBackend* backend, const char* text,
    long long length, long long x, long long y, long long size, long long color,
    long long centered)
{
    SmileDirectXState* state = static_cast<SmileDirectXState*>(backend->state);
    WCHAR* wide;
    int wide_length;
    int physical_size;
    IDWriteTextFormat* format;
    IDWriteTextLayout* layout = 0;
    ID2D1SolidColorBrush* brush;
    DWRITE_TEXT_METRICS metrics;
    D2D1_POINT_2F origin;
    HRESULT result;
    if (!state->frame_active || text == 0 || length <= 0)
        return;
    wide = smile_directx_utf8_to_wide(text, length, &wide_length);
    if (wide == 0 || wide_length <= 0)
        return;
    physical_size = smile_graphics_round_pixel(
        smile_graphics_map_size(&state->viewport, (double)size));
    if (physical_size < 1)
        physical_size = 1;
    format = smile_directx_text_format(state, physical_size);
    brush = smile_directx_brush(state, color);
    if (format == 0 || brush == 0)
    {
        HeapFree(GetProcessHeap(), 0, wide);
        return;
    }
    result = state->dwrite_factory->CreateTextLayout(wide, (UINT32)wide_length, format,
        (FLOAT)state->physical_width * 2.0f, (FLOAT)physical_size * 3.0f, &layout);
    HeapFree(GetProcessHeap(), 0, wide);
    if (FAILED(result))
    {
        smile_directx_set_error(state, 0, 0, "DirectWrite text-layout creation", result);
        return;
    }
    origin.x = (FLOAT)smile_graphics_map_x(&state->viewport, (double)x);
    origin.y = (FLOAT)smile_graphics_map_y(&state->viewport, (double)y);
    if (centered != 0 && SUCCEEDED(layout->GetMetrics(&metrics)))
        origin.x -= metrics.widthIncludingTrailingWhitespace * 0.5f;
    state->d2d_context->DrawTextLayout(origin, layout, brush, D2D1_DRAW_TEXT_OPTIONS_NONE);
    layout->Release();
}

static void smile_directx_draw_number(SmileGraphicsBackend* backend, long long value,
    long long x, long long y, long long size, long long color)
{
    WCHAR buffer[32];
    char narrow[32];
    int index;
    int length = smile_directx_format_number(value, buffer,
        (int)(sizeof(buffer) / sizeof(buffer[0])));
    for (index = 0; index < length; index++)
        narrow[index] = (char)buffer[index];
    smile_directx_draw_text(backend, narrow, length, x, y, size, color, 0);
}

static int smile_directx_present(SmileGraphicsBackend* backend)
{
    SmileDirectXState* state = static_cast<SmileDirectXState*>(backend->state);
    HRESULT result;
    if (state->swap_chain == 0)
        return 1;
    result = smile_directx_end_frame(state);
    if (result == D2DERR_RECREATE_TARGET)
    {
        smile_directx_set_error(state, 0, 0, "Direct2D target recreation", result);
        smile_directx_release_render_target(state);
        if (SUCCEEDED(smile_directx_create_render_target(state)) &&
            SUCCEEDED(smile_directx_create_d2d_target(state)))
            state->device_removal_reason[0] = 0;
        return 0;
    }
    if (FAILED(result))
    {
        smile_directx_set_error(state, 0, 0, "Direct2D EndDraw", result);
        return 0;
    }
    if (state->minimized)
        return 1;
    result = state->swap_chain->Present(state->vsync_enabled ? 1 : 0,
        !state->vsync_enabled && state->tearing_supported ? DXGI_PRESENT_ALLOW_TEARING : 0);
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
    diagnostics->pacing_mode = !state->vsync_enabled && state->tearing_supported
        ? "DXGI low-latency tearing presentation"
        : state->frame_latency_waitable != 0
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
    smile_directx_fill_quadrilateral,
    smile_directx_draw_quadrilateral,
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
