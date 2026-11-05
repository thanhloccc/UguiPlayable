# Control tracks: Slider, Scrollbar, Scroll Rect and Toggle

These tracks animate interactive uGUI controls. All tweened channels default to **Absolute** mode and a **Linear**
ease. See [value-modes-and-restore.md](value-modes-and-restore.md).

## Callback rules (read this first)

Slider, Scrollbar and Toggle have an `onValueChanged` event. The tracks treat it the same way:

- **Send callbacks** is a switch on the track. It is **off by default**.
- With it off, the track writes with `SetValueWithoutNotify`. Nothing listens to the animation.
- With it on, the track raises `onValueChanged` when it writes. This works in **Play Mode only**.
- Scrubbing in Edit Mode and Revert never call `onValueChanged` on the track's behalf.
- In Edit Mode, Unity itself calls `onValueChanged` once when the preview ends (once per toggle for a Toggle in a
  group). The track cannot prevent that.

This is a change from earlier work: Slider and Scrollbar tracks no longer raise `onValueChanged` by default.

If your game logic reads `onValueChanged` to follow an animated slider, turn **Send callbacks** on.

## Slider track

Menu: **UGUI Playable > Controls > Slider**. Binds to a `Slider`.

| Channel | Type | Default From | Default To | Default state |
|---|---|---|---|---|
| Value | float | 0 | 1 | **on** |

Track setting: **Send callbacks** (default off). The value is in the slider's own range (its min and max).

## Scrollbar track

Menu: **UGUI Playable > Controls > Scrollbar**. Binds to a `Scrollbar`.

| Channel | Type | Default From | Default To | Default state |
|---|---|---|---|---|
| Value | float | 0 | 1 | **on** |
| Size | float | 0.2 | 1 | off |

Track setting: **Send callbacks** (default off).

**Limit:** a Scrollbar track no longer scrolls a ScrollRect that is linked to the scrollbar. A linked ScrollRect scrolls
only through the scrollbar's callbacks. To scroll content you have two options:

- animate the **Scroll Rect** track (below), which updates its scrollbars; or
- turn **Send callbacks** on, in Play Mode.

## Scroll Rect track

Menu: **UGUI Playable > Controls > Scroll Rect**. Binds to a `ScrollRect`.

| Channel | Type | Default From | Default To | Default state |
|---|---|---|---|---|
| Normalized Position | Vector2 (x, y) | (0, 1) | (0, 0) | **on** |

X is horizontal (0 left, 1 right). Y is vertical (0 bottom, 1 top). The default scrolls from the top to the bottom.
Turn an axis off to scroll one axis only; the other axis keeps its current position.

Notes:

- The ScrollRect needs a **Content**. Without one the editor shows "This ScrollRect has no Content, so there is
  nothing to scroll. Assign its Content." and the track leaves the ScrollRect alone.
- Each write sets `normalizedPosition` and then clears the velocity, so inertia does not fight the animation.
- The normalized position is not serialized. For the Edit Mode preview, the track registers the content's anchored
  position and the value and size of the scrollbars, which the ScrollRect writes.
- The base position is captured when the track binds the ScrollRect, which can be before the content has a layout.
  The track captures it again while it has not written yet and the content size has changed.
- With Revert, only the axes the track wrote are restored.

## Toggle track

Menu: **UGUI Playable > Controls > Toggle**. Binds to a `Toggle`.

| Channel | Type | Default | Default state |
|---|---|---|---|
| Is On | step (On/Off) | value On, threshold 0.5 | **on** |

Track setting: **Send callbacks** (default off).

Is On is a step channel: the value applies once the clip passes its threshold. Put two clips on the track to switch a
toggle on and then off again.

Toggle groups:

- For a toggle in a `ToggleGroup`, the Edit Mode preview also registers `isOn` of every toggle of the group in the
  scene, so the group is left as it was when the preview ends.
- Revert restores the checkmark too: the checkmark graphic matches the restored state.
- With `allowSwitchOff` off, the group turns the last toggle back on when it is switched off.
- The editor warns "No checkmark graphic: nothing changes visually." when the toggle has no checkmark graphic.

Next: [tracks-text.md](tracks-text.md).
