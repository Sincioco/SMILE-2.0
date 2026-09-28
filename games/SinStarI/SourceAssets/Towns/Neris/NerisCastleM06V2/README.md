# Neris Castle M06 V2 — r005

Editable source: [neris-castle-M06-r005.blend](Source/neris-castle-M06-r005.blend).
Portable complete model: [neris-castle.glb](neris-castle.glb).
[Actual Blender contact sheet](Checkpoints/M06-r005/contact-sheet.png) and
[night render](Checkpoints/M06-r005/courtyard-night.png).

Sin authorized autonomous continuation beyond the original M01 checkpoint and
the native town integration. The proposed dimensions remain unapproved; execution
does not imply artistic or dimensional acceptance. M00–M06 were performed in order.
The original image, flag, measured site plan and elevation references remain in
the safely extracted kit in the dedicated D: workspace. Evidence is real Blender
or native Viewer output. No generated images, cloud generation or new dependencies
were used.

The revision adds curved layered window surrounds, decorated doors, fine gold
lines, complete wall arcades, clear banners, connected balconies with access doors
and flowers, detailed courtyard planting, shaded benches/pots, wall-mounted lamps,
architectural night lighting, an elaborate crystal fountain and a flush processional
path around it. The pathway reaches the stairs. Its gate connector is cut around
the path, eliminating the overlapping faces that caused the reported shimmer.
The bridge has a decorated underside and brass borders. Palace leaves and bridge
remain separate moving owners; the courtyard remains in front of the palace.

## Native placement and ownership

The new castle occupies the former Tripo precinct at native X=-3660, Z=2440,
deck Y=23.12, scale 1700%. Blender town root is (-366,244,0.212), scale 1.7.
The moat front and approach road meet the lowered bridge tip at Z=1012, without
overlapping the leaf. The gate hinge remains local Blender (0,-64,-0.25).
The rectangular map has land outside the moat. Earlier files and live save backups
are preserved; the old Tripo castle is removed only from this map and remains a
palette building/source asset. It loads no model geometry/materials while absent.

`NerisCastlePreview` owns 12 static chunks, seven moving bridge parts and six
palace-door parts: 15 models, 98 objects. `NerisCastleRoute` owns placement,
approach/occupancy, stairs, collision footprints and bump-triggered palace doors.
`NerisCastleFountain` owns animated jets, droplets, ripples and non-overlapping
water surfaces. `NerisCastleGlow` owns halos and four broad native night washes;
daytime lighting retains the warmer stone tone. Native scene lighting approximates
the richer Blender light rig within the existing local-light budget.

The portable export retains the complete site and static fountain water. The
native derivative omits the portable moat, buried foundation faces and static
fountain water replaced by runtime effects. The foundation's top remains.
The complete GLB contains 894,499 triangles, 102 mesh parts, 22 materials and one
embedded PNG. The native static pack has 85 parts in 12 chunks. Exact vertex welding
preserves position/normal/UV seams; no decimation or cooker-limit increase is used.

## Validation and limitations

See [checkpoint](Checkpoints/M06-r005/checkpoint.md),
[offline GLB checks](Checkpoints/M06-r005/offline-validation.json),
[measurements](Checkpoints/M06-r005/scene-measurements.json),
[export settings](export-settings.json) and [checksums](package-manifest.json).
All seven original review cameras retain their positions. Each versioned source
and export is preserved. Automation is plain JavaScript with bounded ephemeral
Blender payloads; it does not change the installed setup or create Python files.

No explorable palace interior/cutscene is authored. Native door entry exposes the
existing event boundary for later cutscene integration. Water reflects geometry
visible in the camera's opaque scene; offscreen objects use the environment fallback.
The new castle currently has a dedicated Neris assembly owner, not a general town
palette template. Tripo remains the reusable palette building requested by Sin.
Khronos validation was not run because no installed offline validator was available.
Studio and Web adoption/publication/browser acceptance remain on hold.

Working source workspace: `D:/Projects/Sin-Star-I-Assets/Neris-Castle`.
Current town source: `../NerisTownV1/Blend/Neris-Town-Royal-Castle-r002.blend`.
