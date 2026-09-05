# Party Viewer handoff — September 5, 2026

This is the current native development preview handoff for Sin and ChatGPT.
It supersedes the camera, Death, asset-path and pending-task descriptions in
earlier September 5 snapshots. Web visuals and production asset approval remain
deferred. Work uses one Codex agent in `D:\SMILE 2.0`, branch `main`.

## Open the current tools

- Viewer/editor: `tools/Character3DViewer/Launch.ps1`; executable
  `tools/Character3DViewer/bin/Character3DViewer.exe`.
- Fire Lab: `tools/AdvancedFireVfxLab/bin/Debug/AdvancedFireVfxLab.exe`.
- Lightning Lab: `tools/AdvancedLightningVfxLab/bin/Debug/AdvancedLightningVfxLab.exe`.
- Dragon Blender scene:
  `games/SinStarI/SourceAssets/Bosses/RedDragon/RedDragonV11/red-dragon-v1.1-rig.blend`.
- Arin nine-clip Blender scene:
  `games/SinStarI/SourceAssets/Characters/Paladin/ArinV57/Blender/arin-v5.7-nine-clips.blend`.
- Orin repaired Death scene:
  `games/SinStarI/SourceAssets/Characters/Tank/OrinV13/Blender/orin-v1.3-death-repaired.blend`.

All paths above are repository-relative. Native Viewer is the current Storm
Presentation Lab as well as the character editor. Separate effect Labs and their
sources live beside it under Tools, not Examples. Window placement is remembered.

## Delivered behavior

Arin, Orin, Dragon and Party have separate tabs; launch defaults to Party. Hero
speed defaults to 200. Individual demos schedule the next clip after three
seconds while allowing the current animation to finish. Orin's Block plays once
and holds in both individual and Party playback.

Party uses a spaced front arc formation and takes turns: Arin, Orin, Dragon.
Dragon attacks make the heroes guard, with a randomized fatal hit at impact.
The selected hero completes Death, remains down, and revives at their own next
turn. Both Death clips finish horizontal on the arena floor and hold.

The right panel follows the active attacker. Party displays the attack name and
rendered camera position/target XYZ, yaw, pitch, FOV and distance. Camera 1
continuously sweeps the front arc; two independent battle cameras cut between
hero and Dragon views. Battle framing uses stable shot anchors, hero clearance
and a minimum height of 24 world units. Only an Orin ground smash triggers a
brief decaying shake, and Flash/Shake Off disables it. No continuous shake is
applied for lightning charge or ordinary strikes.

Orin's hammer glow uses the same final grounded equipment transform as the mesh.
His hammer retains sparks and lightning; his shield effect follows its outline.
Arin defaults to a warm ember shield outline. The original Flames option and
three flame sockets are preserved. Freeze Fire and Freeze Lightning are
independent in Party; Orin also has Freeze Lightning in his own tab.

Dragon has Idle, Roar, FireBreath, ClawStrike, Hit and Fireball. Head-only target
aim leaves his body facing stable. Mouth heat and eye glow remain visible at
idle. Fireball charges for 1.8 seconds, travels for 0.7, then explodes. Hero and
Dragon attacks have original synthesized cues. The final animation refinement
adds shoulder-to-tip wing delay, modest asymmetry, claw anticipation/follow-through,
and quick hit recoil with slower recovery. Feet remain fixed.

Bare Alt/F10 no longer invokes the unwanted native menu pause. Fire Lab uses a
normal orange grid; Lightning Lab uses an uppercase in-window heading and the
same static backdrop treatment as Fire Lab.

## Repairs future character imports must retain

Commit `ab5e216` contains the KO, shield and camera milestone. Earlier relevant
commits are `6e4bee9` (Dragon tab/head aim), `6514929` (Alt/VFX freeze/Lab UI),
and `2f421da` (Orin baseline, equipment transform and storm effects).

Orin had two distinct faults. His animated body baseline differed from its bind
pose by 0.119 model units. Separately, Mixamo's Death fall was stored on the
armature object; stripping object curves removed the fall's orientation. The
repair retains the fall on Root, and measured Death contact curves then place
the horizontal pose on the ground. A height offset alone cannot fix missing
root rotation. Never copy Orin's numeric correction to a new character.

Measure the skinned body without equipment at bind pose and every clip's frame
zero, then inspect guard/hit and the settled Death. Validate exported/re-imported
poses and native individual/Party playback. All overlays use the final actor
transform. Arin's new Death was appended without changing prior geometry or
clips, and all 23 saved keys were migrated by name with values preserved.

Authoritative current hashes live in each character's Calibration profile JSON.
The canonical character folders are ArinV57 and OrinV13. Before commits touching
calibration, export both through `scripts/sync-arin-v5-7-calibration.ps1`.
Do not overwrite saved edits with historical snapshots. Orin currently has zero
saved calibration keys; the approved equipment fit lives in the accepted asset.

## Dragon rig research and practical next steps

The current Dragon has 24 deformation bones, six original clips, 9,912 triangles,
and its original texture. The new animation output retains the prior mesh,
skin attributes, UVs and embedded image bytes. `RedDragonV11/retarget-chains.json`
records its actual spine, neck/head, jaw, wing, leg and tail chains.

Blender's IK system controls an explicit chain toward a target; a pole target
controls the bend direction. Rigify is modular, with separate limb, spine,
head and tail components. My recommendation is to retain this deformation rig
for the current stationary battle, then add a separate control layer with foot
targets and bend poles when locomotion or substantial body motion is required.
For larger wing folds, add anatomically fitted wing-finger/membrane controls and
inspect weights at shoulder/elbow transitions. Bake resulting poses for SMILE;
do not assume Blender constraints are runtime features.
Sources: [Blender IK](https://docs.blender.org/manual/es/latest/animation/constraints/tracking/ik_solver.html),
[Rigify components](https://docs.blender.org/manual/en/latest/addons/rigify/rig_types/index.html).

Unity explicitly treats a dragon as a Generic model, with a chosen Root node.
Use Generic import and verify root-motion policy; a Humanoid avatar is not the
appropriate mapping. This is an import workflow, not a replacement for skinning
or producing compatible creature motion.
[Unity Generic animation](https://docs.unity3d.com/6000.0/Documentation/Manual/GenericAnimations.html).

Unreal's IK Retargeter maps chains and supports different bone counts/names.
Use matching source/target creature chains, fit their reference poses, map root
motion, then inspect foot contact and wing reach. The supplied chain JSON is a
starting map, not an already-created Unreal IK Rig asset. No pre-made animation
was imported in this final refinement. Existing original clips avoided a new
licensing or incompatible-skeleton dependency.
[Unreal IK retargeting](https://dev.epicgames.com/documentation/en-us/unreal-engine/ik-rig-animation-retargeting-in-unreal-engine).

Blender integration is callable in this session. Unity and Unreal connectors
were not exposed by the available tool inventory, so no Unity/Unreal execution
or retargeting validation is claimed. These are researched workflows, while the
actual delivered refinement was produced and checked in Blender and SMILE.

## Validation and limits

- Native Release DirectX Viewer compiled and published 42 assets; rebuilt
  executable installed at the stable Tools path and launched.
- Focused Character3DViewerHardeningTests passed, including final-pose policy,
  contact corrections, camera clearance, and camera-angle calculations.
- Native inspection verified Arin/Orin final Death at 3933 ms, the two Arin
  shield styles, Party close shots, and camera diagnostics.
- Orin importer bake reproduced source Root/Head/feet world poses at frames
  1/60/118 with maximum error below 0.000001.
- Dragon builder checked first/middle/final bounds of all six clips. Re-import
  checks for Idle/Fireball/ClawStrike/Hit retained planted feet and floor contact;
  `animation-refinement-validation.json` records the measurements.
- Six changed SMILE files passed the formatting check; both live calibration
  exports preserved their key counts. No compiler syntax or ABI was added here.

The available native UI tool cannot synthesize a held middle-button drag, so
that particular gesture still needs human visual confirmation. Existing source
model openings/angular membranes remain. This is a lightweight Dragon rig,
without foot IK, new wing fingers, flight or collision-aware deformation.

The earlier M7H-F report remains authoritative about unperformed formal GPU
benchmarks, long soaks and production acceptance. These refinements do not claim
RTX saturation, full volumetric lightning, all Blender transform features or Web
visual parity. VSIX 2.0.59 was previously installed and verified; these asset/tool
source changes do not alter its payload. Artifact: `artifacts/vsix/Smile.VisualStudio.vsix`.

## Session handoff and stream policy

Facebook Live was ended and the user confirmed it. YouTube and Meld shutdown
are the final operational steps after validation/push. Their actual outcomes
are reported in the chat. The user canceled the restart instruction: **do not
restart the computer**. A Codex task completion can surface through the user's
app notification settings; no external email/message notification was sent.

The companion ZIP packages this Start Here handoff, the character repair notes,
current manifests/calibration, Dragon chain map and relevant source companions.
Large source FBX/GLB/Blender assets remain in their committed canonical folders.
Unrelated image edits, older Orin revisions and source experiments remain intact.
