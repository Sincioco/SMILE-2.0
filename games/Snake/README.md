# Snake

`Program.smile` is the normal Snake game with its attract demo. `Program-NoDemo.smile` keeps the same player controls, food, collision, scoring, speed progression, high score, rendering, and sounds while removing the demo pathfinding and lifecycle code.

The repository-owner-supplied `Assets\Background.mp3` loops while the player is alive. The title, attract demo, and game-over screen remain silent, and starting a new round restarts the music.

To study or build the simpler version in Visual Studio, change `<StartupFile>` in `Snake.smileproj` to `Program-NoDemo.smile`.
