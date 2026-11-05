using UnityEngine;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// Blends one <see cref="Vector4"/> property for one frame, axis by axis. See <see cref="FloatAccumulator"/> for
    /// the rule applied to each axis.
    /// </summary>
    /// <remarks>
    /// Used for four-component values such as a UV rect (x, y, width, height) or a padding (left, right, top,
    /// bottom). A clip can skip axes (<see cref="AxisMask"/>), so each axis keeps its own weight and hold value. An
    /// axis that no clip touched resolves to the value the binding already has.
    /// </remarks>
    public struct Vector4Accumulator
    {
        private Vector4 _sum;
        private Vector4 _weight;
        private Vector4 _hold;
        private double _holdEndX;
        private double _holdEndY;
        private double _holdEndZ;
        private double _holdEndW;
        private bool _hasHoldX;
        private bool _hasHoldY;
        private bool _hasHoldZ;
        private bool _hasHoldW;

        /// <summary>Whether <see cref="Resolve"/> has anything to write this frame, on any axis.</summary>
        public bool HasContribution
            => ContributedAxes != AxisMask.None;

        /// <summary>The axes that have weight or a hold value this frame.</summary>
        public AxisMask ContributedAxes
            => (_weight.x > 0f || _hasHoldX ? AxisMask.X : AxisMask.None)
            | (_weight.y > 0f || _hasHoldY ? AxisMask.Y : AxisMask.None)
            | (_weight.z > 0f || _hasHoldZ ? AxisMask.Z : AxisMask.None)
            | (_weight.w > 0f || _hasHoldW ? AxisMask.W : AxisMask.None);

        /// <inheritdoc cref="FloatAccumulator.Reset"/>
        public void Reset()
        {
            this = default;
        }

        /// <inheritdoc cref="FloatAccumulator.Add"/>
        public void Add(Vector4 value, float weight)
        {
            Add(value, weight, AxisMask.None);
        }

        /// <summary>
        /// Adds an active clip's value with its input weight, skipping <paramref name="ignoredAxes"/>.
        /// </summary>
        public void Add(Vector4 value, float weight, AxisMask ignoredAxes)
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

            if ((ignoredAxes & AxisMask.Z) == 0)
            {
                _sum.z += value.z * weight;
                _weight.z += weight;
            }

            if ((ignoredAxes & AxisMask.W) == 0)
            {
                _sum.w += value.w * weight;
                _weight.w += weight;
            }
        }

        /// <inheritdoc cref="FloatAccumulator.OfferHold"/>
        public void OfferHold(Vector4 value, double endTime)
        {
            OfferHold(value, endTime, AxisMask.None);
        }

        /// <summary>
        /// Offers a finished clip's end value, skipping <paramref name="ignoredAxes"/>; per axis, the clip that ended
        /// last wins.
        /// </summary>
        public void OfferHold(Vector4 value, double endTime, AxisMask ignoredAxes)
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

            if ((ignoredAxes & AxisMask.Z) == 0 && (_hasHoldZ == false || endTime >= _holdEndZ))
            {
                _hold.z = value.z;
                _holdEndZ = endTime;
                _hasHoldZ = true;
            }

            if ((ignoredAxes & AxisMask.W) == 0 && (_hasHoldW == false || endTime >= _holdEndW))
            {
                _hold.w = value.w;
                _holdEndW = endTime;
                _hasHoldW = true;
            }
        }

        /// <summary>
        /// Returns the blended value. An axis with no contribution keeps its component of
        /// <paramref name="currentValue"/>.
        /// </summary>
        /// <param name="baseValue">The value the property had when the track bound it.</param>
        /// <param name="currentValue">The value the property has now.</param>
        public Vector4 Resolve(Vector4 baseValue, Vector4 currentValue)
        {
            return new Vector4(
                  ResolveAxis(_sum.x, _weight.x, _hasHoldX, _hold.x, baseValue.x, currentValue.x)
                , ResolveAxis(_sum.y, _weight.y, _hasHoldY, _hold.y, baseValue.y, currentValue.y)
                , ResolveAxis(_sum.z, _weight.z, _hasHoldZ, _hold.z, baseValue.z, currentValue.z)
                , ResolveAxis(_sum.w, _weight.w, _hasHoldW, _hold.w, baseValue.w, currentValue.w)
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
