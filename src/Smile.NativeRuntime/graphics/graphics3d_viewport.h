#pragma once
// Native logical viewport placement and preservation of the surrounding 2D/3D frame.
// One logical subviewport; reset is the existing full-window behavior.
static double smile_viewport_region3d[4];
static double smile_3d_viewport_x(void) {
    return smile_graphics_directx_viewport_x() + floor(
        smile_viewport_region3d[0] * smile_graphics_directx_viewport_width() + .5);
}
static double smile_3d_viewport_y(void) {
    return smile_graphics_directx_viewport_y() + floor(
        smile_viewport_region3d[1] * smile_graphics_directx_viewport_height() + .5);
}
static double smile_3d_viewport_width(void) {
    if (smile_viewport_region3d[2] == 0) return smile_graphics_directx_viewport_width();
    double right = smile_graphics_directx_viewport_x() + floor(
        smile_viewport_region3d[2] * smile_graphics_directx_viewport_width() + .5);
    return right > smile_3d_viewport_x() ? right - smile_3d_viewport_x() : 1;
}
static double smile_3d_viewport_height(void) {
    if (smile_viewport_region3d[3] == 0) return smile_graphics_directx_viewport_height();
    double bottom = smile_graphics_directx_viewport_y() + floor(
        smile_viewport_region3d[3] * smile_graphics_directx_viewport_height() + .5);
    return bottom > smile_3d_viewport_y() ? bottom - smile_3d_viewport_y() : 1;
}


static bool smile_viewport_preserve3d;
static bool smile_viewport_saved3d;
static bool smile_depth_from_preserved_viewport3d;
static ID3D11Texture2D* smile_viewport_backup3d;
static D3D11_TEXTURE2D_DESC smile_viewport_desc3d;
// The single active subviewport owns one small finished-color cache.
static ID3D11Texture2D* smile_viewport_cache3d;
static D3D11_TEXTURE2D_DESC smile_viewport_cache_desc3d;
static void* smile_viewport_captures3d[32];
static long long smile_viewport_capture_ids3d[32];
static long long smile_viewport_next_capture3d = 1;

static void smile_3d_viewport_release() {
    for (int i = 0; i < 32; ++i) {
        smile_graphics_directx_release_snapshot(smile_viewport_captures3d[i]);
        smile_viewport_captures3d[i] = nullptr;
        smile_viewport_capture_ids3d[i] = 0;
    }
    if (smile_viewport_backup3d) smile_viewport_backup3d->Release();
    smile_viewport_backup3d = nullptr;
    smile_viewport_saved3d = false;
    if (smile_viewport_cache3d) smile_viewport_cache3d->Release();
    smile_viewport_cache3d = nullptr;
}

static long long smile_3d_capture_viewport() {
    if (!smile_viewport_cache3d || smile_viewport_cache_desc3d.Width > 2048 ||
        smile_viewport_cache_desc3d.Height > 2048) return 0;
    for (int i = 0; i < 32; ++i) {
        if (smile_viewport_captures3d[i]) continue;
        auto bitmap = smile_graphics_directx_snapshot(smile_viewport_cache3d);
        if (!bitmap) return 0;
        smile_viewport_captures3d[i] = bitmap;
        smile_viewport_capture_ids3d[i] = smile_viewport_next_capture3d++;
        return smile_viewport_capture_ids3d[i];
    }
    return 0;
}

static bool smile_3d_draw_capture(long long handle, long long x, long long y,
    long long width, long long height) {
    if (handle <= 0 || width <= 0 || height <= 0 || width > 1000000 || height > 1000000 ||
        x < -1000000 || y < -1000000 || x > 1000000 || y > 1000000) return false;
    for (int i = 0; i < 32; ++i)
        if (smile_viewport_capture_ids3d[i] == handle)
            return smile_graphics_directx_draw_snapshot(smile_viewport_captures3d[i],
                x, y, width, height) != 0;
    return false;
}

static void smile_3d_release_capture(long long handle) {
    if (handle <= 0) return;
    for (int i = 0; i < 32; ++i) {
        if (smile_viewport_capture_ids3d[i] != handle) continue;
        smile_graphics_directx_release_snapshot(smile_viewport_captures3d[i]);
        smile_viewport_captures3d[i] = nullptr;
        smile_viewport_capture_ids3d[i] = 0;
        return;
    }
}

static bool smile_3d_set_viewport(long long x, long long y, long long width,
    long long height, long long window_width, long long window_height, long long preserve) {
    if (preserve < 0 || preserve > 1) return false;
    if (!x && !y && !width && !height && !window_width && !window_height) {
        memset(smile_viewport_region3d, 0, sizeof(smile_viewport_region3d));
        smile_viewport_preserve3d = false;
        return true;
    }
    if (window_width < 1 || window_height < 1 || window_width > 1000000 || window_height > 1000000 ||
        x < 0 || y < 0 || width < 1 || height < 1 || x > window_width || y > window_height ||
        width > window_width - x || height > window_height - y) return false;
    smile_viewport_region3d[0] = (double)x / window_width;
    smile_viewport_region3d[1] = (double)y / window_height;
    smile_viewport_region3d[2] = (double)(x + width) / window_width;
    smile_viewport_region3d[3] = (double)(y + height) / window_height;
    smile_viewport_preserve3d = preserve != 0;
    return true;
}

static bool smile_3d_preserve_viewport(ID3D11DeviceContext* context) {
    smile_viewport_saved3d = false;
    smile_depth_from_preserved_viewport3d = smile_viewport_preserve3d;
    if (!smile_viewport_preserve3d) return true;
    auto target = (ID3D11RenderTargetView*)smile_graphics_directx_render_target();
    ID3D11Resource* resource = nullptr;
    ID3D11Texture2D* texture = nullptr;
    target->GetResource(&resource);
    HRESULT hr = resource->QueryInterface(__uuidof(ID3D11Texture2D), (void**)&texture);
    resource->Release();
    if (FAILED(hr)) return false;
    D3D11_TEXTURE2D_DESC desc;
    texture->GetDesc(&desc);
    if (smile_viewport_backup3d && (desc.Width != smile_viewport_desc3d.Width ||
        desc.Height != smile_viewport_desc3d.Height || desc.Format != smile_viewport_desc3d.Format ||
        desc.SampleDesc.Count != smile_viewport_desc3d.SampleDesc.Count)) smile_3d_viewport_release();
    if (!smile_viewport_backup3d) {
        auto device = (ID3D11Device*)smile_graphics_directx_device();
        smile_viewport_desc3d = desc;
        desc.BindFlags = 0;
        desc.MiscFlags = 0;
        hr = device->CreateTexture2D(&desc, nullptr, &smile_viewport_backup3d);
    }
    if (SUCCEEDED(hr)) {
        context->CopyResource(smile_viewport_backup3d, texture);
        smile_viewport_saved3d = true;
    }
    texture->Release();
    return SUCCEEDED(hr);
}

static void smile_3d_restore_viewport(ID3D11DeviceContext* context) {
    if (!context || !smile_viewport_saved3d) return;
    ID3D11Resource* target = nullptr;
    ((ID3D11RenderTargetView*)smile_graphics_directx_render_target())->GetResource(&target);
    UINT x = (UINT)smile_3d_viewport_x(), y = (UINT)smile_3d_viewport_y();
    UINT right = x + (UINT)smile_3d_viewport_width(), bottom = y + (UINT)smile_3d_viewport_height();
    D3D11_BOX boxes[4] = {
        {0, 0, 0, smile_viewport_desc3d.Width, y, 1},
        {0, bottom, 0, smile_viewport_desc3d.Width, smile_viewport_desc3d.Height, 1},
        {0, y, 0, x, bottom, 1},
        {right, y, 0, smile_viewport_desc3d.Width, bottom, 1}
    };
    context->OMSetRenderTargets(0, nullptr, nullptr);
    D3D11_TEXTURE2D_DESC cache_desc = smile_viewport_desc3d;
    cache_desc.Width = right - x;
    cache_desc.Height = bottom - y;
    cache_desc.BindFlags = cache_desc.MiscFlags = 0;
    if (smile_viewport_cache3d && (cache_desc.Width != smile_viewport_cache_desc3d.Width ||
        cache_desc.Height != smile_viewport_cache_desc3d.Height ||
        cache_desc.Format != smile_viewport_cache_desc3d.Format)) {
        smile_viewport_cache3d->Release();
        smile_viewport_cache3d = nullptr;
    }
    if (!smile_viewport_cache3d) {
        auto device = (ID3D11Device*)smile_graphics_directx_device();
        device->CreateTexture2D(&cache_desc, nullptr, &smile_viewport_cache3d);
        smile_viewport_cache_desc3d = cache_desc;
    }
    if (smile_viewport_cache3d) {
        D3D11_BOX inset = {x, y, 0, right, bottom, 1};
        context->CopySubresourceRegion(smile_viewport_cache3d, 0, 0, 0, 0, target, 0, &inset);
    }
    for (const auto& box : boxes)
        if (box.right > box.left && box.bottom > box.top)
            context->CopySubresourceRegion(target, 0, box.left, box.top, 0, smile_viewport_backup3d, 0, &box);
    target->Release();
    smile_viewport_saved3d = false;
}

static bool smile_3d_replay_viewport(long long x, long long y, long long width,
    long long height, long long window_width, long long window_height) {
    if (!smile_viewport_cache3d || window_width < 1 || window_height < 1 ||
        window_width > 1000000 || window_height > 1000000 || x < 0 || y < 0 ||
        width < 1 || height < 1 || x > window_width || y > window_height ||
        width > window_width - x || height > window_height - y) return false;
    double scale_x = smile_graphics_directx_viewport_width() / window_width;
    double scale_y = smile_graphics_directx_viewport_height() / window_height;
    UINT left = (UINT)(smile_graphics_directx_viewport_x() + floor(x * scale_x + .5));
    UINT top = (UINT)(smile_graphics_directx_viewport_y() + floor(y * scale_y + .5));
    UINT pixel_width = (UINT)(floor((x + width) * scale_x + .5) - floor(x * scale_x + .5));
    UINT pixel_height = (UINT)(floor((y + height) * scale_y + .5) - floor(y * scale_y + .5));
    if (pixel_width != smile_viewport_cache_desc3d.Width ||
        pixel_height != smile_viewport_cache_desc3d.Height) return false;
    smile_graphics_begin_frame();
    if (!smile_graphics_directx_suspend_2d()) return false;
    auto context = (ID3D11DeviceContext*)smile_graphics_directx_context();
    ID3D11Resource* target = nullptr;
    ((ID3D11RenderTargetView*)smile_graphics_directx_render_target())->GetResource(&target);
    context->OMSetRenderTargets(0, nullptr, nullptr);
    context->CopySubresourceRegion(target, 0, left, top, 0, smile_viewport_cache3d, 0, nullptr);
    target->Release();
    smile_graphics_directx_resume_2d();
    return true;
}
