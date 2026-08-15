# Smile.UI 1.1.1 public API

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
SetItemHasSubmenu(Handle, Index, HasSubmenu) AS BOOLEAN
ItemHasSubmenu(Handle, Index) AS BOOLEAN
ItemRevision(Handle) AS NUMBER
ItemCount(Handle) AS NUMBER
SelectedIndex(Handle) AS NUMBER
SelectedValue(Handle) AS NUMBER
TopIndex(Handle) AS NUMBER
VisibleRows(Handle) AS NUMBER
Bounds(Handle) AS Core.Rect
SetPosition(Handle, X, Y) AS BOOLEAN
SelectedRowRect(Handle) AS Core.Rect
SetSelectedIndex(Handle, Index) AS BOOLEAN
ResetSelection(Handle) AS BOOLEAN
HandleKey(Handle, Key) AS NUMBER
DrawFocused(Handle, Focused)
Draw(Handle)
```

The row count passed to `Create` is retained as the requested count. `VisibleRows` returns the current effective count after window/style constraints. Valid style changes can shrink and later re-expand the effective count while keeping selection and top index in range. `CursorFilterMode` accepts `UI_FILTER_SMOOTH` or `UI_FILTER_PIXEL`.

`ItemTextOverflowMode` accepts `UI_MENU_TEXT_ELLIPSIS`, `UI_MENU_TEXT_CLIP`, or `UI_MENU_TEXT_WRAP`; `ItemTextMaxLines` is bounded by `UI_MAX_MENU_ITEM_LINES` (4). Labels are bounded by `UI_MAX_MENU_ITEM_SCALARS` (256). Ellipsis and wrapping are Unicode-scalar safe. A fixed cursor gutter keeps label geometry stable, and `DrawFocused(FALSE)` suppresses the cursor while retaining normal row rendering. Item revisions invalidate stale navigator bindings after structural item changes.

`ShowScrollbar = TRUE` reserves a stable eight-unit right gutter and draws a track/thumb only while `ItemCount > VisibleRows`; `FALSE` reclaims the gutter. The thumb is bounded and proportional to `VisibleRows / ItemCount`, while its position is proportional to `TopIndex / (ItemCount - VisibleRows)`. `ShowSubmenuIndicator` controls presentation only. `SubmenuIndicatorPosition` must be `UI_SUBMENU_INDICATOR_AFTER_TEXT` or `UI_SUBMENU_INDICATOR_RIGHT_ALIGNED`; both draw the exact literal ` >` for valid bound items. After-text markers follow the fitted final visible line, and right-aligned markers occupy a region before the optional scrollbar gutter.

## Smile.UI.MenuNavigator

```text
IsStyleValid(BYREF MenuNavigatorStyle) AS BOOLEAN
Create(RootMenuHandle, BYREF MenuNavigatorStyle) AS NUMBER
Destroy(NavigatorHandle)
IsValid(NavigatorHandle) AS BOOLEAN
SetStyle(NavigatorHandle, BYREF MenuNavigatorStyle) AS BOOLEAN
Relayout(NavigatorHandle) AS BOOLEAN
Reset(NavigatorHandle) AS BOOLEAN
BindSubmenu(NavigatorHandle, ParentMenuHandle, ParentItemIndex, ChildMenuHandle, ResetChildSelection) AS BOOLEAN
UnbindSubmenu(NavigatorHandle, ParentMenuHandle, ParentItemIndex) AS BOOLEAN
ClearBindings(NavigatorHandle)
HasSubmenu(NavigatorHandle, ParentMenuHandle, ParentItemIndex) AS BOOLEAN
OpenSelected(NavigatorHandle) AS NUMBER
Back(NavigatorHandle) AS NUMBER
HandleKey(NavigatorHandle, Key) AS NUMBER
Depth(NavigatorHandle) AS NUMBER
RootMenu(NavigatorHandle) AS NUMBER
CurrentMenu(NavigatorHandle) AS NUMBER
MenuAtDepth(NavigatorHandle, DepthIndex) AS NUMBER
ParentMenu(NavigatorHandle) AS NUMBER
CanGoBack(NavigatorHandle) AS BOOLEAN
LastAcceptedMenu(NavigatorHandle) AS NUMBER
LastAcceptedIndex(NavigatorHandle) AS NUMBER
LastAcceptedValue(NavigatorHandle) AS NUMBER
DrawActive(NavigatorHandle)
DrawStack(NavigatorHandle)
```

The navigator owns bindings and stack state, never menu handles. It supports `UI_MAX_MENU_NAVIGATORS` (8), `UI_MAX_MENU_DEPTH` (8), and `UI_MAX_SUBMENU_BINDINGS` (128). A child menu may be shared by multiple parent items, while self-links, cycles, active duplicates, stale handles, and stale item revisions are rejected or repaired safely. Binding changes maintain the logical submenu state. Repair also requires each active parent's current selection to equal the stored opening item; the first mismatch prunes that child and all descendants and clears stale accepted-leaf state.

Right, Enter, and Space open a selected enabled submenu. Left and Escape close exactly one submenu level; at the root, Escape returns `UI_EVENT_CANCELLED` and Left returns `UI_EVENT_NONE`. Accepting a leaf records its menu, index, and user value. Opening can reset or preserve the child selection as selected per binding.

`MenuNavigatorStyle` defines the viewport, nonnegative viewport padding, horizontal gap, and `AUTO`, `RIGHT`, or `LEFT` preference. Layout clamps the root, aligns children with their parent row, chooses right/left fallback, clamps vertically, and uses bounded overlap when neither horizontal side fits. Style changes and relayout are transactional. `DrawStack` paints root-to-leaf in deterministic order and requests cursor-visible drawing for every menu; keyboard input still routes only to the deepest menu.

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

`Draw`, `DrawFocused`, `DrawActive`, and `DrawStack` require a `GAME WINDOW`. State, binding, geometry, selection, and key-routing APIs remain usable by Console consumers.
