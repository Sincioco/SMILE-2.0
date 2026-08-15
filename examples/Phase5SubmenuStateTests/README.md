# Phase5SubmenuStateTests

Pure console project/package coverage for `Smile.UI.MenuNavigator` and the flat-menu foundation. Its 80 exact results cover hierarchy/input events, leaf results, shared children, active-edge pruning after programmatic selection or disable, accepted-state clearing, non-pruning label/value/style/position changes, reset/preserve behavior, cycles, stale revisions/handles, exact capacities, indicator validation, Unicode label limits, and transactional viewport placement.

Phase 5.2.2 adds a real ninth-child binding at active depth eight and proves Right, Enter, and Space remain inert without changing accepted state. It also covers hidden markers, a true leaf at depth eight, failed child reset, disabled bound parents, and current-navigator binding authority when another navigator maintains the shared visual marker.

The fixture intentionally contains no `Game Window`; all state, binding, input, and geometry APIs remain console-safe.
