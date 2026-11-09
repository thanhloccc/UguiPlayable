using System;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// Writes a counter value as text into a character buffer: no culture, no allocation.
    /// </summary>
    internal static class CounterFormatMath
    {
        private const int MAX_DIGITS = 20;
        private const int MAX_GROUPS = 7;

        // 2^63 as a double; a scaled value at or beyond it is clamped to the long range.
        private const double LONG_LIMIT = 9223372036854775808d;

        /// <summary>
        /// The buffer length that fits any value in <paramref name="format"/>: sign, digits, group and decimal
        /// separators, prefix and suffix.
        /// </summary>
        public static int RequiredLength(in CounterChannel format)
        {
            return 1
                + MAX_DIGITS
                + (MAX_GROUPS * format.GroupSeparator.Length)
                + format.DecimalSeparator.Length
                + format.Prefix.Length
                + format.Suffix.Length;
        }

        /// <summary>
        /// Writes <paramref name="value"/> and returns the length. The sign comes first, then the prefix, the
        /// digits and the suffix. The value is scaled by 10^decimals and rounded half away from zero to a
        /// <see cref="long"/> (clamped to its range); NaN and -0 print 0.
        /// </summary>
        /// <param name="buffer">At least <see cref="RequiredLength"/> long.</param>
        /// <param name="value">The number to write.</param>
        /// <param name="format">The decimals, grouping, separators, prefix and suffix.</param>
        public static int Fill(char[] buffer, double value, in CounterChannel format)
        {
            var decimals = format.Decimals;
            var grouping = format.Grouping;
            var groupSeparator = format.GroupSeparator;
            var decimalSeparator = format.DecimalSeparator;
            var prefix = format.Prefix;
            var suffix = format.Suffix;

            var scale = 1d;

            for (var i = 0; i < decimals; i++)
            {
                scale *= 10d;
            }

            var scaled = double.IsNaN(value) ? 0d : Math.Round(value * scale, MidpointRounding.AwayFromZero);
            ulong magnitude;
            bool negative;

            if (scaled >= LONG_LIMIT)
            {
                magnitude = long.MaxValue;
                negative = false;
            }
            else if (scaled <= -LONG_LIMIT)
            {
                magnitude = (ulong)long.MaxValue + 1UL;
                negative = true;
            }
            else
            {
                var whole = (long)scaled;

                negative = whole < 0L;
                magnitude = negative ? (ulong)(-(whole + 1L)) + 1UL : (ulong)whole;
            }

            var digitCount = 1;

            for (var rest = magnitude / 10UL; rest != 0UL; rest /= 10UL)
            {
                digitCount++;
            }

            var integerDigits = Math.Max(1, digitCount - decimals);
            var groups = grouping ? (integerDigits - 1) / 3 : 0;
            var length = (negative ? 1 : 0)
                + prefix.Length
                + integerDigits
                + (groups * groupSeparator.Length)
                + (decimals > 0 ? decimalSeparator.Length + decimals : 0)
                + suffix.Length;

            var position = length;

            position -= suffix.Length;
            suffix.CopyTo(0, buffer, position, suffix.Length);

            for (var i = 0; i < decimals; i++)
            {
                buffer[--position] = (char)('0' + (int)(magnitude % 10UL));
                magnitude /= 10UL;
            }

            if (decimals > 0)
            {
                position -= decimalSeparator.Length;
                decimalSeparator.CopyTo(0, buffer, position, decimalSeparator.Length);
            }

            for (var i = 0; i < integerDigits; i++)
            {
                if (i > 0 && i % 3 == 0 && grouping)
                {
                    position -= groupSeparator.Length;
                    groupSeparator.CopyTo(0, buffer, position, groupSeparator.Length);
                }

                buffer[--position] = (char)('0' + (int)(magnitude % 10UL));
                magnitude /= 10UL;
            }

            position -= prefix.Length;
            prefix.CopyTo(0, buffer, position, prefix.Length);

            if (negative)
            {
                buffer[--position] = '-';
            }

            return length;
        }
    }
}
