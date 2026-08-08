# MP3 MediaPlayer implementation report

## Result

SMILE 2.0 supports a single asynchronous MP3 background-music channel through the Windows SDK `Windows.Media.Playback.MediaPlayer` API. The implementation uses C++/WinRT and Windows Media Foundation components included with Windows. It adds no package, third-party audio library, decoder, DLL, background thread, or game-specific native helper.

The native compiler and Visual Studio extension continue to share `src\Smile.Language` as their only lexer, keyword table, parser, syntax tree, diagnostic source, and semantic model.

## Language and compiler

The shared language surface is:

```smile
PLAY MUSIC "Assets\Background.mp3"
PLAY MUSIC "Assets\Background.mp3" LOOP
PAUSE MUSIC
RESUME MUSIC
STOP MUSIC
MUSIC VOLUME 50
```

`MusicStatementSyntax` is separate from WAV-oriented `SoundStatementSyntax`. All music statements require `GAME WINDOW`; paths must be non-empty literals and volume must be numeric. `SML3026` is reserved for music-specific semantic errors, leaving `SML3024` unchanged for WAV effects.

The MASM emitter uses its existing Windows x64 call helper for `smile_music_play`, `smile_music_pause`, `smile_music_resume`, `smile_music_stop`, and `smile_music_set_volume`. A music-bearing program also calls the idempotent `smile_music_shutdown` before every `ExitProcess` route and before a normal return from generated `main`.

## Native implementation

`src\Smile.NativeRuntime\audio\music_mediaplayer.cpp` exposes only a C ABI to generated code. It lazily allocates its own state from the Win32 process heap, initializes the apartment on the calling game thread, retains one `MediaPlayer` and active `MediaSource`, uses MediaPlayer's native loop and volume properties, and catches all exceptions at the ABI boundary. Because `/entry:main` skips static CRT startup, only this object uses the loader-initialized Microsoft dynamic C/C++ runtime needed by C++/WinRT async delegates and exception handling; non-music programs never pull the object. The source is obtained from `StorageFile::GetFileFromPathAsync(...).get()` once per play request; playback itself remains asynchronous.

Both WAV and MP3 paths use one bounded UTF-8-to-UTF-16 resolver in `runtime.c`. Relative paths are based on the generated executable's directory, not its current working directory. A missing, inaccessible, corrupt, or unsupported file is nonfatal.

The native link command adds `WindowsApp.lib`. When the shared syntax tree contains a music statement, the emitter reports that fact to the toolchain and it also adds the loader-initialized Microsoft C/C++ runtime import libraries required under SMILE's custom `/entry:main`. Non-music programs do not reference the MediaPlayer object or receive those music-runtime imports.

## Focus policy

The Win32 window runtime separately records application activation, top-level-window activation, and minimization. Audio is active only when all three allow it. An inactive transition immediately stops the current `PlaySoundW` effect, suppresses later WAV requests, and asks the MediaPlayer module to apply effective volume zero. Returning to an active, non-minimized window restores the exact requested 0-through-100 music volume without restarting playback, resuming a manual pause, replaying a WAV effect, or changing Windows or another application's volume.

The pure `audio_focus_state` helper is exercised without audio hardware by the native regression executable.

## Falling Blocks integration

Falling Blocks remains silent on its title and game-over screens. `ResetGame` starts the exact supplied `Assets\Background.mp3` in looping mode only after the state becomes live. `EndGame` stops music before the game-over WAV. Retry replaces and restarts the track, and both explicit exit paths stop music and WAV effects before `END PROGRAM`.

## Scope

This milestone intentionally excludes playlists, seeking, fading, position or duration queries, multiple music channels, streaming URLs, video, MIDI, recording, DSP, and per-effect volume. MP3 is the documented and tested music format.

## Validation performed

- `scripts\build.cmd` completed with the Release native runtime, compiler, tests, shared-language DLL, Visual Studio extension, templates, and VSIX.
- The native runtime also built explicitly in Debug x64.
- `scripts\smoke-test.cmd` passed 43 shared language/project/timing checks and 35 native graphics/audio-focus checks, every console regression, all four existing games, exact asset hashing, native x64 GUI verification, and VSIX payload verification.
- Retained Falling Blocks MASM contains looping `smile_music_play`, state-based `smile_music_stop`, and `smile_music_shutdown` before every `ExitProcess` and normal `main` return.
- DirectX and GDI executables both rendered the title, entered live gameplay through MediaPlayer initialization, survived focus loss and minimize/restore, and exited cleanly. GDI also reached game over and restarted successfully; DirectX toggled to borderless true full screen and back.
- Temporary nonexistent and corrupt MP3 programs both exited with code zero instead of crashing.
- PE dependency inspection confirmed that non-music `Hello.exe` has no MediaPlayer C++ runtime imports, while Falling Blocks receives the Microsoft C++/WinRT runtime dependencies.

Audible output, looping at the physical end of the track, and perceived volume restoration were not claimed by automated or visual testing. They remain on the manual checklist for a person who can hear the test machine.
