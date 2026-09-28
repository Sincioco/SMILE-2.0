import {runBlender} from './blender-background.mjs';
await runBlender('D:/SMILE 2.0/games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1/Authoring/Catalog.blend',String.raw`
import bpy,json
from mathutils import Vector
root=bpy.data.objects['Royal Castle of Neris']
inverse=root.matrix_world.inverted()
rows=[]
for o in root.children_recursive:
    if o.name.startswith(('Sweeping Royal Garden Stair','Royal Entrance Stair','Carved Ivory Baluster','Terrace Handrail')):
        p=[inverse @ o.matrix_world @ Vector(v) for v in o.bound_box]
        rows.append({'name':o.name,'type':o.type,'low':[min(v[i] for v in p) for i in range(3)],'high':[max(v[i] for v in p) for i in range(3)]})
print('ROYAL_INSPECTION '+json.dumps({'root':root.name,'matrix':[list(v) for v in root.matrix_world],'rows':rows}))
`, 'D:/Projects/Sin-Star-I-Assets/Neris-Castle/native-integration/royal-stairs-inspection.log');
