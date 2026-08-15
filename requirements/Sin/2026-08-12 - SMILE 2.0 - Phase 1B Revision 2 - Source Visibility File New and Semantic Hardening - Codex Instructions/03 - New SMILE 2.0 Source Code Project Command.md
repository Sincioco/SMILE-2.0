# New SMILE 2.0 Source Code Project Command

## Exact command name

The project right-click command must be named exactly:

```text
New SMILE 2.0 Source Code
```

Do not use:

```text
Add new SMILE Source...
Add New SMILE Source...
New SMILE Source...
```

---

# 1. Dialog behavior

The command opens a foreground dialog owned by the active Visual Studio window.

It must not appear behind Visual Studio or on another monitor without activation.

The dialog asks for a source filename and may default to:

```text
NewSource.smile
```

Rules:

- append `.smile` when no extension is entered;
- reject another extension;
- reject invalid Windows filenames;
- reject path traversal;
- reject normalized duplicates case-insensitively;
- do not overwrite an existing physical file;
- Cancel performs no mutation.

---

# 2. Atomic operation

Treat the command as one user operation:

```text
validate
-> create physical file
-> add one project-relative SmileSource entry
-> reload source set
-> rebuild hierarchy
-> notify Visual Studio
-> refresh workspace
-> open source
```

If a later step fails:

- do not leave a phantom project entry;
- do not leave a duplicate entry;
- remove a newly created empty physical file only when rollback is safe;
- show a clear Visual Studio-owned error.

---

# 3. Initial content

Create UTF-8 without BOM using repository convention.

Suggested content:

```smile
' SMILE 2.0 support source.
```

Do not insert executable top-level statements, `Game Window`, or `End Program`.

The new item is an ordinary support source:

```xml
<SmileSource Include="Battle.smile" />
```

Do not mark it `StartupOnly`.

---

# 4. Postconditions

Before returning success, verify:

- file exists;
- project XML contains one normalized entry;
- current source set contains it;
- current hierarchy contains it;
- the hierarchy item is visible;
- workspace registration contains it;
- editor opens the physical source.

Do not report success after only `OpenPath(sourcePath)`.

---

# 5. Native/Web proof

For the live acceptance source, add a small routine:

```smile
Function AddedSourceValue()
    Return 42
End Function
```

Call it from the startup source.

Prove both:

```text
Windows native build
Web build
```

consume the newly created support source.
