# Platform Quest — Original Mario-Style Platformer

Create:

```text
games\PlatformQuest
```

Public identity:

```text
Display name: SMILE 2.0 Platform Quest
Output:       PlatformQuest.exe
```

Platform Quest is an original educational side-scrolling platform game.

---

# 1. Product scope

Required:

- one long side-scrolling level;
- external `default.map`;
- external `custom.map`;
- random level option;
- random fallback when a file is missing/invalid;
- left/right running;
- jumping;
- gravity;
- variable-height jump;
- solid blocks;
- breakable blocks;
- bonus blocks;
- one-way platforms;
- coins;
- spike hazards;
- simple patrolling enemies;
- enemy stomping;
- score;
- lives;
- persistent high score;
- goal gate;
- camera scrolling;
- original background music;
- original sound effects;
- title map selection;
- five-second demo;
- genuine `Program-NoDemo.smile`;
- DirectX and GDI support.

Not required:

- copied Mario characters or art;
- power-up transformations;
- fireballs;
- swimming;
- moving platforms;
- slopes;
- ladders;
- multiple levels;
- save games;
- sprite sheets;
- imported images;
- tile animation framework;
- complex enemy pathfinding.

---

# 2. Logical layout

Use:

```smile
Const CanvasWidth = 960
Const CanvasHeight = 540
Const HudHeight = 60

Const TileSize = 32
Const MapWidth = 120
Const MapHeight = 15
```

World rendering begins below the HUD.

The visible world is approximately 30 tiles wide.

The level is approximately four screens long.

---

# 3. Map symbols

Use the map format in the separate map guide.

Internal tile meanings:

```text
.  empty
#  solid ground/stone
B  breakable block
?  bonus block
=  one-way platform
C  coin
E  enemy spawn
^  spike hazard
S  player start
G  goal gate
```

After parsing:

- `S` becomes empty plus player start;
- `G` remains a goal trigger;
- `C` becomes a collectible state;
- `E` creates an enemy and becomes empty;
- bonus/breakable state remains in the map array.

---

# 4. Fixed-point physics

Suggested scales:

```smile
Const SubpixelsPerPixel = 1000
Const SimulationStep = 8
Const MaxCatchUpSteps = 6

Const RunAcceleration = 1500000
Const RunFriction = 1800000
Const MaximumRunSpeed = 260000
Const Gravity = 1450000
Const JumpVelocity = -520000
Const MaximumFallSpeed = 720000
```

Tune visually.

Player state:

```text
PlayerXSub
PlayerYSub
PlayerVX
PlayerVY
PlayerWidth
PlayerHeight
OnGround
Facing
JumpHeldUntil
JumpBufferUntil
CoyoteUntil
InvulnerableUntil
```

Use current fixed-step patterns.

---

# 5. Controls

| Input | Action |
|---|---|
| A / Left | Run left |
| D / Right | Run right |
| W / Up / Space | Jump |
| Escape | Return to title |
| Alt+Enter | Full screen |

Provide:

- brief jump buffering;
- brief coyote time;
- variable jump height by reducing upward velocity when jump is released early.

Keep the implementation small and commented.

---

# 6. Tile collision

Use an axis-aligned player rectangle.

Apply motion separately:

1. horizontal movement;
2. resolve solid tile overlap;
3. vertical movement;
4. resolve floor/ceiling overlap.

Solid from all directions:

```text
#
B
?
```

One-way platform:

```text
=
```

It collides only when:

- the player is falling;
- the player’s previous bottom was above the platform top.

This lets the player jump upward through it.

Do not add native collision helpers.

---

# 7. Block interaction

## Breakable block `B`

When the player’s head hits it from below:

- remove the block;
- award a small score;
- play `Block.wav`;
- draw a short four-particle geometric burst.

No power state is required.

## Bonus block `?`

When hit from below:

- award one coin;
- award score;
- replace it with a used solid block;
- play `Coin.wav`.

Use a numeric tile value for the used block.

---

# 8. Coins

Coins pulse or spin using circles/arcs/rectangles.

Collect by player overlap.

On collection:

- remove;
- increment coin count;
- add score;
- play `Coin.wav`.

Do not require every coin in order to reach the goal.

---

# 9. Enemies

Use a small original geometric walking creature, such as a colored rounded slime.

Enemy behavior:

- move left or right at constant speed;
- reverse at solid walls;
- reverse before walking off a ledge;
- collide with the player.

Player collision:

- falling onto the top of an enemy:
  - defeat enemy;
  - bounce player upward;
  - award score;
  - play `Stomp.wav`;

- side/bottom collision:
  - lose a life unless invulnerable;
  - play `Hurt.wav`;
  - respawn.

Do not copy Goomba art or exact behavior.

Use fixed parallel arrays with a bounded enemy count.

---

# 10. Hazards and lives

Spikes `^` are drawn as original triangles using quadrilaterals.

Touching spikes or falling below the world loses a life.

User begins with three lives.

Respawn at:

- the original start; or
- the most recent safe-ground checkpoint stored by X progress.

A simple safe-X checkpoint every 20 tiles is acceptable.

At zero lives:

- stop music;
- show normal user game-over;
- allow retry or title;
- save high score if appropriate.

Demo behavior is separate and never displays a terminal overlay.

---

# 11. Camera

Use a horizontal camera:

```text
CameraX
```

Target the player around 40 percent of the screen width.

Clamp to:

```text
0 through MapWidth * TileSize - CanvasWidth
```

Smooth follow is optional.

Convert world X to screen X:

```text
ScreenX = WorldX - CameraX
```

Do not move the tile map itself.

Use simple parallax backgrounds:

- far clouds;
- hills;
- distant blocks;
- ground color.

---

# 12. Goal and user victory

The goal should be an original glowing gate or portal—not a Mario flagpole.

When the player reaches `G`:

- stop normal movement;
- play `Goal.wav`;
- award completion bonus;
- save high score;
- show a user victory panel;
- allow replay or return to title.

If a later map/level system exists in the repository, do not overextend this first game. One complete level is enough.

---

# 13. Title and map selection

Title choices:

```text
Default.MAP
CUSTOM.MAP
Random LEVEL
```

Initial selection:

```text
Default.MAP
```

Controls:

```text
Up/W     previous
Down/S   next
Enter    start
Space    start
Escape   exit
```

Changing selection resets the five-second demo timer.

The title shows controls and high score.

Use original title artwork.

---

# 14. File loading and fallback

Use:

```smile
Load Text File "Maps\\default.map" Into MapBytes Count MapByteCount
Load Text File "Maps\\custom.map" Into MapBytes Count MapByteCount
```

Parse entirely in `Program.smile`.

When a file is missing or invalid:

- discard partial state;
- generate a safe random level;
- show a brief fallback message;
- continue.

`Random LEVEL` always generates.

Reload external maps every time the selected source starts.

---

# 15. Random level generation

Use safe predesigned chunks, not unconstrained random tiles.

Recommended:

```text
12 chunks
10 columns per chunk
```

Create 8–12 numeric chunk patterns in SMILE source.

Each chunk may contain:

- flat ground;
- a short jumpable gap;
- one low obstacle;
- a one-way platform;
- coins;
- one enemy;
- a spike group;
- a block row.

Chunk rules:

- entry and exit ground heights are compatible;
- no required gap exceeds three tiles;
- no required vertical jump exceeds the tuned player jump;
- never spawn an enemy on a gap;
- the first chunk contains start;
- the final chunk contains goal;
- at least one clear route exists.

Randomly choose middle chunks.

This produces variety while guaranteeing playability.

Use a deterministic safe fallback sequence if generation validation fails.

---

# 16. Demo AI

The AI uses the same player controls/physics.

Default behavior:

- move toward the goal;
- scan several tiles ahead;
- jump for:
  - gaps;
  - spikes;
  - solid obstacles;
  - approaching enemies;
  - useful raised platforms;
- steer in the air;
- avoid reversing without reason;
- collect nearby coins when safe.

Suggested lookahead:

```text
2–5 tiles
```

The AI does not need a full physics search.

Before 30 seconds:

- if killed, respawn at the last safe checkpoint;
- preserve `DemoStartedAt`;
- continue;
- do not show game over.

If it reaches the goal before 30 seconds:

- load/generate another run and continue the same demo timer.

After 30 seconds:

- reaching the goal or dying may return directly to title.

At 45 seconds:

- return directly to title.

Any user input returns directly to title and is consumed.

Demo score never saves.

---

# 17. `Program-NoDemo.smile`

Create a genuine separate source.

It retains:

- title map selection;
- map loading;
- random generation;
- user physics;
- enemies;
- score/lives/high score;
- music/effects;
- game-over/victory.

It removes all automatic title demo behavior and all AI.

---

# 18. Audio assets

Create:

```text
Assets\Background.wav
Assets\Start.wav
Assets\Jump.wav
Assets\Coin.wav
Assets\Block.wav
Assets\Stomp.wav
Assets\Hurt.wav
Assets\Goal.wav
Assets\GameOver.wav
```

Play background music only during user/demo play.

Tune `Music Volume` so effects remain audible.

All audio must be original.

---

# 19. Required source comments

Add concise educational comments explaining:

- fixed-point position and velocity;
- gravity;
- separate horizontal/vertical collision;
- one-way platforms;
- camera world-to-screen conversion;
- safe chunk generation;
- simple demo lookahead.

Do not over-comment obvious drawing statements.
