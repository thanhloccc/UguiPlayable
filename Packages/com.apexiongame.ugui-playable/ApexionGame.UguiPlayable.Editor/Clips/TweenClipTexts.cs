namespace ApexionGame.UguiPlayable.Editor.Clips
{
    /// <summary>
    /// The user-facing strings of the tween clip inspector and Timeline hooks.
    /// </summary>
    internal static class TweenClipTexts
    {
        public const string NO_CHANNEL_ENABLED = "This clip animates nothing. Turn on a channel.";
        public const string AXES = "Axes";
        public const string FROM = "From";
        public const string TO = "To";
        public const string VALUE = "Value";
        public const string AT = "At";
        public const string MODE = "Mode";
        public const string SCRAMBLE = "Scramble";
        public const string CHARACTERS = "Chars";
        public const string RATE = "Rate";
        public const string AXIS_TOOLTIP = "Animate this axis. Turn it off to leave the axis unchanged.";
        public const string FROM_CURRENT_NOTE = "Starts from the value the property has when the clip becomes active.";
        public const string RELATIVE_NOTE = "Offsets added to the value the property had when the track bound it.";
        public const string PIPETTE_TOOLTIP = "Set from binding: copy the bound object's current value.";
        public const string PIPETTE_RELATIVE_TOOLTIP = "Relative values are offsets; set them by hand.";
        public const string LINK_TOOLTIP = "Constrain proportions: editing one axis scales the others the same way.";
        public const string STEP_TOOLTIP = "The value is applied once the clip passes this point.";
        public const string NO_TIMELINE = "Open the clip in a Timeline window to use the binding.";
        public const string NO_TRACK = "The clip was not found in the open Timeline.";
        public const string NO_BINDING = "The track has no binding.";
        public const string MIXED_BINDINGS = "The selected clips are bound to different objects.";
        public const string UNSUPPORTED_BINDING = "The bound object has no value for this channel.";
        public const string TEXT_FROM_CURRENT_NOTE = "Starts from the text shown when the clip becomes active.";
        public const string ENABLED_TOOLTIP = "Turn this channel on or off.";
        public const string NAME = "Name";
        public const string TRACK_LOCKED = "The track is locked in the Timeline window.";
        public const string RESTORE_MODE = "Restore Mode";
        public const string SEND_CALLBACKS = "Send callbacks";
        public const string SEND_CALLBACKS_NOTE =
            "Play Mode only. Scrubbing and Revert never call onValueChanged. In Edit Mode, Unity itself calls "
            + "onValueChanged once when the preview ends.";
        public const string SEND_CALLBACKS_SCROLLBAR_NOTE =
            SEND_CALLBACKS_NOTE + " A linked ScrollRect scrolls only through callbacks: animate the ScrollRect track, "
            + "which updates its scrollbars, or turn this on.";
        public const string RESTORE_KEEP_NOTE = "Keep: the last animated value stays when the timeline stops.";
        public const string RESTORE_REVERT_NOTE = "Revert: the bound object returns to its earlier value when stopped.";
        public const string LAYOUT_WARNING =
            "This RectTransform is positioned by a LayoutGroup; animated position (and size, if the group controls it) "
            + "is overwritten on layout rebuild. Animate its size with a Layout Element track, animate a child, or add "
            + "a LayoutElement with Ignore Layout.";

        public const string NEGATIVE_UNSET_NOTE =
            "A negative value means \"not set\": the layout group ignores this size while the value is below 0.";

        public const string SIZE_FITTER_WARNING =
            "This RectTransform's size is driven by a ContentSizeFitter or AspectRatioFitter; animated size is "
            + "overwritten on layout rebuild.";

        public const string GRADIENT_OFF_NOTE =
            "Color Gradient is off on the bound text, so the gradient is not shown. Turn it on in the text's "
            + "Extra Settings.";

        public const string AUTO_SIZE_NOTE =
            "Auto Size is on for the bound text, so TMP sets the font size itself and this channel has no effect. "
            + "Animate Min/Max or turn Auto Size off.";

        public const string AUTO_SIZE_WARNING =
            "Auto Size is on for the bound text, so the Font Size channel has no effect.";

        public const string SCROLL_RECT_NO_CONTENT =
            "This ScrollRect has no Content, so there is nothing to scroll. Assign its Content.";

        public const string STRENGTH = "Strength";
        public const string FREQUENCY = "Frequency";
        public const string ELASTICITY = "Elasticity";
        public const string RANDOMNESS = "Randomness";
        public const string FADE_OUT = "Fade out";
        public const string SEED = "Seed";
        public const string OSCILLATION_AXIS_TOOLTIP = "Add the offset on this axis. Turn it off to leave it alone.";
        public const string FREQUENCY_TOOLTIP = "Oscillations over the clip.";
        public const string ELASTICITY_TOOLTIP = "How long the punch keeps swinging. 0 dies out fast, 1 lasts.";
        public const string RANDOMNESS_TOOLTIP = "0 moves all axes together along the strength, 1 moves each alone.";
        public const string FADE_OUT_TOOLTIP = "Shake less and less until the clip ends.";
        public const string SEED_TOOLTIP = "The same seed always gives the same shake.";
        public const string NEW_SEED_TOOLTIP = "Pick a new random seed.";

        public const string ROTATION_PATH = "Path";
        public const string ROTATION_EULER = "Euler";
        public const string ROTATION_SHORTEST = "Shortest";
        public const string SHORTEST_PATH_NOTE =
            "Turns the short way. Axis toggles are ignored; a full turn becomes no turn.";

        public const string TEXT_RICH_TAGS_NOTE =
            "Rich Text is off on the bound text, so tags show and are revealed as plain characters.";

        public const string TEXT_SCRAMBLE_TOOLTIP =
            "The characters shown at positions that are not revealed yet and differ from the target.";

        public const string TEXT_RATE_TOOLTIP =
            "How many times per second the scramble characters change. 0 changes them only when the reveal advances.";

        public const string TEXT_CUSTOM_TOOLTIP = "The characters used when Scramble is Custom. Empty means no scramble.";

        public static string FormatChannelCount(int enabled, int total)
            => $"{enabled} of {total} channels on";

        /// <summary>
        /// The summary for several clips: "N of K" when every clip has the same count, "N-M of K" otherwise.
        /// </summary>
        public static string FormatChannelCount(int minEnabled, int maxEnabled, int total)
            => minEnabled == maxEnabled
                ? FormatChannelCount(minEnabled, total)
                : $"{minEnabled}\u2013{maxEnabled} of {total} channels on";
    }
}
