using System;
using UnityEngine;
using ApexionGame.UguiPlayable.Easing;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// A string property that changes from <see cref="From"/> to <see cref="To"/> character by character over a
    /// clip, with optional scrambled characters at the positions not revealed yet.
    /// </summary>
    /// <remarks>
    /// Text is not blended: the active input with the highest weight decides. Only <see cref="ValueMode.Absolute"/>
    /// and <see cref="ValueMode.FromCurrent"/> are meaningful; <see cref="ValueMode.Relative"/> behaves like
    /// Absolute. Evaluate a channel with <see cref="TextTweenMath"/>. The serialized field names are user data.
    /// </remarks>
    [Serializable]
    public struct TextChannel
    {
        [SerializeField]
        private bool _enabled;

        [SerializeField]
        private ValueMode _mode;

        [SerializeField]
        private string _from;

        [SerializeField]
        private string _to;

        [SerializeField]
        private EaseSetting _ease;

        [SerializeField]
        private ScrambleMode _scramble;

        [SerializeField]
        private string _customScrambleChars;

        [NonSerialized]
        private string _captured;

        [NonSerialized]
        private bool _hasCaptured;

        /// <summary>
        /// Creates a channel with a linear ease and no scrambling.
        /// </summary>
        public TextChannel(string from, string to, ValueMode mode = ValueMode.Absolute, bool enabled = true)
        {
            _enabled = enabled;
            _mode = mode;
            _from = from ?? string.Empty;
            _to = to ?? string.Empty;
            _ease = EaseSetting.Default;
            _scramble = ScrambleMode.None;
            _customScrambleChars = string.Empty;
            _captured = null;
            _hasCaptured = false;
        }

        /// <summary>
        /// Creates a channel with a linear ease and a scramble.
        /// </summary>
        public TextChannel(
              string from
            , string to
            , ScrambleMode scramble
            , string customScrambleChars = ""
            , ValueMode mode = ValueMode.Absolute
            , bool enabled = true
        )
            : this(from, to, mode, enabled)
        {
            _scramble = scramble;
            _customScrambleChars = customScrambleChars ?? string.Empty;
        }

        /// <summary>Whether the channel drives the text.</summary>
        public bool Enabled => _enabled;

        /// <summary>How <see cref="From"/> is interpreted. Relative behaves like Absolute.</summary>
        public ValueMode Mode => _mode;

        /// <summary>The start text. Ignored in <see cref="ValueMode.FromCurrent"/>.</summary>
        public string From => _from;

        /// <summary>The end text.</summary>
        public string To => _to;

        /// <summary>The ease applied to the clip's progress before the reveal.</summary>
        public EaseSetting Ease => _ease;

        /// <summary>The characters shown at unrevealed positions.</summary>
        public ScrambleMode Scramble => _scramble;

        /// <summary>
        /// The scramble characters, used only when <see cref="Scramble"/> is <see cref="ScrambleMode.Custom"/>.
        /// </summary>
        public string CustomScrambleChars => _customScrambleChars;

        /// <summary>Whether <see cref="Captured"/> holds the text read when the clip became active.</summary>
        public bool HasCaptured => _hasCaptured;

        /// <summary>The text read when the clip became active, used by <see cref="ValueMode.FromCurrent"/>.</summary>
        public string Captured => _captured;

        /// <summary>
        /// Stores the property's current text as the start of a <see cref="ValueMode.FromCurrent"/> tween.
        /// </summary>
        public void Capture(string current)
        {
            _captured = current;
            _hasCaptured = true;
        }

        /// <summary>
        /// Forgets the captured text, so the next activation captures again.
        /// </summary>
        public void ClearCapture()
        {
            _captured = null;
            _hasCaptured = false;
        }
    }
}
