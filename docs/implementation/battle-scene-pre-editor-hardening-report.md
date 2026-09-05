# Battle Scene Pre-Editor Hardening Report

## Identity

- Status: `PASS-NATIVE`
- Package: `smile-2.0-pre-battle-editor-hardening`
- Package file: `2026-09-05-01-smile-2.0-pre-battle-editor-hardening.zip`
- Package SHA-256: `A4C1BC8EF81956EDEC50BBE3AAE2FD0D25ED112DF5990A195524F3A3D5AC4AB8`
- Reviewed SHA: `651492993a8709dd4e9489e57faca8888c89539d`
- Execution start SHA: `e02403dc3fda301cec236408e9326eb946c25d0b`
- Validated implementation commit: `56513fbcf74ebe3f192b6e34f2d230ec161425f7`
- Report commit: the documentation commit containing this file; its SHA is stated in the final handoff rather than embedded self-referentially
- Branch: `main`
- Implementation commit push: `origin/main`, confirmed `0/0` ahead/behind before H6 documentation
- Working tree before H6 documentation: clean

The reviewed SHA was used only as evidence. The checkout was not reset, rebased or rewritten. Newer work was reconciled and preserved.

## H0-H6 completion

| Milestone | Status | Existing or changed | Evidence | Remaining blocker |
| --- | --- | --- | --- | --- |
| H0 | PASS | Recorded current Git/tool state, selected the standalone package by identity and audited current capabilities. | All 14 ZIP entries passed path/collision checks; all 12 manifest-listed files matched byte counts and SHA-256. Baseline report pins branch and execution start. | None. |
| H1 | PASS | Existing versioned character packages and distinct calibration identities were preserved; no historical import was performed. | Final Arin export: 23 keys, SHA below. Final Orin export: 0 keys, SHA below. Ignored start backups match. Isolated native load/edit/undo/save test passed. | None. |
| H2 | PASS | Current native pointer recovery, fractional camera, elapsed animation/VFX and focus-audio implementation was retained and revalidated. | Native harness: 58 checks. Manual Viewer and Lightning Lab: bare Alt, fullscreen, pan/orbit/wheel/reset, minimize/restore and close/reopen passed. | Direct automated MMB drag was unavailable, but capture/release behavior passed the executable harness. |
| H3 | PASS | Changed singleton/actor-owned effects into per-instance actor state plus scene-owned family advancement and bounded light leases. | Two same-character instances, independent clips/clocks/styles/corrections, family freeze, duplicate-advance rejection, optional-emitter failure and stale-handle tests passed natively. | None. |
| H4 | PASS | Verified newer Dragon tab/head aim, accepted Death assets, grounding, KO/revive and shield options. Added explicit Party state and expandable formation coverage where still implicit. | Current asset hashes below; native individual/Party observations; three-member formation test; current Dragon six clips/six sockets; Arin/Orin nine clips. | User aesthetic approval remains pending and is not claimed. |
| H5 | PASS | Added small reusable `LightPool3D` and `SceneVfx3D` seams and retained existing CharacterViewer/renderer/compiler architecture. Replaced obsolete implementation-placement assertions with wiring and behavior checks. | Simple3D package builds; focused hardening and calibration harnesses pass; native Fire/Lightning Lab foundations pass. | None. |
| H6 | PASS-NATIVE | Completed formatter, focused native, ordinary smoke, final builds, real native execution, report and gate. | Commands/results below; machine-readable companion status is `PASS-NATIVE`. | Permitted Web execution deferral recorded separately. |

No Battle Scene Editor E0-E12 implementation was started.

## Data preservation

| Character | Canonical package | Runtime save key | Final keys | Canonical JSON SHA-256 | Profile fingerprint |
| --- | --- | --- | ---: | --- | --- |
| Arin v5.7 | `games/SinStarI/SourceAssets/Characters/Paladin/ArinV57` | `CharacterViewerCalibrationKeyframes` | 23 | `1747367DD5E411D8230AB5159DE1309F221867C8DE6745661DA1396EAE6DB867` | `DB3286E4A9DC8F3064F65C1BC36047E7AF4F2FF771EBC51BF4C73DC6C16AC2ED` |
| Orin v1.3 | `games/SinStarI/SourceAssets/Characters/Tank/OrinV13` | `CharacterViewer.Orin.v1.3.CalibrationKeyframes` | 0 | `07927539BD086FF8581D112FCEB648F5F89F1B12A5DECB263047554DD71E7937` | `5A6F903CCC4C5FF669689CC2A176133874A6B0641ED8F33B8D4429F7A8D6CA4D` |

Arin per-clip saved frames are `BlockImpact[0]`, `Defend[0]`, `Hit[0]`, `Idle[0]`, `Run[0]`, `SwordAttack[6,9,11,16,19,21,28,30,32,33,34,35,38]`, `SwordAttack2[0,10,14,17]`, `Walk[0]`, with no saved Death frames. The accepted current values were exported again immediately before the report. Orin remains intentionally at zero saved correction keys.

The exact ignored start backups are:

- `artifacts/temp/codex-handoff/2026-09-05-01-pre-battle-editor-hardening/calibration-backup-start-6957ac0/arin-v5.7-pose-calibration.json`
- `artifacts/temp/codex-handoff/2026-09-05-01-pre-battle-editor-hardening/calibration-backup-start-6957ac0/orin-v1.3-pose-calibration.json`

Their hashes match the canonical files above. No profile migration or conflict was required; the profiles retain independent storage keys and fingerprints. User source/reference work was preserved in pushed checkpoint commits `6957ac0be4925c907d40c18127c9e3b9470d122e` and `e02403dc3fda301cec236408e9326eb946c25d0b`.

## Canonical asset identity

| Asset | Canonical model SHA-256 | Descriptor SHA-256 | Package SHA-256 | Cooked SM3D SHA-256 |
| --- | --- | --- | --- | --- |
| Arin v5.7 | `EDCFC5F92E22DF7FD58030AB64410E0EBD9931D92F7AA2E297565B966C8C502E` | `2768A01120F5E0D35A85AF8C445D70A186193097F223429809E95D5098081620` | `6D1B792747AB6F92E04F8D3515013E9761308C1A8E15F442B592A3568386973A` | `401C14E7E00A90304C084AA8B766ABFEACD08A1F5B36DDA02025C1F8320B2CCD` |
| Orin v1.3 | `84B55E0EC83746A0188A473102F73377E63C3E9F15F04B597CF3DABA6B78DDCF` | `4345D2F60230F20353FAD7FFE395E8555096D2EF1F26A456C7D68B81AAE85649` | `E5BDE7A822EADD46FC514E23F40D755D5FF2013EC2F17C73428F08FFB1C87E3D` | `459CC2F787479454F2DF4FDDE6BE18871A5BF3EE41E824439D9B2EF8B9A75C43` |
| Red Dragon v1.1 | `782BF48A302665930F1F6872B159CEFBCE5FBA7C3272B7E53D2A63ECD842F4E8` | `A341DA15B7B97C28DA4B954B2B8F13B72737E2B264074C43EBEDA843035EC724` | `1CFFB9135E2BB7A7002B8E50A443379C9AEFD8099F901F006BF31284BEECA855` | `D3CD6FF6BAB02BE0ECD759494DF5E1A8BBD644700EEB2305E009D2821F2A0825` |

Arin Death source `arin-v5.7-mixamo-death.fbx` is SHA-256 `D059E99F96BE79C209261CD2668F02809679CCADA1D2043BA177625005231ADC`, distinct from the rejected historical KO motion. Orin Death source is SHA-256 `92EF957415C492171EF3A361DE4BC7C28B1F28BB85838C0760F270B2B9A9E3ED`; its accepted root repair and grounding records are present and hashed in the machine gate.

## Implementation findings closed

The H0 baseline reproduced three native failures: an obsolete socket-count assertion, paused seek retaining unsafe effect history, and resume producing a catch-up fire burst. The final native gate passes all three corrected behaviors.

Changes made:

- `SceneVfx3D` owns the shared scene frame identity and advances Fire and Lightning at most once per frame. Fire and Lightning freeze independently.
- Arin, Orin and Dragon stage actor-local transforms/effect intent; optional equipment or emitter failure cannot stop another actor or the shared family clock.
- `OrinStorm.Context` owns per-instance clip/action time, charge, style, visibility, trail state, first error and generation-safe identity. It borrows shared immutable material resources and releases owned effects/light leases.
- `LightPool3D` provides four bounded generation-safe leases over a caller-selected renderer slot range; stale and capacity failures are explicit and isolated.
- Party states are explicit (`Alive`, `Acting`, `Guarding`, `Hit`, `KO`, `Reviving`). KO actors suppress incompatible actor-local effects and revive on their own turn in this presentation harness.
- `PartyParticipantLayout` plus `BattleCamera.FormationPosition` provide bounds-aware, arbitrary-count formation placement. Three members are covered technically; no two-member indexing assumption remains in the reusable calculation.
- The current renderer/compiler architecture, Character3D/CharacterViewer workflows, calibration JSON source-of-truth, canonical Sin Star I ownership, VFX Labs and RPG/game-state boundary were preserved.

Already present and verified rather than reimplemented:

- Native pointer capture recovery, wheel remainder preservation, fractional target-anchored orbit, eased zoom and focus audio policy.
- Dragon tab, six current Dragon clips, six sockets, bounded head aiming and target selection.
- Accepted Arin/Orin Death sources, grounded settled poses, Party KO/revive presentation, shield silhouette controls and existing diagnostics.
- CharacterViewer isolated calibration editing and checksummed runtime Save Data envelopes.

No SMILE syntax, compiler semantic rule or native/Web runtime API was added. The work adds two reusable SMILE library modules and Viewer-local records/functions. SMILE 1.0 impact is none.

## Verification

All listed commands were actually executed from `D:\SMILE 2.0`.

| Command | Result |
| --- | --- |
| `pwsh -NoProfile -File scripts/sync-arin-v5-7-calibration.ps1 -Mode Export -Character Arin -AllowMissing` | PASS; 23 keys, canonical hash unchanged. |
| `pwsh -NoProfile -File scripts/sync-arin-v5-7-calibration.ps1 -Mode Export -Character Orin -AllowMissing` | PASS; 0 keys, canonical hash unchanged. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-smile-formatter.ps1` | PASS; 13 focused formatter tests. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/format-smile-style.ps1 -Check -FormatLongIf` | PASS; 380 tracked SMILE files. |
| `pwsh -NoProfile -File scripts/test-viewer-calibration-native.ps1` | PASS; isolated application identity, native Save Data and previous-good backup validation. |
| `pwsh -NoProfile -File scripts/test-character-3d-viewer-hardening.ps1 -NativeOnly` | PASS; Arin 42 calibration assertions, isolation harness, preparation gate and 58 native graphics/pointer/audio checks. |
| `pwsh -NoProfile -File scripts/test-lightning-vfx-foundation.ps1` | PASS; native Lightning tests and exact Web console parity. |
| `pwsh -NoProfile -File scripts/test-native-thermal-fire.ps1` | PASS; 21 thermal checks, native GPU recovery with zero failures, FireEmitter native and exact Web parity. |
| `pwsh -NoProfile -File tools/AdvancedFireVfxLab/Build.ps1` | PASS; native Debug Lab built. |
| `pwsh -NoProfile -File tools/AdvancedLightningVfxLab/Build.ps1` | PASS; native Debug Lab built. |
| `pwsh -NoProfile -File tools/Character3DViewer/Build.ps1 -Configuration Release` | PASS; 42 assets published. |
| `cmd.exe /d /c scripts/smoke-test.cmd` | PASS; ordinary repository smoke, including 295 language/compiler tests, 58 native graphics/input/audio checks, 44 native Text checks, native games and broad native/Web project parity. |
| `cmd.exe /d /c scripts/install-vsix.cmd` | PASS; rebuild, uninstall/clean/install and final verification. |

Native observation details and explicit nonclaims are in `docs/implementation/battle-scene-pre-editor-native-observations.md`.

Generated native executables:

- Final Character Viewer: `tools/Character3DViewer/bin/Character3DViewer.exe`, SHA-256 `94CD91FB5A2F1D6A0D69E8E25E12A5C8C8F47CE6D712E7184E058A8C962B4041`
- Hardening harness: `artifacts/tests/Character3DViewerHardeningTests.exe`, SHA-256 `B1973CBE05534558C3A190225765EE121AEF8E3787BB323D0802B81BB268C7F0`
- Calibration isolation harness: `artifacts/tests/ViewerCalibrationIsolation/CalibrationTests.exe`, SHA-256 `BB3D1D5C5F84C51AE71CFFFE3A613C8B15DD6117F3D08C09761F35454A65E4B5`
- Advanced Fire Lab: `tools/AdvancedFireVfxLab/bin/Debug/AdvancedFireVfxLab.exe`, SHA-256 `95FB0FEFB65633D223EC0E49199FC44A26D80E7DD05478504066849BAB65AB3C`
- Advanced Lightning Lab: `tools/AdvancedLightningVfxLab/bin/Debug/AdvancedLightningVfxLab.exe`, SHA-256 `7E10A2DBF9D6C938FAB4827D977603930A83106E5A0BE5F81C08BD8137FCC7C2`

Current toolchain identity:

- Compiler: `artifacts/compiler/smilec.exe`, SHA-256 `BFB4AA71C967656C4C3BB735DD721E926E6160D31321693ACC4A865D5CE4F2FF`
- Shared language assembly: `artifacts/compiler/Smile.Language.dll`, SHA-256 `8FEFDE3379DBFC6BBB522970BF98A7F3B53AC46CCFC01A3C1FC00333DA1B26D1`
- Built VSIX: `artifacts/vsix/Smile.VisualStudio.vsix`, SHA-256 `95A18D14318E309423673B29BB0321DC38C95F680B72D62B966BEB68B6405C7E`
- Installed VSIX: version `2.0.59`, Visual Studio instance `91f001b5`
- Built/installed `Smile.VisualStudio.dll`: identical SHA-256 `285980242FA4FEFEFDC0CAD390C7BC062BF9F6FA3A8FD7F39CC53FF7F5E8EE16`

This milestone did not modify compiler or VSIX source. The smoke rebuild produced a byte-different assembly from the previously installed build, so repository policy was followed conservatively: VSIX 2.0.59 was reinstalled and the installed DLL was verified against the newly built payload. Visual Studio was not running during the refresh; a restart is required only if an IDE instance later loads a prior in-memory extension.

## Web status

Web acceptance for the complete Viewer remains `DEFERRED-PERMITTED`; it does not block `PASS-NATIVE`.

- The full hardening project compiled for Web.
- Its Web execution then failed in existing screen-space camera normalization because a `Math3D.Dot` intermediate (`9650663384084100`) exceeds the JavaScript safe-integer range. The stack reaches `Interaction.ApplyScreenSpaceCameraControls` through `Math3D.Length/Normalize`.
- Lightning and Fire focused foundations passed exact native/Web parity.
- The ordinary smoke suite passed its broad Web compilation/execution parity set.
- No Web visual Viewer check or user approval was performed or inferred.

**Flag:** The shared screen-space camera math needs a Web-safe fixed-point normalization path before a later Web Viewer/editor acceptance can be claimed. The smallest reusable fix is overflow-bounded vector length/normalization that preserves current native results; it is explicitly deferred from this native-only prerequisite task.

**Flag:** SMILE still has no general JSON parser, operating-system clipboard API, native file picker, mesh ray picking, editor-wide transaction/undo service or multichannel battle-audio authoring surface. These are documented capability boundaries for a future specification, not existing syntax and not implemented here.

## Delivery and authorization

- Checkpoint commits `6957ac0be4925c907d40c18127c9e3b9470d122e` and `e02403dc3fda301cec236408e9326eb946c25d0b` preserve unrelated/newer user source assets and are pushed.
- Implementation commit `56513fbcf74ebe3f192b6e34f2d230ec161425f7` is validated and pushed to `origin/main`.
- Mandatory blockers: none.
- Nonblocking limitations: Web camera overflow above; direct automated MMB gesture and audibility were not manually claimed; high-pitch Party composition and overall aesthetics still require optional user review.
- Gate decision: `PASS-NATIVE`, because every mandatory H0-H5 native item has code or verified existing evidence, required assets/calibration are present and safe, focused native tests and the ordinary smoke pass, and no blocking native regression remains.
- User scope supersedes the older package continuation clause. E0-E12 are not authorized in this task and have not begun.

Stop after H6 and wait for a fresh Battle Scene Editor specification.
