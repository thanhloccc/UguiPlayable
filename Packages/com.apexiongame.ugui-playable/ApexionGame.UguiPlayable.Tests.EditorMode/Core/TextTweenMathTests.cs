using System;
using NUnit.Framework;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Core;

public sealed class TextTweenMathTests
{
    private const string CUSTOM = "#$";

    private static string Fill(
          string from
        , string to
        , float progress
        , ScrambleMode mode = ScrambleMode.None
        , string custom = ""
    )
    {
        var buffer = new char[Math.Max(TextTweenMath.RequiredLength(from, to), 1)];
        var length = TextTweenMath.Fill(buffer, from, to, progress, mode, custom);

        return new string(buffer, 0, length);
    }

    private static string RichFill(
          string from
        , string to
        , float progress
        , out int slotCount
        , ScrambleMode mode = ScrambleMode.None
        , string custom = ""
        , bool richText = true
        , bool parseEscapes = true
    )
    {
        var buffer = new char[Math.Max(TextTweenMath.RequiredLength(from, to, richText), 1)];

        var length = TextTweenMath.Fill(
              buffer
            , from
            , to
            , progress
            , mode
            , custom
            , richText
            , parseEscapes
            , out slotCount
        );

        return new string(buffer, 0, length);
    }

    private static string RichFill(string from, string to, float progress)
        => RichFill(from, to, progress, out _);

    [Test]
    public void RequiredLength_IsTheLongerString()
    {
        Assert.AreEqual(5, TextTweenMath.RequiredLength("abc", "hello"));
        Assert.AreEqual(3, TextTweenMath.RequiredLength("abc", null));
        Assert.AreEqual(0, TextTweenMath.RequiredLength(null, null));
    }

    [Test]
    public void Fill_AtZeroIsFromVerbatim()
    {
        Assert.AreEqual("abcd", Fill("abcd", "Wxyz", 0f));
    }

    [Test]
    public void Fill_AtZeroDoesNotPadOrScramble()
    {
        Assert.AreEqual("ab", Fill("ab", "wxyz", 0f, ScrambleMode.All));
        Assert.AreEqual("abcd", Fill("abcd", "xy", 0f, ScrambleMode.All));
        Assert.AreEqual(string.Empty, Fill(string.Empty, "xy", 0f, ScrambleMode.All));
    }

    [Test]
    public void Fill_AtHalfRevealsHalf()
    {
        // length 4, count = (int)(4 * 0.5) = 2.
        Assert.AreEqual("WXcd", Fill("abcd", "WXYZ", 0.5f));
    }

    [Test]
    public void Fill_AtOneIsTheTargetExactly()
    {
        Assert.AreEqual("WXYZ", Fill("abcd", "WXYZ", 1f, ScrambleMode.All));
        Assert.AreEqual("hi", Fill("hello world", "hi", 1f, ScrambleMode.All));
        Assert.AreEqual("hello world", Fill("hi", "hello world", 1f, ScrambleMode.All));
        Assert.AreEqual(string.Empty, Fill("abc", string.Empty, 1f));
    }

    [Test]
    public void Fill_AtOneWinsOverANewlineOfFrom()
    {
        Assert.AreEqual("ab", Fill("a\nb", "ab", 1f));
    }

    [Test]
    public void Fill_KeepsEqualCharactersWhileUnrevealed()
    {
        var text = Fill("LEVEL 1", "LEVEL 2", 0.1f, ScrambleMode.Numerals);

        Assert.AreEqual("LEVEL ", text.Substring(0, 6));
    }

    [Test]
    public void Fill_ScramblesAnUnrevealedDifferingPosition()
    {
        for (var step = 0; step < 20; step++)
        {
            var text = Fill("LEVEL 1", "LEVEL 2", step / 20f * 0.99f, ScrambleMode.Numerals);

            Assert.AreEqual(7, text.Length);
            Assert.That(char.IsDigit(text[6]), "position 6 must be a digit at step " + step);
        }
    }

    [Test]
    public void Fill_WithoutScrambleKeepsTheFromCharacter()
    {
        Assert.AreEqual("WXcd", Fill("abcd", "WXYZ", 0.5f, ScrambleMode.None));
    }

    [Test]
    public void Fill_ScramblesOnlyUnrevealedDifferingPositions()
    {
        // length 5, count = (int)(5 * 0.5) = 2: positions 0..1 are the target; 2 and 3 differ (scrambled); 4 is equal.
        var text = Fill("abcdE", "XYZWE", 0.5f, ScrambleMode.Custom, CUSTOM);

        Assert.AreEqual("XY", text.Substring(0, 2));
        Assert.That(CUSTOM.Contains(text[2]));
        Assert.That(CUSTOM.Contains(text[3]));
        Assert.AreEqual('E', text[4]);
    }

    [Test]
    public void Fill_UsesTheCharactersOfTheMode()
    {
        for (var step = 0; step < 10; step++)
        {
            var progress = step / 10f;

            AssertAll("ABCDEFGHIJKLMNOPQRSTUVWXYZ", Fill("          ", "!!!!!!!!!!", progress, ScrambleMode.Uppercase), progress);
            AssertAll("abcdefghijklmnopqrstuvwxyz", Fill("          ", "!!!!!!!!!!", progress, ScrambleMode.Lowercase), progress);
            AssertAll("0123456789", Fill("          ", "!!!!!!!!!!", progress, ScrambleMode.Numerals), progress);
        }
    }

    private static void AssertAll(string set, string text, float progress)
    {
        if (progress == 0f)
        {
            Assert.AreEqual("          ", text, "at 0 the text is from verbatim");
            return;
        }

        var count = (int)(10 * progress + 1e-5f);

        for (var i = count; i < text.Length; i++)
        {
            Assert.That(set.Contains(text[i]), $"'{text[i]}' not in the set at {i}");
        }
    }

    [Test]
    public void Fill_AnEmptyCustomSetMeansNoScramble()
    {
        Assert.AreEqual("WXcd", Fill("abcd", "WXYZ", 0.5f, ScrambleMode.Custom, string.Empty));
        Assert.AreEqual("WXcd", Fill("abcd", "WXYZ", 0.5f, ScrambleMode.Custom, null));
    }

    [Test]
    public void Fill_KeepsNewlinesFromEitherString()
    {
        Assert.AreEqual("a\nc", Fill("a\nc", "xyz", 0f, ScrambleMode.None));
        Assert.AreEqual('\n', Fill("ab\ncd", "xyzwq", 0.2f, ScrambleMode.All)[2]);
        Assert.AreEqual('\n', Fill("abcde", "xy\nwq", 0.2f, ScrambleMode.All)[2]);
        Assert.AreEqual('\r', Fill("ab\rcd", "xyzwq", 0.2f, ScrambleMode.All)[2]);
    }

    [Test]
    public void Fill_RevealedPositionWinsOverANewlineOfFrom()
    {
        // length 3, count = (int)(3 * 0.7) = 2: positions 0..1 are the target, even where from has a newline.
        Assert.AreEqual("xyc", Fill("a\nc", "xyz", 0.7f));
        Assert.AreEqual("x\nc", Fill("a\nc", "xyz", 0.4f));
    }

    [Test]
    public void Fill_KeepsACarriageReturn()
    {
        // length 4, count 1: "\r" stays "\r" and "\n" stays "\n", so "\r\n" is not turned into two line breaks.
        Assert.AreEqual("w\r\nb", Fill("a\r\nb", "wxyz", 0.3f));
    }

    [Test]
    public void Fill_RevealsInEvenSteps()
    {
        const string FROM = "abcde";
        const string TO = "VWXYZ";

        var seen = new System.Collections.Generic.HashSet<string>();

        for (var k = 0; k <= TO.Length; k++)
        {
            Assert.AreEqual(TO.Substring(0, k) + FROM.Substring(k), Fill(FROM, TO, k / (float)TO.Length), $"count {k}");
        }

        for (var i = 0; i <= 1000; i++)
        {
            seen.Add(Fill(FROM, TO, i / 1000f));
        }

        Assert.AreEqual(TO.Length + 1, seen.Count);
    }

    [Test]
    public void Fill_PadsAShorterTargetWithSpacesWhileRevealing()
    {
        // length 5, count 4 at 0.99: revealed positions past "hi" show a space; position 4 still keeps "o".
        Assert.AreEqual("hi  o", Fill("hello", "hi", 0.99f));
    }

    [Test]
    public void Fill_TheSameProgressGivesTheSameText()
    {
        var first = Fill("LEVEL 1", "LEVEL 2", 0.37f, ScrambleMode.All);

        for (var i = 0; i < 10; i++)
        {
            Assert.AreEqual(first, Fill("LEVEL 1", "LEVEL 2", 0.37f, ScrambleMode.All));
        }
    }

    [Test]
    public void Fill_ScrambleChangesAsTheRevealAdvances()
    {
        var seen = new System.Collections.Generic.HashSet<string>();

        for (var i = 0; i < 9; i++)
        {
            seen.Add(Fill("----------", "ABCDEFGHIJ", i / 10f, ScrambleMode.All));
        }

        Assert.Greater(seen.Count, 1);
    }

    [Test]
    public void Fill_DoesNotAllocate()
    {
        var buffer = new char[16];
        var from = "LEVEL 1";
        var to = "LEVEL 22";

        TextTweenMath.Fill(buffer, from, to, 0.5f, ScrambleMode.All, string.Empty);

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < 100; i++)
        {
            TextTweenMath.Fill(buffer, from, to, i / 100f, ScrambleMode.All, string.Empty);
            TextTweenMath.Fill(buffer, from, to, i / 100f, ScrambleMode.Custom, CUSTOM);
        }

        Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
    }

    [Test]
    public void RequiredLength_RichIsBothStringsAndAClosingNoParse()
    {
        Assert.AreEqual(8 + 10, TextTweenMath.RequiredLength("abc", "hello", richText: true));
        Assert.AreEqual(8, TextTweenMath.RequiredLength("abc", "hello", richText: false));
        Assert.AreEqual(10, TextTweenMath.RequiredLength(null, null, richText: true));
    }

    [Test]
    public void Fill_RichText_EmitsTagsWhole()
    {
        var seen = new System.Collections.Generic.HashSet<string>();

        for (var i = 0; i <= 100; i++)
        {
            seen.Add(RichFill("xy", "<b>ab</b>", i / 100f));
        }

        // length 2: from verbatim, then "a" revealed with its tags, then the target.
        Assert.That(seen, Is.EquivalentTo(new[] { "xy", "<b>a</b>y", "<b>ab</b>" }));
    }

    [Test]
    public void Fill_RichText_ClosesToTagsAtTheBoundary()
    {
        // length 4, count 2: "a" and "b" revealed; </color> follows at once, so "yz" is not red.
        Assert.AreEqual("a<color=#F00>b</color>yz", RichFill("wxyz", "a<color=#F00>bc</color>d", 0.5f));

        // count 1: the colour tag comes after "a"; neither it nor its closing tag is written.
        Assert.AreEqual("axyz", RichFill("wxyz", "a<color=#F00>bc</color>d", 0.25f));

        // A tag the revealed part opens and `to` never closes stays open, as in the final text.
        Assert.AreEqual("<i>axyz", RichFill("wxyz", "<i>abcd", 0.25f));

        // Only the closing tags the revealed part needs: one </b> for one open <b>, </color> for <#F00>.
        Assert.AreEqual("<b>a</b>xyz", RichFill("wxyz", "<b>a<b>b</b>c</b>d", 0.25f));
        Assert.AreEqual("<#F00>a</color>xyz", RichFill("wxyz", "<#F00>abc</color>d", 0.25f));
        Assert.AreEqual("<B>a</b>xyz", RichFill("wxyz", "<B>a</i>bc</b>d", 0.25f));
    }

    [Test]
    public void Fill_RichText_OpeningTagsAheadNeverReachTheTail()
    {
        // Layout tags and tags TMP does not know, ahead of the revealed part, are not written early.
        Assert.AreEqual("axyz", RichFill("wxyz", "a<pos=50%>bcd", 0.25f));
        Assert.AreEqual("axyz", RichFill("wxyz", "a<space=2em>b<page>cd", 0.25f));
        Assert.AreEqual("1xxxxxxxxx", RichFill("xxxxxxxxxx", "1 < 2 and 3 > 2", 0.1f));
    }

    [Test]
    public void Fill_NeverJoinsALoneBackslashIntoAnEscape()
    {
        // "C:\" revealed, then the "n" of from: the backslash is written as an escape of itself.
        Assert.AreEqual("C:\\\\nefgh", RichFill("abcnefgh", "C:\\Users", 0.375f));

        // Without parsed control escapes only "\u" / "\U" can join; then the backslash becomes "\u005C".
        Assert.AreEqual("C:\\nefgh", RichFill("abcnefgh", "C:\\Users", 0.375f, out _, parseEscapes: false));
        Assert.AreEqual("C:\\u005Cuefgh", RichFill("abcuefgh", "C:\\Users", 0.375f, out _, parseEscapes: false));

        // A dropped style tag of from does not join its "\" and "n" either.
        Assert.AreEqual("x\\\\n", RichFill("a\\<b>n", "xyz", 0.34f));

        // The final text is to verbatim.
        Assert.AreEqual("C:\\Users", RichFill("abcnefgh", "C:\\Users", 1f));
    }

    [Test]
    public void Fill_RichText_NeverClosesALoneAngleIntoATag()
    {
        // A lone "<" revealed, then "b>" of from: </noparse> keeps "<b>" from becoming a bold tag.
        Assert.AreEqual("x<b</noparse>>yz", RichFill("aab>yz", "x<<<<<", 0.34f));

        // The final text is to verbatim.
        Assert.AreEqual("x<<<<<", RichFill("aab>yz", "x<<<<<", 1f));
    }

    [Test]
    public void Fill_RichText_BrTakesOneSlot()
    {
        // length 5, count 3: "a", "b", <br>.
        Assert.AreEqual("ab<br>yz", RichFill("vwxyz", "ab<br>cd", 0.6f, out var slotCount));
        Assert.AreEqual(5, slotCount);

        Assert.AreEqual("ab<BR>yz", RichFill("vwxyz", "ab<BR>cd", 0.6f));
        Assert.AreEqual("a<sprite=1>z", RichFill("xyz", "a<sprite=1>b", 0.67f));
        Assert.AreEqual("a<nbsp>z", RichFill("xyz", "a<nbsp>b", 0.67f));

        // A <br> of from keeps its line in the unrevealed part, as a newline character does.
        Assert.AreEqual("x<br>b", RichFill("a<br>b", "xyz", 0.34f));

        // A <br> of to in the unrevealed part keeps the line as a newline character.
        Assert.AreEqual("x\nc", RichFill("abc", "x<br>z", 0.34f));
    }

    [Test]
    public void Fill_RichText_NoParseKeepsTagsAsText()
    {
        // "<b>" inside <noparse> is three characters; the closing marker follows the revealed "<".
        Assert.AreEqual("<noparse><</noparse>yz", RichFill("xyz", "<noparse><b></noparse>", 0.34f, out var slotCount));
        Assert.AreEqual(3, slotCount);

        // A <noparse> that `to` leaves open is closed before the unrevealed part.
        Assert.AreEqual("<noparse><</noparse>yz", RichFill("xyz", "<noparse><b>", 0.34f));

        // Text inside a <noparse> of from stays literal in the unrevealed part.
        Assert.AreEqual("x<noparse>b>", RichFill("<noparse><b></noparse>", "xyz", 0.34f));

        // Glyph tags inside <noparse> are plain characters too, and names ignore case.
        const string GLYPH_AS_TEXT = "<NoParse><br></NOPARSE>";

        Assert.AreEqual(GLYPH_AS_TEXT, RichFill(string.Empty, GLYPH_AS_TEXT, 1f, out var count));
        Assert.AreEqual(4, count);
    }

    [Test]
    public void Fill_RichText_DropsFromTags()
    {
        // length 3, count 1: the unrevealed part keeps from's characters without its <b> and </b>.
        Assert.AreEqual("ayz", RichFill("<b>xy</b>z", "abc", 0.34f));
        Assert.AreEqual("ayz", RichFill("x<b>y</b>z", "abc", 0.34f));
    }

    [Test]
    public void Fill_RichText_KeepsWhatIsNotATagAsCharacters()
    {
        // No '>' before the next '<', an empty "<>" and a tag longer than TMP's 127 characters are plain characters.
        Assert.AreEqual(4, SlotsOf("a<<b"));
        Assert.AreEqual(4, SlotsOf("a<>b"));
        Assert.AreEqual(2 + 128, SlotsOf("<" + new string('x', 128) + ">"));
        Assert.AreEqual(0, SlotsOf("<" + new string('x', 127) + ">"));
        Assert.AreEqual(5, SlotsOf("a < b"));
    }

    [Test]
    public void Fill_RichText_OffIsThePlainRule()
    {
        string[] texts = { "abcd", "<b>x</b>", "LEVEL 1", "LEVEL 22", "a\nb\rc", "hello", "hi", string.Empty, "<br>" };

        for (var f = 0; f < texts.Length; f++)
        {
            for (var t = 0; t < texts.Length; t++)
            {
                for (var i = 0; i <= 20; i++)
                {
                    var progress = i / 20f;
                    var plain = Fill(texts[f], texts[t], progress, ScrambleMode.All);

                    var rich = RichFill(
                          from: texts[f]
                        , to: texts[t]
                        , progress: progress
                        , slotCount: out var slotCount
                        , mode: ScrambleMode.All
                        , richText: false
                        , parseEscapes: false
                    );

                    Assert.AreEqual(plain, rich, $"'{texts[f]}' -> '{texts[t]}' at {progress}");
                    Assert.LessOrEqual(slotCount, Math.Max(texts[f].Length, texts[t].Length));
                }
            }
        }
    }

    [Test]
    public void Fill_SurrogatePairIsOneSlot()
    {
        const string SMILE = "😀";

        // length 2, count 1: the pair is revealed whole.
        Assert.AreEqual(SMILE + "b", RichFill("ab", SMILE + SMILE, 0.5f, out var slotCount, richText: false));
        Assert.AreEqual(2, slotCount);

        for (var i = 0; i <= 100; i++)
        {
            var text = RichFill("a" + SMILE + "c", SMILE + "x" + SMILE, i / 100f, out _, ScrambleMode.All);

            for (var c = 0; c < text.Length; c++)
            {
                if (char.IsHighSurrogate(text[c]))
                {
                    Assert.That(c + 1 < text.Length && char.IsLowSurrogate(text[c + 1]), $"lone high surrogate at {i}");
                    c++;
                }
                else
                {
                    Assert.IsFalse(char.IsLowSurrogate(text[c]), $"lone low surrogate at {i}");
                }
            }
        }
    }

    [Test]
    public void Fill_ParsedEscapeIsOneSlot()
    {
        // With parsed escapes, "\n" (two characters) is one newline slot and "A" one character.
        Assert.AreEqual("a\\nz", RichFill("xyz", "a\\nb", 0.34f, out var slotCount));
        Assert.AreEqual(3, slotCount);
        Assert.AreEqual(1, SlotsOf("\\u0041"));
        Assert.AreEqual(6, SlotsOf("\\u00z1"));

        // Without them, the backslash is a character of its own; "\u" escapes are always parsed, as in TMP.
        RichFill(string.Empty, "a\\nb", 1f, out var plainCount, parseEscapes: false);
        RichFill(string.Empty, "\\u0041", 1f, out var unicodeCount, parseEscapes: false);
        Assert.AreEqual(4, plainCount);
        Assert.AreEqual(1, unicodeCount);
    }

    [Test]
    public void Fill_RichText_CustomSetNeverForgesATag()
    {
        for (var i = 0; i < 100; i++)
        {
            var text = RichFill("----------", "ABCDEFGHIJ", i / 100f, out _, ScrambleMode.Custom, "<a>\\");

            Assert.That(text.IndexOfAny(new[] { '<', '>', '\\' }) < 0, $"'{text}' at {i}");
        }

        // A set of only tag characters means no scramble.
        Assert.AreEqual("WXcd", RichFill("abcd", "WXYZ", 0.5f, out _, ScrambleMode.Custom, "<>\\"));
    }

    [Test]
    public void Fill_RichText_SlotCountExcludesStyleTags()
    {
        const string TO = "<b>Hi</b> <i>there</i>";

        RichFill(string.Empty, TO, 1f, out var atEnd);
        RichFill(string.Empty, TO, 0.5f, out var midway);
        RichFill("<b>ab</b>", TO, 0f, out var atStart);

        Assert.AreEqual(8, atEnd);
        Assert.AreEqual(8, midway);
        Assert.AreEqual(2, atStart);
    }

    [Test]
    public void Fill_RichText_FitsTheRequiredLength()
    {
        const string SMILE = "😀";

        string[] texts = {
            "a" + SMILE,
            SMILE + "b",
            "<noparse><b>",
            "x<noparse>yz</noparse>",
            "<b>ab</b><br>",
            "\\n\\t" + SMILE,
            string.Empty,
            "C:\\Users",
            "abcnefgh",
            "\\\\\\",
            "a\\<b>n\\u",
            "<<<>>>",
            "x<" + new string('y', 126) + ">",
            "<" + new string('z', 127) + ">",
            "<" + new string('w', 128) + ">",
            "<noparse><noparse></noparse></noparse>",
            "\uD83D<b>\uDE00",
            "\\u0041\\U0001F600\\u00",
            "1 < 2 and 3 > 2",
            "<b><i>a</b></i><#F00>c</color>",
        };

        for (var f = 0; f < texts.Length; f++)
        {
            for (var t = 0; t < texts.Length; t++)
            {
                var required = TextTweenMath.RequiredLength(texts[f], texts[t], richText: true);
                var buffer = new char[required];

                for (var i = 0; i <= 50; i++)
                {
                    foreach (var parseEscapes in new[] { true, false })
                    {
                        foreach (var mode in new[] { ScrambleMode.All, ScrambleMode.None, ScrambleMode.Custom })
                        {
                            var length = TextTweenMath.Fill(
                                  buffer: buffer
                                , from: texts[f]
                                , to: texts[t]
                                , progress: i / 50f
                                , mode: mode
                                , custom: "<>\\nu"
                                , richText: true
                                , parseEscapes: parseEscapes
                                , slotCount: out _
                            );

                            Assert.LessOrEqual(length, required);
                        }
                    }
                }
            }
        }
    }

    [Test]
    public void Fill_RichText_DoesNotAllocate()
    {
        var buffer = new char[128];
        var from = "<noparse><b></noparse> LEVEL 1";
        var to = "<color=#F00>LEVEL</color><br>😀 \\n<sprite=2>";

        TextTweenMath.Fill(buffer, from, to, 0.5f, ScrambleMode.All, string.Empty, true, true, out _);

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < 100; i++)
        {
            TextTweenMath.Fill(buffer, from, to, i / 100f, ScrambleMode.All, string.Empty, true, true, out _);
            TextTweenMath.Fill(buffer, to, from, i / 100f, ScrambleMode.Custom, "<#>", true, false, out _);
        }

        Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
    }

    [Test]
    public void Fill_ScrambleStepReplacesTheRevealCount()
    {
        var byReveal = RichFill("----------", "ABCDEFGHIJ", 0.3f, out _, ScrambleMode.All);

        Assert.AreEqual(byReveal, FillWithStep(3));
        Assert.AreEqual(byReveal, FillWithStep(-1));
        Assert.AreNotEqual(byReveal, FillWithStep(4));
    }

    private static string FillWithStep(int scrambleStep)
    {
        var buffer = new char[64];

        var length = TextTweenMath.Fill(
              buffer: buffer
            , from: "----------"
            , to: "ABCDEFGHIJ"
            , progress: 0.3f
            , mode: ScrambleMode.All
            , custom: string.Empty
            , richText: true
            , parseEscapes: true
            , scrambleStep: scrambleStep
            , slotCount: out _
        );

        return new string(buffer, 0, length);
    }

    private static int SlotsOf(string text)
    {
        RichFill(string.Empty, text, 1f, out var slotCount);
        return slotCount;
    }
}
