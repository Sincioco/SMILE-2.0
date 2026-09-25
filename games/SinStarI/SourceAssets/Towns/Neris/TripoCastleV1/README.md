# Neris Tripo Castle V1

Created by: Louiery R. Sincioco (Sin).

## Ownership and source

`Original/Neris-Castle-Tripo.glb` is the untouched user-supplied export from
`Neris Castle - 4K - 2M - PBR - No Light.glb`. Its SHA-256 is recorded in
`cleanup.json`. `Blend/Neris-Castle-Cleaned.blend` owns the editable cleaned
architecture in the **Neris Tripo Castle Architecture** collection. The expanded
town links that collection by a relative path; keep both packages together.
The original castle in town remains available for comparison.

## Cleanup and placement

The cleanup removes imported blue water, low underside faces and disconnected
low fragments, preserving 1,770,324 of 1,934,613 source triangles. No decimation
was applied. The original 4K PBR atlases and UV coordinates are retained. This
is still the supplied generated mesh, including its irregular ornamental detail;
cleanup does not reconstruct or retopologize the architecture.

The castle is placed at Blender X=-228, Y=176 with uniform scale 170, **twice the
initial comparison scale of 85**. The entrance deck at source Z=0.19 is aligned
to the 0.212-m paving surface. Uneven lower foundation geometry is buried in the
island, rather than suspending the building by its lowest isolated vertex.
The viewer equivalent is scale 170000%, position (-2280,-299.88,1760).

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
3. For the current expanded town, run `Source/refine_castle.py`, then
   `Source/export_native.py` and `Source/export_layout.py` in Blender background.
   The earlier `place_comparison_castle.py` is only for the initial comparison
   layout; it rejects the newer royal reconstruction to preserve its placement.
4. Run the town's `Source/render_minimap.ps1`, then build the native Viewer.

Standalone previews show the cleaned source mesh. The current grounded town
placement and entrance are shown in the adjacent town package's
`Previews/Castle-Comparison.png` and `Previews/Castle-Entrance.png`.
