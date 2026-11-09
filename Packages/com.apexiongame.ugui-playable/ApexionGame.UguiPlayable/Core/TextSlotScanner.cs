using System;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// Splits a string into the elements TextMeshPro would see, without allocation: characters and glyph tags take
    /// one slot each, style tags and <c>&lt;noparse&gt;</c> markers take none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// With <see cref="Rules.Units"/> every UTF-16 unit is one character slot. <see cref="Rules.Elements"/> keeps a
    /// surrogate pair and a <c>\uXXXX</c> or <c>\UXXXXXXXX</c> escape as one slot, as TMP always does.
    /// <see cref="Rules.ControlEscapes"/> adds the <c>\\</c>, <c>\n</c>, <c>\r</c>, <c>\t</c> and <c>\v</c> escapes
    /// that TMP parses when <c>parseCtrlCharacters</c> is on.
    /// </para>
    /// <para>
    /// With <see cref="Rules.RichText"/> a tag runs from <c>&lt;</c> to the first <c>&gt;</c>, with no <c>&lt;</c>
    /// inside and at most 127 characters between them, as TMP scans it. Names are matched ordinal and ignoring ASCII
    /// case. <c>&lt;br&gt;</c>, <c>&lt;cr&gt;</c>, <c>&lt;nbsp&gt;</c>, <c>&lt;zwsp&gt;</c>, <c>&lt;zwj&gt;</c>,
    /// <c>&lt;shy&gt;</c> and <c>&lt;sprite…&gt;</c> are glyph tags; every other tag is a style tag, including a name
    /// TMP does not know (TMP prints those as text). Between <c>&lt;noparse&gt;</c> and <c>&lt;/noparse&gt;</c> only
    /// the closing marker is a tag. Escapes are read before tags, also inside <c>&lt;noparse&gt;</c>, as in TMP.
    /// </para>
    /// </remarks>
    internal static class TextSlotScanner
    {
        internal const string NO_PARSE_OPEN = "<noparse>";
        internal const string NO_PARSE_CLOSE = "</noparse>";

        private const int MAX_TAG_CONTENT = 127;
        private const int UTF16_ESCAPE_LENGTH = 6;
        private const int UTF32_ESCAPE_LENGTH = 10;
        private const int CONTROL_ESCAPE_LENGTH = 2;

        private const string NO_PARSE = "noparse";
        private const string SLASH_NO_PARSE = "/noparse";
        private const string BR = "br";
        private const string CR = "cr";
        private const string NBSP = "nbsp";
        private const string ZWSP = "zwsp";
        private const string ZWJ = "zwj";
        private const string SHY = "shy";
        private const string SPRITE = "sprite";
        private const string COLOR = "color";

        [Flags]
        internal enum Rules : byte
        {
            Units = 0,
            Elements = 1 << 0,
            ControlEscapes = 1 << 1,
            RichText = 1 << 2,
        }

        internal enum Kind : byte
        {
            Character,
            Glyph,
            Style,
            NoParse,
        }

        /// <summary>
        /// Reads the element at <paramref name="index"/> and returns its kind; <paramref name="length"/> is its
        /// number of UTF-16 units. <paramref name="noParse"/> is the <c>&lt;noparse&gt;</c> state, updated by a marker.
        /// </summary>
        public static Kind Next(string text, int index, Rules rules, ref bool noParse, out int length)
        {
            if (rules == Rules.Units)
            {
                length = 1;
                return Kind.Character;
            }

            var c = text[index];

            if (c == '\\' && TryReadEscape(text, index, rules, out length))
            {
                return Kind.Character;
            }

            if (char.IsHighSurrogate(c) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
            {
                length = 2;
                return Kind.Character;
            }

            if (c == '<' && (rules & Rules.RichText) != 0 && TryReadTag(text, index, out length))
            {
                return ClassifyTag(text, index, ref noParse, ref length);
            }

            length = 1;
            return Kind.Character;
        }

        /// <summary>
        /// Moves <paramref name="index"/> past the next slot (a character or glyph tag) and returns it, skipping style
        /// tags and markers. Returns false at the end of the text.
        /// </summary>
        public static bool NextSlot(
              string text
            , ref int index
            , Rules rules
            , ref bool noParse
            , out int start
            , out int length
            , out Kind kind
        )
        {
            while (index < text.Length)
            {
                kind = Next(text, index, rules, ref noParse, out length);
                start = index;
                index += length;

                if (IsSlot(kind))
                {
                    return true;
                }
            }

            start = index;
            length = 0;
            kind = Kind.Style;
            return false;
        }

        /// <summary>The number of slots of <paramref name="text"/>.</summary>
        public static int CountSlots(string text, Rules rules)
        {
            if (rules == Rules.Units)
            {
                return text.Length;
            }

            var count = 0;
            var index = 0;
            var noParse = false;

            while (NextSlot(text, ref index, rules, ref noParse, out _, out _, out _))
            {
                count++;
            }

            return count;
        }

        public static bool IsSlot(Kind kind)
            => kind == Kind.Character || kind == Kind.Glyph;

        /// <summary>
        /// The line break a slot stands for (<c>\n</c> or <c>\r</c>, as a character, an escape or a <c>&lt;br&gt;</c>
        /// / <c>&lt;cr&gt;</c> tag), or <c>'\0'</c> when it is not one.
        /// </summary>
        public static char GetNewline(string text, int start, int length, Kind kind)
        {
            var c = text[start];

            if (kind == Kind.Glyph)
            {
                var nameLength = GetNameLength(text, start, length);

                if (NameEquals(text, start + 1, nameLength, BR))
                {
                    return '\n';
                }

                return NameEquals(text, start + 1, nameLength, CR) ? '\r' : '\0';
            }

            if (length == 1)
            {
                return c == '\n' || c == '\r' ? c : '\0';
            }

            if (length == CONTROL_ESCAPE_LENGTH && c == '\\')
            {
                var escaped = text[start + 1];

                if (escaped == 'n')
                {
                    return '\n';
                }

                return escaped == 'r' ? '\r' : '\0';
            }

            return '\0';
        }

        /// <summary>
        /// Whether a slot is a lone <c>\</c> or, outside <c>&lt;noparse&gt;</c>, a lone <c>&lt;</c>: a character
        /// that could form an escape or a tag with what is written after it.
        /// </summary>
        public static bool IsLoneMark(string text, int start, int length, Kind kind, bool noParse, Rules rules)
        {
            if (rules == Rules.Units || kind != Kind.Character || length != 1)
            {
                return false;
            }

            var c = text[start];
            return c == '\\' || (c == '<' && noParse == false && (rules & Rules.RichText) != 0);
        }

        /// <summary>
        /// The name of a style tag: after <c>&lt;</c> or <c>&lt;/</c>, up to <c>&gt;</c>, <c>=</c> or a space.
        /// <c>&lt;#…&gt;</c> is named <c>color</c>, as <c>&lt;/color&gt;</c> closes it.
        /// </summary>
        public static void GetTagName(
              string text
            , int start
            , int length
            , out bool closing
            , out string nameSource
            , out int nameStart
            , out int nameLength
        )
        {
            closing = text[start + 1] == '/';

            if (closing == false && text[start + 1] == '#')
            {
                nameSource = COLOR;
                nameStart = 0;
                nameLength = COLOR.Length;
                return;
            }

            nameSource = text;
            nameStart = closing ? start + 2 : start + 1;
            nameLength = GetNameLength(text, start, length) - (closing ? 1 : 0);
        }

        /// <summary>Whether two tag names are equal, ignoring ASCII case.</summary>
        public static bool NamesEqual(string a, int aStart, int aLength, string b, int bStart, int bLength)
        {
            if (aLength != bLength)
            {
                return false;
            }

            for (var i = 0; i < aLength; i++)
            {
                if (ToLowerAscii(a[aStart + i]) != ToLowerAscii(b[bStart + i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryReadEscape(string text, int index, Rules rules, out int length)
        {
            if (index + 1 >= text.Length)
            {
                goto FAILED;
            }

            var escaped = text[index + 1];

            switch (escaped)
            {
                case '\\':
                case 'n':
                case 'r':
                case 't':
                case 'v':
                {
                    if ((rules & Rules.ControlEscapes) == 0)
                    {
                        goto FAILED;
                    }

                    length = CONTROL_ESCAPE_LENGTH;
                    return true;
                }

                case 'u':
                {
                    if (index + UTF16_ESCAPE_LENGTH > text.Length || IsHex(text, index + 2, 4) == false)
                    {
                        goto FAILED;
                    }

                    length = UTF16_ESCAPE_LENGTH;
                    return true;
                }

                case 'U':
                {
                    if (index + UTF32_ESCAPE_LENGTH > text.Length || IsHex(text, index + 2, 8) == false)
                    {
                        goto FAILED;
                    }

                    length = UTF32_ESCAPE_LENGTH;
                    return true;
                }
            }

        FAILED:
            length = 0;
            return false;
        }

        private static bool IsHex(string text, int start, int count)
        {
            for (var i = start; i < start + count; i++)
            {
                var c = text[i];

                if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static bool TryReadTag(string text, int index, out int length)
        {
            var end = Math.Min(text.Length, index + 2 + MAX_TAG_CONTENT);

            for (var i = index + 1; i < end; i++)
            {
                var c = text[i];

                if (c == '<')
                {
                    break;
                }

                if (c != '>')
                {
                    continue;
                }

                if (i == index + 1)
                {
                    break;
                }

                length = i - index + 1;
                return true;
            }

            length = 0;
            return false;
        }

        private static Kind ClassifyTag(string text, int index, ref bool noParse, ref int length)
        {
            var nameLength = GetNameLength(text, index, length);

            if (NameEquals(text, index + 1, nameLength, SLASH_NO_PARSE))
            {
                noParse = false;
                return Kind.NoParse;
            }

            if (noParse)
            {
                length = 1;
                return Kind.Character;
            }

            if (NameEquals(text, index + 1, nameLength, NO_PARSE))
            {
                noParse = true;
                return Kind.NoParse;
            }

            var isGlyph = NameEquals(text, index + 1, nameLength, BR)
                || NameEquals(text, index + 1, nameLength, CR)
                || NameEquals(text, index + 1, nameLength, NBSP)
                || NameEquals(text, index + 1, nameLength, ZWSP)
                || NameEquals(text, index + 1, nameLength, ZWJ)
                || NameEquals(text, index + 1, nameLength, SHY)
                || NameEquals(text, index + 1, nameLength, SPRITE);

            return isGlyph ? Kind.Glyph : Kind.Style;
        }

        // The name ends at '>', '=' or a space, as in TMP's tag hash.
        private static int GetNameLength(string text, int tagStart, int tagLength)
        {
            var end = tagStart + tagLength - 1;

            for (var i = tagStart + 1; i < end; i++)
            {
                var c = text[i];

                if (c == '=' || c == ' ')
                {
                    return i - tagStart - 1;
                }
            }

            return end - tagStart - 1;
        }

        private static bool NameEquals(string text, int start, int length, string name)
        {
            if (length != name.Length)
            {
                return false;
            }

            for (var i = 0; i < length; i++)
            {
                if (ToLowerAscii(text[start + i]) != name[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static char ToLowerAscii(char c)
            => c >= 'A' && c <= 'Z' ? (char)(c + ('a' - 'A')) : c;
    }
}
