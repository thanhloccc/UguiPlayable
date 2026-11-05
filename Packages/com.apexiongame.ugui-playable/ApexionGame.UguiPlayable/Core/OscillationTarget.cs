namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// The property an <see cref="OscillationChannel"/> adds its offset to.
    /// </summary>
    /// <remarks>
    /// The integer values are serialized in user timelines and must never change.
    /// </remarks>
    public enum OscillationTarget
    {
        /// <summary>The anchored position.</summary>
        Position = 0,

        /// <summary>The rotation, in euler degrees.</summary>
        Rotation = 1,

        /// <summary>The local scale.</summary>
        Scale = 2,
    }
}
