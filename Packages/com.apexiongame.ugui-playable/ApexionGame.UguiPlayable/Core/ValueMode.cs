namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// How a <see cref="TweenChannel{T}"/> interprets its from and to values.
    /// </summary>
    /// <remarks>
    /// The integer values are serialized in user data: never renumber a member; add new members at the end with the
    /// next explicit value.
    /// </remarks>
    public enum ValueMode
    {
        /// <summary>From and to are the actual property values.</summary>
        Absolute = 0,

        /// <summary>From and to are offsets added to the value the property had when the track bound it.</summary>
        /// <remarks>
        /// The base is captured again whenever the director's graph is rebuilt, for example after
        /// <c>PlayableDirector.Stop()</c>. A timeline that does not return to its start drifts each time it is
        /// replayed, so for show/hide transitions prefer Absolute targets or <see cref="FromCurrent"/>.
        /// </remarks>
        Relative = 1,

        /// <summary>The tween starts from the value the property has when the clip becomes active.</summary>
        FromCurrent = 2,
    }
}
