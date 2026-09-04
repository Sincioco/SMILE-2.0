# M7H-C — Advanced Lightning Lab

Status: implemented and visually inspected on native Windows.
Actual starting commit: `0803a6035e5352b5dda0a362072063ab9c64e9a3`.
Ending commit: `e9b7b19bff702ae887c8ac1e26cfda189052421b`.
Pushed and verified: yes.

## Stations and controls

The native project is `examples/AdvancedLightningVfxLab`; build with its `Build.ps1`.
It opens Godstorm Ultra with Demo on. Nine stations cycle every ten seconds;
manual station selection stops Demo. The tenth is a Low/Selected quality comparison.

Stations: Sky Strike, Forked Judgment, Weapon Charge, Charged Weapon, Thunder Smash,
Chain Lightning, Storm Lance, Arc Storm, Godstorm Ultra and Low/Selected.
Storm Lance continuously connects the fixture's hammer to its target with branched
white-core blue arcs and GPU sparks. Godstorm runs eight simultaneous strikes.

Tab changes station; Space pauses; R restarts; 4 changes quality; G changes the
particle backend request; B changes backdrop; backtick cycles UI visibility.
Left drag pans, middle drag orbits, wheel eases zoom and right click resets.
Buttons expose HDR/bloom, branches, orbit, floor/grid, soft depth, flash mode and
backend. Full/reduced/off flash and shake are supported; reduced is the default.

Window X/Y, width/height and maximized state use the existing runtime's
`RememberWindowPlacement=true` with stable application ID
`smile.examples.advanced-lightning-vfx-lab`. The window was closed/relaunched during
development and restored. No language extension was necessary.

## Diagnostics and observations

The HUD shows FPS, effective backend, staged points, occupied spark slots, GPU pool,
GPU spark bytes, dropped points and charge. Backend 2 confirms GPU sparks. Ultra
reserves three 8,192-point batches and 16,384 sparks; actual eight-strike points are
3,158. `lightning-lab-godstorm.png` records 5,586 occupied sparks, zero dropped points,
2,621,584 GPU spark bytes and 92 FPS. `lightning-lab-storm-lance.png` records 982 points,
302 sparks and 120 FPS. These are instantaneous observations while other demos ran.
They do not prove saturation or isolated frame-time performance.

No per-frame upload/CPU memory benchmark was recorded. Rendering uses three ribbon
layers and a GPU spark system plus scene/post passes. CPU/basic fallback remains
truthful. The primitive figure is a fixture; the real Orin stays in the viewer.

## Validation and limitations

Native `Build.ps1`, foundation native/Web parity and retained ribbon/HDR/GPU checks
passed. Camera pan/orbit/zoom/reset and documented controls were inspected during
development. Screenshots are listed with dimensions/bytes/SHA-256 in
`evidence-manifest.json`; the vertical contact sheet is for phone review.

Files: Program.smile, project, Build.ps1 and README under the Lab folder; shared
LightningVfx3D and original technical assets. The VSIX was rebuilt/installed at
2.0.59 for the shared native changes. Orin integration is reported separately.

Known limitations: no formal High/Ultra benchmark table, dedicated volumetric
clouds, lightsaber system or GPU saturation proof. No Web visual acceptance.
Sin's final visual approval has not been recorded.

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
