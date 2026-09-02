# Renderer3D Visual Generation 2 M0 Reconciliation

## Status

Milestone M0 was reconciled and implemented on 2026-08-30 (Asia/Taipei). It records the current Renderer3D compatibility boundary without implementing SM3D v2, PBR, new animation, or VFX.

The requested plan package was supplied as an untracked, flattened `docs/plans/` tree rather than `docs/plans/smile-2.0-dragonfall-visual-generation-2-plan/`. It was read in the order from `docs/plans/read-me-first.md` and preserved as user-owned input; it is not part of the M0 commit.

## Repository reconciliation

| Item | Reconciled value |
|---|---|
| Repository | `Sincioco/SMILE-2.0` at `D:\SMILE 2.0` |
| Branch | `main` |
| Starting HEAD | `9aa9583a651eab452ea3af80772b08b68fc03220` |
| Upstream after fetch | `origin/main`, ahead 0, behind 0 |
| Plan baseline | `ec61dfa6324de7b22ea5ca0959828ff40e5e3902` |
| Drift | One descendant commit: `9aa9583` (`Sin and Codex: docs(branding): define the SMILE acronym`) |
| Renderer3D drift | None; the intervening commit changed branding/documentation only |
| Initial working tree | Only untracked `docs/plans/` supplied by the user |

Root `AGENTS.md` remains authoritative. The user also established a permanent rule during M0: when a change affects the VSIX or a bundled payload such as the compiler, the newly validated VSIX must be installed and verified before completion.

## Renderer3D bridge and dispatch paths

The source-level bridge shapes are fixed in `src/Smile.Language/Syntax.cs` and checked in `src/Smile.Language/Semantics.cs`:

- `Renderer3D(command, a, b, c, d, e, f, g, h, i, j)` — eleven `Number` arguments, returns `Number`.
- `Renderer3DImage(command, image, a, b, c, d, e, f, g, h)` — command, owned `Image`, and eight numbers, returns `Number`.
- `Renderer3DText(command, text, a, b, c, d, e, f, g, h)` — command, owned `Text`, and eight numbers, returns `Number`; Web emission awaits it.

The complete dispatch route is:

| Layer | Confirmed path |
|---|---|
| Public facade and mirrored constants | `libraries/Smile.Simple3D/Graphics3D.smile` |
| Built-in names, arity, and types | `src/Smile.Language/Syntax.cs`, `src/Smile.Language/Semantics.cs` |
| Native call emission | `src/Smile.Compiler/MasmEmitter.cs` -> `smile_renderer3d_command`, `smile_renderer3d_image_command`, or `smile_renderer3d_text_command` |
| Native ABI declarations | `src/Smile.NativeRuntime/graphics/graphics3d.h` |
| Native numeric/image dispatch and D3D11 resources | `src/Smile.NativeRuntime/graphics/graphics3d_directx.cpp` |
| Native text/asset-manifest dispatch | `src/Smile.NativeRuntime/runtime.c`; it resolves and consumes the text, then calls `smile_renderer3d_load_model_path` |
| Web call emission | `src/Smile.Compiler/WebEmitter.cs` -> `smile.renderer3D`, `smile.renderer3DImage`, or awaited `smile.renderer3DText` |
| WebGL2 resources and all three Web dispatch functions | `src/Smile.Compiler/WebOutputWriter.cs` |

### Numeric command ABI

Arguments not named in the table are ignored and must be zero in the public facade. Boolean/result operations use zero for false/failure and nonzero for true/success. Handles use zero for allocation/load failure.

| ID | Command | Positional arguments | Result |
|---:|---|---|---|
| 1 | `AVAILABLE` | none | Renderer3D availability |
| 2 | `RESET` | none | success after dependency-ordered reset |
| 3 | `CREATE_MESH` | `a=vertexCount, b=indexCount` | mesh handle |
| 4 | `SET_VERTEX` | `a=mesh, b=index, c=x, d=y, e=z` | success |
| 5 | `SET_TRIANGLE` | `a=mesh, b=triangleIndex, c=v0, d=v1, e=v2` | success |
| 6 | `COMMIT_MESH` | `a=mesh` | success |
| 7 | `CREATE_PRIMITIVE` | `a=kind, b=firstSize, c=secondSize, d=segments, e=rings` | mesh handle |
| 8 | `CREATE_OBJECT` | `a=mesh` | object handle |
| 9 | `DESTROY` | `a=typedHandle` | success; refuses a live dependent |
| 10 | `SET_CAMERA` | `a-c=position XYZ, d-f=target XYZ, g=FOV degrees, h=near, i=far` | success |
| 11 | `SET_POSITION` | `a=object, b-d=XYZ` | success |
| 12 | `SET_ROTATION` | `a=object, b-d=XYZ degrees` | success |
| 13 | `SET_SCALE` | `a=object, b-d=XYZ percent` | success |
| 14 | `SET_COLOR` | `a=object, b-d=RGB, e=opacity percent` | success |
| 15 | `SET_VISIBLE` | `a=object, b=visible` | success |
| 16 | `BEGIN` | `a-c=clear RGB` | success |
| 17 | `DRAW` | `a=object` | success |
| 18 | `END` | none | success |
| 19 | `MESH_VERTEX_COUNT` | `a=mesh` | vertex count |
| 20 | `MESH_INDEX_COUNT` | `a=mesh` | index count |
| 21 | `LAST_ERROR` | none | last Renderer3D error number |
| 22 | `LIVE_MESH_COUNT` | none | live meshes |
| 23 | `LIVE_OBJECT_COUNT` | none | live objects |
| 24 | `MAX_MESH_COUNT` | none | 128 |
| 25 | `MAX_OBJECT_COUNT` | none | 512 |
| 26 | `MESH_VALID` | `a=mesh` | validity |
| 27 | `OBJECT_VALID` | `a=object` | validity |
| 28 | `MESH_REFERENCE_COUNT` | `a=mesh` | referring objects |
| 29 | `CREATE_MATERIAL` | `a=texture, b=alphaMode, c-e=RGB, f=opacity, g=unlit, h=emissive, i=cutout` | material handle |
| 30 | `SET_OBJECT_MATERIAL` | `a=object, b=material or 0` | success |
| 31 | `SET_MESH_UV` | `a=mesh, b=index, c-d=UV thousandths` | success |
| 32 | `LIVE_TEXTURE_COUNT` | none | live textures |
| 33 | `LIVE_MATERIAL_COUNT` | none | live materials |
| 34 | `MAX_TEXTURE_COUNT` | none | 128 |
| 35 | `MAX_MATERIAL_COUNT` | none | 128 |
| 36 | `TEXTURE_VALID` | `a=texture` | validity |
| 37 | `MATERIAL_VALID` | `a=material` | validity |
| 38 | `TEXTURE_WIDTH` | `a=texture` | decoded width |
| 39 | `TEXTURE_HEIGHT` | `a=texture` | decoded height |
| 40 | `TEXTURE_REFERENCE_COUNT` | `a=texture` | referring materials |
| 41 | `MATERIAL_REFERENCE_COUNT` | `a=material` | referring objects |
| 42 | `SET_MATERIAL` | `a=material, b=alphaMode, c-e=RGB, f=opacity, g=unlit, h=emissive, i=cutout` | success |
| 43 | `SET_MESH_NORMAL` | `a=mesh, b=index, c-e=normal thousandths` | success |
| 44 | `LIVE_MODEL_COUNT` | none | live models |
| 45 | `MAX_MODEL_COUNT` | none | 64 |
| 46 | `MODEL_VALID` | `a=model` | validity |
| 47 | `MODEL_PART_COUNT` | `a=model` | part count |
| 48 | `MODEL_MATERIAL_COUNT` | `a=model` | material-slot count |
| 49 | `CREATE_MODEL_PART_OBJECT` | `a=model, b=partIndex` | object sharing the model-owned mesh |
| 50 | `MODEL_PART_MATERIAL` | `a=model, b=partIndex` | material-slot index; `-1` on invalid part |
| 51 | `SET_MESH_SKIN` | `a=mesh, b=vertex, c-f=joints 0-3, g-j=weight thousandths 0-3` | success |
| 52 | `CREATE_SKELETON` | `a=boneCount` | skeleton handle |
| 53 | `SET_SKELETON_BONE` | `a=skeleton, b=bone, c=parent, d-f=bind translation XYZ` | success |
| 54 | `COMMIT_SKELETON` | `a=skeleton` | success |
| 55 | `CREATE_CLIP` | `a=skeleton, b=duration milliseconds` | clip handle |
| 56 | `SET_CLIP_TRANSLATION` | `a=clip, b=bone, c-e=start XYZ, f-h=end XYZ` | success |
| 57 | `SET_CLIP_ROTATION` | `a=clip, b=bone, c-f=start XYZW thousandths, g-j=end XYZW thousandths` | success |
| 58 | `SET_CLIP_SCALE` | `a=clip, b=bone, c-e=start XYZ percent, f-h=end XYZ percent` | success |
| 59 | `ADD_CLIP_EVENT` | `a=clip, b=time milliseconds, c=positive event ID` | success |
| 60 | `CREATE_ANIMATOR` | `a=skeleton` | animator handle |
| 61 | `PLAY_ANIMATOR` | `a=animator, b=clip, c=loop, d=speed percent` | success |
| 62 | `UPDATE_ANIMATOR` | `a=animator, b=delta milliseconds` | success |
| 63 | `ANIMATOR_COMPLETE` | `a=animator` | completion state |
| 64 | `ANIMATOR_TIME` | `a=animator` | current milliseconds |
| 65 | `TAKE_ANIMATOR_EVENT` | `a=animator` | pending event ID and clears it |
| 66 | `SET_OBJECT_ANIMATOR` | `a=object, b=animator or 0` | success |
| 67 | `LIVE_SKELETON_COUNT` | none | live skeletons |
| 68 | `LIVE_CLIP_COUNT` | none | live clips |
| 69 | `LIVE_ANIMATOR_COUNT` | none | live animators |
| 70 | `MAX_BONE_COUNT` | none | 32 |
| 71 | `SKELETON_VALID` | `a=skeleton` | validity |
| 72 | `CLIP_VALID` | `a=clip` | validity |
| 73 | `ANIMATOR_VALID` | `a=animator` | validity |
| 74 | `STOP_ANIMATOR` | `a=animator` | success and restores bind pose |
| 75 | `MAX_SKELETON_COUNT` | none | 64 |
| 76 | `MAX_CLIP_COUNT` | none | 128 |
| 77 | `MAX_ANIMATOR_COUNT` | none | 128 |
| 78 | `DRAW_CALL_COUNT` | none | successful visible draw submissions in the current/most recently ended frame |
| 79 | `SUBMITTED_TRIANGLE_COUNT` | none | sum of `mesh.indexCount / 3` for those submissions |

Post-M0 evolution reserves numeric command 123 as `SET_CAMERA_UP` (`a-c=up XYZ`, nonzero, success result). `Graphics3D.Begin3D` sends it after the source-compatible command 10 camera payload. Native Direct3D and WebGL2 use that explicit up direction to avoid the fixed-world-up pole singularity during continuous 360-degree vertical orbit. Commands 80-122 were added by later milestones and are documented by their milestone implementation notes and mirrored constants.

Commands 78 and 79 are the only M0 ABI additions. Both counters reset to zero on a successful new `BEGIN` and on `RESET`, remain queryable after `END`, and do not count invisible or failed draws. The next unassigned numeric command is 80.

### Image and text ABIs

| Bridge | ID | Operation | Arguments | Ownership/result |
|---|---:|---|---|---|
| Image | 1 | `CREATE_TEXTURE` | owned image, `a=filter (0 nearest/1 linear), b=wrap (0 clamp/1 repeat)` | Renderer consumes the image on success or failure and returns a texture handle/zero |
| Text | 1 | `LOAD_MODEL` | owned path text | Native resolves the declared asset path before loading; Web resolves/fetches the logical project path; both consume the text and return model handle/zero |

The next unassigned image command is 2. The next unassigned text command is 2. There are no hidden or reserved command blocks in the current implementation.

## Current resource limits and ownership

Native uses kind-tagged, 16-bit-generation handles. All pools except objects use an 8-bit low slot encoding; the current 1,024-object pool uses ten low slot bits. Web uses monotonically increasing safe-integer handles and Maps; reset does not rewind the handle sequence, so stale handles are not reused. Both backends validate resource presence and apply the same logical ownership rules.

| Resource | Current hard limits | Current ownership and destruction rule |
|---|---|---|
| Mesh | 128 live; 1-65,535 vertices; 1-196,608 indices; indices divisible by 3 | Renderer owns CPU/GPU buffers. Ordinary objects refer to caller-owned meshes. A loaded model owns one mesh per part. Mesh/model destruction refuses while a part/object reference is live. |
| Object | 1,024 live | Object refers to one mesh and optionally one material and animator; it owns none of them. Destroy the object/instance first. |
| Texture | 128 live; decoded width/height each 1-8,192; nearest/linear and clamp/repeat only | Texture owns/retains the decoded image and lazy GPU texture/view/sampler. Materials refer to it. Destruction refuses while a material refers to it. |
| Material | 128 live; one optional texture; alpha modes 0-3; opacity/cutout 0-100; emissive 0-400 percent | Material refers to a texture. Objects refer to the material. Destruction refuses while an object refers to it. |
| Model (SM3D v1) | 64 live; 16 MiB file; 1-16 parts; 1-64 material slots; each part observes mesh limits | Model owns its part meshes and material-slot numbers, not `Material3D` resources. Complete validation precedes mesh allocation; load rolls back partial meshes. Destroy part objects before the model. |
| Skeleton | 64 live; 1-32 parent-ordered bones | Clips and animators refer to the skeleton. Destruction refuses while either exists. Current bind data is translation-only. |
| Animation clip | 128 live; 1-600,000 ms; at most 16 ordered integer events; at most two TRS keys per bone | Clip refers to one committed skeleton. The currently playing animator refers to the clip. Destroy/stop dependent animators first. |
| Animator | 128 live; fixed 32-matrix palette; speed 1-1,000 percent; update delta 0-600,000 ms | Animator refers to one skeleton and optional current clip. Any number of objects may refer to it. Destruction refuses while an object refers to it. |

`ResetRenderer3D` clears objects, models/their meshes, animators, clips, skeletons, materials, textures, and remaining meshes, then invalidates GPU state and resets diagnostics. This order is compatible with the proposed Generation 2 dependency direction; M1 must preserve it when adding atomic v2 static-model loading.

### Dragonfall ownership baseline

`games/Dragonfall/DragonfallScene.smile` owns the complete visual scene. It holds six textures, 24 materials, eight rig templates, 39 arena records, 224 party-part records, 20 party-face records, 90 Cinderling-part records, 97 dragon-part records, 128 particle records plus a particle template, and 24 impact-object records. `Initialize` creates resources; `Shutdown` destroys instances first, mesh-owning templates/arena objects second, materials third, and textures last. Dragonfall currently owns no skeletons, clips, animators, or loaded `Model3D` resources.

A repository-local diagnostic probe against untouched HEAD measured immediately after `Scene.Initialize`:

```text
BattleReady=True
SceneReady=True
LastError=5
Meshes=48/128
Objects=512/512
Materials=24/128
Textures=6/128
AfterMeshes=0
AfterObjects=0
AfterMaterials=0
AfterTextures=0
```

This isolates the current Dragonfall lifecycle failure: the scene reaches the entire object pool, while its gate requires headroom and still expects one texture. The gate reports one failure per restart (`failed: 100`). Teardown is clean, so this is capacity/expectation drift, not monotonic leakage. M0 deliberately records but does not redesign Dragonfall or its Generation 1 particle path.

## M0 diagnostics

Renderer3D did not previously expose draw-call or submitted-triangle metrics. M0 adds only:

- numeric command 78 and `Graphics3D.DrawCallCount3D()`;
- numeric command 79 and `Graphics3D.SubmittedTriangleCount3D()`;
- matched Direct3D 11/WebGL2 increments after a successful indexed draw;
- reset at each new successful 3D frame and full renderer reset;
- native/Web lifecycle assertions that one cube submission reports exactly one draw and 12 triangles.

No render ordering, object visibility, resource ownership, or shader behavior changes.

## Deterministic M1 GLB fixture

`scripts/generate-renderer3d-glb-fixture.ps1` owns generation of `examples/Renderer3DModelTests/Source/M0Triangle.glb`. The model gate regenerates the expected byte stream in memory and rejects fixture drift.

Fixture facts:

| Item | Value |
|---|---|
| GLB version | 2 |
| Bytes | 1,168 |
| SHA-256 | `A4D0E8C9CFE8714C7C44241D4BF03066BEC464128DD587B81EE8244BCBE24060` |
| Scene/node/mesh | One named `M0Triangle` |
| Geometry | Three vertices, one indexed triangle |
| Attributes | `POSITION`, `NORMAL`, `TEXCOORD_0`; tangent intentionally absent for deterministic M1 generation coverage |
| Material | One named `M0Material` with base-color, metallic, and roughness factors |
| Binary layout | One JSON chunk and one 104-byte BIN chunk, both four-byte aligned |

The current AssetTool correctly does not accept GLB yet. Parsing this fixture belongs to M1.

## Baseline gates

The first `scripts/build.cmd` attempt failed before managed compilation because `global.json` pins .NET SDK 10.0.302 and the host initially had only 10.0.400. With user authorization, official Microsoft's `dotnet-install.ps1` installed 10.0.302 to `C:\Users\louie\.dotnet`; user-level `DOTNET_ROOT` and `PATH` now select it. The repository pin was not changed. A clean rerun completed and produced compiler, AssetTool, native runtime, managed tests, and VSIX artifacts.

Untouched-baseline results before M0 edits:

| Gate | Exact result |
|---|---|
| `cmd /c scripts\build.cmd` after SDK installation | PASS, exit 0; compiler, AssetTool, runtime, tests, and VSIX built |
| `scripts\test-renderer3d-lifecycle.ps1` | PASS native/Web exact parity |
| `scripts\test-renderer3d-materials.ps1` | PASS native/Web exact parity |
| `scripts\test-renderer3d-models.ps1` | PASS deterministic conversion, invalid fixtures, native/Web exact parity |
| `scripts\test-renderer3d-animation.ps1` | PASS native/Web exact parity |
| `scripts\test-battle3d.ps1` | PASS native/Web exact parity |
| `scripts\test-dragonfall.ps1` | FAIL at native lifecycle: expected `Dragonfall 3D lifecycle tests passed`, actual `Dragonfall 3D lifecycle tests failed: 100`; native/Web mechanics and native balance passed before the stop |
| `scripts\test-simple3d-space-wars.ps1` | PASS Simple3D package/state/gallery and Space Wars demo/no-demo/state native/Web gates |

The Dragonfall failure reproduced after a clean full build and was isolated by the resource probe above. It predates M0 and is not caused by the new diagnostics.

Post-change focused results:

| Gate | Exact result |
|---|---|
| `cmd /c scripts\build.cmd` | PASS, exit 0; clean compiler, AssetTool, native runtime, managed tests, and VSIX build with SDK 10.0.302 |
| `scripts\test-smile-formatter.ps1` | PASS, all 13 formatter integration tests |
| Repository `format-smile-style.ps1 -Check -FormatLongIf` | FAIL on seven pre-existing files: `games/Dragonfall/DragonfallAudio.smile`, `games/Dragonfall/DragonfallScene.smile`, `games/Dragonfall/Program-NoDemo.smile`, `games/Dragonfall/Program.smile`, `libraries/Smile.Battle3D/Camera.smile`, `libraries/Smile.RPG/BattleCore.smile`, and `libraries/Smile.Simple3D/Interaction.smile` |
| Targeted formatter check for both changed `.smile` files | PASS, two files checked |
| `scripts\test-renderer3d-lifecycle.ps1` | PASS native/Web exact parity, including zeroed metrics and exactly one draw call/12 submitted triangles for one cube |
| `scripts\test-renderer3d-materials.ps1` | PASS native/Web exact parity |
| `scripts\test-renderer3d-models.ps1` | PASS fixture byte/hash check, deterministic conversion, invalid fixtures, native/Web exact parity |
| `scripts\test-renderer3d-animation.ps1` | PASS native/Web exact parity |
| `scripts\test-battle3d.ps1` | PASS native/Web exact parity |
| `scripts\test-simple3d-space-wars.ps1` | PASS all Simple3D package/state/gallery and Space Wars demo/no-demo/state native/Web gates |
| `scripts\test-dragonfall.ps1` | Same known FAIL at native lifecycle: expected `Dragonfall 3D lifecycle tests passed`, actual `Dragonfall 3D lifecycle tests failed: 100`; native/Web mechanics and native balance pass first |
| Manual Web Dragonfall lifecycle compile/run | Same known FAIL: expected `Dragonfall 3D lifecycle tests passed`, actual `Dragonfall 3D lifecycle tests failed: 100` |
| Manual Dragonfall program builds | PASS for native and Web `Program.smile` and `Program-NoDemo.smile` |
| `scripts\smoke-test.ps1` | FAIL in managed tests on an unrelated existing package expectation: `Smile.Game 2.0 value Types and Smile.RPG remain independent built-in source packages: Expected 1.2.1, found 1.3.0.` Doctor, bounded-process checks, and the clean build pass before the stop. |
| Windows PowerShell 5.1 fixture `-Check` | PASS, 1,168 bytes and expected SHA-256 |

The compiler is a VSIX payload, so M0 rebuilt and installed `artifacts\vsix\Smile.VisualStudio.vsix` into Visual Studio after validation. Installation completed successfully with VSIX version `2.0.48`; the installed `Smile.VisualStudio.dll` reports assembly version `2.0.48.0` and SHA-256 `EE7A989C023EF29972C862C92B6C0C7C5B4FFEF5A8BFDB3FE2F0C17B04B653A4`. The VSIX SHA-256 is `231EE0FB88CDFE17DAD079719BAF003D0580AEB39370642AD8C7432D9F786C9C`. Visual Studio must be restarted to load the refreshed extension.

## Plan deviations and final mapping

| Handoff assumption | Current repository fact and M0 decision |
|---|---|
| Plan nested under a named directory | Supplied flattened under untracked `docs/plans/`; use actual location and preserve it. |
| Prepared baseline is current HEAD | HEAD is one branding-only descendant; no 3D implementation drift. |
| Numeric ABI ends at 77 with future range unknown | Confirmed 1-77 before M0; M0 consumes 78-79 for required metrics; M1 starts at 80. |
| Current model docs say 256 objects | Runtime and facade expose 512 on both backends; the model-format document is stale on this value. |
| Dragonfall lifecycle is a green baseline | Native lifecycle currently fails because Dragonfall saturates 512 objects and uses six textures while the gate expects headroom/one texture. Teardown still returns all counts to zero. |
| Web resources mirror native fixed typed pools | Logical limits/ownership match, but Web uses Maps and monotonically increasing non-reused handles rather than native kind/generation bit packing. |
| GLB fixture exists | No GLB existed; M0 now owns a deterministic static fixture and generator. |
| Draw/triangle metrics may exist | They did not; M0 adds only the two reusable counters. |
| Suggested Generation 2 maxima are current limits | They remain proposals. M1 must define v2 limits/layout after reconciling them with the current v1 16 MiB/16-part/64-material and live pool limits. |
| Later Dragonfall2 integration needs both the complete procedural encounter and an imported hero | The native and Web object pool is now 1,024 with ten object-slot bits; the 512 frame-submission and palette-snapshot limits remain unchanged. Character3D now permits a bounded 25,000 percent uniform scale for meter-scale battle actors. |

The exact M1 scope remains: offline GLB container parsing, deterministic tangents, SM3D v2 static core chunks, PBR material metadata/paths, native/Web v2 validation/loading, inspect output, and v1 compatibility. M1 must not add skeletal clips, PBR shading, new animation playback, or VFX.

## M1 readiness

M1 is unblocked for the AssetTool and reusable Renderer3D model path because the ABI, dispatch paths, current ownership, limits, deterministic GLB input, and relevant Renderer3D baselines are now fixed. The isolated Dragonfall object-pool saturation is a recorded non-M1 baseline exception: M1 must not worsen it, and Dragonfall visual integration must recover headroom before M7 acceptance.

## Command ledger

Substantive state, build, diagnostic, and validation commands used for M0 were:

```powershell
git status --short --branch
git rev-parse HEAD
git branch --show-current
git remote -v
git log --oneline ec61dfa6324de7b22ea5ca0959828ff40e5e3902..HEAD
git diff --stat ec61dfa6324de7b22ea5ca0959828ff40e5e3902..HEAD
git fetch origin
git rev-list --left-right --count 'HEAD...@{upstream}'

cmd /c scripts\build.cmd
& .\scripts\test-renderer3d-lifecycle.ps1
& .\scripts\test-renderer3d-materials.ps1
& .\scripts\test-renderer3d-models.ps1
& .\scripts\test-renderer3d-animation.ps1
& .\scripts\test-battle3d.ps1
& .\scripts\test-dragonfall.ps1
& .\scripts\test-simple3d-space-wars.ps1

& "$env:TEMP\smile-dotnet-install\dotnet-install.ps1" -Version '10.0.302' -InstallDir "$env:USERPROFILE\.dotnet" -Architecture x64 -NoPath
dotnet --version
dotnet --list-sdks

& .\artifacts\compiler\smilec.exe --project .\games\Dragonfall\DragonfallResourceProbe.smileproj --configuration Release -o .\artifacts\tests\DragonfallResourceProbe.exe
cmd /c scripts\run-bounded-test.cmd 30 artifacts\tests\DragonfallResourceProbe.exe

& .\scripts\generate-renderer3d-glb-fixture.ps1
& .\scripts\generate-renderer3d-glb-fixture.ps1 -Check
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\generate-renderer3d-glb-fixture.ps1 -Check

& .\scripts\test-smile-formatter.ps1
& .\scripts\format-smile-style.ps1 -Check -FormatLongIf
& .\scripts\format-smile-style.ps1 -Check -FormatLongIf -Files @('libraries\Smile.Simple3D\Graphics3D.smile', 'examples\Renderer3DLifecycleTests\Program.smile')

cmd /c scripts\build.cmd
& .\scripts\test-renderer3d-lifecycle.ps1
& .\scripts\test-renderer3d-materials.ps1
& .\scripts\test-renderer3d-models.ps1
& .\scripts\test-renderer3d-animation.ps1
& .\scripts\test-battle3d.ps1
& .\scripts\test-dragonfall.ps1
& .\scripts\test-simple3d-space-wars.ps1
& .\scripts\smoke-test.ps1

& .\artifacts\compiler\smilec.exe --project .\games\Dragonfall\DragonfallLifecycleTests.smileproj --target web --configuration Release --output-dir .\artifacts\web\DragonfallLifecycleTests
node .\scripts\run-web-test.js .\artifacts\web\DragonfallLifecycleTests --renderer3d --expected .\games\Dragonfall\DragonfallLifecycleTests.expected.txt --frames 4 --timeout 60000
& .\artifacts\compiler\smilec.exe --project .\games\Dragonfall\Dragonfall.smileproj --configuration Release -o .\artifacts\games\Dragonfall.exe
& .\artifacts\compiler\smilec.exe --project .\games\Dragonfall\Dragonfall-NoDemo.smileproj --configuration Release -o .\artifacts\games\Dragonfall-NoDemo.exe
& .\artifacts\compiler\smilec.exe --project .\games\Dragonfall\Dragonfall.smileproj --target web --configuration Release --output-dir .\artifacts\web\Dragonfall
& .\artifacts\compiler\smilec.exe --project .\games\Dragonfall\Dragonfall-NoDemo.smileproj --target web --configuration Release --output-dir .\artifacts\web\Dragonfall-NoDemo

cmd /c scripts\install-vsix.cmd
Get-Item .\artifacts\vsix\Smile.VisualStudio.vsix
Get-FileHash .\artifacts\vsix\Smile.VisualStudio.vsix -Algorithm SHA256
Get-FileHash .\artifacts\compiler\smilec.exe -Algorithm SHA256
Get-FileHash "$env:LOCALAPPDATA\Microsoft\VisualStudio\18.0_91f001b5\Extensions\xj3ifrff.bl5\Smile.VisualStudio.dll" -Algorithm SHA256

git diff --check
```

Read-only reconciliation additionally used `Get-Content`, `rg`, `Get-ChildItem`, `Get-Item`, `Get-FileHash`, `git show`, `git log`, `git diff`, and `git status` across the paths named in this note. Temporary diagnostic probe files were created and removed with `apply_patch`; no probe source remains in the working tree.
