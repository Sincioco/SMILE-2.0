"""Export Arin v5.4 as a single-skin GLB accepted by the SMILE asset cooker."""

from __future__ import annotations

import os
import sys
import json
import struct
import hashlib
import math
import uuid
from math import ceil, floor

import bpy
from mathutils import Matrix


MANIFEST_PATH = os.path.splitext(__file__)[0] + ".manifest.json"
SMILE_ROOT_BONE = "SMILE_Root"


def load_manifest() -> dict:
    with open(MANIFEST_PATH, "r", encoding="utf-8") as stream:
        value = json.load(stream)
    required = {
        "version", "assetId", "candidateVersion", "prototypeAlias", "armature",
        "body", "attachments", "actions", "referenceAction", "referenceFrame",
        "referenceTransformPolicy", "sampleRate", "expectedBlenderVersion",
        "allowedAttachmentModifiers", "allowedGlbExtensions",
    }
    if set(value) != required or value["version"] != 1:
        raise RuntimeError("The Arin export manifest schema is not the supported exact version 1 shape.")
    if value["assetId"] != "sin-star-i.character-1.paladin" or value["candidateVersion"] != "v5.4":
        raise RuntimeError("The export manifest does not identify the approved Arin v5.4 candidate.")
    if len(value["actions"]) != len(set(value["actions"])) or not value["actions"]:
        raise RuntimeError("The export action allowlist must be non-empty and unique.")
    if value["referenceAction"] not in value["actions"] or value["sampleRate"] not in (24, 30, 60):
        raise RuntimeError("The manifest reference action or sample rate is invalid.")
    if bpy.app.version_string.split()[0] != value["expectedBlenderVersion"]:
        raise RuntimeError(
            f"Expected Blender {value['expectedBlenderVersion']}; running {bpy.app.version_string}."
        )
    return value


def output_path() -> str:
    if "--" not in sys.argv:
        raise RuntimeError("Pass the destination GLB path after --.")

    arguments = sys.argv[sys.argv.index("--") + 1 :]
    if len(arguments) != 1:
        raise RuntimeError("Expected exactly one destination GLB path after --.")

    return os.path.abspath(arguments[0])


def sha256_file(path: str) -> str:
    digest = hashlib.sha256()
    with open(path, "rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


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
    allowed_modifiers: set[str],
) -> None:
    if armature.data.bones.get(bone_name) is None:
        raise RuntimeError(f"Required attachment bone is missing: {bone_name}")

    unexpected = sorted({modifier.type for modifier in value.modifiers} - allowed_modifiers)
    if unexpected:
        raise RuntimeError(
            f"Attachment {value.name} has unsupported visible modifiers: {', '.join(unexpected)}"
        )

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
    action_names: list[str],
    sample_rate: int,
) -> dict[str, list[tuple[float, Matrix]]]:
    if armature.animation_data is None:
        raise RuntimeError("ArinRig has no animation data.")

    for track in armature.animation_data.nla_tracks:
        track.mute = True

    source_fps = bpy.context.scene.render.fps / bpy.context.scene.render.fps_base
    if not math.isfinite(source_fps) or source_fps <= 0:
        raise RuntimeError("The Blender scene FPS is invalid.")
    actions = []
    for name in action_names:
        action = bpy.data.actions.get(name)
        if action is None:
            raise RuntimeError(f"Required manifest action is missing: {name}")
        actions.append(action)

    result: dict[str, list[tuple[float, Matrix]]] = {}
    for action in actions:
        armature.animation_data.action = action
        first_frame = float(action.frame_range[0])
        last_frame = float(action.frame_range[1])
        if (
            not math.isfinite(first_frame)
            or not math.isfinite(last_frame)
            or last_frame < first_frame
            or last_frame - first_frame > source_fps * 600
        ):
            raise RuntimeError(f"Action {action.name} has an unsupported time range.")
        step = source_fps / sample_rate
        sample_count = max(1, int(math.floor((last_frame - first_frame) / step)) + 1)
        sample_frames = [first_frame + index * step for index in range(sample_count)]
        if not math.isclose(sample_frames[-1], last_frame, abs_tol=1e-7):
            sample_frames.append(last_frame)
        samples: list[tuple[float, Matrix]] = []
        for frame in sample_frames:
            whole = math.floor(frame)
            bpy.context.scene.frame_set(whole, subframe=frame - whole)
            bpy.context.view_layer.update()
            matrix = armature.matrix_world.copy()
            if not all(math.isfinite(value) for row in matrix for value in row):
                raise RuntimeError(f"Action {action.name} produced a non-finite transform at {frame}.")
            samples.append((frame, matrix))
        result[action.name] = samples

    armature.animation_data.action = None
    return result


def add_smile_root_bone(armature: bpy.types.Object) -> None:
    if armature.data.bones.get(SMILE_ROOT_BONE) is not None:
        raise RuntimeError(
            "The clean export source already contains SMILE_Root; refusing a second root insertion."
        )
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


def remove_armature_object_curves(action_names: list[str]) -> None:
    for action_name in action_names:
        action = bpy.data.actions[action_name]
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
            whole = math.floor(frame)
            bpy.context.scene.frame_set(whole, subframe=frame - whole)
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


def validate_glb_document(document: dict, binary: bytes, allowed_extensions: set[str]) -> None:
    extensions = set(document.get("extensionsUsed", [])) | set(document.get("extensionsRequired", []))
    unexpected = sorted(extensions - allowed_extensions)
    if unexpected:
        raise RuntimeError(f"Unexpected GLB extensions: {', '.join(unexpected)}")
    views = document.get("bufferViews", [])
    accessors = document.get("accessors", [])
    for index, view in enumerate(views):
        if view.get("buffer", 0) != 0:
            raise RuntimeError(f"bufferView {index} references an unsupported buffer.")
        offset = view.get("byteOffset", 0)
        length = view.get("byteLength", 0)
        if offset < 0 or length < 0 or offset + length > len(binary):
            raise RuntimeError(f"bufferView {index} escapes the GLB binary chunk.")
    for index, accessor in enumerate(accessors):
        view_index = accessor.get("bufferView")
        if view_index is not None and (view_index < 0 or view_index >= len(views)):
            raise RuntimeError(f"accessor {index} references an invalid bufferView.")
        if accessor.get("count", 0) < 0:
            raise RuntimeError(f"accessor {index} has an invalid count.")


def externalize_glb_images(document: dict, binary: bytes, destination: str) -> list[str]:
    image_paths: list[str] = []
    base_name = os.path.splitext(os.path.basename(destination))[0]
    destination_directory = os.path.dirname(destination)
    extensions = {"image/jpeg": ".jpg", "image/png": ".png"}
    for index, image in enumerate(document.get("images", [])):
        view_index = image.get("bufferView")
        mime_type = image.get("mimeType")
        if view_index is None or mime_type not in extensions:
            raise RuntimeError(
                f"Image {index} is not a supported embedded JPEG or PNG source."
            )
        view = document["bufferViews"][view_index]
        offset = view.get("byteOffset", 0)
        length = view.get("byteLength", 0)
        image_bytes = binary[offset : offset + length]
        file_name = f"{base_name}.texture-{index:02d}{extensions[mime_type]}"
        image_path = os.path.join(destination_directory, file_name)
        temporary_image_path = image_path + f".tmp-{uuid.uuid4().hex}"
        with open(temporary_image_path, "wb") as stream:
            stream.write(image_bytes)
        os.replace(temporary_image_path, image_path)
        image.pop("bufferView")
        image["uri"] = file_name
        image_paths.append(image_path)
    return image_paths


def optimize_glb_animation_tables(
    path: str,
    destination: str,
    action_names: list[str],
    allowed_extensions: set[str],
) -> list[str]:
    with open(path, "rb") as stream:
        source = stream.read()
    if len(source) < 28:
        raise RuntimeError("Generated GLB is truncated.")
    magic, version, declared_length = struct.unpack_from("<4sII", source, 0)
    if magic != b"glTF" or version != 2 or declared_length != len(source):
        raise RuntimeError("Expected a Blender glTF 2.0 binary export.")
    json_length, json_type = struct.unpack_from("<II", source, 12)
    if json_type != 0x4E4F534A:
        raise RuntimeError("GLB JSON chunk is missing.")
    json_start = 20
    json_end = json_start + json_length
    if json_end + 8 > len(source):
        raise RuntimeError("GLB JSON chunk escapes the file.")
    document = json.loads(source[json_start:json_end].decode("utf-8").rstrip(" \0"))
    binary_length, binary_type = struct.unpack_from("<II", source, json_end)
    if binary_type != 0x004E4942:
        raise RuntimeError("GLB binary chunk is missing.")
    binary_end = json_end + 8 + binary_length
    if binary_end > len(source) or binary_end != len(source):
        raise RuntimeError("GLB binary chunk length does not match the file.")
    binary = source[json_end + 8 : binary_end]
    validate_glb_document(document, binary, allowed_extensions)
    image_paths = externalize_glb_images(document, binary, destination)

    animations_by_name = {
        animation.get("name", ""): animation for animation in document.get("animations", [])
    }
    missing = [name for name in action_names if name not in animations_by_name]
    if missing:
        raise RuntimeError(f"Exported GLB is missing manifest actions: {', '.join(missing)}")
    document["animations"] = [animations_by_name[name] for name in action_names]

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
        if not animation["channels"] or not animation["samplers"]:
            raise RuntimeError(f"Animation {animation.get('name', '<unnamed>')} became empty.")

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
    return image_paths


def main() -> None:
    manifest = load_manifest()
    destination = output_path()
    temporary_destination = destination + f".tmp-{uuid.uuid4().hex}.glb"
    armature = require_object(manifest["armature"], "ARMATURE")
    body = require_object(manifest["body"], "MESH")
    attachments = {
        name: require_object(name, "MESH") for name in manifest["attachments"]
    }
    scene = bpy.context.scene
    active_object = bpy.context.view_layer.objects.active
    active_name = active_object.name if active_object else None
    original_mode = active_object.mode if active_object else "OBJECT"
    selected_names = {value.name for value in bpy.context.selected_objects}
    original_frame = scene.frame_current + scene.frame_subframe
    original_pose_position = armature.data.pose_position
    original_action = armature.animation_data.action if armature.animation_data else None
    original_nla_mutes = [] if armature.animation_data is None else [
        (track, track.mute) for track in armature.animation_data.nla_tracks
    ]
    try:
        if bpy.context.object is not None and bpy.context.object.mode != "OBJECT":
            bpy.ops.object.mode_set(mode="OBJECT")

        motion_samples = sample_armature_motion(
            armature,
            manifest["actions"],
            manifest["sampleRate"],
        )
        reference_action = bpy.data.actions[manifest["referenceAction"]]
        armature.animation_data.action = reference_action
        reference_frame = float(manifest["referenceFrame"])
        reference_whole = math.floor(reference_frame)
        scene.frame_set(reference_whole, subframe=reference_frame - reference_whole)
        bpy.context.view_layer.update()
        reference_transform = armature.matrix_world.copy()
        if abs(reference_transform.determinant()) < 1e-8:
            raise RuntimeError("The manifest reference armature transform is singular.")
        armature.matrix_world = reference_transform

        armature.data.pose_position = "REST"
        scene.frame_set(0)
        bpy.context.view_layer.update()

        original_world = {name: value.matrix_world.copy() for name, value in attachments.items()}
        allowed_modifiers = set(manifest["allowedAttachmentModifiers"])
        for name, bone_name in manifest["attachments"].items():
            convert_rigid_attachment(
                attachments[name], armature, bone_name, original_world[name], allowed_modifiers
            )

        add_smile_root_bone(armature)
        remove_armature_object_curves(manifest["actions"])
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
            filepath=temporary_destination,
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

        image_paths = optimize_glb_animation_tables(
            temporary_destination,
            destination,
            manifest["actions"],
            set(manifest["allowedGlbExtensions"]),
        )
        os.replace(temporary_destination, destination)
        metadata = {
            "version": 1,
            "assetId": manifest["assetId"],
            "candidateVersion": manifest["candidateVersion"],
            "blenderVersion": bpy.app.version_string,
            "gltfExporter": "Blender io_scene_gltf2",
            "scriptSha256": sha256_file(__file__),
            "manifestSha256": sha256_file(MANIFEST_PATH),
            "sourceBlendSha256": sha256_file(bpy.data.filepath),
            "outputGlbSha256": sha256_file(destination),
            "textureFiles": [
                {
                    "name": os.path.basename(image_path),
                    "sha256": sha256_file(image_path),
                }
                for image_path in image_paths
            ],
        }
        with open(destination + ".export.json", "w", encoding="utf-8", newline="\n") as stream:
            json.dump(metadata, stream, indent=2)
            stream.write("\n")
        print(f"Exported {destination}")
        print(json.dumps(metadata, sort_keys=True))
    finally:
        if os.path.exists(temporary_destination):
            os.remove(temporary_destination)
        if armature.animation_data is not None:
            armature.animation_data.action = original_action
            for track, muted in original_nla_mutes:
                track.mute = muted
        armature.data.pose_position = original_pose_position
        frame_whole = math.floor(original_frame)
        scene.frame_set(frame_whole, subframe=original_frame - frame_whole)
        bpy.ops.object.select_all(action="DESELECT")
        for name in selected_names:
            value = bpy.data.objects.get(name)
            if value is not None:
                value.select_set(True)
        bpy.context.view_layer.objects.active = bpy.data.objects.get(active_name) if active_name else None
        if original_mode != "OBJECT" and bpy.context.view_layer.objects.active is not None:
            bpy.ops.object.mode_set(mode=original_mode)


if __name__ == "__main__":
    main()
