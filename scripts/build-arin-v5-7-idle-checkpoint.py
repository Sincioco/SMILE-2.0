"""Build the validated Arin v5.7 multi-animation equipment checkpoint."""

from __future__ import annotations

import json
import math
import os
import sys

import bpy
import numpy as np
from mathutils import Euler, Matrix, Vector


SOURCE_RIGHT_HAND_MESH = "tripo_part_5"
SOURCE_LEFT_HAND_MESH = "tripo_part_6"
SOURCE_RIGHT_HAND_BONE = "R_Hand"
SOURCE_LEFT_HAND_BONE = "L_Hand"
TARGET_RIGHT_HAND_BONE = "mixamorig:RightHand"
TARGET_LEFT_HAND_BONE = "mixamorig:LeftHand"
SWORD_CORRECTION_ROTATION = (-15.51063048, -43.72768386, -81.06488564)
SHIELD_CORRECTION_ROTATION = (0.0, 0.0, -75.0)
SWORD_CORRECTION_OFFSET = (-0.04017985, 0.00752897, 0.01881249)
SHIELD_CORRECTION_OFFSET = (0.0, 0.0, -0.055)
SWORD_CORRECTION_PIVOT = (-0.01415075, -0.00344447, 0.01844119)
EQUIPMENT = {
    "Sword": (SOURCE_RIGHT_HAND_MESH, SOURCE_RIGHT_HAND_BONE, TARGET_RIGHT_HAND_BONE),
    "Shield": (SOURCE_LEFT_HAND_MESH, SOURCE_LEFT_HAND_BONE, TARGET_LEFT_HAND_BONE),
    "Shield Strap Main": (
        SOURCE_LEFT_HAND_MESH,
        SOURCE_LEFT_HAND_BONE,
        TARGET_LEFT_HAND_BONE,
    ),
    "Shield Strap 2": (
        SOURCE_LEFT_HAND_MESH,
        SOURCE_LEFT_HAND_BONE,
        TARGET_LEFT_HAND_BONE,
    ),
}


def arguments() -> tuple[str, str, str, str, str, str]:
    values = sys.argv[sys.argv.index("--") + 1 :]
    if len(values) != 6:
        raise RuntimeError(
            "Expected skinned FBX, animation manifest, clean GLB, equipped GLB, "
            "output Blend, and output GLB paths."
        )
    return tuple(os.path.abspath(value) for value in values)


def base_name(name: str) -> str:
    return name.rsplit(".", 1)[0] if name.rsplit(".", 1)[-1].isdigit() else name


def require_imported(imported: list[bpy.types.Object], name: str, object_type: str):
    matches = [
        obj for obj in imported if obj.type == object_type and base_name(obj.name) == name
    ]
    if len(matches) != 1:
        raise RuntimeError(
            f"Expected one imported {object_type.lower()} named {name}; got "
            f"{[obj.name for obj in matches]}."
        )
    return matches[0]


def normalize_mixamo_scale(
    armature: bpy.types.Object,
    actions: list[bpy.types.Action],
) -> float:
    scale = float(armature.scale.x)
    if scale <= 0.0 or any(abs(float(value) - scale) > 1.0e-6 for value in armature.scale):
        raise RuntimeError(f"Mixamo armature scale is not positive and uniform: {armature.scale}.")
    if abs(scale - 1.0) <= 1.0e-6:
        return scale

    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

    for action in actions:
        for layer in action.layers:
            for strip in layer.strips:
                for channel_bag in strip.channelbags:
                    for curve in channel_bag.fcurves:
                        if not curve.data_path.endswith(".location"):
                            continue
                        for keyframe in curve.keyframe_points:
                            keyframe.co.y *= scale
                            keyframe.handle_left.y *= scale
                            keyframe.handle_right.y *= scale
                        for sample in curve.sampled_points:
                            sample.co.y *= scale

    bpy.context.view_layer.update()
    return scale


def action_curves(action: bpy.types.Action):
    for layer in action.layers:
        for strip in layer.strips:
            for channel_bag in strip.channelbags:
                yield from channel_bag.fcurves


def stabilize_shield_arm(
    configuration: dict,
    entries: list[dict],
    action_by_name: dict[str, bpy.types.Action],
) -> list[str]:
    stabilization = configuration.get("shieldArmStabilization")
    target_names = [
        entry["name"]
        for entry in entries
        if entry.get("stabilizeShieldArm", False)
    ]
    if not target_names:
        return []
    if not isinstance(stabilization, dict):
        raise RuntimeError(
            "Shield-arm stabilization targets require shieldArmStabilization."
        )

    source_name = stabilization.get("sourceAction")
    source_frame = stabilization.get("sourceFrame")
    bone_names = stabilization.get("bones")
    if source_name not in action_by_name or not isinstance(source_frame, (int, float)):
        raise RuntimeError("Shield-arm stabilization source action or frame is invalid.")
    if not isinstance(bone_names, list) or not bone_names:
        raise RuntimeError("Shield-arm stabilization requires at least one bone.")

    prefixes = tuple(f'pose.bones["{name}"]' for name in bone_names)
    source_values = {
        (curve.data_path, curve.array_index): curve.evaluate(float(source_frame))
        for curve in action_curves(action_by_name[source_name])
        if curve.data_path.startswith(prefixes)
    }
    if not source_values:
        raise RuntimeError("Shield-arm stabilization source has no matching curves.")

    for target_name in target_names:
        updated_keys = set()
        for curve in action_curves(action_by_name[target_name]):
            if not curve.data_path.startswith(prefixes):
                continue
            key = (curve.data_path, curve.array_index)
            if key not in source_values:
                raise RuntimeError(
                    f"Shield-arm source is missing {target_name} curve {key}."
                )
            value = source_values[key]
            for keyframe in curve.keyframe_points:
                keyframe.co.y = value
                keyframe.handle_left.y = value
                keyframe.handle_right.y = value
            for sample in curve.sampled_points:
                sample.co.y = value
            updated_keys.add(key)
        if updated_keys != set(source_values):
            missing = sorted(set(source_values) - updated_keys)
            raise RuntimeError(
                f"Shield-arm target {target_name} is missing curves: {missing}."
            )

    return target_names


def rotate_pose_bone_direction(
    armature: bpy.types.Object,
    bone_name: str,
    target_direction: tuple[float, float, float],
) -> None:
    pose_bone = armature.pose.bones[bone_name]
    target = Vector(target_direction).normalized()
    current = (pose_bone.matrix.to_3x3() @ Vector((0.0, 1.0, 0.0))).normalized()
    correction = current.rotation_difference(target)
    head = pose_bone.head.copy()
    pose_bone.matrix = (
        Matrix.Translation(head)
        @ correction.to_matrix().to_4x4()
        @ Matrix.Translation(-head)
        @ pose_bone.matrix
    )
    bpy.context.view_layer.update()


def bake_source_armature(
    armature: bpy.types.Object, meshes: list[bpy.types.Object]
) -> None:
    for mesh in meshes:
        bpy.ops.object.select_all(action="DESELECT")
        mesh.select_set(True)
        bpy.context.view_layer.objects.active = mesh
        for modifier in list(mesh.modifiers):
            if modifier.type == "ARMATURE":
                bpy.ops.object.modifier_apply(modifier=modifier.name)
        world_matrix = mesh.matrix_world.copy()
        mesh.parent = None
        mesh.matrix_world = world_matrix
        mesh.vertex_groups.clear()
    bpy.data.objects.remove(armature, do_unlink=True)


def restore_tripo_body(
    source: bpy.types.Object,
    target: bpy.types.Object,
    target_armature: bpy.types.Object,
) -> dict[str, float]:
    source_points = world_points(source, list(range(len(source.data.vertices))))
    target_points = world_points(target, list(range(len(target.data.vertices))))
    squared_distances = np.square(
        source_points[:, None, :] - target_points[None, :, :]
    ).sum(axis=2)
    nearest_indices = squared_distances.argmin(axis=1)
    residuals = np.sqrt(squared_distances.min(axis=1))
    maximum = float(residuals.max())
    if maximum > 1.0e-5:
        raise RuntimeError(
            f"Tripo/Mixamo geometry mismatch for {base_name(source.name)}: {maximum}."
        )

    groups = {}
    for target_group in target.vertex_groups:
        groups[target_group.index] = source.vertex_groups.new(name=target_group.name)
    for source_index, target_index in enumerate(nearest_indices):
        target_vertex = target.data.vertices[int(target_index)]
        for assignment in target_vertex.groups:
            groups[assignment.group].add(
                [source_index], float(assignment.weight), "REPLACE"
            )

    world_matrix = source.matrix_world.copy()
    source.parent = target_armature
    source.matrix_world = world_matrix
    modifier = source.modifiers.new(name="ArinRig", type="ARMATURE")
    modifier.object = target_armature
    source.name = base_name(target.name)
    source.data.name = base_name(target.data.name) + "TripoUv"
    return {
        "maximum": maximum,
        "rms": float(math.sqrt(np.mean(np.square(residuals)))),
        "vertices": len(source.data.vertices),
    }


def group_weight(mesh: bpy.types.Object, vertex_index: int, group_name: str) -> float:
    group = mesh.vertex_groups.get(group_name)
    if group is None:
        return 0.0
    for assignment in mesh.data.vertices[vertex_index].groups:
        if assignment.group == group.index:
            return assignment.weight
    return 0.0


def world_points(mesh: bpy.types.Object, indices: list[int]) -> np.ndarray:
    return np.array(
        [mesh.matrix_world @ mesh.data.vertices[index].co for index in indices],
        dtype=np.float64,
    )


def similarity_transform(
    source_mesh: bpy.types.Object,
    target_mesh: bpy.types.Object,
    source_hand_bone: str,
    target_hand_bone: str,
) -> tuple[Matrix, dict[str, float]]:
    if len(source_mesh.data.vertices) != len(target_mesh.data.vertices):
        raise RuntimeError(
            f"Hand topology differs: {source_mesh.name} has "
            f"{len(source_mesh.data.vertices)} vertices, but {target_mesh.name} has "
            f"{len(target_mesh.data.vertices)}."
        )

    indices = [
        index
        for index in range(len(source_mesh.data.vertices))
        if group_weight(source_mesh, index, source_hand_bone) >= 0.45
        and group_weight(target_mesh, index, target_hand_bone) >= 0.45
    ]
    if len(indices) < 12:
        indices = list(range(len(source_mesh.data.vertices)))

    source = world_points(source_mesh, indices)
    target = world_points(target_mesh, indices)
    source_mean = source.mean(axis=0)
    target_mean = target.mean(axis=0)
    source_centered = source - source_mean
    target_centered = target - target_mean
    covariance = target_centered.T @ source_centered / len(indices)
    left, singular, right_transposed = np.linalg.svd(covariance)
    reflection = np.eye(3)
    reflection[2, 2] = np.sign(np.linalg.det(left @ right_transposed))
    rotation = left @ reflection @ right_transposed
    source_variance = np.square(source_centered).sum() / len(indices)
    scale = float(np.trace(np.diag(singular) @ reflection) / source_variance)
    translation = target_mean - scale * rotation @ source_mean

    matrix = Matrix.Identity(4)
    for row in range(3):
        for column in range(3):
            matrix[row][column] = float(scale * rotation[row, column])
        matrix[row][3] = float(translation[row])

    fitted = (scale * (rotation @ source.T)).T + translation
    residuals = np.linalg.norm(fitted - target, axis=1)
    diagnostics = {
        "points": len(indices),
        "scale": scale,
        "rms": float(math.sqrt(np.mean(np.square(residuals)))),
        "maximum": float(residuals.max()),
    }
    return matrix, diagnostics


def rigid_attachment(
    source: bpy.types.Object,
    target_armature: bpy.types.Object,
    target_reference: bpy.types.Object,
    transform_world: Matrix,
    target_bone: str,
    name: str,
    correction_rotation: tuple[float, float, float],
    correction_offset: tuple[float, float, float],
    correction_pivot: tuple[float, float, float] | None,
) -> bpy.types.Object:
    result = source.copy()
    result.data = source.data.copy()
    result.name = name
    result.data.name = f"{name}Mesh"
    if name == "ArinSword":
        for material_index, material in enumerate(result.data.materials):
            if material is None:
                continue
            sword_material = material.copy()
            sword_material.name = "ArinSwordMaterial"
            result.data.materials[material_index] = sword_material
    result.animation_data_clear()
    bpy.context.collection.objects.link(result)

    source_to_target = (
        target_reference.matrix_world.inverted()
        @ transform_world
        @ source.matrix_world
    )
    result.data.transform(source_to_target)
    bone = target_armature.data.bones[target_bone]
    pivot = Matrix.Translation(
        correction_pivot
        if correction_pivot is not None
        else (0.0, bone.length * 0.56, 0.0)
    )
    correction = (
        bone.matrix_local
        @ pivot
        @ Euler(
            tuple(math.radians(value) for value in correction_rotation), "XYZ"
        ).to_matrix().to_4x4()
        @ pivot.inverted()
        @ bone.matrix_local.inverted()
    )
    result.data.transform(correction)
    offset = (
        bone.matrix_local
        @ Matrix.Translation(correction_offset)
        @ bone.matrix_local.inverted()
    )
    result.data.transform(offset)
    result.parent = target_reference.parent
    result.parent_type = target_reference.parent_type
    result.parent_bone = target_reference.parent_bone
    result.matrix_parent_inverse = target_reference.matrix_parent_inverse.copy()
    result.matrix_basis = target_reference.matrix_basis.copy()
    result.vertex_groups.clear()
    group = result.vertex_groups.new(name=target_bone)
    group.add(range(len(result.data.vertices)), 1.0, "REPLACE")
    for modifier in list(result.modifiers):
        result.modifiers.remove(modifier)
    modifier = result.modifiers.new(name="ArinRig", type="ARMATURE")
    modifier.object = target_armature
    return result


def look_at(obj: bpy.types.Object, target: Vector) -> None:
    obj.rotation_euler = (target - obj.location).to_track_quat("-Z", "Y").to_euler()


def render_preview(
    output_path: str,
    armature: bpy.types.Object,
    frame: int,
) -> None:
    scene = bpy.context.scene
    scene.frame_set(frame)
    bpy.context.view_layer.update()

    hand = armature.matrix_world @ armature.pose.bones[TARGET_RIGHT_HAND_BONE].head
    elbow = armature.matrix_world @ armature.pose.bones["mixamorig:RightForeArm"].head
    target = hand.lerp(elbow, 0.42)

    camera_data = bpy.data.cameras.new("Checkpoint Camera")
    camera = bpy.data.objects.new("Checkpoint Camera", camera_data)
    bpy.context.collection.objects.link(camera)
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 1.15
    camera.location = target + Vector((-1.25, -2.4, 0.55))
    look_at(camera, target)
    scene.camera = camera

    key_data = bpy.data.lights.new("Checkpoint Key", "AREA")
    key_data.energy = 750.0
    key_data.shape = "DISK"
    key_data.size = 3.0
    key = bpy.data.objects.new("Checkpoint Key", key_data)
    bpy.context.collection.objects.link(key)
    key.location = target + Vector((-1.5, -2.0, 2.5))
    look_at(key, target)

    fill_data = bpy.data.lights.new("Checkpoint Fill", "AREA")
    fill_data.energy = 400.0
    fill_data.size = 2.0
    fill = bpy.data.objects.new("Checkpoint Fill", fill_data)
    bpy.context.collection.objects.link(fill)
    fill.location = target + Vector((2.0, 0.5, 1.0))
    look_at(fill, target)

    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 900
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = output_path
    scene.render.film_transparent = False
    if scene.world is None:
        scene.world = bpy.data.worlds.new("Checkpoint World")
    scene.world.color = (0.0, 0.12, 0.018)
    bpy.ops.render.render(write_still=True)

    bpy.data.objects.remove(camera, do_unlink=True)
    bpy.data.objects.remove(key, do_unlink=True)
    bpy.data.objects.remove(fill, do_unlink=True)


def main() -> None:
    (
        skinned_fbx,
        animation_manifest,
        clean_glb,
        equipped_glb,
        output_blend,
        output_glb,
    ) = arguments()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    status = bpy.ops.import_scene.fbx(filepath=skinned_fbx)
    if "FINISHED" not in status:
        raise RuntimeError("Failed to import the skinned Mixamo reference FBX.")

    target_armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    if len(target_armatures) != 1:
        raise RuntimeError("Mixamo FBX did not contain exactly one armature.")
    target_armature = target_armatures[0]
    target_armature.name = "ArinRig"
    original_action = (
        target_armature.animation_data.action
        if target_armature.animation_data is not None
        else None
    )
    target_armature.animation_data_clear()

    with open(animation_manifest, "r", encoding="utf-8") as stream:
        animation_configuration = json.load(stream)
    animation_entries = animation_configuration.get("animations", [])
    if not animation_entries:
        raise RuntimeError("Arin animation manifest contains no animations.")

    target_bones = {bone.name for bone in target_armature.data.bones}
    animation_directory = os.path.dirname(animation_manifest)
    actions = []
    action_by_name = {}
    for entry in animation_entries:
        clip_name = entry.get("name")
        relative_path = entry.get("file")
        if not clip_name or not relative_path:
            raise RuntimeError("Every Arin animation entry requires name and file values.")
        if clip_name in action_by_name:
            raise RuntimeError(f"Duplicate Arin animation name: {clip_name}.")
        animation_fbx = os.path.abspath(os.path.join(animation_directory, relative_path))
        if not os.path.isfile(animation_fbx):
            raise RuntimeError(f"Arin animation input is missing: {animation_fbx}.")

        before_objects = set(bpy.data.objects)
        status = bpy.ops.import_scene.fbx(filepath=animation_fbx)
        if "FINISHED" not in status:
            raise RuntimeError(f"Failed to import Arin animation: {animation_fbx}.")
        animation_imported = [obj for obj in bpy.data.objects if obj not in before_objects]
        animation_armatures = [obj for obj in animation_imported if obj.type == "ARMATURE"]
        if len(animation_armatures) != 1:
            raise RuntimeError(
                f"Arin animation {clip_name} did not contain one armature."
            )
        animation_armature = animation_armatures[0]
        animation_bones = {bone.name for bone in animation_armature.data.bones}
        if target_bones != animation_bones:
            raise RuntimeError(
                f"Arin animation {clip_name} skeleton differs from the skinned "
                f"reference: target-only={sorted(target_bones - animation_bones)}, "
                f"animation-only={sorted(animation_bones - target_bones)}."
            )
        if (
            animation_armature.animation_data is None
            or animation_armature.animation_data.action is None
        ):
            raise RuntimeError(f"Arin animation {clip_name} is missing its action.")
        imported_action = animation_armature.animation_data.action
        action = imported_action.copy()
        action.name = clip_name
        action.use_fake_user = True
        actions.append(action)
        action_by_name[clip_name] = action
        for obj in animation_imported:
            if obj.name in bpy.data.objects:
                bpy.data.objects.remove(obj, do_unlink=True)
        if imported_action.name in bpy.data.actions:
            bpy.data.actions.remove(imported_action)

    if original_action is not None and original_action.name in bpy.data.actions:
        bpy.data.actions.remove(original_action)

    selected_action = action_by_name.get("Idle")
    if selected_action is None:
        raise RuntimeError("Arin animation manifest must include an Idle action.")
    target_armature.animation_data_create()
    target_armature.animation_data.action = selected_action
    target_armature.animation_data.action_slot = selected_action.slots[0]

    normalized_scale = normalize_mixamo_scale(target_armature, actions)
    stabilized_actions = stabilize_shield_arm(
        animation_configuration,
        animation_entries,
        action_by_name,
    )
    target_body_meshes = [
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH" and obj.parent == target_armature
    ]
    target_meshes = {base_name(obj.name): obj for obj in target_body_meshes}
    target_right_hand = target_meshes.get(SOURCE_RIGHT_HAND_MESH)
    target_left_hand = target_meshes.get(SOURCE_LEFT_HAND_MESH)
    if target_right_hand is None or target_left_hand is None:
        raise RuntimeError("Mixamo FBX is missing the expected hand meshes.")

    action = target_armature.animation_data.action
    frame_start = int(math.floor(action.frame_range[0]))
    frame_end = int(math.ceil(action.frame_range[1]))
    bpy.context.scene.frame_start = frame_start
    bpy.context.scene.frame_end = frame_end

    before_objects = set(bpy.data.objects)
    status = bpy.ops.import_scene.gltf(filepath=clean_glb)
    if "FINISHED" not in status:
        raise RuntimeError("Failed to import the clean v5.7 body GLB.")
    clean_imported = [obj for obj in bpy.data.objects if obj not in before_objects]
    helpers = [
        obj
        for obj in clean_imported
        if obj.type == "MESH" and base_name(obj.name) == "Icosphere"
    ]
    clean_imported = [obj for obj in clean_imported if obj not in helpers]
    for helper in helpers:
        bpy.data.objects.remove(helper, do_unlink=True)
    source_armatures = [obj for obj in clean_imported if obj.type == "ARMATURE"]
    if len(source_armatures) != 1:
        raise RuntimeError("Clean v5.7 body GLB did not contain exactly one armature.")
    source_armature = source_armatures[0]
    source_armature.data.pose_position = "POSE"
    rotate_pose_bone_direction(source_armature, "L_Upperarm", (1.0, 0.05, -0.04))
    rotate_pose_bone_direction(source_armature, "R_Upperarm", (-1.0, 0.05, -0.04))
    rotate_pose_bone_direction(source_armature, "L_Forearm", (0.998, -0.06, 0.0))
    rotate_pose_bone_direction(source_armature, "R_Forearm", (-0.998, -0.06, 0.0))
    source_body_meshes = [obj for obj in clean_imported if obj.type == "MESH"]
    bake_source_armature(source_armature, source_body_meshes)
    source_meshes = {base_name(obj.name): obj for obj in source_body_meshes}
    if set(source_meshes) != set(target_meshes):
        raise RuntimeError(
            "Clean Tripo and Mixamo body mesh sets differ: "
            f"source={sorted(source_meshes)}, target={sorted(target_meshes)}."
        )
    body_transfer_diagnostics = {
        name: restore_tripo_body(source_meshes[name], target_meshes[name], target_armature)
        for name in sorted(source_meshes)
    }

    before_objects = set(bpy.data.objects)
    status = bpy.ops.import_scene.gltf(filepath=equipped_glb)
    if "FINISHED" not in status:
        raise RuntimeError("Failed to import the equipped v5.7 reference GLB.")
    imported = [obj for obj in bpy.data.objects if obj not in before_objects]
    source_right_hand = require_imported(imported, SOURCE_RIGHT_HAND_MESH, "MESH")
    source_left_hand = require_imported(imported, SOURCE_LEFT_HAND_MESH, "MESH")

    right_transform, right_diagnostics = similarity_transform(
        source_right_hand,
        target_right_hand,
        SOURCE_RIGHT_HAND_BONE,
        TARGET_RIGHT_HAND_BONE,
    )
    left_transform, left_diagnostics = similarity_transform(
        source_left_hand,
        target_left_hand,
        SOURCE_LEFT_HAND_BONE,
        TARGET_LEFT_HAND_BONE,
    )

    transforms = {
        SOURCE_RIGHT_HAND_MESH: right_transform,
        SOURCE_LEFT_HAND_MESH: left_transform,
    }
    target_references = {
        SOURCE_RIGHT_HAND_MESH: target_right_hand,
        SOURCE_LEFT_HAND_MESH: target_left_hand,
    }
    attachments = []
    for equipment_name, (hand_mesh, _source_bone, target_bone) in EQUIPMENT.items():
        source_equipment = require_imported(imported, equipment_name, "MESH")
        correction_rotation = (
            SWORD_CORRECTION_ROTATION
            if equipment_name == "Sword"
            else SHIELD_CORRECTION_ROTATION
        )
        correction_offset = (
            SWORD_CORRECTION_OFFSET
            if equipment_name == "Sword"
            else SHIELD_CORRECTION_OFFSET
        )
        correction_pivot = (
            SWORD_CORRECTION_PIVOT if equipment_name == "Sword" else None
        )
        attachments.append(
            rigid_attachment(
                source_equipment,
                target_armature,
                target_references[hand_mesh],
                transforms[hand_mesh],
                target_bone,
                "Arin" + equipment_name.replace(" ", ""),
                correction_rotation,
                correction_offset,
                correction_pivot,
            )
        )

    for obj in imported:
        if obj.name in bpy.data.objects:
            bpy.data.objects.remove(obj, do_unlink=True)
    for obj in target_body_meshes:
        if obj.name in bpy.data.objects:
            bpy.data.objects.remove(obj, do_unlink=True)

    target_armature["smile_candidate_version"] = "v5.7"
    target_armature["smile_candidate_state"] = "Validated multi-animation checkpoint"
    target_armature["smile_right_hand_binding"] = "Rigid Mixamo RightHand attachment"
    target_armature["smile_left_hand_binding"] = "Rigid Mixamo LeftHand attachment"
    target_armature["smile_sword_correction_xyz"] = SWORD_CORRECTION_ROTATION
    target_armature["smile_shield_correction_xyz"] = SHIELD_CORRECTION_ROTATION
    target_armature["smile_sword_offset_xyz"] = SWORD_CORRECTION_OFFSET
    target_armature["smile_shield_offset_xyz"] = SHIELD_CORRECTION_OFFSET
    target_armature["smile_sword_pivot_xyz"] = SWORD_CORRECTION_PIVOT
    target_armature["smile_body_source"] = "Pristine Tripo UVs with Mixamo weights"
    bpy.context.scene["smile_candidate_version"] = "v5.7"
    bpy.context.scene["smile_checkpoint"] = "mixamo-multi-animation-rigid-equipment"

    os.makedirs(os.path.dirname(output_blend), exist_ok=True)
    os.makedirs(os.path.dirname(output_glb), exist_ok=True)
    preview_directory = os.path.join(os.path.dirname(output_glb), "arin-v57-idle-previews")
    os.makedirs(preview_directory, exist_ok=True)
    preview_frames = sorted(
        {
            frame_start,
            frame_start + (frame_end - frame_start) // 3,
            frame_start + 2 * (frame_end - frame_start) // 3,
        }
    )
    previews = []
    for frame in preview_frames:
        preview_path = os.path.join(preview_directory, f"idle-{frame:04d}.png")
        render_preview(preview_path, target_armature, frame)
        previews.append(preview_path)

    bpy.context.scene.frame_set(frame_start)
    bpy.ops.wm.save_as_mainfile(filepath=output_blend, check_existing=False, compress=True)

    bpy.ops.object.select_all(action="DESELECT")
    target_armature.select_set(True)
    for obj in bpy.context.scene.objects:
        if obj.type == "MESH" and obj.parent == target_armature:
            obj.select_set(True)
    bpy.context.view_layer.objects.active = target_armature
    bpy.ops.export_scene.gltf(
        filepath=output_glb,
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

    print(
        "ARIN_V57_IDLE_CHECKPOINT="
        + json.dumps(
            {
                "actions": [entry.get("name") for entry in animation_entries],
                "attachments": [obj.name for obj in attachments],
                "blend": output_blend,
                "bodyTransfer": body_transfer_diagnostics,
                "frameRange": [frame_start, frame_end],
                "glb": output_glb,
                "leftFit": left_diagnostics,
                "normalizedMixamoScale": normalized_scale,
                "previews": previews,
                "rightFit": right_diagnostics,
                "stabilizedShieldArmActions": stabilized_actions,
            },
            sort_keys=True,
        )
    )


if __name__ == "__main__":
    main()
