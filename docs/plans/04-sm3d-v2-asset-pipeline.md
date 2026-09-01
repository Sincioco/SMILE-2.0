# SM3D Version 2 Asset Pipeline

## Goal

Create a deterministic, backward-compatible offline character package that carries the information required for modern skinned actors while keeping game runtime loading small and bounded.

## Compatibility rules

- SM3D version 1 remains readable and test-covered.
- Existing `smileasset.exe model input.gltf -o output.sm3d` behavior remains available.
- SM3D version 2 uses the same `SM3D` magic and a distinct version field.
- A v1 loader must not attempt to interpret v2 bytes.
- A v2 loader validates the complete file before allocating renderer resources.
- Unknown required chunks fail clearly.
- Unknown optional chunks may be ignored only when the format marks them optional.
- Output remains little-endian, checksum-protected, timestamp-free, and byte-deterministic.

## Incremental v2 capability levels

Do not implement every possible extension at once.

### V2 core — M1

Required:

- `.gltf` and `.glb` input;
- indexed triangle primitives;
- POSITION;
- NORMAL;
- TANGENT, or deterministic tangent generation;
- TEXCOORD_0;
- material records;
- external texture path references;
- base-color, normal, ORM, and emissive texture semantics;
- alpha mode and cutoff;
- stable names/string table;
- deterministic bounds;
- validation and reporting.

### V2 animation — M3

Required:

- JOINTS_0;
- WEIGHTS_0;
- skeleton hierarchy;
- complete inverse bind matrices;
- named sampled clips;
- events from the optional descriptor;
- named socket nodes;
- optional root-motion metadata.

### Deferred v2 extensions

Do not block M7 on:

- morph targets;
- embedded compressed textures;
- geometry compression;
- multiple UV sets;
- sparse accessors;
- automatic retargeting;
- in-file multi-LOD groups;
- animation compression beyond obvious safe packing.

These can use reserved optional chunk IDs later.

## Proposed container design

Use a small chunk directory so optional sections can be added without another immediate format rewrite.

Codex may adjust exact packing after reconciling the current loader, but it must document the final byte layout before completing M1.

### Header requirements

The header should include at minimum:

- magic;
- version;
- header size;
- flags;
- exact file size;
- payload checksum;
- chunk count;
- chunk directory offset;
- reserved zero fields.

### Chunk directory requirements

Each directory entry should include:

- four-byte chunk identifier;
- flags, including optional/required;
- byte offset;
- byte length;
- record count where meaningful;
- stride where meaningful;
- reserved zero.

Suggested semantic chunks:

| Chunk | Purpose | Initial milestone |
|---|---|---|
| `STR0` | UTF-8 string table | M1 |
| `PART` | mesh-part records and material mapping | M1 |
| `VERT` | packed vertex records | M1 |
| `INDX` | index data | M1 |
| `MATL` | PBR material records | M1 |
| `TEXR` | texture path and semantic records | M1 |
| `BOND` | model and part bounds | M1 |
| `NODE` | named hierarchy/socket nodes | M3 |
| `SKEL` | bones, parents, bind transforms, inverse bind matrices | M3 |
| `CLIP` | clip metadata | M3 |
| `AFRM` | sampled animation frames | M3 |
| `EVNT` | animation event records | M3 |
| `ROOT` | root-motion policy metadata | M3 |
| `MORP` | optional morph metadata | deferred |
| `LODS` | optional LOD grouping | deferred |

## Logical vertex data

Generation 2 needs:

```text
Position       float32 x 3
Normal         normalized or float representation x 3
Tangent        normalized or float representation x 4
UV0            float32 or safely quantized x 2
JointIndices   uint16 x 4
Weights        normalized uint16 x 4
```

Static meshes use joint zero with full weight only when represented through the skinned format; they must not accidentally enable skinning.

The converter should report:

- source face count where available;
- runtime triangle count;
- vertex count after UV/normal/tangent splits;
- part count;
- material count;
- bone count;
- clip count;
- estimated animation bytes.

## PBR material record

Each material should include:

- name string index;
- base-color texture reference or none;
- normal texture reference or none;
- ORM texture reference or none;
- emissive texture reference or none;
- base-color factor;
- metallic factor;
- roughness factor;
- normal strength;
- occlusion strength;
- emissive RGB/intensity;
- alpha mode;
- alpha cutoff;
- double-sided flag.

The first implementation uses PNG texture assets already declared through the project manifest.

Do not embed texture bytes into SM3D v2 in M1.

## Texture path policy

- Store normalized project-relative paths or model-relative paths using one documented convention.
- Reject absolute paths.
- Reject parent traversal.
- Preserve exact case and separator normalization required by current asset publication.
- Resolve through the existing declared-asset manifest.
- Base color and emissive are color textures.
- Normal and ORM are linear-data textures.
- Missing optional maps use documented defaults.
- Missing declared required maps produce a clear load failure or material fallback according to the final documented policy.

## GLB import

Add standard GLB container parsing to `Smile.AssetTool`.

Minimum validation:

- correct GLB magic;
- supported version;
- exact declared length;
- one JSON chunk;
- zero or one BIN chunk for the required profile;
- aligned chunk boundaries;
- valid JSON;
- accessor ranges contained within buffer views and buffers;
- no integer overflow;
- finite numeric values;
- supported component types;
- triangle primitives only;
- valid material references;
- valid node/skin references in M3.

Do not add runtime GLB parsing.

## Optional descriptor

Use an optional deterministic JSON descriptor for metadata that Blender/glTF export does not represent reliably enough for the first implementation.

Suggested filename:

```text
Arin.sm3d.json
```

Illustrative schema:

```json
{
  "version": 1,
  "sampleRate": 30,
  "rootMotionBone": "Root",
  "clips": {
    "Idle": {
      "loop": true
    },
    "SwordAttack": {
      "loop": false,
      "events": [
        { "timeMs": 180, "name": "SwordTrailOn" },
        { "timeMs": 430, "name": "SwordImpact" },
        { "timeMs": 560, "name": "SwordTrailOff" }
      ]
    }
  },
  "sockets": {
    "SwordTip": "socket-sword-tip",
    "ShieldCenter": "socket-shield-center",
    "Head": "socket-head"
  }
}
```

Rules:

- descriptor version is required;
- unknown required fields fail;
- key ordering does not affect output;
- event names are entered into the deterministic string table;
- times are clamped/validated against clip duration;
- duplicate clip/socket/event definitions fail clearly;
- absent descriptor produces reasonable defaults.

Codex may use a different exact schema if it documents and tests it.

## Command-line shape

Preserve current commands and add only small options, for example:

```powershell
artifacts\assettool\smileasset.exe model `
    games\Dragonfall\Assets\Source\Arin.glb `
    --descriptor games\Dragonfall\Assets\Source\Arin.sm3d.json `
    --profile character `
    -o games\Dragonfall\Assets\Models\Arin.sm3d
```

Recommended additional command:

```powershell
artifacts\assettool\smileasset.exe inspect `
    games\Dragonfall\Assets\Models\Arin.sm3d
```

`inspect` should produce deterministic text suitable for tests:

```text
Version: 2
Parts: 3
Triangles: 12480
Vertices: 10132
Materials: 3
Bones: 68
Clips: 18
Sockets: 5
```

Do not turn `Smile.AssetTool` into a general asset-management framework.

## Initial bounded limits

Codex must reconcile these with current capacities and actual fixture needs.

Suggested Generation 2 maxima:

| Item | Proposed hard maximum |
|---|---:|
| File size | 64 MiB |
| Parts | 32 |
| Total vertices | 131,072 |
| Total indices | 393,216 |
| Materials | 64 |
| Texture references | 256 |
| Bones | 128 |
| Clips | 64 |
| Clip duration | 120 seconds |
| Sample rate | 15–60 Hz |
| Events per clip | 64 |
| Socket nodes | 64 |

These are safety bounds, not target budgets.

## Runtime resource model

M1 may continue exposing v2 static parts through `Model3D`.

M3 should add a higher-level asset handle that can expose:

- model part count;
- material count;
- skeleton;
- clip count and name lookup;
- socket count and lookup.

The loader should allocate atomically:

1. validate complete file;
2. verify all dependencies and capacities;
3. allocate owned resources;
4. publish one valid asset handle;
5. roll back every allocation on failure.

## Required tests

### Converter tests

- v1 input still produces expected v1 output.
- same v2 input converted twice is byte-identical.
- valid `.gltf` and equivalent `.glb` produce equivalent semantic inspection.
- invalid GLB magic/version/length/chunks fail.
- out-of-range accessors fail.
- missing tangent generation is deterministic.
- invalid material/texture path fails.
- NaN/infinity fails.
- unsupported primitive mode fails.
- path traversal fails.
- descriptor duplicates and out-of-range events fail.

### Runtime tests

- v1 asset loads and draws.
- v2 static asset loads and draws.
- v2 material metadata is queryable.
- malformed chunk offsets fail before allocation.
- checksum failure fails.
- unknown required chunk fails.
- unknown optional chunk follows the documented policy.
- repeated load/unload returns all counters to zero.
- native and Web report the same semantic inspection values.

## M1 acceptance

M1 is complete only when:

1. Both current v1 fixtures and new v2 fixtures pass.
2. GLB input works offline.
3. Tangents and PBR material metadata survive conversion and loading.
4. The same v2 fixture draws on Direct3D 11 and WebGL2, even before the full PBR shader is enabled.
5. Conversion is deterministic.
6. No game runtime contains a glTF or GLB parser.
7. Documentation contains the exact final binary layout.
