# Kael v1 — native sword, Earth and Water boss candidate

## Fire Lab preview and silver-gray hair

The [Fire Lab](../../../../../../../tools/AdvancedFireVfxLab/README.md) owns the
feedback preview for FirePunch, FireSweep and FireBlast. Its separate nineteen-clip
`kael-v1-fire-preview.glb` preserves all sixteen accepted clips. Fire adoption into
Viewer/game rotations is pending a later request; their active model stays at sixteen.

Run `Source/author_kael_fire.py` in installed Blender, then
`Source/export_kael.py -- --fire-preview` and `Source/preview_kael.py -- --fire-preview`.
The shared IK baker accepts an optional foot path; existing Earth/Water callers
are unchanged. The package owns both Fire Blender checkpoints, descriptor,
full-frame grounding measurements, GLB round-trip checks and nine pose previews.

`Source/silver_kael_hair.py` assigns a cool silver material to existing skinned hair
geometry without repainting the packed atlas. Exported parts remain body **0** and
sword **1**, with hair appended as **2**. The total body-plus-hair triangle count
stays 42,000; sword visibility and all animation names remain unchanged. The active
Water model, Earth baseline and Fire preview all receive this color update at export.

## Adopted native water model

The [Water Lab](../../../../../../../tools/WaterVfxLab/README.md) now has a Mira/Kael
toggle and three Kael water casts: WaterWhip, WaterOrbit and WaterSurge. The canonical
`kael-v1-water-preview.glb` / `KaelWaterPreview.sm3d.json` contains all sixteen
clips and is now adopted by the Viewer and Sin Star I, including solo demos and
the normal → Earth → Water Party rotation. Historical preview filenames remain
to preserve provenance; `package.json` identifies the active model.

`Source/author_kael_water.py` reuses the grounded IK baker. Run it in installed
Blender background mode, then `Source/export_kael.py -- --water-preview` and
`Source/preview_kael.py -- --water-preview`. These write only water-named checkpoints,
GLB, descriptor, measurements and `Previews/preview-water*` images. The manifest
records their paths and checksums. Every new cast has visible motion in both hands
and the body, locked horizontal root travel, and floor-safe body/equipment samples.

The canonical package for Sin's September 18, 2026 Kael request. The original body
and sword remain unchanged in `Source`. The Viewer and Sin Star I both consume
`kael-v1-water-preview.glb` with `KaelWaterPreview.sm3d.json`.
The thirteen-clip `kael-v1-animation-checkpoint.glb` remains the Earth baseline.

| Property | Runtime checkpoint |
| --- | --- |
| Body / sword triangles | 42,000 / 6,996 |
| Exported vertices: body / sword / hair | 25,505 / 7,045 / 6,762 |
| Skeleton | 41 Mixamo bones + KaelSword |
| Body / sword PBR maps | 4K / 2K |
| Animations / sockets | 16 / 10 |
| GLB bytes / checksum | See `package.json` and `Source/water-export-validation.json` |
| Solo / boss scale | 134-unit equipped bind height / exactly twice the solo scale |

The equipped bind height includes the upright sword; the body itself is about
100 world units tall at solo scale and 200 at boss scale. The blade was flattened
along its depth axis, preserving its front outline and UVs. Its measured source
blade depth went from 98.0 mm to 4.8 mm before the 72% equipment fitting scale.
The ornamental crossguard and pommel retain their intended depth.

The native Viewer exposes **Kael** and **Kael Party**. Kael Party uses Arin, Orin,
Zara and Mira against Kael, with the existing turn scheduler, healing and effects.
Sin Star I exposes **Characters → Kael** and **Battle Simulations → Kael Party**.
Kael has his own camera/head storage identity; his sword uses the normal Weapon/W
visibility control. No runtime pose-calibration bank is added for this version.

Idle now uses a mostly stationary Breathing Idle with sway disabled. The three new
Earth clips add a stomp, planted stance, ground lift and forceful two-handed casts.
The sword hides during all three Earth and all three Water casts and returns for other
clips according to the normal weapon preference. Both original sword attacks stay
in Kael Party's normal/Earth/Water rotation. The [Earth Lab](../../../../../../../tools/EarthVfxLab/README.md)
shares these clips, rocks, dust and original audio with both applications.

Read [the creation and repair journey](KAEL-CREATION-AND-REPAIR-JOURNEY.md) before
changing geometry, rigging, equipment or grounding. `Source/water-export-validation.json`
records every-frame floor checks and the model checksum; `Source/water-roundtrip-validation.json`
checks the exported GLB's clip starts and final Defend/Hit/Death poses.
`Previews/accepted-*` show the final equipped checkpoint.

Native validation passed: both Release builds, all twelve Sin Star I presentation
entries (including the exact Kael Party roster), 58 Viewer graphics/pointer/audio
checks, 42 calibration-transfer checks, 13 formatter groups and the repository style
gate. The cooked Kael SM3D files match between applications. Visible inspection
covered both native Kael tabs, sword animation, battle playback/pause and the
Sin Star I menu routes. `Source/native-validation.json` records this evidence.

Web adoption and browser validation remain on hold.
