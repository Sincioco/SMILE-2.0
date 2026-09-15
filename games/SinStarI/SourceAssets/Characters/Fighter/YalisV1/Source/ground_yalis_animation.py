"""Keep Yalis in place, ground her body, and keep sword sweeps above the arena floor."""

import json
import math
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


PACKAGE = Path(__file__).resolve().parent.parent
SOURCE = PACKAGE / "Source"
BLENDER = PACKAGE / "Blender"
FLOOR_CLEARANCE = 0.001

CONTINUOUS_CONTACT_CLIPS = {"Idle", "Walk", "Defend", "Hit"}


def action_at(rig, name, frame):
    action = bpy.data.actions[name]
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]
    bpy.context.scene.frame_set(frame)
    bpy.context.view_layer.update()
    return action


def write_pose(bone, matrix, frame):
    conversion_arguments = {}
    if bone.parent:
        conversion_arguments = {
            "parent_matrix": bone.parent.matrix,
            "parent_matrix_local": bone.parent.bone.matrix_local,
        }
    bone.matrix_basis = bone.bone.convert_local_to_pose(
        matrix,
        bone.bone.matrix_local,
        invert=True,
        **conversion_arguments,
    )
    bone.rotation_mode = "QUATERNION"
    for property_name in ("location", "rotation_quaternion", "scale"):
        bone.keyframe_insert(data_path=property_name, frame=frame)


def minimum_z(obj):
    evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = evaluated.to_mesh()
    matrix = obj.matrix_world
    value = min((matrix @ vertex.co).z for vertex in mesh.vertices)
    evaluated.to_mesh_clear()
    return value


def canonical_sword_points(sword, sword_rest):
    inverse = sword_rest.inverted()
    return [inverse @ vertex.co for vertex in sword.data.vertices]


def candidate_minimum(rig, pose, points):
    world_pose = rig.matrix_world @ pose
    return min((world_pose @ point).z for point in points)


def floor_safe_sword_pose(rig, pose, points, actual_initial):
    if actual_initial >= FLOOR_CLEARANCE:
        return pose, 0.0, actual_initial

    predicted_initial = candidate_minimum(rig, pose, points)

    best_pose = pose
    best_angle = 0.0
    best_minimum = actual_initial
    for degrees in range(1, 61):
        for signed_degrees in (degrees, -degrees):
            candidate = pose @ Matrix.Rotation(
                math.radians(signed_degrees), 4, "X"
            )
            predicted = candidate_minimum(rig, candidate, points)
            low = actual_initial + predicted - predicted_initial
            if low > best_minimum:
                best_pose = candidate
                best_angle = float(signed_degrees)
                best_minimum = low
            if low >= FLOOR_CLEARANCE:
                return candidate, float(signed_degrees), low
    return best_pose, best_angle, best_minimum


def main():
    bpy.ops.wm.open_mainfile(
        filepath=str(BLENDER / "yalis-v1-equipment-hair.blend")
    )
    scene = bpy.context.scene
    rig = bpy.data.objects["Yalis.Rig"]
    body = bpy.data.objects["Yalis.Body"]
    sword = bpy.data.objects["Yalis.Sword"]
    hips = rig.pose.bones["mixamorig:Hips"]
    sword_bone = rig.pose.bones["YalisSword"]

    rig.data.pose_position = "REST"
    bpy.context.view_layer.update()
    bind_minimum = minimum_z(body)
    common_offset = -bind_minimum
    rig.location.z += common_offset
    rig.data.pose_position = "POSE"
    bpy.context.view_layer.update()

    body_report = []
    for action in sorted(bpy.data.actions, key=lambda item: item.name):
        name = action.name
        end_frame = int(action.frame_range[1])
        action_at(rig, name, 1)
        origin = hips.matrix.translation.copy()
        samples = []
        for frame in range(1, end_frame + 1):
            action_at(rig, name, frame)
            matrix = hips.matrix.copy()
            matrix.translation.x = origin.x
            matrix.translation.y = origin.y
            low = minimum_z(body)
            if name in CONTINUOUS_CONTACT_CLIPS:
                lift = FLOOR_CLEARANCE - low
            else:
                lift = max(0.0, FLOOR_CLEARANCE - low)
            matrix.translation.z += lift
            samples.append((frame, matrix, low, lift))

        for frame, matrix, _low, _lift in samples:
            action_at(rig, name, frame)
            write_pose(hips, matrix, frame)

        body_report.append(
            {
                "clip": name,
                "frames": end_frame,
                "firstMinimumBeforeMeters": samples[0][2],
                "minimumBeforeMeters": min(sample[2] for sample in samples),
                "maximumBeforeMeters": max(sample[2] for sample in samples),
                "minimumCorrectionMeters": min(sample[3] for sample in samples),
                "maximumCorrectionMeters": max(sample[3] for sample in samples),
                "rootXYMeters": [origin.x, origin.y],
            }
        )
        print("YALIS_BODY_GROUNDED " + json.dumps(body_report[-1]), flush=True)

    sword_rest = rig.data.bones["YalisSword"].matrix_local.copy()
    sword_points = canonical_sword_points(sword, sword_rest)
    sword_report = []
    for action in sorted(bpy.data.actions, key=lambda item: item.name):
        name = action.name
        end_frame = int(action.frame_range[1])
        adjustments = []
        predicted_minimum = float("inf")
        for frame in range(1, end_frame + 1):
            action_at(rig, name, frame)
            current = sword_bone.matrix.copy()
            actual_low = minimum_z(sword)
            corrected, degrees, low = floor_safe_sword_pose(
                rig, current, sword_points, actual_low
            )
            if degrees:
                write_pose(sword_bone, corrected, frame)
                adjustments.append({"frame": frame, "degrees": degrees})
            predicted_minimum = min(predicted_minimum, low)

        sword_report.append(
            {
                "clip": name,
                "adjustedFrames": len(adjustments),
                "maximumAbsoluteTiltDegrees": max(
                    [abs(item["degrees"]) for item in adjustments] or [0.0]
                ),
                "predictedBoundingBoxMinimumMeters": predicted_minimum,
                "adjustments": adjustments,
            }
        )
        print(
            "YALIS_SWORD_GROUNDED "
            + json.dumps(
                {
                    key: value
                    for key, value in sword_report[-1].items()
                    if key != "adjustments"
                }
            ),
            flush=True,
        )

    action_at(rig, "Idle", 1)
    rig["YalisGrounded"] = True
    report = {
        "bindBodyMinimumBeforeMeters": bind_minimum,
        "commonActorOffsetMeters": common_offset,
        "floorClearanceMeters": FLOOR_CLEARANCE,
        "body": body_report,
        "sword": sword_report,
        "policy": (
            "All clips keep a stable root X/Y for game-controlled movement. "
            "Idle, Walk, Defend, and Hit retain continuous body contact; other "
            "clips preserve positive airtime and correct only floor penetration. "
            "Held sword sweeps retain the hand grip and use the smallest local "
            "crossguard-axis tilt needed to clear the floor."
        ),
    }
    (SOURCE / "grounding-report.json").write_text(
        json.dumps(report, indent=2), encoding="utf-8"
    )
    bpy.ops.wm.save_as_mainfile(
        filepath=str(BLENDER / "yalis-v1-grounded.blend"),
        compress=True,
    )
    print("YALIS_GROUNDING_READY", flush=True)


if __name__ == "__main__":
    main()
