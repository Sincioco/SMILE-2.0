# SMILE 2.0 — Water Lab (native)

Water Lab inspects Mira and Kael water choreography, contacts and shared native
VFX in the [Viewer arena](../../docs/libraries/arena3d.md). It is an authored
real-time preview, not a fluid/collision simulator or a battle-rules editor.

- [Presentation, ownership, limits and validation](docs/presentation-and-limits.md)
- [Complete September development and evidence archive](docs/history-2026-09.md)
- [Studio](../Character3DViewer/README.md) uses the shared presentation owners;
  the separate Studio concept is abandoned and Web work remains paused.

## Build and launch

Run from the repository root:

```powershell
pwsh -NoProfile -File tools/WaterVfxLab/Launch.ps1 -Build
```

The launcher gracefully closes the matching published Lab process before an
optional build, waiting up to ten seconds; failure to close stops the launch.
It starts `bin/Release/WaterVfxLab.exe` in its publication directory. Build uses
`artifacts/compiler/smilec.exe`, Windows x64/DirectX and repository assets. It
stages ignored model, background, water, lightning and audio copies. Do not use
those disposable copies to replace canonical character packages.

Canonical game assets live in `D:\SMILE 2.0 - Sin Star I`; the build's
`games/SinStarI` paths use the local compatibility junction. The Lab needs those
assets available locally. Blender is required only to re-author model motions.
The normal runtime supplies the official startup presentation and remembered
window placement. No download or new service is part of the normal Lab build.

## Controls and starting state

The Lab starts with **Kael**, **200% speed**, **Demo**, **Realistic Water** and
automatic orbit enabled. Both actors and backgrounds preload. Mira starts on
Waterbending when selected.

| Control | Action |
| --- | --- |
| X / Mira / Kael | Switch preloaded character |
| Tab / cast buttons | Select/repeat a cast; selecting Kael's cast disables Demo |
| D / Demo | Cycle Kael's three casts |
| E / Realistic Water | Compare Kael's clear and thicker irregular water, retaining pause/time |
| Space / Pause | Pause/resume presentation |
| Left / Right while paused | Seek by 100 ms |
| R / Restart | Restart cast |
| + / − / speed buttons | 25-point steps from 25% to 400%; default 200% |
| O / Orbit | Toggle automatic two-minute orbit |
| Left drag / middle drag / wheel | Pan / orbit / zoom; manual pan/orbit stops automatic orbit |
| Right-click / Enter | Reset camera |
| F / G | Toggle floor / grid |
| B / background button | Scene → Logo → Black → Green → Purple → Scene |
| Sound button | Toggle only Lab audio |

Scene/Logo label the shared Landscape/Title assets. Camera and background controls
remain available while paused; background changes do not reset casts or reload
textures. One scaled clock drives poses, water, lightning and cues in steps no
larger than 100 ms; camera response stays real-time and audio pitch is unchanged.

## Effects and limits

Kael previews **Water Whip**, **Serpent Orbit** and **Tidal Surge**, with his sword
hidden. Demo visits four increasingly large pillars. Water releases at 42% and
contacts at 72%; caller-supplied small cylindrical targets can receive wrapping
sheets, while missing/large bounds use a crown splash.

Mira previews **Healing Veil, Tide Barrier, Torrent, Impact, Waterball, Tsunami,
Tempest Combo and Waterbending**. Waterbending holds one randomly chosen pillar
for the cast; other attack previews use the far pillar. Contacts, shield behavior,
target dimensions and exact budgets are in the [topic guide](docs/presentation-and-limits.md).

The shared effect owns two ribbon batches and a 4,096-slot spray system; contact
bursts share a 128-droplet budget even across simultaneous shields. Caller-provided
contact approximations do not add collision, health or battle-scheduler rules.
Reflection/refraction/shadow behavior and material limitations are documented
alongside ownership; renderer limits have not been raised for this Lab.

## Validation and unresolved acceptance

After preparing assets with Build.ps1, run `tools/WaterVfxLab/Test.ps1` and
`tools/WaterVfxLab/Test-Audio.ps1` for relevant native changes. The first requires
Visual C++ tools and includes seam normals, actual water-shader shadows and the
native effect fixture. Audio testing observes real scene Play/Stop commands;
it does not record speaker output. These are reusable checks, not new PASS claims
from this documentation cleanup.

The September shared-preset follow-up recorded successful automated checks but
an unsuccessful computer-use capture. Its requested visual acceptance of all
eight presets—especially Tide Barrier, Waterball and Tsunami—was not conclusively
closed by that record. Preserve that evidence gap; other dated visual passes
must not silently stand in for it. See [historical validation](docs/history-2026-09.md#native-validation-september-16-2026).

## Historical entry points

<a id="kael-water-preview--september-18-2026"></a>
<a id="ownership-and-focused-validation"></a>

- [September 18 Kael adoption and evidence](docs/history-2026-09.md#kael-water-preview--september-18-2026)

<a id="ownership-and-limits"></a>

- [Current ownership and limits](docs/presentation-and-limits.md#ownership-and-resource-budgets)

<a id="waterbending-reference-september-16-2026"></a>

- [Visual references and asset-use constraints](docs/history-2026-09.md#waterbending-reference-september-16-2026)

<a id="native-validation-september-16-2026"></a>

- [September native validation, failures and growth evidence](docs/history-2026-09.md#native-validation-september-16-2026)

<a id="native-water-receives-directional-shadows"></a>

- [Current water lighting/shadow contract](docs/presentation-and-limits.md#materials-and-reflections)
