# Neris Metropolis V1

October 4, 2026 native Studio delivery. The 6,400 × 6,400 metre map is a separate
town, with 630 placed assemblies in the latest saved user layout. Existing Neris
Town and other permanent towns keep their identities and saved data.

## Files and authority

- `Town/Neris Metropolis.town`: verified native prepared bundle; includes the
  latest seven user placement changes captured on October 4. Its matching PNG
  comes from the native editor's frozen photograph/save workflow.
- `Blender/Neris-Metropolis-Studio.blend`: export of that exact Studio document,
  including 77,553 prepared terrain patches, movable assembly roots and lighting.
- `Blender/Neris-Metropolis.blend`: approved authoring scene and reusable template
  library. The named V1–V8 files preserve design iterations, not current map placement.
- `Authoring/catalog-extension.json` and `Templates/`: append-only runtime catalog
  extension. Original catalog IDs 0–39 and its document fingerprint are preserved.
- `Source/`: reproducible geometry, façades, layout, catalog and thumbnail generation.
  `publish_town.py` produces a factory layout; never run it over an edited live town.
- `Previews/`: design studies, catalog thumbnails and actual native review frames.
- `manifest.json`: file checksums and native/Blender validation evidence.

The user's Studio save is authoritative for subsequent placement edits. Export it
before regenerating this checkpoint; do not replace it with `city_layout.py` output.

## Catalog

47 new reusable templates extend the existing 40 to 87: 17 Earth-inspired tower
complexes, four original Neris/Luma landmarks, two conservatories and the Supertree
Grove, four bridges, sixteen varied neighborhood groups, two detailed tree groves,
and the Neris Sphere. Jin Mao remains reusable in the catalog; the user-highlighted
duplicate-looking placement was removed from this map.

The original landmarks are Luma Crown Spire, Neris Tide Arcology, Starweave Civic
Hall and Aether Gate Observatory. Glass landmarks use six filtered blue façade
textures. Mid-rises use varied masonry, rooflines, terraces and window treatments.
Marina Bay Sands uses glass on all three tower façades and detailed Studio trees
on the rooftop. Merdeka and One World Trade Center have distinct angular geometry.

## Native presentation

- Night lighting, independent seeded 1–3 second antenna flashes, and a continuous
  additive light column above the crystal spire.
- Localized fireworks on Marina Bay Sands, Petronas, their nearby water, and the
  lake in front of Burj Khalifa. Burj's source resolves independently from its
  current placement to an inset water point; no global random launch positions.
- Darkened nighttime water reflects buildings, lights and submitted fireworks
  using the shared native planar reflection pass.
- A per-pixel emissive Sphere display blends between aurora, plasma, ripple and
  spiral patterns every twelve seconds, with seeded colors and continuous motion.
- One or two actual Neris Spaceport spacecraft cross the city every 5–15 seconds.

The Sphere, fireworks, aviation lights and spacecraft are Studio runtime effects.
The Blender export preserves static geometry, materials, placements and lighting;
it does not bake those runtime animations into Blender keyframes. The spire beam
is a bounded additive visual effect, not volumetric participating-medium lighting.
Planar water reflection supports one shared horizontal height, not arbitrary
waterfalls or multiple independently elevated reflection planes. Web adoption is
not part of this native delivery.

## Validation

`scripts/test-metropolis-native.ps1 -PublicationDirectory <built Studio folder>`
uses an isolated application ID and a read-only copy of the current fixture.
It verifies real catalog material loading, bridge/water depth separation, drag
and cancellation, all 47 template placements, beacon intervals, spacecraft timing,
four localized launch sites, actual rendered photographs, Sphere pattern frames,
native prepared town/PNG save and reopen, and transition from nighttime city to
the real Neris Town including castle resources.

Latest-layout native evidence: `artifacts/tests/metropolis-0cc136aaedb04f80963e7218806741c7`.
Cold preparation took approximately 30 seconds; the isolated fixture allows 60
seconds. Production save preparation remains cooperative with visible progress.
The earlier 20-second fixture deadline was too short; it did not indicate lost data.

Blender export was reopened and imported through the production converter. All
630 identities/templates survived; maximum transform discrepancy was 0.001211
native units. This exposed and fixed Blender's appended object-name suffixes being
mistaken for incomplete assemblies. Portable membership now uses template identity.

The native reflection regression, focused town editor/route checks and repository
format checks are recorded in the Viewer architecture handoff. No user town,
character calibration, runtime pool or architecture threshold was replaced/raised.

## Build and launch

Use `tools/Character3DViewer/Build.ps1 -Target Native`, followed by its `Launch.ps1`.
The existing native Studio owns this map; no separate application was introduced.
Blender MCP's `use_autostart` setting was verified from a fresh background Blender
process using persisted preferences. It is machine configuration, not a repo asset.
