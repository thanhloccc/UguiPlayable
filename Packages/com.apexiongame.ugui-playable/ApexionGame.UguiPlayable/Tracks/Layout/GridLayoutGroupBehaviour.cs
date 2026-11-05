using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Layout
{
    /// <summary>
    /// The channels of a <see cref="GridLayoutGroupClip"/>: spacing, cell size and padding.
    /// </summary>
    /// <remarks>
    /// The padding channel holds (left, right, top, bottom), with the axes L, R, T and B, and is rounded to whole
    /// pixels when it is written.
    /// </remarks>
    [Serializable]
    public sealed class GridLayoutGroupBehaviour : TweenBehaviour
    {
        internal const string SPACING_PROPERTY = "m_Spacing";
        internal const string CELL_SIZE_PROPERTY = "m_CellSize";

        [SerializeField]
        private TweenChannel<Vector2> _spacing = new(Vector2.zero, new Vector2(10f, 10f));

        [SerializeField]
        private TweenChannel<Vector2> _cellSize = new(new Vector2(100f, 100f), new Vector2(120f, 120f), enabled: false);

        [SerializeField]
        private TweenChannel<Vector4> _padding = new(Vector4.zero, new Vector4(10f, 10f, 10f, 10f), enabled: false);

        /// <summary>The <see cref="GridLayoutGroup.spacing"/> channel.</summary>
        public TweenChannel<Vector2> Spacing
        {
            get => _spacing;
            set => _spacing = value;
        }

        /// <summary>The <see cref="GridLayoutGroup.cellSize"/> channel.</summary>
        public TweenChannel<Vector2> CellSize
        {
            get => _cellSize;
            set => _cellSize = value;
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
                || _cellSize.Enabled
                || _padding.Enabled;

        /// <inheritdoc/>
        public override void CollectPropertyNames(ICollection<string> names)
        {
            if (_spacing.Enabled)
            {
                names.Add(SPACING_PROPERTY);
            }

            if (_cellSize.Enabled)
            {
                names.Add(CELL_SIZE_PROPERTY);
            }

            if (_padding.Enabled)
            {
                names.Add(LayoutGroupBehaviour.PADDING_LEFT_PROPERTY);
                names.Add(LayoutGroupBehaviour.PADDING_RIGHT_PROPERTY);
                names.Add(LayoutGroupBehaviour.PADDING_TOP_PROPERTY);
                names.Add(LayoutGroupBehaviour.PADDING_BOTTOM_PROPERTY);
            }
        }

        /// <inheritdoc/>
        public override void OnActivated(Component binding)
        {
            if (binding is not GridLayoutGroup group)
            {
                return;
            }

            if (_spacing.Enabled && _spacing.Mode == ValueMode.FromCurrent)
            {
                _spacing.Capture(group.spacing);
            }

            if (_cellSize.Enabled && _cellSize.Mode == ValueMode.FromCurrent)
            {
                _cellSize.Capture(group.cellSize);
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
            _cellSize.ClearCapture();
            _padding.ClearCapture();
        }
    }
}
