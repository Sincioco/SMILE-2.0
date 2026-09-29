"""Bake runway paint into the pavement so native mip filtering smooths distant lines."""
import bpy
from pathlib import Path


def apply(scene, destination):
    destination = Path(destination)
    destination.parent.mkdir(parents=True, exist_ok=True)
    surfaces = []
    paint = []
    for obj in scene.objects:
        if obj.get('assembly') != 'Twin Rear Runways':
            continue
        if 'Asphalt' in obj.name or obj.name.startswith('Runway U Turn Road'):
            surfaces.append(obj)
        elif obj.name.startswith(('Runway White', 'Runway Center Dash', 'Runway Threshold',
                                  'Threshold Piano', 'Runway Aiming', 'Runway Number',
                                  'U Turn Gold Centerline')):
            paint.append(obj)
    assert len(surfaces) >= 2 and len(paint) > 50

    # Orthographic emission-only capture contains authored paint, not shadows or
    # reflections. The same packed image is used in Blender and the portable GLB.
    bake = bpy.data.scenes.new('Runway Paint Bake')
    bake.render.engine = 'CYCLES'
    bake.cycles.samples = 8
    bake.cycles.use_denoising = False
    bake.render.resolution_x = 4096
    bake.render.resolution_y = 640
    bake.render.resolution_percentage = 100
    bake.render.pixel_aspect_x = 25 / 24
    bake.render.pixel_aspect_y = 1
    bake.render.image_settings.file_format = 'PNG'
    bake.render.image_settings.color_mode = 'RGB'
    bake.view_settings.view_transform = 'Standard'
    bake.view_settings.look = 'None'
    bake.render.filepath = str(destination)
    materials = {}
    for source in surfaces + paint:
        obj = source.copy()
        obj.data = source.data.copy()
        bake.collection.objects.link(obj)
        for index, original in enumerate(source.data.materials):
            if original.name not in materials:
                material = bpy.data.materials.new('Bake ' + original.name)
                material.use_nodes = True
                nodes = material.node_tree.nodes
                nodes.clear()
                emission = nodes.new('ShaderNodeEmission')
                emission.inputs['Color'].default_value = original.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value
                output = nodes.new('ShaderNodeOutputMaterial')
                material.node_tree.links.new(emission.outputs[0], output.inputs['Surface'])
                materials[original.name] = material
            obj.data.materials[index] = materials[original.name]
    camera = bpy.data.objects.new('Runway Paint Camera', bpy.data.cameras.new('Runway Paint Camera'))
    bake.collection.objects.link(camera)
    camera.location = (0, 312.5, 100)
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = 900
    bake.camera = camera
    bpy.ops.render.render(write_still=True, scene=bake.name)
    bpy.data.batch_remove(ids=tuple(bake.objects))
    bpy.data.scenes.remove(bake)

    image = bpy.data.images.load(str(destination), check_existing=False)
    image.name = 'Horizon Runway Paint r008'
    image.pack()
    material = bpy.data.materials.new('GW Filtered Runway Surface')
    material.use_nodes = True
    material['neris_planar_color_texture'] = str(destination)
    material['neris_planar_bounds'] = [-450, 245, 900, 135]
    shader = material.node_tree.nodes['Principled BSDF']
    shader.inputs['Base Color'].default_value = (1, 1, 1, 1)
    shader.inputs['Roughness'].default_value = .9
    texture = material.node_tree.nodes.new('ShaderNodeTexImage')
    texture.image = image
    texture.interpolation = 'Linear'
    material.node_tree.links.new(texture.outputs['Color'], shader.inputs['Base Color'])
    for obj in surfaces:
        obj.data.materials.clear()
        obj.data.materials.append(material)
        uv = obj.data.uv_layers.new(name='Runway Paint')
        for loop in obj.data.loops:
            p = obj.matrix_world @ obj.data.vertices[loop.vertex_index].co
            uv.data[loop.index].uv = ((p.x + 450) / 900, (p.y - 245) / 135)
        obj['runway_filtered_surface'] = True
    bpy.data.batch_remove(ids=tuple(paint))
    scene['runway_markings'] = 'Packed paint texture with native anisotropic mip filtering'
    print('PASS FILTERED RUNWAYS: baked ' + str(len(paint)) + ' paint objects into 4096x640 pavement texture', flush=True)
