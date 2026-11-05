using UnityEngine;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// Blends one <see cref="Color"/> property for one frame. See <see cref="FloatAccumulator"/> for the rule.
    /// </summary>
    public struct ColorAccumulator
    {
        private Color _sum;
        private float _weight;
        private Color _hold;
        private double _holdEnd;
        private bool _hasHold;

        /// <inheritdoc cref="FloatAccumulator.HasContribution"/>
        public readonly bool HasContribution => _weight > 0f || _hasHold;

        /// <inheritdoc cref="FloatAccumulator.Reset"/>
        public void Reset()
        {
            this = default;
        }

        /// <inheritdoc cref="FloatAccumulator.Add"/>
        public void Add(Color value, float weight)
        {
            _sum += value * weight;
            _weight += weight;
        }

        /// <inheritdoc cref="FloatAccumulator.OfferHold"/>
        public void OfferHold(Color value, double endTime)
        {
            if (_hasHold && endTime < _holdEnd)
            {
                return;
            }

            _hold = value;
            _holdEnd = endTime;
            _hasHold = true;
        }

        /// <inheritdoc cref="FloatAccumulator.Resolve"/>
        public readonly Color Resolve(Color baseValue)
        {
            if (_weight >= 1f)
            {
                return _sum / _weight;
            }

            var hold = _hasHold ? _hold : baseValue;
            return hold * (1f - _weight) + _sum;
        }
    }
}
