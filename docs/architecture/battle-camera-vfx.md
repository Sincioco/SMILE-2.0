# Battle camera and VFX

`Smile.Battle3D.Camera` is a bounded data-driven shot library. Games define named position/target/FOV records, play them with fixed integer durations, and copy the interpolated values into any compatible camera. Seeded shake is advanced by the same fixed steps and composes with the base shot without altering its destination.

Manual input is an additive layer over the active shot. Primary-drag pan, middle-drag orbit, and wheel FOV offsets can update together, remain bounded, recover a missed Web-canvas release, and ease to zero after release without stopping shot interpolation or shake. The underlying state and math live in the general `Smile.Simple3D.Interaction` `CameraControl3D` API; `Smile.Battle3D.Camera` is the battle-timeline adapter. `ProjectWorldPoint` converts world anchors to logical-canvas coordinates using the same camera convention, allowing a game to reject actor silhouettes or interface regions before beginning a drag. A manual-only return update is available for paused simulations.

`DefineFramedShot` derives a camera target and distance from arbitrary world bounds rather than actor-specific coordinates. `PlayShotVariation` adds bounded position, target, and lens variation without mutating a stored shot, while `FollowPoint` supports moving targets such as fly-ins and result orbits.

`Smile.Battle3D.Effects` is a deterministic pool of 128 particles driven by up to 32 reusable presets. A preset controls count, lifetime, velocity spread, gravity, color/size fade, alpha or additive blend, billboard intent, screen flash, and requested shake. Spawning is atomic; a full pool rejects the entire effect. Equivalent tick chunks and seeds produce the same native/Web particle state.

Renderer3D material mode `MATERIAL_ALPHA_ADDITIVE` uses source-alpha/one color blending with depth reads and no depth writes on DirectX and WebGL2. Alpha blending remains source-alpha/one-minus-source-alpha. Both modes are generic material policy; particles can also be represented by planes, primitive meshes, or model parts. Billboard particles expose a camera-facing yaw helper while leaving renderer object ownership to the game.

Screen flash is a presentation value rather than a renderer primitive. Games draw the returned opacity as a final Renderer2D overlay, preserving the permanent HUD-over-3D composition contract.
