# Galaga-Style Star Squadron Game with Demo Mode

The user requested a Galaga-inspired game using the full width and height of SMILE’s screen.

To preserve original branding, create:

```text
Display name:  Star Squadron
Folder:        games\StarSquadron
Project:       StarSquadron.smileproj
Executable:    StarSquadron.exe
Window title:  SMILE 2.0 Star Squadron
```

The supplied commercial screenshot is a visual/mechanical reference only.

Do not copy commercial names, logos, enemy sprites, formations, sounds, music, attack patterns, scoring tables, or source code.

## 1. Product objective

Create an exciting, original, fixed-screen space shooter in `.smile`.

Core experience:

- player ship moves horizontally near the bottom;
- fire upward;
- colorful enemies enter and form ranks;
- enemies dive in curved/piecewise attack patterns;
- enemies fire downward;
- player clears waves and advances stages;
- title, score, high score, lives, sounds, and demo AI.

## 2. Full 16:9 layout

Use the entire logical canvas:

```text
960 x 540
```

Design every major element for 16:9:

- starfield reaches both left and right edges;
- formation uses most of the width;
- diving paths can reach both sides;
- player movement uses nearly the full width;
- HUD uses the top edge without narrowing the playfield.

Do not stretch a narrow arcade image.

On a 16:9 display, no game-authored black side columns may exist.

The shared runtime may still letterbox on a physically non-16:9 monitor to preserve aspect ratio. Do not remove that global safety behavior.

## 3. Project structure

Create:

```text
games\StarSquadron\
    StarSquadron.smileproj
    StarSquadron.slnx
    Program.smile
    README.md
    Assets\
        PlayerShot.wav
        EnemyShot.wav
        EnemyHit.wav
        PlayerHit.wav
        Dive.wav
        StageClear.wav
        Start.wav
        GameOver.wav
```

Use `Auto` graphics and VSync.

Add the game to smoke build and native artifact verification.

## 4. Game states

Recommended:

```text
Title
Ready
Live stage
Player explosion
Stage clear
User game over
Demo play
Demo terminal
```

All rules remain in `Program.smile`.

## 5. Player

Controls:

```text
A / Left     move left
D / Right    move right
Space        fire
Enter        start
Escape       title/exit according to state
Alt+Enter    full screen
```

Use fixed-point movement.

Suggested:

```text
Player speed: 420–520 pixels/second
Maximum simultaneous player shots: 2 or 3
Lives: 3
```

Represent the ship with original quadrilaterals, rectangles, lines, and circles.

Add a short muzzle flash and explosion animation using geometry.

## 6. Starfield

Use fixed arrays for stars:

```text
X
Y
speed tier
brightness/color
```

Move stars downward at two or three speeds and wrap to the top.

The starfield fills all 960 by 540.

Avoid hundreds of expensive objects; approximately 60–100 stars are sufficient.

## 7. Enemy formation

Use approximately:

```text
8–12 columns
4 rows
32–48 enemies
```

Enemy fields:

```text
state
type
formation row/column
X/Y subpixels
dive path progress
shot timer
alive flag
```

Suggested enemy states:

```text
ENTERING
FORMATION
DIVING
RETURNING
DESTROYED
```

Formation:

- centered;
- spreads across most of the screen;
- drifts gently left/right;
- may compress/expand slightly;
- does not touch the side edges.

Use 2–3 original enemy designs and color families.

## 8. Enemy entry and diving

Enemies may enter in small groups before settling into formation.

Avoid trigonometry requirements by using:

- integer lookup tables;
- piecewise linear/quadratic interpolation;
- precomputed curve offsets;
- mirrored paths.

Diving enemies:

- peel away from formation;
- arc toward the player region;
- may fire;
- exit or turn upward;
- return to formation when alive.

Keep enough enemies in formation that the screen remains readable.

## 9. Projectiles and collisions

Use fixed arrays for:

```text
player shots
enemy shots
```

Collision:

- player shot vs enemy;
- enemy shot vs player;
- diving enemy vs player;
- offscreen projectile cleanup.

Use simple circle/rectangle overlap appropriate to the geometric art.

Suggested scoring:

```text
formation enemy      50–100
diving enemy         extra bonus
elite enemy          larger bonus
stage clear          bonus
```

Persist user high score only.

## 10. Stages and difficulty

A stage completes when all enemies are destroyed.

Then:

- play stage-clear sound;
- show short stage-clear overlay;
- increment stage;
- rebuild formation;
- modestly increase dive frequency and projectile speed.

Do not require a boss system for the first milestone.

A small elite enemy type with two hit points is allowed if it remains simple.

## 11. Sounds and excitement

Extend `scripts\generate-sounds.ps1`.

Use original deterministic WAV effects for:

- player shot;
- enemy shot;
- hit/explosion;
- player loss;
- dive warning;
- stage clear;
- start;
- game over.

Excitement should come from:

- starfield motion;
- formation movement;
- dive attacks;
- muzzle flashes;
- short explosion geometry;
- score feedback;
- escalating attack frequency;
- distinct effects.

Do not add copied audio or a new multi-channel mixer by default. The current one-effect WAV surface is acceptable; prioritize velocity.

Shared focus muting automatically silences inactive games.

## 12. User mode

Title starts user play with Enter or Space.

Live play:

- left/right movement;
- Space fires;
- brief ready state;
- three lives;
- game over at zero lives;
- persistent high score;
- Enter/Space retry or title behavior documented.

User gameplay must not be made artificially safe by the demo safeguards.

## 13. Demo AI

The AI controls the player ship.

AI inputs:

```text
nearest dangerous enemy shot
diving enemy positions/velocity
current formation targets
player shot availability
```

Behavior:

1. choose a safe horizontal target;
2. move away from predicted projectile/diver impact;
3. otherwise align with a live enemy;
4. fire when a target is approximately above and shot capacity permits;
5. avoid oscillation with a dead zone and short decision interval;
6. prioritize survival over perfect score.

All AI remains `.smile`.

## 14. Thirty-second safety

Before 30 seconds:

- prevent a terminal game-over display;
- if the demo loses its last life, restore one life or reset the player/formation while preserving the demo timer;
- continue active action;
- do not save demo score.

The AI should normally survive without using this path.

## 15. Demo terminal

At 45 seconds:

- transition to `DEMO OVER` or game-over overlay;
- play game-over sound once;
- keep overlay five seconds;
- return to title.

A natural demo game over after 30 seconds follows the same rule.

Any key during demo returns to title and is consumed.

## 16. Title

Create original title art spanning the screen.

Show:

```text
STAR SQUADRON
ENTER Or SPACE To START
A / D Or ARROWS To MOVE
SPACE To FIRE
DEMO STARTS AUTOMATICALLY
ESCAPE To Exit
```

After five seconds of inactivity, demo starts.

## 17. Documentation and integration

Update:

```text
AGENTS.md
README.md
games\StarSquadron\README.md
scripts\generate-sounds.ps1
scripts\smoke-test.cmd
scripts\verify-artifacts.ps1
```

After completion, current public documentation should describe seven games:

```text
Snake
Falling Blocks
Paddle Ball
Brick Breaker
Dungeon Star I
Maze Muncher
Star Squadron
```

## 18. Fast validation

Automated:

```text
cmd /c scripts\smoke-test.cmd
```

Manual happy path:

1. Confirm full 16:9 title/playfield.
2. Start user play.
3. Move and fire.
4. Destroy an enemy.
5. Observe one dive and one enemy shot.
6. Confirm player hit/life behavior.
7. Return to title.
8. Wait five seconds for demo.
9. Confirm AI moves, dodges, and fires.
10. Confirm active demo through 30 seconds.
11. Confirm terminal by 45 seconds and title five seconds later.
12. Confirm any-key demo cancellation.
13. Briefly toggle Alt+Enter.

One normal stage does not have to be fully completed if the representative happy path is already demonstrated.

No long shooter soak or massive projectile stress test is required unless debugging a known issue.

## 19. Suggested commit

```text
feat(game): add Star Squadron wide-screen arcade shooter
```
