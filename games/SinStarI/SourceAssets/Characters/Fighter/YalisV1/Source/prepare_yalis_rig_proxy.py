"""Build a simple, single-surface Mixamo rigging proxy for Yalis.

Mixamo rejected the visually correct reduced body because the generated source
contains many disconnected armor, cape, and hair shells. This mannequin keeps
Yalis's measured joint locations, proportions, scale, and T-pose while omitting
all decorative geometry. The proxy is only a skeleton donor; it is never shipped.
"""

import bpy
import bmesh
import json
import math
from pathlib import Path

from mathutils import Vector


PACKAGE = Path(__file__).resolve().parent.parent
SOURCE = PACKAGE / "Source"
BLENDER = PACKAGE / "Blender"
INPUT_BLEND = BLENDER / "yalis-v1-unrigged-reduced.blend"
OUTPUT_BLEND = BLENDER / "yalis-v1-mixamo-rig-proxy.blend"
OUTPUT_FBX = SOURCE / "yalis-v1-mixamo-rig-proxy.fbx"
OUTPUT_REPORT = SOURCE / "rig-proxy-report.json"
VOXEL_SIZE = 0.0045
TRIANGLE_TARGET = 20000


def triangle_count(mesh):
    mesh.calc_loop_triangles()
    return len(mesh.loop_triangles)


def keep_largest_component(obj):
    mesh = obj.data
    bm = bmesh.new()
    bm.from_mesh(mesh)
    unseen = set(bm.verts)
    components = []

    while unseen:
        seed = unseen.pop()
        component = {seed}
        pending = [seed]

        while pending:
            vertex = pending.pop()

            for edge in vertex.link_edges:
                neighbor = edge.other_vert(vertex)

                if neighbor in unseen:
                    unseen.remove(neighbor)
                    component.add(neighbor)
                    pending.append(neighbor)

        components.append(component)

    components.sort(key=len, reverse=True)
    removed_vertices = sum(len(component) for component in components[1:])

    if len(components) > 1:
        remove = [vertex for component in components[1:] for vertex in component]
        bmesh.ops.delete(bm, geom=remove, context="VERTS")

    bm.to_mesh(mesh)
    bm.free()
    mesh.validate(verbose=True, clean_customdata=False)
    return len(components), removed_vertices


def add_ellipsoid(name, location, scale):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=16, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return obj


def add_segment(name, start, end, radius):
    start = Vector(start)
    end = Vector(end)
    direction = end - start
    midpoint = (start + end) * 0.5
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=24,
        radius=radius,
        depth=direction.length,
        location=midpoint,
    )
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(direction.normalized())
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    return obj


bpy.ops.wm.open_mainfile(filepath=str(INPUT_BLEND))
body = bpy.data.objects.get("Yalis.Body")

if body is None:
    raise RuntimeError("Yalis.Body is missing from the reduced checkpoint")

parts = [
    add_ellipsoid("Proxy.Head", (0, 0, 0.915), (0.072, 0.065, 0.090)),
    add_segment("Proxy.Neck", (0, 0, 0.815), (0, 0, 0.855), 0.040),
    add_ellipsoid("Proxy.Chest", (0, 0, 0.705), (0.118, 0.074, 0.145)),
    add_ellipsoid("Proxy.Pelvis", (0, 0, 0.535), (0.098, 0.070, 0.080)),
]

for side in (-1, 1):
    shoulder = (side * 0.105, 0, 0.785)
    elbow = (side * 0.225, 0, 0.778)
    wrist = (side * 0.325, 0, 0.778)
    hip = (side * 0.055, 0, 0.525)
    knee = (side * 0.055, 0, 0.300)
    ankle = (side * 0.052, 0, 0.065)
    parts.extend(
        [
            add_segment(f"Proxy.UpperArm.{side}", shoulder, elbow, 0.038),
            add_segment(f"Proxy.Forearm.{side}", elbow, wrist, 0.031),
            add_ellipsoid(
                f"Proxy.Hand.{side}",
                (side * 0.350, -0.003, 0.778),
                (0.035, 0.030, 0.026),
            ),
            add_segment(f"Proxy.Thigh.{side}", hip, knee, 0.058),
            add_segment(f"Proxy.Calf.{side}", knee, ankle, 0.044),
            add_ellipsoid(
                f"Proxy.Foot.{side}",
                (side * 0.052, -0.025, 0.040),
                (0.047, 0.085, 0.040),
            ),
        ]
    )

bpy.ops.object.select_all(action="DESELECT")
for part in parts:
    part.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
proxy = bpy.context.object
proxy.name = "Yalis.RigProxy"
proxy.data.name = "Yalis.RigProxy.Mesh"
material = bpy.data.materials.new("Yalis.RigProxy.Material")
material.diffuse_color = (0.72, 0.42, 0.55, 1.0)
proxy.data.materials.append(material)

bpy.ops.object.select_all(action="DESELECT")
proxy.select_set(True)
bpy.context.view_layer.objects.active = proxy
remesh = proxy.modifiers.new("Closed rigging surface", "REMESH")
remesh.mode = "VOXEL"
remesh.voxel_size = VOXEL_SIZE
remesh.use_smooth_shade = True
bpy.ops.object.modifier_apply(modifier=remesh.name)
component_count, removed_vertices = keep_largest_component(proxy)

before_decimate = triangle_count(proxy.data)
decimate = proxy.modifiers.new("Mixamo proxy reduction", "DECIMATE")
decimate.decimate_type = "COLLAPSE"
decimate.ratio = min(1.0, TRIANGLE_TARGET / before_decimate)
decimate.use_collapse_triangulate = True
bpy.ops.object.modifier_apply(modifier=decimate.name)

bm = bmesh.new()
bm.from_mesh(proxy.data)
bmesh.ops.dissolve_degenerate(bm, dist=1e-8, edges=list(bm.edges))
bmesh.ops.triangulate(bm, faces=list(bm.faces))
bm.to_mesh(proxy.data)
bm.free()
proxy.data.validate(verbose=True, clean_customdata=False)

for polygon in proxy.data.polygons:
    polygon.use_smooth = True

body.hide_set(True)
body.hide_render = True
for obj in bpy.context.scene.objects:
    obj.select_set(False)
proxy.hide_set(False)
proxy.hide_render = False
proxy.select_set(True)
bpy.context.view_layer.objects.active = proxy
bpy.ops.export_scene.fbx(
    filepath=str(OUTPUT_FBX),
    use_selection=True,
    add_leaf_bones=False,
    apply_unit_scale=True,
    bake_space_transform=False,
    path_mode="AUTO",
    bake_anim=False,
    mesh_smooth_type="FACE",
)

report = {
    "purpose": "Mixamo-only Yalis-proportioned skeleton donor; never shipped",
    "jointReferenceMeters": {
        "chinZ": 0.845,
        "shoulderZ": 0.785,
        "wristX": 0.325,
        "groinZ": 0.470,
        "kneeZ": 0.300,
    },
    "voxelSizeMeters": VOXEL_SIZE,
    "componentsBeforeCleanup": component_count,
    "removedLooseVertices": removed_vertices,
    "trianglesBeforeDecimate": before_decimate,
    "trianglesAfterDecimate": triangle_count(proxy.data),
    "targetTriangles": TRIANGLE_TARGET,
    "fbx": OUTPUT_FBX.name,
}
OUTPUT_REPORT.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT_BLEND), compress=True)
print("YALIS_MIXAMO_PROXY_READY " + json.dumps(report), flush=True)
