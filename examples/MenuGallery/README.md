# MenuGallery

Phase 5 visual and interactive proof for project and package references.

Controls:

```text
Up/Down     navigate
Enter/Space accept or advance
Escape      cancel dialogue
SUB-MENUS > open the three-level cascading menu demonstration
1           nine-slice + system text
2           nine-slice + bitmap text
3           vector fallback + system text
```

Theme keys remain active while dialogue is revealing. The current raw page and visible scalar count survive valid system/bitmap/vector changes, and the sample includes multiline dialogue in both text modes.

Choose `SUB-MENUS >` from the main menu to open a Phantasy Star II-style three-level cascade: party member, command, and target. Enter or Space opens the next level, while Escape closes one level at a time. The game composes four ordinary `Smile.UI.Menu` handles (root plus three nested menus), demonstrating painter-order layering without adding RPG-specific hierarchy behavior to the reusable UI library.

All high-resolution PNG and WAV assets are original, generated into this application project, and published by the existing project asset pipeline. `Smile.UI` owns no assets.
