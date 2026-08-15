# SMILE-MAP format 1

SMILE-MAP 1 is a UTF-8, integer, row-major tile field parsed by `Smile.Game.TileMap`. Spaces, tabs, commas, CR/LF, and `#` line comments are accepted where appropriate. Keywords are uppercase and the file must end after `END` except for whitespace/comments.

```text
SMILE-MAP 1
SIZE <width> <height>
CELL <cell-width> <cell-height>
GROUND
<height rows of width tile IDs>
[DETAIL
<height rows>]
[FOREGROUND
<height rows>]
COLLISION
<height rows of 0 or 1>
REGIONS
<height rows of region IDs>
END
```

The section order is fixed. Ground, Collision, and Regions are required. Detail and Foreground may independently be omitted and are then zero-filled. Tile ID zero means no image for optional layers; applications may define IDs 1–255 against an image atlas. Region zero means no application event.

Limits:

- width and height: 1–64
- total cells: at most 4,096
- cell width/height: positive values in the shared safe range
- tile IDs and region IDs: 0–255
- collision values: exactly 0 or 1
- source file: at most 131,072 bytes

`LoadMap` is transactional and returns zero for a missing, truncated, extra-token, out-of-range, or otherwise malformed file. Cell/world conversion uses integer division and map cell dimensions. Drawing receives an application-owned tileset and camera offset and visits only the visible range with one-cell interpolation overscan.
