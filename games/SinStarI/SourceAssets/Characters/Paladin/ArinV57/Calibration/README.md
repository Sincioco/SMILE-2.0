# Arin v5.7 Pose Calibration

This directory is the permanent repository-owned copy of Arin v5.7 Character Viewer pose corrections.

- `arin-v5.7-pose-calibration.json` is the permanent, human-readable source of truth used for review, manual inspection, comparison, commits, and future Arin v5.8 reference work.
- The live working copy remains in stable SMILE application data so it survives native viewer rebuilds.
- `scripts\sync-arin-v5-7-calibration.ps1` exports the live binary working copy to JSON or validates and restores JSON into SMILE application data when that working copy is absent.
- `tools\Character3DViewer\Launch.ps1` starts a small background synchronizer so every subsequent `Save Frame` is mirrored to JSON while that editor session is open.

The JSON may be read and edited by a human. Prefer using the viewer/editor and
Save Frame, then review the generated JSON before committing.

## Hardened Storage (M7E-G0)

JSON schema 2 / runtime storage 3 binds each clip by its exact, case-sensitive
name. `index` is a recomputed runtime hint, not its identity. Every key contains
all 18 numeric channels plus both decoupling flags. Unknown clip names remain in
JSON for review but are not applied to another clip. Frame bounds, sorted unique
keys, integer ranges, three-component vectors, counts and saved-frame references
are validated before any write.

`arin-v5.7-profile.json` records the asset ID/version, model, descriptor and cooked
hashes, eight clip sample counts and thirteen ordered socket names. A mismatching
asset identity requires an explicit migration; editing a version label is not a
safe migration. Known version-2 runtime payloads migrate without changing channel
values. Version-1 payloads require `-MigrateLegacy`; missing decoupling defaults to
false only during that explicit legacy migration.

Successful writes use a flushed temporary file and atomic replacement, keeping
the prior good bytes in `.bak`. Backups and runtime envelopes are ignored, not
repository sources. Malformed data, path escape and concurrent replacement are
rejected without overwriting the last good copy. Watcher reads permit atomic
replacement while a reader is open. **Undo Last Change** restores one previous
saved key set, including a deleted clip's keys; it is session-local, not unlimited
history. Reload Key discards the current unsaved preview.

Read-only checks from the repository root:

```powershell
scripts/sync-arin-v5-7-calibration.ps1 -Mode Validate
scripts/sync-arin-v5-7-calibration.ps1 -Mode Compare
```

Compare exits 2 when the normalized live and canonical snapshots differ. Backup
creates an exact content-addressed copy; Restore with explicit SourcePath and
DestinationPath restores a chosen JSON backup and requires Force before replacing
an existing destination. Default Restore still means the launcher's safe
JSON-to-runtime import, without overwriting a live working copy. All write paths
must remain in this repository or SMILE application data; reparse paths are refused.

The live key count is deliberately not fixed in this README: Sin continues to
refine poses. The latest successful Save Frame and exported JSON take precedence
over historical eight- or nine-key planning snapshots.
