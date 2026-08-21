# smileasset

`smileasset.exe` converts bounded authoring formats into deterministic SMILE runtime assets. It is built by `scripts\build.cmd` into `artifacts\assettool`.

The current command is:

```text
smileasset model input.gltf -o output.sm3d
```

The supported glTF subset, SM3D binary layout, validation ceilings, coordinate conversion, and runtime ownership contract are documented in `docs\architecture\sm3d-model-format.md`. Conversion is an offline build/content step; games never invoke Blender or parse glTF at runtime.
