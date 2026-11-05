# Canvas Group and layout tracks

These tracks animate a `CanvasGroup` and the uGUI layout components. All tweened channels default to **Absolute** mode
and a **Linear** ease. See [value-modes-and-restore.md](value-modes-and-restore.md).

## Canvas Group track

Menu: **UGUI Playable > Groups > Canvas Group**. Binds to a `CanvasGroup`. It is the simplest way to fade a panel and
all of its children, and to turn input on and off while it fades.

| Channel | Type | Default | Default state |
|---|---|---|---|
| Alpha | float, From 0 To 1 | 0 to 1 | **on** |
| Interactable | step (On/Off) | value On, threshold 0.5 | off |
| Blocks Raycasts | step (On/Off) | value On, threshold 0.5 | off |

Interactable and Blocks Raycasts are **step channels**. Their value applies once the clip passes the threshold (0 to
1). Use them to make a popup clickable half-way through its fade-in, or to stop it taking clicks when a fade-out
starts.

## Layout Element track

Menu: **UGUI Playable > Layout > Layout Element**. Binds to a `LayoutElement`. Use it to animate the size a layout
group gives to one child.

| Channel | Type | Default From | Default To | Default state |
|---|---|---|---|---|
| Min Width | float | 0 | 100 | off |
| Min Height | float | 0 | 100 | off |
| Preferred Width | float | 0 | 100 | off |
| Preferred Height | float | 0 | 100 | off |
| Flexible Width | float | 0 | 1 | off |
| Flexible Height | float | 0 | 1 | off |

Notes:

- A negative value (-1 in a fresh `LayoutElement`) means "not set" to uGUI, and the layout group ignores that size. A
  channel that tweens through a negative value switches the size off for those frames. Keep both ends at 0 or above
  unless that is what you want.
- The flexible sizes are weights, not pixels. A flexible width of 1 asks the group to share the free space.

## Layout Group track

Menu: **UGUI Playable > Layout > Layout Group**. Binds to a `HorizontalOrVerticalLayoutGroup` (a Horizontal Layout
Group or a Vertical Layout Group).

| Channel | Type | Default From | Default To | Default state |
|---|---|---|---|---|
| Spacing | float | 0 | 20 | **on** |
| Padding | Vector4 (left, right, top, bottom) | (0, 0, 0, 0) | (10, 10, 10, 10) | off |

## Grid Layout Group track

Menu: **UGUI Playable > Layout > Grid Layout Group**. Binds to a `GridLayoutGroup`.

| Channel | Type | Default From | Default To | Default state |
|---|---|---|---|---|
| Spacing | Vector2 | (0, 0) | (10, 10) | **on** |
| Cell Size | Vector2 | (100, 100) | (120, 120) | off |
| Padding | Vector4 (left, right, top, bottom) | (0, 0, 0, 0) | (10, 10, 10, 10) | off |

## Padding

The Padding channel has the axes L, R, T and B. The value is rounded to whole pixels when it is written. The track
sets the four values on the group's existing padding object and rebuilds the layout only when a value changed.

## Animating things inside a layout group

A layout group owns the position and size of its children. If you animate a child's `RectTransform` position, the
group overwrites it when the layout rebuilds. The Rect Transform track shows a warning for this. Your options:

- animate the child's size with a Layout Element track;
- animate a child of the child;
- give the child a `LayoutElement` with **Ignore Layout** turned on.

See [tracks-transform.md](tracks-transform.md).

Next: [tracks-controls.md](tracks-controls.md).
