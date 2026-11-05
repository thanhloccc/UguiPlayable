using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Layout
{
    /// <summary>
    /// The channels of a <see cref="LayoutElementClip"/>: minimum, preferred and flexible width and height.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All channels are off by default. A negative value (-1 in a fresh LayoutElement) means "not set" to uGUI: the
    /// layout group then ignores that size. A channel that tweens through a negative value therefore switches the size
    /// off for those frames, so keep both ends of a channel at 0 or above unless that is what you want.
    /// </para>
    /// <para>
    /// The flexible sizes are weights, not pixels: a flexible width of 1 asks the group to share the free space.
    /// </para>
    /// </remarks>
    [Serializable]
    public sealed class LayoutElementBehaviour : TweenBehaviour
    {
        internal const string MIN_WIDTH_PROPERTY = "m_MinWidth";
        internal const string MIN_HEIGHT_PROPERTY = "m_MinHeight";
        internal const string PREFERRED_WIDTH_PROPERTY = "m_PreferredWidth";
        internal const string PREFERRED_HEIGHT_PROPERTY = "m_PreferredHeight";
        internal const string FLEXIBLE_WIDTH_PROPERTY = "m_FlexibleWidth";
        internal const string FLEXIBLE_HEIGHT_PROPERTY = "m_FlexibleHeight";

        [SerializeField]
        private TweenChannel<float> _minWidth = new(0f, 100f, enabled: false);

        [SerializeField]
        private TweenChannel<float> _minHeight = new(0f, 100f, enabled: false);

        [SerializeField]
        private TweenChannel<float> _preferredWidth = new(0f, 100f, enabled: false);

        [SerializeField]
        private TweenChannel<float> _preferredHeight = new(0f, 100f, enabled: false);

        [SerializeField]
        private TweenChannel<float> _flexibleWidth = new(0f, 1f, enabled: false);

        [SerializeField]
        private TweenChannel<float> _flexibleHeight = new(0f, 1f, enabled: false);

        /// <summary>The <see cref="LayoutElement.minWidth"/> channel.</summary>
        public TweenChannel<float> MinWidth
        {
            get => _minWidth;
            set => _minWidth = value;
        }

        /// <summary>The <see cref="LayoutElement.minHeight"/> channel.</summary>
        public TweenChannel<float> MinHeight
        {
            get => _minHeight;
            set => _minHeight = value;
        }

        /// <summary>The <see cref="LayoutElement.preferredWidth"/> channel.</summary>
        public TweenChannel<float> PreferredWidth
        {
            get => _preferredWidth;
            set => _preferredWidth = value;
        }

        /// <summary>The <see cref="LayoutElement.preferredHeight"/> channel.</summary>
        public TweenChannel<float> PreferredHeight
        {
            get => _preferredHeight;
            set => _preferredHeight = value;
        }

        /// <summary>The <see cref="LayoutElement.flexibleWidth"/> channel.</summary>
        public TweenChannel<float> FlexibleWidth
        {
            get => _flexibleWidth;
            set => _flexibleWidth = value;
        }

        /// <summary>The <see cref="LayoutElement.flexibleHeight"/> channel.</summary>
        public TweenChannel<float> FlexibleHeight
        {
            get => _flexibleHeight;
            set => _flexibleHeight = value;
        }

        /// <inheritdoc/>
        internal override bool HasEnabledChannel()
            => _minWidth.Enabled
                || _minHeight.Enabled
                || _preferredWidth.Enabled
                || _preferredHeight.Enabled
                || _flexibleWidth.Enabled
                || _flexibleHeight.Enabled;

        /// <inheritdoc/>
        public override void CollectPropertyNames(ICollection<string> names)
        {
            if (_minWidth.Enabled)
            {
                names.Add(MIN_WIDTH_PROPERTY);
            }

            if (_minHeight.Enabled)
            {
                names.Add(MIN_HEIGHT_PROPERTY);
            }

            if (_preferredWidth.Enabled)
            {
                names.Add(PREFERRED_WIDTH_PROPERTY);
            }

            if (_preferredHeight.Enabled)
            {
                names.Add(PREFERRED_HEIGHT_PROPERTY);
            }

            if (_flexibleWidth.Enabled)
            {
                names.Add(FLEXIBLE_WIDTH_PROPERTY);
            }

            if (_flexibleHeight.Enabled)
            {
                names.Add(FLEXIBLE_HEIGHT_PROPERTY);
            }
        }

        /// <inheritdoc/>
        public override void OnActivated(Component binding)
        {
            if (binding is not LayoutElement element)
            {
                return;
            }

            CaptureCurrent(ref _minWidth, element.minWidth);
            CaptureCurrent(ref _minHeight, element.minHeight);
            CaptureCurrent(ref _preferredWidth, element.preferredWidth);
            CaptureCurrent(ref _preferredHeight, element.preferredHeight);
            CaptureCurrent(ref _flexibleWidth, element.flexibleWidth);
            CaptureCurrent(ref _flexibleHeight, element.flexibleHeight);
        }

        /// <inheritdoc/>
        public override void OnDeactivated()
        {
            _minWidth.ClearCapture();
            _minHeight.ClearCapture();
            _preferredWidth.ClearCapture();
            _preferredHeight.ClearCapture();
            _flexibleWidth.ClearCapture();
            _flexibleHeight.ClearCapture();
        }

        private static void CaptureCurrent(ref TweenChannel<float> channel, float current)
        {
            if (channel.Enabled && channel.Mode == ValueMode.FromCurrent)
            {
                channel.Capture(current);
            }
        }
    }
}
