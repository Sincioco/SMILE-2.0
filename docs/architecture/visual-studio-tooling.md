# Visual Studio tooling and debugging

[Architecture index](README.md) · [Compiler/runtime contracts](compiler-and-runtime.md)

The shared language/project model owns semantics. The Visual Studio layer adapts that model to
editor buffers, project commands, builds and debugging; it must not introduce an independent
parser or project model.

## Projects, references and commands

The Visual Studio extension embeds the same compiler/runtime payload and registers one factory
for `.smileproj` and `.smilelibproj`. The shared project model owns startup/support sources,
library identity, and project/package references. Project builds pass `--project` to the
compiler so editor diagnostics, build diagnostics, native/Web emission, dependency order, and
source debugging use the same files and bound model. The project system's existing Stop callback
now cancels the active compiler process and its child tree instead of merely changing UI state.
Solution Explorer projects References as a live node; add/remove reference commands update XML,
hierarchy, and editor analysis immediately.

Source nodes use Visual Studio's standard item menu and icons. The shell/Git provider owns
Compare With, history, unmodified comparisons and ignore/track actions.
`SmileProjectSourceControl.cs` supplies real hierarchy paths and provider glyphs through
`IVsSccProject2`. `SmileProjectFileCommands.cs` is the focused project-hierarchy adapter for
clipboard operations, inline rename and deletion; it reuses the shared project-file editor and
existing refresh/document tracking. The partial declarations preserve one COM hierarchy
identity, not a second project model. `SmileProjectEditors.cs` owns normal and specific-editor
opening through `IVsProject3`, preserving the editor factory, physical/logical view and existing
document data supplied by Visual Studio.

Copy Full Path works for file nodes. Copy/Paste handles one `.smile` at a time; paste into a
project/folder creates a unique name on collision. Cut/Paste moves between SMILE project/folder
nodes. Cut does not promise Explorer move semantics. Physical rename/delete/cut are limited to
source files inside the project root; the startup source can be renamed but cannot be
cut/deleted. Rename preserves explicit or implicit startup identity. Delete sends the file to
the Recycle Bin; Remove from Project preserves the file. Open documents receive save prompts.

The focused adapters live in [`src/Smile.VisualStudio`](../../src/Smile.VisualStudio). The old
file-size measurements are retained only in the [historical
entry](archive/compiler-tooling-before-2026-10-05.md).

## Workspace analysis

The editor workspace retains current text snapshots for every open project buffer. A buffer
change invalidates analysis caches for the other participating files after the normal debounce.
The language analysis carries one direct-provider access context used by project/package
validation, editor completion, the compiler, and native/Web emitters; module presence alone
never grants import access. Focused per-directory watchers use tolerant participation discovery,
preserve last-known reachable paths through partial graph failures, and refresh only the owning
project when direct or transitive dependencies change or reappear. Expected graph or package
failures become shared `SML32xx` diagnostics while local analysis remains available; unexpected
failures are logged and enter a safe diagnostic state instead of faulting the cache. The
selected startup uses ordinary supports; an unselected `StartupOnly` file is instead analyzed as
a hypothetical startup with those same supports and without the selected complete program.
Missing project sources produce a physical-file `SML0001` diagnostic rather than falling back to
unrelated single-file semantics. Loose source builds retain their ordinary program behavior
while any supplied packages use the shared exact-provider resolver.

## Source debugging

Debug builds emit unique source-aware C helpers with physical multi-file `#line` mappings and
compile those helpers as explicit UTF-8 with native Just My Code metadata. Helper parameters use
deterministic ordinal ASCII names (`smile_debug_v0`, `smile_debug_v1`, and so on), so C
keywords, Unicode SMILE identifiers, long names, and sanitization collisions cannot break MSVC
compilation. C-safe original names remain local aliases for the established debugger display;
identifiers that are not valid safe C aliases use their ordinal helper name. Numbers and
Booleans retain readable scalar values, Text values are exposed as read-only strings, and
arrays, images, and records retain inspectable native addresses. The native MASM implementation
remains below that source surface. Consequently Windows breakpoints bind in startup and support
files, hovering an in-scope variable can display its live value, and F10 advances among mapped
SMILE statement helpers, including routine returns, instead of stopping in generated
implementation ranges. Console and game templates build to `bin\Debug` or `bin\Release`, copy
declared assets, populate the SMILE Output pane and Error List, and launch a freshly built
executable for F5 or Ctrl+F5.

## Required UI regression for command changes

For a VSIX regression check, use an ignored disposable project: F2-rename a support source,
rename an implicit `Program.smile`, verify project membership and startup identity, copy/paste a
source, and exercise Delete and Remove separately. Check a tracked modified source's Git
comparison and history in the real solution. For the comparison regression, save an exact copy
of a tracked `.smile` file, append one temporary comment, then invoke Git > Compare with
Unmodified and Compare With against the copy. Both must show an actual difference view
containing that comment, including when the ordinary text editor is already open. Merely
activating the text tab is a failure. Restore the exact original bytes afterward. Build/install
the VSIX before this UI check; compiler tests alone do not prove Visual Studio command routing.
No standalone architecture checker exists for these adapters; ownership and changed-file growth
are reviewed with the diff.

Compiler-only checks cannot prove shell COM editor selection. Changes to the VSIX or its bundled
payload must be rebuilt and installed through the repository scripts before acceptance. The
[September 20 result](archive/compiler-tooling-before-2026-10-05.md#shared-audio-focus-contract)
records the earlier 35-payload validation and exact temporary-comment comparison; it is not a
validation result for today's changes.
