# M06-r005 / town r002 checkpoint — September 28, 2026

## Completed

- Royal façade/courtyard detailing, glowing crystals/lamps, wall gold and arcades,
  balcony connections, unobstructed windows, balcony doors and flower pots.
- Flush path around the fountain to the stairs. Removed the gate-connector overlap
  that produced the latest pathway shimmer. Separate animated fountain surfaces
  avoid the earlier static/runtime water overlap.
- Versioned source, portable GLBs, exact-welded native chunks, seven unchanged-camera
  renders, night render, measured object bounds and offline GLB validation.
- New castle at the former Tripo site. Road ends at the bridge tip Z=1012; normal
  ground height, surrounding moat and rectangular land remain. Tripo source/palette
  preserved; no Tripo geometry/model/material allocation while unplaced.
- Native automatic bridge, palace bump doors, animated fountain and night washes.
- Explicit Fly Inspect, WASD party mapping, held arrow zoom/orbit, Space orbit pause,
  no Alt action, Shift+middle pan across Viewer tabs, top-down key over the header,
  map-centered reset, cursor-anchored orbit and continuous varied landmark tour.
- Shared native blue water, sun shadows with persisted opacity and localized
  screen-space reflection using an independent reusable scene-color snapshot.

## Validation

- Native Viewer Release build: 316 published assets.
- Town foundations, relocated routes, rendering and full session: PASS.
- Session: 468 meshes, 432 materials, 50/64 models, 98 castle parts and 24 accepted
  Arin keyframes. Load/destroy/re-enter cleanup passes.
- Tripo remove/add/remove resource-count regression: PASS.
- Cursor orbit: no start/release jump; slow/moderate changes retain the selected
  world point under the original pixel. Rendered depth hits the courtyard floor.
- Fly Inspect can reach the ground with 0.1 native-unit clearance. Paused tour
  holds its clock; varied close subjects and continuous framing checks pass.
- Native WARP water shader: opacity 0/50/100, unoccluded consistency, SRV release,
  snapshot reuse and different local reflection colors without distortion: PASS.
- Legacy TWN1/Blender checksum compatibility and TWN2 opacity 0/50/100 round trips:
  PASS. Five live town-save keys backed up before each relocation installation.
- Viewer hardening: 58 native graphics/input/audio checks and architecture wiring
  checks pass; Web checks deliberately skipped.
- Actual Viewer inspected after relaunch: detailed fountain/path, varied close tour,
  map-center reset, decorated bridge, and top-down key while pointer is over header.
  Sky's API cannot perform a held middle-button drag; its motion is covered by the
  native regression rather than claimed as a manual gesture acceptance.
- VSIX 2.0.64 rebuilt/installed; 35 payload hashes verified against the built package.
- No resource limits, guardrail baselines, configuration or protections were changed.

## Limits and remaining review

Dimensions remain proposed. Native night lighting uses four broad shared lights;
it is not pixel-identical to the Blender light rig. Screen-space reflections cannot
show offscreen objects. There are no palace interiors or authored cutscene scenes.
No installed Khronos validator was found; the offline checks do not replace it.
Human artistic/camera acceptance remains open. Studio and all Web adoption remain
on hold. No user .NET rebuild, browser refresh or application restart is required
for the already relaunched native Viewer.

Evidence logs are in `artifacts/tests/castle-*.log`; source measurements/render/GLB
reports are beside this checkpoint. The package manifest contains file hashes.
