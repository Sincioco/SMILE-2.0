# M7H-D — Real Orin binding

Status: implemented against the accepted Orin v1.3 package.
Actual starting commit: `e9b7b19bff702ae887c8ac1e26cfda189052421b`.
Ending commit: `aec603808c096f38b840e5f96bdd546c887a53e6`.
Pushed and verified: yes.

## Character, clips and sockets

Canonical package: `games/SinStarI/SourceAssets/Characters/Tank/OrinV13`.
The real character uses Idle, SwordAttack, JumpAttack, ThorAttack, Defend, Hit,
Death, Victory and Run from the consistent supplied Mixamo exports. `ThorAttack`
is the stable clip name for the hammer raise and ground release.

Eleven descriptor sockets were added to the existing ten: ShieldRim0–7,
HammerHead, HammerLeft and HammerRight. The derivation script reads the
accepted rigid hand weights/inverse binds and shield hull; it does not edit the
body mesh, UVs, texture, skin or accepted prop fit. The runtime queries each actual
equipment part after wrist/prop calibration. A zero-jitter closed path follows the
shield perimeter; no whole-face or back glow overlay is drawn.

`scripts/update-orin-lightning-sockets.py` is integrated into the Blender builder.
The descriptor/profile fingerprint migration retained Orin's existing zero saved
keys and Arin's 23 keys. Tabs retain independent playback, timeline, editor targets,
storage keys and per-clip correction tracks. The supplied Blender hammer/shield fit
was preserved; the hammer grip remains at its butt and shield fit includes the
accepted outward rotation and user corrections.

## Charge and presentation ownership

`tools/Character3DViewer/OrinStorm.smile` owns the pure CPU ChargeState and the VFX
bindings. Charge is 0–1000. Each action has contact/release latches. Contact at 35%
fills to 1000; release at 64% consumes 350 exactly once. The controller can process
a skipped interval crossing both thresholds. Paused inspection does not emit
charge/audio events. Glow visibility does not gate the charge calculation.

LightningVfx3D remains reusable: eight handles, ordered targets and consumed
presentation requests; three bounded ribbon layers and GPU sparks with basic
fallback. Orin reserves a smaller 4,096-slot spark pool and 2,048 ribbon-point
batches so Arin fire and dragon fire coexist. No per-frame upload/timing benchmark
was taken for this three-actor scene.

## Validation and evidence

The native viewer compiled, final foundation tests passed native/Web parity, and
the hardening executable passed its added pure charge checks. Both calibration
exports were verified. `orin-charge.png` and `orin-discharge.png` show the actual
calibrated actor, shield edge treatment, charge contact and discharge. See the
evidence manifest for dimensions, bytes and hashes. The Orin tab is the reconciled
Storm Presentation Lab; no second duplicate character editor was added.

VSIX 2.0.59 was already installed for the shared native changes; these bindings
add SMILE source and descriptor data, without a new native ABI.
Known limits: accepted armpit openings remain, and this is a presentation model,
not new gameplay damage. Final user visual approval is not claimed.

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
