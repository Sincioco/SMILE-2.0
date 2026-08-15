# START HERE — Platform Quest and Sky Hopper

**Repository:** `Sincioco/SMILE-2.0`  
**Local repository:** `D:\SMILE 2.0`  
**Verified package-design baseline:** `df467e27c2846ee93a61b5d8b809450626032462`

This package instructs Codex to create two original SMILE 2.0 games:

```text
Platform Quest
    an original Mario-style side-scrolling platform game

Sky Hopper
    an original Flappy Bird-style obstacle-flight game
```

The commercial games are mechanical references only.

Do not copy names, characters, levels, sprites, pipes, music, sound effects, logos, or source code.

---

# 1. No new language syntax is required

The current SMILE 2.0 language already provides everything necessary:

- signed integer arithmetic;
- fixed arrays;
- `Sub` and `Function`;
- loops and conditionals;
- fixed-step timing through `Timer()`;
- queued and held keys;
- rectangles, rounded rectangles, circles, arcs, quadrilaterals, lines, text, and numbers;
- `Load Text File`;
- integer persistence;
- WAV sound effects;
- looping background music through `Play Music`;
- DirectX and GDI rendering;
- automatic focus muting.

Both games must be implemented with the current language.

Do not add:

```text
GRAVITY
PLATFORM
SPRITE
ANIMATION
COLLISION
CAMERA
FLAP
PIPE
Draw Image
```

or game-specific native helpers.

A future image/sprite feature could be useful for artist-supplied artwork, but it is not needed for these primitive-drawn educational games.

---

# 2. Read order

Codex must first read the current:

```text
D:\SMILE 2.0\AGENTS.md
```

Then read this package in order:

```text
00 - START HERE - Platform Quest and Sky Hopper Mission.md
01 - Shared Architecture Demo NoDemo Audio and Originality Rules.md
02 - Platform Quest Mario-Style Platformer Specification.md
03 - Platform Quest Map Format Random Fallback and Student Guide.md
04 - Sky Hopper Flappy-Style Game Specification.md
05 - Integration Commit Plan Validation and Definition of Done.md
```

Also inspect:

```text
Repository-Files\games\PlatformQuest
```

Copy the approved starting files during the Platform Quest milestone:

```text
games\PlatformQuest\MAP_AUTHORING.md
games\PlatformQuest\Maps\default.map
games\PlatformQuest\Maps\custom.map
```

---

# 3. Inspect all newer commits

Before editing:

```text
cmd /c git status --short
cmd /c git log -1 --oneline
cmd /c git log --reverse --oneline df467e27c2846ee93a61b5d8b809450626032462..HEAD
```

The baseline is informational only.

If the repository is newer:

- do not reset;
- preserve all new architecture and games;
- reuse current demo/no-demo conventions;
- reuse the current audio and map patterns;
- adapt game counts and file lists;
- never overwrite uncommitted user work.

---

# 4. Public project identities

Create:

```text
games\PlatformQuest
Window: SMILE 2.0 Platform Quest
Output: PlatformQuest.exe
```

and:

```text
games\SkyHopper
Window: SMILE 2.0 Sky Hopper
Output: SkyHopper.exe
```

Each folder must include:

```text
Program.smile
Program-NoDemo.smile
README.md
<game>.smileproj
<game>.slnx
Assets\
```

Platform Quest additionally includes:

```text
MAP_AUTHORING.md
Maps\default.map
Maps\custom.map
```

---

# 5. Autonomous execution

Codex is authorized to:

- implement both games sequentially;
- generate original audio assets;
- update documentation and scripts;
- build, test, commit, and push;
- continue without asking for intermediate approval.

Implement and push Platform Quest first.

Then implement and push Sky Hopper.

Then complete final integration.

Do not combine both games into one difficult-to-review commit.

---

# 6. Permanent current rules

Follow current `AGENTS.md`, especially:

- games remain in `.smile`;
- every demo game has a genuine `Program-NoDemo.smile`;
- demo completion returns directly to title;
- any demo key returns directly to title;
- demo records never persist;
- focus muting is shared runtime behavior;
- testing is light and happy-path by default;
- commit subjects use the current required prefix.

---

# 7. Expected result

After completion, the repository gains two educational examples:

```text
Platform Quest teaches:
    tile maps
    scrolling cameras
    gravity
    velocity
    collision
    jumping
    enemies
    coins
    level goals
    external level files

Sky Hopper teaches:
    simple vertical physics
    procedural obstacle generation
    object recycling
    collision
    scoring
    increasing difficulty
    small reactive demo AI
```
