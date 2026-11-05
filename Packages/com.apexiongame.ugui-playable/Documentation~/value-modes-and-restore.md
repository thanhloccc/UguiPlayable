# Value modes and Restore Mode

Two settings decide what a track does to the value it animates: the **value mode** of each channel, and the
**Restore Mode** of the track.

## Value modes

Every tweened channel has a mode. The default is **Absolute**.

| Mode | From and To mean | Start value |
|---|---|---|
| Absolute | The real property values | `From` |
| Relative | Offsets added to the base value | base + `From` |
| From current | Only `To` is used | The value the property has when the clip becomes active |

The **base value** is the value the property had when the track bound it. The mixer captures it again whenever the
director's graph is rebuilt, for example after `PlayableDirector.Stop()`.

### Absolute

Use it when you know the values. A fade from 0 to 1 is Absolute. The result is the same every time.

### Relative

Use it for a move or a size change that should work wherever the element sits. A Relative position of `0` to `100`
moves the element 100 units from where it was.

Relative has one trap. The base is captured again each time the graph is rebuilt. A timeline that does not return to its
start drifts every time it plays again. For show and hide transitions that several directors share, prefer Absolute
targets or From current.

### From current

Use it for hides and for "go to X from wherever you are". The clip reads the live property value on the first frame it
has weight, and tweens from there to `To`. A popup that is half-faded can still fade out smoothly.

From current depends on what ran before. If you scrub backwards, or jump into the middle of a timeline, the live value
comes from whatever the earlier frames wrote, so the result can differ from a straight play-through.

### Per-axis control

A vector channel (position, scale, anchors and so on) has one toggle per axis. An axis you turn off is not written, and
a restore leaves it alone. The track blends clips per axis, so two clips may animate different axes of one property.

### Value modes and text

The string tween of the TMP Text track supports Absolute and From current. Relative has no meaning for text and behaves
like Absolute. See [tracks-text.md](tracks-text.md).

### Rotation

Rotation has its own **Path** setting: Euler (default) or Shortest path. Shortest path turns the short way, so a
0 to 360 degree spin is no turn. See [tracks-transform.md](tracks-transform.md).

### Step channels

Interactable, Blocks Raycasts and Is On are **step channels**. They have a value and a threshold from 0 to 1 (default
0.5). The value applies once the clip passes the threshold. They have no value mode and no ease.

## Blending and holding

- Overlapping clips blend by weight, channel by channel.
- When a clip ends, its end value is held until a later clip writes the same channel.
- A channel that no clip touches is never written.
- Punch, shake and wave channels are offsets with no hold. A punch is exactly 0 at both ends of the clip. A shake
  returns to 0 at the end when **Fade out** is on.

## Restore Mode

Restore Mode is a setting on the track. It decides what happens when the graph is destroyed:
`PlayableDirector.Stop()`, or the director is disabled or destroyed.

| Mode | Behaviour |
|---|---|
| Keep (default) | The last written values stay |
| Revert | The properties go back to their base values |

Details:

- Pausing the director keeps the values and the base values. Nothing is restored, and Relative does not drift on resume.
- In Play Mode, Unity runs the destroy callback one update after `Stop()`, so a reverted value shows from the next
  frame.
- Edit Mode preview always reverts, through Timeline's property preview, whatever Restore Mode says.
- Revert writes back only the properties and axes the track wrote.
- The TMP Characters track ignores Restore Mode. It always restores the text mesh when the timeline stops. Its
  inspector has no Restore Mode setting. See [tmp-character-track.md](tmp-character-track.md).
- Slider, Scrollbar and Toggle revert without calling `onValueChanged`.
  See [tracks-controls.md](tracks-controls.md).

When to use Revert: on hide tracks that use a Relative offset, so the element returns to rest when the graph stops.
That is what the starter slide presets do. See [presets.md](presets.md).

## Determinism and allocation

- Scrubbing a clip gives the same result every time. The punch and shake math uses a seed and no random generator.
- The tracks are written to allocate no managed memory per frame after warm-up (zero GC).
- Both statements hold in the player and for Absolute and Relative channels.
- Exceptions:
  - From current depends on the order of evaluation when you scrub backwards (see above).
  - In the Editor, TextMeshPro's `SetCharArray` allocates. The string tween of the TMP Text track calls it.

Next: [tracks-transform.md](tracks-transform.md).
