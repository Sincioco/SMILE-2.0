# SMILE 2.0 Tetris

`Program.smile` is the normal SMILE 2.0 Tetris game with its attract demo. `Program-NoDemo.smile` keeps manual movement, rotation, falling, hard drop, animated row clears, scoring, levels, high score, music, and sounds while removing demo planning and automatic play.

Keyboard controls remain A/D or Left/Right to move, W/Up to rotate clockwise, S/Down to soft drop, Space to hard drop, and Escape to exit. On the Web virtual controller, X rotates counterclockwise, B rotates clockwise, and A or Y hard-drops the piece.

`Assets\Background.mp3` loops during active player and attract-demo play. The title and game-over screen remain silent.

To study or build the simpler version in Visual Studio, change `<StartupFile>` in `Tetris.smileproj` to `Program-NoDemo.smile`.
