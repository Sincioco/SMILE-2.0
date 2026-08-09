# Stage 2 — Raycasting Walkaround Implementation Specification

This is the authoritative implementation specification for Dungeon Star II.

---

# 1. Educational objective

Dungeon Star II should let a student open `Program.smile` and find a complete but approachable raycaster.

The source should demonstrate:

- fixed-point numbers;
- a player position inside a tile map;
- a direction vector;
- a camera plane;
- one ray for each vertical screen strip;
- DDA traversal;
- distance-to-height projection;
- wall collision;
- breadth-first demo navigation.

Do not hide the educational algorithm behind a native helper.

The program may be longer than a tiny example, but it must remain one understandable game—not a generalized engine framework.

---

# 2. Logical canvas and map

Use:

```smile
CONST CanvasWidth = 960
CONST CanvasHeight = 540

CONST MapWidth = 31
CONST MapHeight = 31
CONST MapCellCount = MapWidth * MapHeight
```

Dungeon Star II is one floor only.

The map should contain:

- open rooms;
- corridors;
- loops;
- doors;
- colored wall regions;
- one player start.

Unlike Dungeon Star I, Dungeon Star II **allows and expects** large 2-by-2 and larger walkable areas.

---

# 3. Raycast resolution

Start with:

```smile
CONST RayCount = 240
CONST StripWidth = 4
CONST HorizonY = CanvasHeight / 2
```

`RayCount * StripWidth` must equal `CanvasWidth`.

Draw the ceiling and floor once per frame:

```smile
FILL RECTANGLE 0, 0, CanvasWidth, HorizonY, CeilingColor
FILL RECTANGLE 0, HorizonY, CanvasWidth, CanvasHeight - HorizonY, FloorColor
```

Then draw 240 wall strips over them.

If a short measured check shows the GDI backend cannot present smoothly, reducing to:

```text
160 rays x 6 pixels
```

is permitted. Document the reason. Do not reduce quality speculatively.

---

# 4. Fixed-point scales

Use integer fixed-point values.

Recommended constants:

```smile
CONST CellScale = 1024
CONST VectorScale = 1000000
CONST CameraPlaneMagnitude = 660000
CONST ProjectionDistance = 830
CONST HugeDistance = 1000000000
```

Meaning:

```text
CellScale
    One map cell is 1024 position units.

VectorScale
    Direction vector length 1.0 is represented as 1,000,000.

CameraPlaneMagnitude
    Approximately 0.66, giving a field of view close to classic
    raycasting games.

ProjectionDistance
    Controls apparent wall size on the 960-pixel-wide canvas.
```

Player position:

```text
PlayerX = map column * CellScale + CellScale / 2
PlayerY = map row    * CellScale + CellScale / 2
```

Camera direction examples:

```text
North: DirectionX = 0            DirectionY = -VectorScale
East:  DirectionX = VectorScale  DirectionY = 0
South: DirectionX = 0            DirectionY = VectorScale
West:  DirectionX = -VectorScale DirectionY = 0
```

Camera plane:

```text
PlaneX = -DirectionY * CameraPlaneMagnitude / VectorScale
PlaneY =  DirectionX * CameraPlaneMagnitude / VectorScale
```

The plane is perpendicular to the direction vector.

---

# 5. Smooth fixed-point rotation without SIN or COS

Use a small rotation matrix for one half-degree step.

Recommended:

```smile
CONST RotationScale = 1000000
CONST HalfDegreeCos = 999962
CONST HalfDegreeSin = 8727
CONST HalfDegreeStepsPerCircle = 720
CONST HalfDegreeStepsPerQuarter = 180
```

For a right turn:

```text
NewDirectionX =
    (DirectionX * HalfDegreeCos - DirectionY * HalfDegreeSin)
    / RotationScale

NewDirectionY =
    (DirectionX * HalfDegreeSin + DirectionY * HalfDegreeCos)
    / RotationScale
```

For a left turn, negate the sine term.

Track:

```smile
FacingStep = 0
```

where each step is one-half degree.

Normalize `FacingStep` into:

```text
0 through 719
```

Snap the four exact cardinal directions at steps:

```text
0
180
360
540
```

This periodically removes accumulated integer rounding drift.

After updating direction, derive the camera plane again from the direction rather than independently rotating it.

Use an accumulator so turn speed remains time-based:

```smile
CONST TurnHalfStepsPerSecond = 180
```

That is:

```text
180 half-degree steps/second = 90 degrees/second
```

---

# 6. Fixed-step simulation

Follow the proven fixed-step structure used by the current ball games.

Recommended:

```smile
CONST SimulationStep = 8
CONST MaxCatchUpSteps = 6
```

Each rendered frame:

1. measure elapsed time with `TIMER()`;
2. clamp unusually long elapsed time;
3. add to an accumulator;
4. run zero or more eight-millisecond simulation steps;
5. cap catch-up work;
6. render once;
7. `SHOW SCREEN`.

Do not use frame-count-dependent movement.

Do not add `WAIT 16 MILLISECONDS` to the main game loop.

---

# 7. User movement

Controls:

| Input | Action |
|---|---|
| W or Up | Move forward |
| S or Down | Move backward |
| A or Left | Turn left |
| D or Right | Turn right |
| Enter or Space | Open the door in the center view |
| Escape | Return to title |
| Alt+Enter | Existing full-screen toggle |

Recommended:

```smile
CONST MoveUnitsPerSecond = 2560
CONST PlayerRadius = 180
```

`MoveUnitsPerSecond = 2560` is approximately 2.5 cells per second.

For each simulation step:

```text
MoveAmount = MoveUnitsPerSecond * SimulationStep / 1000

MoveX = DirectionX * MoveAmount / VectorScale
MoveY = DirectionY * MoveAmount / VectorScale
```

Use a negative amount for backward movement.

---

# 8. Collision and wall sliding

The player is a small circle represented by a radius in fixed-point units.

A candidate position is valid only when the four sample points around the radius are walkable:

```text
X - radius, Y - radius
X + radius, Y - radius
X - radius, Y + radius
X + radius, Y + radius
```

Closed doors are solid.

Open doors and floor are walkable.

Apply motion separately:

```text
Try X movement while preserving old Y.
Then try Y movement while preserving resulting X.
```

This creates natural wall sliding and prevents the player from getting stuck on every diagonal corner.

Do not implement arbitrary polygon collision.

---

# 9. Ray direction per strip

For ray index `RayIndex`:

```text
CameraX =
    RayIndex * 2 * VectorScale / RayCount
    - VectorScale
```

`CameraX` ranges approximately from:

```text
-1.0 at the left edge
to
+1.0 at the right edge
```

Then:

```text
RayDirectionX =
    DirectionX + PlaneX * CameraX / VectorScale

RayDirectionY =
    DirectionY + PlaneY * CameraX / VectorScale
```

This is the classic camera-plane method.

It gives fractional ray directions using ordinary integer arithmetic and avoids a large sine/cosine table.

---

# 10. DDA grid traversal

Create a routine such as:

```smile
SUB CastRay(RayIndex)
```

Because SMILE routines currently accept at most four scalar parameters, store ray results in clearly named global scratch variables:

```text
RayHitDistance
RayHitTile
RayHitSide
RayHitMapX
RayHitMapY
RayDoorLift
```

DDA setup:

```text
MapX = PlayerX / CellScale
MapY = PlayerY / CellScale
```

Delta distance:

```text
IF RayDirectionX = 0
    DeltaDistanceX = HugeDistance
ELSE
    DeltaDistanceX =
        ABS(CellScale * VectorScale / RayDirectionX)
END IF
```

Repeat for Y.

Initial side distance:

```text
If ray goes left:
    StepX = -1
    SideDistanceX =
        (PlayerX - MapX * CellScale)
        * VectorScale
        / ABS(RayDirectionX)

If ray goes right:
    StepX = 1
    SideDistanceX =
        ((MapX + 1) * CellScale - PlayerX)
        * VectorScale
        / ABS(RayDirectionX)
```

Repeat for Y.

DDA loop:

```text
Compare SideDistanceX and SideDistanceY.

Advance across the nearer grid boundary.

Add the appropriate DeltaDistance.

Move MapX or MapY by StepX/StepY.

Stop at the first solid tile.
```

Bound the loop:

```smile
CONST MaximumRaySteps = 96
```

Out-of-range map access behaves as a solid wall.

Never permit an infinite ray.

---

# 11. Perpendicular distance and fish-eye correction

When the ray hits:

```text
If X side was crossed:
    PerpendicularDistance = SideDistanceX - DeltaDistanceX

If Y side was crossed:
    PerpendicularDistance = SideDistanceY - DeltaDistanceY
```

Because the ray is built as:

```text
Direction + CameraPlane * CameraX
```

the DDA parameter is already the camera-forward/perpendicular distance used by the classic camera-plane raycaster.

Do not calculate Euclidean square roots.

Clamp:

```text
PerpendicularDistance >= 1
```

Store it in:

```smile
RayDepth[RayIndex]
```

Even though Stage 2 has no sprites, the depth array is a small educational foundation for a possible later sprite stage.

---

# 12. Wall-strip projection

Calculate:

```text
WallHeight =
    ProjectionDistance * CellScale
    / PerpendicularDistance
```

Clamp to a safe maximum, for example:

```text
CanvasHeight * 4
```

Then:

```text
WallTop = HorizonY - WallHeight / 2
WallBottom = HorizonY + WallHeight / 2
```

Clip drawing coordinates to the logical canvas.

Draw:

```smile
FILL RECTANGLE RayIndex * StripWidth,
               DrawTop,
               StripWidth,
               DrawHeight,
               WallColor
```

No texture sampling is part of Stage 2.

---

# 13. Wall types and shading

Internal numeric tile model:

```text
0  floor
1  wall type 1
2  wall type 2
3  wall type 3
4  wall type 4
5  wall type 5
6  wall type 6
7  closed door
```

An open door becomes floor.

Use precomputed color arrays:

```smile
DIM WallNear[7]
DIM WallMiddle[7]
DIM WallFar[7]
DIM WallVeryFar[7]
```

Use original palettes, for example:

```text
1  blue-gray stone
2  brown brick
3  green block
4  red masonry
5  gold panels
6  steel-violet
```

Choose a band from distance.

Darken the side hit when the ray crossed a horizontal grid line versus a vertical one.

Do not extract and recompute RGB channels per ray unless the result remains simple. Precomputed color bands are easier for students.

Door strips use a separate door palette.

---

# 14. Doors

Map symbol:

```text
D  closed door
O  open door
```

Only one door needs to animate at a time.

Global state:

```text
ActiveDoorX
ActiveDoorY
DoorOpening
DoorProgress
DoorStartedAt
```

User opening:

1. cast the center ray or reuse the most recent center-ray result;
2. require a closed door;
3. require distance within approximately 1.5 cells;
4. begin the door animation.

During animation:

- the tile remains solid;
- rays hitting the active door draw a shrinking/lifting vertical panel;
- the visible lower opening grows over time.

At completion:

```text
set the tile to floor
```

A simple rising door is approved and easier to understand than exact Wolfenstein sideways texture clipping.

No door closing is required in Stage 2.

---

# 15. Map loading

Use the current generic statement:

```smile
LOAD TEXT FILE "Maps\default.map" INTO MapFileBytes COUNT MapFileLength
```

Add literal loaders for:

```text
Maps\default.map
Maps\custom.map
```

The map parser, validation, and symbol conversion remain in `Program.smile`.

If loading or validation fails:

```text
generate a random map
show a brief fallback message
continue
```

See the separate map-format specification.

---

# 16. Random Wolfenstein-style map generation

Random generation must not reuse Dungeon Star I’s pipe restrictions.

Recommended generator:

1. Fill the 31-by-31 map with wall type 1.
2. Attempt to place 6–10 rectangular rooms.
3. Room interior sizes should vary approximately 4–9 cells.
4. Require one wall cell of separation before corridors connect them.
5. Carve each accepted room as an open rectangular floor area.
6. Connect each room center to the previous room using an L-shaped corridor.
7. Randomly choose horizontal-first or vertical-first.
8. Add 2–4 extra room connections to create loops.
9. Place several closed doors at suitable room/corridor thresholds.
10. Assign original wall types by room/region.
11. Pick a start inside the first room.
12. Validate complete reachability.
13. Retry a small bounded number of times.
14. Use a deterministic rooms-and-corridors fallback if retries fail.

Open 2-by-2 and larger floor areas are expected.

The generated map should visibly include:

- small rooms;
- larger rooms;
- connecting hallways;
- bends;
- loops;
- several doors.

Do not generate only narrow tubes.

---

# 17. Random-map validation

Verify:

- outer border is solid;
- exactly one valid start exists;
- all walkable cells are reachable;
- closed doors connect two usable sides;
- no start is inside a wall or door;
- player has clearance around the start;
- at least three distinct rooms exist;
- at least one loop or extra connection exists where generation permits;
- every ray is guaranteed to hit the closed border.

Use a bounded retry count.

Do not run hundreds or thousands of seeds by default.

---

# 18. Demo navigation

Demo mode uses the same player, movement, collision, raycaster, and door routines as user play.

Planning:

1. Convert current position to a map cell.
2. Run BFS through floor, open doors, and closed doors.
3. Choose a distant reachable cell—preferably the farthest discovered cell.
4. Reconstruct a cell-center route.
5. Steer continuously toward the center of the next route cell.
6. Open a closed door when it blocks the center path.
7. Advance to the next route cell when close enough.
8. When the route ends, plan another distant route.

Steering does not need `ATAN2`.

For target vector:

```text
TargetDeltaX = TargetCenterX - PlayerX
TargetDeltaY = TargetCenterY - PlayerY

Cross =
    DirectionX * TargetDeltaY
    - DirectionY * TargetDeltaX

Dot =
    DirectionX * TargetDeltaX
    + DirectionY * TargetDeltaY
```

In the game’s downward-positive Y coordinates:

```text
Cross > threshold   target is to the right
Cross < -threshold  target is to the left
Dot < 0             target is behind
```

Turn until reasonably aligned, then move forward.

If no progress occurs for approximately 1.5 seconds:

- replan;
- turn away from the blocking direction;
- do not teleport during normal operation.

A rare last-resort demo reset is permitted only to prevent a broken attract mode.

---

# 19. Title and screen states

Recommended states:

```smile
CONST STATE_TITLE = 0
CONST STATE_USER_WALK = 1
CONST STATE_DEMO_WALK = 2
CONST STATE_DEMO_COMPLETE = 3
```

Title:

```text
DUNGEON STAR II
RAYCASTING WALKAROUND

DEFAULT.MAP
CUSTOM.MAP
RANDOM MAP

UP / DOWN SELECT
ENTER START
```

Display an original raycast-room motif in the background.

At five seconds of inactivity, start demo with the selected source.

Demo runs 45 seconds.

`DEMO COMPLETE` remains five seconds, then title.

Any demo key returns immediately to title and is consumed.

---

# 20. Minimal HUD

Keep the first-person view dominant.

Allowed:

- selected map source;
- `DEMO`;
- `PRESS ANY KEY TO RETURN`;
- short fallback message;
- small center crosshair/dot;
- concise control hints on title only.

Do not add:

- minimap;
- weapons;
- score;
- lives;
- inventory;
- enemies;
- debug numbers in the normal release view.

A temporary developer diagnostic overlay may be used during implementation but must be removed or disabled by default.

---

# 21. Source organization

Recommended `Program.smile` sections:

```text
1. Constants
2. Fixed arrays
3. Global state initialization
4. Tile and map helpers
5. External map parser
6. Random rooms-and-corridors generator
7. Map validation
8. Camera initialization and rotation
9. Player collision and movement
10. Door behavior
11. DDA ray casting
12. Wall colors and rendering
13. Demo BFS and steering
14. Title and state transitions
15. Main fixed-step loop
```

Initialize every shared scalar at top level before routine declarations, consistent with current SMILE scoping rules.

Use global scratch values where the four-parameter routine limit makes that clearer than an artificial abstraction.

---

# 22. DirectX and GDI

Use only existing generic drawing calls.

Both backends render the same:

```text
ceiling rectangle
floor rectangle
240 wall-strip rectangles
title/HUD text
```

Do not add a raycasting method to the graphics vtable.

Do not access Direct3D or GDI directly from the game.

Do not add a pixel buffer in this milestone.

---

# 23. Expected performance

Target a visually smooth VSync-paced walkaround on the current development machine.

The intended workload is modest:

```text
240 rays
at most 96 DDA cell steps per ray
240 filled wall rectangles per frame
```

Use the existing graphics diagnostics only when needed.

A short performance observation is sufficient.

Do not run a long benchmark unless investigating a measured problem.
