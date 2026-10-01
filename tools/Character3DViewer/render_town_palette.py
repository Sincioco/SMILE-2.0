"""Render the actual immutable Neris assemblies into a compact editor thumbnail atlas."""
import array
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
TOWN = ROOT / 'games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1'
sys.path.insert(0, str(TOWN / 'Source'))
from catalog_native import palette_slots
catalog = json.loads((TOWN / 'Authoring/catalog.json').read_text(encoding='utf-8'))
slots = palette_slots(catalog)
bpy.ops.wm.open_mainfile(filepath=str(TOWN / 'Authoring/Catalog.blend'), use_scripts=False)
scene = bpy.context.scene
scene.render.engine = 'BLENDER_WORKBENCH'
scene.display.shading.light = 'STUDIO'
scene.display.shading.color_type = 'MATERIAL'
scene.display.shading.show_shadows = True
scene.display.shading.show_cavity = True
scene.display.shading.cavity_type = 'BOTH'
scene.display.shading.background_type = 'WORLD'
scene.world.color = (.045, .075, .10)
scene.render.film_transparent = True
scene.render.resolution_x, scene.render.resolution_y = 128, 96
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
camera = bpy.data.objects.new('Palette Camera', bpy.data.cameras.new('Palette Camera'))
scene.collection.objects.link(camera)
camera.data.type = 'ORTHO'
scene.camera = camera
samples = {}
for item in catalog['instances']:
    samples.setdefault(item['template'], item)
# Procedural landforms have catalog meshes but no placed example in Catalog.blend.
# Import their actual source geometry so every visible palette entry has a picture.
missing = [t for t in catalog['templates'] if t['id'] not in samples]
for chunk in sorted({part[0] for t in missing for part in t['parts']}):
    bpy.ops.import_scene.gltf(filepath=str(TOWN / 'Authoring' / catalog['chunks'][chunk]['file']))
for template in missing:
    members = [obj.name for obj in scene.objects if obj.type == 'MESH'
               and obj.name.startswith(template['label'] + ' ')]
    if len(members) != len(template['parts']):
        raise ValueError('Missing palette geometry: ' + template['label'])
    samples[template['id']] = dict(members=members, position=(0, 0, 0),
                                   rotation=(0, 0, 0), scale=(1, 1, 1))
objects = list(scene.objects)
columns, rows = 5, math.ceil(len(set(slots)) / 5)
width, height = columns*128, rows*96
pixels = array.array('f', [0]) * (width*height*4)
out = TOWN / 'Authoring/Town-Palette.png'
out.parent.mkdir(parents=True, exist_ok=True)
rendered = set()
for template in catalog['templates']:
    slot = slots[template['id']]
    if slot in rendered:
        continue
    rendered.add(slot)
    sample = samples[template['id']]
    for obj in objects:
        obj.hide_render = obj.name not in sample['members'] and obj != camera
    # Catalog bounds are local to the assembled item; scale them into world bounds.
    from mathutils import Matrix, Euler
    transform = Matrix.LocRotScale(Vector(sample['position']), Euler(sample['rotation']).to_quaternion(), Vector(sample['scale']))
    lo, hi = map(Vector, template['bounds'])
    center = transform @ ((lo + hi) / 2)
    radius = max((hi-lo).length / 2 * max(sample['scale']), .2)
    direction = Vector((.9, -1.5, 1.05)).normalized()
    camera.location = center + direction * radius * 4
    camera.rotation_euler = (center - camera.location).to_track_quat('-Z', 'Y').to_euler()
    camera.data.ortho_scale = radius * 2.9
    camera.data.clip_end = radius * 20 + 100
    # Loading an on-disk render avoids Render Result's lazy pixel storage.
    frame = out.with_name('palette-frame.png')
    scene.render.filepath = str(frame)
    bpy.ops.render.render(write_still=True)
    image = bpy.data.images.load(str(frame), check_existing=False)
    thumb = array.array('f', [0]) * (128*96*4)
    image.pixels.foreach_get(thumb)
    if not any(thumb[i] > .01 for i in range(3, len(thumb), 4)):
        raise ValueError('Empty thumbnail: ' + template['label'])
    x = slot % columns * 128
    y = (rows - 1 - slot // columns) * 96
    for row in range(96):
        start = ((y + row) * width + x) * 4
        pixels[start:start+128*4] = thumb[row*128*4:(row+1)*128*4]
    bpy.data.images.remove(image)
    frame.unlink(missing_ok=True)
    print('Thumbnail:', template['label'], flush=True)
atlas = bpy.data.images.new('Town Palette', width=width, height=height, alpha=True)
atlas.pixels.foreach_set(pixels)
atlas.filepath_raw = str(out)
atlas.file_format = 'PNG'
atlas.save()
print('PASS palette:', out, flush=True)
