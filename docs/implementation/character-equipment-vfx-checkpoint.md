# Valor and Zara equipment VFX — September 9, 2026

## September 10 follow-up: Vrax Original Audio

The original Unity controller and BattleSystem scene resolve Sound13/14/15 to
the swoosh, growl and death vocalization. VraxV1 now owns their import recipe,
source GUID/checksum manifest, private originals and three PCM16 stereo 44100 Hz
runtime copies. Attack through Attack6 play growl at clip start and swoosh at
200 ms; Death/Death2 use the original death voice. No Hit sound was defined by
the original controller. No model, animation, VFX or Unity project changes.

`ViewerDragon.UpdateVraxAudio` serves standalone and Party callers with separate
state and channels 4/5. Pause/hidden opponent and teardown stop the new channels;
clip-time cue handling preserves speed and silent seek semantics. PublicRoster
excludes the private audio wildcards. Original assets are not tracked publicly.

Native and normal Chrome Web builds pass; focused formatting passes four changed
SMILE sources. Native standalone/Party playback was inspected. Chrome measured
standalone cues at 12/202 ms and Party cues at 16/201 ms, and verified all three
nonzero decoded AudioBuffers, separate channels, pause stops and no page errors.
An initial browser test interceptor missed the generated script's cache query;
correcting that test-only URL match resolved its ReferenceError. There was no
production runtime error. Audio output/mix remains subject to Sin's preference.
The separate PublicRoster Web build also passes (35 published assets), with no
Zara or Vrax private audio files in its output.

Local native delivery: `artifacts/deliverables/Character3DViewer-VraxAudio-2026-09-10`
(112 files verified, including technical textures). Executable SHA-256:
`0c759fa3023c91c009c9496a9bbfe7d65810ba5d1cf4c9cc02d4ce44dd5ba5dc`.
Web delivery: `artifacts/deliverables/Character3DViewer-VraxAudio-Web-2026-09-10.zip`
(105 entries byte-compared, 199452966 bytes), SHA-256:
`3efa0d7c8f5c691230157c311c0c86ba004e3de2c22f54dc57e5df25b3861490`.
These are local licensed-roster deliveries, not a public website deployment.

The reversible Dragon retarget experiment was separately validated and pushed
as `ca7e9fe8f68f373b09f9b04ad5dac8a4bdfc2db3`. Its Trial/Original launcher and
unchanged original Dragon backup remain under RedDragonV12VraxTrial. The original
snapshot's missing technical-texture defect was repaired and verified there.

## September 10 delivery: Party cinematics, Zara effects/audio and tab loading

This entry supersedes the earlier presentation descriptions below. Work began
from clean `main` at `746704807cac8a65057cbc43e58a9b83bae81aa5`, matching
`origin/main`, and stayed with one agent. The completed Double, refactor and
post-delivery hardening milestones were not reopened.

### Implemented behavior

- Party Vrax has a level horizontal 360-degree intro, eased into continuous slow
  Camera 1 revolutions. Close-up/tracking battle shots use the same angular rate.
  Sin's final hero references replace the attack/impact/recovery compositions
  with a low frontal shot: the full attacking hero aligns with the imposing boss.
  Impact timing remains intact; Beats 3/3a/4 no longer cut out to the wide orbit.
  The complete camera pose/lens lock at Beat 3 through Beat 4. The selected hero
  determines its horizontal angle around the established arena target; distance
  846, camera height 85, aim height 121 and lens 24 match the final three references.
  No tracking, pan, orbit or dolly occurs during the hold. Neither actor's authored
  placement is changed to fit. Pictures from different battles are composition
  guides, not a motion sequence. This final fixed-shot direction supersedes the
  earlier requests to turn the hero camera while following the attacker.
- Vrax's own sequence closes up, follows his Run approach to the selected defender,
  holds Beat 3 for the complete attack, skips Beat 3a, then shows his return.
  Sin's final reference changes Beats 3/4 to a low camera planted behind the
  defender, with a fixed lens/position; only the look direction tracks Vrax.
  The defender remains in full back view while Vrax and the attack effects dominate.
  Body/head/fire/lightning aim at the selected defender. Hit reactions finish.
- Party Vrax hides Valor and balances the remaining three silhouettes around Orin.
  Independent controls retain Arin/Orin 150 and Zara/Vrax 100. Standalone
  Valor/Zara remain 200 and Vrax 100. No canceled 200-speed Party change was applied.
- Party Dragon restores the `c7f8f07` pre-Vrax formation, approach positions,
  timing and camera path. The two Party policies remain independently owned.
- Zara's red-tinted white outline covers the complete weapon silhouette.
  SwordAttack uses Godstorm Ultra; SwordAttack2 uses Forked Judgment. Four
  crimson strikes preserve shared scene capacity. The 5-percent charge and
  25-percent strike coordinate effects, reaction and camera timing.
- Original Unity discharge/impact audio is privately staged alongside existing
  Lightning Lab thunder on independent channels. Unity visual effects were not
  imported. The previous Zara appearance remains backed up locally at
  `artifacts/deliverables/Zara-SMILE-before-Unity-VFX-20260909`.
- Every tab click restarts that tab using the existing SMILE loading presentation,
  including cached clicks; the old View label is removed. Shared `Window_Loading()`
  preserves the canonical logo, original artifact metadata, credits and links,
  with a fresh one-second visible minimum. Its owners and evidence are in the
  startup checkpoint. No emitted file was edited.
- Web PBR now orients its face normal consistently with the native mesh winding,
  including reflections. This repairs the flat metallic appearance without extra
  lights, texture samples, draw calls or reduced quality.

### Validation and evidence

Evidence is under ignored `artifacts/temp/zara-attacks`:

- `smoke-delivery.log`: normal `scripts/smoke-test.cmd --skip-doctor` passed,
  including 323 managed tests, formatter integration/style, startup and the
  applicable native/Web renderer/game regressions. No Doctor or retired tests ran.
- `startup-delivery.log`: repeated native visible intervals of 1,000–1,031 ms,
  slow-load overlap, Web progress/visibility, and actual generated API calls passed.
- `hardening-final-native.log`, `hardening-final-web.log` and
  `calibration-delivery.log`: actual Viewer owners, native/Web exact console
  parity, storage isolation and precision passed. Final shot changes received
  focused `hardening-low-angle.log` / `hardening-low-angle-web.log` checks.
- `probe-cameras.log` / `probe-web-delivery.log`: locally licensed actual-model
  probes cover Zara effects, Vrax full Hit, selected-defender approach/return,
  independent rates and the historical Dragon camera path. The core shared
  regressions continue using public/synthetic inputs.
- `browser-delivery.log`: Chrome tab/repeated-tab splash metadata and all seven
  links, real audio buffer starts, and short drag/pan/zoom/reset checks passed
  without page errors. The native tab splash was also visually observed.
- `native-final-delivery.log` / `web-low-angle.log`: final native and Web Viewer
  builds use the current source and licensed local roster. Only affected Viewer
  outputs were rebuilt after the final camera adjustment.
- `browser-heroes-final.log`: all three heroes retain identical camera positions
  and look targets through attack/recovery in Chrome, without page errors.
  The six `hero-frontal-*-attack.png` / `hero-frontal-*-return.png` images were
  visually reviewed. The first fixture waited only for Walk, but Orin returns
  with Run; its selector was corrected to require the same hero and either clip.
  The earlier Vrax three-defender review remains valid for the unchanged boss camera.
- `vsix-delivery.log`: SMILE 2.0.61 installed with all 35 payload hashes matching.
  `installed-compiler.log`: the installed compiler built and ran Window_Loading.
  No repeat VSIX build/install was needed for subsequent Viewer-only framing.

The initial reload test used a reserved identifier and then did not await a GUI
process; both fixture issues were corrected and its full checks passed. An old
hardening countdown expected four seconds before the new boss approach was
included; its expectation was corrected to six and native/Web checks passed.
A camera capture made while a native input test was running had orbit disabled;
it was excluded and the visual review was rerun sequentially with Camera 2 asserted.

### Boundaries and delivery

Valor's comparison is recorded in its package journey: normal/metallic maps
transferred, but Unity's material color/eye smoothness factors differ, and SMILE
does not yet have Unity's environment-image reflections. Its canonical derivative
was preserved. Matching-pose/material-only review and reusable environment lighting
are possible next work, not part of this delivery. No exact Unity/SMILE visual
parity or physical iPhone/Safari check is claimed.

Arin/Orin calibration exports preserve the authored 24/zero-key snapshots.
Raw licensed models/audio, private previews, generated cooking mirrors and the
local old-look backup remain ignored. The normal Web ZIP includes only the
authorized cooked publication. Public-site upload remains Sin's publication step.

The final desktop executable was launched through `Launch.ps1` with both live
calibrations preserved and was confirmed responding. A subsequent Desktop UI
capture reported a physical Escape stop; no final native screenshot is claimed.
The focused native camera fixture and final Chrome visual review passed.

Delivery artifacts (ignored local outputs):

- `artifacts/deliverables/Character3DViewer-2026-09-10/Character3DViewer.exe`,
  SHA-256 `269348985df3811b611f08e42196f2148533cd00c348a7be8f63d84af7bfc102`.
- `artifacts/deliverables/Character3DViewer-Web-2026-09-10.zip`, 199,031,440 bytes,
  SHA-256 `82961a467fe4fb4f0f239cc6a7e14670d3f8c2dafc8e1287981c9e6f8830de7f`.
  All 102 archive entries were compared byte-for-byte with the final publication.

Current On Hold: none, following Sin's explicit instruction to clear the list.

## Permanent roster and battle presentation follow-up — September 9, 2026

Sin confirmed that Valor, Zara and Vrax are purchased characters to remain in the
Character 3D Viewer. The checked-in profile policy now enables all three, and the
normal project declares their ignored `BuildAssets` cooking mirrors. The preparation
owner verifies each canonical private GLB against its package manifest before
refreshing that mirror. Normal direct Desktop/Web compilation therefore retains the
seven Viewer tabs after preparation; raw purchased files remain in ignored `Private`
folders. `-PublicRoster` remains an explicit compatibility Web build rather than the
normal roster.

The Party formation moved from a 300-unit/40-degree arc to a 450-unit/60-degree
arc. A 250-ms lead and 300-ms approach replace the former 650-ms approach before
each hero attack; the 700-ms return remains. Dragon FireBreath now uses radius 16
and intensity 300. Vrax keeps one mouth emitter and two arm-lightning leases while
increasing mouth-fire radius to 24, intensity to 320, reach to 420 and velocity to
900. No pool limit, model, animation, socket, calibration or shared clock changed.

## Public small-screen notice delivery — September 9, 2026

Sin supplied `http://sincioco.com/smile/character3dviewer/`. The existing Azure
publication's index matched the HTTP response byte for byte. Its old build
`9d92ccfd666eca49` reproduced the blocking notice in Chrome at 844 by 390.

`Build.ps1 -Configuration Release -Target Web -PublicRoster` now explicitly selects
the original public Arin/Orin/Dragon roster even on the development machine with
private Unity packages installed. It writes to `Web - Public`, preserving the
normal native and private-roster Web outputs. This is a build-owner selection;
no generated program, coordinator, character asset or calibration was edited.

The build passes with 35 public assets. All 41 live files were backed up and
compared: 38, including every character/texture/calibration asset, are byte-identical.
Only `game.js`, `smile-runtime.js` and `index.html` changed. The new public build
is `38405a79e1f708d8`, compiled September 9 at 14:40:25 +08:00 with SMILE 2.0.61.
The native/private Web executable hashes still match the Vrax delivery below.

The three changed files were staged and checked over FTPS, installed with the
index last, and verified by SHA-256 through the public HTTP URL at
14:49:06 +08:00. The first Python FTPS upload timed out during TLS shutdown;
reconnection proved that staged file complete, and the installed curl client
finished the transfer with certificate verification enabled. The failed attempt
did not modify the live files. Local rollback copies remain available.

Published SHA-256:

- `game.js`: `5a5a3c954463cbbd0128e9ff015c4378743b5b5b9059086a7cd3df1d9dcd313d`
- `smile-runtime.js`: `001f939db3e8ef34dd6b98c5ddff1b055fdbfd273446acb035fd5dd03ac13f19`
- `index.html`: `2699476bea74265ceb49cf132106c6a81606b2d396a556bc175c55ef9ba8454a`

Local Chrome inspection confirms Continue removes the notice and reveals the
running public Party scene. Earlier ignored clicks came from an unfocused Chrome
window; bringing Chrome forward resolved the automation issue without a code change.
On the live URL, Chrome at 844 by 390 shows Continue and clicking it removes
the overlay while Party playback continues. Dismissal remains after resizing to
390 by 844. A fresh portrait reload also shows Continue; activating it removes
the notice. Both layouts report no console errors. Physical iPhone/Safari
observation remains unperformed. Evidence and the rollback copy are under
`artifacts/temp/viewer-mobile-publication`. No private roster assets were uploaded,
and no unchanged native, smoke or VSIX build/install was repeated for this delivery.

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

The earlier small-screen Continue fix is committed/pushed as `35e9dba`; its public
website delivery follows above. The physical iPhone check remains unperformed.
The local private-roster Web output is not approved for public asset redistribution
and has not been uploaded.

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

None. Sin cleared the list; historical phases are not automatically restarted.
