# Orin v1.3 Character Package

Orin is Sin Star I's second implemented party member and Tank. His concept art is
`Sin Star - Character 3 - Tank - Purple Background.png`. His stable identity is
`sin-star-i.character-2.tank`. The later lightning handoff's proposed `orin`
alias maps to this identity; it does not replace it.

## Current Source Contract

- `orin-v1.3.original.glb` is the untouched Tripo T-pose with 41-bone skin and
  the 3.54-second Angry armpit-inspection clip.
- `orin-v1.2-neutral-reference.glb` preserves the same neutral rig without the
  test animation.
- `orin-v1.0-equipment-source.glb` is used only for the named `Weapon` hammer
  and `Shield`. The v1.0 body and its faulty cloth/forearm weights are not used.
- `orin-v1.3-mixamo-t-pose-with-skeleton.fbx` is the canonical Mixamo upload.
  Mixamo accepts its existing 41-bone Tripo rig directly, so every downloaded
  clip retains the exact same hierarchy and bind matrices.
- `orin-v1.3-mixamo-t-pose-no-skeleton.fbx` is retained only as the successful
  auto-rig fallback used during diagnosis.
- `orin-v1.3-animation-checkpoint.glb` is the runtime cooking input.
- `OrinV13.sm3d.json` defines animation policy and runtime sockets.
- `Calibration/orin-v1.3-profile.json` fingerprints the exact runtime package.
- `Calibration/orin-v1.3-pose-calibration.json` is the human-readable editor
  correction source once Orin keys are saved.
- `orin-v1.3-package.json` is the package manifest, and
  `ORIN-CREATION-AND-REPAIR-JOURNEY.md` records the reproducible handoff.

The body keeps v1.3 geometry, UVs, textures, and the accepted skin from the final
Mixamo Idle (8) export. Hammer and shield are rigid hand attachments with
independent materials and runtime parts. Orin grips the hammer at the butt of
its handle. The hammer rests inside his right hand, upright and aimed slightly
forward. His left hand sits at the shield's vertical midpoint; the shield has a
40-degree outward fit, followed by Sin's approved Blender translation and
rotation corrections so it does not cut through his forearm, torso, or legs.

The runtime set contains nine Mixamo clips: Idle, SwordAttack, JumpAttack,
ThorAttack, Defend, Hit, Death, Victory, and Run. `ThorAttack` is the stable
animation contract for lightning: raise the hammer, receive the charge, and
release it into the ground. The viewer adds a white hammer core, blue/white
electrical aura and an eight-point shield perimeter glow. Neither shield face
nor back receives a full-surface glow overlay.

## Known Visual Limits

Sin accepts Orin's smaller armpit openings for the current preview. The original
GLB's Angry clip exists specifically to inspect them and remains source-only
because the final viewer uses the Mixamo FBX bind matrices as one unit. Do not
close or otherwise modify the source geometry as part of animation or VFX work.
The derived checkpoint removes only runtime-invalid zero-area faces required by
the strict model cooker.

The pink/purple background in reference sheets is a transparency key. Reference
images are visual guidance and are not runtime textures.

## Viewer Behavior

The Character Viewer provides Arin, Orin, and Party tabs. Orin has independent
animation selection, playback, timeline, pose corrections, storage, sockets,
material inspection, camera controls, equipment toggles, and white equipment
Glow. Party renders Arin and Orin together, facing the Red Dragon, taking turns
advancing and attacking while the arena camera orbits.

The Orin tab doubles as the Storm Presentation Lab. The button beside Floor/Grid
cycles Thunder Smash, Storm Lance, Chain Arcs and Godstorm discharge previews.
The next row controls full/reduced/off flash and shake and displays stored charge.
Reduced is the default. Chain Arcs are arena presentation paths; this one-boss
preview does not claim multiple-enemy damage or target selection. The generic
Lightning Lab separately demonstrates caller-ordered multi-target chains.

Forked Judgment now uses the Lightning Lab's four SkyStrike recipe during the
raised-hammer charge (12–52% of ThorAttack). Thunder Smash begins at 64% and
spreads eight radial ground spokes, paired into four effects to fit the shared
battle budget. Strike effects use Ultra quality; equipment remains separately
budgeted. A GPU particle trail deposits white sparks along the moving hammer
and lets them linger in world space. The ground slam fires once per action.

`tools/Character3DViewer/OrinStorm.smile` owns presentation timing and the pure
0–1000 charge state. Contact at 35% of ThorAttack fills charge; release at 64%
spends 350 once per action. Idle and Run retain the remaining aura. Paused frame
inspection changes the visual phase without applying charge or audio events.
Glow visibility does not change charge calculations. All transforms are resolved
through the actual calibrated equipment parts after wrist, Move and Rotate edits.

The original GLB, animations, UVs, textures and approved fit are unchanged.
Eleven descriptor sockets were added: ShieldRim0–7 and HammerHead/Left/Right.
`scripts/update-orin-lightning-sockets.py` derives them from the rigid hand bind
matrices. The profile migration retained all Orin keys (zero at migration) and
left Arin's 23-key snapshot unchanged.

Arin and Orin default to playback speed 200. Individual demos target three seconds,
then finish the current animation before advancing. Block plays once and holds.

The viewer removes Orin's bind-pose-to-animated-sole height offset on every clip.
Block, Hit and Death also receive measured frame-dependent ground corrections.
`Calibration/orin-v1.3-grounding.json` records the 30 Hz skinned-body measurements;
`scripts/measure-orin-grounding.py` reproduces them without editing the GLB.
The raw body bound starts at -0.116 model units while Idle starts near +0.003,
so the shared baseline correction is -0.119. Jump/run motion is preserved above
that corrected baseline. Corrections are selected by clip name, not cooker order,
and apply in both the individual tab and Party. The hammer glow uses the final
actor position, keeping it aligned with the lowered equipment.

## Rebuild

Run `scripts/build-orin-v1-3-mixamo.py` through Blender 5.2 after replacing or
adding canonical Mixamo inputs. Then run
`tools/Character3DViewer/Build.ps1`. Use the viewer launcher for normal editor
work so Arin and Orin calibration snapshots stay synchronized.

## September 5: Death root motion repair

Orin's Mixamo Death export stores the fall's global rotation/translation on the
armature object. Removing those object channels left him upright at the last
frame; a floor-height adjustment could never correct that orientation defect.
The accepted repair bakes that motion into the Root joint, aligned to the
accepted first pose, while retaining every other clip and all geometry, skin,
textures and equipment bytes. The source FBX is unchanged.

`scripts/repair-orin-death-root.py` reproduces the surgical repair from the
pre-repair checkpoint. `Calibration/orin-v1.3-death-root-repair.json` records
source/result hashes and joint-position alignment error. The normal builder now
bakes object motion before removing object channels. Inspect the whole
fall and its final horizontal pose, not just a mid-fall screenshot.

The regenerated grounding measurement and Death contact curve replace the old
upright-pose correction. The shared -0.119 baseline remains unchanged. Death
plays once and holds its settled pose. Defend is non-looping in both descriptor
and runtime policy; Party never restarts an already-held Orin guard.

Both individual and Party paths use the final grounded transform for equipment
and VFX. Current hashes live in `Calibration/orin-v1.3-profile.json`.
