# M06 r002 delivery checkpoint

Implemented and tested under Sin's explicit autonomous continuation. This is the
current detailed exterior and native west-side comparison castle. Dimension and
artistic approval were not inferred. M00/M01 evidence and the earlier native
terrain work remain preserved in the original workspace and M01 package.

## Deliverables

- Source: Source/neris-castle-M06-r002.blend in the versioned repository package;
  working copy D:/Projects/Sin-Star-I-Assets/Neris-Castle/source/neris-castle-M06-r002.blend.
- Portable: neris-castle.glb, castle.glb and drawbridge.glb. Exact sizes/hashes are
  in offline-validation.json. Native/ contains eight bounded static GLBs and texture.
- Seven fixed-camera images and contact-sheet.png are actual Blender reimport renders.
  bridge-0/45/90.png prove the exported pivot, and entrance-detail.png shows the facade.
- Native/ contains real Viewer captures, test/build logs and native-validation.json.

## Measurements and tests

Source has 251,729 evaluated triangles and 11 asset materials. Portable candidate
has 251,250 triangles; 479 zero-area pole triangles were removed only from export
copies. Clean reimport retains bounds X[-70,70], Y[-84,82], Z[-5,60] metres.
Blender Z-up marker (3,7,11) maps to glTF (3,11,-7). Reimport restores the bridge
pivot (0,-64,-0.25), its three material parts and the expected 0/45/90-degree tips.
All accessor/index/UV and native payload checks pass. Source bridge clearance
checks cover 91 one-degree poses; route checks cover 422 cross sections.

Native loading uses nine models and 42 objects, including all three moving bridge
parts. Actual Viewer launch retained the current town and older castles. The
normal-height road connects to the leaf, which lowered when Arin approached.
Native tests also verify departure raises all three parts and block walking through
planters, fountain and the closed palace door. The palace has no authored interior.

Water shadow regression executes the actual HLSL on D3D WARP: the occluded pixel
sum changed from 0.181154 to 0.057355; the unoccluded sum remained 0.173791 and the
shadow SRV was released. Water Lab reports zero failures. The live town view shows
castle shadows on the moat. Reflections and ambient light remain visible in shade.

Native camera tests cover maximum-distance sky-facing requests, inspection that
retains the follow flag, upper orbit bound and preserved low party shot. Live UI
showed the party-to-inspection transition clamp to 8 degrees. Vertical slider,
zoom in/out, left-drag pan and Fit were exercised. Existing automated fixtures
cover slow/moderate input; a live middle-button drag is not claimed.

Full native/.NET/VSIX build passed, VSIX 2.0.64 installed with 35 payload hashes
verified. Viewer hardening passed 49 calibration and 58 native checks. Nine changed
SMILE sources pass formatting. Arin and Orin calibration exports match the current
working saves. The rebuilt Viewer is already launched to the right of Codex.

## Validation levels and limits

1. Candidate sizes/hashes: passed.
2. Khronos validator: not run; no installed offline validator found, no installation.
3. Clean Blender reimport, bounds, materials, pivot, fixed-camera renders: passed.
4. Independent visual consumer: the native SMILE Viewer, through its cooker; no
   separate raw-glTF viewer was used.
5. Actual SMILE project cooker and native runtime behavior: passed within the
   recorded route/camera/render checks. Web adoption was not performed.

The facade follows the reference's ivory/teal/gold palette, pointed apertures,
ribbed dome, crystal crown and heraldry. Ornament density is simplified relative
to the painted concept and existing Royal castle. Exact visual parity is not
claimed. Static heraldry, restrained materials and the measured composition were
preserved; no cloth simulation, chain physics, bloom or interiors were added.
Dimensions remain proposed. The Blender source viewport frames the entire castle,
but UI automation could not restore its minimized window; the complete native
castle was visibly inspected and captured. Source scenes and existing town data
were preserved. The native build and installed extension need no user rebuild.

## Ownership and change size

The asset package owns source/export/native payloads. NerisCastlePreview owns
resource lifetime; NerisCastleRoute owns collision/placement; ProximityDrawbridge
retains timing. NerisTownCamera owns camera limits. Water shader owns shadow
sampling; existing DirectX submission supplies constants/resources. No dependency,
public API, resource limit, architecture exclusion or baseline was changed.

| Owner | Before | After | Net |
|---|---:|---:|---:|
| tools/Character3DViewer/NerisCastlePreview.smile | 117 | 125 | +8 |
| tools/Character3DViewer/NerisCastleRoute.smile | 143 | 152 | +9 |
| tools/Character3DViewer/NerisTownCamera.smile | 333 | 360 | +27 |
| tools/Character3DViewer/NerisTownCameraPanel.smile | 176 | 180 | +4 |
| tools/Character3DViewer/NerisTown.smile | 812 | 815 | +3 |
| src/Smile.NativeRuntime/graphics/water_surface3d.h | 142 | 174 | +32 |
| src/Smile.NativeRuntime/graphics/graphics3d_directx.cpp | 10672 | 10684 | +12 |

NerisTown's three added lines are parameter wiring only. The legacy DirectX file's
12 added lines supply existing resources/constants; water shading stays in its
focused header. Existing hardening ownership checks passed. A new 136-line WARP
fixture directly protects the reported water defect. No speculative test suite,
repo-wide refactor or architecture exception was added.

## Recovery and remaining scope

No implementation task remains in this authorized exterior/native delivery.
Aesthetic acceptance and optional future fidelity work remain user decisions, not
implicit approvals. Preserve this source; future edits create a new revision and
rebuild only changed owners and real dependencies. Automation/README.md describes
operation replay limits. Do not restore the historical town snapshot over live edits.
Studio and all Web adoption/publication/browser validation remain on hold.
