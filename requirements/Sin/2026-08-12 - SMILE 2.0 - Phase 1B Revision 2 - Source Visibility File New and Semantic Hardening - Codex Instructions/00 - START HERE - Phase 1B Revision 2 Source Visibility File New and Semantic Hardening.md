# SMILE 2.0 — Phase 1B Revision 2

## Source Visibility, File > New, Context-Menu Cleanup, and Semantic Hardening

**Repository:** `D:\SMILE 2.0`  
**GitHub:** `Sincioco/SMILE-2.0`  
**Reviewed baseline commit:** `ed71c1818055985e927a4c752a88d823def2841b`  
**Execution mode:** autonomous, single-agent Codex only

This package **supersedes the earlier Phase 1B package**.

The user's latest manual testing refined the source-creation defect. Do not use the earlier description that the project command fails to create a file.

---

# Exact confirmed behavior

When the user right-clicks a SMILE project:

1. **Add new SMILE Source...** is visible.
2. The command creates a new `.smile` file.
3. The new file opens in the SMILE editor.
4. The new file does **not** appear under the project in Solution Explorer.
5. **Add Existing SMILE Source...** then says the file is already included in the project.
6. Restarting Visual Studio does not make the source visible.

Therefore:

```text
physical file creation:              PASS
project-file/source-set inclusion:   apparently PASS; Codex must verify the XML
opening the source in the editor:    PASS
Solution Explorer hierarchy:         FAIL
persistence after Visual Studio restart: FAIL
complete Add New Source feature:     FAIL
```

The “already included” message must not be weakened or bypassed. It is likely correct. The remediation is to make the included source visible and usable in the project hierarchy.

---

# Additional confirmed failure

The user cannot create a SMILE source through:

```text
File
-> New
-> File...
```

because **SMILE 2.0 Source Code** is not installed/listed as a file template.

---

# Locked user-facing names

Use these exact visible names:

```text
Project command:
New SMILE 2.0 Source Code

File template:
SMILE 2.0 Source Code

Existing-file project command:
Add Existing SMILE 2.0 Source Code...
```

Do not retain `Add new SMILE Source...` as the final project command name.

---

# Confirmed working behavior to preserve

- Native Windows x64 compile and launch.
- Web compile and launch.
- Web sound.
- Startup and support-file breakpoints.
- F10 staying in physical `.smile` source.
- Right-click source menu.
- **Set as Startup** for `Program-NoDemo.smile`.
- Cross-file IntelliSense and diagnostics.
- File > Open.
- Solution Explorer double-click.
- Tools > Build SMILE File.
- DirectX/Direct2D and GDI.
- All ten normal and no-demo game editions.

---

# Reading order

Read all numbered files before implementation:

1. `00 - START HERE - Phase 1B Revision 2 Source Visibility File New and Semantic Hardening.md`
2. `01 - Exact Defect Classification and Required User Experience.md`
3. `02 - Solution Explorer Source Visibility and Hierarchy Persistence.md`
4. `03 - New SMILE 2.0 Source Code Project Command.md`
5. `04 - File New Item Template Integration.md`
6. `05 - Dedicated SMILE Context Menus and Command Cleanup.md`
7. `06 - Multi-File Semantic Declaration Hardening.md`
8. `07 - Workspace Lifecycle Multi-Project and Governance Hardening.md`
9. `08 - Autonomous Single-Agent Codex Implementation Instructions.md`
10. `09 - Validation Matrix Definition of Done and Final Report.md`

Also inspect every file under `Repository-Files`.

---

# Scope

This corrective milestone may update:

- `src\Smile.VisualStudio`
- `src\Smile.Language`
- `src\Smile.Compiler` only when required by shared semantic changes
- VSIX manifest/templates/command tables
- focused tests and smoke scripts
- root governance and documentation
- the supplied examples/diagnostic fixtures

Do not begin:

- `Module`
- `Import`
- `Public`
- `Private`
- `.smilelib`
- `.smilelibproj`
- `Type`
- `ByRef`
- image/sprite support
- multiple sound channels
- RPG libraries

Phase 2 begins only after this package is complete and reviewed.
