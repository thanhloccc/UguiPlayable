namespace ApexionGame.UguiPlayable.Tracks.Text
{
    /// <summary>
    /// What one step of a <see cref="TmpTextBehaviour"/> typewriter reveals.
    /// </summary>
    /// <remarks>
    /// The integer values are serialized in user data: never renumber a member; add new members at the end with the
    /// next explicit value.
    /// </remarks>
    public enum TypewriterUnit
    {
        /// <summary>One character at a time.</summary>
        Character = 0,

        /// <summary>
        /// One word at a time, with the spaces and punctuation that follow it. Words are TMP's: runs of letters,
        /// digits and hyphens, so a text with no space between its letters (CJK) can be one word.
        /// </summary>
        Word = 1,

        /// <summary>One line at a time, as TMP laid the text out.</summary>
        Line = 2,
    }
}
