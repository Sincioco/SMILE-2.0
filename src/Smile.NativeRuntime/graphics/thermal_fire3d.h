#ifndef SMILE_THERMAL_FIRE3D_H
#define SMILE_THERMAL_FIRE3D_H

#include <math.h>
#include <stdint.h>

// System constants only: the published particle schema stays exactly 80 bytes.
struct SmileThermalDynamics3D
{
    float gravity_buoyancy[4];
    float wind_drag[4];
    float turbulence[4]; // acceleration, frequency, speed, octaves
    float evolution[4]; // cooling, dissipation, growth, speed limit
    float bounds_min[4];
    float bounds_max[4];
    float render[4]; // enabled, shader mode, alignment, atlas columns
    float time[4]; // seconds, atlas rows, reserved, reserved
};
static_assert(sizeof(SmileThermalDynamics3D) == 128, "Thermal constants packing");

static uint32_t smile_fire_hash(uint32_t value)
{
    value ^= value >> 16; value *= 0x7feb352dU;
    value ^= value >> 15; value *= 0x846ca68bU;
    return value ^ (value >> 16);
}

static float smile_fire_noise(float x, float y, float z, uint32_t seed)
{
    const float fx = floorf(x), fy = floorf(y), fz = floorf(z);
    float u = x - fx, v = y - fy, w = z - fz;
    u = u * u * (3 - 2 * u); v = v * v * (3 - 2 * v); w = w * w * (3 - 2 * w);
    float result = 0;
    for (int corner = 0; corner < 8; ++corner)
    {
        const uint32_t ix = (uint32_t)(int32_t)fx + (corner & 1);
        const uint32_t iy = (uint32_t)(int32_t)fy + ((corner >> 1) & 1);
        const uint32_t iz = (uint32_t)(int32_t)fz + ((corner >> 2) & 1);
        const float value = (float)(smile_fire_hash(ix * 73856093U ^ iy * 19349663U ^
            iz * 83492791U ^ seed) & 65535U) / 32767.5f - 1;
        result += value * ((corner & 1) ? u : 1-u) *
            ((corner & 2) ? v : 1-v) * ((corner & 4) ? w : 1-w);
    }
    return result;
}

// CPU deterministic reference/fallback. GPU logical occupancy remains CPU-owned;
// early visual deaths do not release slots until their scheduled lifetime ends.
static bool smile_fire_step(float* position, float* velocity, float* size,
    float* thermal, uint32_t seed, const SmileThermalDynamics3D& d, float seconds)
{
    for (int axis = 0; axis < 3; ++axis)
        if (!isfinite(position[axis]) || !isfinite(velocity[axis])) return false;
    if (!isfinite(size[0]) || !isfinite(size[1]) ||
        !isfinite(thermal[0]) || !isfinite(thermal[1])) return false;
    thermal[0] = fmaxf(0, thermal[0] - d.evolution[0] * seconds);
    thermal[1] = fmaxf(0, thermal[1] - d.evolution[1] * seconds);
    float p[3];
    for (int axis = 0; axis < 3; ++axis)
        p[axis] = position[axis] * d.turbulence[1] +
            d.time[0] * d.turbulence[2] + (float)(seed & 255U) * .03125f;
    float turbulence[3] = {};
    if (d.turbulence[0] > 0)
        for (int octave = 0; octave < (int)d.turbulence[3]; ++octave)
        {
            const float frequency = octave == 0 ? 1.0f : 2.0f;
            const float gain = octave == 0 ? 1.0f : .5f;
            for (int axis = 0; axis < 3; ++axis)
                turbulence[axis] += gain * smile_fire_noise(p[0]*frequency,
                    p[1]*frequency, p[2]*frequency, seed + (uint32_t)axis * 1013U);
        }
    const float drag = expf(-d.wind_drag[3] * seconds);
    float speed_squared = 0;
    for (int axis = 0; axis < 3; ++axis)
    {
        const float acceleration = d.gravity_buoyancy[axis] + d.wind_drag[axis] +
            (axis == 1 ? d.gravity_buoyancy[3] * thermal[0] : 0) +
            turbulence[axis] * d.turbulence[0];
        velocity[axis] = (velocity[axis] + acceleration * seconds) * drag;
        speed_squared += velocity[axis] * velocity[axis];
    }
    const float speed_limit = d.evolution[3];
    const float clamp = speed_squared > speed_limit * speed_limit ?
        speed_limit / sqrtf(speed_squared) : 1.0f;
    for (int axis = 0; axis < 3; ++axis)
    {
        velocity[axis] *= clamp;
        position[axis] += velocity[axis] * seconds;
        if (!isfinite(position[axis]) || position[axis] < d.bounds_min[axis] ||
            position[axis] > d.bounds_max[axis]) return false;
    }
    size[0] += d.evolution[2] * seconds;
    size[1] += d.evolution[2] * seconds;
    return thermal[1] > 0 && size[0] > 0 && size[1] > 0 &&
        size[0] <= 1000000 && size[1] <= 1000000;
}

// Same bounded value-noise field as the CPU reference. Unsigned hash wrap is
// intentional; frequency <= 1 and world bounds <= 1e6 keep lattice casts safe.
#define SMILE_THERMAL_HLSL \
    "uint FireHash(uint v){v^=v>>16;v*=0x7feb352d;v^=v>>15;v*=0x846ca68b;return v^(v>>16);}" \
    "float FireNoise(float3 p,uint seed){float3 f=floor(p),u=frac(p);u=u*u*(3-2*u);float result=0;" \
    "[unroll]for(int c=0;c<8;c++){uint3 q=(uint3)(int3)f+uint3(c&1,(c>>1)&1,(c>>2)&1);" \
    "float value=(FireHash(q.x*73856093^q.y*19349663^q.z*83492791^seed)&65535)/32767.5-1;" \
    "result+=value*((c&1)?u.x:1-u.x)*((c&2)?u.y:1-u.y)*((c&4)?u.z:1-u.z);}return result;}" \
    "bool FireStep(inout Particle p){if(!all(isfinite(p.positionAge))||!all(isfinite(p.velocityLifetime))||" \
    "!all(isfinite(p.sizeRotationAngular))||!all(isfinite(p.thermalDensityNoise)))return false;" \
    "p.thermalDensityNoise.xy=max(0,p.thermalDensityNoise.xy-evolution.xy*stepSeconds);" \
    "uint seed=p.seedFlagsGradientFrame.x;float3 pos=p.positionAge.xyz*turbulence.y+fireTime.x*turbulence.z+(seed&255)*.03125;float3 flow=0;" \
    "if(turbulence.x>0){[unroll]for(int octave=0;octave<2;octave++){if(octave>=turbulence.w)break;float frequency=octave==0?1:2;float gain=octave==0?1:.5;" \
    "flow+=gain*float3(FireNoise(pos*frequency,seed),FireNoise(pos*frequency,seed+1013),FireNoise(pos*frequency,seed+2026));}}" \
    "float3 acceleration=gravityBuoyancy.xyz+windDrag.xyz+float3(0,gravityBuoyancy.w*p.thermalDensityNoise.x,0)+flow*turbulence.x;" \
    "p.velocityLifetime.xyz=(p.velocityLifetime.xyz+acceleration*stepSeconds)*exp(-windDrag.w*stepSeconds);" \
    "float speed=length(p.velocityLifetime.xyz);if(speed>evolution.w)p.velocityLifetime.xyz*=evolution.w/max(speed,.00001);" \
    "p.positionAge.xyz+=p.velocityLifetime.xyz*stepSeconds;p.sizeRotationAngular.xy+=evolution.z*stepSeconds;" \
    "return all(isfinite(p.positionAge.xyz))&&all(p.positionAge.xyz>=boundsMin.xyz)&&all(p.positionAge.xyz<=boundsMax.xyz)&&" \
    "p.thermalDensityNoise.y>0&&all(p.sizeRotationAngular.xy>0)&&all(p.sizeRotationAngular.xy<=1000000);}"

#endif
