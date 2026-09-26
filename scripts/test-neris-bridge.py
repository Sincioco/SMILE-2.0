"""Blender regression: the comparison bridge support must not fight its grout.

Run with Blender --background <Neris-Town-Waterfront.blend> --python-exit-code 1
--python scripts/test-neris-bridge.py. Inspects the saved production meshes.
"""
import bpy


def heights(name):
    obj = bpy.data.objects[name]
    return [float((obj.matrix_world @ vertex.co).z) for vertex in obj.data.vertices]


assert bpy.app.background, 'Run this check in background Blender.'
bpy.context.view_layer.update()
decks=[o.name for o in bpy.data.objects if o.name.startswith('Waterfront Bridge Deck')]
assert len(decks)==8, 'Only actual water crossings need bridge supports'
grout_bottom=min(heights('Waterfront Grout'))
tile_bottom=min(heights('Waterfront Slate Tiles'))
for name in decks:
    deck_top=max(heights(name))
    assert grout_bottom-deck_top >= .035, name+' crowds the grout surface'
assert tile_bottom-grout_bottom >= .02, 'Tile/grout separation is insufficient'
assert abs(tile_bottom-.212)<.0001, 'Bridge walking surface changed height'
print('PASS all eight bridge layers remain separated at the accepted walking height',flush=True)
