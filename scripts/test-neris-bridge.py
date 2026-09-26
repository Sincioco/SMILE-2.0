"""Blender regression: the comparison bridge support must not fight its grout.

Run with Blender --background <Neris-Town-Waterfront.blend> --python-exit-code 1
--python scripts/test-neris-bridge.py. Inspects the saved production meshes.
"""
import bpy
import json
from pathlib import Path
from mathutils import Vector


def heights(name):
    obj = bpy.data.objects[name]
    return [float((obj.matrix_world @ vertex.co).z) for vertex in obj.data.vertices]


assert bpy.app.background, 'Run this check in background Blender.'
bpy.context.view_layer.update()
decks=[o.name for o in bpy.data.objects if o.name.startswith('Waterfront Bridge Deck')]
assert len(decks)==10, 'Every actual water crossing needs its bridge support'
grout_bottom=min(heights('Waterfront Grout'))
tile_bottom=min(heights('Waterfront Slate Tiles'))
for name in decks:
    deck_top=max(heights(name))
    assert grout_bottom-deck_top >= .035, name+' crowds the grout surface'
assert tile_bottom-grout_bottom >= .02, 'Tile/grout separation is insufficient'
assert abs(tile_bottom-.212)<.0001, 'Bridge walking surface changed height'
layout=json.loads((Path(bpy.data.filepath).parents[1]/'expansion-layout.json').read_text())
for obj in bpy.data.objects:
    if not obj.name.startswith(('Waterfront Bridge Parapet','Waterfront Bridge Bronze')):continue
    points=[obj.matrix_world@Vector(v) for v in obj.bound_box]
    if max(p.y for p in points)>=0:continue
    for p in points:
        assert any(a-.001<=p.x<=c+.001 and b-.001<=p.y<=d+.001
                   for a,b,c,d in layout['waterRectangles']), obj.name+' protrudes onto a civic landing'
print('PASS civic bridge rails stop at the water banks',flush=True)
print('PASS all ten bridge layers remain separated at the accepted walking height',flush=True)
