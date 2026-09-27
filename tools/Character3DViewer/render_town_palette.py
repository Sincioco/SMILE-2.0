"""Render the actual immutable Neris assemblies into a compact editor thumbnail atlas."""
import array
import json
import math
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
TOWN = ROOT / 'games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1'
catalog = json.loads((TOWN / 'Authoring/catalog.json').read_text(encoding='utf-8'))
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
objects = list(scene.objects)
samples = {}
for item in catalog['instances']:
    samples.setdefault(item['template'], item)
columns, rows = 5, math.ceil(len(catalog['templates']) / 5)
width, height = columns*128, rows*96
pixels = array.array('f', [0]) * (width*height*4)
out = TOWN / 'Authoring/Town-Palette.png'
out.parent.mkdir(parents=True, exist_ok=True)
for template in catalog['templates']:
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
    x = template['id'] % columns * 128
    y = (rows - 1 - template['id'] // columns) * 96
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
