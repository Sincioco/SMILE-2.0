# Kael v1 creation and repair journey

## Sources and ownership

Sin supplied two unrigged Tripo GLBs: a 1,981,524-triangle body and a
1,851,642-triangle sword. `Source/*.original.glb` are byte-identical copies of the
Downloads originals. References remain beside the package. This folder owns all
Kael-specific geometry, skinning, animation, sockets and measurements; the Viewer
owns only profile metadata, presentation and shared battle orchestration.

## Reduction and sword repair

Use the installed Blender 5.2, with `--background --python-exit-code 1 --python`.
Run these package scripts in order:

1. `Source/prepare_kael.py -- reduce` preserves UVs and reduces body/sword to
   42,000/6,996 triangles. Merge only coincident vertices at 0.000001 m before
   collapse: GLB's split UV/normal accessor vertices otherwise decimate into shards.
   Clear imported split normals and keep smooth surface shading.
2. `Source/repair_kael_sword.py` compresses blade depth below source Z=0.60 m,
   blending back into the ornamental guard by Z=0.77 m. Front X/Z coordinates and
   UVs remain unchanged; this removes the excessive side thickness and depth spikes.
   `Source/sword-repair.json` records 0.0980063 → 0.00480536 m blade depth.
3. Upload the reduced body FBX to Mixamo. The accepted auto-rig uses the
   **2 Chain Fingers (41 bones)** preset. Keep the downloaded rigged T-pose FBX.
4. `Source/assemble_kael_animation.py` transfers the rig weights to the preserved
   body materials and bakes the ten selected animations at 30 fps. It validates
   skeleton identity and a maximum joint error below 0.00001 m. Export each clip
   **Without Skin**, 30 fps, no keyframe reduction.
5. `Source/fit_and_ground_kael.py` fits the repaired sword at 72% scale, with source
   grip Z=0.845 m. The palm is derived from Kael's own wrist/index-knuckle bind
   positions. Do not copy another character's wrist offsets. The sword direction
   is opposite the hand's local Z axis; the other sign produces a reversed grip.
   Defend gets a baked right-arm sword guard. Floor correction rotates the wrist
   and sword together; Death releases the sword over frames 30–48.
6. `Source/author_kael_earth.py` opens the preserved grounded checkpoint, replaces
   Idle with this rig's Breathing Idle, then bakes EarthHurl/EarthVolley/EarthSlam.
   The authored pose sequence uses a raised knee/stomp, deep planted stance, lift,
   wind-up and torso-led strikes inspired by Jared Koh's reference. Both arms move;
   the shared Kael adapter hides the sword during Earth casts, following Sin's
   later instruction. The hidden sword node remains floor-safe without restricting
   the casting wrist. The original grounded checkpoint remains available.
7. `Source/export_kael.py` validates weights, tangents, all animated floor samples,
   root stability and runtime vertex/bone limits, then packs the Blender checkpoint
   and exports GLB plus the ten-socket descriptor.
8. `Source/preview_kael.py` renders the final views and checks the GLB
   round trip. Refresh `checksums.sha256` and `package.json` after accepted changes.

Blender does not reliably return a failing process status for Python exceptions
unless `--python-exit-code 1` is supplied. Keep that option. Mixamo may append
numeric suffixes to downloads; verify the latest file and skeleton rather than
copying an older same-named animation from another character. The selected clips
and original combat ZIP are preserved under `Source/Mixamo`; the ZIP is a source
archive, not a delivery bundle.

## Clip provenance

| Runtime clip | Mixamo source |
| --- | --- |
| Idle | BreathingIdle.fbx, Sway 0, Breathing 25, Overdrive 50; 30 fps, no reduction |
| Walk | sword and shield walk.fbx |
| Run | sword and shield run.fbx |
| Attack | sword and shield slash (4).fbx |
| Attack2 | sword and shield slash (3).fbx |
| Defend | sword and shield block idle.fbx, then authored sword guard |
| Hit | sword and shield impact.fbx |
| Death | sword and shield death.fbx, then keyed sword release |
| Dodge | Dodging Right, separately downloaded for this rig |
| Victory | Victory, separately downloaded for this rig |
| EarthHurl | Original authored stomp, lift and two-handed thrust; 97 frames |
| EarthVolley | Original authored lift and three alternating thrusts; 121 frames |
| EarthSlam | Original authored lift and low downward strike; 91 frames |

Idle, Walk and Run loop. Defend and Death hold their final poses. Walk and Run were
downloaded with root motion; the grounding pass removes horizontal root travel
for game-controlled movement. No external service is required for normal builds.

## Grounding and export evidence

Ground the skinned body independently of equipment before applying per-clip
corrections. The final body bind minimum is effectively 0 m; Idle frame zero is
0.001 m. Every clip begins at 0.001 m, excluding equipment. Idle, Walk, Defend and
Hit maintain contact. Other clips preserve positive airtime; Death maintains
contact after frame 40. Every Blender sample passes body/sword floor checks and
horizontal root stability; the exported GLB round trip also passes clip-start
and final Defend/Hit/Death checks. Use the JSON reports for exact values and hashes.

Export repairs eight body and sixteen sword collapsed UV faces, two body and six
sword corner normals, and one sword micro-triangle vertex displaced by 0.561 mm.
The depth-repair stage preserves the front outline exactly; the final export has
this small tangent repair. There are no unweighted vertices and no more than four
influences per body vertex. The native compiler accepts the PBR model and produces
the same cooked Kael asset for the Viewer and Sin Star I.

## Native integration

`Profiles.smile` owns the thirteen-clip/ten-socket metadata and equipped height of 134.
`ViewerDragon` owns the opponent actor and applies twice the solo fit scale.
`ViewerParty` keeps the existing four-hero turn sequence and selects Kael's own
Attack/EarthHurl/Attack2/EarthVolley/EarthSlam cycle. Kael approaches to 150 world units; heroes close further than
for Vrax's broad body. Vrax-only node aiming, effects and audio are not attached
to Kael. `ViewerBeatSequence` reserves identity 7 for Kael camera/head saves.
Sin Star I's menu routes into the same Viewer session, with no duplicate battle
implementation. Keep Studio and Web work on hold.

## Earth casting and Idle acceptance

The source reference is [Jared Koh's Avatar Earthbending Animation](https://www.youtube.com/watch?v=lZRYNRqdWD4).
The first restrained casting pass was not visibly strong enough. The revised
authoring uses distinct keyed anticipation, stomp, crouch, lift, chamber, strike
and recovery phases, with three separate Volley thrusts. Sin subsequently allowed
the sword to disappear during Earthbending, freeing the right arm for these poses.
Do not restore the superseded sword-in-hand constraint for Earth clips.

`Source/earth-animation-report.json` records both hands, head and planted-foot travel.
Measure the baked action with temporary IK constraints muted, or the stationary
helper targets conceal actual keyed hand movement. The native Earth Lab fixture
also samples the cooked actor's hand/head sockets: body motion must survive export
and native playback, not merely exist in a Blender action. Idle must remain below
the small native movement threshold. Inspect the actual newly launched executable;
an older running process retains its previous loaded model after a build.

Ground all clips by the body's minimum, independently of the hidden sword, preserve
the intentional raised foot, and verify frame zero plus Block/Hit/settled Death.
Normal sword attacks and their attachment/grounding remain unchanged. The adapter
restores the normal Weapon/W preference when leaving an Earth clip. Model scale,
speed 200, ten sockets and the four-hero battle roster remain unchanged.

## Water Lab review candidate — September 18, 2026

Use the three links in the Water Lab README as visual references: sweeping arms and
weight shifts, a clear winding water body, then a torso-led release. The water
authoring uses original pose keys and the existing two-bone IK bake. Reuse the
accepted quiet Idle and its grounding; do not retarget or replace it again.
WaterWhip, WaterOrbit and WaterSurge each contain 151 frames at 30 Hz (5 seconds).
The native Lab plays them at 200%, hiding the sword and synchronizing water through
the same normalized clock. Validate with the IK helpers muted and with native
socket sampling, as for Earth. The initial supporting-arm samples were too small;
the final poses exceed 0.35 m of travel on both arms and 0.12 m of head-height change.

The water GLB has sixteen clips and ten sockets. Sin subsequently authorized
native Viewer/game adoption; both now stage this same water-named GLB and descriptor.
Keep its filename and checksum stable. The preserved thirteen-clip Earth baseline hash is
`d3187af4bba99e1709aa7a27faa7987bd3f0fa837c00494d790517d33a6fbbda`.
The water GLB hash is
`231f4494c656b9a3a5c363b2108a6d6d1f737772a38609e2bd174a6abd5dde8e`.
The bind body's minimum is effectively zero; Idle and new Water clip starts are
0.001 m. `water-export-validation.json` records every-frame floor checks, root spans,
topology and budgets; `water-roundtrip-validation.json` records imported starts,
Defend/Hit/settled Death and mid/end water poses. Separate water Blender checkpoints
and nine pose previews preserve the complete reproducible candidate.

The optional Realistic Water preset changes only authored geometry/material inputs.
Small-target wrapping requires explicit cylindrical bounds and follows the target's
recoil. It does not add fluid simulation or infer collisions from arbitrary meshes.
The native fixture checks body motion, helix clearance, target contact, dimensions,
wrap coverage behind the pillar, shader-look toggling, demo progression and cleanup.

## Native Viewer/game adoption — September 18, 2026

`KaelWater` maps final grounded actor pose, clip time and caller-supplied target body
bounds into the shared water effect. `WaterFlow3D` accepts an optional scale;
geometry is computed in caster-local units and returned to world space, preserving
world target contact. Impacts retain the target's actual dimensions. No animations
were rebaked for adoption. The realistic preset is on in Viewer/game, while the
Lab retains its comparison toggle. Both adapters respect the existing sword toggle.

Normal/Earth/Water categories repeat every three boss turns, every Earth and Water
skill appears within nine turns, and normal attacks alternate continuously.
The game fixture advances the actual scheduler and all sixteen demo clips, samples
water flight/contact and checks full scene cleanup. Web adoption remains held.
