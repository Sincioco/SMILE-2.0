# Renderer3D M7E-C GPU Particle Common Tests

This focused project validates the target-neutral persistent particle resource, fixed-slot spawn contract, deterministic CPU reference scheduler, ping-pong generations, bounded lifetime behavior, in-flight destruction protection, reset behavior, and legacy `ParticleBatch3D` compatibility.

M7E-C deliberately reports the particle backend as unavailable and uses the deterministic CPU reference simulation on native and Web. Later milestones replace that simulation backend without changing this public SMILE contract.

Run `scripts\test-renderer3d-gpu-particle-common.ps1` from the repository root.
