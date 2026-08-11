# Phase 1 Report, Findings, and Corrective Scope

## Overall assessment

The Phase 1 commit delivered the central compiler feature:

- one selected startup source and several support sources form one SMILE compilation;
- physical files retain separate syntax trees and diagnostics;
- Windows and Web consume the same source set;
- cross-file routines, constants, arrays, and globals bind;
- alternate full programs are excluded through `StartupOnly="true"`;
- `MultiFileBasics` proves the basic model;
- the ten legacy game projects were migrated.

That work is valuable and should be preserved.

Phase 1 is not yet ready to serve as the foundation for modules and libraries because the user found two important Visual Studio defects and the review identified several project-workspace hardening needs.

## Defect A: source stepping is incomplete

### Reproduction

1. Open `examples\MultiFileBasics\MultiFileBasics.slnx`.
2. Select `Debug | Windows 64-bit .exe`.
3. Put a breakpoint on an executable line in `GameState.smile`.
4. Press `F5`.
5. Let Visual Studio stop at the breakpoint.
6. Press `F10`.

### Actual behavior

Visual Studio opens the **Source Not Available** page.

### Why this blocks acceptance

The Phase 1 acceptance objective was source-level debugging in support files. A bindable helper location is only the first half of that requirement.

A usable debugger must support:

```text
break
-> inspect
-> step over
-> remain in the correct .smile source
-> continue through calls and returns
```

The fix must map actual executing native ranges, or an equivalent stepping path, to physical `.smile` source locations.

## Defect B: no project/item/folder context menus

### Reproduction

Open `games\Snake\Snake.slnx`.

Right-click:

- the `Snake` project;
- `Program.smile`;
- `Program-NoDemo.smile`;
- a project-owned folder.

### Actual behavior

No context menu appears. Only the Solution node behaves normally.

### Why this blocks acceptance

Multi-file compilation is not practical in Visual Studio without a source-management UI.

The user should not have to hand-edit `.smileproj` XML merely to:

- add a new support source;
- add an existing source;
- choose the no-demo program;
- understand which source is the startup file.

## Hardening finding C: unsaved cross-file editor state can be stale

The project-aware workspace currently analyzes the current buffer text but reads the other project sources from disk.

That means this workflow can become inconsistent:

```text
edit GameState.smile without saving
-> switch to Program.smile
-> IntelliSense/diagnostics in Program.smile still see the old GameState.smile on disk
```

The multi-file editor must analyze the latest open-buffer snapshots for all open participating files, not only the active file.

Changes in one open file should invalidate or refresh the other open files in that project.

## Hardening finding D: alternate startup candidates need project context

A non-selected source marked `StartupOnly="true"` is excluded from the active compilation, which is correct for builds.

However, while editing `Program-NoDemo.smile`, the editor should be able to analyze this hypothetical source set:

```text
Program-NoDemo.smile as startup
+ all ordinary support sources
```

This becomes essential as normal and no-demo programs begin sharing support files.

Selecting the file as startup should not be required merely to receive correct IntelliSense and diagnostics while editing it.

## Hardening finding E: UI mutations must not leave stale state

After Add Source, Set Startup, Include as Support, or Remove from Project:

- `.smileproj` must be updated on disk;
- the in-memory source set must be rebuilt;
- Solution Explorer must refresh;
- all relevant open buffers must reanalyze;
- build inputs must refresh;
- the next native F5 must not run the previously built startup program;
- Web publication must use the new source set;
- no Visual Studio restart or solution reload may be required.

## Hardening finding F: source metadata needs validation

Project mutation should ensure:

- source entries end in `.smile`, case-insensitively;
- paths stored in `.smileproj` are project-relative and portable;
- normalized duplicate paths are rejected;
- files outside the project are copied into the project rather than creating a machine-specific absolute reference;
- the current startup cannot be removed or converted to support without selecting another startup;
- XML updates preserve unrelated properties and assets.

## Hardening finding G: live debugger behavior needs an explicit acceptance test

Automated debug-site tests can prove that source paths and lines were emitted, but they cannot prove Visual Studio's `F10` behavior.

The final milestone must explicitly report the live test:

```text
breakpoint hit in GameState.smile
F10 repeated through at least three SMILE statements
routine return reached in SMILE source
Source Not Available did not appear
```

## Scope

### In scope

- native Windows source stepping;
- debug mapping architecture;
- project/item/folder context menus;
- Add New SMILE Source;
- Add Existing SMILE Source;
- Set as Startup File;
- Include as Support File;
- Remove from Project without deleting the physical file;
- visual startup indication;
- project XML mutation;
- workspace/open-buffer coherence;
- alternate-startup editing context;
- stale-output prevention;
- focused tests, documentation, VSIX version bump, commit, and push.

### Out of scope

- `MODULE`, `IMPORT`, `PUBLIC`, or `PRIVATE`;
- `.smilelib` and `.smilelibproj`;
- reusable UI/RPG components;
- `TYPE`, `BYREF`, `BYVAL`, or general mutable `TEXT`;
- image/sprite support;
- multiple sound channels;
- browser `.smile` breakpoints;
- a browser debugger;
- unrelated project-system replacement.

## Compatibility policy

SMILE 2.0 is still under development. It is acceptable to improve project metadata and debugging internals even when existing projects need migration.

When affected, update all ten legacy games and their teaching editions so they continue to prove SMILE's established capabilities.
