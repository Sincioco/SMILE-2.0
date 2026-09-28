import fs from 'node:fs';
import { runBlender } from './blender-background.mjs';
const root = 'D:/Projects/Sin-Star-I-Assets/Neris-Castle';
const directory = root + '/checkpoints/M06-r005';
if (fs.existsSync(directory + '/hero-front.png')) throw Error('Preserve existing review renders.');
await runBlender(root + '/source/neris-castle-M06-r005.blend', String.raw`
import bpy,json,math
from pathlib import Path
from mathutils import Vector
root=Path('D:/Projects/Sin-Star-I-Assets/Neris-Castle')
out=root/'checkpoints/M06-r005'
s=bpy.context.scene
baseline=json.loads((root/'checkpoints/M01-r002/scene-measurements.json').read_text())
checks=[]
for name in ['Front','Back','Left','Right','Top','HeroFront','HeroThreeQuarter']:
    camera=bpy.data.objects['NC.CAM.'+name]
    previous=next(c for c in baseline['objects'] if c['name']==camera.name)
    assert max(abs(camera.location[i]-previous['location'][i]) for i in range(3))<.0001
    checks.append({'camera':camera.name,'fixed_location':list(camera.location),'rotation':list(camera.rotation_euler)})
    s.camera=camera
    slug={'HeroFront':'hero-front','HeroThreeQuarter':'hero-three-quarter'}.get(name,name.lower())
    s.render.resolution_x=2560 if name.startswith('Hero') else 2048
    s.render.resolution_y=2048 if name=='Top' else (1440 if name.startswith('Hero') else 1152)
    s.render.resolution_percentage=100
    s.render.filepath=str(out/(slug+'.png'))
    bpy.ops.render.render(write_still=True)
objects=[]
for o in s.objects:
    if o.type!='MESH' or not o.get('owner'):continue
    corners=[o.matrix_world@Vector(v) for v in o.bound_box]
    objects.append({'name':o.name,'owner':o.get('owner'),'bounds':[[min(v[a] for v in corners),max(v[a] for v in corners)] for a in range(3)]})
(out/'scene-measurements.json').write_text(json.dumps({'source':bpy.data.filepath,'fixed_cameras':checks,'objects':objects,'dimensions_status':'proposed; no dimensions changed by r004/r005'},indent=2))
night=next(scene for scene in bpy.data.scenes if scene.name.startswith('NC.Review.Night.'))
night.render.filepath=str(out/'courtyard-night.png')
bpy.ops.render.render(write_still=True,scene=night.name)
print('PASS Seven unchanged fixed cameras and separate night review rendered from r005; source file untouched')
`, directory + '/review-render.log');
