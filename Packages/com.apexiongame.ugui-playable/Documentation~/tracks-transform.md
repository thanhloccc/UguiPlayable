# Rect Transform track

Menu: **UGUI Playable > Transforms > Rect Transform**. Binds to a `RectTransform`. Use it to move, resize, rotate and
scale UI elements, and to punch or shake them.

All channels are **off** by default. A new clip animates nothing until you turn a channel on. Every tweened channel
below starts in **Absolute** mode with a **Linear** ease, and with no axis ignored. See
[value-modes-and-restore.md](value-modes-and-restore.md).

## Channels

| Channel | Type | Default From | Default To | Default state |
|---|---|---|---|---|
| Anchored Position | Vector3 (x, y, z) | (0, 0, 0) | (0, 0, 0) | off |
| Size Delta | Vector2 | (100, 100) | (100, 100) | off |
| Rotation | Vector3 (Euler angles, degrees) | (0, 0, 0) | (0, 0, 0) | off |
| Scale | Vector3 | (1, 1, 1) | (1, 1, 1) | off |
| Pivot | Vector2 | (0.5, 0.5) | (0.5, 0.5) | off |
| Anchor Min | Vector2 | (0.5, 0.5) | (0.5, 0.5) | off |
| Anchor Max | Vector2 | (0.5, 0.5) | (0.5, 0.5) | off |
| Punch | oscillation channel | see below | see below | off |
| Shake | oscillation channel | see below | see below | off |

Notes:

- **Anchored Position** includes the Z axis (`anchoredPosition3D`).
- **Size Delta** is the `sizeDelta` of the rect, not the final size. With stretched anchors it is an offset.
- **Size Delta** and **Scale** have a **link** button (constrain proportions). While it is on, editing one axis scales
  the others by the same ratio. It is an editor aid; the runtime ignores it.
- **Pivot**, **Anchor Min** and **Anchor Max** change what the size and position mean. The track writes, in this
  order: pivot, anchors, size, position, rotation, scale.
- Vector channels blend per axis, so clips that animate different axes of one property do not disturb each other.
- In **Relative** mode, From and To are offsets added to the base value. In **From current** mode, only To is used.

## Rotation mode

The Rotation channel has a **Path** setting.

| Path | Behaviour |
|---|---|
| Euler (default) | Blends each Euler angle on its own, in degrees. 0 to 360 is a full turn. |
| Shortest | Turns the short way between two orientations (quaternion path). The axis toggles are ignored. |

Limit: with **Shortest**, a full turn (0 to 360 degrees) is no turn, because both ends are the same orientation. Use
Euler for spins and for turns of more than 180 degrees.

Do not mix the two modes on one track. When clips with different modes meet, the track picks a path by rule: the
quaternion path runs when a Shortest clip has weight; when nothing has weight, the path follows the clip that finished
last. An Euler clip that eases in after a Shortest clip can jump at the start of the ease.

## Punch and shake

Punch and shake are offsets added on top of the other channels. They return to the starting value, so they suit
attention effects and button feedback. They need no From or To.

| Setting | Punch default | Shake default | Meaning |
|---|---|---|---|
| Target | Scale | Position | What moves: Position, Rotation or Scale |
| Strength | (0.2, 0.2, 0) | (10, 10, 0) | The peak offset per axis |
| Frequency | 4 | 10 | Oscillations over the clip |
| Elasticity | 0.5 | 0.5 | How long a punch keeps swinging. 0 dies out fast, 1 lasts |
| Randomness | 0.5 | 0.5 | 0 moves all axes together along the strength, 1 moves each alone |
| Fade out | on | on | Shake less and less until the clip ends |
| Seed | 0 | 0 | The same seed always gives the same shake |
| Ignored axes | none | none | Axes the offset leaves alone |

The units depend on the target: position in canvas units, rotation in degrees, scale added to the scale. A punch is
exactly 0 at both ends of the clip. The Elasticity setting is used by punch; Randomness, Fade out and Seed by shake.

## Warnings in the track and the clip

The editor warns when it can see a problem:

- "This RectTransform is positioned by a LayoutGroup": animated position (and size, if the group controls it) is
  overwritten when the layout rebuilds. Animate its size with a Layout Element track, animate a child, or add a
  `LayoutElement` with Ignore Layout.
- "This RectTransform's size is driven by a ContentSizeFitter or AspectRatioFitter": animated size is overwritten when
  the layout rebuilds.

## Restore

With **Revert**, the track writes back only the properties and axes it wrote. See
[value-modes-and-restore.md](value-modes-and-restore.md).

Next: [tracks-graphics.md](tracks-graphics.md).
