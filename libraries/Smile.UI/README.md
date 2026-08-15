# Smile.UI

Phase 5 reusable UI components authored entirely in SMILE: vector and nine-slice windows, fixed-grid bitmap fonts, multiline text dispatch, keyboard menus, bounded paged typewriter dialogue, and reusable hierarchical submenu navigation. Version 1.1.3 standardizes the public `Core.Insets.Left` and `Core.Insets.Right` presentation casing while retaining the Phase 5.2.2 bound-item acceptance and fixed-row alignment hardening. It preserves active-edge pruning, cursors on every visible stack menu, proportional scrollbars, Unicode-safe overflow, viewport layout, shared children, and stale-handle/revision safety.

The library contains no assets and never plays sounds. Consuming applications own and load their skins, cursors, bitmap-font atlases, continuation indicators, and event sounds. See [API.md](API.md) for the public contract.
