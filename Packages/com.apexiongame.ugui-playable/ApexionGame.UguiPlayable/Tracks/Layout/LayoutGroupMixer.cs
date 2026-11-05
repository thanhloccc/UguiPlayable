using UnityEngine;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Layout
{
    /// <summary>
    /// Blends <see cref="LayoutGroupClip"/> channels into the bound <see cref="HorizontalOrVerticalLayoutGroup"/>.
    /// </summary>
    /// <remarks>
    /// The padding blends per axis and is written in place (see <c>LayoutPaddingWriter</c>), so only the sides a clip
    /// animates are written and reverted. The spacing setter marks the layout dirty itself.
    /// </remarks>
    public sealed class LayoutGroupMixer : TweenMixer<HorizontalOrVerticalLayoutGroup, LayoutGroupBehaviour>
    {
        private float _baseSpacing;
        private Vector4 _basePadding;
        private FloatAccumulator _spacing;
        private Vector4Accumulator _padding;
        private bool _wroteSpacing;
        private AxisMask _wrotePadding;

        /// <inheritdoc/>
        protected override void CaptureBase(HorizontalOrVerticalLayoutGroup binding)
        {
            _baseSpacing = binding.spacing;
            _basePadding = LayoutPaddingWriter.Read(binding);
            _wroteSpacing = false;
            _wrotePadding = AxisMask.None;
        }

        /// <inheritdoc/>
        /// <remarks>Only the spacing and padding sides this mixer wrote are restored.</remarks>
        protected override void RestoreBase(HorizontalOrVerticalLayoutGroup binding)
        {
            if (_wroteSpacing)
            {
                binding.spacing = _baseSpacing;
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
            _padding.Reset();
        }

        /// <inheritdoc/>
        protected override void Accumulate(LayoutGroupBehaviour behaviour, float weight, float progress)
        {
            var spacing = behaviour.Spacing;

            if (spacing.Enabled)
            {
                _spacing.Add(TweenChannelMath.Evaluate(spacing, progress, _baseSpacing), weight);
            }

            var padding = behaviour.Padding;

            if (padding.Enabled)
            {
                var value = TweenChannelMath.Evaluate(padding, progress, _basePadding);
                _padding.Add(value, weight, padding.IgnoredAxes);
            }
        }

        /// <inheritdoc/>
        protected override void AccumulateHold(LayoutGroupBehaviour behaviour, double endTime)
        {
            var spacing = behaviour.Spacing;

            if (spacing.Enabled)
            {
                _spacing.OfferHold(TweenChannelMath.Evaluate(spacing, 1f, _baseSpacing), endTime);
            }

            var padding = behaviour.Padding;

            if (padding.Enabled)
            {
                var value = TweenChannelMath.Evaluate(padding, 1f, _basePadding);
                _padding.OfferHold(value, endTime, padding.IgnoredAxes);
            }
        }

        /// <inheritdoc/>
        protected override void Apply(HorizontalOrVerticalLayoutGroup binding)
        {
            if (_spacing.HasContribution)
            {
                binding.spacing = _spacing.Resolve(_baseSpacing);
                _wroteSpacing = true;
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
