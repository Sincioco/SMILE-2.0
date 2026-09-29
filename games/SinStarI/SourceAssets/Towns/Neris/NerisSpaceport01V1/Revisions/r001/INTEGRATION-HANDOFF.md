# Completed southwest integration

Completed locally on September 28, 2026. The native Viewer was rebuilt and left
running Neris Town in its Spaceport orbit. No user rebuild, .NET compilation,
restart, browser refresh, commit or push is needed.

## Delivered

- Isolated M00–M09 source and original checkpoints remain in the asset workspace.
- New editable town: `Town/Neris-Town-Spaceport-SW-r001.blend`.
- Matching saved terrain/lighting: `Town/Neris-Town-Spaceport-SW-r001.town`.
- 720 × 620 × 356 m; 4 pads, 2 hangars, 13 primary spires, 9 keel pylons,
  4 lift cabins and 24 separate hangar door panels in the editable source.
- 361 old placements and 16,833 pre-existing non-terrain objects preserved.
- Roads connect the new arrival causeway to Royal Court and the town; southwest
  is water and the west side is land. Old cell edges and source castle files remain.
- Live save keys were backed up and checked for concurrent changes before installation.

## Validation

The package's original fixed front, west, east, rear and top cameras and hero view
are actual Blender renders in `Checkpoints/M09`. Original source checks: 44 asset
contracts, 30 source clearance, 22 fresh GLB import and 30 collision checks passed.
The assembled town reopens with all 1,991 source objects and 239,912 evaluated
triangles. The portable/native derivative retains 239,864 triangles after the
original export's 48 zero-area artifacts are removed.

Native build publishes 319 project assets. Focused route, town foundations,
editable rendering, expanded session, original scene/camera/calibration and
resource-release fixtures pass. Expanded resources: 485 meshes, 466 materials,
53/64 models, 98 Royal Court parts, 24 accepted Arin pose keys. Ten changed SMILE
files pass the formatter check. No runtime/compiler source changed.

The actual native window completed a 146.9-second observed orbit (one revolution
at 2.5 degrees/second), sampled at front, both sides and rear. Additional close,
north-up, horizontal/vertical adjustment, pan, zoom-in/out and reset views were
checked. No flashing surfaces, large z-fighting patches or disappearing components
were observed in those views. Fine distant trim remains subpixel geometry; this
bounded visual check is not a guarantee for every possible camera or display.
The HUD showed roughly 58–120 FPS in captured views; this was not a benchmark.
`Checkpoints/Native` contains unmodified actual native screenshots.

## Corrected and remaining issues

- Corrected derived tangent generation; original source geometry was unchanged.
- Expanded camera range/far plane and map framing for the larger terrain.
- Corrected stale tests for the already-approved fixed map orbit and made the
  top-down alignment test scale-aware. Initial failed logs are retained separately.
- Lift/door animation and general mesh collision are not implemented; current
  navigation deliberately exposes the safe central arrival-to-hall route only.
- The expanded map leaves 11 model slots. The optional 14-model Tripo castle cannot
  join every current asset simultaneously under the unchanged 64-model limit.
  User impact: avoid adding that optional template to this expanded revision.
  Deferred because the task preserves unrelated castle assets and forbids runtime
  changes. Next action: separately consolidate that optional native asset package
  or introduce reviewed application-level admission; keep all source assets intact.
- Mobile visibility of chat images remains unconfirmed; no public upload was made.

Studio development and Web adoption/publication/browser validation remain on hold.
There are no remaining implementation steps for the requested static spaceport,
town layout, native loading and bounded orbit inspection.
