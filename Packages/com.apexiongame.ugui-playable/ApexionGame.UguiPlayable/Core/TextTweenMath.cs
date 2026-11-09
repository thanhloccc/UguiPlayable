using System;
using UnityEngine;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// Fills a character buffer with the text of a string tween at a progress, without allocation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The text is as long as the longer of the two strings. The reveal count is
    /// <c>clamp((int)(progress * length + 1e-5), 0, length)</c> (progress 1 or more is the whole text): positions
    /// below it show <c>to</c> (a space past its end), even before any newline rule. At count 0 the text is
    /// <c>from</c> exactly, without padding or scramble. The other positions keep <c>from</c>, except that a position
    /// whose characters differ (or that is past <c>from</c>) shows a scramble character when scrambling is on. A
    /// newline of either string stays as its own character (<c>\r</c> stays <c>\r</c>). At progress 1 the result is
    /// exactly <c>to</c>.
    /// </para>
    /// <para>
    /// The scramble character is a hash of the position and the reveal count, never a random number: the same
    /// progress always gives the same text, so scrubbing is stable, and the text still changes while the reveal
    /// advances. The typewriter uses the same count rule. The behaviour matches TweenPlayables' string tween, with the
    /// reveal and scramble rules changed on purpose.
    /// </para>
    /// </remarks>
    public static class TextTweenMath
    {
        internal const float REVEAL_EPSILON = 1e-5f;

        private const string UPPERCASE = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string LOWERCASE = "abcdefghijklmnopqrstuvwxyz";
        private const string NUMERALS = "0123456789";

        private static readonly string s_all = UPPERCASE + LOWERCASE + NUMERALS;

        /// <summary>The buffer length <see cref="Fill"/> needs for this pair.</summary>
        public static int RequiredLength(string from, string to)
            => Math.Max(from == null ? 0 : from.Length, to == null ? 0 : to.Length);

        /// <summary>
        /// Writes the text at <paramref name="progress"/> (already eased) into <paramref name="buffer"/> and returns
        /// its length. The caller guarantees <c>buffer.Length &gt;= RequiredLength(from, to)</c>.
        /// </summary>
        public static int Fill(
              char[] buffer
            , string from
            , string to
            , float progress
            , ScrambleMode mode
            , string custom
        )
        {
            from ??= string.Empty;
            to ??= string.Empty;

            var fromLength = from.Length;
            var toLength = to.Length;

            if (progress >= 1f)
            {
                to.CopyTo(0, buffer, 0, toLength);
                return toLength;
            }

            var length = Math.Max(fromLength, toLength);

            if (length == 0)
            {
                return 0;
            }

            var count = CountOf(progress, length);

            if (count == 0)
            {
                from.CopyTo(0, buffer, 0, fromLength);
                return fromLength;
            }

            var set = GetSet(mode, custom);

            for (var i = 0; i < length; i++)
            {
                var hasFrom = i < fromLength;
                var hasTo = i < toLength;

                if (i < count)
                {
                    buffer[i] = hasTo ? to[i] : ' ';
                }
                else if (hasFrom && IsNewline(from[i]))
                {
                    buffer[i] = from[i];
                }
                else if (hasTo && IsNewline(to[i]))
                {
                    buffer[i] = to[i];
                }
                else if (hasFrom && hasTo && from[i] == to[i])
                {
                    buffer[i] = from[i];
                }
                else if (set.Length > 0)
                {
                    buffer[i] = set[(int)(Hash(i, count) % (uint)set.Length)];
                }
                else
                {
                    buffer[i] = hasFrom ? from[i] : ' ';
                }
            }

            return length;
        }

        internal static int CountOf(float ratio, int length)
            => Math.Clamp((int)(Mathf.Clamp01(ratio) * length + REVEAL_EPSILON), 0, length);

        private static bool IsNewline(char c)
            => c == '\n' || c == '\r';

        private static string GetSet(ScrambleMode mode, string custom)
            => mode switch {
                ScrambleMode.All => s_all,
                ScrambleMode.Uppercase => UPPERCASE,
                ScrambleMode.Lowercase => LOWERCASE,
                ScrambleMode.Numerals => NUMERALS,
                ScrambleMode.Custom => custom ?? string.Empty,
                _ => string.Empty,
            };

        // A murmur3-style integer mix: deterministic, no state, no allocation.
        private static uint Hash(int index, int count)
        {
            unchecked
            {
                var h = ((uint)index * 0x9E3779B1u) ^ ((uint)count * 0x85EBCA6Bu);

                h ^= h >> 16;
                h *= 0x7FEB352Du;
                h ^= h >> 15;
                h *= 0x846CA68Bu;
                h ^= h >> 16;
                return h;
            }
        }
    }
}
