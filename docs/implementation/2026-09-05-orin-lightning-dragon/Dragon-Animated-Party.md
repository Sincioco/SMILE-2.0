# Red Dragon animated Party milestone

Status: implemented, native build and live inspection passed.
Branch: main. Actual starting commit: `aec603808c096f38b840e5f96bdd546c887a53e6`.
Implementation: `60fdab504a442170b6e4025f7ac75f08098ab6c4`.
Final validation: `e31cb70d358e26b61c9a50cc4c4f2f09f1d5c783`. Pushed and verified against origin/main.

This was the last feature implementation requested by Sin. The self-contained
`games/SinStarI/SourceAssets/Bosses/RedDragon/RedDragonV11` package preserves the
original Tripo3D model, cleaned static source and red cyber-dragon reference.
It adds an editable Blender rig, five original clips, an animated GLB, four sockets,
SM3D descriptor, package/checksum manifests and pose-validation results.

`scripts/rig-red-dragon.py` creates a 24-bone rig. Idle moves the wings, head and
tail; Roar opens the jaw; FireBreath charges and opens the mouth; ClawStrike raises
the attacking limb; Hit reacts to party contact. Animation durations are 4, 3, 4,
2.2 and 0.8 seconds respectively, sampled at 30 Hz. The original cleaned 9,912
triangles, UVs and texture remain. A 2× source scale with actor scale 25,000 preserves
the prior static dragon's arena size using the existing Character3D bound.

Party runs Arin → Orin → Dragon. The dragon alternates Fire Breath and Claw Strike,
then returns to idle. Shared FireEmitter3D fire follows the animated Mouth socket,
first toward Arin, then Orin. Characters guard and play their own hit reactions.
Claw Strike includes a smooth forward approach and return. Lightning now targets
the animated Chest socket. Shared VFX pools accommodate both characters and boss.

Validation:

- Blender 5.2.1 builder: 24 bones, five clips, unchanged cleaned triangle count;
  first/middle/final samples for all clips are finite, bounded and above the floor
  tolerance. Detailed bounds are in `rig-validation.json`.
- `tools/Character3DViewer/Build.ps1`: native Release cooking and compilation passed;
  37 assets published. No new syntax/runtime ABI or VSIX change for the dragon.
- `scripts/test-character-3d-viewer-hardening.ps1 -NativeOnly`: passed, including
  the new animated package checks and retained original source/geometry checks.
- Live native inspection: both dragon attacks, approach/return, fire from mouth to
  party, guarded hit reactions, retained equipment VFX and continuing full orbit.
- Both calibration exports and targeted formatting/diff checks passed.

Native screenshots: `dragon-fire-breath.png` and `dragon-claw-strike.png`. The PNG
inside the dragon package is a separate Blender rig render, not native evidence.
Hashes/dimensions are recorded in the package and shared evidence manifests.

Limitations: preview rig, with original angular wing/mouth topology; no IK, flight,
facial authoring system, navigation, collisions or gameplay damage. Sin's final
visual approval is not claimed. Web visual work remains deferred. A future art
review can refine weight transitions and attack contact without changing the compiler.
