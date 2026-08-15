# SMILE 2.0 VSIX Quality-of-Life Enhancements — Validation, Regression, and Delivery

**Testing policy:** focused happy-path evidence, expanded only for a known defect

## 1. Validation goals

Prove that:

- shared symbol resolution identifies the correct declarations;
- documentation comments are parsed safely;
- Quick Info shows educational content;
- F12 navigates to the correct declaration;
- project startup selection changes Visual Studio's real startup project;
- current editor, project, build, and debugging behaviors remain intact.

This is not a request for exhaustive editor automation, long-running game tests, or a new CI suite.

## 2. Shared-language automated tests

Add focused tests to the repository's existing test harness.

### 2.1 Documentation tests

Required:

1. summary extraction;
2. multi-line summary;
3. `@param` extraction in declaration order;
4. `@returns` extraction;
5. optional `@remarks`;
6. case-insensitive tags and parameter names;
7. ordinary comments ignored;
8. malformed tags tolerated;
9. unknown parameter documentation tolerated without compiler errors;
10. no documentation returns an empty documentation object, not null exceptions;
11. comments remain semantically inert and generated program behavior is unchanged.

### 2.2 Symbol-resolution tests

Required:

1. imported alias token resolves to the correct `ModuleSymbol`;
2. alias result has a declaration location on the module identifier;
3. qualified public routine token resolves to `RoutineSymbol`/member;
4. routine declaration location is the identifier span, not the whole function body;
5. case-insensitive qualified use resolves;
6. private member from another module does not resolve as accessible;
7. unqualified local routine resolves;
8. routine parameter/local variable resolves;
9. record type and field resolve when supported;
10. unresolved identifier returns false without throwing;
11. position exactly at an identifier's end resolves consistently;
12. comment/string positions do not resolve.

### 2.3 Existing completion compatibility

At least one focused test must confirm `SmileCompletionService.GetCompletions(...)` still returns the existing qualified `Menu.` members after any presentation refactor.

Do not rewrite all completion tests.

## 3. Build validation

Use the current repository-supported commands. A likely baseline is:

```bat
cmd /c dotnet build "SMILE 2.0.sln" -c Release
```

If repository scripts are the authoritative build path, use them instead and report the exact commands.

Required build evidence:

- `Smile.Language` builds;
- `Smile.Tests` builds and focused tests pass;
- compiler builds;
- native runtime/build artifacts still build;
- `Smile.VisualStudio` builds;
- a `.vsix` package is produced.

Report the exact VSIX path and file version.

## 4. Concise Visual Studio experimental-instance verification

Use the Visual Studio experimental instance with the newly built VSIX. Keep the session short and purposeful.

### 4.1 Test solution

Use or create a temporary solution containing:

- runnable SMILE application A;
- runnable SMILE application B;
- the `Smile.UI` library or a project directly referencing it;
- a source file containing:

  ```smile
  Import Smile.UI.Menu As Menu

  Dim MainMenu As Number
  MainMenu = Menu.Create(MenuStyle, 80, 70, 480, 300, 6)
  ```

The temporary verification project does not need to be committed unless it is a small valuable regression sample already consistent with repository conventions.

### 4.2 Quick Info checks

- Hover `Menu`.
- Hover `Create`.
- Confirm `Create` shows every parameter explanation and return explanation.
- Confirm source/module/provider information.
- Confirm no tooltip inside a string or comment.
- Confirm editing an open source updates the tooltip after the normal analysis delay.

### 4.3 F12 checks

- F12 on `Menu` opens `Menu.smile` at `Module Smile.UI.Menu`.
- F12 on `Create` opens `Menu.smile` at the `Create` declaration.
- F12 on an unresolved identifier does not crash or steal unrelated command behavior.
- F12 on one local routine/variable navigates correctly if implemented.

### 4.4 Startup project checks

- Right-click app A: command appears.
- Right-click app B: command appears.
- Choose app B: Visual Studio recognizes app B as startup.
- Press F5 or Ctrl+F5: app B is the project built/launched.
- Verify app A and app B project XML `<StartupFile>` values did not change.
- Right-click the library project: command is hidden.
- Right-click a `.smile` source: **Set as Startup Source** still works.

### 4.5 Regression checks

Required concise checks:

- IntelliSense after `Menu.` still appears.
- Open one `.smile` file with **File > Open**.
- Open one `.smile` file by double-clicking it in Solution Explorer.
- Build and run one Windows native application.
- Set and hit one SMILE source breakpoint in the native executable.
- Build/publish one Web application through the existing path.
- Exercise one existing project context command, such as Refresh or Edit Project File.

## 5. Failure-driven expansion only

If a focused check exposes a crash, hang, stale-analysis defect, or command-routing conflict, Codex may expand testing. Before doing a long test, record:

```text
Known problem being investigated:
Why the longer test is necessary:
Stop condition:
```

Do not add a permanent broad test burden for a one-time investigation without approval.

## 6. Documentation validation

Inspect `libraries\Smile.UI\Menu.smile` and verify:

- every `Public` routine has a contiguous `'''` documentation block;
- each parameter is documented;
- each function documents its return value;
- sentinel/failure values are explained;
- comments do not change line-sensitive compiler behavior;
- the library still compiles and its API version is not changed merely for comments unless repository policy requires it.

Update `libraries\Smile.UI\API.md` only where it improves public guidance. Avoid duplicating every comment verbatim if the source is already authoritative.

## 7. Version/package validation

- Increment the current VSIX patch version once.
- Keep `Smile.VisualStudio.csproj` and `source.extension.vsixmanifest` synchronized.
- Build a fresh VSIX after the version change.
- Confirm the package contains the updated `Smile.VisualStudio` and `Smile.Language` assemblies.
- Confirm install/upgrade succeeds in the experimental instance.

## 8. Commit and push

Use one coherent commit after validation.

Required subject prefix:

```text
Sin and Codex:
```

Recommended subject:

```text
Sin and Codex: feat(vsix): add educational Quick Info, F12 navigation, and startup-project selection
```

Recommended body:

```text
Summary:
- Adds student-focused SMILE Quick Info, shared symbol navigation, and project startup selection.

Changes:
- Adds shared symbol-at-position, declaration-location, and documentation services.
- Adds async Quick Info and typed Go To Definition handling to the VSIX.
- Adds Set as Startup Project to runnable SMILE project nodes.
- Adds educational documentation comments to Smile.UI.Menu.
- Preserves source-level startup selection and existing editor/project behaviors.

Validation:
- List exact builds and tests.
- List experimental-instance checks.
- List native/Web/debugger regression checks.

Known limitations:
- State any binary-only source navigation limitation, or "None identified."
```

Push the validated commit. Do not amend, rebase, force-push, or rewrite previously pushed history.

## 9. Final report checklist

Codex's final report must include:

- branch;
- commit hash;
- push result;
- exact VSIX version;
- exact VSIX path;
- files added/modified;
- shared-language APIs added;
- documentation syntax implemented;
- number/scope of public Menu routines documented;
- Quick Info scenarios passed;
- F12 scenarios passed;
- startup-project scenarios passed;
- IntelliSense status;
- File > Open status;
- Solution Explorer double-click status;
- native build/run status;
- native breakpoint status;
- Web build/publish status;
- source-level startup-source status;
- any source-unavailable limitation;
- any remaining manual check in **bold**.

Do not report a check as passed unless it was actually performed.
