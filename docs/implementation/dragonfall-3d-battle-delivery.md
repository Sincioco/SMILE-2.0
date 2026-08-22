# Dragonfall 3D battle delivery

## Outcome

Dragonfall: The Ember Observatory is the release proof for SMILE 2.0's reusable indexed-triangle RPG battle stack. The same SMILE sources build for native Windows/DirectX and browser/WebGL2. The crowd startup runs a complete hands-free encounter; the teaching startup contains the complete playable battle without demo AI or automatic replay.

The delivery preserves Renderer2D, the educational software-rendered Simple3D API, the existing round-based Smile.RPG battle modules, GDI behavior, and the fifteen-module `Smile.RPG@1.2.1` package. Renderer3D is an adjacent capability rather than a replacement. New battle-time and presentation behavior lives in the target-neutral `Smile.BattleTime` and `Smile.Battle3D` packages so another game can reuse it without importing Dragonfall code.

## Contract reconciliation

The planning archive was reconciled against repository commit `8d804565ff86d57fe06b37cb4650b1442caaeee4` before implementation.

- Existing project asset publication already handled target-neutral path validation and copying, so PNG, WAV, and SM3D resources use that contract instead of adding a second publisher.
- `Smile.RPG@1.2.1` was already a compatibility release with an intentionally fixed fifteen-module API. Deterministic ATB therefore ships as `Smile.BattleTime`, while Dragonfall composes it with the existing RPG package.
- Renderer2D remains the permanent HUD, text, fallback, and final-composition layer. DirectX/WebGL2 Renderer3D draws the arena first; GDI remains a valid Renderer2D and educational-wireframe target.
- The repository already had a bounded handle-based Simple3D source facade. Renderer3D lifecycle diagnostics, materials, models, animation, cameras, and effects extend that general facade and its native/Web dispatch implementations rather than adding game-specific runtime calls.
- Dragonfall's arena and cast are original procedural geometry. The SM3D humanoid and dragon fixtures prove the reusable offline asset pipeline without making the demo dependent on copyrighted models.

## Delivered layers

1. Renderer3D uses generation-safe bounded mesh, object, texture, material, model, skeleton, clip, and animator ownership. Live-count diagnostics and reference rejection make leaks and stale handles observable.
2. Materials support opaque, cutout, alpha-blend, additive, unlit, and emissive paths with shared texture ownership in DirectX and WebGL2.
3. `smileasset.exe` converts bounded glTF input to deterministic SM3D version 1. Native and Web loaders validate the complete file before allocating renderer resources.
4. The animation layer supports up to 32 bones, parent hierarchies, bind/inverse-bind poses, translation/rotation/scale interpolation, loop/once/hold playback, one-shot events, independent animators, and GPU skinning.
5. `Smile.Battle3D` binds battle participants to actors and compiles logical pose, movement, effect, number, shake, sound, and visibility cues into a skippable presentation timeline.
6. `Smile.BattleTime` provides a fixed-step ATB scheduler with deterministic ties, agility/status modifiers, player/enemy readiness, wait/active modes, KO exclusion, and terminal-state stopping.
7. The reusable battle camera and VFX modules provide interpolated shots, decaying shake, bounded additive/alpha particles, flashes, and exhaustion-safe preallocated pools.
8. Dragonfall composes those layers into a three-hero boss fight with intro, Phase 1, a 50% enrage transition, Phase 2, attack/special/heal/defend/item commands, multiple boss actions, victory, defeat, retry, and exit.

## Acceptance evidence

The focused gate is:

```powershell
.\scripts\test-dragonfall.ps1
```

It passes native/Web mechanics with exact console parity, native/Web complete-scene lifecycle tests, and native/Web builds of both `Dragonfall.smileproj` and `Dragonfall-NoDemo.smileproj`. The mechanics fixture covers every command, multiple enemy actions, both outcomes, the 50% phase transition, 100 battle restarts, and 108,000 fixed ticks (30 simulated minutes). The lifecycle fixture creates, draws, and destroys the complete arena 100 times and requires every Renderer3D live count to return to zero.

Additional focused native/Web fixtures cover:

- thousands of Begin3D/End3D transitions, resize/state restoration, stale handles, capacity failures, and Renderer2D composition;
- valid/shared/missing/invalid textures and every material alpha/light path;
- valid humanoid/dragon SM3D assets and rejection of bad magic, version, range, bone, material, and size data;
- hierarchy/bind pose, looping and one-shot clips, hit/KO/victory/dragon poses, exact animation events, independent actors, and render-rate-independent timing;
- deterministic ATB readiness, ties, wait/active modes, status agility, KO/end handling, and 30/60/120-FPS input invariance;
- participant/formation binding and every Battle3D cue, including presentation skipping without changing the mechanics result;
- default, attack, cast, breath, enrage, death, and victory cameras, FOV interpolation, shake decay, and presentation/mechanics isolation;
- slash, impact, fire, frost, heal, breath, flash, death, additive/alpha blending, bounded pool exhaustion, and steady-state allocation.

The existing compiler suite passes 287 tests. Existing NativeGraphics and NativeText suites pass 39 and 40 tests. Existing Renderer2D, Simple3D/Neon Cycles, Phase 9 rollback, Battle3D, BattleTime, and BattleDrama checks remain green. The complete RPGSystems persistence, initialization, lifetime, native DirectX, native GDI, and Web integration gate passes. The repository style gate passes all 318 tracked SMILE sources.

Manual native inspection used Windows Graphics Capture on the live release executable. It verified animated three-hero/dragon composition, procedural textured arena, attack and impact cuts, enrage, damage banners, victory framing, readable HP/MP/ATB HUD, particle/light effects, and responsive controls. Manual Web inspection used the visible Codex in-app browser against the published Web build and verified live WebGL2 arena rendering, advancing battle state, diagnostics, HUD composition, key input, and no browser warnings or errors. The explicit 2D warning keeps mechanics and HUD usable when Renderer3D scene initialization is unavailable.

The render path performs no impact-time asset loads: all assets load before the encounter, particle objects are preallocated, renderer collections are fixed-capacity, and per-frame paths do not grow unbounded lists. Battle mechanics use fixed ticks and remain correct independently of visual frame cadence. The 960-by-540 logical presentation scales through the repository's existing aspect-preserving window/Web policy, including 1920-by-1080 output.

## Content hygiene

No Final Fantasy names, characters, story, dialogue, models, textures, UI, logos, fonts, music, or effects are distributed. The environment texture is an original generated asset documented in `games/Dragonfall/README.md`. Combat WAV files are processed from CC0 audio packs with exact provenance recorded in `games/Dragonfall/Assets/SFX/LICENSE.md`. Final Fantasy VII is mentioned only as non-distributable design inspiration in project documentation.

## Release artifacts

- `artifacts/games/Dragonfall.exe`: hands-free native crowd demo.
- `artifacts/games/Dragonfall-NoDemo.exe`: native playable teaching build.
- `artifacts/web/Dragonfall`: browser crowd demo.
- `artifacts/web/Dragonfall-NoDemo`: browser playable teaching build.
- `artifacts/compiler/smilec.exe`: compiler.
- `artifacts/assettool/smileasset.exe`: deterministic model converter.
- `artifacts/vsix/Smile.VisualStudio.vsix`: Visual Studio extension.
