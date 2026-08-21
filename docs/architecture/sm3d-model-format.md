# SM3D model asset format and pipeline

SMILE model assets use a deterministic offline pipeline:

```text
Blender or another authoring tool -> glTF 2.0 (.gltf) -> smileasset.exe -> .sm3d
```

Blender and glTF parsing are never required by a game at runtime. Project asset publication remains format-neutral; a project declares `.sm3d` files with ordinary `Asset` items, and the existing exact-path/case manifest protects native and Web loading.

## Converter

```powershell
artifacts\assettool\smileasset.exe model Assets\Source\Hero.gltf -o Assets\Models\Hero.sm3d
```

The v1 converter accepts one glTF 2.0 scene, triangle primitives, indexed POSITION/NORMAL/TEXCOORD_0 attributes, unsigned indices, 1-64 material slots, embedded base64 buffers, and relative external buffers confined beneath the source directory. It converts glTF right-handed Z coordinates and triangle winding to Renderer3D's convention. Sparse accessors, GLB containers, morph targets, multiple UV sets, non-triangle modes, compressed geometry, and runtime Blender dependencies are rejected or outside v1. Skin and animation sections are added by the separate skeletal-animation package.

Every accessor range, stride, count, numeric value, index, and material reference is validated before output. The writer uses little-endian values, stable traversal order, no timestamps or source paths, an exact byte length, and an FNV-1a payload checksum. Identical input produces byte-identical output. Publication uses an atomic temporary-file replacement.

## Binary format 1

All integers and IEEE-754 floats are little-endian. Offsets are relative to the start of the file.

| Offset | Size | Value |
|---:|---:|---|
| 0 | 4 | ASCII `SM3D` |
| 4 | 2 | version `1` |
| 6 | 2 | header size `32` |
| 8 | 4 | part count |
| 12 | 4 | total vertex count |
| 16 | 4 | total index count |
| 20 | 4 | material-slot count |
| 24 | 4 | exact file size |
| 28 | 4 | FNV-1a checksum of bytes 32 through EOF |

The header is followed by one 24-byte record per part: first vertex, vertex count, first index, index count, material slot, and a reserved zero. Vertices follow as eight float32 values: position XYZ, normal XYZ, and UV. Indices follow as local uint32 part indices.

Runtime bounds are 16 MiB, 16 parts, 65,535 vertices and 196,608 indices per part, 64 material slots, 64 live models, 128 total live meshes, and 256 live objects. Counts, arithmetic, finite floats, local index ranges, material references, magic, version, reserved values, exact size, and checksum are validated before any mesh is allocated.

## Runtime API and ownership

`Graphics3D.LoadModel3D(path)` returns a `Core.Model3D` with its part and material counts. `CreateModelPart3D(model, part)` creates an ordinary `Object3D` that shares the model-owned mesh. `ModelPartMaterial3D` maps the part to an application-supplied material slot; applications may share one `Material3D` across any number of model parts. Renderer3D has no knowledge of the meaning of those slots.

Objects must be destroyed before their model. `DestroyModel3D` refuses while any part object is live and leaves the public record unchanged. Successful model destruction releases every owned mesh. Model handles are generation-checked natively and never reused on Web. Reset destroys objects before models and returns all model-owned mesh counts to zero.

`LoadModel3D` returns a zero handle for an undeclared/missing, malformed, oversized, unsupported, or exhausted asset. `LastError` distinguishes malformed data, missing/read failure, capacity exhaustion, and a live-owner destruction refusal.

## Verification

`scripts/test-renderer3d-models.ps1` verifies deterministic double conversion, invalid index and material rejection, malformed runtime files, valid humanoid and two-part dragon loads, material-slot mapping, shared mesh ownership, refusal with live objects, DirectX/Web drawing, exact native/Web console parity, and 100 complete load/unload cycles with zero residual handles.
