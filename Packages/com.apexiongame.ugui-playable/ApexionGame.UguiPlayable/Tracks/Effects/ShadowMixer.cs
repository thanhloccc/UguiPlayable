using UnityEngine;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Effects
{
    /// <summary>
    /// Blends <see cref="ShadowClip"/> channels into the bound <see cref="Shadow"/> (or <see cref="Outline"/>).
    /// </summary>
    /// <remarks>
    /// The distance blends per axis, so only the axes a clip animates are written and reverted.
    /// </remarks>
    public sealed class ShadowMixer : TweenMixer<Shadow, ShadowBehaviour>
    {
        private Color _baseColor;
        private Vector2 _baseDistance;
        private ColorAccumulator _effectColor;
        private Vector2Accumulator _effectDistance;
        private bool _wroteColor;
        private AxisMask _wroteDistance;

        /// <inheritdoc/>
        protected override void CaptureBase(Shadow binding)
        {
            _baseColor = binding.effectColor;
            _baseDistance = binding.effectDistance;
            _wroteColor = false;
            _wroteDistance = AxisMask.None;
        }

        /// <inheritdoc/>
        /// <remarks>Only the distance axes this mixer wrote are restored.</remarks>
        protected override void RestoreBase(Shadow binding)
        {
            if (_wroteColor)
            {
                binding.effectColor = _baseColor;
            }

            if (_wroteDistance != AxisMask.None)
            {
                binding.effectDistance = AxisMaskMath.Merge(binding.effectDistance, _baseDistance, _wroteDistance);
            }
        }

        /// <inheritdoc/>
        protected override void BeginFrame()
        {
            _effectColor.Reset();
            _effectDistance.Reset();
        }

        /// <inheritdoc/>
        protected override void Accumulate(ShadowBehaviour behaviour, float weight, float progress)
        {
            var effectColor = behaviour.EffectColor;

            if (effectColor.Enabled)
            {
                _effectColor.Add(TweenChannelMath.Evaluate(effectColor, progress, _baseColor), weight);
            }

            var effectDistance = behaviour.EffectDistance;

            if (effectDistance.Enabled)
            {
                var value = TweenChannelMath.Evaluate(effectDistance, progress, _baseDistance);
                _effectDistance.Add(value, weight, effectDistance.IgnoredAxes);
            }
        }

        /// <inheritdoc/>
        protected override void AccumulateHold(ShadowBehaviour behaviour, double endTime)
        {
            var effectColor = behaviour.EffectColor;

            if (effectColor.Enabled)
            {
                _effectColor.OfferHold(TweenChannelMath.Evaluate(effectColor, 1f, _baseColor), endTime);
            }

            var effectDistance = behaviour.EffectDistance;

            if (effectDistance.Enabled)
            {
                var value = TweenChannelMath.Evaluate(effectDistance, 1f, _baseDistance);
                _effectDistance.OfferHold(value, endTime, effectDistance.IgnoredAxes);
            }
        }

        /// <inheritdoc/>
        protected override void Apply(Shadow binding)
        {
            if (_effectColor.HasContribution)
            {
                binding.effectColor = _effectColor.Resolve(_baseColor);
                _wroteColor = true;
            }

            if (_effectDistance.HasContribution)
            {
                binding.effectDistance = _effectDistance.Resolve(_baseDistance, binding.effectDistance);
                _wroteDistance |= _effectDistance.ContributedAxes;
            }
        }
    }
}
