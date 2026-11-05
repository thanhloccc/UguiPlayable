# UGUI Playable

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md)
[![Unity 6000.3+](https://img.shields.io/badge/Unity-6000.3%2B-black?logo=unity)](https://unity.com/releases/editor/archive)
[![Version](https://img.shields.io/github/v/tag/thanhloccc/UguiPlayable?include_prereleases&sort=semver&label=version)](https://github.com/thanhloccc/UguiPlayable/tags)

Timeline tracks, clips and UI Toolkit editor tooling for animating uGUI in Unity. Move, fade, scale, recolor and fill
UI elements on a Timeline instead of writing tween code.

> Status: preview (`0.1.0-preview.2`). The tracks and the editor tools work. The base classes for custom tracks are
> marked "Preview API" and may change before 1.0. See [CHANGELOG.md](CHANGELOG.md).

## What you get

- 15 Timeline tracks for uGUI: Rect Transform, Graphic, Image, Raw Image, Canvas Group, Shadow/Outline, Layout Element,
  Layout Group, Grid Layout Group, Slider, Scrollbar, Scroll Rect, Toggle, TMP Text and TMP Characters.
- Blending clips, 31 easing functions plus a custom curve, and three value modes: Absolute, Relative and From current.
- Punch and shake channels on the Rect Transform track, and a per-character track for TextMeshPro.
- A clip inspector built with UI Toolkit, with multi-clip editing.
- 27 starter tween presets (popups, screens, toasts, buttons, attention, text, lists) and a preset browser.

## Requirements

- Unity 6000.3 or newer
- Timeline 1.8.13 or newer (`com.unity.timeline`)
- uGUI 2.0.0 or newer (`com.unity.ugui`), which includes TextMeshPro

Both packages are installed as dependencies of this package.

## Installation

Open **Window > Package Manager**, choose **+ > Install package from git URL…** and enter:

```text
https://github.com/thanhloccc/UguiPlayable.git?path=/Packages/com.apexiongame.ugui-playable
```

To pin a release, append `#` and a tag name, for example `#0.1.0-preview.2`.

OpenUPM: planned. There is no OpenUPM install line yet.

## Quick start

1. Put a `CanvasGroup` on a UI panel.
2. Select the panel, open **Window > Sequencing > Timeline** and click **Create**.
3. Right-click the track area and choose **UGUI Playable > Groups > Canvas Group**.
4. Bind the panel to the track, right-click the track and add a clip.
5. Drag the playhead. The panel fades from 0 to 1, because the Alpha channel is on by default.

[getting-started.md](Documentation~/getting-started.md) walks through this step by step.

## Documentation

The manual is in [Documentation~/index.md](Documentation~/index.md). It is plain Markdown and Unity does not import it.

| Page | Topic |
|---|---|
| [getting-started.md](Documentation~/getting-started.md) | An empty scene to a fading panel |
| [value-modes-and-restore.md](Documentation~/value-modes-and-restore.md) | Absolute, Relative, From current, Restore Mode |
| [tracks-transform.md](Documentation~/tracks-transform.md) | Rect Transform track |
| [tracks-graphics.md](Documentation~/tracks-graphics.md) | Graphic, Image, Raw Image, Shadow |
| [tracks-groups-and-layout.md](Documentation~/tracks-groups-and-layout.md) | Canvas Group and the layout tracks |
| [tracks-controls.md](Documentation~/tracks-controls.md) | Slider, Scrollbar, Scroll Rect, Toggle |
| [tracks-text.md](Documentation~/tracks-text.md) | TMP Text |
| [tmp-character-track.md](Documentation~/tmp-character-track.md) | TMP Characters and its limits |
| [multi-clip-editing.md](Documentation~/multi-clip-editing.md) | Editing several clips together |
| [presets.md](Documentation~/presets.md) | Tween presets and the 27 starter presets |
| [custom-tracks.md](Documentation~/custom-tracks.md) | Writing your own track (preview) |
| [troubleshooting.md](Documentation~/troubleshooting.md) | Common problems |

## Sample

The package has one sample, shown in **Window > Package Manager > UGUI Playable > Samples**. It is a small scene with
screens, popups and a toast, driven by shared transition timelines. The sample text uses TextMeshPro, so import the TMP
Essential Resources first (**Window > TextMeshPro > Import TMP Essential Resources**). See
[troubleshooting.md](Documentation~/troubleshooting.md).

## Determinism and allocation

- Scrubbing a clip gives the same result every time. The punch and shake channels use a seed, not a random generator.
- The tracks are written to allocate no managed memory per frame after warm-up (zero GC).
- Both statements hold in the player and for Absolute and Relative channels. They have two exceptions:
  - From current reads the live value when the clip becomes active. When you scrub backwards, that value depends on
    the order in which the frames ran.
  - In the Editor, TextMeshPro's `SetCharArray` allocates, so the string tween of the TMP Text track allocates there.

## Known limits

- Slider, Scrollbar and Toggle tracks do not raise `onValueChanged` unless you turn on **Send callbacks**, and then
  only in Play Mode. See [tracks-controls.md](Documentation~/tracks-controls.md).
- A Scrollbar track does not scroll a linked ScrollRect. Animate the ScrollRect track instead.
- The TMP Characters track does not support Reverse geometry sorting, and underline and strikethrough stay still.
  See [tmp-character-track.md](Documentation~/tmp-character-track.md).
- Rotation mode Shortest path turns the short way, so a 0 to 360 degree spin is no turn. Use Euler for full turns.
- A Relative slide in a preset can snap when a hide is interrupted by a show.
  See [presets.md](Documentation~/presets.md).

## License

MIT. See [LICENSE.md](LICENSE.md) and [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
