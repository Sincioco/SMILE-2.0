"""Locally relax generated nasal/eye folds on an HD copy, retaining UVs and identity."""
import bpy
import json
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform

PACKAGE = Path(__file__).resolve().parent.parent
STAGE = Path(r'D:\AI\Mira3D\MiraTripoV1')
bpy.ops.wm.open_mainfile(filepath=str(PACKAGE / 'Blender/mira-tripo-hd-original.blend'))
body = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
mesh = body.data
positions = np.empty(len(mesh.vertices) * 3, dtype=np.float32)
mesh.vertices.foreach_get('co', positions)
positions = positions.reshape(-1, 3)
original_positions = positions.copy()
# Treat UV-seam duplicates as one position while retaining the original UV loops.
unique, inverse = np.unique(np.round(positions, 7), axis=0, return_inverse=True)
original = unique.copy()
radius = np.sqrt(np.sum(((unique - (0, -0.077, 0.887)) / (0.018, 0.021, 0.010)) ** 2, axis=1))
weight = np.clip((1 - radius) / 0.45, 0, 1)
weight = weight * weight * (3 - 2 * weight)
for x in (-0.023, 0.023):
    eye_radius = np.sqrt(np.sum(((unique - (x, -0.067, 0.916)) / (0.017, 0.022, 0.010)) ** 2, axis=1))
    eye_weight = np.clip((1 - eye_radius) / 0.4, 0, 1)
    eye_weight = eye_weight * eye_weight * (3 - 2 * eye_weight)
    weight = np.maximum(weight, eye_weight)
selected = np.flatnonzero(weight > 0)
local_indices = np.full(len(unique), -1, dtype=np.int32)
local_indices[selected] = np.arange(len(selected))
edges = np.empty(len(mesh.edges) * 2, dtype=np.int32)
mesh.edges.foreach_get('vertices', edges)
edges = inverse[edges.reshape(-1, 2)]
edges = edges[(weight[edges[:, 0]] > 0) | (weight[edges[:, 1]] > 0)]
edges = np.unique(np.sort(edges, axis=1), axis=0)
directed = np.concatenate((edges, edges[:, ::-1]), axis=0)
directed = directed[weight[directed[:, 0]] > 0]
centers = local_indices[directed[:, 0]]
neighbors = directed[:, 1]
counts = np.bincount(centers, minlength=len(selected)).clip(1)
for iteration in range(70):
    for amount in (0.5,):
        average = np.stack([np.bincount(centers, weights=unique[neighbors, axis],
                                       minlength=len(selected)) / counts for axis in range(3)], axis=1)
        unique[selected] += amount * weight[selected, None] * (average - unique[selected])
positions[:] = unique[inverse]
mesh.vertices.foreach_set('co', positions.ravel())
mesh.update()
# Preserve facial artwork in its original frontal position as the surface relaxes.
# Reproject UVs from the original front surface instead of dragging brows/irises.
loop_vertices = np.empty(len(mesh.loops), dtype=np.int32)
mesh.loops.foreach_get('vertex_index', loop_vertices)
triangles = loop_vertices.reshape(-1, 3)
head = (original_positions[:, 2] > 0.84) & (original_positions[:, 1] < -0.035) & (np.abs(original_positions[:, 0]) < 0.06)
head_faces = np.flatnonzero(head[triangles].all(axis=1))
head_triangles = triangles[head_faces]
head_ids, compact = np.unique(head_triangles, return_inverse=True)
tree = BVHTree.FromPolygons(original_positions[head_ids].tolist(), compact.reshape(-1, 3).tolist(), all_triangles=True)
uv_data = mesh.uv_layers.active.data
uv = np.empty(len(mesh.loops) * 2, dtype=np.float32)
uv_data.foreach_get('uv', uv)
uv = uv.reshape(-1, 2)
original_uv = uv.copy()
projected = {}
for index in selected:
    point = unique[index]
    hit, normal, face_index, distance = tree.ray_cast(Vector((point[0], -0.18, point[2])), Vector((0, 1, 0)), 0.20)
    if hit is not None:
        face = int(head_faces[face_index])
        a, b, c = [Vector(original_positions[i]) for i in triangles[face]]
        ta, tb, tc = [Vector((float(u), float(v), 0)) for u, v in original_uv[face * 3:face * 3 + 3]]
        mapped = barycentric_transform(hit, a, b, c, ta, tb, tc)
        projected[int(index)] = (mapped.x, mapped.y)
adjusted_loops = 0
for loop_index in np.flatnonzero(weight[inverse[loop_vertices]] > 0):
    key = int(inverse[loop_vertices[loop_index]])
    if key in projected and np.linalg.norm(original_uv[loop_index] - projected[key]) < 0.05:
        uv[loop_index] = projected[key]
        adjusted_loops += 1
uv_data.foreach_set('uv', uv.ravel())
mesh.update()
bpy.context.view_layer.objects.active = body
body.select_set(True)
if mesh.has_custom_normals:
    bpy.ops.mesh.customdata_custom_splitnormals_clear()
# Suppress hard specular rims on facial skin; keep the original color artwork.
skin = np.clip((positions[:, 2] - 0.838) / 0.015, 0, 1)
skin *= np.clip((0.954 - positions[:, 2]) / 0.012, 0, 1)
skin *= np.clip((0.058 - np.abs(positions[:, 0])) / 0.012, 0, 1)
skin *= np.clip((-positions[:, 1] - 0.035) / 0.015, 0, 1)
attribute = mesh.attributes.new('MiraFaceSkin', 'FLOAT', 'POINT')
attribute.data.foreach_set('value', skin.astype(np.float32))
material = mesh.materials[0]
nodes, links = material.node_tree.nodes, material.node_tree.links
shader = next(n for n in nodes if n.type == 'BSDF_PRINCIPLED')
orm = next(n for n in nodes if n.type == 'TEX_IMAGE' and n.image.name.endswith('_1'))
mask = nodes.new('ShaderNodeAttribute')
mask.attribute_name = attribute.name
channels = nodes.new('ShaderNodeSeparateColor')
links.new(orm.outputs['Color'], channels.inputs['Color'])


def math_node(operation, first, second):
    node = nodes.new('ShaderNodeMath')
    node.operation = operation
    for index, value in enumerate((first, second)):
        if isinstance(value, (int, float)):
            node.inputs[index].default_value = value
        else:
            links.new(value, node.inputs[index])
    return node.outputs[0]


inverse_mask = math_node('SUBTRACT', 1, mask.outputs['Fac'])
floor = math_node('MAXIMUM', channels.outputs['Green'], 0.65)
roughness = math_node('ADD', math_node('MULTIPLY', floor, mask.outputs['Fac']),
                      math_node('MULTIPLY', channels.outputs['Green'], inverse_mask))
metallic = math_node('MULTIPLY', channels.outputs['Blue'], inverse_mask)
links.new(roughness, shader.inputs['Roughness'])
links.new(metallic, shader.inputs['Metallic'])
normal_map = next(n for n in nodes if n.type == 'NORMAL_MAP')
links.new(math_node('SUBTRACT', 1, math_node('MULTIPLY', mask.outputs['Fac'], 0.8)), normal_map.inputs['Strength'])
corrected_orm = nodes.new('ShaderNodeCombineColor')
corrected_orm.name = 'SMILE.CorrectedORM'
links.new(channels.outputs['Red'], corrected_orm.inputs['Red'])
links.new(roughness, corrected_orm.inputs['Green'])
links.new(metallic, corrected_orm.inputs['Blue'])
movement = np.linalg.norm(unique - original, axis=1)
report = {'area': 'nose and eye surfaces only', 'unique_vertices_affected': int(np.count_nonzero(movement > 1e-7)),
          'maximum_movement_source_m': float(movement.max()),
          'reprojected_uv_loops': adjusted_loops,
          'facial_skin_roughness_floor': 0.65, 'facial_normal_strength': 0.2,
          'iterations': 70, 'source_preserved': 'Source/mira-tripo-hd-original.glb',
          'state': 'Candidate correction; close-up review required before final bake.'}
(STAGE / 'face-repair.json').write_text(json.dumps(report, indent=2) + '\n')
bpy.ops.wm.save_as_mainfile(filepath=str(STAGE / 'mira-tripo-hd-face-repaired.blend'), compress=True)
print('FACE_REPAIR_READY ' + json.dumps(report), flush=True)
