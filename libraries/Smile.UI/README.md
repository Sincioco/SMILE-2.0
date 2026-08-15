# Smile.UI

Phase 5 reusable UI components authored entirely in SMILE: vector and nine-slice windows, fixed-grid bitmap fonts, multiline text dispatch, keyboard menus, bounded paged typewriter dialogue, and reusable hierarchical submenu navigation. Version 1.1.1 adds active-edge pruning, a cursor on every visible stack menu, proportional scrollbars, and hidden/after-text/right-aligned submenu indicators while preserving Unicode-safe overflow, viewport layout, shared children, and stale-handle/revision safety.

The library contains no assets and never plays sounds. Consuming applications own and load their skins, cursors, bitmap-font atlases, continuation indicators, and event sounds. See [API.md](API.md) for the public contract.
