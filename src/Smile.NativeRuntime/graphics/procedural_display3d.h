#pragma once

// A caller-clocked emissive display, evaluated per pixel in object coordinates.
// No texture uploads or simulation advance occur during reflected/replayed draws.
static int smile_3d_set_procedural_display(SmileMaterial3D* material,
    long long milliseconds, long long seed, long long x, long long y, long long z,
    long long radius, long long brightness)
{
    if (material == 0 || material->mode != 1 || milliseconds < 0 || seed < 0 ||
        seed > 65535 || radius <= 0 || radius > 1000000000 ||
        brightness < 0 || brightness > 400)
    {
        smile_last_error3d = 39;
        return 0;
    }
    material->display[0] = (float)(milliseconds % 86400000) / 1000.0f;
    material->display[1] = (float)seed;
    material->display[2] = (float)brightness / 100.0f;
    material->display[3] = brightness > 0 ? 1.0f : 0.0f;
    material->display_geometry[0] = (float)x;
    material->display_geometry[1] = (float)y;
    material->display_geometry[2] = (float)z;
    material->display_geometry[3] = (float)radius;
    return 1;
}

#define SMILE_PROCEDURAL_DISPLAY_HLSL \
    "float DisplayHash(float n){return frac(sin(n*12.9898+display.y*.173)*43758.5453);}" \
    "float3 DisplayPalette(float v,float seed){float h=DisplayHash(seed);" \
    "return .025+.58*(.5+.5*cos(6.2831853*(v+float3(h,h+.33,h+.67))));}" \
    "float3 DisplayPattern(float3 q,float t,float clip){" \
    "float mode=fmod(clip+floor(DisplayHash(floor(clip/4))*4),4);" \
    "float phase=DisplayHash(clip+7)*6.2831853;float f=0;float glow=0;" \
    "if(mode<.5){f=q.y*2.4+.4*sin(q.x*4+t*.8)+.25*cos(q.z*5-t*.5);" \
    "glow=pow(.5+.5*sin(f*8-t*1.6),3);}" \
    "else if(mode<1.5){f=sin(q.x*4+t)+sin(q.y*5-t*.7)+sin(q.z*4+t*.5);" \
    "f+=.5*sin(length(q+float3(sin(t*.3),cos(t*.4),0))*8-t);glow=.5+.5*sin(f*2);}" \
    "else if(mode<2.5){float3 center=float3(sin(t*.4+phase),cos(t*.3),cos(t*.4+phase));" \
    "f=length(q-center)*3-t*.6;glow=pow(.5+.5*cos(f*6.2831853),8);}" \
    "else{float a=atan2(q.z,q.x);f=a*2+q.y*7-t*1.7+sin(q.y*4+t);" \
    "glow=pow(.5+.5*sin(f),5);f=f/6.2831853;}" \
    "return DisplayPalette(f*.23+t*.06,clip)*(.18+glow*1.25);}" \
    "float3 ProceduralDisplay(float3 local){float3 q=(local-displayGeometry.xyz)/displayGeometry.w;" \
    "float clip=floor(display.x/12);float blend=smoothstep(0,2,fmod(display.x,12));" \
    "return lerp(DisplayPattern(q,display.x,max(0,clip-1)),DisplayPattern(q,display.x,clip),blend)*display.z;}"
