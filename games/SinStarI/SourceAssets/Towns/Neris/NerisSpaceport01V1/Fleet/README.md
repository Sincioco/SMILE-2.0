# Four alien visitors — r001

These original visitors occupy the four elevated pads of Neris Spaceport.
They deliberately have distinct silhouettes and materials rather than Neris heraldry.

| Ship | Design | Triangles |
| --- | --- | ---: |
| Aster Saucer | Silver lenticular hull, cyan ion belt | 15,108 |
| Vesper Manta | Swept violet wings, amber drives | 9,236 |
| Khepri Tri-Pod | Copper seed hull and three green engine pods | 10,884 |
| Vanta Prism | Crimson angular hull and violet drives | 5,816 |

`Alien-Visitors-r001.blend` contains editable geometry, materials and a review camera.
`Previews/Fleet.png` is an actual Blender render. `Native` holds four three-part
GLBs, validated against `manifest.json` by the parent preparation script.
`Source/build_fleet.py` rebuilds these assets with the existing static GLB exporter.

`NerisAlienFleet.smile` owns native resource lifetime and independent pad clocks.
Randomized ground time is 18–28 seconds; lifting and descent each take 5–7 seconds.
After the departing craft clears the pad, it stays completely empty for 10–15
seconds before an incoming craft begins descending. Initial departures are also
randomized. This supersedes the earlier fixed 20-second stagger request.

The four models use 12 mesh/material parts, 20,640 vertices and 41,044 triangles.
Pads retain the authored 64 m / 104 m deck heights. Models sit 1 m above those
surfaces and rise vertically. Existing native limits and save formats are unchanged.
The three-map native session checks loading, flight transforms, the minimum clear
interval and release on map changes. The linked Blender export shows parked ships.
