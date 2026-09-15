"""Round-trip the exported Yalis GLB and verify its runtime-facing payload."""

import hashlib
import json
from pathlib import Path

import bpy


PACKAGE = Path(__file__).resolve().parent.parent
MODEL = PACKAGE / "yalis-v1-animation-checkpoint.glb"
REPORT = Path(__file__).resolve().parent / "roundtrip-validation.json"

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
EXPECTED_BONES = {
    "mixamorig:Hips",
    "mixamorig:Head",
    "mixamorig:RightHand",
    "YalisSword",
    "YalisHairRoot",
    "YalisHairMid",
    "YalisHairTip",
}


def triangle_count(obj):
    return sum(len(polygon.vertices) - 2 for polygon in obj.data.polygons)


def set_action(rig, action, frame):
    if rig.animation_data is None:
        rig.animation_data_create()
    rig.animation_data.action = action
    if action.slots:
        rig.animation_data.action_slot = action.slots[0]
    bpy.context.scene.frame_set(frame)
    bpy.context.view_layer.update()


def object_bounds(obj):
    graph = bpy.context.evaluated_depsgraph_get()
    evaluated = obj.evaluated_get(graph)
    mesh = evaluated.to_mesh()
    points = [evaluated.matrix_world @ vertex.co for vertex in mesh.vertices]
    result = {
        "minimum": [min(point[axis] for point in points) for axis in range(3)],
        "maximum": [max(point[axis] for point in points) for axis in range(3)],
    }
    evaluated.to_mesh_clear()
    return result


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(MODEL))

    rigs = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    meshes = [
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH"
        and any(modifier.type == "ARMATURE" for modifier in obj.modifiers)
    ]
    if len(rigs) != 1 or len(meshes) != 2:
        raise RuntimeError(
            "Unexpected round-trip objects: "
            f"{len(rigs)} rigs, "
            f"{[(obj.name, triangle_count(obj)) for obj in meshes]} meshes"
        )

    rig = rigs[0]
    meshes.sort(key=triangle_count, reverse=True)
    body, sword = meshes
    actions = {action.name: action for action in bpy.data.actions}
    if set(actions) != EXPECTED_ACTIONS:
        raise RuntimeError(f"Unexpected round-trip actions: {sorted(actions)}")
    bone_names = {bone.name for bone in rig.data.bones}
    missing_bones = sorted(EXPECTED_BONES - bone_names)
    if missing_bones:
        raise RuntimeError(
            f"Missing round-trip bones: {missing_bones}; "
            f"found {sorted(bone.name for bone in rig.data.bones)}"
        )

    maximum_influences = max(
        len([group for group in vertex.groups if group.weight > 0.000001])
        for vertex in body.data.vertices
    )
    unweighted_vertices = sum(not vertex.groups for vertex in body.data.vertices)
    if maximum_influences > 4 or unweighted_vertices:
        raise RuntimeError(
            f"Invalid round-trip skin: {maximum_influences} influences, "
            f"{unweighted_vertices} unweighted vertices"
        )

    clip_samples = []
    for name in sorted(actions):
        action = actions[name]
        start, end = map(int, action.frame_range)
        frames = sorted({start, (start + end) // 2, end})
        samples = []
        for frame in frames:
            set_action(rig, action, frame)
            samples.append(
                {
                    "frame": frame - start,
                    "body": object_bounds(body),
                    "sword": object_bounds(sword),
                }
            )
        clip_samples.append(
            {
                "clip": name,
                "frameRange": [start, end],
                "samples": samples,
            }
        )

    images = []
    for image in sorted(bpy.data.images, key=lambda item: item.name):
        images.append(
            {
                "name": image.name,
                "size": [int(image.size[0]), int(image.size[1])],
                "packed": image.packed_file is not None,
            }
        )

    report = {
        "modelSha256": hashlib.sha256(MODEL.read_bytes()).hexdigest(),
        "armature": rig.name,
        "bones": len(rig.data.bones),
        "requiredBonesPresent": sorted(EXPECTED_BONES),
        "meshes": [
            {
                "name": obj.name,
                "vertices": len(obj.data.vertices),
                "triangles": triangle_count(obj),
                "materials": len(obj.data.materials),
            }
            for obj in meshes
        ],
        "actions": sorted(actions),
        "maximumBodyInfluences": maximum_influences,
        "unweightedBodyVertices": unweighted_vertices,
        "images": images,
        "clipSamples": clip_samples,
        "acceptance": "Exported GLB round-trip validated in Blender.",
    }
    REPORT.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print("YALIS_ROUNDTRIP_READY " + json.dumps(report), flush=True)


if __name__ == "__main__":
    main()
