using UnityEngine;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Groups
{
    /// <summary>
    /// Blends <see cref="CanvasGroupClip"/> channels into the bound <see cref="CanvasGroup"/>.
    /// </summary>
    public sealed class CanvasGroupMixer : TweenMixer<CanvasGroup, CanvasGroupBehaviour>
    {
        private float _baseAlpha;
        private bool _baseInteractable;
        private bool _baseBlocksRaycasts;

        private FloatAccumulator _alpha;
        private StepAccumulator _interactable;
        private StepAccumulator _blocksRaycasts;

        private bool _wroteAlpha;
        private bool _wroteInteractable;
        private bool _wroteBlocksRaycasts;

        /// <inheritdoc/>
        protected override void CaptureBase(CanvasGroup binding)
        {
            _baseAlpha = binding.alpha;
            _baseInteractable = binding.interactable;
            _baseBlocksRaycasts = binding.blocksRaycasts;
            _wroteAlpha = false;
            _wroteInteractable = false;
            _wroteBlocksRaycasts = false;
        }

        /// <inheritdoc/>
        protected override void RestoreBase(CanvasGroup binding)
        {
            if (_wroteAlpha)
            {
                binding.alpha = _baseAlpha;
            }

            if (_wroteInteractable)
            {
                binding.interactable = _baseInteractable;
            }

            if (_wroteBlocksRaycasts)
            {
                binding.blocksRaycasts = _baseBlocksRaycasts;
            }
        }

        /// <inheritdoc/>
        protected override void BeginFrame()
        {
            _alpha.Reset();
            _interactable.Reset();
            _blocksRaycasts.Reset();
        }

        /// <inheritdoc/>
        protected override void Accumulate(CanvasGroupBehaviour behaviour, float weight, float progress)
        {
            var alpha = behaviour.Alpha;

            if (alpha.Enabled)
            {
                _alpha.Add(TweenChannelMath.Evaluate(alpha, progress, _baseAlpha), weight);
            }

            var interactable = behaviour.Interactable;

            if (interactable.Enabled)
            {
                _interactable.Add(interactable, progress, weight);
            }

            var blocksRaycasts = behaviour.BlocksRaycasts;

            if (blocksRaycasts.Enabled)
            {
                _blocksRaycasts.Add(blocksRaycasts, progress, weight);
            }
        }

        /// <inheritdoc/>
        protected override void AccumulateHold(CanvasGroupBehaviour behaviour, double endTime)
        {
            var alpha = behaviour.Alpha;

            if (alpha.Enabled)
            {
                _alpha.OfferHold(TweenChannelMath.Evaluate(alpha, 1f, _baseAlpha), endTime);
            }

            var interactable = behaviour.Interactable;

            if (interactable.Enabled)
            {
                _interactable.OfferHold(interactable, endTime);
            }

            var blocksRaycasts = behaviour.BlocksRaycasts;

            if (blocksRaycasts.Enabled)
            {
                _blocksRaycasts.OfferHold(blocksRaycasts, endTime);
            }
        }

        /// <inheritdoc/>
        protected override void Apply(CanvasGroup binding)
        {
            if (_alpha.HasContribution)
            {
                binding.alpha = _alpha.Resolve(_baseAlpha);
                _wroteAlpha = true;
            }

            if (_interactable.HasContribution)
            {
                binding.interactable = _interactable.Resolve(_baseInteractable);
                _wroteInteractable = true;
            }

            if (_blocksRaycasts.HasContribution)
            {
                binding.blocksRaycasts = _blocksRaycasts.Resolve(_baseBlocksRaycasts);
                _wroteBlocksRaycasts = true;
            }
        }
    }
}
