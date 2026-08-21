# BattleTime deterministic ATB

`Smile.BattleTime` supplies the `Smile.RPG.BattleTime` module without modifying the existing `Smile.RPG` 1.2.1 package. It is an optional scheduler and adapter above BattleCore, not a replacement combat engine.

Each active RPG state owns bounded gauges for the encounter's current participants. `Advance` accepts integer simulation ticks, fills living non-pending actors from effective BattleCore Agility and configured speed, and assigns monotonically increasing ready serials. The serial makes same-tick order stable and independent of render traversal. Active mode fills during presentation; Wait mode holds when its caller reports a busy presentation or selection period.

Action submission calls the ordinary `BattleCore.QueueAction` API. Submitted actors reset and lock until all living actors have commands, when `TryStartRound` calls the ordinary `BattleCore.StartRound`. BattleCore continues to own validation, damage, effects, inventory, outcomes, events, rewards, and deterministic resolution. Round-only consumers do not reference this package and behave exactly as before.

Clocks retain the active generation-safe battle handle and participant count. Battle cancellation, end, state destruction, or reuse invalidates the clock and all gauges.
