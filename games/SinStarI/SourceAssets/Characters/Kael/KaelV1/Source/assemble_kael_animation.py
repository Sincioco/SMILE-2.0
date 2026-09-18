"""Skin Kael's reduced body and retarget his selected Mixamo animation set."""

import json
from pathlib import Path

import bpy
from mathutils import Matrix, Vector
from mathutils.kdtree import KDTree


PACKAGE = Path(__file__).resolve().parent.parent
SOURCE = PACKAGE / "Source"
MIXAMO = SOURCE / "Mixamo"
BLENDER = PACKAGE / "Blender"

CLIPS = {
    "Idle": ("Idle.fbx", "Sword And Shield Idle"),
    "Walk": ("Walk.fbx", "Sword And Shield Walk (root motion removed during grounding)"),
    "Run": ("Run.fbx", "Sword And Shield Run (root motion removed during grounding)"),
    "Attack": ("Attack.fbx", "Sword and Shield slash (4)"),
    "Attack2": ("Attack2.fbx", "Sword and Shield slash (3)"),
    "Dodge": ("Dodge.fbx", "Dodging Right"),
    "Defend": ("Defend.fbx", "Sword And Shield Block Idle"),
    "Hit": ("Hit.fbx", "Sword And Shield Impact"),
    "Death": ("Death.fbx", "Sword And Shield Death"),
    "Victory": ("Victory.fbx", "Victory"),
}


def apply_rig_transforms(rig, proxy):
    rig.data.pose_position = "REST"
    bpy.ops.object.select_all(action="DESELECT")
    for obj in (rig, proxy):
        obj.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    rig.data.pose_position = "POSE"
    for bone in rig.pose.bones:
        bone.matrix_basis.identity()


def transfer_proxy_weights(proxy, body):
    """Interpolate Mixamo weights from the closest point on the proxy surface."""
    for group in list(body.vertex_groups):
        body.vertex_groups.remove(group)

    group_names = [group.name for group in proxy.vertex_groups]
    for name in group_names:
        body.vertex_groups.new(name=name)

    transfer = body.modifiers.new(name="Kael Proxy Weight Transfer", type="DATA_TRANSFER")
    transfer.object = proxy
    transfer.use_vert_data = True
    transfer.data_types_verts = {"VGROUP_WEIGHTS"}
    transfer.vert_mapping = "POLYINTERP_NEAREST"
    transfer.layers_vgroup_select_src = "ALL"
    transfer.layers_vgroup_select_dst = "NAME"
    bpy.ops.object.select_all(action="DESELECT")
    body.select_set(True)
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.modifier_apply(modifier=transfer.name)

    bpy.ops.object.vertex_group_limit_total(limit=4)
    bpy.ops.object.vertex_group_normalize_all(lock_active=False)

    tree = KDTree(len(proxy.data.vertices))
    for vertex in proxy.data.vertices:
        world_position = proxy.matrix_world @ vertex.co
        tree.insert(world_position, vertex.index)
    tree.balance()

    distances = []
    maximum_influences = 0
    unweighted = 0
    for vertex in body.data.vertices:
        world_position = body.matrix_world @ vertex.co
        _nearest, _proxy_index, distance = tree.find(world_position)
        distances.append(distance)
        influences = [item for item in vertex.groups if item.weight > 0.000001]
        if not influences:
            unweighted += 1
            continue
        maximum_influences = max(maximum_influences, len(influences))

    distances.sort()
    percentile_index = min(len(distances) - 1, int(len(distances) * 0.95))
    return {
        "bodyVertices": len(body.data.vertices),
        "proxyVertices": len(proxy.data.vertices),
        "unweightedVertices": unweighted,
        "maximumInfluences": maximum_influences,
        "mapping": "nearest proxy face with barycentric weight interpolation",
        "meanNearestDistanceMeters": sum(distances) / len(distances),
        "p95NearestDistanceMeters": distances[percentile_index],
        "maximumNearestDistanceMeters": distances[-1],
    }


def parent_to_rig(body, rig):
    for modifier in list(body.modifiers):
        if modifier.type == "ARMATURE":
            body.modifiers.remove(modifier)
    modifier = body.modifiers.new(name="Kael Armature", type="ARMATURE")
    modifier.object = rig
    modifier.use_deform_preserve_volume = False
    world_matrix = body.matrix_world.copy()
    body.parent = rig
    body.matrix_world = world_matrix


def import_rig():
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(
        filepath=str(MIXAMO / "kael-v1-rigged-tpose.fbx"),
        use_anim=True,
        automatic_bone_orientation=False,
    )
    imported = set(bpy.data.objects) - before
    rig = next(obj for obj in imported if obj.type == "ARMATURE")
    proxy = next(obj for obj in imported if obj.type == "MESH")
    rig.name = "Kael.Rig"
    proxy.name = "Kael.RigProxy.Donor"
    rig.animation_data_clear()
    return rig, proxy


def retarget_clip(rig, clip_name, source_path, description):
    scene = bpy.context.scene
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(
        filepath=str(source_path),
        use_anim=True,
        automatic_bone_orientation=False,
    )
    imported = set(bpy.data.objects) - before
    source = next(obj for obj in imported if obj.type == "ARMATURE")
    source_action = source.animation_data.action
    start, end = map(int, source_action.frame_range)
    conversion = rig.matrix_world.inverted() @ source.matrix_world

    source_names = {bone.name for bone in source.pose.bones}
    rig_names = {bone.name for bone in rig.pose.bones if bone.name.startswith("mixamorig:")}
    missing = sorted(rig_names - source_names)
    if missing:
        raise RuntimeError(
            f"{clip_name} is missing destination bones: {', '.join(missing)}"
        )

    rig.animation_data_create()
    action = bpy.data.actions.new(clip_name)
    action.use_fake_user = True
    rig.animation_data.action = action
    maximum_error = 0.0

    for source_frame in range(start, end + 1):
        destination_frame = source_frame - start + 1
        scene.frame_set(source_frame)
        bpy.context.view_layer.update()
        desired = {}
        for pose_bone in source.pose.bones:
            matrix = conversion @ pose_bone.matrix
            desired[pose_bone.name] = Matrix.LocRotScale(
                matrix.translation,
                matrix.to_quaternion(),
                Vector((1.0, 1.0, 1.0)),
            )

        for pose_bone in rig.pose.bones:
            if pose_bone.name not in rig_names:
                continue
            conversion_arguments = {}
            if pose_bone.parent:
                conversion_arguments = {
                    "parent_matrix": desired[pose_bone.parent.name],
                    "parent_matrix_local": pose_bone.parent.bone.matrix_local,
                }
            pose_bone.matrix_basis = pose_bone.bone.convert_local_to_pose(
                desired[pose_bone.name],
                pose_bone.bone.matrix_local,
                invert=True,
                **conversion_arguments,
            )
            pose_bone.rotation_mode = "QUATERNION"
            for property_name in ("location", "rotation_quaternion", "scale"):
                pose_bone.keyframe_insert(
                    data_path=property_name,
                    frame=destination_frame,
                )

        bpy.context.view_layer.update()
        frame_error = max(
            (
                pose_bone.matrix.translation
                - desired[pose_bone.name].translation
            ).length
            for pose_bone in rig.pose.bones
            if pose_bone.name in rig_names
        )
        maximum_error = max(maximum_error, frame_error)

    result = {
        "clip": clip_name,
        "source": source_path.name,
        "mixamoDescription": description,
        "samples": end - start + 1,
        "maximumJointErrorMeters": maximum_error,
    }
    if maximum_error >= 0.00001:
        raise RuntimeError(f"Retarget error exceeds tolerance: {result}")

    for obj in imported:
        bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.actions.remove(source_action)
    print("KAEL_CLIP_READY " + json.dumps(result), flush=True)
    return result


def main():
    bpy.ops.wm.open_mainfile(
        filepath=str(BLENDER / "kael-v1-body-reduced.blend")
    )
    scene = bpy.context.scene
    scene.render.fps = 30
    body = bpy.data.objects["Kael.Body"]
    rig, proxy = import_rig()
    apply_rig_transforms(rig, proxy)

    for action in list(bpy.data.actions):
        bpy.data.actions.remove(action)

    weight_report = transfer_proxy_weights(proxy, body)
    if weight_report["unweightedVertices"] != 0:
        raise RuntimeError(f"Unweighted body vertices: {weight_report}")
    parent_to_rig(body, rig)

    proxy.hide_render = True
    proxy.hide_set(True)
    proxy.display_type = "WIRE"

    animation_report = []
    for clip_name, (filename, description) in CLIPS.items():
        animation_report.append(
            retarget_clip(rig, clip_name, MIXAMO / filename, description)
        )

    rig.animation_data.action = bpy.data.actions["Idle"]
    rig.animation_data.action_slot = bpy.data.actions["Idle"].slots[0]
    scene.frame_set(1)
    scene.frame_start = 1
    scene.frame_end = int(bpy.data.actions["Idle"].frame_range[1])

    report = {
        "rigSource": "Mixamo auto-rigged Kael body",
        "skeletonPreset": "2 Chain Fingers (41 bones)",
        "weightTransfer": weight_report,
        "animations": animation_report,
    }
    (SOURCE / "animation-assembly.json").write_text(
        json.dumps(report, indent=2), encoding="utf-8", newline="\n"
    )
    bpy.ops.wm.save_as_mainfile(
        filepath=str(BLENDER / "kael-v1-animation-assembled.blend"),
        compress=True,
    )
    print("KAEL_ANIMATION_ASSEMBLED", flush=True)


if __name__ == "__main__":
    main()
