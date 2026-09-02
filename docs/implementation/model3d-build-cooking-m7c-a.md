# Model3D Build Cooking — M7C-A

## Result

M7C-A is complete. A SMILE application project can now declare a source GLB or glTF as a distinct `Model3DAsset`. The compiler validates it, discovers its dependencies, extracts and semantically prepares textures, invokes the same SM3D v2 converter implementation used by `smileasset.exe`, caches the generated files by content, and publishes identical logical assets for native and Web builds.

The existing manually prepared Character Viewer project remains available. `Character3DViewerCooked.smileproj` reuses the same viewer source and profile while proving that the original Arin GLB can be built without checking generated SM3D or extracted textures into the project.

This milestone did not add runtime GLB loading, a new runtime model format, PBR features, retargeting, new animation, VFX, or M8 work.

## Reconciliation

- Branch: `main`
- Starting commit: `437598a71436afac55aaa2efefad85abad0d4222`
- Ending commit: the commit containing this report; recorded in the final task report after Git assigns it.
- M7B.1 prerequisite: complete, pushed, and retained.
- Existing `<Asset Include>` behavior remains an exact-copy path.
- Existing projects without `Model3DAsset` items take the previous compiler path unchanged.
- Native Direct3D 11 and WebGL2 both consume published SM3D and PNG files; neither runtime sees or parses the source GLB.
- Dragonfall Classic remains the release path. Arin is still a prototype alias in Dragonfall and the canonical production identity remains `sin-star-i.character-1.paladin` for Sin Star I.

## Project contract

Example:

```xml
<Model3DAsset Include="SourceAssets\Arin\sin-star-i-character-1-paladin-tripo-v01.original.glb"
              Descriptor="SourceAssets\Arin\ArinPrototype.sm3d.json"
              LogicalPath="Assets\Generation2\Arin\ArinPrototype.sm3d"
              TextureOutputDirectory="Assets\Generation2\Arin\Textures"
              Profile="Character"
              Identity="sin-star-i.character-1.paladin"
              ProductionState="Prototype" />
```

| Attribute | Requirement/default |
| --- | --- |
| `Include` | Required, concrete, confined, exact-case existing `.glb` or `.gltf` file. |
| `LogicalPath` | Required, concrete, confined project-relative `.sm3d` output. |
| `Profile` | Required: `Static` or `Character`. `Static` rejects skeletons/clips; `Character` requires both. |
| `Descriptor` | Optional confined, exact-case existing `.json` descriptor. |
| `Identity` | Optional lowercase dot-separated identifier, at most 128 characters. |
| `TextureOutputDirectory` | Optional confined directory. Defaults to `Textures` beside the logical SM3D output. |
| `SampleRate` | Optional integer from 15 through 60. The converter default applies when omitted. |
| `ProductionState` | Optional: `Prototype`, `ProductionCandidate`, or `ProductionApproved`; defaults to `Prototype`. |

Application projects may declare at most 64 items. Library projects reject them so target-neutral `.smilelib` packages do not acquire hidden platform build dependencies. Output paths and ordinary/generated collisions are compared case-insensitively for portable publication.

Visual Studio projects the source under its physical folder and labels it with the profile, for example `sin-star-i-character-1-paladin-tripo-v01.original.glb (Character Model3DAsset)`. The project refresh coordinator watches model and descriptor directories.

## Cooker

- Shared implementation: `Model3DAssetCooker` lives in `Smile.AssetTool`; the compiler references that assembly and calls it in-process. `smileasset.exe` and project cooking therefore use the same `Sm3dV2.Convert` implementation.
- Converter identity: `smile-model3d-cooker-m7c-a-v3`.
- Dependencies: GLB JSON/BIN chunks, confined external buffers/images, base64 data URIs, and the optional descriptor participate in the cache key. Absolute, escaping, backslash, missing, unsupported-MIME, and over-limit dependencies are rejected.
- Texture semantics: embedded/external PNG or JPEG inputs are decoded and deterministically re-encoded as PNG. Base color, tangent normal, ORM, and emissive references are rewritten to generated logical paths. Shared metallic-roughness/occlusion preserves RGB; metallic-roughness without occlusion receives neutral R=255; separate occlusion is packed into R after matching dimensions; occlusion-only receives neutral roughness/metallic channels.
- Cache key: SHA-256 over converter identity, logical output paths, profile, identity, sample rate, production state, source bytes, descriptor bytes, and discovered dependency identity/bytes.
- Cache location: `<project>\obj\Smile\Model3DCache\<sha256>`.
- Cache validation: manifest version/key/converter and every output length/hash are checked. Corrupt entries become `CACHE-RECOVER` and are rebuilt.
- Concurrency: a global named mutex serializes a cache key across processes. Abandoned mutex ownership is recovered. The compiler performs the cook in-process, so canceling the compiler cannot leave a child converter process behind.
- Atomicity: outputs and the manifest are written through unique temporary files with write-through flushes. A complete temporary cache entry displaces the old entry only after validation. Existing staged asset publication then atomically replaces the destination and removes stale generated outputs.
- Status output: `COOK`, `CACHE-HIT`, or `CACHE-RECOVER`, followed by source, logical output, and cache key.

## Bounded inputs and ownership

| Resource | Limit/owner |
| --- | --- |
| `Model3DAsset` items | 64 per application project; project model owns declarations. |
| Source GLB/glTF | 64 MiB; cooker owns build-time bytes only. |
| JSON chunk/document | 4 MiB. |
| Buffer dependency | 64 MiB each. |
| Compressed image | 32 MiB each. |
| Image dimensions | 4096 per side and 16,777,216 pixels. |
| Aggregate decoded textures | 256 MiB. |
| Runtime meshes/objects/models/textures/materials/skeletons/clips/animators | Existing Renderer3D resource ownership and limits are unchanged. Generated SM3D/PNG files enter through the existing project asset publisher and runtime loaders. |

The source GLB and build cache are compiler/build-time resources. Published SM3D and PNG files are application assets. Native and Web runtime ownership remains unchanged.

## Diagnostics

- `SML3700`–`SML3711`: project declaration shape, count, project kind, paths, profile, descriptor, identity, texture directory, sample rate, production state, and portable output collision errors.
- `SML3712`: cooking failure at the original project item location, retaining the stable `SMA1400`–`SMA1439` cooker reason.
- `SML3713`: generated output collision with any ordinary or generated project asset.

Focused negative evidence:

```text
SML3712: ... Static Model3DAsset profile rejects skeletons and animation clips.
SML3713: Generated Model3DAsset output collides with another project asset.
```

Failed cooking and collision tests preserve the previously published output. A renamed logical output invalidates the key and removes the stale published model.

## Arin cooked output

Source:

```text
games/Dragonfall/SourceAssets/Arin/sin-star-i-character-1-paladin-tripo-v01.original.glb
1,479,468 bytes
identity: sin-star-i.character-1.paladin
profile/state: Character / Prototype
```

Inspection:

```text
parts: 1
vertices: 6,631
indices: 29,922
triangles: 9,974
materials: 1
texture references: 3
bones: 41
nodes: 73
clips: 3
events: 4
sockets: 6
```

Native and Web published the same five asset paths and SHA-256 values:

| Logical path | Bytes | SHA-256 |
| --- | ---: | --- |
| `Assets/Generation2/Arin/ArinPrototype.sm3d` | 709,956 | `7A8C2B93D825DC80EB4C5BDAC4C2FF4A624A4C26849918DE54E4B67F18D6C1C6` |
| `Assets/Generation2/Arin/Textures/ArinPrototype-m0-base-color-ccd55f34588c.png` | 2,183,883 | `CAEAF1BAFF6C7F0B465A75F0808A0DF2EC8998C9562A9341E41209E978B89254` |
| `Assets/Generation2/Arin/Textures/ArinPrototype-m0-normal-8dd49de64c69.png` | 1,454,013 | `942A193949BEF7B067B0113553A7788604AA01E3716B963354E1905765FC472E` |
| `Assets/Generation2/Arin/Textures/ArinPrototype-m0-orm-49c70f0d00f7.png` | 1,755,724 | `DC9AEE72FCCD2512154D44887F8B29749AD17A5FB65D7432703731D6944B65DD` |
| `TechnicalAssets/Generation2/AnimationArticulated.sm3d` | 9,712 | `23B2571E40612FE39AE9F28B923B2BAEA1F610A95F47288D379EB7FE5B86329B` |

The source GLB was not published. Generated textures are 1024×1024 PNGs, but their source MIME is JPEG, so the cache manifest correctly records `sourceWasLossy: true`; PNG re-encoding does not promote prototype texture quality.

## Determinism and compatibility evidence

The repository-owned static glTF fixture in `examples/Model3DAssetCooking` builds through the same contract and publishes only its SM3D result.

The focused cooker gate proves:

```text
Model3DAsset cooking tests passed.
Cold build: COOK
Second target: CACHE-HIT
Corrupt entry: CACHE-RECOVER
Concurrent native/Web: COOK plus CACHE-HIT with identical outputs
Collision: SML3713; failed cook preserved output; renamed cook removed stale outputs
Published parity assets: 4
```

For its controlled fixture path, the four generated hashes are:

```text
SM3D  055EB692817D7CA4639BF725E3D382716B17A272F02432A59BB2C535B2729E19
Base  CAEAF1BAFF6C7F0B465A75F0808A0DF2EC8998C9562A9341E41209E978B89254
Normal 942A193949BEF7B067B0113553A7788604AA01E3716B963354E1905765FC472E
ORM   DC9AEE72FCCD2512154D44887F8B29749AD17A5FB65D7432703731D6944B65DD
```

The controlled SM3D differs from the viewer SM3D only because the logical texture paths are part of the deterministic model payload. The texture bytes are identical to the manually prepared prototype assets.

The final full-smoke run measured 2,510 ms for a cold native build and cook, and 816 ms for the following Web cache-hit build. These are end-to-end compiler timings, not isolated microbenchmarks. The source is 1,479,468 bytes; generated model plus three textures are 6,103,576 bytes. Decoded texture work is capped at 256 MiB aggregate rather than treated as an unbounded allocation target.

## Viewer evidence

The cooked project uses `Character3DViewer.smile` and `Character3DViewerProfile.smile` unchanged. It proves:

- profile-driven model, identity, role, expected clips, sockets, and material channels;
- calculated auto-fit through the existing bounds query;
- Idle/Walk/Run clip browsing and speed control;
- socket and material-channel diagnostics;
- smoothed mouse/button orbit, pan, and zoom;
- `O` toggles smooth automatic orbit;
- identical source-level Viewer behavior on native and Web.

The native image is the authoritative current rendering proof. The Web capture exercises the same cooked asset bytes and WebGL2 path; its current animated-skinned presentation retains the known Web appearance visible in the preceding M7B.1 evidence and is not caused by the cooker, because manually copied and cooked payloads reproduce the same Web frame.

## Screenshots

See [screenshot-index.md](screenshots/m7c-model3d-cooking/screenshot-index.md). Eight required images are committed as true PNG bytes. The phone contact sheet is 1170×2532 and below the 5 MiB evidence limit.

## Visual Studio extension

- VSIX: `artifacts\vsix\Smile.VisualStudio.vsix`
- Version: `2.0.57`
- Installed instance: `91f001b5`
- Installed assembly version: `2.0.57.0`
- Installed/built DLL SHA-256: `002BE4EBF9EA10F18C65FE410F2D623CF9D3B96E72077159DD43067447CB5ED3`
- Verification: exactly one installed manifest, version match, assembly-version match, and installed/built DLL hash match.

## Plan deviations

1. Build cooking calls the AssetTool assembly in-process instead of launching `smileasset.exe`. This still uses one converter implementation, removes child-process/orphan risk, and makes compiler cancellation immediate.
2. Detailed generated provenance lives in the content-addressed cache manifest. Runtime publication retains the established `smile-assets.json` schema so existing runtimes and projects remain compatible.
3. The manually prepared viewer was not rewritten. A separate cooked project reuses its source/profile, giving an A/B path without destabilizing the retained M7B.1 workflow.
4. Texture decode/encode uses `System.Drawing.Common` in the Windows build-time tool, consistent with the repository's Windows-native priority. Published output remains target-neutral and identical for native/Web.
5. M7C-A does not add `Model3DAnimation`; the currently supplied package contains animations in one exact-skeleton GLB and does not justify a speculative multi-file merge contract.

## Known limitations and M7C-B readiness

- The current prototype has only Idle, Walk, and Run, not the ten required production combat clips.
- Required combat events, eight production sockets, deformation review, and explicit user visual approval are incomplete.
- The 1K base/normal/ORM sources are JPEG-derived, not the required 2K lossless production maps.
- Provenance and distribution/modification-rights evidence is incomplete.
- Equipment structure is one runtime part and is not established as swappable.
- Arbitrary skeleton retargeting is deliberately unsupported.

M7C-A unblocks ordinary GLB-first development and M7C-B intake. It does not unblock production promotion or M8.
