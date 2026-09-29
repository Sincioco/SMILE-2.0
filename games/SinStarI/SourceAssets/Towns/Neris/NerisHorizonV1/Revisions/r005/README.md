# Neris Horizon — Gentle Wave r005

The approved civilian terminal is built in Blender and integrated into the western
side of Neris Town at full scale. The ivory wave roof covers a glazed two-level
terminal; the taller communications tower is centered behind it. Existing hangars,
glass halls and aircraft occupy the sides. The front promenade is clear. There is
no rear runway and no exterior light pole.

| Measurement | Metres |
| --- | --- |
| Site | 960 × 600 |
| Terminal | 520 × 150 × 79 |
| Communications tower | 285 high, 44 shaft width, 70 cab width |

Source: `Source/Neris-Horizon-Gentle-Wave-r005.blend`.
Actual fixed-camera Blender images: `Previews/r005` (Hero, Front, Terminal, Top,
Left, Right, Rear). These are rendered geometry, not concept images.
The native export has 3 models, 22 material parts and 47,686 triangles. The
canonical `Native` exports and checksums match `Revisions/r005`. Older revisions
remain available. Glass uses single transparent planes with modeled interiors.

## Town layout and travel

`Town/r005/Neris-Town-Horizon-r005.town` retains all 362 live placements and the
user's saved Sun settings. The southern extension is removed. Western and northern
land extends the map to X [-11600, 2750], Z [-3550, 6250] in native units. The
496 × 461 nonuniform grid stays within the existing 512 × 512 limit.
Horizon's origin is (-7400, 23.12, 1250), yaw -90°, ten native units/metre.
Its frontage faces east toward the town. Roads meet the forecourt edge and connect
to Royal Court and the town circuit, without overlapping the airport deck.

The original Spaceport 01 r08 lives in the separate **Neris Spaceport** town/tab:
`Town/r005/Neris-Spaceport-r001.town`. Walk south along the western road at X=-5000
to travel there; walk north along its entry causeway at X=-5500 to return. The
whole party transfers. Arrival points sit outside their return trigger to prevent
immediate travel back. These are authored walking gates, not a general world-map
travel system.

Blender review town: `Town/r005/Neris-Town-Horizon-r005.blend`.
Round-trip editor exports: `Town/r005/Neris-Town-Horizon-Editable-r005.blend` and
`Town/r005/Neris-Spaceport-Editable-r001.blend`. `town_blender_landmarks.py` retains
the same Royal Court/Horizon/original-spaceport assemblies during future exports.
Do not use the earlier two-spaceport comparison town as the active save.

Backups of both physical and redirected app-data profiles, plus the pre-migration
362-item document, are in `Town/r005`. The migration changed no original castle
or unrelated source asset. No commit or push was made.

## Native owners and validation

`NerisHorizonPreview` owns incremental models and aircraft lifetimes; route owns
placement and walking clearance; traffic owns deterministic flight clocks.
`NerisHorizonLighting` supplies concealed terminal, façade, hall, hangar and tower
lights through the existing town light owner. `NerisTownLandmarks` switches only
the selected town's resources. `NerisTownConnections` owns the two walking gates.

Native route, real rendering, full party round-trip, resource-release and save
checks passed. The Neris Town scene uses 53/64 models, 496 meshes and 464 materials;
all 24 accepted Arin keys remain loaded. The 58-check native hardening gate passed.
Both editor Blender exports were reopened and imported with matching item counts,
terrain cells and grid edges. Native daylight orbit inspection showed the wave
roof, transparent terminal and unobstructed frontage. No flicker was observed in
that brief inspection; this is not a guarantee for every camera position.
The final night preview verifies thirteen concealed lighting sources across the
terminal façade/interior, side halls/hangars and tower cab; original Day settings
were restored with Undo after the check. Native launch started its worker directly.

Camera follow-up work, Studio development and all Web adoption/publication/browser
validation remain on hold. The native Viewer has been rebuilt; no browser refresh
is involved.
