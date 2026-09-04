#include "../../src/Smile.NativeRuntime/graphics/graphics3d.h"
#include <stdio.h>
#include <windows.h>

extern "C" void smile_game_open(const char*,long long,long long,long long);
extern "C" void smile_graphics_configure(long long,long long);
extern "C" void smile_show_screen(void);
extern "C" void smile_media_shutdown(void);

static int failures;
static long long command(long long n,long long a=0,long long b=0,long long c=0,long long d=0,
    long long e=0,long long f=0,long long g=0,long long h=0,long long i=0,long long j=0)
{ return smile_renderer3d_command(n,a,b,c,d,e,f,g,h,i,j); }
static void check(bool ok,const char* name)
{ if(!ok){++failures;printf("FAIL %s\n",name);} }
static void spawn(long long system,long long serial)
{
    check(command(127,2,system,0,0,0,20,50,70,0)!=0,"spawn kinematics");
    check(command(127,3,system,0,500,20,30,0,0,1000,800)!=0,"spawn visual");
    check(command(127,4,system,0,serial,17)!=0,"spawn commit");
    check(command(127,5,system,20)!=0,"compute advance");
}
static void performance_matrix()
{
    // A bounded renderer-only matrix. These are full-capacity thermal systems,
    // not a claim that the high-level five-layer preset fits four times on GPU.
    const int counts[]={1024,1024,4096,16384};
    for(int scenario=0;scenario<4;++scenario) for(int mode=0;mode<8;++mode) {
        command(SMILE_3D_RESET);
        const bool hdr=(mode&1)!=0,depth=(mode&2)!=0,heat=(mode&4)!=0;
        command(SMILE_3D_CONFIGURE_POST,hdr,hdr,hdr,100,1400,35,2,2,4);
        command(125,1,depth?1:0);
        command(126,1,heat?1:0,2);
        command(SMILE_3D_SET_CAMERA,0,130,-600,0,80,20,55,1,5000);
        command(SMILE_3D_SET_CAMERA_UP,0,1,0);
        const auto material=command(SMILE_3D_CREATE_MATERIAL,0,3,255,255,255,10,1,150,0);
        command(125,2,material,1,0);
        const auto floor_mesh=command(SMILE_3D_CREATE_PRIMITIVE,2,800,800);
        const auto floor=command(SMILE_3D_CREATE_OBJECT,floor_mesh);
        check(floor!=0,"performance floor creation");
        command(SMILE_3D_SET_POSITION,floor,0,-5,20);
        command(SMILE_3D_SET_COLOR,floor,25,25,25,100);
        long long heat_system=0;
        if(heat) {
            const auto heat_material=command(SMILE_3D_CREATE_MATERIAL,0,3,255,255,255,100,1,100,0);
            check(command(126,2,heat_material,12,160,30,20,100)!=0,"performance heat material");
            heat_system=command(127,1,64,heat_material,2,10);
            check(heat_system!=0,"performance heat reservation");
            for(int p=0;p<64;++p) {
                command(127,2,heat_system,p,(p%8)*15-50,35+(p/8)*15,20,0,5,0);
                command(127,3,heat_system,p,10000,30,60,0,0,800,250);
                check(command(127,4,heat_system,p,p+1,171+p)!=0,"performance heat spawn");
            }
        }
        long long systems[4]={};const int system_count=scenario==1?4:1;
        const int capacity=counts[scenario]/system_count;
        for(int s=0;s<system_count;++s) {
            systems[s]=command(127,1,capacity,material,2,10);
            check(systems[s]!=0,"performance reservation");
            command(127,11,systems[s],0,0,0,20,0,0,50,700);
            command(127,12,systems[s],70,10000,600,2);
            command(127,13,systems[s],100,0,1,500);
            command(127,14,systems[s],-1000,-1000,-1000,1000,1000,1000);
            command(127,15,systems[s],1,1,1,1);
            for(int p=0;p<capacity;++p) {
                command(127,2,systems[s],p,(p%32)*5-80+s*65,0,20,0,20,0);
                command(127,3,systems[s],p,10000,8,16,0,0,950,120);
                check(command(127,4,systems[s],p,p+1,17+p)!=0,"performance spawn");
                if((p+1)%256==0) command(127,5,systems[s],10);
            }
        }
        LARGE_INTEGER frequency,start,finish;QueryPerformanceFrequency(&frequency);
        long long dispatch_start=0,draw_start=0,upload_start=0;
        for(int frame=0;frame<40;++frame) {
            if(frame==10) {
                QueryPerformanceCounter(&start);
                dispatch_start=command(127,10,14);draw_start=command(127,10,15);
                upload_start=command(127,10,16);
            }
            for(int s=0;s<system_count;++s) check(command(127,5,systems[s],10)!=0,"performance advance");
            if(heat_system) check(command(127,5,heat_system,10)!=0,"performance heat advance");
            check(command(SMILE_3D_BEGIN,0,0,0)!=0,"performance begin");
            check(command(SMILE_3D_DRAW,floor)!=0,"performance opaque depth source");
            for(int s=0;s<system_count;++s) check(command(127,7,systems[s])!=0,"performance draw");
            if(heat_system) check(command(127,7,heat_system)!=0,"performance heat draw");
            check(command(SMILE_3D_END)!=0,"performance end");smile_show_screen();
        }
        QueryPerformanceCounter(&finish);
        check(command(117,13)==4,"4x MSAA retained with HDR/LDR and heat on/off");
        if(heat) check(command(126,3,2)!=0,"heat remains effective alongside MSAA");
        printf("PERF requested=1920x1080 target=%lldx%lld scenario=%d flame-slots=%d flame-systems=%d heat-slots=%d HDR=%d depth=%d heat=%d submit+present-ms=%.3f dispatches=%lld draws=%lld warm-upload-bytes=%lld state-bytes=%lld target-bytes=%lld GPU-timer=unavailable\n",
            command(117,14),command(117,15),scenario,counts[scenario],system_count,heat?64:0,hdr,depth,heat,
            (finish.QuadPart-start.QuadPart)*1000.0/frequency.QuadPart/30,
            command(127,10,14)-dispatch_start,command(127,10,15)-draw_start,
            command(127,10,16)-upload_start,command(127,10,17),command(117,27));
        check(command(127,10,19)==0,"performance no readback");
        for(int s=0;s<system_count;++s) check(command(127,9,systems[s])!=0,"performance destroy");
        if(heat_system) check(command(127,9,heat_system)!=0,"performance heat destroy");
        check(command(127,10,17)==0&&command(127,10,8)==0,"performance exact teardown");
    }
}
int main()
{
    smile_graphics_configure(2,0);
    smile_game_open("Thermal GPU Recovery",20,1920,1080);
    command(SMILE_3D_RESET);
    check(command(127,10,22)==1,"thermal capability initializes pipeline");
    const auto material=command(SMILE_3D_CREATE_MATERIAL,0,3,255,255,255,100,1,150,0);
    const auto system=command(127,1,1024,material,2,10);
    check(material!=0&&system!=0,"resource reservation");
    check(command(127,11,system,0,0,0,20,0,0,50,700)!=0,"forces");
    check(command(127,12,system,100,10000,600,2)!=0,"two octave turbulence");
    check(command(127,13,system,300,200,5,500)!=0,"evolution");
    check(command(127,14,system,-1000,-1000,-1000,1000,1000,1000)!=0,"bounds");
    check(command(127,15,system,1,1,4,4)!=0,"thermal render");
    spawn(system,1);
    check(command(127,10,38,system)==2,"GPU backend");
    check(command(127,10,14)==2,"fixed dispatch count");
    check(command(127,10,55,system,0)==-1,"no pretend GPU temperature readback");
    check(command(SMILE_3D_SET_CAMERA,0,80,-250,0,20,20,60,1,5000)!=0,"camera position");
    check(command(SMILE_3D_SET_CAMERA_UP,0,1,0)!=0,"camera up");
    check(command(SMILE_3D_BEGIN,0,0,0)!=0,"begin frame");
    check(command(127,7,system)!=0,"queue fire");
    check(command(127,11,system,0,0,0,0,0,0,0,0)==0,"in-flight dynamics rejected");
    check(command(127,9,system)==0,"in-flight teardown rejected");
    check(command(SMILE_3D_END)!=0,"render thermal shader");
    smile_show_screen();
    const auto bytes=command(127,10,17);
    check(bytes==1024*160+144,"exact GPU state bytes");
    // Exercise the actual renderer's device-loss callback, not a new production ABI.
    smile_graphics3d_on_device_lost();
    check(command(127,8,system)==1,"logical handle survives device loss");
    check(command(127,10,31,system)==0,"old visual tail cleared on loss");
    check(command(127,10,18)==1,"one restart recorded");
    check(command(127,10,17)==0,"lost GPU allocation released");
    spawn(system,2);
    check(command(127,10,38,system)==2,"GPU recreated after callback");
    check(command(127,10,17)==bytes,"exact allocation after recreation");
    check(command(127,10,51,system)==1,"thermal configuration survives recovery");
    check(command(127,9,system)!=0,"destroy recovered system");
    check(command(127,10,1)==0&&command(127,10,8)==0&&command(127,10,17)==0,"exact teardown");
    check(command(127,10,19)==0,"zero particle readbacks");
    performance_matrix();
    command(SMILE_3D_RESET);
    smile_media_shutdown();
    printf("Native thermal GPU recovery: %d failures\n",failures);
    return failures?1:0;
}
