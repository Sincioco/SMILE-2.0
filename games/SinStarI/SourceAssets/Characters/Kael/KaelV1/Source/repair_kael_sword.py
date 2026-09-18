"""Thin the blade and absorb the unwanted depth spikes without changing its front outline."""

import json
import math
import sys
from pathlib import Path

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
import prepare_kael as preparation


def main():
    bpy.ops.wm.open_mainfile(
        filepath=str(preparation.BLENDER / "kael-v1-sword-reduced.blend"))
    sword = bpy.data.objects["Kael.Sword"]
    blade = [v for v in sword.data.vertices if v.co.z < 0.60]
    before = max(v.co.y for v in blade) - min(v.co.y for v in blade)
    maximum_front_change = 0.0
    for vertex in sword.data.vertices:
        x, depth, z = vertex.co
        if z >= 0.77:
            continue
        # A continuous monotonic compression leaves UVs and topology intact.
        # The asymptotic limit folds the unwanted front/back spikes into the
        # blade surface instead of leaving smaller versions of those spikes.
        half = 0.0025 * min(1.0, max(0.12, (z + 0.001) / 0.10))
        thin_depth = half * (2.0 / math.pi) * math.atan(depth / 0.003)
        transition = min(1.0, max(0.0, (z - 0.60) / 0.17))
        transition = transition * transition * (3.0 - 2.0 * transition)
        vertex.co.y = thin_depth * (1.0 - transition) + depth * transition
        maximum_front_change = max(maximum_front_change,
                                   abs(vertex.co.x - x), abs(vertex.co.z - z))
    preparation.select(sword)
    bpy.ops.mesh.customdata_custom_splitnormals_clear()
    for polygon in sword.data.polygons:
        polygon.use_smooth = True
    sword.data.update()
    after = max(v.co.y for v in blade) - min(v.co.y for v in blade)
    report = {"bladeRegionMaximumZ": 0.60, "blendIntoGuardEndsAtZ": 0.77,
              "sourceBladeDepthMeters": before, "repairedBladeDepthMeters": after,
              "maximumFrontSilhouetteChangeMeters": maximum_front_change,
              "method": "Continuous depth compression; unchanged front coordinates and UVs",
              "originalPreserved": "Source/kael-v1-sword.original.glb"}
    (preparation.SOURCE / "sword-repair.json").write_text(
        json.dumps(report, indent=2) + "\n", encoding="utf-8", newline="\n")
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(
        filepath=str(preparation.BLENDER / "kael-v1-sword-repaired.blend"), compress=True)
    preparation.preview(sword, "repaired-sword")
    print("KAEL_SWORD_REPAIRED " + json.dumps(report), flush=True)


if __name__ == "__main__":
    main()
