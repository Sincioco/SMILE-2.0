# Dungeon Star II Map Authoring Guide

Dungeon Star II loads editable text maps from:

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
RANDOM MAP
```

Edit or replace `custom.map`, then select `CUSTOM.MAP`.

If the selected file is missing or invalid, the game safely generates a random rooms-and-corridors map.

---

# 1. Map dimensions

Dungeon Star II Stage 2 uses one floor:

```text
31 columns
31 rows
```

The file begins with:

```text
[FLOOR 1]
```

Then provide exactly 31 map rows.

Every map row must contain exactly 31 symbols.

Blank lines are allowed.

A comment line begins with:

```text
;
```

Example:

```text
; My first raycast map
[FLOOR 1]
###############################
...
###############################
```

Do not type literal `...` in a real map.

---

# 2. Symbols

| Symbol | Meaning |
|---|---|
| `#` | Default wall |
| `1` | Wall material 1 |
| `2` | Wall material 2 |
| `3` | Wall material 3 |
| `4` | Wall material 4 |
| `5` | Wall material 5 |
| `6` | Wall material 6 |
| `.` | Walkable floor |
| `D` | Closed door |
| `O` | Door that begins open |
| `N` | Start facing north |
| `E` | Start facing east |
| `S` | Start facing south |
| `W` | Start facing west |
| `U` | Dungeon Star I compatibility marker, treated as floor |
| `V` | Dungeon Star I compatibility marker, treated as floor |

There must be exactly one start marker.

---

# 3. Wall materials

`#` and `1` both use the default wall material.

Symbols `2` through `6` select different original wall color families.

They do not change collision.

Every wall symbol is solid.

Example:

```text
###########
#222222222#
#2.......2#
#2...E...2#
#2.......2#
#222222222#
###########
```

---

# 4. Open rooms are allowed

Dungeon Star I was designed as a narrow tube and rejected open 2-by-2 areas.

Dungeon Star II is different.

Open rooms are expected:

```text
#############
#...........#
#...........#
#...........#
#...........#
#############
```

You can create:

- small rooms;
- large rooms;
- broad halls;
- loops;
- pillars;
- wall islands;
- narrow corridors between rooms.

This is what gives the map a Wolfenstein-style layout rather than a pipe-only layout.

---

# 5. Keep the outside border closed

Every cell on the outside edge must be a wall:

```text
#
1
2
3
4
5
6
```

Correct:

```text
###############################
#.............................#
#.............................#
###############################
```

Incorrect:

```text
.##############################
```

The solid border guarantees that every ray eventually reaches a wall.

---

# 6. Player start

Use one:

```text
N
E
S
W
```

The symbol tells the game both the start cell and facing direction.

Place it on floor, not inside a wall or door.

Example:

```text
###########
#.........#
#....E....#
#.........#
###########
```

Unlike Dungeon Star I, the start may be inside a room.

Leave enough clear space around it for the player radius.

---

# 7. Doors

Use:

```text
D
```

for a closed door.

A door must be inside one straight opening.

Horizontal travel:

```text
#####.#####
.....D.....
#####.#####
```

The player travels left/right through the door.

Vertical travel:

```text
#####.#####
#####.#####
.....D.....
#####.#####
#####.#####
```

For a true vertical example, the cells above and below `D` must be floor while the left and right cells are walls.

A clearer vertical diagram:

```text
#####.#####
#####.#####
#####D#####
#####.#####
#####.#####
```

Use `O` when the opening should begin walkable.

Do not put a door:

- on the outside border;
- on the start;
- where all four neighboring cells are floor;
- where all four neighboring cells are walls.

Dungeon Star II does not require the long door spacing used by Dungeon Star I.

---

# 8. Copying a Dungeon Star I map

Dungeon Star II intentionally understands the first floor of the Dungeon Star I format.

To experiment:

1. Copy a Dungeon Star I `.map` file.
2. Rename the copy to:

   ```text
   custom.map
   ```

3. Put it in:

   ```text
   games\DungeonStarII\Maps
   ```

4. Build/run Dungeon Star II.
5. Select `CUSTOM.MAP`.

Dungeon Star II uses `[FLOOR 1]`.

If the file also contains `[FLOOR 2]` and `[FLOOR 3]`, they are ignored.

Shared symbols work:

```text
# . D O N E S W
```

`U` and `V` are accepted as ordinary floor in Stage 2.

The imported map will still look narrow because Dungeon Star I maps were designed as tubes.

To evolve it:

- remove wall cells;
- create wider floor areas;
- add wall material digits;
- retain a solid outside border;
- keep every walkable area connected.

---

# 9. Loading from the title screen

Dungeon Star II does not need a Windows file dialog.

The title has a fixed student-editable slot:

```text
CUSTOM.MAP
```

This keeps the language lesson simple and makes the path visible in SMILE source:

```smile
LOAD TEXT FILE "Maps\custom.map" INTO MapFileBytes COUNT MapFileLength
```

Every time you start `CUSTOM.MAP`, the game reads it again.

---

# 10. Connectivity

Every floor and door should be reachable from the start.

Bad:

```text
#############
#...#########
#############
#########...#
#############
```

The lower area is isolated.

The map validator rejects disconnected maps.

Closed doors count as possible connections because the player can open them.

---

# 11. A small example

This is smaller than a real 31-by-31 file:

```text
[FLOOR 1]
###############
#111111#222222#
#1....1#2....2#
#1.E..D......2#
#1....1#2....2#
#111111#222222#
#.............#
#33333D4444444#
#3...........4#
#3333334444444#
###############
```

A real map still needs exactly 31 rows of 31 symbols.

---

# 12. Common mistakes

## Wrong row length

Every map row must have 31 symbols.

## Missing header

The first floor must be introduced by:

```text
[FLOOR 1]
```

## Multiple starts

Use exactly one of `N`, `E`, `S`, or `W`.

## Open outside edge

Keep all border cells solid.

## Invalid door

A door must connect floor on exactly one axis.

## Disconnected room

Connect every room to the player’s reachable region.

## Editing the wrong file

The title’s student slot reads:

```text
Maps\custom.map
```

## File changes do not appear

Make sure the edited map has been copied beside the built executable.

A normal Visual Studio project build copies declared map assets.

---

# 13. Validation checklist

- [ ] `[FLOOR 1]` exists.
- [ ] There are exactly 31 map rows.
- [ ] Every row has exactly 31 symbols.
- [ ] Only documented symbols are used.
- [ ] The outside edge is solid.
- [ ] There is exactly one start.
- [ ] The start has room for the player.
- [ ] All floor areas connect.
- [ ] Every door has one valid orientation.
- [ ] The map contains the rooms and corridors you intended.
- [ ] `custom.map` is in the output Maps directory.

When a map fails validation, Dungeon Star II uses a random map instead, so the program remains playable.
