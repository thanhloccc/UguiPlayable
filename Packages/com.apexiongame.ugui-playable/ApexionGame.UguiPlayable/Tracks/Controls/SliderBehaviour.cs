using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Controls
{
    /// <summary>
    /// The channel of a <see cref="SliderClip"/>: the slider value.
    /// </summary>
    /// <remarks>
    /// Absolute values are in the slider's own range (<see cref="Slider.minValue"/> to
    /// <see cref="Slider.maxValue"/>). The <see cref="Slider.value"/> setter clamps to that range and rounds when
    /// <see cref="Slider.wholeNumbers"/> is on; the clip does not work around either.
    /// <see cref="Slider.onValueChanged"/> follows the track's <see cref="SliderTrack.SendCallbacks"/> switch: off by
    /// default, so scrubbing, playing and Revert write the value silently; on, the track notifies in Play Mode only.
    /// In Edit Mode, Unity itself calls onValueChanged once when the preview ends.
    /// </remarks>
    [Serializable]
    public sealed class SliderBehaviour : TweenBehaviour
    {
        internal const string VALUE_PROPERTY = "m_Value";

        [SerializeField]
        private TweenChannel<float> _value = new(0f, 1f);

        /// <summary>The <see cref="Slider.value"/> channel.</summary>
        public TweenChannel<float> Value
        {
            get => _value;
            set => _value = value;
        }

        /// <inheritdoc/>
        internal override bool HasEnabledChannel()
            => _value.Enabled;

        /// <inheritdoc/>
        public override void CollectPropertyNames(ICollection<string> names)
        {
            if (_value.Enabled)
            {
                names.Add(VALUE_PROPERTY);
            }
        }

        /// <inheritdoc/>
        public override void OnActivated(Component binding)
        {
            if (_value.Enabled && _value.Mode == ValueMode.FromCurrent && binding is Slider slider)
            {
                _value.Capture(slider.value);
            }
        }

        /// <inheritdoc/>
        public override void OnDeactivated()
        {
            _value.ClearCapture();
        }
    }
}
