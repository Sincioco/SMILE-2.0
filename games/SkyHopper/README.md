# SMILE 2.0 Sky Hopper

Sky Hopper is an original one-button obstacle-flight game written entirely in SMILE source for the shared 960-by-540 game runtime. It demonstrates fixed-step gravity and flap velocity, procedural safe-gap movement, gate recycling, forgiving collision, first-pass scoring, gentle difficulty growth, persistent human high score, and geometric artwork without a map system.

`Program.smile` is the normal startup source. Five idle title seconds launch an attract pilot that targets the next gate through the same flap physics used by the player. Any ordinary key cancels directly to the title, protected demo collisions safely restart the stream, terminal demo events return directly to the title, and demo scores are never persisted.

`Program-NoDemo.smile` is the complete player-focused teaching edition. It preserves the procedural gate stream, controls, physics, collision, scoring, high score, difficulty, rendering, audio, retry, and normal game-over behavior while removing the attract lifecycle, AI, timing, recovery, UI, cancellation, and record guards.

To build the teaching edition in Visual Studio, change:

```xml
<StartupFile>Program-NoDemo.smile</StartupFile>
```

in `SkyHopper.smileproj`. Both sources remain declared in the project.

Controls:

- Space / `W` / Up: flap
- Escape: return to the title
- Alt+Enter: runtime-managed full screen

The original WAV effects and airy melody are generated deterministically by `scripts\generate-sounds.ps1`. The same melody is retained as `Background.wav` and encoded with the already-installed local FFmpeg tool as `Background.mp3` for the current shared MediaPlayer path. Shared runtime focus handling automatically mutes inactive games.
