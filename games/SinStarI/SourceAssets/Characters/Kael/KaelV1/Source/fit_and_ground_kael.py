"""Fit Kael's repaired sword and bake grounded, in-place native animation."""

import json
import math
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

PACKAGE = Path(__file__).resolve().parent.parent
SOURCE = PACKAGE / "Source"
BLENDER = PACKAGE / "Blender"
FLOOR = 0.001
SWORD_SCALE = 0.72
GRIP_SOURCE_Z = 0.845


def action_at(rig, name, frame):
    action = bpy.data.actions[name]
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]
    bpy.context.scene.frame_set(frame)
    bpy.context.view_layer.update()
    return action


def minimum_z(obj):
    evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = evaluated.to_mesh()
    result = min((obj.matrix_world @ vertex.co).z for vertex in mesh.vertices)
    evaluated.to_mesh_clear()
    return result


def write_pose(bone, matrix, frame):
    args = {}
    if bone.parent:
        args = {"parent_matrix": bone.parent.matrix,
                "parent_matrix_local": bone.parent.bone.matrix_local}
    bone.matrix_basis = bone.bone.convert_local_to_pose(
        matrix, bone.bone.matrix_local, invert=True, **args)
    bone.rotation_mode = "QUATERNION"
    for name in ("location", "rotation_quaternion", "scale"):
        bone.keyframe_insert(data_path=name, frame=frame)


def ground_body(rig, body):
    rig.data.pose_position = "REST"
    bpy.context.view_layer.update()
    bind_low = minimum_z(body)
    rig.location.z -= bind_low
    rig.data.pose_position = "POSE"
    hips = rig.pose.bones["mixamorig:Hips"]
    report = []
    for action in sorted(bpy.data.actions, key=lambda a: a.name):
        name = action.name
        end = int(action.frame_range[1])
        action_at(rig, name, 1)
        origin = hips.matrix.translation.copy()
        first_low = minimum_z(body)
        base_lift = FLOOR - first_low
        samples = []
        for frame in range(1, end + 1):
            action_at(rig, name, frame)
            pose = hips.matrix.copy()
            pose.translation.x = origin.x
            pose.translation.y = origin.y
            low = minimum_z(body)
            continuous = name in {"Idle", "Walk", "Defend", "Hit"}
            continuous |= name == "Death" and frame >= 40
            lift = FLOOR - low if continuous else max(base_lift, FLOOR - low)
            pose.translation.z += lift
            samples.append((frame, pose, low, lift))
        for frame, pose, _, _ in samples:
            action_at(rig, name, frame)
            write_pose(hips, pose, frame)
        row = {"clip": name, "frames": end, "firstMinimumBeforeMeters": first_low,
               "minimumCorrectionMeters": min(s[3] for s in samples),
               "maximumCorrectionMeters": max(s[3] for s in samples)}
        report.append(row)
        print("KAEL_BODY_GROUNDED " + json.dumps(row), flush=True)
    return {"bindMinimumBeforeMeters": bind_low, "commonOffsetMeters": -bind_low,
            "floorClearanceMeters": FLOOR, "clips": report}


def held_pose(hand, palm):
    result = Matrix((hand.col[1].xyz, hand.col[0].xyz,
                     -hand.col[2].xyz)).transposed().to_4x4()
    result.translation = hand @ palm
    return result


def fit_defend(rig):
    """Give the shield animation a right-hand sword guard fitted to Kael."""
    target = bpy.data.objects.new("Temporary Kael Guard", None)
    pole = bpy.data.objects.new("Temporary Kael Elbow", None)
    for obj in (target, pole):
        bpy.context.scene.collection.objects.link(obj)
    names = ["mixamorig:" + name for name in ("RightArm", "RightForeArm", "RightHand")]
    forearm = rig.pose.bones[names[1]]
    constraint = forearm.constraints.new("IK")
    constraint.target = target
    constraint.pole_target = pole
    constraint.chain_count = 2
    constraint.use_stretch = False
    constraint.pole_angle = math.pi
    samples = []
    action = action_at(rig, "Defend", 1)
    for frame in range(1, int(action.frame_range[1]) + 1):
        action_at(rig, "Defend", frame)
        hips = rig.pose.bones["mixamorig:Hips"]
        actor = hips.matrix @ hips.bone.matrix_local.inverted()
        target.location = rig.matrix_world @ actor @ Vector((-0.11, -0.18, 0.65))
        pole.location = rig.matrix_world @ actor @ Vector((-0.29, -0.03, 0.73))
        bpy.context.view_layer.update()
        poses = {name: rig.pose.bones[name].matrix.copy() for name in names}
        blade = (actor.to_quaternion() @ Vector((-0.25, -0.25, 0.935))).normalized()
        cross = Vector((1.0, 0.0, 0.0))
        cross = (cross - blade * cross.dot(blade)).normalized()
        thickness = blade.cross(cross).normalized()
        hand = Matrix((thickness, cross, -blade)).transposed().to_4x4()
        hand.translation = poses[names[2]].translation
        poses[names[2]] = hand
        samples.append((frame, poses))
    forearm.constraints.remove(constraint)
    for frame, poses in samples:
        action_at(rig, "Defend", frame)
        for name in names:
            write_pose(rig.pose.bones[name], poses[name], frame)
            bpy.context.view_layer.update()
    for obj in (target, pole):
        bpy.data.objects.remove(obj, do_unlink=True)


def fit_sword(rig):
    with bpy.data.libraries.load(str(BLENDER / "kael-v1-sword-repaired.blend")) as (src, dst):
        dst.objects = [name for name in src.objects if name == "Kael.Sword"]
    sword = dst.objects[0]
    bpy.context.scene.collection.objects.link(sword)
    hand_rest = rig.data.bones["mixamorig:RightHand"].matrix_local
    wrist = hand_rest.translation
    knuckle = rig.data.bones["mixamorig:RightHandIndex1"].head_local
    palm_world = wrist.lerp(knuckle, 0.62)
    palm = hand_rest.inverted() @ palm_world
    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    bone = rig.data.edit_bones.new("KaelSword")
    bone.head = palm_world
    bone.tail = palm_world + Vector((0.0, 0.1, 0.0))
    bone.parent = rig.data.edit_bones["mixamorig:Hips"]
    bpy.ops.object.mode_set(mode="OBJECT")
    rest = rig.data.bones["KaelSword"].matrix_local
    points = []
    for vertex in sword.data.vertices:
        original = vertex.co.copy()
        local = Vector((original.x, -original.y, GRIP_SOURCE_Z - original.z)) * SWORD_SCALE
        points.append(local)
        vertex.co = rest @ local
    group = sword.vertex_groups.new(name="KaelSword")
    group.add(list(range(len(sword.data.vertices))), 1.0, "REPLACE")
    modifier = sword.modifiers.new("Kael Sword Rig", "ARMATURE")
    modifier.object = rig
    sword.parent = rig
    sword.matrix_parent_inverse = rig.matrix_world.inverted()
    return sword, palm, points


def floor_safe_hand(rig, hand, palm, points):
    def low(candidate):
        world = rig.matrix_world @ held_pose(candidate, palm)
        return min((world @ point).z for point in points)
    initial = low(hand)
    if initial >= FLOOR:
        return hand, 0
    # Rotate the wrist and attached sword together, preserving their grip.
    for angle in range(1, 121):
        for signed in (angle, -angle):
            candidate = hand @ Matrix.Rotation(math.radians(signed), 4, "Y")
            if low(candidate) >= FLOOR:
                return candidate, signed
    raise RuntimeError(f"No floor-safe wrist pose at {bpy.context.scene.frame_current}: {initial}")


def main():
    bpy.ops.wm.open_mainfile(filepath=str(BLENDER / "kael-v1-animation-assembled.blend"))
    rig = bpy.data.objects["Kael.Rig"]
    body = bpy.data.objects["Kael.Body"]
    fit_defend(rig)
    report = ground_body(rig, body)
    sword, palm, points = fit_sword(rig)
    equipment = []
    for action in sorted(bpy.data.actions, key=lambda a: a.name):
        corrections = []
        for frame in range(1, int(action.frame_range[1]) + 1):
            action_at(rig, action.name, frame)
            hand = rig.pose.bones["mixamorig:RightHand"]
            released = action.name == "Death" and frame > 30
            pose, angle = (hand.matrix.copy(), 0) if released else floor_safe_hand(
                rig, hand.matrix.copy(), palm, points)
            if angle:
                write_pose(hand, pose, frame)
                bpy.context.view_layer.update()
                corrections.append({"frame": frame, "degrees": angle})
            sword_pose = held_pose(hand.matrix, palm)
            if released:
                amount = min(1.0, (frame - 30) / 18.0)
                amount = amount * amount * (3.0 - 2.0 * amount)
                drop = Matrix((Vector((0.0, 1.0, 0.0)), Vector((0.0, 0.0, 1.0)),
                               Vector((1.0, 0.0, 0.0)))).transposed().to_4x4()
                drop.translation = Vector((-0.12, -0.20, FLOOR - min(p.y for p in points)))
                sword_pose = Matrix.LocRotScale(sword_pose.translation.lerp(drop.translation, amount),
                    sword_pose.to_quaternion().slerp(drop.to_quaternion(), amount), Vector((1, 1, 1)))
                low = min(((rig.matrix_world @ sword_pose) @ p).z for p in points)
                sword_pose.translation.z += max(0.0, FLOOR - low)
            write_pose(rig.pose.bones["KaelSword"], sword_pose, frame)
        row = {"clip": action.name, "wristCorrections": corrections}
        equipment.append(row)
        print("KAEL_SWORD_KEYED " + action.name, flush=True)
    report["sword"] = {"scale": SWORD_SCALE, "gripSourceZMeters": GRIP_SOURCE_Z,
                       "palmLocalMeters": list(palm), "clips": equipment}
    report["policy"] = ("Ground bind pose before clip corrections; lock root X/Y. "
                         "Every clip starts at the floor. Idle, Walk, Defend, Hit, "
                         "and settled Death maintain contact; other motion retains airtime. "
                         "Floor corrections rotate the wrist and sword together. "
                         "Death releases the sword from frames 30 to 48.")
    (SOURCE / "grounding-report.json").write_text(json.dumps(report, indent=2), encoding="utf-8", newline="\n")
    action_at(rig, "Idle", 1)
    donor = bpy.data.objects.get("Kael.RigProxy.Donor")
    if donor:
        bpy.data.objects.remove(donor, do_unlink=True)
    bpy.data.orphans_purge(do_local_ids=True, do_linked_ids=False, do_recursive=True)
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER / "kael-v1-grounded.blend"), compress=True)
    print("KAEL_GROUNDING_READY", flush=True)


if __name__ == "__main__":
    main()
