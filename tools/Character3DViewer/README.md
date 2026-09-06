# SMILE 2.0 - 3D Viewer, Animation Editor

Native-first reusable inspection and lightweight pose-correction tool. Party is the launch default. The Character tabs select Arin, Orin, Dragon, or the Party arena. Desktop Profile retains Arin v5.6, the earlier prototype, and the technical fixture for diagnostics. Web publication omits that control, its shortcut and the obsolete diagnostic assets. Current Web parity is in progress under H6.1; this is not the future Battle Scene Editor.

Dragon inspection uses the same clip buttons, timeline/frame stepping, playback speed, demo, lighting/material channels, sockets, pan/orbit/zoom and reset as the hero tabs. Both heroes remain in the arena with their own assets and saved corrections. Head Aim constrains only the head joint; At Arin/At Orin selects its target. The current Pose Calibration targets remain humanoid wrists and equipment, so they do not apply to Dragon. Dragon VFX and hero equipment visibility remain independent. Pose is disabled for Dragon, including its turn in Party.

Party members start on a 300-unit front arc with 40 degrees between its endpoints. The placement function distributes any supplied member count over that arc. The two current attack destinations remain in front of the Dragon; approach and retreat interpolate from each member's own home position. Battle cameras sample this frame's actor/Dragon poses.

The Party runtime stores home, approach, bounds-derived clearance and facing metadata in
participant records. It separates approach lanes when the actors' measured ground-plane
bounds would overlap. The shared formation evaluator is covered with a technical third
participant; no unfinished third hero asset is implied.

The editor source and build/launch entry points belong here. Sin Star I owns the self-contained character package at `games\SinStarI\SourceAssets\Characters\Paladin\ArinV57`. Orin owns `games\SinStarI\SourceAssets\Characters\Tank\OrinV13`. Do not edit ignored cooking inputs as canonical character assets.

## Build and launch

Run `Build.ps1`, then `Launch.ps1`. Builds use the same configuration layout as
normal SMILE Visual Studio projects:

```text
bin/
  Debug/
    Character3DViewer.exe
    Assets/ ...
    Web/index.html ...
  Release/
    Character3DViewer.exe
    Assets/ ...
    Web/index.html ...
```

`Build.ps1` defaults to `-Configuration Release -Target All`, building native
first and then Web. Select `-Configuration Debug` for both Debug outputs, or
`-Target Native` / `-Target Web` for one target. Each Web directory is a complete
static publication; upload its entire contents, including all assets.

The Viewer Web build generates an ignored publication project and profile policy
containing only the current Arin, Orin and Dragon model assets. The normal asset
publisher removes obsolete managed diagnostic files from that Web output only.
Textures are neither transcoded nor resized; Desktop diagnostics and canonical
packages remain intact. Visual Studio's direct project build does not invoke this
tool-specific publication script yet; use `Build.ps1` for the current slim bundle.

`Launch.ps1` defaults to `bin\Release\Character3DViewer.exe`. Use
`Launch.ps1 -Configuration Debug` for Debug. `-Build` rebuilds the selected native
configuration before launch. A custom `-Executable` must already exist and cannot
be combined with `-Build`, preventing an unrelated build followed by a stale launch.
The launcher exports live calibration, closes old instances, preserves/restores
the stable working copy and watches both characters' saves to mirror them into
their separate canonical repository JSON files. Output relocation does not change
application IDs, data keys, fingerprints, or the saved-data location.

## Inspection

Automation that already closes the old Viewer normally can pass
`Launch.ps1 -Build -SkipWindowActivation`, then use its supported native window
control to foreground the returned process. The normal interactive launch behavior
is unchanged; calibration synchronization still runs in either mode.

### Published pose defaults

`Prepare-BuildAssets.ps1` validates the current canonical Arin and Orin JSON and
serializes fingerprinted, name-bound SMKF defaults into ignored `Assets/Calibration`.
Both native and Web publication include these declared assets. On first load the
Viewer uses these corrections only when no saved working copy exists. Existing
Save Data, including a deliberately cleared track, takes precedence; legacy Arin
saves retain their migration path. Missing or invalid required defaults are rejected.
Loading defaults does not write a save or change the canonical JSON/model.

To build the current Web Viewer from the repository root:

```powershell
tools/Character3DViewer/Build.ps1 -Configuration Release -Target Web
python -m http.server 8766 --bind 127.0.0.1 --directory tools/Character3DViewer/bin/Release/Web
```

Open `http://127.0.0.1:8766/` in Chrome or Edge. Port 8766 avoids the existing
hardening server on 8765; changing ports changes the browser storage origin.
To retain browser-authored saves from `http://127.0.0.1:8765`, stop that server
first and serve this directory on **8765** instead (the URL path may change,
but host, scheme and port must remain the same). Do not erase either origin's
storage. The older `artifacts/web/h6-1/Character3DViewer` is test evidence, not the
normal publishable build. Browser working saves are separate and origin-scoped;
rebuilding does not replace them.
The Windows JSON path/Explorer action belongs to native. Browser JSON transfer,
storage recovery, MSAA and the rest of the H6.1 workflow gate remain in progress;
no automatic browser-to-repository synchronization is claimed.

- Backtick cycles through panels hidden (including Pose Calibration), all UI hidden, then the prior UI restored. Headers, the timeline, and helper text remain after the first tap. Hidden controls cannot intercept the mouse. This does not change panel-open preferences, edits, playback, or the camera. Right-click reset restores the normal UI with Pose Calibration hidden.
- Space pauses/resumes movement while keeping camera controls active.
- Flames keep animating when Space pauses the scene. The separate Pause Flames / Play Flames button controls only the flames, independently of scene playback. Reset starts both again.
- Orin has a Freeze Lightning / Play Lightning toggle. Party's VFX Playback row independently freezes Fire and Lightning across all actors. Frozen effects retain their current world-space snapshot while character animation and camera controls continue; resume advances from that snapshot without a catch-up burst. Reset unfreezes both families.
- Fire and Lightning now advance once at the Viewer scene boundary after every actor has staged its effect endpoints. An unavailable optional equipment path cannot stall another emitter. Orin and Dragon borrow distinct generation-safe local-light leases instead of writing fixed renderer slots.
- Bare Alt no longer enters Windows' modal keyboard-menu state. Alt+Enter still toggles fullscreen and Alt+F4 closes the window. Recompile older game executables to pick up the shared native runtime fix.
- Right-click resets presentation as on a fresh launch: Idle, Demo, dragon/floor/grid visible, landscape backdrop, unpaused. There is no inactivity timer that re-enables Demo.
- Left drag pans the view; middle drag orbits; wheel zooms smoothly.
- With Pose Calibration open, a middle drag anchors the point under the cursor at the selected joint/equipment depth. It preserves that point's on-screen location, including after prior pan or close-up zoom, instead of orbiting the distant arena center. The view remains where it was on release; reset clears the custom anchor. This uses a depth plane rather than mesh-surface picking.
- Zoom extends to -144 for glove and grip inspection. Beyond the former -48 limit, it moves the camera closer to the current panned anchor, reaching one tenth of the former distance. Pan the glove toward the center, then zoom in; the arena size and character pose are unchanged.
- H Orbit, V Orbit and Zoom support hover-wheel adjustment and capture slider drags until release, even outside the track. Vertical orbit supports 360 degrees.
- Arin and Orin start at speed 200. Their individual demos target three seconds per sequence and let an in-progress animation finish before advancing. Orin Block plays once and holds its final pose. Selecting an animation disables Demo; Block remains a one-shot.
- D toggles the dragon; W toggles the current character’s weapon; S toggles shield. Hiding the dragon does not shrink the arena.
- B/BG cycles colors and two static bitmaps. The default is the Sin Star I landscape without its title.
- Floor / Grid hides/shows both. Glow, Socket, Channel and lighting controls remain available. Profile is Desktop-only and hidden in Party.
- Pose shows/hides Pose Calibration, which is **hidden at startup and reset**.
- Sword Fire and Shield Fire independently toggle the default-on thermal effects on Arin v5.7. W/S also hide the corresponding fire with its equipment. The sword has a fuller orange flame and world-space lingering trail; the shield uses much smaller flames instead of the old solid golden glow.
- Normal animation loop wraps and clip changes retain existing fire particles until they fade; source velocity inheritance is zeroed for the transition update so the pose reset does not launch particles across the gap. Editing a paused pose clears stale emission. Explicit right-click reset clears the effects for a fresh start.
- G0 clarification: the retained-tail clip-change behavior applies to automatic Demo advancement. Explicit clip selection/navigation is a cut that clears/reseeds visual history; it is not a corrected-pose cross-fade.

The timeline supports drag scrubbing and hover-wheel single-frame steps. `0-Frame` jumps to the start; `< Key` / `Key >` jump between saved corrections; `< Frame` / `Frame >` step immediately on click and repeat one frame every 300 ms while held. Release stops repetition; reset or hiding the UI cancels it. Holding captures the gesture so it cannot pan the camera; a delayed update never catches up with a burst of frame steps. Drag a green keyframe tick to move it. All timeline navigation pauses the scene automatically and never toggles it back into playback. Dragon has no saved pose keys to navigate or drag.

## Pose corrections

Select Weapon Wrist, Shield Wrist, Weapon or Shield, an axis, and Rotate or Move. The slider and hover-wheel edit the selected channel; the former -5/+5 buttons are removed. Decouple switches allow each equipment item to retain its base animation while its wrist is corrected independently.

The viewport uses Blender-inspired axis transform controls, not Blender's full transform system. They are **hidden and disabled by default** on Desktop and Web. Click **Show Gizmo** in Pose Calibration to enable them; **Hide Gizmo** removes their drawing and mouse hit testing without changing or saving the current numeric preview. Hidden handles cannot start an invisible keyboard drag. Numeric controls, axis selection and Save/Cancel remain available. A fresh character load or full reset restores the hidden default; this UI setting is not written into character calibration JSON.

When enabled, red X, green Y and blue Z handles appear at the selected wrist or equipment socket. The rings use 128 segments, camera-relative thousandths for smooth projection, round stroke joins and a 12-pixel pick tolerance; hover highlights the picked axis. Drag an arrow to move equipment or drag along a colored ring to rotate the selected target. Slow rotation retains partial degrees. `G` starts Move for equipment, `R` starts Rotate, and `X`, `Y`, or `Z` constrains the active axis. `Enter` first accepts an active drag as an unsaved preview; pressing Enter again or Save Frame saves the pose. `Esc` or right-click cancels the drag. The `E` key is also accepted as a classroom-friendly Rotate alias. Plane handles, free trackball, scaling and local/global space selection are not provided; the outer ring is a visual guide only.

To turn a fitted sword or shield without pulling its grip away, select **Weapon** or **Shield**, enable **In Place**, choose **Rotate**, then adjust X/Y/Z. This holds the equipment's current hand-attachment point while compensating its position offsets; it does not edit either wrist. Save Frame stores the resulting rotation and position together in the existing format. Cancel/Reload restores both. The control is an editing mode, not another animated property. Position compensation retains the existing whole-world-unit precision and rejects edits beyond the saved +/-100-unit range.

`Save Frame` stores the entire correction snapshot (all 20 channels, including both equipment coupling flags), not just the visible axis. There are up to 256 keys per clip. One key is held throughout a clip; multiple keys interpolate between frames, using shortest-path rotation and cyclic interpolation for loops.

Prev Key / Next Key navigate; Delete Key removes the current saved key. Copy Key / Paste Key transfer complete snapshots. Reload Key discards unsaved changes and restores saved corrections. Reset resets the selected target's correction values; Delete All Key Frames clears only this animation's saved keys after the Confirm Current Clip confirmation. It sits in the lower-left corner with a red warning border. There is no Reset All button. Save Frame and Cancel are together in the lower-right corner of Pose Calibration.

Arin’s saved JSON is `games\SinStarI\SourceAssets\Characters\Paladin\ArinV57\Calibration\arin-v5.7-pose-calibration.json`. Its full path appears below the timeline in muted gray at the original 9-point size; click it to select the file in Explorer. It remains visible with panels hidden, and hides with the full UI. Runtime binary Save Data is disposable infrastructure, not the repository source of truth. Before committing calibration changes, run `scripts\sync-arin-v5-7-calibration.ps1 -Mode Export -AllowMissing`.

Orin uses `games\SinStarI\SourceAssets\Characters\Tank\OrinV13\Calibration\orin-v1.3-pose-calibration.json`.
The same synchronizer accepts `-Character Orin`; its default remains Arin for existing scripts.
Each character has a distinct storage key and model/clip/socket fingerprint. Equal clip names
and frame numbers never share correction values. Switching away from an unsaved pose asks
for Save Frame or Cancel inside the editor. Both characters use the same correction code.

## Party arena

Party places Arin and Orin together on the arena, facing the dragon. They approach, attack,
return to formation, and take turns while the other guards. The camera orbits by default.
Camera 1 sweeps a smooth front arc from one side of the boss to the other, using the same
12-degree-per-second phase rate as the individual tabs and easing at the arc endpoints.
It keeps advancing while battle cameras are selected. Camera 2 frames the heroes' attack
and defense beats; rear views sit near waist height and look upward toward the boss.
Camera 3 looks past the Dragon's head and mouth toward the party during its windup and
fireball charge. The two battle cameras cut immediately between their independent poses.
Arin and Orin begin side by side on the dragon's forward centerline, converge on
two close chest lanes, and stop outside its body before attacking. Orin applies his own -55-degree
visual yaw correction so his imported hammer stance faces the target. The closer arena camera
keeps both sides readable while the nearer actor crosses the foreground. The viewer opens
directly in Party mode. Its controls sit in a dedicated left panel below the shared character
tabs, and the panel names the active attack while a party member strikes. Orin's Death
presentation follows a measured ground curve so his falling body settles onto the arena floor.
Space pauses movement; the usual pan, orbit, eased zoom, keyboard controls and reset remain
available. Weapon and Shield affect both members. Party uses the same right inspector and
bottom timeline as the individual tabs, following whichever actor owns the current turn.
Timeline navigation pauses the scene and previews that actor without advancing the battle.
Open Pose to edit the active Arin or Orin using that hero's own saved correction track.
The Pose panel temporarily occupies the Party/Enemy left-panel area; closing it restores
the Party controls. Dragon permits timeline inspection only. Resume restores the original
demo clip/time after preview, without rerolling a target, advancing a turn, or applying any
combat side effects. Save or cancel an active pose edit before resuming. The existing demo
choreography remains unchanged; this is not a battle-sequence authoring interface.
The right status panel follows the current attacker, including Dragon animation details,
and reports the actual remaining turn time. Speed changes restart the Party demonstration.
Arin retains his thermal equipment fire. Orin's hammer glows white with crawling lightning;
only the shield perimeter receives the aura. His tab offers Thunder Smash, Storm Lance,
Chain Arcs and Godstorm styles, with full/reduced/off flash and shake. Reduced is the default.
The CPU charge controller and calibrated equipment sockets live in `OrinStorm.smile`.
Each Orin presenter has its own generation-safe context, charge latches, clip/time
history, handles, artistic style, visibility, trail, and first error. The scene owns one
Full/Reduced/Off comfort ceiling for every actor, so an actor style cannot silently change
another actor's accessibility policy. Shared Lightning textures, clock advancement, draw
pass, and shutdown also remain scene-owned. A paused forward/backward seek or clip cut
rebases the actor context so resume cannot replay stale thunder or discharge thresholds.
Each context may own at most four of Lightning's eight logical effects. Charge and impact
temporarily yield the decorative shield rim and second hammer arc, admitting up to three
high-priority battle arcs plus the primary hammer effect while leaving the other four slots
available to a second Orin. Requested/effective quality, effect counts and fallback cause
are observable. A GPU spark trail follows the hammer's swept path and fades in world space
after a normal swing. Hiding its hammer or owner destroys that context's trail immediately,
including while frozen, without affecting another actor. The slam does not retrigger during
follow-through. The white glow mesh uses the actor's final grounded transform.

The dragon opens the demonstration, alternating Fire Breath and Claw Strike on its turns.
Heroes guard before responding. Arin rotates through both sword attacks; Orin rotates
through Sword Attack, Jump Attack and Thor Attack. An extra boss-first guard beat occurs
periodically. This is bounded clip rotation, not a combat AI or randomized hero move picker.
The dragon's aim, fire and hit/KO reaction share the same target chosen once per turn.
The claw animation
includes a short approach and return. Idle wings, head and tail keep moving between turns;
party impacts trigger a brief hit reaction. This is a presentation demo, without combat damage
or enemy AI. The self-contained rig, six clips, original model, reference, descriptor and
checksums belong to `games/SinStarI/SourceAssets/Bosses/RedDragon/RedDragonV11`.

## Shared presentation libraries

- `Smile.Simple3D.Arena3D`: black floor and one emissive grid mesh, configurable dimensions, tile spacing, thickness and color. Viewer uses blue; Fire Lab uses orange and independently configured larger tiles.
- `Smile.Simple3D.StaticBackdrop3D`: load/select/clear/destroy a screen-fixed backdrop, shared with Fire Lab. No world plane or camera-driven positioning.
- `Smile.Simple3D.SceneVfx3D`: one per-scene Fire/Lightning advance boundary with independent family freeze and duplicate-frame rejection.
- `Smile.Simple3D.LightPool3D`: bounded generation-safe leases over scene-reserved renderer point-light slots.
- `Smile.UI.Controls`: matching panels, buttons, slider drawing, hover hit-testing and exclusive drag capture.

Equipment and owner visibility are evaluated before Fire/Lightning freeze. Hiding Arin's
sword or shield immediately destroys the matching emitters, clears glow trails and shuts
down the shield rim; showing the equipment while frozen cannot resurrect the old effect.
Hiding the dragon clears its breath, mouth/projectile Fire, glow and leased light before any
freeze return. Normal emission stop still allows accepted world-space tails to age out.

Dragon timeline seeks and explicit clip cuts clear incompatible breath/presence even when
Fire is frozen. Resuming after the animation moved rebases the current effect without
replaying skipped fireball impact audio. An unchanged frozen action retains its snapshot;
ordinary continuous impact playback still produces one cue. The existing isolated Viewer
fixture checks actual emitters, age/generation, another unaffected owner and cue submissions.
`DragonPresence.ImpactCueCount()` is a diagnostic of cosmetic cue requests, not audible
device playback or combat outcomes.

`ActorIsolationTests.smileproj` is the bounded real-render fixture for two instances of the
current Orin model. It exercises different clips, times, speeds, transforms, fixture-local
yaw corrections and styles; two independent storm contexts and local-light leases; shared
single-frame advancement; frozen hide/seek/resume; scene comfort; capacity rejection;
forced GPU fallback; context recreation; stale handles; and leak-free native/Web teardown.

These effects do not modify Arin's models, rig or animation sources. The canonical descriptor supplies mesh-derived sword endpoints and shield flame anchors; the rendered equipment transform, including calibration and decoupling, positions the emitters. Existing saved keys remain valid. The sword keeps a fiery outline under its flames; the shield's old golden overlay and glow trail are not drawn when the thermal equipment preview is available.

## Permanent character-workflow handoff

Before changing Arin's import/export, attachments, calibration or effects, read
`games/SinStarI/SourceAssets/Characters/Paladin/ArinV57/ARIN-CREATION-AND-REPAIR-JOURNEY.md`.
It distinguishes current behavior from historical experiments and explains the
Blender-to-SMILE pipeline. The separate free-roam demo remains deferred by Sin.

## September 5 mid-development checkpoint

The initial checkpoint used a full orbit and four camera poses. Subsequent visual tuning
replaces that with the front arc and two independently selected battle cameras described
above. Close framing and Dragon point-of-view composition remain under visual tuning.
Arin slash/crosscut and Dragon breath/claw/fireball now have original synthesized
attack cues. Orin's Block plays once and holds instead of repeatedly restarting.
Dragon's six-clip preview adds Fireball and stronger wing/arm/hit motion, eye anchors,
and idle mouth fire. Both effect Labs now live in `tools` beside this Viewer.

See `docs/implementation/party-battle-mid-development-2026-09-05.md` for the precise
checkpoint scope, validation and remaining requested work. This is not a final release.

### September 5: KO, shield comparison and camera diagnostics

Arin now has Death alongside his existing eight clips. Both heroes play Death
once and hold the final pose. On a Dragon turn, the whole party guards; randomly
one hero takes a fatal hit, finishes the fall, and revives for their own next turn.
Orin's guard never loops or restarts just because Party advances a stage.

Party presentation uses explicit Alive, Acting, Guarding, Hit, KO and Reviving states.
A hit is shown before a fatal actor enters KO, KO actors are excluded from guard/attack
updates and incompatible equipment effects, and their own next turn first transitions
through Reviving before normal movement resumes. These states are visual only and do
not apply damage, consume MP, award rewards or modify a game save.

Arin Shield switches between Ember Outline and the preserved Flames effect.
Freeze Fire affects either treatment; Freeze Lightning remains independent.

Battle cameras use stable actor homes/travel instead of bone bobbing or KO
height. They stay above the floor, keep clearance from both heroes, and cut at
shot-stage changes rather than moving through a character. A brief decaying
shake is reserved for Orin's ground smash and respects Flash/Shake Off.
Only the active hero enters an authored attack approach; full animated-model
bounds never rewrite those cinematic positions. Animated dragon camera sockets
fall back to stable arena anchors if a sampled socket leaves the scene envelope.
Below Camera, Party displays rendered position/target XYZ, yaw/pitch, FOV and
distance, so screenshots contain enough information to diagnose a bad angle.

### Saved JSON download (September 6)

The filename below the timeline identifies the current character's calibration.
**Download Key Frames**, beside **Import Key Frames**, exports the current saved JSON snapshot:
Web requests a browser download; native Windows uses a Save As dialog. This
explicit export button does not open Explorer or include temporary preview edits.
On native Windows, clicking the **filename** opens the canonical file's Explorer location.
On Web, clicking the **filename** downloads the current **saved** schema-2 JSON snapshot, not
temporary unsaved pose adjustments. All 20 channels and name-bound clips are
preserved. The label does not claim the browser can read or write `D:\`.

Downloads do not synchronize with the repository or another browser origin.
The native calibration synchronizer remains authoritative for desktop integration.
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
Rejected/unavailable working storage remains write-blocked; its existing recovery
workflow must be resolved first. There is no automatic cross-tab/process merge.
The shared export fixture validates both characters against canonical JSON and
the desktop binary serializer. An actual Edge Arin download also round-tripped
byte-for-byte through the native text import/export dialogs in an isolated sample.

### September 5: grounding, capture and saved-profile regression fixes

Orin's Jump Attack now lands for the kneeling smash/recovery while preserving its
launch and the accepted other clips. Its canonical package records the surgical Root
translation repair and hashes. Saved calibration fingerprints must match the repaired
model and cooked SM3D: stale fingerprints can let individual geometry draw yet reject
companions in Dragon/Party. The isolated native calibration test now seeds copies of
both current canonical saves and loads all four tabs before testing edits.

Solid-floor orbit is clamped at ground height in every tab; grid-only navigation still
allows below-floor inspection. A timeline drag keeps exclusive pointer ownership beyond
the window's left/right edges and through its release frame, preventing simultaneous pan.

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

### Tab switching and startup loading

Desktop and Web both opt into `Character3D.SetUnusedAssetCacheLimit(3)`. Changing
tabs still destroys the old actor instances, animation/pose state and scene VFX,
but up to three unused character assets can remain loaded: model geometry,
textures, materials and animation source data. Reopening a compatible character
reuses that asset and creates fresh independent actor/animator state. Cache keys
still include asset path, rendering profile and fallback variant. Eviction,
shutdown and renderer-reset invalidation remain bounded and explicit; the
default for other Character3D callers remains immediate last-owner release.
The tradeoff is retained memory (bounded by the chosen assets), not duplicate
actors. Setting the limit to zero purges unused retained assets. Resource
admission failure evicts unused entries before one retry.

The Web runtime also retains a bounded page-local encoded download cache, helping
repeated loads of backgrounds/effects without retaining their live instances.
A first visit can still take time to download, decode and prepare new assets.
Native and Web show a loading notice while changing tabs. No speed percentage
is promised without a measured comparison on the user's deployed connection.

All three tools' generated Web startup pages use the official logo derivative,
real asset activity/counts, creator credits and the Snake tutorial copyright
footer with new-tab links. The original branding PNG and full-fidelity character
textures remain unchanged. The loader does not represent tab-switch or GPU
preparation time as a false completed-download percentage.
