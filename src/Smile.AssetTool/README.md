# smileasset

`smileasset.exe` converts bounded authoring formats into deterministic SMILE runtime assets. It is built by `scripts\build.cmd` into `artifacts\assettool`.

The commands are:

```text
smileasset model input.gltf -o output.sm3d
smileasset model input.gltf --format-version 2 -o output.sm3d
smileasset model input.glb -o output.sm3d
smileasset inspect input.sm3d
```

The original textual-glTF command continues to emit byte-compatible SM3D v1. `--format-version 2` emits the SM3D v2 static core from textual glTF, while strict GLB 2.0 input selects v2 automatically. `inspect` validates a complete v1/v2 file and prints deterministic semantic metadata suitable for tests.

The supported glTF subset, SM3D binary layout, validation ceilings, coordinate conversion, and runtime ownership contract are documented in `docs\architecture\sm3d-model-format.md`. Conversion is an offline build/content step; games never invoke Blender or parse glTF at runtime.
