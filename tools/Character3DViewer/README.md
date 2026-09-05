# SMILE 2.0 - 3D Viewer, Animation Editor

Native-first reusable inspection and lightweight pose-correction tool. Party is the launch default. The Character tabs select Arin, Orin, or the Party arena. Profile retains Arin v5.6, the earlier prototype, and the technical fixture for diagnostics. Web editor feature work is deferred.

The editor source and build/launch entry points belong here. Sin Star I owns the self-contained character package at `games\SinStarI\SourceAssets\Characters\Paladin\ArinV57`. Orin owns `games\SinStarI\SourceAssets\Characters\Tank\OrinV13`. Do not edit ignored cooking inputs as canonical character assets.

## Build and launch

Run `Build.ps1`, then `Launch.ps1`. The regular launcher defaults to `bin\Character3DViewer.exe`. The active Debug development build is `bin\Debug\Character3DViewer.exe`; pass that absolute path with `Launch.ps1 -Executable` to launch it. The launcher exports live calibration, closes old instances, preserves/restores the stable working copy and watches both characters’ saves to mirror them into their separate canonical repository JSON files.

## Inspection

- Backtick cycles through panels hidden (including Pose Calibration), all UI hidden, then the prior UI restored. Headers, the timeline, and helper text remain after the first tap. Hidden controls cannot intercept the mouse. This does not change panel-open preferences, edits, playback, or the camera. Right-click reset restores the normal UI with Pose Calibration hidden.
- Space pauses/resumes movement while keeping camera controls active.
- Flames keep animating when Space pauses the scene. The separate Pause Flames / Play Flames button controls only the flames, independently of scene playback. Reset starts both again.
- Right-click resets presentation as on a fresh launch: Idle, Demo, dragon/floor/grid visible, landscape backdrop, unpaused. There is no inactivity timer that re-enables Demo.
- Left drag pans the view; middle drag orbits; wheel zooms smoothly.
- Zoom extends to -144 for glove and grip inspection. Beyond the former -48 limit, it moves the camera closer to the current panned anchor, reaching one tenth of the former distance. Pan the glove toward the center, then zoom in; the arena size and character pose are unchanged.
- H Orbit, V Orbit and Zoom support hover-wheel adjustment and capture slider drags until release, even outside the track. Vertical orbit supports 360 degrees.
- Arin and Orin start at speed 200. Their individual demos target three seconds per sequence and let an in-progress animation finish before advancing. Orin Block plays once and holds its final pose. Selecting an animation disables Demo; Block remains a one-shot.
- D toggles the dragon; W toggles the current character’s weapon; S toggles shield. Hiding the dragon does not shrink the arena.
- B/BG cycles colors and two static bitmaps. The default is the Sin Star I landscape without its title.
- Floor / Grid hides/shows both. Profile, Glow, Socket, Channel and lighting controls remain available.
- Pose shows/hides Pose Calibration, which is **hidden at startup and reset**.
- Sword Fire and Shield Fire independently toggle the default-on thermal effects on Arin v5.7. W/S also hide the corresponding fire with its equipment. The sword has a fuller orange flame and world-space lingering trail; the shield uses much smaller flames instead of the old solid golden glow.
- Normal animation loop wraps and clip changes retain existing fire particles until they fade; source velocity inheritance is zeroed for the transition update so the pose reset does not launch particles across the gap. Editing a paused pose clears stale emission. Explicit right-click reset clears the effects for a fresh start.
- G0 clarification: the retained-tail clip-change behavior applies to automatic Demo advancement. Explicit clip selection/navigation is a cut that clears/reseeds visual history; it is not a corrected-pose cross-fade.

The timeline supports drag scrubbing and hover-wheel single-frame steps. `0-Frame` jumps to the start; `< Key` / `Key >` jump between saved corrections; `< Frame` / `Frame >` step immediately on click and repeat one frame every 300 ms while held. Release stops repetition; reset or hiding the UI cancels it. Holding captures the gesture so it cannot pan the camera; a delayed update never catches up with a burst of frame steps. Drag a green keyframe tick to move it. These controls do not toggle pause.

## Pose corrections

Select Weapon Wrist, Shield Wrist, Weapon or Shield, an axis, and Rotate or Move. The slider and hover-wheel edit the selected channel; the former -5/+5 buttons are removed. Decouple switches allow each equipment item to retain its base animation while its wrist is corrected independently.

The viewport uses Blender-compatible transform controls. Red X, green Y and blue Z handles appear at the selected wrist or equipment socket. Drag an arrow to move equipment or drag a colored ring to rotate the selected target. `G` starts Move for equipment, `R` starts Rotate, and `X`, `Y`, or `Z` constrains the active axis. `Enter` accepts the current drag and saves an active edit; `Esc` or right-click cancels it. The `E` key is also accepted as a classroom-friendly Rotate alias.

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
available. Weapon and Shield affect both members. Select the individual Character tab to
edit that character’s animation; Party evaluates each member’s own saved correction track.
The right status panel follows the current attacker, including Dragon animation details,
and reports the actual remaining turn time. Speed changes restart the Party demonstration.
Arin retains his thermal equipment fire. Orin's hammer glows white with crawling lightning;
only the shield perimeter receives the aura. His tab offers Thunder Smash, Storm Lance,
Chain Arcs and Godstorm styles, with full/reduced/off flash and shake. Reduced is the default.
The CPU charge controller and calibrated equipment sockets live in `OrinStorm.smile`.
Forked Judgment converges four Ultra-quality sky strikes on the raised hammer; Thunder
Smash sends eight ground spokes from the impact point. A GPU spark trail follows the
hammer's swept path and fades in world space after the swing. The slam does not retrigger
during follow-through. The white glow mesh uses the actor's final grounded transform.

The dragon takes the third turn, alternating Fire Breath and Claw Strike. Mouth-attached
fire sweeps toward Arin and then Orin while they guard and react. The claw animation
includes a short approach and return. Idle wings, head and tail keep moving between turns;
party impacts trigger a brief hit reaction. This is a presentation demo, without combat damage
or enemy AI. The self-contained rig, six clips, original model, reference, descriptor and
checksums belong to `games/SinStarI/SourceAssets/Bosses/RedDragon/RedDragonV11`.

## Shared presentation libraries

- `Smile.Simple3D.Arena3D`: black floor and one emissive grid mesh, configurable dimensions, tile spacing, thickness and color. Viewer uses blue; Fire Lab uses orange and independently configured larger tiles.
- `Smile.Simple3D.StaticBackdrop3D`: load/select/clear/destroy a screen-fixed backdrop, shared with Fire Lab. No world plane or camera-driven positioning.
- `Smile.UI.Controls`: matching panels, buttons, slider drawing, hover hit-testing and exclusive drag capture.

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
