# Neris Tripo Castle V1

Created by: Louiery R. Sincioco (Sin).

## Ownership and source

`Original/Neris-Castle-Tripo.glb` is the untouched user-supplied export from
`Neris Castle - 4K - 2M - PBR - No Light.glb`. Its SHA-256 is recorded in
`cleanup.json`. `Blend/Neris-Castle-Cleaned.blend` owns the editable cleaned
architecture in the **Neris Tripo Castle Architecture** collection. The Waterfront
town links that collection by a relative path; keep both packages together.
The original castle in town remains available for comparison.

## Cleanup and placement

The cleanup removes imported blue water, low underside faces and disconnected
low fragments, preserving 1,770,324 of 1,934,613 source triangles. No decimation
was applied. The original 4K PBR atlases and UV coordinates are retained. This
is still the supplied generated mesh, including its irregular ornamental detail;
cleanup does not reconstruct or retopologize the architecture.

The castle is placed at Blender X=-366, Y=226 with uniform scale 170, **twice the
initial comparison scale of 85**. The entrance deck at source Z=0.19 is aligned
to the 0.212-m paving surface. Uneven lower foundation geometry is buried in the
island, rather than suspending the building by its lowest isolated vertex.
The viewer equivalent is scale 170000%, position (-3660,-299.88,2260).

The town owns the blue moat, banks, supported entrance crossing and connecting
street. `NerisTownAppearance` owns its animated native water and dedicated
spotlight. The castle package contains neither a baked light nor imported water.

## Runtime and regeneration

`Runtime` contains fourteen GLB partitions / twenty-eight parts. Partitioning
preserves every cleaned triangle; each part stays within the existing vertex and
triangle bounds. Shared external albedo, normal and metallic/roughness atlases avoid
embedding another copy into every partition. Five unusable exported tangent
vectors at collapsed UV corners receive a stable orthogonal basis; valid tangents,
geometry and UVs stay intact. `Runtime/manifest.json` records their
source, output hashes and placement. The native build copies these cooking inputs
and publishes SM3D assets and textures through the standard asset pipeline.

1. Run Blender in background with `Source/clean_castle.py` to regenerate cleaned
   Blender/GLB outputs from Original. Preserve hand edits as a new revision first.
2. Run `Source/split_castle.py` with the installed Python/numpy to partition that GLB.
3. Run `Source/resize-runtime-textures.ps1` to derive the three shared 2K runtime
   maps. The cleaned GLB/Blender source retains the original 4K maps and UVs.
4. Follow the adjacent town README's Waterfront build/export/minimap sequence,
   then build and launch the native Viewer.

Static asset cooking shares identical converted PBR pixels across the partitions,
including normal and ORM maps, preserving character fingerprints. A local native
run measured about 0.24 seconds for Old Castle geometry/material setup. This is
not a full startup or cold-cache measurement. Lower resolution reduces loading
cost; it does not repair the generated mesh's irregular surface detail.

Standalone previews show the cleaned source mesh. The current grounded town
placement and entrance are shown in the adjacent town package's
`Previews/Waterfront-Overview.png`.
