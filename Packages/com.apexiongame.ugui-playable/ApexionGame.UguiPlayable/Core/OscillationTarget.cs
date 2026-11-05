namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// The property an <see cref="OscillationChannel"/> or a <c>WaveChannel</c> adds its offset to; the channel's docs
    /// give the units.
    /// </summary>
    /// <remarks>
    /// The integer values are serialized in user data: never renumber a member; add new members at the end with the
    /// next explicit value.
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
