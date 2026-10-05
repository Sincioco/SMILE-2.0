# Game Window, drawing and input

[Language reference](README.md) · Window lifecycle, graphics, audio and native input.

## Loading and close requests

`Window_Loading()` returns a `Boolean` and requires `Game Window`. Call it before
loading another scene to show the standard SMILE logo, credits, links and original
artifact build stamp again. It returns after visible presentation begins, allowing
asset loading to overlap the one-second visible minimum. The next `Show Screen`
submits the new scene and closes the splash after any remaining minimum. Repeated
calls during one loading interval share that interval. Native and Web use the
same contract; this operation does not replace or disable mandatory startup.
`False` means loading preparation failed or was cancelled. Keep the old scene
and edits in that case; another call can retry.

```smile
Dim LoadingShown As Boolean

LoadingShown = Window_Loading()

If LoadingShown Then

    Call LoadNextScene()

End If

Show Screen
```

`Window_DeferClose(Enabled As Boolean)` returns `Boolean` and requires `Game Window`.
The default is `False`. On native Windows, enabling it keeps the window alive when
the user presses X or Alt+F4; `Window_CloseRequested()` consumes that request and
returns `True` once. The application's ordinary frame loop can offer Save, Discard
and Cancel, and must only exit after a successful save or explicit discard.
Disabling deferral restores normal native closing. No automatic save is implied.

On Web, enabling deferral requests the browser's standard unsaved-work confirmation
on tab close/reload. Browser policy requires user interaction and controls the prompt;
applications cannot customize it or guarantee a prompt during process termination.
`Window_CloseRequested()` returns `False` on Web because that prompt belongs to the
browser. In-application Close buttons can use their own ordinary frame-loop dialog.

## Game surface

```smile
Game Window "Example" Size 960 By 540

Load HighScore From "HighScore" Default 0
Play Sound "Assets\Start.wav"
Music Volume 70
Play Music "Assets\Background.mp3" Loop

Do
    Get Key Key
    Clear Rgb(12, 18, 30)
    Fill Rounded Rectangle 380, 450, 200, 22, 7, LIGHT_BLUE
    Fill Quadrilateral 0, 0, 240, 80, 240, 460, 0, 540, DARK_GREEN
    Draw Quadrilateral 0, 0, 240, 80, 240, 460, 0, 540, LIGHT_GREEN
    Draw Circle 480, 300, 12, WHITE
    Draw Arc 480, 300, 40, 180, 90, LIGHT_BLUE
    Draw Line 40, 40, 920, 40, DARK_GRAY
    Draw Text "Score" At 40, 15 Size 18 Color CYAN
    Draw Number HighScore At 130, 10 Size 28 Color YELLOW
    Show Screen
    Wait 16 Milliseconds
Loop Until Game_Closed() = True

Save HighScore To "HighScore"
Stop Music
Stop Sound
End Program
```

Drawing statements support filled or outlined rectangles, rounded rectangles, circles, and arbitrary four-corner quadrilaterals, plus outlined arcs, lines, text expressions, and numbers. Quadrilaterals take four perimeter-ordered `(X, Y)` points followed by a color. `Show Screen` presents the logical canvas. `Play Sound` starts an asynchronous WAV effect and missing files are safe. `Load` and `Save` persist integer values in storage isolated by executable name.

## Arc drawing

```smile
Draw Arc CenterX, CenterY, Radius, StartAngle, SweepAngle, Color
```

`Draw Arc` draws only the curved outline using the normal one-logical-pixel graphics stroke. It does not fill a pie slice, draw a chord, or connect either endpoint to the center. `Fill Arc` is not part of the language.

Angles are integer degrees in screen coordinates:

| Angle | Direction |
|---:|---|
| `0` | right |
| `90` | down |
| `180` | left |
| `270` | up |

Positive sweeps move clockwise and negative sweeps move counterclockwise. Start angles normalize to `0` through `359`. A zero sweep or non-positive radius draws nothing; an absolute sweep of at least `360` draws one complete circle. `examples\ArcBasics.smile` demonstrates four joined rounded corners, both sweep directions, a long arc, and a complete circle.

## Background music

Background-music syntax is:

```smile
Play Music "Assets\Background.mp3"
Play Music "Assets\Background.mp3" Loop
Pause Music
Resume Music
Stop Music
Music Volume 50
```

Music paths are resolved relative to the generated executable. `Music Volume` accepts a numeric expression; the native runtime clamps the requested level to 0 through 100. MP3 playback uses the Windows `Windows.Media.Playback.MediaPlayer` API through C++/WinRT and Windows Media Foundation, independently of the selected graphics backend. No third-party decoder is bundled. Windows installations missing required media components fail playback safely without terminating the game.

## Automatic focus behavior

Every `Game Window` program inherits the same native focus behavior without adding SMILE activation code:

- loss of application activation, top-level window activation, or minimization immediately silences that game's audio;
- MP3 playback continues silently at effective volume zero, preserving both playback position and the exact requested `Music Volume`;
- restoring an active, non-minimized window reapplies the requested volume without restarting playback or resuming a track paused or stopped by the program;
- the current asynchronous WAV effect stops on focus loss, and new `Play Sound` requests are suppressed while inactive rather than queued for later;
- Windows master volume and other applications are never changed;
- DirectX and GDI follow the identical shared runtime policy.

## Named input and colors

Named input constants include `KEY_W`, `KEY_A`, `KEY_S`, `KEY_D`, `KEY_O`, `KEY_F`, `KEY_G`, `KEY_R`, `KEY_P`, `KEY_B`, `KEY_X`, `KEY_Y`, `KEY_Z`, `KEY_E`, `KEY_CONTROL`, `KEY_BACKTICK`, the four arrows, `KEY_ENTER`, `KEY_ESCAPE`, `KEY_SPACE`, `KEY_1`, `KEY_2`, `KEY_3`, `KEY_4`, `KEY_TAB`, `KEY_OTHER`, `KEY_NONE`, and the pad-only `KEY_PAD_A`, `KEY_PAD_B`, `KEY_PAD_X`, and `KEY_PAD_Y`. The four pad constants have distinct values 23 through 26 and are emitted by the matching generated Web virtual-controller buttons; physical keyboard keys do not emit them. `KEY_3` has value `20`, `KEY_TAB` has value `21`, `KEY_4` has value `22`, `KEY_O` has value `27`, `KEY_F` has value `28`, `KEY_G` has value `29`, `KEY_R` has value `30`, `KEY_P` has value `31`, `KEY_B` has value `32`, `KEY_CONTROL` has value `33`, `KEY_BACKTICK` has value `34`, `KEY_X` has value `35`, `KEY_Y` has value `36`, `KEY_Z` has value `37`, and `KEY_E` has value `38`; `Get Key` returns `KEY_OTHER` (value `19`) for an otherwise unnamed ordinary key event, and `Key_Held(KEY_OTHER)` is always false. Named colors include the standard red/green/blue/cyan/magenta/yellow set plus orange, gray, dark variants, light variants, black, and white.

`KEY_PLUS` (39) and `KEY_MINUS` (40) are available to native apps. Plus maps the
main `+`/`=` key or numpad Add; minus maps the main `-`/`_` key or numpad Subtract.
They support `Get Key`, `Key_Held` and queued `Key_Event_Held` snapshots. The Water
Lab uses them for global animation speed. Existing key values are unchanged.
Web input adoption remains on hold and is not claimed as supported.

`KEY_C` (41) exposes C through native `Get Key`, `Key_Held` and queued
`Key_Event_Held` snapshots. The Battle System uses it to ease the camera back
to its default orders view. Existing key values are unchanged. Web input
adoption remains on hold.

`KEY_M` (42) exposes M through native `Get Key`, `Key_Held` and queued
`Key_Event_Held` snapshots. Neris Town uses it to show or hide the minimap.
Existing key values are unchanged; Web input adoption remains on hold.

`KEY_BACKTICK` maps the US grave-accent/tilde key (Windows `VK_OEM_3`, browser physical `Backquote`) in `Get Key` and `Key_Held`. The physical glyph can differ with keyboard layout. Character Viewer and Fire Lab use it to hide/show their entire UI; it does not change animation pause state.

For native file dialogs, text prompts, Unicode scalar construction and appended editor keys, see [Data and files](data-and-files.md#native-text-prompts-and-editor-keys).
