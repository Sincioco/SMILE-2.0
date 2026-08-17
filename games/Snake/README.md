# Snake

`Program.smile` is the normal Snake game with its attract demo. `Program-NoDemo.smile` keeps the same player controls, food, collision, scoring, speed progression, high score, rendering, and sounds while removing the demo pathfinding and lifecycle code. Both startup variants compile with `SnakeModel.smile`.

`SnakeModel.smile` is the focused object-oriented boundary: `GameState` and `MoveDirection` replace magic numbers and keyboard-domain directions, `GridPoint` groups a copyable coordinate, and one `Snake` Class owns private segment storage plus turning, movement, growth, collision, and read-only state. Score, food policy, input, timing, audio, drawing, and lifecycle intentionally remain in the startup programs.

The repository-owner-supplied `Assets\Background.mp3` loops while the player or attract-demo snake is alive. The title and game-over screen remain silent, and starting a new player round or demo restarts the music.

To study or build the simpler version in Visual Studio, change `<StartupFile>` in `Snake.smileproj` to `Program-NoDemo.smile`. Keep the existing `<Compile Include="SnakeModel.smile" />` support-source entry.
