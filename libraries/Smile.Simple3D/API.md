# Smile.Simple3D 2.0 API

Public 3D positions and sizes use integer world units. Rotations use integer degrees, scales and opacity use percentages, and `Matrix4` values use `Core.FIXED_ONE` fixed point. A zero handle means failure.

## `Smile.Simple3D.Core`

True-3D types:

- `Vector3`: `X`, `Y`, and `Z`.
- `Matrix4`: `M11` through `M44`, scaled by `FIXED_ONE`.
- `Camera3D`: position, target, projection fields, near/far planes, FOV, and the legacy wireframe viewport fields.
- `CameraControl3D`: composable pan, wheel-zoom, middle-drag orbit, and return-spring state.
- `Object3D`: validated object/mesh handles plus mirrored position, rotation, scale, color, opacity, and visibility values.
- `Texture3D`: dimensions, usage, requested filter/wrap/anisotropy, effective anisotropy, and mip count.
- `Material3D`: simple/PBR kind, texture bindings, alpha/culling state, and mirrored factors.
- `Model3D`: validated model handle plus part, material, format-version, vertex, index, texture-reference, and model-owned PBR resource counts.

The legacy wireframe types and limits remain source compatible.

## `Smile.Simple3D.Math3D`

- `Vector`, `Add`, `Subtract`, `MultiplyScalar`, `Dot`, `Cross`, `Length`, `Normalize`, and `Distance`.
- `Identity`, `Translation`, `Scale`, `RotationX`, `RotationY`, `RotationZ`, and `Multiply`.
- `TransformPoint`, `Perspective`, and `LookAt`.

Normalization returns a vector with length `FIXED_ONE`; normalizing zero returns zero. Matrix operations are deterministic integer operations shared by Windows and Web.

## `Smile.Simple3D.Graphics3D`

Availability and lifecycle:

- `RendererAvailable()`
- `LastError()`
- `ResetRenderer3D()`
- `LiveMeshCount3D()` and `LiveObjectCount3D()`
- `MaximumMeshCount3D()` and `MaximumObjectCount3D()`
- `DrawCallCount3D()` and `SubmittedTriangleCount3D()` for the current or most recently ended 3D frame; a successful new `Begin3D` resets both to zero
- `PbrDrawCount3D()`, `SimpleDrawCount3D()`, and `PbrTriangleCount3D()`
- `PbrShaderAvailable3D()`
- `MeshHandleValid3D(Mesh)` and `ObjectHandleValid3D(Object)`
- `MeshReferenceCount3D(Mesh)`
- `DestroyObject3D(ByRef Object)` for an object and its owned mesh
- `DestroyObjectInstance3D(ByRef Object)` for an instance using a shared mesh

Camera and frame:

- `DefaultCamera()`
- `Begin3D(Camera, Red, Green, Blue)`
- `DrawObject3D(Object)`
- `End3D()`

Primitive objects:

- `CreateCube3D(Size)`
- `CreatePlane3D(Width, Depth)`
- `CreatePyramid3D(Size, Height)`
- `CreateSphere3D(Radius)`
- `CreateCylinder3D(Radius, Height)`
- `CreateTorus3D(MajorRadius, MinorRadius)` and `CreateDonut3D(...)`

Custom indexed meshes:

- `CreateMesh3D(VertexCount, IndexCount)`
- `SetMeshVertex3D(Mesh, Index, X, Y, Z)`
- `SetMeshTriangle3D(Mesh, TriangleIndex, A, B, C)`
- `CommitMesh3D(Mesh)`
- `CreateObjectFromMesh3D(Mesh)`
- `MeshVertexCount3D(Mesh)` and `MeshIndexCount3D(Mesh)`

Offline static models:

- `LoadModel3D(Path)`, `DestroyModel3D(ByRef Model)`, and `CreateModelPart3D(Model, PartIndex)`
- `ModelPartMaterial3D`, `ModelHandleValid3D`, `LiveModelCount3D`, and `MaximumModelCount3D`
- `ModelTangentHandednessCount3D(Model, Handedness)`
- `ModelMaterialValue3D(Model, MaterialIndex, Property)` using the public `MODEL_MATERIAL_*` property constants; finite factors are returned in thousandths, texture references are one-based with zero meaning absent, and name hashes are unsigned FNV-1a values
- `ModelTextureValue3D(Model, TextureIndex, Property)` using `MODEL_TEXTURE_SEMANTIC` or `MODEL_TEXTURE_PATH_HASH`
- `ModelBoundsValue3D(Model, PartIndex, Component)` using part `-1` for model bounds and the public `MODEL_BOUNDS_*` components; values are returned in thousandths
- `ModelPartNameHash3D` and `ModelNameHash3D`

SM3D v1 remains supported. Loading an SM3D v2 model atomically resolves its exact declared texture paths, creates model-owned PBR textures/materials, and assigns each imported material to its model-part object. `ModelPartUsesPbr3D` reports this assignment without exposing model-owned handles. Clearing an explicit override on an imported part restores its imported material. Model destruction refuses while a part object is live and returns every owned mesh, material, texture, and image reference on success.

PBR textures:

- `LoadTexture3DEx(Path, Usage, Filter, Wrap, Anisotropy)` creates an explicitly classified PBR texture.
- `TEXTURE_USAGE_COLOR` selects sRGB sampling for base-color/emissive data; `TEXTURE_USAGE_DATA` selects linear sampling for normal/ORM data.
- `TEXTURE_FILTER_NEAREST`, `TEXTURE_FILTER_LINEAR`, `TEXTURE_FILTER_MIP_LINEAR`, and `TEXTURE_FILTER_ANISOTROPIC` are `0` through `3`.
- Anisotropy is requested from `1` through `16`; `Texture3D.EffectiveAnisotropy` reports the hardware result and falls back to `1` with mip-linear filtering when anisotropy is unavailable.
- `PBR_TEXTURE_*` property constants query usage, filter, wrap, requested/effective anisotropy, and mip count.

PBR materials:

- `CreatePbrMaterial3D(Base, Normal, Orm, Emissive, AlphaMode, DoubleSided)` accepts zero-handle textures for neutral no-map defaults.
- `SetPbrMaterialTextures3D` rebinds all four maps and alpha/culling state.
- `SetPbrMaterialFactors3D(Material, Red, Green, Blue, Opacity, Metallic, Roughness, NormalStrength, OcclusionStrength, Cutout)` uses RGB `0`–`255`, opacity/metallic/roughness/occlusion/cutout percentages `0`–`100`, and normal strength `0`–`400` percent.
- `SetPbrMaterialEmissive3D` uses independent linear RGB percentages from `0` through `400`.
- `MaterialKind3D` returns `MATERIAL_KIND_SIMPLE` or `MATERIAL_KIND_PBR`; `PbrMaterialValue3D` exposes the documented integer/thousandths diagnostics.

PBR lighting:

- `ResetLights3D` restores ambient white at 25%, directional white at 100%, and disables all four local slots.
- `SetAmbientLight3D` and `SetDirectionalLight3D` accept RGB `0`–`255`; light intensity is a percentage (`0`–`100` ambient, `0`–`1600` directional/local).
- `SetPointLight3D` and `SetSpotLight3D` configure fixed slots `0`–`3`; positions/ranges are integer world units.
- `SetSpotLightCone3D` supplies a nonzero direction plus inner/outer degrees from `1` through `89` with inner no greater than outer.
- `DisableLight3D`, `ClearAdditionalLights3D`, `ActiveLightCount3D`, and `LightValue3D` provide bounded lifecycle and deterministic diagnostics. Normalized direction and intensity queries use thousandths.

Transforms and appearance:

- `SetObjectPosition` and `MoveObject`
- `SetObjectRotation` and `RotateObject`
- `SetObjectScale`
- `SetObjectColor`
- `SetObjectOpacity`
- `SetObjectVisible`

## `Smile.Simple3D.Interaction`

The standard 3D camera-control contract is renderer-independent and deterministic:

- `ResetCameraControls`
- `UpdatePanZoomControls` for simultaneous primary-drag pan and wheel zoom
- `UpdateOrbitControls` for independent middle-drag orbit
- `UpdateCameraControlsFromPointer` for the conventional primary/middle/wheel binding
- `AdvanceCameraControls` for the bounded slow return spring, including paused scenes
- `ApplyCameraControls` to compose an override over an authored `Camera3D` without drift
- `CameraControlsDragging` and `CameraControlsActive`

Games decide whether a press started on valid world geometry and pass that decision through `AllowPanStart` or `AllowOrbitStart`. Once accepted, the gesture retains capture until release; a missing Web-canvas release is recovered when the button is no longer held. Pan, zoom, and orbit remain mutually composable.

## Renderer contract

Windows uses D3D11 indexed triangle lists, generated normals, model/view/perspective matrices, a resize-aware D24S8 depth buffer, and hardware-selected 4x/2x/1x multisample anti-aliasing before the existing Direct2D HUD pass. Web uses an antialiased offscreen WebGL2 canvas with the same indexed mesh and depth contract, then composites it into the Canvas 2D back buffer before ordinary 2D drawing.

Both backends bound live data to 128 meshes, 512 objects, 64 models, 128 textures, 128 materials, 64 skeletons, 128 clips, and 128 animators, and reject stale or deleted handles. Mesh destruction is rejected while a live object still references that mesh. Meshes support at most 65,535 vertices and 196,608 indices. One SM3D v2 model may own up to 16 part meshes, 64 imported materials, and 128 imported textures, subject to those same global pools.

An object returned by a primitive creator or chosen as the owner of a custom mesh must outlive every shared instance. Destroy shared instances with `DestroyObjectInstance3D` before destroying the owning object with `DestroyObject3D`. `ResetRenderer3D` is the scene/battle ownership boundary and invalidates every outstanding Renderer3D handle without changing Renderer2D state.

## Legacy wireframe modules

`FixedMath`, `Mesh`, `Primitives`, `Renderer`, and the original `OrbitState3D` interaction calls remain source compatible with the deterministic wireframe examples, GDI builds, and Space Wars. `Interaction` also exposes the reusable true-3D camera-control contract above.
