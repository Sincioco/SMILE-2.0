"""Replace only Horizon in the previous review town; never write live Viewer saves."""
import bpy
import math
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Town/r007'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'Town/r006/Neris-Town-Horizon-r006.blend'))
scene = bpy.context.scene
old = bpy.data.collections['Horizon Airport - West']
before = {o.name for o in scene.objects if o.name not in old.all_objects}
for obj in list(old.all_objects):
    bpy.data.objects.remove(obj, do_unlink=True)
bpy.data.collections.remove(old)
with bpy.data.libraries.load(str(ROOT / 'Source/Neris-Horizon-Gentle-Wave-r007.blend'), link=False) as (available, loaded):
    loaded.scenes = ['Horizon Gentle Wave r007']
source = loaded.scenes[0]
collection = bpy.data.collections.new('Horizon Airport - West')
scene.collection.children.link(collection)
anchor = bpy.data.objects.new('Horizon Full Size Placement', None)
collection.objects.link(anchor)
anchor.location = (-740, 125, .212)
anchor.rotation_euler.z = math.pi / 2
count = 0
for obj in list(source.objects):
    if not (obj.get('horizon_asset') or obj.get('horizon_internal_light') or obj.get('runway_aircraft_preview')):
        continue
    collection.objects.link(obj)
    if obj.parent is None:
        obj.parent = anchor
    count += 1
bpy.data.scenes.remove(source)
bpy.context.window.scene = scene
assert before.issubset({o.name for o in scene.objects})
assert len([o for o in scene.objects if o.get('town_assembly')]) == 362
scene['horizon_revision'] = 'r007'
scene.camera = scene.objects['Review Town Hero']
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'Neris-Town-Horizon-r007.blend'), compress=True)
shutil.copyfile(ROOT / 'Town/r006/Neris-Town-Horizon-r006.town', OUT / 'Neris-Town-Horizon-r007.town')
print('PASS R007 TOWN: 362 placements, ' + str(len(before)) + ' unrelated objects preserved; ' + str(count) + ' airport objects; live saves untouched.', flush=True)
