# Smile.UI

Smile.UI 2.0.0 is the breaking lightweight-OOP migration of the reusable Phase 5 UI library. `Menu`, `MenuNavigator`, and `Dialogue` are now discoverable reference Classes with constructors, methods, properties, named/default arguments, and idempotent `Destroy()` actions. Their fixed-capacity generation-safe handle engines remain private, so stale-slot rejection and deterministic behavior are preserved.

Configuration and geometry remain value Types, while Window, Text, and BitmapFont keep their focused service surfaces. The library owns no assets and never plays sounds; applications load and own skins, cursors, bitmap-font atlases, continuation indicators, and event sounds.

The facade lifecycle contract is deliberately total. Invalid styles and exhausted fixed-capacity registries produce a facade whose `Valid` property is `False` without reserving a slot. `Destroy()` is idempotent, every method and property has a safe invalid-facade result, and every alias observes destruction immediately. Generation checks prevent a stale alias from reading or mutating a later facade that reuses the same private slot. Navigators repair destroyed roots and children, clear invalid accepted-leaf state, and retain a shared submenu marker until the final owning binding is removed. Applications should explicitly destroy navigators and dialogues before their menus, destroy menus before owned graphics, and then assign remaining facade references to `Nothing`.

The native and Web lifecycle fixtures cover invalid construction, exact-capacity failure and recovery, repeated destruction, post-destroy calls, stale aliases across slot reuse, unrelated destruction order, root/child invalidation, accepted-state repair, and shared-marker cleanup. Native fixture runs also require `SMILE_CLASS_LIVE=0`.

See [API.md](API.md) for the complete 2.0 contract.
