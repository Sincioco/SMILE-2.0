# M00 r001 — Passed

- Identified Blender 5.2.1 LTS, installed Blender Lab MCP 1.0.0, and actual native tool schemas.
- Preserved original Neris Town process 72340 and its unsaved state. A separate clean process 27740 was launched to the right of Codex.
- Confirmed port 9876 listens on 127.0.0.1 and belongs to the new process.
- Read the clean unsaved scene: Camera, Cube, Light; no prior user work.
- Created `NC.Preflight.Probe.Cube` in its own collection; measured exactly 1 × 1 × 1 m at (0, 0, 0.5).
- Saved `source/neris-castle-M00-probe-r001.blend`, reopened it through the installed MCP, and verified the probe persisted with the same dimensions.
- Rendered and visually inspected `probe.png`; copied the native tool's actual temp output intact into this checkpoint.
- Removed only the owned probe/collection and the known default startup objects from the new process. Saved the empty isolated scene as `source/neris-castle-M00-isolated-r001.blend`.
- No install, upgrade, Codex configuration change, protection change, custom Python file or repository change.
- Ready to continue to provisional M01 only. M01 dimensions and visual approval remain pending.

Environment: `../../logs/environment.json`. Archive integrity and extraction checks: `../../extraction-audit.json`.
