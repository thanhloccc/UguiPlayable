using System;
using NUnit.Framework;
using UnityEngine;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Core;

public sealed class Vector4AccumulatorTests
{
    private const float TOLERANCE = 1e-5f;

    private static readonly Vector4 s_base = new(1f, 2f, 3f, 4f);
    private static readonly Vector4 s_current = new(10f, 20f, 30f, 40f);

    [Test]
    public void AxisMask_KeepsStoredValues()
    {
        Assert.AreEqual(0, (int)AxisMask.None);
        Assert.AreEqual(1, (int)AxisMask.X);
        Assert.AreEqual(2, (int)AxisMask.Y);
        Assert.AreEqual(4, (int)AxisMask.Z);
        Assert.AreEqual(8, (int)AxisMask.W);
    }

    [Test]
    public void Resolve_WithoutContribution_KeepsCurrent()
    {
        var accumulator = new Vector4Accumulator();

        Assert.IsFalse(accumulator.HasContribution);
        AssertVector(s_current, accumulator.Resolve(s_base, s_current));
    }

    [Test]
    public void Resolve_FullWeight_ReturnsValue()
    {
        var accumulator = new Vector4Accumulator();
        accumulator.Add(new Vector4(5f, 6f, 7f, 8f), 1f);

        AssertVector(new Vector4(5f, 6f, 7f, 8f), accumulator.Resolve(s_base, s_current));
    }

    [Test]
    public void Resolve_PartialWeight_BlendsFromBase()
    {
        var accumulator = new Vector4Accumulator();
        accumulator.Add(new Vector4(5f, 6f, 7f, 8f), 0.5f);

        AssertVector(new Vector4(3f, 4f, 5f, 6f), accumulator.Resolve(s_base, s_current));
    }

    [Test]
    public void Resolve_PartialWeight_BlendsFromHold()
    {
        var accumulator = new Vector4Accumulator();
        accumulator.OfferHold(Vector4.zero, 1.0);
        accumulator.Add(new Vector4(4f, 4f, 4f, 4f), 0.25f);

        AssertVector(Vector4.one, accumulator.Resolve(s_base, s_current));
    }

    [Test]
    public void OfferHold_LatestEndWins()
    {
        var accumulator = new Vector4Accumulator();
        accumulator.OfferHold(new Vector4(9f, 9f, 9f, 9f), 2.0);
        accumulator.OfferHold(new Vector4(1f, 1f, 1f, 1f), 1.0);

        AssertVector(new Vector4(9f, 9f, 9f, 9f), accumulator.Resolve(s_base, s_current));
    }

    [Test]
    public void IgnoredAxes_KeepCurrentPerAxis()
    {
        var accumulator = new Vector4Accumulator();
        accumulator.Add(new Vector4(5f, 6f, 7f, 8f), 1f, AxisMask.Y | AxisMask.W);

        Assert.AreEqual(AxisMask.X | AxisMask.Z, accumulator.ContributedAxes);
        AssertVector(new Vector4(5f, 20f, 7f, 40f), accumulator.Resolve(s_base, s_current));
    }

    [Test]
    public void IgnoredAxes_SkipHoldPerAxis()
    {
        var accumulator = new Vector4Accumulator();
        accumulator.OfferHold(new Vector4(5f, 6f, 7f, 8f), 1.0, AxisMask.W);

        Assert.AreEqual(AxisMask.X | AxisMask.Y | AxisMask.Z, accumulator.ContributedAxes);
        AssertVector(new Vector4(5f, 6f, 7f, 40f), accumulator.Resolve(s_base, s_current));
    }

    [Test]
    public void Evaluate_AbsoluteRelativeAndFromCurrent()
    {
        var from = new Vector4(0f, 0f, 0f, 0f);
        var to = new Vector4(2f, 4f, 6f, 8f);

        var absolute = new TweenChannel<Vector4>(from, to);
        AssertVector(new Vector4(1f, 2f, 3f, 4f), TweenChannelMath.Evaluate(absolute, 0.5f, s_base));

        var relative = new TweenChannel<Vector4>(from, to, ValueMode.Relative);
        AssertVector(s_base + new Vector4(1f, 2f, 3f, 4f), TweenChannelMath.Evaluate(relative, 0.5f, s_base));

        var fromCurrent = new TweenChannel<Vector4>(from, to, ValueMode.FromCurrent);
        AssertVector(s_base, TweenChannelMath.Evaluate(fromCurrent, 0f, s_base));

        fromCurrent.Capture(s_current);
        AssertVector(s_current, TweenChannelMath.Evaluate(fromCurrent, 0f, s_base));
        AssertVector(to, TweenChannelMath.Evaluate(fromCurrent, 1f, s_base));
    }

    [Test]
    public void AccumulateAndResolve_DoesNotAllocate()
    {
        var channel = new TweenChannel<Vector4>(Vector4.zero, Vector4.one);
        var accumulator = new Vector4Accumulator();

        // Warm up so JIT and static initialisation are not counted.
        Run(ref accumulator, channel);

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < 100; i++)
        {
            Run(ref accumulator, channel);
        }

        Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private static void Run(ref Vector4Accumulator accumulator, in TweenChannel<Vector4> channel)
    {
        accumulator.Reset();
        accumulator.Add(TweenChannelMath.Evaluate(channel, 0.5f, s_base), 0.5f, AxisMask.Z);
        accumulator.OfferHold(TweenChannelMath.Evaluate(channel, 1f, s_base), 1.0);
        accumulator.Resolve(s_base, s_current);
    }

    private static void AssertVector(Vector4 expected, Vector4 actual)
    {
        for (var i = 0; i < 4; i++)
        {
            Assert.That(actual[i], Is.EqualTo(expected[i]).Within(TOLERANCE), $"axis {i}");
        }
    }
}
