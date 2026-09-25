# Neris Town V1 — Blender Exterior Scene

Created by: Louiery R. Sincioco (Sin). September 25, 2026.

## Open and explore

**[Open the complete Blender town](Blend/Neris-Town-V1.blend)**

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

## Layout and Paseo reference

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
- **Landscape:** 27 faceted trees, low boundary walls and an open southern gate.
- **Scale:** 102 × 90 m foundation; meters, Blender Z up, north +Y.

Names and the arrival-circle purpose are visual design suggestions. No new story
events, teleport mechanics or other gameplay were implemented.

## Editing and source ownership

Town changes live entirely in this package. No compiler, runtime, Studio, Character
Viewer or website code changed. The build reuses the existing building package's
geometry, material and architectural-detail helpers instead of duplicating them.

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

This is a detailed, editable **exterior town concept**, not an integrated playable
level. It has no interiors, collision meshes, navmesh, NPCs, gameplay triggers,
LOD meshes or baked lighting. Water jets are static geometry. Approach checks
establish plan connectivity, not character collision or accessibility acceptance;
small curbs, stairs and furniture still need runtime collision treatment.

The original detailed building meshes dominate the scene's geometry. Collection
instances reduce scene duplication; this is not a low-poly optimization pass.
The town is delivered as `.blend` with PNG previews, not as an additional whole-town
GLB or a video. No .NET rebuild, application restart or browser refresh is required.

## Handoff

Open the saved town to review placement, scale and the three home styles. Any
future gameplay integration or mesh optimization should be a separate bounded
task with an agreed runtime budget. Existing Studio/Web holds remain unchanged.
