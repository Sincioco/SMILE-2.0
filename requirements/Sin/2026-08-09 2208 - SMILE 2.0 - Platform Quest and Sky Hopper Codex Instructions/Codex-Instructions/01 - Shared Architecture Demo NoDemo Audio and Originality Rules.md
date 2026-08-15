# Shared Architecture, Demo, No-Demo, Audio, and Originality Rules

These requirements apply to both Platform Quest and Sky Hopper.

---

# 1. Use the current language

No new SMILE syntax is approved for these games.

Implement physics with integer fixed-point values.

Use:

```smile
Const SubpixelsPerPixel = 1000
Const SimulationStep = 8
Const MaxCatchUpSteps = 6
```

or the current repository’s latest established equivalent.

Movement must be time-based and refresh-independent.

Do not use `Wait 16 Milliseconds` to pace a VSync game loop.

---

# 2. Demo lifecycle

Use current repository conventions.

Unless a newer current convention supersedes these values:

```smile
Const TitleDemoDelay = 5000
Const DemoMinimumPlayTime = 30000
Const DemoMaximumPlayTime = 45000
Const TitleInputArmDelay = 250
```

Required:

- title inactivity starts demo after five seconds;
- any ordinary input during demo returns directly to title;
- the canceling key is consumed;
- Alt+Enter remains runtime-reserved;
- demo never writes user high scores;
- when demo time expires, return directly to title;
- if demo naturally ends after the protected minimum, return directly to title;
- do not show a demo game-over, demo victory, retry, or rematch screen.

Before 30 seconds, a demo-only recovery may reset the run without exposing a terminal screen.

---

# 3. Genuine `Program-NoDemo.smile`

Each game must include:

```text
Program.smile
Program-NoDemo.smile
```

`Program-NoDemo.smile` must be a complete playable teaching source.

It must remove—not merely disable:

- demo AI;
- demo state;
- title inactivity auto-start;
- demo timers;
- demo safety recovery;
- demo cancellation;
- demo UI;
- demo-only record guards.

It must preserve:

- user controls;
- user physics;
- rendering;
- scoring;
- high-score persistence;
- maps where applicable;
- audio;
- normal game-over/victory;
- all user-facing gameplay.

Both files must be declared in the `.smileproj`.

`Program.smile` remains the normal startup source.

Document how to switch `StartupFile` to `Program-NoDemo.smile`.

Compile and verify both editions in the smoke suite.

---

# 4. Original visuals

Use runtime-drawn geometric artwork only.

Allowed primitives include:

```text
rectangles
rounded rectangles
circles
arcs
quadrilaterals
lines
text
numbers
```

Do not copy:

- Mario;
- Luigi;
- Goombas;
- mushrooms;
- question-block artwork;
- flagpole layouts;
- Flappy Bird;
- its bird silhouette;
- its green pipes;
- commercial backgrounds;
- commercial sound effects.

Platform Quest should use an original explorer and original block/enemy designs.

Sky Hopper should use an original winged creature and original sky-gate obstacles.

---

# 5. Background music

Both games require original looping background music during user gameplay and demo gameplay.

The title should remain silent unless current repository convention says otherwise.

Preferred implementation:

```smile
Music Volume <tuned value>
Play Music "Assets\\Background.wav" Loop
```

The current native music channel uses Windows `MediaPlayer` and loads a file-backed media source. Codex must perform one brief playback check with the generated WAV.

If the current target environment does not accept WAV through `Play Music`, encode the same original composition as:

```text
Background.mp3
```

using an already available local tool.

Do not add a third-party repository dependency merely to encode music.

Do not copy copyrighted music.

---

# 6. Music generation

Extend:

```text
scripts\generate-sounds.ps1
```

with the smallest reusable deterministic melody helper needed to create a loopable music file.

A suitable helper may accept:

```text
relative output path
note-frequency array
note-duration array
tempo or sample duration
```

Generate a short original 8–16-second mono 16-bit PCM loop.

Reduce clicks with brief attack/release envelopes.

Platform Quest music should feel upbeat and adventurous.

Sky Hopper music should feel light and airy.

Keep the compositions simple and original.

---

# 7. Sound effects

Generate original WAV effects through the shared script.

Sound effects may interrupt each other because the current WAV surface is one asynchronous effect channel. Prioritize important events.

Focus muting remains automatic runtime behavior.

Do not write focus handling in the games.

---

# 8. Persistence

Use per-executable integer persistence for user records.

Demo mode must not update or save those records.

Suggested:

```text
Platform Quest:
    HighScore
    BestCoins or BestCompletionScore optional

Sky Hopper:
    HighScore
```

No save-game system is required.

---

# 9. Rendering and backend rules

Both games must work through the existing backend-neutral graphics surface.

Do not access GDI, Direct2D, or Direct3D directly from `.smile`.

Do not add game-specific graphics exports.

Use the full logical:

```text
960 x 540
```

canvas.

Test one brief DirectX run and one brief GDI run per game.

---

# 10. Light validation

For each game:

- compile demo source;
- compile no-demo source;
- run the normal smoke suite;
- perform one short user happy path;
- perform one normal demo cycle or a shortened local diagnostic equivalent;
- verify assets copy;
- verify native x64 GUI/no CLR;
- verify one DirectX and one GDI launch.

Do not run long soak tests or exhaustive random-generation campaigns unless investigating a known defect.
