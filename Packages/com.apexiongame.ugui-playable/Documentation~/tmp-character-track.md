# TMP Characters track

Menu: **UGUI Playable > Text > TMP Characters**. Binds to a `TMP_Text`. It works on `TextMeshProUGUI` and on the
world-space `TextMeshPro`.

The track animates each character of a text on its own: offset, rotation, scale, alpha and tint, with a stagger that
spreads the starts, plus a wave and a per-character shake. The default clip is a per-character fade-in.

TextMeshPro needs its Essential Resources in the project. See [troubleshooting.md](troubleshooting.md).

## How it works

The track writes the text mesh, never a serialized property. It re-applies its effect whenever TMP rebuilds the text.
Because of that, a [TMP Text track](tracks-text.md) (colour, typewriter, string tween) on the same text keeps working
together with it. Hidden characters, such as those a typewriter has not revealed, are skipped.

Each character gets its own progress:

```text
p_i = clamp01((p - s * u) / (1 - s))
```

with `p` the clip progress, `s` the stagger and `u` the character's place in the order, from 0 to 1. The result is then
eased by the clip's ease. With a stagger of 0, all characters animate together.

## Clip settings

| Setting | Default | Meaning |
|---|---|---|
| Range Start | 0 | The first animated character, as an index in TMP's character info. Rich-text tags are not counted |
| Range Length | 0 | The number of animated characters. 0 means to the end of the text |
| Stagger | 0.5 | The share of the clip over which the characters' starts are spread. 0 to 0.99 |
| Order | Forward | Forward, Backward, Centre out, Edges in or Random |
| Order Seed | 0 | The seed of the Random order. The same seed gives the same order |
| Ease | Out Cubic | The ease applied to each character's progress |
| Pivot | Center | The point each character rotates and scales about: Center (box centre) or Baseline (middle of the baseline) |

The order counts only the visible characters of the range. Spaces are skipped. For Centre out and Edges in, mirrored
characters share a rank.

## Transitions

A transition has a From and a To. Both are relative to the character's rest pose (offset 0, rotation 0, scale 1,
alpha 1, tint white), so a transition has **no value mode**. A transition blends toward the rest pose and **holds**
its To value after the clip ends.

| Channel | Type | Default From | Default To | Default state |
|---|---|---|---|---|
| Offset | Vector2 (text units) | (0, -30) | (0, 0) | off |
| Rotation | float (degrees, about Z) | 90 | 0 | off |
| Scale | Vector2 | (0, 0) | (1, 1) | off |
| Alpha | float (multiplier) | 0 | 1 | **on** |
| Color (tint) | Color (multiplies the vertex colours) | white | white | off |

Only Alpha is on in a new clip. Scale has a link button (constrain proportions); it is an editor aid and the runtime
ignores it.

## Wave

A wave adds an offset that travels through the characters. It is added on top of the transitions and has **no hold**:
the text rests after the clip. The phase of a character is `cycles * p - phasePerCharacter * rank`, with `rank` the
character's place in the clip's order.

| Setting | Default | Meaning |
|---|---|---|
| State | off | |
| Target | Position | Position (x and y of the amplitude, in text units), Rotation (z, in degrees) or Scale (x and y, added to the scale) |
| Shape | Sine | Sine (from minus to plus the amplitude) or Bounce (absolute value of a sine: 0 to the amplitude) |
| Amplitude | (0, 10, 0) | The peak offset per axis |
| Cycles | 2 | The number of wave cycles over the clip |
| Phase per character | 0.1 | The phase delay, in cycles, from one character to the next |

## Shake

A per-character shake adds an offset with no hold. Each character shakes with its own noise.

| Setting | Default |
|---|---|
| State | off |
| Target | Position |
| Strength | (3, 3, 0) |
| Frequency | 10 |
| Randomness | 0.5 |
| Fade out | on |
| Seed | 0 |
| Ignored axes | none |

## Restore

The track **always** restores the text when the timeline stops, whatever a Restore Mode would say. A mesh effect cannot
outlive the graph, because the next rebuild of the text would wipe it. The track inspector has no Restore Mode.

## Limits

- **One character track per text.** A second track would animate the first track's result. The track shows a warning.
- **Reverse geometry sorting is not supported.** If the bound text uses Geometry Sorting = Reverse (in Extra Settings),
  the track writes nothing and shows a warning.
- **Underline and strikethrough stay still.** They do not follow the characters.
- **Large scales soften the edges.** TMP keeps each glyph's SDF scale, so scales well above 1 make SDF edges soft. The
  clip card shows a warning for this.

## Presets

Starter presets for this track: `text-char-fade-in`, `text-char-bounce-in` and `text-wave`. See
[presets.md](presets.md).

Next: [multi-clip-editing.md](multi-clip-editing.md).
