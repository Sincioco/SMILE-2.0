# Mira1 — Pixal3D comparison candidate

Sin named this candidate **Mira1** on September 13, 2026 and requested two new
AI-model comparisons because its visual quality was not acceptable. Preserve it
for a possible return. It is **not an accepted Character Viewer asset**.

- Generator: Pixal3D multi-view, run locally in ComfyUI on the RTX 5090.
- Reference appearance: the supplied Mira T-pose and four-view turnaround.
- Equipped mesh: 19,801 triangles (body 18,326; removable staff 1,475).
- Closed body: zero boundary and non-manifold edges after welding exact UV seams.
- Staff and four closed grip fingers were modeled in Blender.
- Rear cape was repaired with a closed, reference-textured cloth panel.
- Rig: Mixamo 25-bone skeleton, original-pose bind geometry; grip vertices and
  staff share the RightHand transform.
- Nine animation clips are preserved. Idle is a gentle original-pose loop;
  remaining clips derive from the included Mixamo sources.

Open `Blender/mira1-rigged-animation-checkpoint.blend` to resume the current work.
`mira1-animation-checkpoint.glb` is the normalized, cooked native Viewer comparison
asset. The **Mira1** tab loads nine clips and seven sockets. The Weapon button
and W key independently hide/show the staff. Final grounding is not accepted.
`Blender/mira-v1-unrigged-source.blend` preserves the unrigged repaired model.
`Source/mira-v1-pixal3d-original.zip` holds the complete original generated GLB.
The folder name MiraV1 is retained as the canonical versioned package location.

## Unfinished work and known limitations

- Sin rejected the visual quality; compare Mira2 and Mira3 before choosing an asset.
- The generated lower garment can distort in wide stances. The new cape panel
  uses pelvis weights, but the original generated cloth still needs review.
- Several clips penetrate the floor; frame-zero and full-clip grounding remain
  unfinished. Staff clearance also remains unfinished in Death.
- Water VFX/audio and both Party battle integrations remain unfinished. Sin requested
  immediate comparison access while Mira2/TRELLIS.2 and Mira3/Hunyuan3D-2.1 are evaluated.
- No Sin Star I game code was changed. Native Viewer profile, tab routing and asset
  staging use the existing owners; Arin/Orin calibration banks remain independent.

## Bind-pose transfer lesson

Mixamo's animation-only FBXs use a T-pose rest skeleton, whereas this character's
With Skin and Original Pose exports use the uploaded pose as their bind skeleton.
Bone-name equality does not imply equal rest matrices. Direct action assignment
was rejected after it visibly distorted the character. The saved candidate now
bakes absolute animated bone matrices into the original-pose rig with
`Bone.convert_local_to_pose(..., invert=True)`. A matching With Skin heal export
confirmed the same animated global joints; all eight transferred clips reproduce
source joint positions within 0.000007 meters. The transfer report is in Source.
`Source/mira1-grounding-checkpoint.json` records the current asset checksum, bind
minimum and first/middle/final samples of all nine clips, excluding equipment.
Bind and Idle frame zero agree at the floor within 0.000001 m. Later contact errors
are therefore clip/skin issues, not a shared character placement offset. Defend
ends 0.106 m below the floor and Death ends 0.156 m below it. HealParty's midpoint
reaches -0.234 m. Repair cloth weights and clip contacts before final acceptance;
do not copy another character's offsets or present this as a finished healer.

## Native comparison validation, September 13, 2026

- `Launch.ps1 -Build -SkipWindowActivation` cooked the model and launched the new Viewer.
- `test-character-3d-viewer-hardening.ps1 -NativeOnly` passed, including expanded tab
  hit routing and 58 native graphics, pointer and audio-focus checks.
- Visible inspection confirmed Mira1's tab, Idle/Attack/HealParty playback and
  staff removal/restoration. The model is 19,801 triangles; the scene counter also
  includes 46 environment triangles.
- Unit normalization preserved sampled vertices within 0.00000635 m. Converting
  the staff to the common skin preserved sampled staff vertices within 0.00000075 m.
- Zero-area UV islands on 1,475 staff triangles and 320 body triangles were repaired
  around their existing palette samples. SM3D cooking then passed.
- Web candidate publication is explicitly disabled pending a later milestone.
