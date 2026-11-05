namespace ApexionGame.UguiPlayable.Tracks.Text
{
    /// <summary>
    /// The point each character of a <see cref="TmpCharacterClip"/> rotates and scales about.
    /// </summary>
    /// <remarks>
    /// The integer values are serialized in user timelines and must never change; new pivots are appended.
    /// </remarks>
    public enum CharacterPivot
    {
        /// <summary>The centre of the character's box.</summary>
        Center = 0,

        /// <summary>The middle of the character's baseline.</summary>
        Baseline = 1,
    }
}
