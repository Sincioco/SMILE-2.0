"""Rebuild the approved Neris Horizon concept using installed Blender, no downloads.

Run through Blender MCP in Object mode; it preserves the original scene.
"""
import sys
import json
import math
from pathlib import Path
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0,str(Path(__file__).parent))
from forms import material, box, beam, cone, star, panel, dish, ship, mesh

assert bpy.context.mode == 'OBJECT', 'Switch to Object mode before authoring.'
scene = bpy.data.scenes.new('Neris Horizon r002')
bpy.context.window.scene = scene
for name in ('Source','Native','Previews','Reference'):
    (ROOT/name).mkdir(exist_ok=True)
material('Ivory',(.78,.74,.64),.12,.42)
material('Gold',(.58,.36,.105),.62,.27)
material('Teal',(.018,.13,.17),.36,.3)
material('Glass',(.035,.23,.31),.53,.19)
material('Glass Dark',(.017,.09,.13),.42,.23)
material('Glass Highlight',(.12,.37,.45),.48,.24)
material('Metal',(.055,.065,.075),.5,.44)
material('Paving',(.40,.42,.43),.04,.79)
material('Joint',(.22,.25,.27),.08,.72)
material('Light',(.08,.65,.95),.15,.23,1)
material('Warm Light',(.95,.79,.42),.1,.3,1)
material('Leaves',(.035,.105,.032),0,.85)
material('Soil',(.065,.05,.032),0,.96)

# Top is at local Z=0; all buried pier structure remains beneath harbor water.
box('Waterfront Foundation',(0,0,-7),(540,450,14),'Paving')
box('Arrival Causeway',(0,-307.5,-3),(40,165,6),'Paving')
for x in (-269,269):
    box('Quay Edge',(x,0,.12),(2,450,.24),'Ivory')
box('Sea Edge',(0,224,.12),(540,2,.24),'Ivory')
for x in range(-255,256,15):
    box('Paving Joint',(x,0,.018),(.075,448,.025),'Joint')
for y in range(-210,226,15):
    box('Paving Joint',(0,y,.025),(538,.075,.03),'Joint')
for y in range(-380,-230,15):
    box('Causeway Joint',(0,y,.025),(38,.08,.03),'Joint')


def apron(label,x,y,w,d):
    # Chamfered rectangles remain flat and directly aligned with open hangars.
    c = 8
    points = [(x-w/2+c,y-d/2),(x+w/2-c,y-d/2),(x+w/2,y-d/2+c),
              (x+w/2,y+d/2-c),(x+w/2-c,y+d/2),(x-w/2+c,y+d/2),
              (x-w/2,y+d/2-c),(x-w/2,y-d/2+c)]
    for i,a in enumerate(points):
        b = points[(i+1)%8]
        beam(label+' Guidance',(a[0],a[1],.11),(b[0],b[1],.11),.65,'Light',.13)
    star(label+' Landing Mark',x,y+54,.08,10)
    for yy in range(round(y-d/2),round(y+d/2),12):
        box(label+' Center Line',(x,yy,.07),(.35,4,.06),'Gold')


def hangar(label,x,y,w,d,h):
    front,back = y+d/2,y-d/2
    for side in (-1,1):
        box(label+' Wall',(x+side*(w/2-1),y,h/2),(2,d,h),'Ivory')
    box(label+' Back',(x,back+1,h/2),(w,2,h),'Ivory')
    box(label+' Floor',(x,y,.06),(w-4,d-2,.12),'Joint')
    for side in (-1,1):
        a,b=(x,back-3,h+10),(x+side*(w/2+3),back-3,h)
        c,e=(x+side*(w/2+3),front+3,h),(x,front+3,h+10)
        panel(label+' Roof',[a,b,c,e],'Teal')
        beam(label+' Roof Edge',(x,front+3,h+10),(x+side*(w/2+3),front+3,h),2.5,'Ivory')
        beam(label+' Gold Roof',(x,front+3.1,h+10.8),(x+side*(w/2+3),front+3.1,h+.8),.45,'Gold')
        box(label+' Portal',(x+side*(w/2-2),front,h/2),(4,5,h),'Ivory')
        for rib in range(6):
            yy=back+6+rib*(d-12)/5
            xx=x+side*(w/2-3.2)
            beam(label+' Inner Rib',(xx,yy,1),(xx,yy,h-1),1.3,'Metal')
            beam(label+' Rafter',(xx,yy,h-1),(x,yy,h+8),1.1,'Metal')
            beam(label+' Ceiling Strip',(x+side*9,yy,h+5),(x+side*w*.30,yy,h+1),.7,'Warm Light')
    box(label+' Header',(x,front,h-1),(w,5,3),'Ivory')
    star(label+' Crest',x,front+3.05,h+4,3,True)
    # Equipment stays against the rear wall, outside the ship clearance volume.
    for i in range(5):
        box(label+' Service Cabinet',(x-w*.35+i*w*.175,back+4,2),(4,4,4),'Metal')
    return {'name':label,'center':[x,y],'clear_width':w-8,'clear_depth':d-10,'clear_height':h-3}


def glass_wall(label,a,b,height):
    ax,ay=a; bx,by=b
    length=math.hypot(bx-ax,by-ay)
    count=max(1,round(length/7))
    for i in range(count):
        t,u=i/count,(i+1)/count
        x1,y1=ax+(bx-ax)*t,ay+(by-ay)*t
        x2,y2=ax+(bx-ax)*u,ay+(by-ay)*u
        panel(label+' Glass',[(x1,y1,2),(x2,y2,2),(x2,y2,height),(x1,y1,height)],
              'Glass Highlight' if i%5==1 else 'Glass')
        beam(label+' Mullion',(x1,y1,1.5),(x1,y1,height),.45,'Gold')
    beam(label+' Sill',(ax,ay,1.5),(bx,by,1.5),1.4,'Ivory')
    beam(label+' Midrail',(ax,ay,height*.44),(bx,by,height*.44),.3,'Gold')
    beam(label+' Head',(ax,ay,height),(bx,by,height),1.5,'Ivory')


def terminal_bay(label,x,y,w,d,h,rise):
    box(label+' Plinth',(x,y,.55),(w+3,d+2,1.1),'Ivory')
    glass_wall(label,(x-w/2,y+d/2),(x+w/2,y+d/2),h)
    glass_wall(label,(x-w/2,y-d/2),(x-w/2,y+d/2),h)
    glass_wall(label,(x+w/2,y+d/2),(x+w/2,y-d/2),h)
    glass_wall(label,(x+w/2,y-d/2),(x-w/2,y-d/2),h)
    for xx in (x-w/2,x+w/2):
        for yy in (y-d/2,y+d/2):
            box(label+' Column',(xx,yy,h/2),(1.7,1.7,h),'Ivory')
    # Roof slopes in the depth direction. Raised rear clerestory is also glazing.
    roof=[(x-w/2-3,y-d/2-2,h+rise),(x+w/2+3,y-d/2-2,h+rise),
          (x+w/2+3,y+d/2+3,h+1),(x-w/2-3,y+d/2+3,h+1)]
    panel(label+' Roof',roof,'Ivory')
    for i in range(4):
        beam(label+' Fascia',roof[i],roof[(i+1)%4],1.8,'Ivory')
        beam(label+' Gold Edge',Vector(roof[i])+Vector((0,0,1)),Vector(roof[(i+1)%4])+Vector((0,0,1)),.35,'Gold')
    panel(label+' Clerestory',[(x-w/2,y-d/2,h),(x+w/2,y-d/2,h),
          (x+w/2,y-d/2,h+rise),(x-w/2,y-d/2,h+rise)],'Glass Dark')
    for xx in (x-w/2,x+w/2):
        mesh(label+' Side Wedge',[(xx,y-d/2,h),(xx,y+d/2,h),(xx,y-d/2,h+rise)],[(0,1,2)],'Glass')


def garden(x,y):
    box('Ivory Planter',(x,y,.9),(6,8,1.8),'Ivory')
    box('Planter Soil',(x,y,1.85),(5,7,.14),'Soil')
    cone('Evergreen Lower',(x,y,6),2.4,9,'Leaves',top=.6)
    cone('Evergreen Upper',(x,y,11),1.55,9,'Leaves')


def crystal(x,y):
    box('Crystal Pedestal',(x,y,1),(2,2,2),'Ivory')
    cone('Crystal Base',(x,y,2.5),.9,1,'Gold',top=.9,sides=6)
    cone('Crystal Lantern',(x,y,4),.7,2,'Light',top=.6,sides=6)
    cone('Crystal Tip',(x,y,5.5),.6,1,'Light',sides=6)


apron('Transport',-82,65,166,202)
apron('Royal',148,74,108,158)
hangars=[hangar('Transport Hangar',-82,-121,148,114,46),
         hangar('Royal Hangar',148,-126,90,104,32)]
for i in range(4):
    terminal_bay('Transport Hall Bay '+str(i+1),-224,112-i*44,66,44,39,11)
terminal_bay('Royal Pavilion',235,73,46,108,32,12)
star('Royal Pavilion Crest',235,127.1,26,5,True)
for x,y,w in [(-224,143,33),(235,145,30)]:
    box('Entrance Canopy',(x,y,9),(w,17,1.2),'Ivory')
    box('Canopy Gold Edge',(x,y+8.6,9),(w,.5,1),'Gold')
    for side in (-1,1):
        box('Canopy Column',(x+side*(w/2-1),y+6,4.5),(1,1,9),'Gold')
        crystal(x+side*(w/2+4),y+9)
        garden(x+side*(w/2+4),y-4)
    for i in range(3):
        box('Entrance Door',(x-6+i*6,y-8.8,4),(5,.3,7),'Glass Dark')
# Rear concourse is split to leave the arrival road unobstructed.
for x,w in [(-142,244),(144,248)]:
    terminal_bay('Arrival Concourse',x,-198,w,16,10,1)
    glass_wall('Concourse Rear',(x-w/2,-207),(x+w/2,-207),10)
box('Arrival Gateway',(0,-208,12),(39,18,2),'Ivory')
for x in (-18,18):
    box('Arrival Column',(x,-207,5.5),(2,2,11),'Ivory')
    crystal(x,-222)

# Slender control tower, clear of the central walking route and both flight lanes.
tx,ty=27,-142
box('Tower Plinth',(tx,ty,1),(30,30,2),'Ivory')
box('Tower Shaft',(tx,ty,41),(20,21,80),'Ivory')
box('Tower Vertical Glazing',(tx,ty+10.6,42),(9,.25,74),'Glass')
for xx in (-4.6,4.6):
    box('Tower Gold Mullion',(tx+xx,ty+10.9,42),(.45,.3,74),'Gold')
star('Tower Crest',tx+7,ty+10.65,59,4,True)
box('Control Room Floor',(tx,ty,81),(35,31,2),'Ivory')
for a,b in [((-16,-14),(16,-14)),((16,-14),(16,14)),((16,14),(-16,14)),((-16,14),(-16,-14))]:
    corners=[(tx+a[0]*.82,ty+a[1]*.82,82),(tx+b[0]*.82,ty+b[1]*.82,82),
             (tx+b[0],ty+b[1],94),(tx+a[0],ty+a[1],94)]
    panel('Control Room Glass',corners,'Glass')
    for i in range(5):
        t=i/4
        beam('Control Room Mullion',Vector(corners[0]).lerp(Vector(corners[1]),t),
             Vector(corners[3]).lerp(Vector(corners[2]),t),.55,'Gold')
box('Tower Canopy',(tx,ty,95),(37,34,2),'Ivory')
box('Tower Canopy Gold',(tx,ty,96.2),(37.3,34.3,.4),'Gold')
box('Equipment Deck',(tx+6,ty-2,97),(27,22,1),'Metal')
dish('Radar Large',(tx+12,ty+2,103),6.5,(.25,.85,.6))
dish('Radar Small',(tx-6,ty,102),4.2,(-.65,.5,.65))
for i,height in enumerate((17,24,19)):
    beam('Antenna Mast',(tx-6+i*5,ty-8,98),(tx-6+i*5,ty-8,98+height),.42,'Metal')
    cone('Antenna Beacon',(tx-6+i*5,ty-8,98+height),.6,1,'Light',top=.6,sides=8)
for y in (-180,-150,-105,-60,-15,30,75,120,175,208):
    for x in (-262,262):
        garden(x,y)
for y in range(-365,-225,35):
    for x in (-16,16):
        box('Arrival Lamp',(x,y,3),(1,1,6),'Metal')
        box('Lamp Head',(x,y,6),(1.8,1.8,1),'Warm Light')
for x in range(-250,251,25):
    cone('Quay Bollard',(x,216,1),.8,2,'Metal',top=.8,sides=8)
ships=[ship('Transport Display Craft',-82,51),ship('Royal Display Craft',148,68,.52,True)]
assert all(h['clear_width']>s['width']+10 and h['clear_height']>s['height']+5
           and h['clear_depth']>s['length']+5 for h,s in zip(hangars,ships))
objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
for obj in objects:
    obj['horizon_asset']=True
    obj.select_set(False)
scene=bpy.context.scene
scene['asset_identity']='sin-star-i.neris-horizon-v1'
scene['program']='Two terminals, two open hangars, two aprons, one control tower'
scene['placement']='Blender (-550,-760,0.212), yaw 180; native units/metre 10'
bpy.context.view_layer.update()

# Evaluate authored meshes and consolidate by material using the shared static writer.
sys.path.insert(0,str(ROOT.parent/'NerisTownV1/Source'))
import static_glb
groups={'Horizon':{},'Transport':{},'Royal':{}}
depsgraph=bpy.context.evaluated_depsgraph_get()
for obj in objects:
    evaluated=obj.evaluated_get(depsgraph)
    data=evaluated.to_mesh()
    data.calc_loop_triangles()
    transform=obj.matrix_world
    normals=transform.to_3x3().inverted().transposed()
    for triangle in data.loop_triangles:
        mat=data.materials[triangle.material_index]
        family=obj.get('fleet','Horizon')
        group=groups[family].setdefault(mat.name,[mat,[]])
        corners=[]
        for vi in triangle.vertices:
            p=transform @ data.vertices[vi].co-Vector(obj.get('fleet_origin',(0,0,0)))
            n=(normals @ triangle.normal).normalized()
            corners.append((tuple(p),tuple(n)))
        group[1].append(tuple(corners))
    evaluated.to_mesh_clear()
for family,materials in groups.items():
    batch=[(name,mat,triangles,None) for name,(mat,triangles) in sorted(materials.items())]
    static_glb.write(ROOT/f'Native/{family}.glb',batch)
(ROOT/'Horizon.sm3d.json').write_text('{"version":1}\n')
report={'objects':len(objects),'models':3,
        'parts':{k:len(v) for k,v in groups.items()},
        'triangles':{k:sum(len(v[1]) for v in g.values()) for k,g in groups.items()},'hangars':hangars,'ships':ships,
        'placement':{'native':[-5500,23.12,-7600],'yaw':180,'scale':10},
        'bounds_m':{'min':[-270,-390,-14],'max':[270,225,122]},
        'glass':'Opaque PBR teal panels; broad reflections, no transparency dependency.'}
(ROOT/'asset-manifest.json').write_text(json.dumps(report,indent=2)+'\n')
from fleet_animation import add as add_fleet_animation
add_fleet_animation(scene)
bpy.data.libraries.write(str(ROOT/'Source/Neris-Horizon-r003.blend'),{scene},fake_user=True)
print('HORIZON_ASSET '+json.dumps(report),flush=True)

# Real geometry preview. Presentation water is excluded from the runtime asset.
material('Preview Water',(.025,.095,.16),.35,.24)
box('Preview Water',(0,0,-.15),(2500,2400,.1),'Preview Water')
world=bpy.data.worlds.new('Horizon Day')
world.use_nodes=True
world.node_tree.nodes['Background'].inputs[0].default_value=(.48,.64,.8,1)
world.node_tree.nodes['Background'].inputs[1].default_value=.65
scene.world=world
bpy.ops.object.light_add(type='SUN',location=(-100,100,200))
bpy.context.object.rotation_euler=(math.radians(30),math.radians(-25),math.radians(-35))
bpy.context.object.data.energy=3
bpy.ops.object.camera_add(location=(430,850,435))
camera=bpy.context.object
camera.rotation_euler=(Vector((0,-20,22))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO'
camera.data.ortho_scale=820
camera.data.clip_end=10000
scene.camera=camera
scene.render.engine='CYCLES'
scene.cycles.samples=24
scene.render.resolution_x=1600
scene.render.resolution_y=1100
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.view_settings.view_transform='AgX'
scene.render.filepath=str(ROOT/'Previews/Horizon-Geometry-Hero.png')
bpy.context.view_layer.update()
# Save a separate presentation scene; water/camera never enter the native model.
bpy.data.libraries.write(str(ROOT/'Source/Neris-Horizon-Presentation-r004.blend'),{scene},fake_user=True)
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_perspective='CAMERA'
        area.spaces.active.region_3d.view_camera_zoom=0
        area.spaces.active.clip_end=10000
