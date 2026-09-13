# SMILE 2.0 - 3D Viewer, Animation Editor

## Mira comparisons (native)

The **Dragon** tab now includes **Arin, Orin and Mira**. Its Demo runs the same battle
choreography as Party Dragon, opening with the Tempest cycle so Orin's Thor Attack
and Mira's water storm combine during the first round. Later rounds cycle through
her torrent, both heals, waterball and tsunami. Space pauses the entire scene;
Demo Off restores independent Dragon clip inspection. Both expanded tabs start
with wider arena framing and show the acting character and turn countdown.

**Mira** is the new Tripo healer and appears immediately before **Mira1**, the preserved
Pixal3D healer. **Mira2** loads the independent TRELLIS.2
candidate, and **Mira3** loads Hunyuan3D-2.1 shape/paint with a refined face texture.
All four have nine animation choices. Use **Weapon** or **W** to hide/show each
character's own staff. The **Mira** tab uses Arin's arena view with **Dragon** and a
separate, calibrated **Arin** companion. **Demo On** runs Arin and Mira's attacks,
guards, hit reactions, healing and Dragon counters. **Demo Off** or selecting a clip
returns to Mira inspection with idle Arin. **Dragon** or **D** toggles the opponent. Her attack previews target
Dragon, **Heal One** targets Arin, and **Heal Party** includes Arin and Mira.
Mira1/Mira2/Mira3 remain solo comparisons. Mira2/Mira3 each have a closed 19,652-triangle equipped surface,
baked textures and measured floor corrections. This is an appearance and animation
comparison. New Mira is the current
healer in both Party battles, cycling Torrent, Heal One (Arin), Heal Party, Waterball,
Tsunami and the Mira/Orin Tempest combo with blue water VFX and sound. The staff glows
while equipped, small shimmering particles circle only its head/crystal, and a body
aura rises during a cast. Boss attacks meet small, subtle
water shields in front of individual party members. At impact the struck shield
gives slightly, then recovers; impact droplets spread across it and disperse around
its rim. No shield encloses the whole
party. Waterball contact adds a splash and brief
recoil. The native Water Lab exposes all seven effect previews independently.
Tab/click selects her Beat Camera. Healing previews
existing presentation states; no Sin Star I game code or health system is changed.
Mira1 now has a cleaner 4K reference bake, repaired waist cape, closed staff and baked
free-left-hand casts/staff hold. Its original comparison source is preserved. It has
19,738 equipped triangles; detailed native visual inspection of the repair is pending
after an Escape stop in Computer Use. The native build and focused regressions pass.
Canonical packages and current limitations are in
`games/SinStarI/SourceAssets/Characters/Healer/MiraV1/README.md` and
`games/SinStarI/SourceAssets/Characters/Healer/MiraV2/README.md` and
`games/SinStarI/SourceAssets/Characters/Healer/MiraV3/README.md`.
The Tripo package is `games/SinStarI/SourceAssets/Characters/Healer/MiraTripoV1`:
19,782 equipped triangles, 4K body/2K staff PBR maps, nine clips and nine sockets.
Its cape, nose/eye surfaces, staff grip, back carry and Death drop are authored in
the package. Native model inspection, both six-cast Party fixtures and seven-mode
Water Lab checks pass; the package README records validation and remaining delivery.
All Mira tabs are native-only;
Sin halted all Web work on September 13, including publication and browser checks.

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
[checkpoint](../../docs/implementation/party-beat-camera-checkpoint.md) records the
shared source changes and browser follow-through still required.
Studio creation, development, acceptance and next phases are also on hold. Its
existing shared source listing is compatibility wiring, not an active Studio phase.

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

[SMILE 2.0 Studio S1](../SmileStudio/README.md) provides Viewer and Character Editor
workspaces around this same implementation. The standalone window and controls
remain available through this folder's normal `Launch.ps1`. Both launchers protect
one canonical calibration editing session; a pending pose must be saved or discarded
before the other host can replace it.

The normal Viewer project permanently includes locally licensed Valor, Zara and
Vrax packages. Tabs are Arin, Orin, Valor, Zara, Dragon, Vrax, Party Dragon and
Party Vrax. Party Dragon preserves Arin and Orin against Dragon; Party Vrax uses
Arin, Orin and Zara against Vrax and remains the launch default. Valor is temporarily
hidden in Party Vrax, including his effects and turns; his individual tab and
canonical package remain available. Imported
sets contain 31, 26 and 24 animations, with nine clips per page. Vrax uses twice
his first preview's transform scale. `Prepare-BuildAssets.ps1` verifies each private
export checksum and refreshes the ignored project-local cooking mirror.
Canonical packages own conversion, grounding, checksums and private originals:
[ValorV1](../../games/SinStarI/SourceAssets/Characters/Knight/ValorV1/VALOR-CREATION-AND-REPAIR-JOURNEY.md),
[ZaraV1](../../games/SinStarI/SourceAssets/Characters/Warrior/ZaraV1/ZARA-CREATION-AND-REPAIR-JOURNEY.md) and
[VraxV1](../../games/SinStarI/SourceAssets/Bosses/Vrax/VraxV1/VRAX-CREATION-AND-REPAIR-JOURNEY.md).
Normal builds require all three private exports and diagnose missing prerequisites;
PublicRoster is an explicit separate compatibility publication.
Existing calibration editing applies to Arin/Orin. Valor and Zara now have measured
equipment VFX sockets; their original rigs and pose storage remain independent.
Valor and Zara's individual tabs use the same arena-preview contract as Arin and
Orin, but load Vrax as their opponent. Their normal demo cycles and explicit clip
buttons drive the selected hero while Vrax remains independently owned and visible.
Those two encounters double the arena camera distance so the accepted enlarged
Vrax and the active hero remain visible together.
Valor and Zara default to playback speed 200; Vrax retains speed 100.
Zara's white outline has a red tint and covers the full weapon silhouette, including its handle
and guard. SwordAttack uses Lightning Lab's Godstorm Ultra layout; SwordAttack2
uses Forked Judgment, both with crimson branches and pale red cores. Four strikes leave
capacity for the other actors. Weapon charge starts at 5 percent of clip time;
the storm, hit reaction and impact camera meet at 25 percent. Original Unity
discharge/impact sounds use separate channels from the existing Lab thunder.
Weapon/Glow visibility, family freeze, clip changes and teardown retain their
existing ownership. Unity visual effects were not imported.

Vrax now uses his original Unity growl at attack start and swoosh at 200 ms of
clip time, plus his original death vocalization. Standalone and Party Vrax share
the same cue policy on channels 4/5, separate from the hero sounds. Pause and tab
teardown stop those channels. Source files and provenance are preserved in the
VraxV1 package; the normal local build includes them and PublicRoster omits them.
The [Dragon animation trial](../../games/SinStarI/SourceAssets/Bosses/RedDragon/RedDragonV12VraxTrial/README.md)
has its own Trial/Original launcher and does not replace the default Dragon.

Standard native and Web builds now include the shared mandatory SMILE startup
presentation. It overlaps asset preparation with a one-second visible logo
minimum, shows creator credits and the artifact's compilation time/version, and
reports Web asset bytes separately from overall preparation. No Viewer source
initialization or optional import is required for startup. Every tab click also
calls the shared `Window_Loading()` operation before releasing the old scene;
the same splash remains until the new frame is presented, including cached switches.
On native Windows, the repeat splash is centered over the Viewer and uses 80-percent
window opacity so the retained scene remains visible behind it.
Failed or cancelled splash preparation keeps the previous scene, edit state and
interaction captures, and displays a retry notice. Click the tab again to retry.
See the
[startup contract](../../docs/architecture/startup-presentation.md).

Ctrl+Left/Right step the timeline using the queued press's modifier snapshot on
Desktop and Web. Quick taps remain frame steps even after Control is released;
plain arrows still orbit. This uses the shared `Key_Event_Held` built-in and does
not change live `Key_Held` behavior or any saved calibration.

Native-first reusable inspection and lightweight pose-correction tool. Party is the launch default. The Character tabs select the available roster listed above or the Party arena. Desktop Profile retains Arin v5.6, the earlier prototype, and the technical fixture for diagnostics. Web publication omits that control, its shortcut and the obsolete diagnostic assets. Current ownership and focused checks are in [ARCHITECTURE.md](ARCHITECTURE.md). The accepted [Studio design](../../docs/architecture/2026-09-09%20-%20SMILE%202.0%20Studio.md) governs future hosting; it is not implemented by renaming this tool.

The Viewer explicitly enables the shared polished planar floor on native Desktop and Web. The visible `Battle Floor: Reflective / Original` button is available in each character inspector and in the default Party panel beside the floor/environment controls. Reflective mode mirrors eligible characters and equipment beneath the original grid. It also maps only the screen-fixed background region visible above the projected floor edge, so the reflection cannot reveal artwork hidden behind the floor. Original mode keeps the established matte black floor and grid. The toggle preserves calibration, playback, camera, background, and character selection; full presentation reset restores the session default Reflective mode. Hiding the floor suppresses the receiver work without changing the preference. An optional graphics allocation/render failure keeps the original matte scene usable and reports `Battle Floor: Unavailable` rather than claiming the effect is active. Animation diagnostics are placed below the floor control and are hidden at compact window heights where they would collide with lower controls.

`VFX Reflections: On / Off` is a separate, default-on session control in the character
and Party panels. It mirrors eligible glow meshes, committed particles and trails
through the same renderer and frozen actor pose. Turning it off preserves the
character/equipment reflection and the actual scene's effects. Original floor mode
or hiding the floor suppresses all reflection capture while retaining this preference.
Reset restores both reflection preferences to On. Reflected effects use the floor's
existing strength/softness and its own depth buffer, so hands, legs and other opaque
geometry occlude their glow. This applies to all characters and effect families.
Heat distortion is excluded; reflected particles use hardware depth clipping rather
than the main camera's soft-intersection depth texture. Transparent effects retain
submission order. No extra simulation, pose update or calibration channel is created.

Dragon inspection uses the same clip buttons, timeline/frame stepping, playback speed, demo, lighting/material channels, sockets, pan/orbit/zoom and reset as the hero tabs. Both heroes remain in the arena with their own assets and saved corrections. Head Aim constrains only the head joint; At Arin/At Orin selects its target. The current Pose Calibration targets remain humanoid wrists and equipment, so they do not apply to Dragon. Dragon VFX and hero equipment visibility remain independent. Pose is disabled for Dragon, including its turn in Party.

Party Vrax's three heroes start on a 450-unit front arc from 15 to 45 degrees.
Zara's home is moved 20 percent toward Orin to balance her narrower silhouette
against Arin, while Orin remains the center facing Vrax.
The Party panel has separate minus/plus controls for Arin, Orin, Zara and Vrax.
Arin and Orin default to 150; Zara and Vrax default to 100. Each setting drives
that actor's clips, attack duration and impact timing. Changes restart the Party
presentation and retain the other actors' speeds; Party Dragon keeps its shared
speed control. Its intro makes one level horizontal
revolution in two seconds, easing in the same direction as Camera 1, then holds
the normal slow orbit for one second. Camera 1 defaults to vertical orbit 17 and
zoom -7, matching the accepted 846-unit reference framing. It continues through
full 360-degree slow revolutions. Camera 2 retains each battle composition while
orbiting in the same direction and at the same rate, using Camera 1's phase,
except the later reference-driven attack/recovery shots described below.
After a 250-ms close-up, each member reaches the selected boss in 300 ms, attacks,
and returns to its own home position. The hero shots show the close-up, track the
approach from behind and frame the full attack. Hero Beats 3, impact 3a and
recovery 4 now share the requested low frontal view, lining up the full attacking
hero with the imposing boss. At Beat 3 the full camera pose and lens lock through
Beat 4, including impact. The selected hero supplies the horizontal orbit angle
around the established arena target; the 846-unit distance, 85-unit height,
121-unit aim height and 24-degree lens follow Sin's three reference shots.
There is no tracking, panning, orbit or forward/backward movement during the hold.
Impact still registers at 64 percent, or Zara's
earlier 25-percent boundary, without an intervening wide-orbit cut. Vrax's own turns
use a 650-ms close-up, an 800-ms Run approach following from behind, a close battle
shot for the complete attack, then a frontal aftermath while he returns home.
For these two beats, the camera stays planted low behind the defender. Only its
look direction follows Vrax; the lens and camera position remain fixed. This
later requested exception to battle-camera orbit keeps the defender's full back
view in the foreground while Vrax and his fire/lightning dominate the scene.
Vrax skips Beat 3a, as requested. His selected defender rotates through Arin, Orin
and Zara; body facing, head aim, fire and arm lightning target that defender.
His Hit reaction finishes its clip before returning to Idle.
These five phases belong to Party Vrax. Party Dragon restores commit `c7f8f07`:
Arin/Orin only, radius 300, angles 10 to 50, the original approach locations,
650-ms lead and 650-ms approach, speed 200, and the original camera shots.
Clicking any tab, including the selected one, restarts its presentation. The
left-hand View label is removed. Saved pose calibration remains independent.

The Party runtime stores home, approach, bounds-derived clearance and facing metadata in
participant records. It separates approach lanes when the actors' measured ground-plane
bounds would overlap. The public formation regression uses a synthetic third participant; local builds
retain the separately packaged Valor and Zara actors, with Valor hidden by the
small `ViewerParty.PARTY_VALOR_VISIBLE` policy.

The editor source and build/launch entry points belong here. Sin Star I owns the self-contained character package at `games\SinStarI\SourceAssets\Characters\Paladin\ArinV57`. Orin owns `games\SinStarI\SourceAssets\Characters\Tank\OrinV13`. Do not edit ignored cooking inputs as canonical character assets.

## Maintainer routing

### Vrax attacks

Vrax breathes orange fire and fires blue-white lightning from both arms during
Attack through Attack6, in his own tab and Party Vrax. Party Vrax cycles all six
clips using Vrax's independent playback setting; its default 100 matches
the Vrax tab. Web PBR now uses the same front-face normal convention as native,
including reflected geometry, restoring armor highlights without extra render work.
Four rig-attached sockets follow the final grounded pose at the accepted 20000%
scale. The expanded mouth stream uses radius 24, intensity 320, a 420-unit reach
and 900-unit velocity. Effects run from one-sixth to four-fifths of each clip and clear on recovery,
Idle, Death, hide, tab changes and timeline discontinuities. The existing Party
fire/lightning freeze controls apply independently. No new combat or damage system
is involved. See [effect ownership and checks](ARCHITECTURE.md).

### Equipment VFX controls

Valor uses the shared sword flame and faint shield flames with Orin's blue/cyan/white
palette, plus a white shield rim with a blue halo. Zara has a white-red blade rim
and skinned equipment outline; her original gold emissive blade material is retained.
Both individual tabs and Party use their own weapon parts, bone attachments and
effect instances. Weapon/Shield hiding clears the corresponding attached effects.
Valor exposes the same flame freeze and shield style controls as Arin. Party shares
its existing fire-family freeze and shield-style controls across both flame users.
With Valor shown, the four-hero/Vrax scene uses ten fire emitters at rest and eleven during Vrax's
attacks, within the existing twelve-emitter limit. Zara's rim uses three
ribbon batches and no fire emitters. See the
[effect ownership and checks](ARCHITECTURE.md).

Orin's **VFX** button cycles **Blue Flame → Neon Arcs → Lightning**. Blue Flame
is the startup selection: Arin's shared fire family in blue/cyan/hot white, now
at the same emission intensity as Arin's sword (200 at the panel's 100% setting).
It surrounds the hammer head's measured outline
and leaves a world-space trail during motion; the shield adds faint edge flames.
Neon Arcs keeps a stronger closed neon outline with short travelling edge arcs
on both pieces of equipment, without star particles. Lightning preserves the
earlier appearance and star trail for comparison. Attack selection and discharge
timing remain independent of this appearance button.

Arin, Orin, Valor and Zara have separate **Weapon** and **Shield** strength sliders
in Character Status, from 0% to 200%. They use the same drag and hover-wheel
interaction as H Orbit. 100% is each style's intended baseline, not an absolute
brightness unit. Changes affect attached glow and emission; renderer capacity and
opacity limits still apply at high settings. Preferences
survive character-tab changes during this session and do not write pose saves.
Freeze Flames/Lightning follows Orin's selected effect family.

The shared fire pool admits twelve emitters. The public two-hero/Dragon Party
and Dragon inspection scenario uses eight:
Arin's sword and three shield edges, Orin's hammer and shield, Dragon mouth heat,
and the active breath/projectile. Both native and Web also support all twelve in
CPU fallback through 64 shared particle batches, retaining the 8,192-particle
ceiling. That Dragon scenario leaves four slots. The local four-hero/Vrax scene
uses ten hero emitters (Arin four, Orin two, Valor four) and one Vrax mouth emitter
during attacks, leaving one slot. Zara uses ribbons only. Vrax borrows two of the
shared lightning system's eight effect slots; a partially admitted pair is released
and retried on the next update. New effects require whole-scene admission checks.
Floor reflections replay existing submissions and consume no extra emitters.
ViewerDragon retries temporary pool pressure and clears failed admission on an
explicit effect reset. The production-actor budget/retry test is part of the
native calibration gate and its Web precision subset.

Weapon outlines are caller-owned `Precision3D.Contour3D` data, transformed through
the existing calibrated Character3D socket and sampled by shared fire/lightning
operations. Orin's package supplies his head contour; another weapon supplies its
own outline. There is no hammer-shape branch in the renderer or effect library.

Equipment glow uses shared appearance styles selected by
`ViewerProfiles.EquipmentGlowStyle`. Fire keeps the bright gold surface coating;
Lightning uses a front-culled expanded weapon outline so the metal and grip remain
visible. `ViewerEffects.ConfigureEquipmentGlow` applies either style to both the
primary actor and Party companions. Another character selects the Lightning style
without a new rendering branch. The shield retains its front-culled outline.
`UpdateEquipmentGlow` defaults to exact geometry, animation and calibrated transform
for surface coatings; only outline callers request expansion. Native/Web additive
mesh depth accepts matching surface fragments while preserving foreground occlusion.
Do not add character-specific scale/position offsets to hide blade/guard intersections.
The material contract is documented in `docs/architecture/renderer3d-materials.md`.

The shared `LightningVfx3D.WeaponTrail` provides a denser corona at two supplied
weapon edges and a short fading world-space trail during movement. `OrinStorm`
only supplies its calibrated attachment points and existing visibility/freeze
controls. Another character can use the same operation with its own points.
No new renderer, character asset, effect pool reservation or attack timing is needed.
Arin's shield defaults to Flames: its ember rim remains visible, with three small
LineFire emitters whose combined emission rate is about 10% of the sword's.
The small flames leave short wisps with zero inherited launch velocity. The
Ember Outline option still removes those flames without changing the saved pose.

Orin's ground discharge now centers on the enemy target at floor height, latched
when the attack enters release. Charge effects remain attached to the hammer;
the existing ground-arc radius, attack timing and Party target selection are unchanged.

Camera pan/orbit/zoom and Party approach/return now retain Double fractions through
the same renderer and Character3D actors. Current-pose camera anchors, equipment
and attached effects use precise world coordinates. The controls, actor scale,
sequence timing, IDs and saved integral calibration channels keep their established
meaning. The GPU still uses float32; this improves continuous authoring and motion
without increasing shader or depth-buffer precision. See
`docs/libraries/precision3d-boundary.md` and the compact
`docs/language/double.md` for limits and validation state.

Start with [ARCHITECTURE.md](ARCHITECTURE.md) for current ownership, public
operations, frame order and focused checks. Program keeps the ordered application
story and cross-owner input/result adapters; its justified size exception is
documented there. Subsystems retain their state and behavior. Character-specific
corrections also require the applicable package's creation-and-repair journey.

## Build and launch

Run `Build.ps1`, then `Launch.ps1`. Builds use the same configuration layout as
normal SMILE Visual Studio projects:

```text
bin/
  Debug/
    Character3DViewer.exe
    Assets/ ...
    Web/index.html ...
    Web - Optimized Low/index.html ...
    Web - Optimized Medium/index.html ...
    Web - Optimized High/index.html ...
  Release/
    Character3DViewer.exe
    Assets/ ...
    Web/index.html ...
    Web - Optimized Low/index.html ...
    Web - Optimized Medium/index.html ...
    Web - Optimized High/index.html ...
```

`Build.ps1` defaults to `-Configuration Release -Target All`, building native
first and then Web. Select `-Configuration Debug` for both Debug outputs, or
`-Target Native` / `-Target Web` for one target. Each Web directory is a complete
static publication; upload its entire contents, including all assets. Full is the
default. Low, Medium, and High require `-Target Web -WebQuality <quality>` and write
only to their named optimized directory, so the four Web publications do not
overwrite one another.

The normal `Build.ps1 -Target Web` publication includes the permanent Valor, Zara
and Vrax roster and is the full upload bundle for an authorized deployment. Use
`Build.ps1 -Target Web -PublicRoster` only for the compatibility roster that omits
those locally licensed packages. It writes to `bin\Release\Web - Public` (or the
selected optimized folder with ` - Public` appended) without overwriting the full
Web output.

The Viewer Web build generates an ignored publication project and profile policy
containing Arin, Orin, Dragon, Valor, Zara and Vrax. The normal asset publisher
removes obsolete managed diagnostic files from that Web output only.
Textures are neither transcoded nor resized; Desktop diagnostics and canonical
packages remain intact. Visual Studio's direct project build does not invoke this
tool-specific publication script; run `Prepare-BuildAssets.ps1` once after changing
or restoring a licensed package, then direct Desktop or Web compilation uses the
same permanent roster. Use `Build.ps1` for the current slim Web bundle.

`Launch.ps1` defaults to `bin\Release\Character3DViewer.exe`. Use
`Launch.ps1 -Configuration Debug` for Debug. `-Build` rebuilds the selected native
configuration before launch. A custom `-Executable` must already exist and cannot
be combined with `-Build`, preventing an unrelated build followed by a stale launch.
The launcher exports live calibration, closes old instances, preserves/restores
the stable working copy and watches both characters' saves to mirror them into
their separate canonical repository JSON files. Output relocation does not change
application IDs, data keys, fingerprints, or the saved-data location.

## Inspection

Below 800 × 540 logical units, a small-screen notice offers a touch-sized
**Continue** button. Dismissing it reveals the running scene and releases camera
pointer input while keeping the cramped editor panels hidden. The dismissal lasts
for the current Viewer session, including scene resets and orientation changes.
The normal editor panels return when the window is large enough; reloading starts
a new session. The Continue press is consumed so it cannot activate hidden controls.

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

Open `http://127.0.0.1:8766/` in installed Chrome. Chrome is the primary Web
acceptance browser; use Edge only for an Edge-specific problem. Port 8766 avoids the existing
hardening server on 8765; changing ports changes the browser storage origin.
To retain browser-authored saves from `http://127.0.0.1:8765`, stop that server
first and serve this directory on **8765** instead (the URL path may change,
but host, scheme and port must remain the same). Do not erase either origin's
storage. Browser working saves are separate and origin-scoped;
rebuilding does not replace them.
The Windows JSON path/Explorer action belongs to native. Browser JSON import/download,
primary/backup recovery and MSAA are supported. Context restoration may require
reselecting a character tab; the operating limits below distinguish tested targets. No automatic browser-to-repository synchronization is claimed.

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
- Arin, Orin, Valor and Zara expose independent Weapon and Shield strength sliders in
  Character Status. Each uses the same drag and hover-wheel interaction as H Orbit,
  ranges from 0 to 200 percent and starts at 100 percent for the Viewer session.
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

Party Vrax places Arin, Orin, Valor and Zara opposite Vrax. Party Dragon preserves
Arin and Orin opposite Dragon. Heroes approach, attack and return to formation in
turn, while the others idle or guard. The camera orbits by default.
Camera 1 sweeps a smooth front arc from one side of the boss to the other, using the same
12-degree-per-second phase rate as the individual tabs and easing at the arc endpoints.
It keeps advancing while battle cameras are selected. Camera 2 presents five hero
phases: an attacker close-up, a rear tracking approach, a full-character attack,
a wide front-orbit impact view and a side/rear aftermath view that includes the boss
and formation when framing permits.
Camera 3 frames the selected boss toward the party; Dragon uses this shot for
its windup and fireball charge. Beat boundaries cut immediately between independent
poses, while each active shot eases toward the moving actor.
Participants use their formation homes and bounds-derived approach lanes, stopping
outside the selected boss before attacking. Orin applies his own -55-degree
visual yaw correction so his imported hammer stance faces the target. The closer arena camera
keeps both sides readable while the nearer actor crosses the foreground. The viewer opens
directly in Party mode. Its controls sit in a dedicated left panel below the shared character
tabs, and the panel names the active attack while a party member strikes. Orin's Death
presentation follows a measured ground curve so his falling body settles onto the arena floor.
Space pauses movement; the usual pan, orbit, eased zoom, keyboard controls and reset remain
available. Weapon and Shield affect all current heroes and their attached effects; Zara has no shield. Party uses the same right inspector and
bottom timeline as the individual tabs, following whichever actor owns the current turn.
Timeline navigation pauses the scene and previews that actor without advancing the battle.
Open Pose to edit the active Arin or Orin using that hero's own saved correction track.
The Pose panel temporarily occupies the Party/Enemy left-panel area; closing it restores
the Party controls. Valor, Zara, Vrax and Dragon permit timeline inspection without pose editing. Resume restores the original
demo clip/time after preview, without rerolling a target, advancing a turn, or applying any
combat side effects. Save or cancel an active pose edit before resuming. The existing demo
choreography remains unchanged; this is not a battle-sequence authoring interface.
The right status panel follows the current attacker, including the selected boss animation details,
and reports the actual remaining turn time. Speed changes restart the Party demonstration.
Arin retains his thermal equipment fire. Orin defaults to blue/white flames with a white shield rim and offers the
Neon Arcs and Lightning appearance alternatives described above. His tab offers Thunder Smash, Storm Lance,
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

In the public two-hero/Dragon demonstration, Dragon opens and alternates Fire
Breath and Claw Strike. The local Vrax roster instead rotates boss, Arin, Orin,
Valor and Zara turns using their own imported clips.
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
Blender-to-SMILE pipeline. The package retains version-specific repair instructions; unrelated game work is outside this tool.

## Party Dragon reactions and diagnostics

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

## Calibration transfer

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

### Grounding and capture

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

## Operating limits and preservation

Native Windows and Chrome are the routine targets. Real Chrome Viewer and both
Labs have exercised High/GPU rendering, forced fallback, focus, context recovery
and shutdown. This is not Firefox/Safari/physical-device or arbitrary-GPU coverage.
Viewer context restoration can require selecting a character tab and then Party;
automatic full-scene recovery is not guaranteed. Concurrent saves do not merge.

Publish a complete selected Web folder, including Assets and TechnicalAssets,
with filename case preserved. Visitors need JavaScript/WebGL2; audio requires
user activation. Serve HTTP/HTTPS, not file URLs. New host/scheme/port means a new
save origin. Packaged defaults seed missing storage without overwriting existing
working keys. Keep private roster publication separate from public distribution.

Orin's storm clouds are light/flash presentation, not volumetric weather; one-boss
Chain Arcs do not imply multi-enemy combat. Dragon is a preview rig without flight
or foot-plant IK. Current calibration tools are not a full Blender replacement.
