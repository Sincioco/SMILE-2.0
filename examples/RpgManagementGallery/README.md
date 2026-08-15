# RPG Management Gallery

This keyboard-first Game project composes the ordinary `Smile.RPG` 1.0.2 and `Smile.UI` 1.1.3 packages. It uses `Smile.UI.MenuNavigator` to demonstrate Party, Inventory, Equipment, Abilities and Magic Points, Shops, and Save / Load management without adding battle gameplay. The Live State panel reads the current RPG state every frame, so Party order, Gold, Inventory, Equipment, MP, Shop stock, and save-slot existence visibly refresh after actions. Its repeated `SaveGames.Exists` call is an observational query and does not replace a caller-owned codec payload.

Build it with the default DirectX backend, `--graphics Gdi`, or `--target web`. The Web build uses the existing DPR-aware SMILE canvas host; test it at browser device-pixel-ratio 2 as part of Phase 6 hands-on acceptance.

Use the arrow keys to navigate, Enter to open or run an action, and Left or Escape to return. Failed management actions report a safe rejection and leave state unchanged.
