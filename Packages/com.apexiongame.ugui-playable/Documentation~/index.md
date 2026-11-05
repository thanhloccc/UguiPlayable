# UGUI Playable manual

UGUI Playable adds Timeline tracks and clips that animate uGUI. You build the animation in the Timeline window. You do
not write tween code.

This manual belongs to version `0.1.0-preview.2`. For install steps and requirements, see the package
[README](../README.md).

## How it works

- A **track** is bound to one component, for example a `CanvasGroup`. The track menu is **UGUI Playable > ...**.
- A **clip** on the track animates **channels** of that component. A channel is one animatable property, for example
  Alpha. Every tweened channel has an on/off switch, a from value, a to value and an ease.
- Clips can overlap. Timeline blends them, per channel.
- After a clip ends, the track holds the clip's end value until a later clip changes it. Punch and shake channels do
  not hold: they start and end at zero offset.
- A channel that no clip enables is never written. The clip inspector tells you when a clip animates nothing.

## Contents

| Page | What it covers |
|---|---|
| [getting-started.md](getting-started.md) | From an empty scene to a fading panel |
| [value-modes-and-restore.md](value-modes-and-restore.md) | Absolute, Relative, From current, and what happens when the timeline stops |
| [tracks-transform.md](tracks-transform.md) | Rect Transform: position, size, rotation, scale, pivot, anchors, punch, shake |
| [tracks-graphics.md](tracks-graphics.md) | Graphic, Image, Raw Image and Shadow/Outline |
| [tracks-groups-and-layout.md](tracks-groups-and-layout.md) | Canvas Group, Layout Element, Layout Group, Grid Layout Group |
| [tracks-controls.md](tracks-controls.md) | Slider, Scrollbar, Scroll Rect, Toggle, and the callback rules |
| [tracks-text.md](tracks-text.md) | TMP Text: colour, size, spacing, typewriter, gradient, string tween |
| [tmp-character-track.md](tmp-character-track.md) | TMP Characters: per-character effects and their limits |
| [multi-clip-editing.md](multi-clip-editing.md) | Select several clips and edit them together |
| [presets.md](presets.md) | Tween presets, the preset browser and the 27 starter presets |
| [custom-tracks.md](custom-tracks.md) | Writing your own track (preview API) |
| [troubleshooting.md](troubleshooting.md) | Problems and fixes, including TMP Essential Resources |

## The tracks at a glance

| Menu path | Binds to | Page |
|---|---|---|
| UGUI Playable/Transforms/Rect Transform | `RectTransform` | [tracks-transform.md](tracks-transform.md) |
| UGUI Playable/Graphics/Graphic | `Graphic` | [tracks-graphics.md](tracks-graphics.md) |
| UGUI Playable/Graphics/Image | `Image` | [tracks-graphics.md](tracks-graphics.md) |
| UGUI Playable/Graphics/Raw Image | `RawImage` | [tracks-graphics.md](tracks-graphics.md) |
| UGUI Playable/Effects/Shadow | `Shadow` (and `Outline`) | [tracks-graphics.md](tracks-graphics.md) |
| UGUI Playable/Groups/Canvas Group | `CanvasGroup` | [tracks-groups-and-layout.md](tracks-groups-and-layout.md) |
| UGUI Playable/Layout/Layout Element | `LayoutElement` | [tracks-groups-and-layout.md](tracks-groups-and-layout.md) |
| UGUI Playable/Layout/Layout Group | `HorizontalOrVerticalLayoutGroup` | [tracks-groups-and-layout.md](tracks-groups-and-layout.md) |
| UGUI Playable/Layout/Grid Layout Group | `GridLayoutGroup` | [tracks-groups-and-layout.md](tracks-groups-and-layout.md) |
| UGUI Playable/Controls/Slider | `Slider` | [tracks-controls.md](tracks-controls.md) |
| UGUI Playable/Controls/Scrollbar | `Scrollbar` | [tracks-controls.md](tracks-controls.md) |
| UGUI Playable/Controls/Scroll Rect | `ScrollRect` | [tracks-controls.md](tracks-controls.md) |
| UGUI Playable/Controls/Toggle | `Toggle` | [tracks-controls.md](tracks-controls.md) |
| UGUI Playable/Text/TMP Text | `TMP_Text` | [tracks-text.md](tracks-text.md) |
| UGUI Playable/Text/TMP Characters | `TMP_Text` | [tmp-character-track.md](tmp-character-track.md) |

## Things every track shares

- **Restore Mode** on the track: Keep (default) or Revert. See [value-modes-and-restore.md](value-modes-and-restore.md).
  The TMP Characters track has no Restore Mode.
- **Ease** on every tweened channel: one of 31 built-in easing functions (Linear, then Sine, Quad, Cubic, Quart,
  Quint, Expo, Circ, Elastic, Back and Bounce, each In, Out and In-Out) or a custom `AnimationCurve`. The default is
  Linear.
- **Blending and extrapolation**: clips support Timeline blending and extrapolation (hold, loop, ping-pong).
- **Axes**: a vector channel has one toggle per axis. An axis you turn off is left alone.
- **Set from binding**: a button next to a value copies the bound object's current value into the field.

## Limits and notes

Every limit is on the page of the feature it affects. The short list:

- Slider, Scrollbar and Toggle: callbacks only with **Send callbacks**, Play Mode only.
  [tracks-controls.md](tracks-controls.md)
- A Scrollbar track no longer scrolls a linked ScrollRect. [tracks-controls.md](tracks-controls.md)
- TMP Characters: no Reverse geometry sorting, underline and strikethrough stay still.
  [tmp-character-track.md](tmp-character-track.md)
- Shortest path rotation: 0 to 360 degrees is no turn. [tracks-transform.md](tracks-transform.md)
- Relative slides in presets and the interrupted-hide caveat. [presets.md](presets.md)
- Determinism and zero GC hold in the player and for Absolute and Relative channels; From current depends on the order
  when you scrub backwards; TMP's `SetCharArray` allocates in the Editor.
  [value-modes-and-restore.md](value-modes-and-restore.md)
