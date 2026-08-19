# Web and Mobile Virtual Controls

**Introduced:** post-`d1893fbd24226cd22e856939b8af4dee95c719f7` implementation  
**Scope:** generated SMILE Web host and runtime

## Purpose

SMILE Web games use the same `.smile` source as Windows games. Browser keyboard events already feed the runtime key queue used by `Get Key` and held state used by `Key_Held(...)`.

Touch-first devices need an on-screen controller that feeds those same structures. The controller is host UI above the Web canvas, not part of SMILE drawing and not a language keyword.

## Capability boundary

**Flag:** SMILE 2.0 has keyboard input in Web games but no reusable touch-screen virtual-control adapter. Mobile Web players therefore need the Web runtime to translate pointer presses into existing Get Key and Key_Held(...) behavior.

The reusable capability is a generated Web overlay plus a source-aware input broker. It handles button press, hold, release, cancellation, and simultaneous pointer gestures. It does not add arbitrary touch coordinates, swipe recognition, or a general gesture API.

## Generated files

The feature remains within the existing generated files:

```text
index.html
smile-runtime.js
game.js
smile.css
```

`index.html` carries a deterministic content build marker and versioned CSS/JavaScript references. The shared runtime performs a no-store same-page freshness check on `pageshow`; if a newly deployed marker differs, it reloads through a versioned URL without changing the four generated filenames.

No additional script, image, font, package, or framework is required.

## Visibility

The exact `smile-controls` query parameter controls visibility:

- `auto` or missing — capability-based behavior;
- `on` — force visible after `Game Window`;
- `off` — force hidden.

Unknown, malformed, or duplicated values fall back to `auto`. Auto initially considers positive `navigator.maxTouchPoints` together with a coarse primary pointer or a no-hover environment. An observed touch or pen Pointer Event can reveal controls on a hybrid device. The runtime does not parse the browser user agent.

Controls remain hidden until `Game Window` is created. Console programs therefore remain unchanged.

## Standard mapping

| Control | Key |
|---|---|
| Up | `KEY_UP` |
| Down | `KEY_DOWN` |
| Left | `KEY_LEFT` |
| Right | `KEY_RIGHT` |
| A | `KEY_PAD_A` |
| B | `KEY_PAD_B` |
| X | `KEY_PAD_X` |
| Y | `KEY_PAD_Y` |

A virtual press queues one value and owns held state until release. It does not enqueue browser-style repeats or duplicate aliases. The four action buttons have independent pad-only values, so games can assign them independently without colliding with physical keyboard controls.
The compact default intentionally does not emit `KEY_ESCAPE`: several bundled games treat Escape as immediate program termination, which would make one action-button tap irreversibly stop a mobile session. Physical keyboard keys retain their existing behavior.

## Shared input broker

Keyboard and pointer sources share one broker. Each active source owns one key:

```text
keyboard:<KeyboardEvent.code>
pointer:<PointerEvent.pointerId>
```

A held key may have multiple owners. Releasing one owner does not release another. This prevents premature release when keyboard and a virtual button hold the same key.

The queue remains bounded at 256 entries. Active input sources are bounded at 32. Excess input sources are ignored without evicting an established press.

## Pointer and mouse behavior

The runtime accepts touch and pen Pointer Events and primary mouse presses whenever the controller is visible. This lets the forced-on mode support desktop testing and lets a touch-first Web game continue to use the visible controls when a mouse is attached. Desktop Auto remains hidden in an ordinary fine-pointer, hover-capable environment.

Accepted virtual pointers use pointer capture when available. The runtime releases ownership after `pointerup`, `pointercancel`, `lostpointercapture`, orientation change, blur, visibility loss, page hide, controls hiding, runtime finish, or runtime failure. Cleanup is idempotent and does not depend on a final pointer-up reaching the button.

## Browser gesture policy

The overlay root ignores pointer events outside controls. Buttons receive pointer events and use `touch-action: none`, which contains pan/zoom gestures that start on a game control.

The feature does not disable browser zoom globally and does not add `user-scalable=no`. The canvas and the rest of the page retain their existing browser behavior.

## Layout and accessibility

The controller uses real buttons, descriptive ARIA labels, `aria-pressed`, visible focus styling, a restrained translucent outline, safe-area insets, and responsive portrait/landscape layouts. Every target is at least 56 by 56 CSS pixels. Dynamic viewport units keep landscape controls inside the browser's visible viewport. In portrait, the canvas and a dedicated controls region form one vertically centered composition instead of placing controls against the page bottom. All D-pad directions use one geometrically centered CSS triangle rotated around its center, avoiding both font-baseline drift and emoji-style mobile glyph substitution. The controller uses CSS shapes and text, so it introduces no runtime asset.

## Audio interaction

Every accepted D-pad or action-button press counts as user interaction and follows the same music synchronization path. Merely displaying controls does not start media. When the page loses visibility, requested Web music pauses; foreground focus resumes it unless the program explicitly paused or stopped it.

## Diagnostics

Web media diagnostics expose the selected controls mode, visibility, active virtual pointer count, active input-source count, queue count, and finite limits. They do not expose the user agent, device model, pointer coordinates, or pointer history.

## Compatibility boundary

The immutable `standard` profile covers normal single-player directional, confirm, action, and menu input without exposing a destructive Escape control. The data-driven profile object is the internal seam for future explicit profiles.

A fixed pad cannot infer arbitrary custom controls or same-device multiplayer mappings. It also cannot know whether a game's Escape key means safe cancel or immediate exit. The runtime does not branch on game names or change key numeric values. A future explicit profile or game declaration would be needed for a dedicated cancel control or layouts such as one local player using `W`/`S` while another uses arrow keys.

## Non-goals

This feature does not add:

- SMILE syntax or project/package schema;
- native Windows virtual controls;
- touch coordinates, analog sticks, swipe recognition, or arbitrary gestures;
- haptics, accelerometer, or physical Gamepad API support;
- remapping UI or automatic local-multiplayer layouts;
- a service worker, PWA, npm dependency, or browser framework.
