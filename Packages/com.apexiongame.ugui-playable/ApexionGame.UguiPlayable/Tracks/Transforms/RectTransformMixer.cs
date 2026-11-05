using UnityEngine;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Transforms
{
    /// <summary>
    /// Blends <see cref="RectTransformClip"/> channels into the bound <see cref="RectTransform"/>.
    /// </summary>
    /// <remarks>
    /// Vector channels blend per axis, so clips that animate different axes of one property do not disturb each
    /// other. Properties are written in the order pivot, anchors, size, position, rotation, scale, because changing
    /// the pivot or the anchors changes what the size and the anchored position mean.
    /// </remarks>
    public sealed class RectTransformMixer : TweenMixer<RectTransform, RectTransformBehaviour>
    {
        private const float ROTATION_SYNC_DEGREES = 0.01f;

        private Vector3 _baseAnchoredPosition;
        private Vector2 _baseSizeDelta;
        private Vector3 _baseRotation;
        private Vector3 _baseScale;
        private Vector2 _basePivot;
        private Vector2 _baseAnchorMin;
        private Vector2 _baseAnchorMax;

        private Vector3Accumulator _anchoredPosition;
        private Vector2Accumulator _sizeDelta;
        private Vector3Accumulator _rotation;
        private Vector3Accumulator _scale;
        private Vector2Accumulator _pivot;
        private Vector2Accumulator _anchorMin;
        private Vector2Accumulator _anchorMax;

        private AxisMask _wroteAnchoredPosition;
        private AxisMask _wroteSizeDelta;
        private AxisMask _wroteRotation;
        private AxisMask _wroteScale;
        private AxisMask _wrotePivot;
        private AxisMask _wroteAnchorMin;
        private AxisMask _wroteAnchorMax;

        // The euler angles this mixer last wrote. localEulerAngles is rebuilt from the quaternion on read and can
        // come back as a flipped decomposition (X in [0, 90] ∪ [270, 360)), so rotation never reads it back except
        // to resync after another system rotated the transform.
        private Vector3 _lastRotation;

        // The oscillation offsets written last frame, 0 on axes without one. The binding holds pre-offset value +
        // last offset, so subtracting them gives the value the tween channels work from; this is what keeps an
        // offset from drifting or leaking into FromCurrent captures.
        private Vector3 _lastOffsetPosition;
        private Vector3 _lastOffsetRotation;
        private Vector3 _lastOffsetScale;

        // This frame's offset sums and the axes that have one.
        private Vector3 _offsetPosition;
        private Vector3 _offsetRotation;
        private Vector3 _offsetScale;
        private AxisMask _offsetAxesPosition;
        private AxisMask _offsetAxesRotation;
        private AxisMask _offsetAxesScale;

        /// <inheritdoc/>
        protected override void CaptureBase(RectTransform binding)
        {
            _baseAnchoredPosition = binding.anchoredPosition3D;
            _baseSizeDelta = binding.sizeDelta;
            _baseRotation = AxisMaskMath.NormalizeEuler(binding.localEulerAngles);
            _baseScale = binding.localScale;
            _basePivot = binding.pivot;
            _baseAnchorMin = binding.anchorMin;
            _baseAnchorMax = binding.anchorMax;
            _lastRotation = _baseRotation;
            _lastOffsetPosition = Vector3.zero;
            _lastOffsetRotation = Vector3.zero;
            _lastOffsetScale = Vector3.zero;
            _wroteAnchoredPosition = AxisMask.None;
            _wroteSizeDelta = AxisMask.None;
            _wroteRotation = AxisMask.None;
            _wroteScale = AxisMask.None;
            _wrotePivot = AxisMask.None;
            _wroteAnchorMin = AxisMask.None;
            _wroteAnchorMax = AxisMask.None;
        }

        /// <inheritdoc/>
        /// <remarks>Only the axes this mixer wrote are restored; other axes keep their current value.</remarks>
        protected override void RestoreBase(RectTransform binding)
        {
            if (_wrotePivot != AxisMask.None)
            {
                binding.pivot = AxisMaskMath.Merge(binding.pivot, _basePivot, _wrotePivot);
            }

            if (_wroteAnchorMin != AxisMask.None)
            {
                binding.anchorMin = AxisMaskMath.Merge(binding.anchorMin, _baseAnchorMin, _wroteAnchorMin);
            }

            if (_wroteAnchorMax != AxisMask.None)
            {
                binding.anchorMax = AxisMaskMath.Merge(binding.anchorMax, _baseAnchorMax, _wroteAnchorMax);
            }

            if (_wroteSizeDelta != AxisMask.None)
            {
                binding.sizeDelta = AxisMaskMath.Merge(binding.sizeDelta, _baseSizeDelta, _wroteSizeDelta);
            }

            if (_wroteAnchoredPosition != AxisMask.None)
            {
                binding.anchoredPosition3D = AxisMaskMath.Merge(
                      binding.anchoredPosition3D
                    , _baseAnchoredPosition
                    , _wroteAnchoredPosition
                );
            }

            if (_wroteRotation != AxisMask.None)
            {
                SyncRotation(binding);
                binding.localEulerAngles = AxisMaskMath.Merge(_lastRotation, _baseRotation, _wroteRotation);
            }

            if (_wroteScale != AxisMask.None)
            {
                binding.localScale = AxisMaskMath.Merge(binding.localScale, _baseScale, _wroteScale);
            }

            _lastOffsetPosition = Vector3.zero;
            _lastOffsetRotation = Vector3.zero;
            _lastOffsetScale = Vector3.zero;
        }

        /// <inheritdoc/>
        protected override void OnInputActivated(RectTransformBehaviour behaviour, RectTransform binding)
        {
            SyncRotation(binding);
            behaviour.CaptureRotation(_lastRotation);

            behaviour.CapturePreOffset(
                  binding.anchoredPosition3D - _lastOffsetPosition
                , binding.localScale - _lastOffsetScale
            );
        }

        /// <inheritdoc/>
        protected override void BeginFrame()
        {
            _anchoredPosition.Reset();
            _sizeDelta.Reset();
            _rotation.Reset();
            _scale.Reset();
            _pivot.Reset();
            _anchorMin.Reset();
            _anchorMax.Reset();

            _offsetPosition = Vector3.zero;
            _offsetRotation = Vector3.zero;
            _offsetScale = Vector3.zero;
            _offsetAxesPosition = AxisMask.None;
            _offsetAxesRotation = AxisMask.None;
            _offsetAxesScale = AxisMask.None;
        }

        /// <inheritdoc/>
        protected override void Accumulate(RectTransformBehaviour behaviour, float weight, float progress)
        {
            var anchoredPosition = behaviour.AnchoredPosition;

            if (anchoredPosition.Enabled)
            {
                var value = TweenChannelMath.Evaluate(anchoredPosition, progress, _baseAnchoredPosition);
                _anchoredPosition.Add(value, weight, anchoredPosition.IgnoredAxes);
            }

            var sizeDelta = behaviour.SizeDelta;

            if (sizeDelta.Enabled)
            {
                var value = TweenChannelMath.Evaluate(sizeDelta, progress, _baseSizeDelta);
                _sizeDelta.Add(value, weight, sizeDelta.IgnoredAxes);
            }

            var rotation = behaviour.Rotation;

            if (rotation.Enabled)
            {
                var value = TweenChannelMath.Evaluate(rotation, progress, _baseRotation);
                _rotation.Add(value, weight, rotation.IgnoredAxes);
            }

            var scale = behaviour.Scale;

            if (scale.Enabled)
            {
                var value = TweenChannelMath.Evaluate(scale, progress, _baseScale);
                _scale.Add(value, weight, scale.IgnoredAxes);
            }

            var pivot = behaviour.Pivot;

            if (pivot.Enabled)
            {
                var value = TweenChannelMath.Evaluate(pivot, progress, _basePivot);
                _pivot.Add(value, weight, pivot.IgnoredAxes);
            }

            var anchorMin = behaviour.AnchorMin;

            if (anchorMin.Enabled)
            {
                var value = TweenChannelMath.Evaluate(anchorMin, progress, _baseAnchorMin);
                _anchorMin.Add(value, weight, anchorMin.IgnoredAxes);
            }

            var anchorMax = behaviour.AnchorMax;

            if (anchorMax.Enabled)
            {
                var value = TweenChannelMath.Evaluate(anchorMax, progress, _baseAnchorMax);
                _anchorMax.Add(value, weight, anchorMax.IgnoredAxes);
            }

            var punch = behaviour.Punch;

            if (punch.Enabled)
            {
                AccumulateOffset(punch, isShake: false, weight, progress);
            }

            var shake = behaviour.Shake;

            if (shake.Enabled)
            {
                AccumulateOffset(shake, isShake: true, weight, progress);
            }
        }

        private void AccumulateOffset(in OscillationChannel channel, bool isShake, float weight, float progress)
        {
            var offset = OscillationMath.Evaluate(channel, progress, isShake) * weight;
            var strength = channel.Strength;
            var axes = ~channel.IgnoredAxes & (AxisMask.X | AxisMask.Y | AxisMask.Z);

            if (strength.x == 0f)
            {
                axes &= ~AxisMask.X;
            }

            if (strength.y == 0f)
            {
                axes &= ~AxisMask.Y;
            }

            if (strength.z == 0f)
            {
                axes &= ~AxisMask.Z;
            }

            switch (channel.Target)
            {
                case OscillationTarget.Position:
                {
                    _offsetPosition += offset;
                    _offsetAxesPosition |= axes;
                    break;
                }

                case OscillationTarget.Rotation:
                {
                    _offsetRotation += offset;
                    _offsetAxesRotation |= axes;
                    break;
                }

                case OscillationTarget.Scale:
                {
                    _offsetScale += offset;
                    _offsetAxesScale |= axes;
                    break;
                }
            }
        }

        /// <inheritdoc/>
        protected override void AccumulateHold(RectTransformBehaviour behaviour, double endTime)
        {
            var anchoredPosition = behaviour.AnchoredPosition;

            if (anchoredPosition.Enabled)
            {
                var value = TweenChannelMath.Evaluate(anchoredPosition, 1f, _baseAnchoredPosition);
                _anchoredPosition.OfferHold(value, endTime, anchoredPosition.IgnoredAxes);
            }

            var sizeDelta = behaviour.SizeDelta;

            if (sizeDelta.Enabled)
            {
                var value = TweenChannelMath.Evaluate(sizeDelta, 1f, _baseSizeDelta);
                _sizeDelta.OfferHold(value, endTime, sizeDelta.IgnoredAxes);
            }

            var rotation = behaviour.Rotation;

            if (rotation.Enabled)
            {
                var value = TweenChannelMath.Evaluate(rotation, 1f, _baseRotation);
                _rotation.OfferHold(value, endTime, rotation.IgnoredAxes);
            }

            var scale = behaviour.Scale;

            if (scale.Enabled)
            {
                var value = TweenChannelMath.Evaluate(scale, 1f, _baseScale);
                _scale.OfferHold(value, endTime, scale.IgnoredAxes);
            }

            var pivot = behaviour.Pivot;

            if (pivot.Enabled)
            {
                var value = TweenChannelMath.Evaluate(pivot, 1f, _basePivot);
                _pivot.OfferHold(value, endTime, pivot.IgnoredAxes);
            }

            var anchorMin = behaviour.AnchorMin;

            if (anchorMin.Enabled)
            {
                var value = TweenChannelMath.Evaluate(anchorMin, 1f, _baseAnchorMin);
                _anchorMin.OfferHold(value, endTime, anchorMin.IgnoredAxes);
            }

            var anchorMax = behaviour.AnchorMax;

            if (anchorMax.Enabled)
            {
                var value = TweenChannelMath.Evaluate(anchorMax, 1f, _baseAnchorMax);
                _anchorMax.OfferHold(value, endTime, anchorMax.IgnoredAxes);
            }
        }

        /// <inheritdoc/>
        protected override void Apply(RectTransform binding)
        {
            if (_pivot.HasContribution)
            {
                binding.pivot = _pivot.Resolve(_basePivot, binding.pivot);
                _wrotePivot |= _pivot.ContributedAxes;
            }

            if (_anchorMin.HasContribution)
            {
                binding.anchorMin = _anchorMin.Resolve(_baseAnchorMin, binding.anchorMin);
                _wroteAnchorMin |= _anchorMin.ContributedAxes;
            }

            if (_anchorMax.HasContribution)
            {
                binding.anchorMax = _anchorMax.Resolve(_baseAnchorMax, binding.anchorMax);
                _wroteAnchorMax |= _anchorMax.ContributedAxes;
            }

            if (_sizeDelta.HasContribution)
            {
                binding.sizeDelta = _sizeDelta.Resolve(_baseSizeDelta, binding.sizeDelta);
                _wroteSizeDelta |= _sizeDelta.ContributedAxes;
            }

            var hadOffsetPosition = IsNonZero(_lastOffsetPosition);

            if (_anchoredPosition.HasContribution || _offsetAxesPosition != AxisMask.None || hadOffsetPosition)
            {
                var pre = binding.anchoredPosition3D - _lastOffsetPosition;

                if (_anchoredPosition.HasContribution)
                {
                    pre = _anchoredPosition.Resolve(_baseAnchoredPosition, pre);
                }

                binding.anchoredPosition3D = pre + _offsetPosition;
                _wroteAnchoredPosition |= _anchoredPosition.ContributedAxes | _offsetAxesPosition;
                _lastOffsetPosition = _offsetPosition;
            }

            var hadOffsetRotation = IsNonZero(_lastOffsetRotation);

            if (_rotation.HasContribution || _offsetAxesRotation != AxisMask.None || hadOffsetRotation)
            {
                SyncRotation(binding);

                if (_rotation.HasContribution)
                {
                    _lastRotation = _rotation.Resolve(_baseRotation, _lastRotation);
                }

                binding.localEulerAngles = _lastRotation + _offsetRotation;
                _wroteRotation |= _rotation.ContributedAxes | _offsetAxesRotation;
                _lastOffsetRotation = _offsetRotation;
            }

            var hadOffsetScale = IsNonZero(_lastOffsetScale);

            if (_scale.HasContribution || _offsetAxesScale != AxisMask.None || hadOffsetScale)
            {
                var pre = binding.localScale - _lastOffsetScale;

                if (_scale.HasContribution)
                {
                    pre = _scale.Resolve(_baseScale, pre);
                }

                binding.localScale = pre + _offsetScale;
                _wroteScale |= _scale.ContributedAxes | _offsetAxesScale;
                _lastOffsetScale = _offsetScale;
            }
        }

        // Vector3's == treats tiny vectors as equal; an offset that was written must be subtracted exactly.
        private static bool IsNonZero(Vector3 value)
            => value.x != 0f || value.y != 0f || value.z != 0f;

        // Re-reads the rotation only when another system rotated the transform since this mixer last wrote it.
        private void SyncRotation(RectTransform binding)
        {
            var written = Quaternion.Euler(_lastRotation + _lastOffsetRotation);

            if (Quaternion.Angle(written, binding.localRotation) > ROTATION_SYNC_DEGREES)
            {
                _lastRotation = AxisMaskMath.NormalizeEuler(binding.localEulerAngles);
                _lastOffsetRotation = Vector3.zero;
            }
        }
    }
}
