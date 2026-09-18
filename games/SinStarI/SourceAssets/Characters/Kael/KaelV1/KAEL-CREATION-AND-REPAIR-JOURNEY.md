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
6. `Source/export_kael.py` validates weights, tangents, all animated floor samples,
   root stability and runtime vertex/bone limits, then packs the Blender checkpoint
   and exports GLB plus the ten-socket descriptor.
7. `Source/preview_kael.py` renders complete equipped views and checks the GLB
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
| Idle | sword and shield idle.fbx |
| Walk | sword and shield walk.fbx |
| Run | sword and shield run.fbx |
| Attack | sword and shield slash (4).fbx |
| Attack2 | sword and shield slash (3).fbx |
| Defend | sword and shield block idle.fbx, then authored sword guard |
| Hit | sword and shield impact.fbx |
| Death | sword and shield death.fbx, then keyed sword release |
| Dodge | Dodging Right, separately downloaded for this rig |
| Victory | Victory, separately downloaded for this rig |

Idle, Walk and Run loop. Defend and Death hold their final poses. Walk and Run were
downloaded with root motion; the grounding pass removes horizontal root travel
for game-controlled movement. No external service is required for normal builds.

## Grounding and export evidence

Ground the skinned body independently of equipment before applying per-clip
corrections. The final body bind minimum is effectively 0 m; Idle frame zero is
0.001 m. Every clip begins at 0.001 m, excluding equipment. Idle, Walk, Defend and
Hit maintain contact. Other clips preserve positive airtime; Death maintains
contact after frame 40. All 730 Blender samples pass body/sword floor checks and
horizontal root stability; the exported GLB round trip also passes clip-start
and final Defend/Hit/Death checks. Use the JSON reports for exact values and hashes.

Export repairs eight body and sixteen sword collapsed UV faces, two body and six
sword corner normals, and one sword micro-triangle vertex displaced by 0.561 mm.
The depth-repair stage preserves the front outline exactly; the final export has
this small tangent repair. There are no unweighted vertices and no more than four
influences per body vertex. The native compiler accepts the PBR model and produces
the same cooked Kael asset for the Viewer and Sin Star I.

## Native integration

`Profiles.smile` owns the ten-clip/ten-socket metadata and equipped height of 134.
`ViewerDragon` owns the opponent actor and applies twice the solo fit scale.
`ViewerParty` keeps the existing four-hero turn sequence and selects Kael's own
Attack/Attack2 clips. Kael approaches to 150 world units; heroes close further than
for Vrax's broad body. Vrax-only node aiming, effects and audio are not attached
to Kael. `ViewerBeatSequence` reserves identity 7 for Kael camera/head saves.
Sin Star I's menu routes into the same Viewer session, with no duplicate battle
implementation. Keep Studio and Web work on hold.
