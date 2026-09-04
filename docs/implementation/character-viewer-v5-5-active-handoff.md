# SMILE 2.0 Character Viewer v5.5 Active Work Handoff

Date: September 3, 2026 (Asia/Taipei)

Status: Active, uncommitted work. Resume in `D:\SMILE 2.0` on `main`. Do not reset, restore, clean, or discard the dirty worktree. Use exactly one Codex agent.

## Resume objective

Finish the current Arin v5.5 and Character Viewer repair, run focused validation, reinstall the VSIX if any compiler/runtime payload changes after the recorded installation, then create a detailed `Sin and Codex:` commit and push it to `origin/main`.

## User interaction rules to retain

- Do not take mouse or keyboard control unless it is necessary and announced first.
- When foregrounding an application, keep the Codex window visible on the left.
- If Blender and the Character Viewer are both shown, divide the right side vertically: Blender on the upper-right and Character Viewer on the lower-right.
- Never send base64 image text or `data:` image URLs. Use normal rendered inline images only; otherwise provide the local image path.
- The user may reboot immediately after this handoff. On `resume`, begin with repository and process state inspection rather than assuming applications survived.

## Current repository baseline

- Repository: `D:\SMILE 2.0`
- Branch: `main`
- Last completed and pushed baseline: `42b7785733f218c188b8bfdd0d348e61d08f261b`
- Remote: `Sincioco/SMILE-2.0`
- Current changes are intentionally uncommitted and must be preserved.

Last known changed paths:

```text
 M docs/language/README.md
 M games/Dragonfall/Character3DViewer.smileproj
 M games/Dragonfall/Character3DViewerCooked.smileproj
 M games/Dragonfall/SourceAssets/Arin/Prepared/ArinPrototype.preparation-manifest.json
 D games/Dragonfall/TechnicalAssets/Generation2/CharacterViewerFlameAtlas.png
 D scripts/generate-character-viewer-flame-atlas.ps1
 M scripts/test-character-3d-viewer-hardening.ps1
 M src/Smile.Compiler/WebOutputWriter.cs
 M src/Smile.Language/Syntax.cs
 M src/Smile.NativeRuntime/runtime.c
 M src/Smile.Tests/Program.cs
 M tools/Character3DViewer/Program.smile
 M tools/Character3DViewer/README.md
```

Re-run `git status --short`, `git diff --stat`, and a focused `git diff` immediately after reboot. Preserve any additional user work.

## Completed but uncommitted work

### Removed rejected flame effect

- Removed the Character Viewer sword flame implementation and its generated atlas.
- Deleted `games/Dragonfall/TechnicalAssets/Generation2/CharacterViewerFlameAtlas.png`.
- Deleted `scripts/generate-character-viewer-flame-atlas.ps1`.
- Removed the flame atlas asset from both Character Viewer project files.
- Kept the existing white/cyan sword glow and gold shield glow temporarily.

### Background cycling

- `B` and the clickable `BG` button cycle `Black -> Green -> Purple -> Black`.
- Green is intentionally useful for silhouette and detached-mesh inspection.
- Native visual checks confirmed all three backgrounds.

### Pause and inspection behavior

- `P` and `Space` toggle the same full-scene pause.
- Pause is indefinite until explicitly resumed or reset. Do not restore the superseded two-minute timeout.
- While paused, mouse pan, zoom, orbit, background selection, frame stepping, and right-click reset remain active.
- While paused, animation, auto-orbit, automatic animation cycling, idle reset, and VFX history are frozen.
- Right-click reset resumes and restarts the presentation.
- Native visual checks confirmed pause, sustained frozen pose, zoom while paused, orbit while paused, and resume.

### Keyboard support

Reusable shared key constants were added:

- `KEY_P = 31`
- `KEY_B = 32`
- `KEY_CONTROL = 33`

They were added to the language, compiler/Web output, native runtime, tests, and documentation.

`Ctrl+Left` and `Ctrl+Right` are intended to step one animation frame backward/forward. The source path exists, but automated chord testing was inconclusive because the automation released Control before the queued arrow was consumed. Verify with a physical held-Control test after reboot.

### UI layout

- The animation section was moved down enough to remove the previous header overlap.
- New user evidence shows the animation button labels themselves still overflow or run together, especially `SwordAttack`, `ShieldBashCandidate`, and `BlockImpact`.
- Fix this by giving the two columns adequate width/gap, clipping or fitting text correctly, and keeping labels inside their buttons at all supported window sizes. Do not abbreviate the authored animation names unless unavoidable.

### VSIX already rebuilt and installed

- VSIX version: `2.0.58`
- Artifact: `D:\SMILE 2.0\artifacts\vsix\Smile.VisualStudio.vsix`
- Installed assembly: `C:\Users\louie\AppData\Local\Microsoft\VisualStudio\18.0_91f001b5\Extensions\kxq5yn1j.dc4\Smile.VisualStudio.dll`
- Installed assembly version: `2.0.58.0`
- SHA-256: `2FB996AF6F3E48CA4A6E4BB4FF96421F8453F963336F0767C99E4E723EAAB938`
- Visual Studio needs a restart to load that installation.
- Rebuild and reinstall again before completion only if compiler/runtime/VSIX payload changes after this handoff.

## Highest-priority unresolved defects

### 1. Arin's right sword arm, glove, and wrist

The defect is severe and visible across multiple clips:

- The empty forearm armor cuff follows the arm.
- The separate black sword-grip glove and sword move away from the cuff.
- The wrist and hand are visibly disconnected, sometimes by a large distance.
- The arm can look twisted.
- In Run, the palm faces outward/backward and holds the sword unnaturally.
- This is not a glow-only artifact; it is likely an attachment parenting/bone-space/rest-transform problem inherited from the v5.4 attachments and reused by the v5.5 builder.

Exact user-observed poses:

- Idle: `810 / 7567 ms` has a clear wrist gap.
- Idle: approximately `1183 / 7567 ms` and `4176 / 7567 ms` also show the separation/twist.
- Run: around `144 / 733 ms` and `609 / 733 ms` shows outward palm, odd grip, and wrist separation.
- Defend: approximately `1059 / 1433 ms` shows detached glove/sword.

Relevant assets and scripts:

- Canonical blend: `games/SinStarI/SourceAssets/Characters/Paladin/arin-integrated-candidate-v5.5.blend`
- Runtime source GLB: `games/Dragonfall/SourceAssets/Arin/arin-integrated-candidate-v5.5.glb`
- `scripts/build-arin-v5-5-candidate.ps1`
- `scripts/build-arin-v5-5-candidate.py`
- `scripts/build-arin-v5-5-candidate?` should not be guessed; inspect exact tracked names with `rg --files scripts | rg "arin-v5-5"`.
- `scripts/build-arin-v5-5-candidate.manifest.json`
- `scripts/export-arin-v5-5-viewer.ps1`
- Exporter currently reuses `scripts/export-v5-4-viewer.py` or similarly named v5.4 exporter; confirm exact path.

SM3D v5.5 part inventory:

```text
Part 0  ArinBody              6341 vertices  material 0
Part 1  ArinShield             242 vertices  material 1
Part 2  ArinSword              259 vertices  material 1
Part 3  ArinSwordGripGlove     534 vertices  material 1
Total                         7376 vertices, 10296 triangles
Bones 42, nodes 46, clips 11, sockets 10
```

The v5.5 manifest records the attachment pieces as:

```text
ArinSword           sourcePart tripo_part_8
ArinShield          sourcePart tripo_part_9
ArinSwordGripGlove  sourcePart tripo_part_5
```

The v5.5 build appears to reuse attachment objects from v5.4 and only replaces their material with the 2K equipment material. Inspect in Blender CLI before editing:

- object parent, parent type, parent bone;
- constraints and modifier targets;
- vertex groups and armature binding;
- object/world/rest matrices;
- right-hand/wrist bone names and transforms at the exact problem frames;
- whether the glove is rigid-parented to a hand bone while the arm uses retargeted animation with a different bone/rest space;
- whether the sword should follow a socket independent of the glove.

Repair this in the reusable v5.5 build/export pipeline, not only by editing a single exported binary. Add deterministic validation that samples all clips and detects implausible glove-to-wrist distance/orientation. Rebuild the canonical blend and runtime GLB/SM3D asset. Check every animation against the green background.

### 2. Mesh-fitted equipment glow

The current sword glow is a camera-facing ribbon/rectangle between SwordBase and SwordTip. It disappears edge-on and looks like a glowing bar through the blade. Replace it with a mesh-fitted outline using model part 2 (`ArinSword`).

The shield currently uses a duplicate model-part outline but is not uniformly visible at every angle. Draw slightly enlarged additive sword and shield duplicates behind the opaque character/equipment so only the silhouettes remain visible. Keep the sword outline white/cyan and shield outline gold. Make scale/opacity sufficient for a uniform edge without creating halos, ribbons, trails, flame leaves, or spider-web geometry.

Suggested small refactor:

- Add `ARIN_SWORD_PART_INDEX = 2`.
- Change `SwordGlow` from `Core.RibbonBatch3D` to `Core.Object3D` created with `CreateModelPart3D(ArinModel, ARIN_SWORD_PART_INDEX)`.
- Assign the same animator as the character.
- Compute sword center from the SwordBase/SwordTip sockets, scale the duplicate outward around that center, and draw it before the opaque character.
- Draw both equipment outlines before `Scene3D.Draw(Character)`; draw any safe short particle points after the character only if retained.
- Do not restore the rejected flame system.

### 3. Motion shakiness despite 120 FPS

120 FPS only proves presentation frequency. The likely camera cause is integer-degree quantization: an accumulated millidegree value is eventually assigned to an integer `OrbitYaw`, producing visible one-degree steps. Animation may also expose fixed-rate authored samples or integer-time sampling.

Inspect:

- `AdvanceAutoOrbit` and all conversions from `AutoOrbitYaw1000` to camera yaw;
- `Smile.Interaction3D` camera control storage and trig units;
- native/Web animator sampling and interpolation between keyframes;
- use of integer elapsed milliseconds with alternating 8/9 ms frames.

Fix the reusable camera/controller or timing path if necessary so orbiting and character motion are visually smooth without merely increasing reported FPS. Preserve the repository's permanent smooth-camera rule.

### 4. Zoom inspection limit

Increase the Character Viewer zoom-in limit from `-24 degrees` to approximately `-48 degrees` so the user can inspect defects closely. Keep existing wheel easing and bounds. Add/adjust hardening assertions and README documentation.

## Remaining UI and behavior work

- Fix animation button label placement/overflow at wide-screen and narrower supported sizes.
- Verify `Ctrl+Left` and `Ctrl+Right` physically while paused.
- Confirm `P` and Space pause indefinitely.
- Confirm no pause path auto-resumes.
- Confirm pan, wheel zoom, middle-button orbit, background cycling, and frame stepping stay usable while paused.
- Confirm right-click still resets view/camera, resumes, restarts auto-orbit, and restarts the five-second animation sequence.
- Preserve dynamic window adaptation and saved window geometry behavior already implemented in earlier commits.

## Validation already completed for the dirty worktree

- `scripts\build.cmd`: passed after P/B changes and again after KEY_CONTROL. Expected NU1503 warning for the C++ project restore.
- `dotnet run --project src\Smile.Tests\Smile.Tests.csproj -c Release --no-build`: `294 SMILE language, compiler, project, completion, and timing tests passed.` Expected synthetic failure diagnostics appear before the success line.
- Cooked native and Web Character Viewer builds: passed; 13 assets.
- `node --check game.js`: passed.
- `node --check smile-runtime.js`: passed.
- `scripts\test-renderer3d-vfx-batches.ps1`: passed native/Web parity, 1,024-instance path, and lab native/Web; native runtime 957 ms.
- `artifacts\tests\Smile.NativeGraphicsTests.exe`: `54 native graphics, pointer-input, and audio-focus checks passed.`
- `scripts\test-character-3d-viewer-hardening.ps1`: passed after regenerating the Arin preparation manifest.
- `scripts\format-smile-style.ps1 -Check -FormatLongIf` for the viewer: passed after rebuild.

The Arin preparation manifest changed because its recorded AssetTool hash changed after the shared language rebuild. The transactional preparation check passes with the updated manifest.

Re-run all relevant focused gates after the rig, glow, jitter, zoom, and UI fixes. At minimum:

```text
scripts\build.cmd
dotnet run --project src\Smile.Tests\Smile.Tests.csproj -c Release --no-build
scripts\format-smile-style.ps1 -Check -FormatLongIf
scripts\test-character-3d-viewer-hardening.ps1
scripts\test-renderer3d-vfx-batches.ps1
artifacts\tests\Smile.NativeGraphicsTests.exe
native and Web Character Viewer builds
node --check for both Web outputs
brief native launch with physical input and exact animation-frame inspection
```

Use only the light focused validation required by repository rules; no soak test is needed unless a specific intermittent jitter defect requires it. If a longer test becomes necessary, record the known problem, why, and stop condition first.

## Resume sequence

1. Read root `AGENTS.md` and this file.
2. Run `git status -sb`, `git diff --stat`, `git diff --check`, and inspect the focused diffs. Do not discard anything.
3. Check for running Character Viewer/Blender processes after reboot; assume none.
4. Finish the viewer-only zoom and animation-button layout fixes.
5. Use Blender CLI in the background to inspect v5.5 attachment parenting and exact failure frames without taking UI control.
6. Repair the sword-arm/glove binding in the reusable builder/export pipeline, rebuild all owned assets, and add deterministic continuity checks.
7. Replace the ribbon sword glow with mesh-part outlines and improve shield outline consistency.
8. Diagnose and fix the quantized motion path.
9. Run focused validation and install the VSIX again if shared compiler/runtime payload changed.
10. Warn the user before any foreground UI work. If approved/appropriate, show Blender upper-right and Character Viewer lower-right while Codex remains on the left; leave a corrected SwordAttack animation visibly playing.
11. Commit with a detailed public message beginning exactly `Sin and Codex:` and push `main` without rewriting history.
12. Verify `HEAD == origin/main`, ahead/behind is `0 0`, and report files, exact tests, VSIX, commit, and remaining manual checks.

## Scope exclusions

- Do not reintroduce the rejected fire/flame emitter.
- Do not start the Unity MCP/Asset Store task in this work item.
- Do not create subagents.
- Do not force-push, amend, rebase, reset, restore, or clean.

