# Smile.UI 2.0.0 public API

Smile.UI 2.0 replaces the public numeric Menu, MenuNavigator, and Dialogue handle routines with reference-Class facades. Their proven generation-safe, fixed-capacity engines remain private. Applications own the facade references, call idempotent `Destroy()` to release the bounded engine slot, and let SMILE ARC release the facade itself.

`Rect`, `Insets`, `BitmapFontStyle`, `TextStyle`, `WindowStyle`, `MenuStyle`, `MenuNavigatorStyle`, and `DialogueStyle` remain value Types. `Core.Insets` fields are `Left`, `Top`, `Right`, and `Bottom`; name binding remains case-insensitive.

## Smile.UI.Window

```text
IsStyleValid(ByRef WindowStyle) As Boolean
ContentRect(ByRef WindowStyle, X, Y, Width, Height) As Core.Rect
Draw(ByRef WindowStyle, X, Y, Width, Height)
```

Skin source rectangles, nine-slice borders, filter values, opacity, destination borders, and bounded padding/vector fields are validated before drawing. `Opacity = 0` means the compatibility default of 100 percent, `1..100` is exact skin opacity, and values outside `0..100` are invalid.

## Smile.UI.BitmapFont

BitmapFont retains its established generation-safe numeric handle surface in this milestone:

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

Newline scalar 10 splits lines in both text modes. Empty interior lines and a trailing newline are preserved. Width is the widest line. Height is `line count * line height + (line count - 1) * TextStyle.LineSpacing`.

## Smile.UI.Menu

Both stateful classes are exported by the spanning `Smile.UI.Menu` module:

```smile
Import Smile.UI.Menu As Menus

Dim Root As New Menus.Menu(
    Style,
    X:=200,
    Y:=250,
    Width:=600,
    Height:=200
)

Dim Navigator As New Menus.MenuNavigator(Root, NavigatorStyle)
```

### Menu

```text
Sub New(ByRef MenuStyle, X, Y, Width, Height, Optional VisibleRows = 5)
Valid As Boolean { Get }
Destroy()
ClearItems()
SetStyle(ByRef MenuStyle) As Boolean
AddItem(Label, UserValue, Optional Enabled = True) As Number
SetItemLabel(Index, Label) As Boolean
SetItemEnabled(Index, Enabled) As Boolean
SetItemValue(Index, UserValue) As Boolean
SetItemHasSubmenu(Index, HasSubmenu) As Boolean
ItemHasSubmenu(Index) As Boolean
ItemRevision As Number { Get }
ItemCount As Number { Get }
SelectedIndex As Number { Get; Set }
SelectedValue As Number { Get }
TopIndex As Number { Get }
VisibleRows As Number { Get }
Bounds As Core.Rect { Get }
SelectedRowRect As Core.Rect { Get }
SetPosition(X, Y) As Boolean
ResetSelection() As Boolean
Update(Key) As Number
DrawFocused(Focused)
Draw()
```

Constructor failure leaves a valid Class reference whose `Valid` property is `False`. `Destroy()` is idempotent and sets the facade to an invalid state. The setter for `SelectedIndex` safely ignores an invalid candidate; read it back when an application needs confirmation.

Labels are bounded and Unicode-scalar safe. Fixed cursor gutters, wrapped/ellipsized layout, centered text blocks, submenu indicators, proportional scrollbars, item revisions, disabled selection, and requested-versus-effective row behavior retain the 1.x hardened semantics.

### MenuNavigator

```text
Sub New(RootMenu As Menu, ByRef MenuNavigatorStyle)
Valid As Boolean { Get }
Destroy()
SetStyle(ByRef MenuNavigatorStyle) As Boolean
Relayout() As Boolean
Reset() As Boolean
BindSubmenu(ParentMenu, ParentItemIndex, ChildMenu, Optional ResetChildSelection = True) As Boolean
UnbindSubmenu(ParentMenu, ParentItemIndex) As Boolean
ClearBindings()
HasSubmenu(ParentMenu, ParentItemIndex) As Boolean
OpenSelected() As Number
Back() As Number
Update(Key) As Number
Depth As Number { Get }
CanGoBack As Boolean { Get }
LastAcceptedIndex As Number { Get }
LastAcceptedValue As Number { Get }
DrawActive()
Draw()
```

The navigator owns bindings and stack state, not Menu objects. A private bounded facade registry translates Menu object identity to generation-safe engine handles, so no raw handle is part of the public API. The navigator rejects stale references and cycles, repairs active edges, lays out menus transactionally, routes input to the deepest menu, and draws root-to-leaf. Raw-handle query members from 1.x (`RootMenu`, `CurrentMenu`, `MenuAtDepth`, `ParentMenu`, and `LastAcceptedMenu`) are intentionally removed; observable state is available through depth, back, event, and accepted-leaf properties.

## Smile.UI.Dialogue

```text
Sub New(ByRef DialogueStyle, X, Y, Width, Height)
Valid As Boolean { Get }
Destroy()
ClearPages()
SetStyle(ByRef DialogueStyle) As Boolean
AddPage(Text) As Boolean
Start(NowMilliseconds) As Boolean
Update(NowMilliseconds) As Number
HandleKey(Key, NowMilliseconds) As Number
Draw(NowMilliseconds)
Active As Boolean { Get }
Complete As Boolean { Get }
PageCount As Number { Get }
CurrentPage As Number { Get }
VisibleCharacters As Number { Get }
```

Dialogue preserves bounded Unicode wrapping, spill pagination, caller-time typewriter reveal, active-theme reflow, and transactional page preparation. Construction failure yields `Valid = False`; `Destroy()` is idempotent.

## Capability boundary

`Menu.Draw`, `Menu.DrawFocused`, `MenuNavigator.Draw`, `MenuNavigator.DrawActive`, `Dialogue.Draw`, and measurement-dependent Dialogue operations retain `requiresGameWindow` in format-6 metadata. State, binding, selection, geometry, and update operations remain usable by Console consumers. Project and package references enforce the same consumer-located `SML3704` diagnostics.

## Smile.UI.Controls

Shared immediate-mode controls used by Character Viewer and Fire Lab; applications still own layout and actions.

- `DrawPanel(X, Y, Width, Height, Optional Opacity = 80)`: translucent dark panel.
- `DrawButton(X, Y, Width, Height, Label, Selected)`: fitted centered white text, dark normal fill and cyan selected fill.
- `DrawSlider(X, Y, Width, Height, Value, MinimumValue, MaximumValue, Optional KnobWidth = 4)`: dark track, cyan progress, white thumb.
- `Contains(X, Y, Width, Height)`, `Clicked(X, Y, Width, Height)`: logical-canvas hit testing and primary press.
- `UpdateSlider(ByRef DragOwner, Id, X, Y, Width, Height, MinimumValue, MaximumValue, WheelStep, ByRef Value) As Boolean`: press-only drag start, clamp across the entire drag even outside the track/window, release handling, and hover-wheel adjustment. Return True means consume the input frame; do not pan/orbit the scene in that frame. Share one DragOwner number among a panel's controls, use unique positive IDs, and clear it on scene reset. A press that started elsewhere cannot be stolen merely by entering the slider. Camera and calibration actions remain application-specific.

No new language syntax, native widget framework, or per-frame object allocation was introduced.
