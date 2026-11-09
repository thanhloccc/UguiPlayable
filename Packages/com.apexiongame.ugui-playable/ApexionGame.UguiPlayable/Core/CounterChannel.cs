using System;
using UnityEngine;
using ApexionGame.UguiPlayable.Easing;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// A number that counts from <see cref="From"/> to <see cref="To"/> over a clip and is written as formatted
    /// text: decimals, digit grouping, separators, a prefix and a suffix.
    /// </summary>
    /// <remarks>
    /// The text is never parsed, so a From current counter starts from the last value the mixer wrote, or from
    /// <see cref="From"/>. Formatting does not use the culture, so every machine prints the same text. Only
    /// <see cref="ValueMode.Absolute"/> and <see cref="ValueMode.FromCurrent"/> are meaningful;
    /// <see cref="ValueMode.Relative"/> behaves like Absolute. The serialized field names are user data.
    /// </remarks>
    [Serializable]
    public struct CounterChannel
    {
        /// <summary>The most decimals a counter prints.</summary>
        public const int MAX_DECIMALS = 6;

        /// <summary>The group separator used when the field is null or empty.</summary>
        public const string DEFAULT_GROUP_SEPARATOR = ",";

        /// <summary>The decimal separator used when the field is null or empty.</summary>
        public const string DEFAULT_DECIMAL_SEPARATOR = ".";

        [SerializeField]
        private bool _enabled;

        [SerializeField]
        private ValueMode _mode;

        [SerializeField]
        private double _from;

        [SerializeField]
        private double _to;

        [SerializeField]
        private EaseSetting _ease;

        [SerializeField]
        private int _decimals;

        [SerializeField]
        private bool _grouping;

        [SerializeField]
        private string _groupSeparator;

        [SerializeField]
        private string _decimalSeparator;

        [SerializeField]
        private string _prefix;

        [SerializeField]
        private string _suffix;

        [NonSerialized]
        private double _captured;

        [NonSerialized]
        private bool _hasCaptured;

        /// <summary>
        /// Creates a channel with a linear ease, no decimals, no grouping and no prefix or suffix.
        /// </summary>
        public CounterChannel(double from, double to, ValueMode mode = ValueMode.Absolute, bool enabled = true)
            : this(from, to, 0, false, DEFAULT_GROUP_SEPARATOR, DEFAULT_DECIMAL_SEPARATOR, string.Empty, string.Empty, mode, enabled)
        {
        }

        /// <summary>
        /// Creates a channel with the given ease, no decimals, no grouping and no prefix or suffix.
        /// </summary>
        public CounterChannel(
              double from
            , double to
            , EaseSetting ease
            , ValueMode mode = ValueMode.Absolute
            , bool enabled = true
        )
            : this(from, to, ease, 0, false, DEFAULT_GROUP_SEPARATOR, DEFAULT_DECIMAL_SEPARATOR, string.Empty, string.Empty, mode, enabled)
        {
        }

        /// <summary>
        /// Creates a channel with a linear ease and the given format.
        /// </summary>
        public CounterChannel(
              double from
            , double to
            , int decimals
            , bool grouping
            , string groupSeparator = DEFAULT_GROUP_SEPARATOR
            , string decimalSeparator = DEFAULT_DECIMAL_SEPARATOR
            , string prefix = ""
            , string suffix = ""
            , ValueMode mode = ValueMode.Absolute
            , bool enabled = true
        )
            : this(from, to, EaseSetting.Default, decimals, grouping, groupSeparator, decimalSeparator, prefix, suffix, mode, enabled)
        {
        }

        /// <summary>
        /// Creates a channel with the given ease and format.
        /// </summary>
        public CounterChannel(
              double from
            , double to
            , EaseSetting ease
            , int decimals
            , bool grouping
            , string groupSeparator = DEFAULT_GROUP_SEPARATOR
            , string decimalSeparator = DEFAULT_DECIMAL_SEPARATOR
            , string prefix = ""
            , string suffix = ""
            , ValueMode mode = ValueMode.Absolute
            , bool enabled = true
        )
        {
            _enabled = enabled;
            _mode = mode;
            _from = from;
            _to = to;
            _ease = ease;
            _decimals = Math.Clamp(decimals, 0, MAX_DECIMALS);
            _grouping = grouping;
            _groupSeparator = groupSeparator ?? string.Empty;
            _decimalSeparator = decimalSeparator ?? string.Empty;
            _prefix = prefix ?? string.Empty;
            _suffix = suffix ?? string.Empty;
            _captured = 0d;
            _hasCaptured = false;
        }

        /// <summary>Whether the channel drives the text.</summary>
        public readonly bool Enabled => _enabled;

        /// <summary>How <see cref="From"/> is interpreted. Relative behaves like Absolute.</summary>
        public readonly ValueMode Mode => _mode;

        /// <summary>The start value. Ignored in <see cref="ValueMode.FromCurrent"/> once a value is captured.</summary>
        public readonly double From => _from;

        /// <summary>The end value.</summary>
        public readonly double To => _to;

        /// <summary>The ease applied to the clip's progress before the count.</summary>
        public readonly EaseSetting Ease => _ease;

        /// <summary>How many decimals are printed, 0 to <see cref="MAX_DECIMALS"/>.</summary>
        public readonly int Decimals => Math.Clamp(_decimals, 0, MAX_DECIMALS);

        /// <summary>Whether the integer digits are grouped by three.</summary>
        public readonly bool Grouping => _grouping;

        /// <summary>The text between groups; null or empty reads as a comma.</summary>
        public readonly string GroupSeparator
            => string.IsNullOrEmpty(_groupSeparator) ? DEFAULT_GROUP_SEPARATOR : _groupSeparator;

        /// <summary>The text between the integer and decimal digits; null or empty reads as a point.</summary>
        public readonly string DecimalSeparator
            => string.IsNullOrEmpty(_decimalSeparator) ? DEFAULT_DECIMAL_SEPARATOR : _decimalSeparator;

        /// <summary>The text before the number (after the sign); null reads as empty.</summary>
        public readonly string Prefix => _prefix ?? string.Empty;

        /// <summary>The text after the number; null reads as empty.</summary>
        public readonly string Suffix => _suffix ?? string.Empty;

        /// <summary>Whether <see cref="Captured"/> holds the value read when the clip became active.</summary>
        public readonly bool HasCaptured => _hasCaptured;

        /// <summary>The value read when the clip became active, used by <see cref="ValueMode.FromCurrent"/>.</summary>
        public readonly double Captured => _captured;

        /// <summary>
        /// Stores the value a <see cref="ValueMode.FromCurrent"/> count starts from.
        /// </summary>
        public void Capture(double current)
        {
            _captured = current;
            _hasCaptured = true;
        }

        /// <summary>
        /// Forgets the captured value, so the next activation captures again.
        /// </summary>
        public void ClearCapture()
        {
            _captured = 0d;
            _hasCaptured = false;
        }
    }
}
