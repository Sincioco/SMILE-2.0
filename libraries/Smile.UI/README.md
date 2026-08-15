# Smile.UI

Phase 5 reusable UI components authored entirely in SMILE: vector and nine-slice windows, fixed-grid bitmap fonts, multiline text dispatch, keyboard menus, bounded paged typewriter dialogue, and reusable hierarchical submenu navigation. Version 1.1.2 prevents valid bound submenu items from becoming leaves when opening is unavailable and gives fixed menu rows one prepared, vertically centered text/cursor/marker layout. It preserves active-edge pruning, cursors on every visible stack menu, proportional scrollbars, Unicode-safe overflow, viewport layout, shared children, and stale-handle/revision safety.

The library contains no assets and never plays sounds. Consuming applications own and load their skins, cursors, bitmap-font atlases, continuation indicators, and event sounds. See [API.md](API.md) for the public contract.
