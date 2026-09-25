# Neris Town V1 — Blender Scene and Native Viewer Assets

Created by: Louiery R. Sincioco (Sin). September 25, 2026.

## Open and explore

**[Open the complete Blender town](Blend/Neris-Town-V1.blend)**

**[Open the expanded town](Blend/Neris-Town-Expanded.blend)** — current native
Viewer source, with residential districts, castle, moat, bridge and military HQ.

**[Open the detailed-tree revision](Blend/Neris-Town-Detailed.blend)** — preserved
pre-expansion revision. The original town and its hand-editable scene remain intact.
See the [new trees preview](Previews/Neris-Detailed-Trees.png).

The self-contained Blender 5.2.1 LTS file opens with a material-preview overview.
It embeds the five previously modeled Neris buildings, eight new homes in three
styles, and all streets, landscape and props. No external textures or linked
Blender libraries are needed to open it. The original building files are preserved.

| View | Render | Camera / timeline frame |
| --- | --- | --- |
| Town overview | [1920 × 1080](Previews/Neris-Town-overview.png) | 01 / frame 1 |
| Town plan, north at top | [1920 × 1920](Previews/Neris-Town-plan.png) | 02 / frame 2 |
| Southern arrival gate | [1920 × 1080](Previews/Neris-Town-arrival.png) | 03 / frame 3 |
| East market walk | [1920 × 1080](Previews/Neris-Town-market.png) | 04 / frame 4 |
| Western garden homes | [1920 × 1080](Previews/Neris-Town-homes.png) | 05 / frame 5 |

With the pointer over the 3D viewport, Numpad 0 enters the active camera view.
Set the timeline frame to 1–5 to select the corresponding saved camera. These are
viewpoint markers, not an animated movie. The plan preview is rendered square;
the saved scene's default render format is 16:9. Change output resolution to
1920 × 1920 if manually rendering the plan.

Middle-drag orbits, Shift + middle-drag pans, and the wheel zooms using Blender's
standard controls. Toggle overlays to see/select helpers. The scene was shown in
a separate Blender window placed to the right of Codex; the user's previous
unsaved Blender scene was preserved.

## Original layout and Paseo reference

The reference was Phantasy Star II's starting town, Paseo:

- [Paseo map at FantasyAnime](https://fantasyanime.com/phantasystar2/images/maps/paseo.png)
- [Paseo town description at Phantasy Star Cave](https://www.pscave.com/ps2/towns/paseo.shtml)

The design borrows the readable central landmark, paved service courts connected
by straight walks, three shops grouped on one side, and green spaces between
buildings. Neris uses its own ivory/teal/gold architecture, gardens and crystal
technology. The map image was used for visual study and is not included in this
asset package. This is an original town arrangement inspired by that reference,
not a reconstruction of the Paseo map.

- **Civic spine:** the existing city hall at the north end, the communication
  tower in the center, a crystal fountain and arrival gate to the south.
- **East market:** existing weapon, item and armor stores face west toward the
  neighborhood walk and civic district.
- **Residential district:** eight homes using a courtyard cottage, a two-story
  family house and a domed garden pavilion. Smaller homes also frame the northern
  and southeastern edges.
- **Public space:** two garden canals with six bridges, a crystal arrival circle,
  two small market stalls, benches, lamps, signs, flower beds and private hedges.
- **Landscape:** 27 trees, low boundary walls and an open southern gate. The original
  uses faceted crowns; the detailed revision uses branching trunks and mesh leaves.
- **Scale:** 102 × 90 m foundation; meters, Blender Z up, north +Y.

Names and the arrival-circle purpose are visual design suggestions. No new story
events, teleport mechanics or other gameplay were implemented.

## Editing and source ownership

The editable town and its static native exports live in this package. The native
Character Viewer owns navigation, party presentation and tab integration; Studio
and website adoption remain on hold. The Blender builder reuses the existing
building package's geometry, material and architectural-detail helpers.

The five landmarks are embedded from the validated `Assets/Towns/Neris/ModelsV1`
GLBs. They retain separately editable material meshes under named placement roots.
For the original finer component hierarchy, use [NerisBuildingsV1](../NerisBuildingsV1/README.md).
Source GLB paths and SHA-256 values are recorded in [placements.json](placements.json)
and on the placement objects.

Homes, trees, lamps, benches, flower beds and stalls use embedded collection
instances. Select a placement and use Blender's **Make Instances Real** command
when individual objects are needed; make mesh data single-user before changing
one copy independently. All template collections remain embedded in the file.

| Source file | Responsibility |
| --- | --- |
| `Source/build.py` | Fresh-process guard, palette, orchestration |
| `Source/homes.py` | Three residential templates |
| `Source/props.py` | Reusable vegetation/furniture, fountain and gate |
| `Source/market.py` | Stalls, private gardens, signs and arrival pylons |
| `Source/town_layout.py` | Building placement, streets, canal crossings and landscape layout |
| `Source/presentation.py` | Lighting, five cameras, saved scene and previews |
| `Source/validate.py` | Reload, source integrity, footprint/approach checks and preview regression |

## Rebuild and validation

Preserve hand edits as a new revision before rebuilding. The builder regenerates
the named V1 outputs and refuses an interactive process or an already-loaded file.
It needs only installed Blender and the repository-owned building package.

From this package directory in PowerShell 7:

```powershell
$blenderExe = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
& $blenderExe --background --factory-startup --python-exit-code 1 --python .\Source\build.py
& $blenderExe --background --factory-startup --python-exit-code 1 --python .\Source\validate.py
```

Add `-- --draft` to the build command for a smaller overview-only iteration.

[Validation results and hashes](validation.json) record:

- Successful reopening of the delivered Blender file.
- All five original GLB source hashes match their recorded values.
- Thirteen building footprints have no overlaps.
- All thirteen building approach points connect to the southern entrance on a
  coarse 1 m plan grid accounting for building bounds, canals, bridges and fountain.
- Mesh coordinates are finite, meshes have materials, and the scene has no
  external linked libraries or unpacked file textures.
- Five correct preview sizes and five distinct preview hashes.
- Timeline camera markers select the intended camera for each preview frame.

Visual review includes all five views and the interactive Blender overview.
Two authoring issues were corrected: timeline markers initially repeated the
overview render, and roof ribs intersected the hipped roof surfaces. Preview
camera positions were also moved away from foreground lamps and trees.

## Scope and limitations

This is a detailed, editable **exterior town** with a bounded native Viewer walk
mode. It has no interiors, authored collision meshes, navmesh, NPCs, gameplay
triggers, LOD meshes or baked lighting. Water jets are static geometry. The Viewer
uses building footprints and simple obstacle bounds rather than triangle-mesh
collision. Small decorative props are generally nonblocking.

The original detailed building meshes dominate the scene's geometry. Collection
instances reduce scene duplication; this is not a low-poly optimization pass.
The package includes the `.blend`, PNG previews and chunked static GLBs for the
native Viewer. Rebuilding the Blender source does not automatically update the
Viewer: regenerate the static exports, then build and relaunch the native Viewer.

## Handoff

Open the saved town to edit placement, scale and the three home styles. Select
**Neris Town** in the native Viewer to explore it with the walking party. Further
gameplay or mesh optimization remains a separate bounded task. Existing
Studio/Web holds remain unchanged.

## September 25: native Character Viewer reconstruction

The native Viewer imports `Blend/Neris-Town-Detailed.blend` through `Runtime/`.
The fixed town uses 24 GLBs / 89 mesh parts / 2,142,732 triangles. Two reusable tree
models each contain four parts and 23,024 triangles, placed at the original 27 roots.
The complete town contains 2,764,380 triangles before the ten tiny falling leaves.
Export evaluates 12,596 non-tree instances and removes 190,542 degenerate triangles.
Identical PBR materials and exact position/normal vertices are shared without
decimation. The original `Neris-Town-V1.blend` remains byte-for-byte unchanged.
`Runtime/manifest.json` records current source/export hashes, tree transforms and
individual lamp positions. `export_layout.py` derives runtime placement data.

`Source/detail_trees.py` creates two deterministic tree templates with tapered
trunks, root flares, 158 branch/root sections and 2,970 individual shaped leaves
per tree (80,190 leaves across town). Leaf surfaces have folded geometry and three
green PBR materials; they are not solid crown blobs or textured billboards. Existing
positions, tree scales, building geometry and collision footprints are preserved.
The original 22-chunk export is recoverable from Git history. `detail_flowers.py`
replaces 14 placeholder planters with bowls, open bronze rim inlays, soil and 29
stemmed, leafy, layered flowers each. `detail_materials.py` supplies the richer garden
palette and deterministic stone grain/normal maps in `Textures/`. Paving roughness
is 0.43 with shallow normal strength 0.25. No external texture URLs are required.

Rebuild the static export with installed Blender 5.2:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background `
  'Blend/Neris-Town-V1.blend' --python-exit-code 1 --python 'Source/detail_trees.py'
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background `
  'Blend/Neris-Town-Detailed.blend' --python-exit-code 1 `
  --python 'Source/detail_flowers.py' --python 'Source/detail_materials.py'
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background `
  'Blend/Neris-Town-Detailed.blend' --python-exit-code 1 `
  --python 'Source/export_native.py' --python 'Source/export_layout.py'
```

Run the full recipe from this package directory to reconstruct the derived revision
from the original; for placement-only edits, run just the last command.
`static_glb.py` preserves evaluated normals, embeds the stone maps and supplies
planar UVs with a tangent basis. It avoids
Blender's second-export vertex splitting. A changed chunk count requires updating
`NerisTownAssets.CHUNK_COUNT` and the native project asset inventory.

The Viewer uses existing precise transforms to sway each tree by at most 0.65 degrees
in intermittent six-second breezes. Ten reusable mesh leaves fall at four-second
intervals, settle, then fade for two seconds, one at a time, before recycling. This
adds no per-frame mesh creation or renderer extension. Crystals and the 24 individual
door lamps use separate cyan and amber halos, with restrained crystal sparkles.
`Previews/Neris-Detailed-Garden.png` shows the new planters and stone; the updated
tree preview shows the stronger greens. These are Blender previews, not native captures.

The Viewer provides a small exterior navigation layer, grounded party followers
and the common arena/camera controls; it is not a complete game level. Small
decorative props are generally nonblocking. The fifth requested character, Mila,
still needs a model identity/path. See the Viewer README for the available roster
and controls. Native acceptance: `scripts/test-neris-town.ps1`; shared Viewer
checks: `scripts/test-character-3d-viewer-hardening.ps1 -NativeOnly`.

## Residential and royal expansion — September 25

The expanded revision preserves the original and detailed Blender files. Its
foundation spans 270 × 302 m (X −135…135, north Y −120…182). It includes 37 buildings:
the original thirteen, 22 new residences, the King's Castle and Military Headquarters.

- West: four large homes on spacious lawns, with four shade trees per estate.
- East: six medium homes with smaller courts and one tree per lot.
- South: twelve smaller workers' homes along shared paved courts, with sparse trees.
- North-west: a large tiered castle on an island with four moat bands, a bridge,
  open gate, courtyard fountain, towers, teal roofs and Neris banners.
- North-east: military command hall, barracks, walls, open gate and a 50 × 33 m
  parade lawn with a Neris flag.
- City Hall retains its location and gains an open 34 × 19 m forecourt. The relay
  tower moves to (15, 75) m near military HQ; one older pavilion moves south-west.

The five supplied home/castle/HQ PNG designs and Neris flag in `Assets/Towns/Neris`
guide the stylized exterior models. Templates are separate editable collections.
The grass spacing, tree distribution and architecture distinguish the districts.
All paving is darker slate, with its previous grain and sheen. New street crossings
use the union of one grid, avoiding overlapping coplanar tile surfaces.

| Preview | View |
| --- | --- |
| [Whole town](Previews/Neris-Expanded-Overview.png) | Expanded footprint |
| [Royal district](Previews/Neris-Royal-District.png) | Castle, bridge, moat and HQ |
| [City Hall](Previews/Neris-City-Hall-Approach.png) | Unobstructed civic approach |
| [Estates](Previews/Neris-Rich-Neighborhood.png) | Large homes and gardens |
| [Middle homes](Previews/Neris-Middle-Neighborhood.png) | Moderate plots |
| [Workers' homes](Previews/Neris-Workers-Neighborhood.png) | Compact shared courts |

### Expansion owners and rebuilding

`expansion_architecture.py` owns shared architectural parts;
`residential_expansion.py` owns the three home templates; `royal_district.py`
owns castle/HQ exteriors; `expand_town.py` owns placement and non-overlapping paving.
`expansion-layout.json` records footprint, roads, home lots and landmark locations.
`render_minimap.ps1` uses Windows System.Drawing for the map and alpha marker.

Run installed Blender in background with `Blend/Neris-Town-Detailed.blend` loaded
and `--python-exit-code 1 --python Source/expand_town.py`. Then open the resulting
expanded file in background and run `Source/export_native.py` followed by
`Source/export_layout.py`. Finish with `pwsh -File Source/render_minimap.ps1` and
the native Viewer build. Run these sequentially; the compiler may lock source
files while the placement exporter writes generated data.

The native export contains 28 static chunks / 100 parts / 2,776,669 triangles and
53 unique material groups. Two shared tree templates supply 73 placements (292
part objects); a ten-leaf pool adds one reusable mesh. The manifest records current
source/export hashes. No renderer capacity was increased. Water uses five paired
surface/refraction batches: two canals, two fountains and one closed moat strip.

The Viewer starts with Floor and Grid off. O resumes a cinematic orbit; mouse pan,
orbit and zoom remain shared arena controls. The map fades with actual party
movement, including followers gathering. Navigation accounts for district homes,
castle walls/moat/bridge, HQ walls/parade lawn and the relocated tower.
These are exterior inspection models; interiors and NPC interactions remain outside scope.

### Expansion validation

The native build and `scripts/test-neris-town.ps1` pass with all 28 static chunks,
four party actors and the published minimap images. Focused checks cover district
collision, moat and gate routes, paving heights, water geometry, Floor/Grid
defaults, O input, follower gathering, map projection/fading and resource cleanup.
Viewer hardening and native calibration round trips also pass, including 58 shared
graphics, pointer and audio checks. The ten preceding Arin frame-zero reference
rows and all 24 accepted pose keys are preserved when TownIdle is appended.

Manual native inspection covers startup, the expanded scene, relaxed Arin idle,
pan, zoom in/out and O restoring cinematic orbit. Saved Blender previews show all three neighborhoods, the royal
district and the open City Hall approach. Movement-driven map fading is covered
by the real-asset scene check. Web validation remains on hold.
