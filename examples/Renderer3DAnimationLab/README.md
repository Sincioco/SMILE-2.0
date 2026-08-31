# Renderer3D Animation Lab

This native/Web lab proves the M3 model-owned animation path with the deterministic 68-bone actor, PBR skinning, exact named clips, crossfades, events, root-motion extraction, the `SwordTip` socket, and palette diagnostics.

Controls:

- `1`–`4`: select Idle, Walk, Attack, or Hit;
- `Tab`: select Victory;
- `Enter`: play the selected clip;
- `Space`: crossfade to the selected clip;
- `W`: toggle root-motion extraction;
- `A`: apply or ignore extracted root motion;
- `S`: show or hide the socket marker;
- `D`: show or hide diagnostics;
- `Down`: reset both actors;
- pointer drag, wheel, and middle drag: use the shared camera controls;
- `Esc`: exit.

Build from the repository root with `scripts\test-renderer3d-animation-v2.ps1`. The same `Program.smile` compiles for Windows DirectX and WebGL2.
