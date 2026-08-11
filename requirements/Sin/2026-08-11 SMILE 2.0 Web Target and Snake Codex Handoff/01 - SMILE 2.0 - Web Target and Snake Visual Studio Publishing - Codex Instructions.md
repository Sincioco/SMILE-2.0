# SMILE 2.0 — Web Target, Visual Studio Multi-Target Publishing, and Snake Browser Milestone

## Codex implementation instructions

**Repository:** `D:\SMILE 2.0`  
**GitHub repository:** `Sincioco/SMILE-2.0`  
**Project identity:** This task is exclusively for **SMILE 2.0**, the new native compiler project with ten games.  
**Do not inspect, modify, migrate, or preserve compatibility with the original SMILE/SMILE 1.0 repository.**

---

## 1. Read this first

1. Open `D:\SMILE 2.0`.
2. Read the current repository-root `AGENTS.md` completely before editing anything.
3. Inspect the latest committed implementation before deciding exact file changes. The repository may be newer than this handoff.
4. Check `git status` and preserve all existing user work. Never discard or overwrite unrelated uncommitted changes.
5. Continue through implementation, focused validation, Visual Studio integration, browser launch, commit, and push without asking Sin to confirm intermediate steps.
6. Assume the happy path and use minimal, focused testing. Do not ask Sin to perform testing for you.
7. If a real crash, hang, debugger failure, browser failure, intermittent defect, or other known problem requires broader or longer testing, record this before expanding validation:

```text
Known problem being investigated:
Why the longer test is necessary:
Stop condition:
```

Do not run broad or long tests merely for reassurance.

---

## 2. Mission

Add a first-class **Web** compilation target to SMILE 2.0 without removing or destabilizing the existing native Windows x64 target.

The first complete browser conversion is the existing Snake game:

```text
games\Snake\Snake.smileproj
games\Snake\Program.smile
games\Snake\Program-NoDemo.smile
games\Snake\Assets\...
```

The same existing Snake project and same `.smile` source must be usable for both targets:

```text
Debug | x64  -> existing native Snake.exe
Debug | Web  -> publish-ready browser game
```

There must not be a second Snake web project, a copied web-only Snake source file, or hard-coded Snake logic in the compiler/runtime.

### Successful user experience

From Visual Studio 2026 Enterprise, Sin must be able to:

1. Open the existing `games\Snake\Snake.slnx` and `Snake.smileproj`.
2. Leave the target/platform as `x64`, build normally, and receive the existing native `.exe` behavior.
3. Select `Web` as the target/platform in the normal Visual Studio configuration/platform UI.
4. Choose **Build Solution** and have Visual Studio publish a static browser game under the Snake project’s output folder.
5. Press `F5` or `Ctrl+F5` while `Web` is selected and have Visual Studio:
   - build the Web output when necessary;
   - start a loopback-only local static-file server;
   - open the published `index.html` through an `http://127.0.0.1:<port>/` URL in the default browser;
   - run a playable Snake game on the web page.
6. Switch back to `x64` and continue building/debugging the Windows game as before.

For this milestone, **Build with the Web platform selected is the Visual Studio publish operation**. Do not add a large publish wizard. The resulting Web folder must already be deployable to any static web host. Actual upload to GitHub Pages, Azure, Cloudflare, Netlify, or another remote host is out of scope because no hosting account or credentials are part of this task.

---

## 3. Non-negotiable requirements

### 3.1 Repository and project identity

- Work only in `D:\SMILE 2.0` / `Sincioco/SMILE-2.0`.
- Never use code or assumptions from the original `Sincioco/SMILE` project.
- `src\Smile.Language` remains the single language authority.
- Do not create a second parser, semantic analyzer, keyword table, or completion catalog for Web.

### 3.2 Preserve native Windows behavior

The existing Windows path is the compatibility baseline.

- The compiler must still default to native Windows x64 when no target is specified.
- Existing commands must continue to work without modification, including:

```text
smilec.exe games\Snake\Program.smile -o Snake.exe
```

- Keep the existing Visual Studio platform name `x64`. Do not rename it to `Windows`, `Windows-x64`, `Native`, or anything else in this milestone.
- Keep the current native output location and naming convention. For example:

```text
games\Snake\bin\Debug\Snake.exe
games\Snake\bin\Debug\Snake.pdb
```

- Do not silently move native output into a new `x64` subfolder.
- Preserve the current MASM emission, native runtime linking, DirectX/GDI options, VSync behavior, debug helper generation, PDB generation, and native debugger launch.
- The existing `--graphics`, `--vsync`, `--debug`, and `--keep-temp` behavior must remain valid for native output.
- Do not make the native compiler depend on any browser files, JavaScript runtime, web server, npm tool, or Web output folder.

### 3.3 Preserve recently repaired Visual Studio behavior

The following behaviors were recently fixed and are mandatory regression guardrails:

1. **Windows `.exe` breakpoints continue to bind and hit in `.smile` source.**
2. **SMILE IntelliSense continues to work.**
3. **A `.smile` file opened through `File > Open > File` opens in the normal Visual Studio text editor with SMILE content type/language services.**
4. **Double-clicking a `.smile` file in Solution Explorer opens it correctly.**
5. **The existing Tools > Build SMILE File command remains functional and remains native by default.**

Do not replace, bypass, or broadly rewrite these working areas unless a very small change is truly required:

```text
src\Smile.VisualStudio\SmileContentType.cs
src\Smile.VisualStudio\SmileCompletionSource.cs
src\Smile.Language\Completion.cs
src\Smile.VisualStudio\SmileProjectSystem.cs
src\Smile.VisualStudio\SmileBuildService.cs
src\Smile.Compiler\CompilerDriver.cs
src\Smile.Compiler\MasmEmitter.cs
src\Smile.Compiler\NativeToolchain.cs
```

In particular, preserve the current `.smile` content-type registration, `OpenSpecificEditor` path, hierarchy item opening, native `IVsDebuggableProjectCfg` behavior, `--debug` propagation, generated debug source mapping, PDB creation, and native debug-engine selection.

### 3.4 One project, multiple targets

- Do not create `Snake.Web.smileproj`.
- Do not create `Program-Web.smile`.
- Do not duplicate the `Assets` folder.
- Do not fork game logic into a hand-written JavaScript Snake implementation.
- Do not modify every one of the ten game projects in this milestone.
- The existing `ProjectKind=Game` project becomes multi-target capable through the project system.
- Update `Snake.slnx` only if Visual Studio genuinely requires a minimal solution-platform mapping change after the project configuration provider exposes `Web`.

### 3.5 Keep the first Web implementation small

Use:

- plain HTML;
- plain CSS;
- pure JavaScript;
- an HTML `<canvas>`;
- the Canvas 2D API;
- browser keyboard events;
- browser audio facilities;
- browser local storage.

Do not add:

- React, Angular, Vue, Blazor, Electron, or another framework;
- npm, Node-based bundling, webpack, Vite, Rollup, or package restoration;
- WebAssembly for this first milestone;
- WebGL for this first milestone;
- a custom web IDE;
- a new SMILE keyword;
- a Web Component requirement;
- cloud deployment logic;
- a second compiler project unless the current architecture makes it absolutely unavoidable.

The official target is named **Web**, not WebGL. Canvas 2D is the first Web renderer. WebGL can be considered later after the shared browser target works.

### 3.6 No silent miscompilation

The first Web backend only has to support the complete language/runtime subset exercised by:

```text
games\Snake\Program.smile
games\Snake\Program-NoDemo.smile
```

However:

- implement that subset generically from syntax and semantic information;
- never detect the file name `Snake` and emit special behavior;
- never hard-code Snake variables, routines, coordinates, colors, or rules;
- when a valid SMILE program uses a statement that the first Web backend does not yet support, emit a clear Web-target diagnostic with file, line, and column instead of crashing or generating wrong JavaScript.

Target-specific support diagnostics belong in the compiler/Web backend, not in the shared SMILE language grammar.

---

## 4. Current architecture to extend, not replace

The intended pipeline is:

```text
Program.smile
    -> shared Smile.Language lexer/parser/semantic model
    -> target selection
        -> Windows x64 target
            -> existing MasmEmitter
            -> existing NativeToolchain
            -> existing Smile.NativeRuntime.lib
            -> .exe + optional .pdb

        -> Web target
            -> new generic JavaScript emitter
            -> new small browser runtime
            -> HTML/CSS/JavaScript files
            -> declared project assets copied beside them
```

`Smile.Language` must remain target-independent. The Web emitter consumes the same `SmileAnalysisResult`, syntax tree, symbols, routine symbols, expression types, constants, and diagnostics already consumed by the native compiler.

Prefer a small target dispatch inside the existing compiler project. A reasonable organization is:

```text
src\Smile.Compiler\SmileCompilationTarget.cs
src\Smile.Compiler\WebEmitter.cs
src\Smile.Compiler\WebTargetValidator.cs
src\Smile.Compiler\WebOutputWriter.cs       (only if useful)
src\Smile.Compiler\WebTemplates\...         (only if simpler than embedded strings)
src\Smile.VisualStudio\SmileWebServer.cs
```

Exact names may follow the current repository style. Do not create abstractions merely to match this list.

---

## 5. Compiler command-line target support

### 5.1 Add target selection

Add a small target enum/model such as:

```csharp
WindowsX64
Web
```

Support:

```text
--target windows-x64
--target web
```

Target names are case-insensitive.

The default remains `windows-x64`, so all pre-existing commands and scripts continue to produce `.exe` output.

### 5.2 Web output directory

A Web program contains several files. Add a clear Web output-directory option:

```text
--output-dir <directory>
```

Example:

```text
artifacts\compiler\smilec.exe games\Snake\Program.smile --target web --output-dir games\Snake\bin\Debug\Web
```

Rules:

- Native Windows continues to use `-o <output.exe>`.
- Web uses `--output-dir <directory>`.
- Give a clear compiler error for contradictory combinations rather than guessing.
- A Web build must not search for or require `Smile.NativeRuntime.lib`.
- Native compilation must not create Web files.
- Keep existing diagnostic formatting compatible with the Visual Studio Output window and Error List:

```text
path(line,column): error SMLxxxx: message
```

### 5.3 Isolate the current native path

Refactor only enough to dispatch cleanly:

```text
Analyze once
If target is WindowsX64:
    execute the existing MASM/native path
If target is Web:
    execute the Web validation/emission path
```

Do not rewrite `MasmEmitter` as part of this milestone. Do not change native code generation merely to make the Web backend look symmetrical.

### 5.4 Web compiler output

Generate a deterministic publish folder such as:

```text
Web\
    index.html
    smile-runtime.js
    game.js
    smile.css
    Assets\
        Start.wav
        Eat.wav
        GameOver.wav
```

The compiler creates the four generated text files. The Visual Studio project build copies project-declared asset trees into the output folder while preserving relative paths.

Use UTF-8 without a BOM unless the existing repository convention requires otherwise.

All references in generated HTML/JavaScript must be relative. Never emit a `D:\SMILE 2.0\...` path into published output.

---

## 6. Generic JavaScript code generation required for Snake

### 6.1 Symbol handling

Use the shared semantic model to distinguish:

- global variables;
- constants;
- arrays;
- routine parameters;
- routine locals;
- subroutines;
- functions.

SMILE identifiers are case-insensitive. JavaScript identifiers are case-sensitive and have reserved words. Generate collision-safe JavaScript names from symbol identity rather than relying on raw source names alone.

Do not create a second symbol table with different language rules.

### 6.2 Value representation

For this first Web milestone, JavaScript `Number` is acceptable because Snake’s values remain within JavaScript’s safe integer range.

Required behavior:

- preserve integer-only arithmetic;
- implement `/` as integer division truncating toward zero;
- preserve `MOD` behavior;
- preserve inclusive `RANDOM ... FROM ... TO ...` behavior;
- represent SMILE BOOLEAN runtime values compatibly with the existing native `0`/`1` behavior;
- generate conditions by testing the SMILE Boolean value, not JavaScript object truthiness;
- use clear safe-integer checks rather than silently producing inaccurate values outside JavaScript’s exact integer range.

A valid Web build that contains an unsafe integer literal or encounters an unsafe integer result must fail clearly rather than pretending to provide exact signed-64-bit semantics. Document this as an initial Web-target limitation. Do not add BigInt complexity unless it proves simpler and is fully validated.

### 6.3 Arrays

Support zero-initialized, fixed-size:

- one-dimensional NUMBER arrays;
- two-dimensional NUMBER arrays.

Preserve the existing dimension and index order. Use a generic representation such as flat arrays with calculated offsets or arrays of arrays. Do not specialize for Snake.

### 6.4 Statements and control flow

The Web emitter must correctly generate the forms used by both Snake sources:

- top-level assignments and implicit variable declarations as defined by the semantic model;
- `CONST`;
- `DIM` with one or two dimensions;
- scalar and array-element assignment;
- `IF / ELSE IF / ELSE / END IF`;
- ascending `FOR`;
- descending `FOR ... DOWN TO`;
- post-test `DO / LOOP UNTIL`;
- `SUB`, `FUNCTION`, parameters, `CALL`, and `RETURN`;
- `GET KEY`;
- `RANDOM`;
- `LOAD ... DEFAULT` and `SAVE`;
- `GAME WINDOW`;
- `CLEAR <color>`;
- filled and outlined rectangles;
- filled and outlined rounded rectangles;
- `DRAW TEXT`;
- `DRAW NUMBER`;
- `PLAY SOUND` and `STOP SOUND`;
- `SHOW SCREEN`;
- `END PROGRAM`.

Also emit the expression forms Snake uses:

- numeric, Boolean, and text literals where valid;
- variables, constants, and array elements;
- unary minus and `NOT`;
- `+`, `-`, `*`, integer `/`, `MOD`;
- comparisons and equality;
- `AND` and `OR` with correct short-circuit behavior;
- parentheses;
- routine calls;
- `TIMER()`;
- `ABS()`;
- `MIN()`;
- `MAX()`;
- `RGB()`;
- `GAME_CLOSED()`;
- built-in key and color constants.

Unsupported valid statements must produce a Web-target diagnostic rather than a `NotImplementedException`, null reference, invalid JavaScript, or silent omission.

### 6.5 Loop/frame lowering

Do not emit Snake’s outer game loop as a synchronous JavaScript busy loop. That would freeze the browser.

The generated program must cooperate with the browser event loop. `SHOW SCREEN` is the frame boundary and must yield until a browser animation frame.

A suitable conceptual result is:

```javascript
async function smileMain() {
    // generated setup

    do {
        // generated update and drawing logic
        await smile.showScreen();
    } while (!smile.gameClosed());
}
```

Requirements:

- `SHOW SCREEN` presents the completed frame and yields with `requestAnimationFrame`.
- The browser continues processing keyboard, audio, resize, and paint events.
- Do not convert the game into timer callbacks with game-specific state machines.
- Do not rewrite `Program.smile` to accommodate browser scheduling.
- If future yielding statements occur inside routines, the emitter may mark only the required routines/call sites async. Do not build a generalized coroutine framework beyond what this milestone needs.

---

## 7. Browser runtime requirements

Create a small generic browser runtime used by generated Web games.

### 7.1 Canvas and presentation

- Create one visible HTML canvas.
- Use the logical dimensions declared by `GAME WINDOW`; retain the current default logical canvas when size is omitted. Snake uses the existing 960-by-540 logical game space.
- Preserve the logical coordinate system.
- Scale responsively in CSS while keeping the aspect ratio.
- Center the game and letterbox with a neutral/black surrounding background when needed.
- Use a separate in-memory canvas as a back buffer if needed so `SHOW SCREEN` presents complete frames rather than partially drawn frames.
- Use Canvas 2D only for this milestone.
- Use a normal system font stack led by Segoe UI for text.
- Match current SMILE text positioning and centering closely enough that the Snake title, score panel, board, and game-over overlay are readable and correctly placed.

### 7.2 Colors

SMILE’s current color values use Windows `COLORREF` byte order. Convert correctly for the browser:

```text
red   = color & 0xFF
green = (color >> 8) & 0xFF
blue  = (color >> 16) & 0xFF
```

Do not treat the values as ordinary `0xRRGGBB` without conversion. `RED`, `BLUE`, `RGB(...)`, and all named colors must display correctly.

### 7.3 Drawing primitives needed by Snake

Implement generic runtime operations for:

- clear/fill background;
- fill rectangle;
- draw rectangle;
- fill rounded rectangle;
- draw rounded rectangle;
- draw text;
- draw number;
- present/show screen.

Use logical one-pixel outlines unless the existing native behavior indicates another clear convention.

### 7.4 Keyboard input

Implement a generic keyboard layer that returns the existing SMILE key constant values. Do not duplicate or invent different key values.

Map at least:

- W, A, S, D;
- arrow keys;
- Enter;
- Escape;
- Space;
- 1 and 2;
- ordinary unrecognized key presses to `KEY_OTHER`;
- no queued key to `KEY_NONE`.

Requirements:

- `GET KEY` is non-blocking and consumes one queued key event or returns `KEY_NONE`.
- Prevent default browser scrolling for game-control keys while the game is active.
- Ignore or deliberately handle key-repeat consistently.
- Clear held/queued state appropriately on focus loss.
- Capture enough ordinary keys that Snake’s attract/demo cancellation works.
- Clicking the canvas focuses the game.
- Page-level keyboard handling should allow `Press Enter to Start` without requiring a tiny hidden text input.

### 7.5 Full screen

Map Alt+Enter to the browser Fullscreen API when allowed by the browser.

- Treat the key event as the user gesture needed by the Fullscreen API.
- Preserve aspect ratio.
- Do not fail or crash when a browser declines the request.
- Browser Escape behavior may leave full screen before the game receives Escape; handle this safely.

### 7.6 Timing and random values

- `TIMER()` returns monotonically increasing integer milliseconds based on `performance.now()` or an equivalent browser clock.
- `RANDOM` is inclusive at both ends, matching the existing SMILE contract.

### 7.7 Sound

Snake uses WAV effects through `PLAY SOUND`.

Implement the existing one-effect-at-a-time model with browser audio:

- normalize SMILE backslashes to URL forward slashes;
- stop/restart the previous effect as required by current behavior;
- `STOP SOUND` stops the current effect;
- catch browser `play()` rejection instead of producing an unhandled promise rejection;
- audio that is blocked before the first user interaction may be suppressed, but the game must continue without crashing;
- after a user key/click unlocks audio, later effects should play;
- on window blur or document invisibility, stop the active effect and suppress new effects until active, mirroring the current shared focus policy as closely as practical.

Do not add music support merely for Snake unless it falls out naturally from a tiny generic design. MP3 music can be a later Web-target milestone.

### 7.8 Persistence

Map:

```smile
LOAD HighScore FROM "HighScore" DEFAULT 0
SAVE HighScore TO "HighScore"
```

onto browser `localStorage`.

Requirements:

- scope keys by the generated game/output identity so different games do not collide;
- preserve signed integer text values;
- use the specified default if the key is absent or corrupt;
- catch storage security/quota failures and continue safely with the default/in-memory value;
- never require a server database.

### 7.9 Program lifecycle and errors

- `GAME_CLOSED()` returns false while the browser game is running and true once the runtime has stopped.
- `END PROGRAM` stops the generated game loop cleanly. A browser tab usually cannot close itself; leaving the final frame or showing a small stopped/reload message is acceptable.
- Unhandled runtime errors must be written to the browser console and shown in a small readable error panel rather than leaving a blank page.
- Expose a small generic status hook for focused automated validation, for example:

```javascript
window.__smileWeb = {
    status: "starting" | "running" | "stopped" | "error",
    frameCount: 0
};
```

This hook must be generic and must not contain Snake state or cheats.

---

## 8. Visual Studio project-system changes

### 8.1 Add Web beside x64

The existing project system currently exposes `Debug`, `Release`, and one platform, `x64`.

Extend it to expose these four canonical project configurations:

```text
Debug|x64
Release|x64
Debug|Web
Release|Web
```

Requirements:

- `x64` remains first/default.
- `GetCfgNames` still reports the distinct configuration names `Debug` and `Release`.
- `GetPlatformNames` and `GetSupportedPlatformNames` report `x64` and `Web`.
- `GetCfgOfName` respects both configuration and platform.
- `OpenProjectCfg` parses both parts of the canonical name.
- `SmileProjectConfiguration` stores both configuration and platform.
- `get_CanonicalName` returns the actual pair.
- Build, clean, up-to-date checks, query launch, and launch use the selected platform.
- Keep the project type GUID unchanged.
- Keep existing `.smileproj` files loadable without adding a required target property.

Do not rename or remove `Debug`, `Release`, or `x64`.

### 8.2 Target-aware output paths

Keep native paths unchanged:

```text
bin\Debug\Snake.exe
bin\Debug\Snake.pdb
bin\Release\Snake.exe
```

Use a separate Web subtree:

```text
bin\Debug\Web\index.html
bin\Debug\Web\smile-runtime.js
bin\Debug\Web\game.js
bin\Debug\Web\smile.css
bin\Debug\Web\Assets\...

bin\Release\Web\...
```

`Release|Web` must be publish-ready and contain no debug server address or machine-local path.

Make Clean target-aware when practical so cleaning Web does not unnecessarily delete the working native executable and cleaning x64 does not unnecessarily delete the Web publish folder. Do not destabilize native Clean merely to perfect this behavior.

### 8.3 Build behavior

For `x64`, preserve the existing command line and behavior.

For `Web`, invoke conceptually:

```text
smilec.exe Program.smile --target web --output-dir <project>\bin\<Configuration>\Web
```

Do not pass native-only `--graphics` or `--vsync` options to the Web target.

After successful Web generation:

- copy every declared `<Asset Include="..." />` tree into the Web output root;
- preserve relative directories;
- print a clear Output-window message such as:

```text
SMILE web publish succeeded: D:\SMILE 2.0\games\Snake\bin\Debug\Web\index.html
```

The existing native success message may remain unchanged.

### 8.4 F5 and Ctrl+F5 for Web

For `x64`:

- preserve the existing `SVsShellDebugger` native launch path;
- preserve `NativeOnly_guid`;
- preserve `.smile` breakpoint behavior.

For `Web`:

1. Build if the Web entry point does not exist or the current build requires regeneration.
2. Start/reuse a tiny in-process static-file server owned by the extension.
3. Bind only to loopback (`127.0.0.1`) on an available port.
4. Serve only files under the selected Web output directory.
5. Reject path traversal.
6. Return reasonable MIME types for HTML, JavaScript, CSS, WAV, MP3, JSON, text, and common image formats.
7. Open the default browser at the local URL.
8. Restart or retarget the server cleanly when the selected project/output folder changes.
9. Do not leave an external server process behind.

A small `TcpListener`-based server is acceptable and avoids new dependencies. Do not add ASP.NET Core/Kestrel packages, Node, Python, IIS Express, or a new host project for this milestone.

Browser `.smile` breakpoints are explicitly out of scope. F5 with Web may launch without a browser debugger. The Windows x64 breakpoint path must continue to work.

### 8.5 Do not disturb editor integration

Target selection must affect only build/output/launch behavior.

Do not alter the working editor content type or file opening design. In particular:

- retain `[FileExtension(".smile")]` and the current SMILE content type;
- retain the MEF completion source;
- retain shared `SmileCompletionService` usage;
- retain `OpenItem`, `ReopenItem`, `IsDocumentInProject`, canonical path handling, and `OpenSpecificEditor` behavior;
- do not create a Web-specific `.smile` editor;
- do not make IntelliSense dependent on the selected target;
- do not add generated Web files to the project hierarchy automatically.

### 8.6 Existing Tools command

`Tools > Build SMILE File` remains a native loose-file build by default. Do not silently make it follow a project Web platform because a loose file may not belong to a project.

A separate loose-file Web command is not required in this milestone.

---

## 9. Snake project requirements

### 9.1 Reuse existing files

Use the current:

```text
games\Snake\Snake.smileproj
games\Snake\Program.smile
games\Snake\Program-NoDemo.smile
games\Snake\Assets\**\*
```

Do not create a web-specific source copy.

The project file may remain unchanged if the project system can infer Web support from `ProjectKind=Game`. If an optional Web property is genuinely necessary, it must have a backward-compatible default and must not be required in all ten game projects.

### 9.2 Compile both teaching variants

The required playable acceptance target is the normal demo-enabled `Program.smile` because it is the project startup file.

Also compile `Program-NoDemo.smile` through the Web backend once as a focused compatibility check. It does not need its own Visual Studio platform/project.

### 9.3 Do not rewrite game rules

- Keep game state, movement, collision, scoring, food spawning, demo AI, high-score behavior, title screen, game-over screen, and rendering in SMILE source.
- Do not port the game by hand into `game.js`.
- Generated JavaScript must come from the generic emitter.
- Avoid editing Snake source. If a tiny source text change is unavoidable, it must remain correct for both x64 and Web and must not introduce target conditionals.

---

## 10. Focused tests to add

Add only proportional automated checks. Avoid a large new framework.

Useful focused checks include:

1. Target parsing is case-insensitive.
2. No `--target` still selects Windows x64.
3. Web output writes the required files.
4. Web emission of a tiny program preserves integer division, Boolean conditions, a routine call, an array access, and a frame yield.
5. The Web target reports a clear diagnostic for one known unsupported valid statement instead of throwing.
6. Snake `Program.smile` Web emission succeeds.
7. Snake `Program-NoDemo.smile` Web emission succeeds.
8. Existing shared completion tests still pass.
9. The VSIX still includes the compiler and project templates.

Do not create brittle tests that compare the entire generated `game.js` character-for-character.

Do not add a browser automation package solely for testing. Use an already-installed Edge/Chrome headless mode, existing browser tooling, or a brief self-performed Visual Studio/browser interaction.

---

## 11. Minimal validation plan

Perform this validation yourself. Do not ask Sin to confirm steps.

### 11.1 Before implementation

- Record the starting commit.
- Record `git status`.
- Confirm the current native Snake project builds before changing code if that can be done quickly.

### 11.2 Focused build/test validation

Use the smallest commands that prove the affected components. Exact commands may be adjusted to the current solution, but a suitable sequence is:

```text
dotnet build src\Smile.Language\Smile.Language.csproj -c Debug
dotnet build src\Smile.Compiler\Smile.Compiler.csproj -c Debug
dotnet run --project src\Smile.Tests\Smile.Tests.csproj -c Debug
cmd /c scripts\build.cmd
```

The repository’s test executable is expected to be fast and is acceptable. Do **not** run `scripts\smoke-test.cmd` by default because it compiles and verifies all ten games. Run the broad smoke suite only if a concrete regression or failure requires it under the repository’s longer-testing exception.

### 11.3 Native compatibility check

Compile the current Snake startup source through the default/native path without `--target`:

```text
artifacts\compiler\smilec.exe games\Snake\Program.smile -o artifacts\validation\Snake-Windows.exe --debug
```

Confirm:

- compilation succeeds;
- `Snake-Windows.exe` exists;
- the corresponding PDB exists;
- the game launches briefly and renders;
- no Web output is required for this command.

Do not perform a long playthrough.

### 11.4 Web compiler check

Compile:

```text
artifacts\compiler\smilec.exe games\Snake\Program.smile --target web --output-dir artifacts\validation\Snake-Web
```

Also compile the no-demo source to a separate temporary folder.

Confirm:

- all expected files exist;
- generated JavaScript parses without syntax errors;
- no absolute repository paths appear in output;
- asset URLs use `/`;
- the generated page can be served over loopback;
- the generic status hook reaches `running`;
- frame count increases;
- no uncaught browser-console error occurs.

### 11.5 Brief browser interaction

Use the available browser yourself and perform one short happy-path interaction:

1. Open the page through HTTP.
2. Confirm the title screen is visible and correctly laid out.
3. Either observe the attract demo begin after its normal short delay or press Enter to start.
4. Press one or two arrow/WASD keys and confirm the snake moves.
5. Confirm the page remains responsive.
6. Confirm at least one WAV request does not crash the game; audio may require the first interaction.
7. Confirm refresh/reopen preserves a stored high score value or at minimum that persistence calls do not error.
8. Stop. Do not run a long demo or exhaustive playthrough.

### 11.6 Visual Studio end-to-end acceptance

Build/install the current VSIX using the repository’s supported process. Then perform these checks yourself in Visual Studio 2026 Enterprise.

#### A. File-open regression

1. Use `File > Open > File` to open a `.smile` file.
2. Confirm it opens in the Visual Studio text editor with SMILE syntax/language behavior.
3. Open `games\Snake\Snake.slnx`.
4. Double-click `Program.smile` in Solution Explorer.
5. Confirm it opens correctly.

#### B. IntelliSense regression

In an editor buffer, invoke completion and confirm at least:

- a known keyword such as `PRINT` appears;
- a built-in such as `RGB` or `GAME_CLOSED` appears;
- a Snake symbol such as `Score`, `SnakeX`, or `MoveSnake` appears in the proper source context.

Undo any temporary typing. Do not leave test edits in game source.

#### C. Windows breakpoint regression

1. Select `Debug|x64`.
2. Set a breakpoint on a guaranteed reachable `.smile` line, such as the top-level `CALL EnterTitle()` immediately after `GAME WINDOW`.
3. Press F5.
4. Confirm the breakpoint binds and is hit in the `.smile` source.
5. Continue briefly and close the game.

This check is mandatory. Do not merely confirm that a PDB file exists.

#### D. Existing Windows build/run

1. Build `Debug|x64`.
2. Confirm the existing native output path remains `bin\Debug\Snake.exe`.
3. Confirm F5 still uses the native debugger.

#### E. Web publish and launch

1. Select `Debug|Web` in the same existing Snake project.
2. Choose Build Solution.
3. Confirm `bin\Debug\Web\index.html` and companion files/assets are produced.
4. Confirm the SMILE Output pane reports Web publish success.
5. Press F5 or Ctrl+F5.
6. Confirm a loopback URL opens in the browser and the playable Snake page runs.
7. Switch back to `x64` and confirm the project remains buildable.

#### F. Release Web output

Build `Release|Web` once and confirm the folder is publish-ready and contains only relative/static dependencies. No long browser test is required for Release.

### 11.7 When broader testing is allowed

Only broaden beyond the checks above if a known problem is discovered, such as:

- breakpoint no longer binds;
- Visual Studio crashes;
- Web server hangs or leaves ports/processes behind;
- browser frame loop freezes;
- intermittent key loss prevents play;
- native output changes unexpectedly;
- IntelliSense stops participating;
- `.smile` files fail to open by one of the required paths.

Record the problem, reason, and stop condition before running longer tests. Stop once the stated defect is understood and fixed.

---

## 12. Documentation updates

Update current SMILE 2.0 documentation proportionally.

At minimum, update `README.md` to explain:

- SMILE 2.0 now has an initial Web target in addition to Windows x64;
- Windows x64 remains the default and complete native target;
- Snake is the first validated Web game;
- how to select `Web` in Visual Studio;
- Web output folder location;
- how F5 launches through a local server;
- the first Web backend uses Canvas 2D;
- browser breakpoints in `.smile` are not yet supported;
- Windows `.smile` breakpoints remain supported;
- initial Web-target limitations, including any safe-integer restriction and unsupported statements;
- actual remote hosting/upload is separate from local Web publication.

Do not rewrite the language specification because this milestone adds no SMILE syntax.

If the VSIX version is incremented, inspect the current version first and increment it by one patch level rather than assuming an old value.

---

## 13. Definition of done

This milestone is done only when all of the following are true:

### Compiler

- [ ] `smilec` accepts `--target web`.
- [ ] No target specified still produces the existing Windows x64 executable.
- [ ] Web output does not require the native runtime library or MASM/linker.
- [ ] The Web backend generically supports every feature used by Snake `Program.smile` and `Program-NoDemo.smile`.
- [ ] Unsupported valid Web features produce clear diagnostics instead of crashes or bad output.

### Visual Studio

- [ ] The existing project exposes `x64` and `Web` platforms.
- [ ] Existing `Debug|x64` and `Release|x64` behavior remains intact.
- [ ] `Debug|Web` and `Release|Web` build from the same `.smileproj`.
- [ ] Web Build publishes a static folder.
- [ ] Web F5/Ctrl+F5 starts a loopback server and opens the browser.
- [ ] No separate Web project is required.

### Snake

- [ ] The existing demo-enabled Snake source runs on a web page.
- [ ] Enter starts the game.
- [ ] WASD/arrows move the snake.
- [ ] Drawing, timing, random food, collisions, scoring, title/demo behavior, and game-over behavior execute from generated code.
- [ ] WAV playback failure or browser autoplay restrictions do not crash the game.
- [ ] High-score persistence uses browser storage.
- [ ] The no-demo source also compiles for Web.

### Regression guardrails

- [ ] Native Snake still builds at its existing `.exe` path.
- [ ] Native F5 still launches with the native debugger.
- [ ] A Windows `.smile` breakpoint binds and is actually hit.
- [ ] IntelliSense still returns shared keywords, built-ins, and source symbols.
- [ ] `File > Open > File` still opens `.smile` correctly.
- [ ] Double-clicking `.smile` in Solution Explorer still opens it correctly.
- [ ] Tools > Build SMILE File remains native and functional.

### Validation discipline

- [ ] Focused tests/builds were run.
- [ ] One brief native launch was performed.
- [ ] One brief browser interaction was performed.
- [ ] One Visual Studio x64 breakpoint check was performed.
- [ ] One Visual Studio Web publish/F5 check was performed.
- [ ] The broad all-ten-game smoke suite was not run unless a recorded known problem justified it.

### Repository

- [ ] Documentation is current.
- [ ] No unrelated user work was discarded.
- [ ] No original SMILE/SMILE 1.0 repository was touched.
- [ ] The milestone is committed and pushed.

---

## 14. Commit and push

Create one coherent validated milestone commit unless a genuine technical reason requires two independently working commits.

The commit subject must begin exactly with:

```text
Sin and Codex:
```

Suggested subject:

```text
Sin and Codex: feat(web): publish Snake from the existing SMILE project
```

Use a detailed body following repository policy:

```text
Summary:
- Added the initial Web target and Visual Studio multi-target publishing.

Changes:
- Added generic JavaScript/Canvas 2D generation and browser runtime.
- Added x64/Web Visual Studio configurations and Web launch hosting.
- Published the existing Snake sources without a duplicate Web project.
- Preserved native debugging, IntelliSense, and .smile file opening.

Validation:
- List exact focused builds and tests.
- List native Snake compile/launch.
- List browser Snake check.
- List Visual Studio x64 breakpoint and Web publish/F5 checks.

Known limitations:
- Browser .smile breakpoints are not yet supported.
- List any other honest initial Web-target limitations.
```

Push the validated commit. Do not amend, rebase, force-push, or rewrite previously pushed history.

---

## 15. Final Codex report to Sin

Do not ask Sin to verify the implementation before you finish. Complete the work and then report:

1. starting and final commit hashes;
2. pushed branch;
3. files added/changed;
4. exact compiler target syntax;
5. Visual Studio target-selection workflow;
6. native output path;
7. Web publish output path;
8. local browser URL behavior;
9. generated Web file list;
10. focused automated validation results;
11. native Snake launch result;
12. browser Snake interaction result;
13. Windows `.smile` breakpoint result, including the line used and confirmation that it was hit;
14. IntelliSense result;
15. File > Open result;
16. Solution Explorer double-click result;
17. VSIX path and installed version;
18. any honest remaining limitations.

Do not report a check as passed unless you actually performed it.

---

# Final directive

Implement this end-to-end now in **SMILE 2.0**. Use the existing Snake project and source for both `x64` and `Web`. Make the Web build publish and run from Visual Studio. Preserve the existing native `.exe` generation, Windows breakpoints, IntelliSense, and both `.smile` file-opening paths. Test the completed happy path yourself with light, focused validation, expanding only when a concrete failure warrants it. Commit and push the validated milestone without waiting for Sin to confirm intermediate steps.
