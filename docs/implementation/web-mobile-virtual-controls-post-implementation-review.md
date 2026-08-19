# Web and Mobile Virtual Controls — Post-Implementation Review

## Capability flag

**Flag:** SMILE 2.0 has keyboard input in Web games but no reusable touch-screen virtual-control adapter. Mobile Web players therefore need the Web runtime to translate pointer presses into existing Get Key and Key_Held(...) behavior.

The implementation adds that adapter inside the generated Web host. No new SMILE syntax is required.

## Baseline

- Starting SHA: `d1893fbd24226cd22e856939b8af4dee95c719f7`
- Pushed implementation SHA: `eb761d6664c10580335c4ff61cca25f912fb0c06`
- Remote branch: `origin/main`
- Reviewed baseline reconciliation: the actual starting SHA matched the reviewed handoff baseline, so no newer committed code required reconciliation.
- Working-tree conditions preserved: `games/SinStarI/SinStarI.slnx` had a pre-existing uncommitted project-ID change from `04226d7c-539f-ffac-44e1-6fc96d8b916b` to `e8f9efbb-55af-b457-a3a2-89484937cc71`. Build tooling rewrote it once; the captured change was restored exactly and excluded from both feature milestones.

## Result

Generated graphical Web games now provide a reusable controller with a D-pad, A/B/X/Y, and number buttons 1 through 4. The overlay defaults to Auto, stays hidden until `Game Window`, appears for touch-first capability, can reveal after observed touch or pen input on a hybrid device, and supports deterministic `?smile-controls=on`, `off`, and `auto` overrides.

Touch, pen, and primary mouse button gestures enter the same bounded broker as keyboard events. A press queues one existing SMILE key value and retains held state until that source releases. Multiple pointers and keyboard-plus-pointer ownership work without prematurely releasing a shared key.

## Architecture implemented

- Visibility modes: missing, malformed, unknown, or duplicated query state becomes `auto`; `on` and `off` are deterministic; Forced Off wins over capabilities and observed pointers.
- Capability detection: positive `navigator.maxTouchPoints` is combined with `pointer: coarse` or `hover: none`; an observed touch or pen Pointer Event reveals Auto controls on hybrid hardware. There is no user-agent parsing.
- Shared input broker: `keyboard:<code>` and `pointer:<pointerId>` sources own logical key values through a `Map`; a per-key reference count supplies `Key_Held(...)`. The `Get Key` queue remains bounded at 256 and active sources are bounded at 32.
- Pointer lifecycle: accepted Pointer Events use capture when available and release through pointer-up, pointer-cancel, lost capture, orientation change, blur, document visibility loss, page hide, runtime finish, runtime failure, or controls hiding.
- Mouse support: a primary mouse press is accepted whenever the controller is visible. Desktop Auto remains hidden; Forced On supports deterministic desktop use, and touch-first devices with an attached mouse can use the same controls.
- Layout and safe areas: the controller is an overlay that does not change logical canvas size or aspect ratio. CSS uses safe-area inset variables and separate portrait/landscape positioning.
- Accessibility: all 12 controls are real buttons with `type="button"`, descriptive labels, `aria-pressed`, visible focus styling, and targets at least 56 by 56 CSS pixels.
- Gesture containment: overlay blank space uses `pointer-events: none`; buttons use `touch-action: none`. No page-wide touch suppression or zoom restriction was added.
- Audio interaction: only an accepted virtual press marks user interaction and calls the existing `syncMusic()` path; showing controls alone does not start playback.
- Diagnostics: existing Web media diagnostics gained controls mode/visibility, pointer/source/queue counts, and finite limits. No device identity or pointer-coordinate data is recorded.
- Generated file contract: publication remains exactly `index.html`, `smile-runtime.js`, `game.js`, and `smile.css`.

## Standard mapping

| Control | Existing SMILE key | Numeric value |
|---|---|---:|
| Up | `KEY_UP` | 10 |
| Down | `KEY_DOWN` | 11 |
| Left | `KEY_LEFT` | 12 |
| Right | `KEY_RIGHT` | 13 |
| A | `KEY_ENTER` | 14 |
| B | `KEY_ESCAPE` | 15 |
| X | `KEY_SPACE` | 16 |
| Y | `KEY_TAB` | 21 |
| 1 | `KEY_1` | 17 |
| 2 | `KEY_2` | 18 |
| 3 | `KEY_3` | 20 |
| 4 | `KEY_4` | 22 |

The mapping is an immutable internal `standard` profile. One press maps to one value; no arrow/WASD aliases are enqueued together.

## Files changed

Implementation milestone `eb761d6664c10580335c4ff61cca25f912fb0c06`:

- `src/Smile.Compiler/WebOutputWriter.cs`
- `scripts/run-web-test.js`
- `scripts/smoke-test.cmd`
- `src/Smile.Tests/Program.cs`
- `docs/language/phase4-media.md`
- `docs/architecture/web-mobile-virtual-controls.md`

Evidence-review milestone:

- `docs/implementation/web-mobile-virtual-controls-post-implementation-review.md`

No game source, shared language source, native runtime source, package code, or Visual Studio language-service source changed.

## Language and package impact

- New SMILE syntax: `None`
- Package format change: `None`
- Native runtime behavior change: `None`
- Visual Studio language-service change: `None`
- Game-specific runtime branch: `None`
- New generated Web file: `None`
- New dependency or browser framework: `None`

## Automated validation

All commands ran from `D:\SMILE 2.0`.

```text
cmd /c scripts\build.cmd
PASS (exit 0): compiler, native runtime, native test executables, managed tests, Visual Studio extension, and VSIX built. The existing NU1503 native-vcxproj restore warning was emitted; compilation succeeded with no feature error.

dotnet run --project src\Smile.Tests\Smile.Tests.csproj -c Release --no-restore
PASS (exit 0): 284 SMILE language, compiler, project, completion, and timing tests passed. Expected synthetic failure diagnostics used by rollback tests were observed.

artifacts\compiler\smilec.exe examples\ArcBasics.smile --target web --output-dir artifacts\temp\MobileVirtualControlsWeb
PASS (exit 0): generated the four Web output files.

node --check artifacts\temp\MobileVirtualControlsWeb\smile-runtime.js
PASS (exit 0): no JavaScript syntax error.

node scripts\run-web-test.js artifacts\temp\MobileVirtualControlsWeb --mobile-controls --timeout 10000
PASS (exit 0): "Web execution passed: D:\SMILE 2.0\artifacts\temp\MobileVirtualControlsWeb (mobile virtual controls)".

node scripts\run-web-test.js artifacts\temp\MobileVirtualControlsWeb --frames 3 --timeout 10000
PASS (exit 0): "Web execution passed: D:\SMILE 2.0\artifacts\temp\MobileVirtualControlsWeb".

cmd /c scripts\smoke-test.cmd
PASS (exit 0): included the new permanent mobile-control invocation, 284 managed tests, 13 formatter integration tests, the 278-file style gate, 39 native graphics/audio-focus checks, 38 native Text checks, retained native/Web parity and rollback/publication gates, Phase 4 and Phase 5 Web harness modes, all seven game demo/no-demo native and Web builds, native x64 GUI inspection, viewport/DPI checks, and VSIX payload/version verification.

git diff --check
PASS: no whitespace errors; Git emitted only the repository's line-ending conversion notices.
```

The managed output contract test additionally proves one hidden controls root, all 12 symbolic controls exactly once, real button types, `viewport-fit=cover`, the scoped `touch-action` rule, the absence of global zoom suppression, the query contract, the absence of `userAgent`, and the unchanged four-file managed list.

## Focused hardening evidence

The dependency-free `--mobile-controls` mode executes isolated generated-runtime hosts and passed these deterministic cases:

- ordinary desktop Auto remains hidden before and after `Game Window`, while keyboard queue and held state continue to work;
- touch-first Auto becomes visible only after `Game Window`;
- Forced On, Forced Off, unknown-query fallback, duplicated-query fallback, and hybrid pen/touch reveal behave as specified;
- every D-pad, action, and number button maps to its documented numeric value;
- duplicate pointer-down is idempotent and one press creates one queue entry;
- two pointers simultaneously hold direction and action with ordered queue events;
- pointer release preserves a keyboard owner of the same key;
- pointer-cancel, lost capture, blur, visibility loss, orientation change, page hide, runtime finish, and runtime failure clear sources and `aria-pressed` state;
- 300 keyboard presses leave exactly the newest 256 queue entries;
- the 33rd simultaneous pointer is ignored at the 32-source bound;
- unknown controls, blank overlay space, and non-primary mouse buttons are ignored;
- showing controls does not unlock music, while an accepted press follows the existing audio synchronization path;
- runtime failure displays the established error panel after clearing controls and input.

## Manual validation

Actually performed in the Codex in-app desktop browser against the freshly generated Sin Star I Web output at `?smile-controls=on`:

- the controller appeared after game initialization with 12 accessible buttons;
- Move Up, A/Enter, B/Escape, X/Space, and Number 1 accepted primary-mouse clicks;
- all buttons returned to `aria-pressed="false"` with no stuck visual press;
- browser logs contained no warning or error;
- every target measured at least 56 CSS pixels;
- a 390×844 CSS-pixel portrait viewport kept all buttons in bounds with no pairwise overlap;
- an 844×390 CSS-pixel landscape viewport kept all buttons in bounds and created no page scroll;
- the default 640×360 desktop viewport retained controls visibility and a 56-pixel minimum target.

The deterministic harness, rather than this sequential mouse check, proves simultaneous multi-touch, same-key multi-source ownership, pointer cancellation, source bounds, and lifecycle cleanup.

Not performed / remaining physical-device checks:

- Android Chrome on physical touch hardware;
- iPhone Safari on physical touch hardware;
- physical multi-touch direction plus action;
- background/foreground return on both mobile browsers;
- mobile audio unlock after the first touch;
- safe-area behavior on hardware with a display cutout or home indicator.

No physical Android or iPhone validation is claimed.

## Regression evidence

- Desktop keyboard unchanged: normal generated Web execution, existing keyboard-driven Phase 4/5 harness modes, exact native/Web parity runs, and the focused desktop case passed. Numeric mappings, modifier filtering, Alt+Enter behavior, repeat suppression, and the 256-entry policy were retained.
- Console programs unchanged: controls start hidden and become eligible only from `gameWindow(...)`; no new Console path or language function exists.
- Canvas DPR/aspect behavior unchanged: the existing resize implementation was not modified, and retained Phase 4/5 DPR, viewport, and high-resolution tests passed.
- Publication unchanged: managed files remain exactly `index.html`, `smile-runtime.js`, `game.js`, and `smile.css`; transactional publication and asset rollback tests passed.
- Native Windows behavior unchanged: no language/native/game source changed, native runtime and test projects built, 39 graphics/audio-focus and 38 Text checks passed, and the full native smoke matrix passed.

## Known limitations

The standard profile does not automatically infer arbitrary custom or same-device multiplayer keyboard layouts. A game where one local player uses `W`/`S` and another uses arrows needs a future explicit profile or game-declared mapping. The current implementation deliberately does not branch on game names, duplicate directional aliases, or change key constants.

The controller supports discrete touch/pen/mouse press, hold, release, cancellation, and simultaneous-button gestures. It does not expose touch coordinates, analog motion, swipe recognition, or arbitrary gestures to SMILE programs.

Physical mobile-browser checks remain outstanding as listed above.

## Commit and push

- Implementation subject: `Sin and Codex: feat(web): add mobile virtual controls`
- Implementation SHA: `eb761d6664c10580335c4ff61cca25f912fb0c06`
- Push result: `d1893fb..eb761d6  main -> main`
- Evidence-review subject: `Sin and Codex: docs(web): record mobile controls evidence`
- Evidence-review SHA: this report is the content of the follow-up documentation milestone; its immutable pushed SHA is recorded in the task's final Codex report because a Git commit cannot contain its own hash.
