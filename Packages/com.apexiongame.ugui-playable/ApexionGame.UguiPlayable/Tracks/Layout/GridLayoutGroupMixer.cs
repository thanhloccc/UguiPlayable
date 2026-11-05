using UnityEngine;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Layout
{
    /// <summary>
    /// Blends <see cref="GridLayoutGroupClip"/> channels into the bound <see cref="GridLayoutGroup"/>.
    /// </summary>
    /// <remarks>
    /// Spacing, cell size and padding blend per axis, so only the axes a clip animates are written and reverted.
    /// The padding is written in place (see <c>LayoutPaddingWriter</c>). The spacing and cell size setters mark the
    /// layout dirty themselves.
    /// </remarks>
    public sealed class GridLayoutGroupMixer : TweenMixer<GridLayoutGroup, GridLayoutGroupBehaviour>
    {
        private Vector2 _baseSpacing;
        private Vector2 _baseCellSize;
        private Vector4 _basePadding;
        private Vector2Accumulator _spacing;
        private Vector2Accumulator _cellSize;
        private Vector4Accumulator _padding;
        private AxisMask _wroteSpacing;
        private AxisMask _wroteCellSize;
        private AxisMask _wrotePadding;

        /// <inheritdoc/>
        protected override void CaptureBase(GridLayoutGroup binding)
        {
            _baseSpacing = binding.spacing;
            _baseCellSize = binding.cellSize;
            _basePadding = LayoutPaddingWriter.Read(binding);
            _wroteSpacing = AxisMask.None;
            _wroteCellSize = AxisMask.None;
            _wrotePadding = AxisMask.None;
        }

        /// <inheritdoc/>
        /// <remarks>Only the axes this mixer wrote are restored.</remarks>
        protected override void RestoreBase(GridLayoutGroup binding)
        {
            if (_wroteSpacing != AxisMask.None)
            {
                binding.spacing = AxisMaskMath.Merge(binding.spacing, _baseSpacing, _wroteSpacing);
            }

            if (_wroteCellSize != AxisMask.None)
            {
                binding.cellSize = AxisMaskMath.Merge(binding.cellSize, _baseCellSize, _wroteCellSize);
            }

            if (_wrotePadding != AxisMask.None)
            {
                var merged = AxisMaskMath.Merge(LayoutPaddingWriter.Read(binding), _basePadding, _wrotePadding);
                LayoutPaddingWriter.Write(binding, merged);
            }
        }

        /// <inheritdoc/>
        protected override void BeginFrame()
        {
            _spacing.Reset();
            _cellSize.Reset();
            _padding.Reset();
        }

        /// <inheritdoc/>
        protected override void Accumulate(GridLayoutGroupBehaviour behaviour, float weight, float progress)
        {
            var spacing = behaviour.Spacing;

            if (spacing.Enabled)
            {
                var value = TweenChannelMath.Evaluate(spacing, progress, _baseSpacing);
                _spacing.Add(value, weight, spacing.IgnoredAxes);
            }

            var cellSize = behaviour.CellSize;

            if (cellSize.Enabled)
            {
                var value = TweenChannelMath.Evaluate(cellSize, progress, _baseCellSize);
                _cellSize.Add(value, weight, cellSize.IgnoredAxes);
            }

            var padding = behaviour.Padding;

            if (padding.Enabled)
            {
                var value = TweenChannelMath.Evaluate(padding, progress, _basePadding);
                _padding.Add(value, weight, padding.IgnoredAxes);
            }
        }

        /// <inheritdoc/>
        protected override void AccumulateHold(GridLayoutGroupBehaviour behaviour, double endTime)
        {
            var spacing = behaviour.Spacing;

            if (spacing.Enabled)
            {
                var value = TweenChannelMath.Evaluate(spacing, 1f, _baseSpacing);
                _spacing.OfferHold(value, endTime, spacing.IgnoredAxes);
            }

            var cellSize = behaviour.CellSize;

            if (cellSize.Enabled)
            {
                var value = TweenChannelMath.Evaluate(cellSize, 1f, _baseCellSize);
                _cellSize.OfferHold(value, endTime, cellSize.IgnoredAxes);
            }

            var padding = behaviour.Padding;

            if (padding.Enabled)
            {
                var value = TweenChannelMath.Evaluate(padding, 1f, _basePadding);
                _padding.OfferHold(value, endTime, padding.IgnoredAxes);
            }
        }

        /// <inheritdoc/>
        protected override void Apply(GridLayoutGroup binding)
        {
            if (_spacing.HasContribution)
            {
                binding.spacing = _spacing.Resolve(_baseSpacing, binding.spacing);
                _wroteSpacing |= _spacing.ContributedAxes;
            }

            if (_cellSize.HasContribution)
            {
                binding.cellSize = _cellSize.Resolve(_baseCellSize, binding.cellSize);
                _wroteCellSize |= _cellSize.ContributedAxes;
            }

            if (_padding.HasContribution)
            {
                var resolved = _padding.Resolve(_basePadding, LayoutPaddingWriter.Read(binding));
                LayoutPaddingWriter.Write(binding, resolved);
                _wrotePadding |= _padding.ContributedAxes;
            }
        }
    }
}
