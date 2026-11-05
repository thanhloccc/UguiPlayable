using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Controls
{
    /// <summary>
    /// The channel of a <see cref="ToggleClip"/>: <see cref="Toggle.isOn"/> as a step.
    /// </summary>
    /// <remarks>
    /// The step value applies when the clip passes its threshold, and the last finished clip holds it.
    /// <see cref="Toggle.onValueChanged"/> follows the track's <see cref="ToggleTrack.SendCallbacks"/> switch: off by
    /// default, so scrubbing, playing and Revert write the value silently; on, the track notifies in Play Mode only.
    /// In Edit Mode, Unity itself calls onValueChanged once per toggle registered for the preview when the preview
    /// ends. In a <see cref="ToggleGroup"/> that does not allow switching off, the group turns the last toggle back on
    /// when it is switched off.
    /// </remarks>
    [Serializable]
    public sealed class ToggleBehaviour : TweenBehaviour
    {
        internal const string IS_ON_PROPERTY = "m_IsOn";

        [SerializeField]
        private StepChannel _isOn = new(true);

        /// <summary>The <see cref="Toggle.isOn"/> channel.</summary>
        public StepChannel IsOn
        {
            get => _isOn;
            set => _isOn = value;
        }

        /// <inheritdoc/>
        internal override bool HasEnabledChannel()
            => _isOn.Enabled;

        /// <inheritdoc/>
        public override void CollectPropertyNames(ICollection<string> names)
        {
            if (_isOn.Enabled)
            {
                names.Add(IS_ON_PROPERTY);
            }
        }
    }
}
