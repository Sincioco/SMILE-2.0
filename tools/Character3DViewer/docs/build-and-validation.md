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

Launch reconciles Arin/Orin JSON by **content hash**, exports pending accepted live
saves, and watches both distinct working saves while the editor runs. See
[calibration authority and recovery](characters-and-calibration.md#calibration-transfer).
Output relocation changes neither ApplicationId, storage keys nor fingerprints.

The initial window is 1440 x 960 unless placement was remembered. Use a taller
window for the full Town Editor palette. Below 800 x 540 logical units, Continue
dismisses the small-screen notice for this session and keeps cramped panels hidden.
Startup shows the official logo for at least one visible second; compile metadata
comes from the artifact, not the current clock. See the
[startup contract](../../../docs/architecture/startup-presentation.md).

## Focused validation

Choose the checks that exercise the change; do not treat this list as a requirement
to rerun unrelated suites. Inspect each script's prerequisites and target flags.
Run native town checks sequentially against complete assets:

```powershell
pwsh -NoProfile -File scripts/test-town-export-photographs.ps1
pwsh -NoProfile -File scripts/test-neris-acceptance-policy.ps1
pwsh -NoProfile -File scripts/test-neris-town.ps1
pwsh -NoProfile -File scripts/test-town-editor.ps1
```

The [Viewer export guide](viewer-export.md#acceptance-boundary) separates the
established warm contract from unfinished R04 work. The working-tree-only
`test-town-cold-export.ps1` uses actual assets beside a live Metropolis scene. On
October 5 its seven-map fixture **failed overall**: small Cottage/Save As checks
passed, but larger-map material admission, Metropolis capture and inherited
backdrop composition remain blockers. Warm identity passed at the actual 17-slot
bound. See [exact evidence and limits](status.md#viewer-export-r04); a passing build
or warm fixture does not close cold-scene acceptance.

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
