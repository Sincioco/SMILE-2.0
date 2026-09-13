"""Rebake Mira1 directly from her clean reference, without high-mesh ray streaks.

Run with Blender 5.2 --background --python this_file. The selected comparison
source is immutable; outputs go to the local authoring staging directory.
"""
import bpy
import bmesh
import hashlib
import json
import math
from pathlib import Path
from mathutils.kdtree import KDTree

PACKAGE = Path(__file__).resolve().parent.parent
STAGE = Path(r'D:\AI\Mira3D\Mira1Selected')
STAGE.mkdir(parents=True, exist_ok=True)
SOURCE = PACKAGE / 'Blender/mira1-selected-comparison-source.blend'
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest() == 'a898fe163fd9a4c357f50b9941e83d1828656f97b791496d367115927fd9a6b6'
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene = bpy.context.scene
rig = bpy.data.objects['Mira.Rig']
body = bpy.data.objects['Mira.SkinnedBody']
staff = bpy.data.objects['Mira.Staff']
rig.data.pose_position = 'REST'
scene.frame_set(1)

# Coincident UV-seam vertices must share the same four-weight skin before welding.
tree = KDTree(len(body.data.vertices))
for vertex in body.data.vertices:
    tree.insert(vertex.co, vertex.index)
tree.balance()
seen = set()
seam_repairs = 0
for vertex in body.data.vertices:
    if vertex.index in seen:
        continue
    indices = [index for co, index, distance in tree.find_range(vertex.co, 0.000001)]
    seen.update(indices)
    weights = {}
    original = []
    for index in indices:
        current = {g.group: g.weight for g in body.data.vertices[index].groups}
        original.append(current)
        for group, weight in current.items():
            weights[group] = weights.get(group, 0) + weight / len(indices)
    weights = dict(sorted(weights.items(), key=lambda item: -item[1])[:4])
    total = sum(weights.values())
    if any(current != original[0] for current in original[1:]):
        seam_repairs += 1
    for group in body.vertex_groups:
        group.remove(indices)
    for group, weight in weights.items():
        body.vertex_groups[group].add(indices, weight / total, 'REPLACE')

# Preserve the original face, hands and clean authored cape through an explicit UV layer.
old_image = next(n.image for n in body.data.materials[0].node_tree.nodes if n.type == 'TEX_IMAGE')
body.data.uv_layers.active.name = 'Original'
keep = body.data.attributes.new('MiraKeepOriginal', 'FLOAT', 'POINT')
front_costume = body.data.attributes.new('MiraFrontCostume', 'FLOAT', 'POINT')
for vertex in body.data.vertices:
    names = {body.vertex_groups[g.group].name: g.weight for g in vertex.groups}
    hand = sum(weight for name, weight in names.items() if name.endswith('Hand'))
    head = min(1, max(0, (vertex.co.z - 1.46) / 0.06))
    keep.data[vertex.index].value = max(head, min(1, hand * 2), 1 if vertex.index >= 19021 else 0)
    # Side illustrations contain a lowered hand over the skirt. They cannot paint
    # the hidden cloth or the raised staff arm. Use the unobstructed front there.
    lower = min(1, max(0, (vertex.co.z - .76) / .045), max(0, (1.045 - vertex.co.z) / .04))
    staff_arm = sum(weight for name, weight in names.items()
                    if name.endswith(('RightArm', 'RightForeArm')))
    front_costume.data[vertex.index].value = max(lower, min(1, staff_arm * 2))
bm = bmesh.new()
bm.from_mesh(body.data)
bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=0.000001)
bm.to_mesh(body.data)
bm.free()
# Relax small voxel ridges on costume surfaces without adding polygons. Preserve
# the face, hands, authored cape and grip; bound every change to five millimeters.
neighbors = {v.index: set() for v in body.data.vertices}
for edge in body.data.edges:
    a, b = edge.vertices
    neighbors[a].add(b)
    neighbors[b].add(a)
original_coordinates = [v.co.copy() for v in body.data.vertices]
for step in range(6):
    positions = [v.co.copy() for v in body.data.vertices]
    strength = .32 if step % 2 == 0 else -.33
    for vertex in body.data.vertices:
        retained = body.data.attributes['MiraKeepOriginal'].data[vertex.index].value
        if retained > .01 or not neighbors[vertex.index]:
            continue
        average = sum((positions[i] for i in neighbors[vertex.index]), positions[vertex.index] * 0) / len(neighbors[vertex.index])
        proposed = positions[vertex.index] + (average - positions[vertex.index]) * strength
        displacement = proposed - original_coordinates[vertex.index]
        if displacement.length > .005:
            proposed = original_coordinates[vertex.index] + displacement.normalized() * .005
        vertex.co = proposed
for polygon in body.data.polygons:
    polygon.use_smooth = True
body.data.update()
bpy.ops.object.select_all(action='DESELECT')
body.select_set(True)
bpy.context.view_layer.objects.active = body
body.data.uv_layers.new(name='UVMap')
body.data.uv_layers.active_index = len(body.data.uv_layers) - 1
body.data.uv_layers.active.active_render = True
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(65), island_margin=0.004)
bpy.ops.object.mode_set(mode='OBJECT')

mat = bpy.data.materials.new('Mira1.CleanReferenceProjection')
mat.use_nodes = True
body.data.materials.clear()
body.data.materials.append(mat)
nodes = mat.node_tree.nodes
nodes.clear()
links = mat.node_tree.links

def scalar(operation, a, b):
    node = nodes.new('ShaderNodeMath')
    node.operation = operation
    for index, value in enumerate((a, b)):
        if hasattr(value, 'is_output'):
            links.new(value, node.inputs[index])
        else:
            node.inputs[index].default_value = value
    return node.outputs[0]

def vector(operation, a, b):
    node = nodes.new('ShaderNodeVectorMath')
    node.operation = operation
    for index, value in enumerate((a, b)):
        slot = 3 if operation == 'SCALE' and index == 1 else index
        if hasattr(value, 'is_output'):
            links.new(value, node.inputs[slot])
        else:
            node.inputs[slot].default_value = value
    return node.outputs['Value' if operation == 'DOT_PRODUCT' else 'Vector']

geom = nodes.new('ShaderNodeNewGeometry')
position = nodes.new('ShaderNodeSeparateXYZ')
links.new(geom.outputs['Position'], position.inputs[0])
reference = bpy.data.images.load(str(PACKAGE / 'References/mira-turnaround.png'), check_existing=True)
old_uv = nodes.new('ShaderNodeUVMap')
old_uv.uv_map = 'Original'
old_texture = nodes.new('ShaderNodeTexImage')
old_texture.image = old_image
links.new(old_uv.outputs['UV'], old_texture.inputs['Vector'])
weighted = []
weights = []
front_attribute = nodes.new('ShaderNodeAttribute')
front_attribute.attribute_name = 'MiraFrontCostume'
views = [((0, -1, 0), 'X', 1, 285, 465),
         ((1, 0, 0), 'Y', 1, 650, 465),
         ((0, 1, 0), 'X', -1, 1085, 465),
         ((-1, 0, 0), 'Y', -1, 1485, 465)]

def reference_color(coordinate):
    # Pad the reference silhouette in projection space. Reusing the damaged old
    # atlas at a tiny camera mismatch reintroduced its black streaks on shoulders.
    chosen, valid = None, None
    offsets = [(0, 0), (-4, 0), (4, 0), (0, -4), (0, 4), (-8, 0),
               (8, 0), (0, -8), (0, 8), (-14, 0), (14, 0), (0, -14), (0, 14)]
    for dx, dy in offsets:
        texture = nodes.new('ShaderNodeTexImage')
        texture.image = reference
        texture.extension = 'CLIP'
        shifted = vector('ADD', coordinate, (dx / 1692, dy / 929, 0))
        links.new(shifted, texture.inputs['Vector'])
        channels = nodes.new('ShaderNodeSeparateColor')
        links.new(texture.outputs['Color'], channels.inputs[0])
        magenta = scalar('MULTIPLY', scalar('GREATER_THAN', channels.outputs['Red'], scalar('MULTIPLY', channels.outputs['Green'], 1.3)), scalar('GREATER_THAN', channels.outputs['Blue'], scalar('MULTIPLY', channels.outputs['Green'], 1.3)))
        good = scalar('SUBTRACT', 1, magenta)
        color = texture.outputs['Color']
        if chosen is None:
            chosen, valid = color, good
        else:
            replace = scalar('MULTIPLY', scalar('SUBTRACT', 1, valid), good)
            chosen = vector('ADD', vector('SCALE', chosen, scalar('SUBTRACT', 1, replace)), vector('SCALE', color, replace))
            valid = scalar('MAXIMUM', valid, good)
    return chosen, valid

for direction, axis, scale, center, bottom in views:
    uv = nodes.new('ShaderNodeCombineXYZ')
    source_scale = 1.75 / (.4821260869503021 + .48963961005210876)
    depth = scalar('DIVIDE', vector('DOT_PRODUCT', geom.outputs['Position'], direction), source_scale)
    focal = scalar('DIVIDE', 455 / math.tan(math.radians(10)), scalar('SUBTRACT', .55 / math.tan(math.radians(10)), depth))
    horizontal = scalar('MULTIPLY', scalar('MULTIPLY', position.outputs[axis], scale / source_scale), focal)
    vertical = scalar('MULTIPLY', scalar('ADD', scalar('DIVIDE', position.outputs['Z'], source_scale), -.48963961005210876), focal)
    u = scalar('DIVIDE', scalar('ADD', horizontal, center), 1692)
    v = scalar('SUBTRACT', 1, scalar('DIVIDE', scalar('SUBTRACT', bottom, vertical), 929))
    links.new(u, uv.inputs['X'])
    links.new(v, uv.inputs['Y'])
    projected, valid = reference_color(uv.outputs[0])
    facing = scalar('POWER', scalar('MAXIMUM', vector('DOT_PRODUCT', geom.outputs['Normal'], direction), 0), 8)
    weight = scalar('MULTIPLY', facing, valid)
    if direction == (0, -1, 0):
        weight = scalar('MAXIMUM', weight, scalar('MULTIPLY', front_attribute.outputs['Fac'], valid))
    else:
        weight = scalar('MULTIPLY', weight, scalar('SUBTRACT', 1, front_attribute.outputs['Fac']))
    weights.append(weight)
    weighted.append(vector('SCALE', projected, weight))
total = weights[0]
color = weighted[0]
for weight, item in zip(weights[1:], weighted[1:]):
    total = scalar('ADD', total, weight)
    color = vector('ADD', color, item)
color = vector('SCALE', color, scalar('DIVIDE', 1, scalar('MAXIMUM', total, 0.000001)))
attribute = nodes.new('ShaderNodeAttribute')
attribute.attribute_name = 'MiraKeepOriginal'
empty = scalar('LESS_THAN', total, .00001)
color = vector('ADD', vector('SCALE', color, scalar('SUBTRACT', 1, empty)), vector('SCALE', (.66, .7, .72), empty))
preserve = attribute.outputs['Fac']
color = vector('ADD', vector('SCALE', color, scalar('SUBTRACT', 1, preserve)), vector('SCALE', old_texture.outputs['Color'], preserve))
emit = nodes.new('ShaderNodeEmission')
out = nodes.new('ShaderNodeOutputMaterial')
links.new(color, emit.inputs['Color'])
links.new(emit.outputs[0], out.inputs[0])
target = nodes.new('ShaderNodeTexImage')
target.image = bpy.data.images.new('Mira1.CleanBaseColor', width=4096, height=4096, alpha=False)
nodes.active = target
scene.render.engine = 'CYCLES'
scene.cycles.samples = 4
preferences = bpy.context.preferences.addons['cycles'].preferences
preferences.compute_device_type = 'OPTIX'
preferences.get_devices()
for device in preferences.devices:
    device.use = device.type == 'OPTIX'
scene.cycles.device = 'GPU'
scene.render.bake.use_selected_to_active = False
scene.render.bake.margin = 16
scene.render.bake.margin_type = 'EXTEND'
print('BAKING_DIRECT_REFERENCE', flush=True)
bpy.ops.object.bake(type='EMIT')
image = target.image
image.filepath_raw = str(STAGE / 'mira1-clean-basecolor.png')
image.file_format = 'PNG'
image.save()
image.pack()
clean = bpy.data.materials.new('Mira1.CleanCostume')
clean.use_nodes = True
shader = clean.node_tree.nodes.get('Principled BSDF')
shader.inputs['Metallic'].default_value = 0.12
shader.inputs['Roughness'].default_value = 0.58
tex = clean.node_tree.nodes.new('ShaderNodeTexImage')
tex.image = image
clean.node_tree.links.new(tex.outputs['Color'], shader.inputs['Base Color'])
body.data.materials[0] = clean
rig.data.pose_position = 'POSE'
rig.animation_data.action = bpy.data.actions['Idle']
rig.animation_data.action_slot = bpy.data.actions['Idle'].slots[0]
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(STAGE / 'mira1-retextured.blend'))
(STAGE / 'mira1-texture-repair.json').write_text(json.dumps({'sourceBlendSha256': hashlib.sha256(SOURCE.read_bytes()).hexdigest(), 'method': 'Calibrated direct reference projection, new UV layout, padded silhouette and 4K bake; original face/hands/cape retained', 'seamWeightRepairs': seam_repairs, 'maximumWeights': max(len(v.groups) for v in body.data.vertices), 'maximumCostumeRelaxationMeters': max((v.co - original_coordinates[v.index]).length for v in body.data.vertices), 'baseColorSha256': hashlib.sha256(Path(image.filepath_raw).read_bytes()).hexdigest(), 'projectionViews': views}, indent=2))
print('MIRA1_TEXTURE_READY', flush=True)
