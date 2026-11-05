using UnityEngine;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// The pure math of <see cref="OscillationChannel"/>. Nothing here allocates or uses a random generator, so
    /// scrubbing a clip always gives the same result.
    /// </summary>
    public static class OscillationMath
    {
        private const int SHARED_AXIS = 3;

        /// <summary>
        /// Returns a damped oscillation at clip progress <paramref name="p"/> (clamped to 0..1). It is exactly 0 at
        /// both ends and moves toward positive first.
        /// </summary>
        public static float Punch(float p, float frequency, float elasticity)
        {
            p = Mathf.Clamp01(p);

            var decay = Mathf.Pow(1f - p, 1f + 3f * (1f - elasticity));

            return Mathf.Sin(2f * Mathf.PI * frequency * p) * decay;
        }

        /// <summary>
        /// Returns smooth value noise in [-1, 1] for one axis, from <paramref name="frequency"/> samples over the
        /// clip. Axis ids 0 to 2 are the axes; 3 is the shared noise.
        /// </summary>
        public static float ShakeAxis(float p, float frequency, int seed, int axis)
        {
            var t = p * frequency;
            var index = Mathf.FloorToInt(t);
            var f = t - index;

            var a = Hash01(seed, axis, index) * 2f - 1f;
            var b = Hash01(seed, axis, index + 1) * 2f - 1f;

            return Mathf.Lerp(a, b, f * f * (3f - 2f * f));
        }

        /// <summary>
        /// Returns the unit shake at clip progress <paramref name="p"/>, 0 at both ends. The caller multiplies it by
        /// the strength per axis.
        /// </summary>
        public static Vector3 Shake(float p, float frequency, int seed, float randomness, bool fadeOut)
        {
            var t = p * frequency;
            var shared = ShakeAxis(p, frequency, seed, SHARED_AXIS);

            var x = Mathf.Lerp(shared, ShakeAxis(p, frequency, seed, 0), randomness);
            var y = Mathf.Lerp(shared, ShakeAxis(p, frequency, seed, 1), randomness);
            var z = Mathf.Lerp(shared, ShakeAxis(p, frequency, seed, 2), randomness);

            var envelope = Mathf.Clamp01(t) * Mathf.Clamp01(frequency - t) * (fadeOut ? 1f - p : 1f);

            return new Vector3(x, y, z) * envelope;
        }

        /// <summary>
        /// Returns the offset of <paramref name="channel"/> at clip progress <paramref name="p"/>. The caller says
        /// whether the channel is a shake or a punch.
        /// </summary>
        public static Vector3 Evaluate(in OscillationChannel channel, float p, bool isShake)
        {
            p = Mathf.Clamp01(p);

            var strength = channel.Strength;
            var ignored = channel.IgnoredAxes;
            Vector3 unit;

            if (isShake)
            {
                unit = Shake(p, channel.Frequency, channel.Seed, channel.Randomness, channel.FadeOut);
            }
            else
            {
                var punch = Punch(p, channel.Frequency, channel.Elasticity);
                unit = new Vector3(punch, punch, punch);
            }

            return new Vector3(
                  (ignored & AxisMask.X) != 0 ? 0f : strength.x * unit.x
                , (ignored & AxisMask.Y) != 0 ? 0f : strength.y * unit.y
                , (ignored & AxisMask.Z) != 0 ? 0f : strength.z * unit.z
            );
        }

        // An integer hash of (seed, axis, index) mapped to [0, 1).
        private static float Hash01(int seed, int axis, int index)
        {
            unchecked
            {
                var h = (uint)seed * 0x9E3779B1u;
                h ^= (uint)axis * 0x85EBCA6Bu + 0x7F4A7C15u;
                h = (h ^ (h >> 15)) * 0x2C1B3C6Du;
                h ^= (uint)index * 0xC2B2AE35u;
                h = (h ^ (h >> 13)) * 0x297A2D39u;
                h ^= h >> 16;
                h *= 0x85EBCA6Bu;
                h ^= h >> 13;

                return (h >> 8) * (1f / 16777216f);
            }
        }
    }
}
