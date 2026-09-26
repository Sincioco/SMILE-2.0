# Native World and Town Authoring

## Scope and current state

Sin's September 26 World and Town Editor request applies to the existing native
Character Viewer. It does not resume SMILE Studio or Web adoption. This document
records the implementation boundary; the editing workflow is not yet implemented.

The existing Neris scene is exported as material-batched static meshes. Individual
buildings cannot be moved or deleted inside those batches. Authoring therefore
requires an instance catalog and a town document, rather than transforms applied
to arbitrary runtime mesh chunks. The saved Blender source remains the asset source.

## Requested workflow

- An Edit button opens a right-side palette of existing town items.
- Buildings, Roads/Ground and Decorations are mutually exclusive selection modes.
- Place, select, rubber-band select, move groups on the ground plane, rotate
  buildings and delete selected items. Blender X/Y correspond to native X/Z;
  authoring does not expose vertical placement in this version.
- Ground, paving/roads and water support line drawing with two clicks and rectangle
  drawing. Preview before applying. Rectangle selection can erase surface tiles.
  Expanding water regenerates its exterior bank outline without internal borders.
- Save for Viewer only, save back to Blender, or save a named new version for both.
  New-version naming is an explicit prompt. Keep the original on cancellation/error.
- New town/blank environment and a world map connecting multiple town documents
  are both required. They are distinct commands, not alternative interpretations.
- Edit the global sun's RGB, intensity, azimuth and elevation. Existing renderer
  shadow support follows that light for buildings and trees.
- WASD/arrows inspect by default with the existing wheel/pan/orbit controls. Tab
  explicitly enables party control. Text entry must consume movement shortcuts.

## Ownership and dependencies

Use caller-owned document state and the existing shared renderer, precise camera,
mesh/material operations and native persistence. No new compiler, window host,
asset manager, character calibration format or rendering backend is required.

| Owner | Boundary |
| --- | --- |
| Catalog export | Stable template/instance identities; local geometry and attached entrance, collision and effect metadata from saved Blender |
| Town document | Surface cells, item identities/transforms, lighting and selected document/version; no GPU handles |
| Surface editing | Line/rectangle rasterization and connected bank edges; no mouse input or files |
| Town renderer | Catalog handles, instance draws and changed surface geometry; incremental loading and disposal |
| Editor interaction | Active category/tool, selection, ground picking, drag preview and committed document edits |
| Editor panel | Palette, tool/save/new-document/name/light controls; delegates mutations |
| Persistence | Validated versioned document encoding and atomic native Save Data; no calibration dependency |
| Blender bridge | A launcher-owned hidden worker consumes explicit save requests and builds a saved Blender version in the background |
| World map | Town document references, node positions and links; uses existing RPG World scene/spawn/transition identities |
| Native host / NerisTown | Lifecycle and delegated input/update/draw only |

Keep these responsibilities in the smallest coherent set of modules. Do not put
editor algorithms or persistence into the host. A module's 600-line threshold is
a review trigger; no baseline or exception may be raised to fit implementation.

## Data and rendering constraints

Catalog geometry is exported once in template-local coordinates. Instance data
contains the stable source identity, template, category, horizontal transform,
preserved authored vertical placement and scale. Moving an assembly moves its
doors, stairs, collision and effects too. Existing meshes are not duplicated for
every tree or house. The native submission snapshot already supports reused objects.

Measure the catalog before replacing the static publication: the runtime has
64 model, 256 mesh, 512 material, 1,024 object and 2,048 draw-submission capacities.
The current scene consumes 155 meshes, 144 materials and all 16 ribbon batches.
Do not silently raise those limits or keep both complete scene representations live.

Surface cells use the established two-metre paving grid. Road overlays and land/
water are distinct so bridges remain representable. Merge surface geometry into
bounded batches. Regenerate bank edges from adjacent cells; suppress edges inside
a single body of water. Remove grass geometry from painted road/water cells.

## Save and interaction contracts

Saving to the Viewer uses its application-owned atomic Save Data storage. Blender
save requests carry structured data, never executable commands. The bridge works
on saved files and must not save or discard an interactive Blender session.
Write a temporary result and validate it before replacing the selected saved file.
Version names are data; sanitize file components and enforce the town output root.
Report actual progress and errors while the native window continues rendering.

Preserve unsaved edits across view changes. A dirty document cannot be silently
replaced by opening another town or creating a blank environment. Cancellation
must preserve both the current document and existing files.

## Validation

Use focused native checks for document round trips, two-click line and rectangle
results, water boundary joins, category selection and group transform ownership.
Check a real saved Blender round trip and a new named version. Check loading/draw/
cleanup against the actual catalog and existing actor calibrations. Briefly inspect
the panel and one edit workflow. Web validation stays held.
