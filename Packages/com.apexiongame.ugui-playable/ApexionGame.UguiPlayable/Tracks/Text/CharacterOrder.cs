namespace ApexionGame.UguiPlayable.Tracks.Text
{
    /// <summary>
    /// The order in which the characters of a <see cref="TmpCharacterClip"/> start, when the clip has a stagger.
    /// </summary>
    /// <remarks>
    /// The integer values are serialized in user timelines and must never change; new orders are appended.
    /// </remarks>
    public enum CharacterOrder
    {
        /// <summary>The first character starts first.</summary>
        Forward = 0,

        /// <summary>The last character starts first.</summary>
        Backward = 1,

        /// <summary>The middle characters start first, the outer ones last.</summary>
        CenterOut = 2,

        /// <summary>The outer characters start first, the middle ones last.</summary>
        EdgesIn = 3,

        /// <summary>A shuffled order, the same for the same seed.</summary>
        Random = 4,
    }
}
