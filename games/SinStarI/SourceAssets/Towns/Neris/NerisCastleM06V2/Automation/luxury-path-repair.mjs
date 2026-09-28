import fs from 'node:fs';
import { primitives } from './model-primitives.mjs';
import { helpers } from './m03-facade.mjs';
import { runBlender } from './blender-background.mjs';
const root='D:/Projects/Sin-Star-I-Assets/Neris-Castle/';
fs.mkdirSync(root+'checkpoints/M06-r005',{recursive:true});
await runBlender(root+'source/neris-castle-M06-r004.blend',primitives+helpers+String.raw`
from pathlib import Path
target=Path('D:/Projects/Sin-Star-I-Assets/Neris-Castle/source/neris-castle-M06-r005.blend')
assert not target.exists()
path=bpy.data.objects['NC.Courtyard.ProcessionalPath']
connector=bpy.data.objects['NC.Courtyard.GateConnector']
cutter=path.copy();cutter.data=path.data.copy()
bpy.data.collections['NC.Courtyard'].objects.link(cutter);cutter.name='NC.Courtyard.ConnectorPathCut'
for v in cutter.data.vertices:v.co.z+=1 if v.co.z>0 else -1
cut(connector,cutter)
bpy.context.view_layer.update()
# Verify the reported patch is no longer covered by both paving owners.
origin=connector.matrix_world.inverted()@Vector((0,-48,1))
assert not connector.ray_cast(origin,Vector((0,0,-1)))[0]
s['path_overlap_repaired']='GateConnector cut around flush ProcessionalPath; both surfaces retain original height.'
bpy.ops.wm.save_as_mainfile(filepath=str(target),compress=True)
print('NC_RESULT path remains flush at 0.02; gate connector no longer overlaps it')
`,root+'checkpoints/M06-r005/path-repair.log');
