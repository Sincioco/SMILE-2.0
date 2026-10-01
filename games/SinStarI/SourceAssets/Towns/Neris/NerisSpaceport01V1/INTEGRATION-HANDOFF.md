# Current r09 pavement revision — October 1, 2026

The current standalone source is `Source/NSP01-final-r09.blend`. The earlier
r08 file and historical town assemblies are preserved. Native and Blender export
both use r09. Sixty grid strips are removed; six gold markings become filtered
paint on the approach deck. Bounds remain 720 by 620 by 404 metres. Portable:
482,028 triangles; native static: 472,220 triangles in six models and 22 parts.
The existing borrowed doors, four alien visitors and both castles are unchanged.

Fresh GLB roundtrip and appended Blender export checks pass. The export check
includes evaluated curves, packed texture presence, world bounds and absent grids.
Native build/session validation is recorded in the current revision evidence.
Moving-camera visual acceptance has not been performed for r09.

Earlier handoffs below describe their own historical validation only.

# Historical r08 removal revision

September 29: removed four ivory/gold flying-strip pairs (eight objects) and eight
hanging gold dock ties, exactly matching the two marked screenshots. Saved fresh
Source/NSP01-final-r08.blend and Town/Neris-Town-Spaceport-SW-r003.blend. All 361
placements and 16,837 non-spaceport objects remain intact. Bounds remain
720 × 620 × 404 m. Portable geometry now has 483,336 triangles; native static
geometry has 473,528. Six models and 21 static parts are retained.

All thirteen fixed cameras were rendered again; fresh import and component checks
pass. Native build and expanded-town session pass; actual updated Viewer views
confirm the marked parts are absent. Source SHA256: 6f313b75abc8fce2e37563c8e3e476e6958e2594fc9d2f74d0609d8f715333fd.
Portable SHA256: c8d8148bbd3ca524bd5714bb6c24c24ab8d942a8bcb55b52ff3049d3ccc74b83.

O/Orbit, door, crystal and hangar fixes below are unchanged. Their original native
screenshots remain labeled r07. No .NET rebuild, browser refresh, commit or push is
needed; the rebuilt native Viewer is launched. Studio and Web remain held.

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
