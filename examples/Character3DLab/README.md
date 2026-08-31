# Character3D Lab

This native/Web lab demonstrates the M4 beginner-facing `Character3D` and `Scene3D` modules. Two independently animated actors share one cached articulated model, while the socket marker, lighting presets, quality-aware reload, root-motion policy, and resource diagnostics remain explicit.

Controls:

- `1` Idle, `2` Bend, `3` WalkLike, `4` AttackLike, `Tab` RootMove.
- `Space` crossfades the left actor to the selected clip.
- `W` toggles applied root motion; `S` toggles the HandTip socket marker.
- `A` cycles CharacterStudio, Daylight, Dungeon, Moonlight, and EmberObservatory.
- `Up` cycles Low, Medium, High, and Auto quality for the next reload.
- `Down` destroys and reloads both actors so the selected quality takes effect.
- `D` toggles diagnostics; pointer controls pan/orbit/zoom the camera; `Escape` exits.

The M3.1 fixture contains `Idle`, `Bend`, `WalkLike`, `AttackLike`, and `RootMove`. It intentionally does not invent the handoff’s illustrative `Hit` or `Victory` clips.
