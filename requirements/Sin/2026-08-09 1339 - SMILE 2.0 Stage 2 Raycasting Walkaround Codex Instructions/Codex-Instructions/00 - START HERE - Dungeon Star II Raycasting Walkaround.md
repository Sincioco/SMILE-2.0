# START HERE — Dungeon Star II: Raycasting Walkaround

**Milestone:** Stage 2 — Raycasting Walkaround  
**Public game name:** `Dungeon Star II`  
**Subtitle:** `Raycasting Walkaround`  
**Repository:** `Sincioco/SMILE-2.0`  
**Local repository:** `D:\SMILE 2.0`  
**Verified design baseline:** `b5c4c66834c2132b89273eb56c6fc52cbde0fe29`

This package implements the educational step after Dungeon Star I:

```text
Dungeon Star I
    discrete square movement
    four cardinal directions
    projected quadrilateral corridor geometry

Dungeon Star II
    continuous fixed-point movement
    continuous rotation
    one ray per vertical screen strip
    Wolfenstein-style rooms, corridors, walls, and doors
```

The goal is only to walk around a raycast dungeon and understand how raycasting works.

There is no combat.

---

# 1. Read order

Codex must first read:

```text
D:\SMILE 2.0\AGENTS.md
```

Then read this package in order:

```text
00 - START HERE - Dungeon Star II Raycasting Walkaround.md
01 - Stage 2 Raycasting Walkaround Implementation Specification.md
02 - Raycasting Mathematics and Required Student Comments.md
03 - Compatible Map Format Title Loading and Student Map Guide.md
04 - Integration Validation Commit Plan and Definition of Done.md
```

Also inspect the ready-to-copy files under:

```text
Repository-Files\games\DungeonStarII
```

Those files are approved starting material for:

```text
games\DungeonStarII\MAP_AUTHORING.md
games\DungeonStarII\RAYCASTING_EXPLAINED.md
games\DungeonStarII\Maps\default.map
games\DungeonStarII\Maps\custom.map
```

Copy them into the repository during the applicable milestone.

---

# 2. Preserve newer work

Before editing:

```text
cmd /c git status --short
cmd /c git log -1 --oneline
```

The baseline above is informational only.

If `HEAD` is newer:

- do not reset;
- preserve newer architecture and features;
- preserve all uncommitted user work;
- adapt these requirements to the actual current repository;
- do not undo later demo-mode or game integration work.

Never:

- clean or discard user work;
- amend pushed commits;
- rebase pushed history;
- force-push;
- replace the shared compiler/runtime architecture.

---

# 3. Important current capabilities

At the verified baseline, SMILE already has everything required for this milestone:

- signed 64-bit integer numbers;
- one- and two-dimensional fixed arrays;
- procedures and functions;
- loops and conditionals;
- `Abs`, `Min`, `Max`, `Rgb`, and `Timer`;
- queued and held keyboard input;
- fixed-step timing examples;
- `Fill Rectangle` and the other generic drawing primitives;
- native DirectX and GDI graphics backends;
- `Load Text File "path" Into Array Count Variable`;
- executable-relative assets;
- automatic focus-loss audio muting;
- project asset copying;
- a student-editable Dungeon Star I map format.

No new language syntax is required for Stage 2.

Do **not** add:

```text
SIN
COS
FLOAT
DOUBLE
RAYCAST
Draw WALL COLUMN
CREATE 3D CAMERA
Load WOLFENSTEIN MAP
```

The camera-plane raycaster described in this package avoids trigonometric built-ins by using:

- fixed-point direction vectors;
- a perpendicular camera plane;
- a small fixed rotation matrix;
- DDA grid traversal.

This is simpler, keeps the raycasting mathematics visible in SMILE source, and follows the project’s KISS rule.

If Codex encounters a genuine implementation blocker, it may make the smallest generic language proposal consistent with `AGENTS.md`, but it must first demonstrate why the approved fixed-point design is insufficient. Do not add speculative math features.

---

# 4. Originality

Use Wolfenstein 3D only as a high-level technical and visual reference for:

- open rooms connected by corridors;
- first-person wall strips;
- sliding or lifting doors;
- continuous movement;
- continuous turning;
- flat ceiling and floor;
- distance-darkened colored walls.

Do not copy:

- maps;
- textures;
- sprites;
- enemies;
- weapons;
- sounds;
- music;
- logos;
- names;
- source code;
- exact UI.

Dungeon Star II must use original:

- branding;
- maps;
- wall colors;
- title art;
- code;
- documentation.

Public documentation should call it:

```text
an original Wolfenstein-style educational raycasting walkaround
```

not a Wolfenstein clone.

---

# 5. Approved product scope

Required:

- one-floor 31-by-31 raycast map;
- continuous forward/backward movement;
- continuous left/right turning;
- collision with walls and closed doors;
- wall sliding instead of getting stuck on corners;
- open rooms and broad spaces;
- several wall types/colors;
- distance shading;
- side shading;
- default external map;
- editable custom map;
- random map fallback;
- title map selection;
- five-second attract delay;
- self-playing demo;
- comments explaining the raycasting code;
- a student raycasting guide;
- a student map-authoring guide;
- DirectX and GDI support;
- 960-by-540 logical canvas;
- borderless Alt+Enter through the existing runtime.

Explicitly out of scope:

- enemies;
- weapons;
- shooting;
- health;
- inventory;
- pickups;
- sprites;
- texture mapping;
- floor or ceiling textures;
- varying floor heights;
- stairs between floors;
- BSP trees;
- portals;
- mouse look;
- strafing;
- audio requirements;
- image loading;
- a reusable native 3D engine;
- a game-specific raycasting runtime function.

---

# 6. Public project structure

Create:

```text
games\DungeonStarII\
    DungeonStarII.smileproj
    DungeonStarII.slnx
    Program.smile
    README.md
    MAP_AUTHORING.md
    RAYCASTING_EXPLAINED.md
    Maps\
        default.map
        custom.map
```

Window:

```smile
Game Window "Dungeon Star II - Raycasting Walkaround"
```

Output:

```text
DungeonStarII.exe
```

Use:

```xml
<GraphicsBackend>Auto</GraphicsBackend>
<VSync>true</VSync>
```

Declare:

```xml
<Asset Include="Maps\**\*" />
```

No image or audio assets are required.

---

# 7. Title map choices

The title must offer:

```text
Default.MAP
CUSTOM.MAP
Random MAP
```

`Default.MAP` is selected initially.

Controls:

```text
Up / W       previous map source
Down / S     next map source
Enter/Space  start selected source
Escape       exit
Alt+Enter    full screen
```

A student creates or replaces:

```text
Maps\custom.map
```

and chooses `CUSTOM.MAP` on the title.

The game re-reads the external file every time it starts that source. No operating-system file picker is required.

Missing or invalid selected files safely fall back to a random map.

---

# 8. Demo behavior

Use the repository’s current arcade-attract convention when one exists at implementation time.

When no later convention supersedes it, use:

```text
Title inactivity:       5 seconds
Demo walkaround:        45 seconds
Demo-complete overlay:   5 seconds
Then:                   return to title
```

The demo:

- loads the currently selected map source;
- uses `Default.MAP` when the user has not changed the title selection;
- walks through rooms and corridors;
- turns continuously;
- opens doors;
- uses the same collision and movement routines as the user;
- never teleports as its normal navigation method.

Any ordinary key during demo returns to title and is consumed.

---

# 9. Implementation approach

The renderer must stay in `Program.smile`.

For each of 240 rays:

1. determine the ray direction from the camera direction and camera plane;
2. use DDA to step from grid boundary to grid boundary;
3. stop at the first wall or closed door;
4. calculate perpendicular distance;
5. convert distance to a wall-strip height;
6. choose an original wall color and distance shade;
7. draw one four-pixel-wide vertical rectangle.

The result is a 960-pixel-wide view:

```text
240 rays x 4 pixels = 960 pixels
```

Do not cast one ray per physical output pixel.

---

# 10. Execution directive

Implement the complete package without stopping after planning.

Use the permanent velocity rule:

- assume the happy path;
- use focused tests;
- run the normal smoke suite;
- perform short representative gameplay checks;
- do not run long soak tests unless investigating a known defect.

Commit and push the completed milestone using the current `AGENTS.md` prefix and detailed body format.

Suggested subject:

```text
Sin and Codex: feat(game): add Dungeon Star II raycasting walkaround
```
