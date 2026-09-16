#pragma once
#include <math.h>
#include <stdlib.h>

// Reused batch-local scratch. Quantization joins independently sampled ribbon seams.
struct SmileWaterNormalKey3D
{
    long long position[3];
    unsigned int vertex;
};

static int smile_water_normal_key_compare(const void* left, const void* right)
{
    const SmileWaterNormalKey3D* a = (const SmileWaterNormalKey3D*)left;
    const SmileWaterNormalKey3D* b = (const SmileWaterNormalKey3D*)right;
    for (int axis = 0; axis < 3; ++axis)
    {
        if (a->position[axis] < b->position[axis]) return -1;
        if (a->position[axis] > b->position[axis]) return 1;
    }
    return 0;
}

// Area-weighted triangle-strip normals, welded across coincident sheet edges.
// Degenerate strip connectors contribute nothing; no normal crosses a real gap.
template<class Vertex>
static void smile_water_ribbon_normals(Vertex* vertices, unsigned int count,
    SmileWaterNormalKey3D* keys)
{
    for (unsigned int index = 0; index < count; ++index)
    {
        keys[index].vertex = index;
        for (int axis = 0; axis < 3; ++axis)
        {
            vertices[index].normal[axis] = 0;
            keys[index].position[axis] = (long long)floor(vertices[index].position[axis] * 4096.0 + .5);
        }
    }
    for (unsigned int index = 2; index < count; ++index)
    {
        Vertex& a = vertices[index - 2];
        Vertex& b = vertices[index - 1];
        Vertex& c = vertices[index];
        if (a.color[3] == 0 && b.color[3] == 0 && c.color[3] == 0) continue;
        float ab[3], ac[3], normal[3];
        for (int axis = 0; axis < 3; ++axis)
        { ab[axis] = b.position[axis] - a.position[axis]; ac[axis] = c.position[axis] - a.position[axis]; }
        normal[0] = ab[1] * ac[2] - ab[2] * ac[1];
        normal[1] = ab[2] * ac[0] - ab[0] * ac[2];
        normal[2] = ab[0] * ac[1] - ab[1] * ac[0];
        for (int axis = 0; axis < 3; ++axis)
        {
            float value = (index & 1) ? -normal[axis] : normal[axis];
            a.normal[axis] += value; b.normal[axis] += value; c.normal[axis] += value;
        }
    }
    qsort(keys, count, sizeof(*keys), smile_water_normal_key_compare);
    for (unsigned int first = 0; first < count;)
    {
        unsigned int end = first + 1;
        while (end < count && smile_water_normal_key_compare(keys + first, keys + end) == 0) ++end;
        float normal[3] = {};
        for (unsigned int index = first; index < end; ++index)
            for (int axis = 0; axis < 3; ++axis)
                normal[axis] += vertices[keys[index].vertex].normal[axis];
        float length = sqrtf(normal[0]*normal[0] + normal[1]*normal[1] + normal[2]*normal[2]);
        if (length > .000001f)
            for (int axis = 0; axis < 3; ++axis) normal[axis] /= length;
        for (unsigned int index = first; index < end; ++index)
            for (int axis = 0; axis < 3; ++axis)
                vertices[keys[index].vertex].normal[axis] = normal[axis];
        first = end;
    }
}
