# MP3 music and focus-muting manual checklist

Automated tests prove syntax, diagnostics, emitted calls, executable linking, asset identity, volume derivation, focus-state transitions, and inactive WAV suppression policy without requiring speakers. The checks below require a person who can hear the test machine; do not mark them complete based only on process survival.

Run both variants with an adjacent copy of `Assets\Background.mp3`:

```text
artifacts\games\FallingBlocks\FallingBlocks-DirectX.exe
artifacts\games\FallingBlocks\FallingBlocks-GDI.exe
```

For each backend:

- [ ] Title screen is silent.
- [ ] Enter starts gameplay and looping music from the beginning.
- [ ] Movement, rotation, and line-clear WAV effects remain audible over music.
- [ ] Alt+Tab immediately silences music and WAV effects.
- [ ] Returning to the game continues music from its current position at the same requested volume.
- [ ] Minimizing silences all game audio; restoring applies the same policy as Alt+Tab.
- [ ] Twenty focus changes at `Music Volume 50` show no cumulative volume drift.
- [ ] A manually paused track remains paused after focus loss and return.
- [ ] A stopped track remains stopped after focus loss and return.
- [ ] A WAV requested while inactive is not heard later after return.
- [ ] Game over stops music before the game-over WAV.
- [ ] Retry restarts music from the beginning.
- [ ] Alt+Enter preserves playback and true borderless full-screen behavior.
- [ ] Escape and window close leave no MediaPlayer/system-media session behind.
- [ ] Windows master volume and every other application remain unchanged.
- [ ] Missing or corrupt MP3 fails silently while the game continues.

Record the test date, Windows edition, audio device, and backend beside any completed run.
