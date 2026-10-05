# Native World and Town Authoring

The existing native [Sin Star Studio](../../tools/Character3DViewer/README.md) is
authoritative for maps and town editing. This document summarizes shared contracts;
the [Town Editor guide](../../tools/Character3DViewer/docs/town-editor.md) owns current
controls and the [Viewer architecture](../../tools/Character3DViewer/ARCHITECTURE.md)
owns implementation boundaries and focused tests.

## Current behavior and limits

- `TownTabs.CAPACITY` and `TownWorldDocument.MAX_NODES` are **17**. The application
  starts at Neris Metropolis; do not infer current bounds from the earlier eight- or
  sixteen-tab fixtures.
- World Map connections are a read-only projection of each town's yellow **Map
  Load** destination areas. Repeated tiles do not duplicate directed edges.
  Old graph create/delete-link instructions are historical. The World view has
  independent pan/zoom and photograph cards; it does not add a general RPG runtime.
- Road journeys use incremental, distance-based **A*** over validated connectivity.
  Travel still uses normal collision. Manual movement or a map/document change
  cancels the journey. Minimap road travel and Map Load transitions have separate
  owners.
- Guides support names, list operations and resize handles. Independent `.guide`
  files hold up to 128 markers, fractional grid spacing, filters, snapping and
  visibility. Guides do not enter `.town` or `.blend`. Undo history is session state.
- Day/Night lighting is per map. The current default Night Intensity is **112%**
  (`TownDocument.NIGHT_INTENSITY`); saved custom presets remain authoritative.
  Exact orthographic editing shares projection and picking with the renderer.

Current constants and algorithms are in
[`TownTabs`](../../tools/Character3DViewer/TownTabs.smile),
[`TownWorldDocument`](../../tools/Character3DViewer/TownWorldDocument.smile),
[`TownWorldTravel`](../../tools/Character3DViewer/TownWorldTravel.smile),
[`TownRoadSearch`](../../tools/Character3DViewer/TownRoadSearch.smile),
[`TownGuideFiles`](../../tools/Character3DViewer/TownGuideFiles.smile) and
[`TownDocument`](../../tools/Character3DViewer/TownDocument.smile).

## Save authority and prepared documents

Explicit Viewer/Blender saves and permanent-map UI updates prepare an immutable
clicked revision. Later edits remain live and unsaved. Terrain recipe pages and
road connectivity travel in portable `.town` bundles; GPU handles and an active
journey's search state do not. Legacy documents with missing derived preparation
rebuild incrementally; a new save writes the prepared version.

`Data_BundleStart(Saving, Key, Path, Records)` uses the native data-file status,
progress and message queries. `Records` lists relative checked Save Data companions
separated by pipes. Imports require a fresh staging key. Whole-file verification
precedes companion writes; the root record is the commit point. Exports verify a
durable temporary file before atomically replacing the destination. The SMB1
trailer retains the SMD4 root and checks each companion and the whole bundle.

`TownDocumentStore` selects the payload version required by the document and reads
TWN1 through TWN16; the early TWN1/TWN4 descriptions are not current write-format
promises. The document carries authored transforms, surfaces, lighting and newer
map data. The [codec](../../tools/Character3DViewer/TownDocumentStore.smile) remains
the authority; do not manually downgrade data to match historical examples.

Save For Viewer runs inside the native application with no PowerShell or Blender
dependency. Save For Blender is a separate, explicit conversion. Suggested file
names use local `YYYY-MM-DD HHmm - Town Name`. Persistent progress and errors must
retain edits on failure; only verified completion reaches 100%, then reveals the
exact chosen file. A `.town` write and its photograph have separate failure states.
R04 cold-photograph support is an in-progress, uncommitted working-tree proposal.
Its real-asset native fixture currently fails; it is not a released or accepted
contract. Consult [current Studio status](../../tools/Character3DViewer/docs/status.md).

The live editor is authoritative. A Blender import happens only after explicit
Open; the worker never reloads an old canonical scene over live edits. Blender
exports touch only the chosen path, never save, close or replace the interactive
Blender session. Whole-assembly transforms/deletions round-trip; arbitrary model,
material and direct Blender terrain edits are outside that importer.

## Ownership

| Owner | Responsibility |
| --- | --- |
| `TownDocument`, `TownDocumentStore`, `TownLibrary` | Handle-free authored state, bounded transactional codec, permanent/recovery authority |
| `TownTabs`, `TownHistory`, `TownSelection`, `TownEditor` | Per-document snapshots, bounded undo, selection and cancelable edits |
| `TownGuides`, `TownGuideFiles` | Construction aids and independent guide files |
| `TownFileJobs`, preparation/cache owners, `TownSaveQueue` | Immutable clicked requests, preparation, file progress and Save All sequencing |
| `TownCatalogRenderer`, `TownSurfaceRenderer`, attachments/light owners | Shared templates, incremental terrain, visual resources and authored lighting |
| `TownDocumentNavigation`, `TownRoadSearch`, `TownRoadTravel` | Committed navigation, distance search and one active journey |
| `TownWorldTravel`, `TownWorldDocument`, `TownWorldEditor` | Map Load-derived graph, bounded world data and World UI coordination |
| `TownEditorSession`, `NerisTown` | Active scene lifecycle, delegated input/update/draw and party/camera integration |
| `Watch-TownSaves`, Blender file/codec helpers | Process-bound Blender conversion and verified results |

Keep general mechanics in existing shared Scene3D, Precision3D, SurfaceGrid3D and
Character3D owners. The [Viewer source map](../../tools/Character3DViewer/ARCHITECTURE.md)
owns finer public operations and coordinator exceptions; this table does not
authorize another runtime or duplicate document model.

## Coordinates, surfaces and resource admission

Ten native units equal one metre; catalog Blender XYZ maps to native XZY with
the authored origin offset. Preserve original dimensions, character identities
and calibration. Map bounds, floor and grid share a center; camera reset and
overview obey the active map, while manual orbit preserves the displayed shot.

Use the [surface-layer policy](town-surface-layering.md) for numeric elevations,
deck clearance, navigation heights and overlay rules. Its JSON source is shared
by native asset preparation and Blender. [Terrain elevation](terrain-elevation.md)
owns height editing and its persistence boundary. Never change the user's roads,
draw order or transparency to conceal intersecting solid geometry.

Catalog templates share model resources; placement does not duplicate a model per
house or tree. Current admission must account for the live scene plus temporary
preparation/export resources. Historical mesh/material counts describe their
recorded catalog only. Failed terrain builds keep the prior committed surface;
failed admission must not evict a live scene or silently omit authored content.

## Validation, open work and evidence

Use [build and validation guidance](../../tools/Character3DViewer/docs/build-and-validation.md)
for focused native editor, terrain, guide, route and file fixtures. Blender
round-trip checks use isolated paths and verify the canonical scene is unchanged.
No build, VSIX installation, runtime or browser check was rerun merely by moving
this documentation. Existing dated pass counts are preserved in the archive.

Bulk Move/Copy Sections is implemented for supported documents. Protected airports,
Royal Court duplication and curved/elevated/directed-water transfers remain
unsupported; do not flatten or omit content to transfer them. The editor is an instance/surface
tool, not a mesh modeler. Catalog migration and automatic placement collision
avoidance are not implied. Dense scenes can reach shared resource limits and short
windows can crowd controls. Follow [current limits and issues](../../tools/Character3DViewer/docs/status.md)
for active acceptance work, including R04; do not infer acceptance from old builds.

## Historical milestones and previous anchors

The complete [September 26–October 1 record](archive/town-authoring-2026-09-26-to-10-01.md)
preserves original constraints, defect explanations, test evidence and heading
anchors. Its old Studio direction, catalog counts, capacities, guide behavior,
formats and World-link workflow are explicitly historical.

<a id="october-1-prepared-save-contract"></a>

[October 1 prepared save contract](archive/town-authoring-2026-09-26-to-10-01.md#october-1-prepared-save-contract).

<a id="surface-layering-policy-september-30"></a>

[Surface layering policy (September 30)](archive/town-authoring-2026-09-26-to-10-01.md#surface-layering-policy-september-30).

<a id="current-september-29-update"></a>

[Current September 29 update](archive/town-authoring-2026-09-26-to-10-01.md#current-september-29-update).

<a id="scope-and-implemented-workflow"></a>

[Scope and implemented workflow](archive/town-authoring-2026-09-26-to-10-01.md#scope-and-implemented-workflow).

<a id="ownership-and-state"></a>

[Ownership and state](archive/town-authoring-2026-09-26-to-10-01.md#ownership-and-state).

<a id="catalog-coordinates-and-resource-budget"></a>

[Catalog, coordinates and resource budget](archive/town-authoring-2026-09-26-to-10-01.md#catalog-coordinates-and-resource-budget).

<a id="saves-and-background-work"></a>

[Saves and background work](archive/town-authoring-2026-09-26-to-10-01.md#saves-and-background-work).

<a id="shared-capability-changes-and-defect-fixes"></a>

[Shared capability changes and defect fixes](archive/town-authoring-2026-09-26-to-10-01.md#shared-capability-changes-and-defect-fixes).

<a id="validation-and-practical-limits"></a>

[Validation and practical limits](archive/town-authoring-2026-09-26-to-10-01.md#validation-and-practical-limits).

<a id="september-26-validation-result"></a>

[September 26 validation result](archive/town-authoring-2026-09-26-to-10-01.md#september-26-validation-result).

<a id="native-files-and-tab-ownership-september-28"></a>

[Native files and tab ownership (September 28)](archive/town-authoring-2026-09-26-to-10-01.md#native-files-and-tab-ownership-september-28).
