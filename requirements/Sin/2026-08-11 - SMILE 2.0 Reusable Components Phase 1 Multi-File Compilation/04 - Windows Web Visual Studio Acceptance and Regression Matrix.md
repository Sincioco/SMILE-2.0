# Phase 1 Acceptance and Regression Matrix

## Validation philosophy

Assume the happy path. Use focused tests first, then one normal smoke run after the implementation is stable.

The goal is confidence in changed behavior, not maximal test volume.

A phase is not complete merely because the C# solution compiles. The same source set must build through Windows, Web, and Visual Studio project paths.

---

# A. Shared language analysis

| ID | Requirement | Evidence |
|---|---|---|
| A1 | Existing one-source `SmileLanguage.Analyze(text, path)` still works | Existing tests remain green plus one explicit compatibility test |
| A2 | Multi-source API accepts exactly one startup source | Focused valid and invalid tests |
| A3 | Each file has a distinct syntax tree/source path | Test tree count and paths |
| A4 | Startup calls support routine | `MultiFileBasics` analysis/build |
| A5 | Support routine calls another support routine/function | Focused test and sample |
| A6 | Support constant/array visible across files | Focused test and sample |
| A7 | Support routine can read a startup global | Focused test and sample |
| A8 | Cross-file forward reference works | Focused test |
| A9 | Case-insensitive duplicate across files reports later file | Focused diagnostic test |
| A10 | Support top-level executable statement is rejected in that file | Focused diagnostic test |
| A11 | Parser/semantic diagnostic line and column are local to the correct file | Focused diagnostic assertions |
| A12 | No source-text concatenation changes physical locations | Review plus location tests |

---

# B. Command-line compiler

| ID | Requirement | Evidence |
|---|---|---|
| B1 | Existing single-file native command remains valid | Compile an existing example/game |
| B2 | Repeated `--source` parses | Focused option test |
| B3 | Duplicate/missing source is rejected clearly | Focused option/compiler test |
| B4 | Multi-file native output builds | `MultiFileBasics.exe` produced |
| B5 | Multi-file Web output publishes | `index.html`, runtime, game JS, CSS produced |
| B6 | Native and Web keep existing output-option rules | Focused option tests |
| B7 | Web failure in support source names that support file | Focused compiler test |

---

# C. Native Windows backend

| ID | Requirement | Evidence |
|---|---|---|
| C1 | All compilation globals/routines emitted once | Focused emitter assertions/native execution |
| C2 | Only startup top-level statements execute | Sample behavior and support-top-level diagnostic |
| C3 | Support-file routines can call each other | Native sample execution |
| C4 | Existing runtime ABI remains unchanged unless strictly required | Diff review and legacy smoke |
| C5 | DirectX/Direct2D path still launches | One brief launch of a changed/new game |
| C6 | GDI path still compiles/launches or existing focused graphics check remains green | Existing smoke/focused path |
| C7 | Debug and Release retain current behavior | Build both where proportional |
| C8 | Native executable remains PE32+ x64 without CLR header | Existing smoke/artifact verification |

---

# D. Multi-file Windows breakpoints

| ID | Requirement | Evidence |
|---|---|---|
| D1 | Debug sites include source path plus line | Focused debug-generation test |
| D2 | Same line number in two files produces unique helpers | Focused test |
| D3 | Startup-file breakpoint still binds/hits | Brief Visual Studio F5 check when safe |
| D4 | Support-routine breakpoint binds/hits the support `.smile` file | Brief Visual Studio F5 check when safe |
| D5 | Release does not emit unnecessary debug helpers | Focused output/build check |
| D6 | Real filenames are used, not temporary fake `.smile` files | PDB/debug-source review and live check |

A live D3/D4 check is strongly preferred and should be performed by Codex in an isolated/safe Visual Studio instance. If impossible without disrupting unrelated unsaved work, report only those exact checks in the bold final manual-testing section.

---

# E. Web backend

| ID | Requirement | Evidence |
|---|---|---|
| E1 | Support globals/routines emitted into one game JS | Focused emitter check |
| E2 | Only startup top-level code enters `smileMain` | Focused emitter check |
| E3 | Browser output has valid JavaScript syntax | Existing syntax checker/Node syntax check if available |
| E4 | `MultiFileBasics` launches without console errors | Brief browser launch |
| E5 | Existing Web game publication still works | Normal smoke suite or one representative game |
| E6 | No Web-specific SMILE parser/semantic model added | Review |
| E7 | Browser `.smile` breakpoints remain out of scope | No regression claim required |

---

# F. `.smileproj` source selection

| ID | Requirement | Evidence |
|---|---|---|
| F1 | `StartupOnly` defaults false | Focused parser/model test |
| F2 | `StartupOnly=true` parses case-insensitively | Focused test |
| F3 | Invalid boolean reports clear project error | Focused test |
| F4 | Selected startup always included exactly once | Focused source-set test |
| F5 | Non-selected startup-only source excluded | Focused source-set test |
| F6 | Ordinary source becomes support | Focused source-set test |
| F7 | Duplicate/missing source paths fail clearly | Focused test |
| F8 | All ten games mark alternate complete programs correctly | Repository inspection/test |
| F9 | Templates produce correct metadata | Template content test |
| F10 | Changing `StartupFile` to `Program-NoDemo.smile` requires no other edit | One project/source-set test |

---

# G. Visual Studio editor and build

| ID | Requirement | Evidence |
|---|---|---|
| G1 | Build saves all open participating sources | Focused project-system test or safe live check |
| G2 | Windows command includes all support sources | Output-pane/argument test |
| G3 | Web command includes the same support sources | Output-pane/argument test |
| G4 | Cross-file routine/global completion appears | Focused completion test and safe live check |
| G5 | Current routine locals remain correctly scoped | Existing plus focused completion test |
| G6 | Diagnostic squiggle is shown only in owning file | Focused tagger/cache test or safe live check |
| G7 | Error List reports support-file path/line/column | Build invalid support fixture |
| G8 | File > Open > File still opens `.smile` normally | Preserve registration; safe live check if possible |
| G9 | Double-clicking `.smile` in Solution Explorer still works | Safe live check if possible |
| G10 | Tools > Build SMILE File remains native single-file behavior | Existing test/live check |
| G11 | Native F5 and repeated F5 remain reliable | Brief safe check or existing regression coverage |
| G12 | Web F5/Ctrl+F5 republishes and launches | Brief safe check |

Do not broadly rewrite working editor-opening/content-type/debugger code merely to make these checks easier.

---

# H. Legacy proof suite

At minimum, the final normal smoke suite must continue to prove:

- all ten games compile on the native path;
- all normal/no-demo teaching variants expected by the repository compile;
- Web publication of the existing games/variants remains green according to the current smoke contract;
- assets/maps copy and validate;
- VSIX packages;
- native artifacts remain native x64;
- existing focused language tests remain green.

The ten games do not need full playthroughs for this compiler/project milestone.

Perform only one or a very small number of representative brief launches unless a known defect requires more.

---

# I. MultiFileBasics visible behavior

The companion sample should display a simple animated marker/panel and allow left/right movement.

Expected ownership:

```text
Program.smile
- Game Window
- startup global FrameCount
- input/main loop
- calls support routines

GameState.smile
- constants
- shared numeric array
- reset/update/query routines

Drawing.smile
- draws the scene
- calls GameState functions
- reads FrameCount declared by Program.smile
```

The exact colors/layout may be adjusted, but the cross-file dependency coverage must remain.

---

# J. Required commands/evidence in final report

Report exact commands actually used. Expected categories include:

```text
scripts\build.cmd

dotnet run --project src\Smile.Tests\Smile.Tests.csproj -c Release --no-build

artifacts\compiler\smilec.exe examples\MultiFileBasics\Program.smile \
  --source examples\MultiFileBasics\GameState.smile \
  --source examples\MultiFileBasics\Drawing.smile \
  -o artifacts\games\MultiFileBasics\MultiFileBasics.exe \
  --debug

artifacts\compiler\smilec.exe examples\MultiFileBasics\Program.smile \
  --source examples\MultiFileBasics\GameState.smile \
  --source examples\MultiFileBasics\Drawing.smile \
  --target web \
  --output-dir artifacts\web\MultiFileBasics

scripts\smoke-test.cmd

git diff --check
```

Adjust paths/flags to the final implementation and current repository conventions.

---

# K. Definition of done

Phase 1 is done only when all are true:

1. The repository has a real source-aware multi-file analysis model.
2. One startup plus support files compile as one program.
3. Windows and Web output work from the same files.
4. Project source selection handles alternate normal/no-demo startup files.
5. Cross-file diagnostics and completion work.
6. Windows debug mapping supports support-file breakpoints without breaking startup breakpoints.
7. `MultiFileBasics` proves the feature.
8. The ten legacy games and normal/no-demo source contracts remain green.
9. Documentation and templates reflect the new model.
10. Tests are proportional and green.
11. The milestone is committed and pushed.
12. The final report contains the bold manual-testing section.

If any required automated test is red, do not commit a claimed-complete milestone.
