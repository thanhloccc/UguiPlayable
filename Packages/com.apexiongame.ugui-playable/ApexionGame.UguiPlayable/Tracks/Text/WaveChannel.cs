using System;
using UnityEngine;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Text
{
    /// <summary>
    /// A wave that runs through the characters of a <see cref="TmpCharacterClip"/>: an offset added on top of the
    /// transitions, with no hold after the clip.
    /// </summary>
    /// <remarks>
    /// The phase of a character is <c>cycles · p − phasePerCharacter · rank</c>, with <c>p</c> the clip progress and
    /// <c>rank</c> the character's place in the clip's order. The serialized field names are user data.
    /// </remarks>
    [Serializable]
    public struct WaveChannel
    {
        [SerializeField]
        private bool _enabled;

        [SerializeField]
        private OscillationTarget _target;

        [SerializeField]
        private WaveShape _shape;

        [SerializeField]
        private Vector3 _amplitude;

        [SerializeField]
        private float _cycles;

        [SerializeField]
        private float _phasePerCharacter;

        /// <summary>
        /// Creates a wave channel.
        /// </summary>
        public WaveChannel(
              OscillationTarget target
            , WaveShape shape
            , Vector3 amplitude
            , float cycles = 2f
            , float phasePerCharacter = 0.1f
            , bool enabled = true
        )
        {
            _enabled = enabled;
            _target = target;
            _shape = shape;
            _amplitude = amplitude;
            _cycles = cycles;
            _phasePerCharacter = phasePerCharacter;
        }

        /// <summary>Whether the wave adds its offset.</summary>
        public readonly bool Enabled => _enabled;

        /// <summary>
        /// What the wave moves: the position (X and Y of <see cref="Amplitude"/>, in text units), the rotation (Z, in
        /// degrees) or the scale (X and Y, added to the scale).
        /// </summary>
        public readonly OscillationTarget Target => _target;

        /// <summary>The shape of the wave.</summary>
        public readonly WaveShape Shape => _shape;

        /// <summary>The peak offset per axis.</summary>
        public readonly Vector3 Amplitude => _amplitude;

        /// <summary>The number of wave cycles over the clip.</summary>
        public readonly float Cycles => _cycles;

        /// <summary>The phase delay, in cycles, from one character to the next in the clip's order.</summary>
        public readonly float PhasePerCharacter => _phasePerCharacter;
    }
}
