# Graphic, Image, Raw Image and Shadow tracks

These tracks change how a uGUI element looks. All tweened channels default to **Absolute** mode and a **Linear** ease,
with no axis ignored. See [value-modes-and-restore.md](value-modes-and-restore.md).

## Color and alpha

The three graphic tracks share a Color channel and an Alpha channel. Both write the `color` of the graphic.

- **Color** is RGBA. If Alpha is off, the A of the Color channel is used too.
- **Alpha** is the A component only.
- If both are on, Alpha decides the A and Color decides RGB.
- Only the components the track wrote are restored (RGB, or A, or both).

## Graphic track

Menu: **UGUI Playable > Graphics > Graphic**. Binds to any `Graphic` (`Image`, `RawImage`, legacy `Text`, and others).

| Channel | Type | Default From | Default To | Default state |
|---|---|---|---|---|
| Color | Color | white | white | off |
| Alpha | float | 0 | 1 | off |

## Image track

Menu: **UGUI Playable > Graphics > Image**. Binds to an `Image`.

| Channel | Type | Default From | Default To | Default state |
|---|---|---|---|---|
| Color | Color | white | white | off |
| Alpha | float | 0 | 1 | off |
| Fill Amount | float | 0 | 1 | off |

Fill Amount shows only when the image's **Image Type** is **Filled** (a Unity rule). Use it for progress bars and radial
timers.

## Raw Image track

Menu: **UGUI Playable > Graphics > Raw Image**. Binds to a `RawImage`.

| Channel | Type | Default From | Default To | Default state |
|---|---|---|---|---|
| Color | Color | white | white | off |
| Alpha | float | 0 | 1 | off |
| UV Rect | Vector4 (x, y, width, height) | (0, 0, 1, 1) | (1, 0, 1, 1) | **on** |

UV Rect has axes X, Y, W and H. Animate X or Y alone to scroll the texture; animate W and H to zoom. The default clip
scrolls the texture one full width. Set the texture's **Wrap Mode** to **Repeat** so it tiles while it scrolls.
Loop the clip with Timeline's extrapolation for an endless background.

## Shadow track

Menu: **UGUI Playable > Effects > Shadow**. Binds to a `Shadow`. `Outline` derives from `Shadow`, so the track accepts
an outline too.

| Channel | Type | Default From | Default To | Default state |
|---|---|---|---|---|
| Effect Color | Color | (0, 0, 0, 0.5) | (0, 0, 0, 1) | **on** |
| Effect Distance | Vector2 | (1, -1) | (4, -4) | off |

## Tips

- To fade a whole panel with its children, use a Canvas Group track instead. See
  [tracks-groups-and-layout.md](tracks-groups-and-layout.md).
- To fade out from whatever the current alpha is, set the Alpha channel to **From current** with To = 0.
- A clip with every channel off shows "This clip animates nothing. Turn on a channel."

Next: [tracks-groups-and-layout.md](tracks-groups-and-layout.md).
