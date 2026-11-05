namespace ApexionGame.UguiPlayable.Tracks.Text
{
    /// <summary>
    /// The point each character of a <see cref="TmpCharacterClip"/> rotates and scales about.
    /// </summary>
    /// <remarks>
    /// The integer values are serialized in user data: never renumber a member; add new members at the end with the
    /// next explicit value.
    /// </remarks>
    public enum CharacterPivot
    {
        /// <summary>The centre of the character's box.</summary>
        Center = 0,

        /// <summary>The middle of the character's baseline.</summary>
        Baseline = 1,
    }
}
