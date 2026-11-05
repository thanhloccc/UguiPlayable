using UnityEngine;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Controls
{
    /// <summary>
    /// Blends <see cref="ScrollRectClip"/> channels into the bound <see cref="ScrollRect"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The base position is captured when the track binds the scroll rect, which can happen before the content has
    /// been laid out. The mixer therefore captures it again while it has not written yet and the content size has
    /// changed since the capture.
    /// </para>
    /// <para>
    /// A write sets <see cref="ScrollRect.normalizedPosition"/> and then clears <see cref="ScrollRect.velocity"/>, so
    /// inertia does not fight the animation. Only the axes a clip animates are written and reverted; the other axis
    /// keeps its current position. A scroll rect without a content is left alone.
    /// </para>
    /// </remarks>
    public sealed class ScrollRectMixer : TweenMixer<ScrollRect, ScrollRectBehaviour>
    {
        private Vector2 _basePosition;
        private Vector2 _capturedContentSize;
        private Vector2Accumulator _position;
        private AxisMask _wrotePosition;

        /// <inheritdoc/>
        protected override void CaptureBase(ScrollRect binding)
        {
            _wrotePosition = AxisMask.None;
            Capture(binding);
        }

        /// <inheritdoc/>
        /// <remarks>Only the axes this mixer wrote are restored; the other axis keeps its current position.</remarks>
        protected override void RestoreBase(ScrollRect binding)
        {
            if (_wrotePosition == AxisMask.None || binding.content.IsInvalid())
            {
                return;
            }

            binding.normalizedPosition = AxisMaskMath.Merge(binding.normalizedPosition, _basePosition, _wrotePosition);
            binding.velocity = Vector2.zero;
        }

        /// <inheritdoc/>
        protected override void BeginFrame()
        {
            _position.Reset();

            var binding = Binding;

            if (_wrotePosition == AxisMask.None
                && binding.IsValid()
                && binding.content.IsValid()
                && binding.content.rect.size != _capturedContentSize
            )
            {
                Capture(binding);
            }
        }

        /// <inheritdoc/>
        protected override void Accumulate(ScrollRectBehaviour behaviour, float weight, float progress)
        {
            var position = behaviour.NormalizedPosition;

            if (position.Enabled)
            {
                var value = TweenChannelMath.Evaluate(position, progress, _basePosition);
                _position.Add(value, weight, position.IgnoredAxes);
            }
        }

        /// <inheritdoc/>
        protected override void AccumulateHold(ScrollRectBehaviour behaviour, double endTime)
        {
            var position = behaviour.NormalizedPosition;

            if (position.Enabled)
            {
                var value = TweenChannelMath.Evaluate(position, 1f, _basePosition);
                _position.OfferHold(value, endTime, position.IgnoredAxes);
            }
        }

        /// <inheritdoc/>
        protected override void Apply(ScrollRect binding)
        {
            if (_position.HasContribution == false || binding.content.IsInvalid())
            {
                return;
            }

            binding.normalizedPosition = _position.Resolve(_basePosition, binding.normalizedPosition);
            binding.velocity = Vector2.zero;
            _wrotePosition |= _position.ContributedAxes;
        }

        private void Capture(ScrollRect binding)
        {
            var content = binding.content;

            if (content.IsInvalid())
            {
                _basePosition = Vector2.zero;
                _capturedContentSize = Vector2.zero;
                return;
            }

            _basePosition = binding.normalizedPosition;
            _capturedContentSize = content.rect.size;
        }
    }
}
