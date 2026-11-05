using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Controls
{
    /// <summary>
    /// Blends <see cref="ScrollbarClip"/> channels into the bound <see cref="Scrollbar"/>.
    /// </summary>
    /// <remarks>
    /// The base values are captured when the track binds the scrollbar.
    /// </remarks>
    public sealed class ScrollbarMixer : TweenMixer<Scrollbar, ScrollbarBehaviour>
    {
        /// <summary>
        /// Whether a write raises <c>onValueChanged</c>. Set by the track; effective in Play Mode only.
        /// </summary>
        internal bool SendCallbacks { get; set; }

        private float _baseValue;
        private float _baseSize;

        private FloatAccumulator _value;
        private FloatAccumulator _size;

        private bool _wroteValue;
        private bool _wroteSize;

        /// <inheritdoc/>
        protected override void CaptureBase(Scrollbar binding)
        {
            _baseValue = binding.value;
            _baseSize = binding.size;
            _wroteValue = false;
            _wroteSize = false;
        }

        /// <inheritdoc/>
        protected override void RestoreBase(Scrollbar binding)
        {
            if (_wroteSize)
            {
                binding.size = _baseSize;
            }

            if (_wroteValue)
            {
                binding.SetValueWithoutNotify(_baseValue);
            }
        }

        /// <inheritdoc/>
        protected override void BeginFrame()
        {
            _value.Reset();
            _size.Reset();
        }

        /// <inheritdoc/>
        protected override void Accumulate(ScrollbarBehaviour behaviour, float weight, float progress)
        {
            var value = behaviour.Value;

            if (value.Enabled)
            {
                _value.Add(TweenChannelMath.Evaluate(value, progress, _baseValue), weight);
            }

            var size = behaviour.Size;

            if (size.Enabled)
            {
                _size.Add(TweenChannelMath.Evaluate(size, progress, _baseSize), weight);
            }
        }

        /// <inheritdoc/>
        protected override void AccumulateHold(ScrollbarBehaviour behaviour, double endTime)
        {
            var value = behaviour.Value;

            if (value.Enabled)
            {
                _value.OfferHold(TweenChannelMath.Evaluate(value, 1f, _baseValue), endTime);
            }

            var size = behaviour.Size;

            if (size.Enabled)
            {
                _size.OfferHold(TweenChannelMath.Evaluate(size, 1f, _baseSize), endTime);
            }
        }

        /// <inheritdoc/>
        protected override void Apply(Scrollbar binding)
        {
            if (_size.HasContribution)
            {
                binding.size = _size.Resolve(_baseSize);
                _wroteSize = true;
            }

            if (_value.HasContribution)
            {
                Write(binding, _value.Resolve(_baseValue));
                _wroteValue = true;
            }
        }

        private void Write(Scrollbar binding, float value)
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
