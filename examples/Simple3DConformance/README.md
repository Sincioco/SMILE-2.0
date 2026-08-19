# True Simple3D conformance sample

This sample renders all six required indexed primitives with a perspective camera, overlapping near/far cubes for depth validation, continuous rotation, and a Renderer2D HUD above the 3D pass.

Build Windows DirectX:

```powershell
artifacts\compiler\smilec.exe --project examples\Simple3DConformance\Simple3DConformance.smileproj --target windows-x64 --graphics DirectX -o artifacts\examples\Simple3DConformance\Simple3DConformance.exe
```

Build Web:

```powershell
artifacts\compiler\smilec.exe --project examples\Simple3DConformance\Simple3DConformance.smileproj --target web --output-dir artifacts\web\Simple3DConformance
```

Use WASD or the arrows to move the camera, resize the window/browser to verify aspect recomputation, and press Escape to exit.
