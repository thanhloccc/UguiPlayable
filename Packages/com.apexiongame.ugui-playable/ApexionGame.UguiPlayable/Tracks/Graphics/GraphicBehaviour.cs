using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Graphics
{
    /// <summary>
    /// The channels of a <see cref="GraphicClip"/>: color (RGBA) and alpha (A only).
    /// </summary>
    /// <remarks>
    /// Both channels write <see cref="Graphic.color"/>. When both are enabled the alpha channel wins for A.
    /// </remarks>
    [Serializable]
    public sealed class GraphicBehaviour : TweenBehaviour
    {
        internal const string COLOR_PROPERTY = "m_Color";

        [SerializeField]
        private TweenChannel<Color> _color = new(UnityEngine.Color.white, UnityEngine.Color.white, enabled: false);

        [SerializeField]
        private TweenChannel<float> _alpha = new(0f, 1f, enabled: false);

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

        /// <inheritdoc/>
        public override void CollectPropertyNames(ICollection<string> names)
        {
            if (_color.Enabled || _alpha.Enabled)
            {
                names.Add(COLOR_PROPERTY);
            }
        }

        /// <inheritdoc/>
        public override void OnActivated(Component binding)
        {
            GraphicColorBlend.CaptureCurrent(binding, ref _color, ref _alpha);
        }

        /// <inheritdoc/>
        public override void OnDeactivated()
        {
            _color.ClearCapture();
            _alpha.ClearCapture();
        }
    }
}
