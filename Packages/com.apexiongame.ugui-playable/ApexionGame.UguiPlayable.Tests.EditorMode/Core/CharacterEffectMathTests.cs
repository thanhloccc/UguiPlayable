using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Tracks.Text;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Core;

public sealed class CharacterEffectMathTests
{
    private const float TOLERANCE = 1e-4f;

    [Test]
    public void EnumValues_AreStable()
    {
        Assert.AreEqual(0, (int)CharacterOrder.Forward);
        Assert.AreEqual(1, (int)CharacterOrder.Backward);
        Assert.AreEqual(2, (int)CharacterOrder.CenterOut);
        Assert.AreEqual(3, (int)CharacterOrder.EdgesIn);
        Assert.AreEqual(4, (int)CharacterOrder.Random);
        Assert.AreEqual(0, (int)CharacterPivot.Center);
        Assert.AreEqual(1, (int)CharacterPivot.Baseline);
        Assert.AreEqual(0, (int)WaveShape.Sine);
        Assert.AreEqual(1, (int)WaveShape.Bounce);
    }

    [TestCase(CharacterOrder.Forward, new[] { 0, 1, 2, 3, 4 })]
    [TestCase(CharacterOrder.Backward, new[] { 4, 3, 2, 1, 0 })]
    [TestCase(CharacterOrder.CenterOut, new[] { 4, 2, 0, 2, 4 })]
    [TestCase(CharacterOrder.EdgesIn, new[] { 0, 2, 4, 2, 0 })]
    public void Rank_FollowsTheOrder(CharacterOrder order, int[] expected)
    {
        var ranks = Enumerable.Range(0, 5).Select(k => CharacterEffectMath.Rank(order, k, 5)).ToArray();

        Assert.That(ranks, Is.EqualTo(expected));
    }

    [Test]
    public void Rank_CenterOut_WithAnEvenCount_StartsWithTheTwoMiddleCharacters()
    {
        var ranks = Enumerable.Range(0, 4).Select(k => CharacterEffectMath.Rank(CharacterOrder.CenterOut, k, 4));

        Assert.That(ranks, Is.EqualTo(new[] { 3, 1, 1, 3 }));
    }

    [Test]
    public void FillRanks_Random_IsAPermutation_AndDeterministicPerSeed()
    {
        const int COUNT = 20;
        var indices = Enumerable.Range(10, COUNT).ToArray();

        var first = FillRandom(indices, seed: 7);
        var again = FillRandom(indices, seed: 7);
        var other = FillRandom(indices, seed: 8);

        Assert.That(first.OrderBy(r => r), Is.EqualTo(Enumerable.Range(0, COUNT)), "every rank once");
        Assert.That(again, Is.EqualTo(first), "same seed, same order");
        Assert.That(other, Is.Not.EqualTo(first), "another seed, another order");
        Assert.That(first, Is.Not.EqualTo(Enumerable.Range(0, COUNT).ToArray()), "shuffled");
    }

    [Test]
    public void FillRanks_OtherOrders_MatchRank()
    {
        var indices = new[] { 0, 2, 3, 5 };
        var ranks = new int[4];

        CharacterEffectMath.FillRanks(CharacterOrder.Backward, 0, indices, 4, ranks, new uint[4], new int[4]);

        Assert.That(ranks, Is.EqualTo(new[] { 3, 2, 1, 0 }));
    }

    [Test]
    public void Normalize_IsZeroForOneCharacter_AndSpansZeroToOne()
    {
        Assert.That(CharacterEffectMath.Normalize(0, 1), Is.EqualTo(0f));
        Assert.That(CharacterEffectMath.Normalize(0, 0), Is.EqualTo(0f));
        Assert.That(CharacterEffectMath.Normalize(0, 5), Is.EqualTo(0f));
        Assert.That(CharacterEffectMath.Normalize(4, 5), Is.EqualTo(1f));
        Assert.That(CharacterEffectMath.Normalize(2, 5), Is.EqualTo(0.5f).Within(TOLERANCE));
    }

    [Test]
    public void CharacterProgress_StaggerEndpoints()
    {
        // The first character starts at once and ends at 1 - s; the last starts at s and ends at 1.
        Assert.That(CharacterEffectMath.CharacterProgress(0f, 0.5f, 0f), Is.EqualTo(0f));
        Assert.That(CharacterEffectMath.CharacterProgress(0.5f, 0.5f, 0f), Is.EqualTo(1f).Within(TOLERANCE));
        Assert.That(CharacterEffectMath.CharacterProgress(0.5f, 0.5f, 1f), Is.EqualTo(0f).Within(TOLERANCE));
        Assert.That(CharacterEffectMath.CharacterProgress(1f, 0.5f, 1f), Is.EqualTo(1f).Within(TOLERANCE));
        Assert.That(CharacterEffectMath.CharacterProgress(0.75f, 0.5f, 1f), Is.EqualTo(0.5f).Within(TOLERANCE));
    }

    [Test]
    public void CharacterProgress_WithoutStagger_IsTheClipProgress()
    {
        Assert.That(CharacterEffectMath.CharacterProgress(0.3f, 0f, 1f), Is.EqualTo(0.3f).Within(TOLERANCE));
        Assert.That(CharacterEffectMath.CharacterProgress(0.3f, 0f, 0f), Is.EqualTo(0.3f).Within(TOLERANCE));
    }

    [Test]
    public void CharacterProgress_ClampsTheStagger()
    {
        var atMax = CharacterEffectMath.CharacterProgress(1f, TmpCharacterBehaviour.MAX_STAGGER, 1f);
        var above = CharacterEffectMath.CharacterProgress(1f, 5f, 1f);

        Assert.That(atMax, Is.EqualTo(1f).Within(TOLERANCE));
        Assert.That(above, Is.EqualTo(atMax));
    }

    [Test]
    public void Pivot_CenterAndBaseline()
    {
        var bottomLeft = new Vector3(10f, -4f, 0f);
        var topRight = new Vector3(20f, 16f, 0f);

        var center = CharacterEffectMath.Pivot(CharacterPivot.Center, bottomLeft, topRight, 0f);
        var baseline = CharacterEffectMath.Pivot(CharacterPivot.Baseline, bottomLeft, topRight, 0f);

        Assert.That(center, Is.EqualTo(new Vector2(15f, 6f)));
        Assert.That(baseline, Is.EqualTo(new Vector2(15f, 0f)));
    }

    [Test]
    public void Transform_IsIdentityAtRest()
    {
        var vertex = new Vector3(3f, 4f, 1f);

        var result = CharacterEffectMath.Transform(vertex, new Vector2(1f, 1f), Vector2.zero, 0f, 1f, Vector2.one);

        Assert.That(result, Is.EqualTo(vertex));
    }

    [Test]
    public void Transform_ScalesThenRotatesAboutThePivot_ThenOffsets()
    {
        // (3, 1) about (1, 1): d = (2, 0); scale 2 → (4, 0); +90° → (0, 4); + pivot + offset (10, 0).
        var vertex = new Vector3(3f, 1f, 5f);
        var radians = 90f * Mathf.Deg2Rad;

        var result = CharacterEffectMath.Transform(
              vertex
            , new Vector2(1f, 1f)
            , new Vector2(10f, 0f)
            , Mathf.Sin(radians)
            , Mathf.Cos(radians)
            , new Vector2(2f, 2f)
        );

        Assert.That(result.x, Is.EqualTo(11f).Within(TOLERANCE));
        Assert.That(result.y, Is.EqualTo(5f).Within(TOLERANCE));
        Assert.That(result.z, Is.EqualTo(5f), "Z is kept");
    }

    [Test]
    public void Tint_MultipliesTheColorAndTheAlpha()
    {
        var source = new Color32(200, 100, 50, 255);

        Assert.That(CharacterEffectMath.Tint(source, Color.white, 1f), Is.EqualTo(source));

        var tinted = CharacterEffectMath.Tint(source, new Color(0.5f, 1f, 0f, 1f), 0.5f);

        Assert.That(tinted, Is.EqualTo(new Color32(100, 100, 0, 128)));
    }

    [Test]
    public void Wave_SineAndBounce()
    {
        // Phase = cycles·p − phase·rank = 1·0.25 − 0 = 0.25 → sin(π/2) = 1.
        Assert.That(CharacterEffectMath.Wave(WaveShape.Sine, 1f, 0.25f, 0.25f, 0), Is.EqualTo(1f).Within(TOLERANCE));

        // Rank 2 shifts by 0.5 cycle: sin(2π·(0.25 − 0.5)) = −1; Bounce keeps it upward.
        Assert.That(CharacterEffectMath.Wave(WaveShape.Sine, 1f, 0.25f, 0.25f, 2), Is.EqualTo(-1f).Within(TOLERANCE));
        Assert.That(CharacterEffectMath.Wave(WaveShape.Bounce, 1f, 0.25f, 0.25f, 2), Is.EqualTo(1f).Within(TOLERANCE));
    }

    [Test]
    public void Shake_IsDeterministic_DiffersPerCharacter_AndHonoursIgnoredAxes()
    {
        var channel = OscillationChannel.Shake(OscillationTarget.Position, new Vector3(5f, 5f, 5f), fadeOut: false);

        var a = CharacterEffectMath.Shake(channel, 0.37f, 3);
        var again = CharacterEffectMath.Shake(channel, 0.37f, 3);
        var other = CharacterEffectMath.Shake(channel, 0.37f, 4);

        Assert.That(again, Is.EqualTo(a));
        Assert.That(other, Is.Not.EqualTo(a));
        Assert.That(CharacterEffectMath.Shake(channel, 0f, 3), Is.EqualTo(Vector3.zero), "0 at the clip start");
    }

    [Test]
    public void Hash_IsStable()
    {
        Assert.AreEqual(CharacterEffectMath.Hash(1, 2), CharacterEffectMath.Hash(1, 2));
        Assert.AreNotEqual(CharacterEffectMath.Hash(1, 2), CharacterEffectMath.Hash(1, 3));
        Assert.AreNotEqual(CharacterEffectMath.Hash(1, 2), CharacterEffectMath.Hash(2, 2));
    }

    private static int[] FillRandom(int[] indices, int seed)
    {
        var count = indices.Length;
        var ranks = new int[count];

        CharacterEffectMath.FillRanks(
              CharacterOrder.Random
            , seed
            , indices
            , count
            , ranks
            , new uint[count]
            , new int[count]
        );

        return ranks;
    }
}
