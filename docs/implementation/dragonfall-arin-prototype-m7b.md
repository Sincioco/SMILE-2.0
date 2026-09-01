# Dragonfall Arin Prototype — M7B

Date: 2026-09-01 (Asia/Taipei)

Milestone: M7B — user-approved early Arin / Paladin prototype integration and reusable Character 3D Viewer

Status: **Prototype integration complete**; final-production provenance, combat animation, target bone-count, and authored-socket acceptance remain incomplete.

Branch: `main`

Starting local commit: `e0eba97116fc1bdc88386ab7b0d7347ef312a3c5`

Starting `origin/main`: `e0eba97116fc1bdc88386ab7b0d7347ef312a3c5`; ahead/behind `0/0`.

Ending commit: the focused M7B commit containing this report; its SHA is recorded in the delivery report because a commit cannot contain its own SHA.

Pushed and verified: recorded in the delivery report after the focused commit is pushed and `origin/main` is verified.

## Reconciliation and scope

- Root `AGENTS.md`, the M6.1-then-M7 instructions, the existing M7 blocker, M7A report, converter/runtime boundaries, Character3D/Scene3D APIs, Dragonfall adapter, and actual branch state were reconciled before implementation.
- M7A was already complete, pushed, and remote-verified at the starting commit.
- Existing untracked `docs/plans/` and four Sin Star Paladin view PNGs remain preserved and excluded as unrelated user work.
- No reset, restore, clean, rebase, amend, force-push, or history rewrite was used.
- M8 was not started. No language syntax, SM3D v2 format, PBR feature, new animation system, or VFX feature was added.

## Capability flags and resolution

1. SMILE does not runtime-load GLB. The repository-owned offline converter remains the smallest reusable boundary: GLB/GLTF source is prepared and converted to SM3D, while native and Web programs load the published SM3D plus PNG textures.
2. The supplied GLB has 550 buffer views and 547 accessors, beyond the previous converter-only 512-entry JSON-table cap. Sin explicitly authorized increasing the limit. Buffer-view and accessor limits are now 1,024, with exact 1,024 acceptance and 1,025 rejection tests.
3. SM3D conversion intentionally does not ingest embedded image bytes. The deterministic preparation script externalizes the three embedded 1K JPEG images to PNG and preserves the original GLB unchanged.
4. The source has 30 compatible mesh primitives while one runtime model permits at most 16 parts. The converter now deterministically coalesces compatible same-material/same-skin fragments while respecting the existing per-part vertex/index ceilings. Arin becomes one part; incompatible 17-part inputs still fail. Runtime part and pool limits were not raised.
5. The prototype has only Idle, Walk, and Run and no authored production socket nodes. The descriptor adds six explicit prototype sockets from existing skeleton nodes, and the Dragonfall adapter uses clearly flagged prototype-only clip fallbacks. Missing combat content is not mislabeled as final production readiness.

## Preserved source and deterministic outputs

Original source:

- path: `games\Dragonfall\SourceAssets\Arin\sin-star-i-character-1-paladin-tripo-v01.original.glb`;
- bytes: 1,479,468;
- SHA-256: `0B75E3664FC2743637C9E75E86A55EBDFB8D4A4E3740AC06E593ADE1588013F6`;
- GLB version: 2; generator metadata: Tripo;
- scenes/nodes/meshes/materials/skins/animations: 1 / 73 / 30 / 1 / 1 / 3;
- embedded images: three JPEG images, each 1,024 x 1,024.

The source intake/provenance boundary is recorded in `games\Dragonfall\SourceAssets\Arin\README.md`. Sin explicitly authorized repository prototype use, but no separate export receipt, creator identity, service project link, or final redistribution license was supplied. The note therefore preserves authorization without claiming third-party ownership.

`scripts\prepare-dragonfall-arin-prototype.ps1` hash-pins the original, parses the GLB, externalizes images, makes the ORM red channel opaque because no occlusion texture is declared, writes prepared GLTF/BIN, invokes the existing converter with the descriptor, verifies the exact profile, and publishes the runtime model/textures. `-Check` regenerates in `artifacts\temp` and compares hashes without touching tracked outputs.

| Runtime output | Bytes | SHA-256 |
| --- | ---: | --- |
| `ArinPrototype.sm3d` | 709,884 | `859C96D0F763DE96130DBE82C6A526D2886986FFAD50FCE8D644FA55064A3121` |
| `Arin-base-color.png` | 2,183,883 | `CAEAF1BAFF6C7F0B465A75F0808A0DF2EC8998C9562A9341E41209E978B89254` |
| `Arin-normal.png` | 1,454,013 | `942A193949BEF7B067B0113553A7788604AA01E3716B963354E1905765FC472E` |
| `Arin-orm.png` | 1,755,724 | `DC9AEE72FCCD2512154D44887F8B29749AD17A5FB65D7432703731D6944B65DD` |

## Exact SM3D profile

```text
Version: 2
Name: Scene
Parts: 1
Vertices: 6631
Indices: 29922
Triangles: 9974
Materials: 1
TextureReferences: 3
Bounds: -0.381257,0.000212,-0.120926 | 0.381316,1.000579,0.120926
Tangents: +6624 -7
Bones: 41
Nodes: 73
Clips: 3
AnimationBytes: 269852
Events: 4
Sockets: 6
RootMotionClips: 0
StaticBytes: 440032
TotalBytes: 709884
```

Clips:

| Clip | Duration | Samples | Tracks | Bytes | Loop |
| --- | ---: | ---: | ---: | ---: | --- |
| `preset:biped:walk` | 2,375 ms | 73 | 41 | 21,832 | Yes |
| `preset:biped:idle` | 15,375 ms | 463 | 41 | 120,072 | Yes |
| `preset:biped:run` | 1,292 ms | 40 | 41 | 13,516 | Yes |

Walk and Run each have left/right footstep events. Descriptor sockets are Root, Head, Chest, SwordBase, SwordTip, and ShieldCenter. There is one `JOINTS_0` / `WEIGHTS_0` vec4 set, so the asset remains within the four-influence limit.

## Converter and inspector hardening

- Converter JSON table limits: buffer views 1,024; accessors 1,024.
- Existing converter inputs with 16 or fewer parts remain byte-identical.
- More than 16 source primitives are coalesced only when material and skin match and the merged part stays within 65,535 vertices and 196,608 indices.
- Seventeen incompatible material groups still reject rather than silently changing material identity.
- A real sampled animation exposed redundant quaternion sign stabilization in the converter and an exact-float equality check in the inspector. Runtime interpolation already applies shortest-path sign handling. Stored samples now retain canonical signs, and inspection separately verifies normalization tolerance and canonical sign.

No runtime resource pool, model-file, vertex, index, bone, clip, event, socket, submission, or command limit was increased.

## Dragonfall adapter mapping

The M7A production clip names remain first choice. `CreateOptions.PrototypeAsset` opts into the following explicit prototype-only fallbacks:

| Dragonfall state | Production attempt | Prototype fallback |
| --- | --- | --- |
| Approach | `Run` | `preset:biped:run` |
| Ready, Attack, Special, Defend, Block impact, Hit, KO, Victory | existing production state name | `preset:biped:idle` |

`UsedPrototypeClipFallback` exposes this condition. `FootstepLeft` and `FootstepRight` map to the root presentation anchor. Production state/socket mappings and the Classic fallback remain intact.

The focused test covers all states, sockets, conservative bounds, one footstep event, normal PBR rendering, forced PBR failure to Classic, draw/triangle diagnostics, and complete teardown to zero live actors, cache entries, models, animators, objects, meshes, materials, and textures.

Release Dragonfall remains `RELEASE_MODE = MODE_CLASSIC`; this early asset is not silently enabled in the normal game.

## Reusable Character 3D Viewer

`games\Dragonfall\Character3DViewer.smileproj` is a separate SMILE executable project built directly on Character3D, Scene3D, Graphics3D, and the shared Simple3D Interaction API. Arin is its included sample, while the viewer code is character-neutral.

Capabilities:

- persistent primary-drag orbit or pan mode;
- middle-drag orbit and secondary-drag pan;
- horizontal and vertical orbit buttons/arrow keys;
- pan buttons and W/A/S/D keys;
- wheel and on-screen zoom;
- Idle, Walk, and Run selection;
- four inspection-lighting profiles;
- live asset, animation, lighting, camera, draw-call, and triangle diagnostics;
- touch-friendly on-screen controls and reset;
- native Direct3D 11 and WebGL2 builds with four published assets.

The verified Web screenshot is `docs\implementation\screenshots\m7b-arin-prototype\character-3d-viewer-web.png`, SHA-256 `A009EFC9FC8EDD1E4A3C8827AAF3FBBBFF73BA63E290FB39A552300927DB7C24`. Its live panel shows the real Arin prototype, PBR lighting, Idle, a 15-degree horizontal / 2-unit vertical orbit, two draw calls, and 9,976 submitted triangles (character plus floor). Browser console warnings/errors: none.

## Current resource limits and ownership

Runtime Renderer3D pools are unchanged:

| Resource | Limit | Ownership |
| --- | ---: | --- |
| Meshes | 128 | Primitive/custom owner or model; shared instances borrow mesh references. |
| Objects | 512 | Caller for primitives; Character3D owns one object per model part. |
| Models | 64 | Character3D cache owns loaded models while referenced. |
| Textures | 128 | Model owns deduplicated imported textures; materials/in-flight snapshots retain references. |
| Materials | 128 | Model owns imported materials; objects borrow bindings. |
| Legacy skeletons | 64 | Explicit legacy API ownership; imported SM3D animation is model-owned. |
| Legacy clips | 128 | Explicit legacy API ownership; one imported model permits 64 clips. |
| Total animators | 128 | Character3D owns one independent model animator per actor. |

Per-model limits remain 16 parts, 64 materials, 128 texture references, 256 nodes, 128 production bones, 64 clips, 64 events per clip, 64 sockets, 16 MiB complete file, 65,535 vertices and 196,608 indices per part. Frame limits remain 512 physical submission snapshots and 512 palette snapshots.

Character3D owns fixed pools of 16 cached assets, 32 actors, and 16 parts per actor. Dragonfall.VisualActor owns 16 generation-safe adapter slots and borrows or explicitly owns its Classic objects according to options. Battle3D remains bounded to 12 battle actors.

This prototype consumes one Character3D actor, one cached model, one animator, one part object, one imported material, and three imported textures. The Viewer adds one caller-owned floor object/mesh. Successful teardown returns every measured live pool to zero.

## Renderer3D command ABI

No runtime command was added or renumbered:

- numeric commands: 1-121; next numeric ID 122;
- image commands: 1-2; next image ID 3;
- text commands: 1-9; next text ID 10.

Native DirectX and Web dispatch remain unchanged. M7B changes only offline AssetTool conversion/inspection behavior, SMILE adapter/viewer source, declared assets, tests, evidence, and documentation.

## Tests and exact results

| Gate | Exact result |
| --- | --- |
| Deterministic asset preparation `-Check` | PASS; 709,884-byte SM3D regenerated with the exact profile above. |
| SM3D v2 boundary gate | PASS; exact 1,024 buffer-view/accessor input accepted, 1,025 rejected, compatible 17-part input coalesced, incompatible 17-material input rejected; exact boundary outputs 7,865,176 / 3,212 / 708 bytes. |
| Animation-v2 hardening | PASS; native/Web exact parity for fractional timing, sampling, crossfade, root, events, memory, deformation, palette reuse, and lifecycle. |
| M7B native normal | PASS; exact `Dragonfall M7B Arin prototype tests passed.` |
| M7B native forced PBR failure | PASS; exact output parity and Classic fallback. |
| M7B Web normal and forced failure | PASS; exact console parity in both modes. |
| M7B Lab native/Web builds | PASS; four assets published; both Web JavaScript files pass `node --check`. |
| Character 3D Viewer native/Web builds | PASS; four assets published; both Web JavaScript files pass `node --check`. |
| Retained M7A gate | PASS; exact native/Web adapter, mixed draw, state/clip, anchor/socket, effect, bounds, fallback, 100-restart checks. |
| Retained Dragonfall gate | PASS; `Dragonfall native/Web mechanics, lifecycle, demo, and no-demo validation passed.` |
| Combined M7B gate | PASS; `Dragonfall M7B Arin source preservation, deterministic conversion, 1,024-table boundary, compatible-part coalescing, animation hardening, native/Web PBR/fallback, Character 3D Viewer, M7A adapter, crowd-demo, and no-demo tests passed.` |
| `cmd /c scripts\build.cmd` | PASS; compiler, AssetTool, native runtime/tests, managed solution, and VSIX artifacts built; only the established NU1503 native-project restore warning appeared. |
| Formatter integration and repository check | PASS; 13 formatter integration tests and 340 tracked SMILE files. |
| Retained model/PBR/animation/Character3D/Battle3D gates | PASS; native/Web exact parity, PBR normal/fallback/hardening, animation-v2, Character3D/Scene3D, and Battle3D. |
| Retained Simple3D gates | PASS; Simple3D/Space Wars and true-Simple3D/Neon Cycles native/Web focused validation. |
| Managed suite inside smoke | PASS; 288 language, compiler, project, completion, and timing tests; expected synthetic diagnostics observed. |
| Native runtime inside smoke | PASS; 39 graphics/audio-focus and 44 Text runtime checks. |
| Artifact verification inside smoke | PASS; libraries, native executables, game assets, VSIX payload/version 2.0.56, viewport, and DPI checks. |
| `cmd /c scripts\smoke-test.cmd` | PASS, exit 0; .NET SDK 10.0.400, Node.js 24.14.0, full native/Web/game/library/artifact baseline. |

## Plan deviations and remaining gate

- The supplied optimized prototype is 9,974 triangles, 26 below the plan's approximate 10,000 lower target and suitable for this early visual slice.
- It has 41 bones rather than the planned 55-80 deformation-bone target, though it remains well below the runtime maximum of 128.
- Only Idle, Walk, and Run exist. Required production combat, hit, KO, defend, special, and victory clips remain missing; prototype fallback never claims those motions exist.
- Socket transforms are descriptor-authored from existing skeleton nodes, not artist-authored source socket nodes.
- The user authorized prototype repository use, but final third-party provenance/license evidence remains incomplete.
- The release game stays Classic. This run validates viewing and the adapter path; it does not declare final-production M7 acceptance.

The early Arin prototype and M7B converter/viewer testing are unblocked and complete. Final production M7 is not complete, so M8 remains blocked and was not started.

## Command ledger

Substantive commands used for M7B:

```powershell
Get-Content <root rules, M7 instructions/reports, Character3D/Scene3D/Interaction APIs, adapter/tests/projects>
Get-ChildItem <Downloads source, repository asset trees, validation outputs>
Get-FileHash -Algorithm SHA256 <source, prepared, runtime, texture, screenshot files>
rg <converter limits, animation validation, ownership, viewer interaction, ABI/resource searches>

& .\scripts\prepare-dragonfall-arin-prototype.ps1
& .\scripts\prepare-dragonfall-arin-prototype.ps1 -Check
& .\scripts\format-smile-style.ps1 -Files <M7B SMILE files> -FormatLongIf
& .\scripts\test-dragonfall-arin-prototype.ps1

& .\artifacts\compiler\smilec.exe --project .\games\Dragonfall\Character3DViewer.smileproj --target windows-x64 --configuration Release -o .\artifacts\games\Character3DViewer.exe
& .\artifacts\compiler\smilec.exe --project .\games\Dragonfall\Character3DViewer.smileproj --target web --configuration Release --output-dir .\artifacts\web\Character3DViewer
python -m http.server 8769 --bind 127.0.0.1
```

Read-only browser interaction, screenshots, JavaScript console inspection, `node --check`, Git reconciliation, and final retained validation commands are also part of the evidence trail.

## VSIX

M7B changes AssetTool, Dragonfall-local source/projects/assets, tests, screenshots, and documentation. AssetTool is not a VSIX payload. No compiler, template, language-service, or VSIX payload changed, so the installed SMILE extension does not require a redundant reinstall for this milestone.
