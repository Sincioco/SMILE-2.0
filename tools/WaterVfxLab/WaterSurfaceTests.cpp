#include "../../src/Smile.NativeRuntime/graphics/water_ribbon_normals3d.h"
#include <stdio.h>

struct WaterTestVertex
{
    float position[3], color[4], normal[3];
};

int main()
{
    // Two perpendicular sheets joined with the same zero-area connectors as the Lab.
    WaterTestVertex vertices[16] = {
        {{0,0,0}}, {{0,0,0}}, {{0,0,0}}, {{1,0,0}},
        {{0,1,0}}, {{1,1,0}}, {{1,1,0}}, {{1,1,0}},
        {{1,0,0}}, {{1,0,0}}, {{1,0,0}}, {{1,0,-1}},
        {{1,1,0}}, {{1,1,-1}}, {{1,1,-1}}, {{1,1,-1}}
    };
    for (int index = 0; index < 16; ++index) vertices[index].color[3] = 1;
    SmileWaterNormalKey3D keys[16] = {};
    smile_water_ribbon_normals(vertices, 16, keys);
    bool passed = vertices[3].normal[0] > .2f && vertices[3].normal[2] > .2f;
    for (int axis = 0; axis < 3; ++axis)
    {
        passed = passed && fabsf(vertices[3].normal[axis] - vertices[10].normal[axis]) < .00001f;
        passed = passed && fabsf(vertices[5].normal[axis] - vertices[12].normal[axis]) < .00001f;
    }
    passed = passed && fabsf(vertices[2].normal[2] - 1) < .00001f;
    passed = passed && fabsf(vertices[11].normal[0] - 1) < .00001f;
    WaterTestVertex collapsed[4] = {};
    smile_water_ribbon_normals(collapsed, 4, keys);
    for (int index = 0; index < 4; ++index)
        for (int axis = 0; axis < 3; ++axis)
            passed = passed && collapsed[index].normal[axis] == 0;
    puts(passed ? "Water surface seam normals passed." : "Water surface seam normals FAILED.");
    return passed ? 0 : 1;
}
