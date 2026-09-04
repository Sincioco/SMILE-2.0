# M7H-F — Validation and acceptance record

Status: focused native milestone validated; full planned M7H-F acceptance is not claimed.
Branch: main. Foundation starting commit: `b645ce9c7e20b889b8bf45a4f58bad1f9931955d`.
Validated implementation ending commit: `e31cb70d358e26b61c9a50cc4c4f2f09f1d5c783`. Pushed and verified.
All previously validated milestones remain in history; no reset/rebase/amend was used.

## Commands and results

| Command | Result |
|---|---|
| `scripts/test-lightning-vfx-foundation.ps1` | Native pass and exact Web console parity; final output retained |
| `scripts/test-renderer3d-vfx-batches.ps1` | Native/Web batch, lifecycle, queue, HDR/direct-LDR and hot-path pass |
| `scripts/generate-lightning-vfx-assets.ps1 -Check` | Original masks and thunder asset reproducibility passed |
| `scripts/test-native-thermal-fire.ps1 -SkipBuild` | Retained thermal reference, native GPU recovery/coexistence, FireEmitter and Web contracts passed during the Lab stage |
| `scripts/test-character-3d-viewer-hardening.ps1 -NativeOnly` | Final post-dragon pass, original asset checks retained, new rig/clip/hash checks, calibration behavior and 58 native graphics/input/audio checks passed |
| `scripts/run-bounded-test.cmd 30 artifacts/tests/OrinStormChargeTests.exe` | Added pure charge checks passed |
| `scripts/format-smile-style.ps1 -Files tools/Character3DViewer/Program.smile -Check -FormatLongIf` | Passed after final viewer edit |
| `tools/Character3DViewer/Build.ps1` | Final native Release pass, 37 assets published |
| `examples/AdvancedLightningVfxLab/Build.ps1` | Native Lab pass |
| `blender --background --python scripts/rig-red-dragon.py` | 15 bounded/grounded pose samples across five clips passed |
| `scripts/sync-arin-v5-7-calibration.ps1 -Mode Export -AllowMissing` | Arin retained 23 keys |
| Same synchronizer with `-Character Orin` | Orin retained zero saved keys; accepted fit remains in source |
| `git diff --check` | Passed before milestone commits |

The normal `scripts/smoke-test.cmd` run initially exposed an SDK probe completion
race, repaired in ca05fa4, then a Menu Gallery named-key mismatch, repaired in
0803a60. After the latter, the original remaining smoke commands were resumed from
the Menu Gallery check using `artifacts/temp/lightning-smoke-continue.cmd`; that
continuation exited zero. The completed coverage includes 295 managed tests,
13 formatter integration tests, retained renderer/Character3D/HDR/MSAA/shadow,
soft-depth/distortion, native/Web GPU-particle/no-readback/model-cooking groups,
game builds and artifact/VSIX payload checks. This is a completed run plus its
continuation, not a claim of one uninterrupted green invocation.

Logs in the bundle preserve the smoke failure/continuation, final lightning
native/Web output, retained batch result, final viewer hardening, dragon builder
and build, and VSIX installation. Existing tests were not weakened; the old static
project-input assertion was replaced by specific animated descriptor/profile,
24-bone/five-clip/four-socket/hash checks while source/geometry tests were retained.

## Evidence and native observations

All screenshot files are genuine PNG encodings of native window captures. Earlier
capture payloads supplied JPEG pixels; they were re-encoded as PNG without altering
the captured content. `evidence-manifest.json` records dimensions, bytes and SHA-256.
`phone-contact-sheet.png` lays out the unmodified captures vertically for phone review.
The Blender dragon render is labeled separately. No generated image substitutes
for a native screenshot, and no video was recorded.

Godstorm HUD: GPU backend 2, 3,158 staged points, 5,586 occupied sparks, 16,384-slot
pool, 2,621,584 GPU spark bytes, zero dropped points, 92 FPS. Storm Lance HUD:
982 points, 302 sparks, 120 FPS. Party captures show roughly 90–120 FPS, with
other native demos open. These are instantaneous visual samples. No isolated
CPU/GPU timing, bandwidth, saturation or endurance benchmark is claimed.

## Installed tooling

VSIX: `D:\SMILE 2.0\artifacts\vsix\Smile.VisualStudio.vsix`, version 2.0.59.
Archive SHA-256: `4F752EE52EA2B94AA2E2A4D9461BC605A2A0160A58E0B830BE7964B9023DBB83`.
Installed DLL SHA-256: `745E10AF13E2A9A65B6E268F30736A15ABD3818D00D21998CCBC3772395D9449`.
Installed/bundled compiler executable SHA-256:
`E9C37B471FE399651385AE8EA4266671BC1CF727F3EF3F2713AB22E77622625E`.
Installed/bundled native runtime SHA-256:
`5C67450DB453161793A16F0B457311BCA0550597EA59ED494BBC8E899BDB947B`.
The separate `artifacts/runtime` build is not the packaged compiler runtime.
Restart Visual Studio to load the refreshed extension. No extra VSIX change was
needed for the source-only Orin/dragon presentation after installation.

## Remaining acceptance limits

- No long charged-aura soak, full-scene VFX-off gameplay hash, exhaustive interruption
  matrix or Orin-specific device-loss audiovisual replay audit was run. The existing
  bounded lifecycle, no-readback, recovery and pure charge contracts passed.
- No formal High/Ultra CPU/GPU benchmark or RTX saturation proof was produced.
- Cloud treatment is presentation lighting/flash, not a volumetric weather system;
  no local ionization distortion or separate expanding shock-ring primitive.
- The Orin tab is the Storm Presentation Lab. Chain Arcs in the one-boss viewer
  do not claim multiple-enemy combat. Damage/authorization remain outside VFX.
- Dragon is a preview rig with preserved original angular geometry and no IK/flight.
- Blender-style Move/Rotate handles and keyboard constraints are implemented in
  the lightweight editor; this is not every Blender transform/snapping feature.
- Web visual work remains deferred. Sin's final visual acceptance is unrecorded.

These limitations follow the latest KISS/velocity and focused validation rules;
they are documented rather than represented as completed production gates.

## Visual review form

Reviewer/date: __________

| Item | Accept / Needs adjustment | Notes |
|---|---|---|
| Orin hammer grip and white/electric aura | | |
| Shield perimeter only; face/back unchanged | | |
| Sky contact and charge/discharge readability | | |
| Storm Lance and Godstorm quality | | |
| Party approach, attack direction and camera distance | | |
| Dragon mouth fire and claw reach | | |
| Flash/shake comfort and window restore | | |

This is a review record, not an approval prompt blocking delivery. Sin authorized
autonomous work. Recommended next step is visual art feedback on these native
previews before adding further effect families or claiming cinematic acceptance.

## Reconciliation and boundaries

Repository: `D:\SMILE 2.0`, branch `main`. The planning SHA was not restored or
rewritten. Exact M7H ZIP names were absent; the available September 5 foundation
and Advanced Lightning Lab/Orin preset packages were read from Downloads under
`artifacts/temp/codex-handoff`. M7H labels here come from Sin's top-level instruction
file; they do not assert that every planned M7H acceptance target was completed.

Existing numeric Renderer3D commands end at 132, image commands at 2 and text
commands at 12. Ribbon command 120, GPU particles 127, soft depth 125, distortion
126, HDR/post 113 and the existing Character3D APIs were reused. No command IDs,
language syntax, backend-specific student API, or ABI layouts were added.
`examples/AdvancedFireVfxLab`, `FireEmitter3D.smile`, `Arena3D`, `StaticBackdrop3D`
and `Smile.UI.Controls` remain the structural reference and shared services.
Renderer3D was extended incrementally; FireEmitter3D was not replaced.

Orin is the real v1.3 model, stable ID `sin-star-i.character-2.tank`, using the final
Mixamo Idle (8) skin and nine supplied Mixamo clips. The Lab's neutral primitive
figure is explicitly a fixture. VFX does not choose targets, apply damage or own
gameplay authorization. The viewer remains a presentation/editor tool.

Unrelated user image edits, older Orin revisions and untracked source experiments
were not committed. Arin's model, accepted animation sources and 23 saved keys were
preserved. Orin's accepted GLB hash remains
`6DD3EC872CAD79FD28AD3B8D5A5228149CBC35C74652A69B6123922D94901936`.

Web visual work is deferred. Shared compilation/ABI regression coverage and the
basic ribbon/CPU fallback remain; no Web Lightning Lab UI or visual parity is claimed.
