using UnityEngine;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Graphics
{
    /// <summary>
    /// Blends <see cref="RawImageClip"/> channels into the bound <see cref="RawImage"/>.
    /// </summary>
    /// <remarks>
    /// The UV rect blends per axis as a <see cref="Vector4"/> (x, y, width, height) and is written once as a
    /// <see cref="Rect"/>. Only the axes a clip animates are written and reverted.
    /// </remarks>
    public sealed class RawImageMixer : TweenMixer<RawImage, RawImageBehaviour>
    {
        private GraphicColorBlend _blend;
        private Vector4 _baseUvRect;
        private Vector4Accumulator _uvRect;
        private AxisMask _wroteUvRect;

        /// <inheritdoc/>
        protected override void CaptureBase(RawImage binding)
        {
            _blend.Capture(binding);
            _baseUvRect = ToVector4(binding.uvRect);
            _wroteUvRect = AxisMask.None;
        }

        /// <inheritdoc/>
        /// <remarks>Only the UV rect axes this mixer wrote are restored.</remarks>
        protected override void RestoreBase(RawImage binding)
        {
            _blend.Restore(binding);

            if (_wroteUvRect != AxisMask.None)
            {
                var merged = AxisMaskMath.Merge(ToVector4(binding.uvRect), _baseUvRect, _wroteUvRect);
                binding.uvRect = ToRect(merged);
            }
        }

        /// <inheritdoc/>
        protected override void BeginFrame()
        {
            _blend.Reset();
            _uvRect.Reset();
        }

        /// <inheritdoc/>
        protected override void Accumulate(RawImageBehaviour behaviour, float weight, float progress)
        {
            _blend.Accumulate(behaviour.Color, behaviour.Alpha, weight, progress);

            var uvRect = behaviour.UvRect;

            if (uvRect.Enabled)
            {
                var value = TweenChannelMath.Evaluate(uvRect, progress, _baseUvRect);
                _uvRect.Add(value, weight, uvRect.IgnoredAxes);
            }
        }

        /// <inheritdoc/>
        protected override void AccumulateHold(RawImageBehaviour behaviour, double endTime)
        {
            _blend.AccumulateHold(behaviour.Color, behaviour.Alpha, endTime);

            var uvRect = behaviour.UvRect;

            if (uvRect.Enabled)
            {
                var value = TweenChannelMath.Evaluate(uvRect, 1f, _baseUvRect);
                _uvRect.OfferHold(value, endTime, uvRect.IgnoredAxes);
            }
        }

        /// <inheritdoc/>
        protected override void Apply(RawImage binding)
        {
            _blend.Apply(binding);

            if (_uvRect.HasContribution)
            {
                var resolved = _uvRect.Resolve(_baseUvRect, ToVector4(binding.uvRect));
                binding.uvRect = ToRect(resolved);
                _wroteUvRect |= _uvRect.ContributedAxes;
            }
        }

        private static Vector4 ToVector4(Rect rect)
            => new(rect.x, rect.y, rect.width, rect.height);

        private static Rect ToRect(Vector4 value)
            => new(value.x, value.y, value.z, value.w);
    }
}
