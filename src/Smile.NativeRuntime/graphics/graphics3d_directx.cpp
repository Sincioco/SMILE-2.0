#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <d3d11.h>
#include <d3dcompiler.h>
#include <math.h>
#include <stdint.h>
#include "graphics3d.h"
#include "graphics_common.h"
#include "graphics_directx.h"
#include "image_resource.h"

#define SMILE_3D_MAX_MESHES 128
#define SMILE_3D_MAX_OBJECTS 256
#define SMILE_3D_MAX_TEXTURES 128
#define SMILE_3D_MAX_MATERIALS 128
#define SMILE_3D_MAX_MODELS 64
#define SMILE_3D_MAX_MODEL_PARTS 16
#define SMILE_3D_MAX_MODEL_BYTES (16 * 1024 * 1024)
#define SMILE_3D_MAX_SKELETONS 64
#define SMILE_3D_MAX_CLIPS 128
#define SMILE_3D_MAX_ANIMATORS 128
#define SMILE_3D_MAX_BONES 32
#define SMILE_3D_MAX_ANIMATION_EVENTS 16
#define SMILE_3D_MESH_HANDLE 0x10000000LL
#define SMILE_3D_OBJECT_HANDLE 0x20000000LL
#define SMILE_3D_TEXTURE_HANDLE 0x30000000LL
#define SMILE_3D_MATERIAL_HANDLE 0x40000000LL
#define SMILE_3D_MODEL_HANDLE 0x50000000LL
#define SMILE_3D_SKELETON_HANDLE 0x60000000LL
#define SMILE_3D_CLIP_HANDLE 0x70000000LL
#define SMILE_3D_ANIMATOR_HANDLE 0x80000000LL
#define SMILE_3D_HANDLE_KIND 0xF0000000LL
#define SMILE_3D_PI 3.14159265358979323846f

struct SmileVertex3D
{
    float x, y, z;
    float nx, ny, nz;
    float u, v;
    float joints[4];
    float weights[4];
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
};

struct SmileObject3D
{
    unsigned short generation;
    unsigned char active;
    unsigned char visible;
    long long mesh_handle;
    long long material_handle;
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
    SmileImageResource* image;
    ID3D11Texture2D* texture;
    ID3D11ShaderResourceView* view;
    ID3D11SamplerState* sampler;
};

struct SmileMaterial3D
{
    unsigned short generation;
    unsigned char active;
    unsigned char alpha_mode;
    unsigned char unlit;
    long long texture_handle;
    float color[4];
    float emissive;
    float cutoff;
};

struct SmileModel3D
{
    unsigned short generation;
    unsigned char active;
    unsigned char part_count;
    unsigned short material_count;
    long long mesh_handles[SMILE_3D_MAX_MODEL_PARTS];
    unsigned short material_slots[SMILE_3D_MAX_MODEL_PARTS];
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
    SmileMatrix3D bones[SMILE_3D_MAX_BONES];
};

struct SmileConstants3D
{
    SmileMatrix3D model;
    SmileMatrix3D mvp;
    float color[4];
    float material[4];
    float animation[4];
    SmileMatrix3D bones[SMILE_3D_MAX_BONES];
};

static SmileMesh3D smile_meshes3d[SMILE_3D_MAX_MESHES];
static SmileObject3D smile_objects3d[SMILE_3D_MAX_OBJECTS];
static SmileTexture3D smile_textures3d[SMILE_3D_MAX_TEXTURES];
static SmileMaterial3D smile_materials3d[SMILE_3D_MAX_MATERIALS];
static SmileModel3D smile_models3d[SMILE_3D_MAX_MODELS];
static SmileSkeleton3D smile_skeletons3d[SMILE_3D_MAX_SKELETONS];
static SmileAnimationClip3D smile_clips3d[SMILE_3D_MAX_CLIPS];
static SmileAnimator3D smile_animators3d[SMILE_3D_MAX_ANIMATORS];
static ID3D11VertexShader* smile_vertex_shader3d;
static ID3D11PixelShader* smile_pixel_shader3d;
static ID3D11InputLayout* smile_input_layout3d;
static ID3D11Buffer* smile_constant_buffer3d;
static ID3D11DepthStencilState* smile_depth_state3d;
static ID3D11DepthStencilState* smile_depth_read_state3d;
static ID3D11RasterizerState* smile_raster_state3d;
static ID3D11BlendState* smile_blend_state3d;
static ID3D11BlendState* smile_additive_blend_state3d;
static ID3D11Texture2D* smile_depth_texture3d;
static ID3D11DepthStencilView* smile_depth_view3d;
static int smile_depth_width3d;
static int smile_depth_height3d;
static int smile_frame_active3d;
static int smile_last_error3d;
static float smile_camera_position3d[3] = { 0.0f, 300.0f, -800.0f };
static float smile_camera_target3d[3] = { 0.0f, 0.0f, 0.0f };
static float smile_camera_fov3d = 55.0f;
static float smile_camera_near3d = 1.0f;
static float smile_camera_far3d = 10000.0f;

static SmileMatrix3D smile_3d_identity(void);

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
    slot = (int)(handle & 255LL) - 1;
    generation = (unsigned short)((handle >> 8) & 65535LL);
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

static int smile_3d_mesh_reference_count(long long mesh_handle)
{
    int count = 0;
    int index;
    if (smile_3d_mesh(mesh_handle) == 0) return 0;
    for (index = 0; index < SMILE_3D_MAX_OBJECTS; ++index)
        if (smile_objects3d[index].active && smile_objects3d[index].mesh_handle == mesh_handle) count++;
    return count;
}

static int smile_3d_texture_reference_count(long long texture_handle)
{
    int count = 0;
    int index;
    if (smile_3d_texture(texture_handle) == 0) return 0;
    for (index = 0; index < SMILE_3D_MAX_MATERIALS; ++index)
        if (smile_materials3d[index].active && smile_materials3d[index].texture_handle == texture_handle) count++;
    return count;
}

static int smile_3d_material_reference_count(long long material_handle)
{
    int count = 0;
    int index;
    if (smile_3d_material(material_handle) == 0) return 0;
    for (index = 0; index < SMILE_3D_MAX_OBJECTS; ++index)
        if (smile_objects3d[index].active && smile_objects3d[index].material_handle == material_handle) count++;
    return count;
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
    mesh->generation++;
    if (mesh->generation == 0) mesh->generation = 1;
}

static int smile_3d_delete_model(SmileModel3D* model)
{
    int index;
    if (model == 0) return 0;
    for (index = 0; index < model->part_count; ++index)
        if (smile_3d_mesh_reference_count(model->mesh_handles[index]) != 0) return 0;
    for (index = 0; index < model->part_count; ++index)
    {
        SmileMesh3D* mesh = smile_3d_mesh(model->mesh_handles[index]);
        if (mesh != 0) smile_3d_delete_mesh(mesh);
        model->mesh_handles[index] = 0;
        model->material_slots[index] = 0;
    }
    model->active = 0;
    model->part_count = 0;
    model->material_count = 0;
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
    clip->skeleton_handle = skeleton_handle;
    clip->duration_ms = duration_ms;
    return smile_3d_handle(SMILE_3D_CLIP_HANDLE, slot, clip->generation);
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
    texture->generation++;
    if (texture->generation == 0) texture->generation = 1;
}

static void smile_3d_delete_material(SmileMaterial3D* material)
{
    material->active = 0;
    material->texture_handle = 0;
    material->generation++;
    if (material->generation == 0) material->generation = 1;
}

static long long smile_3d_create_texture(SmileImageResource* image, int filter, int wrap)
{
    int slot;
    SmileTexture3D* texture;
    long long width = smile_image_resource_width(image);
    long long height = smile_image_resource_height(image);
    if (image == 0 || smile_image_resource_pixels(image) == 0 || width <= 0 || height <= 0 ||
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
    texture->filter = (unsigned char)filter;
    texture->wrap = (unsigned char)wrap;
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
    if (material == 0 || alpha_mode < 0 || alpha_mode > 3 || opacity < 0 || opacity > 100 ||
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
    material->active = 1;
    material->texture_handle = texture_handle;
    if (!smile_3d_set_material(material, alpha_mode, red, green, blue, opacity, unlit, emissive, cutoff))
    {
        smile_3d_delete_material(material);
        return 0;
    }
    return smile_3d_handle(SMILE_3D_MATERIAL_HANDLE, slot, material->generation);
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
    for (unsigned int index = 0; index < vertex_count; ++index) mesh->vertices[index].weights[0] = 1.0f;
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

static long long smile_3d_create_object(long long mesh_handle)
{
    int slot;
    SmileObject3D* object;
    if (smile_3d_mesh(mesh_handle) == 0) { smile_last_error3d = 5; return 0; }
    for (slot = 0; slot < SMILE_3D_MAX_OBJECTS; ++slot)
        if (!smile_objects3d[slot].active) break;
    if (slot == SMILE_3D_MAX_OBJECTS) { smile_last_error3d = 9; return 0; }
    object = &smile_objects3d[slot];
    if (object->generation == 0) object->generation = 1;
    object->active = 1; object->visible = 1; object->mesh_handle = mesh_handle;
    object->material_handle = 0; object->animator_handle = 0;
    object->position[0] = object->position[1] = object->position[2] = 0.0f;
    object->rotation[0] = object->rotation[1] = object->rotation[2] = 0.0f;
    object->scale[0] = object->scale[1] = object->scale[2] = 1.0f;
    object->color[0] = object->color[1] = object->color[2] = 1.0f; object->color[3] = 1.0f;
    return smile_3d_handle(SMILE_3D_OBJECT_HANDLE, slot, object->generation);
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

extern "C" long long smile_renderer3d_load_model_path(const wchar_t* path)
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
        if (model->generation == 0) model->generation = 1;
        model->active = 1;
        model->part_count = (unsigned char)part_count;
        model->material_count = (unsigned short)material_count;
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

static SmileMatrix3D smile_3d_view(void)
{
    float zx = smile_camera_target3d[0] - smile_camera_position3d[0];
    float zy = smile_camera_target3d[1] - smile_camera_position3d[1];
    float zz = smile_camera_target3d[2] - smile_camera_position3d[2];
    float xx, xy, xz, yx, yy, yz;
    SmileMatrix3D result = smile_3d_identity();
    smile_3d_normalize(&zx, &zy, &zz);
    smile_3d_cross(0, 1, 0, zx, zy, zz, &xx, &xy, &xz);
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
        "cbuffer C:register(b0){row_major float4x4 model;row_major float4x4 mvp;float4 tint;float4 material;float4 animation;row_major float4x4 bones[32];}"
        "struct I{float3 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;float4 j:BLENDINDICES;float4 w:BLENDWEIGHT;};"
        "struct O{float4 p:SV_POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;};"
        "O main(I i){O o;float4 p=float4(i.p,1);float3 n=i.n;if(animation.x>.5){"
        "float4x4 s=bones[(uint)i.j.x]*i.w.x+bones[(uint)i.j.y]*i.w.y+bones[(uint)i.j.z]*i.w.z+bones[(uint)i.j.w]*i.w.w;"
        "p=mul(p,s);n=mul(float4(n,0),s).xyz;}o.p=mul(p,mvp);o.n=normalize(mul(float4(n,0),model).xyz);o.uv=i.uv;return o;}";
    static const char* pixel_source =
        "cbuffer C:register(b0){row_major float4x4 model;row_major float4x4 mvp;float4 tint;float4 material;float4 animation;row_major float4x4 bones[32];}"
        "Texture2D baseTexture:register(t0);SamplerState baseSampler:register(s0);"
        "float4 main(float4 p:SV_POSITION,float3 n:NORMAL,float2 uv:TEXCOORD0):SV_TARGET{"
        "float4 base=tint;if(material.x>.5){float4 sample=baseTexture.Sample(baseSampler,uv);"
        "if(sample.a>.0001)sample.rgb/=sample.a;base*=sample;}if(material.w>=0&&base.a<material.w)discard;"
        "float l=.28+.72*max(0,dot(normalize(n),normalize(float3(-.35,.8,-.45))));"
        "float light=material.y>.5?1:l+material.z;return float4(base.rgb*light,base.a);}";
    ID3D11Device* device = (ID3D11Device*)smile_graphics_directx_device();
    ID3DBlob* vs = 0; ID3DBlob* ps = 0;
    D3D11_INPUT_ELEMENT_DESC elements[5] = {};
    D3D11_BUFFER_DESC buffer = {};
    D3D11_DEPTH_STENCIL_DESC depth = {};
    D3D11_DEPTH_STENCIL_DESC depth_read = {};
    D3D11_RASTERIZER_DESC raster = {};
    D3D11_BLEND_DESC blend = {};
    HRESULT result;
    if (device == 0) return 0;
    if (smile_vertex_shader3d != 0) return 1;
    result = smile_3d_compile(device, vertex_source, "main", "vs_4_0", &vs);
    if (SUCCEEDED(result)) result = smile_3d_compile(device, pixel_source, "main", "ps_4_0", &ps);
    if (SUCCEEDED(result)) result = device->CreateVertexShader(vs->GetBufferPointer(), vs->GetBufferSize(), 0, &smile_vertex_shader3d);
    if (SUCCEEDED(result)) result = device->CreatePixelShader(ps->GetBufferPointer(), ps->GetBufferSize(), 0, &smile_pixel_shader3d);
    elements[0].SemanticName = "POSITION"; elements[0].Format = DXGI_FORMAT_R32G32B32_FLOAT; elements[0].InputSlotClass = D3D11_INPUT_PER_VERTEX_DATA;
    elements[1].SemanticName = "NORMAL"; elements[1].Format = DXGI_FORMAT_R32G32B32_FLOAT; elements[1].AlignedByteOffset = 12; elements[1].InputSlotClass = D3D11_INPUT_PER_VERTEX_DATA;
    elements[2].SemanticName = "TEXCOORD"; elements[2].Format = DXGI_FORMAT_R32G32_FLOAT; elements[2].AlignedByteOffset = 24; elements[2].InputSlotClass = D3D11_INPUT_PER_VERTEX_DATA;
    elements[3].SemanticName = "BLENDINDICES"; elements[3].Format = DXGI_FORMAT_R32G32B32A32_FLOAT; elements[3].AlignedByteOffset = 32; elements[3].InputSlotClass = D3D11_INPUT_PER_VERTEX_DATA;
    elements[4].SemanticName = "BLENDWEIGHT"; elements[4].Format = DXGI_FORMAT_R32G32B32A32_FLOAT; elements[4].AlignedByteOffset = 48; elements[4].InputSlotClass = D3D11_INPUT_PER_VERTEX_DATA;
    if (SUCCEEDED(result)) result = device->CreateInputLayout(elements, 5, vs->GetBufferPointer(), vs->GetBufferSize(), &smile_input_layout3d);
    buffer.ByteWidth = sizeof(SmileConstants3D); buffer.Usage = D3D11_USAGE_DEFAULT; buffer.BindFlags = D3D11_BIND_CONSTANT_BUFFER;
    if (SUCCEEDED(result)) result = device->CreateBuffer(&buffer, 0, &smile_constant_buffer3d);
    depth.DepthEnable = TRUE; depth.DepthWriteMask = D3D11_DEPTH_WRITE_MASK_ALL; depth.DepthFunc = D3D11_COMPARISON_LESS;
    if (SUCCEEDED(result)) result = device->CreateDepthStencilState(&depth, &smile_depth_state3d);
    depth_read.DepthEnable = TRUE; depth_read.DepthWriteMask = D3D11_DEPTH_WRITE_MASK_ZERO; depth_read.DepthFunc = D3D11_COMPARISON_LESS;
    if (SUCCEEDED(result)) result = device->CreateDepthStencilState(&depth_read, &smile_depth_read_state3d);
    raster.FillMode = D3D11_FILL_SOLID; raster.CullMode = D3D11_CULL_NONE; raster.DepthClipEnable = TRUE;
    if (SUCCEEDED(result)) result = device->CreateRasterizerState(&raster, &smile_raster_state3d);
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
    if (SUCCEEDED(result)) result = device->CreateBlendState(&blend, &smile_additive_blend_state3d);
    smile_3d_release(vs); smile_3d_release(ps);
    if (FAILED(result)) { smile_last_error3d = 10; smile_graphics3d_on_device_lost(); return 0; }
    return 1;
}

static int smile_3d_create_depth(void)
{
    ID3D11Device* device = (ID3D11Device*)smile_graphics_directx_device();
    int width = smile_graphics_directx_physical_width(), height = smile_graphics_directx_physical_height();
    D3D11_TEXTURE2D_DESC description = {};
    HRESULT result;
    if (device == 0 || width <= 0 || height <= 0) return 0;
    if (smile_depth_view3d != 0 && width == smile_depth_width3d && height == smile_depth_height3d) return 1;
    smile_3d_release(smile_depth_view3d); smile_3d_release(smile_depth_texture3d);
    description.Width = (UINT)width; description.Height = (UINT)height; description.MipLevels = 1; description.ArraySize = 1;
    description.Format = DXGI_FORMAT_D24_UNORM_S8_UINT; description.SampleDesc.Count = 1; description.BindFlags = D3D11_BIND_DEPTH_STENCIL;
    result = device->CreateTexture2D(&description, 0, &smile_depth_texture3d);
    if (SUCCEEDED(result)) result = device->CreateDepthStencilView(smile_depth_texture3d, 0, &smile_depth_view3d);
    if (FAILED(result)) { smile_last_error3d = 11; return 0; }
    smile_depth_width3d = width; smile_depth_height3d = height;
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
    D3D11_TEXTURE2D_DESC description = {};
    D3D11_SUBRESOURCE_DATA data = {};
    D3D11_SAMPLER_DESC sampler = {};
    HRESULT result;
    if (texture->view != 0 && texture->sampler != 0) return 1;
    if (device == 0 || texture->image == 0) return 0;
    description.Width = (UINT)smile_image_resource_width(texture->image);
    description.Height = (UINT)smile_image_resource_height(texture->image);
    description.MipLevels = 1;
    description.ArraySize = 1;
    description.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
    description.SampleDesc.Count = 1;
    description.Usage = D3D11_USAGE_IMMUTABLE;
    description.BindFlags = D3D11_BIND_SHADER_RESOURCE;
    data.pSysMem = smile_image_resource_pixels(texture->image);
    data.SysMemPitch = smile_image_resource_stride(texture->image);
    result = device->CreateTexture2D(&description, &data, &texture->texture);
    if (SUCCEEDED(result)) result = device->CreateShaderResourceView(texture->texture, 0, &texture->view);
    sampler.Filter = texture->filter == 0 ? D3D11_FILTER_MIN_MAG_MIP_POINT : D3D11_FILTER_MIN_MAG_MIP_LINEAR;
    sampler.AddressU = texture->wrap == 0 ? D3D11_TEXTURE_ADDRESS_CLAMP : D3D11_TEXTURE_ADDRESS_WRAP;
    sampler.AddressV = sampler.AddressU;
    sampler.AddressW = sampler.AddressU;
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

static int smile_3d_begin(long long red, long long green, long long blue)
{
    ID3D11DeviceContext* context;
    ID3D11RenderTargetView* target;
    D3D11_VIEWPORT viewport = {};
    float clear[4];
    if (smile_frame_active3d) return 1;
    smile_graphics_begin_frame();
    if (!smile_graphics_directx_suspend_2d() || !smile_3d_create_pipeline() || !smile_3d_create_depth())
    {
        smile_graphics_directx_resume_2d(); smile_last_error3d = 13; return 0;
    }
    context = (ID3D11DeviceContext*)smile_graphics_directx_context();
    target = (ID3D11RenderTargetView*)smile_graphics_directx_render_target();
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
    context->ClearRenderTargetView(target, clear);
    context->ClearDepthStencilView(smile_depth_view3d, D3D11_CLEAR_DEPTH | D3D11_CLEAR_STENCIL, 1.0f, 0);
    context->IASetInputLayout(smile_input_layout3d); context->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    context->VSSetShader(smile_vertex_shader3d, 0, 0); context->PSSetShader(smile_pixel_shader3d, 0, 0);
    context->VSSetConstantBuffers(0, 1, &smile_constant_buffer3d); context->PSSetConstantBuffers(0, 1, &smile_constant_buffer3d);
    smile_frame_active3d = 1;
    return 1;
}

static int smile_3d_draw(long long handle)
{
    SmileObject3D* object = smile_3d_object(handle);
    SmileMesh3D* mesh;
    SmileMaterial3D* material = 0;
    SmileTexture3D* texture = 0;
    SmileAnimator3D* animator = 0;
    ID3D11DeviceContext* context = (ID3D11DeviceContext*)smile_graphics_directx_context();
    ID3D11ShaderResourceView* texture_view = 0;
    ID3D11SamplerState* texture_sampler = 0;
    SmileConstants3D constants = {};
    SmileMatrix3D view, projection;
    UINT stride = sizeof(SmileVertex3D), offset = 0;
    float aspect;
    int alpha_mode;
    if (!smile_frame_active3d || object == 0) { smile_last_error3d = 14; return 0; }
    if (!object->visible) return 1;
    mesh = smile_3d_mesh(object->mesh_handle);
    if (mesh == 0 || !smile_3d_upload(mesh)) return 0;
    if (object->material_handle != 0)
    {
        material = smile_3d_material(object->material_handle);
        if (material == 0) { smile_last_error3d = 5; return 0; }
        if (material->texture_handle != 0)
        {
            texture = smile_3d_texture(material->texture_handle);
            if (texture == 0 || !smile_3d_upload_texture(texture)) return 0;
            texture_view = texture->view;
            texture_sampler = texture->sampler;
        }
    }
    if (object->animator_handle != 0)
    {
        animator = smile_3d_animator(object->animator_handle);
        SmileSkeleton3D* skeleton = animator == 0 ? 0 : smile_3d_skeleton(animator->skeleton_handle);
        if (animator == 0 || skeleton == 0 || mesh->max_joint >= skeleton->bone_count)
        {
            smile_last_error3d = 36;
            return 0;
        }
    }
    constants.model = smile_3d_model(object);
    view = smile_3d_view();
    aspect = (float)smile_graphics_directx_viewport_width() / (float)smile_graphics_directx_viewport_height();
    projection = smile_3d_projection(aspect > 0.0f ? aspect : 1.0f);
    constants.mvp = smile_3d_multiply(smile_3d_multiply(constants.model, view), projection);
    constants.color[0] = object->color[0] * (material == 0 ? 1.0f : material->color[0]);
    constants.color[1] = object->color[1] * (material == 0 ? 1.0f : material->color[1]);
    constants.color[2] = object->color[2] * (material == 0 ? 1.0f : material->color[2]);
    constants.color[3] = object->color[3] * (material == 0 ? 1.0f : material->color[3]);
    constants.material[0] = texture == 0 ? 0.0f : 1.0f;
    constants.material[1] = material == 0 ? 0.0f : (float)material->unlit;
    constants.material[2] = material == 0 ? 0.0f : material->emissive;
    constants.material[3] = material != 0 && material->alpha_mode == 1 ? material->cutoff : -1.0f;
    constants.animation[0] = animator == 0 ? 0.0f : 1.0f;
    for (int bone = 0; bone < SMILE_3D_MAX_BONES; ++bone)
        constants.bones[bone] = animator == 0 ? smile_3d_identity() : animator->bones[bone];
    alpha_mode = material == 0 ? (constants.color[3] < 0.999f ? 2 : 0) : material->alpha_mode;
    context->UpdateSubresource(smile_constant_buffer3d, 0, 0, &constants, 0, 0);
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
    context->DrawIndexed(mesh->index_count, 0, 0);
    return 1;
}

static void smile_3d_end(void)
{
    if (!smile_frame_active3d) return;
    ID3D11DeviceContext* context = (ID3D11DeviceContext*)smile_graphics_directx_context();
    if (context != 0)
    {
        ID3D11ShaderResourceView* empty_view = 0;
        context->PSSetShaderResources(0, 1, &empty_view);
        context->OMSetRenderTargets(0, 0, 0);
    }
    smile_frame_active3d = 0;
    smile_graphics_directx_resume_2d();
}

extern "C" void smile_graphics3d_on_device_lost(void)
{
    int index;
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
    smile_3d_release(smile_depth_view3d); smile_3d_release(smile_depth_texture3d);
    smile_3d_release(smile_additive_blend_state3d); smile_3d_release(smile_blend_state3d);
    smile_3d_release(smile_raster_state3d);
    smile_3d_release(smile_depth_read_state3d); smile_3d_release(smile_depth_state3d);
    smile_3d_release(smile_constant_buffer3d); smile_3d_release(smile_input_layout3d);
    smile_3d_release(smile_pixel_shader3d); smile_3d_release(smile_vertex_shader3d);
    smile_depth_width3d = smile_depth_height3d = 0;
}

static void smile_3d_reset(void)
{
    int index;
    smile_3d_end();
    for (index = 0; index < SMILE_3D_MAX_OBJECTS; ++index)
        if (smile_objects3d[index].active)
        {
            smile_objects3d[index].active = 0; smile_objects3d[index].generation++;
            if (smile_objects3d[index].generation == 0) smile_objects3d[index].generation = 1;
        }
    for (index = 0; index < SMILE_3D_MAX_MODELS; ++index)
        if (smile_models3d[index].active) smile_3d_delete_model(&smile_models3d[index]);
    for (index = 0; index < SMILE_3D_MAX_ANIMATORS; ++index)
        if (smile_animators3d[index].active) smile_3d_delete_animator(&smile_animators3d[index]);
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
    (void)j;
    switch (command)
    {
        case SMILE_3D_AVAILABLE: return smile_graphics_directx_device() != 0 ? 1 : 0;
        case SMILE_3D_RESET: smile_3d_reset(); return 1;
        case SMILE_3D_CREATE_MESH: return smile_3d_create_mesh((unsigned int)a, (unsigned int)b);
        case SMILE_3D_SET_VERTEX:
            mesh = smile_3d_mesh(a); if (mesh == 0 || b < 0 || b >= mesh->vertex_count) { smile_last_error3d = 5; return 0; }
            smile_3d_vertex(mesh, (unsigned int)b, (float)c, (float)d, (float)e); mesh->committed = 0; return 1;
        case SMILE_3D_SET_TRIANGLE:
            mesh = smile_3d_mesh(a); if (mesh == 0 || b < 0 || b * 3 + 2 >= mesh->index_count || c < 0 || d < 0 || e < 0) { smile_last_error3d = 5; return 0; }
            smile_3d_triangle(mesh, (unsigned int)b, (unsigned int)c, (unsigned int)d, (unsigned int)e); mesh->committed = 0; return 1;
        case SMILE_3D_COMMIT_MESH: return smile_3d_commit_mesh(smile_3d_mesh(a));
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
                if (smile_3d_material_reference_count(a) != 0) { smile_last_error3d = 22; return 0; }
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
            smile_camera_position3d[0] = (float)a; smile_camera_position3d[1] = (float)b; smile_camera_position3d[2] = (float)c;
            smile_camera_target3d[0] = (float)d; smile_camera_target3d[1] = (float)e; smile_camera_target3d[2] = (float)f;
            smile_camera_fov3d = (float)g; smile_camera_near3d = (float)h; smile_camera_far3d = (float)i;
            if (smile_camera_fov3d < 10 || smile_camera_fov3d > 160 || smile_camera_near3d <= 0 || smile_camera_far3d <= smile_camera_near3d) { smile_last_error3d = 15; return 0; }
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
        case SMILE_3D_END: smile_3d_end(); return 1;
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
            if (object == 0 || (b != 0 && smile_3d_material(b) == 0)) { smile_last_error3d = 5; return 0; }
            object->material_handle = b; return 1;
        case SMILE_3D_SET_MESH_UV:
            mesh = smile_3d_mesh(a);
            if (mesh == 0 || b < 0 || b >= mesh->vertex_count) { smile_last_error3d = 5; return 0; }
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
            clip->scale[b][1][0]=(float)f/100;clip->scale[b][1][1]=(float)g/100;clip->scale[b][1][2]=(float)h/100;return 1;
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
            return b < 0 ? 0 : smile_3d_update_animator(smile_3d_animator(a),(unsigned int)b);
        case SMILE_3D_ANIMATOR_COMPLETE: animator=smile_3d_animator(a);return animator==0?0:animator->complete;
        case SMILE_3D_ANIMATOR_TIME: animator=smile_3d_animator(a);return animator==0?0:animator->time_ms;
        case SMILE_3D_TAKE_ANIMATOR_EVENT:
            animator=smile_3d_animator(a);if(animator==0)return 0;{unsigned int value=animator->pending_event;animator->pending_event=0;return value;}
        case SMILE_3D_SET_OBJECT_ANIMATOR:
            object=smile_3d_object(a);animator=b==0?0:smile_3d_animator(b);mesh=object==0?0:smile_3d_mesh(object->mesh_handle);
            skeleton=animator==0?0:smile_3d_skeleton(animator->skeleton_handle);
            if(object==0||mesh==0||(b!=0&&(animator==0||skeleton==0||mesh->max_joint>=skeleton->bone_count))){smile_last_error3d=36;return 0;}
            object->animator_handle=b;return 1;
        case SMILE_3D_LIVE_SKELETON_COUNT:return smile_3d_live_skeleton_count();
        case SMILE_3D_LIVE_CLIP_COUNT:return smile_3d_live_clip_count();
        case SMILE_3D_LIVE_ANIMATOR_COUNT:return smile_3d_live_animator_count();
        case SMILE_3D_MAX_BONE_COUNT:return SMILE_3D_MAX_BONES;
        case SMILE_3D_SKELETON_VALID:return smile_3d_skeleton(a)!=0;
        case SMILE_3D_CLIP_VALID:return smile_3d_clip(a)!=0;
        case SMILE_3D_ANIMATOR_VALID:return smile_3d_animator(a)!=0;
        case SMILE_3D_STOP_ANIMATOR:
            animator=smile_3d_animator(a);if(animator==0)return 0;animator->clip_handle=0;animator->time_ms=0;
            animator->previous_time_ms=0;animator->complete=0;animator->pending_event=0;smile_3d_update_animation_pose(animator);return 1;
        case SMILE_3D_MAX_SKELETON_COUNT:return SMILE_3D_MAX_SKELETONS;
        case SMILE_3D_MAX_CLIP_COUNT:return SMILE_3D_MAX_CLIPS;
        case SMILE_3D_MAX_ANIMATOR_COUNT:return SMILE_3D_MAX_ANIMATORS;
        default: smile_last_error3d = 1; return 0;
    }
}

extern "C" long long smile_renderer3d_image_command(long long command, void* image,
    long long a, long long b, long long c, long long d,
    long long e, long long f, long long g, long long h)
{
    (void)c; (void)d; (void)e; (void)f; (void)g; (void)h;
    if (command == SMILE_3D_IMAGE_CREATE_TEXTURE)
        return smile_3d_create_texture((SmileImageResource*)image, (int)a, (int)b);
    smile_image_resource_release((SmileImageResource*)image);
    smile_last_error3d = 1;
    return 0;
}
