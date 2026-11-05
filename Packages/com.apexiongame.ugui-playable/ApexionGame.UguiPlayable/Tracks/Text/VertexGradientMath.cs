using TMPro;
using UnityEngine;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Text
{
    /// <summary>
    /// Evaluates a <see cref="TweenChannel{T}"/> of <see cref="VertexGradient"/>, without allocation.
    /// </summary>
    public static class VertexGradientMath
    {
        /// <summary>
        /// Returns the channel's gradient at the normalized clip <paramref name="progress"/>.
        /// </summary>
        /// <remarks>
        /// Each corner follows the color rule of <see cref="TweenChannelMath"/>: in <see cref="ValueMode.Relative"/>
        /// the offset is added per component and the alpha is clamped to [0, 1].
        /// </remarks>
        /// <param name="baseValue">The gradient the text had when the track bound it.</param>
        public static VertexGradient Evaluate(
              in TweenChannel<VertexGradient> channel
            , float progress
            , VertexGradient baseValue
        )
        {
            var eased = channel.Ease.Evaluate(progress);
            var from = channel.From;
            var to = channel.To;

            switch (channel.Mode)
            {
                case ValueMode.Relative:
                    return new VertexGradient(
                          Offset(baseValue.topLeft, from.topLeft, to.topLeft, eased)
                        , Offset(baseValue.topRight, from.topRight, to.topRight, eased)
                        , Offset(baseValue.bottomLeft, from.bottomLeft, to.bottomLeft, eased)
                        , Offset(baseValue.bottomRight, from.bottomRight, to.bottomRight, eased)
                    );

                case ValueMode.FromCurrent:
                {
                    var start = channel.HasCaptured ? channel.Captured : baseValue;

                    return new VertexGradient(
                          Color.LerpUnclamped(start.topLeft, to.topLeft, eased)
                        , Color.LerpUnclamped(start.topRight, to.topRight, eased)
                        , Color.LerpUnclamped(start.bottomLeft, to.bottomLeft, eased)
                        , Color.LerpUnclamped(start.bottomRight, to.bottomRight, eased)
                    );
                }

                default:
                    return new VertexGradient(
                          Color.LerpUnclamped(from.topLeft, to.topLeft, eased)
                        , Color.LerpUnclamped(from.topRight, to.topRight, eased)
                        , Color.LerpUnclamped(from.bottomLeft, to.bottomLeft, eased)
                        , Color.LerpUnclamped(from.bottomRight, to.bottomRight, eased)
                    );
            }
        }

        private static Color Offset(Color baseColor, Color from, Color to, float eased)
        {
            var color = baseColor + Color.LerpUnclamped(from, to, eased);
            color.a = Mathf.Clamp01(color.a);
            return color;
        }
    }
}
