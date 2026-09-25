"""Lighting, saved viewpoints, Blender delivery and reproducible preview renders."""
import json
import math

import bpy
from mathutils import Vector

from delivery import repair_normals


def camera(collection, name, position, target, ortho=None, lens=40):
    data = bpy.data.cameras.new(name)
    data.clip_end = 2000
    data.lens = lens
    if ortho:
        data.type = 'ORTHO'
        data.ortho_scale = ortho
    obj = bpy.data.objects.new(name, data)
    collection.objects.link(obj)
    obj.location = position
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()
    return obj


def finish(package, placements, draft=False):
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene['Created By'] = 'Louiery R. Sincioco (Sin)'
    scene['Title'] = 'NERIS — City of the First Light'
    scene['Status'] = 'Town exterior concept. No gameplay, interiors, collision or navmesh.'
    scene['Layout Reference'] = 'Paseo, Phantasy Star II — central landmark and connected service courts.'
    scene['Coordinates'] = 'Meters; Z up; north +Y; south entrance -Y.'
    scene['Building Sources'] = 'Five unchanged ModelsV1 GLBs, embedded locally.'
    for collection in bpy.data.collections:
        repair_normals(collection)
    scene.render.engine = 'BLENDER_EEVEE'
    scene.eevee.taa_render_samples = 32 if draft else 64
    scene.render.resolution_x = 1440 if draft else 1920
    scene.render.resolution_y = 810 if draft else 1080
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.world.use_nodes = True
    bg = scene.world.node_tree.nodes['Background']
    bg.inputs['Color'].default_value = (.39, .56, .69, 1)
    bg.inputs['Strength'].default_value = .45
    scene.view_settings.view_transform = 'AgX'
    scene.view_settings.look = 'AgX - Medium High Contrast'
    scene.render.film_transparent = False
    stage = bpy.data.collections.new('05 Cameras and Lighting')
    scene.collection.children.link(stage)
    sun = bpy.data.lights.new('Warm Afternoon Sun', 'SUN')
    sun.energy = 2.5
    sun.angle = .15
    sun.color = (1, .88, .7)
    obj = bpy.data.objects.new(sun.name, sun)
    stage.objects.link(obj)
    obj.rotation_euler = (math.radians(28), math.radians(-25), math.radians(-35))
    fill = bpy.data.lights.new('Large Sky Fill', 'AREA')
    fill.energy, fill.size = 65000, 100
    obj = bpy.data.objects.new(fill.name, fill)
    stage.objects.link(obj)
    obj.location = (0, 0, 70)
    cameras = [
        camera(stage, '01 Town Overview', (115, -155, 143), (0, 1, 1), 161),
        camera(stage, '02 Town Plan', (0, 0, 180), (0, 0, 0), 108),
        camera(stage, '03 Arrival Promenade', (0, -51, 3.3), (0, 4, 7), lens=23),
        camera(stage, '04 Market Walk', (22, -33, 3.3), (31, 9, 6), lens=25),
        camera(stage, '05 Garden Homes', (-15, -24, 6), (-34, 9, 6), lens=29),
    ]
    for i, cam in enumerate(cameras):
        marker = scene.timeline_markers.new(cam.name, frame=i + 1)
        marker.camera = cam
    scene.frame_end = len(cameras)
    scene.camera = cameras[0]
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == 'VIEW_3D':
                space = area.spaces.active
                space.shading.type = 'MATERIAL'
                space.shading.studiolight_rotate_z = .5
                space.overlay.show_overlays = False
                space.region_3d.view_distance = 135
                space.region_3d.view_location = (0, 0, 3)
                space.region_3d.view_rotation = cameras[0].rotation_euler.to_quaternion()
                space.clip_end = 2000
    bpy.ops.object.select_all(action='DESELECT')
    bpy.context.view_layer.objects.active = None
    bpy.ops.wm.save_as_mainfile(filepath=str(package / 'Blend' / 'Neris-Town-V1.blend'), compress=True)
    (package / 'placements.json').write_text(json.dumps(placements, indent=2) + '\n', encoding='utf-8')
    for i, cam in enumerate(cameras[:1] if draft else cameras):
        # Timeline camera markers also run during render evaluation. Match the
        # frame to its marker so it cannot silently reset to the overview camera.
        scene.frame_set(i + 1)
        scene.camera = cam
        scene.render.resolution_x = 1440 if draft else 1920
        scene.render.resolution_y = (1920 if i == 1 else 1080) if not draft else 810
        label = ['overview', 'plan', 'arrival', 'market', 'homes'][i]
        scene.render.filepath = str(package / 'Previews' / ('Neris-Town-' + label + '.png'))
        print('NERIS_TOWN_RENDER', label, flush=True)
        bpy.ops.render.render(write_still=True)
    print('NERIS_TOWN_COMPLETE', len(scene.objects), 'scene objects', flush=True)
