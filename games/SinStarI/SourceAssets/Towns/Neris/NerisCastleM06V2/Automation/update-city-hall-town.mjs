import fs from 'node:fs';
import {runBlender} from './blender-background.mjs';
const root='D:/Projects/Sin-Star-I-Assets/Neris-Castle';
await runBlender(root+'/native-integration/Neris-Town-Royal-Castle-r003.blend',String.raw`
import bpy,json,sys,hashlib
from pathlib import Path
from mathutils import Matrix
sys.path.insert(0,'D:/SMILE 2.0/tools/Character3DViewer')
from town_blender_files import import_document
from town_document_codec import decode
root=Path('${root}');town=Path('D:/SMILE 2.0/games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1')
catalog=json.loads((town/'Authoring/catalog.json').read_text())
# Only replace City Hall's twenty foliage/soil meshes in the assembled r003.
anchors=[o for o in bpy.context.scene.objects if o.get('town_template')==9]
assert len(anchors)==1
anchor=anchors[0]
names=['Clipped Cypress.%03d'%i for first in [40,45,60,65] for i in range(first,first+5)]
with bpy.data.libraries.load(str(town/'Authoring/Catalog-r003.blend'),link=False) as (src,dst):dst.objects=list(names)
# Appended objects retain original source transforms; source anchor is 2x at (0,-75,.16).
source=next(i for i in catalog['instances'] if i['template']==9)
from town_blender_save import source_matrix
inverse=source_matrix(source).inverted()
for obj in dst.objects:
 name=next(n for n in names if obj.name==n or obj.name.startswith(n+'.'))
 matches=[o for o in anchor.children_recursive if o.get('town_member')==name];assert len(matches)==1
 clone=matches[0];clone.data=obj.data
 # Replacement vertices are in City Hall local coordinates.
 clone.matrix_world=anchor.matrix_world.copy();clone.modifiers.clear()
 clone['town_local_matrix']=[n for row in Matrix.Identity(4) for n in row]
 clone['royal_court_shrub_source']=obj.get('royal_court_shrub_source','')
for obj in dst.objects:bpy.data.objects.remove(obj,do_unlink=True)
bpy.context.view_layer.update()
target=root/'native-integration/Neris-Town-Royal-Castle-r004.blend';assert not target.exists()
bpy.ops.wm.save_as_mainfile(filepath=str(target),compress=True)
expected=json.loads(bpy.data.texts['Town Editor Document.json'].as_string())
for rev in ['r002','r003','r004']:
 actual=decode(import_document(root/('native-integration/Neris-Town-Royal-Castle-'+rev+'.blend'),catalog),catalog)
 assert actual['cells']==expected['cells'] or rev=='r002'
 assert len(actual['items'])==len(expected['items'])
 assert actual['sun']==expected['sun'] or rev=='r002'
 print('PASS catalog compatibility and town import',rev,len(actual['items']),flush=True)
print('PASS four City Hall planters updated; original saved layout and lighting retained')
`,root+'/checkpoints/City-Hall-r001/town-update-fixed.log');
fs.copyFileSync(root+'/native-integration/Neris-Town-Royal-Castle-r004.blend','games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1/Blend/Neris-Town-Royal-Castle-r004.blend');
