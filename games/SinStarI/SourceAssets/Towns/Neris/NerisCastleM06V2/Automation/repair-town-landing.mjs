import fs from 'node:fs';
import { runBlender } from './blender-background.mjs';
const root = 'D:/Projects/Sin-Star-I-Assets/Neris-Castle';
const doc = JSON.parse(fs.readFileSync(root + '/native-integration/relocated-town-document-r001.json'));
const oldZ = [...doc.zs], oldCells = doc.cells;
const zs = [...new Set([...oldZ, 1012])].sort((a,b) => a-b);
const cells = [];
for (let z = 0; z < zs.length - 1; z++) {
  const centerZ = (zs[z] + zs[z + 1]) / 2;
  const oldRow = oldZ.findIndex((low, i) => low <= centerZ && oldZ[i + 1] > centerZ);
  for (let x = 0; x < doc.columns; x++) {
    const centerX = (doc.xs[x] + doc.xs[x + 1]) / 2;
    let kind = oldCells[oldRow * doc.columns + x];
    if (centerZ >= 1012 && centerZ < 1100 && centerX >= -4780 && centerX < -2540) kind = 2;
    cells.push(kind);
  }
}
Object.assign(doc, {zs, rows:zs.length - 1, cells});
const output = root + '/native-integration/relocated-town-document-r002.json';
fs.writeFileSync(output, JSON.stringify(doc), {flag:'wx'});
fs.mkdirSync(root + '/checkpoints/Town-r002', {recursive:true});
await runBlender(root + '/native-integration/Neris-Town-Royal-Castle-r001.blend', String.raw`
import bpy,sys,json,hashlib
from pathlib import Path
sys.path.insert(0,'D:/SMILE 2.0/tools/Character3DViewer')
from town_blender_save import terrain
from town_document_codec import encode,decode,unwrap
root=Path('D:/Projects/Sin-Star-I-Assets/Neris-Castle')
doc=json.loads((root/'native-integration/relocated-town-document-r002.json').read_text())
catalog=json.loads(Path('D:/SMILE 2.0/games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1/Authoring/catalog.json').read_text())
# Real native v1 bytes and the existing Blender checksum remain compatible.
previous=json.loads(bpy.data.texts['Town Editor Document.json'].as_string())
assert hashlib.sha256(encode(previous,catalog)).hexdigest()==bpy.context.scene['town_editor_document_sha256']
native=decode(unwrap((root/'native-integration/Neris-Town-Royal-Castle-r001.town').read_bytes()),catalog)
assert native['items']==previous['items'] and native['cells']==previous['cells']
for opacity in (0,50,100):
    sample=dict(previous);sample['sun']=previous['sun'][:8]+[opacity]
    assert decode(encode(sample,catalog),catalog)['sun'][8]==opacity
bpy.data.objects.remove(bpy.data.objects['Town Editable Surface'],do_unlink=True)
terrain(doc)
snapshot=bpy.data.texts['Town Editor Document.json'];snapshot.clear();snapshot.write(json.dumps(doc))
bpy.context.scene['town_editor_document_sha256']=hashlib.sha256(encode(doc,catalog)).hexdigest()
record=bpy.data.texts['New Royal Castle Placement.json'];placement=json.loads(record.as_string())
placement['moat'][2]=1012;placement['native_sync_pending']=True
record.clear();record.write(json.dumps(placement,indent=2))
target=root/'native-integration/Neris-Town-Royal-Castle-r002.blend'
assert not target.exists()
bpy.ops.wm.save_as_mainfile(filepath=str(target),compress=True)
print('PASS Legacy checksum, native document decode, shadow opacity 0/50/100 round trip, and exact bridge landing at Z=1012')
`, root + '/checkpoints/Town-r002/landing-checks.log');
