using UnityEngine;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// Blends one <see cref="Vector2"/> property for one frame. See <see cref="FloatAccumulator"/> for the rule.
    /// </summary>
    public struct Vector2Accumulator
    {
        private Vector2 _sum;
        private float _weight;
        private Vector2 _hold;
        private double _holdEnd;
        private bool _hasHold;

        /// <inheritdoc cref="FloatAccumulator.HasContribution"/>
        public bool HasContribution => _weight > 0f || _hasHold;

        /// <inheritdoc cref="FloatAccumulator.Reset"/>
        public void Reset()
        {
            this = default;
        }

        /// <inheritdoc cref="FloatAccumulator.Add"/>
        public void Add(Vector2 value, float weight)
        {
            _sum += value * weight;
            _weight += weight;
        }

        /// <inheritdoc cref="FloatAccumulator.OfferHold"/>
        public void OfferHold(Vector2 value, double endTime)
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
        public Vector2 Resolve(Vector2 baseValue)
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
