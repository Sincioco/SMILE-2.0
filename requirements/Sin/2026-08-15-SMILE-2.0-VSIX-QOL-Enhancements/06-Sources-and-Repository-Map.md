# Sources and Current Repository Map

**Inspection date:** August 15, 2026
**Repository:** `Sincioco/SMILE-2.0`
**Purpose:** help Codex anchor the handoff to the existing implementation before adapting it to current HEAD

## 1. Repository implementation anchors

### Visual Studio completion and analysis

```text
src\Smile.VisualStudio\SmileCompletionSource.cs
```

Current behavior observed:

- exports `IAsyncCompletionSourceProvider`;
- scopes completion to the SMILE content type;
- uses `ITextDocumentFactoryService` for file identity;
- reuses a per-buffer `SmileAnalysisCache`;
- calls `SmileCompletionService.GetCompletions(...)`;
- displays `SmileCompletion.Description`.

```text
src\Smile.VisualStudio\SmileAnalysisCache.cs
```

Current behavior observed:

- owns debounced per-buffer analysis;
- knows current file and containing `.smileproj`/`.smilelibproj`;
- calls `SmileProjectWorkspace.Analyze(...)`;
- publishes `SmileAnalysisResult`;
- registers open source buffers.

```text
src\Smile.VisualStudio\SmileProjectWorkspace.cs
```

Current behavior observed:

- tracks project ownership;
- includes unsaved open-buffer text;
- analyzes multi-file project sources;
- loads directly referenced library project/package sources;
- returns the shared `SmileAnalysisResult`.

These three files are the integration path for Quick Info and F12. Do not bypass them with separate disk scans.

### Shared semantic model

```text
src\Smile.Language\SmileLanguage.cs
src\Smile.Language\Semantics.cs
src\Smile.Language\Modules.cs
src\Smile.Language\Completion.cs
src\Smile.Language\Text.cs
```

Current useful objects observed:

- `SmileAnalysisResult.SyntaxTrees`;
- `SmileAnalysisResult.SemanticModel`;
- `SemanticModel.Modules`;
- `SemanticModel.GetImports(SourceText)`;
- `RoutineSymbol.DeclarationLocation`;
- `VariableSymbol.DeclarationLocation`;
- `SmileType.DeclarationLocation`;
- `RecordFieldSymbol.DeclarationLocation`;
- `SmileModuleMember.DeclarationLocation`;
- `ModuleSymbol.SyntaxTrees`;
- `SourceLocation.FilePath`, `Line`, and `Column`;
- existing completion signature/provider/capability formatting.

No separate Visual Studio parser is needed.

### Custom SMILE project system and menus

```text
src\Smile.VisualStudio\SmileProjectSystem.cs
src\Smile.VisualStudio\SmileProjectCommands.cs
src\Smile.VisualStudio\Commands.vsct
```

Current behavior observed:

- `SmileProject` implements `IVsUIHierarchy`, `IVsProject2`, and `IOleCommandTarget`;
- it displays its own project/source/folder/reference context menus;
- project commands are routed through `CommandStatus(...)` and `ExecuteProjectCommand(...)`;
- project root commands currently include Build, Rebuild, Clean, source/reference operations, Edit Project File, Open Project Folder, and Refresh;
- source nodes already have a command that changes the SMILE project's startup source;
- no project-level **Set as Startup Project** command is currently defined.

The new command belongs in these existing paths.

### Version files

```text
src\Smile.VisualStudio\Smile.VisualStudio.csproj
src\Smile.VisualStudio\source.extension.vsixmanifest
```

Version observed during inspection:

```text
2.0.30
```

Codex must inspect current HEAD and increment the actual current patch version, not assume this value is still current.

### Example library API

```text
libraries\Smile.UI\Menu.smile
libraries\Smile.UI\API.md
```

Observed declaration:

```smile
Public Function Create(ByRef Style As Core.MenuStyle, X As Number, Y As Number, Width As Number, Height As Number, VisibleRows As Number) As Number
```

The source currently has ordinary comments/implementation but no educational documentation block immediately above this routine. `API.md` lists the signature and general menu behavior but does not explain every `Create` parameter.

## 2. Official Visual Studio SDK references

Codex should use the current SDK reference in the repository and verify exact signatures during implementation.

### Async Quick Info

- `IAsyncQuickInfoSourceProvider`
- `IAsyncQuickInfoSource`
- `IAsyncQuickInfoSource.GetQuickInfoItemAsync`
- `QuickInfoItem`

Microsoft Learn:

```text
https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.language.intellisense.iasyncquickinfosourceprovider?view=visualstudiosdk-2022
https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.language.intellisense.iasyncquickinfosource.getquickinfoitemasync?view=visualstudiosdk-2022
```

Important documented behavior: `GetQuickInfoItemAsync` returns the item plus applicable tracking span and is called on a background thread.

### Typed editor commands and Go To Definition

- `ICommandHandler<T>`
- `IChainedCommandHandler<T>`
- `GoToDefinitionCommandArgs`

Microsoft Learn:

```text
https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.commanding.icommandhandler?view=visualstudiosdk-2022
https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.text.editor.commanding.commands.gotodefinitioncommandargs?view=visualstudiosdk-2022
```

The editor command system supports MEF handlers scoped by content type and text-view role. This is the intended F12 integration point for the SMILE content type.

### Solution startup project

- `SVsSolutionBuildManager`
- `IVsSolutionBuildManager2.get_StartupProject`
- `IVsSolutionBuildManager2.set_StartupProject`

Microsoft Learn:

```text
https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.interop.ivssolutionbuildmanager2.get_startupproject?view=visualstudiosdk-2022
https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.interop.ivssolutionbuildmanager2.set_startupproject?view=visualstudiosdk-2022
```

These APIs get or set the hierarchy that Visual Studio runs when F5 is pressed.

## 3. Architectural interpretation

The repository already contains the difficult foundations:

- a semantic model with declaration spans;
- import/module/member metadata;
- referenced-library source loading;
- open-buffer tracking;
- editor content type and completion;
- a custom project hierarchy and context-menu router;
- native/Web build and launch support.

Therefore this milestone should be a targeted extension:

```text
Smile.Language
  ├── symbol at position
  ├── declaration location
  ├── documentation extraction
  └── signature/presentation data
          │
          ├── existing completion
          ├── VS Quick Info
          └── VS Go To Definition

SmileProject
  └── project context command
          └── IVsSolutionBuildManager2.set_StartupProject(this)
```

It should not become a new compiler, LSP, Roslyn integration, or project system.

## 4. Source-of-truth precedence

When this handoff, an old screenshot, and current repository code differ:

1. user-visible requirements in documents 01–05 govern the desired behavior;
2. current `AGENTS.md` governs repository policy;
3. current HEAD governs exact class names, SDK versions, and integration details;
4. official Visual Studio SDK documentation governs API contracts;
5. this map is an inspection aid, not permission to overwrite newer code.

## 5. Confidence and uncertainties

High confidence:

- Quick Info can be added through the existing SMILE content type and analysis cache.
- Module/member F12 can use current semantic declaration locations.
- The custom project context menu can set the real startup hierarchy through the solution build manager.

Implementation-time checks still required:

- exact typed command-handler method signatures in the installed SDK;
- exact document-opening/navigation overload most convenient for net472;
- whether current Visual Studio automatically refreshes the startup-project visual indicator or needs a small hierarchy/UI refresh;
- how binary-only `.smilelib` packages represent source paths.

These are normal integration details and do not change the required behavior.
