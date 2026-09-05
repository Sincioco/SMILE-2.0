# Battle Scene Prerequisite Hardening Ledger

This ledger records only H0-H6 from `2026-09-05-01-smile-2.0-pre-battle-editor-hardening.zip`. Battle Scene Editor E0-E12 work is explicitly excluded.

| Milestone | Commit | Commands and result | Blockers / next action |
| --- | --- | --- | --- |
| Preserve current work | `6957ac0be4925c907d40c18127c9e3b9470d122e` | Exported and validated Arin (23) and Orin (0) keys; pushed `main`. | None. |
| Preserve newly present Final Boss references | `e02403dc3fda301cec236408e9326eb946c25d0b` | Confirmed staged paths were ten PNG files under the intended source/reference folder; pushed `main`. | None. |
| H0 intake and baseline | `56513fbcf74ebe3f192b6e34f2d230ec161425f7` | Safe ZIP intake: 12/12 manifest files and hashes passed. Formatter: 13 passed. Baseline style: 378 passed. Baseline Viewer native gate reproduced three failures. | Closed by the implementation milestone. |
| H1 character preservation | `56513fbcf74ebe3f192b6e34f2d230ec161425f7` | Live, canonical and ignored backup hashes match. Final exports preserve Arin 23 / Orin 0 keys and distinct saved keys. Isolated load/edit/undo/save round trip passed. | None. |
| H2 native hardening | `56513fbcf74ebe3f192b6e34f2d230ec161425f7` | Existing pointer recovery, elapsed camera/VFX motion and focus audio behavior passed 58 native checks. Actual Viewer and Lightning Lab interactions passed. | Direct automated middle-button drag was unavailable; native capture/release logic is covered by the executable harness. |
| H3 actor/VFX ownership | `56513fbcf74ebe3f192b6e34f2d230ec161425f7` | Added scene-owned VFX advancement, independent family freeze, per-instance Orin state and generation-safe light leases. Native isolation/failure-path checks passed. | None. |
| H4 Viewer/Party/Dragon backlog | `56513fbcf74ebe3f192b6e34f2d230ec161425f7` | Verified newer Dragon tab/head aim and accepted Death assets; added explicit Party states and expandable formation coverage. Native individual/Party observations passed. | Aesthetic user sign-off remains intentionally unclaimed. |
| H5 reusable seams | `56513fbcf74ebe3f192b6e34f2d230ec161425f7` | Added `LightPool3D` and `SceneVfx3D`; retained CharacterViewer coverage and replaced obsolete source-placement assertions with behavior/wiring checks. | None. |
| H6 native gate | `56513fbcf74ebe3f192b6e34f2d230ec161425f7` | Native-only wrapper: PASS, 58 checks. Formatter: 13 passed. Style: 380 passed. Ordinary smoke: PASS. Final Viewer and one VFX Lab executed. VSIX 2.0.59 installed and verified. Gate: `PASS-NATIVE`. | Web hardening-harness execution is a permitted deferral recorded in the report; E0-E12 remain prohibited by the changed user scope. |

The implementation commit is pushed on `main`. The report and gate are delivered in the documentation commit containing this ledger; its SHA is reported in the final handoff to avoid a self-referential document commit.
