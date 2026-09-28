import fs from 'node:fs';
import {runBlender} from './blender-background.mjs';
const root='D:/Projects/Sin-Star-I-Assets/Neris-Castle';
if(fs.existsSync(root+'/source/neris-castle-M06-r006.blend'))throw Error('Preserve the existing revision.');
fs.mkdirSync(root+'/checkpoints/M06-r006',{recursive:true});
await runBlender(root+'/source/neris-castle-M06-r005.blend',String.raw`
import bpy,json,hashlib
from pathlib import Path
from mathutils import Vector
s=bpy.context.scene
out=Path('${root}/checkpoints/M06-r006')
def fingerprint(owner):
    h=hashlib.sha256()
    for o in sorted(bpy.data.collections['NC.'+owner].objects,key=lambda o:o.name):
        h.update((o.name+str(tuple(tuple(r) for r in o.matrix_world))).encode())
        if o.type=='MESH':
            for v in o.data.vertices:h.update(str(tuple(v.co)).encode())
    return h.hexdigest()
unchanged={owner:fingerprint(owner) for owner in ['Site','Fortifications','Bridge','Palace','Courtyard']}
vault=bpy.data.objects['NC.Gatehouse.Arch.Vault']
front=min((vault.matrix_world@v.co).y for v in vault.data.vertices)
assert abs(front+63.6)<.001
for v in vault.data.vertices:
    world=vault.matrix_world@v.co
    if abs(world.y-front)<.001:
        world.y=-64.0;v.co=vault.matrix_world.inverted()@world
vault.data.update()
# Place the mouldings wholly in front of the now continuous wall face.
offsets={'NC.Gatehouse.Arch.Front.Stone':-.55,'NC.Gatehouse.Arch.Front.Gold':-.60,
    'NC.Gatehouse.Arch.Front.OuterReveal':-.61,
    'NC.Gatehouse.Luxury.Arch.0':-.48,'NC.Gatehouse.Luxury.ArchGold.0':-.48,
    'NC.Gatehouse.Luxury.Arch.1':-.50,'NC.Gatehouse.Luxury.ArchGold.1':-.50}
for name,delta in offsets.items():bpy.data.objects[name].location.y+=delta
for o in bpy.data.collections['NC.Gatehouse'].objects:
    if o.name.startswith('NC.Gatehouse.Arch.Insignia.'):o.location.y-=.60
for name,new_front in [('NC.Gatehouse.Cornice.Stone',-64.12),('NC.Gatehouse.Cornice.Gold',-64.17)]:
    o=bpy.data.objects[name];lo=min((o.matrix_world@v.co).y for v in o.data.vertices)
    for v in o.data.vertices:
        p=o.matrix_world@v.co
        if abs(p.y-lo)<.001:p.y=new_front;v.co=o.matrix_world.inverted()@p
    o.data.update()
bpy.context.view_layer.update()
assert all(fingerprint(owner)==value for owner,value in unchanged.items())
planes={name:min((bpy.data.objects[name].matrix_world@v.co).y for v in bpy.data.objects[name].data.vertices)
    for name in ['NC.Gatehouse.Jamb.West','NC.Gatehouse.Jamb.East','NC.Gatehouse.Arch.Vault']}
assert max(planes.values())-min(planes.values())<.00001
checks={'revision':'M06-r006','wall_front_planes':planes,'previous_vault_recess_metres':.4,
    'unchanged_owners':unchanged,'clear_opening_width':12,'clear_opening_peak':15,'trim_offsets':offsets}
(out/'entrance-checks.json').write_text(json.dumps(checks,indent=2))
s['entrance_flush_revision']='M06-r006: arch spandrel aligned to jambs at Y=-64; no recessed center strip'
d=bpy.data.cameras.new('NC.CAM.EntranceFlush');camera=bpy.data.objects.new(d.name,d)
bpy.data.collections['NC.Review'].objects.link(camera)
camera.location=(31,-115,26);target=Vector((0,-59,11))
camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();d.lens=48;d.clip_end=1000
night=next(sc for sc in bpy.data.scenes if sc.name.startswith('NC.Review.Night.'))
# Night review reuses the same authored geometry, with its own environment.
for scene in [s,night]:
    scene.camera=camera;scene.render.resolution_x=1280;scene.render.resolution_y=900;scene.render.resolution_percentage=100
s.camera=bpy.data.objects['NC.CAM.HeroThreeQuarter']
bpy.ops.wm.save_as_mainfile(filepath='${root}/source/neris-castle-M06-r006.blend',compress=True)
for scene,label in [(s,'entrance-day'),(night,'entrance-night')]:
    scene.camera=camera;scene.render.filepath=str(out/(label+'.png'))
    bpy.ops.render.render(write_still=True,scene=scene.name)
print('PASS flush entrance planes, unchanged owner fingerprints and two real review renders')
`,root+'/checkpoints/M06-r006/entrance-repair.log');
