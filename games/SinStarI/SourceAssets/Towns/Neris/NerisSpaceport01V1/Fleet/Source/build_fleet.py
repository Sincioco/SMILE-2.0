"""Four original, bounded alien visitors for the four elevated Spaceport 01 pads."""
import bpy,sys,math,json,hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT.parent/'NerisTownV1/Source'))
import static_glb
bpy.ops.wm.read_factory_settings(use_empty=True)
OUT=ROOT/'Fleet';(OUT/'Native').mkdir(parents=True,exist_ok=True);(OUT/'Previews').mkdir(exist_ok=True)
def mat(name,color,metal=.5,rough=.3,glow=0):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough;p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=glow;return m
silver=mat('Visitor Silver',(.40,.48,.55));dark=mat('Obsidian Violet',(.075,.035,.12));copper=mat('Organic Copper',(.35,.105,.035));red=mat('Crimson Ceramic',(.32,.025,.055));black=mat('Window Graphite',(.008,.017,.032),.75,.2)
cyan=mat('Ion Cyan',(.015,.75,1),.1,.3,3);amber=mat('Plasma Amber',(1,.29,.025),.1,.3,3);green=mat('Reactor Lime',(.12,1,.2),.1,.3,3);violet=mat('Prism Violet',(.52,.08,1),.1,.3,3)
objects=[]
def finish(o,name,m):
 o.name=name;o.data.materials.append(m);o['alien_ship']=ship
 for p in o.data.polygons:p.use_smooth=True
 objects.append(o);return o
def ell(name,pos,scale,m):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=16,location=pos);o=bpy.context.object;o.scale=scale;return finish(o,name,m)
def ring(name,pos,r,t,m,rotation=(0,0,0)):
 bpy.ops.mesh.primitive_torus_add(major_segments=48,minor_segments=8,location=pos,major_radius=r,minor_radius=t,rotation=rotation);return finish(bpy.context.object,name,m)
def strut(name,a,b,r,m):
 d=Vector(b)-Vector(a);bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=r,depth=d.length,location=(Vector(a)+Vector(b))/2);o=bpy.context.object;o.rotation_euler=d.to_track_quat('Z','Y').to_euler();return finish(o,name,m)
def wing(name,pts,m):
 # Thick sculpted wing, with a bevel to keep grazing highlights broad.
 n=len(pts);vs=[(x,y,z) for x,y,z in pts]+[(x,y,z-2.2) for x,y,z in pts]
 faces=[tuple(range(n)),tuple(reversed(range(n,n*2)))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],faces);o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);be=o.modifiers.new('Soft Armor Edges','BEVEL');be.width=.7;be.segments=3;return finish(o,name,m)
def feet(radius,m):
 for a in (30,150,270):
  x,y=radius*math.cos(math.radians(a)),radius*math.sin(math.radians(a));strut('Landing Leg',(x*.7,y*.7,10),(x,y,1.4),.65,m);ell('Landing Foot',(x,y,1),(2.7,1.7,1),m)
names=['Aster Saucer','Vesper Manta','Khepri Tri-Pod','Vanta Prism']
for ship in range(4):
 objects=[]
 if ship==0:
  ell('Lenticular Hull',(0,0,12),(29,29,7),silver);ell('Observation Dome',(0,-2,18),(12,12,5.5),black);ring('Ion Belt',(0,0,12),28.6,.65,cyan);ring('Dome Collar',(0,-2,18),12,.7,silver);ell('Ventral Reactor',(0,0,6),(12,12,2),cyan)
  for a in range(0,360,45):
   x,y=22*math.cos(math.radians(a)),22*math.sin(math.radians(a));ell('Rim Port',(x,y,16),(2.5,2.5,.7),cyan)
  feet(18,silver)
 elif ship==1:
  ell('Manta Fuselage',(0,0,12),(9,27,8),dark);ell('Amber Cockpit',(0,-13,18),(5,11,3),black)
  for side in (-1,1):
   wing('Swept Manta Wing',[(side*5,-19,14),(side*36,9,11),(side*25,26,13),(side*9,12,18)],dark);ell('Wing Engine',(side*24,12,12),(4,12,4),black);ell('Amber Drive',(side*24,23,12),(3.4,1,3.4),amber);strut('Amber Wing Edge',(side*9,-12,15),(side*32,10,12),.45,amber)
  feet(12,dark)
 elif ship==2:
  ell('Seed Core',(0,0,14),(13,13,11),copper);ell('Green Eye',(0,-11,17),(6,2.5,5),green);ring('Core Collar',(0,0,14),13,.65,black)
  for a in (30,150,270):
   x,y=22*math.cos(math.radians(a)),22*math.sin(math.radians(a));strut('Organic Arm',(x*.25,y*.25,15),(x,y,10),2,copper);ell('Engine Pod',(x,y,10),(7,7,8),copper);ring('Green Pod Ring',(x,y,8),6.5,.7,green);ell('Pod Foot',(x,y,1.5),(5,5,1.5),black)
 else:
  wing('Prismatic Hull',[(0,-34,12),(19,8,19),(12,28,12),(-12,28,12),(-19,8,19)],red);wing('Dorsal Crystal',[(0,-19,16),(0,15,32),(8,14,18),(-8,14,18)],black)
  for side in (-1,1):
   wing('Outrigger Blade',[(side*13,-2,15),(side*32,20,8),(side*26,31,10),(side*12,19,17)],red);strut('Violet Spine',(side*2,-25,13),(side*16,8,20),.65,violet);ell('Violet Drive',(side*13,24,12),(5,1.5,4),violet)
  feet(15,black)
 bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get();groups={}
 for o in objects:
  ev=o.evaluated_get(deps);me=ev.to_mesh();me.calc_loop_triangles();normal=o.matrix_world.to_3x3().inverted().transposed()
  for tri in me.loop_triangles:
   a,b,c=[o.matrix_world@me.vertices[v].co for v in tri.vertices]
   if (b-a).cross(c-a).length_squared < 1e-12:continue
   m=me.materials[tri.material_index];bucket=groups.setdefault(m.name,[m,[]])[1]
   bucket.append(tuple((tuple(o.matrix_world@me.vertices[v].co),tuple((normal@me.corner_normals[l].vector).normalized())) for v,l in zip(tri.vertices,tri.loops)))
  ev.to_mesh_clear()
 static_glb.write(OUT/f'Native/Alien-{ship}.glb',[(n,m,t,None) for n,(m,t) in sorted(groups.items())])
 for o in objects:o.location.x+=(ship%2)*95-47.5;o.location.y+=(ship//2)*100-50
scene=bpy.context.scene;scene.world=bpy.data.worlds.new('Visitor Studio');scene.world.color=(.16,.16,.16)
bpy.ops.object.light_add(type='AREA',location=(0,-50,140));bpy.context.object.data.energy=220000;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=150
bpy.ops.object.camera_add(location=(150,-220,210));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,10))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=230;scene.camera=cam
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.resolution_x=1280;scene.render.resolution_y=960;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX';bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Alien-Visitors-r001.blend'),compress=True);scene.render.filepath=str(OUT/'Previews/Fleet.png');bpy.ops.render.render(write_still=True)
report={'ships':[],'pads_blender_m':[[-306,44,64],[-306,156,104],[306,44,64],[306,156,104]],'traffic':{'independent_pads':True,'parked_seconds':[18,28],'takeoff_seconds':[5,7],'clear_before_approach_seconds':[10,15],'landing_seconds':[5,7]}}
for i,name in enumerate(names):
 p=OUT/f'Native/Alien-{i}.glb';data=p.read_bytes();doc=json.loads(data[20:20+int.from_bytes(data[12:16],'little')]);v=sum(doc['accessors'][q['attributes']['POSITION']]['count'] for me in doc['meshes'] for q in me['primitives']);t=sum(doc['accessors'][q['indices']]['count']//3 for me in doc['meshes'] for q in me['primitives']);assert v<131072 and len(doc['meshes'])<=4
 report['ships'].append({'name':name,'file':p.name,'parts':len(doc['meshes']),'vertices':v,'triangles':t,'sha256':hashlib.sha256(data).hexdigest()})
(OUT/'manifest.json').write_text(json.dumps(report,indent=2));print('PASS FLEET',json.dumps(report),flush=True)
