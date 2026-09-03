# Renderer3D Soft Depth — M7E-A Report

Milestone/subphase: M7E-A

Status: implemented and validated on 2026-09-04 (Asia/Taipei), pending the dedicated commit containing this report.

Branch: `main`

Actual starting commit: `7f8e5ec518182cc28ec7587da055277c2dcfd4f3`

Ending commit: the dedicated `Sin and Codex: feat(renderer3d): add soft-depth VFX` commit containing this report.

## Reconciliation

- M7E-0 remains the frozen preflight baseline.
- The pushed Arin v5.7 and Dragon arena checkpoints are untouched.
- The unrelated untracked Character Viewer handoff file was not read, edited, staged, or deleted.
- Existing `ParticleBatch3D`, `RibbonBatch3D`, CPU-deterministic `Effects3D`, M5 post/shadow behavior, Renderer2D, and every pre-existing ABI command remain available.

## ABI and API

- Numeric commands: 1-125; new append-only command 125 is soft depth. Next numeric ID is 126.
- Image commands: 1-2; next image ID is 3.
- Text commands: 1-12; next text ID is 13.
- `Graphics3D.ConfigureSoftDepth3D` controls the global requested mode outside a frame.
- `Graphics3D.SetEffectMaterialSoftDepth3D` selects Off, Automatic, or Explicit per effect material.
- `Graphics3D.SoftDepthValue3D` exposes target, pass, fallback, generation, and material diagnostics.
- Existing programs default to soft depth Off. `Effects3D.Initialize` explicitly opts in and assigns 24-unit alpha, 8-unit additive, and 12-unit ribbon behavior.

## Implementation

Native Direct3D 11 uses a typeless D24 scene depth texture, compatible DSV/SRV views, an R32F single-sample target, and separate 1x and `Texture2DMS` fullscreen linearization shaders. MSAA depth uses the minimum visible sample. The copy pass unbinds the source resource before restoring depth testing. HDR, MSAA direct LDR, and 1x direct LDR retain their established presentation paths.

WebGL2 uses a `DEPTH_COMPONENT24` scene attachment and a framebuffer-tested R32F target with RGBA8 packing fallback. Its linearization uses WebGL clip-space conventions. The direct-LDR scene is copied before Renderer2D composition, keeping the HUD post-exempt.

Both targets render opaque and alpha-mask submissions first, snapshot linear depth, then draw transparent VFX. Soft particles clamp negative separation to zero and multiply only alpha, preventing bright intersection fringes.

## Capabilities and fallback

- Requested mode is explicit and defaults Off for compatibility.
- Native effective mode is Float32 when resources and shaders succeed.
- Web effective mode prefers Float32 and can fall back to packed RGBA8.
- Shader and target failures are separately reported.
- Forced first-generation failure produces ordinary hard-edged VFX rather than failing the frame.
- A compatible previous valid target generation is preserved if a replacement fails.

## Measurements

The focused 320x240 native test allocates 307,200 bytes for its R32F linear-depth target. The Web test harness uses a 1920x1440 backing store and reports 11,059,200 bytes. Exactly one depth-copy draw and one softened VFX draw occur in each enabled test frame; disabled frames report zero for both. The forced failure path reports zero target bytes, zero depth-copy draws, one copy/resource failure, zero softened VFX draws, and target fallback reason 3.

At 1920x1080 an R32F target accounts for 8,294,400 bytes (7.91 MiB). The allocation joins M5 target accounting and is recreated only when dimensions, quality, or the configuration revision changes.

## Validation

- `cmd /c scripts\build.cmd`: pass.
- `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts\test-renderer3d-soft-particles.ps1`: pass for native/Web, HDR+MSAA, direct-LDR+1x, material diagnostics, forced fallback, JavaScript syntax, and exact console parity.
- `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts\test-renderer3d-post-processing.ps1`: pass for retained M5 behavior and native/Web fallback paths.
- `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts\test-renderer3d-post-processing-hardening.ps1`: pass.
- `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts\test-renderer3d-vfx-batches.ps1`: pass.
- `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts\test-renderer3d-vfx-hardening.ps1`: pass.
- `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts\test-smile-formatter.ps1`: all 13 focused formatter tests pass.
- Repository-wide formatting check is rerun after the formatter normalized the new API section.

## Deferred by phase boundary

M7E-A intentionally adds no distortion, GPU particle simulation, turbulence texture, thermal fire shader, Generation 3 emitter, AetherBlade production preset, Dragon fire breath, or Arin flaming-sword integration. Those remain the separately committed M7E-B through M7E-I and final Character Viewer work.
