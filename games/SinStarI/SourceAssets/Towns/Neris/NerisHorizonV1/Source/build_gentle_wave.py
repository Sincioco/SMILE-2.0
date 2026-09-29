"""Approved Gentle Wave r005: isolated revision, retained side buildings, clear front."""
import bpy, sys, math, json, hashlib
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT/'Revisions/r005'
OUT.mkdir(parents=True, exist_ok=True)
(OUT/'Native').mkdir(exist_ok=True)
(ROOT/'Previews/r005').mkdir(exist_ok=True)
sys.path.insert(0, str(Path(__file__).parent))
import forms as F
from forms import box, beam, panel, mesh, cone, dish

bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Source/Neris-Horizon-r004.blend'))
source = bpy.context.scene
source.frame_set(1500)
scene = bpy.data.scenes.new('Horizon Gentle Wave r005')
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
palette = ('Ivory','Gold','Teal','Glass','Glass Dark','Glass Highlight','Metal',
           'Paving','Joint','Light','Warm Light','Leaves','Soil')
for name in palette:
    mat = bpy.data.materials[name].copy()
    mat.name = 'GW '+name
    F.MATERIALS[name] = mat
for name in ('Glass','Glass Highlight'):
    mat=F.MATERIALS[name]
    node=mat.node_tree.nodes.get('Principled BSDF')
    node.inputs['Base Color'].default_value=(.055,.24,.30,1)
    node.inputs['Metallic'].default_value=.12
    node.inputs['Roughness'].default_value=.24
    node.inputs['Alpha'].default_value=.38 if name=='Glass' else .48
    node.inputs['Emission Strength'].default_value=.04
    mat.surface_render_method='DITHERED'
    mat.use_backface_culling=True
F.MATERIALS['Ivory'].node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.76,.74,.68,1)
F.MATERIALS['Paving'].node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.28,.30,.31,1)
F.MATERIALS['Warm Light'].node_tree.nodes['Principled BSDF'].inputs['Emission Strength'].default_value=2
retained={}

def reuse(prefix, old, new, family):
    before=[]
    transform=Matrix.Translation(Vector(new)) @ Matrix.Rotation(math.pi,4,'Z') @ Matrix.Translation(-Vector(old))
    for original in source.objects:
        if original.type!='MESH' or not original.name.startswith(prefix):continue
        obj=original.copy();obj.data=original.data.copy();obj.parent=None
        obj.animation_data_clear()
        obj.data.transform(transform @ original.matrix_world)
        obj.matrix_world=Matrix.Identity(4)
        for i,mat in enumerate(obj.data.materials):
            obj.data.materials[i]=F.MATERIALS[mat.name.split('.')[0]]
        obj.name=original.name
        obj['assembly']=family
        obj['horizon_asset']=True
        if 'fleet' in obj:obj['fleet_origin']=list(new)
        scene.collection.objects.link(obj)
        before.append(obj)
    retained[family]=len(before)
    return before

# All side structures stay entirely outside the terminal's x=[-260,260] frontage.
reuse('Transport Hangar',(-82,-121,0),(-365,108,0),'Transport Hangar')
reuse('Royal Hangar',(148,-126,0),(365,108,0),'Royal Hangar')
reuse('Transport Hall Bay',(-224,46,0),(-365,280,0),'Transport Glass Halls')
reuse('Royal Pavilion',(235,73,0),(365,280,0),'Royal Glass Hall')
reuse('Transport Display Craft',(-82,51,0),(-365,-90,0),'Transport Aircraft')
reuse('Royal Display Craft',(148,68,0),(365,-90,0),'Royal Aircraft')

# A continuous platform, not a rear runway. Aircraft aprons remain on the sides.
box('Waterfront Foundation',(0,100,-6),(960,600,12),'Paving')['assembly']='Platform'
for x in range(-480,481,20):
    box('Paving Expansion Joint',(x,100,.012),(.045,598,.016),'Joint')
for y in range(-200,401,20):
    box('Paving Expansion Joint',(0,y,.023),(958,.045,.016),'Joint')
for x in (-479,479):
    box('Quay Coping',(x,100,.22),(1.5,600,.4),'Ivory')
for x in (-365,365):
    for edge in (-84,84):
        beam('Side Apron Boundary',(x+edge,-174,.13),(x+edge,40,.13),.32,'Gold',.1)

terminal_start=set(scene.objects)
def roof_z(x,y=0):
    return 53 + 24*math.exp(-(x/90)**2) + 10*math.exp(-((abs(x)-228)/53)**2) + 2*math.sin(math.pi*max(0,min(150,y))/150)

box('Terminal Ground Floor',(0,75,.4),(520,150,.8),'Ivory')
box('Terminal Upper Concourse',(0,98,25),(516,100,1.2),'Ivory')
box('Terminal Rear Service Wall',(0,148,25),(515,1.5,50),'Ivory')
for x in range(-234,235,26):
    for y in (52,104):
        box('Interior Column',(x,y,25),(1.6,1.6,50),'Ivory')
for x in range(-260,261,26):
    h=roof_z(x)
    box('Facade Ivory Column',(x,-.6,h/2),(2.5,3.2,h),'Ivory')
    box('Rear Ivory Column',(x,150,h/2),(2.5,3.2,h),'Ivory')
    for y in range(0,150,15):
        beam('Roof Structural Rib',(x,y,roof_z(x,y)-1.3),(x,y+15,roof_z(x,y+15)-1.3),.9,'Ivory')

# Single glazing planes: no duplicated coplanar inner walls.
for x in range(-260,260,13):
    for y,reverse in ((-1.0,False),(151.0,True)):
        heights=[.9,12.5,25.7,39,roof_z(x,y)-1]
        for lo,hi in zip(heights,heights[1:]):
            if y<0 and abs(x+6.5)<26 and lo<12.5:continue
            top_a=hi;top_b=roof_z(x+13,y)-1 if hi==heights[-1] else hi
            corners=[(x+.18,y,lo),(x+12.82,y,lo),(x+12.82,y,top_b),(x+.18,y,top_a)]
            panel('Terminal Glazing',list(reversed(corners)) if reverse else corners,'Glass')
        beam('Facade Fine Mullion',(x,y-.15,.9),(x,y-.15,roof_z(x,y)-1),.28,'Teal')
        for z in (12.5,25.7,39):
            beam('Facade Transom',(x,y-.15,z),(x+13,y-.15,z),.25,'Teal')
for x in (-260,260):
    for y in range(0,150,15):
        h=roof_z(x,y)-1
        points=[(x,y,.9),(x,y+15,.9),(x,y+15,h),(x,y,h)]
        panel('Terminal End Glazing',points if x>0 else list(reversed(points)),'Glass')
        beam('Terminal End Mullion',(x,y,.9),(x,y,h),.35,'Teal')
        beam('Terminal End Transom',(x,y,25.7),(x,y+15,25.7),.3,'Teal')

# Closed roof volume with three genuine openings filled by discrete skylight panels.
roof_vertices=[];roof_faces=[]
for x in range(-265,265,5):
    skylight=abs(x+2.5)<15 or 205<abs(x+2.5)<225
    for y in range(-5,155,10):
        top=[(x,y,roof_z(x,y)),(x+5,y,roof_z(x+5,y)),(x+5,y+10,roof_z(x+5,y+10)),(x,y+10,roof_z(x,y+10))]
        if skylight and 5<=y<145:
            panel('Roof Skylight',top,'Glass Highlight')
        else:
            start=len(roof_vertices);roof_vertices.extend(top+[(a,b,c-1.3) for a,b,c in top])
            roof_faces.extend(tuple(start+i for i in face) for face in ((0,1,2,3),(7,6,5,4),(0,4,5,1),(2,6,7,3)))
    for y in (-5,155):
        beam('Wave Fascia',(x,y,roof_z(x,y)-.2),(x+5,y,roof_z(x+5,y)-.2),1.4,'Ivory',1.8)
        beam('Wave Gold Edge',(x,y-.93,roof_z(x,y)-.85),(x+5,y-.93,roof_z(x+5,y)-.85),.16,'Gold')
mesh('Gentle Wave Roof',roof_vertices,roof_faces,'Ivory')
for x in (-225,-205,-15,15,205,225):
    for y in range(5,145,10):
        beam('Skylight Edge',(x,y,roof_z(x,y)+.08),(x,y+10,roof_z(x,y+10)+.08),.32,'Teal')

# Passenger facilities remain simple actual geometry visible through the glass.
for level in (1.0,26.0):
    for x in (-210,-145,-80,80,145,210):
        for y in (60,112):
            box('Waiting Bench Base',(x,y,level+1.1),(17,2,.45),'Metal')
            for seat in range(6):
                xx=x-7.5+seat*3
                box('Waiting Seat',(xx,y,level+1.42),(2.5,2,.35),'Teal')
                box('Waiting Seat Back',(xx,y+.8,level+2.2),(2.5,.3,1.5),'Teal')
    for x in (-200,-130,-60,60,130,200):
        box('Terminal Ceiling Light',(x,76,level+22),(33,.35,.18),'Warm Light')
for x in (-120,-80,80,120):
    box('Passenger Counter',(x,31,2.4),(24,4,3.2),'Ivory')
    box('Counter Teal Fascia',(x,28.97,2.5),(24,.12,1.5),'Teal')
    box('Departure Display',(x,34,10),(18,.4,3.2),'Teal')
    box('Departure Display Glow',(x,33.76,10),(16,.04,1.9),'Light')
for x in (-190,190):
    for i in range(40):
        box('Concourse Stair',(x,43+i*.8,.8+(i+1)*.605/2),(8,.8,(i+1)*.605),'Ivory')
    for side in (-4.3,4.3):
        beam('Stair Handrail',(x+side,43,2.4),(x+side,75,27),.17,'Gold')
beam('Upper Concourse Handrail',(-255,47.5,27),(255,47.5,27),.22,'Gold')
for x in range(-250,251,10):
    box('Upper Concourse Baluster',(x,47.5,26),( .16,.16,2.4),'Metal')

# Clear passenger entrance: canopy only, no objects across the forecourt.
box('Entrance Canopy',(0,-13,12),(82,26,1.4),'Ivory')
box('Canopy Gold Trim',(0,-26.1,11.9),(82,.22,.35),'Gold')
for x in (-36,36):
    box('Entrance Canopy Support',(x,-20,5.8),(1.7,1.7,11.6),'Ivory')
for x in range(-24,25,8):
    panel('Entrance Glass Door',[(x-3.7,-1.5,.8),(x+3.7,-1.5,.8),(x+3.7,-1.5,10.5),(x-3.7,-1.5,10.5)],'Glass')
    beam('Entrance Door Jamb',(x-3.9,-1.7,.8),(x-3.9,-1.7,11),.32,'Gold')
    box('Door Pull',(x+2.6,-1.95,4.4),(.2,.3,1.8),'Gold')
for i in range(4):
    box('Entrance Step',(0,-30+i*2,.1*(i+1)),(66,2,.2*(i+1)),'Ivory')
for x in (-42,42):
    mesh('Accessible Entrance Ramp',[(x-4,-36,0),(x+4,-36,0),(x+4,-1,.8),(x-4,-1,.8)],[(0,1,2,3)],'Ivory')

def lettering(body,position,size):
    data=bpy.data.curves.new(body,'FONT');data.body=body;data.size=size;data.align_x='CENTER';data.extrude=.03
    obj=bpy.data.objects.new(body,data);scene.collection.objects.link(obj)
    obj.location=position;obj.rotation_euler=(math.pi/2,0,0);obj.data.materials.append(F.MATERIALS['Teal'])
    bpy.context.view_layer.objects.active=obj;obj.select_set(True)
    bpy.ops.object.convert(target='MESH');obj.select_set(False)
    return obj
box('Horizon Name Panel',(238,-2.4,24),(35,1.2,46),'Ivory')
lettering('HORIZON',(238,-3.04,26),4)
lettering('SPACEPORT',(238,-3.04,20),3)
lettering('NERIS',(238,-3.04,15),2.2)
for obj in set(scene.objects)-terminal_start:obj['assembly']='Gentle Wave Terminal'

# Centered rear control tower. Its ground-level base is physically behind the terminal.
tower_start=set(scene.objects);tx,ty=0,230
box('Tower Foundation',(tx,ty,1.2),(54,54,2.4),'Ivory')
box('Tower Shaft',(tx,ty,94),(44,44,184),'Ivory')
for sign in (-1,1):
    box('Tower Vertical Glazing',(0,ty+sign*22.1,96),(12,.15,172),'Glass Dark')
    for x in (-6.5,6.5):box('Tower Gold Mullion',(x,ty+sign*22.2,96),(.4,.2,172),'Gold')
cone('Control Room Lower Balcony',(0,ty,187),32,3,'Ivory',top=32,sides=8)
for i in range(8):
    a,b=(i*math.tau/8,(i+1)*math.tau/8)
    bottom=[(27*math.cos(t),ty+27*math.sin(t),189) for t in (a,b)]
    top=[(35*math.cos(t),ty+35*math.sin(t),219) for t in (a,b)]
    panel('Control Room Glass',[bottom[0],bottom[1],top[1],top[0]],'Glass')
    for fraction in (0,.5):
        beam('Control Room Mullion',Vector(bottom[0]).lerp(Vector(bottom[1]),fraction),Vector(top[0]).lerp(Vector(top[1]),fraction),.65,'Teal')
    beam('Control Room Gold Edge',top[0],top[1],.45,'Gold')
cone('Control Room Roof',(0,ty,221),36,3,'Ivory',top=36,sides=8)
cone('Control Roof Gold Band',(0,ty,222.65),36.15,.3,'Gold',top=36.15,sides=8)
for i in range(32):
    a=i*math.tau/32;b=(i+1)*math.tau/32
    beam('Roof Rail Post',(29*math.cos(a),ty+29*math.sin(a),223),(29*math.cos(a),ty+29*math.sin(a),226),.23,'Metal')
    beam('Roof Rail',(29*math.cos(a),ty+29*math.sin(a),226),(29*math.cos(b),ty+29*math.sin(b),226),.2,'Metal')
for x,height in ((-17,33),(0,62),(17,42)):
    beam('Communication Antenna',(x,ty+4,223),(x,ty+4,223+height),.5,'Metal')
dish('Communication Dish',(15,ty-8,244),5.5,(0,-1,.1))
for obj in set(scene.objects)-tower_start:obj['assembly']='Centered Rear Tower'

# Four unobtrusive ceiling-mounted sources. No exterior light poles or masts.
for x in (-195,-65,65,195):
    data=bpy.data.lights.new('Terminal Recessed Lighting','AREA');data.energy=45000;data.shape='DISK';data.size=35;data.color=(1,.84,.62)
    obj=bpy.data.objects.new(data.name,data);scene.collection.objects.link(obj);obj.location=(x,72,roof_z(x,72)-4)
    obj['horizon_internal_light']=True

bpy.context.view_layer.update()
asset=[o for o in scene.objects if o.type=='MESH']
for obj in asset:obj['horizon_asset']=True
points=[o.matrix_world@Vector(p) for o in asset for p in o.bound_box]
bounds={'min':[min(p[a] for p in points) for a in range(3)],'max':[max(p[a] for p in points) for a in range(3)]}
assert not any('Floodlight' in o.name or 'Lamp Head' in o.name for o in asset)
for obj in asset:
    if obj.get('assembly') in retained:
        pts=[obj.matrix_world@Vector(p) for p in obj.bound_box]
        assert max(p.x for p in pts)<-275 or min(p.x for p in pts)>275, obj.name

# Export each family by the existing static material writer; do not mutate r004.
sys.path.insert(0,str(ROOT.parent/'NerisTownV1/Source'))
import static_glb
groups={'Horizon':{},'Transport':{},'Royal':{}}
deps=bpy.context.evaluated_depsgraph_get()
for obj in asset:
    evaluated=obj.evaluated_get(deps);data=evaluated.to_mesh();data.calc_loop_triangles()
    transform=obj.matrix_world;normal=transform.to_3x3().inverted().transposed()
    family=obj.get('fleet','Horizon');origin=Vector(obj.get('fleet_origin',(0,0,0)))
    for tri in data.loop_triangles:
        mat=data.materials[tri.material_index];bucket=groups[family].setdefault(mat.name,[mat,[]])
        bucket[1].append(tuple((tuple(transform@data.vertices[v].co-origin),tuple((normal@tri.normal).normalized())) for v in tri.vertices))
    evaluated.to_mesh_clear()
for family,mats in groups.items():
    static_glb.write(OUT/f'Native/{family}.glb',[(name,mat,tris,None) for name,(mat,tris) in sorted(mats.items())])
report={'revision':'r005','design':'Gentle Wave','terminal_m':[520,150,79],
        'site_m':[960,600],'tower_m':{'height':285,'shaft_width':44,'cab_width':70,'center':[0,230]},
        'retained_objects':retained,'bounds_m':bounds,'models':3,
        'parts':{k:len(v) for k,v in groups.items()},
        'triangles':{k:sum(len(v[1]) for v in values.values()) for k,values in groups.items()},
        'light_poles':0,'frontage_obstructions':0,'glass':'Single alpha-blended planes with modeled interior'}
(OUT/'asset-manifest.json').write_text(json.dumps(report,indent=2))
(OUT/'checksums.json').write_text(json.dumps({f'Native/{f}.glb':hashlib.sha256((OUT/f'Native/{f}.glb').read_bytes()).hexdigest() for f in groups},indent=2))

# Authoritative Blender review cameras; backdrop is excluded from exported geometry.
F.material('Preview Water',(.035,.14,.20),.15,.32)
box('Preview Water',(0,0,-.3),(10000,10000,.15),'Preview Water')
scene.world=bpy.data.worlds.new('Horizon Daylight');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.42,.57,.70,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.65
data=bpy.data.lights.new('Sun','SUN');data.energy=2.5;data.angle=.12
sun=bpy.data.objects.new('Sun',data);scene.collection.objects.link(sun);sun.rotation_euler=(.35,-.5,-.5)
views={
 'Hero':((620,-1000,460),(0,95,85),1120),
 'Front':((0,-1300,142),(0,100,142),1080),
 'Terminal':((80,-1050,200),(0,90,132),680),
 'Top':((0,100,1500),(0,100,0),1120),
 'Left':((-1300,100,142),(0,100,142),760),
 'Right':((1300,100,142),(0,100,142),760),
 'Rear':((0,1400,170),(0,100,140),1080)}
for name,(eye,target,width) in views.items():
    data=bpy.data.cameras.new(name);obj=bpy.data.objects.new('Review '+name,data);scene.collection.objects.link(obj)
    obj.location=eye;obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    data.type='ORTHO';data.ortho_scale=width;data.clip_end=20000
scene.camera=scene.objects['Review Hero']
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x=1600;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX';scene.render.image_settings.file_format='PNG'
scene['asset_identity']='sin-star-i.neris-horizon-v1';scene['revision']='r005'
scene['approved_concept']='exec-34eb51a2-105c-47db-8386-a313c906b813.png'
for obj in scene.objects:obj.select_set(False)
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            space=area.spaces.active;space.clip_end=20000;space.overlay.show_overlays=False;space.shading.type='MATERIAL'
            space.region_3d.view_perspective='CAMERA';space.region_3d.view_camera_zoom=0
for other in list(bpy.data.scenes):
    if other!=scene:bpy.data.scenes.remove(other)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Source/Neris-Horizon-Gentle-Wave-r005.blend'),compress=True)
for name in ('Hero','Front','Terminal','Top','Left','Right','Rear'):
    scene.camera=scene.objects['Review '+name];scene.render.filepath=str(ROOT/f'Previews/r005/{name}.png')
    bpy.ops.render.render(write_still=True)
print('PASS GENTLE WAVE '+json.dumps(report),flush=True)
