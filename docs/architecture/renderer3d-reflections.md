# Planar reflections

SMILE provides one bounded horizontal planar-reflection facility shared by native
Direct3D 11 and WebGL 2. It composes the ordinary production view with an explicit
improper reflection transform at the effective floor plane, replays the current
frame's eligible opaque and masked 3D submissions with parity-correct face
culling, and composites the result only on the designated receiver.

The reflected view uses the same immutable per-frame object snapshots as the main
view. It does not advance animation, simulation, events, VFX, or audio a second
time. The screen-fixed bitmap backdrop is captured without camera rotation, while
the receiver, grid, debug helpers, gizmos and unsupported distortion are excluded.
IncludeVfx optionally replays supported transparent meshes,
committed particles and ribbons without a second simulation or resource admission.

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
  core WebGL2 half-float linear filtering, independent of the 32-bit
  `OES_texture_float_linear` extension. The direct LDR path uses `RGBA8` and
  linear filtering. Renderability and framebuffer completeness remain required.
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
  transform gizmos remain excluded. Eligible VFX can be included explicitly;
  Viewer defaults that option On.
- The animation diagnostics begin below the floor toggle and are suppressed at
  compact heights where they would overlap lower controls.
- Reset restores the documented default without changing character calibration.

## Sin Star I Battle Arena Preview

The Battle menu opens a bounded production preview that uses the
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

## VFX and depth ownership

Graphics3D's IncludeVfx option defaults off for generic callers. Viewer explicitly
enables it beside its floor preference. Replay uses committed CPU/GPU particles,
ribbons and eligible additive meshes. It never respawns particles or advances time.
Reflected effects use reflected depth and floor clipping, not main-camera soft depth.
Backdrop submission restores the capture depth attachment even when the main view
uses another resolution/MSAA count. Heat distortion remains excluded.

## Focused checks

`scripts/test-renderer3d-reflections.ps1` exercises actual native/generated-Web
receiver transforms, immutable submissions, raster/culling diagnostics, HDR/LDR
resource transitions, VFX inclusion, zero-work Off and failure cleanup. Viewer
hardening checks typed UI selection and frame order. Actual Chrome/native visual
checks cover fixed background, asymmetric geometry and reflected VFX when changed.
