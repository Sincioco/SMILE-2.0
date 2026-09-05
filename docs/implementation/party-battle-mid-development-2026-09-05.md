# Party battle: mid-development source checkpoint

September 5, 2026. Requested by Sin so ChatGPT can inspect the latest source.
This is an in-progress development push, not a completed milestone or release.

## Implemented in this checkpoint

- Party's Camera 1 advances continuously through 360 degrees, retaining fractional
  yaw during composition. Cameras 2, 3 and 4 provide portrait, approach and strike
  compositions. Shot selection switches cameras; motion within a shot eases.
  The strike camera has a small impact push-in. Close-ups use a world-up direction
  and pull back to keep the Dragon's head and attacking character inside the frame.
- Left-panel hit testing no longer claims the arena. Existing middle-button orbit,
  left-button pan and eased wheel zoom remain available. Manual interaction takes
  over the camera. The Party roster and controls sit below the character tabs.
- Orin Block plays once and holds its final pose in individual inspection and Party
  playback. Party stage changes do not restart an already-held guard.
- Original synthesized Arin slash/crosscut and Dragon breath/claw/fireball sounds
  are stored with their character packages. A cue crossing plays once per cycle.
- The Dragon preview rig now has six clips, including Fireball, stronger wing/arm
  motion and Hit recoil. New eye sockets and mouth heat use shared VFX resources.
  Fireball has mouth charge, projectile travel and impact-burst phases.
- The complete Fire Lab and Lightning Lab projects moved from `examples` into
  `tools`, beside Character3DViewer. Build and active validation paths were updated.
  Lightning Lab defaults to the same screen-fixed landscape as Fire Lab and adds
  gentle height/distance variation to its cinematic orbit. Both Labs were rebuilt
  and relaunched from their new locations; their application IDs remain stable.

## Validation

- Native Character Viewer compilation and asset publication (42 assets).
- Native hardening wrapper, including 42 calibration checks, isolated save-data
  persistence, fractional-orbit/wrap checks, and 58 native graphics/input/audio checks.
- Five generated attack WAVs and two manifests pass reproducibility checking.
- Dragon builder validates finite first/middle/final poses for six clips while
  preserving the 9,912-triangle source geometry and 24-bone contract.
- Both Labs compile from their new Tools locations and run natively.
- Saved calibration exported: Arin 23 keys, Orin 0 keys, unchanged identities.
- Native camera inspection is ongoing. No long soak or Web visual validation is claimed.

## Requested work still in progress

1. Dynamic Party right panel showing the current attacker and its details.
2. Dragon inspection/editor tab before Party, with party opponents.
3. Retarget the newly supplied Arin Death FBX; party KO/guard/revive demonstration.
4. Dragon head tracking toward its attack target.
5. Wider party formation on a front arc, ready for additional members.
6. Arin shield fire outline without outward flames.
7. Denser Orin hammer sparks and lingering swing trail; stronger lightning impacts.
8. Further visual review of camera composition, Dragon fireball and effects.

The active native editor source is `tools/Character3DViewer`. Models, audio and
calibration remain under the canonical ArinV57, OrinV13 and RedDragonV11 packages.
Unrelated local reference-image edits and experimental asset exports are not part
of this source checkpoint. No SMILE syntax, native runtime ABI or VSIX payload changed.
