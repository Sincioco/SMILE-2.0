# Phase 4 high-resolution 2D media

Phase 4 adds one shared Windows/Web language surface for modern illustrated and pre-rendered 2D media. Windows uses WIC plus Direct2D or GDI+, and Web uses Canvas 2D. SMILE source does not change between targets.

## Images and ownership

`IMAGE` is an opaque owned built-in type. Its empty value is safe. It can be used in scalar and array variables, record fields, typed parameters, `BYREF` parameters, and function returns.

```smile
DIM Portrait AS IMAGE
LOAD IMAGE Portrait FROM "Assets\Portrait.png"
PRINT IMAGE_WIDTH(Portrait)
PRINT IMAGE_HEIGHT(Portrait)
PRINT IMAGE_LOADED(Portrait)
UNLOAD IMAGE Portrait
```

Loaded PNGs retain their original dimensions and per-pixel alpha. Repeated loads of the same normalized path share one immutable decoded resource. Assignment retains that resource; replacing, unloading, or leaving an owning scope releases it. The last release permits cache eviction. Program shutdown clears remaining image, WAV, and backend resources.

On both targets, an IMAGE expression produces one owned value. Assignment, `BYVAL`, and `RETURN` transfer that owner; `BYREF` aliases the storage location. `IMAGE_WIDTH`, `IMAGE_HEIGHT`, `IMAGE_LOADED`, and `DRAW IMAGE` consume expression temporaries. Record copies retain owned fields exactly once, while record-return temporaries transfer without an extra clone.

## Drawing

Full-image drawing uses the original image as its source:

```smile
DRAW IMAGE Portrait AT 40, 30
DRAW IMAGE Portrait AT 40, 30 SIZE 480 BY 270 FILTER SMOOTH
```

Explicit source and destination rectangles are independent:

```smile
DRAW IMAGE Sheet FROM 512, 0 SIZE 512 BY 512 AT 480, 440 SIZE 320 BY 320 ANCHOR 160, 320 OPACITY 80 FILTER SMOOTH FLIP HORIZONTAL
```

`SMOOTH` is the default filter. `FILTER PIXEL` opts into nearest-neighbor scaling. Opacity is from 0 through 100. `FLIP HORIZONTAL`, `FLIP VERTICAL`, and `FLIP BOTH` preserve the destination anchor. Drawing remains immediate and follows source order, so images, shapes, and text have deterministic painter-order layering.

Rotation is not part of Phase 4 because it could not be added with the same simple semantics across Direct2D, GDI+, and Canvas without broadening this milestone.

Web canvases keep logical SMILE coordinates while allocating equal visible/back-buffer physical dimensions from CSS size and `devicePixelRatio`, capped at 8192 pixels per dimension and 33,554,432 pixels total. Canvas transforms are restored after resize, fullscreen, orientation, and DPR changes. Smooth sampling remains the default and high-resolution sources rasterize directly into the physical backing store.

## Structured clipping and text measurement

Clips can nest and intersect. The compiler restores each active clip on normal exit and before `RETURN`, `EXIT FOR`, `EXIT DO`, or `END PROGRAM` transfers control out of its scope.

```smile
CLIP RECTANGLE 100, 80, 500, 360
    CLIP RECTANGLE 160, 120, 220, 180
        DRAW IMAGE Portrait AT 100, 80 SIZE 500 BY 360
    END CLIP
END CLIP
```

`TEXT_WIDTH(Text, Size)` and `TEXT_HEIGHT(Text, Size)` use the target's actual text engine and match `DRAW TEXT` closely enough for dynamic layout. Cross-backend tests should compare sensible ordering and ranges rather than exact pixels.

The logical clip stack is independent of a backend frame. Native user clips unwind before Direct2D/GDI presentation and reapply on the next frame or after resize/fullscreen/DPI changes. Web backing-store changes rebuild the same logical nested clips. Empty text has width zero and a positive height for every positive requested size.

## Persistent binary data

Phase 4 keeps the existing integer `LOAD` and `SAVE` statements and adds exact byte blocks:

```smile
DIM Bytes[256]
DIM ByteCount AS NUMBER
SAVE DATA Bytes COUNT 256 TO "PlayerProfile"
LOAD DATA "PlayerProfile" INTO Bytes COUNT ByteCount
```

Bytes must be 0 through 255. Count cannot exceed the fixed one-dimensional destination/source array or `DATA_BLOCK_MAX_BYTES` (1 MiB). A missing key returns zero count and a zeroed buffer. Native writes use a temporary file plus atomic replacement; Web stores a versioned base64 block in the application's isolated local-storage namespace. Invalid or corrupt blocks fail safely and visibly.

DATA keys are case-sensitive exact UTF-8 values identified by SHA-256, so punctuation and Unicode cannot collide through filename sanitization. The compiler supplies a stable application identity (`OutputName` for projects) that is independent of `GAME WINDOW` title. Native and Web use the same `SMD4` version-1 envelope: magic, version, byte length, SHA-256 payload digest, and payload. Legacy integer `LOAD` and `SAVE` keep their existing format and behavior.

All media paths use one canonical project-relative form with `/` separators. Repeated separators and `.` segments collapse; contained `..` segments normalize, while escaping traversal, drive/rooted/UNC paths, URI schemes, NUL, empty paths, undeclared assets, and incorrect project asset case are rejected. IMAGE, WAV effects, music, and text assets share this rule.

## WAV effect channels and music

`SOUND_CHANNEL_COUNT` is 16. The original `PLAY SOUND` statement uses channel 0. Explicit channels can overlap; replay on a channel replaces only that channel.

```smile
PLAY SOUND "Assets\Impact.wav" ON CHANNEL 1
PLAY SOUND "Assets\Spark.wav" ON CHANNEL 2
STOP SOUND ON CHANNEL 1
STOP SOUND
```

Bare `STOP SOUND` stops all effects. Background `PLAY MUSIC`, pause/resume, and volume remain separate. Losing focus stops all effect channels, suppresses new effect requests, and does not queue them for replay. Music retains its independent focus policy. Native and Web cache decoded WAV resources by logical path.

Web requests carry a per-channel generation across every asynchronous boundary, so only the newest request may start and late completion cannot clear a replacement. Normal completion, `END PROGRAM`, runtime failure, and `pagehide` use one idempotent shutdown path. Native XAudio2 callbacks only mark channel/generation completion; the main thread reaps and destroys completed voices.

## Visual proof

Open `examples\Phase4VisualSlice\Phase4VisualSlice.slnx`. Its deterministic original assets include a 2304x1296 illustrated background, a transparent 2048x1024 two-state sprite sheet, a transparent 1920x1080 foreground, a 37x53 pixel-filter proof, two WAV effects, and separate looping WAV music.

`examples\Phase4Hardening` adds deterministic DATA key/corruption, IMAGE return/record ownership, clip-across-frame/resize, and stale same-channel audio fixtures.
