# Platform Quest Map Authoring Guide

Platform Quest loads text levels from:

```text
Maps
```

The supplied files are:

```text
Maps\default.map
Maps\custom.map
```

The title screen offers:

```text
DEFAULT.MAP
CUSTOM.MAP
RANDOM LEVEL
```

Edit `custom.map`, rebuild or copy it beside the executable, and select `CUSTOM.MAP`.

If the selected file is missing or invalid, Platform Quest safely creates a random level from known-safe level chunks.

---

# 1. Required size

A level begins with:

```text
[LEVEL 1]
```

Then provide exactly:

```text
15 rows
120 symbols in every row
```

Blank lines are allowed.

A comment begins with:

```text
;
```

Comments do not count as level rows.

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

Use exactly one `S` and one `G`.

---

# 3. Solid blocks

These block the player from every direction:

```text
#
B
?
```

`B` breaks when struck from below.

`?` awards a coin once and becomes a used solid block.

---

# 4. One-way platforms

`=` blocks the player only while landing from above.

The player can jump upward through it.

Example:

```text
................
....C.C.C.......
...=========....
................
```

Do not place enemies or spikes on unsupported air.

---

# 5. Ground gaps

The default physics is designed for short gaps.

Keep required gaps at no more than:

```text
3 tiles
```

Example:

```text
########...########
```

Longer gaps may be unreachable.

Optional platforms can provide another route.

---

# 6. Jump height

Keep required ledges and platforms within the demonstrated jump height in `default.map`.

Use the supplied level as a measuring example.

Avoid requiring the player to jump through a solid ceiling.

---

# 7. Start and goal

Place `S` above a supporting solid tile.

Place `G` above a supporting solid tile.

Example:

```text
...S................G...
########################
```

The goal is an original glowing gate.

---

# 8. Coins

Use `C` in air or above a platform.

Coins do not support the player.

Example:

```text
....C.C.C....
....=====....
```

---

# 9. Enemies

Use `E` above solid support.

Example:

```text
......E......
#############
```

The enemy patrols the available surface.

Do not spawn an enemy over a gap.

---

# 10. Spikes

Use `^` above solid support.

Example:

```text
....^^^.....
############
```

Touching a spike costs a life.

---

# 11. Loading your level

The student-editable title slot is:

```text
CUSTOM.MAP
```

The SMILE source reads:

```smile
LOAD TEXT FILE "Maps\\custom.map" INTO MapBytes COUNT MapByteCount
```

Every start reloads the file.

No Windows file dialog is required.

---

# 12. Common mistakes

## Wrong row width

Every level row must contain exactly 120 symbols.

## Wrong row count

There must be exactly 15 map rows.

## Missing header

Use:

```text
[LEVEL 1]
```

## Missing or duplicate start/goal

Use exactly one `S` and one `G`.

## Unsupported enemy or spike

Place support directly below `E` and `^`.

## Gap too wide

Keep required bottom gaps to three tiles or fewer.

## Unknown symbol

Only use the documented characters.

---

# 13. Checklist

- [ ] `[LEVEL 1]` exists.
- [ ] 15 map rows exist.
- [ ] Every row has 120 symbols.
- [ ] Only documented symbols are used.
- [ ] Exactly one start exists.
- [ ] Exactly one goal exists.
- [ ] Start and goal have support.
- [ ] Enemies and spikes have support.
- [ ] Required gaps are jumpable.
- [ ] Platforms and blocks form the intended route.
- [ ] The file is copied beside the executable under `Maps`.
