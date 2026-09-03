"""Audit v5.7 sword and shield body contacts for every approved action."""

from __future__ import annotations

import json
import math

import bpy
from mathutils.bvhtree import BVHTree


EQUIPMENT = {
    "ArinSword",
    "ArinShield",
    "ArinShieldStrapMain",
    "ArinShieldStrap2",
}
RIGHT_HAND_PART = "tripo_part_5"
RIGHT_FOREARM_PART = "tripo_part_7"
LEFT_HAND_PART = "tripo_part_6"


def base_name(name: str) -> str:
    pieces = name.rsplit(".", 1)
    return pieces[0] if len(pieces) == 2 and pieces[1].isdigit() else name


def world_bvh(obj: bpy.types.Object, depsgraph: bpy.types.Depsgraph) -> BVHTree:
    evaluated = obj.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    try:
        vertices = [evaluated.matrix_world @ vertex.co for vertex in mesh.vertices]
        polygons = [tuple(polygon.vertices) for polygon in mesh.polygons]
        return BVHTree.FromPolygons(
            vertices,
            polygons,
            all_triangles=False,
            epsilon=0.00005,
        )
    finally:
        evaluated.to_mesh_clear()


def audit_action(
    armature: bpy.types.Object,
    action: bpy.types.Action,
    equipment_name: str,
    excluded_parts: set[str],
) -> dict:
    scene = bpy.context.scene
    depsgraph = bpy.context.evaluated_depsgraph_get()
    equipment = bpy.data.objects[equipment_name]
    body_objects = [
        obj
        for obj in scene.objects
        if obj.type == "MESH"
        and obj.name not in EQUIPMENT
        and base_name(obj.name) not in excluded_parts
    ]
    armature.animation_data.action = action
    armature.animation_data.action_slot = action.slots[0]
    start = int(math.floor(action.frame_range[0]))
    end = int(math.ceil(action.frame_range[1]))
    contact_frames = []
    contacts_by_object = {}
    maximum_triangles_by_object = {}
    for frame in range(start, end + 1):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        equipment_bvh = world_bvh(equipment, depsgraph)
        frame_contacts = []
        for body in body_objects:
            overlaps = equipment_bvh.overlap(world_bvh(body, depsgraph))
            if not overlaps:
                continue
            count = len(overlaps)
            frame_contacts.append({"object": body.name, "triangles": count})
            contacts_by_object[body.name] = contacts_by_object.get(body.name, 0) + 1
            maximum_triangles_by_object[body.name] = max(
                maximum_triangles_by_object.get(body.name, 0),
                count,
            )
        if frame_contacts:
            contact_frames.append({"frame": frame, "contacts": frame_contacts})
    return {
        "contactFrameCount": len(contact_frames),
        "contactsByObject": contacts_by_object,
        "firstContactFrames": contact_frames[:12],
        "frameRange": [start, end],
        "maximumTrianglesByObject": maximum_triangles_by_object,
        "testedFrames": end - start + 1,
    }


armature = bpy.data.objects["ArinRig"]
action_names = sorted(action.name for action in bpy.data.actions)
if not action_names:
    raise RuntimeError("Arin v5.7 checkpoint contains no animation actions.")

results = {}
for action_name in action_names:
    action = bpy.data.actions[action_name]
    results[action_name] = {
        "shield": audit_action(
            armature,
            action,
            "ArinShield",
            {LEFT_HAND_PART},
        ),
        "swordCritical": audit_action(
            armature,
            action,
            "ArinSword",
            {RIGHT_HAND_PART, RIGHT_FOREARM_PART},
        ),
        "swordWithAdjacentForearm": audit_action(
            armature,
            action,
            "ArinSword",
            {RIGHT_HAND_PART},
        ),
    }

armature.animation_data.action = bpy.data.actions["Idle"]
armature.animation_data.action_slot = bpy.data.actions["Idle"].slots[0]
bpy.context.scene.frame_set(1)
print("ARIN_V57_ANIMATION_AUDIT=" + json.dumps(results, sort_keys=True))
print(
    "ARIN_V57_ANIMATION_AUDIT_SUMMARY="
    + json.dumps(
        {
            action_name: {
                "frames": result["shield"]["testedFrames"],
                "shieldContacts": result["shield"]["contactFrameCount"],
                "swordCriticalContacts": result["swordCritical"]["contactFrameCount"],
            }
            for action_name, result in results.items()
        },
        sort_keys=True,
    )
)
