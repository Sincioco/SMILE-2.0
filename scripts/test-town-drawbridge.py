"""Regression: a moved Royal Court drawbridge clears an overlapping painted road.

Run with installed Blender --background --python-exit-code 1 --python this-file.
Only constructs an in-memory scene; never writes the user's town or source assets.
"""
import sys
from pathlib import Path

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'tools/Character3DViewer'))
from town_blender_landmarks import append_court

append_court((375.25, -140.5))
bpy.context.view_layer.update()
root = bpy.data.objects['Royal Court Assembly']
assert abs(root.matrix_world.translation.x - (-366 + 37.525)) < .0001
assert abs(root.matrix_world.translation.y - (244 - 14.05)) < .0001
assert abs(root.matrix_world.translation.z - .412) < .0001
leaf = bpy.data.objects['NC.Bridge.Leaf.Main']
road_top = .212
top = max((leaf.matrix_world @ v.co).z for v in leaf.data.vertices)
assert top - road_top > .08, ('Road intersects the recessed body between planks', top)
planks = [obj for obj in bpy.context.scene.objects if obj.name.startswith('NC.Bridge.Plank.')]
assert len(planks) == 32
for obj in planks:
    plank_top = max((obj.matrix_world @ v.co).z for v in obj.data.vertices)
    assert abs(plank_top - .412) < .0001, ('Blender/native walking height differs', obj.name, plank_top)
print('PASS relocated Blender court retains authored placement and all 32 bridge planks clear painted roads')
