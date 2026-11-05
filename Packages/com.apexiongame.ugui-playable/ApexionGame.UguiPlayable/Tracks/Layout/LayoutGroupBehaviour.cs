using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Layout
{
    /// <summary>
    /// The channels of a <see cref="LayoutGroupClip"/>: spacing and padding.
    /// </summary>
    /// <remarks>
    /// The padding channel holds (left, right, top, bottom), with the axes L, R, T and B, and is rounded to whole
    /// pixels when it is written.
    /// </remarks>
    [Serializable]
    public sealed class LayoutGroupBehaviour : TweenBehaviour
    {
        internal const string SPACING_PROPERTY = "m_Spacing";
        internal const string PADDING_LEFT_PROPERTY = "m_Padding.m_Left";
        internal const string PADDING_RIGHT_PROPERTY = "m_Padding.m_Right";
        internal const string PADDING_TOP_PROPERTY = "m_Padding.m_Top";
        internal const string PADDING_BOTTOM_PROPERTY = "m_Padding.m_Bottom";

        [SerializeField]
        private TweenChannel<float> _spacing = new(0f, 20f);

        [SerializeField]
        private TweenChannel<Vector4> _padding = new(Vector4.zero, new Vector4(10f, 10f, 10f, 10f), enabled: false);

        /// <summary>The <see cref="HorizontalOrVerticalLayoutGroup.spacing"/> channel.</summary>
        public TweenChannel<float> Spacing
        {
            get => _spacing;
            set => _spacing = value;
        }

        /// <summary>The <see cref="LayoutGroup.padding"/> channel, as (left, right, top, bottom).</summary>
        public TweenChannel<Vector4> Padding
        {
            get => _padding;
            set => _padding = value;
        }

        /// <inheritdoc/>
        internal override bool HasEnabledChannel()
            => _spacing.Enabled
                || _padding.Enabled;

        /// <inheritdoc/>
        public override void CollectPropertyNames(ICollection<string> names)
        {
            if (_spacing.Enabled)
            {
                names.Add(SPACING_PROPERTY);
            }

            if (_padding.Enabled)
            {
                names.Add(PADDING_LEFT_PROPERTY);
                names.Add(PADDING_RIGHT_PROPERTY);
                names.Add(PADDING_TOP_PROPERTY);
                names.Add(PADDING_BOTTOM_PROPERTY);
            }
        }

        /// <inheritdoc/>
        public override void OnActivated(Component binding)
        {
            if (binding is not HorizontalOrVerticalLayoutGroup group)
            {
                return;
            }

            if (_spacing.Enabled && _spacing.Mode == ValueMode.FromCurrent)
            {
                _spacing.Capture(group.spacing);
            }

            if (_padding.Enabled && _padding.Mode == ValueMode.FromCurrent)
            {
                _padding.Capture(LayoutPaddingWriter.Read(group));
            }
        }

        /// <inheritdoc/>
        public override void OnDeactivated()
        {
            _spacing.ClearCapture();
            _padding.ClearCapture();
        }
    }
}
