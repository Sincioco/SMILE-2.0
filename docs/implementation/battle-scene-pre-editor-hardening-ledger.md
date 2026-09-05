# Battle Scene Prerequisite Hardening Ledger

This ledger records only H0-H6 from `2026-09-05-01-smile-2.0-pre-battle-editor-hardening.zip`. Battle Scene Editor E0-E12 work is explicitly excluded.

| Milestone | Commit | Commands and result | Blockers / next action |
| --- | --- | --- | --- |
| Preserve current work | `6957ac0be4925c907d40c18127c9e3b9470d122e` | Exported and validated Arin (23) and Orin (0) keys; pushed `main`. | None. |
| Preserve newly present Final Boss references | `e02403dc3fda301cec236408e9326eb946c25d0b` | Confirmed staged paths were ten PNG files under the intended source/reference folder; pushed `main`. | None. |
| H0 intake and baseline | Pending | Safe ZIP intake: 12/12 manifest files and hashes passed. Formatter: 13 passed. Style: 378 passed. Viewer native baseline: 3 failures. | Correct shared VFX ownership and obsolete assertions. |
| H1 character preservation | Pending | Live/canonical/backup hashes match for both characters; package identity and grounding evidence reviewed. | Re-export before any calibration-affecting commit. |
| H2 native hardening | Pending | Source evidence present; native focused execution still required. | Validate slow/moderate camera interaction and audio lifecycle without a soak. |
| H3 actor/VFX ownership | Pending | Baseline identified actor-owned shared clocks, singleton Orin state, and fixed local-light slots. | Add scene-owned clocks, per-actor Orin contexts, and bounded leases. |
| H4 Viewer/Party/Dragon backlog | Pending | Newer source already contains the requested features. | Run focused behavior tests and native observation. |
| H5 reusable seams | Pending | Existing CharacterViewer interaction and calibration isolation retained. | Replace brittle source assertions and validate extracted seams. |
| H6 native gate | Pending | Not started. | Build, run, document, commit, push, emit gate. |

The final report will replace pending commit cells with immutable SHAs and list exact native/Web/VSIX acceptance status.
