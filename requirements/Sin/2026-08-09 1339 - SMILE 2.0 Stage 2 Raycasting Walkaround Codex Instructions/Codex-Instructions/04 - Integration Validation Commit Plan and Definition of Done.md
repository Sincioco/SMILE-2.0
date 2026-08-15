# Integration, Validation, Commit Plan, and Definition of Done

Follow the repository’s permanent velocity rule.

Assume the happy path.

Do not run long tests unless investigating a known problem.

---

# 1. Expected repository changes

Create:

```text
games\DungeonStarII\DungeonStarII.smileproj
games\DungeonStarII\DungeonStarII.slnx
games\DungeonStarII\Program.smile
games\DungeonStarII\README.md
games\DungeonStarII\MAP_AUTHORING.md
games\DungeonStarII\RAYCASTING_EXPLAINED.md
games\DungeonStarII\Maps\default.map
games\DungeonStarII\Maps\custom.map
scripts\validate-raycasting-maps.ps1
```

Update current files as applicable:

```text
AGENTS.md
README.md
docs\architecture\README.md
docs\language\README.md
scripts\smoke-test.cmd
scripts\verify-artifacts.ps1
```

Do not change language/compiler/runtime files unless a genuine blocker is demonstrated.

The approved implementation should work with the current SMILE language.

---

# 2. Current documentation

Add Dungeon Star II to the current game list.

Describe it as:

```text
an original educational fixed-point raycasting walkaround with
continuous movement, editable maps, open rooms, random fallback,
doors, and a self-playing attract mode
```

Update the game count according to the actual repository at implementation time.

Do not rewrite historical milestone reports solely to change an earlier game count.

Update `AGENTS.md` so Dungeon Star II remains a `.smile` source proof.

Do not weaken the generic-runtime rule.

---

# 3. Smoke integration

The smoke suite must:

1. run the raycasting map validator;
2. compile `games\DungeonStarII\Program.smile`;
3. copy the `Maps` directory;
4. verify `default.map`;
5. verify `custom.map`;
6. verify `DungeonStarII.exe` exists;
7. include it in native artifact verification;
8. preserve all existing tests and game builds.

Expected output:

```text
artifacts\games\DungeonStarII\DungeonStarII.exe
artifacts\games\DungeonStarII\Maps\default.map
artifacts\games\DungeonStarII\Maps\custom.map
```

---

# 4. Focused automated validation

Required:

```text
cmd /c scripts\smoke-test.cmd
```

The map validator must pass.

No new shared-language tests are necessary when no language syntax changes.

A small deterministic math check may be added only if it remains simple, such as verifying:

- cardinal camera directions;
- camera plane perpendicularity at cardinal directions;
- strip width times ray count equals 960;
- rotation snapping values.

Do not build a headless 3D-render test framework.

---

# 5. Short manual happy path

Use one brief DirectX run:

1. Launch title.
2. Confirm `Default.MAP`, `CUSTOM.MAP`, and `Random MAP`.
3. Start `Default.MAP`.
4. Walk forward and backward.
5. Turn continuously left and right.
6. Enter a large room.
7. Slide along a wall at an angle.
8. Open one door.
9. Return to title.
10. Start `CUSTOM.MAP`.
11. Start `Random MAP`.
12. Temporarily rename `custom.map`; confirm fallback.
13. Restore the file.
14. Wait five seconds on title; confirm demo.
15. Confirm demo walks, turns, and opens a door.
16. Press a key; confirm return to title.
17. Toggle Alt+Enter once.

Use one brief GDI run:

1. Start default map.
2. Move and turn.
3. Confirm wall strips, floor, ceiling, and text render.
4. Exit.

No long playthrough is required.

---

# 6. Short performance observation

With normal VSync:

- movement should respond continuously;
- turning should not visibly pause;
- wall strips should fill the screen without gaps;
- no ray should escape the map;
- no division-by-zero crash;
- no unbounded DDA loop.

If performance is visibly poor:

1. enable existing graphics diagnostics;
2. observe a short run;
3. identify whether the bottleneck is ray count, game-side DDA, or backend drawing;
4. make the smallest correction;
5. document any reduction from 240 rays.

Do not run a long benchmark by default.

---

# 7. Map compatibility check

Copy a valid Dungeon Star I `.map` file to:

```text
games\DungeonStarII\Maps\custom.map
```

Build/copy assets and select `CUSTOM.MAP`.

Confirm:

- floor 1 loads;
- `#`, `.`, `D`, `O`, start markers, `U`, and `V` are accepted;
- later floors are ignored;
- the result remains narrow because the original map is narrow.

Restore the supplied `custom.map` afterward.

This is one brief compatibility check, not a permanent duplicate asset.

---

# 8. Code-review checklist

Before commit, inspect `Program.smile`.

Confirm:

- raycasting is not hidden in native code;
- no new language syntax was added unnecessarily;
- comments explain fixed point, camera plane, DDA, distance, projection, collision, and demo steering;
- DDA has a hard step limit;
- zero ray directions use `HugeDistance`;
- map access is bounds-aware;
- movement is fixed-step;
- player collision samples radius;
- X/Y collision is separate;
- demo uses normal movement;
- no commercial assets/names are present;
- no enemies, weapons, or textures slipped into scope.

---

# 9. Commit strategy

A single coherent commit is acceptable:

```text
Sin and Codex: feat(game): add Dungeon Star II raycasting walkaround
```

A two-commit sequence is also acceptable when it improves reviewability:

```text
Sin and Codex: feat(game): add fixed-point raycasting walkaround

Sin and Codex: docs(game): add raycasting and map lessons
```

Every commit must use the detailed body required by `AGENTS.md`.

Build and smoke test before pushing.

Do not amend or force-push.

---

# 10. Definition of Done

## Project

- [ ] Folder is `games\DungeonStarII`.
- [ ] Output is `DungeonStarII.exe`.
- [ ] Window title identifies Dungeon Star II and Raycasting Walkaround.
- [ ] Complete game behavior is in `Program.smile`.
- [ ] Project copies the Maps directory.

## Raycaster

- [ ] 240 rays by default, or a documented measured reduction.
- [ ] Full 960-by-540 view.
- [ ] Fixed-point camera direction and plane.
- [ ] Half-degree rotation matrix.
- [ ] Bounded DDA.
- [ ] Perpendicular distance.
- [ ] Distance-to-wall-height projection.
- [ ] Original wall palettes.
- [ ] Side and distance shading.
- [ ] Depth array.
- [ ] Solid ceiling and floor.
- [ ] No textures.

## Movement

- [ ] Continuous forward/backward movement.
- [ ] Continuous left/right turning.
- [ ] Refresh-independent fixed simulation.
- [ ] Player-radius collision.
- [ ] Wall sliding.
- [ ] Closed doors block movement.
- [ ] Door use works from the center view.

## Maps

- [ ] `default.map` exists and contains open rooms.
- [ ] `custom.map` exists and is student-editable.
- [ ] Title loads both files.
- [ ] Random map option exists.
- [ ] Missing/invalid files fall back safely.
- [ ] Map parser is written in SMILE.
- [ ] Dungeon Star I floor-one compatibility works.
- [ ] Open 2-by-2 areas are allowed.
- [ ] Map guide is committed.

## Random generation

- [ ] Rectangular rooms.
- [ ] Connecting corridors.
- [ ] Extra loops.
- [ ] Doors.
- [ ] Several wall types.
- [ ] Valid start.
- [ ] Connectivity validation.
- [ ] Bounded retry.
- [ ] Deterministic fallback.
- [ ] Not a tube-only generator.

## Demo

- [ ] Starts after five seconds.
- [ ] Uses selected map source.
- [ ] Walks continuously.
- [ ] Turns continuously.
- [ ] Opens doors.
- [ ] Uses BFS route cells plus continuous steering.
- [ ] Runs approximately 45 seconds.
- [ ] Shows five-second completion overlay.
- [ ] Any key cancels and is consumed.

## Education

- [ ] Required comments appear in `Program.smile`.
- [ ] `RAYCASTING_EXPLAINED.md` is committed.
- [ ] `MAP_AUTHORING.md` is committed.
- [ ] No over-engineered framework.
- [ ] No copied commercial content.

## Integration

- [ ] Map validator passes.
- [ ] Smoke suite passes.
- [ ] DirectX short happy path passes.
- [ ] GDI short happy path passes.
- [ ] Native x64 GUI/no CLR verification passes.
- [ ] Current documentation is updated.
- [ ] Commit is pushed.

---

# 11. Final Codex report

Report:

1. starting commit;
2. final commit hash or hashes;
3. branch pushed;
4. files added/changed/deleted;
5. confirmation that no new language syntax was required;
6. fixed-point scales used;
7. ray count and strip width;
8. DDA design;
9. map-format compatibility;
10. random-generation design;
11. demo-navigation design;
12. executable path;
13. smoke-suite result;
14. brief DirectX result;
15. brief GDI result;
16. any performance adjustment;
17. checks deferred for the user;
18. known limitations or `None identified.`
