# Playable native battle

The title's **Battle** opens a fresh encounter: Arin, Orin, Zara and Mira at level 1,
against level-20 Kael. Kael uses Zara's complete stat formula. Battle Simulations
remain the existing choreography previews. Encounter XP and resulting levels are
shown after victory; entering a new encounter resets this test battle to the
requested starting levels. This slice does not introduce a campaign save or inventory.

Select **Fight** to repeat the hero's last command (initially Attack). **Order →
Fight** opens the character's authored physical attacks: Arin's two sword attacks,
Orin's hammer/jump/Thunder attacks, Zara's six sword attacks, or Mira's staff attack.
The chosen attack and its animation are remembered. Additional physical variants
add ten percentage points of attack power per list position.

**Order** also offers Magic, the Item placeholder, Defend, Limit Break and Run.
Magic costs MP; healing allows selection of a living ally. Empty MP causes a
remembered spell to fall back to Attack. Defend triples both defense stats until
that hero's next action. LB fills from actual HP lost, caps at maximum HP and is
consumed when used. Mira's LB heals the most injured living ally; others deal damage.
Run affects only that character: 50% initially, +2.5 percentage points per level
above Kael, capped at 100% at a 20-level advantage. Fractional chances round down.

Turn order is Arin → Orin → Zara → Mira → Kael, skipping defeated or escaped heroes.
After the first round, the last commands repeat automatically. A key or mouse-button
press during execution requests orders at the next round's first available hero.
Arrow keys select menu rows; Enter/Space confirms; Escape backs out of a submenu.
The Back button exits the encounter. The active hero has a red arrow and highlighted
panel. Panels use the shared 80% opacity, and damage/healing labels rise and fade.

The camera begins behind the heroes. Shared arena controls remain left-drag pan,
middle-drag orbit, eased wheel zoom, right-click reset, F floor, G grid and B the
Viewer's background palette. Arena RGB/spacing/size remain library creation options.
Kael's theme plays. His Earth/Water casts stay home without a Run transition.
Mira uses speed 200. Character models, grounding, equipment and saved Arin/Orin
calibration are consumed from the existing canonical packages; no pose is rebaked.

## Progression

`Characters/Progression.smile` is the pure formula authority. The **Characters**
presentations offer **Stats Table** and **Graphs** for level ranges 1–300 and 1–9,999.
Tables show absolute stats or per-level increases and exact integer XP. Graphs show
each stat or an overlay, with actual values or normalized percentages. The optional
normalization compares each stat to its own final value. Viewer adoption is on hold.

For level L, set g = L − 1. Each table cell below is starting value + per-level gain.
Priority weights use 10 HP, 5 MP, 1 Attack or 1 Physical Defense per point, so the
different stat units do not make HP automatically outrank every other attribute.

| Character | HP | MP | Attack | Magic | Physical Defense | Magic Defense |
|---|---:|---:|---:|---:|---:|---:|
| Arin | 240 + 30g | 55 + 10g | 40 + 4g | 30 + 3g | 50 + 5g | 42 + 4g |
| Orin | 270 + 40g | 40 + 10g | 50 + 5g | 22 + 2g | 40 + 3g | 32 + 3g |
| Zara / Kael | 250 + 40g | 70 + 15g | 48 + 5g | 32 + 3g | 30 + 2g | 30 + 2g |
| Mira | 220 + 40g | 130 + 25g | 23 + 2g | 52 + 5g | 35 + 3g | 45 + 4g |
| Other inspected characters | 230 + 24g | 65 + 6g | 43 + 4g | 28 + 3g | 30 + 2g | 28 + 2g |

The four requested priorities have weights 5 > 4 > 3 > 2. Other characters use
baseline inspection stats pending their game roles. LB power is 6 × Attack + 5 × L;
Mira substitutes Magic. Normal attack power is 2 × Attack, spell power is 3 × Magic,
and healing restores 3 × Magic. Damage is power × 100 / (100 + applicable defense),
with minimum 1; HP changes clamp to available/current maximum HP.

XP to advance from L is `60 + 20g + 3g²` before level 300. From 300 onward, the
cost becomes `25 × normalCost + 500000 × (L − 299)²`. This deliberately makes
post-300 progress extremely hard. Level 9,999 is the hard ceiling. Closed-form
prefix sums and binary level lookup avoid long loops in the UI; graphs use 300
samples even for the extended range. Total XP at level 9,999 is 152113650571981357,
stored and displayed with integer arithmetic, without Double precision loss.

## Owners and validation

- `BattleRules`: caller-owned encounter, commands, HP/MP/LB, turn transitions and XP.
- `BattleAttacks`: the shared physical-attack labels and animation names.
- `BattleActors`: five renderer actors, grounded placement, accepted calibration and
  clip timing. Kael faces his current target; canonical Arin/Orin facing is preserved.
- `BattleArena`: scene/effect resources, separate Kael effect target and shared arena.
- `BattleFeedback`: a bounded pool of eight fading labels using the existing bitmap font.
- `BattleUI`: five panels and stacked command choices, without combat authority.
- `BattleScreen`: lifecycle and update/draw coordination; Program only routes screens.
- `StatsView`: popup state supplied by its caller; no dependency on the Viewer host.

Build/run `BattleRulesTests.smileproj` for rule/progression assertions. Run
`scripts/test-sin-star-presentation.ps1 -BattleOnly` for real-asset battle transitions,
all attack clips, menus and release checks; the default invocation retains the
existing character/simulation/music checks. Native `Precision3DTests` covers the
rear camera's upright orientation and screen-space pan. Shared camera composition
now derives its right vector from the base camera rather than assuming a view
from negative Z. Arena/actor failures retain an operation and renderer/actor codes
on the recovery screen and print them to a captured native console when available.

No language/runtime extension, external dependency, architecture threshold change,
Studio work or Web publication is introduced. Inventory and controller support remain
future work as requested. The original intermittent Viewer recovery investigation
remains on hold; this battle's distinct effect-target failure is covered here.

Native validation: Release game build (254 published assets), pure battle/XP checks,
real-asset attack/spell/defense/menu/release checks, all nine existing character
presentations and three simulations, Precision3D rear-camera regression, the Viewer
hardening fixture and its 58 graphics/input/audio checks, 13 formatter checks,
repository formatting and scoped formatting all pass. Visible native inspection
confirmed the upright opening, mouse attack selection, four hero turns, enemy
response, automatic repetition, damage labels/LB gains, next-round interruption,
pan, wheel zoom and reset, plus the table/overlay/individual-stat views. Native
input fixtures cover the shared orbit policy; no new human middle-drag acceptance
or Web validation is claimed. Arin and Orin exports matched their canonical JSON
without byte changes.

Ownership review: nine production modules add 2,646 lines; the largest are the
cohesive battle-rules (544) and battle-UI (525) owners. Neither depends on Program
or the Viewer host. The existing startup grows by 12 net routing lines, title
routing by seven, character presentation by 67 and the shared camera helper by
seven. No baseline/exclusion is raised. Rule and presentation fixtures add 280
lines, with 31 regression lines in the existing precision fixture. Build products
remain ignored. The source-only library fix needs consumer recompilation; no .NET
runtime or VSIX rebuild is required.
