using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Graphics
{
    /// <summary>
    /// Blends <see cref="ImageClip"/> channels into the bound <see cref="Image"/>.
    /// </summary>
    public sealed class ImageMixer : TweenMixer<Image, ImageBehaviour>
    {
        private GraphicColorBlend _blend;
        private float _baseFillAmount;
        private FloatAccumulator _fillAmount;
        private bool _wroteFillAmount;

        /// <inheritdoc/>
        protected override void CaptureBase(Image binding)
        {
            _blend.Capture(binding);
            _baseFillAmount = binding.fillAmount;
            _wroteFillAmount = false;
        }

        /// <inheritdoc/>
        protected override void RestoreBase(Image binding)
        {
            _blend.Restore(binding);

            if (_wroteFillAmount)
            {
                binding.fillAmount = _baseFillAmount;
            }
        }

        /// <inheritdoc/>
        protected override void BeginFrame()
        {
            _blend.Reset();
            _fillAmount.Reset();
        }

        /// <inheritdoc/>
        protected override void Accumulate(ImageBehaviour behaviour, float weight, float progress)
        {
            _blend.Accumulate(behaviour.Color, behaviour.Alpha, weight, progress);

            var fillAmount = behaviour.FillAmount;

            if (fillAmount.Enabled)
            {
                _fillAmount.Add(TweenChannelMath.Evaluate(fillAmount, progress, _baseFillAmount), weight);
            }
        }

        /// <inheritdoc/>
        protected override void AccumulateHold(ImageBehaviour behaviour, double endTime)
        {
            _blend.AccumulateHold(behaviour.Color, behaviour.Alpha, endTime);

            var fillAmount = behaviour.FillAmount;

            if (fillAmount.Enabled)
            {
                _fillAmount.OfferHold(TweenChannelMath.Evaluate(fillAmount, 1f, _baseFillAmount), endTime);
            }
        }

        /// <inheritdoc/>
        protected override void Apply(Image binding)
        {
            _blend.Apply(binding);

            if (_fillAmount.HasContribution)
            {
                binding.fillAmount = _fillAmount.Resolve(_baseFillAmount);
                _wroteFillAmount = true;
            }
        }
    }
}
