#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <d3d11.h>
#include <d3dcompiler.h>
#include <math.h>
#include "graphics3d.h"
#include "graphics_common.h"
#include "graphics_directx.h"
#include "image_resource.h"

#define SMILE_3D_MAX_MESHES 128
#define SMILE_3D_MAX_OBJECTS 256
#define SMILE_3D_MAX_TEXTURES 128
#define SMILE_3D_MAX_MATERIALS 128
#define SMILE_3D_MESH_HANDLE 0x10000000LL
#define SMILE_3D_OBJECT_HANDLE 0x20000000LL
#define SMILE_3D_TEXTURE_HANDLE 0x30000000LL
#define SMILE_3D_MATERIAL_HANDLE 0x40000000LL
#define SMILE_3D_HANDLE_KIND 0xF0000000LL
#define SMILE_3D_PI 3.14159265358979323846f

struct SmileVertex3D
{
    float x, y, z;
    float nx, ny, nz;
    float u, v;
};

struct SmileMesh3D
{
    unsigned short generation;
    unsigned char active;
    unsigned char committed;
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

struct SmileMatrix3D { float m[16]; };

struct SmileConstants3D
{
    SmileMatrix3D model;
    SmileMatrix3D mvp;
    float color[4];
    float material[4];
};

static SmileMesh3D smile_meshes3d[SMILE_3D_MAX_MESHES];
static SmileObject3D smile_objects3d[SMILE_3D_MAX_OBJECTS];
static SmileTexture3D smile_textures3d[SMILE_3D_MAX_TEXTURES];
static SmileMaterial3D smile_materials3d[SMILE_3D_MAX_MATERIALS];
static ID3D11VertexShader* smile_vertex_shader3d;
static ID3D11PixelShader* smile_pixel_shader3d;
static ID3D11InputLayout* smile_input_layout3d;
static ID3D11Buffer* smile_constant_buffer3d;
static ID3D11DepthStencilState* smile_depth_state3d;
static ID3D11DepthStencilState* smile_depth_read_state3d;
static ID3D11RasterizerState* smile_raster_state3d;
static ID3D11BlendState* smile_blend_state3d;
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
    mesh->generation++;
    if (mesh->generation == 0) mesh->generation = 1;
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
    if (material == 0 || alpha_mode < 0 || alpha_mode > 2 || opacity < 0 || opacity > 100 ||
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
        smile_3d_cross(ux, uy, uz, vx, vy, vz, &nx, &ny, &nz);
        mesh->vertices[ia].nx += nx; mesh->vertices[ia].ny += ny; mesh->vertices[ia].nz += nz;
        mesh->vertices[ib].nx += nx; mesh->vertices[ib].ny += ny; mesh->vertices[ib].nz += nz;
        mesh->vertices[ic].nx += nx; mesh->vertices[ic].ny += ny; mesh->vertices[ic].nz += nz;
    }
    for (index = 0; index < mesh->vertex_count; ++index)
        smile_3d_normalize(&mesh->vertices[index].nx, &mesh->vertices[index].ny, &mesh->vertices[index].nz);
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
    object->active = 1; object->visible = 1; object->mesh_handle = mesh_handle; object->material_handle = 0;
    object->position[0] = object->position[1] = object->position[2] = 0.0f;
    object->rotation[0] = object->rotation[1] = object->rotation[2] = 0.0f;
    object->scale[0] = object->scale[1] = object->scale[2] = 1.0f;
    object->color[0] = object->color[1] = object->color[2] = 1.0f; object->color[3] = 1.0f;
    return smile_3d_handle(SMILE_3D_OBJECT_HANDLE, slot, object->generation);
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
        "cbuffer C:register(b0){row_major float4x4 model;row_major float4x4 mvp;float4 tint;float4 material;}"
        "struct I{float3 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;};"
        "struct O{float4 p:SV_POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;};"
        "O main(I i){O o;o.p=mul(float4(i.p,1),mvp);o.n=normalize(mul(float4(i.n,0),model).xyz);o.uv=i.uv;return o;}";
    static const char* pixel_source =
        "cbuffer C:register(b0){row_major float4x4 model;row_major float4x4 mvp;float4 tint;float4 material;}"
        "Texture2D baseTexture:register(t0);SamplerState baseSampler:register(s0);"
        "float4 main(float4 p:SV_POSITION,float3 n:NORMAL,float2 uv:TEXCOORD0):SV_TARGET{"
        "float4 base=tint;if(material.x>.5){float4 sample=baseTexture.Sample(baseSampler,uv);"
        "if(sample.a>.0001)sample.rgb/=sample.a;base*=sample;}if(material.w>=0&&base.a<material.w)discard;"
        "float l=.28+.72*max(0,dot(normalize(n),normalize(float3(-.35,.8,-.45))));"
        "float light=material.y>.5?1:l+material.z;return float4(base.rgb*light,base.a);}";
    ID3D11Device* device = (ID3D11Device*)smile_graphics_directx_device();
    ID3DBlob* vs = 0; ID3DBlob* ps = 0;
    D3D11_INPUT_ELEMENT_DESC elements[3] = {};
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
    if (SUCCEEDED(result)) result = device->CreateInputLayout(elements, 3, vs->GetBufferPointer(), vs->GetBufferSize(), &smile_input_layout3d);
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
    ID3D11DeviceContext* context = (ID3D11DeviceContext*)smile_graphics_directx_context();
    ID3D11ShaderResourceView* texture_view = 0;
    ID3D11SamplerState* texture_sampler = 0;
    SmileConstants3D constants;
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
    alpha_mode = material == 0 ? (constants.color[3] < 0.999f ? 2 : 0) : material->alpha_mode;
    context->UpdateSubresource(smile_constant_buffer3d, 0, 0, &constants, 0, 0);
    context->OMSetBlendState(alpha_mode == 2 ? smile_blend_state3d : 0, 0, 0xffffffff);
    context->OMSetDepthStencilState(alpha_mode == 2 ? smile_depth_read_state3d : smile_depth_state3d, 0);
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
    smile_3d_release(smile_blend_state3d); smile_3d_release(smile_raster_state3d);
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
