# Phase 2 Visual Studio Project and Reference UX

## 1. New project template

Add:

```text
SMILE 2.0 Library
```

to Create New Project.

It creates:

```text
<name>.smilelibproj
Module.smile
```

Starter source:

```smile
Module <sanitized.project.name>

Public Const VERSION_MAJOR = 1

End Module
```

Use the same SMILE project type implementation where practical.

---

# 2. Library project commands

Library project context menu:

```text
Build
Rebuild
Clean
New SMILE 2.0 Source Code
Add Existing SMILE 2.0 Source Code...
Add SMILE 2.0 Library Reference...
Refresh SMILE 2.0 Project
Edit SMILE 2.0 Project File
Open Project Folder
```

F5 does not run a library.

Display a clear message:

```text
SMILE 2.0 library projects are built and referenced; they are not directly executable.
```

Do not launch a stale executable.

---

# 3. References node

Application and library projects display:

```text
References
    Smile.Math.Extras
```

Each reference item exposes:

- display name;
- exact version when known;
- project or package path;
- project/package kind;
- resolved/missing/invalid state.

Reference context menu:

```text
Remove Reference
Open Containing Folder
```

---

# 4. Add reference command

Add:

```text
Add SMILE 2.0 Library Reference...
```

Allow selecting:

```text
.smilelibproj
.smilelib
```

Store a normalized relative reference.

Reject duplicates.

Refresh immediately:

- References node;
- module resolution;
- IntelliSense;
- diagnostics;
- native/Web build graph.

Use the accepted Phase 1 project refresh/parent hierarchy implementation. Do not build a second hierarchy system.

---

# 5. Source management

Library source files use existing commands:

```text
New SMILE 2.0 Source Code
Add Existing SMILE 2.0 Source Code...
Remove from Project
```

Immediate Solution Explorer refresh remains a mandatory regression gate.

---

# 6. Missing references

A missing reference remains visible with an error state.

Build fails clearly.

Restoring the project/package path clears the state automatically through the existing refresh/file-monitoring architecture.

---

# 7. Project references in a solution

When both consumer and library projects are loaded:

- the consumer recognizes the loaded library project;
- library source changes invalidate consumer module analysis;
- unsaved library buffers participate in IntelliSense when practical;
- build order is dependency-first;
- native debugging can enter project-referenced library source.

Do not require the user to manually rebuild the library merely for completion refresh.
