# Smile.UI 1.0.1 public API

All handles are generation-safe `NUMBER` values. Handle `0` is invalid.

## Smile.UI.Window

```text
IsStyleValid(BYREF WindowStyle) AS BOOLEAN
ContentRect(BYREF WindowStyle, X, Y, Width, Height) AS Core.Rect
Draw(BYREF WindowStyle, X, Y, Width, Height)
```

Skin source rectangles, nine-slice borders, filter values, opacity, destination borders, and bounded padding/vector fields are validated before drawing. `Opacity = 0` means the compatibility default of 100 percent, `1..100` is exact skin opacity, and values outside `0..100` are invalid. The vector fallback is binary visible because generic rectangle primitives do not expose alpha.

## Smile.UI.BitmapFont

```text
IsStyleValid(BYREF BitmapFontStyle) AS BOOLEAN
Create(BYREF BitmapFontStyle) AS NUMBER
Destroy(Handle)
IsValid(Handle) AS BOOLEAN
MeasureWidth(Handle, TEXT) AS NUMBER
MeasureHeight(Handle, TEXT) AS NUMBER
Draw(Handle, TEXT, X, Y, Alignment, Opacity)
```

## Smile.UI.Text

```text
IsStyleValid(BYREF TextStyle) AS BOOLEAN
MeasureWidth(BYREF TextStyle, TEXT) AS NUMBER
MeasureHeight(BYREF TextStyle, TEXT) AS NUMBER
Draw(BYREF TextStyle, TEXT, X, Y, Alignment, Opacity)
```

Newline scalar 10 splits lines in both modes. Empty values are one line; empty interior lines and a trailing newline are preserved. Width is the widest line. Height is `line count * line height + (line count - 1) * TextStyle.LineSpacing`. Alignment is applied per line and unknown alignment values normalize to left. `Opacity <= 0` draws nothing; positive opacity draws system text fully opaque, while bitmap text uses clamped `1..100` image opacity. A stale bitmap-font handle safely uses system text measurement/drawing with the style's system size, while `IsStyleValid` returns `FALSE` for that candidate style.

## Smile.UI.Menu

```text
IsStyleValid(BYREF MenuStyle) AS BOOLEAN
Create(BYREF MenuStyle, X, Y, Width, Height, VisibleRows) AS NUMBER
Destroy(Handle)
IsValid(Handle) AS BOOLEAN
SetStyle(Handle, BYREF MenuStyle) AS BOOLEAN
ClearItems(Handle)
AddItem(Handle, Label, UserValue, Enabled) AS NUMBER
SetItemLabel(Handle, Index, Label) AS BOOLEAN
SetItemEnabled(Handle, Index, Enabled) AS BOOLEAN
SetItemValue(Handle, Index, UserValue) AS BOOLEAN
ItemCount(Handle) AS NUMBER
SelectedIndex(Handle) AS NUMBER
SelectedValue(Handle) AS NUMBER
TopIndex(Handle) AS NUMBER
VisibleRows(Handle) AS NUMBER
SetSelectedIndex(Handle, Index) AS BOOLEAN
HandleKey(Handle, Key) AS NUMBER
Draw(Handle)
```

The row count passed to `Create` is retained as the requested count. `VisibleRows` returns the current effective count after window/style constraints. Valid style changes can shrink and later re-expand the effective count while keeping selection and top index in range. `CursorFilterMode` accepts `UI_FILTER_SMOOTH` or `UI_FILTER_PIXEL`.

## Smile.UI.Dialogue

```text
IsStyleValid(BYREF DialogueStyle) AS BOOLEAN
Create(BYREF DialogueStyle, X, Y, Width, Height) AS NUMBER
Destroy(Handle)
IsValid(Handle) AS BOOLEAN
SetStyle(Handle, BYREF DialogueStyle) AS BOOLEAN
ClearPages(Handle)
AddPage(Handle, TEXT) AS BOOLEAN
Start(Handle, NowMilliseconds) AS BOOLEAN
Update(Handle, NowMilliseconds) AS NUMBER
HandleKey(Handle, Key, NowMilliseconds) AS NUMBER
Draw(Handle, NowMilliseconds)
IsActive(Handle) AS BOOLEAN
IsComplete(Handle) AS BOOLEAN
PageCount(Handle) AS NUMBER
CurrentPage(Handle) AS NUMBER
VisibleCharacters(Handle) AS NUMBER
```

Dialogue line advance is `measured text height + TextStyle.LineSpacing + DialogueStyle.LineSpacing`; each spacing value is applied once.

`UI_MAX_DIALOGUE_PAGE_SCALARS` is 2048. `AddPage` rejects a larger value immediately without changing existing pages. `Start` prepares bounded spill pages transactionally. A valid `SetStyle` on an active dialogue reflows with the candidate style while preserving active state, raw-page identity, current content, and the already-visible scalar count; failed validation or reflow leaves the previous style and state unchanged.
