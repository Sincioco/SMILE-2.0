"""Blender regression: editable lawns stay below castle floors; shores have no trim.

Run in background Blender. Opens the immutable catalog without saving it, then
exercises the production terrain builder and checks real mesh coordinates.
"""
from pathlib import Path
import re
import sys

import bpy

ROOT = Path(__file__).resolve().parents[1]
VIEWER = ROOT / 'tools/Character3DViewer'
sys.path.insert(0, str(VIEWER))
from town_blender_save import terrain

assert bpy.app.background
bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'games/SinStarI/SourceAssets/Towns/Neris/'
                                    'NerisTownV1/Authoring/Catalog.blend'))
bpy.context.view_layer.update()

# Measure authored top faces, rather than relying on a guessed floor elevation.
floors = {}
for name in ('Royal Castle of Neris', 'Neris Military Headquarters'):
    levels = []
    for obj in bpy.data.objects[name].children_recursive:
        if obj.type != 'MESH':
            continue
        for face in obj.data.polygons:
            if face.normal.z < .99 or face.area < 20:
                continue
            points = [obj.matrix_world @ obj.data.vertices[i].co for i in face.vertices]
            low, high = min(p.z for p in points), max(p.z for p in points)
            if high-low < .0001 and .05 < high < .25:
                levels.append(high)
    assert levels, 'Missing authored floor: ' + name
    floors[name] = min(levels)

bridge = bpy.data.objects['Royal Bridge Deck']
floors['Royal Bridge Deck'] = max((bridge.matrix_world @ v.co).z for v in bridge.data.vertices)

terrain({'columns': 3, 'rows': 2, 'cells': [1, 2, 3, 1, 2, 4],
         'xs': [-20, 0, 20, 40], 'zs': [0, 20, 40]})
mesh = bpy.data.objects['Town Editable Surface'].data
heights = {}
for face in mesh.polygons:
    heights.setdefault(face.material_index, []).extend(mesh.vertices[i].co.z for i in face.vertices)
grass = max(heights[0])
for name, floor in floors.items():
    assert floor-grass >= .1, f'{name}: lawn {grass:.4f} crowds floor {floor:.4f}'
    print(f'PASS {name}: lawn clearance {floor-grass:.3f} m', flush=True)

assert abs(min(heights[1])-.085) < .00001, 'Water height changed'
assert abs(max(heights[2])-.212) < .00001, 'Road/bridge height changed'
assert min(heights[2])-floors['Royal Bridge Deck'] >= .06, 'Bridge paving crowds the authored support'
assert 3 in heights and 4 in heights, 'Bridge rail walls/caps were removed'

# Compare the native upload height with the actual Blender output (XZY, x10, +21).
native = (VIEWER / 'TownSurfaceRenderer.smile').read_text(encoding='utf-8')
upload = native.split('Private Sub UploadNext', 1)[1]
native_grass = float(re.search(r'^    Height = (-?[\d.]+)$', upload, re.M)[1])
assert abs(native_grass - (grass*10+21)) < .00001, 'Native/Blender terrain height differs'
assert 'Grid.Bank(' not in native, 'Native shoreline trim returned'

# Without a bridge there must be no trim geometry around either water or road.
bpy.data.objects.remove(bpy.data.objects['Town Editable Surface'], do_unlink=True)
terrain({'columns': 3, 'rows': 1, 'cells': [1, 2, 3],
         'xs': [-20, 0, 20, 40], 'zs': [0, 20]})
mesh = bpy.data.objects['Town Editable Surface'].data
assert len(mesh.polygons) == 3 and {p.material_index for p in mesh.polygons} == {0, 1, 2}
print('PASS clean shorelines, retained bridge rails and native/Blender height parity', flush=True)
