# SMILE 2.0 — `Draw Arc` Language and Runtime Implementation Instructions

**Repository:** `Sincioco/SMILE-2.0`  
**Local repository:** `D:\SMILE 2.0`  
**Latest observed commit while this specification was prepared:**  
`8bf94c3f81f8c444de64d830e419eb1899dd80f4`

**Primary immediate use:** Maze Muncher rounded maze walls  
**Feature type:** General-purpose SMILE graphics primitive  
**Approved syntax:**

```smile
Draw Arc CenterX, CenterY, Radius, StartAngle, SweepAngle, Color
```

---

# 1. Codex execution directive

Read `AGENTS.md` first.

Preserve all current Maze Muncher work and all newer commits.

Before editing:

```text
cmd /c git status --short
cmd /c git log -1 --oneline
```

The commit above is informational only.

If the repository is newer:

- do not reset;
- do not discard current Maze Muncher changes;
- adapt this specification to the actual architecture;
- keep `src\Smile.Language` as the sole language authority.

Implement `Draw Arc` as a coherent shared graphics capability.

After it builds and passes the light validation below:

1. commit it;
2. push it;
3. immediately resume the Maze Muncher implementation;
4. use the new primitive for rounded maze-wall corners.

Suggested commit subject:

```text
Sin and Codex: feat(graphics): add native arc drawing support
```

Do not wait for manual approval before continuing Maze Muncher.

---

# 2. Decision

The approved statement is:

```smile
Draw Arc CenterX, CenterY, Radius, StartAngle, SweepAngle, Color
```

This is the correct first arc primitive for SMILE 2.0.

It is:

- beginner-readable;
- consistent with `Draw Circle`, `Draw Line`, and other graphics statements;
- useful beyond Maze Muncher;
- simple to implement in both Direct2D and GDI;
- grounded in familiar BASIC-style graphics commands.

Do **not** add `Fill Arc` in this milestone.

Do **not** add an optional thickness parameter only for arcs.

The current graphics surface uses the normal one-logical-pixel outline stroke for lines, circles, rectangles, quadrilaterals, and rounded rectangles. Arc should follow the same rule.

Maze Muncher can create thicker, double-line, or glowing walls by drawing multiple concentric arcs with slightly different radii and colors.

---

# 3. Official semantics

## 3.1 Coordinates and radius

```text
CenterX, CenterY
```

identify the logical center of the circle containing the arc.

```text
Radius
```

is the logical radius measured from the center to the arc’s centerline.

Coordinates and radius use the same logical canvas units as the other graphics primitives.

The active backend scales them through the existing viewport.

## 3.2 Angles

Angles use integer degrees.

The coordinate convention is:

| Angle | Direction |
|---:|---|
| `0` | Right |
| `90` | Down |
| `180` | Left |
| `270` | Up |
| `360` | Right again |

This matches normal screen coordinates, where positive Y points downward.

The conceptual point formula is:

```text
PointX = CenterX + COS(Angle) * Radius
PointY = CenterY + SIN(Angle) * Radius
```

The trigonometry is implemented internally by the graphics backend. No `SIN()` or `COS()` SMILE language functions are required for this feature.

## 3.3 Sweep direction

Positive sweep angles move clockwise.

Negative sweep angles move counterclockwise.

Examples:

```smile
' Right to down, clockwise
Draw Arc 200, 200, 50, 0, 90, BLUE

' Right to up, counterclockwise
Draw Arc 200, 200, 50, 0, -90, BLUE
```

## 3.4 Angle normalization

`StartAngle` may be any integer expression.

Normalize it to the equivalent angle from `0` through `359`.

Examples:

```text
450  becomes 90
-90  becomes 270
```

## 3.5 Sweep limits

Use these simple runtime rules:

```text
SweepAngle = 0
    Draw nothing.

SweepAngle >= 360
    Draw one complete clockwise circle.

SweepAngle <= -360
    Draw one complete counterclockwise circle.

-359 through -1
    Draw the requested counterclockwise partial arc.

1 through 359
    Draw the requested clockwise partial arc.
```

A complete arc should visually match:

```smile
Draw Circle CenterX, CenterY, Radius, Color
```

Do not draw more than one revolution.

## 3.6 Invalid or degenerate values

```text
Radius <= 0
    Draw nothing.

Offscreen coordinates
    Clip safely through the existing viewport behavior.

Very large coordinates or angles
    Must never crash.
```

## 3.7 Stroke behavior

The arc uses:

- the same logical stroke thickness as existing outline primitives;
- the same viewport scaling;
- the same clipping;
- the same color representation;
- the same frame lifecycle.

The arc does not draw:

- a line to the center;
- a chord between endpoints;
- a filled pie slice;
- endpoint handles.

It draws only the curved outline.

---

# 4. Correct rounded-corner examples

The following examples are correct under the approved angle convention.

```smile
' Top-left corner:
' Start at the left point and sweep clockwise to the top point.
Draw Arc CornerX + Radius, CornerY + Radius, Radius, 180, 90, Blue

' Top-right corner:
' Start at the top point and sweep clockwise to the right point.
Draw Arc CornerX - Radius, CornerY + Radius, Radius, 270, 90, Blue

' Bottom-right corner:
' Start at the right point and sweep clockwise to the bottom point.
Draw Arc CornerX - Radius, CornerY - Radius, Radius, 0, 90, Blue

' Bottom-left corner:
' Start at the bottom point and sweep clockwise to the left point.
Draw Arc CornerX + Radius, CornerY - Radius, Radius, 90, 90, Blue
```

For a rounded rectangular maze-wall outline, connect the arc endpoints with ordinary `Draw Line` calls.

---

# 5. Maze Muncher glow and double-line examples

Do not add a thickness argument merely for Maze Muncher.

Use multiple arcs.

## 5.1 Simple glow

```smile
Draw Arc CenterX, CenterY, Radius + 2, StartAngle, SweepAngle, DARK_BLUE
Draw Arc CenterX, CenterY, Radius + 1, StartAngle, SweepAngle, BLUE
Draw Arc CenterX, CenterY, Radius, StartAngle, SweepAngle, LIGHT_BLUE
```

## 5.2 Double maze wall

```smile
Draw Arc CenterX, CenterY, OuterRadius, StartAngle, SweepAngle, BLUE
Draw Arc CenterX, CenterY, InnerRadius, StartAngle, SweepAngle, BLUE
```

The matching straight wall sections should use parallel `Draw Line` calls.

This lets students see that the rounded wall consists of the same two outlines as the straight wall.

---

# 6. Shared language implementation

## 6.1 `src\Smile.Language\Syntax.cs`

Add:

```text
ArcKeyword
```

Place it inside the normal keyword range, preferably near:

```text
CircleKeyword
QuadrilateralKeyword
LineKeyword
```

Add:

```csharp
["Arc"] = SyntaxKind.ArcKeyword
```

Do not create a Visual Studio-only keyword list.

The existing shared classifier should recognize it automatically.

## 6.2 `src\Smile.Language\GameSyntax.cs`

Add:

```text
GraphicsOperation.DrawArc
```

No `FillArc` operation is approved.

## 6.3 `src\Smile.Language\Parser.cs`

Extend `ParseGraphicsStatement`.

Recognize only:

```smile
Draw Arc CenterX, CenterY, Radius, StartAngle, SweepAngle, Color
```

Parse exactly six numeric expressions.

Recommended branch:

```text
else if (!isFill && Current.Kind == SyntaxKind.ArcKeyword)
```

Use:

```text
ParseFixedArguments(6)
```

`Fill Arc` must remain invalid and should produce the normal unsupported-fill-primitive diagnostic.

Preserve all existing graphics syntax.

## 6.4 `src\Smile.Language\Semantics.cs`

The current generic graphics-argument analysis should validate all six arguments as `Number`.

Do not add special semantic machinery unless needed for a clear diagnostic.

The runtime owns value behavior such as:

- radius less than or equal to zero;
- sweep clamping;
- angle normalization.

`Draw Arc` requires `Game Window`, like every other graphical drawing statement.

---

# 7. Compiler implementation

## `src\Smile.Compiler\MasmEmitter.cs`

Add:

```text
EXTERN smile_draw_arc:PROC
```

Map:

```text
GraphicsOperation.DrawArc
```

to:

```text
smile_draw_arc
```

Emit all six arguments through the current Windows x64 native-call emitter.

Expected native argument order:

```text
CenterX
CenterY
Radius
StartAngle
SweepAngle
Color
```

Do not change the general call emitter unless a failing test demonstrates a real defect.

---

# 8. Native C ABI

Add:

```c
void smile_draw_arc(
    long long center_x,
    long long center_y,
    long long radius,
    long long start_angle,
    long long sweep_angle,
    long long color);
```

Update the shared routing layer consistently.

Likely affected files include:

```text
src\Smile.NativeRuntime\runtime.c
src\Smile.NativeRuntime\graphics\graphics_backend.h
src\Smile.NativeRuntime\graphics\graphics_common.h
src\Smile.NativeRuntime\graphics\graphics_common.c
src\Smile.NativeRuntime\graphics\graphics_directx.cpp
src\Smile.NativeRuntime\graphics\graphics_gdi.c
src\Smile.NativeGraphicsTests\NativeGraphicsTests.c
```

Add a `draw_arc` entry to `SmileGraphicsBackendVTable`.

Update every real and mock vtable in exactly the same position.

The common router must:

- begin a frame when needed;
- dispatch to the selected backend;
- remain backend-neutral.

---

# 9. DirectX / Direct2D implementation

Use Direct2D.

## 9.1 Partial arc

Recommended implementation:

1. Return immediately when `radius <= 0` or `sweep_angle == 0`.
2. Normalize `start_angle`.
3. Clamp the sweep to one revolution.
4. Map the logical center and radius through the existing uniform viewport.
5. Convert start and end angles to radians.
6. Compute start and end points using screen-coordinate orientation:

   ```text
   X = CenterX + COS(Angle) * Radius
   Y = CenterY + SIN(Angle) * Radius
   ```

7. Create an `ID2D1PathGeometry`.
8. Open an `ID2D1GeometrySink`.
9. Begin the figure at the start point.
10. Add one `D2D1_ARC_SEGMENT`.
11. Use:

    ```text
    D2D1_SWEEP_DIRECTION_CLOCKWISE
    ```

    for a positive sweep.

12. Use:

    ```text
    D2D1_SWEEP_DIRECTION_COUNTER_CLOCKWISE
    ```

    for a negative sweep.

13. Use:

    ```text
    D2D1_ARC_SIZE_SMALL
    ```

    when the absolute sweep is at most 180 degrees.

14. Use:

    ```text
    D2D1_ARC_SIZE_LARGE
    ```

    when the absolute sweep is greater than 180 degrees.

15. End the figure as open.
16. Close the sink.
17. Draw with the existing cached solid brush.
18. Use the same scaled outline width as existing lines/circles.
19. Release the sink and geometry on every path.

## 9.2 Complete circle

When absolute sweep is at least 360 degrees:

- use the existing circle/ellipse drawing path; or
- draw two 180-degree arc segments.

Prefer reusing the existing circle drawing implementation so full-circle arc behavior matches `Draw Circle`.

## 9.3 Failure behavior

COM allocation or geometry errors must:

- fail safely;
- use existing backend diagnostics where appropriate;
- release all resources;
- not terminate the game.

Do not add an unbounded geometry cache.

Maze Muncher needs only a modest number of static quarter arcs, so the smallest correct implementation is preferred.

---

# 10. GDI implementation

Use the existing physical back buffer, viewport mapper, pen cache, and clipping behavior.

Recommended implementation:

1. Return when `radius <= 0` or `sweep_angle == 0`.
2. Normalize start angle.
3. Clamp sweep to one revolution.
4. Map center and radius to physical output coordinates.
5. Calculate the physical bounding rectangle.
6. Calculate physical start and end points.
7. Select:
   - the existing cached scaled pen;
   - `NULL_BRUSH`.
8. Save the current GDI arc direction.
9. Call:

   ```c
   SetArcDirection(
       dc,
       sweep_angle > 0 ? AD_CLOCKWISE : AD_COUNTERCLOCKWISE);
   ```

10. Call `Arc`.
11. Restore the previous arc direction.
12. Restore the previous pen and brush.

For an absolute sweep of at least 360 degrees, use `Ellipse` or the existing circle outline helper.

Include `<math.h>` only if needed for endpoint calculations.

Do not create and leak a new pen for every call.

---

# 11. Tests

Follow the permanent light-testing rule.

## 11.1 Shared language tests

Add focused checks for:

1. Valid `Draw Arc` analysis.
2. Operation is `GraphicsOperation.DrawArc`.
3. Six arguments are present.
4. Too few arguments produce a parser diagnostic.
5. Too many arguments are not silently ignored.
6. A non-number argument produces a semantic diagnostic.
7. `Fill Arc` is rejected.
8. Existing graphics statements remain valid.

Update the reported test count accurately.

## 11.2 Native graphics tests

Update the mock vtable and add a mock `draw_arc`.

Verify:

- dispatch reaches the active backend;
- all six values are forwarded in the correct order;
- the common router starts a frame consistently;
- Auto/DirectX/GDI selection tests remain intact.

Do not add a large arc test framework.

## 11.3 Graphics example

Update:

```text
examples\GraphicsBasics.smile
```

or add:

```text
examples\ArcBasics.smile
```

The example should visibly include:

- four 90-degree corners;
- a positive clockwise arc;
- a negative counterclockwise arc;
- a greater-than-180-degree arc;
- a complete 360-degree arc.

Compile it in the smoke suite.

---

# 12. Quick visual acceptance

Perform one brief DirectX run and one brief GDI run.

Verify:

- `0, 90` draws the bottom-right quarter;
- `0, -90` draws the top-right quarter;
- the four approved corner examples join their straight lines;
- an arc greater than 180 degrees chooses the long path;
- a full-circle arc matches `Draw Circle`;
- resize and Alt+Enter remain correct;
- no gaps appear at quarter-arc endpoints beyond ordinary one-pixel rasterization.

Do not run a long soak test.

---

# 13. Documentation

Update current documentation:

```text
README.md
docs\language\README.md
docs\architecture\README.md
```

Document:

```smile
Draw Arc CenterX, CenterY, Radius, StartAngle, SweepAngle, Color
```

Include the angle table:

```text
0 right
90 down
180 left
270 up
positive clockwise
negative counterclockwise
```

State that the arc uses the normal outline stroke and has no fill/chord/radial lines.

Add `Draw Arc` to the current graphics-capability list.

Do not rewrite historical milestone reports merely to add the new primitive.

---

# 14. Maze Muncher continuation

After the shared primitive is committed and pushed, resume Maze Muncher.

Use `Draw Arc` for:

- rounded outer maze corners;
- rounded inner wall corners;
- U-shaped passages;
- curved tunnel entrances;
- double-line neon wall outlines.

Keep Maze Muncher wall-generation logic in `.smile`.

Do not add a native `Draw MAZE WALL` helper.

The following quarter-circle examples are approved:

```smile
Draw Arc CornerX + Radius, CornerY + Radius, Radius, 180, 90, Blue
Draw Arc CornerX - Radius, CornerY + Radius, Radius, 270, 90, Blue
Draw Arc CornerX - Radius, CornerY - Radius, Radius, 0, 90, Blue
Draw Arc CornerX + Radius, CornerY - Radius, Radius, 90, 90, Blue
```

---

# 15. Definition of Done

- [ ] `Arc` is a shared SMILE keyword.
- [ ] `Draw Arc` parses exactly six numeric expressions.
- [ ] `Fill Arc` is invalid.
- [ ] Visual Studio receives syntax coloring and diagnostics through shared language facts.
- [ ] MASM emits `smile_draw_arc`.
- [ ] Stable C ABI exists.
- [ ] Backend-neutral router dispatches it.
- [ ] Direct2D renders partial, long, negative, and complete arcs.
- [ ] GDI renders the same angle convention.
- [ ] Positive sweep is clockwise.
- [ ] Negative sweep is counterclockwise.
- [ ] Zero sweep and non-positive radius are safe no-ops.
- [ ] Absolute sweep of 360 or more draws one circle.
- [ ] Native mock tests pass.
- [ ] Shared language tests pass.
- [ ] Smoke suite passes.
- [ ] Brief DirectX check passes.
- [ ] Brief GDI check passes.
- [ ] Documentation is current.
- [ ] Arc capability is committed and pushed.
- [ ] Codex resumes Maze Muncher afterward.

---

# 16. Required Codex report

Report:

1. starting commit;
2. arc commit hash;
3. branch pushed;
4. files changed;
5. exact syntax and semantics;
6. Direct2D implementation;
7. GDI implementation;
8. test counts/results;
9. smoke-suite result;
10. quick DirectX/GDI observations;
11. Maze Muncher files updated to use arcs;
12. checks deferred for Sin;
13. known limitations or `None identified.`
