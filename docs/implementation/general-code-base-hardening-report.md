# SMILE 2.0 General Code-Base Hardening — Post-Implementation Report

**Implementation date:** 2026-08-19

**Starting commit:** `5708be3e9281d3197d767eb825e0a1068accb65a`

**Ending implementation commit:** `5b4ac9c2b539f0abda4cb45a6dfcaaacc3d23c68`

**Branch:** `main`

## Result

Pass A and Pass B completed successfully. The priority package, process, compiler-lifecycle, publication, and native Debug hardening is implemented, and every non-optional Pass B acceptance criterion is covered. The full official smoke gate passed from the repository root, repository parent, and an unrelated caller directory containing spaces. Only move-only decomposition of the largest test, smoke, compiler, and language files was intentionally deferred because combining thousands of relocated lines with functional hardening would reduce reviewability; the established entry points and authoritative implementations remain unchanged.

## Pass A — Priority hardening completed

### `.smilelib` bounded resource policy

- Physical package maximum: 64 MiB
- Maximum entries: 1,026 (manifest, public API, and up to 1,024 sources)
- Maximum entry-name length: 512 characters
- Maximum manifest bytes: 4 MiB
- Maximum public API bytes: 16 MiB
- Maximum individual source bytes: 4 MiB
- Maximum source count: 1,024
- Maximum total expanded bytes: 64 MiB
- Compression-ratio rule, if any: none; actual expanded bytes are bounded while streaming
- Diagnostic code(s): `SML3210` for a package resource limit and `SML3211` for package-output lock timeout

Implementation summary:

`SmileLibraryResourcePolicy.Production` is the single internal policy authority. Package loading performs cheap metadata checks first, then hashes and copies through bounded streams without reading the entire physical package into memory. Actual bytes, rather than advisory ZIP lengths, drive the manifest, public API, source, and aggregate limits; checked arithmetic prevents aggregate overflow. Validation failure does not publish a partial provider into the cache. Format 6 remains the only supported `.smilelib` format.

### External process and cancellation hardening

- Native tool timeout policy: `vswhere.exe` is limited to 30 seconds; the combined C/MASM/link invocation is limited to 10 minutes. `SML5005` distinguishes timeout, `SML5006` cancellation, and `SML5003` an ordinary tool failure.
- VSIX compiler timeout/cancellation behavior: Visual Studio compiler launches are limited to 10 minutes. The project system's Stop action cancels the active build token and reports `SML5006`; timeout reports `SML5005`.
- Process-tree termination behavior: the shared managed runner uses `Process.Kill(entireProcessTree: true)`; the .NET Framework VSIX path uses `taskkill /T /F` with direct-process fallback.
- Output-lock timeout behavior: native and Web publication wait at most 30 seconds for a normalized, case-insensitive destination lock and report `SML5008`. Abandoned locks recover, and different destinations remain independent.

### Compiler intermediate ownership and cleanup

- Repository build intermediates: no compiler invocation falls back to an arbitrary caller working directory or repository-level `artifacts\temp` scratch path.
- Project/loose-source intermediates: `<owner-directory>\obj\Smile\Compiler\<base>.<process-id>.<guid>\`.
- `--keep-temp` behavior: retains the unique intermediate directory and prints the exact directory, assembly, object, and optional Debug C/object paths.
- Failure cleanup behavior: all compiler-owned files and the unique directory are removed on success and every handled failure unless `--keep-temp` is present.

Repository-root discovery, compiler-install/runtime lookup, project ownership, and loose-source ownership are separate concepts; an installed compiler can locate its adjacent runtime without pretending it is running in a source checkout.

### Transactional output publication

`.smilelib`:

Library output is written to a unique same-directory sibling, flushed to disk, reopened and fully validated, then atomically replaces or moves into the destination while holding the finite same-target lock. Failed staging, validation, replacement, or lock acquisition preserves the previous valid package and removes owned residue.

Native executable/PDB:

The toolchain links to unique staged EXE/PDB paths. Generated outputs and assets enter one managed publication transaction with destination backups and rollback. Failure preserves the previous EXE/PDB/assets; success removes superseded backups and stale owned files.

Web output:

All generated Web files and assets are prepared beneath a unique same-volume staging root, then committed as one managed set. A generated-file or asset failure cannot leave a mixed old/new site, and unrelated destination files are outside the managed set.

Asset publication:

Direct and compiler-driven publication stage every asset, commit the new manifest only after replacements are ready, roll back replacements on commit failure, and delete stale previously owned assets only after success. Malformed, mismatched, and legacy manifests retain their established warning/safety behavior.

Concurrent same-target publication:

Normalized destination locks serialize same-target native, Web, and library publication; a compiler output lock encloses its associated asset transaction. The wait is finite, an abandoned owner is recoverable, and different outputs may publish concurrently.

### Native Debug identifier lowering

- Safe identifier scheme: helper parameters are deterministic ASCII ordinals (`smile_debug_v0`, `smile_debug_v1`, and so on). A source identifier becomes a local alias only when it is a safe non-keyword C identifier; keywords, Unicode, long identifiers, and sanitizer collisions retain the ordinal name.
- Unicode path handling: generated Debug C is explicit UTF-8, including physical multi-file `#line` paths.
- Debug/PDB compatibility evidence: `NativeDebugIdentifiers.smile` covers a C keyword, mixed case, Latin Unicode, non-Latin Unicode, and a long identifier. A real Release compiler `--debug` build succeeded through MSVC, produced a 111,104-byte EXE and 1,806,336-byte PDB, and the executable printed `15`.

## Pass B — Engineering recommendations completed

### Smoke gate/root handling

`scripts\smoke-test.cmd` derives its repository root from its own location, `pushd`s before any gate, exports `SMILE_REPOSITORY_ROOT`, invokes the unchanged ordered gate as a subroutine, and guarantees `popd` through the single outer exit path. Managed source-contract tests validate that environment value or locate `SMILE 2.0.sln` plus `AGENTS.md` by walking upward from `AppContext.BaseDirectory`; caller `Environment.CurrentDirectory` is no longer an input.

The complete smoke gate passed from `D:\SMILE 2.0`, `D:\`, and `C:\Users\louie\Documents\SMILE 2.0 - SinBASIC`.

### Doctor/prerequisite validation

`scripts\doctor.ps1` is a small, non-installing check for Windows x64, Windows PowerShell 5.1+, exact SDK 10.0.302 and its .NET 10 targeting pack, `vswhere`, the Visual Studio C++ x64 and extension-development surfaces, `vcvars64.bat`, MSBuild, `link.exe`, `ml64.exe`, Node.js 20+, and writable artifact/intermediate locations. It accumulates failures, prints a remediation for each, and exits nonzero on a blocking issue. README prerequisites now name Node and the doctor command.

### Test-process timeouts

`scripts\Invoke-BoundedProcess.ps1` launches an executable with arguments, captures stdout/stderr asynchronously, returns the child exit code, enforces a caller-selected timeout, kills the complete tree with `taskkill /T /F` plus fallback, and identifies the timed-out command with exit code 124. `run-bounded-test.cmd` provides the smoke-friendly wrapper. Every direct native runtime/generated-program launch in the official smoke gate—62 invocations including repeated lifetime modes—uses a generous 60-second limit. Existing Node behavioral execution retains its own finite timeout.

`scripts\test-bounded-process.ps1` permanently verifies captured output, nonzero exit propagation, timeout reporting, and bounded termination.

### Managed test organization

The lightweight executable runner remains. `TestInfrastructure.cs` now owns `TestContext`, full `exception.ToString()` failure capture, counts, and stable repository-root discovery. `Program.cs` preserves all test names and deterministic registration order, delegates its existing `Run` helper to that context, and reports the suite accurately instead of the stale “project-option” wording.

A thematic relocation of the 6,000-line registration file was intentionally deferred. It would be a large move-only diff unrelated to the functional hardening and would obscure review of new regression coverage. Revisit it as a dedicated mechanical commit when the next test-focused maintenance phase begins.

### Smoke script organization

The stable `scripts\smoke-test.cmd` entry point and fail-fast order remain. Root/cleanup orchestration, prerequisite checks, and bounded process execution moved to focused scripts. A full stage-by-stage split of the 1,300-line batch body was deferred for a dedicated move-only change because it is optional and would make this already broad hardening diff harder to audit. Revisit it when a smoke stage next needs substantial maintenance.

### SDK/toolchain pinning

- `global.json`: SDK `10.0.302`
- SDK roll-forward policy: `latestPatch`, with prerelease SDKs disabled
- `LangVersion` decision: centralized explicit C# `14.0`; both per-project `latest` settings were removed

### Central managed build/warning policy

`Directory.Build.props` contains only genuinely shared managed policy: C# 14, deterministic compilation, warning level 5, and warnings-as-errors for `Smile.Language`, `Smile.Compiler`, and `Smile.Tests`. Visual Studio/template projects retain their project-specific suppressions and are not placed under a blanket warnings-as-errors rule. The stricter core gate found and forced correction of a nullable dereference during implementation.

### NuGet locking evaluation

Implemented. The VSIX, project-template, and item-template graphs now commit `packages.lock.json`, enable lock-file generation, and restore in locked mode. A normal locked restore and the complete solution build passed. README documents the intentional `RestoreLockedMode=false` update procedure so package graph changes remain explicit and reviewable.

### `.gitattributes`

The root policy makes managed/source/project/script/document line endings explicit, preserves formatter-owned `.smile` LF files, and marks images, audio, binaries, VSIX/ZIP archives, and `.smilelib` packages as binary. No historical mass normalization was performed.

### Visual Studio warning diagnostics

`SmileCompilerDiagnosticParser` is shared internal logic that parses `error` and `warning` SML lines, ignores malformed/unrelated output, preserves Windows paths containing spaces and parentheses, and retains one-based diagnostic positions. `SmileBuildService` clears stale rows as before, maps severity to `TaskErrorCategory.Error` or `Warning`, then normalizes line/column to the Visual Studio zero-based API. Focused managed coverage includes one warning, one error, mixed output, both path cases, and a malformed line.

### Native build-flag exception review

MediaPlayer CRT/runtime choice:

The C++/WinRT MediaPlayer translation unit intentionally uses the DLL CRT to match the import libraries selected by the custom final link. Its nontrivial state is allocated with `HeapAlloc`, placement-constructed, explicitly destructed, and released with `HeapFree` behind the C ABI; no CRT-owned allocation crosses into the C runtime. Project comments, architecture documentation, and a source-contract test preserve this boundary. The native runtime explicitly retains warning level 4, SDL checks, and buffer-security checks.

Debug helper `/GS` policy:

The generated Debug C helper is not the entry point and contains no arrays or writable buffers. The native program uses custom `/entry:main`, bypassing CRT startup and therefore normal security-cookie initialization; enabling `/GS` on the helper would claim protection whose cookie was never initialized. `/GS-` remains limited to this one generated, buffer-free helper compile. Production runtime C/C++ remains `/GS` protected and production SMILE code is MASM. Code comments, architecture documentation, and a focused guard test cover the decision.

### Mechanical large-file decomposition

Deferred with no behavior change. `Semantics.cs`, `MasmEmitter.cs`, `WebEmitter.cs`, and related parser/module units remain the single authoritative implementations. Moving thousands of lines after functional compiler/lifecycle hardening would reduce rather than improve reviewability. Revisit as one or more dedicated move-only commits before a substantial feature phase directly expands those files, with build and full smoke after each move.

## Public compatibility

- SMILE syntax: no change
- diagnostic behavior/codes: yes, only hardening diagnostics were added (`SML3210`, `SML3211`, `SML5005`, `SML5006`, and `SML5008`); normal native tool failure remains `SML5003`, and compiler warnings now appear as warnings in the VS Error List
- `.smilelib` formatVersion: unchanged at 6
- public Smile.UI API/version: unchanged at 2.0.0
- public Smile.Game API/version: unchanged at 2.0.0
- public Smile.RPG API/version: unchanged at 1.2.1
- VSIX version: unchanged at 2.0.48; artifact verification confirms synchronized versions
- native runtime ABI: unchanged
- Web runtime public behavior: unchanged

No library or VSIX version was bumped for internal hardening. Oversized or adversarial packages that exceed the documented supported resource policy are now deliberately rejected before trust or cache publication.

## Focused regression tests added

- Exact-at-limit and one-over-limit `.smilelib` fixtures cover physical bytes, entry count, entry-name length, manifest bytes, API bytes, individual source bytes, source count, and aggregate expanded bytes.
- Highly compressed oversized entries prove limits use actual streamed expansion rather than ZIP metadata or a compression-ratio heuristic.
- Invalid-package tests prove no partial provider-cache publication; package transaction tests prove last-known-good preservation, residue cleanup, same-target serialization, and independent destinations.
- `BoundedProcessRunner` tests cover stdout/stderr capture, nonzero exit, start failure, timeout, cancellation, and descendant-tree termination.
- Output lock tests cover contention timeout, abandoned-owner recovery, same-target exclusion, and different-target concurrency.
- Native/Web/asset transaction tests inject emission, staged-write, copy, and commit failures and verify rollback, stale-file timing, unrelated-file preservation, and residue cleanup.
- Intermediate tests cover repository, project, loose-source, installed-compiler, success, all synthetic failure phases, and exact `--keep-temp` reporting.
- `NativeDebugIdentifiers.smile` and generated-source assertions cover keywords, case, Unicode, long names, ordinal collision safety, UTF-8 paths, and real MSVC/PDB generation.
- Visual Studio source-contract coverage proves Stop reaches the active cancellation token and child-tree termination path.
- Repository-root tests run the managed suite outside the checkout; the complete smoke gate additionally ran from all three required caller locations.
- The PowerShell bounded-process self-test covers output, nonzero exit, timeout code/message, and timely termination.
- Diagnostic parser tests cover errors, warnings, mixed output, spaces, parentheses, and ignored malformed lines.
- Native build-policy guards cover explicit runtime buffer protection, the MediaPlayer CRT rationale, and the narrow `/GS-` helper exception.

## Validation performed

```text
scripts\build.cmd
Passed. Compiler and VSIX produced; one expected NU1503 solution-restore warning for the native vcxproj.

dotnet run --project src\Smile.Tests\Smile.Tests.csproj -c Release --no-build
Passed: 284 SMILE language, compiler, project, completion, and timing tests.
Also passed from C:\Users\louie\Documents with the project path absolute.

powershell -NoProfile -ExecutionPolicy Bypass -File scripts\test-smile-formatter.ps1
Passed: 13 focused formatter integration tests.

powershell -NoProfile -ExecutionPolicy Bypass -File scripts\format-smile-style.ps1 -Check -FormatLongIf
Passed: 278 SMILE source files.

powershell -NoProfile -ExecutionPolicy Bypass -File scripts\doctor.ps1
Passed every required environment check.

powershell -NoProfile -ExecutionPolicy Bypass -File scripts\test-bounded-process.ps1
Passed output, exit-code, timeout, and termination checks.

dotnet restore src\Smile.VisualStudio\Smile.VisualStudio.csproj --locked-mode
Passed with the committed VSIX/template lock graphs.

scripts\smoke-test.cmd
Passed from D:\SMILE 2.0, D:\, and C:\Users\louie\Documents\SMILE 2.0 - SinBASIC.
Each run selected 284 managed tests, 13 formatter tests, 278 style files,
39 native graphics/audio-focus checks, and 38 native Text checks, then passed the full native/Web/package/game gate.

powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-artifacts.ps1
Passed as the final smoke stage: format-6 libraries, native x64 GUI files, game assets,
VSIX payload/version 2.0.48, viewport mappings, and DPI calculations verified.

git diff --check
Passed with no whitespace errors. Expected working-tree EOL notices reflect the newly declared policy.
```

## Manual validation performed

- Installed Visual Studio build/cancel/error-list behavior: not interactively exercised; VSIX built successfully and the cancellation/diagnostic mapping paths have focused managed/source-contract coverage.
- Native Debug/PDB behavior: real `NativeDebugIdentifiers.smile --debug` MSVC build completed, produced EXE/PDB outputs, and ran with output `15`; interactive F10/hover was not required.
- DirectX/GDI graphical acceptance: not required; both targets compiled and artifact/runtime checks passed, while the smoke gate still reports manual gameplay separately.
- Web browser acceptance: not required; the repository Node host passed the complete Web behavioral/parity matrix.
- Audio acceptance: not required; native audio-focus checks and compilation passed, while audible playback remains a manual product check.

## Known limitations / intentionally deferred recommendations

- Thematic `Smile.Tests` registration split: deferred because moving roughly 6,000 lines would swamp functional review. Revisit as a dedicated mechanical test-maintenance commit when the next broad test change is scheduled.
- Stage-by-stage `smoke-test.cmd` decomposition: deferred because the permanent entry, root handling, helper extraction, ordering, and fail-fast behavior are already hardened. Revisit when an individual smoke stage next needs substantial maintenance.
- Oversized compiler/language partial-file decomposition: deferred because it is move-only and explicitly last priority. Revisit before a new feature substantially expands `Semantics.cs`, `MasmEmitter.cs`, `WebEmitter.cs`, or parser/module units.
- Separate `release-validation.cmd`: not added because the pinned SDK, doctor, locked restore, build, smoke, and artifact verification already provide the recommended release evidence without a duplicate entry point. Revisit if CI needs an explicit clean-worktree orchestration command.

## Files significantly changed

- `src\Smile.Language\SmileLibraries.cs`, `docs\libraries\README.md`: bounded format-6 loading/publication and package contract.
- `src\Smile.Compiler\BoundedProcessRunner.cs`, `BuildLifecycle.cs`, `NativeToolchain.cs`, `CompilerDriver.cs`, `WebOutputWriter.cs`: bounded processes, owned intermediates, locks, staged outputs, rollback, and Debug lowering.
- `src\Smile.Language\SmileProjectAssets.cs`: staged asset publication, legacy manifest safety, rollback, and stale ownership.
- `src\Smile.VisualStudio\SmileBuildService.cs`, `SmileProjectSystem.cs`, and `src\Smile.Language\SmileCompilerDiagnosticParser.cs`: VS timeout/cancellation and warning/error-list behavior.
- `scripts\smoke-test.cmd`, `Invoke-BoundedProcess.ps1`, `run-bounded-test.cmd`, `test-bounded-process.ps1`, and `doctor.ps1`: location-independent, prerequisite-aware, bounded validation.
- `src\Smile.Tests\Program.cs`, `TestInfrastructure.cs`, and `Fixtures\NativeDebugIdentifiers.smile`: regression coverage, runner context, and Debug compatibility fixture.
- `global.json`, `Directory.Build.props`, `.gitattributes`, and the three VSIX/template `packages.lock.json` files: reproducible SDK, compiler, warnings, restore, and repository text/binary policy.
- `README.md` and `docs\architecture\README.md`: developer workflow and native flag/publication/lifecycle contracts.

## Final repository state

- Working tree clean: no; the pre-existing user-owned edit to `games\SinStarI\SinStarI.slnx` remains intentionally uncommitted and was excluded from every hardening commit
- Commit pushed: yes
- Final implementation commit SHA: `5b4ac9c2b539f0abda4cb45a6dfcaaacc3d23c68`

The hardening implementation was delivered in three cohesive pushed commits:

- `cd4bf78b01716aef393d1aabac34116ffeed9a9f` — SMILE library package boundaries
- `9e476cbdc6f7f81af575a21a370f04b9d8695d8b` — compiler build/publication lifecycle
- `5b4ac9c2b539f0abda4cb45a6dfcaaacc3d23c68` — developer validation and build policy

## Final assessment

The general-review hardening is complete. Supported packages and build outputs now have explicit resource, time, ownership, concurrency, and rollback boundaries; developer validation is reproducible and cannot silently depend on caller location or unbounded child processes; and the complete existing language/runtime/game/VSIX regression surface remains green. SMILE 2.0 is ready for the next feature phase, with only separately reviewable move-only organization work intentionally left for future maintenance.
