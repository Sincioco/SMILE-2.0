# Valor and Zara equipment VFX — September 9, 2026

## Vrax attack follow-up — September 9, 2026

Sin explicitly requested mouth fire and arm lightning after the completed
post-delivery hardening. This bounded Viewer enhancement follows notice-fix commit
`35e9dba`; it does not reopen a Loader phase or Sin Star I gameplay work.

`ViewerDragon.AttackEffects` owns one mouth emitter and two arm lightning leases.
The primary `ViewerEffects.State` and Party boss state own independent contexts.
Both consume the same final-pose socket queries and clip-time policy. `Profiles`
cycles Attack through Attack6, and `ViewerParty` uses each selected clip's duration.
`Program.smile` and the once-per-scene VFX clock/draw ordering are unchanged.

The canonical Vrax descriptor adds four head/forearm sockets. Fire points along
the animated head's forward direction; blue-white lightning leaves both forearms
toward the same forward endpoint. These are cosmetic previews, with no combat,
damage, targeting AI, new sounds or model/animation edits. Timing is one-sixth
through four-fifths of each attack. Recovery, Idle, Death, hide and tab teardown
clear owned effects; explicit cuts outrank freeze. Family resume rebases moving
attachments. Failed fire admission retries normally; a partial arm pair releases
both candidates and retries without touching other callers.

The full roster uses ten hero fire emitters plus one Vrax mouth emitter during
attacks: eleven of twelve. The two arm bolts use existing shared lightning slots
and its existing ribbon batches. No limits, global resets or dependencies changed.
The public regression uses synthetic points and existing public fixture assets.
Private licensed model inputs remain local and ignored.

Validation:

- Native calibration isolation passed with `CheckVraxAttackEffects`: six clip names,
  windup/recovery suppression, actual fire/paired-bolt allocation, capacity rollback,
  same-context recovery, independent callers, freeze/resume, Death/hide cleanup and
  restored budgets. The generated Web renderer-state fixture passes the same check.
- Actual HardeningTests pass on native and generated Web with exact expected output.
- Native and Full Web Viewer builds pass (103 native / 92 Web published local assets).
- Native and Chrome visual inspection shows attached mouth fire, both arm bolts,
  and floor reflections. Chrome attack completion removes the effects.
- A bounded local production-model fixture passes on native and generated Web. It
  renders all six attacks in the individual
  tab and Party, verifies all three effect leases, eleven-emitter admission, hero
  preservation on boss hide, recovery on show and complete cleanup on leaving Party.
- Focused six-file SMILE formatting and `git diff --check` pass. No language/runtime
  or VSIX payload changed; the previously verified 2.0.61 installation is retained.

Evidence is under `artifacts/temp/vrax-attack-vfx`. Early probe setup mistakes
(passing presentation indexes to the runtime selector, assuming humanoid equipment
glow on Vrax, and omitting the Web renderer-state harness option) were corrected in
the disposable test inputs/invocation before recording passing results. They were
not suppressed or treated as product passes. No emitted program files were patched.
Native Viewer SHA-256: `139B3590C1328EF427DD3B9B7ADAF15B1F4868CCE79306509DC1B9171AB2388E`.
Full Web game.js SHA-256: `B0D71510BFD305522298508DFBBAFA30D0B3B8183654E742AC34B0D368121C1C`.

The earlier small-screen Continue fix is committed/pushed as `35e9dba`. Its physical
iPhone check and update of the existing public website remain pending the complete
page URL requested from Sin. The local private-roster Web output is not approved
for public asset redistribution and has not been uploaded.

On hold: other VFX/Loader phases, further Sin Star I game work, Battle Scene Editor,
semantic CLI, subject-first syntax, SMILE 1.0 and unrelated suspended features.

## Post-delivery hardening: VFX-01

Package `smile-2.0-post-delivery-review-hardening`, revision 1, verified against
clean HEAD and fetched origin/main `6c523b63c8fca0f64f5c606ec555e30adffd4349`.
All 11 manifest files matched; archive SHA-256
`dace532fde81ca7d23b26bec0a1c4a2929e0396de29db0c8d144e47f7105c627`.
No newer source or user edits required reconciliation.

VFX-01 reproduced on native and compiler-generated Web: two available ribbon
slots left a material and two ribbons stranded, and later freed capacity did not
repair the same context. Texture exhaustion also admitted dependent resources
without the required texture. `ArinShieldRim.TryAcquire` now commits only a complete
texture/material/three-ribbon candidate; failed acquisition releases owned resources
in reverse dependency order and returns failure. A normal later Update retries.
An initialized context retains its handles; other callers and pool limits are unchanged.

`CalibrationTests.CheckEquipmentRimRecovery` and its texture-pressure companion
exercise the real module with public Arin assets through the existing isolated
native/generated-Web harness. Rollback, same-context recovery, an independent healthy
rim, six actual ribbon draws, steady handles, hide/restore, repeated shutdown and
return to starting resource counts pass. Generated Web evidence uses the existing
renderer-state harness, not browser-pixel observation. The longer sequential recovery
test intentionally keeps admission, recovery and teardown in one existing test owner;
the production acquisition helper remains below the 60-logical-line review trigger.
`Program.smile`, canonical models/calibration, palettes and numeric rules are unchanged.

Focused command: `scripts/test-viewer-calibration-native.ps1 -IncludeWebPrecision`.
Reproduction and passing logs: `artifacts/temp/post-delivery-hardening-r1/vfx-*`.
The harness's reused output directory reported SML3605 for its prior isolated
application identity; the generated publication was replaced and exact parity passed.
Final integration, source commit identities and installed-payload delivery are recorded
with the post-acceptance compiler follow-up in the existing Double checkpoint.
VFX source milestone `3a1418b` was pushed normally before the compiler hardening.
The final native/Full Web Viewer builds at `25d80ad` passed brief local Party,
Valor and Zara checks, including foreground Chrome: visible rims, reflected
equipment and intact PBR geometry. These observations cover ordinary presentation;
capacity-exhaustion recovery is proved separately by the production-module fixture.
Private roster assets and screenshots were not published or committed.

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
