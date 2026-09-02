# M7D-A Paladin Combat Presentation Implementation

## Status and reconciliation

- Milestone: M7D-A — Paladin combat presentation and technical acceptance.
- Status: Implemented and validated; production release remains disabled.
- Repository: `D:\SMILE 2.0`.
- Branch: `main`.
- Starting commit: `bbda6efb7da08e80edab775d59f42331f1b85dc5`.
- Ending commit: the M7D-A feature commit containing this report; the final task report records its exact SHA-256 Git object ID.
- Reviewed handoff baseline: `d16d9630eb31f5a4f2cb2bab1b23a2c32578c1f2`.
- Reconciliation: local `main` was already a clean, newer descendant because M7C-B.1 had been completed and pushed as `bbda6efb7da08e80edab775d59f42331f1b85dc5`. No reset, restore, clean, rebase, amend, squash, or unrelated-work discard was used.

The stable identity remains `sin-star-i.character-1.paladin`; the display name is Arin, the party role is Paladin, the candidate version is v5.4, and `dragonfall.arin-v5-4` remains only an integration alias. Classic remains the release fallback.

## Implemented

### Renderer3D metadata and tooling ABI

All existing command IDs remain unchanged. The verified ranges after this milestone are:

| ABI family | Range | M7D-A additions |
|---|---:|---|
| Numeric `Renderer3D` | 1 through 124 | 124 `SetModelAnimatorTime` |
| Image `Renderer3DImage` | 1 through 2 | None |
| Text command / text-value selector | 1 through 12 | 10 clip name, 11 socket name, 12 event name |

`Renderer3DTextValue` is a shared-language Text-returning builtin routed through the MASM native emitter, native runtime, Web emitter, and WebGL2 runtime. Returned clip, socket, and event names are exact UTF-8 values bounded to 1,024 bytes.

Numeric command 98 (`ModelAnimationValue`) now exposes these append-only properties:

| Property | Meaning | Indexing |
|---:|---|---|
| 14 | clip sample count | zero-based clip |
| 15 | clip loop recommendation | zero-based clip |
| 16 | clip event count | zero-based clip |
| 17 | event clip index | one-based event |
| 18 | event time in milliseconds | one-based event |
| 19 | socket node index | zero-based socket |

Command 124 seeks only a valid model animator with an active clip and an in-range time. A successful seek cancels any crossfade, clears queued and dropped event state, clears accumulated root delta/remainders, and evaluates the requested pose without fabricating events. Invalid seeks are rejected without changing the animator.

`Smile.Simple3D.Graphics3D` and `Character3D` now expose clip names, durations, sample rates/counts, loop recommendations, event counts/names/clip indices/times/values, socket names/node indices, animation time, and safe animation seek.

### Character Viewer timeline

The reusable Character 3D Viewer now reads the actual cooked metadata rather than relying on a parallel name table. It includes a responsive timeline/event inspector with pause/resume, click-to-seek, frame stepping, previous/next event navigation, exact clip and event display, and selected/all socket inspection. Existing responsive layout, camera, window-placement, material-channel, Classic/candidate, native, and Web behavior is preserved.

### Events and sockets

The deterministic Arin v5.4 descriptor and cook contain eight presentation-only events:

| Clip | Time (ms) | Event | Value |
|---|---:|---|---:|
| Walk | 200 | FootstepLeft | 2001 |
| Walk | 766 | FootstepRight | 2002 |
| Run | 117 | FootstepLeft | 2001 |
| Run | 483 | FootstepRight | 2002 |
| SwordAttack | 300 | SwordTrailOn | 1001 |
| SwordAttack | 633 | SwordImpact | 1002 |
| SwordAttack | 967 | SwordTrailOff | 1003 |
| ShieldBashCandidate | 500 | ShieldImpact | 1101 |

The cook contains the required ten sockets. Cooked order is deterministic and alphabetical: Chest, FootLeft, FootRight, HandLeft, HandRight, Head, Root, ShieldCenter, SwordBase, SwordTip. SwordBase and SwordTip follow `R_Hand`; ShieldCenter follows `L_Hand`. All ten enumerate by exact name and expose their cooked node indices.

### Sin Star I Paladin Combat Presentation Lab

`games\SinStarI\PaladinCombatLab.smileproj` is the canonical presentation-only review surface. It uses automatic Model3D cooking and exercises Idle, Ready, Run, Sword Attack, Shield Bash candidate, Defend, Block Impact, Hit, KO, and Victory. It includes:

- an explicit authoritative presentation cue separate from gameplay authority;
- socket-following sword ribbon and layered sword/shield impacts;
- caller-owned footstep/audio cue mapping;
- camera presets, bounded shake, transient local light, Renderer2D flash, and presentation-only hit-stop;
- HDR/direct-LDR compatible presentation paths;
- timeline seek/event navigation, socket visualization, and material-channel inspection.

Animation events, VFX, camera, audio, and frame cadence never authorize damage or battle state.

`Effects3D.EffectPreset.RibbonRadius` is a small reusable addition. Existing presets retain the legacy radius of 170 when no radius is supplied; the Combat Lab requests 18 for Arin's meter-scale sword trail.

## Animation and deformation review

All eleven clips enumerate and were reviewed. Idle, Walk, Run, Ready, SwordAttack, Defend, BlockImpact, Hit, KO, and Victory pass the current candidate technical presentation gate. KO's final held pose reaches the ground. Run contains authored forward travel and therefore uses a subject-relative wide/follow camera. ShieldBashCandidate remains honestly named and is retained for revision rather than accepted for production.

The machine-readable record is `docs\implementation\paladin-combat-presentation-m7d-a.review.json`. It keeps release disabled and records the per-clip decision, deformation evidence, exact events, exact sockets, authority boundary, and production blockers.

## Assets, resources, and ownership

### Arin v5.4 cook

- Source GLB SHA-256: `D080754339ABD4F3F4CFBCAF4F26146631BDEEE30DD2EAA284682EF896B16CA3`.
- Cooked SM3D SHA-256: `B8E1C98A5F3162DF61BEC978688890B7FE35ABE7645C5877FB9A247CCE490394`.
- Cooked bytes: 863,720.
- Parts: 4; vertices: 7,375; indices: 30,888; triangles: 10,296.
- Materials: 4; texture references: 9.
- Bones: 42; nodes: 46; clips: 11; events: 8; sockets: 10.
- Animation bytes: 383,304; static bytes: 480,416.

### Current bounded resource contracts

| Resource | Limit | Ownership/lifecycle |
|---|---:|---|
| Meshes | 128 | Direct meshes are caller-owned. A loaded model owns its part meshes. Destroy dependent objects before direct meshes; model part meshes release with the model. |
| Objects | 1,024 | Caller/adapter-owned instances referencing a mesh, optional material, and optional animator. Destroy objects before those dependencies. |
| Models | 64 | Own part meshes, animation/string payload, and prepared PBR materials/textures. Destroy part objects and model animators first. |
| Textures | 128 | Direct textures are caller-owned; prepared model textures are model-owned. Materials retain references. |
| Materials | 128 | Direct materials are caller-owned; prepared model materials are model-owned. Objects retain references. |
| Legacy skeletons | 64 | Caller-owned; referenced by legacy clips/animators. Legacy skeletons allow 32 bones. |
| Legacy clips | 128 | Caller-owned; referenced by legacy animators. Legacy clips allow 16 events. |
| Animators | 128 total | Caller/Character3D-owned mutable instances; model animators retain their model. |
| Model animation | 256 nodes, 128 bones, 64 clips, 64 sockets | Payload is model-owned; per-clip events are bounded to 64 in SM3D v2, with 32 pending runtime events. |
| Model geometry | 16 parts, 131,072 aggregate vertices, 393,216 aggregate indices | Each part remains bounded to 65,535 vertices and 196,608 indices. |
| Model payload | 16 MiB SM3D; 64 materials; 128 textures; 32 chunks | Model load/prepare is bounded and transactional. |
| Frame submission | 512 submissions and 512 palette snapshots | Frame-owned transient records, reset at frame completion. |

`Character3D` retains its 16-entry shared asset cache and 32 actor instances, with at most 16 objects per actor. Each asset entry owns one model and its prepared PBR resources (or fallback material). Each character owns one animator and its part objects; destruction releases objects, animator, and the shared reference in dependency order.

Dragonfall remains unchanged: its declared boss-phase high-water mark is 448 objects with 64 objects of required headroom, 48 meshes, 24 materials, and 6 textures. `Smile.Battle3D` remains bounded to 12 actors, 256 commands, 32 camera shots, 32 effect presets, and 128 presentation particles.

### Observed presentation cost

- Ready/run baseline: 6 draws and 10,310 submitted triangles.
- Sword ribbon/impact peak evidence: 8 draws and 10,476 triangles.
- Shield impact: 7 draws and 10,390 triangles.
- All-socket inspection: 7 draws and 10,330 triangles.
- Direct3D 11 and WebGL2 use the same cooked bytes. Web reports only the expected MSAA-reduced fallback for the High profile.

## Validation

The following focused gates passed:

- `scripts\build.cmd` — compiler, native runtime, tests, AssetTool, and VSIX build passed; only the existing NU1503 C++ restore warning was emitted.
- `scripts\test-smile-formatter.ps1` — 13 formatter tests passed.
- `scripts\format-smile-style.ps1 -Check -FormatLongIf` — repository SMILE style check passed.
- `scripts\test-model3d-asset-cooking.ps1` — deterministic automatic cooking passed.
- `scripts\test-renderer3d-v2-boundaries.ps1` — exact and over-limit v2 boundaries passed.
- `scripts\test-renderer3d-models.ps1` — native/Web model validation passed.
- `scripts\test-renderer3d-pbr-hardening.ps1` — native/Web PBR hardening passed.
- `scripts\test-renderer3d-animation-v2-hardening.ps1` — native/Web animation timing, seek, metadata, events, deformation, memory, and lifecycle passed.
- `scripts\test-character3d.ps1` — native/Web Character3D passed.
- `scripts\test-renderer3d-vfx-hardening.ps1` — VFX hardening passed.
- `scripts\test-effects3d.ps1` — Effects3D and calibrated ribbon radius passed.
- `scripts\test-model3d-metadata-enumeration.ps1` — exact name/metadata/seek ABI parity passed.
- `scripts\test-paladin-animation-events-sockets.ps1` — deterministic cook, 11 clips, 8 events, and 10 sockets passed.
- `scripts\test-sin-star-paladin-combat-lab.ps1` — native/Web lab compilation, exact-console parity, and true-PNG evidence gate passed.
- `scripts\test-character-3d-viewer-hardening.ps1` — native/Web Viewer hardening passed.
- `scripts\test-paladin-v5-4-viewer-export-hardening.ps1` — two byte-identical Blender exports/cooks and retained Viewer evidence passed.
- `scripts\test-dragonfall-character-generation-2.ps1` — adapter, lifecycle, 100-restart, crowd/demo, native/Web, and no-demo validation passed.
- `scripts\test-dragonfall-arin-prototype.ps1` — source preservation, deterministic preparation, boundary, animation, Viewer, adapter, native/Web, and fallback validation passed.
- `scripts\test-dragonfall.ps1` — native/Web mechanics, lifecycle, demo, and no-demo validation passed.
- `scripts\test-battle3d.ps1` — native/Web Battle3D validation passed.
- `scripts\test-simple3d-space-wars.ps1` — Simple3D and Space Wars native/Web validation passed.
- `scripts\test-true-simple3d-neon-cycles.ps1` — true Simple3D and Neon Cycles native/Web validation passed.
- `dotnet run --project src\Smile.Tests\Smile.Tests.csproj -c Release` — 294 tests passed; printed compiler errors are intentional negative-test diagnostics.
- `scripts\verify-artifacts.ps1` — artifact/version verification passed.
- `scripts\smoke-test.cmd` — full retained smoke suite passed.

The first retained Viewer run correctly detected that the committed classic preparation manifest still named the previous repository-built AssetTool hash. Republishing with the current repository tool changed only that provenance hash; every prepared GLTF, binary, texture, and cooked-model hash remained identical. The check and both dependent Viewer gates then passed.

The first direct artifact-verification run also exposed three stale 2.0.57 regular expressions after the deliberate 2.0.58 VSIX version bump. The verifier was synchronized to 2.0.58, rerun successfully, and the full smoke suite independently repeated that successful verification.

Native manual review covered all required states, exact timeline seeking, socket overlays, material channels, camera presentation, ground contact, sword ribbon, and layered impacts. Web manual review covered SwordAttack and ShieldBashCandidate with the same cook, exact events, responsive UI, and clean console apart from the expected Web MSAA fallback indication.

## Evidence

Evidence is under `docs\implementation\screenshots\m7d-paladin-combat-presentation`. The authoritative per-image dimensions, byte sizes, SHA-256 values, clip/time, event, socket, quality profile, draw/triangle counts, significance, and known issues are in `screenshot-index.md`.

- Native PNGs: 1,282 by 752.
- Web PNGs: 1,280 by 720.
- Native/Web comparison: 2,560 by 800.
- iPhone contact sheet: 1,170 by 2,532.
- Every required file begins with the PNG signature and passed the repository evidence gate.

## VSIX

- Version: 2.0.58; assembly version 2.0.58.0.
- Artifact: `artifacts\vsix\Smile.VisualStudio.vsix`.
- Built VSIX SHA-256: `E65304F0C611A873F4714B20FE89D5BAF2F12C637C6AA23333E0B98C148BD65E`.
- Built DLL SHA-256: `F5DDB2CAFAB41015CC2C761670E892FA1C1E4F5FBBCCD2F7AE5918AD55B4E9ED`.
- Installed DLL SHA-256: `F5DDB2CAFAB41015CC2C761670E892FA1C1E4F5FBBCCD2F7AE5918AD55B4E9ED`.
- Installed and verified in Visual Studio instance `91f001b5` using the repository scripts.
- Visual Studio restart is required to load the refreshed extension.

## Plan mapping and deviations

- The plan expected missing exact-name enumeration and tooling seek. They were absent at current HEAD, so the smallest reusable append-only ABI was added with native/Web parity; no existing command was renumbered.
- The name-return path is a dedicated Text-returning builtin because the existing `Renderer3DText` ABI returns a Number and cannot safely return exact UTF-8 text. The selector range remains shared and append-only.
- The timeline is implemented in both the neutral Character 3D Viewer and the canonical Sin Star I lab instead of creating a third inspection executable.
- The required socket order in prose is treated as a required set; deterministic cooked order remains alphabetical. Lookup by exact name is the stable contract.
- ShieldBashCandidate is not promoted. Its event/socket/VFX contract is proven, but the motion remains candidate content requiring revision or explicit production acceptance.
- The current source contains 1K lossy JPEG textures, not the required 2K lossless production set. M7D-A therefore does not pretend to complete M7D-B.
- No PBR model, new animation system, IK, retargeting, morph targets, cloth, runtime GLB loading, WebGPU, battle mechanics, or M8 work was added.

## Production blockers and next phase

M7D-A is technically complete. M7D-B is asset-gated and remains blocked by:

1. complete source-service provenance, export, and rights evidence;
2. 2K lossless production textures or a newer source asset containing them;
3. Shield Bash production acceptance or a revised source animation;
4. final native/Web deformation acceptance;
5. explicit user approval to enable production release.

Until all five are resolved, v5.4 remains Candidate, release remains disabled, and Classic remains available. M8 is not started.
