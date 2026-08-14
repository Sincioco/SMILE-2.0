# Phase 4.1 high-resolution media hardening

Baseline: `104b6dcda19fccc09370f542834bff845195ccd3` on `main`.

## Recorded pre-fix evidence

- Web visible and back canvases were assigned the logical `960x540` dimensions directly, regardless of CSS size or DPR.
- Direct2D pushed its viewport clip before user clips; `SHOW SCREEN` popped only one clip, so an active user clip was mistaken for the viewport and `EndDraw` saw an unbalanced stack.
- The IMAGE ownership fixture's generated Web operations left one cache entry with eight reference owners after global cleanup: four observation/call/draw temporaries and two returned record temporaries were not consumed, alongside retain-then-assign duplication.
- Native `Slot/A` and `Slot?A` both sanitized to `Slot_A.bin`; the supplied fixture printed `7,8,9` for both loads.
- Web persistence used `GAME WINDOW` title as its storage namespace.
- DirectWrite's empty UTF-8 conversion returned no layout, producing height zero.
- XAudio2 active-channel diagnostics counted a source voice forever after natural completion because no voice callback/reaper existed.
- Web same-channel playback performed asynchronous decode/resume without a request generation, allowing an older request to start after a replacement.
- Native IMAGE release decremented to zero before taking the cache lock, allowing lookup/retain to race a final unlink/free; WIC COM initialization was not balanced per decoding thread.

## Hardening result

Phase 4.1 adds DPR-sized paired Web backing stores with logical transforms, shared logical clip stacks across frame invalidation, one owned Web IMAGE-expression convention, SHA-256 app/key identity with the cross-target `SMD4` envelope, canonical declared project assets, generation-safe Web SFX, callback-mark/main-thread-reap native SFX, empty-text metrics, and a double-checked locked native IMAGE cache with per-thread WIC COM balance.

Focused fixtures live in `examples\Phase4Hardening`; the normal smoke gate compiles and runs them alongside the accepted Phase 1-4 suite.
