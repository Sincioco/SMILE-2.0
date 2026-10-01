"""Two original Neris orbital craft: sealed cabins, compact fins and VTOL engines."""
import bpy, json, math, sys, hashlib
from pathlib import Path
from mathutils import Vector

OUT = Path(__file__).resolve().parent
sys.path.insert(0, str(OUT.parents[1] / 'NerisTownV1/Source'))
import static_glb

bpy.ops.wm.read_factory_settings(use_empty=True)
(OUT / 'Native').mkdir(exist_ok=True)
def material(name, color, metal=0.3, glow=0):
    value = bpy.data.materials.new(name)
    value.use_nodes = True
    shader = value.node_tree.nodes['Principled BSDF']
    shader.inputs['Base Color'].default_value = (*color, 1)
    shader.inputs['Metallic'].default_value = metal
    shader.inputs['Roughness'].default_value = .38
    shader.inputs['Emission Color'].default_value = (*color, 1)
    shader.inputs['Emission Strength'].default_value = glow
    return value

ivory = material('Neris Ceramic', (.74, .72, .61))
teal = material('Neris Thermal Teal', (.015, .075, .1), .65)
gold = material('Neris Gold', (.65, .43, .12), .7)
ion = material('Neris Ion', (.03, .65, .82), .2, 2)
objects = []
def finish(obj, name, mat):
    obj.name = name
    obj.data.materials.append(mat)
    obj['neris_spacecraft'] = ship
    objects.append(obj)
    return obj

def ellipsoid(name, position, scale, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, location=position)
    obj = bpy.context.object
    obj.scale = scale
    for face in obj.data.polygons:
        face.use_smooth = True
    return finish(obj, name, mat)

def prism(name, points, thickness, mat):
    size = len(points)
    vertices = points + [(x, y, z-thickness) for x,y,z in points]
    faces = [tuple(range(size)), tuple(reversed(range(size, 2*size)))]
    faces += [(i, (i+1)%size, (i+1)%size+size, i+size) for i in range(size)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    bevel = obj.modifiers.new('Rounded Hull Seams', 'BEVEL')
    bevel.width = .16
    bevel.segments = 2
    return finish(obj, name, mat)

def cylinder(name, position, radius, depth, mat, rotation=(0,0,0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=radius, depth=depth,
                                      location=position, rotation=rotation)
    return finish(bpy.context.object, name, mat)

manifest = {'revision': 1, 'front_blender': '-Y', 'units': 'metres', 'ships': []}
for ship in range(2):
    objects = []
    length = 15 if ship == 0 else 22
    half_width = 4.2 if ship == 0 else 6.1
    height = 5 if ship == 0 else 7
    dome = 3 if ship == 0 else 4
    ellipsoid('Sealed Pressure Hull', (0,0,height), (half_width,length,3 if ship==0 else 4), ivory)
    ellipsoid('Continuous Thermal Belly', (0,0,height-1.3), (half_width*.96,length*.94,2), teal)
    ellipsoid('Forward Observation Canopy', (0,-length*.48,height+dome-.8),
              (half_width*.73,length*.37,1.4), teal)
    # Short radiator fins and engine pods make a space shuttle, not an airliner.
    span = 10.5 if ship == 0 else 12.5
    for side in (-1,1):
        prism('Thermal Radiator Fin', [(side*3,-5,height),(side*span,5,height-.5),
              (side*span,length*.7,height-.5),(side*3,length*.8,height)], .7, teal)
        ellipsoid('Armored Engine Pod', (side*(span-2),length*.4,height-1), (1.8,5,1.8), ivory)
        cylinder('Rear Drive Collar', (side*(span-2),length*.4+4.7,height-1), 1.4, .7, gold, (math.pi/2,0,0))
        cylinder('Rear Ion Drive', (side*(span-2),length*.4+5.1,height-1), 1.05,.15,ion,(math.pi/2,0,0))
        for y in (-length*.35,length*.4):
            cylinder('Ventral VTOL Housing', (side*half_width*.72,y,2.3), 1.1,1.1,teal)
            cylinder('Ventral Ion Nozzle', (side*half_width*.72,y,1.7), .8,.15,ion)
            foot = Vector((side*(half_width+1),y,.6))
            attach = Vector((side*half_width*.75,y,height-1))
            strut = cylinder('Landing Strut', (foot+attach)/2, .25,(attach-foot).length,gold)
            strut.rotation_euler = (attach-foot).to_track_quat('Z','Y').to_euler()
            ellipsoid('Landing Shoe', (side*(half_width+1),y,.35), (1.1,1.5,.35),teal)
        prism('Gold Cabin Stripe', [(side*.9,-length*.78,height+dome-.25),
              (side*1.2,-length*.05,height+dome+.05),(side*1.45,-length*.05,height+dome+.05),
              (side*1.15,-length*.78,height+dome-.25)],.1,gold)
    # The same four-point heraldic motif used on Neris buildings.
    z = height+dome+.12
    prism('Neris Compass Crest', [(0,-2,z),(.55,.2,z),(2,1,z),(.55,1.8,z),
          (0,4,z),(-.55,1.8,z),(-2,1,z),(-.55,.2,z)], .15,gold)
    if ship == 1:
        ellipsoid('Aft Docking Airlock', (0,length*.72,height+2.4),(3,3,2.2),ivory)
        cylinder('Docking Seal', (0,length*.72,height+4.3),1.8,.25,gold)
    bpy.context.view_layer.update()
    deps=bpy.context.evaluated_depsgraph_get()
    groups={}
    bounds=[]
    for obj in objects:
        ev=obj.evaluated_get(deps);mesh=ev.to_mesh();mesh.calc_loop_triangles()
        normal=ev.matrix_world.to_3x3().inverted().transposed()
        bounds.extend(tuple(ev.matrix_world@v.co) for v in mesh.vertices)
        for tri in mesh.loop_triangles:
            points=[ev.matrix_world@mesh.vertices[v].co for v in tri.vertices]
            if (points[1]-points[0]).cross(points[2]-points[0]).length_squared<1e-12:
                continue
            mat=mesh.materials[tri.material_index]
            bucket=groups.setdefault(mat.name,[mat,[]])[1]
            bucket.append(tuple((tuple(p),tuple((normal@mesh.corner_normals[l].vector).normalized()))
                                for p,l in zip(points,tri.loops)))
        ev.to_mesh_clear()
    path=OUT/f'Native/Neris-Orbital-{ship}.glb'
    static_glb.write(path,[(name,mat,tris,None) for name,(mat,tris) in sorted(groups.items())])
    blob=path.read_bytes();doc=json.loads(blob[20:20+int.from_bytes(blob[12:16],'little')])
    vertices=sum(doc['accessors'][p['attributes']['POSITION']]['count'] for m in doc['meshes'] for p in m['primitives'])
    triangles=sum(doc['accessors'][p['indices']]['count']//3 for m in doc['meshes'] for p in m['primitives'])
    assert len(doc['meshes'])==4 and vertices<131072 and min(p[2] for p in bounds)>=-.001
    manifest['ships'].append(dict(file=path.name,parts=4,vertices=vertices,triangles=triangles,
        bounds=[[min(p[i] for p in bounds) for i in range(3)], [max(p[i] for p in bounds) for i in range(3)]],
        sha256=hashlib.sha256(blob).hexdigest()))
    for obj in objects:
        obj.location.x += -23 if ship==0 else 23

scene=bpy.context.scene
scene.world=bpy.data.worlds.new('Orbital Craft Studio');scene.world.color=(.18,.18,.18)
bpy.ops.object.light_add(type='AREA',location=(0,-35,65))
bpy.context.object.data.energy=90000;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=70
bpy.ops.object.camera_add(location=(65,-95,85))
camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,3))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO';camera.data.ortho_scale=100;scene.camera=camera
scene.render.engine='CYCLES';scene.cycles.samples=16
scene.render.resolution_x=1280;scene.render.resolution_y=720;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Neris-Orbital-r001.blend'),compress=True)
scene.render.filepath=str(OUT/'Preview.png');bpy.ops.render.render(write_still=True)
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('PASS NERIS ORBITAL CRAFT',json.dumps(manifest),flush=True)
