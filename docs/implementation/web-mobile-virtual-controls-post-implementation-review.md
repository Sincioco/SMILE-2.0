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

## Mobile screenshot follow-up

- Follow-up starting SHA: `d7ff8a8dce9253f295b7ee024528b9d456193249`.
- User-supplied phone screenshots exposed behavior that the original desktop emulation missed: the number strip consumed vertical space, lower controls could fall behind browser chrome, portrait content was not centered as one composition, landscape controls were clipped, and left/right Unicode arrows selected an emoji-style glyph.
- The same report also covered the observed B-button termination: B emitted `KEY_ESCAPE`, and bundled games that intentionally end on Escape stopped the Web runtime and hid its controls.
- Follow-up working-tree condition preserved: `SMILE 2.0.sln` had a pre-existing uncommitted RPGSystems project-ID change from `EA20FCAD-4E88-B4D4-091C-B39D26B68FA6` to `2C35845D-5BE6-C84A-2DC5-8D4FEEFEFE47`, plus removal of its leading blank line. It remained unchanged and was excluded from the commits.

## Result

Generated graphical Web games now provide a reusable compact controller with a D-pad and A/B/X/Y. The overlay defaults to Auto, stays hidden until `Game Window`, appears for touch-first capability, can reveal after observed touch or pen input on a hybrid device, and supports deterministic `?smile-controls=on`, `off`, and `auto` overrides.

Touch, pen, and primary mouse button gestures enter the same bounded broker as keyboard events. A press queues one existing SMILE key value and retains held state until that source releases. Multiple pointers and keyboard-plus-pointer ownership work without prematurely releasing a shared key.

## Architecture implemented

- Visibility modes: missing, malformed, unknown, or duplicated query state becomes `auto`; `on` and `off` are deterministic; Forced Off wins over capabilities and observed pointers.
- Capability detection: positive `navigator.maxTouchPoints` is combined with `pointer: coarse` or `hover: none`; an observed touch or pen Pointer Event reveals Auto controls on hybrid hardware. There is no user-agent parsing.
- Shared input broker: `keyboard:<code>` and `pointer:<pointerId>` sources own logical key values through a `Map`; a per-key reference count supplies `Key_Held(...)`. The `Get Key` queue remains bounded at 256 and active sources are bounded at 32.
- Pointer lifecycle: accepted Pointer Events use capture when available and release through pointer-up, pointer-cancel, lost capture, orientation change, blur, document visibility loss, page hide, runtime finish, runtime failure, or controls hiding.
- Mouse support: a primary mouse press is accepted whenever the controller is visible. Desktop Auto remains hidden; Forced On supports deterministic desktop use, and touch-first devices with an attached mouse can use the same controls.
- Layout and safe areas: landscape uses an overlay sized to the dynamic visible viewport. Portrait places the unchanged-aspect canvas and a dedicated controls region in one vertically centered flex composition. CSS uses safe-area inset variables in both orientations.
- Direction glyphs: all four D-pad arrows use the same plain triangle glyph with CSS rotation, avoiding emoji-style left/right glyph substitution on mobile browsers.
- Non-destructive compact mapping: B now shares the Space action with X instead of emitting Escape. Several bundled games terminate immediately on Escape, so exposing it on the default mobile pad could strand a session after one tap. Physical Escape remains unchanged; a future explicit profile is required where a game needs a distinct safe-cancel control.
- Visual treatment: resting control borders use a fainter translucent outline while pressed and focus states remain clearly visible.
- Accessibility: all eight controls are real buttons with `type="button"`, descriptive labels, `aria-pressed`, visible focus styling, and targets at least 56 by 56 CSS pixels.
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
| B | `KEY_SPACE` | 16 |
| X | `KEY_SPACE` | 16 |
| Y | `KEY_TAB` | 21 |

The mapping is an immutable internal `standard` profile. One press maps to one value; no arrow/WASD aliases are enqueued together.
Physical keyboard number keys keep their existing behavior; the compact on-screen layout no longer duplicates them.

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

artifacts\compiler\smilec.exe examples\ArcBasics.smile --target web --output-dir artifacts\temp\MobileVirtualControlsWeb-FollowUp
PASS (exit 0): generated the four Web output files.

node --check artifacts\temp\MobileVirtualControlsWeb-FollowUp\smile-runtime.js
PASS (exit 0): no JavaScript syntax error.

node scripts\run-web-test.js artifacts\temp\MobileVirtualControlsWeb-FollowUp --mobile-controls --timeout 10000
PASS (exit 0): "Web execution passed: D:\SMILE 2.0\artifacts\temp\MobileVirtualControlsWeb-FollowUp (mobile virtual controls)".

node scripts\run-web-test.js artifacts\temp\MobileVirtualControlsWeb-FollowUp --frames 3 --timeout 10000
PASS (exit 0): "Web execution passed: D:\SMILE 2.0\artifacts\temp\MobileVirtualControlsWeb-FollowUp".

cmd /c scripts\smoke-test.cmd
PASS (exit 0): included the new permanent mobile-control invocation, 284 managed tests, 13 formatter integration tests, the 278-file style gate, 39 native graphics/audio-focus checks, 38 native Text checks, retained native/Web parity and rollback/publication gates, Phase 4 and Phase 5 Web harness modes, all seven game demo/no-demo native and Web builds, native x64 GUI inspection, viewport/DPI checks, and VSIX payload/version verification.

git diff --check
PASS: no whitespace errors; Git emitted only the repository's line-ending conversion notices.
```

The managed output contract test additionally proves one hidden controls root, all eight symbolic controls exactly once, absence of the four retired number buttons, real button types, dynamic-viewport and centered-portrait rules, `viewport-fit=cover`, the scoped `touch-action` rule, the absence of global zoom suppression, the query contract, the absence of `userAgent`, and the unchanged four-file managed list.

## Focused hardening evidence

The dependency-free `--mobile-controls` mode executes isolated generated-runtime hosts and passed these deterministic cases:

- ordinary desktop Auto remains hidden before and after `Game Window`, while keyboard queue and held state continue to work;
- touch-first Auto becomes visible only after `Game Window`;
- Forced On, Forced Off, unknown-query fallback, duplicated-query fallback, and hybrid pen/touch reveal behave as specified;
- every D-pad and action button maps to its documented numeric value;
- B queues and holds `KEY_SPACE`, never `KEY_ESCAPE`, while a physical Escape key still queues, holds, and releases `KEY_ESCAPE` normally;
- simultaneous B and X presses each queue one Space event, and releasing either source preserves held Space until the other releases;
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

- the controller appeared after game initialization with exactly eight accessible buttons and no 1–4 strip;
- a same-origin 390×844 CSS-pixel portrait fixture measured a 390×219 canvas and 390×168 controls region whose combined center was exactly Y=422, the viewport center;
- all eight portrait buttons measured 56×56 CSS pixels, remained inside the viewport, had no pairwise overlap, and produced no page scroll;
- an 844×390 CSS-pixel landscape fixture kept all eight buttons within bounds and produced no page scroll;
- every D-pad direction used the same `▲` text glyph; computed transforms were none, -90, +90, and 180 degrees for up, left, right, and down;
- resting border color computed to `rgba(220, 247, 255, 0.56)`;
- clicking B through the real primary-mouse Pointer Event path left the controls visible and the portrait layout class active;
- browser logs contained no warning or error;
- the default in-app browser viewport loaded the forced-on Sin Star I output successfully before the two responsive fixtures were inspected.

The deterministic harness, rather than this sequential mouse check, proves simultaneous multi-touch, same-key multi-source ownership, pointer cancellation, source bounds, and lifecycle cleanup.

The user supplied physical-phone screenshots of the pre-fix failure. Codex did not operate that device, and no post-fix physical-device pass is claimed.

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
- Canvas DPR/aspect behavior unchanged: the backing-store resize implementation and logical dimensions were not modified; only host composition CSS changed. Retained Phase 4/5 DPR, viewport, and high-resolution tests passed.
- Publication unchanged: managed files remain exactly `index.html`, `smile-runtime.js`, `game.js`, and `smile.css`; transactional publication and asset rollback tests passed.
- Native Windows behavior unchanged: no language or native-runtime input code changed. The separate Smile 2.0 Tetris change updates display strings only. Native runtime and test projects built, 39 graphics/audio-focus and 38 Text checks passed, and the full native smoke matrix passed.

## Known limitations

The standard profile does not automatically infer arbitrary custom or same-device multiplayer keyboard layouts. A game where one local player uses `W`/`S` and another uses arrows needs a future explicit profile or game-declared mapping. It also cannot infer whether Escape means safe cancel or destructive exit, so the compact default exposes no Escape button and B/X both use Space. A distinct cancel control needs a future explicit profile or game-declared mapping. The implementation deliberately does not branch on game names, duplicate directional aliases within one press, or change key constants.

The controller supports discrete touch/pen/mouse press, hold, release, cancellation, and simultaneous-button gestures. It does not expose touch coordinates, analog motion, swipe recognition, or arbitrary gestures to SMILE programs.

Physical mobile-browser checks remain outstanding as listed above.

## Commit and push

- Implementation subject: `Sin and Codex: feat(web): add mobile virtual controls`
- Implementation SHA: `eb761d6664c10580335c4ff61cca25f912fb0c06`
- Push result: `d1893fb..eb761d6  main -> main`
- Evidence-review subject: `Sin and Codex: docs(web): record mobile controls evidence`
- Evidence-review SHA: `d7ff8a8dce9253f295b7ee024528b9d456193249`
