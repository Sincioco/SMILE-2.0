# Visual Studio Project Context Menus, Add Source, and Startup UI

## Mission

Make SMILE 2.0 projects usable through normal Visual Studio Solution Explorer workflows.

The current hierarchy displays nodes, but only the Solution node has a right-click menu.

## Context-menu integration

Use the standard Visual Studio hierarchy context-menu identities where practical:

```text
Project node: IDM_VS_CTXT_PROJNODE
File/item:    IDM_VS_CTXT_ITEMNODE
Folder:       IDM_VS_CTXT_FOLDERNODE
```

Return the appropriate context-menu property for each hierarchy node and implement the required command routing.

Do not attach project nodes to the Solution menu.

Do not show commands that are visible but nonfunctional.

Commands must also work when invoked from the normal Visual Studio menu or keyboard route when applicable; do not implement mouse-only behavior.

## Project-node menu

Right-clicking a SMILE project must produce a nonempty menu.

Required working actions:

- Build;
- Rebuild;
- Clean;
- Add New SMILE Source File;
- Add Existing SMILE Source File;
- Open Folder in File Explorer, when practical.

### Add New SMILE Source File

Preferred:

- register a `SMILE 2.0 Source File` item template;
- expose it through Add > New Item.

A small, reliable custom filename dialog is acceptable if normal item-template integration is disproportionate.

Rules:

- require a `.smile` extension;
- create UTF-8 without BOM unless repository policy changes;
- create under the project root or selected project-owned source folder;
- add as an ordinary support source:

```xml
<SmileSource Include="Helpers.smile" />
```

- do not add `StartupOnly="true"` by default;
- reject invalid names and normalized duplicates clearly;
- open the real file in the SMILE editor;
- refresh Solution Explorer immediately;
- include it immediately in project-wide IntelliSense, diagnostics, Windows builds, and Web builds;
- require no solution reload.

A sensible initial file body is:

```smile
' SMILE 2.0 support source.
```

Do not insert executable top-level statements.

### Add Existing SMILE Source File

Rules:

- filter for `.smile`;
- reject non-SMILE files clearly;
- store a project-relative `Include`;
- never write an absolute machine path into `.smileproj`;
- when the chosen file is outside the project directory, copy it into the selected project-owned location;
- handle filename collisions clearly;
- add as a support source by default;
- reject duplicates case-insensitively after path normalization;
- refresh the hierarchy and workspace immediately.

## Source-file menu

Right-clicking a project-owned `.smile` file must produce a nonempty menu.

Required working actions:

- Open;
- Set as Startup File;
- Include as Support File;
- Remove from Project;
- Open Containing Folder, when practical.

### Set as Startup File

This is the normal UI for running `Program-NoDemo.smile`.

Expected workflow:

```text
right-click Program-NoDemo.smile
-> Set as Startup File
-> select Windows 64-bit .exe or Web
-> F5 or Ctrl+F5
```

Required behavior:

1. Set `<StartupFile>` to the selected project's relative source path.
2. Mark the newly selected complete program `StartupOnly="true"`.
3. Keep the former startup source `StartupOnly="true"` so it is excluded rather than compiled as support.
4. Preserve every ordinary support source.
5. Save `.smileproj`.
6. rebuild the in-memory source set;
7. refresh the project workspace;
8. refresh hierarchy and command states;
9. visibly identify the new startup source;
10. ensure the next Windows or Web build uses it immediately;
11. prevent native F5 from launching a stale executable built from the previous startup source.

Disable or check the command when the file is already the startup source.

### Startup visual indication

Prefer the standard Visual Studio bold startup-item presentation.

A clear `(Startup)` caption suffix is acceptable only if standard bolding is impractical and the physical filename/moniker remains unchanged.

Do not rename the physical file.

### Include as Support File

This reverses `StartupOnly="true"` after another startup has been selected.

Example:

```xml
<SmileSource Include="Helpers.smile" StartupOnly="true" />
```

becomes:

```xml
<SmileSource Include="Helpers.smile" />
```

Rules:

- disable this for the current startup source;
- refresh all state immediately;
- do not silently change the selected startup.

### Remove from Project

Remove the `<SmileSource>` entry but leave the physical file on disk.

Rules:

- disable for the current startup source;
- never leave `<StartupFile>` pointing to a removed item;
- preserve unrelated source and asset entries;
- refresh hierarchy/workspace immediately.

A permanent Delete command is not required in this corrective milestone.

## Folder-node menu

Project-owned folders must display a nonempty menu.

For source folders, support:

- Add New SMILE Source File;
- Add Existing SMILE Source File;
- Open Folder in File Explorer.

For `Assets`:

- preserve current asset behavior;
- do not add selected files as `<SmileSource>`;
- do not show source-only actions when they would be misleading.

It is acceptable to limit new source creation to the project root if nested source-folder mutation would require a disproportionate redesign. If so, still provide a useful folder menu and document the limitation.

## Project XML updates

Use the existing XML model and preserve unrelated data.

Requirements:

- preserve `PropertyGroup`;
- preserve `ProjectKind`, `OutputName`, `GraphicsBackend`, and `VSync`;
- preserve assets and maps;
- preserve comments/order where practical;
- use consistent project-relative separators;
- update atomically when practical;
- reject duplicates;
- rebuild `SmileProjectSourceSet` from the saved project;
- keep `.smileproj` authoritative.

## Hierarchy notifications

After mutation, fire the appropriate Visual Studio hierarchy/project notifications.

Do not merely replace internal dictionaries while leaving stale UI nodes.

Open documents should remain open.

## Exact Snake acceptance workflow

Codex must personally test:

1. Open `games\Snake\Snake.slnx`.
2. Right-click the `Snake` project and confirm a menu appears.
3. Add a temporary support source through the UI.
4. Confirm it appears, opens, and participates in analysis/build.
5. Right-click `Program-NoDemo.smile`.
6. Choose `Set as Startup File`.
7. Confirm the startup indicator changes.
8. Run `Debug | Windows 64-bit .exe`.
9. Confirm the no-demo version is actually running.
10. Run `Debug | Web`.
11. Confirm the no-demo version is actually published/running.
12. Set `Program.smile` back as startup through the UI.
13. Remove the temporary support source from the project through the UI.
14. Confirm the physical file remains when using Remove from Project, then clean up the temporary test file deliberately.
15. Confirm `git status` contains no test residue.

## Context-menu definition of done

- project right-click works;
- source right-click works;
- relevant folder right-click works;
- Add New Source works;
- Add Existing Source works;
- Set as Startup works;
- Include as Support works;
- Remove from Project works;
- startup is visibly identifiable;
- no restart/reload is needed;
- Windows and Web immediately use the new state;
- existing double-click and File > Open behavior remains intact.
