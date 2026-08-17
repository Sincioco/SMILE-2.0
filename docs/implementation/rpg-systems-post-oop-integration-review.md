# RPGSystems Post-OOP Integration Review

**Review date:** August 18, 2026  
**Starting commit:** `719fe44e5a037a4dbeb3f78cc53014bc0ae1abad`

## Result

The accepted lightweight-OOP implementation remains frozen. The follow-up work
hardens application integration in `games\RPGSystems` without changing compiler,
runtime, public library, VSIX, or package-format behavior.

## Persistence finding and resolution

A dedicated test ApplicationId reproduced the expected SaveGames behavior:

```text
same application identity + same physical slot = later payload replaces earlier payload
```

The native proof printed `RPGSystems raw slot collision: REPRODUCED`. This is not
a Smile.RPG defect. The application now owns one nominal mapping:

```text
Management -> physical slot 1
Dungeon    -> physical slot 2
World      -> physical slot 3
```

The regression saves all three domains, mutates and reloads them, repeats saves
in another order, and verifies exact native/Web parity. Each subsystem retains
its existing schema policy.

## Initialization results

- Management now gates RPG creation, every definition and initial mutation,
  seven Menu façades, every required item, Navigator validity, and six submenu
  bindings.
- World now gates eight images, RPG/world/story/shop/encounter definitions, two
  Menu/Navigator pairs, Dialogue, three maps and their tiles, and initial
  progress/spawn/camera synchronization.
- Battle now gates five images, all character/party/item/ability/effect/status/
  enemy/formation/AI definitions, world scene/spawn/actor setup, and its initial
  standing-order strategy.
- Dungeon retains and extends its cumulative gate with image validity while
  preserving Menu, Navigator, Dialogue, Animation, map/tile, definition,
  workflow, and initial presentation validation.

Every ordinary failure returns before the interactive loop, then follows the
same guarded Shutdown path as an ordinary Escape return.

## Re-entry and cleanup

All four Modules reset run-sensitive globals at entry and clear them at shutdown.
Battle, Dungeon, and World stop both SFX and music. UI Class references are
destroyed when present and then set to `Nothing`. Dungeon releases all Animation
and TileMap handles; World releases all TileMap handles. Each system unloads its
images and destroys its RPG state.

The bounded failure fixture exhausts all four RPG state slots before invoking
each system, then verifies a new state can be allocated. Separate Menu-capacity
cases force incomplete Management and World UI initialization and prove every
partially acquired Menu slot becomes available again.

Native lifetime diagnostics finish with:

```text
SMILE_CLASS_LIVE=0
SMILE_IMAGE_LIVE=0
SMILE_TEXT_LIVE=0
```

The final normal-entry DirectX sequence and brief GDI interaction are recorded
in the milestone handoff report after hands-on validation.

## Automated coverage

`scripts\test-rpg-systems-integration.ps1` is the focused permanent gate. It
performs persistence and capacity fixture compilation/execution on native and
Web, exact output checks, lifetime checks, production DirectX/GDI/Web builds,
generated-JavaScript validation, short Web runtime execution, and source/project
contract assertions.

The normal smoke workflow runs both:

```text
scripts\test-lightweight-oop-hardening.ps1
scripts\test-rpg-systems-integration.ps1
```

exactly once in its early focused-gate section.

## Versions

This application-only milestone does not change:

- Smile.UI `2.0.0`;
- Smile.Game `2.0.0`;
- Smile.RPG `1.2.1`;
- `.smilelib` format `6`;
- VSIX `2.0.48`.

## Frozen and deferred work

Inheritance, interfaces, overloads, user finalizers, Class-reference fields,
Class arrays, cycles, and tracing garbage collection remain deferred. No public
SaveGames namespace API or additional save-slot capacity is introduced. If the
gallery outgrows the finite application mapping, revisit the application design
with a concrete requirement rather than expanding Smile.RPG speculatively.
