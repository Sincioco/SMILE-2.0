# Neris Castle M01 r003 — awaiting review

M00 and M01 are complete. All source dimensions remain proposals. The separate native preview and automatic drawbridge were explicitly authorized before blockout approval. No M02–M06 approval is inferred.

One Blender scene supplies all seven fixed views. The route remains gate → southern courtyard → northern palace. The tower-inclusive footprint is 108 × 132 m, peak 60 m, gateway 12 m, drawbridge 10 × 20 m. These are proposed dimensions, not recovered measurements from the concept.

## Latest user revisions

- Removed all four external stone landing objects in r003; the remaining 243 scene objects retain the r002 geometry and transforms.
- Native Viewer supplies continuous normal textured ground in front of the bridge and connects it to the town. This is context terrain, not castle source geometry.
- Palace doors, windows and trim remain planned for M03; materials for M05. Sin said he can wait when informed these belong to later milestones.
- Sin manually tested Arin approaching and leaving: the bridge lowered and raised as requested. This confirms preview behavior, not approval of dimensions.

## Evidence

- `contact-sheet.png`: seven actual Blender renders, visually inspected.
- `measured-checks.json`: 62 passing checks.
- `scene-measurements.json`: measured scene records.
- `checkpoint.json`: hashes, changes, deviations and approval state.
- `../M01-r002/bridge-sweep.json`: 91 actual mesh poses, no surface intersections. The retained r003 geometry is unchanged by removal of the landing.
- The export record verifies Blender reimport bounds: static 7,490 triangles plus 12 moving-leaf triangles. Actual UV islands were authored in disposable export staging for the installed native cooker.
- The repository package includes `native-validation.json` and native screenshots.

Source: `D:/Projects/Sin-Star-I-Assets/Neris-Castle/source/neris-castle-M01-r003.blend`. The repository package includes an identical source copy. Prior sources and evidence are preserved.

## Approval still outstanding

Review the footprint, height, gate/bridge proportions, courtyard-before-palace arrangement and fixed framing. Continue numbered milestones only after relevant approval, rebuilding changed owners and real dependencies. No façade finish, heraldry, chains, interior or final export acceptance is claimed.
