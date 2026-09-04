# Lightning Foundation

The shared `Smile.Simple3D.LightningVfx3D` module owns bounded, generation-safe
effect handles and three reusable ribbon layers. Caller code owns endpoints,
chain target order and charge. No gameplay damage or target selection occurs
inside the VFX module. Native Direct3D particles add sparks; a basic ribbon and
CPU endpoint-sprite fallback remains available without GPU particles.

Run `scripts/test-lightning-vfx-foundation.ps1` for native execution and retained
Web compilation/console parity. Run `scripts/generate-lightning-vfx-assets.ps1
-Check` to verify deterministic source textures. Tool-local Assets are disposable
copies of `TechnicalAssets/Generation3/Lightning`.

The foundation admits eight effects, 62 points per effect, four ordered chain
targets, three 512-point ribbon batches and 64 CPU endpoint sprites. It admits
an optional GPU spark pool separately. These are foundation bounds, not a claim
of finished Ultra visual quality. Midpoint displacement preserves endpoints;
zero-width separators keep independent paths from joining. Topology changes on
fixed simulation ticks. Mutation is rejected while a frame is in flight.

Validation covers deterministic paths, endpoints, capacity rejection, charge
requests, stale handles, in-flight rejection, cleanup and actual GPU spark
admission. The native visual preview uses HDR bloom and retained window placement.
Space pauses/resumes. Close the window to exit. Web visual tuning is deferred.
