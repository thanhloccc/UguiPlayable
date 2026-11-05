using UnityEngine;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Graphics
{
    /// <summary>
    /// The color and alpha blending shared by the Graphic and Image mixers: one <see cref="Graphic.color"/> written
    /// from a color channel and an alpha channel.
    /// </summary>
    /// <remarks>
    /// RGB and A blend separately. A comes from one accumulator: a clip with the alpha channel enabled contributes its
    /// alpha, otherwise a clip with the color channel enabled contributes its color's A. So within one clip alpha wins
    /// for A, and across clips the latest finished clip wins, as for every other property. A mixer owns one instance
    /// as a field and must not copy it.
    /// </remarks>
    internal struct GraphicColorBlend
    {
        private Color _baseColor;
        private ColorAccumulator _rgb;
        private FloatAccumulator _a;
        private bool _wroteRgb;
        private bool _wroteA;

        /// <summary>Stores the graphic's current color as the base value.</summary>
        public void Capture(Graphic graphic)
        {
            _baseColor = graphic.color;
            _wroteRgb = false;
            _wroteA = false;
        }

        /// <summary>Writes the base RGB and/or A back, for the components this blend wrote.</summary>
        public void Restore(Graphic graphic)
        {
            if (_wroteRgb == false && _wroteA == false)
            {
                return;
            }

            var color = graphic.color;

            if (_wroteRgb)
            {
                color.r = _baseColor.r;
                color.g = _baseColor.g;
                color.b = _baseColor.b;
            }

            if (_wroteA)
            {
                color.a = _baseColor.a;
            }

            graphic.color = color;
        }

        /// <summary>Clears the accumulators at the start of a frame.</summary>
        public void Reset()
        {
            _rgb.Reset();
            _a.Reset();
        }

        /// <summary>Adds an active clip's enabled channels.</summary>
        public void Accumulate(
              in TweenChannel<Color> color
            , in TweenChannel<float> alpha
            , float weight
            , float progress
        )
        {
            if (color.Enabled)
            {
                var value = TweenChannelMath.Evaluate(color, progress, _baseColor);
                _rgb.Add(value, weight);

                if (alpha.Enabled == false)
                {
                    _a.Add(value.a, weight);
                }
            }

            if (alpha.Enabled)
            {
                _a.Add(TweenChannelMath.Evaluate(alpha, progress, _baseColor.a), weight);
            }
        }

        /// <summary>Offers a finished clip's enabled channels as hold values.</summary>
        public void AccumulateHold(in TweenChannel<Color> color, in TweenChannel<float> alpha, double endTime)
        {
            if (color.Enabled)
            {
                var value = TweenChannelMath.Evaluate(color, 1f, _baseColor);
                _rgb.OfferHold(value, endTime);

                if (alpha.Enabled == false)
                {
                    _a.OfferHold(value.a, endTime);
                }
            }

            if (alpha.Enabled)
            {
                _a.OfferHold(TweenChannelMath.Evaluate(alpha, 1f, _baseColor.a), endTime);
            }
        }

        /// <summary>Writes <see cref="Graphic.color"/> once if either channel received a contribution.</summary>
        public void Apply(Graphic graphic)
        {
            var rgbContributed = _rgb.HasContribution;
            var aContributed = _a.HasContribution;

            if (rgbContributed == false && aContributed == false)
            {
                return;
            }

            var result = graphic.color;

            if (rgbContributed)
            {
                var rgb = _rgb.Resolve(_baseColor);
                result.r = rgb.r;
                result.g = rgb.g;
                result.b = rgb.b;
                _wroteRgb = true;
            }

            if (aContributed)
            {
                result.a = _a.Resolve(_baseColor.a);
                _wroteA = true;
            }

            graphic.color = result;
        }

        /// <summary>Captures the current value of each enabled <see cref="ValueMode.FromCurrent"/> channel.</summary>
        public static void CaptureCurrent(
              Component binding
            , ref TweenChannel<Color> color
            , ref TweenChannel<float> alpha
        )
        {
            if (binding is not Graphic graphic)
            {
                return;
            }

            if (color.Enabled && color.Mode == ValueMode.FromCurrent)
            {
                color.Capture(graphic.color);
            }

            if (alpha.Enabled && alpha.Mode == ValueMode.FromCurrent)
            {
                alpha.Capture(graphic.color.a);
            }
        }
    }
}
