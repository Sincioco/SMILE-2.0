# H01-H03 compiler record-array hardening — closure and handoff

Status: **CLOSED for the tested H01-H03 contracts**.
Verified UTC: `2026-09-07T14:29:42.8492895+00:00`.
Historical baseline / preserved H01 commit: `dab573a1ecfbd54e7f750628bc76b96b0cb92a0f`.

## Scope and precedence

This record supersedes the H01-H03 execution instructions in the earlier September 7
hardening ZIPs and interrupted Codex status files. Do not recreate the removed H01
assembly-rewriting fixture, roll back H01, restart the Viewer refactor, or repeat the
original investigation simply because an old plan says it is pending. Existing tests
remain available; reopen a finding only on a current failing regression, a new symptom,
or an actual source/contract change that invalidates this evidence. Record that evidence.
This is not permission to hide regressions or skip checks for newly changed code.

## Dispositions

- H01: already repaired, verified on the user's Windows machine, and committed in
  `dab573a`. The H23 hotfix does not modify `MasmEmitter.cs` or the existing test entry point.
- H02: indexed Web ByRef now retains the SMILE value-record location, not an obsolete
  JavaScript array object. Indices are captured once. Class roots retain original identity.
- H03: each evaluated Web index is checked against its bound dimension before a later
  index runs. This aligns the tested invalid-index behavior with native. The existing Web
  test runner also checks expected console output on failure instead of only an error substring.

## Test-fixture correction (Doctor 1.0.1)

The original Doctor 1.0.0 supplied 12 `As New Box` initializers across 11 test
source files. These were corrected to `As New Box()` before this successful
verification. This was a test-input syntax defect, not a new SMILE language feature
or another H01 repair. Expected outputs, bounds checks and ownership assertions
were not weakened. For resumed 1.0.0 transactions, the original failed report and pre-correction
fixture bytes are preserved in the local Doctor transaction backups.

## Owners, changes and regressions

`src/Smile.Compiler/WebEmitter.cs` owns location capture and ordered index emission.
`src/Smile.Language` remains the unmodified authority for bound dimensions and types.
`scripts/run-web-test.js` retains the browser-API test double and exact failure-output assertion.
The focused tests live under `examples/RecordArrayLocationParity` and use this command:

```powershell
node scripts/test-record-array-location-parity.js --repo "D:\SMILE 2.0"
```

Run after the normal compiler build when these paths change. Tests use disposable outputs;
they do not touch live calibration or canonical assets. They cover root/nested/forwarded/With
locations, single evaluation, original Class identity, value copying, read/write/ByRef bounds,
nested/standalone/Class errors, and returned-Image ownership on failure.

## Evidence

The paired `record-array-h01-h03-evidence.json` records tested source and artifact SHA-256
identities and actual successful checks. It deliberately does not invent a self-referential
commit hash; the Git commit containing this file identifies the delivered changes.

- PASS: 25 utility self-checks
- PASS: Isolated compiler build
- PASS: Revision 2 utility self-checks
- PASS: Revision 2 fixture correction: 12 constructor calls in 11 owned test inputs
- PASS: 15 compiled native/generated-Web cases: locations, Class identity, copies, exact failure traces and resource cleanup
- PASS: 95 utility self-checks (Doctor 1.0.1)
- PASS: Normal repository compiler/runtime/solution/VSIX build
- PASS: Existing shared compiler/language test suite (exit 0)
- PASS: Existing fixed-array ownership/bounds/package-independent regression harness
- PASS: Existing native Text runtime tests
- PASS: Character Viewer native Release and Full Web publication (build checks, not a new visual acceptance)
- PASS: 125 utility self-checks (Doctor 1.0.1)
- PASS: Installed Chrome: RootReplacement terminal=stopped exact output/error/ownership
- PASS: Installed Chrome: ClassIdentity terminal=stopped exact output/error/ownership
- PASS: Installed Chrome: FieldByRef terminal=error exact output/error/ownership
- PASS: Chrome verifier: normal stopped/completed terminal states accepted only with exact output, empty error and zero ownership; expected-error checks preserved
- PASS: VSIX 2.0.60 identity/DLL plus installed compiler, language and native-runtime hashes match validated build

VSIX installed and payload hash verified: **True**.
Native `MasmEmitter.cs` bytes and existing `src/Smile.Tests/Program.cs` were preserved.
No lost calibration, new visual regression, or live Viewer crash was claimed by this work.
Installed-Chrome checks, when listed above, are headless language fixtures, not a new
interactive Viewer acceptance. Previously accepted Viewer results retain their own scope.

## Stop boundary

H01-H03 are no longer pending implementation. Do not restart them from old handoffs.
Double, fractional renderer changes, loader/splash enhancements, semantic-inspection CLI,
Battle Scene Editor and subject-first syntax are still separate, deferred work.

## Source anchors

- `dab573a`: existing H01 commit and the reviewed pre-hotfix Web emitter.
- `src/Smile.Compiler/WebEmitter.cs`: `IndexedReference`, `ClassOwnedReference`, checked index emission.
- `scripts/run-web-test.js`: expected-error console trace assertion.
- `scripts/verify-vsix-install.ps1`: normal installed-extension identity/hash verification.
