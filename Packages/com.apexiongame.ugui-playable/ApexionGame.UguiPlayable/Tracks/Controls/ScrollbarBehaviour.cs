using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Controls
{
    /// <summary>
    /// The channels of a <see cref="ScrollbarClip"/>: value and handle size, both 0 to 1.
    /// </summary>
    /// <remarks>
    /// <see cref="Scrollbar.onValueChanged"/> follows the track's <see cref="ScrollbarTrack.SendCallbacks"/> switch:
    /// off by default, so scrubbing, playing and Revert write the value silently; on, the track notifies in Play Mode
    /// only. In Edit Mode, Unity itself calls onValueChanged once when the preview ends. A linked
    /// <see cref="ScrollRect"/> scrolls only through that callback: to scroll content, animate the ScrollRect track,
    /// which updates its scrollbars, or turn on Send callbacks. The size channel is disabled by default and never
    /// notifies.
    /// </remarks>
    [Serializable]
    public sealed class ScrollbarBehaviour : TweenBehaviour
    {
        internal const string VALUE_PROPERTY = "m_Value";
        internal const string SIZE_PROPERTY = "m_Size";

        [SerializeField]
        private TweenChannel<float> _value = new(0f, 1f);

        [SerializeField]
        private TweenChannel<float> _size = new(0.2f, 1f, enabled: false);

        /// <summary>The <see cref="Scrollbar.value"/> channel.</summary>
        public TweenChannel<float> Value
        {
            get => _value;
            set => _value = value;
        }

        /// <summary>The <see cref="Scrollbar.size"/> channel.</summary>
        public TweenChannel<float> Size
        {
            get => _size;
            set => _size = value;
        }

        /// <inheritdoc/>
        internal override bool HasEnabledChannel()
            => _value.Enabled
                || _size.Enabled;

        /// <inheritdoc/>
        public override void CollectPropertyNames(ICollection<string> names)
        {
            if (_value.Enabled)
            {
                names.Add(VALUE_PROPERTY);
            }

            if (_size.Enabled)
            {
                names.Add(SIZE_PROPERTY);
            }
        }

        /// <inheritdoc/>
        public override void OnActivated(Component binding)
        {
            if (binding is not Scrollbar scrollbar)
            {
                return;
            }

            if (_value.Enabled && _value.Mode == ValueMode.FromCurrent)
            {
                _value.Capture(scrollbar.value);
            }

            if (_size.Enabled && _size.Mode == ValueMode.FromCurrent)
            {
                _size.Capture(scrollbar.size);
            }
        }

        /// <inheritdoc/>
        public override void OnDeactivated()
        {
            _value.ClearCapture();
            _size.ClearCapture();
        }
    }
}
