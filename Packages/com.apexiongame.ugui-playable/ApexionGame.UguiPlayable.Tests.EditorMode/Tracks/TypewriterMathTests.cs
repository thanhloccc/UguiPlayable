using System;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Tracks.Text;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Tracks;

public sealed class TypewriterMathTests
{
    private const string SENTENCE = "Hello, brave new world.";
    private const int SWEEP = 2000;

    [Test]
    public void Character_NoPause_EqualsTheFloorRule()
    {
        var info = Characters(SENTENCE);
        var plain = Characters("Hello World");

        for (var i = 0; i <= SWEEP; i++)
        {
            var ratio = i / (float)SWEEP;

            Assert.AreEqual(
                  TextTweenMath.CountOf(ratio, SENTENCE.Length)
                , TypewriterMath.VisibleCount(info, SENTENCE.Length, TypewriterUnit.Character, 0f, ratio)
                , $"ratio {ratio}"
            );

            // A pause changes nothing in a text with no punctuation, through the weighted scan.
            Assert.AreEqual(
                  TextTweenMath.CountOf(ratio, 11)
                , TypewriterMath.VisibleCount(plain, 11, TypewriterUnit.Character, 3f, ratio)
                , $"ratio {ratio} with a pause"
            );
        }
    }

    [Test]
    public void Word_RevealsPunctuationWithItsWord()
    {
        var info = Words(SENTENCE);
        var n = SENTENCE.Length;

        Assert.AreEqual(4, info.wordCount);
        Assert.AreEqual(0, Count(info, TypewriterUnit.Word, 0f, 0.24f));
        Assert.AreEqual(7, Count(info, TypewriterUnit.Word, 0f, 0.25f), "\"Hello, \" with its comma and space");
        Assert.AreEqual(13, Count(info, TypewriterUnit.Word, 0f, 0.5f));
        Assert.AreEqual(17, Count(info, TypewriterUnit.Word, 0f, 0.99f));
        Assert.AreEqual(n, Count(info, TypewriterUnit.Word, 0f, 1f), "the last word brings the full stop");

        var allowed = new HashSet<int> { 0, 7, 13, 17, n };

        for (var i = 0; i <= SWEEP; i++)
        {
            var count = Count(info, TypewriterUnit.Word, 0f, i / (float)SWEEP);

            Assert.IsTrue(allowed.Contains(count), $"{count} is not a word boundary");
        }
    }

    [Test]
    public void Word_NoWords_FallsBackToCharacters()
    {
        // TMP reports one word for a text with none (wordInfo[0] stays zeroed here).
        var info = Characters("... !!");
        info.wordCount = 1;

        for (var i = 0; i <= SWEEP; i++)
        {
            var ratio = i / (float)SWEEP;

            Assert.AreEqual(TextTweenMath.CountOf(ratio, 6), Count(info, TypewriterUnit.Word, 0f, ratio));
        }
    }

    [Test]
    public void NoWords_WithStaleWordInfo_FallsBackToCharacters()
    {
        // wordInfo[0] left over from an earlier "Word": in range, but no character of this text is a word character.
        var info = Characters("?!..");
        info.wordCount = 1;
        info.wordInfo = new TMP_WordInfo[1];
        info.wordInfo[0].firstCharacterIndex = 0;
        info.wordInfo[0].lastCharacterIndex = 3;
        info.wordInfo[0].characterCount = 4;

        for (var i = 0; i <= SWEEP; i++)
        {
            var ratio = i / (float)SWEEP;

            Assert.AreEqual(TextTweenMath.CountOf(ratio, 4), Count(info, TypewriterUnit.Word, 0f, ratio));
        }
    }

    [Test]
    public void Line_RevealsWholeLines()
    {
        var info = Lines("First line\nSecond", 0, 11);

        Assert.AreEqual(0, Count(info, TypewriterUnit.Line, 0f, 0.49f));
        Assert.AreEqual(11, Count(info, TypewriterUnit.Line, 0f, 0.5f), "the first line with its line feed");
        Assert.AreEqual(11, Count(info, TypewriterUnit.Line, 0f, 0.99f));
        Assert.AreEqual(17, Count(info, TypewriterUnit.Line, 0f, 1f));
    }

    [Test]
    public void Pause_DelaysTheNextUnit()
    {
        // "Wait. Go!" by character, pause 6: the full stop weighs 7, the rest 1; W = 15.
        var info = Characters("Wait. Go!");

        Assert.AreEqual(4, Count(info, TypewriterUnit.Character, 0f, 8f / 15f), "no pause: 4.8 of 9");
        Assert.AreEqual(5, Count(info, TypewriterUnit.Character, 6f, 5f / 15f), "the full stop shows at once");
        Assert.AreEqual(5, Count(info, TypewriterUnit.Character, 6f, 11.9f / 15f), "then the wait");
        Assert.AreEqual(6, Count(info, TypewriterUnit.Character, 6f, 12f / 15f), "the space after the wait");

        // By word: "Wait." weighs 7 and "Go!" 1, so "Go!" waits until the end.
        var words = Words("Wait. Go!");

        Assert.AreEqual(6, Count(words, TypewriterUnit.Word, 6f, 0.125f));
        Assert.AreEqual(6, Count(words, TypewriterUnit.Word, 6f, 0.99f));
        Assert.AreEqual(9, Count(words, TypewriterUnit.Word, 6f, 1f));

        // Only the last full stop of a run waits: at T = 4 of W = 8 the second one already shows.
        Assert.AreEqual(3, Count(Characters("a..b"), TypewriterUnit.Character, 4f, 0.5f));

        // A line that ends with punctuation before its line feed waits.
        var lines = Lines("One.\nTwo", 0, 5);

        Assert.AreEqual(5, Count(lines, TypewriterUnit.Line, 2f, 0.5f), "W = 4: the first line shows at 0.25");
        Assert.AreEqual(5, Count(lines, TypewriterUnit.Line, 2f, 0.99f));
    }

    [Test]
    public void Pause_NotAfterTheLastUnit()
    {
        var info = Characters("Go.");

        for (var i = 0; i <= SWEEP; i++)
        {
            var ratio = i / (float)SWEEP;

            Assert.AreEqual(TextTweenMath.CountOf(ratio, 3), Count(info, TypewriterUnit.Character, 10f, ratio));
        }

        // "Hi." weighs 1 + 2 and the last word "Go." 1: W = 4, so "Go." shows only at the end.
        var words = Words("Hi. Go.");

        Assert.AreEqual(4, Count(words, TypewriterUnit.Word, 2f, 0.25f));
        Assert.AreEqual(4, Count(words, TypewriterUnit.Word, 2f, 0.99f));
        Assert.AreEqual(7, Count(words, TypewriterUnit.Word, 2f, 1f));
    }

    [TestCase(TypewriterUnit.Character, 0f)]
    [TestCase(TypewriterUnit.Character, 3f)]
    [TestCase(TypewriterUnit.Word, 0f)]
    [TestCase(TypewriterUnit.Word, 2f)]
    [TestCase(TypewriterUnit.Line, 0f)]
    [TestCase(TypewriterUnit.Line, 1f)]
    public void Inverse_RoundTrips(TypewriterUnit unit, float pause)
    {
        var info = Words(SENTENCE);
        SetLines(info, 0, 13);

        var n = SENTENCE.Length;
        var reached = new HashSet<int>();

        for (var i = 0; i <= SWEEP; i++)
        {
            reached.Add(Count(info, unit, pause, i / (float)SWEEP));
        }

        foreach (var count in reached)
        {
            var ratio = TypewriterMath.UnitRatio(info, n, unit, pause, count);

            Assert.AreEqual(count, Count(info, unit, pause, ratio), $"count {count} through ratio {ratio}");
        }

        if (unit == TypewriterUnit.Word)
        {
            // Inside "brave": the ratio of the words before it.
            var ratio = TypewriterMath.UnitRatio(info, n, unit, pause, 9);

            Assert.AreEqual(7, Count(info, unit, pause, ratio));
        }
    }

    [Test]
    public void StaleArrays_DoNotThrow()
    {
        // Counts larger than the arrays, a text count larger than the characters, and no text info at all.
        var stale = new TMP_TextInfo {
            characterCount = 30,
            wordCount = 5,
            lineCount = 9,
            characterInfo = new TMP_CharacterInfo[2],
            wordInfo = new TMP_WordInfo[1],
            lineInfo = new TMP_LineInfo[1],
        };

        stale.characterInfo[0].character = 'a';
        stale.characterInfo[1].character = '.';
        stale.wordInfo[0].lastCharacterIndex = 25;
        stale.lineInfo[0].lastCharacterIndex = 40;

        var units = (TypewriterUnit[])Enum.GetValues(typeof(TypewriterUnit));

        foreach (var info in new[] { stale, null, new TMP_TextInfo() })
        {
            foreach (var unit in units)
            {
                for (var i = 0; i <= 20; i++)
                {
                    var ratio = i / 20f;
                    var count = -1;
                    var back = -1f;

                    Assert.DoesNotThrow(() => {
                        count = TypewriterMath.VisibleCount(info, 20, unit, 2f, ratio);
                        back = TypewriterMath.UnitRatio(info, 20, unit, 2f, i);
                    });

                    Assert.That(count, Is.InRange(0, 20));
                    Assert.That(back, Is.InRange(0f, 1f));
                }
            }
        }
    }

    [Test]
    public void DoesNotAllocate()
    {
        var info = Words(SENTENCE);
        SetLines(info, 0, 13);

        var n = SENTENCE.Length;
        var sum = 0f;

        for (var i = 0; i < 10; i++)
        {
            sum += Exercise(info, n, i / 10f);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < 1000; i++)
        {
            sum += Exercise(info, n, i / 1000f);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
        Assert.That(sum, Is.GreaterThan(0f));
    }

    private static float Exercise(TMP_TextInfo info, int n, float ratio)
    {
        var sum = 0f;

        for (var unit = TypewriterUnit.Character; unit <= TypewriterUnit.Line; unit++)
        {
            var count = TypewriterMath.VisibleCount(info, n, unit, 2f, ratio);

            sum += count + TypewriterMath.UnitRatio(info, n, unit, 2f, count);
        }

        return sum;
    }

    private static int Count(TMP_TextInfo info, TypewriterUnit unit, float pause, float ratio)
        => TypewriterMath.VisibleCount(info, info.characterCount, unit, pause, ratio);

    private static TMP_TextInfo Characters(string text)
    {
        var info = new TMP_TextInfo {
            characterCount = text.Length,
            characterInfo = new TMP_CharacterInfo[text.Length],
        };

        for (var i = 0; i < text.Length; i++)
        {
            info.characterInfo[i].character = text[i];
        }

        return info;
    }

    // Words by TMP's rule: runs of letters, digits and hyphens.
    private static TMP_TextInfo Words(string text)
    {
        var info = Characters(text);
        var words = new List<TMP_WordInfo>();
        var first = -1;

        for (var i = 0; i <= text.Length; i++)
        {
            var isWord = i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '-');

            if (isWord && first < 0)
            {
                first = i;
            }
            else if (isWord == false && first >= 0)
            {
                words.Add(new TMP_WordInfo {
                    firstCharacterIndex = first,
                    lastCharacterIndex = i - 1,
                    characterCount = i - first,
                });

                first = -1;
            }
        }

        info.wordInfo = words.ToArray();
        info.wordCount = words.Count;
        return info;
    }

    private static TMP_TextInfo Lines(string text, params int[] starts)
    {
        var info = Characters(text);
        SetLines(info, starts);
        return info;
    }

    private static void SetLines(TMP_TextInfo info, params int[] starts)
    {
        info.lineInfo = new TMP_LineInfo[starts.Length];
        info.lineCount = starts.Length;

        for (var k = 0; k < starts.Length; k++)
        {
            info.lineInfo[k].firstCharacterIndex = starts[k];
            info.lineInfo[k].lastCharacterIndex = k + 1 < starts.Length ? starts[k + 1] - 1 : info.characterCount - 1;
        }
    }
}
