using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Easing;

namespace ApexionGame.UguiPlayable.Tracks.Text
{
    /// <summary>
    /// The timing and channels of a <see cref="TmpCharacterClip"/>: which characters animate and when, five
    /// transitions (offset, rotation, scale, alpha, tint), a wave and a per-character shake.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each character of the range gets its own progress: <c>pᵢ = clamp01((p − s·u) / (1 − s))</c>, eased by
    /// <see cref="Ease"/>, with <c>p</c> the clip progress, <c>s</c> the <see cref="Stagger"/> and <c>u</c> the
    /// character's place in the <see cref="Order"/>, from 0 to 1. The order counts only the visible characters of the
    /// range (spaces and characters hidden by a typewriter are skipped).
    /// </para>
    /// <para>
    /// Transitions blend toward the rest pose and hold their end value after the clip. The wave and the shake are
    /// offsets with no hold. Only the alpha channel is on by default: the default clip is a per-character fade-in.
    /// </para>
    /// <para>
    /// The track writes the text mesh, never a serialized property, so no property is registered for the preview.
    /// </para>
    /// </remarks>
    [Serializable]
    public sealed class TmpCharacterBehaviour : TweenBehaviour
    {
        /// <summary>The largest <see cref="Stagger"/>: at 1 a character would have no time left to animate.</summary>
        public const float MAX_STAGGER = 0.99f;

        [SerializeField]
        private int _rangeStart;

        [SerializeField]
        private int _rangeLength;

        [SerializeField, Range(0f, MAX_STAGGER)]
        private float _stagger = 0.5f;

        [SerializeField]
        private CharacterOrder _order = CharacterOrder.Forward;

        [SerializeField]
        private int _orderSeed;

        [SerializeField]
        private EaseSetting _ease = new(Easing.Ease.OutCubic);

        [SerializeField]
        private CharacterPivot _pivot = CharacterPivot.Center;

        [SerializeField]
        private CharacterChannel<Vector2> _offset = new(new Vector2(0f, -30f), Vector2.zero, enabled: false);

        [SerializeField]
        private CharacterChannel<float> _rotation = new(90f, 0f, enabled: false);

        [SerializeField]
        private CharacterChannel<Vector2> _scale = new(Vector2.zero, Vector2.one, enabled: false);

        [SerializeField]
        private bool _scaleConstrainFrom;

        [SerializeField]
        private bool _scaleConstrainTo;

        [SerializeField]
        private CharacterChannel<float> _alpha = new(0f, 1f, enabled: true);

        [SerializeField]
        private CharacterChannel<Color> _color = new(UnityEngine.Color.white, UnityEngine.Color.white, enabled: false);

        [SerializeField]
        private WaveChannel _wave = new(
              OscillationTarget.Position
            , WaveShape.Sine
            , new Vector3(0f, 10f, 0f)
            , enabled: false
        );

        [SerializeField]
        private OscillationChannel _shake = OscillationChannel.Shake(
              OscillationTarget.Position
            , new Vector3(3f, 3f, 0f)
            , enabled: false
        );

        /// <summary>
        /// The first animated character, as an index in <see cref="TMP_TextInfo.characterInfo"/> (rich-text tags are
        /// not counted).
        /// </summary>
        public int RangeStart
        {
            get => _rangeStart;
            set => _rangeStart = value;
        }

        /// <summary>The number of animated characters from <see cref="RangeStart"/>; 0 means to the end.</summary>
        public int RangeLength
        {
            get => _rangeLength;
            set => _rangeLength = value;
        }

        /// <summary>
        /// The share of the clip, from 0 to <see cref="MAX_STAGGER"/>, over which the characters' starts are spread.
        /// 0 animates every character together.
        /// </summary>
        public float Stagger
        {
            get => _stagger;
            set => _stagger = value;
        }

        /// <summary>The order in which the characters start.</summary>
        public CharacterOrder Order
        {
            get => _order;
            set => _order = value;
        }

        /// <summary>The seed of the <see cref="CharacterOrder.Random"/> order.</summary>
        public int OrderSeed
        {
            get => _orderSeed;
            set => _orderSeed = value;
        }

        /// <summary>The ease applied to each character's progress.</summary>
        public EaseSetting Ease
        {
            get => _ease;
            set => _ease = value;
        }

        /// <summary>The point each character rotates and scales about.</summary>
        public CharacterPivot Pivot
        {
            get => _pivot;
            set => _pivot = value;
        }

        /// <summary>The position offset of each character, in text units.</summary>
        public CharacterChannel<Vector2> Offset
        {
            get => _offset;
            set => _offset = value;
        }

        /// <summary>The rotation of each character about Z, in degrees.</summary>
        public CharacterChannel<float> Rotation
        {
            get => _rotation;
            set => _rotation = value;
        }

        /// <summary>
        /// The scale of each character. TMP does not update the SDF scale, so scales well above 1 soften the edges.
        /// </summary>
        public CharacterChannel<Vector2> Scale
        {
            get => _scale;
            set => _scale = value;
        }

        /// <summary>Editor only: keep the scale's from value proportional. The runtime ignores it.</summary>
        public bool ScaleConstrainFrom
        {
            get => _scaleConstrainFrom;
            set => _scaleConstrainFrom = value;
        }

        /// <summary>Editor only: keep the scale's to value proportional. The runtime ignores it.</summary>
        public bool ScaleConstrainTo
        {
            get => _scaleConstrainTo;
            set => _scaleConstrainTo = value;
        }

        /// <summary>The alpha multiplier of each character.</summary>
        public CharacterChannel<float> Alpha
        {
            get => _alpha;
            set => _alpha = value;
        }

        /// <summary>The tint each character's vertex colours are multiplied by.</summary>
        public CharacterChannel<Color> Color
        {
            get => _color;
            set => _color = value;
        }

        /// <summary>The wave offset.</summary>
        public WaveChannel Wave
        {
            get => _wave;
            set => _wave = value;
        }

        /// <summary>The shake offset; each character shakes with its own noise.</summary>
        public OscillationChannel Shake
        {
            get => _shake;
            set => _shake = value;
        }

        /// <summary>Whether any transition channel is enabled: only those offer a hold value.</summary>
        internal bool HasTransition
            => _offset.Enabled || _rotation.Enabled || _scale.Enabled || _alpha.Enabled || _color.Enabled;

        /// <summary>
        /// The animated character indices of a text with <paramref name="characterCount"/> characters:
        /// [<paramref name="start"/>, <paramref name="end"/>), clamped to the text.
        /// </summary>
        internal void GetRange(int characterCount, out int start, out int end)
        {
            start = Mathf.Clamp(_rangeStart, 0, characterCount);
            end = _rangeLength > 0 ? Mathf.Min(characterCount, start + _rangeLength) : characterCount;
        }

        /// <summary>Whether the character at <paramref name="index"/> is in the animated range.</summary>
        internal bool IsInRange(int index, int characterCount)
        {
            GetRange(characterCount, out var start, out var end);
            return index >= start && index < end;
        }

        /// <inheritdoc/>
        internal override bool HasEnabledChannel()
            => HasTransition || _wave.Enabled || _shake.Enabled;

        /// <inheritdoc/>
        /// <remarks>The track writes the text mesh, which is never serialized, so this adds nothing.</remarks>
        public override void CollectPropertyNames(ICollection<string> names)
        {
        }
    }
}
