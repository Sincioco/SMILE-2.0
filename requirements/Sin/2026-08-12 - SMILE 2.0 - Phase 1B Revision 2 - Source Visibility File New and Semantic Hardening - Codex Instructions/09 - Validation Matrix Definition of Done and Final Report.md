# Validation Matrix, Definition of Done, and Final Report

## A. Source visibility

| ID | Requirement |
|---|---|
| H1 | New command creates physical `.smile` file |
| H2 | Project XML contains exactly one entry |
| H3 | Source set contains it |
| H4 | Solution Explorer shows it immediately |
| H5 | Double-click opens the real file |
| H6 | Restarting Visual Studio preserves visibility |
| H7 | Add Existing reports duplicate only while the item is visible/included |
| H8 | Remove from Project hides item, removes XML entry, preserves file |
| H9 | Add Existing re-adds and immediately displays it |
| H10 | Root child/sibling traversal reaches every source |
| H11 | Static `SourceVisibilityBasics` shows all sources on initial open |
| H12 | No duplicate or reserved hierarchy IDs |

## B. User interface

| ID | Requirement |
|---|---|
| U1 | Project command is exactly `New SMILE 2.0 Source Code` |
| U2 | Existing command is `Add Existing SMILE 2.0 Source Code...` |
| U3 | File > New lists `SMILE 2.0 Source Code` |
| U4 | File > New works with no solution |
| U5 | Project menu omits Connected Services |
| U6 | Project menu omits New EditorConfig File |
| U7 | Edit SMILE 2.0 Project File works |
| U8 | Set as Startup remains working |

## C. Build/editor integration

| ID | Requirement |
|---|---|
| B1 | Newly created routine resolves through IntelliSense |
| B2 | Native build consumes new support source |
| B3 | Web build consumes new support source |
| B4 | diagnostics use new file path |
| B5 | File > Open remains working |
| B6 | Tools > Build SMILE File remains working |
| B7 | F10 remains working |
| B8 | Web sound remains working |

## D. Semantic hardening

| ID | Requirement |
|---|---|
| S1 | Later support CONST sizes startup DIM |
| S2 | Cross-file constant chain works |
| S3 | Reversed source order gives same result |
| S4 | Circular constants fail clearly |
| S5 | CONST/routine collision fails |
| S6 | DIM/routine collision fails |
| S7 | implicit global collision fails |
| S8 | single-file compatibility remains |

## E. Workspace/governance

| ID | Requirement |
|---|---|
| W1 | closed buffer unregisters |
| W2 | stale callback is released |
| W3 | one physical source can be registered by multiple projects |
| W4 | source mutation refreshes all affected open files |
| W5 | root AGENTS reflects Windows/Web and approved roadmap |

---

# Required live Visual Studio sequence

## Snake hierarchy round trip

1. Open `games\Snake\Snake.slnx`.
2. Right-click project.
3. Click **New SMILE 2.0 Source Code**.
4. Create `Phase1BVisible.smile`.
5. Confirm it appears immediately in Solution Explorer.
6. Confirm it opens with SMILE language services.
7. Add a function and call it temporarily from the selected startup.
8. Build/run native.
9. Build/run Web.
10. Close Visual Studio.
11. Reopen Snake.
12. Confirm `Phase1BVisible.smile` remains visible.
13. Attempt Add Existing on it; confirm duplicate message and no duplicate node/XML.
14. Remove from Project; confirm file remains.
15. Add Existing; confirm item reappears.
16. Clean all temporary test edits/files before commit.

## File > New

1. Close the solution.
2. Use File > New > File.
3. Select `SMILE 2.0 Source Code`.
4. Confirm highlighting/IntelliSense.
5. Save or close without saving.

## Regression

- Set `Program-NoDemo.smile` as startup.
- Run native and Web.
- Restore `Program.smile`.
- Confirm F10 in a support file.
- Confirm Web sound with a short representative game.

---

# Definition of Done

This milestone is not complete while any included source remains invisible.

The following statements are unacceptable:

```text
The XML is correct, so hierarchy visibility is cosmetic.
Add Existing says it is included, so Add New passed.
Restart Visual Studio to refresh it.
Edit the project file manually.
```

All required matrices must pass, changes must be committed and pushed, and no temporary test residue may remain.

---

# Final report template

```text
Phase 1B Revision 2 completion report

Commit:
Branch:
Push:

Confirmed hierarchy root cause:

Source creation:
- physical creation:
- project entry:
- source set:
- hierarchy visibility:
- restart persistence:
- remove/re-add:

File > New:
- installed template:
- no-solution result:

Context menu:
- exact labels:
- unsupported entries removed:

Semantic hardening:
- constant resolution:
- cycle diagnostics:
- namespace collisions:

Workspace lifecycle:
- buffer unregister:
- multi-project ownership:

Validation:
- focused tests:
- build:
- smoke:
- native live:
- Web live:
- VSIX:

Known limitations:
```

End with the required bold manual-testing line.
