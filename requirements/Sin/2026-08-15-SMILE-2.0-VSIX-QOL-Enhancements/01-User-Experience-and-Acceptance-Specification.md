# SMILE 2.0 VSIX Quality-of-Life Enhancements — User Experience and Acceptance Specification

**Status:** Approved implementation handoff
**Date:** August 15, 2026
**Scope:** Existing SMILE 2.0 Visual Studio extension and shared language services

## 1. Goal

Make the working SMILE 2.0 Visual Studio experience substantially more helpful to a student without replacing the existing architecture.

The primary example is:

```smile
Import Smile.UI.Menu As Menu

Dim MainMenu As Number
MainMenu = Menu.Create(MenuStyle, 80, 70, 480, 300, 6)
```

The student should be able to:

- hover over `Menu` and understand that it is an imported module alias;
- hover over `Create` and understand the routine, each argument, its return value, and where it is defined;
- press F12 while the caret is on `Menu` and reach the `Module Smile.UI.Menu` declaration;
- press F12 while the caret is on `Create` and reach the `Public Function Create(...)` declaration;
- right-click the runnable SMILE project and make it the solution startup project.

## 2. Terminology

This milestone distinguishes two different startup concepts:

- **Startup project:** the project Visual Studio launches when the user presses F5 or Ctrl+F5.
- **Startup source:** the `.smile` source file selected by a SMILE application project as its entry/startup source.

The existing source-level command must remain functional. The new project-level command must never rewrite `<StartupFile>` or change which `.smile` file is the project's entry source.

## 3. Educational hover Quick Info

### 3.1 Hovering an imported module alias

Hovering `Menu` in `Menu.Create` must produce useful Quick Info similar to:

```text
Module Smile.UI.Menu
Imported as Menu

Reusable menu creation, item management, input handling, and drawing services.

Defined in:
libraries\Smile.UI\Menu.smile
```

Minimum required content:

- full module name;
- local import alias;
- source provider/library when known;
- physical source file when known.

A module summary is shown when documentation exists. A useful module identity and source are still shown when no summary exists.

### 3.2 Hovering a routine or function

Hovering `Create` in `Menu.Create` must produce Quick Info equivalent in meaning to:

```text
Function Smile.UI.Menu.Create(
    ByRef Style As Core.MenuStyle,
    X As Number,
    Y As Number,
    Width As Number,
    Height As Number,
    VisibleRows As Number
) As Number

Creates a menu instance using the supplied style, position, size, and requested visible-row count.

Parameters
Style — Menu appearance, window, text, cursor, spacing, and overflow settings. Passed by reference.
X — Left edge of the menu in game-window coordinates.
Y — Top edge of the menu in game-window coordinates.
Width — Menu width in pixels.
Height — Menu height in pixels.
VisibleRows — Requested number of rows visible at once. The effective count may be reduced to fit.

Returns
A positive generation-safe menu handle, or 0 when the style is invalid or no menu slot is available.

Defined in:
libraries\Smile.UI\Menu.smile
```

The exact typography may use Visual Studio classified text and stacked sections, but all of the following information is required:

- symbol kind (`Function`, `Sub`, module, type, variable, constant, or field as applicable);
- fully qualified name when applicable;
- complete parameter list in declaration order;
- `ByRef` where applicable;
- parameter types;
- return type for a function;
- plain-language summary when documented;
- a plain-language explanation for every documented parameter;
- return-value explanation when documented;
- capability note such as “requires Game Window” when the semantic model reports it;
- module/provider and physical source file when known.

### 3.3 Undocumented symbols

Documentation will be added incrementally. Quick Info must remain useful when a symbol has no documentation comment.

For an undocumented routine, show at least:

- the complete signature;
- module/provider;
- source file;
- `ByRef`, types, and return type;
- capability information already available from the semantic model.

Do not invent semantic meanings from parameter names. Absence of prose documentation must not produce misleading generated prose.

### 3.4 Symbol coverage required now

Required for this milestone:

- imported module aliases;
- qualified public functions and subroutines;
- unqualified functions and subroutines in the current compilation;
- global/module variables and constants;
- record types and record fields when the shared semantic model can resolve them without a separate parser;
- routine parameters and local variables;
- built-in functions/keywords may continue using existing completion descriptions when no declaration exists.

The first three entries are release-blocking. The remaining symbol categories should use the same shared resolver and are expected unless the current semantic model lacks a reliable binding. Any deliberately deferred category must be documented in the final report.

### 3.5 Hover behavior and safety

- Match SMILE's case-insensitive symbol rules.
- Use the identifier token under the mouse trigger point as the applicable tracking span.
- Treat a trigger exactly at the end of an identifier as belonging to that identifier when Visual Studio supplies that position.
- Do not show SMILE symbol Quick Info inside a string literal or ordinary comment.
- Respect cancellation.
- Do not block the UI thread for file-system scans, project loads, or builds.
- Reuse current project/open-buffer analysis so unsaved edits are reflected after the normal analysis debounce.
- Return no item rather than throwing when the source is temporarily malformed.
- Do not show modal error dialogs during hover.

## 4. F12 Go To Definition

### 4.1 Imported module alias

Given:

```smile
Import Smile.UI.Menu As Menu
MainMenu = Menu.Create(MenuStyle, 80, 70, 480, 300, 6)
```

With the caret on `Menu` in `Menu.Create`, F12 must:

1. resolve the import alias through the current file's semantic import map;
2. open the physical source that declares `Smile.UI.Menu`;
3. position and select the module identifier in:

   ```smile
   Module Smile.UI.Menu
   ```

When a module is declared across more than one physical source, choose its first deterministic declaration location. The implementation may prefer the source containing the selected member when that is more useful and deterministic.

### 4.2 Qualified member

With the caret on `Create` in `Menu.Create`, F12 must:

1. resolve `Menu` as the imported module alias;
2. resolve `Create` from that module's accessible public members;
3. open the member's physical source;
4. position and select the `Create` identifier in its declaration.

This must work across:

- another source in the same project;
- a directly referenced `.smilelibproj` whose source is available;
- a loaded referenced library source already included by `SmileProjectWorkspace`.

### 4.3 Other definitions

The shared resolver should also allow F12 for the symbol classes listed under Quick Info when reliable declaration locations already exist in the semantic model.

Required examples:

```smile
Call LocalSubroutine()
Dim Player As Models.Player
Print Player.Name
```

Where the symbols resolve, F12 should navigate to the local routine, type, or field declaration.

### 4.4 Unresolved or unavailable definitions

- If the caret is not on a resolvable SMILE symbol, the SMILE command handler must return “not handled” so the next Visual Studio handler can run.
- If the symbol is semantically known but its package has no physical source path, do not crash and do not open a fake file.
- For known-but-unavailable source, show a concise non-modal status-bar message such as:

  ```text
  SMILE definition found, but source is not available for this library package.
  ```

- Do not use a modal message box for normal unresolved/navigation cases.
- Built-ins without physical source do not need a fake definition file in this milestone.

### 4.5 Navigation details

- Open the target through supported Visual Studio document services.
- Convert SMILE's one-based `SourceLocation.Line` and `Column` to the zero-based editor coordinates expected by Visual Studio.
- Select the declaration identifier when practical; otherwise place the caret at its start.
- Bring the target view to the foreground and ensure the destination is visible.
- Source files outside the current Solution Explorer hierarchy may open as normal miscellaneous documents.
- F12 must not build or save the project.

## 5. Set as Startup Project

### 5.1 Project context command

Right-clicking the root node of a runnable `.smileproj` must show:

```text
Set as Startup Project
```

Recommended placement: near the existing Build/Rebuild/Clean commands.

### 5.2 Behavior

Selecting the command must set that SMILE hierarchy as the actual solution startup project used by Visual Studio for F5 and Ctrl+F5.

The command must:

- operate on the clicked project root;
- use Visual Studio's solution build manager;
- preserve the project's `<StartupFile>`;
- preserve configuration/platform selection;
- work when the solution contains two or more SMILE application projects;
- update the current startup indication through normal Visual Studio behavior.

### 5.3 Status and visibility

- Visible and enabled for runnable SMILE application projects.
- Hidden for `.smilelibproj` library projects because they cannot launch.
- If the clicked project is already the startup project, it may be shown latched/checked or disabled; either is acceptable if the state is obvious and stable.
- Failure to obtain the solution build manager should be logged and reported once through the existing project-command error path.

### 5.4 Existing source startup command

The source-node command currently labeled `Set as Startup` must continue to choose the startup `.smile` source inside a project.

To prevent student confusion, its visible text should become:

```text
Set as Startup Source
```

No project-file behavior may change beyond the label unless needed to fix an existing defect discovered by focused testing.

## 6. Compatibility requirements

The implementation is unacceptable if it breaks any of the following:

- completion after identifiers or a dot;
- syntax coloring;
- diagnostic squiggles;
- opening `.smile` through **File > Open**;
- double-click opening from Solution Explorer;
- project refresh;
- adding/removing source files or references;
- source-level startup selection;
- native Windows build;
- Web build/publish;
- source-level debugging and breakpoints for the Windows executable;
- project templates.

The new services must use the current SMILE content type rather than registering a competing content type.

## 7. Acceptance scenarios

### Scenario A — Hover `Menu`

1. Open a project that imports `Smile.UI.Menu As Menu`.
2. Hover the `Menu` token in `Menu.Create`.
3. Quick Info identifies `Smile.UI.Menu`, the alias `Menu`, its provider, and source file.
4. No build or file save occurs.

### Scenario B — Hover `Create`

1. Hover `Create` in `Menu.Create`.
2. Quick Info shows the complete signature.
3. Every `Create` parameter has a meaningful explanation.
4. The return value is explained.
5. The source module/file is shown.

### Scenario C — F12 module

1. Put the caret on `Menu`.
2. Press F12.
3. `libraries\Smile.UI\Menu.smile` opens.
4. The caret/selection is on `Smile.UI.Menu` in the module declaration.

### Scenario D — F12 member

1. Put the caret on `Create`.
2. Press F12.
3. `libraries\Smile.UI\Menu.smile` opens.
4. The caret/selection is on the `Create` declaration.

### Scenario E — Unsaved referenced source

1. Open `Menu.smile`, edit the `Create` declaration without saving, and leave the file open.
2. Return to the consuming project after analysis updates.
3. Hover/F12 use the shared open-buffer workspace and do not load a conflicting stale copy.
4. Restore the edit after the test.

### Scenario F — Startup project

1. Open a solution with two runnable SMILE application projects.
2. Right-click the second project and choose **Set as Startup Project**.
3. Visual Studio recognizes the second project as startup.
4. F5 builds/launches the second project.
5. Neither project's `<StartupFile>` changes.

### Scenario G — Library project

1. Right-click a `.smilelibproj`.
2. **Set as Startup Project** is not offered.

### Scenario H — Regression smoke

Verify:

- completion for `Menu.` still lists public members;
- File > Open opens a `.smile` file;
- Solution Explorer double-click opens a `.smile` file;
- a native breakpoint binds and is hit;
- one native project builds/runs;
- one Web project builds/publishes;
- source-level **Set as Startup Source** still changes only the startup source.

## 8. Out of scope

This milestone does not require:

- Ctrl+Click navigation;
- Peek Definition;
- Find All References;
- Rename Symbol;
- a navigation history UI;
- generated source/decompilation for binary-only packages;
- a Language Server Protocol implementation;
- migration to Roslyn;
- a new project system;
- exhaustive documentation of every SMILE standard library in one pass.

The architecture should allow those features later without duplicating semantic rules.
