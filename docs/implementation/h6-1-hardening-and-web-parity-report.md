# H6.1 hardening and Web parity report

## Decision

**PASS-NATIVE-WEB on the user-revised Windows/Chrome baseline.** Implementation
commit: `bc6f607bec5a60df1e72a0d3541156bc9175fe82`, branch `main`. The report is
a subsequent documentation-only milestone; its commit is discoverable with
`git log -1 --format=%H -- docs/implementation/h6-1-hardening-and-web-parity-report.md`.
All implementation milestones are pushed. Final reconciliation fetched origin:
HEAD and origin/main matched, 0 ahead / 0 behind, with the reviewed SHA retained
in ancestry. No reset, history rewrite or discard of unrelated work occurred.

This is a new gate, not a reinterpretation of historical H6's `PASS-NATIVE` /
`DEFERRED-PERMITTED` Web result. All 26 inventoried issues (F01–F05, W01–W21) have
verified dispositions. This does not prove absence of undiscovered defects.

Sin explicitly replaced the original Firefox requirement with installed browsers,
then made Chrome the sole routine acceptance target. G10 remains present. Firefox
is **NOT-RUN**, not a passing fallback; historical Edge observations remain evidence
from the same Chromium engine family. The gate's schema revision 2 records this
explicit scope change, retains all twelve G01–G12 checks and all issue IDs, and
does not weaken shader, lifecycle, storage or native proof requirements.

**Battle Scene Editor E0–E12 was not started. Double was not added.** The separately
approved optimized-Web profiles, README rewrite and later loader/splash extensions
remain subsequent work; they are not certified features of this checkpoint.

## Intake and preservation

H6.1 execution started on `main` at reviewed SHA
`902a7022c895bf97010d979ea578fc5361cdcbf4`. Package:
`2026-09-05-smile-2.0-h6-1-hardening-and-web-parity.zip`, SHA-256
`0C23AA583AC97520B55841785B533CAA3936492500891EAD86C754E994A287D9`.
Actual Windows Downloads resolves to `C:\Users\louie\Downloads`. Intake used the
ignored handoff folder, safe path checks and supplied hashes; final reconciliation
again verifies all 22 manifest-listed byte counts/hashes. No ZIP program was run.

Original package copies remain in
`artifacts/temp/codex-handoff/2026-09-05-smile-2.0-h6-1-hardening-and-web-parity/preservation-start-902a7022`
(108 files, 184,006,573 bytes). Additional pre-import save backups remain under
`artifacts/temp/codex-handoff/h6-1-json-import-resume-909fd6a`.
The historical H0–H6 package/report records the earlier prerequisite baseline and
backups separately. Neither historical Arin 23-key data nor Orin's old fingerprint
was restored over newer work.

| Character | Current saved keys / native data key | Preserved or intentional change |
|---|---|---|
| Arin v5.7 | 24 / `CharacterViewerCalibrationKeyframes` | Sin's newer Death frame-zero key retained. Accepted model, descriptor and package remain unchanged. |
| Orin v1.3 | 0 / `CharacterViewer.Orin.v1.3.CalibrationKeyframes` | Surgical JumpAttack landing correction; explicit asset/profile fingerprint migration. Other clip/mesh/skin/material data preserved. |

Both use application ID `smile.tools.character3d-viewer` but distinct character
asset identities and data keys. Arin keys by clip: SwordAttack 13, SwordAttack2 4,
BlockImpact/Death/Defend/Hit/Idle/Run/Walk one each. Orin's nine clips have no saved
keys. Final exports remain:

- Arin JSON: `C05C87BF0A92B373DB7ECD1CB304F4446B851E7AFEA836E8BB05D058B1B20F0B`.
- Orin JSON: `13AE135FDA40302CB5A4B0146D7103A2ED5346AAEEBB3852AF6DD3C397F5D293`.

The [artifact manifest](h6-1-artifacts.json) records full current profile/model/
descriptor/package identities. Canonical schema-2 JSON remains the readable source
of truth; generated binary SMKF and native Save Data envelopes are not the same
file/hash. Actual browser import replacement used disposable origin 8769. It
changed one test wrist channel, then survived reload and matched all downloaded
24 keys by clip name. Normal origin/native/canonical keys remained unchanged.

## Implemented and already verified

| Phase | Implemented | Existing architecture retained and verified | Key commits |
|---|---|---|---|
| W0 | Current issue inventory, package/save reconciliation | Canonical versioned packages; historical H0–H6 preservation | Reviewed `902a7022` and this report |
| W1 | Overflow-safe camera normalization, bounded Orin attack framing | Shared integer-world camera, ground/grid distinction | `5c2036a`, `0768860` |
| W2 | Freeze-safe visibility/clear, scene-owned comfort, retained part visibility, Dragon cut rebasing | Scene-owned effects clock, bounded shared admission, independent actor contexts | `b8fce49`, `b835628`, `28b78a3` |
| W3 | Web backdrop, current correction dispatch/socket semantics, matching Euler signs, multisample scene resolve | Existing Character3D/Battle3D/PBR/skinning/post architecture | `519e780`, `6b62f3b`, `78e00f4`, `ee2a951` |
| W4 | High-level thermal GPU Fire and current Lightning on Web; shader/buffer/pool fixes | Native compute path and explicit lower-capability fallbacks | `519e780`, `cf84ac8` |
| W5 | Transactional storage/JSON transfer, uniform Party active-actor inspector, white headers, opt-in smoother gizmo, bounded model cache, input ownership | Existing Party demo; hero-only calibration and Dragon timeline-only workflow | `2cb6c52`, `89755fe`, `0fd7993`, `c4379a7` |
| W6 | Two live same-model proof, actual browser save/audio/context recovery, normal smoke reconciliation, current outputs/VSIX | Existing shared/VM and native fixtures, no new test framework | `cf84ac8`, `8457e78`, `4a30cd3`, `bc6f607` |

Additional user-reported fixes include Orin's grounded smash/recovery, varied two-
and three-attack Party patterns, boss-first defensive beats, consistent latched
fire/impact/KO target, exclusive off-window timeline drag ownership, and above-
solid-floor orbit bounds. Timeline operations remain presentation-only: no combat
damage, MP consumption, reward, combat RNG reroll or live game-save mutation.

Generic syntax added during this iteration is supported and tested, not proposed:
`File_Export(FileName, Contents) As Boolean`, `File_Import() As Text`, optional
`Status <Number target>` on Save Data / Load Data with shared status constants,
and `Key_Event_Held(key) As Boolean`. Shared language/parser/semantics and both
emitters own these contracts. `Key_Held` retains physical state semantics. No
replacement engine/framework, SMILE 1.0 compatibility layer or Double type.

## Validation and acceptance

The [parity matrix](h6-1-native-web-parity-matrix.md),
[observations](h6-1-native-and-browser-observations.md) and chronological
[ledger](h6-1-hardening-and-web-parity-ledger.md) separate native execution,
generated-Web VM fixtures, actual Chrome actions and user reports. The
[issue register](h6-1-known-web-issues.json) retains symptoms, causes, fixes and
evidence, including failures discovered during testing.

- Normal compiler build and latest 300 shared language tests passed.
- Normal smoke completed as the recorded initial run plus exact continuation
  after correcting two stale harness assumptions. It was not one uninterrupted
  all-green run. Later native/Web focused suites cover the subsequent changes.
- Viewer hardening ran **without NativeOnly** and passed, including 58 native
  graphics/pointer/audio checks and generated-Web exact console parity.
- Native/current Web calibration, two-live-actor, Fire/Lightning, GPU/fallback,
  PBR/socket, post/MSAA/soft-depth/distortion, storage and input checks passed.
- Actual Chrome rendered all four Viewer tabs and both Labs, with thermal Fire
  High/GPU and Lightning Ultra/GPU. Actual forced CPU fallback remained separate.
- Actual frozen visibility/cuts, JSON import/download/reload, shortcuts/fullscreen,
  audio focus/overlap, context loss/restoration and shutdown/second launch passed
  through the documented actions. Sin reported Desktop/Chrome camera and ring
  dragging "It seems to be working"; this is user evidence, not an agent benchmark.
- Final compile/install scripts rebuilt and verified VSIX **2.0.59**, assembly
  **2.0.59.0**, with installed and built compiler hashes matching.

## Delivery

The [deployment instructions](h6-1-web-deployment.md) describe static hosting,
normal builds, dependencies and origin-scoped saves. Current native outputs:

- `tools/Character3DViewer/bin/Release/Character3DViewer.exe`
- `tools/AdvancedFireVfxLab/bin/Release/AdvancedFireVfxLab.exe`
- `tools/AdvancedLightningVfxLab/bin/Release/AdvancedLightningVfxLab.exe`

Each normal Web publication is alongside it in `bin/Release/Web`. The portable
ZIP is `artifacts/handoffs/h6-1-bc6f607/smile-2.0-h6-1-web-bc6f607.zip`:
113,222,717 bytes, SHA-256
`79FD91E2F8ED922478458E8CE00991B1F242322894DAB4455EDC328E44055A92`.
All 63 application files were checked against their archive streams. A separate
report/evidence ZIP in the same folder packages these individually usable documents,
the machine gate/schema/manifest, companion fixtures and selected logs.

VSIX: `artifacts/vsix/Smile.VisualStudio.vsix`, SHA-256
`54255DD3DE1A2CE06FF2B50DDD288915B61F76C0FED260A74974A7F40B605C6E`.
Installed under Visual Studio `18.0_91f001b5/Extensions/p3fwicpi.oet`; extension DLL
SHA-256 `650D729423D51888107CBD910416C6D32000BC76ABA0972001A395E02F3F3C0B`.
Installer verification passed; no new interactive IDE session is claimed.

## Limits and next authorized work

No Firefox/Safari/iOS/physical-touch-device certification, exhaustive game manual
playthrough, physical speaker-audibility proof, fixed switching-time promise,
slow-network benchmark or arbitrary-GPU matrix. Native recovery evidence uses
existing fixtures/source checks, not induced physical driver removal. Chrome's
generic WebKit renderer string alone does not identify a physical GPU.

Viewer context recovery was verified by selecting Arin and then Party after
restoration; an automated Enter attempt did not recover it. The Labs resumed
automatically. No automatic Viewer recovery claim is made. Real browser private-
mode/quota exhaustion was not induced; those failures use isolated VM fixtures.
Concurrent save merging is not supported. Visual equivalence is not pixel identity
or blanket artistic approval. Sin's specific Arin and Mac observations are recorded
as user reports; the final Mac build was not retested by the agent.

This closes the hardening milestone only. Proceed with the explicitly approved
Web Optimized tiers, then visual README rewrite, then loader/splash enhancements.
Do not start E0–E12; the Battle Scene Editor specification will be refreshed by Sin.
