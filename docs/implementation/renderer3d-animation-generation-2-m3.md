# Renderer3D Animation Generation 2 M3

## Milestone status and reconciliation

M3 was implemented on `main` from commit `66a5a0a0eed64969a5c99244b4f090be16c8432c`, with `origin/main` at the same commit. The starting tree contained only the user-owned untracked `docs/plans/` directory; it was preserved and is not part of this milestone. The starting commit already contained M2.1 PBR hardening, numeric commands 1-97, image commands 1-2, text commands 1-3, model-owned PBR dependencies, the 32-bone legacy teaching path, and the 512-object Dragonfall headroom repair.

**Flag:** Before M3, SMILE had only the legacy custom-mesh, 32-bone, two-key animation API. It could not import a production GLB skin/clip payload, carry more than 32 bones, crossfade imported clips, extract root motion, or query named sockets. M3 adds the smallest reusable capability: an optional model-owned SM3D v2 animation group, independent bounded model animators, and matching native/Web palette transports. It does not add Character3D, game-specific behavior, IK, morphs, retargeting, VFX, or M4 work.

## SM3D animation format

Animated SM3D remains version 2. The seven required static chunks and the 48-byte `VERT` record are unchanged. A production animated asset appends one wholly present optional group; each directory entry uses optional flag bit 0 so an older static reader may ignore it.

| Chunk | Stride | Record contract |
|---|---:|---|
| `NODE` | 64 | exact name, parent-ordered hierarchy, joint/socket flags, full bind translation/quaternion/uniform scale |
| `SKIN` | 16 | four uint16 joints plus four uint16 weights with exact sum 65,535 for every static vertex |
| `SKEL` | 80 | retained node, parent bone, complete float32 4x4 inverse-bind matrix |
| `CLIP` | 40 | exact name, duration/rate/sample count, contiguous track/event ranges, loop bit, optional root record |
| `TRAK` | 48 | clip/node, present/sampled channel flags, and AFRM first/count pairs for translation/rotation/scale |
| `AFRM` | 4 | deterministic fixed-rate float32 samples; one value per component represents a constant channel |
| `EVNT` | 20 | clip, time in milliseconds, exact name, signed value, stable descriptor order |
| `SOCK` | 64 | exact name, retained node, local translation/quaternion/uniform scale |
| `ROOT` | 24 | clip/node, XYZ extraction bits, yaw bit, remove-from-pose bit |

The group must be wholly present or absent. Native, Web, and AssetTool inspection enforce the same counts, strides, optional flags, ranges, references, finite values, quaternion normalization, uniform positive scale, exact weight sum, hierarchy, names, and reserved-zero fields. The existing 16 MiB file limit is unchanged.

The representative 68-bone fixture contains 1,520 static bytes and 12,672 animation bytes for 14,192 total bytes. It has 68 nodes/bones, five 1,000 ms clips sampled at 30 Hz, three events, one socket, and one root-motion record. The 128-bone boundary fixture is 23,100 bytes. Static v1/v2 fixtures remain byte compatible.

## Strict GLB importer and descriptor

The production profile requires exactly one skin, 1-128 joints, 1-256 retained hierarchy nodes, one `SKIN` record per vertex, and 1-64 uniquely named animations. JOINTS_0 accepts unsigned byte/short VEC4. WEIGHTS_0 accepts normalized unsigned byte/short or float VEC4 and is deterministically normalized/quantized to uint16. Bind TRS, inverse-bind matrices, sampled tracks, geometry, winding, and quaternion orientation receive the existing single glTF-to-SMILE coordinate reflection.

Translation, rotation, and positive uniform scale channels support LINEAR and STEP. The converter samples at 15-60 Hz, includes the exact final clip time, uses normalized shortest-path quaternion interpolation, and elides absent/constant channels. CUBICSPLINE, multiple skins, more than 128 bones, nonuniform production scale, morphs, duplicate targets/names, and partial skin/animation inputs fail with stable `SMA` diagnostics. Clip duration is limited to 120,000 ms.

`--descriptor <path>` accepts strict JSON descriptor version 1. It owns global/per-clip sample rates, loop policy, ordered time/name/value events, root-motion node/axes/yaw/remove policy, and exact named socket node/TRS. Unknown fields or versions fail. Conversion is deterministic and transactional; runtime glTF/GLB parsing remains prohibited.

## Runtime ownership and lifecycle

An animated `Model3D` owns the complete immutable chunk payload in addition to its part meshes and optional PBR dependencies. Each `CreateModelAnimator3D` instance borrows that payload and owns independent current/destination clip, time, speed, mode, crossfade, event FIFO, extracted root delta, pose, palette, revision, and scratch state. Imported clips do not consume legacy clip slots, and imported bones do not consume legacy skeleton slots. The shared global animator limit remains 128.

Objects borrow animators. A bound object blocks animator destruction. A live model-part object or production model animator blocks model destruction; refusals leave the public record and live counts intact. Reset order is objects, animators, models/their animation/PBR/mesh ownership, legacy clips, legacy skeletons, materials, textures, and remaining meshes. Native generation-tagged handles and Web non-reused safe-integer handles preserve stale-handle rejection.

| Resource | Current hard limit | Ownership |
|---|---:|---|
| Mesh | 128 live; 65,535 vertices and 196,608 indices per mesh | caller-owned standalone, or owned by one model; objects borrow |
| Object/model part | 512 live | borrows mesh, material, and optional animator |
| Model | 64 live; 16 parts; 16 MiB | owns part meshes, imported materials/textures/images, and immutable animation bytes |
| Texture | 128 live | caller-owned standalone or model-owned; materials borrow |
| Material | 128 live | caller-owned standalone or model-owned; objects borrow |
| Legacy skeleton | 64 live; 32 bones | caller-owned; legacy clips/animators borrow |
| Legacy clip | 128 live; 16 events | caller-owned; active legacy animators borrow |
| Animator | 128 total | caller-owned mutable state; objects borrow; production animators borrow their model |
| Production hierarchy | 256 nodes/model | model-owned immutable payload |
| Production skeleton | 128 bones/model | model-owned immutable payload and 128-matrix palette |
| Production clips | 64/model; 120,000 ms; 15-60 Hz | model-owned immutable tracks/samples |
| Production events | 64/clip; 32 pending/animator | model-owned definitions; animator-owned FIFO |
| Production sockets | 64/model | model-owned definitions; evaluated per animator |

Dragonfall remains on its existing custom Renderer3D scene and owns no models or animators. M3 does not change its 48-mesh, 441-object initial scene, 448-object boss scene, 24-material, six-texture, or 35-object effect-pool design.

## Palette and PBR parity

Direct3D 11 uses a dedicated row-major 128-matrix vertex constant buffer at b1. WebGL2 uses one shared RGBA32F 4-by-128 palette texture with vertex-stage `texelFetch`; the legacy 32-uniform path remains intact. Both simple and PBR vertex shaders consume the production palette. Uploads are cached by animator handle and pose revision, and `ModelPaletteUploadCount3D` exposes the count. The Web production update/draw source check rejects per-update `new`, `subarray`, spread, or `map`; runtime state uses preallocated typed arrays.

If the 128-bone production transport cannot be created, `ModelAnimationAvailable3D` returns false and imported animated drawing fails through the bounded Renderer3D error path. Static/simple/PBR rendering and legacy animation remain available according to their existing capabilities.

## Playback, events, root motion, and sockets

Production playback supports `ANIMATION_LOOP`, `ANIMATION_ONCE`, and `ANIMATION_HOLD` at 1-1,000 percent speed. One base-layer crossfade blends local translation/scale linearly and normalized quaternion rotation by shortest path. The destination clip supplies events and root motion during the fade.

The 32-entry pending FIFO traverses all crossed event times in chronological order, including multiple wraps; equal-time events preserve descriptor order. Overflow retains the first 32 and sets error 49. Exact named take removes the first matching queue entry without reordering the rest.

Root motion extracts descriptor-selected XYZ translation and yaw, handles loop/multi-loop boundaries, and can remove the extracted components from the visible pose. `TakeAnimatorRootDelta3D` returns one atomic thousandths-scaled XYZ/yaw value. Raw socket queries return animated model space; world queries apply the bound object's position, rotation, and scale. Socket position and 3-by-3 orientation components use thousandths.

Production node/socket/animated scale must be positive and uniform. PBR object transforms keep the M2.1 positive, nonsingular contract. The legacy two-key simple path retains nonuniform scale, with its existing error-45 PBR rejection.

## Append-only command ABI

The public bridge shapes and every command 1-97, image 1-2, and text 1-3 are unchanged. Arguments not listed below are zero.

| Numeric ID | Operation | Positional arguments | Result |
|---:|---|---|---|
| 98 | `MODEL_ANIMATION_VALUE` | `a=model, b=property, c=index` | animation presence/count/bytes/clip/event metadata |
| 99 | `CREATE_MODEL_ANIMATOR` | `a=model` | animator handle |
| 100 | `PLAY_MODEL_ANIMATOR` | `a=animator, b=clipIndex, c=mode, d=speedPercent` | success |
| 101 | `CROSSFADE_MODEL_ANIMATOR` | `a=animator, b=clipIndex, c=fadeMs, d=mode` | success |
| 102 | `ANIMATOR_CLIP_INDEX` | `a=animator` | current zero-based clip or -1 |
| 103 | `ANIMATOR_FADE_PERCENT` | `a=animator` | 0-100 |
| 104 | `ANIMATOR_PENDING_EVENT_COUNT` | `a=animator` | 0-32 |
| 105 | `SET_ANIMATOR_ROOT_MOTION` | `a=animator, b=mode` | success |
| 106 | `TAKE_ANIMATOR_ROOT_DELTA` | `a=animator, b=component 1-4` | thousandths delta; component 4 completes the atomic drain |
| 107 | `ANIMATOR_SOCKET_VALUE` | `a=animator, b=socketIndex, c=property 1-12, d=optional bound object` | thousandths position/orientation |
| 108 | `MODEL_ANIMATION_AVAILABLE` | none | production palette capability |
| 109 | `MODEL_PALETTE_UPLOAD_COUNT` | none | cached upload count |

| Text ID | Operation | Positional arguments after owned text | Result |
|---:|---|---|---|
| 4 | `MODEL_CLIP_INDEX` | `a=model`; text=exact clip name | zero-based index |
| 5 | `MODEL_SOCKET_INDEX` | `a=model`; text=exact socket name | zero-based index |
| 6 | `MODEL_EVENT_NAME_MATCHES` | `a=model, b=one-based event`; text=exact name | Boolean |
| 7 | `PLAY_MODEL_ANIMATOR` | `a=animator, b=mode, c=speed`; text=exact clip name | success |
| 8 | `CROSSFADE_MODEL_ANIMATOR` | `a=animator, b=fadeMs, c=mode`; text=exact clip name | success |
| 9 | `TAKE_MODEL_ANIMATOR_EVENT` | `a=animator`; text=exact event name | one-based event or zero |

No image command was added. The next free IDs are numeric 110, image 3, and text 10.

The complete dispatch routes remain:

| Layer | Path |
|---|---|
| Public facade/constants | `libraries/Smile.Simple3D/Graphics3D.smile`, with mirrored records in `Core.smile` |
| Built-in bridge arity/types | `src/Smile.Language/Syntax.cs`, `src/Smile.Language/Semantics.cs` (unchanged) |
| Native emission | `src/Smile.Compiler/MasmEmitter.cs` -> `smile_renderer3d_command` / `_image_command` / `_text_command` |
| Native declarations/IDs | `src/Smile.NativeRuntime/graphics/graphics3d.h` |
| Native numeric, animation, palette, PBR dispatch | `src/Smile.NativeRuntime/graphics/graphics3d_directx.cpp` |
| Native owned-text routing | `src/Smile.NativeRuntime/runtime.c` -> `smile_renderer3d_model_text_operation` |
| Web emission | `src/Smile.Compiler/WebEmitter.cs` -> `smile.renderer3D`, `renderer3DImage`, awaited `renderer3DText` |
| Web validation, sampling, palette, shader, all dispatch | `src/Smile.Compiler/WebOutputWriter.cs` |

## Deterministic fixture and Animation Lab

`scripts/generate-renderer3d-animation-v2-fixtures.ps1` owns the compact 68-, 128-, and rejected 129-bone GLBs, descriptor, converted SM3Ds, partial-group SM3D, and bad-weight SM3D. `-Check` regenerates conversion outputs in a temporary directory, compares byte for byte, verifies lab publication, and reports hashes.

| Fixture | Bytes | SHA-256 |
|---|---:|---|
| `AnimationActor68.glb` | 12,272 | `927688356870150B44C4263E1D58F90782826745E50F0A2BF8CFEEB16CD55FC0` |
| `AnimationActor128.glb` | 20,156 | `3B94CD611280AF4C233FC64275B202F9E696DEBBEE945FC01226EC96D7A045D7` |
| `AnimationActor129.glb` | 20,292 | `8F0D7631C95A810772E82A2505D4E768644CADCE2BBE859C8D9CC2BFF0A0B149` |
| descriptor v1 | 427 | `DC3FB5B50BD0B4238C15A0C96A33E234FE3EC5D4363DE34B3D9D02B04F798A11` |
| `AnimationActor68.sm3d` | 14,192 | `71C74566C4D93CA49FEC16F15EB51A0B70E1F7FEF064231359AAA28C95E3D97E` |
| `AnimationActor128.sm3d` | 23,100 | `55AA55DD4B4961EF156F7483AB20B0483164B6DB62948811E29EC77D6596E176` |

The native/Web Animation Lab draws two independent animators over one 68-bone PBR model, the animated SwordTip socket marker, bounded root-path markers, and a Renderer2D diagnostic overlay. Controls select/play/crossfade five clips and toggle extraction, application, socket display, diagnostics, reset, and shared camera control. A verified frame uses 1 model, 12 objects, 2 animators, 12 draws, 112 submitted triangles, and one PBR model path. Focused teardown returns models, objects, and animators to zero.

## Plan mapping and deviations

- The handoff's production model animation is implemented by extending the existing generation-safe `Animator3D` resource with a model-owned mode, instead of introducing a parallel public resource type. Legacy handles, commands, limits, and semantics remain unchanged.
- All nine animation chunks use the v2 optional-directory bit and all-or-none validation. No v3 marker, `VERT` stride change, or file-limit increase was needed.
- Raw socket queries are preserved alongside an explicit world overload that requires the bound object. This makes the object transform dependency visible and prevents querying an unrelated instance.
- The Web palette follows the requested RGBA32F vertex-texture path. The native palette uses the requested fixed b1 buffer. Both cache by animator/revision.
- The focused visual pass found and corrected a Web-only socket composition-order defect; exact native/Web assertions now lock model-space `1517` and world-space `303355` for the deterministic attack pose.
- Once and hold both retain the final pose; the explicit playback mode and completion state remain available for caller policy without adding a second terminal pose representation.
- The fixture actor is deliberately one deterministic skinned triangle with two-sided PBR metadata. It proves conversion, deformation, palette transport, crossfade, socket, root, and ownership behavior without importing an external copyrighted asset.
- No Character3D, IK, morphs, retargeting, VFX, shadows, HDR, Dragonfall conversion, or M4 behavior was implemented.

## Validation evidence

Validation completed on 2026-08-31 (Asia/Taipei) with .NET SDK 10.0.302 from `C:\Users\louie\AppData\Local\Microsoft\dotnet`. The system `dotnet` host did not resolve the repository-pinned SDK on the first build attempt; prepending the installed SDK root corrected the environment without changing the pinned SDK or project target.

| Command or gate | Exact result |
|---|---|
| `scripts/build.cmd` | PASS; native runtime/tests, compiler, AssetTool, language tests, templates, and VSIX built. The expected `NU1503` native `.vcxproj` restore warning remained non-fatal. |
| `scripts/test-smile-formatter.ps1` | PASS; 13 formatter cases. |
| `scripts/format-smile-style.ps1 -Check -FormatLongIf` | PASS; 325 repository SMILE files required no changes. |
| `scripts/test-renderer3d-animation-v2.ps1` | PASS; deterministic fixtures/import diagnostics, native/Web exact output, 128-bone boundary, playback/crossfade/events/root/socket/palette/lifecycle/malformed rollback, and Animation Lab builds. |
| `scripts/test-renderer3d-lifecycle.ps1` | PASS. |
| `scripts/test-renderer3d-materials.ps1` | PASS. |
| `scripts/test-renderer3d-models.ps1` | PASS. |
| `scripts/test-renderer3d-m11-hardening.ps1` | PASS. |
| `scripts/test-renderer3d-v2-boundaries.ps1` | PASS; the script intentionally rejected invalid inputs. A surrounding ad-hoc wrapper subsequently observed the expected rejection exit code and reported a false failure; the gate itself completed successfully. |
| `scripts/test-renderer3d-animation.ps1` | PASS; legacy 32-bone native/Web exact parity retained. |
| `scripts/test-renderer3d-pbr.ps1` | PASS. |
| `scripts/test-renderer3d-pbr-hardening.ps1` | PASS. |
| `scripts/test-battle3d.ps1` | PASS. |
| `scripts/test-simple3d-space-wars.ps1` | PASS. |
| `scripts/test-dragonfall.ps1` | PASS; native/Web and demo/no-demo lifecycle coverage. |
| Native manual Animation Lab | PASS; Walk crossfade accepted, current clip 1, root X advanced, two independent actors, SwordTip marker, PBR, 12 draws/112 triangles. |
| Web manual Animation Lab | PASS; both actors and world-space SwordTip marker rendered; socket Y matched native at 314000 in Idle; no browser console errors. |
| `scripts/smoke-test.cmd` | PASS; complete repository native/Web compiler, language, library, UI, RPG, game, packaging, artifact, viewport, DPI, and VSIX verification. The first full run found three stale 2.0.48 verifier expectations; after correcting them, `scripts/verify-artifacts.ps1` and the complete smoke rerun passed at 2.0.49. |
| `scripts/verify-artifacts.ps1` | PASS; VSIX payload plus identity, assembly, file, and product versions synchronized at 2.0.49. |
| `scripts/install-vsix.cmd` | PASS; rebuilt and installed `artifacts/vsix/Smile.VisualStudio.vsix` into Visual Studio instance `91f001b5`. Installed assembly version 2.0.49.0; built/installed DLL SHA-256 `39D6272A01F658AAB895D8253169025C17894EDD123ED5A7D77E8DD99267F7D6`. |

The short manual launch is a functional visual check, not a benchmark. The lab clamps one update to at most 50 ms and performs no production update/draw allocation. Live palette uploads vary with the number of frames and pose revisions; cache behavior is asserted by the focused gate rather than inferred from an absolute launch count.

## M4 readiness

M4 is unblocked: the focused regression gates and complete smoke suite pass, and the validated VSIX is installed. M4 may build generic higher-level character presentation on the exact model animator, root delta, event, and socket APIs. It must not replace the immutable model ownership, append-only ABI, or bounded palette contracts established here.
