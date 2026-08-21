#ifndef SMILE_GRAPHICS3D_H
#define SMILE_GRAPHICS3D_H

#include <wchar.h>

#ifdef __cplusplus
extern "C" {
#endif

enum SmileRenderer3DCommand
{
    SMILE_3D_AVAILABLE = 1,
    SMILE_3D_RESET = 2,
    SMILE_3D_CREATE_MESH = 3,
    SMILE_3D_SET_VERTEX = 4,
    SMILE_3D_SET_TRIANGLE = 5,
    SMILE_3D_COMMIT_MESH = 6,
    SMILE_3D_CREATE_PRIMITIVE = 7,
    SMILE_3D_CREATE_OBJECT = 8,
    SMILE_3D_DESTROY = 9,
    SMILE_3D_SET_CAMERA = 10,
    SMILE_3D_SET_POSITION = 11,
    SMILE_3D_SET_ROTATION = 12,
    SMILE_3D_SET_SCALE = 13,
    SMILE_3D_SET_COLOR = 14,
    SMILE_3D_SET_VISIBLE = 15,
    SMILE_3D_BEGIN = 16,
    SMILE_3D_DRAW = 17,
    SMILE_3D_END = 18,
    SMILE_3D_MESH_VERTEX_COUNT = 19,
    SMILE_3D_MESH_INDEX_COUNT = 20,
    SMILE_3D_LAST_ERROR = 21,
    SMILE_3D_LIVE_MESH_COUNT = 22,
    SMILE_3D_LIVE_OBJECT_COUNT = 23,
    SMILE_3D_MAX_MESH_COUNT = 24,
    SMILE_3D_MAX_OBJECT_COUNT = 25,
    SMILE_3D_MESH_VALID = 26,
    SMILE_3D_OBJECT_VALID = 27,
    SMILE_3D_MESH_REFERENCE_COUNT = 28,
    SMILE_3D_CREATE_MATERIAL = 29,
    SMILE_3D_SET_OBJECT_MATERIAL = 30,
    SMILE_3D_SET_MESH_UV = 31,
    SMILE_3D_LIVE_TEXTURE_COUNT = 32,
    SMILE_3D_LIVE_MATERIAL_COUNT = 33,
    SMILE_3D_MAX_TEXTURE_COUNT = 34,
    SMILE_3D_MAX_MATERIAL_COUNT = 35,
    SMILE_3D_TEXTURE_VALID = 36,
    SMILE_3D_MATERIAL_VALID = 37,
    SMILE_3D_TEXTURE_WIDTH = 38,
    SMILE_3D_TEXTURE_HEIGHT = 39,
    SMILE_3D_TEXTURE_REFERENCE_COUNT = 40,
    SMILE_3D_MATERIAL_REFERENCE_COUNT = 41,
    SMILE_3D_SET_MATERIAL = 42,
    SMILE_3D_SET_MESH_NORMAL = 43,
    SMILE_3D_LIVE_MODEL_COUNT = 44,
    SMILE_3D_MAX_MODEL_COUNT = 45,
    SMILE_3D_MODEL_VALID = 46,
    SMILE_3D_MODEL_PART_COUNT = 47,
    SMILE_3D_MODEL_MATERIAL_COUNT = 48,
    SMILE_3D_CREATE_MODEL_PART_OBJECT = 49,
    SMILE_3D_MODEL_PART_MATERIAL = 50,
    SMILE_3D_SET_MESH_SKIN = 51,
    SMILE_3D_CREATE_SKELETON = 52,
    SMILE_3D_SET_SKELETON_BONE = 53,
    SMILE_3D_COMMIT_SKELETON = 54,
    SMILE_3D_CREATE_CLIP = 55,
    SMILE_3D_SET_CLIP_TRANSLATION = 56,
    SMILE_3D_SET_CLIP_ROTATION = 57,
    SMILE_3D_SET_CLIP_SCALE = 58,
    SMILE_3D_ADD_CLIP_EVENT = 59,
    SMILE_3D_CREATE_ANIMATOR = 60,
    SMILE_3D_PLAY_ANIMATOR = 61,
    SMILE_3D_UPDATE_ANIMATOR = 62,
    SMILE_3D_ANIMATOR_COMPLETE = 63,
    SMILE_3D_ANIMATOR_TIME = 64,
    SMILE_3D_TAKE_ANIMATOR_EVENT = 65,
    SMILE_3D_SET_OBJECT_ANIMATOR = 66,
    SMILE_3D_LIVE_SKELETON_COUNT = 67,
    SMILE_3D_LIVE_CLIP_COUNT = 68,
    SMILE_3D_LIVE_ANIMATOR_COUNT = 69,
    SMILE_3D_MAX_BONE_COUNT = 70,
    SMILE_3D_SKELETON_VALID = 71,
    SMILE_3D_CLIP_VALID = 72,
    SMILE_3D_ANIMATOR_VALID = 73,
    SMILE_3D_STOP_ANIMATOR = 74,
    SMILE_3D_MAX_SKELETON_COUNT = 75,
    SMILE_3D_MAX_CLIP_COUNT = 76,
    SMILE_3D_MAX_ANIMATOR_COUNT = 77
};

enum SmileRenderer3DImageCommand
{
    SMILE_3D_IMAGE_CREATE_TEXTURE = 1
};

enum SmileRenderer3DTextCommand
{
    SMILE_3D_TEXT_LOAD_MODEL = 1
};

enum SmilePrimitive3D
{
    SMILE_3D_CUBE = 1,
    SMILE_3D_PLANE = 2,
    SMILE_3D_PYRAMID = 3,
    SMILE_3D_SPHERE = 4,
    SMILE_3D_CYLINDER = 5,
    SMILE_3D_TORUS = 6
};

long long smile_renderer3d_command(long long command,
    long long a, long long b, long long c, long long d, long long e,
    long long f, long long g, long long h, long long i, long long j);
long long smile_renderer3d_image_command(long long command, void* image,
    long long a, long long b, long long c, long long d,
    long long e, long long f, long long g, long long h);
long long smile_renderer3d_text_command(long long command, void* text,
    long long a, long long b, long long c, long long d,
    long long e, long long f, long long g, long long h);
long long smile_renderer3d_load_model_path(const wchar_t* path);
void smile_graphics3d_on_device_lost(void);

#ifdef __cplusplus
}
#endif

#endif
