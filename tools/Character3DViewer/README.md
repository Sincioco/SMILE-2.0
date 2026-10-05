# SMILE 2.0 - Sin Star I - Game Engine and Studio

**Sin Star Studio** is the native Windows application in this folder, retaining
the Character Viewer filenames and identity. It provides character inspection,
lightweight pose correction, battle simulations, Battle System and town authoring.
Studio is authoritative for approved town/battle behavior integrated into the
independent `D:\SMILE 2.0 - Sin Star I` game checkout.

Web development/publication is paused indefinitely. The separate `tools/SmileStudio`
S1 concept and September design plans are historical. They do not replace this app.

## Guides

| Start with | Covers |
| --- | --- |
| [Build and validation](docs/build-and-validation.md) | Complete native publication, safe staging, launch and focused checks |
| [Town Editor](docs/town-editor.md) | Maps/travel, cameras, editing, guides, Viewer/Blender exports and recovery |
| [Viewer export](docs/viewer-export.md) | Frozen identities, isolated cold capture and bounded native acceptance |
| [Characters and calibration](docs/characters-and-calibration.md) | Roster/packages, inspection, pose editing, authoritative JSON and transfer |
| [Battles and cameras](docs/battles-and-cameras.md) | Party simulations, Battle System orders/rewards and Beat Camera editing |
| [Status and limits](docs/status.md) | R04 evidence, remaining suite failures, recovery reports and unsupported operations |
| [ARCHITECTURE](ARCHITECTURE.md) | Compact owner map, runtime contracts, guardrails and architecture archives |

Read repository [AGENTS.md](../../AGENTS.md) before implementation. Shared contracts
remain in [town authoring](../../docs/architecture/town-authoring.md),
[terrain elevation](../../docs/architecture/terrain-elevation.md),
[precision boundary](../../docs/libraries/precision3d-boundary.md) and the
[Beat Camera checkpoint](../../docs/implementation/party-beat-camera-checkpoint.md).
Use current entries for contracts and their linked archives for older UI and
recorded evidence. Character-specific changes also require the canonical
package's creation-and-repair journey. Do not copy numeric fixes between characters.

## Native startup

Run from the repository root with PowerShell 7:

```powershell
pwsh -NoProfile -File tools/Character3DViewer/Launch.ps1
```

For development while an editor is running, build a separate complete publication:

```powershell
pwsh -NoProfile -File tools/Character3DViewer/Build.ps1 -Configuration Release -Target Native -OutputDirectory artifacts/studio-staging
```

Use an unused staging directory. **Never overwrite a running Studio or publish a
stripped asset declaration over its live output.** The publisher deletes undeclared
assets. `Build.ps1` defaults to All, so explicitly choose Native. Output relocation
does not change save identity; tests need isolated ApplicationIds and disposable data.

For a normal graceful rebuild/restart after preserving pending edits:

```powershell
pwsh -NoProfile -File tools/Character3DViewer/Launch.ps1 -Build
```

Launch verifies prerequisites/publication, requests normal closure without forcing
past edits, and reconciles/watches Arin and Orin's accepted JSON. It defaults to
`bin/Release/Character3DViewer.exe`; `-Configuration Debug` selects Debug.
Private package prerequisites and complete assets are required. See the build guide
before using a custom EXE or running focused native fixtures.

## First use

- **Sin Star I** starts on **Neris Metropolis**. Maps offers 17 permanent map names;
  open town documents and World atlases each have a 17-entry bound. Saved documents
  and authored initial cameras win over old layout descriptions.
- **Characters** exposes Arin, Orin, Valor, Zara, Mira, Dragon, Vrax, Kael and Milo. Yalis
  and older Mira comparisons are preserved but hidden in the native strip.
- **Battles** offers Party Dragon, Party Vrax and Kael Party with Arin, Orin, Zara
  and Mira. These are presentation simulations; **Battle System** adds orders,
  HP/MP, stats and session rewards. The game reuses the approved shared owners.
- Town: WASD moves the party, Tab eases behind it, middle drag orbits, left drag
  pans and wheel zooms. Shift+middle drag pans across Viewer tabs. O/Orbit preserves
  the displayed shot; right-click restores the selected map's initial overview.
- Character: select a clip to leave Demo, Space pauses playback, timeline controls
  scrub/step, and right-click resets presentation. Arin/Orin Pose is hidden by
  default; save or cancel pending edits before switching.
- The default window is 1440 x 960 unless saved placement is restored. A taller
  window helps the Town Editor palette. Help in the town toolbar lists its controls.

## Save and preserve work

**Edit Town > Files** provides Save for Viewer, Save As, Save All for Viewer,
Open for Viewer, Recent Versions, Blender export/import and permanent-map updates.
Viewer exports are native `.town` files plus matching 384 x 240 PNG photographs.
Blender conversion is separate and never mutates an open Blender scene.

Saves freeze the clicked revision; newer edits remain unsaved. Accepted photographs
retain their frozen content identity across later edits, replacement and eviction.
A PNG failure after town success is reported as partial success. Check
[Viewer export guide](docs/viewer-export.md) and [R04 evidence](docs/status.md#viewer-export-r04):
isolated cold capture now passes seven real-asset cases beside a live scene, alongside
the 17-slot warm identity regression. The final native Studio publication also builds successfully.
Only verified success reaches 100%; failed saves retain edits. Successful single
saves reveal the exact chosen file. Batch processing continues after an individual
failure without opening Explorer for every map.

Permanent-map replacement is explicit, preserves the previous map as recovery,
and requires exact `Yes` when names differ. Builds/launches must never restore old
generated defaults over user maps. Use `scripts/get-smile-data-root.ps1` to resolve
Windows' actual Saved Games location; do not choose legacy stores by timestamps.

For poses, **Sin's saved/exported calibration JSON is authoritative** in each
canonical versioned character package. Hashes, not timestamps, detect replacements.
Native binary saves and cooking mirrors are implementation details. Each character
has independent keys and fingerprints; rejected storage is not overwritten.
Use Launch for synchronization and read the calibration guide before import/export.
There is no automatic cross-process or browser-save merge.

## Current behavior and limits

- World Map lines derive from yellow **Map Load** destinations. Arrange cards there;
  edit playable connections in Town Editor, not by creating/deleting graph lines.
- Minimap routes use distance-weighted A* on validated road cells, not the historical
  breadth-first search. Shift+click queues up to 64 stops.
- Night Intensity defaults to **112%**; saved overrides remain editable. Every map
  shares nighttime water reflections; Metropolis retains them in daylight too.
- Section transfer, catalog/Blender imports, terrain/save budgets and scene effect
  limits remain explicit. Guide overlays are separate files, not town content.
- The intermittent Kael recovery and original Arin rejection trigger are unresolved;
  their reports, reproduction guidance and held investigation are in [status](docs/status.md).
- New validation must distinguish native execution, visual checks and historical
  evidence. For source-only Viewer work, rebuild/relaunch native Studio; no browser
  refresh or unrelated .NET/VSIX rebuild is implied.

## Historical evidence and old links

[October evidence](docs/archive/2026-10.md) and
[September/undated evidence](docs/archive/2026-09.md) preserve the former README body,
including dated test outcomes, r008/r009 layouts, southwest Spaceport, obsolete
camera coordinates and separate Studio S1 plans. These are historical records,
not current instructions or fresh acceptance. The following anchors keep old
README links useful; use the guides above for current operations.

<a id="october-5-dragon-creature-animation"></a>[October 5 Dragon creature animation](docs/archive/2026-10.md#october-5-dragon-creature-animation)<br>
<a id="october-3-2100-handoff-export-identity-and-native-acceptance"></a>[October 3 21:00 handoff: export identity and native acceptance](docs/archive/2026-10.md#october-3-2100-handoff-export-identity-and-native-acceptance)<br>
<a id="october-3-authored-maps-resident-spawns-and-exports"></a>[October 3 authored maps, resident spawns and exports](docs/archive/2026-10.md#october-3-authored-maps-resident-spawns-and-exports)<br>
<a id="october-3-predictable-town-zoom-and-initial-orbit-camera"></a>[October 3 predictable town zoom and initial orbit camera](docs/archive/2026-10.md#october-3-predictable-town-zoom-and-initial-orbit-camera)<br>
<a id="october-3-map-polish-and-arrival-editing"></a>[October 3 map polish and arrival editing](docs/archive/2026-10.md#october-3-map-polish-and-arrival-editing)<br>
<a id="october-3-continuous-road-edges-and-spaceport-access"></a>[October 3 continuous road edges and Spaceport access](docs/archive/2026-10.md#october-3-continuous-road-edges-and-spaceport-access)<br>
<a id="october-3-landscape-and-travel-fixes"></a>[October 3 landscape and travel fixes](docs/archive/2026-10.md#october-3-landscape-and-travel-fixes)<br>
<a id="october-2-landscape-and-editor-refinement"></a>[October 2 landscape and editor refinement](docs/archive/2026-10.md#october-2-landscape-and-editor-refinement)<br>
<a id="october-2-queued-exports-and-landscape-corrections"></a>[October 2 queued exports and landscape corrections](docs/archive/2026-10.md#october-2-queued-exports-and-landscape-corrections)<br>
<a id="october-2-local-terrain-appearance-and-outdoor-refinements"></a>[October 2 local terrain appearance and outdoor refinements](docs/archive/2026-10.md#october-2-local-terrain-appearance-and-outdoor-refinements)<br>
<a id="october-2-terrain-ramps-and-flowing-water"></a>[October 2 terrain, ramps and flowing water](docs/archive/2026-10.md#october-2-terrain-ramps-and-flowing-water)<br>
<a id="october-1-persistent-map-photographs"></a>[October 1 persistent map photographs](docs/archive/2026-10.md#october-1-persistent-map-photographs)<br>
<a id="october-1-demo-camera-and-countdown"></a>[October 1 Demo camera and countdown](docs/archive/2026-10.md#october-1-demo-camera-and-countdown)<br>
<a id="october-1-neris-orbital-spacecraft"></a>[October 1 Neris orbital spacecraft](docs/archive/2026-10.md#october-1-neris-orbital-spacecraft)<br>
<a id="october-1-spaceport-paving"></a>[October 1 spaceport paving](docs/archive/2026-10.md#october-1-spaceport-paving)<br>
<a id="october-1-editable-landscape-shapes"></a>[October 1 editable landscape shapes](docs/archive/2026-10.md#october-1-editable-landscape-shapes)<br>
<a id="october-1-resident-interaction"></a>[October 1 resident interaction](docs/archive/2026-10.md#october-1-resident-interaction)<br>
<a id="october-1-prepared-map-loading"></a>[October 1 prepared map loading](docs/archive/2026-10.md#october-1-prepared-map-loading)<br>
<a id="october-1-loading-correction"></a>[October 1 loading correction](docs/archive/2026-10.md#october-1-loading-correction)<br>
<a id="september-30-map-access-and-residents"></a>[September 30 map access and residents](docs/archive/2026-09.md#september-30-map-access-and-residents)<br>
<a id="september-30-precision-editing-and-presentation"></a>[September 30 precision editing and presentation](docs/archive/2026-09.md#september-30-precision-editing-and-presentation)<br>
<a id="minimap-road-routing"></a>[Minimap road routing](docs/archive/2026-09.md#minimap-road-routing)<br>
<a id="safe-native-builds-and-launch"></a>[Safe native builds and launch](docs/archive/2026-09.md#safe-native-builds-and-launch)<br>
<a id="native-neris-town"></a>[Native Neris Town](docs/archive/2026-09.md#native-neris-town)<br>
<a id="native-loading-and-validation"></a>[Native loading and validation](docs/archive/2026-09.md#native-loading-and-validation)<br>
<a id="native-battle-system"></a>[Native Battle System](docs/archive/2026-09.md#native-battle-system)<br>
<a id="automatic-native-recovery-reports"></a>[Automatic native recovery reports](docs/archive/2026-09.md#automatic-native-recovery-reports)<br>
<a id="native-party-status-and-roster"></a>[Native party status and roster](docs/archive/2026-09.md#native-party-status-and-roster)<br>
<a id="kael-native"></a>[Kael (native)](docs/archive/2026-09.md#kael-native)<br>
<a id="yalis-native"></a>[Yalis (native)](docs/archive/2026-09.md#yalis-native)<br>
<a id="mira-comparisons-native"></a>[Mira comparisons (native)](docs/archive/2026-09.md#mira-comparisons-native)<br>
<a id="party-beat-cameras"></a>[Party Beat Cameras](docs/archive/2026-09.md#party-beat-cameras)<br>
<a id="maintainer-routing"></a>[Maintainer routing](docs/archive/2026-09.md#maintainer-routing)<br>
<a id="vrax-attacks"></a>[Vrax attacks](docs/archive/2026-09.md#vrax-attacks)<br>
<a id="equipment-vfx-controls"></a>[Equipment VFX controls](docs/archive/2026-09.md#equipment-vfx-controls)<br>
<a id="build-and-launch"></a>[Build and launch](docs/archive/2026-09.md#build-and-launch)<br>
<a id="inspection"></a>[Inspection](docs/archive/2026-09.md#inspection)<br>
<a id="published-pose-defaults"></a>[Published pose defaults](docs/archive/2026-09.md#published-pose-defaults)<br>
<a id="pose-corrections"></a>[Pose corrections](docs/archive/2026-09.md#pose-corrections)<br>
<a id="party-arena"></a>[Party arena](docs/archive/2026-09.md#party-arena)<br>
<a id="shared-presentation-libraries"></a>[Shared presentation libraries](docs/archive/2026-09.md#shared-presentation-libraries)<br>
<a id="permanent-character-workflow-handoff"></a>[Permanent character-workflow handoff](docs/archive/2026-09.md#permanent-character-workflow-handoff)<br>
<a id="party-dragon-reactions-and-diagnostics"></a>[Party Dragon reactions and diagnostics](docs/archive/2026-09.md#party-dragon-reactions-and-diagnostics)<br>
<a id="calibration-transfer"></a>[Calibration transfer](docs/archive/2026-09.md#calibration-transfer)<br>
<a id="grounding-and-capture"></a>[Grounding and capture](docs/archive/2026-09.md#grounding-and-capture)<br>
<a id="checked-calibration-persistence"></a>[Checked calibration persistence](docs/archive/2026-09.md#checked-calibration-persistence)<br>
<a id="tab-switching-and-startup-loading"></a>[Tab switching and startup loading](docs/archive/2026-09.md#tab-switching-and-startup-loading)<br>
<a id="operating-limits-and-preservation"></a>[Operating limits and preservation](docs/archive/2026-09.md#operating-limits-and-preservation)<br>
<a id="complete-pose-recovery-check-september-20-2026"></a>[Complete pose recovery check (September 20, 2026)](docs/archive/2026-09.md#complete-pose-recovery-check-september-20-2026)<br>
<a id="native-world-and-town-editor"></a>[Native World and Town Editor](docs/archive/2026-09.md#native-world-and-town-editor)<br>
<a id="town-tabs-file-dialogs-and-versions"></a>[Town tabs, file dialogs and versions](docs/archive/2026-09.md#town-tabs-file-dialogs-and-versions)<br>
<a id="town-editor-guides-and-history"></a>[Town Editor guides and history](docs/archive/2026-09.md#town-editor-guides-and-history)<br>
<a id="provisional-west-side-castle"></a>[Provisional west-side castle](docs/archive/2026-09.md#provisional-west-side-castle)<br>
<a id="october-1-demo-and-blender-follow-up"></a>[October 1 Demo and Blender follow-up](docs/archive/2026-10.md#october-1-demo-and-blender-follow-up)<br>
