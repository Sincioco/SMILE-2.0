# Smile.Simple3D 2.0 API

Public 3D positions and sizes use integer world units. Rotations use integer degrees, scales and opacity use percentages, and `Matrix4` values use `Core.FIXED_ONE` fixed point. A zero handle means failure.

## `Smile.Simple3D.Core`

True-3D types:

- `Vector3`: `X`, `Y`, and `Z`.
- `Matrix4`: `M11` through `M44`, scaled by `FIXED_ONE`.
- `Camera3D`: position, target, explicit up direction, projection fields, near/far planes, FOV, and the legacy wireframe viewport fields. `Graphics3D.DefaultCamera()` supplies world-up; `Interaction.ApplyCameraControls` rotates position and up together for continuous 360-degree vertical orbit.
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
- `BeginSubmissionGroup3D(Capacity)` returns a frame-serial token for one bounded nonnested atomic group
- `CommitSubmissionGroup3D(Token)` publishes that group; `RollbackSubmissionGroup3D(Token)` releases it
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
- `ModelClipName3D`, `ModelClipDuration3D`, `ModelClipSampleRate3D`, `ModelClipSampleCount3D`, `ModelClipLoopRecommended3D`, and `ModelClipEventCount3D` inspect one zero-based clip. Names are returned as exact bounded UTF-8 text rather than hashes.
- `ModelSocketName3D` and `ModelSocketNodeIndex3D` enumerate one zero-based socket. `ModelAnimationEventName3D`, `ModelAnimationEventClipIndex3D`, `ModelAnimationEventTime3D`, and `ModelAnimationEventValue3D` enumerate one one-based event; zero remains the no-event sentinel.
- `ModelClipIndex3D` and `ModelSocketIndex3D` perform exact case-sensitive name lookup. `ModelEventNameMatches3D` compares the exact event name; hashes are not identity.
- `ModelAnimationEventValue3D` accepts a one-based event index returned by an animator; zero remains “no event.”
- `CreateModelAnimator3D(Model)` creates independent playback state. `SetObjectAnimator3D` binds it to a compatible model part; `ClearObjectAnimator3D` releases that object dependency.
- `PlayModelAnimator3D`/`PlayModelAnimatorNamed3D` select zero-based/exact-named clips, `ANIMATION_LOOP`, `ANIMATION_ONCE`, or `ANIMATION_HOLD`, and speed `1`–`1000` percent.
- `CrossFadeModelAnimator3D`/`CrossFadeModelAnimatorNamed3D` provide one base-layer fade. `AnimatorClipIndex3D` and `AnimatorFadePercent3D` expose its state.
- `AnimatorPendingEventCount3D`, `TakeAnimatorEvent3D`, and `TakeAnimatorEventNamed3D` consume the bounded chronological FIFO. The queue holds 32 entries and reports error 49 when a multi-wrap update has more events.
- `SetModelAnimatorTime3D` seeks the active clip to an exact in-range millisecond. A successful seek cancels a fade, clears pending/overflow/dropped events and root delta, evaluates the pose immediately, and does not synthesize events. Invalid or inactive seeks fail without changing animator state.
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

M5 shadows and post-processing:

- `ConfigurePostProcessing3D` selects direct LDR or HDR/post rendering, bloom, exposure `25`-`400` percent, threshold `500`-`8000` thousandths, intensity `0`-`400` percent, half/quarter downsampling, zero to two blur cycles, and requested samples `1`, `2`, or `4`.
- `ConfigureShadows3D` selects no caster, the directional light, or one spot slot; resolutions are exactly `1024` or `2048`, constant bias is `0`-`1000` millionths, and normal bias is `0`-`1000` hundred-thousandths.
- `SetDirectionalShadowArea3D` configures the directional center, width, height, near, and far bounds. `SetObjectShadowsChecked3D` transactionally updates the mirrored cast/receive flags; `SetObjectShadows3D` is the compatibility subroutine wrapper.
- `M5Value3D` exposes logical/physical/rejected submissions, provisional/reserved group entries, palette snapshots, in-flight mesh/texture references, snapshot bytes, shadow/HDR/bloom requested and effective state, scene format, effective samples, pass counters, target dimensions/bytes, caster/bias state, per-object cast/receive state, and read-only captured-submission probes through the public `M5_QUERY_*` constants.
- A frame accepts at most 512 physical snapshots and 512 palette snapshots. Each accepted draw captures its transform, color/opacity, visibility/shadow flags, material factors/textures/alpha/culling state, mesh, and animator pose revision. Duplicate submissions of one object therefore remain independent even when the source object, material, or animator changes or is destroyed before `End3D`.
- Submission groups reserve conservatively, reject nesting and stale tokens with Renderer3D error 52, and publish only on commit. An open group at `End3DChecked` is rolled back, releases its references, and fails the end with error 52. Queue/palette overflow remains error 51. Mesh mutation or recommit while a snapshot is in flight is refused with error 53.
- `M5_FALLBACK_*` is an independent bit field: shadow resolution `1`, shadow disabled `2`, HDR unavailable `4`, MSAA reduced `8`, bloom resolution reduced `16`, bloom disabled `32`, tone mapping disabled `64`, and direct LDR `128`.

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

Yaw and pitch remain unbounded while an orbit gesture is held, then normalize on release. `ApplyCameraControls` rotates the complete camera offset around X and Y, so middle-drag orbit supports full horizontal and vertical revolutions instead of treating pitch as a linear height offset. Native and Web look-at paths select a pole-safe alternate up vector near vertical views.

## `Smile.Simple3D.CharacterViewer`

`Profile` supplies the bounded, data-driven inputs for a reusable character inspector. `AutoFit` derives scale, centering, camera, an enlarged inspection floor, pan, and shadow framing from immutable model bounds. `GroundOffset` is an optional bounded fitted-world correction for assets whose static bind AABB does not share the animated sole plane; it should be measured per asset and must not be used to erase authored root motion.

`ZoomState`, `InitializeZoom`, `AdjustZoomTarget`, and `AdvanceZoom` provide frame-rate-independent bounded zoom easing. `RetainedPointerDelta` preserves fractional pointer movement so slow pan and orbit gestures remain smooth at integer-world scale.

`CursorPointAtDepth` resolves a logical pointer position on a camera-facing plane at an explicit world depth. `KeepCursorAnchor` translates a newly orbited camera so that world point remains at its original logical pixel. These are reusable projection helpers, not mesh raycasts; an inspector can choose the selected joint's depth without changing the pose or snapping the camera on mouse-down.

## `Smile.Simple3D.Effects3D`

`EffectPreset` defines bounded deterministic particle, ribbon, flash, shake, light, audio, and composite-emitter presentation data. `RibbonPointCount`, `RibbonWidth`, and `RibbonRadius` control a zero-particle ribbon independently: radius `0` preserves the legacy 170-world-unit curve, while an explicit value from `1` through `100,000` calibrates the curve to the scene or subject scale. Effects may follow a `Character3D` socket through `SpawnAtSocket` and `MoveToSocket`; caller-owned light and audio requests remain presentation-only and never authorize gameplay damage.

## `Smile.Simple3D.Scene3D`

Quality profiles keep asset and render policy separate. `QUALITY_LOW` uses linear filtering/anisotropy 1 and direct LDR with no shadow or bloom. `QUALITY_MEDIUM` uses mip-linear/anisotropy 4, HDR tone mapping, a 1024 shadow, quarter-resolution one-cycle bloom, and requests 2x samples. `QUALITY_HIGH` uses anisotropic filtering requested at 8, HDR tone mapping, a 2048 shadow, half-resolution two-cycle bloom, and requests 4x samples. `QUALITY_AUTO` selects High when PBR is available and Low with `SCENE_FALLBACK_PBR_UNAVAILABLE` otherwise. Existing actors are never silently rebuilt when render settings change; `AssetProfileKey` and `RenderProfileKey` make that distinction explicit.

- `SetQuality`, `RequestedQuality`, `EffectiveQuality`, `RefreshCapabilities`, `PbrAvailable`, `LastFallback`, and `FallbackFlags`.
- `TextureFilter`, `TextureWrap`, `RequestedAnisotropy`, `PbrPreferred`, `SimpleFallbackAllowed`, `AssetProfileKey`, and `RenderProfileKey` are deterministic read-only policy helpers.
- `SetShadows`, `SetShadowCaster`, `SetShadowArea`, `SetShadowBias`, `ShadowsRequested`, `ShadowsEffective`, and `ShadowResolution` control the one-caster shadow profile while the scene is closed.
- `SetHdr`, `SetExposure`, `SetBloom`, `SetPostProcessing`, their requested/effective queries, `EffectiveSampleCount`, and `SceneFormat` control or inspect post-processing while preserving the direct-LDR fallback.
- `FeatureAvailable` and `RefreshRenderCapabilities` expose actual renderer results. `FallbackFlags` maps every independent Renderer3D fallback to the public `SCENE_FALLBACK_FLAG_*` bits.
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

- `Place`, `Rotate`, `SetScale`, `SetVisible`, and yaw-only `LookAt` update every model part transactionally. Positions are bounded to -1,000,000 through 1,000,000, rotation input to the same safe integer range and normalized to 0-359 degrees, and uniform scale to 1-25,000 percent. The upper range supports meter-scale imported characters in higher-precision integer-world scenes and established worlds whose procedural actors are larger than 100 units.
- Camera-driven programs should preserve partial pointer deltas, use a scene scale large enough to avoid visible integer-coordinate quantization, and ease bounded zoom targets over multiple frames. Pan, zoom, orbit, and rotation should not snap by a complete input step in one frame.
- `SetShadows(ByRef Actor, CastsShadow, ReceivesShadow)`, `CastsShadow`, and `ReceivesShadow` update or inspect every part transactionally. Partial renderer refusal restores all accepted parts or quarantines the actor if rollback cannot be proven.
- `PlayAnimation`, `PlayMode`, `CrossFade`, `StopAnimation`, `Update`, `IsPlaying`, `AnimationComplete`, and `CurrentClipNameMatches` use exact case-sensitive clip names. `AnimationTime` reports the live millisecond and `SetAnimationTime` provides the same deterministic, event-suppressing seek contract as `SetModelAnimatorTime3D`.
- `SetRootMotion` accepts `ROOT_MOTION_IGNORE` or `ROOT_MOTION_APPLY`. Apply mode drains the combined low-level model-space delta once per update, rotates translation into world space using the actor's pre-update yaw, then applies root yaw. Position/yaw subunits remain in thousandths.
- `LastRootDelta`, `PositionX`, `PositionY`, `PositionZ`, and `RotationY` expose deterministic actor motion diagnostics.

Events, sockets, drawing, and diagnostics:

- `TakeEvent`, `PendingEventCount`, `EventOverflowed`, `DroppedEventCount`, and `ClearEvents` preserve the bounded chronological animator FIFO.
- `ClipName`, `ClipDuration`, `ClipSampleRate`, `ClipSampleCount`, `ClipLoopRecommended`, and `ClipEventCount` enumerate immutable clip metadata from the actor-owned model. `EventCount`, `EventName`, `EventClipIndex`, `EventTime`, and `EventValue` enumerate one-based authored events.
- `HasSocket`, `SocketName`, `SocketNodeIndex`, `SocketPosition`, and `SocketValueThousandths` enumerate sockets and use the primary bound part for world-space socket evaluation.
- `Draw`, `IsValid`, `PartCount`, local `BoundsValueThousandths`/`Height`, and explicit `LocalBounds`, `WorldBounds`, `WorldBoundsValueThousandths`, `WorldHeight`, `WorldCenter`, and `WorldRadius`. `WorldBounds` transforms all eight static AABB corners and accepts an optional positive animation margin; it is conservative static geometry, not exact skinned bounds.
- `SetPartVisible` provides bounded per-part visibility for editor and equipment workflows. `PrimaryObjectHandle`, indexed `PartObjectHandle`, `AnimatorHandle`, and `ModelHandle` are borrowed read-only advanced Battle3D interop values. Callers must not destroy them or mutate Character3D-owned transforms. Character3D does not depend on Battle3D.
- `LiveActorCount`, `MaximumActorCount`, `CachedAssetCount`, `PendingReleaseAssetCount`, `AssetReferenceCount`, `ActorAssetState`, `ActorAssetVariant`, `ActorAssetProfileKey`, `ActorUsesPbr`, `ActorUsesFallback`, `AnimationResidentBytes`, and `CachedAnimationResidentBytes`.
- `LastError`, `LastRendererError`, `LastCleanupRendererError`, `LastFallback`, actor-specific error/renderer/fallback queries, and `ClearError`.

`Load`, `Play`, `Stop`, and `End` are reserved SMILE keywords and cannot currently be routine identifiers. M4 therefore uses the explicit source-level names `LoadActor`, `PlayAnimation`, `StopAnimation`, and `EndScene` without changing the language grammar.

## Renderer contract

Windows direct LDR uses D3D11 indexed triangle lists and a resize-aware D24S8 target. M5 HDR uses `R16G16B16A16_FLOAT`, hardware-selected 4x/2x/1x MSAA plus resolve, an optional D32 shadow map, ACES-fitted tone mapping, and bounded bloom before the existing Direct2D HUD pass. Web checks `EXT_color_buffer_float` and framebuffer completeness before using `RGBA16F`; it stays single-sample and records an MSAA fallback. Both backends finish all 3D post work before ordinary Renderer2D drawing and preserve the exact direct-LDR path when M5 is disabled or unavailable.

Both backends bound live data to 128 meshes, 1,024 objects, 64 models, 128 textures, 128 materials, 64 legacy skeletons, 128 legacy clips, and 128 total animators, and reject stale or deleted handles. Each frame additionally has 512 fixed tagged submission snapshots, 512 fixed palette snapshots, one shadow target, one HDR scene target, and at most two bloom targets. Accepted snapshots retain mesh and distinct texture in-flight references; destruction or mutation that would invalidate those resources is refused until commit rollback or frame end releases them. Meshes support at most 65,535 vertices and 196,608 indices. One SM3D v2 model may contain up to 16 part meshes, 64 imported materials, 128 metadata texture references, 256 animation nodes, 128 production bones, 64 imported clips, 64 events per clip, and 64 sockets; its complete file remains at most 16 MiB, and its deduplicated owned textures/materials must fit the global pools.

The supported PBR production transform profile uses positive, nonsingular object scale. A singular or mirrored PBR object draw is rejected before submission with error 46, leaving draw/triangle counters unchanged; the simple path retains its existing behavior. Two-key clips may use nonuniform bone scale on simple materials. A PBR draw using a clip with any nonuniform scale key is rejected with error 45; uniform animation scale remains supported.

An object returned by a primitive creator or chosen as the owner of a custom mesh must outlive every shared instance. Destroy shared instances with `DestroyObjectInstance3D` before destroying the owning object with `DestroyObject3D`. `ResetRenderer3D` is the scene/battle ownership boundary and invalidates every outstanding Renderer3D handle without changing Renderer2D state.

## Legacy wireframe modules

`FixedMath`, `Mesh`, `Primitives`, `Renderer`, and the original `OrbitState3D` interaction calls remain source compatible with the deterministic wireframe examples, GDI builds, and Space Wars. `Interaction` also exposes the reusable true-3D camera-control contract above.

## `Smile.Simple3D.NodeAim3D`

Bounded additive joint aiming uses the existing animated socket matrices and node rotation offsets. `Configure(ByRef Constraint, Actor, SocketName, LocalForward, Limits)` selects one joint through a socket with no extra authored rotation. Limits are maximum X/Y/Z angles in degrees (0..90); zero locks an axis. `Update(ByRef Constraint, ByRef Actor, Target, Elapsed, Enabled)` aims after clip sampling and actor placement, before reading attached effects. It reads the unmodified animated joint basis every frame, eases toward the bounded correction, and blends back to the clip when disabled. It never changes the actor's position or body rotation. `Solve` and `Direction` expose the same bounded local-space math for inspection. This is a source-library helper, not IK or new language syntax.

## `Smile.Simple3D.Arena3D`

`Create(Width, Depth, RedValue, GreenValue, BlueValue, Optional TileSize = 40, Optional LineThickness = 2) As ArenaFloor` creates a black floor plus one emissive grid mesh. All dimensions are integer world units. `ArenaFloor.Ready` reports success; `LineCount` reports the actual interior strips. Both Character Viewer and Fire Lab use the same geometry/material recipe (blue versus orange). Arin's arena remains 1000 by 1000 when the dragon is hidden.

`Draw(ByRef ArenaFloor, ShowFloor, ShowGrid) As Boolean` supports independent visibility. `Destroy(ByRef ArenaFloor)` releases both meshes/objects and the grid material, is repeat-safe, and ignores stale resources after a renderer reset. Create/destroy are rejected while a 3D frame is active. Create rejects dimensions outside 40..100000, tile size below 2, thickness outside 1..TileSize-1, and more than 512 requested tiles on either axis. Partial creation rolls back; no per-frame allocation or per-line draw calls.

Camera settings are separate: `Interaction.ResetCameraControls` initializes manual offsets and `CharacterViewer.AdvanceOrbitYaw1000(ByRef Angle1000, ElapsedMilliseconds, DegreesPerSecond)` provides smooth horizontal auto-orbit. Applications select a starting yaw/pitch, target, distance, and whether automatic motion is enabled. The arena does not own a camera or timer.

## `Smile.Simple3D.FireEmitter3D`

Bounded persistent native thermal fire with an explicit simpler CPU fallback. Initialize with `Initialize("Assets/Fire")`, then `StartAt(Preset, Position, Seed, Optional Quality = 3, Optional Simulation = 3, Optional Radius = 20)`. Presets: TorchFire, BrazierFire, LineFire, PaladinBladeFire, Fireball, FireBurstGen3, DragonFireBreathPreview. `StartOnSegment` uses an actor's base/tip sockets; `SetSegment` supplies a moving world-space segment directly.

Use `Update(ElapsedMilliseconds)` before Begin3D and `Draw(Emitter)` inside the frame. `SetPosition`, `SetDirection`, `SetWind`, `SetTurbulence`, `SetIntensity`, `SetEnabled`, and `SetSourceVelocityInheritance` change bounded emitter settings. `StopEmitter` stops spawning while tails finish; `Destroy` releases one logical emitter; `Shutdown` releases the shared assets. Never mutate an in-flight emitter.

Quality Low/Medium/High = 1/2/3. Simulation CPU/GPU/Auto = 1/2/3. Six logical emitters maximum. High reserves five GPU systems (1664 slots); Medium uses five systems and 832 slots. The shared renderer admits at most 32 GPU systems / 32,768 total slots, and 32 CPU particle batches / 8,192 staged particles. Admission checks the complete five-system and slot budget before creation; partial creation rolls back. High may retry Medium, then CPU Low (four 96-slot batches). Fallback reasons distinguish unavailable GPU (1), exhausted systems/batches (2), exhausted particle slots (3), and creation failure (4).

All characters/effect families share those limits. Arin's sword plus three shield-edge emitters use 20 systems; a dragon emitter brings that to 25, leaving seven system slots for other effects. This is a bounded capacity extension preserving the approved five-layer look, not shared render views or an unbounded pool. Ice/magic presets are future work; their low-level resources already participate in admission. The native check covers High sword + Medium impact + two Medium torches + High dragon, plus unrelated resource pressure and complete fallback/teardown.

Query requested/effective backend and quality separately with `Value`. CPU Low intentionally lacks GPU thermal/turbulence and heat-distortion parity; the high-level emitter still uses CPU Low on Web. Low-level WebGL2 retains its existing GPU path with the same 32-system shared limit. `GPU_PARTICLE_QUERY_MAX_TOTAL_CAPACITY` (60) exposes the existing 32,768-slot ceiling without changing any command ID. Bounds follow current/recent segment positions with radius/lifetime/velocity padding, rather than covering the entire world. Zero elapsed time does not advance or consume source history.

Native thermal dynamics expose wind, gravity, buoyancy, dt-aware drag, seeded 1/2-octave turbulence, cooling, dissipation, size evolution, speed limits and kill bounds through the Graphics3D GPU-particle setters. Particle simulation has no GPU state readback. Heat distortion now retains supported 4x/2x/1x scene MSAA in HDR and LDR: resolve the opaque snapshot, distort it, copy color back without tone mapping, preserve MSAA depth for transparent draws, then perform normal final resolve/post-processing.

## `Smile.Simple3D.StaticBackdrop3D`

Shared screen-fixed image backgrounds for Character Viewer and Fire Lab; no camera-facing plane, world transform, per-frame image upload, or orbit-dependent UV calculations. The existing native renderer draws the backdrop behind the scene, then the existing post-processing and Renderer2D HUD run normally. Local heat refraction can affect pixels near a fire; it does not move the background with the camera. Web backdrop rendering remains deferred: selection returns `False` on that backend, leaving the clear color visible.

- `Create(Path) As Backdrop` loads one published image; check `Backdrop.Ready`.
- `SetActive(Backdrop) As Boolean` selects it before `Begin3D`. Selection persists across frames.
- `ClearActive() As Boolean` returns to the scene's clear color without unloading images.
- `Destroy(ByRef Backdrop)` clears this backdrop if selected, releases its texture, and empties the value. Destroying a different inactive backdrop does not clear the selected one.

Creation, selection, clearing, and destruction are refused inside an active 3D frame. Renderer-epoch checks reject stale backgrounds after reset. Callers own the returned value and must not duplicate ownership or mix direct low-level backdrop selection with this module. Reload after `ResetRenderer3D`. This is a source-library addition, not new language syntax or an ABI extension.
