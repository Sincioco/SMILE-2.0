# Party Beat Camera Editor — Desktop Checkpoint

This is the current native Beat Editor entry point. The editor runs in
[Sin Star Studio](../../tools/Character3DViewer/README.md), the existing Character
Viewer. It edits camera sequences independently of battle action timing.

- [Controls, persistence and owner map](party-beat-camera-contract.md)
- [Complete September checkpoint and evidence](archive/party-beat-camera-2026-09.md)
- [Studio architecture](../../tools/Character3DViewer/ARCHITECTURE.md)
- [Repository direction and asset authority](../../AGENTS.md)

## Current contract

Open a Party tab's Beat Preview/Edit controls to select a character and camera
shot. Beat Sequence replaces the animation timeline until Preview closes.
There are four nondeletable main markers and at most sixteen extra shots.
Dragging camera boundaries never retimes movement, animation, impact, audio/VFX
cues or gameplay counters. Pan, orbit, zoom and the fixed Camera panel continue
to control the displayed Beat camera after a seek.

Space plays/pauses the preview; another Space at the end restarts from zero.
Save writes the complete selected character sequence while keeping the editor,
selection and playhead open. Cancel or closing Preview restores the prior battle.
Head cuboids save independently. Failed head saves need identity-scoped Retry
or Discard; camera Save/Cancel cannot erase that warning.

Reset Beat, Reset All Beats and Reset All Beat Lengths have different scopes and
remain drafts until Save. See the [reset table](party-beat-camera-contract.md#editing-and-resets)
before using them. Existing V2 sequences, legacy camera records, head cuboids and
canonical pose JSON retain their separate identities.

## Open reports and preservation

**Arin load-rejection trigger remains unconfirmed.** The September 21 report
showed rejected storage despite matching canonical payloads; restarting removed
the reproduction. Current `ViewerCalibration` retries a rejected read before
import and retains rejection protection. That recovery path does not prove the
original trigger repaired. On recurrence, preserve the exact new rejection text,
selected profile/clip and canonical/live hashes before edits or restart; reproduce
that transition in the isolated fixture. Do not restore historical poses or relax
fingerprint validation. [Original report and hashes](archive/party-beat-camera-2026-09.md#open-native-arin-load-rejection-trigger--september-21-2026).

**The intermittent Kael Party recovery cause was not isolated.** Diagnostic
hardening and successful bounded attempts are not a root-cause repair. Hunting
was paused after the authorized ten-minute limit; this documentation cleanup
does not resume it. Preserve saved poses and Beat cameras. On recurrence, inspect
the launcher's captured report in `artifacts/reports/Character3DViewer` before
repeating interactions; [export helper](../../scripts/export-viewer-recovery.ps1)
and [original evidence](archive/party-beat-camera-2026-09.md#open-native-recovery-report--september-20-2026)
retain the recovery steps and limits.

The archived standalone `tools/SmileStudio` plans, build mismatch, old startup
selection, asset counts, VSIX versions and delivery hashes describe their dates.
The existing Character Viewer is now Studio and native development is active.
The separate Studio concept was abandoned; Web development/adoption/publication
remains paused until explicitly directed. These product directions are not a
request to resume either workstream or install a historical VSIX.

## Focused validation

From the repository root, the existing fixtures are:

```powershell
pwsh -NoProfile -File scripts/test-character-3d-viewer-hardening.ps1 -NativeOnly
pwsh -NoProfile -File scripts/test-viewer-calibration-native.ps1
```

`BeatCameraTests.smile` and `HardeningTests.smile` cover camera framing,
timing/persistence and preview behavior through those owners. Use isolated test
storage and preserve the accepted character JSON. Build/launch through the
[current Studio instructions](../../tools/Character3DViewer/README.md); explicitly
select Native when invoking its build script because its default also includes Web.
Run only affected checks for a change; this document rewrite reran no native,
compiler, VSIX, live-gesture or browser acceptance.

The archive preserves exact earlier PASS claims, failures, negative controls,
artifact hashes and human observations with their original context. Native
evidence does not establish Web parity. Outstanding historical Chrome gestures
and adoption prerequisites remain in the archive for an explicitly resumed scope.

## Historical entry points

Old heading links remain valid here and point to the preserved dated text.

<a id="open-native-arin-load-rejection-trigger--september-21-2026"></a>

- [September 21 Arin rejection](archive/party-beat-camera-2026-09.md#open-native-arin-load-rejection-trigger--september-21-2026)

<a id="native-battle-system-part-1b--september-20-2026"></a>
<a id="native-battle-system-part-1a--september-20-2026"></a>
<a id="native-playable-battle-follow-through--september-20-2026"></a>
<a id="held-studio-asset-inventory"></a>

- [September 20 battle and former Studio inventory](archive/party-beat-camera-2026-09.md#native-battle-system-part-1b--september-20-2026)

<a id="open-native-recovery-report--september-20-2026"></a>

- [September 20 Kael recovery report](archive/party-beat-camera-2026-09.md#open-native-recovery-report--september-20-2026)

<a id="native-earth-follow-up-2026-09-18"></a>
<a id="september-18-native-party-follow-through--web-remains-on-hold"></a>

- [September 18 Earth/Water and Party follow-through](archive/party-beat-camera-2026-09.md#native-earth-follow-up-2026-09-18)

<a id="native-review-hardening-2026-09-13"></a>

- [September 13 native hardening](archive/party-beat-camera-2026-09.md#native-review-hardening-2026-09-13)

<a id="imported-character-profile-alignment-2026-09-11"></a>
<a id="camera-frame-hardening-addendum-2026-09-11"></a>
<a id="additional-desktop-viewer-controls--september-11-2026"></a>

- [September 11 profiles, camera framing and controls](archive/party-beat-camera-2026-09.md#imported-character-profile-alignment-2026-09-11)

<a id="owners-and-compatibility"></a>

- [Current owners and compatibility](party-beat-camera-contract.md#owners-and-compatibility)

<a id="validation-and-observations"></a>
<a id="reproduced-defects-fixed-in-this-milestone"></a>
<a id="source-and-artifact-evidence"></a>

- [Historical validation, fixes and hashes](archive/party-beat-camera-2026-09.md#validation-and-observations)

<a id="on-hold--studio-and-web-adoption-record"></a>
<a id="next-action--remaining"></a>

- [Historical adoption record and then-current next steps](archive/party-beat-camera-2026-09.md#on-hold--studio-and-web-adoption-record)
