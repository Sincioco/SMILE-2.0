# Paddle Ball

`Program.smile` is the normal Paddle Ball game with its computer-versus-computer attract demo. `Program-NoDemo.smile` keeps one-player and local two-player modes, the one-player opponent, scoring, rally tracking, rendering, music, and sounds while removing demo paddle control and lifecycle code.

The repository-owner-supplied `Assets\Background.mp3` loops throughout an active player match or attract demo. The title and match-result screen remain silent, and starting or restarting a match restarts the music.

To study or build the simpler version in Visual Studio, change `<StartupFile>` in `PaddleBall.smileproj` to `Program-NoDemo.smile`.
