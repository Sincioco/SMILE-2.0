# Royal Court r006 / Neris Town r005

September 28, 2026. Native delivery; Studio and Web adoption remain held.

## Result

- Royal Court's gatehouse vault now shares the jambs' front plane at Blender Y=-64.
  The former 0.4 m recess is removed. Ornamental trims retain separated depths;
  the 12 m opening, 15 m peak and all other castle owners are unchanged.
- Original Royal Castle side stairs and their rails are removed from Catalog-r002
  and two native catalog chunks: 526 objects and 17,320 unique triangles. Its
  26 central steps and threshold remain. Earlier authoring sources are preserved.
- Town r003 preserves all 361 live placements and the exact accepted Night values.
  Its 470 changed cells complete the road across Royal Court's frontage, meeting
  the open bridge at Z=1012. No Tripo instance is present; its reusable source and
  on-demand palette remain. Royal Court has a minimap label and Visit button.
- Simple map orbit uses the approved 33-degree pitch, 8,725 distance and 32-degree
  lens. It turns at 2.5 degrees per second, with no building selection or zoom changes.
  Manual town orbit reaches 0 degrees in both keyboard modes and cannot go negative.
- Fully opaque water retains blue color without showing submerged objects. Its
  localized screen-space reflections, sheen and sun shadows remain active.
- Day uses azimuth 207. Night uses RGB 140/174/255, intensity 8%, ambient 8%,
  azimuth 207, elevation 25 and shadow opacity 65%. Placed street posts and the
  Royal Castle, Military HQ, Comm Tower and City Hall emit native local light.
- City Hall reuses four smaller Royal Court evergreens. Existing catalog model
  space holds the added foliage, retaining capacity for the optional Tripo castle.
- Comm Tower dishes face front/back, alternating with its left/right crystal poles.
  All 16 dish/bracket members retain their identities and connected mounts.
  Town r005 and Catalog-r004 preserve the saved scene and native placement format.

## Validation

- Native town foundations, routes, rendering and session fixtures pass. Session
  retains 473 meshes, 442 materials, 50/64 models, 98 Royal Court parts and 24 Arin keys.
- Lighting checks cover slot 63, rejection of slot 64, full cleanup, lamp movement,
  removal, all four landmark types and lights switching off during the day.
- Camera regression samples one complete map revolution with fixed center, height
  and distance, plus pause and actual composition. Manual party/Fly inspection clamps at zero.
- WARP runs the production water shader: shadows at 0/50/100, localized reflected
  scene colors without distortion, and identical opaque-water pixels above black
  and white submerged beds. Water Lab native lifecycle passes.
- Native PBR checks and 58 Viewer hardening checks pass. The 327 compiler/language
  checks pass. VSIX 2.0.64 installed; all 35 payload hashes match the built package.
- Older r002 and new r003 Blender/native round trips preserve every saved Sun
  field, cell and assembly transform. Only the requested road rectangle changed.
- Actual Blender entrance day/night renders, town render and native night image
  accompany this checkpoint. The live Viewer was inspected at 119–120 FPS in
  sampled night views; this is not a general benchmark. Day/Night buttons match
  the accepted values. Town r003 was opened in Blender, preserving the separate
  castle editing session.

## Boundaries

Native local-light capacity is now 64; model and other resource caps are unchanged.
The town lighting owner updates existing fixed slots without per-frame allocation.
Web remains at four local slots pending explicit adoption. Screen-space reflection
cannot include offscreen objects. Sky cannot inject held-middle drags, so those
gestures have regression coverage but no new manual drag acceptance claim.
No palace interior or cutscene was added. No new dependency, setup change,
architecture baseline exception or persistent Python file was introduced.
