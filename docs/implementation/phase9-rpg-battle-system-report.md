# Phase 9 RPG battle-system report

## Closure status and commit evidence

Phase 9 is complete at the reusable-library, public-gallery, private-reference, automated-test, artifact, and Visual Studio acceptance layers. The work adds a deterministic, bounded, renderer-neutral RPG battle authority without adding a language keyword, compiler/runtime helper, active-battle persistence, or 3D renderer.

- Actual Phase 8.1 starting SHA: `e3779c73cb08eb27b3144a3dcc80e758f9676f12`
- Phase 9 implementation SHA: `b740f110698b066d5e79007f99f18e0d1f3d8e1d`
- Documentation/evidence SHA: the commit containing this report; its exact SHA is recorded in the completion handoff
- Branch: `main`
- Push result: the implementation and documentation/evidence commits were pushed to `origin/main`; exact final synchronization is recorded in the completion handoff
- Final worktree state: clean and synchronized with `origin/main`; exact final SHA is recorded in the completion handoff

The older SHA named in the authored handoff was not used. Repository inspection found the legitimate later Phase 8.1 closure at `e3779c73`, with its report, pushed commits, clean worktree, and explicit statement that Phase 9 had not started.

## Gap matrix result

The pre-production matrix is `docs/architecture/phase9-rpg-battle-gap-matrix.md`. It found that persistent characters, party order, HP, MP, statistics, EXP, Gold, inventory, abilities, encounters, and exact World return locations already had authoritative owners. It therefore rejected a duplicate RPG-state model and limited the new surface to four ordinary transient modules.

The matrix also explicitly deferred active-battle serialization, persistent ailments, broad loot/level-curve systems, a 3D renderer, and Phase 10 chapter work. Private PS1/PS2 work was initially paused by Sin, then resumed and completed after Sin lifted that pause.

## Module architecture

- `Smile.RPG.BattleCore` owns battle registration, active sessions, participants, formations, command validation, turn/round resolution, deterministic PRNG state, event emission, outcomes, and atomic rewards.
- `Smile.RPG.BattleStrategy` owns standing per-character orders, order editing, repeat/one-shot policy, safe interrupt requests, deterministic AI profiles/rules, target-selection policy, and validated submission into `BattleCore`.
- `Smile.RPG.BattleEffects` owns bounded effect and status definitions, composite components, effect-kind semantics, status durations, action denial, and signed statistic modifiers.
- `Smile.RPG.BattleView` owns renderer-neutral logical slots and presentation cues derived from authoritative battle events.

The modules compose the existing `Characters`, `Party`, `Inventory`, `Abilities`, `Encounters`, `World`, and `SaveGames` authorities. They contain no `Game Window`, Image, audio, UI, menu, renderer, direct-drawing, or timing dependency. Application code remains responsible for input, drawing, animation timing, music, sound playback, and visual layout.

## Public API and capacities

The public API is documented in `docs/libraries/smile-rpg-battle-api.md`, with architectural guidance in `docs/architecture/phase9-rpg-battle-system.md` and language-facing usage in `docs/language/phase9-rpg-battles.md`.

Core bounds per RPG state are four active party participants, eight simultaneous enemy instances, four enemy groups, twelve total participants, 64 enemy definitions, 32 formations, eight formation members, eight statuses per participant, and 256 battle events. Effects provide 128 effect definitions with eight components each and 32 status definitions. Strategy provides 64 definitions/rules. View provides twelve logical slots and 256 presentation cues.

The ordinary API covers enemy/formation definition, explicit seeded begin/end, participant inspection, Attack/Ability/Item/Defend/Run commands, command readiness, deterministic advancing, event inspection, results, standing orders, one-shot orders, interrupt requests, AI rules/profiles, effect/status definitions, logical slots, cue generation, and cue inspection. Public terms are Ability and Magic Points/MP throughout.

## Deterministic seed model

Each battle accepts an explicit integer seed. The battle PRNG advances as:

`seed = (seed * 25173 + 13849) mod 65536`

Only damage variance and Run resolution consume PRNG values. Registration, validation, target repair, scheduling, status timing, reward calculation, and view-cue generation do not use wall-clock time or implicit randomness. Identical initial state, definitions, commands, and seed therefore produce identical native/Web state and console evidence.

## Battle lifecycle

One active battle is allowed per RPG state. `BeginBattle` validates the generation-safe state handle and registered formation, snapshots the first four active party positions into transient participants, instantiates the bounded enemy formation/group layout, initializes the seed/event stream, and enters the command boundary.

Each round accepts party commands or standing Fight orders, deterministically creates enemy commands, schedules eligible participants, and resolves one action at a time. Round-end status ticks occur after scheduled actions. The battle ends only in victory, defeat, or successful escape. Settlement is single-use; status/session data is transient and cleaned when the application closes the finished battle. Stale handles and a second concurrent begin are rejected.

## Turn and round formula

Eligible participants are ordered by effective Agility descending. Ties use the lower stable participant ID first. Effective statistics include bounded battle-scoped signed status modifiers. A defeated or action-denied participant cannot execute a normal action. Action denial is consumed at action start; Poison and timed statistic modifiers tick at round end.

Command validation and payment occur at an action transaction boundary. Each action either completes its MP/item payment and all target mutations or restores the exact pre-action state. Outcome/reward settlement uses a separate transaction boundary.

## Damage and healing formulas

Physical damage is:

`max(1, attacker Strength + action Power + random(0..4) - target Defense / 2)`

Magic damage substitutes attacker Magic and target Resistance. Integer arithmetic is used throughout. Defend halves incoming damage with round-up semantics. Healing, MP restoration, and revive use fixed effect power with bounded clamping: HP cannot exceed maximum HP, MP cannot exceed maximum MP, and revive restores at least 1 HP and no more than maximum HP. Composite effects apply their components atomically.

## Target and retarget rules

The engine supports self, one ally, one enemy, all allies, and all enemies. Normal beneficial effects require living allies; revive requires a defeated ally; hostile effects require living opponents. A requested legal target is retained. If it becomes illegal before resolution, deterministic repair selects the first legal stable participant. All-target actions enumerate legal targets in stable participant order. If no legal target exists, the action produces no illegal mutation and the event stream records the resolved outcome.

## Status definitions and timing

Phase 9 statuses cover Poison, action denial, and signed Strength/Defense/Magic/Resistance/Agility/Luck modifiers. A participant holds no more than eight active statuses. Reapplication refreshes duration rather than creating unbounded duplicates. Action denial is consumed at action start. Poison and timed modifiers decrement/tick at round end. Battle-scoped statuses are removed with the transient session; durable ailments are intentionally not added to SRPG.

## Reward policy

Victory sums formation EXP and Gold. Every active character present at battle start receives the full EXP award, while Gold is awarded once to the party wallet. Reward capacity is preflighted; character EXP updates and party Gold either all commit once or all roll back. Defeat and escape award neither EXP nor Gold. Repeated settle calls cannot duplicate rewards.

## Save boundary

`SaveGames.Encode`, `Decode`, `Save`, and `Load` return `RPG_RESULT_BATTLE_ACTIVE` while the state owns an active battle. A battle session is never encoded. SRPG continues to write format 2 and read formats 1 and 2; no SRPG 3 format or migration was introduced.

## Fight, Strategy, Order, and Run

Fight submits the current standing orders for all command-ready active characters. Strategy selects or updates the standing policy used by Fight. Order exposes atomic per-character edits. Run requests the party escape action.

Standing orders may repeat or be one-shot. A repeating valid Ability or Item remains selected across rounds. A one-shot order becomes Attack after it is queued once. If its Ability/Item definition, resource, quantity, or target is no longer legal, submission safely falls back to Attack. Defend can remain a standing order. Atomic edit APIs prevent a partially written strategy table.

Only one party Run attempt occurs in a round. Success ends the battle before enemy attacks. Failure lets the enemy side act. An Escape-key-style interrupt request never opens a command menu mid-action; it becomes ready only at the next safe command boundary after the current round.

## Deterministic enemy AI

AI rules are evaluated by explicit priority with stable definition order as the tie-breaker. Conditions include always, actor HP below a threshold, actor MP at least a threshold, and ally HP below a threshold. Target selectors include first, lowest health, highest health, self, and first defeated ally. An unusable rule is skipped; if no rule produces a valid command, AI falls back to Attack against the first living opponent. AI consumes no PRNG state.

## Battle event stream

The bounded event stream exposes battle start, round/turn boundaries, submitted/resolved actions, payments, effects, HP/MP/status changes, messages, target/outcome information, victory/defeat/escape, and rewards. Events carry stable identifiers and numeric payloads rather than renderer objects. Applications can drain or inspect events at their own animation rate without changing mechanics.

## BattleView slot and cue contract

`BattleView` maps up to twelve participants onto logical X/Y/Z coordinates plus facing, layer, anchor, and visibility. It converts authoritative events to bounded pose, move, effect, number, message, shake, sound, wait, hide, and reward cues. Cues specify intent and stable IDs, not Images, sound buffers, fonts, draw calls, or real-time duration ownership.

## BattleEffects contract

Each effect definition contains up to eight ordered components. Components cover damage, healing, MP restoration, status apply/remove, and revive, with explicit power, target policy, and status ID. Definitions are validated before registration. Composite execution is transactional, including the injected case where an early target/component mutation succeeds and a later one fails.

## Future Renderer3D seam

A future renderer can consume the same participant identities, logical X/Y/Z slots, facing/layer/anchor data, event stream, and cue kinds. It can substitute cameras, meshes, particles, spatial audio, and animation graphs without changing battle mechanics. Phase 9 deliberately implements no 3D API or renderer.

## RpgBattleGallery results

The original public gallery demonstrates a PSII-inspired rear-facing four-member party battle with multiple enemy groups, a PSI-inspired environmental overworld battle, and a first-person crystalline corridor battle. It includes Fight/Strategy/Order/Run, abilities, items, Defend, status UI, messages, floating damage/healing values, animation cues, battle effects, deterministic rewards, music, and sound effects.

- DirectX: Debug/Release mechanics and presentation builds passed; a live native walkthrough reached title, exploration, and battle presentation with input, animation, event/cue consumption, and clean process behavior.
- GDI: compatibility builds and native launch verification passed with the same application-owned assets and battle authority.
- Web/DPR-2: Web build, asset publication, deterministic execution, viewport/DPR checks, and a live browser launch passed. The final IDE Web launch used the installed 2.0.47 compiler and displayed the gallery title/menu and original Starfall Plateau art.

The five public PNGs are original ImageGen assets created in the built-in design workflow from the tracked prompt set in `examples/RpgBattleGallery/Assets/README.md`: `StarfallPlateau.png`, `LumenPlaza.png`, `PrismVault.png`, `PartyLineup.png`, and `EnemyLineup.png`. This materially established an original, high-resolution public visual identity without using commercial game art. Six deterministic synthesized WAV files provide the three scene themes and Strike/Ability/Victory cues. The project publishes exactly eleven assets.

## Exploration integration results

- Overworld: an encounter starts with explicit formation/seed and exact World return scene/cell/facing; victory, defeat, and escape restore the saved exploration location.
- Top-down dungeon/town presentation: the application enters battle from the top-down scene, runs the same authority/cue pipeline, and returns through the existing World boundary.
- First-person corridor: the crystalline corridor presentation starts a distinct formation, consumes the same renderer-neutral events, and restores the recorded first-person exploration state.

No exploration renderer or map format was moved into `Smile.RPG`.

## Private PS1 result

The native-only local PS1 capability project now resolves a Palma environmental/overworld battle and a Camineet Warehouse corridor battle using the shared public modules. It defines Fire, Green Slime, and Wing Eye content in local application code, exposes Fight/Strategy/Order/Run, uses deterministic AI/rewards, and returns to the exact scene/cell/facing after victory, defeat, or escape. The former encounter preview is replaced by real resolution. DirectX and GDI Release builds published the expected fifteen private assets and passed launch probes.

## Private PS2 result

The native-only local PS2 capability project now begins with Rolf, Nei, Rudo, and Amy, renders all four field party members in corrected follower order, and resolves multi-group Mosquito/Locust battles from the overworld and Shure. It exercises Fight/Strategy/Order/Run, Foi/Resta, deterministic AI, rewards, safe interrupt, and exact exploration return. DirectX and GDI Release builds published the expected nineteen private assets and passed launch probes. Its opaque battle fill was removed so the intended presentation remains visible.

The private smoke also retained the PS1 258-cell road-connectivity and PS2 222-cell town/starter-party checks. Private projects remain native-only and produced no Web directory.

## Source-fidelity uncertainties

The private demonstrations use local reference imagery, sprites, music, derived maps, and observed game layouts, but Phase 9 does not claim byte-identical emulation, frame-perfect timing, exact original encounter tables, exact undocumented enemy statistics/formulas, or identical behavior across every commercial release/localization. The public battle formulas and AI are SMILE contracts, not reverse-engineered claims. Subjective similarity of sprite scale, placements, pacing, audio balance, and battle composition remains Sin's manual review surface.

## Copyright and repository safety

All commercial/reference material remains under `D:\SMILE 2.0 Local Reference Tests`, outside Git, the public solution/smoke payload, packages, VSIX, and Web output. The tracked SHA-256 audit scans raw files and every public VSIX/ZIP/NuGet archive entry, rejects matches, and confirms no private Web output. The closure audit found zero raw matches and zero archive-entry matches across 217 private files (82 unique hashes), the complete public tree, and 89 public archives containing 663 entries.

## Versions and formats

- Smile.Game: 1.0.0 (unchanged)
- Smile.RPG: 1.2.0 (four new battle modules; fifteen total modules)
- Smile.UI: 1.1.3 (unchanged)
- VSIX: 2.0.47; assembly/file/product version 2.0.47.0
- SMILE-MAP: writes and reads format 1
- SRPG: writes format 2; reads formats 1 and 2
- `.smilelib`: format 5

The installed VSIX DLL was loaded from `C:\Users\louie\AppData\Local\Microsoft\VisualStudio\18.0_91f001b5\Extensions\svrdvud4.jxk\Smile.VisualStudio.dll`. Its assembly version is 2.0.47.0 and its SHA-256 is `9EBCA1C82E851719B114E445D4D3C82E25FD59CF24DA53559B857A9994CD3B2D`, matching the newly built DLL.

## Focused test counts and parity

`Phase9BattleStateTests` passed 194 checks through the project-reference and package-reference paths on native Windows and Web. Native project/package output matched exactly; Web project/package output matched native exactly. Coverage includes capacities, stale handles, lifecycle, every command, targeting/retargeting, order, formulas, Defend, MP/item payment, composite effects, revive, statuses/timing, Fight/Strategy/Order/Run, repeat/one-shot fallback, interrupt, AI, events, view cues, rewards, save blocking, outcomes, and exact deterministic replay.

The disposable rollback suite injected failure after MP payment, item removal, first multi-target application, composite status mutation, first character EXP reward, and Gold reward. All six checkpoints restored exact pre-transaction state in native and Web, and all outputs matched. No injection hook or conditional remains in production source.

## Full smoke counts and duration

`cmd /c scripts\smoke-test.cmd` passed in 317.17 seconds. It completed 228 managed language/compiler/project/completion/timing tests, eight formatter integration tests, the 184-file style gate, 39 native graphics/audio-focus checks, 38 native Text checks, all retained RPG/package/rollback/native/Web suites, the 194-check Phase 9 project/package native/Web matrix, all six native/Web rollback checkpoints, DirectX/GDI/Web gallery builds, legacy coverage, all seven game builds, native x64 GUI/asset inspection, Web viewport/DPR checks, and final VSIX payload/version verification.

## Artifact verification

Independent artifact verification passed before and inside the durable smoke run. It verified required native x64 GUI outputs, copied game assets, compiler/shared-language/project-template VSIX payload, synchronized 2.0.47 identities, seven required viewports, and DPI calculations at 100, 125, 150, and 200 percent. `git diff --check` passed; only normal checkout line-ending notices were emitted. Static inspection confirmed that all four battle modules are free of Game/UI/drawing/audio ownership.

## Visual Studio acceptance

The final 2.0.47 VSIX was built, installed, and loaded into a fresh Visual Studio 2026 instance. Live acceptance passed:

- Completion after `Battle.` showed battle members including `BeginBattle` and action constants.
- Quick Info showed the `Smile.RPG.BattleCore.BeginBattle` signature, documentation, package path, and `Smile.RPG@1.2.0` identity.
- F12 navigated from the gallery call to the `BattleCore.smile` definition.
- Web build used the newly installed extension compiler, published eleven assets, succeeded, and launched the gallery in Chrome.
- Native Windows x64 debug compiled, loaded symbols for `RpgBattleGallery.exe`, stopped on an enabled SMILE breakpoint at `Call InitializeRpg()`, and F10 advanced to mapped executable SMILE statements. The debug process then stopped and exited with code 0.

## Known limitations

The system is intentionally bounded rather than dynamically allocated. Battle state is transient and cannot be saved. Statuses do not become persistent ailments. Reward policy is EXP and Gold only; complex loot tables and automatic level curves are deferred. The gallery uses application-owned 2D roster cards/backgrounds rather than a general sprite-animation framework. GDI acceptance is compatibility/build/launch focused, while the broadest hands-on presentation pass was DirectX. Private projects are native-only. No 3D renderer was implemented.

## Phase boundary

Phase 10 was not started. No complete RPG chapter, persistent battle format, broad progression system, or 3D renderer work was added.

Subjective manual review remaining:

- DirectX/GDI/Web battle pacing, command feel, animation cadence, floating-number readability, and cue timing.
- Original public art composition, roster-card treatment, status UI clarity, music balance, and sound-effect balance.
- PS1 environmental and Camineet Warehouse sprite scale, enemy choice/placement, scene fidelity, music balance, and perceived battle feel versus the reference game.
- PS2 four-member field/battle lineup, multi-group composition, enemy scale/placement, Fight/Strategy/Order/Run feel, music balance, and perceived fidelity versus the reference game.

**SIN MANUAL ACCEPTANCE REQUIRED**
