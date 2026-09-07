# Reflective Battle Floor Continuation Checkpoint

This is the compact tracked checkpoint for the reflective battle-floor milestone
authorized by Sin on September 8, 2026. It records the recovered instruction
package, architecture, public contract, integration defaults, and acceptance
evidence without reopening completed Character Viewer refactor work.

## Instruction identity and preserved baseline

- Exact archive:
  `C:\Users\louie\Downloads\2026-09-07-2320-smile-2.0-reflective-battle-floor.zip`
- Archive SHA-256:
  `5EB39768B493943CFC197080A43BA69FBD47D8ECEA78A9603398E08E5E2641A5`
- The package's Markdown instructions and included supporting documents were read
  before implementation. The reference image was used only as visual direction
  and was not added to the repository.
- The completed continuation baseline and milestones documented in
  `docs/implementation/post-refactor-continuation.md` remain preserved.
- H01/H02/H03 remain closed. The Doctor repair path and retired H01 test were not
  rerun or recreated.
- `tools/Character3DViewer/Program.smile` remains the accepted, unchanged
  1,851-line application coordinator. This milestone extends its focused owners
  and shared runtime rather than restarting the refactor.

## Result

SMILE now provides one bounded horizontal planar-reflection facility shared by
native Direct3D 11 and WebGL 2. It captures the current frame's eligible opaque
and masked 3D submissions from a camera mirrored across the floor plane, reverses
face culling for that mirrored view, and composites a softly filtered reflection
only on the designated receiver.

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

`Graphics3D.ReflectionValue3D` exposes the last completed frame's requested and
effective states, fallback reason, target dimensions and byte ownership, reflected
draw/triangle/capture/composition counts, configuration revision, resource
generation, and configured quality values. These diagnostics make the zero-work
Off path and fallback behavior directly testable on both targets.

## Quality, bounds, and failure behavior

- The reflection target preserves HDR color when the owning renderer uses HDR and
  supports the existing direct LDR path.
- The target follows viewport and quality scale but is capped at 2,048 pixels on
  its longest side. Strength and five-tap softness are independently controlled.
- Turning reflections Off performs zero reflection captures, reflected draws, or
  receiver compositions for that frame.
- Missing receiver, below-plane camera, allocation failure, and render failure are
  explicit fail-soft outcomes. The main scene continues normally and the UI can
  report `Floor Reflections: Unavailable`.
- A failed allocation is cached for the current configuration and viewport instead
  of being retried every frame. A new configuration boundary, including Off then
  On, permits a bounded retry.
- Resize, renderer reset, device loss, and Web context lifecycle paths release or
  invalidate reflection-owned resources with the parent renderer. Reset reports
  zero target bytes.

## Character Viewer behavior

- Individual and Party views default to `Floor Reflections: On`.
- The visible control and `R` key switch between On and Off; Party pointer routing
  consumes the click so it cannot leak into camera or other controls.
- `Unavailable` is reserved for a requested reflection whose optional target or
  pass failed. The floor remains usable and matte.
- Backdrop, grid, HUD, inspector, socket helpers, transform gizmos, and VFX retain
  their established ownership and are not reflected.
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

## Acceptance matrix

| ID | Requirement | Evidence |
| --- | --- | --- |
| R01 | Horizontal plane math and receiver height | Focused gate proves nonzero-plane mirror involution, fixed points, signed-distance reversal, and distance symmetry; native/Web fixtures derive the plane from the receiver. |
| R02 | Fixed bitmap backdrop | Native/Web reflection owners capture the backdrop as a screen-space pass. Chrome Viewer and Sin Star I orbit checks preserved the fixed asymmetric backdrop while the 3D view moved. |
| R03 | Current live pose and equipment without double advancement | Reflection replay consumes immutable frame submissions. Viewer hardening plus live Chrome animation showed current actor, sword, and shield; no animation/event/audio update exists in the reflection pass. |
| R04 | Eligibility and exclusions | Native/Web focused checks cover eligible, receiver, excluded, opaque/masked, non-object, VFX/debug, and below-floor/frustum policy. Grid and helpers are explicitly excluded by shared/Viewer owners. |
| R05 | Individual and Party UI ownership | Viewer hardening covers On/Off/Unavailable labels, individual and Party hit regions, hidden-state rejection, consumed Party actions, reset default, and both toggle paths. Chrome Party toggling removed and restored the reflection. |
| R06 | On/Off/recreate lifecycle and zero work | Native/Web fixtures prove On-Off-On, receiver absence, camera below plane, stable generation, reset cleanup, and zero captures/draws/compositions while Off. |
| R07 | HDR and LDR composition | Native/Web owners select reflection color format from renderer HDR state; the focused fixture covers direct LDR and the permanent post-processing gate covers the HDR pipeline. |
| R08 | Immutable ownership | Fixture mutation after submission cannot change reflection eligibility; mid-frame role/configuration changes are rejected; teardown releases target ownership. |
| R09 | Fail-soft allocation and retry | Native and Web forced-failure-once runs prove main-frame completion, cached failure without per-frame retry, and successful retry after an explicit configuration boundary. |
| R10 | Resize, reset, below-plane, and context recovery | Target compatibility includes viewport size; native reset/device-loss and Web context hooks invalidate owned resources. Focused tests cover reset bytes, recreation, and below-plane fallback. |
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
shared/native/Web ownership contract, and runs Character Viewer hardening.

## Scope boundary

This milestone provides one reusable polished horizontal battle-floor reflection,
not arbitrary mirrors, recursive reflections, ray tracing, a new material system,
or a second scene graph. Transparent VFX reflection remains intentionally excluded.
Future Battle Scene Editor, syntax, or semantic-inspection work requires its own
approved task package and is not part of this implementation.
