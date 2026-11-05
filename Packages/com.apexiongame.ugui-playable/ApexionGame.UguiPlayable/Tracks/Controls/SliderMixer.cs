using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Controls
{
    /// <summary>
    /// Blends <see cref="SliderClip"/> channels into the bound <see cref="Slider"/>.
    /// </summary>
    /// <remarks>
    /// The base value is captured when the track binds the slider.
    /// </remarks>
    public sealed class SliderMixer : TweenMixer<Slider, SliderBehaviour>
    {
        /// <summary>
        /// Whether a write raises <c>onValueChanged</c>. Set by the track; effective in Play Mode only.
        /// </summary>
        internal bool SendCallbacks { get; set; }

        private float _baseValue;
        private FloatAccumulator _value;
        private bool _wroteValue;

        /// <inheritdoc/>
        protected override void CaptureBase(Slider binding)
        {
            _baseValue = binding.value;
            _wroteValue = false;
        }

        /// <inheritdoc/>
        protected override void RestoreBase(Slider binding)
        {
            if (_wroteValue)
            {
                binding.SetValueWithoutNotify(_baseValue);
            }
        }

        /// <inheritdoc/>
        protected override void BeginFrame()
        {
            _value.Reset();
        }

        /// <inheritdoc/>
        protected override void Accumulate(SliderBehaviour behaviour, float weight, float progress)
        {
            var value = behaviour.Value;

            if (value.Enabled)
            {
                _value.Add(TweenChannelMath.Evaluate(value, progress, _baseValue), weight);
            }
        }

        /// <inheritdoc/>
        protected override void AccumulateHold(SliderBehaviour behaviour, double endTime)
        {
            var value = behaviour.Value;

            if (value.Enabled)
            {
                _value.OfferHold(TweenChannelMath.Evaluate(value, 1f, _baseValue), endTime);
            }
        }

        /// <inheritdoc/>
        protected override void Apply(Slider binding)
        {
            if (_value.HasContribution)
            {
                Write(binding, _value.Resolve(_baseValue));
                _wroteValue = true;
            }
        }

        private void Write(Slider binding, float value)
        {
            if (SendCallbacks && PlayModeSeam.IsPlaying)
            {
                binding.value = value;
            }
            else
            {
                binding.SetValueWithoutNotify(value);
            }
        }
    }
}
