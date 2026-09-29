# Neris Spaceport r07 handoff

September 29, 2026. Native Viewer rebuilt and launched. No .NET rebuild or browser
refresh is needed. Source r07 and town r002 are new files; prior builds remain.
No commit/push, addon change or SMILE runtime/compiler modification was performed.

## Completed and verified

- Royal Court windows, limestone and gold facade details on front/sides/rear;
  tall ceremonial entrance, glazed central spire, crowned roofs and curved supports.
- Thirteen tower-tip crystals with native additive glows.
- Actual Royal Court double doors: 95° opening, delayed closing, bounded walking
  threshold and one-shot entry event. Native closed/open/reclosed/trigger screenshots
  are in Checkpoints/r07. The isolated review fixture uses production town, route and
  rendering owners. Its O key stages party proximity only for testing. In the normal
  Viewer, O retains its camera-orbit meaning. No cutscene content is included.
- The original coplanar inner-wall/rib defect is corrected at all 84 ribs: 0.5 m
  face separation, 102 m clearance. r01 negative control fails at 0.0 m. Revised
  west/east hangar views show clean stripes at sampled moving-camera positions.
  This is bounded evidence, not a guarantee at every pixel or viewpoint.
- O and Orbit retain position, target, zoom, height and FOV. The main-Viewer button
  test retained target (-5475,1070,-6717), eye height 8159 and distance 13015 after pan.
- Town r002 preserves 361 placements, unchanged embedded document and 16,837
  non-spaceport transforms. Royal Court source hash remains
  fc3db55f3889aa87afe5d1be4f72f8021655b05ddf9954785ffb4dc5d55cd272.

## Validation

Fixed Blender cameras: front, hero, arrival, west/east hangar, top, west/east side,
rear, dock, rear service, underside and hall. Fresh GLB import: 720 × 620 × 404 m,
485,800 triangles, eleven materials and 185 UV-bearing export meshes. Counts:
4 pads, 2 hangars, 13 towers, 9 keels, 4 cabins, 24 hangar panels, 2 entrance leaves.

Native build publishes 323 assets. Route/door, original town camera/scene, expanded
town foundations/rendering/session and cleanup fixtures pass. They retain 24 Arin
pose keys and 98 Royal Court parts. Expanded resources: 495/512 meshes, 461/512
materials, 56/64 models. Nine changed SMILE owners/tests pass style checking.
Viewer hardening reports 58 native graphics/input/audio checks passed; Web skipped.
Logs remain in workspace town-integration and copied r07 checkpoints.

The first updated camera assertion incorrectly expected neutral zoom zero; the
shared neutral is -16. It was corrected. Composed-camera position and full-orbit
radius checks passed before and after that correction. The old r01 no-flicker
claim is superseded by the user's defect report and the targeted r07 checks.

## Limits and future work

The architecture is a simplified interpretation of the references, with opaque
glazing and sparse interiors. Fine distant ornament may alias. No cinematic,
elevator or hangar-controller content or general collision importer is included.
The entry event is ready for a later cutscene. Old collision proxies describe the
original shell. The optional 14-model Tripo composition requires consolidation to
fit the eight free model slots. Phone image visibility remains unconfirmed.
Studio and Web adoption/publication/browser validation remain held.

Current source SHA256: 5fa2be1e5179c176736cc8445de94d59931ffe8a52f6725dabc8100569483595.
Portable SHA256: 47bd896b79c0148f22d78ee9a33f3287d0551ebdc3801e89bc97da688ae77f1f.
