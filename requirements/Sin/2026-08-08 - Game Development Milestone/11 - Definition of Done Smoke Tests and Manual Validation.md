# Definition of Done, Smoke Tests, and Manual Validation

## Regression

These still compile:

```text
Hello.smile
LanguageBasics.smile
RuntimeBasics.smile
ConsoleSnake.smile
```

## Language smoke coverage

Cover constants, operators, 2D arrays, routines, function conditions, argument errors, missing return, SELECT CASE, loop exits, keys, colors, graphics, sound, storage, and missing keywords.

## Shared services

For representative valid/invalid files, compiler and Visual Studio must report the same diagnostic code, message, line, and column.

## Native verification

All games:

- PE x64;
- Windows GUI subsystem;
- no CLR dependency;
- no console window;
- run without the compiler present;
- game logic originates from SMILE source.

## Graphics checks

- 960×540;
- every primitive;
- no obvious flicker;
- resize and aspect preservation;
- exact 1080p and 4K scale math;
- Alt+Enter repeatedly;
- restore window;
- DPI changes where available;
- clean close;
- no GDI resource leak.

## Input checks

WASD, arrows, Enter, Escape, Space, 1, 2, simultaneous held keys, focus loss, and full-screen.

## Audio/storage checks

- assets copy;
- asynchronous playback;
- missing file safe;
- save/reload value;
- corrupt value defaults;
- per-game isolation.

## Visual Studio checks

- VSIX installs;
- both templates appear;
- projects open in Solution Explorer;
- highlighting and shared diagnostics work;
- Ctrl+Shift+B builds;
- Ctrl+F5 and F5 run;
- console/game subsystem correct;
- assets copy;
- loose-file command remains.

## Game checks

Perform all game-specific acceptance criteria in files 05–08.

## Smoke script

Update `scripts\smoke-test.cmd` to build the solution, compile console examples, GraphicsBasics, and all games, verify executables/assets, and run noninteractive diagnostics. Do not auto-launch all interactive games unattended.

## Final Git state

- detailed commit bodies;
- all commits pushed;
- no accidental temp files;
- no external reference images;
- clean `git status`;
- README works from a fresh clone.

## Final report

Provide commit hashes, syntax added, architecture changes, VS behavior, artifact paths, validation results, manual results, 1080p/4K checks for Sin, limitations, and next milestone.
