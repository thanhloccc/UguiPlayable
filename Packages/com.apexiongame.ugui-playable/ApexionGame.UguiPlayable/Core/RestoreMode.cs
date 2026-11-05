namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// What a tween track does to the properties it animated when its graph is destroyed
    /// (<c>PlayableDirector.Stop()</c>, or the director disabled or destroyed). Pausing keeps the values.
    /// </summary>
    /// <remarks>
    /// Edit-mode preview always reverts, through Timeline's property preview. In Play Mode, Unity runs the stop
    /// destroy callback one update after <c>PlayableDirector.Stop()</c>, so a reverted value is visible from the next frame.
    /// The integer values are serialized in user timelines and must never change.
    /// </remarks>
    public enum RestoreMode
    {
        /// <summary>Leave the last written values in place.</summary>
        Keep = 0,

        /// <summary>Write back the values the properties had when the track bound them.</summary>
        Revert = 1,
    }
}
