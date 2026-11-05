using TMPro;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Text
{
    /// <summary>
    /// Blends one <see cref="VertexGradient"/> property for one frame: one <see cref="ColorAccumulator"/> per corner.
    /// See <see cref="FloatAccumulator"/> for the rule.
    /// </summary>
    public struct VertexGradientAccumulator
    {
        private ColorAccumulator _topLeft;
        private ColorAccumulator _topRight;
        private ColorAccumulator _bottomLeft;
        private ColorAccumulator _bottomRight;

        /// <inheritdoc cref="FloatAccumulator.HasContribution"/>
        public readonly bool HasContribution
            => _topLeft.HasContribution
                || _topRight.HasContribution
                || _bottomLeft.HasContribution
                || _bottomRight.HasContribution;

        /// <inheritdoc cref="FloatAccumulator.Reset"/>
        public void Reset()
        {
            this = default;
        }

        /// <inheritdoc cref="FloatAccumulator.Add"/>
        public void Add(in VertexGradient value, float weight)
        {
            _topLeft.Add(value.topLeft, weight);
            _topRight.Add(value.topRight, weight);
            _bottomLeft.Add(value.bottomLeft, weight);
            _bottomRight.Add(value.bottomRight, weight);
        }

        /// <inheritdoc cref="FloatAccumulator.OfferHold"/>
        public void OfferHold(in VertexGradient value, double endTime)
        {
            _topLeft.OfferHold(value.topLeft, endTime);
            _topRight.OfferHold(value.topRight, endTime);
            _bottomLeft.OfferHold(value.bottomLeft, endTime);
            _bottomRight.OfferHold(value.bottomRight, endTime);
        }

        /// <inheritdoc cref="FloatAccumulator.Resolve"/>
        public readonly VertexGradient Resolve(in VertexGradient baseValue)
            => new(
                  _topLeft.Resolve(baseValue.topLeft)
                , _topRight.Resolve(baseValue.topRight)
                , _bottomLeft.Resolve(baseValue.bottomLeft)
                , _bottomRight.Resolve(baseValue.bottomRight)
            );
    }
}
