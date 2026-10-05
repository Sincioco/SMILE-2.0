# Battles and Beat Cameras

[Start here](../README.md) · [Characters and poses](characters-and-calibration.md) · [Status](status.md)

## Current presentations

**Battles** offers Party Dragon, Party Vrax and Kael Party, each with Arin, Orin,
Zara and Mira. Their Alive/Acting/Guarding/Hit/KO/Reviving states are presentation
states without damage, MP or rewards. **Battle System** is the playable orders,
stats and reward simulation described below. The town is the launch view; older
Party/Battle startup statements and two/three-hero rosters are historical.

All use the shared actors, canonical poses, equipment/effects and camera owners.
Timeline preview pauses the current actor without advancing live turns or effects
with combat consequences. Pose editing applies to Arin/Orin; save/cancel before
resuming. Selecting actors for Beat cameras is independent from whose turn plays.
The existing [camera checkpoint](../../../docs/implementation/party-beat-camera-checkpoint.md)
retains diagnostics and development evidence. Its separate-Studio/old-startup
statements are historical; do not use it to restart the abandoned S1 project.

## Native Battle System

The **Battle System** tab uses the existing Kael
Party arena, actors, accepted poses, equipment and effects. Arin, Orin, Zara and
Mira start at level 1 against level-20 Kael (Zara's stat growth). The ordinary
Kael Party tab retains its demo. Yalis and the three older Mira comparison tabs
are hidden in the native tab strip; the accepted Mira follows Zara.

- Initial orders select **Auto** after 30 seconds without keyboard, pointer-motion,
  mouse-button or wheel activity. Activity starts a fresh 30-second countdown.
  This also applies after a battle reset. Open statistics, inspectors or pending
  edits suspend the countdown. Later orders explicitly requested during combat
  still wait for confirmation.
- Confirm Arin, Orin, Zara and Mira in order;
  no action resolves until all available heroes have orders. **Fight** confirms
  the remembered order (Attack initially). **Order** opens attack variants, magic
  and healing targets, the Item placeholder, Defend, full-gauge Limit Break or Run.
- **Auto** starts immediately using confirmed orders and the remaining remembered
  orders. After any started round, the whole party acts, then Kael, then the
  same orders repeat automatically. Click a Battle System panel, or press Enter,
  to request new orders after the current party-and-boss round finishes.
  Camera pan/orbit/zoom, scene clicks and other keys leave Auto running. Defeated
  or escaped heroes are skipped. Illegal repeat orders fall back to Attack.
- Arrows choose; Enter confirms; Escape goes back. Menus also accept clicks.
  **Space** pauses/resumes battle playback like Kael Party, preserving Auto and
  current orders. Combat, animation, initial-idle and ending/restart clocks wait
  while paused; pan/orbit/zoom and C's eased camera return remain available.
  Backtick cycles the three legacy panel visibility states, then a fourth Battle
  view hiding the header and tabs, without stopping Auto. Battle System starts
  in this fourth view, with Kael's info, HP and stats buttons at an 18-pixel top
  margin. Another backtick restores the full editor layout. The ordinary character
  tabs keep their existing three-state cycle.
  Clicking battle panels or statistics requests orders at the round boundary;
  inspector editing waits for that boundary. Close inspectors with backtick to
  resume normal battle presentation. Save/cancel pending pose or Beat edits first.
- The five panels use the shared 80%-opacity navy style and remembered-order
  icons. With all editor panels hidden, the four hero panels widen and move down;
  the centered Fight/Order/Auto panel stays 166 pixels wide. Kael's left-aligned
  info and HP bar span the same available width with 18-pixel outer margins.
  Showing the editors restores the narrower layout. HUD drawing and clicks use
  the same layout, including each Stats Table/Graphs button.
- A floating red crystal pointer with shaded facets, luminous edges and a pulsing
  aura marks the hero receiving orders. It is hidden throughout attack execution
  and after battle; the active hero's card remains highlighted. Damage/healing
  numbers rise and fade above their targets.
- Attacking fighters face their destination during approach and return, then
  restore their accepted attack/standing facing on arrival. Arin's authored
  equipment corrections and glow turn with him; Orin's standing hammer stance
  is restored after travel. Saved poses and character packages are preserved.
- The right panel has **Battle** and **Inspector** tabs. **Cinematic Camera Battle**
  starts on and uses Kael Party's existing formation framing and attack cameras.
  Hero attacks hold the same wide view behind the party; Kael uses the same
  behind-defender shot. The former separate Battle System close-up/zoom formulas
  are removed.
- The opening uses Kael Party's formation-wide view and complete, eased two-second
  360-degree revolution, then blends for one second into Sin's default view behind
  the party: position `(623, 0, 382)`, target `(-67, 122, -66)`, FOV 24 degrees.
  **Choose Party Action** and individual orders stay in that view with no automatic
  rotation. Returning to orders restores it once. Right-click and automatic
  restart replay the opening.
- **C** eases from the current camera back to that default orders view over one
  second, restoring its position, angle and zoom without stopping Auto Battle.
  It holds the default framing across attacks; pan/orbit/zoom remain available,
  including during the transition. Turn cinematics off/on to resume attack shots.
  C also works with cinematics off and can end the result camera's orbit without
  changing rewards or the battle restart timer. Active editors and stats retain
  their input ownership.
- Left drag pans, middle drag orbits and the wheel zooms during both hero and enemy
  actions. Pan/orbit take over the current shot; later camera updates retain the
  manual view. Zoom uses the same smooth controls and preserves its target across
  shot changes. Arrow keys also orbit during execution; during orders they navigate
  menus. Turn cinematics off to retain the shot, or off/on to resume automatic
  framing. Active pose edits and Beat previews retain their existing ownership.
  The twelve-second ending orbit and fifteen-second battle restart remain in place.
- Each hero and Kael has **Stats Table** and **Graphs** buttons: per-level values
  or increases, XP, individual graphs and normalized/actual-value overlays,
  with levels 1–300 or 1–9,999. Kael's last column/graph is **EXP Reward**.
- Right-click restarts the encounter with full HP/MP while retaining earned EXP
  and levels for this running Viewer session. Returning to Battle System also
  retains that progress. Closing the application ends the session.
- Victory or defeat automatically restarts the encounter after 15 seconds with
  the same full-heal, retained-EXP behavior, returning to initial orders. Reward
  dialog activity or dismissal does not postpone it. Pending editor changes
  retain the existing save/cancel protection.
- Defeating Kael awards EXP once to each hero who has not escaped (including KO
  heroes). A navy reward dialog counts up EXP and levels with a short tick sound.
  Mouse/keyboard activity resets its ten-second inactivity timeout; Escape,
  Enter, **OK** or **X** closes it. Closing early does not lose the award.
- Victory and defeat each finish with a twelve-second, eased 360-degree camera
  orbit. Living winners play their own Victory clips; defeated actors retain
  Death. Arin, Zara and Mira now have rig-matched Mixamo Victory clips alongside
  Orin's existing clip. Right-click can restart during the ending.

The Viewer owns its curved stat/reward policy and reuses the game's existing XP
thresholds. For level `L`, let `g = (L - 1) + floor((L - 1)^3 / 900)`.
The table shows each stat's level-1 value plus its weighted growth points:

| Hero | HP | MP | Attack | Magic | Physical Def | Magic Def |
| --- | --- | --- | --- | --- | --- | --- |
| Arin | 240 + 30g | 55 + 10g | 40 + 4g | 30 + 3g | 50 + 5g | 42 + 4g |
| Orin | 270 + 40g | 40 + 10g | 50 + 5g | 22 + 2g | 40 + 3g | 32 + 3g |
| Zara / Kael | 250 + 40g | 70 + 15g | 48 + 5g | 32 + 3g | 30 + 2g | 30 + 2g |
| Mira | 220 + 40g | 130 + 25g | 23 + 2g | 52 + 5g | 35 + 3g | 45 + 4g |

Priority units are 10 HP, 5 MP, 1 Attack or 1 Defense per growth point. This smooth
accelerating curve follows the supplied visual reference rather than claiming
exact values from that unlabelled image. Kael rewards `160 + 40g` EXP per eligible
hero: 1,200 at level 20. For XP costs only, let `n = L - 1`. XP to the
next level is `60 + 20n + 3n²` through level 299; from level 300 onward it becomes
`25 × normal cost + 500,000 × (L - 299)²`. Level 9,999 is the maximum. Closed-form
totals and bounded graph sampling keep inspection responsive.

LB accumulates actual HP lost up to maximum HP; its power is `6 × Attack + 5 × L`
(Mira uses Magic for healing). Defend adds 300% to physical and magic defense
through Kael's response: four times the base, expiring at the next round. Run is
individual, starting at 50%, reaching 100% at 20 levels above the enemy.

Build/launch: `pwsh -File tools\Character3DViewer\Launch.ps1 -Build`.
Focused native regression: `pwsh -File scripts\test-viewer-battle.ps1` after asset
preparation. No .NET/VSIX rebuild is required for this source-only slice. Web work remains paused; native Studio is the active host.

The standalone **Kael Party** tab loops `Starforge Ascend (Kael).mp3` from
Sin Star I's canonical music assets. Scene resets keep the song playing; selecting
another tab or closing the Viewer stops it. Hosted presentations leave music with
their game. Native builds deploy the MP3 beside the application.

## Party Beat Cameras

In **Party Dragon** or **Party Vrax**, press **Tab** to cycle visible combatants,
or click an actor without dragging. The right panel identifies the selected actor
independently of whose turn is playing. Dragon and Vrax have their own four shots.
Clicks first test reusable head/body regions, then complete actor bounds. This avoids
extended wings/limbs stealing nearby hero clicks; it is approximate selection, not
pixel-perfect mesh picking. Tab disambiguates overlapping characters. A click keeps
the current camera; dragging begins panning after a small movement threshold.

Choose **Beat 1–4** to pause and edit that beat's starting composition with middle
drag (orbit), left drag (pan), wheel (zoom), or arrow keys (orbit). Enter resets the
draft to its opening composition. Right-click exits Beat Edit and resets the current
Party tab as it does outside the editor. **Save** stores the selected
character's entire camera sequence draft and keeps Beat Editor open at the current
shot and playhead. **Cancel** closes Beat Editor and restores playback without saving
the camera. Save or cancel a draft before
switching characters/tabs. An asterisk marks a saved beat.

**Copy** copies one camera's composition, motion and connection. Select any character,
open the destination beat and **Paste**, then Save. Head definitions remain owned by
the destination character. **Motion** cycles Stationary, Orbit Left/Right (20 degrees
over the beat), and Zoom Forward/Backward (a 20-percent dolly). Stationary means no
added cinematic motion: the shot still follows its attacker/target reference frame.
**Connect** cycles Cut, To Next and From Previous. A connection becomes active when
its neighboring shot has been saved. A reciprocal connection traverses the shared
boundary once. Unauthored beats continue using the existing battle camera policy.

During Beat Preview/Edit, **Beat Sequence** replaces the normal animation timeline in
the same bottom position, without a separate backing panel. Closing Beat Preview
restores the normal animation timeline. Its Desktop controls match that timeline:
**0-Frame**, **< Beat**, **Beat >**,
**< Frame** and **Frame >**. Beat navigation visits main and extra markers in time
order. Frame buttons step sequence time at the attacker's clip sample rate; holding
one repeats. **Space** plays/pauses from the playhead, stops at the sequence end,
and restarts from zero when pressed there. Pausing selects the shot under the playhead.
Scrub, then pan/orbit/zoom or use arrow keys to edit from the displayed view.

Drag a cyan main marker (2–4) to change adjacent camera durations. Beat 1 starts at
zero and the total battle duration stays fixed. **Camera timing only** changes;
animation, movement, impacts, audio, VFX cues and gameplay counters keep their
existing schedule. The Dragon's four segments cover anticipation, wind-up,
attack/impact and recovery within its existing attack clip. Vrax and the heroes
retain their existing approach, attack and return timing. Preview samples those
existing actions without playing audio or advancing the live battle's turn.

Use **Add Shot** between markers for up to 16 extra camera shots. Green markers
can move between main beats; **Delete Shot** removes only extra shots. Original
Beats 1–4 cannot be deleted. Motion and connections use each shot's interval, including
extra shots. Changing main durations moves their contained extra shots proportionally.
Markers require a little space from neighbors when inserted/moved (16 milliseconds).
The same spacing is checked after final millisecond rounding. A retime that would merge
an extra shot with a main or neighboring marker is rejected without moving or deleting
the authored shot; move that extra marker and retry. If changed action timing makes a
stored sequence inapplicable, Preview reports the fallback and uses the four safe main
markers without overwriting the stored bytes.

**Reset Beat** restores the current main beat's built-in camera, motion and
connection policy, retaining timeline timing and extra shots. From an extra shot it
selects and resets the containing main beat. **Reset All Beats** restores all four
built-in cameras and default timing and removes extra shots from the draft. Both
wait for **Save**; **Cancel** preserves saved sequences. Neither changes head cuboids,
poses, another character's settings or battle action timing. Built-in defaults remain
dynamic camera-policy fallbacks, rather than a frozen snapshot of the current view.

**Reset All Beat Lengths** restores only the default four camera durations. It
preserves all camera compositions, motion/connections and extra shots. Extra shots
keep their proportional positions within the restored main beats. Save applies the
timing reset; Cancel preserves the previously saved lengths.

**Preview Beats** opens the timeline without editing; press it again to close.
Timing/shot edits require Beat Edit mode. The new timeline controls are validated
and delivered on Desktop first. Web adoption and publication are on hold; the
[checkpoint](../../../docs/implementation/party-beat-camera-checkpoint.md) records the
shared source changes and browser follow-through still required.

Cyan **Attacker** and yellow **Target** cuboids start at each actor's Head socket;
missing sockets use an upper-bounds estimate. Drag a cuboid to offset its center in
the current camera plane. Orbit to adjust another direction. Select **Head Box:
Attacker/Target** and use independent Width, Height and Depth buttons to resize it.
Offsets and dimensions auto-save per character when released/adjusted and are reused
across beats and attacker/target roles; Cancel only cancels camera changes. Centers
follow animated head sockets; cuboid axes and offsets follow the actor's facing.
The cuboid is a framing reference, not collision geometry or an animation edit.
Inspector scrolling changes only the panel-control coordinate. Picking and dragging
either cuboid continue to use the same raw viewport pixels, so a held pointer does not
move the head when the panel is scrolled.
If a head save fails, the character-specific warning remains visible after camera Save,
Preview close and actor navigation. Use **Retry Head Save** after correcting the storage
problem, or **Discard Head** to restore only that character's last-persisted cuboid while
leaving the camera draft in place for review. Native X/Alt+F4 is deferred while that
recovery remains pending, so resolve the visible warning and close again.

Camera position and aim use Double coordinates relative to the two head centers,
with lateral distance, height above their baseline and fractional horizontal distance
along it. Horizontal distance remains unbounded for wide and rear camera placement.
Its vertical influence is limited to the span between the heads, so a distant camera
does not amplify animated head-height changes into a vertical flight. The coordinates
still adapt to formation translation, rotation and changed separation. Near-coincident
heads use a deterministic one-unit forward baseline. Render submission retains the
existing Precision3D boundary and GPU float precision limits. Linked cameras follow a
stateless bounded arc between viewing directions. Exactly opposing views use one stable
great-circle plane, and a parallel up direction is repaired before native submission.

Camera sequences and head cuboids use separate checksummed **Save Data** records under the
Viewer ApplicationId (`BattleCamera.Sequence.<Character>.V2` and
`BattleCamera.Head.<Character>`). The sequence stores all timing weights, markers and
shots in one envelope. Newly captured cameras use the stable `SMILE-Shot-2` frame.
Ordinary `SMILE-Shot-1` cameras retain their original coordinates; legacy cameras
with an extreme longitudinal fraction fall back to the built-in camera and are
recaptured as version 2 when edited. If no valid V2 sequence exists, the older
`BattleCamera.<Character>.Beat1` through `Beat4` records are read as defaults;
they are not deleted or overwritten. A saved V2 reset intentionally takes precedence
over older custom shots. Native storage and each Web origin are independent;
this slice does not transfer camera saves between them. Pose-calibration JSON,
canonical models and gameplay counters are not camera storage.

## Ownership and limitations

The [architecture source map](../ARCHITECTURE.md#party-beat-camera-ownership) owns
sequence persistence, frame order and implementation boundaries. Camera saves
are not calibration JSON and do not modify accepted models. Exact old camera
coordinates/layout evolution remain in [September evidence](archive/2026-09.md#party-beat-cameras).
Open recovery and rejected-storage issues are tracked in [status](status.md).
