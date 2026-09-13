"""Repair the reduced copy and rebake the HD material onto its new 4K UV atlas."""
import bpy
import bmesh
import json
import math
import numpy as np
import time
from pathlib import Path

PACKAGE = Path(__file__).resolve().parent.parent
STAGE = Path(r'D:\AI\Mira3D\MiraTripoV1')
START = time.monotonic()


def progress(message):
    print(f'[{time.monotonic() - START:.1f}s] {message}', flush=True)


bpy.ops.wm.open_mainfile(filepath=str(STAGE / 'mira-tripo-reduction-work.blend'))
scene = bpy.context.scene
source = bpy.data.objects['Mira.Tripo.HD.Original']
body = bpy.data.objects['Mira.Tripo.Reduced']
bm = bmesh.new()
bm.from_mesh(body.data)
collapsed = 0
maximum_move = 0.0
for repair_pass in range(8):
    defects = [e for e in bm.edges if len(e.link_faces) > 2]
    if not defects:
        break
    for edge in defects:
        if edge.is_valid and len(edge.link_faces) > 2:
            assert edge.calc_length() < 0.006, 'Review a defect before changing a larger surface'
            maximum_move = max(maximum_move, edge.calc_length() / 2)
            bmesh.ops.pointmerge(bm, verts=list(edge.verts),
                                 merge_co=(edge.verts[0].co + edge.verts[1].co) / 2)
            collapsed += 1
boundaries = [e for e in bm.edges if e.is_boundary]
filled = bmesh.ops.holes_fill(bm, edges=boundaries, sides=0)['faces']
bmesh.ops.triangulate(bm, faces=list(bm.faces))
bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
# Split faces that touch only at a pinched vertex into independent surface fans.
# This changes topology without moving or thickening the visible surface.
coordinates, face_corners = [], {}
split_fans = 0
for vertex in bm.verts:
    remaining = set(vertex.link_faces)
    fan_count = 0
    while remaining:
        first = remaining.pop()
        pending, fan = [first], {first}
        while pending:
            face = pending.pop()
            for edge in face.edges:
                if vertex not in edge.verts:
                    continue
                for adjacent in edge.link_faces:
                    if adjacent in remaining:
                        remaining.remove(adjacent)
                        pending.append(adjacent)
                        fan.add(adjacent)
        index = len(coordinates)
        coordinates.append(tuple(vertex.co))
        for face in fan:
            face_corners[(vertex, face)] = index
        fan_count += 1
    split_fans += max(0, fan_count - 1)
faces = [[face_corners[(vertex, face)] for vertex in face.verts] for face in bm.faces]
mesh = bpy.data.meshes.new('Mira.Tripo.RepairedSurface')
mesh.from_pydata(coordinates, [], faces)
mesh.update()
bm.free()
body.data = mesh
bm = bmesh.new()
bm.from_mesh(mesh)
info = {'collapsed_defect_edges': collapsed, 'maximum_vertex_move_m': maximum_move,
        'filled_faces': len(filled), 'split_pinched_fans': split_fans, 'triangles': len(bm.faces),
        'boundary_edges': sum(e.is_boundary for e in bm.edges),
        'non_manifold_edges': sum(not e.is_manifold for e in bm.edges),
        'non_manifold_vertices': sum(not v.is_manifold for v in bm.verts),
        'degenerate_faces': sum(f.calc_area() < 1e-12 for f in bm.faces)}
(STAGE / 'surface-repair.json').write_text(json.dumps(info, indent=2) + '\n')
progress('Surface repair: ' + json.dumps(info))
assert info['boundary_edges'] == info['non_manifold_edges'] == info['non_manifold_vertices'] == info['degenerate_faces'] == 0, info
bm.to_mesh(body.data)
bm.free()
body.data.update()
for polygon in body.data.polygons:
    polygon.use_smooth = True
bpy.ops.object.select_all(action='DESELECT')
body.hide_set(False)
body.select_set(True)
bpy.context.view_layer.objects.active = body
for layer in list(body.data.uv_layers):
    body.data.uv_layers.remove(layer)
body.data.uv_layers.new(name='UVMap')
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.003,
                         area_weight=0.3, correct_aspect=True)
bpy.ops.object.mode_set(mode='OBJECT')
progress('Reduced UV atlas ready')

original_material = source.data.materials[0]
original_nodes = original_material.node_tree.nodes
original_links = original_material.node_tree.links
original_output = next(n for n in original_nodes if n.type == 'OUTPUT_MATERIAL')
original_shader = next(n for n in original_nodes if n.type == 'BSDF_PRINCIPLED')
color_image = next(n for n in original_nodes if n.type == 'TEX_IMAGE' and n.image.name.endswith('_0'))
orm_image = next(n for n in original_nodes if n.type == 'TEX_IMAGE' and n.image.name.endswith('_1'))
emission = original_nodes.new('ShaderNodeEmission')
material = bpy.data.materials.new('Mira.Tripo.4K.PBR')
material.use_nodes = True
nodes = material.node_tree.nodes
links = material.node_tree.links
shader = next(n for n in nodes if n.type == 'BSDF_PRINCIPLED')
body.data.materials.clear()
body.data.materials.append(material)
target_node = nodes.new('ShaderNodeTexImage')
nodes.active = target_node
source.hide_set(False)
source.hide_render = False
source.select_set(True)
body.select_set(True)
bpy.context.view_layer.objects.active = body
scene.render.engine = 'CYCLES'
preferences = bpy.context.preferences.addons['cycles'].preferences
preferences.compute_device_type = 'OPTIX'
preferences.get_devices()
devices = []
for device in preferences.devices:
    device.use = device.type == 'OPTIX'
    if device.use:
        devices.append(device.name)
assert devices, 'Expected the installed RTX GPU for background texture baking'
scene.cycles.device = 'GPU'
scene.cycles.samples = 16
scene.render.bake.use_selected_to_active = True
scene.render.bake.use_clear = True
scene.render.bake.cage_extrusion = 0.006
scene.render.bake.max_ray_distance = 0.014
scene.render.bake.margin = 12
scene.render.bake.margin_type = 'EXTEND'
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGB'
scene.render.image_settings.color_depth = '8'
progress('Bake device: ' + ', '.join(devices))


def bake(name, bake_type, color_space='Non-Color'):
    image = bpy.data.images.new(name, width=4096, height=4096, alpha=False)
    image.colorspace_settings.name = color_space
    target_node.image = image
    nodes.active = target_node
    progress('Baking ' + name)
    bpy.ops.object.bake(type=bake_type)
    image.filepath_raw = str(STAGE / (name + '.png'))
    image.file_format = 'PNG'
    image.save()
    image.pack()
    return image


original_links.new(color_image.outputs['Color'], emission.inputs['Color'])
original_links.new(emission.outputs[0], original_output.inputs['Surface'])
base = bake('mira-tripo-basecolor-4k', 'EMIT', 'sRGB')
orm_output = original_nodes.get('SMILE.CorrectedORM') or orm_image
original_links.new(orm_output.outputs['Color'], emission.inputs['Color'])
orm = bake('mira-tripo-orm-4k', 'EMIT')
original_links.new(original_shader.outputs[0], original_output.inputs['Surface'])
normal = bake('mira-tripo-normal-4k', 'NORMAL')
ao_node = original_nodes.new('ShaderNodeAmbientOcclusion')
ao_node.inputs['Distance'].default_value = 0.02
ao_node.samples = 16
ao_node.only_local = True
original_links.new(ao_node.outputs['AO'], emission.inputs['Color'])
original_links.new(emission.outputs[0], original_output.inputs['Surface'])
ao = bake('mira-tripo-ao-4k', 'EMIT')
original_links.new(original_shader.outputs[0], original_output.inputs['Surface'])
original_nodes.remove(emission)
original_nodes.remove(ao_node)

# glTF's packed ORM red channel stores subtle, local ambient occlusion.
orm_pixels = np.empty(4096 * 4096 * 4, dtype=np.float32)
ao_pixels = np.empty_like(orm_pixels)
orm.pixels.foreach_get(orm_pixels)
ao.pixels.foreach_get(ao_pixels)
orm_pixels[0::4] = np.clip(ao_pixels[0::4], 0, 1)
orm.pixels.foreach_set(orm_pixels)
orm.update()
orm.save()
orm.pack()
nodes.remove(target_node)
base_node = nodes.new('ShaderNodeTexImage')
base_node.image = base
links.new(base_node.outputs['Color'], shader.inputs['Base Color'])
normal_node = nodes.new('ShaderNodeTexImage')
normal_node.image = normal
normal_map = nodes.new('ShaderNodeNormalMap')
links.new(normal_node.outputs['Color'], normal_map.inputs['Color'])
links.new(normal_map.outputs['Normal'], shader.inputs['Normal'])
orm_node = nodes.new('ShaderNodeTexImage')
orm_node.image = orm
channels = nodes.new('ShaderNodeSeparateColor')
links.new(orm_node.outputs['Color'], channels.inputs['Color'])
links.new(channels.outputs['Green'], shader.inputs['Roughness'])
links.new(channels.outputs['Blue'], shader.inputs['Metallic'])
occlusion_group = bpy.data.node_groups.new('glTF Material Output', 'ShaderNodeTree')
occlusion_group.interface.new_socket(name='Occlusion', in_out='INPUT', socket_type='NodeSocketFloat')
occlusion = nodes.new('ShaderNodeGroup')
occlusion.node_tree = occlusion_group
links.new(channels.outputs['Red'], occlusion.inputs['Occlusion'])
source.hide_render = True
source.hide_set(True)
source.select_set(False)
scene.render.bake.use_selected_to_active = False
bpy.ops.wm.save_as_mainfile(filepath=str(STAGE / 'mira-tripo-baked-work.blend'), compress=True)
progress('BAKE_READY')
