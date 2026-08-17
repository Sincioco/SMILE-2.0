# RPG Systems Battle asset provenance

Every asset in this directory is original work produced for the public Phase 9 gallery. No commercial game image, sprite, recording, extracted data, or derivative asset was used.

The five PNG files were generated with the built-in OpenAI ImageGen workflow, then used directly as application-owned raster assets:

| File | Prompt/design brief |
| --- | --- |
| `StarfallPlateau.png` | High-resolution original 2D science-fantasy JRPG battlefield at night, luminous blue plateau, distant planets and crystal flora, clean side-view composition, no characters, logos, text, or recognizable franchise elements. |
| `LumenPlaza.png` | High-resolution original top-down science-fantasy town plaza, pale stone paths, gardens, glowing technology and civic buildings, readable walkable center, no characters, logos, text, or recognizable franchise elements. |
| `PrismVault.png` | High-resolution original first-person crystalline dungeon corridor, symmetrical forward perspective, dark violet stone and luminous prism machinery, open staging area, no characters, logos, text, or recognizable franchise elements. |
| `PartyLineup.png` | Original four-member science-fantasy adventurer roster, distinct silhouettes and colors, rear/three-quarter battle poses, polished 2D game illustration, no logos, text, or recognizable franchise characters. |
| `EnemyLineup.png` | Original multi-group science-fantasy enemy roster with crystal beasts and floating constructs, distinct silhouettes, polished 2D game illustration, no logos, text, or recognizable franchise creatures. |

The roster images intentionally retain their baked light checker field and are presented as framed roster cards rather than transparency-dependent sprites.

The six WAV files are original deterministic synthesis generated at 44.1 kHz mono PCM by the tracked `scripts\generate-phase9-battle-audio.ps1`:

- `OverworldTheme.wav`, `TownTheme.wav`, and `DungeonTheme.wav` are looping scene themes.
- `Strike.wav`, `Ability.wav`, and `Victory.wav` are battle cues.

Regenerate the WAVs from the repository root with:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\generate-phase9-battle-audio.ps1
```

The gallery project publishes exactly these eleven declared assets for native and Web builds.
