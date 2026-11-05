using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Effects
{
    /// <summary>
    /// The channels of a <see cref="ShadowClip"/>: effect color (RGBA) and effect distance.
    /// </summary>
    /// <remarks>
    /// The color channel includes alpha, so there is no separate alpha channel. uGUI clamps the distance to
    /// +-600 on each axis. The distance honours <see cref="TweenChannel{T}.IgnoredAxes"/>.
    /// </remarks>
    [Serializable]
    public sealed class ShadowBehaviour : TweenBehaviour
    {
        internal const string EFFECT_COLOR_PROPERTY = "m_EffectColor";
        internal const string EFFECT_DISTANCE_PROPERTY = "m_EffectDistance";

        [SerializeField]
        private TweenChannel<Color> _effectColor = new(new Color(0f, 0f, 0f, 0.5f), new Color(0f, 0f, 0f, 1f));

        [SerializeField]
        private TweenChannel<Vector2> _effectDistance = new(new Vector2(1f, -1f), new Vector2(4f, -4f), enabled: false);

        /// <summary>The <see cref="Shadow.effectColor"/> channel.</summary>
        public TweenChannel<Color> EffectColor
        {
            get => _effectColor;
            set => _effectColor = value;
        }

        /// <summary>The <see cref="Shadow.effectDistance"/> channel.</summary>
        public TweenChannel<Vector2> EffectDistance
        {
            get => _effectDistance;
            set => _effectDistance = value;
        }

        /// <inheritdoc/>
        internal override bool HasEnabledChannel()
            => _effectColor.Enabled
                || _effectDistance.Enabled;

        /// <inheritdoc/>
        public override void CollectPropertyNames(ICollection<string> names)
        {
            if (_effectColor.Enabled)
            {
                names.Add(EFFECT_COLOR_PROPERTY);
            }

            if (_effectDistance.Enabled)
            {
                names.Add(EFFECT_DISTANCE_PROPERTY);
            }
        }

        /// <inheritdoc/>
        public override void OnActivated(Component binding)
        {
            if (binding is not Shadow shadow)
            {
                return;
            }

            if (_effectColor.Enabled && _effectColor.Mode == ValueMode.FromCurrent)
            {
                _effectColor.Capture(shadow.effectColor);
            }

            if (_effectDistance.Enabled && _effectDistance.Mode == ValueMode.FromCurrent)
            {
                _effectDistance.Capture(shadow.effectDistance);
            }
        }

        /// <inheritdoc/>
        public override void OnDeactivated()
        {
            _effectColor.ClearCapture();
            _effectDistance.ClearCapture();
        }
    }
}
