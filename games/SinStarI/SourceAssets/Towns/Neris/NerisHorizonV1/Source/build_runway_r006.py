"""r006: rear runway, parallel halls, matching hangar waves, open passenger entrance."""
import bpy, sys, math, json, hashlib
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT/'Revisions/r006'
OUT.mkdir(parents=True, exist_ok=True)
(OUT/'Native').mkdir(exist_ok=True)
(ROOT/'Previews/r006').mkdir(exist_ok=True)
sys.path.insert(0, str(Path(__file__).parent))
import forms as F
from forms import box, beam, panel, mesh

bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Source/Neris-Horizon-Gentle-Wave-r005.blend'))
scene = bpy.context.scene
scene.name = 'Horizon Gentle Wave r006'
for mat in bpy.data.materials:
    if mat.name.startswith('GW '): F.MATERIALS[mat.name[3:]] = mat
F.material('Runway Asphalt', (.045,.052,.059), rough=.94).name = 'GW Runway Asphalt'

def bounds(objects):
    points = [o.matrix_world@Vector(p) for o in objects for p in o.bound_box]
    return {'min':[min(p[a] for p in points) for a in range(3)],
            'max':[max(p[a] for p in points) for a in range(3)]}

def tag(objects, assembly):
    for obj in objects:
        obj['horizon_asset'] = True
        obj['assembly'] = assembly

hall_bounds = {}
for name, oldx, newx in [('Transport Glass Halls',-365,-235),('Royal Glass Hall',365,235)]:
    objects = [o for o in scene.objects if o.get('assembly') == name]
    transform = Matrix.Translation((newx,245,0)) @ Matrix.Rotation(math.pi/2,4,'Z') @ Matrix.Translation((-oldx,-280,0))
    for obj in objects: obj.matrix_world = transform @ obj.matrix_world
    bpy.context.view_layer.update()
    hall_bounds[name] = bounds(objects)
    assert hall_bounds[name]['max'][1] < 285

# Replace only the old roof assemblies; walls, door openings and floor stay intact.
hangar_bounds = {}
for name, cx, half, front, back, wall in [('Transport Hangar',-365,77,48,168,46),('Royal Hangar',365,48,53,163,32)]:
    for obj in list(scene.objects):
        if obj.get('assembly') == name and any(s in obj.name for s in ('Roof','Rafter','Ceiling Strip','Crest')):
            bpy.data.objects.remove(obj,do_unlink=True)
    before = set(scene.objects)
    def height(x):
        u = abs((x-cx)/half)
        return wall + 1.6 + 11*math.exp(-(u/.34)**2) + 4.5*math.exp(-((u-.86)/.2)**2)
    vertices, faces = [], []
    for i in range(64):
        a, b = cx-half+2*half*i/64, cx-half+2*half*(i+1)/64
        top = [(a,front,height(a)),(b,front,height(b)),(b,back,height(b)),(a,back,height(a))]
        start = len(vertices)
        vertices.extend(top+[(x,y,z-1.1) for x,y,z in top])
        faces.extend(tuple(start+j for j in face) for face in ((0,1,2,3),(7,6,5,4),(0,4,5,1),(2,6,7,3)))
        for y in (front,back):
            beam(name+' Wave Fascia',(a,y,height(a)-.35),(b,y,height(b)-.35),1.3,'Ivory',1.5)
            beam(name+' Wave Gold Edge',(a,y-.8,height(a)-.7),(b,y-.8,height(b)-.7),.15,'Gold')
        # Close the rear gable to the original wall; front remains open above the header.
        panel(name+' Rear Wave Infill',[(a,back-3,wall),(a,back-3,height(a)-1.1),(b,back-3,height(b)-1.1),(b,back-3,wall)],'Ivory')
    mesh(name+' Gentle Wave Roof',vertices,faces,'Ivory')
    for x in (cx-half+3,cx+half-3):
        box(name+' Side Roof Infill',(x,(front+back)/2,(wall+height(x)-1.1)/2),(2,back-front-6,height(x)-1.1-wall),'Ivory')
    for y in (front+10,(front+back)/2,back-10):
        for i in range(24):
            a,b=cx-half+4+(2*half-8)*i/24,cx-half+4+(2*half-8)*(i+1)/24
            beam(name+' Wave Rafter',(a,y,height(a)-1.5),(b,y,height(b)-1.5),.75,'Ivory')
        for x in (cx-half*.45,cx+half*.45):
            box(name+' Recessed Ceiling Light',(x,y,height(x)-2.2),(half*.45,.5,.15),'Warm Light')
    tag(set(scene.objects)-before,name)
    bpy.context.view_layer.update()
    hangar_bounds[name] = bounds([o for o in scene.objects if o.get('assembly')==name])

# Physically open central passage. Parked sliding leaves sit beside the opening.
for obj in list(scene.objects):
    bb = bounds([obj]) if obj.type=='MESH' else None
    if not bb: continue
    x=(bb['min'][0]+bb['max'][0])/2
    y=(bb['min'][1]+bb['max'][1])/2
    if obj.name.startswith('Interior Column') and abs(x)<.5:
        bpy.data.objects.remove(obj,do_unlink=True)
        continue
    if ((obj.name.startswith('Entrance Glass Door') and abs(x)<12) or
        (obj.name.startswith('Entrance Door Jamb') and abs(x)<10) or
        (obj.name.startswith('Door Pull') and -10<x<12)):
        bpy.data.objects.remove(obj,do_unlink=True)
    elif abs(x)<.5 and y<0 and obj.name.startswith(('Facade Ivory Column','Facade Fine Mullion')):
        # Raise the base to the canopy/header; never leave a column across the doorway.
        transform=obj.matrix_world.copy()
        obj.data.transform(transform);obj.matrix_world=Matrix.Identity(4)
        lo,hi=bb['min'][2],bb['max'][2]
        for v in obj.data.vertices:v.co.z=12.6+(v.co.z-lo)*(hi-12.6)/(hi-lo)
before=set(scene.objects)
for sign in (-1,1):
    x=sign*17.9
    panel('Open Sliding Entrance Leaf',[(x-5.7,-2.2,.8),(x+5.7,-2.2,.8),(x+5.7,-2.2,10.5),(x-5.7,-2.2,10.5)],'Glass')
    for edge in (-5.7,5.7):beam('Sliding Leaf Frame',(x+edge,-2.3,.8),(x+edge,-2.3,10.5),.2,'Gold')
box('Entrance Sliding Track',(0,-2,11),(24.5,1,.5),'Ivory')
box('Entrance Level Landing',(0,-11.5,.4),(66,23,.8),'Ivory')
tag(set(scene.objects)-before,'Gentle Wave Terminal')

# Rear runway runs east-west in the Blender asset, north-south in Neris Town.
before=set(scene.objects)
box('Runway 18 36 Asphalt',(0,355,.06),(900,50,.12),'Runway Asphalt')
box('Parallel Return Taxiway',(0,310,.055),(800,16,.11),'Runway Asphalt')
for end in (-1,1):
    # Half-circle pavement joins the runway to the return taxiway at either end.
    for i in range(40):
        a,b=math.pi*i/40,math.pi*(i+1)/40
        def point(angle,r):return (end*(400+r*math.sin(angle)),332.5+r*math.cos(angle),.115)
        panel('Taxi Turn Pavement',[point(a,14.5),point(a,30.5),point(b,30.5),point(b,14.5)],'Runway Asphalt')
        beam('Taxi Turn Centerline',point(a,22.5),point(b,22.5),.2,'Gold',.03)
beam('Taxi Centerline',(-400,310,.145),(400,310,.145),.2,'Gold',.03)
for y in (331,379):box('Runway White Edge',(0,y,.145),(896,.45,.035),'Ivory')
for x in range(-300,301,45):box('Runway Center Dash',(x,355,.155),(24,.6,.035),'Ivory')
for end in (-1,1):
    box('Runway Threshold',(end*423,355,.155),(1.8,46,.035),'Ivory')
    for y in (-19,-14,-9,-4,4,9,14,19):box('Threshold Piano Key',(end*408,355+y,.155),(23,2,.035),'Ivory')
    for y in (-14,14):box('Runway Aiming Point',(end*260,355+y,.155),(30,4,.035),'Ivory')
    data=bpy.data.curves.new('Runway Number','FONT');data.body='36' if end<0 else '18';data.size=14;data.align_x='CENTER';data.align_y='CENTER';data.extrude=.012
    obj=bpy.data.objects.new('Runway '+data.body,data);scene.collection.objects.link(obj)
    obj.location=(end*365,355,.18);obj.rotation_euler.z=-end*math.pi/2;data.materials.append(F.MATERIALS['Ivory'])
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj;bpy.ops.object.convert(target='MESH');obj.select_set(False)
for x in range(-420,421,60):
    for y in (329,381):box('Flush Runway Edge Light',(x,y,.17),(.7,.7,.08),'Warm Light')
for x in range(-330,331,30):box('Flush Runway Center Light',(x,355,.20),(.5,.5,.06),'Warm Light')
for x in (-426,426):
    for y in range(335,376,5):box('Flush Threshold Light',(x,y,.20),(.7,.7,.06),'Light')
tag(set(scene.objects)-before,'Rear Runway And Taxiway')
before=set(scene.objects)
F.cone('Antenna Beacon Housing',(0,234,285.15),.8,.3,'Metal',top=.8,sides=12)
tag(set(scene.objects)-before,'Centered Rear Tower')
bpy.context.view_layer.update()

asset=[o for o in scene.objects if o.type=='MESH' and o.get('horizon_asset')]
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
report={'revision':'r006','design':'Gentle Wave with rear runway','terminal_m':[520,150,79],
    'site_m':[960,600],'tower_m':{'height':285,'center':[0,230]},'runway_m':[900,50],
    'runway_center':[0,355],'glass_hall_bounds_m':hall_bounds,'hangar_bounds_m':hangar_bounds,
    'bounds_m':bounds(asset),'models':3,'parts':{k:len(v) for k,v in groups.items()},
    'triangles':{k:sum(len(v[1]) for v in values.values()) for k,values in groups.items()},
    'aircraft_instances':3,'runway_aircraft_scale':.55,'runway_cycle_seconds':180,
    'light_poles':0,'entrance_clear_width_m':23.4,'glass':'Single alpha-blended planes with modeled interior'}
(OUT/'asset-manifest.json').write_text(json.dumps(report,indent=2))
(OUT/'checksums.json').write_text(json.dumps({f'Native/{f}.glb':hashlib.sha256((OUT/f'Native/{f}.glb').read_bytes()).hexdigest() for f in groups},indent=2))

# Third aircraft is a linked-data Blender preview of the same shared native model.
craft=bpy.data.objects.new('Runway Aircraft - Native Shared Transport',None);scene.collection.objects.link(craft)
craft.location=(-380,355,.14);craft.rotation_euler.z=math.pi/2;craft.scale=(.55,)*3;craft['runway_aircraft_preview']=True
for original in [o for o in asset if o.get('fleet')=='Transport']:
    obj=original.copy();obj.data=original.data.copy();obj.parent=craft
    obj.data.transform(Matrix.Translation(-Vector(original['fleet_origin']))@original.matrix_world)
    obj.matrix_basis=Matrix.Identity(4);obj.name='Runway Aircraft '+original.name
    for key in list(obj.keys()):del obj[key]
    obj['runway_aircraft_preview']=True;scene.collection.objects.link(obj)
scene['revision']='r006'
rear=scene.objects['Review Rear'];rear.location=(580,1150,700)
rear.rotation_euler=(Vector((0,145,40))-rear.location).to_track_quat('-Z','Y').to_euler();rear.data.ortho_scale=1110
scene.camera=rear
scene.cycles.samples=16
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Source/Neris-Horizon-Gentle-Wave-r006.blend'),compress=True)
print('PASS R006 GEOMETRY '+json.dumps(report),flush=True)
for name in ('Rear','Top','Hero','Front'):
    scene.camera=scene.objects['Review '+name];scene.render.filepath=str(ROOT/f'Previews/r006/{name}.png')
    bpy.ops.render.render(write_still=True)
