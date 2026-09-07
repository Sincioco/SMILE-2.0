# SMILE 2.0 Post-Refactor Continuation

This is the single compact checkpoint for the continuation authorized by
`2026-09-07-2249-smile-2.0-new-codex-continuation.zip`.

## Reconciled baseline

- Completed baseline `191dc992a1f4a46a4c546894581c2972e951d851` is an ancestor of the
  preserved September 7 work.
- Reconciliation began from `dd4a03c7` on `main`, aligned with `origin/main`.
- H01/H02/H03 remain closed under
  `docs/implementation/record-array-h01-h03-closure.md`; the retired test and
  Doctor repair path are not part of this continuation.
- The completed Character Viewer refactor remains governed by
  `docs/implementation/character-viewer-refactor-checkpoint.json`.
- Live calibration was preserved: Arin v5.7 has 24 keys and Orin v1.3 has zero
  keys in the packaged Release build.

## Ordered work

| Order | Milestone | State |
| --- | --- | --- |
| 1 | Restore native Desktop Character Viewer sound effects without changing Web cues | Completed and pushed in `a3b645d` |
| 2 | Correct the underlying NU1503 restore/build warning without suppression | Completed and pushed in `bac36a2` |
| 3 | Reassess the 1,851-line `Program.smile` for useful responsibility extraction | Pending |
| 4 | Reconcile and continue only approved, implementation-ready suspended work | Pending |
| 5 | Execute `C:\Users\louie\Downloads\Implement Reflective Battle Ground Floor.txt` after the ordered continuation work | Queued by Sin |

## Desktop sound-effects milestone

### Root cause

The Character Viewer crossed its authored SwordAttack cue and reached the native
sound path, but XAudio2 returned `0x800401F0` (`CO_E_NOTINITIALIZED`). The native
SFX subsystem depended on some unrelated caller having initialized COM on the
playback thread. That hidden precondition prevented creation of a Windows audio
session and made valid packaged WAV cues silent.

### Correction

`src/Smile.NativeRuntime/audio/sfx_channels.cpp` now owns a balanced
multi-threaded COM apartment reference for the lifetime of its XAudio2 engine.
It accepts an already initialized apartment, releases only the reference it owns,
and unwinds that reference if XAudio2 or its mastering voice cannot initialize.
The focused native test rejects `CO_E_NOTINITIALIZED` and verifies shutdown
balances the apartment lifetime.

### Evidence

- Native text/runtime fixture: 46 checks passed, including actual WAV completion
  and balanced COM shutdown.
- Exact Release Desktop Viewer: Arin SwordAttack created an active, unmuted
  process audio session at volume `1.0`; the endpoint meter observed a `0.722`
  peak during the cue.
- Full Release Web publication rebuilt successfully. Generated output retains one
  slash and one crosscut `playSound` call, publishes both WAV files, and both
  generated JavaScript files pass `node --check`.

## NU1503 milestone

### Root cause and correction

The repository build script used `dotnet restore` on a mixed managed and native
Visual Studio solution. The .NET restore host cannot evaluate the C++
`Smile.NativeRuntime.vcxproj`, so it skipped that valid project and emitted NU1503.
`scripts/build.cmd` now uses Visual Studio MSBuild for the solution restore. That
host owns both project systems, restores the managed graph, evaluates the native
project without a warning, and does not suppress any diagnostic.

### Evidence

- Direct Visual Studio MSBuild restore completed without NU1503.
- The normal full build and VSIX packaging path completed without NU1503.
- The correction was committed and pushed separately as `bac36a2`.

## Full-smoke regression follow-through

The ordinary full smoke exposed two stale assumptions after earlier checked-array
work. The Phase 3B invalid fixture still used a fixed-size record-array field that
is now valid language syntax; it now tests an actually invalid unsized field and
retains SML3403 coverage. More importantly, native `And` and `Or` still evaluated
both operands while generated Web JavaScript already short-circuited. Existing
shared SMILE guard expressions therefore reached sentinel array index `-1` only on
native builds. The MASM emitter now evaluates logical operators left-to-right,
skips the right operand when its result cannot affect the expression, and
normalizes the result to Boolean `0` or `1`.

### Evidence

- Focused native and Web fixture produced the identical result
  `False,0,True,0`, proving the skipped operand did not run.
- Managed compiler suite: 309 checks passed, including new native/Web emitter
  assertions for `And` and `Or`.
- Full repository smoke passed: 13 formatter tests, 421 tracked SMILE style
  checks, and all native/Web language phases, libraries, games, and VSIX payload
  checks completed successfully.
- The refreshed installed VSIX is version `2.0.60`, assembly version `2.0.60.0`,
  with SHA-256
  `6AA1C8B6B9CF8BDF7594AF7FD4173CE288F81E0C47EB1138C8F2F9C2757070C6`.

## Next action

Commit and push the native logical-operator regression follow-through, then review
the remaining Character Viewer `Program.smile` responsibilities against the
completed refactor ownership map. Extract only a cohesive responsibility with a
clearer dependency boundary; otherwise record an evidence-based no-change decision.
