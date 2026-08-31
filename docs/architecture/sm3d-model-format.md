# SM3D model asset format and pipeline

SMILE model assets use a deterministic offline pipeline:

```text
Blender or another authoring tool -> glTF 2.0 (.gltf or .glb) -> smileasset.exe -> .sm3d
```

Blender and glTF parsing are never required by a game at runtime. Project asset publication remains format-neutral; a project declares `.sm3d` files with ordinary `Asset` items, and the existing exact-path/case manifest protects native and Web loading.

## Converter

```powershell
artifacts\assettool\smileasset.exe model Assets\Source\Hero.gltf -o Assets\Models\Hero.sm3d
artifacts\assettool\smileasset.exe model Assets\Source\Hero.gltf --format-version 2 -o Assets\Models\Hero.sm3d
artifacts\assettool\smileasset.exe model Assets\Source\Hero.glb -o Assets\Models\Hero.sm3d
artifacts\assettool\smileasset.exe inspect Assets\Models\Hero.sm3d
```

The original command remains the v1 textual-glTF compatibility path. `--format-version 2` selects v2 for textual glTF; GLB input always selects v2. The strict GLB reader accepts version 2, one JSON chunk followed by at most one BIN chunk, four-byte chunk alignment, exact declared length, and no trailing bytes. The v2 static profile selects the declared active scene and traverses its nodes depth-first in declared order. It accepts indexed triangle primitives, POSITION/NORMAL/TEXCOORD_0, optional TANGENT, unsigned indices, PBR metallic/roughness metadata, embedded data buffers, confined relative external buffers, or GLB buffer zero. Unreachable meshes are ignored. A reachable mesh used by multiple nodes becomes one deterministic SM3D part per instance, with node/mesh/primitive/instance identity in the part name.

Static node `matrix` or TRS transforms are composed from parent to child and baked into positions. Quaternions with usable nonzero length are normalized. Normals use the inverse-transpose linear transform; tangents are transformed and re-orthogonalized. Singular/non-finite transforms and matrix-plus-TRS nodes fail. Node and coordinate reflections update winding and tangent handedness, and the glTF-to-SMILE coordinate conversion is applied exactly once. Bounds are computed from the final transformed geometry.

Sparse accessors, skins, animations, joint/weight attributes, morph targets/weights, multiple UV sets, non-triangle modes, compressed geometry, embedded image bytes, runtime glTF/GLB parsing, and runtime Blender dependencies are rejected rather than discarded. The only accepted extension is `KHR_materials_emissive_strength`; unknown required extensions and output-changing node, primitive, material, texture, sampler, or texture-info extensions fail. Texture info must use UV0. An absent sampler uses the glTF repeat/trilinear defaults; an explicit sampler must use linear magnification (`9729`), trilinear minification (`9987`), and repeat S/T (`10497`). M1 stores no texture bytes. Animation-related v2 chunks are reserved for M3 and are not emitted or interpreted by the static loader.

Every accessor range, alignment, target, stride, count, numeric value, index, and material reference is validated before output against the buffer's declared logical length. A primitive without a material receives an implicit `Default` material; it never aliases declared material zero, and the implicit entry counts against the 64-material limit. The writer uses little-endian values, stable traversal order, no timestamps or source paths, an exact byte length, and an FNV-1a payload checksum. Identical input produces byte-identical output. Publication uses a unique temporary file, flushes before atomic replacement, preserves an existing output on failure, and removes temporary residue in `finally`. Input and output may not resolve to the same file.

### Converter source limits

The converter rejects excessive input before expensive allocation or whole-file reads:

| Source item | Limit |
|---|---:|
| Textual glTF JSON | 4 MiB |
| GLB container | 64 MiB |
| One declared buffer | 32 MiB |
| Aggregate declared buffers | 64 MiB |
| Buffers | 16 |
| Buffer views | 512 |
| Accessors | 512 |
| Scenes | 16 |
| Nodes | 4,096 |
| Meshes | 256 |
| Reachable source primitives | 4,096 |
| Materials | 64 |
| Textures/samplers | 128 each |
| Images | 128 |
| Source and emitted names | 1,024 UTF-8 bytes |

Base64 encoded size is preflighted before decoding. Physical bytes beyond an external/data buffer's declared length are not addressable; a GLB BIN chunk may contain only its normal zero-to-three bytes of alignment padding. `inspect` checks the 16 MiB SM3D limit before reading the file.

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

Runtime bounds are 16 MiB, 16 parts, 65,535 vertices and 196,608 indices per part, 64 material slots, 64 live models, 128 total live meshes, and 512 live objects. Counts, arithmetic, finite floats, local index ranges, material references, magic, version, reserved values, exact size, and checksum are validated before any mesh is allocated.

## Binary format 2

All offsets are absolute file offsets, all integers and IEEE-754 floats are little-endian, and every directory entry and chunk starts on a four-byte boundary. The writer zero-fills alignment padding and writes chunks in the required order below.

### Header

| Offset | Size | Value |
|---:|---:|---|
| 0 | 4 | ASCII `SM3D` |
| 4 | 2 | version `2` |
| 6 | 2 | header size `64` |
| 8 | 4 | flags, currently zero |
| 12 | 4 | exact file size |
| 16 | 4 | FNV-1a checksum of bytes 64 through EOF |
| 20 | 4 | chunk-directory entry count, 1-32 |
| 24 | 4 | directory offset, currently 64 |
| 28 | 4 | directory-entry size, currently 32 |
| 32 | 4 | model-name offset in `STR0` |
| 36 | 4 | part count |
| 40 | 4 | total vertex count |
| 44 | 4 | total index count |
| 48 | 4 | material count |
| 52 | 4 | texture-reference count |
| 56 | 8 | reserved zero |

### Chunk directory

Each 32-byte entry contains: ASCII ID at +0, flags at +4, file offset at +8, byte length at +12, record/string count at +16, record stride at +20, and two reserved zeros at +24 and +28. Flag bit 0 means optional; all other bits are invalid. Ranges must be aligned, within the exact file size, and non-overlapping. Duplicate IDs fail.

Unknown required chunks fail. Unknown optional chunks are checksum-, flag-, alignment-, and range-validated and then ignored. Every known M1 chunk is required and must have flags zero. The writer order is:

| ID | Count | Stride | Contents |
|---|---:|---:|---|
| `STR0` | strings | 0 | strict UTF-8 NUL-terminated strings; offset zero is the empty string |
| `PART` | parts | 32 | part records |
| `VERT` | vertices | 48 | static vertices |
| `INDX` | indices | 4 | local unsigned 32-bit part indices |
| `MATL` | materials | 80 | static PBR metadata |
| `TEXR` | texture references | 16 | external path and semantic |
| `BOND` | parts + 1 | 32 | model bounds, then part bounds |

### Records

`PART` uses uint32 fields: name string offset +0, first vertex +4, vertex count +8, first index +12, index count +16, material index +20, bounds index +24, reserved zero +28. Part ranges are contiguous, indices are local to the part, and bounds index is part index + 1.

`VERT` uses twelve float32 fields: position XYZ +0, normal XYZ +12, tangent XYZW +24, and UV0 +40. Normal and tangent XYZ must be finite, unit length, and mutually orthogonal within a squared-length/dot-product tolerance of `0.0001`; tangent W must be exactly -1 or +1. AssetTool, native, and Web enforce the same rule. `INDX` contains local uint32 indices and complete nondegenerate triangles.

`MATL` uses uint32 name/base-color/normal/ORM/emissive references at +0 through +16, alpha mode at +20 (`0` opaque, `1` mask, `2` blend), double-sided bit at +24, and reserved zero at +28. A missing texture reference is `0xFFFFFFFF`. Float32 metadata is base-color RGBA +32, metallic +48, roughness +52, normal strength +56, occlusion strength +60, emissive RGB +64, and alpha cutoff +76. Factors are finite and range-checked. M2 consumes them directly through the built-in PBR-lite shader.

`TEXR` uses path string offset +0, semantic +4 (`1` base color, `2` normal, `3` packed occlusion/roughness/metallic, `4` emissive), and reserved zeros +8/+12. `BOND` uses float32 minimum XYZ +0 and maximum XYZ +12 plus reserved zeros +24/+28. Declared model/part bounds must exactly match computed positions.

### Hard limits

V2 retains the 16 MiB file, 16-part, 65,535-vertex-per-part, 196,608-index-per-part, and 64-material ceilings. It adds totals of 131,072 vertices and 393,216 indices plus at most 128 external texture references. Runtime pool limits remain 64 models, 128 meshes, 512 objects, 128 textures, and 128 materials. The loader validates the entire file and preflights model/mesh/texture/material capacity before allocation; any later path, image, shader, or allocation failure releases every resource created by that load and leaves prior live counts unchanged.

### Texture paths and tangents

Texture references are case-preserving project-relative UTF-8 paths of 1-1,024 bytes with forward slashes. Absolute, drive, UNC, URI, backslash, empty/dot/parent, wildcard, control-character, and network paths fail conversion and loading. Runtime resolution remains the existing exact declared-asset manifest contract.

Valid glTF tangent `VEC4` data is normalized, Gram-Schmidt orthogonalized, normalized again, and imported; the Z reflection also flips handedness. Otherwise the converter deterministically accumulates triangle tangent/bitangent vectors in source order, Gram-Schmidt orthogonalizes against the normal, and derives handedness offline. Non-finite data, zero vectors, degenerate geometry, and degenerate UV derivatives fail conversion.

## Runtime API and ownership

`Graphics3D.LoadModel3D(path)` returns a `Core.Model3D` with its format version, part/material/vertex/index counts, v2 texture-reference count, and model-owned PBR resource counts. For v2, it resolves every retained `TEXR` path only through the existing exact declared-asset manifest, creates one PBR texture per texture record and one PBR material per material record, and publishes the model only after the complete dependency graph succeeds. Color semantics use sRGB; normal/ORM use linear data sampling. Command-80 hashes remain diagnostics and are never lookup keys.

`CreateModelPart3D(model, part)` creates an ordinary `Object3D` that shares the model-owned mesh and automatically borrows the imported PBR material for that part. An explicit material override is allowed; clearing it restores the imported material. Model-owned material handles are internal and reject direct destruction. Objects must be destroyed before their model. `DestroyModel3D` refuses while any part object is live and leaves the public record unchanged. Successful model destruction releases owned materials, textures/retained images, and meshes in dependency order. Model handles are generation-checked natively and never reused on Web. Reset destroys objects before models and returns every model-owned resource count to its prior value.

`LoadModel3D` returns a zero handle for an undeclared/missing, wrong-case, malformed, oversized, unsupported, shader-unavailable, image-decode, or exhausted asset. `LastError` distinguishes malformed data, missing/read failure, dependency/capacity failure, shader failure, and a live-owner destruction refusal. Command 80 name/path values are FNV-1a diagnostics for tests and inspection, not unique identifiers or texture-resolution keys. Invalid command 80 queries, indices, properties, and stale model handles return zero and set `LastError` on both native and Web.

## Verification

`scripts/test-renderer3d-m11-hardening.ps1` owns the generated scene/material/unsupported-feature/source-safety/publication corpus. `scripts/test-renderer3d-models.ps1` verifies v1 byte compatibility and structural inspection; equivalent GLB/glTF v2 output; deterministic imported/generated tangents; PBR metadata and path survival; the shared malformed SM3D corpus through inspect/native/Web; invalid command 80 use; required/optional chunk policy; unchanged live counts on failure; native/Web semantic parity and drawing; and complete teardown. `scripts/test-renderer3d-pbr.ps1` adds exact manifest resolution, automatic material assignment, model-owned dependency sharing, missing/wrong-case and pool-exhaustion rollback, ten lifecycle cycles, and native/Web PBR diagnostics. `scripts/test-renderer3d-v2-boundaries.ps1` generates exact 131,072-vertex/393,216-index input plus over-limit part, per-part geometry, material, texture-reference, and file-size cases.
