# M7H-E — Orin storm charge and attacks

Status: implemented as a focused native presentation milestone.
Actual starting commit: `e9b7b19bff702ae887c8ac1e26cfda189052421b`.
Ending commit: `aec603808c096f38b840e5f96bdd546c887a53e6`.
Animated dragon chest target refinement: `60fdab504a442170b6e4025f7ac75f08098ab6c4`.
Pushed and verified: yes.

## Sky call, stored charge and release

During ThorAttack the raised hammer receives a descending lightning path and
upward streamer, white core pulses and a contact burst. The live HammerHead socket
keeps the connection aligned after edits. The active sky phase spans 12–64% of the
clip; charge contact is latched at 35%. Release at 64% consumes 350 and drives the
chosen discharge style through 93%. Sky effects restart inside the charging phase
when needed, instead of ending permanently after a single short effect.

Idle and Run retain the remaining white hammer glow, crawling head arcs, sparks,
one local light and the shield's perimeter aura. The face/back of the shield retain
their normal texture. Initial viewer charge is 1000. The next Thor contact refills
it; this is caller-owned preview state, not a game balance implementation.

The style button beside Floor/Grid in Orin's tab selects Thunder Smash, Storm
Lance, Chain Arcs or Godstorm. Thunder Smash uses radial ground arcs. Storm Lance
projects to the animated dragon chest. Chain Arcs are arena presentation paths;
the separate Lab demonstrates caller-ordered multiple targets. The viewer does not
pretend there are several enemies or apply damage. Godstorm layers additional
strikes within the shared eight-effect budget.

## Requests and comfort

One release cue owns synthesized thunder, a bounded flash, hammer light and small
camera offset. Full/reduced/off flash and shake are available, with reduced default.
The scene-pause path suppresses charge and audio event application while supporting
visual frame inspection. Hidden Glow suppresses rendering while CPU charge logic
continues. Equipment visibility is resolved for the actor actually on screen.

The reusable API supplies seeded paths, eight handles, mutable ordered targets,
branch/style/spark budgets and consumable requests. The Orin pool reserves 4,096
GPU sparks and three 2,048-point batches. Draws/uploads/CPU-GPU bytes were not
independently benchmarked for Orin; the Lab HUD evidence is reported in M7H-C.

## Validation, files and limits

Files: OrinStorm.smile, viewer Program/Profiles/project/build preparation, the
socket derivation script, canonical descriptor/profile and package documentation.
Native build and viewer hardening passed. Pure charge checks exercise once-only
contact/release, skipped thresholds, attacks without charging and reset behavior.
Foundation native/Web tests passed after adding mutable chain/jitter/spark controls.
Evidence: `orin-charge.png`, `orin-discharge.png`, plus final Party dragon captures;
dimensions, bytes and hashes are in `evidence-manifest.json`.

This reuses the Orin tab as the Storm Presentation Lab. No separate duplicate
editor, gameplay damage, local ionization distortion, volumetric cloud simulation
or separate expanding shock-ring primitive was added. Ground arcs provide the
smash presentation. These limits mean full planned M7H-E/F cinematic acceptance
is not claimed. User visual approval remains pending; it does not block this
autonomously authorized native preview delivery. VSIX remains installed at 2.0.59.

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
