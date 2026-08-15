# Sky Hopper — Original Flappy-Style Game

Create:

```text
games\SkyHopper
```

Public identity:

```text
Display name: SMILE 2.0 Sky Hopper
Output:       SkyHopper.exe
```

Sky Hopper is an original one-button obstacle-flight game.

It does not require a map file.

Every run procedurally generates and recycles obstacle gates.

---

# 1. Product scope

Required:

- full 960-by-540 playfield;
- original flying creature;
- one-button flap control;
- gravity and vertical velocity;
- scrolling obstacle gates;
- randomized gap positions;
- object recycling;
- collision;
- score;
- persistent high score;
- increasing speed/difficulty;
- original background;
- original music;
- original sound effects;
- five-second demo;
- genuine `Program-NoDemo.smile`;
- DirectX and GDI support.

Not required:

- copied Flappy Bird character;
- copied green pipes;
- imported sprites;
- a map file;
- levels;
- enemies;
- power-ups;
- multiplayer;
- mouse input;
- online scores.

---

# 2. Visual identity

Use an original small winged creature, for example:

- circle/rounded body;
- triangular or quadrilateral beak;
- arc/circle eye;
- animated wing made from circle/arc/quadrilateral;
- distinct colors.

Obstacles should be original sky gates or towers, not green plumbing pipes.

Suggested palette:

```text
sky blue / sunset gradient bands
purple or blue stone gates
gold edge highlights
white clouds
green/brown ground strip
```

Use several parallax cloud/hill layers.

---

# 3. Fixed-step physics

Suggested:

```smile
Const SubpixelsPerPixel = 1000
Const SimulationStep = 8
Const MaxCatchUpSteps = 6

Const Gravity = 1150000
Const FlapVelocity = -390000
Const MaximumFallSpeed = 650000
Const BirdX = 250
```

Tune visually.

State:

```text
BirdYSub
BirdVelocity
BirdRadius or collision box
WingFrame
```

Each flap:

- sets or clamps upward velocity;
- plays `Flap.wav`.

Do not make motion frame-rate dependent.

---

# 4. Controls

| Input | Action |
|---|---|
| Space | Flap |
| W / Up | Flap |
| Escape | Return to title |
| Alt+Enter | Full screen |

Title Enter/Space starts.

No left/right movement is required.

---

# 5. Obstacles

Use fixed arrays, for example:

```text
GateActive
GateXSub
GapCenterY
GapHeight
GatePassed
```

Maximum active gate pairs:

```text
5 or 6
```

A gate pair consists of:

- a top obstacle;
- a bottom obstacle;
- one open gap.

When a pair leaves the left edge:

- move it behind the rightmost pair;
- choose a new safe random gap center;
- reset its passed flag.

Initial spacing should be generous.

Recommended:

```text
GateWidth:       80–100 pixels
Horizontal gap:  260–330 pixels
Vertical gap:    170–200 pixels
```

Gradually:

- increase horizontal speed;
- decrease vertical gap to a safe minimum.

No map file is needed because this recycling system is the core lesson.

---

# 6. Collision

Use a simple bird circle or small rectangle.

Collision occurs when:

- bird overlaps the top gate;
- bird overlaps the bottom gate;
- bird touches the ground;
- bird leaves the top play boundary.

Keep collision slightly forgiving.

User collision:

- stop music;
- play `Hit.wav`;
- show normal user game over;
- save high score;
- allow retry/title.

---

# 7. Scoring

When the bird passes the center/right edge of a gate pair for the first time:

- increment score;
- mark pair passed;
- play `Score.wav`.

Persist user high score only.

Difficulty may use score bands.

---

# 8. Demo AI

The AI controls flaps using the next gate.

Find the nearest gate ahead of the bird.

Compute a target:

```text
TargetY = GapCenterY
```

Optionally shift slightly based on horizontal distance and falling velocity.

Flap when:

- the bird is below the target zone;
- the bird is falling too quickly;
- an emergency lower-bound condition is reached.

Avoid flapping when near the top of the gap.

Use a short AI decision interval so it does not react every simulation step.

Before 30 seconds:

- if collision occurs, reset bird and gate positions;
- preserve `DemoStartedAt`;
- continue;
- do not show game over.

After 30 seconds:

- natural collision may return directly to title.

At 45 seconds:

- return directly to title.

Any ordinary input returns directly to title and is consumed.

Demo score does not save.

---

# 9. Title and states

Recommended states:

```text
TITLE
USER_READY
USER_PLAY
USER_GAME_OVER
DEMO_PLAY
```

Title shows:

```text
SKY HOPPER
SPACE / W / UP To FLAP
PRESS ENTER Or SPACE
DEMO STARTS AUTOMATICALLY
```

The title remains silent.

After five seconds inactive, demo begins.

Demo completion returns directly to title.

---

# 10. `Program-NoDemo.smile`

Create a complete separate teaching version.

It removes:

- demo state;
- AI;
- automatic title start;
- demo timers;
- demo recovery;
- demo UI;
- demo record guards.

It preserves:

- procedural gates;
- user physics;
- score/high score;
- music/effects;
- title;
- normal user game over/retry.

---

# 11. Audio assets

Create:

```text
Assets\Background.wav
Assets\Start.wav
Assets\Flap.wav
Assets\Score.wav
Assets\Hit.wav
Assets\GameOver.wav
```

Music plays during user/demo flight only.

Use a lower music volume so flap and score effects remain clear.

All sounds and music must be original.

---

# 12. Required comments

Add concise comments explaining:

- gravity and flap velocity;
- fixed-step movement;
- gate recycling;
- random gap clamping;
- first-pass scoring;
- simple demo target control.

---

# 13. No map requirement

Do not create:

```text
default.map
custom.map
map menu
file parser
```

for Sky Hopper.

The game’s procedural obstacle stream is the intended equivalent of level generation.

If generation somehow enters an invalid state, reset the gate sequence safely.
