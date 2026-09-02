# Paladin v5.4 Viewer and Export Hardening (M7C-B.1)

## Status

- Milestone: M7C-B.1
- Branch: `main`
- Reconciled starting commit: `750df0d6c281c20929e00df42d50682ccc61f5ac`
- Ending commit: the M7C-B.1 commit containing this report; the exact immutable SHA is recorded in the final task report and on `origin/main`.
- Scope: Renderer3D camera safety, viewer timing/resources/layout, native window DPI and placement, deterministic Paladin v5.4 export, current native/Web evidence, and focused regression coverage.
- Explicitly excluded: production promotion, replacement production textures, new combat animation content, new PBR features, and M7D implementation.

## Reconciliation against the handoff

The handoff was reconciled against the actual `main` branch rather than treated as a patch prescription. Existing M7B.1/M7C work already provided the Character3D cache, Model3D cooking, PBR preparation, eleven candidate clips, six sockets, and a functional native viewer. M7C-B.1 therefore hardens those paths in place.

Current-code differences from the handoff and the resulting mapping are:

1. `Model3DAsset` publication confines source files to the declaring project. The reusable viewer implementation and profiles now live in `tools/Character3DViewer`, while the three Dragonfall `.smileproj` wrappers remain the authoritative asset/cooking owners.
2. The canonical editable Blender source belongs to Sin Star I rather than Dragonfall, so it moved to `games/SinStarI/SourceAssets/Characters/Paladin/arin-integrated-candidate-v5.4.blend`. The candidate GLB, deterministic export record, and cooked publication remain under Dragonfall because that is the current project owner.
3. The handoff expected Web responsive-window behavior to follow project metadata, but the current compiler did not pass `ResponsiveWindow` into Web emission. The compiler now does so without changing fixed-canvas projects.
4. Current cooked SM3D texture pixels already use renderer-native orientation. Web was flipping those pixels a second time, producing torn atlas sampling. Web uploads now use `UNPACK_FLIP_Y_WEBGL = false`, matching native output.
5. The existing grid used many rigid objects. It is now one reusable mesh, one object, and one draw. Socket axes are four selected-socket objects plus one optional particle batch for all socket origins, with transactional rollback.
6. Paladin v5.4 remains a candidate. M7C-B.1 closes viewer/export hardening and Web visual evidence, not the production asset gate.

## Renderer3D ABI and dispatch paths

The exact current command namespaces are:

| Surface | IDs | Call shape | Next free ID |
| --- | --- | --- | --- |
| Numeric | `1..123` | `Renderer3D(command, a, b, c, d, e, f, g, h, i, j)` | `124` |
| Image | `1..2` | `Renderer3DImage(command, image, a, b, c, d, e, f, g, h)` | `3` |
| Text | `1..9` | `Renderer3DText(command, text, a, b, c, d, e, f, g, h)` | `10` |

M7C-B.1 adds numeric command `123`, `SMILE_3D_SET_CAMERA_UP`. It adds no image or text command.

The shared-language surface is declared in `src/Smile.Language/Syntax.cs` and bound by the shared semantic model. `libraries/Smile.Simple3D/Graphics3D.smile` is the typed source-level wrapper and calls those three intrinsic functions.

Native dispatch is:

```text
SMILE source
  -> shared parser/semantic model
  -> MasmEmitter intrinsic call
  -> smile_renderer3d_command / _image_command / _text_command
  -> graphics3d_directx.cpp numeric/image implementation
  -> runtime.c text path resolution and text ownership bridge
  -> graphics3d_directx.cpp model/PBR/text operations
```

Web dispatch is:

```text
SMILE source
  -> shared parser/semantic model
  -> WebEmitter smile.renderer3D / renderer3DImage / renderer3DText
  -> generated smile-runtime.js numeric/image/text switches
  -> WebGL2 renderer and browser asset loaders
```

The authoritative numeric, image, and text IDs remain `src/Smile.NativeRuntime/graphics/graphics3d.h`; native and Web focused gates assert parity with that header.

## Atomic camera contract

`SetCamera3D` stages command `10` and `SetCameraUp3D` stages command `123`. `Begin3D` validates and atomically promotes the complete pair. Neither command mutates the live camera by itself. A failure clears the pending transaction and preserves the last valid live camera. When no transaction is pending, raw repeated `Begin` calls deliberately reuse that live camera for ABI compatibility; if either half is pending, both halves are required.

Validation is identical on native and Web:

- position and target components: inclusive `-1,000,000..1,000,000`;
- position and target must differ;
- field of view: inclusive `10..160` degrees;
- near plane: greater than zero;
- far plane: greater than near and no more than `2,000,000`;
- up components: inclusive `-1,000,000..1,000,000` and not all zero;
- up and forward may not be parallel or nearly parallel; squared cross-product length must exceed `forwardLengthSquared * upLengthSquared * 0.00000001`;
- once either half is staged, both staged halves must exist before `Begin3D`;
- camera staging while a 3D frame is active is rejected.

New camera errors are:

| Error | Meaning |
| ---: | --- |
| 58 | Invalid camera position or target component |
| 59 | Zero view direction |
| 60 | Invalid projection |
| 61 | Invalid up vector |
| 62 | Parallel or nearly parallel forward/up basis |
| 63 | Incomplete pending camera transaction |
| 64 | Camera mutation or nested begin while a frame is active |

Renderer reset restores position `(0, 300, -800)`, target `(0, 0, 0)`, up `(0, 1, 0)`, FOV `55`, near `1`, far `10,000`, and clears pending state. `Smile.Simple3D.Graphics3D.Begin3D` retains a source-friendly zero-up default; it selects world X rather than world Y only when the view is effectively vertical, preventing a singular fallback basis.

## Resource limits and ownership

### Renderer3D native/Web parity limits

| Resource | Maximum | Ownership/reference rule |
| --- | ---: | --- |
| Meshes | 128 | Standalone meshes are caller-owned. Model part meshes are model-owned. Objects and in-flight submissions prevent deletion. |
| Objects | 1,024 | Caller/adapter-owned instances. An object references one mesh, optional material, and optional animator; destroy objects before those dependencies. |
| Textures | 128 | Standalone textures are caller-owned. Prepared model textures are model-owned. Materials and in-flight submissions prevent deletion. |
| Materials | 128 | Standalone materials are caller-owned. Prepared PBR materials are model-owned. Objects, particle batches, and ribbon batches prevent deletion. |
| Models | 64 | A model owns its part meshes, animation/string payload, and prepared PBR materials/textures. Part objects and model animators must be destroyed first. |
| Skeletons | 64 | Standalone caller-owned resources; clips and animators reference them. |
| Clips | 128 | Standalone caller-owned resources; animators reference them. |
| Animators | 128 | Caller/Character3D-owned mutable instances; objects reference them. |

Additional format/runtime ceilings are 16 model parts, 131,072 vertices, 393,216 indices, 64 model materials, 128 model texture references, 32 model chunks, a 16 MiB model file, 32 bones for the standalone skeletal ABI, and embedded Model3D animation limits of 256 nodes, 128 bones, 64 clips, and 64 sockets.

`Character3D` owns a shared asset cache of 16 entries and up to 32 actor instances, with at most 16 part objects per actor. Each asset cache entry owns one model and its PBR resources (or one fallback material). Each actor owns its animator and part objects and retains one cache reference. Actor destruction releases objects, then animator, decrements the asset reference, and releases the asset only when unused. Pending-release state preserves failed cleanup for deterministic retry. Character scale accepts `1..25,000` percent and world positions use the shared `+/-1,000,000` bound.

Dragonfall's `VisualActor` adapter owns at most 16 visual actors. Character actors transfer ownership to `Character3D`; Classic objects transfer only when `CreateOptions.OwnsClassic` is true, otherwise they remain borrowed. Release mode remains Classic, so v5.4 is still explicitly candidate/prototype content.

The current Dragonfall scene reserves 39 arena objects, 224 party objects, 20 party-face objects, 97 dragon objects, 90 cinderling objects, 35 particle objects, and 24 impact objects. Its declared totals are 441 initial objects and 448 boss-phase objects, with 64 objects of required headroom, 48 meshes, 24 materials, and 6 textures. These fit Renderer3D's 1,024-object, 128-mesh, 128-material, and 128-texture ceilings. `Smile.Battle3D` separately caps battle presentation at 12 actors and 256 commands, with 32 camera shots, 32 effect presets, and 128 presentation particles.

## Viewer timing, layout, and reset behavior

- All presentation timing uses bounded elapsed-time deltas rather than frame counts.
- Focused partitions cover 10, 30, 60, 120, and 240 FPS, mixed deltas, a stall, and a 24-hour clock boundary.
- Raw, accepted, and dropped time remain separately observable; a long stall is bounded rather than replayed as an input or animation burst.
- Native and Web use a responsive logical canvas. Native logical pixels are physical pixels scaled by `96 / DPI`; Web logical size follows the viewport only for projects declaring `ResponsiveWindow`.
- Minimum viewer layout is `800x540`; panels anchor to edges and the 3D scene receives the center remainder.
- Right-click/full reset restores view, target, zoom, animation sequence, and auto-orbit. Other reset variants preserve their documented narrower scope.
- The ground grid is one mesh/object/draw rather than approximately 62 line objects/draws.
- Selected-socket axes use exactly four objects. Displaying all socket origins adds at most one particle batch, independent of the model's socket count (maximum 64).
- Normal v5.4 scene evidence records 6 draw calls and 10,378 submitted triangles. The selected-socket view records 10 draw calls and 10,426 triangles; all origins add only the one optional batch.

## Native window/DPI placement

The native runtime now treats authored window dimensions as 96-DPI logical pixels, converts to physical pixels for the selected monitor, and updates responsive logical dimensions atomically on `WM_DPICHANGED`.

Placement format v2 is a fixed 64-byte repository runtime record keyed as `__smile_internal_window_placement_v2`. It stores magic/version, FNV-1a checksum, saved DPI, logical client width/height, work-area-relative logical offsets, saved monitor work area, and normal/maximized state. Writes use a temporary file plus replacement. Reads verify exact length, magic, version, checksum, dimension bounds, recover to the primary monitor when the saved monitor is absent, clamp the window to the work area, and fall back to the existing v1 record when v2 is missing or invalid.

## Stable identity and exporter

- Canonical ID: `sin-star-i.character-1.paladin`
- Display name: `Arin`
- Party role: `Paladin`
- Candidate version: `v5.4`
- Compatibility alias: `dragonfall.arin-v5-4`
- Neutral reusable viewer code: `tools/Character3DViewer`
- Canonical editable source: `games/SinStarI/SourceAssets/Characters/Paladin/arin-integrated-candidate-v5.4.blend`

The repository-owned exporter uses `scripts/export-arin-v5-4-viewer.manifest.json` as the exact action/attachment contract. It exports from a disposable copy, validates the Blender version and expected named content, samples all eleven actions at 30 FPS (including fractional source timing), evaluates attachments from source-world times reference-inverse transforms, rejects unexpected modifiers/extensions, filters bind-pose-only channels, emits external JPEG texture inputs, writes metadata hashes, restores source state, and publishes outputs atomically. The canonical `.blend` is hash-checked before and after export.

Determinism evidence:

| Artifact | SHA-256 |
| --- | --- |
| Canonical Blend | `CD58B33AC94E7B3CFEEDB9A85B2603B49DB4935FE8D2590DE5B50BE371C4A35C` |
| Exported GLB (two independent exports) | `D080754339ABD4F3F4CFBCAF4F26146631BDEEE30DD2EAA284682EF896B16CA3` |
| Directly cooked comparison SM3D | `CC0F8950171A9CC873E5A86869A02E404CB51A8ABE553D9F66DE95C06C3F3BC4` |
| Project-published SM3D | `508063F78C08B97DBD44ED19DC3A0D8C1DAAEF1A093D8F19E5A6929456993023` |

The cooked candidate contains 4 parts, 7,376 vertices, 10,296 triangles, 4 materials, 9 textures, 42 bones, 46 nodes, 11 clips, and 6 sockets.

## Production gate

Passing candidate evidence now includes deterministic export/cook, identical native/Web texture orientation, current native and Web captures, all eleven named clips present, six sockets present, stable identity, deterministic hashes, responsive layouts, full vertical orbit, grounded KO evidence, bounded resource diagnostics, and clean teardown gates.

Production promotion remains blocked by the intentionally deferred content/approval inputs: a complete provenance and license package; approved lossless 2K production texture sources; explicit deformation acceptance including Shield Bash; accepted production event timing; final required hand/socket naming such as `HandRight`/`HandLeft`; and explicit production approval. Dragonfall therefore keeps its Classic release path.

## Validation and evidence

All retained M7C-B.1 gates pass on the reconciled branch:

| Command | Exact result |
| --- | --- |
| `pwsh -NoProfile -File scripts/test-smile-formatter.ps1` | PASS: 13 focused formatter integration tests |
| `pwsh -NoProfile -File scripts/format-smile-style.ps1 -Check -FormatLongIf` | PASS: 350 SMILE files |
| `cmd /c scripts\build.cmd` | PASS: compiler, asset tool, native runtime, tests, and VSIX 2.0.57 built; known `NU1503` native-project restore warning only |
| `pwsh -NoProfile -File scripts/test-model3d-asset-cooking.ps1` | PASS |
| `pwsh -NoProfile -File scripts/test-renderer3d-v2-boundaries.ps1` | PASS |
| `pwsh -NoProfile -File scripts/test-renderer3d-models.ps1` | PASS |
| `pwsh -NoProfile -File scripts/test-renderer3d-pbr-hardening.ps1` | PASS |
| `pwsh -NoProfile -File scripts/test-renderer3d-animation-v2-hardening.ps1` | PASS |
| `pwsh -NoProfile -File scripts/test-character3d.ps1` | PASS |
| `pwsh -NoProfile -File scripts/test-character-3d-viewer-hardening.ps1` | PASS: 54 native checks |
| `pwsh -NoProfile -File scripts/test-paladin-v5-4-viewer-export-hardening.ps1` | PASS: two deterministic Blender exports, direct cooks, native/Web capture, layout/resource/evidence checks |
| `pwsh -NoProfile -File scripts/test-renderer3d-vfx-hardening.ps1` | PASS |
| `pwsh -NoProfile -File scripts/test-dragonfall-character-generation-2.ps1` | PASS |
| `pwsh -NoProfile -File scripts/test-dragonfall-arin-prototype.ps1` | PASS, including intentional malformed-manifest `SML3605` recovery case |
| `pwsh -NoProfile -File scripts/test-dragonfall.ps1` | PASS |
| `pwsh -NoProfile -File scripts/test-battle3d.ps1` | PASS |
| `pwsh -NoProfile -File scripts/test-simple3d-space-wars.ps1` | PASS |
| `pwsh -NoProfile -File scripts/test-true-simple3d-neon-cycles.ps1` | PASS |
| `dotnet run --project src\Smile.Tests\Smile.Tests.csproj -c Release` | PASS: 294 language/compiler/project/completion/timing tests |
| `pwsh -NoProfile -File scripts/verify-artifacts.ps1` | PASS: artifacts and VSIX 2.0.57 synchronized |
| `cmd /c scripts\smoke-test.cmd` | PASS: complete retained repository suite; graphical gameplay remains a documented manual check |
| `cmd /c scripts\install-vsix.cmd` | PASS: rebuilt and installed VSIX 2.0.57 into Visual Studio instance `91f001b5` |
| `pwsh -NoProfile -File scripts/verify-vsix-install.ps1 -InstanceId 91f001b5 -BuiltDllPath src\Smile.VisualStudio\bin\Release\net472\Smile.VisualStudio.dll -ManifestPath src\Smile.VisualStudio\source.extension.vsixmanifest` | PASS: installed assembly `2.0.57.0`, SHA-256 `8A41360B84FDF1DBE065A0D42AB8FBA687F393334BBAA6CEB5B377072503A7CF` |

Three focused first runs exposed and closed real reconciliation issues before their final green reruns: the raw Renderer3D lifecycle needed to reuse its live camera when no camera transaction is pending; three Web captures had JPEG bytes despite `.png` names and were converted to true PNG; and the MenuGallery Web harness still used now-dedicated `R` and `O` keys as generic test input, so it was moved to unmapped `U` and `P` keys without changing runtime behavior.

Current screenshot evidence and per-file hashes are indexed at `docs/implementation/screenshots/m7c-b1-paladin-v5-4-hardening/screenshot-index.md`. The evidence set contains six current native captures, three current Web captures, responsive/resource composites, and a mobile-friendly contact sheet. No image is embedded as base64 text.

## M7D readiness

M7D-A may begin only after this milestone's retained gates pass, the rebuilt VSIX is installed and verified, and this commit is pushed/remote-verified. M7D-B's production-acceptance work remains asset/approval-gated by the items above.
