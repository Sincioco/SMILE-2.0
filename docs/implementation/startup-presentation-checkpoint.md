# Mandatory startup presentation

September 9, 2026. Baseline `b43ab33c38e3a52853c045e23bb9f593b6ac2f22`;
HEAD and origin/main matched, worktree clean at reconciliation. One agent.
Sin explicitly released this milestone. Double and H01/H02/H03 remain closed.

## Current state

Implementation, required validation, native/Full Web Viewer delivery and VSIX
installation are complete. Source commit/push is the remaining publication step.

## Owners and behavior

- `StartupBuildMetadata` reads the compiler's embedded authoritative VSIX manifest
  and captures a compilation timestamp with an explicit UTC offset. Native/Web
  generated output carries that metadata; no runtime-clock substitution.
- `MasmEmitter` automatically starts `Smile.NativeRuntime/startup` before user
  code. The independent GDI+ splash thread paints/flushes the embedded approved
  logo, then overlaps preparation. First-frame submission, console input and
  normal shutdown honor the remaining one-second visible minimum. Existing
  renderer, window placement and actor ownership remain unchanged.
- `WebOutputWriter` publishes mandatory `smile-logo.png` transactionally and
  includes `WebRuntime/Startup.js` before the runtime/program scripts. Logo decode
  and a presentation boundary precede execution; background-tab time is excluded.
  Overall preparation stays indeterminate until ready. Current-asset progress
  uses streamed response bytes and reliable uncompressed Content-Length only;
  unknown/compressed sizes and decoding have explicit labels. Existing bounded
  encoded download cache and failure recovery remain in their original owner.
- Both use the already approved 427,320-byte `smile-2.0-logo-web.png` derivative
  (SHA256 `494F2B7A8476EA58702DEF84105ADEB52D77BCE3AA209A85A5507ECF5371A374`).
  Canonical original `43D695C36FAB50849ADD26330E2D857F18C60BFBA91AF0EAA0D02127E0009AC9`
  is untouched. No character asset, calibration, SMILE syntax or library ABI change.

## Validation and delivery evidence

- `artifacts/temp/startup-managed-final.log`: all **320 managed tests** passed.
  Checks include deterministic supplied artifact metadata, authoritative version,
  mandatory logo bytes and asset-name collision rejection preserving prior output.
- `startup-contract-final.log`: focused Web visible minimum/background/slow-load,
  reliable/unknown/compressed byte progress, decode/cache/failure and native timing
  checks passed. Custom-entry console output remains exact. Both DirectX and GDI
  first-frame fixtures terminate cleanly. Earlier timing evidence measured 1,000 ms
  remaining for fast startup and 1,515 ms preparation plus only 47 ms remaining for
  slow startup; the final log records equivalent measurements.
- Formatter integration: 13 tests; repository style check: 433 tracked files.
- Normal smoke used explicit `--skip-doctor`. `startup-smoke.log` passed through
  Phase 4 media, then exposed an old harness gesture/audio scheduling assumption.
  The harness now waits for the loader's presentation boundary before simulating
  those actions. The affected audio check passed; `startup-smoke-continue.log`
  resumes there and completes all remaining suites, game builds and artifact
  verification. No production audio workaround was introduced. Later native
  first-frame/minimized-owner handling was covered by the final focused fixtures
  and real Viewer launch; the final compiler-only collision guard passed the 320
  managed tests. Valid unchanged earlier smoke evidence is reused.
- `startup-viewer-native-final.log` / `startup-viewer-web-final.log`: rebuilt native
  Viewer and Full Web publication, all canonical model cooks cache hits. Native
  launched normally through Launch.ps1, preserving its calibration watchers.
- Tool observations: native and Chrome startup logo/credits/footer/build metadata
  visible; both enter the real Viewer and Space pauses playback. Final Chrome AX
  observation showed an actual texture download at **2,162,688 / 4,157,809 bytes
  (52%)**, separately from overall preparation. This is tool evidence, not new
  human acceptance. `startup-native-viewer-ready.png` and
  `startup-web-viewer-ready.png` capture the resulting paused viewers. Slow-start
  layout captures: `startup-native-splash.png`, `startup-web-splash.png`; the native
  layout capture predates reuse of the visually equivalent approved derivative.
- `startup-delivery-build.log`: final compiler/runtime/VSIX build passed.
  `startup-vsix-install.log`: installed VSIX **2.0.61**, verified all **35 payload
  hashes** under `C:/Users/louie/AppData/Local/Microsoft/VisualStudio/18.0_91f001b5/Extensions/t1wkjdml.mqq`.
  No Visual Studio window was closed for installation.
  `startup-installed-compiler.log` confirms that the installed compiler also
  compiled and ran a native console program with exact preserved stdout.
- `startup-arin-export.log` / `startup-orin-export.log`: preserved Arin's 24 keys
  (`7A3E7BC823CF544FA0136920A9D0752BF7DE585C0891B9073E688B1F783B3F67`) and Orin's
  zero keys (`13AE135FDA40302CB5A4B0146D7103A2ED5346AAEEBB3852AF6DD3C397F5D293`).
  No canonical character asset or calibration changed. Sin Star I remains closed.

Final artifact SHA256:

| Artifact | SHA256 |
| --- | --- |
| Native Viewer | `55CDBBF5E1B179226089BE956952AC7D37C251E877BCCFFC7C0776826A7ECE75` |
| Full Web index | `35B231EC8863BF4CC06B135E49D666C14CEFAD2ABDF173891404E669A8F0D849` |
| Full Web game.js | `5549CD3761E13E638E7A276723476920B255A7E735D3973D6335058C3B119A9B` |
| Full Web runtime | `F3C8236965358E7F327B10573E31355BD57437E4BE75103D2C734D12226C1E99` |
| Installed VSIX artifact | `0491A280E62EACECA14C83280B94FA377FF5782B3C48A642E371A55FD95EDE7E` |
| Bundled native runtime | `734818A95C21775CAEA999F869713B83713107B34493BE0F351408E8B41799EA` |

The Viewer's generated gameplay JavaScript is unchanged from the previous VFX
milestone. Its validated outputs are reused after the final compiler-only
publication-collision guard, which does not change these generated programs.

## Compatibility and limits

Recompile existing executables/Web publications to adopt startup behavior.
Console stdout stays unchanged but native launches now include the required
visible minimum. Libraries are not executable programs. WebLoadingAuthor also
supplies native creator credits. Legacy WebLoadingLogo remains accepted/published
but does not override official branding. Recompilation updates the artifact stamp
and Web content identity; reloading does not change its compile time.

The clock measures presentation/visibility, not human attention; other windows,
monitor power and physical occlusion cannot be proved from these APIs. Source
and generated files remain editable. There is no branding bypass in standard
builds, and no DRM/obfuscation. No public-site upload is implied by local publication.

## Next action

Commit and push the validated milestone, record publication, then stop. No required
startup observations remain pending. No general gameplay reacceptance is claimed.

## On Hold

Further Sin Star I game updates, Battle Scene Editor, semantic-inspection CLI,
subject-first syntax, SMILE 1.0 and unrelated suspended features.
