# Autonomous Single-Agent Codex Implementation Instructions

## Mission

Implement the complete Phase 1 formal specification in the current SMILE 2.0 repository, validate it on Windows and Web, update affected legacy project metadata/templates/documentation, commit all repository changes, and push them.

Work independently. Do not stop after analysis, planning, partial scaffolding, or a compiler-only implementation.

---

# 1. Single-agent rule

Use exactly one Codex agent for this phase.

Do not:

- spawn subagents;
- delegate code review to another agent;
- launch parallel implementation agents;
- split native and Web work across agents;
- ask Sin to coordinate agents.

You may use ordinary tools, shell commands, builds, tests, Visual Studio automation, browsers, and repository inspection within the one-agent session.

---

# 2. Preflight

Before editing:

1. Open `D:\SMILE 2.0`.
2. Read repository-root `AGENTS.md` completely.
3. Read this package in numeric order.
4. Inspect the latest commit and all commits after the package’s recorded baseline.
5. Run `git status --short`.
6. Preserve all existing user work and requirement files.
7. Confirm the current branch and upstream.
8. Inspect the actual current implementations of at least:

```text
src\Smile.Language\SmileLanguage.cs
src\Smile.Language\Text.cs
src\Smile.Language\Diagnostics.cs
src\Smile.Language\Semantics.cs
src\Smile.Language\Completion.cs
src\Smile.Compiler\CompilerDriver.cs
src\Smile.Compiler\SmileCompilationTarget.cs
src\Smile.Compiler\MasmEmitter.cs
src\Smile.Compiler\WebEmitter.cs
src\Smile.Compiler\WebOutputWriter.cs
src\Smile.VisualStudio\SmileAnalysisCache.cs
src\Smile.VisualStudio\SmileDiagnosticTagger.cs
src\Smile.VisualStudio\SmileCompletionSource.cs
src\Smile.VisualStudio\SmileProjectSystem.cs
src\Smile.VisualStudio\SmileBuildService.cs
src\Smile.Tests\Program.cs
scripts\build.cmd
scripts\smoke-test.cmd
src\Smile.VisualStudio\Templates\...
games\*\*.smileproj
```

The repository may have advanced. Adapt the implementation to the latest architecture while preserving the formal behavior in this package.

Do not reset the repository to the package baseline.

---

# 3. Preserve the package

If the package is outside the repository, copy it into the approved `requirements\Sin\...` folder and commit it with the implementation.

Verify `MANIFEST.json` before modifying any copied companion sample.

Do not edit the preserved numbered requirement documents to make implementation easier. If a genuine contradiction with newer approved repository governance exists, document the resolution in the final report and choose the newer explicit user decision.

---

# 4. Implement the shared source/compilation model first

Create the smallest clear source-document and compilation-analysis model in `Smile.Language`.

Required outcome:

- single-source `Analyze` remains working;
- multi-source analysis accepts one startup plus support documents;
- each file is parsed separately;
- analysis exposes all trees and the startup tree;
- every source-aware operation can identify the owning file;
- semantic symbols/routines carry declaration-source identity;
- diagnostics retain correct physical file paths and spans.

Prefer a few focused types over a large workspace/compiler framework.

Do not add a general package manager, incremental compiler, Roslyn clone, or speculative abstraction hierarchy.

---

# 5. Refactor semantic analysis into compilation-wide passes

Modify the existing semantic analyzer rather than creating a second one.

Implement the minimum passes necessary to:

- register declarations/routines across files;
- collect existing implicit startup globals consistently;
- allow cross-file forward references;
- bind routine bodies against one compilation-wide symbol model;
- keep routine locals scoped correctly;
- enforce support-file top-level restrictions;
- diagnose cross-file duplicates.

Keep current single-file behavior and diagnostics green unless the new source-aware design requires an intentional repository-wide update.

When changing a shared public API, update all repository consumers in the same commit.

---

# 6. Extend compiler options and source loading

Implement repeatable:

```text
--source <path.smile>
```

Update:

- usage text;
- option model;
- validation;
- source loading;
- error reporting;
- Windows dispatch;
- Web dispatch.

Read each file independently and create the multi-source analysis once. Do not analyze separately per target.

Keep the existing first positional source as the startup file.

Do not add `.smileproj` parsing to the command-line compiler in this phase.

---

# 7. Update native emission and debugging

Update `MasmEmitter` with minimal disruption.

It must:

- collect across all trees;
- assign labels across the compilation;
- emit one global storage section;
- emit every routine once;
- emit startup top-level execution only;
- detect runtime feature use across all routine/source bodies;
- preserve current native ABI and runtime calls.

Replace line-only debug tracking with source-aware unique debug sites.

Update debug C generation so real source paths/lines from several files enter the PDB without symbol-name collisions.

Do not weaken or remove existing startup-file breakpoint behavior to make support-file breakpoints work.

---

# 8. Update Web emission

Update the existing Web emitter—not a duplicate emitter—to consume the multi-source analysis.

It must:

- name symbols/routines deterministically across files;
- emit support routines and globals;
- execute only startup top-level statements;
- preserve the current browser runtime contract;
- map Web target errors to the correct file;
- keep static output and Canvas 2D publication working.

Do not introduce a second hand-written JavaScript implementation of the sample.

---

# 9. Update the project system

Implement `StartupOnly="true"` source metadata and the source-selection rules from the formal specification.

Update project loading/hierarchy data structures so each source retains:

- relative include path;
- full path;
- startup-only flag;
- selected-startup status;
- support-source status.

Update build invocation for Windows and Web to pass all selected support sources.

Save every open participating source before build. Preserve unsaved unrelated/nonparticipating files.

Keep:

- existing configuration/platform names;
- native output paths;
- Web output directories;
- asset copying;
- native debugger launch;
- Web browser launch;
- File > Open behavior;
- Solution Explorer double-click behavior;
- Tools > Build SMILE File.

Do not broadly replace the project system.

---

# 10. Make editor analysis project-aware

Extend the existing analysis-cache/completion/tagger path with the smallest safe project-aware mechanism.

Required result:

- current-file tokens still drive classification;
- current-file diagnostics use only that file’s spans;
- cross-file routines/globals appear in completion;
- routine locals/parameters remain scoped to the active routine/file;
- loose files remain single-file;
- errors in support files do not create bogus squiggles at the same numeric span in another file.

Do not duplicate project parsing/selection rules inconsistently. Reuse a small shared project-source model where practical.

Avoid a broad workspace framework. Phase 2 can evolve the model after this implementation is inspected.

---

# 11. Migrate repository project metadata

Update all ten game `.smileproj` files and relevant templates so complete normal/no-demo programs are startup-only candidates rather than simultaneous support sources.

Expected pattern:

```xml
<SmileSource Include="Program.smile" StartupOnly="true" />
<SmileSource Include="Program-NoDemo.smile" StartupOnly="true" />
```

Preserve any assets/maps and existing startup selection.

Update documentation that shows project-file examples or explains switching to `Program-NoDemo.smile`.

Do not rewrite the ten game source files unless a real compiler/language correction requires it. If source changes are required, update both normal/no-demo variants and all affected tests/docs.

---

# 12. Add the MultiFileBasics example

Copy/adapt the supplied companion project into:

```text
examples\MultiFileBasics
```

Include it in focused validation and appropriate public documentation.

The sample must remain simple enough for a child/student to understand:

```text
Program.smile       owns the window and loop
GameState.smile     owns shared data and update routines
Drawing.smile       owns drawing routines
```

Do not turn it into a framework or add assets.

---

# 13. Focused tests

Add proportional tests to the existing test harness. Test behavior, not private implementation details.

At minimum cover:

- single-source API compatibility;
- multi-source cross-file routine call;
- support-to-support call;
- support declaration visibility;
- startup global visibility from support routine;
- source-aware duplicate diagnostics;
- support top-level rejection;
- correct Web error file;
- compiler-option repeated `--source` parsing;
- `StartupOnly` project parsing;
- source-set selection;
- debug-site uniqueness for identical line numbers in different files;
- cross-file completion results.

Do not build a large new testing framework.

---

# 14. Build and validation order

Use the lightest focused sequence that proves the milestone:

1. Build the changed C# projects.
2. Run the focused/shared test executable.
3. Compile and briefly run `MultiFileBasics` as native Windows x64.
4. Publish `MultiFileBasics` for Web and perform JavaScript syntax/browser-console checks.
5. Run the normal repository build.
6. Run the normal smoke suite once after focused checks are green.
7. Perform one brief Visual Studio validation when safely possible:
   - cross-file IntelliSense;
   - support-file diagnostic;
   - native breakpoint in `Program.smile`;
   - native breakpoint in a support routine;
   - Windows F5;
   - Web F5/Ctrl+F5 or equivalent launch.
8. Run `git diff --check`.

Do not run long playthroughs, stress loops, broad browser matrices, or repeated full smoke suites without a known problem.

When broader testing is truly required, first record:

```text
Known problem being investigated:
Why the longer test is necessary:
Stop condition:
```

---

# 15. Visual Studio safety

Do not force-close a Visual Studio instance containing unrelated unsaved user work.

Prefer:

- an isolated/experimental instance;
- a safely installable refreshed VSIX;
- automated project-system tests;
- a normal instance only when it can be used without discarding user state.

If a live breakpoint/IntelliSense check cannot be performed safely because of an active unrelated IDE session, finish all other validation, commit/push when green, and place that exact check in the bold manual-testing section. Do not ask Sin to return before continuing.

Bump the VSIX patch version because this phase changes project and language-service behavior.

---

# 16. Failure handling

For ordinary failures:

- inspect the error;
- fix the implementation;
- rerun the narrow failing check;
- continue.

A genuine blocker is limited to something like:

- inaccessible required repository/credentials after retries;
- corrupted external tool installation that cannot be repaired safely;
- unavailable signing/hosting secret required by the user-visible result;
- contradictory explicit user requirements with no safe interpretation.

If truly blocked, leave the repository intact and report exact commands/errors. Do not manufacture success.

---

# 17. Commit and push

After validation is green:

1. Review all changes.
2. Ensure no secrets, temporary browser profiles, `bin`, `obj`, generated publish folders, PDBs, executables, or machine-local files are unintentionally tracked.
3. Run `git add -A` so all intended unstaged repository work is included.
4. Commit one coherent milestone with a detailed body.
5. Push to the current upstream branch.
6. Confirm the pushed commit hash.
7. Confirm `git status --short` is clean.

Recommended commit:

```text
Sin and Codex: feat(language): add true multi-file SMILE compilation

Summary:
- Compile one SMILE program from a selected startup source and reusable support source files on Windows and Web.

Changes:
- Add source-aware multi-file analysis, semantic binding, emission, debugging, project metadata, project-wide tooling, and the MultiFileBasics example.
- Preserve alternate normal/no-demo startup sources through StartupOnly project metadata.
- Update all affected legacy projects, templates, tests, and documentation.

Validation:
- List exact focused tests, build/smoke results, native/Web artifacts, browser checks, and Visual Studio checks.

Known limitations:
- Phase 2 modules/imports/libraries remain intentionally deferred.
- List any real remaining limitation or "None identified."
```

Do not split requirement preservation and implementation into multiple half-finished commits unless the current repository state makes a separate already-existing requirements commit unavoidable. The final implementation commit must be coherent and green.
