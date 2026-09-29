# Neris Horizon — Gentle Wave r008

Horizon remains at full size in western Neris Town. The terminal and both hangars
retain their teal wave roofs, original skylights and Neris facade ornaments.
The two hangar rear roof edges now align with the terminal at local Y=150 m.
Both VTOL noses face outward toward the front apron; building orientations are unchanged.
Hangar side and rear walls use terminal-style vertical glass at 80% opacity.

The control tower moves 40 m closer, to (0,190), with four ivory lancet surrounds,
gold tracery, Neris compass emblems and cyan crystal reliefs. Its antenna reaches
285.30 m. All three antenna tips flash red in an irregular sequence, one at a time.
Both rear glass halls and their
planters are removed. Two 900 x 50 m strips at Y=355 and Y=270 are connected by
18 m wide U-turn roads with a 42.5 m centerline radius. The platform remains
960 x 600 m. No light poles were added.

The Spaceport 01 deck material is reused exactly, with 12 m paving tiles, four
gold approach inlays, three compass medallions, apron borders and 56 flush cyan
floor fixtures. Both strips have 71 guidance lights each. The ordinary terminal
bays use 43% opacity; glass behind the gold ornaments stays opaque. The Neris-style
teal/gold Horizon Airport sign follows the wave roof above the entrance, with two
compass banners. The former ground sign and white platform border are removed.

## Current files

- `Source/Neris-Horizon-Gentle-Wave-r008.blend`: authored airport, including aircraft previews.
- `Town/r008/Neris-Town-Horizon-r008.blend`: full review town; all 362 placements
  and 16,842 unrelated objects are preserved.
- `Town/r008/Neris-Town-Horizon-r008.town`: unchanged layout snapshot. Live saves
  remain authoritative and were not replaced.
- `Native` and `Revisions/r008/Native`: matching, checksummed native exports.
- `Previews/r008`: actual Blender and native Viewer evidence.
- `Revisions/r008/validation.json`: measured geometry and model budgets.

Earlier revisions, original Spaceport 01 and all castle packages remain intact.
Save For Blender appends the r008 airport collection. The authored ship meshes
keep the native -Z nose axis; the same outward orientation appears in Blender and
the Viewer. **Visit Horizon Airport** brings the party to the front entrance and
looks toward it from behind Arin. Right-click in Free Camera restores a bird's-eye
town view and slow orbit. O/Orbit retains the current view; Follow Party remains
leader-centered.

## Traffic and lighting

Nine regional aircraft instances and one larger Neris cargo aircraft share four
model resources with the airport and its two VTOL spacecraft. Smoothed hulls and
engines use denser sections, teal panels, gold stripes and Neris emblems. The cargo
ship measures approximately 125 m long and 112 m wide; the regional aircraft use
55% of the transport model. The cargo is 130% of that full-size model.

`NerisRunwayTraffic` schedules a new arrival every fifteen seconds on the outer strip.
Each aircraft rolls out, takes the U-turn road, then leaves along the inner strip.
The cargo occupies one of the ten staggered slots, returning every 150 seconds.
An aircraft is hidden once clear of the scene, then returns on approach.
`NerisHorizonPreview` owns the four models and 86 drawing objects; motion remains
in the traffic owner. `NerisHorizonDownwash` owns 48 low-opacity dust particles
that spread near the ground during VTOL ascent/descent and disappear aloft.

`NerisHorizonGlow` owns 201 halo particles: 142 runway fixtures, 56 floor fixtures
and three beacons. The existing thirteen concealed night-light sources are retained;
the removed rear-hall washes now illuminate the forecourt. The minimap obtains
its selected-airport label and actual terminal position from NerisTownLandmarks.
Both Horizon and original Spaceport 01 are represented in their respective towns.

## Validation

Geometry checks confirm the aligned roof backs, two strips, four tower crystal
panels, retained 196 terminal skylight panels and 32 hangar skylight panels,
56 floor lenses, 142 runway fixtures and the clear passenger doorway. The deck
color matches the source Spaceport 01 material. GLB preparation verifies the
actual cockpit geometry's forward axis, checksums and unchanged native limits.
Horizon has 123,301 vertices against the 131,072 limit; four models contain
32 material parts and 78,290 triangles. No compiler/runtime or pool limits changed.

Earlier native editing, routes, rendering and full-session checks passed, covering airport
entry by all four party members, flight poses, fifteen-second staggering, downwash
activation, minimap selection, linked-town travel and resource release. The town
uses 54/64 models (latest resource counts are in the session log),, retaining all 24 Arin calibration
keys. The latest sign/glass/filter session rerun and live visual acceptance remain
pending at this pre-map-move checkpoint. The native hardening gate and its 58 graphics/input/audio checks pass.

Runway paint is baked into one packed 4096 x 640 surface texture, using native
anisotropic filtering and mipmaps. Its former thin overlay meshes are removed,
so the lines filter smoothly with distance without competing depth surfaces.
U-turn road polygons are clipped to the gap between strips; fine roof seams are
wider and lifted from the roof. Final live orbit inspection is recorded
in the Viewer architecture handoff. Studio, Web and other paused camera work
remain on hold.
