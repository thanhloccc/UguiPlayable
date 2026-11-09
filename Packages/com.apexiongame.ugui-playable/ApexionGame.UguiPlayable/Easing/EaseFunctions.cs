using UnityEngine;

namespace ApexionGame.UguiPlayable.Easing
{
    /// <summary>
    /// Evaluates the <see cref="Ease"/> presets with the standard Penner formulas.
    /// </summary>
    /// <remarks>
    /// Allocation-free and thread-safe.
    /// </remarks>
    public static class EaseFunctions
    {
        private const float BACK_C1 = 1.70158f;
        private const float BACK_C2 = BACK_C1 * 1.525f;
        private const float BACK_C3 = BACK_C1 + 1f;
        private const float ELASTIC_C4 = 2f * Mathf.PI / 3f;
        private const float ELASTIC_C5 = 2f * Mathf.PI / 4.5f;
        private const float BOUNCE_N1 = 7.5625f;
        private const float BOUNCE_D1 = 2.75f;

        /// <summary>
        /// Maps the normalized time <paramref name="t"/> through the preset <paramref name="ease"/>.
        /// </summary>
        /// <remarks>
        /// <paramref name="t"/> is expected in [0, 1] and is not clamped, so callers may pass values outside that
        /// range to extrapolate.
        /// </remarks>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown for <see cref="Ease.Custom"/> or an undefined value, which have no formula.
        /// </exception>
        public static float Evaluate(Ease ease, float t)
        {
            switch (ease)
            {
                case Ease.Linear: return t;
                case Ease.InSine: return 1f - Mathf.Cos(t * Mathf.PI / 2f);
                case Ease.OutSine: return Mathf.Sin(t * Mathf.PI / 2f);
                case Ease.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
                case Ease.InQuad: return t * t;
                case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
                case Ease.InOutQuad: return InOutPower(t, 2f);
                case Ease.InCubic: return t * t * t;
                case Ease.OutCubic: return OutPower(t, 3f);
                case Ease.InOutCubic: return InOutPower(t, 3f);
                case Ease.InQuart: return t * t * t * t;
                case Ease.OutQuart: return OutPower(t, 4f);
                case Ease.InOutQuart: return InOutPower(t, 4f);
                case Ease.InQuint: return t * t * t * t * t;
                case Ease.OutQuint: return OutPower(t, 5f);
                case Ease.InOutQuint: return InOutPower(t, 5f);
                case Ease.InExpo: return InExpo(t);
                case Ease.OutExpo: return OutExpo(t);
                case Ease.InOutExpo: return InOutExpo(t);
                case Ease.InCirc: return 1f - Mathf.Sqrt(1f - t * t);
                case Ease.OutCirc: return Mathf.Sqrt(1f - (t - 1f) * (t - 1f));
                case Ease.InOutCirc: return InOutCirc(t);
                case Ease.InElastic: return InElastic(t);
                case Ease.OutElastic: return OutElastic(t);
                case Ease.InOutElastic: return InOutElastic(t);
                case Ease.InBack: return BACK_C3 * t * t * t - BACK_C1 * t * t;
                case Ease.OutBack: return OutBack(t);
                case Ease.InOutBack: return InOutBack(t);
                case Ease.InBounce: return 1f - OutBounce(1f - t);
                case Ease.OutBounce: return OutBounce(t);
                case Ease.InOutBounce: return InOutBounce(t);

                default:
                {
                    ThrowHelper.ThrowEaseHasNoFormula(ease);
                    return t;
                }
            }
        }

        /// <summary>
        /// Maps <paramref name="t"/> through a Back, Elastic or Bounce preset with its parameters changed. Any
        /// other ease ignores <paramref name="strength"/> and <paramref name="period"/> and behaves like
        /// <see cref="Evaluate(Ease, float)"/>.
        /// </summary>
        /// <param name="ease">The preset.</param>
        /// <param name="t">The normalized time, not clamped.</param>
        /// <param name="strength">
        /// Back: the overshoot, scaled from 1.70158 (negative is 0, no overshoot). Elastic: the amplitude (below 1 is
        /// 1). Bounce: the bounciness, where 1 matches the preset and 0 does not bounce.
        /// </param>
        /// <param name="period">
        /// Elastic only: the length of one oscillation, as a fraction of the duration (of each half for In-Out). 0 or
        /// less uses 0.3 (0.45 for In-Out).
        /// </param>
        /// <remarks>
        /// With <c>strength</c> 1 and the preset period the result equals the preset within float tolerance.
        /// </remarks>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown for <see cref="Ease.Custom"/> or an undefined value, which have no formula.
        /// </exception>
        public static float Evaluate(Ease ease, float t, float strength, float period)
        {
            switch (ease)
            {
                case Ease.InElastic: return InElastic(t, strength, period);
                case Ease.OutElastic: return OutElastic(t, strength, period);
                case Ease.InOutElastic: return InOutElastic(t, strength, period);
                case Ease.InBack: return InBack(t, BackOvershoot(strength));
                case Ease.OutBack: return OutBack(t, BackOvershoot(strength));
                case Ease.InOutBack: return InOutBack(t, BackOvershoot(strength) * 1.525f);
                case Ease.InBounce: return 1f - OutBounce(1f - t, strength);
                case Ease.OutBounce: return OutBounce(t, strength);
                case Ease.InOutBounce:
                    return t < 0.5f
                        ? (1f - OutBounce(1f - 2f * t, strength)) / 2f
                        : (1f + OutBounce(2f * t - 1f, strength)) / 2f;
                default: return Evaluate(ease, t);
            }
        }

        private static float BackOvershoot(float strength)
            => BACK_C1 * Mathf.Max(0f, strength);

        private static float InBack(float t, float s)
            => (s + 1f) * t * t * t - s * t * t;

        private static float OutBack(float t, float s)
        {
            var u = t - 1f;
            return 1f + (s + 1f) * u * u * u + s * u * u;
        }

        private static float InOutBack(float t, float s)
        {
            if (t < 0.5f)
            {
                var a = 2f * t;
                return a * a * ((s + 1f) * a - s) / 2f;
            }

            var b = 2f * t - 2f;
            return (b * b * ((s + 1f) * b + s) + 2f) / 2f;
        }

        // The sine is shifted by phase = period / 2pi * asin(1 / amplitude), so a larger amplitude still starts
        // from rest. Returns the angular frequency 2pi / period through omega.
        private static float ElasticPhase(
              float strength
            , float period
            , float defaultPeriod
            , out float amplitude
            , out float omega
        )
        {
            amplitude = Mathf.Max(1f, strength);

            var p = period > 0f ? period : defaultPeriod;
            omega = 2f * Mathf.PI / p;

            return p / (2f * Mathf.PI) * Mathf.Asin(1f / amplitude);
        }

        private static float InElastic(float t, float strength, float period)
        {
            if (t <= 0f)
            {
                return 0f;
            }

            if (t >= 1f)
            {
                return 1f;
            }

            var phase = ElasticPhase(strength, period, 0.3f, out var amplitude, out var omega);
            return -amplitude * Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((t - 1f - phase) * omega);
        }

        private static float OutElastic(float t, float strength, float period)
        {
            if (t <= 0f)
            {
                return 0f;
            }

            if (t >= 1f)
            {
                return 1f;
            }

            var phase = ElasticPhase(strength, period, 0.3f, out var amplitude, out var omega);
            return amplitude * Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - phase) * omega) + 1f;
        }

        private static float InOutElastic(float t, float strength, float period)
        {
            if (t <= 0f)
            {
                return 0f;
            }

            if (t >= 1f)
            {
                return 1f;
            }

            var phase = ElasticPhase(strength, period, 0.45f, out var amplitude, out var omega);
            var x = 2f * t - 1f;
            var wave = amplitude * Mathf.Sin((x - phase) * omega);

            return t < 0.5f
                ? -(Mathf.Pow(2f, 10f * x) * wave) / 2f
                : Mathf.Pow(2f, -10f * x) * wave / 2f + 1f;
        }

        // Three bounces whose widths shrink by e and heights by e^2. In units of x = t * total, the first fall is
        // x^2 on [0, 1] and bounce k is 1 - e^(2k) + (x - center)^2 over a width of 2 * e^k.
        private static float OutBounce(float t, float strength)
        {
            var e1 = Mathf.Clamp(0.5f * strength, 0f, 0.99f);
            var e2 = e1 * e1;
            var e3 = e2 * e1;
            var x = t * (1f + 2f * (e1 + e2 + e3));

            if (x < 1f)
            {
                return x * x;
            }

            var start2 = 1f + 2f * e1;
            var start3 = start2 + 2f * e2;

            if (x < start2)
            {
                var d = x - (1f + e1);
                return 1f - e2 + d * d;
            }

            if (x < start3)
            {
                var d = x - (start2 + e2);
                return 1f - e2 * e2 + d * d;
            }

            var last = x - (start3 + e3);
            return 1f - e3 * e3 + last * last;
        }

        private static float OutPower(float t, float power)
            => 1f - Mathf.Pow(1f - t, power);

        private static float InOutPower(float t, float power)
            => t < 0.5f
                ? Mathf.Pow(2f, power - 1f) * Mathf.Pow(t, power)
                : 1f - Mathf.Pow(-2f * t + 2f, power) / 2f;

        private static float InExpo(float t)
            => t <= 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f);

        private static float OutExpo(float t)
            => t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);

        private static float InOutExpo(float t)
        {
            if (t <= 0f)
            {
                return 0f;
            }

            if (t >= 1f)
            {
                return 1f;
            }

            return t < 0.5f
                ? Mathf.Pow(2f, 20f * t - 10f) / 2f
                : (2f - Mathf.Pow(2f, -20f * t + 10f)) / 2f;
        }

        private static float InOutCirc(float t)
            => t < 0.5f
                ? (1f - Mathf.Sqrt(1f - (2f * t) * (2f * t))) / 2f
                : (Mathf.Sqrt(1f - (-2f * t + 2f) * (-2f * t + 2f)) + 1f) / 2f;

        private static float InElastic(float t)
        {
            if (t <= 0f)
            {
                return 0f;
            }

            if (t >= 1f)
            {
                return 1f;
            }

            return -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((10f * t - 10.75f) * ELASTIC_C4);
        }

        private static float OutElastic(float t)
        {
            if (t <= 0f)
            {
                return 0f;
            }

            if (t >= 1f)
            {
                return 1f;
            }

            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((10f * t - 0.75f) * ELASTIC_C4) + 1f;
        }

        private static float InOutElastic(float t)
        {
            if (t <= 0f)
            {
                return 0f;
            }

            if (t >= 1f)
            {
                return 1f;
            }

            return t < 0.5f
                ? -(Mathf.Pow(2f, 20f * t - 10f) * Mathf.Sin((20f * t - 11.125f) * ELASTIC_C5)) / 2f
                : Mathf.Pow(2f, -20f * t + 10f) * Mathf.Sin((20f * t - 11.125f) * ELASTIC_C5) / 2f + 1f;
        }

        private static float OutBack(float t)
        {
            var u = t - 1f;
            return 1f + BACK_C3 * u * u * u + BACK_C1 * u * u;
        }

        private static float InOutBack(float t)
        {
            if (t < 0.5f)
            {
                var a = 2f * t;
                return a * a * ((BACK_C2 + 1f) * a - BACK_C2) / 2f;
            }

            var b = 2f * t - 2f;
            return (b * b * ((BACK_C2 + 1f) * b + BACK_C2) + 2f) / 2f;
        }

        private static float OutBounce(float t)
        {
            if (t < 1f / BOUNCE_D1)
            {
                return BOUNCE_N1 * t * t;
            }

            if (t < 2f / BOUNCE_D1)
            {
                t -= 1.5f / BOUNCE_D1;
                return BOUNCE_N1 * t * t + 0.75f;
            }

            if (t < 2.5f / BOUNCE_D1)
            {
                t -= 2.25f / BOUNCE_D1;
                return BOUNCE_N1 * t * t + 0.9375f;
            }

            t -= 2.625f / BOUNCE_D1;
            return BOUNCE_N1 * t * t + 0.984375f;
        }

        private static float InOutBounce(float t)
            => t < 0.5f
                ? (1f - OutBounce(1f - 2f * t)) / 2f
                : (1f + OutBounce(2f * t - 1f)) / 2f;
    }
}
