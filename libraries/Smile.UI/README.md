# Smile.UI

Smile.UI 2.0.0 is the breaking lightweight-OOP migration of the reusable Phase 5 UI library. `Menu`, `MenuNavigator`, and `Dialogue` are now discoverable reference Classes with constructors, methods, properties, named/default arguments, and idempotent `Destroy()` actions. Their fixed-capacity generation-safe handle engines remain private, so stale-slot rejection and deterministic behavior are preserved.

Configuration and geometry remain value Types, while Window, Text, and BitmapFont keep their focused service surfaces. The library owns no assets and never plays sounds; applications load and own skins, cursors, bitmap-font atlases, continuation indicators, and event sounds.

See [API.md](API.md) for the complete 2.0 contract.
