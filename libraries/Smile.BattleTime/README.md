# Smile.BattleTime

`Smile.BattleTime` exports `Smile.RPG.BattleTime`, a deterministic fixed-timestep ATB adapter around the unchanged `Smile.RPG.BattleCore` round engine.

Gauges fill from effective BattleCore Agility and a configurable speed from 1 through 5. Ready serials preserve stable participant order when multiple gauges fill on the same tick. Active mode continues while presentation is busy; Wait mode freezes. Player code submits any ready action through `SubmitAction`; reusable enemy AI may use `SubmitReadyEnemyAttack` or the same general API. Once every living participant has submitted, `TryStartRound` delegates resolution to BattleCore.

The package is separate so existing Smile.RPG projects, its 1.2.1 identity, and round-only behavior remain unchanged. Run `scripts/test-battle-time.ps1` for native/Web fixed-step parity.
