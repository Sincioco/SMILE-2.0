# Native Equipment Fire Preview — Start Here

September 4, 2026. This is an incremental Character Viewer preview, **not completion
of the full M7E-G free-roam/trail/preset milestone**. Read this note, then the Viewer
README for controls and the canonical ArinV57 README for asset provenance.

## Changes

- Thermal fire follows the actual sword object's calibrated base/tip sockets.
  The stale sword descriptor pointed the old tip away from the mesh by 0.517122
  model units; mesh-derived endpoints correct that roughly sideways source.
- The sword outline is orange/gold under a fuller flame: 200% emission and
  blade-length/12 source radius. Low velocity inheritance leaves a stronger
  world-space wake, using the existing FireEmitter3D behavior and particle lifetime.
- Three quieter shield flame segments use actual shield perimeter vertices.
  Radius 3 and 75% emission replace the first radius-2/45% preview. The old shield
  golden mesh overlay and its legacy glow particles are no longer drawn while
  the thermal preview is available. This avoids its visible face-loss behavior.
- Normal loop wraps and clip changes no longer destroy the emitter. Existing
  particles dissipate while fresh fire starts at the new blade pose; transition-frame
  inheritance is zero. Paused pose edits still clear stale particles; explicit reset
  clears the emitters for a fresh start.
- Separate Sword Fire / Shield Fire buttons default on. Equipment visibility also
  hides its fire; reset restores both effects. Fire updates before scene submission.
- Pose Calibration adds **In Place**: choose Sword or Shield, Rotate, then X/Y/Z.
  It retains the current equipment hand-attachment point by compensating translation
  against the inverse XYZ rotation. Wrist values are untouched. Save Frame stores
  all resulting channels in the existing format; Cancel/Reload restores them.
  This editing mode is not a new keyframe channel. Whole-world-unit position
  precision and the existing +/-100-unit saved-position bounds still apply.
- Pose Calibration's Delete All Key Frames replaces the Reset Clip label, retaining
  current-clip-only scope and the Confirm Current Clip prompt. Save Frame and Cancel
  move to the lower-right; -5/+5 buttons are removed. The full clickable JSON path
  moves below the timeline in muted gray at the unchanged 9-point font size.
- Timeline < Frame / Frame > buttons step immediately and repeat every 300 ms
  while held, capturing the gesture until release without a catch-up burst.
- Flames animate by default while the scene is paused. Pause Flames / Play Flames
  controls the thermal simulation independently; Space controls scene movement.

## Preservation

The accepted GLB remains SHA-256
`393D82C06ECCEDF5A13CF3CA835700AA03A6E90ED74B1420569902885E3E1524`.
No model, rig, animation source, or texture was edited. Socket metadata belongs to
`games/SinStarI/SourceAssets/Characters/Paladin/ArinV57/ArinV57.sm3d.json`.
The user's three live saved keys were exported normally; no key was deleted by
these changes. Disposable fire atlases are copied into the ignored tool asset
directory by Prepare-BuildAssets.ps1.

## Focused Validation

- Native Debug Viewer compiled with 27 published assets at
  `tools/Character3DViewer/bin/Debug/Character3DViewer.exe`.
- Explicit Program.smile formatting check and `git diff --check` passed.
- A temporary native SMILE math check covered X=90, Y=-90, Z=180 and combined
  XYZ rotations; inverse compensation returned zero failures at 0.1-unit tolerance.
  This checks the rotation order, not end-to-end artist acceptance of the control.
- Mesh audit verified sword vertices bind rigidly to RightHand, shield vertices
  bind rigidly to LeftHand, and the new sword-tip coordinate matches a mesh vertex
  within 0.000000003 model units.
- Brief live preview rendered both equipment effects on GPU with error 0.
- `screenshots/m7e-g-arin-flaming-sword/01-arin-idle-flaming-sword.png`
  records the initial corrected sword-socket alignment, before the later shield
  and intensity adjustments. It is not evidence of the final shield tuning.
- The earlier 21:12 Debug build was running in the user's newly opened 21:14 viewer;
  the live status showed GPU backend 2/error 0 and the user was editing a pose.
  No unsaved user edit was interrupted for additional screenshots.
- The old all-purpose Viewer hardening script stops at its obsolete requirement
  for the removed IDLE_RESET_MILLISECONDS timer. It was not reported as passing;
  those unrelated legacy source assertions were not rewritten for this preview.
- No compiler/runtime/VSIX payload or shared Fire Lab preset changed here. The
  previously installed VSIX remains 2.0.59. Web editor work is deferred.

## Remaining Manual Review / Scope

Sin should review the increased shield intensity, loop/clip-transition trail fade,
independent flame pause, frame-button hold repeat and In Place rotation on a fitted
grip, then Save Frame if the correction is wanted.
The current wake consists of persistent flame particles, not the complete planned
event-driven swept blade ribbon. Free-roam demo, authored attack events, full
production preset pass and requested M7E-G endurance matrix remain later work.
