# Sin Star I

Sin Star I opens with **Characters**, **Battle Simulations**, **Legacy**, and **Exit**.
Characters offers Arin, Orin, Mira, Zara, Valor, Dragon, and Vrax. Each selection
runs that character's animation cycle with the Character Viewer's background,
rotating reflective arena, calibrated equipment, VFX and sounds. Battle Simulations
offers the existing Dragon and Vrax party choreography, including Mira's healing,
water attacks and Orin combo. Legacy preserves the earlier character gallery,
Town, Town 2, Shop, Dungeon, and Original Battle Preview.

## Native build and launch

Run `games/SinStarI/Build.ps1` from PowerShell 7, then launch
`games/SinStarI/bin/Release/SinStarI.exe`. Use `-PrepareOnly` before building the
project directly in Visual Studio. The preparation reuses the Viewer's canonical
model, audio, VFX and calibration packaging. Tool-generated inputs under BuildAssets
and the named Assets subdirectories are ignored mirrors. The seven accepted models
are cooked by the normal compiler; preserved Mira comparisons are not published.
All inputs and dependencies are local. Web adoption remains on hold.

## Focused validation

`scripts/test-sin-star-presentation.ps1` compiles the actual game presentation
module against all seven packages, opens all nine entries, draws the first frame
before the ordinary update loop, advances another frame, and verifies renderer
object/animator/particle/ribbon cleanup after each exit. It also observes private
Viewer load failures in a disposable source copy so a partial load cannot pass
just because later calls clear the renderer's last-error value.

The shared loader acknowledges the character cache's one-time renderer-reset
signal before setting the next scene's cache policy. Session construction is
explicit in `Enter`, and the camera is prepared there before the menu shell's
first draw. Native checks cover these demonstrated integration failures. The
remaining module-declaration initializer defect is recorded in the
[language reference](../../docs/language/README.md#open-native-module-initializer-defect).

Native manual validation observed the exact four-item main menu, seven-character
submenu, Mira's animation/water presentation, both running battle simulations,
the four-character Vrax roster, pause/pan/zoom/reflection/reset controls, Back and
Escape navigation, Legacy's sprite gallery and original 3D preview, and normal
Exit. The existing 58 native graphics/input/audio-focus checks and architecture
guards also pass. The shared camera and battle algorithms were reused; this was
a focused host-integration check, not an exhaustive replay of every effect.

## Architecture

`Program.smile` deliberately owns the game window, main loop, scene transitions,
and thin scene routing. `SinStarI.TitleScreen` remains an application-local Module:
there is one title-screen service with private assets and selection state, so a
Class would add identity without a useful second instance.

The module exports the typed `TitleAction` enum. Its explicit values preserve the
existing scene contract: `None=0`, `Character=1`, `Town=2`, `Town2=3`, `Shop=4`,
`Dungeon=5`, and `Battle=6`. Navigation uses explicit enum transitions rather
than enum arithmetic. New action values are appended; Legacy keeps values 1–6.

`TitleScreen` owns its three submenu states, keyboard/pointer selection and title
music. `CharacterPresentation` owns one hosted `Character3DViewerWorkflow.Session`
and game-facing Back/status controls. It links the exact Viewer sources, as Studio
does, rather than copying choreography, lighting, calibration, or VFX algorithms.
The shared session's `CycleAllClips` option selects the gallery's full clip cycle;
battle simulation entries use the existing Party Dragon and Party Vrax tabs.
Editor UI and pose-save commands are not exposed by the game host. Packaged pose
defaults load in Sin Star I's own application namespace; Viewer saves are not
written by the game. Exiting a presentation releases its actors, effects and input
captures before returning to the retained submenu and restarting title music.

## Controls

The Battle Arena Preview uses the shared precise camera controller and the existing
renderer/actor owners. Pan, orbit, zoom and auto-orbit retain fractional world units
and degrees until GPU float32 acceptance. Imported asset scale, actor grounding
rules, IDs, scene transitions and saved calibration remain unchanged.

- Menus: click an item, or use Up/Down/W/S and Enter/Space. Escape or Back returns
  from a submenu; Exit closes the game after normal resource shutdown.
- Every scene: Escape returns to its submenu and restarts title music.
- Characters/Battle Simulations: left-drag pans, middle-drag orbits, wheel zooms,
  O toggles auto-orbit, Space pauses/resumes, F toggles reflections, and R or
  right-click resets the view. The Back button returns to the submenu.
- Character: 1-4 or Tab selects the manual preview; arrows/WASD move; Space
  toggles its walk/run sheet.
- Town: arrows/WASD move; 1 and 2 switch the visible character.
- Town 2: arrows/WASD take manual control; hold Space to run as Character 1;
  Enter starts or pauses the edge tour; 1 and 2 switch characters.
- Battle Arena Preview: click `Battle Floor: Reflective / Original` or press R to
  toggle the shared floor; left-drag pans, middle-drag orbits, the wheel zooms,
  O toggles the default smooth auto-orbit, and Escape returns to the title.

Title music plays only while the title is active. Opening a scene stops it, and
returning with Escape restarts it. Screen images and scene resources are released
by their owning application-local Modules during shutdown.

## Legacy Battle Arena Preview

`SinStarI.BattleArenaPreview` replaces only the former Battle placeholder. It owns a
small shared `Arena3D`, the existing screen-fixed title backdrop, one live animated
and socket-grounded Arin v5.7 actor, smooth shared camera controls, a visible
reflection preference, and bounded lifecycle. Both
native and Web builds use the same `Smile.Simple3D.Graphics3D` planar-reflection
implementation as the Character Viewer. This preserved Legacy module imports no Viewer module and owns no
GPU reflection algorithm, calibration editor, battle authority, damage model, or scene
schema. Leaving the preview destroys its actor, arena, and backdrop; re-entry creates a
fresh scene with reflections requested On.

## Content

The project publishes its accepted PNG, MP3, and `.smilemap` content through the
existing recursive asset rules. `CONTENT_PIPELINES.md` records the reusable town
and character authoring workflows; visual asset revision remains a separate,
reviewed art task.

## Paladin Combat Presentation Lab

`PaladinCombatLab.smileproj` is a separate technical presentation program for the
candidate Arin v5.4 character. It automatically cooks the repository-owned GLB
and descriptor, enumerates the model's exact clips, events, and sockets, and
exercises Ready, Run, Sword Attack, Shield Bash, Defend, Block Impact, Hit, KO,
and Victory without implementing battle authority. Authored events align VFX,
transient light, hit-stop, and caller-owned audio cues; they never authorize
damage. The candidate remains separate from Sin Star I release content until the
production texture, provenance, visual-acceptance, and explicit user gates pass.
