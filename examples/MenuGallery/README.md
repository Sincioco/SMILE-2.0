# MenuGallery

Phase 5.2.1 project/package acceptance client for the reusable `Smile.UI.MenuNavigator` library.

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
W                hide submenu indicators
A                place indicators after rendered text
S                right-align indicators
D                toggle scrollbar reservation/drawing
```

The sample stores plain labels. `MenuNavigator` bindings let `Smile.UI.Menu` hide or draw the exact literal ` >` after fitted text or in a reserved right-side region. Every visible menu draws its own cursor while only the deepest menu receives keyboard input; every row uses the same fixed cursor gutter so text never shifts.

The hierarchy contains three submenu levels beyond the root, a shared child, a disabled submenu binding, accepted leaves, and a scrollable eight-item detail menu. It demonstrates proportional top/middle/bottom thumb movement, scrollbar-off gutter reclamation, scalar-safe ellipsis, bounded two-line wrapping in both marker positions, system and bitmap text, right-side placement, automatic left fallback, bottom-edge adjustment, and narrow-space overlap. The automated `KEY_OTHER` path changes an ancestor selection and proves descendants prune immediately; it is intentionally omitted from the normal control legend.

All high-resolution PNG and WAV assets are application-owned and published through the existing project asset pipeline. `Smile.UI` owns no assets, menus, game loop, or sound playback.
