using System;
using UnityEngine;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// Fills a character buffer with the text of a string tween at a progress, without allocation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The text is walked in slots: one slot per character (a surrogate pair or a parsed escape such as <c>\n</c> is
    /// one slot), and, with rich text, one slot per glyph tag (<c>&lt;br&gt;</c>, <c>&lt;sprite…&gt;</c>…); style
    /// tags take no slot. The six-argument overload keeps one slot per UTF-16 unit and no tags.
    /// </para>
    /// <para>
    /// The text is as long as the longer of the two strings in slots. The reveal count is
    /// <c>clamp((int)(progress * length + 1e-5), 0, length)</c> (progress 1 or more is the whole text): slots below
    /// it show <c>to</c> (a space past its end), even before any newline rule. At count 0 the text is <c>from</c>
    /// exactly, without padding or scramble. The other slots keep <c>from</c>, except that a slot that differs (or
    /// that is past <c>from</c>) shows a scramble character when scrambling is on. A newline of either string stays
    /// as its own character (<c>\r</c> stays <c>\r</c>). At progress 1 the result is exactly <c>to</c>.
    /// </para>
    /// <para>
    /// With rich text, the revealed part is <c>to</c> verbatim, tags included, followed by the closing tags of
    /// <c>to</c> that close what the revealed part opened (opening tags ahead are not written). The style tags of
    /// <c>from</c> are dropped in the unrevealed part; its glyph tags and <c>&lt;noparse&gt;</c> markers are kept. A
    /// custom scramble set never gives <c>&lt;</c>, <c>&gt;</c> or <c>\</c>. Where text from different places meets,
    /// a lone <c>\</c> or <c>&lt;</c> never joins what follows into an escape or a tag.
    /// </para>
    /// <para>
    /// The scramble character is a hash of the slot and the reveal count, never a random number: the same progress
    /// always gives the same text, so scrubbing is stable, and the text still changes while the reveal advances. The
    /// typewriter uses the same count rule. The behaviour matches TweenPlayables' string tween, with the reveal and
    /// scramble rules changed on purpose.
    /// </para>
    /// </remarks>
    public static class TextTweenMath
    {
        internal const float REVEAL_EPSILON = 1e-5f;

        /// <summary>The scramble step that means "use the reveal count".</summary>
        internal const int SCRAMBLE_STEP_FROM_REVEAL = -1;

        // A lone backslash written as "\" takes five more characters.
        private const int BACKSLASH_ESCAPE_EXTRA = 5;

        private const string UPPERCASE = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string LOWERCASE = "abcdefghijklmnopqrstuvwxyz";
        private const string NUMERALS = "0123456789";

        private static readonly string s_all = UPPERCASE + LOWERCASE + NUMERALS;

        /// <summary>
        /// The buffer length <see cref="Fill(char[], string, string, float, ScrambleMode, string)"/> needs for this
        /// pair.
        /// </summary>
        public static int RequiredLength(string from, string to)
            => Math.Max(from == null ? 0 : from.Length, to == null ? 0 : to.Length);

        /// <summary>
        /// The buffer length
        /// <see cref="Fill(char[], string, string, float, ScrambleMode, string, bool, bool, out int)"/> needs for
        /// this pair.
        /// </summary>
        /// <remarks>
        /// The sum of both lengths (a surrogate pair or an escape in one string can stand where the other has a
        /// single character, so the longer length alone is not enough), plus room for each backslash to become an
        /// escape of itself, and, with rich text, for a closing <c>&lt;/noparse&gt;</c> and one more per
        /// <c>&lt;</c>, so text from different places never joins into an escape or a tag.
        /// </remarks>
        public static int RequiredLength(string from, string to, bool richText)
        {
            from ??= string.Empty;
            to ??= string.Empty;

            var length = from.Length + to.Length;
            var backslashes = CountCharacter(from, '\\') + CountCharacter(to, '\\');

            length += backslashes * BACKSLASH_ESCAPE_EXTRA;

            if (richText)
            {
                var angles = CountCharacter(from, '<') + CountCharacter(to, '<');
                length += (angles + 1) * TextSlotScanner.NO_PARSE_CLOSE.Length;
            }

            return length;
        }

        /// <summary>
        /// Writes the text at <paramref name="progress"/> (already eased) into <paramref name="buffer"/> and returns
        /// its length. The caller guarantees <c>buffer.Length &gt;= RequiredLength(from, to)</c>.
        /// </summary>
        /// <remarks>
        /// Every UTF-16 unit is one position and tags are plain characters. Use the overload that takes
        /// <c>richText</c> for text that TextMeshPro parses.
        /// </remarks>
        public static int Fill(
              char[] buffer
            , string from
            , string to
            , float progress
            , ScrambleMode mode
            , string custom
        )
        {
            return FillCore(
                  buffer
                , from
                , to
                , progress
                , mode
                , custom
                , TextSlotScanner.Rules.Units
                , SCRAMBLE_STEP_FROM_REVEAL
                , out _
            );
        }

        /// <summary>
        /// Writes the text at <paramref name="progress"/> (already eased) into <paramref name="buffer"/> as
        /// TextMeshPro will parse it, and returns its length. The caller guarantees
        /// <c>buffer.Length &gt;= RequiredLength(from, to, richText)</c>.
        /// </summary>
        /// <param name="buffer">The buffer that receives the text.</param>
        /// <param name="from">The start string.</param>
        /// <param name="to">The end string.</param>
        /// <param name="progress">The eased progress, 0 to 1.</param>
        /// <param name="mode">The scramble characters.</param>
        /// <param name="custom">The characters used when <paramref name="mode"/> is
        /// <see cref="ScrambleMode.Custom"/>.</param>
        /// <param name="richText">Whether tags are markup (<c>TMP_Text.richText</c>).</param>
        /// <param name="parseEscapes">Whether <c>\n</c>, <c>\r</c>, <c>\t</c>, <c>\v</c> and <c>\\</c> escapes are
        /// one character (<c>TMP_Text.parseCtrlCharacters</c>).</param>
        /// <param name="slotCount">The number of characters written, glyph tags included and style tags excluded:
        /// the character count TextMeshPro gives the text.</param>
        public static int Fill(
              char[] buffer
            , string from
            , string to
            , float progress
            , ScrambleMode mode
            , string custom
            , bool richText
            , bool parseEscapes
            , out int slotCount
        )
        {
            return Fill(
                  buffer
                , from
                , to
                , progress
                , mode
                , custom
                , richText
                , parseEscapes
                , SCRAMBLE_STEP_FROM_REVEAL
                , out slotCount
            );
        }

        /// <summary>
        /// As the public rich overload, with the step that seeds the scramble characters; a negative
        /// <paramref name="scrambleStep"/> uses the reveal count.
        /// </summary>
        internal static int Fill(
              char[] buffer
            , string from
            , string to
            , float progress
            , ScrambleMode mode
            , string custom
            , bool richText
            , bool parseEscapes
            , int scrambleStep
            , out int slotCount
        )
        {
            var rules = TextSlotScanner.Rules.Elements;

            if (richText)
            {
                rules |= TextSlotScanner.Rules.RichText;
            }

            if (parseEscapes)
            {
                rules |= TextSlotScanner.Rules.ControlEscapes;
            }

            return FillCore(buffer, from, to, progress, mode, custom, rules, scrambleStep, out slotCount);
        }

        internal static int CountOf(float ratio, int length)
            => Math.Clamp((int)(Mathf.Clamp01(ratio) * length + REVEAL_EPSILON), 0, length);

        private static int FillCore(
              char[] buffer
            , string from
            , string to
            , float progress
            , ScrambleMode mode
            , string custom
            , TextSlotScanner.Rules rules
            , int scrambleStep
            , out int slotCount
        )
        {
            from ??= string.Empty;
            to ??= string.Empty;

            if (progress >= 1f)
            {
                to.CopyTo(0, buffer, 0, to.Length);
                slotCount = TextSlotScanner.CountSlots(to, rules);
                return to.Length;
            }

            var fromSlots = TextSlotScanner.CountSlots(from, rules);
            var length = Math.Max(fromSlots, TextSlotScanner.CountSlots(to, rules));
            var count = CountOf(progress, length);

            if (count == 0)
            {
                from.CopyTo(0, buffer, 0, from.Length);
                slotCount = fromSlots;
                return from.Length;
            }

            var writer = new Writer(buffer, rules);
            var toIndex = 0;
            var toNoParse = false;

            WriteRevealed(ref writer, to, count, rules, ref toIndex, ref toNoParse);
            WriteClosingTags(ref writer, to, toIndex, toNoParse, rules);

            var set = GetSet(mode, custom);

            WriteTail(
                  ref writer
                , from
                , to
                , rules
                , set
                , CountScrambleCharacters(set, rules)
                , scrambleStep < 0 ? count : scrambleStep
                , count
                , length
                , toIndex
                , toNoParse
            );

            slotCount = length;
            return writer.Length;
        }

        // The revealed part: `to` verbatim up to its count-th slot, then a space per slot past its end.
        private static void WriteRevealed(
              ref Writer writer
            , string to
            , int count
            , TextSlotScanner.Rules rules
            , ref int toIndex
            , ref bool toNoParse
        )
        {
            var revealed = 0;

            while (revealed < count && toIndex < to.Length)
            {
                var kind = TextSlotScanner.Next(to, toIndex, rules, ref toNoParse, out var length);
                var lone = TextSlotScanner.IsLoneMark(to, toIndex, length, kind, toNoParse, rules);

                writer.Put(to, toIndex, length, lone);
                toIndex += length;

                if (TextSlotScanner.IsSlot(kind))
                {
                    revealed++;
                }
            }

            for (; revealed < count; revealed++)
            {
                writer.Put(' ');
            }
        }

        // Closes what the revealed part leaves open: a </noparse> if it ends inside one, then each closing style tag
        // of `to` after it that matches a tag the revealed part opened. Opening tags ahead are not written, so layout
        // tags (<pos>, <space>, <page>…) and tags TMP does not know never reach the unrevealed part.
        private static void WriteClosingTags(
              ref Writer writer
            , string to
            , int toIndex
            , bool toNoParse
            , TextSlotScanner.Rules rules
        )
        {
            if ((rules & TextSlotScanner.Rules.RichText) == 0)
            {
                return;
            }

            if (toNoParse)
            {
                writer.Put(TextSlotScanner.NO_PARSE_CLOSE, 0, TextSlotScanner.NO_PARSE_CLOSE.Length, false);
            }

            var revealedEnd = toIndex;
            var revealedNoParse = toNoParse;
            var noParse = toNoParse;

            while (toIndex < to.Length)
            {
                var start = toIndex;
                var kind = TextSlotScanner.Next(to, toIndex, rules, ref noParse, out var length);

                toIndex += length;

                if (kind == TextSlotScanner.Kind.Style
                    && ClosesRevealedTag(to, start, length, revealedEnd, revealedNoParse, rules)
                )
                {
                    writer.Put(to, start, length, false);
                }
            }
        }

        // A closing tag is needed when the revealed part opened more tags of its name than it closed, and fewer
        // closing tags of that name came before it after the revealed part.
        private static bool ClosesRevealedTag(
              string to
            , int start
            , int length
            , int revealedEnd
            , bool revealedNoParse
            , TextSlotScanner.Rules rules
        )
        {
            TextSlotScanner.GetTagName(
                  to
                , start
                , length
                , out var closing
                , out var name
                , out var nameStart
                , out var nameLength
            );

            if (closing == false || nameLength == 0)
            {
                return false;
            }

            CountTags(to, 0, revealedEnd, false, rules, name, nameStart, nameLength, out var opened, out var closed);
            CountTags(to, revealedEnd, start, revealedNoParse, rules, name, nameStart, nameLength, out _, out var before);

            return before < opened - closed;
        }

        private static void CountTags(
              string text
            , int index
            , int end
            , bool noParse
            , TextSlotScanner.Rules rules
            , string name
            , int nameStart
            , int nameLength
            , out int opened
            , out int closed
        )
        {
            opened = 0;
            closed = 0;

            while (index < end)
            {
                var start = index;
                var kind = TextSlotScanner.Next(text, index, rules, ref noParse, out var length);

                index += length;

                if (kind != TextSlotScanner.Kind.Style)
                {
                    continue;
                }

                TextSlotScanner.GetTagName(
                      text
                    , start
                    , length
                    , out var closing
                    , out var tagName
                    , out var tagStart
                    , out var tagLength
                );

                if (TextSlotScanner.NamesEqual(tagName, tagStart, tagLength, name, nameStart, nameLength) == false)
                {
                    continue;
                }

                if (closing)
                {
                    closed++;
                }
                else
                {
                    opened++;
                }
            }
        }

        private static void WriteTail(
              ref Writer writer
            , string from
            , string to
            , TextSlotScanner.Rules rules
            , string set
            , int setCount
            , int step
            , int count
            , int length
            , int toIndex
            , bool toNoParse
        )
        {
            var fromIndex = 0;
            var fromNoParse = false;
            var skipped = 0;

            while (skipped < count && fromIndex < from.Length)
            {
                var kind = TextSlotScanner.Next(from, fromIndex, rules, ref fromNoParse, out var skippedLength);

                fromIndex += skippedLength;

                if (TextSlotScanner.IsSlot(kind))
                {
                    skipped++;
                }
            }

            if (fromNoParse)
            {
                writer.Put(TextSlotScanner.NO_PARSE_OPEN, 0, TextSlotScanner.NO_PARSE_OPEN.Length, false);
            }

            for (var slot = count; slot < length; slot++)
            {
                var hasFrom = NextFromSlot(
                      ref writer
                    , from
                    , ref fromIndex
                    , rules
                    , ref fromNoParse
                    , out var fromStart
                    , out var fromLength
                    , out var fromKind
                );

                var hasTo = TextSlotScanner.NextSlot(
                      to
                    , ref toIndex
                    , rules
                    , ref toNoParse
                    , out var toStart
                    , out var toLength
                    , out var toKind
                );

                var fromLone = hasFrom
                    && TextSlotScanner.IsLoneMark(from, fromStart, fromLength, fromKind, fromNoParse, rules);

                if (hasFrom && TextSlotScanner.GetNewline(from, fromStart, fromLength, fromKind) != '\0')
                {
                    writer.Put(from, fromStart, fromLength, fromLone);
                    continue;
                }

                var toNewline = hasTo ? TextSlotScanner.GetNewline(to, toStart, toLength, toKind) : '\0';

                if (toNewline != '\0' && toKind == TextSlotScanner.Kind.Glyph)
                {
                    writer.Put(toNewline);
                }
                else if (toNewline != '\0')
                {
                    writer.Put(to, toStart, toLength, false);
                }
                else if (hasFrom && hasTo && IsSameSlot(from, fromStart, fromLength, to, toStart, toLength))
                {
                    writer.Put(from, fromStart, fromLength, fromLone);
                }
                else if (setCount > 0)
                {
                    writer.Put(PickScramble(set, setCount, rules, Hash(slot, step)));
                }
                else if (hasFrom)
                {
                    writer.Put(from, fromStart, fromLength, fromLone);
                }
                else
                {
                    writer.Put(' ');
                }
            }
        }

        // The next slot of `from`, writing the <noparse> markers it passes so its literal text stays literal; its
        // style tags are dropped.
        private static bool NextFromSlot(
              ref Writer writer
            , string from
            , ref int fromIndex
            , TextSlotScanner.Rules rules
            , ref bool fromNoParse
            , out int start
            , out int length
            , out TextSlotScanner.Kind kind
        )
        {
            while (fromIndex < from.Length)
            {
                kind = TextSlotScanner.Next(from, fromIndex, rules, ref fromNoParse, out length);
                start = fromIndex;
                fromIndex += length;

                if (kind == TextSlotScanner.Kind.NoParse)
                {
                    writer.Put(from, start, length, false);
                }
                else if (kind != TextSlotScanner.Kind.Style)
                {
                    return true;
                }
            }

            start = fromIndex;
            length = 0;
            kind = TextSlotScanner.Kind.Style;
            return false;
        }

        private static bool IsSameSlot(string a, int aStart, int aLength, string b, int bStart, int bLength)
            => aLength == bLength && string.CompareOrdinal(a, aStart, b, bStart, aLength) == 0;

        private static int CountCharacter(string text, char c)
        {
            var count = 0;

            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == c)
                {
                    count++;
                }
            }

            return count;
        }

        private static string GetSet(ScrambleMode mode, string custom)
            => mode switch {
                ScrambleMode.All => s_all,
                ScrambleMode.Uppercase => UPPERCASE,
                ScrambleMode.Lowercase => LOWERCASE,
                ScrambleMode.Numerals => NUMERALS,
                ScrambleMode.Custom => custom ?? string.Empty,
                _ => string.Empty,
            };

        private static int CountScrambleCharacters(string set, TextSlotScanner.Rules rules)
        {
            if (rules == TextSlotScanner.Rules.Units)
            {
                return set.Length;
            }

            var count = 0;

            for (var i = 0; i < set.Length; i++)
            {
                if (IsScrambleCharacter(set[i], rules))
                {
                    count++;
                }
            }

            return count;
        }

        // Picks the hash-th allowed character by walking the set, so the set is filtered without a copy.
        private static char PickScramble(string set, int setCount, TextSlotScanner.Rules rules, uint hash)
        {
            var target = (int)(hash % (uint)setCount);

            if (setCount == set.Length)
            {
                return set[target];
            }

            for (var i = 0; i < set.Length; i++)
            {
                var c = set[i];

                if (IsScrambleCharacter(c, rules) == false)
                {
                    continue;
                }

                if (target == 0)
                {
                    return c;
                }

                target--;
            }

            return ' ';
        }

        // A lone surrogate never shows, a tag or escape start never forms markup with the characters around it.
        private static bool IsScrambleCharacter(char c, TextSlotScanner.Rules rules)
        {
            if (char.IsSurrogate(c))
            {
                return false;
            }

            var richText = (rules & TextSlotScanner.Rules.RichText) != 0;
            var escapes = (rules & TextSlotScanner.Rules.ControlEscapes) != 0;

            if (richText && (c == '<' || c == '>'))
            {
                return false;
            }

            return (richText || escapes) == false || c != '\\';
        }

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

        /// <summary>
        /// Writes the text so that parts taken from different places never join into markup TMP would parse. A lone
        /// <c>\</c> followed by a character that would make an escape is written as an escape of itself
        /// (<c>\\</c> when TMP parses control escapes, else <c>\u005C</c>). A <c>&gt;</c> that would close a lone
        /// <c>&lt;</c> into a tag gets a <c>&lt;/noparse&gt;</c> before it, which TMP consumes as a tag in any state
        /// (<c>TMP_Text.ValidateHtmlTag</c>). Neither changes the characters TMP shows.
        /// </summary>
        private ref struct Writer
        {
            private const string BACKSLASH_ESCAPE = "\\\\";
            private const string UNICODE_BACKSLASH_ESCAPE = "\\u005C";

            private readonly char[] _buffer;
            private readonly TextSlotScanner.Rules _rules;
            private int _length;
            private int _backslashAt;
            private bool _openAngle;

            public Writer(char[] buffer, TextSlotScanner.Rules rules)
            {
                _buffer = buffer;
                _rules = rules;
                _length = 0;
                _backslashAt = -1;
                _openAngle = false;
            }

            public readonly int Length => _length;

            /// <summary>
            /// Writes <paramref name="length"/> units of <paramref name="source"/>; <paramref name="lone"/> marks a
            /// lone <c>\</c> or <c>&lt;</c> slot (<see cref="TextSlotScanner.IsLoneMark"/>).
            /// </summary>
            public void Put(string source, int start, int length, bool lone)
            {
                var first = source[start];

                Guard(first);
                source.CopyTo(start, _buffer, _length, length);
                _length += length;
                Track(first, lone);
            }

            public void Put(char c)
            {
                Guard(c);
                _buffer[_length++] = c;
                Track(c, false);
            }

            private void Guard(char next)
            {
                if (_backslashAt >= 0 && _backslashAt == _length - 1 && JoinsBackslash(next))
                {
                    var escape = (_rules & TextSlotScanner.Rules.ControlEscapes) != 0
                        ? BACKSLASH_ESCAPE
                        : UNICODE_BACKSLASH_ESCAPE;

                    // The backslash already written is the escape's first character.
                    escape.CopyTo(1, _buffer, _length, escape.Length - 1);
                    _length += escape.Length - 1;
                    _backslashAt = -1;
                }

                if (_openAngle && next == '>')
                {
                    var close = TextSlotScanner.NO_PARSE_CLOSE;

                    close.CopyTo(0, _buffer, _length, close.Length);
                    _length += close.Length;
                    _openAngle = false;
                }
            }

            private void Track(char first, bool lone)
            {
                _backslashAt = lone && first == '\\' ? _length - 1 : -1;

                // TMP's tag scan stops at the next '<', so any '<' ends the open one; a lone '<' starts a new one.
                if (first == '<')
                {
                    _openAngle = lone;
                }
            }

            private readonly bool JoinsBackslash(char next)
            {
                if (next == 'u' || next == 'U')
                {
                    return true;
                }

                return (_rules & TextSlotScanner.Rules.ControlEscapes) != 0
                    && (next == '\\' || next == 'n' || next == 'r' || next == 't' || next == 'v');
            }
        }
    }
}
