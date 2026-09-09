# Red Dragon Vrax Animation Trial

September 10, 2026. A reversible experiment built from RedDragonV11 and VraxV1.
The normal Dragon package, default Viewer build and Party Dragon choreography
remain unchanged. The separate trial Viewer adds ten selectable clips to Dragon:
`Vrax_Idle`, `Vrax_Walk`, `Vrax_Run`, `Vrax_Hit`, and `Vrax_Attack` through
`Vrax_Attack6`. Its original six animations remain available.

## Preview And Restore

From the repository root, open the trial:

```powershell
& games/SinStarI/SourceAssets/Bosses/RedDragon/RedDragonV12VraxTrial/Preview.ps1 -Mode Trial
```

Click **Dragon**, then **Next Page** for the adapted attacks. To return to the
exact saved Viewer and original Dragon:

```powershell
& games/SinStarI/SourceAssets/Bosses/RedDragon/RedDragonV12VraxTrial/Preview.ps1 -Mode Original
```

This switches executables; it does not overwrite the original model. The 19-file
original Dragon backup is in `Private/OriginalDragonV11`, verified against
`original-backup-checksums.json`. `Private/OriginalViewer` preserves the complete
September 10 baseline application, including `Assets`, `TechnicalAssets` and the
publication manifest. Its private verification manifest records every file hash.
The baseline executable SHA-256 is
`269348985DF3811B611F08E42196F2148533CD00C348A7BE8F63D84AF7BFC102`.

## Adaptation And Limits

`build_trial.py` runs in background Blender against the preserved source files.
It compares Vrax's evaluated world rotations to his Idle reference, transfers
bounded rotation deltas onto Dragon's rest rig, and bakes explicit quaternion keys.
The mesh, skin weights, UVs, bone lengths and accepted scale are unchanged.
Wings retain their sideways rest direction; smaller gains and angle limits avoid
copying Vrax's forward tentacle reach or longer limb lengths directly.

Dragon's small hands share the wing chain. This rig cannot independently reproduce
all four tentacles; its wing-root/arm/tip mapping blends the two source chains.
The experiment is an approximation, not a production retarget. Per-frame root
height prevents body penetration; it does not provide IK foot planting, so raised
or sliding feet and simplified limb motion may remain. The other fourteen Vrax
clips, including Death, have not been transferred. The original six Dragon clips
have not been renamed or replaced in Party playback.

The open Blender scene was preserved. Its MCP server was unavailable, so the
installed Blender 5.2.1 was used in a separate background process. Original Unity
and Blender assets were not edited. Licensed motion, the candidate GLB/Blend,
compiled previews and screenshots are local-only under ignored `Private`.

## Rebuild And Evidence

Run Blender with `--background --factory-startup --python` pointing at
`build_trial.py`, then `validate_trial.py`. The latter reimports the exported GLB
at 30 Hz, checks every frame of all ten added clips and renders three previews.
`retarget-validation.json` records the mapping, invariants and asset hashes;
`roundtrip-preview-validation.json` records exported-frame floor measurements.

```powershell
& games/SinStarI/SourceAssets/Bosses/RedDragon/RedDragonV12VraxTrial/Preview.ps1 -Mode Trial -Build -NoLaunch
& games/SinStarI/SourceAssets/Bosses/RedDragon/RedDragonV12VraxTrial/Preview.ps1 -Mode Trial -Target Web -NoLaunch
```

Both targets compiled successfully. Chrome selected `Vrax_Attack` on page two,
reported 16 clips and no page errors. Native playback and the original restore
were inspected. The Viewer animation grid now pages any profile with more than
nine clips; previously the trial's extra buttons overlapped the floor controls.
The focused SMILE formatter check passed.

An initially incomplete original-Viewer copy caused a PNG decode error because
its technical VFX atlas was missing. The snapshot now copies and verifies every
published file; relaunching the original verified the correction.
