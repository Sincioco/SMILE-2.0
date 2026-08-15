# Phase 5 Unicode text and reusable UI

Phase 5 keeps one shared language model for the compiler and Visual Studio extension while adding the minimum general-purpose surface needed by reusable SMILE-authored UI.

## Unicode scalar inspection

```smile
Print Text_Length("A😀B")
Print Text_Code_At("A😀B", 1)
Print Text_Slice("A😀B", 1, 1)
```

These print `3`, `128512`, and `😀`. A combining mark is a separate scalar. Native code uses bounded UTF-8 decoding; Web code uses code-point-aware iteration. All three built-ins consume normal owned `Text` expressions, so variables, record fields, array elements, parameters, function returns, and nested calls follow the existing ownership model.

## Routine capability

Every routine has a derived `requiresGameWindow` flag. Direct drawing, clipping, screen text measurement, and other game-window operations mark the containing routine. A fixpoint propagates that flag through calls, including recursion. Library analysis itself remains valid; a top-level Console call to a flagged routine receives one `SML3704` at that consumer call site. Game consumers compile normally.

Format-version 5 packages serialize the flag on every public routine. Project and package references therefore enforce the same boundary, and imported completion marks routines that require `Game Window`.

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

Navigator layout clamps the root to a padded viewport, aligns each child with its parent row, tries the configured right/left direction and fallback, clamps vertically, and uses bounded overlap when neither side fits. Relayout and style updates are transactional. `DrawStack` uses deterministic root-to-leaf painter order; Phase 5.2.1 draws a cursor for every visible menu while routing input only to the deepest level. All navigation, state, and geometry APIs remain Console-safe; only drawing APIs require `Game Window`.

`examples\Phase5SubmenuStateTests` proves project/package state parity and the capacity, cycle, revision, stale-handle, placement, and transactional matrix. `examples\InvalidPhase5Submenus\ConsoleDrawStack` proves the consumer-located `SML3704`. `examples\Phase5SubmenuViewport` is the focused DirectX/GDI/Web rendering proof. `examples\MenuGallery` is migrated to a four-level reusable navigator with a shared child, a disabled submenu, long overflow labels, automatic markers, leaf acceptance, and application-owned event audio.

### Phase 5.2.1 hierarchy and presentation hardening

`Smile.UI` 1.1.1 requires every active child edge to remain attached to the exact currently selected parent item that opened it. Navigator repair prunes from the first mismatched edge, clears all deeper stack entries and stale accepted-leaf state, and leaves the changed parent current. Label, user-value, style, position, and viewport changes preserve descendants when the selected edge is unchanged.

`MenuNavigator.DrawStack` requests cursor-visible drawing for every visible menu, while keyboard input remains exclusive to the deepest level. `Menu.DrawFocused(False)` remains the explicit lower-level cursor suppression API. The fixed left gutter keeps every label X stable and the cursor clamped left of text.

`MenuStyle.ShowScrollbar` reserves a stable gutter whenever true, but draws a track and proportional thumb only when `ItemCount > VisibleRows`; false draws nothing and reclaims the gutter. Thumb size follows `VisibleRows / ItemCount`, thumb position follows `TopIndex / MaxTop`, and track/thumb draw after row content inside the menu clip.

`ShowSubmenuIndicator` controls marker visibility without changing navigation. `SubmenuIndicatorPosition` accepts `UI_SUBMENU_INDICATOR_AFTER_TEXT` or `UI_SUBMENU_INDICATOR_RIGHT_ALIGNED`. Both use the row text style and exact literal ` >`; after-text fitting keeps the marker on the final visible line, while right alignment reserves a region before the optional scrollbar gutter. The existing 256-scalar label and four-line wrap bounds remain unchanged.

### Phase 5.2.2 bound acceptance and fixed-row alignment

`Smile.UI` 1.1.2 treats the current navigator's repaired binding table as the authority before Enter or Space can accept a leaf. A selected item with a valid binding remains a submenu item when opening cannot proceed because the stack is already at `UI_MAX_MENU_DEPTH`, the child is active, reset selection fails, layout cannot commit, or another safe precondition fails. Right, Enter, and Space then return `UI_EVENT_NONE` without changing stack or accepted-leaf state. A marker retained for another navigator does not create a binding in the current navigator, and hiding a marker does not remove its binding.

Menu prepares the exact bounded visible lines for ellipsis, clip, or wrap before drawing. For measured line height `H`, nonnegative line spacing `S`, and visible line count `N`, the text-block height is `N * H + (N - 1) * S`; the block begins at `RowY + Max(0, (RowDrawHeight - TextBlockHeight) / 2)`. Every continuation uses the fixed label X. A right-aligned ` >` uses the first visible line's Y, while an after-text marker uses its target final visible line's Y.

The cursor begins at `RowY + Max(0, (RowDrawHeight - CursorHeight) / 2)`, applies `CursorOffsetY`, and is then clamped or clipped to the visible row. This keeps the gutter and label X fixed across selection, focus, enabled state, marker visibility, scrollbar visibility, and the ancestor cursors drawn by `MenuNavigator.DrawStack`. Row height remains application-selected and fixed; Phase 5.2.2 adds no automatic height, baseline API, native menu helper, input modality, or Phase 6 feature.

### Smile.UI 1.1.3 identifier presentation

`Smile.UI` 1.1.3 presents the public inset fields as `Left`, `Top`, `Right`, and `Bottom`. The `Left` and `Right` casing update follows the SMILE 2.0 Visual Basic-style identifier convention in source, completion, Quick Info, definitions, project references, and package references. Name binding remains case-insensitive, and the `.smilelib` package format remains version 5.
