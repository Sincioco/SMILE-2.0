# Mandatory startup presentation

`Window_Loading()` is a shared, zero-argument Boolean built-in requiring a game
window. It reopens the existing native/Web splash and returns after presentation
begins. The next `Show Screen` presents the new frame and honors any remaining
one-second visible interval. Repeated calls within the same load are idempotent;
successive loads get fresh visible timing and progress. The compiled title, author,
timestamp, version, canonical logo and seven footer links are retained.

Native keeps its independent GDI+ owner and closes each thread/event normally.
Closing a reload overlay cannot terminate an existing editor. Web resets only the
presentation/progress cycle; immutable encoded-asset caching remains intact.
The Character Viewer's existing tab selection calls the operation before teardown.
No generated program is rewritten and mandatory initial startup is unchanged.

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
  The top bar starts indeterminate, then counts ready files out of known load
  files. The existing model preparation owner registers its texture dependencies
  before sequential downloads. New dependencies may increase the total; unused
  published files are excluded and repeated paths count once. Queued, decoding
  and failed files do not advance the ready count. Current-asset progress
  uses streamed response bytes and reliable uncompressed Content-Length only;
  unknown/compressed sizes and decoding have explicit labels. Existing bounded
  encoded download cache and failure recovery remain in their original owner.
- Both use the already approved 427,320-byte `smile-2.0-logo-web.png` derivative
  (SHA256 `494F2B7A8476EA58702DEF84105ADEB52D77BCE3AA209A85A5507ECF5371A374`).
  Canonical original `43D695C36FAB50849ADD26330E2D857F18C60BFBA91AF0EAA0D02127E0009AC9`
  remains the canonical branding source.

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

## Focused checks

`scripts/test-startup.ps1` covers native visible timing, slow-load overlap, repeated
loading cycles, generated native/Web calls, metadata and Web progress/visibility.
Use the normal smoke gate for compiler/runtime integration. Recompiling a tool
adopts the standard contract without a tool-local splash implementation.
