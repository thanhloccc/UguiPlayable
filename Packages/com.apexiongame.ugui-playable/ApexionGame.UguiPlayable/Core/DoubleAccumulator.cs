namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// Blends one double property for one frame, by the rule of <see cref="FloatAccumulator"/>.
    /// </summary>
    /// <remarks>
    /// With total weight W below 1 the result is <c>hold * (1 - W)</c> plus the weighted sum, where hold is the
    /// offered hold value, or the base value when no clip has finished. With W of 1 or more it is the weighted mean.
    /// </remarks>
    internal struct DoubleAccumulator
    {
        private double _sum;
        private double _weight;
        private double _hold;
        private double _holdEnd;
        private bool _hasHold;

        /// <summary>Whether <see cref="Resolve"/> has anything to write this frame.</summary>
        public readonly bool HasContribution => _weight > 0d || _hasHold;

        /// <summary>Clears the accumulator at the start of a frame.</summary>
        public void Reset()
        {
            this = default;
        }

        /// <summary>Adds an active clip's value with its input weight.</summary>
        public void Add(double value, float weight)
        {
            _sum += value * weight;
            _weight += weight;
        }

        /// <summary>Offers a finished clip's end value; the clip that ended last wins.</summary>
        public void OfferHold(double value, double endTime)
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
        public readonly double Resolve(double baseValue)
        {
            if (_weight >= 1d)
            {
                return _sum / _weight;
            }

            var hold = _hasHold ? _hold : baseValue;
            return hold * (1d - _weight) + _sum;
        }
    }
}
