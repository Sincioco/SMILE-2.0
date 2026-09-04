# Arin v5.7 Pose Calibration

This directory is the permanent repository-owned copy of Arin v5.7 Character Viewer pose corrections.

- `arin-v5.7-pose-calibration.json` is the permanent, human-readable source of truth used for review, manual inspection, comparison, commits, and future Arin v5.8 reference work.
- The live working copy remains in stable SMILE application data so it survives native viewer rebuilds.
- `scripts\sync-arin-v5-7-calibration.ps1` exports the live binary working copy to JSON or validates and restores JSON into SMILE application data when that working copy is absent.
- `tools\Character3DViewer\Launch.ps1` starts a small background synchronizer so every subsequent `Save Frame` is mirrored to JSON while that editor session is open.

The JSON may be read and edited by a human. The synchronizer strictly validates clip indices, frame order, channel triplets, ranges, and saved-keyframe references before restoring it. Prefer using the viewer/editor and `Save Frame`, then review the generated JSON before committing.
