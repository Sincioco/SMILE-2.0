# Smile.RPG 1.2.1 battle API

Phase 9 introduced these four ordinary, source-authored modules in 1.2.0. The
1.2.1 format-6 package preserves that public API unchanged. They are
deterministic, bounded, headless, renderer-neutral, and owned by `Smile.RPG`;
none imports `Smile.Game`, `Smile.UI`, images, audio, or drawing primitives. All
definition IDs are caller-supplied stable positive Numbers. Runtime participant
IDs are stable for one active battle.

## `Smile.RPG.BattleCore`

Capacities are four active party members, eight enemy instances, four enemy groups, twelve total participants, 64 enemy definitions, 32 formations, eight enemies per formation, eight statuses per participant, and 1,024 events. One RPG state can own at most one active battle.

`EnemyDefinition` supplies HP, MP, six battle statistics, Experience, Gold, and an AI profile ID. `FormationDefinition` supplies a stable ID, name, and escape chance from 0 through 100. `FormationMemberDefinition` assigns an enemy definition to one formation group and logical slot.

- definitions: `DefineEnemy`, `DefineFormation`, `AddFormationMember`, `EnemyDefinitionCount`, `FormationCount`
- lifecycle: `BeginBattle`, `IsActive`, `CurrentBattleHandle`, `IsBattleHandleValid`, `Phase`, `Outcome`, `RoundNumber`, `EndBattle`, `CancelBattle`
- participants: `ParticipantCount`, side/reference/definition/group/slot/name queries, HP/MP queries, `IsAlive`, `Agility`, `AiProfileId`, and status queries
- actions: `QueueAction`, `StartRound`, `Advance`, `ClearQueuedActions`
- event stream: `EventCount`, event kind/actor/target/value/text queries, and `ConsumeEvents`

Actions are Attack, Ability, Item, Defend, and Run. A round freezes every submitted living participant and sorts them by effective Agility descending; the lower participant ID wins a tie. `Advance` resolves exactly one scheduled action, or closes the round after its final action. A defeated actor's queued action is skipped safely.

Attack and physical effects use `max(1, Strength + Power + random(0..4) - Defense / 2)`. Magic damage substitutes Magic and Resistance. Defend halves incoming damage with integer rounding up. Healing and MP restoration add the effect's fixed Power and clamp at the target maximum. Revive restores its fixed Power, clamped from one through maximum HP.

Single-target actions retain their requested legal target; an invalid or defeated target deterministically retargets to the first legal participant. Self, ally, enemy, all-allies, and all-enemies modes reuse the existing `RPG_TARGET_*` constants. A revive component requires a defeated ally; other effects require a living target.

The caller supplies a nonnegative battle seed. The internal sequence is `seed = (seed * 25173 + 13849) mod 65536`. It drives damage variance and Run rolls only, so the same definitions, state, commands, and seed produce the same events and outcome on native and Web.

Victory atomically awards the sum of the formation's Experience to every active party character and the sum of its Gold once to the party. Overflow or any failed reward mutation restores every character's prior Experience and the prior Gold. Defeat and escape award nothing. MP payment, item removal, multi-target effects, compound status application, per-character Experience, Gold, events, status ticks, and PRNG state are action-transaction boundaries; a failed `Advance` leaves the action retryable without partial mutation.

## `Smile.RPG.BattleEffects`

Capacities are 128 effect definitions, eight ordered components per effect, and 32 status definitions.

- definitions: `DefineEffect`, `DefineStatus`, `AddComponent`
- effect queries: definition count/ID/name and component count/kind/power/status/duration
- status queries: definition count/ID/name/kind/statistic/value/tick timing

Effect components support physical damage, magic damage, HP healing, MP restoration, apply status, remove status, and revive. Statuses support Poison, action denial, and signed Strength/Defense/Magic/Resistance/Agility/Luck modifiers. Poison and stat modifiers tick at round end. Action denial is consumed at that participant's action start. Applying an existing status refreshes its duration instead of creating a duplicate.

## `Smile.RPG.BattleStrategy`

Up to 64 deterministic strategy/AI rules are available per RPG state. `StandingOrderDefinition` supplies action, source Ability/Item ID, target selector, and repeat policy. `AiRuleDefinition` adds AI profile, priority, and condition.

- standing orders: `BeginOrderEdit`, `SetEditedOrder`, `CommitOrderEdit`, `CancelOrderEdit`, `HasStandingOrder`, `StandingActionKind`
- automatic rounds: `PrepareFightRound`, `PrepareRunRound`
- enemy AI: `DefineAiRule`, `AiRuleCount`
- safe interruption: `RequestInterrupt`, `InterruptReady`, `AcknowledgeInterrupt`

Order edits are atomic: Commit replaces the complete standing-order set and Cancel preserves it. Fight queues each living party member's standing order, or Attack when none exists, then queues enemy AI and begins the round. Repeat policies retain an order, convert a one-shot order to Attack after one queue, fall back to Attack when Ability/Item becomes invalid, or retain Defend. Strategy is an application menu that commits a prepared set through the same edit transaction.

Enemy rules are scanned by ascending priority and stable definition order. Conditions include always, actor HP below percent, actor MP at least, and ally HP below percent. Targets include first, lowest HP, highest HP, self, and first defeated ally. If no valid rule queues, AI deterministically attacks the first living opponent.

Run queues one living party member's escape attempt plus enemy actions. A successful roll ends battle resolution immediately; failure grants the scheduled enemy actions. An interrupt request made during resolution becomes ready only at the following command boundary.

## `Smile.RPG.BattleView`

Twelve logical battlefield slots and 256 presentation cues form the renderer seam. `SlotDefinition` contains participant ID, logical X/Y/Z, cardinal facing, layer, and center/feet/head/left/right anchor. The coordinates are not pixels and remain usable by a future 3D renderer.

`AppendEvents` translates mechanics events into ordered pose, move, effect, number, message, shake, sound, wait, hide, and reward cues. Applications query cue fields, advance deterministic durations with `Update`, inspect `IsComplete`, and clear the cue buffer. BattleView does not load assets or draw; applications choose sprites, animation, camera, particles, fonts, and sounds.

## Persistence boundary

Battle definitions live with the in-memory RPG state, while participants, queued actions, statuses, events, cues, seed, and outcome are transient. `SaveGames.Encode`, `Decode`, `SaveGame`, and `LoadGame` return `RPG_RESULT_BATTLE_ACTIVE` while a battle is active. SRPG writing remains format 2, reading remains formats 1 and 2, and no battle-session fields were added.

See `examples\Phase9BattleStateTests` for the headless contract and the Battle option in `games\RPGSystems` for application-owned DirectX, GDI, and Web presentation.
