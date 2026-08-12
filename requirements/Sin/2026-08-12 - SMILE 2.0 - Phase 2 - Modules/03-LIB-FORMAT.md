# Phase 2 Library Projects and Package Format

## 1. Library project extension

Add:

```text
.smilelibproj
```

Use the same shared SMILE project model and project-system implementation where practical.

Recommended XML:

```xml
<SmileProject Version="1.0">
  <PropertyGroup>
    <ProjectKind>Library</ProjectKind>
    <LibraryName>Smile.Math.Extras</LibraryName>
    <Version>1.0.0</Version>
    <OutputName>Smile.Math.Extras</OutputName>
  </PropertyGroup>

  <ItemGroup>
    <SmileSource Include="Clamp.smile" />
    <SmileSource Include="Range.smile" />
  </ItemGroup>
</SmileProject>
```

Rules:

- `.smilelibproj` is recognized by the project factory.
- `ProjectKind` must be `Library`.
- `LibraryName` is required.
- `Version` is required and initially accepts `major.minor.patch`.
- There is no startup file.
- Every source must be a module source.
- One package may contain one or more modules.
- Library output defaults to:

```text
bin\<Configuration>\<OutputName>.smilelib
```

---

# 2. Target-neutral `.smilelib`

A `.smilelib` is a deterministic ZIP-based package containing SMILE source and generated metadata.

It is not:

```text
a Windows DLL
a native object library
a Web-only JavaScript package
```

Required archive entries:

```text
manifest.json
api/public-symbols.json
src/<project-relative sources>
```

Example manifest:

```json
{
  "formatVersion": 1,
  "name": "Smile.Math.Extras",
  "version": "1.0.0",
  "modules": ["Smile.Math.Extras"],
  "sources": [
    "src/Clamp.smile",
    "src/Range.smile"
  ],
  "dependencies": []
}
```

Include deterministic source hashes.

---

# 3. Package safety

The package reader must:

- reject unknown required format versions;
- reject duplicate archive entries;
- reject rooted paths;
- reject `..` path traversal;
- reject unsafe extraction outside the cache;
- validate manifest/module/source consistency;
- validate public API metadata against authoritative analysis;
- reject missing declared sources;
- reject arbitrary executable payload as meaningful library content.

---

# 4. Determinism

For unchanged inputs, build the same package deterministically.

Normalize:

- archive entry order;
- path separators;
- text encoding;
- JSON property ordering;
- ZIP entry timestamps;
- source ordering.

Prefer byte-identical output.

At minimum, manifest/API/source hashes must be identical if byte-identical ZIP output is disproportionately difficult.

---

# 5. Public metadata

Generate `api/public-symbols.json` from the shared semantic model.

Do not hand-author it.

The compiler still compiles packaged SMILE source in Phase 2. Precompiled IR is not required.

---

# 6. Dependencies

The manifest may list exact dependencies:

```json
{
  "name": "Smile.Validation",
  "version": "1.0.0"
}
```

Phase 2 supports direct local references only.

Do not implement:

```text
online restore
version ranges
remote feeds
package registry
automatic downloads
```

A consumer must explicitly supply every required package/project reference.
