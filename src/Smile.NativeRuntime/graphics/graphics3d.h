#ifndef SMILE_GRAPHICS3D_H
#define SMILE_GRAPHICS3D_H

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
    SMILE_3D_LAST_ERROR = 21
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
void smile_graphics3d_on_device_lost(void);

#ifdef __cplusplus
}
#endif

#endif
