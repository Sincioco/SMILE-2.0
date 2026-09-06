# H6.1 Web delivery — Start Here

This is the full-fidelity Character Viewer, Advanced Fire VFX Lab and Advanced
Lightning VFX Lab at implementation commit
`bc6f607bec5a60df1e72a0d3541156bc9175fe82`. It is not the future optimized-tier
delivery. Canonical textures and character packages have not been downsized.

## Publish or run

Unpack the Web ZIP into a new folder. Publish each application's complete folder,
including `Assets` and `TechnicalAssets`, to a static HTTP/HTTPS server. Keep
filenames and letter case unchanged. Do not upload just `index.html`. No Node,
.NET, native DLL, package manager or server-side application is needed by visitors;
the browser needs JavaScript, WebGL2 and permission to load same-origin assets.
Audio starts only after user activation. Use Chrome for the declared tested baseline.

For local inspection, if Python 3 is already available, run this in the extracted
folder:

```powershell
python -m http.server 8770 --bind 127.0.0.1
```

Open `http://127.0.0.1:8770/Character3DViewer/`,
`http://127.0.0.1:8770/AdvancedFireVfxLab/`, or
`http://127.0.0.1:8770/AdvancedLightningVfxLab/`. Do not use `file://`.
Upload to a fresh versioned directory to avoid stale generated JavaScript or assets.
No public deployment or slow-network measurement is claimed for this ZIP.

## Rebuild from the repository

On the configured SMILE Windows development machine, from `D:\SMILE 2.0`:

```powershell
cmd /c scripts\build.cmd
pwsh -NoProfile -File tools\Character3DViewer\Build.ps1 -Configuration Release -Target All
pwsh -NoProfile -File tools\AdvancedFireVfxLab\Build.ps1 -Configuration Release -Target All
pwsh -NoProfile -File tools\AdvancedLightningVfxLab\Build.ps1 -Configuration Release -Target All
```

The compiler's existing Windows build prerequisites apply; consult the repository
build documentation rather than installing arbitrary tools from this delivery.
Normal Web folders are `tools\<application>\bin\Release\Web`; native executables
are `tools\<application>\bin\Release\<application>.exe` with their published assets.
Launch the native Viewer through `tools\Character3DViewer\Launch.ps1` to maintain
the existing calibration synchronization workflow.

## Saves, checksums and limits

The browser uses origin-scoped storage, not files on drive D:. A different host,
scheme or port has different saves. Packaged defaults seed missing saves and do
not overwrite newer browser keys. Download Key Frames exports a portable JSON;
Import Key Frames validates identity and asks before replacing keys. Downloads
do not automatically update the repository or native saves.

The artifact manifest lists every packaged Web file and its SHA-256, plus the ZIP,
native executables, compiler and VSIX identities. The evidence/handoff ZIP also
contains the reports, source references and selected actual logs. No private native
Save Data envelope, source GLB/FBX/Blend package or diagnostic HTML is included in
the public Web ZIP. Asset license/provenance rules in the repository still apply.

The existing loader is included. The newly requested byte-progress bars, compile
metadata, one-second shared splash and Web Optimized Low/Medium/High profiles are
separate approved milestones, not features claimed by this hardening delivery.
