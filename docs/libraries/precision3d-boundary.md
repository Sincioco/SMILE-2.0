# Fractional 3D boundary and ownership

Established before D2 edits against D1 commit `40c183d`, then updated for D3
production adoption. This map describes the implemented boundary; see the single
[Double checkpoint](../implementation/double-precision-checkpoint.md) for state.

| Route | Reviewed baseline type/unit and owner | Implemented change / exact narrowing point |
|---|---|---|
| Camera authoring | ViewerCamera Base/Live Core.Camera3D, Number world units/degrees; CharacterViewer/Interaction Number control values | Precision3D camera/vector values and a focused shared continuous controller; retain UI extents and input identities |
| Camera acceptance | Graphics3D.Begin3D sends commands 10 and 123 as Number; native pending/accepted float arrays; Web camera object | Complete typed 12-Double transaction into the same accepted state; validate before acceptance, including after float narrowing |
| Scene ownership | Scene3D Begin/End, lighting/profile/frame state | Shared preparation/completion with BeginPrecise; never call the integer camera setter after precise acceptance |
| Object transform | Core.Object3D owns exact handles; renderer object pool has float position/rotation/scale | Non-owning precise transform values target existing handles; typed atomic transform write; no second pool |
| Actor placement | Character3D actor/part pool, six Number thousandths arrays; ApplyTransformSnapshot rounds each position/angle to whole units | Replace only continuous position/rotation storage with Double world units/degrees; preserve transaction rollback, generation and per-part visibility |
| Sockets/equipment | Existing animator/node/model matrix, then rounded thousandths and Core.Vector3 whole units | Reuse the actual socket matrix and object transform; precise query returns the unrounded runtime component; legacy queries retain their units |
| Bounds/grounding | Model authored bounds in thousandths, Character3D fixed rotation/world bounds, Viewer grounding corrections | Explicit authored-data conversion; precise world bounds/placement path. Saved calibration and per-clip corrections stay integral |
| Root motion | Animator authored float delta exposed as integer thousandths, exact ms/events | Convert at the authored boundary, then compose continuous actor translation/yaw without whole-degree trig; events/times unchanged |
| Reflections | Same captured camera and immutable object snapshots; automatic receiver plane from geometry | Typed receiver transform plus unrounded effective/requested plane queries; keep commands 133–135 and fallback behavior |
| Matrix/upload | Native SmileMatrix3D/constant buffers float32; Web Float32Array uniforms | Explicit measured float32 narrowing, no whole-unit conversion. Accepted snapshots remain immutable |

Public units remain world units, degrees, percentage scale and X-then-Y-then-Z
Euler order. Existing Core fixed vectors/matrices and integer APIs remain available.
No asset scaling/rebaking, second renderer, second owning Object3D or actor pool.

The canonical internal built-ins are `Renderer3DDouble(Command As Number,
Resource As Number, A As Double, B As Double, C As Double, D As Double,
E As Double, F As Double, G As Double, H As Double, I As Double, J As Double,
K As Double, L As Double) As Number` and
`Renderer3DDoubleValue(Command As Number, Resource As Number, Index As Number,
Component As Number) As Double`. Shared analysis propagates Game Window capability.
Native external ABI uses argument-position XMM registers; handles remain integral.

The typed family has its own command namespace, without changing legacy IDs:

| Family | Command | Payload |
|---|---:|---|
| Mutation | 1 | Complete camera: position XYZ, target XYZ, up XYZ, FOV, near, far |
| Mutation | 2 | Complete object: position XYZ, rotation XYZ, scale XYZ percent; remaining slots zero |
| Mutation | 3 / 4 / 5 | Object position / rotation / scale XYZ, remaining slots zero |
| Mutation | 6 | Object pivot XYZ in world units, followed by XYZ rotation in degrees |
| Mutation | 7 | Existing point-light slot: XYZ, integral RGB components, intensity percent and range; style fields are validated before explicit conversion |
| Mutation | 10000 + Index | Existing ribbon point, Index 0–8191: left XYZ, right XYZ, U in 0–1 |
| Mutation | 20000 + AtlasFrame × 4096 + Index | Existing particle instance, Index 0–4095 and AtlasFrame 0–255: XYZ, size, rotation degrees |
| Mutation | 2000000 + Slot | Existing GPU particle spawn staging, Slot 0–32767: XYZ, velocity XYZ in world units/second |
| Query | 1 | Accepted camera, component 0–11 |
| Query | 2 | Live object transform, component 0–8 (scale in percent) |
| Query | 3 | Actual object/animator socket, Index is socket index; components 0–2 position or 3–5 position ignoring additive node offsets; 6–14 matrix basis (X/Y/Z columns), 15–23 basis ignoring node offsets |
| Query | 4 | Effective plane component 0, requested plane component 1 |
| Query | 5 | Captured object transform, Index is submission slot, component 0–8 |
| Query | 6 | Staged ribbon point, Index is point index; components 0–2 left and 3–5 right |
| Query | 7 / 8 | Staged / committed particle instance position, Index is instance index; components 0–2 |
| Query | 9 | GPU particle staging, Index is slot; components 0–2 position and 3–5 velocity |
| Query | 10 | Existing point-light slot position; components 0–2 |

Typed calls clear LastError on success and set the existing renderer error on
failure. Query wrappers return Boolean and write a ByRef result only after status
success, so a valid zero is distinguishable from an invalid/stale handle. Mutations
return the existing 0/1 status. Queries do not encode identities as floating values.

Native precision dispatch is a focused include inside the existing renderer's
translation unit, borrowing its camera validation, object/animator lookup, socket
matrix, reflection accessors and submission arrays. It owns no independent state.
Web uses a focused composed runtime source with those same existing owners.
Precision3D owns typed bridge/value contracts; PrecisionMath3D owns vector/angle/
lerp/cubic evaluation. The existing Scene3D and Character3D owners retain lifecycle.

World coordinates are bounded to one million, FOV to 10–160 degrees and far to
two million. A camera must remain usable after narrowing: distinct eye/target,
up/forward maximum component greater than 1e-12 and cross-product squared greater
than forward² × up² × 1e-8. This avoids float32 normalization underflow.
For bounded coordinates within 1000, the initial upload budget is 2e-4 world units;
quarter units are exact there. Near one million, float32 spacing is about 0.0625;
Double authoring does not change shaders, authored animation precision or z-buffer
precision. No origin-rebasing feature is included.

## Production adoption and compatibility

`PrecisionCamera3D` owns shared continuous pan, orbit, bounded eased zoom,
auto-fit, projection and cursor anchoring. ViewerCamera and Sin Star I's
BattleArenaPreview supply their own scene sensitivity and framing. UI pixel
coordinates, pointer identities and elapsed milliseconds remain Number; conversion
occurs before continuous motion math. Integer labels do not feed camera acceptance.

ViewerParty retains its sequence, event order, 650-ms approach and 700-ms return.
It evaluates Double progress from the actual integer elapsed time, passes precise
positions through ViewerActors/Character3D, and submits the same owned render parts.
Current-pose sockets, equipment pivots, glow, fire, lightning and Orin trails use
the typed path. FireEmitter3D and LightningVfx3D retain their existing pools,
seeds, admission budgets, integer clocks, ages and event counters. Their legacy
Number entry points explicitly adapt to the same precise owners. Culling bounds,
density counts, atlas selectors and checksum display remain integral.

The shared cubic visual example is `examples/PrecisionCurve3D`; its 6-second
forward/return traversal uses the same clamped de Casteljau evaluator covered by
the precision fixture. It is a visual slice, not a spline editor or framework.

Authored scale percentages, millith-unit model bounds and saved calibration channels
retain their current meaning. Saved wrist/equipment corrections narrow explicitly
only when committing an integral channel value. No asset is rescaled or rebaked.
Legacy Core vectors and queries keep their Number contracts. Existing source using
them remains valid; new continuous code uses explicit ToDouble/ToNumber boundaries.
All `.smilelib` dependencies must be rebuilt as format 7 after the Double compiler
upgrade; format 1–6 packages receive the explicit rebuild diagnostic.
