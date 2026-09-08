#ifndef SMILE_GRAPHICS3D_REFLECTIONS_H
#define SMILE_GRAPHICS3D_REFLECTIONS_H

#include <d3d11.h>

enum SmileReflectionFallback3D
{
    SMILE_3D_REFLECTION_FALLBACK_NONE = 0,
    SMILE_3D_REFLECTION_FALLBACK_DISABLED = 1,
    SMILE_3D_REFLECTION_FALLBACK_NO_RECEIVER = 2,
    SMILE_3D_REFLECTION_FALLBACK_CAMERA_BELOW_FLOOR = 3,
    SMILE_3D_REFLECTION_FALLBACK_ALLOCATION_FAILED = 4,
    SMILE_3D_REFLECTION_FALLBACK_RENDER_FAILED = 5,
    SMILE_3D_REFLECTION_FALLBACK_UNSUPPORTED_RECEIVER = 6
};

int smile_reflections_configure(int enabled, int strength_percent,
    int softness_percent, int scale_percent, float floor_height,
    int include_backdrop, int include_vfx);
int smile_reflections_requested(void);
int smile_reflections_strength_percent(void);
int smile_reflections_softness_percent(void);
int smile_reflections_scale_percent(void);
float smile_reflections_floor_height(void);
float smile_reflections_requested_floor_height(void);
void smile_reflections_resolve_floor_height(float floor_height);
int smile_reflections_include_backdrop(void);
int smile_reflections_include_vfx(void);
void smile_reflections_begin_frame(void);
void smile_reflections_skip(int reason);
int smile_reflections_prepare(ID3D11Device* device, int width, int height, int hdr);
ID3D11RenderTargetView* smile_reflections_target(void);
ID3D11DepthStencilView* smile_reflections_depth(void);
ID3D11ShaderResourceView* smile_reflections_shader_view(void);
ID3D11SamplerState* smile_reflections_sampler(void);
int smile_reflections_width(void);
int smile_reflections_height(void);
int smile_reflections_effective(void);
void smile_reflections_complete_capture(long long draws, long long triangles);
void smile_reflections_record_composition(void);
void smile_reflections_record_receiver_sample_error(long long millionths);
long long smile_reflections_value(int index);
void smile_reflections_on_device_lost(void);
void smile_reflections_reset(void);

#endif
