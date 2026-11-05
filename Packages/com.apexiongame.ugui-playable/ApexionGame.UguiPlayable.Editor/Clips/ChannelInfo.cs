namespace ApexionGame.UguiPlayable.Editor.Clips
{
    /// <summary>
    /// The two channel layouts a tween clip's template can hold.
    /// </summary>
    internal enum ChannelShape
    {
        /// <summary>A <c>TweenChannel&lt;T&gt;</c>: enabled, mode, from, to, ease, ignored axes.</summary>
        Tween,

        /// <summary>A <c>StepChannel</c>: enabled, value, threshold.</summary>
        Step,
    }

    /// <summary>
    /// The value type of a tween channel's from/to fields.
    /// </summary>
    internal enum ChannelValueKind
    {
        Float,
        Vector2,
        Vector3,
        Vector4,
        Color,

        /// <summary>A <c>TMPro.VertexGradient</c>: four corner colors.</summary>
        VertexGradient,
    }

    /// <summary>
    /// One channel found under a clip's <c>_template</c>.
    /// </summary>
    /// <param name="Name">The serialized field name of the channel, for example <c>_anchoredPosition</c>.</param>
    /// <param name="Shape">Whether the channel is a tween or a step channel.</param>
    /// <param name="ValueKind">The from/to value type. Unused for a step channel.</param>
    /// <param name="HasConstrain">Whether the template has <c>&lt;Name&gt;ConstrainFrom/To</c> sibling bools.</param>
    internal readonly record struct ChannelInfo(
          string Name
        , ChannelShape Shape
        , ChannelValueKind ValueKind
        , bool HasConstrain
    );
}
