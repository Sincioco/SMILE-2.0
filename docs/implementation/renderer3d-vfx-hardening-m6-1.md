# Renderer3D VFX Generation 2 Hardening — M6.1

Status: implemented, validated, installed, and ready for a separate M6.1 commit on 2026-09-01 (Asia/Taipei).

Starting baseline: `5f042ae5ff6a05f9e27e03b5733d782a340cb501` on `main`, equal to `origin/main`. The pre-existing untracked `docs/plans/` tree is preserved and excluded. M5.1 (`8b8fbe1bd095652a56a4657fd9b9f1c3caf59689`) and M6 (`5f042ae5ff6a05f9e27e03b5733d782a340cb501`) were independently confirmed in the local and remote history before M6.1 work began.

The supplied package `2026-09-01-03-smile-2.0-m6-1-vfx-hardening.zip` was recovered from `C:\Users\louie\Downloads`, verified at SHA-256 `FBDFF99F2DD0CCE750D0136B1F919B9B1A0B1FA7193F9FA4CDD4737157903C81`, safely extracted, read in its required order, and checked against every manifest hash. The M7 package was identified but deliberately not opened before M6.1 validation and push.

## Scope and reconciliation

M6.1 hardens the existing M6 public surface. It adds no Renderer3D command ID, SM3D format revision, PBR feature, animation feature, VFX graph, game-specific native helper, or Dragonfall migration.

The actual branch differed from the handoff in these important ways:

- M6 already had fixed batch pools, immutable tagged submissions, fixed 10 ms simulation, capacity diagnostics, HDR/direct-LDR, alpha/additive blending, socket attachment, and a deterministic VFX atlas.
- M6 still used one mutable CPU array as both staging and committed batch data. It rejected all staging writes while a revision was in flight.
- `Effects3D.Initialize` destroyed the current system before all replacement batches were known-good.
- transient light/audio messages were single-slot newest-wins values rather than bounded FIFO requests.
- the existing Web context listeners invalidated batch GPU handles, but no lazy committed-revision reupload path existed.
- the existing renderer already exposed draw calls and triangles; M6.1 did not add a duplicate diagnostic system.

The implementation therefore uses the existing architecture and makes only the smallest reusable changes: separate staging and committed CPU storage, transactional replacement, fixed FIFO request arrays, additional M6 resource queries, lazy Web batch restoration, and one focused hardening gate.

## Command ABI and dispatch paths

The command ABI is unchanged from M6:

- numeric `Renderer3D`: 1-121 inclusive; next free numeric command 122;
- image `Renderer3DImage`: 1-2 inclusive; next free image command 3;
- text `Renderer3DText`: 1-9 inclusive; next free text command 10.

Numeric commands 119-121 remain:

| ID | Command | Contract |
| ---: | --- | --- |
| 119 | `PARTICLE_BATCH` | create, stage transform/frame, stage color, commit, draw, destroy, validate |
| 120 | `RIBBON_BATCH` | create, stage left/right point/U, stage color, commit, draw, destroy, validate |
| 121 | `M6_VALUE` | global and resource-scoped VFX diagnostics |

M6.1 extends command 121's query values without consuming a command ID:

| Query | Meaning |
| ---: | --- |
| 37 | current staging revision |
| 38 | GPU-uploaded committed revision |
| 39 | resource state: empty `1`, committed `3`, in-flight `7` |
| 40 | pending destruction; always `0` because the existing explicit contract rejects destroy while in flight |
| 41 | committed payload bytes for the active prefix |

The complete dispatch paths remain:

| Layer | Path |
| --- | --- |
| Public facade/constants | `libraries/Smile.Simple3D/Graphics3D.smile` |
| Built-in syntax, arity, types | `src/Smile.Language/Syntax.cs`, `src/Smile.Language/Semantics.cs` |
| Native lowering | `src/Smile.Compiler/MasmEmitter.cs` -> `smile_renderer3d_command`, `smile_renderer3d_image_command`, `smile_renderer3d_text_command` |
| Native ABI | `src/Smile.NativeRuntime/graphics/graphics3d.h` |
| Native numeric/image/D3D11 dispatch | `src/Smile.NativeRuntime/graphics/graphics3d_directx.cpp` |
| Native text/asset resolution | `src/Smile.NativeRuntime/runtime.c` |
| Web lowering | `src/Smile.Compiler/WebEmitter.cs` -> `smile.renderer3D`, `smile.renderer3DImage`, awaited `smile.renderer3DText` |
| WebGL2 and all Web dispatch | `src/Smile.Compiler/WebOutputWriter.cs` |

The source-level bridge shapes are still one numeric command plus ten numeric payload arguments, one image plus eight numeric payload arguments, and one text plus eight numeric payload arguments respectively.

## Resource limits and ownership

Native resources use typed generation handles; Web resources use monotonically increasing safe integers and Maps. Both enforce the same logical limits and ownership.

| Resource | Current limit | Owner and dependency rule |
| --- | ---: | --- |
| meshes | 128; 65,535 vertices and 196,608 indices each | caller- or model-owned; objects and immutable submissions retain them |
| objects | 512 | caller-owned; borrow mesh/material/animator |
| models | 64; 16 parts, 131,072 vertices, 393,216 indices, 64 materials, 128 textures, 32 chunks, 16 MiB | model owns part meshes and prepared imported resources |
| textures | 128; up to 8,192 x 8,192 | caller- or model-owned; materials/submissions retain them |
| materials | 128 | caller- or model-owned; objects and VFX batches retain them |
| skeletons | 64; 32 legacy bones | caller-owned; clips/legacy animators refer to them |
| clips | 128; 16 legacy events | caller-owned; playing animators retain them |
| animators | 128 total | caller-owned; objects refer and frame palettes snapshot |
| production animation | 256 nodes, 128 bones, 64 clips, 64 sockets, 32 pending events | model/animator fixed storage |
| local lights | 4 | scene-owned renderer state |
| frame submissions/palettes | 512 / 512 | renderer-owned immutable fixed arrays |
| particle batches | 16; 1-4,096 each; 8,192 total capacity | caller-owned; borrow one alpha/additive material; own staging, committed CPU storage, and one dynamic GPU buffer |
| ribbon batches | 16; 2-1,024 points each; 2,048 total capacity | caller-owned; borrow one alpha/additive material; own point staging, two vertex arrays, and one dynamic GPU buffer |
| Effects3D presets | 64 x 8 emitter layers | library-owned fixed definitions |
| Effects3D active effects | 64 | library-owned generation-safe slots |
| Effects3D particles | Low 256, Medium 1,024, High 2,048 | library-owned fixed simulation/reservation slots |
| Effects3D impulses | 32 | library-owned fixed camera-shake slots |
| transient light/audio requests | 32 / 32 | library-owned FIFO queues; the caller still owns actual lighting and audio playback |

M6.1's committed/staging isolation intentionally changes VFX CPU accounting:

- particle batch: 96 CPU bytes and 48 GPU bytes per capacity slot;
- ribbon batch: 188 CPU bytes and 72 GPU bytes per capacity point;
- fixed particle quad: 76 GPU bytes;
- maximum global VFX batch reservation: 1,171,456 CPU bytes and 540,748 GPU bytes;
- one High Effects3D system's two 2,048-particle batches plus 256-point ribbon: 441,344 CPU bytes and 215,116 GPU bytes, excluding the application-owned atlas and Effects3D fixed simulation arrays.

Setters mutate only staging storage, including while committed revision N is retained by a submission. Commit remains prohibited while in flight; a successful commit publishes N+1 atomically. A failed commit keeps N's count, bytes, revision, GPU contents, and submitted output. Destroy remains an explicit failure while in flight, so no hidden deferred-destruction queue was added. End, rollback, reset, and device/context loss release retained references in the existing dependency order.

Dragonfall remains on its existing bounded Generation 1 scene/effect ownership. M6.1 changes no Dragonfall source or battle-state behavior.

## Rendering contract

- particle batches use one immutable quad and instanced draws; ribbon batches use one triangle strip;
- alpha uses source-alpha/one-minus-source-alpha and additive uses source-alpha/one;
- VFX depth testing is enabled with depth writes disabled;
- VFX does not cast or receive shadows and is skipped by the shadow pass;
- submissions render in caller declaration order through the immutable queue; Effects3D submits alpha, additive, then ribbon in one three-command atomic group;
- post-processing runs after the 3D submissions; Renderer2D composition remains post-exempt;
- no alpha-sort flag exists, so camera-distance alpha sorting and camera-sort updates are not applicable to the current API;
- the atlas is a deterministic 4 x 4 repository fixture. Frames are lifetime-clamped to the declared range; separate loop/hold/disappear modes and gutter metadata are not exposed by M6 and are therefore not applicable to M6.1;
- ribbons receive explicit bounded left/right points. Retained history, joins/miters, wrap/break/teleport classification, and zero-length-derived tangent generation are not part of this low-level ABI, so their proposed special cases are not applicable. Duplicate finite points remain bounded and safe.

## Audit matrix

| Area | Status | Evidence |
| --- | --- | --- |
| local M5.1/M6 commits and reports | Already correct and covered | exact commits/reports present on `main` and `origin/main` |
| unrelated working tree | Already correct and covered | only user-owned untracked `docs/plans/`; preserved/excluded |
| separate staging/committed revisions | Confirmed and fixed | native arrays/pointers and Web typed arrays are independent; queries 37-41 |
| mutate staging during in-flight revision | Confirmed and fixed | setters accept staging writes; commit/destroy still reject in flight |
| failed commit preserves last-good | Confirmed and fixed | upload-before-swap and native/Web exact-parity assertions |
| frame failure/reference release | Already correct and covered | M5.1 rollback/End/reset paths retained; post-hardening gate green |
| transactional initialization | Confirmed and fixed | candidate alpha/additive/ribbon batches publish only after all succeed |
| failure at each creation stage | Confirmed and fixed | first particle, second particle, and ribbon exhaustion cases preserve last-good quality/system |
| repeated lifecycle | Confirmed and fixed | ten complete Initialize/Shutdown cycles plus reset/stale-handle cleanup |
| fixed-step determinism | Already correct and covered | fixed 10 ms; 100x1, 10x10, 5x20, 2x50, 1x100 partition hashes match |
| seed and catch-up bounds | Already correct and covered | same/different seed, 250 ms clamp, 25-step cap retained |
| atomic composed-effect reservation | Confirmed and fixed | effect, particles, impulse, light, and audio capacity preflight before mutation |
| impulse/light/audio ordering | Confirmed and fixed | 32-entry bounded FIFO request queues; one-over rejection has no partial mutation |
| quality transitions | Confirmed and fixed | requested/effective diagnostics; Low/High replacement rollback and success covered |
| invalid socket/actor | Confirmed and fixed | invalid move stops effect and increments attachment-loss diagnostic |
| blend/depth/order/shadow/HDR | Already correct and covered | matched native/Web paths and retained M5/M5.1/M6 gates |
| alpha sorting | Not applicable, with evidence | no public sort mode or retained per-particle camera-distance key |
| flipbook mode/gutters | Not applicable, with evidence | current ABI exposes atlas dimensions/frame only; fixture/check validates 4 x 4 cells |
| derived ribbon history/teleport/miters | Not applicable, with evidence | public ABI accepts already-expanded left/right points only |
| WebGL restoration | Confirmed and fixed | one listener pair; loss invalidates GPU handles; lazy helpers recreate and upload committed revision exactly once |
| Web/native hot path allocation | Already correct and covered | PBR/M5.1/M6/M6.1 source guards pass after initialization helpers were kept outside audited draw regions |
| asset path security | Already correct and covered | strict relative path/character validation and current malformed-input gates retained |
| battle/Generation 1 isolation | Already correct and covered | Battle3D/Dragonfall/Simple3D native/Web gates and full smoke pass unchanged |

## Focused and retained validation

Final results:

| Gate | Exact result |
| --- | --- |
| `cmd /c scripts\build.cmd` | PASS, exit 0; compiler, AssetTool, runtime, tests, and VSIX. Only established native-project `NU1503` restore warning. |
| formatter integration/check | PASS; 13 groups and 337 repository SMILE files |
| `test-renderer3d-m11-hardening.ps1` | PASS under current PowerShell; M1.1 scene/material/input/atomic-publication checks |
| v2 boundaries/models | PASS; exact/one-over limits, deterministic GLB/glTF conversion, invalid fixtures, native/Web parity |
| lifecycle/materials/legacy animation | PASS native/Web exact parity |
| PBR/PBR hardening | PASS native/Web parity, failure/fallback, ownership, transform, skinning, lifecycle |
| animation v2/hardening | PASS native/Web 128-bone, playback, crossfade, events, root, sockets, irregular/fractional timing |
| Character3D/Scene3D | PASS native/Web cache, ownership, atomicity, animation, sockets, rendering, fallback, Lab builds |
| post-processing/M5.1 | PASS native/Web queue, snapshots, groups, shadow, targets, color, hot path |
| M6 batch gate | PASS; native/Web queue/lifecycle, 1,024-instance, HDR/direct-LDR, hot path; final native runtime 933 ms |
| Effects3D gate | PASS deterministic seed, partition, quality, exhaustion, stop/reset, native/Web parity |
| M6.1 hardening gate | PASS revision isolation, transactional lifecycle, determinism, request capacity, socket invalidation, restoration source contracts, hot path; nested final M6 runtime 942 ms |
| Battle3D | PASS native/Web exact parity |
| Dragonfall | PASS native/Web mechanics, lifecycle, demo, no-demo, balance, programs, assets |
| Simple3D/Space Wars | PASS package/state/gallery and native/Web demo/no-demo |
| True Simple3D/Neon Cycles | PASS conformance, state, native/Web |
| managed suite | PASS; 288 tests. Printed synthetic errors are intentional negative cases. |
| artifact verification | PASS; VSIX/compiler/templates and version 2.0.56 synchronized |
| `cmd /c scripts\smoke-test.cmd` | PASS, exit 0; full toolchain/build/managed/formatter/native/runtime/library/application/game/Web/artifact matrix |
| manual native lab | PASS; current Direct3D build rendered effects/ribbon/HDR and readable diagnostics |
| manual Web lab | PASS; current Web build rendered effects/ribbon/stress/HDR, console log list empty |

Corrective findings were retained:

- the first changed-file style check found two files needing the repository formatter; both were formatted transactionally and the 337-file check then passed;
- an intermediate Effects3D expectation assumed newest-wins messages; the new FIFO correctly returned chronological request 10 rather than 8, so the assertion was corrected and exact native/Web parity passed;
- the initial Windows PowerShell 5.1 wrapper for the M1.1 gate threw a null-expression before compilation; direct invocation in the repository's current PowerShell passed immediately without a code change;
- the first PBR gate correctly found the new initialization-only VFX typed arrays inside its audited text region. The helpers were relocated before `renderer3DDrawPbr`; PBR, PBR hardening, M5.1, M6, and M6.1 then passed;
- artifact review found three stale 2.0.55 regular expressions behind already-updated 2.0.56 verifier text. The expressions were corrected before artifact verification and smoke, both of which passed.

## VSIX installation

The compiler/Web runtime is a VSIX payload, so the version was advanced to 2.0.56 and installed through the repository script.

- artifact: `artifacts\vsix\Smile.VisualStudio.vsix`
- bytes/SHA-256: 1,733,906 / `9BC73EDD0399D5969D89D30C9D87737BD75092A2B88B704C7A59368C96132005`
- installed instance: Visual Studio Enterprise `91f001b5`
- installed DLL: `C:\Users\louie\AppData\Local\Microsoft\VisualStudio\18.0_91f001b5\Extensions\wohumo5q.1gz\Smile.VisualStudio.dll`
- installed assembly version: 2.0.56.0
- installed DLL SHA-256: `1392BC98285D93FA8146DCC9D2DCE91DB868E8A65CCE5FB63823A981E3F269FB`
- installation result: verified; restart Visual Studio to load the refreshed extension

## Mobile-review evidence

- pre-existing committed screenshots: M6 evidence remains under `artifacts/screenshots/` in the M6 commit;
- new committed screenshots: `docs/implementation/evidence/m6-1-vfx/m6-1-01-native-vfx-lab.jpg`, `m6-1-02-web-vfx-lab.jpg`, `m6-1-03-sword-ribbon.jpg`, `m6-1-04-particle-stress.jpg`;
- artifact-only screenshots: none;
- contact sheet: `docs/implementation/evidence/m6-1-vfx/m6-1-mobile-contact-sheet.jpg`;
- notes: `docs/implementation/evidence/m6-1-vfx/m6-1-mobile-review-notes.md`;
- hashes: recorded in the notes file.

## M7 readiness

M6.1 itself is green. M7 may be evaluated only after this milestone is committed, pushed, and remote-verified. M7's separate production-character asset gate remains authoritative; M6.1 makes no claim that the required licensed GLB/descriptor/PBR texture package exists.

## PNG evidence follow-up

The later sequential execution instructions required committed PNG evidence under `docs/implementation/screenshots/m6-1-vfx-hardening/`. A separate, non-rewriting documentation follow-up preserves the original M6.1 commit and adds native, Web, 1,024-particle stress, sword-ribbon, HDR/bloom, and direct-LDR PNGs plus `screenshot-index.md`. The direct-LDR image was captured from the pushed Web build with HDR/bloom/format diagnostics at zero and an empty warning/error console log. Exact dimensions, sizes, hashes, and Dragonfall significance are recorded in the screenshot index.

## Command ledger

The substantive commands used for reconciliation, implementation, validation, evidence, installation, and Git verification were:

```powershell
cmd /c "git status -sb"
cmd /c "git branch --show-current"
cmd /c "git rev-parse HEAD"
cmd /c "git remote -v"
cmd /c "git fetch origin --prune"
cmd /c "git rev-parse origin/main"
cmd /c "git rev-list --left-right --count origin/main...HEAD"
cmd /c "git log --decorate --oneline -10"
Get-ChildItem C:\Users\louie\Downloads
Get-FileHash -Algorithm SHA256 <M6.1/M7 ZIP>
Expand-Archive <validated handoff> <repository temp path>
Get-FileHash -Algorithm SHA256 <manifest files>

cmd /c scripts\build.cmd
& .\scripts\test-smile-formatter.ps1
& .\scripts\format-smile-style.ps1 -Check -FormatLongIf
& .\scripts\test-renderer3d-m11-hardening.ps1
& .\scripts\test-renderer3d-v2-boundaries.ps1
& .\scripts\test-renderer3d-models.ps1
& .\scripts\test-renderer3d-lifecycle.ps1
& .\scripts\test-renderer3d-materials.ps1
& .\scripts\test-renderer3d-animation.ps1
& .\scripts\test-renderer3d-pbr.ps1
& .\scripts\test-renderer3d-pbr-hardening.ps1
& .\scripts\test-renderer3d-animation-v2.ps1
& .\scripts\test-renderer3d-animation-v2-hardening.ps1
& .\scripts\test-character3d.ps1
& .\scripts\test-renderer3d-post-processing.ps1
& .\scripts\test-renderer3d-post-processing-hardening.ps1
& .\scripts\test-renderer3d-vfx-batches.ps1
& .\scripts\test-effects3d.ps1
& .\scripts\test-renderer3d-vfx-hardening.ps1
& .\scripts\test-battle3d.ps1
& .\scripts\test-dragonfall.ps1
& .\scripts\test-simple3d-space-wars.ps1
& .\scripts\test-true-simple3d-neon-cycles.ps1
dotnet run --project src\Smile.Tests\Smile.Tests.csproj -c Release
& .\scripts\verify-artifacts.ps1
cmd /c scripts\smoke-test.cmd
cmd /c scripts\install-vsix.cmd
```
