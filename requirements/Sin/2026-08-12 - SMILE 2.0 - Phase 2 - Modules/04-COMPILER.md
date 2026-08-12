# Phase 2 Compiler, CLI, and Build Graph

## 1. Library build command

Add:

```bat
cmd /c artifacts\compiler\smilec.exe ^
  --project libraries\Smile.Math.Extras\Smile.Math.Extras.smilelibproj ^
  --target library ^
  -o artifacts\libraries\Smile.Math.Extras.smilelib
```

`--target library`:

- requires `--project`;
- rejects a startup source argument;
- loads library sources and references;
- performs shared semantic analysis;
- writes `.smilelib`.

Update CLI usage and diagnostics proportionally.

---

# 2. Consumer CLI

Native:

```bat
cmd /c artifacts\compiler\smilec.exe ^
  examples\LibraryConsumer\Program.smile ^
  --library artifacts\libraries\Smile.Math.Extras.smilelib ^
  -o artifacts\games\LibraryConsumer.exe
```

Web:

```bat
cmd /c artifacts\compiler\smilec.exe ^
  examples\LibraryConsumer\Program.smile ^
  --library artifacts\libraries\Smile.Math.Extras.smilelib ^
  --target web ^
  --output-dir artifacts\web\LibraryConsumer
```

Allow repeated:

```text
--library <path.smilelib>
```

Keep repeated:

```text
--source <support.smile>
```

---

# 3. Project references

Application or library project:

```xml
<SmileProjectReference Include="..\..\libraries\Smile.Math.Extras\Smile.Math.Extras.smilelibproj" />
```

Packaged reference:

```xml
<SmileLibraryReference Include="..\Packages\Smile.Math.Extras.smilelib" />
```

Rules:

- normalized project-relative paths;
- duplicate references rejected;
- missing references diagnosed clearly;
- project-reference cycles rejected;
- library projects may reference other libraries;
- app projects may reference library projects/packages.

---

# 4. Build graph

For project references:

1. normalize and validate the graph;
2. detect cycles;
3. build dependencies first;
4. produce configuration-appropriate `.smilelib`;
5. pass packages to the consumer compiler;
6. rebuild when:
   - output is missing;
   - project/source/reference input is newer;
   - referenced package hash changed.

Do not rebuild unchanged dependencies merely for reassurance.

---

# 5. Extraction/cache

Extract package source to a deterministic intermediate path such as:

```text
obj\Smile\Libraries\<name>\<version>\<package-hash>\
```

Requirements:

- safe paths;
- reuse unchanged extraction;
- invalidate on package hash change;
- no extraction into repository source folders;
- stable physical paths for diagnostics/debugging.

---

# 6. Shared project parsing

Do not create separate XML readers in:

```text
compiler
Visual Studio extension
tests
```

Extend one shared project/reference parser consumed by all three.

---

# 7. Diagnostics

Add clear compiler diagnostics for:

```text
invalid .smilelibproj
missing library project source
invalid package
unsafe package path
missing referenced library
duplicate reference
project-reference cycle
duplicate module provider
unsupported package format
```
