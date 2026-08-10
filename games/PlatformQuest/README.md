# SMILE 2.0 Platform Quest

Platform Quest is an original 960-by-540 side-scrolling platform game written entirely in SMILE source. It demonstrates fixed-point running and jumping, separate horizontal and vertical tile collision, one-way platforms, an independently scrolling camera, enemies, hazards, blocks, coins, a goal gate, external map parsing, and safe chunk-based random generation.

`Program.smile` is the normal startup source. Five idle title seconds launch an attract pilot that uses the same controls and physics as the player. Any ordinary key cancels it directly to the title, demo terminal events return directly to the title after the protected minimum, and demo scores are never persisted.

`Program-NoDemo.smile` is the complete player-focused teaching edition. It preserves all maps, random generation, physics, controls, collision, audio, enemies, scoring, lives, high-score persistence, game-over, and victory behavior while removing the attract lifecycle, AI, timing, recovery, UI, cancellation, and record guards.

To build the teaching edition in Visual Studio, change:

```xml
<StartupFile>Program-NoDemo.smile</StartupFile>
```

in `PlatformQuest.smileproj`. Both sources remain declared in the project.

The title offers `DEFAULT.MAP`, `CUSTOM.MAP`, and `RANDOM LEVEL`. File maps reload on every start. Missing or invalid files fall back to a safe random level. See `MAP_AUTHORING.md` for the exact 120-by-15 format and editing checklist.

Controls:

- `A` / Left: run left
- `D` / Right: run right
- `W` / Up / Space: jump; release early for a shorter jump
- Escape: return to the title
- Alt+Enter: runtime-managed full screen

The original WAV effects and adventure melody are generated deterministically by `scripts\generate-sounds.ps1`. The same melody is retained as `Background.wav` and encoded with the already-installed local FFmpeg tool as `Background.mp3`, because the current shared music channel uses file-backed MP3 playback. Shared runtime focus handling automatically mutes inactive games.
