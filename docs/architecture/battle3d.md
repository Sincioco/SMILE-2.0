# Battle3D presentation architecture

`Smile.Battle3D` sits above `Smile.RPG.BattleView` and below a game renderer. It deliberately does not depend on Dragonfall content, damage formulas, inventory, or command AI.

The actor layer maps each active participant to caller-owned render handles, copies renderer-neutral slot coordinates into a stable transform snapshot, converts cardinal facing to yaw, and exposes center/feet/head/side anchors. The presentation layer atomically compiles a BattleView cue suffix into a bounded sequential timeline. Each command preserves actor, target, values, duration, and text while adding a generic 3D anchor and suggested camera shot.

The caller advances the compiled timeline with deterministic integer steps. `Presentation.Update` advances the source BattleView cue stream by the same amount, so native and Web presentation state cannot drift because of visual frame rate. Renderers inspect commands and remain responsible for applying transforms, playing animation, spawning effects, projecting numbers, drawing the HUD, and playing sound.

All state is generation-safe and bounded to 12 actors and 256 commands, matching BattleCore and BattleView. No renderer resource is created or destroyed by the bridge; actor and animator handles remain caller-owned.
