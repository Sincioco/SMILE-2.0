"""Build Orin v1.3 from one verified Mixamo rig and its compatible actions."""

from __future__ import annotations

import bmesh
import bpy
import hashlib
import json
import math
import numpy as np
from pathlib import Path
from mathutils import Matrix, Vector


REPOSITORY = Path(__file__).resolve().parents[1]
PACKAGE = REPOSITORY / "games/SinStarI/SourceAssets/Characters/Tank/OrinV13"
ANIMATIONS = PACKAGE / "Animations"
PREVIEWS = PACKAGE / "Previews"

IDLE_SOURCE = ANIMATIONS / "orin-v1.3-mixamo-sword-and-shield-idle-with-skin.fbx"
PRISTINE_BODY_SOURCE = PACKAGE / "orin-v1.3.original.glb"
ATTACK_SOURCES = (
    ("SwordAttack", ANIMATIONS / "orin-v1.3-mixamo-sword-and-shield-attack.fbx"),
    ("ThorAttack", ANIMATIONS / "orin-v1.3-mixamo-thor-attack.fbx"),
    ("Death", ANIMATIONS / "orin-v1.3-mixamo-death.fbx"),
    ("Defend", ANIMATIONS / "orin-v1.3-mixamo-defend.fbx"),
    ("Victory", ANIMATIONS / "orin-v1.3-mixamo-victory.fbx"),
    ("Hit", ANIMATIONS / "orin-v1.3-mixamo-hit.fbx"),
    ("Run", ANIMATIONS / "orin-v1.3-mixamo-run.fbx"),
    ("JumpAttack", ANIMATIONS / "orin-v1.3-mixamo-jump-attack.fbx"),
)
EQUIPMENT_SOURCE = PACKAGE / "orin-v1.0-equipment-source.glb"
ATTACHMENT_CORRECTIONS = {
    "Shield": Matrix((
        (0.888739824295044, -0.45841196179389954, 0.0, 0.0),
        (0.45841196179389954, 0.888739824295044, 0.0, 0.0611208975315094),
        (0.0, 0.0, 1.0, 0.0),
        (0.0, 0.0, 0.0, 1.0),
    )),
    "Weapon": Matrix((
        (0.4699461758136749, 0.8780974745750427, 0.08997533470392227, -0.25602987408638),
        (-0.8442742824554443, 0.47689563035964966, -0.24448226392269135, -0.006479349918663502),
        (-0.25758808851242065, 0.03892965987324715, 0.9654702544212341, -0.0020221127197146416),
        (0.0, 0.0, 0.0, 1.0),
    )),
}


def import_file(path: Path) -> list[bpy.types.Object]:
    before = set(bpy.data.objects)
    if path.suffix.lower() == ".fbx":
        bpy.ops.import_scene.fbx(filepath=str(path))
    else:
        bpy.ops.import_scene.gltf(filepath=str(path))
    return [obj for obj in bpy.data.objects if obj not in before]


def base_name(name: str) -> str:
    pieces = name.rsplit(".", 1)
    if len(pieces) == 2 and pieces[1].isdigit():
        return pieces[0]
    return name


def action_channelbags(action: bpy.types.Action):
    for layer in action.layers:
        for strip in layer.strips:
            for channelbag in strip.channelbags:
                yield channelbag


def assign_action(rig: bpy.types.Object, action: bpy.types.Action | None) -> None:
    rig.animation_data_create()
    rig.animation_data.action = action
    if action is not None and action.slots:
        rig.animation_data.action_slot = action.slots[0]


def clear_pose(rig: bpy.types.Object) -> None:
    for bone in rig.pose.bones:
        bone.matrix_basis = Matrix.Identity(4)
    bpy.context.view_layer.update()


def rest_signature(rig: bpy.types.Object) -> str:
    content = "\n".join(
        bone.name + ":" + ",".join(f"{value:.9g}" for row in bone.matrix_local for value in row)
        for bone in rig.data.bones
    )
    return hashlib.sha256(content.encode("utf-8")).hexdigest()


def normalize_rig(rig: bpy.types.Object, actions: list[bpy.types.Action]) -> float:
    scale = float(rig.scale.x)
    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

    if abs(scale - 1.0) > 0.000001:
        for action in actions:
            for channelbag in action_channelbags(action):
                for curve in channelbag.fcurves:
                    if curve.data_path.endswith(".location"):
                        for key in curve.keyframe_points:
                            key.co.y *= scale
                            key.handle_left.y *= scale
                            key.handle_right.y *= scale

    return scale


def remove_armature_object_channels(actions: list[bpy.types.Action]) -> None:
    for action in actions:
        for channelbag in action_channelbags(action):
            for curve in list(channelbag.fcurves):
                if not curve.data_path.startswith('pose.bones['):
                    channelbag.fcurves.remove(curve)


def clean_mesh(mesh_object: bpy.types.Object) -> None:
    edit_mesh = bmesh.new()
    edit_mesh.from_mesh(mesh_object.data)
    bmesh.ops.dissolve_degenerate(edit_mesh, dist=0.0000001, edges=list(edit_mesh.edges))
    tiny_faces = [face for face in edit_mesh.faces if face.calc_area() < 0.000001]
    if tiny_faces:
        bmesh.ops.delete(edit_mesh, geom=tiny_faces, context="FACES_ONLY")
    edit_mesh.to_mesh(mesh_object.data)
    edit_mesh.free()


def restore_pristine_body(target_parts: list[bpy.types.Object]) -> dict[str, object]:
    existing_actions = set(bpy.data.actions)
    imported = import_file(PRISTINE_BODY_SOURCE)
    source_parts = {
        base_name(obj.name): obj
        for obj in imported
        if obj.type == "MESH" and base_name(obj.name) != "Icosphere"
    }
    targets = {base_name(obj.name): obj for obj in target_parts}
    if set(source_parts) != set(targets):
        raise RuntimeError(
            "Pristine Tripo and Mixamo body mesh sets differ: "
            f"source={sorted(source_parts)}, target={sorted(targets)}"
        )

    maximum_delta = 0.0
    vertex_count = 0
    for name in sorted(targets):
        source = source_parts[name]
        target = targets[name]
        if len(source.data.vertices) != len(target.data.vertices):
            raise RuntimeError(f"Vertex count mismatch for {name}")
        if len(source.data.polygons) != len(target.data.polygons):
            raise RuntimeError(f"Face count mismatch for {name}")
        deltas = [
            (source.data.vertices[index].co - target.data.vertices[index].co).length
            for index in range(len(source.data.vertices))
        ]
        maximum_delta = max(maximum_delta, max(deltas, default=0.0))
        vertex_count += len(deltas)
        target.data = source.data.copy()

    if maximum_delta > 1.0e-6:
        raise RuntimeError(f"Pristine/Mixamo local geometry mismatch: {maximum_delta}")

    for obj in imported:
        bpy.data.objects.remove(obj, do_unlink=True)

    for action in list(bpy.data.actions):
        if action not in existing_actions:
            bpy.data.actions.remove(action, do_unlink=True)

    return {
        "source": PRISTINE_BODY_SOURCE.name,
        "meshes": len(target_parts),
        "vertices": vertex_count,
        "maximumLocalVertexDelta": maximum_delta,
        "policy": "Pristine Tripo geometry, UVs, and JPEG materials with Mixamo skin weights",
    }


def source_hash(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def fitted_equipment(
    rig: bpy.types.Object,
    animated_hands: dict[str, Matrix],
    rest_hands: dict[str, Matrix],
) -> tuple[list[bpy.types.Object], dict[str, Vector]]:
    equipment_objects = import_file(EQUIPMENT_SOURCE)
    equipment_rig = next(obj for obj in equipment_objects if obj.type == "ARMATURE")
    assign_action(equipment_rig, None)
    clear_pose(equipment_rig)

    fitted = []
    sockets = {}
    settings = (
        ("Shield", "L_Hand", "00_Shield"),
        ("Weapon", "R_Hand", "01_Weapon"),
    )

    for source_name, bone_name, output_name in settings:
        obj = next(item for item in equipment_objects if item.name == source_name)
        animated_hand = animated_hands[bone_name]
        rest_hand = rest_hands[bone_name]
        target = animated_hand @ Vector((0.0, rig.data.bones[bone_name].length * 0.3, 0.0))
        points = np.array([tuple(obj.matrix_world @ vertex.co) for vertex in obj.data.vertices])

        if source_name == "Weapon":
            target += Vector((0.0, -0.04, -0.10))
            centered = points - points.mean(0)
            _, axes = np.linalg.eigh(centered.T @ centered)
            axis = axes[:, -1]
            projections = centered @ axis
            radial = np.linalg.norm(centered - np.outer(projections, axis), axis=1)
            low_radius = radial[projections < np.percentile(projections, 15)].mean()
            high_radius = radial[projections > np.percentile(projections, 85)].mean()
            butt_at_low_end = low_radius < high_radius
            butt_index = np.argmin(projections) if butt_at_low_end else np.argmax(projections)
            butt = Vector(tuple(points[butt_index]))
            butt_to_head = Vector(tuple(axis if butt_at_low_end else -axis)).normalized()
            desired_direction = Vector((0.0, -0.22, 1.0)).normalized()
            rotation = butt_to_head.rotation_difference(desired_direction)
            animated_fit = (
                Matrix.Translation(target)
                @ rotation.to_matrix().to_4x4()
                @ Matrix.Translation(-butt)
                @ obj.matrix_world
            )
            hammer_length = float(projections.max() - projections.min())
            animated_tip = target + desired_direction * hammer_length
            animated_fit = ATTACHMENT_CORRECTIONS[source_name] @ animated_fit
            target = ATTACHMENT_CORRECTIONS[source_name] @ target
            animated_tip = ATTACHMENT_CORRECTIONS[source_name] @ animated_tip
            sockets["SwordBase"] = rest_hand @ animated_hand.inverted() @ target
            sockets["SwordTip"] = rest_hand @ animated_hand.inverted() @ animated_tip
        else:
            center = Vector(tuple((points.min(0) + points.max(0)) / 2.0))
            shield_target = target + Vector((0.0, -0.065, 0.0))
            flare = Matrix.Rotation(math.radians(-40.0), 4, "Z")
            animated_fit = (
                Matrix.Translation(shield_target)
                @ flare
                @ Matrix.Translation(-center)
                @ obj.matrix_world
            )
            animated_fit = ATTACHMENT_CORRECTIONS[source_name] @ animated_fit
            shield_target = ATTACHMENT_CORRECTIONS[source_name] @ shield_target
            sockets["ShieldCenter"] = rest_hand @ animated_hand.inverted() @ shield_target

        rest_fit = rest_hand @ animated_hand.inverted() @ animated_fit
        obj.data.transform(rest_fit)
        obj.matrix_world = Matrix.Identity(4)
        obj.parent = rig
        obj.matrix_parent_inverse = rig.matrix_world.inverted()
        obj.modifiers.clear()
        obj.vertex_groups.clear()
        obj.vertex_groups.new(name=bone_name).add(
            list(range(len(obj.data.vertices))), 1.0, "REPLACE"
        )
        obj.modifiers.new("Rigid Hand Attachment", "ARMATURE").object = rig
        obj.name = output_name

        material = obj.data.materials[0].copy()
        material.name = "Orin" + source_name
        obj.data.materials.clear()
        obj.data.materials.append(material)
        fitted.append(obj)

    for obj in equipment_objects:
        if obj not in fitted:
            bpy.data.objects.remove(obj, do_unlink=True)

    return fitted, sockets


def point_in_bone(rest_matrix: Matrix, point: Vector) -> list[float]:
    return [round(value, 8) for value in (rest_matrix.inverted() @ point)]


def look_at(obj: bpy.types.Object, target: Vector) -> None:
    obj.rotation_euler = (target - obj.location).to_track_quat("-Z", "Y").to_euler()


def render_previews(
    scene: bpy.types.Scene,
    rig: bpy.types.Object,
    body: bpy.types.Object,
    equipment: list[bpy.types.Object],
    actions: list[bpy.types.Action],
) -> None:
    preview_camera_data = bpy.data.cameras.new("Preview Camera")
    preview_camera = bpy.data.objects.new("Preview Camera", preview_camera_data)
    scene.collection.objects.link(preview_camera)
    scene.camera = preview_camera
    preview_camera_data.type = "ORTHO"
    preview_camera_data.ortho_scale = 2.45
    preview_camera.location = (0.0, -5.0, 1.05)
    look_at(preview_camera, Vector((0.0, 0.0, 0.9)))

    world = bpy.data.worlds.new("Orin Preview World")
    world.color = (0.16, 0.16, 0.16)
    scene.world = world
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 900
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False

    for location, energy, size in (
        ((2.5, -3.5, 4.0), 850.0, 3.0),
        ((-3.0, -1.5, 2.0), 500.0, 2.5),
        ((0.0, 2.0, 3.0), 650.0, 2.0),
    ):
        light_data = bpy.data.lights.new("Preview Light", "AREA")
        light_data.energy = energy
        light_data.shape = "DISK"
        light_data.size = size
        light = bpy.data.objects.new("Preview Light", light_data)
        scene.collection.objects.link(light)
        light.location = location
        look_at(light, Vector((0.0, 0.0, 0.9)))

    PREVIEWS.mkdir(parents=True, exist_ok=True)
    preview_frames = {
        "Idle": 1,
        "SwordAttack": 25,
        "ThorAttack": 29,
        "Death": 54,
        "Defend": 20,
        "Victory": 45,
        "Hit": 15,
        "Run": 12,
        "JumpAttack": 28,
    }
    for action in actions:
        assign_action(rig, action)
        scene.frame_set(preview_frames[action.name])
        bpy.context.view_layer.update()
        scene.render.filepath = str(PREVIEWS / f"{action.name}.png")
        bpy.ops.render.render(write_still=True)

    for obj in list(scene.objects):
        if obj not in [rig, body, *equipment]:
            bpy.data.objects.remove(obj, do_unlink=True)


def build() -> None:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    imported = import_file(IDLE_SOURCE)
    rig = next(obj for obj in imported if obj.type == "ARMATURE")
    rig.name = "Orin"
    body_parts = [obj for obj in imported if obj.type == "MESH"]
    idle = rig.animation_data.action
    idle.name = "Idle"
    idle.use_fake_user = True
    actions = [idle]
    expected_rest = rest_signature(rig)

    for action_name, source_path in ATTACK_SOURCES:
        action_objects = import_file(source_path)
        action_rig = next(obj for obj in action_objects if obj.type == "ARMATURE")
        if rest_signature(action_rig) != expected_rest:
            raise RuntimeError(f"{source_path.name} does not match the Orin v1.3 Mixamo rest rig")
        source_action = action_rig.animation_data.action
        action = source_action.copy()
        action.name = action_name
        action.use_fake_user = True
        actions.append(action)
        source_action.use_fake_user = False
        for obj in action_objects:
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.actions.remove(source_action, do_unlink=True)

    remove_armature_object_channels(actions)
    normalized_scale = normalize_rig(rig, actions)
    body_transfer = restore_pristine_body(body_parts)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in body_parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = body_parts[0]
    bpy.ops.object.join()
    body = bpy.context.object
    body.name = "02_Body"

    scene = bpy.context.scene
    scene.render.fps = 30
    scene.render.fps_base = 1
    assign_action(rig, idle)
    scene.frame_set(1)
    bpy.context.view_layer.update()
    hand_names = ("L_Hand", "R_Hand")
    animated_hands = {
        name: (rig.matrix_world @ rig.pose.bones[name].matrix).copy() for name in hand_names
    }

    assign_action(rig, None)
    clear_pose(rig)
    rest_hands = {
        name: (rig.matrix_world @ rig.data.bones[name].matrix_local).copy() for name in hand_names
    }
    equipment, equipment_sockets = fitted_equipment(rig, animated_hands, rest_hands)

    for obj in [body, *equipment]:
        clean_mesh(obj)

    assign_action(rig, idle)
    scene.frame_start = 1
    scene.frame_end = max(math.ceil(action.frame_range[1]) for action in actions)
    scene.frame_set(1)

    render_previews(scene, rig, body, equipment, actions)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in [rig, body, *equipment]:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = rig
    output_path = PACKAGE / "orin-v1.3-animation-checkpoint.glb"
    output_path.unlink(missing_ok=True)
    bpy.ops.export_scene.gltf(
        filepath=str(output_path),
        export_format="GLB",
        use_selection=True,
        export_materials="EXPORT",
        export_animations=True,
        export_animation_mode="ACTIONS",
        export_merge_animation="ACTION",
        export_anim_single_armature=True,
        export_armature_object_remove=True,
        export_rest_position_armature=True,
        export_reset_pose_bones=False,
        export_optimize_animation_keep_anim_armature=False,
        export_extra_animations=False,
        export_skins=True,
    )

    sockets = {
        "Root": {"node": "Hip"},
        "Head": {"node": "Head"},
        "Chest": {"node": "Spine02"},
        "HandRight": {"node": "R_Hand"},
        "HandLeft": {"node": "L_Hand"},
        "FootLeft": {"node": "L_Foot"},
        "FootRight": {"node": "R_Foot"},
        "SwordBase": {
            "node": "R_Hand",
            "translation": point_in_bone(rest_hands["R_Hand"], equipment_sockets["SwordBase"]),
        },
        "SwordTip": {
            "node": "R_Hand",
            "translation": point_in_bone(rest_hands["R_Hand"], equipment_sockets["SwordTip"]),
        },
        "ShieldCenter": {
            "node": "L_Hand",
            "translation": point_in_bone(rest_hands["L_Hand"], equipment_sockets["ShieldCenter"]),
        },
    }
    descriptor = {
        "version": 1,
        "sampleRate": 30,
        "clips": {
            "Idle": {"loop": True},
            "SwordAttack": {"loop": False},
            "ThorAttack": {"loop": False},
            "Death": {"loop": False},
            "Defend": {"loop": True},
            "Victory": {"loop": False},
            "Hit": {"loop": False},
            "Run": {"loop": True},
            "JumpAttack": {"loop": False},
        },
        "sockets": sockets,
    }
    (PACKAGE / "OrinV13.sm3d.json").write_text(
        json.dumps(descriptor, indent=2) + "\n", encoding="utf-8"
    )

    blend_path = PACKAGE / "Blender/orin-v1.3-animation-working.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path), compress=True)

    report = {
        "version": "1.3",
        "bodySource": PRISTINE_BODY_SOURCE.name,
        "skinAndAnimationSource": IDLE_SOURCE.name,
        "bodyTransfer": body_transfer,
        "bodyVertices": len(body.data.vertices),
        "normalizedScale": normalized_scale,
        "restRigSha256": expected_rest.upper(),
        "animations": [
            {
                "name": action.name,
                "file": IDLE_SOURCE.name if action.name == "Idle" else next(
                    path.name for name, path in ATTACK_SOURCES if name == action.name
                ),
                "sourceSha256": source_hash(
                    IDLE_SOURCE if action.name == "Idle" else next(
                        path for name, path in ATTACK_SOURCES if name == action.name
                    )
                ),
                "frameStart": int(action.frame_range[0]),
                "frameEnd": int(action.frame_range[1]),
                "loop": action.name in ("Idle", "Defend", "Run"),
            }
            for action in actions
        ],
        "equipmentSource": EQUIPMENT_SOURCE.name,
        "equipmentPolicy": (
            "Rigid hand attachments from named v1.0 meshes; shield centered on left hand and "
            "user-approved Blender correction matrices applied after a 40-degree outward shield fit and butt-grip hammer fit"
        ),
        "footIntegrity": "Preserved by retaining the Idle (8) skin weights, bind matrices, and rig on matching pristine body topology",
    }
    (PACKAGE / "orin-v1.3-animation-set.json").write_text(
        json.dumps(report, indent=2) + "\n", encoding="utf-8"
    )
    print("ORIN_V13_CHECKPOINT_READY=" + json.dumps(report))


build()
