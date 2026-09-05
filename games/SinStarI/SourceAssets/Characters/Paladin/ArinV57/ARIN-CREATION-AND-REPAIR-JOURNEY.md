# Arin: Creation, Import, Animation, and Repair Lessons

Permanent handoff for Sin and future Codex work. Recorded September 5, 2026.
Repository: `D:\SMILE 2.0`. Character identity: `sin-star-i.character-1.paladin`.

This is the consolidated account of what the Arin journey taught us, grounded
in the current source, package manifests, committed reports, and Sin's visual
feedback. It is not an instruction to rebuild or change the approved character.
Historical settings below are version-specific, not defaults for future models.
Where an old screenshot showed a symptom but did not establish its cause, this
guide says so rather than inventing a diagnosis.

## 1. Read This Before Touching Arin

- Arin v5.7's sword, shield, flames, and artist-authored poses are approved for
  the current Character Viewer. Preserve their appearance.
- The remaining confirmed model defect is open geometry revealing the hollow
  interior, notably near the armpits. Development preview is allowed; production
  asset approval and release remain blocked pending a separately reviewed repair.
- The canonical character package is this `ArinV57` directory. The editor is in
  `tools/Character3DViewer`. Its `BuildAssets`, `obj`, and binaries are mirrors,
  not places to edit the authoritative model or poses.
- Saved corrections are in `Calibration/arin-v5.7-pose-calibration.json`.
  Saving a key does **not** bake it into the GLB, FBX, or Blender file.
- As of G0, Sin saved **23 keys**. Older reports saying one, two, three, eight,
  or nine keys describe historical checkpoints. Never restore those counts over
  newer saved work merely because a handoff specifies them.
- Keep animation pose, wrist correction, equipment correction, socket metadata,
  world transform, and VFX simulation separate. Fix the faulty layer.
- Never apply a blanket 45/90/135-degree rotation to a new character because it
  helped Arin. Axis, sign, bind basis, pivot, and multiplication order matter.
- Do not use Arin's working equipment to compensate for an incorrect debug marker.
  Validate socket ownership and correct the marker/source transform instead.
- The separate free-roaming flaming-sword demo was **deferred by Sin** after
  G0. The Character Viewer already provides the desired working showcase.
- The dragon will also have fire. Future characters may have ice and magic.
  Resource budgets and attachment lifecycles must work across the whole scene.
- Use one agent, small changes, native Windows first, and light focused checks.
  Keep Sin informed before taking UI control or interrupting an editing session.

## 2. The Journey in Context

### Early prototype, v5.4, and v5.5

Arin began as a Tripo-generated Paladin brought into the developing SMILE 3D
pipeline. Earlier prototype and v5.4 work established source GLBs, editable
Blender assemblies, deterministic GLB-to-SM3D cooking, clips, sockets, and the
inspection viewer. The early tools/assets used Dragonfall wrappers; active
ownership later moved to Sin Star I and the reusable Tools directory.

v5.5 combined a new 2K body with separately sourced equipment and fresh Mixamo
animations. The T-pose hand was not a convincing sword grip, so that candidate
used a dedicated closed-grip glove. This exposed a major lesson: equipment and
a detached glove can look correct in a static Blender view yet separate from the
forearm during animation or after export.

The v5.5 round-trip investigation found that Blender's rest-position armature
export option changed rigid attachment binding. Its versioned exporter disabled
that option and added GLB re-import comparisons at first/middle/last frames.
That is a **v5.5 exporter decision**, not a universal prohibition: the accepted
v5.7 builder uses a different normalized rig/attachment pipeline and explicitly
sets `export_rest_position_armature=True`.

The lesson is to validate exported deformation, not to copy export checkboxes
between different assemblies. Blender's viewport is not proof of runtime parity.
Compressed `.blend` bytes can vary between saves; compare deterministic exported
artifacts and geometry diagnostics rather than demanding identical Blend bytes.

v5.6 remains a selectable diagnostic profile, including its historical animation
work. Sin liked its attack, but the eventual current v5.7 primary attack came
from a new Mixamo download. Do not claim the two clips are the same or delete
v5.6 merely because an older retirement note calls it superseded.

### v5.7 became the stable reference

The v5.7 source had a better modeled equipment grip. We preserved the original
exports, made a genuinely equipment-free derivative, adopted a shared Mixamo
rig, restored pristine Tripo texture/UV data, and rebuilt rigid equipment around
the correct hand spaces. The first accepted checkpoint had seven clips; the
later Slash (4) addition made the current eight-clip checkpoint.

Sin repeatedly found wrist curling, unnatural grip direction, inward shields,
and blade/body intersections that broad numerical checks did not settle. The
workflow therefore evolved from repeatedly requesting asset rebuilds into
direct, saved, per-frame correction in the Viewer.

Important immutable history anchors:

| Commit | What it records |
| --- | --- |
| `d16d963` | v5.4 integration and smooth camera/explicit camera-up work |
| `d5c1947` | v5.5 2K retarget candidate and attachment export validation |
| `6fceefd` | initial seven-clip v5.7 checkpoint, texture/scale/grip work |
| `cfb3698` | equipment glow needed the actor's arena yaw too |
| `d610e29` | self-healing native pointer-button/capture state |
| `3e79183` | lightweight animation editor, JSON persistence, package ownership |
| `d6cd370` | further editor/runtime, precise pivots, activation/backdrop work |
| `8bebf79` | independent wrist/equipment decoupling and complete saved state |
| `8273d56` | thermal fire and shared inspection libraries |
| `6f26aad` | equipment thermal preview and in-place grip rotation |
| `6e9f88a` | pose UI refinements and independent flame playback |
| `6776d9e` | glove close-up zoom and marked destructive action |
| `fa81d737` | G0 persistence, transform/lifecycle/resource hardening and evidence |
| `7decf3e` | Sin deferred the separate free-roam follow-up |

These are reference points, never instructions to reset the repository to them.

## 3. Canonical Files and Ownership

Paths in this section are relative to this `ArinV57` folder unless stated otherwise.

| File or directory | Role |
| --- | --- |
| `arin-v5.7-with-sword-and-shield.original.glb` | Untouched equipped Tripo export |
| `arin-v5.7-no-sword-and-shield.original.glb` | Untouched export whose label was misleading |
| `arin-v5.7-no-equipment.cleaned.glb` | Derivative with equipment actually removed |
| `arin-v5.7-mixamo-rigged-t-pose.fbx` | Approved neutral rig/wrist reference |
| `arin-v5.7-mixamo-sword-and-shield-idle-with-skin.fbx` | Shared Mixamo rig and skin-weight reference |
| `arin-v5.7-mixamo-*-without-skin.fbx` | Source actions, including archived rejected candidates |
| `arin-v5.7-animation-set.json` | Exact selected action files and arm-stabilization policy |
| `arin-v5.7-idle-equipment-checkpoint.glb` | Accepted multi-animation runtime source, despite the word Idle |
| `ArinV57.sm3d.json` | Cooker clip policy and bone-local socket metadata |
| `Blender/arin-v5.7-sword-attack-working.blend` | Editable working inspection scene, not a live pose-JSON mirror |
| `Calibration/arin-v5.7-profile.json` | Identity, asset hashes, clip/sample/socket contract |
| `Calibration/arin-v5.7-pose-calibration.json` | Human-readable saved correction source of truth |
| `Diagnostics/model-quality.json` | Read-only geometry diagnostic and production-gate evidence |
| `arin-v57-idle-previews` | Historical builder preview images; not every latest editor pose |
| `arin-v5.7-package.json` | Package index |

Reusable repository tooling:

- `scripts/build-arin-v5-7-idle-checkpoint.ps1` and `.py`: source rebuild.
- `scripts/audit-arin-v5-7-animation-set.py`: animation/equipment contacts.
- `scripts/audit-model-topology.py`: read-only open-surface diagnosis.
- `scripts/validate-arin-attachment-roundtrip.py`: earlier attachment round-trip
  technique; inspect its version-specific assumptions before using it on v5.7.
- `tools/Character3DViewer/Prepare-BuildAssets.ps1`: copy owned source assets and
  effect textures into ignored tool-local cooking inputs.
- `tools/Character3DViewer/Build.ps1`, `Launch.ps1`: compile and launch/synchronize.
- `scripts/sync-arin-v5-7-calibration.ps1`: validate, compare, backup, restore,
  import, export, and watch calibration.

The original Dragonfall game is retained. Dragonfall2 was discontinued and
removed. Some historical v5.6 inputs and script names still mention Dragonfall;
that is not a reason to relocate current Arin ownership back there.

## 4. Tripo Source Inspection: Trust Contents, Not Labels

Both original v5.7 exports were inspected as glTF 2.0 files. They had the same
41-bone source rig/rest hierarchy, no animation clips, and three embedded
2048×2048 JPEG textures. The export called “No Sword and Shield” still contained
`Sword`, `Shield`, `Shield Strap Main`, and `Shield Strap 2`.

We created the cleaned derivative by removing those four objects while retaining
the rig, body, and source appearance. Preserve both originals: a filename is not
evidence of topology, equipment presence, resolution, or animation compatibility.

The equipped source had useful hand geometry but the sword included weights to
unrelated toe/thigh bones as well as the hand. A rigid prop must not inherit such
weights. Inspect vertex groups before deciding a visual error is a socket bug.

The older source assessment reported 6,533 open/non-manifold boundary edges.
The G0 audit reports 670 remaining boundary edges after a temporary position weld
across its inspected 35 meshes. These are different methods/representations, not
proof that thousands of holes were repaired. Split seams and intentional armor
edges inflate counts; visible openings still need human classification.

## 5. Mixamo and Blender: The Reproducible Pipeline

### Keep one rig contract per character revision

v5.7 uses its skinned Mixamo reference as the authoritative **65-bone** target
rig, not the original 41-bone Tripo rig. The builder imports each selected action,
requires exactly one armature, checks its bone-name set, copies the action, and
rejects missing/duplicate names. It also checks the neutral-reference skeleton.
Name-set validation is useful but not a general proof of equal rest matrices;
future retargeting must inspect hierarchy/rest transforms too.

Download new animations for the same uploaded/rigged character. Keep one skinned
reference, the neutral T-pose reference, and the action-only FBXs. The earlier
v5.5 workflow archived With Skin files for all actions; do not confuse that older
source organization with v5.7's manifest.

### Normalize scale before attachment fitting or cooking

The troublesome Mixamo import used armature-object scale `0.01`, mesh scale
`100`, and translation animation values in pre-normalized units. Blender could
compensate visually while exported SMILE animation became roughly 100× too small.

The v5.7 builder:

1. Requires a positive uniform armature scale and records it.
2. Applies the armature's rotation/scale transform.
3. Multiplies **location** F-curve values, both Bezier handles, and sampled values
   by the original armature scale, across all action layers/channel bags.
4. Does not multiply quaternion components or scale curves by the unit factor.
5. Fits equipment only after this normalization.

Check imported `mixamorig:Hips`, bone translations, object scales, and cooked
bounds. Do not compensate an export-unit bug with a camera constant. Separately,
the Viewer intentionally uses a larger integer-world scale for smooth input;
that precision choice is not the same operation as repairing Mixamo units.

Blender 5.2 uses action slots/layers/channel bags here. The builder sets
`animation_data.action_slot` and walks channel bags. A script written only for
an older flat `action.fcurves` model can miss animation data.

### Restore original texture/UV quality without losing Mixamo weights

The builder imports the pristine cleaned Tripo body, brings it to the matching
reference pose, bakes the temporary source armature, and removes the helper
`Icosphere`. It requires matching mesh sets, then transfers Mixamo weights by
nearest matching world-space geometry onto the pristine source meshes.

The maximum allowed matching residual is `1e-5`. Original Tripo UVs/materials are
retained; the Mixamo mesh supplies skinning. This is why the 2K appearance could
be recovered without abandoning the new animation rig.

This is **not general retopology transfer**. Changed topology, vertex placement,
or changed hand shapes invalidate its assumptions. Stop on a mismatch; do not
raise the tolerance until the validation becomes meaningless.

### Fit rigid equipment in the correct space

Source hands `tripo_part_5`/`tripo_part_6` and bones `R_Hand`/`L_Hand` are matched
to `mixamorig:RightHand`/`mixamorig:LeftHand`. The similarity fit uses corresponding
hand vertices, preferentially those with at least 0.45 hand weight, and records
point count, scale, RMS, and maximum error.

The equipment mesh is converted from source world space into the target reference
space. Bone-local pivot rotation, local offset, and final attachment rotation are
then applied in a defined order. The resulting sword is fully weighted to the
right hand; shield and straps are fully weighted to the left hand. Existing
unrelated vertex groups are cleared.

The sword receives a separate, visually identical material datablock named
`ArinSwordMaterial`. This keeps it independently addressable after cooking rather
than merging it with shield geometry merely because their materials match.
An extra datablock need not duplicate the actual texture assets.

### Historical v5.7 baked corrections

These constants belong to `scripts/build-arin-v5-7-idle-checkpoint.py`. They are
already represented in the accepted source checkpoint. **Do not reapply them as
editor offsets**, and do not transfer them numerically to a new rig.

| Correction | Value / interpretation |
| --- | --- |
| Left wrist reference | Neutral quaternion followed by local-Y outward roll `+135°` |
| Right wrist reference | Neutral quaternion followed by local-Y outward roll `-135°` |
| Sword correction XYZ | `(-15.51063048, -43.72768386, -81.06488564)` degrees |
| Sword local offset | `(-0.04017985, 0.00752897, 0.01881249)` |
| Sword correction pivot | `(-0.01415075, -0.00344447, 0.01844119)` |
| Sword attachment XYZ | `(0, 135, 0)` degrees |
| Shield correction XYZ | `(0, 0, -75)` degrees |
| Shield local offset | `(0, 0, -0.055)` |
| Shield attachment XYZ | `(0, -45, 0)` degrees |

Left and right outward rolls have opposite signs because their local bases differ.
The builder holds hand quaternion curves to these neutral-derived references and
measures hand/forearm axis deviation. This stabilized the base but did **not**
eliminate Sin's need for visual per-clip calibration. It is not a universal IK,
grip-solving, or anatomical correctness system.

The animation manifest separately stabilizes equipment-sensitive arm chains:

- Shield chain reference: `Defend`, Blender source frame 22.
- Sword chain reference: `Idle`, Blender source frame 1.
- Walk/Run/BlockImpact/Hit stabilize both chains; Defend stabilizes the sword
  chain; SwordAttack2 stabilizes the shield chain; current SwordAttack is not
  marked for either arm-chain stabilization.

Do not confuse those source frames with the zero-based editor timeline.

### Export and round-trip validation

The current builder selects the target armature and its meshes, exports GLB with
materials, skins, and ACTIONS, binds the single armature/action slots, disables
extra animations, and saves an editable Blend plus Idle preview renders. Treat
the checked-in script as the authoritative option set.

Re-import the GLB before promotion. Compare hand, hilt, shield, rest transforms,
and first/middle/last poses; inspect cooked output too. Stable metadata does not
prove that the arm does not intersect the chest.

The original seven-clip checkpoint passed a 261-frame equipment/body audit. That
result predates the current Slash (4) addition and later editor corrections; it
must not be presented as a fresh exhaustive audit of today's eight-clip poses.
The archived KO/fall animation was rejected because equipment passed through
the body. Keeping the source is useful; publishing it as accepted is not.

## 6. Animation Identity and Sampling

Current display order: Idle, Attack, Attack 2, Defend, Block Impact, Hit, Walk, Run.
Display labels are not storage identities or runtime indices.

| Exact clip | Current source / purpose | Cooked samples at 30 Hz |
| --- | --- | ---: |
| `Idle` | Calm sword-and-shield idle | 78 |
| `SwordAttack` | Sin's `Sword And Shield Slash (4).fbx` download | 47 |
| `SwordAttack2` | Retained compact hilt-melee strike | 32 |
| `Defend` | Defensive pose | 44 |
| `BlockImpact` | Block reaction | 25 |
| `Hit` | Unblocked hit reaction | 31 |
| `Walk` | Locomotion | 35 |
| `Run` | Faster locomotion | 23 |

Descriptor loops are enabled for Idle/Walk/Run/Defend. The Viewer can deliberately
repeat attack/reaction clips for inspection. It starts in Demo, which completes
at least three whole loops **and** at least five seconds before advancing. Manual
clip selection disables Demo and repeats that clip. There is no inactivity timer
that secretly starts Demo again.

The current descriptor has no authored production sword-trail/impact event set.
Do not infer events from UI order, copy an older v5.5 event time, or claim an
event-driven full-blade sweep exists because flame particles leave a wake.

## 7. What SMILE Receives

The native and Web runtimes consume cooked **SM3D and published textures**, not
the source GLB. `Model3DAsset` declaration, shared cooking, content-keyed cache,
and normal asset publication bridge the formats. Model/descriptor/texture bytes
and cooker identity all matter to cache validity.

Embedded JPEG/PNG sources are decoded and published as real PNG texture assets.
Renaming JPEG bytes to `.png` does not convert them. This also applies to evidence
screenshots: desktop capture returned JPEG data during G0, so genuine PNG
transcoding was required before archiving `.png` files.

Missing/decode-error PNG dialogs should lead to inspection of the exact logical
path, published file, file signature, texture dependency, and executable working
directory. The screenshot alone never proved every historical launch error had
the same cause. Build/publish dependencies rather than scattering ad-hoc copies.

Older Web atlas tearing was traced to a second Y flip: SM3D prepared pixels were
already in renderer orientation. Native/Web texture paths must agree on where
the flip occurs; do it once, not twice.

Retain source and published hashes. A preparation manifest whose only drift is
tool provenance is different from changed model/texture output. G0 permits that
limited provenance comparison without weakening asset-content validation.

## 8. Why Wrist, Sword, Shield, and Sockets Must Be Separate

The transform sequence is conceptually:

1. Sample the base skeletal animation.
2. Apply the saved wrist-node rotations to the body.
3. Decide independently whether sword/shield inherit those node offsets.
4. Apply each equipment item's saved position and pivot rotation.
5. Apply the actor/world transform consistently to body and equipment.
6. Evaluate equipment sockets through the resulting equipment object.
7. Use those corrected sources for markers, glow, and fire births.

Sin's “rotate outward another 45°” observations were useful visual direction, not
a basis-independent formula. A natural forearm can coexist with a wrong wrist;
a good attack swing can coexist with a bad hand pose. Correcting the hand should
not force us to ruin the attack's already-correct sword motion.

**Decouple Sword / Decouple Shield** therefore exclude additive wrist corrections
from that equipment's animation and glow. They do not freeze the object in world
space or remove its original animation. Each equipment item still receives its
own Move/Rotate corrections. A temporary hand/hilt gap is possible until the
artist finishes the independent adjustments.

**In Place** solves a different problem: turning a fitted prop without dragging
its grip away. It captures a stable starting grip anchor, recomputes rotation
from the edit's baseline, and compensates translation using inverse rotation
order Z, Y, X. Repeatedly correcting from already rounded intermediate drag
values accumulates drift; G0 explicitly avoids that. The mode itself is not a
keyframe property; the resulting XYZ rotations and movements are saved.

Continuous hand breathing needs smooth pivot evaluation. Equipment pivot
coordinates use thousandths (`SocketValueThousandths` and
`SetPartPivotRotationThousandths`), rather than whole-unit rounding every frame.
A small rounded pivot jump can become a large visible movement at a sword tip.
Adding extra keys cannot fix that numerical discontinuity.

## 9. Socket Lessons: Two Different Alignment Bugs

### Descriptor coordinates can be wrong

The original sword-fire source pointed roughly 90 degrees away from the blade.
The old tip was about `0.517122` model units from the nearest sword vertex.
The corrected base/tip were derived from the actual accepted `ArinSword` mesh,
then expressed in `mixamorig:RightHand` **bind-local space**. The segment is
approximately `0.403` model units long; the tip matches a mesh vertex within
about `3e-9` model units.

Current authoritative descriptor coordinates:

- SwordBase: `(-0.09090214, -0.02039842, -0.04319661)`.
- SwordTip: `(-0.46575380, -0.07528231, -0.18014731)`.
- Shield flame anchors: `ShieldFireLeft`, `ShieldFireRight`, `ShieldFireTip` in
  `ArinV57.sm3d.json`, derived from the actual perimeter.

These are not actor-world coordinates and must not be multiplied by the hand
transform twice. There are now **13 sockets**, not the older ten.

### Correct metadata can still be displayed through the wrong object

After the equipment and fire were correct, socket markers still appeared wrong.
G0 fixed marker ownership: sword sockets query the calibrated sword object,
shield sockets query the calibrated shield, anatomical sockets query the body.
`SocketReferencePart` makes that distinction explicit.

This did not require changing Sin's accepted sword, shield, or pose values.
There is still no general Socket Calibration editor. Sin agreed to validate that
workflow on a future character, rather than destabilizing Arin.

For the next character, validate rig, attachments, bind-local sockets, marker
ownership, and orientation **before** artist pose fine-tuning. That would have
saved considerable work here. It does not mean current Arin must start over.

## 10. Calibration: Artist Intent Must Survive Rebuilds

The Viewer became a lightweight animation editor because the fastest reliable
feedback was Sin directly fixing the visible defect. The current persistence
contract is JSON schema **2**, runtime payload/storage **3**, inside the generated
checksummed native `SMD4` envelope.

Each key stores a complete independent snapshot:

| Target | Saved values |
| --- | --- |
| Sword wrist | Rotation X/Y/Z |
| Shield wrist | Rotation X/Y/Z |
| Sword | Rotation X/Y/Z, position X/Y/Z, decoupled flag |
| Shield | Rotation X/Y/Z, position X/Y/Z, decoupled flag |

That is 18 numeric channels plus two flags, **20 total**. Wrist translation is
not a current editing capability. Never describe all targets as supporting Move.

One saved key is held across the whole clip while the underlying animation still
moves. Multiple keys interpolate corrections; rotations use shortest-angle
interpolation, looping clips use cyclic interpolation, and flags hold discretely.
Sparse “only the last edited axis” saves would allow edits to leak between keys;
complete snapshots prevent that category of error.

Clip names are authoritative and case-sensitive; indices are recomputed hints.
Unknown clips remain reviewable in JSON but are not applied to unrelated clips.
Frame/count/vector/range/duplicate/identity validation precedes writes. A profile
fingerprint ties the save to the model, descriptor, cooked hash, clips and sockets.
Renaming a version field is not a valid migration to a different skeleton.

The launcher keeps a stable application-data working copy and mirrors Save Frame
to the repository JSON. Recompilation must not erase it. The `.bin`/SMD4 file is
runtime infrastructure, not the human-authored repository format. Backups and
unique temporary files are ignored by Git. Successful writes flush and atomically
replace, preserving previous-good bytes; invalid or concurrently changed data
must not overwrite the good snapshot.

Current controls and their distinct meanings:

- Save Frame: persist all channels at the editing frame.
- Cancel / Reload Key: discard the unsaved preview and evaluate saved state.
- Reset: reset the selected target's correction values; not Delete Key.
- Delete Key: remove that saved frame.
- Delete All Key Frames: current clip only, confirmed, red-bordered lower-left.
- Undo Last Change: one session-local saved-state undo, including clip deletion.
- Copy/Paste Key: complete snapshot; green timeline ticks can be retimed.
- Prev/Next Key: navigate saved corrections, independently of frame stepping.

The clickable JSON path is below the timeline. Do not restore older instructions
that put it inside Pose Calibration or recommend deleting JSON to fix a crash.

The G0 frozen frames were:

`BlockImpact 0; Defend 0; Hit 0; Idle 0; Run 0; SwordAttack 6, 9, 11, 16, 19,
21, 28, 30, 32, 33, 34, 35, 38; SwordAttack2 0, 10, 14, 17; Walk 0`.

Canonical G0 SHA-256:
`6FE2268E390D228AF4F52AF85E5358B66ACF8DE606D60C514FAC6CA0CF8B51B1`.
This is evidence of that save, not a future fixed value. Export and compare again
before a normal commit. All 20 channels matched the pre-migration user save.

## 11. Jitter, Glow, and Fire: Diagnose the Right Layer

We encountered multiple superficially similar failures. Do not reduce them all
to “bad keyframes” or “breathing animation.” Sin observed failures with only an
Idle frame-0 key, failures after renaming JSON, and behavior changing when the
dragon was hidden. Those observations were valuable isolation evidence.

| Symptom | Established lesson / next diagnostic |
| --- | --- |
| Glove separates from forearm after export | Inspect skinning, parent/bind space and round-trip geometry; a glow fix cannot repair it |
| Smooth breathing but equipment jumps | Keep pivot/socket precision; compare consecutive sampled transforms before adding keys |
| Glow detached while sword remains correct | Match actor yaw, pose, coupling, pivot, position and scale on the duplicate; `cfb3698` fixed missing arena yaw |
| Dragon visibility changes equipment/glow behavior | Test multi-actor render state and transform ordering; actor visibility must not change the other actor's pose or arena scale |
| UI alive but character immediately in recovery | Preserve first renderer/viewer error; isolate optional effects, asset load, and core character paths |
| Shield overlay loses faces or looks like another object | Inspect culling/depth/duplicate geometry; accepted thermal shield replaces the old solid golden overlay |
| Flame extends sideways while physical blade is correct | Validate bind-local descriptor endpoints, then query through corrected equipment |
| Trail vanishes at each automatic loop/clip boundary | Retain living world-space particles, suppress discontinuous source inheritance |
| Web atlas appears torn/flipped | Verify pixel orientation is transformed exactly once |
| High FPS but stepped camera motion | Preserve fractional input/pivots and inspect integer quantization; FPS alone is not continuity |

Not every old “dragon makes it shake” report has an independently recorded single
root cause. Do not retroactively claim it was definitely the cache, the GPU, or
the JSON. The current safeguards cover precise pivots, complete calibration,
per-object offset policy, animator/pose-revision-aware palette state, immutable
render submissions, and optional-effect isolation. Use a minimal reproducer if
the symptom returns; preserve the saved file first.

### Fire progression and accepted result

Early glow/ribbon experiments could become visible bars, fans, webs, or residual
post-attack geometry. A rejected earlier flame-atlas experiment is not the same
system as the later approved thermal fire. Do not reintroduce an old connected
trail merely because an earlier handoff suggests it.

The accepted sword has a golden/fiery outline and stronger segment-emitted flame,
with world-space wisps surviving behind the moving blade. The shield uses three
quieter edge emitters; its prior solid golden overlay is suppressed in the thermal
preview. Sword Fire and Shield Fire are independently toggleable and default on
in this Viewer. Sin requested additional sword flame/wake and slightly stronger
shield flame while keeping the shield less dominant.

The preview settings documented at introduction were sword emission 200%, source
radius blade-length/12; shield edge radius 3 and emission 75%. Check current
`Program.smile` before tuning. These are preview settings, not a universal preset
for all actor scales or a completed production-effects specification.

Particles already emitted remain in world space. A visual continuity epoch and
first-sample rule distinguish real motion from editor seeks, pose edits, resets,
teleports, profile changes, and recovery. Normal playback and automatic Demo
changes preserve tails. Explicit navigation clears/reseeds; source inheritance
is zero across discontinuities. Do not emit a long bridge from an old pose.

Space pauses the scene, but flames animate by default. Pause Flames independently
stops fire simulation. Resuming must not accumulate elapsed catch-up bursts.
These are separate clocks; camera interaction remains available.

G0 keeps High's existing five GPU layers but expands bounded admission: 32 GPU
systems, unchanged 32,768 total slots, 32 CPU batches, six emitter handles.
High uses 1,664 slots, Medium 832; full CPU fallback uses four batches/384 slots.
Arin sword plus three shield edges uses 20 systems. A measured sword/impact/two
torches/dragon-breath fixture used 25 systems and 5,824 slots without fallback.
Atomic admission must provide the complete requested/fallback effect or reject
cleanly, never leave just smoke or an invisible blade.

This is not shared simulation/render-view implementation, volumetric fluid fire,
or a finished event-driven full-blade swept surface. Those scope distinctions
matter even though the current Viewer looks good and Sin approved it.

## 12. The Viewer Became an Editor: UI and Camera Lessons

Sin's final controls supersede the many intermediate requests in old messages:

- Space pauses/resumes movement. Right mouse resets as a fresh launch, including
  Demo, dragon, floor/grid, default landscape, and hidden Pose Calibration.
- Left drag is screen-like pan; middle drag orbits around Arin's anchor; wheel
  zooms. H/V/Zoom sliders support hover-wheel and exclusive capture until release.
- A drag begun on a slider belongs to it even outside its rectangle. It must not
  also pan/orbit the scene. Hidden panels must not consume input.
- Native pointer state reconciles missing button edges/capture on mouse motion;
  UI event ordering matters when only one panel responds but others do not.
- Vertical orbit uses a consistent explicit up vector; crossing poles must not
  accidentally invert the scene. Upside-down world with upright HUD is a 3D
  camera issue, not a reason to flip the UI or model.
- Preserve sub-unit motion and eased zoom. The old -48 zoom limit was insufficient
  for gloves; current close-up zoom extends to -144 with camera-distance reduction.
- Timeline supports drag scrub, hover-wheel one-frame steps, key jumps, retimed
  key ticks, and frame-button repeat every 300 ms without a delayed catch-up burst.
- D/W/S toggle dragon/sword/shield. Hiding the dragon must not shrink the arena.
- Backtick first hides panels, then all UI, then restores the previous UI state.
- Backdrops are screen-fixed images, not camera-facing world planes. The default
  is Sin Star I's landscape without the title; the title version is also available.
- Foreground activation and remembered window geometry are part of usable tools.
  Relaunch the executable actually rebuilt, not another stale output path.

Reusable results are `Arena3D`, `StaticBackdrop3D`, and `Smile.UI.Controls`.
Character Viewer and Fire Lab share behavior; palette and tile settings differ.
Arena grid is an emissive mesh rather than many objects. Thin distant lines need
appropriate tile size, thickness and scene anti-aliasing. Fire/HDR/post passes
must not silently change the main scene's quality or distort Renderer2D UI.

No inactivity timer should commandeer an artist's editing session. Save/Cancel
does not silently toggle the latest explicit scene-pause policy. A user keyboard
or KVM-generated Escape is not a request to abandon repository work.

## 13. Safe Rebuild and Commit Workflow

1. Read root `AGENTS.md`, this guide, package/Calibration READMEs, and current
   source. Inspect actual HEAD, status, current saved keys and process paths.
2. Ask Sin to save before interrupting an active edit; do not assume an old
   “unsaved edits can be discarded” approval applies indefinitely.
3. Export/compare saved calibration and keep a backup before asset migration.
4. For source-model experiments, use explicit **new output paths**. The default
   builder GLB output overwrites the canonical accepted checkpoint.
5. Rebuild through the appropriate tool/project path; verify published textures
   and SM3D, not only source-file timestamps.
6. Launch the actual new executable and foreground the intended tool. The normal
   launcher synchronizes saves, but can close older instances; inspect its current
   behavior before invoking it while Sin edits.
7. Use focused checks matching the changed layer. Do not rerun a large soak for
   a documentation-only change.
8. Export live saves before commit, stage all intended source/package changes,
   review the diff, commit with `Sin and Codex:`, push and verify the remote SHA.
9. If compiler/runtime/VSIX payload changes, rebuild/install and compare the
   installed compiler/runtime too, not only the extension DLL. Native `.lib`
   archive hashes can change on rebuild despite unchanged source.

Read-only / normal save-preservation commands, from the repository root:

```powershell
pwsh -NoProfile -File scripts/sync-arin-v5-7-calibration.ps1 -Mode Validate
pwsh -NoProfile -File scripts/sync-arin-v5-7-calibration.ps1 -Mode Compare
pwsh -NoProfile -File scripts/sync-arin-v5-7-calibration.ps1 -Mode Export -AllowMissing
```

Build-path trap: `tools/Character3DViewer/Build.ps1 -Configuration Debug` currently
still writes `bin/Character3DViewer.exe`. The separately used development output
is `bin/Debug/Character3DViewer.exe`; configuration does not automatically select
that path in this script. If compiling there explicitly, first run
`Prepare-BuildAssets.ps1`, then invoke the compiler with the project and explicit
`-o` path. Use `Launch.ps1 -Executable` with that exact executable. Do not say
“rebuilt Debug” while launching the stale regular binary.

For a deliberate source rebuild, use `build-arin-v5-7-idle-checkpoint.ps1` with
both `-OutputBlend` and `-OutputGlb` pointing to a new experiment directory.
This writes previews too and is not necessary for editor-only corrections.

Useful existing focused gates include `test-arin-calibration.ps1`,
`test-viewer-calibration-native.ps1`, `test-character-3d-viewer-hardening.ps1`,
`test-native-thermal-fire.ps1`, and the Character3D/VFX wrappers. G0 repaired the
obsolete idle-reset assertion rather than falsely reporting a skipped wrapper
as green. The final full smoke and 618.671-second native preview passed; see the
G0 report for exact commands and limits of that evidence.

## 14. How to Approach Arin v5.8 or the Next Character

1. Preserve v5.7 as a known-good visual reference, including JSON and hashes.
2. Create a new versioned package; retain original downloads and provenance.
3. Inspect real mesh/texture/equipment contents and visible holes before rigging.
4. If retopology changes geometry, re-rig/reweight or perform a verified transfer.
   Do not reuse vertex indices/weights or accept the current exact-match transfer
   blindly. Bone names alone do not prove animation compatibility.
5. Normalize units and establish the neutral wrist/forearm relationship.
6. Fit rigid sword/shield attachments and validate full-animation hand continuity.
7. Establish blade endpoints, grip axes and shield perimeter sockets in bind-local
   space; display them through the correct object and validate after world rotation.
8. Only then add fine per-clip artist corrections. Independently adjust wrist and
   prop when an imported swing is correct but the hand is wrong.
9. Reuse workflow/scripts, not v5.7's numeric offsets. Migrate saved corrections
   explicitly against the new profile and inspect them visually.
10. Audit first/middle/last and problem poses, then a proportional full-frame
    contact check when needed. Keep known source-model holes visible in diagnosis.

A re-export does not mean every tool, script, animation-selection decision, UI
feature, or lesson is lost. It can mean substantial rig/weight/attachment work must
be repeated if topology or rest space changes. Do not promise a drop-in migration.

Blender can repair geometry, but the approved work so far did **not** close Arin's
holes. Nor is there an approved one-click “bake all Viewer corrections permanently
to Blender/GLB” workflow. Both need a separate scoped change and visual review.

## 15. Evidence and Source Reading Order

Read current facts before historical proposals. All repository paths below are
relative to `D:\SMILE 2.0`:

1. This guide and `ArinV57/README.md`, `Calibration/README.md`, package/profile JSON.
2. `scripts/build-arin-v5-7-idle-checkpoint.py` and the animation manifest for exact
   Blender transforms, slot handling, matching thresholds, and export options.
3. `tools/Character3DViewer/README.md`, `Program.smile`, `Profiles.smile` for controls,
   correction order, explicit cuts, precision, and runtime ownership.
4. `docs/implementation/approved-viewer-thermal-fire-hardening-m7e-g0.md`,
   `arin-v5-7-calibration-validation.json`, and `m7e-g0-validation-results.json`.
5. `docs/implementation/screenshots/m7e-g0-approved-viewer-fire-hardening/` for
   actual PNGs, screenshot index, socket/pose evidence, and phone contact sheet.
6. Historical context: `paladin-production-acceptance-m7d-b.md`,
   `paladin-v5-4-viewer-export-hardening-m7c-b1.md`,
   `model3d-build-cooking-m7c-a.md`, `character-viewer-v5-5-active-handoff.md`,
   and `m7e-g-equipment-fire-preview.md` under `docs/implementation`.

Older reports contain superseded pause keys, key counts, export options, active
paths, and unresolved-at-the-time findings. They explain the journey; they do
not override current code, root instructions, or Sin's latest approval.

Maintain this guide as the workflow changes. Record what changed, why it changed,
which revision it applies to, evidence, and what remains unimplemented. That is
more valuable than another confident but untested “rotate it 90 degrees” fix.

## Multi-character Viewer Follow-up

The native Viewer now uses separate Character tabs and calibration storage for Arin and
Orin, plus a Party preview. The existing Arin storage key, profile fingerprint, and all
saved values remain unchanged. The shared synchronizer retains its historical filename;
`-Character Orin` selects Orin’s package and data key. Launch starts a watcher for each.
Party reuses the same correction evaluator for both actors, with distinct in-memory clip
ranges. Equipment UI labels now say Weapon so they also describe Orin’s hammer.

Orin’s first equipped Tripo export illustrates another source-rig hazard: some hip-cloth
vertices carried hand/arm weights, and its hand bone controlled much of a forearm. Raising
arms lifted cloth and independent hand rotation separated the gauntlet. These were observed
in the T-pose derivative, not evidence of deleted mesh geometry. Removing remote cloth
influences fixed the lifted cloth; Sin is providing a clean T-pose as the preferred rigging
source. Do not use this initial derivative as an accepted animation rig.

### Attack audio checkpoint (September 5)

`Audio/attack-audio.json` records original synthesized slash and crosscut WAVs.
The Viewer triggers each cue once when its clip crosses the cue time; holding an
end frame does not repeat it. Shared window-focus audio muting remains authoritative.
This addition does not change the accepted GLB, descriptor or 23 saved pose keys.
The newly supplied Death FBX and the requested shield fire outline are pending
in this mid-development checkpoint, not silently substituted with the archived KO.

## September 5: Death and selectable shield outline

The current package contains nine clips and 21 sockets. The new user-supplied
`arin-v5.7-mixamo-death.fbx` is separate from the older rejected KO source.
`scripts/append-character-animation.py` appended Death from a matching rebuilt
rig while preserving the accepted mesh, skin, textures and eight previous clips
byte-for-byte. `Diagnostics/death-append-validation.json` records that comparison.

All 23 saved calibration keys were migrated by clip name when the sorted runtime
indices changed. Their frame numbers and all 20 channel values remain identical.
The hashes in `Calibration/arin-v5.7-profile.json` describe the current package;
older hashes in historical sections describe their earlier milestones.

`Calibration/arin-v5.7-grounding.json` records frame zero for every clip and full
Idle/Death samples. Arin's Death ends horizontal and plays once, holding the final
pose. Its measured contact correction is independent from Orin's placement offset.

The viewer defaults to an eight-point warm ember shield outline. Choose **Flames**
on Arin's tab or the Party's Arin Shield button to restore the previous effect.
The original three fire sockets and emitter code remain intact. Both treatments
follow calibrated shield geometry; Freeze Fire applies to either style.

When rebuilding, prepare a review GLB with the manifest, compare existing clips,
then run `scripts/prepare-arin-shield-rim.py` against the resulting checkpoint and
descriptor. Any asset hash/clip/socket change requires an explicit name-preserving
calibration migration before importing the new runtime profile.

## September 5: scene-owned Fire advancement

The prerequisite hardening retained Arin's accepted model, descriptor, 23 calibration
keys, fire presets, attachments, and rendered appearance. It changed only runtime
ownership: the Viewer now stages sword and shield endpoints, then advances the shared
Fire family once at the scene boundary. Dragon fire uses the same clock without either
actor owning it. A disabled or failed optional equipment path therefore cannot freeze
another actor's emitter.

Paused visual-history invalidation clears stale emitters immediately. Resume creates
the current-pose emitter and advances it only by the current frame elapsed time; the old
100-millisecond warm-up was removed because it produced a visible catch-up burst.
Automatic clip changes still retain world-space tails, while explicit cuts and paused
pose edits preserve their documented clearing behavior. This ownership change does not
alter the canonical GLB or calibration profile hashes.

## September 5: publish the saved pose layer to Web

Sin found that the Web Viewer showed the model without the native saved poses.
The corrections are not baked into the GLB: publication must carry that separate
layer. `Prepare-BuildAssets.ps1` now uses the authoritative synchronizer serializer
to generate a declared, ignored SMKF default asset from the current canonical JSON.
The shared Viewer loads it only in the absence of a working save, then uses its
existing fingerprint/name/frame/channel decoder and correction evaluator. Existing
native/browser saves and deliberate empty tracks are not replaced by the default.

This checkpoint preserves all 23 keys and every canonical model/profile byte.
The native isolation fixture compares the entire default payload with canonical
saved data; that fixture also passed in visible Edge. Edge's Attack pose panel
showed 13 keys and the saved timeline markers. This is technical evidence, not
new artistic approval or completion of the remaining Web workflow gate.

## September 6: Web rotation parity and Sin's Death correction

Web's Euler matrix used the opposite rotation signs from native for all three
axes. That reversed arena facing and changed wrist/node/world-pivot corrections,
especially the shield. The Web renderer now matches native's X-then-Y-then-Z
convention. Do not compensate for this renderer defect by changing accepted
character offsets or baking a second Web-only model. Native/shared numeric
fixtures cover axis and combined object, node and pivot rotations.

Sin confirmed Desktop and Web were looking good and then asked to stop further
screenshot comparisons because Arin looked good on Web. This is visual feedback
on the observed Viewer, not acceptance of every remaining Web workflow.

Sin subsequently saved Death frame 0, bringing the canonical snapshot to 24 keys.
The exported JSON SHA-256 is
`C05C87BF0A92B373DB7ECD1CB304F4446B851E7AFEA836E8BB05D058B1B20F0B`.
All previous keys and model/profile bytes remain unchanged. Web publication now
includes this current snapshot; existing browser-authored saves still take
precedence. The earlier comparison captures retain their 23-key baseline.

Timeline 0-Frame, previous/next key and previous/next frame now pause the shared
scene on both targets, including navigation on clips with no saved keys. Space
resumes, camera controls remain usable, and navigation does not save a new key.

## September 6: saved JSON download and clip-order lesson

The Web filename below the timeline now requests a schema-2 JSON download of
the saved calibration buffer. Temporary preview edits are not silently saved.
Identity metadata is generated from the validated canonical profile at publication;
there are no separate hardcoded Web model hashes or Web-only pose defaults.

The first export fixture caught an ordinal-clip assumption: a saved package can
list clips in a different order from the loaded model. Match exact names and treat
indices only as hints, just as the existing decoder does. Both current snapshots
now round-trip through the desktop serializer. The actual Edge Arin download
preserved all 24 keys, and native text-picker import/export reproduced its bytes.
This does not yet establish an in-Viewer JSON import workflow or storage recovery.
No live save, model, descriptor or profile identity changed in this milestone.

## September 6: checked calibration persistence

The Viewer treats writing a pose as a transaction: no saved JSON or Undo state is
committed until persistent storage accepts the block. Failed Save Frame keeps the
temporary preview for retry/cancel while restoring the saved track; failed Undo
retains its entry. A missing/corrupt primary may load a checksummed backup with an
explicit recovery status, without rewriting the primary. Wrong profile fingerprints
remain blocked. This changes no character offsets, accepted model bytes or saved
identity; the current 24-key snapshot, including Death frame 0, is preserved.
