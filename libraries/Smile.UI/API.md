# Smile.UI public API

All handles are generation-safe `NUMBER` values. Handle `0` is invalid.

## Smile.UI.Window

```text
IsStyleValid(BYREF WindowStyle) AS BOOLEAN
ContentRect(BYREF WindowStyle, X, Y, Width, Height) AS Core.Rect
Draw(BYREF WindowStyle, X, Y, Width, Height)
```

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
MeasureWidth(BYREF TextStyle, TEXT) AS NUMBER
MeasureHeight(BYREF TextStyle, TEXT) AS NUMBER
Draw(BYREF TextStyle, TEXT, X, Y, Alignment, Opacity)
```

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
SetSelectedIndex(Handle, Index) AS BOOLEAN
HandleKey(Handle, Key) AS NUMBER
Draw(Handle)
```

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
