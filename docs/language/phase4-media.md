# Phase 4 high-resolution 2D media

Phase 4 adds one shared Windows/Web language surface for modern illustrated and pre-rendered 2D media. Windows uses WIC plus Direct2D or GDI+, and Web uses Canvas 2D. SMILE source does not change between targets.

## Images and ownership

`Image` is an opaque owned built-in type. Its empty value is safe. It can be used in scalar and array variables, record fields, typed parameters, `ByRef` parameters, and function returns.

```smile
Dim Portrait As Image
Load Image Portrait From "Assets\Portrait.png"
Print Image_Width(Portrait)
Print Image_Height(Portrait)
Print Image_Loaded(Portrait)
Unload Image Portrait
```

Loaded PNGs retain their original dimensions and per-pixel alpha. Repeated loads of the same normalized path share one immutable decoded resource. Assignment retains that resource; replacing, unloading, or leaving an owning scope releases it. The last release permits cache eviction. Program shutdown clears remaining image, WAV, and backend resources.

On both targets, an Image expression produces one owned value. Assignment, `ByVal`, and `Return` transfer that owner; `ByRef` aliases the storage location. `Image_Width`, `Image_Height`, `Image_Loaded`, and `Draw Image` consume expression temporaries. Record copies retain owned fields exactly once, while record-return temporaries transfer without an extra clone.

## Web startup and reusable downloads

Generated Web pages display a startup loader before the runtime and program
scripts execute. It shows actual outstanding asset paths and completed-file
counts with an indeterminate activity bar: this is not a whole-download byte
percentage, because programs may discover more assets while loading. The first
presented frame or console output dismisses it. Runtime failure exposes the
error panel; a failed script download reports a reload/connection message.

Application project properties `WebLoadingAuthor` (one-line text, 1–128 characters)
and `WebLoadingLogo` (project-relative PNG path) optionally add creator credits
and a logo. Metadata is validated in the shared project model and HTML-escaped.
The PNG is published as `Assets/Branding/WebLoadingLogo.png` using the existing
transactional asset publisher. Missing files, invalid PNG signatures and files
larger than 8 MiB block Web compilation; image decoding remains browser-owned.
Native builds do not publish this Web-only asset. The loader's SMILE copyright
and links identify the language/platform, not the application's copyright owner.

The Web runtime retains up to 128 MiB / 256 entries of encoded model/image
downloads in page-local least-recently-used storage. This avoids another network
request after decoded owners are released; it does not retain actors, poses,
materials or save data. Files are fetched fresh on their first request in a new
page, failed downloads are not cached, and shutdown releases retained bytes.
No service worker, persistent browser cache or extra preloading download is added.

## Drawing

Full-image drawing uses the original image as its source:

```smile
Draw Image Portrait At 40, 30
Draw Image Portrait At 40, 30 Size 480 By 270 Filter Smooth
```

Explicit source and destination rectangles are independent:

```smile
Draw Image Sheet From 512, 0 Size 512 By 512 At 480, 440 Size 320 By 320 Anchor 160, 320 Opacity 80 Filter Smooth Flip Horizontal
```

`Smooth` is the default filter. `Filter Pixel` opts into nearest-neighbor scaling. Opacity is from 0 through 100. `Flip Horizontal`, `Flip Vertical`, and `Flip Both` preserve the destination anchor. Drawing remains immediate and follows source order, so images, shapes, and text have deterministic painter-order layering.

Rotation is not part of Phase 4 because it could not be added with the same simple semantics across Direct2D, GDI+, and Canvas without broadening this milestone.

Web canvases keep logical SMILE coordinates while allocating equal visible/back-buffer physical dimensions from CSS size and `devicePixelRatio`, capped at 8192 pixels per dimension and 33,554,432 pixels total. Canvas transforms are restored after resize, fullscreen, orientation, and DPR changes. Smooth sampling remains the default and high-resolution sources rasterize directly into the physical backing store.

## Web virtual controls

Graphical Web output includes a reusable on-screen controller for touch-first play. It remains hidden until `Game Window` runs, so Console programs are unchanged. Its default `Auto` policy uses `navigator.maxTouchPoints`, the `pointer: coarse` and `hover: none` media features, and an observed touch or pen Pointer Event. It does not parse the browser user agent.

The generated page accepts deterministic visibility overrides:

```text
?smile-controls=on
?smile-controls=off
?smile-controls=auto
```

The D-pad maps to `KEY_UP`, `KEY_DOWN`, `KEY_LEFT`, and `KEY_RIGHT`. A, B, X, and Y map to the distinct pad-only constants `KEY_PAD_A`, `KEY_PAD_B`, `KEY_PAD_X`, and `KEY_PAD_Y`. The compact default does not emit `KEY_ESCAPE` because existing games may use Escape as immediate program termination. Physical keyboard keys retain their existing behavior and do not emit pad-only values. Touch, pen, and primary mouse presses on a visible controller enter the same bounded, source-aware broker as keyboard input, so each press creates one `Get Key` event and `Key_Held(...)` remains true until every owning source releases. Multi-touch direction-plus-action input is supported, and every accepted D-pad or action-button press follows the same music-unlock path.

The controller uses eight real accessible buttons with restrained translucent outlines, safe-area-aware portrait and landscape layouts, and at least 56-by-56 CSS-pixel targets. Landscape placement uses the dynamic visible viewport. In portrait, the game canvas and controls are vertically centered together so mobile browser chrome does not push the lower controls off screen. All D-pad arrows use one geometrically centered CSS triangle rotated around its center, avoiding font-baseline drift and emoji-style rendering. Gesture suppression is limited to the controls; browser zoom is not disabled globally. The published Web output remains exactly `index.html`, `smile-runtime.js`, `game.js`, and `smile.css`, and the same `.smile` source continues to target Windows and Web.

Each Web compilation places a deterministic content version in `index.html` and appends it to the generated CSS and JavaScript URLs. The page requests a no-store freshness check on initial display and browser page restoration; when the deployed build marker changes, it reloads through a versioned page URL while preserving existing query options. Generated no-cache metadata also asks static hosts not to retain `index.html`. The first deployment from an older compiler may still require one manual refresh because an already-cached old page cannot contain the new freshness logic. Server `Cache-Control` response headers remain authoritative and should allow `index.html` to revalidate.

The standard profile does not infer arbitrary custom or same-device multiplayer keyboard layouts. Such games need a future explicit profile or game-declared mapping; the runtime does not duplicate aliases or branch on game names.

Web keyboard input belongs to the focused program canvas (or focused console for
console programs). Clicking the canvas claims focus. Typing in another HTML
control, composing text, browser/OS modifier shortcuts and function keys do not
enter `Get Key` or create held game actions. Ctrl+Left/Right remain available to
canvas programs for frame navigation; Control is held state, not a queued action
by itself. Repeated keydown events do not duplicate queued actions. Leaving the
program surface clears queued/held keyboard input without taking ownership away
from independently held virtual-controller buttons.

`Shift+Tab` leaves the canvas and focuses the generated Full Screen button,
which becomes visible on keyboard focus; Enter activates it. Alt+Enter also
toggles fullscreen while the program surface has focus. The button follows the
browser's actual fullscreen state and is hidden when fullscreen is unsupported.
Browser F11 and other reserved shortcuts remain browser-owned. No fullscreen
request is made on load or by a timer.

## Game-canvas pointer input

Every `Game Window` program can read the mouse, pen, or touch contact over its logical canvas through the same cross-target API:

```smile
If Pointer_Pressed(POINTER_PRIMARY) Then
    Print Pointer_X(); ","; Pointer_Y()
End If

If Pointer_Held(POINTER_PRIMARY) Then
    Print Pointer_Delta_X(); ","; Pointer_Delta_Y()
End If
```

`POINTER_PRIMARY`, `POINTER_SECONDARY`, and `POINTER_MIDDLE` identify the three supported button roles. `Pointer_X()` and `Pointer_Y()` use the logical `Game Window` pixel coordinates after native letterboxing or Web CSS/DPR scaling. `Pointer_Inside()` reports whether the latest position lies within that logical canvas. `Pointer_Delta_X()`, `Pointer_Delta_Y()`, and `Pointer_Wheel_Delta()` accumulate events during the current frame. On Windows, `Pointer_Wheel_Remainder()` reports retained raw wheel units smaller than one 120-unit step; Web input is already normalized and reports zero remainder. `Pointer_Held(Button)` persists while at least one contact owns that button; `Pointer_Pressed(Button)` and `Pointer_Released(Button)` latch even when both transitions occur in one frame.

`Show Screen` ends the input frame and clears pointer deltas, wheel movement, and pressed/released transitions. Held state and the last position remain. Native captures a pressed pointer until release. Web listens only on the game canvas, uses Pointer Events and canvas-scoped `touch-action: none`, and keeps virtual-controller button events isolated in the existing source-aware key broker. Capture loss, cancellation, focus loss, page hiding, runtime completion, and runtime failure release active contacts so a game cannot retain stuck input. Console programs are unchanged.

## Structured clipping and text measurement

Clips can nest and intersect. The compiler restores each active clip on normal exit and before `Return`, `Exit For`, `Exit Do`, or `End Program` transfers control out of its scope.

```smile
Clip Rectangle 100, 80, 500, 360
    Clip Rectangle 160, 120, 220, 180
        Draw Image Portrait At 100, 80 Size 500 By 360
    End Clip
End Clip
```

`Text_Width(Text, Size)` and `Text_Height(Text, Size)` use the target's actual text engine and match `Draw Text` closely enough for dynamic layout. Cross-backend tests should compare sensible ordering and ranges rather than exact pixels.

The logical clip stack is independent of a backend frame. Native user clips unwind before Direct2D/GDI presentation and reapply on the next frame or after resize/fullscreen/DPI changes. Web backing-store changes rebuild the same logical nested clips. Empty text has width zero and a positive height for every positive requested size.

## Persistent binary data

Phase 4 keeps the existing integer `Load` and `Save` statements and adds exact byte blocks:

```smile
Dim Bytes[256]
Dim ByteCount As Number
Save Data Bytes Count 256 To "PlayerProfile"
Load Data "PlayerProfile" Into Bytes Count ByteCount
```

Bytes must be 0 through 255. Count cannot exceed the fixed one-dimensional destination/source array or `DATA_BLOCK_MAX_BYTES` (1 MiB). A missing key returns zero count and a zeroed buffer. Native writes use a temporary file plus atomic replacement; Web stores a versioned base64 block in the application's isolated local-storage namespace. Invalid or corrupt blocks fail safely and visibly.

Data keys are case-sensitive exact UTF-8 values identified by SHA-256, so punctuation and Unicode cannot collide through filename sanitization. The compiler supplies a stable application identity (`OutputName` for projects) that is independent of `Game Window` title. Native and Web use the same `SMD4` version-1 envelope: magic, version, byte length, SHA-256 payload digest, and payload. Legacy integer `Load` and `Save` keep their existing format and behavior.

All media paths use one canonical project-relative form with `/` separators. Repeated separators and `.` segments collapse; contained `..` segments normalize, while escaping traversal, drive/rooted/UNC paths, URI schemes, NUL, empty paths, undeclared assets, and incorrect project asset case are rejected. Image, WAV effects, music, and text assets share this rule.

## Project asset resolution and publication

Application projects declare the complete runtime asset set with `<Asset Include="..." />`. The shared resolver accepts exact files plus `*` and `?` within one path segment and `**` as a complete zero-or-more-directory segment. Matching is ordinal and case-sensitive, results are sorted, overlaps publish once, directories never publish, and an empty wildcard is valid. Unsupported glob syntax, missing explicit assets, wrong explicit-path case, output collisions, and library-owned assets report project-located `SML36xx` diagnostics.

Both native and Web `smilec --project` builds publish the resolved files automatically after code generation succeeds. Native files go beside the selected `.exe`; Web files go beside `index.html`, `game.js`, `smile-runtime.js`, and `smile.css`. A small safe publication manifest removes only formerly managed assets on later builds. Loose-file compilation has no project manifest and therefore continues to require manual asset placement.

## WAV effect channels and music

`SOUND_CHANNEL_COUNT` is 16. The original `Play Sound` statement uses channel 0. Explicit channels can overlap; replay on a channel replaces only that channel.

```smile
Play Sound "Assets\Impact.wav" On Channel 1
Play Sound "Assets\Spark.wav" On Channel 2
Stop Sound On Channel 1
Stop Sound
```

Bare `Stop Sound` stops all effects. Background `Play Music`, pause/resume, and volume remain separate. Losing focus stops all effect channels, suppresses new effect requests, and does not queue them for replay. On Web, backgrounding the browser or hiding the page pauses requested music and returning to the foreground resumes it; an explicit `Pause Music` remains paused. Native keeps its established focus-volume policy. Native and Web cache decoded WAV resources by logical path.

Web requests carry a per-channel generation across every asynchronous boundary, so only the newest request may start and late completion cannot clear a replacement. Normal completion, `End Program`, runtime failure, and `pagehide` use one idempotent shutdown path. Native XAudio2 callbacks only mark channel/generation completion; the main thread reaps and destroys completed voices.

## Visual proof

Open `examples\Phase4VisualSlice\Phase4VisualSlice.slnx`. Its deterministic original assets include a 2304x1296 illustrated background, a transparent 2048x1024 two-state sprite sheet, a transparent 1920x1080 foreground, a 37x53 pixel-filter proof, two WAV effects, and separate looping WAV music.

`examples\Phase4Hardening` adds deterministic Data key/corruption, Image return/record ownership, clip-across-frame/resize, and stale same-channel audio fixtures.
