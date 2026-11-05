using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Controls
{
    /// <summary>
    /// The channel of a <see cref="ScrollRectClip"/>: the normalized scroll position, X horizontal (0 left, 1 right)
    /// and Y vertical (0 bottom, 1 top).
    /// </summary>
    /// <remarks>
    /// The channel honours <see cref="TweenChannel{T}.IgnoredAxes"/>, so a clip can scroll one axis only. The default
    /// scrolls from the top to the bottom.
    /// </remarks>
    [Serializable]
    public sealed class ScrollRectBehaviour : TweenBehaviour
    {
        // Not a serialized property: the track maps this name to the content and scrollbar properties it drives.
        internal const string NORMALIZED_POSITION_PROPERTY = "normalizedPosition";

        [SerializeField]
        private TweenChannel<Vector2> _normalizedPosition = new(new Vector2(0f, 1f), Vector2.zero);

        /// <summary>The <see cref="ScrollRect.normalizedPosition"/> channel.</summary>
        public TweenChannel<Vector2> NormalizedPosition
        {
            get => _normalizedPosition;
            set => _normalizedPosition = value;
        }

        /// <inheritdoc/>
        internal override bool HasEnabledChannel()
            => _normalizedPosition.Enabled;

        /// <inheritdoc/>
        public override void CollectPropertyNames(ICollection<string> names)
        {
            if (_normalizedPosition.Enabled)
            {
                names.Add(NORMALIZED_POSITION_PROPERTY);
            }
        }

        /// <inheritdoc/>
        public override void OnActivated(Component binding)
        {
            if (_normalizedPosition.Enabled
                && _normalizedPosition.Mode == ValueMode.FromCurrent
                && binding is ScrollRect scrollRect
            )
            {
                _normalizedPosition.Capture(scrollRect.normalizedPosition);
            }
        }

        /// <inheritdoc/>
        public override void OnDeactivated()
        {
            _normalizedPosition.ClearCapture();
        }
    }
}
