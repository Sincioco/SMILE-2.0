# Space Wars audio assets

The six WAV files in this directory are original deterministic procedural laser, explosion, shield-hit, mission-start, mission-complete, and victory sound effects generated for this repository by `scripts/generate-space-wars-audio.ps1`.

They are mono 16-bit PCM WAV files at 22,050 Hz. Regenerate them from the repository root with:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\generate-space-wars-audio.ps1
```
