"""Actual Blender export regression for the grid-staircase bug and renamed airports.

Pass prepared .town files after --. Writes only a unique artifacts/tests folder.
"""
import json
import sys
import tempfile
from pathlib import Path

import bpy

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tools/Character3DViewer'))
from town_document_codec import decode, encode, unwrap, prepared_records, landmark
from town_blender_files import export_document, import_document, TOWN
from town_blender_terrain import read_prepared, mesh_data

catalog = json.loads((TOWN / 'Authoring/catalog.json').read_text())
folder = Path(tempfile.mkdtemp(prefix='curved-blender-', dir=ROOT / 'artifacts/tests'))
for index, name in enumerate(sys.argv[sys.argv.index('--') + 1:]):
    source = Path(name)
    raw = source.read_bytes()
    document = decode(unwrap(raw), catalog)
    records = prepared_records(raw)
    patches = read_prepared(records.__getitem__)
    assert any(p[-1] == 3 for p in patches), 'Fixture must have native curve triangles'
    # Exercise an arbitrary name, not only the automatic numeric copy suffix.
    identity = landmark(document)
    document['landmark'] = identity
    document['name'] += ' Export Copy'
    # Normalize dirty/version state just as the exporter does.
    document = decode(encode(document, catalog), catalog)
    target = folder / (document['name'] + '.blend')
    export_document(document, catalog, target, index + 1,
                    lambda progress, message: print(progress, message, flush=True), patches)
    mesh = bpy.data.objects['Town Editable Surface'].data
    vertices, faces, materials = mesh_data(patches)
    assert len(mesh.vertices) == len(vertices) and len(mesh.polygons) == len(faces)
    for actual, expected in zip(mesh.vertices, vertices):
        assert max(abs(a - b) for a, b in zip(actual.co, expected)) < .001
    assert [p.material_index for p in mesh.polygons] == materials
    if identity == 1:
        assert bpy.context.scene.get('town_landmark_Spaceport01', 0) > 0
    elif identity == 2:
        assert bpy.context.scene.get('town_landmark_Horizon', 0) > 0
    reopened = decode(import_document(target, catalog), catalog)
    assert landmark(reopened) == identity
    assert reopened['sun'] == document['sun']
    assert reopened['curves'] == document['curves']
    assert len(reopened['items']) == len(document['items'])
    print('PASS exact native contour geometry, landmark, lighting and Blender reopen:', target, flush=True)
