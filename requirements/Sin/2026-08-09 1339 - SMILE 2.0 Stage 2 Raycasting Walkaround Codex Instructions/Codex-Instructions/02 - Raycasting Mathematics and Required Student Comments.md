# Raycasting Mathematics and Required Student Comments

This milestone is educational. The source comments must help a beginning programmer understand the algorithm without turning every assignment into a paragraph.

Commit the supplied student guide as:

```text
games\DungeonStarII\RAYCASTING_EXPLAINED.md
```

Also place concise comments directly in `Program.smile`.

---

# 1. Comment style

Use:

- section headers;
- one-to-four-line explanations before important algorithms;
- meaningful variable names;
- a few comments beside non-obvious fixed-point constants.

Avoid:

- commenting obvious assignments;
- academic proofs;
- engine terminology that is not explained;
- hiding the algorithm behind generic abstractions;
- excessively long comments inside the main loop.

The student should be able to read the code from top to bottom.

---

# 2. Required comment blocks in `Program.smile`

The wording may be adjusted, but each idea must be explained.

## 2.1 Fixed-point values

Place near the scale constants:

```smile
' SMILE currently uses integer numbers. We store fractions by scaling them.
' One map cell is CellScale position units, and a direction length of 1.0
' is VectorScale units. We divide by the same scale after multiplication.
```

## 2.2 Direction and camera plane

Place near camera initialization:

```smile
' Direction points straight ahead. Plane points sideways across the camera.
' A ray is Direction plus a portion of Plane. The left side uses a negative
' portion, the center uses zero, and the right side uses a positive portion.
```

## 2.3 Fixed rotation matrix

Place before camera rotation:

```smile
' This small rotation matrix turns the direction by one-half degree.
' Using fixed constants keeps the lesson in ordinary SMILE integer math,
' so this sample does not require floating point or SIN/COS built-ins.
```

## 2.4 One ray per strip

Place before the ray loop:

```smile
' The screen is divided into vertical strips. We cast one ray for each strip.
' The first wall touched by that ray determines the height and color drawn
' in that part of the screen.
```

## 2.5 CameraX

Place where `CameraX` is calculated:

```smile
' CameraX is -1.0 on the left, 0 in the center, and almost +1.0 on the right.
' It chooses how much of the sideways camera plane is added to this ray.
```

## 2.6 DDA

Place before the DDA loop:

```smile
' DDA means Digital Differential Analyzer. Instead of moving the ray one
' tiny pixel at a time, it jumps directly to the next vertical or horizontal
' map-grid boundary. This makes finding the first wall much faster.
```

## 2.7 Perpendicular distance

Place before wall-distance selection:

```smile
' The camera-plane form gives us the forward distance to the wall.
' Using this perpendicular distance prevents the curved fish-eye look that
' appears when raw angled-ray distance is used for wall height.
```

## 2.8 Wall projection

Place before `WallHeight`:

```smile
' Nearby walls appear tall and distant walls appear short.
' Dividing a projection constant by the wall distance creates that effect.
```

## 2.9 Collision sliding

Place before movement:

```smile
' Test X and Y separately. If one direction is blocked but the other is open,
' the player slides along the wall instead of stopping completely.
```

## 2.10 Demo steering

Place before cross/dot steering:

```smile
' The demo follows a normal grid route, but movement remains continuous.
' The cross product tells whether the next cell center is left or right,
' while the dot product tells whether it is generally in front or behind.
```

---

# 3. Student guide content

`RAYCASTING_EXPLAINED.md` must explain these sections:

```text
1. What raycasting is
2. The top-down tile map
3. Player position and direction
4. Why fixed-point math is used
5. The camera plane
6. How one ray becomes one vertical strip
7. How DDA walks through the grid
8. Why wall distance changes wall height
9. Why the view is not true 3D
10. Collision and doors
11. How the demo can navigate a continuous camera
12. Experiments a student can safely try
13. What Stage 3 could add later
```

Use diagrams made from text where helpful.

Do not use copyrighted screenshots.

---

# 4. Safe student experiments

The guide should invite students to change:

```smile
RayCount
CameraPlaneMagnitude
ProjectionDistance
MoveUnitsPerSecond
TurnHalfStepsPerSecond
CeilingColor
FloorColor
wall palette arrays
```

Explain expected effects:

- lower ray count = chunkier but faster;
- larger camera plane = wider field of view;
- larger projection distance = taller walls;
- larger movement speed = faster travel;
- wall palettes change regions without textures.

Warn students not to:

- remove the solid map border;
- divide without checking zero;
- remove the DDA step limit;
- make `RayCount * StripWidth` differ from 960 without also adjusting layout.

---

# 5. Avoid over-engineering

Do not introduce:

- camera classes;
- renderer interfaces inside SMILE;
- entity-component systems;
- generic scene graphs;
- plugin systems;
- a reusable 3D engine;
- texture resource managers;
- object inheritance;
- complex code generation.

A handful of well-named routines and arrays is enough.

Suggested central routines:

```text
InitializeCamera
RotateCameraStep
CanOccupy
UpdatePlayer
CastRay
DrawRaycastView
TryOpenDoor
LoadSelectedMap
GenerateRandomMap
ValidateMap
PlanDemoRoute
UpdateDemoDriver
EnterTitle
```

---

# 6. Accuracy statement

The documentation should be honest:

- Dungeon Star II is a raycaster.
- It is often described as “2.5D.”
- The world remains a two-dimensional grid.
- All walls have the same floor-to-ceiling height.
- The game creates a first-person illusion by drawing vertical wall strips.
- It is not a general polygonal 3D engine.
