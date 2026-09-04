#include "../../src/Smile.NativeRuntime/graphics/thermal_fire3d.h"
#include <stdio.h>
#include <limits>

static int checks, failures;
static void check(bool value, const char* name)
{
    ++checks;
    if (!value) { ++failures; printf("FAIL %s\n", name); }
}
static bool near(float a, float b) { return fabsf(a-b) < .0005f; }
static SmileThermalDynamics3D defaults()
{
    SmileThermalDynamics3D d = {};
    d.evolution[3] = 1000;
    d.turbulence[1] = .1f; d.turbulence[3] = 1;
    for (int i=0;i<3;++i) { d.bounds_min[i]=-1000; d.bounds_max[i]=1000; }
    return d;
}
struct State
{
    float p[4]={},v[4]={},s[4]={1,2,0,0},t[4]={1,1,0,0};
    bool step(const SmileThermalDynamics3D& d,float dt=.01f)
    { return smile_fire_step(p,v,s,t,17,d,dt); }
};
int main()
{
    auto d=defaults(); State a;
    a.v[0]=10; check(a.step(d),"zero force live"); check(near(a.p[0],.1f),"zero force position");
    check(near(a.v[0],10),"zero force velocity");
    a=State{}; d.gravity_buoyancy[1]=-10; a.step(d);
    check(near(a.v[1],-.1f),"gravity");
    a=State{}; d=defaults(); d.wind_drag[0]=20; a.step(d);
    check(near(a.v[0],.2f),"wind");
    a=State{}; d=defaults(); d.gravity_buoyancy[3]=30; a.step(d);
    check(near(a.v[1],.3f),"buoyancy");
    a=State{}; d=defaults(); d.wind_drag[3]=2; a.v[0]=10; State b=a;
    a.step(d,.02f); b.step(d); b.step(d);
    check(near(a.v[0],b.v[0]),"exponential drag partition");
    a=State{}; d=defaults(); d.evolution[0]=.3f; d.evolution[1]=.2f; d.evolution[2]=2;
    a.step(d); check(near(a.t[0],.997f),"cooling"); check(near(a.t[1],.998f),"dissipation");
    check(near(a.s[0],1.02f)&&near(a.s[1],2.02f),"growth both dimensions");
    a=State{}; d=defaults(); d.turbulence[0]=0; a.step(d);
    check(near(a.v[0],0)&&near(a.v[1],0)&&near(a.v[2],0),"noise disabled");
    d.turbulence[0]=100; a=State{}; b=a; a.step(d); b.step(d);
    check(near(a.v[0],b.v[0])&&near(a.v[1],b.v[1]),"noise reproducible");
    check(fabsf(a.v[0])+fabsf(a.v[1])+fabsf(a.v[2])>.001f,"one octave moves");
    d.turbulence[3]=2; b=State{}; b.step(d);
    check(fabsf(a.v[0]-b.v[0])+fabsf(a.v[1]-b.v[1])>.001f,"two octaves differ");
    check(fabsf(smile_fire_noise(.99999f,2,3,17)-smile_fire_noise(1.00001f,2,3,17))<.001f,
        "noise continuous across lattice");
    a=State{}; d=defaults(); d.evolution[3]=5; a.v[0]=1000; a.v[1]=1000; a.step(d);
    check(near(sqrtf(a.v[0]*a.v[0]+a.v[1]*a.v[1]),5),"vector speed clamp");
    a=State{}; d=defaults(); a.p[0]=999; a.v[0]=1000;
    check(!a.step(d),"world bound kill");
    a=State{}; a.p[1]=std::numeric_limits<float>::quiet_NaN(); check(!a.step(d),"NaN kill");
    a=State{}; a.v[2]=std::numeric_limits<float>::infinity(); check(!a.step(d),"infinite velocity kill");
    a=State{}; a.t[1]=0; check(!a.step(d),"zero density kill");
    a=State{}; a.s[0]=1000001; check(!a.step(d),"oversize kill");
    printf("Thermal dynamics: %d checks, %d failures\n",checks,failures);
    return failures ? 1 : 0;
}
