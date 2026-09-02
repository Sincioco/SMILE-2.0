#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <d3d11.h>
#include <d3dcompiler.h>
#include <float.h>
#include <math.h>
#include <stddef.h>
#include <stdint.h>
#include <string.h>
#include "graphics3d.h"
#include "graphics_common.h"
#include "graphics_directx.h"
#include "image_resource.h"

#define SMILE_3D_MAX_MESHES 128
#define SMILE_3D_MAX_OBJECTS 1024
#define SMILE_3D_MAX_TEXTURES 128
#define SMILE_3D_MAX_MATERIALS 128
#define SMILE_3D_MAX_MODELS 64
#define SMILE_3D_MAX_MODEL_PARTS 16
#define SMILE_3D_MAX_MODEL_VERTICES 131072
#define SMILE_3D_MAX_MODEL_INDICES 393216
#define SMILE_3D_MAX_MODEL_MATERIALS 64
#define SMILE_3D_MAX_MODEL_TEXTURES 128
#define SMILE_3D_MAX_MODEL_CHUNKS 32
#define SMILE_3D_MAX_MODEL_BYTES (16 * 1024 * 1024)
#define SMILE_3D_MAX_SKELETONS 64
#define SMILE_3D_MAX_CLIPS 128
#define SMILE_3D_MAX_ANIMATORS 128
#define SMILE_3D_MAX_BONES 32
#define SMILE_3D_MAX_ANIMATION_EVENTS 16
#define SMILE_3D_MAX_MODEL_ANIMATION_NODES 256
#define SMILE_3D_MAX_MODEL_ANIMATION_BONES 128
#define SMILE_3D_MAX_MODEL_ANIMATION_CLIPS 64
#define SMILE_3D_MAX_MODEL_ANIMATION_SOCKETS 64
#define SMILE_3D_MAX_PENDING_MODEL_EVENTS 32
#define SMILE_3D_MAX_LOCAL_LIGHTS 4
#define SMILE_3D_MAX_FRAME_SUBMISSIONS 512
#define SMILE_3D_MAX_FRAME_PALETTES 512
#define SMILE_3D_MAX_PARTICLE_BATCHES 16
#define SMILE_3D_MAX_PARTICLES_PER_BATCH 4096
#define SMILE_3D_MAX_STAGED_PARTICLES 8192
#define SMILE_3D_MAX_RIBBON_BATCHES 16
#define SMILE_3D_MAX_RIBBON_POINTS_PER_BATCH 1024
#define SMILE_3D_MAX_STAGED_RIBBON_POINTS 2048
#define SMILE_3D_SUBMISSION_OBJECT 1
#define SMILE_3D_SUBMISSION_PARTICLE_BATCH 2
#define SMILE_3D_SUBMISSION_RIBBON_BATCH 3
#define SMILE_3D_SUBMISSION_SNAPSHOT_BYTES 512
#define SMILE_3D_PALETTE_SNAPSHOT_BYTES 8208
#define SMILE_3D_M5_FALLBACK_SHADOW_RESOLUTION_REDUCED 1
#define SMILE_3D_M5_FALLBACK_SHADOW_DISABLED 2
#define SMILE_3D_M5_FALLBACK_HDR_UNAVAILABLE 4
#define SMILE_3D_M5_FALLBACK_MSAA_REDUCED 8
#define SMILE_3D_M5_FALLBACK_BLOOM_REDUCED 16
#define SMILE_3D_M5_FALLBACK_BLOOM_DISABLED 32
#define SMILE_3D_M5_FALLBACK_TONE_MAPPING_DISABLED 64
#define SMILE_3D_M5_FALLBACK_DIRECT_LDR 128
#define SMILE_3D_MESH_HANDLE 0x10000000LL
#define SMILE_3D_OBJECT_HANDLE 0x20000000LL
#define SMILE_3D_TEXTURE_HANDLE 0x30000000LL
#define SMILE_3D_MATERIAL_HANDLE 0x40000000LL
#define SMILE_3D_MODEL_HANDLE 0x50000000LL
#define SMILE_3D_SKELETON_HANDLE 0x60000000LL
#define SMILE_3D_CLIP_HANDLE 0x70000000LL
#define SMILE_3D_ANIMATOR_HANDLE 0x80000000LL
#define SMILE_3D_PARTICLE_BATCH_HANDLE 0x90000000LL
#define SMILE_3D_RIBBON_BATCH_HANDLE 0xA0000000LL
#define SMILE_3D_HANDLE_KIND 0xF0000000LL
#define SMILE_3D_PI 3.14159265358979323846f
#define SMILE_3D_PBR_PIPELINE_NOT_ATTEMPTED 0
#define SMILE_3D_PBR_PIPELINE_AVAILABLE 1
#define SMILE_3D_PBR_PIPELINE_UNAVAILABLE 2

extern "C" int smile_resolve_asset_path_utf8(const char* path, long long length,
    WCHAR* resolved_path, int capacity);

struct SmileVertex3D
{
    float x, y, z;
    float nx, ny, nz;
    float u, v;
    float joints[4];
    float weights[4];
    float tangent[4];
};

struct SmileMesh3D
{
    unsigned short generation;
    unsigned char active;
    unsigned char committed;
    unsigned char explicit_normals;
    unsigned char max_joint;
    unsigned int vertex_count;
    unsigned int index_count;
    SmileVertex3D* vertices;
    unsigned int* indices;
    ID3D11Buffer* vertex_buffer;
    ID3D11Buffer* index_buffer;
    unsigned int in_flight;
};

struct SmileObject3D
{
    unsigned short generation;
    unsigned char active;
    unsigned char visible;
    unsigned char casts_shadow;
    unsigned char receives_shadow;
    long long mesh_handle;
    long long material_handle;
    long long default_material_handle;
    long long animator_handle;
    float position[3];
    float rotation[3];
    float scale[3];
    float color[4];
};

struct SmileTexture3D
{
    unsigned short generation;
    unsigned char active;
    unsigned char filter;
    unsigned char wrap;
    unsigned char pbr;
    unsigned char semantic;
    unsigned char requested_anisotropy;
    unsigned char effective_anisotropy;
    unsigned char mip_levels;
    SmileImageResource* image;
    ID3D11Texture2D* texture;
    ID3D11ShaderResourceView* view;
    ID3D11SamplerState* sampler;
    unsigned int in_flight;
};

struct SmileMaterial3D
{
    unsigned short generation;
    unsigned char active;
    unsigned char alpha_mode;
    unsigned char unlit;
    unsigned char mode;
    unsigned char double_sided;
    long long texture_handles[4];
    long long owner_model_handle;
    float color[4];
    float emissive;
    float cutoff;
    float metallic;
    float roughness;
    float normal_strength;
    float occlusion_strength;
    float emissive_color[3];
};

struct SmileModelChunkV2
{
    unsigned int id;
    unsigned int flags;
    unsigned int offset;
    unsigned int length;
    unsigned int count;
    unsigned int stride;
};

struct SmileModel3D
{
    unsigned short generation;
    unsigned char active;
    unsigned char part_count;
    unsigned short material_count;
    unsigned short texture_count;
    unsigned char format_version;
    unsigned char prepared_pbr;
    unsigned char pbr_failure;
    unsigned int vertex_count;
    unsigned int index_count;
    unsigned int model_name_offset;
    unsigned int string_bytes;
    char* strings;
    long long mesh_handles[SMILE_3D_MAX_MODEL_PARTS];
    unsigned short material_slots[SMILE_3D_MAX_MODEL_PARTS];
    unsigned int part_name_offsets[SMILE_3D_MAX_MODEL_PARTS];
    float bounds[6];
    float part_bounds[SMILE_3D_MAX_MODEL_PARTS][6];
    struct
    {
        unsigned int name_offset;
        int texture_references[4];
        unsigned char alpha_mode;
        unsigned char double_sided;
        float base_color[4];
        float metallic;
        float roughness;
        float normal_strength;
        float occlusion_strength;
        float emissive[3];
        float alpha_cutoff;
    } materials[SMILE_3D_MAX_MODEL_MATERIALS];
    struct
    {
        unsigned int path_offset;
        unsigned char semantic;
    } textures[SMILE_3D_MAX_MODEL_TEXTURES];
    unsigned char prepared_material_count;
    unsigned char prepared_reference_count;
    unsigned char owned_texture_count;
    long long prepared_material_handles[SMILE_3D_MAX_MODEL_MATERIALS];
    long long prepared_texture_by_reference[SMILE_3D_MAX_MODEL_TEXTURES];
    long long owned_texture_handles[SMILE_3D_MAX_MODEL_TEXTURES];
    unsigned char has_animation;
    unsigned char animation_bone_count;
    unsigned char animation_clip_count;
    unsigned char animation_socket_count;
    unsigned short animation_node_count;
    unsigned short animation_event_count;
    unsigned int animation_bytes;
    unsigned int animation_file_bytes;
    unsigned int animation_resident_bytes;
    unsigned char* animation_data;
    SmileModelChunkV2 animation_chunks[9];
};

struct SmileModelPartV2
{
    unsigned int name_offset;
    unsigned int first_vertex;
    unsigned int vertex_count;
    unsigned int first_index;
    unsigned int index_count;
    unsigned int material;
    unsigned int bounds_index;
};

struct SmileModelMaterialV2
{
    unsigned int name_offset;
    int texture_references[4];
    unsigned char alpha_mode;
    unsigned char double_sided;
    float base_color[4];
    float metallic;
    float roughness;
    float normal_strength;
    float occlusion_strength;
    float emissive[3];
    float alpha_cutoff;
};

struct SmileModelTextureV2
{
    unsigned int path_offset;
    unsigned char semantic;
};

struct SmileSkeleton3D
{
    unsigned short generation;
    unsigned char active;
    unsigned char committed;
    unsigned char bone_count;
    signed char parents[SMILE_3D_MAX_BONES];
    float bind_translation[SMILE_3D_MAX_BONES][3];
    float inverse_bind_translation[SMILE_3D_MAX_BONES][3];
};

struct SmileAnimationClip3D
{
    unsigned short generation;
    unsigned char active;
    unsigned char event_count;
    unsigned char pbr_scale_safe;
    long long skeleton_handle;
    unsigned int duration_ms;
    unsigned char translation_tracks[SMILE_3D_MAX_BONES];
    unsigned char rotation_tracks[SMILE_3D_MAX_BONES];
    unsigned char scale_tracks[SMILE_3D_MAX_BONES];
    float translation[SMILE_3D_MAX_BONES][2][3];
    float rotation[SMILE_3D_MAX_BONES][2][4];
    float scale[SMILE_3D_MAX_BONES][2][3];
    unsigned int event_time[SMILE_3D_MAX_ANIMATION_EVENTS];
    unsigned int event_id[SMILE_3D_MAX_ANIMATION_EVENTS];
};

struct SmileMatrix3D { float m[16]; };

struct SmileAnimator3D
{
    unsigned short generation;
    unsigned char active;
    unsigned char loop;
    unsigned char complete;
    long long skeleton_handle;
    long long clip_handle;
    unsigned int time_ms;
    unsigned int previous_time_ms;
    unsigned int speed_percent;
    unsigned int pending_event;
    unsigned char model_animation;
    unsigned char playback_mode;
    unsigned char destination_mode;
    unsigned char root_motion_mode;
    signed char clip_index;
    signed char destination_clip;
    unsigned char event_head;
    unsigned char event_count;
    long long model_handle;
    unsigned int destination_time_ms;
    unsigned int time_remainder;
    unsigned int destination_time_remainder;
    unsigned int fade_elapsed_ms;
    unsigned int fade_duration_ms;
    unsigned int pose_revision;
    unsigned int dropped_event_count;
    unsigned char destination_complete;
    unsigned char event_overflowed;
    unsigned int pending_events[SMILE_3D_MAX_PENDING_MODEL_EVENTS];
    float root_delta[4];
    SmileMatrix3D node_global[SMILE_3D_MAX_MODEL_ANIMATION_NODES];
    SmileMatrix3D bones[SMILE_3D_MAX_MODEL_ANIMATION_BONES];
};

struct SmilePaletteSnapshot3D
{
    long long animator_handle;
    unsigned int pose_revision;
    unsigned char mode;
    unsigned char bone_count;
    SmileMatrix3D bones[SMILE_3D_MAX_MODEL_ANIMATION_BONES];
};

struct SmileSubmission3D
{
    unsigned char kind;
    unsigned char visible;
    unsigned char casts_shadow;
    unsigned char receives_shadow;
    unsigned char has_material;
    unsigned char animation_mode;
    unsigned char alpha_mode;
    unsigned char double_sided;
    long long source_handle;
    long long mesh_handle;
    long long texture_handles[4];
    unsigned int resource_revision;
    int palette_index;
    SmileObject3D object;
    SmileMaterial3D material;
};

struct SmileParticleInstance3D
{
    float position_size[4];
    float color[4];
    float rotation_uv[4];
};

struct SmileParticleBatch3D
{
    unsigned short generation;
    unsigned char active;
    unsigned char billboard_mode;
    unsigned short atlas_columns;
    unsigned short atlas_rows;
    unsigned int capacity;
    unsigned int count;
    unsigned int staging_revision;
    unsigned int revision;
    unsigned int uploaded_revision;
    unsigned int in_flight;
    long long material_handle;
    SmileParticleInstance3D* instances;
    SmileParticleInstance3D* committed_instances;
    ID3D11Buffer* instance_buffer;
};

struct SmileRibbonPoint3D
{
    float left[3];
    float right[3];
    float color[4];
    float u;
};

struct SmileRibbonVertex3D
{
    float position[3];
    float uv[2];
    float color[4];
};

struct SmileRibbonBatch3D
{
    unsigned short generation;
    unsigned char active;
    unsigned int capacity;
    unsigned int count;
    unsigned int staging_revision;
    unsigned int revision;
    unsigned int uploaded_revision;
    unsigned int in_flight;
    long long material_handle;
    SmileRibbonPoint3D* points;
    SmileRibbonVertex3D* staging_vertices;
    SmileRibbonVertex3D* vertices;
    ID3D11Buffer* vertex_buffer;
};

struct SmileVfxConstants3D
{
    SmileMatrix3D view_projection;
    float camera_right[4];
    float camera_up[4];
    float atlas_output[4];
    float material[4];
};

struct SmileConstants3D
{
    SmileMatrix3D model;
    SmileMatrix3D mvp;
    float color[4];
    float material[4];
    float animation[4];
    SmileMatrix3D shadow_mvp;
    float shadow[4];
    float output[4];
    float shadow_light[4];
    SmileMatrix3D bones[SMILE_3D_MAX_BONES];
};

struct SmilePbrConstants3D
{
    SmileMatrix3D model;
    SmileMatrix3D mvp;
    SmileMatrix3D normal_matrix;
    float object_color[4];
    float base_factor[4];
    float surface_factors[4];
    float emissive_alpha[4];
    float texture_flags[4];
    float camera_position[4];
    float ambient[4];
    float directional_direction[4];
    float directional_color[4];
    float local_position_type[SMILE_3D_MAX_LOCAL_LIGHTS][4];
    float local_direction_range[SMILE_3D_MAX_LOCAL_LIGHTS][4];
    float local_color_intensity[SMILE_3D_MAX_LOCAL_LIGHTS][4];
    float local_cone[SMILE_3D_MAX_LOCAL_LIGHTS][4];
    float animation[4];
    SmileMatrix3D shadow_mvp;
    float shadow[4];
    float output[4];
    SmileMatrix3D bones[SMILE_3D_MAX_BONES];
};

struct SmileShadowConstants3D
{
    SmileMatrix3D mvp;
    float alpha[4];
    float animation[4];
    SmileMatrix3D bones[SMILE_3D_MAX_BONES];
};

struct SmilePostConstants3D
{
    float first[4];
    float second[4];
};

struct SmileDirectionalLight3D
{
    unsigned char enabled;
    float direction[3];
    float color[3];
    float intensity;
};

struct SmileLocalLight3D
{
    unsigned char type;
    float position[3];
    float direction[3];
    float color[3];
    float intensity;
    float range;
    float inner_cosine;
    float outer_cosine;
};

static SmileMesh3D smile_meshes3d[SMILE_3D_MAX_MESHES];
static SmileObject3D smile_objects3d[SMILE_3D_MAX_OBJECTS];
static SmileTexture3D smile_textures3d[SMILE_3D_MAX_TEXTURES];
static SmileMaterial3D smile_materials3d[SMILE_3D_MAX_MATERIALS];
static SmileModel3D smile_models3d[SMILE_3D_MAX_MODELS];
static SmileSkeleton3D smile_skeletons3d[SMILE_3D_MAX_SKELETONS];
static SmileAnimationClip3D smile_clips3d[SMILE_3D_MAX_CLIPS];
static SmileAnimator3D smile_animators3d[SMILE_3D_MAX_ANIMATORS];
static SmileParticleBatch3D smile_particle_batches3d[SMILE_3D_MAX_PARTICLE_BATCHES];
static SmileRibbonBatch3D smile_ribbon_batches3d[SMILE_3D_MAX_RIBBON_BATCHES];
static ID3D11VertexShader* smile_vertex_shader3d;
static ID3D11PixelShader* smile_pixel_shader3d;
static ID3D11InputLayout* smile_input_layout3d;
static ID3D11Buffer* smile_constant_buffer3d;
static ID3D11VertexShader* smile_pbr_vertex_shader3d;
static ID3D11PixelShader* smile_pbr_pixel_shader3d;
static ID3D11InputLayout* smile_pbr_input_layout3d;
static ID3D11Buffer* smile_pbr_constant_buffer3d;
static ID3D11Buffer* smile_model_palette_buffer3d;
static ID3D11VertexShader* smile_shadow_vertex_shader3d;
static ID3D11PixelShader* smile_shadow_pixel_shader3d;
static ID3D11InputLayout* smile_shadow_input_layout3d;
static ID3D11Buffer* smile_shadow_constant_buffer3d;
static ID3D11Texture2D* smile_shadow_texture3d;
static ID3D11DepthStencilView* smile_shadow_depth_view3d;
static ID3D11ShaderResourceView* smile_shadow_shader_view3d;
static ID3D11SamplerState* smile_shadow_sampler3d;
static ID3D11RasterizerState* smile_shadow_raster_state3d;
static ID3D11RasterizerState* smile_shadow_double_raster_state3d;
static ID3D11VertexShader* smile_post_vertex_shader3d;
static ID3D11PixelShader* smile_post_pixel_shader3d;
static ID3D11Buffer* smile_post_constant_buffer3d;
static ID3D11SamplerState* smile_post_sampler3d;
static ID3D11VertexShader* smile_particle_vertex_shader3d;
static ID3D11VertexShader* smile_ribbon_vertex_shader3d;
static ID3D11PixelShader* smile_vfx_pixel_shader3d;
static ID3D11InputLayout* smile_particle_input_layout3d;
static ID3D11InputLayout* smile_ribbon_input_layout3d;
static ID3D11Buffer* smile_vfx_constant_buffer3d;
static ID3D11Buffer* smile_particle_quad_vertex_buffer3d;
static ID3D11Buffer* smile_particle_quad_index_buffer3d;
static ID3D11Texture2D* smile_scene_resolve_texture3d;
static ID3D11ShaderResourceView* smile_scene_shader_view3d;
static ID3D11Texture2D* smile_bloom_texture_a3d;
static ID3D11RenderTargetView* smile_bloom_view_a3d;
static ID3D11ShaderResourceView* smile_bloom_shader_a3d;
static ID3D11Texture2D* smile_bloom_texture_b3d;
static ID3D11RenderTargetView* smile_bloom_view_b3d;
static ID3D11ShaderResourceView* smile_bloom_shader_b3d;
static ID3D11DepthStencilState* smile_depth_state3d;
static ID3D11DepthStencilState* smile_depth_read_state3d;
static ID3D11RasterizerState* smile_raster_state3d;
static ID3D11RasterizerState* smile_cull_raster_state3d;
static ID3D11BlendState* smile_blend_state3d;
static ID3D11BlendState* smile_additive_blend_state3d;
static ID3D11Texture2D* smile_color_texture3d;
static ID3D11RenderTargetView* smile_color_view3d;
static ID3D11Texture2D* smile_depth_texture3d;
static ID3D11DepthStencilView* smile_depth_view3d;
static int smile_target_width3d;
static int smile_target_height3d;
static UINT smile_sample_count3d = 1;
static UINT smile_sample_quality3d;
static int smile_frame_active3d;
static long long smile_resource_epoch3d = 1;
static int smile_last_error3d;
static long long smile_draw_call_count3d;
static long long smile_submitted_triangle_count3d;
static long long smile_pbr_draw_count3d;
static int smile_material_inspection3d;
static long long smile_simple_draw_count3d;
static long long smile_pbr_triangle_count3d;
static int smile_pbr_shader_available3d;
static int smile_pbr_pipeline_state3d;
static int smile_pbr_pipeline_failure3d;
static long long smile_pbr_pipeline_attempt_count3d;
static long long smile_model_palette_upload_count3d;
static long long smile_model_palette_cached_animator3d;
static unsigned int smile_model_palette_cached_revision3d;
static float smile_camera_position3d[3] = { 0.0f, 300.0f, -800.0f };
static float smile_camera_target3d[3] = { 0.0f, 0.0f, 0.0f };
static float smile_camera_up3d[3] = { 0.0f, 1.0f, 0.0f };
static float smile_camera_fov3d = 55.0f;
static float smile_camera_near3d = 1.0f;
static float smile_camera_far3d = 10000.0f;
static float smile_pending_camera_position3d[3];
static float smile_pending_camera_target3d[3];
static float smile_pending_camera_up3d[3];
static float smile_pending_camera_fov3d;
static float smile_pending_camera_near3d;
static float smile_pending_camera_far3d;
static int smile_pending_camera_has_projection3d;
static int smile_pending_camera_has_up3d;
static float smile_ambient_color3d[3] = { 1.0f, 1.0f, 1.0f };
static float smile_ambient_intensity3d = 0.25f;
static SmileDirectionalLight3D smile_directional_light3d = {
    1, { -0.35f, 0.8f, -0.45f }, { 1.0f, 1.0f, 1.0f }, 1.0f
};
static SmileLocalLight3D smile_local_lights3d[SMILE_3D_MAX_LOCAL_LIGHTS];
static unsigned int smile_max_anisotropy3d = 16;
static SmileSubmission3D smile_frame_submissions3d[SMILE_3D_MAX_FRAME_SUBMISSIONS];
static SmilePaletteSnapshot3D smile_frame_palettes3d[SMILE_3D_MAX_FRAME_PALETTES];
static unsigned int smile_frame_submission_count3d;
static unsigned int smile_frame_palette_count3d;
static unsigned int smile_submission_group_start3d;
static unsigned int smile_submission_group_palette_start3d;
static unsigned int smile_submission_group_reserved3d;
static unsigned int smile_submission_group_physical3d;
static unsigned int smile_submission_group_logical3d;
static int smile_submission_group_active3d;
static long long smile_submission_group_token3d;
static long long smile_submission_group_serial3d;
static long long smile_logical_submission_count3d;
static long long smile_physical_submission_count3d;
static long long smile_rejected_submission_count3d;
static long long smile_shadow_draw_count3d;
static long long smile_shadow_triangle_count3d;
static long long smile_shadow_palette_upload_count3d;
static long long smile_post_draw_count3d;
static long long smile_resolve_count3d;
static int smile_multipass_active3d;
static int smile_post_requested3d;
static int smile_hdr_requested3d;
static int smile_bloom_requested3d;
static int smile_shadow_requested3d;
static int smile_post_effective3d;
static int smile_hdr_effective3d;
static int smile_bloom_effective3d;
static int smile_shadow_effective3d;
static int smile_tone_mapping_effective3d;
static int smile_shadow_caster3d = 1;
static int smile_shadow_slot3d;
static int smile_shadow_requested_resolution3d = 2048;
static int smile_shadow_resolution3d;
static int smile_exposure_percent3d = 100;
static int smile_bloom_threshold3d = 1200;
static int smile_bloom_intensity3d = 80;
static int smile_bloom_downsample3d = 2;
static int smile_bloom_cycles3d = 2;
static int smile_requested_sample_count3d = 4;
static int smile_bloom_width3d;
static int smile_bloom_height3d;
static int smile_m5_fallback_flags3d;
static int smile_m5_resource_generation3d = 1;
static int smile_m5_configuration_revision3d = 1;
static int smile_m5_applied_revision3d;
static int smile_shadow_applied_revision3d;
static int smile_m5_target_width3d;
static int smile_m5_target_height3d;
static long long smile_m5_target_bytes3d;
static long long smile_shadow_bytes3d;
static long long smile_scene_bytes3d;
static long long smile_bloom_bytes3d;
static unsigned int smile_staged_particle_capacity3d;
static unsigned int smile_staged_ribbon_capacity3d;
static long long smile_vfx_draw_count3d;
static long long smile_vfx_triangle_count3d;
static long long smile_vfx_upload_count3d;
static long long smile_vfx_rejected_operation_count3d;
static long long smile_vfx_particle_draw_count3d;
static long long smile_vfx_ribbon_draw_count3d;
static long long smile_vfx_particle_triangle_count3d;
static long long smile_vfx_ribbon_triangle_count3d;
static long long smile_vfx_particle_submission_count3d;
static long long smile_vfx_ribbon_submission_count3d;

#define SMILE_3D_CAMERA_WORLD_BOUND 1000000LL
#define SMILE_3D_CAMERA_ERROR_INVALID_POSITION_TARGET 58
#define SMILE_3D_CAMERA_ERROR_ZERO_VIEW_DIRECTION 59
#define SMILE_3D_CAMERA_ERROR_INVALID_PROJECTION 60
#define SMILE_3D_CAMERA_ERROR_INVALID_UP 61
#define SMILE_3D_CAMERA_ERROR_PARALLEL_UP 62
#define SMILE_3D_CAMERA_ERROR_PENDING_INCOMPLETE 63
#define SMILE_3D_CAMERA_ERROR_FRAME_ACTIVE 64

struct SmileM5TargetState3D
{
    ID3D11Texture2D* color_texture;
    ID3D11RenderTargetView* color_view;
    ID3D11Texture2D* depth_texture;
    ID3D11DepthStencilView* depth_view;
    ID3D11Texture2D* resolve_texture;
    ID3D11ShaderResourceView* scene_view;
    ID3D11Texture2D* bloom_texture_a;
    ID3D11RenderTargetView* bloom_view_a;
    ID3D11ShaderResourceView* bloom_shader_a;
    ID3D11Texture2D* bloom_texture_b;
    ID3D11RenderTargetView* bloom_view_b;
    ID3D11ShaderResourceView* bloom_shader_b;
    int target_width;
    int target_height;
    int m5_width;
    int m5_height;
    int bloom_width;
    int bloom_height;
    UINT sample_count;
    UINT sample_quality;
    int post_effective;
    int hdr_effective;
    int bloom_effective;
    int tone_effective;
    long long target_bytes;
    long long scene_bytes;
    long long bloom_bytes;
};
static float smile_shadow_center3d[3] = { 0.0f, 100.0f, 0.0f };
static float smile_shadow_width3d = 1200.0f;
static float smile_shadow_height3d = 900.0f;
static float smile_shadow_near3d = 1.0f;
static float smile_shadow_far3d = 2400.0f;
static float smile_shadow_bias3d = 0.0015f;
static float smile_shadow_normal_bias3d = 0.006f;
static float smile_frame_clear3d[4];
static SmileMatrix3D smile_shadow_view_projection3d;

static SmileMatrix3D smile_3d_identity(void);
static unsigned int smile_3d_model_v2_string_hash(const SmileModel3D* model, unsigned int offset);
static void smile_3d_delete_texture(SmileTexture3D* texture);
static void smile_3d_delete_material(SmileMaterial3D* material);
static int smile_3d_clear_model_pbr(SmileModel3D* model);
static int smile_3d_create_pipeline(void);
static int smile_3d_prepare_model_pbr(long long model_handle,
    long long filter, long long wrap, long long anisotropy);
static int smile_3d_prepare_m5_resources(void);
static int smile_3d_draw_immediate(long long handle);
static int smile_3d_render_shadow_pass(void);
static int smile_3d_run_post_processing(void);

template<typename T> static void smile_3d_release(T*& value)
{
    if (value != 0) value->Release();
    value = 0;
}

static void* smile_3d_allocate(SIZE_T bytes)
{
    if (bytes == 0) return 0;
    return HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, bytes);
}

static void smile_3d_free(void*& value)
{
    if (value != 0) HeapFree(GetProcessHeap(), 0, value);
    value = 0;
}

static float smile_3d_degrees(long long degrees)
{
    return (float)degrees * SMILE_3D_PI / 180.0f;
}

static long long smile_3d_handle(long long kind, int slot, unsigned short generation)
{
    return kind | ((long long)generation << 8) | (long long)(slot + 1);
}

static long long smile_3d_object_handle(int slot, unsigned short generation)
{
    /* Objects own a 1,024-entry pool, so their generation-safe handle reserves
       ten low bits for the zero-based slot. Other resource pools remain at 128 or fewer and
       retain their existing eight-bit layout. */
    return SMILE_3D_OBJECT_HANDLE | ((long long)generation << 10) | (long long)slot;
}

static SmileMesh3D* smile_3d_mesh(long long handle)
{
    int slot;
    unsigned short generation;
    if ((handle & SMILE_3D_HANDLE_KIND) != SMILE_3D_MESH_HANDLE) return 0;
    slot = (int)(handle & 255LL) - 1;
    generation = (unsigned short)((handle >> 8) & 65535LL);
    if (slot < 0 || slot >= SMILE_3D_MAX_MESHES || !smile_meshes3d[slot].active ||
        smile_meshes3d[slot].generation != generation) return 0;
    return &smile_meshes3d[slot];
}

static SmileObject3D* smile_3d_object(long long handle)
{
    int slot;
    unsigned short generation;
    if ((handle & SMILE_3D_HANDLE_KIND) != SMILE_3D_OBJECT_HANDLE) return 0;
    slot = (int)(handle & 1023LL);
    generation = (unsigned short)((handle >> 10) & 65535LL);
    if (slot < 0 || slot >= SMILE_3D_MAX_OBJECTS || !smile_objects3d[slot].active ||
        smile_objects3d[slot].generation != generation) return 0;
    return &smile_objects3d[slot];
}

static SmileTexture3D* smile_3d_texture(long long handle)
{
    int slot;
    unsigned short generation;
    if ((handle & SMILE_3D_HANDLE_KIND) != SMILE_3D_TEXTURE_HANDLE) return 0;
    slot = (int)(handle & 255LL) - 1;
    generation = (unsigned short)((handle >> 8) & 65535LL);
    if (slot < 0 || slot >= SMILE_3D_MAX_TEXTURES || !smile_textures3d[slot].active ||
        smile_textures3d[slot].generation != generation) return 0;
    return &smile_textures3d[slot];
}

static SmileMaterial3D* smile_3d_material(long long handle)
{
    int slot;
    unsigned short generation;
    if ((handle & SMILE_3D_HANDLE_KIND) != SMILE_3D_MATERIAL_HANDLE) return 0;
    slot = (int)(handle & 255LL) - 1;
    generation = (unsigned short)((handle >> 8) & 65535LL);
    if (slot < 0 || slot >= SMILE_3D_MAX_MATERIALS || !smile_materials3d[slot].active ||
        smile_materials3d[slot].generation != generation) return 0;
    return &smile_materials3d[slot];
}

static SmileModel3D* smile_3d_model_resource(long long handle)
{
    int slot;
    unsigned short generation;
    if ((handle & SMILE_3D_HANDLE_KIND) != SMILE_3D_MODEL_HANDLE) return 0;
    slot = (int)(handle & 255LL) - 1;
    generation = (unsigned short)((handle >> 8) & 65535LL);
    if (slot < 0 || slot >= SMILE_3D_MAX_MODELS || !smile_models3d[slot].active ||
        smile_models3d[slot].generation != generation) return 0;
    return &smile_models3d[slot];
}

static SmileSkeleton3D* smile_3d_skeleton(long long handle)
{
    int slot;
    unsigned short generation;
    if ((handle & SMILE_3D_HANDLE_KIND) != SMILE_3D_SKELETON_HANDLE) return 0;
    slot = (int)(handle & 255LL) - 1;
    generation = (unsigned short)((handle >> 8) & 65535LL);
    if (slot < 0 || slot >= SMILE_3D_MAX_SKELETONS || !smile_skeletons3d[slot].active ||
        smile_skeletons3d[slot].generation != generation) return 0;
    return &smile_skeletons3d[slot];
}

static SmileAnimationClip3D* smile_3d_clip(long long handle)
{
    int slot;
    unsigned short generation;
    if ((handle & SMILE_3D_HANDLE_KIND) != SMILE_3D_CLIP_HANDLE) return 0;
    slot = (int)(handle & 255LL) - 1;
    generation = (unsigned short)((handle >> 8) & 65535LL);
    if (slot < 0 || slot >= SMILE_3D_MAX_CLIPS || !smile_clips3d[slot].active ||
        smile_clips3d[slot].generation != generation) return 0;
    return &smile_clips3d[slot];
}

static SmileAnimator3D* smile_3d_animator(long long handle)
{
    int slot;
    unsigned short generation;
    if ((handle & SMILE_3D_HANDLE_KIND) != SMILE_3D_ANIMATOR_HANDLE) return 0;
    slot = (int)(handle & 255LL) - 1;
    generation = (unsigned short)((handle >> 8) & 65535LL);
    if (slot < 0 || slot >= SMILE_3D_MAX_ANIMATORS || !smile_animators3d[slot].active ||
        smile_animators3d[slot].generation != generation) return 0;
    return &smile_animators3d[slot];
}

static SmileParticleBatch3D* smile_3d_particle_batch(long long handle)
{
    int slot;
    unsigned short generation;
    if ((handle & SMILE_3D_HANDLE_KIND) != SMILE_3D_PARTICLE_BATCH_HANDLE) return 0;
    slot = (int)(handle & 255LL) - 1;
    generation = (unsigned short)((handle >> 8) & 65535LL);
    if (slot < 0 || slot >= SMILE_3D_MAX_PARTICLE_BATCHES ||
        !smile_particle_batches3d[slot].active ||
        smile_particle_batches3d[slot].generation != generation) return 0;
    return &smile_particle_batches3d[slot];
}

static SmileRibbonBatch3D* smile_3d_ribbon_batch(long long handle)
{
    int slot;
    unsigned short generation;
    if ((handle & SMILE_3D_HANDLE_KIND) != SMILE_3D_RIBBON_BATCH_HANDLE) return 0;
    slot = (int)(handle & 255LL) - 1;
    generation = (unsigned short)((handle >> 8) & 65535LL);
    if (slot < 0 || slot >= SMILE_3D_MAX_RIBBON_BATCHES ||
        !smile_ribbon_batches3d[slot].active ||
        smile_ribbon_batches3d[slot].generation != generation) return 0;
    return &smile_ribbon_batches3d[slot];
}

static int smile_3d_live_mesh_count(void)
{
    int count = 0;
    int index;
    for (index = 0; index < SMILE_3D_MAX_MESHES; ++index)
        if (smile_meshes3d[index].active) count++;
    return count;
}

static int smile_3d_live_object_count(void)
{
    int count = 0;
    int index;
    for (index = 0; index < SMILE_3D_MAX_OBJECTS; ++index)
        if (smile_objects3d[index].active) count++;
    return count;
}

static int smile_3d_live_texture_count(void)
{
    int count = 0;
    int index;
    for (index = 0; index < SMILE_3D_MAX_TEXTURES; ++index)
        if (smile_textures3d[index].active) count++;
    return count;
}

static int smile_3d_live_material_count(void)
{
    int count = 0;
    int index;
    for (index = 0; index < SMILE_3D_MAX_MATERIALS; ++index)
        if (smile_materials3d[index].active) count++;
    return count;
}

static int smile_3d_live_model_count(void)
{
    int count = 0;
    int index;
    for (index = 0; index < SMILE_3D_MAX_MODELS; ++index)
        if (smile_models3d[index].active) count++;
    return count;
}

static int smile_3d_live_skeleton_count(void)
{
    int count = 0;
    for (int index = 0; index < SMILE_3D_MAX_SKELETONS; ++index)
        if (smile_skeletons3d[index].active) count++;
    return count;
}

static int smile_3d_live_clip_count(void)
{
    int count = 0;
    for (int index = 0; index < SMILE_3D_MAX_CLIPS; ++index)
        if (smile_clips3d[index].active) count++;
    return count;
}

static int smile_3d_live_animator_count(void)
{
    int count = 0;
    for (int index = 0; index < SMILE_3D_MAX_ANIMATORS; ++index)
        if (smile_animators3d[index].active) count++;
    return count;
}

static int smile_3d_skeleton_reference_count(long long handle)
{
    int count = 0;
    for (int index = 0; index < SMILE_3D_MAX_CLIPS; ++index)
        if (smile_clips3d[index].active && smile_clips3d[index].skeleton_handle == handle) count++;
    for (int index = 0; index < SMILE_3D_MAX_ANIMATORS; ++index)
        if (smile_animators3d[index].active && smile_animators3d[index].skeleton_handle == handle) count++;
    return count;
}

static int smile_3d_clip_reference_count(long long handle)
{
    int count = 0;
    for (int index = 0; index < SMILE_3D_MAX_ANIMATORS; ++index)
        if (smile_animators3d[index].active && smile_animators3d[index].clip_handle == handle) count++;
    return count;
}

static int smile_3d_animator_reference_count(long long handle)
{
    int count = 0;
    for (int index = 0; index < SMILE_3D_MAX_OBJECTS; ++index)
        if (smile_objects3d[index].active && smile_objects3d[index].animator_handle == handle) count++;
    return count;
}

static int smile_3d_live_particle_batch_count(void)
{
    int count = 0;
    for (int index = 0; index < SMILE_3D_MAX_PARTICLE_BATCHES; ++index)
        if (smile_particle_batches3d[index].active) count++;
    return count;
}

static int smile_3d_live_ribbon_batch_count(void)
{
    int count = 0;
    for (int index = 0; index < SMILE_3D_MAX_RIBBON_BATCHES; ++index)
        if (smile_ribbon_batches3d[index].active) count++;
    return count;
}

static int smile_3d_model_animator_reference_count(const SmileModel3D* model)
{
    int count = 0;
    if (model == 0) return 0;
    for (int index = 0; index < SMILE_3D_MAX_ANIMATORS; ++index)
        if (smile_animators3d[index].active && smile_animators3d[index].model_animation &&
            smile_3d_model_resource(smile_animators3d[index].model_handle) == model) count++;
    return count;
}

static int smile_3d_mesh_reference_count(long long mesh_handle)
{
    int count = 0;
    int index;
    SmileMesh3D* mesh = smile_3d_mesh(mesh_handle);
    if (mesh == 0) return 0;
    count = (int)mesh->in_flight;
    for (index = 0; index < SMILE_3D_MAX_OBJECTS; ++index)
        if (smile_objects3d[index].active && smile_objects3d[index].mesh_handle == mesh_handle) count++;
    return count;
}

static int smile_3d_texture_reference_count(long long texture_handle)
{
    int count = 0;
    int index;
    SmileTexture3D* texture = smile_3d_texture(texture_handle);
    if (texture == 0) return 0;
    count = (int)texture->in_flight;
    for (index = 0; index < SMILE_3D_MAX_MATERIALS; ++index)
    {
        if (!smile_materials3d[index].active) continue;
        for (int semantic = 0; semantic < 4; ++semantic)
        {
            if (smile_materials3d[index].texture_handles[semantic] != texture_handle) continue;
            count++;
            break;
        }
    }
    return count;
}

static int smile_3d_material_reference_count(long long material_handle)
{
    int count = 0;
    int index;
    if (smile_3d_material(material_handle) == 0) return 0;
    for (index = 0; index < SMILE_3D_MAX_OBJECTS; ++index)
        if (smile_objects3d[index].active && smile_objects3d[index].material_handle == material_handle) count++;
    for (index = 0; index < SMILE_3D_MAX_PARTICLE_BATCHES; ++index)
        if (smile_particle_batches3d[index].active &&
            smile_particle_batches3d[index].material_handle == material_handle) count++;
    for (index = 0; index < SMILE_3D_MAX_RIBBON_BATCHES; ++index)
        if (smile_ribbon_batches3d[index].active &&
            smile_ribbon_batches3d[index].material_handle == material_handle) count++;
    return count;
}

static long long smile_3d_model_static_value(SmileModel3D* model,
    long long query, long long index, long long property)
{
    if (model == 0) { smile_last_error3d = 5; return 0; }
    if (query == 1) return model->format_version;
    if (query == 2) return model->vertex_count;
    if (query == 3) return model->index_count;
    if (query == 4) return model->texture_count;
    if (query == 5 || query == 6)
    {
        long long count = 0;
        if (model->format_version != 2) return 0;
        for (unsigned int part = 0; part < model->part_count; ++part)
        {
            SmileMesh3D* mesh = smile_3d_mesh(model->mesh_handles[part]);
            if (mesh == 0) continue;
            for (unsigned int vertex = 0; vertex < mesh->vertex_count; ++vertex)
                if ((query == 5 && mesh->vertices[vertex].tangent[3] > 0.0f) ||
                    (query == 6 && mesh->vertices[vertex].tangent[3] < 0.0f)) count++;
        }
        return count;
    }
    if (query == 7)
    {
        if (model->format_version != 2 || index < 0 || index >= model->material_count)
        { smile_last_error3d = 5; return 0; }
        if (property >= 1 && property <= 4)
            return (long long)llroundf(model->materials[index].base_color[property - 1] * 1000.0f);
        if (property == 5) return (long long)llroundf(model->materials[index].metallic * 1000.0f);
        if (property == 6) return (long long)llroundf(model->materials[index].roughness * 1000.0f);
        if (property == 7) return (long long)llroundf(model->materials[index].normal_strength * 1000.0f);
        if (property == 8) return (long long)llroundf(model->materials[index].occlusion_strength * 1000.0f);
        if (property >= 9 && property <= 11)
            return (long long)llroundf(model->materials[index].emissive[property - 9] * 1000.0f);
        if (property == 12) return model->materials[index].alpha_mode;
        if (property == 13) return (long long)llroundf(model->materials[index].alpha_cutoff * 1000.0f);
        if (property == 14) return model->materials[index].double_sided;
        if (property >= 15 && property <= 18) return model->materials[index].texture_references[property - 15] + 1;
        if (property == 19) return smile_3d_model_v2_string_hash(model, model->materials[index].name_offset);
    }
    else if (query == 8)
    {
        if (model->format_version != 2 || index < 0 || index >= model->texture_count)
        { smile_last_error3d = 5; return 0; }
        if (property == 1) return model->textures[index].semantic;
        if (property == 2) return smile_3d_model_v2_string_hash(model, model->textures[index].path_offset);
    }
    else if (query == 9)
    {
        const float* selected;
        if (model->format_version != 2 || property < 0 || property >= 6 || index < -1 || index >= model->part_count)
        { smile_last_error3d = 5; return 0; }
        selected = index < 0 ? model->bounds : model->part_bounds[index];
        return (long long)llroundf(selected[property] * 1000.0f);
    }
    else if (query == 10)
    {
        if (model->format_version != 2 || index < 0 || index >= model->part_count)
        { smile_last_error3d = 5; return 0; }
        return smile_3d_model_v2_string_hash(model, model->part_name_offsets[index]);
    }
    else if (query == 11)
    {
        if (model->format_version != 2) { smile_last_error3d = 5; return 0; }
        return smile_3d_model_v2_string_hash(model, model->model_name_offset);
    }
    smile_last_error3d = 5;
    return 0;
}

static void smile_3d_delete_mesh(SmileMesh3D* mesh)
{
    void* vertices = mesh->vertices;
    void* indices = mesh->indices;
    smile_3d_release(mesh->vertex_buffer);
    smile_3d_release(mesh->index_buffer);
    smile_3d_free(vertices);
    smile_3d_free(indices);
    mesh->vertices = 0;
    mesh->indices = 0;
    mesh->vertex_count = 0;
    mesh->index_count = 0;
    mesh->active = 0;
    mesh->committed = 0;
    mesh->explicit_normals = 0;
    mesh->max_joint = 0;
    mesh->in_flight = 0;
    mesh->generation++;
    if (mesh->generation == 0) mesh->generation = 1;
}

static int smile_3d_delete_model(SmileModel3D* model)
{
    int index;
    if (model == 0) return 0;
    if (smile_3d_model_animator_reference_count(model) != 0) return 0;
    for (index = 0; index < model->part_count; ++index)
        if (smile_3d_mesh_reference_count(model->mesh_handles[index]) != 0) return 0;
    for (index = 0; index < model->prepared_material_count; ++index)
        if (smile_3d_material_reference_count(model->prepared_material_handles[index]) != 0) return 0;
    if (!smile_3d_clear_model_pbr(model)) return 0;
    for (index = 0; index < model->part_count; ++index)
    {
        SmileMesh3D* mesh = smile_3d_mesh(model->mesh_handles[index]);
        if (mesh != 0) smile_3d_delete_mesh(mesh);
        model->mesh_handles[index] = 0;
        model->material_slots[index] = 0;
    }
    { void* strings = model->strings; smile_3d_free(strings); }
    { void* animation = model->animation_data; smile_3d_free(animation); }
    model->strings = 0;
    model->animation_data = 0;
    ZeroMemory(model->animation_chunks, sizeof(model->animation_chunks));
    model->string_bytes = 0;
    model->active = 0;
    model->part_count = 0;
    model->material_count = 0;
    model->texture_count = 0;
    model->format_version = 0;
    model->vertex_count = 0;
    model->index_count = 0;
    model->model_name_offset = 0;
    model->prepared_pbr = 0;
    model->generation++;
    if (model->generation == 0) model->generation = 1;
    return 1;
}

static void smile_3d_delete_skeleton(SmileSkeleton3D* skeleton)
{
    skeleton->active = 0;
    skeleton->committed = 0;
    skeleton->bone_count = 0;
    skeleton->generation++;
    if (skeleton->generation == 0) skeleton->generation = 1;
}

static void smile_3d_delete_clip(SmileAnimationClip3D* clip)
{
    clip->active = 0;
    clip->skeleton_handle = 0;
    clip->event_count = 0;
    clip->duration_ms = 0;
    clip->generation++;
    if (clip->generation == 0) clip->generation = 1;
}

static void smile_3d_delete_animator(SmileAnimator3D* animator)
{
    animator->active = 0;
    animator->skeleton_handle = 0;
    animator->clip_handle = 0;
    animator->pending_event = 0;
    animator->model_animation = 0;
    animator->model_handle = 0;
    animator->event_count = 0;
    animator->generation++;
    if (animator->generation == 0) animator->generation = 1;
}

static long long smile_3d_create_skeleton(int bone_count)
{
    int slot;
    if (bone_count <= 0 || bone_count > SMILE_3D_MAX_BONES) { smile_last_error3d = 28; return 0; }
    for (slot = 0; slot < SMILE_3D_MAX_SKELETONS; ++slot)
        if (!smile_skeletons3d[slot].active) break;
    if (slot == SMILE_3D_MAX_SKELETONS) { smile_last_error3d = 29; return 0; }
    SmileSkeleton3D* skeleton = &smile_skeletons3d[slot];
    unsigned short generation = skeleton->generation == 0 ? 1 : skeleton->generation;
    ZeroMemory(skeleton, sizeof(*skeleton));
    skeleton->generation = generation;
    skeleton->active = 1;
    skeleton->bone_count = (unsigned char)bone_count;
    for (int index = 0; index < bone_count; ++index) skeleton->parents[index] = -2;
    return smile_3d_handle(SMILE_3D_SKELETON_HANDLE, slot, skeleton->generation);
}

static int smile_3d_commit_skeleton(SmileSkeleton3D* skeleton)
{
    if (skeleton == 0) { smile_last_error3d = 5; return 0; }
    for (int bone = 0; bone < skeleton->bone_count; ++bone)
    {
        int parent = skeleton->parents[bone];
        float x = skeleton->bind_translation[bone][0];
        float y = skeleton->bind_translation[bone][1];
        float z = skeleton->bind_translation[bone][2];
        if (parent < -1 || parent >= bone) { smile_last_error3d = 30; return 0; }
        if (parent >= 0)
        {
            x -= skeleton->inverse_bind_translation[parent][0];
            y -= skeleton->inverse_bind_translation[parent][1];
            z -= skeleton->inverse_bind_translation[parent][2];
        }
        skeleton->inverse_bind_translation[bone][0] = -x;
        skeleton->inverse_bind_translation[bone][1] = -y;
        skeleton->inverse_bind_translation[bone][2] = -z;
    }
    skeleton->committed = 1;
    return 1;
}

static long long smile_3d_create_clip(long long skeleton_handle, unsigned int duration_ms)
{
    int slot;
    SmileSkeleton3D* skeleton = smile_3d_skeleton(skeleton_handle);
    if (skeleton == 0 || !skeleton->committed || duration_ms == 0 || duration_ms > 600000)
    {
        smile_last_error3d = 31;
        return 0;
    }
    for (slot = 0; slot < SMILE_3D_MAX_CLIPS; ++slot)
        if (!smile_clips3d[slot].active) break;
    if (slot == SMILE_3D_MAX_CLIPS) { smile_last_error3d = 32; return 0; }
    SmileAnimationClip3D* clip = &smile_clips3d[slot];
    unsigned short generation = clip->generation == 0 ? 1 : clip->generation;
    ZeroMemory(clip, sizeof(*clip));
    clip->generation = generation;
    clip->active = 1;
    clip->pbr_scale_safe = 1;
    clip->skeleton_handle = skeleton_handle;
    clip->duration_ms = duration_ms;
    return smile_3d_handle(SMILE_3D_CLIP_HANDLE, slot, clip->generation);
}

static void smile_3d_update_clip_pbr_scale_safety(SmileAnimationClip3D* clip)
{
    clip->pbr_scale_safe = 1;
    for (int bone = 0; bone < SMILE_3D_MAX_BONES; ++bone)
    {
        if (!clip->scale_tracks[bone]) continue;
        for (int key = 0; key < 2; ++key)
        {
            float x = clip->scale[bone][key][0];
            float y = clip->scale[bone][key][1];
            float z = clip->scale[bone][key][2];
            if (fabsf(x - y) > 0.0001f || fabsf(y - z) > 0.0001f)
            {
                clip->pbr_scale_safe = 0;
                return;
            }
        }
    }
}

static long long smile_3d_create_animator(long long skeleton_handle)
{
    int slot;
    SmileSkeleton3D* skeleton = smile_3d_skeleton(skeleton_handle);
    if (skeleton == 0 || !skeleton->committed) { smile_last_error3d = 31; return 0; }
    for (slot = 0; slot < SMILE_3D_MAX_ANIMATORS; ++slot)
        if (!smile_animators3d[slot].active) break;
    if (slot == SMILE_3D_MAX_ANIMATORS) { smile_last_error3d = 33; return 0; }
    SmileAnimator3D* animator = &smile_animators3d[slot];
    unsigned short generation = animator->generation == 0 ? 1 : animator->generation;
    ZeroMemory(animator, sizeof(*animator));
    animator->generation = generation;
    animator->active = 1;
    animator->skeleton_handle = skeleton_handle;
    animator->speed_percent = 100;
    for (int bone = 0; bone < SMILE_3D_MAX_BONES; ++bone) animator->bones[bone] = smile_3d_identity();
    return smile_3d_handle(SMILE_3D_ANIMATOR_HANDLE, slot, animator->generation);
}

static void smile_3d_delete_texture(SmileTexture3D* texture)
{
    smile_3d_release(texture->view);
    smile_3d_release(texture->texture);
    smile_3d_release(texture->sampler);
    smile_image_resource_release(texture->image);
    texture->image = 0;
    texture->active = 0;
    texture->in_flight = 0;
    texture->generation++;
    if (texture->generation == 0) texture->generation = 1;
}

static void smile_3d_delete_material(SmileMaterial3D* material)
{
    material->active = 0;
    for (int semantic = 0; semantic < 4; ++semantic) material->texture_handles[semantic] = 0;
    material->owner_model_handle = 0;
    material->mode = 0;
    material->generation++;
    if (material->generation == 0) material->generation = 1;
}

static void smile_3d_delete_particle_batch(SmileParticleBatch3D* batch)
{
    void* instances = batch->instances;
    void* committed_instances = batch->committed_instances;
    smile_3d_release(batch->instance_buffer);
    smile_3d_free(instances);
    smile_3d_free(committed_instances);
    smile_staged_particle_capacity3d -= batch->capacity;
    batch->instances = 0;
    batch->committed_instances = 0;
    batch->active = 0;
    batch->capacity = 0;
    batch->count = 0;
    batch->revision = 0;
    batch->uploaded_revision = 0;
    batch->in_flight = 0;
    batch->material_handle = 0;
    batch->generation++;
    if (batch->generation == 0) batch->generation = 1;
}

static void smile_3d_delete_ribbon_batch(SmileRibbonBatch3D* batch)
{
    void* points = batch->points;
    void* staging_vertices = batch->staging_vertices;
    void* vertices = batch->vertices;
    smile_3d_release(batch->vertex_buffer);
    smile_3d_free(points);
    smile_3d_free(staging_vertices);
    smile_3d_free(vertices);
    smile_staged_ribbon_capacity3d -= batch->capacity;
    batch->points = 0;
    batch->staging_vertices = 0;
    batch->vertices = 0;
    batch->active = 0;
    batch->capacity = 0;
    batch->count = 0;
    batch->revision = 0;
    batch->uploaded_revision = 0;
    batch->in_flight = 0;
    batch->material_handle = 0;
    batch->generation++;
    if (batch->generation == 0) batch->generation = 1;
}

static long long smile_3d_create_particle_batch(unsigned int capacity,
    long long material_handle, int billboard_mode, int atlas_columns, int atlas_rows)
{
    int slot;
    SmileMaterial3D* material = smile_3d_material(material_handle);
    if (capacity == 0 || capacity > SMILE_3D_MAX_PARTICLES_PER_BATCH ||
        capacity > SMILE_3D_MAX_STAGED_PARTICLES - smile_staged_particle_capacity3d ||
        material == 0 || material->mode != 0 ||
        (material->alpha_mode != 2 && material->alpha_mode != 3) ||
        billboard_mode < 1 || billboard_mode > 2 ||
        atlas_columns < 1 || atlas_columns > 16 || atlas_rows < 1 || atlas_rows > 16)
    {
        smile_last_error3d = 54;
        smile_vfx_rejected_operation_count3d++;
        return 0;
    }
    for (slot = 0; slot < SMILE_3D_MAX_PARTICLE_BATCHES; ++slot)
        if (!smile_particle_batches3d[slot].active) break;
    if (slot == SMILE_3D_MAX_PARTICLE_BATCHES)
    {
        smile_last_error3d = 55;
        smile_vfx_rejected_operation_count3d++;
        return 0;
    }
    SmileParticleInstance3D* instances = (SmileParticleInstance3D*)smile_3d_allocate(
        sizeof(SmileParticleInstance3D) * capacity);
    SmileParticleInstance3D* committed_instances = (SmileParticleInstance3D*)smile_3d_allocate(
        sizeof(SmileParticleInstance3D) * capacity);
    if (instances == 0 || committed_instances == 0)
    {
        void* staging_allocation = instances;
        void* committed_allocation = committed_instances;
        smile_3d_free(staging_allocation);
        smile_3d_free(committed_allocation);
        smile_last_error3d = 55;
        smile_vfx_rejected_operation_count3d++;
        return 0;
    }
    SmileParticleBatch3D* batch = &smile_particle_batches3d[slot];
    unsigned short generation = batch->generation == 0 ? 1 : batch->generation;
    ZeroMemory(batch, sizeof(*batch));
    ZeroMemory(instances, sizeof(SmileParticleInstance3D) * capacity);
    ZeroMemory(committed_instances, sizeof(SmileParticleInstance3D) * capacity);
    batch->generation = generation;
    batch->active = 1;
    batch->billboard_mode = (unsigned char)billboard_mode;
    batch->atlas_columns = (unsigned short)atlas_columns;
    batch->atlas_rows = (unsigned short)atlas_rows;
    batch->capacity = capacity;
    batch->material_handle = material_handle;
    batch->instances = instances;
    batch->committed_instances = committed_instances;
    smile_staged_particle_capacity3d += capacity;
    return smile_3d_handle(SMILE_3D_PARTICLE_BATCH_HANDLE, slot, batch->generation);
}

static long long smile_3d_create_ribbon_batch(unsigned int capacity, long long material_handle)
{
    int slot;
    SmileMaterial3D* material = smile_3d_material(material_handle);
    if (capacity < 2 || capacity > SMILE_3D_MAX_RIBBON_POINTS_PER_BATCH ||
        capacity > SMILE_3D_MAX_STAGED_RIBBON_POINTS - smile_staged_ribbon_capacity3d ||
        material == 0 || material->mode != 0 ||
        (material->alpha_mode != 2 && material->alpha_mode != 3))
    {
        smile_last_error3d = 54;
        smile_vfx_rejected_operation_count3d++;
        return 0;
    }
    for (slot = 0; slot < SMILE_3D_MAX_RIBBON_BATCHES; ++slot)
        if (!smile_ribbon_batches3d[slot].active) break;
    if (slot == SMILE_3D_MAX_RIBBON_BATCHES)
    {
        smile_last_error3d = 55;
        smile_vfx_rejected_operation_count3d++;
        return 0;
    }
    SmileRibbonPoint3D* points = (SmileRibbonPoint3D*)smile_3d_allocate(
        sizeof(SmileRibbonPoint3D) * capacity);
    SmileRibbonVertex3D* vertices = (SmileRibbonVertex3D*)smile_3d_allocate(
        sizeof(SmileRibbonVertex3D) * capacity * 2);
    SmileRibbonVertex3D* staging_vertices = (SmileRibbonVertex3D*)smile_3d_allocate(
        sizeof(SmileRibbonVertex3D) * capacity * 2);
    if (points == 0 || vertices == 0 || staging_vertices == 0)
    {
        void* points_allocation = points;
        void* vertices_allocation = vertices;
        void* staging_vertices_allocation = staging_vertices;
        smile_3d_free(points_allocation);
        smile_3d_free(vertices_allocation);
        smile_3d_free(staging_vertices_allocation);
        smile_last_error3d = 55;
        smile_vfx_rejected_operation_count3d++;
        return 0;
    }
    SmileRibbonBatch3D* batch = &smile_ribbon_batches3d[slot];
    unsigned short generation = batch->generation == 0 ? 1 : batch->generation;
    ZeroMemory(batch, sizeof(*batch));
    ZeroMemory(points, sizeof(SmileRibbonPoint3D) * capacity);
    ZeroMemory(vertices, sizeof(SmileRibbonVertex3D) * capacity * 2);
    ZeroMemory(staging_vertices, sizeof(SmileRibbonVertex3D) * capacity * 2);
    batch->generation = generation;
    batch->active = 1;
    batch->capacity = capacity;
    batch->material_handle = material_handle;
    batch->points = points;
    batch->staging_vertices = staging_vertices;
    batch->vertices = vertices;
    smile_staged_ribbon_capacity3d += capacity;
    return smile_3d_handle(SMILE_3D_RIBBON_BATCH_HANDLE, slot, batch->generation);
}

static int smile_3d_upload_particle_data(SmileParticleBatch3D* batch,
    const SmileParticleInstance3D* instances, unsigned int count, unsigned int revision)
{
    ID3D11Device* device = (ID3D11Device*)smile_graphics_directx_device();
    ID3D11DeviceContext* context = (ID3D11DeviceContext*)smile_graphics_directx_context();
    if (batch == 0 || device == 0 || context == 0 || !smile_3d_create_pipeline())
    { smile_last_error3d = 57; return 0; }
    if (batch->instance_buffer == 0)
    {
        D3D11_BUFFER_DESC description = {};
        description.ByteWidth = sizeof(SmileParticleInstance3D) * batch->capacity;
        description.Usage = D3D11_USAGE_DYNAMIC;
        description.BindFlags = D3D11_BIND_VERTEX_BUFFER;
        description.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;
        if (FAILED(device->CreateBuffer(&description, 0, &batch->instance_buffer)))
        { smile_last_error3d = 57; return 0; }
    }
    if (batch->uploaded_revision == revision) return 1;
    D3D11_MAPPED_SUBRESOURCE mapped = {};
    if (FAILED(context->Map(batch->instance_buffer, 0, D3D11_MAP_WRITE_DISCARD, 0, &mapped)))
    { smile_last_error3d = 57; return 0; }
    if (count != 0)
        memcpy(mapped.pData, instances, sizeof(SmileParticleInstance3D) * count);
    context->Unmap(batch->instance_buffer, 0);
    batch->uploaded_revision = revision;
    smile_vfx_upload_count3d++;
    return 1;
}

static int smile_3d_upload_particle_batch(SmileParticleBatch3D* batch)
{
    return smile_3d_upload_particle_data(batch, batch->committed_instances,
        batch->count, batch->revision);
}

static int smile_3d_upload_ribbon_data(SmileRibbonBatch3D* batch,
    const SmileRibbonVertex3D* vertices, unsigned int count, unsigned int revision)
{
    ID3D11Device* device = (ID3D11Device*)smile_graphics_directx_device();
    ID3D11DeviceContext* context = (ID3D11DeviceContext*)smile_graphics_directx_context();
    if (batch == 0 || device == 0 || context == 0 || !smile_3d_create_pipeline())
    { smile_last_error3d = 57; return 0; }
    if (batch->vertex_buffer == 0)
    {
        D3D11_BUFFER_DESC description = {};
        description.ByteWidth = sizeof(SmileRibbonVertex3D) * batch->capacity * 2;
        description.Usage = D3D11_USAGE_DYNAMIC;
        description.BindFlags = D3D11_BIND_VERTEX_BUFFER;
        description.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;
        if (FAILED(device->CreateBuffer(&description, 0, &batch->vertex_buffer)))
        { smile_last_error3d = 57; return 0; }
    }
    if (batch->uploaded_revision == revision) return 1;
    D3D11_MAPPED_SUBRESOURCE mapped = {};
    if (FAILED(context->Map(batch->vertex_buffer, 0, D3D11_MAP_WRITE_DISCARD, 0, &mapped)))
    { smile_last_error3d = 57; return 0; }
    if (count != 0)
        memcpy(mapped.pData, vertices, sizeof(SmileRibbonVertex3D) * count * 2);
    context->Unmap(batch->vertex_buffer, 0);
    batch->uploaded_revision = revision;
    smile_vfx_upload_count3d++;
    return 1;
}

static int smile_3d_upload_ribbon_batch(SmileRibbonBatch3D* batch)
{
    return smile_3d_upload_ribbon_data(batch, batch->vertices,
        batch->count, batch->revision);
}

static int smile_3d_commit_particle_batch(SmileParticleBatch3D* batch, unsigned int count)
{
    unsigned int revision;
    SmileParticleInstance3D* swap;
    if (batch == 0 || count > batch->capacity)
    { smile_last_error3d = 54; smile_vfx_rejected_operation_count3d++; return 0; }
    if (batch->in_flight != 0)
    { smile_last_error3d = 56; smile_vfx_rejected_operation_count3d++; return 0; }
    revision = batch->revision + 1;
    if (revision == 0) revision = 1;
    if (!smile_3d_upload_particle_data(batch, batch->instances, count, revision)) return 0;
    swap = batch->committed_instances;
    batch->committed_instances = batch->instances;
    batch->instances = swap;
    memcpy(batch->instances, batch->committed_instances,
        sizeof(SmileParticleInstance3D) * batch->capacity);
    batch->count = count;
    batch->revision = revision;
    return 1;
}

static int smile_3d_commit_ribbon_batch(SmileRibbonBatch3D* batch, unsigned int count)
{
    unsigned int revision;
    SmileRibbonVertex3D* swap;
    if (batch == 0 || count > batch->capacity)
    { smile_last_error3d = 54; smile_vfx_rejected_operation_count3d++; return 0; }
    if (batch->in_flight != 0)
    { smile_last_error3d = 56; smile_vfx_rejected_operation_count3d++; return 0; }
    for (unsigned int point = 0; point < count; ++point)
    {
        for (int side = 0; side < 2; ++side)
        {
            SmileRibbonVertex3D* vertex = &batch->staging_vertices[point * 2 + side];
            const float* position = side == 0 ? batch->points[point].left : batch->points[point].right;
            memcpy(vertex->position, position, sizeof(vertex->position));
            vertex->uv[0] = batch->points[point].u;
            vertex->uv[1] = side == 0 ? 0.0f : 1.0f;
            memcpy(vertex->color, batch->points[point].color, sizeof(vertex->color));
        }
    }
    revision = batch->revision + 1;
    if (revision == 0) revision = 1;
    if (!smile_3d_upload_ribbon_data(batch, batch->staging_vertices, count, revision)) return 0;
    swap = batch->vertices;
    batch->vertices = batch->staging_vertices;
    batch->staging_vertices = swap;
    batch->count = count;
    batch->revision = revision;
    return 1;
}

static long long smile_3d_create_texture(SmileImageResource* image, int filter, int wrap)
{
    int slot;
    SmileTexture3D* texture;
    long long width = smile_image_resource_width(image);
    long long height = smile_image_resource_height(image);
    if (image == 0 || smile_image_resource_straight_pixels(image) == 0 || width <= 0 || height <= 0 ||
        width > 8192 || height > 8192 || filter < 0 || filter > 1 || wrap < 0 || wrap > 1)
    {
        smile_image_resource_release(image);
        smile_last_error3d = 17;
        return 0;
    }
    for (slot = 0; slot < SMILE_3D_MAX_TEXTURES; ++slot)
        if (!smile_textures3d[slot].active) break;
    if (slot == SMILE_3D_MAX_TEXTURES)
    {
        smile_image_resource_release(image);
        smile_last_error3d = 18;
        return 0;
    }
    texture = &smile_textures3d[slot];
    if (texture->generation == 0) texture->generation = 1;
    texture->active = 1;
    texture->in_flight = 0;
    texture->filter = (unsigned char)filter;
    texture->wrap = (unsigned char)wrap;
    texture->pbr = 0;
    texture->semantic = 0;
    texture->requested_anisotropy = 1;
    texture->effective_anisotropy = 1;
    texture->mip_levels = 1;
    texture->image = image;
    texture->texture = 0;
    texture->view = 0;
    texture->sampler = 0;
    return smile_3d_handle(SMILE_3D_TEXTURE_HANDLE, slot, texture->generation);
}

static int smile_3d_set_material(SmileMaterial3D* material, int alpha_mode,
    long long red, long long green, long long blue, long long opacity,
    int unlit, long long emissive, long long cutoff)
{
    if (material == 0 || material->mode != 0 || alpha_mode < 0 || alpha_mode > 3 ||
        opacity < 0 || opacity > 100 ||
        emissive < 0 || emissive > 400 || cutoff < 0 || cutoff > 100)
    {
        smile_last_error3d = 19;
        return 0;
    }
    material->alpha_mode = (unsigned char)alpha_mode;
    material->unlit = (unsigned char)(unlit != 0);
    material->color[0] = (float)(red & 255) / 255.0f;
    material->color[1] = (float)(green & 255) / 255.0f;
    material->color[2] = (float)(blue & 255) / 255.0f;
    material->color[3] = (float)opacity / 100.0f;
    material->emissive = (float)emissive / 100.0f;
    material->cutoff = (float)cutoff / 100.0f;
    return 1;
}

static long long smile_3d_create_material(long long texture_handle, int alpha_mode,
    long long red, long long green, long long blue, long long opacity,
    int unlit, long long emissive, long long cutoff)
{
    int slot;
    SmileMaterial3D* material;
    if (texture_handle != 0 && smile_3d_texture(texture_handle) == 0)
    {
        smile_last_error3d = 5;
        return 0;
    }
    for (slot = 0; slot < SMILE_3D_MAX_MATERIALS; ++slot)
        if (!smile_materials3d[slot].active) break;
    if (slot == SMILE_3D_MAX_MATERIALS)
    {
        smile_last_error3d = 20;
        return 0;
    }
    material = &smile_materials3d[slot];
    if (material->generation == 0) material->generation = 1;
    ZeroMemory(&material->alpha_mode, sizeof(*material) - offsetof(SmileMaterial3D, alpha_mode));
    material->active = 1;
    material->mode = 0;
    material->texture_handles[0] = texture_handle;
    if (!smile_3d_set_material(material, alpha_mode, red, green, blue, opacity, unlit, emissive, cutoff))
    {
        smile_3d_delete_material(material);
        return 0;
    }
    return smile_3d_handle(SMILE_3D_MATERIAL_HANDLE, slot, material->generation);
}

static unsigned char smile_3d_mip_count(long long width, long long height)
{
    unsigned char count = 1;
    long long extent = width > height ? width : height;
    while (extent > 1)
    {
        extent >>= 1;
        count++;
    }
    return count;
}

static long long smile_3d_create_pbr_texture(SmileImageResource* image, int semantic,
    int filter, int wrap, int anisotropy)
{
    int slot;
    long long width = smile_image_resource_width(image);
    long long height = smile_image_resource_height(image);
    if (image == 0 || smile_image_resource_straight_pixels(image) == 0 || width <= 0 || height <= 0 ||
        width > 8192 || height > 8192 || semantic < 1 || semantic > 2 ||
        filter < 0 || filter > 3 || wrap < 0 || wrap > 1 || anisotropy < 1 || anisotropy > 16)
    {
        smile_image_resource_release(image);
        smile_last_error3d = 38;
        return 0;
    }
    for (slot = 0; slot < SMILE_3D_MAX_TEXTURES; ++slot)
        if (!smile_textures3d[slot].active) break;
    if (slot == SMILE_3D_MAX_TEXTURES)
    {
        smile_image_resource_release(image);
        smile_last_error3d = 18;
        return 0;
    }
    SmileTexture3D* texture = &smile_textures3d[slot];
    unsigned short generation = texture->generation == 0 ? 1 : texture->generation;
    ZeroMemory(texture, sizeof(*texture));
    texture->generation = generation;
    texture->active = 1;
    texture->filter = (unsigned char)filter;
    texture->wrap = (unsigned char)wrap;
    texture->pbr = 1;
    texture->semantic = (unsigned char)semantic;
    texture->requested_anisotropy = (unsigned char)anisotropy;
    texture->effective_anisotropy = (unsigned char)(filter == 3
        ? (anisotropy < (int)smile_max_anisotropy3d ? anisotropy : (int)smile_max_anisotropy3d)
        : 1);
    texture->mip_levels = filter >= 2 ? smile_3d_mip_count(width, height) : 1;
    texture->image = image;
    return smile_3d_handle(SMILE_3D_TEXTURE_HANDLE, slot, texture->generation);
}

static int smile_3d_pbr_texture_set_valid(const long long handles[4])
{
    static const unsigned char expected[4] = { 1, 2, 2, 1 };
    for (int semantic = 0; semantic < 4; ++semantic)
    {
        SmileTexture3D* texture;
        if (handles[semantic] == 0) continue;
        texture = smile_3d_texture(handles[semantic]);
        if (texture == 0 || !texture->pbr || texture->semantic != expected[semantic]) return 0;
    }
    return 1;
}

static int smile_3d_set_pbr_textures(SmileMaterial3D* material,
    long long base_texture, long long normal_texture, long long orm_texture,
    long long emissive_texture, int alpha_mode, int double_sided)
{
    long long handles[4] = { base_texture, normal_texture, orm_texture, emissive_texture };
    if (material == 0 || material->mode != 1 || alpha_mode < 0 || alpha_mode > 2 ||
        (double_sided != 0 && double_sided != 1) || !smile_3d_pbr_texture_set_valid(handles))
    {
        smile_last_error3d = 39;
        return 0;
    }
    for (int semantic = 0; semantic < 4; ++semantic)
        material->texture_handles[semantic] = handles[semantic];
    material->alpha_mode = (unsigned char)alpha_mode;
    material->double_sided = (unsigned char)double_sided;
    return 1;
}

static int smile_3d_set_pbr_factors(SmileMaterial3D* material,
    long long red, long long green, long long blue, long long alpha,
    long long metallic, long long roughness, long long normal_strength,
    long long occlusion_strength, long long cutoff)
{
    if (material == 0 || material->mode != 1 || red < 0 || red > 1000 ||
        green < 0 || green > 1000 || blue < 0 || blue > 1000 || alpha < 0 || alpha > 1000 ||
        metallic < 0 || metallic > 1000 || roughness < 0 || roughness > 1000 ||
        normal_strength < 0 || normal_strength > 4000 ||
        occlusion_strength < 0 || occlusion_strength > 1000 || cutoff < 0 || cutoff > 1000)
    {
        smile_last_error3d = 39;
        return 0;
    }
    material->color[0] = (float)red / 1000.0f;
    material->color[1] = (float)green / 1000.0f;
    material->color[2] = (float)blue / 1000.0f;
    material->color[3] = (float)alpha / 1000.0f;
    material->metallic = (float)metallic / 1000.0f;
    material->roughness = (float)roughness / 1000.0f;
    material->normal_strength = (float)normal_strength / 1000.0f;
    material->occlusion_strength = (float)occlusion_strength / 1000.0f;
    material->cutoff = (float)cutoff / 1000.0f;
    return 1;
}

static int smile_3d_set_pbr_emissive(SmileMaterial3D* material,
    long long red, long long green, long long blue)
{
    if (material == 0 || material->mode != 1 || red < 0 || red > 4000 ||
        green < 0 || green > 4000 || blue < 0 || blue > 4000)
    {
        smile_last_error3d = 39;
        return 0;
    }
    material->emissive_color[0] = (float)red / 1000.0f;
    material->emissive_color[1] = (float)green / 1000.0f;
    material->emissive_color[2] = (float)blue / 1000.0f;
    return 1;
}

static long long smile_3d_create_pbr_material(long long base_texture,
    long long normal_texture, long long orm_texture, long long emissive_texture,
    int alpha_mode, int double_sided, long long owner_model_handle)
{
    int slot;
    for (slot = 0; slot < SMILE_3D_MAX_MATERIALS; ++slot)
        if (!smile_materials3d[slot].active) break;
    if (slot == SMILE_3D_MAX_MATERIALS) { smile_last_error3d = 20; return 0; }
    SmileMaterial3D* material = &smile_materials3d[slot];
    unsigned short generation = material->generation == 0 ? 1 : material->generation;
    ZeroMemory(material, sizeof(*material));
    material->generation = generation;
    material->active = 1;
    material->mode = 1;
    material->owner_model_handle = owner_model_handle;
    material->color[0] = material->color[1] = material->color[2] = material->color[3] = 1.0f;
    material->roughness = 1.0f;
    material->normal_strength = 1.0f;
    material->occlusion_strength = 1.0f;
    material->cutoff = 0.5f;
    if (!smile_3d_set_pbr_textures(material, base_texture, normal_texture, orm_texture,
        emissive_texture, alpha_mode, double_sided))
    {
        smile_3d_delete_material(material);
        return 0;
    }
    return smile_3d_handle(SMILE_3D_MATERIAL_HANDLE, slot, material->generation);
}

static int smile_3d_clear_model_pbr(SmileModel3D* model)
{
    if (model == 0) return 0;
    for (int index = 0; index < model->owned_texture_count; ++index)
    {
        SmileTexture3D* texture = smile_3d_texture(model->owned_texture_handles[index]);
        if (texture != 0 && texture->in_flight != 0) return 0;
    }
    for (int index = 0; index < model->prepared_material_count; ++index)
        if (smile_3d_material_reference_count(model->prepared_material_handles[index]) != 0)
            return 0;
    for (int index = 0; index < model->prepared_material_count; ++index)
    {
        SmileMaterial3D* material = smile_3d_material(model->prepared_material_handles[index]);
        if (material != 0) smile_3d_delete_material(material);
        model->prepared_material_handles[index] = 0;
    }
    for (int index = 0; index < model->owned_texture_count; ++index)
    {
        SmileTexture3D* texture = smile_3d_texture(model->owned_texture_handles[index]);
        if (texture != 0) smile_3d_delete_texture(texture);
        model->owned_texture_handles[index] = 0;
    }
    ZeroMemory(model->prepared_texture_by_reference, sizeof(model->prepared_texture_by_reference));
    model->prepared_material_count = 0;
    model->prepared_reference_count = 0;
    model->owned_texture_count = 0;
    model->prepared_pbr = 0;
    model->has_animation = 0;
    return 1;
}

static void smile_3d_reset_lights(void)
{
    smile_ambient_color3d[0] = smile_ambient_color3d[1] = smile_ambient_color3d[2] = 1.0f;
    smile_ambient_intensity3d = 0.25f;
    smile_directional_light3d.enabled = 1;
    smile_directional_light3d.direction[0] = -0.35f;
    smile_directional_light3d.direction[1] = 0.8f;
    smile_directional_light3d.direction[2] = -0.45f;
    smile_directional_light3d.color[0] = smile_directional_light3d.color[1] =
        smile_directional_light3d.color[2] = 1.0f;
    smile_directional_light3d.intensity = 1.0f;
    ZeroMemory(smile_local_lights3d, sizeof(smile_local_lights3d));
}

static int smile_3d_set_ambient(long long red, long long green, long long blue, long long intensity)
{
    if (red < 0 || red > 255 || green < 0 || green > 255 || blue < 0 || blue > 255 ||
        intensity < 0 || intensity > 1000) { smile_last_error3d = 43; return 0; }
    smile_ambient_color3d[0] = (float)red / 255.0f;
    smile_ambient_color3d[1] = (float)green / 255.0f;
    smile_ambient_color3d[2] = (float)blue / 255.0f;
    smile_ambient_intensity3d = (float)intensity / 1000.0f;
    return 1;
}

static int smile_3d_normalized_direction(long long x, long long y, long long z, float output[3])
{
    float fx = (float)x, fy = (float)y, fz = (float)z;
    float length;
    if (x < -1000 || x > 1000 || y < -1000 || y > 1000 || z < -1000 || z > 1000)
        return 0;
    length = sqrtf(fx * fx + fy * fy + fz * fz);
    if (length <= 0.0001f) return 0;
    output[0] = fx / length;
    output[1] = fy / length;
    output[2] = fz / length;
    return 1;
}

static int smile_3d_set_directional(long long x, long long y, long long z,
    long long red, long long green, long long blue, long long intensity)
{
    float direction[3];
    if (!smile_3d_normalized_direction(x, y, z, direction) || red < 0 || red > 255 ||
        green < 0 || green > 255 || blue < 0 || blue > 255 || intensity < 0 || intensity > 16000)
    { smile_last_error3d = 43; return 0; }
    memcpy(smile_directional_light3d.direction, direction, sizeof(direction));
    smile_directional_light3d.color[0] = (float)red / 255.0f;
    smile_directional_light3d.color[1] = (float)green / 255.0f;
    smile_directional_light3d.color[2] = (float)blue / 255.0f;
    smile_directional_light3d.intensity = (float)intensity / 1000.0f;
    smile_directional_light3d.enabled = intensity != 0;
    return 1;
}

static int smile_3d_set_local_light(long long slot, long long type, long long x, long long y,
    long long z, long long red, long long green, long long blue, long long intensity, long long range)
{
    SmileLocalLight3D* light;
    if (slot < 0 || slot >= SMILE_3D_MAX_LOCAL_LIGHTS || type < 0 || type > 2)
    { smile_last_error3d = 43; return 0; }
    light = &smile_local_lights3d[slot];
    if (type == 0) { ZeroMemory(light, sizeof(*light)); return 1; }
    if (x < -1000000 || x > 1000000 || y < -1000000 || y > 1000000 ||
        z < -1000000 || z > 1000000 || red < 0 || red > 255 || green < 0 || green > 255 ||
        blue < 0 || blue > 255 || intensity < 0 || intensity > 16000 || range < 1 || range > 1000000)
    { smile_last_error3d = 43; return 0; }
    light->type = (unsigned char)type;
    light->position[0] = (float)x; light->position[1] = (float)y; light->position[2] = (float)z;
    light->color[0] = (float)red / 255.0f; light->color[1] = (float)green / 255.0f;
    light->color[2] = (float)blue / 255.0f;
    light->intensity = (float)intensity / 1000.0f;
    light->range = (float)range;
    if (light->inner_cosine == 0.0f && light->outer_cosine == 0.0f)
    {
        light->direction[1] = -1.0f;
        light->inner_cosine = cosf(20.0f * SMILE_3D_PI / 180.0f);
        light->outer_cosine = cosf(30.0f * SMILE_3D_PI / 180.0f);
    }
    return 1;
}

static int smile_3d_set_spot_cone(long long slot, long long x, long long y, long long z,
    long long inner_degrees, long long outer_degrees)
{
    float direction[3];
    if (slot < 0 || slot >= SMILE_3D_MAX_LOCAL_LIGHTS ||
        smile_local_lights3d[slot].type != 2 ||
        !smile_3d_normalized_direction(x, y, z, direction) ||
        inner_degrees < 1 || inner_degrees > 89 || outer_degrees < inner_degrees || outer_degrees > 89)
    { smile_last_error3d = 43; return 0; }
    SmileLocalLight3D* light = &smile_local_lights3d[slot];
    memcpy(light->direction, direction, sizeof(direction));
    light->inner_cosine = cosf((float)inner_degrees * SMILE_3D_PI / 180.0f);
    light->outer_cosine = cosf((float)outer_degrees * SMILE_3D_PI / 180.0f);
    return 1;
}

static long long smile_3d_pbr_texture_value(SmileTexture3D* texture, long long property)
{
    if (texture == 0 || !texture->pbr) { smile_last_error3d = 5; return 0; }
    if (property == 1) return texture->semantic;
    if (property == 2) return texture->filter;
    if (property == 3) return texture->wrap;
    if (property == 4) return texture->requested_anisotropy;
    if (property == 5) return texture->effective_anisotropy;
    if (property == 6) return texture->mip_levels;
    smile_last_error3d = 5;
    return 0;
}

static long long smile_3d_pbr_material_value(SmileMaterial3D* material, long long property)
{
    if (material == 0) { smile_last_error3d = 5; return 0; }
    if (property == 1) return material->mode;
    if (property >= 2 && property <= 5) return material->texture_handles[property - 2];
    if (property == 6) return material->alpha_mode;
    if (property == 7) return material->double_sided;
    if (property == 8) return (long long)llroundf(material->metallic * 1000.0f);
    if (property == 9) return (long long)llroundf(material->roughness * 1000.0f);
    if (property == 10) return (long long)llroundf(material->normal_strength * 1000.0f);
    if (property == 11) return (long long)llroundf(material->occlusion_strength * 1000.0f);
    if (property >= 12 && property <= 14)
        return (long long)llroundf(material->emissive_color[property - 12] * 1000.0f);
    if (property == 15) return (long long)llroundf(material->cutoff * 1000.0f);
    if (property == 16) return material->owner_model_handle != 0;
    smile_last_error3d = 5;
    return 0;
}

static long long smile_3d_light_value(long long query, long long index, long long property)
{
    if (query == 1)
    {
        long long count = smile_ambient_intensity3d > 0.0f ? 1 : 0;
        if (smile_directional_light3d.enabled) count++;
        for (int slot = 0; slot < SMILE_3D_MAX_LOCAL_LIGHTS; ++slot)
            if (smile_local_lights3d[slot].type != 0) count++;
        return count;
    }
    if (query == 2)
    {
        if (property >= 1 && property <= 3)
            return (long long)llroundf(smile_ambient_color3d[property - 1] * 255.0f);
        if (property == 4) return (long long)llroundf(smile_ambient_intensity3d * 1000.0f);
    }
    if (query == 3)
    {
        if (property == 1) return smile_directional_light3d.enabled;
        if (property >= 2 && property <= 4)
            return (long long)llroundf(smile_directional_light3d.direction[property - 2] * 1000.0f);
        if (property >= 5 && property <= 7)
            return (long long)llroundf(smile_directional_light3d.color[property - 5] * 255.0f);
        if (property == 8) return (long long)llroundf(smile_directional_light3d.intensity * 1000.0f);
    }
    if (query == 4 && index >= 0 && index < SMILE_3D_MAX_LOCAL_LIGHTS)
    {
        SmileLocalLight3D* light = &smile_local_lights3d[index];
        if (property == 1) return light->type;
        if (property >= 2 && property <= 4) return (long long)llroundf(light->position[property - 2]);
        if (property >= 5 && property <= 7)
            return (long long)llroundf(light->direction[property - 5] * 1000.0f);
        if (property >= 8 && property <= 10)
            return (long long)llroundf(light->color[property - 8] * 255.0f);
        if (property == 11) return (long long)llroundf(light->intensity * 1000.0f);
        if (property == 12) return (long long)llroundf(light->range);
    }
    smile_last_error3d = 5;
    return 0;
}

static long long smile_3d_model_pbr_value(SmileModel3D* model, long long property, long long index)
{
    if (model == 0) { smile_last_error3d = 5; return 0; }
    if (property == 1) return model->prepared_pbr;
    if (property == 2) return model->prepared_material_count;
    if (property == 3) return model->owned_texture_count;
    if (property == 4 && index >= 0 && index < model->part_count)
    {
        if (!model->prepared_pbr) return 0;
        int slot = model->material_slots[index];
        if (slot >= model->prepared_material_count) { smile_last_error3d = 5; return 0; }
        return smile_3d_material(model->prepared_material_handles[slot]) != 0 ? 1 : 0;
    }
    if (property == 5) return 1;
    if (property == 6) return model->pbr_failure;
    if (property == 7) return model->texture_count;
    smile_last_error3d = 5;
    return 0;
}

static long long smile_3d_create_mesh(unsigned int vertex_count, unsigned int index_count)
{
    int slot;
    SmileMesh3D* mesh;
    if (vertex_count == 0 || vertex_count > 65535 || index_count == 0 ||
        index_count > 196608 || index_count % 3 != 0)
    {
        smile_last_error3d = 2;
        return 0;
    }
    for (slot = 0; slot < SMILE_3D_MAX_MESHES; ++slot)
        if (!smile_meshes3d[slot].active) break;
    if (slot == SMILE_3D_MAX_MESHES)
    {
        smile_last_error3d = 3;
        return 0;
    }
    mesh = &smile_meshes3d[slot];
    if (mesh->generation == 0) mesh->generation = 1;
    mesh->vertices = (SmileVertex3D*)smile_3d_allocate(sizeof(SmileVertex3D) * vertex_count);
    mesh->indices = (unsigned int*)smile_3d_allocate(sizeof(unsigned int) * index_count);
    if (mesh->vertices == 0 || mesh->indices == 0)
    {
        smile_3d_delete_mesh(mesh);
        smile_last_error3d = 4;
        return 0;
    }
    mesh->vertex_count = vertex_count;
    mesh->index_count = index_count;
    mesh->active = 1;
    mesh->committed = 0;
    mesh->explicit_normals = 0;
    mesh->max_joint = 0;
    mesh->in_flight = 0;
    for (unsigned int index = 0; index < vertex_count; ++index)
    {
        mesh->vertices[index].weights[0] = 1.0f;
        mesh->vertices[index].tangent[0] = 1.0f;
        mesh->vertices[index].tangent[3] = 1.0f;
    }
    return smile_3d_handle(SMILE_3D_MESH_HANDLE, slot, mesh->generation);
}

static void smile_3d_cross(float ax, float ay, float az, float bx, float by, float bz,
    float* x, float* y, float* z)
{
    *x = ay * bz - az * by;
    *y = az * bx - ax * bz;
    *z = ax * by - ay * bx;
}

static void smile_3d_normalize(float* x, float* y, float* z)
{
    float length = sqrtf(*x * *x + *y * *y + *z * *z);
    if (length <= 0.000001f) { *x = 0.0f; *y = 1.0f; *z = 0.0f; return; }
    *x /= length; *y /= length; *z /= length;
}

static int smile_3d_commit_mesh(SmileMesh3D* mesh)
{
    unsigned int index;
    if (mesh == 0) { smile_last_error3d = 5; return 0; }
    if (!mesh->explicit_normals)
        for (index = 0; index < mesh->vertex_count; ++index)
            mesh->vertices[index].nx = mesh->vertices[index].ny = mesh->vertices[index].nz = 0.0f;
    for (index = 0; index < mesh->index_count; index += 3)
    {
        unsigned int ia = mesh->indices[index], ib = mesh->indices[index + 1], ic = mesh->indices[index + 2];
        float ux, uy, uz, vx, vy, vz, nx, ny, nz;
        if (ia >= mesh->vertex_count || ib >= mesh->vertex_count || ic >= mesh->vertex_count)
        {
            smile_last_error3d = 6;
            return 0;
        }
        ux = mesh->vertices[ib].x - mesh->vertices[ia].x;
        uy = mesh->vertices[ib].y - mesh->vertices[ia].y;
        uz = mesh->vertices[ib].z - mesh->vertices[ia].z;
        vx = mesh->vertices[ic].x - mesh->vertices[ia].x;
        vy = mesh->vertices[ic].y - mesh->vertices[ia].y;
        vz = mesh->vertices[ic].z - mesh->vertices[ia].z;
        if (!mesh->explicit_normals)
        {
            smile_3d_cross(ux, uy, uz, vx, vy, vz, &nx, &ny, &nz);
            mesh->vertices[ia].nx += nx; mesh->vertices[ia].ny += ny; mesh->vertices[ia].nz += nz;
            mesh->vertices[ib].nx += nx; mesh->vertices[ib].ny += ny; mesh->vertices[ib].nz += nz;
            mesh->vertices[ic].nx += nx; mesh->vertices[ic].ny += ny; mesh->vertices[ic].nz += nz;
        }
    }
    for (index = 0; index < mesh->vertex_count; ++index)
    {
        if (!isfinite(mesh->vertices[index].nx) || !isfinite(mesh->vertices[index].ny) ||
            !isfinite(mesh->vertices[index].nz))
        {
            smile_last_error3d = 6;
            return 0;
        }
        smile_3d_normalize(&mesh->vertices[index].nx, &mesh->vertices[index].ny, &mesh->vertices[index].nz);
    }
    smile_3d_release(mesh->vertex_buffer);
    smile_3d_release(mesh->index_buffer);
    mesh->committed = 1;
    return 1;
}

static void smile_3d_vertex(SmileMesh3D* mesh, unsigned int index, float x, float y, float z)
{
    mesh->vertices[index].x = x; mesh->vertices[index].y = y; mesh->vertices[index].z = z;
}

static void smile_3d_uv(SmileMesh3D* mesh, unsigned int index, float u, float v)
{
    mesh->vertices[index].u = u;
    mesh->vertices[index].v = v;
}

static void smile_3d_normal(SmileMesh3D* mesh, unsigned int index, float x, float y, float z)
{
    mesh->vertices[index].nx = x;
    mesh->vertices[index].ny = y;
    mesh->vertices[index].nz = z;
    mesh->explicit_normals = 1;
}

static int smile_3d_skin(SmileMesh3D* mesh, unsigned int index,
    long long joint0, long long joint1, long long joint2, long long joint3,
    long long weight0, long long weight1, long long weight2, long long weight3)
{
    long long joints[4] = { joint0, joint1, joint2, joint3 };
    long long weights[4] = { weight0, weight1, weight2, weight3 };
    long long total = weight0 + weight1 + weight2 + weight3;
    if (mesh == 0 || index >= mesh->vertex_count || total != 1000)
    {
        smile_last_error3d = 34;
        return 0;
    }
    for (int influence = 0; influence < 4; ++influence)
    {
        if (joints[influence] < 0 || joints[influence] >= SMILE_3D_MAX_BONES ||
            weights[influence] < 0 || weights[influence] > 1000)
        {
            smile_last_error3d = 34;
            return 0;
        }
        mesh->vertices[index].joints[influence] = (float)joints[influence];
        mesh->vertices[index].weights[influence] = (float)weights[influence] / 1000.0f;
        if (weights[influence] != 0 && joints[influence] > mesh->max_joint)
            mesh->max_joint = (unsigned char)joints[influence];
    }
    mesh->committed = 0;
    return 1;
}

static void smile_3d_triangle(SmileMesh3D* mesh, unsigned int triangle,
    unsigned int a, unsigned int b, unsigned int c)
{
    unsigned int offset = triangle * 3;
    mesh->indices[offset] = a; mesh->indices[offset + 1] = b; mesh->indices[offset + 2] = c;
}

static long long smile_3d_cube(float size)
{
    static const float positions[72] = {
        -1,-1,-1, 1,-1,-1, 1,1,-1, -1,1,-1,
        -1,-1,1, -1,1,1, 1,1,1, 1,-1,1,
        -1,-1,-1, -1,1,-1, -1,1,1, -1,-1,1,
        1,-1,-1, 1,-1,1, 1,1,1, 1,1,-1,
        -1,1,-1, 1,1,-1, 1,1,1, -1,1,1,
        -1,-1,-1, -1,-1,1, 1,-1,1, 1,-1,-1
    };
    long long handle = smile_3d_create_mesh(24, 36);
    SmileMesh3D* mesh = smile_3d_mesh(handle);
    unsigned int face, vertex;
    if (mesh == 0) return 0;
    for (vertex = 0; vertex < 24; ++vertex)
        smile_3d_vertex(mesh, vertex, positions[vertex * 3] * size * 0.5f,
            positions[vertex * 3 + 1] * size * 0.5f, positions[vertex * 3 + 2] * size * 0.5f);
    for (face = 0; face < 6; ++face)
    {
        smile_3d_uv(mesh, face * 4, 0.0f, 1.0f);
        smile_3d_uv(mesh, face * 4 + 1, 0.0f, 0.0f);
        smile_3d_uv(mesh, face * 4 + 2, 1.0f, 0.0f);
        smile_3d_uv(mesh, face * 4 + 3, 1.0f, 1.0f);
        smile_3d_triangle(mesh, face * 2, face * 4, face * 4 + 1, face * 4 + 2);
        smile_3d_triangle(mesh, face * 2 + 1, face * 4, face * 4 + 2, face * 4 + 3);
    }
    return smile_3d_commit_mesh(mesh) ? handle : 0;
}

static long long smile_3d_plane(float width, float depth)
{
    long long handle = smile_3d_create_mesh(4, 6);
    SmileMesh3D* mesh = smile_3d_mesh(handle);
    if (mesh == 0) return 0;
    smile_3d_vertex(mesh, 0, -width * 0.5f, 0, -depth * 0.5f);
    smile_3d_vertex(mesh, 1, -width * 0.5f, 0, depth * 0.5f);
    smile_3d_vertex(mesh, 2, width * 0.5f, 0, depth * 0.5f);
    smile_3d_vertex(mesh, 3, width * 0.5f, 0, -depth * 0.5f);
    smile_3d_uv(mesh, 0, 0, 0); smile_3d_uv(mesh, 1, 0, 1);
    smile_3d_uv(mesh, 2, 1, 1); smile_3d_uv(mesh, 3, 1, 0);
    smile_3d_triangle(mesh, 0, 0, 1, 2); smile_3d_triangle(mesh, 1, 0, 2, 3);
    return smile_3d_commit_mesh(mesh) ? handle : 0;
}

static long long smile_3d_pyramid(float size, float height)
{
    long long handle = smile_3d_create_mesh(5, 18);
    SmileMesh3D* mesh = smile_3d_mesh(handle);
    if (mesh == 0) return 0;
    smile_3d_vertex(mesh, 0, -size * 0.5f, -height * 0.5f, -size * 0.5f);
    smile_3d_vertex(mesh, 1, size * 0.5f, -height * 0.5f, -size * 0.5f);
    smile_3d_vertex(mesh, 2, size * 0.5f, -height * 0.5f, size * 0.5f);
    smile_3d_vertex(mesh, 3, -size * 0.5f, -height * 0.5f, size * 0.5f);
    smile_3d_vertex(mesh, 4, 0, height * 0.5f, 0);
    smile_3d_uv(mesh, 0, 0, 1); smile_3d_uv(mesh, 1, 1, 1);
    smile_3d_uv(mesh, 2, 1, 0); smile_3d_uv(mesh, 3, 0, 0); smile_3d_uv(mesh, 4, 0.5f, 0);
    smile_3d_triangle(mesh, 0, 0, 2, 1); smile_3d_triangle(mesh, 1, 0, 3, 2);
    smile_3d_triangle(mesh, 2, 0, 1, 4); smile_3d_triangle(mesh, 3, 1, 2, 4);
    smile_3d_triangle(mesh, 4, 2, 3, 4); smile_3d_triangle(mesh, 5, 3, 0, 4);
    return smile_3d_commit_mesh(mesh) ? handle : 0;
}

static long long smile_3d_sphere(float radius, int segments, int rings)
{
    long long handle;
    SmileMesh3D* mesh;
    int ring, segment;
    unsigned int triangle = 0;
    if (segments < 6) segments = 6; if (segments > 48) segments = 48;
    if (rings < 3) rings = 3; if (rings > 32) rings = 32;
    handle = smile_3d_create_mesh((unsigned int)((rings + 1) * (segments + 1)),
        (unsigned int)(rings * segments * 6));
    mesh = smile_3d_mesh(handle); if (mesh == 0) return 0;
    for (ring = 0; ring <= rings; ++ring)
    {
        float latitude = -SMILE_3D_PI * 0.5f + SMILE_3D_PI * (float)ring / (float)rings;
        float ring_radius = cosf(latitude) * radius;
        float y = sinf(latitude) * radius;
        for (segment = 0; segment <= segments; ++segment)
        {
            float longitude = 2.0f * SMILE_3D_PI * (float)segment / (float)segments;
            unsigned int vertex = (unsigned int)(ring * (segments + 1) + segment);
            smile_3d_vertex(mesh, vertex, cosf(longitude) * ring_radius, y, sinf(longitude) * ring_radius);
            smile_3d_uv(mesh, vertex, (float)segment / (float)segments,
                1.0f - (float)ring / (float)rings);
        }
    }
    for (ring = 0; ring < rings; ++ring)
        for (segment = 0; segment < segments; ++segment)
        {
            unsigned int a = (unsigned int)(ring * (segments + 1) + segment);
            unsigned int b = a + 1, c = a + (unsigned int)(segments + 1), d = c + 1;
            smile_3d_triangle(mesh, triangle++, a, c, b);
            smile_3d_triangle(mesh, triangle++, b, c, d);
        }
    return smile_3d_commit_mesh(mesh) ? handle : 0;
}

static long long smile_3d_cylinder(float radius, float height, int segments)
{
    long long handle;
    SmileMesh3D* mesh;
    int segment;
    unsigned int triangle = 0;
    if (segments < 6) segments = 6; if (segments > 64) segments = 64;
    handle = smile_3d_create_mesh((unsigned int)(segments * 2 + 2), (unsigned int)(segments * 12));
    mesh = smile_3d_mesh(handle); if (mesh == 0) return 0;
    for (segment = 0; segment < segments; ++segment)
    {
        float angle = 2.0f * SMILE_3D_PI * (float)segment / (float)segments;
        float x = cosf(angle) * radius, z = sinf(angle) * radius;
        smile_3d_vertex(mesh, (unsigned int)segment, x, -height * 0.5f, z);
        smile_3d_vertex(mesh, (unsigned int)(segment + segments), x, height * 0.5f, z);
        smile_3d_uv(mesh, (unsigned int)segment, (float)segment / (float)segments, 1.0f);
        smile_3d_uv(mesh, (unsigned int)(segment + segments), (float)segment / (float)segments, 0.0f);
    }
    smile_3d_vertex(mesh, (unsigned int)(segments * 2), 0, -height * 0.5f, 0);
    smile_3d_vertex(mesh, (unsigned int)(segments * 2 + 1), 0, height * 0.5f, 0);
    for (segment = 0; segment < segments; ++segment)
    {
        unsigned int next = (unsigned int)((segment + 1) % segments);
        unsigned int bottom = (unsigned int)segment, top = bottom + (unsigned int)segments;
        unsigned int next_top = next + (unsigned int)segments;
        smile_3d_triangle(mesh, triangle++, bottom, top, next);
        smile_3d_triangle(mesh, triangle++, next, top, next_top);
        smile_3d_triangle(mesh, triangle++, (unsigned int)(segments * 2), next, bottom);
        smile_3d_triangle(mesh, triangle++, (unsigned int)(segments * 2 + 1), top, next_top);
    }
    return smile_3d_commit_mesh(mesh) ? handle : 0;
}

static long long smile_3d_torus(float major_radius, float minor_radius, int major_segments, int minor_segments)
{
    long long handle;
    SmileMesh3D* mesh;
    int major, minor;
    unsigned int triangle = 0;
    if (major_segments < 6) major_segments = 6; if (major_segments > 48) major_segments = 48;
    if (minor_segments < 4) minor_segments = 4; if (minor_segments > 24) minor_segments = 24;
    handle = smile_3d_create_mesh((unsigned int)((major_segments + 1) * (minor_segments + 1)),
        (unsigned int)(major_segments * minor_segments * 6));
    mesh = smile_3d_mesh(handle); if (mesh == 0) return 0;
    for (major = 0; major <= major_segments; ++major)
    {
        float a = 2.0f * SMILE_3D_PI * (float)major / (float)major_segments;
        for (minor = 0; minor <= minor_segments; ++minor)
        {
            float b = 2.0f * SMILE_3D_PI * (float)minor / (float)minor_segments;
            float ring = major_radius + minor_radius * cosf(b);
            unsigned int vertex = (unsigned int)(major * (minor_segments + 1) + minor);
            smile_3d_vertex(mesh, vertex, cosf(a) * ring, minor_radius * sinf(b), sinf(a) * ring);
            smile_3d_uv(mesh, vertex, (float)major / (float)major_segments,
                (float)minor / (float)minor_segments);
        }
    }
    for (major = 0; major < major_segments; ++major)
        for (minor = 0; minor < minor_segments; ++minor)
        {
            unsigned int a = (unsigned int)(major * (minor_segments + 1) + minor);
            unsigned int b = a + 1, c = a + (unsigned int)(minor_segments + 1), d = c + 1;
            smile_3d_triangle(mesh, triangle++, a, c, b);
            smile_3d_triangle(mesh, triangle++, b, c, d);
        }
    return smile_3d_commit_mesh(mesh) ? handle : 0;
}

static long long smile_3d_create_primitive(int kind, float first, float second, int segments, int rings)
{
    if (first <= 0.0f || (kind != SMILE_3D_CUBE && second <= 0.0f))
    {
        smile_last_error3d = 7; return 0;
    }
    switch (kind)
    {
        case SMILE_3D_CUBE: return smile_3d_cube(first);
        case SMILE_3D_PLANE: return smile_3d_plane(first, second);
        case SMILE_3D_PYRAMID: return smile_3d_pyramid(first, second);
        case SMILE_3D_SPHERE: return smile_3d_sphere(first, segments, rings);
        case SMILE_3D_CYLINDER: return smile_3d_cylinder(first, second, segments);
        case SMILE_3D_TORUS: return smile_3d_torus(first, second, segments, rings);
        default: smile_last_error3d = 8; return 0;
    }
}

static long long smile_3d_create_object_with_material(long long mesh_handle,
    long long default_material_handle)
{
    int slot;
    SmileObject3D* object;
    if (smile_3d_mesh(mesh_handle) == 0 ||
        (default_material_handle != 0 && smile_3d_material(default_material_handle) == 0))
    { smile_last_error3d = 5; return 0; }
    for (slot = 0; slot < SMILE_3D_MAX_OBJECTS; ++slot)
        if (!smile_objects3d[slot].active) break;
    if (slot == SMILE_3D_MAX_OBJECTS) { smile_last_error3d = 9; return 0; }
    object = &smile_objects3d[slot];
    if (object->generation == 0) object->generation = 1;
    object->active = 1; object->visible = 1;
    object->casts_shadow = 1; object->receives_shadow = 1;
    object->mesh_handle = mesh_handle;
    object->material_handle = default_material_handle;
    object->default_material_handle = default_material_handle;
    object->animator_handle = 0;
    object->position[0] = object->position[1] = object->position[2] = 0.0f;
    object->rotation[0] = object->rotation[1] = object->rotation[2] = 0.0f;
    object->scale[0] = object->scale[1] = object->scale[2] = 1.0f;
    object->color[0] = object->color[1] = object->color[2] = 1.0f; object->color[3] = 1.0f;
    return smile_3d_object_handle(slot, object->generation);
}

static long long smile_3d_create_object(long long mesh_handle)
{
    return smile_3d_create_object_with_material(mesh_handle, 0);
}

static unsigned int smile_3d_read_u32(const unsigned char* value)
{
    return (unsigned int)value[0] | ((unsigned int)value[1] << 8) |
        ((unsigned int)value[2] << 16) | ((unsigned int)value[3] << 24);
}

static unsigned short smile_3d_read_u16(const unsigned char* value)
{
    return (unsigned short)((unsigned int)value[0] | ((unsigned int)value[1] << 8));
}

static float smile_3d_read_float(const unsigned char* value)
{
    union { unsigned int bits; float number; } result;
    result.bits = smile_3d_read_u32(value);
    return result.number;
}

static unsigned int smile_3d_checksum(const unsigned char* value, unsigned int length)
{
    unsigned int result = 2166136261U;
    unsigned int index;
    for (index = 0; index < length; ++index)
    {
        result ^= value[index];
        result *= 16777619U;
    }
    return result;
}

static unsigned int smile_3d_fourcc(char a, char b, char c, char d)
{
    return (unsigned int)(unsigned char)a | ((unsigned int)(unsigned char)b << 8) |
        ((unsigned int)(unsigned char)c << 16) | ((unsigned int)(unsigned char)d << 24);
}

static int smile_3d_model_v2_known_chunk(unsigned int id)
{
    return id == smile_3d_fourcc('S','T','R','0') || id == smile_3d_fourcc('P','A','R','T') ||
        id == smile_3d_fourcc('V','E','R','T') || id == smile_3d_fourcc('I','N','D','X') ||
        id == smile_3d_fourcc('M','A','T','L') || id == smile_3d_fourcc('T','E','X','R') ||
        id == smile_3d_fourcc('B','O','N','D') || id == smile_3d_fourcc('N','O','D','E') ||
        id == smile_3d_fourcc('S','K','I','N') || id == smile_3d_fourcc('S','K','E','L') ||
        id == smile_3d_fourcc('C','L','I','P') || id == smile_3d_fourcc('T','R','A','K') ||
        id == smile_3d_fourcc('A','F','R','M') || id == smile_3d_fourcc('E','V','N','T') ||
        id == smile_3d_fourcc('S','O','C','K') || id == smile_3d_fourcc('R','O','O','T');
}

static const SmileModelChunkV2* smile_3d_model_v2_chunk(
    const SmileModelChunkV2* chunks, unsigned int count, unsigned int id)
{
    for (unsigned int index = 0; index < count; ++index)
        if (chunks[index].id == id) return &chunks[index];
    return 0;
}

static int smile_3d_model_v2_utf8(const unsigned char* value, unsigned int length)
{
    unsigned int index = 0;
    while (index < length)
    {
        unsigned int first = value[index++];
        if (first < 0x80) continue;
        if (first >= 0xC2 && first <= 0xDF)
        {
            if (index >= length || (value[index++] & 0xC0) != 0x80) return 0;
        }
        else if (first >= 0xE0 && first <= 0xEF)
        {
            unsigned int second;
            if (index + 1 >= length) return 0;
            second = value[index++];
            if ((second & 0xC0) != 0x80 || (first == 0xE0 && second < 0xA0) ||
                (first == 0xED && second >= 0xA0) || (value[index++] & 0xC0) != 0x80) return 0;
        }
        else if (first >= 0xF0 && first <= 0xF4)
        {
            unsigned int second;
            if (index + 2 >= length) return 0;
            second = value[index++];
            if ((second & 0xC0) != 0x80 || (first == 0xF0 && second < 0x90) ||
                (first == 0xF4 && second >= 0x90) || (value[index++] & 0xC0) != 0x80 ||
                (value[index++] & 0xC0) != 0x80) return 0;
        }
        else return 0;
    }
    return 1;
}

static int smile_3d_model_v2_strings(const unsigned char* bytes, const SmileModelChunkV2* strings)
{
    unsigned int offset = 0;
    unsigned int count = 0;
    if (strings == 0 || strings->length == 0 || bytes[strings->offset] != 0) return 0;
    while (offset < strings->length)
    {
        unsigned int end = offset;
        while (end < strings->length && bytes[strings->offset + end] != 0) end++;
        if (end >= strings->length || !smile_3d_model_v2_utf8(bytes + strings->offset + offset, end - offset)) return 0;
        count++;
        offset = end + 1;
    }
    return offset == strings->length && count == strings->count;
}

static const char* smile_3d_model_v2_string(const unsigned char* bytes,
    const SmileModelChunkV2* strings, unsigned int offset, unsigned int* length)
{
    unsigned int end;
    if (offset >= strings->length || (offset != 0 && bytes[strings->offset + offset - 1] != 0)) return 0;
    end = offset;
    while (end < strings->length && bytes[strings->offset + end] != 0) end++;
    if (end >= strings->length) return 0;
    if (length != 0) *length = end - offset;
    return (const char*)(bytes + strings->offset + offset);
}

static int smile_3d_model_v2_texture_path(const char* value, unsigned int length)
{
    unsigned int segment = 0;
    if (value == 0 || length == 0 || length > 1024 || value[0] == '/') return 0;
    for (unsigned int index = 0; index <= length; ++index)
    {
        unsigned int character = index == length ? '/' : (unsigned char)value[index];
        if (character == '/')
        {
            unsigned int segment_length = index - segment;
            if (segment_length == 0 || (segment_length == 1 && value[segment] == '.') ||
                (segment_length == 2 && value[segment] == '.' && value[segment + 1] == '.')) return 0;
            segment = index + 1;
        }
        else if (character < 32 || character == 127 || character == '\\' || character == ':' ||
            character == '*' || character == '?' || character == '[' || character == ']' ||
            character == '{' || character == '}' || character == '!' || character == ';' ||
            character == '"' || character == '<' || character == '>' || character == '|') return 0;
    }
    return 1;
}

static unsigned int smile_3d_model_v2_string_hash(const SmileModel3D* model, unsigned int offset)
{
    unsigned int result = 2166136261U;
    if (model == 0 || model->strings == 0 || offset >= model->string_bytes) return 0;
    while (offset < model->string_bytes && model->strings[offset] != 0)
    {
        result ^= (unsigned char)model->strings[offset++];
        result *= 16777619U;
    }
    return result;
}

static int smile_3d_model_v2_finite_range(float value, float minimum, float maximum)
{
    return isfinite(value) && value >= minimum && value <= maximum;
}

static int smile_3d_model_v2_bounds(const unsigned char* value, float* output)
{
    for (unsigned int index = 0; index < 6; ++index)
    {
        output[index] = smile_3d_read_float(value + index * 4);
        if (!isfinite(output[index])) return 0;
    }
    return smile_3d_read_u32(value + 24) == 0 && smile_3d_read_u32(value + 28) == 0 &&
        output[0] <= output[3] && output[1] <= output[4] && output[2] <= output[5];
}

static int smile_3d_model_v2_animation(const unsigned char* bytes,
    const SmileModelChunkV2* chunks, unsigned int chunk_count,
    const SmileModelChunkV2* strings, unsigned int vertex_count,
    SmileModelChunkV2* output, unsigned int* animation_bytes)
{
    const unsigned int ids[9] = {
        smile_3d_fourcc('N','O','D','E'), smile_3d_fourcc('S','K','I','N'),
        smile_3d_fourcc('S','K','E','L'), smile_3d_fourcc('C','L','I','P'),
        smile_3d_fourcc('T','R','A','K'), smile_3d_fourcc('A','F','R','M'),
        smile_3d_fourcc('E','V','N','T'), smile_3d_fourcc('S','O','C','K'),
        smile_3d_fourcc('R','O','O','T')
    };
    const unsigned int strides[9] = { 64, 16, 80, 40, 48, 4, 20, 64, 24 };
    unsigned int present = 0;
    unsigned int index;
    *animation_bytes = 0;
    for (index = 0; index < 9; ++index)
    {
        const SmileModelChunkV2* chunk = smile_3d_model_v2_chunk(chunks, chunk_count, ids[index]);
        if (chunk == 0) continue;
        output[index] = *chunk;
        present++;
        if (chunk->flags != 1 || chunk->stride != strides[index] ||
            chunk->count > UINT_MAX / strides[index] || chunk->length != chunk->count * strides[index]) return -1;
        *animation_bytes += chunk->length;
    }
    if (present == 0) return 0;
    if (present != 9 || output[0].count == 0 || output[0].count > SMILE_3D_MAX_MODEL_ANIMATION_NODES ||
        output[1].count != vertex_count || output[2].count == 0 ||
        output[2].count > SMILE_3D_MAX_MODEL_ANIMATION_BONES || output[3].count == 0 ||
        output[3].count > SMILE_3D_MAX_MODEL_ANIMATION_CLIPS ||
        output[4].count > SMILE_3D_MAX_MODEL_ANIMATION_NODES * SMILE_3D_MAX_MODEL_ANIMATION_CLIPS ||
        output[5].count == 0 || output[6].count > SMILE_3D_MAX_MODEL_ANIMATION_CLIPS * 64 ||
        output[7].count > SMILE_3D_MAX_MODEL_ANIMATION_SOCKETS ||
        output[8].count > SMILE_3D_MAX_MODEL_ANIMATION_CLIPS) return -1;

    for (index = 0; index < output[0].count; ++index)
    {
        const unsigned char* record = bytes + output[0].offset + index * 64;
        int parent = (int)smile_3d_read_u32(record + 4);
        unsigned int flags = smile_3d_read_u32(record + 8);
        float quaternion_length = 0.0f;
        if (smile_3d_model_v2_string(bytes, strings, smile_3d_read_u32(record), 0) == 0 ||
            parent < -1 || parent >= (int)index || (flags & ~3U) != 0 ||
            smile_3d_read_u32(record + 12) != 0 || smile_3d_read_u32(record + 56) != 0 ||
            smile_3d_read_u32(record + 60) != 0) return -1;
        for (unsigned int component = 0; component < 10; ++component)
        {
            float value = smile_3d_read_float(record + 16 + component * 4);
            if (!isfinite(value)) return -1;
            if (component >= 3 && component < 7) quaternion_length += value * value;
        }
        if (fabsf(quaternion_length - 1.0f) > 0.0001f ||
            smile_3d_read_float(record + 44) <= 0.0f ||
            fabsf(smile_3d_read_float(record + 44) - smile_3d_read_float(record + 48)) > 0.0001f ||
            fabsf(smile_3d_read_float(record + 44) - smile_3d_read_float(record + 52)) > 0.0001f) return -1;
    }

    {
        unsigned int root_bones = 0;
        for (index = 0; index < output[2].count; ++index)
        {
            const unsigned char* record = bytes + output[2].offset + index * 80;
            unsigned int node = smile_3d_read_u32(record);
            int parent = (int)smile_3d_read_u32(record + 4);
            if (node >= output[0].count || parent < -1 || parent >= (int)index ||
                smile_3d_read_u32(record + 8) != 0 || smile_3d_read_u32(record + 12) != 0) return -1;
            if (parent < 0) root_bones++;
            for (unsigned int component = 0; component < 16; ++component)
                if (!isfinite(smile_3d_read_float(record + 16 + component * 4))) return -1;
        }
        if (root_bones != 1) return -1;
    }

    for (index = 0; index < output[1].count; ++index)
    {
        const unsigned char* record = bytes + output[1].offset + index * 16;
        unsigned int total = 0;
        for (unsigned int influence = 0; influence < 4; ++influence)
        {
            unsigned int joint = smile_3d_read_u16(record + influence * 2);
            unsigned int weight = smile_3d_read_u16(record + 8 + influence * 2);
            if (joint >= output[2].count || (weight == 0 && joint != 0)) return -1;
            total += weight;
        }
        if (total != 65535U) return -1;
    }

    for (index = 0; index < output[3].count; ++index)
    {
        const unsigned char* record = bytes + output[3].offset + index * 40;
        unsigned int duration = smile_3d_read_u32(record + 4);
        unsigned int rate = smile_3d_read_u32(record + 8);
        unsigned int samples = smile_3d_read_u32(record + 12);
        unsigned int first_track = smile_3d_read_u32(record + 16);
        unsigned int tracks = smile_3d_read_u32(record + 20);
        unsigned int first_event = smile_3d_read_u32(record + 24);
        unsigned int events = smile_3d_read_u32(record + 28);
        unsigned int flags = smile_3d_read_u32(record + 32);
        unsigned int root = smile_3d_read_u32(record + 36);
        unsigned int minimum_samples = duration * rate / 1000U + 1U;
        unsigned int maximum_samples = (duration * rate + 999U) / 1000U + 1U;
        if (smile_3d_model_v2_string(bytes, strings, smile_3d_read_u32(record), 0) == 0 ||
            duration == 0 || duration > 120000 || rate < 15 || rate > 60 ||
            samples < minimum_samples || samples > maximum_samples ||
            first_track > output[4].count || tracks > output[4].count - first_track ||
            first_event > output[6].count || events > 64 || events > output[6].count - first_event ||
            flags > 1 || (root != 0xFFFFFFFFU && root >= output[8].count)) return -1;
    }

    for (index = 0; index < output[4].count; ++index)
    {
        const unsigned char* record = bytes + output[4].offset + index * 48;
        unsigned int clip = smile_3d_read_u32(record);
        unsigned int node = smile_3d_read_u32(record + 4);
        unsigned int flags = smile_3d_read_u32(record + 8);
        if (clip >= output[3].count || node >= output[0].count || (flags & ~63U) != 0 ||
            smile_3d_read_u32(record + 12) != 0 || smile_3d_read_u32(record + 40) != 0 ||
            smile_3d_read_u32(record + 44) != 0) return -1;
        const unsigned char* clip_record = bytes + output[3].offset + clip * 40;
        unsigned int sample_count = smile_3d_read_u32(clip_record + 12);
        unsigned int first_track = smile_3d_read_u32(clip_record + 16);
        unsigned int track_count = smile_3d_read_u32(clip_record + 20);
        if (index < first_track || index >= first_track + track_count) return -1;
        const unsigned int component_counts[3] = { 3, 4, 3 };
        const unsigned int present_flags[3] = { 1, 4, 16 };
        const unsigned int sampled_flags[3] = { 2, 8, 32 };
        for (unsigned int channel = 0; channel < 3; ++channel)
        {
            unsigned int first = smile_3d_read_u32(record + 16 + channel * 8);
            unsigned int count = smile_3d_read_u32(record + 20 + channel * 8);
            unsigned int expected = (flags & sampled_flags[channel]) != 0 ? sample_count : 1;
            if ((flags & present_flags[channel]) == 0)
            {
                if (first != 0xFFFFFFFFU || count != 0) return -1;
            }
            else if (count != expected || first > output[5].count ||
                count > (output[5].count - first) / component_counts[channel]) return -1;
        }
    }
    for (index = 0; index < output[5].count; ++index)
        if (!isfinite(smile_3d_read_float(bytes + output[5].offset + index * 4))) return -1;

    {
        int prior_clip = -1;
        unsigned int prior_time = 0, prior_order = 0;
        for (index = 0; index < output[6].count; ++index)
        {
            const unsigned char* record = bytes + output[6].offset + index * 20;
            unsigned int clip = smile_3d_read_u32(record);
            unsigned int time = smile_3d_read_u32(record + 4);
            unsigned int order = smile_3d_read_u32(record + 16);
            if (clip >= output[3].count || time > smile_3d_read_u32(bytes + output[3].offset + clip * 40 + 4) ||
                smile_3d_model_v2_string(bytes, strings, smile_3d_read_u32(record + 8), 0) == 0 ||
                (prior_clip == (int)clip && (time < prior_time || (time == prior_time && order <= prior_order))) ||
                prior_clip > (int)clip) return -1;
            prior_clip = (int)clip; prior_time = time; prior_order = order;
        }
    }
    for (index = 0; index < output[7].count; ++index)
    {
        const unsigned char* record = bytes + output[7].offset + index * 64;
        if (smile_3d_model_v2_string(bytes, strings, smile_3d_read_u32(record), 0) == 0 ||
            smile_3d_read_u32(record + 4) >= output[0].count || smile_3d_read_u32(record + 8) != 0 ||
            smile_3d_read_u32(record + 12) != 0 || smile_3d_read_u32(record + 56) != 0 ||
            smile_3d_read_u32(record + 60) != 0) return -1;
        for (unsigned int component = 0; component < 10; ++component)
            if (!isfinite(smile_3d_read_float(record + 16 + component * 4))) return -1;
    }
    for (index = 0; index < output[8].count; ++index)
    {
        const unsigned char* record = bytes + output[8].offset + index * 24;
        unsigned int clip = smile_3d_read_u32(record);
        if (clip >= output[3].count || smile_3d_read_u32(record + 4) >= output[0].count ||
            smile_3d_read_u32(record + 8) < 1 || smile_3d_read_u32(record + 8) > 7 ||
            smile_3d_read_u32(record + 12) > 1 || smile_3d_read_u32(record + 16) > 1 ||
            smile_3d_read_u32(record + 20) != 0 ||
            smile_3d_read_u32(bytes + output[3].offset + clip * 40 + 36) != index) return -1;
    }
    return 1;
}

static long long smile_3d_load_model_v2(const unsigned char* bytes, unsigned int size)
{
    SmileModelChunkV2 chunks[SMILE_3D_MAX_MODEL_CHUNKS] = {};
    SmileModelPartV2 parts[SMILE_3D_MAX_MODEL_PARTS] = {};
    SmileModelMaterialV2 materials[SMILE_3D_MAX_MODEL_MATERIALS] = {};
    SmileModelTextureV2 textures[SMILE_3D_MAX_MODEL_TEXTURES] = {};
    SmileModelChunkV2 animation_chunks[9] = {};
    SmileModelChunkV2 compact_animation_chunks[9] = {};
    float bounds[6] = {};
    float part_bounds[SMILE_3D_MAX_MODEL_PARTS][6] = {};
    long long mesh_handles[SMILE_3D_MAX_MODEL_PARTS] = {};
    char* strings_copy = 0;
    unsigned char* animation_copy = 0;
    unsigned int animation_bytes = 0;
    unsigned int animation_resident_bytes = 0;
    int animation_status = 0;
    unsigned int chunk_count, directory_end, part_count, vertex_count, index_count, material_count, texture_count;
    unsigned int model_name_offset, part_index, model_slot;
    const SmileModelChunkV2* strings;
    const SmileModelChunkV2* part_chunk;
    const SmileModelChunkV2* vertex_chunk;
    const SmileModelChunkV2* index_chunk;
    const SmileModelChunkV2* material_chunk;
    const SmileModelChunkV2* texture_chunk;
    const SmileModelChunkV2* bounds_chunk;
    long long result = 0;

    if (size < 64 || smile_3d_read_u16(bytes + 6) != 64 || smile_3d_read_u32(bytes + 8) != 0 ||
        smile_3d_read_u32(bytes + 12) != size || smile_3d_read_u32(bytes + 16) != smile_3d_checksum(bytes + 64, size - 64) ||
        smile_3d_read_u32(bytes + 24) != 64 || smile_3d_read_u32(bytes + 28) != 32 ||
        smile_3d_read_u32(bytes + 56) != 0 || smile_3d_read_u32(bytes + 60) != 0)
    {
        smile_last_error3d = 24;
        return 0;
    }
    chunk_count = smile_3d_read_u32(bytes + 20);
    if (chunk_count == 0 || chunk_count > SMILE_3D_MAX_MODEL_CHUNKS ||
        (unsigned long long)64 + (unsigned long long)chunk_count * 32 > size)
    {
        smile_last_error3d = 24;
        return 0;
    }
    directory_end = 64 + chunk_count * 32;
    directory_end = (directory_end + 3U) & ~3U;
    for (unsigned int chunk_index = 0; chunk_index < chunk_count; ++chunk_index)
    {
        const unsigned char* entry = bytes + 64 + chunk_index * 32;
        SmileModelChunkV2* chunk = &chunks[chunk_index];
        for (unsigned int character = 0; character < 4; ++character)
            if (entry[character] < 32 || entry[character] > 126)
            {
                smile_last_error3d = 24;
                return 0;
            }
        chunk->id = smile_3d_read_u32(entry);
        chunk->flags = smile_3d_read_u32(entry + 4);
        chunk->offset = smile_3d_read_u32(entry + 8);
        chunk->length = smile_3d_read_u32(entry + 12);
        chunk->count = smile_3d_read_u32(entry + 16);
        chunk->stride = smile_3d_read_u32(entry + 20);
        if ((chunk->flags & ~1U) != 0 || smile_3d_read_u32(entry + 24) != 0 || smile_3d_read_u32(entry + 28) != 0 ||
            chunk->offset < directory_end || (chunk->offset & 3U) != 0 || chunk->offset > size ||
            chunk->length > size - chunk->offset || (!smile_3d_model_v2_known_chunk(chunk->id) && (chunk->flags & 1U) == 0))
        {
            smile_last_error3d = 24;
            return 0;
        }
        for (unsigned int prior = 0; prior < chunk_index; ++prior)
        {
            if (chunks[prior].id == chunk->id ||
                (chunk->length != 0 && chunks[prior].length != 0 &&
                 chunk->offset < chunks[prior].offset + chunks[prior].length &&
                 chunks[prior].offset < chunk->offset + chunk->length))
            {
                smile_last_error3d = 24;
                return 0;
            }
        }
    }

    strings = smile_3d_model_v2_chunk(chunks, chunk_count, smile_3d_fourcc('S','T','R','0'));
    part_chunk = smile_3d_model_v2_chunk(chunks, chunk_count, smile_3d_fourcc('P','A','R','T'));
    vertex_chunk = smile_3d_model_v2_chunk(chunks, chunk_count, smile_3d_fourcc('V','E','R','T'));
    index_chunk = smile_3d_model_v2_chunk(chunks, chunk_count, smile_3d_fourcc('I','N','D','X'));
    material_chunk = smile_3d_model_v2_chunk(chunks, chunk_count, smile_3d_fourcc('M','A','T','L'));
    texture_chunk = smile_3d_model_v2_chunk(chunks, chunk_count, smile_3d_fourcc('T','E','X','R'));
    bounds_chunk = smile_3d_model_v2_chunk(chunks, chunk_count, smile_3d_fourcc('B','O','N','D'));
    part_count = smile_3d_read_u32(bytes + 36);
    vertex_count = smile_3d_read_u32(bytes + 40);
    index_count = smile_3d_read_u32(bytes + 44);
    material_count = smile_3d_read_u32(bytes + 48);
    texture_count = smile_3d_read_u32(bytes + 52);
    model_name_offset = smile_3d_read_u32(bytes + 32);
    if (strings == 0 || part_chunk == 0 || vertex_chunk == 0 || index_chunk == 0 || material_chunk == 0 ||
        texture_chunk == 0 || bounds_chunk == 0 || strings->flags != 0 || part_chunk->flags != 0 ||
        vertex_chunk->flags != 0 || index_chunk->flags != 0 || material_chunk->flags != 0 ||
        texture_chunk->flags != 0 || bounds_chunk->flags != 0 ||
        part_count == 0 || part_count > SMILE_3D_MAX_MODEL_PARTS ||
        vertex_count == 0 || vertex_count > SMILE_3D_MAX_MODEL_VERTICES ||
        index_count == 0 || index_count > SMILE_3D_MAX_MODEL_INDICES || index_count % 3 != 0 ||
        material_count == 0 || material_count > SMILE_3D_MAX_MODEL_MATERIALS || texture_count > SMILE_3D_MAX_MODEL_TEXTURES ||
        strings->count == 0 || strings->stride != 0 || strings->length == 0 ||
        part_chunk->count != part_count || part_chunk->stride != 32 || part_chunk->length != part_count * 32U ||
        vertex_chunk->count != vertex_count || vertex_chunk->stride != 48 || vertex_chunk->length != vertex_count * 48U ||
        index_chunk->count != index_count || index_chunk->stride != 4 || index_chunk->length != index_count * 4U ||
        material_chunk->count != material_count || material_chunk->stride != 80 || material_chunk->length != material_count * 80U ||
        texture_chunk->count != texture_count || texture_chunk->stride != 16 || texture_chunk->length != texture_count * 16U ||
        bounds_chunk->count != part_count + 1 || bounds_chunk->stride != 32 || bounds_chunk->length != (part_count + 1) * 32U ||
        !smile_3d_model_v2_strings(bytes, strings) ||
        smile_3d_model_v2_string(bytes, strings, model_name_offset, 0) == 0 ||
        !smile_3d_model_v2_bounds(bytes + bounds_chunk->offset, bounds))
    {
        smile_last_error3d = 24;
        return 0;
    }

    for (unsigned int texture_index = 0; texture_index < texture_count; ++texture_index)
    {
        const unsigned char* record = bytes + texture_chunk->offset + texture_index * 16;
        unsigned int length = 0;
        const char* path;
        textures[texture_index].path_offset = smile_3d_read_u32(record);
        textures[texture_index].semantic = (unsigned char)smile_3d_read_u32(record + 4);
        path = smile_3d_model_v2_string(bytes, strings, textures[texture_index].path_offset, &length);
        if (textures[texture_index].semantic < 1 || textures[texture_index].semantic > 4 ||
            smile_3d_read_u32(record + 8) != 0 || smile_3d_read_u32(record + 12) != 0 ||
            path == 0 || !smile_3d_model_v2_texture_path(path, length))
        {
            smile_last_error3d = 24;
            return 0;
        }
        for (unsigned int prior = 0; prior < texture_index; ++prior)
            if (textures[prior].semantic == textures[texture_index].semantic &&
                strcmp((const char*)bytes + strings->offset + textures[prior].path_offset, path) == 0)
            {
                smile_last_error3d = 24;
                return 0;
            }
    }

    for (unsigned int material_index = 0; material_index < material_count; ++material_index)
    {
        const unsigned char* record = bytes + material_chunk->offset + material_index * 80;
        SmileModelMaterialV2* material = &materials[material_index];
        material->name_offset = smile_3d_read_u32(record);
        if (smile_3d_model_v2_string(bytes, strings, material->name_offset, 0) == 0)
        {
            smile_last_error3d = 24;
            return 0;
        }
        for (unsigned int channel = 0; channel < 4; ++channel)
        {
            unsigned int reference = smile_3d_read_u32(record + 4 + channel * 4);
            material->texture_references[channel] = reference == 0xFFFFFFFFU ? -1 : (int)reference;
            if (reference != 0xFFFFFFFFU && (reference >= texture_count || textures[reference].semantic != channel + 1))
            {
                smile_last_error3d = 24;
                return 0;
            }
        }
        material->alpha_mode = (unsigned char)smile_3d_read_u32(record + 20);
        material->double_sided = (unsigned char)smile_3d_read_u32(record + 24);
        if (material->alpha_mode > 2 || material->double_sided > 1 || smile_3d_read_u32(record + 28) != 0)
        {
            smile_last_error3d = 24;
            return 0;
        }
        for (unsigned int component = 0; component < 4; ++component)
        {
            material->base_color[component] = smile_3d_read_float(record + 32 + component * 4);
            if (!smile_3d_model_v2_finite_range(material->base_color[component], 0.0f, 1.0f))
            { smile_last_error3d = 24; return 0; }
        }
        material->metallic = smile_3d_read_float(record + 48);
        material->roughness = smile_3d_read_float(record + 52);
        material->normal_strength = smile_3d_read_float(record + 56);
        material->occlusion_strength = smile_3d_read_float(record + 60);
        for (unsigned int component = 0; component < 3; ++component)
            material->emissive[component] = smile_3d_read_float(record + 64 + component * 4);
        material->alpha_cutoff = smile_3d_read_float(record + 76);
        if (!smile_3d_model_v2_finite_range(material->metallic, 0.0f, 1.0f) ||
            !smile_3d_model_v2_finite_range(material->roughness, 0.0f, 1.0f) ||
            !smile_3d_model_v2_finite_range(material->normal_strength, 0.0f, 8.0f) ||
            !smile_3d_model_v2_finite_range(material->occlusion_strength, 0.0f, 1.0f) ||
            !smile_3d_model_v2_finite_range(material->emissive[0], 0.0f, 64.0f) ||
            !smile_3d_model_v2_finite_range(material->emissive[1], 0.0f, 64.0f) ||
            !smile_3d_model_v2_finite_range(material->emissive[2], 0.0f, 64.0f) ||
            !smile_3d_model_v2_finite_range(material->alpha_cutoff, 0.0f, 1.0f))
        {
            smile_last_error3d = 24;
            return 0;
        }
    }

    {
        unsigned int expected_vertex = 0;
        unsigned int expected_index = 0;
        float computed_model[6] = { FLT_MAX, FLT_MAX, FLT_MAX, -FLT_MAX, -FLT_MAX, -FLT_MAX };
        for (part_index = 0; part_index < part_count; ++part_index)
        {
            const unsigned char* record = bytes + part_chunk->offset + part_index * 32;
            SmileModelPartV2* part = &parts[part_index];
            float computed[6] = { FLT_MAX, FLT_MAX, FLT_MAX, -FLT_MAX, -FLT_MAX, -FLT_MAX };
            part->name_offset = smile_3d_read_u32(record);
            part->first_vertex = smile_3d_read_u32(record + 4);
            part->vertex_count = smile_3d_read_u32(record + 8);
            part->first_index = smile_3d_read_u32(record + 12);
            part->index_count = smile_3d_read_u32(record + 16);
            part->material = smile_3d_read_u32(record + 20);
            part->bounds_index = smile_3d_read_u32(record + 24);
            if (smile_3d_model_v2_string(bytes, strings, part->name_offset, 0) == 0 ||
                part->first_vertex != expected_vertex || part->first_index != expected_index ||
                part->vertex_count == 0 || part->vertex_count > 65535 ||
                part->index_count == 0 || part->index_count > 196608 || part->index_count % 3 != 0 ||
                part->vertex_count > vertex_count - part->first_vertex || part->index_count > index_count - part->first_index ||
                part->material >= material_count || part->bounds_index != part_index + 1 || smile_3d_read_u32(record + 28) != 0 ||
                !smile_3d_model_v2_bounds(bytes + bounds_chunk->offset + (part_index + 1) * 32, part_bounds[part_index]))
            {
                smile_last_error3d = 24;
                return 0;
            }
            for (unsigned int vertex = 0; vertex < part->vertex_count; ++vertex)
            {
                const unsigned char* source = bytes + vertex_chunk->offset + (part->first_vertex + vertex) * 48;
                float values[12];
                for (unsigned int field = 0; field < 12; ++field)
                {
                    values[field] = smile_3d_read_float(source + field * 4);
                    if (!isfinite(values[field])) { smile_last_error3d = 24; return 0; }
                }
                float normal_length = values[3]*values[3]+values[4]*values[4]+values[5]*values[5];
                float tangent_length = values[6]*values[6]+values[7]*values[7]+values[8]*values[8];
                float basis_dot = values[3]*values[6]+values[4]*values[7]+values[5]*values[8];
                if (fabsf(normal_length - 1.0f) > 0.0001f ||
                    fabsf(tangent_length - 1.0f) > 0.0001f || fabsf(basis_dot) > 0.0001f ||
                    fabsf(fabsf(values[9]) - 1.0f) > 0.0001f)
                { smile_last_error3d = 24; return 0; }
                for (unsigned int component = 0; component < 3; ++component)
                {
                    if (values[component] < computed[component]) computed[component] = values[component];
                    if (values[component] > computed[component + 3]) computed[component + 3] = values[component];
                }
            }
            for (unsigned int index = 0; index < part->index_count; ++index)
                if (smile_3d_read_u32(bytes + index_chunk->offset + (part->first_index + index) * 4) >= part->vertex_count)
                { smile_last_error3d = 24; return 0; }
            for (unsigned int triangle = 0; triangle < part->index_count; triangle += 3)
            {
                unsigned int ia = smile_3d_read_u32(bytes + index_chunk->offset + (part->first_index + triangle) * 4);
                unsigned int ib = smile_3d_read_u32(bytes + index_chunk->offset + (part->first_index + triangle + 1) * 4);
                unsigned int ic = smile_3d_read_u32(bytes + index_chunk->offset + (part->first_index + triangle + 2) * 4);
                const unsigned char* a = bytes + vertex_chunk->offset + (part->first_vertex + ia) * 48;
                const unsigned char* b = bytes + vertex_chunk->offset + (part->first_vertex + ib) * 48;
                const unsigned char* c = bytes + vertex_chunk->offset + (part->first_vertex + ic) * 48;
                float ux=smile_3d_read_float(b)-smile_3d_read_float(a),uy=smile_3d_read_float(b+4)-smile_3d_read_float(a+4),uz=smile_3d_read_float(b+8)-smile_3d_read_float(a+8);
                float vx=smile_3d_read_float(c)-smile_3d_read_float(a),vy=smile_3d_read_float(c+4)-smile_3d_read_float(a+4),vz=smile_3d_read_float(c+8)-smile_3d_read_float(a+8);
                float x=uy*vz-uz*vy,y=uz*vx-ux*vz,z=ux*vy-uy*vx;
                if (x*x+y*y+z*z <= 0.000000000001f) { smile_last_error3d = 24; return 0; }
            }
            for (unsigned int component = 0; component < 6; ++component)
                if (computed[component] != part_bounds[part_index][component]) { smile_last_error3d = 24; return 0; }
            for (unsigned int component = 0; component < 3; ++component)
            {
                if (computed[component] < computed_model[component]) computed_model[component] = computed[component];
                if (computed[component + 3] > computed_model[component + 3]) computed_model[component + 3] = computed[component + 3];
            }
            expected_vertex += part->vertex_count;
            expected_index += part->index_count;
        }
        if (expected_vertex != vertex_count || expected_index != index_count)
        { smile_last_error3d = 24; return 0; }
        for (unsigned int component = 0; component < 6; ++component)
            if (computed_model[component] != bounds[component]) { smile_last_error3d = 24; return 0; }
    }

    animation_status = smile_3d_model_v2_animation(bytes, chunks, chunk_count, strings,
        vertex_count, animation_chunks, &animation_bytes);
    if (animation_status < 0) { smile_last_error3d = 47; return 0; }

    if (smile_3d_live_mesh_count() + (int)part_count > SMILE_3D_MAX_MESHES)
    { smile_last_error3d = 3; return 0; }
    for (model_slot = 0; model_slot < SMILE_3D_MAX_MODELS; ++model_slot)
        if (!smile_models3d[model_slot].active) break;
    if (model_slot == SMILE_3D_MAX_MODELS) { smile_last_error3d = 25; return 0; }
    strings_copy = (char*)smile_3d_allocate(strings->length);
    if (strings_copy == 0) { smile_last_error3d = 4; return 0; }
    memcpy(strings_copy, bytes + strings->offset, strings->length);
    if (animation_status > 0)
    {
        for (unsigned int index = 0; index < 9; ++index)
        {
            animation_resident_bytes = (animation_resident_bytes + 3U) & ~3U;
            compact_animation_chunks[index] = animation_chunks[index];
            compact_animation_chunks[index].offset = animation_resident_bytes;
            if (animation_chunks[index].length > UINT_MAX - animation_resident_bytes)
            { smile_last_error3d = 4; goto rollback_v2; }
            animation_resident_bytes += animation_chunks[index].length;
        }
        animation_copy = (unsigned char*)smile_3d_allocate(animation_resident_bytes);
        if (animation_copy == 0) { smile_last_error3d = 4; goto rollback_v2; }
        ZeroMemory(animation_copy, animation_resident_bytes);
        for (unsigned int index = 0; index < 9; ++index)
            memcpy(animation_copy + compact_animation_chunks[index].offset,
                bytes + animation_chunks[index].offset, animation_chunks[index].length);
    }

    for (part_index = 0; part_index < part_count; ++part_index)
    {
        SmileModelPartV2* part = &parts[part_index];
        SmileMesh3D* mesh;
        mesh_handles[part_index] = smile_3d_create_mesh(part->vertex_count, part->index_count);
        mesh = smile_3d_mesh(mesh_handles[part_index]);
        if (mesh == 0) goto rollback_v2;
        for (unsigned int vertex = 0; vertex < part->vertex_count; ++vertex)
        {
            const unsigned char* source = bytes + vertex_chunk->offset + (part->first_vertex + vertex) * 48;
            mesh->vertices[vertex].x = smile_3d_read_float(source);
            mesh->vertices[vertex].y = smile_3d_read_float(source + 4);
            mesh->vertices[vertex].z = smile_3d_read_float(source + 8);
            mesh->vertices[vertex].nx = smile_3d_read_float(source + 12);
            mesh->vertices[vertex].ny = smile_3d_read_float(source + 16);
            mesh->vertices[vertex].nz = smile_3d_read_float(source + 20);
            mesh->vertices[vertex].tangent[0] = smile_3d_read_float(source + 24);
            mesh->vertices[vertex].tangent[1] = smile_3d_read_float(source + 28);
            mesh->vertices[vertex].tangent[2] = smile_3d_read_float(source + 32);
            mesh->vertices[vertex].tangent[3] = smile_3d_read_float(source + 36);
            mesh->vertices[vertex].u = smile_3d_read_float(source + 40);
            mesh->vertices[vertex].v = smile_3d_read_float(source + 44);
            if (animation_status > 0)
            {
                const unsigned char* skin = bytes + animation_chunks[1].offset +
                    (part->first_vertex + vertex) * 16;
                for (unsigned int influence = 0; influence < 4; ++influence)
                {
                    unsigned int joint = smile_3d_read_u16(skin + influence * 2);
                    mesh->vertices[vertex].joints[influence] = (float)joint;
                    mesh->vertices[vertex].weights[influence] =
                        (float)smile_3d_read_u16(skin + 8 + influence * 2) / 65535.0f;
                    if (joint > mesh->max_joint) mesh->max_joint = (unsigned char)joint;
                }
            }
        }
        mesh->explicit_normals = 1;
        for (unsigned int index = 0; index < part->index_count; ++index)
            mesh->indices[index] = smile_3d_read_u32(bytes + index_chunk->offset + (part->first_index + index) * 4);
        if (!smile_3d_commit_mesh(mesh)) goto rollback_v2;
    }
    {
        SmileModel3D* model = &smile_models3d[model_slot];
        unsigned short generation = model->generation == 0 ? 1 : model->generation;
        ZeroMemory(model, sizeof(*model));
        model->generation = generation;
        model->active = 1;
        model->format_version = 2;
        model->part_count = (unsigned char)part_count;
        model->material_count = (unsigned short)material_count;
        model->texture_count = (unsigned short)texture_count;
        model->vertex_count = vertex_count;
        model->index_count = index_count;
        model->model_name_offset = model_name_offset;
        model->string_bytes = strings->length;
        model->strings = strings_copy;
        strings_copy = 0;
        if (animation_status > 0)
        {
            model->has_animation = 1;
            model->animation_node_count = (unsigned short)animation_chunks[0].count;
            model->animation_bone_count = (unsigned char)animation_chunks[2].count;
            model->animation_clip_count = (unsigned char)animation_chunks[3].count;
            model->animation_event_count = (unsigned short)animation_chunks[6].count;
            model->animation_socket_count = (unsigned char)animation_chunks[7].count;
            model->animation_bytes = animation_bytes;
            model->animation_file_bytes = size;
            model->animation_resident_bytes = animation_resident_bytes;
            model->animation_data = animation_copy;
            memcpy(model->animation_chunks, compact_animation_chunks,
                sizeof(compact_animation_chunks));
            animation_copy = 0;
        }
        memcpy(model->bounds, bounds, sizeof(bounds));
        for (part_index = 0; part_index < part_count; ++part_index)
        {
            model->mesh_handles[part_index] = mesh_handles[part_index];
            model->material_slots[part_index] = (unsigned short)parts[part_index].material;
            model->part_name_offsets[part_index] = parts[part_index].name_offset;
            memcpy(model->part_bounds[part_index], part_bounds[part_index], sizeof(part_bounds[part_index]));
        }
        for (unsigned int material_index = 0; material_index < material_count; ++material_index)
        {
            model->materials[material_index].name_offset = materials[material_index].name_offset;
            memcpy(model->materials[material_index].texture_references, materials[material_index].texture_references,
                sizeof(materials[material_index].texture_references));
            model->materials[material_index].alpha_mode = materials[material_index].alpha_mode;
            model->materials[material_index].double_sided = materials[material_index].double_sided;
            memcpy(model->materials[material_index].base_color, materials[material_index].base_color, sizeof(materials[material_index].base_color));
            model->materials[material_index].metallic = materials[material_index].metallic;
            model->materials[material_index].roughness = materials[material_index].roughness;
            model->materials[material_index].normal_strength = materials[material_index].normal_strength;
            model->materials[material_index].occlusion_strength = materials[material_index].occlusion_strength;
            memcpy(model->materials[material_index].emissive, materials[material_index].emissive, sizeof(materials[material_index].emissive));
            model->materials[material_index].alpha_cutoff = materials[material_index].alpha_cutoff;
        }
        for (unsigned int texture_index = 0; texture_index < texture_count; ++texture_index)
        {
            model->textures[texture_index].path_offset = textures[texture_index].path_offset;
            model->textures[texture_index].semantic = textures[texture_index].semantic;
        }
        result = smile_3d_handle(SMILE_3D_MODEL_HANDLE, (int)model_slot, model->generation);
    }
    return result;

rollback_v2:
    for (part_index = 0; part_index < SMILE_3D_MAX_MODEL_PARTS; ++part_index)
    {
        SmileMesh3D* mesh = smile_3d_mesh(mesh_handles[part_index]);
        if (mesh != 0) smile_3d_delete_mesh(mesh);
    }
    { void* allocation = strings_copy; smile_3d_free(allocation); }
    { void* allocation = animation_copy; smile_3d_free(allocation); }
    return 0;
}

static long long smile_3d_load_model_path(const wchar_t* path, int prepare_pbr)
{
    HANDLE file = INVALID_HANDLE_VALUE;
    LARGE_INTEGER file_size;
    DWORD bytes_read = 0;
    unsigned char* bytes = 0;
    unsigned int size;
    unsigned int part_count, vertex_count, index_count, material_count;
    unsigned int part_table_bytes, vertex_bytes, expected_size;
    unsigned int part_index, model_slot;
    long long mesh_handles[SMILE_3D_MAX_MODEL_PARTS] = {};
    long long result = 0;

    if (path == 0) { smile_last_error3d = 26; return 0; }
    file = CreateFileW(path, GENERIC_READ, FILE_SHARE_READ, 0, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
    if (file == INVALID_HANDLE_VALUE || !GetFileSizeEx(file, &file_size) ||
        file_size.QuadPart < 32 || file_size.QuadPart > SMILE_3D_MAX_MODEL_BYTES)
    {
        if (file != INVALID_HANDLE_VALUE) CloseHandle(file);
        smile_last_error3d = 26;
        return 0;
    }
    size = (unsigned int)file_size.QuadPart;
    bytes = (unsigned char*)smile_3d_allocate(size);
    if (bytes == 0 || !ReadFile(file, bytes, size, &bytes_read, 0) || bytes_read != size)
    {
        CloseHandle(file);
        { void* allocation = bytes; smile_3d_free(allocation); }
        smile_last_error3d = 26;
        return 0;
    }
    CloseHandle(file);

    if (bytes[0] == 'S' && bytes[1] == 'M' && bytes[2] == '3' && bytes[3] == 'D' &&
        smile_3d_read_u16(bytes + 4) == 2)
    {
        result = smile_3d_load_model_v2(bytes, size);
        if (prepare_pbr && result != 0 && !smile_3d_prepare_model_pbr(result, 3, 1, 8))
        {
            int failure = smile_last_error3d;
            SmileModel3D* model = smile_3d_model_resource(result);
            if (model != 0) smile_3d_delete_model(model);
            smile_last_error3d = failure;
            result = 0;
        }
        goto cleanup;
    }

    part_count = smile_3d_read_u32(bytes + 8);
    vertex_count = smile_3d_read_u32(bytes + 12);
    index_count = smile_3d_read_u32(bytes + 16);
    material_count = smile_3d_read_u32(bytes + 20);
    part_table_bytes = part_count * 24U;
    vertex_bytes = vertex_count * 32U;
    expected_size = 32U + part_table_bytes + vertex_bytes + index_count * 4U;
    if (bytes[0] != 'S' || bytes[1] != 'M' || bytes[2] != '3' || bytes[3] != 'D' ||
        smile_3d_read_u16(bytes + 4) != 1 || smile_3d_read_u16(bytes + 6) != 32 ||
        part_count == 0 || part_count > SMILE_3D_MAX_MODEL_PARTS ||
        vertex_count == 0 || vertex_count > SMILE_3D_MAX_MODEL_PARTS * 65535U ||
        index_count == 0 || index_count > SMILE_3D_MAX_MODEL_PARTS * 196608U ||
        material_count == 0 || material_count > 64 || smile_3d_read_u32(bytes + 24) != size ||
        expected_size != size || smile_3d_read_u32(bytes + 28) != smile_3d_checksum(bytes + 32, size - 32))
    {
        smile_last_error3d = 24;
        goto cleanup;
    }

    for (part_index = 0; part_index < part_count; ++part_index)
    {
        const unsigned char* part = bytes + 32 + part_index * 24U;
        unsigned int first_vertex = smile_3d_read_u32(part);
        unsigned int part_vertices = smile_3d_read_u32(part + 4);
        unsigned int first_index = smile_3d_read_u32(part + 8);
        unsigned int part_indices = smile_3d_read_u32(part + 12);
        unsigned int material_slot = smile_3d_read_u32(part + 16);
        unsigned int index;
        if (part_vertices == 0 || part_vertices > 65535 || part_indices == 0 ||
            part_indices > 196608 || part_indices % 3 != 0 ||
            first_vertex > vertex_count || part_vertices > vertex_count - first_vertex ||
            first_index > index_count || part_indices > index_count - first_index ||
            material_slot >= material_count || smile_3d_read_u32(part + 20) != 0)
        {
            smile_last_error3d = 24;
            goto cleanup;
        }
        for (index = 0; index < part_vertices * 8U; ++index)
            if (!isfinite(smile_3d_read_float(bytes + 32 + part_table_bytes +
                (first_vertex * 8U + index) * 4U)))
            {
                smile_last_error3d = 24;
                goto cleanup;
            }
        for (index = 0; index < part_indices; ++index)
            if (smile_3d_read_u32(bytes + 32 + part_table_bytes + vertex_bytes +
                (first_index + index) * 4U) >= part_vertices)
            {
                smile_last_error3d = 24;
                goto cleanup;
            }
    }
    if (smile_3d_live_mesh_count() + (int)part_count > SMILE_3D_MAX_MESHES)
    {
        smile_last_error3d = 3;
        goto cleanup;
    }
    for (model_slot = 0; model_slot < SMILE_3D_MAX_MODELS; ++model_slot)
        if (!smile_models3d[model_slot].active) break;
    if (model_slot == SMILE_3D_MAX_MODELS)
    {
        smile_last_error3d = 25;
        goto cleanup;
    }

    for (part_index = 0; part_index < part_count; ++part_index)
    {
        const unsigned char* part = bytes + 32 + part_index * 24U;
        unsigned int first_vertex = smile_3d_read_u32(part);
        unsigned int part_vertices = smile_3d_read_u32(part + 4);
        unsigned int first_index = smile_3d_read_u32(part + 8);
        unsigned int part_indices = smile_3d_read_u32(part + 12);
        unsigned int vertex_index, index;
        SmileMesh3D* mesh;
        mesh_handles[part_index] = smile_3d_create_mesh(part_vertices, part_indices);
        mesh = smile_3d_mesh(mesh_handles[part_index]);
        if (mesh == 0) goto rollback;
        for (vertex_index = 0; vertex_index < part_vertices; ++vertex_index)
        {
            const unsigned char* vertex = bytes + 32 + part_table_bytes +
                (first_vertex + vertex_index) * 32U;
            mesh->vertices[vertex_index].x = smile_3d_read_float(vertex);
            mesh->vertices[vertex_index].y = smile_3d_read_float(vertex + 4);
            mesh->vertices[vertex_index].z = smile_3d_read_float(vertex + 8);
            mesh->vertices[vertex_index].nx = smile_3d_read_float(vertex + 12);
            mesh->vertices[vertex_index].ny = smile_3d_read_float(vertex + 16);
            mesh->vertices[vertex_index].nz = smile_3d_read_float(vertex + 20);
            mesh->vertices[vertex_index].u = smile_3d_read_float(vertex + 24);
            mesh->vertices[vertex_index].v = smile_3d_read_float(vertex + 28);
        }
        mesh->explicit_normals = 1;
        for (index = 0; index < part_indices; ++index)
            mesh->indices[index] = smile_3d_read_u32(bytes + 32 + part_table_bytes + vertex_bytes +
                (first_index + index) * 4U);
        if (!smile_3d_commit_mesh(mesh)) goto rollback;
    }
    {
        SmileModel3D* model = &smile_models3d[model_slot];
        unsigned short generation = model->generation == 0 ? 1 : model->generation;
        ZeroMemory(model, sizeof(*model));
        model->generation = generation;
        model->active = 1;
        model->format_version = 1;
        model->part_count = (unsigned char)part_count;
        model->material_count = (unsigned short)material_count;
        model->vertex_count = vertex_count;
        model->index_count = index_count;
        for (part_index = 0; part_index < part_count; ++part_index)
        {
            const unsigned char* part = bytes + 32 + part_index * 24U;
            model->mesh_handles[part_index] = mesh_handles[part_index];
            model->material_slots[part_index] = (unsigned short)smile_3d_read_u32(part + 16);
        }
        result = smile_3d_handle(SMILE_3D_MODEL_HANDLE, (int)model_slot, model->generation);
    }
    goto cleanup;

rollback:
    for (part_index = 0; part_index < SMILE_3D_MAX_MODEL_PARTS; ++part_index)
    {
        SmileMesh3D* mesh = smile_3d_mesh(mesh_handles[part_index]);
        if (mesh != 0) smile_3d_delete_mesh(mesh);
    }
cleanup:
    { void* allocation = bytes; smile_3d_free(allocation); }
    return result;
}

extern "C" long long smile_renderer3d_load_model_path(const wchar_t* path)
{
    return smile_3d_load_model_path(path, 1);
}

extern "C" long long smile_renderer3d_load_model_geometry_path(const wchar_t* path)
{
    return smile_3d_load_model_path(path, 0);
}

static int smile_3d_prepare_model_pbr(long long model_handle,
    long long filter, long long wrap, long long anisotropy)
{
    SmileModel3D* model = smile_3d_model_resource(model_handle);
    SmileImageResource* decoded[SMILE_3D_MAX_MODEL_TEXTURES] = {};
    long long texture_by_reference[SMILE_3D_MAX_MODEL_TEXTURES] = {};
    long long owned_texture_handles[SMILE_3D_MAX_MODEL_TEXTURES] = {};
    long long material_handles[SMILE_3D_MAX_MODEL_MATERIALS] = {};
    unsigned char unique_for_reference[SMILE_3D_MAX_MODEL_TEXTURES] = {};
    unsigned char first_reference[SMILE_3D_MAX_MODEL_TEXTURES] = {};
    unsigned char usage_by_unique[SMILE_3D_MAX_MODEL_TEXTURES] = {};
    int unique_textures = 0;
    int created_textures = 0;
    int created_materials = 0;
    if (model == 0 || model->format_version != 2 || filter < 0 || filter > 3 ||
        wrap < 0 || wrap > 1 || anisotropy < 1 || anisotropy > 16)
    {
        smile_last_error3d = 40;
        if (model != 0) model->pbr_failure = 40;
        return 0;
    }
    if (model->prepared_pbr)
    {
        model->pbr_failure = 0;
        return 1;
    }
    if (!smile_3d_create_pipeline() || !smile_pbr_shader_available3d)
    {
        smile_last_error3d = 44;
        model->pbr_failure = 44;
        return 0;
    }

    for (int reference = 0; reference < model->texture_count; ++reference)
    {
        const char* path = model->strings + model->textures[reference].path_offset;
        int source_semantic = model->textures[reference].semantic;
        int usage = source_semantic == 1 || source_semantic == 4 ? 1 : 2;
        int unique;
        for (unique = 0; unique < unique_textures; ++unique)
        {
            const char* existing = model->strings +
                model->textures[first_reference[unique]].path_offset;
            if (usage_by_unique[unique] == usage && strcmp(existing, path) == 0) break;
        }
        if (unique == unique_textures)
        {
            first_reference[unique] = (unsigned char)reference;
            usage_by_unique[unique] = (unsigned char)usage;
            unique_textures++;
        }
        unique_for_reference[reference] = (unsigned char)unique;
    }

    if (smile_3d_live_texture_count() + unique_textures > SMILE_3D_MAX_TEXTURES ||
        smile_3d_live_material_count() + model->material_count > SMILE_3D_MAX_MATERIALS)
    {
        smile_last_error3d = 41;
        model->pbr_failure = 41;
        return 0;
    }

    for (int unique = 0; unique < unique_textures; ++unique)
    {
        WCHAR resolved[2048];
        int reference = first_reference[unique];
        const char* path = model->strings + model->textures[reference].path_offset;
        int length = lstrlenA(path);
        if (!smile_resolve_asset_path_utf8(path, length, resolved,
            (int)(sizeof(resolved) / sizeof(resolved[0]))))
        {
            smile_last_error3d = 42;
            goto rollback_prepare;
        }
        decoded[unique] = smile_image_resource_load(resolved);
        if (decoded[unique] == 0)
        {
            smile_last_error3d = 42;
            goto rollback_prepare;
        }
        owned_texture_handles[unique] = smile_3d_create_pbr_texture(decoded[unique],
            usage_by_unique[unique],
            (int)filter, (int)wrap, (int)anisotropy);
        decoded[unique] = 0;
        if (owned_texture_handles[unique] == 0) goto rollback_prepare;
        created_textures++;
    }

    for (int reference = 0; reference < model->texture_count; ++reference)
        texture_by_reference[reference] = owned_texture_handles[unique_for_reference[reference]];

    for (int index = 0; index < model->material_count; ++index)
    {
        long long selected[4] = {};
        for (int semantic = 0; semantic < 4; ++semantic)
        {
            int reference = model->materials[index].texture_references[semantic];
            if (reference >= 0) selected[semantic] = texture_by_reference[reference];
        }
        material_handles[index] = smile_3d_create_pbr_material(selected[0], selected[1],
            selected[2], selected[3], model->materials[index].alpha_mode,
            model->materials[index].double_sided, model_handle);
        if (material_handles[index] == 0) goto rollback_prepare;
        created_materials++;
        {
            SmileMaterial3D* material = smile_3d_material(material_handles[index]);
            memcpy(material->color, model->materials[index].base_color, sizeof(material->color));
            material->metallic = model->materials[index].metallic;
            material->roughness = model->materials[index].roughness;
            material->normal_strength = model->materials[index].normal_strength;
            material->occlusion_strength = model->materials[index].occlusion_strength;
            memcpy(material->emissive_color, model->materials[index].emissive,
                sizeof(material->emissive_color));
            material->cutoff = model->materials[index].alpha_cutoff;
        }
    }
    memcpy(model->prepared_texture_by_reference, texture_by_reference,
        sizeof(long long) * model->texture_count);
    memcpy(model->owned_texture_handles, owned_texture_handles,
        sizeof(long long) * unique_textures);
    memcpy(model->prepared_material_handles, material_handles,
        sizeof(long long) * model->material_count);
    model->prepared_reference_count = (unsigned char)model->texture_count;
    model->owned_texture_count = (unsigned char)unique_textures;
    model->prepared_material_count = (unsigned char)model->material_count;
    model->prepared_pbr = 1;
    model->pbr_failure = 0;
    return 1;

rollback_prepare:
    for (int index = 0; index < unique_textures; ++index)
        smile_image_resource_release(decoded[index]);
    for (int index = 0; index < created_materials; ++index)
    {
        SmileMaterial3D* material = smile_3d_material(material_handles[index]);
        if (material != 0) smile_3d_delete_material(material);
    }
    for (int index = 0; index < created_textures; ++index)
    {
        SmileTexture3D* texture = smile_3d_texture(owned_texture_handles[index]);
        if (texture != 0) smile_3d_delete_texture(texture);
    }
    model->pbr_failure = (unsigned char)smile_last_error3d;
    return 0;
}

extern "C" long long smile_renderer3d_prepare_model_pbr(long long model_handle,
    long long filter, long long wrap, long long anisotropy)
{
    return smile_3d_prepare_model_pbr(model_handle, filter, wrap, anisotropy);
}

static SmileMatrix3D smile_3d_identity(void)
{
    SmileMatrix3D result = {};
    result.m[0] = result.m[5] = result.m[10] = result.m[15] = 1.0f;
    return result;
}

static SmileMatrix3D smile_3d_multiply(const SmileMatrix3D& left, const SmileMatrix3D& right)
{
    SmileMatrix3D result = {};
    int row, column, index;
    for (row = 0; row < 4; ++row)
        for (column = 0; column < 4; ++column)
            for (index = 0; index < 4; ++index)
                result.m[row * 4 + column] += left.m[row * 4 + index] * right.m[index * 4 + column];
    return result;
}

static SmileMatrix3D smile_3d_model(const SmileObject3D* object)
{
    float sx = object->scale[0], sy = object->scale[1], sz = object->scale[2];
    float ax = smile_3d_degrees((long long)object->rotation[0]);
    float ay = smile_3d_degrees((long long)object->rotation[1]);
    float az = smile_3d_degrees((long long)object->rotation[2]);
    SmileMatrix3D scale = smile_3d_identity(), rx = smile_3d_identity(), ry = smile_3d_identity();
    SmileMatrix3D rz = smile_3d_identity(), translation = smile_3d_identity();
    scale.m[0] = sx; scale.m[5] = sy; scale.m[10] = sz;
    rx.m[5] = cosf(ax); rx.m[6] = sinf(ax); rx.m[9] = -sinf(ax); rx.m[10] = cosf(ax);
    ry.m[0] = cosf(ay); ry.m[2] = -sinf(ay); ry.m[8] = sinf(ay); ry.m[10] = cosf(ay);
    rz.m[0] = cosf(az); rz.m[1] = sinf(az); rz.m[4] = -sinf(az); rz.m[5] = cosf(az);
    translation.m[12] = object->position[0]; translation.m[13] = object->position[1]; translation.m[14] = object->position[2];
    return smile_3d_multiply(smile_3d_multiply(smile_3d_multiply(smile_3d_multiply(scale, rx), ry), rz), translation);
}

static SmileMatrix3D smile_3d_normal_matrix(const SmileMatrix3D& model)
{
    SmileMatrix3D result = {};
    float a = model.m[0], b = model.m[1], c = model.m[2];
    float d = model.m[4], e = model.m[5], f = model.m[6];
    float g = model.m[8], h = model.m[9], i = model.m[10];
    float determinant = a * (e * i - f * h) - b * (d * i - f * g) + c * (d * h - e * g);
    if (fabsf(determinant) <= 0.0000001f) return smile_3d_identity();
    float inverse = 1.0f / determinant;
    result.m[0] = (e * i - f * h) * inverse;
    result.m[1] = (f * g - d * i) * inverse;
    result.m[2] = (d * h - e * g) * inverse;
    result.m[4] = (c * h - b * i) * inverse;
    result.m[5] = (a * i - c * g) * inverse;
    result.m[6] = (b * g - a * h) * inverse;
    result.m[8] = (b * f - c * e) * inverse;
    result.m[9] = (c * d - a * f) * inverse;
    result.m[10] = (a * e - b * d) * inverse;
    result.m[15] = 1.0f;
    return result;
}

static SmileMatrix3D smile_3d_pose(float tx, float ty, float tz,
    float qx, float qy, float qz, float qw, float sx, float sy, float sz)
{
    SmileMatrix3D result = smile_3d_identity();
    float length = sqrtf(qx*qx + qy*qy + qz*qz + qw*qw);
    if (length <= 0.000001f) { qx = qy = qz = 0.0f; qw = 1.0f; }
    else { qx /= length; qy /= length; qz /= length; qw /= length; }
    result.m[0] = (1 - 2*qy*qy - 2*qz*qz) * sx;
    result.m[1] = (2*qx*qy + 2*qw*qz) * sx;
    result.m[2] = (2*qx*qz - 2*qw*qy) * sx;
    result.m[4] = (2*qx*qy - 2*qw*qz) * sy;
    result.m[5] = (1 - 2*qx*qx - 2*qz*qz) * sy;
    result.m[6] = (2*qy*qz + 2*qw*qx) * sy;
    result.m[8] = (2*qx*qz + 2*qw*qy) * sz;
    result.m[9] = (2*qy*qz - 2*qw*qx) * sz;
    result.m[10] = (1 - 2*qx*qx - 2*qy*qy) * sz;
    result.m[12] = tx; result.m[13] = ty; result.m[14] = tz;
    return result;
}

static float smile_3d_lerp(float first, float second, float amount)
{
    return first + (second - first) * amount;
}

static void smile_3d_update_animation_pose(SmileAnimator3D* animator)
{
    SmileSkeleton3D* skeleton = smile_3d_skeleton(animator->skeleton_handle);
    SmileAnimationClip3D* clip = smile_3d_clip(animator->clip_handle);
    SmileMatrix3D global[SMILE_3D_MAX_BONES];
    float amount = clip == 0 || clip->duration_ms == 0 ? 0.0f :
        (float)animator->time_ms / (float)clip->duration_ms;
    if (skeleton == 0) return;
    for (int bone = 0; bone < skeleton->bone_count; ++bone)
    {
        float tx = skeleton->bind_translation[bone][0];
        float ty = skeleton->bind_translation[bone][1];
        float tz = skeleton->bind_translation[bone][2];
        float qx = 0, qy = 0, qz = 0, qw = 1;
        float sx = 1, sy = 1, sz = 1;
        if (clip != 0 && clip->translation_tracks[bone])
        {
            tx = smile_3d_lerp(clip->translation[bone][0][0], clip->translation[bone][1][0], amount);
            ty = smile_3d_lerp(clip->translation[bone][0][1], clip->translation[bone][1][1], amount);
            tz = smile_3d_lerp(clip->translation[bone][0][2], clip->translation[bone][1][2], amount);
        }
        if (clip != 0 && clip->rotation_tracks[bone])
        {
            float dot = 0;
            for (int component = 0; component < 4; ++component)
                dot += clip->rotation[bone][0][component] * clip->rotation[bone][1][component];
            float direction = dot < 0 ? -1.0f : 1.0f;
            qx = smile_3d_lerp(clip->rotation[bone][0][0], clip->rotation[bone][1][0] * direction, amount);
            qy = smile_3d_lerp(clip->rotation[bone][0][1], clip->rotation[bone][1][1] * direction, amount);
            qz = smile_3d_lerp(clip->rotation[bone][0][2], clip->rotation[bone][1][2] * direction, amount);
            qw = smile_3d_lerp(clip->rotation[bone][0][3], clip->rotation[bone][1][3] * direction, amount);
        }
        if (clip != 0 && clip->scale_tracks[bone])
        {
            sx = smile_3d_lerp(clip->scale[bone][0][0], clip->scale[bone][1][0], amount);
            sy = smile_3d_lerp(clip->scale[bone][0][1], clip->scale[bone][1][1], amount);
            sz = smile_3d_lerp(clip->scale[bone][0][2], clip->scale[bone][1][2], amount);
        }
        SmileMatrix3D local = smile_3d_pose(tx, ty, tz, qx, qy, qz, qw, sx, sy, sz);
        int parent = skeleton->parents[bone];
        global[bone] = parent < 0 ? local : smile_3d_multiply(local, global[parent]);
        SmileMatrix3D inverse = smile_3d_identity();
        inverse.m[12] = skeleton->inverse_bind_translation[bone][0];
        inverse.m[13] = skeleton->inverse_bind_translation[bone][1];
        inverse.m[14] = skeleton->inverse_bind_translation[bone][2];
        animator->bones[bone] = smile_3d_multiply(inverse, global[bone]);
    }
    for (int bone = skeleton->bone_count; bone < SMILE_3D_MAX_BONES; ++bone)
        animator->bones[bone] = smile_3d_identity();
    animator->pose_revision++;
    if (animator->pose_revision == 0) animator->pose_revision = 1;
}

static int smile_3d_update_animator(SmileAnimator3D* animator, unsigned int delta_ms)
{
    SmileAnimationClip3D* clip;
    unsigned long long advance;
    unsigned long long total;
    int wrapped;
    if (animator == 0 || delta_ms > 600000) { smile_last_error3d = 35; return 0; }
    clip = smile_3d_clip(animator->clip_handle);
    if (clip == 0) { smile_3d_update_animation_pose(animator); return 1; }
    animator->previous_time_ms = animator->time_ms;
    advance = (unsigned long long)delta_ms * animator->speed_percent / 100U;
    total = (unsigned long long)animator->time_ms + advance;
    wrapped = animator->loop && total >= clip->duration_ms;
    if (animator->loop)
    {
        animator->time_ms = (unsigned int)(total % clip->duration_ms);
        animator->complete = 0;
    }
    else
    {
        animator->time_ms = (unsigned int)(total >= clip->duration_ms ? clip->duration_ms : total);
        animator->complete = total >= clip->duration_ms;
    }
    for (int event = 0; event < clip->event_count; ++event)
    {
        unsigned int time = clip->event_time[event];
        if ((!wrapped && time > animator->previous_time_ms && time <= animator->time_ms) ||
            (wrapped && (time > animator->previous_time_ms || time <= animator->time_ms)) ||
            (animator->loop && advance >= clip->duration_ms))
            animator->pending_event = clip->event_id[event];
    }
    smile_3d_update_animation_pose(animator);
    return 1;
}

static void smile_3d_clear_pending_camera(void)
{
    smile_pending_camera_has_projection3d = 0;
    smile_pending_camera_has_up3d = 0;
}

static int smile_3d_camera_world_value(long long value)
{
    return value >= -SMILE_3D_CAMERA_WORLD_BOUND && value <= SMILE_3D_CAMERA_WORLD_BOUND;
}

static int smile_3d_validate_pending_camera(void)
{
    double forward_x;
    double forward_y;
    double forward_z;
    double right_x;
    double right_y;
    double right_z;
    double forward_length_squared;
    double up_length_squared;
    double right_length_squared;
    if (!smile_pending_camera_has_projection3d || !smile_pending_camera_has_up3d)
    {
        smile_last_error3d = SMILE_3D_CAMERA_ERROR_PENDING_INCOMPLETE;
        return 0;
    }
    forward_x = (double)smile_pending_camera_target3d[0] - smile_pending_camera_position3d[0];
    forward_y = (double)smile_pending_camera_target3d[1] - smile_pending_camera_position3d[1];
    forward_z = (double)smile_pending_camera_target3d[2] - smile_pending_camera_position3d[2];
    forward_length_squared = forward_x * forward_x + forward_y * forward_y + forward_z * forward_z;
    if (forward_length_squared <= 0.0)
    {
        smile_last_error3d = SMILE_3D_CAMERA_ERROR_ZERO_VIEW_DIRECTION;
        return 0;
    }
    up_length_squared =
        (double)smile_pending_camera_up3d[0] * smile_pending_camera_up3d[0] +
        (double)smile_pending_camera_up3d[1] * smile_pending_camera_up3d[1] +
        (double)smile_pending_camera_up3d[2] * smile_pending_camera_up3d[2];
    if (up_length_squared <= 0.0)
    {
        smile_last_error3d = SMILE_3D_CAMERA_ERROR_INVALID_UP;
        return 0;
    }
    right_x = (double)smile_pending_camera_up3d[1] * forward_z -
        (double)smile_pending_camera_up3d[2] * forward_y;
    right_y = (double)smile_pending_camera_up3d[2] * forward_x -
        (double)smile_pending_camera_up3d[0] * forward_z;
    right_z = (double)smile_pending_camera_up3d[0] * forward_y -
        (double)smile_pending_camera_up3d[1] * forward_x;
    right_length_squared = right_x * right_x + right_y * right_y + right_z * right_z;
    if (right_length_squared <= forward_length_squared * up_length_squared * 0.00000001)
    {
        smile_last_error3d = SMILE_3D_CAMERA_ERROR_PARALLEL_UP;
        return 0;
    }
    return 1;
}

static void smile_3d_promote_pending_camera(void)
{
    memcpy(smile_camera_position3d, smile_pending_camera_position3d,
        sizeof(smile_camera_position3d));
    memcpy(smile_camera_target3d, smile_pending_camera_target3d,
        sizeof(smile_camera_target3d));
    memcpy(smile_camera_up3d, smile_pending_camera_up3d, sizeof(smile_camera_up3d));
    smile_camera_fov3d = smile_pending_camera_fov3d;
    smile_camera_near3d = smile_pending_camera_near3d;
    smile_camera_far3d = smile_pending_camera_far3d;
    smile_3d_clear_pending_camera();
}

static SmileMatrix3D smile_3d_view(void)
{
    float zx = smile_camera_target3d[0] - smile_camera_position3d[0];
    float zy = smile_camera_target3d[1] - smile_camera_position3d[1];
    float zz = smile_camera_target3d[2] - smile_camera_position3d[2];
    float xx, xy, xz, yx, yy, yz;
    SmileMatrix3D result = smile_3d_identity();
    smile_3d_normalize(&zx, &zy, &zz);
    smile_3d_cross(smile_camera_up3d[0], smile_camera_up3d[1], smile_camera_up3d[2],
        zx, zy, zz, &xx, &xy, &xz);
    smile_3d_normalize(&xx, &xy, &xz);
    smile_3d_cross(zx, zy, zz, xx, xy, xz, &yx, &yy, &yz);
    result.m[0] = xx; result.m[1] = yx; result.m[2] = zx;
    result.m[4] = xy; result.m[5] = yy; result.m[6] = zy;
    result.m[8] = xz; result.m[9] = yz; result.m[10] = zz;
    result.m[12] = -(xx * smile_camera_position3d[0] + xy * smile_camera_position3d[1] + xz * smile_camera_position3d[2]);
    result.m[13] = -(yx * smile_camera_position3d[0] + yy * smile_camera_position3d[1] + yz * smile_camera_position3d[2]);
    result.m[14] = -(zx * smile_camera_position3d[0] + zy * smile_camera_position3d[1] + zz * smile_camera_position3d[2]);
    return result;
}

static SmileMatrix3D smile_3d_projection(float aspect)
{
    SmileMatrix3D result = {};
    float y_scale = 1.0f / tanf(smile_camera_fov3d * SMILE_3D_PI / 360.0f);
    float x_scale = y_scale / aspect;
    result.m[0] = x_scale; result.m[5] = y_scale;
    result.m[10] = smile_camera_far3d / (smile_camera_far3d - smile_camera_near3d);
    result.m[11] = 1.0f;
    result.m[14] = -smile_camera_near3d * smile_camera_far3d / (smile_camera_far3d - smile_camera_near3d);
    return result;
}

static HRESULT smile_3d_compile(ID3D11Device* device, const char* source, const char* entry,
    const char* target, ID3DBlob** bytecode)
{
    ID3DBlob* errors = 0;
    HRESULT result = D3DCompile(source, lstrlenA(source), "SMILE Renderer3D", 0, 0, entry, target,
        D3DCOMPILE_ENABLE_STRICTNESS, 0, bytecode, &errors);
    (void)device;
    smile_3d_release(errors);
    return result;
}

static int smile_3d_create_pipeline(void)
{
    static const char* vertex_source =
        "cbuffer C:register(b0){row_major float4x4 model;row_major float4x4 mvp;float4 tint;float4 material;float4 animation;row_major float4x4 shadowMvp;float4 shadow;float4 output;float4 shadowLight;row_major float4x4 bones[32];}"
        "cbuffer B:register(b1){row_major float4x4 modelBones[128];}"
        "struct I{float3 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;float4 j:BLENDINDICES;float4 w:BLENDWEIGHT;};"
        "struct O{float4 p:SV_POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;float4 sp:TEXCOORD2;};"
        "O main(I i){O o;float4 p=float4(i.p,1);float3 n=i.n;if(animation.x>.5){float4x4 s;"
        "if(animation.x>1.5)s=modelBones[(uint)i.j.x]*i.w.x+modelBones[(uint)i.j.y]*i.w.y+modelBones[(uint)i.j.z]*i.w.z+modelBones[(uint)i.j.w]*i.w.w;"
        "else s=bones[(uint)i.j.x]*i.w.x+bones[(uint)i.j.y]*i.w.y+bones[(uint)i.j.z]*i.w.z+bones[(uint)i.j.w]*i.w.w;"
        "p=mul(p,s);n=mul(float4(n,0),s).xyz;}float4 world=mul(p,model);o.p=mul(p,mvp);o.n=normalize(mul(float4(n,0),model).xyz);o.uv=i.uv;o.world=world.xyz;o.sp=mul(p,shadowMvp);return o;}";
    static const char* pixel_source =
        "cbuffer C:register(b0){row_major float4x4 model;row_major float4x4 mvp;float4 tint;float4 material;float4 animation;row_major float4x4 shadowMvp;float4 shadow;float4 output;float4 shadowLight;row_major float4x4 bones[32];}"
        "Texture2D baseTexture:register(t0);SamplerState baseSampler:register(s0);Texture2D shadowMap:register(t5);SamplerComparisonState shadowSampler:register(s5);"
        "float ShadowValue(float4 p,float3 world,float3 n){if(shadow.x<.5||p.w<=0)return 1;float3 q=p.xyz/p.w;float2 uv=float2(q.x*.5+.5,-q.y*.5+.5);if(any(uv<0)||any(uv>1)||q.z<0||q.z>1)return 1;float3 L=shadowLight.w>1.5?normalize(shadowLight.xyz-world):normalize(shadowLight.xyz);float bias=output.y+output.z*(1-saturate(dot(normalize(n),L)));float sum=0;[unroll]for(int y=-1;y<=1;++y)[unroll]for(int x=-1;x<=1;++x)sum+=shadowMap.SampleCmpLevelZero(shadowSampler,uv+float2(x,y)*shadow.z,q.z-bias);return sum/9;}"
        "float3 ToLinear(float3 c){return lerp(c/12.92,pow((c+.055)/1.055,2.4),step(.04045,c));}"
        "float4 main(float4 p:SV_POSITION,float3 n:NORMAL,float2 uv:TEXCOORD0,float3 world:TEXCOORD1,float4 sp:TEXCOORD2):SV_TARGET{"
        "float4 base=tint;if(material.x>.5){float4 sample=baseTexture.Sample(baseSampler,uv);"
        "if(sample.a>.0001)sample.rgb/=sample.a;base*=sample;}if(material.w>=0&&base.a<material.w)discard;"
        "float l=.28+.72*max(0,dot(normalize(n),normalize(float3(-.35,.8,-.45))))*ShadowValue(sp,world,n);"
        "float light=material.y>.5?1:l+material.z;float3 color=base.rgb*light;return output.x>.5?float4(max(ToLinear(color),0),base.a):float4(color,base.a);}";
    static const char* pbr_vertex_source =
        "cbuffer P:register(b0){row_major float4x4 model;row_major float4x4 mvp;row_major float4x4 normalMatrix;"
        "float4 objectColor;float4 baseFactor;float4 surfaceFactors;float4 emissiveAlpha;float4 textureFlags;"
        "float4 cameraPosition;float4 ambientLight;float4 directionalDirection;float4 directionalColor;"
        "float4 localPositionType[4];float4 localDirectionRange[4];float4 localColorIntensity[4];float4 localCone[4];"
        "float4 animation;row_major float4x4 shadowMvp;float4 shadow;float4 output;row_major float4x4 bones[32];}"
        "cbuffer B:register(b1){row_major float4x4 modelBones[128];}"
        "struct I{float3 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;float4 j:BLENDINDICES;float4 w:BLENDWEIGHT;float4 t:TANGENT;};"
        "struct O{float4 p:SV_POSITION;float3 world:TEXCOORD1;float3 n:NORMAL;float4 t:TANGENT;float2 uv:TEXCOORD0;float4 sp:TEXCOORD2;};"
        "O main(I i){O o;float4 p=float4(i.p,1);float3 n=i.n;float4 t=i.t;if(animation.x>.5){float4x4 s;"
        "if(animation.x>1.5)s=modelBones[(uint)i.j.x]*i.w.x+modelBones[(uint)i.j.y]*i.w.y+modelBones[(uint)i.j.z]*i.w.z+modelBones[(uint)i.j.w]*i.w.w;"
        "else s=bones[(uint)i.j.x]*i.w.x+bones[(uint)i.j.y]*i.w.y+bones[(uint)i.j.z]*i.w.z+bones[(uint)i.j.w]*i.w.w;"
        "p=mul(p,s);n=mul(float4(n,0),s).xyz;t.xyz=mul(float4(t.xyz,0),s).xyz;}"
        "float4 world=mul(p,model);o.p=mul(p,mvp);o.world=world.xyz;"
        "o.n=normalize(mul(float4(n,0),normalMatrix).xyz);float3 wt=mul(float4(t.xyz,0),model).xyz;"
        "o.t=float4(normalize(wt-o.n*dot(o.n,wt)),t.w);o.uv=i.uv;o.sp=mul(p,shadowMvp);return o;}";
    static const char* pbr_pixel_source =
        "cbuffer P:register(b0){row_major float4x4 model;row_major float4x4 mvp;row_major float4x4 normalMatrix;"
        "float4 objectColor;float4 baseFactor;float4 surfaceFactors;float4 emissiveAlpha;float4 textureFlags;"
        "float4 cameraPosition;float4 ambientLight;float4 directionalDirection;float4 directionalColor;"
        "float4 localPositionType[4];float4 localDirectionRange[4];float4 localColorIntensity[4];float4 localCone[4];"
        "float4 animation;row_major float4x4 shadowMvp;float4 shadow;float4 output;row_major float4x4 bones[32];}"
        "Texture2D baseTexture:register(t0);Texture2D normalMap:register(t1);Texture2D ormTexture:register(t2);Texture2D emissiveTexture:register(t3);"
        "SamplerState baseSampler:register(s0);SamplerState normalSampler:register(s1);SamplerState ormSampler:register(s2);SamplerState emissiveSampler:register(s3);Texture2D shadowMap:register(t5);SamplerComparisonState shadowSampler:register(s5);"
        "static const float PI=3.14159265359;"
        "float3 F(float3 f0,float v){return f0+(1-f0)*pow(1-v,5);}"
        "float D(float nh,float r){float a=r*r;float a2=a*a;float q=nh*nh*(a2-1)+1;return a2/max(PI*q*q,.0001);}"
        "float G1(float nv,float r){float k=(r+1)*(r+1)/8;return nv/max(nv*(1-k)+k,.0001);}"
        "float3 Shade(float3 N,float3 V,float3 L,float3 radiance,float3 base,float metal,float rough){float3 H=normalize(V+L);"
        "float nl=saturate(dot(N,L));float nv=saturate(dot(N,V));float vh=saturate(dot(V,H));float nh=saturate(dot(N,H));"
        "float3 f0=lerp(float3(.04,.04,.04),base,metal);float3 fresnel=F(f0,vh);float geometry=G1(nv,rough)*G1(nl,rough);"
        "float3 spec=D(nh,rough)*geometry*fresnel/max(4*nv*nl,.0001);float3 kd=(1-fresnel)*(1-metal);"
        "return (kd*base/PI+spec)*radiance*nl;}"
        "float ShadowValue(float4 p,float3 N,float3 L){if(shadow.x<.5||p.w<=0)return 1;float3 q=p.xyz/p.w;float2 uv=float2(q.x*.5+.5,-q.y*.5+.5);if(any(uv<0)||any(uv>1)||q.z<0||q.z>1)return 1;float bias=output.y+output.z*(1-saturate(dot(N,L)));float sum=0;[unroll]for(int y=-1;y<=1;++y)[unroll]for(int x=-1;x<=1;++x)sum+=shadowMap.SampleCmpLevelZero(shadowSampler,uv+float2(x,y)*shadow.w,q.z-bias);return sum/9;}"
        "float3 ApplyLdrOutputTransfer(float3 c){float3 low=c*12.92;float3 high=1.055*pow(max(c,0),1.0/2.4)-.055;return lerp(low,high,step(.0031308,c));}"
        "float4 main(float4 p:SV_POSITION,float3 world:TEXCOORD1,float3 inputNormal:NORMAL,float4 inputTangent:TANGENT,float2 uv:TEXCOORD0,float4 sp:TEXCOORD2,bool front:SV_IsFrontFace):SV_TARGET{"
        "float4 sampled=textureFlags.x>.5?baseTexture.Sample(baseSampler,uv):float4(1,1,1,1);float4 base=baseFactor*objectColor*sampled;"
        "if(emissiveAlpha.w>=0&&base.a<emissiveAlpha.w)discard;float3 N=normalize(inputNormal);if(!front)N=-N;"
        "float3 T=normalize(inputTangent.xyz-N*dot(N,inputTangent.xyz));float3 B=normalize(cross(N,T)*inputTangent.w);"
        "if(textureFlags.y>.5){float3 mapped=normalMap.Sample(normalSampler,uv).xyz*2-1;mapped.xy*=surfaceFactors.z;N=normalize(T*mapped.x+B*mapped.y+N*mapped.z);}"
        "float3 orm=textureFlags.z>.5?ormTexture.Sample(ormSampler,uv).rgb:float3(1,1,1);"
        "float ao=lerp(1,orm.r,surfaceFactors.w);float rough=clamp(surfaceFactors.y*orm.g,.045,1);float metal=saturate(surfaceFactors.x*orm.b);"
        "float3 V=normalize(cameraPosition.xyz-world);float3 color=ambientLight.rgb*ambientLight.w*base.rgb*ao;"
        "if(directionalDirection.w>.5){float3 L=normalize(directionalDirection.xyz);float sf=shadow.y<1.5?ShadowValue(sp,N,L):1;color+=Shade(N,V,L,directionalColor.rgb*directionalColor.w*sf,base.rgb,metal,rough);}"
        "[unroll]for(int light=0;light<4;++light){float type=localPositionType[light].w;if(type>.5){float3 delta=localPositionType[light].xyz-world;"
        "float distance=length(delta);float range=max(localDirectionRange[light].w,.0001);if(distance<range){float3 L=delta/max(distance,.0001);"
        "float ratio=distance/range;float attenuation=pow(saturate(1-ratio*ratio),2)/(1+2*ratio*ratio);"
        "if(type>1.5){float spot=dot(-L,normalize(localDirectionRange[light].xyz));attenuation*=smoothstep(localCone[light].y,localCone[light].x,spot);}"
        "float sf=shadow.y>1.5&&abs(shadow.z-light)<.5?ShadowValue(sp,N,L):1;color+=Shade(N,V,L,localColorIntensity[light].rgb*localColorIntensity[light].w*attenuation*sf,base.rgb,metal,rough);}}}"
        "float3 emissive=emissiveAlpha.rgb*(textureFlags.w>.5?emissiveTexture.Sample(emissiveSampler,uv).rgb:float3(1,1,1));"
        "float3 finalColor=max(color+emissive,0);if(output.w>.5){if(output.w<1.5)finalColor=base.rgb;else if(output.w<2.5)finalColor=N*.5+.5;"
        "else if(output.w<3.5)finalColor=rough.xxx;else if(output.w<4.5)finalColor=metal.xxx;else if(output.w<5.5)finalColor=ao.xxx;else finalColor=emissive;}"
        "return output.x>.5?float4(finalColor,base.a):float4(saturate(ApplyLdrOutputTransfer(finalColor)),base.a);}";
    static const char* shadow_vertex_source =
        "cbuffer S:register(b0){row_major float4x4 mvp;float4 alpha;float4 animation;row_major float4x4 bones[32];}"
        "cbuffer B:register(b1){row_major float4x4 modelBones[128];}"
        "struct I{float3 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;float4 j:BLENDINDICES;float4 w:BLENDWEIGHT;};"
        "struct O{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};"
        "O main(I i){O o;float4 p=float4(i.p,1);if(animation.x>.5){float4x4 s;if(animation.x>1.5)s=modelBones[(uint)i.j.x]*i.w.x+modelBones[(uint)i.j.y]*i.w.y+modelBones[(uint)i.j.z]*i.w.z+modelBones[(uint)i.j.w]*i.w.w;else s=bones[(uint)i.j.x]*i.w.x+bones[(uint)i.j.y]*i.w.y+bones[(uint)i.j.z]*i.w.z+bones[(uint)i.j.w]*i.w.w;p=mul(p,s);}o.p=mul(p,mvp);o.uv=i.uv;return o;}";
    static const char* shadow_pixel_source =
        "cbuffer S:register(b0){row_major float4x4 mvp;float4 alpha;float4 animation;row_major float4x4 bones[32];}"
        "Texture2D baseTexture:register(t0);SamplerState baseSampler:register(s0);"
        "float4 main(float4 p:SV_POSITION,float2 uv:TEXCOORD0):SV_TARGET{float value=alpha.z;if(alpha.x>.5)value*=baseTexture.Sample(baseSampler,uv).a;if(alpha.y>=0&&value<alpha.y)discard;return 0;}";
    static const char* post_vertex_source =
        "struct O{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};O main(uint id:SV_VertexID){O o;float2 p=id==0?float2(-1,-1):(id==1?float2(-1,3):float2(3,-1));o.p=float4(p,0,1);o.uv=float2(p.x*.5+.5,1-(p.y*.5+.5));return o;}";
    static const char* post_pixel_source =
        "cbuffer P:register(b0){float4 first;float4 second;}Texture2D sceneTexture:register(t0);Texture2D bloomTexture:register(t1);SamplerState postSampler:register(s0);"
        "float3 SampleBlur(float2 uv,float2 axis){float3 c=sceneTexture.Sample(postSampler,uv).rgb*.4;c+=(sceneTexture.Sample(postSampler,uv+axis).rgb+sceneTexture.Sample(postSampler,uv-axis).rgb)*.24;c+=(sceneTexture.Sample(postSampler,uv+axis*2).rgb+sceneTexture.Sample(postSampler,uv-axis*2).rgb)*.06;return c;}"
        "float3 Tone(float3 x){return saturate((x*(2.51*x+.03))/(x*(2.43*x+.59)+.14));}"
        "float3 Encode(float3 c){float3 low=c*12.92;float3 high=1.055*pow(max(c,0),1.0/2.4)-.055;return lerp(low,high,step(.0031308,c));}"
        "float4 main(float4 p:SV_POSITION,float2 uv:TEXCOORD0):SV_TARGET{if(first.x<.5){float3 c=sceneTexture.Sample(postSampler,uv).rgb;float bright=max(c.r,max(c.g,c.b));return float4(bright>=first.w?c:0,1);}if(first.x<1.5)return float4(SampleBlur(uv,float2(first.y,0)),1);if(first.x<2.5)return float4(SampleBlur(uv,float2(0,first.z)),1);float3 scene=sceneTexture.Sample(postSampler,uv).rgb;float3 bloom=bloomTexture.Sample(postSampler,uv).rgb*second.x;return float4(Encode(Tone(max((scene+bloom)*second.y,0))),1);}";
    static const char* particle_vertex_source =
        "cbuffer V:register(b0){row_major float4x4 vp;float4 cameraRight;float4 cameraUp;float4 atlasOutput;float4 material;}"
        "struct I{float2 corner:POSITION;float2 uv:TEXCOORD0;float4 positionSize:TEXCOORD1;float4 color:COLOR0;float4 rotationUv:TEXCOORD2;};"
        "struct O{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR0;};"
        "O main(I i){O o;float c=cos(i.rotationUv.x),s=sin(i.rotationUv.x);float2 q=float2(i.corner.x*c-i.corner.y*s,i.corner.x*s+i.corner.y*c)*i.positionSize.w;float3 world=i.positionSize.xyz+cameraRight.xyz*q.x+cameraUp.xyz*q.y;o.p=mul(float4(world,1),vp);o.uv=i.rotationUv.yz+i.uv*atlasOutput.xy;o.color=i.color;return o;}";
    static const char* ribbon_vertex_source =
        "cbuffer V:register(b0){row_major float4x4 vp;float4 cameraRight;float4 cameraUp;float4 atlasOutput;float4 material;}"
        "struct I{float3 p:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR0;};struct O{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR0;};"
        "O main(I i){O o;o.p=mul(float4(i.p,1),vp);o.uv=i.uv;o.color=i.color;return o;}";
    static const char* vfx_pixel_source =
        "cbuffer V:register(b0){row_major float4x4 vp;float4 cameraRight;float4 cameraUp;float4 atlasOutput;float4 material;}"
        "Texture2D effectTexture:register(t0);SamplerState effectSampler:register(s0);"
        "float3 ToLinear(float3 c){return lerp(c/12.92,pow((c+.055)/1.055,2.4),step(.04045,c));}"
        "float4 main(float4 p:SV_POSITION,float2 uv:TEXCOORD0,float4 color:COLOR0):SV_TARGET{float4 sampled=atlasOutput.w>.5?effectTexture.Sample(effectSampler,uv):float4(1,1,1,1);if(sampled.a>.0001)sampled.rgb/=sampled.a;float4 base=color*material*sampled;float3 rgb=atlasOutput.z>.5?ToLinear(saturate(base.rgb))*max(cameraRight.w,1):saturate(base.rgb*max(cameraRight.w,1));return float4(rgb,base.a);}";
    ID3D11Device* device = (ID3D11Device*)smile_graphics_directx_device();
    ID3DBlob* vs = 0;
    ID3DBlob* ps = 0;
    ID3DBlob* pbr_vs = 0;
    ID3DBlob* pbr_ps = 0;
    ID3DBlob* shadow_vs = 0;
    ID3DBlob* shadow_ps = 0;
    ID3DBlob* post_vs = 0;
    ID3DBlob* post_ps = 0;
    ID3DBlob* particle_vs = 0;
    ID3DBlob* ribbon_vs = 0;
    ID3DBlob* vfx_ps = 0;
    D3D11_INPUT_ELEMENT_DESC elements[5] = {};
    D3D11_INPUT_ELEMENT_DESC pbr_elements[6] = {};
    D3D11_INPUT_ELEMENT_DESC particle_elements[5] = {};
    D3D11_INPUT_ELEMENT_DESC ribbon_elements[3] = {};
    D3D11_BUFFER_DESC buffer = {};
    D3D11_DEPTH_STENCIL_DESC depth = {};
    D3D11_DEPTH_STENCIL_DESC depth_read = {};
    D3D11_RASTERIZER_DESC raster = {};
    D3D11_BLEND_DESC blend = {};
    HRESULT result = S_OK;
    HRESULT pbr_result = S_OK;
    if (device == 0) return 0;
    elements[0].SemanticName = "POSITION"; elements[0].Format = DXGI_FORMAT_R32G32B32_FLOAT; elements[0].InputSlotClass = D3D11_INPUT_PER_VERTEX_DATA;
    elements[1].SemanticName = "NORMAL"; elements[1].Format = DXGI_FORMAT_R32G32B32_FLOAT; elements[1].AlignedByteOffset = 12; elements[1].InputSlotClass = D3D11_INPUT_PER_VERTEX_DATA;
    elements[2].SemanticName = "TEXCOORD"; elements[2].Format = DXGI_FORMAT_R32G32_FLOAT; elements[2].AlignedByteOffset = 24; elements[2].InputSlotClass = D3D11_INPUT_PER_VERTEX_DATA;
    elements[3].SemanticName = "BLENDINDICES"; elements[3].Format = DXGI_FORMAT_R32G32B32A32_FLOAT; elements[3].AlignedByteOffset = 32; elements[3].InputSlotClass = D3D11_INPUT_PER_VERTEX_DATA;
    elements[4].SemanticName = "BLENDWEIGHT"; elements[4].Format = DXGI_FORMAT_R32G32B32A32_FLOAT; elements[4].AlignedByteOffset = 48; elements[4].InputSlotClass = D3D11_INPUT_PER_VERTEX_DATA;
    if (smile_vertex_shader3d == 0)
    {
        result = smile_3d_compile(device, vertex_source, "main", "vs_4_0", &vs);
        if (SUCCEEDED(result)) result = smile_3d_compile(device, pixel_source, "main", "ps_4_0", &ps);
        if (SUCCEEDED(result)) result = device->CreateVertexShader(
            vs->GetBufferPointer(), vs->GetBufferSize(), 0, &smile_vertex_shader3d);
        if (SUCCEEDED(result)) result = device->CreatePixelShader(
            ps->GetBufferPointer(), ps->GetBufferSize(), 0, &smile_pixel_shader3d);
        if (SUCCEEDED(result)) result = device->CreateInputLayout(
            elements, 5, vs->GetBufferPointer(), vs->GetBufferSize(), &smile_input_layout3d);
        buffer.ByteWidth = sizeof(SmileConstants3D);
        buffer.Usage = D3D11_USAGE_DEFAULT;
        buffer.BindFlags = D3D11_BIND_CONSTANT_BUFFER;
        if (SUCCEEDED(result)) result = device->CreateBuffer(&buffer, 0, &smile_constant_buffer3d);
        buffer.ByteWidth = sizeof(SmileMatrix3D) * SMILE_3D_MAX_MODEL_ANIMATION_BONES;
        if (SUCCEEDED(result)) result = device->CreateBuffer(&buffer, 0, &smile_model_palette_buffer3d);
        depth.DepthEnable = TRUE;
        depth.DepthWriteMask = D3D11_DEPTH_WRITE_MASK_ALL;
        depth.DepthFunc = D3D11_COMPARISON_LESS;
        if (SUCCEEDED(result)) result = device->CreateDepthStencilState(&depth, &smile_depth_state3d);
        depth_read.DepthEnable = TRUE;
        depth_read.DepthWriteMask = D3D11_DEPTH_WRITE_MASK_ZERO;
        depth_read.DepthFunc = D3D11_COMPARISON_LESS;
        if (SUCCEEDED(result))
            result = device->CreateDepthStencilState(&depth_read, &smile_depth_read_state3d);
        raster.FillMode = D3D11_FILL_SOLID;
        raster.CullMode = D3D11_CULL_NONE;
        raster.DepthClipEnable = TRUE;
        raster.MultisampleEnable = TRUE;
        if (SUCCEEDED(result)) result = device->CreateRasterizerState(&raster, &smile_raster_state3d);
        raster.CullMode = D3D11_CULL_BACK;
        if (SUCCEEDED(result))
            result = device->CreateRasterizerState(&raster, &smile_cull_raster_state3d);
        blend.RenderTarget[0].BlendEnable = TRUE;
        blend.RenderTarget[0].SrcBlend = D3D11_BLEND_SRC_ALPHA;
        blend.RenderTarget[0].DestBlend = D3D11_BLEND_INV_SRC_ALPHA;
        blend.RenderTarget[0].BlendOp = D3D11_BLEND_OP_ADD;
        blend.RenderTarget[0].SrcBlendAlpha = D3D11_BLEND_ONE;
        blend.RenderTarget[0].DestBlendAlpha = D3D11_BLEND_INV_SRC_ALPHA;
        blend.RenderTarget[0].BlendOpAlpha = D3D11_BLEND_OP_ADD;
        blend.RenderTarget[0].RenderTargetWriteMask = D3D11_COLOR_WRITE_ENABLE_ALL;
        if (SUCCEEDED(result)) result = device->CreateBlendState(&blend, &smile_blend_state3d);
        blend.RenderTarget[0].DestBlend = D3D11_BLEND_ONE;
        blend.RenderTarget[0].DestBlendAlpha = D3D11_BLEND_ONE;
        if (SUCCEEDED(result))
            result = device->CreateBlendState(&blend, &smile_additive_blend_state3d);
        if (SUCCEEDED(result)) result = smile_3d_compile(
            device, particle_vertex_source, "main", "vs_4_0", &particle_vs);
        if (SUCCEEDED(result)) result = smile_3d_compile(
            device, ribbon_vertex_source, "main", "vs_4_0", &ribbon_vs);
        if (SUCCEEDED(result)) result = smile_3d_compile(
            device, vfx_pixel_source, "main", "ps_4_0", &vfx_ps);
        if (SUCCEEDED(result)) result = device->CreateVertexShader(
            particle_vs->GetBufferPointer(), particle_vs->GetBufferSize(), 0,
            &smile_particle_vertex_shader3d);
        if (SUCCEEDED(result)) result = device->CreateVertexShader(
            ribbon_vs->GetBufferPointer(), ribbon_vs->GetBufferSize(), 0,
            &smile_ribbon_vertex_shader3d);
        if (SUCCEEDED(result)) result = device->CreatePixelShader(
            vfx_ps->GetBufferPointer(), vfx_ps->GetBufferSize(), 0,
            &smile_vfx_pixel_shader3d);
        particle_elements[0].SemanticName = "POSITION";
        particle_elements[0].Format = DXGI_FORMAT_R32G32_FLOAT;
        particle_elements[0].InputSlotClass = D3D11_INPUT_PER_VERTEX_DATA;
        particle_elements[1].SemanticName = "TEXCOORD";
        particle_elements[1].SemanticIndex = 0;
        particle_elements[1].Format = DXGI_FORMAT_R32G32_FLOAT;
        particle_elements[1].AlignedByteOffset = 8;
        particle_elements[1].InputSlotClass = D3D11_INPUT_PER_VERTEX_DATA;
        particle_elements[2].SemanticName = "TEXCOORD";
        particle_elements[2].SemanticIndex = 1;
        particle_elements[2].Format = DXGI_FORMAT_R32G32B32A32_FLOAT;
        particle_elements[2].InputSlot = 1;
        particle_elements[2].InputSlotClass = D3D11_INPUT_PER_INSTANCE_DATA;
        particle_elements[2].InstanceDataStepRate = 1;
        particle_elements[3].SemanticName = "COLOR";
        particle_elements[3].Format = DXGI_FORMAT_R32G32B32A32_FLOAT;
        particle_elements[3].InputSlot = 1;
        particle_elements[3].AlignedByteOffset = 16;
        particle_elements[3].InputSlotClass = D3D11_INPUT_PER_INSTANCE_DATA;
        particle_elements[3].InstanceDataStepRate = 1;
        particle_elements[4].SemanticName = "TEXCOORD";
        particle_elements[4].SemanticIndex = 2;
        particle_elements[4].Format = DXGI_FORMAT_R32G32B32A32_FLOAT;
        particle_elements[4].InputSlot = 1;
        particle_elements[4].AlignedByteOffset = 32;
        particle_elements[4].InputSlotClass = D3D11_INPUT_PER_INSTANCE_DATA;
        particle_elements[4].InstanceDataStepRate = 1;
        if (SUCCEEDED(result)) result = device->CreateInputLayout(
            particle_elements, 5, particle_vs->GetBufferPointer(), particle_vs->GetBufferSize(),
            &smile_particle_input_layout3d);
        ribbon_elements[0].SemanticName = "POSITION";
        ribbon_elements[0].Format = DXGI_FORMAT_R32G32B32_FLOAT;
        ribbon_elements[0].InputSlotClass = D3D11_INPUT_PER_VERTEX_DATA;
        ribbon_elements[1].SemanticName = "TEXCOORD";
        ribbon_elements[1].Format = DXGI_FORMAT_R32G32_FLOAT;
        ribbon_elements[1].AlignedByteOffset = 12;
        ribbon_elements[1].InputSlotClass = D3D11_INPUT_PER_VERTEX_DATA;
        ribbon_elements[2].SemanticName = "COLOR";
        ribbon_elements[2].Format = DXGI_FORMAT_R32G32B32A32_FLOAT;
        ribbon_elements[2].AlignedByteOffset = 20;
        ribbon_elements[2].InputSlotClass = D3D11_INPUT_PER_VERTEX_DATA;
        if (SUCCEEDED(result)) result = device->CreateInputLayout(
            ribbon_elements, 3, ribbon_vs->GetBufferPointer(), ribbon_vs->GetBufferSize(),
            &smile_ribbon_input_layout3d);
        buffer.ByteWidth = sizeof(SmileVfxConstants3D);
        buffer.Usage = D3D11_USAGE_DEFAULT;
        buffer.BindFlags = D3D11_BIND_CONSTANT_BUFFER;
        if (SUCCEEDED(result)) result = device->CreateBuffer(&buffer, 0,
            &smile_vfx_constant_buffer3d);
        if (SUCCEEDED(result))
        {
            static const float quad_vertices[16] = {
                -0.5f, -0.5f, 0.0f, 1.0f,
                -0.5f,  0.5f, 0.0f, 0.0f,
                 0.5f,  0.5f, 1.0f, 0.0f,
                 0.5f, -0.5f, 1.0f, 1.0f
            };
            static const unsigned short quad_indices[6] = { 0, 1, 2, 0, 2, 3 };
            D3D11_SUBRESOURCE_DATA initial = {};
            buffer.ByteWidth = sizeof(quad_vertices);
            buffer.Usage = D3D11_USAGE_IMMUTABLE;
            buffer.BindFlags = D3D11_BIND_VERTEX_BUFFER;
            initial.pSysMem = quad_vertices;
            result = device->CreateBuffer(&buffer, &initial, &smile_particle_quad_vertex_buffer3d);
            buffer.ByteWidth = sizeof(quad_indices);
            buffer.BindFlags = D3D11_BIND_INDEX_BUFFER;
            initial.pSysMem = quad_indices;
            if (SUCCEEDED(result)) result = device->CreateBuffer(
                &buffer, &initial, &smile_particle_quad_index_buffer3d);
        }
        smile_3d_release(vs);
        smile_3d_release(ps);
        smile_3d_release(particle_vs);
        smile_3d_release(ribbon_vs);
        smile_3d_release(vfx_ps);
        if (FAILED(result))
        {
            smile_last_error3d = 10;
            smile_graphics3d_on_device_lost();
            return 0;
        }
    }
    if (smile_shadow_requested3d && smile_shadow_vertex_shader3d == 0)
    {
        D3D11_SAMPLER_DESC comparison = {};
        result = smile_3d_compile(device, shadow_vertex_source, "main", "vs_4_0", &shadow_vs);
        if (SUCCEEDED(result))
            result = smile_3d_compile(device, shadow_pixel_source, "main", "ps_4_0", &shadow_ps);
        if (SUCCEEDED(result)) result = device->CreateVertexShader(
            shadow_vs->GetBufferPointer(), shadow_vs->GetBufferSize(), 0, &smile_shadow_vertex_shader3d);
        if (SUCCEEDED(result)) result = device->CreatePixelShader(
            shadow_ps->GetBufferPointer(), shadow_ps->GetBufferSize(), 0, &smile_shadow_pixel_shader3d);
        if (SUCCEEDED(result)) result = device->CreateInputLayout(
            elements, 5, shadow_vs->GetBufferPointer(), shadow_vs->GetBufferSize(),
            &smile_shadow_input_layout3d);
        buffer.ByteWidth = sizeof(SmileShadowConstants3D);
        buffer.Usage = D3D11_USAGE_DEFAULT;
        buffer.BindFlags = D3D11_BIND_CONSTANT_BUFFER;
        if (SUCCEEDED(result))
            result = device->CreateBuffer(&buffer, 0, &smile_shadow_constant_buffer3d);
        comparison.Filter = D3D11_FILTER_COMPARISON_MIN_MAG_LINEAR_MIP_POINT;
        comparison.AddressU = D3D11_TEXTURE_ADDRESS_BORDER;
        comparison.AddressV = D3D11_TEXTURE_ADDRESS_BORDER;
        comparison.AddressW = D3D11_TEXTURE_ADDRESS_BORDER;
        comparison.BorderColor[0] = comparison.BorderColor[1] =
            comparison.BorderColor[2] = comparison.BorderColor[3] = 1.0f;
        comparison.ComparisonFunc = D3D11_COMPARISON_LESS_EQUAL;
        comparison.MaxLOD = D3D11_FLOAT32_MAX;
        if (SUCCEEDED(result))
            result = device->CreateSamplerState(&comparison, &smile_shadow_sampler3d);
        smile_3d_release(shadow_vs);
        smile_3d_release(shadow_ps);
        if (FAILED(result))
        {
            smile_3d_release(smile_shadow_sampler3d);
            smile_3d_release(smile_shadow_constant_buffer3d);
            smile_3d_release(smile_shadow_input_layout3d);
            smile_3d_release(smile_shadow_pixel_shader3d);
            smile_3d_release(smile_shadow_vertex_shader3d);
            smile_m5_fallback_flags3d |= SMILE_3D_M5_FALLBACK_SHADOW_DISABLED;
        }
    }
    if (smile_post_requested3d && smile_post_vertex_shader3d == 0)
    {
        D3D11_SAMPLER_DESC post_sampler = {};
        result = smile_3d_compile(device, post_vertex_source, "main", "vs_4_0", &post_vs);
        if (SUCCEEDED(result))
            result = smile_3d_compile(device, post_pixel_source, "main", "ps_4_0", &post_ps);
        if (SUCCEEDED(result)) result = device->CreateVertexShader(
            post_vs->GetBufferPointer(), post_vs->GetBufferSize(), 0, &smile_post_vertex_shader3d);
        if (SUCCEEDED(result)) result = device->CreatePixelShader(
            post_ps->GetBufferPointer(), post_ps->GetBufferSize(), 0, &smile_post_pixel_shader3d);
        buffer.ByteWidth = sizeof(SmilePostConstants3D);
        buffer.Usage = D3D11_USAGE_DEFAULT;
        buffer.BindFlags = D3D11_BIND_CONSTANT_BUFFER;
        if (SUCCEEDED(result))
            result = device->CreateBuffer(&buffer, 0, &smile_post_constant_buffer3d);
        post_sampler.Filter = D3D11_FILTER_MIN_MAG_LINEAR_MIP_POINT;
        post_sampler.AddressU = post_sampler.AddressV = post_sampler.AddressW =
            D3D11_TEXTURE_ADDRESS_CLAMP;
        post_sampler.MaxLOD = D3D11_FLOAT32_MAX;
        if (SUCCEEDED(result))
            result = device->CreateSamplerState(&post_sampler, &smile_post_sampler3d);
        smile_3d_release(post_vs);
        smile_3d_release(post_ps);
        if (FAILED(result))
        {
            smile_3d_release(smile_post_sampler3d);
            smile_3d_release(smile_post_constant_buffer3d);
            smile_3d_release(smile_post_pixel_shader3d);
            smile_3d_release(smile_post_vertex_shader3d);
            smile_m5_fallback_flags3d |= SMILE_3D_M5_FALLBACK_HDR_UNAVAILABLE |
                SMILE_3D_M5_FALLBACK_BLOOM_DISABLED |
                SMILE_3D_M5_FALLBACK_TONE_MAPPING_DISABLED |
                SMILE_3D_M5_FALLBACK_DIRECT_LDR;
        }
    }
    if (smile_pbr_pipeline_state3d == SMILE_3D_PBR_PIPELINE_NOT_ATTEMPTED)
    {
        WCHAR forced_failure[2];
        smile_pbr_pipeline_attempt_count3d++;
        pbr_result = GetEnvironmentVariableW(
            L"SMILE_TEST_RENDERER3D_FORCE_PBR_FAILURE", forced_failure, 2) != 0
            ? E_FAIL
            : smile_3d_compile(device, pbr_vertex_source, "main", "vs_4_0", &pbr_vs);
        if (SUCCEEDED(pbr_result))
            pbr_result = smile_3d_compile(device, pbr_pixel_source, "main", "ps_4_0", &pbr_ps);
        if (SUCCEEDED(pbr_result)) pbr_result = device->CreateVertexShader(
            pbr_vs->GetBufferPointer(), pbr_vs->GetBufferSize(), 0, &smile_pbr_vertex_shader3d);
        if (SUCCEEDED(pbr_result)) pbr_result = device->CreatePixelShader(
            pbr_ps->GetBufferPointer(), pbr_ps->GetBufferSize(), 0, &smile_pbr_pixel_shader3d);
        memcpy(pbr_elements, elements, sizeof(elements));
        pbr_elements[5].SemanticName = "TANGENT";
        pbr_elements[5].Format = DXGI_FORMAT_R32G32B32A32_FLOAT;
        pbr_elements[5].AlignedByteOffset = 64;
        pbr_elements[5].InputSlotClass = D3D11_INPUT_PER_VERTEX_DATA;
        if (SUCCEEDED(pbr_result)) pbr_result = device->CreateInputLayout(
            pbr_elements, 6, pbr_vs->GetBufferPointer(), pbr_vs->GetBufferSize(),
            &smile_pbr_input_layout3d);
        buffer.ByteWidth = sizeof(SmilePbrConstants3D);
        buffer.Usage = D3D11_USAGE_DEFAULT;
        buffer.BindFlags = D3D11_BIND_CONSTANT_BUFFER;
        if (SUCCEEDED(pbr_result))
            pbr_result = device->CreateBuffer(&buffer, 0, &smile_pbr_constant_buffer3d);
        smile_3d_release(pbr_vs);
        smile_3d_release(pbr_ps);
        if (FAILED(pbr_result))
        {
            smile_3d_release(smile_pbr_constant_buffer3d);
            smile_3d_release(smile_pbr_input_layout3d);
            smile_3d_release(smile_pbr_pixel_shader3d);
            smile_3d_release(smile_pbr_vertex_shader3d);
            smile_pbr_shader_available3d = 0;
            smile_pbr_pipeline_state3d = SMILE_3D_PBR_PIPELINE_UNAVAILABLE;
            smile_pbr_pipeline_failure3d = 44;
            smile_last_error3d = 44;
            return 1;
        }
        smile_pbr_pipeline_state3d = SMILE_3D_PBR_PIPELINE_AVAILABLE;
        smile_pbr_pipeline_failure3d = 0;
    }
    smile_pbr_shader_available3d =
        smile_pbr_pipeline_state3d == SMILE_3D_PBR_PIPELINE_AVAILABLE;
    return 1;
}

static const unsigned char* smile_3d_model_animation_record(
    const SmileModel3D* model, int chunk, unsigned int index)
{
    if (model == 0 || !model->has_animation || model->animation_data == 0 ||
        chunk < 0 || chunk >= 9 ||
        index >= model->animation_chunks[chunk].count) return 0;
    return model->animation_data + model->animation_chunks[chunk].offset +
        index * model->animation_chunks[chunk].stride;
}

static int smile_3d_model_clip_index(const SmileModel3D* model, const char* name)
{
    if (model == 0 || !model->has_animation || name == 0) return -1;
    for (unsigned int index = 0; index < model->animation_clip_count; ++index)
    {
        const unsigned char* record = smile_3d_model_animation_record(model, 3, index);
        unsigned int offset = smile_3d_read_u32(record);
        if (offset < model->string_bytes && strcmp(model->strings + offset, name) == 0) return (int)index;
    }
    return -1;
}

static int smile_3d_model_socket_index(const SmileModel3D* model, const char* name)
{
    if (model == 0 || !model->has_animation || name == 0) return -1;
    for (unsigned int index = 0; index < model->animation_socket_count; ++index)
    {
        const unsigned char* record = smile_3d_model_animation_record(model, 7, index);
        unsigned int offset = smile_3d_read_u32(record);
        if (offset < model->string_bytes && strcmp(model->strings + offset, name) == 0) return (int)index;
    }
    return -1;
}

static void smile_3d_model_channel(const SmileModel3D* model, const unsigned char* track,
    int channel, unsigned int time_ms, unsigned int duration_ms, unsigned int rate,
    unsigned int sample_count, float* output, unsigned int components)
{
    unsigned int first = smile_3d_read_u32(track + 16 + channel * 8);
    unsigned int count = smile_3d_read_u32(track + 20 + channel * 8);
    const unsigned char* frames = model->animation_data + model->animation_chunks[5].offset;
    if (count == 0 || first == 0xFFFFFFFFU) return;
    if (count == 1)
    {
        for (unsigned int component = 0; component < components; ++component)
            output[component] = smile_3d_read_float(frames + (first + component) * 4);
        return;
    }
    unsigned long long scaled = (unsigned long long)time_ms * rate;
    unsigned int first_sample;
    float amount;
    if (time_ms >= duration_ms)
    {
        first_sample = sample_count - 1;
        amount = 0.0f;
    }
    else
    {
        unsigned int final_first = sample_count - 2;
        unsigned long long final_start = (unsigned long long)final_first * 1000U;
        unsigned long long final_end = (unsigned long long)duration_ms * rate;
        if (scaled >= final_start)
        {
            first_sample = final_first;
            amount = final_end <= final_start ? 0.0f :
                (float)(scaled - final_start) / (float)(final_end - final_start);
        }
        else
        {
            first_sample = (unsigned int)(scaled / 1000U);
            amount = (float)(scaled % 1000U) / 1000.0f;
        }
    }
    unsigned int second_sample = first_sample + 1 < sample_count ? first_sample + 1 : first_sample;
    if (channel == 1)
    {
        float dot = 0.0f;
        for (unsigned int component = 0; component < 4; ++component)
            dot += smile_3d_read_float(frames + (first + first_sample * 4 + component) * 4) *
                smile_3d_read_float(frames + (first + second_sample * 4 + component) * 4);
        float direction = dot < 0.0f ? -1.0f : 1.0f;
        float length = 0.0f;
        for (unsigned int component = 0; component < 4; ++component)
        {
            float a = smile_3d_read_float(frames + (first + first_sample * 4 + component) * 4);
            float b = smile_3d_read_float(frames + (first + second_sample * 4 + component) * 4) * direction;
            output[component] = smile_3d_lerp(a, b, amount);
            length += output[component] * output[component];
        }
        length = sqrtf(length);
        if (length > 0.000001f)
            for (unsigned int component = 0; component < 4; ++component) output[component] /= length;
    }
    else
    {
        for (unsigned int component = 0; component < components; ++component)
        {
            float a = smile_3d_read_float(frames + (first + first_sample * components + component) * 4);
            float b = smile_3d_read_float(frames + (first + second_sample * components + component) * 4);
            output[component] = smile_3d_lerp(a, b, amount);
        }
    }
}

static void smile_3d_model_locals(const SmileModel3D* model, int clip_index, unsigned int time_ms,
    float translation[SMILE_3D_MAX_MODEL_ANIMATION_NODES][3],
    float rotation[SMILE_3D_MAX_MODEL_ANIMATION_NODES][4],
    float scale[SMILE_3D_MAX_MODEL_ANIMATION_NODES][3])
{
    for (unsigned int node = 0; node < model->animation_node_count; ++node)
    {
        const unsigned char* record = smile_3d_model_animation_record(model, 0, node);
        for (unsigned int component = 0; component < 3; ++component)
        {
            translation[node][component] = smile_3d_read_float(record + 16 + component * 4);
            scale[node][component] = smile_3d_read_float(record + 44 + component * 4);
        }
        for (unsigned int component = 0; component < 4; ++component)
            rotation[node][component] = smile_3d_read_float(record + 28 + component * 4);
    }
    if (clip_index < 0 || clip_index >= model->animation_clip_count) return;
    const unsigned char* clip = smile_3d_model_animation_record(model, 3, (unsigned int)clip_index);
    unsigned int duration = smile_3d_read_u32(clip + 4);
    unsigned int rate = smile_3d_read_u32(clip + 8);
    unsigned int samples = smile_3d_read_u32(clip + 12);
    unsigned int first = smile_3d_read_u32(clip + 16);
    unsigned int count = smile_3d_read_u32(clip + 20);
    for (unsigned int index = first; index < first + count; ++index)
    {
        const unsigned char* track = smile_3d_model_animation_record(model, 4, index);
        unsigned int node = smile_3d_read_u32(track + 4);
        unsigned int flags = smile_3d_read_u32(track + 8);
        if ((flags & 1U) != 0) smile_3d_model_channel(model, track, 0, time_ms, duration,
            rate, samples, translation[node], 3);
        if ((flags & 4U) != 0) smile_3d_model_channel(model, track, 1, time_ms, duration,
            rate, samples, rotation[node], 4);
        if ((flags & 16U) != 0) smile_3d_model_channel(model, track, 2, time_ms, duration,
            rate, samples, scale[node], 3);
    }
}

static void smile_3d_remove_model_root(const SmileModel3D* model, int clip_index,
    float translation[SMILE_3D_MAX_MODEL_ANIMATION_NODES][3],
    float rotation[SMILE_3D_MAX_MODEL_ANIMATION_NODES][4])
{
    if (clip_index < 0 || clip_index >= model->animation_clip_count) return;
    const unsigned char* clip = smile_3d_model_animation_record(model, 3, clip_index);
    unsigned int root_index = smile_3d_read_u32(clip + 36);
    if (root_index == 0xFFFFFFFFU) return;
    const unsigned char* root = smile_3d_model_animation_record(model, 8, root_index);
    if (smile_3d_read_u32(root + 16) == 0) return;
    unsigned int node = smile_3d_read_u32(root + 4);
    unsigned int axes = smile_3d_read_u32(root + 8);
    const unsigned char* bind = smile_3d_model_animation_record(model, 0, node);
    for (unsigned int component = 0; component < 3; ++component)
        if ((axes & (1U << component)) != 0)
            translation[node][component] = smile_3d_read_float(bind + 16 + component * 4);
    if (smile_3d_read_u32(root + 12) == 0) return;
    float twist_length = sqrtf(rotation[node][1] * rotation[node][1] +
        rotation[node][3] * rotation[node][3]);
    float twist_y = twist_length > 0.000001f ? rotation[node][1] / twist_length : 0.0f;
    float twist_w = twist_length > 0.000001f ? rotation[node][3] / twist_length : 1.0f;
    float swing[4] = {
        rotation[node][0] * twist_w + rotation[node][2] * twist_y,
        -rotation[node][3] * twist_y + rotation[node][1] * twist_w,
        -rotation[node][0] * twist_y + rotation[node][2] * twist_w,
        rotation[node][3] * twist_w + rotation[node][1] * twist_y
    };
    float bind_y = smile_3d_read_float(bind + 32);
    float bind_w = smile_3d_read_float(bind + 40);
    float bind_length = sqrtf(bind_y * bind_y + bind_w * bind_w);
    bind_y = bind_length > 0.000001f ? bind_y / bind_length : 0.0f;
    bind_w = bind_length > 0.000001f ? bind_w / bind_length : 1.0f;
    rotation[node][0] = swing[0] * bind_w - swing[2] * bind_y;
    rotation[node][1] = swing[3] * bind_y + swing[1] * bind_w;
    rotation[node][2] = swing[0] * bind_y + swing[2] * bind_w;
    rotation[node][3] = swing[3] * bind_w - swing[1] * bind_y;
}

static void smile_3d_update_model_pose(SmileAnimator3D* animator)
{
    SmileModel3D* model = smile_3d_model_resource(animator->model_handle);
    float translation[SMILE_3D_MAX_MODEL_ANIMATION_NODES][3] = {};
    float rotation[SMILE_3D_MAX_MODEL_ANIMATION_NODES][4] = {};
    float scale[SMILE_3D_MAX_MODEL_ANIMATION_NODES][3] = {};
    float destination_translation[SMILE_3D_MAX_MODEL_ANIMATION_NODES][3] = {};
    float destination_rotation[SMILE_3D_MAX_MODEL_ANIMATION_NODES][4] = {};
    float destination_scale[SMILE_3D_MAX_MODEL_ANIMATION_NODES][3] = {};
    if (model == 0 || !model->has_animation) return;
    smile_3d_model_locals(model, animator->clip_index, animator->time_ms,
        translation, rotation, scale);
    if (animator->root_motion_mode != 0)
        smile_3d_remove_model_root(model, animator->clip_index, translation, rotation);
    if (animator->destination_clip >= 0)
    {
        smile_3d_model_locals(model, animator->destination_clip, animator->destination_time_ms,
            destination_translation, destination_rotation, destination_scale);
        if (animator->root_motion_mode != 0)
            smile_3d_remove_model_root(model, animator->destination_clip,
                destination_translation, destination_rotation);
        float amount = animator->fade_duration_ms == 0 ? 1.0f :
            (float)animator->fade_elapsed_ms / (float)animator->fade_duration_ms;
        if (amount > 1.0f) amount = 1.0f;
        for (unsigned int node = 0; node < model->animation_node_count; ++node)
        {
            for (unsigned int component = 0; component < 3; ++component)
            {
                translation[node][component] = smile_3d_lerp(translation[node][component],
                    destination_translation[node][component], amount);
                scale[node][component] = smile_3d_lerp(scale[node][component],
                    destination_scale[node][component], amount);
            }
            float dot = 0.0f;
            for (unsigned int component = 0; component < 4; ++component)
                dot += rotation[node][component] * destination_rotation[node][component];
            float direction = dot < 0.0f ? -1.0f : 1.0f;
            float length = 0.0f;
            for (unsigned int component = 0; component < 4; ++component)
            {
                rotation[node][component] = smile_3d_lerp(rotation[node][component],
                    destination_rotation[node][component] * direction, amount);
                length += rotation[node][component] * rotation[node][component];
            }
            length = sqrtf(length);
            if (length > 0.000001f)
                for (unsigned int component = 0; component < 4; ++component)
                    rotation[node][component] /= length;
        }
    }
    for (unsigned int node = 0; node < model->animation_node_count; ++node)
    {
        const unsigned char* record = smile_3d_model_animation_record(model, 0, node);
        int parent = (int)smile_3d_read_u32(record + 4);
        SmileMatrix3D local = smile_3d_pose(translation[node][0], translation[node][1], translation[node][2],
            rotation[node][0], rotation[node][1], rotation[node][2], rotation[node][3],
            scale[node][0], scale[node][1], scale[node][2]);
        animator->node_global[node] = parent < 0 ? local : smile_3d_multiply(local, animator->node_global[parent]);
    }
    for (unsigned int bone = 0; bone < model->animation_bone_count; ++bone)
    {
        const unsigned char* record = smile_3d_model_animation_record(model, 2, bone);
        unsigned int node = smile_3d_read_u32(record);
        SmileMatrix3D inverse = {};
        for (unsigned int component = 0; component < 16; ++component)
            inverse.m[component] = smile_3d_read_float(record + 16 + component * 4);
        animator->bones[bone] = smile_3d_multiply(inverse, animator->node_global[node]);
    }
    for (unsigned int bone = model->animation_bone_count;
        bone < SMILE_3D_MAX_MODEL_ANIMATION_BONES; ++bone) animator->bones[bone] = smile_3d_identity();
    animator->pose_revision++;
    if (animator->pose_revision == 0) animator->pose_revision = 1;
}

static void smile_3d_clear_model_events(SmileAnimator3D* animator)
{
    animator->event_head = 0;
    animator->event_count = 0;
    animator->event_overflowed = 0;
    animator->dropped_event_count = 0;
}

static void smile_3d_drop_model_events(SmileAnimator3D* animator, unsigned long long count)
{
    if (count == 0) return;
    animator->event_overflowed = 1;
    unsigned long long total = (unsigned long long)animator->dropped_event_count + count;
    animator->dropped_event_count = total > UINT_MAX ? UINT_MAX : (unsigned int)total;
    smile_last_error3d = 49;
}

static void smile_3d_queue_model_event(SmileAnimator3D* animator, unsigned int event_index)
{
    if (animator->event_count >= SMILE_3D_MAX_PENDING_MODEL_EVENTS)
    {
        smile_3d_drop_model_events(animator, 1);
        return;
    }
    unsigned int tail = (animator->event_head + animator->event_count) %
        SMILE_3D_MAX_PENDING_MODEL_EVENTS;
    animator->pending_events[tail] = event_index;
    animator->event_count++;
}

static unsigned int smile_3d_count_model_event_range(SmileModel3D* model,
    unsigned int first, unsigned int count, unsigned int minimum, unsigned int maximum,
    int include_zero)
{
    unsigned int result = 0;
    for (unsigned int ordinal = 0; ordinal < count; ++ordinal)
    {
        const unsigned char* event_record = smile_3d_model_animation_record(model, 6, first + ordinal);
        unsigned int time = smile_3d_read_u32(event_record + 4);
        if ((include_zero && time == 0) || (time > minimum && time <= maximum)) result++;
    }
    return result;
}

static void smile_3d_queue_model_event_range(SmileAnimator3D* animator, SmileModel3D* model,
    unsigned int first, unsigned int count, unsigned int minimum, unsigned int maximum,
    int include_zero)
{
    for (unsigned int ordinal = 0; ordinal < count; ++ordinal)
    {
        unsigned int event_index = first + ordinal;
        const unsigned char* event_record = smile_3d_model_animation_record(model, 6, event_index);
        unsigned int time = smile_3d_read_u32(event_record + 4);
        if (!((include_zero && time == 0) || (time > minimum && time <= maximum))) continue;
        smile_3d_queue_model_event(animator, event_index);
    }
}

static void smile_3d_queue_model_events(SmileAnimator3D* animator, SmileModel3D* model,
    int clip_index, unsigned int previous, unsigned int current, unsigned int advance, int mode)
{
    if (clip_index < 0) return;
    const unsigned char* clip = smile_3d_model_animation_record(model, 3, clip_index);
    unsigned int duration = smile_3d_read_u32(clip + 4);
    unsigned int first = smile_3d_read_u32(clip + 24);
    unsigned int count = smile_3d_read_u32(clip + 28);
    if (count == 0) return;
    unsigned int wraps = mode == 1
        ? (unsigned int)(((unsigned long long)previous + advance) / duration) : 0;
    if (wraps == 0)
    {
        smile_3d_queue_model_event_range(animator, model, first, count, previous, current, 0);
        return;
    }
    smile_3d_queue_model_event_range(animator, model, first, count, previous, duration, 0);
    unsigned int intermediate_wraps = wraps - 1;
    while (intermediate_wraps != 0 &&
        animator->event_count < SMILE_3D_MAX_PENDING_MODEL_EVENTS)
    {
        smile_3d_queue_model_event_range(animator, model, first, count, 0, duration, 1);
        intermediate_wraps--;
    }
    if (intermediate_wraps != 0)
    {
        unsigned int events_per_wrap = smile_3d_count_model_event_range(model,
            first, count, 0, duration, 1);
        smile_3d_drop_model_events(animator,
            (unsigned long long)intermediate_wraps * events_per_wrap);
    }
    smile_3d_queue_model_event_range(animator, model, first, count, 0, current, 1);
}

static void smile_3d_queue_model_time_zero(SmileAnimator3D* animator,
    SmileModel3D* model, int clip_index)
{
    if (clip_index < 0) return;
    const unsigned char* clip = smile_3d_model_animation_record(model, 3, clip_index);
    smile_3d_queue_model_event_range(animator, model, smile_3d_read_u32(clip + 24),
        smile_3d_read_u32(clip + 28), 0, 0, 1);
}

static long long smile_3d_create_model_animator(long long model_handle)
{
    SmileModel3D* model = smile_3d_model_resource(model_handle);
    if (model == 0 || !model->has_animation || !smile_3d_create_pipeline() ||
        smile_model_palette_buffer3d == 0) { smile_last_error3d = 48; return 0; }
    int slot;
    for (slot = 0; slot < SMILE_3D_MAX_ANIMATORS; ++slot)
        if (!smile_animators3d[slot].active) break;
    if (slot == SMILE_3D_MAX_ANIMATORS) { smile_last_error3d = 34; return 0; }
    SmileAnimator3D* animator = &smile_animators3d[slot];
    unsigned short generation = animator->generation == 0 ? 1 : animator->generation;
    ZeroMemory(animator, sizeof(*animator));
    animator->generation = generation;
    animator->active = 1;
    animator->model_animation = 1;
    animator->model_handle = model_handle;
    animator->clip_index = -1;
    animator->destination_clip = -1;
    animator->speed_percent = 100;
    smile_3d_update_model_pose(animator);
    return smile_3d_handle(SMILE_3D_ANIMATOR_HANDLE, slot, animator->generation);
}

static int smile_3d_play_model_animator(SmileAnimator3D* animator, int clip_index,
    int mode, unsigned int speed_percent)
{
    SmileModel3D* model = animator == 0 ? 0 : smile_3d_model_resource(animator->model_handle);
    if (animator == 0 || !animator->model_animation || model == 0 || clip_index < 0 ||
        clip_index >= model->animation_clip_count || mode < 1 || mode > 3 ||
        speed_percent == 0 || speed_percent > 1000) { smile_last_error3d = 48; return 0; }
    animator->clip_index = (signed char)clip_index;
    animator->destination_clip = -1;
    animator->playback_mode = (unsigned char)mode;
    animator->speed_percent = speed_percent;
    animator->time_ms = 0;
    animator->previous_time_ms = 0;
    animator->time_remainder = 0;
    animator->destination_time_remainder = 0;
    animator->complete = 0;
    animator->destination_complete = 0;
    animator->fade_elapsed_ms = animator->fade_duration_ms = 0;
    smile_3d_clear_model_events(animator);
    ZeroMemory(animator->root_delta, sizeof(animator->root_delta));
    smile_3d_queue_model_time_zero(animator, model, clip_index);
    smile_3d_update_model_pose(animator);
    return 1;
}

static int smile_3d_crossfade_model_animator(SmileAnimator3D* animator, int clip_index,
    unsigned int fade_ms, int mode)
{
    SmileModel3D* model = animator == 0 ? 0 : smile_3d_model_resource(animator->model_handle);
    if (animator == 0 || !animator->model_animation || model == 0 || clip_index < 0 ||
        clip_index >= model->animation_clip_count || fade_ms > 600000 || mode < 1 || mode > 3)
    { smile_last_error3d = 48; return 0; }
    if (animator->clip_index < 0 || fade_ms == 0)
        return smile_3d_play_model_animator(animator, clip_index, mode, animator->speed_percent);
    if (animator->destination_clip >= 0)
    {
        animator->clip_index = animator->destination_clip;
        animator->playback_mode = animator->destination_mode;
        animator->time_ms = animator->destination_time_ms;
        animator->previous_time_ms = animator->time_ms;
        animator->time_remainder = animator->destination_time_remainder;
        animator->complete = animator->destination_complete;
    }
    animator->destination_clip = (signed char)clip_index;
    animator->destination_mode = (unsigned char)mode;
    animator->destination_time_ms = 0;
    animator->destination_time_remainder = 0;
    animator->destination_complete = 0;
    animator->fade_elapsed_ms = 0;
    animator->fade_duration_ms = fade_ms;
    animator->complete = 0;
    smile_3d_queue_model_time_zero(animator, model, clip_index);
    smile_3d_update_model_pose(animator);
    return 1;
}

static unsigned int smile_3d_advance_model_time(const SmileModel3D* model, int clip_index,
    unsigned int time, unsigned int advance, int mode, unsigned char* complete, unsigned int* wraps)
{
    const unsigned char* clip = smile_3d_model_animation_record(model, 3, clip_index);
    unsigned int duration = smile_3d_read_u32(clip + 4);
    unsigned long long total = (unsigned long long)time + advance;
    *wraps = 0;
    if (mode == 1)
    {
        *wraps = (unsigned int)(total / duration);
        *complete = 0;
        return (unsigned int)(total % duration);
    }
    *complete = total >= duration;
    return (unsigned int)(total >= duration ? duration : total);
}

static int smile_3d_model_root_sample(const SmileModel3D* model, int clip_index,
    unsigned int time_ms, float* value, unsigned int* components)
{
    if (model == 0 || clip_index < 0 || clip_index >= model->animation_clip_count) return 0;
    const unsigned char* clip = smile_3d_model_animation_record(model, 3, clip_index);
    unsigned int root_index = smile_3d_read_u32(clip + 36);
    if (root_index == 0xFFFFFFFFU) return 0;
    const unsigned char* root = smile_3d_model_animation_record(model, 8, root_index);
    unsigned int node = smile_3d_read_u32(root + 4);
    const unsigned char* node_record = smile_3d_model_animation_record(model, 0, node);
    for (unsigned int component = 0; component < 3; ++component)
        value[component] = smile_3d_read_float(node_record + 16 + component * 4);
    float rotation[4] = {
        smile_3d_read_float(node_record + 28), smile_3d_read_float(node_record + 32),
        smile_3d_read_float(node_record + 36), smile_3d_read_float(node_record + 40)
    };
    unsigned int first = smile_3d_read_u32(clip + 16);
    unsigned int count = smile_3d_read_u32(clip + 20);
    for (unsigned int index = first; index < first + count; ++index)
    {
        const unsigned char* track = smile_3d_model_animation_record(model, 4, index);
        if (smile_3d_read_u32(track + 4) == node && (smile_3d_read_u32(track + 8) & 1U) != 0)
            smile_3d_model_channel(model, track, 0, time_ms, smile_3d_read_u32(clip + 4),
                smile_3d_read_u32(clip + 8), smile_3d_read_u32(clip + 12), value, 3);
        if (smile_3d_read_u32(track + 4) == node && (smile_3d_read_u32(track + 8) & 4U) != 0)
            smile_3d_model_channel(model, track, 1, time_ms, smile_3d_read_u32(clip + 4),
                smile_3d_read_u32(clip + 8), smile_3d_read_u32(clip + 12), rotation, 4);
    }
    value[3] = smile_3d_read_u32(root + 12) == 0 ? 0.0f :
        atan2f(2.0f * (rotation[3] * rotation[1] + rotation[0] * rotation[2]),
            1.0f - 2.0f * (rotation[1] * rotation[1] + rotation[2] * rotation[2])) *
        180.0f / SMILE_3D_PI;
    *components = smile_3d_read_u32(root + 8) |
        (smile_3d_read_u32(root + 12) == 0 ? 0U : 8U);
    return 1;
}

static float smile_3d_root_delta(float current, float previous, int angle)
{
    float result = current - previous;
    if (angle)
    {
        while (result > 180.0f) result -= 360.0f;
        while (result < -180.0f) result += 360.0f;
    }
    return result;
}

static unsigned int smile_3d_scaled_model_advance(unsigned int delta_ms,
    unsigned int speed_percent, unsigned int* remainder)
{
    unsigned long long scaled = (unsigned long long)delta_ms * speed_percent + *remainder;
    *remainder = (unsigned int)(scaled % 100U);
    return (unsigned int)(scaled / 100U);
}

static void smile_3d_model_root_transition(const SmileModel3D* model, int clip_index,
    unsigned int previous, unsigned int current, unsigned int wraps, float output[4])
{
    float root_previous[4] = {};
    float root_current[4] = {};
    float root_start[4] = {};
    float root_end[4] = {};
    unsigned int components = 0;
    ZeroMemory(output, sizeof(float) * 4);
    if (!smile_3d_model_root_sample(model, clip_index, previous,
        root_previous, &components)) return;
    unsigned int ignored = 0;
    smile_3d_model_root_sample(model, clip_index, current, root_current, &ignored);
    if (wraps != 0)
    {
        const unsigned char* clip = smile_3d_model_animation_record(model, 3, clip_index);
        smile_3d_model_root_sample(model, clip_index, 0, root_start, &ignored);
        smile_3d_model_root_sample(model, clip_index, smile_3d_read_u32(clip + 4),
            root_end, &ignored);
    }
    for (unsigned int component = 0; component < 4; ++component)
        if ((components & (1U << component)) != 0)
            output[component] = wraps == 0
                ? smile_3d_root_delta(root_current[component], root_previous[component],
                    component == 3)
                : smile_3d_root_delta(root_end[component], root_previous[component], component == 3) +
                    (wraps - 1U) * smile_3d_root_delta(root_end[component],
                        root_start[component], component == 3) +
                    smile_3d_root_delta(root_current[component], root_start[component],
                        component == 3);
}

static void smile_3d_promote_model_destination(SmileAnimator3D* animator)
{
    animator->clip_index = animator->destination_clip;
    animator->playback_mode = animator->destination_mode;
    animator->time_ms = animator->destination_time_ms;
    animator->previous_time_ms = animator->time_ms;
    animator->time_remainder = animator->destination_time_remainder;
    animator->complete = animator->destination_complete;
    animator->destination_clip = -1;
    animator->destination_time_ms = 0;
    animator->destination_time_remainder = 0;
    animator->destination_complete = 0;
    animator->fade_elapsed_ms = 0;
    animator->fade_duration_ms = 0;
}

static void smile_3d_advance_model_current(SmileAnimator3D* animator,
    SmileModel3D* model, unsigned int delta_ms)
{
    unsigned int previous = animator->time_ms;
    unsigned int advance = smile_3d_scaled_model_advance(delta_ms,
        animator->speed_percent, &animator->time_remainder);
    unsigned int wraps = 0;
    animator->previous_time_ms = previous;
    animator->time_ms = smile_3d_advance_model_time(model, animator->clip_index,
        previous, advance, animator->playback_mode, &animator->complete, &wraps);
    smile_3d_queue_model_events(animator, model, animator->clip_index, previous,
        animator->time_ms, advance, animator->playback_mode);
    if (animator->root_motion_mode != 0)
    {
        float delta[4];
        smile_3d_model_root_transition(model, animator->clip_index, previous,
            animator->time_ms, wraps, delta);
        for (unsigned int component = 0; component < 4; ++component)
            animator->root_delta[component] += delta[component];
    }
}

static int smile_3d_update_model_animator(SmileAnimator3D* animator, unsigned int delta_ms)
{
    SmileModel3D* model = animator == 0 ? 0 : smile_3d_model_resource(animator->model_handle);
    if (animator == 0 || !animator->model_animation || model == 0 || delta_ms > 600000)
    { smile_last_error3d = 48; return 0; }
    if (animator->clip_index < 0) { smile_3d_update_model_pose(animator); return 1; }
    unsigned int remaining = delta_ms;
    if (animator->destination_clip >= 0)
    {
        unsigned int fade_remaining = animator->fade_duration_ms - animator->fade_elapsed_ms;
        unsigned int fade_delta = remaining < fade_remaining ? remaining : fade_remaining;
        unsigned int source_previous = animator->time_ms;
        unsigned int destination_previous = animator->destination_time_ms;
        unsigned int source_advance = smile_3d_scaled_model_advance(fade_delta,
            animator->speed_percent, &animator->time_remainder);
        unsigned int destination_advance = smile_3d_scaled_model_advance(fade_delta,
            animator->speed_percent, &animator->destination_time_remainder);
        unsigned int source_wraps = 0;
        unsigned int destination_wraps = 0;
        unsigned char source_complete = 0;
        animator->previous_time_ms = source_previous;
        animator->time_ms = smile_3d_advance_model_time(model, animator->clip_index,
            source_previous, source_advance, animator->playback_mode,
            &source_complete, &source_wraps);
        animator->destination_time_ms = smile_3d_advance_model_time(model,
            animator->destination_clip, destination_previous, destination_advance,
            animator->destination_mode, &animator->destination_complete, &destination_wraps);
        smile_3d_queue_model_events(animator, model, animator->destination_clip,
            destination_previous, animator->destination_time_ms, destination_advance,
            animator->destination_mode);
        if (animator->root_motion_mode != 0)
        {
            float source_delta[4];
            float destination_delta[4];
            smile_3d_model_root_transition(model, animator->clip_index, source_previous,
                animator->time_ms, source_wraps, source_delta);
            smile_3d_model_root_transition(model, animator->destination_clip,
                destination_previous, animator->destination_time_ms, destination_wraps,
                destination_delta);
            float start_weight = animator->fade_duration_ms == 0 ? 1.0f :
                (float)animator->fade_elapsed_ms / (float)animator->fade_duration_ms;
            float end_weight = animator->fade_duration_ms == 0 ? 1.0f :
                (float)(animator->fade_elapsed_ms + fade_delta) /
                    (float)animator->fade_duration_ms;
            float weight = (start_weight + end_weight) * 0.5f;
            for (unsigned int component = 0; component < 4; ++component)
                animator->root_delta[component] += smile_3d_lerp(source_delta[component],
                    destination_delta[component], weight);
        }
        animator->fade_elapsed_ms += fade_delta;
        animator->complete = animator->destination_complete;
        remaining -= fade_delta;
        if (animator->fade_elapsed_ms >= animator->fade_duration_ms)
            smile_3d_promote_model_destination(animator);
    }
    if (animator->destination_clip < 0 && remaining != 0)
        smile_3d_advance_model_current(animator, model, remaining);
    smile_3d_update_model_pose(animator);
    return 1;
}

static long long smile_3d_model_animation_value(SmileModel3D* model,
    long long property, long long index)
{
    if (model == 0) { smile_last_error3d = 5; return 0; }
    if (property == 1) return model->has_animation;
    if (property == 2) return model->animation_bone_count;
    if (property == 3) return model->animation_clip_count;
    if (property == 4) return model->animation_socket_count;
    if (property == 5) return model->animation_bytes;
    if (!model->has_animation) return 0;
    if (property == 6 || property == 7)
    {
        if (index < 0 || index >= model->animation_clip_count) { smile_last_error3d = 48; return 0; }
        const unsigned char* clip = smile_3d_model_animation_record(model, 3, (unsigned int)index);
        return property == 6 ? smile_3d_read_u32(clip + 4) : smile_3d_read_u32(clip + 8);
    }
    if (property == 8) return model->animation_event_count;
    if (property == 9) return model->animation_node_count;
    if (property == 10)
    {
        if (index <= 0 || index > model->animation_event_count) { smile_last_error3d = 48; return 0; }
        return (int)smile_3d_read_u32(smile_3d_model_animation_record(model, 6,
            (unsigned int)index - 1) + 12);
    }
    if (property == 11) return model->animation_file_bytes;
    if (property == 12) return model->animation_resident_bytes;
    if (property == 13) return sizeof(SmileAnimator3D);
    smile_last_error3d = 48;
    return 0;
}

static long long smile_3d_animator_production_value(SmileAnimator3D* animator,
    long long property)
{
    if (animator == 0 || !animator->model_animation)
    { smile_last_error3d = 48; return 0; }
    if (property == 1) return animator->destination_clip;
    if (property == 2) return animator->time_remainder;
    if (property == 3) return animator->destination_time_remainder;
    if (property == 4) return animator->destination_time_ms;
    if (property == 5) return animator->event_overflowed;
    if (property == 6) return animator->dropped_event_count;
    if (property == 7) return animator->playback_mode;
    if (property == 8) return animator->destination_mode;
    if (property == 9) return animator->pose_revision;
    if (property == 10) return sizeof(SmileAnimator3D);
    smile_last_error3d = 48;
    return 0;
}

static long long smile_3d_take_model_event(SmileAnimator3D* animator, const char* name)
{
    SmileModel3D* model = animator == 0 ? 0 : smile_3d_model_resource(animator->model_handle);
    if (animator == 0 || !animator->model_animation || model == 0) return 0;
    for (unsigned int ordinal = 0; ordinal < animator->event_count; ++ordinal)
    {
        unsigned int queue = (animator->event_head + ordinal) % SMILE_3D_MAX_PENDING_MODEL_EVENTS;
        unsigned int event_index = animator->pending_events[queue];
        const unsigned char* record = smile_3d_model_animation_record(model, 6, event_index);
        unsigned int name_offset = smile_3d_read_u32(record + 8);
        if (name != 0 && strcmp(model->strings + name_offset, name) != 0) continue;
        for (unsigned int move = ordinal; move + 1 < animator->event_count; ++move)
        {
            unsigned int destination = (animator->event_head + move) % SMILE_3D_MAX_PENDING_MODEL_EVENTS;
            unsigned int source = (animator->event_head + move + 1) % SMILE_3D_MAX_PENDING_MODEL_EVENTS;
            animator->pending_events[destination] = animator->pending_events[source];
        }
        animator->event_count--;
        return event_index + 1;
    }
    return 0;
}

static long long smile_3d_model_socket_value(SmileAnimator3D* animator,
    long long socket_index, long long property, long long object_handle)
{
    SmileModel3D* model = animator == 0 ? 0 : smile_3d_model_resource(animator->model_handle);
    if (animator == 0 || !animator->model_animation || model == 0 || socket_index < 0 ||
        socket_index >= model->animation_socket_count) { smile_last_error3d = 48; return 0; }
    const unsigned char* socket = smile_3d_model_animation_record(model, 7, (unsigned int)socket_index);
    unsigned int node = smile_3d_read_u32(socket + 4);
    SmileMatrix3D local = smile_3d_pose(smile_3d_read_float(socket + 16),
        smile_3d_read_float(socket + 20), smile_3d_read_float(socket + 24),
        smile_3d_read_float(socket + 28), smile_3d_read_float(socket + 32),
        smile_3d_read_float(socket + 36), smile_3d_read_float(socket + 40),
        smile_3d_read_float(socket + 44), smile_3d_read_float(socket + 48),
        smile_3d_read_float(socket + 52));
    SmileMatrix3D value = smile_3d_multiply(local, animator->node_global[node]);
    if (object_handle != 0)
    {
        SmileObject3D* object = smile_3d_object(object_handle);
        if (object == 0 || object->animator_handle !=
            smile_3d_handle(SMILE_3D_ANIMATOR_HANDLE,
                (int)(animator - smile_animators3d), animator->generation))
        { smile_last_error3d = 48; return 0; }
        value = smile_3d_multiply(value, smile_3d_model(object));
    }
    if (property >= 1 && property <= 3)
        return (long long)llroundf(value.m[11 + property] * 1000.0f);
    if (property >= 4 && property <= 12)
    {
        static const unsigned int fields[9] = { 0, 1, 2, 4, 5, 6, 8, 9, 10 };
        return (long long)llroundf(value.m[fields[property - 4]] * 1000.0f);
    }
    smile_last_error3d = 48;
    return 0;
}

static float smile_3d_linear_determinant(const SmileMatrix3D& model)
{
    float a = model.m[0], b = model.m[1], c = model.m[2];
    float d = model.m[4], e = model.m[5], f = model.m[6];
    float g = model.m[8], h = model.m[9], i = model.m[10];
    return a * (e * i - f * h) - b * (d * i - f * g) + c * (d * h - e * g);
}

static SmileMatrix3D smile_3d_look_at(const float eye[3], const float target[3])
{
    float zx = target[0] - eye[0], zy = target[1] - eye[1], zz = target[2] - eye[2];
    float up_x = fabsf(zy) > 0.99f ? 1.0f : 0.0f;
    float up_y = fabsf(zy) > 0.99f ? 0.0f : 1.0f;
    float xx, xy, xz, yx, yy, yz;
    SmileMatrix3D result = smile_3d_identity();
    smile_3d_normalize(&zx, &zy, &zz);
    smile_3d_cross(up_x, up_y, 0.0f, zx, zy, zz, &xx, &xy, &xz);
    smile_3d_normalize(&xx, &xy, &xz);
    smile_3d_cross(zx, zy, zz, xx, xy, xz, &yx, &yy, &yz);
    result.m[0] = xx; result.m[1] = yx; result.m[2] = zx;
    result.m[4] = xy; result.m[5] = yy; result.m[6] = zy;
    result.m[8] = xz; result.m[9] = yz; result.m[10] = zz;
    result.m[12] = -(xx * eye[0] + xy * eye[1] + xz * eye[2]);
    result.m[13] = -(yx * eye[0] + yy * eye[1] + yz * eye[2]);
    result.m[14] = -(zx * eye[0] + zy * eye[1] + zz * eye[2]);
    return result;
}

static SmileMatrix3D smile_3d_orthographic(float width, float height, float near_depth,
    float far_depth)
{
    SmileMatrix3D result = {};
    result.m[0] = 2.0f / width;
    result.m[5] = 2.0f / height;
    result.m[10] = 1.0f / (far_depth - near_depth);
    result.m[14] = -near_depth / (far_depth - near_depth);
    result.m[15] = 1.0f;
    return result;
}

static SmileMatrix3D smile_3d_perspective(float fov_degrees, float aspect,
    float near_depth, float far_depth)
{
    SmileMatrix3D result = {};
    float y_scale = 1.0f / tanf(fov_degrees * SMILE_3D_PI / 360.0f);
    result.m[0] = y_scale / aspect;
    result.m[5] = y_scale;
    result.m[10] = far_depth / (far_depth - near_depth);
    result.m[11] = 1.0f;
    result.m[14] = -near_depth * far_depth / (far_depth - near_depth);
    return result;
}

static int smile_3d_update_shadow_matrix(void)
{
    float eye[3];
    float target[3];
    SmileMatrix3D view;
    SmileMatrix3D projection;
    if (smile_shadow_caster3d == 1)
    {
        if (!smile_directional_light3d.enabled) return 0;
        for (int component = 0; component < 3; ++component)
        {
            target[component] = smile_shadow_center3d[component];
            eye[component] = target[component] +
                smile_directional_light3d.direction[component] * smile_shadow_far3d * 0.5f;
        }
        view = smile_3d_look_at(eye, target);
        if (smile_shadow_resolution3d > 0)
        {
            float light_x = target[0] * view.m[0] + target[1] * view.m[4] +
                target[2] * view.m[8] + view.m[12];
            float light_y = target[0] * view.m[1] + target[1] * view.m[5] +
                target[2] * view.m[9] + view.m[13];
            float texel_x = smile_shadow_width3d / (float)smile_shadow_resolution3d;
            float texel_y = smile_shadow_height3d / (float)smile_shadow_resolution3d;
            float snapped_x = roundf(light_x / texel_x) * texel_x;
            float snapped_y = roundf(light_y / texel_y) * texel_y;
            view.m[12] += snapped_x - light_x;
            view.m[13] += snapped_y - light_y;
        }
        projection = smile_3d_orthographic(smile_shadow_width3d, smile_shadow_height3d,
            smile_shadow_near3d, smile_shadow_far3d);
    }
    else if (smile_shadow_caster3d == 2)
    {
        SmileLocalLight3D* light;
        float outer;
        if (smile_shadow_slot3d < 0 || smile_shadow_slot3d >= SMILE_3D_MAX_LOCAL_LIGHTS)
            return 0;
        light = &smile_local_lights3d[smile_shadow_slot3d];
        if (light->type != 2 || light->range <= 0.0f) return 0;
        for (int component = 0; component < 3; ++component)
        {
            eye[component] = light->position[component];
            target[component] = eye[component] + light->direction[component];
        }
        outer = acosf(light->outer_cosine) * 360.0f / SMILE_3D_PI;
        view = smile_3d_look_at(eye, target);
        projection = smile_3d_perspective(outer, 1.0f, 1.0f, light->range);
    }
    else return 0;
    smile_shadow_view_projection3d = smile_3d_multiply(view, projection);
    return 1;
}

static void smile_3d_release_shadow_target(void)
{
    smile_3d_release(smile_shadow_raster_state3d);
    smile_3d_release(smile_shadow_double_raster_state3d);
    smile_3d_release(smile_shadow_shader_view3d);
    smile_3d_release(smile_shadow_depth_view3d);
    smile_3d_release(smile_shadow_texture3d);
    smile_shadow_effective3d = 0;
    smile_shadow_resolution3d = 0;
    smile_shadow_bytes3d = 0;
}

static int smile_3d_create_shadow_target(void)
{
    ID3D11Device* device = (ID3D11Device*)smile_graphics_directx_device();
    D3D11_TEXTURE2D_DESC texture = {};
    D3D11_DEPTH_STENCIL_VIEW_DESC depth = {};
    D3D11_SHADER_RESOURCE_VIEW_DESC view = {};
    D3D11_RASTERIZER_DESC raster = {};
    static const int choices[2] = { 2048, 1024 };
    WCHAR forced[2];
    HRESULT result = E_FAIL;
    ID3D11Texture2D* previous_texture;
    ID3D11DepthStencilView* previous_depth;
    ID3D11ShaderResourceView* previous_view;
    ID3D11RasterizerState* previous_raster;
    ID3D11RasterizerState* previous_double_raster;
    int previous_resolution;
    int previous_effective;
    long long previous_bytes;
    if (!smile_shadow_requested3d) { smile_3d_release_shadow_target(); return 1; }
    if (device == 0 || smile_shadow_vertex_shader3d == 0 ||
        smile_shadow_pixel_shader3d == 0 ||
        GetEnvironmentVariableW(L"SMILE_TEST_RENDERER3D_FORCE_SHADOW_FAILURE", forced, 2) != 0)
    {
        if (smile_shadow_texture3d == 0)
            smile_m5_fallback_flags3d |= SMILE_3D_M5_FALLBACK_SHADOW_DISABLED;
        return 1;
    }
    previous_texture = smile_shadow_texture3d;
    previous_depth = smile_shadow_depth_view3d;
    previous_view = smile_shadow_shader_view3d;
    previous_raster = smile_shadow_raster_state3d;
    previous_double_raster = smile_shadow_double_raster_state3d;
    previous_resolution = smile_shadow_resolution3d;
    previous_effective = smile_shadow_effective3d;
    previous_bytes = smile_shadow_bytes3d;
    smile_shadow_texture3d = 0; smile_shadow_depth_view3d = 0;
    smile_shadow_shader_view3d = 0; smile_shadow_raster_state3d = 0;
    smile_shadow_double_raster_state3d = 0; smile_shadow_resolution3d = 0;
    smile_shadow_effective3d = 0; smile_shadow_bytes3d = 0;
    for (int option = 0; option < 2; ++option)
    {
        int resolution = choices[option];
        if (resolution > smile_shadow_requested_resolution3d) continue;
        texture.Width = texture.Height = (UINT)resolution;
        texture.MipLevels = texture.ArraySize = 1;
        texture.Format = DXGI_FORMAT_R32_TYPELESS;
        texture.SampleDesc.Count = 1;
        texture.Usage = D3D11_USAGE_DEFAULT;
        texture.BindFlags = D3D11_BIND_DEPTH_STENCIL | D3D11_BIND_SHADER_RESOURCE;
        result = device->CreateTexture2D(&texture, 0, &smile_shadow_texture3d);
        depth.Format = DXGI_FORMAT_D32_FLOAT;
        depth.ViewDimension = D3D11_DSV_DIMENSION_TEXTURE2D;
        if (SUCCEEDED(result)) result = device->CreateDepthStencilView(
            smile_shadow_texture3d, &depth, &smile_shadow_depth_view3d);
        view.Format = DXGI_FORMAT_R32_FLOAT;
        view.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
        view.Texture2D.MipLevels = 1;
        if (SUCCEEDED(result)) result = device->CreateShaderResourceView(
            smile_shadow_texture3d, &view, &smile_shadow_shader_view3d);
        raster.FillMode = D3D11_FILL_SOLID;
        raster.CullMode = D3D11_CULL_BACK;
        raster.DepthClipEnable = TRUE;
        raster.DepthBias = (INT)(smile_shadow_bias3d * 16777216.0f);
        raster.SlopeScaledDepthBias = 1.0f;
        raster.DepthBiasClamp = 0.02f;
        if (SUCCEEDED(result))
            result = device->CreateRasterizerState(&raster, &smile_shadow_raster_state3d);
        raster.CullMode = D3D11_CULL_NONE;
        if (SUCCEEDED(result))
            result = device->CreateRasterizerState(&raster, &smile_shadow_double_raster_state3d);
        if (SUCCEEDED(result))
        {
            smile_shadow_resolution3d = resolution;
            smile_shadow_effective3d = 1;
            smile_shadow_bytes3d = (long long)resolution * resolution * 4;
            if (resolution < smile_shadow_requested_resolution3d)
                smile_m5_fallback_flags3d |=
                    SMILE_3D_M5_FALLBACK_SHADOW_RESOLUTION_REDUCED;
            smile_3d_release(previous_double_raster);
            smile_3d_release(previous_raster);
            smile_3d_release(previous_view);
            smile_3d_release(previous_depth);
            smile_3d_release(previous_texture);
            return 1;
        }
        smile_3d_release_shadow_target();
    }
    smile_shadow_texture3d = previous_texture;
    smile_shadow_depth_view3d = previous_depth;
    smile_shadow_shader_view3d = previous_view;
    smile_shadow_raster_state3d = previous_raster;
    smile_shadow_double_raster_state3d = previous_double_raster;
    smile_shadow_resolution3d = previous_resolution;
    smile_shadow_effective3d = previous_effective;
    smile_shadow_bytes3d = previous_bytes;
    if (previous_texture == 0)
        smile_m5_fallback_flags3d |= SMILE_3D_M5_FALLBACK_SHADOW_DISABLED;
    return 1;
}

static int smile_3d_create_bloom_target(ID3D11Device* device, int width, int height,
    ID3D11Texture2D** texture, ID3D11RenderTargetView** target,
    ID3D11ShaderResourceView** view)
{
    D3D11_TEXTURE2D_DESC description = {};
    HRESULT result;
    description.Width = (UINT)width;
    description.Height = (UINT)height;
    description.MipLevels = description.ArraySize = 1;
    description.Format = DXGI_FORMAT_R16G16B16A16_FLOAT;
    description.SampleDesc.Count = 1;
    description.Usage = D3D11_USAGE_DEFAULT;
    description.BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
    result = device->CreateTexture2D(&description, 0, texture);
    if (SUCCEEDED(result)) result = device->CreateRenderTargetView(*texture, 0, target);
    if (SUCCEEDED(result)) result = device->CreateShaderResourceView(*texture, 0, view);
    return SUCCEEDED(result);
}

static SmileM5TargetState3D smile_3d_take_target_state(void)
{
    SmileM5TargetState3D state = {};
    state.color_texture = smile_color_texture3d;
    state.color_view = smile_color_view3d;
    state.depth_texture = smile_depth_texture3d;
    state.depth_view = smile_depth_view3d;
    state.resolve_texture = smile_scene_resolve_texture3d;
    state.scene_view = smile_scene_shader_view3d;
    state.bloom_texture_a = smile_bloom_texture_a3d;
    state.bloom_view_a = smile_bloom_view_a3d;
    state.bloom_shader_a = smile_bloom_shader_a3d;
    state.bloom_texture_b = smile_bloom_texture_b3d;
    state.bloom_view_b = smile_bloom_view_b3d;
    state.bloom_shader_b = smile_bloom_shader_b3d;
    state.target_width = smile_target_width3d;
    state.target_height = smile_target_height3d;
    state.m5_width = smile_m5_target_width3d;
    state.m5_height = smile_m5_target_height3d;
    state.bloom_width = smile_bloom_width3d;
    state.bloom_height = smile_bloom_height3d;
    state.sample_count = smile_sample_count3d;
    state.sample_quality = smile_sample_quality3d;
    state.post_effective = smile_post_effective3d;
    state.hdr_effective = smile_hdr_effective3d;
    state.bloom_effective = smile_bloom_effective3d;
    state.tone_effective = smile_tone_mapping_effective3d;
    state.target_bytes = smile_m5_target_bytes3d;
    state.scene_bytes = smile_scene_bytes3d;
    state.bloom_bytes = smile_bloom_bytes3d;
    return state;
}

static void smile_3d_clear_target_state(void)
{
    smile_color_texture3d = 0; smile_color_view3d = 0;
    smile_depth_texture3d = 0; smile_depth_view3d = 0;
    smile_scene_resolve_texture3d = 0; smile_scene_shader_view3d = 0;
    smile_bloom_texture_a3d = 0; smile_bloom_view_a3d = 0; smile_bloom_shader_a3d = 0;
    smile_bloom_texture_b3d = 0; smile_bloom_view_b3d = 0; smile_bloom_shader_b3d = 0;
    smile_target_width3d = smile_target_height3d = 0;
    smile_m5_target_width3d = smile_m5_target_height3d = 0;
    smile_bloom_width3d = smile_bloom_height3d = 0;
    smile_sample_count3d = 1; smile_sample_quality3d = 0;
    smile_post_effective3d = smile_hdr_effective3d = smile_bloom_effective3d = 0;
    smile_tone_mapping_effective3d = 0;
    smile_m5_target_bytes3d = smile_scene_bytes3d = smile_bloom_bytes3d = 0;
}

static void smile_3d_restore_target_state(const SmileM5TargetState3D& state)
{
    smile_color_texture3d = state.color_texture; smile_color_view3d = state.color_view;
    smile_depth_texture3d = state.depth_texture; smile_depth_view3d = state.depth_view;
    smile_scene_resolve_texture3d = state.resolve_texture; smile_scene_shader_view3d = state.scene_view;
    smile_bloom_texture_a3d = state.bloom_texture_a; smile_bloom_view_a3d = state.bloom_view_a;
    smile_bloom_shader_a3d = state.bloom_shader_a; smile_bloom_texture_b3d = state.bloom_texture_b;
    smile_bloom_view_b3d = state.bloom_view_b; smile_bloom_shader_b3d = state.bloom_shader_b;
    smile_target_width3d = state.target_width; smile_target_height3d = state.target_height;
    smile_m5_target_width3d = state.m5_width; smile_m5_target_height3d = state.m5_height;
    smile_bloom_width3d = state.bloom_width; smile_bloom_height3d = state.bloom_height;
    smile_sample_count3d = state.sample_count; smile_sample_quality3d = state.sample_quality;
    smile_post_effective3d = state.post_effective; smile_hdr_effective3d = state.hdr_effective;
    smile_bloom_effective3d = state.bloom_effective; smile_tone_mapping_effective3d = state.tone_effective;
    smile_m5_target_bytes3d = state.target_bytes; smile_scene_bytes3d = state.scene_bytes;
    smile_bloom_bytes3d = state.bloom_bytes;
}

static void smile_3d_release_target_state(SmileM5TargetState3D& state)
{
    smile_3d_release(state.scene_view); smile_3d_release(state.resolve_texture);
    smile_3d_release(state.bloom_shader_a); smile_3d_release(state.bloom_view_a);
    smile_3d_release(state.bloom_texture_a); smile_3d_release(state.bloom_shader_b);
    smile_3d_release(state.bloom_view_b); smile_3d_release(state.bloom_texture_b);
    smile_3d_release(state.color_view); smile_3d_release(state.color_texture);
    smile_3d_release(state.depth_view); smile_3d_release(state.depth_texture);
}

static int smile_3d_create_targets(void)
{
    ID3D11Device* device = (ID3D11Device*)smile_graphics_directx_device();
    int width = smile_graphics_directx_physical_width(), height = smile_graphics_directx_physical_height();
    static const UINT preferred_samples[] = { 4, 2, 1 };
    D3D11_TEXTURE2D_DESC color = {};
    D3D11_TEXTURE2D_DESC description = {};
    HRESULT result = E_FAIL;
    UINT candidate;
    WCHAR forced_hdr[2];
    SmileM5TargetState3D previous;
    if (device == 0 || width <= 0 || height <= 0) return 0;
    if (smile_depth_view3d != 0 && width == smile_target_width3d &&
        height == smile_target_height3d &&
        smile_m5_applied_revision3d == smile_m5_configuration_revision3d) return 1;
    previous = smile_3d_take_target_state();
    smile_3d_clear_target_state();
    smile_m5_fallback_flags3d &=
        SMILE_3D_M5_FALLBACK_SHADOW_RESOLUTION_REDUCED |
        SMILE_3D_M5_FALLBACK_SHADOW_DISABLED;

    if (smile_post_requested3d && smile_hdr_requested3d &&
        smile_post_vertex_shader3d != 0 && smile_post_pixel_shader3d != 0 &&
        GetEnvironmentVariableW(L"SMILE_TEST_RENDERER3D_FORCE_HDR_FAILURE", forced_hdr, 2) == 0)
    {
        UINT support = 0;
        if (SUCCEEDED(device->CheckFormatSupport(DXGI_FORMAT_R16G16B16A16_FLOAT, &support)) &&
            (support & D3D11_FORMAT_SUPPORT_RENDER_TARGET) != 0 &&
            (support & D3D11_FORMAT_SUPPORT_SHADER_SAMPLE) != 0)
        {
            for (candidate = 0;
                candidate < (UINT)(sizeof(preferred_samples) / sizeof(preferred_samples[0]));
                ++candidate)
            {
                UINT samples = preferred_samples[candidate];
                UINT color_levels = 1;
                UINT depth_levels = 1;
                UINT quality = 0;
                if ((int)samples > smile_requested_sample_count3d) continue;
                if (samples > 1)
                {
                    color_levels = depth_levels = 0;
                    if (FAILED(device->CheckMultisampleQualityLevels(
                            DXGI_FORMAT_R16G16B16A16_FLOAT, samples, &color_levels)) ||
                        FAILED(device->CheckMultisampleQualityLevels(
                            DXGI_FORMAT_D24_UNORM_S8_UINT, samples, &depth_levels)) ||
                        color_levels == 0 || depth_levels == 0) continue;
                    quality = (color_levels < depth_levels ? color_levels : depth_levels) - 1;
                }
                color.Width = (UINT)width;
                color.Height = (UINT)height;
                color.MipLevels = color.ArraySize = 1;
                color.Format = DXGI_FORMAT_R16G16B16A16_FLOAT;
                color.SampleDesc.Count = samples;
                color.SampleDesc.Quality = quality;
                color.Usage = D3D11_USAGE_DEFAULT;
                color.BindFlags = D3D11_BIND_RENDER_TARGET |
                    (samples == 1 ? D3D11_BIND_SHADER_RESOURCE : 0);
                result = device->CreateTexture2D(&color, 0, &smile_color_texture3d);
                if (SUCCEEDED(result)) result = device->CreateRenderTargetView(
                    smile_color_texture3d, 0, &smile_color_view3d);
                description.Width = (UINT)width;
                description.Height = (UINT)height;
                description.MipLevels = description.ArraySize = 1;
                description.Format = DXGI_FORMAT_D24_UNORM_S8_UINT;
                description.SampleDesc.Count = samples;
                description.SampleDesc.Quality = quality;
                description.Usage = D3D11_USAGE_DEFAULT;
                description.BindFlags = D3D11_BIND_DEPTH_STENCIL;
                if (SUCCEEDED(result)) result = device->CreateTexture2D(
                    &description, 0, &smile_depth_texture3d);
                if (SUCCEEDED(result)) result = device->CreateDepthStencilView(
                    smile_depth_texture3d, 0, &smile_depth_view3d);
                if (SUCCEEDED(result) && samples == 1)
                    result = device->CreateShaderResourceView(
                        smile_color_texture3d, 0, &smile_scene_shader_view3d);
                if (SUCCEEDED(result) && samples > 1)
                {
                    color.SampleDesc.Count = 1;
                    color.SampleDesc.Quality = 0;
                    color.BindFlags = D3D11_BIND_SHADER_RESOURCE;
                    result = device->CreateTexture2D(
                        &color, 0, &smile_scene_resolve_texture3d);
                    if (SUCCEEDED(result)) result = device->CreateShaderResourceView(
                        smile_scene_resolve_texture3d, 0, &smile_scene_shader_view3d);
                }
                if (SUCCEEDED(result))
                {
                    smile_sample_count3d = samples;
                    smile_sample_quality3d = quality;
                    smile_hdr_effective3d = smile_post_effective3d = 1;
                    smile_tone_mapping_effective3d = 1;
                    if ((int)samples < smile_requested_sample_count3d)
                        smile_m5_fallback_flags3d |= SMILE_3D_M5_FALLBACK_MSAA_REDUCED;
                    break;
                }
                smile_3d_release(smile_scene_shader_view3d);
                smile_3d_release(smile_scene_resolve_texture3d);
                smile_3d_release(smile_color_view3d);
                smile_3d_release(smile_color_texture3d);
                smile_3d_release(smile_depth_view3d);
                smile_3d_release(smile_depth_texture3d);
            }
        }
        if (smile_hdr_effective3d && smile_bloom_requested3d)
        {
            smile_bloom_width3d = width / smile_bloom_downsample3d;
            smile_bloom_height3d = height / smile_bloom_downsample3d;
            if (smile_bloom_width3d < 1) smile_bloom_width3d = 1;
            if (smile_bloom_height3d < 1) smile_bloom_height3d = 1;
            if (smile_3d_create_bloom_target(device, smile_bloom_width3d,
                    smile_bloom_height3d, &smile_bloom_texture_a3d,
                    &smile_bloom_view_a3d, &smile_bloom_shader_a3d) &&
                smile_3d_create_bloom_target(device, smile_bloom_width3d,
                    smile_bloom_height3d, &smile_bloom_texture_b3d,
                    &smile_bloom_view_b3d, &smile_bloom_shader_b3d))
                smile_bloom_effective3d = 1;
            else
            {
                smile_3d_release(smile_bloom_shader_a3d);
                smile_3d_release(smile_bloom_view_a3d);
                smile_3d_release(smile_bloom_texture_a3d);
                smile_3d_release(smile_bloom_shader_b3d);
                smile_3d_release(smile_bloom_view_b3d);
                smile_3d_release(smile_bloom_texture_b3d);
                smile_bloom_width3d = smile_bloom_height3d = 0;
                smile_m5_fallback_flags3d |= SMILE_3D_M5_FALLBACK_BLOOM_DISABLED;
            }
        }
    }

    if (!smile_hdr_effective3d)
    {
        if (smile_post_requested3d)
            smile_m5_fallback_flags3d |= SMILE_3D_M5_FALLBACK_HDR_UNAVAILABLE |
                SMILE_3D_M5_FALLBACK_TONE_MAPPING_DISABLED |
                SMILE_3D_M5_FALLBACK_DIRECT_LDR;
        if (smile_bloom_requested3d)
            smile_m5_fallback_flags3d |= SMILE_3D_M5_FALLBACK_BLOOM_DISABLED;

        for (candidate = 0;
            candidate < (UINT)(sizeof(preferred_samples) / sizeof(preferred_samples[0]));
            ++candidate)
        {
            UINT samples = preferred_samples[candidate];
            UINT color_levels = 1;
            UINT depth_levels = 1;
            UINT quality = 0;
            if (samples > 1)
            {
                color_levels = depth_levels = 0;
                if (FAILED(device->CheckMultisampleQualityLevels(
                        DXGI_FORMAT_B8G8R8A8_UNORM, samples, &color_levels)) ||
                    FAILED(device->CheckMultisampleQualityLevels(
                        DXGI_FORMAT_D24_UNORM_S8_UINT, samples, &depth_levels)) ||
                    color_levels == 0 || depth_levels == 0) continue;
                quality = (color_levels < depth_levels ? color_levels : depth_levels) - 1;
                color.Width = (UINT)width;
                color.Height = (UINT)height;
                color.MipLevels = color.ArraySize = 1;
                color.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
                color.SampleDesc.Count = samples;
                color.SampleDesc.Quality = quality;
                color.Usage = D3D11_USAGE_DEFAULT;
                color.BindFlags = D3D11_BIND_RENDER_TARGET;
                result = device->CreateTexture2D(&color, 0, &smile_color_texture3d);
                if (SUCCEEDED(result)) result = device->CreateRenderTargetView(
                    smile_color_texture3d, 0, &smile_color_view3d);
                if (FAILED(result))
                {
                    smile_3d_release(smile_color_view3d);
                    smile_3d_release(smile_color_texture3d);
                    continue;
                }
            }
            description.Width = (UINT)width;
            description.Height = (UINT)height;
            description.MipLevels = description.ArraySize = 1;
            description.Format = DXGI_FORMAT_D24_UNORM_S8_UINT;
            description.SampleDesc.Count = samples;
            description.SampleDesc.Quality = quality;
            description.Usage = D3D11_USAGE_DEFAULT;
            description.BindFlags = D3D11_BIND_DEPTH_STENCIL;
            result = device->CreateTexture2D(&description, 0, &smile_depth_texture3d);
            if (SUCCEEDED(result)) result = device->CreateDepthStencilView(
                smile_depth_texture3d, 0, &smile_depth_view3d);
            if (SUCCEEDED(result))
            {
                smile_sample_count3d = samples;
                smile_sample_quality3d = quality;
                break;
            }
            smile_3d_release(smile_color_view3d);
            smile_3d_release(smile_color_texture3d);
            smile_3d_release(smile_depth_view3d);
            smile_3d_release(smile_depth_texture3d);
        }
    }
    if (FAILED(result))
    {
        SmileM5TargetState3D failed = smile_3d_take_target_state();
        smile_3d_clear_target_state();
        smile_3d_release_target_state(failed);
        if (previous.depth_view != 0 && previous.target_width == width &&
            previous.target_height == height)
        {
            smile_3d_restore_target_state(previous);
            smile_m5_applied_revision3d = smile_m5_configuration_revision3d;
            return 1;
        }
        smile_3d_release_target_state(previous);
        smile_last_error3d = 11;
        return 0;
    }
    smile_target_width3d = smile_m5_target_width3d = width;
    smile_target_height3d = smile_m5_target_height3d = height;
    smile_scene_bytes3d = (long long)width * height *
        (smile_hdr_effective3d ? 8 : 4) *
        (smile_sample_count3d + (smile_hdr_effective3d && smile_sample_count3d > 1 ? 1 : 0));
    smile_scene_bytes3d += (long long)width * height * 4 * smile_sample_count3d;
    smile_bloom_bytes3d = smile_bloom_effective3d
        ? (long long)smile_bloom_width3d * smile_bloom_height3d * 16
        : 0;
    smile_m5_target_bytes3d = smile_shadow_bytes3d + smile_scene_bytes3d + smile_bloom_bytes3d;
    smile_m5_applied_revision3d = smile_m5_configuration_revision3d;
    smile_m5_resource_generation3d++;
    if (smile_m5_resource_generation3d <= 0 || smile_m5_resource_generation3d > 2147483647)
        smile_m5_resource_generation3d = 1;
    smile_3d_release_target_state(previous);
    return 1;
}

static int smile_3d_prepare_m5_resources(void)
{
    smile_m5_fallback_flags3d = 0;
    if (smile_shadow_applied_revision3d != smile_m5_configuration_revision3d)
    {
        if (!smile_3d_create_shadow_target()) return 0;
        smile_shadow_applied_revision3d = smile_m5_configuration_revision3d;
    }
    smile_shadow_effective3d = smile_shadow_requested3d &&
        smile_shadow_depth_view3d != 0 && smile_shadow_shader_view3d != 0 &&
        smile_3d_update_shadow_matrix();
    if (smile_shadow_requested3d && !smile_shadow_effective3d)
        smile_m5_fallback_flags3d |= SMILE_3D_M5_FALLBACK_SHADOW_DISABLED;
    else if (smile_shadow_effective3d &&
        smile_shadow_resolution3d < smile_shadow_requested_resolution3d)
        smile_m5_fallback_flags3d |= SMILE_3D_M5_FALLBACK_SHADOW_RESOLUTION_REDUCED;
    if (!smile_3d_create_targets()) return 0;
    if (smile_post_requested3d && smile_hdr_requested3d && !smile_hdr_effective3d)
        smile_m5_fallback_flags3d |= SMILE_3D_M5_FALLBACK_HDR_UNAVAILABLE |
            SMILE_3D_M5_FALLBACK_TONE_MAPPING_DISABLED |
            SMILE_3D_M5_FALLBACK_DIRECT_LDR;
    if (smile_bloom_requested3d && !smile_bloom_effective3d)
        smile_m5_fallback_flags3d |= SMILE_3D_M5_FALLBACK_BLOOM_DISABLED;
    if (smile_requested_sample_count3d > (int)smile_sample_count3d)
        smile_m5_fallback_flags3d |= SMILE_3D_M5_FALLBACK_MSAA_REDUCED;
    smile_multipass_active3d = smile_shadow_effective3d || smile_hdr_effective3d;
    smile_m5_target_bytes3d = smile_shadow_bytes3d + smile_scene_bytes3d + smile_bloom_bytes3d;
    return 1;
}

static int smile_3d_upload(SmileMesh3D* mesh)
{
    ID3D11Device* device = (ID3D11Device*)smile_graphics_directx_device();
    D3D11_BUFFER_DESC description = {};
    D3D11_SUBRESOURCE_DATA data = {};
    HRESULT result;
    if (mesh->vertex_buffer != 0 && mesh->index_buffer != 0) return 1;
    if (device == 0 || !mesh->committed) return 0;
    description.ByteWidth = (UINT)(sizeof(SmileVertex3D) * mesh->vertex_count); description.Usage = D3D11_USAGE_IMMUTABLE; description.BindFlags = D3D11_BIND_VERTEX_BUFFER;
    data.pSysMem = mesh->vertices; result = device->CreateBuffer(&description, &data, &mesh->vertex_buffer);
    description.ByteWidth = (UINT)(sizeof(unsigned int) * mesh->index_count); description.BindFlags = D3D11_BIND_INDEX_BUFFER;
    data.pSysMem = mesh->indices; if (SUCCEEDED(result)) result = device->CreateBuffer(&description, &data, &mesh->index_buffer);
    if (FAILED(result)) { smile_last_error3d = 12; smile_3d_release(mesh->vertex_buffer); smile_3d_release(mesh->index_buffer); return 0; }
    return 1;
}

static int smile_3d_upload_texture(SmileTexture3D* texture)
{
    ID3D11Device* device = (ID3D11Device*)smile_graphics_directx_device();
    ID3D11DeviceContext* context = (ID3D11DeviceContext*)smile_graphics_directx_context();
    D3D11_TEXTURE2D_DESC description = {};
    D3D11_SUBRESOURCE_DATA data = {};
    D3D11_SHADER_RESOURCE_VIEW_DESC view = {};
    D3D11_SAMPLER_DESC sampler = {};
    HRESULT result;
    if (texture->view != 0 && texture->sampler != 0) return 1;
    if (device == 0 || context == 0 || texture->image == 0) return 0;
    description.Width = (UINT)smile_image_resource_width(texture->image);
    description.Height = (UINT)smile_image_resource_height(texture->image);
    description.MipLevels = texture->mip_levels;
    description.ArraySize = 1;
    description.Format = texture->pbr ? DXGI_FORMAT_B8G8R8A8_TYPELESS : DXGI_FORMAT_B8G8R8A8_UNORM;
    description.SampleDesc.Count = 1;
    description.Usage = texture->pbr && texture->mip_levels > 1 ? D3D11_USAGE_DEFAULT : D3D11_USAGE_IMMUTABLE;
    description.BindFlags = D3D11_BIND_SHADER_RESOURCE;
    if (texture->pbr && texture->mip_levels > 1)
    {
        description.BindFlags |= D3D11_BIND_RENDER_TARGET;
        description.MiscFlags = D3D11_RESOURCE_MISC_GENERATE_MIPS;
    }
    data.pSysMem = texture->pbr
        ? smile_image_resource_straight_pixels(texture->image)
        : smile_image_resource_acquire_premultiplied_pixels(texture->image);
    data.SysMemPitch = smile_image_resource_stride(texture->image);
    if (data.pSysMem == 0) { smile_last_error3d = 21; return 0; }
    result = device->CreateTexture2D(&description,
        texture->pbr && texture->mip_levels > 1 ? 0 : &data, &texture->texture);
    if (!texture->pbr) smile_image_resource_release_premultiplied_pixels(texture->image);
    if (SUCCEEDED(result) && texture->pbr)
    {
        view.Format = texture->semantic == 1
            ? DXGI_FORMAT_B8G8R8A8_UNORM_SRGB : DXGI_FORMAT_B8G8R8A8_UNORM;
        view.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
        view.Texture2D.MostDetailedMip = 0;
        view.Texture2D.MipLevels = texture->mip_levels;
        result = device->CreateShaderResourceView(texture->texture, &view, &texture->view);
        if (SUCCEEDED(result) && texture->mip_levels > 1)
        {
            context->UpdateSubresource(texture->texture, 0, 0, data.pSysMem, data.SysMemPitch, 0);
            context->GenerateMips(texture->view);
        }
    }
    else if (SUCCEEDED(result)) result = device->CreateShaderResourceView(texture->texture, 0, &texture->view);
    if (texture->filter == 0) sampler.Filter = D3D11_FILTER_MIN_MAG_MIP_POINT;
    else if (texture->filter == 1) sampler.Filter = D3D11_FILTER_MIN_MAG_LINEAR_MIP_POINT;
    else if (texture->filter == 2) sampler.Filter = D3D11_FILTER_MIN_MAG_MIP_LINEAR;
    else sampler.Filter = D3D11_FILTER_ANISOTROPIC;
    sampler.AddressU = texture->wrap == 0 ? D3D11_TEXTURE_ADDRESS_CLAMP : D3D11_TEXTURE_ADDRESS_WRAP;
    sampler.AddressV = sampler.AddressU;
    sampler.AddressW = sampler.AddressU;
    sampler.MaxAnisotropy = texture->effective_anisotropy;
    sampler.MaxLOD = D3D11_FLOAT32_MAX;
    if (SUCCEEDED(result)) result = device->CreateSamplerState(&sampler, &texture->sampler);
    if (FAILED(result))
    {
        smile_last_error3d = 21;
        smile_3d_release(texture->view);
        smile_3d_release(texture->texture);
        smile_3d_release(texture->sampler);
        return 0;
    }
    return 1;
}

static int smile_3d_model_owns_mesh(const SmileModel3D* model, const SmileMesh3D* mesh)
{
    if (model == 0 || mesh == 0) return 0;
    for (unsigned int part = 0; part < model->part_count; ++part)
        if (smile_3d_mesh(model->mesh_handles[part]) == mesh) return 1;
    return 0;
}

static int smile_3d_palette_snapshot(long long animator_handle, SmileAnimator3D* animator)
{
    unsigned char mode;
    unsigned int bone_count;
    if (animator == 0) return -1;
    mode = animator->model_animation ? 2 : 1;
    bone_count = animator->model_animation ? SMILE_3D_MAX_MODEL_ANIMATION_BONES : SMILE_3D_MAX_BONES;
    for (unsigned int index = 0; index < smile_frame_palette_count3d; ++index)
        if (smile_frame_palettes3d[index].animator_handle == animator_handle &&
            smile_frame_palettes3d[index].pose_revision == animator->pose_revision &&
            smile_frame_palettes3d[index].mode == mode)
            return (int)index;
    if (smile_frame_palette_count3d >= SMILE_3D_MAX_FRAME_PALETTES)
    {
        smile_last_error3d = 51;
        return -2;
    }
    SmilePaletteSnapshot3D* snapshot = &smile_frame_palettes3d[smile_frame_palette_count3d];
    snapshot->animator_handle = animator_handle;
    snapshot->pose_revision = animator->pose_revision;
    snapshot->mode = mode;
    snapshot->bone_count = (unsigned char)bone_count;
    memcpy(snapshot->bones, animator->bones, sizeof(SmileMatrix3D) * bone_count);
    return (int)smile_frame_palette_count3d++;
}

static int smile_3d_upload_palette_snapshot(ID3D11DeviceContext* context,
    const SmileSubmission3D* submission, int shadow_pass)
{
    if (submission->palette_index < 0) return 1;
    const SmilePaletteSnapshot3D* snapshot = &smile_frame_palettes3d[submission->palette_index];
    if (snapshot->mode != 2) return 1;
    if (context == 0 || smile_model_palette_buffer3d == 0)
    {
        smile_last_error3d = 48;
        return 0;
    }
    if (smile_model_palette_cached_animator3d != snapshot->animator_handle ||
        smile_model_palette_cached_revision3d != snapshot->pose_revision)
    {
        context->UpdateSubresource(smile_model_palette_buffer3d, 0, 0, snapshot->bones, 0, 0);
        smile_model_palette_cached_animator3d = snapshot->animator_handle;
        smile_model_palette_cached_revision3d = snapshot->pose_revision;
        if (shadow_pass) smile_shadow_palette_upload_count3d++;
        else smile_model_palette_upload_count3d++;
    }
    context->VSSetConstantBuffers(1, 1, &smile_model_palette_buffer3d);
    return 1;
}

static int smile_3d_submission_has_texture(const SmileSubmission3D* submission,
    long long texture_handle, int before)
{
    for (int semantic = 0; semantic < before; ++semantic)
        if (submission->texture_handles[semantic] == texture_handle) return 1;
    return 0;
}

static void smile_3d_release_submission(SmileSubmission3D* submission)
{
    if (submission->kind == SMILE_3D_SUBMISSION_OBJECT)
    {
        SmileMesh3D* mesh = smile_3d_mesh(submission->mesh_handle);
        if (mesh != 0 && mesh->in_flight > 0) mesh->in_flight--;
    }
    else if (submission->kind == SMILE_3D_SUBMISSION_PARTICLE_BATCH)
    {
        SmileParticleBatch3D* batch = smile_3d_particle_batch(submission->source_handle);
        if (batch != 0 && batch->in_flight > 0) batch->in_flight--;
    }
    else if (submission->kind == SMILE_3D_SUBMISSION_RIBBON_BATCH)
    {
        SmileRibbonBatch3D* batch = smile_3d_ribbon_batch(submission->source_handle);
        if (batch != 0 && batch->in_flight > 0) batch->in_flight--;
    }
    for (int semantic = 0; semantic < 4; ++semantic)
    {
        long long handle = submission->texture_handles[semantic];
        if (handle == 0 || smile_3d_submission_has_texture(submission, handle, semantic)) continue;
        SmileTexture3D* texture = smile_3d_texture(handle);
        if (texture != 0 && texture->in_flight > 0) texture->in_flight--;
    }
    ZeroMemory(submission, sizeof(*submission));
}

static void smile_3d_release_submissions(unsigned int first, unsigned int last)
{
    while (last > first) smile_3d_release_submission(&smile_frame_submissions3d[--last]);
}

static int smile_3d_capture_submission(long long handle, SmileSubmission3D* submission)
{
    SmileObject3D* object = smile_3d_object(handle);
    SmileMesh3D* mesh;
    SmileMaterial3D* material = 0;
    SmileAnimator3D* animator = 0;
    int palette_index = -1;
    if (!smile_frame_active3d || object == 0) { smile_last_error3d = 14; return 0; }
    if (!object->visible) return 2;
    mesh = smile_3d_mesh(object->mesh_handle);
    if (mesh == 0 || !smile_3d_upload(mesh)) return 0;
    if (object->material_handle != 0)
    {
        material = smile_3d_material(object->material_handle);
        if (material == 0) { smile_last_error3d = 5; return 0; }
        for (int semantic = 0; semantic < 4; ++semantic)
        {
            long long texture_handle = material->texture_handles[semantic];
            if (texture_handle == 0) continue;
            SmileTexture3D* texture = smile_3d_texture(texture_handle);
            if (texture == 0 || !smile_3d_upload_texture(texture)) return 0;
        }
    }
    if (object->animator_handle != 0)
    {
        animator = smile_3d_animator(object->animator_handle);
        if (animator != 0 && animator->model_animation)
        {
            SmileModel3D* model = smile_3d_model_resource(animator->model_handle);
            if (model == 0 || !smile_3d_model_owns_mesh(model, mesh) ||
                mesh->max_joint >= model->animation_bone_count)
            { smile_last_error3d = 36; return 0; }
        }
        else
        {
            SmileSkeleton3D* skeleton = animator == 0 ? 0 : smile_3d_skeleton(animator->skeleton_handle);
            if (animator == 0 || skeleton == 0 || mesh->max_joint >= skeleton->bone_count)
            { smile_last_error3d = 36; return 0; }
            if (material != 0 && material->mode == 1)
            {
                SmileAnimationClip3D* clip = smile_3d_clip(animator->clip_handle);
                if (clip != 0 && !clip->pbr_scale_safe)
                { smile_last_error3d = 45; return 0; }
            }
        }
        palette_index = smile_3d_palette_snapshot(object->animator_handle, animator);
        if (palette_index == -2) return 0;
    }
    ZeroMemory(submission, sizeof(*submission));
    submission->kind = SMILE_3D_SUBMISSION_OBJECT;
    submission->visible = 1;
    submission->casts_shadow = object->casts_shadow;
    submission->receives_shadow = object->receives_shadow;
    submission->source_handle = handle;
    submission->mesh_handle = object->mesh_handle;
    submission->palette_index = palette_index;
    submission->object = *object;
    submission->animation_mode = animator == 0 ? 0 : (animator->model_animation ? 2 : 1);
    if (material != 0)
    {
        submission->has_material = 1;
        submission->material = *material;
        submission->alpha_mode = material->alpha_mode;
        submission->double_sided = material->double_sided;
        memcpy(submission->texture_handles, material->texture_handles,
            sizeof(submission->texture_handles));
    }
    else submission->alpha_mode = object->color[3] < 0.999f ? 2 : 0;
    mesh->in_flight++;
    for (int semantic = 0; semantic < 4; ++semantic)
    {
        long long texture_handle = submission->texture_handles[semantic];
        if (texture_handle == 0 || smile_3d_submission_has_texture(submission, texture_handle, semantic)) continue;
        SmileTexture3D* texture = smile_3d_texture(texture_handle);
        if (texture != 0) texture->in_flight++;
    }
    return 1;
}

static int smile_3d_capture_vfx_submission(unsigned char kind, long long handle,
    SmileSubmission3D* submission)
{
    SmileMaterial3D* material;
    SmileTexture3D* texture = 0;
    long long material_handle;
    unsigned int revision;
    unsigned int count;
    if (!smile_frame_active3d) { smile_last_error3d = 14; return 0; }
    if (kind == SMILE_3D_SUBMISSION_PARTICLE_BATCH)
    {
        SmileParticleBatch3D* batch = smile_3d_particle_batch(handle);
        if (batch == 0) { smile_last_error3d = 54; return 0; }
        if (!smile_3d_upload_particle_batch(batch)) return 0;
        material_handle = batch->material_handle;
        revision = batch->revision;
        count = batch->count;
    }
    else
    {
        SmileRibbonBatch3D* batch = smile_3d_ribbon_batch(handle);
        if (batch == 0) { smile_last_error3d = 54; return 0; }
        if (!smile_3d_upload_ribbon_batch(batch)) return 0;
        material_handle = batch->material_handle;
        revision = batch->revision;
        count = batch->count;
    }
    if (count == 0) return 2;
    material = smile_3d_material(material_handle);
    if (material == 0 || material->mode != 0 ||
        (material->alpha_mode != 2 && material->alpha_mode != 3))
    { smile_last_error3d = 54; return 0; }
    if (material->texture_handles[0] != 0)
    {
        texture = smile_3d_texture(material->texture_handles[0]);
        if (texture == 0 || !smile_3d_upload_texture(texture)) return 0;
    }
    ZeroMemory(submission, sizeof(*submission));
    submission->kind = kind;
    submission->visible = 1;
    submission->source_handle = handle;
    submission->resource_revision = revision;
    submission->has_material = 1;
    submission->material = *material;
    submission->alpha_mode = material->alpha_mode;
    submission->texture_handles[0] = material->texture_handles[0];
    submission->palette_index = -1;
    if (kind == SMILE_3D_SUBMISSION_PARTICLE_BATCH)
        smile_3d_particle_batch(handle)->in_flight++;
    else
        smile_3d_ribbon_batch(handle)->in_flight++;
    if (texture != 0) texture->in_flight++;
    return 1;
}

static int smile_3d_begin(long long red, long long green, long long blue)
{
    ID3D11DeviceContext* context;
    ID3D11RenderTargetView* target;
    D3D11_VIEWPORT viewport = {};
    float clear[4];
    int use_pending_camera;
    if (smile_frame_active3d)
    {
        smile_3d_clear_pending_camera();
        smile_last_error3d = SMILE_3D_CAMERA_ERROR_FRAME_ACTIVE;
        return 0;
    }
    use_pending_camera = smile_pending_camera_has_projection3d ||
        smile_pending_camera_has_up3d;
    if (use_pending_camera && !smile_3d_validate_pending_camera())
    {
        smile_3d_clear_pending_camera();
        return 0;
    }
    smile_graphics_begin_frame();
    if (!smile_graphics_directx_suspend_2d() || !smile_3d_create_pipeline() ||
        !smile_3d_prepare_m5_resources())
    {
        smile_graphics_directx_resume_2d();
        smile_3d_clear_pending_camera();
        smile_last_error3d = 13;
        return 0;
    }
    if (use_pending_camera) smile_3d_promote_pending_camera();
    context = (ID3D11DeviceContext*)smile_graphics_directx_context();
    target = smile_color_view3d != 0
        ? smile_color_view3d
        : (ID3D11RenderTargetView*)smile_graphics_directx_render_target();
    context->OMSetRenderTargets(1, &target, smile_depth_view3d);
    context->OMSetDepthStencilState(smile_depth_state3d, 0);
    context->OMSetBlendState(0, 0, 0xffffffff);
    context->RSSetState(smile_raster_state3d);
    viewport.TopLeftX = (FLOAT)smile_graphics_directx_viewport_x();
    viewport.TopLeftY = (FLOAT)smile_graphics_directx_viewport_y();
    viewport.Width = (FLOAT)smile_graphics_directx_viewport_width();
    viewport.Height = (FLOAT)smile_graphics_directx_viewport_height();
    viewport.MinDepth = 0.0f; viewport.MaxDepth = 1.0f;
    context->RSSetViewports(1, &viewport);
    clear[0] = (float)(red & 255) / 255.0f; clear[1] = (float)(green & 255) / 255.0f;
    clear[2] = (float)(blue & 255) / 255.0f; clear[3] = 1.0f;
    memcpy(smile_frame_clear3d, clear, sizeof(clear));
    if (smile_hdr_effective3d)
        for (int component = 0; component < 3; ++component)
            clear[component] = clear[component] <= 0.04045f
                ? clear[component] / 12.92f
                : powf((clear[component] + 0.055f) / 1.055f, 2.4f);
    context->ClearRenderTargetView(target, clear);
    context->ClearDepthStencilView(smile_depth_view3d, D3D11_CLEAR_DEPTH | D3D11_CLEAR_STENCIL, 1.0f, 0);
    context->IASetInputLayout(smile_input_layout3d); context->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    context->VSSetShader(smile_vertex_shader3d, 0, 0); context->PSSetShader(smile_pixel_shader3d, 0, 0);
    context->VSSetConstantBuffers(0, 1, &smile_constant_buffer3d); context->PSSetConstantBuffers(0, 1, &smile_constant_buffer3d);
    smile_draw_call_count3d = 0;
    smile_submitted_triangle_count3d = 0;
    smile_pbr_draw_count3d = 0;
    smile_simple_draw_count3d = 0;
    smile_pbr_triangle_count3d = 0;
    smile_3d_release_submissions(0, smile_frame_submission_count3d);
    smile_frame_submission_count3d = 0;
    smile_frame_palette_count3d = 0;
    smile_submission_group_active3d = 0;
    smile_submission_group_token3d = 0;
    smile_submission_group_reserved3d = 0;
    smile_submission_group_physical3d = 0;
    smile_submission_group_logical3d = 0;
    smile_logical_submission_count3d = 0;
    smile_physical_submission_count3d = 0;
    smile_rejected_submission_count3d = 0;
    smile_shadow_draw_count3d = 0;
    smile_shadow_triangle_count3d = 0;
    smile_shadow_palette_upload_count3d = 0;
    smile_post_draw_count3d = 0;
    smile_resolve_count3d = 0;
    smile_vfx_draw_count3d = 0;
    smile_vfx_triangle_count3d = 0;
    smile_vfx_particle_draw_count3d = 0;
    smile_vfx_ribbon_draw_count3d = 0;
    smile_vfx_particle_triangle_count3d = 0;
    smile_vfx_ribbon_triangle_count3d = 0;
    smile_vfx_particle_submission_count3d = 0;
    smile_vfx_ribbon_submission_count3d = 0;
    smile_frame_active3d = 1;
    return 1;
}

static int smile_3d_draw_pbr(const SmileSubmission3D* submission)
{
    const SmileObject3D* object = &submission->object;
    SmileMesh3D* mesh = smile_3d_mesh(submission->mesh_handle);
    const SmileMaterial3D* material = &submission->material;
    SmileTexture3D* textures[4] = {};
    ID3D11ShaderResourceView* views[4] = {};
    ID3D11SamplerState* samplers[4] = {};
    ID3D11DeviceContext* context = (ID3D11DeviceContext*)smile_graphics_directx_context();
    SmilePbrConstants3D constants = {};
    SmileMatrix3D view, projection;
    UINT stride = sizeof(SmileVertex3D), offset = 0;
    float aspect;
    if (context == 0 || !smile_pbr_shader_available3d || smile_pbr_vertex_shader3d == 0 ||
        smile_pbr_pixel_shader3d == 0)
    {
        smile_last_error3d = 44;
        return 0;
    }
    if (mesh == 0) { smile_last_error3d = 5; return 0; }
    constants.model = smile_3d_model(object);
    if (smile_3d_linear_determinant(constants.model) <= 0.0000001f)
    {
        smile_last_error3d = 46;
        return 0;
    }
    for (int semantic = 0; semantic < 4; ++semantic)
    {
        if (material->texture_handles[semantic] == 0) continue;
        textures[semantic] = smile_3d_texture(material->texture_handles[semantic]);
        if (textures[semantic] == 0 || !smile_3d_upload_texture(textures[semantic])) return 0;
        views[semantic] = textures[semantic]->view;
        samplers[semantic] = textures[semantic]->sampler;
    }
    constants.normal_matrix = smile_3d_normal_matrix(constants.model);
    view = smile_3d_view();
    aspect = (float)smile_graphics_directx_viewport_width() /
        (float)smile_graphics_directx_viewport_height();
    projection = smile_3d_projection(aspect > 0.0f ? aspect : 1.0f);
    constants.mvp = smile_3d_multiply(smile_3d_multiply(constants.model, view), projection);
    constants.shadow_mvp = smile_3d_multiply(constants.model, smile_shadow_view_projection3d);
    constants.shadow[0] = smile_shadow_effective3d && submission->receives_shadow ? 1.0f : 0.0f;
    constants.shadow[1] = (float)smile_shadow_caster3d;
    constants.shadow[2] = (float)smile_shadow_slot3d;
    constants.shadow[3] = smile_shadow_resolution3d > 0
        ? 1.0f / (float)smile_shadow_resolution3d : 0.0f;
    constants.output[0] = smile_hdr_effective3d ? 1.0f : 0.0f;
    constants.output[1] = smile_shadow_bias3d;
    constants.output[2] = smile_shadow_normal_bias3d;
    constants.output[3] = (float)smile_material_inspection3d;
    memcpy(constants.object_color, object->color, sizeof(constants.object_color));
    memcpy(constants.base_factor, material->color, sizeof(constants.base_factor));
    constants.surface_factors[0] = material->metallic;
    constants.surface_factors[1] = material->roughness;
    constants.surface_factors[2] = material->normal_strength;
    constants.surface_factors[3] = material->occlusion_strength;
    memcpy(constants.emissive_alpha, material->emissive_color, sizeof(material->emissive_color));
    constants.emissive_alpha[3] = material->alpha_mode == 1 ? material->cutoff : -1.0f;
    for (int semantic = 0; semantic < 4; ++semantic)
        constants.texture_flags[semantic] = textures[semantic] == 0 ? 0.0f : 1.0f;
    memcpy(constants.camera_position, smile_camera_position3d, sizeof(smile_camera_position3d));
    memcpy(constants.ambient, smile_ambient_color3d, sizeof(smile_ambient_color3d));
    constants.ambient[3] = smile_ambient_intensity3d;
    memcpy(constants.directional_direction, smile_directional_light3d.direction,
        sizeof(smile_directional_light3d.direction));
    constants.directional_direction[3] = smile_directional_light3d.enabled ? 1.0f : 0.0f;
    memcpy(constants.directional_color, smile_directional_light3d.color,
        sizeof(smile_directional_light3d.color));
    constants.directional_color[3] = smile_directional_light3d.intensity;
    for (int light = 0; light < SMILE_3D_MAX_LOCAL_LIGHTS; ++light)
    {
        memcpy(constants.local_position_type[light], smile_local_lights3d[light].position,
            sizeof(smile_local_lights3d[light].position));
        constants.local_position_type[light][3] = (float)smile_local_lights3d[light].type;
        memcpy(constants.local_direction_range[light], smile_local_lights3d[light].direction,
            sizeof(smile_local_lights3d[light].direction));
        constants.local_direction_range[light][3] = smile_local_lights3d[light].range;
        memcpy(constants.local_color_intensity[light], smile_local_lights3d[light].color,
            sizeof(smile_local_lights3d[light].color));
        constants.local_color_intensity[light][3] = smile_local_lights3d[light].intensity;
        constants.local_cone[light][0] = smile_local_lights3d[light].inner_cosine;
        constants.local_cone[light][1] = smile_local_lights3d[light].outer_cosine;
    }
    constants.animation[0] = (float)submission->animation_mode;
    for (int bone = 0; bone < SMILE_3D_MAX_BONES; ++bone)
        constants.bones[bone] = submission->palette_index < 0
            ? smile_3d_identity() : smile_frame_palettes3d[submission->palette_index].bones[bone];
    context->UpdateSubresource(smile_pbr_constant_buffer3d, 0, 0, &constants, 0, 0);
    context->OMSetBlendState(material->alpha_mode == 2 ? smile_blend_state3d : 0, 0, 0xffffffff);
    context->OMSetDepthStencilState(material->alpha_mode == 2 ? smile_depth_read_state3d : smile_depth_state3d, 0);
    context->RSSetState(material->double_sided ? smile_raster_state3d : smile_cull_raster_state3d);
    context->IASetInputLayout(smile_pbr_input_layout3d);
    context->IASetVertexBuffers(0, 1, &mesh->vertex_buffer, &stride, &offset);
    context->IASetIndexBuffer(mesh->index_buffer, DXGI_FORMAT_R32_UINT, 0);
    context->VSSetShader(smile_pbr_vertex_shader3d, 0, 0);
    context->PSSetShader(smile_pbr_pixel_shader3d, 0, 0);
    context->VSSetConstantBuffers(0, 1, &smile_pbr_constant_buffer3d);
    context->PSSetConstantBuffers(0, 1, &smile_pbr_constant_buffer3d);
    if (!smile_3d_upload_palette_snapshot(context, submission, 0)) return 0;
    context->PSSetShaderResources(0, 4, views);
    context->PSSetSamplers(0, 4, samplers);
    context->PSSetShaderResources(5, 1, &smile_shadow_shader_view3d);
    context->PSSetSamplers(5, 1, &smile_shadow_sampler3d);
    context->DrawIndexed(mesh->index_count, 0, 0);
    smile_draw_call_count3d++;
    smile_submitted_triangle_count3d += mesh->index_count / 3;
    smile_pbr_draw_count3d++;
    smile_pbr_triangle_count3d += mesh->index_count / 3;
    return 1;
}

static int smile_3d_draw_vfx_submission(const SmileSubmission3D* submission)
{
    ID3D11DeviceContext* context = (ID3D11DeviceContext*)smile_graphics_directx_context();
    const SmileMaterial3D* material = &submission->material;
    SmileTexture3D* texture = 0;
    ID3D11ShaderResourceView* texture_view = 0;
    ID3D11SamplerState* texture_sampler = 0;
    SmileVfxConstants3D constants = {};
    SmileMatrix3D view;
    SmileMatrix3D projection;
    float aspect;
    if (context == 0 || smile_particle_vertex_shader3d == 0 ||
        smile_ribbon_vertex_shader3d == 0 || smile_vfx_pixel_shader3d == 0)
    { smile_last_error3d = 57; return 0; }
    if (material->texture_handles[0] != 0)
    {
        texture = smile_3d_texture(material->texture_handles[0]);
        if (texture == 0 || !smile_3d_upload_texture(texture)) return 0;
        texture_view = texture->view;
        texture_sampler = texture->sampler;
    }
    view = smile_3d_view();
    aspect = (float)smile_graphics_directx_viewport_width() /
        (float)smile_graphics_directx_viewport_height();
    projection = smile_3d_projection(aspect > 0.0f ? aspect : 1.0f);
    constants.view_projection = smile_3d_multiply(view, projection);
    constants.camera_right[0] = view.m[0];
    constants.camera_right[1] = view.m[4];
    constants.camera_right[2] = view.m[8];
    constants.camera_right[3] = material->emissive;
    constants.camera_up[0] = view.m[1];
    constants.camera_up[1] = view.m[5];
    constants.camera_up[2] = view.m[9];
    constants.camera_up[3] = material->cutoff;
    constants.atlas_output[2] = smile_hdr_effective3d ? 1.0f : 0.0f;
    constants.atlas_output[3] = texture == 0 ? 0.0f : 1.0f;
    memcpy(constants.material, material->color, sizeof(constants.material));
    context->OMSetBlendState(material->alpha_mode == 3
        ? smile_additive_blend_state3d : smile_blend_state3d, 0, 0xffffffff);
    context->OMSetDepthStencilState(smile_depth_read_state3d, 0);
    context->RSSetState(smile_raster_state3d);
    context->PSSetShader(smile_vfx_pixel_shader3d, 0, 0);
    context->VSSetConstantBuffers(0, 1, &smile_vfx_constant_buffer3d);
    context->PSSetConstantBuffers(0, 1, &smile_vfx_constant_buffer3d);
    context->PSSetShaderResources(0, 1, &texture_view);
    context->PSSetSamplers(0, 1, &texture_sampler);
    if (submission->kind == SMILE_3D_SUBMISSION_PARTICLE_BATCH)
    {
        SmileParticleBatch3D* batch = smile_3d_particle_batch(submission->source_handle);
        ID3D11Buffer* buffers[2];
        UINT strides[2] = { sizeof(float) * 4, sizeof(SmileParticleInstance3D) };
        UINT offsets[2] = {};
        if (batch == 0 || batch->revision != submission->resource_revision ||
            !smile_3d_upload_particle_batch(batch))
        { smile_last_error3d = 56; return 0; }
        if (batch->billboard_mode == 2)
        {
            constants.camera_right[1] = 0.0f;
            smile_3d_normalize(&constants.camera_right[0], &constants.camera_right[1],
                &constants.camera_right[2]);
            constants.camera_up[0] = 0.0f;
            constants.camera_up[1] = 1.0f;
            constants.camera_up[2] = 0.0f;
        }
        constants.atlas_output[0] = 1.0f / (float)batch->atlas_columns;
        constants.atlas_output[1] = 1.0f / (float)batch->atlas_rows;
        context->UpdateSubresource(smile_vfx_constant_buffer3d, 0, 0, &constants, 0, 0);
        buffers[0] = smile_particle_quad_vertex_buffer3d;
        buffers[1] = batch->instance_buffer;
        context->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
        context->IASetInputLayout(smile_particle_input_layout3d);
        context->IASetVertexBuffers(0, 2, buffers, strides, offsets);
        context->IASetIndexBuffer(smile_particle_quad_index_buffer3d,
            DXGI_FORMAT_R16_UINT, 0);
        context->VSSetShader(smile_particle_vertex_shader3d, 0, 0);
        context->DrawIndexedInstanced(6, batch->count, 0, 0, 0);
        smile_vfx_particle_draw_count3d++;
        smile_vfx_particle_triangle_count3d += (long long)batch->count * 2;
        smile_vfx_triangle_count3d += (long long)batch->count * 2;
        smile_submitted_triangle_count3d += (long long)batch->count * 2;
    }
    else if (submission->kind == SMILE_3D_SUBMISSION_RIBBON_BATCH)
    {
        SmileRibbonBatch3D* batch = smile_3d_ribbon_batch(submission->source_handle);
        UINT stride = sizeof(SmileRibbonVertex3D), offset = 0;
        if (batch == 0 || batch->revision != submission->resource_revision ||
            !smile_3d_upload_ribbon_batch(batch))
        { smile_last_error3d = 56; return 0; }
        constants.atlas_output[0] = 1.0f;
        constants.atlas_output[1] = 1.0f;
        context->UpdateSubresource(smile_vfx_constant_buffer3d, 0, 0, &constants, 0, 0);
        context->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP);
        context->IASetInputLayout(smile_ribbon_input_layout3d);
        context->IASetVertexBuffers(0, 1, &batch->vertex_buffer, &stride, &offset);
        context->IASetIndexBuffer(0, DXGI_FORMAT_UNKNOWN, 0);
        context->VSSetShader(smile_ribbon_vertex_shader3d, 0, 0);
        context->Draw(batch->count * 2, 0);
        smile_vfx_ribbon_draw_count3d++;
        smile_vfx_ribbon_triangle_count3d += batch->count < 2 ? 0 : (long long)batch->count * 2 - 2;
        smile_vfx_triangle_count3d += batch->count < 2 ? 0 : (long long)batch->count * 2 - 2;
        smile_submitted_triangle_count3d += batch->count < 2 ? 0 :
            (long long)batch->count * 2 - 2;
    }
    else
    { smile_last_error3d = 54; return 0; }
    smile_draw_call_count3d++;
    smile_vfx_draw_count3d++;
    return 1;
}

static int smile_3d_draw_submission(const SmileSubmission3D* submission)
{
    const SmileObject3D* object = &submission->object;
    SmileMesh3D* mesh = smile_3d_mesh(submission->mesh_handle);
    const SmileMaterial3D* material = submission->has_material ? &submission->material : 0;
    SmileTexture3D* texture = 0;
    ID3D11DeviceContext* context = (ID3D11DeviceContext*)smile_graphics_directx_context();
    ID3D11ShaderResourceView* texture_view = 0;
    ID3D11SamplerState* texture_sampler = 0;
    SmileConstants3D constants = {};
    SmileMatrix3D view, projection;
    UINT stride = sizeof(SmileVertex3D), offset = 0;
    float aspect;
    int alpha_mode;
    if (!smile_frame_active3d)
    { smile_last_error3d = 14; return 0; }
    if (submission->kind != SMILE_3D_SUBMISSION_OBJECT)
        return smile_3d_draw_vfx_submission(submission);
    if (mesh == 0 || !smile_3d_upload(mesh)) return 0;
    if (material != 0)
    {
        if (material->mode == 1) return smile_3d_draw_pbr(submission);
        if (material->texture_handles[0] != 0)
        {
            texture = smile_3d_texture(material->texture_handles[0]);
            if (texture == 0 || !smile_3d_upload_texture(texture)) return 0;
            texture_view = texture->view;
            texture_sampler = texture->sampler;
        }
    }
    constants.model = smile_3d_model(object);
    view = smile_3d_view();
    aspect = (float)smile_graphics_directx_viewport_width() / (float)smile_graphics_directx_viewport_height();
    projection = smile_3d_projection(aspect > 0.0f ? aspect : 1.0f);
    constants.mvp = smile_3d_multiply(smile_3d_multiply(constants.model, view), projection);
    constants.shadow_mvp = smile_3d_multiply(constants.model, smile_shadow_view_projection3d);
    constants.shadow[0] = smile_shadow_effective3d && submission->receives_shadow ? 1.0f : 0.0f;
    constants.shadow[1] = (float)smile_shadow_caster3d;
    constants.shadow[2] = (float)smile_shadow_slot3d;
    constants.shadow[3] = smile_shadow_resolution3d > 0
        ? 1.0f / (float)smile_shadow_resolution3d : 0.0f;
    constants.output[0] = smile_hdr_effective3d ? 1.0f : 0.0f;
    constants.output[1] = smile_shadow_bias3d;
    constants.output[2] = smile_shadow_normal_bias3d;
    if (smile_shadow_caster3d == 2)
    {
        memcpy(constants.shadow_light, smile_local_lights3d[smile_shadow_slot3d].position,
            sizeof(smile_local_lights3d[smile_shadow_slot3d].position));
        constants.shadow_light[3] = 2.0f;
    }
    else
    {
        memcpy(constants.shadow_light, smile_directional_light3d.direction,
            sizeof(smile_directional_light3d.direction));
        constants.shadow_light[3] = 1.0f;
    }
    constants.color[0] = object->color[0] * (material == 0 ? 1.0f : material->color[0]);
    constants.color[1] = object->color[1] * (material == 0 ? 1.0f : material->color[1]);
    constants.color[2] = object->color[2] * (material == 0 ? 1.0f : material->color[2]);
    constants.color[3] = object->color[3] * (material == 0 ? 1.0f : material->color[3]);
    constants.material[0] = texture == 0 ? 0.0f : 1.0f;
    constants.material[1] = material == 0 ? 0.0f : (float)material->unlit;
    constants.material[2] = material == 0 ? 0.0f : material->emissive;
    constants.material[3] = material != 0 && material->alpha_mode == 1 ? material->cutoff : -1.0f;
    constants.animation[0] = (float)submission->animation_mode;
    for (int bone = 0; bone < SMILE_3D_MAX_BONES; ++bone)
        constants.bones[bone] = submission->palette_index < 0
            ? smile_3d_identity() : smile_frame_palettes3d[submission->palette_index].bones[bone];
    alpha_mode = material == 0 ? (constants.color[3] < 0.999f ? 2 : 0) : material->alpha_mode;
    context->UpdateSubresource(smile_constant_buffer3d, 0, 0, &constants, 0, 0);
    context->RSSetState(smile_raster_state3d);
    context->IASetInputLayout(smile_input_layout3d);
    context->VSSetShader(smile_vertex_shader3d, 0, 0);
    context->PSSetShader(smile_pixel_shader3d, 0, 0);
    context->VSSetConstantBuffers(0, 1, &smile_constant_buffer3d);
    context->PSSetConstantBuffers(0, 1, &smile_constant_buffer3d);
    if (!smile_3d_upload_palette_snapshot(context, submission, 0)) return 0;
    context->OMSetBlendState(
        alpha_mode == 3 ? smile_additive_blend_state3d : (alpha_mode == 2 ? smile_blend_state3d : 0),
        0,
        0xffffffff
    );
    context->OMSetDepthStencilState(
        alpha_mode == 2 || alpha_mode == 3 ? smile_depth_read_state3d : smile_depth_state3d,
        0
    );
    context->IASetVertexBuffers(0, 1, &mesh->vertex_buffer, &stride, &offset);
    context->IASetIndexBuffer(mesh->index_buffer, DXGI_FORMAT_R32_UINT, 0);
    context->PSSetShaderResources(0, 1, &texture_view);
    context->PSSetSamplers(0, 1, &texture_sampler);
    context->PSSetShaderResources(5, 1, &smile_shadow_shader_view3d);
    context->PSSetSamplers(5, 1, &smile_shadow_sampler3d);
    context->DrawIndexed(mesh->index_count, 0, 0);
    smile_draw_call_count3d++;
    smile_submitted_triangle_count3d += mesh->index_count / 3;
    smile_simple_draw_count3d++;
    return 1;
}

static int smile_3d_draw(long long handle)
{
    SmileObject3D* object = smile_3d_object(handle);
    SmileSubmission3D* submission;
    int captured;
    int result;
    if (!smile_frame_active3d || object == 0)
    {
        smile_last_error3d = 14;
        return 0;
    }
    if (smile_multipass_active3d || smile_submission_group_active3d)
    {
        if (!object->visible)
        {
            if (smile_submission_group_active3d) smile_submission_group_logical3d++;
            else smile_logical_submission_count3d++;
            return 1;
        }
        if (smile_frame_submission_count3d >= SMILE_3D_MAX_FRAME_SUBMISSIONS ||
            (smile_submission_group_active3d &&
                smile_submission_group_physical3d >= smile_submission_group_reserved3d))
        {
            smile_rejected_submission_count3d++;
            smile_last_error3d = 51;
            return 0;
        }
        submission = &smile_frame_submissions3d[smile_frame_submission_count3d];
        captured = smile_3d_capture_submission(handle, submission);
        if (!captured) return 0;
        if (captured == 1)
        {
            smile_frame_submission_count3d++;
            if (smile_submission_group_active3d) smile_submission_group_physical3d++;
        }
        if (smile_submission_group_active3d) smile_submission_group_logical3d++;
        else
        {
            smile_logical_submission_count3d++;
            if (captured == 1) smile_physical_submission_count3d++;
        }
        return 1;
    }
    unsigned int palette_start = smile_frame_palette_count3d;
    submission = &smile_frame_submissions3d[0];
    captured = smile_3d_capture_submission(handle, submission);
    if (!captured)
    {
        smile_frame_palette_count3d = palette_start;
        return 0;
    }
    result = captured == 2 ? 1 : smile_3d_draw_submission(submission);
    if (captured == 1) smile_3d_release_submission(submission);
    smile_frame_palette_count3d = palette_start;
    if (result)
    {
        smile_logical_submission_count3d++;
        if (captured == 1) smile_physical_submission_count3d++;
    }
    return result;
}

static int smile_3d_draw_vfx_batch(unsigned char kind, long long handle)
{
    SmileSubmission3D* submission;
    int captured;
    int result;
    if (!smile_frame_active3d)
    { smile_last_error3d = 14; return 0; }
    if (smile_multipass_active3d || smile_submission_group_active3d)
    {
        if (smile_frame_submission_count3d >= SMILE_3D_MAX_FRAME_SUBMISSIONS ||
            (smile_submission_group_active3d &&
                smile_submission_group_physical3d >= smile_submission_group_reserved3d))
        {
            smile_rejected_submission_count3d++;
            smile_vfx_rejected_operation_count3d++;
            smile_last_error3d = 51;
            return 0;
        }
        submission = &smile_frame_submissions3d[smile_frame_submission_count3d];
        captured = smile_3d_capture_vfx_submission(kind, handle, submission);
        if (!captured) return 0;
        if (captured == 1)
        {
            smile_frame_submission_count3d++;
            if (smile_submission_group_active3d) smile_submission_group_physical3d++;
        }
        if (smile_submission_group_active3d) smile_submission_group_logical3d++;
        else
        {
            smile_logical_submission_count3d++;
            if (captured == 1) smile_physical_submission_count3d++;
        }
        if (kind == SMILE_3D_SUBMISSION_PARTICLE_BATCH)
            smile_vfx_particle_submission_count3d++;
        else smile_vfx_ribbon_submission_count3d++;
        return 1;
    }
    submission = &smile_frame_submissions3d[0];
    captured = smile_3d_capture_vfx_submission(kind, handle, submission);
    if (!captured) return 0;
    result = captured == 2 ? 1 : smile_3d_draw_submission(submission);
    if (captured == 1) smile_3d_release_submission(submission);
    if (result)
    {
        smile_logical_submission_count3d++;
        if (captured == 1) smile_physical_submission_count3d++;
        if (kind == SMILE_3D_SUBMISSION_PARTICLE_BATCH)
            smile_vfx_particle_submission_count3d++;
        else smile_vfx_ribbon_submission_count3d++;
    }
    return result;
}

static int smile_3d_render_shadow_pass(void)
{
    ID3D11DeviceContext* context = (ID3D11DeviceContext*)smile_graphics_directx_context();
    D3D11_VIEWPORT viewport = {};
    ID3D11ShaderResourceView* no_view = 0;
    if (!smile_shadow_effective3d || context == 0) return 1;
    context->PSSetShaderResources(5, 1, &no_view);
    context->OMSetRenderTargets(0, 0, smile_shadow_depth_view3d);
    context->ClearDepthStencilView(smile_shadow_depth_view3d, D3D11_CLEAR_DEPTH, 1.0f, 0);
    viewport.Width = viewport.Height = (FLOAT)smile_shadow_resolution3d;
    viewport.MinDepth = 0.0f;
    viewport.MaxDepth = 1.0f;
    context->RSSetViewports(1, &viewport);
    context->OMSetDepthStencilState(smile_depth_state3d, 0);
    context->OMSetBlendState(0, 0, 0xffffffff);
    context->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    context->IASetInputLayout(smile_shadow_input_layout3d);
    context->VSSetShader(smile_shadow_vertex_shader3d, 0, 0);
    context->PSSetShader(smile_shadow_pixel_shader3d, 0, 0);
    context->VSSetConstantBuffers(0, 1, &smile_shadow_constant_buffer3d);
    context->PSSetConstantBuffers(0, 1, &smile_shadow_constant_buffer3d);
    smile_model_palette_cached_animator3d = 0;
    smile_model_palette_cached_revision3d = 0;
    for (unsigned int submission = 0; submission < smile_frame_submission_count3d; ++submission)
    {
        const SmileSubmission3D* entry = &smile_frame_submissions3d[submission];
        const SmileObject3D* object = &entry->object;
        SmileMesh3D* mesh;
        const SmileMaterial3D* material;
        SmileTexture3D* texture = 0;
        ID3D11ShaderResourceView* texture_view = 0;
        ID3D11SamplerState* texture_sampler = 0;
        SmileShadowConstants3D constants = {};
        SmileMatrix3D model;
        UINT stride = sizeof(SmileVertex3D), offset = 0;
        int alpha_mode;
        if (entry->kind != SMILE_3D_SUBMISSION_OBJECT) continue;
        if (!entry->visible || !entry->casts_shadow) continue;
        mesh = smile_3d_mesh(entry->mesh_handle);
        if (mesh == 0 || !smile_3d_upload(mesh)) return 0;
        material = entry->has_material ? &entry->material : 0;
        alpha_mode = entry->alpha_mode;
        if (alpha_mode == 2 || alpha_mode == 3) continue;
        if (material != 0 && material->texture_handles[0] != 0)
        {
            texture = smile_3d_texture(material->texture_handles[0]);
            if (texture == 0 || !smile_3d_upload_texture(texture)) return 0;
            texture_view = texture->view;
            texture_sampler = texture->sampler;
        }
        model = smile_3d_model(object);
        if (smile_3d_linear_determinant(model) <= 0.0000001f)
        { smile_last_error3d = 46; return 0; }
        constants.mvp = smile_3d_multiply(model, smile_shadow_view_projection3d);
        constants.alpha[0] = texture == 0 ? 0.0f : 1.0f;
        constants.alpha[1] = alpha_mode == 1 && material != 0 ? material->cutoff : -1.0f;
        constants.alpha[2] = object->color[3] * (material == 0 ? 1.0f : material->color[3]);
        constants.animation[0] = (float)entry->animation_mode;
        for (int bone = 0; bone < SMILE_3D_MAX_BONES; ++bone)
            constants.bones[bone] = entry->palette_index < 0
                ? smile_3d_identity() : smile_frame_palettes3d[entry->palette_index].bones[bone];
        context->UpdateSubresource(smile_shadow_constant_buffer3d, 0, 0, &constants, 0, 0);
        if (!smile_3d_upload_palette_snapshot(context, entry, 1)) return 0;
        context->RSSetState(entry->double_sided
            ? smile_shadow_double_raster_state3d : smile_shadow_raster_state3d);
        context->IASetVertexBuffers(0, 1, &mesh->vertex_buffer, &stride, &offset);
        context->IASetIndexBuffer(mesh->index_buffer, DXGI_FORMAT_R32_UINT, 0);
        context->PSSetShaderResources(0, 1, &texture_view);
        context->PSSetSamplers(0, 1, &texture_sampler);
        context->DrawIndexed(mesh->index_count, 0, 0);
        smile_shadow_draw_count3d++;
        smile_shadow_triangle_count3d += mesh->index_count / 3;
    }
    context->PSSetShaderResources(0, 1, &no_view);
    context->OMSetRenderTargets(0, 0, 0);
    smile_model_palette_cached_animator3d = 0;
    smile_model_palette_cached_revision3d = 0;
    return 1;
}

static void smile_3d_bind_post_pass(ID3D11DeviceContext* context,
    ID3D11RenderTargetView* target, int width, int height,
    ID3D11ShaderResourceView* first_view, ID3D11ShaderResourceView* second_view,
    const SmilePostConstants3D* constants)
{
    D3D11_VIEWPORT viewport = {};
    ID3D11ShaderResourceView* views[2] = { first_view, second_view };
    context->OMSetRenderTargets(1, &target, 0);
    viewport.Width = (FLOAT)width;
    viewport.Height = (FLOAT)height;
    viewport.MinDepth = 0.0f;
    viewport.MaxDepth = 1.0f;
    context->RSSetViewports(1, &viewport);
    context->OMSetDepthStencilState(0, 0);
    context->OMSetBlendState(0, 0, 0xffffffff);
    context->RSSetState(smile_raster_state3d);
    context->IASetInputLayout(0);
    context->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    context->VSSetShader(smile_post_vertex_shader3d, 0, 0);
    context->PSSetShader(smile_post_pixel_shader3d, 0, 0);
    context->VSSetConstantBuffers(0, 1, &smile_post_constant_buffer3d);
    context->PSSetConstantBuffers(0, 1, &smile_post_constant_buffer3d);
    context->UpdateSubresource(smile_post_constant_buffer3d, 0, 0, constants, 0, 0);
    context->PSSetShaderResources(0, 2, views);
    context->PSSetSamplers(0, 1, &smile_post_sampler3d);
    context->Draw(3, 0);
    smile_post_draw_count3d++;
}

static int smile_3d_run_post_processing(void)
{
    ID3D11DeviceContext* context = (ID3D11DeviceContext*)smile_graphics_directx_context();
    ID3D11RenderTargetView* destination =
        (ID3D11RenderTargetView*)smile_graphics_directx_render_target();
    ID3D11ShaderResourceView* no_views[2] = {};
    SmilePostConstants3D constants = {};
    if (!smile_hdr_effective3d || context == 0 || destination == 0) return 1;
    context->PSSetShaderResources(0, 2, no_views);
    context->OMSetRenderTargets(0, 0, 0);
    if (smile_sample_count3d > 1)
    {
        context->ResolveSubresource(smile_scene_resolve_texture3d, 0,
            smile_color_texture3d, 0, DXGI_FORMAT_R16G16B16A16_FLOAT);
        smile_resolve_count3d++;
    }
    if (smile_bloom_effective3d)
    {
        constants.first[0] = 0.0f;
        constants.first[3] = (float)smile_bloom_threshold3d / 1000.0f;
        smile_3d_bind_post_pass(context, smile_bloom_view_a3d,
            smile_bloom_width3d, smile_bloom_height3d,
            smile_scene_shader_view3d, 0, &constants);
        for (int cycle = 0; cycle < smile_bloom_cycles3d; ++cycle)
        {
            context->PSSetShaderResources(0, 2, no_views);
            constants.first[0] = 1.0f;
            constants.first[1] = 1.0f / (float)smile_bloom_width3d;
            constants.first[2] = 1.0f / (float)smile_bloom_height3d;
            smile_3d_bind_post_pass(context, smile_bloom_view_b3d,
                smile_bloom_width3d, smile_bloom_height3d,
                smile_bloom_shader_a3d, 0, &constants);
            context->PSSetShaderResources(0, 2, no_views);
            constants.first[0] = 2.0f;
            smile_3d_bind_post_pass(context, smile_bloom_view_a3d,
                smile_bloom_width3d, smile_bloom_height3d,
                smile_bloom_shader_b3d, 0, &constants);
        }
    }
    context->PSSetShaderResources(0, 2, no_views);
    constants.first[0] = 3.0f;
    constants.second[0] = smile_bloom_effective3d
        ? (float)smile_bloom_intensity3d / 100.0f : 0.0f;
    constants.second[1] = (float)smile_exposure_percent3d / 100.0f;
    smile_3d_bind_post_pass(context, destination,
        smile_graphics_directx_physical_width(), smile_graphics_directx_physical_height(),
        smile_scene_shader_view3d,
        smile_bloom_effective3d ? smile_bloom_shader_a3d : 0, &constants);
    context->PSSetShaderResources(0, 2, no_views);
    context->OMSetRenderTargets(0, 0, 0);
    return 1;
}

static long long smile_3d_submission_group(long long operation, long long value)
{
    if (!smile_frame_active3d) { smile_last_error3d = 52; return 0; }
    if (operation == 1)
    {
        if (smile_submission_group_active3d || value < 0 ||
            value > SMILE_3D_MAX_FRAME_SUBMISSIONS - smile_frame_submission_count3d ||
            value > SMILE_3D_MAX_FRAME_PALETTES - smile_frame_palette_count3d)
        { smile_last_error3d = 52; return 0; }
        smile_submission_group_active3d = 1;
        smile_submission_group_start3d = smile_frame_submission_count3d;
        smile_submission_group_palette_start3d = smile_frame_palette_count3d;
        smile_submission_group_reserved3d = (unsigned int)value;
        smile_submission_group_physical3d = 0;
        smile_submission_group_logical3d = 0;
        smile_submission_group_serial3d++;
        if (smile_submission_group_serial3d <= 0 ||
            smile_submission_group_serial3d > 2147483647)
            smile_submission_group_serial3d = 1;
        smile_submission_group_token3d = smile_submission_group_serial3d;
        return smile_submission_group_token3d;
    }
    if (!smile_submission_group_active3d || value != smile_submission_group_token3d)
    { smile_last_error3d = 52; return 0; }
    if (operation == 3)
    {
        smile_3d_release_submissions(smile_submission_group_start3d,
            smile_frame_submission_count3d);
        smile_frame_submission_count3d = smile_submission_group_start3d;
        smile_frame_palette_count3d = smile_submission_group_palette_start3d;
        smile_submission_group_active3d = 0;
        smile_submission_group_token3d = 0;
        smile_submission_group_reserved3d = 0;
        smile_submission_group_physical3d = 0;
        smile_submission_group_logical3d = 0;
        return 1;
    }
    if (operation == 2)
    {
        int success = 1;
        if (!smile_multipass_active3d)
            for (unsigned int index = smile_submission_group_start3d;
                index < smile_frame_submission_count3d; ++index)
                if (!smile_3d_draw_submission(&smile_frame_submissions3d[index]))
                { success = 0; break; }
        if (success)
        {
            smile_logical_submission_count3d += smile_submission_group_logical3d;
            smile_physical_submission_count3d += smile_submission_group_physical3d;
        }
        if (!smile_multipass_active3d || !success)
        {
            smile_3d_release_submissions(smile_submission_group_start3d,
                smile_frame_submission_count3d);
            smile_frame_submission_count3d = smile_submission_group_start3d;
            smile_frame_palette_count3d = smile_submission_group_palette_start3d;
        }
        smile_submission_group_active3d = 0;
        smile_submission_group_token3d = 0;
        smile_submission_group_reserved3d = 0;
        smile_submission_group_physical3d = 0;
        smile_submission_group_logical3d = 0;
        return success;
    }
    smile_last_error3d = 52;
    return 0;
}

static int smile_3d_end(void)
{
    int success = 1;
    if (!smile_frame_active3d) return 1;
    ID3D11DeviceContext* context = (ID3D11DeviceContext*)smile_graphics_directx_context();
    if (smile_submission_group_active3d)
    {
        smile_3d_submission_group(3, smile_submission_group_token3d);
        smile_last_error3d = 52;
        success = 0;
    }
    if (success && smile_multipass_active3d && context != 0)
    {
        ID3D11RenderTargetView* target;
        D3D11_VIEWPORT viewport = {};
        success = smile_3d_render_shadow_pass();
        target = smile_color_view3d != 0
            ? smile_color_view3d
            : (ID3D11RenderTargetView*)smile_graphics_directx_render_target();
        context->OMSetRenderTargets(1, &target, smile_depth_view3d);
        context->OMSetDepthStencilState(smile_depth_state3d, 0);
        viewport.TopLeftX = (FLOAT)smile_graphics_directx_viewport_x();
        viewport.TopLeftY = (FLOAT)smile_graphics_directx_viewport_y();
        viewport.Width = (FLOAT)smile_graphics_directx_viewport_width();
        viewport.Height = (FLOAT)smile_graphics_directx_viewport_height();
        viewport.MinDepth = 0.0f;
        viewport.MaxDepth = 1.0f;
        context->RSSetViewports(1, &viewport);
        if (success)
            for (unsigned int submission = 0;
                submission < smile_frame_submission_count3d; ++submission)
                if (!smile_3d_draw_submission(&smile_frame_submissions3d[submission]))
                { success = 0; break; }
    }
    if (context != 0)
    {
        ID3D11ShaderResourceView* empty_views[6] = {};
        context->PSSetShaderResources(0, 6, empty_views);
        context->OMSetRenderTargets(0, 0, 0);
        if (success && smile_hdr_effective3d)
            success = smile_3d_run_post_processing();
        else if (smile_sample_count3d > 1 && smile_color_texture3d != 0)
        {
            ID3D11RenderTargetView* destination_view =
                (ID3D11RenderTargetView*)smile_graphics_directx_render_target();
            ID3D11Resource* destination = 0;
            if (destination_view != 0)
                destination_view->GetResource(&destination);
            if (destination != 0)
            {
                context->ResolveSubresource(destination, 0, smile_color_texture3d, 0,
                    DXGI_FORMAT_B8G8R8A8_UNORM);
                smile_resolve_count3d++;
                destination->Release();
            }
        }
        ID3D11Buffer* empty_buffers[2] = {};
        ID3D11SamplerState* empty_samplers[6] = {};
        UINT empty_strides[2] = {};
        UINT empty_offsets[2] = {};
        context->VSSetShader(0, 0, 0);
        context->PSSetShader(0, 0, 0);
        context->IASetInputLayout(0);
        context->IASetVertexBuffers(0, 2, empty_buffers, empty_strides, empty_offsets);
        context->IASetIndexBuffer(0, DXGI_FORMAT_UNKNOWN, 0);
        context->VSSetConstantBuffers(0, 2, empty_buffers);
        context->PSSetConstantBuffers(0, 2, empty_buffers);
        context->PSSetSamplers(0, 6, empty_samplers);
        context->OMSetBlendState(0, 0, 0xffffffff);
        context->OMSetDepthStencilState(0, 0);
        context->RSSetState(0);
    }
    smile_frame_active3d = 0;
    smile_3d_release_submissions(0, smile_frame_submission_count3d);
    smile_frame_submission_count3d = 0;
    smile_frame_palette_count3d = 0;
    smile_submission_group_active3d = 0;
    smile_submission_group_token3d = 0;
    smile_submission_group_reserved3d = 0;
    smile_submission_group_physical3d = 0;
    smile_submission_group_logical3d = 0;
    smile_graphics_directx_resume_2d();
    return success;
}

extern "C" void smile_graphics3d_on_device_lost(void)
{
    int index;
    smile_3d_release_submissions(0, smile_frame_submission_count3d);
    smile_frame_submission_count3d = 0;
    smile_frame_palette_count3d = 0;
    smile_submission_group_active3d = 0;
    smile_submission_group_token3d = 0;
    smile_frame_active3d = 0;
    for (index = 0; index < SMILE_3D_MAX_MESHES; ++index)
    {
        smile_3d_release(smile_meshes3d[index].vertex_buffer);
        smile_3d_release(smile_meshes3d[index].index_buffer);
    }
    for (index = 0; index < SMILE_3D_MAX_TEXTURES; ++index)
    {
        smile_3d_release(smile_textures3d[index].view);
        smile_3d_release(smile_textures3d[index].texture);
        smile_3d_release(smile_textures3d[index].sampler);
    }
    for (index = 0; index < SMILE_3D_MAX_PARTICLE_BATCHES; ++index)
    {
        smile_3d_release(smile_particle_batches3d[index].instance_buffer);
        smile_particle_batches3d[index].uploaded_revision = 0;
    }
    for (index = 0; index < SMILE_3D_MAX_RIBBON_BATCHES; ++index)
    {
        smile_3d_release(smile_ribbon_batches3d[index].vertex_buffer);
        smile_ribbon_batches3d[index].uploaded_revision = 0;
    }
    smile_3d_release(smile_particle_quad_index_buffer3d);
    smile_3d_release(smile_particle_quad_vertex_buffer3d);
    smile_3d_release(smile_vfx_constant_buffer3d);
    smile_3d_release(smile_ribbon_input_layout3d);
    smile_3d_release(smile_particle_input_layout3d);
    smile_3d_release(smile_vfx_pixel_shader3d);
    smile_3d_release(smile_ribbon_vertex_shader3d);
    smile_3d_release(smile_particle_vertex_shader3d);
    smile_3d_release(smile_color_view3d); smile_3d_release(smile_color_texture3d);
    smile_3d_release(smile_depth_view3d); smile_3d_release(smile_depth_texture3d);
    smile_3d_release(smile_additive_blend_state3d); smile_3d_release(smile_blend_state3d);
    smile_3d_release(smile_cull_raster_state3d); smile_3d_release(smile_raster_state3d);
    smile_3d_release(smile_depth_read_state3d); smile_3d_release(smile_depth_state3d);
    smile_3d_release(smile_pbr_constant_buffer3d); smile_3d_release(smile_pbr_input_layout3d);
    smile_3d_release(smile_pbr_pixel_shader3d); smile_3d_release(smile_pbr_vertex_shader3d);
    smile_3d_release(smile_model_palette_buffer3d);
    smile_3d_release(smile_constant_buffer3d); smile_3d_release(smile_input_layout3d);
    smile_3d_release(smile_pixel_shader3d); smile_3d_release(smile_vertex_shader3d);
    smile_target_width3d = smile_target_height3d = 0;
    smile_m5_applied_revision3d = 0;
    smile_shadow_applied_revision3d = 0;
    smile_multipass_active3d = 0;
    smile_frame_submission_count3d = 0;
    smile_sample_count3d = 1; smile_sample_quality3d = 0;
    smile_pbr_shader_available3d = 0;
    smile_model_palette_cached_animator3d = 0;
    smile_model_palette_cached_revision3d = 0;
    smile_pbr_pipeline_state3d = SMILE_3D_PBR_PIPELINE_NOT_ATTEMPTED;
    smile_pbr_pipeline_failure3d = 0;
    smile_pbr_pipeline_attempt_count3d = 0;
}

static long long smile_3d_particle_batch_command(long long operation,
    long long b, long long c, long long d, long long e, long long f,
    long long g, long long h, long long i)
{
    SmileParticleBatch3D* batch;
    if (operation == 1)
        return smile_3d_create_particle_batch((unsigned int)b, c, (int)d, (int)e, (int)f);
    batch = smile_3d_particle_batch(b);
    if (operation == 7) return batch != 0 ? 1 : 0;
    if (batch == 0)
    { smile_last_error3d = 54; smile_vfx_rejected_operation_count3d++; return 0; }
    if (operation == 2)
    {
        if (c < 0 || c >= batch->capacity || d < -1000000 || d > 1000000 ||
            e < -1000000 || e > 1000000 || f < -1000000 || f > 1000000 ||
            g <= 0 || g > 1000000 || h < -1000000 || h > 1000000 ||
            i < 0 || i >= (long long)batch->atlas_columns * batch->atlas_rows)
        { smile_last_error3d = 54; smile_vfx_rejected_operation_count3d++; return 0; }
        SmileParticleInstance3D* instance = &batch->instances[c];
        instance->position_size[0] = (float)d;
        instance->position_size[1] = (float)e;
        instance->position_size[2] = (float)f;
        instance->position_size[3] = (float)g;
        instance->rotation_uv[0] = smile_3d_degrees(h);
        instance->rotation_uv[1] = (float)(i % batch->atlas_columns) /
            (float)batch->atlas_columns;
        instance->rotation_uv[2] = (float)(i / batch->atlas_columns) /
            (float)batch->atlas_rows;
        batch->staging_revision++;
        if (batch->staging_revision == 0) batch->staging_revision = 1;
        return 1;
    }
    if (operation == 3)
    {
        if (c < 0 || c >= batch->capacity || d < 0 || d > 255 ||
            e < 0 || e > 255 || f < 0 || f > 255 || g < 0 || g > 100)
        { smile_last_error3d = 54; smile_vfx_rejected_operation_count3d++; return 0; }
        SmileParticleInstance3D* instance = &batch->instances[c];
        instance->color[0] = (float)d / 255.0f;
        instance->color[1] = (float)e / 255.0f;
        instance->color[2] = (float)f / 255.0f;
        instance->color[3] = (float)g / 100.0f;
        batch->staging_revision++;
        if (batch->staging_revision == 0) batch->staging_revision = 1;
        return 1;
    }
    if (operation == 4) return c < 0 ? 0 :
        smile_3d_commit_particle_batch(batch, (unsigned int)c);
    if (operation == 5) return smile_3d_draw_vfx_batch(
        SMILE_3D_SUBMISSION_PARTICLE_BATCH, b);
    if (operation == 6)
    {
        if (batch->in_flight != 0)
        { smile_last_error3d = 56; smile_vfx_rejected_operation_count3d++; return 0; }
        smile_3d_delete_particle_batch(batch);
        return 1;
    }
    smile_last_error3d = 54;
    smile_vfx_rejected_operation_count3d++;
    return 0;
}

static long long smile_3d_ribbon_batch_command(long long operation,
    long long b, long long c, long long d, long long e, long long f,
    long long g, long long h, long long i, long long j)
{
    SmileRibbonBatch3D* batch;
    if (operation == 1) return smile_3d_create_ribbon_batch((unsigned int)b, c);
    batch = smile_3d_ribbon_batch(b);
    if (operation == 7) return batch != 0 ? 1 : 0;
    if (batch == 0)
    { smile_last_error3d = 54; smile_vfx_rejected_operation_count3d++; return 0; }
    if (operation == 2)
    {
        if (c < 0 || c >= batch->capacity || d < -1000000 || d > 1000000 ||
            e < -1000000 || e > 1000000 || f < -1000000 || f > 1000000 ||
            g < -1000000 || g > 1000000 || h < -1000000 || h > 1000000 ||
            i < -1000000 || i > 1000000 || j < 0 || j > 1000)
        { smile_last_error3d = 54; smile_vfx_rejected_operation_count3d++; return 0; }
        SmileRibbonPoint3D* point = &batch->points[c];
        point->left[0] = (float)d; point->left[1] = (float)e; point->left[2] = (float)f;
        point->right[0] = (float)g; point->right[1] = (float)h; point->right[2] = (float)i;
        point->u = (float)j / 1000.0f;
        batch->staging_revision++;
        if (batch->staging_revision == 0) batch->staging_revision = 1;
        return 1;
    }
    if (operation == 3)
    {
        if (c < 0 || c >= batch->capacity || d < 0 || d > 255 ||
            e < 0 || e > 255 || f < 0 || f > 255 || g < 0 || g > 100)
        { smile_last_error3d = 54; smile_vfx_rejected_operation_count3d++; return 0; }
        SmileRibbonPoint3D* point = &batch->points[c];
        point->color[0] = (float)d / 255.0f;
        point->color[1] = (float)e / 255.0f;
        point->color[2] = (float)f / 255.0f;
        point->color[3] = (float)g / 100.0f;
        batch->staging_revision++;
        if (batch->staging_revision == 0) batch->staging_revision = 1;
        return 1;
    }
    if (operation == 4) return c < 0 ? 0 :
        smile_3d_commit_ribbon_batch(batch, (unsigned int)c);
    if (operation == 5) return smile_3d_draw_vfx_batch(
        SMILE_3D_SUBMISSION_RIBBON_BATCH, b);
    if (operation == 6)
    {
        if (batch->in_flight != 0)
        { smile_last_error3d = 56; smile_vfx_rejected_operation_count3d++; return 0; }
        smile_3d_delete_ribbon_batch(batch);
        return 1;
    }
    smile_last_error3d = 54;
    smile_vfx_rejected_operation_count3d++;
    return 0;
}

static long long smile_3d_m6_value(long long query, long long handle)
{
    if (query == 1) return smile_3d_live_particle_batch_count();
    if (query == 2) return SMILE_3D_MAX_PARTICLE_BATCHES;
    if (query == 3) return smile_3d_live_ribbon_batch_count();
    if (query == 4) return SMILE_3D_MAX_RIBBON_BATCHES;
    if (query == 5) return smile_staged_particle_capacity3d;
    if (query == 6) return SMILE_3D_MAX_STAGED_PARTICLES;
    if (query == 7) return smile_staged_ribbon_capacity3d;
    if (query == 8) return SMILE_3D_MAX_STAGED_RIBBON_POINTS;
    if (query == 9)
    {
        long long count = 0;
        for (int index = 0; index < SMILE_3D_MAX_PARTICLE_BATCHES; ++index)
            if (smile_particle_batches3d[index].active) count += smile_particle_batches3d[index].count;
        return count;
    }
    if (query == 10)
    {
        long long count = 0;
        for (int index = 0; index < SMILE_3D_MAX_RIBBON_BATCHES; ++index)
            if (smile_ribbon_batches3d[index].active) count += smile_ribbon_batches3d[index].count;
        return count;
    }
    if (query == 11) return smile_vfx_draw_count3d;
    if (query == 12) return smile_vfx_triangle_count3d;
    if (query == 13) return smile_vfx_upload_count3d;
    if (query == 14) return
        (long long)smile_staged_particle_capacity3d * sizeof(SmileParticleInstance3D) * 2 +
        (long long)smile_staged_ribbon_capacity3d *
            (sizeof(SmileRibbonPoint3D) + sizeof(SmileRibbonVertex3D) * 4);
    if (query == 15) return
        (long long)smile_staged_particle_capacity3d * sizeof(SmileParticleInstance3D) +
        (long long)smile_staged_ribbon_capacity3d * sizeof(SmileRibbonVertex3D) * 2 +
        sizeof(float) * 16 + sizeof(unsigned short) * 6;
    if (query == 16) return smile_vfx_rejected_operation_count3d;
    if (query == 17) return smile_vfx_particle_draw_count3d;
    if (query == 18) return smile_vfx_ribbon_draw_count3d;
    if (query == 19)
    {
        long long count = 0;
        for (int index = 0; index < SMILE_3D_MAX_PARTICLE_BATCHES; ++index)
            if (smile_particle_batches3d[index].active && smile_particle_batches3d[index].in_flight) count++;
        for (int index = 0; index < SMILE_3D_MAX_RIBBON_BATCHES; ++index)
            if (smile_ribbon_batches3d[index].active && smile_ribbon_batches3d[index].in_flight) count++;
        return count;
    }
    if (query == 20) return smile_vfx_particle_submission_count3d;
    if (query == 21) return smile_vfx_ribbon_submission_count3d;
    if (query == 22) return smile_vfx_particle_triangle_count3d;
    if (query == 23) return smile_vfx_ribbon_triangle_count3d;
    if (query >= 30 && query <= 41)
    {
        SmileParticleBatch3D* particle = smile_3d_particle_batch(handle);
        SmileRibbonBatch3D* ribbon = smile_3d_ribbon_batch(handle);
        if (particle != 0)
        {
            if (query == 30) return particle->capacity;
            if (query == 31) return particle->count;
            if (query == 32) return particle->revision;
            if (query == 33)
                return (long long)particle->capacity * sizeof(SmileParticleInstance3D) * 2;
            if (query == 34)
                return (long long)particle->capacity * sizeof(SmileParticleInstance3D);
            if (query == 35) return particle->in_flight;
            if (query == 36) return particle->material_handle;
            if (query == 37) return particle->staging_revision;
            if (query == 38) return particle->uploaded_revision;
            if (query == 39) return particle->in_flight != 0 ? 7 : (particle->revision != 0 ? 3 : 1);
            if (query == 40) return 0;
            if (query == 41) return (long long)particle->count * sizeof(SmileParticleInstance3D);
        }
        if (ribbon != 0)
        {
            if (query == 30) return ribbon->capacity;
            if (query == 31) return ribbon->count;
            if (query == 32) return ribbon->revision;
            if (query == 33) return (long long)ribbon->capacity *
                (sizeof(SmileRibbonPoint3D) + sizeof(SmileRibbonVertex3D) * 4);
            if (query == 34) return (long long)ribbon->capacity *
                sizeof(SmileRibbonVertex3D) * 2;
            if (query == 35) return ribbon->in_flight;
            if (query == 36) return ribbon->material_handle;
            if (query == 37) return ribbon->staging_revision;
            if (query == 38) return ribbon->uploaded_revision;
            if (query == 39) return ribbon->in_flight != 0 ? 7 : (ribbon->revision != 0 ? 3 : 1);
            if (query == 40) return 0;
            if (query == 41) return (long long)ribbon->count * sizeof(SmileRibbonVertex3D) * 2;
        }
    }
    smile_last_error3d = 54;
    return 0;
}

static void smile_3d_reset(void)
{
    int index;
    smile_3d_end();
    smile_material_inspection3d = 0;
    for (index = 0; index < SMILE_3D_MAX_OBJECTS; ++index)
        if (smile_objects3d[index].active)
        {
            smile_objects3d[index].active = 0; smile_objects3d[index].generation++;
            if (smile_objects3d[index].generation == 0) smile_objects3d[index].generation = 1;
        }
    for (index = 0; index < SMILE_3D_MAX_ANIMATORS; ++index)
        if (smile_animators3d[index].active) smile_3d_delete_animator(&smile_animators3d[index]);
    for (index = 0; index < SMILE_3D_MAX_PARTICLE_BATCHES; ++index)
        if (smile_particle_batches3d[index].active)
            smile_3d_delete_particle_batch(&smile_particle_batches3d[index]);
    for (index = 0; index < SMILE_3D_MAX_RIBBON_BATCHES; ++index)
        if (smile_ribbon_batches3d[index].active)
            smile_3d_delete_ribbon_batch(&smile_ribbon_batches3d[index]);
    for (index = 0; index < SMILE_3D_MAX_MODELS; ++index)
        if (smile_models3d[index].active) smile_3d_delete_model(&smile_models3d[index]);
    for (index = 0; index < SMILE_3D_MAX_CLIPS; ++index)
        if (smile_clips3d[index].active) smile_3d_delete_clip(&smile_clips3d[index]);
    for (index = 0; index < SMILE_3D_MAX_SKELETONS; ++index)
        if (smile_skeletons3d[index].active) smile_3d_delete_skeleton(&smile_skeletons3d[index]);
    for (index = 0; index < SMILE_3D_MAX_MATERIALS; ++index)
        if (smile_materials3d[index].active) smile_3d_delete_material(&smile_materials3d[index]);
    for (index = 0; index < SMILE_3D_MAX_TEXTURES; ++index)
        if (smile_textures3d[index].active) smile_3d_delete_texture(&smile_textures3d[index]);
    for (index = 0; index < SMILE_3D_MAX_MESHES; ++index)
        if (smile_meshes3d[index].active) smile_3d_delete_mesh(&smile_meshes3d[index]);
    smile_graphics3d_on_device_lost();
    smile_last_error3d = 0;
    smile_draw_call_count3d = 0;
    smile_submitted_triangle_count3d = 0;
    smile_pbr_draw_count3d = 0;
    smile_simple_draw_count3d = 0;
    smile_pbr_triangle_count3d = 0;
    smile_model_palette_upload_count3d = 0;
    smile_frame_submission_count3d = 0;
    smile_logical_submission_count3d = 0;
    smile_rejected_submission_count3d = 0;
    smile_shadow_draw_count3d = 0;
    smile_shadow_triangle_count3d = 0;
    smile_shadow_palette_upload_count3d = 0;
    smile_post_draw_count3d = 0;
    smile_resolve_count3d = 0;
    smile_vfx_draw_count3d = 0;
    smile_vfx_triangle_count3d = 0;
    smile_vfx_upload_count3d = 0;
    smile_vfx_rejected_operation_count3d = 0;
    smile_vfx_particle_draw_count3d = 0;
    smile_vfx_ribbon_draw_count3d = 0;
    smile_vfx_particle_triangle_count3d = 0;
    smile_vfx_ribbon_triangle_count3d = 0;
    smile_vfx_particle_submission_count3d = 0;
    smile_vfx_ribbon_submission_count3d = 0;
    smile_post_requested3d = 0;
    smile_hdr_requested3d = 0;
    smile_bloom_requested3d = 0;
    smile_shadow_requested3d = 0;
    smile_m5_fallback_flags3d = 0;
    smile_m5_configuration_revision3d++;
    smile_camera_position3d[0] = 0.0f;
    smile_camera_position3d[1] = 300.0f;
    smile_camera_position3d[2] = -800.0f;
    smile_camera_target3d[0] = 0.0f;
    smile_camera_target3d[1] = 0.0f;
    smile_camera_target3d[2] = 0.0f;
    smile_camera_up3d[0] = 0.0f;
    smile_camera_up3d[1] = 1.0f;
    smile_camera_up3d[2] = 0.0f;
    smile_camera_fov3d = 55.0f;
    smile_camera_near3d = 1.0f;
    smile_camera_far3d = 10000.0f;
    smile_3d_clear_pending_camera();
    smile_3d_reset_lights();
    smile_resource_epoch3d++;
    if (smile_resource_epoch3d <= 0 || smile_resource_epoch3d > 2147483647)
        smile_resource_epoch3d = 1;
}

extern "C" long long smile_renderer3d_command(long long command,
    long long a, long long b, long long c, long long d, long long e,
    long long f, long long g, long long h, long long i, long long j)
{
    SmileMesh3D* mesh;
    SmileObject3D* object;
    SmileTexture3D* texture;
    SmileMaterial3D* material;
    SmileModel3D* model;
    SmileSkeleton3D* skeleton;
    SmileAnimationClip3D* clip;
    SmileAnimator3D* animator;
    switch (command)
    {
        case SMILE_3D_AVAILABLE: return smile_graphics_directx_device() != 0 ? 1 : 0;
        case SMILE_3D_RESET: smile_3d_reset(); return 1;
        case SMILE_3D_CREATE_MESH: return smile_3d_create_mesh((unsigned int)a, (unsigned int)b);
        case SMILE_3D_SET_VERTEX:
            mesh = smile_3d_mesh(a); if (mesh == 0 || b < 0 || b >= mesh->vertex_count) { smile_last_error3d = 5; return 0; }
            if (mesh->in_flight != 0) { smile_last_error3d = 53; return 0; }
            smile_3d_vertex(mesh, (unsigned int)b, (float)c, (float)d, (float)e); mesh->committed = 0; return 1;
        case SMILE_3D_SET_TRIANGLE:
            mesh = smile_3d_mesh(a); if (mesh == 0 || b < 0 || b * 3 + 2 >= mesh->index_count || c < 0 || d < 0 || e < 0) { smile_last_error3d = 5; return 0; }
            if (mesh->in_flight != 0) { smile_last_error3d = 53; return 0; }
            smile_3d_triangle(mesh, (unsigned int)b, (unsigned int)c, (unsigned int)d, (unsigned int)e); mesh->committed = 0; return 1;
        case SMILE_3D_COMMIT_MESH:
            mesh = smile_3d_mesh(a);
            if (mesh != 0 && mesh->in_flight != 0) { smile_last_error3d = 53; return 0; }
            return smile_3d_commit_mesh(mesh);
        case SMILE_3D_CREATE_PRIMITIVE: return smile_3d_create_primitive((int)a, (float)b, (float)c, (int)d, (int)e);
        case SMILE_3D_CREATE_OBJECT: return smile_3d_create_object(a);
        case SMILE_3D_DESTROY:
            mesh = smile_3d_mesh(a);
            if (mesh != 0)
            {
                if (smile_3d_mesh_reference_count(a) != 0) { smile_last_error3d = 16; return 0; }
                smile_3d_delete_mesh(mesh); return 1;
            }
            object = smile_3d_object(a); if (object != 0) { object->active = 0; object->generation++; if (object->generation == 0) object->generation = 1; return 1; }
            model = smile_3d_model_resource(a);
            if (model != 0)
            {
                if (!smile_3d_delete_model(model)) { smile_last_error3d = 27; return 0; }
                return 1;
            }
            animator = smile_3d_animator(a);
            if (animator != 0)
            {
                if (smile_3d_animator_reference_count(a) != 0) { smile_last_error3d = 37; return 0; }
                smile_3d_delete_animator(animator); return 1;
            }
            clip = smile_3d_clip(a);
            if (clip != 0)
            {
                if (smile_3d_clip_reference_count(a) != 0) { smile_last_error3d = 37; return 0; }
                smile_3d_delete_clip(clip); return 1;
            }
            skeleton = smile_3d_skeleton(a);
            if (skeleton != 0)
            {
                if (smile_3d_skeleton_reference_count(a) != 0) { smile_last_error3d = 37; return 0; }
                smile_3d_delete_skeleton(skeleton); return 1;
            }
            material = smile_3d_material(a);
            if (material != 0)
            {
                if (material->owner_model_handle != 0 || smile_3d_material_reference_count(a) != 0)
                { smile_last_error3d = 22; return 0; }
                smile_3d_delete_material(material); return 1;
            }
            texture = smile_3d_texture(a);
            if (texture != 0)
            {
                if (smile_3d_texture_reference_count(a) != 0) { smile_last_error3d = 23; return 0; }
                smile_3d_delete_texture(texture); return 1;
            }
            smile_last_error3d = 5; return 0;
        case SMILE_3D_SET_CAMERA:
            if (smile_frame_active3d)
            {
                smile_3d_clear_pending_camera();
                smile_last_error3d = SMILE_3D_CAMERA_ERROR_FRAME_ACTIVE;
                return 0;
            }
            if (!smile_3d_camera_world_value(a) || !smile_3d_camera_world_value(b) ||
                !smile_3d_camera_world_value(c) || !smile_3d_camera_world_value(d) ||
                !smile_3d_camera_world_value(e) || !smile_3d_camera_world_value(f))
            {
                smile_3d_clear_pending_camera();
                smile_last_error3d = SMILE_3D_CAMERA_ERROR_INVALID_POSITION_TARGET;
                return 0;
            }
            if (a == d && b == e && c == f)
            {
                smile_3d_clear_pending_camera();
                smile_last_error3d = SMILE_3D_CAMERA_ERROR_ZERO_VIEW_DIRECTION;
                return 0;
            }
            if (g < 10 || g > 160 || h <= 0 || i <= h || i > 2000000)
            {
                smile_3d_clear_pending_camera();
                smile_last_error3d = SMILE_3D_CAMERA_ERROR_INVALID_PROJECTION;
                return 0;
            }
            smile_pending_camera_position3d[0] = (float)a;
            smile_pending_camera_position3d[1] = (float)b;
            smile_pending_camera_position3d[2] = (float)c;
            smile_pending_camera_target3d[0] = (float)d;
            smile_pending_camera_target3d[1] = (float)e;
            smile_pending_camera_target3d[2] = (float)f;
            smile_pending_camera_fov3d = (float)g;
            smile_pending_camera_near3d = (float)h;
            smile_pending_camera_far3d = (float)i;
            smile_pending_camera_has_projection3d = 1;
            return 1;
        case SMILE_3D_SET_CAMERA_UP:
            if (smile_frame_active3d)
            {
                smile_3d_clear_pending_camera();
                smile_last_error3d = SMILE_3D_CAMERA_ERROR_FRAME_ACTIVE;
                return 0;
            }
            if (!smile_3d_camera_world_value(a) || !smile_3d_camera_world_value(b) ||
                !smile_3d_camera_world_value(c) || (a == 0 && b == 0 && c == 0))
            {
                smile_3d_clear_pending_camera();
                smile_last_error3d = SMILE_3D_CAMERA_ERROR_INVALID_UP;
                return 0;
            }
            smile_pending_camera_up3d[0] = (float)a;
            smile_pending_camera_up3d[1] = (float)b;
            smile_pending_camera_up3d[2] = (float)c;
            smile_pending_camera_has_up3d = 1;
            return 1;
        case SMILE_3D_SET_POSITION:
        case SMILE_3D_SET_ROTATION:
        case SMILE_3D_SET_SCALE:
            object = smile_3d_object(a); if (object == 0) { smile_last_error3d = 5; return 0; }
            if (command == SMILE_3D_SET_POSITION) { object->position[0] = (float)b; object->position[1] = (float)c; object->position[2] = (float)d; }
            else if (command == SMILE_3D_SET_ROTATION) { object->rotation[0] = (float)b; object->rotation[1] = (float)c; object->rotation[2] = (float)d; }
            else { object->scale[0] = (float)b / 100.0f; object->scale[1] = (float)c / 100.0f; object->scale[2] = (float)d / 100.0f; }
            return 1;
        case SMILE_3D_SET_COLOR:
            object = smile_3d_object(a); if (object == 0) { smile_last_error3d = 5; return 0; }
            object->color[0] = (float)(b & 255) / 255.0f; object->color[1] = (float)(c & 255) / 255.0f;
            object->color[2] = (float)(d & 255) / 255.0f; object->color[3] = (float)(e < 0 ? 0 : e > 100 ? 100 : e) / 100.0f; return 1;
        case SMILE_3D_SET_VISIBLE:
            object = smile_3d_object(a); if (object == 0) { smile_last_error3d = 5; return 0; } object->visible = b != 0; return 1;
        case SMILE_3D_BEGIN: return smile_3d_begin(a, b, c);
        case SMILE_3D_DRAW: return smile_3d_draw(a);
        case SMILE_3D_END: return smile_3d_end();
        case SMILE_3D_MESH_VERTEX_COUNT: mesh = smile_3d_mesh(a); return mesh == 0 ? 0 : mesh->vertex_count;
        case SMILE_3D_MESH_INDEX_COUNT: mesh = smile_3d_mesh(a); return mesh == 0 ? 0 : mesh->index_count;
        case SMILE_3D_LAST_ERROR: return smile_last_error3d;
        case SMILE_3D_LIVE_MESH_COUNT: return smile_3d_live_mesh_count();
        case SMILE_3D_LIVE_OBJECT_COUNT: return smile_3d_live_object_count();
        case SMILE_3D_MAX_MESH_COUNT: return SMILE_3D_MAX_MESHES;
        case SMILE_3D_MAX_OBJECT_COUNT: return SMILE_3D_MAX_OBJECTS;
        case SMILE_3D_MESH_VALID: return smile_3d_mesh(a) != 0 ? 1 : 0;
        case SMILE_3D_OBJECT_VALID: return smile_3d_object(a) != 0 ? 1 : 0;
        case SMILE_3D_MESH_REFERENCE_COUNT: return smile_3d_mesh_reference_count(a);
        case SMILE_3D_CREATE_MATERIAL:
            return smile_3d_create_material(a, (int)b, c, d, e, f, (int)g, h, i);
        case SMILE_3D_SET_OBJECT_MATERIAL:
            object = smile_3d_object(a);
            if (object == 0 || (b != 0 && smile_3d_material(b) == 0))
            { smile_last_error3d = 5; return 0; }
            object->material_handle = b == 0 ? object->default_material_handle : b;
            return 1;
        case SMILE_3D_SET_MESH_UV:
            mesh = smile_3d_mesh(a);
            if (mesh == 0 || b < 0 || b >= mesh->vertex_count) { smile_last_error3d = 5; return 0; }
            if (mesh->in_flight != 0) { smile_last_error3d = 53; return 0; }
            smile_3d_uv(mesh, (unsigned int)b, (float)c / 1000.0f, (float)d / 1000.0f);
            mesh->committed = 0; return 1;
        case SMILE_3D_LIVE_TEXTURE_COUNT: return smile_3d_live_texture_count();
        case SMILE_3D_LIVE_MATERIAL_COUNT: return smile_3d_live_material_count();
        case SMILE_3D_MAX_TEXTURE_COUNT: return SMILE_3D_MAX_TEXTURES;
        case SMILE_3D_MAX_MATERIAL_COUNT: return SMILE_3D_MAX_MATERIALS;
        case SMILE_3D_TEXTURE_VALID: return smile_3d_texture(a) != 0 ? 1 : 0;
        case SMILE_3D_MATERIAL_VALID: return smile_3d_material(a) != 0 ? 1 : 0;
        case SMILE_3D_TEXTURE_WIDTH:
            texture = smile_3d_texture(a);
            return texture == 0 ? 0 : smile_image_resource_width(texture->image);
        case SMILE_3D_TEXTURE_HEIGHT:
            texture = smile_3d_texture(a);
            return texture == 0 ? 0 : smile_image_resource_height(texture->image);
        case SMILE_3D_TEXTURE_REFERENCE_COUNT: return smile_3d_texture_reference_count(a);
        case SMILE_3D_MATERIAL_REFERENCE_COUNT: return smile_3d_material_reference_count(a);
        case SMILE_3D_SET_MATERIAL:
            return smile_3d_set_material(smile_3d_material(a), (int)b, c, d, e, f, (int)g, h, i);
        case SMILE_3D_SET_MESH_NORMAL:
            mesh = smile_3d_mesh(a);
            if (mesh == 0 || b < 0 || b >= mesh->vertex_count)
            {
                smile_last_error3d = 5;
                return 0;
            }
            if (mesh->in_flight != 0) { smile_last_error3d = 53; return 0; }
            smile_3d_normal(mesh, (unsigned int)b, (float)c / 1000.0f,
                (float)d / 1000.0f, (float)e / 1000.0f);
            mesh->committed = 0;
            return 1;
        case SMILE_3D_LIVE_MODEL_COUNT: return smile_3d_live_model_count();
        case SMILE_3D_MAX_MODEL_COUNT: return SMILE_3D_MAX_MODELS;
        case SMILE_3D_MODEL_VALID: return smile_3d_model_resource(a) != 0 ? 1 : 0;
        case SMILE_3D_MODEL_PART_COUNT:
            model = smile_3d_model_resource(a);
            return model == 0 ? 0 : model->part_count;
        case SMILE_3D_MODEL_MATERIAL_COUNT:
            model = smile_3d_model_resource(a);
            return model == 0 ? 0 : model->material_count;
        case SMILE_3D_CREATE_MODEL_PART_OBJECT:
            model = smile_3d_model_resource(a);
            if (model == 0 || b < 0 || b >= model->part_count)
            {
                smile_last_error3d = 5;
                return 0;
            }
            if (model->prepared_pbr && model->material_slots[b] < model->prepared_material_count)
                return smile_3d_create_object_with_material(model->mesh_handles[b],
                    model->prepared_material_handles[model->material_slots[b]]);
            return smile_3d_create_object(model->mesh_handles[b]);
        case SMILE_3D_MODEL_PART_MATERIAL:
            model = smile_3d_model_resource(a);
            if (model == 0 || b < 0 || b >= model->part_count)
            {
                smile_last_error3d = 5;
                return -1;
            }
            return model->material_slots[b];
        case SMILE_3D_SET_MESH_SKIN:
            mesh = smile_3d_mesh(a);
            if (mesh != 0 && mesh->in_flight != 0) { smile_last_error3d = 53; return 0; }
            return b < 0 ? 0 : smile_3d_skin(mesh, (unsigned int)b, c, d, e, f, g, h, i, j);
        case SMILE_3D_CREATE_SKELETON: return smile_3d_create_skeleton((int)a);
        case SMILE_3D_SET_SKELETON_BONE:
            skeleton = smile_3d_skeleton(a);
            if (skeleton == 0 || b < 0 || b >= skeleton->bone_count || c < -1 || c >= b)
            {
                smile_last_error3d = 30; return 0;
            }
            skeleton->parents[b] = (signed char)c;
            skeleton->bind_translation[b][0] = (float)d;
            skeleton->bind_translation[b][1] = (float)e;
            skeleton->bind_translation[b][2] = (float)f;
            skeleton->committed = 0; return 1;
        case SMILE_3D_COMMIT_SKELETON: return smile_3d_commit_skeleton(smile_3d_skeleton(a));
        case SMILE_3D_CREATE_CLIP: return b < 0 ? 0 : smile_3d_create_clip(a, (unsigned int)b);
        case SMILE_3D_SET_CLIP_TRANSLATION:
            clip = smile_3d_clip(a); skeleton = clip == 0 ? 0 : smile_3d_skeleton(clip->skeleton_handle);
            if (clip == 0 || skeleton == 0 || b < 0 || b >= skeleton->bone_count) { smile_last_error3d = 31; return 0; }
            clip->translation_tracks[b] = 1;
            clip->translation[b][0][0]=(float)c;clip->translation[b][0][1]=(float)d;clip->translation[b][0][2]=(float)e;
            clip->translation[b][1][0]=(float)f;clip->translation[b][1][1]=(float)g;clip->translation[b][1][2]=(float)h;return 1;
        case SMILE_3D_SET_CLIP_ROTATION:
            clip = smile_3d_clip(a); skeleton = clip == 0 ? 0 : smile_3d_skeleton(clip->skeleton_handle);
            if (clip == 0 || skeleton == 0 || b < 0 || b >= skeleton->bone_count) { smile_last_error3d = 31; return 0; }
            clip->rotation_tracks[b] = 1;
            clip->rotation[b][0][0]=(float)c/1000;clip->rotation[b][0][1]=(float)d/1000;clip->rotation[b][0][2]=(float)e/1000;clip->rotation[b][0][3]=(float)f/1000;
            clip->rotation[b][1][0]=(float)g/1000;clip->rotation[b][1][1]=(float)h/1000;clip->rotation[b][1][2]=(float)i/1000;clip->rotation[b][1][3]=(float)j/1000;return 1;
        case SMILE_3D_SET_CLIP_SCALE:
            clip = smile_3d_clip(a); skeleton = clip == 0 ? 0 : smile_3d_skeleton(clip->skeleton_handle);
            if (clip == 0 || skeleton == 0 || b < 0 || b >= skeleton->bone_count ||
                c <= 0 || d <= 0 || e <= 0 || f <= 0 || g <= 0 || h <= 0) { smile_last_error3d = 31; return 0; }
            clip->scale_tracks[b] = 1;
            clip->scale[b][0][0]=(float)c/100;clip->scale[b][0][1]=(float)d/100;clip->scale[b][0][2]=(float)e/100;
            clip->scale[b][1][0]=(float)f/100;clip->scale[b][1][1]=(float)g/100;clip->scale[b][1][2]=(float)h/100;
            smile_3d_update_clip_pbr_scale_safety(clip);return 1;
        case SMILE_3D_ADD_CLIP_EVENT:
            clip = smile_3d_clip(a);
            if (clip == 0 || b <= 0 || b > clip->duration_ms || c <= 0 ||
                clip->event_count >= SMILE_3D_MAX_ANIMATION_EVENTS ||
                (clip->event_count > 0 && b < clip->event_time[clip->event_count-1])) { smile_last_error3d = 31; return 0; }
            clip->event_time[clip->event_count]=(unsigned int)b;clip->event_id[clip->event_count]=(unsigned int)c;clip->event_count++;return 1;
        case SMILE_3D_CREATE_ANIMATOR: return smile_3d_create_animator(a);
        case SMILE_3D_PLAY_ANIMATOR:
            animator=smile_3d_animator(a);clip=smile_3d_clip(b);
            if(animator==0||clip==0||clip->skeleton_handle!=animator->skeleton_handle||d<=0||d>1000){smile_last_error3d=35;return 0;}
            animator->clip_handle=b;animator->loop=c!=0;animator->complete=0;animator->time_ms=0;
            animator->previous_time_ms=0;animator->speed_percent=(unsigned int)d;animator->pending_event=0;
            smile_3d_update_animation_pose(animator);return 1;
        case SMILE_3D_UPDATE_ANIMATOR:
            animator = smile_3d_animator(a);
            return b < 0 ? 0 : (animator != 0 && animator->model_animation
                ? smile_3d_update_model_animator(animator, (unsigned int)b)
                : smile_3d_update_animator(animator, (unsigned int)b));
        case SMILE_3D_ANIMATOR_COMPLETE: animator=smile_3d_animator(a);return animator==0?0:animator->complete;
        case SMILE_3D_ANIMATOR_TIME: animator=smile_3d_animator(a);return animator==0?0:animator->time_ms;
        case SMILE_3D_TAKE_ANIMATOR_EVENT:
            animator=smile_3d_animator(a);if(animator==0)return 0;
            if(animator->model_animation)return smile_3d_take_model_event(animator,0);
            {unsigned int value=animator->pending_event;animator->pending_event=0;return value;}
        case SMILE_3D_SET_OBJECT_ANIMATOR:
            object=smile_3d_object(a);animator=b==0?0:smile_3d_animator(b);mesh=object==0?0:smile_3d_mesh(object->mesh_handle);
            skeleton=animator==0||animator->model_animation?0:smile_3d_skeleton(animator->skeleton_handle);
            if(object==0||mesh==0||(b!=0&&(animator==0||
                (animator->model_animation?!smile_3d_model_owns_mesh(smile_3d_model_resource(animator->model_handle),mesh):
                 (skeleton==0||mesh->max_joint>=skeleton->bone_count))))){smile_last_error3d=36;return 0;}
            object->animator_handle=b;return 1;
        case SMILE_3D_LIVE_SKELETON_COUNT:return smile_3d_live_skeleton_count();
        case SMILE_3D_LIVE_CLIP_COUNT:return smile_3d_live_clip_count();
        case SMILE_3D_LIVE_ANIMATOR_COUNT:return smile_3d_live_animator_count();
        case SMILE_3D_MAX_BONE_COUNT:return SMILE_3D_MAX_BONES;
        case SMILE_3D_SKELETON_VALID:return smile_3d_skeleton(a)!=0;
        case SMILE_3D_CLIP_VALID:return smile_3d_clip(a)!=0;
        case SMILE_3D_ANIMATOR_VALID:return smile_3d_animator(a)!=0;
        case SMILE_3D_STOP_ANIMATOR:
            animator=smile_3d_animator(a);if(animator==0)return 0;
            if(animator->model_animation){animator->clip_index=-1;animator->destination_clip=-1;
                animator->time_ms=animator->previous_time_ms=animator->destination_time_ms=0;
                animator->time_remainder=animator->destination_time_remainder=0;
                animator->fade_elapsed_ms=animator->fade_duration_ms=0;
                animator->complete=animator->destination_complete=0;
                animator->playback_mode=animator->destination_mode=0;
                smile_3d_clear_model_events(animator);ZeroMemory(animator->root_delta,sizeof(animator->root_delta));
                smile_3d_update_model_pose(animator);return 1;}
            animator->clip_handle=0;animator->time_ms=0;animator->previous_time_ms=0;animator->complete=0;
            animator->pending_event=0;smile_3d_update_animation_pose(animator);return 1;
        case SMILE_3D_MAX_SKELETON_COUNT:return SMILE_3D_MAX_SKELETONS;
        case SMILE_3D_MAX_CLIP_COUNT:return SMILE_3D_MAX_CLIPS;
        case SMILE_3D_MAX_ANIMATOR_COUNT:return SMILE_3D_MAX_ANIMATORS;
        case SMILE_3D_DRAW_CALL_COUNT:return smile_draw_call_count3d;
        case SMILE_3D_SUBMITTED_TRIANGLE_COUNT:return smile_submitted_triangle_count3d;
        case SMILE_3D_MODEL_STATIC_VALUE:
            return smile_3d_model_static_value(smile_3d_model_resource(a), b, c, d);
        case SMILE_3D_CREATE_PBR_MATERIAL:
            if (!smile_3d_create_pipeline() || !smile_pbr_shader_available3d)
            { smile_last_error3d = 44; return 0; }
            return smile_3d_create_pbr_material(a, b, c, d, (int)e, (int)f, 0);
        case SMILE_3D_SET_PBR_FACTORS:
            return smile_3d_set_pbr_factors(smile_3d_material(a), b, c, d, e, f, g, h, i, j);
        case SMILE_3D_SET_PBR_EMISSIVE:
            return smile_3d_set_pbr_emissive(smile_3d_material(a), b, c, d);
        case SMILE_3D_SET_PBR_TEXTURES:
            return smile_3d_set_pbr_textures(smile_3d_material(a), b, c, d, e, (int)f, (int)g);
        case SMILE_3D_RESET_LIGHTS: smile_3d_reset_lights(); return 1;
        case SMILE_3D_SET_AMBIENT_LIGHT: return smile_3d_set_ambient(a, b, c, d);
        case SMILE_3D_SET_DIRECTIONAL_LIGHT:
            return smile_3d_set_directional(a, b, c, d, e, f, g);
        case SMILE_3D_SET_LOCAL_LIGHT:
            return smile_3d_set_local_light(a, b, c, d, e, f, g, h, i, j);
        case SMILE_3D_SET_SPOT_CONE: return smile_3d_set_spot_cone(a, b, c, d, e, f);
        case SMILE_3D_PBR_TEXTURE_VALUE: return smile_3d_pbr_texture_value(texture = smile_3d_texture(a), b);
        case SMILE_3D_PBR_MATERIAL_VALUE: return smile_3d_pbr_material_value(material = smile_3d_material(a), b);
        case SMILE_3D_LIGHT_VALUE: return smile_3d_light_value(a, b, c);
        case SMILE_3D_PBR_DRAW_COUNT: return smile_pbr_draw_count3d;
        case SMILE_3D_SIMPLE_DRAW_COUNT: return smile_simple_draw_count3d;
        case SMILE_3D_PBR_TRIANGLE_COUNT: return smile_pbr_triangle_count3d;
        case SMILE_3D_PBR_SHADER_AVAILABLE:
            if (a == 1) return smile_pbr_pipeline_state3d;
            if (a == 2) return smile_pbr_pipeline_failure3d;
            if (a == 3) return smile_pbr_pipeline_attempt_count3d;
            return smile_3d_create_pipeline() && smile_pbr_shader_available3d ? 1 : 0;
        case SMILE_3D_MODEL_PBR_VALUE:
            return smile_3d_model_pbr_value(smile_3d_model_resource(a), b, c);
        case SMILE_3D_MODEL_ANIMATION_VALUE:
            return smile_3d_model_animation_value(smile_3d_model_resource(a), b, c);
        case SMILE_3D_CREATE_MODEL_ANIMATOR:
            return smile_3d_create_model_animator(a);
        case SMILE_3D_PLAY_MODEL_ANIMATOR:
            return smile_3d_play_model_animator(smile_3d_animator(a), (int)b, (int)c,
                d < 0 ? 0U : (unsigned int)d);
        case SMILE_3D_CROSSFADE_MODEL_ANIMATOR:
            return c < 0 ? 0 : smile_3d_crossfade_model_animator(smile_3d_animator(a),
                (int)b, (unsigned int)c, (int)d);
        case SMILE_3D_ANIMATOR_CLIP_INDEX:
            animator = smile_3d_animator(a);
            return animator == 0 || !animator->model_animation ? -1 : animator->clip_index;
        case SMILE_3D_ANIMATOR_FADE_PERCENT:
            animator = smile_3d_animator(a);
            return animator == 0 || !animator->model_animation || animator->destination_clip < 0 ? 0 :
                (long long)((unsigned long long)animator->fade_elapsed_ms * 100U / animator->fade_duration_ms);
        case SMILE_3D_ANIMATOR_PENDING_EVENT_COUNT:
            animator = smile_3d_animator(a);
            return animator == 0 || !animator->model_animation ? 0 : animator->event_count;
        case SMILE_3D_SET_ANIMATOR_ROOT_MOTION:
            animator = smile_3d_animator(a);
            if (animator == 0 || !animator->model_animation || b < 0 || b > 1)
            { smile_last_error3d = 48; return 0; }
            animator->root_motion_mode = (unsigned char)b;
            ZeroMemory(animator->root_delta, sizeof(animator->root_delta));
            smile_3d_update_model_pose(animator);
            return 1;
        case SMILE_3D_TAKE_ANIMATOR_ROOT_DELTA:
            animator = smile_3d_animator(a);
            if (animator == 0 || !animator->model_animation || b < 1 || b > 4)
            { smile_last_error3d = 48; return 0; }
            { long long value = (long long)llroundf(animator->root_delta[b - 1] * 1000.0f);
              if (b == 4) ZeroMemory(animator->root_delta, sizeof(animator->root_delta));
              return value; }
        case SMILE_3D_ANIMATOR_SOCKET_VALUE:
            return smile_3d_model_socket_value(smile_3d_animator(a), b, c, d);
        case SMILE_3D_MODEL_ANIMATION_AVAILABLE:
            return smile_3d_create_pipeline() && smile_model_palette_buffer3d != 0 ? 1 : 0;
        case SMILE_3D_MODEL_PALETTE_UPLOAD_COUNT:
            return smile_model_palette_upload_count3d;
        case SMILE_3D_ANIMATOR_PRODUCTION_VALUE:
            return smile_3d_animator_production_value(smile_3d_animator(a), b);
        case SMILE_3D_CLEAR_ANIMATOR_EVENTS:
            animator = smile_3d_animator(a);
            if (animator == 0 || !animator->model_animation)
            { smile_last_error3d = 48; return 0; }
            smile_3d_clear_model_events(animator);
            return 1;
        case SMILE_3D_RENDERER_STATE:
            if (a == 1) return smile_resource_epoch3d;
            if (a == 2) return smile_frame_active3d ? 1 : 0;
            smile_last_error3d = 5;
            return 0;
        case SMILE_3D_CONFIGURE_POST:
            if (smile_frame_active3d || a < 0 || a > 1 || b < 0 || b > 1 ||
                c < 0 || c > 1 || d < 25 || d > 400 || e < 500 || e > 8000 ||
                f < 0 || f > 400 || (g != 2 && g != 4) || h < 0 || h > 2 ||
                (i != 1 && i != 2 && i != 4))
            { smile_last_error3d = 50; return 0; }
            if (smile_post_requested3d == (a != 0) &&
                smile_hdr_requested3d == (b != 0) &&
                smile_bloom_requested3d == (c != 0) &&
                smile_exposure_percent3d == d && smile_bloom_threshold3d == e &&
                smile_bloom_intensity3d == f && smile_bloom_downsample3d == g &&
                smile_bloom_cycles3d == h && smile_requested_sample_count3d == i)
                return 1;
            smile_post_requested3d = a != 0;
            smile_hdr_requested3d = b != 0;
            smile_bloom_requested3d = c != 0;
            smile_exposure_percent3d = (int)d;
            smile_bloom_threshold3d = (int)e;
            smile_bloom_intensity3d = (int)f;
            smile_bloom_downsample3d = (int)g;
            smile_bloom_cycles3d = (int)h;
            smile_requested_sample_count3d = (int)i;
            smile_m5_configuration_revision3d++;
            if (smile_m5_configuration_revision3d <= 0 ||
                smile_m5_configuration_revision3d > 2147483647)
                smile_m5_configuration_revision3d = 1;
            return 1;
        case SMILE_3D_CONFIGURE_SHADOW:
            if (smile_frame_active3d || a < 0 || a > 1 || b < 0 || b > 2 ||
                c < 0 || c >= SMILE_3D_MAX_LOCAL_LIGHTS ||
                (d != 1024 && d != 2048) || e < 0 || e > 1000 || f < 0 || f > 1000 ||
                (a != 0 && b == 0))
            { smile_last_error3d = 50; return 0; }
            if (smile_shadow_requested3d == (a != 0) && smile_shadow_caster3d == b &&
                smile_shadow_slot3d == c && smile_shadow_requested_resolution3d == d &&
                (long long)llroundf(smile_shadow_bias3d * 1000000.0f) == e &&
                (long long)llroundf(smile_shadow_normal_bias3d * 100000.0f) == f)
                return 1;
            smile_shadow_requested3d = a != 0;
            smile_shadow_caster3d = (int)b;
            smile_shadow_slot3d = (int)c;
            smile_shadow_requested_resolution3d = (int)d;
            smile_shadow_bias3d = (float)e / 1000000.0f;
            smile_shadow_normal_bias3d = (float)f / 100000.0f;
            smile_m5_configuration_revision3d++;
            if (smile_m5_configuration_revision3d <= 0 ||
                smile_m5_configuration_revision3d > 2147483647)
                smile_m5_configuration_revision3d = 1;
            return 1;
        case SMILE_3D_SET_SHADOW_AREA:
            if (smile_frame_active3d || a < -1000000 || a > 1000000 ||
                b < -1000000 || b > 1000000 || c < -1000000 || c > 1000000 ||
                d <= 0 || d > 2000000 || e <= 0 || e > 2000000 ||
                f <= 0 || g <= f || g > 2000000)
            { smile_last_error3d = 50; return 0; }
            if (smile_shadow_center3d[0] == (float)a &&
                smile_shadow_center3d[1] == (float)b &&
                smile_shadow_center3d[2] == (float)c && smile_shadow_width3d == (float)d &&
                smile_shadow_height3d == (float)e && smile_shadow_near3d == (float)f &&
                smile_shadow_far3d == (float)g)
                return 1;
            smile_shadow_center3d[0] = (float)a;
            smile_shadow_center3d[1] = (float)b;
            smile_shadow_center3d[2] = (float)c;
            smile_shadow_width3d = (float)d;
            smile_shadow_height3d = (float)e;
            smile_shadow_near3d = (float)f;
            smile_shadow_far3d = (float)g;
            smile_m5_configuration_revision3d++;
            if (smile_m5_configuration_revision3d <= 0 ||
                smile_m5_configuration_revision3d > 2147483647)
                smile_m5_configuration_revision3d = 1;
            return 1;
        case SMILE_3D_SET_OBJECT_SHADOWS:
            object = smile_3d_object(a);
            if (object == 0 || b < 0 || b > 1 || c < 0 || c > 1)
            { smile_last_error3d = 50; return 0; }
            object->casts_shadow = b != 0;
            object->receives_shadow = c != 0;
            return 1;
        case SMILE_3D_M5_VALUE:
            if (a == 1) return smile_logical_submission_count3d;
            if (a == 2) return SMILE_3D_MAX_FRAME_SUBMISSIONS;
            if (a == 3) return smile_multipass_active3d;
            if (a == 4) return smile_shadow_requested3d;
            if (a == 5) return smile_shadow_effective3d;
            if (a == 6) return smile_shadow_resolution3d;
            if (a == 7) return smile_shadow_draw_count3d;
            if (a == 8) return smile_shadow_triangle_count3d;
            if (a == 9) return smile_shadow_palette_upload_count3d;
            if (a == 10) return smile_hdr_requested3d;
            if (a == 11) return smile_hdr_effective3d;
            if (a == 12) return smile_hdr_effective3d ? 1 : 0;
            if (a == 13) return smile_sample_count3d;
            if (a == 14) return smile_m5_target_width3d;
            if (a == 15) return smile_m5_target_height3d;
            if (a == 16) return smile_resolve_count3d;
            if (a == 17) return smile_bloom_requested3d;
            if (a == 18) return smile_bloom_effective3d;
            if (a == 19) return smile_bloom_width3d;
            if (a == 20) return smile_bloom_height3d;
            if (a == 21) return smile_bloom_effective3d ? smile_bloom_cycles3d : 0;
            if (a == 22) return smile_post_draw_count3d;
            if (a == 23) return smile_tone_mapping_effective3d;
            if (a == 24) return smile_exposure_percent3d;
            if (a == 25) return smile_m5_fallback_flags3d;
            if (a == 26) return smile_m5_resource_generation3d;
            if (a == 27) return smile_m5_target_bytes3d;
            if (a == 28) return smile_shadow_caster3d;
            if (a == 29) return smile_shadow_slot3d;
            if (a == 30) return (long long)llroundf(smile_shadow_bias3d * 1000000.0f);
            if (a == 31) return (long long)llroundf(smile_shadow_normal_bias3d * 100000.0f);
            if (a == 32) return smile_post_requested3d;
            if (a == 33) return smile_post_effective3d;
            if (a == 34) return smile_rejected_submission_count3d;
            if (a == 35) return smile_shadow_bytes3d;
            if (a == 36) return smile_scene_bytes3d;
            if (a == 37) return smile_bloom_bytes3d;
            if (a == 42) return smile_physical_submission_count3d;
            if (a == 43) return smile_submission_group_physical3d;
            if (a == 44) return smile_submission_group_reserved3d;
            if (a == 45) return smile_frame_palette_count3d;
            if (a == 46)
            {
                long long count = 0;
                for (int index = 0; index < SMILE_3D_MAX_MESHES; ++index)
                    count += smile_meshes3d[index].in_flight;
                return count;
            }
            if (a == 47)
            {
                long long count = 0;
                for (int index = 0; index < SMILE_3D_MAX_TEXTURES; ++index)
                    count += smile_textures3d[index].in_flight;
                return count;
            }
            if (a == 48) return
                (long long)smile_frame_submission_count3d * SMILE_3D_SUBMISSION_SNAPSHOT_BYTES +
                (long long)smile_frame_palette_count3d * SMILE_3D_PALETTE_SNAPSHOT_BYTES;
            if (a == 49) return SMILE_3D_MAX_FRAME_PALETTES;
            if (a == 50) return smile_submission_group_active3d;
            if (a == 51) return smile_submission_group_logical3d;
            if (a >= 60 && a <= 69)
            {
                if (b < 0 || b >= smile_frame_submission_count3d)
                { smile_last_error3d = 50; return 0; }
                const SmileSubmission3D* submission = &smile_frame_submissions3d[b];
                if (a == 60) return submission->source_handle;
                if (a == 61) return submission->mesh_handle;
                if (a == 62) return (long long)llroundf(submission->object.position[0] * 1000.0f);
                if (a == 63) return (long long)llroundf(submission->object.color[0] * 1000.0f);
                if (a == 64) return (long long)llroundf(submission->object.color[3] * 1000.0f);
                if (a == 65) return submission->has_material ? submission->material.mode : -1;
                if (a == 66) return submission->double_sided;
                if (a == 67) return submission->palette_index + 1;
                if (a == 68) return submission->palette_index < 0 ? 0 :
                    smile_frame_palettes3d[submission->palette_index].pose_revision;
                if (a == 69) return submission->casts_shadow;
            }
            if (a == 40 || a == 41)
            {
                object = smile_3d_object(b);
                if (object == 0) { smile_last_error3d = 50; return 0; }
                return a == 40 ? object->casts_shadow : object->receives_shadow;
            }
            smile_last_error3d = 50;
            return 0;
        case SMILE_3D_SUBMISSION_GROUP:
            return smile_3d_submission_group(a, b);
        case SMILE_3D_PARTICLE_BATCH:
            return smile_3d_particle_batch_command(a, b, c, d, e, f, g, h, i);
        case SMILE_3D_RIBBON_BATCH:
            return smile_3d_ribbon_batch_command(a, b, c, d, e, f, g, h, i, j);
        case SMILE_3D_M6_VALUE:
            return smile_3d_m6_value(a, b);
        case SMILE_3D_MATERIAL_INSPECTION:
            if (a == -1) return smile_material_inspection3d;
            if (smile_frame_active3d || a < 0 || a > 6)
            { smile_last_error3d = 5; return 0; }
            smile_material_inspection3d = (int)a;
            return 1;
        default: smile_last_error3d = 1; return 0;
    }
}

extern "C" long long smile_renderer3d_image_command(long long command, void* image,
    long long a, long long b, long long c, long long d,
    long long e, long long f, long long g, long long h)
{
    (void)e; (void)f; (void)g; (void)h;
    if (command == SMILE_3D_IMAGE_CREATE_TEXTURE)
        return smile_3d_create_texture((SmileImageResource*)image, (int)a, (int)b);
    if (command == SMILE_3D_IMAGE_CREATE_PBR_TEXTURE)
        return smile_3d_create_pbr_texture((SmileImageResource*)image, (int)a, (int)b,
            (int)c, (int)d);
    smile_image_resource_release((SmileImageResource*)image);
    smile_last_error3d = 1;
    return 0;
}

extern "C" long long smile_renderer3d_model_text_operation(long long command,
    const char* text, long long length, long long a, long long b, long long c)
{
    char name[1025];
    if (text == 0 || length <= 0 || length > 1024) { smile_last_error3d = 48; return 0; }
    memcpy(name, text, (size_t)length);
    name[length] = 0;
    if (command == SMILE_3D_TEXT_MODEL_CLIP_INDEX)
    {
        int result = smile_3d_model_clip_index(smile_3d_model_resource(a), name);
        if (result < 0) smile_last_error3d = 48;
        return result;
    }
    if (command == SMILE_3D_TEXT_MODEL_SOCKET_INDEX)
    {
        int result = smile_3d_model_socket_index(smile_3d_model_resource(a), name);
        if (result < 0) smile_last_error3d = 48;
        return result;
    }
    if (command == SMILE_3D_TEXT_MODEL_EVENT_NAME_MATCHES)
    {
        SmileModel3D* model = smile_3d_model_resource(a);
        if (model == 0 || b <= 0 || b > model->animation_event_count) return 0;
        const unsigned char* record = smile_3d_model_animation_record(model, 6, (unsigned int)b - 1);
        return strcmp(model->strings + smile_3d_read_u32(record + 8), name) == 0;
    }
    if (command == SMILE_3D_TEXT_PLAY_MODEL_ANIMATOR)
    {
        SmileAnimator3D* animator = smile_3d_animator(a);
        SmileModel3D* model = animator == 0 ? 0 : smile_3d_model_resource(animator->model_handle);
        int clip = smile_3d_model_clip_index(model, name);
        return smile_3d_play_model_animator(animator, clip, (int)b, c < 0 ? 0U : (unsigned int)c);
    }
    if (command == SMILE_3D_TEXT_CROSSFADE_MODEL_ANIMATOR)
    {
        SmileAnimator3D* animator = smile_3d_animator(a);
        SmileModel3D* model = animator == 0 ? 0 : smile_3d_model_resource(animator->model_handle);
        int clip = smile_3d_model_clip_index(model, name);
        return b < 0 ? 0 : smile_3d_crossfade_model_animator(animator, clip, (unsigned int)b, (int)c);
    }
    if (command == SMILE_3D_TEXT_TAKE_MODEL_ANIMATOR_EVENT)
        return smile_3d_take_model_event(smile_3d_animator(a), name);
    smile_last_error3d = 1;
    return 0;
}
