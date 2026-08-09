# Dungeon Star II - Raycasting Walkaround

Dungeon Star II is an original educational fixed-point raycasting walkaround written entirely in SMILE 2.0. Its one-floor 31-by-31 maps contain open rooms, corridors, loops, colored wall materials, pillars, and rising doors. The 960-by-540 first-person view casts 960 one-pixel rays, merges consecutive hits on the same wall plane into anti-aliased quadrilaterals, and combines bounded DDA traversal with stable side shading, continuous movement, player-radius collision, and wall sliding.

Two complete teaching sources are included:

- `Program.smile` is the normal startup source and includes the five-second attract demo, breadth-first route planning, continuous demo steering, door use, cancellation, and direct return to the title.
- `Program-NoDemo.smile` preserves the complete user walkaround while removing demo AI, lifecycle, timing, cancellation, and demo-only UI.

To build the simpler edition in Visual Studio, change `<StartupFile>` in `DungeonStarII.smileproj` to `Program-NoDemo.smile`.

## Controls

- W/S or Up/Down: move continuously forward/backward.
- A/D or Left/Right: turn continuously.
- Enter or Space: open the closed door in the center view.
- Escape: return to the title; Escape again exits.
- Alt+Enter: toggle borderless full screen through the shared runtime.

The title reloads `Maps\default.map` or `Maps\custom.map` whenever that source starts. `RANDOM MAP` generates connected rectangular rooms, corridors, loops, wall materials, and doors. A missing or invalid external map safely uses the same random fallback.

Read `RAYCASTING_EXPLAINED.md` for the fixed-point camera and DDA lesson. Read `MAP_AUTHORING.md` to edit maps or import the first floor of a Dungeon Star I map.
