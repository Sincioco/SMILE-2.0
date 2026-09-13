"""Bake the preserved HD staff onto its closed reduced surface at 2K."""
import bpy
import bmesh
import json
import math
from pathlib import Path

STAGE = Path(r'D:\AI\Mira3D\MiraTripoV1')
bpy.ops.wm.open_mainfile(filepath=str(STAGE / 'mira-staff-reduced-work.blend'))
scene = bpy.context.scene
source = bpy.data.objects['Mira.Staff.HD.Source']
staff = bpy.data.objects['Mira.Staff.Reduced']
report = json.loads((STAGE / 'staff-reduction.json').read_text())
assert report['triangles'] <= 1500, report
assert report['boundary_edges'] == report['nonmanifold_edges'] == report['nonmanifold_vertices'] == 0
for p in staff.data.polygons:
    p.use_smooth = True
bpy.ops.object.select_all(action='DESELECT')
staff.select_set(True)
bpy.context.view_layer.objects.active = staff
staff.data.uv_layers.new(name='UVMap')
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=.004, area_weight=.3)
bpy.ops.object.mode_set(mode='OBJECT')
material = bpy.data.materials.new('Mira.Staff.2K.PBR')
material.use_nodes = True
staff.data.materials.clear()
staff.data.materials.append(material)
nodes, links = material.node_tree.nodes, material.node_tree.links
shader = next(n for n in nodes if n.type == 'BSDF_PRINCIPLED')
target = nodes.new('ShaderNodeTexImage')
nodes.active = target
original = source.data.materials[0]
sn, sl = original.node_tree.nodes, original.node_tree.links
output = next(n for n in sn if n.type == 'OUTPUT_MATERIAL')
principled = next(n for n in sn if n.type == 'BSDF_PRINCIPLED')
base_source = next(n for n in sn if n.type == 'TEX_IMAGE' and n.image.name.endswith('_0'))
orm_source = next(n for n in sn if n.type == 'TEX_IMAGE' and n.image.name.endswith('_1'))
emission = sn.new('ShaderNodeEmission')
source.hide_set(False)
source.hide_render = False
source.select_set(True)
scene.render.engine = 'CYCLES'
prefs = bpy.context.preferences.addons['cycles'].preferences
prefs.compute_device_type = 'OPTIX'
prefs.get_devices()
for device in prefs.devices:
    device.use = device.type == 'OPTIX'
scene.cycles.device = 'GPU'
scene.cycles.samples = 16
scene.render.bake.use_selected_to_active = True
scene.render.bake.cage_extrusion = .004
scene.render.bake.max_ray_distance = .012
scene.render.bake.margin = 8
scene.render.bake.margin_type = 'EXTEND'


def bake(name, kind, color_space):
    image = bpy.data.images.new(name, width=2048, height=2048, alpha=False)
    image.colorspace_settings.name = color_space
    target.image = image
    nodes.active = target
    print('BAKING '+name,flush=True)
    bpy.ops.object.bake(type=kind)
    image.filepath_raw = str(STAGE / (name + '.png'))
    image.file_format = 'PNG'
    image.save()
    image.pack()
    return image


sl.new(base_source.outputs['Color'], emission.inputs['Color'])
sl.new(emission.outputs[0], output.inputs['Surface'])
base = bake('mira-staff-basecolor-2k', 'EMIT', 'sRGB')
sl.new(orm_source.outputs['Color'], emission.inputs['Color'])
orm = bake('mira-staff-orm-2k', 'EMIT', 'Non-Color')
sl.new(principled.outputs[0], output.inputs['Surface'])
normal = bake('mira-staff-normal-2k', 'NORMAL', 'Non-Color')
sn.remove(emission)
nodes.remove(target)
base_node = nodes.new('ShaderNodeTexImage')
base_node.image = base
links.new(base_node.outputs['Color'], shader.inputs['Base Color'])
orm_node = nodes.new('ShaderNodeTexImage')
orm_node.image = orm
channels = nodes.new('ShaderNodeSeparateColor')
links.new(orm_node.outputs['Color'], channels.inputs[0])
links.new(channels.outputs['Green'], shader.inputs['Roughness'])
links.new(channels.outputs['Blue'], shader.inputs['Metallic'])
normal_node = nodes.new('ShaderNodeTexImage')
normal_node.image = normal
normal_map = nodes.new('ShaderNodeNormalMap')
links.new(normal_node.outputs['Color'], normal_map.inputs['Color'])
links.new(normal_map.outputs['Normal'], shader.inputs['Normal'])
source.hide_render = True
source.hide_set(True)
source.select_set(False)
scene.render.bake.use_selected_to_active = False
bpy.ops.wm.save_as_mainfile(filepath=str(STAGE / 'mira-staff-baked-work.blend'),compress=True)
print('STAFF_BAKE_READY',flush=True)
