# RPG Battle Gallery

The Battle option in `RPGSystems` is the public Phase 9 presentation and integration proof for the renderer-neutral `Smile.RPG` battle modules.

The title menu opens three original exploration/battle presentations:

- Starfall Plateau: overworld side-view staging;
- Lumen Plaza: top-down exploration and compact battlefield staging;
- Prism Vault: cardinal first-person exploration and front-facing battle staging.

Arrow keys or W/A/S/D move in exploration. Enter starts a battle after recording the exact `Smile.RPG.World` scene, cell, and facing. Victory, defeat, or escape returns to that exact location.

The battle command menu exposes Fight, Strategy, Order, and Run. Fight repeats standing party orders. Strategy selects an atomic party preset. Order changes the lead character's standing action. Run either escapes or grants an enemy-only round. Escape requests an after-round interrupt while actions are resolving.

All artwork and audio in `Assets` are original Phase 9 assets produced for this public example. The battle modules themselves load no image or sound and issue no draw or audio command.
