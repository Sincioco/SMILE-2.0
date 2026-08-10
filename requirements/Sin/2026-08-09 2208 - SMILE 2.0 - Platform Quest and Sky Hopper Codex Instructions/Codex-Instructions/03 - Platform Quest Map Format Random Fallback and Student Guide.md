# Platform Quest Map Format, Random Fallback, and Student Guide

Commit the supplied files as:

```text
games\PlatformQuest\MAP_AUTHORING.md
games\PlatformQuest\Maps\default.map
games\PlatformQuest\Maps\custom.map
```

---

# 1. File shape

Use:

```text
[LEVEL 1]
```

followed by exactly:

```text
15 rows
120 symbols per row
```

Blank lines are allowed.

Lines beginning with `;` are comments.

Only one level is required.

---

# 2. Symbols

| Symbol | Meaning |
|---|---|
| `.` | Empty air |
| `#` | Solid ground or stone |
| `B` | Breakable block |
| `?` | Bonus block |
| `=` | One-way platform |
| `C` | Coin |
| `E` | Enemy spawn |
| `^` | Spike hazard |
| `S` | Player start |
| `G` | Goal gate |

Exactly one `S` and one `G` are required.

---

# 3. Parser requirements

The parser remains in both SMILE sources.

Validate:

- header;
- exact dimensions;
- legal symbols;
- one start;
- one goal;
- start and goal have support;
- enemy/spike placement has support;
- bottom-row gaps are no wider than the approved jump;
- no access outside the map arrays.

On any failure, generate a random safe level.

---

# 4. Custom loading

Title entries:

```text
DEFAULT.MAP
CUSTOM.MAP
RANDOM LEVEL
```

`CUSTOM.MAP` always reads:

```text
Maps\custom.map
```

This is the student-editable slot.

No Windows file picker or mutable path string is needed.

---

# 5. Student guide

`MAP_AUTHORING.md` must explain:

- width/height;
- all symbols;
- solid versus one-way platforms;
- safe gaps;
- safe jump height;
- start and goal;
- blocks, coins, enemies, and spikes;
- title loading;
- missing/invalid fallback;
- common mistakes;
- a checklist.

Use text diagrams.

---

# 6. Static validator

Add:

```text
scripts\validate-platform-quest-maps.ps1
```

Validate the committed maps quickly.

Checks:

- required files;
- header;
- row count;
- width;
- legal symbols;
- one start/goal;
- support beneath start/goal/enemies/spikes;
- bottom gaps no wider than three cells.

Do not attempt a full platform-physics solver in PowerShell.

---

# 7. Artifact checks

The smoke suite must copy and byte-compare:

```text
Maps\default.map
Maps\custom.map
```

Compile:

```text
Program.smile
Program-NoDemo.smile
```

Verify both native executables.
