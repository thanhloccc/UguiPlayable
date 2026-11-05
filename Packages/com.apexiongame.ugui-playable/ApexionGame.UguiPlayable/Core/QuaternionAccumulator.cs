using UnityEngine;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// Blends one whole rotation for one frame, as a sign-aligned weighted sum of quaternions. The quaternion twin of
    /// <see cref="Vector3Accumulator"/>, with the rule of <see cref="FloatAccumulator"/> applied to the rotation.
    /// </summary>
    /// <remarks>
    /// q and -q are the same rotation, so every quaternion is flipped into the hemisphere of the first one before it
    /// is summed, and the hold or the base is flipped into the hemisphere of the sum before the top-up.
    /// </remarks>
    internal struct QuaternionAccumulator
    {
        private Vector4 _sum;
        private Quaternion _reference;
        private Quaternion _hold;
        private double _holdEnd;
        private float _weight;
        private bool _hasHold;

        /// <summary>Whether <see cref="Resolve"/> has anything to write this frame.</summary>
        public bool HasContribution => _weight > 0f || _hasHold;

        /// <summary>Whether a finished clip offered its end rotation this frame.</summary>
        public bool HasHold => _hasHold;

        /// <summary>The sum of the weights of the active clips this frame.</summary>
        public float Weight => _weight;

        /// <summary>The end time of the clip whose rotation is held, or 0 without a hold.</summary>
        public double HoldEndTime => _holdEnd;

        /// <inheritdoc cref="FloatAccumulator.Reset"/>
        public void Reset()
        {
            this = default;
        }

        /// <summary>Adds an active clip's rotation with its input weight.</summary>
        public void Add(Quaternion q, float weight)
        {
            if (weight <= 0f)
            {
                return;
            }

            if (_weight <= 0f)
            {
                _reference = q;
            }

            var value = Align(q, _reference);
            _sum += value * weight;
            _weight += weight;
        }

        /// <summary>Offers a finished clip's end rotation; the clip that ended last wins.</summary>
        public void OfferHold(Quaternion q, double endTime)
        {
            if (_hasHold && endTime < _holdEnd)
            {
                return;
            }

            _hold = q;
            _holdEnd = endTime;
            _hasHold = true;
        }

        /// <summary>
        /// Returns the blended rotation, or <paramref name="currentValue"/> when nothing contributed.
        /// </summary>
        /// <param name="baseValue">The rotation the property had when the track bound it.</param>
        /// <param name="currentValue">The rotation the property has now.</param>
        public Quaternion Resolve(Quaternion baseValue, Quaternion currentValue)
        {
            if (_weight <= 0f && _hasHold == false)
            {
                return currentValue;
            }

            if (_weight <= 0f)
            {
                return _hold;
            }

            var sum = _sum;

            if (_weight < 1f)
            {
                var topUp = Align(_hasHold ? _hold : baseValue, _reference);
                sum += topUp * (1f - _weight);
            }

            return ToNormalizedQuaternion(sum, currentValue);
        }

        private static Vector4 Align(Quaternion q, Quaternion reference)
        {
            var value = new Vector4(q.x, q.y, q.z, q.w);
            return Quaternion.Dot(q, reference) < 0f ? -value : value;
        }

        private static Quaternion ToNormalizedQuaternion(Vector4 sum, Quaternion fallback)
        {
            var magnitude = sum.magnitude;

            if (magnitude <= Mathf.Epsilon)
            {
                return fallback;
            }

            sum /= magnitude;
            return new Quaternion(sum.x, sum.y, sum.z, sum.w);
        }
    }
}
