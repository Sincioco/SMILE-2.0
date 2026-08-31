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
- `Model3D`: validated model handle plus part, material, format-version, vertex, index, metadata texture-reference, geometry/PBR readiness, PBR failure, model-owned PBR resource counts, and imported animation/bone/clip/socket/byte counts.
- `Animator3D`: independent legacy or model-owned playback state, including model/clip, mode, speed, and root-motion policy.
- `RootMotionDelta3D`: one atomically drained XYZ/yaw result in thousandths.

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
- `ResourceEpoch3D()` returns the nonzero logical resource epoch; each successful explicit reset advances it once, while device/context recreation does not
- `FrameActive3D()` reports the renderer's actual logical frame state
- `LiveMeshCount3D()` and `LiveObjectCount3D()`
- `MaximumMeshCount3D()` and `MaximumObjectCount3D()`
- `LiveModelCount3D()`, `LiveAnimatorCount3D()`, and their fixed maximum-count queries
- `DrawCallCount3D()` and `SubmittedTriangleCount3D()` for the current or most recently ended 3D frame; a successful new `Begin3D` resets both to zero
- `PbrDrawCount3D()`, `SimpleDrawCount3D()`, and `PbrTriangleCount3D()`
- `PbrShaderAvailable3D()`
- `PbrPipelineState3D()`, `PbrPipelineFailure3D()`, and `PbrPipelineAttemptCount3D()` inspect the cached generation state without causing another compile attempt
- `MeshHandleValid3D(Mesh)` and `ObjectHandleValid3D(Object)`
- `MeshReferenceCount3D(Mesh)`
- `DestroyObject3D(ByRef Object)` for an object and its owned mesh
- `DestroyObjectInstance3D(ByRef Object)` for an instance using a shared mesh
- `TryDestroyObjectInstance3D`, `TryDestroyAnimator3D`, `TryDestroyModel3D`, and `TryDestroyMaterial3D` preserve public records when dependency-aware destruction is refused
- `SetObjectPositionChecked3D`, `SetObjectRotationChecked3D`, `SetObjectScaleChecked3D`, and `SetObjectVisibleChecked3D` update mirrored records only after renderer acceptance

Camera and frame:

- `DefaultCamera()`
- `Begin3D(Camera, Red, Green, Blue)`
- `DrawObject3D(Object)`
- `End3D()`
- `End3DChecked()`

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

- `LoadModel3D(Path)` keeps the atomic all-in-one geometry-plus-PBR behavior
- `LoadModelGeometry3D(Path)` publishes validated v1/v2 geometry and metadata without resolving any PBR image
- `PrepareModelPbr3D(ByRef Model, Filter, Wrap, Anisotropy)` atomically prepares a geometry-only SM3D v2 model
- `DestroyModel3D(ByRef Model)` and `CreateModelPart3D(Model, PartIndex)`
- `ModelPartMaterial3D`, `ModelHandleValid3D`, `LiveModelCount3D`, and `MaximumModelCount3D`
- `ModelGeometryReady3D`, `ModelPbrFailure3D`, and `ModelPartUsesPbr3D`
- `ModelTangentHandednessCount3D(Model, Handedness)`
- `ModelMaterialValue3D(Model, MaterialIndex, Property)` using the public `MODEL_MATERIAL_*` property constants; finite factors are returned in thousandths, texture references are one-based with zero meaning absent, and name hashes are unsigned FNV-1a values
- `ModelTextureValue3D(Model, TextureIndex, Property)` using `MODEL_TEXTURE_SEMANTIC` or `MODEL_TEXTURE_PATH_HASH`
- `ModelBoundsValue3D(Model, PartIndex, Component)` using part `-1` for model bounds and the public `MODEL_BOUNDS_*` components; values are returned in thousandths
- `ModelPartNameHash3D` and `ModelNameHash3D`

SM3D v1 remains supported. `LoadModel3D` loads geometry, resolves exact declared texture paths, creates model-owned PBR textures/materials, and returns zero unless the complete operation succeeds. `LoadModelGeometry3D` is the reusable fallback path: geometry remains valid even when `PrepareModelPbr3D` later fails. Preparation preflights unique texture identities and publishes all textures/materials at once; failure leaves the model handle, meshes, and prior live counts unchanged and records the reason in `Model3D.PbrFailure` and `LastError()`.

Prepare a model before creating its part objects. Objects created before preparation retain their simple/default-zero material; objects created afterward receive the imported PBR default. Texture metadata references remain separate, but exact references with the same path, color/data usage, filter, wrap, anisotropy, and mip policy share one model-owned texture. Clearing an explicit override on a prepared imported part restores its imported material. Model destruction refuses while a part object is live and returns every owned mesh, material, unique texture, and image reference on success.

Imported SM3D v2 animation:

- `ModelHasAnimation3D`, `ModelBoneCount3D`, `ModelClipCount3D`, `ModelSocketCount3D`, `ModelAnimationBytes3D`, `ModelAnimationNodeCount3D`, and `ModelAnimationEventCount3D` expose immutable model metadata.
- `ModelClipDuration3D` and `ModelClipSampleRate3D` inspect one zero-based clip.
- `ModelClipIndex3D` and `ModelSocketIndex3D` perform exact case-sensitive name lookup. `ModelEventNameMatches3D` compares the exact event name; hashes are not identity.
- `ModelAnimationEventValue3D` accepts a one-based event index returned by an animator; zero remains “no event.”
- `CreateModelAnimator3D(Model)` creates independent playback state. `SetObjectAnimator3D` binds it to a compatible model part; `ClearObjectAnimator3D` releases that object dependency.
- `PlayModelAnimator3D`/`PlayModelAnimatorNamed3D` select zero-based/exact-named clips, `ANIMATION_LOOP`, `ANIMATION_ONCE`, or `ANIMATION_HOLD`, and speed `1`–`1000` percent.
- `CrossFadeModelAnimator3D`/`CrossFadeModelAnimatorNamed3D` provide one base-layer fade. `AnimatorClipIndex3D` and `AnimatorFadePercent3D` expose its state.
- `AnimatorPendingEventCount3D`, `TakeAnimatorEvent3D`, and `TakeAnimatorEventNamed3D` consume the bounded chronological FIFO. The queue holds 32 entries and reports error 49 when a multi-wrap update has more events.
- `SetAnimatorRootMotion3D` selects `ROOT_MOTION_NONE` or `ROOT_MOTION_EXTRACT`. `TakeAnimatorRootDelta3D` returns one atomic XYZ/yaw result; translation and degrees are scaled by 1000.
- `AnimatorSocketValue3D` queries animated model space. `AnimatorSocketWorldValue3D` additionally applies the bound object's position, rotation, and scale. Position and 3-by-3 orientation properties use thousandths.
- `ModelAnimationAvailable3D` reports whether the production 128-bone palette transport is available. `ModelPaletteUploadCount3D` exposes cached native/Web uploads.
- `StopAnimator3D` clears playback and pending events. `DestroyAnimator3D` refuses while an object remains bound. `DestroyModel3D` refuses while a model animator or part object remains live.

The production importer accepts one skin with 1–128 bones, 1–64 clips, 1–256 retained nodes, up to 64 events per clip, and up to 64 sockets. Direct3D uses a 128-matrix vertex constant buffer; WebGL2 uses one shared RGBA32F 4-by-128 palette texture. Existing custom-mesh animation remains the separate 32-bone teaching API.

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

PBR blend draws use straight source alpha, read depth, do not write depth, and are submitted in caller order. Draw opaque/masked geometry first, then submit blended objects from farthest to nearest when overlap matters. Renderer3D does not add a hidden transparent sort.

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

## `Smile.Simple3D.Scene3D`

Quality profiles affect only newly loaded assets. `QUALITY_LOW` uses linear filtering and anisotropy 1, `QUALITY_MEDIUM` uses mip-linear filtering and anisotropy 4, and `QUALITY_HIGH` uses anisotropic filtering requested at 8. `QUALITY_AUTO` selects High when PBR is available and Low with `SCENE_FALLBACK_PBR_UNAVAILABLE` otherwise. Existing actors are never silently rebuilt when quality changes. Lighting-only changes do not affect the asset-profile key.

- `SetQuality`, `RequestedQuality`, `EffectiveQuality`, `RefreshCapabilities`, `PbrAvailable`, `LastFallback`, and `FallbackFlags`.
- `TextureFilter`, `TextureWrap`, `RequestedAnisotropy`, `PbrPreferred`, `SimpleFallbackAllowed`, and `AssetProfileKey` are deterministic read-only asset-policy helpers.
- `UseLighting`, `CurrentLightingMatches`, and `ApplyLighting` select exact built-in names: `CharacterStudio`, `Daylight`, `Dungeon`, `Moonlight`, or `EmberObservatory`.
- `UseCustomLighting` preserves advanced lights configured directly through `Graphics3D`.
- `Begin`, `EndScene`, and `IsOpen` reject nested/unmatched frames and leave Renderer2D available after the 3D pass.
- `Synchronize`, `ResetState`, and `Shutdown` reconcile Scene3D ownership with the Renderer3D epoch and actual frame state without ending a frame Scene3D does not own.
- `LastError`, `LastRendererError`, and `ClearError` expose stable high-level and low-level failure information.

## `Smile.Simple3D.Character3D`

`Actor` contains only a generation-safe `Handle`. The module owns fixed pools of 16 exact path/profile/actual-variant cache entries, 32 actors, and at most 16 model parts per actor. Request policy is admission only: equivalent Auto, Require PBR, and Allow simple fallback requests share the same prepared PBR asset, while permitted Auto/Allow requests share the same simple variant when PBR is unavailable. A cache entry normally exists only while an actor references it; a dependency-refused final release remains uniquely pending until retried.

Loading and lifecycle:

- `LoadActor(Path)` and `LoadWithPolicy(Path, Policy)`.
- `CHARACTER_LOAD_AUTO`, `CHARACTER_LOAD_REQUIRE_PBR`, and `CHARACTER_LOAD_ALLOW_SIMPLE_FALLBACK`.
- `Destroy(ByRef Actor)`, `RetryPendingReleases()`, and idempotent `Shutdown()`.
- PBR content errors remain errors. Simple fallback is allowed only for unavailable PBR capability according to policy; it does not hide a missing declared texture when PBR validation is available.

Transforms and animation:

- `Place`, `Rotate`, `SetScale`, `SetVisible`, and yaw-only `LookAt` update every model part transactionally. Positions are bounded to -1,000,000 through 1,000,000, rotation input to the same safe integer range and normalized to 0-359 degrees, and uniform scale to 1-1,000 percent.
- `PlayAnimation`, `PlayMode`, `CrossFade`, `StopAnimation`, `Update`, `IsPlaying`, `AnimationComplete`, and `CurrentClipNameMatches` use exact case-sensitive clip names.
- `SetRootMotion` accepts `ROOT_MOTION_IGNORE` or `ROOT_MOTION_APPLY`. Apply mode drains the combined low-level model-space delta once per update, rotates translation into world space using the actor's pre-update yaw, then applies root yaw. Position/yaw subunits remain in thousandths.
- `LastRootDelta`, `PositionX`, `PositionY`, `PositionZ`, and `RotationY` expose deterministic actor motion diagnostics.

Events, sockets, drawing, and diagnostics:

- `TakeEvent`, `PendingEventCount`, `EventOverflowed`, `DroppedEventCount`, and `ClearEvents` preserve the bounded chronological animator FIFO.
- `HasSocket`, `SocketPosition`, and `SocketValueThousandths` use the primary bound part for world-space socket evaluation.
- `Draw`, `IsValid`, `PartCount`, local `BoundsValueThousandths`/`Height`, and explicit `LocalBounds`, `WorldBounds`, `WorldBoundsValueThousandths`, `WorldHeight`, `WorldCenter`, and `WorldRadius`. `WorldBounds` transforms all eight static AABB corners and accepts an optional positive animation margin; it is conservative static geometry, not exact skinned bounds.
- `PrimaryObjectHandle`, indexed `PartObjectHandle`, `AnimatorHandle`, and `ModelHandle` are borrowed read-only advanced Battle3D interop values. Callers must not destroy them or mutate Character3D-owned transforms. Character3D does not depend on Battle3D.
- `LiveActorCount`, `MaximumActorCount`, `CachedAssetCount`, `PendingReleaseAssetCount`, `AssetReferenceCount`, `ActorAssetState`, `ActorAssetVariant`, `ActorAssetProfileKey`, `ActorUsesPbr`, `ActorUsesFallback`, `AnimationResidentBytes`, and `CachedAnimationResidentBytes`.
- `LastError`, `LastRendererError`, `LastCleanupRendererError`, `LastFallback`, actor-specific error/renderer/fallback queries, and `ClearError`.

`Load`, `Play`, `Stop`, and `End` are reserved SMILE keywords and cannot currently be routine identifiers. M4 therefore uses the explicit source-level names `LoadActor`, `PlayAnimation`, `StopAnimation`, and `EndScene` without changing the language grammar.

## Renderer contract

Windows uses D3D11 indexed triangle lists, generated normals, model/view/perspective matrices, a resize-aware D24S8 depth buffer, and hardware-selected 4x/2x/1x multisample anti-aliasing before the existing Direct2D HUD pass. Web uses an antialiased offscreen WebGL2 canvas with the same indexed mesh and depth contract, then composites it into the Canvas 2D back buffer before ordinary 2D drawing.

Both backends bound live data to 128 meshes, 512 objects, 64 models, 128 textures, 128 materials, 64 legacy skeletons, 128 legacy clips, and 128 total animators, and reject stale or deleted handles. Mesh destruction is rejected while a live object still references that mesh. Meshes support at most 65,535 vertices and 196,608 indices. One SM3D v2 model may contain up to 16 part meshes, 64 imported materials, 128 metadata texture references, 256 animation nodes, 128 production bones, 64 imported clips, 64 events per clip, and 64 sockets; its complete file remains at most 16 MiB, and its deduplicated owned textures/materials must fit the global pools.

The supported PBR production transform profile uses positive, nonsingular object scale. A singular or mirrored PBR object draw is rejected before submission with error 46, leaving draw/triangle counters unchanged; the simple path retains its existing behavior. Two-key clips may use nonuniform bone scale on simple materials. A PBR draw using a clip with any nonuniform scale key is rejected with error 45; uniform animation scale remains supported.

An object returned by a primitive creator or chosen as the owner of a custom mesh must outlive every shared instance. Destroy shared instances with `DestroyObjectInstance3D` before destroying the owning object with `DestroyObject3D`. `ResetRenderer3D` is the scene/battle ownership boundary and invalidates every outstanding Renderer3D handle without changing Renderer2D state.

## Legacy wireframe modules

`FixedMath`, `Mesh`, `Primitives`, `Renderer`, and the original `OrbitState3D` interaction calls remain source compatible with the deterministic wireframe examples, GDI builds, and Space Wars. `Interaction` also exposes the reusable true-3D camera-control contract above.
