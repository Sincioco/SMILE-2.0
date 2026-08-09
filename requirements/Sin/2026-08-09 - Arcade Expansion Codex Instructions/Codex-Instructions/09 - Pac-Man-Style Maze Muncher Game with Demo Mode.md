# Pac-Man-Style Maze Muncher Game with Demo Mode

The user requested a “Pack-Man” game inspired by the supplied classic maze-chase screenshot.

To preserve SMILE’s original-branding rule, create the public game as:

```text
Display name:  Maze Muncher
Folder:        games\MazeMuncher
Project:       MazeMuncher.smileproj
Executable:    MazeMuncher.exe
Window title:  SMILE 2.0 Maze Muncher
```

The commercial reference is for broad visual/mechanical inspiration only.

Do not copy:

- the original maze;
- character or ghost shapes pixel-for-pixel;
- commercial names;
- logos;
- sprite sheets;
- sounds;
- music;
- bonus-item art;
- source code.

Create an original maze, original geometric characters, original sounds, and original branding.

## 1. Product objective

Create a complete native SMILE 2.0 maze-chase arcade sample written in `.smile`.

Core experience:

- move through a neon-lined maze;
- eat small pellets;
- eat larger power pellets;
- avoid four pursuing enemies;
- temporarily chase vulnerable enemies after a power pellet;
- use left/right wrap tunnels;
- clear the maze to advance;
- lose a life when caught;
- title, game-over, high score, and attract demo.

## 2. Scope

Required:

- one original default maze;
- score and persistent high score;
- three lives;
- pellets and power pellets;
- four enemies;
- enemy home/start area;
- wrap tunnel;
- frightened/vulnerable state;
- increasing level speed;
- original WAV effects;
- five-second title demo delay;
- 30-second minimum demo play;
- 45-second normal demo duration;
- five-second demo terminal;
- any-key demo cancellation;
- automatic shared audio focus muting;
- DirectX and GDI support.

Not required:

- exact arcade timing;
- exact commercial enemy personalities;
- intermission cartoons;
- fruit catalog;
- cutscenes;
- two-player alternating mode;
- joystick/controller input;
- image assets;
- texture loading;
- networking.

## 3. Project structure

Create:

```text
games\MazeMuncher\
    MazeMuncher.smileproj
    MazeMuncher.slnx
    Program.smile
    README.md
    Maps\
        default.map
    Assets\
        Pellet.wav
        Power.wav
        EnemyEaten.wav
        PlayerCaught.wav
        Start.wav
        LevelClear.wav
        GameOver.wav
```

Use:

```xml
<GraphicsBackend>Auto</GraphicsBackend>
<VSync>true</VSync>
```

Copy `Maps\**\*` and `Assets\**\*`.

Add the game to smoke compilation and native artifact verification.

## 4. Display layout

Use the full 960-by-540 logical canvas.

Suggested arrangement:

```text
Maze area:  approximately 30, 45 through 735, 515
HUD area:   approximately 755 through 940
```

The maze may be:

```text
31 columns x 21 rows
```

with a cell size around 22 pixels.

Use:

- black background;
- original blue/cyan maze outlines;
- warm small pellets;
- larger power pellets;
- a bright circular player;
- four distinct colorful geometric enemies;
- readable score/lives/level HUD.

Do not reproduce the exact reference maze.

## 5. Maze data

Reuse the generic statement added by the Dungeon milestone:

```smile
LOAD TEXT FILE "Maps\default.map" INTO MazeFileBytes COUNT MazeFileLength
```

Keep the maze parser in `Program.smile`.

Suggested maze symbols:

```text
#  wall
.  pellet corridor
o  power pellet corridor
-  empty corridor without pellet
P  player start
1  enemy start 1
2  enemy start 2
3  enemy start 3
4  enemy start 4
=  enemy-home gate
T  wrap-tunnel endpoint
```

The parser must:

- validate dimensions;
- validate exactly one player start;
- validate four enemy starts;
- confirm all pellet corridors are reachable by the player;
- confirm two tunnel endpoints;
- fall back to a small embedded/deterministically generated safe maze if the file is missing or invalid.

Do not add a Maze Muncher-specific native loader.

## 6. Maze rendering

Use existing primitives:

```smile
DRAW LINE
DRAW RECTANGLE
DRAW ROUNDED RECTANGLE
FILL CIRCLE
FILL QUADRILATERAL
DRAW TEXT
DRAW NUMBER
```

Walls should look like glowing double lines or rounded blue channels.

The player can be rendered as:

1. a filled yellow circle;
2. a black quadrilateral or small black shape over one side to create an animated mouth;
3. mouth direction follows movement;
4. mouth alternates open/closed by timer.

Enemies should be original geometric designs, for example:

- rounded head;
- rectangular/rounded body;
- two eyes;
- small triangular/quadrilateral feet;
- distinct color and eye direction.

Vulnerable enemies use a shared cool/dim palette and flashing warning near the end.

## 7. Movement

Use grid-center movement with fixed-point subpixels.

Player:

- W/A/S/D and arrows;
- direction changes are buffered;
- turn only when centered and the requested corridor is open;
- continue current direction otherwise;
- wrap through tunnel endpoints;
- stop when blocked.

Recommended movement:

```text
Player: 95–120 pixels/second
Enemies: slightly slower on level 1
Vulnerable enemies: slower
```

Use a fixed simulation step consistent with existing ball games where appropriate.

## 8. Pellets, levels, and score

Suggested values:

```text
small pellet       10
power pellet       50
vulnerable enemy   200, 400, 800, 1600 in one power cycle
level clear bonus  optional
```

When all pellets are eaten:

- play level-clear sound;
- pause briefly;
- rebuild/reset pellets;
- increment level;
- slightly increase movement speed;
- reset characters.

Save high score only in user mode.

## 9. Enemy AI

Use four simple original roles:

```text
Hunter      targets current player cell
Ambusher    targets several cells ahead
Flanker     targets a side/ahead combination
Wanderer    alternates chase and distant corner
```

At tile centers, choose a legal direction.

Rules:

- do not reverse direction unless frightened, leaving home, or no alternative;
- use BFS distance or a simple target-distance heuristic;
- add a small scatter/chase schedule;
- leave the home area at staggered times;
- collisions are evaluated consistently.

Do not use the commercial character names or exact original targeting formulas.

## 10. Power mode

When the player eats a power pellet:

- set a timed vulnerable state;
- enemies may reverse once;
- enemies move more slowly;
- collision lets the player eat an enemy;
- eaten enemy returns to home and later re-enters;
- near expiration, vulnerable colors flash.

Suggested level-1 duration:

```text
6–8 seconds
```

Reduce modestly on later levels.

## 11. User death

When caught in user mode:

- play caught/death animation and sound;
- decrement life;
- reset positions after a brief pause;
- preserve eaten pellets;
- game over at zero lives;
- Enter/Space retries or returns as designed;
- Escape follows the project’s title/exit convention.

## 12. Demo AI

The AI becomes the player.

At tile centers:

1. identify reachable pellets/power pellets;
2. score candidate targets by distance;
3. add danger penalties for nearby normal enemies;
4. add opportunity reward for vulnerable enemies;
5. prefer a power pellet when danger is high;
6. BFS toward the best target;
7. reject a next step that places the player too close to a normal enemy;
8. use tunnel escape when useful.

A simple rolling plan is enough; recalculate often.

### Thirty-second safety

Before 30 seconds:

- if caught, reset player/enemy positions and continue;
- do not show game over;
- retain the original demo timer;
- do not save score.

### Demo ending

At 45 seconds:

- display `DEMO OVER` or normal game-over art;
- retain for five seconds;
- return to title.

Any key during demo returns to title and is consumed.

## 13. Title

Create original title art.

Show:

```text
MAZE MUNCHER
ENTER OR SPACE TO START
ARROWS OR W A S D
DEMO STARTS AUTOMATICALLY
ESCAPE TO EXIT
```

Five seconds of no activity starts demo.

## 14. Sounds

Extend `scripts\generate-sounds.ps1` with original deterministic effects.

Keep effects short and distinctive.

Do not copy arcade sounds.

One asynchronous WAV effect at a time is acceptable for this milestone. Do not expand the audio mixer unless a real implementation blocker is demonstrated.

Focus muting remains automatic shared runtime behavior.

## 15. Documentation

Update:

```text
AGENTS.md
README.md
games\MazeMuncher\README.md
scripts\smoke-test.cmd
scripts\verify-artifacts.ps1
scripts\generate-sounds.ps1
```

After this game exists, current public docs should describe six games until Star Squadron is added.

## 16. Fast validation

Automated:

```text
cmd /c scripts\smoke-test.cmd
```

Manual happy path:

1. Title displays.
2. Start user game.
3. Move through maze and eat pellets.
4. Use one wrap tunnel.
5. Eat one power pellet.
6. Confirm one vulnerable-enemy collision.
7. Confirm focus mute/restoration briefly if audio code changed.
8. Return to title.
9. Wait five seconds for demo.
10. Observe AI through 30 seconds.
11. Confirm terminal by 45 seconds and title after five seconds.
12. Confirm any-key cancellation.

No full multi-level completion or long AI endurance run is required.

## 17. Suggested commit

```text
feat(game): add Maze Muncher arcade sample
```
