using UnityEngine;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// Blends one <see cref="Vector2"/> property for one frame, axis by axis. See <see cref="FloatAccumulator"/> for
    /// the rule applied to each axis.
    /// </summary>
    /// <remarks>
    /// A clip can skip axes (<see cref="AxisMask"/>), so each axis keeps its own weight and hold value. An axis that
    /// no clip touched resolves to the value the binding already has.
    /// </remarks>
    public struct Vector2Accumulator
    {
        private Vector2 _sum;
        private Vector2 _weight;
        private Vector2 _hold;
        private double _holdEndX;
        private double _holdEndY;
        private bool _hasHoldX;
        private bool _hasHoldY;

        /// <summary>Whether <see cref="Resolve"/> has anything to write this frame, on any axis.</summary>
        public readonly bool HasContribution
            => _weight.x > 0f || _weight.y > 0f || _hasHoldX || _hasHoldY;

        /// <summary>The axes that have weight or a hold value this frame.</summary>
        public readonly AxisMask ContributedAxes
            => (_weight.x > 0f || _hasHoldX ? AxisMask.X : AxisMask.None)
            | (_weight.y > 0f || _hasHoldY ? AxisMask.Y : AxisMask.None);

        /// <inheritdoc cref="FloatAccumulator.Reset"/>
        public void Reset()
        {
            this = default;
        }

        /// <inheritdoc cref="FloatAccumulator.Add"/>
        public void Add(Vector2 value, float weight)
        {
            Add(value, weight, AxisMask.None);
        }

        /// <summary>
        /// Adds an active clip's value with its input weight, skipping <paramref name="ignoredAxes"/>.
        /// </summary>
        public void Add(Vector2 value, float weight, AxisMask ignoredAxes)
        {
            if ((ignoredAxes & AxisMask.X) == 0)
            {
                _sum.x += value.x * weight;
                _weight.x += weight;
            }

            if ((ignoredAxes & AxisMask.Y) == 0)
            {
                _sum.y += value.y * weight;
                _weight.y += weight;
            }
        }

        /// <inheritdoc cref="FloatAccumulator.OfferHold"/>
        public void OfferHold(Vector2 value, double endTime)
        {
            OfferHold(value, endTime, AxisMask.None);
        }

        /// <summary>
        /// Offers a finished clip's end value, skipping <paramref name="ignoredAxes"/>; per axis, the clip that ended
        /// last wins.
        /// </summary>
        public void OfferHold(Vector2 value, double endTime, AxisMask ignoredAxes)
        {
            if ((ignoredAxes & AxisMask.X) == 0 && (_hasHoldX == false || endTime >= _holdEndX))
            {
                _hold.x = value.x;
                _holdEndX = endTime;
                _hasHoldX = true;
            }

            if ((ignoredAxes & AxisMask.Y) == 0 && (_hasHoldY == false || endTime >= _holdEndY))
            {
                _hold.y = value.y;
                _holdEndY = endTime;
                _hasHoldY = true;
            }
        }

        /// <summary>
        /// Returns the blended value. An axis with no contribution keeps its component of
        /// <paramref name="currentValue"/>.
        /// </summary>
        /// <param name="baseValue">The value the property had when the track bound it.</param>
        /// <param name="currentValue">The value the property has now.</param>
        public readonly Vector2 Resolve(Vector2 baseValue, Vector2 currentValue)
        {
            return new Vector2(
                  ResolveAxis(_sum.x, _weight.x, _hasHoldX, _hold.x, baseValue.x, currentValue.x)
                , ResolveAxis(_sum.y, _weight.y, _hasHoldY, _hold.y, baseValue.y, currentValue.y)
            );
        }

        private static float ResolveAxis(
              float sum
            , float weight
            , bool hasHold
            , float hold
            , float baseValue
            , float currentValue
        )
        {
            if (weight <= 0f && hasHold == false)
            {
                return currentValue;
            }

            if (weight >= 1f)
            {
                return sum / weight;
            }

            return (hasHold ? hold : baseValue) * (1f - weight) + sum;
        }
    }
}
