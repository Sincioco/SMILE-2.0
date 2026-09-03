# Renderer3D M7E-B Implementation Report

## Result

Native Direct3D 11 and WebGL 2 now implement the same opt-in heat-distortion intent. Distortion emitters write flow vectors, a fullscreen pass warps only the rendered 3D scene, and ordinary transparent VFX continue afterward.

## Public surface

- Renderer ABI command `126` (`SMILE_3D_DISTORTION`).
- `Graphics3D.ConfigureDistortion3D` selects Off/Automatic and Low/Medium/High quality.
- `Graphics3D.SetEffectMaterialDistortion3D` configures effect strength, noise, and flow.
- `Graphics3D.DistortionValue3D` exposes configuration, format, allocation, submission, and fallback diagnostics.
- `Effects3D` requests a quality-matched distortion capability during initialization and reports the effective state after Renderer3D prepares resources.

## Backend details

- Direct3D 11 allocates a half- or quarter-resolution `RGBA16_FLOAT` vector target and a full-resolution scene-format scratch target.
- WebGL 2 prefers `RGBA16F`; when float color targets are unavailable it uses centered signed values in `RGBA8`.
- Both backends clamp composite displacement and edge coordinates.
- A frame with no distortion emitters skips the fullscreen composite.
- Low quality is a deliberate Off result, not a transaction failure.
- Device/context reset clears backend resources and recreates them from the requested configuration.

## Validation

`scripts/test-renderer3d-distortion.ps1` proves:

- ABI parity and backend implementation markers;
- native and Web compilation and exact console parity;
- High half-resolution and Medium quarter-resolution targets;
- HDR and direct-LDR operation;
- material configuration and invalid-flow rejection;
- one vector draw and one composite for one emitter;
- zero-emitter composite elision;
- Low-quality and forced-allocation fallbacks;
- bounded edge sampling and Renderer2D composition after Renderer3D.

The focused gate is included in `scripts/smoke-test.cmd` immediately after the M7E-A soft-depth gate.

## Deferred

GPU-resident particle resources and simulation remain M7E-C through M7E-E. Thermal fire shading and production fire presets remain M7E-F.
