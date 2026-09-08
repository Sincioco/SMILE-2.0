#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <d3d11.h>
#include <math.h>
#include "graphics3d_reflections.h"

template<typename T> static void smile_reflections_release(T*& value)
{
    if (value != 0)
    {
        value->Release();
        value = 0;
    }
}

struct SmileReflectionState3D
{
    int requested;
    int strength_percent;
    int softness_percent;
    int scale_percent;
    float requested_floor_height;
    float effective_floor_height;
    int include_backdrop;
    int include_vfx;
    int effective;
    int fallback_reason;
    int width;
    int height;
    int hdr;
    int configuration_revision;
    int applied_revision;
    int resource_generation;
    long long target_bytes;
    long long capture_count;
    long long draw_count;
    long long triangle_count;
    long long composition_count;
    long long receiver_sample_error_millionths;
    ID3D11Texture2D* color_texture;
    ID3D11RenderTargetView* color_view;
    ID3D11ShaderResourceView* shader_view;
    ID3D11Texture2D* depth_texture;
    ID3D11DepthStencilView* depth_view;
    ID3D11SamplerState* sampler;
    int failed_revision;
    int failed_width;
    int failed_height;
    int failed_hdr;
    int forced_failure_consumed;
};

static SmileReflectionState3D smile_reflections_initial_state(void)
{
    SmileReflectionState3D state = {};
    state.strength_percent = 45;
    state.softness_percent = 35;
    state.scale_percent = 50;
    state.requested_floor_height = -1.0f;
    state.include_backdrop = 1;
    state.fallback_reason = SMILE_3D_REFLECTION_FALLBACK_DISABLED;
    state.configuration_revision = 1;
    state.resource_generation = 1;
    return state;
}

static SmileReflectionState3D smile_reflections3d =
    smile_reflections_initial_state();

static void smile_reflections_release_resources(void)
{
    smile_reflections_release(smile_reflections3d.sampler);
    smile_reflections_release(smile_reflections3d.depth_view);
    smile_reflections_release(smile_reflections3d.depth_texture);
    smile_reflections_release(smile_reflections3d.shader_view);
    smile_reflections_release(smile_reflections3d.color_view);
    smile_reflections_release(smile_reflections3d.color_texture);
    smile_reflections3d.width = 0;
    smile_reflections3d.height = 0;
    smile_reflections3d.hdr = 0;
    smile_reflections3d.target_bytes = 0;
    smile_reflections3d.applied_revision = 0;
}

int smile_reflections_configure(int enabled, int strength_percent,
    int softness_percent, int scale_percent, float floor_height,
    int include_backdrop, int include_vfx)
{
    if (enabled < 0 || enabled > 1 || strength_percent < 0 ||
        strength_percent > 100 || softness_percent < 0 ||
        softness_percent > 100 || scale_percent < 25 || scale_percent > 100 ||
        !isfinite(floor_height) || floor_height < -1000000.0f ||
        floor_height > 1000000.0f || include_backdrop < 0 ||
        include_backdrop > 1 || include_vfx < 0 || include_vfx > 1)
        return 0;
    if (smile_reflections3d.requested == enabled &&
        smile_reflections3d.strength_percent == strength_percent &&
        smile_reflections3d.softness_percent == softness_percent &&
        smile_reflections3d.scale_percent == scale_percent &&
        smile_reflections3d.requested_floor_height == floor_height &&
        smile_reflections3d.include_backdrop == include_backdrop &&
        smile_reflections3d.include_vfx == include_vfx)
        return 1;
    smile_reflections3d.requested = enabled;
    smile_reflections3d.strength_percent = strength_percent;
    smile_reflections3d.softness_percent = softness_percent;
    smile_reflections3d.scale_percent = scale_percent;
    smile_reflections3d.requested_floor_height = floor_height;
    smile_reflections3d.include_backdrop = include_backdrop;
    smile_reflections3d.include_vfx = include_vfx;
    smile_reflections3d.configuration_revision++;
    if (smile_reflections3d.configuration_revision <= 0)
        smile_reflections3d.configuration_revision = 1;
    if (!enabled)
    {
        smile_reflections3d.effective = 0;
        smile_reflections3d.fallback_reason =
            SMILE_3D_REFLECTION_FALLBACK_DISABLED;
    }
    return 1;
}

int smile_reflections_requested(void)
{
    return smile_reflections3d.requested;
}

int smile_reflections_strength_percent(void)
{
    return smile_reflections3d.strength_percent;
}

int smile_reflections_softness_percent(void)
{
    return smile_reflections3d.softness_percent;
}

int smile_reflections_scale_percent(void)
{
    return smile_reflections3d.scale_percent;
}

float smile_reflections_floor_height(void)
{
    return smile_reflections3d.effective_floor_height;
}

float smile_reflections_requested_floor_height(void)
{
    return smile_reflections3d.requested_floor_height;
}

void smile_reflections_resolve_floor_height(float floor_height)
{
    smile_reflections3d.effective_floor_height = floor_height;
}

int smile_reflections_include_backdrop(void)
{
    return smile_reflections3d.include_backdrop;
}

int smile_reflections_include_vfx(void)
{
    return smile_reflections3d.include_vfx;
}

void smile_reflections_begin_frame(void)
{
    smile_reflections3d.effective_floor_height =
        smile_reflections3d.requested_floor_height >= 0.0f
            ? smile_reflections3d.requested_floor_height
            : 0.0f;
    smile_reflections3d.effective = 0;
    smile_reflections3d.fallback_reason = smile_reflections3d.requested
        ? SMILE_3D_REFLECTION_FALLBACK_NO_RECEIVER
        : SMILE_3D_REFLECTION_FALLBACK_DISABLED;
    smile_reflections3d.capture_count = 0;
    smile_reflections3d.draw_count = 0;
    smile_reflections3d.triangle_count = 0;
    smile_reflections3d.composition_count = 0;
    smile_reflections3d.receiver_sample_error_millionths = 0;
}

void smile_reflections_skip(int reason)
{
    smile_reflections3d.effective = 0;
    smile_reflections3d.fallback_reason = reason;
}

int smile_reflections_prepare(ID3D11Device* device, int width, int height, int hdr)
{
    D3D11_TEXTURE2D_DESC color = {};
    D3D11_TEXTURE2D_DESC depth = {};
    D3D11_SAMPLER_DESC sampler_description = {};
    ID3D11Texture2D* color_texture = 0;
    ID3D11RenderTargetView* color_view = 0;
    ID3D11ShaderResourceView* shader_view = 0;
    ID3D11Texture2D* depth_texture = 0;
    ID3D11DepthStencilView* depth_view = 0;
    ID3D11SamplerState* sampler = 0;
    WCHAR forced_failure[8];
    DWORD forced_failure_length;
    HRESULT result;
    int target_width;
    int target_height;
    int longest;
    if (!smile_reflections3d.requested || device == 0 || width <= 0 || height <= 0)
        return 0;
    target_width = width * smile_reflections3d.scale_percent / 100;
    target_height = height * smile_reflections3d.scale_percent / 100;
    if (target_width < 1) target_width = 1;
    if (target_height < 1) target_height = 1;
    longest = target_width > target_height ? target_width : target_height;
    if (longest > 2048)
    {
        target_width = target_width * 2048 / longest;
        target_height = target_height * 2048 / longest;
    }
    if (smile_reflections3d.color_texture != 0 &&
        smile_reflections3d.width == target_width &&
        smile_reflections3d.height == target_height &&
        smile_reflections3d.hdr == hdr)
    {
        smile_reflections3d.applied_revision =
            smile_reflections3d.configuration_revision;
        smile_reflections3d.effective = 1;
        smile_reflections3d.fallback_reason = SMILE_3D_REFLECTION_FALLBACK_NONE;
        return 1;
    }
    if (smile_reflections3d.failed_revision ==
            smile_reflections3d.configuration_revision &&
        smile_reflections3d.failed_width == target_width &&
        smile_reflections3d.failed_height == target_height &&
        smile_reflections3d.failed_hdr == hdr)
    {
        smile_reflections_skip(SMILE_3D_REFLECTION_FALLBACK_ALLOCATION_FAILED);
        return 0;
    }
    forced_failure_length = GetEnvironmentVariableW(
        L"SMILE_TEST_RENDERER3D_FORCE_REFLECTION_FAILURE", forced_failure, 8);
    if (forced_failure_length != 0 &&
        !((forced_failure[0] == L'o' || forced_failure[0] == L'O') &&
            smile_reflections3d.forced_failure_consumed))
    {
        smile_reflections3d.forced_failure_consumed = 1;
        smile_reflections3d.failed_revision =
            smile_reflections3d.configuration_revision;
        smile_reflections3d.failed_width = target_width;
        smile_reflections3d.failed_height = target_height;
        smile_reflections3d.failed_hdr = hdr;
        smile_reflections_skip(SMILE_3D_REFLECTION_FALLBACK_ALLOCATION_FAILED);
        return 0;
    }
    color.Width = (UINT)target_width;
    color.Height = (UINT)target_height;
    color.MipLevels = 1;
    color.ArraySize = 1;
    color.Format = hdr
        ? DXGI_FORMAT_R16G16B16A16_FLOAT
        : DXGI_FORMAT_B8G8R8A8_UNORM;
    color.SampleDesc.Count = 1;
    color.Usage = D3D11_USAGE_DEFAULT;
    color.BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
    result = device->CreateTexture2D(&color, 0, &color_texture);
    if (SUCCEEDED(result))
        result = device->CreateRenderTargetView(color_texture, 0, &color_view);
    if (SUCCEEDED(result))
        result = device->CreateShaderResourceView(color_texture, 0, &shader_view);
    depth = color;
    depth.Format = DXGI_FORMAT_D24_UNORM_S8_UINT;
    depth.BindFlags = D3D11_BIND_DEPTH_STENCIL;
    if (SUCCEEDED(result))
        result = device->CreateTexture2D(&depth, 0, &depth_texture);
    if (SUCCEEDED(result))
        result = device->CreateDepthStencilView(depth_texture, 0, &depth_view);
    sampler_description.Filter = D3D11_FILTER_MIN_MAG_MIP_LINEAR;
    sampler_description.AddressU = D3D11_TEXTURE_ADDRESS_CLAMP;
    sampler_description.AddressV = D3D11_TEXTURE_ADDRESS_CLAMP;
    sampler_description.AddressW = D3D11_TEXTURE_ADDRESS_CLAMP;
    sampler_description.MaxLOD = D3D11_FLOAT32_MAX;
    if (SUCCEEDED(result))
        result = device->CreateSamplerState(&sampler_description, &sampler);
    if (FAILED(result))
    {
        smile_reflections_release(sampler);
        smile_reflections_release(depth_view);
        smile_reflections_release(depth_texture);
        smile_reflections_release(shader_view);
        smile_reflections_release(color_view);
        smile_reflections_release(color_texture);
        smile_reflections3d.failed_revision =
            smile_reflections3d.configuration_revision;
        smile_reflections3d.failed_width = target_width;
        smile_reflections3d.failed_height = target_height;
        smile_reflections3d.failed_hdr = hdr;
        smile_reflections_skip(SMILE_3D_REFLECTION_FALLBACK_ALLOCATION_FAILED);
        return 0;
    }
    smile_reflections_release_resources();
    smile_reflections3d.color_texture = color_texture;
    smile_reflections3d.color_view = color_view;
    smile_reflections3d.shader_view = shader_view;
    smile_reflections3d.depth_texture = depth_texture;
    smile_reflections3d.depth_view = depth_view;
    smile_reflections3d.sampler = sampler;
    smile_reflections3d.width = target_width;
    smile_reflections3d.height = target_height;
    smile_reflections3d.hdr = hdr;
    smile_reflections3d.applied_revision = smile_reflections3d.configuration_revision;
    smile_reflections3d.failed_revision = 0;
    smile_reflections3d.failed_width = 0;
    smile_reflections3d.failed_height = 0;
    smile_reflections3d.failed_hdr = 0;
    smile_reflections3d.resource_generation++;
    if (smile_reflections3d.resource_generation <= 0)
        smile_reflections3d.resource_generation = 1;
    smile_reflections3d.target_bytes =
        (long long)target_width * target_height * (hdr ? 12 : 8);
    smile_reflections3d.effective = 1;
    smile_reflections3d.fallback_reason = SMILE_3D_REFLECTION_FALLBACK_NONE;
    return 1;
}

ID3D11RenderTargetView* smile_reflections_target(void)
{
    return smile_reflections3d.color_view;
}

ID3D11DepthStencilView* smile_reflections_depth(void)
{
    return smile_reflections3d.depth_view;
}

ID3D11ShaderResourceView* smile_reflections_shader_view(void)
{
    return smile_reflections3d.effective ? smile_reflections3d.shader_view : 0;
}

ID3D11SamplerState* smile_reflections_sampler(void)
{
    return smile_reflections3d.effective ? smile_reflections3d.sampler : 0;
}

int smile_reflections_width(void)
{
    return smile_reflections3d.width;
}

int smile_reflections_height(void)
{
    return smile_reflections3d.height;
}

int smile_reflections_effective(void)
{
    return smile_reflections3d.effective;
}

void smile_reflections_complete_capture(long long draws, long long triangles)
{
    smile_reflections3d.capture_count = 1;
    smile_reflections3d.draw_count += draws;
    smile_reflections3d.triangle_count += triangles;
}

void smile_reflections_record_composition(void)
{
    smile_reflections3d.composition_count++;
}

void smile_reflections_record_receiver_sample_error(long long millionths)
{
    smile_reflections3d.receiver_sample_error_millionths = millionths;
}

long long smile_reflections_value(int index)
{
    if (index == 1) return smile_reflections3d.requested;
    if (index == 2) return smile_reflections3d.effective;
    if (index == 3) return smile_reflections3d.fallback_reason;
    if (index == 4) return smile_reflections3d.width;
    if (index == 5) return smile_reflections3d.height;
    if (index == 6) return smile_reflections3d.draw_count;
    if (index == 7) return smile_reflections3d.triangle_count;
    if (index == 8) return smile_reflections3d.capture_count;
    if (index == 9) return smile_reflections3d.composition_count;
    if (index == 10) return smile_reflections3d.configuration_revision;
    if (index == 11) return smile_reflections3d.resource_generation;
    if (index == 12) return smile_reflections3d.target_bytes;
    if (index == 13) return smile_reflections3d.strength_percent;
    if (index == 14) return smile_reflections3d.softness_percent;
    if (index == 15) return smile_reflections3d.scale_percent;
    if (index == 16)
        return (long long)llroundf(smile_reflections3d.requested_floor_height);
    if (index == 17) return smile_reflections3d.include_backdrop;
    if (index == 18)
        return (long long)llroundf(smile_reflections3d.effective_floor_height);
    if (index == 19) return smile_reflections3d.hdr ? 2 :
        (smile_reflections3d.color_texture != 0 ? 1 : 0);
    if (index == 20) return smile_reflections3d.receiver_sample_error_millionths;
    if (index == 21) return smile_reflections3d.include_vfx;
    return 0;
}

void smile_reflections_on_device_lost(void)
{
    smile_reflections_release_resources();
    smile_reflections3d.failed_revision = 0;
    smile_reflections3d.failed_width = 0;
    smile_reflections3d.failed_height = 0;
    smile_reflections3d.failed_hdr = 0;
    smile_reflections3d.effective = 0;
    smile_reflections3d.fallback_reason = smile_reflections3d.requested
        ? SMILE_3D_REFLECTION_FALLBACK_NO_RECEIVER
        : SMILE_3D_REFLECTION_FALLBACK_DISABLED;
}

void smile_reflections_reset(void)
{
    smile_reflections_on_device_lost();
    smile_reflections3d.requested = 0;
    smile_reflections3d.strength_percent = 45;
    smile_reflections3d.softness_percent = 35;
    smile_reflections3d.scale_percent = 50;
    smile_reflections3d.requested_floor_height = -1.0f;
    smile_reflections3d.effective_floor_height = 0.0f;
    smile_reflections3d.include_backdrop = 1;
    smile_reflections3d.include_vfx = 0;
    smile_reflections3d.forced_failure_consumed = 0;
    smile_reflections3d.configuration_revision++;
    if (smile_reflections3d.configuration_revision <= 0)
        smile_reflections3d.configuration_revision = 1;
    smile_reflections_begin_frame();
}
