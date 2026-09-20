# Sin Star I

Sin Star I opens with **Characters**, **Battle Simulations**, **Legacy**, and **Exit**.
The title uses `Assets/Backgrounds/SinStarLandscape.png` with the transparent
`Assets/Sin Star I - Logo.png` lettering selected by Sin at upper left. The choices
remain grouped in the left menu area. Credits form one centered line across the
window, twenty pixels above its bottom, with no background panel.
The game-owned `Music.smile` keeps the current track playing through navigation
until a screen requests an override. Tracks in `Assets/Music` are:

- Title: `Starforge March (Title Screen).mp3`.
- Arin: `Bloom (Arin).mp3`.
- Orin: `Sunrise Oath (Orin).mp3`.
- Mira: `Golden Hour Ascend (Mira).mp3`.
- Kael: `Starforge Ascend (Kael).mp3`.

All three battle simulations use the Viewer's current four-hero party: Arin, Orin,
Zara and Mira. A status panel shows every hero and the selected boss with live state,
animation and speed. Dragon includes Zara's attacks, hit/KO/revival and water shields.
Kael starts at speed 200 in both his character presentation and battle simulation.
His hair now uses the canonical silver-gray material, with the sword visibility
and existing sixteen-clip rotation preserved. New fire attacks are available for
feedback in the separate native Fire Lab.

Kael's sixteen-clip model includes his quiet Breathing Idle, two normal attacks,
three Earth attacks and three Water attacks, shared with the Viewer.
Kael Party repeats **normal → Earth → Water**: Boulder Hurl / Water Whip,
Stone Volley / Serpent Orbit, then Fault Line / Tidal Surge, with a normal attack
before each pair. Normal attacks alternate between Attack and Attack2.
The character demo includes all sixteen clips. Both bending families hide the sword
and synchronize casting, target contact and audio; water uses the realistic Lab
preset and wraps sufficiently small targets. Hurl/Volley clear all loose ground
stones after lift-off, and their impact dust fades through its remaining lifetime.

Characters offers Arin, Orin, Mira, Zara, Valor, Dragon, Vrax, Yalis, and Kael. Each selection
runs that character's animation cycle with the Character Viewer's background,
rotating reflective arena, calibrated equipment, VFX and sounds. Battle Simulations
offers Dragon, Vrax and **Kael Party** choreography, including Mira's healing,
water attacks and Orin combo. Kael Party pits Arin, Orin, Zara and Mira against
Kael at three times his solo model scale. Earth and Water casts remain at his arena
home while facing the selected target; normal sword attacks still approach.
Legacy contains only the character gallery, Town, Town 2, and Back.
Mira's shared presentation no longer adds body glow, staff glow or head sparkles;
her authored model/materials and water attacks remain.

## Native build and launch

Run `games/SinStarI/Build.ps1` from PowerShell 7, then launch
`games/SinStarI/bin/Release/SinStarI.exe`. Use `-PrepareOnly` before building the
project directly in Visual Studio. The preparation reuses the Viewer's canonical
model, audio, VFX and calibration packaging. Tool-generated inputs under BuildAssets
and the named Assets subdirectories are ignored mirrors. The nine character models
are cooked by the normal compiler; preserved Mira comparisons are not published.
All inputs and dependencies are local. Web adoption remains on hold.

## Focused validation

`scripts/test-sin-star-presentation.ps1` compiles the actual game presentation
module against all nine packages, opens all twelve entries, draws the first frame
before the ordinary update loop, advances another frame, and verifies renderer
object/animator/particle/ribbon cleanup after each exit. It also observes private
Viewer load failures in a disposable source copy so a partial load cannot pass
just because later calls clear the renderer's last-error value. All three battle
entries check their exact four-hero roster, and Kael's solo and Party entries
check speed 200; Kael Party also checks its boss identity.

The shared loader acknowledges the character cache's one-time renderer-reset
signal before setting the next scene's cache policy. Session construction is
explicit in `Enter`, and the camera is prepared there before the menu shell's
first draw. Native checks cover these demonstrated integration failures. The
remaining module-declaration initializer defect is recorded in the
[language reference](../../docs/language/README.md#open-native-module-initializer-defect).

Native manual validation observed the exact four-item main menu, the prior seven-character
submenu, Mira's animation/water presentation, both running battle simulations,
the four-character Vrax roster, pause/pan/zoom/reflection/reset controls, Back and
Escape navigation, Legacy's sprite gallery and original 3D preview, and normal
Exit. The automated native fixture now additionally opens and releases Yalis. The
existing 58 native graphics/input/audio-focus checks and architecture
guards also pass. The shared camera and battle algorithms were reused; this was
a focused host-integration check, not an exhaustive replay of every effect.

## Architecture

`Program.smile` deliberately owns the game window, main loop, scene transitions,
and thin scene routing. `SinStarI.TitleScreen` remains an application-local Module:
there is one title-screen service with private assets and selection state, so a
Class would add identity without a useful second instance.

The module exports the typed `TitleAction` enum. Retained scenes keep their explicit
values: `None=0`, `Character=1`, `Town=2`, `Town2=3`; retired values 4–6 are unused.
Navigation uses explicit enum transitions rather than enum arithmetic. Yalis is
value 17, Kael is 18, and KaelBattle is 19.

`TitleScreen` owns its three submenu states, keyboard/pointer selection and requests
the title track from `BackgroundMusic`, the sole music-state owner.
`CharacterPresentation` owns one hosted `Character3DViewerWorkflow.Session`
and game-facing Back/status controls. It links the exact Viewer sources, as Studio
does, rather than copying choreography, lighting, calibration, or VFX algorithms.
The shared session's `CycleAllClips` option selects the gallery's full clip cycle;
battle simulation entries use the Party Dragon, Party Vrax and Kael Party tabs.
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
- Every scene: Escape returns to its submenu and selects title music; an already
  playing title track continues without restarting.
- Characters/Battle Simulations: left-drag pans, middle-drag orbits, wheel zooms,
  O toggles auto-orbit, Space pauses/resumes, F toggles Floor, G toggles Grid, B cycles the shared Viewer backgrounds, and R or
  right-click resets the view. The Back button returns to the submenu.
- Character: 1-4 or Tab selects the manual preview; arrows/WASD move; Space
  toggles its walk/run sheet.
- Town: arrows/WASD move; 1 and 2 switch the visible character.
- Town 2: arrows/WASD take manual control; hold Space to run as Character 1;
  Enter starts or pauses the edge tour; 1 and 2 switch characters.

Screen images and scene resources are released by their owning Modules during
shutdown. The music owner stops playback only on a track change or game shutdown.

## Content

The [story package](Story/Start%20Here.md) contains the visual storyboard, complete
v0.2 narrative script and accepted D01-D16 canon, including Sin's clarification
that prosecuting Orin's father would expose deeper military corruption. These
are narrative/art drafts and accepted story decisions, not playable encounters.

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
