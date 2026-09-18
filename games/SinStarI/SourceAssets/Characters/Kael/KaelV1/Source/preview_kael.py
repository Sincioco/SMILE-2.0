"""Render the accepted portable checkpoint, then audit its GLB round trip."""

import json
import sys
from pathlib import Path

import bpy

SOURCE = Path(__file__).resolve().parent
PACKAGE = SOURCE.parent
sys.path.insert(0, str(SOURCE))
from prepare_kael import preview
from fit_and_ground_kael import action_at
from export_kael import bounds, CLIPS

bpy.ops.wm.open_mainfile(filepath=str(PACKAGE / "Blender/kael-v1-rigged-animation-checkpoint.blend"))
rig = bpy.data.objects["Kael.Rig"]
body = bpy.data.objects["Kael.Body"]
sword = bpy.data.objects["Kael.Sword"]
for name, frame in (("Idle",1),("EarthHurl",54),("EarthVolley",66),("EarthSlam",47)):
    action_at(rig, name, frame)
    sword.hide_render = name.startswith("Earth")
    preview(body, "accepted-" + name.lower(), () if sword.hide_render else (sword,))

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(PACKAGE / "kael-v1-animation-checkpoint.glb"))
rig = next(obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE")
body = bpy.data.objects["Kael.Body"]
sword = bpy.data.objects["Kael.Sword"]
bpy.context.scene.render.fps = 30
report = []
for name in CLIPS:
    action = next(a for a in bpy.data.actions if a.name == name or a.name.startswith(name + "_"))
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]
    for strip in rig.animation_data.nla_tracks:
        strip.mute = True
    frames = [int(action.frame_range[0])]
    if name in ("Defend", "Hit", "Death"):
        frames.append(int(action.frame_range[1]))
    for frame in frames:
        bpy.context.scene.frame_set(frame)
        bpy.context.view_layer.update()
        b, s = bounds(body), bounds(sword)
        if min(b["minimum"][2], s["minimum"][2]) < -.0002:
            raise RuntimeError((name, frame, b, s))
        report.append({"clip":name,"frame":frame,"bodyMinimumMeters":b["minimum"][2],
                       "swordMinimumMeters":s["minimum"][2]})
(SOURCE / "roundtrip-validation.json").write_text(json.dumps(report,indent=2),encoding="utf-8", newline="\n")
print("KAEL_ROUNDTRIP_VALID", flush=True)
