"""Export Arin v5.4 as a single-skin GLB accepted by the SMILE asset cooker."""

from __future__ import annotations

import os
import sys
import json
import struct
from math import ceil, floor

import bpy
from mathutils import Matrix


ARMATURE_NAME = "ArinRig"
BODY_NAME = "ArinBody"
SMILE_ROOT_BONE = "SMILE_Root"
RIGID_ATTACHMENTS = {
    "ArinSword": "R_Hand",
    "ArinSwordGripGlove": "R_Hand",
    "ArinShield": "L_Hand",
}


def output_path() -> str:
    if "--" not in sys.argv:
        raise RuntimeError("Pass the destination GLB path after --.")

    arguments = sys.argv[sys.argv.index("--") + 1 :]
    if len(arguments) != 1:
        raise RuntimeError("Expected exactly one destination GLB path after --.")

    return os.path.abspath(arguments[0])


def require_object(name: str, object_type: str) -> bpy.types.Object:
    value = bpy.data.objects.get(name)
    if value is None or value.type != object_type:
        raise RuntimeError(f"Required {object_type.lower()} object is missing: {name}")

    return value


def convert_rigid_attachment(
    value: bpy.types.Object,
    armature: bpy.types.Object,
    bone_name: str,
    original_world: Matrix,
) -> None:
    if armature.data.bones.get(bone_name) is None:
        raise RuntimeError(f"Required attachment bone is missing: {bone_name}")

    value.data = value.data.copy()
    value.data.transform(armature.matrix_world.inverted() @ original_world)
    value.parent = armature
    value.parent_type = "OBJECT"
    value.parent_bone = ""
    value.matrix_parent_inverse = Matrix.Identity(4)
    value.matrix_basis = Matrix.Identity(4)

    value.vertex_groups.clear()
    group = value.vertex_groups.new(name=bone_name)
    group.add(range(len(value.data.vertices)), 1.0, "REPLACE")

    for modifier in list(value.modifiers):
        value.modifiers.remove(modifier)

    modifier = value.modifiers.new(name="ArinRig", type="ARMATURE")
    modifier.object = armature


def sample_armature_motion(
    armature: bpy.types.Object,
) -> dict[str, list[tuple[int, Matrix]]]:
    if armature.animation_data is None:
        raise RuntimeError("ArinRig has no animation data.")

    for track in armature.animation_data.nla_tracks:
        track.mute = True

    result: dict[str, list[tuple[int, Matrix]]] = {}
    for action in sorted(bpy.data.actions, key=lambda value: value.name):
        armature.animation_data.action = action
        first_frame = ceil(action.frame_range[0])
        last_frame = floor(action.frame_range[1])
        samples: list[tuple[int, Matrix]] = []
        for frame in range(first_frame, last_frame + 1):
            bpy.context.scene.frame_set(frame)
            bpy.context.view_layer.update()
            samples.append((frame, armature.matrix_world.copy()))
        result[action.name] = samples

    armature.animation_data.action = None
    return result


def add_smile_root_bone(armature: bpy.types.Object) -> None:
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    root_bones = [bone for bone in armature.data.edit_bones if bone.parent is None]
    if len(root_bones) != 1:
        raise RuntimeError(
            f"Expected one ArinRig root bone before export; found {len(root_bones)}."
        )

    smile_root = armature.data.edit_bones.new(SMILE_ROOT_BONE)
    smile_root.head = (0.0, 0.0, 0.0)
    smile_root.tail = (0.0, 0.1, 0.0)
    smile_root.use_deform = False
    root_bones[0].parent = smile_root
    bpy.ops.object.mode_set(mode="OBJECT")


def remove_armature_object_curves() -> None:
    for action in bpy.data.actions:
        for layer in action.layers:
            for strip in layer.strips:
                for channel_bag in strip.channelbags:
                    for curve in list(channel_bag.fcurves):
                        if not curve.data_path.startswith("pose.bones"):
                            channel_bag.fcurves.remove(curve)


def bake_armature_motion_to_root(
    armature: bpy.types.Object,
    samples: dict[str, list[tuple[int, Matrix]]],
    reference_transform: Matrix,
) -> None:
    if armature.animation_data is None:
        raise RuntimeError("ArinRig has no animation data.")

    reference_inverse = reference_transform.inverted()
    smile_root = armature.pose.bones.get(SMILE_ROOT_BONE)
    if smile_root is None:
        raise RuntimeError(f"Export root bone was not created: {SMILE_ROOT_BONE}")

    smile_root.rotation_mode = "QUATERNION"
    for action_name, action_samples in samples.items():
        action = bpy.data.actions[action_name]
        armature.animation_data.action = action
        for frame, source_world in action_samples:
            bpy.context.scene.frame_set(frame)
            bpy.context.view_layer.update()
            delta = source_world @ reference_inverse
            smile_root.matrix = delta @ smile_root.bone.matrix_local
            smile_root.keyframe_insert(
                data_path="location", frame=frame, group=SMILE_ROOT_BONE
            )
            smile_root.keyframe_insert(
                data_path="rotation_quaternion", frame=frame, group=SMILE_ROOT_BONE
            )
            smile_root.keyframe_insert(
                data_path="scale", frame=frame, group=SMILE_ROOT_BONE
            )

    armature.animation_data.action = None


def read_float_accessor(
    document: dict,
    binary: bytes,
    accessor_index: int,
) -> list[tuple[float, ...]] | None:
    accessor = document["accessors"][accessor_index]
    components_by_type = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4}
    components = components_by_type.get(accessor.get("type"))
    if (
        components is None
        or accessor.get("componentType") != 5126
        or "sparse" in accessor
        or "bufferView" not in accessor
    ):
        return None

    view = document["bufferViews"][accessor["bufferView"]]
    packed_size = components * 4
    stride = view.get("byteStride", packed_size)
    offset = view.get("byteOffset", 0) + accessor.get("byteOffset", 0)
    result: list[tuple[float, ...]] = []
    for index in range(accessor["count"]):
        result.append(
            struct.unpack_from(f"<{components}f", binary, offset + index * stride)
        )
    return result


def channel_matches_bind_pose(
    document: dict,
    binary: bytes,
    animation: dict,
    channel: dict,
) -> bool:
    sampler = animation["samplers"][channel["sampler"]]
    if sampler.get("interpolation", "LINEAR") == "CUBICSPLINE":
        return False

    values = read_float_accessor(document, binary, sampler["output"])
    if values is None:
        return False

    path = channel["target"]["path"]
    node = document["nodes"][channel["target"]["node"]]
    defaults = {
        "translation": (0.0, 0.0, 0.0),
        "rotation": (0.0, 0.0, 0.0, 1.0),
        "scale": (1.0, 1.0, 1.0),
    }
    expected = tuple(node.get(path, defaults[path]))
    tolerance = 0.000001
    if path == "rotation":
        for value in values:
            dot = sum(left * right for left, right in zip(value, expected))
            if abs(abs(dot) - 1.0) > tolerance:
                return False
        return True

    return all(
        all(abs(left - right) <= tolerance for left, right in zip(value, expected))
        for value in values
    )


def compact_glb_tables(document: dict) -> None:
    referenced_accessors: set[int] = set()
    for mesh in document.get("meshes", []):
        for primitive in mesh.get("primitives", []):
            referenced_accessors.update(primitive.get("attributes", {}).values())
            if "indices" in primitive:
                referenced_accessors.add(primitive["indices"])
            for target in primitive.get("targets", []):
                referenced_accessors.update(target.values())
    for skin in document.get("skins", []):
        if "inverseBindMatrices" in skin:
            referenced_accessors.add(skin["inverseBindMatrices"])
    for animation in document.get("animations", []):
        for sampler in animation.get("samplers", []):
            referenced_accessors.add(sampler["input"])
            referenced_accessors.add(sampler["output"])

    accessor_map = {
        old_index: new_index
        for new_index, old_index in enumerate(sorted(referenced_accessors))
    }
    for mesh in document.get("meshes", []):
        for primitive in mesh.get("primitives", []):
            primitive["attributes"] = {
                name: accessor_map[index]
                for name, index in primitive.get("attributes", {}).items()
            }
            if "indices" in primitive:
                primitive["indices"] = accessor_map[primitive["indices"]]
            for target in primitive.get("targets", []):
                for name, index in list(target.items()):
                    target[name] = accessor_map[index]
    for skin in document.get("skins", []):
        if "inverseBindMatrices" in skin:
            skin["inverseBindMatrices"] = accessor_map[skin["inverseBindMatrices"]]
    for animation in document.get("animations", []):
        for sampler in animation.get("samplers", []):
            sampler["input"] = accessor_map[sampler["input"]]
            sampler["output"] = accessor_map[sampler["output"]]

    accessors = [
        document["accessors"][index] for index in sorted(referenced_accessors)
    ]
    document["accessors"] = accessors
    referenced_views: set[int] = set()
    for accessor in accessors:
        if "bufferView" in accessor:
            referenced_views.add(accessor["bufferView"])
        if "sparse" in accessor:
            referenced_views.add(accessor["sparse"]["indices"]["bufferView"])
            referenced_views.add(accessor["sparse"]["values"]["bufferView"])
    for image in document.get("images", []):
        if "bufferView" in image:
            referenced_views.add(image["bufferView"])

    view_map = {
        old_index: new_index
        for new_index, old_index in enumerate(sorted(referenced_views))
    }
    for accessor in accessors:
        if "bufferView" in accessor:
            accessor["bufferView"] = view_map[accessor["bufferView"]]
        if "sparse" in accessor:
            accessor["sparse"]["indices"]["bufferView"] = view_map[
                accessor["sparse"]["indices"]["bufferView"]
            ]
            accessor["sparse"]["values"]["bufferView"] = view_map[
                accessor["sparse"]["values"]["bufferView"]
            ]
    for image in document.get("images", []):
        if "bufferView" in image:
            image["bufferView"] = view_map[image["bufferView"]]
    document["bufferViews"] = [
        document["bufferViews"][index] for index in sorted(referenced_views)
    ]


def optimize_glb_animation_tables(path: str) -> None:
    with open(path, "rb") as stream:
        source = stream.read()
    magic, version, _ = struct.unpack_from("<4sII", source, 0)
    if magic != b"glTF" or version != 2:
        raise RuntimeError("Expected a Blender glTF 2.0 binary export.")
    json_length, json_type = struct.unpack_from("<II", source, 12)
    if json_type != 0x4E4F534A:
        raise RuntimeError("GLB JSON chunk is missing.")
    json_start = 20
    json_end = json_start + json_length
    document = json.loads(source[json_start:json_end].decode("utf-8").rstrip(" \0"))
    binary_length, binary_type = struct.unpack_from("<II", source, json_end)
    if binary_type != 0x004E4942:
        raise RuntimeError("GLB binary chunk is missing.")
    binary = source[json_end + 8 : json_end + 8 + binary_length]

    removed_channels = 0
    for animation in document.get("animations", []):
        kept_channels = []
        for channel in animation.get("channels", []):
            if channel_matches_bind_pose(document, binary, animation, channel):
                removed_channels += 1
            else:
                kept_channels.append(channel)
        used_samplers = sorted({channel["sampler"] for channel in kept_channels})
        sampler_map = {
            old_index: new_index
            for new_index, old_index in enumerate(used_samplers)
        }
        for channel in kept_channels:
            channel["sampler"] = sampler_map[channel["sampler"]]
        animation["channels"] = kept_channels
        animation["samplers"] = [
            animation["samplers"][index] for index in used_samplers
        ]

    compact_glb_tables(document)
    if len(document["bufferViews"]) > 1024 or len(document["accessors"]) > 1024:
        raise RuntimeError(
            "Optimized character GLB exceeds SMILE's 1,024 bufferView/accessor limit: "
            f"{len(document['bufferViews'])} bufferViews, "
            f"{len(document['accessors'])} accessors."
        )

    json_bytes = json.dumps(
        document, ensure_ascii=False, separators=(",", ":")
    ).encode("utf-8")
    json_bytes += b" " * ((4 - len(json_bytes) % 4) % 4)
    binary += b"\0" * ((4 - len(binary) % 4) % 4)
    total_length = 12 + 8 + len(json_bytes) + 8 + len(binary)
    result = bytearray(struct.pack("<4sII", b"glTF", 2, total_length))
    result.extend(struct.pack("<II", len(json_bytes), 0x4E4F534A))
    result.extend(json_bytes)
    result.extend(struct.pack("<II", len(binary), 0x004E4942))
    result.extend(binary)
    with open(path, "wb") as stream:
        stream.write(result)
    print(
        f"Removed {removed_channels} bind-pose animation channels; "
        f"retained {len(document['bufferViews'])} bufferViews and "
        f"{len(document['accessors'])} accessors."
    )


def main() -> None:
    destination = output_path()
    armature = require_object(ARMATURE_NAME, "ARMATURE")
    body = require_object(BODY_NAME, "MESH")
    attachments = {
        name: require_object(name, "MESH") for name in RIGID_ATTACHMENTS
    }

    if bpy.context.object is not None and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")

    motion_samples = sample_armature_motion(armature)
    idle_samples = motion_samples.get("Idle")
    if not idle_samples:
        raise RuntimeError("Idle animation has no sampled armature motion.")
    reference_transform = idle_samples[0][1]
    armature.matrix_world = reference_transform

    armature.data.pose_position = "REST"
    bpy.context.scene.frame_set(0)
    bpy.context.view_layer.update()

    original_world = {name: value.matrix_world.copy() for name, value in attachments.items()}
    for name, bone_name in RIGID_ATTACHMENTS.items():
        convert_rigid_attachment(attachments[name], armature, bone_name, original_world[name])

    add_smile_root_bone(armature)
    remove_armature_object_curves()
    armature.data.pose_position = "POSE"
    bake_armature_motion_to_root(armature, motion_samples, reference_transform)
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action="DESELECT")
    for value in [armature, body, *attachments.values()]:
        value.hide_set(False)
        value.select_set(True)
    bpy.context.view_layer.objects.active = armature

    os.makedirs(os.path.dirname(destination), exist_ok=True)
    status = bpy.ops.export_scene.gltf(
        filepath=destination,
        export_format="GLB",
        use_selection=True,
        export_materials="EXPORT",
        export_animations=True,
        export_animation_mode="ACTIONS",
        export_merge_animation="ACTION",
        export_anim_single_armature=True,
        export_anim_scene_split_object=True,
        export_armature_object_remove=True,
        export_reset_pose_bones=False,
        export_rest_position_armature=True,
        export_optimize_animation_keep_anim_armature=False,
        export_extra_animations=False,
        export_skins=True,
    )
    if "FINISHED" not in status:
        raise RuntimeError(f"Blender GLB export failed: {sorted(status)}")

    optimize_glb_animation_tables(destination)
    print(f"Exported {destination}")


if __name__ == "__main__":
    main()
