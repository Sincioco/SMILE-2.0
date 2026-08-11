# File > New Item Template Integration

## Required visible template name

```text
SMILE 2.0 Source Code
```

## Required route

```text
File
-> New
-> File...
-> SMILE 2.0 Source Code
```

This must work when no solution is open.

Also make the same template available through Visual Studio's normal Add New Item experience when practical.

---

# 1. Real VSIX template asset

Package a real Visual Studio item/file template with:

- `.vstemplate`;
- starter `.smile` file;
- exact display name;
- description;
- sensible icon when the existing SMILE icon can be reused;
- default filename such as `NewSource.smile`;
- correct language/category metadata;
- required VSIX asset registration;
- correct package output.

The current project-template asset does not substitute for an item template.

---

# 2. Standalone document behavior

Creating through File > New must:

- open a new `.smile` document;
- activate SMILE content type;
- show syntax highlighting;
- provide IntelliSense;
- provide diagnostics;
- support Save / Save As;
- not silently add itself to a project.

---

# 3. Fallback requirement

If the installed Visual Studio 2026 File > New catalog does not expose a standard VSIX item template through the exact route, implement the smallest dedicated command under File > New named:

```text
SMILE 2.0 Source Code
```

It must use the same starter content and open a standalone unsaved SMILE document.

The user-visible outcome is mandatory; the internal integration mechanism may follow the supported Visual Studio 2026 route.

---

# 4. Packaging verification

After building the VSIX:

- inspect the VSIX archive;
- confirm the item-template files are present;
- confirm manifest/asset registration;
- install the VSIX;
- restart Visual Studio as needed;
- test the actual File > New route.

File presence inside the build tree is not sufficient.
