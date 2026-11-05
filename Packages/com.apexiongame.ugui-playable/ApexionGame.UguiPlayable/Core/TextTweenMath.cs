using System;
using UnityEngine;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// Fills a character buffer with the text of a string tween at a progress, without allocation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The text is as long as the longer of the two strings. The position <c>(length - 1) * progress</c> is the
    /// reveal index: positions up to it show <c>to</c>. The rest keeps <c>from</c>, except that a position whose
    /// characters differ (or that is past <c>from</c>) shows a scramble character when scrambling is on. A newline
    /// in either string is kept. At progress 1 the result is exactly <c>to</c>.
    /// </para>
    /// <para>
    /// The scramble character is a hash of the position and the reveal index, never a random number: the same
    /// progress always gives the same text, so scrubbing is stable, and the text still changes while the reveal
    /// advances. The behaviour matches TweenPlayables' string tween, with the scramble rule changed on purpose.
    /// </para>
    /// </remarks>
    public static class TextTweenMath
    {
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

            var reveal = (int)((length - 1) * Mathf.Clamp01(progress));
            var set = GetSet(mode, custom);

            for (var i = 0; i < length; i++)
            {
                var hasFrom = i < fromLength;
                var hasTo = i < toLength;

                if ((hasFrom && IsNewline(from[i])) || (hasTo && IsNewline(to[i])))
                {
                    buffer[i] = '\n';
                }
                else if (i <= reveal)
                {
                    buffer[i] = hasTo ? to[i] : ' ';
                }
                else if (hasFrom && hasTo && from[i] == to[i])
                {
                    buffer[i] = from[i];
                }
                else if (set.Length > 0)
                {
                    buffer[i] = set[(int)(Hash(i, reveal) % (uint)set.Length)];
                }
                else
                {
                    buffer[i] = hasFrom ? from[i] : ' ';
                }
            }

            return length;
        }

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
        private static uint Hash(int index, int reveal)
        {
            unchecked
            {
                var h = ((uint)index * 0x9E3779B1u) ^ ((uint)reveal * 0x85EBCA6Bu);

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
