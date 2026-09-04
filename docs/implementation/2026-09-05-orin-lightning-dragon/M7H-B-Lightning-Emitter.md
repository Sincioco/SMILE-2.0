# M7H-B — Reusable lightning effects

Status: implemented, with bounded handles and presentation request ownership.
Actual starting commit: `b645ce9c7e20b889b8bf45a4f58bad1f9931955d`.
Initial ending commit: `6ecf88a4858e83be5e03a5b27b133ca30c9d9eef`.
Final integration refinements: `aec603808c096f38b840e5f96bdd546c887a53e6`.
Pushed and verified: yes.

## API, lifecycle and ownership

The existing source-level `LightningVfx3D` module is the reconciled emitter API.
It provides Initialize, StartAt, SetEndpoints, AddChainTarget, SetChainTarget,
SetCharge, SetEnabled, SetFlashMode, Configure, SetStyle, SetBranchBudget,
SetPathJitter, SetSparkIntensity, Update, DrawAll, Destroy, Value and Shutdown.
Generation checks reject stale handles. Requests for light, flash, shake and audio
are consumed explicitly by the caller. No effect selects a target or applies damage.
Source/target changes and ordered chain points remain caller-owned.

There are eight handles and up to eight ordered chain targets. Lifecycle includes
anticipation, leader/streamer, return stroke, residual/aura and completion, depending
on the preset. Quiet equipment corona uses reduced spark rates; a zero-jitter,
zero-branch path can follow a calibrated polygon precisely.

Presets: StormGather, SkyStrike, SkyToWeaponCharge, WeaponCorona/StoredStorm,
ThunderLance/ChargedBlast, ChainTempest, ThunderGroundSlam and StormCrown.
Preset names are source-level values, not alternate language keywords. The assets
are two original PNG masks and an original synthesized thunder WAV.

## Rendering and cost

Three layered ribbon batches share bounded path arrays. Native sparks use the
existing GPU thermal-particle controls with a white electric appearance, drag
and gravity; CPU sprites are the basic fallback. The Lab Ultra reservation is
three 8,192-point batches plus a 16,384-slot spark pool; actual eight-strike path
count is 3,158. Per-frame uploads and isolated CPU/GPU timings were not measured.
The caller can request smaller capacities for a full battle scene.

## Validation and evidence

`scripts/test-lightning-vfx-foundation.ps1` passed native and exact Web console
parity. Tests cover handle/lifecycle validation, chain capacity, invalid arguments,
disabled effects, requests and the dense eight-effect path. The latest run is
preserved in `logs/orin-lightning-final.log` and `logs/LightningVfxFoundationTests.out`.
The retained batch and GPU-particle gates passed. The Lab screenshots demonstrate
continuous arcs and simultaneous strikes; metadata is in `evidence-manifest.json`.

The Lab provides ten stations and reduced/full/off flash; Orin uses calibrated
equipment and a separate pure CPU charge controller described in M7H-D/E.
VSIX 2.0.59 was installed and verified for the shared native payload.

Known limitations: this is an artistic VFX controller, not a physical electricity
or gameplay system. There is no full-scene VFX-off gameplay-state-hash acceptance
test in the viewer. User visual approval is not claimed.

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
