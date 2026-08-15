# Completion Report, Commit, Push, and Next-Phase Gate

## 1. Final report structure

At the end of Phase 1, Codex must provide one complete report with these headings.

### Summary

State in plain language what a SMILE programmer can now do that was impossible before.

### Repository baseline and final commit

Report:

- branch;
- starting commit inspected;
- final local commit hash;
- pushed commit hash/upstream;
- whether the working tree is clean.

### Architecture implemented

Describe:

- source-document model;
- startup/support distinction;
- semantic pass changes;
- source-aware locations;
- native emission changes;
- Web emission changes;
- Visual Studio project/analysis changes;
- debug-location changes.

### Project-format changes

Show the final exact XML form for:

```xml
<SmileSource ... StartupOnly="true" />
```

Explain how a student switches between normal and no-demo startup sources.

### Command-line changes

Show the final exact syntax for repeated support sources.

### Files changed

List important files/folders by area rather than dumping an unreadable list.

Include:

- language;
- compiler;
- native/Web emitters;
- Visual Studio;
- tests;
- templates;
- ten game projects;
- example;
- docs/requirements.

### Tests and validation

Report exact commands and outcomes, including pass counts where available.

Separate:

- focused tests;
- native sample build/run;
- Web sample build/run;
- Visual Studio checks;
- full build;
- smoke suite;
- diff/working-tree checks.

### Generated artifacts

Report paths to:

- compiler;
- VSIX;
- native `MultiFileBasics.exe`/PDB if retained in normal artifact locations;
- Web publish directory used for validation.

Do not commit generated artifacts unless repository policy already tracks them.

### Legacy-game result

State whether all ten games and required normal/no-demo variants remain green on the current Windows/Web smoke paths.

### Known limitations

State only real limitations. Include that modules/imports/libraries are intentionally Phase 2 and browser `.smile` breakpoints remain outside Phase 1.

### Commit message

Paste the actual subject and concise body summary.

### Push result

State the upstream and push result. If push fails because of authentication/network after reasonable retries, the implementation may remain locally committed, but report the exact failure and do not claim it was pushed.

### Manual testing

End the entire report with one of these bold forms:

```text
**MANUAL TESTING REQUESTED From SIN:** None.
```

or:

```text
**MANUAL TESTING REQUESTED From SIN:**
- Open ...
- Perform ...
- Expected ...
- Codex could not perform this because ...
```

Do not place additional text after the manual-testing section.

---

## 2. Commit contents

When green, the commit must include all intended unstaged repository changes, including:

- this preserved requirement package if newly added;
- implementation files;
- tests;
- project migrations;
- template updates;
- example files;
- documentation.

Before `git add -A`, inspect for accidental:

- credentials;
- browser profiles;
- logs containing secrets;
- absolute machine paths in generated output;
- `bin`/`obj` directories;
- publish folders;
- executables/PDBs not normally tracked;
- temporary files.

Do not omit a relevant changed source/document merely because it was unstaged when the session began. Sin has authorized the complete coherent repository work to be staged, committed, and pushed after review and green tests.

Never discard user work.

---

## 3. Next-phase gate

After commit/push, stop implementation work.

Do not begin:

```text
Module
Import
Public
Private
Option Explicit
.smilelibproj
.smilelib
ProjectReference
```

Sin will tell ChatGPT that Codex committed the phase. ChatGPT will inspect the actual repository, evaluate the result, and prepare the Phase 2 package based on the implementation that really exists.

The expected next phase is:

```text
SMILE 2.0 Reusable Components — Phase 2
Modules, Imports, and Target-Neutral SMILE Libraries
```

Its details are intentionally not frozen here.
