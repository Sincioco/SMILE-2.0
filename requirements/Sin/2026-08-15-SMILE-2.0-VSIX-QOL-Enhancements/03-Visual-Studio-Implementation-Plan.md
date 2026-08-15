# SMILE 2.0 Visual Studio Extension Implementation Plan

**Target:** existing `src\Smile.VisualStudio` net472 VSIX
**Approach:** extend the current MEF/editor/project-system implementation

## 1. Current architecture to preserve

The current extension already has:

- `SmileContentType`;
- syntax classification;
- asynchronous completion;
- diagnostic tagging;
- a per-buffer `SmileAnalysisCache`;
- `SmileProjectWorkspace`, including open-buffer text and referenced project sources;
- a custom `SmileProject` hierarchy/project system;
- custom project/source/folder/reference context menus;
- native and Web build/launch support;
- Windows source-level debugging support.

The implementation should add a few focused files and modify existing command tables/project handling. Do not introduce a parallel language-service host.

## 2. Quick Info implementation

### 2.1 MEF provider

Add a provider equivalent to:

```csharp
[Export(typeof(IAsyncQuickInfoSourceProvider))]
[Name(nameof(SmileQuickInfoSourceProvider))]
[ContentType(SmileContentType.Name)]
internal sealed class SmileQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
{
    // Import ITextDocumentFactoryService.
    // Return a per-buffer source.
}
```

The source should implement:

```csharp
IAsyncQuickInfoSource
```

and obtain the same buffer singleton `SmileAnalysisCache` used by completion and diagnostics.

Do not create a second analysis cache for the same text buffer.

### 2.2 Analysis flow

Inside `GetQuickInfoItemAsync(...)`:

1. Honor the cancellation token.
2. Obtain the trigger point mapped to the subject buffer/current snapshot.
3. Get current analysis from `SmileAnalysisCache`.
4. If the snapshot is newer than the cache, use the same safe workspace-analysis fallback pattern already used by completion.
5. Obtain the `SyntaxTree` for the current file.
6. Call the shared `SmileSymbolService.TryResolve(...)`.
7. Return no item if no symbol resolves.
8. Build a `QuickInfoItem` whose applicable tracking span is the resolved identifier's `ReferenceSpan`.
9. Render structured content from the shared presentation/documentation result.

The method may run on a background thread. Do not call UI-thread-only project services from the analysis/presentation portion.

### 2.3 Visual layout

Use Visual Studio's modern Quick Info content objects when practical, such as classified text and vertical containers. A plain string is an acceptable fallback only if the current SDK makes rich content disproportionately complex.

Recommended visual sections:

```text
[signature]
[summary]

Parameters
  Name — description
  ...

Returns
  description

Remarks
  ...

Defined in
  provider / file
```

Do not display empty headings.

Parameter names and types must come from the routine symbol. Documentation text must come from the shared documentation service.

### 2.4 Disposal

Unsubscribe from cache events only if the Quick Info source subscribes. Implement `Dispose()` correctly and avoid retaining text views/buffers after document close.

## 3. F12 implementation

### 3.1 Use the typed editor command system

Implement an editor command handler scoped to the SMILE content type:

```csharp
[Export(typeof(ICommandHandler))]
[Name(nameof(SmileGoToDefinitionCommandHandler))]
[ContentType(SmileContentType.Name)]
[TextViewRole(PredefinedTextViewRoles.Editable)]
internal sealed class SmileGoToDefinitionCommandHandler
    : ICommandHandler<GoToDefinitionCommandArgs>
{
}
```

Use the exact interface/method signatures supplied by the currently referenced Visual Studio SDK. `IChainedCommandHandler<GoToDefinitionCommandArgs>` is also acceptable if it gives cleaner fallback behavior.

The handler must:

- be exported as the non-generic `ICommandHandler` MEF part;
- act only on SMILE buffers;
- use `args.TextView` and `args.SubjectBuffer`;
- return `false` or invoke the next handler when SMILE does not resolve a navigable target;
- not attach a global `IOleCommandTarget` filter to intercept F12.

### 3.2 Caret mapping

Use the caret position mapped to the subject buffer. Handle projections safely even though ordinary `.smile` documents are expected to be direct buffers.

Resolve through the shared `SmileSymbolService`, not through text matching.

### 3.3 Navigation helper

Add a small VS-specific helper, for example:

```text
src\Smile.VisualStudio\SmileNavigationService.cs
```

Responsibilities:

1. Verify that `DeclarationLocation` exists and has a usable physical path.
2. Switch to the UI thread only for document opening/navigation.
3. Open the document using supported Visual Studio shell/document services.
4. Obtain the `IVsTextView`.
5. convert one-based SMILE line/column to zero-based editor coordinates;
6. select the declaration identifier span or position the caret;
7. ensure the destination is visible;
8. activate the document window.

`VsShellUtilities.OpenDocument(...)`, `IVsUIShellOpenDocument`, or the current supported equivalent may be used. Choose the smallest API that builds against the repository's current SDK.

Do not use EnvDTE text searching to find declarations.

### 3.4 Known symbol without source

If `SmileResolvedSymbol` is valid but its source path is empty/missing:

- mark the command as handled only if a non-modal status message is shown;
- use `SVsStatusbar` or the existing output/status mechanism;
- do not show a modal message box;
- do not throw.

## 4. Project-level Set as Startup Project

### 4.1 Command identifier

Add a unique command constant to `SmileProjectCommands.cs`.

Recommended if still unused:

```csharp
public const uint SetStartupProject = 0x210A;
```

If current HEAD already uses that value, select the next unused value and keep `Commands.vsct` synchronized.

Do not reuse `SetStartupSource`.

### 4.2 VSCT command

Add a button to the SMILE project context menu:

```text
Set as Startup Project
```

Place it in the project build group before Build or immediately after Clean. Use `DynamicVisibility`/`DefaultInvisible` if needed for status control.

Also change the source-node label:

```text
Set as Startup
```

to:

```text
Set as Startup Source
```

Only the label changes for the existing source command.

### 4.3 Command status

Extend `SmileProject.CommandStatus(...)`:

- project root + application project: supported and enabled;
- project root + library project: invisible;
- non-project nodes: invisible;
- current startup project: either:
  - supported/enabled/latched; or
  - supported but disabled.

Use `IVsSolutionBuildManager2.get_StartupProject(out ...)` when available to compare the returned hierarchy with `this`.

Do not depend solely on a cached Boolean; Visual Studio or the user may change startup configuration elsewhere.

### 4.4 Command execution

Extend `SmileProject.ExecuteProjectCommand(...)`:

```csharp
var manager =
    Package.GetGlobalService(typeof(SVsSolutionBuildManager))
    as IVsSolutionBuildManager2;

ErrorHandler.ThrowOnFailure(manager.set_StartupProject(this));
```

Use the actual service/interface signatures available in the current SDK.

After success, request only the minimum hierarchy/UI refresh needed for Visual Studio to update its startup indication. Do not reload or rewrite the `.smileproj`.

The command must not:

- change `StartupFile`;
- edit XML;
- build;
- launch;
- save unrelated documents.

### 4.5 Libraries

`SourceSet.IsLibrary` is the authoritative project-model signal. Hide the command for libraries even if the filename extension is ambiguous.

## 5. Expected files

Codex should adapt names to current conventions, but a likely change set is:

```text
src\Smile.Language\Documentation.cs                         [new]
src\Smile.Language\SymbolResolution.cs                      [new]
src\Smile.Language\Completion.cs                            [modified only as needed to share presentation]
src\Smile.Language\Modules.cs                               [small module location addition if needed]
src\Smile.Language\Semantics.cs                             [small location/presentation additions if needed]

src\Smile.VisualStudio\SmileQuickInfoSource.cs              [new]
src\Smile.VisualStudio\SmileGoToDefinitionCommandHandler.cs [new]
src\Smile.VisualStudio\SmileNavigationService.cs            [new]
src\Smile.VisualStudio\SmileAnalysisCache.cs                [reuse; change only if a small accessor is needed]
src\Smile.VisualStudio\SmileProjectCommands.cs              [modified]
src\Smile.VisualStudio\SmileProjectSystem.cs                [modified]
src\Smile.VisualStudio\Commands.vsct                        [modified]
src\Smile.VisualStudio\Smile.VisualStudio.csproj            [version/reference changes only if required]
src\Smile.VisualStudio\source.extension.vsixmanifest        [patch version bump]

libraries\Smile.UI\Menu.smile                               [documentation comments]
libraries\Smile.UI\API.md                                   [brief documentation convention/update if useful]

src\Smile.Tests\Program.cs                                  [focused tests]
```

Do not create files merely to match this list. Prefer fewer files when responsibilities remain clear.

## 6. Threading and performance

- Quick Info analysis and formatting must be cancellation-aware.
- Do not call `ThreadHelper.JoinableTaskFactory.Run(...)` from hover merely to block on the UI thread.
- Navigation may switch to the UI thread because document services require it.
- Avoid reading every project file on each hover. Reuse `SmileAnalysisCache`.
- Avoid a process-wide unbounded documentation cache.
- Do not build a project for Quick Info or F12.
- Do not save documents for Quick Info or F12.

## 7. Error handling and logging

- Unexpected extension exceptions should be written to the Visual Studio Activity Log using existing conventions.
- Hover failures return no Quick Info rather than displaying an error.
- F12 unresolved results fall through.
- Known source-unavailable results use a status message.
- Startup command failures may use the existing project-command exception/message path.
- No new telemetry dependency is required.

## 8. Versioning

At inspection time, the VSIX version was `2.0.30` in both:

```text
src\Smile.VisualStudio\Smile.VisualStudio.csproj
src\Smile.VisualStudio\source.extension.vsixmanifest
```

Codex must inspect current HEAD and increment the current patch version exactly once after successful implementation. Keep assembly/file/manifest versions synchronized according to the existing repository convention.

Do not change the extension identity:

```text
Smile.VisualStudio.2.0
```

## 9. No-rewrite constraints

Do not:

- replace `SmileProject`;
- migrate to CPS;
- add a language server;
- add Roslyn workspaces;
- duplicate the parser;
- duplicate import resolution in Visual Studio code;
- replace the current completion service;
- remove current context menus;
- redesign project templates;
- change compiler/runtime semantics for documentation comments.

This milestone should look like a natural extension of the current codebase.
