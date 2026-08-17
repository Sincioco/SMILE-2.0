# SMILE 2.0 Lightweight OOP Post-Implementation Review

**Review date:** August 17, 2026  
**Starting commit:** `86016e815e1d71be195f29adcf4aa247386360a5`  
**Hardened implementation commit:** `4b1ec2cc12b801150a4b0ef4c34cb30c9012c64c`  
**Status:** Accepted after focused hardening

## Result

The lightweight-OOP implementation is hardened across native and Web execution, deterministic
format-6 packages, parser recovery, formatter/editor behavior, `Smile.UI` lifecycle ownership,
official consumers, and installed Visual Studio use. No rollback or architecture restart is
recommended.

Versions were intentionally unchanged because this work strengthens internal contracts without
changing official public APIs:

```text
Smile.UI   2.0.0
Smile.Game 2.0.0
Smile.RPG  1.2.1
VSIX       2.0.48
.smilelib  format 6
```

## Implemented Hardening

### Non-local native unwind

Native code now links active routine frames and unwinds them newest-first for `End Program`,
runtime `Nothing`, and Class allocation failure. The frame cleanup covers Text, Image, records,
Types, Classes, and clip state. Staged call resources remain separate so partial argument
evaluation, receiver captures, property assignments, and constructor preallocation are released
exactly once. Recursive and caller/callee fixtures finish with zero live Text/Class/Image counts.

### Canonical Class cleanup

Native and Web Class cleanup use reverse declaration order. Arrays clear in reverse element order.
Generated-order assertions cover interleaved Text, Type, Class, and array fields.

### Allocation failure

The native runtime accepts the internal test-only `SMILE_CLASS_ALLOCATION_FAIL_AFTER` environment
variable. A forced failure has a dedicated diagnostic, stable exit code 3, skips the constructor
body, and performs the same total unwind as other non-local termination. Normal allocation is
unchanged when the variable is absent.

### `Smile.UI` lifecycle

Menu, MenuNavigator, and Dialogue fixtures now cover invalid construction, capacity exhaustion and
recovery, aliases, repeated destroy, stale generations, slot reuse, destroyed roots/children,
collection revision, accepted-leaf cleanup, shared markers, and post-destroy calls. Consumers use
explicit teardown; lifecycle runs finish with `SMILE_CLASS_LIVE=0` on native and Web.

### Format 6 and tools

The negative package matrix rejects missing, duplicate, mismatched, hidden, private, or tampered
metadata across providers, source identity, Enum/Type/Class members, constructors, properties,
Optional defaults, capability metadata, and hidden `Me`/setter `Value` fields. Independent package
rebuilds remain deterministic and project/package behavior agrees.

Parser recovery is bounded around malformed new constructs and retains later declarations when
safe. Formatter mutation converges to byte-identical output, check mode does not write, and partial
selection comment commands are covered. Project/package completion, Quick Info, F12, shared IDE
snapshot analysis, and nonexecuting property hover are regression-tested.

## Reproduced Failure and Fix

The first full smoke pass exposed one regression in `InvalidClassMembers\IllegalStatement`: a
terminated invalid Class containing `Dim` emitted `SML2001,SML3450,SML2002` instead of the exact
established `SML3450`. Recovery now distinguishes an illegal in-block declaration from a later
top-level declaration by finding the matching nominal terminator before a strong declaration
boundary. A focused exact-code test was added, and the complete smoke matrix then passed.

## Automated Evidence

Commands completed successfully from `D:\SMILE 2.0`:

```text
scripts\build.cmd
scripts\test-lightweight-oop-hardening.ps1
scripts\test-smile-formatter.ps1
scripts\format-smile-style.ps1 -Check -FormatLongIf
scripts\smoke-test.cmd
scripts\verify-artifacts.ps1
```

Recorded results:

- 271 managed language/compiler/project/completion/timing tests passed.
- 13 formatter integration tests passed; 274 SMILE files passed style checking.
- 39 native graphics/audio-focus tests and 38 native Text tests passed.
- focused native/Web OOP unwind, finalizer-order, allocation-failure, and UI fixture parity passed.
- all official libraries, examples, format-6 packages, seven games, no-demo variants, DirectX,
  GDI, and Web smoke targets passed.
- artifact verification, asset publication, viewport/DPI, and generated package checks passed.

## Installed-Product Evidence

Visual Studio instance `91f001b5` loaded the installed VSIX 2.0.48. The installed assembly is
2.0.48.0 at
`C:\Users\louie\AppData\Local\Microsoft\VisualStudio\18.0_91f001b5\Extensions\wbrfmvxj.kbs\Smile.VisualStudio.dll`.
Its SHA-256 is
`2C5C75C185053211B38C632E25A72ABF1021DBDFF4DD675225D930A83CDF7B23`, matching the verified
build/install evidence.

A generated Game project built in the installed IDE, F5 bound and hit a SMILE breakpoint, F10
advanced with correct source mapping, and Ctrl+F5 displayed the expected game window before a
clean exit. The `LightweightOopCalls` project then built in the installed IDE. Alias completion
listed imported Enum/Type/Class symbols, Quick Info reported the package identity/version/source,
F12 opened `Library\Api.smile`, and debugging stepped from the project into the imported OOP
method with the correct source and line. No responsiveness warning appeared.

Managed tests additionally verify current Console/Game/Library templates, source and Module item
templates, headers/project metadata, comment commands, formatter commands, local/project/package
navigation, named-argument and member completion, diagnostics clearing, and static property Quick
Info behavior.

## Focused Graphical Evidence

The following native checks were performed on the hardened build:

- MenuGallery DirectX and GDI: system/bitmap/vector styles, disabled and ellipsized items,
  scrolling, nested navigation, leaf acceptance dialogue, back/exit.
- Snake demo and no-demo: title, attract demo, cancellation/return, player start, direction input,
  wall collision, game-over/retry, and clean close.
- Sin Star I: title actions, Character gallery, Tab selection, manual/automatic movement, run
  toggle, Town and Town 2 loading, player movement, camera scrolling, minimap/assets, and clean
  close.
- RPGSystems DirectX: battle selection and round progression, first-person dungeon movement,
  management mutation, validated save/load round trip, and world movement.
- RPGSystems GDI: main menu and world-gallery rendering/navigation.
- Web builds and execution were covered by the automated smoke matrix; no claim is made that an
  automated agent can judge subjective visual smoothness or audio quality.

## Known Limitations and Manual Test Request

**MANUAL TEST REQUEST:** Sin should make the final subjective judgment for music/SFX audibility,
mix, focus mute/restore behavior as perceived through speakers/headphones, and any desired extended
visual playthrough. Automated native focus-state tests and Web/native execution passed, but those
cannot replace human listening or artistic review.

No known hardening defect remains. The intentionally deferred language features are inheritance,
interfaces, generics, delegates, lambdas, events, user finalizers, tracing GC, Class-reference
fields/cycles, Class arrays, and wholesale `Smile.RPG` object migration. Keep the lightweight-OOP
surface frozen until production usage demonstrates a concrete need for another public feature.
