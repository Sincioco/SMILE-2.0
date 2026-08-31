# smileasset

`smileasset.exe` converts bounded authoring formats into deterministic SMILE runtime assets. It is built by `scripts\build.cmd` into `artifacts\assettool`.

The commands are:

```text
smileasset model input.gltf -o output.sm3d
smileasset model input.gltf --format-version 2 -o output.sm3d
smileasset model input.glb -o output.sm3d
smileasset model input.glb --descriptor input.sm3d.json -o output.sm3d
smileasset inspect input.sm3d
```

The original textual-glTF command continues to emit byte-compatible SM3D v1. `--format-version 2` emits SM3D v2 from textual glTF, while strict GLB 2.0 input selects v2 automatically. A source with neither skin nor animation emits the static core. A source with exactly one skin and 1–64 animations emits the complete optional M3 animation group and supports up to 128 bones. `--descriptor` selects the strict version-1 JSON policy for fixed sample rates, loop flags, named events, root-motion extraction, and named sockets. `inspect` validates a complete v1/v2 file and prints deterministic static plus animation metadata suitable for tests.

The converter accepts animation translation/rotation/scale channels with LINEAR or STEP interpolation and samples them at 15–60 Hz with an exact final sample. CUBICSPLINE, multiple skins, more than 128 bones, nonuniform production scale, morphs, and partial skin/animation sources fail explicitly. Output remains deterministic, transactional, and limited to 16 MiB.

The supported glTF subset, SM3D binary layout, descriptor, validation ceilings, coordinate conversion, and runtime ownership contract are documented in `docs\architecture\sm3d-model-format.md`. Conversion is an offline build/content step; games never invoke Blender or parse glTF at runtime. The repository-owned animation corpus is regenerated and verified with `scripts\generate-renderer3d-animation-v2-fixtures.ps1 -Check`.
