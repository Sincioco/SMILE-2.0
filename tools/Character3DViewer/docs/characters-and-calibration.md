# Characters, inspection and calibration

[Start here](../README.md) · [Build and checks](build-and-validation.md) · [Battle cameras](battles-and-cameras.md)

## Roster and packages

Native Characters exposes Arin, Orin, Valor, Zara, Mira, Dragon, Vrax, Kael, Milo and Arin v5.8
(`NativeViewerTabs.ChildAt`). Yalis and Mira1/2/3 remain preserved profiles/packages,
hidden from the current native strip. Party Dragon, Party Vrax and Kael Party field
Arin, Orin, Zara and Mira; Valor remains outside that active party. Individual and
Party actors use their own assets, playback, effects and calibration.

The canonical game root is `D:\SMILE 2.0 - Sin Star I`. Package links below use the
ignored local `games/SinStarI` compatibility junction. Read the relevant package
journey before changing model, rig, equipment, grounding, clips, calibration or VFX:

| Package | Authority |
| --- | --- |
| [MiloV1](../../../games/SinStarI/SourceAssets/Characters/Pet/MiloV1/MILO-CREATION-AND-REPAIR-JOURNEY.md) | Tripo original, 25-bone canine Blender rig, eight authored clips and floor-contact measurements |
| [ArinV58](../../../games/SinStarI/SourceAssets/Characters/Paladin/ArinV58/ARIN-CREATION-AND-REPAIR-JOURNEY.md) | Separate 11-clip review profile, approved Idle grip, repaired facial weights and independent zero-key calibration |
| [ArinV57](../../../games/SinStarI/SourceAssets/Characters/Paladin/ArinV57/ARIN-CREATION-AND-REPAIR-JOURNEY.md) | Accepted Arin assets, pose JSON and repair workflow |
| [OrinV13](../../../games/SinStarI/SourceAssets/Characters/Tank/OrinV13/ORIN-CREATION-AND-REPAIR-JOURNEY.md) | Orin assets, grounding and independent calibration |
| [ValorV1](../../../games/SinStarI/SourceAssets/Characters/Knight/ValorV1/VALOR-CREATION-AND-REPAIR-JOURNEY.md) / [ZaraV1](../../../games/SinStarI/SourceAssets/Characters/Warrior/ZaraV1/ZARA-CREATION-AND-REPAIR-JOURNEY.md) | Licensed originals, conversion, equipment sockets and checksums |
| [VraxV1](../../../games/SinStarI/SourceAssets/Bosses/Vrax/VraxV1/VRAX-CREATION-AND-REPAIR-JOURNEY.md) / [KaelV1](../../../games/SinStarI/SourceAssets/Characters/Kael/KaelV1/README.md) | Boss rigs, clips, sounds and limits |
| [MiraTripoV1](../../../games/SinStarI/SourceAssets/Characters/Healer/MiraTripoV1/README.md) | Accepted healer; no added body aura, staff outline or head sparkles |
| [RedDragonV13](../../../games/SinStarI/SourceAssets/Bosses/RedDragon/RedDragonV13/README.md) | Current v1.3.1 rig, eight baked clips and foot-contact evidence |

Dragon Walk/Run are in-place loops; its six combat slots retain their order and
impact timing. The v1.3.1 arms are independent of wings and Hit includes staggered
recoil/rebound. Studio plays baked skeletal keys, not terrain-adaptive runtime IK.
Kael has sixteen live clips with normal/Earth/Water rotation and speed 200; Fire
Lab's additional preview is not adopted into the current Party rotation. Sword hides
during bending, which casts from home. Mira also defaults to 200. Character package
notes retain exact clip, scale and visual limitations instead of duplicating them here.

Arin v5.8 uses its own asset, fingerprint, in-memory bank, data key and canonical
JSON. `sync-arin-v5-7-calibration.ps1 -Character ArinV58` manages that revision.
Launch watches it alongside Arin v5.7 and Orin. Party retains v5.7; the v5.8 tab
is for Sin's manual pose corrections, with no old calibration or fire offsets.

## Inspection and equipment

- Backtick cycles through panels hidden (including Pose Calibration), all UI hidden, then the prior UI restored. Headers, the timeline, and helper text remain after the first tap. Hidden controls cannot intercept the mouse. This does not change panel-open preferences, edits, playback, or the camera. Right-click reset restores the normal UI with Pose Calibration hidden.
- Space pauses/resumes movement while keeping camera controls active.
- Fire and Lightning keep animating by default when Space pauses the scene on Desktop
  and Web. Scene pause freezes actor/choreography time and leaves camera controls active;
  it does not implicitly freeze either VFX family.
- The existing Freeze Fire / Play Fire and Freeze Lightning / Play Lightning controls
  independently freeze each family across Party actors, even while the scene remains
  paused. Frozen effects retain their current world-space snapshot; re-enabling advances
  from that snapshot without a catch-up burst. Reset re-enables both families.
- Fire and Lightning now advance once at the Viewer scene boundary after every actor has staged its effect endpoints. An unavailable optional equipment path cannot stall another emitter. Orin and Dragon borrow distinct generation-safe local-light leases instead of writing fixed renderer slots.
- Bare Alt no longer enters Windows' modal keyboard-menu state. Alt+Enter still toggles fullscreen and Alt+F4 closes the window. Recompile older game executables to pick up the shared native runtime fix.
- Right-click resets presentation as on a fresh launch: Idle, Demo, dragon/floor/grid visible, landscape backdrop, unpaused. There is no inactivity timer that re-enables Demo.
- Left drag pans the view; middle drag orbits; wheel zooms smoothly.
- With Pose Calibration open, a middle drag anchors the point under the cursor at the selected joint/equipment depth. It preserves that point's on-screen location, including after prior pan or close-up zoom, instead of orbiting the distant arena center. The view remains where it was on release; reset clears the custom anchor. This uses a depth plane rather than mesh-surface picking.
- Zoom extends to -144 for glove and grip inspection. Beyond the former -48 limit, it moves the camera closer to the current panned anchor, reaching one tenth of the former distance. Pan the glove toward the center, then zoom in; the arena size and character pose are unchanged.
- A dedicated Camera panel stays in the original lower-right location on every
  character and Party tab. H Orbit, V Orbit and Zoom support hover-wheel adjustment
  and capture slider drags until release, even outside the track. Vertical orbit
  supports 360 degrees. View, Present, All and Fit share that fixed panel on every tab.
- During Beat Edit, that Camera panel reads and edits the active Beat camera. View
  restores the edit-start composition, Present restarts Beat Sequence playback, All
  stages the Reset All Beats draft, and Fit stages the current main Beat's built-in
  camera. Save or Cancel retains the normal Beat draft rules.
- All Viewer panel backgrounds render at 80-percent opacity. Text, buttons, sliders,
  markers and scroll indicators retain their own control styling.
- Right-side panel content above Camera scrolls vertically with the mouse wheel when
  it cannot fit. A thin scrollbar appears only while the pointer is over that
  overflowing panel and hides when the pointer leaves.
- Arin v5.7, Arin v5.8, Orin, Valor and Zara expose independent Weapon and Shield strength sliders in
  Character Status. Each uses the same drag and hover-wheel interaction as H Orbit,
  ranges from 0 to 200 percent and starts at 100 percent for the Viewer session.
- Arin and Orin start at speed 200. Their individual demos target three seconds per sequence and let an in-progress animation finish before advancing. Orin Block plays once and holds its final pose. Selecting an animation disables Demo; Block remains a one-shot.
- D toggles the dragon; W toggles the current character’s weapon; S toggles shield. Hiding the dragon does not shrink the arena.
- B/BG cycles colors and two static bitmaps. The default is the Sin Star I landscape without its title.
- Floor/F toggles the floor independently; G toggles the grid. Glow, Socket, Channel and lighting controls remain available. Profile is Desktop-only and hidden in Party.
- Pose shows/hides Pose Calibration, which is **hidden at startup and reset**.
- Sword Fire and Shield Fire independently toggle the default-on thermal effects on Arin v5.7 and v5.8. W/S also hide the corresponding fire with its equipment. The sword has a fuller orange flame and world-space lingering trail; the shield uses much smaller flames instead of the old solid golden glow.
- Normal animation loop wraps and clip changes retain existing fire particles until they fade; source velocity inheritance is zeroed for the transition update so the pose reset does not launch particles across the gap. Editing a paused pose clears stale emission. Explicit right-click reset clears the effects for a fresh start.
- G0 clarification: the retained-tail clip-change behavior applies to automatic Demo advancement. Explicit clip selection/navigation is a cut that clears/reseeds visual history; it is not a corrected-pose cross-fade.

The timeline supports drag scrubbing and hover-wheel single-frame steps. `0-Frame` jumps to the start; `< Key` / `Key >` jump between saved corrections; `< Frame` / `Frame >` step immediately on click and repeat one frame every 300 ms while held. Release stops repetition; reset or hiding the UI cancels it. Holding captures the gesture so it cannot pan the camera; a delayed update never catches up with a burst of frame steps. Drag a green keyframe tick to move it. All timeline navigation pauses the scene automatically and never toggles it back into playback. Dragon has no saved pose keys to navigate or drag.

Reflective Battle Floor and VFX Reflections are separate default-on session controls.
Floor hiding or Original suppresses reflection work without erasing preferences;
failure reports Unavailable while retaining the usable matte scene. Reflections
replay accepted poses/effects and create no extra simulation or effect admission.
Heat distortion is excluded. Shared VFX must budget all actors together: twelve
fire emitters and eight lightning effects are not per-character quotas. Native
rendering stages up to 16,384 particles on demand (Web: 8,192); its separate persistent
GPU pool has 32,768 aggregate slots. Fire uses that persistent pool; consult the
[renderer particle contract](../../../docs/architecture/renderer3d-gpu-particles.md)
for per-system, backend and admission limits.
GPU float32 and depth precision limits still apply despite Double authoring paths.
See [precision boundary](../../../docs/libraries/precision3d-boundary.md),
[shared effects/owners](../ARCHITECTURE.md#selectable-equipment-presentation) and
[archived detailed VFX controls](archive/2026-09.md#equipment-vfx-controls).

## Pose corrections

Select Weapon Wrist, Shield Wrist, Weapon or Shield, an axis, and Rotate or Move. The slider and hover-wheel edit the selected channel; the former -5/+5 buttons are removed. Decouple switches allow each equipment item to retain its base animation while its wrist is corrected independently.

The viewport uses Blender-inspired axis transform controls, not Blender's full transform system. They are **hidden and disabled by default** on Desktop and Web. Click **Show Gizmo** in Pose Calibration to enable them; **Hide Gizmo** removes their drawing and mouse hit testing without changing or saving the current numeric preview. Hidden handles cannot start an invisible keyboard drag. Numeric controls, axis selection and Save/Cancel remain available. A fresh character load or full reset restores the hidden default; this UI setting is not written into character calibration JSON.

When enabled, red X, green Y and blue Z handles appear at the selected wrist or equipment socket. The rings use 128 segments, camera-relative thousandths for smooth projection, round stroke joins and a 12-pixel pick tolerance; hover highlights the picked axis. Drag an arrow to move equipment or drag along a colored ring to rotate the selected target. Slow rotation retains partial degrees. `E` starts Move for equipment, `R` starts Rotate, and `X`, `Y`, or `Z` constrains the active axis. `Enter` first accepts an active drag as an unsaved preview; pressing Enter again or Save Frame saves the pose. `Esc` or right-click cancels the drag. Plane handles, free trackball, scaling and local/global space selection are not provided; the outer ring is a visual guide only.

To turn a fitted sword or shield without pulling its grip away, select **Weapon** or **Shield**, enable **In Place**, choose **Rotate**, then adjust X/Y/Z. This holds the equipment's current hand-attachment point while compensating its position offsets; it does not edit either wrist. Save Frame stores the resulting rotation and position together in the existing format. Cancel/Reload restores both. The control is an editing mode, not another animated property. Position compensation retains the existing whole-world-unit precision and rejects edits beyond the saved +/-100-unit range.

`Save Frame` stores the entire correction snapshot (all 20 channels, including both equipment coupling flags), not just the visible axis. There are up to 256 keys per clip. One key is held throughout a clip; multiple keys interpolate between frames, using shortest-path rotation and cyclic interpolation for loops.

Prev Key / Next Key navigate; Delete Key removes the current saved key. Copy Key / Paste Key transfer complete snapshots. Reload Key discards unsaved changes and restores saved corrections. Reset resets the selected target's correction values; Delete All Key Frames clears only this animation's saved keys after the Confirm Current Clip confirmation. It sits in the lower-left corner with a red warning border. There is no Reset All button. Save Frame and Cancel are together in the lower-right corner of Pose Calibration.

Arin’s saved JSON is `games\SinStarI\SourceAssets\Characters\Paladin\ArinV57\Calibration\arin-v5.7-pose-calibration.json`. Its full path appears below the timeline in muted gray at the original 9-point size; click it to select the file in Explorer. It remains visible with panels hidden, and hides with the full UI. Runtime binary Save Data is disposable infrastructure, not the repository source of truth. Before committing calibration changes, run `scripts\sync-arin-v5-7-calibration.ps1 -Mode Export -AllowMissing`.

Orin uses `games\SinStarI\SourceAssets\Characters\Tank\OrinV13\Calibration\orin-v1.3-pose-calibration.json`.
The same synchronizer accepts `-Character Orin`; its default remains Arin for existing scripts.
Each character has a distinct storage key and model/clip/socket fingerprint. Equal clip names
and frame numbers never share correction values. Switching away from an unsaved pose asks
for Save Frame or Cancel inside the editor. Both characters use the same correction code.

## Calibration transfer

The filename below the timeline identifies the current character's calibration.
**Download Key Frames**, beside **Import Key Frames**, exports the current saved JSON snapshot:
Web requests a browser download; native Windows uses a Save As dialog. This
explicit export button does not open Explorer or include temporary preview edits.
On native Windows, clicking the **filename** opens the canonical file's Explorer location.
On Web, clicking the **filename** downloads the current **saved** schema-2 JSON snapshot, not
temporary unsaved pose adjustments. All 20 channels and name-bound clips are
preserved. The label does not claim the browser can read or write `D:\`.

Sin's saved/exported JSON is the authoritative character pose revision. Save it to
the canonical package path shown below the timeline. An export elsewhere remains
a separate file until placed there; browser downloads do not write the repository.
Native `Launch.ps1` reconciles that JSON before opening the editor. A small ignored
application-data receipt records the last synchronized JSON hash. Replacing the JSON
invalidates the receipt even when its timestamp is older: launch adopts that export,
and commit/watch exports refuse to overwrite it with a differing working save.
Ordinary later Save Frame operations still synchronize through the launcher watcher.
Matching exports preserve the exact JSON bytes. Game/Lab builds cook that same JSON
into disposable assets; running programs require a rebuild/relaunch to adopt changes.
**Import Key Frames** uses the shared UTF-8 picker. The transfer row is ordered
Import Key Frames, Download Key Frames, JSON filename, then status text. Narrow
windows show status on the line below instead of truncating the confirmation.
Save or cancel
an active pose edit first. Selecting a file validates it without changing saved
keys; click **Replace Keys?** to confirm replacing this character's complete saved
track. **Undo Last Change** restores the preceding track. Import pauses the scene.
Changing character/profile cancels a pending import, and a changed saved baseline
requires choosing the file again. A failed write retains the saved track and Undo.

The source-level `CalibrationJson` reader accepts current schema-2/storage-3
snapshots with this publication's exact character, application, data-key and asset
hash metadata. Clip names are authoritative; object property order and clip order
may differ, and indices are hints. It rejects unknown/duplicate fields or clips,
incomplete channels, invalid flags/vectors/ranges, repeated or out-of-range frames,
count mismatches, malformed/trailing JSON and files above the 8 MiB transfer limit.
Current identity/clip strings are bounded printable ASCII (including equivalent
JSON escapes). This is not a general JSON API or a legacy-character migration tool.
Omitted clips are cleared by the explicitly confirmed full-snapshot replacement.
Import retries a previously rejected working save using the normal checked,
read-only loader. A valid save restores its keys before the picker/confirmation;
still-invalid or unavailable storage stays write-blocked and reports its reason.
No import bypasses the character fingerprint or overwrites rejected storage.
There is no automatic cross-tab/process merge.
The shared export fixture validates both characters against canonical JSON and
the desktop binary serializer. An actual Edge Arin download also round-tripped
byte-for-byte through the native text import/export dialogs in an isolated sample.

### Checked calibration persistence

The shared Viewer now uses optional `Save Data` / `Load Data` Status results.
Denied storage, quota, corrupt envelopes and oversized blocks no longer terminate
the scene. A failed load blocks saves instead of silently seeding over the unknown
working copy. A checksummed backup can be read with a visible recovery notice;
the primary is not changed until a successful explicit save.

Failed writes restore the previous saved bytes and key track. Save Frame keeps its
temporary pose preview open for Retry/Cancel; a failed Undo retains the undo entry.
The JSON download still contains saved keys, never a failed candidate. Browser
primary and last-good backup remain origin/app/key-specific; neither writes Drive D.
Concurrent tabs/processes still require coordination by the user (no merge/locking).
Wrong-profile working data remains rejected. The import validator never guesses
an identity migration or silently overrides a rejected working save.

## Grounding and accepted defaults

Packaged runtime defaults seed missing storage only; an existing working save,
including a deliberately cleared track, takes precedence over those defaults. The
launcher separately reconciles authoritative canonical JSON by content hash; a
replaced JSON file wins under that synchronization policy. Required defaults must
validate the current model/clip/socket identity. Failed loads block writes instead
of seeding over unknown data. Never relax a fingerprint to make a companion load.

Before accepting a character package, compare the skinned body's bind-pose minimum
Y with Idle frame 0 (excluding equipment/VFX), then every clip's frame 0, Block/Hit
contact and settled Death. Fix shared placement before per-clip correction, preserve
intentional jumps, and record measurement/hash in the package. Do not copy another
character's numeric offset. Equipment and VFX use the final grounded actor transform.
Solid-floor orbit stops at ground height; grid-only inspection may go below it.

Runtime binary Save Data and ignored cooked assets are disposable mirrors. Before
commits containing calibration changes, export accepted native saves with
`scripts/sync-arin-v5-7-calibration.ps1 -Mode Export -AllowMissing`, and again with
`-Character Orin` when Orin is affected. Preserve replaced JSON by content, even
with an older timestamp. There is no automatic cross-process or browser merge.
Browser transfer details above describe preserved behavior, not resumed Web work.
See [status](status.md) for unresolved rejection triggers and recovery limits.
