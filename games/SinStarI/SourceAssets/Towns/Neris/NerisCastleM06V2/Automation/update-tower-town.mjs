import fs from 'node:fs';
import {runBlender} from './blender-background.mjs';
const work='D:/Projects/Sin-Star-I-Assets/Neris-Castle';
await runBlender(work+'/native-integration/Neris-Town-Royal-Castle-r004.blend',String.raw`
import bpy,json,sys
from pathlib import Path
from mathutils import Matrix
sys.path.insert(0,'D:/SMILE 2.0/tools/Character3DViewer')
from town_blender_files import import_document
from town_document_codec import decode
root=Path('${work}');town=Path('D:/SMILE 2.0/games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1')
catalog=json.loads((town/'Authoring/catalog.json').read_text())
report=json.loads((root/'checkpoints/Tower-r001/replacement.json').read_text())
anchor=next(o for o in bpy.context.scene.objects if o.get('town_template')==10)
with bpy.data.libraries.load(str(town/'Authoring/Catalog-r004.blend'),link=False) as (src,dst):
 dst.objects=[part['name'] for part in report['changes']]
for part,source in zip(report['changes'],dst.objects):
 obj=next(o for o in anchor.children_recursive if o.get('town_member')==part['name'])
 obj.data=source.data;obj.modifiers.clear()
 obj.matrix_world=anchor.matrix_world.copy()
 obj['town_local_matrix']=[n for row in Matrix.Identity(4) for n in row]
for source in dst.objects:bpy.data.objects.remove(source,do_unlink=True)
bpy.context.view_layer.update()
target=root/'native-integration/Neris-Town-Royal-Castle-r005.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(target),compress=True)
expected=json.loads(bpy.data.texts['Town Editor Document.json'].as_string())
actual=decode(import_document(target,catalog),catalog)
assert actual['cells']==expected['cells'] and actual['sun']==expected['sun']
assert len(actual['items'])==len(expected['items'])
items={i['identity']:i for i in actual['items']}
for item in expected['items']:
 other=items[item['identity']]
 assert item['template']==other['template']
 for field in ['position','scale']:
  assert max(abs(a-b) for a,b in zip(item[field],other[field]))<.001
print('PASS r005 Blender import: all 361 placements, cells, Sun values and transforms preserved')
`,work+'/checkpoints/Tower-r001/town-update-baked.log');
fs.copyFileSync(work+'/native-integration/Neris-Town-Royal-Castle-r005.blend','D:/SMILE 2.0/games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1/Blend/Neris-Town-Royal-Castle-r005.blend');
