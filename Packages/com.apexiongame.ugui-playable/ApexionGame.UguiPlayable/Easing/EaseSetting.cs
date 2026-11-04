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

        /// <summary>
        /// Creates a setting that uses the preset <paramref name="ease"/>.
        /// </summary>
        public EaseSetting(Ease ease)
        {
            _ease = ease;
            _curve = null;
        }

        /// <summary>
        /// Creates a setting that uses <paramref name="curve"/> and sets the ease to <see cref="Ease.Custom"/>.
        /// </summary>
        public EaseSetting(AnimationCurve curve)
        {
            _ease = Ease.Custom;
            _curve = curve;
        }

        private EaseSetting(Ease ease, AnimationCurve curve)
        {
            _ease = ease;
            _curve = curve;
        }

        /// <summary>
        /// A linear ease that carries an ease-in-out curve, ready for the user to switch to <see cref="Ease.Custom"/>.
        /// </summary>
        public static EaseSetting Default
            => new(Easing.Ease.Linear, AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));

        /// <summary>
        /// The selected ease.
        /// </summary>
        public Ease Ease => _ease;

        /// <summary>
        /// The custom curve, used only when <see cref="Ease"/> is <see cref="Easing.Ease.Custom"/>.
        /// </summary>
        public AnimationCurve Curve => _curve;

        /// <summary>
        /// Maps the normalized time <paramref name="t"/> through the preset, or through the curve for
        /// <see cref="Easing.Ease.Custom"/>. A missing curve falls back to linear.
        /// </summary>
        public float Evaluate(float t)
        {
            if (_ease != Easing.Ease.Custom)
            {
                return EaseFunctions.Evaluate(_ease, t);
            }

            return _curve == null ? t : _curve.Evaluate(t);
        }
    }
}
