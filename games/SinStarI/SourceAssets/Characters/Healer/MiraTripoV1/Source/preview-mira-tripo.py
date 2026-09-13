"""Render matched HD/reduced views under the accepted Blender lighting preview."""
import bpy
import json
from pathlib import Path
from mathutils import Vector

PACKAGE = Path(__file__).resolve().parent.parent
STAGE = Path(r'D:\AI\Mira3D\MiraTripoV1')
bpy.ops.wm.open_mainfile(filepath=str(STAGE / 'mira-tripo-baked-work.blend'))
scene = bpy.context.scene
with bpy.data.libraries.load(str(STAGE / 'mira-tripo-hd-lighting-preview.blend')) as (available, loaded):
    loaded.collections = ['SMILE Viewer Lighting Preview']
    loaded.worlds = ['SMILE Preview — Soft Ambient']
scene.collection.children.link(loaded.collections[0])
scene.world = loaded.worlds[0]
scene.render.engine = 'BLENDER_EEVEE'
scene.view_settings.view_transform = 'AgX'
scene.view_settings.look = 'AgX - Medium High Contrast'
scene.render.resolution_x = 700
scene.render.resolution_y = 960
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGB'
camera_data = bpy.data.cameras.new('Inspection Camera')
camera = bpy.data.objects.new('Inspection Camera', camera_data)
scene.collection.objects.link(camera)
scene.camera = camera
camera_data.type = 'ORTHO'
source = bpy.data.objects['Mira.Tripo.HD.Original']
body = bpy.data.objects['Mira.Tripo.Reduced']
views = [('front', (0, -2, 0.65), (0, 0, 0.50), 1.10),
         ('back', (0.4, 2, 0.8), (0, 0, 0.50), 1.10),
         ('face', (0.25, -1.5, 1.0), (0, -0.02, 0.855), 0.31)]
for name, mesh in [('hd', source), ('reduced', body)]:
    source.hide_render = mesh != source
    body.hide_render = mesh != body
    mesh.hide_set(False)
    for view, location, target, scale in views:
        camera.location = location
        camera.rotation_euler = (Vector(target) - camera.location).to_track_quat('-Z', 'Y').to_euler()
        camera_data.ortho_scale = scale
        path = PACKAGE / 'Previews' / f'mira-tripo-{name}-{view}.png'
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        print('PREVIEW_READY ' + str(path), flush=True)
source.hide_render = True
source.hide_set(True)
body.hide_render = False
body.hide_set(False)
bpy.ops.wm.save_as_mainfile(filepath=str(STAGE / 'mira-tripo-preview-work.blend'), compress=True)
print('PREVIEWS_COMPLETE', flush=True)
