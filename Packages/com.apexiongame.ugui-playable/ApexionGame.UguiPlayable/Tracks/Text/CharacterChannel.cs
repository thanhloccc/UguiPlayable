using System;
using UnityEngine;

namespace ApexionGame.UguiPlayable.Tracks.Text
{
    /// <summary>
    /// One per-character transition of a <see cref="TmpCharacterClip"/>: whether it is enabled, and its from and to
    /// values.
    /// </summary>
    /// <remarks>
    /// The values are always relative to the character's rest pose (offset 0, rotation 0, scale 1, alpha 1, tint
    /// white), so the channel has no value mode. The clip's ease and stagger shape the progress. The serialized field
    /// names are user data.
    /// </remarks>
    [Serializable]
    public struct CharacterChannel<T> where T : struct
    {
        [SerializeField]
        private bool _enabled;

        [SerializeField]
        private T _from;

        [SerializeField]
        private T _to;

        /// <summary>
        /// Creates a channel.
        /// </summary>
        public CharacterChannel(T from, T to, bool enabled = true)
        {
            _enabled = enabled;
            _from = from;
            _to = to;
        }

        /// <summary>Whether the channel animates the characters.</summary>
        public bool Enabled => _enabled;

        /// <summary>The value at the start of a character's transition.</summary>
        public T From => _from;

        /// <summary>The value at the end of a character's transition, held after the clip.</summary>
        public T To => _to;
    }
}
