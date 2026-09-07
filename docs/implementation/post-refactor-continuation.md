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
| 1 | Restore native Desktop Character Viewer sound effects without changing Web cues | In progress: fixed and validated; commit/push pending |
| 2 | Correct the underlying NU1503 restore/build warning without suppression | Pending |
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

### Evidence before milestone commit

- Native text/runtime fixture: 46 checks passed, including actual WAV completion
  and balanced COM shutdown.
- Exact Release Desktop Viewer: Arin SwordAttack created an active, unmuted
  process audio session at volume `1.0`; the endpoint meter observed a `0.722`
  peak during the cue.
- Full Release Web publication rebuilt successfully. Generated output retains one
  slash and one crosscut `playSound` call, publishes both WAV files, and both
  generated JavaScript files pass `node --check`.

## Next action

Run the focused Character Viewer hardening gate, export both canonical calibration
sources, commit and push the sound fix as the first milestone, then begin NU1503
diagnosis. Do not start a later milestone before that push succeeds.
