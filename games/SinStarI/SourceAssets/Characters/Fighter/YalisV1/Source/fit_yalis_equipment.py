"""Attach Yalis's sword and add restrained secondary motion to her long hair."""

import json
import math
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


PACKAGE = Path(__file__).resolve().parent.parent
SOURCE = PACKAGE / "Source"
BLENDER = PACKAGE / "Blender"

SWORD_SCALE = 0.72
SWORD_GRIP_SOURCE_Z = 0.875
SWORD_BIND_OFFSET = Vector((0.0, 0.0, 0.70))
SWORD_PALM_OFFSET = Vector((-0.023, -0.037, -0.015))
DEATH_RELEASE_START = 30
DEATH_RELEASE_END = 48

HAIR_AMPLITUDES_DEGREES = {
    "Idle": 1.2,
    "Walk": 2.8,
    "Run": 4.5,
    "Attack": 6.0,
    "Attack2": 7.0,
    "Dodge": 6.0,
    "Defend": 1.8,
    "Hit": 5.0,
    "Death": 7.0,
    "Victory": 3.5,
}

HAIR_CYCLES = {
    "Idle": 1.0,
    "Walk": 2.0,
    "Run": 2.5,
    "Attack": 1.5,
    "Attack2": 2.5,
    "Dodge": 1.5,
    "Defend": 1.0,
    "Hit": 1.0,
    "Death": 1.0,
    "Victory": 2.0,
}


def action_at(rig, name, frame=1):
    action = bpy.data.actions[name]
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]
    bpy.context.scene.frame_set(frame)
    bpy.context.view_layer.update()
    return action


def write_pose(bone, matrix, frame, parent_matrix=None):
    conversion_arguments = {}
    if bone.parent:
        conversion_arguments = {
            "parent_matrix": (
                parent_matrix if parent_matrix is not None else bone.parent.matrix
            ),
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


def forearm_basis(direction):
    along = direction.normalized()
    up = Vector((0.0, 0.0, 1.0))
    up = (up - along * up.dot(along)).normalized()
    side = up.cross(along).normalized()
    return Matrix((up, along, side)).transposed().to_4x4()


def sword_orientation(blade_direction, crossguard_hint):
    blade = blade_direction.normalized()
    crossguard = crossguard_hint - blade * crossguard_hint.dot(blade)
    crossguard.normalize()
    thickness = blade.cross(crossguard).normalized()
    return Matrix((crossguard, thickness, blade)).transposed().to_4x4()


def hand_pose_for_sword(sword_pose, translation):
    sword_x = sword_pose.col[0].xyz
    sword_y = sword_pose.col[1].xyz
    sword_z = sword_pose.col[2].xyz
    hand = Matrix((-sword_y, sword_x, sword_z)).transposed().to_4x4()
    hand.translation = translation
    return hand


def fit_defend_guard(rig):
    """Replace the invisible-shield right arm with a sword-forward guard."""
    scene = bpy.context.scene
    target = bpy.data.objects.new("Temporary Yalis Guard Target", None)
    pole = bpy.data.objects.new("Temporary Yalis Guard Pole", None)
    scene.collection.objects.link(target)
    scene.collection.objects.link(pole)
    names = (
        "mixamorig:RightArm",
        "mixamorig:RightForeArm",
        "mixamorig:RightHand",
    )
    forearm = rig.pose.bones[names[1]]
    constraint = forearm.constraints.new("IK")
    constraint.target = target
    constraint.pole_target = pole
    constraint.chain_count = 2
    constraint.use_stretch = False
    constraint.pole_angle = math.pi

    action = action_at(rig, "Defend")
    samples = []
    for frame in range(1, int(action.frame_range[1]) + 1):
        action_at(rig, "Defend", frame)
        hips = rig.pose.bones["mixamorig:Hips"]
        body_frame = hips.matrix @ hips.bone.matrix_local.inverted()
        target.location = body_frame @ Vector((-0.12, -0.15, 0.66))
        pole.location = body_frame @ Vector((-0.32, -0.08, 0.73))
        bpy.context.view_layer.update()

        desired = {name: rig.pose.bones[name].matrix.copy() for name in names}
        forearm_pose = forearm_basis(
            desired[names[2]].translation - desired[names[1]].translation
        )
        forearm_pose.translation = desired[names[1]].translation
        desired[names[1]] = forearm_pose

        actor_rotation = body_frame.to_quaternion()
        guard = sword_orientation(
            actor_rotation @ Vector((-0.28, -0.16, 0.947)),
            actor_rotation @ Vector((0.96, -0.28, 0.0)),
        )
        desired[names[2]] = hand_pose_for_sword(
            guard, desired[names[2]].translation
        )
        samples.append((frame, desired))

    forearm.constraints.remove(constraint)
    for frame, desired in samples:
        action_at(rig, "Defend", frame)
        for name in names:
            bone = rig.pose.bones[name]
            parent_matrix = (
                desired.get(bone.parent.name, bone.parent.matrix)
                if bone.parent
                else None
            )
            write_pose(bone, desired[name], frame, parent_matrix)

    for obj in (target, pole):
        bpy.data.objects.remove(obj, do_unlink=True)
    print("YALIS_DEFEND_GUARD_FITTED", flush=True)


def add_custom_bones(rig):
    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    rig.data.pose_position = "REST"
    bpy.ops.object.mode_set(mode="EDIT")

    sword_bone = rig.data.edit_bones.new("YalisSword")
    sword_bone.head = SWORD_BIND_OFFSET
    sword_bone.tail = SWORD_BIND_OFFSET + Vector((0.0, 1.0, 0.0))
    sword_bone.parent = rig.data.edit_bones["mixamorig:Hips"]

    hair_root = rig.data.edit_bones.new("YalisHairRoot")
    hair_root.head = (0.0, 0.045, 0.855)
    hair_root.tail = (0.0, 0.060, 0.705)
    hair_root.parent = rig.data.edit_bones["mixamorig:Head"]

    hair_mid = rig.data.edit_bones.new("YalisHairMid")
    hair_mid.head = hair_root.tail
    hair_mid.tail = (0.0, 0.067, 0.565)
    hair_mid.parent = hair_root
    hair_mid.use_connect = True

    hair_tip = rig.data.edit_bones.new("YalisHairTip")
    hair_tip.head = hair_mid.tail
    hair_tip.tail = (0.0, 0.072, 0.425)
    hair_tip.parent = hair_mid
    hair_tip.use_connect = True

    bpy.ops.object.mode_set(mode="OBJECT")
    rig.data.pose_position = "POSE"


def fit_sword(sword, rig):
    for group in list(sword.vertex_groups):
        sword.vertex_groups.remove(group)
    for modifier in list(sword.modifiers):
        if modifier.type == "ARMATURE":
            sword.modifiers.remove(modifier)

    sword_rest = rig.data.bones["YalisSword"].matrix_local.copy()
    for vertex in sword.data.vertices:
        source = vertex.co.copy()
        local = Vector(
            (
                source.x,
                -source.y,
                SWORD_GRIP_SOURCE_Z - source.z,
            )
        ) * SWORD_SCALE
        vertex.co = sword_rest @ local

    sword.name = "Yalis.Sword"
    group = sword.vertex_groups.new(name="YalisSword")
    group.add(list(range(len(sword.data.vertices))), 1.0, "REPLACE")
    modifier = sword.modifiers.new(name="Yalis Sword Rig", type="ARMATURE")
    modifier.object = rig
    modifier.use_deform_preserve_volume = False
    world_matrix = sword.matrix_world.copy()
    sword.parent = rig
    sword.matrix_world = world_matrix
    sword.hide_set(False)
    sword.hide_render = False


def sculpt_right_hand_grip(body):
    """Curl the modeled glove fingertips around the measured vertical hilt."""
    center = Vector((-0.320, 0.021, 0.794))
    affected = 0
    maximum_displacement = 0.0
    for vertex in body.data.vertices:
        x, y, z = vertex.co
        if not (
            x < -0.325
            and 0.765 < z < 0.815
            and -0.025 < y < 0.055
        ):
            continue
        amount = min(1.0, max(0.0, (-x - 0.325) / 0.035))
        amount = amount * amount * (3.0 - 2.0 * amount)
        if amount <= 0.001:
            continue

        original = vertex.co.copy()
        offset = Vector((x - center.x, y - center.y))
        radius = max(0.001, offset.length)
        angle = math.atan2(offset.y, offset.x)
        angle += math.radians(105.0) * amount
        target_radius = 0.013
        radius = radius * (1.0 - 0.65 * amount) + target_radius * 0.65 * amount
        vertex.co.x = center.x + math.cos(angle) * radius
        vertex.co.y = center.y + math.sin(angle) * radius
        maximum_displacement = max(
            maximum_displacement, (vertex.co - original).length
        )
        affected += 1
    return {
        "affectedVertices": affected,
        "maximumDisplacementMeters": maximum_displacement,
        "hiltCenterMeters": list(center),
        "curlDegrees": 105.0,
    }


def hair_weight(vertex):
    x, y, z = vertex.co
    rear = min(1.0, max(0.0, (y - 0.038) / 0.047))
    horizontal = min(1.0, max(0.0, (0.205 - abs(x)) / 0.055))
    lower = min(1.0, max(0.0, (z - 0.40) / 0.08))
    upper = min(1.0, max(0.0, (0.93 - z) / 0.05))
    return 0.34 * rear * horizontal * lower * upper


def assign_hair_weights(body):
    groups = {
        name: body.vertex_groups.new(name=name)
        for name in ("YalisHairRoot", "YalisHairMid", "YalisHairTip")
    }
    affected = []
    per_bone = {name: 0 for name in groups}
    for vertex in body.data.vertices:
        amount = hair_weight(vertex)
        if amount <= 0.002:
            continue

        if vertex.co.z >= 0.70:
            bone_name = "YalisHairRoot"
        elif vertex.co.z >= 0.56:
            bone_name = "YalisHairMid"
        else:
            bone_name = "YalisHairTip"

        existing = [
            (item.group, item.weight * (1.0 - amount))
            for item in vertex.groups
        ]
        for group_index, weight in existing:
            body.vertex_groups[group_index].remove([vertex.index])
            if weight > 0.0001:
                body.vertex_groups[group_index].add(
                    [vertex.index], weight, "REPLACE"
                )
        groups[bone_name].add([vertex.index], amount, "REPLACE")
        affected.append(vertex)
        per_bone[bone_name] += 1

    bpy.ops.object.select_all(action="DESELECT")
    body.select_set(True)
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.vertex_group_limit_total(limit=4)
    bpy.ops.object.vertex_group_normalize_all(lock_active=False)

    bounds = None
    if affected:
        bounds = {
            "minimum": [
                min(vertex.co[axis] for vertex in affected) for axis in range(3)
            ],
            "maximum": [
                max(vertex.co[axis] for vertex in affected) for axis in range(3)
            ],
        }
    return {
        "affectedVertices": len(affected),
        "perBone": per_bone,
        "selectionBoundsMeters": bounds,
        "maximumAddedWeight": 0.34,
    }


def held_sword_pose(hand):
    orientation = Matrix(
        (
            hand.col[1].xyz,
            -hand.col[0].xyz,
            hand.col[2].xyz,
        )
    ).transposed().to_4x4()
    orientation.translation = hand @ SWORD_PALM_OFFSET
    return orientation


def fit_victory_hand(rig, frame):
    """Turn the generic celebration wrist into a safe sword-aloft pose."""
    hand = rig.pose.bones["mixamorig:RightHand"]
    hips = rig.pose.bones["mixamorig:Hips"]
    body_frame = hips.matrix @ hips.bone.matrix_local.inverted()
    actor_rotation = body_frame.to_quaternion()
    victory_sword = sword_orientation(
        actor_rotation @ Vector((-0.16, -0.08, 0.984)),
        actor_rotation @ Vector((0.99, -0.10, 0.0)),
    )
    desired = hand_pose_for_sword(victory_sword, hand.matrix.translation.copy())
    write_pose(hand, desired, frame)
    bpy.context.view_layer.update()


def dropped_sword_pose():
    blade_direction = Vector((-0.80, -0.60, 0.0)).normalized()
    crossguard_direction = Vector((0.60, -0.80, 0.0)).normalized()
    thickness_direction = Vector((0.0, 0.0, 1.0))
    matrix = Matrix(
        (
            crossguard_direction,
            thickness_direction,
            blade_direction,
        )
    ).transposed().to_4x4()
    matrix.translation = Vector((-0.13, -0.08, 0.042))
    return matrix


def interpolate_pose(start, end, amount):
    amount = min(1.0, max(0.0, amount))
    position = start.translation.lerp(end.translation, amount)
    rotation = start.to_quaternion().slerp(end.to_quaternion(), amount)
    return Matrix.LocRotScale(position, rotation, Vector((1.0, 1.0, 1.0)))


def key_equipment_and_hair(rig):
    report = []
    drop_pose = dropped_sword_pose()
    for action in sorted(bpy.data.actions, key=lambda item: item.name):
        name = action.name
        end_frame = int(action.frame_range[1])
        amplitude = HAIR_AMPLITUDES_DEGREES[name]
        cycles = HAIR_CYCLES[name]

        for frame in range(1, end_frame + 1):
            action_at(rig, name, frame)
            phase = (frame - 1) / max(1, end_frame - 1)
            if name == "Victory":
                fit_victory_hand(rig, frame)
            hand = rig.pose.bones["mixamorig:RightHand"].matrix.copy()
            sword_pose = held_sword_pose(hand)
            released = False
            if name == "Death" and frame > DEATH_RELEASE_START:
                release_amount = (
                    (frame - DEATH_RELEASE_START)
                    / (DEATH_RELEASE_END - DEATH_RELEASE_START)
                )
                release_amount = min(1.0, max(0.0, release_amount))
                release_amount = release_amount * release_amount * (3.0 - 2.0 * release_amount)
                sword_pose = interpolate_pose(sword_pose, drop_pose, release_amount)
                released = release_amount >= 1.0
            write_pose(rig.pose.bones["YalisSword"], sword_pose, frame)

            wave = phase * math.tau * cycles
            fade = 1.0
            if name == "Death":
                fade = max(0.18, 1.0 - phase * 0.82)
            for bone_name, multiplier, delay in (
                ("YalisHairRoot", 0.24, 0.00),
                ("YalisHairMid", 0.52, 0.35),
                ("YalisHairTip", 0.78, 0.70),
            ):
                bone = rig.pose.bones[bone_name]
                bone.rotation_mode = "XYZ"
                delayed_wave = wave - delay
                bone.rotation_euler = (
                    math.radians(amplitude * multiplier * math.sin(delayed_wave) * fade),
                    math.radians(amplitude * 0.10 * math.cos(delayed_wave) * fade),
                    math.radians(amplitude * multiplier * 0.30 * math.sin(delayed_wave * 0.8) * fade),
                )
                bone.keyframe_insert(data_path="rotation_euler", frame=frame)

        report.append(
            {
                "clip": name,
                "samples": end_frame,
                "hairAmplitudeDegrees": amplitude,
                "swordBehavior": (
                    f"held through frame {DEATH_RELEASE_START}, then dropped by frame {DEATH_RELEASE_END}"
                    if name == "Death"
                    else "right-hand attachment"
                ),
            }
        )
        print(
            "YALIS_EQUIPMENT_AND_HAIR_KEYED " + json.dumps(report[-1]),
            flush=True,
        )
    return report


def main():
    bpy.ops.wm.open_mainfile(
        filepath=str(BLENDER / "yalis-v1-animation-assembled.blend")
    )
    rig = bpy.data.objects["Yalis.Rig"]
    body = bpy.data.objects["Yalis.Body"]
    sword = bpy.data.objects["Yalis.Sword"]

    add_custom_bones(rig)
    fit_defend_guard(rig)
    fit_sword(sword, rig)
    grip_report = sculpt_right_hand_grip(body)
    hair_report = assign_hair_weights(body)
    clip_report = key_equipment_and_hair(rig)

    action_at(rig, "Idle", 1)
    report = {
        "swordScale": SWORD_SCALE,
        "swordGripSourceZMeters": SWORD_GRIP_SOURCE_Z,
        "swordPalmOffsetMeters": list(SWORD_PALM_OFFSET),
        "modeledGrip": grip_report,
        "swordTriangles": len(sword.data.polygons),
        "hair": hair_report,
        "rigBones": len(rig.data.bones),
        "clips": clip_report,
    }
    (SOURCE / "equipment-fit.json").write_text(
        json.dumps(report, indent=2), encoding="utf-8"
    )
    bpy.ops.wm.save_as_mainfile(
        filepath=str(BLENDER / "yalis-v1-equipment-hair.blend"),
        compress=True,
    )
    print("YALIS_EQUIPMENT_FIT_READY", flush=True)


if __name__ == "__main__":
    main()
