# Acceptance, Regression, Commit, Push, and Final Report

## Definition of done

This corrective milestone is accepted only when every mandatory item passes.

# A. Native source debugging

- [ ] A breakpoint in `Program.smile` binds and hits.
- [ ] A breakpoint in `GameState.smile` binds and hits.
- [ ] A breakpoint in `Drawing.smile` binds and hits.
- [ ] `F10` advances through at least three SMILE statements.
- [ ] `F10` returns from a support routine to the caller's SMILE source.
- [ ] Source Not Available does not appear.
- [ ] Generated C/C++, MASM, runtime source, or disassembly does not replace the SMILE source view.
- [ ] Same line numbers in different files remain distinct.
- [ ] Repeated F5 remains reliable.
- [ ] Release and Web output do not regress.

# B. Solution Explorer context menus

- [ ] Right-clicking the project node shows a useful menu.
- [ ] Right-clicking a `.smile` source shows a useful menu.
- [ ] Right-clicking a relevant folder shows a useful menu.
- [ ] Build works from the project context menu.
- [ ] Rebuild works.
- [ ] Clean works.
- [ ] Add New SMILE Source works.
- [ ] Add Existing SMILE Source works.
- [ ] Set as Startup File works.
- [ ] Include as Support File works.
- [ ] Remove from Project works without deleting the file.
- [ ] Startup source is visibly identifiable.
- [ ] Double-click opening remains functional.

# C. Program-NoDemo UI workflow

- [ ] Open Snake.
- [ ] Set `Program-NoDemo.smile` as startup through the UI.
- [ ] The project file updates.
- [ ] The new startup is visibly indicated.
- [ ] Native F5 builds/runs the no-demo program, not stale normal output.
- [ ] Web F5 publishes/runs the no-demo program.
- [ ] Set `Program.smile` back through the UI.
- [ ] Native/Web return to normal behavior.

# D. Source-management workflow

- [ ] Add a new support `.smile` file through the UI.
- [ ] It appears immediately.
- [ ] It opens in the normal SMILE editor.
- [ ] IntelliSense sees its declarations.
- [ ] Windows includes it.
- [ ] Web includes it.
- [ ] Remove from Project updates the project immediately.
- [ ] Temporary test residue is cleaned.

# E. Workspace hardening

- [ ] Unsaved edits in one open source affect other open sources.
- [ ] Alternate startup candidates see ordinary support sources.
- [ ] The currently selected complete startup is excluded from alternate-startup analysis.
- [ ] Add/remove/startup changes invalidate open analyses.
- [ ] No restart/reload is required.
- [ ] No stale native executable is launched.

# F. Existing Visual Studio regressions

- [ ] IntelliSense still works.
- [ ] Error squiggles still work.
- [ ] Error List uses correct file, line, and column.
- [ ] File > Open opens `.smile` correctly.
- [ ] Solution Explorer double-click opens `.smile` correctly.
- [ ] Tools > Build SMILE File still works and remains native by default.
- [ ] Windows Debug breakpoints still work in legacy single-file games.
- [ ] Web F5 and Ctrl+F5 still work.

# G. Legacy games

For all ten games:

- [ ] normal Windows source compiles;
- [ ] no-demo Windows source compiles where applicable;
- [ ] normal Web source publishes;
- [ ] no-demo Web source publishes where applicable;
- [ ] JavaScript syntax checks remain green;
- [ ] assets/maps copy correctly;
- [ ] native outputs remain x64 GUI executables with no CLR header.

# H. Automated validation

Required:

```bat
cmd /c scripts\build.cmd
cmd /c scripts\smoke-test.cmd
cmd /c git diff --check
cmd /c git status --short
```

Record exact pass counts.

# I. Documentation

Update:

- root README;
- architecture documentation;
- language/project documentation where relevant;
- Visual Studio usage instructions;
- no-demo startup instructions so they prefer the UI;
- test documentation;
- VSIX version.

Do not claim browser source breakpoints were added.

# J. Commit and push

When green:

- [ ] review all changes;
- [ ] stage all reviewed milestone files;
- [ ] create one coherent `Sin and Codex:` commit;
- [ ] push normally;
- [ ] report the commit and remote result.

# Required final report format

```text
Phase 1 Corrective Milestone Result

Baseline:
- Commit:

Root causes:
- Source stepping:
- Missing context menus:
- Workspace hardening:

Implemented:
- Debugger:
- Project UI:
- Source management:
- Startup management:
- Workspace:
- Documentation:

Validation:
- Focused tests:
- scripts\build.cmd:
- Live F10:
- Live Snake Program-NoDemo:
- MultiFileBasics native:
- MultiFileBasics Web:
- Legacy games:
- scripts\smoke-test.cmd:
- git diff --check:

Artifacts:
- Compiler:
- VSIX:
- Example outputs:

Commit and push:
- Commit:
- Push:

Known limitations:
- ...

**MANUAL TESTING REQUESTED From SIN: ...**
```

Do not state that the milestone is complete when the Source Not Available defect remains.
