# VFX Generation 3 Preflight and Fallback Hardening — M7E-0

Status: implemented and validated on 2026-09-04 (Asia/Taipei), pending the dedicated M7E-0 commit containing this report.

Branch: `main`

Actual starting commit: `0ba5746c63472170274dd399f5a12fb66fe6cf64`

Ending commit: the dedicated `Sin and Codex: refactor(vfx): prepare Generation 3 fallbacks` commit containing this report.

## Isolation and scope

The committed Arin v5.7 checkpoint (`6fceefd09f7a6c7c0bc41734f33fb8adc8dc548b`) and Dragon arena checkpoint (`0ba5746c63472170274dd399f5a12fb66fe6cf64`) were green and already pushed before M7E-0 began. M7E-0 changes no Character Viewer source, production model, texture, animation, equipment profile, or Dragon asset. Its only model is a deterministic technical fixture generated from the existing articulated animation test source.

The supplied M7E-0 and advanced M7E packages were copied to a repository-local temporary handoff directory, path-validated, hash-verified against their manifests, and read completely in their numbered order. No package file was treated as a repository patch.

## Result

M7E-0 freezes the capability and render-phase contracts needed by the advanced implementation without adding a renderer ABI command or changing current output:

- `Effects3D` accepts `CPU_DETERMINISTIC`, `GPU_FAST`, and `AUTO` source policies;
- unsupported GPU simulation, soft depth, distortion, and thermal shading resolve to explicit M7E-0 effective modes and fallback flags;
- renderer reset clears requested/effective capability state to the documented default;
- `AetherBlade3D` provides a reusable original socket-driven blade with outer halo, inner glow, bright core, and safe unconnected afterimages;
- trail history clears on serial/timing discontinuity, enable/disable, missing sockets, degenerate sockets, and renderer reset;
- native DirectX and WebGL2 use the existing GPU-instanced particle/ribbon rendering while simulation remains CPU deterministic;
- the visible lab reports requested/effective policy rather than inferring capability from a GPU name.

## Compatibility and ABI

No numeric, image, or text command was added. Numeric commands remain 1-124, image commands 1-2, and text commands 1-12. Particle batch 119, ribbon batch 120, M6 diagnostics 121, material inspection 122, camera up 123, and animator-time control 124 retain their exact meanings.

Generation 3 disabled retains the existing frame behavior. The advanced phase order and transactional insertion points are frozen in `docs/architecture/renderer3d-vfx-generation-3-preflight.md`.

## Measurements and bounds

The audited inherited baseline is unchanged:

| Resource | Bound/accounting |
|---|---|
| particle batches | 16; 1-4,096 each; 8,192 aggregate |
| ribbon batches | 16; 1-1,024 points each; 2,048 aggregate |
| particle capacity bytes | 96 CPU and 48 GPU per slot |
| ribbon capacity bytes | 188 CPU and 72 GPU per point |
| global VFX reservation ceiling | 1,171,456 CPU bytes; 540,748 GPU bytes |
| Effects3D quality capacity | Low 256; Medium 1,024; High 2,048 particles |
| AetherBlade particles | 88 slots: 72 layer samples and 16 unconnected trail samples |
| AetherBlade ribbons | three two-point strips: halo, glow, and core |
| trail simulation | fixed 12 ms; maximum four catch-up samples per update |

The focused executable confirms 72 committed blade-layer samples at idle, four trail samples after a 48-millisecond swing update, zero trail samples after a serial reset, resource invalidation after renderer reset, and zero live Renderer3D resources after teardown.

## Failure behavior

- invalid simulation values fail without changing the last valid policy;
- a policy change during an active frame is rejected;
- `GPU_FAST` and `AUTO` fall back to CPU deterministically in M7E-0;
- missing or coincident sockets refuse attachment without creating an active blade;
- renderer-epoch changes make stale blade resources unusable;
- disabling the blade commits empty particle/ribbon prefixes and clears trail history;
- destroy and shutdown are repeatable and leave no live model/object/VFX resource.

## Validation

Focused validation completed:

- deterministic fixture generation and `-Check` hash parity;
- Smile.Simple3D library compilation;
- native DirectX test compilation and exact output: `AetherBlade M7E-0 tests passed`;
- Web compilation, JavaScript syntax validation, and exact console parity;
- native and Web visible-lab compilation;
- native/Web screenshots plus portrait/landscape iPhone contact sheet;
- inherited M6 Effects3D and renderer VFX gates, formatter gates, full smoke, and artifact verification are recorded in the commit validation after their final run.

Evidence: `docs/implementation/screenshots/m7e-0-vfx3-preflight/screenshot-index.md`.

## Deferred by design

M7E-0 implements no sampleable depth, soft-particle fade, distortion pass, compute shader, transform feedback, deterministic noise texture, or thermal fire shader. Those are the separately committed M7E-A through M7E-I phases. Production Paladin/Arin integration is also deferred until the fire preset is implemented, at which point the Character Viewer `GLOW` control will own the default-on flaming-sword effect.
