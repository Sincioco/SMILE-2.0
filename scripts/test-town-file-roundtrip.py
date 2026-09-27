"""Actual Blender round trip in an isolated folder; never writes the working town."""
import json
import sys
from pathlib import Path
import tempfile

import bpy

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tools/Character3DViewer'))
from town_blender_files import export_document, import_document, TOWN
from town_document_codec import decode, encode, unwrap

catalog = json.loads((TOWN / 'Authoring/catalog.json').read_text(encoding='utf-8'))
source = Path(sys.argv[sys.argv.index('--') + 1])
document = decode(unwrap(source.read_bytes()), catalog)
folder = Path(tempfile.mkdtemp(prefix='town-files-', dir=ROOT / 'artifacts/tests'))
target = folder / 'Round Trip Town.blend'
export_document(document, catalog, target, 1, lambda *args: None)
opened = decode(import_document(target, catalog), catalog)
assert opened['name'] == 'Round Trip Town'
assert opened['cells'] == document['cells']
assert opened['sun'] == document['sun']
expected = {item['identity']: item for item in document['items']}
for item in opened['items']:
    original = expected[item['identity']]
    assert item['template'] == original['template']
    for key in ('position', 'scale'):
        assert max(abs(a-b) for a,b in zip(item[key], original[key])) < .01, (item['identity'], key)
    assert abs((item['yaw'] - original['yaw'] + 180) % 360 - 180) < .01
assert len(opened['items']) == len(document['items'])

anchors = [o for o in bpy.context.scene.objects if 'town_assembly' in o]
moved = anchors[0]
identity = moved['town_assembly']
moved.location.x += 3
removed = anchors[1]
removed_id = removed['town_assembly']
bpy.data.batch_remove(ids=list(removed.children_recursive) + [removed])
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(target), compress=True)
changed = decode(import_document(target, catalog), catalog)
items = {item['identity']: item for item in changed['items']}
assert removed_id not in items
assert abs(items[identity]['position'][0] - expected[identity]['position'][0] - 30) < .01
assert encode(changed, catalog) == changed['payload']
print('PASS Blender file round trip, full hierarchy, moved/deleted assemblies, surfaces and lighting')
print(target)
