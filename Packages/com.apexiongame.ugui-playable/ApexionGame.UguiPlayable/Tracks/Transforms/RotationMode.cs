namespace ApexionGame.UguiPlayable.Tracks.Transforms
{
    /// <summary>
    /// How a <see cref="RectTransformClip"/> turns from its from rotation to its to rotation.
    /// </summary>
    /// <remarks>
    /// The integer values are serialized in user data: never renumber a member; add new members at the end with the
    /// next explicit value. Don't mix modes on one track.
    /// </remarks>
    public enum RotationMode
    {
        /// <summary>Blends each euler angle on its own, in degrees, so 0 → 360° is a full turn.</summary>
        Euler = 0,

        /// <summary>
        /// Turns the short way between two orientations; the axis toggles are ignored; a full turn (0 → 360°) is no
        /// turn.
        /// </summary>
        ShortestPath = 1,
    }
}
