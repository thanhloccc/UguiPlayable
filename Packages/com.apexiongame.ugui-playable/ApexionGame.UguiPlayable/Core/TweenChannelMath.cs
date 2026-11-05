using UnityEngine;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// Evaluates <see cref="TweenChannel{T}"/> values for the supported value types, without allocation.
    /// </summary>
    public static class TweenChannelMath
    {
        /// <summary>
        /// Returns the channel's value at the normalized clip <paramref name="progress"/>.
        /// </summary>
        /// <param name="baseValue">The value the property had when the track bound it.</param>
        public static float Evaluate(in TweenChannel<float> channel, float progress, float baseValue)
        {
            var eased = channel.Ease.Evaluate(progress);

            switch (channel.Mode)
            {
                case ValueMode.Relative:
                    return baseValue + Mathf.LerpUnclamped(channel.From, channel.To, eased);

                case ValueMode.FromCurrent:
                    return Mathf.LerpUnclamped(channel.HasCaptured ? channel.Captured : baseValue, channel.To, eased);

                default:
                    return Mathf.LerpUnclamped(channel.From, channel.To, eased);
            }
        }

        /// <inheritdoc cref="Evaluate(in TweenChannel{float}, float, float)"/>
        public static Vector2 Evaluate(in TweenChannel<Vector2> channel, float progress, Vector2 baseValue)
        {
            var eased = channel.Ease.Evaluate(progress);

            switch (channel.Mode)
            {
                case ValueMode.Relative:
                    return baseValue + Vector2.LerpUnclamped(channel.From, channel.To, eased);

                case ValueMode.FromCurrent:
                    return Vector2.LerpUnclamped(channel.HasCaptured ? channel.Captured : baseValue, channel.To, eased);

                default:
                    return Vector2.LerpUnclamped(channel.From, channel.To, eased);
            }
        }

        /// <inheritdoc cref="Evaluate(in TweenChannel{float}, float, float)"/>
        public static Vector3 Evaluate(in TweenChannel<Vector3> channel, float progress, Vector3 baseValue)
        {
            var eased = channel.Ease.Evaluate(progress);

            switch (channel.Mode)
            {
                case ValueMode.Relative:
                    return baseValue + Vector3.LerpUnclamped(channel.From, channel.To, eased);

                case ValueMode.FromCurrent:
                    return Vector3.LerpUnclamped(channel.HasCaptured ? channel.Captured : baseValue, channel.To, eased);

                default:
                    return Vector3.LerpUnclamped(channel.From, channel.To, eased);
            }
        }

        /// <inheritdoc cref="Evaluate(in TweenChannel{float}, float, float)"/>
        public static Vector4 Evaluate(in TweenChannel<Vector4> channel, float progress, Vector4 baseValue)
        {
            var eased = channel.Ease.Evaluate(progress);

            switch (channel.Mode)
            {
                case ValueMode.Relative:
                    return baseValue + Vector4.LerpUnclamped(channel.From, channel.To, eased);

                case ValueMode.FromCurrent:
                    return Vector4.LerpUnclamped(channel.HasCaptured ? channel.Captured : baseValue, channel.To, eased);

                default:
                    return Vector4.LerpUnclamped(channel.From, channel.To, eased);
            }
        }

        /// <inheritdoc cref="Evaluate(in TweenChannel{float}, float, float)"/>
        /// <remarks>
        /// In <see cref="ValueMode.Relative"/> the offset is added per component and the alpha is clamped to [0, 1].
        /// </remarks>
        public static Color Evaluate(in TweenChannel<Color> channel, float progress, Color baseValue)
        {
            var eased = channel.Ease.Evaluate(progress);

            switch (channel.Mode)
            {
                case ValueMode.Relative:
                {
                    var color = baseValue + Color.LerpUnclamped(channel.From, channel.To, eased);
                    color.a = Mathf.Clamp01(color.a);
                    return color;
                }

                case ValueMode.FromCurrent:
                    return Color.LerpUnclamped(channel.HasCaptured ? channel.Captured : baseValue, channel.To, eased);

                default:
                    return Color.LerpUnclamped(channel.From, channel.To, eased);
            }
        }
    }
}
