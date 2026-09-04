# Renderer3D M7E-C GPU Particle Common Tests

This focused project validates the target-neutral persistent particle resource, fixed-slot spawn contract, deterministic CPU reference scheduler, ping-pong generations, bounded lifetime behavior, in-flight destruction protection, reset behavior, and legacy `ParticleBatch3D` compatibility.

The common test explicitly requests the deterministic CPU reference simulation on native and Web, preserving exact cross-target parity. M7E-D adds a native-only project that verifies D3D11 compute simulation, direct structured-buffer rendering, 1K/4K/16K capacities, soft-depth and distortion coexistence, shader/buffer fallback, and zero GPU readback without changing the public SMILE contract. M7E-E adds Web-only projects that verify WebGL2 transform-feedback simulation, direct instanced rendering, 1K/4K/8K capacities, exact varying order, context-loss restart accounting, capability fallback, and zero readback.

Run `scripts\test-renderer3d-gpu-particle-common.ps1`, `scripts\test-renderer3d-gpu-particle-d3d11.ps1`, and `scripts\test-renderer3d-gpu-particle-webgl2.ps1` from the repository root.
