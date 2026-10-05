# Current limits, recovery and evidence

[Start here](../README.md) · [Build and validation](build-and-validation.md)

This is the maintained issue/constraint entry point. Historical PASS records in
the archives describe only their recorded builds. Native Windows is the active
target. Web work is indefinitely paused; separate Studio S1 is abandoned for now.
The existing Viewer is native Sin Star Studio, authoritative for approved Towns,
Battle Systems and Battle Simulations brought into the independent Sin Star I game.

## Viewer export R04

The October 3 implementation freezes accepted document/photograph pairs at queue
capture. Same-name/same-revision content mismatches are rejected; later cache
replacement/eviction cannot substitute a newer image. Save As keeps source identity.
PNG failure after town success remains explicit partial success. Blender-only
export has no photo requirement. Cancellation releases bounded queued resources.

**R04 is unfinished.** Its working-tree implementation is uncommitted and fails
native acceptance. The released warm-photo contract above remains distinct from
the [proposed cold-export design](viewer-export.md). Cold capture must preserve
accepted/prepared identity and live-scene isolation; switching the live tab or
mutating the editor document is not an acceptable substitute.

The October 5 native evidence is bounded to these fixtures:

| Check | Result and limit |
| --- | --- |
| Warm identity, run `c9539632f7954a1b916fd31fea568581` | PASS at the current 17-slot bound |
| Seven-map cold fixture, run `4b97e0448b0e40f5b1bd1b28c4bbb293` | FAIL overall beside a live Metropolis scene |
| Cold Cottage + Save As copy | Prepared `.town` reopens exactly; both 384 x 240 PNGs match in this fixture. The earlier reopen mismatch was a fixture import-normalization bug, corrected with `PreserveDirty=True` |
| Newer map, Royal Court, Spaceport, Horizon | Catalog admission fails at 508–512 materials with live Metropolis's 384-material baseline |
| Cold Metropolis | Reaches stage 4, with 426 materials reported at peak, then fails capture/composition; the exact failing owner is not localized |
| Cleanup/isolation checks | No failures reported for live route/camera/resident/fire checks, all 32 capture slots or zero resources after teardown in this run; this does not prove visual isolation |

Visual inspection of the small-map PNG found the active arena's static backdrop
behind the miniature town. Snapshot backdrop policy/isolation therefore remains a
separate blocker even where native capture succeeds. Shared catalog model leases
improve admission but do not resolve the larger asset union. R04 requires corrected
composition, supported resource headroom and a passing real-asset acceptance before
it can be treated as complete. Independent cold rerenders are not promised identical
pixels because some authored effects read wall time.

The [original handoff](archive/2026-10.md#october-3-2100-handoff-export-identity-and-native-acceptance)
retains the October 3 rejection behavior and its historical sixteen-map evidence.
Current source supports 17 maps/tabs/nodes. No native cold-export code is accepted by
this documentation change.

## Recovery and unresolved reports

The launcher exports handled recovery reports to ignored
`artifacts/reports/Character3DViewer`, including the first stage/operation, codes,
actor/clip, Party/Beat timing, camera, resource counts and last 32 input/state events.
Its watcher adds executable SHA-256/process identity; nothing is uploaded. Direct
EXE runs retain only the latest report and backup until the next launch/export.
Use `scripts/export-viewer-recovery.ps1`; game reports use
`-ApplicationId smile.game.sin-star-i`. This is not an OS crash dump and cannot
record termination that bypasses handled recovery. Reports never change poses/drafts.

- **Kael Party intermittent recovery:** unresolved; investigation was paused at
  Sin's time limit. Preserve reports and the
  [checkpoint evidence](../../../docs/implementation/party-beat-camera-checkpoint.md#open-native-recovery-report--september-20-2026).
  A successful unrelated fixture is not proof of repair. Resume only on direction.
- **Initial Arin native load rejection:** its recovery retry was repaired, but the
  original trigger was not reproduced after restart. On recurrence retain exact
  rejection text, clip/profile and canonical/live hashes before edits/restart, then
  reproduce that transition in an isolated fixture. Do not restore historical poses
  or weaken fingerprints. See [rejection evidence](../../../docs/implementation/party-beat-camera-checkpoint.md#open-native-arin-load-rejection-trigger--september-21-2026).
- **Mira1 comparison repair:** historical detailed visual acceptance was interrupted;
  passing build/regressions did not complete it. The profile is currently hidden;
  see its [package](../../../games/SinStarI/SourceAssets/Characters/Healer/MiraV1/README.md).

## Retained limits

- Protected Spaceport/Horizon assemblies cannot be moved by bulk section edits;
  Royal Court can move but cannot duplicate. Curved/elevated/directed-water section
  transfer rejects unsupported content before replacing a destination.
- `.town` has a 512 KiB authored-document budget. Guide history/overlays are separate
  from town/Blender files. Import requires compatible catalog identity. Arbitrary
  Blender mesh/material/surface edits are outside the assembly import contract.
- Town door events expose entrances, not authored interiors/cutscenes. Collision
  uses bounded authored surfaces/obstacles rather than a complete triangle navmesh.
- Dragon's foot IK is baked during authoring; flight and terrain-adaptive runtime
  IK are unsupported. Original angular wing topology remains an art limitation.
  Pose tools remain lightweight wrist/equipment correction, not a full DCC suite.
- Very short editor windows can crowd controls. VFX/reflection allocation failures
  retain bounded fallback behavior; scene-wide effect limits still apply.
- Save processes/tabs do not merge concurrent edits. JSON content identity wins over
  timestamps; never overwrite unknown/rejected storage. Historical Chrome/GPU/fallback
  checks do not establish arbitrary-GPU, Firefox, Safari or physical-device coverage.
  Automatic full-scene graphics context recovery is not guaranteed.

## Evidence and document precedence

Current workflow lives in this guide set; current code and applicable repository
instructions resolve conflicts. [ARCHITECTURE](../ARCHITECTURE.md) is the owner map,
[town authoring](../../../docs/architecture/town-authoring.md) owns shared contracts,
and canonical packages own assets/repair histories. Linked historical evidence
retains old tab counts, graph gestures, camera/layout numbers and separate Studio
plans without overriding the current contracts.

The [October README archive](archive/2026-10.md) and [September/undated README archive](archive/2026-09.md)
retain the entire former README body. The [October architecture archive](archive/architecture-2026-10.md)
and [September/undated architecture archive](archive/architecture-2026-09.md) retain
the entire former ARCHITECTURE body. Relative links and old heading anchors are preserved. They preserve
one-time validation, old r008/r009 and southwest Spaceport layouts, Save Games repair
paths, original S1/Web instructions, and the earlier coverage mapping without
presenting any of them as fresh acceptance. New results belong beside the relevant
owner/issue, not as another long diary entry in the compact README.
