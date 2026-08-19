# Space Wars: Vector Assault

Space Wars is an original first-person vector rail shooter for SMILE 2.0. The player pilots an interceptor through the Obsidian Array in a three-mission campaign rendered entirely through the source-level `Smile.Simple3D` software wireframe library and existing 2D drawing commands.

## Campaign

1. **Outer Defense** — destroy eight attacking fighters drawn from two original vector silhouettes.
2. **Array Surface** — cross the perspective grid, destroy five lattice relays, and withstand surface turrets.
3. **Reactor Conduit** — weave through recycled conduit gates and destroy the reactor prism.

The game includes score and persistent high score, six shields with hit invulnerability, fixed enemy/projectile/explosion/section/star pools, mission briefings, help, pause, mission-complete, victory, and game-over states.

## Controls

| Input | Action |
|---|---|
| Pointer movement, arrows, or WASD | Aim |
| Primary pointer | Confirm outside a mission; fire during a mission |
| Space or virtual X | Fire during a mission |
| Enter, Space, or virtual A | Start / confirm |
| Tab or virtual B | Pause / resume |
| Virtual Y | Shield pulse |
| Escape | Pause in a mission; back on other screens; exit at title |
| 4 | Toggle training shields |

Keyboard, pointer, touch/pen Pointer Events, and the existing Web virtual controls all feed the same game actions. The campaign is completable without a mouse.

## Demo and no-demo startup

`Program.smile` waits five seconds on the title screen, then runs a bounded attract-mode demonstration. Any key or pointer-button press returns directly to the title. The demonstration never exposes a victory or game-over screen.

`Program-NoDemo.smile` is a complete independent teaching startup with none of the demo timer, automation, cancellation, UI, or recovery code. It shares only ordinary game support modules.

Build the normal project:

```powershell
artifacts\compiler\smilec.exe --project games\SpaceWars\SpaceWars.smileproj --target windows-x64 -o artifacts\games\SpaceWars\SpaceWars.exe
artifacts\compiler\smilec.exe --project games\SpaceWars\SpaceWars.smileproj --target web --output-dir artifacts\web\SpaceWars
```

Compile the no-demo startup directly after building `Smile.Simple3D`:

```powershell
artifacts\compiler\smilec.exe games\SpaceWars\Program-NoDemo.smile --source games\SpaceWars\SpaceWarsTypes.smile --source games\SpaceWars\SpaceWarsModels.smile --source games\SpaceWars\SpaceWarsGameplay.smile --library libraries\Smile.Simple3D\bin\Release\Smile.Simple3D.smilelib --target windows-x64 -o artifacts\games\SpaceWars-NoDemo\SpaceWars-NoDemo.exe
```

Run the focused native/Web validation with:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\test-simple3d-space-wars.ps1
```

## Source map

- `Program.smile` — main game loop and bounded demonstration.
- `Program-NoDemo.smile` — genuine no-demo game loop.
- `SpaceWarsTypes.smile` — state, mission, pool, and entity constants.
- `SpaceWarsModels.smile` — original vector mesh coordinates.
- `SpaceWarsGameplay.smile` — state machine, campaign logic, pools, HUD, rendering, and sound playback.
- `Program-Validation.smile` / `SpaceWarsStateTests.smileproj` — deterministic campaign and saturation tests.
- `Assets/` — original generated PCM WAV sound effects and provenance.

## Originality and limits

The setting, Obsidian Array, interceptor/fighter/relay/turret/gate/reactor-prism meshes, coordinates, mission writing, UI, and synthesized audio are original to this repository. The game uses broad historical wireframe rail-shooter techniques only; it contains no licensed franchise names, characters, vehicles, geometry, audio, or story elements.

Rendering is intentionally educational fixed-point wireframe software rendering. It does not provide hidden-surface removal, triangle fill, lighting, textures, or a GPU 3D API. Pools and the renderer line budget are deliberately bounded; saturated pools reject excess spawns without allocation.
