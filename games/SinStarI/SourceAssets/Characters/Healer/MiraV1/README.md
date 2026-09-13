# Mira1 — selected healer, texture and cape repair

Sin selected **Mira1** on September 13, 2026 after comparing the Pixal3D, TRELLIS.2
and Hunyuan3D-2.1 candidates. Mira1 now supplies the healer in **Party Dragon** and
**Party Vrax**, as well as her own native Viewer tab. Mira2 and Mira3 remain available
as independent comparisons. No Sin Star I game code or health rules were changed.

Mira1 was generated locally with **Pixal3D Multi-View BF16**. This cleanup uses the
existing mesh and original supplied artwork in Blender; no new AI model generation
or image upscale was used. Her face, hands, hair and authored cape artwork are retained.

## Current package

- Equipped model: **19,738 triangles** (body 18,326; removable staff 1,412).
- Body and staff are closed: zero boundary/non-manifold edges and degenerate faces.
- Body base color: **4096 × 4096**. Staff: a 64 × 16 four-color material palette.
- Nine clips: Idle, Walk, Run, Attack, Defend, HealOne, HealParty, Hit and Death.
- Mira1's own Mixamo rig plus one cape hinge: 26 bones, at most four skin influences.
- Seven existing sockets; body is part 0, staff is part 1. **Weapon/W** toggles the staff.
- The current GLB, SM3D descriptor, rigged Blender source, original exports,
  animation sources, references, audio, authoring scripts and measurements live here.

Open `Blender/mira1-rigged-animation-checkpoint.blend` for the current repair.
`Blender/mira1-selected-comparison-source.blend` preserves the exact original
comparison, including its original appearance and animations. Its SHA-256 is
`a898fe163fd9a4c357f50b9941e83d1828656f97b791496d367115927fd9a6b6`.
The original generated GLB remains archived in `Source/mira-v1-pixal3d-original.zip`.

## What changed

The previous 4K atlas contained black comb-like streaks and mismatched image
projections. A new UV layout and direct reference bake remove the damaged atlas
fallback. Projection uses the original reference camera scale, pads small silhouette
mismatches, and avoids projecting a lowered reference hand onto hidden skirt cloth.
The source turnaround is only 1692 × 929 across four views: 4K describes the output
atlas dimensions, not four thousand pixels of original detail per character view.

Coincident skin seams now share normalized four-weight skinning. One demonstrated
seam had separated by 3.55 mm during Attack. Small costume surface ridges were relaxed
by at most 5 mm while preserving the face, hands, cape and grip geometry.

The closed waist cape now extends beneath the belt and blends into nearby waist
weights. A baked hinge clears the floor independently of foot placement. The original
oval staff was reconstructed from its own authored dimensions as closed primitives.
Attack and HealOne cast with the free left hand; right-arm poses keep the staff upright
in living movement/cast clips. Death settles the staff beside the character.

Grounding is measured from Mira1's own bind body, then each clip by name. The shared
correction is approximately 1.69 mm after costume relaxation. Idle and Walk maintain
contact; positive Run airtime is retained. Cape or equipment clearance does not lift
the entire character. No Arin/Orin numeric correction or calibration bank is reused.

## Viewer behavior and validation

Both battles cycle Water Attack, Heal One (Arin), and Heal Party. Water VFX use final
actor transforms and the free left hand; the same effects are available on Mira1's
individual tab. Original water/chime cues and their standard-library generator are
included under Audio and Source. The Viewer previews restoration using its existing
presentation states; this does not implement game combat or healing rules.

The native cooker/build and launch passed. Source validation checked **559 frames**
across all nine clips, including every frame zero and settled Death, with no body or
staff penetration beyond 0.0001 m tolerance. Bind and Idle floor minima agree within
0.000001 m. UV tangents are valid and both meshes meet the closed-surface budget.
The exact GLB checksum, sampled contacts and full-clip minima are recorded in
`Source/mira1-grounding-checkpoint.json`.

The native hardening gate passed, including 58 graphics/pointer/audio checks. The
real-asset regression fixture passed all three Mira tabs, standalone water ownership,
the selected Mira1 in both battles, all three cast types, recipient cleanup, healing
without boss damage, and Beat Preview restoration. Arin's 24 saved keys and Orin's
empty bank round-tripped unchanged in isolated storage.

**Native visual inspection remains pending.** Blender inspection showed substantially
cleaner shoulder/skirt texturing and the attached cape during HealParty. The final
Viewer build launched, but Computer Use retained an Escape stop before inspection.
Next: inspect Mira1 front/side/back close-ups, Attack/HealOne/HealParty, Walk/Run,
staff hide/show, settled Death, and visible water effects in both Party scenes.
Assess front-garment stretching and fast cape-angle changes during those clips;
correct visible defects before final visual acceptance.

## Remaining appearance limits

Fine belt accessories and costume ornament remain soft and simplified. The selected
AI mesh still has coarse folds; extreme leg poses stretch parts of its original
front garment. The cape is a baked hinge, not simulated cloth; fast Hit/Death motion
can change its angle quickly. These are asset-quality limits, not proof of native
renderer defects. Review those areas in the pending native inspection before calling
this a final visual acceptance. The original comparison can always be reopened.

## Reproduction

Read `MIRA-CREATION-AND-REPAIR-JOURNEY.md`. Run Blender 5.2 in background with
`Source/retexture-mira1.py`, then `Source/repair-mira1-animation.py`, then
`Source/export-mira1.py`. The animation pass imports `Source/repair-mira1-staff.py`.
Intermediate files use `D:\AI\Mira3D\Mira1Selected`; the final export returns here.
`checksums.sha256` covers binary assets; editable source, reports and documentation
are versioned by Git. The native Viewer builds from the GLB/descriptor/audio without
Blender, AI weights, Python dependencies or downloads. Web adoption remains deferred.
