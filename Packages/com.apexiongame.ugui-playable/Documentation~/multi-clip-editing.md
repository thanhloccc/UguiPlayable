# Multi-clip editing

You can select several clips of one type in the Timeline window and edit them together in one clip card. All clip
types of this package support it, including the Rotation path row, the Toggle track and the TMP Characters cards.

## How to use it

1. In the Timeline window, select two or more clips of the same type (Ctrl or Shift click, or drag a box).
2. The inspector shows one clip card for all of them.
3. Change a value. The edit applies to every selected clip.
4. Press Undo once to revert the edit on all of them.

## Mixed values

When the selected clips have different values in a field, the field shows the mixed dash. A mixed field does not change
anything until you edit it.

An edit writes **only the field you changed**:

- For a vector, only the edited axis is written. Each clip keeps its other axes.
- For the Constrain proportions link, the constrained scaling is applied clip by clip, and the result is still one
  Undo step.

The other controls handle mixed states too:

- **Axis buttons** show a mixed look when the clips differ.
- **Mode controls** (Absolute, Relative, From current) and other segmented controls show a mixed state when the clips
  differ.
- **Switches** show a mixed state.
- The **summary** shows a range when the number of enabled channels differs, for example "1-3 of 9 channels on".
- The **seed** button gives each clip its own seed.

## Set from binding

The pipette button (**Set from binding**) copies the bound object's current value. With several clips it works when
every clip's track is bound to the same object. Otherwise it is disabled and the tooltip says "The selected clips are
bound to different objects."

## Notes

- While several clips are inspected, the cards check their state every 250 ms. Editing one clip does not poll.
- The mixed link shows the unlinked icon.
- The soft-scale warning of the TMP Characters card reads the first clip only.
- Presets work on several clips too: **Apply Preset…** copies a preset's values into every selected clip in one Undo
  step. See [presets.md](presets.md).

Next: [presets.md](presets.md).
