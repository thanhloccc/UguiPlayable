namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// Resolves one boolean <see cref="StepChannel"/> property for one frame: the active input with the highest
    /// weight that has reached its threshold decides; otherwise the latest finished clip's value; otherwise the base.
    /// </summary>
    public struct StepAccumulator
    {
        private bool _touched;
        private bool _hasValue;
        private bool _value;
        private float _bestWeight;
        private bool _hasHold;
        private bool _hold;
        private double _holdEnd;

        /// <inheritdoc cref="FloatAccumulator.HasContribution"/>
        public bool HasContribution => _touched || _hasHold;

        /// <inheritdoc cref="FloatAccumulator.Reset"/>
        public void Reset()
        {
            this = default;
        }

        /// <summary>Adds an active clip's step channel at its progress and input weight.</summary>
        public void Add(in StepChannel channel, float progress, float weight)
        {
            _touched = true;

            if (channel.TryEvaluate(progress, out var value) == false || weight <= _bestWeight)
            {
                return;
            }

            _value = value;
            _bestWeight = weight;
            _hasValue = true;
        }

        /// <summary>Offers a finished clip's value; the clip that ended last wins.</summary>
        public void OfferHold(in StepChannel channel, double endTime)
        {
            if (_hasHold && endTime < _holdEnd)
            {
                return;
            }

            _hold = channel.Value;
            _holdEnd = endTime;
            _hasHold = true;
        }

        /// <summary>Returns the resolved value.</summary>
        public bool Resolve(bool baseValue)
        {
            if (_hasValue)
            {
                return _value;
            }

            return _hasHold ? _hold : baseValue;
        }
    }
}
