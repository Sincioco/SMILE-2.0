"""Inspect and prepare immutable Tripo sources using the installed Blender.

Run Blender --background --python this_file -- inspect|reduce.
The package owns all artist-derived geometry; the runtime remains an asset consumer.
"""

import bpy
import bmesh
import hashlib
import json
import math
import sys
from pathlib import Path
from mathutils import Vector

PACKAGE = Path(__file__).resolve().parent.parent
SOURCE = PACKAGE / "Source"
BLENDER = PACKAGE / "Blender"
PREVIEWS = PACKAGE / "Previews"


def select(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.hide_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def bounds(obj):
    points = [obj.matrix_world @ vertex.co for vertex in obj.data.vertices]
    return {"min": [min(v[a] for v in points) for a in range(3)],
            "max": [max(v[a] for v in points) for a in range(3)]}


def import_model(kind):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    path = SOURCE / f"kael-v1-{kind}.original.glb"
    bpy.ops.import_scene.gltf(filepath=str(path))
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    if len(meshes) != 1:
        raise RuntimeError(f"Expected one source mesh, got {len(meshes)}")
    obj = meshes[0]
    obj.name = "Kael." + kind.title()
    select(obj)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    obj.data.calc_loop_triangles()
    return obj, {"sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
                 "vertices": len(obj.data.vertices),
                 "triangles": len(obj.data.loop_triangles), "bounds": bounds(obj),
                 "images": [{"name": i.name, "size": list(i.size)}
                            for i in bpy.data.images if i.type == "IMAGE"]}


def preview(obj, label, equipment=()):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 12
    scene.render.resolution_x = 600
    scene.render.resolution_y = 800
    scene.render.resolution_percentage = 100
    scene.view_settings.view_transform = "AgX"
    scene.world = bpy.data.worlds.new("Kael.Preview.World")
    scene.world.use_nodes = True
    scene.world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.25, 0.3, 0.4, 1)
    scene.world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.5
    points = []
    for subject in (obj, *equipment):
        evaluated = subject.evaluated_get(bpy.context.evaluated_depsgraph_get())
        mesh = evaluated.to_mesh()
        points.extend(subject.matrix_world @ vertex.co for vertex in mesh.vertices)
        evaluated.to_mesh_clear()
    extent = {"min": [min(p[i] for p in points) for i in range(3)],
              "max": [max(p[i] for p in points) for i in range(3)]}
    center = Vector([(a + b) * 0.5 for a, b in zip(extent["min"], extent["max"])])
    height = max(extent["max"][2] - extent["min"][2],
                 (extent["max"][0] - extent["min"][0]) / 0.75,
                 (extent["max"][1] - extent["min"][1]) / 0.75)
    helpers = []
    for location, power, size in [((2, -3, 3), 350, 3), ((-3, -1, 1), 200, 3), ((0, 2, 2), 250, 2)]:
        bpy.ops.object.light_add(type="AREA", location=tuple(center + Vector(location) * height))
        light = bpy.context.object
        light.data.energy = power * height * height
        light.data.shape = "DISK"
        light.data.size = size * height
        light.rotation_euler = (center - light.location).to_track_quat('-Z', 'Y').to_euler()
        helpers.append(light)
    bpy.ops.object.camera_add()
    camera = bpy.context.object
    helpers.append(camera)
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = height * 1.13
    scene.camera = camera
    for name, direction in [("front", (0, -3, 0)), ("right", (3, 0, 0)), ("back", (0, 3, 0))]:
        camera.location = center + Vector(direction) * height
        camera.rotation_euler = (center - camera.location).to_track_quat('-Z', 'Y').to_euler()
        scene.render.filepath = str(PREVIEWS / f"{label}-{name}.png")
        bpy.ops.render.render(write_still=True)
    for helper in helpers:
        bpy.data.objects.remove(helper, do_unlink=True)


def reduce(obj, target):
    select(obj)
    # GLB accessor vertices split at UV/normal seams. Collapse must see a coherent
    # surface; otherwise the seam islands turn into disconnected shards.
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=0.000001)
    bm.to_mesh(obj.data)
    bm.free()
    bpy.ops.mesh.customdata_custom_splitnormals_clear()
    obj.data.calc_loop_triangles()
    modifier = obj.modifiers.new("Runtime triangle budget", "DECIMATE")
    modifier.ratio = min(1, target / len(obj.data.loop_triangles))
    modifier.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.dissolve_degenerate(bm, dist=1e-8, edges=list(bm.edges))
    bad = [f for f in bm.faces if f.calc_area() < 1e-14]
    if bad:
        bmesh.ops.delete(bm, geom=bad, context="FACES")
    bmesh.ops.triangulate(bm, faces=list(bm.faces))
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.validate(clean_customdata=False)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    obj.data.calc_loop_triangles()
    return {"vertices": len(obj.data.vertices), "triangles": len(obj.data.loop_triangles),
            "bounds": bounds(obj)}


def main():
    mode = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else "inspect"
    report = {}
    kinds = ("body",) if mode == "reduce-body" else ("body", "sword")
    for kind in kinds:
        obj, original = import_model(kind)
        print("KAEL_SOURCE " + kind + " " + json.dumps(original), flush=True)
        report[kind] = {"original": original}
        if mode.startswith("reduce"):
            report[kind]["reduced"] = reduce(obj, 42000 if kind == "body" else 7000)
            if kind == "sword":
                for image in bpy.data.images:
                    if image.type == "IMAGE" and image.size[0] > 2048:
                        image.scale(2048, 2048)
                        image.pack()
            bpy.ops.file.pack_all()
            bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER / f"kael-v1-{kind}-reduced.blend"), compress=True)
            if kind == "body":
                select(obj)
                bpy.ops.export_scene.fbx(filepath=str(SOURCE / "kael-v1-mixamo-input.fbx"),
                    use_selection=True, add_leaf_bones=False, apply_unit_scale=True,
                    bake_anim=False, path_mode="AUTO", mesh_smooth_type="FACE")
            preview(obj, f"reduced-{kind}")
        else:
            preview(obj, f"original-{kind}")
    (SOURCE / f"{mode}-report.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
