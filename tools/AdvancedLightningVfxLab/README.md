# Advanced Lightning Lab

Build with `tools/AdvancedLightningVfxLab/Build.ps1`, then launch
`bin/Debug/AdvancedLightningVfxLab.exe`. Native Windows is the visual target.
The stable application ID preserves window X/Y, width/height and maximized state
through SMILE's existing `RememberWindowPlacement` support, including rebuilds.

The on-screen header is uppercase; the Windows title bar is unchanged. Bare Alt
no longer enters a modal menu loop in the rebuilt native runtime.

The Lab lives beside the Character Viewer and Fire Lab in `tools`. Its default
Sin Star I landscape is screen-fixed, using the same backdrop treatment as Fire
Lab. Automatic orbit also eases through a small height and distance variation;
manual pan, orbit or zoom takes control without resetting the cinematic offset.

The Lab starts in Godstorm Ultra and cycles through nine stations every ten
seconds. Selecting a station turns Demo off. The tenth station compares Low
on the left with the selected quality on the right; the backend button changes
the shared particle simulation request. The basic fallback draws CPU-staged
ribbons and endpoint sprites. A displayed backend of 2 confirms GPU sparks.

Stations: Sky Strike, Forked Judgment, Weapon Charge, Charged Weapon, Thunder
Smash, Chain Lightning, Storm Lance, Arc Storm, Godstorm Ultra and Low/Selected.
The technical figure and hammer use original primitives, not a production actor.
Stored charge belongs to the caller and is separate from effect opacity.

Controls:

- Tab: next station. Space: pause/resume. R: restart.
- 4: quality. G: particle backend request. B: background.
- Backtick: full UI, minimal UI, hidden UI.
- Left drag: pan. Middle drag: orbit. Wheel: eased zoom. Right click: reset view.
- Buttons also control HDR/bloom, branches, soft depth, auto orbit and floor/grid.
- Flash Mode cycles full, reduced and off; reduced is the default.

The native renderer admits up to 8,192 ribbon points per batch and 32,768 across
all batches, using dynamic allocation only at creation. The Lab's Ultra request
reserves three 8,192-point batches and a 16,384-slot GPU spark pool. Actual path
counts are intentionally lower: each Ultra trunk has 128 segments and up to 24
eight-segment branches. Eight independent strikes use 3,158 staged path points
including invisible separators, rendered through three layers. The HUD reports
actual staged points and CPU-authoritative occupied particle slots, not estimates
of visible fragments or a claim that the GPU is saturated. Other quality levels
reserve smaller pools. Shared renderer resources remain bounded.

White hot cores, blue halos, tapered branches, stepped leader/streamer timing,
return-stroke flicker, residual arcs, sparks, light and flash cues are composed
from the existing reusable renderer. The optional native particle dynamics retain
white sparks with drag/gravity instead of relying on the default ember palette.
One original synthesized thunder cue plays per strike cycle; window focus audio
behavior is inherited from SMILE.

Validation entry points:

- `scripts/test-lightning-vfx-foundation.ps1`: native and retained Web contracts,
  including the dense eight-effect path and actual GPU spawn admission.
- `scripts/test-renderer3d-vfx-batches.ps1`: shared ribbon resource lifecycle.
- `scripts/test-native-thermal-fire.ps1 -SkipBuild`: retained fire and GPU checks.
- `scripts/generate-lightning-vfx-assets.ps1 -Check`: asset reproducibility.
- `scripts/smoke-test.cmd`: normal repository smoke suite.

Web visual tuning is deferred. The common Web ribbon bound remains compatible;
the module preserves a basic fallback. No language syntax or ABI command IDs
were added. Native shader and visual controls remain internal to SMILE.
