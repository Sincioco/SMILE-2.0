"""Original rock geometry, PBR textures, dust and PCM cues. Run with installed Blender."""
import math
import wave
from pathlib import Path
import bpy
import numpy as np
from mathutils import Vector, noise

ROOT=Path(__file__).resolve().parent.parent
ROOT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
rng=np.random.default_rng(5090)

def image(name, pixels, color=True):
    h,w,_=pixels.shape
    im=bpy.data.images.new(name,width=w,height=h,alpha=True)
    if not color: im.colorspace_settings.name='Non-Color'
    im.pixels.foreach_set(pixels.astype(np.float32).ravel())
    im.filepath_raw=str(ROOT/(name+'.png')); im.file_format='PNG'; im.save(); im.pack()
    return im

# Seamless, irregular mineral mottling: no repeated sine-wave/checker pattern.
n=2048
y,x=np.mgrid[0:n,0:n]/n

def field(cells):
    lattice=rng.uniform(-1,1,(cells,cells))
    sx=x*cells; sy=y*cells
    ix=sx.astype(int); iy=sy.astype(int)
    fx=sx-ix; fy=sy-iy
    fx=fx*fx*(3-2*fx); fy=fy*fy*(3-2*fy)
    a=lattice[iy%cells,ix%cells]*(1-fx)+lattice[iy%cells,(ix+1)%cells]*fx
    b=lattice[(iy+1)%cells,ix%cells]*(1-fx)+lattice[(iy+1)%cells,(ix+1)%cells]*fx
    return a*(1-fy)+b*fy

broad=field(7); medium=field(29); fine=field(113); grit=field(457)
h=broad*.30+medium*.19+fine*.075+grit*.024
seams=np.exp(-np.abs(medium+broad*.65)*65)*np.clip(broad+.5,0,1)
quartz=np.clip((fine+grit*.4-.35)*2,0,1)
grain=np.clip(.36+broad*.10+medium*.08+fine*.055+grit*.025-seams*.065,.12,.68)
rgba=np.ones((n,n,4)); rgba[:,:,:3]=grain[:,:,None]*np.array([1.04,.98,.86])
rgba[:,:,:3]+=quartz[:,:,None]*np.array([.13,.13,.12])
iron=np.clip((broad-medium-.35)*.13,0,.09)
rgba[:,:,0]+=iron; rgba[:,:,2]-=iron*.55
albedo=image('earth-stone',rgba)
h-=seams*.025
dx=(np.roll(h,1,1)-np.roll(h,-1,1))*9
dy=(np.roll(h,1,0)-np.roll(h,-1,0))*9
normal=np.stack((dx,dy,np.ones_like(h)),axis=2); normal/=np.linalg.norm(normal,axis=2)[:,:,None]
rgba[:,:,:3]=normal*.5+.5
normal_image=image('earth-normal',rgba,False)
mat=bpy.data.materials.new('Weathered ochre granite'); mat.use_nodes=True
bsdf=mat.node_tree.nodes.get('Principled BSDF'); bsdf.inputs['Roughness'].default_value=.86
tex=mat.node_tree.nodes.new('ShaderNodeTexImage'); tex.image=albedo
mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
tex=mat.node_tree.nodes.new('ShaderNodeTexImage'); tex.image=normal_image
nm=mat.node_tree.nodes.new('ShaderNodeNormalMap'); nm.inputs['Strength'].default_value=.65
mat.node_tree.links.new(tex.outputs['Color'],nm.inputs['Color']); mat.node_tree.links.new(nm.outputs['Normal'],bsdf.inputs['Normal'])
for index in range(3):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=5,radius=1)
    obj=bpy.context.object; obj.name=f'EarthRock{index}'
    for v in obj.data.vertices:
        p=v.co.copy(); q=p*2.1+Vector((index*3.7,1.9,-2.3))
        radius=(1+noise.noise_vector(q).x*.22+noise.noise_vector(q*2.7).y*.085
                +noise.noise_vector(q*8.1).z*.024+noise.noise_vector(q*19).x*.009)
        v.co=p*radius
        v.co.x*=1.12 if index==0 else .86
        v.co.y*=.83 if index==0 else 1.08
        # Weathered fracture faces, with local relief instead of perfectly flat cuts.
        for axis in range(3):
            lower=-.87+noise.noise_vector(q*3.1)[axis]*.027
            upper=.93+noise.noise_vector(q*3.1+Vector((4,1,7)))[axis]*.027
            v.co[axis]=max(lower,min(upper,v.co[axis]))
    obj.data.materials.append(mat)
    # Continuous spherical UVs keep the fine grain coherent across adjacent triangles.
    while obj.data.uv_layers: obj.data.uv_layers.remove(obj.data.uv_layers[0])
    uv=obj.data.uv_layers.new(name='StoneUV')
    for face in obj.data.polygons:
        coords=[]
        for vi in face.vertices:
            p=obj.data.vertices[vi].co.normalized()
            coords.append([math.atan2(p.y,p.x)/math.tau+.5,math.asin(p.z)/math.pi+.5])
        if max(c[0] for c in coords)-min(c[0] for c in coords)>.5:
            for c in coords:
                if c[0]<.5: c[0]+=1
        for li,c in zip(face.loop_indices,coords): uv.data[li].uv=c
    for p in obj.data.polygons: p.use_smooth=True
bpy.ops.object.select_all(action='SELECT')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Source/earth-rocks.blend'),compress=True)
bpy.ops.export_scene.gltf(filepath=str(ROOT/'earth-rocks.glb'),export_format='GLB',use_selection=True,
    export_animations=False,export_tangents=True,export_yup=True)

n=256
y,x=np.mgrid[0:n,0:n]/(n-1)*2-1
r=np.sqrt(x*x+y*y)
cloud=np.zeros_like(r)
for f,a in ((3,.23),(7,.13),(13,.07)):
    cloud+=a*np.sin(x*f+y*2)*np.cos(y*f-x*3)
alpha=np.clip(1-r,0,1)**1.7*np.clip(.65+cloud,0,1)
rgba=np.ones((n,n,4)); rgba[:,:,:3]=np.array([.55,.42,.28]); rgba[:,:,3]=alpha
image('earth-dust',rgba)

# Layered stone pressure, grit, cracking transients and a low impact tail.
rate=48000
for name,seconds,seed in (('earth-rise',.85,41),('earth-launch',.50,72),('earth-impact',1.25,19)):
    gen=np.random.default_rng(seed); count=int(seconds*rate); t=np.arange(count)/rate
    raw=gen.uniform(-1,1,count)
    low=np.convolve(raw,np.ones(45)/45,mode='same')
    grit=np.convolve(raw,np.ones(3)/3,mode='same')
    if name=='earth-rise':
        envelope=np.minimum(1,t/.035)*np.exp(-t*3.5)
        signal=(.65*low+.13*grit+.14*np.sin(math.tau*(55*t-13*t*t)))*envelope
    elif name=='earth-launch':
        envelope=np.sin(math.pi*np.minimum(1,t/seconds))**1.5
        signal=(.6*low+.07*grit)*envelope
    else:
        signal=(.8*low+.15*grit+.21*np.sin(math.tau*(62*t-14*t*t)))*np.exp(-t*5)
        for at in (.0,.027,.06,.11,.19,.32):
            delta=np.maximum(0,t-at); signal+=.15*raw*np.exp(-delta*95)*(t>=at)
        signal*=np.minimum(1,t/.002)
    signal=np.tanh(signal*2)
    signal*=.86/max(.86,float(np.max(np.abs(signal))))
    stereo=np.stack((signal,np.roll(signal,73)*.94),axis=1)
    with wave.open(str(ROOT/(name+'.wav')),'wb') as out:
        out.setnchannels(2); out.setsampwidth(2); out.setframerate(rate)
        out.writeframes((stereo*32767).astype('<i2').tobytes())
print('EARTH_ASSETS_READY',flush=True)
