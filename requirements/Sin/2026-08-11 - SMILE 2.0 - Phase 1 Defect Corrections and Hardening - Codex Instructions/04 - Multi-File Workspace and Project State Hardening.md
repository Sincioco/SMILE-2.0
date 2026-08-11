# Multi-File Workspace and Project State Hardening

## Mission

Make the Phase 1 multi-file editor and project state reliable during normal interactive development.

## 1. Use all open buffer snapshots

Project-wide analysis must not read stale disk text for another source that is currently open and modified.

Required scenario:

1. Open `GameState.smile` and `Program.smile`.
2. Rename a routine in `GameState.smile` without saving.
3. Switch to `Program.smile`.

Expected:

- IntelliSense and diagnostics in `Program.smile` see the renamed routine from the unsaved `GameState.smile` buffer;
- the old name is no longer treated as current;
- saving is not required merely to synchronize editor analysis.

Maintain a project-level collection of current open-buffer snapshots, or an equivalent small design.

When any participating buffer changes, invalidate/reanalyze other open buffers in that project after the normal debounce.

Avoid analysis loops and UI-thread blocking.

## 2. Analyze alternate startup candidates correctly

For a non-selected source marked:

```xml
StartupOnly="true"
```

such as `Program-NoDemo.smile`, editor analysis should use:

```text
that source as the hypothetical startup
+ all ordinary support sources
```

Do not include the currently selected complete startup at the same time.

This permits correct IntelliSense and diagnostics before the user chooses Set as Startup File.

The active build still uses only the selected `<StartupFile>`.

## 3. Refresh after project mutation

After:

- Add New Source;
- Add Existing Source;
- Set as Startup;
- Include as Support;
- Remove from Project;

perform all necessary updates:

```text
write project XML
-> rebuild source-set model
-> register workspace mapping
-> refresh hierarchy
-> invalidate open-buffer analyses
-> update startup visual state
-> invalidate stale build output/up-to-date state
```

No solution reload or Visual Studio restart is allowed.

## 4. Prevent stale native launch

Changing startup/source membership changes the program even when the previous `.exe` already exists.

The next F5 must build the changed source set before launch.

Use the current Visual Studio build lifecycle correctly. Additionally invalidate/delete stale output or mark the project dirty when necessary.

Do not let:

```text
Program.smile built
-> Set Program-NoDemo.smile as startup
-> F5
```

launch the previously built `Program.smile` executable.

The same requirement applies after adding or removing a support source.

## 5. Source-entry validation

Harden project source parsing and mutation:

- require `.smile` extension, case-insensitively;
- reject empty includes;
- reject normalized duplicates;
- preserve real physical paths;
- use project-relative paths in XML;
- avoid absolute machine-specific references;
- produce clear errors for missing files;
- preserve one selected startup;
- preserve `StartupOnly` semantics;
- do not let a source be simultaneously treated as an alternate complete program and ordinary support source.

Do not introduce breaking restrictions unrelated to real SMILE source safety.

## 6. Clear project-aware editor diagnostics

When project analysis cannot read a required source:

- do not silently downgrade the entire file to unrelated single-file semantics and leave confusing cross-file errors;
- retain the latest known open-buffer/disk snapshot when safe;
- otherwise surface a clear project/source diagnostic;
- recover automatically once the file becomes available.

Keep diagnostics attached to their owning physical source file.

## 7. Cross-file invalidation tests

Add focused coverage for:

- an unsaved declaration change in one file affecting another open file;
- an unsaved support-file syntax error appearing in project-aware analysis;
- an alternate `StartupOnly` file seeing ordinary support symbols;
- the active startup not being included when analyzing another startup candidate;
- source-set refresh after startup switch;
- source-set refresh after add/remove;
- stale native output invalidation;
- Windows/Web compiler arguments after mutation.

## 8. Preserve editor features

Do not regress:

- syntax highlighting;
- IntelliSense;
- squiggles;
- Error List;
- File > Open;
- Solution Explorer double-click;
- real filename tabs;
- project-aware source paths;
- loose-file analysis outside a project.

## Workspace definition of done

All must be true:

- open unsaved buffers are coherent across files;
- alternate startup candidates receive support-file context;
- UI mutations immediately affect analysis/build;
- F5 cannot launch stale source-set output;
- diagnostics remain source-aware;
- all existing projects still load and build.
