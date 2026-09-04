# M7H-A — Lightning path and batch foundation

Status: implemented and validated as a bounded native foundation.
Actual starting commit: `b645ce9c7e20b889b8bf45a4f58bad1f9931955d`.
Initial ending commit: `6ecf88a4858e83be5e03a5b27b133ca30c9d9eef`.
Dense path refinement: `e9b7b19bff702ae887c8ac1e26cfda189052421b`.
Pushed and verified: yes; both are ancestors of the final verified snapshot.

## Path, batch and fallback

`libraries/Smile.Simple3D/LightningVfx3D.smile` generates seeded paths on the CPU,
using stable perpendicular bases and midpoint displacement with decaying offsets.
Endpoints remain exact. Topology changes in 60 ms ticks rather than on every frame.
Tapered eight-segment branches, invisible separators, layered white cores and blue
halos use the existing ribbon renderer. Leader/streamer timing and four return
stroke pulses are presentation states, not physical electrical simulation.

The native and shared Web ribbon bounds are 8,192 points per batch and 32,768 total.
Storage is allocated on resource creation and remains bounded while updating.
There are eight effect handles and at most 1,022 local points per effect. Low uses
8 trunk segments/1 branch, Medium 12/2, High up to 64/16 and Ultra up to 128/24;
admission reduces branch counts when the caller's capacity is smaller.
The basic fallback uses the same ribbons and CPU endpoint sprites.

## Native Ultra evidence

The Lab reserves three 8,192-point batches and one 16,384-slot GPU spark system.
Eight Ultra strikes stage 3,158 path points including separators, rendered in three
layers. This is not a claim of 8,192 distinct visible segments or GPU saturation.
The core draw arrangement is three ribbon submissions plus a GPU spark draw;
scene and post-processing passes add their own draws. Per-frame upload bytes and
isolated GPU timings were not profiled. The captured GPU spark reservation is
2,621,584 bytes. Full process CPU memory is not inferred from this number.

`lightning-lab-godstorm.png` shows backend 2 (GPU), 3,158 path points, 5,586 occupied
spark slots, zero dropped points and a 92 FPS instantaneous HUD sample. This was
captured with other native demo windows running; it is visual evidence, not a benchmark.

## Files and validation

Files include `LightningVfx3D.smile`, the Simple3D project, existing native/Web ribbon
capacity code, deterministic assets under `TechnicalAssets/Generation3/Lightning`,
`examples/LightningVfxFoundationTests`, and the focused foundation test script.

- `scripts/test-lightning-vfx-foundation.ps1`: native pass and exact Web console parity.
- `scripts/test-renderer3d-vfx-batches.ps1`: native/Web batch, queue, lifecycle,
  HDR/direct-LDR and hot-path checks passed.
- `scripts/generate-lightning-vfx-assets.ps1 -Check`: reproducible original textures/audio.

Evidence metadata is in `evidence-manifest.json`. Lab/Orin details are reported in
M7H-C/D/E. VSIX 2.0.59 contains the native capacity changes; installed payload hashes
are listed in M7H-F and `build-artifacts.json`.

Known limitations: CPU path generation; no dedicated GPU bolt generator, volumetric
weather, ionization simulation or full GPU-load target. User visual approval remains
unrecorded. Further visual tuning should follow Sin's review of the native capture.

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
