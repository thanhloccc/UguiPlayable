using System;
using UnityEngine;

namespace ApexionGame.UguiPlayable.Easing
{
    /// <summary>
    /// Holds either an <see cref="Ease"/> preset or a custom <see cref="AnimationCurve"/>.
    /// </summary>
    [Serializable]
    public struct EaseSetting
    {
        [SerializeField]
        private Ease _ease;

        [SerializeField]
        private AnimationCurve _curve;

        [SerializeField]
        private bool _customize;

        [SerializeField]
        private float _strength;

        [SerializeField]
        private float _period;

        /// <summary>
        /// Creates a setting that uses the preset <paramref name="ease"/>.
        /// </summary>
        public EaseSetting(Ease ease)
        {
            _ease = ease;
            _curve = null;
            _customize = false;
            _strength = 0f;
            _period = 0f;
        }

        /// <summary>
        /// Creates a customized setting for a Back, Elastic or Bounce <paramref name="ease"/>.
        /// </summary>
        /// <param name="ease">The preset. The parameters apply only to the Back, Elastic and Bounce families.</param>
        /// <param name="strength">
        /// Back: the overshoot (1 is the preset, 0 none). Elastic: the amplitude (1 is the preset, below 1 is
        /// treated as 1). Bounce: the bounciness (1 is the preset, 0 does not bounce).
        /// </param>
        /// <param name="period">
        /// Elastic only: the length of one oscillation, as a fraction of the duration (of each half for In-Out). 0
        /// or less uses the preset (0.3, or 0.45 for In-Out).
        /// </param>
        public EaseSetting(Ease ease, float strength, float period)
        {
            _ease = ease;
            _curve = null;
            _customize = true;
            _strength = strength;
            _period = period;
        }

        /// <summary>
        /// Creates a setting that uses <paramref name="curve"/> and sets the ease to <see cref="Ease.Custom"/>.
        /// </summary>
        public EaseSetting(AnimationCurve curve)
        {
            _ease = Ease.Custom;
            _curve = curve;
            _customize = false;
            _strength = 0f;
            _period = 0f;
        }

        private EaseSetting(Ease ease, AnimationCurve curve)
        {
            _ease = ease;
            _curve = curve;
            _customize = false;
            _strength = 0f;
            _period = 0f;
        }

        /// <summary>
        /// A linear ease that carries an ease-in-out curve, ready for the user to switch to <see cref="Ease.Custom"/>.
        /// </summary>
        /// <remarks>
        /// Returns a new value with its own curve on each call; do not call it per frame.
        /// </remarks>
        public static EaseSetting Default
            => new(Easing.Ease.Linear, AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));

        /// <summary>
        /// The selected ease.
        /// </summary>
        public readonly Ease Ease => _ease;

        /// <summary>
        /// The custom curve, used only when <see cref="Ease"/> is <see cref="Easing.Ease.Custom"/>.
        /// </summary>
        public readonly AnimationCurve Curve => _curve;

        /// <summary>
        /// Whether <see cref="Strength"/> and <see cref="Period"/> replace the preset's own parameters. Only the
        /// Back, Elastic and Bounce families use them.
        /// </summary>
        public readonly bool Customize => _customize;

        /// <summary>
        /// Back: the overshoot. Elastic: the amplitude. Bounce: the bounciness. Used only while
        /// <see cref="Customize"/> is on.
        /// </summary>
        public readonly float Strength => _strength;

        /// <summary>
        /// Elastic only: the length of one oscillation, as a fraction of the duration (of each half for In-Out). 0
        /// or less uses the preset period. Used only while <see cref="Customize"/> is on.
        /// </summary>
        public readonly float Period => _period;

        /// <summary>
        /// Whether <paramref name="ease"/> belongs to a family that <see cref="Customize"/> can change (Back,
        /// Elastic and Bounce, each In, Out and In-Out).
        /// </summary>
        public static bool IsParametric(Ease ease)
            => ease >= Easing.Ease.InElastic && ease <= Easing.Ease.InOutBounce;

        /// <summary>
        /// Maps the normalized time <paramref name="t"/> through the preset, or through the curve for
        /// <see cref="Easing.Ease.Custom"/>. A missing curve falls back to linear.
        /// </summary>
        public readonly float Evaluate(float t)
        {
            if (_ease != Easing.Ease.Custom)
            {
                if (_customize && IsParametric(_ease))
                {
                    return EaseFunctions.Evaluate(_ease, t, _strength, _period);
                }

                return EaseFunctions.Evaluate(_ease, t);
            }

            return _curve == null ? t : _curve.Evaluate(t);
        }
    }
}
