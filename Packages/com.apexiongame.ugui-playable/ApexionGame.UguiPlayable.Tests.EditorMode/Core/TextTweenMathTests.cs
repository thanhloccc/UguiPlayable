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

    [Test]
    public void RequiredLength_IsTheLongerString()
    {
        Assert.AreEqual(5, TextTweenMath.RequiredLength("abc", "hello"));
        Assert.AreEqual(3, TextTweenMath.RequiredLength("abc", null));
        Assert.AreEqual(0, TextTweenMath.RequiredLength(null, null));
    }

    [Test]
    public void Fill_AtZeroRevealsOnlyTheFirstCharacter()
    {
        Assert.AreEqual("Wbcd", Fill("abcd", "Wxyz", 0f));
    }

    [Test]
    public void Fill_AtHalfRevealsHalf()
    {
        // length 4, reveal = (int)(3 * 0.5) = 1.
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
        // length 5, reveal = (int)(4 * 0.5) = 2: positions 0..2 are the target; 3 differs (scrambled); 4 is equal.
        var text = Fill("abcdE", "XYZWE", 0.5f, ScrambleMode.Custom, CUSTOM);

        Assert.AreEqual("XYZ", text.Substring(0, 3));
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
        var reveal = (int)(9 * progress);

        for (var i = reveal + 1; i < text.Length; i++)
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
        Assert.AreEqual("x\nc", Fill("a\nc", "xyz", 0f, ScrambleMode.None));
        Assert.AreEqual('\n', Fill("ab\ncd", "xyzwq", 0.2f, ScrambleMode.All)[2]);
        Assert.AreEqual('\n', Fill("abcde", "xy\nwq", 0.2f, ScrambleMode.All)[2]);
        Assert.AreEqual('\n', Fill("ab\rcd", "xyzwq", 0.2f, ScrambleMode.All)[2]);
    }

    [Test]
    public void Fill_PadsAShorterTargetWithSpacesWhileRevealing()
    {
        // length 5, reveal 3 at 0.99: revealed positions past "hi" show a space; position 4 still keeps "o".
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
}
