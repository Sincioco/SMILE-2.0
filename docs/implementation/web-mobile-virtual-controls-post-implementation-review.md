**Flag:** SMILE 2.0 has keyboard input in Web games but no reusable touch-screen virtual-control adapter. Mobile Web players therefore need the Web runtime to translate pointer presses into existing Get Key and Key_Held(...) behavior.

# Web and Mobile Virtual Controls — Post-Implementation Review

## Baseline and delivery

- Reviewed handoff baseline: `d1893fbd24226cd22e856939b8af4dee95c719f7`.
- Actual starting SHA for this follow-up: `eef67f180d4f7c9d0a6fead05ceca80c0c2247ac`.
- Implementation milestone: `7e1a9b818fbc5f5d32fdeeda094d8d041021966c` (`Sin and Codex: feat(web): harden gamepad controls and Tetris`).
- HTML-versioning hardening milestone: `19afbd4a8d2fedb7aaed13ef4633ae70a9c15ec7` (`Sin and Codex: fix(web): version the HTML template`).
- Both milestones were pushed to `origin/main`.
- The existing uncommitted `SMILE 2.0.sln` edit was preserved exactly and excluded from every commit.

The user-supplied physical-phone screenshots showed the controller's number strip and lower controls falling behind mobile browser chrome, landscape clipping, font/emoji variation in left and right arrows, and poor portrait composition. Follow-up reports also identified B terminating a game, Web music continuing while iPhone Chrome was backgrounded, stale Web deployments, action buttons that could not be distinguished, and inconsistent audio unlock between D-pad and action buttons.

## Result

Generated graphical Web games now expose one reusable D-pad plus A/B/X/Y virtual controller. Visibility defaults to Auto, remains hidden until `Game Window`, and supports `?smile-controls=on`, `?smile-controls=off`, and `?smile-controls=auto`. Console programs remain unchanged.

Touch, pen, and primary-mouse Pointer Events enter the same source-aware broker as keyboard input. A press queues exactly one bounded `Get Key` event; `Key_Held(...)` remains true until every source owning that logical key releases. Multiple pointers can hold direction and action simultaneously.

The controller is general runtime infrastructure. It contains no game-name branch. The Tetris source uses the shared pad constants to choose its own controls.

## Input architecture

- Auto capability detection combines `navigator.maxTouchPoints`, `pointer: coarse`, and `hover: none`.
- An observed touch or pen Pointer Event can reveal Auto controls on hybrid hardware.
- No user-agent parsing was added.
- `keyboard:<code>` and `pointer:<pointerId>` sources own logical values through one bounded broker.
- The key queue remains bounded at 256 entries and active input sources at 32.
- Duplicate pointer-down is idempotent and does not create duplicate queue events.
- Release and cleanup cover pointer-up, pointer-cancel, lost pointer capture, orientation change, blur, visibility loss, controls hiding, page hide, runtime finish, and runtime failure.
- Primary-mouse presses work whenever the virtual controls are visible. Desktop Auto remains hidden; Forced On provides deterministic mouse testing and reuse.
- Discrete press, hold, release, cancellation, and multi-touch gestures are supported. Analog motion, free coordinates, swipes, and arbitrary gesture recognition are not new language/runtime APIs.

## Standard pad profile

| Control | SMILE constant | Numeric value |
|---|---|---:|
| Up | `KEY_UP` | 10 |
| Down | `KEY_DOWN` | 11 |
| Left | `KEY_LEFT` | 12 |
| Right | `KEY_RIGHT` | 13 |
| A | `KEY_PAD_A` | 23 |
| B | `KEY_PAD_B` | 24 |
| X | `KEY_PAD_X` | 25 |
| Y | `KEY_PAD_Y` | 26 |

The immutable internal `standard` profile maps one control to one value. A/B/X/Y are distinct pad-only values, so games can interpret each independently. Physical keyboard keys do not emit pad-only values. B no longer emits Escape, so pressing it cannot terminate a game and hide the controller. Physical Escape retains its existing behavior.

The profile seam is intentionally small. It does not infer arbitrary custom keyboard layouts or same-device multiplayer mappings. A title where players use different direction/action layouts needs a future explicit profile or game-declared mapping.

## Layout, accessibility, and gestures

- The retired 1/2/3/4 strip was removed; exactly eight controls remain.
- All controls are real `button` elements with `type="button"`, descriptive labels, `aria-pressed`, and visible focus treatment.
- Primary targets are at least 56 by 56 CSS pixels.
- Control outlines use the requested fainter `rgba(220, 247, 255, 0.56)` resting border.
- Safe-area insets are applied in portrait and landscape.
- Landscape positions controls against the dynamic visible viewport.
- Portrait vertically centers the game canvas and controls as one composition.
- All directions use the same zero-size CSS triangle rotated around its geometric center; no Unicode or emoji arrow glyph remains.
- `touch-action: none` is scoped to controller buttons. Browser zoom is not disabled globally.
- Blank overlay space remains pointer-transparent.

## Audio lifecycle

- Every accepted D-pad or A/B/X/Y press marks user interaction and invokes the same requested-music synchronization path.
- Showing the controller by itself does not start media.
- Web music pauses, rather than merely becoming inaudible, while the page is blurred or hidden.
- Foreground focus resumes requested music unless the program explicitly paused or stopped it.
- Runtime finish, runtime failure, and page hide retain idempotent cleanup.
- Native Windows audio focus behavior was not changed.

## Deployment freshness

Every Web compilation computes a deterministic 16-hex build marker from the unversioned HTML template, shared runtime, emitted game, and CSS. `index.html` contains the marker plus versioned references to `smile.css`, `smile-runtime.js`, and `game.js`.

The runtime performs a same-page `cache: "no-store"` freshness request on `pageshow` and active-window focus. If the deployed marker changes, it preserves existing query options and reloads through a `smile-version=<marker>` URL. Generated no-cache metadata also requests revalidation of `index.html`.

HTTP response headers remain authoritative. A first deployment from an older compiler can require one manual refresh because an already-cached old page cannot contain the new freshness logic.

The generated host-file contract is still exactly:

```text
index.html
smile-runtime.js
game.js
smile.css
```

Project publication may additionally copy declared assets and `smile-assets.json`; no new generated host file or framework was introduced.

## SMILE 2.0 Tetris

- The tracked game/project folder is now `games/Tetris`.
- Project files are `Tetris.smileproj` and `Tetris.slnx`.
- The window, title, panels, documentation, build scripts, artifact verification, and repository references use the exact display name `SMILE 2.0 Tetris`.
- X rotates counterclockwise/left.
- B rotates clockwise/right.
- A and Y hard-drop.
- Down/S soft-drop; left/right movement and existing keyboard controls remain available.
- Both demo and no-demo sources use the same mapping.
- Native outputs are `artifacts/games/Tetris/Tetris.exe` and `Tetris-NoDemo.exe`.
- Web outputs are `artifacts/web/Tetris` and `Tetris-NoDemo`.

## Syntax and compatibility impact

- Syntax added: `KEY_PAD_A`, `KEY_PAD_B`, `KEY_PAD_X`, and `KEY_PAD_Y`.
- Constant values: 23, 24, 25, and 26 respectively.
- Package format change: None.
- New native input mapping: None; the Windows keyboard runtime does not emit pad-only values.
- New game-specific Web runtime branch: None.
- New generated Web file: None.
- New npm dependency or browser framework: None.
- New native runtime behavior: None.

## Files changed

- `AGENTS.md`
- `README.md`
- `docs/architecture/README.md`
- `docs/architecture/web-mobile-virtual-controls.md`
- `docs/implementation/web-mobile-virtual-controls-post-implementation-review.md`
- `docs/language/README.md`
- `docs/language/phase4-media.md`
- `docs/testing/direct2d-manual-test-checklist.md`
- `docs/testing/mp3-music-manual-test-checklist.md`
- tracked `games/FallingBlocks/**` content moved/renamed to `games/Tetris/**`
- `games/Tetris/Program.smile`
- `games/Tetris/Program-NoDemo.smile`
- `games/Tetris/README.md`
- `games/Tetris/Tetris.smileproj`
- `games/Tetris/Tetris.slnx`
- `scripts/generate-sounds.ps1`
- `scripts/run-web-test.js`
- `scripts/smoke-test.cmd`
- `scripts/verify-artifacts.ps1`
- `src/Smile.Compiler/WebOutputWriter.cs`
- `src/Smile.Language/Syntax.cs`
- `src/Smile.Tests/Program.cs`

## Automated validation

All commands ran from `D:\SMILE 2.0` against the final implementation.

```text
cmd /c scripts\smoke-test.cmd
PASS (exit 0)
```

The normal repository gate reported:

- 285 SMILE language, compiler, project, completion, and timing tests passed.
- 13 focused formatter integration tests passed.
- SMILE style check passed for 278 files.
- 39 native graphics and audio-focus checks passed.
- 38 native Text runtime checks passed.
- 92 Phase 8 dungeon map topology checks passed.
- Native/Web parity, Phase 4 media, Phase 5 UI, publication rollback, formatter safety, package/library, RPG phase, viewport, and DPI validations passed.
- All seven bundled game demo/no-demo native and Web builds passed.
- Tetris and Tetris-NoDemo native x64 GUI verification passed.
- Game asset copies passed.
- VSIX payload/version verification passed at 2.0.48.

Focused final Web checks:

```text
node --check artifacts\web\Tetris\smile-runtime.js
PASS (exit 0)

node scripts\run-web-test.js artifacts\web\Tetris --mobile-controls --timeout 10000
PASS (exit 0): Web execution passed: D:\SMILE 2.0\artifacts\web\Tetris (mobile virtual controls)

node scripts\run-web-test.js artifacts\web\Tetris --frames 3 --timeout 10000
PASS (exit 0): Web execution passed: D:\SMILE 2.0\artifacts\web\Tetris

git diff --check
PASS: no whitespace errors; only repository line-ending notices were emitted.
```

The focused dependency-free harness verifies:

- Auto/On/Off query behavior and `Game Window` visibility gating;
- capability detection without `userAgent`;
- distinct values for all four action buttons;
- one queued event per press and correct held state across multiple sources;
- simultaneous multi-touch direction/action presses;
- 256-entry queue and 32-source bounds;
- every D-pad and action button unlocking requested music;
- hidden/blurred music pause and foreground resume;
- pointer-up, cancellation, lost capture, blur, visibility, hide, page-hide, finish, and failure cleanup;
- physical Escape behavior remaining independent;
- runtime failure displaying the existing error panel after cleanup.

## Manual browser validation

Performed against the freshly generated `artifacts/web/Tetris` output with `?smile-controls=on` in the Codex in-app desktop browser:

- Final build marker: `01ddca9b7b49d462`.
- 390 by 844 CSS-pixel portrait: eight buttons, minimum 56 by 56, all in bounds, no scroll, and combined canvas/controller center exactly 422, matching the viewport center.
- 844 by 390 CSS-pixel landscape: all buttons in bounds, no overlap, and no scroll.
- Every arrow measured a 0-pixel X and Y center offset from its button in both orientations.
- A/B/X/Y were exercised through the real button path; B left controls visible and no runtime error appeared.
- Action buttons exposed independent accessible labels.
- Resting outline matched the requested translucent border.

The deterministic harness, rather than sequential desktop clicking, proves simultaneous multi-touch, cancellation, source bounds, audio lifecycle, and background/foreground behavior.

No post-fix physical Android or iPhone test was performed or claimed. The supplied screenshots are evidence of the pre-fix device failure, not a post-fix device pass.

Remaining physical-device checks:

- iPhone Chrome background/foreground music pause and resume;
- iPhone Chrome portrait/landscape safe-area and browser-chrome behavior;
- Android Chrome portrait/landscape safe-area behavior;
- physical two-finger direction plus action;
- first-touch audio unlock from every action button;
- home-indicator/display-cutout clearance.

## Regression evidence and known limitations

- Desktop Web keyboard behavior remains unchanged. Existing keyboard, modifier, repeat-suppression, queue-bound, Phase 4, Phase 5, publication, and smoke tests passed.
- Windows native behavior remains unchanged. The language accepts four new named constants, but the native keyboard runtime does not emit them. Native builds and verification passed.
- Console programs remain unchanged because controls cannot appear before `Game Window`.
- Generated host files remain exactly the established four-file contract.
- Custom and local-multiplayer layouts are not automatically inferred. The standard profile is a deliberate default, not a mapping-discovery system.
- Touch support is discrete digital control input; analog sticks, swipe recognition, and arbitrary gesture coordinates are outside this change.
