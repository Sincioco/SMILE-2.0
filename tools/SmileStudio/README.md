# SMILE 2.0 Studio S1

Studio hosts the existing Character Viewer and its Arin/Orin pose correction workflow.
The [canonical Studio design](../../docs/architecture/2026-09-09%20-%20SMILE%202.0%20Studio.md)
remains the product design. S1 enables Viewer and Character Editor; other workspace
labels are unavailable, and do not create hidden editors or loaders. The shell follows
the [working visual design](../../docs/architecture/studio-visual-design.md): navy
workspace navigation, left Scene Explorer and Asset Browser, a central live viewport,
adjacent Inspector, right Scene/Code/Visual area, lower Timeline and bottom status.
The Explorer describes the active preview; the character buttons select existing
Viewer profiles. A generic resource catalog and scene hierarchy editing remain planned.
The Timeline displays the actual current clip and time, with working transport and
frame controls. It does not imply sequence authoring or source/visual synchronization.

## Build and launch

From the repository root, using PowerShell 7 (`pwsh`):

```powershell
& tools/SmileStudio/Build.ps1 -Target All
& tools/SmileStudio/Launch.ps1
```

For Visual Studio, first prepare the ignored local asset inputs (also done by every
normal Studio build):

```powershell
& tools/SmileStudio/Build.ps1 -PrepareOnly
```

Open [SmileStudio.slnx](SmileStudio.slnx). Its permanent
[SmileStudio.smileproj](SmileStudio.smileproj) exposes `Program.smile`,
`StudioShell.smile` and links to the existing Viewer modules. Select **Debug** and
**Windows 64-bit .exe**, then use **F5** to build and debug. Release and Web quality
configurations are also present. CLI Debug builds emit the same native debug
information. Preparation is needed again after changing canonical asset inputs;
ordinary source edits build directly from Visual Studio.

Close any running standalone Viewer or Studio normally before F5 because both use
the same saved calibration identity. For everyday editing, `Launch.ps1` handles
that handover and runs the existing calibration synchronization watcher.

Outputs are `tools/SmileStudio/bin/Release/SmileStudio.exe` and
`tools/SmileStudio/bin/Release/Web/index.html`. Serve the Web directory with a local
HTTP server and open it in Chrome. These generated files are ignored. The build
checks the permanent project's inventory against Character3DViewer's shared
sources/assets and reuses its private-roster checks. Project-local assets and
cooking inputs are disposable ignored copies; sources and canonical assets keep
their original owners. Visual Studio Web configurations include the full local
diagnostic roster. The scripted Web build omits native-only diagnostic profiles;
`-PublicRoster` remains an explicit separate publication. No licensed assets move.
The mandatory SMILE splash, credits, links and artifact version/time appear on both
targets and every tab reload.

## Existing workflow

- Select a character in the Asset Browser, then a clip in the Inspector. Viewport and Character Editor show the same live
  scene, camera and selection. Selecting a clip pauses it for inspection.
- Click inside the viewport for camera/keyboard focus. LMB uses the Pan/Orbit toolbar tool, MMB always orbits, the wheel
  eases zoom, Space toggles pause, R resets the view, and Esc stops the preview.
  Inspector/navigation clicks and pointer exit cancel camera capture.
- In Character Editor, Arin/Orin expose wrist/equipment rotation target and X/Y/Z
  channel choices, previous/next frame, and one-degree minus/plus controls. Save Frame
  persists through the existing calibration owner. Undo cancels a pending preview or
  undoes the last saved correction. S1 does not add a new animation or asset format.
- Pending edits block tab, clip and frame changes. Workspace changes and Stop retain
  them. Stop freezes animation/VFX and silences audio; Resume primes the clock.
- Close offers Save And Close, Discard and Cancel for a pending pose. A failed save
  keeps the preview and dialog open. Native X/Alt+F4 route to this same confirmation.
  Browser tab close/reload uses the browser's standard unsaved-work prompt, subject
  to browser interaction policy. It cannot recover a forcibly terminated browser.

The inspector reduces its visible clip rows at compact heights. At less than
1200 x 720 logical pixels it asks for a larger window and preserves the session.
The host uses the existing logical-canvas/DPI transform, not physical pointer pixels.

## Ownership and persistence

There is one window, active viewport, frame loop, ViewerWorkflow.Session and renderer.
`Program.smile` owns the host loop and close decision. `StudioShell.smile` owns only
layout and UI commands. `ViewerWorkflow.smile` binds the existing typed Viewer owners;
standalone `Character3DViewer/Program.smile` uses that same implementation.
The [Viewer architecture](../Character3DViewer/ARCHITECTURE.md) maps the public seams.

`Graphics3D.SetViewport3D(X, Y, Width, Height)` admits a positive rectangle within
current logical window bounds, only outside Begin/End3D. Invalid requests return
False and keep the previous rectangle. `ResetViewport3D()` restores the full canvas
between frames. A renderer reset also restores the default. This is one region of
the existing engine; 2D chrome is drawn after the 3D frame.

Both hosts use the existing `smile.tools.character3d-viewer` save identity. Official
launchers gracefully close any other repo-owned Viewer/Studio process before opening
one, export live Arin/Orin keyframes and keep the same synchronization watcher.
They abort replacement if an unsaved-work prompt is pending; they never force-kill it.
Recovery files, canonical packages and original Dragon restore/trial data are unchanged.

## Focused validation

```powershell
& scripts/test-studio-session.ps1
& scripts/test-character-3d-viewer-hardening.ps1
& scripts/test-viewer-calibration-native.ps1 -IncludeWebPrecision
```

SessionTests uses the actual shared class with disposable application identities.
It checks successful Arin/Orin saves, Undo, failed Arin atomic replacement, dirty
switch prevention, Stop/Resume clock continuity and final resource counts. Native
execution and generated-Web Node logic are reported separately. The script prints
WebSuccess/WebWriteFailure directories for actual Chrome execution; Node alone
cannot prove rendering or browser interaction. Existing calibration and hardening
regressions remain present. Use isolated storage for destructive/failure testing.

S1 excludes Scene Editor, Water, new document loaders, asset/VFX/audio authoring,
declarative grammar and export. Those need their own approved milestone.

### Current S1 acceptance (September 10, 2026)

Implemented and validated: the shared hosted workflow, permanent Visual Studio
project/solution and working-design shell. Full S1 interaction acceptance remains
in progress; the next action is the human camera check below, not a new feature phase.

- `scripts/smoke-test.cmd --skip-doctor` passed for the shared compiler/runtime
  changes (`artifacts/temp/studio-project-integration.log`). The later shell-only
  changes passed the focused session checks and final native/Web builds.
- Final `scripts/test-studio-session.ps1` passed native and generated-Web logic.
  Both actual Chrome fixtures also reported `Studio session isolation passed.`
  Save/Undo, rejected save, dirty selection guards, transport, clock continuity,
  cleanup and actual shell-layout bounds use disposable save identities.
- Native UI observation confirmed pose nudge, dirty-close confirmation, Cancel and
  Undo without saving over live calibration. Chrome observation confirmed viewport
  pan and keyboard reset, responsive panel layout and no reported console errors.
  Automated geometry checks cover the 1200 x 720 minimum; they are not mouse tests.
- Studio and standalone Viewer Release builds passed for native and Web. Final
  Studio artifacts: `bin/Release/SmileStudio.exe` SHA-256
  `A5C113B3BBB466CBF10CD6DDDA53D72439C3B6500FCA4A159780ED024B5E72EC`;
  `bin/Release/Web/game.js` SHA-256
  `BE9C64829165DA933F27970291FF79CA27DE6BB62777CAEC1BE1965AF5A383E1`.
- Visual Studio loaded the permanent solution, built and launched native Debug,
  hit a source breakpoint and stepped. VSIX 2.0.63 was installed afterward and
  its 35 compiler/language/library/template payload hashes matched the built VSIX.
  Subsequent tool-source changes did not alter that payload.
- Calibration preservation checks passed. Pre-publication Arin/Orin exports
  retained their canonical 24/0 keys without changes to live saves or asset sources.

**Pending human observation:** in both hosted native Studio and Chrome Studio,
try slow and moderate horizontal/vertical MMB orbit, LMB pan, wheel zoom in/out
and R reset. Confirm smooth motion, clean release and no jump or stuck capture.
The available mouse tool cannot reproduce MMB dragging. These observations are
not inferred from the standalone Viewer's earlier acceptance or numeric tests.
