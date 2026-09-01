# Dragonfall Character Generation 2 — M7A Visual Adapter

Date: 2026-09-01 (Asia/Taipei)

Milestone: M7A — generic Dragonfall visual adapter

Status: **Complete**; production-character work remains separately gated in M7B.

Branch: `main`

Starting local commit: `97bbcae9acf3cc212cfc203799e4d6b6a78fb93f`

Starting `origin/main`: `97bbcae9acf3cc212cfc203799e4d6b6a78fb93f`; ahead/behind `0/0`.

Ending commit: the focused M7A commit containing this report; its SHA is recorded in the delivery report because a commit cannot contain its own SHA.

Pushed and verified: recorded in the delivery report after the focused commit is pushed and `origin/main` is verified.

## Prerequisites and reconciliation

- Root `AGENTS.md`, the sequential M6.1/M7 instructions, all numbered M7 package files, and the package manifest were read.
- M0 through M6.1 reports and the current Character3D, Scene3D, Effects3D, Battle3D, Renderer3D, and Dragonfall ownership paths were reconciled against the actual branch.
- M6.1 is green and pushed. Its required PNG follow-up is pushed at `97bbcae9acf3cc212cfc203799e4d6b6a78fb93f`.
- The starting tracked tree was clean. The existing untracked `docs/plans/` tree remains preserved and excluded.
- Four untracked Paladin view PNGs appeared under `games/SinStarI/Assets/Characters/` during validation. They are unrelated user work and remain preserved and excluded from M7A.
- No reset, clean, restore, rebase, amend, force-push, or history rewrite was used.

## Implemented

`games/Dragonfall/DragonfallVisualActor.smile` is a bounded Dragonfall-local adapter with at most 16 generation-safe visual identities. It does not add a native command or duplicate Character3D ownership.

The adapter provides:

- forced Classic, forced Generation 2, and Auto modes;
- a permanent `RELEASE_MODE = MODE_CLASSIC` default while production Arin remains unapproved;
- Classic rigid and Character3D create, update, draw, destroy, transform, visibility, and shadow operations;
- mixed-scene rendering without exposing ownership through the borrowed primary interop handle;
- atomic Character3D creation with the primary error preserved and Auto fallback to a valid Classic object;
- caller-borrowed or adapter-owned Classic object lifetimes;
- Dragonfall state-to-production-clip mapping;
- a separately flagged technical-fixture clip fallback used only by the M7A proof;
- Dragonfall anchor-to-production-socket mapping with `HandTip` used only as a technical-fixture alias;
- animation-event mapping for sword/shield impacts into bounded standard Effects3D presets;
- conservative Character3D bounds or Classic proxy bounds through one API;
- explicit teardown and diagnostics.

The adapter source is compiled by both `Dragonfall.smileproj` and `Dragonfall-NoDemo.smileproj`, but neither startup path opts into it. Technical assets live outside the release project's `Assets` glob. Current Classic Dragonfall and its published asset set remain the exact normal release output.

## Technical proof projects

`DragonfallVisualAdapterTests.smileproj` exercises both paths, mixed drawing, exact failure diagnostics, missing model, missing texture, PBR unavailability, borrowed/owned Classic lifetimes, event/socket/effect mapping, bounds, and 100 sequential Character3D restarts. Native and Web exact console output is:

```text
Dragonfall M7A visual adapter tests passed.
```

`DragonfallVisualAdapterLab.smileproj` is the visible native/Web proof. Its committed asset copies are exact SHA-256 matches of repository-owned fixtures:

| Asset | Purpose |
| --- | --- |
| `AnimationArticulated.sm3d` | 32-vertex, 36-triangle, eight-bone, five-clip articulated technical actor |
| `AnimationArticulatedMissingTexture.sm3d` | deterministic PBR preparation failure/fallback input |
| `VfxAtlas.png` | existing bounded M6 Effects3D atlas |

These assets are technical evidence only and are not named or presented as production Arin.

## State, socket, and event mapping

| Dragonfall state | Production clip | Technical proof fallback |
| --- | --- | --- |
| Ready | `Ready` | `Idle` |
| Approach | `Run` | `WalkLike` |
| Attack | `SwordAttack` | `AttackLike` |
| Special | `ShieldBash` | `Bend` |
| Defend | `Defend` | `Bend` |
| Block impact | `BlockImpact` | `Bend` |
| Hit | `Hit` | `Bend` |
| KO | `KO` | `Bend` |
| Victory | `Victory` | `Idle` |

Production anchors remain `SwordBase`, `SwordTip`, `ShieldCenter`, `Chest`, and `Head`. The fixture exposes only `HandTip`, so the M7A test path uses that name as an explicit technical alias. M7B must reject a production asset missing the required production sockets rather than use this alias.

`SwordImpact` maps to `PRESET_HOLY_SWORD_STRIKE`; `ShieldImpact` maps to `PRESET_SHIELD_IMPACT`. The fixture's `Impact` and `AttackStart` event names are explicit technical aliases. Effects only present an event and never submit damage, healing, ATB, targeting, or outcomes.

## Mechanics and compatibility

The existing Dragonfall battle, scene, audio, crowd-demo, no-demo, inputs, balance, outcome, and Renderer2D HUD sources are unchanged. The retained Dragonfall gate passed after compiling the adapter into both game projects, proving exact native/Web mechanics output, 100 existing scene lifecycle cycles, balance, both startup programs, and asset publication.

Preserved unchanged:

- Renderer3D numeric commands 1-121; next numeric ID 122;
- Renderer3D image commands 1-2; next image ID 3;
- Renderer3D text commands 1-9; next text ID 10;
- SM3D v1/v2, PBR, both animation paths, Scene3D, Effects3D, Renderer2D, GDI, and Battle3D;
- all existing Dragonfall release visuals and battle mechanics;
- Simple3D, Space Wars, and Neon Cycles.

No language syntax, compiler behavior, runtime command, VSIX payload, SM3D format, PBR feature, animation feature, VFX feature, or M8 work was added.

## Resources and performance evidence

The visible technical lab's idle Web frame reports four draw submissions and 50 triangles: floor (two), Classic cube (12), and the two-part Character3D fixture (36). The captured impact frame reports six submissions and 340 triangles after Effects3D adds its bounded batches. The native impact capture also reports six submissions and 340 triangles.

The Generation 2 technical actor owns one Character3D identity, one animator, two part objects, and one shared cached model. The Classic proxy uses one object and its owned primitive mesh. Effects3D owns its bounded particle/ribbon batches. The focused test requires all adapter actors, Character3D actors/cache, models, animators, objects, meshes, materials, and textures to return to zero after teardown.

These are integration-proof measurements, not production Arin budgets or an FPS claim.

## Tests and exact results

| Gate | Exact result |
| --- | --- |
| Targeted SMILE formatter | PASS; three new SMILE files formatted and checked. |
| M7A native normal | PASS; exact `Dragonfall M7A visual adapter tests passed.` |
| M7A native forced PBR failure | PASS; exact output parity with normal. |
| M7A Web normal | PASS; exact console parity. |
| M7A Web forced PBR failure | PASS; exact console parity. |
| M7A Lab native/Web builds | PASS; two declared assets published; both Web JavaScript files pass `node --check`. |
| Existing Dragonfall focused gate | PASS; `Dragonfall native/Web mechanics, lifecycle, demo, and no-demo validation passed.` |
| Combined M7A gate | PASS; `Dragonfall M7A Classic/Character3D adapter, mixed draw, state/clip, anchor/socket, event/Effects3D, bounds, atomic fallback, 100-restart, native/Web, crowd-demo, and no-demo tests passed.` |
| `cmd /c scripts\build.cmd` | PASS; compiler, AssetTool, native runtime/tests, managed solution, and VSIX artifacts built. |
| Formatter integration and repository check | PASS; 13 formatter integration tests and 337 tracked SMILE files. |
| M1.1 and SM3D v2 boundaries | PASS; hardening plus exact 7,865,176-byte boundary and over-limit rejection. |
| Models, lifecycle, and materials | PASS; deterministic fixture/conversion and native/Web exact parity. |
| PBR and PBR hardening | PASS; native/Web normal/fallback, ownership, transform, skinning, and lifecycle gates. |
| Animation v1, animation v2, and M3.1 hardening | PASS; native/Web hierarchy, import, 128-bone, crossfade, event, root-motion, socket, deformation, memory, and lifecycle gates. |
| Character3D and Scene3D | PASS; native/Web cache, ownership, atomicity, profiles, lighting, animation, sockets, bounds, reset, fallback, and Lab builds. |
| M5 and M5.1 | PASS; native/Web shadow/post pipeline plus snapshot, ownership, target, color, and hot-path hardening. |
| M6 and M6.1 | PASS; native/Web VFX batches, 1,024 instances, deterministic Effects3D, transactional lifecycle, revision isolation, request capacity, restoration, and hot path; native M6 runs reported 908 ms and 968 ms. |
| Battle3D | PASS; native/Web exact parity. |
| Simple3D/Space Wars and Neon Cycles | PASS; native/Web focused gates. |
| Managed suite | PASS; 288 language, compiler, project, completion, and timing tests; expected synthetic failure diagnostics observed. |
| Artifact verification | PASS; libraries, native executables, game assets, VSIX payload/version 2.0.56, viewport, and DPI checks. |
| `cmd /c scripts\smoke-test.cmd` | PASS, exit 0; .NET SDK 10.0.400, Node.js 24.14.0, full native/Web/game/library/artifact baseline. |

## Native and Web manual checks

Native Direct3D 11 displayed the mixed Classic/Character3D lab and a mapped impact burst. WebGL2 displayed the same mixed pair, an idle interval at four draws/50 triangles, and the mapped impact at six draws/340 triangles. The lab visibly labels the fixture as non-production and the release mode as Classic.

## Mobile-review evidence

Committed PNGs and their purpose are indexed at `docs/implementation/screenshots/m7a-dragonfall-visual-adapter/screenshot-index.md`.

## VSIX

M7A changes only Dragonfall-local source, projects, fixtures, a test script, screenshots, and documentation. It does not affect the compiler, templates, language services, or any VSIX payload. The already installed and verified VSIX remains version 2.0.56; no redundant rebuild or installation is required.

## Known limitations and M7B readiness

- The articulated fixture is intentionally tiny technical proof, not production art.
- Current release Dragonfall remains Classic until M7B passes its production asset, rights, converter, deformation, clip/event/socket, PBR, native/Web, and visual-review gates.
- The user-supplied `Paladin_1K.glb` has suitable 1K embedded textures, but its separate geometry is 1,931,538 triangles, it has no animation clips, it lacks the required production sockets/events, and its 76,287,916-byte GLB exceeds the current 64 MiB converter input ceiling. Raising runtime geometry limits would not make that asset production-safe.
- M7B therefore needs an optimized approximately 10,000-15,000-triangle derivative with the required clips/events/sockets and a complete public-repository provenance/license record. The original GLB can be preserved as source intake without claiming M7B completion.

M7A is complete and removes the code-seam blocker. M7B is not complete. M8 is not authorized and was not started.

## Command ledger

Substantive commands used for M7A:

```powershell
Get-Content <root rules, sequential instructions, M0-M6.1 reports, M7 package, Dragonfall and Simple3D APIs>
rg <Dragonfall, Character3D, Scene3D, Effects3D, Battle3D, and Renderer3D ownership/API searches>
Get-ChildItem <repository, handoff, fixtures, and asset inventories>
Get-FileHash -Algorithm SHA256 <fixture and screenshot paths>

artifacts\compiler\smilec.exe --project games\Dragonfall\DragonfallVisualAdapterTests.smileproj --target windows-x64 --configuration Release --graphics DirectX -o artifacts\tests\DragonfallVisualAdapterTests.exe
scripts\run-bounded-test.cmd 60 artifacts\tests\DragonfallVisualAdapterTests.exe
artifacts\compiler\smilec.exe --project games\Dragonfall\DragonfallVisualAdapterTests.smileproj --target web --configuration Release --output-dir artifacts\web\DragonfallVisualAdapterTests
node scripts\run-web-test.js artifacts\web\DragonfallVisualAdapterTests --expected games\Dragonfall\DragonfallVisualAdapterTests.expected.txt --timeout 60000 --renderer3d
node scripts\run-web-test.js artifacts\web\DragonfallVisualAdapterTests --expected games\Dragonfall\DragonfallVisualAdapterTests.expected.txt --timeout 60000 --renderer3d --force-renderer3d-pbr-failure
scripts\format-smile-style.ps1 -Files <three M7A SMILE files> -FormatLongIf
scripts\format-smile-style.ps1 -Check -Files <three M7A SMILE files> -FormatLongIf
scripts\test-dragonfall-character-generation-2.ps1 -Configuration Release

artifacts\compiler\smilec.exe --project games\Dragonfall\DragonfallVisualAdapterLab.smileproj --target windows-x64 --configuration Release --graphics DirectX -o artifacts\examples\DragonfallVisualAdapterLab.exe
artifacts\compiler\smilec.exe --project games\Dragonfall\DragonfallVisualAdapterLab.smileproj --target web --configuration Release --output-dir artifacts\web\DragonfallVisualAdapterLab
python -m http.server 8767 --bind 127.0.0.1 --directory artifacts\web\DragonfallVisualAdapterLab
```

Read-only browser/Windows capture, `node --check`, image dimension/hash inspection, `git status`, `git diff`, `git log`, and repository validation commands are also part of the M7A evidence trail.
