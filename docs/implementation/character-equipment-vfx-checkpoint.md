# Valor and Zara equipment VFX — September 9, 2026

## Requested appearance

Valor retains Arin's sword flame behavior and three faint shield edge emitters,
using Orin's blue/cyan/white fire palette. His shield rim has a white core and blue
halo; his sword uses a white skinned outline. Zara uses a pure white blade rim and
white skinned weapon outline. Her original gold emissive blade remains visible.
This applies to individual tabs and the four-hero Party scene.

## Ownership and assets

- Valor: weapon part 10 (`Sword_Joint1`), shield part 9 (`Shield_Joint`), 14 sockets.
- Zara: weapon part 3 (`Weapon`), no shield, 10 sockets including eight blade rim
  points. Equipment geometry is fully weighted to its dedicated bone in each model.
- Package-local `equipment-vfx-attachments.json` records model checksums and measured
  bind-space points. Socket translations use each bone's inverse bind matrix.
- Model/animation/texture exports and grounding are unchanged. Arin/Orin saved
  calibration is unchanged. The imported packages do not gain pose editing.
- `ViewerEffects` reuses its equipment routines with profile-specific parts.
  `ViewerParty` owns separate Valor/Zara effect instances. Only the primary scene
  state initializes/advances the shared fire pool and VFX clock.
- `ArinShieldRim` now takes caller-owned context, socket prefix, part and color style.
  Three characters can render separate rims without overwriting one another.
- Party uses ten of twelve fire emitters: Arin four, Orin two, Valor four. Zara
  uses three ribbon batches and no emitters. Dragon remains preserved in its tab.
- Weapon/Shield visibility clears the relevant effects, including Party members.
  Valor has the existing fire freeze and shield flame/outline controls.

## Defect corrected during validation

Zara's ribbon-only effect exposed a native renderer topology leak. Reflection
capture ended with a triangle-strip ribbon. The subsequent main-scene mesh draws
assumed triangle-list topology and could render the character as overlapping
triangles. Fire particle draws previously masked this by restoring triangle-list
topology. `smile_3d_draw_submission` now sets triangle-list topology before every
mesh draw, including PBR. Web already supplies the topology to each draw call.
This is a renderer correction, not a model/material change or an extra SMILE API.

## Validation

- `scripts/build.cmd`: native runtime, compiler and VSIX build passed.
- Final Release Character Viewer native and Web builds passed, publishing 92
  project assets. Native output is
  `tools/Character3DViewer/bin/Release/Character3DViewer.exe`; local Web output is
  `tools/Character3DViewer/bin/Release/Web`.
- `test-character-3d-viewer-hardening.ps1 -NativeOnly`: 42 calibration checks,
  calibration-storage isolation and 58 native graphics/input/audio checks passed.
- Repository formatting check passed for 433 SMILE files; `git diff --check` passed.
- Native `Renderer3DReflectionTests` passed with the rebuilt runtime. Final native
  inspection confirmed Zara's body renders correctly with the white ribbon and
  reflective floor enabled.
- A temporary real-asset probe passed against the final runtime: three clip poses,
  Valor blue/white palette on sword and shield emitters, Zara weapon-only glow and
  blade rim, hide/restore, independent Party handles, ten-emitter admission and
  complete emitter cleanup when leaving Party. This bounded investigation is kept
  under ignored `artifacts/temp/equipment-vfx`, not added as a permanent test project.
- Native and Chrome individual/Party visual checks passed. Chrome Party weapon
  and shield hide/restore removed/restored attached effects for all four heroes;
  no browser warnings or errors were reported. Temporary browser viewport settings
  were reset, and the native Viewer was restored to the foreground.
- `scripts/install-vsix.cmd --skip-build` installed and verified SMILE VSIX 2.0.61,
  including all 35 bundled payload hashes. Artifact:
  `artifacts/vsix/Smile.VisualStudio.vsix`.
- Arin's 24 saved calibration keys and Orin's zero-key snapshot were exported and
  remain unchanged in Git. Valor/Zara GLB checksums still match their manifests.
- Final native, Chrome and Party previews are preserved in each character's
  `Private/Previews` folder and checksummed in its package manifest. Private Unity
  models, textures, animations and screenshots are not publicly published.

Detailed build/test logs are in `artifacts/temp/equipment-vfx`. All requested
equipment-glow work is implemented and validated; no manual acceptance check
remains required for this milestone. No language syntax or public runtime API was
added.

## On Hold

Further Sin Star I game updates, Battle Scene Editor, semantic-inspection CLI,
subject-first syntax, SMILE 1.0 changes and unrelated suspended features.
