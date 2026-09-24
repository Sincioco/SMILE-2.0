# DLSS 5 / Kael Party native experiment

## Outcome: blocked at the official feature-package prerequisite

Experiment date: **September 24, 2026 (+08:00)**. Branch: **`DLSS5`**.
Baseline: **`d6cfdc91bb42013c2f4a29593ab718a61fdee70d`**, the matching local
`main` and fetched `origin/main`. Neither mainline ref was modified or merged.
No pre-existing local or remote `DLSS5` branch was present.

The complete revised instruction file in Sin's Downloads folder was read:
`2026-09-24-2148-smile-2.0-dlss5-kael-party-experiment-revised.md`.
It supersedes the earlier 21:36 version. Its blocked-outcome rule applies here.

**The obtained official Streamline 2.14.1 x64 package identifies DLSS neural
rendering but does not contain its feature implementation or integration
contract.** There is no usable DLSS 5 integration in this experiment. Renderer,
language, compiler, Viewer source, calibration and Web behavior are unchanged.

| Milestone | Actual result |
| --- | --- |
| Official Streamline package acquired and inspected | Yes, 2.14.1 |
| Streamline initialized in SMILE | No; no integration was added |
| DLSS 5 feature support query | Not executed; required plugin is absent |
| DLSS 5 feature available to this experiment | No runnable implementation obtained |
| DLSS 5 feature initialized/evaluated/active | No / no / no |
| Real Kael Party through existing D3D11 | Yes; baseline validation below |
| Real Kael Party through DLSS 5 | **No — blocked** |
| D3D12 requirement verified or D3D12 checkpoint implemented | Neither |

This is an investigation completed with a blocked integration result, not a
successful or partially functioning neural-rendering implementation.

## Official SDK evidence and access

All URLs below were accessed on September 24, 2026. The
[evidence inventory](dlss5-sdk-evidence.json) records the actual package's headers,
guides, relevant DLL versions, hashes and source revision.

| Evidence | Result |
| --- | --- |
| [Official release](https://github.com/NVIDIA-RTX/Streamline/releases/tag/v2.14.1) | Latest API response: `v2.14.1`, published `2026-09-08T15:29:48Z` |
| [Official x64 download](https://github.com/NVIDIA-RTX/Streamline/releases/download/v2.14.1/streamline-sdk-v2.14.1.zip) | 275,994,000 bytes; GitHub asset ID `550764153` |
| Archive SHA-256 | `92c4d954631a1710da86ca3fa8d5034f2b9503838c95fc4ae977ae149319781b` |
| GitHub release asset digest | Exact match to the downloaded archive |
| `include/sl_version.h` | 2 / 14 / 1 |
| `include/sl_core_types.h:253` | `kFeatureDLSS_NR = 1004` |
| `include/sl_helpers.h:347` | Maps that identifier to `dlss_nr` |
| Package `changelog.txt` | Names `sl.dlss_nr` under Release 2.14.0 |
| Feature implementation | No `sl.dlss_nr.dll`, NR feature header, NR guide, or NR plugin source found in the 501 extracted files |
| Existing DLSS feature headers | `sl_dlss.h`, `sl_dlss_d.h`, `sl_dlss_g.h` |
| Existing DLSS DLLs | `sl.dlss`, `sl.dlss_d`, `sl.dlss_g`: 2.14.1.0; corresponding `nvngx_dlss`, `nvngx_dlssd`, `nvngx_dlssg`: 310.9.1.0 |
| Signature check | Production `sl.interposer.dll`: Valid, NVIDIA Corporation |
| [Public Streamline source](https://github.com/NVIDIA-RTX/Streamline/tree/2122257e0fce486f91b385aa63b9a09b0a34b363) | Complete GitHub recursive tree, not truncated; same absence of NR-specific files |

The ZIP, extracted package, release metadata and source-tree response remain
outside the repository at:

```text
C:\Users\louie\AppData\Local\SMILE-Experiments\DLSS5\Streamline-2.14.1
```

This resolves what **this distribution actually contains**. It does not resolve
why the [changelog](https://github.com/NVIDIA-RTX/Streamline/blob/2122257e0fce486f91b385aa63b9a09b0a34b363/changelog.txt)
mentions an unshipped plugin. A feature-number declaration is insufficient to
invent its option structs, buffers, initialization or evaluation contract.

The [DLSS download overview](https://developer.nvidia.com/rtx/dlss) still describes
DLSS 4.5 and links to Streamline and existing Unreal packages. Its September 2026
package note lists SL 2.14.1 / NGX 310.9.1. The
[September 22 NVIDIA developer article](https://developer.nvidia.com/blog/whats-new-for-game-developers-dlss-5-with-3d-guided-neural-rendering-nvidia-ace-updates-and-new-rtx-kit-capabilities)
describes DLSS 5 and directs developers to a
[notification form](https://developer.nvidia.com/rtx/dlss/notify-me), not a
feature-specific SDK download. That form was inspected as a link, not submitted.

The official `NVIDIA/DLSS` recursive tree was also checked at
`374959484e79a640feaba44c93ac8cfb0a03f5b5`: it exposes existing NGX SR/RR/FG headers,
not a neural-rendering integration package. Unofficial repositories, wrappers,
game-extracted DLLs and injectors were not used. Community requests for access
were not treated as an NVIDIA statement of availability or requirements.

No NVIDIA authentication, NDA or separate access approval was bypassed. No
verified private-access package or entitlement was available in this task.
This result does **not** assert that DLSS 5 does not exist, that no partner SDK
exists, or that public distribution can never become available.

### Licensing and next access action

The package includes `license.txt` for Streamline core and separate NVIDIA runtime
terms in `bin/x64/nvngx_dlss.license.txt` and other license files. Core's license
must not be generalized to proprietary feature DLLs or a future NR package.
No SDK sources, DLLs, archives, installers or license-controlled runtime files
are committed or deployed beside the Viewer. No driver/SDK installer was run.

**Required next action:** obtain from NVIDIA an authorized DLSS 5 NR development
package and its matching integration guide, runtime/model distribution terms,
supported APIs, minimum OS/driver, and custom-engine application-identity process.
The official developer DLSS forum is linked from the download page; Sin can ask
NVIDIA there or through an existing developer contact to explain the 2.14.1
changelog/package mismatch. No support message was sent on Sin's behalf.
If NVIDIA requires sign-in, a license acceptance, application ID, NDA or manual
approval, Sin must complete that step before integration resumes. The existing
Streamline identity fields are not proof of the NR access requirements.

## Environment and verified feature requirements

| Item | Observed value / confidence |
| --- | --- |
| OS | Windows 11 Pro, 10.0.26200 |
| GPU used by launched Viewer | NVIDIA GeForce RTX 5090; Viewer PID 23944 listed by `nvidia-smi` on GPU 0 |
| NVIDIA driver | 595.79; Windows driver version 32.0.15.9579 |
| GPU memory | 32,607 MiB reported by `nvidia-smi`; not the truncated WMI AdapterRAM value |
| Other installed adapter | AMD Radeon(TM) Graphics; not an AMD validation run |
| Viewer artifact | Release build, SMILE 2.0.64, compiled September 24, 22:51:58 +08:00 |
| Executing graphics path | Project selects DirectX; implementation creates D3D11; process loads `d3d11.dll`, `d2d1.dll`, `DWrite.dll` |
| NVIDIA feature runtime loaded | No `sl.*` or `nvngx*` module observed in the Viewer |
| SDK examined / SDK used by application | Streamline 2.14.1 / none |

NVIDIA's [DLSS 5 research overview](https://research.nvidia.com/labs/adlr/DLSS5/)
identifies RTX 50 Series hardware, rendered frame color, engine motion vectors,
temporal state and artistic controls at a high level. The RTX 5090 belongs to
that series; this alone is not a successful feature-support query.

| Requirement | Established for DLSS 5 NR? |
| --- | --- |
| Core feature identifier / plugin name | Yes: the package declares 1004 / `dlss_nr`; plugin implementation absent |
| D3D11 support | **Unknown**; Streamline-wide D3D11 support is not NR support |
| D3D12 required or supported | **Unknown**; no feature contract obtained |
| Minimum driver / Windows version | Unknown; installed driver was not upgraded speculatively |
| Motion-vector formats, units, direction, resolution, invalid-pixel rules | Unknown; engine vectors are a high-level input, not a complete specification |
| Depth, normals or other geometry buffers | No verified required-resource contract |
| Color space, HDR/LDR, format, alpha and dimensions | No verified feature contract |
| Exposure, camera matrices, jitter, reset flags | No verified feature contract |
| Skinned geometry, transparency, VFX, reflected motion | No verified feature-specific guidance |
| HUD exclusion, masks and composition point | High-level masking controls described; binding/order contract unavailable |
| Init/shutdown, thread/queue ownership and synchronization | No verified feature-specific contract |
| Runtime/model redistribution and application approval | Must be established for the actual NR package |

These unknowns are deliberately not filled with Super Resolution, Ray
Reconstruction or Frame Generation requirements.

## Repository investigation and ownership

The Viewer architecture/README, Kael creation journey, native graphics owners,
Viewer frame callers, native tests and current recovery checkpoint were reviewed.

| Owner / reachable code | Existing behavior and integration consequence |
| --- | --- |
| `src/Smile.NativeRuntime/graphics/graphics_directx.cpp`, `SmileDirectXState` | Owns D3D11 device/immediate context, DXGI swap chain, backbuffer RTV and Direct2D/DirectWrite resources. Device creation uses hardware D3D11 with BGRA support. Flip-discard swap chain is BGRA8. |
| `graphics_directx.h`, `graphics_backend.h` | Existing backend vtable owns 2D/window lifecycle. Renderer3D accesses D3D11 device/context/target through explicit native accessors; it is not already an API-neutral D3D12 backend. |
| `graphics3d_directx.cpp`, `smile_3d_create_pipeline` | Basic and PBR shaders skin current positions with current bone palettes. They output current clip position; there is no screen-space motion-vector render target/output. |
| `SmileAnimator3D`, `SmilePaletteSnapshot3D`, `SmileSubmission3D` | Animation times, current pose/offsets and immutable **same-frame** draw snapshots exist. `previous_time_ms` supports animation/event traversal; it is not previous-rendered-frame skeletal geometry. Frame palette/submission counts are cleared at end. |
| `smile_3d_create_targets` / target allocation region | Optional RGBA16F HDR color, MSAA/resolve, D24S8 depth. Soft-particle depth can use R24G8 typeless plus shader view and an R32F linear-depth snapshot. These are existing resources, not validated NR bindings. |
| `smile_3d_end`, `smile_3d_run_post_processing` | Shadows and reflected submissions, opaque scene, depth snapshot, distortion, transparency and particles precede MSAA resolve/bloom/tone mapping. Final color is copied/composited to the DXGI backbuffer. Exposure is an existing percentage control. |
| `graphics3d_reflections.cpp/.h` plus renderer replay | Separate D3D11 reflection targets/depth and mirrored camera. Replays accepted current-frame actor/VFX snapshots without advancing actors twice. Backdrop reflection uses the accepted screen-fixed image region. |
| `smile_graphics_directx_suspend_2d/resume_2d` | Ends Direct2D before 3D and resumes it afterward. A future scene-only neural pass must finish before crisp Viewer UI/text; exact placement relative to tone mapping still requires NR documentation. |
| `smile_directx_resize`, `smile_graphics3d_on_device_lost` | Resize releases 3D GPU resources, clears D3D11 state, resizes buffers and rebuilds backbuffer/D2D targets. Renderer snapshots/resources are invalidated. Present records device-removed/reset reasons; no new recovery guarantee is claimed. |
| `tools/Character3DViewer/NativeProgram.smile`, `NativeViewerHost.smile` | Thin native window/host and frame delegation. Startup defaults to Battle System; **Kael Party must be explicitly selected** for this acceptance scene. |
| `ViewerWorkflow`, `ViewerLifecycle`, `ViewerSession`, `Profiles` | Own scene loading, ordered updates and tab/profile selection. Kael Party uses tab 14 and the canonical Kael profile, without a second renderer. |
| `ViewerParty`, `ViewerDragon`, `ViewerActors`, `ViewerEffects` | Own four heroes, boss, final grounded transforms, equipment and effects. Party Kael is 3x solo; Earth/Water casts stay at home. |
| `ViewerRendering.DrawFrame` | Configures shared Arena3D/backdrop, draws floor/grid/actors/equipment/effects, ends Scene3D; native host then draws UI and presents. |

No new state owner, abstraction, API, compiler syntax, module or dependency was
introduced. Production source growth is **zero**. Existing large renderer and
Viewer owners were not enlarged; no guardrail or exception was changed.

### Temporal work that would actually be needed

SMILE currently cannot provide a verified NR motion input. If the eventual
contract requires per-pixel rendered motion, the renderer must retain previous
**rendered** camera and object transforms plus final skinned palettes, including
root movement, calibration, sockets and weapons. Current animation time alone
cannot represent camera movement, scale/rotation changes or edited poses.

First frame, tab/scene change, camera cut, teleport, resource recreation,
resize/reset and newly visible/reused object handles need deliberate invalid
history rules. Hidden/disappearing objects must not reuse unrelated history.
VFX, transparency and reflected animated geometry need the official treatment;
particle simulation velocity is not screen-space optical motion. No zero-filled
or camera-only vectors were supplied as a substitute.

### Bounded D3D12 decision

No evidence establishes that D3D12 is required, so no backend implementation was
started. This follows the revised prerequisite gate, not a prohibition on D3D12.
If NVIDIA establishes that requirement, first evaluate documented D3D11/D3D12
resource interop or D3D11On12 **only if NR explicitly supports that arrangement**.
Existing D3D11 objects cannot simply be cast to D3D12 resources.

The smallest acceptable follow-up would keep one Viewer/scene and the existing
D3D11 fallback, with experimental device/queue, resource lifetime and presentation
ownership behind native graphics. Prove the actual Kael Party scene with DLSS OFF,
including reflections, VFX and Direct2D UI, and commit that checkpoint separately
before adding NR. If interop is unsupported and full Renderer3D command/resource
reimplementation is necessary, return a staged scope proposal; do not silently
turn this branch into a whole-engine port or unrelated demo project.

## Validation and observations

- `pwsh -NoProfile -File scripts/test-character-3d-viewer-hardening.ps1 -NativeOnly`
  **passed**. Includes existing architecture/owner assertions, 49 Arin calibration
  checks, isolated real-asset Viewer calibration and JSON round trips, recovery
  evidence checks, and 58 native graphics/pointer/audio checks. Live saves were
  not used as failure fixtures.
- The old Viewer was inspected and closed normally with Alt+F4. Then
  `pwsh -NoProfile -File tools/Character3DViewer/Launch.ps1 -Build -SkipWindowActivation`
  **passed**: native Release executable rebuilt, 164 project assets published,
  both canonical calibration JSON files retained byte-for-byte, process 23944
  launched. No force termination was used.
- Runtime/DLSS code did not change. This native baseline also validates the final
  production source; a second identical suite after documentation edits would
  not test different executable behavior. Full repository smoke and Web tests
  were not run; the smoke script includes held Web work. No Web acceptance claim.
- Visible validation: selected the real **Kael Party** tab in the rebuilt Viewer,
  observed changing turns, skinned poses, sword/shield effects, the reflective
  floor/grid and fixed bitmap background. UI/text remained readable; no recovery
  overlay or obvious rendering corruption appeared in this brief observation.
  Battle System also rendered before the tab switch. This was not a complete
  nine-turn visual acceptance run or an exhaustive inspection of every effect.
- The Viewer remained at captured desktop position `(481, 0)`, window capture
  size `1421 x 1025`, beside Codex. The capture includes window chrome; internal
  render/output dimensions were not separately instrumented. The captured FPS
  label was 93, from the existing half-second counter, with other observations
  varying with camera/focus. This is not a controlled average or DLSS comparison.
- Manual resize was **not verified**: the attempted edge drag did not change
  the captured bounds; subsequent input attempts overlapped live user input.
  The existing graphics tests cover resize routing with a mock backend, not
  actual GPU resize. Real-asset calibration tests exercised existing resets and
  multiple tabs; forced device removal/recovery was not tested.
- Final JSON parsing, archive-digest comparison, documentation review and
  `git diff --check` passed. Production source and canonical calibration changes
  relative to the baseline were empty.

Logs remain ignored and local:

```text
artifacts/temp/dlss5-native-baseline.log
artifacts/temp/dlss5-viewer-build-launch.log
artifacts/temp/Character3DViewerHardeningTests.out
artifacts/temp/Character3DViewerHardeningTests.err
artifacts/temp/dlss5/kael-party-dlss-off.png
```

No NR support/init/evaluate, ON/OFF transition, unsupported-GPU runtime fallback,
DLSS allocation failure, GPU timing, VRAM delta or startup-overhead measurement
was possible. Normal rendering with no NVIDIA plugin deployed remains usable;
that is not proof of newly implemented optional-feature fallback handling.
No DLSS ON capture or quality/performance comparison exists. No average FPS or
frame-time benchmark is claimed, especially while the desktop is livestreaming.
No forced driver/device removal was performed.

## Existing issues, files and next step

The old running Viewer displayed `Saved Keys Rejected: Character Profile Mismatch`
before this experiment changed anything. Its normal rebuild/relaunch retained the
authoritative JSON bytes; the rebuilt scene showed the normal saved-key status.
The pre-restart working-save hash was not captured in this run. This symptom belongs to the
[existing native Arin load-rejection investigation](../implementation/party-beat-camera-checkpoint.md),
not to DLSS. The initial rejection cause remains unresolved; no calibration
rewrites or claimed fix are included here. The separately paused intermittent
Kael Party renderer-recovery investigation also remains unresolved and held.

Changes on `DLSS5`:

- `docs/experiments/dlss5-kael-party.md`: this report.
- `docs/experiments/dlss5-sdk-evidence.json`: reproducible public-package inventory.
- `docs/implementation/party-beat-camera-checkpoint.md`: observed recurrence in
  the existing calibration issue, with its validation limits and next action.
- `games/SinStarI/Assets/Characters/3.png`: pre-existing user artwork, preserved
  unchanged under Sin's explicit permission to commit unstaged files. Separate
  commit `8a99d87`; no new runtime reference.

**Recommendation:** keep this branch isolated and do not merge into `main`.
Resolve NVIDIA's feature-package/contract gap first, then reassess the verified
API and driver requirements. No .NET or VSIX rebuild, browser refresh, SDK install
or driver upgrade is required to review these findings. The native Viewer was
already rebuilt for baseline testing.

**On Hold:** Studio creation/development/acceptance; all Web adoption/publication/
browser validation; the previously paused Kael Party recovery investigation.
