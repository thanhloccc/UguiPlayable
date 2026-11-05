namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// Blends one float property for one frame: weighted values from active clips, plus the end value of the latest
    /// finished clip as the hold value.
    /// </summary>
    /// <remarks>
    /// With total weight <c>W</c> below 1 the result is <c>hold * (1 - W) + Σ wᵢ·vᵢ</c>, where <c>hold</c> is the
    /// offered hold value, or the base value when no clip has finished. With <c>W ≥ 1</c> it is the weighted mean.
    /// </remarks>
    public struct FloatAccumulator
    {
        private float _sum;
        private float _weight;
        private float _hold;
        private double _holdEnd;
        private bool _hasHold;

        /// <summary>Whether <see cref="Resolve"/> has anything to write this frame.</summary>
        public readonly bool HasContribution => _weight > 0f || _hasHold;

        /// <summary>Clears the accumulator at the start of a frame.</summary>
        public void Reset()
        {
            this = default;
        }

        /// <summary>Adds an active clip's value with its input weight.</summary>
        public void Add(float value, float weight)
        {
            _sum += value * weight;
            _weight += weight;
        }

        /// <summary>Offers a finished clip's end value; the clip that ended last wins.</summary>
        public void OfferHold(float value, double endTime)
        {
            if (_hasHold && endTime < _holdEnd)
            {
                return;
            }

            _hold = value;
            _holdEnd = endTime;
            _hasHold = true;
        }

        /// <summary>Returns the blended value.</summary>
        public readonly float Resolve(float baseValue)
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
