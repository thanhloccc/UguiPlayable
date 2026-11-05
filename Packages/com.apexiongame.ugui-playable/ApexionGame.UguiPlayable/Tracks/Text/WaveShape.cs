namespace ApexionGame.UguiPlayable.Tracks.Text
{
    /// <summary>
    /// The shape of a <see cref="WaveChannel"/>.
    /// </summary>
    /// <remarks>
    /// The integer values are serialized in user timelines and must never change; new shapes are appended.
    /// </remarks>
    public enum WaveShape
    {
        /// <summary>A sine wave, from minus to plus the amplitude.</summary>
        Sine = 0,

        /// <summary>The absolute value of a sine: from 0 to the amplitude, never below.</summary>
        Bounce = 1,
    }
}
