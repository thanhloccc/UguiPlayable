using System;
using UnityEngine;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// A boolean property that switches to <see cref="Value"/> once the clip's progress reaches
    /// <see cref="Threshold"/>. Step channels are not blended: the active input with the highest weight decides.
    /// </summary>
    /// <remarks>
    /// The serialized field names are user data.
    /// </remarks>
    [Serializable]
    public struct StepChannel
    {
        /// <summary>The threshold used when none is given.</summary>
        public const float DEFAULT_THRESHOLD = 0.5f;

        [SerializeField]
        private bool _enabled;

        [SerializeField]
        private bool _value;

        [SerializeField, Range(0f, 1f)]
        private float _threshold;

        /// <summary>
        /// Creates a step channel.
        /// </summary>
        public StepChannel(bool value, float threshold = DEFAULT_THRESHOLD, bool enabled = true)
        {
            _enabled = enabled;
            _value = value;
            _threshold = threshold;
        }

        /// <summary>Whether the channel drives its property.</summary>
        public bool Enabled => _enabled;

        /// <summary>The value written once <see cref="Threshold"/> is reached.</summary>
        public bool Value => _value;

        /// <summary>The normalized clip progress, from 0 to 1, at which <see cref="Value"/> applies.</summary>
        public float Threshold => _threshold;

        /// <summary>
        /// Returns true and sets <paramref name="value"/> when <paramref name="progress"/> has reached the threshold.
        /// </summary>
        public bool TryEvaluate(float progress, out bool value)
        {
            value = _value;
            return progress >= _threshold;
        }
    }
}
