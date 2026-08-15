# CODEX Execution Instructions — SMILE 2.0 VSIX Quick Info, F12, and Startup Project

## 1. Operating mode

Work autonomously from the current repository state through completion.

Do not stop after:

- analysis;
- a plan;
- interface stubs;
- a partial Quick Info prototype;
- a command that appears but does not change F5 startup behavior;
- tests without a built VSIX.

Continue through implementation, debugging, focused validation, package generation, commit, and push.

Ask the user only when an external dependency or decision genuinely prevents further progress.

## 2. First actions

1. Change to:

   ```text
   D:\SMILE 2.0
   ```

2. Read `AGENTS.md`.
3. Read all six documents in this requirement folder in numeric order.
4. Run `git status`.
5. Record current branch and HEAD.
6. Inspect all uncommitted files before editing.
7. Preserve unrelated user work.
8. Inspect the current versions of:
   - `Smile.VisualStudio.csproj`;
   - `source.extension.vsixmanifest`;
   - Microsoft Visual Studio SDK package references.
9. Reconfirm current implementations of:
   - `SmileCompletionSource`;
   - `SmileAnalysisCache`;
   - `SmileProjectWorkspace`;
   - `SmileProjectSystem`;
   - `SmileProjectCommands`;
   - `Commands.vsct`;
   - semantic symbols/modules;
   - `libraries\Smile.UI\Menu.smile`.

The repository may have advanced since this handoff was prepared. Adapt to current HEAD while preserving the required behavior.

## 3. Implementation sequence

### Phase A — Shared documentation and symbol resolution

Implement in `src\Smile.Language`:

- documentation comment extraction;
- shared symbol-at-position resolution;
- declaration locations for imported modules and qualified members;
- shared signature/presentation data;
- focused tests.

Add `'''` documentation to all public routines in `libraries\Smile.UI\Menu.smile`.

Build and run focused shared-language tests before continuing.

### Phase B — Visual Studio Quick Info

Implement:

- async Quick Info provider/source;
- per-buffer cache reuse;
- student-friendly presentation;
- safe cancellation/failure behavior.

Build the VSIX project.

### Phase C — F12 navigation

Implement:

- typed `GoToDefinitionCommandArgs` handler for SMILE;
- shared resolver consumption;
- UI-thread document navigation;
- safe fallback/status handling.

Build again and resolve all SDK/API mismatches rather than leaving pseudocode.

### Phase D — Startup project command

Implement:

- command constant;
- VSCT button;
- command visibility/status;
- actual solution startup selection;
- source command relabeling to **Set as Startup Source**;
- no project XML changes.

Build again.

### Phase E — Version, package, and verification

- Increment current VSIX patch version once.
- Build a fresh VSIX.
- Run focused automated tests.
- Use the experimental instance for concise manual checks.
- Verify protected regressions.
- Commit and push.

## 4. Engineering rules

- `src\Smile.Language` remains authoritative.
- Do not parse qualified names with a VSIX-only regex.
- Do not duplicate module/import visibility rules.
- Do not add LSP or Roslyn.
- Do not replace the current project system.
- Do not add a new `Main`.
- Keep net472 compatibility.
- Use existing repository coding style and nullable conventions.
- Keep public API additions small.
- Avoid unnecessary abstractions and dependencies.
- Do not introduce a documentation warning/error system in this milestone.
- Do not make documentation comments affect generated code.
- Do not reformat entire large files, especially `Semantics.cs`, `Modules.cs`, `SmileProjectSystem.cs`, or `Menu.smile`.
- Do not change unrelated language syntax.
- Do not update library/package versions solely because comments were added unless current repository policy requires it.
- Do not remove existing completion descriptions.
- Do not save/build on hover or F12.
- Do not use modal dialogs for normal unresolved symbols.

## 5. Visual Studio API guidance

Use current supported SDK APIs that build against the repository.

Expected direction:

- Quick Info:
  - `IAsyncQuickInfoSourceProvider`;
  - `IAsyncQuickInfoSource`;
  - `QuickInfoItem`.
- Go To Definition:
  - MEF `ICommandHandler<GoToDefinitionCommandArgs>` or chained equivalent;
  - export for `SmileContentType.Name`;
  - return unhandled when no SMILE destination exists.
- Startup project:
  - `SVsSolutionBuildManager`;
  - `IVsSolutionBuildManager2.get_StartupProject`;
  - `IVsSolutionBuildManager2.set_StartupProject`.

If an exact signature differs in the current SDK, inspect the referenced assemblies/docs and use the available equivalent. Do not fall back to a global legacy editor command filter merely because it is familiar.

## 6. Protected behaviors

Treat these as release blockers:

- IntelliSense;
- `.smile` syntax/diagnostics;
- File > Open;
- Solution Explorer double-click;
- project refresh;
- native build;
- Web build;
- native breakpoint binding/hit;
- source-level startup-file command;
- existing project menus.

If one breaks, fix it before commit.

## 7. Test discipline

Use focused tests first.

Do not run:

- long game soaks;
- exhaustive library matrices;
- broad seed sweeps;
- repeated full VS installations;
- unrelated performance benchmarks.

Expand only when a known failure needs it, and document why.

## 8. Git discipline

- Do not reset or clean the working tree.
- Do not discard user changes.
- Do not commit broken intermediate milestones.
- Use one coherent commit unless a repository constraint makes two commits clearly safer.
- Commit subject must begin exactly `Sin and Codex:`.
- Push after validation.
- Do not amend, rebase, force-push, or rewrite pushed history.

## 9. Completion definition

The task is complete only when:

- hover gives educational `Menu.Create` information;
- F12 on `Menu` reaches the module;
- F12 on `Create` reaches the function;
- **Set as Startup Project** changes the actual F5 project;
- a new VSIX is produced;
- required regressions have been checked;
- changes are committed and pushed;
- the final report contains concrete evidence.

## 10. Required final response

Use a structured final report with these headings:

```text
Result
Commit and Push
Files Changed
Shared Language Services
Quick Info
F12 Navigation
Startup Project
Documentation Added
Validation
VSIX Artifact
Regressions
Known Limitations
Manual Check
```

Bold any manual action still required from Sin.
