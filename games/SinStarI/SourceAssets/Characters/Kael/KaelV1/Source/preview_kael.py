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

water = '--water-preview' in sys.argv
checkpoint = "Blender/kael-v1-water-preview.blend" if water else "Blender/kael-v1-rigged-animation-checkpoint.blend"
model = "kael-v1-water-preview.glb" if water else "kael-v1-animation-checkpoint.glb"
clip_names = CLIPS + (("WaterWhip", "WaterOrbit", "WaterSurge") if water else ())
poses = (("WaterWhip",57),("WaterOrbit",76),("WaterSurge",58)) if water else (
    ("Idle",1),("EarthHurl",54),("EarthVolley",66),("EarthSlam",47))
fire = '--fire-preview' in sys.argv
if fire:
    checkpoint = "Blender/kael-v1-fire-preview.blend"
    model = "kael-v1-fire-preview.glb"
    clip_names = CLIPS + ("WaterWhip", "WaterOrbit", "WaterSurge", "FirePunch", "FireSweep", "FireBlast")
    poses = (("FirePunch",43),("FireSweep",64),("FireBlast",81))
bpy.ops.wm.open_mainfile(filepath=str(PACKAGE / checkpoint))
rig = bpy.data.objects["Kael.Rig"]
body = bpy.data.objects["Kael.Body"]
sword = bpy.data.objects["Kael.Sword"]
hair = bpy.data.objects.get("Kael.ZHair")
for name, frame in poses:
    action_at(rig, name, frame)
    sword.hide_render = name.startswith(("Earth", "Water", "Fire"))
    equipment=(() if sword.hide_render else (sword,)) + ((hair,) if hair else ())
    preview(body, ("preview-" if water or fire else "accepted-") + name.lower(), equipment)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(PACKAGE / model))
rig = next(obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE")
body = bpy.data.objects["Kael.Body"]
sword = bpy.data.objects["Kael.Sword"]
bpy.context.scene.render.fps = 30
report = []
for name in clip_names:
    action = next(a for a in bpy.data.actions if a.name == name or a.name.startswith(name + "_"))
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]
    for strip in rig.animation_data.nla_tracks:
        strip.mute = True
    frames = [int(action.frame_range[0])]
    if name in ("Defend", "Hit", "Death"):
        frames.append(int(action.frame_range[1]))
    elif name.startswith(("Water", "Fire")):
        frames += [int(sum(action.frame_range)/2), int(action.frame_range[1])]
    for frame in frames:
        bpy.context.scene.frame_set(frame)
        bpy.context.view_layer.update()
        b, s = bounds(body), bounds(sword)
        if min(b["minimum"][2], s["minimum"][2]) < -.0002:
            raise RuntimeError((name, frame, b, s))
        report.append({"clip":name,"frame":frame,"bodyMinimumMeters":b["minimum"][2],
                       "swordMinimumMeters":s["minimum"][2]})
(SOURCE / ("fire-roundtrip-validation.json" if fire else "water-roundtrip-validation.json" if water else "roundtrip-validation.json")).write_text(
    json.dumps(report,indent=2),encoding="utf-8", newline="\n")
print("KAEL_ROUNDTRIP_VALID", flush=True)
