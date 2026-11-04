namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// How a <see cref="TweenChannel{T}"/> interprets its from and to values.
    /// </summary>
    /// <remarks>
    /// The integer values are serialized in user timelines and must never change.
    /// </remarks>
    public enum ValueMode
    {
        /// <summary>From and to are the actual property values.</summary>
        Absolute = 0,

        /// <summary>From and to are offsets added to the value the property had when the track bound it.</summary>
        Relative = 1,

        /// <summary>The tween starts from the value the property has when the clip becomes active.</summary>
        FromCurrent = 2,
    }
}
