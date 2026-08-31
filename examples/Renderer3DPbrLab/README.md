# Renderer3D PBR Lab

This Windows/Web example exercises the M2 PBR-lite path from one SMILE source file. It shows rough and smooth dielectric and metal materials, a normal/ORM-mapped object, emissive output, an automatically material-bound two-part SM3D v2 fixture, a rotating alpha-mask/double-sided plane, and one legacy simple-material object.

Controls:

- `1`–`4`: move the point light over the four material comparisons.
- `A`: toggle the point light.
- `S`: toggle normal-map strength.
- `D`: toggle diagnostics.
- primary drag/wheel/middle drag: pan, zoom, and orbit.
- `Escape`: exit.

Regenerate or verify the repository-owned model and PNG fixtures with `scripts\generate-renderer3d-pbr-fixtures.ps1` and its `-Check` switch.
