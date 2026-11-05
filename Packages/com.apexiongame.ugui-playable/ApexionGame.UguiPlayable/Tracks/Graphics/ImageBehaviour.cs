using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Easing;

namespace ApexionGame.UguiPlayable.Tracks.Graphics
{
    /// <summary>
    /// The channels of an <see cref="ImageClip"/>: color (RGBA), alpha (A only) and fill amount.
    /// </summary>
    /// <remarks>
    /// Color and alpha both write <see cref="Graphic.color"/>. When both are enabled the alpha channel wins for A.
    /// The fill amount is clamped to [0, 1] by uGUI.
    /// </remarks>
    [Serializable]
    public sealed class ImageBehaviour : TweenBehaviour
    {
        internal const string COLOR_PROPERTY = "m_Color";
        internal const string FILL_AMOUNT_PROPERTY = "m_FillAmount";

        [SerializeField]
        private TweenChannel<Color> _color = new(UnityEngine.Color.white, UnityEngine.Color.white, enabled: false);

        [SerializeField]
        private TweenChannel<float> _alpha = new(0f, 1f, enabled: false);

        [SerializeField]
        private TweenChannel<float> _fillAmount = new(0f, 1f, enabled: false);

        /// <summary>The <see cref="Graphic.color"/> channel.</summary>
        public TweenChannel<Color> Color
        {
            get => _color;
            set => _color = value;
        }

        /// <summary>The channel for the A component of <see cref="Graphic.color"/>.</summary>
        public TweenChannel<float> Alpha
        {
            get => _alpha;
            set => _alpha = value;
        }

        /// <summary>The <see cref="Image.fillAmount"/> channel.</summary>
        public TweenChannel<float> FillAmount
        {
            get => _fillAmount;
            set => _fillAmount = value;
        }

        /// <inheritdoc/>
        internal override bool HasEnabledChannel()
            => _color.Enabled
                || _alpha.Enabled
                || _fillAmount.Enabled;

        /// <inheritdoc/>
        internal override bool TryGetFirstEnabledEase(out EaseSetting ease)
        {
            if (_color.Enabled)
            {
                ease = _color.Ease;
                return true;
            }

            if (_alpha.Enabled)
            {
                ease = _alpha.Ease;
                return true;
            }

            if (_fillAmount.Enabled)
            {
                ease = _fillAmount.Ease;
                return true;
            }

            ease = default;
            return false;
        }

        /// <inheritdoc/>
        public override void CollectPropertyNames(ICollection<string> names)
        {
            if (_color.Enabled || _alpha.Enabled)
            {
                names.Add(COLOR_PROPERTY);
            }

            if (_fillAmount.Enabled)
            {
                names.Add(FILL_AMOUNT_PROPERTY);
            }
        }

        /// <inheritdoc/>
        public override void OnActivated(Component binding)
        {
            GraphicColorBlend.CaptureCurrent(binding, ref _color, ref _alpha);

            if (_fillAmount.Enabled && _fillAmount.Mode == ValueMode.FromCurrent && binding is Image image)
            {
                _fillAmount.Capture(image.fillAmount);
            }
        }

        /// <inheritdoc/>
        public override void OnDeactivated()
        {
            _color.ClearCapture();
            _alpha.ClearCapture();
            _fillAmount.ClearCapture();
        }
    }
}
