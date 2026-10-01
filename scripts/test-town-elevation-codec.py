"""Terrain codec/transfer regression. Reads native fixture output; no user map writes."""
import copy
import json
import math
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tools/Character3DViewer'))
from town_document_codec import decode, encode, unwrap, key_path, require_blender_support
from town_blender_terrain import read_prepared, mesh_data

catalog = json.loads((ROOT / 'games/SinStarI/SourceAssets/Towns/Neris/'
                     'NerisTownV1/Authoring/catalog.json').read_text())
folder = Path(sys.argv[1])
document = decode(unwrap(key_path(folder, 'Terrain.Save.A').read_bytes()), catalog)
assert document['payload'][3] == 11
assert document['heights'][2*9+2] == -12.34
assert document['flows'] == [[1, -1, 0]]
restored = decode(encode(document, catalog), catalog)
for field in ('heights', 'flows', 'curves', 'xs', 'zs', 'base_cells'):
    assert restored[field] == document[field], field
print('PASS native/Python TWN11 height, flow, curve and grid round trip')

for bad in (float('nan'), float('inf'), -10000.01):
    candidate = copy.deepcopy(document)
    candidate['heights'][0] = bad
    try:
        encode(candidate, catalog)
    except ValueError:
        pass
    else:
        raise AssertionError('Invalid elevation accepted')
for raw in (document['payload'][:-1], document['payload'][:3]+b'\x0c'+document['payload'][4:]):
    try:
        decode(raw, catalog)
    except ValueError:
        pass
    else:
        raise AssertionError('Truncated/future document accepted')
try:
    require_blender_support(document)
except ValueError as error:
    assert 'destination retained' in str(error)
else:
    raise AssertionError('Unsupported Blender data accepted')
print('PASS malformed data and unsupported Blender preflight')

patches = read_prepared(lambda suffix: unwrap(key_path(
    folder, 'Preparation.Snapshot' + suffix).read_bytes()))
vertices, faces, materials = mesh_data(patches)
assert vertices and faces and len(materials) == len(faces)
print('PASS Blender reader accepts legacy terrain in the new prepared version4')

# Current TWN9 curves and an older supported fixture stay on their legacy versions.
for relative in ('tools/Character3DViewer/Fixtures/DenseCanals.town',
                 'games/SinStarI/SourceAssets/Towns/Neris/StoryTownsV1/Towns/East Valley.town'):
    path = ROOT / relative
    legacy = decode(unwrap(path.read_bytes()), catalog)
    reopened = decode(encode(legacy, catalog), catalog)
    assert reopened['payload'][3] == legacy['payload'][3]
    assert reopened['cells'] == legacy['cells']
    assert reopened.get('curves') == legacy.get('curves')
    require_blender_support(reopened)
    print('PASS legacy TWN%d: %s' % (legacy['payload'][3], path.name))
