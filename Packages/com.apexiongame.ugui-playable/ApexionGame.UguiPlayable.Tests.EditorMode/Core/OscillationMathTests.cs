using System;
using NUnit.Framework;
using UnityEngine;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Core;

public sealed class OscillationMathTests
{
    [Test]
    public void TargetValues_AreStable()
    {
        Assert.AreEqual(0, (int)OscillationTarget.Position);
        Assert.AreEqual(1, (int)OscillationTarget.Rotation);
        Assert.AreEqual(2, (int)OscillationTarget.Scale);
    }

    [Test]
    public void Punch_IsZeroAtBothEnds()
    {
        Assert.That(OscillationMath.Punch(0f, 4f, 0.5f), Is.EqualTo(0f));
        Assert.That(OscillationMath.Punch(1f, 4f, 0.5f), Is.EqualTo(0f));
        Assert.That(OscillationMath.Punch(-1f, 4f, 0.5f), Is.EqualTo(0f));
        Assert.That(OscillationMath.Punch(2f, 4f, 0.5f), Is.EqualTo(0f));
    }

    [Test]
    public void Punch_MovesTowardPositiveFirst()
    {
        Assert.That(OscillationMath.Punch(0.01f, 4f, 0.5f), Is.GreaterThan(0f));
        Assert.That(OscillationMath.Punch(0.05f, 8f, 1f), Is.GreaterThan(0f));
    }

    [Test]
    public void Punch_DecaysOverTheClip()
    {
        var early = Mathf.Abs(OscillationMath.Punch(0.125f, 2f, 0.5f));
        var late = Mathf.Abs(OscillationMath.Punch(0.875f, 2f, 0.5f));

        Assert.That(late, Is.LessThan(early));
    }

    [Test]
    public void Shake_IsZeroAtBothEnds()
    {
        for (var fadeOut = 0; fadeOut < 2; fadeOut++)
        {
            var start = OscillationMath.Shake(0f, 10f, 3, 1f, fadeOut == 1);
            var end = OscillationMath.Shake(1f, 10f, 3, 1f, fadeOut == 1);

            Assert.That(start.sqrMagnitude, Is.EqualTo(0f));
            Assert.That(end.sqrMagnitude, Is.EqualTo(0f));
        }
    }

    [Test]
    public void Shake_SameSeed_GivesSameValues()
    {
        var first = OscillationMath.Shake(0.37f, 10f, 42, 0.5f, true);
        var second = OscillationMath.Shake(0.37f, 10f, 42, 0.5f, true);

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first.sqrMagnitude, Is.GreaterThan(0f));
    }

    [Test]
    public void Shake_DifferentSeed_GivesDifferentValues()
    {
        var first = OscillationMath.Shake(0.37f, 10f, 42, 1f, false);
        var second = OscillationMath.Shake(0.37f, 10f, 43, 1f, false);

        Assert.That(first, Is.Not.EqualTo(second));
    }

    [Test]
    public void Shake_RandomnessZero_MakesAllAxesEqual()
    {
        var value = OscillationMath.Shake(0.37f, 10f, 42, 0f, true);

        Assert.That(value.sqrMagnitude, Is.GreaterThan(0f));
        Assert.That(value.x, Is.EqualTo(value.y));
        Assert.That(value.y, Is.EqualTo(value.z));
    }

    [Test]
    public void Shake_RandomnessOne_MakesAxesIndependent()
    {
        var value = OscillationMath.Shake(0.37f, 10f, 42, 1f, false);

        Assert.That(value.x, Is.Not.EqualTo(value.y));
    }

    [Test]
    public void ShakeAxis_StaysInUnitRange()
    {
        for (var i = 0; i <= 200; i++)
        {
            var value = OscillationMath.ShakeAxis(i / 200f, 10f, 7, i % 4);

            Assert.That(value, Is.InRange(-1f, 1f));
        }
    }

    [Test]
    public void Evaluate_HonoursIgnoredAxesAndStrength()
    {
        var channel = OscillationChannel.Shake(
              OscillationTarget.Position
            , new Vector3(10f, 20f, 30f)
            , randomness: 1f
            , fadeOut: false
            , seed: 5
        );

        var free = OscillationMath.Evaluate(channel, 0.4f, true);
        Assert.That(free.x, Is.Not.EqualTo(0f));

        var punch = OscillationChannel.Punch(OscillationTarget.Scale, new Vector3(2f, 0f, 4f));
        var value = OscillationMath.Evaluate(punch, 0.05f, false);

        Assert.That(value.x, Is.GreaterThan(0f));
        Assert.That(value.y, Is.EqualTo(0f));
        Assert.That(value.z, Is.EqualTo(value.x * 2f).Within(1e-5f));
    }

    [Test]
    public void Evaluate_IgnoredAxis_IsZero()
    {
        var punch = OscillationChannel.Punch(OscillationTarget.Scale, Vector3.one);
        var shake = OscillationChannel.Shake(OscillationTarget.Position, Vector3.one, randomness: 1f);

        var ignoredPunch = OscillationMath.Evaluate(WithIgnored(punch), 0.05f, false);
        var ignoredShake = OscillationMath.Evaluate(WithIgnored(shake), 0.4f, true);

        Assert.That(ignoredPunch.y, Is.EqualTo(0f));
        Assert.That(ignoredPunch.x, Is.Not.EqualTo(0f));
        Assert.That(ignoredShake.y, Is.EqualTo(0f));
        Assert.That(ignoredShake.x, Is.Not.EqualTo(0f));
    }

    [Test]
    public void Evaluate_DoesNotAllocate()
    {
        var punch = OscillationChannel.Punch(OscillationTarget.Scale, Vector3.one);
        var shake = OscillationChannel.Shake(OscillationTarget.Position, Vector3.one);
        var sum = 0f;

        for (var i = 0; i < 10; i++)
        {
            var p = i / 10f;
            sum += OscillationMath.Evaluate(punch, p, false).x + OscillationMath.Evaluate(shake, p, true).x;
        }

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < 1000; i++)
        {
            var p = i / 1000f;
            sum += OscillationMath.Evaluate(punch, p, false).x + OscillationMath.Evaluate(shake, p, true).x;
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
        Assert.That(float.IsNaN(sum), Is.False);
    }

    // Builds a channel that ignores Y through serialization, since the fields are private.
    private static OscillationChannel WithIgnored(OscillationChannel channel)
    {
        var json = JsonUtility.ToJson(channel);
        var edited = json.Replace("\"_ignoredAxes\":0", "\"_ignoredAxes\":2");
        var ignored = JsonUtility.FromJson<OscillationChannel>(edited);

        Assert.That(ignored.IgnoredAxes, Is.EqualTo(AxisMask.Y));
        return ignored;
    }
}
