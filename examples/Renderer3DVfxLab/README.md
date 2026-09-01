# Renderer3D VFX Generation 2 Lab

This native/Web lab demonstrates the M6 fixed-pool VFX path with an articulated `Character3D` actor, batched particles, a dynamic ribbon, alpha and additive blending, atlas animation, camera-facing and vertical billboards, HDR/bloom, and direct-LDR fallback.

Controls:

- `1`–`4`: select and trigger the first four standard effects.
- `Left`/`Right`: select any of the six standard effects.
- `Space`: trigger the selected effect.
- `S`: toggle the 1,024-particle stress burst.
- `A`: toggle HDR/post processing and direct LDR.
- `W`: toggle bloom.
- `D`: toggle diagnostics.
- `Enter`: reset only the VFX system.
- `Esc`: exit.

The atlas is original repository-owned content. Regenerate it deterministically with:

```powershell
.\scripts\generate-renderer3d-vfx-fixtures.ps1
```
