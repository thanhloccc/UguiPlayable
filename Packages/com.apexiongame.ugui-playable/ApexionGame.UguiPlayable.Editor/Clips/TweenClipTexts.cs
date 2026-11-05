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
        public const string UNSUPPORTED_BINDING = "The bound object has no value for this channel.";
        public const string ENABLED_TOOLTIP = "Turn this channel on or off.";
        public const string NAME = "Name";
        public const string TRACK_LOCKED = "The track is locked in the Timeline window.";
        public const string RESTORE_MODE = "Restore Mode";
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

        public const string SCROLL_RECT_NO_CONTENT =
            "This ScrollRect has no Content, so there is nothing to scroll. Assign its Content.";

        public static string FormatChannelCount(int enabled, int total)
            => $"{enabled} of {total} channels on";
    }
}
