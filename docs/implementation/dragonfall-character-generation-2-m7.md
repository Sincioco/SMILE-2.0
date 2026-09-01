# Dragonfall Character Generation 2 — M7 Asset Gate

> Supersession note (2026-09-01): this report remains the historical record of the original missing-asset gate. Sin subsequently supplied and explicitly approved an optimized Tripo GLB as the early Arin prototype. `dragonfall-visual-adapter-m7a.md` records the adapter seam and `dragonfall-arin-prototype-m7b.md` records the loadable prototype and Character 3D Viewer. The prototype does not retroactively satisfy the final-production provenance, combat-clip, deformation-bone-target, or authored-socket gates described below.

Milestone: M7 Dragonfall one-character vertical slice

Status: **Blocked—missing** at the production-character asset gate on 2026-09-01 (Asia/Taipei).

Branch: `main`

Starting local commit: `b7a74e5b08f3f58a62ceb575307dd1810e63f5a1`

Starting `origin/main`: `b7a74e5b08f3f58a62ceb575307dd1810e63f5a1`

Ending commit: the documentation-only blocker commit containing this report; its exact SHA is recorded in the final delivery report because a commit cannot contain its own SHA.

Pushed and verified: this documentation-only blocker commit is pushed and remote-verified as part of delivery; its exact SHA is recorded in the final report. This status does not represent M7 completion.

## M6.1 prerequisite

M6.1 is green, committed separately, pushed, and remote-verified at `b7a74e5b08f3f58a62ceb575307dd1810e63f5a1`. Its focused gate passed again after the push:

```text
Renderer3D M6 native/Web batch, queue, lifecycle, 1,024-instance, HDR/direct-LDR, and hot-path tests passed in 978 ms native runtime.
Effects3D deterministic seed, partition, quality, exhaustion, stop, reset, and native/Web parity tests passed.
Renderer3D M6.1 native/Web revision isolation, transactional lifecycle, determinism, request capacity, socket invalidation, restoration, and hot-path tests passed.
```

The complete M7 instruction package was then read in manifest order. The downloaded ZIP SHA-256 is `38B288E3C5670A8C6FC75CB8FDA95B26F4674C2D4E85FEB67067D7EE35CDCDF2`, and all ten numbered Markdown files matched the package manifest.

## Asset gate

- source search: `D:\SMILE 2.0`, `C:\Users\louie\Downloads`, every nested loose file, and the entry inventory of all 20 downloaded ZIPs;
- alternate Downloads search: `C:\Users\louie\OneDrive\Downloads` does not exist, and Windows reports no other Downloads location;
- required GLB: missing;
- required SM3D descriptor: missing from any production Arin package;
- required PBR PNG texture set: missing from any production Arin package;
- required provenance/license Markdown: missing;
- required preview paired with the licensed model: missing;
- provenance: unavailable;
- license: unavailable;
- result: **Blocked—missing**.

The tracked reference at `games\SinStarI\Assets\Characters\Sin Star - Character 1 - Paladin - T-Pose.png` is a 1,086 x 1,448 PNG, 1,875,428 bytes, SHA-256 `906C5FF8457B4E476021A60DF2CE3BD33D07B63B4A6E8C891D802054EB0AA4B9`. It is not a mesh, skeleton, animation set, descriptor, texture package, or rights record and was not treated as production input.

A separate loose downloaded image, `RAID - Fusion - The Cowardly Lion.png`, is likewise only a PNG with no matching GLB, descriptor, texture package, or approved provenance. It was not copied, converted, or used.

## Asset statistics

Not available because no candidate production asset exists:

- SM3D bytes: not applicable;
- triangles/vertices/parts: not applicable;
- materials/textures: not applicable;
- bones/clips/events/sockets: not applicable;
- animation resident bytes: not applicable;
- GPU texture estimate: not applicable.

The tiny repository-owned converter fixtures were explicitly rejected as final art. The existing 56-part rigid Arin was not subdivided or relabeled as a skinned production character.

## Dragonfall adapter

- Classic path: unchanged and still validated by the existing Dragonfall gate;
- Generation 2 path: not integrated because production input did not pass the mandatory gate;
- fallback: unchanged; no speculative Dragonfall-local adapter was committed.

No Dragonfall source, battle state, mechanics, scene, asset declaration, animation mapping, VFX/audio mapping, lighting, camera, or fallback behavior changed.

## Animation and VFX mapping

Not implemented. There is no approved skeleton, no required clip set, no event timeline, and no exact socket mapping to validate. M6.1 remains available and green, but its effects were not attached to fabricated actor data.

## Lighting and camera

Not changed. The existing Dragonfall scene, rigid actors, cameras, Generation 1 effects, lighting, and Renderer2D HUD remain authoritative.

## Mechanics parity

The existing deterministic Dragonfall gate passed after M6.1 was pushed:

```text
Dragonfall native/Web mechanics, lifecycle, demo, and no-demo validation passed.
```

No Generation 2 comparison profile exists, so Classic-versus-Generation-2 mechanics parity is not claimable and was not fabricated.

## Command ABI

No command was added or changed:

- numeric Renderer3D range: 1-121; next ID 122;
- image Renderer3D range: 1-2; next ID 3;
- text Renderer3D range: 1-9; next ID 10.

M7 added no language syntax and made no SMILE 1.0 change.

## Tests and exact results

| Gate | Exact result |
| --- | --- |
| M6.1 prerequisite focused gate | PASS; native/Web revision isolation, transactional lifecycle, determinism, request capacity, socket invalidation, restoration, and hot-path checks; nested M6 native runtime 978 ms |
| Dragonfall baseline gate | PASS; native/Web mechanics, lifecycle, demo, no-demo, balance, both startup programs, and asset publication |
| M7 production asset gate | **Blocked—missing**; no candidate package contained the required production inputs |
| M7 converter/Character Lab | Not run; no approved GLB/descriptor/textures/provenance input |
| M7 adapter/mechanics parity/lifecycle | Not run; production integration is prohibited after the failed asset gate |
| M7 native/Web visual acceptance | Not run; there is no Generation 2 Arin to review |

The full retained suite and smoke had already passed for the immediately preceding M6.1 commit. No source or payload changed after that validation; this M7 outcome adds documentation only.

## Native and Web manual checks

M6.1's current native Direct3D 11 and WebGL2 VFX Lab were manually reviewed before its commit. M7 manual review is not applicable because generating or presenting a fake production Arin would violate the gate. Existing Dragonfall native and Web builds were regenerated successfully by the baseline gate.

## Before/after resources and performance

There is no M7 after-state. Objects, meshes, materials, textures, animators, draws, triangles, target bytes, and lifecycle counts remain exactly on the existing Classic Dragonfall path. No unobserved FPS or performance improvement is asserted.

## Mobile-review evidence

- previously committed screenshots: M6.1 VFX evidence under `docs/implementation/evidence/m6-1-vfx/`;
- new M7 committed screenshots: none;
- artifact-only M7 screenshots: none;
- M7 contact sheet: none;
- M7 notes: this asset-gate report;
- M7 screenshot hashes: not applicable.

The reference T-pose is not presented as M7 output. The required Classic-before, Generation-2 idle/attack/impact/shadow/Web/fallback set cannot truthfully be captured until an approved character is integrated.

## VSIX

No M7 change affected the extension, compiler, templates, language services, or VSIX payload. M6.1's already installed and verified VSIX remains version 2.0.56; no redundant M7 rebuild or reinstall was required.

## Plan deviation and blocker

M7 stopped at Phase 1 exactly as required by the handoff. Phases 2-7 were not started. No integration-readiness code was necessary or justified without a real asset, so the repository retains no speculative adapter seam.

To unblock M7, supply one approved original or properly licensed package containing:

```text
Arin.glb
Arin.sm3d.json
PNG base-color, normal, ORM, and any emissive textures
arin-provenance.md
preview image or video
```

The provenance must authorize repository/source and compiled-game distribution. The asset must then pass the 10,000-15,000 triangle target, 55-80 deformation-bone target (128 maximum), four-influence limit, required clips/events/sockets, PBR scale-safety, deterministic conversion, Character Lab, native/Web, memory, and deformation gates.

## M8 readiness

M8 is not authorized and was not started. M7 remains blocked until an approved production asset is supplied, integrated, validated, screenshotted, committed, pushed, and explicitly reviewed by the user.

## Command ledger

```powershell
Get-Content <M7 package files in manifest order>
Get-FileHash -Algorithm SHA256 <M7 ZIP and package files>
rg --files <repository production-asset patterns>
Get-ChildItem C:\Users\louie\Downloads -Recurse -File
[System.IO.Compression.ZipFile]::OpenRead(<each of 20 downloaded ZIPs>)
Test-Path C:\Users\louie\OneDrive\Downloads
& .\scripts\test-renderer3d-vfx-hardening.ps1
& .\scripts\test-dragonfall.ps1
git status -sb
```
