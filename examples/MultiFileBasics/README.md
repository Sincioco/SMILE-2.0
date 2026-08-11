# MultiFileBasics

This example is the Phase 1 proof that one SMILE 2.0 program can be built from several physical source files.

```text
Program.smile
    Startup source: window, input, main loop, FrameCount global.

GameState.smile
    Support source: constants, shared array, update routines, query functions.

Drawing.smile
    Support source: rendering; calls GameState functions and reads FrameCount.
```

The project intentionally uses only language/runtime features that existed before Phase 1. The new capability is compilation across physical source files.

Expected controls:

- Left Arrow: move marker left.
- Right Arrow: move marker right.
- Alt+Enter on Windows: toggle full screen.
- Close the window/browser page to stop.

The final implemented compiler command should resemble:

```text
artifacts\compiler\smilec.exe Program.smile --source GameState.smile --source Drawing.smile -o MultiFileBasics.exe
```

For Web:

```text
artifacts\compiler\smilec.exe Program.smile --source GameState.smile --source Drawing.smile --target web --output-dir Web
```

Codex may adjust this sample only where necessary to match the final approved implementation while retaining all cross-file proof points.
