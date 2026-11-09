using System;
using TMPro;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Text
{
    /// <summary>
    /// Turns a typewriter ratio into a visible character count by character, word or line, with an optional pause
    /// after punctuation, and back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each unit weighs 1, plus the pause when it ends with punctuation (<c>.,:;!?…</c>) and the next character is not
    /// punctuation; the last unit never pauses. A ratio <c>r</c> reaches <c>r * W</c> of the total weight <c>W</c>,
    /// and unit <c>k</c> shows once that reaches its start plus 1, so its pause is the time after it appears. The
    /// count runs to the first character of the next unit, so the spaces and punctuation after a word come with it.
    /// By character with no pause this is <see cref="TextTweenMath.CountOf"/>.
    /// </para>
    /// <para>
    /// Words and lines are read from <see cref="TMP_TextInfo"/>. TMP never clears <c>wordInfo</c> between rebuilds and
    /// reports one word for a text with none, so the text has words only when one of its characters is a TMP word
    /// character; every unit used must lie inside the count and follow the previous one. Otherwise the scan falls back
    /// to characters. Arrays are indexed up to their length, so stale text info never throws. No allocation, O(n).
    /// </para>
    /// </remarks>
    internal static class TypewriterMath
    {
        // The inverse lands a quarter unit after the unit appears: far from both edges, so it survives float rounding.
        private const float INVERSE_OFFSET = 0.25f;

        private const string PUNCTUATION = ".,:;!?…";

        /// <summary>
        /// The number of the first <paramref name="characterCount"/> characters that <paramref name="ratio"/> shows:
        /// 0 at 0 or below, <paramref name="characterCount"/> at 1 or above.
        /// </summary>
        internal static int VisibleCount(
              TMP_TextInfo info
            , int characterCount
            , TypewriterUnit unit
            , float pause
            , float ratio
        )
        {
            if (characterCount <= 0 || ratio <= 0f)
            {
                return 0;
            }

            if (ratio >= 1f)
            {
                return characterCount;
            }

            pause = ToPause(pause);

            var scan = Resolve(info, characterCount, unit);

            if (scan.Unit == TypewriterUnit.Character && pause <= 0f)
            {
                return TextTweenMath.CountOf(ratio, characterCount);
            }

            var target = ratio * scan.TotalWeight(pause) + TextTweenMath.REVEAL_EPSILON;
            var start = 0f;
            var shown = 0;

            for (var k = 0; k < scan.Count; k++)
            {
                if (target < start + 1f)
                {
                    break;
                }

                shown = k + 1;
                start += scan.Weight(k, pause);
            }

            return scan.CountAfter(shown);
        }

        /// <summary>
        /// The ratio at which <see cref="VisibleCount"/> shows the whole units within
        /// <paramref name="visibleCount"/> characters: the inverse, so a From current typewriter starts where the
        /// text is. A count that ends inside a unit gives the ratio of the units before it.
        /// </summary>
        internal static float UnitRatio(
              TMP_TextInfo info
            , int characterCount
            , TypewriterUnit unit
            , float pause
            , int visibleCount
        )
        {
            if (characterCount <= 0 || visibleCount <= 0)
            {
                return 0f;
            }

            if (visibleCount >= characterCount)
            {
                return 1f;
            }

            pause = ToPause(pause);

            var scan = Resolve(info, characterCount, unit);

            if (scan.Unit == TypewriterUnit.Character && pause <= 0f)
            {
                return visibleCount / (float)characterCount;
            }

            var start = 0f;
            var lastStart = 0f;
            var shown = 0;

            for (var k = 0; k < scan.Count; k++)
            {
                if (scan.CountAfter(k + 1) > visibleCount)
                {
                    break;
                }

                shown = k + 1;
                lastStart = start;
                start += scan.Weight(k, pause);
            }

            if (shown == 0)
            {
                return 0f;
            }

            return Math.Min((lastStart + 1f + INVERSE_OFFSET) / scan.TotalWeight(pause), 1f);
        }

        /// <summary>Whether TMP counts <paramref name="c"/> as part of a word.</summary>
        internal static bool IsWordCharacter(char c)
            => char.IsLetterOrDigit(c) || c == '-' || c == '­' || c == '‐' || c == '‑';

        /// <summary>Whether a pause may follow <paramref name="c"/>.</summary>
        internal static bool IsPunctuation(char c)
            => c != '\0' && PUNCTUATION.IndexOf(c) >= 0;

        private static float ToPause(float pause)
            => pause > 0f ? pause : 0f;

        private static Scan Resolve(TMP_TextInfo info, int characterCount, TypewriterUnit unit)
        {
            var fallback = new Scan(info, characterCount, TypewriterUnit.Character, characterCount);

            if (info == null)
            {
                return fallback;
            }

            switch (unit)
            {
                case TypewriterUnit.Word:
                {
                    var words = info.wordInfo;

                    if (words == null || HasWordCharacter(info, characterCount) == false)
                    {
                        return fallback;
                    }

                    var count = Math.Min(info.wordCount, words.Length);
                    var previousLast = -1;

                    for (var k = 0; k < count; k++)
                    {
                        var first = words[k].firstCharacterIndex;
                        var last = words[k].lastCharacterIndex;

                        if (IsInOrder(first, last, previousLast, characterCount) == false)
                        {
                            return fallback;
                        }

                        previousLast = last;
                    }

                    return count > 0 ? new Scan(info, characterCount, unit, count) : fallback;
                }

                case TypewriterUnit.Line:
                {
                    var lines = info.lineInfo;

                    if (lines == null)
                    {
                        return fallback;
                    }

                    var count = Math.Min(info.lineCount, lines.Length);
                    var previousLast = -1;

                    for (var k = 0; k < count; k++)
                    {
                        var first = lines[k].firstCharacterIndex;
                        var last = lines[k].lastCharacterIndex;

                        if (IsInOrder(first, last, previousLast, characterCount) == false)
                        {
                            return fallback;
                        }

                        previousLast = last;
                    }

                    return count > 0 ? new Scan(info, characterCount, unit, count) : fallback;
                }

                default:
                    return fallback;
            }
        }

        private static bool IsInOrder(int first, int last, int previousLast, int characterCount)
            => first > previousLast && first <= last && last < characterCount;

        // wordInfo[0] can be left over from an earlier text, so the characters decide whether there is a word.
        private static bool HasWordCharacter(TMP_TextInfo info, int characterCount)
        {
            var characters = info.characterInfo;

            if (characters == null)
            {
                return false;
            }

            var limit = Math.Min(characterCount, characters.Length);

            for (var i = 0; i < limit; i++)
            {
                if (IsWordCharacter(characters[i].character))
                {
                    return true;
                }
            }

            return false;
        }

        // The units one scan walks: characters, or the words or lines of the text info, all checked by Resolve.
        private readonly struct Scan
        {
            private readonly TMP_TextInfo _info;
            private readonly int _characterCount;

            public readonly TypewriterUnit Unit;
            public readonly int Count;

            public Scan(TMP_TextInfo info, int characterCount, TypewriterUnit unit, int count)
            {
                _info = info;
                _characterCount = characterCount;
                Unit = unit;
                Count = count;
            }

            public float TotalWeight(float pause)
            {
                var total = 0f;

                for (var k = 0; k < Count; k++)
                {
                    total += Weight(k, pause);
                }

                return total;
            }

            public float Weight(int k, float pause)
                => pause > 0f && k + 1 < Count && EndsWithPunctuation(k) ? 1f + pause : 1f;

            // The count once the first `shown` units show: up to the next unit, or every character after the last.
            public int CountAfter(int shown)
            {
                if (shown <= 0)
                {
                    return 0;
                }

                return shown >= Count ? _characterCount : First(shown);
            }

            // The last character before the next unit that is not white space, with no punctuation right after it.
            private bool EndsWithPunctuation(int k)
            {
                var first = First(k);
                var next = First(k + 1);
                var j = next - 1;

                while (j >= first && char.IsWhiteSpace(CharacterAt(j)))
                {
                    j--;
                }

                return j >= first && IsPunctuation(CharacterAt(j)) && IsPunctuation(CharacterAt(next)) == false;
            }

            private int First(int k)
                => Unit switch {
                    TypewriterUnit.Word => _info.wordInfo[k].firstCharacterIndex,
                    TypewriterUnit.Line => _info.lineInfo[k].firstCharacterIndex,
                    _ => k,
                };

            private char CharacterAt(int index)
            {
                var characters = _info?.characterInfo;

                if (characters == null || index < 0 || index >= _characterCount || index >= characters.Length)
                {
                    return '\0';
                }

                return characters[index].character;
            }
        }
    }
}
