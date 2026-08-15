# Smile.UI 1.1.3 public API

All handles are generation-safe `Number` values. Handle `0` is invalid.

The public `Core.Insets` fields are `Left`, `Top`, `Right`, and `Bottom`. Version 1.1.3 standardizes the presentation casing of `Left` and `Right`; SMILE name binding remains case-insensitive.

## Smile.UI.Window

```text
IsStyleValid(ByRef WindowStyle) As Boolean
ContentRect(ByRef WindowStyle, X, Y, Width, Height) As Core.Rect
Draw(ByRef WindowStyle, X, Y, Width, Height)
```

Skin source rectangles, nine-slice borders, filter values, opacity, destination borders, and bounded padding/vector fields are validated before drawing. `Opacity = 0` means the compatibility default of 100 percent, `1..100` is exact skin opacity, and values outside `0..100` are invalid. The vector fallback is binary visible because generic rectangle primitives do not expose alpha.

## Smile.UI.BitmapFont

```text
IsStyleValid(ByRef BitmapFontStyle) As Boolean
Create(ByRef BitmapFontStyle) As Number
Destroy(Handle)
IsValid(Handle) As Boolean
MeasureWidth(Handle, Text) As Number
MeasureHeight(Handle, Text) As Number
Draw(Handle, Text, X, Y, Alignment, Opacity)
```

## Smile.UI.Text

```text
IsStyleValid(ByRef TextStyle) As Boolean
MeasureWidth(ByRef TextStyle, Text) As Number
MeasureHeight(ByRef TextStyle, Text) As Number
Draw(ByRef TextStyle, Text, X, Y, Alignment, Opacity)
```

Newline scalar 10 splits lines in both modes. Empty values are one line; empty interior lines and a trailing newline are preserved. Width is the widest line. Height is `line count * line height + (line count - 1) * TextStyle.LineSpacing`. Alignment is applied per line and unknown alignment values normalize to left. `Opacity <= 0` draws nothing; positive opacity draws system text fully opaque, while bitmap text uses clamped `1..100` image opacity. A stale bitmap-font handle safely uses system text measurement/drawing with the style's system size, while `IsStyleValid` returns `False` for that candidate style.

## Smile.UI.Menu

```text
IsStyleValid(ByRef MenuStyle) As Boolean
Create(ByRef MenuStyle, X, Y, Width, Height, VisibleRows) As Number
Destroy(Handle)
IsValid(Handle) As Boolean
SetStyle(Handle, ByRef MenuStyle) As Boolean
ClearItems(Handle)
AddItem(Handle, Label, UserValue, Enabled) As Number
SetItemLabel(Handle, Index, Label) As Boolean
SetItemEnabled(Handle, Index, Enabled) As Boolean
SetItemValue(Handle, Index, UserValue) As Boolean
SetItemHasSubmenu(Handle, Index, HasSubmenu) As Boolean
ItemHasSubmenu(Handle, Index) As Boolean
ItemRevision(Handle) As Number
ItemCount(Handle) As Number
SelectedIndex(Handle) As Number
SelectedValue(Handle) As Number
TopIndex(Handle) As Number
VisibleRows(Handle) As Number
Bounds(Handle) As Core.Rect
SetPosition(Handle, X, Y) As Boolean
SelectedRowRect(Handle) As Core.Rect
SetSelectedIndex(Handle, Index) As Boolean
ResetSelection(Handle) As Boolean
HandleKey(Handle, Key) As Number
DrawFocused(Handle, Focused)
Draw(Handle)
```

The row count passed to `Create` is retained as the requested count. `VisibleRows` returns the current effective count after window/style constraints. Valid style changes can shrink and later re-expand the effective count while keeping selection and top index in range. `CursorFilterMode` accepts `UI_FILTER_SMOOTH` or `UI_FILTER_PIXEL`.

`ItemTextOverflowMode` accepts `UI_MENU_TEXT_ELLIPSIS`, `UI_MENU_TEXT_CLIP`, or `UI_MENU_TEXT_WRAP`; `ItemTextMaxLines` is bounded by `UI_MAX_MENU_ITEM_LINES` (4). Labels are bounded by `UI_MAX_MENU_ITEM_SCALARS` (256). Ellipsis and wrapping are Unicode-scalar safe. Menu prepares every visible label line before drawing. Measured line height and nonnegative `TextStyle.LineSpacing` determine the complete text-block height, which is vertically centered in the visible portion of the fixed row. Every continuation uses the same label X. A fixed cursor gutter keeps label geometry stable, and `DrawFocused(False)` suppresses the cursor while retaining normal row rendering. The cursor image is centered in the visible row, `CursorOffsetY` applies afterward, and the final destination is clamped or clipped inside that row. Item revisions invalidate stale navigator bindings after structural item changes.

`ShowScrollbar = True` reserves a stable eight-unit right gutter and draws a track/thumb only while `ItemCount > VisibleRows`; `False` reclaims the gutter. The thumb is bounded and proportional to `VisibleRows / ItemCount`, while its position is proportional to `TopIndex / (ItemCount - VisibleRows)`. `ShowSubmenuIndicator` controls presentation only. `SubmenuIndicatorPosition` must be `UI_SUBMENU_INDICATOR_AFTER_TEXT` or `UI_SUBMENU_INDICATOR_RIGHT_ALIGNED`; both draw the exact literal ` >` for valid bound items. After-text markers share the fitted final visible line's Y, and right-aligned markers share the first visible line's Y before the optional scrollbar gutter. Sharing the same row style and line origin gives the label and marker one baseline without a separate font-ascent API.

## Smile.UI.MenuNavigator

```text
IsStyleValid(ByRef MenuNavigatorStyle) As Boolean
Create(RootMenuHandle, ByRef MenuNavigatorStyle) As Number
Destroy(NavigatorHandle)
IsValid(NavigatorHandle) As Boolean
SetStyle(NavigatorHandle, ByRef MenuNavigatorStyle) As Boolean
Relayout(NavigatorHandle) As Boolean
Reset(NavigatorHandle) As Boolean
BindSubmenu(NavigatorHandle, ParentMenuHandle, ParentItemIndex, ChildMenuHandle, ResetChildSelection) As Boolean
UnbindSubmenu(NavigatorHandle, ParentMenuHandle, ParentItemIndex) As Boolean
ClearBindings(NavigatorHandle)
HasSubmenu(NavigatorHandle, ParentMenuHandle, ParentItemIndex) As Boolean
OpenSelected(NavigatorHandle) As Number
Back(NavigatorHandle) As Number
HandleKey(NavigatorHandle, Key) As Number
Depth(NavigatorHandle) As Number
RootMenu(NavigatorHandle) As Number
CurrentMenu(NavigatorHandle) As Number
MenuAtDepth(NavigatorHandle, DepthIndex) As Number
ParentMenu(NavigatorHandle) As Number
CanGoBack(NavigatorHandle) As Boolean
LastAcceptedMenu(NavigatorHandle) As Number
LastAcceptedIndex(NavigatorHandle) As Number
LastAcceptedValue(NavigatorHandle) As Number
DrawActive(NavigatorHandle)
DrawStack(NavigatorHandle)
```

The navigator owns bindings and stack state, never menu handles. It supports `UI_MAX_MENU_NAVIGATORS` (8), `UI_MAX_MENU_DEPTH` (8), and `UI_MAX_SUBMENU_BINDINGS` (128). A child menu may be shared by multiple parent items, while self-links, cycles, active duplicates, stale handles, and stale item revisions are rejected or repaired safely. Binding changes maintain the logical submenu state. Repair also requires each active parent's current selection to equal the stored opening item; the first mismatch prunes that child and all descendants and clears stale accepted-leaf state.

Right, Enter, and Space open a selected enabled submenu. A repaired binding in the current navigator is authoritative: a bound item never falls through to leaf acceptance when maximum depth, an active child, a failed child reset, layout failure, or another safe precondition prevents opening. Those failed opens return `UI_EVENT_NONE` without changing depth, current menu, or accepted-leaf state. An item marked by another navigator but unbound in the current navigator remains a normal leaf. Left and Escape close exactly one submenu level; at the root, Escape returns `UI_EVENT_CANCELLED` and Left returns `UI_EVENT_NONE`. Accepting a true leaf records its menu, index, and user value. Opening can reset or preserve the child selection as selected per binding.

`MenuNavigatorStyle` defines the viewport, nonnegative viewport padding, horizontal gap, and `AUTO`, `RIGHT`, or `LEFT` preference. Layout clamps the root, aligns children with their parent row, chooses right/left fallback, clamps vertically, and uses bounded overlap when neither horizontal side fits. Style changes and relayout are transactional. `DrawStack` paints root-to-leaf in deterministic order and requests cursor-visible drawing for every menu; keyboard input still routes only to the deepest menu.

## Smile.UI.Dialogue

```text
IsStyleValid(ByRef DialogueStyle) As Boolean
Create(ByRef DialogueStyle, X, Y, Width, Height) As Number
Destroy(Handle)
IsValid(Handle) As Boolean
SetStyle(Handle, ByRef DialogueStyle) As Boolean
ClearPages(Handle)
AddPage(Handle, Text) As Boolean
Start(Handle, NowMilliseconds) As Boolean
Update(Handle, NowMilliseconds) As Number
HandleKey(Handle, Key, NowMilliseconds) As Number
Draw(Handle, NowMilliseconds)
IsActive(Handle) As Boolean
IsComplete(Handle) As Boolean
PageCount(Handle) As Number
CurrentPage(Handle) As Number
VisibleCharacters(Handle) As Number
```

Dialogue line advance is `measured text height + TextStyle.LineSpacing + DialogueStyle.LineSpacing`; each spacing value is applied once.

`UI_MAX_DIALOGUE_PAGE_SCALARS` is 2048. `AddPage` rejects a larger value immediately without changing existing pages. `Start` prepares bounded spill pages transactionally. A valid `SetStyle` on an active dialogue reflows with the candidate style while preserving active state, raw-page identity, current content, and the already-visible scalar count; failed validation or reflow leaves the previous style and state unchanged.

`Draw`, `DrawFocused`, `DrawActive`, and `DrawStack` require a `Game Window`. State, binding, geometry, selection, and key-routing APIs remain usable by Console consumers.
