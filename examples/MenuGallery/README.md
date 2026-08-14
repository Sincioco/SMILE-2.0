# MenuGallery

Phase 5.2 project/package acceptance client for the reusable `Smile.UI.MenuNavigator` library.

Controls:

```text
Up/Down          move within the active menu
Right            open the selected submenu
Enter/Space      open a submenu or accept a leaf
Left             close exactly one submenu level
Escape           close one level; cancel at the root
1                nine-slice window with system text
2                nine-slice window with bitmap text
3                vector window with system text
```

The sample stores plain labels. `MenuNavigator` bindings make `Smile.UI.Menu` draw the automatic literal ` >` marker in a reserved right-side region. Only the topmost menu draws the active cursor; ancestor menus retain their path highlight without appearing focused, and every row uses the same fixed cursor gutter so text never shifts when selection changes.

The hierarchy contains three submenu levels beyond the root, a shared child, a disabled submenu binding, and accepted leaves. It demonstrates default scalar-safe ellipsis, bounded two-line wrapping, system and bitmap text, right-side placement, automatic left fallback, bottom-edge upward adjustment, and narrow-space overlap while every complete menu remains inside the 960-by-540 logical viewport.

All high-resolution PNG and WAV assets are application-owned and published through the existing project asset pipeline. `Smile.UI` owns no assets, menus, game loop, or sound playback.
