using System;
using NUnit.Framework;
using UnityEngine;
using ApexionGame.UguiPlayable.Easing;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Easing;

public class EaseFunctionsTests
{
    private const float TOLERANCE = 1e-4f;

    private static readonly Ease[] s_presets = GetPresets();

    private static readonly Ease[] s_parametric =
    {
        Ease.InElastic, Ease.OutElastic, Ease.InOutElastic,
        Ease.InBack, Ease.OutBack, Ease.InOutBack,
        Ease.InBounce, Ease.OutBounce, Ease.InOutBounce,
    };

    private static Ease[] GetPresets()
    {
        var presets = new Ease[(int)Ease.Custom];

        for (var i = 0; i < presets.Length; i++)
        {
            presets[i] = (Ease)i;
        }

        return presets;
    }

    [Test]
    public void Presets_HitZeroAtStartAndOneAtEnd([ValueSource(nameof(s_presets))] Ease ease)
    {
        Assert.That(EaseFunctions.Evaluate(ease, 0f), Is.EqualTo(0f).Within(TOLERANCE), $"{ease} at 0");
        Assert.That(EaseFunctions.Evaluate(ease, 1f), Is.EqualTo(1f).Within(TOLERANCE), $"{ease} at 1");
    }

    [Test]
    public void Linear_ReturnsInput()
    {
        Assert.That(EaseFunctions.Evaluate(Ease.Linear, 0.3f), Is.EqualTo(0.3f).Within(TOLERANCE));
    }

    [Test]
    public void Quad_MatchesKnownValues()
    {
        Assert.That(EaseFunctions.Evaluate(Ease.OutQuad, 0.5f), Is.EqualTo(0.75f).Within(TOLERANCE));
        Assert.That(EaseFunctions.Evaluate(Ease.InQuad, 0.5f), Is.EqualTo(0.25f).Within(TOLERANCE));
    }

    [Test]
    public void InOutCubic_PassesThroughHalfAtMidpoint()
    {
        Assert.That(EaseFunctions.Evaluate(Ease.InOutCubic, 0.5f), Is.EqualTo(0.5f).Within(TOLERANCE));
    }

    [Test]
    public void InSine_MatchesClosedForm()
    {
        var expected = 1f - Mathf.Cos(Mathf.PI / 4f);

        Assert.That(EaseFunctions.Evaluate(Ease.InSine, 0.5f), Is.EqualTo(expected).Within(TOLERANCE));
        Assert.That(expected, Is.EqualTo(0.29289f).Within(TOLERANCE));
    }

    [Test]
    public void OutBounce_MatchesKnownValue()
    {
        Assert.That(EaseFunctions.Evaluate(Ease.OutBounce, 0.5f), Is.EqualTo(0.765625f).Within(TOLERANCE));
    }

    [Test]
    public void OutBack_MatchesFormulaAndOvershoots()
    {
        const float C1 = 1.70158f;
        const float C3 = C1 + 1f;

        // 1 + c3 * (t - 1)^3 + c1 * (t - 1)^2 at t = 0.5
        var expected = 1f + C3 * -0.125f + C1 * 0.25f;

        Assert.That(EaseFunctions.Evaluate(Ease.OutBack, 0.5f), Is.EqualTo(expected).Within(TOLERANCE));
        Assert.That(EaseFunctions.Evaluate(Ease.OutBack, 0.8f), Is.GreaterThan(1f));
    }

    [Test]
    public void Custom_HasNoFormula()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EaseFunctions.Evaluate(Ease.Custom, 0.5f));
    }

    [Test]
    public void EaseSetting_Preset_UsesFormula()
    {
        var setting = new EaseSetting(Ease.OutQuad);

        Assert.That(setting.Ease, Is.EqualTo(Ease.OutQuad));
        Assert.That(setting.Evaluate(0.5f), Is.EqualTo(0.75f).Within(TOLERANCE));
    }

    [Test]
    public void EaseSetting_Custom_UsesCurve()
    {
        var setting = new EaseSetting(AnimationCurve.Linear(0f, 0f, 1f, 2f));

        Assert.That(setting.Ease, Is.EqualTo(Ease.Custom));
        Assert.That(setting.Evaluate(0.5f), Is.EqualTo(1f).Within(TOLERANCE));
    }

    [Test]
    public void EaseSetting_CustomWithoutCurve_FallsBackToLinear()
    {
        var setting = new EaseSetting(Ease.Custom);

        Assert.That(setting.Evaluate(0.3f), Is.EqualTo(0.3f).Within(TOLERANCE));
    }

    [Test]
    public void EaseSetting_Default_IsLinearWithCurve()
    {
        var setting = EaseSetting.Default;

        Assert.That(setting.Ease, Is.EqualTo(Ease.Linear));
        Assert.That(setting.Curve, Is.Not.Null);
        Assert.That(setting.Evaluate(0.25f), Is.EqualTo(0.25f).Within(TOLERANCE));
    }

    [Test]
    public void EaseSetting_DefaultStruct_IsLinear()
    {
        var setting = default(EaseSetting);

        Assert.That(setting.Evaluate(0.7f), Is.EqualTo(0.7f).Within(TOLERANCE));
    }

    [Test]
    public void Ease_SerializedValuesArePinned()
    {
        Assert.That((int)Ease.Linear, Is.EqualTo(0));
        Assert.That((int)Ease.OutBounce, Is.EqualTo(29));
        Assert.That((int)Ease.Custom, Is.EqualTo(31));
    }

    [Test]
    public void Customize_Off_IsBitExact([ValueSource(nameof(s_parametric))] Ease ease)
    {
        // Customize off keeps today's path, whatever the stored parameters are.
        var setting = new EaseSetting(ease);

        for (var i = 0; i <= 100; i++)
        {
            var t = i / 100f;

            Assert.IsTrue(EaseFunctions.Evaluate(ease, t) == setting.Evaluate(t), $"{ease} at {t}");
        }
    }

    [Test]
    public void Parametric_DefaultsMatchThePresets([ValueSource(nameof(s_parametric))] Ease ease)
    {
        var period = ease == Ease.InOutElastic ? 0.45f : 0.3f;
        var setting = new EaseSetting(ease, 1f, period);

        for (var i = 0; i <= 100; i++)
        {
            var t = i / 100f;

            Assert.That(setting.Evaluate(t), Is.EqualTo(EaseFunctions.Evaluate(ease, t)).Within(1e-5f), $"{ease} at {t}");
        }
    }

    [Test]
    public void Parametric_BackStrengthZero_HasNoOvershoot()
    {
        foreach (var ease in new[] { Ease.InBack, Ease.OutBack, Ease.InOutBack })
        {
            for (var i = 0; i <= 100; i++)
            {
                var value = EaseFunctions.Evaluate(ease, i / 100f, 0f, 0f);

                Assert.That(value, Is.InRange(-1e-5f, 1f + 1e-5f), $"{ease} at {i}");
            }
        }

        Assert.That(EaseFunctions.Evaluate(Ease.OutBack, 0.8f, 2f, 0f), Is.GreaterThan(EaseFunctions.Evaluate(Ease.OutBack, 0.8f)));
    }

    [Test]
    public void Parametric_ElasticPeriod_ChangesTheOscillation()
    {
        var short_ = EaseFunctions.Evaluate(Ease.OutElastic, 0.4f, 1f, 0.2f);
        var long_ = EaseFunctions.Evaluate(Ease.OutElastic, 0.4f, 1f, 0.6f);

        Assert.That(short_, Is.Not.EqualTo(long_).Within(1e-3f));
    }

    [Test]
    public void Parametric_BounceStrengthZero_DoesNotBounce()
    {
        // One fall to the end, no rebound: monotonic and never above 1.
        var previous = 0f;

        for (var i = 0; i <= 100; i++)
        {
            var value = EaseFunctions.Evaluate(Ease.OutBounce, i / 100f, 0f, 0f);

            Assert.That(value, Is.GreaterThanOrEqualTo(previous - 1e-6f), $"at {i}");
            Assert.That(value, Is.LessThanOrEqualTo(1f + 1e-6f), $"at {i}");
            previous = value;
        }
    }

    [Test]
    public void Parametric_HitsZeroAndOne(
          [ValueSource(nameof(s_parametric))] Ease ease
        , [Values(0f, 0.5f, 1f, 2f)] float strength
        , [Values(0f, 0.2f, 0.6f)] float period
    )
    {
        Assert.That(EaseFunctions.Evaluate(ease, 0f, strength, period), Is.EqualTo(0f).Within(TOLERANCE), $"{ease} at 0");
        Assert.That(EaseFunctions.Evaluate(ease, 1f, strength, period), Is.EqualTo(1f).Within(TOLERANCE), $"{ease} at 1");
    }

    [Test]
    public void Parametric_DoesNotAllocate()
    {
        var sink = 0f;

        for (var i = 0; i < s_parametric.Length; i++)
        {
            sink += EaseFunctions.Evaluate(s_parametric[i], 0.37f, 1.5f, 0.4f);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var run = 0; run < 1000; run++)
        {
            var ease = s_parametric[run % s_parametric.Length];
            sink += EaseFunctions.Evaluate(ease, (run % 100) / 100f, 1.5f, 0.4f);
        }

        var after = GC.GetAllocatedBytesForCurrentThread();

        Assert.That(after - before, Is.EqualTo(0L));
        Assert.That(float.IsNaN(sink), Is.False);
    }

    [Test]
    public void Evaluate_DoesNotAllocate()
    {
        var sink = 0f;

        for (var i = 0; i < s_presets.Length; i++)
        {
            sink += EaseFunctions.Evaluate(s_presets[i], 0.37f);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var run = 0; run < 1000; run++)
        {
            var ease = s_presets[run % s_presets.Length];
            sink += EaseFunctions.Evaluate(ease, (run % 100) / 100f);
        }

        var after = GC.GetAllocatedBytesForCurrentThread();

        Assert.That(after - before, Is.EqualTo(0L));
        Assert.That(float.IsNaN(sink), Is.False);
    }
}
