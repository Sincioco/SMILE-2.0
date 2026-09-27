# Neris Castle — M01 r003 provisional comparison

Sin Star I owns this package. It is a measured Blender blockout awaiting Sin's approval, not a finished castle or an approved M02–M06 delivery.

Authoring workspace: `D:\Projects\Sin-Star-I-Assets\Neris-Castle`. The source kit was safely extracted from the newest matching Downloads ZIP; `extraction-audit.json` records its checksum and path validation. M00 connection, isolated save/reopen/render and cleanup passed using the installed Blender 5.2.1 LTS and existing Blender Lab MCP. No software, connector, configuration, protection or persistent Python changes were made.

## Source and evidence

- `neris-castle-M01-r003.blend`: one physical model, category materials, independent bridge hinge, fixed review cameras and packed references.
- `M01-r003/contact-sheet.png`: seven actual Blender renders.
- `M01-r003/checkpoint.json`, `M01-r003/measured-checks.json`, `M01-r003/review.md`: approval state, 62 checks and proposed departures.
- `M01-r002/bridge-sweep.json` and its bridge pose renders: 91 geometric motion checks retained; r003 only removed the external landing.
- `native-validation.json` and `NativePreview/`: native checks and actual Viewer screenshots.
- `castle.glb`, `drawbridge.glb`: provisional preview exports. The static castle has 7,490 triangles; the separate hinge-local leaf has 12. Both were reimported in Blender and their bounds checked.
- `export-validation.json`: actual reimport results. The full authoring workspace retains r001 and M00 evidence.

Proposed dimensions: tower-inclusive footprint 108 × 132 m, 60 m peak, 12 m gateway, 10 × 20 m bridge. The route is gate → southern courtyard → northern palace. Roofs, arch, crystals and fountain remain massing placeholders; windows, heraldry, chains and interiors are not finished.

The user separately requested a working native bridge before blockout approval. A 0.4 m arch setback and hinge-seat extension remove sweep intersections. Five directly affected objects changed; 242 others were verified unchanged. The r001 checkpoint remains intact in the authoring workspace.

## Native comparison contract

The native Neris Town scene displays the castle as a temporary comparison assembly in the west water area, keeping both Royal Castle and Old Castle. It appears in Neris Town and its named copies. It does not alter the catalog fingerprint, saved assemblies or Blender town exports; a new versioned .town carries only the authorized terrain edits. The comparison is not a new editable catalog tile yet.

Placement converts Blender X/Y/Z to native X/Z/Y with 17 world units per metre, origin X −3650, Z −852, deck Y 23.12. The peak is 1,020 units above the deck. The moat uses existing animated town water at Y 21.85; frozen r003 static mesh part 2 is therefore omitted from native drawing. The external stone landing block was removed at Sin’s request in r003. The bridge meets the normal road level. The portable town file Neris-Town-Castle-Approach-r002.town extends land to the rectangular western map edges, retaining a water moat around the castle, using actual native terrain cells: grass Y 20.9 and road Y 23.12. A main road runs from the existing western road to the front of the drawbridge. The preview adds no terrain mesh, so existing roads remain visible without overlapping grass. All original road cells, 362 assemblies and lighting remain unchanged.

`NerisCastlePreview` owns resource loading, display and cleanup. `NerisCastleRoute` owns the placement, bounded ground/collision route and occupancy snapshot. `ProximityDrawbridge` owns reusable hinge motion. Party movement and the visible leaf use the same bridge state. Any of the four party members on the approach or within the castle keeps the leaf down. It lowers over three seconds, waits 2.5 seconds after everyone leaves, then rises over three seconds. Crossing is allowed only when fully lowered. The fountain, surrounding moat, walls and unfinished palace volume remain impassable; stairs reach the entrance recess.

Click **Visit West Castle** outside Edit Town to place the party on the approach. WASD/arrows walk as usual. The button is a preview inspection convenience; right-click restores the town overview.

`prepare-native.mjs` validates the frozen part layout and creates project-local cooking mirrors using installed Node and built-in modules only. The disposable Blender export staging has actual UV islands because the static cooker requires nondegenerate UVs, even for untextured materials. The preparation script validates and copies these exports without changing geometry. Native tests exercise approach, moving-leaf collision, crossing, last-follower occupancy, delayed closure, bridge/stair traversal, loaded model parts and the actual leaf transform.

Future revisions must preserve prior source checkpoints and update only changed owners and their real dependencies. Native placement is a preview decision; source dimensions and cameras still await approval.

The façade is explicitly deferred: palace doors/windows/trim belong to M03 and surface treatment to M05. Sin confirmed he can wait for those later plans. No façade preview or later milestone approval is implied.

The prior current-town records were backed up under the authoring workspace native-integration/backups. The terrain-only r002 revision was applied to the current Neris Town while the Viewer was closed. No user assembly, lighting value or original road cell was replaced. The normal terrain renderer owns the new land and road; the castle preview contains only four display objects and no overlapping ground mesh.
