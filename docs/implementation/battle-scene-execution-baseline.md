# Battle Scene Prerequisite Execution Baseline

Date: 5 September 2026  
Package: `2026-09-05-01-smile-2.0-pre-battle-editor-hardening.zip`  
Reviewed reference: `651492993a8709dd4e9489e57faca8888c89539d`  
Execution start: `e02403dc3fda301cec236408e9326eb946c25d0b`

## Repository state

- Branch: `main`
- Local HEAD and `origin/main`: `e02403dc3fda301cec236408e9326eb946c25d0b`
- Ahead/behind at execution start: `0/0`
- Worktree at execution start: clean
- The reviewed SHA is a comparison reference only. No reset, rebase, or history rewrite was performed.
- Newer commits were retained, including Dragon inspection/head aim, grounded hero KO presentation, refined Dragon wing motion, current Orin source work, and Final Boss reference art.

The handoff was extracted only after rejecting absolute, rooted, traversal, and alternate-data-stream archive names. All 12 manifest-listed files matched their declared byte counts and SHA-256 hashes. The source ZIP SHA-256 is `a4c1bc8ef81956edec50bbe3aae2fd0d25ed112df5990a195524f3a3d5ac4ab8`.

## H0-H6 reconciliation

| Phase | Baseline disposition | Evidence or required action |
| --- | --- | --- |
| H0 | In progress | This inventory records the actual branch, source, installed tooling, and capability boundary. |
| H1 | Present; revalidated | Arin and Orin packages, profiles, storage keys, grounding records, and calibration exports exist. Exact live-to-canonical copies were backed up before work. |
| H2 | Present; focused revalidation required | Native pointer capture recovery, wheel remainder preservation, target-anchored orbit, smooth zoom, focus audio policy, and elapsed-time animation are already implemented. |
| H3 | Needs work | Shared Fire advancement still occurs inside actor-specific update paths. Orin lightning state is a module singleton and uses a fixed renderer light slot. |
| H4 | Present in newer commits; verify | Dragon tab/head aim, Arin Death, Party KO/revive, grounded deaths, front-arc formation, shield silhouette options, and diagnostics are already implemented. |
| H5 | Needs work | Shared VFX ownership and bounded local-light leasing need reusable seams. A brittle source-text gate still asserts obsolete implementation placement and an eight-clip profile. |
| H6 | Pending | Re-run focused builds/tests, execute native validation, write the readiness report, and emit the machine-readable gate. |

## Preserved character identity

| Character | Package | Calibration key | Saved keys | Canonical JSON SHA-256 | Profile fingerprint |
| --- | --- | --- | ---: | --- | --- |
| Arin v5.7 | `games/SinStarI/SourceAssets/Characters/Paladin/ArinV57` | `CharacterViewerCalibrationKeyframes` | 23 | `1747367dd5e411d8230ab5159de1309f221867c8de6745661da1396eae6db867` | `db3286e4a9dc8f3064f65c1bc36047e7af4f2ff771ebc51bf4c73dc6c16ac2ed` |
| Orin v1.3 | `games/SinStarI/SourceAssets/Characters/Tank/OrinV13` | `CharacterViewer.Orin.v1.3.CalibrationKeyframes` | 0 | `07927539bd086ff8581d112fceb648f5f89f1b12a5decb263047554dd71e7937` | `5a6f903ccc4c5ff669689cc2a176133874a6b0641ed8f33b8d4429f7a8d6ca4d` |

The ignored execution-start backups are under `artifacts/temp/codex-handoff/2026-09-05-01-pre-battle-editor-hardening/calibration-backup-start-6957ac0`. Their hashes exactly match the canonical JSON files. No historical checkpoint was imported over current data.

## Current artifact identity

- Compiler executable: `artifacts/compiler/smilec.exe`, SHA-256 `4c4ad2cc0d4827a83d7ffe209092ae54e8c3de5bb4dad59cf4d203b76729a533`
- Shared language assembly: `artifacts/compiler/Smile.Language.dll`, SHA-256 `db4b7f6aa470530a163b0d54f35da777e1f4267a88a286b71a6ed725f993a802`
- Built VSIX: `artifacts/vsix/Smile.VisualStudio.vsix`, SHA-256 `d70173280de99bfebac357431a3a789548f06b0b8b622694f491fa28deff17db`
- Installed VSIX: version `2.0.59`, instance `91f001b5`; verification passed with installed DLL SHA-256 `36f544527367ec6ef2404d14d3025ac1aef1932f7b58dcb69a07f1b4ac0d2f85`
- Existing stable Viewer executable: `tools/Character3DViewer/bin/Character3DViewer.exe`, SHA-256 `17d5efcd2439f65018311b674e84183d1e0290c9f60fb2a6bd1b16d3e191b7b5`

## Capability boundary

Supported and reusable today:

- Shared parser, semantic model, modules, records, fixed arrays, project references, diagnostics, and native/Web compilation.
- Dynamic `Load Text File` paths into bounded byte arrays, and checksummed `Save Data` envelopes.
- Character clip playback, exact seek time, socket queries, presentation offsets, independent actor handles, camera projection, cursor-anchored orbit, and elapsed-time VFX modules.
- File reveal and Viewer-local calibration copy/paste.

**Flag:** SMILE does not yet provide a general JSON parser, operating-system clipboard API, native file picker, or mesh-ray picking API. A future editor would otherwise need private parsing and host-specific workarounds. Those capabilities are deliberately classified `Deferred-By-Explicit-Scope` because this task ends after H6.

**Flag:** SMILE does not yet provide a general application transaction/undo service or multichannel battle-audio authoring surface. The current Viewer has a bounded calibration undo operation and the runtime has reusable audio focus behavior, but an editor-wide command history and battle-audio preview adapter belong to a later specification. They are not added during prerequisite hardening.

No `.battle` format, sequence player, timeline, director, scene export, curve editor, or Web editor implementation is authorized in this run.

## Baseline validation

Commands executed from the clean execution start:

```powershell
.\scripts\test-smile-formatter.ps1
.\scripts\format-smile-style.ps1 -Check -FormatLongIf
.\scripts\test-character-3d-viewer-hardening.ps1 -NativeOnly
```

Results:

- Formatter integration: 13 passed.
- Repository SMILE formatting: 378 files passed.
- Arin calibration script: 42 assertions passed using isolated storage.
- Native Viewer calibration harness: three failures: obsolete 13-socket expectation, paused seek did not immediately clear/warm-state safely, and resume produced a catch-up fire burst.

Those failures are the starting evidence for H3/H5. They are not waived and must pass before the H6 status can be `PASS-NATIVE`.
