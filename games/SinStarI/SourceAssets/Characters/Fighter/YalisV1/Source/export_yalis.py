"""Validate and export the canonical Yalis v1 Character Viewer checkpoint."""

import hashlib
import json
import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Quaternion, Vector


PACKAGE = Path(__file__).resolve().parent.parent
SOURCE = PACKAGE / "Source"
BLENDER = PACKAGE / "Blender"
MODEL = PACKAGE / "yalis-v1-animation-checkpoint.glb"
CHECKPOINT = BLENDER / "yalis-v1-rigged-animation-checkpoint.blend"

EXPECTED_ACTIONS = {
    "Idle",
    "Walk",
    "Run",
    "Attack",
    "Attack2",
    "Dodge",
    "Defend",
    "Hit",
    "Death",
    "Victory",
}


def action_at(rig, action, frame):
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]
    bpy.context.scene.frame_set(frame)
    bpy.context.view_layer.update()


def bounds(body, sword):
    graph = bpy.context.evaluated_depsgraph_get()
    body_evaluated = body.evaluated_get(graph)
    sword_evaluated = sword.evaluated_get(graph)
    body_mesh = body_evaluated.to_mesh()
    sword_mesh = sword_evaluated.to_mesh()
    body_points = [body.matrix_world @ vertex.co for vertex in body_mesh.vertices]
    sword_points = [sword.matrix_world @ vertex.co for vertex in sword_mesh.vertices]
    result = {
        "bodyMinimumMeters": min(point.z for point in body_points),
        "bodyMaximumMeters": max(point.z for point in body_points),
        "swordMinimumMeters": min(point.z for point in sword_points),
        "swordMaximumMeters": max(point.z for point in sword_points),
    }
    body_evaluated.to_mesh_clear()
    sword_evaluated.to_mesh_clear()
    return result


def prepare_topology(obj):
    corrected = obj.data.validate(verbose=True, clean_customdata=False)
    if not obj.data.uv_layers:
        raise RuntimeError(f"{obj.name} has no texture coordinates")
    active_uv = obj.data.uv_layers.active
    active_uv.name = "UVMap"
    active_uv.active_render = True
    for uv_layer in list(obj.data.uv_layers):
        if uv_layer != active_uv:
            obj.data.uv_layers.remove(uv_layer)

    uv_data = active_uv.data
    repaired_uv_faces = 0
    for polygon in obj.data.polygons:
        loops = list(polygon.loop_indices)
        if len(loops) != 3:
            continue
        first, second, third = [uv_data[index].uv.copy() for index in loops]
        area = abs(
            (second - first).x * (third - first).y
            - (second - first).y * (third - first).x
        )
        if area >= 1e-13:
            continue
        center = (first + second + third) / 3.0
        for loop_index, offset in zip(
            loops,
            ((-0.00012, -0.00012), (0.00012, -0.00012), (0.0, 0.00012)),
        ):
            uv_data[loop_index].uv = center + Vector(offset)
        repaired_uv_faces += 1

    audit = bmesh.new()
    audit.from_mesh(obj.data)
    triangle_count = sum(len(face.verts) - 2 for face in audit.faces)
    row = {
        "part": obj.name,
        "vertices": len(obj.data.vertices),
        "triangles": triangle_count,
        "correctedMeshRecords": bool(corrected),
        "boundaryEdges": sum(edge.is_boundary for edge in audit.edges),
        "nonManifoldEdges": sum(not edge.is_manifold for edge in audit.edges),
        "degenerateTriangles": sum(face.calc_area() < 1e-12 for face in audit.faces),
        "repairedCollapsedUvFaces": repaired_uv_faces,
    }
    audit.free()
    if row["degenerateTriangles"]:
        raise RuntimeError(f"Degenerate export triangles: {row}")

    obj.data.calc_tangents(uvmap="UVMap")
    row["invalidTangents"] = sum(
        loop.tangent.length < 0.5 for loop in obj.data.loops
    )
    repaired_corner_normals = 0
    if row["invalidTangents"]:
        invalid_loops = {
            loop.index
            for loop in obj.data.loops
            if loop.tangent.length < 0.5
        }
        corner_normals = [loop.normal.copy() for loop in obj.data.loops]
        for polygon in obj.data.polygons:
            for loop_index in polygon.loop_indices:
                if loop_index in invalid_loops:
                    corner_normals[loop_index] = polygon.normal.copy()
                    repaired_corner_normals += 1
        obj.data.normals_split_custom_set(corner_normals)
        obj.data.free_tangents()
        obj.data.update()
        obj.data.calc_tangents(uvmap="UVMap")
        row["invalidTangents"] = sum(
            loop.tangent.length < 0.5 for loop in obj.data.loops
        )
    moved_vertices = {}
    if row["invalidTangents"]:
        invalid_loops = {
            loop.index
            for loop in obj.data.loops
            if loop.tangent.length < 0.5
        }
        for polygon in obj.data.polygons:
            if not any(index in invalid_loops for index in polygon.loop_indices):
                continue
            vertex_indices = list(polygon.vertices)
            points = [obj.data.vertices[index].co.copy() for index in vertex_indices]
            edge = max(
                ((0, 1, 2), (1, 2, 0), (2, 0, 1)),
                key=lambda item: (points[item[1]] - points[item[0]]).length,
            )
            start, end, opposite = [points[index] for index in edge]
            axis = (end - start).normalized()
            foot = start + axis * (opposite - start).dot(axis)
            altitude = opposite - foot
            if altitude.length < 1e-12:
                raise RuntimeError(
                    f"Collapsed source triangle requires topology repair: {polygon.index}"
                )
            target = foot + altitude.normalized() * (1.08e-6 / (end - start).length)
            vertex_index = vertex_indices[edge[2]]
            moved_vertices.setdefault(
                vertex_index, obj.data.vertices[vertex_index].co.copy()
            )
            obj.data.vertices[vertex_index].co = target
        obj.data.free_tangents()
        obj.data.update()
        obj.data.calc_tangents(uvmap="UVMap")
        row["invalidTangents"] = sum(
            loop.tangent.length < 0.5 for loop in obj.data.loops
        )
    if row["invalidTangents"]:
        invalid_loops = {
            loop.index
            for loop in obj.data.loops
            if loop.tangent.length < 0.5
        }
        corner_normals = [loop.normal.copy() for loop in obj.data.loops]
        for polygon in obj.data.polygons:
            for loop_index in polygon.loop_indices:
                if loop_index in invalid_loops:
                    corner_normals[loop_index] = polygon.normal.copy()
                    repaired_corner_normals += 1
        obj.data.normals_split_custom_set(corner_normals)
        obj.data.free_tangents()
        obj.data.update()
        obj.data.calc_tangents(uvmap="UVMap")
        row["invalidTangents"] = sum(
            loop.tangent.length < 0.5 for loop in obj.data.loops
        )
    row["repairedImportedCornerNormals"] = repaired_corner_normals
    row["repairedMicroTriangleVertices"] = len(moved_vertices)
    row["maximumVertexRepairMeters"] = max(
        (
            (obj.data.vertices[index].co - original).length
            for index, original in moved_vertices.items()
        ),
        default=0.0,
    )
    if row["invalidTangents"]:
        invalid_loops = {
            loop.index
            for loop in obj.data.loops
            if loop.tangent.length < 0.5
        }
        row["invalidTangentDetails"] = [
            {
                "polygon": polygon.index,
                "area": polygon.area,
                "vertices": list(polygon.vertices),
                "coordinates": [
                    list(obj.data.vertices[index].co)
                    for index in polygon.vertices
                ],
                "uv": [list(uv_data[index].uv) for index in polygon.loop_indices],
                "loopNormalLengths": [
                    obj.data.loops[index].normal.length
                    for index in polygon.loop_indices
                ],
            }
            for polygon in obj.data.polygons
            if any(index in invalid_loops for index in polygon.loop_indices)
        ]
        raise RuntimeError(f"Invalid export tangents: {row}")
    return row


def main():
    bpy.ops.wm.open_mainfile(filepath=str(BLENDER / "yalis-v1-grounded.blend"))
    scene = bpy.context.scene
    rig = bpy.data.objects["Yalis.Rig"]
    body = bpy.data.objects["Yalis.Body"]
    sword = bpy.data.objects["Yalis.Sword"]

    proxy = bpy.data.objects.get("Yalis.RigProxy.Donor")
    if proxy:
        bpy.data.objects.remove(proxy, do_unlink=True)

    action_names = {action.name for action in bpy.data.actions}
    if action_names != EXPECTED_ACTIONS:
        raise RuntimeError(
            f"Unexpected Yalis actions: {sorted(action_names)}"
        )
    if any(bone.constraints for bone in rig.pose.bones):
        raise RuntimeError("Temporary pose constraints remain on Yalis's rig")

    topology = [prepare_topology(body), prepare_topology(sword)]
    total_triangles = sum(row["triangles"] for row in topology)
    if total_triangles > 90000:
        raise RuntimeError(f"Yalis exceeds the 90k triangle checkpoint: {topology}")

    maximum_body_influences = max(
        len([item for item in vertex.groups if item.weight > 0.000001])
        for vertex in body.data.vertices
    )
    unweighted_body_vertices = sum(not vertex.groups for vertex in body.data.vertices)
    if maximum_body_influences > 4 or unweighted_body_vertices:
        raise RuntimeError(
            "Yalis body skinning is outside the runtime contract: "
            f"{maximum_body_influences} influences, "
            f"{unweighted_body_vertices} unweighted vertices"
        )

    rig.data.pose_position = "REST"
    scene.frame_set(1)
    bpy.context.view_layer.update()
    bind_bounds = bounds(body, sword)
    rig.data.pose_position = "POSE"

    contacts = []
    for action in sorted(bpy.data.actions, key=lambda item: item.name):
        start, end = map(int, action.frame_range)
        samples = []
        hips_positions = []
        for frame in range(start, end + 1):
            action_at(rig, action, frame)
            sample = {"frame": frame - start, **bounds(body, sword)}
            if sample["bodyMinimumMeters"] < -0.0001:
                raise RuntimeError((action.name, "body below floor", sample))
            if sample["swordMinimumMeters"] < -0.0001:
                raise RuntimeError((action.name, "sword below floor", sample))
            samples.append(sample)
            hips_positions.append(
                rig.pose.bones["mixamorig:Hips"].matrix.translation.copy()
            )

        root_x_span = max(point.x for point in hips_positions) - min(
            point.x for point in hips_positions
        )
        root_y_span = max(point.y for point in hips_positions) - min(
            point.y for point in hips_positions
        )
        if root_x_span > 0.0001 or root_y_span > 0.0001:
            raise RuntimeError(
                f"{action.name} retained root motion: {root_x_span}, {root_y_span}"
            )
        contacts.append(
            {
                "clip": action.name,
                "framesChecked": len(samples),
                "minimumBodyMeters": min(
                    sample["bodyMinimumMeters"] for sample in samples
                ),
                "minimumSwordMeters": min(
                    sample["swordMinimumMeters"] for sample in samples
                ),
                "rootXSpanMeters": root_x_span,
                "rootYSpanMeters": root_y_span,
                "samples": [
                    samples[0],
                    samples[(len(samples) - 1) // 2],
                    samples[-1],
                ],
            }
        )
        print("YALIS_EXPORT_CLIP_VALID " + json.dumps(contacts[-1]), flush=True)

    idle = next(row for row in contacts if row["clip"] == "Idle")
    bind_to_idle_floor_delta = abs(
        bind_bounds["bodyMinimumMeters"]
        - idle["samples"][0]["bodyMinimumMeters"]
    )
    if bind_to_idle_floor_delta > 0.002:
        raise RuntimeError(
            f"Bind/Idle frame-zero grounding mismatch: {bind_to_idle_floor_delta}"
        )

    action_at(rig, bpy.data.actions["Idle"], 1)
    scene.frame_start = 1
    scene.frame_end = int(bpy.data.actions["Idle"].frame_range[1])
    scene.name = "Yalis — Nimble Sword Fighter"
    for action in bpy.data.actions:
        action.use_fake_user = True

    bpy.ops.object.select_all(action="DESELECT")
    for obj in (rig, body, sword):
        obj.select_set(True)
    bpy.context.view_layer.objects.active = rig

    for window in bpy.context.window_manager.windows:
        for area in window.screen.areas:
            if area.type != "VIEW_3D":
                continue
            space = area.spaces.active
            space.shading.type = "MATERIAL"
            space.overlay.show_overlays = False
            space.region_3d.view_rotation = Quaternion((1, 0, 0), math.pi / 2)
            space.region_3d.view_location = Vector((0.0, 0.0, 0.55))
            space.region_3d.view_distance = 1.55
            space.region_3d.view_perspective = "ORTHO"

    bpy.data.orphans_purge(
        do_local_ids=True, do_linked_ids=False, do_recursive=True
    )
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(CHECKPOINT), compress=True)
    bpy.ops.export_scene.gltf(
        filepath=str(MODEL),
        export_format="GLB",
        use_selection=True,
        export_animations=True,
        export_animation_mode="ACTIONS",
        export_skins=True,
        export_influence_nb=4,
        export_all_influences=False,
        export_tangents=True,
        export_yup=True,
    )

    report = {
        "modelSha256": hashlib.sha256(MODEL.read_bytes()).hexdigest(),
        "modelBytes": MODEL.stat().st_size,
        "units": "meters; Blender Z is exported as glTF/SM3D Y",
        "bindBounds": bind_bounds,
        "bindToIdleFrameZeroFloorDeltaMeters": bind_to_idle_floor_delta,
        "clips": contacts,
        "topology": topology,
        "triangles": total_triangles,
        "rigBones": len(rig.data.bones),
        "mixamoBones": 25,
        "authoredBones": [
            "YalisSword",
            "YalisHairRoot",
            "YalisHairMid",
            "YalisHairTip",
        ],
        "bodyTextureSize": [4096, 4096],
        "swordTextureSize": [2048, 2048],
        "bodyMaximumInfluences": maximum_body_influences,
        "unweightedBodyVertices": unweighted_body_vertices,
        "acceptance": (
            "Canonical native Character Viewer candidate; runtime import and "
            "brief animation playback remain required."
        ),
    }
    (SOURCE / "export-validation.json").write_text(
        json.dumps(report, indent=2), encoding="utf-8"
    )
    print("YALIS_EXPORT_READY " + json.dumps(report), flush=True)


if __name__ == "__main__":
    main()
