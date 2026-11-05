using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Graphics
{
    /// <summary>
    /// The channels of a <see cref="RawImageClip"/>: color (RGBA), alpha (A only) and UV rect.
    /// </summary>
    /// <remarks>
    /// Color and alpha both write <see cref="Graphic.color"/>. When both are enabled the alpha channel wins for A.
    /// The UV rect channel holds (x, y, width, height) of <see cref="RawImage.uvRect"/>, with the axes X, Y, W and H,
    /// so a clip can scroll the texture by animating X or Y only, or zoom it by animating W and H. The default
    /// scrolls the texture one full width.
    /// </remarks>
    [Serializable]
    public sealed class RawImageBehaviour : TweenBehaviour
    {
        internal const string COLOR_PROPERTY = "m_Color";
        internal const string UV_RECT_PROPERTY = "m_UVRect";

        [SerializeField]
        private TweenChannel<Color> _color = new(UnityEngine.Color.white, UnityEngine.Color.white, enabled: false);

        [SerializeField]
        private TweenChannel<float> _alpha = new(0f, 1f, enabled: false);

        [SerializeField]
        private TweenChannel<Vector4> _uvRect = new(new Vector4(0f, 0f, 1f, 1f), new Vector4(1f, 0f, 1f, 1f));

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

        /// <summary>The <see cref="RawImage.uvRect"/> channel, as (x, y, width, height).</summary>
        public TweenChannel<Vector4> UvRect
        {
            get => _uvRect;
            set => _uvRect = value;
        }

        /// <inheritdoc/>
        internal override bool HasEnabledChannel()
            => _color.Enabled
                || _alpha.Enabled
                || _uvRect.Enabled;

        /// <inheritdoc/>
        public override void CollectPropertyNames(ICollection<string> names)
        {
            if (_color.Enabled || _alpha.Enabled)
            {
                names.Add(COLOR_PROPERTY);
            }

            if (_uvRect.Enabled)
            {
                names.Add(UV_RECT_PROPERTY);
            }
        }

        /// <inheritdoc/>
        public override void OnActivated(Component binding)
        {
            GraphicColorBlend.CaptureCurrent(binding, ref _color, ref _alpha);

            if (_uvRect.Enabled && _uvRect.Mode == ValueMode.FromCurrent && binding is RawImage rawImage)
            {
                var rect = rawImage.uvRect;
                _uvRect.Capture(new Vector4(rect.x, rect.y, rect.width, rect.height));
            }
        }

        /// <inheritdoc/>
        public override void OnDeactivated()
        {
            _color.ClearCapture();
            _alpha.ClearCapture();
            _uvRect.ClearCapture();
        }
    }
}
