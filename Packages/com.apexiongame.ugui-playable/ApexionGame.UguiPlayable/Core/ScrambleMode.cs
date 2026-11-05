namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// Which characters a <see cref="TextChannel"/> shows at positions that are not revealed yet.
    /// </summary>
    /// <remarks>
    /// The integer values are serialized in user timelines and must never change.
    /// </remarks>
    public enum ScrambleMode
    {
        /// <summary>No scrambling: an unrevealed position keeps its <c>from</c> character.</summary>
        None = 0,

        /// <summary>Letters (upper and lower case) and digits.</summary>
        All = 1,

        /// <summary>The letters A to Z.</summary>
        Uppercase = 2,

        /// <summary>The letters a to z.</summary>
        Lowercase = 3,

        /// <summary>The digits 0 to 9.</summary>
        Numerals = 4,

        /// <summary>The characters of <see cref="TextChannel.CustomScrambleChars"/>.</summary>
        Custom = 5,
    }
}
