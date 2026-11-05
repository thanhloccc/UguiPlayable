# Troubleshooting

## Text is missing or the sample shows no text (TMP Essential Resources)

The TMP tracks and the sample need TextMeshPro's Essential Resources: the default font asset, the shaders and the
settings. Unity does not import them with the package. Without them, TMP text shows nothing, shows pink, or logs
errors about a missing font or material.

Fix: choose **Window > TextMeshPro > Import TMP Essential Resources** and import everything. Do this once per project.
The sample's font (Liberation Sans SDF) comes from this import. After the import, reopen the scene.

## Nothing happens in Play Mode

Check these in order:

1. The track has a **binding**. A track with no binding does nothing.
2. The clip is not muted and the track is not muted.
3. The clip has at least one channel turned on. The clip card says "This clip animates nothing. Turn on a channel." when
   none is. Most channels are off in a new clip.
4. The `PlayableDirector` has **Play On Awake** on, or something calls `Play()`.
5. Another script or animation writes the same property after the timeline each frame.
6. The target is driven by a layout group or a fitter. See below.

## An element's position or size snaps back

A `LayoutGroup`, `ContentSizeFitter` or `AspectRatioFitter` owns position and size and overwrites them when the layout
rebuilds. The Rect Transform track shows a warning. Animate the size with a Layout Element track, animate a child, or
add a `LayoutElement` with **Ignore Layout**. See [tracks-groups-and-layout.md](tracks-groups-and-layout.md).

## Values stay after the timeline stops

The track's **Restore Mode** is Keep by default. Keep leaves the last written values. Set the track to **Revert** to
return the properties to their base values. In Play Mode the revert shows from the frame after `Stop()`. See
[value-modes-and-restore.md](value-modes-and-restore.md). The TMP Characters track always restores.

## An animation drifts each time it plays

A **Relative** channel is measured from the base value captured when the graph was built. If the timeline does not
return to its start, the element ends in a new place on every replay. Use Absolute targets or From current for show and
hide transitions, or set the hide track to Revert. See [presets.md](presets.md).

## A From current clip gives a different result after scrubbing

From current reads the live value when the clip becomes active. When you scrub backwards or jump, the live value
depends on what ran before. Use Absolute for repeatable scrubbing. See
[value-modes-and-restore.md](value-modes-and-restore.md).

## A rotation does not turn

With Rotation Path = **Shortest**, a 0 to 360 degree spin is no turn, because both ends are the same orientation. Use
**Euler** for spins and for turns of more than 180 degrees. See [tracks-transform.md](tracks-transform.md).

## `onValueChanged` is not called

Slider, Scrollbar and Toggle tracks do not raise `onValueChanged` unless you turn on **Send callbacks** on the track.
That works in Play Mode only. Scrubbing in Edit Mode and Revert never call it. In Edit Mode, Unity itself calls
`onValueChanged` once when the preview ends. See [tracks-controls.md](tracks-controls.md).

## A Scrollbar track does not scroll my content

A Scrollbar track no longer scrolls a linked ScrollRect. Animate the **Scroll Rect** track instead, or turn on **Send
callbacks** on the Scrollbar track (Play Mode only). The Scroll Rect track needs a **Content** assigned.

## A Toggle in a group changes other toggles in Edit Mode

In Edit Mode, Unity itself calls `onValueChanged` once per toggle of the group when the preview ends. The package
registers the whole group so that the group is left as it was. With `allowSwitchOff` off, the group turns the last
toggle back on. See [tracks-controls.md](tracks-controls.md).

## The gradient does not show

The Color Gradient channel writes the gradient only. TMP shows it when **Color Gradient** is on in the text's **Extra
Settings**. Turn it on. See [tracks-text.md](tracks-text.md).

## The character track does nothing or looks wrong

- **Reverse geometry sorting.** The track writes nothing on a text with Geometry Sorting = Reverse. Change the sorting.
- **Two character tracks on one text.** Use one. The second would animate the first one's result.
- **Underline and strikethrough** stay still. This is a known limit.
- **Soft edges** at scales well above 1. TMP keeps each glyph's SDF scale.
- **Nothing visible.** The default clip fades alpha 0 to 1, and hidden characters are skipped. Check the text has
  visible characters and that a TMP Text typewriter has revealed them.

See [tmp-character-track.md](tmp-character-track.md).

## A preset leaves tracks unbound

The preset binds by path from the **binding root**. A missing path or component gives a warning and leaves that track
unbound. Check the hierarchy against the preset's paths, change the binding root, or bind by hand. With no director
(a timeline asset, or a sub-timeline) the tracks are always added unbound. See [presets.md](presets.md).

## Adding a preset changed other objects

Several directors play the same timeline asset. Use **Duplicate and add** in the picker or the browser, so the preset
goes into a copy that only this director plays. See [presets.md](presets.md).

## The Editor allocates memory while I scrub text

TextMeshPro's `SetCharArray` allocates in the Editor. The string tween of the TMP Text track uses it. The zero-GC
statement holds in the player. See [value-modes-and-restore.md](value-modes-and-restore.md).

## Console: DrivenPropertyManager error

A track registered a property name that does not exist on the bound component. For a custom track, check the names your
behaviour returns from `CollectPropertyNames`. See [custom-tracks.md](custom-tracks.md).

## The sample does not react to clicks

The sample adds its UI input module at runtime, so it works with the Input System package or the old Input Manager. If
clicks still do nothing, check that the scene has an `EventSystem` and that **Active Input Handling** in Player Settings
is not set to a mode your project has no package for.

## Still stuck

Report it in the repository at <https://github.com/thanhloccc/UguiPlayable> with the Unity version, the package version
and the console log.
