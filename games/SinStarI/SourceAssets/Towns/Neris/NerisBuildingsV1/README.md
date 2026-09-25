# Neris Buildings V1 — Blender Models

Created by: Louiery R. Sincioco (Sin).

Five editable, stylized exterior models reconstructed in Blender 5.2.1 LTS from the five ChatGPT contact sheets supplied in `games/SinStarI/Assets/Towns/Neris`. Created September 25, 2026.

## Open the models

| Building | Editable Blender file | GLB export | Four-view preview | Triangles |
| --- | --- | --- | --- | ---: |
| Weapon Store | [Open .blend](Blend/Neris-Weapon-Store.blend) | [Open .glb](../../../../Assets/Towns/Neris/ModelsV1/Neris-Weapon-Store.glb) | [Contact sheet](Previews/Neris-Weapon-Store-contact-sheet.png) | 251,804 |
| Armor Store | [Open .blend](Blend/Neris-Armor-Store.blend) | [Open .glb](../../../../Assets/Towns/Neris/ModelsV1/Neris-Armor-Store.glb) | [Contact sheet](Previews/Neris-Armor-Store-contact-sheet.png) | 254,170 |
| Item Store | [Open .blend](Blend/Neris-Item-Store.blend) | [Open .glb](../../../../Assets/Towns/Neris/ModelsV1/Neris-Item-Store.glb) | [Contact sheet](Previews/Neris-Item-Store-contact-sheet.png) | 261,446 |
| Communication Tower | [Open .blend](Blend/Neris-Communication-Tower.blend) | [Open .glb](../../../../Assets/Towns/Neris/ModelsV1/Neris-Communication-Tower.glb) | [Contact sheet](Previews/Neris-Communication-Tower-contact-sheet.png) | 184,192 |
| City Hall | [Open .blend](Blend/Neris-City-Hall.blend) | [Open .glb](../../../../Assets/Towns/Neris/ModelsV1/Neris-City-Hall.glb) | [Contact sheet](Previews/Neris-City-Hall-contact-sheet.png) | 370,342 |

The `Previews` folder also contains full-resolution front, back, left, right, and three-quarter beauty renders. [City hall rendered from its independently re-imported GLB](Previews/Neris-City-Hall-glb-check.png) provides a visual export check.

## What is modeled

- Ivory stone architecture with visible masonry courses, carved arches, teal domes and awnings, gold ribs, trim, raised lettering, star banners, cyan crystal fixtures, and warm lanterns.
- Weapon store: large cyan sword emblem, sword racks, workshop pipes, crates, barrels, and a shield display.
- Armor store: large shield crest, helmets, pauldrons, breastplates, and shield stands.
- Item store: flask medallion, potion shelves, bottles, and a sales counter.
- Communication tower: tapered shaft, buttresses, mounted relay dishes, antenna rings and braces, crystal mast, rear service door, and pipes.
- City hall: larger footprint and dome, broad stairs, balustrades, civic crystals, planted terraces, and distinct front/rear elevations.

These are authored 3D approximations of the references, not an automatic exact image reconstruction. Fine carving, weathering, small props, and irregularities in the generated references are simplified. Roof terraces are level instead of reproducing every slope in the images.

## Editing and export

Each `.blend` contains a named building collection with individually editable parts, a root empty at the building origin, standard PBR materials, preview cameras/lights, and the original reference sheet packed into the file. The reference is hidden in its own collection; unhide it to inspect it. No external image files are needed to open the model.

- Blender coordinates: meters; Z up; front toward -Y.
- Standard GLB conversion: Y up.
- Shops are approximately 12.23 m high, city hall 18.94 m, and tower 25 m.
- The original reference images were preserved byte-for-byte and verified against the packed copies.
- GLB files combine geometry by material (14–22 groups per building), bake curves/text/modifiers, clean zero-area faces, and triangulate the result. The preview ground, lights, camera, and reference are excluded.
- Materials use standard base color, metalness, roughness, and emission. No downloaded textures, plugins, or external generators were needed.
- Doors/windows are closed facade assemblies. There are no walkable interiors, collision meshes, animations, LOD levels, baked texture atlases, or game-runtime bindings.
- These are detailed concept assets, approximately 184k–370k triangles each. A separately authorized game-optimization pass can establish the target platform budget and reduce them. They have not been declared production-ready runtime assets.
- Building foundations meet Z=0. The weapon/armor exports have a small prop extent down to -0.005 m; the recorded bounds retain this sub-centimeter bevel extent.

The interactive Blender session was not replaced. Modeling, rendering, and validation ran in separate background Blender processes because the interactive MCP server was unavailable.

## Source ownership

All new authoring code is confined to this asset package. No SMILE compiler, runtime, Character Viewer, Studio, or website code changed.

| File | Responsibility |
| --- | --- |
| `Source/build.py` | Fresh-process setup, building selection, delivery orchestration |
| `Source/geometry.py` | Mesh primitives, transforms, palette, masonry |
| `Source/details.py` | Arches, heraldry, lamps, crystals, props |
| `Source/shops.py` | Commercial building composition |
| `Source/civic.py` | Communication tower and city hall composition |
| `Source/delivery.py` | Preview stage/cameras, packed references, Blender save, GLB export |
| `Source/validate.py` | Asset reload, packed-reference, geometry, and GLB round-trip checks |
| `Source/Review-Sheets.ps1` | Windows drawing API layout of the rendered contact sheets |

## Rebuild and validate

Preserve any hand-edited `.blend` as a new revision before rebuilding: these commands regenerate the named V1 output. The build script refuses an interactive or already-loaded Blender file, protecting existing scenes.

From this package directory in PowerShell 7:

```powershell
$blenderExe = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
& $blenderExe --background --factory-startup --python-exit-code 1 --python .\Source\build.py -- 'Weapon Store'
& $blenderExe --background --factory-startup --python-exit-code 1 --python .\Source\build.py -- 'Armor Store'
& $blenderExe --background --factory-startup --python-exit-code 1 --python .\Source\build.py -- 'Item Store'
& $blenderExe --background --factory-startup --python-exit-code 1 --python .\Source\build.py -- 'Communication Tower'
& $blenderExe --background --factory-startup --python-exit-code 1 --python .\Source\build.py -- 'City Hall'
& $blenderExe --background --factory-startup --python-exit-code 1 --python .\Source\validate.py
& .\Source\Review-Sheets.ps1
```

## Validation completed

[Machine-readable validation and checksums](validation.json) record all five passing results:

- Saved Blender files reopen successfully in background Blender.
- Packed reference hashes match the supplied original PNGs.
- Every GLB imports into a fresh scene without the source Blender file.
- All exported triangle counts match after re-import.
- Bounds match within 1 mm; all positions are finite and all meshes have materials.
- No preview camera or light is included in GLB exports.
- Five beauty views and twenty cardinal elevations were rendered and visually reviewed.
- A separate city hall GLB render was visually checked.

During review, zero-area export faces were removed, tower fixture/banner collisions were corrected, and city hall rear windows were moved clear of the corner pillars.

Open any delivered `.blend` directly in Blender. No .NET rebuild, browser refresh, or application restart is required.
