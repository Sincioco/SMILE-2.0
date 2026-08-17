# MenuGallery

Smile.UI 2.0.0 project/package acceptance client for the reusable `Menus.Menu` and `Menus.MenuNavigator` Class facades. The setup deliberately uses `With`, nested `With`, constructors, properties, and named/default arguments while the library retains its private bounded engines.

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
Other keys       advance the automated pruning, cursor-offset, oversized-cursor, and clipping proofs
```

The sample stores plain labels. `Navigator.BindSubmenu(...)` lets the Menu runtime hide or draw the exact literal ` >` after fitted text or in a reserved right-side region. Every visible menu draws its own vertically centered cursor while only the deepest menu receives keyboard input; every row uses the same fixed cursor gutter so text never shifts.

The hierarchy contains three submenu levels beyond the root, a shared child, a disabled submenu binding, accepted leaves, and a scrollable eight-item detail menu. Its long shared-library command proves a complete two-line text block is centered in the fixed row, continuation text keeps the same label X, a right marker shares the first line Y, and an after-text marker shares its target line Y. It also demonstrates centered one-line rows, proportional top/middle/bottom thumb movement, scrollbar-off gutter reclamation, scalar-safe ellipsis, hidden-marker navigation, system and bitmap text, right-side placement, automatic left fallback, bottom-edge adjustment, and narrow-space overlap. The automated `KEY_OTHER` path changes an ancestor selection and proves descendants prune immediately; it is intentionally omitted from the normal control legend.

All high-resolution PNG and WAV assets are application-owned and published through the existing project asset pipeline. `Smile.UI` owns no assets, menus, game loop, or sound playback.
