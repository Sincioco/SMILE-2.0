"""Verify that Arin's exported rigid hand attachments preserve Blender poses."""

from __future__ import annotations

import math
import os
import sys

import bpy


ATTACHMENTS = ("ArinSword", "ArinSwordGripGlove")
EXPECTED_BONE = "R_Hand"
POSITION_TOLERANCE = 0.0001


def command_argument() -> str:
    if "--" not in sys.argv:
        raise RuntimeError("Pass the exported Arin GLB path after --.")
    arguments = sys.argv[sys.argv.index("--") + 1 :]
    if len(arguments) != 1:
        raise RuntimeError("Expected exactly one exported Arin GLB path after --.")
    return os.path.abspath(arguments[0])


def evaluated_world_vertices(value: bpy.types.Object) -> list:
    graph = bpy.context.evaluated_depsgraph_get()
    evaluated = value.evaluated_get(graph)
    mesh = evaluated.to_mesh()
    try:
        return [evaluated.matrix_world @ vertex.co for vertex in mesh.vertices]
    finally:
        evaluated.to_mesh_clear()


def active_vertex_groups(value: bpy.types.Object) -> set[str]:
    result: set[str] = set()
    for vertex in value.data.vertices:
        for membership in vertex.groups:
            if membership.weight > 0.000001:
                result.add(value.vertex_groups[membership.group].name)
    return result


def main() -> None:
    glb_path = command_argument()
    if not os.path.isfile(glb_path):
        raise RuntimeError(f"Exported Arin GLB is missing: {glb_path}")

    source_armature = bpy.data.objects.get("ArinRig")
    if source_armature is None or source_armature.type != "ARMATURE":
        raise RuntimeError("The Blender source does not contain ArinRig.")
    source_attachments = {}
    for name in ATTACHMENTS:
        value = bpy.data.objects.get(name)
        if value is None or value.type != "MESH":
            raise RuntimeError(f"The Blender source attachment is missing: {name}")
        source_attachments[name] = value

    source_actions = list(bpy.data.actions)
    if not source_actions:
        raise RuntimeError("The Blender source contains no actions.")
    for action in source_actions:
        action.name = "SOURCE_" + action.name
    for value in list(bpy.data.objects):
        value.name = "SOURCE_" + value.name

    status = bpy.ops.import_scene.gltf(filepath=glb_path)
    if "FINISHED" not in status:
        raise RuntimeError(f"Blender failed to re-import {glb_path}: {sorted(status)}")
    imported_armatures = [
        value
        for value in bpy.context.scene.objects
        if value.type == "ARMATURE" and not value.name.startswith("SOURCE_")
    ]
    if len(imported_armatures) != 1:
        raise RuntimeError(
            f"Expected one re-imported Arin armature; found {len(imported_armatures)}."
        )
    imported_armature = imported_armatures[0]
    imported_attachments = {}
    for name in ATTACHMENTS:
        value = bpy.data.objects.get(name)
        if value is None or value.type != "MESH":
            raise RuntimeError(f"The exported attachment is missing: {name}")
        groups = active_vertex_groups(value)
        if groups != {EXPECTED_BONE}:
            raise RuntimeError(
                f"Exported {name} uses {sorted(groups)}; expected only {EXPECTED_BONE}."
            )
        imported_attachments[name] = value

    maximum_delta = 0.0
    samples = 0
    action_names = sorted(action.name.removeprefix("SOURCE_") for action in source_actions)
    for action_name in action_names:
        source_action = bpy.data.actions.get("SOURCE_" + action_name)
        imported_action = bpy.data.actions.get(action_name)
        if imported_action is None:
            raise RuntimeError(f"The exported action is missing: {action_name}")
        source_range = tuple(source_action.frame_range)
        imported_range = tuple(imported_action.frame_range)
        if any(
            abs(left - right) > 0.0001
            for left, right in zip(source_range, imported_range)
        ):
            raise RuntimeError(
                f"Action {action_name} changed range: {source_range} to {imported_range}."
            )
        source_armature.animation_data.action = source_action
        imported_armature.animation_data.action = imported_action
        first = int(math.floor(source_range[0]))
        last = int(math.ceil(source_range[1]))
        for frame in sorted({first, (first + last) // 2, last}):
            bpy.context.scene.frame_set(frame)
            bpy.context.view_layer.update()
            for name in ATTACHMENTS:
                source_vertices = evaluated_world_vertices(source_attachments[name])
                imported_vertices = evaluated_world_vertices(imported_attachments[name])
                if len(source_vertices) != len(imported_vertices):
                    raise RuntimeError(
                        f"Attachment {name} changed vertex count during round-trip."
                    )
                delta = max(
                    (left - right).length
                    for left, right in zip(source_vertices, imported_vertices)
                )
                maximum_delta = max(maximum_delta, delta)
                samples += 1
                if delta > POSITION_TOLERANCE:
                    raise RuntimeError(
                        f"Attachment {name} changed by {delta:.8f} in "
                        f"{action_name} frame {frame}; tolerance is "
                        f"{POSITION_TOLERANCE:.8f}."
                    )

    print(
        "Arin attachment round-trip passed: "
        f"{len(action_names)} actions, {samples} attachment samples, "
        f"maximum vertex delta {maximum_delta:.8f}."
    )


if __name__ == "__main__":
    main()
