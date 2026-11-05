using System;
using UnityEngine;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// An offset added on top of a property's blended tween result: a damped punch or a deterministic shake. Which
    /// one it is depends on the field that holds the channel, not on the channel itself.
    /// </summary>
    /// <remarks>
    /// The serialized field names are user data.
    /// </remarks>
    [Serializable]
    public struct OscillationChannel
    {
        [SerializeField]
        private bool _enabled;

        [SerializeField]
        private OscillationTarget _target;

        [SerializeField]
        private Vector3 _strength;

        [SerializeField]
        private float _frequency;

        [SerializeField, Range(0f, 1f)]
        private float _elasticity;

        [SerializeField, Range(0f, 1f)]
        private float _randomness;

        [SerializeField]
        private bool _fadeOut;

        [SerializeField]
        private int _seed;

        [SerializeField]
        private AxisMask _ignoredAxes;

        /// <summary>Whether the channel adds its offset.</summary>
        public readonly bool Enabled => _enabled;

        /// <summary>The property the offset is added to.</summary>
        public readonly OscillationTarget Target => _target;

        /// <summary>The peak offset per axis.</summary>
        public readonly Vector3 Strength => _strength;

        /// <summary>
        /// The oscillations over the clip: the punch's vibrato, or the number of noise samples a shake takes.
        /// </summary>
        public readonly float Frequency => _frequency;

        /// <summary>Punch only: from 0 (fast decay) to 1 (slow decay).</summary>
        public readonly float Elasticity => _elasticity;

        /// <summary>
        /// Shake only: 0 makes every axis follow one shared noise, 1 gives each axis its own noise.
        /// </summary>
        public readonly float Randomness => _randomness;

        /// <summary>Shake only: whether the shake fades to zero over the clip.</summary>
        public readonly bool FadeOut => _fadeOut;

        /// <summary>Shake only: the seed of the noise, so scrubbing always gives the same shake.</summary>
        public readonly int Seed => _seed;

        /// <summary>The axes that get no offset.</summary>
        public readonly AxisMask IgnoredAxes => _ignoredAxes;

        /// <summary>Creates a punch channel.</summary>
        public static OscillationChannel Punch(
              OscillationTarget target
            , Vector3 strength
            , float frequency = 4f
            , float elasticity = 0.5f
            , bool enabled = true
        )
        {
            return new OscillationChannel
            {
                _enabled = enabled,
                _target = target,
                _strength = strength,
                _frequency = frequency,
                _elasticity = elasticity,
                _randomness = 0.5f,
                _fadeOut = true,
                _seed = 0,
                _ignoredAxes = AxisMask.None,
            };
        }

        /// <summary>Creates a shake channel.</summary>
        public static OscillationChannel Shake(
              OscillationTarget target
            , Vector3 strength
            , float frequency = 10f
            , float randomness = 0.5f
            , bool fadeOut = true
            , int seed = 0
            , bool enabled = true
        )
        {
            return new OscillationChannel
            {
                _enabled = enabled,
                _target = target,
                _strength = strength,
                _frequency = frequency,
                _elasticity = 0.5f,
                _randomness = randomness,
                _fadeOut = fadeOut,
                _seed = seed,
                _ignoredAxes = AxisMask.None,
            };
        }
    }
}
