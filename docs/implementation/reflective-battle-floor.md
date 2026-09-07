# Reflective Battle Floor Continuation Checkpoint

This is the one compact tracked checkpoint for the reflective battle-floor
milestone and its bounded September 8, 2026 hardening repair. It records the
reviewed instruction identities, architecture, public contract, integration
defaults, corrected acceptance claims, and current evidence without reopening
completed Character Viewer refactor work.

## Instruction identity and preserved baseline

- Initial implementation archive:
  `C:\Users\louie\Downloads\2026-09-07-2320-smile-2.0-reflective-battle-floor.zip`
- Archive SHA-256:
  `5EB39768B493943CFC197080A43BA69FBD47D8ECEA78A9603398E08E5E2641A5`
- The package's Markdown instructions and included supporting documents were read
  before implementation. The reference image was used only as visual direction
  and was not added to the repository.
- Hardening archive:
  `C:\Users\louie\Downloads\2026-09-08-0609-smile-2.0-reflection-hardening-before-double.zip`
- Hardening manifest identity:
  `smile-2.0-reflection-hardening-before-double`, revision `1`, reviewed commit
  `61a64c41a2fe7d681979f84f31b09975561af508`.
- The numbered hardening documents were read in order. Document 04 remains
  reference-only; this repair does not implement Double.
- The repair baseline reconciled once at
  `72801a46a130bf950dde1263e0d4b6841b93d006`, where local `HEAD` and
  `origin/main` matched, the reviewed commit was an ancestor, and the worktree
  was clean.
- The completed continuation baseline and milestones documented in
  `docs/implementation/post-refactor-continuation.md` remain preserved.
- H01/H02/H03 remain closed. The Doctor repair path and retired H01 test were not
  rerun or recreated.
- `tools/Character3DViewer/Program.smile` remains the accepted, unchanged
  1,851-line application coordinator. This milestone extends its focused owners
  and shared runtime rather than restarting the refactor.

## Result

SMILE provides one bounded horizontal planar-reflection facility shared by native
Direct3D 11 and WebGL 2. It composes the ordinary production view with an explicit
improper reflection transform at the effective floor plane, replays the current
frame's eligible opaque and masked 3D submissions with parity-correct face
culling, and composites the result only on the designated receiver.

The reflected view uses the same immutable per-frame object snapshots as the main
view. It does not advance animation, simulation, events, VFX, or audio a second
time. The screen-fixed bitmap backdrop is captured without camera rotation, while
the receiver, grid, debug helpers, gizmos, translucent VFX, and non-object
submissions are excluded from reflected geometry.

## Responsibility map

| Responsibility | Native owner | Web/shared owner |
| --- | --- | --- |
| Reflection render target, depth target, allocation cap, retry generation, teardown | `src/Smile.NativeRuntime/graphics/graphics3d_reflections.cpp` | `src/Smile.Compiler/WebRuntime/Renderer3DReflections.js` |
| Mirrored-camera replay, clipping, reverse culling, receiver composition, counters | `src/Smile.NativeRuntime/graphics/graphics3d_directx.cpp` | `Renderer3DReflections.js` plus the existing generated Renderer3D frame owner |
| Append-only runtime dispatch | Commands 133-135 in `graphics3d.h` | Matching dispatch cases emitted by `WebOutputWriter.cs` |
| Public SMILE configuration, roles, fallback/query contract | `libraries/Smile.Simple3D/Graphics3D.smile` | Same shared SMILE source |
| Optional polished arena floor | `libraries/Smile.Simple3D/Arena3D.smile` | Same shared SMILE source |
| Character Viewer state, rendering, Party routing, labels, tests | Focused `ViewerRendering`, `ViewerUi`, `ViewerParty`, inspector, and hardening owners | Same Viewer sources on both targets |
| Sin Star I production preview | `games/SinStarI/BattleArenaPreview.smile` | Same game source on both targets |

No Viewer renderer owner was copied into Sin Star I. Both integrations consume
the shared `Smile.Simple3D` API and the target runtime implementation.

## Public contract

`Graphics3D.ConfigureReflections3D` configures the next frame using:

- requested On/Off state;
- strength percent;
- softness percent;
- render-target scale percent;
- horizontal floor height, or receiver-derived height when negative;
- backdrop inclusion.

`Graphics3D.SetObjectReflectionMode3D` and its checked form assign one of three
roles: excluded, eligible, or receiver. Created 3D objects default to eligible.
The `Arena3D.Create(..., Optional Reflective As Boolean = False)` default keeps
ordinary reusable arenas matte. The Character Viewer and Sin Star I Battle Arena
Preview explicitly request reflection and default it On.

When floor height is negative, the runtime resolves one horizontal plane from the
immutable receiver submission, including its submitted model transform. An
explicit floor height must agree with that receiver. Tilted receivers or multiple
incompatible receiver planes fail soft as unsupported instead of mixing different
planes across view, clipping, culling, and receiver sampling.

`Graphics3D.ReflectionValue3D` exposes the last completed frame's requested and
effective states, fallback reason, target dimensions and byte ownership, reflected
draw/triangle/capture/composition counts, configuration revision, resource
generation, configured quality values, effective floor height, effective target
format, and production receiver-sample error. These diagnostics make the
zero-work Off path and fallback behavior directly testable on both targets.

## Quality, bounds, and failure behavior

- Native HDR reflection capture uses `R16G16B16A16_FLOAT`. Web uses
  `RGBA16F`/`HALF_FLOAT` when `EXT_color_buffer_float` is available, with
  `OES_texture_float_linear` when supported and a valid nearest-filter fallback
  otherwise. The direct LDR path uses `RGBA8`.
- The target follows viewport and quality scale but is capped at 2,048 pixels on
  its longest side. Strength and five-tap softness are independently controlled.
- Turning reflections Off performs zero reflection captures, reflected draws, or
  receiver compositions for that frame.
- Missing receiver, below-plane camera, allocation failure, and render failure are
  explicit fail-soft outcomes. The main scene continues normally and the UI can
  report `Battle Floor: Unavailable`.
- A failed allocation is cached for the current configuration, viewport, and
  effective target format instead of being retried every frame. A new
  configuration boundary, including Off then On, or a compatible format change
  permits a bounded retry.
- Resize, renderer reset, device loss, and Web context lifecycle paths release or
  invalidate reflection-owned resources with the parent renderer. Reset reports
  zero target bytes.

## Character Viewer behavior

- Individual and Party views default to `Battle Floor: Reflective`.
- The visible control and `R` key switch between `Battle Floor: Reflective` and
  `Battle Floor: Original`; Party pointer routing consumes the click so it cannot
  leak into camera or other controls.
- `Unavailable` is reserved for a requested reflection whose optional target or
  pass failed. The floor remains usable and matte.
- The shared battle-arena recipe uses a mirror-oriented `85` percent strength,
  `5` percent softness, and `100` percent target scale. The original matte floor
  remains available through the existing button and `R` toggle.
- Reflective mode mirrors eligible characters and equipment with the real mirrored
  camera. Because the backdrop is a flat screen-fixed image rather than 3D geometry,
  its reflection is bounded by the topmost projected receiver edge and samples only
  the image region visible above that edge; floor-covered backdrop pixels cannot be
  revealed in the reflection. The original grid is excluded from the capture and
  drawn above the receiver in both floor modes. HUD, inspector, socket helpers,
  transform gizmos, and VFX retain their established ownership and are not reflected.
- The animation diagnostics begin below the floor toggle and are suppressed at
  compact heights where they would overlap lower controls.
- Reset restores the documented default. Live character calibration remains
  canonical and was not changed by this milestone.

## Sin Star I Battle Arena Preview

The former Battle placeholder now opens a bounded production preview that uses the
shared arena, backdrop, renderer, character, scene, and interaction modules. It
plays Arin v5.7's authored Idle clip with sword and shield, dynamically centers
and grounds the actor from live socket data, and renders the same-frame character
and equipment reflection.

The preview includes a visible On/Off control and `R` shortcut, smooth pointer
pan/orbit/wheel camera controls, optional automatic orbit toggled with `O`, and
`Escape` return to the menu. Exit and re-entry reset renderer-owned state and
resynchronize the Character3D cache epoch; they do not import or copy Character
Viewer code. The bitmap backdrop stays screen-fixed while the actor, grid, camera,
and reflection move together.

## Corrected acceptance boundary

The first checkpoint overstated three areas. Scalar mirror arithmetic proved only
a math sanity check, not the production view/projection composition. Draw counters
proved replay but not rasterized winding or visible placement. Source selection of
an HDR renderer did not prove that the Web reflection target preserved values
above the LDR range, because the target was still `RGBA8`. Those claims are
superseded by the production sample diagnostic, immutable receiver-plane fixtures,
format transition checks, and actual asymmetric native/installed-Chrome rendering
listed below.

## Acceptance matrix

| ID | Requirement | Evidence |
| --- | --- | --- |
| R01 | Production reflected view/projection and receiver plane | Native/Web fixtures exercise the actual model/view/projection path at translated positive and negative receiver heights. The receiver sample diagnostic stays within two millionths; mutating the live object after submission cannot move the effective plane. |
| R02 | Fixed bitmap backdrop | Native/Web reflection owners capture the backdrop as a screen-space pass. Chrome Viewer and Sin Star I orbit checks preserved the fixed asymmetric backdrop while the 3D view moved. |
| R03 | Current live pose and equipment without double advancement | Reflection replay consumes immutable frame submissions. Viewer hardening plus live Chrome animation showed current actor, sword, and shield; no animation/event/audio update exists in the reflection pass. |
| R04 | Eligibility, culling, and exclusions | Source/fixture checks cover immutable eligible, receiver, excluded, opaque/masked, and non-object policies. An asymmetric single-sided triangle visibly matches in native and installed Chrome, including the reflection winding reversal. Grid and helpers remain explicitly excluded. |
| R05 | Individual and Party UI ownership | Viewer hardening covers On/Off/Unavailable labels, individual and Party hit regions, hidden-state rejection, consumed Party actions, reset default, and both toggle paths. Chrome Party toggling removed and restored the reflection. |
| R06 | On/Off/recreate lifecycle and zero work | Native/Web fixtures prove On-Off-On, receiver absence, camera below plane, stable generation, reset cleanup, and zero captures/draws/compositions while Off. |
| R07 | HDR and LDR composition | The focused native/Web fixture transitions LDR to HDR and back at one viewport, proving effective format `RGBA8`/`RGBA16F`, resource-generation changes, and 8/12 bytes per pixel including depth. Actual Chrome renders the HDR full-resolution mirror path without console errors. |
| R08 | Immutable ownership | Fixture mutation after submission cannot change reflection eligibility; mid-frame role/configuration changes are rejected; teardown releases target ownership. |
| R09 | Fail-soft allocation and retry | Native and Web forced-failure-once runs prove main-frame completion, cached failure without per-frame retry, and successful retry after an explicit configuration boundary. |
| R10 | Resize, reset, below-plane, and context recovery | Target compatibility includes viewport size and format; native reset/device-loss and Web context hooks invalidate owned resources. Focused tests cover reset bytes, recreation, below-effective-plane fallback, failure caching, and bounded retry. |
| R11 | Shared Viewer/game reuse and clean re-entry | Static gate rejects Character Viewer ownership in Sin Star I. Live Chrome checks exercised toggle, exit, clean re-entry, animation, auto-orbit, and a zero-error console. |
| R12 | Existing behavior and permanent regression | Reflection gate is part of `scripts/smoke-test.cmd`; the final milestone validation runs the focused native/Web gate, Viewer hardening, formatter checks, all existing 3D gates, games, libraries, compiler tests, and VSIX checks. |

## Validation commands

```powershell
cmd /c scripts\build.cmd
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\test-renderer3d-reflections.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\test-smile-formatter.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\format-smile-style.ps1 -Check -FormatLongIf
cmd /c scripts\smoke-test.cmd
```

The focused gate compiles and executes native and Web normal paths plus forced
allocation-failure/retry paths, validates generated JavaScript syntax, checks the
shared/native/Web ownership contract, and runs Character Viewer hardening. The
separate asymmetric visual fixture is deliberately not a draw-counter test: it
places a red cube, blue pyramid, and green single-sided triangle across the plane
so incorrect view composition or winding is visibly discriminating on both
production renderers.

## RF01/RF02/RF03 hardening evidence

- `scripts/test-renderer3d-reflections.ps1 -Configuration Release` passed the
  native and generated-Web production fixtures, both forced-allocation retry
  paths, translated automatic plane at Y=7, explicit plane at Y=7, automatic
  negative-world plane at Y=-4, immutable post-submission mutation, below-effective-
  plane fallback, tilted/incompatible receiver rejection, production sample error,
  LDR/HDR/LDR format recreation, Viewer calibration isolation, Viewer hardening,
  and 58 native graphics/pointer/audio-focus checks.
- The asymmetric production fixture rendered at native 960x640 and installed-
  Chrome 1480x987. Both visibly retained red-left/blue-right placement and the
  green single-sided contact triangle across the reflection plane. This exposed
  and then retained the Web/Direct3D winding-parity correction.
- An installed-Chrome float readback from the actual full-resolution
  `RGBA16F` reflection framebuffer measured a maximum linear HDR component of
  `40.2813`, with target-format query `2` and WebGL error `0`. The value survived
  above `1.0` until the intended output boundary.
- The Release Character Viewer rebuilt for native with 46 assets and Web Full
  with 35 assets. Actual Desktop and installed-Chrome Party scenes retained the
  grid, screen-fixed backdrop, current Arin/Orin/Dragon poses and equipment, and
  the sharper reflective floor. The original matte grid floor and the reflective
  floor remained selectable. Chrome reported no warning or error logs.
- Sin Star I rebuilt for native and Web Full with 84 assets on each target; the
  shared Battle Arena Preview remains linked through `Arena3D` rather than a
  copied Viewer renderer.
- `scripts/smoke-test.cmd` passed the final integration gate, including 309
  language/compiler tests, the 423-file style check, all Renderer3D gates,
  libraries, games, native graphics/text/audio-focus tests, and VSIX package
  verification.
- VSIX `2.0.60` was installed in Visual Studio instance `91f001b5` while Visual
  Studio was closed. The installed extension assembly and the compiler,
  compiler-language, native-runtime, and Visual Studio shared-language payloads
  exactly matched their built SHA-256 values: extension
  `3E2C01565E03D5D3CEA70EDE11F2D54709AEC49CBB5DB2ECF7DD4E49655FBB85`,
  compiler `8EB4C632C1C0E3441E3802D638AC5727D4BBF196F5D6A729764AA1B7A79CD826`,
  compiler language
  `28201ACC5485008BDEF2F3B9363C60F70FE14386E934C8AB841D3636DEA83683`,
  native runtime
  `FEC84E5BA3080E77BD93C477A60800A12CC9605F8BDFA136CAACB65B92B1683B`,
  and Visual Studio shared language
  `4087806760DA3E1366111E566F15A3CA52503AE7D08285DBD238E0DF28D149D9`.

## Scope boundary

This milestone provides one reusable polished horizontal battle-floor reflection,
not arbitrary mirrors, recursive reflections, ray tracing, a new material system,
or a second scene graph. Transparent VFX reflection remains intentionally excluded.
Future Battle Scene Editor, syntax, or semantic-inspection work requires its own
approved task package and is not part of this implementation.
