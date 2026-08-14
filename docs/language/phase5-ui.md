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
- `Smile.UI.MenuNavigator` — reusable hierarchical bindings, stack navigation, viewport placement, leaf acceptance, and painter-ordered drawing.
- `Smile.UI.Dialogue` — measured Unicode wrapping, explicit newlines, long-word splitting, spill pagination, caller-time typewriter reveal, events, and continuation drawing.

The library uses fixed module-owned arrays and generation-safe numeric handles. Destroy/reset paths clear owned text and image-containing styles. It owns no images or sounds, never plays event audio, and contains no game-specific inventory, battle, actor, tile-map, menu-flow, or camera rules.

Dialogue line advance is exactly measured text height plus `TextStyle.LineSpacing` plus `DialogueStyle.LineSpacing`; each spacing value is applied once.

### Phase 5.1 hardening contract

`Smile.UI` 1.0.1 deeply validates loaded nine-slice and bitmap-font source regions with subtraction-style bounds, valid Unicode scalar ranges, filters, opacity, and bounded layout fields. Nested Menu and Dialogue styles validate before retained records are replaced. Unloaded optional UI images remain safe no-draw/vector fallbacks, and stale bitmap handles use the system-text fields without accessing a later font generation.

Text literals may span source lines. Public Text measurement and drawing split on Unicode newline scalar 10, preserve empty and trailing lines, measure the widest line, align each line independently, and add `TextStyle.LineSpacing` only between lines. Empty text is one positive-height line. `Opacity <= 0` suppresses both modes; positive system text is fully opaque because Phase 5.1 does not add generic text-alpha syntax, while bitmap text uses image opacity.

Menu stores requested and effective row counts separately; `VisibleRows` reports the effective value, style changes reflow it in both directions, selection stays visible, the scrollbar is bounded, and `CursorFilterMode` selects smooth or pixel filtering. Dialogue accepts at most `UI_MAX_DIALOGUE_PAGE_SCALARS` (2048) scalars per raw page and uses bounded transactional preparation. Active `SetStyle` reflows with the candidate style while preserving the active raw-page/visible state, and a failed reflow changes nothing.

`examples\MenuGallery` supplies original high-resolution PNG/WAV assets and demonstrates project/package references on DirectX, GDI, and Web. `examples\Phase5Hardening` covers the Phase 5.1 validation, reflow, multiline, bounds, and ownership matrix. `examples\Phase5UIStateTests` proves pure menu state from a Console project. `examples\InvalidPhase5\ConsoleCallsDraw` proves the single consumer-located `SML3704`.

### Phase 5.2 submenu navigation contract

`Smile.UI` 1.1.0 adds `MenuNavigator` without adding a language keyword or native menu-flow helper. A navigator references application-owned Menu handles, binds parent menu items to child menus, manages a bounded active stack, routes Right/Enter/Space to open, routes Left/Escape one level back, and records accepted leaf values. Child menus may be shared, but self-links, cycles, active duplicates, stale handles, and stale item revisions are rejected or repaired safely. Per-binding policy chooses whether opening resets or preserves the child's selection.

Menu labels accept at most 256 Unicode scalars and support ellipsis, clip, or wrap overflow. Wrap is bounded to four lines, and final-line ellipsis remains scalar-safe. Menu reserves stable cursor and right-marker gutters, draws the exact automatic marker ` >` for bound rows, and exposes bounds, selected-row geometry, position, selection reset, revision, and focus-aware drawing to the reusable navigator.

Navigator layout clamps the root to a padded viewport, aligns each child with its parent row, tries the configured right/left direction and fallback, clamps vertically, and uses bounded overlap when neither side fits. Relayout and style updates are transactional. `DrawStack` uses deterministic root-to-leaf painter order with only the active menu focused. All navigation, state, and geometry APIs remain Console-safe; only drawing APIs require `GAME WINDOW`.

`examples\Phase5SubmenuStateTests` proves project/package state parity and the capacity, cycle, revision, stale-handle, placement, and transactional matrix. `examples\InvalidPhase5Submenus\ConsoleDrawStack` proves the consumer-located `SML3704`. `examples\Phase5SubmenuViewport` is the focused DirectX/GDI/Web rendering proof. `examples\MenuGallery` is migrated to a four-level reusable navigator with a shared child, a disabled submenu, long overflow labels, automatic markers, leaf acceptance, and application-owned event audio.
