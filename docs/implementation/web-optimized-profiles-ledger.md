# Web optimized deployment profiles ledger

## Status — September 6, 2026

Hardening is complete and pushed: `main`
`5ffc165bfbecc2bf61ad5b1b046e376a6500edce`, certifying implementation
`bc6f607` on the explicit Windows/Chrome baseline. This ledger records the
completed optimized-deployment and Visual Studio profile milestone that follows
that gate.

## Implemented

- Added the shared `SmileWebDeployment` vocabulary: `Web`, `Web - Optimized
  Low`, `Web - Optimized Medium`, and `Web - Optimized High`.
- Added the Web-only compiler option `--web-quality Full|Low|Medium|High`.
- Kept normal `Web` byte-for-byte full fidelity and gave every optimized tier
  a separate output folder under `bin\Debug` or `bin\Release`.
- Added lossless-preserving Web publication staging and PNG-only downsampling:
  Low is quarter scale capped at 512 pixels, Medium is half scale capped at
  1024, and High is three-quarter scale capped at 2048. Images at or below 256
  pixels and non-beneficial results remain exact. Alpha remains PNG; no JPEG,
  WebP, new package dependency, or canonical/native asset rewrite is involved.
- Mapped logical image dimensions and crop coordinates to optimized physical
  pixels while Renderer3D texture dimensions and mip generation use physical
  dimensions. `smile-web-quality.json` records logical/physical sizes, savings,
  and before/after image hashes.
- Extended the Character Viewer, Fire VFX Lab, and Lightning VFX Lab build
  wrappers to build all three optimized profiles without changing their normal
  Web or native outputs.
- Fixed the focused Renderer3D PBR harness's obsolete generated-JavaScript end
  marker instead of weakening the actual renderer check.
- Exposed exactly five SMILE deployment platforms in the Visual Studio project
  system, including the native default. Configuration display identities now
  include platform identity so Visual Studio no longer collapses them to
  `Default`.
- Migrated all 22 tracked solution files (20 `.slnx`, 2 `.sln`) to make
  `Windows 64-bit .exe` the first/default SMILE platform. Application solutions
  expose all five deployment profiles. The root compiler/developer solution
  retains its required .NET/native host platforms while adding native-first
  SMILE mappings.
- Added a bounded new-single-project selection hook so a newly created SMILE
  solution activates `Windows 64-bit .exe` after Visual Studio finishes
  materializing its configurations.

## Validation performed

- Compiler-only Release build: PASS, zero warnings and zero errors.
- Shared language/compiler/project/timing tests: 302 PASS, including quality
  parsing, platform/output identity, PNG alpha/size/logical metadata, and exact
  normal-Web/model preservation.
- Repository build: PASS. The only package warning is the existing Visual C++
  restore warning `NU1503`.
- Optimized image crop/flip/reference VM check: PASS after using an appropriate
  bounded comparison for generated floating-point canvas coordinates.
- Focused Renderer3D PBR check: PASS after correcting the stale harness marker.
- Character Viewer outputs: Full 97.82 MiB, Low 15.32 MiB, Medium 35.90 MiB,
  High 67.65 MiB.
- Fire VFX Lab outputs: Full 10.49 MiB, Low 3.24 MiB, Medium 4.88 MiB, High
  7.55 MiB.
- Lightning VFX Lab outputs: Full 9.92 MiB, Low 3.31 MiB, Medium 4.78 MiB,
  High 7.21 MiB.
- Full/native/canonical hashes were preserved across all three tool builds.
- Solution metadata audit: PASS for all 22 tracked solutions, with no `Default`
  or unresolved project platform and native listed first.
- Representative old solution: `games\Dragonfall\Dragonfall.sln` visibly opened
  in Visual Studio with `Windows 64-bit .exe` active and all five deployment
  profiles listed.
- Representative middle solution: `games\Snake\Snake.slnx` metadata PASS and a
  focused native compile produced
  `artifacts\temp\solution-profile-check\Snake.exe`.
- Representative new solution: a disposable fresh project copied from the
  installed SMILE Game VSIX template compiled to
  `artifacts\temp\solution-profile-check\FreshSmileProfileCheck\FreshSmileProfileCheck\bin\Debug\FreshSmileProfileCheck.exe`.
  Visual Studio 18 returned `E_NOTIMPL` for both external DTE template/file-add
  calls, so that disposable automation route could not perform a second UI
  creation check; it did not alter repository source.
- VSIX build/install verification: PASS, version `2.0.59`; installed assembly
  SHA-256
  `4539527D030EAEECED190C356E1FB7204E82178410E512067B334A951A8A6532`.
- Visible optimized-Web smoke check: PASS in one reused Chromium tab for the
  Low-profile Character Viewer, Fire VFX Lab, and Lightning VFX Lab. All three
  loaded and rendered their representative 3D/VFX scene. The current control
  session did not expose installed Chrome, so this was not a new Chrome claim;
  the hardening gate retains the separate real-Chrome baseline.
- Calibration pre-commit export and validation: PASS. Arin v5.7 has 24 keys,
  JSON SHA-256
  `C05C87BF0A92B373DB7ECD1CB304F4446B851E7AFEA836E8BB05D058B1B20F0B`;
  Orin v1.3 has 0 keys, JSON SHA-256
  `13AE135FDA40302CB5A4B0146D7103A2ED5346AAEEBB3852AF6DD3C397F5D293`.

## Milestone completion

The implementation, focused builds, representative Visual Studio/solution
checks, optimized-Web visual checks, VSIX installation, and calibration export
are complete. The public commit and push identify the exact source checkpoint.

After this milestone: rewrite the public README, then implement the shared
mandatory one-second native/Web startup branding and the meaningful overall and
current-item Web loader progress. Only after every approved task is complete:
restore the native Viewer for the stream, end YouTube Live, and take Meld
offline.

Exactly one agent. Do not start Battle Scene Editor E0–E12 or `Double`. Preserve
current Arin/Orin calibration, newer work, and historical evidence; do not
reset, rebase, amend pushed commits, or force-push.
