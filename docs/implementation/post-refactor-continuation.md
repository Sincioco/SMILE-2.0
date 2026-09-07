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
| 3 | Reassess the 1,851-line `Program.smile` for useful responsibility extraction | Completed; evidence-based no-change decision |
| 4 | Reconcile and continue only approved, implementation-ready suspended work | Completed; Reflective Battle Ground Floor is the only supplied implementation handoff |
| 5 | Execute the supplied Reflective Battle Ground Floor handoff after the ordered continuation work | Completed and corrected after native/Web visual acceptance; tracked in `docs/implementation/reflective-battle-floor.md` |

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

## Program responsibility review

The complete 1,851-line entry point and all 51 procedures were reviewed against
`tools/Character3DViewer/ARCHITECTURE.md`, the focused owner modules, and the
retained-responsibility list in the current refactor checkpoint. The file is
byte-for-byte unchanged from the accepted `f1b2355` audit, with SHA-256
`9951954FC0DB49FBB1C33A68A29D11EB506E9A9AD8BBD4643045B73A39044E33`.

No additional extraction improves responsibility ownership. The seven substantial
retained regions are the top-level executable/frame story plus the documented
keyboard, pointer, Party, gizmo and overlay coordinators. Each deliberately samples
runtime input or sequences calls across multiple focused owners. The remaining
procedures are small readiness/result adapters and readable lifecycle steps. Moving
either group would hide required ordering, broaden a subsystem with application
state, or assemble the same dependencies into a replacement application-controller
monolith. No source change was made merely to reduce the line count.

The fresh full repository smoke and installed VSIX validation recorded above cover
the unchanged Viewer entry point and its current native/Web compiler paths.

## Approved-work reconciliation

- Semantic-inspection CLI remains approved future work, but no task-specific ZIP or
  Markdown handoff was supplied. The existing asset-pipeline plan describes semantic
  inspection outcomes, not an implementation-ready CLI milestone. It was not started.
- Battle Scene Editor E0-E12 remains unstarted. The hardened repository and README
  explicitly require a fresh specification, and the Character Viewer is not the
  Editor. No fresh Editor package was supplied, so it was not started.
- Subject-first syntax remains approved future work. No implementation handoff was
  supplied, so it was not started.
- Reflective Battle Ground Floor now has the explicit task-specific instruction file
  `C:\Users\louie\Downloads\Implement Reflective Battle Ground Floor.txt` supplied
  by Sin. It is the only reconciled item ready to proceed after priorities 1-3.

Clearing the temporary On Hold list did not turn unsupplied proposals into active
implementation tasks. This reconciliation preserves their actual pending state while
following Sin's instruction-file gate.

## Continuation disposition

The supplied Reflective Battle Ground Floor task is implemented and validated in
`docs/implementation/reflective-battle-floor.md`. No additional task from this
continuation is implementation-ready without a new task-specific ZIP or Markdown
handoff from Sin.

## Reflective-floor acceptance corrections

Visual acceptance after the initial `61a64c4` delivery found three presentation
regressions. The reflection refactor had applied default back-face culling to legacy
simple-material geometry, hiding the established grid. The first backdrop attempt
also flipped the whole flat image around screen center, which exposed artwork that
the floor covered in the direct view. Finally, the new floor control shared the
animation-diagnostics row.

The native and Web owners now preserve the legacy double-sided simple-material path,
derive a conservative backdrop source boundary from the topmost projected receiver
edge, and map only the visible image region above that boundary into the floor.
Eligible 3D characters and equipment still use the mirrored-camera pass. The UI
explicitly switches between `Battle Floor: Reflective` and `Battle Floor: Original`,
and animation diagnostics begin on a separate row with compact-height suppression.

Focused native/Web reflection and Viewer hardening gates passed. Release native and
Web publications for Character Viewer and Sin Star I rebuilt successfully. Visible
native and installed-Chrome checks retained the grid, kept floor-covered backdrop
pixels out of the reflection, preserved the floor-mode toggle, and showed separated
control and diagnostic rows. Arin and Orin calibration hashes remained unchanged.
