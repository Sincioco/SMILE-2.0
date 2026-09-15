"""Preserve, reduce, and prepare Yalis for Mixamo and SMILE 2.0.

Run with Blender 5.2 using ``--background --python``. The source GLBs remain
immutable. The reduced body keeps its original 4K PBR atlas; the separate sword
uses a 2K derivative of its original 4K PBR atlas.
"""

import bpy
import bmesh
import hashlib
import json
import math
from pathlib import Path

from mathutils import Vector


PACKAGE = Path(__file__).resolve().parent.parent
SOURCE = PACKAGE / "Source"
BLENDER = PACKAGE / "Blender"
PREVIEWS = PACKAGE / "Previews"
BODY_SOURCE = SOURCE / "yalis-v1-body.original.glb"
SWORD_SOURCE = SOURCE / "yalis-v1-sword.original.glb"
BODY_TARGET = 45000
SWORD_TARGET = 6000


def file_hash(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def triangle_count(mesh):
    mesh.calc_loop_triangles()
    return len(mesh.loop_triangles)


def bounds(objects):
    points = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    return {
        "minimum": [min(point[axis] for point in points) for axis in range(3)],
        "maximum": [max(point[axis] for point in points) for axis in range(3)],
        "dimensions": [
            max(point[axis] for point in points) - min(point[axis] for point in points)
            for axis in range(3)
        ],
    }


def topology(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    result = {
        "vertices": len(obj.data.vertices),
        "triangles": triangle_count(obj.data),
        "boundaryEdges": sum(edge.is_boundary for edge in bm.edges),
        "nonManifoldEdges": sum(not edge.is_manifold for edge in bm.edges),
        "degenerateFaces": sum(face.calc_area() < 1e-14 for face in bm.faces),
        "uvLayers": [layer.name for layer in obj.data.uv_layers],
    }
    bm.free()
    return result


def detach_and_apply(objects):
    for obj in objects:
        world = obj.matrix_world.copy()
        obj.parent = None
        obj.matrix_world = world
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)


def join_meshes(objects, name):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    joined = bpy.context.object
    joined.name = name
    joined.data.name = name + ".Mesh"
    while len(joined.data.materials) > 1:
        joined.data.materials.pop(index=len(joined.data.materials) - 1)
    return joined


def reduce_mesh(obj, target, label):
    before = topology(obj)
    modifier = obj.modifiers.new(f"{label} runtime reduction", "DECIMATE")
    modifier.decimate_type = "COLLAPSE"
    modifier.ratio = min(1.0, target / before["triangles"])
    modifier.use_collapse_triangulate = True
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.dissolve_degenerate(bm, dist=1e-8, edges=list(bm.edges))
    collapsed = [face for face in bm.faces if face.calc_area() < 1e-14]
    if collapsed:
        bmesh.ops.delete(bm, geom=collapsed, context="FACES")
    bmesh.ops.triangulate(bm, faces=list(bm.faces))
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.validate(verbose=True, clean_customdata=False)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    after = topology(obj)
    if after["triangles"] > target + 128:
        raise RuntimeError(f"{label} exceeded its triangle target: {after}")
    return {"before": before, "after": after, "target": target}


def reduce_sword(source, target):
    sword = source.copy()
    sword.data = source.data.copy()
    bpy.context.scene.collection.objects.link(sword)
    sword.name = "Yalis.Sword"
    detach_and_apply([sword])
    bpy.ops.object.select_all(action="DESELECT")
    sword.select_set(True)
    bpy.context.view_layer.objects.active = sword
    before = topology(sword)

    remesh = sword.modifiers.new("Closed sword authoring surface", "REMESH")
    remesh.mode = "VOXEL"
    remesh.voxel_size = 0.0015
    remesh.use_smooth_shade = True
    bpy.ops.object.modifier_apply(modifier=remesh.name)

    modifier = sword.modifiers.new("Yalis sword runtime reduction", "DECIMATE")
    modifier.decimate_type = "COLLAPSE"
    modifier.ratio = min(1.0, target / triangle_count(sword.data))
    modifier.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    bm = bmesh.new()
    bm.from_mesh(sword.data)
    bmesh.ops.dissolve_degenerate(bm, dist=1e-8, edges=list(bm.edges))
    collapsed = [face for face in bm.faces if face.calc_area() < 1e-14]
    if collapsed:
        bmesh.ops.delete(bm, geom=collapsed, context="FACES")
    bmesh.ops.triangulate(bm, faces=list(bm.faces))
    bm.to_mesh(sword.data)
    bm.free()
    sword.data.validate(verbose=True, clean_customdata=False)
    for polygon in sword.data.polygons:
        polygon.use_smooth = True

    after = topology(sword)
    if after["triangles"] > target + 128:
        raise RuntimeError(f"Yalis sword exceeded its triangle target: {after}")
    report = {
        "before": before,
        "after": after,
        "target": target,
        "voxelSizeMeters": 0.0015,
    }
    return sword, report


def bake_sword(source, sword):
    for layer in list(sword.data.uv_layers):
        sword.data.uv_layers.remove(layer)
    sword.data.uv_layers.new(name="UVMap")
    bpy.ops.object.select_all(action="DESELECT")
    sword.select_set(True)
    bpy.context.view_layer.objects.active = sword
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(
        angle_limit=math.radians(66),
        island_margin=0.004,
        area_weight=0.3,
        correct_aspect=True,
    )
    bpy.ops.object.mode_set(mode="OBJECT")

    material = bpy.data.materials.new("Yalis.Sword.2K.PBR")
    material.use_nodes = True
    sword.data.materials.clear()
    sword.data.materials.append(material)
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    shader = next(node for node in nodes if node.type == "BSDF_PRINCIPLED")
    target = nodes.new("ShaderNodeTexImage")
    nodes.active = target

    source_material = source.data.materials[0]
    source_nodes = source_material.node_tree.nodes
    source_links = source_material.node_tree.links
    output = next(node for node in source_nodes if node.type == "OUTPUT_MATERIAL")
    source_shader = next(node for node in source_nodes if node.type == "BSDF_PRINCIPLED")
    images = {image.name: image for image in bpy.data.images}
    base_source = images["Yalis.Sword.BaseColor"]
    normal_source = images["Yalis.Sword.Normal"]
    orm_source = images["Yalis.Sword.MetallicRoughness"]
    emission = source_nodes.new("ShaderNodeEmission")
    source_texture = source_nodes.new("ShaderNodeTexImage")

    bpy.ops.object.select_all(action="DESELECT")
    source.hide_set(False)
    source.hide_render = False
    source.select_set(True)
    sword.select_set(True)
    bpy.context.view_layer.objects.active = sword
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 16
    try:
        preferences = bpy.context.preferences.addons["cycles"].preferences
        preferences.compute_device_type = "OPTIX"
        preferences.get_devices()
        enabled = False
        for device in preferences.devices:
            device.use = device.type == "OPTIX"
            enabled = enabled or device.use
        if enabled:
            scene.cycles.device = "GPU"
    except (KeyError, TypeError):
        scene.cycles.device = "CPU"
    scene.render.bake.use_selected_to_active = True
    scene.render.bake.use_clear = True
    scene.render.bake.cage_extrusion = 0.004
    scene.render.bake.max_ray_distance = 0.018
    scene.render.bake.margin = 8
    scene.render.bake.margin_type = "EXTEND"

    def bake(name, kind, source_image, color_space):
        image = bpy.data.images.new(name, width=2048, height=2048, alpha=False)
        image.colorspace_settings.name = color_space
        target.image = image
        nodes.active = target
        source_texture.image = source_image
        source_links.new(source_texture.outputs["Color"], emission.inputs["Color"])
        source_links.new(emission.outputs[0], output.inputs["Surface"])
        print("YALIS_SWORD_BAKING " + name, flush=True)
        bpy.ops.object.bake(type=kind)
        image.filepath_raw = str(SOURCE / (name + ".png"))
        image.file_format = "PNG"
        image.save()
        image.pack()
        return image

    base = bake("yalis-v1-sword-basecolor-2k", "EMIT", base_source, "sRGB")
    orm = bake("yalis-v1-sword-metallic-roughness-2k", "EMIT", orm_source, "Non-Color")
    source_links.new(source_shader.outputs[0], output.inputs["Surface"])
    normal = bpy.data.images.new(
        "yalis-v1-sword-normal-2k", width=2048, height=2048, alpha=False
    )
    normal.colorspace_settings.name = "Non-Color"
    target.image = normal
    nodes.active = target
    print("YALIS_SWORD_BAKING yalis-v1-sword-normal-2k", flush=True)
    bpy.ops.object.bake(type="NORMAL")
    normal.filepath_raw = str(SOURCE / "yalis-v1-sword-normal-2k.png")
    normal.file_format = "PNG"
    normal.save()
    normal.pack()

    source_nodes.remove(source_texture)
    source_nodes.remove(emission)
    nodes.remove(target)
    base_node = nodes.new("ShaderNodeTexImage")
    base_node.image = base
    links.new(base_node.outputs["Color"], shader.inputs["Base Color"])
    normal_node = nodes.new("ShaderNodeTexImage")
    normal_node.image = normal
    normal_map = nodes.new("ShaderNodeNormalMap")
    links.new(normal_node.outputs["Color"], normal_map.inputs["Color"])
    links.new(normal_map.outputs["Normal"], shader.inputs["Normal"])
    orm_node = nodes.new("ShaderNodeTexImage")
    orm_node.image = orm
    channels = nodes.new("ShaderNodeSeparateColor")
    links.new(orm_node.outputs["Color"], channels.inputs["Color"])
    links.new(channels.outputs["Green"], shader.inputs["Roughness"])
    links.new(channels.outputs["Blue"], shader.inputs["Metallic"])
    occlusion_group = bpy.data.node_groups.new("glTF Material Output", "ShaderNodeTree")
    occlusion_group.interface.new_socket(
        name="Occlusion", in_out="INPUT", socket_type="NodeSocketFloat"
    )
    occlusion = nodes.new("ShaderNodeGroup")
    occlusion.node_tree = occlusion_group
    links.new(channels.outputs["Red"], occlusion.inputs["Occlusion"])
    scene.render.bake.use_selected_to_active = False
    return [base, normal, orm]


def rename_images(images, prefix):
    suffixes = ("BaseColor", "Normal", "MetallicRoughness")
    ordered = sorted(images, key=lambda image: image.name)
    for image, suffix in zip(ordered, suffixes):
        image.name = f"Yalis.{prefix}.{suffix}"


BLENDER.mkdir(parents=True, exist_ok=True)
PREVIEWS.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.scene.render.fps = 30

before_objects = set(bpy.data.objects)
before_images = set(bpy.data.images)
bpy.ops.import_scene.gltf(filepath=str(BODY_SOURCE))
body_objects = [obj for obj in set(bpy.data.objects) - before_objects if obj.type == "MESH"]
body_images = list(set(bpy.data.images) - before_images)
for index, obj in enumerate(sorted(body_objects, key=lambda value: value.name)):
    obj.name = f"Yalis.Body.Source.{index:02d}"
rename_images(body_images, "Body")

before_objects = set(bpy.data.objects)
before_images = set(bpy.data.images)
bpy.ops.import_scene.gltf(filepath=str(SWORD_SOURCE))
sword_source = next(obj for obj in set(bpy.data.objects) - before_objects if obj.type == "MESH")
sword_images = list(set(bpy.data.images) - before_images)
sword_source.name = "Yalis.Sword.Source"
rename_images(sword_images, "Sword")

source_report = {
    "body": {
        "file": BODY_SOURCE.name,
        "sha256": file_hash(BODY_SOURCE),
        "meshes": len(body_objects),
        "triangles": sum(triangle_count(obj.data) for obj in body_objects),
        "bounds": bounds(body_objects),
        "textures": [{"name": image.name, "size": list(image.size)} for image in body_images],
    },
    "sword": {
        "file": SWORD_SOURCE.name,
        "sha256": file_hash(SWORD_SOURCE),
        "triangles": triangle_count(sword_source.data),
        "bounds": bounds([sword_source]),
        "textures": [{"name": image.name, "size": list(image.size)} for image in sword_images],
    },
    "skins": 0,
    "animations": 0,
}
(SOURCE / "source-inspection.json").write_text(
    json.dumps(source_report, indent=2) + "\n", encoding="utf-8"
)

bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER / "yalis-v1-original-import.blend"), compress=True)
print("YALIS_ORIGINAL_IMPORT_SAVED", flush=True)

detach_and_apply(body_objects)
body = join_meshes(body_objects, "Yalis.Body")
bpy.ops.object.select_all(action="DESELECT")
body.select_set(True)
bpy.context.view_layer.objects.active = body
body_report = reduce_mesh(body, BODY_TARGET, "Yalis body")

sword, sword_report = reduce_sword(sword_source, SWORD_TARGET)
sword_baked_images = bake_sword(sword_source, sword)

body_bounds = bounds([body])
sword_bounds = bounds([sword])
reduction_report = {
    "body": {**body_report, "bounds": body_bounds, "textureSize": [4096, 4096]},
    "sword": {**sword_report, "bounds": sword_bounds, "textureSize": [2048, 2048]},
    "totalTriangles": body_report["after"]["triangles"] + sword_report["after"]["triangles"],
    "policy": "Preserve layered body surfaces and original UVs; retain 4K body PBR; use 2K sword PBR.",
}
(SOURCE / "reduction-report.json").write_text(
    json.dumps(reduction_report, indent=2) + "\n", encoding="utf-8"
)

sword.hide_render = True
sword.hide_set(True)
bpy.ops.object.select_all(action="DESELECT")
body.select_set(True)
bpy.context.view_layer.objects.active = body
bpy.ops.export_scene.fbx(
    filepath=str(SOURCE / "yalis-v1-mixamo-input.fbx"),
    use_selection=True,
    add_leaf_bones=False,
    apply_unit_scale=True,
    bake_space_transform=False,
    path_mode="AUTO",
    bake_anim=False,
    mesh_smooth_type="FACE",
)

sword.hide_set(False)
sword.hide_render = False
bpy.data.objects.remove(sword_source, do_unlink=True)
bpy.data.orphans_purge(do_local_ids=True, do_linked_ids=False, do_recursive=True)
bpy.ops.object.select_all(action="DESELECT")
for obj in (body, sword):
    obj.select_set(True)
bpy.context.view_layer.objects.active = body
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER / "yalis-v1-unrigged-reduced.blend"), compress=True)
print("YALIS_REDUCTION_READY " + json.dumps(reduction_report), flush=True)
