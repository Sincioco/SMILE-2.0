"""Build Arin v5.5 from reviewed geometry and fresh 2K Mixamo retarget sources."""

from __future__ import annotations

import hashlib
import json
import math
import os
import sys

import bpy
from mathutils import Matrix


def command_arguments() -> tuple[str, str]:
    if "--" not in sys.argv:
        raise RuntimeError("Pass the destination Blend and manifest paths after --.")
    arguments = sys.argv[sys.argv.index("--") + 1 :]
    if len(arguments) != 2:
        raise RuntimeError("Expected destination Blend and build manifest paths after --.")
    return os.path.abspath(arguments[0]), os.path.abspath(arguments[1])


def sha256_file(path: str) -> str:
    digest = hashlib.sha256()
    with open(path, "rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def load_manifest(path: str) -> dict:
    with open(path, "r", encoding="utf-8") as stream:
        value = json.load(stream)
    required = {
        "version", "assetId", "candidateVersion", "baseCandidateVersion",
        "baseBlend", "bodySource", "equipmentSource", "armature", "body",
        "bodyPartPrefix", "excludedBodyParts", "expectedBodyParts",
        "expectedBodyVertices", "equipmentTextureParts", "removedAttachments",
        "actions", "animationSources",
        "expectedBlenderVersion", "restMatrixTolerance", "expectedTextureSize",
    }
    if set(value) != required or value["version"] != 2:
        raise RuntimeError("The Arin v5.5 build manifest is not the supported exact shape.")
    if value["assetId"] != "sin-star-i.character-1.paladin":
        raise RuntimeError("The build manifest does not identify canonical Arin.")
    if value["candidateVersion"] != "v5.5" or value["baseCandidateVersion"] != "v5.4":
        raise RuntimeError("The build manifest candidate lineage is invalid.")
    if len(value["actions"]) != 11 or len(value["actions"]) != len(set(value["actions"])):
        raise RuntimeError("The build manifest must name eleven unique actions.")
    if set(value["animationSources"]) != set(value["actions"]):
        raise RuntimeError("Every action must have exactly one fresh animation source.")
    for name, specification in value["animationSources"].items():
        if set(specification) != {"source", "mixamoDescription", "frames", "sha256"}:
            raise RuntimeError(f"Animation source {name} does not have the exact supported shape.")
        if not isinstance(specification["frames"], int) or specification["frames"] < 2:
            raise RuntimeError(f"Animation source {name} has an invalid frame count.")
        if not isinstance(specification["sha256"], str) or len(specification["sha256"]) != 64:
            raise RuntimeError(f"Animation source {name} has an invalid SHA-256 value.")
    if bpy.app.version_string.split()[0] != value["expectedBlenderVersion"]:
        raise RuntimeError(
            f"Expected Blender {value['expectedBlenderVersion']}; running {bpy.app.version_string}."
        )
    return value


def repository_path(manifest_path: str, relative_path: str) -> str:
    repository_root = os.path.dirname(os.path.dirname(manifest_path))
    result = os.path.abspath(os.path.join(repository_root, relative_path))
    prefix = repository_root + os.sep
    if not result.startswith(prefix):
        raise RuntimeError(f"Manifest path escaped the repository: {relative_path}")
    return result


def require_object(name: str, object_type: str) -> bpy.types.Object:
    value = bpy.data.objects.get(name)
    if value is None or value.type != object_type:
        raise RuntimeError(f"Required {object_type.lower()} object is missing: {name}")
    return value


def imported_objects(path: str) -> list[bpy.types.Object]:
    before = set(bpy.data.objects)
    status = bpy.ops.import_scene.gltf(filepath=path)
    if "FINISHED" not in status:
        raise RuntimeError(f"Blender failed to import {path}: {sorted(status)}")
    return [value for value in bpy.data.objects if value not in before]


def remove_objects(values: list[bpy.types.Object]) -> None:
    for value in values:
        try:
            name = value.name
        except ReferenceError:
            continue
        if name in bpy.data.objects:
            bpy.data.objects.remove(value, do_unlink=True)


def validate_texture_images(prefix: str, expected_size: int) -> list[bpy.types.Image]:
    images = sorted(
        [image for image in bpy.data.images if image.name.startswith(prefix)],
        key=lambda image: image.name,
    )
    if len(images) != 3:
        raise RuntimeError(f"Expected three {prefix} images; found {len(images)}.")
    for image in images:
        if tuple(image.size) != (expected_size, expected_size) or image.packed_file is None:
            raise RuntimeError(
                f"Image {image.name} is not a packed {expected_size}x{expected_size} source."
            )
    return images


def validate_rest_pose(
    target: bpy.types.Object,
    imported: bpy.types.Object,
    tolerance: float,
) -> float:
    target_names = set(target.data.bones.keys())
    imported_names = set(imported.data.bones.keys())
    if target_names != imported_names or len(target_names) != 41:
        raise RuntimeError("The 2K body skeleton does not match the v5.4 Arin skeleton.")
    maximum = 0.0
    for name in sorted(target_names):
        left = target.data.bones[name].matrix_local
        right = imported.data.bones[name].matrix_local
        difference = max(
            abs(left[row][column] - right[row][column])
            for row in range(4)
            for column in range(4)
        )
        maximum = max(maximum, difference)
        if difference > tolerance:
            raise RuntimeError(
                f"The 2K body rest pose differs at {name}: {difference} > {tolerance}."
            )
    return maximum


def bind_to_armature(value: bpy.types.Object, armature: bpy.types.Object) -> None:
    modifiers = [modifier for modifier in value.modifiers if modifier.type == "ARMATURE"]
    if len(modifiers) != 1:
        raise RuntimeError(f"Body part {value.name} does not have exactly one armature modifier.")
    modifiers[0].object = armature
    value.parent = armature
    value.parent_type = "OBJECT"
    value.parent_bone = ""
    value.matrix_parent_inverse = Matrix.Identity(4)
    value.matrix_basis = Matrix.Identity(4)


def join_body_parts(
    values: list[bpy.types.Object],
    armature: bpy.types.Object,
    body_name: str,
    expected_vertices: int,
) -> bpy.types.Object:
    bpy.ops.object.select_all(action="DESELECT")
    for value in values:
        bind_to_armature(value, armature)
        value.select_set(True)
    bpy.context.view_layer.objects.active = values[0]
    status = bpy.ops.object.join()
    if "FINISHED" not in status:
        raise RuntimeError(f"Blender failed to join the 2K body parts: {sorted(status)}")
    body = bpy.context.view_layer.objects.active
    body.name = body_name
    body.data.name = body_name + "Mesh"
    if len(body.data.vertices) != expected_vertices:
        raise RuntimeError(
            f"Joined body has {len(body.data.vertices)} vertices; expected {expected_vertices}."
        )
    if len(body.data.materials) != 1:
        raise RuntimeError(f"Joined body has {len(body.data.materials)} material slots; expected one.")
    body.data.materials[0].name = "ArinBodyPBR2K"
    return body


def assign_material(value: bpy.types.Object, material: bpy.types.Material) -> None:
    value.data.materials.clear()
    value.data.materials.append(material)


def clear_actions() -> None:
    for value in bpy.data.objects:
        if value.animation_data is None:
            continue
        value.animation_data.action = None
        for track in list(value.animation_data.nla_tracks):
            value.animation_data.nla_tracks.remove(track)
    for action in list(bpy.data.actions):
        bpy.data.actions.remove(action)


def validate_action_pose(
    armature: bpy.types.Object,
    action: bpy.types.Action,
) -> tuple[int, int]:
    if armature.animation_data is None:
        armature.animation_data_create()
    armature.animation_data.action = action
    scene = bpy.context.scene
    first = int(math.floor(action.frame_range[0]))
    last = int(math.ceil(action.frame_range[1]))
    for frame in range(first, last + 1):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        for bone in armature.pose.bones:
            if not all(math.isfinite(component) for row in bone.matrix for component in row):
                raise RuntimeError(
                    f"Action {action.name} produced a non-finite {bone.name} pose at {frame}."
                )
    return first, last


def import_animation_action(
    path: str,
    target_armature: bpy.types.Object,
    action_name: str,
    expected_frames: int,
    tolerance: float,
) -> dict:
    before_objects = set(bpy.data.objects)
    before_actions = set(bpy.data.actions)
    status = bpy.ops.import_scene.fbx(filepath=path)
    if "FINISHED" not in status:
        raise RuntimeError(f"Blender failed to import {path}: {sorted(status)}")
    new_objects = [value for value in bpy.data.objects if value not in before_objects]
    imported_armatures = [value for value in new_objects if value.type == "ARMATURE"]
    new_actions = [value for value in bpy.data.actions if value not in before_actions]
    if len(imported_armatures) != 1 or len(new_actions) != 1:
        raise RuntimeError(
            f"Animation source {action_name} must import one armature and one action; "
            f"found {len(imported_armatures)} armatures and {len(new_actions)} actions."
        )
    imported_armature = imported_armatures[0]
    rest_delta = validate_rest_pose(target_armature, imported_armature, tolerance)
    action = new_actions[0]
    action.name = action_name
    action.use_fake_user = True
    first, last = validate_action_pose(target_armature, action)
    actual_frames = last - first + 1
    if actual_frames != expected_frames:
        raise RuntimeError(
            f"Animation source {action_name} has {actual_frames} frames "
            f"({first}..{last}); expected {expected_frames}."
        )
    remove_objects(new_objects)
    bpy.data.orphans_purge(do_recursive=True)
    return {
        "sourceSha256": sha256_file(path),
        "firstFrame": first,
        "lastFrame": last,
        "frames": actual_frames,
        "restMatrixMaxDelta": rest_delta,
    }


def main() -> None:
    destination, manifest_path = command_arguments()
    manifest = load_manifest(manifest_path)
    body_source = repository_path(manifest_path, manifest["bodySource"])
    equipment_source = repository_path(manifest_path, manifest["equipmentSource"])
    animation_paths = {
        name: repository_path(manifest_path, specification["source"])
        for name, specification in manifest["animationSources"].items()
    }
    for path in (body_source, equipment_source, *animation_paths.values()):
        if not os.path.isfile(path):
            raise RuntimeError(f"Required 2K source is missing: {path}")
    for name, path in animation_paths.items():
        expected_hash = manifest["animationSources"][name]["sha256"].upper()
        actual_hash = sha256_file(path)
        if actual_hash != expected_hash:
            raise RuntimeError(
                f"Animation source {name} hash is {actual_hash}; expected {expected_hash}."
            )

    armature = require_object(manifest["armature"], "ARMATURE")
    old_body = require_object(manifest["body"], "MESH")
    attachments = {
        name: require_object(name, "MESH")
        for name in manifest["equipmentTextureParts"]
    }
    removed_attachments = [
        require_object(name, "MESH") for name in manifest["removedAttachments"]
    ]

    body_import = imported_objects(body_source)
    imported_armatures = [value for value in body_import if value.type == "ARMATURE"]
    if len(imported_armatures) != 1:
        raise RuntimeError("The 2K body source must import exactly one armature.")
    rest_delta = validate_rest_pose(
        armature,
        imported_armatures[0],
        float(manifest["restMatrixTolerance"]),
    )
    body_parts = {
        value.name: value
        for value in body_import
        if value.type == "MESH" and value.name.startswith(manifest["bodyPartPrefix"])
    }
    excluded_names = set(manifest["excludedBodyParts"])
    excluded = [body_parts[name] for name in sorted(excluded_names) if name in body_parts]
    if len(excluded) != len(excluded_names):
        raise RuntimeError("An expected excluded 2K body part is missing.")
    included = [
        value for name, value in sorted(body_parts.items()) if name not in excluded_names
    ]
    if len(included) != manifest["expectedBodyParts"]:
        raise RuntimeError(
            f"The 2K source supplied {len(included)} body parts; "
            f"expected {manifest['expectedBodyParts']}."
        )
    validate_texture_images("Arin_2K", manifest["expectedTextureSize"])
    old_body.name = manifest["body"] + "V54Retired"
    body = join_body_parts(
        included,
        armature,
        manifest["body"],
        manifest["expectedBodyVertices"],
    )
    remove_objects([value for value in body_import if value is not body])

    equipment_import = imported_objects(equipment_source)
    equipment_parts = {
        value.name: value
        for value in equipment_import
        if value.type == "MESH" and value.name.startswith(manifest["bodyPartPrefix"])
    }
    equipment_material = None
    for target_name, specification in manifest["equipmentTextureParts"].items():
        source = equipment_parts.get(specification["sourcePart"])
        if source is None or len(source.data.vertices) != specification["vertices"]:
            raise RuntimeError(f"The 2K equipment source part is invalid for {target_name}.")
        if len(source.data.materials) != 1:
            raise RuntimeError(f"The 2K equipment source part has no single material: {source.name}")
        if equipment_material is None:
            equipment_material = source.data.materials[0]
        elif source.data.materials[0] != equipment_material:
            raise RuntimeError("The 2K sword, shield, and glove do not share one atlas material.")
    validate_texture_images("Paladin_2K", manifest["expectedTextureSize"])
    equipment_material.name = "ArinEquipmentPBR2K"
    for value in attachments.values():
        assign_material(value, equipment_material)
    remove_objects(equipment_import)
    bpy.data.objects.remove(old_body, do_unlink=True)
    remove_objects(removed_attachments)

    clear_actions()
    animation_results = {}
    for name in manifest["actions"]:
        specification = manifest["animationSources"][name]
        animation_results[name] = import_animation_action(
            animation_paths[name],
            armature,
            name,
            specification["frames"],
            float(manifest["restMatrixTolerance"]),
        )

    armature["smile_asset_id"] = manifest["assetId"]
    armature["smile_candidate_version"] = manifest["candidateVersion"]
    armature["smile_candidate_state"] = "M7D-B 2K candidate; release disabled"
    armature["smile_animation_source"] = "fresh Mixamo 2K With Skin retarget set (2026-09-03)"
    armature["smile_texture_source"] = "2K embedded JPEG; lossless gate pending"
    armature["smile_body_source_sha256"] = sha256_file(body_source)
    armature["smile_equipment_source_sha256"] = sha256_file(equipment_source)
    armature["smile_rest_matrix_max_delta"] = rest_delta
    armature.animation_data.action = bpy.data.actions["Idle"]
    armature.data.pose_position = "POSE"
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()
    bpy.data.orphans_purge(do_recursive=True)

    bpy.ops.object.select_all(action="DESELECT")
    body.select_set(True)
    armature.select_set(True)
    bpy.context.view_layer.objects.active = armature
    os.makedirs(os.path.dirname(destination), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=destination, check_existing=False, compress=True)
    result = {
        "assetId": manifest["assetId"],
        "candidateVersion": manifest["candidateVersion"],
        "bodySourceSha256": sha256_file(body_source),
        "equipmentSourceSha256": sha256_file(equipment_source),
        "restMatrixMaxDelta": rest_delta,
        "bodyVertices": len(body.data.vertices),
        "animationSources": animation_results,
        "outputBlendSha256": sha256_file(destination),
    }
    print("SMILE_ARIN_V55_BUILD=" + json.dumps(result, sort_keys=True))


if __name__ == "__main__":
    main()
