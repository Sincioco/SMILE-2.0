# How Dungeon Star II Raycasting Works

Dungeon Star II looks three-dimensional, but its world is still a flat grid.

The game uses a classic technique called **raycasting**.

This guide explains the ideas used by `Program.smile`.

---

# 1. The map is still two-dimensional

A map is a grid viewed from above:

```text
################
#......##......#
#......D.......#
#......##......#
#..............#
################
```

From above:

- `#` is a wall;
- `.` is floor;
- `D` is a closed door;
- the player has an X/Y position and a direction.

The raycaster turns that top-down information into a first-person picture.

---

# 2. What is a ray?

A ray is an imaginary line that begins at the player and travels forward.

The game casts many rays:

```text
             \  |  /
              \ | /
               \|/
              PLAYER
```

A left-side ray points slightly left.

The center ray points straight ahead.

A right-side ray points slightly right.

Each ray stops at the first wall or closed door it touches.

---

# 3. One ray becomes one vertical strip

Dungeon Star II casts one ray for every logical screen pixel:

```text
960 rays x 1 pixel = 960 pixels
```

For every ray, the game asks:

```text
How far away is the first wall?
What kind of wall is it?
Did the ray hit an X side or a Y side?
```

Consecutive samples that hit the same map plane are merged into one filled
quadrilateral. Direct2D therefore anti-aliases a long, continuous top and bottom
edge instead of hundreds of tiny strip edges. Each ray also records its exact map
cell and grid face. Neighboring runs terminate at one shared projected corner only
when those faces have an identical grid endpoint; neither face overlaps the other,
and unrelated near and far surfaces remain separate regardless of viewing distance.

Close walls may project above or below the logical canvas. The game keeps those
true off-screen endpoints until the complete quadrilateral is built, then lets
the graphics backend clip it. Clamping endpoints first would bend a straight wall
into a false wedge as the player approached it. Projected height is likewise not
capped before clipping, so both ends of a close wall retain the same perspective.

The game simulation remains fixed at 8 millisecond steps for predictable
collision, doors, and demo decisions. Rendering interpolates the camera between
the previous and current fixed states using the leftover accumulator time. Rays
therefore see a smoothly moving rigid camera instead of advancing in 8 ms pulses.

A nearby wall makes a tall strip.

A distant wall makes a short strip.

All strips together form the first-person view.

---

# 4. Fixed-point numbers

SMILE currently uses integer numbers.

Raycasting needs fractions, so Dungeon Star II uses **fixed-point math**.

For position:

```text
1 map cell = 1024 position units
```

The center of map cell `(4, 7)` is:

```text
X = 4 * 1024 + 512
Y = 7 * 1024 + 512
```

For direction:

```text
1.0 = 1,000,000 direction units
```

East is:

```text
DirectionX = 1,000,000
DirectionY = 0
```

North is:

```text
DirectionX = 0
DirectionY = -1,000,000
```

After multiplying two scaled values, the program divides by the scale again.

That restores the intended size.

---

# 5. Direction and camera plane

The camera stores two important vectors:

```text
Direction
Camera plane
```

Direction points straight ahead.

The camera plane points sideways across the view:

```text
             camera plane
        <------------------->
                  |
                  |
              direction
                  |
               player
```

A ray is calculated as:

```text
ray = direction + part of camera plane
```

At the center of the screen, the added part is zero.

At the left edge, it is negative.

At the right edge, it is positive.

This produces many different ray directions without needing a large sine table.

---

# 6. Turning without floating point

Dungeon Star II turns the camera with a small rotation matrix.

One update rotates the direction by one-half degree.

The program stores fixed constants for:

```text
cosine of one-half degree
sine of one-half degree
```

The direction is multiplied by those constants.

Every quarter turn, the program snaps to an exact north, east, south, or west direction.

That prevents small integer-rounding errors from accumulating forever.

---

# 7. DDA: jumping between grid lines

DDA means **Digital Differential Analyzer**.

A slow ray test might move forward one tiny amount at a time:

```text
step
step
step
step
check wall
step
step
```

DDA is smarter.

It calculates the next vertical grid boundary and the next horizontal grid boundary.

Then it jumps directly to whichever is nearer:

```text
current cell
    |
    +---- next vertical boundary
    |
    +---- next horizontal boundary
```

Every jump enters one new map cell.

The process repeats until the new cell is a wall or closed door.

Because the map is only 31 by 31, each ray needs only a small number of steps.

The code also has a hard maximum so a malformed map cannot create an infinite ray.

---

# 8. Why the walls have different heights

After a ray hits a wall, the game knows its distance.

The basic projection is:

```text
wall height = projection constant / wall distance
```

More precisely, the program includes the cell scale.

The idea is still simple:

```text
small distance -> large height
large distance -> small height
```

That is the main illusion behind the raycaster.

---

# 9. Fish-eye correction

If a raycaster uses the raw length of angled rays, walls can appear curved.

Dungeon Star II uses a camera direction plus a perpendicular camera plane.

With the DDA distance used by this camera-plane form, the value represents the camera-forward distance needed for wall projection.

This keeps a straight wall looking straight across the screen.

The program does not need a square root.

---

# 10. Wall colors and shading

The map can use several wall symbols:

```text
1  bright turquoise stone
2  cheerful orange brick
3  lively green block
4  rose masonry
5  sunny gold panel
6  violet steel
```

The renderer does not attach a distance-based light to the camera. Wall colors
therefore stay stable as the player moves. It only darkens one wall orientation
slightly.

That helps the player see room corners even without textures.

---

# 11. Collision

The player is treated as a small circle.

Before moving, the game checks points around the player radius.

If the new X position is safe, it applies X movement.

Then it checks Y movement separately.

This lets the player slide along walls.

Without separate checks, a diagonal collision would stop all motion and feel sticky.

---

# 12. Doors

A closed door behaves like a wall.

When the player presses Enter or Space:

1. the game checks the center ray;
2. the door must be close enough;
3. the opening animation begins;
4. the visible door strip becomes shorter;
5. after the animation, the map cell becomes floor.

Stage 2 does not require doors to close again.

---

# 13. Demo navigation

The demo still uses the same continuous camera.

It plans a route through map cells with breadth-first search.

Each route cell has a center.

The demo turns toward that center and moves forward.

Two ordinary vector calculations help:

```text
cross product -> target is left or right
dot product   -> target is in front or behind
```

The demo therefore follows a grid plan without snapping the visible player from cell to cell.

---

# 14. Why this is called 2.5D

Dungeon Star II is not a general polygonal 3D engine.

Its limits are intentional:

- the map is two-dimensional;
- every wall has the same height;
- the floor is flat;
- the ceiling is flat;
- rays hit grid cells;
- there are no sloped surfaces.

The first-person view looks three-dimensional, so this style is often called **2.5D**.

---

# 15. Safe experiments

Try changing these constants in `Program.smile`.

## Ray count

```text
More rays
    smoother walls
    more work

Fewer rays
    chunkier retro appearance
    less work
```

Keep:

```text
RayCount * StripWidth = 960
```

## Camera plane magnitude

Larger values widen the field of view.

Smaller values narrow it.

Very large values create strong perspective distortion.

## Projection distance

Larger values make walls appear taller.

Smaller values make them appear shorter.

## Movement and turning speed

Change them independently.

The fixed-step simulation keeps the result refresh-independent.

## Wall colors

Edit the near, middle, far, and very-far palette arrays.

The map does not need textures to look different.

---

# 16. What a later stage could add

A future Stage 3 could build on this walkaround with:

- wall textures;
- transparent sprite billboards;
- decorative objects;
- a per-ray depth buffer for sprite occlusion;
- animated enemies;
- sound;
- one simple weapon.

Those features are intentionally outside Dungeon Star II Stage 2.

First understand the rays.
