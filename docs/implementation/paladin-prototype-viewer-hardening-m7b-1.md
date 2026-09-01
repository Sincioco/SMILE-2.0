# Arin Character Viewer Hardening — M7B.1

## Status and reconciliation

M7B.1 was implemented on `main` from starting commit `393b7cecee513cd06d20032169f03e116130c15c`, which matched `origin/main` with ahead/behind `0 0`. No reset, restore, clean, rebase, amend, or unrelated-work discard was used. The ending commit is the `Sin and Codex: fix(viewer): harden Arin prototype inspection` commit containing this report; its exact SHA is recorded in the final task report and pushed repository history.

M7C automatic project cooking, runtime GLB loading, final combat animation, production art, IBL/subsurface shading, M8 cast work, IK, morphs, retargeting, cloth, WebGPU, and third-party runtime engines were not started.

## Identity and release boundary

- Canonical asset ID: `sin-star-i.character-1.paladin`.
- Official character name: `Arin`.
- Party role: `Paladin`.
- Temporary game alias: `dragonfall.arin-prototype`.
- Source-game identity: Sin Star I Character 1 Paladin.
- Prototype loadability: enabled for technical inspection.
- Production-ready: false.
- Release-enabled: false.
- Dragonfall release visual mode: Classic.

`paladin-prototype-asset.json` now records unknown provenance as null rather than inferring it, separates non-runtime reference images, lists the lossy source texture quality, marks all six sockets as non-authored prototype data, records the fused one-part equipment limitation, and carries the ten-state production clip matrix. Seven required production clips remain missing, so the viewer and adapter cannot mistake an Idle fallback for a completed attack, defend, hit, KO, or victory animation.

## Preparation and source preservation

The accepted source remains byte-for-byte unchanged:

| Artifact | Bytes | SHA-256 |
|---|---:|---|
| Original GLB | 1,479,468 | `0B75E3664FC2743637C9E75E86A55EBDFB8D4A4E3740AC06E593ADE1588013F6` |
| Prepared GLTF | deterministic JSON | `125EF4C92C4FF91E99D989C5EEB7C5EE7EA61D384B6721B5A14104BFDC3B4067` |
| Prepared BIN | deterministic payload | `BC84950418D6949E89E48D586C88C7DC408E4172A3B743BBE66C911A9577E893` |
| Runtime SM3D | 709,884 | `859C96D0F763DE96130DBE82C6A526D2886986FFAD50FCE8D644FA55064A3121` |
| Base-color PNG | 2,183,883 | `CAEAF1BAFF6C7F0B465A75F0808A0DF2EC8998C9562A9341E41209E978B89254` |
| Normal PNG | 1,454,013 | `942A193949BEF7B067B0113553A7788604AA01E3716B963354E1905765FC472E` |
| ORM PNG | 1,755,724 | `DC9AEE72FCCD2512154D44887F8B29749AD17A5FB65D7432703731D6944B65DD` |

Preparation resolves texture semantics through material → texture → image references rather than image-array order. GLB chunk headers, declared buffer length, image buffer views, image signature/MIME, dimensions, pixel count, and positive/negative stride traversal are bounded before publication. ORM preparation changes red only to neutral occlusion white and preserves green roughness, blue metallic, and alpha. The source material has no authored occlusion binding; the deterministic preparation manifest records that truth explicitly.

All seven outputs stage under `artifacts/temp`, publish as one rollback-capable transaction, and restore prior bytes in reverse order after any failure. A synthetic failure after the fourth publication restored every output hash and left no temporary residue. The manifest records source, descriptor, tool, prepared model, and texture hashes. PNG publication is lossless from the decoded pixels but cannot recover normal/ORM information already lost in the source 1K JPEGs.

## Reusable viewer and input

`Smile.Simple3D.CharacterViewer` adds only reusable profile, bounds-derived framing, elapsed fixed-point zoom, and retained pointer-delta helpers. Profiles carry asset identity/path, display name, party role, alias, desired view height, framing margin, animation margin, and 25–200% playback bounds. The Arin profile and a differently sized articulated fixture profile prove that the main viewer can switch assets without source edits or hard-coded camera constants.

Auto-fit derives scale (clamped 1–10,000%), grounded position, target, camera distance, floor, pan limits, shadow width/depth, and shadow near/far from validated model bounds. Zoom retains fractional elapsed work and produces the same result for 30/60/120/240-style frame partitions and alternating deltas. Native pointer state now preserves sub-120 Windows wheel units, press and release in one pump, capture loss, focus loss, deltas, and held state. Web wheel input is already normalized and reports a zero raw remainder.

`KEY_O` is the smallest cross-target input addition required for the requested control. Its value is 27 in the shared language, native input mapping/held query, and Web `KeyO` mapping. `O` toggles a 30-degrees-per-second elapsed-time auto-orbit; manual orbit and Reset stop it. The viewer never takes ownership of the operating-system pointer.

The recovery overlay preserves the first viewer and renderer errors, keeps the loop responsive, and lets Enter or Reset perform ordered Character3D, Scene3D, Renderer3D, and profile reconstruction.

## Animation, sockets, and materials

Arin defaults to authored 100% speed and exposes exact Idle, Walk, and Run clip names. The Dragonfall adapter now records the requested production clip, actual clip or Classic rigid visual, whether technical/prototype fallback occurred, and whether the current character state played a production clip without fallback.

The socket overlay draws bounded origin and normalized RGB local-axis endpoints for Root, Head, Chest, SwordBase, SwordTip, and ShieldCenter. Their metadata remains `descriptor-alias` or `inferred-from-bone`, never `production-authored`. Sword and shield geometry is fused into the single skinned runtime part and cannot be swapped independently.

Renderer3D numeric command 122 adds a development material-inspection mode with values:

| Value | Output |
|---:|---|
| 0 | Lit PBR |
| 1 | Base color |
| 2 | Normal |
| 3 | Roughness |
| 4 | Metallic |
| 5 | Occlusion |
| 6 | Emissive |

The public SMILE facade is `SetMaterialInspection3D` / `MaterialInspection3D`. Setting is rejected during an active 3D frame; querying remains available. Native dispatch is in `graphics3d_directx.cpp`, Web dispatch/uniform output is in `WebOutputWriter.cs`, the shared command constant is in `graphics3d.h`, and the beginner-facing wrapper is in `Graphics3D.smile`.

Current command ranges are numeric 1–122, image 1–2, and text 1–9. The next safe append-only IDs are numeric 123, image 3, and text 10. No prior command was renumbered or changed.

## Evidence and mobile review

The historical `docs/implementation/screenshots/m7b-arin-prototype/character-3d-viewer-web.png` path previously contained JPEG/JFIF bytes. Its prior Git blob remains in history as `ed038b5c26a2d24169616938764862ff9d48b2a9`; the current path is re-encoded as true PNG.

Ten required source screenshots, a 1170-pixel-wide phone contact sheet, and exact format/dimension/size/hash metadata live under [m7b-1-paladin-viewer](screenshots/m7b-1-paladin-viewer/screenshot-index.md). The validator checks magic bytes, extension agreement, complete chunk boundaries/IEND, decoded PNG format, RGB/RGBA color type, bounded dimensions/pixels/filesize, reparse points, Git LFS pointers, required paths, contact-sheet size, and index hashes. Its fixtures prove rejection of JPEG-as-PNG, truncation, oversized IHDR, and wrong extension.

The Web frames visibly retain the source prototype's JPEG-derived normal/ORM artifacts; this is recorded rather than cosmetically hidden. Native front/side/back frames show the intact authored silhouette. This difference remains a production-quality blocker and a useful material/shader diagnostic for M7C, not permission to promote the prototype.

## Performance and resource observations

- Accepted model: 1 runtime part, 1 material, 3 texture references, 6,631 vertices, 9,974 triangles, 41 bones, 73 nodes, 3 clips, 4 events, and 6 sockets.
- Animation payload: 269,852 bytes; static payload: 440,032 bytes; total SM3D: 709,884 bytes.
- Normal frame: 2 draws and 9,976 submitted triangles (floor plus character).
- Socket inspection frame: 26 draws and 10,264 submitted triangles.
- Technical auto-fit fixture: 3 draws and 38 submitted triangles.
- Focused teardown: zero live Character3D actors, cached assets, Renderer3D models, animators, objects, meshes, materials, and textures.
- Native and Web manual observation: responsive 960×540 / 1280×720 capture, stable elapsed auto-orbit, bounded pan/orbit, elapsed zoom, and clean continued rendering during profile/clip/material/socket changes.

## Plan deviations and readiness

- The handoff referred generically to the Paladin identity; the user explicitly established `Arin` as the official name and `Paladin` as the separate party role, so all viewer/profile/metadata/report presentation follows that decision.
- The source GLB does not bind an occlusion texture. M7B.1 uses the metallic-roughness image semantically, publishes neutral white red, and records the absent binding rather than inventing authored occlusion.
- Public material inspection required one append-only numeric command because both native and Web needed the same deterministic shader output. No runtime GLB or M7C cooking command was introduced.
- The existing noncontiguous values of `KEY_1`, `KEY_2`, and `KEY_3` made arithmetic clip selection incorrect for `3`; the viewer now maps those named keys explicitly.

M7C is technically unblocked only after this M7B.1 commit, full retained gates, VSIX installation/hash verification, push, and remote evidence verification are green. The prototype itself is not production-unblocked: rights/provenance, visual approval, lossless production textures, deformation acceptance, seven combat clips, authored equipment sockets, native/Web visual acceptance, mobile approval, and explicit release approval remain outstanding.
