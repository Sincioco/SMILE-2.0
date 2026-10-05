# Current limits, recovery and evidence

[Start here](../README.md) · [Build and validation](build-and-validation.md)

This is the maintained issue/constraint entry point. Historical PASS records in
the archives describe only their recorded builds. Native Windows is the active
target. Web work is indefinitely paused; separate Studio S1 is abandoned for now.
The existing Viewer is native Sin Star Studio, authoritative for approved Towns,
Battle Systems and Battle Simulations brought into the independent Sin Star I game.

## Metropolis loading optimization — October 5

`TownDocumentMap.Rebuild` now filters each minimap row's ordered surface brushes
into 16-pixel-wide regions before sampling. The 256 x 256 raster, four exact
samples per pixel, brush precedence, eight-row update budget and complete-cache
publication remain unchanged. This reuses `SurfacePaint3D.SelectRegion`; there is
no new cache identity, file format, dependency or renderer allocation. Existing
prepared terrain/road caches and shared catalog model leases remain authoritative.

The isolated native cached-load profile measured **2,973 ms before**, **2,524 ms
after**, and **2,525 ms during pixel verification** (about 15% faster). These are
same-machine process launches with prepared terrain, not cold-disk measurements
or a universal time guarantee. The profile uses copied saves and separate test
ApplicationIds; live maps are untouched. Evidence is under `artifacts/tests/`:
`town-loading-baseline-43ffe242d01f462dadb8d44c45d90b64`,
`town-loading-tiled-879a6bbe341d4423b49a0428aa15f727`, and
`town-loading-pixel-check-c742e7c20af04e7d853439964708e217`. A test-only native seam
compared all 65,536 published pixel values against the previous row-wide algorithm at overview,
cursor zoom, pan and document invalidation: all four comparisons passed.
`scripts/test-metropolis-native.ps1 -PublicationDirectory tools/Character3DViewer/bin/Release`
also passed (`metropolis-da6b84c0630347cba93a6bb98ad1d97c`), including real city
draws, lighting/reflections, map transitions and prepared save/PNG reopen.
`Launch.ps1 -Build -SkipWindowActivation` rebuilt the complete native Studio,
verified 401 published assets and gracefully replaced the previous process.
The relaunched Metropolis scene was visually inspected; its displayed map load
time was **2,346 ms**. This live observation is separate from the controlled profile.

Ownership stays in the minimap module (583 -> 594 physical lines); the existing
600-line review trigger and other exceptions are unchanged. A focused style check
and diff review pass. This bounded performance investigation does not add a
permanent benchmark suite. Remaining startup work includes renderer/document
initialization, catalog uploads, terrain uploads and four party actors; retaining
more GPU assets or preloading other maps would need resource-budget and lifecycle
validation before adoption.

## Viewer export R04

**Implemented; bounded native acceptance passes.** An accepted frozen map without
a matching photograph now prepares and renders its own scene beside the live town.
Warm photographs retain exact accepted content through later edits, cache replacement
and eviction. Save As preserves source identity, including legacy NPC defaults;
per-queue-slot freeze results prevent a failed freeze from adopting stale image data.
The [export contract](viewer-export.md) describes ownership and remaining limits.

| Check | Result and scope |
| --- | --- |
| Warm identity, `town-export-f4921740d657403fa8d2f6b45ab90b28` | PASS at 17 slots, including failed-freeze stale-image rejection, legacy Save As NPC preparation, warm provenance, cancellation and PNG partial failure |
| Real cold scenes, `town-cold-export-6669404964d94d0d99b2711ff89dc4fa` | PASS: Cold, Newer, Copy, Court, Spaceport, Horizon and Metropolis beside a live Metropolis scene; seven exact prepared-document reopens and seven 384 x 240 PNGs |
| Live isolation and cleanup in that cold run | Route contexts, cameras, resident layout, paused party/fire and light state retained; all 11 resource/reservation counters return to baseline after each job and zero after teardown; 32 capture slots work and the 33rd is rejected |
| Pixel evidence | Cold/Copy hashes match; Newer differs. Court, Spaceport and Metropolis PNGs were visually reviewed. The separate scoped-backdrop fixture restores a byte-identical before/after image and changes the image while cleared |
| Native PBR sharing and VFX capacity fixtures | PASS; immutable imported-material leases and native staged-particle admission/lifetime have focused coverage |
| Studio native publication | PASS: `artifacts/studio-r04-final-20261005/Character3DViewer.exe`, 401 assets; SHA-256 `870AABC5A6F754746DDC868943D32B635B82F97677633780C1A18803641CCD60` |
| Town Editor suite | Foundations, routes, render and PNG checks PASS. Current and HEAD-baseline session fixtures have the identical 49 failures: one last-Decor-page assertion, 47 palette-visibility assertions and one exhausted-water-budget assertion. Both compile/publish 401 assets and exit normally with empty stderr. The corrected Save All assertion passes: a batch with any failed map ends at 99%; a completely successful batch reaches 100% |

The final run supersedes the earlier failed native fixture without erasing it.
Historical run `town-cold-export-4b97e0448b0e40f5b1bd1b28c4bbb293` exposed:

| Earlier failure | Current correction |
| --- | --- |
| Large-map admission reached 508–512 materials beside a 384-material live baseline | Shared catalog models plus exact, immutable, untextured native imported-PBR values use reference-counted leases; scene/node overrides remain independent |
| Cold Metropolis failed during composition | Native staged-particle admission is 16,384 on demand: two 5,547-slot city scenes plus the 4,608-slot CPU Fire allowance total 15,702; Web stays at 8,192 |
| Small-map PNG inherited the arena backdrop | `StaticBackdrop3D.ExchangeActive` clears the borrowed selection for capture and restores it through common cleanup, with epoch/generation guards |
| Earlier exact-reopen comparison was invalid | The fixture imports with `PreserveDirty=True`, comparing the prepared serialization without import normalization |

Earlier warm run `town-export-c9539632f7954a1b916fd31fea568581` also passed; the final
warm run adds the stale-freeze and legacy Save As cases. The
[October 3 handoff](archive/2026-10.md#october-3-2100-handoff-export-identity-and-native-acceptance)
retains the original rejection-only behavior and sixteen-map evidence.
Independent cold rerenders need not be byte-identical because authored beacon,
firelight and Sphere effects can read wall time. The resource budget is bounded,
not unlimited scene admission. PBR hardening with the proper manifest passes both
normal and forced runs. The original legacy fixture still has the same 15 failures
on baseline and current code (`pbr-baseline-c027fad7d94e`), with identical full output;
that pre-existing fixture issue is distinct from the passing regressions.

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
