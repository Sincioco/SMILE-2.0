# Phase 5 Unicode text and reusable UI

Phase 5 keeps one shared language model for the compiler and Visual Studio extension while adding the minimum general-purpose surface needed by reusable SMILE-authored UI.

## Unicode scalar inspection

```smile
PRINT TEXT_LENGTH("A😀B")
PRINT TEXT_CODE_AT("A😀B", 1)
PRINT TEXT_SLICE("A😀B", 1, 1)
```

These print `3`, `128512`, and `😀`. A combining mark is a separate scalar. Native code uses bounded UTF-8 decoding; Web code uses code-point-aware iteration. All three built-ins consume normal owned `TEXT` expressions, so variables, record fields, array elements, parameters, function returns, and nested calls follow the existing ownership model.

## Routine capability

Every routine has a derived `requiresGameWindow` flag. Direct drawing, clipping, screen text measurement, and other game-window operations mark the containing routine. A fixpoint propagates that flag through calls, including recursion. Library analysis itself remains valid; a top-level Console call to a flagged routine receives one `SML3704` at that consumer call site. Game consumers compile normally.

Format-version 5 packages serialize the flag on every public routine. Project and package references therefore enforce the same boundary, and imported completion marks routines that require `GAME WINDOW`.

## Smile.UI

`libraries\Smile.UI` contains ordinary SMILE modules:

- `Smile.UI.Core` — constants and style/geometry record types.
- `Smile.UI.Window` — vector fallback and high-resolution alpha nine-slice drawing.
- `Smile.UI.BitmapFont` — fixed-grid atlas measurement/drawing with smooth or pixel filtering.
- `Smile.UI.Text` — system/bitmap dispatch.
- `Smile.UI.Menu` — fixed-capacity keyboard state, disabled-item skipping, wrapping, scrolling, drawing, and events.
- `Smile.UI.Dialogue` — measured Unicode wrapping, explicit newlines, long-word splitting, spill pagination, caller-time typewriter reveal, events, and continuation drawing.

The library uses fixed module-owned arrays and generation-safe numeric handles. Destroy/reset paths clear owned text and image-containing styles. It owns no images or sounds, never plays event audio, and contains no game-specific inventory, battle, actor, tile-map, menu-flow, or camera rules.

Dialogue line advance is exactly measured text height plus `TextStyle.LineSpacing` plus `DialogueStyle.LineSpacing`; each spacing value is applied once.

`examples\MenuGallery` supplies original high-resolution PNG/WAV assets and demonstrates project/package references on DirectX, GDI, and Web. `examples\Phase5UIStateTests` proves pure menu state from a Console project. `examples\InvalidPhase5\ConsoleCallsDraw` proves the single consumer-located `SML3704`.
