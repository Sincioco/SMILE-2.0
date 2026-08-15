# SMILE 2.0 legacy items review

Date: August 15, 2026

Repository baseline: `58887b96bbc433de809cb7cf35af73d87329f73b` on `main`

## Purpose

This report identifies files that may no longer justify their place in the SMILE 2.0 repository. It distinguishes disposable generated output from tracked historical material and from files that only look old but remain active.

No candidate in this report was deleted during the audit except the User Guide folder that Sin explicitly approved while the audit was in progress.

## Audit method

The audit covered the complete tracked repository plus ignored and generated output. It used:

- the Git index and working tree to inventory tracked, untracked, and ignored files;
- the solution, project files, smoke suite, managed tests, artifact verifier, documentation, and tutorial manifests to trace usage;
- exact filename and path searches to find active and stale references;
- exact-content hashes to examine duplicate tracked files;
- PDF metadata, extracted text, and rendered cover pages to verify the two User Guide versions;
- current product identity checks against the seven game folders, Smile.UI 1.1.3, and VSIX 2.0.38.

The repository contained 552 tracked files before the approved User Guide deletion. There were no nonignored untracked files before this report was created.

## Already approved and deleted

### A1. `docs\User Guide`

Status: Deleted with Sin's approval during this audit.

Scope: 2 tracked PDFs, 2,787,044 bytes total, approximately 2.66 MiB.

Recovery: Both files remain available in Git history.

Deleted files:

- `2026-08-09 SMILE 2.0 Complete Guide v2.0.1.pdf`
- `2026-08-15 SMILE 2.0 Complete Guide v2.0.2.pdf`

Evidence:

- Neither PDF was referenced by any tracked source, script, project, tutorial, or Markdown file.
- Version 2.0.1 described commit `92bf27e9`, VSIX 2.0.1, and eight games.
- Version 2.0.2 retained all 70 pages of the 2.0.1 guide and appended only three update pages. Its original cover and main content still described VSIX 2.0.1 and eight games; its update described VSIX 2.0.33.
- The current repository has VSIX 2.0.38 and seven games.
- Both guides contained old all-uppercase SMILE examples and predated the current authoritative formatting convention.

## Recommended cleanup candidates

### C1. Generated and ignored local output

Recommendation: Safe to delete whenever local disk cleanup is desired.

Confidence: High.

Repository impact: None; these paths are ignored by Git and recreated by Visual Studio or the normal build and smoke scripts.

| Category | Current size | Notes |
| --- | ---: | --- |
| `artifacts` | 217.13 MiB | Compiler, runtime, VSIX, native/Web game output, logs, generated assembly, and temporary test material. |
| All `.vs` folders | 82.99 MiB | Visual Studio caches and per-solution state. Close Visual Studio before removing them. |
| All `bin` folders | 37.97 MiB | Managed, native, template, example, game, and generated library output. |
| All `obj` folders | 11.15 MiB | Restore, compiler, generated source, cache, and intermediate output. |
| Total | 349.24 MiB | Rebuild before running generated programs again. |

These are not legacy source files. They are repeatable local output and can accumulate stale versions even when the tracked repository is clean.

### C2. `docs\implementation`

Recommendation: Delete the folder if milestone history is no longer wanted inside the live repository.

Confidence: High for product/build safety; decision required for historical value.

Scope: 12 tracked Markdown files, 71,073 bytes.

Files:

- `direct2d-baseline.md`
- `direct2d-implementation-report.md`
- `mp3-mediaplayer-implementation-report.md`
- `phase4-future-3d-readiness-report.md`
- `phase4.1-hardening-report.md`
- `phase4.2-asset-publication-report.md`
- `phase5-reusable-ui-report.md`
- `phase5.1-ui-hardening-report.md`
- `phase5.2-submenu-navigation-report.md`
- `phase5.2.1-submenu-ui-hardening-report.md`
- `phase5.2.2-submenu-acceptance-row-alignment-report.md`
- `runtime-hover-and-built-in-library-color-report.md`

Evidence:

- No build, compiler, runtime, game, library, template, or automated test consumes these reports.
- The reports primarily preserve completed milestone ledgers, machine-specific paths, one-time test evidence, commit hashes, and old artifact hashes.
- Several reports describe VSIX 2.0.27 through 2.0.32, Smile.UI 1.1.0 or 1.1.1, or the former ten-game matrix.
- `direct2d-baseline.md` mentions the deleted requirements archive and an old dirty-worktree snapshot.
- Current behavior is already documented in `README.md`, `docs\architecture\README.md`, `docs\language`, and `docs\libraries\README.md`.

Required follow-up if approved:

- Update `docs\testing\direct2d-manual-test-checklist.md`. Its final instruction currently tells the tester to record results in `docs\implementation\direct2d-implementation-report.md`.

### C3. Redundant generated-test module fixtures

Recommendation: Reasonable to delete if the repository should keep automated coverage rather than static copies of the same negative scenarios.

Confidence: Medium-high.

Scope: 5 directories, 11 tracked files, 1,303 bytes.

Directories:

- `examples\InvalidModules\DuplicateModuleProvider`
- `examples\InvalidModules\MalformedSmileLibrary`
- `examples\InvalidModules\MissingLibraryReference`
- `examples\InvalidModules\ProjectReferenceCycle`
- `examples\InvalidModules\UnsafeSmileLibraryPath`

Evidence:

- Neither `scripts\smoke-test.cmd` nor `src\Smile.Tests\Program.cs` reads these directories or files by path.
- `Smile.Tests` constructs and validates duplicate providers, malformed packages, missing dependencies, project-reference cycles, and unsafe ZIP paths in temporary directories.
- The static folders therefore act as documentation/manual fixtures rather than automated inputs.

Tradeoff:

- Deleting them removes ready-made examples that a maintainer can open manually.
- `examples\InvalidModules\README.md` must be updated if these directories are removed.

### C4. Manual-only invalid library structure fixtures

Recommendation: Delete only if ready-to-run diagnostic examples are not useful.

Confidence: Medium.

Scope: 2 directories, 4 tracked files, 492 bytes.

Directories:

- `examples\InvalidModules\LibrarySourceWithoutModule`
- `examples\InvalidModules\LibraryTopLevelExecutable`

Evidence:

- No script or managed test reads these fixture files by path.
- They are small manual examples for invalid library structure.

Tradeoff:

- Unlike C3, these exact static examples are useful for manually demonstrating the corresponding compiler diagnostics.
- `examples\InvalidModules\README.md` must be updated if they are removed.

### C5. `examples\Phase2AHardening`

Recommendation: Consider deletion if the repository no longer needs a manually buildable dependency-package demonstration.

Confidence: Medium.

Scope: 8 tracked files, 2,832 bytes.

Evidence:

- The normal smoke suite does not compile this directory.
- The managed suite creates equivalent Base, Dependent, package-only consumer, mixed consumer, dependency, provider, and package-validation graphs in temporary directories.
- The only external tracked reference is one paragraph in `docs\libraries\README.md`.

Tradeoff:

- This remains the only ready-made, manually buildable Phase 2A package-dependency fixture.
- Deletion would require removing or rewriting its paragraph in `docs\libraries\README.md`.

## Items that look old but should remain

### Active phase-named examples

The Phase 3, Phase 4, Phase 5, `InvalidPhase*`, MenuGallery, multiline-expression, source-visibility, and asset-publication examples are active regression inputs. They are referenced by `scripts\smoke-test.cmd`, focused PowerShell tests, `scripts\verify-artifacts.ps1`, `scripts\run-web-test.js`, or `src\Smile.Tests\Program.cs`. Their phase-oriented names do not make them obsolete.

### Manual test checklists

`docs\testing\direct2d-manual-test-checklist.md` and `docs\testing\mp3-music-manual-test-checklist.md` are not automated inputs, but they document current hands-on hardware, graphics, focus, and audio checks. Keep them unless those manual acceptance procedures are intentionally retired.

### Phase 6 recommendation

`docs\recommendations\phase6-application-id.md` is an unimplemented future recommendation, not a completed milestone archive. Keep it until the Phase 6 application-identity decision is made.

### Snake tutorial snapshots and synchronization script

The tutorial copies under `tutorials\Snake\assets\code` are linked as downloadable snapshots from `19-complete-source.html`, so they are not orphan duplicates. `scripts\sync-snake-tutorial.ps1` has no caller, but it is the maintenance tool that updates those snapshots, source excerpts, line links, and manifest.

The tutorial currently needs synchronization rather than deletion:

- current `games\Snake\Program-NoDemo.smile` blob: `024fce74e7cad74d4430a7c95bd7cb92204b8038`;
- tutorial manifest source blob: `c1fd1fb247895778fc507b337e97b1a515408abf`.

Running the synchronization script was outside this deletion audit because it would modify the tutorial.

### Exact duplicate files

The audit found 13 exact duplicate-content groups. They are intentional test assets, shared visual proof assets copied into independently publishable projects, tiny expected-output files, project fixtures, or required per-assembly metadata. No duplicate group was identified as a safe standalone deletion.

### Core product and student material

No deletion candidate was identified in:

- `src\Smile.Language`, `src\Smile.Compiler`, or `src\Smile.NativeRuntime`;
- the Visual Studio extension source and templates;
- the native and managed test projects;
- the four official SMILE libraries;
- the seven remaining game projects;
- the active language, architecture, library, and formatting documentation;
- the Snake tutorial pages or media assets.

## Decision checklist

- [x] A1 - Delete the stale User Guide folder. Approved and completed.
- [ ] C1 - Delete generated local output and caches, approximately 349.24 MiB.
- [x] C2 - Delete the 12 historical implementation reports and redirect the Direct2D checklist. Approved and completed.
- [x] C3 - Delete five redundant generated-test fixture directories. Approved and completed.
- [x] C4 - Delete two manual-only invalid library structure fixture directories. Approved and completed.
- [x] C5 - Delete the manual Phase 2A dependency hardening fixture and update library documentation. Approved and completed.
- [x] Separate maintenance decision - synchronize the Snake tutorial to its current source. Approved and completed.

No unchecked item should be deleted until Sin explicitly approves it.
