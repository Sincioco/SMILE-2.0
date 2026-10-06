# Native build, launch and validation

[Start here](../README.md) · [Current status](status.md) · [Ownership](../ARCHITECTURE.md)

Run commands from the repository root in PowerShell 7. The complete
`Character3DViewer.smileproj`, its declared assets and the canonical game packages
are required. `Build.ps1` defaults to `-Target All`; always pass **`-Target Native`**
during the native-only work period. Do not use the historical `-Studio` route.

## Build while an editor is running

```powershell
pwsh -NoProfile -File tools/Character3DViewer/Build.ps1 -Configuration Release -Target Native -OutputDirectory artifacts/studio-staging
```

Choose a staging directory that no running Studio process owns. Build refuses to
overwrite a running publication. Never remove Asset/Model3DAsset declarations to
make a test project smaller and publish it over the live output with the same
ApplicationId: publication intentionally deletes undeclared assets. The September
29 stripped publication removed all 333 then-declared assets; its repair evidence
is [archived](archive/2026-09.md#safe-native-builds-and-launch).

Code-only experiments also need their own publication directory. A different
output directory alone does **not** isolate saves: use fixture ApplicationIds and
disposable save data for tests. Do not substitute a test EXE for the user's editor.
Never overwrite user maps from generated defaults during build or launch.

## Launch the working editor

```powershell
pwsh -NoProfile -File tools/Character3DViewer/Launch.ps1
# Graceful native rebuild and relaunch, after preserving pending edits:
pwsh -NoProfile -File tools/Character3DViewer/Launch.ps1 -Build
```

The default is `bin/Release/Character3DViewer.exe`; `-Configuration Debug` selects
Debug. `-Executable <path>` launches an existing custom build and cannot accompany
`-Build`. The launcher validates prerequisites before requesting normal closure,
never force-terminates an editor, and verifies the complete publication before
launch. `Check-Publication.ps1` checks the manifest, declarations and published files.
Use `-SkipWindowActivation` only when automation owns subsequent window activation.

`Prepare-BuildAssets.ps1` verifies canonical/private package prerequisites and
refreshes ignored mirrors. Do not edit `BuildAssets`, cooked models or binary saves
as canonical sources. The game lives in `D:\SMILE 2.0 - Sin Star I`; its build runs
through its own `Build.ps1`. The ignored `games/SinStarI` junction is compatibility
access, not an engine-owned asset copy. Studio remains authoritative for approved
town and battle behavior brought into the game.

Launch reconciles Arin v5.7/Arin v5.8/Orin JSON by **content hash**, exports pending accepted live
saves, and watches all three distinct working saves while the editor runs. See
[calibration authority and recovery](characters-and-calibration.md#calibration-transfer).
Output relocation changes neither ApplicationId, storage keys nor fingerprints.

The initial window is 1440 x 960 unless placement was remembered. Use a taller
window for the full Town Editor palette. Below 800 x 540 logical units, Continue
dismisses the small-screen notice for this session and keeps cramped panels hidden.
Startup shows the official logo for at least one visible second; compile metadata
comes from the artifact, not the current clock. See the
[startup contract](../../../docs/architecture/startup-presentation.md).

## Focused validation

Arin v5.8 uses `ArinV58Tests.smileproj` and the exact expected line
`Arin v5.8 native clips, draw and calibration isolation PASS`. It loads/draws all
11 actual clips, checks the 21-socket publication and three parts, verifies separate calibration
banks, and exercises the native 100,000-vertex/300,000-index limits. The cooker
boundary regression is `scripts/test-model3d-part-limit.ps1`. Blender face,
equipment and floor evidence stays in the canonical ArinV58 package.
The older `test-viewer-calibration-native.ps1` explicitly pins its generated
profile to v5.7 because its populated parser fixtures and accepted pose matrices
belong to that historical model. Active v5.8 uses ArinV58Tests plus native
Party/battle/town validation; historical pose keys are never seeded into v5.8.

Milo's isolated native fixture is `MiloTests.smileproj`. Compile with the installed
`artifacts/compiler/smilec.exe --project tools/Character3DViewer/MiloTests.smileproj
--target windows-x64 --configuration Release --graphics DirectX
-o artifacts/milo-native/MiloTests.exe`, then run it through
`scripts/Invoke-TownNativeCheck.ps1` with the exact expected line
`Milo native profile, eight clips, draw and actor isolation PASS`.
It checks all eight cooked clips/sockets, actual draws, independent animators and
the regression for Milo's initially cropped framing. Package export/loop/contact
validation runs separately in Blender via `Pet/MiloV1/preview_milo.py`.

Choose the checks that exercise the change; do not treat this list as a requirement
to rerun unrelated suites. Inspect each script's prerequisites and target flags.
Run native town checks sequentially against complete assets:

```powershell
pwsh -NoProfile -File scripts/test-town-export-photographs.ps1
pwsh -NoProfile -File scripts/test-town-cold-export.ps1 -PublicationDirectory "artifacts/studio-r04-final-20261005"
pwsh -NoProfile -File scripts/test-renderer3d-pbr-sharing.ps1
pwsh -NoProfile -File scripts/test-neris-acceptance-policy.ps1
pwsh -NoProfile -File scripts/test-neris-town.ps1
pwsh -NoProfile -File scripts/test-town-editor.ps1
```

The [Viewer export guide](viewer-export.md#acceptance-boundary) distinguishes the
17-slot warm identity regression from real-asset cold capture. Both pass: the cold
runner exports seven town/PNG pairs beside a live Metropolis scene, verifies exact
prepared reopens, state/resource restoration and capture limits. Native PBR-sharing,
scoped-backdrop and VFX-capacity fixtures also pass. See
[exact run IDs and limits](status.md#viewer-export-r04).

The final Studio native publication passes with 401 assets. Town Editor foundations,
routes, render and PNG checks pass. The session fixture retains the exact 49 failures
also present on HEAD baseline (Decor page, palette visibility and exhausted water
budget); its corrected partial-failure Save All assertion passes. PBR hardening
with the proper manifest passes normal/forced runs, while the
original fixture's 15 baseline/current failures remain a separate known issue. Run
only the native VFX fixture during this work period: `test-renderer3d-vfx-batches.ps1` also contains
Web compilation/execution and is not a native-only command.

The Neris/town-session runners accept `-PublicationDirectory <verified folder>`.
Metropolis has `scripts/test-metropolis-native.ps1 -PublicationDirectory <folder>`.
Other routes are `test-viewer-battle.ps1`, `test-viewer-calibration-native.ps1`,
`test-character-3d-viewer-hardening.ps1`, `test-character-viewer-preservation.ps1`
and `test-character-3d-viewer-actor-isolation.ps1` under `scripts/`; architecture
documents map their ownership and scope. No Web flag or browser run is implied.

`Invoke-TownNativeCheck.ps1` requires bounded completion, exit code zero, the exact
PASS line and no FAIL marker in either stream. Only the process started by that
check may be terminated on timeout. Native negative controls cover exit 7 after
PASS, missing PASS, PASS plus FAIL, timeout and a failed actual draw. Generated
JavaScript/source checks and artifact checks are distinct from native execution.
Record manual native inspection separately. Never use live user calibration for
failure tests. Historical test runs remain in the archives, not current pass claims.

Source-only Viewer changes require a native Studio rebuild/relaunch. Rebuild .NET
or reinstall VSIX only for compiler/runtime/extension changes that require it;
recook Blender only for changed source assets. Web publication, browser refresh
and the separate Studio S1 workflow remain outside the active work.
