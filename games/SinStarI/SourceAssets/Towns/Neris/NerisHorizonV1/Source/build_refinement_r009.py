"""Window banners, seam-free apron and operable terminal leaves; preserves r008."""
import bpy, sys, json, math, hashlib, shutil
from pathlib import Path
from mathutils import Vector, Matrix
ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'Source'))
sys.path.insert(0, str(ROOT.parent / 'NerisTownV1/Source'))
import forms as F
import static_glb
bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'Source/Neris-Horizon-Gentle-Wave-r008.blend'))
scene = bpy.context.scene
scene.name = 'Horizon Gentle Wave r009'
for mat in bpy.data.materials:
    if mat.name.startswith('GW '): F.MATERIALS[mat.name[3:]] = mat
removed = 0
for obj in list(scene.objects):
    if obj.name.startswith('Paving Tile Joint'):
        bpy.data.objects.remove(obj, do_unlink=True); removed += 1
    elif obj.name.startswith(('Roof Sign Neris Banner','Roof Sign Banner','Roof Sign Mount','Roof Sign Neris Compass')):
        points=[obj.matrix_world @ Vector(p) for p in obj.bound_box]
        cx=sum(p.x for p in points)/8
        side=1 if cx>0 else -1
        # Whole banner assembly moves from the eave to the clear bay centered at +/-91 m.
        oldtop=53+24*math.exp(-(109/90)**2)+10*math.exp(-((109-228)/53)**2)+22
        obj.location += Vector((-18*side,-1,44-oldtop))
        obj['assembly']='Window Hanging Neris Banners'
    elif obj.name.startswith(('Entrance Glass Door','Door Pull')):
        bpy.data.objects.remove(obj, do_unlink=True)
# Gold-framed rectangular glass leaves exactly fill the existing clear opening.
for side in (-1,1):
    before=set(scene.objects)
    label='Left' if side<0 else 'Right'
    for suffix,original in [('Glass','Terminal Tint'),('Gold','Gold')]:
        mat=F.MATERIALS[original].copy();mat.name='GW Door '+label+' '+suffix
        F.MATERIALS['Door '+label+' '+suffix]=mat
    cx=side*5.85
    F.box('Terminal Door '+label+' Glass',(cx,-1.5,5.65),(11.3,.22,9.4),'Door '+label+' Glass')
    for x in (cx-5.7,cx+5.7):
        F.box('Terminal Door '+label+' Stile',(x,-1.52,5.65),(.30,.42,9.7),'Door '+label+' Gold')
    for z in (.95,10.35):
        F.box('Terminal Door '+label+' Rail',(cx,-1.52,z),(11.7,.42,.30),'Door '+label+' Gold')
    F.box('Terminal Door '+label+' Pull',(side*.8,-1.85,5.0),(.22,.30,1.8),'Door '+label+' Gold')
    for obj in set(scene.objects)-before:
        obj['horizon_asset']=True;obj['horizon_family']='Door';obj['assembly']='Operable Terminal Doors'
        obj['door_side']=side;obj['door_origin']=(0,-1.5,.8)
bpy.context.view_layer.update()
out=ROOT/'Revisions/r009';(out/'Native').mkdir(parents=True,exist_ok=True)
preview=ROOT/'Previews/r009';preview.mkdir(parents=True,exist_ok=True)
groups={k:{} for k in ('Horizon','Transport','Royal','Cargo','Door')}
deps=bpy.context.evaluated_depsgraph_get()
for obj in scene.objects:
    if obj.get('fleet'): continue
    if obj.type!='MESH' or not obj.get('horizon_asset'): continue
    ev=obj.evaluated_get(deps);data=ev.to_mesh();data.calc_loop_triangles()
    transform=obj.matrix_world;normal=transform.to_3x3().inverted().transposed()
    family=obj.get('horizon_family',obj.get('fleet','Horizon'))
    origin=Vector(obj.get('door_origin',obj.get('fleet_origin',(0,0,0))))
    for tri in data.loop_triangles:
        mat=data.materials[tri.material_index];bucket=groups[family].setdefault(mat.name,[mat,[]])
        bucket[1].append(tuple((tuple(transform@data.vertices[v].co-origin),tuple((normal@(data.corner_normals[l].vector if obj.get('fleet') else tri.normal)).normalized())) for v,l in zip(tri.vertices,tri.loops)))
    ev.to_mesh_clear()
for family,mats in groups.items():
    if family in ('Transport','Royal','Cargo'):
        shutil.copyfile(ROOT/f'Revisions/r008/Native/{family}.glb',out/f'Native/{family}.glb')
        shutil.copyfile(out/f'Native/{family}.glb',ROOT/f'Native/{family}.glb')
        continue
    static_glb.write(out/f'Native/{family}.glb',[(n,m,t,None) for n,(m,t) in sorted(mats.items())])
    shutil.copyfile(out/f'Native/{family}.glb',ROOT/f'Native/{family}.glb')
report=json.loads((ROOT/'asset-manifest.json').read_text())
report.update(revision='r009',parts={k:len(v) for k,v in groups.items()},triangles={k:sum(len(t) for m,t in v.values()) for k,v in groups.items()},tile_spacing_m=None,paving_grid=False,gold_inlays_preserved=True,banner_bay_centers=[-91,91],banner_top_m=44,operable_door_width_m=23.4,operable_door_height_m=9.7,models=5)
for f in ('Transport','Royal','Cargo'):
    previous=json.loads((ROOT/'Revisions/r008/asset-manifest.json').read_text())
    report['parts'][f]=previous['parts'][f];report['triangles'][f]=previous['triangles'][f]
checks={f'Native/{f}.glb':hashlib.sha256((out/f'Native/{f}.glb').read_bytes()).hexdigest() for f in groups}
for base in (out,ROOT):
    (base/'asset-manifest.json').write_text(json.dumps(report,indent=2));(base/'checksums.json').write_text(json.dumps(checks,indent=2))
scene['revision']='r009';scene['removed_paving_seams']=removed
bpy.context.preferences.filepaths.save_version=0
scene.camera=scene.objects['Review Hero']
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Source/Neris-Horizon-Gentle-Wave-r009.blend'),compress=True)
assert removed>100
assert not any(o.name.startswith('Paving Tile Joint') for o in scene.objects)
assert sum(o.name.startswith('Arrival Gold Inlay') for o in scene.objects)==4
print('PASS R009 GEOMETRY',report['parts'], 'removed seams',removed,flush=True)
scene.cycles.samples=12
for name in ('Front','Hero','Top'):
    scene.camera=scene.objects['Review '+name];scene.render.filepath=str(preview/(name+'.png'));bpy.ops.render.render(write_still=True)
