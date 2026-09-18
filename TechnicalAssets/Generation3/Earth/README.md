# Original Earth effects assets

`Source/prepare_earth.py` reproduces this package using the already-installed
Blender 5.2 and its bundled Python/numpy. Run Blender with `--background
--python-exit-code 1 --python` followed by that source path. No downloads are needed.

- `earth-rocks.glb`: three deformed, weathered rock meshes, 5,120 triangles each.
  Continuous spherical UVs, smooth normals and uneven fracture faces replace the
  initial visibly faceted 1,280-triangle rocks. Only one UV set is exported, as
  required by the existing SM3D static model contract.
- `earth-stone.png` / `earth-normal.png`: original 2048² seamless procedural
  mineral mottling, quartz grains, iron discoloration, dark seams and fine relief.
- `earth-dust.png`: original 256² soft irregular dust sprite.
- `earth-rise.wav`, `earth-launch.wav`, `earth-impact.wav`: original seeded-noise,
  grit/transient and low-frequency synthesis; 48 kHz stereo 16-bit PCM.
- `Source/earth-rocks.blend`: packed editable generation checkpoint.
- `EarthRocks.sm3d.json`: current static model descriptor.

`Smile.Simple3D.EarthVfx3D` shares these resources between Earth Lab, Character
Viewer and Sin Star I. It reuses 63 instances and a bounded 4,096-slot GPU dust
pool. Hurl/Volley clear all sixty loose ground stones after lift-off. Impact
fragments still appear at the target; each dust puff expands and fades through
the cast's recovery and into the next animation instead of being cut off.

The three user-supplied YouTube references are linked in the Earth Lab README.
No video, sampled audio or extracted media from those references is included.
