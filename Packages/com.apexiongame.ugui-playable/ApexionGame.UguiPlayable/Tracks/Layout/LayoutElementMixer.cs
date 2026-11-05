using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Layout
{
    /// <summary>
    /// Blends <see cref="LayoutElementClip"/> channels into the bound <see cref="LayoutElement"/>.
    /// </summary>
    /// <remarks>
    /// Each size blends on its own and is written only when a clip animates it, so a size no clip touches keeps the
    /// value the element has. Every setter marks the layout dirty, so a layout group rebuilds on the next layout pass.
    /// </remarks>
    public sealed class LayoutElementMixer : TweenMixer<LayoutElement, LayoutElementBehaviour>
    {
        private SizeBlend _minWidth;
        private SizeBlend _minHeight;
        private SizeBlend _preferredWidth;
        private SizeBlend _preferredHeight;
        private SizeBlend _flexibleWidth;
        private SizeBlend _flexibleHeight;

        /// <inheritdoc/>
        protected override void CaptureBase(LayoutElement binding)
        {
            _minWidth.Capture(binding.minWidth);
            _minHeight.Capture(binding.minHeight);
            _preferredWidth.Capture(binding.preferredWidth);
            _preferredHeight.Capture(binding.preferredHeight);
            _flexibleWidth.Capture(binding.flexibleWidth);
            _flexibleHeight.Capture(binding.flexibleHeight);
        }

        /// <inheritdoc/>
        /// <remarks>Only the sizes this mixer wrote are restored.</remarks>
        protected override void RestoreBase(LayoutElement binding)
        {
            if (_minWidth.Wrote)
            {
                binding.minWidth = _minWidth.Base;
            }

            if (_minHeight.Wrote)
            {
                binding.minHeight = _minHeight.Base;
            }

            if (_preferredWidth.Wrote)
            {
                binding.preferredWidth = _preferredWidth.Base;
            }

            if (_preferredHeight.Wrote)
            {
                binding.preferredHeight = _preferredHeight.Base;
            }

            if (_flexibleWidth.Wrote)
            {
                binding.flexibleWidth = _flexibleWidth.Base;
            }

            if (_flexibleHeight.Wrote)
            {
                binding.flexibleHeight = _flexibleHeight.Base;
            }
        }

        /// <inheritdoc/>
        protected override void BeginFrame()
        {
            _minWidth.Reset();
            _minHeight.Reset();
            _preferredWidth.Reset();
            _preferredHeight.Reset();
            _flexibleWidth.Reset();
            _flexibleHeight.Reset();
        }

        /// <inheritdoc/>
        protected override void Accumulate(LayoutElementBehaviour behaviour, float weight, float progress)
        {
            _minWidth.Accumulate(behaviour.MinWidth, weight, progress);
            _minHeight.Accumulate(behaviour.MinHeight, weight, progress);
            _preferredWidth.Accumulate(behaviour.PreferredWidth, weight, progress);
            _preferredHeight.Accumulate(behaviour.PreferredHeight, weight, progress);
            _flexibleWidth.Accumulate(behaviour.FlexibleWidth, weight, progress);
            _flexibleHeight.Accumulate(behaviour.FlexibleHeight, weight, progress);
        }

        /// <inheritdoc/>
        protected override void AccumulateHold(LayoutElementBehaviour behaviour, double endTime)
        {
            _minWidth.AccumulateHold(behaviour.MinWidth, endTime);
            _minHeight.AccumulateHold(behaviour.MinHeight, endTime);
            _preferredWidth.AccumulateHold(behaviour.PreferredWidth, endTime);
            _preferredHeight.AccumulateHold(behaviour.PreferredHeight, endTime);
            _flexibleWidth.AccumulateHold(behaviour.FlexibleWidth, endTime);
            _flexibleHeight.AccumulateHold(behaviour.FlexibleHeight, endTime);
        }

        /// <inheritdoc/>
        protected override void Apply(LayoutElement binding)
        {
            if (_minWidth.TryResolve(out var minWidth))
            {
                binding.minWidth = minWidth;
            }

            if (_minHeight.TryResolve(out var minHeight))
            {
                binding.minHeight = minHeight;
            }

            if (_preferredWidth.TryResolve(out var preferredWidth))
            {
                binding.preferredWidth = preferredWidth;
            }

            if (_preferredHeight.TryResolve(out var preferredHeight))
            {
                binding.preferredHeight = preferredHeight;
            }

            if (_flexibleWidth.TryResolve(out var flexibleWidth))
            {
                binding.flexibleWidth = flexibleWidth;
            }

            if (_flexibleHeight.TryResolve(out var flexibleHeight))
            {
                binding.flexibleHeight = flexibleHeight;
            }
        }

        // One size of the element: its base value, this frame's blend and whether it was ever written. The mixer owns
        // each instance as a field and must not copy it.
        private struct SizeBlend
        {
            private FloatAccumulator _accumulator;

            public float Base { get; private set; }

            public bool Wrote { get; private set; }

            public void Capture(float value)
            {
                Base = value;
                Wrote = false;
            }

            public void Reset()
            {
                _accumulator.Reset();
            }

            public void Accumulate(in TweenChannel<float> channel, float weight, float progress)
            {
                if (channel.Enabled)
                {
                    _accumulator.Add(TweenChannelMath.Evaluate(channel, progress, Base), weight);
                }
            }

            public void AccumulateHold(in TweenChannel<float> channel, double endTime)
            {
                if (channel.Enabled)
                {
                    _accumulator.OfferHold(TweenChannelMath.Evaluate(channel, 1f, Base), endTime);
                }
            }

            public bool TryResolve(out float value)
            {
                if (_accumulator.HasContribution == false)
                {
                    value = default;
                    return false;
                }

                value = _accumulator.Resolve(Base);
                Wrote = true;
                return true;
            }
        }
    }
}
